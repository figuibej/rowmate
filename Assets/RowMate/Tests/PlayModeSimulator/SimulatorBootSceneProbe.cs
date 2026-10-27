using System.Collections;
using NUnit.Framework;
using RowMate.App.Device;
using RowMate.Bootstrap;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace RowMate.Tests.PlayMode.Simulator
{
    // Arranque real de Boot.unity con ROWMATE_SIMULATOR (QA-G2-01): la misma prueba que BootSceneTests de Tests/PlayMode,
    // que con el define abre en la vista conectada (el remo simulado se conecta solo en AppRoot.Start).
    public static class SimulatorBootSceneProbe
    {
        public static IEnumerator Run(DeviceViewKind expectedView)
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

            var appRoot = Object.FindAnyObjectByType<AppRoot>();
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
    }
}
