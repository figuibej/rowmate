using System;
using System.Collections.Generic;
using RowMate.Core.Text;

namespace RowMate.Core.Common
{
    // Un monitor actual: su área de trabajo en px de pantalla y su escala (DPI / 96).
    public readonly struct MonitorArea
    {
        public MonitorArea(WindowRect workArea, double scale)
        {
            WorkArea = workArea;
            Scale = scale > 0 ? scale : 1;
        }

        public WindowRect WorkArea { get; }

        public double Scale { get; }
    }

    // La ventana guardada al cerrar (It. 15, #74, AC-D15-19, DV-145; DT15-08), en dp LÓGICOS enteros (px / la escala del monitor de la
    // ventana): el rectángulo EXTERIOR en estado normal (rcNormalPosition de GetWindowPlacement) y si estaba maximizada.
    public sealed record SavedWindow(int X, int Y, int Width, int Height, bool Maximized);

    // Lo que hay que hacer al abrir: el rectángulo exterior en px, el monitor elegido y si se maximiza.
    public sealed record WindowChoice(WindowRect Rect, int MonitorIndex, bool Maximized);

    // La regla pura de "la ventana se recuerda" (It. 15, #74, AC-D15-19, DV-145, DV-129, DV-130; DT15-08):
    // - Capture: al cerrar se guarda la ventana NORMAL en dp (px / escala del monitor de la ventana). En pantalla completa se guarda la
    //   ventana de antes (WindowModeState.Restore) y sin maximizar: la pantalla completa no se recuerda (DV-129).
    // - Choose: al abrir se prueba cada monitor en orden (px = dp × su escala) y se usa el primero donde se ve al menos el 50 % del
    //   rectángulo. El contenido nunca queda por debajo de 640 × 360 lógicos (se agranda, sin moverlo). Sin guardado, con datos
    //   inválidos o sin un monitor donde se vea, null: el llamador abre centrada en 1280 × 720 (WindowGeometry.Initial, DV-130).
    // - Con -screen-* en la línea de comandos no se restaura nada (WindowGeometry.ShouldPlaceInitial, lo mira el llamador).
    public static class WindowPlacement
    {
        public const double MinVisibleFraction = 0.5;

        // px → dp lógicos enteros (redondeo de Dart: medio se aleja del cero).
        public static SavedWindow Capture(WindowRect normalPx, double scale, bool maximized, bool fullscreen, WindowRect restorePx)
        {
            var source = fullscreen ? restorePx : normalPx;
            var logical = ToLogical(source, scale);
            return new SavedWindow(logical.X, logical.Y, logical.Width, logical.Height, !fullscreen && maximized);
        }

        public static WindowRect ToLogical(WindowRect px, double scale)
        {
            var s = scale > 0 ? scale : 1;
            return new WindowRect(Round(px.X / s), Round(px.Y / s), Round(px.Width / s), Round(px.Height / s));
        }

        public static WindowRect ToPixels(SavedWindow dp, double scale)
        {
            var s = scale > 0 ? scale : 1;
            return new WindowRect(Round(dp.X * s), Round(dp.Y * s), Round(dp.Width * s), Round(dp.Height * s));
        }

        public static WindowChoice? Choose(SavedWindow? saved, IReadOnlyList<MonitorArea> monitors, WindowFrameEdges frame = default)
        {
            if (saved == null || monitors == null || saved.Width <= 0 || saved.Height <= 0)
            {
                return null;
            }

            for (var i = 0; i < monitors.Count; i++)
            {
                var monitor = monitors[i];
                var px = ToPixels(saved, monitor.Scale);
                var minWidth = Round(WindowGeometry.MinWidth * monitor.Scale) + frame.Width;
                var minHeight = Round(WindowGeometry.MinHeight * monitor.Scale) + frame.Height;
                var rect = new WindowRect(px.X, px.Y, Math.Max(px.Width, minWidth), Math.Max(px.Height, minHeight));
                if (VisibleFraction(rect, monitor.WorkArea) >= MinVisibleFraction)
                {
                    return new WindowChoice(rect, i, saved.Maximized);
                }
            }

            return null;
        }

        // Qué parte del rectángulo cae dentro del área (0 a 1).
        public static double VisibleFraction(WindowRect rect, WindowRect area)
        {
            if (rect.Width <= 0 || rect.Height <= 0)
            {
                return 0;
            }

            var left = Math.Max((long)rect.X, area.X);
            var top = Math.Max((long)rect.Y, area.Y);
            var right = Math.Min((long)rect.X + rect.Width, (long)area.X + area.Width);
            var bottom = Math.Min((long)rect.Y + rect.Height, (long)area.Y + area.Height);
            if (right <= left || bottom <= top)
            {
                return 0;
            }

            return (double)(right - left) * (bottom - top) / ((double)rect.Width * rect.Height);
        }

        private static int Round(double value)
        {
            var rounded = DartMath.Round(value);
            return rounded > int.MaxValue ? int.MaxValue : rounded < int.MinValue ? int.MinValue : (int)rounded;
        }
    }
}
