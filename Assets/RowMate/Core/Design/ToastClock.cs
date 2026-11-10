using System;

namespace RowMate.Core.Design
{
    // El reloj de un aviso con acción (It. 14, #69, AC-DEBT-05; DT14-29; A12-04): cuenta el tiempo restante y se detiene mientras
    // el puntero o el foco están encima; al salir los dos, sigue desde donde estaba (no se reinicia). El tiempo llega por
    // argumento (Advance con los ms del cuadro). Sin asignaciones.
    public sealed class ToastClock
    {
        private bool _hover;
        private bool _focus;

        public ToastClock(double durationMs)
        {
            if (!(durationMs > 0))
            {
                throw new ArgumentOutOfRangeException(nameof(durationMs), durationMs, "ToastClock: la duración tiene que ser positiva.");
            }

            DurationMs = durationMs;
            RemainingMs = durationMs;
        }

        public double DurationMs { get; }

        public double RemainingMs { get; private set; }

        public bool IsPaused => _hover || _focus;

        public bool Expired => RemainingMs <= 0;

        public void SetHover(bool hover)
        {
            _hover = hover;
        }

        public void SetFocus(bool focus)
        {
            _focus = focus;
        }

        // Avanza deltaMs si no está en pausa; devuelve true si en este paso venció.
        public bool Advance(double deltaMs)
        {
            if (Expired || IsPaused || !(deltaMs > 0))
            {
                return false;
            }

            RemainingMs -= deltaMs;
            return RemainingMs <= 0;
        }

        // Un aviso nuevo en el mismo lugar: vuelve a la duración completa (la pausa sigue según el puntero y el foco).
        public void Restart()
        {
            RemainingMs = DurationMs;
        }
    }
}
