using System;
using System.Collections.Generic;
using System.Text;

namespace RowMate.Tests.Support
{
    public enum YamlKind
    {
        Null,
        Scalar,
        Map,
        List,
    }

    // Un nodo de MiniYaml: mapa (claves en orden), lista, escalar (texto) o nulo. Line = la línea (desde 1) donde empieza.
    public sealed class YamlNode
    {
        private static readonly IReadOnlyList<KeyValuePair<string, YamlNode>> NoEntries = new KeyValuePair<string, YamlNode>[0];
        private static readonly IReadOnlyList<YamlNode> NoItems = new YamlNode[0];

        private readonly List<KeyValuePair<string, YamlNode>>? _entries;
        private readonly List<YamlNode>? _items;

        private YamlNode(YamlKind kind, int line, string? value)
        {
            Kind = kind;
            Line = line;
            Value = value;
            if (kind == YamlKind.Map)
            {
                _entries = new List<KeyValuePair<string, YamlNode>>();
            }
            else if (kind == YamlKind.List)
            {
                _items = new List<YamlNode>();
            }
        }

        public YamlKind Kind { get; }

        public int Line { get; }

        // El texto del escalar (sin comillas); null en los demás.
        public string? Value { get; }

        public IReadOnlyList<KeyValuePair<string, YamlNode>> Entries => _entries ?? NoEntries;

        public IReadOnlyList<YamlNode> Items => _items ?? NoItems;

        public bool IsMap => Kind == YamlKind.Map;

        public bool IsList => Kind == YamlKind.List;

        // El valor de una clave de un mapa (null si no está o no es un mapa).
        public YamlNode? this[string key]
        {
            get
            {
                if (_entries == null)
                {
                    return null;
                }

                foreach (var entry in _entries)
                {
                    if (entry.Key == key)
                    {
                        return entry.Value;
                    }
                }

                return null;
            }
        }

        public bool Has(string key)
        {
            return this[key] != null;
        }

        // Una ruta de claves ("jobs", "gate", "timeout-minutes"); null si falta alguna.
        public YamlNode? At(params string[] path)
        {
            YamlNode? node = this;
            foreach (var key in path)
            {
                node = node?[key];
            }

            return node;
        }

        // Todos los escalares de este nodo y sus hijos, con la ruta (para buscar "uses:", "secrets." y rutas).
        public IEnumerable<KeyValuePair<string, YamlNode>> Scalars(string path = "")
        {
            switch (Kind)
            {
                case YamlKind.Scalar:
                    yield return new KeyValuePair<string, YamlNode>(path, this);
                    break;
                case YamlKind.Map:
                    foreach (var entry in Entries)
                    {
                        foreach (var inner in entry.Value.Scalars(path.Length == 0 ? entry.Key : path + "." + entry.Key))
                        {
                            yield return inner;
                        }
                    }

                    break;
                case YamlKind.List:
                    for (var i = 0; i < Items.Count; i++)
                    {
                        foreach (var inner in Items[i].Scalars(path + "[" + i + "]"))
                        {
                            yield return inner;
                        }
                    }

                    break;
            }
        }

        public override string ToString()
        {
            switch (Kind)
            {
                case YamlKind.Scalar:
                    return Value ?? "";
                case YamlKind.Map:
                    return "{" + Entries.Count + " claves}";
                case YamlKind.List:
                    return "[" + Items.Count + " elementos]";
                default:
                    return "null";
            }
        }

        internal static YamlNode NewMap(int line)
        {
            return new YamlNode(YamlKind.Map, line, null);
        }

        internal static YamlNode NewList(int line)
        {
            return new YamlNode(YamlKind.List, line, null);
        }

        internal static YamlNode NewScalar(int line, string value)
        {
            return new YamlNode(YamlKind.Scalar, line, value);
        }

        internal static YamlNode NewNull(int line)
        {
            return new YamlNode(YamlKind.Null, line, null);
        }

        internal void Add(string key, YamlNode value, int line)
        {
            if (this[key] != null || ContainsNullKey(key))
            {
                throw new MiniYamlException(line, "clave repetida: " + key);
            }

            _entries!.Add(new KeyValuePair<string, YamlNode>(key, value));
        }

