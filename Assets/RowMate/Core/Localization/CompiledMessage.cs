using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using RowMate.Core.Text;

namespace RowMate.Core.Localization
{
    // Mensaje del catálogo ya analizado por MessageFormatter (se analiza una vez, al cargar el catálogo).
    public sealed class CompiledMessage
    {
        private readonly MessagePart[] _parts;
        private readonly HashSet<string> _names;
        private readonly HashSet<string> _plural;
        private readonly string? _literal;

        internal CompiledMessage(string pattern, MessagePart[] parts, IList<string> names, ICollection<string> pluralNames)
        {
            Pattern = pattern;
            _parts = parts;
            ArgumentNames = new ReadOnlyCollection<string>(new List<string>(names));
            _names = new HashSet<string>(names, StringComparer.Ordinal);
            _plural = new HashSet<string>(pluralNames, StringComparer.Ordinal);
            _literal = AllLiteral(parts);
        }

        // Texto original del catálogo.
        public string Pattern { get; }

        // Todos los argumentos que usa el mensaje (también los de adentro de un plural), en orden de aparición.
        public IReadOnlyCollection<string> ArgumentNames { get; }

        // Verdadero si el argumento elige un caso de plural (entonces tiene que ser un entero).
        public bool IsPluralArgument(string name)
        {
            return name != null && _plural.Contains(name);
        }

        // Arma el texto. Los argumentos tienen que ser exactamente los del mensaje (ni de más, ni de menos, sin
        // repetidos) y los de plural, enteros; si no, ArgumentException.
        public string Format(IReadOnlyList<MessageArg> args)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            var values = new Dictionary<string, MessageArg>(args.Count, StringComparer.Ordinal);
            for (var i = 0; i < args.Count; i++)
            {
                var arg = args[i];
                if (!_names.Contains(arg.Name))
                {
                    throw new ArgumentException("Mensaje \"" + Pattern + "\": argumento desconocido \"" + arg.Name + "\".", nameof(args));
                }

                if (values.ContainsKey(arg.Name))
                {
                    throw new ArgumentException("Mensaje \"" + Pattern + "\": argumento repetido \"" + arg.Name + "\".", nameof(args));
                }

                if (_plural.Contains(arg.Name) && arg.Kind != MessageArgKind.Number)
                {
                    throw new ArgumentException("Mensaje \"" + Pattern + "\": el argumento de plural \"" + arg.Name + "\" tiene que ser un entero.", nameof(args));
                }

                values.Add(arg.Name, arg);
            }

            foreach (var name in ArgumentNames)
            {
                if (!values.ContainsKey(name))
                {
                    throw new ArgumentException("Mensaje \"" + Pattern + "\": falta el argumento \"" + name + "\".", nameof(args));
                }
            }

            if (_literal != null)
            {
                return _literal;
            }

            var sb = new StringBuilder(Pattern.Length + 16);
            MessagePart.RenderAll(_parts, values, 0, sb);
            return sb.ToString();
        }

        private static string? AllLiteral(MessagePart[] parts)
        {
            var sb = new StringBuilder();
            foreach (var part in parts)
            {
                if (!(part is LiteralPart literal))
                {
                    return null;
                }

                sb.Append(literal.Text);
            }

            return sb.ToString();
        }
    }

    // Partes de un mensaje analizado (internas: las arma MessageFormatter).
    internal abstract class MessagePart
    {
        // pound = el número del plural más cercano (lo que vale '#' dentro de un caso).
        public abstract void Render(Dictionary<string, MessageArg> args, long pound, StringBuilder sb);

        public static void RenderAll(MessagePart[] parts, Dictionary<string, MessageArg> args, long pound, StringBuilder sb)
        {
            for (var i = 0; i < parts.Length; i++)
            {
                parts[i].Render(args, pound, sb);
            }
        }
    }

    internal sealed class LiteralPart : MessagePart
    {
        public LiteralPart(string text)
        {
            Text = text;
        }

        public string Text { get; }

        public override void Render(Dictionary<string, MessageArg> args, long pound, StringBuilder sb)
        {
            sb.Append(Text);
        }
    }

    // {nombre}: texto tal cual o entero con Fmt.Integer.
    internal sealed class ArgumentPart : MessagePart
    {
        public ArgumentPart(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public override void Render(Dictionary<string, MessageArg> args, long pound, StringBuilder sb)
        {
            sb.Append(args[Name].Render());
        }
    }

    // '#' dentro de un caso de plural: el número del plural.
    internal sealed class PoundPart : MessagePart
    {
        public static readonly PoundPart Instance = new PoundPart();

        public override void Render(Dictionary<string, MessageArg> args, long pound, StringBuilder sb)
        {
            sb.Append(Fmt.Integer(pound));
        }
    }

    // {nombre, plural, =N{…} one{…} other{…}}: primero el =N exacto, después one si el número es 1, si no other.
    internal sealed class PluralPart : MessagePart
    {
        private readonly KeyValuePair<long, MessagePart[]>[] _exact;
        private readonly MessagePart[]? _one;
        private readonly MessagePart[] _other;

        public PluralPart(string name, KeyValuePair<long, MessagePart[]>[] exact, MessagePart[]? one, MessagePart[] other)
        {
            Name = name;
            _exact = exact;
            _one = one;
            _other = other;
        }

        public string Name { get; }

        public override void Render(Dictionary<string, MessageArg> args, long pound, StringBuilder sb)
        {
            var n = args[Name].NumberValue;
            RenderAll(Select(n), args, n, sb);
        }

        private MessagePart[] Select(long n)
        {
            for (var i = 0; i < _exact.Length; i++)
            {
                if (_exact[i].Key == n)
                {
                    return _exact[i].Value;
                }
            }

            if (n == 1 && _one != null)
            {
                return _one;
            }

            return _other;
        }
    }
}
