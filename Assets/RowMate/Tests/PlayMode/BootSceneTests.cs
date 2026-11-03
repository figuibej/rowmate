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

        // It. 5 (QA-V4b-06; 05-series-parciales/00-deuda-4b §2): el arranque precalienta el HUD en su primer cuadro y lo desarma en
        // el siguiente, sin avisos y sin dejar nada en el árbol; el motor sigue en reposo.
        [UnityTest, Category("QA-V4b-06"), Category("AC-APP-01-03")]
        public IEnumerator TheBootWarmsUpTheHudAndLeavesNothing()
        {
            var messages = new System.Collections.Generic.List<string>();
            Application.LogCallback collect = (message, stack, type) => messages.Add(type + ": " + message);
            Application.logMessageReceived += collect;
            try
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
                Assert.That(appRoot!.HudWarmedUp, Is.True, "precalentado y desarmado");
                var root = appRoot.GetComponent<UIDocument>().rootVisualElement;
                Assert.That(root.Q(RowMate.Presentation.Workout.HudWarmup.HolderName), Is.Null, "nada en el árbol");
                Assert.That(appRoot.App!.Workout.State.Value.Phase, Is.EqualTo(RowMate.Core.Workout.WorkoutPhase.Idle));
                Assert.That(messages.Exists(m => m.Contains("[UI] HUD precalentado (QA-V4b-06)")), Is.True, string.Join("\n", messages));
                Assert.That(messages.Exists(m => m.Contains("no se pudo precalentar") || m.Contains("al desarmar el HUD precalentado")), Is.False,
                    string.Join("\n", messages));
                var unload = SceneManager.UnloadSceneAsync(SceneName);
                while (unload != null && !unload.isDone)
                {
                    yield return null;
                }
            }
            finally
            {
                Application.logMessageReceived -= collect;
            }
        }
    }

    // It. 5 (05-series-parciales/02-tech-design §2.7, §6.3 BST; AC-WKT-18-10, -11; QA-G5T-05): las preferencias del arranque real, en
    // la carpeta temporal de ROWMATE_DATA_DIR (nunca la del usuario).
    public sealed class BootScenePreferencesTests
    {
        private string? _previousDataDir;
        private string _dataDir = "";

        [SetUp]
        public void UseATestDataFolder()
        {
            _previousDataDir = Environment.GetEnvironmentVariable(DataLocation.Variable);
            _dataDir = Path.Combine(Path.GetTempPath(), "RowMate.Tests", "boot-prefs-" + Guid.NewGuid().ToString("N"));
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
            }
        }

        private string PrefsFile => Path.Combine(_dataDir, RowMate.Infrastructure.Preferences.PreferencesFiles.Name);

        private static IEnumerator Load()
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
        }

        private static IEnumerator Unload()
        {
            var unload = SceneManager.UnloadSceneAsync(BootSceneTests.SceneName);
            while (unload != null && !unload.isDone)
            {
                yield return null;
            }
        }

        [UnityTest, Category("AC-WKT-18-11"), Category("AC-HIS-20-04")]
        public IEnumerator TheBootReadsThePreferencesFromTheDataFolderAndWritesNothingWithoutTouches()
        {
            yield return Load();
            var appRoot = UnityEngine.Object.FindAnyObjectByType<AppRoot>();
            Assert.That(appRoot, Is.Not.Null);
            Assert.That(appRoot!.App!.Preferences, Is.InstanceOf<RowMate.Infrastructure.Preferences.JsonFilePreferences>(), "el almacén real");
            Assert.That(appRoot.App.SeriesPanels.State.Value.Laps || appRoot.App.SeriesPanels.State.Value.Reps, Is.False, "cerrados por defecto");
            yield return Unload();
            Assert.That(File.Exists(PrefsFile), Is.False, "sin toques, prefs.json no existe al terminar (el precalentado no escribe)");
        }

        [UnityTest, Category("AC-WKT-18-10"), Category("AC-WKT-18-11")]
        public IEnumerator PreferencesSurviveARestart()
        {
            yield return Load();
            var appRoot = UnityEngine.Object.FindAnyObjectByType<AppRoot>();
            appRoot!.App!.SeriesPanels.Toggle(RowMate.Core.Workout.SeriesPanelKind.Laps);
            yield return Unload();
            Assert.That(File.Exists(PrefsFile), Is.True, "guardado al tocar (y terminado al cerrar)");
            StringAssert.Contains("\"immersive.lapsExpanded\":true", File.ReadAllText(PrefsFile).Replace(" ", ""));

            yield return Load();
            appRoot = UnityEngine.Object.FindAnyObjectByType<AppRoot>();
            Assert.That(appRoot!.App!.SeriesPanels.State.Value.Laps, Is.True, "el segundo arranque lo encuentra abierto");
            Assert.That(appRoot.App.SeriesPanels.State.Value.Reps, Is.False);
            yield return Unload();
        }

        [UnityTest, Category("AC-WKT-18-11")]
        public IEnumerator ASeededFileStaysByteForByte()
        {
            Directory.CreateDirectory(_dataDir);
            var bytes = System.Text.Encoding.UTF8.GetBytes("{\"immersive.repsExpanded\":true,\"hrm.lastDevice\":\"x\"}");
            File.WriteAllBytes(PrefsFile, bytes);
            var stamp = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);
            File.SetLastWriteTimeUtc(PrefsFile, stamp);
            yield return Load();
            var appRoot = UnityEngine.Object.FindAnyObjectByType<AppRoot>();
            Assert.That(appRoot!.App!.SeriesPanels.State.Value.Reps, Is.True, "lo leyó");
            yield return Unload();
            Assert.That(File.ReadAllBytes(PrefsFile), Is.EqualTo(bytes), "los mismos bytes (el precalentado no escribe)");
            Assert.That(File.GetLastWriteTimeUtc(PrefsFile), Is.EqualTo(stamp), "y la misma fecha");
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
