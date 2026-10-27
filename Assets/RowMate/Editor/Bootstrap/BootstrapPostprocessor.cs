using UnityEditor;
using UnityEngine;

namespace RowMate.EditorTools
{
    // Disparador del bootstrapper en batchmode (hallazgo del primer open real, It. 2: 00-primer-open.md §3).
    // Con -batchmode Unity no da ninguna vuelta del loop del Editor antes de salir (-quit) ni antes de -executeMethod, así
    // que el EditorApplication.delayCall que agenda ProjectBootstrapper no corre: el primer open terminó con código 0 y sin
    // ProjectSettings/RowMateBootstrap.json. OnPostprocessAllAssets(…, didDomainReload: true) llega al final del refresh
    // de cada recarga de dominio, también en batchmode, y ahí se pueden crear assets: es el mismo gancho que usa URP 17
    // para crear su Global Settings (UniversalRenderPipelineGlobalSettingsPostprocessor).
    // En el Editor con ventana sigue el delayCall (puede mostrar el aviso de reinicio del Input System).
    internal sealed class BootstrapPostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets,
            string[] movedFromAssetPaths, bool didDomainReload)
        {
            if (didDomainReload && Application.isBatchMode)
            {
                ProjectBootstrapper.RunInBatchMode();
            }
        }
    }
}
