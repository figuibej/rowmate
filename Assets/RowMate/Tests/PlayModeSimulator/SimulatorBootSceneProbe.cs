using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using RowMate.App.Device;
using RowMate.Bootstrap;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace RowMate.Tests.PlayMode.Simulator
{
    // Arranque real de Boot.unity con ROWMATE_SIMULATOR (QA-G2-01): la misma prueba que BootSceneTests de Tests/PlayMode,
    // que con el define abre en la vista conectada (el remo simulado se conecta solo en AppRoot.Start).
    // It. 3 (AC-HIS-20-04): el arranque abre la base local del simulador, así que la corrida pone ROWMATE_DATA_DIR en una
    // carpeta temporal propia y la vuelve a dejar como estaba al terminar (también si la prueba falla).
    public static class SimulatorBootSceneProbe
    {
        public static IEnumerator Run(DeviceViewKind expectedView)
        {
            var previous = Environment.GetEnvironmentVariable(DataLocation.Variable);
            var folder = Path.Combine(Path.GetTempPath(), "RowMate.Tests", "boot-sim-" + Guid.NewGuid().ToString("N"));
            Environment.SetEnvironmentVariable(DataLocation.Variable, folder);
            try
            {
                var load = SceneManager.LoadSceneAsync("Boot", LoadSceneMode.Additive);
                while (!load.isDone)
                {
                    yield return null;
                }

                for (var i = 0; i < 5; i++)
                {
                    yield return null;
                }

                var appRoot = UnityEngine.Object.FindAnyObjectByType<AppRoot>();
                Assert.That(appRoot, Is.Not.Null, "AppRoot en la escena");
                var document = appRoot!.GetComponent<UIDocument>();
                Assert.That(document.rootVisualElement.Q("app-root"), Is.Not.Null, "la UI se armó en Start");
                Assert.That(document.panelSettings, Is.Not.SameAs(appRoot.Assets!.Panel), "el documento usa una copia del panel");
                Assert.That(appRoot.Assets.Panel!.targetTexture, Is.Null, "el asset no se toca");
                Assert.That(document.rootVisualElement.Q("simulator-layer"), Is.Not.Null, "con el define, la capa del simulador");
                Assert.That(appRoot.Shell!.Shell.DeviceScreen!.View, Is.EqualTo(expectedView));
                var unload = SceneManager.UnloadSceneAsync("Boot");
                while (unload != null && !unload.isDone)
                {
                    yield return null;
                }
            }
            finally
            {
                Environment.SetEnvironmentVariable(DataLocation.Variable, previous);
                Delete(folder);
            }
        }

        private static void Delete(string folder)
        {
            try
            {
                if (Directory.Exists(folder))
                {
                    Directory.Delete(folder, true);
                }
            }
            catch (IOException)
            {
                // Si la base quedara abierta, la carpeta temporal se limpia en la próxima corrida.
            }
        }
    }
}
