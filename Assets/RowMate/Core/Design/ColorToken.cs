using System;

namespace RowMate.Core.Design
{
    // Token de color (ADR 016). El hex existe solo en Tokens.cs (PureCodeRulesTests).
    public sealed class ColorToken : IDesignToken
    {
        private ColorToken(string name, Rgb rgb, int alphaPercent, ColorRole role, bool isContract)
        {
            Name = name;
            Rgb = rgb;
            AlphaPercent = alphaPercent;
            Alpha = alphaPercent / 100.0;
            Role = role;
            IsContract = isContract;
        }

        public string Name { get; }

        public TokenKind Kind => TokenKind.Color;

        public Rgb Rgb { get; }

        // Transparencia en porcentaje entero, como la escribe el Designer ("@86 %"); 100 = opaco.
        public int AlphaPercent { get; }

        // AlphaPercent / 100.0, SIN redondear a byte (método de contraste de 02-tech-design §4.4).
        public double Alpha { get; }

        public ColorRole Role { get; }

        // [CONTRATO]: hex del legacy que no se cambia nunca (AC-BASE-04).
        public bool IsContract { get; }

        public bool IsOpaque => AlphaPercent == 100;

        // Color de contrato del legacy, opaco.
        public static ColorToken Contract(string name, string hex, ColorRole role = ColorRole.Graphic)
        {
            return new ColorToken(CheckName(name), Rgb.Parse(hex), 100, role, true);
        }

        // Color opaco.
        public static ColorToken Hex(string name, string hex, ColorRole role = ColorRole.None)
        {
            return new ColorToken(CheckName(name), Rgb.Parse(hex), 100, role, false);
        }

        // Color con transparencia en porcentaje entero [0; 100].
        public static ColorToken HexAlpha(string name, string hex, int alphaPercent, ColorRole role = ColorRole.None)
        {
            if (alphaPercent < 0 || alphaPercent > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(alphaPercent), alphaPercent,
                    "ColorToken " + name + ": la transparencia va de 0 a 100.");
            }

            return new ColorToken(CheckName(name), Rgb.Parse(hex), alphaPercent, role, false);
        }

        public override string ToString()
        {
            return Name + " " + Rgb.ToHex() + (IsOpaque ? "" : " @" + AlphaPercent + " %");
        }

        private static string CheckName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("El token necesita nombre.", nameof(name));
            }

            return name;
        }
    }
}
