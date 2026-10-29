using System;
using System.Collections;
using NUnit.Framework;
using RowMate.Core.Workout;
using RowMate.Tests.Support;
using RowMate.Tests.UiSupport;
using UnityEngine.TestTools;

namespace RowMate.Tests.PlayMode
{
    // WPS · El guardado visto desde la pantalla (F-WKT-06, Q-13; 04a-motor-hud-funcional/02-tech-design §3): la sesión en la
    // base de prueba (SQLite, el mismo archivo que las rutinas) o en un repositorio de guion con demoras y fallas.
    public sealed class WorkoutPersistenceTests : WorkoutUiTestBase
    {
        [UnityTest, Category("AC-WKT-01-02")]
        public IEnumerator FreeStartCreatesTheSessionAndTheClockRuns()
        {
            CreateWorkout(RoutineSeeds.Lista);
            yield return StartFree();
            Assert.That(H.Immersive, Is.Not.Null, "el inmersivo en el acto");
            Assert.That(Phase, Is.EqualTo(WorkoutPhase.Active));
            var session = LastSession();
            Assert.That(session, Is.Not.Null, "la sesión creada en la base");
            Assert.That(session!.RoutineName, Is.Null, "sin rutina");
            Assert.That(session.IsCut, Is.True, "abierta mientras dura");
            Assert.That(H.TextOf("hud-time-value"), Is.EqualTo("00:00"));
            yield return AdvanceTo(1.05);
            Assert.That(H.TextOf("hud-time-value"), Is.EqualTo("00:01"), "el primer segundo se ve 1 s después");
        }

        [UnityTest, Category("AC-WKT-01-11")]
        public IEnumerator CreateFailsButTheWorkoutRuns()
        {
            CreateWorkout(RoutineSeeds.Lista, o => o.Sessions = HostSessions.Scripted(r => r.FailOnCreate = true));
            yield return StartFree();
            yield return AdvanceTo(2.05);
            Assert.That(H.Shell.Toasts.CurrentText, Is.EqualTo("No se pudo guardar la sesión"));
            Assert.That(H.TextOf("hud-time-value"), Is.EqualTo("00:02"), "el reloj arranca igual");
            yield return AdvanceTo(10.05);
            Assert.That(H.TextOf("hud-time-value"), Is.EqualTo("00:10"));
            yield return Tap("hud-finish");
            yield return Tap("dialog-confirm");
            yield return Settle();
            Assert.That(H.Immersive, Is.Null, "al terminar vuelve a reposo");
            Assert.That(H.Shell.Toasts.CurrentText, Is.EqualTo("No se pudo guardar la sesión"), "con el mismo aviso");
            Assert.That(H.ScriptedSessions!.Store.ListAsync().Result.Count, Is.EqualTo(0), "no se guardó nada de esa sesión");
        }

        [UnityTest, Category("AC-WKT-06-12"), Category("AC-APP-03-11")]
        public IEnumerator SaveErrorsShowOnceAndTheWorkoutGoesOn()
        {
            CreateWorkout(RoutineSeeds.Lista, o => o.Sessions = HostSessions.Scripted(r =>
            {
                r.FailBatch = 1;
                r.FailBatchesFrom = true;
                r.FailOnFinish = true;
            }));
            yield return StartFree();
            var shown = H.Shell.Toasts.ShowCount;
            yield return AdvanceTo(30.05);
            Assert.That(H.Shell.Toasts.ShowCount - shown, Is.EqualTo(1), "el aviso a lo sumo una vez mientras se rema");
            Assert.That(H.TextOf("hud-time-value"), Is.EqualTo("00:30"), "el entrenamiento sigue en pantalla");
            Assert.That(Phase, Is.EqualTo(WorkoutPhase.Active));
            var toast = Bounds("toast");
            Assert.That(toast.yMax + 8, Is.LessThanOrEqualTo(Bounds("hud-controls").yMin + 0.5f), "en el inmersivo no tapa los controles");
            yield return Tap("hud-finish");
            yield return Tap("dialog-confirm");
            yield return Settle();
            Assert.That(H.Immersive, Is.Null);
            Assert.That(H.Shell.Toasts.CurrentText, Is.EqualTo("No se pudo guardar la sesión"), "al terminar, el mismo aviso");
        }

        [UnityTest, Category("AC-WKT-06-13")]
        public IEnumerator SlowSaveDoesNotStallTheHud()
        {
            CreateWorkout(RoutineSeeds.Lista, o => o.Sessions = HostSessions.Scripted(r =>
            {
                r.WriteDelay = TimeSpan.FromSeconds(2);
                r.FinishDelay = TimeSpan.FromSeconds(2);
            }));
            yield return StartFree();
            for (var second = 1; second <= 20; second++)
            {
                yield return AdvanceTo(second + 0.05);
                Assert.That(H.TextOf("hud-time-value"), Is.EqualTo("00:" + second.ToString("00")), "el reloj al día en el segundo " + second);
            }

            Assert.That(H.TextOf("hud-spm-value"), Is.EqualTo("24.0"), "los valores al día");
            yield return Tap("hud-finish");
            yield return Frames();
            H.Tap("dialog-confirm");
            yield return Frames();
            H.Advance(1);
            yield return Frames();
            Assert.That(H.Immersive, Is.Not.Null, "el inmersivo espera el cierre guardado");
            yield return Advance(3);
            yield return Settle();
            Assert.That(H.Immersive, Is.Null, "y se cierra cuando quedó guardado");
            var calls = H.ScriptedSessions!.Calls;
            Assert.That(calls[calls.Count - 1], Does.StartWith("finish"), "el cierre después del último punto");
        }
    }
}
