namespace RowMate.Core.Design
{
    // La salida de la capa de arranque (It. 14, AC-APP-16-10; DT14-10, DT14-11; T-01 de 00-arte §3.1): fondo bg.app y la marca de
    // 96 dp centrada hasta que el shell está listo; desde ahí, opacidad 1 → 0 en 200 ms con ease.exit (con reducir movimiento,
    // 120 ms lineal) y la capa sale de la jerarquía. No hay tiempo mínimo. Si pasan 1 500 ms sin shell listo, el indicador de
    // 32 dp aparece 32 dp debajo de la marca con un fundido de 160 ms. El tiempo llega por argumento (ms desde el hecho).
    public static class BootSplashMotion
    {
        public const int ExitMs = 200;
        public const int ReducedExitMs = 120;
        public const int MarkDp = 96;
        public const int SpinnerDelayMs = 1500;
        public const int SpinnerFadeMs = 160;
        public const int SpinnerDp = 32;
        public const int SpinnerGapDp = 32;

        public static int DurationMs(bool reduceMotion)
        {
            return reduceMotion ? ReducedExitMs : ExitMs;
        }

        // Opacidad de la capa a msSinceReady del shell listo (negativo = todavía no listo → 1).
        public static double Opacity(double msSinceReady, bool reduceMotion)
        {
            if (double.IsNaN(msSinceReady) || msSinceReady <= 0)
            {
                return 1;
            }

            var duration = DurationMs(reduceMotion);
            if (msSinceReady >= duration)
            {
                return 0;
            }

            var t = msSinceReady / duration;
            return reduceMotion ? 1 - t : 1 - Easing.Evaluate(Tokens.EaseExit, t);
        }

        // ¿Terminó la salida? (la capa sale de la jerarquía).
        public static bool IsDone(double msSinceReady, bool reduceMotion)
        {
            return !double.IsNaN(msSinceReady) && msSinceReady >= DurationMs(reduceMotion);
        }

        // ¿La capa bloquea la entrada? Hasta que empieza la salida.
        public static bool BlocksInput(double msSinceReady)
        {
            return double.IsNaN(msSinceReady) || msSinceReady < 0;
        }

        // Opacidad del indicador a msSinceStart del primer cuadro: 0 hasta los 1 500 ms y un fundido de 160 ms (lineal) después.
        public static double SpinnerOpacity(double msSinceStart)
        {
            if (double.IsNaN(msSinceStart) || msSinceStart <= SpinnerDelayMs)
            {
                return 0;
            }

            var t = (msSinceStart - SpinnerDelayMs) / SpinnerFadeMs;
            return t >= 1 ? 1 : t;
        }
    }
}
