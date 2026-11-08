using RowMate.Platform.Ble;
using UnityEditor;

namespace RowMate.EditorTools
{
    // RowMateBle.dll queda cargada en el Editor entre recargas del dominio (R-02 de la It. 10a): antes de recargar se cierra el plugin
    // si esta sesión lo inició (detiene la búsqueda, cierra las sesiones y une sus hilos), así ningún hilo nativo sobrevive a los
    // objetos de C# que lo usaban. Si nunca se usó, no carga la DLL.
    [InitializeOnLoad]
    internal static class NativeBleReload
    {
        static NativeBleReload()
        {
            AssemblyReloadEvents.beforeAssemblyReload += WindowsBleApi.ShutdownIfInitialized;
            EditorApplication.quitting += WindowsBleApi.ShutdownIfInitialized;
        }
    }
}
