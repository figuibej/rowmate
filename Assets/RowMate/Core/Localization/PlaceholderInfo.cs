using System;

namespace RowMate.Core.Localization
{
    // Parámetro declarado en los metadatos "@clave" del catálogo: nombre y tipo ("String" o "int", como en los .arb).
    public sealed class PlaceholderInfo
    {
        public const string StringType = "String";
        public const string IntType = "int";

        public PlaceholderInfo(string name, string type)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Type = type ?? throw new ArgumentNullException(nameof(type));
        }

        public string Name { get; }

        public string Type { get; }

        // Tipo de MessageArg que acepta.
        public MessageArgKind Kind => Type == IntType ? MessageArgKind.Number : MessageArgKind.Text;

        public override string ToString()
        {
            return Name + ":" + Type;
        }
    }
}