        internal void Add(YamlNode item)
        {
            _items!.Add(item);
        }

        private bool ContainsNullKey(string key)
        {
            foreach (var entry in _entries!)
            {
                if (entry.Key == key)
                {
                    return true;
                }
            }

            return false;
        }
    }

    public sealed class MiniYamlException : Exception
    {
        public MiniYamlException(int line, string message)
            : base("YAML, línea " + line + ": " + message)
        {
            LineNumber = line;
        }

        public int LineNumber { get; }
    }

    // Un parser propio del subconjunto de YAML de bloque que usan los workflows de GitHub Actions (It. 14, AC-APP-19-10; 02-tech-
    // design §8.3): mapas y listas de bloque, listas "- clave: valor", listas en flujo [a, b] y mapas en flujo {a: b} (anidados,
    // en una línea), escalares sin comillas, con comillas simples ('' escapa) o dobles (\" \\ \n \t), bloques | y > (con - o +),
    // comentarios y expresiones ${{ }}. Falla (MiniYamlException con la línea) con tabulaciones en la sangría, sangría
    // inconsistente, claves repetidas, comillas o corchetes sin cerrar y anclas, alias o etiquetas (&, *, !) que no se usan.
    // netstandard2.1 y sin dependencias: compila igual en dotnet y en Unity. No pretende ser YAML completo.
    public static class MiniYaml
    {
        public static YamlNode Parse(string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            var lines = Prepare(text);
            var position = 0;
            if (lines.Count == 0)
            {
                return YamlNode.NewNull(1);
            }

            if (lines[0].Indent != 0)
            {
                throw new MiniYamlException(lines[0].Number, "el documento no empieza en la columna 0");
            }

            var root = ParseBlock(lines, ref position, 0);
            if (position < lines.Count)
            {
                throw new MiniYamlException(lines[position].Number, "sangría inconsistente");
            }

            return root;
        }

        private sealed class Line
        {
            public Line(int number, int indent, string text, string raw)
            {
                Number = number;
                Indent = indent;
                Text = text;
                Raw = raw;
            }

            public int Number { get; }

            public int Indent { get; set; }

            // Sin sangría ni comentario, sin espacios al final.
            public string Text { get; set; }

            // La línea original (para los bloques | y >).
            public string Raw { get; }

            public bool Blank => Text.Length == 0;
        }

        // Las líneas con contenido (los bloques | y > recuperan las vacías y los # desde Raw).
        private static List<Line> Prepare(string text)
        {
            var result = new List<Line>();
            var raw = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            for (var i = 0; i < raw.Length; i++)
            {
                var line = raw[i];
                var indent = 0;
                while (indent < line.Length && (line[indent] == ' ' || line[indent] == '\t'))
                {
                    if (line[indent] == '\t')
                    {
                        throw new MiniYamlException(i + 1, "tabulación en la sangría");
                    }

                    indent++;
                }

                var content = StripComment(line.Substring(indent), i + 1).TrimEnd();
                if (content == "---" && result.Count == 0)
                {
                    continue;
                }

                result.Add(new Line(i + 1, indent, content, line));
            }

            return result;
        }

        private static string StripComment(string text, int number)
        {
            char quote = '\0';
            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];
                if (quote != '\0')
                {
                    if (quote == '"' && c == '\\')
                    {
                        i++;
                        continue;
                    }

                    if (c == quote)
                    {
                        quote = '\0';
                    }

                    continue;
                }

                if ((c == '"' || c == '\'') && (i == 0 || IsQuoteStart(text, i)))
                {
                    quote = c;
                    continue;
                }

                if (c == '#' && (i == 0 || text[i - 1] == ' '))
                {
                    return text.Substring(0, i);
                }
            }

