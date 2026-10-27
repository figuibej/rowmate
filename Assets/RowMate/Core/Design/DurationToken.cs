using System;

namespace RowMate.Core.Design
{
    // Duración de movimiento en milisegundos (ANIMATION_BIBLE §1.1).
    public sealed class DurationToken : IDesignToken
    {
        private DurationToken(string name, int milliseconds)
        {
            Name = name;
            Milliseconds = milliseconds;
        }

        public string Name { get; }

        public TokenKind Kind => TokenKind.Duration;

        public int Milliseconds { get; }

        public TimeSpan Duration => TimeSpan.FromMilliseconds(Milliseconds);

        public static DurationToken Ms(string name, int milliseconds)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("El token necesita nombre.", nameof(name));
            }

            if (milliseconds < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(milliseconds), milliseconds, "DurationToken " + name + ": duración negativa.");
            }

            return new DurationToken(name, milliseconds);
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
