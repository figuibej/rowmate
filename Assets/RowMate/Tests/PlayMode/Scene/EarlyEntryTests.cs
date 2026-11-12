using System.Collections;
using NUnit.Framework;
using RowMate.Core.Design;
using RowMate.Core.Scene;
using RowMate.Presentation.Workout;
using RowMate.Scene;
using RowMate.Tests.UiSupport;
using UnityEngine;
using UnityEngine.TestTools;

namespace RowMate.Tests.PlayMode.Scene
{
    // EET · La entrada antes de que termine la precarga (#1; 06b-agua-avanzada/02-tech-design §2.1; QA-V6a-R3-01, -R2-04; AC-6B-01, -02),
    // con el tramo del player (SceneWarmupPolicy con el WarmupFrame de SceneBridge):
    // - ninguna unidad pesada (el preámbulo y el post al tamaño de la sesión) corre en un cuadro con el inmersivo abierto fuera del negro de
    //   la entrada: las que quedaron pendientes corren en ese cuadro negro y quieto (RunPendingHeavyWork);
    // - el toque toma la sesión que se preparó en reposo (desde la fase 3, antes del mundo);
    // - el negro corre el reloj de la entrada lo que tardó el trabajo pesado (el vuelo anclado a los 220 ms del toque + lo que tardó);
    // - cada sub-dibujo del calentamiento cabe en VisibleUnitMaxMs (4 ms) en la segunda precarga (la primera paga la compilación del
    //   Editor). La medida real del primer arranque es la de build.ps1 -PerfEarly (el Editor no reproduce la primera vez del proceso).
    // El orden del player loop y el cuadro del toque con el Input System: EarlyEntryInputTests (UnityEditMode).
    [Category("GPU")]
    public sealed class EarlyEntryTests : SceneTestBase
    {
        private UiTestHost CreatePlayerLike()
        {
            return CreateScene(DeviceProfile.TV, o =>
            {
                o.ScenePlayerWarmup = true;
                o.ScenePlayerStreaming = true;
            });
        }

        private IEnumerator UntilReady(int frames = 60 * 30)
        {
            for (var i = 0; i < frames && SceneHost.Readiness.Value != SceneReadiness.Ready; i++)
            {
                H.Step();
                yield return null;
            }

            Assert.That(SceneHost.Readiness.Value, Is.EqualTo(SceneReadiness.Ready), "la precarga terminó");
        }

        [UnityTest, Category("AC-6B-01")]
        public IEnumerator HeavyUnitsNeverRunOnVisibleImmersiveFrames()
        {
            AssumeGpu();
            CreatePlayerLike();
            yield return GoToWorkout();
            // El toque llega enseguida: el preámbulo puede estar a medias.
            Assert.That(SceneHost.Readiness.Value, Is.Not.EqualTo(SceneReadiness.Ready), "la precarga no terminó");
            H.Tap("workout-free");
            for (var i = 0; i < 60 * 12 && SceneHost.Readiness.Value != SceneReadiness.Ready; i++)
            {
                H.Step();
                yield return null;
            }

            Debug.Log("[EET] pesadas: " + SceneHost.HeavyUnits + " en total, " + SceneHost.HeavyUnitsWhileOpen + " con el inmersivo abierto fuera del negro, "
                      + SceneHost.BlackFrameHeavyRuns + " corridas en el negro (" + SceneHost.LastBlackFrameHeavyMs.ToString("0.00") + " ms); peor sub-dibujo "
                      + SceneHost.WorstSubDrawMs.ToString("0.00") + " ms; peor unidad liviana " + SceneHost.WorstLightUnitMs.ToString("0.00") + " ms ("
                      + SceneHost.WorstLightUnitPhase + ")");
            Assert.That(SceneHost.Readiness.Value, Is.EqualTo(SceneReadiness.Ready), "la escena llegó");
            Assert.That(SceneHost.HeavyUnitsWhileOpen, Is.EqualTo(0), "ninguna pesada a la vista");
            AssertClean(H);
        }

        [UnityTest, Category("AC-6B-01"), Category("AC-6B-02")]
        public IEnumerator TapFrameReusesPreparedSession()
        {
            AssumeGpu();
            CreatePlayerLike();
            yield return GoToWorkout();
            yield return UntilReady();
            for (var i = 0; i < 10 && SceneHost.PreparedSessionForTest == null; i++)
            {
                H.Step();
                yield return null;
            }

            var prepared = SceneHost.PreparedSessionForTest;
            Assert.That(prepared, Is.Not.Null, "la sesión preparada en reposo");
            H.Tap("workout-free");
            H.Step();
            yield return null;
            Assert.That(SceneHost.Session, Is.SameAs(prepared), "el toque toma la sesión preparada");
            AssertClean(H);
        }

