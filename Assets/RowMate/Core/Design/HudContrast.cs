using System;
using RowMate.Core.Workout;

namespace RowMate.Core.Design
{
    // El contraste de las píldoras del HUD sobre la escena (06a-escena-lago/02-tech-design §2.18; AC-WKT-17-13; DD6a-18, P6a-01):
    // con el método de Contrast (composición sRGB sin redondear) contra el peor vidrio, glass.fallback sobre blanco (#2A2F37), y
    // contra el peor vidrio renderizado (redondeado al byte + 1 por canal, #2B3038). La hecha es el tipo al 85 %; el relleno, el
    // tipo pleno; el borde de la hueca, el tipo al 85 % sobre su interior al 18 %. En el HUD van con su variante .hud
    // (step.work.hud, step.cooldown.hud, step.unknown.hud). El borde de la hueca contra su propio interior al 18 % (2,93 a 3,02)
    // es una excepción contada, como la pista al 30 % (00-deuda-diseno §9.3).
    public static class HudContrast
    {
        public const double GraphicsThreshold = 3.0;

        public static (double R, double G, double B) RenderedWorstGlass()
        {
            var g = Contrast.WorstGlassBackground();
            return (Math.Floor(g.R + 0.5) + 1, Math.Floor(g.G + 0.5) + 1, Math.Floor(g.B + 0.5) + 1);
        }

        public static double Done(ColorToken step, bool rendered)
        {
            return Against(step, StepPills.DoneAlpha, rendered ? RenderedWorstGlass() : Contrast.WorstGlassBackground());
        }

        public static double Fill(ColorToken step, bool rendered)
        {
            return Against(step, 1, rendered ? RenderedWorstGlass() : Contrast.WorstGlassBackground());
        }

        // El borde (al 85 %) sobre el interior de la hueca (al 18 %), contra el vidrio de afuera.
        public static double HollowBorder(ColorToken step)
        {
            var glass = Contrast.WorstGlassBackground();
            var interior = Over(step, StepPills.PendingAlpha, glass);
            var border = Over(step, StepPills.DoneAlpha, interior);
            return Ratio(border, glass);
        }

        // La excepción contada: el borde contra su propio interior.
        public static double HollowBorderOverInterior(ColorToken step)
        {
            var interior = Over(step, StepPills.PendingAlpha, Contrast.WorstGlassBackground());
            return Ratio(Over(step, StepPills.DoneAlpha, interior), interior);
        }

        private static double Against(ColorToken step, double alpha, (double R, double G, double B) background)
        {
            return Ratio(Over(step, alpha, background), background);
        }

        private static (double R, double G, double B) Over(ColorToken step, double alpha, (double R, double G, double B) under)
        {
            if (step == null)
            {
                throw new ArgumentNullException(nameof(step));
            }

            var a = alpha * step.Alpha;
            return (a * step.Rgb.R + (1 - a) * under.R, a * step.Rgb.G + (1 - a) * under.G, a * step.Rgb.B + (1 - a) * under.B);
        }

        private static double Ratio((double R, double G, double B) a, (double R, double G, double B) b)
        {
            return Contrast.Ratio(Contrast.RelativeLuminance(a.R, a.G, a.B), Contrast.RelativeLuminance(b.R, b.G, b.B));
        }
    }
}
