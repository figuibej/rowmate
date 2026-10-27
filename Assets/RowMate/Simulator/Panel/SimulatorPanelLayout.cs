using RowMate.Core.Ports;

namespace RowMate.Simulator.Panel
{
    // Medidas del panel del simulador y de su botón de la llave (AC-BLE-16-02, -03), en dp.
    // legacy: lib/core/dev/simulator_overlay.dart:40-49, :83-95
    public static class SimulatorPanelLayout
    {
        // :91-92: ancho fijo.
        public const double Width = 280;

        // :93: alto máximo mínimo.
        public const double MinMaxHeight = 120;

        // :41: a 16 dp del borde derecho.
        public const double FabRight = 16;

        // AC-BLE-16-02: 16 dp de aire sobre la barra de pestañas (legacy: 96 desde abajo sobre una barra de 80; DV-07, D-12).
        public const double GapAboveTabBar = 16;

        // :84: lo que la fórmula del legacy resta por la llave y la barra.
        public const double LegacyBottomReserve = 96;

        // :86: aire arriba.
        public const double TopGap = 16;

        // AC-BLE-16-03 (:83-86) con D-26: (alto − margen seguro inferior) − 96 − margen seguro superior − 16, mínimo 120.
        // Sin márgenes da la fórmula de la spec: 360 → 248; 914 → 802; 200 → 120. Con márgenes 24 / 48 (Android con barra
        // de gestos): 914 → 730, y el borde superior del panel queda en 24 + 32 = 56, debajo de la barra de estado.
        // El margen inferior se resta UNA vez (QA-G2-06): la barra de pestañas crece con él y el panel se ancla sobre ella.
        public static double MaxHeight(double windowHeightDp, SafeInsets insets)
        {
            var height = (windowHeightDp - insets.Bottom) - LegacyBottomReserve - insets.Top - TopGap;
            return height < MinMaxHeight ? MinMaxHeight : height;
        }

        // Distancia desde el borde inferior de la ventana hasta la llave (y el borde inferior del panel abierto):
        // tabBarHeightDp = alto real de la barra, con su margen inferior (64 + margen, 02-tech-design §3.2).
        public static double FabBottom(double tabBarHeightDp)
        {
            return tabBarHeightDp + GapAboveTabBar;
        }
    }
}
