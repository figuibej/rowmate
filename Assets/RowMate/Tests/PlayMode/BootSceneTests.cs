using System.Collections;
using NUnit.Framework;
using RowMate.App.Device;
using RowMate.Bootstrap;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace RowMate.Tests.PlayMode
{
    // Arranque real de Boot.unity (QA-G2-01): AppRoot configura en Awake y arma la UI en Start. Ningún Error ni excepción en
    // el registro (el Test Framework falla solo con un Error no esperado), #app-root bajo la raíz del documento, el documento
    // usa una COPIA del panel y el asset sigue sin targetTexture. Sin el define abre en la vista de búsqueda.
    public sealed class BootSceneTests
    {
        public const string SceneName = "Boot";

        [UnityTest, Category("AC-APP-01-03"), Category("AC-APP-01-07"), Category("QA-G2-01")]
        public IEnumerator BootSceneStartsWithoutExceptions()
        {
            // Sin el define abre en la vista de búsqueda; con el define (corrida -Simulator) el remo simulado se conecta solo.
            yield return BootSceneProbe.Run(BuildFlags.IsSimulator ? DeviceViewKind.Connected : DeviceViewKind.Scan);
        }
    }

    public static class BootSceneProbe
    {
        public static IEnumerator Run(DeviceViewKind expectedView)
        {
            var load = SceneManager.LoadSceneAsync(BootSceneTests.SceneName, LoadSceneMode.Additive);
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
            Assert.That(document.rootVisualElement, Is.Not.Null);
            Assert.That(document.rootVisualElement.Q("app-root"), Is.Not.Null, "la UI se armó en Start");
            Assert.That(appRoot.Assets, Is.Not.Null);
            Assert.That(document.panelSettings, Is.Not.SameAs(appRoot.Assets!.Panel), "el documento usa una copia del panel");
            Assert.That(appRoot.Assets.Panel!.targetTexture, Is.Null, "el asset no se toca");
            Assert.That(appRoot.Shell, Is.Not.Null);
            Assert.That(appRoot.Shell!.Shell.DeviceScreen!.View, Is.EqualTo(expectedView));
            var unload = SceneManager.UnloadSceneAsync(BootSceneTests.SceneName);
            while (unload != null && !unload.isDone)
            {
                yield return null;
            }
        }
    }
}
