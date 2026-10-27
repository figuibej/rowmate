using System;

namespace RowMate.Core.Design
{
    // Longitud en dp (1 px de USS = 1 dp a 160 DPI, ART_BIBLE §2.4).
    public sealed class LengthToken : IDesignToken
    {
        private LengthToken(string name, double value)
        {
            Name = name;
            Value = value;
        }

        public string Name { get; }

        public TokenKind Kind => TokenKind.Length;

        public double Value { get; }

        public static LengthToken Dp(string name, double dp)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("El token necesita nombre.", nameof(name));
            }

            if (double.IsNaN(dp) || double.IsInfinity(dp))
            {
                throw new ArgumentOutOfRangeException(nameof(dp), dp, "LengthToken " + name + ": valor no finito.");
            }

            return new LengthToken(name, dp);
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