            return text;
        }

        // Una comilla abre un escalar solo al principio de un valor (después de ": ", "- ", "[", "{" o ",").
        private static bool IsQuoteStart(string text, int i)
        {
            var j = i - 1;
            while (j >= 0 && text[j] == ' ')
            {
                j--;
            }

            return j < 0 || text[j] == ':' || text[j] == '-' || text[j] == '[' || text[j] == '{' || text[j] == ',';
        }

        private static void SkipBlank(List<Line> lines, ref int position)
        {
            while (position < lines.Count && lines[position].Blank)
            {
                position++;
            }
        }

        private static YamlNode ParseBlock(List<Line> lines, ref int position, int indent)
        {
            SkipBlank(lines, ref position);
            if (position >= lines.Count)
            {
                return YamlNode.NewNull(lines[lines.Count - 1].Number);
            }

            var line = lines[position];
            if (line.Indent != indent)
            {
                throw new MiniYamlException(line.Number, "sangría inconsistente");
            }

            return IsListItem(line.Text) ? ParseList(lines, ref position, indent) : ParseMap(lines, ref position, indent);
        }

        private static bool IsListItem(string text)
        {
            return text == "-" || text.StartsWith("- ", StringComparison.Ordinal);
        }

        private static YamlNode ParseList(List<Line> lines, ref int position, int indent)
        {
            var list = YamlNode.NewList(lines[position].Number);
            while (true)
            {
                SkipBlank(lines, ref position);
                if (position >= lines.Count || lines[position].Indent < indent)
                {
                    return list;
                }

                var line = lines[position];
                if (line.Indent > indent)
                {
                    throw new MiniYamlException(line.Number, "sangría inconsistente");
                }

                if (!IsListItem(line.Text))
                {
                    return list;
                }

                var rest = line.Text.Length > 1 ? line.Text.Substring(2) : "";
                var spaces = 0;
                while (spaces < rest.Length && rest[spaces] == ' ')
                {
                    spaces++;
                }

                rest = rest.Substring(spaces);
                if (rest.Length == 0)
                {
                    position++;
                    SkipBlank(lines, ref position);
                    if (position < lines.Count && lines[position].Indent > indent)
                    {
                        list.Add(ParseBlock(lines, ref position, lines[position].Indent));
                    }
                    else
                    {
                        list.Add(YamlNode.NewNull(line.Number));
                    }

                    continue;
                }

                if (FindKeySeparator(rest) >= 0 && rest[0] != '[' && rest[0] != '{' && rest[0] != '"' && rest[0] != '\'')
                {
                    // "- clave: valor": un mapa cuya primera clave está en esta línea, con la sangría del texto.
                    line.Indent = indent + 2 + spaces;
                    line.Text = rest;
                    list.Add(ParseMap(lines, ref position, line.Indent));
                    continue;
                }

                position++;
                list.Add(ParseInline(rest, line.Number));
            }
        }

        private static YamlNode ParseMap(List<Line> lines, ref int position, int indent)
        {
            var map = YamlNode.NewMap(lines[position].Number);
            while (true)
            {
                SkipBlank(lines, ref position);
                if (position >= lines.Count || lines[position].Indent < indent)
                {
                    return map;
                }

                var line = lines[position];
                if (line.Indent > indent)
                {
                    throw new MiniYamlException(line.Number, "sangría inconsistente");
                }

                if (IsListItem(line.Text))
                {
                    return map;
                }

                var separator = FindKeySeparator(line.Text);
                if (separator < 0)
                {
                    throw new MiniYamlException(line.Number, "se esperaba \"clave: valor\": " + line.Text);
                }

                var key = Unquote(line.Text.Substring(0, separator).Trim(), line.Number);
                var value = line.Text.Substring(separator + 1).Trim();
                position++;
                YamlNode node;
                if (value.Length == 0)
                {
                    SkipBlank(lines, ref position);
                    if (position < lines.Count && lines[position].Indent > indent)
                    {
                        node = ParseBlock(lines, ref position, lines[position].Indent);
                    }
                    else if (position < lines.Count && lines[position].Indent == indent && IsListItem(lines[position].Text))
                    {
                        node = ParseList(lines, ref position, indent);
                    }
                    else
                    {
                        node = YamlNode.NewNull(line.Number);
                    }
                }
                else if (value[0] == '|' || value[0] == '>')
                {
                    node = ParseBlockScalar(lines, ref position, indent, value, line.Number);
                }
                else
                {
                    node = ParseInline(value, line.Number);
                }

                map.Add(key, node, line.Number);
            }
        }

        // La posición del ':' que separa clave y valor (seguido de espacio o fin), fuera de comillas, corchetes y ${{ }}.
        private static int FindKeySeparator(string text)
        {
            char quote = '\0';
            var depth = 0;
            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];
                if (quote != '\0')
                {
                    if (quote == '"' && c == '\\')
                    {
                        i++;
                    }
                    else if (c == quote)
                    {
                        quote = '\0';
                    }

                    continue;
                }

                if ((c == '"' || c == '\'') && i == 0)
                {
                    quote = c;
                }
                else if (c == '{' || c == '[')
                {
                    depth++;
                }
                else if (c == '}' || c == ']')
                {
                    depth--;
                }
                else if (c == ':' && depth == 0 && (i + 1 == text.Length || text[i + 1] == ' '))
                {
                    return i;
                }
            }

            return -1;
        }

        private static YamlNode ParseBlockScalar(List<Line> lines, ref int position, int indent, string header, int number)
        {
            var folded = header[0] == '>';
            var chomp = header.Length > 1 ? header[1] : ' ';
            if (header.Length > 2 || (header.Length == 2 && chomp != '-' && chomp != '+'))
            {
                throw new MiniYamlException(number, "cabecera de bloque no admitida: " + header);
            }

            var body = new List<string>();
            var blockIndent = -1;
            while (position < lines.Count)
            {
                var line = lines[position];
                var rawBlank = line.Raw.Trim().Length == 0;
                if (!rawBlank && line.Indent <= indent)
                {
                    break;
                }

                if (!rawBlank)
                {
                    if (blockIndent < 0)
                    {
                        blockIndent = line.Indent;
                    }
                    else if (line.Indent < blockIndent)
                    {
                        throw new MiniYamlException(line.Number, "sangría inconsistente en el bloque");
                    }

                    body.Add(line.Raw.Substring(blockIndent).TrimEnd());
                }
                else
                {
                    body.Add("");
                }

                position++;
            }

            while (chomp != '+' && body.Count > 0 && body[body.Count - 1].Length == 0)
            {
                body.RemoveAt(body.Count - 1);
            }

            var builder = new StringBuilder();
            for (var i = 0; i < body.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(folded && body[i].Length > 0 && body[i - 1].Length > 0 ? " " : "\n");
                }

                builder.Append(body[i]);
            }

            if (chomp != '-' && body.Count > 0)
            {
                builder.Append('\n');
            }

            return YamlNode.NewScalar(number, builder.ToString());
        }

        private static YamlNode ParseInline(string text, int number)
        {
            if (text.Length > 0 && (text[0] == '&' || text[0] == '*' || text[0] == '!'))
            {
                throw new MiniYamlException(number, "anclas, alias y etiquetas no admitidos: " + text);
            }

            if (text.StartsWith("[", StringComparison.Ordinal) || text.StartsWith("{", StringComparison.Ordinal))
            {
                var position = 0;
                var node = ParseFlow(text, ref position, number);
                SkipSpaces(text, ref position);
                if (position != text.Length)
                {
                    throw new MiniYamlException(number, "texto después del cierre: " + text.Substring(position));
                }

                return node;
            }

            return YamlNode.NewScalar(number, Unquote(text, number));
        }

        private static YamlNode ParseFlow(string text, ref int position, int number)
        {
            SkipSpaces(text, ref position);
            if (position >= text.Length)
            {
                throw new MiniYamlException(number, "valor en flujo sin cerrar");
            }

            if (text[position] == '[')
            {
                var list = YamlNode.NewList(number);
                position++;
                SkipSpaces(text, ref position);
                if (position < text.Length && text[position] == ']')
                {
                    position++;
                    return list;
                }

                while (true)
                {
                    list.Add(ParseFlow(text, ref position, number));
                    SkipSpaces(text, ref position);
                    if (position >= text.Length)
                    {
                        throw new MiniYamlException(number, "lista en flujo sin cerrar");
                    }

                    if (text[position] == ']')
                    {
                        position++;
                        return list;
                    }

                    Expect(text, ref position, ',', number);
                }
            }

            if (text[position] == '{' && !(position > 0 && text[position - 1] == '$'))
            {
                var map = YamlNode.NewMap(number);
                position++;
                SkipSpaces(text, ref position);
                if (position < text.Length && text[position] == '}')
                {
                    position++;
                    return map;
                }

                while (true)
                {
                    SkipSpaces(text, ref position);
                    var key = FlowScalar(text, ref position, number, true);
                    SkipSpaces(text, ref position);
                    Expect(text, ref position, ':', number);
                    map.Add(key, ParseFlow(text, ref position, number), number);
                    SkipSpaces(text, ref position);
                    if (position >= text.Length)
                    {
                        throw new MiniYamlException(number, "mapa en flujo sin cerrar");
                    }

                    if (text[position] == '}')
                    {
                        position++;
                        return map;
                    }

                    Expect(text, ref position, ',', number);
                }
            }

            return YamlNode.NewScalar(number, FlowScalar(text, ref position, number, false));
        }

        // Un escalar dentro de un flujo: con comillas, o hasta ',', ']', '}' (o ':' + espacio si es clave), respetando ${{ }}.
        private static string FlowScalar(string text, ref int position, int number, bool isKey)
        {
            SkipSpaces(text, ref position);
            if (position < text.Length && (text[position] == '"' || text[position] == '\''))
            {
                var quote = text[position];
                var start = position;
                position++;
                while (position < text.Length)
                {
                    if (quote == '"' && text[position] == '\\')
                    {
                        position += 2;
                        continue;
                    }

                    if (text[position] == quote)
                    {
                        if (quote == '\'' && position + 1 < text.Length && text[position + 1] == '\'')
                        {
                            position += 2;
                            continue;
                        }

                        position++;
                        return Unquote(text.Substring(start, position - start), number);
                    }

                    position++;
                }

                throw new MiniYamlException(number, "comillas sin cerrar");
            }

            var begin = position;
            var expression = 0;
            while (position < text.Length)
            {
                var c = text[position];
                if (c == '$' && position + 2 < text.Length && text[position + 1] == '{' && text[position + 2] == '{')
                {
                    expression++;
                    position += 3;
                    continue;
                }

                if (expression > 0 && c == '}' && position + 1 < text.Length && text[position + 1] == '}')
                {
                    expression--;
                    position += 2;
                    continue;
                }

                if (expression == 0 && (c == ',' || c == ']' || c == '}' || (isKey && c == ':')))
                {
                    break;
                }

                position++;
            }

            return text.Substring(begin, position - begin).Trim();
        }

        private static void Expect(string text, ref int position, char c, int number)
        {
            SkipSpaces(text, ref position);
            if (position >= text.Length || text[position] != c)
            {
                throw new MiniYamlException(number, "se esperaba '" + c + "' en: " + text);
            }

            position++;
        }

        private static void SkipSpaces(string text, ref int position)
        {
            while (position < text.Length && text[position] == ' ')
            {
                position++;
            }
        }

        private static string Unquote(string text, int number)
        {
            if (text.Length == 0)
            {
                return text;
            }

            var first = text[0];
            if (first != '"' && first != '\'')
            {
                return text;
            }

            if (text.Length < 2 || text[text.Length - 1] != first)
            {
                throw new MiniYamlException(number, "comillas sin cerrar: " + text);
            }

            var inner = text.Substring(1, text.Length - 2);
            if (first == '\'')
            {
                return inner.Replace("''", "'");
            }

            var builder = new StringBuilder(inner.Length);
            for (var i = 0; i < inner.Length; i++)
            {
                var c = inner[i];
                if (c != '\\')
                {
                    if (c == '"')
                    {
                        throw new MiniYamlException(number, "comilla doble sin escapar: " + text);
                    }

                    builder.Append(c);
                    continue;
                }

                if (++i >= inner.Length)
                {
                    throw new MiniYamlException(number, "escape incompleto: " + text);
                }

                switch (inner[i])
                {
                    case 'n':
                        builder.Append('\n');
                        break;
                    case 't':
                        builder.Append('\t');
                        break;
                    case '"':
                        builder.Append('"');
                        break;
                    case '\\':
                        builder.Append('\\');
                        break;
                    default:
                        throw new MiniYamlException(number, "escape no admitido: \\" + inner[i]);
                }
            }

            return builder.ToString();
        }
    }
}
