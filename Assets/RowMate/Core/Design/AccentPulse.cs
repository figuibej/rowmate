using System;

namespace RowMate.Core.Design
{
    // Destello del acento de una tarjeta del tablero al llegar un dato cuyo TEXTO cambió (04-design-notes §5.2, DN-07,
    // AC-BLE-11-06): k = 1 en el cuadro de la lectura y baja a 0,60 en 300 ms con ease.pulse. El borde superior va en el
    // color de la métrica con alfa k (0,60 en reposo, como el legacy: withOpacity(0.6), device_screen.dart:300; DV-02) y el
    // brillo, con opacidad 0,14·k. Una lectura nueva reinicia en 1. La tarjeta del pulsómetro no destella (k = 0,6 fijo).
    public static class AccentPulse
    {
        // Factor k a elapsedMs del dato (antes del dato o sin dato: reposo, 0,60).
        public static double K(double elapsedMs)
        {
            if (double.IsNaN(elapsedMs) || elapsedMs < 0 || elapsedMs >= MotionSpecs.AccentFlashMs)
            {
                return MotionSpecs.AccentRestAlpha;
            }

            var u = elapsedMs / MotionSpecs.AccentFlashMs;
            return 1 - (1 - MotionSpecs.AccentRestAlpha) * Easing.Evaluate(Tokens.EasePulse, u);
        }

        // Opacidad del brillo bajo el borde: 0,14·k (0,084 en reposo).
        public static double GlowOpacity(double elapsedMs)
        {
            return MotionSpecs.AccentGlowPeak * K(elapsedMs);
        }

        public static bool IsFinished(double elapsedMs)
        {
            return !(elapsedMs < MotionSpecs.AccentFlashMs);
        }

        // Reposo (sin destello): 0,60, el alfa del legacy.
        public static double RestK => MotionSpecs.AccentRestAlpha;

        // Para quien necesite el color con alfa: alfa del token × k (los colores de métrica son opacos).
        public static double AlphaFor(ColorToken token, double elapsedMs)
        {
            if (token == null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            return token.Alpha * K(elapsedMs);
        }
    }
}
