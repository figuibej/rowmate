using RowMate.Core.Design;

namespace RowMate.Core.Scene.Stroke
{
    public enum BoatShadowMode
    {
        Map,
        Oval,
    }

    // La sombra del bote (AC-SCN-08-06; DT7-08; boat_painter.dart:118-130): con mapa de sombras si el nivel las tiene y la fuerza de
    // sombra de la luz del cuadro es ≥ 0,05; si no, el óvalo del legacy (negro, alfa 0,08·ambiente, de z 5,8 a 12,5, 0,5 m de ancho,
    // corrido 0,35 m al lado opuesto del astro en la pantalla del legacy; con el astro centrado, a la izquierda). Nunca los dos.
    public static class BoatShadowRule
    {
        public const double MinLightStrength = 0.05;
        public const double OvalOffset = 0.35, OvalWidth = 0.5;
        public const double OvalBowZ = SceneCamera.BoatZ + 4.0 - 1.0, OvalSternZ = SceneCamera.BoatZ - 4.2 + 0.5;

        public static BoatShadowMode Mode(bool tierHasShadows, double lightShadowStrength)
        {
            return tierHasShadows && lightShadowStrength >= MinLightStrength ? BoatShadowMode.Map : BoatShadowMode.Oval;
        }

        // :120: +0,35 si el astro está a la izquierda del centro; si no (también centrado), −0,35.
        public static double LegacySide(double astroScreenX, double centerX)
        {
            return astroScreenX < centerX ? OvalOffset : -OvalOffset;
        }

        // :128.
        public static double OvalAlpha(double ambient)
        {
            return 0.08 * ambient;
        }
    }

    // El lado del óvalo: cuando el astro cruza el centro de la pantalla pasa de +0,35 a −0,35 en 1,5 s ease.move (DA7-10); con la
    // hora forzada o al abrir, en el acto. Sin asignar.
    public struct ShadowSideFader
    {
        public double Value;
        private double _from;
        private double _to;
        private double _progress;
        private bool _started;

        public void Step(double target, double dt, bool instant)
        {
            if (instant || !_started)
            {
                Value = target;
                _from = target;
                _to = target;
                _progress = 1;
                _started = true;
                return;
            }

            if (target != _to)
            {
                _from = Value;
                _to = target;
                _progress = 0;
            }

            if (_progress < 1)
            {
                _progress = SceneMath.Clamp01(_progress + (dt < 0 ? 0 : dt) / MotionSpecs.ShadowSideFadeSeconds);
                Value = _from + (_to - _from) * Easing.Evaluate(Tokens.EaseMove, _progress);
            }
        }
    }
}
