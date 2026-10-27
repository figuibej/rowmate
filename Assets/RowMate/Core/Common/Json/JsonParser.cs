using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace RowMate.Core.Common.Json
{
    // Lector JSON mínimo y estricto (RFC 8259). Errores = FormatException con línea y columna (base 1).
    // Rechaza: comas finales, comentarios, comillas simples, ceros a la izquierda, NaN/Infinity, caracteres
    // de control sin escapar, sustitutos UTF-16 sueltos, claves duplicadas y más de 64 niveles de anidamiento.
    // Ignora un BOM inicial (RFC 8259 §8.1 lo permite).
    public static class JsonParser
    {
        public const int MaxDepth = 64;

        public static JsonNode Parse(string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            var reader = new Reader(text);
            reader.SkipBom();
            reader.SkipWhitespace();
            var value = reader.ReadValue(0);
            reader.SkipWhitespace();
            if (!reader.AtEnd)
            {
                throw reader.Error("hay texto después del valor");
            }

            return value;
        }

        private sealed class Reader
        {
            private readonly string _text;
            private int _pos;

            public Reader(string text)
            {
                _text = text;
            }

            public bool AtEnd => _pos >= _text.Length;

            public void SkipBom()
            {
                if (_pos < _text.Length && _text[_pos] == '﻿')
                {
                    _pos++;
                }
            }

            public void SkipWhitespace()
            {
                while (_pos < _text.Length)
                {
                    var c = _text[_pos];
                    if (c == ' ' || c == '\t' || c == '\n' || c == '\r')
                    {
                        _pos++;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            public JsonNode ReadValue(int depth)
            {
                if (AtEnd)
                {
                    throw Error("se esperaba un valor y terminó el texto");
                }

                var c = _text[_pos];
                switch (c)
                {
                    case '{':
                        return ReadObject(depth + 1);
                    case '[':
                        return ReadArray(depth + 1);
                    case '"':
                        return JsonNode.String(ReadString());
                    case 't':
                        ReadLiteral("true");
                        return JsonNode.Bool(true);
                    case 'f':
                        ReadLiteral("false");
                        return JsonNode.Bool(false);
                    case 'n':
                        ReadLiteral("null");
                        return JsonNode.Null();
                    default:
                        if (c == '-' || (c >= '0' && c <= '9'))
                        {
                            return JsonNode.Number(ReadNumber());
                        }

                        throw Error("carácter inesperado '" + c + "'");
                }
            }

            public FormatException Error(string message)
            {
                var line = 1;
                var column = 1;
                var end = Math.Min(_pos, _text.Length);
                for (var i = 0; i < end; i++)
                {
                    if (_text[i] == '\n')
                    {
                        line++;
                        column = 1;
                    }
                    else
                    {
                        column++;
                    }
                }

                return new FormatException("JSON inválido (línea " + line.ToString(CultureInfo.InvariantCulture)
                    + ", columna " + column.ToString(CultureInfo.InvariantCulture) + "): " + message + ".");
            }

            private JsonNode ReadObject(int depth)
            {
                CheckDepth(depth);
                _pos++; // {
                var properties = new List<KeyValuePair<string, JsonNode>>();
                var index = new Dictionary<string, JsonNode>(StringComparer.Ordinal);
                SkipWhitespace();
                if (Peek() == '}')
                {
                    _pos++;
                    return JsonNode.Object(properties, index);
                }

                while (true)
                {
                    SkipWhitespace();
                    if (Peek() != '"')
                    {
                        throw Error("se esperaba una clave entre comillas");
                    }

                    var keyPos = _pos;
                    var key = ReadString();
                    if (index.ContainsKey(key))
                    {
                        _pos = keyPos;
                        throw Error("clave duplicada \"" + key + "\"");
                    }

                    SkipWhitespace();
                    if (Peek() != ':')
                    {
                        throw Error("se esperaba ':'");
                    }

                    _pos++;
                    SkipWhitespace();
                    var value = ReadValue(depth);
                    properties.Add(new KeyValuePair<string, JsonNode>(key, value));
                    index.Add(key, value);
                    SkipWhitespace();
                    var next = Peek();
                    if (next == ',')
                    {
                        _pos++;
                        continue;
                    }

                    if (next == '}')
                    {
                        _pos++;
                        return JsonNode.Object(properties, index);
                    }

                    throw Error("se esperaba ',' o '}'");
                }
            }

            private JsonNode ReadArray(int depth)
            {
                CheckDepth(depth);
                _pos++; // [
                var items = new List<JsonNode>();
                SkipWhitespace();
                if (Peek() == ']')
                {
                    _pos++;
                    return JsonNode.Array(items);
                }

                while (true)
                {
                    SkipWhitespace();
                    items.Add(ReadValue(depth));
                    SkipWhitespace();
                    var next = Peek();
                    if (next == ',')
                    {
                        _pos++;
                        continue;
                    }

                    if (next == ']')
                    {
                        _pos++;
                        return JsonNode.Array(items);
                    }

                    throw Error("se esperaba ',' o ']'");
                }
            }

            private string ReadString()
            {
                _pos++; // "
                StringBuilder? sb = null;
                var start = _pos;
                while (true)
                {
                    if (AtEnd)
                    {
                        throw Error("texto sin cerrar");
                    }

                    var c = _text[_pos];
                    if (c == '"')
                    {
                        string result;
                        if (sb == null)
                        {
                            result = _text.Substring(start, _pos - start);
                        }
                        else
                        {
                            sb.Append(_text, start, _pos - start);
                            result = sb.ToString();
                        }

                        _pos++;
                        return result;
                    }

                    if (c < ' ')
                    {
                        throw Error("carácter de control sin escapar en un texto");
                    }

                    if (char.IsSurrogate(c))
                    {
                        if (char.IsHighSurrogate(c) && _pos + 1 < _text.Length && char.IsLowSurrogate(_text[_pos + 1]))
                        {
                            _pos += 2;
                            continue;
                        }

                        throw Error("sustituto UTF-16 suelto");
                    }

                    if (c != '\\')
                    {
                        _pos++;
                        continue;
                    }

                    if (sb == null)
                    {
                        sb = new StringBuilder();
                    }

                    sb.Append(_text, start, _pos - start);
                    _pos++; // \
                    if (AtEnd)
                    {
                        throw Error("escape incompleto");
                    }

                    var e = _text[_pos];
                    switch (e)
                    {
                        case '"':
                            sb.Append('"');
                            break;
                        case '\\':
                            sb.Append('\\');
                            break;
                        case '/':
                            sb.Append('/');
                            break;
                        case 'b':
                            sb.Append('\b');
                            break;
                        case 'f':
                            sb.Append('\f');
                            break;
                        case 'n':
                            sb.Append('\n');
                            break;
                        case 'r':
                            sb.Append('\r');
                            break;
                        case 't':
                            sb.Append('\t');
                            break;
                        case 'u':
                            _pos++;
                            var unit = ReadHex4();
                            if (char.IsHighSurrogate(unit))
                            {
                                if (_pos + 1 < _text.Length && _text[_pos] == '\\' && _text[_pos + 1] == 'u')
                                {
                                    _pos += 2;
                                    var low = ReadHex4();
                                    if (!char.IsLowSurrogate(low))
                                    {
                                        throw Error("par sustituto inválido en \\u");
                                    }

                                    sb.Append(unit);
                                    sb.Append(low);
                                }
                                else
                                {
                                    throw Error("sustituto alto sin pareja en \\u");
                                }
                            }
                            else if (char.IsLowSurrogate(unit))
                            {
                                throw Error("sustituto bajo suelto en \\u");
                            }
                            else
                            {
                                sb.Append(unit);
                            }

                            start = _pos;
                            continue;
                        default:
                            throw Error("escape desconocido '\\" + e + "'");
                    }

                    _pos++;
                    start = _pos;
                }
            }

            private char ReadHex4()
            {
                if (_pos + 4 > _text.Length)
                {
                    throw Error("escape \\u incompleto");
                }

                var value = 0;
                for (var i = 0; i < 4; i++)
                {
                    var c = _text[_pos + i];
                    int digit;
                    if (c >= '0' && c <= '9')
                    {
                        digit = c - '0';
                    }
                    else if (c >= 'a' && c <= 'f')
                    {
                        digit = c - 'a' + 10;
                    }
                    else if (c >= 'A' && c <= 'F')
                    {
                        digit = c - 'A' + 10;
                    }
                    else
                    {
                        throw Error("dígito hexadecimal inválido en \\u");
                    }

                    value = (value << 4) | digit;
                }

                _pos += 4;
                return (char)value;
            }

            private string ReadNumber()
            {
                var start = _pos;
                if (Peek() == '-')
                {
                    _pos++;
                }

                if (AtEnd)
                {
                    throw Error("número incompleto");
                }

                var c = _text[_pos];
                if (c == '0')
                {
                    _pos++;
                    if (!AtEnd && IsDigit(_text[_pos]))
                    {
                        throw Error("número con ceros a la izquierda");
                    }
                }
                else if (c >= '1' && c <= '9')
                {
                    ReadDigits();
                }
                else
                {
                    throw Error("número inválido");
                }

                if (!AtEnd && _text[_pos] == '.')
                {
                    _pos++;
                    if (AtEnd || !IsDigit(_text[_pos]))
                    {
                        throw Error("faltan dígitos después del punto decimal");
                    }

                    ReadDigits();
                }

                if (!AtEnd && (_text[_pos] == 'e' || _text[_pos] == 'E'))
                {
                    _pos++;
                    if (!AtEnd && (_text[_pos] == '+' || _text[_pos] == '-'))
                    {
                        _pos++;
                    }

                    if (AtEnd || !IsDigit(_text[_pos]))
                    {
                        throw Error("faltan dígitos en el exponente");
                    }

                    ReadDigits();
                }

                return _text.Substring(start, _pos - start);
            }

            private void ReadDigits()
            {
                while (!AtEnd && IsDigit(_text[_pos]))
                {
                    _pos++;
                }
            }

            private void ReadLiteral(string literal)
            {
                if (string.CompareOrdinal(_text, _pos, literal, 0, literal.Length) != 0)
                {
                    throw Error("valor desconocido");
                }

                _pos += literal.Length;
            }

            private char Peek()
            {
                return AtEnd ? '\0' : _text[_pos];
            }

            private void CheckDepth(int depth)
            {
                if (depth > MaxDepth)
                {
                    throw Error("más de " + MaxDepth.ToString(CultureInfo.InvariantCulture) + " niveles de anidamiento");
                }
            }

            private static bool IsDigit(char c)
            {
                return c >= '0' && c <= '9';
            }
        }
    }
}
