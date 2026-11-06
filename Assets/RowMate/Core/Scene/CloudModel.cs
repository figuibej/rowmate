using System;
using RowMate.Core.Text;

namespace RowMate.Core.Scene
{
    // Una nube del legacy (sky_painter.dart:29-33): x inicial, y relativa (fracciones de pantalla), tamaño en dp y velocidad.
    public readonly struct CloudSpec
    {
        public CloudSpec(double x0, double yRel, double sizeDp, double speed)
        {
            X0 = x0;
            YRel = yRel;
            SizeDp = sizeDp;
            Speed = speed;
        }

        public double X0 { get; }

        public double YRel { get; }

        public double SizeDp { get; }

        public double Speed { get; }
    }

    // Las 7 nubes (06a-escena-lago/02-tech-design §2.8; AC-SCN-11-06; D-ART-06, DV-56).
    public static class CloudModel
    {
        public const int Count = 7;

        private static readonly CloudSpec[] Specs =
        {
            new CloudSpec(0.08, 0.10, 34.0, 1.0), new CloudSpec(0.26, 0.18, 22.0, 1.4), new CloudSpec(0.42, 0.07, 40.0, 0.8),
            new CloudSpec(0.58, 0.22, 26.0, 1.2), new CloudSpec(0.72, 0.12, 36.0, 0.9), new CloudSpec(0.88, 0.20, 24.0, 1.3),
            new CloudSpec(0.97, 0.05, 30.0, 1.1),
        };

        public static ReadOnlySpan<CloudSpec> Legacy => Specs;

        // sky_painter.dart:95 (contrato): la x del legacy con la deriva por distancia.
        public static double LegacyX(int i, double width, double distance, double time)
        {
            var c = Specs[i];
            return DartMath.Mod(c.X0 + distance * 0.0004 * c.Speed + time * 0.004 * c.Speed, 1.2) * width - width * 0.1;
        }

        // D-ART-06 (DV-56): solo con el viento, ((x₀ + t·0,004·v) mod 1,2)·ancho − 0,1·ancho. Es LegacyX con la distancia en 0.
        public static double X(int i, double width, double time)
        {
            return LegacyX(i, width, 0, time);
        }

        // La fase de la vuelta, (x₀ + t·0,004·v) mod 1,2.
        public static double Phase(int i, double time)
        {
            var c = Specs[i];
            return DartMath.Mod(c.X0 + 0 * 0.0004 * c.Speed + time * 0.004 * c.Speed, 1.2);
        }

        // :96: horizonte·(0,12 + y·0,6).
        public static double Y(int i, double horizonY)
        {
            return horizonY * (0.12 + Specs[i].YRel * 0.6);
        }

        // DV-56 (QA-6a-PAR-08): el factor del alfa cerca de la vuelta, sin tocar la x:
        // S(0; kR·s; 1,1·ancho − X)·S(0; kL·s; X + 0,1·ancho); 0 en el punto de vuelta por los dos lados.
        public static double WrapFade(int i, double width, double time, double kRight, double kLeft)
        {
            var s = Specs[i].SizeDp;
            var x = X(i, width, time);
            return SceneMath.SmoothStep(0, kRight * s, 1.1 * width - x) * SceneMath.SmoothStep(0, kLeft * s, x + 0.1 * width);
        }

        // :91-93
        public static double BodyAlpha(double ambient)
        {
            return 0.08 + 0.5 * ambient * ambient;
        }

        public static SceneColor BodyColor(in ScenePalette p)
        {
            return SceneColor.Lerp(SceneColor.White, p.SkyHorizon, 0.25);
        }

        public static double ShadeAlpha(double ambient)
        {
            return 0.12 * ambient;
        }
    }

    // La curva del telón del cielo que copia el shader Sky (DV-66, DN6a-09, QA-6a-PAR-10): q = clamp(τ/τ_top; 0; 1) con
    // τ_top = HorizonY/Focal, y 1 − (1 − q)²: 0 → skyHorizon, 1 → skyTop, mezclado en sRGB como Color.lerp.
    public static class SkyBackdrop
    {
        public static double Fraction(double tau, double tauTop)
        {
            return SceneMath.Clamp01(tau / tauTop);
        }

        public static double Gradient(double q)
        {
            var r = 1 - SceneMath.Clamp01(q);
            return 1 - r * r;
        }

        public static SceneColor Color(in ScenePalette p, double q)
        {
            return SceneColor.Lerp(p.SkyHorizon, p.SkyTop, Gradient(q));
        }
    }

    // Lo que tapa el astro a las estrellas, que copia el shader Stars (QA-G6aT-08, DT6a-17): 1 fuera del halo; 1 − el alfa del
    // halo adentro (0,7 / 0,25 / 0 del legacy en 0, la mitad y el borde, × haloAlphaScale: la visibilidad y, la luna,
    // × (0,4 + 0,6·iluminación)); 0 adentro del disco (también del lado oscuro de la luna), con el borde suave del disco.
    public static class CelestialMask
    {
        public static double StarAttenuation(double angleToDiskRad, double diskRadiusRad, double haloRadiusRad, double haloAlphaScale)
        {
            if (angleToDiskRad >= haloRadiusRad)
            {
                return 1;
            }

            var edge = SceneMath.DegToRad(Art.SkyArt.DiskEdgeDeg);
            var outsideDisk = SceneMath.SmoothStep(diskRadiusRad - edge, diskRadiusRad, angleToDiskRad);
            return outsideDisk * (1 - HaloAlpha(angleToDiskRad / haloRadiusRad) * haloAlphaScale);
        }

        // El alfa del halo del legacy en la fracción r del radio (RadialGradient con 3 colores: paradas 0, 0,5 y 1).
        public static double HaloAlpha(double r)
        {
            var a = Art.SkyArt.HaloAlphas;
            if (r <= 0)
            {
                return a[0];
            }

            if (r >= 1)
            {
                return a[2];
            }

            return r < 0.5 ? a[0] + (a[1] - a[0]) * (r / 0.5) : a[1] + (a[2] - a[1]) * ((r - 0.5) / 0.5);
        }
    }
}
