using RowMate.App.Composition;
using RowMate.Core.Common;
using RowMate.Presentation.Shell;
#if ROWMATE_SIMULATOR
using RowMate.Simulator;
using RowMate.Simulator.Panel;
using RowMate.Simulator.Presentation;
#endif

namespace RowMate.Bootstrap
{
    // La capa del simulador sobre el shell (02-tech-design §1.1, §3.5). No elige servicios (eso es de DeviceBackendFactory):
    // solo agrega la capa si el backend es el del simulador. Sin ROWMATE_SIMULATOR la capa no existe y devuelve null.
    public static class SimulatorLayerFactory
    {
        // Capa completa (cinta, llave y panel) si el backend es SimulatorDeviceBackend; si no, null.
        public static TopLayerFactory? CreateOrNull(DeviceBackend backend, AppContext app, IClock clock)
        {
#if ROWMATE_SIMULATOR
            if (backend is SimulatorDeviceBackend simulator)
            {
                return (ui, layout, tabBar) =>
                    new SimulatorLayerView(ui, new SimulatorPanelStore(simulator, app.Scene, clock, app.Logger), layout, tabBar);
            }
#endif
            return null;
        }

#if ROWMATE_SIMULATOR
        // Solo cinta y llave, sin panel (UiTestHost con dispositivos de prueba, capturas "Prueba"; QA-G2-05).
        public static TopLayerFactory CreateChromeOnly()
        {
            return (ui, layout, tabBar) => new SimulatorLayerView(ui, null, layout, tabBar);
        }
#endif
    }
}
