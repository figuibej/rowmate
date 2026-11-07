using System;

namespace RowMate.Core.Scene.Stroke
{
    // El ritmo según el SPM (Q-70; AC-SCN-04-05; ANIMATION_BIBLE §3.2): el drive casi no se acorta y la recuperación sí. La fase
    // visual uniforme φ se mapea a la canónica c (la del legacy, drive 0–0,40) con el ratio de drive r; los quiebres caen en c = 0 y
    // c = 0,40, donde todos los ramps del legacy tienen velocidad 0.
    public static class StrokeRhythm
    {
        public const double MinRatio = 0.22, MaxRatio = 0.48;
        public const double CanonicalDrive = 0.40;

        // T_d = clamp(1,05 − 0,0194·(spm − 18); 0,60; 1,15) s.
        public static double DriveSeconds(double spm)
        {
            return SceneMath.Clamp(1.05 - 0.0194 * (spm - 18), 0.60, 1.15);
        }

        // r = clamp(T_d·spm/60; 0,22; 0,48).
        public static double DriveRatio(double spm)
        {
            return SceneMath.Clamp(DriveSeconds(spm) * spm / 60, MinRatio, MaxRatio);
        }

        // c = 0,40·φ/r si φ < r; si no, 0,40 + 0,60·(φ − r)/(1 − r). Continua y monótona.
        public static double ToCanonical(double phase, double ratio)
        {
            return phase < ratio
                ? CanonicalDrive * phase / ratio
                : CanonicalDrive + (1 - CanonicalDrive) * (phase - ratio) / (1 - ratio);
        }

        // La inversa.
        public static double FromCanonical(double canonical, double ratio)
        {
            return canonical < CanonicalDrive
                ? canonical * ratio / CanonicalDrive
                : ratio + (canonical - CanonicalDrive) * (1 - ratio) / (1 - CanonicalDrive);
        }
    }
}
