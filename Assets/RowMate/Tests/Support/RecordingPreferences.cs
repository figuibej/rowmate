using System;
using System.Collections.Generic;
using RowMate.Core.Ports;

namespace RowMate.Tests.Support
{
    // Preferencias de prueba (05-series-parciales/02-tech-design §6.2): en memoria, anotan cada escritura, pueden fallar al
    // guardar (FailWrites: el valor en memoria cambia igual, como el caché del legacy; solo no "llega al disco") y pueden
    // demorar la lectura (deferLoad: arranca sin leer, TryGet* da false, y CompleteLoad() trae lo sembrado y dispara Loaded: el
    // toque antes de la lectura de series_panels.dart:130-149). Nunca tocan un archivo (AC-HIS-20-04).
    public sealed class RecordingPreferences : IPreferences
    {
        private readonly Dictionary<string, object> _values = new Dictionary<string, object>(StringComparer.Ordinal);
        private readonly Dictionary<string, object> _disk = new Dictionary<string, object>(StringComparer.Ordinal);
        private readonly List<string> _writes = new List<string>();
        private Dictionary<string, object>? _deferred;

        public RecordingPreferences(bool deferLoad = false, params KeyValuePair<string, object>[] seed)
        {
            var initial = new Dictionary<string, object>(StringComparer.Ordinal);
            foreach (var entry in seed)
            {
                initial[entry.Key] = entry.Value;
                _disk[entry.Key] = entry.Value;
            }

            if (deferLoad)
            {
                _deferred = initial;
            }
            else
            {
                foreach (var entry in initial)
                {
                    _values[entry.Key] = entry.Value;
                }
            }
        }

        public bool IsLoaded => _deferred == null;

        public event Action? Loaded;

        // Que guardar falle desde ahora (el valor en memoria cambia igual).
        public bool FailWrites { get; set; }

        // Cada escritura pedida, en orden: "set key=value", "remove key"; con "!" al final si el guardado falló.
        public IReadOnlyList<string> Writes => _writes.ToArray();

        public int WriteCount => _writes.Count;

        // Lo que "llegó al disco" (lo que leería la app al reiniciar).
        public IReadOnlyDictionary<string, object> Disk => _disk;

        public static KeyValuePair<string, object> Seed(string key, object value)
        {
            return new KeyValuePair<string, object>(key, value);
        }

        // Termina la lectura demorada: lo sembrado (sin pisar lo que ya se escribió) y Loaded.
        public void CompleteLoad()
        {
            var deferred = _deferred;
            if (deferred == null)
            {
                return;
            }

            _deferred = null;
            foreach (var entry in deferred)
            {
                if (!_values.ContainsKey(entry.Key))
                {
                    _values[entry.Key] = entry.Value;
                }
            }

            Loaded?.Invoke();
        }

        public bool TryGetBool(string key, out bool value)
        {
            if (IsLoaded && _values.TryGetValue(key, out var stored) && stored is bool b)
            {
                value = b;
                return true;
            }

            value = false;
            return false;
        }

        public bool TryGetString(string key, out string value)
        {
            if (IsLoaded && _values.TryGetValue(key, out var stored) && stored is string s)
            {
                value = s;
                return true;
            }

            value = string.Empty;
            return false;
        }

        public void SetBool(string key, bool value)
        {
            Write(key, value, "set " + key + "=" + (value ? "true" : "false"));
        }

        public void SetString(string key, string value)
        {
            Write(key, value, "set " + key + "=" + value);
        }

        public void Remove(string key)
        {
            _values.Remove(key);
            if (!FailWrites)
            {
                _disk.Remove(key);
            }

            _writes.Add("remove " + key + (FailWrites ? "!" : ""));
        }

        private void Write(string key, object value, string line)
        {
            _values[key] = value;
            if (!FailWrites)
            {
                _disk[key] = value;
            }

            _writes.Add(line + (FailWrites ? "!" : ""));
        }
    }
}
