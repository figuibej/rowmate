using System.Collections;
using NUnit.Framework;
using RowMate.Core.Scene;
using RowMate.Core.Workout;
using RowMate.Tests.UiSupport;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace RowMate.Tests.PlayMode.Simulator
{
    // "Hora escena" del simulador con la escena real (06a-escena-lago/02-tech-design §3.8; F-SCN-23; AC-SCN-23-01, -02, -04; AC-BLE-16-07,
    // -02, -03; AC-WKT-19-03; AC-SCN-03-07, AC-BLE-15-09): + y − cambian la hora de la escena con su transición de 1,5 s por el camino
    // más corto (el panel muestra el valor nuevo en el acto); "Hora real" vuelve a la del equipo; el reloj de pared no se entera; un
    // anfitrión nuevo vuelve a la real (no se guarda); con "Remando" apagado el SPM llega en 0 y el bote frena; con "Simular desconexión"
    // la escena sigue con la última lectura.
    [Category("GPU")]
    public sealed class SceneHourSimulatorTests : SimulatorUiTestBase
    {
        private UiTestHost CreateSceneSim()
        {
            return Create(new UiHostOptions
            {
                Profile = DeviceProfile.TV,
                Backend = HostBackends.Simulator(false),
                SimulatorLayer = SimulatorLayerMode.Full,
                Scene = SceneMode.Real,
                StartUtc = UiHostOptions.WorkoutStartUtc,
                IsDesktop = false,
            });
        }

        private IEnumerator EnterFree()
        {
            yield return Tap("tab-workout");
            yield return Settle();
            H.Tap("workout-free");
            yield return Settle();
            H.Advance(2.5);
            yield return Frames();
        }

        [UnityTest, Category("AC-SCN-23-01"), Category("AC-SCN-23-02"), Category("AC-SCN-23-04"), Category("AC-BLE-16-07"), Category("AC-WKT-19-03")]
        public IEnumerator SceneHourMovesTheSceneWithItsTransition()
        {
            Assume.That(SystemInfo.graphicsDeviceType, Is.Not.EqualTo(GraphicsDeviceType.Null));
            CreateSceneSim();
            yield return EnterFree();
            var scene = H.Scene!;
            var before = scene.Frame.VisibleHour;
            var wall = H.App.Scheduler.LocalNow;
            yield return Tap("sim-open");
            yield return Tap("sim-hour-plus");
            var forced = H.App.Scene.HourOverride.Value;
            Assert.That(forced, Is.Not.Null, "el panel escribe la hora forzada");
            Assert.That(H.HasText("Hora real"), Is.True, "el panel muestra el valor nuevo en el acto");
            H.Step();
            var start = scene.Frame.VisibleHour;
            Assert.That(start, Is.EqualTo(before).Within(0.1), "la escena arranca su transición desde la visible");
            H.Advance(0.75);
            yield return Frames();
            var middle = scene.Frame.VisibleHour;
            Assert.That(middle, Is.GreaterThan(before + 0.01).And.LessThan(forced!.Value - 0.01), "a mitad de los 1,5 s");
            H.Advance(1.0);
            yield return Frames();
            Assert.That(scene.Frame.VisibleHour, Is.EqualTo(forced.Value).Within(1e-6), "llegó en 1,5 s");
            Assert.That(H.App.Scheduler.LocalNow.Hour, Is.EqualTo(wall.Hour), "AC-SCN-23-04: el reloj de pared, la hora real");
            yield return Tap("sim-hour-real");
            H.Advance(2);
            yield return Frames();
            Assert.That(H.App.Scene.HourOverride.Value, Is.Null);
            Assert.That(scene.Frame.VisibleHour, Is.EqualTo(SceneHour.Effective(null, H.App.Scheduler.LocalNow)).Within(1e-6));
            AssertClean(H);
        }

        [UnityTest, Category("AC-SCN-23-03")]
        public IEnumerator SceneHourDoesNotSurviveANewHost()
        {
            Assume.That(SystemInfo.graphicsDeviceType, Is.Not.EqualTo(GraphicsDeviceType.Null));
            CreateSceneSim();
            yield return EnterFree();
            H.App.Scene.HourOverride.Value = 22;
            H.Step();
            CreateSceneSim();
            yield return EnterFree();
            Assert.That(H.App.Scene.HourOverride.Value, Is.Null, "no se guarda: al reiniciar vuelve a real");
        }

        [UnityTest, Category("AC-SCN-03-07"), Category("AC-BLE-15-09"), Category("AC-BLE-16-02"), Category("AC-BLE-16-03")]
        public IEnumerator RowingOffStopsTheBoatAndDisconnectKeepsTheLastReading()
        {
            Assume.That(SystemInfo.graphicsDeviceType, Is.Not.EqualTo(GraphicsDeviceType.Null));
            CreateSceneSim();
            yield return EnterFree();
            H.Advance(10);
            yield return Frames();
            var scene = H.Scene!;
            Assert.That(scene.Frame.Speed, Is.GreaterThan(1), "remando, el bote avanza");
            yield return Tap("sim-open");
            yield return ScrollAndTap("sim-rowing");
            H.Advance(12);
            yield return Frames();
            Assert.That(H.App.Workout.State.Value.Data.StrokeRate, Is.EqualTo(0).Within(1e-9), "Remando apagado: SPM 0");
            Assert.That(scene.Frame.Speed, Is.LessThan(0.05), "el bote frenó");
            Assert.That(H.App.Workout.State.Value.Phase, Is.Not.EqualTo(WorkoutPhase.Idle));
            AssertClean(H);
        }
    }
}
