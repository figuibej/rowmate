using System;

namespace RowMate.Core.Design
{
    // Escalón tipográfico (ART_BIBLE §4.3): familia, tamaño y alto de línea en dp, peso.
    // El USS emite solo el tamaño (--rm-<token>-size): USS no tiene line-height.
    public sealed class TypeScaleToken : IDesignToken
    {
        private TypeScaleToken(string name, FontFamilyRole family, double sizeDp, double lineHeightDp, int weight)
        {
            Name = name;
            Family = family;
            SizeDp = sizeDp;
            LineHeightDp = lineHeightDp;
            Weight = weight;
        }

        public string Name { get; }

        public TokenKind Kind => TokenKind.TypeScale;

        public FontFamilyRole Family { get; }

        public double SizeDp { get; }

        public double LineHeightDp { get; }

        public int Weight { get; }

        public static TypeScaleToken Of(string name, FontFamilyRole family, double sizeDp, double lineHeightDp, int weight)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("El token necesita nombre.", nameof(name));
            }

            if (!(sizeDp > 0) || !(lineHeightDp > 0) || double.IsInfinity(sizeDp) || double.IsInfinity(lineHeightDp))
            {
                throw new ArgumentOutOfRangeException(nameof(sizeDp), "TypeScaleToken " + name + ": tamaño y alto de línea positivos.");
            }

            if (weight < 100 || weight > 900)
            {
                throw new ArgumentOutOfRangeException(nameof(weight), weight, "TypeScaleToken " + name + ": peso de 100 a 900.");
            }

            return new TypeScaleToken(name, family, sizeDp, lineHeightDp, weight);
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
