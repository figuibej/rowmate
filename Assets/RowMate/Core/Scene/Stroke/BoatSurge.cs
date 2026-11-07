using System;

namespace RowMate.Core.Scene.Stroke
{
    // El surge (ANIMATION_BIBLE §4.5; AC-SCN-06-02): un desfase periódico de dibujo respecto de la posición media del bote, mínimo en
    // el catch y máximo en el finish, con media exacta 0 y desfase 0 en el catch y en el finish. La distancia de escena no lo ve.
    public static class BoatSurge
    {
        // A(spm) = clamp(0,075 − 0,001·(spm − 20); 0,05; 0,08) × (0,85 + 0,30·e).
        public static double Amplitude(double spm, double effort)
        {
            return SceneMath.Clamp(0.075 - 0.001 * (spm - 20), 0.05, 0.08) * (0.85 + 0.30 * effort);
        }

        // g: −1 + 2·smooth(φ/r) en el drive; 1 − 2·smooth((φ − r)/(1 − r)) en la recuperación.
        public static double SpeedFactor(double phi, double ratio)
        {
            if (phi < ratio)
            {
                return -1 + 2 * StrokeCycle.Smooth(phi / ratio);
            }

            return 1 - 2 * StrokeCycle.Smooth((phi - ratio) / (1 - ratio));
        }

        // La integral analítica de v̄·A·g: v̄·T·r·A·(−u + 2u³ − u⁴) en el drive y v̄·T·(1 − r)·A·(w − 2w³ + w⁴) en la recuperación, con
        // T = 60/max(spm_s; 12).
        public static double OffsetMeters(double phi, double ratio, double amplitude, double meanSpeed, double spmSmoothed)
        {
            var period = 60 / Math.Max(spmSmoothed, SceneState.IdleSpm);
            if (phi < ratio)
            {
                var u = phi / ratio;
                return meanSpeed * period * ratio * amplitude * (-u + 2 * u * u * u - u * u * u * u);
            }

            var w = (phi - ratio) / (1 - ratio);
            return meanSpeed * period * (1 - ratio) * amplitude * (w - 2 * w * w * w + w * w * w * w);
        }
    }
}
