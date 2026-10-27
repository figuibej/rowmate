using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace RowMate.Core.Design
{
    // Enumeración de los tokens de Tokens.cs por reflexión. SOLO para herramientas (tools/tokens), tests y Editor
    // (R-T10: bajo managed stripping la lista podría quedar incompleta en un player). Los consumidores usan los
    // campos (Tokens.StepWork), que verifica el compilador.
    public static class TokenCatalog
    {
        private static readonly object Gate = new object();
        private static IReadOnlyList<IDesignToken>? _all;
        private static Dictionary<string, IDesignToken>? _byName;

        // Campos public static readonly de Tokens, en orden ordinal por nombre de token.
        public static IReadOnlyList<IDesignToken> All
        {
            get
            {
                EnsureLoaded();
                return _all!;
            }
        }

        // KeyNotFoundException si no existe.
        public static IDesignToken Get(string name)
        {
            EnsureLoaded();
            if (name != null && _byName!.TryGetValue(name, out var token))
            {
                return token;
            }

            throw new KeyNotFoundException("TokenCatalog: no existe el token \"" + name + "\".");
        }

        public static ColorToken Color(string name)
        {
            var token = Get(name);
            if (token is ColorToken color)
            {
                return color;
            }

            throw new InvalidCastException("TokenCatalog: \"" + name + "\" no es un color (" + token.Kind + ").");
        }

        // PascalCase por segmento: "metric.split.hud" → "MetricSplitHud", "text.onAccent" → "TextOnAccent",
        // "r.glass" → "RGlass".
        public static string FieldNameFor(string tokenName)
        {
            if (string.IsNullOrEmpty(tokenName))
            {
                throw new ArgumentException("Nombre de token vacío.", nameof(tokenName));
            }

            var sb = new StringBuilder(tokenName.Length);
            var upperNext = true;
            for (var i = 0; i < tokenName.Length; i++)
            {
                var c = tokenName[i];
                if (c == '.')
                {
                    upperNext = true;
                    continue;
                }

                sb.Append(upperNext ? char.ToUpperInvariant(c) : c);
                upperNext = false;
            }

            return sb.ToString();
        }

        // Campos de Tokens con su token (para las reglas de TokenCatalogTests: nombre de campo = FieldNameFor).
        public static IReadOnlyList<KeyValuePair<string, IDesignToken>> Fields()
        {
            var result = new List<KeyValuePair<string, IDesignToken>>();
            var fields = typeof(Tokens).GetFields(BindingFlags.Public | BindingFlags.Static);
            for (var i = 0; i < fields.Length; i++)
            {
                var field = fields[i];
                if (!typeof(IDesignToken).IsAssignableFrom(field.FieldType))
                {
                    continue;
                }

                if (!field.IsInitOnly)
                {
                    throw new InvalidOperationException("Tokens." + field.Name + " tiene que ser static readonly.");
                }

                var token = field.GetValue(null) as IDesignToken;
                if (token == null)
                {
                    throw new InvalidOperationException("Tokens." + field.Name + " es null.");
                }

                result.Add(new KeyValuePair<string, IDesignToken>(field.Name, token));
            }

            return result;
        }

        private static void EnsureLoaded()
        {
            if (_all != null)
            {
                return;
            }

            lock (Gate)
            {
                if (_all != null)
                {
                    return;
                }

                var fields = Fields();
                var list = new List<IDesignToken>(fields.Count);
                var byName = new Dictionary<string, IDesignToken>(StringComparer.Ordinal);
                for (var i = 0; i < fields.Count; i++)
                {
                    var token = fields[i].Value;
                    if (byName.ContainsKey(token.Name))
                    {
                        throw new InvalidOperationException("Tokens: el nombre \"" + token.Name + "\" está repetido.");
                    }

                    byName.Add(token.Name, token);
                    list.Add(token);
                }

                list.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
                _byName = byName;
                _all = list.ToArray();
            }
        }
    }
}
