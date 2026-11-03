using System;
using RowMate.Core.Design;
using RowMate.Core.Ports;

namespace RowMate.Simulator.Panel
{
    // Dónde queda la llave (DD4a-01): el carril, la x y el borde de abajo. Left en dp desde la izquierda de la ventana y
    // Bottom en dp desde ABAJO (como BottomObstacle); la caja mide Size × Size y no escala con hudScale (R1).
    public readonly struct WrenchSpot
    {
        public WrenchSpot(bool rightLane, double left, double bottom, bool clear, bool overSeries = false)
        {
            RightLane = rightLane;
            Left = left;
            Bottom = bottom;
            Clear = clear;
            OverSeries = overSeries;
        }

        // Carril derecho = el entrenamiento (con el lugar del HUD); izquierdo = el resto de la app.
        public bool RightLane { get; }

        public double Left { get; }

        public double Bottom { get; }

        // false: ninguna altura cumple (la llave subida por los obstáculos ya no entra en la ventana, o el lugar del HUD
        // cae afuera). Con el lugar del HUD, nunca en los 5 perfiles (04-design-notes §3.8, también con s = 0,9).
        public bool Clear { get; }

        // It. 5 (R10 con los paneles): con el diálogo de completado no hubo hueco que no tocara la columna de los paneles de series
        // (L640: el diálogo llega hasta la columna) y la llave quedó en su lugar de la 4b, sobre un panel inerte debajo del velo.
        public bool OverSeries { get; }

        public double Right => Left + WrenchPlacement.Size;

        // Borde de arriba, medido desde abajo.
        public double Top => Bottom + WrenchPlacement.Size;

        // La caja como obstáculo para el aviso y para el relleno de lo que se desplaza (R5, RegisterClearance).
        public BottomObstacle Obstacle => new BottomObstacle(Left, Right, Bottom, WrenchPlacement.Size);

        // La caja en dp de ventana con y desde ARRIBA (la convención de 04-design-notes §3.8 y del arnés).
        public DpRect RectIn(double windowHeight)
        {
            return DpRect.FromEdges(Left, windowHeight - Top, Right, windowHeight - Bottom);
        }
    }

    // La esquina de la llave desde la que crece el panel (R9, DD4a-03).
    public enum PanelCorner
    {
        // Fuera del entrenamiento: crece hacia arriba y a la derecha.
        BottomLeft,

        // En el entrenamiento: crece hacia arriba y a la izquierda.
        BottomRight,
    }

    // Dónde se abre el panel: la esquina, el borde izquierdo del panel (dp desde la izquierda), su borde de abajo (dp desde
    // abajo, el de la llave) y el alto máximo medido desde ese borde (AC-BLE-16-03).
    public readonly struct PanelAnchor
    {
        public PanelAnchor(PanelCorner corner, double left, double bottom, double maxHeight)
        {
            Corner = corner;
            Left = left;
            Bottom = bottom;
            MaxHeight = maxHeight;
        }

        public PanelCorner Corner { get; }

        public double Left { get; }

        public double Bottom { get; }

        public double MaxHeight { get; }

        public double Width => SimulatorPanelLayout.Width;

        public double Right => Left + SimulatorPanelLayout.Width;

        public bool GrowsRight => Corner == PanelCorner.BottomLeft;
    }

    // La regla de D-ART-10 (00-deuda-diseno §1.3, R1 a R5 y R9 de la It. 4a), con el entrenamiento de 04-design-notes §3.9
    // (DV-39 en lugar de DV-13; DN4a-14). Pura y sin estado: SimulatorLayerView la llama con la superficie de la pantalla de
    // arriba (ShellLayout.SetSurface) cada vez que cambian la ventana, los márgenes o la superficie.
    // - R1, carril: izquierdo (16 + margen izquierdo) en pestañas, pantallas apiladas, diálogos y menús; en el entrenamiento,
    //   el lugar que declara el HUD (OverlaySurface.WrenchSlot), tal cual.
    // - R2, altura: pestaña, barra visible + 16; pantalla apilada, 16 + margen inferior.
    // - R3, obstáculos (fuera del entrenamiento): sube hasta 8 dp sobre cada obstáculo que se cruce con su caja agrandada 8
    //   (BottomObstacles.Lift). Hoy no hay ninguno en el carril izquierdo.
    // - R9, panel: se abre donde está la llave y crece desde su esquina (PanelFor).
    // legacy: lib/core/dev/simulator_overlay.dart:28-50 (right 16, bottom 96 en toda la app; DV-39).
    public static class WrenchPlacement
    {
        public const double Size = 40;
        public const double EdgeMargin = 16;
        public const double ObstacleGap = 8;

