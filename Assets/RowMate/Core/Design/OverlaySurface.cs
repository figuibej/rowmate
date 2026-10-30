using System;
using System.Collections.Generic;

namespace RowMate.Core.Design
{
    // Dónde está la pantalla de arriba, para la llave del simulador (D-ART-10; 00-deuda-diseno §1.3 R1-R5 de la It. 4a).
    // Tab: una pestaña con la barra visible; StackedPage: una pantalla apilada que tapa la barra (el editor; después, el
    // detalle y Perfil); Workout: el entrenamiento a pantalla completa (carril derecho, con el lugar que calcula el HUD).
    public enum OverlayContextKind
    {
        Tab,
        StackedPage,
        Workout,
    }

    // Lo que declara la pantalla de arriba para la llave (DD4a-01, DN4a-14; 02-tech-design §6.10). Lo guarda ShellLayout
    // (SetSurface) y manda la de arriba de la pila. Pura: la regla que la usa (WrenchPlacement) es del simulador y esta pieza
    // es de Core, así Presentation no conoce el simulador.
    // - TabBarHeight: el alto real de la barra de pestañas, con su margen seguro inferior (64 + margen; It. 2 §3.2). Solo
    //   cuenta en Tab.
    // - Obstacles: obstáculos fijos del carril de la llave fuera del entrenamiento (R3; hoy ninguno: el + de Rutinas dejó de
    //   serlo). En dp desde la izquierda y desde ABAJO (BottomObstacle).
    // - WrenchSlot: en el entrenamiento, el lugar de la llave que calcula HudGeometry (DN4a-14), en dp de ventana con y desde
    //   ARRIBA; WrenchPlacement lo usa tal cual.
    // - PanelCeiling: en el entrenamiento vertical, el borde de arriba (y desde arriba) que el panel no pasa: 8 dp debajo del
    //   lugar de Watts con su objetivo (AC-BLE-16-03). null = sin techo propio (el panel llega a 16 dp del margen superior).
    // - It. 4b (R10; DN4b-22; 02-tech-design §5.7): LeftLane, las piezas del HUD que se cruzan con el carril izquierdo (dp de
    //   ventana, y desde ARRIBA), y Dialog, el rectángulo del diálogo de completado abierto. Sin diálogo, los dos null (lo de
    //   la 4a). PanelCeiling vale también en horizontal (DN4b-23).
    public sealed record OverlaySurface(OverlayContextKind Kind, double TabBarHeight, IReadOnlyList<BottomObstacle> Obstacles,
                                        DpRect? WrenchSlot, double? PanelCeiling)
    {
        private static readonly IReadOnlyList<BottomObstacle> None = Array.Empty<BottomObstacle>();

        // Con el diálogo de completado abierto: las piezas del carril izquierdo y el diálogo.
        public IReadOnlyList<DpRect>? LeftLane { get; init; }

        public DpRect? Dialog { get; init; }

        // Una pestaña con su barra (tabBarHeight con el margen inferior incluido).
        public static OverlaySurface Tab(double tabBarHeight, IReadOnlyList<BottomObstacle>? obstacles = null)
        {
            return new OverlaySurface(OverlayContextKind.Tab, tabBarHeight, obstacles ?? None, null, null);
        }

        // Una pantalla apilada que tapa la barra (el editor).
        public static OverlaySurface Stacked(IReadOnlyList<BottomObstacle>? obstacles = null)
        {
            return new OverlaySurface(OverlayContextKind.StackedPage, 0, obstacles ?? None, null, null);
        }

        // El inmersivo, con el lugar de la llave y el techo del panel que calcula HudGeometry (HudLayout.Wrench y
        // HudLayout.PanelCeiling). It. 4b: con el diálogo de completado abierto, las piezas del carril izquierdo
        // (HudLayout.LeftLaneObstacles) y el rectángulo del diálogo (R10).
        public static OverlaySurface Workout(DpRect wrenchSlot, double? panelCeiling, IReadOnlyList<DpRect>? leftLane = null, DpRect? dialog = null)
        {
            return new OverlaySurface(OverlayContextKind.Workout, 0, None, wrenchSlot, panelCeiling) { LeftLane = leftLane, Dialog = dialog };
        }
    }
}
