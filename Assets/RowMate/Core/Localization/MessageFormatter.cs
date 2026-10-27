using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using RowMate.Core.Text;

namespace RowMate.Core.Localization
{
    // Formateador ICU mínimo de los mensajes del catálogo (F-APP-10; 02-tech-design §3.3, MessageFormatter):
    // - texto literal, sin escape con apóstrofos (como gen-l10n sin use-escaping: "won't" es literal);
    // - {nombre} (texto o entero) y {nombre, plural, =N{…} one{…} other{…}} con other obligatorio;
    // - '#' dentro de un caso de plural = el número de ese plural (fuera de un plural es literal);
    // - argumentos y plurales anidados dentro de los casos, y varios plurales en un mismo mensaje (D-13).
    // Selección del plural: =N exacto primero, one si el número es 1, si no other (one = exactamente 1 y other = todo lo
    // demás, incluido 0, AC-APP-10-06). No hay reglas CLDR: zero/two/few/many no se admiten (para es y en darían other).
    // Sintaxis inválida, '}' suelto, tipo de argumento o selector desconocido → FormatException (al CARGAR el catálogo).
    public static class MessageFormatter
    {
        private const int MaxDepth = 8;

        public static CompiledMessage Parse(string pattern)
        {
            if (pattern == null)
            {
                throw new ArgumentNullException(nameof(pattern));
            }

            var parser = new Parser(pattern);
            var parts = parser.ParseSequence(0, false, false);
            return new CompiledMessage(pattern, parts, parser.Names, parser.PluralNames);
        }

        private sealed class Parser
        {
            private readonly string _p;
            private readonly HashSet<string> _seen = new HashSet<string>(StringComparer.Ordinal);
            private int _pos;

            public Parser(string pattern)
            {
                _p = pattern;
            }

            public List<string> Names { get; } = new List<string>();

            public HashSet<string> PluralNames { get; } = new HashSet<string>(StringComparer.Ordinal);

            // Secuencia de texto y argumentos. nested = dentro de un caso de plural: termina en el '}' que lo cierra
            // (sin consumirlo).
            public MessagePart[] ParseSequence(int depth, bool inPlural, bool nested)
            {
                var parts = new List<MessagePart>();
                var text = new StringBuilder();
                while (_pos < _p.Length)
                {
                    var c = _p[_pos];
                    if (c == '{')
                    {
                        Flush(text, parts);
                        parts.Add(ParseArgument(depth + 1));
                        continue;
                    }

                    if (c == '}')
                    {
                        if (nested)
                        {
                            Flush(text, parts);
                            return parts.ToArray();
                        }

                        throw Error("'}' sin una '{' que lo abra");
                    }

                    if (c == '#' && inPlural)
                    {
                        Flush(text, parts);
                        parts.Add(PoundPart.Instance);
                        _pos++;
                        continue;
                    }

                    text.Append(c);
                    _pos++;
                }

                if (nested)
                {
                    throw Error("falta la '}' que cierra un caso de plural");
                }

                Flush(text, parts);
                return parts.ToArray();
            }

            private MessagePart ParseArgument(int depth)
            {
                if (depth > MaxDepth)
                {
                    throw Error("demasiados argumentos anidados");
                }

                _pos++; // '{'
                SkipWhitespace();
                var name = ReadIdentifier("el nombre de un argumento");
                SkipWhitespace();
                if (Peek() == '}')
                {
                    _pos++;
                    Register(name);
                    return new ArgumentPart(name);
                }

                Expect(',');
                SkipWhitespace();
                var type = ReadIdentifier("el tipo del argumento \"" + name + "\"");
                if (type != "plural")
                {
                    throw Error("tipo de argumento no admitido \"" + type + "\" (solo {x} y {x, plural, …})");
                }

                SkipWhitespace();
                Expect(',');
                Register(name);
                PluralNames.Add(name);
                return ParsePluralCases(name, depth);
            }

            private PluralPart ParsePluralCases(string name, int depth)
            {
                var exact = new List<KeyValuePair<long, MessagePart[]>>();
                var selectors = new HashSet<string>(StringComparer.Ordinal);
                MessagePart[]? one = null;
                MessagePart[]? other = null;
                while (true)
                {
                    SkipWhitespace();
                    if (_pos >= _p.Length)
                    {
                        throw Error("falta la '}' que cierra el plural \"" + name + "\"");
                    }

                    if (Peek() == '}')
                    {
                        _pos++;
                        break;
                    }

                    string selector;
                    long exactValue = 0;
                    var isExact = false;
                    if (Peek() == '=')
                    {
                        _pos++;
                        var start = _pos;
                        if (Peek() == '-')
                        {
                            _pos++;
                        }

                        while (_pos < _p.Length && _p[_pos] >= '0' && _p[_pos] <= '9')
                        {
                            _pos++;
                        }

                        var digits = _p.Substring(start, _pos - start);
                        if (!long.TryParse(digits, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out exactValue))
                        {
                            throw Error("selector exacto inválido \"=" + digits + "\"");
                        }

                        isExact = true;
                        selector = "=" + Fmt.Integer(exactValue);
                    }
                    else
                    {
                        selector = ReadIdentifier("un selector de plural");
                        if (selector != "one" && selector != "other")
                        {
                            throw Error("selector de plural no admitido \"" + selector + "\" (solo =N, one y other)");
                        }
                    }

                    if (!selectors.Add(selector))
                    {
                        throw Error("selector de plural repetido \"" + selector + "\"");
                    }

                    SkipWhitespace();
                    Expect('{');
                    var body = ParseSequence(depth, true, true);
                    Expect('}');
                    if (isExact)
                    {
                        exact.Add(new KeyValuePair<long, MessagePart[]>(exactValue, body));
                    }
                    else if (selector == "one")
                    {
                        one = body;
                    }
                    else
                    {
                        other = body;
                    }
                }

                if (other == null)
                {
                    throw Error("el plural \"" + name + "\" no tiene el caso other");
                }

                return new PluralPart(name, exact.ToArray(), one, other);
            }

            private void Register(string name)
            {
                if (_seen.Add(name))
                {
                    Names.Add(name);
                }
            }

            private string ReadIdentifier(string what)
            {
                var start = _pos;
                while (_pos < _p.Length)
                {
                    var c = _p[_pos];
                    var ok = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || c == '_' || (_pos > start && c >= '0' && c <= '9');
                    if (!ok)
                    {
                        break;
                    }

                    _pos++;
                }

                if (_pos == start)
                {
                    throw Error("se esperaba " + what);
                }

                return _p.Substring(start, _pos - start);
            }

            private void SkipWhitespace()
            {
                while (_pos < _p.Length && char.IsWhiteSpace(_p[_pos]))
                {
                    _pos++;
                }
            }

            private char Peek()
            {
                return _pos < _p.Length ? _p[_pos] : '\0';
            }

            private void Expect(char c)
            {
                if (Peek() != c)
                {
                    throw Error("se esperaba '" + c + "'");
                }

                _pos++;
            }

            private static void Flush(StringBuilder text, List<MessagePart> parts)
            {
                if (text.Length > 0)
                {
                    parts.Add(new LiteralPart(text.ToString()));
                    text.Clear();
                }
            }

            private FormatException Error(string message)
            {
                return new FormatException("Mensaje inválido (posición " + Fmt.Integer(_pos) + "): "
                    + message + ". Mensaje: \"" + _p + "\"");
            }
        }
    }
}