        public static WrenchSpot For(double windowWidth, double windowHeight, SafeInsets insets, OverlaySurface surface)
        {
            if (surface == null)
            {
                throw new ArgumentNullException(nameof(surface));
            }

            if (surface.Kind == OverlayContextKind.Workout && surface.WrenchSlot.HasValue)
            {
                // DN4a-14: el lugar del HUD, tal cual (HudGeometry ya lo dejó libre de toda pieza del HUD).
                var slot = surface.WrenchSlot.Value;
                if (surface.Dialog.HasValue && slot.Intersects(surface.Dialog.Value, ObstacleGap))
                {
                    // It. 4b (R10; DN4b-22): con el diálogo de completado encima de su lugar, el hueco libre más bajo del carril
                    // izquierdo. It. 5: sin tocar la columna de los paneles de series.
                    return LeftLaneAvoiding(windowWidth, windowHeight, insets, surface);
                }

                var bottom = windowHeight - slot.Bottom;
                return new WrenchSpot(true, slot.Left, bottom, slot.IsInside(windowWidth, windowHeight));
            }

            if (surface.Kind == OverlayContextKind.Workout)
            {
                // Sin el lugar del HUD (no pasa en la app: la página lo declara al armarse): el rincón de abajo a la derecha,
                // con los obstáculos que haya.
                var right = windowWidth - EdgeMargin - insets.Right;
                return Lifted(true, right - Size, EdgeMargin + insets.Bottom, windowHeight, insets, surface);
            }

            var left = EdgeMargin + insets.Left;
            var baseBottom = surface.Kind == OverlayContextKind.Tab
                ? surface.TabBarHeight + EdgeMargin
                : EdgeMargin + insets.Bottom;
            return Lifted(false, left, baseBottom, windowHeight, insets, surface);
        }

        // R9 (DD4a-03; AC-BLE-16-03): el panel crece desde la esquina de abajo de la llave hacia adentro de la ventana. Fuera
        // del entrenamiento, desde la de la izquierda, con el aire de arriba de la It. 2 (SimulatorPanelLayout.AppTopReserve);
        // en el entrenamiento, desde la de la derecha, hasta 16 dp debajo del margen superior o hasta PanelCeiling (el de
        // Watts, en vertical). En una ventana más angosta que el panel, el panel no sale por la izquierda.
        public static PanelAnchor PanelFor(double windowWidth, double windowHeight, SafeInsets insets, WrenchSpot wrench, OverlaySurface surface)
        {
            if (surface == null)
            {
                throw new ArgumentNullException(nameof(surface));
            }

            if (surface.Kind == OverlayContextKind.Workout)
            {
                var maxHeight = SimulatorPanelLayout.MaxHeight(windowHeight, insets, wrench.Bottom, SimulatorPanelLayout.WorkoutTopReserve,
                    surface.PanelCeiling);
                if (!wrench.RightLane)
                {
                    // R10: la llave en el carril izquierdo (con el diálogo de completado): el panel crece hacia la derecha.
                    var leftLane = wrench.Left;
                    if (leftLane + SimulatorPanelLayout.Width > windowWidth - insets.Right && windowWidth - insets.Right - SimulatorPanelLayout.Width >= insets.Left)
                    {
                        leftLane = windowWidth - insets.Right - SimulatorPanelLayout.Width;
                    }

                    return new PanelAnchor(PanelCorner.BottomLeft, leftLane, wrench.Bottom, maxHeight);
                }

                var left = wrench.Right - SimulatorPanelLayout.Width;
                if (left < insets.Left)
                {
                    left = insets.Left;
                }

                return new PanelAnchor(PanelCorner.BottomRight, left, wrench.Bottom, maxHeight);
            }

            var appLeft = wrench.Left;
            if (appLeft + SimulatorPanelLayout.Width > windowWidth - insets.Right && windowWidth - insets.Right - SimulatorPanelLayout.Width >= insets.Left)
            {
                appLeft = windowWidth - insets.Right - SimulatorPanelLayout.Width;
            }

            var appMax = SimulatorPanelLayout.MaxHeight(windowHeight, insets, wrench.Bottom, SimulatorPanelLayout.AppTopReserve, null);
            return new PanelAnchor(PanelCorner.BottomLeft, appLeft, wrench.Bottom, appMax);
        }

