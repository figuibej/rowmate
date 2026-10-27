using System;
using System.Collections.Generic;
using System.Globalization;

namespace RowMate.Core.Common.Json
{
    // Valor JSON inmutable que produce JsonParser. Un solo lector para el catálogo de textos, los vectores
    // dorados y el lint del proyecto (D-08): Core no referencia Newtonsoft ni System.Text.Json.
    // Los números guardan su texto exacto (RawNumber): un entero de 64 bits nunca pasa por double.
    public sealed class JsonNode
    {
        private static readonly JsonNode[] NoItems = new JsonNode[0];
        private static readonly KeyValuePair<string, JsonNode>[] NoProperties = new KeyValuePair<string, JsonNode>[0];

        private readonly string? _text;
        private readonly bool _bool;
        private readonly IReadOnlyList<JsonNode> _items;
        private readonly IReadOnlyList<KeyValuePair<string, JsonNode>> _properties;
        private readonly Dictionary<string, JsonNode>? _index;

        private JsonNode(JsonKind kind, string? text, bool boolean, IReadOnlyList<JsonNode> items,
            IReadOnlyList<KeyValuePair<string, JsonNode>> properties, Dictionary<string, JsonNode>? index)
        {
            Kind = kind;
            _text = text;
            _bool = boolean;
            _items = items;
            _properties = properties;
            _index = index;
        }

        public JsonKind Kind { get; }

        // Texto exacto del número tal como apareció en el documento.
        public string RawNumber
        {
            get
            {
                Expect(JsonKind.Number);
                return _text!;
            }
        }

        public IReadOnlyList<JsonNode> Items
        {
            get
            {
                Expect(JsonKind.Array);
                return _items;
            }
        }

        // Propiedades en el orden en que aparecen en el documento.
        public IReadOnlyList<KeyValuePair<string, JsonNode>> Properties
        {
            get
            {
                Expect(JsonKind.Object);
                return _properties;
            }
        }

        public bool IsNull => Kind == JsonKind.Null;

        // KeyNotFoundException si la clave no existe.
        public JsonNode this[string key]
        {
            get
            {
                Expect(JsonKind.Object);
                if (key == null)
                {
                    throw new ArgumentNullException(nameof(key));
                }

                if (_index!.TryGetValue(key, out var value))
                {
                    return value;
                }

                throw new KeyNotFoundException("JSON: falta la clave \"" + key + "\".");
            }
        }

        public bool TryGet(string key, out JsonNode value)
        {
            Expect(JsonKind.Object);
            if (key != null && _index!.TryGetValue(key, out var found))
            {
                value = found;
                return true;
            }

            value = null!;
            return false;
        }

        public string AsString()
        {
            Expect(JsonKind.String);
            return _text!;
        }

        public bool AsBool()
        {
            Expect(JsonKind.Bool);
            return _bool;
        }

        // Solo enteros escritos como tales ("12", "-3"): "1.0" o "1e3" son un error.
        public long AsLong()
        {
            Expect(JsonKind.Number);
            if (long.TryParse(_text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var value))
            {
                return value;
            }

            throw new FormatException("JSON: el número " + _text + " no es un entero de 64 bits.");
        }

        // Cultura invariante. Los double de los vectores dorados viajan por sus bits, no por este método.
        public double AsDouble()
        {
            Expect(JsonKind.Number);
            return double.Parse(_text!, NumberStyles.Float, CultureInfo.InvariantCulture);
        }

        public override string ToString()
        {
            switch (Kind)
            {
                case JsonKind.Null:
                    return "null";
                case JsonKind.Bool:
                    return _bool ? "true" : "false";
                case JsonKind.Number:
                    return _text!;
                case JsonKind.String:
                    return "\"" + _text + "\"";
                case JsonKind.Array:
                    return "[" + _items.Count + " elementos]";
                default:
                    return "{" + _properties.Count + " propiedades}";
            }
        }

        internal static JsonNode Null()
        {
            return new JsonNode(JsonKind.Null, null, false, NoItems, NoProperties, null);
        }

        internal static JsonNode Bool(bool value)
        {
            return new JsonNode(JsonKind.Bool, null, value, NoItems, NoProperties, null);
        }

        internal static JsonNode Number(string raw)
        {
            return new JsonNode(JsonKind.Number, raw, false, NoItems, NoProperties, null);
        }

        internal static JsonNode String(string value)
        {
            return new JsonNode(JsonKind.String, value, false, NoItems, NoProperties, null);
        }

        internal static JsonNode Array(List<JsonNode> items)
        {
            return new JsonNode(JsonKind.Array, null, false, items.ToArray(), NoProperties, null);
        }

        internal static JsonNode Object(List<KeyValuePair<string, JsonNode>> properties, Dictionary<string, JsonNode> index)
        {
            return new JsonNode(JsonKind.Object, null, false, NoItems, properties.ToArray(), index);
        }

        private void Expect(JsonKind kind)
        {
            if (Kind != kind)
            {
                throw new InvalidOperationException("JSON: se esperaba " + kind + " y el valor es " + Kind + ".");
            }
        }
    }
}
