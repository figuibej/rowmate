using System.Collections.Generic;
using RowMate.Core.Design;
using RowMate.Core.Ports;

namespace RowMate.Simulator.Panel
{
    // Medidas del panel del simulador y de su botón de la llave (AC-BLE-16-02, -03), en dp.
    // legacy: lib/core/dev/simulator_overlay.dart:40-49, :83-95
    // It. 3 (DV-13, DN3-14; AC-BLE-16-02 con el texto nuevo): la llave se aparta de los obstáculos inferiores que se cruzan
    // con ella (el + de Rutinas, mientras Rutinas es la pestaña visible y ninguna página la tapa) y queda 16 dp por encima,
    // con el borde derecho alineado (x fija). El panel se abre SIEMPRE en el mismo lugar: barra + 16 (AC-BLE-16-03).
    public static class SimulatorPanelLayout
    {
        // :91-92: ancho fijo.
        public const double Width = 280;

        // :93: alto máximo mínimo.
        public const double MinMaxHeight = 120;

        // :41: a 16 dp del borde derecho.
        public const double FabRight = 16;

        // FloatingActionButton.small (:44-48): 40 × 40.
        public const double FabSize = 40;

        // AC-BLE-16-02: 16 dp de aire sobre la barra de pestañas (legacy: 96 desde abajo sobre una barra de 80; DV-07, D-12).
        public const double GapAboveTabBar = 16;

        // DV-13: 16 dp de aire sobre el obstáculo que se cruza con la llave (en Rutinas, barra + 16 + 56 + 16 = barra + 88).
        public const double ObstacleGapDp = 16;

        // :84: lo que la fórmula del legacy resta por la llave y la barra.
        public const double LegacyBottomReserve = 96;

        // :86: aire arriba.
        public const double TopGap = 16;

        private static readonly IReadOnlyList<BottomObstacle> NoObstacles = new BottomObstacle[0];

        // AC-BLE-16-03 (:83-86) con D-26: (alto − margen seguro inferior) − 96 − margen seguro superior − 16, mínimo 120.
        // Sin márgenes da la fórmula de la spec: 360 → 248; 914 → 802; 200 → 120. Con márgenes 24 / 48 (Android con barra
        // de gestos): 914 → 730, y el borde superior del panel queda en 24 + 32 = 56, debajo de la barra de estado.
        // El margen inferior se resta UNA vez (QA-G2-06): la barra de pestañas crece con él y el panel se ancla sobre ella.
        public static double MaxHeight(double windowHeightDp, SafeInsets insets)
        {
            var height = (windowHeightDp - insets.Bottom) - LegacyBottomReserve - insets.Top - TopGap;
            return height < MinMaxHeight ? MinMaxHeight : height;
        }

        // Distancia desde el borde inferior de la ventana hasta la llave, sin obstáculos: tabBarHeightDp = alto real de la
        // barra, con su margen inferior (64 + margen, 02-tech-design §3.2 de la It. 2; con la barra oculta por el editor, el
        // último alto visible).
        public static double FabBottom(double tabBarHeightDp)
        {
            return tabBarHeightDp + GapAboveTabBar;
        }

        // Con obstáculos (DV-13): la llave (x = [ancho − 16 − 40; ancho − 16], 40 de alto) arranca a barra + 16 y sube 16 dp
        // por encima de cada obstáculo que se cruza con ella agrandado 16 (BottomObstacles.Lift). En Rutinas, sobre el +
        // (56 a barra + 16): barra + 88; sin obstáculos, barra + 16. Los obstáculos van en dp desde el borde izquierdo y desde
        // abajo de la ventana.
        public static double FabBottom(double windowWidthDp, double tabBarHeightDp, IReadOnlyList<BottomObstacle>? obstacles)
        {
            var right = windowWidthDp - FabRight;
            return BottomObstacles.Lift(right - FabSize, right, FabBottom(tabBarHeightDp), FabSize, obstacles ?? NoObstacles, ObstacleGapDp);
        }

        // El rectángulo de la llave como obstáculo para el aviso y para el relleno inferior del contenido (DN3-13, DN3-14).
        public static BottomObstacle FabObstacle(double windowWidthDp, double fabBottomDp)
        {
            var right = windowWidthDp - FabRight;
            return new BottomObstacle(right - FabSize, right, fabBottomDp, FabSize);
        }

        // Borde inferior del panel abierto: barra + 16 SIEMPRE, en todas las pestañas (el panel no sigue a la llave; en
        // Rutinas tapa el +, como en el legacy, y mientras está abierto la llave no se ve).
        public static double PanelBottom(double tabBarHeightDp)
        {
            return tabBarHeightDp + GapAboveTabBar;
        }
    }
}
