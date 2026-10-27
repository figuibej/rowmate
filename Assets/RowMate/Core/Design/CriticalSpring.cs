using System;

namespace RowMate.Core.Design
{
    // Resorte críticamente amortiguado (ζ = 1, ANIMATION_BIBLE §1.2): sigue un objetivo sin rebote, conservando la
    // velocidad si el objetivo cambia en medio (la píldora de la barra, 04-design-notes §5.4; la escala de FitBox, §5.9).
    // Solución exacta de x'' + 2ωx' + ω²x = 0 para cada paso, así el resultado no depende de la frecuencia de cuadros:
    // un paso de 0,1 s da lo mismo que seis de 1/60 s. Struct sin asignaciones.
    //   x(t) = (x0 + (v0 + ω·x0)·t)·e^(−ωt)      v(t) = (v0 − ω·(v0 + ω·x0)·t)·e^(−ωt)      (x = posición − objetivo)
    // ω de los tokens de resorte: spring.snappy 22 (98 % en ≈ 0,26 s), spring.smooth 12 (≈ 0,49 s), spring.gentle 6 (≈ 0,97 s).
    public struct CriticalSpring
    {
        public const double SnappyOmega = 22;
        public const double SmoothOmega = 12;
        public const double GentleOmega = 6;

        public double Position;
        public double Velocity;

        public CriticalSpring(double position, double velocity = 0)
        {
            Position = position;
            Velocity = velocity;
        }

        // Avanza dtSeconds hacia target. dt ≤ 0 o ω ≤ 0 no cambian nada.
        public void Step(double dtSeconds, double target, double omega)
        {
            if (!(dtSeconds > 0) || !(omega > 0))
            {
                return;
            }

            var x = Position - target;
            var v = Velocity;
            var decay = Math.Exp(-omega * dtSeconds);
            var temp = (v + omega * x) * dtSeconds;
            Position = target + (x + temp) * decay;
            Velocity = (v - omega * temp) * decay;
        }

        // Salta al objetivo, quieto (reducir movimiento, primer cálculo).
        public void SnapTo(double target)
        {
            Position = target;
            Velocity = 0;
        }

        // Llegó (dentro de la tolerancia en posición y casi sin velocidad).
        public bool IsSettled(double target, double tolerance = 1e-3)
        {
            return Math.Abs(Position - target) <= tolerance && Math.Abs(Velocity) <= tolerance;
        }
    }
}
