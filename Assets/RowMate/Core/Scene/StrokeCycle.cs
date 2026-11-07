using System;

namespace RowMate.Core.Scene
{
    // La secuencia de la palada en función de la fase p ∈ [0; 1) (scene/stroke_cycle.dart:5-51; F-SCN-05). Drive 0,00–0,40 y
    // recuperación 0,40–1,00 (ratio 1:2 del contrato). Puerto sin cambiar un número ni el orden de las operaciones (LT-92 a LT-95):
    // lo visual se evalúa sobre la fase canónica c (StrokeRhythm) con estas mismas funciones.
    public static class StrokeCycle
    {
        // :9: el ángulo del remo en el catch (pala hacia la proa).
        public const double CatchSweep = -55 * Math.PI / 180;

        // :12: el ángulo del remo en el finish (pala hacia la popa).
        public const double FinishSweep = 35 * Math.PI / 180;

        // :15-18: 0 = piernas plegadas (catch), 1 = extendidas (finish).
        public static double Legs(double p)
        {
            if (p < 0.60)
            {
                return Ramp(p, 0.00, 0.18);
            }

            return 1 - Ramp(p, 0.60, 1.00);
        }

        // :21-24: 0 = tronco adelante (+25°), 1 = tronco atrás (−20°).
        public static double Back(double p)
        {
            if (p < 0.50)
            {
                return Ramp(p, 0.10, 0.30);
            }

            return 1 - Ramp(p, 0.50, 0.75);
        }

        // :27-30: 0 = brazos estirados, 1 = flexionados al pecho.
        public static double Arms(double p)
        {
            if (p < 0.40)
            {
                return Ramp(p, 0.22, 0.36);
            }

            return 1 - Ramp(p, 0.40, 0.60);
        }

        // :33-34: la inclinación del tronco (rad), + hacia la popa (hacia la cámara).
        public static double TorsoLean(double p)
        {
            return Lerp(25 * Math.PI / 180, -20 * Math.PI / 180, Back(p));
        }

        // :36.
        public static bool BladeInWater(double p)
        {
            return p < 0.38;
        }

        // :37.
        public static bool BladeFeathered(double p)
        {
            return !BladeInWater(p);
        }

        // :41-45: el ángulo del remo respecto de la perpendicular al bote (rad); negativo = pala hacia la proa.
        public static double OarSweep(double p)
        {
            if (p < 0.36)
            {
                return Lerp(CatchSweep, FinishSweep, Smooth(p / 0.36));
            }

            if (p < 0.40)
            {
                return FinishSweep;
            }

            return Lerp(FinishSweep, CatchSweep, Smooth((p - 0.40) / 0.60));
        }

        // :47-48 (el clamp de Dart sobre double).
        public static double Ramp(double p, double a, double b)
        {
            var t = (p - a) / (b - a);
            return Smooth(t < 0.0 ? 0.0 : (t > 1.0 ? 1.0 : t));
        }

        // :49.
        public static double Smooth(double t)
        {
            return t * t * (3 - 2 * t);
        }

        // :50.
        public static double Lerp(double a, double b, double t)
        {
            return a + (b - a) * t;
        }
    }
}
