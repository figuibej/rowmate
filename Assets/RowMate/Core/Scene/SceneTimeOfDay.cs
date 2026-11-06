using System;
using RowMate.Core.Text;

namespace RowMate.Core.Scene
{
    // El sol o la luna del legacy (time_of_day.dart:57-67): posición de pantalla en dp, fuerza del brillo sobre el agua,
    // la fracción del ancho y la elevación de la que salen, y si es la luna. Es contrato (AC-SCN-10-02, -03): la escena 3D pone
    // el astro donde dice CelestialPlacement (D-ART-05), pero la fuerza de las líneas del agua y estos valores siguen siendo estos.
    public readonly struct LegacyLight
    {
        public LegacyLight(double x, double y, double strength, double xFraction, double elevation, bool isMoon)
        {
            X = x;
            Y = y;
            Strength = strength;
            XFraction = xFraction;
            Elevation = elevation;
            IsMoon = isMoon;
        }

        public double X { get; }

        public double Y { get; }

        public double Strength { get; }

        public double XFraction { get; }

        public double Elevation { get; }

        public bool IsMoon { get; }
    }

    // La hora del día del legacy (scene/time_of_day.dart:1-117): la paleta con sus 6 keyframes y el cierre 22 → 28 → 29, la
    // elevación del sol, la noche y el astro. Los keyframes se escriben con los Color(0xFF…) del legacy (ADR 016: los hex de
    // texto solo viven en Tokens.cs; estos son enteros, como en el legacy).
    public static class SceneTimeOfDay
    {
        // La noche: sin((h − 6)/12·π) ≤ −0,4, de 19:34:19 a 4:25:41 (AC-SCN-10-01).
        public static readonly double NightStartHour = 6 + 12 * (1 + Math.Asin(0.4) / Math.PI);
        public static readonly double NightEndHour = 6 - 12 * Math.Asin(0.4) / Math.PI;

        public static readonly ScenePalette Dawn = Palette(0xFF2A1B5E, 0xFFFF9A5C, 0xFF1A2B4A, 0xFF3A2F55, 0xFFFFB070, 0xFFFF7A3D, 0xFFC98A7A, 0.7);
        public static readonly ScenePalette Morning = Palette(0xFF3A7BD5, 0xFFFFD9A0, 0xFF1E5A9E, 0xFF5C8FC9, 0xFFFFE4A0, 0xFFFFC46B, 0xFFDCE8F5, 0.95);
        public static readonly ScenePalette Noon = Palette(0xFF0B4FA8, 0xFF8FD3FF, 0xFF0E4F8F, 0xFF2F8FD6, 0xFFFFF6D5, 0xFFFFE9A6, 0xFFCFE9FF, 1.0);
        public static readonly ScenePalette Sunset = Palette(0xFF2E3C8C, 0xFFFF8C5A, 0xFF163A66, 0xFF7C5C7A, 0xFFFFB36B, 0xFFFF6B3D, 0xFFE0A28A, 0.85);
        public static readonly ScenePalette Dusk = Palette(0xFF0D1A4A, 0xFF4A3A7A, 0xFF0B1E3A, 0xFF2A2A55, 0xFFDDE6FF, 0xFF8FA3FF, 0xFF30335A, 0.5);
        public static readonly ScenePalette Night = Palette(0xFF050A1E, 0xFF121C3A, 0xFF06122A, 0xFF0E1E3C, 0xFFE6EEFF, 0xFFA6B8FF, 0xFF0F1730, 0.35);

        // Keyframes cíclicos (time_of_day.dart:94-98): 22 h → 5 h del día siguiente (29); la noche queda cerrada hasta las 4 h
        // (28) para que la medianoche no sea un falso amanecer.
        private static readonly double[] KeyHours = { 5, 7, 12, 18, 20, 22, 28, 29 };
        private static readonly ScenePalette[] KeyPalettes = { Dawn, Morning, Noon, Sunset, Dusk, Night, Night, Dawn };

        public static int KeyframeCount => KeyHours.Length;

        public static double KeyframeHour(int index)
        {
            return KeyHours[index];
        }

        // :52
        public static double SunElevation(double hour)
        {
            return Math.Sin((hour - 6) / 12 * Math.PI);
        }

        // :55
        public static bool IsNight(double hour)
        {
            return SunElevation(hour) <= -0.4;
        }

        // :57-67: de noche la hora espejada (h + 12) mod 24; x = ancho·clamp(0,25 + 0,5·(h − 6)/12; 0,15; 0,85);
        // y = horizonte − e·horizonte·0,9; fuerza 0,45 de noche y clamp(1,2·e; 0,2; 1) de día.
        public static LegacyLight LightFor(double hour, in SceneCamera cam)
        {
            var night = IsNight(hour);
            var h = night ? DartMath.Mod(hour + 12, 24) : hour;
            var elev = Clamp(Math.Sin((h - 6) / 12 * Math.PI), 0.0, 1.0);
            var fx = Clamp(0.25 + 0.5 * (h - 6) / 12, 0.15, 0.85);
            return new LegacyLight(
                cam.Width * fx,
                cam.HorizonY - elev * cam.HorizonY * 0.9,
                night ? 0.45 : Clamp(elev * 1.2, 0.2, 1.0),
                fx,
                elev,
                night);
        }

        // :69-116: h = hora mod 24 (+24 si < 5) y lineal entre los dos keyframes que la encierran.
        public static ScenePalette PaletteFor(double hour)
        {
            var h = DartMath.Mod(hour, 24);
            if (h < 5)
            {
                h += 24;
            }

            for (var i = 0; i < KeyHours.Length - 1; i++)
            {
                var h0 = KeyHours[i];
                var h1 = KeyHours[i + 1];
                if (h >= h0 && h <= h1)
                {
                    return ScenePalette.Lerp(KeyPalettes[i], KeyPalettes[i + 1], (h - h0) / (h1 - h0));
                }
            }

            return Night;
        }

        // num.clamp de Dart con double: los extremos, y el valor tal cual adentro.
        internal static double Clamp(double x, double lower, double upper)
        {
            if (x < lower)
            {
                return lower;
            }

            return x > upper ? upper : x;
        }

        private static ScenePalette Palette(uint skyTop, uint skyHorizon, uint waterNear, uint waterFar, uint sunColor, uint sunGlow, uint fog,
                                            double ambient)
        {
            return new ScenePalette(SceneColor.FromArgb(skyTop), SceneColor.FromArgb(skyHorizon), SceneColor.FromArgb(waterNear),
                SceneColor.FromArgb(waterFar), SceneColor.FromArgb(sunColor), SceneColor.FromArgb(sunGlow), SceneColor.FromArgb(fog), ambient);
        }
    }
}
