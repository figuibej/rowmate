using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using RowMate.Core.Common.Json;

namespace RowMate.Core.Localization
{
    // Catálogo de textos de un idioma (F-APP-10, ADR 011): Assets/RowMate/Localization/strings.<lang>.json, con el mismo
    // formato que los .arb del legacy (lib/l10n/app_{es,en}.arb):
    //   "@@locale": "es",
    //   "clave": "texto con {parametro}",
    //   "@clave": { "placeholders": { "parametro": { "type": "String" | "int" } } }
    // Parse valida todo al cargar: JSON estricto (claves repetidas = error), cada mensaje con MessageFormatter, que los
    // argumentos de cada mensaje sean EXACTAMENTE los de sus metadatos (ni sin metadato ni metadato sin uso), que los
    // tipos sean "String" o "int" y que todo argumento de plural sea "int". Cualquier problema → FormatException.
    public sealed class LocalizationCatalog
    {
        public const string LocaleKey = "@@locale";

        private static readonly Regex KeyPattern = new Regex(@"^[A-Za-z_][A-Za-z0-9_]*\z", RegexOptions.CultureInvariant);
        private static readonly PlaceholderInfo[] NoPlaceholders = new PlaceholderInfo[0];

        private readonly Dictionary<string, Entry> _entries;

        private LocalizationCatalog(string locale, List<string> keys, Dictionary<string, Entry> entries)
        {
            Locale = locale;
            Keys = new ReadOnlyCollection<string>(keys);
            _entries = entries;
        }

        // Valor de "@@locale" ("es", "en").
        public string Locale { get; }

        // Claves de mensajes en el orden del archivo (sin "@@locale" ni los metadatos "@clave").
        public IReadOnlyList<string> Keys { get; }

        public static LocalizationCatalog Parse(string json)
        {
            if (json == null)
            {
                throw new ArgumentNullException(nameof(json));
            }

            var root = JsonParser.Parse(json);
            if (root.Kind != JsonKind.Object)
            {
                throw new FormatException("Catálogo: la raíz tiene que ser un objeto JSON.");
            }

            if (!root.TryGet(LocaleKey, out var localeNode) || localeNode.Kind != JsonKind.String || localeNode.AsString().Length == 0)
            {
                throw new FormatException("Catálogo: falta \"" + LocaleKey + "\".");
            }

            var locale = localeNode.AsString();
            var keys = new List<string>();
            var messages = new Dictionary<string, string>(StringComparer.Ordinal);
            var metadata = new Dictionary<string, JsonNode>(StringComparer.Ordinal);
            foreach (var property in root.Properties)
            {
                var key = property.Key;
                if (key.StartsWith("@@", StringComparison.Ordinal))
                {
                    continue; // atributos globales del .arb (@@locale y los que no usa la app)
                }

                if (key.StartsWith("@", StringComparison.Ordinal))
                {
                    if (property.Value.Kind != JsonKind.Object)
                    {
                        throw Error(locale, key, "los metadatos tienen que ser un objeto");
                    }

                    metadata[key.Substring(1)] = property.Value;
                    continue;
                }

                if (!KeyPattern.IsMatch(key))
                {
                    throw Error(locale, key, "la clave no es un identificador válido");
                }

                if (property.Value.Kind != JsonKind.String)
                {
                    throw Error(locale, key, "el mensaje tiene que ser un texto");
                }

                keys.Add(key);
                messages[key] = property.Value.AsString();
            }

            foreach (var key in metadata.Keys)
            {
                if (!messages.ContainsKey(key))
                {
                    throw Error(locale, "@" + key, "metadatos de una clave que no existe");
                }
            }

            var entries = new Dictionary<string, Entry>(StringComparer.Ordinal);
            foreach (var key in keys)
            {
                CompiledMessage compiled;
                try
                {
                    compiled = MessageFormatter.Parse(messages[key]);
                }
                catch (FormatException e)
                {
                    throw Error(locale, key, e.Message);
                }

                var placeholders = metadata.TryGetValue(key, out var meta) ? ReadPlaceholders(locale, key, meta) : NoPlaceholders;
                Validate(locale, key, compiled, placeholders);
                entries[key] = new Entry(compiled, placeholders);
            }

            return new LocalizationCatalog(locale, keys, entries);
        }

