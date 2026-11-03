using System;
using System.Collections.Generic;
using RowMate.Core.Ports;

namespace RowMate.Core.Common
{
    // Preferencias en memoria (02-tech-design §1.1 y §2.7 de la It. 5): la composición sin archivo (dotnet, las pruebas, el
    // arnés: una por toma, sembrada con lo que la toma pide). Ya leídas desde el principio (Loaded no se dispara nunca). Cada
    // clave guarda su valor con su tipo: una clave de otro tipo da false en TryGet*, como en JsonFilePreferences. Nunca toca
    // un archivo (AC-HIS-20-04).
    public sealed class MemoryPreferences : IPreferences
    {
        private readonly Dictionary<string, object> _values = new Dictionary<string, object>(StringComparer.Ordinal);

        public MemoryPreferences()
        {
        }

        // Sembrada: cada par clave → bool o string.
        public MemoryPreferences(IEnumerable<KeyValuePair<string, object>> seed)
        {
            if (seed == null)
            {
                throw new ArgumentNullException(nameof(seed));
            }

            foreach (var entry in seed)
            {
                if (entry.Key == null || !(entry.Value is bool || entry.Value is string))
                {
                    throw new ArgumentException("MemoryPreferences: solo claves con bool o string.", nameof(seed));
                }

                _values[entry.Key] = entry.Value;
            }
        }

        public bool IsLoaded => true;

        // Nunca se dispara: ya está leída.
        public event Action? Loaded
        {
            add { }
            remove { }
        }

        // Las escrituras hechas (Set* y Remove), para las pruebas.
        public int Writes { get; private set; }

        public bool TryGetBool(string key, out bool value)
        {
            if (key != null && _values.TryGetValue(key, out var stored) && stored is bool b)
            {
                value = b;
                return true;
            }

            value = false;
            return false;
        }

        public bool TryGetString(string key, out string value)
        {
            if (key != null && _values.TryGetValue(key, out var stored) && stored is string s)
            {
                value = s;
                return true;
            }

            value = string.Empty;
            return false;
        }

        public void SetBool(string key, bool value)
        {
            Put(key, value);
        }

        public void SetString(string key, string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            Put(key, value);
        }

        public void Remove(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            _values.Remove(key);
            Writes++;
        }

        private void Put(string key, object value)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            _values[key] = value;
            Writes++;
        }
    }
}
