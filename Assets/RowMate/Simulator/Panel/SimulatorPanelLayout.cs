using RowMate.Core.Ports;

namespace RowMate.Simulator.Panel
{
    // Medidas del panel del simulador (AC-BLE-16-03), en dp. legacy: lib/core/dev/simulator_overlay.dart:40-49, :83-95.
    // It. 4a (D-ART-10, DV-39 en lugar de DV-13; DD4a-03): el lugar de la llave y del panel lo decide WrenchPlacement; acá
    // quedan el ancho y el alto máximo, que se mide desde el BORDE DE ABAJO DEL PANEL (el de la llave, donde se ancla).
    // Sale FabBottom con obstáculos (la llave ya no se aparta del + de Rutinas: está del otro lado).
    public static class SimulatorPanelLayout
    {
        // :91-92: ancho fijo.
        public const double Width = 280;

        // :93: alto máximo mínimo.
        public const double MinMaxHeight = 120;

        // :86: aire arriba (también el del entrenamiento: "deja libres el margen seguro superior y al menos 16 dp").
        public const double TopGap = 16;

        // Fuera del entrenamiento, el aire de arriba de la It. 2: la fórmula del legacy resta 96 por la llave y la barra
        // (:84) y la llave está a 80 (barra de 64 + 16, DV-07): quedan 16 de más arriba, 32 en total (spec §15: 360 → 248,
        // 914 → 802, 200 → 120; con una pantalla apilada, 64 más: 312, 866, 152).
        public const double AppTopReserve = 32;

        // En el entrenamiento: del borde de abajo de la llave hasta 16 dp debajo del margen seguro superior (04-design-notes
        // §3.9: TH 368,6; L640 269,5; W1280 653,6), salvo el techo de Watts en vertical.
        public const double WorkoutTopReserve = TopGap;

        // La barra de pestañas sin margen (DV-07) y el aire de la llave sobre ella (AC-BLE-16-02).
        public const double DefaultTabBarHeight = 64;
        public const double GapAboveTabBar = 16;

        // El alto máximo en una pestaña con la barra de siempre (64 + margen inferior), la fórmula de la It. 2 (D-26):
        // (alto − margen inferior) − 96 − margen superior − 16, mínimo 120. Sin márgenes: 360 → 248; 914 → 802; 200 → 120.
        // Con márgenes 24 / 48 (Android con barra de gestos): 914 → 730 y el borde de arriba del panel queda en 24 + 32 = 56.
        public static double MaxHeight(double windowHeightDp, SafeInsets insets)
        {
            return MaxHeight(windowHeightDp, insets, DefaultTabBarHeight + insets.Bottom + GapAboveTabBar, AppTopReserve, null);
        }

        // El alto máximo medido desde el borde de abajo del panel (DD4a-03): del borde de abajo (panelBottomDp, desde ABAJO
        // de la ventana) hasta el mayor entre margen superior + topReserveDp y el techo (ceilingFromTopDp, y desde ARRIBA,
        // el de Watts en el entrenamiento vertical), con mínimo 120. Si el contenido no entra, se desplaza adentro del panel.
        public static double MaxHeight(double windowHeightDp, SafeInsets insets, double panelBottomDp, double topReserveDp, double? ceilingFromTopDp)
        {
            var top = insets.Top + topReserveDp;
            if (ceilingFromTopDp.HasValue && ceilingFromTopDp.Value > top)
            {
                top = ceilingFromTopDp.Value;
            }

            var height = windowHeightDp - panelBottomDp - top;
            return height < MinMaxHeight ? MinMaxHeight : height;
        }

        // It. 4b (DD4b-04; 04b/00-deuda-diseno §3.2): el panel va con la llave, anclado a su borde de abajo, con el mismo resorte,
        // y su alto se calcula EN CADA CUADRO con el borde de abajo de ese cuadro (no con el del destino). Si el contenido no
        // entra, el borde de arriba queda quieto (en una pestaña o una pantalla apilada, margen superior + 32) y el panel se
        // estira o se encoge; si entra, se traslada entero. Nunca sale de la ventana ni salta. contentHeightDp: el alto que
        // pide el contenido. Devuelve el alto del panel en ese cuadro.
        public static double FrameHeight(double contentHeightDp, double windowHeightDp, SafeInsets insets, double panelBottomDp, double topReserveDp,
                                         double? ceilingFromTopDp)
        {
            var max = MaxHeight(windowHeightDp, insets, panelBottomDp, topReserveDp, ceilingFromTopDp);
            return contentHeightDp < max ? contentHeightDp : max;
        }

        // El borde de arriba del panel (dp desde ARRIBA) en ese cuadro.
        public static double FrameTop(double contentHeightDp, double windowHeightDp, SafeInsets insets, double panelBottomDp, double topReserveDp,
                                      double? ceilingFromTopDp)
        {
            return windowHeightDp - panelBottomDp - FrameHeight(contentHeightDp, windowHeightDp, insets, panelBottomDp, topReserveDp, ceilingFromTopDp);
        }
    }
}
