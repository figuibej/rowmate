using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using RowMate.Core.Common;
using RowMate.Core.Common.Json;
using RowMate.Core.Ports;
using RowMate.Infrastructure.Threading;

namespace RowMate.Infrastructure.Preferences
{
    // prefs.json (ARCHITECTURE §9.4; ADR 021; 02-tech-design §2.7 de la It. 5; AC-WKT-18-11). Hilo principal salvo la escritura.
    // - En memoria: las claves en el orden del archivo, cada una con su valor JSON (el lector del Core, JsonParser: sin
    //   deserialización genérica, RA-05). Las que la app no conoce (hrm.*, scene.* de las It. 8, 11 y 12) se conservan tal cual.
    // - Lectura: una vez, al construirse, en el hilo que lo construye (el arranque: un archivo de unos cientos de bytes). Si
    //   falta, está vacío, no se puede leer o no es un objeto JSON, arranca vacío sin aviso (una línea Info en el registro, sin
    //   la ruta) y el próximo Set guarda un archivo válido.
    // - Una clave de OTRO TIPO (immersive.lapsExpanded = "sí", 1, null…): solo esa da false en TryGet* (el llamador usa su
    //   valor por defecto); las demás se leen, y todas se conservan al guardar, esa también, hasta que un Set la reemplace.
    //   TryGetBool acepta solo un booleano JSON y TryGetString solo una cadena.
    // - Escritura: cada Set o Remove cambia la memoria en el acto y, en el MISMO hilo principal, arma el texto entero (JSON
    //   compacto en UTF-8, unos cientos de bytes, una vez por toque). El ejecutor serie (fuera del hilo principal) solo escribe
    //   bytes: prefs.json.tmp y después lo pone en lugar de prefs.json (File.Replace si existe; File.Move si no), así un corte
    //   deja el viejo o el nuevo, nunca uno a medias.
    // - Escrituras seguidas se juntan: a lo sumo una en curso y una pendiente; un Set nuevo reemplaza los bytes pendientes, así
    //   dos toques rápidos dan dos o una escritura y la última siempre gana.
    // - Si guardar falla: una línea Warning (sin la ruta ni el mensaje del sistema, que la trae) y nada más; el valor en memoria
    //   queda (la misma ejecución sigue con lo que tocó el usuario; después de reiniciar, lo último guardado).
    public sealed class JsonFilePreferences : IPreferences, IDisposable
    {
        public const string Tag = "PREFS";

        private readonly string _path;
        private readonly string _tempPath;
        private readonly IAppLogger _logger;
        private readonly IDbExecutor _writer;
        private readonly IPreferencesFileOps _ops;
        private readonly bool _ownsWriter;
        private readonly List<KeyValuePair<string, Value>> _entries = new List<KeyValuePair<string, Value>>();
        private readonly object _gate = new object();
        private byte[]? _pending;
        private bool _draining;
        private int _writes;
        private bool _disposed;

        // Producción: con su propio hilo de escritura (un SerialExecutor que dispone Dispose).
        public JsonFilePreferences(string path, IAppLogger logger)
            : this(path, logger, new SerialExecutor("RowMate.Preferences", logger ?? throw new ArgumentNullException(nameof(logger))),
                SystemPreferencesFileOps.Instance, true)
        {
        }

        // Pruebas: el ejecutor (en línea, en cola o serie) y las operaciones de archivo (las que fallan).
        internal JsonFilePreferences(string path, IAppLogger logger, IDbExecutor writer, IPreferencesFileOps ops)
            : this(path, logger, writer, ops, false)
        {
        }

        private JsonFilePreferences(string path, IAppLogger logger, IDbExecutor writer, IPreferencesFileOps ops, bool ownsWriter)
        {
            _path = path ?? throw new ArgumentNullException(nameof(path));
            _tempPath = path + PreferencesFiles.TempSuffix;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _writer = writer ?? throw new ArgumentNullException(nameof(writer));
            _ops = ops ?? throw new ArgumentNullException(nameof(ops));
            _ownsWriter = ownsWriter;
            Load();
        }

        // Lee al construirse: siempre leída.
        public bool IsLoaded => true;

        // Nunca se dispara: la lectura terminó al construirse.
        public event Action? Loaded
        {
            add { }
            remove { }
        }

        // Las escrituras que llegaron al disco (las pruebas).
        public int Writes
        {
            get
            {
                lock (_gate)
                {
                    return _writes;
                }
            }
        }

