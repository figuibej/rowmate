using System;
using System.Collections.Generic;
using System.Diagnostics;
using NUnit.Framework;
using RowMate.App.Device;
using RowMate.App.Routines;
using RowMate.App.Workout;
using RowMate.Core.Ble;
using RowMate.Core.Models;
using RowMate.Core.Ports;
using RowMate.Core.Workout;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Workout
{
    // WST: Workout en reposo (F-WKT-12, F-BLE-10; 02-tech-design §2.11 de la It. 4a): el remo conectado, la colección
    // compartida, iniciar y los avisos, con el motor de verdad, el remo de prueba y el ciclo de vida de prueba. La pantalla la
    // prueba WSC (PlayMode).
    // legacy: lib/features/workout/workout_screen.dart:50-167; lib/features/device/device_provider.dart:94.
    [TestFixture]
    public sealed class WorkoutStoreTests
    {
        private static readonly BleStatus[] NoRower = { BleStatus.Off, BleStatus.Scanning, BleStatus.Connecting, BleStatus.Disconnected };

        [Test, Category("AC-BLE-10-03")]
        public void AllFourNoRowerStates()
        {
            // QA-G4a-P07: "conectado" es solo Connected (el legacy mira status == connected); los otros cuatro son "sin remo".
            using (var f = new Fixture())
            {
                Assert.That(f.Store.State.Value.RowerConnected, Is.True, "el remo de prueba arranca conectado");
                foreach (var status in NoRower)
                {
                    f.H.Rower.SetStatus(status);
                    Assert.That(f.Store.State.Value.RowerConnected, Is.False, status.ToString());
                    Assert.That(f.Store.StartFree(), Is.False, status + ": sin sesión");
                    Assert.That(f.Store.TapRoutine(RoutineSeeds.Corta()), Is.EqualTo(RoutineTapOutcome.Ignored), status.ToString());
                    f.H.Rower.SetStatus(BleStatus.Connected);
                    Assert.That(f.Store.State.Value.RowerConnected, Is.True, "vuelve con Connected (AC-BLE-10-05)");
                }

                Assert.That(f.Sessions.Sessions, Is.Empty);
                Assert.That(f.Toasts.Count, Is.EqualTo(0));
            }
        }

        [Test, Category("AC-WKT-12-03"), Category("AC-BLE-10-04")]
        public void FreeNeedsTheRower()
        {
            // workout_screen.dart:85-94: sin remo, Entrenamiento libre no hace nada; con el remo, inicia la libre en el acto.
            using (var f = new Fixture())
            {
                f.H.Rower.SetStatus(BleStatus.Disconnected);
                Assert.That(f.Store.StartFree(), Is.False);
                Assert.That(f.H.Engine.Phase, Is.EqualTo(WorkoutPhase.Idle));
                Assert.That(f.Sessions.Sessions, Is.Empty);
                f.H.Rower.SetStatus(BleStatus.Connected);
                Assert.That(f.Store.StartFree(), Is.True);
                Assert.That(f.H.Engine.Phase, Is.EqualTo(WorkoutPhase.Active));
                Assert.That(f.Store.State.Value.SessionPhase, Is.EqualTo(WorkoutPhase.Active));
                Assert.That(f.Sessions.Sessions.Count, Is.EqualTo(1));
                Assert.That(f.Sessions.Sessions[0].RoutineId, Is.Null, "libre");
            }
        }

        [Test, Category("AC-WKT-12-05")]
        public void UsesTheSharedCollection()
        {
            // routines_provider.dart:27-52: Workout ve la MISMA colección que Rutinas, sin volver a leer la base: alta arriba,
            // renombre en su lugar y baja, sin estado de carga.
            using (var f = new Fixture(RoutineSeeds.Lista.Routines))
            {
                Assert.That(f.Store.State.Value.Routines, Is.SameAs(f.Routines.State.Value.Routines));
                Assert.That(f.Store.State.Value.RoutinesPhase, Is.EqualTo(RoutinesPhase.Ready));
                Assert.That(f.Repository.GetAllCalls, Is.EqualTo(1));
                var recorder = new StateRecorder<WorkoutRestState>(f.Store.State);
                Assert.That(f.Routines.CreateAsync("Nueva", "", new[] { RoutineSeeds.Step(StepType.Work, dur: 60) }).IsCompleted, Is.True);
                var solo = Find(f.Routines, "Solo calentamiento");
                Assert.That(f.Routines.UpdateAsync(solo with { Name = "Solo entrada en calor" }).IsCompleted, Is.True);
                Assert.That(f.Routines.DeleteAsync(Find(f.Routines, "Pirámide 500").Id!.Value).IsCompleted, Is.True);
                Assert.That(Names(f.Store.State.Value.Routines), Is.EqualTo(new[] { "Nueva", "Escalera SPM", "Solo entrada en calor", "Intervalos 4×" }));
                Assert.That(f.Store.State.Value.Routines, Is.SameAs(f.Routines.State.Value.Routines));
                foreach (var state in recorder.Values)
                {
                    Assert.That(state.RoutinesPhase, Is.EqualTo(RoutinesPhase.Ready), "sin indicador de carga");
                }

                Assert.That(f.Repository.GetAllCalls, Is.EqualTo(1), "Workout no vuelve a leer la base");
                recorder.Dispose();
            }
        }

        [Test, Category("AC-WKT-12-07")]
        public void TapConfirmsWhenItCanStart()
        {
            // :118-120 y :145-167: con el remo y una rutina que se puede iniciar, el toque abre el diálogo (nada más); Iniciar
            // la inicia con la rutina de la plantilla.
            using (var f = new Fixture())
            {
                var corta = WorkoutHarness.Saved(RoutineSeeds.Corta(), 3);
                Assert.That(f.Store.TapRoutine(corta), Is.EqualTo(RoutineTapOutcome.Confirm));
                Assert.That(f.Sessions.Sessions, Is.Empty, "el toque no crea nada");
                Assert.That(f.Toasts.Count, Is.EqualTo(0));
                Assert.That(f.Store.StartRoutine(corta), Is.True);
                Assert.That(f.H.Engine.Phase, Is.EqualTo(WorkoutPhase.Active));
                Assert.That(f.Sessions.Sessions[0].RoutineId, Is.EqualTo(3));
                Assert.That(f.Sessions.Sessions[0].RoutineName, Is.EqualTo("Corta"));
            }
        }

        [Test, Category("AC-WKT-12-08"), Category("AC-RUT-06-08")]
        public void CannotStartToastWithoutSession()
        {
            // R3-08 (DV-28): Serie enorme, Vatios imposibles y Serie ×0 no abren el diálogo: el aviso, ninguna sesión y la app
            // sigue (después, la libre inicia normalmente). En la PC se decide en menos de 50 ms y sin expandir.
            foreach (var name in new[] { "Serie enorme", "Vatios imposibles", "Serie ×0" })
            {
                using (var f = new Fixture())
                {
                    var routine = RoutineSeeds.Rara4a(name).Routines[0];
                    var watch = Stopwatch.StartNew();
                    var outcome = f.Store.TapRoutine(routine);
                    watch.Stop();
                    Assert.That(outcome, Is.EqualTo(RoutineTapOutcome.CannotStart), name);
                    Assert.That(watch.Elapsed.TotalMilliseconds, Is.LessThan(50), name + ": sin expandir");
                    Assert.That(f.Toasts.Values, Is.EqualTo(new[] { WorkoutToast.CannotStart }), name);
                    Assert.That(f.Sessions.Sessions, Is.Empty, name + ": la base no cambia");
                    Assert.That(f.Store.StartRoutine(routine), Is.False, name + ": tampoco desde el diálogo");
                    Assert.That(f.Toasts.Count, Is.EqualTo(2));
                    Assert.That(f.Sessions.Sessions, Is.Empty);
                    Assert.That(f.Store.StartFree(), Is.True, name + ": la libre inicia");
                }
            }
        }

        [Test, Category("AC-WKT-01-10")]
        public void SecondStartIsIgnored()
        {
            // DV-30: el segundo toque (libre o Iniciar) no crea otra sesión; con una sesión en curso, las tarjetas no responden.
            using (var f = new Fixture())
            {
                Assert.That(f.Store.StartFree(), Is.True);
                Assert.That(f.Store.StartFree(), Is.False);
                Assert.That(f.Store.StartRoutine(RoutineSeeds.Corta()), Is.False);
                Assert.That(f.Store.TapRoutine(RoutineSeeds.Corta()), Is.EqualTo(RoutineTapOutcome.Ignored));
                Assert.That(f.Sessions.Sessions.Count, Is.EqualTo(1));
            }

            using (var f = new Fixture())
            {
                Assert.That(f.Store.StartRoutine(RoutineSeeds.Corta()), Is.True);
                Assert.That(f.Store.StartRoutine(RoutineSeeds.Corta()), Is.False);
                Assert.That(f.Sessions.Sessions.Count, Is.EqualTo(1));
            }
        }

        [Test, Category("AC-BLE-10-06")]
        public void StartRoutineDoesNotCheckTheRower()
        {
            // :118-119 y :163-165: la conexión se mira al tocar la tarjeta; Iniciar con el remo ya desconectado inicia igual.
            using (var f = new Fixture())
            {
                Assert.That(f.Store.TapRoutine(RoutineSeeds.Corta()), Is.EqualTo(RoutineTapOutcome.Confirm));
                f.H.Rower.SetStatus(BleStatus.Disconnected);
                Assert.That(f.Store.StartRoutine(RoutineSeeds.Corta()), Is.True);
                Assert.That(f.H.Engine.Phase, Is.EqualTo(WorkoutPhase.Active));
            }
        }

        [Test, Category("AC-WKT-01-11")]
        public void SaveErrorOnStartAndAtTheEnd()
        {
            // DV-29: con el alta que falla, el aviso al iniciar y otra vez al terminar.
            using (var f = new Fixture(sessions: s => new ScriptedSessionRepository(s) { FailOnCreate = true }))
            {
                Assert.That(f.Store.StartFree(), Is.True);
                Assert.That(f.Toasts.Values, Is.EqualTo(new[] { WorkoutToast.SaveError }));
                f.H.At(12);
                f.H.Engine.FinishAsync();
                Assert.That(f.H.Engine.Phase, Is.EqualTo(WorkoutPhase.Idle));
                Assert.That(f.Toasts.Values, Is.EqualTo(new[] { WorkoutToast.SaveError, WorkoutToast.SaveError }));
            }
        }

        [Test, Category("AC-WKT-03-05")]
        public void CompletedToast()
        {
            // :210-216: la rutina que termina sola avisa workoutCompleted cuando el guardado volvió; terminar a mano no avisa
            // nada (Q-53).
            using (var f = new Fixture())
            {
                Assert.That(f.Store.StartRoutine(RoutineSeeds.Corta()), Is.True);
                f.H.At(29.9);
                Assert.That(f.Toasts.Count, Is.EqualTo(0));
                f.H.At(30);
                Assert.That(f.Toasts.Values, Is.EqualTo(new[] { WorkoutToast.Completed }));
                Assert.That(f.Store.State.Value.SessionPhase, Is.EqualTo(WorkoutPhase.Idle));
                Assert.That(f.Store.StartFree(), Is.True);
                f.H.At(40);
                f.H.Engine.FinishAsync();
                Assert.That(f.Toasts.Values, Is.EqualTo(new[] { WorkoutToast.Completed }), "a mano: nada");
            }
        }

        [Test, Category("AC-WKT-06-12")]
        public void SaveErrorToast()
        {
            // Un punto que no se guarda: el aviso a lo sumo una vez mientras se rema, y otra al terminar (la sesión quedó
            // cortada: el cierre vuelve a fallar con el mismo punto).
            using (var f = new Fixture(sessions: s => new ScriptedSessionRepository(s) { FailBatch = 1, FailBatchesFrom = true, FailFinishWithRetriedPoints = true }))
            {
                Assert.That(f.Store.StartFree(), Is.True);
                f.H.At(31);
                Assert.That(f.Toasts.Values, Is.EqualTo(new[] { WorkoutToast.SaveError }), "una vez mientras se rema");
                f.H.Engine.FinishAsync();
                Assert.That(f.Toasts.Values, Is.EqualTo(new[] { WorkoutToast.SaveError, WorkoutToast.SaveError }));
            }
        }

        [Test, Category("AC-WKT-06-12")]
        public void TransientFailureRecoveredAtTheClose()
        {
            // DT4a-17: el lote 2 falla una vez y el cierre lo recupera: el aviso de la falla y, al final, el de un guardado bueno
            // (workoutCompleted para la rutina que terminó sola).
            using (var f = new Fixture(sessions: s => new ScriptedSessionRepository(s) { FailBatch = 2 }))
            {
                Assert.That(f.Store.StartRoutine(RoutineSeeds.Corta()), Is.True);
                f.H.At(31);
                Assert.That(f.Toasts.Values, Is.EqualTo(new[] { WorkoutToast.SaveError, WorkoutToast.Completed }));
                Assert.That(f.H.Engine.LastFinishedSessionId, Is.EqualTo(1));
            }
        }

        [Test, Category("AC-WKT-02-06"), Category("AC-WKT-02-08")]
        public void HeldToastShowsOnceWhenFacingTheUserAgain()
        {
            // QA-G4a-P08: en segundo plano no se emite nada; se guarda solo el ÚLTIMO aviso (la falla del lote queda tapada por el
            // fin) y sale UNA vez cuando la app vuelve adelante. Después, nada más.
            using (var f = new Fixture(sessions: s => new ScriptedSessionRepository(s) { FailBatch = 3 }))
            {
                Assert.That(f.Store.StartRoutine(RoutineSeeds.Corta()), Is.True);
                f.H.At(8);
                f.H.Lifecycle.SetBackground(true);
                f.H.At(40);
                Assert.That(f.Toasts.Count, Is.EqualTo(0), "en segundo plano no se emite");
                Assert.That(f.Store.HeldToast, Is.EqualTo(WorkoutToast.Completed), "el último: el fin");
                f.H.Lifecycle.SetBackground(false);
                Assert.That(f.Toasts.Values, Is.EqualTo(new[] { WorkoutToast.Completed }));
                Assert.That(f.Store.HeldToast, Is.Null);
                Assert.That(f.Store.State.Value.SessionPhase, Is.EqualTo(WorkoutPhase.Idle), "en reposo al volver");
                f.H.Lifecycle.SetFocus(false);
                f.H.Lifecycle.SetFocus(true);
                f.H.Lifecycle.SetBackground(true);
                f.H.Lifecycle.SetBackground(false);
                Assert.That(f.Toasts.Count, Is.EqualTo(1), "una sola vez");
            }
        }

        [Test, Category("AC-WKT-02-09")]
        public void HeldToastShowsOnFocus()
        {
            // Windows: la ventana sin foco (o minimizada) sigue andando; el aviso del fin sale una vez al volver el foco.
            using (var f = new Fixture())
            {
                Assert.That(f.Store.StartRoutine(RoutineSeeds.Corta()), Is.True);
                f.H.At(10);
                f.H.Lifecycle.SetFocus(false);
                f.H.At(35);
                Assert.That(f.Toasts.Count, Is.EqualTo(0));
                Assert.That(f.Store.HeldToast, Is.EqualTo(WorkoutToast.Completed));
                f.H.Lifecycle.SetFocus(true);
                Assert.That(f.Toasts.Values, Is.EqualTo(new[] { WorkoutToast.Completed }));
                f.H.Lifecycle.SetFocus(false);
                f.H.Lifecycle.SetFocus(true);
                Assert.That(f.Toasts.Count, Is.EqualTo(1));
            }
        }

        [Test, Category("AC-WKT-02-08")]
        public void HeldToastWaitsForFocusAndForeground()
        {
            // Sin el usuario delante = en segundo plano O sin foco: volver adelante sin foco todavía no lo emite.
            using (var f = new Fixture())
            {
                Assert.That(f.Store.StartRoutine(RoutineSeeds.Corta()), Is.True);
                f.H.Lifecycle.SetFocus(false);
                f.H.Lifecycle.SetBackground(true);
                f.H.At(31);
                f.H.Lifecycle.SetBackground(false);
                Assert.That(f.Toasts.Count, Is.EqualTo(0), "adelante pero sin foco");
                f.H.Lifecycle.SetFocus(true);
                Assert.That(f.Toasts.Values, Is.EqualTo(new[] { WorkoutToast.Completed }));
            }
        }

        [Test]
        public void DisposeStopsEverything()
        {
            using (var f = new Fixture())
            {
                f.Store.Dispose();
                Assert.That(f.Store.StartFree(), Is.False);
                Assert.That(f.Store.TapRoutine(RoutineSeeds.Corta()), Is.EqualTo(RoutineTapOutcome.Ignored));
                Assert.That(f.Store.StartRoutine(RoutineSeeds.Corta()), Is.False);
                f.H.Rower.SetStatus(BleStatus.Disconnected);
                Assert.That(f.Store.State.Value.RowerConnected, Is.True, "ya no sigue al remo");
                f.Store.Dispose();
            }
        }

        private static Routine Find(RoutinesStore store, string name)
        {
            foreach (var routine in store.State.Value.Routines)
            {
                if (routine.Name == name)
                {
                    return routine;
                }
            }

            throw new InvalidOperationException("no está " + name);
        }

        private static List<string> Names(IReadOnlyList<Routine> routines)
        {
            var names = new List<string>();
            foreach (var routine in routines)
            {
                names.Add(routine.Name);
            }

            return names;
        }

        // El motor de WorkoutHarness (FakeScheduler, remo R-4 conectado), el dispositivo, la colección cargada y el store.
        private sealed class Fixture : IDisposable
        {
            public Fixture(IEnumerable<Routine>? routines = null, Func<FakeScheduler, ISessionRepository>? sessions = null)
            {
                var logger = new RecordingLogger();
                H = new WorkoutHarness(sessionsFor: sessions);
                H.At(0);
                Device = new DeviceStore(H.Rower, logger);
                Repository = new InMemoryRoutineRepository(routines ?? RoutineSeeds.F4A.Routines);
                Routines = new RoutinesStore(Repository, new SequenceClock(), logger);
                Assert.That(Routines.LoadAsync().IsCompleted, Is.True);
                Store = new WorkoutStore(H.Engine, Device, Routines, H.Lifecycle, logger);
                Toasts = new StateRecorder<string>(Store.Toasts);
            }

            public WorkoutHarness H { get; }

            public DeviceStore Device { get; }

            public InMemoryRoutineRepository Repository { get; }

            public RoutinesStore Routines { get; }

            public WorkoutStore Store { get; }

            public StateRecorder<string> Toasts { get; }

            // Las sesiones guardadas (de la base en memoria o de la de guion).
            public InMemorySessionRepository Sessions => H.Memory ?? ((ScriptedSessionRepository)H.Sessions).Store;

            public void Dispose()
            {
                Toasts.Dispose();
                Store.Dispose();
                Routines.Dispose();
                Device.Dispose();
                H.Dispose();
            }
        }
    }
}
