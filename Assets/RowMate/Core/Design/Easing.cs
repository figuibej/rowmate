using System;

namespace RowMate.Core.Design
{
    // Evaluación de una curva cubic-bezier (EasingToken, ANIMATION_BIBLE §1.2) para el motion que maneja el código
    // (D-14: rodillo, latido, píldora, transiciones). Puntos de control (0,0), (x1,y1), (x2,y2), (1,1); x1 y x2 en [0; 1],
    // así x(s) es monótona y hay una sola s para cada t. Se resuelve x(s) = t con Newton y, si no converge, bisección
    // (el método de WebKit, UnitBezier). Sin asignaciones: se puede llamar en cada cuadro.
    public static class Easing
    {
        private const double Epsilon = 1e-7;
        private const int NewtonIterations = 8;
        private const int BisectionIterations = 60;

        // Progreso de la curva en t ∈ [0; 1] (t fuera de rango se recorta). y puede salir de [0; 1] (ease.emphasis).
        public static double Evaluate(EasingToken curve, double t)
        {
            if (curve == null)
            {
                throw new ArgumentNullException(nameof(curve));
            }

            return Evaluate(curve.X1, curve.Y1, curve.X2, curve.Y2, t);
        }

        public static double Evaluate(double x1, double y1, double x2, double y2, double t)
        {
            if (double.IsNaN(t) || t <= 0)
            {
                return 0;
            }

            if (t >= 1)
            {
                return 1;
            }

            if (x1 == y1 && x2 == y2)
            {
                return t; // lineal
            }

            var s = SolveX(x1, x2, t);
            return Sample(y1, y2, s);
        }

        // B(s) para un eje: 3(1−s)²s·p1 + 3(1−s)s²·p2 + s³, en forma de Horner.
        private static double Sample(double p1, double p2, double s)
        {
            var c = 3 * p1;
            var b = 3 * (p2 - p1) - c;
            var a = 1 - c - b;
            return ((a * s + b) * s + c) * s;
        }

        private static double SampleDerivative(double p1, double p2, double s)
        {
            var c = 3 * p1;
            var b = 3 * (p2 - p1) - c;
            var a = 1 - c - b;
            return (3 * a * s + 2 * b) * s + c;
        }

        private static double SolveX(double x1, double x2, double t)
        {
            var s = t;
            for (var i = 0; i < NewtonIterations; i++)
            {
                var error = Sample(x1, x2, s) - t;
                if (Math.Abs(error) < Epsilon)
                {
                    return s;
                }

                var derivative = SampleDerivative(x1, x2, s);
                if (Math.Abs(derivative) < 1e-6)
                {
                    break;
                }

                s -= error / derivative;
            }

            var low = 0.0;
            var high = 1.0;
            s = t;
            for (var i = 0; i < BisectionIterations; i++)
            {
                var x = Sample(x1, x2, s);
                if (Math.Abs(x - t) < Epsilon)
                {
                    return s;
                }

                if (x < t)
                {
                    low = s;
                }
                else
                {
                    high = s;
                }

                s = (low + high) / 2;
            }

            return s;
        }
    }
}