        public bool Contains(string key)
        {
            return key != null && _entries.ContainsKey(key);
        }

        // Texto crudo del mensaje (con sus {parámetros}). MissingTranslationException si la clave no existe.
        public string Message(string key)
        {
            return Find(key).Compiled.Pattern;
        }

        // Parámetros declarados, en el orden de los metadatos (vacío si la clave no tiene).
        public IReadOnlyList<PlaceholderInfo> Placeholders(string key)
        {
            return Find(key).Placeholders;
        }

        internal CompiledMessage Compiled(string key)
        {
            return Find(key).Compiled;
        }

        private Entry Find(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (_entries.TryGetValue(key, out var entry))
            {
                return entry;
            }

            throw new MissingTranslationException(key, Locale);
        }

        private static PlaceholderInfo[] ReadPlaceholders(string locale, string key, JsonNode meta)
        {
            if (!meta.TryGet("placeholders", out var node))
            {
                return NoPlaceholders;
            }

            if (node.Kind != JsonKind.Object)
            {
                throw Error(locale, "@" + key, "\"placeholders\" tiene que ser un objeto");
            }

            var result = new List<PlaceholderInfo>();
            foreach (var property in node.Properties)
            {
                if (property.Value.Kind != JsonKind.Object || !property.Value.TryGet("type", out var typeNode) || typeNode.Kind != JsonKind.String)
                {
                    throw Error(locale, "@" + key, "el parámetro \"" + property.Key + "\" no tiene \"type\"");
                }

                var type = typeNode.AsString();
                if (type != PlaceholderInfo.StringType && type != PlaceholderInfo.IntType)
                {
                    throw Error(locale, "@" + key, "tipo no admitido \"" + type + "\" en \"" + property.Key + "\" (solo String e int)");
                }

                result.Add(new PlaceholderInfo(property.Key, type));
            }

            return result.ToArray();
        }

        private static void Validate(string locale, string key, CompiledMessage compiled, PlaceholderInfo[] placeholders)
        {
            var declared = new HashSet<string>(StringComparer.Ordinal);
            foreach (var placeholder in placeholders)
            {
                declared.Add(placeholder.Name);
                if (compiled.IsPluralArgument(placeholder.Name) && placeholder.Type != PlaceholderInfo.IntType)
                {
                    throw Error(locale, key, "el plural \"" + placeholder.Name + "\" tiene que ser de tipo int");
                }
            }

            foreach (var name in compiled.ArgumentNames)
            {
                if (!declared.Contains(name))
                {
                    throw Error(locale, key, "el argumento \"" + name + "\" no está en los metadatos");
                }
            }

            foreach (var name in declared)
            {
                var used = false;
                foreach (var argument in compiled.ArgumentNames)
                {
                    if (argument == name)
                    {
                        used = true;
                    }
                }

                if (!used)
                {
                    throw Error(locale, key, "el parámetro \"" + name + "\" de los metadatos no aparece en el mensaje");
                }
            }
        }

        private static FormatException Error(string locale, string key, string message)
        {
            return new FormatException("Catálogo \"" + locale + "\", \"" + key + "\": " + message + ".");
        }

        private sealed class Entry
        {
            public Entry(CompiledMessage compiled, PlaceholderInfo[] placeholders)
            {
                Compiled = compiled;
                Placeholders = new ReadOnlyCollection<PlaceholderInfo>(placeholders);
            }

            public CompiledMessage Compiled { get; }

            public IReadOnlyList<PlaceholderInfo> Placeholders { get; }
        }
    }
}
