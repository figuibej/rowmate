using System;

namespace RowMate.Core.Scene
{
    // La luz y la niebla de la orilla (shore_painter.dart:26, :44-50; F-SCN-20; AC-SCN-20-01). Vale para todo objeto de la
    // orilla, también para lo agregado. Las luces de la noche no pasan por acá (AC-SCN-09-06).
    public static class ShoreShading
    {
        // :47: rojo y verde × ambiente, azul × mín(1; ambiente + 0,12).
        public static SceneColor Tint(in SceneColor c, double ambient)
        {
            return new SceneColor(c.R * ambient, c.G * ambient, c.B * Math.Min(1.0, ambient + 0.12));
        }

        // :48-49: clamp((relZ − 80)/320; 0; 1)·0,8 (a 240 m, 40 %; 80 % desde 400 m).
        public static double Fog(double relZ)
        {
            return SceneTimeOfDay.Clamp((relZ - 80) / 320, 0.0, 1.0) * 0.8;
        }

        // :45-50: Color.lerp(tinte; fog; Fog(relZ)).
        public static SceneColor Shade(in SceneColor c, double relZ, in ScenePalette p)
        {
            return SceneColor.Lerp(Tint(c, p.Ambient), p.Fog, Fog(relZ));
        }

        // La de 00-deuda-diseno §6.5 para el terreno lejano: igual hasta 400 m y hasta 1 en 1 200 m.
        public static double ExtendedFog(double relZ)
        {
            return 0.8 * SceneMath.Clamp01((relZ - 80) / 320) + 0.2 * SceneMath.Clamp01((relZ - 400) / 800);
        }
    }

    // Una línea de brillo del agua degradada (water_painter.dart:92-103): su t, la y de pantalla, el medio ancho, el centro y el
    // alfa del legacy.
    public readonly struct ShimmerLine
    {
        public ShimmerLine(double t, double y, double halfWidth, double centerX, double alpha)
        {
            T = t;
            Y = y;
            HalfWidth = halfWidth;
            CenterX = centerX;
            Alpha = alpha;
        }

        public double T { get; }
        public double Y { get; }
        public double HalfWidth { get; }
        public double CenterX { get; }
        public double Alpha { get; }
    }

    // El agua degradada (water_painter.dart:79-103; F-SCN-13; AC-SCN-13-02, -03): los tres tramos y las 10 líneas del legacy, y
    // la fase en double que recibe el shader (Q-68).
    public static class DegradedWater
    {
        public const int LineCount = 10;

        // :79-90: de arriba a abajo lerp(waterFar; fog; 0,5), waterFar y waterNear (el lago no tiñe el agua, environment.dart:63).
        public static void Bands(in ScenePalette p, out SceneColor top, out SceneColor middle, out SceneColor bottom)
        {
            top = SceneColor.Lerp(p.WaterFar, p.Fog, 0.5);
            middle = p.WaterFar;
            bottom = p.WaterNear;
        }

        // It. 8 (AC-SCN-17-01): con el tinte del escenario en el orden del legacy (:80-88): near y far teñidos, y la franja de arriba
        // lerp(far teñido; fog; 0,5). En el lago, el río y la regata (sin tinte) da lo mismo que Bands(p).
        public static void Bands(in ScenePalette p, ShoreEnvironment environment, out SceneColor top, out SceneColor middle, out SceneColor bottom)
        {
            if (environment == null)
            {
                throw new ArgumentNullException(nameof(environment));
            }

            var near = environment.TintWater(p.WaterNear);
            var far = environment.TintWater(p.WaterFar);
            top = SceneColor.Lerp(far, p.Fog, 0.5);
            middle = far;
            bottom = near;
        }

        // El alto del rectángulo del agua del legacy: Rect.fromLTWH(0; horizonte; ancho; alto − horizonte).height
        // = (horizonte + (alto − horizonte)) − horizonte, como lo calcula Flutter.
        public static double WaterHeight(in SceneCamera cam)
        {
            var bottom = cam.HorizonY + (cam.Height - cam.HorizonY);
            return bottom - cam.HorizonY;
        }

        // :92-103: t = Mod(i/10 + Mod(d·0,02; 0,1); 1), y = horizonte + altoAgua·t², medio ancho ancho·(0,15 + 0,35·t), alfa
        // 0,05 + 0,1·(1 − t)·fuerza (la fuerza del legacy). El centro lo pone quien llama: la x del astro que se ve (DV-59).
        public static ShimmerLine Line(int i, double distance, double width, double horizonY, double waterHeight, double centerX, double strength)
        {
            var t = Text.DartMath.Mod(i / 10.0 + Text.DartMath.Mod(distance * 0.02, 0.1), 1.0);
            var y = horizonY + waterHeight * t * t;
            var halfWidth = width * (0.15 + 0.35 * t);
            var alpha = 0.05 + 0.1 * (1 - t) * strength;
            return new ShimmerLine(t, y, halfWidth, centerX, alpha);
        }

        // Mod(d·0,02; 0,1) en double: lo que recibe el shader (sin saltos a 4 096 m ni a 42 195 m).
        public static double Phase(double distance)
        {
            return Text.DartMath.Mod(distance * 0.02, 0.1);
        }

        // DV-59 (DN6a-06): lo que se ve = el alfa del legacy × la visibilidad del astro que se ve (0 en el cambio de astro).
        public static double ScreenAlpha(double legacyAlpha, double diskVisibility)
        {
            return legacyAlpha * diskVisibility;
        }

        // La x de pantalla del astro que se ve con la cámara del contrato: centro + focal·tan(acimut).
        public static double DiskScreenX(in SceneCamera cam, double azimuthDeg)
        {
            return cam.CenterX + cam.Focal * Math.Tan(SceneMath.DegToRad(azimuthDeg));
        }
    }
}
