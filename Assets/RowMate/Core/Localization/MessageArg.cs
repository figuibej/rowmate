using System;
using RowMate.Core.Text;

namespace RowMate.Core.Localization
{
    // Argumento con nombre de un mensaje del catálogo: texto (parámetro "String") o entero (parámetro "int", que es
    // el único que puede elegir un plural). Los enteros se escriben con Fmt.Integer (= int.toString() de Dart).
    public readonly struct MessageArg
    {
        private readonly string? _name;

        private MessageArg(string name, MessageArgKind kind, string? textValue, long numberValue)
        {
            _name = name;
            Kind = kind;
            TextValue = textValue;
            NumberValue = numberValue;
        }

        public string Name => _name ?? "";

        public MessageArgKind Kind { get; }

        // Solo en Text.
        public string? TextValue { get; }

        // Solo en Number.
        public long NumberValue { get; }

        public static MessageArg Text(string name, string value)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            return new MessageArg(name, MessageArgKind.Text, value, 0);
        }

        public static MessageArg Number(string name, long value)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            return new MessageArg(name, MessageArgKind.Number, null, value);
        }

        // Texto que el argumento pone en el mensaje.
        public string Render()
        {
            return Kind == MessageArgKind.Number ? Fmt.Integer(NumberValue) : TextValue ?? "";
        }

        public override string ToString()
        {
            return Kind == MessageArgKind.Number ? Name + "=" + Fmt.Integer(NumberValue) : Name + "=\"" + TextValue + "\"";
        }
    }
}