        // Las claves en memoria, en orden (las pruebas).
        public IReadOnlyList<string> Keys
        {
            get
            {
                var keys = new string[_entries.Count];
                for (var i = 0; i < keys.Length; i++)
                {
                    keys[i] = _entries[i].Key;
                }

                return keys;
            }
        }

        public bool TryGetBool(string key, out bool value)
        {
            var index = IndexOf(key);
            if (index >= 0 && _entries[index].Value.Node.Kind == JsonKind.Bool)
            {
                value = _entries[index].Value.Node.AsBool();
                return true;
            }

            value = false;
            return false;
        }

        public bool TryGetString(string key, out string value)
        {
            var index = IndexOf(key);
            if (index >= 0 && _entries[index].Value.Node.Kind == JsonKind.String)
            {
                value = _entries[index].Value.Node.AsString();
                return true;
            }

            value = string.Empty;
            return false;
        }

        public void SetBool(string key, bool value)
        {
            Put(key, Value.Of(value ? "true" : "false", JsonKind.Bool));
        }

        public void SetString(string key, string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            var text = new StringBuilder(value.Length + 2);
            JsonText.WriteString(text, value);
            Put(key, Value.Of(text.ToString(), JsonKind.String));
        }

        public void Remove(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            var index = IndexOf(key);
            if (index < 0)
            {
                return;
            }

            _entries.RemoveAt(index);
            RequestWrite();
        }

        // Espera lo pendiente (las pruebas y el cierre de la app): termina cuando lo que había por escribir al llamar llegó al
        // disco (o falló, sin aviso).
        public Task FlushAsync()
        {
            lock (_gate)
            {
                if (!_draining && _pending == null)
                {
                    return Task.CompletedTask;
                }
            }

            if (_disposed)
            {
                return Task.CompletedTask;
            }

            try
            {
                return _writer.Run(() => { });
            }
            catch (ObjectDisposedException)
            {
                return Task.CompletedTask;
            }
        }

        // No acepta más escrituras (la memoria sigue respondiendo); con su propio hilo, lo cierra después de lo encolado.
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            if (_ownsWriter)
            {
                _writer.Dispose();
            }
        }

        private void Load()
        {
            string text;
            try
            {
                if (!_ops.Exists(_path))
                {
                    return;
                }

                text = _ops.ReadAllText(_path);
            }
            catch (Exception error)
            {
                _logger.Log(LogLevel.Info, Tag, "no se pudo leer el archivo de preferencias (" + error.GetType().Name + "): arranca vacío");
                return;
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                _logger.Log(LogLevel.Info, Tag, "el archivo de preferencias está vacío: arranca vacío");
                return;
            }

            JsonNode root;
            try
            {
                root = JsonParser.Parse(text);
            }
            catch (FormatException)
            {
                _logger.Log(LogLevel.Info, Tag, "el archivo de preferencias está dañado: arranca vacío");
                return;
            }

            if (root.Kind != JsonKind.Object)
            {
                _logger.Log(LogLevel.Info, Tag, "el archivo de preferencias no es un objeto JSON: arranca vacío");
                return;
            }

            foreach (var property in root.Properties)
            {
                var raw = new StringBuilder();
                JsonText.Write(raw, property.Value);
                _entries.Add(new KeyValuePair<string, Value>(property.Key, Value.Of(raw.ToString(), property.Value)));
            }
        }

