using System;
using RowMate.Core.Design;
using RowMate.Core.Ports;

namespace RowMate.Simulator.Panel
{
    // Dónde queda la llave (DD4a-01): el carril, la x y el borde de abajo. Left en dp desde la izquierda de la ventana y
    // Bottom en dp desde ABAJO (como BottomObstacle); la caja mide Size × Size y no escala con hudScale (R1).
    public readonly struct WrenchSpot
    {
        public WrenchSpot(bool rightLane, double left, double bottom, bool clear)
        {
            RightLane = rightLane;
            Left = left;
            Bottom = bottom;
            Clear = clear;
        }

        // Carril derecho = el entrenamiento (con el lugar del HUD); izquierdo = el resto de la app.
        public bool RightLane { get; }

        public double Left { get; }

        public double Bottom { get; }

        // false: ninguna altura cumple (la llave subida por los obstáculos ya no entra en la ventana, o el lugar del HUD
        // cae afuera). Con el lugar del HUD, nunca en los 5 perfiles (04-design-notes §3.8, también con s = 0,9).
        public bool Clear { get; }

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
                var left = wrench.Right - SimulatorPanelLayout.Width;
                if (left < insets.Left)
                {
                    left = insets.Left;
                }

                var maxHeight = SimulatorPanelLayout.MaxHeight(windowHeight, insets, wrench.Bottom, SimulatorPanelLayout.WorkoutTopReserve,
                    surface.PanelCeiling);
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

        private static WrenchSpot Lifted(bool rightLane, double left, double bottom, double windowHeight, SafeInsets insets, OverlaySurface surface)
        {
            var lifted = BottomObstacles.Lift(left, left + Size, bottom, Size, surface.Obstacles, ObstacleGap);
            var clear = lifted + Size <= windowHeight - insets.Top + 1e-6;
            return new WrenchSpot(rightLane, left, lifted, clear);
        }
    }
}
