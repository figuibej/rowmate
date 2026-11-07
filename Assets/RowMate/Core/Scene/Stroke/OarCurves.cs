using System;

namespace RowMate.Core.Scene.Stroke
{
    // Las curvas del remo y de la pala sobre la fase canónica c (ANIMATION_BIBLE §3.6; AC-SCN-05-07, AC-SCN-08-03). Cada tramo es
    // smooth (C1, velocidad 0 en los extremos): ningún ángulo ni altura salta entre dos cuadros.
    public static class OarCurves
    {
        public const double CatchDeg = -62, ArcDeg = 104;
        public const double BuriedY = -0.13, ExtractedY = 0.05, RecoveryY = 0.09, SquaredY = 0.17;

        // α_v = −62° + 104°·(α_legacy + 55°)/90° (catch −62°, finish +42°; QA7-G-02), con α_legacy = StrokeCycle.OarSweep(c).
        public static double VisualSweepDeg(double c)
        {
            var legacy = StrokeCycle.OarSweep(c) * 180 / Math.PI;
            return CatchDeg + ArcDeg * (legacy + 55) / 90;
        }

        // El centro de la pala sobre el agua (m): −0,13 enterrada (0,015–0,355); sale a +0,05 (0,355–0,395); +0,09 (0,395–0,43);
        // se cuadra a +0,17 (0,80–0,93); entra a −0,13 (0,97 → 1,015, con c' = c + 1 si c < 0,5). Cruza el agua en c ≈ 0,381 (sale)
        // y c ≈ 0,9945 (entra).
        public static double BladeCenterY(double c)
        {
            if (c < 0.015)
            {
                return Segment(c + 1, 0.97, 1.015, SquaredY, BuriedY);
            }

            if (c < 0.355)
            {
                return BuriedY;
            }

            if (c < 0.395)
            {
                return Segment(c, 0.355, 0.395, BuriedY, ExtractedY);
            }

            if (c < 0.43)
            {
                return Segment(c, 0.395, 0.43, ExtractedY, RecoveryY);
            }

            if (c < 0.80)
            {
                return RecoveryY;
            }

            if (c < 0.93)
            {
                return Segment(c, 0.80, 0.93, RecoveryY, SquaredY);
            }

            if (c < 0.97)
            {
                return SquaredY;
            }

            return Segment(c, 0.97, 1.015, SquaredY, BuriedY);
        }

        // El plumeo (grados sobre el eje del remo): 0 → 90 en 0,385–0,43 (después de que la pala salió); 90 → 0 en 0,84–0,95.
        public static double FeatherDeg(double c)
        {
            if (c < 0.385)
            {
                return 0;
            }

            if (c < 0.43)
            {
                return Segment(c, 0.385, 0.43, 0, 90);
            }

            if (c < 0.84)
            {
                return 90;
            }

            return c < 0.95 ? Segment(c, 0.84, 0.95, 90, 0) : 0;
        }

        // La flexión de la caña en la punta (m): (0,02 + 0,04·e)·sin(π·c/0,36) en el drive, 0 después (AC-SCN-08-03).
        public static double ShaftBendM(double c, double effort)
        {
            return c < 0.36 && c >= 0 ? (0.02 + 0.04 * effort) * Math.Sin(Math.PI * c / 0.36) : 0;
        }

        // La altura del puño por la palanca del remo (ANIMATION_BIBLE §3.5): y_pin + (y_pin − y_pala)·0,82/1,76.
        public static double GripY(in BoatSpec boat, double bladeY)
        {
            return boat.PinY + (boat.PinY - bladeY) * boat.GripFromPin / boat.BladeFromPin;
        }

        private static double Segment(double c, double a, double b, double from, double to)
        {
            return from + (to - from) * StrokeCycle.Ramp(c, a, b);
        }
    }
}