        private int IndexOf(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            for (var i = 0; i < _entries.Count; i++)
            {
                if (string.Equals(_entries[i].Key, key, StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return -1;
        }

        private void Put(string key, Value value)
        {
            var index = IndexOf(key);
            if (index >= 0)
            {
                _entries[index] = new KeyValuePair<string, Value>(key, value);
            }
            else
            {
                _entries.Add(new KeyValuePair<string, Value>(key, value));
            }

            RequestWrite();
        }

        // El texto se arma acá, en el hilo del Set; el ejecutor recibe bytes.
        private void RequestWrite()
        {
            if (_disposed)
            {
                return;
            }

            var bytes = Encode();
            lock (_gate)
            {
                _pending = bytes;
                if (_draining)
                {
                    return;
                }

                _draining = true;
            }

            Task drain;
            try
            {
                drain = _writer.Run(Drain);
            }
            catch (ObjectDisposedException)
            {
                drain = Task.FromException(new ObjectDisposedException(nameof(JsonFilePreferences)));
            }

            if (drain.IsFaulted)
            {
                // El ejecutor ya está cerrado: lo pendiente queda en memoria.
                lock (_gate)
                {
                    _draining = false;
                }
            }
        }

        // En el hilo del ejecutor: escribe lo pendiente hasta que no quede nada (los Set que llegan mientras tanto reemplazan
        // los bytes pendientes y los toma la vuelta siguiente).
        private void Drain()
        {
            while (true)
            {
                byte[] bytes;
                lock (_gate)
                {
                    if (_pending == null)
                    {
                        _draining = false;
                        return;
                    }

                    bytes = _pending;
                    _pending = null;
                }

                WriteNow(bytes);
            }
        }

        private void WriteNow(byte[] bytes)
        {
            try
            {
                _ops.WriteAllBytes(_tempPath, bytes);
                if (_ops.Exists(_path))
                {
                    _ops.Replace(_tempPath, _path);
                }
                else
                {
                    _ops.Move(_tempPath, _path);
                }

                lock (_gate)
                {
                    _writes++;
                }
            }
            catch (Exception error)
            {
                _logger.Log(LogLevel.Warning, Tag, "no se pudo guardar el archivo de preferencias (" + error.GetType().Name + ")");
                try
                {
                    if (_ops.Exists(_tempPath))
                    {
                        _ops.Delete(_tempPath);
                    }
                }
                catch (Exception)
                {
                    // El temporal que no se pudo borrar lo pisa la próxima escritura.
                }
            }
        }

        private byte[] Encode()
        {
            var text = new StringBuilder(64 + _entries.Count * 40);
            text.Append('{');
            for (var i = 0; i < _entries.Count; i++)
            {
                if (i > 0)
                {
                    text.Append(',');
                }

                JsonText.WriteString(text, _entries[i].Key);
                text.Append(':');
                text.Append(_entries[i].Value.Raw);
            }

            text.Append('}');
            return new UTF8Encoding(false).GetBytes(text.ToString());
        }

        // Un valor: su texto JSON (lo que se escribe) y su nodo (lo que se lee).
        private readonly struct Value
        {
            private Value(string raw, JsonNode node)
            {
                Raw = raw;
                Node = node;
            }

            public string Raw { get; }

            public JsonNode Node { get; }

            public static Value Of(string raw, JsonNode node)
            {
                return new Value(raw, node);
            }

            // Un bool o una cadena de un Set: el nodo sale del mismo texto (así se lee igual que uno del archivo).
            public static Value Of(string raw, JsonKind kind)
            {
                var node = JsonParser.Parse(raw);
                if (node.Kind != kind)
                {
                    throw new InvalidOperationException("JsonFilePreferences: " + raw + " no es " + kind + ".");
                }

                return new Value(raw, node);
            }
        }
    }

    // Un escritor JSON mínimo para lo que lee JsonParser: los números con su texto exacto (RawNumber), las cadenas escapadas
    // según RFC 8259 (comillas, barra invertida y los de control; lo demás tal cual, en UTF-8).
    internal static class JsonText
    {
        public static void Write(StringBuilder text, JsonNode node)
        {
            switch (node.Kind)
            {
                case JsonKind.Null:
                    text.Append("null");
                    return;
                case JsonKind.Bool:
                    text.Append(node.AsBool() ? "true" : "false");
                    return;
                case JsonKind.Number:
                    text.Append(node.RawNumber);
                    return;
                case JsonKind.String:
                    WriteString(text, node.AsString());
                    return;
                case JsonKind.Array:
                    text.Append('[');
                    for (var i = 0; i < node.Items.Count; i++)
                    {
                        if (i > 0)
                        {
                            text.Append(',');
                        }

                        Write(text, node.Items[i]);
                    }

                    text.Append(']');
                    return;
                default:
                    text.Append('{');
                    for (var i = 0; i < node.Properties.Count; i++)
                    {
                        if (i > 0)
                        {
                            text.Append(',');
                        }

                        WriteString(text, node.Properties[i].Key);
                        text.Append(':');
                        Write(text, node.Properties[i].Value);
                    }

                    text.Append('}');
                    return;
            }
        }

        public static void WriteString(StringBuilder text, string value)
        {
            text.Append('"');
            foreach (var c in value)
            {
                switch (c)
                {
                    case '"':
                        text.Append("\\\"");
                        break;
                    case '\\':
                        text.Append("\\\\");
                        break;
                    case '\n':
                        text.Append("\\n");
                        break;
                    case '\r':
                        text.Append("\\r");
                        break;
                    case '\t':
                        text.Append("\\t");
                        break;
                    case '\b':
                        text.Append("\\b");
                        break;
                    case '\f':
                        text.Append("\\f");
                        break;
                    default:
                        if (c < 0x20)
                        {
                            text.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            text.Append(c);
                        }

                        break;
                }
            }

            text.Append('"');
        }
    }
}