        // R10 (DN4b-22; 02-tech-design §5.7 de la It. 4b): el y más bajo del carril izquierdo (x 16-56 desde el margen izquierdo)
        // donde la caja de 40, agrandada 8, no toca ninguna pieza del HUD que se cruza con el carril (siguen a la vista debajo
        // del velo) ni el diálogo, dentro del área segura. Desde 16 + margen inferior, sube hasta 8 dp sobre cada pieza que
        // toca. Si no hay hueco (no pasa en los cinco perfiles), "16 + margen inferior" con Clear = false (la vista lo anota).
        // It. 5 (AC-BLE-16-02, "tampoco tapa los paneles"; 05-series-parciales/02-tech-design, cambios de WP-B): con la columna
        // de los paneles de series (OverlaySurface.Series), primero el carril de siempre sin tocarla (TV y P360: debajo del
        // diálogo, como en la 4b); si no hay hueco (TH: la columna, SPM y la barra llenan el carril), el carril pegado a la derecha
        // de la columna, a 8 dp (redondeado hacia arriba a 0,1 dp); si tampoco (L640: el diálogo llega hasta la columna, no hay
        // ningún hueco libre en la ventana), el lugar de la 4b, sobre un panel inerte debajo del velo (OverSeries).
        private static WrenchSpot LeftLaneAvoiding(double windowWidth, double windowHeight, SafeInsets insets, OverlaySurface surface)
        {
            var left = EdgeMargin + insets.Left;
            var lowest = EdgeMargin + insets.Bottom;
            var series = surface.Series;
            if (series.HasValue)
            {
                var first = Lowest(left, windowWidth, windowHeight, insets, surface, series);
                if (first.HasValue)
                {
                    return first.Value;
                }

                var beside = Math.Ceiling((series.Value.Right + ObstacleGap) * 10) / 10;
                var second = Lowest(beside, windowWidth, windowHeight, insets, surface, series);
                if (second.HasValue)
                {
                    return second.Value;
                }
            }

            var spot = Lowest(left, windowWidth, windowHeight, insets, surface, null);
            if (!spot.HasValue)
            {
                return new WrenchSpot(false, left, lowest, false);
            }

            var over = series.HasValue && spot.Value.RectIn(windowHeight).Intersects(series.Value, ObstacleGap);
            return new WrenchSpot(false, spot.Value.Left, spot.Value.Bottom, true, over);
        }

        // El hueco más bajo de un carril de x left: desde 16 + margen inferior, sube hasta 8 dp sobre cada pieza del carril, el
        // diálogo y la columna (si se pasa) que toca la caja agrandada 8. null si no hay (la caja pasa el margen de arriba o sale
        // por la derecha).
        private static WrenchSpot? Lowest(double left, double windowWidth, double windowHeight, SafeInsets insets, OverlaySurface surface, DpRect? series)
        {
            if (left + Size > windowWidth - insets.Right + 1e-6)
            {
                return null;
            }

            var bottomFromTop = windowHeight - (EdgeMargin + insets.Bottom);
            var obstacles = surface.LeftLane;
            var dialog = surface.Dialog;
            var limit = (obstacles?.Count ?? 0) + 3;
            for (var guard = 0; guard <= limit; guard++)
            {
                var box = DpRect.FromEdges(left, bottomFromTop - Size, left + Size, bottomFromTop);
                if (box.Top < insets.Top - 1e-6)
                {
                    return null;
                }

                var moved = false;
                var next = bottomFromTop;
                if (obstacles != null)
                {
                    foreach (var piece in obstacles)
                    {
                        if (box.Intersects(piece, ObstacleGap) && piece.Top - ObstacleGap < next)
                        {
                            next = piece.Top - ObstacleGap;
                            moved = true;
                        }
                    }
                }

                if (dialog.HasValue && box.Intersects(dialog.Value, ObstacleGap) && dialog.Value.Top - ObstacleGap < next)
                {
                    next = dialog.Value.Top - ObstacleGap;
                    moved = true;
                }

                if (series.HasValue && box.Intersects(series.Value, ObstacleGap) && series.Value.Top - ObstacleGap < next)
                {
                    next = series.Value.Top - ObstacleGap;
                    moved = true;
                }

                if (!moved)
                {
                    return new WrenchSpot(false, left, windowHeight - bottomFromTop, true);
                }

                bottomFromTop = next;
            }

            return null;
        }

        private static WrenchSpot Lifted(bool rightLane, double left, double bottom, double windowHeight, SafeInsets insets, OverlaySurface surface)
        {
            var lifted = BottomObstacles.Lift(left, left + Size, bottom, Size, surface.Obstacles, ObstacleGap);
            var clear = lifted + Size <= windowHeight - insets.Top + 1e-6;
            return new WrenchSpot(rightLane, left, lifted, clear);
        }
    }
}