        // QA-V6b-01 (AC-6B-02): con la atribución corregida, el cuadro del toque en frío (antes del preámbulo, sin sesión preparada) medía
        // 41-43 ms en el build, 16,5 de ellos de la escena: armar la sesión, su Open y su primer Tick con el JIT de Core por primera vez.
        // Ahora el toque solo anota la hora: la sesión se arma en el negro de la entrada, después del preámbulo, al día con el toque.
        [UnityTest, Category("AC-6B-02"), Category("AC-6B-01")]
        public IEnumerator AColdTapLeavesTheSessionToTheBlackFrame()
        {
            AssumeGpu();
            CreatePlayerLike();
            yield return GoToWorkout();
            // La precarga desde cero: el toque llega con el preámbulo sin correr.
            SceneHost.SimulateLowMemoryForTest();
            SceneHost.Preload();
            Assert.That(SceneHost.BuildPhase, Is.LessThanOrEqualTo(RowMate.Scene.SceneHost.PhaseCoreJit), "el preámbulo sin correr");
            var blackBefore = SceneHost.BlackFrameHeavyRuns;
            var tapAt = H.Scheduler.MonotonicSeconds;
            H.Tap("workout-free");
            Assert.That(SceneHost.OpenPending, Is.True, "el toque solo anota la hora");
            Assert.That(SceneHost.Session, Is.Null, "sin sesión en el cuadro del toque");
            H.Step();
            yield return null;
            Assert.That(SceneHost.Session, Is.Null, "tampoco en el siguiente: ninguna pesada a la vista");
            for (var i = 0; i < 60 && SceneHost.Session == null; i++)
            {
                H.Step();
                yield return null;
            }

            Assert.That(SceneHost.Session, Is.Not.Null, "la sesión llegó");
            Assert.That(SceneHost.BlackFrameHeavyRuns, Is.GreaterThan(blackBefore), "en el negro de la entrada");
            Assert.That(SceneHost.OpenPending, Is.False);
            Assert.That(SceneHost.BuildPhase, Is.GreaterThan(RowMate.Scene.SceneHost.PhaseCoreJit), "después del preámbulo");
            H.Step();
            yield return null;
            Assert.That(SceneHost.Frame.SceneTime, Is.EqualTo(H.Scheduler.MonotonicSeconds - tapAt).Within(0.05), "al día con el toque");
            Assert.That(SceneHost.HeavyUnitsWhileOpen, Is.EqualTo(0));
            AssertClean(H);
        }

        [UnityTest, Category("AC-6B-01")]
        public IEnumerator BlackFrameRunsPendingHeavyAndShiftsTheClock()
        {
            // Con un puerto de escena de prueba: el trabajo pesado del negro "tarda" 50 ms; el vuelo se ancla a los 220 ms del toque + 50 ms.
            var fake = new FakeImmersiveScene(new RowMate.Tests.Support.RecordingLogger(), ImmersiveSceneReadiness.Ready) { HeavyWorkMs = 50 };
            CreateWorkout(null, o => o.FakeScene = fake);
            yield return StartFree();
            yield return AdvanceTo(1.0);
            Assert.That(fake.HeavyRuns, Is.EqualTo(1), "una vez, en el cuadro del armado del HUD");
            Assert.That(fake.Flights, Is.EqualTo(1));
            Assert.That(fake.LastFlightStart, Is.EqualTo(SceneTimelines.FlightStartSeconds(SessionStart + 0.05)).Within(0.02), "el vuelo, corrido 50 ms");
            AssertClean(H);
        }

        [UnityTest, Category("AC-6B-01"), Category("Perf")]
        public IEnumerator EverySubDrawUnderFourMs()
        {
            AssumeGpu();
            CreatePlayerLike();
            yield return GoToWorkout();
            yield return UntilReady();
            // La segunda precarga (los shaders ya compilados por el Editor: lo que se compara con el player).
            SceneHost.SimulateLowMemoryForTest();
            SceneHost.Preload();
            yield return UntilReady();
            Debug.Log("[EET] segunda precarga: peor sub-dibujo " + SceneHost.WorstSubDrawMs.ToString("0.00") + " ms (" + SceneHost.SubDraws + " sub-dibujos); peor unidad liviana "
                      + SceneHost.WorstLightUnitMs.ToString("0.00") + " ms (" + SceneHost.WorstLightUnitPhase + "); peor unidad " + SceneHost.WorstUnitMs.ToString("0.00")
                      + " ms (" + SceneHost.WorstUnitPhase + ")");
            Assert.That(SceneHost.WarmupDraws, Is.EqualTo(2), "un dibujo de calentamiento por precarga, partido");
            Assert.That(SceneHost.SubDraws, Is.EqualTo(2 * RowMate.Scene.SceneHost.SubDrawGroups));
            Assert.That(SceneHost.WorstSubDrawMs, Is.LessThanOrEqualTo(SceneWarmupPolicy.VisibleUnitMaxMs), "cada sub-dibujo ≤ 4 ms");
            AssertClean(H);
        }
    }
}
