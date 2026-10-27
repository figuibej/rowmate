using System;

namespace RowMate.Core.Design
{
    // Curva de movimiento (ANIMATION_BIBLE §1.2). En UI Toolkit manda la palabra clave de USS (USS no tiene
    // cubic-bezier); el cubic-bezier es la aproximación para el código C# que necesite la misma curva.
    // Solo x se limita a [0; 1]: y puede salir del rango (ease.emphasis tiene y1 = 1,56, el rebote).
    public sealed class EasingToken : IDesignToken
    {
        private EasingToken(string name, double x1, double y1, double x2, double y2, string? ussKeyword)
        {
            Name = name;
            X1 = x1;
            Y1 = y1;
            X2 = x2;
            Y2 = y2;
            UssKeyword = ussKeyword;
        }

        public string Name { get; }

        public TokenKind Kind => TokenKind.Easing;

        public double X1 { get; }

        public double Y1 { get; }

        public double X2 { get; }

        public double Y2 { get; }

        // Palabra clave de transition-timing-function (ease-out-cubic…). Sin palabra clave, el USS no la emite.
        public string? UssKeyword { get; }

        public static EasingToken CubicBezier(string name, double x1, double y1, double x2, double y2, string? ussKeyword = null)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("El token necesita nombre.", nameof(name));
            }

            if (!(x1 >= 0 && x1 <= 1) || !(x2 >= 0 && x2 <= 1))
            {
                throw new ArgumentOutOfRangeException(nameof(x1), "EasingToken " + name + ": x1 y x2 van de 0 a 1.");
            }

            if (double.IsNaN(y1) || double.IsInfinity(y1) || double.IsNaN(y2) || double.IsInfinity(y2))
            {
                throw new ArgumentOutOfRangeException(nameof(y1), "EasingToken " + name + ": y1 e y2 tienen que ser finitos.");
            }

            return new EasingToken(name, x1, y1, x2, y2, ussKeyword);
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
