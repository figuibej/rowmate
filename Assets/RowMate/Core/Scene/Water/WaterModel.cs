using System;

namespace RowMate.Core.Scene.Water
{
    // El contrato del agua del legacy en números (water.frag:55-98; water_painter.dart:32-60; F-SCN-12; AC-6B-10), con la cámara
    // del contrato a 2,2 m y la popa a 5,3 m (9,5 − 4,2). Es lo que el agua avanzada tiene que dar con la normal plana: el shader
    // suma ondas y reflejo encima, nunca cambia estos números (06b-agua-avanzada/02-tech-design §3.1, DT6b-03, DT6b-04).
    public static class WaterModel
    {
        public const double CamHeight = SceneCamera.CamHeight;
        public const double SternRelZ = SceneCamera.BoatZ - WaterShaderInputs.SternOffset;
        public const double WakeFoamMax = 0.35;
        public const double ColumnPeak = 0.4;

        // water.frag:66-68: lerp(near; far; S(2; 120; z)) y el brillo de la ola × (1 + h·A·0,25).
        public const double BaseNear = 2, BaseFar = 120, ColorGain = CoastSwell.ColorGain;

        // water.frag:71-72: el peso del reflejo 0,6·(1 − cos)³ con cos = 2,2/√(z² + 2,2²).
        public const double FresnelGain = 0.6, FresnelPower = 3;

        // water.frag:96: la niebla del agua.
        public const double FogNear = 80, FogFar = 400;

        // water.frag:75-76: la columna de brillo de 25 + 4·z dp de ancho.
        public const double ColumnBaseDp = 25, ColumnPerMeterDp = 4;

        // water.frag:85-86 y water_painter.dart:57: la estela, 0,3 + 0,35·retroceso de semiancho y clamp(v/4; 0; 1) de intensidad.
        public const double WakeHalfWidthBase = 0.3, WakeHalfWidthSlope = 0.35, WakeFullSpeed = 4;

        public static SceneColor BaseColor(in SceneColor near, in SceneColor far, double z, double h, double amplitude)
        {
            var c = SceneColor.Lerp(near, far, SceneMath.SmoothStep(BaseNear, BaseFar, z));
            var k = 1 + h * amplitude * ColorGain;
            return new SceneColor(c.R * k, c.G * k, c.B * k);
        }

        // 0,128 a 5 m; 0,290 a 10 m; 0,424 a 20 m; 0,561 a 100 m (DT6b-03: sobre la normal plana).
        public static double ReflectionWeight(double z)
        {
            var c = SceneMath.Clamp01(CamHeight / Math.Sqrt(z * z + CamHeight * CamHeight));
            return FresnelGain * Math.Pow(1 - c, FresnelPower);
        }

        // S(80; 400; z): 0 a 80 m, 0,5 a 240 m y 1 a 400 m (DA6b-05: después del reflejo y del brillo).
        public static double FogWeight(double z)
        {
            return SceneMath.SmoothStep(FogNear, FogFar, z);
        }

        // lerp(col; fog; FogWeight(z)): a 400 m es exactamente el color de la niebla de la hora.
        public static SceneColor ApplyFog(in SceneColor col, in SceneColor fog, double z)
        {
            var w = FogWeight(z);
            if (w >= 1)
            {
                return fog;
            }

            return w <= 0 ? col : SceneColor.Lerp(col, fog, w);
        }

        public static double WakeIntensity(double speedMps)
        {
            if (double.IsNaN(speedMps))
            {
                return 0;
            }

            return SceneMath.Clamp(speedMps / WakeFullSpeed, 0, 1);
        }

        // El semiancho de la V a "back" metros detrás de la popa: 2,155 m a 5,3 m (pendiente 0,35 = 19,3°, el ángulo de Kelvin).
        public static double WakeHalfWidth(double backM)
        {
            return WakeHalfWidthBase + WakeHalfWidthSlope * Math.Max(0, backM);
        }

        public static double ColumnWidthDp(double z)
        {
            return ColumnBaseDp + ColumnPerMeterDp * z;
        }

        // exp(−(Δx/ancho)²)·0,4 × la fuerza de la luz del legacy (0,2-1,0 de día y 0,45 de noche).
        public static double ColumnWeight(double dxDp, double z, double lightStrength)
        {
            var d = dxDp / ColumnWidthDp(z);
            return Math.Exp(-d * d) * ColumnPeak * lightStrength;
        }

        // water.frag:57: las filas en el horizonte o medio dp debajo quedan transparentes.
        public static bool IsTransparentRow(double rowDp, double horizonDp)
        {
            return rowDp <= horizonDp + 0.5;
        }

        // La z del contrato de una fila de la pantalla (water.frag:61): focal·2,2/(y − horizonte).
        public static double RowDepth(double rowDp, double horizonDp, double focalDp)
        {
            return focalDp * CamHeight / (rowDp - horizonDp);
        }
    }
}
