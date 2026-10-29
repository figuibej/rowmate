using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using RowMate.App.Device;
using RowMate.Bootstrap;
using RowMate.Infrastructure.Database;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace RowMate.Tests.PlayMode
{
    // Arranque real de Boot.unity (QA-G2-01): AppRoot configura en Awake y arma la UI en Start. Ningún Error ni excepción en
    // el registro (el Test Framework falla solo con un Error no esperado), #app-root bajo la raíz del documento, el documento
    // usa una COPIA del panel y el asset sigue sin targetTexture. Sin el define abre en la vista de búsqueda.
    // It. 3 (AC-HIS-20-04; 03-rutinas/02-tech-design §6.2): el arranque real abre la base local, así que cada prueba pone
    // ROWMATE_DATA_DIR en una carpeta temporal propia (y la vuelve a dejar como estaba): ninguna corrida abre las bases del
    // usuario aunque se corra desde el Editor, sin run-unity-tests.ps1.
    public sealed class BootSceneTests
    {
        public const string SceneName = "Boot";

        private string? _previousDataDir;
        private string _dataDir = "";

        [SetUp]
        public void UseATestDataFolder()
        {
            _previousDataDir = Environment.GetEnvironmentVariable(DataLocation.Variable);
            _dataDir = Path.Combine(Path.GetTempPath(), "RowMate.Tests", "boot-" + Guid.NewGuid().ToString("N"));
            Environment.SetEnvironmentVariable(DataLocation.Variable, _dataDir);
        }

        [TearDown]
        public void RestoreTheDataFolder()
        {
            Environment.SetEnvironmentVariable(DataLocation.Variable, _previousDataDir);
            try
            {
                if (Directory.Exists(_dataDir))
                {
                    Directory.Delete(_dataDir, true);
                }
            }
            catch (IOException)
            {
                // Si la base quedara abierta, la carpeta temporal se limpia en la próxima corrida.
            }
        }

        [UnityTest, Category("AC-APP-01-03"), Category("AC-APP-01-07"), Category("QA-G2-01")]
        public IEnumerator BootSceneStartsWithoutExceptions()
        {
            // Sin el define abre en la vista de búsqueda; con el define (corrida -Simulator) el remo simulado se conecta solo.
            yield return BootSceneProbe.Run(BuildFlags.IsSimulator ? DeviceViewKind.Connected : DeviceViewKind.Scan);
        }

        [UnityTest, Category("AC-HIS-20-04")]
        public IEnumerator UsesTheTestDataFolder()
        {
            yield return BootSceneProbe.Run(BuildFlags.IsSimulator ? DeviceViewKind.Connected : DeviceViewKind.Scan);
            var file = Path.Combine(_dataDir, DatabaseFiles.NameFor(BuildFlags.IsSimulator));
            Assert.That(File.Exists(file), Is.True, "AppRoot abrió la base en ROWMATE_DATA_DIR (" + DatabaseFiles.NameFor(BuildFlags.IsSimulator) + ")");
        }

        // It. 4a (04a-motor-hud-funcional/02-tech-design §2.13, §3.5, §4.2, §4.4): AppRoot compone una sola base local para las
        // rutinas y las sesiones, el ciclo de vida (adelante y con foco al arrancar) y la pantalla encendida (pedida en Awake; en
        // el Editor de Windows, SetThreadExecutionState de verdad). Al descargar la escena la suelta.
        [UnityTest, Category("AC-APP-14-01"), Category("AC-HIS-20-04")]
        public IEnumerator ComposesTheStoreLifecycleAndKeepAwake()
        {
            var load = SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Additive);
            while (!load.isDone)
            {
                yield return null;
            }

            for (var i = 0; i < 5; i++)
            {
                yield return null;
            }

            var appRoot = UnityEngine.Object.FindAnyObjectByType<AppRoot>();
            Assert.That(appRoot, Is.Not.Null);
            Assert.That(appRoot!.Store, Is.Not.Null, "una base local");
            Assert.That(appRoot.Store!.Routines, Is.Not.Null);
            Assert.That(appRoot.Store.Sessions, Is.Not.Null, "las sesiones en el mismo archivo");
            Assert.That(appRoot.Lifecycle, Is.Not.Null);
            Assert.That(appRoot.Lifecycle!.InBackground.Value, Is.False, "arranca adelante");
            Assert.That(appRoot.Lifecycle.IsUserFacing, Is.True);
            Assert.That(appRoot.KeepAwake, Is.Not.Null, "la pantalla encendida");
            var keepAwake = appRoot.KeepAwake!;
            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                Assert.That(keepAwake.IsHeld, Is.True, "en el Editor de Windows el sistema acepta el pedido");
            }

            Assert.That(File.Exists(Path.Combine(_dataDir, DatabaseFiles.NameFor(BuildFlags.IsSimulator))), Is.True);
            var unload = SceneManager.UnloadSceneAsync(SceneName);
            while (unload != null && !unload.isDone)
            {
                yield return null;
            }

            Assert.That(keepAwake.IsHeld, Is.False, "al cerrar la suelta");
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

            var appRoot = UnityEngine.Object.FindAnyObjectByType<AppRoot>();
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
