using System;
using NUnit.Framework;
using RowMate.Core.Models;
using RowMate.Core.Workout;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Workout
{
    // WSU: P-17 de la 4a con el motor de verdad (AC-WKT-02-11, -04; 02-tech-design §2.9 de la It. 4b): el tiempo con el equipo
    // suspendido no cuenta. Con el ciclo de vida de prueba (LifecycleHarness: el FrameScheduler de la app con SuspendAccounting,
    // a 60 fps), en los dos casos del monótono (el crudo cuenta el sueño o no, QA-V4a-ROB-05) y con aviso, con medida o con los
    // dos. Un libre con R-4, suspendido a los 62,4 s por 600 s: al despertar el reloj dice 01:02; los puntos son los de los
    // segundos en marcha 5 a 60 y después 65, 70…; terminada a los 120 s en marcha, 25 puntos (24 + el final). Sin ninguna señal
    // y con un crudo que contó, 145 (la conducta de la 4a, AC-WKT-02-04). El legacy cuenta disparos de un Timer.periodic(1 s)
    // que no corre con el equipo dormido (workout_provider.dart:147, :247).
    [TestFixture]
    public sealed class WorkoutSuspendTests
    {
        [TestCase(true, true, false, Category = "AC-WKT-02-11,AC-WKT-02-08", TestName = "SuspendedFreeSession_RawCounts_Signal")]
        [TestCase(true, false, true, Category = "AC-WKT-02-11,AC-WKT-02-08", TestName = "SuspendedFreeSession_RawCounts_Measure")]
        [TestCase(true, true, true, Category = "AC-WKT-02-11,AC-WKT-02-08", TestName = "SuspendedFreeSession_RawCounts_SignalAndMeasure")]
        [TestCase(false, true, false, Category = "AC-WKT-02-11,AC-WKT-02-08", TestName = "SuspendedFreeSession_RawStops_Signal")]
        [TestCase(false, false, true, Category = "AC-WKT-02-11,AC-WKT-02-08", TestName = "SuspendedFreeSession_RawStops_Measure")]
        [TestCase(false, true, true, Category = "AC-WKT-02-11,AC-WKT-02-08", TestName = "SuspendedFreeSession_RawStops_SignalAndMeasure")]
        public void SuspendedFreeSession(bool monotonicCounts, bool signal, bool measured)
        {
            using (var run = new Run())
            {
                run.Harness.RunUntil(0);
                run.Engine.StartFree();
                run.Harness.Suspend(62.4, signal);
                var snapshots = new StateRecorder<WorkoutSnapshot>(run.Engine.State);
                run.Harness.Wake(600, monotonicCounts, measured, resumeSignal: signal);
                Assert.That(run.Engine.State.Value.TotalElapsedSeconds, Is.EqualTo(62), "al despertar el reloj dice 01:02");
                Assert.That(WorkoutFormats.SessionClock(run.Engine.State.Value.TotalElapsedSeconds), Is.EqualTo("01:02"));
                Assert.That(run.Harness.Lifecycle.WakeCount, Is.GreaterThanOrEqualTo(1), "IAppLifecycle.Woke");
                Assert.That(snapshots.Count, Is.GreaterThan(0));
                Assert.That(snapshots.Values[0].Instant, Is.True, "el primer cuadro despierto ya muestra el presente (AC-WKT-02-08)");
                Assert.That(run.Harness.Logger is RecordingLogger logger && logger.Contains("el equipo durmió"), Is.True, "el registro lo dice");
                snapshots.Dispose();
                Finish(run);
                var points = run.Sessions.PointsOf(1);
                Assert.That(points.Count, Is.EqualTo(25), "24 puntos de 5 en 5 y el final, no 145");
                Assert.That(points[11].ElapsedSeconds - points[10].ElapsedSeconds, Is.EqualTo(5), "60 y 65 sin el lapso");
            }
        }

        [Test, Category("AC-WKT-02-11")]
        public void ResumeBeforeAndAfterTheFirstAwakeFrame()
        {
            // QA-G4bT-05: con el "reanuda" antes y después del primer cuadro despierto, 25.
            foreach (var before in new[] { true, false })
            {
                using (var run = new Run())
                {
                    run.Harness.RunUntil(0);
                    run.Engine.StartFree();
                    run.Harness.Suspend(62.4);
                    run.Harness.Wake(600, monotonicCounts: true, measured: false, resumeBeforeFirstFrame: before);
                    Assert.That(run.Engine.State.Value.TotalElapsedSeconds, Is.EqualTo(62));
                    Finish(run);
                    Assert.That(run.Sessions.PointsOf(1).Count, Is.EqualTo(25), before ? "antes" : "después");
                }
            }
        }

        [Test, Category("AC-WKT-02-11")]
        public void StallBeforeTheSleep()
        {
            // QA-G4bT-05 (b): un tirón de 1,2 s entre el aviso y el sueño se descuenta (y queda en el registro); el sueño también.
            using (var run = new Run())
            {
                run.Harness.RunUntil(0);
                run.Engine.StartFree();
                run.Harness.Suspend(62.0);
                run.Harness.Stall(1.2);
                run.Harness.RunUntil(62.4);
                run.Harness.Wake(600, monotonicCounts: true, measured: false);
                Assert.That(run.Engine.State.Value.TotalElapsedSeconds, Is.EqualTo(62));
                Assert.That(((RecordingLogger)run.Harness.Logger).Contains("se descuentan"), Is.True, "el tirón, en el registro");
                Finish(run);
                Assert.That(run.Sessions.PointsOf(1).Count, Is.EqualTo(25));
            }
        }

        [Test, Category("AC-WKT-02-11")]
        public void SuspendedDuringTheBackground()
        {
            // QA-G4bT-05 (a): la suspensión durante el segundo plano y la vuelta por ResumeFromBackground: el alcance va hasta el
            // monótono despierto, 25 (no 145).
            using (var run = new Run())
            {
                run.Harness.RunUntil(0);
                run.Engine.StartFree();
                run.Harness.Background(62.4);
                run.Harness.Suspend(62.4, signal: true, framesAfterSignal: 0);
                run.Harness.Wake(600, monotonicCounts: true, measured: true, viaBackground: true);
                Assert.That(run.Engine.State.Value.TotalElapsedSeconds, Is.EqualTo(62));
                Assert.That(run.Engine.State.Value.Instant, Is.True);
                Finish(run);
                Assert.That(run.Sessions.PointsOf(1).Count, Is.EqualTo(25));
            }
        }

        [Test, Category("AC-WKT-02-04")]
        public void WithoutEvidenceItCountsLikeALateFrame()
        {
            // Sin aviso ni medida y con un crudo que contó, el lapso cuenta como un cuadro tarde (la 4a): 145 puntos. Queda escrito.
            using (var run = new Run())
            {
                run.Harness.RunUntil(0);
                run.Engine.StartFree();
                run.Harness.Suspend(62.4, signal: false);
                run.Harness.Wake(600, monotonicCounts: true, measured: false, resumeSignal: false);
                Assert.That(run.Engine.State.Value.TotalElapsedSeconds, Is.EqualTo(662));
                Assert.That(run.Harness.Lifecycle.WakeCount, Is.EqualTo(0), "sin evidencia no hay despertar");
                Assert.That(((RecordingLogger)run.Harness.Logger).Contains("sin aviso de suspensión: cuenta"), Is.True);
                Finish(run);
                Assert.That(run.Sessions.PointsOf(1).Count, Is.EqualTo(145));
            }
        }

        [Test, Category("AC-WKT-02-11")]
        public void PausedStaysPausedAndTheDialogStaysOpen()
        {
            using (var run = new Run())
            {
                run.Harness.RunUntil(0);
                run.Engine.StartFree();
                run.Harness.RunUntil(30.2);
                run.Engine.Pause();
                run.Harness.Suspend(62.4);
                run.Harness.Wake(600, monotonicCounts: true, measured: true);
                Assert.That(run.Engine.State.Value.Phase, Is.EqualTo(WorkoutPhase.Paused), "en pausa sigue en pausa");
                Assert.That(run.Engine.State.Value.TotalElapsedSeconds, Is.EqualTo(30));
            }

            using (var run = new Run())
            {
                run.Harness.RunUntil(0);
                run.Engine.StartWithRoutine(RoutineSeeds.Corta());
                run.Harness.RunUntil(31);
                run.Harness.Suspend(40);
                run.Harness.Wake(600, monotonicCounts: true, measured: true);
                Assert.That(run.Engine.State.Value.Phase, Is.EqualTo(WorkoutPhase.Finished), "el diálogo sigue abierto");
                Assert.That(run.Engine.State.Value.Close, Is.EqualTo(CloseStatus.Saved));
                Assert.That(run.Notices.Count, Is.EqualTo(0));
            }
        }

        [Test, Category("AC-WKT-02-11")]
        public void TheCloseTimeoutDoesNotCountTheSleep()
        {
            // El cierre (ForegroundTimeout de 15 s) mide con el reloj despierto: no hay workoutSaveError falso.
            var harness = new LifecycleHarness(WorkoutHarness.SessionStart);
            var rower = new ScriptedRower(harness.Scheduler, RowerScripts.R4, harness.Logger);
            var sessions = new ScriptedSessionRepository(harness.Scheduler) { FinishDelay = TimeSpan.FromSeconds(10) };
            using (var engine = new WorkoutEngine(rower, new ScriptedHeartRateService(harness.Logger), sessions, harness.Scheduler, harness.Lifecycle,
                       harness.Logger))
            using (var notices = new StateRecorder<WorkoutNotice>(engine.Notices))
            {
                rower.Start();
                harness.RunUntil(0);
                engine.StartFree();
                harness.RunUntil(60.2);
                engine.FinishAsync();
                harness.Suspend(62.4);
                harness.Wake(600, monotonicCounts: true, measured: true);
                Assert.That(engine.State.Value.Phase, Is.EqualTo(WorkoutPhase.Finished), "el cierre sigue en vuelo");
                harness.RunUntil(71);
                Assert.That(engine.State.Value.Phase, Is.EqualTo(WorkoutPhase.Idle));
                Assert.That(notices.Values[0].ToString(), Is.EqualTo("Ended(Manual, true)"), "ningún workoutSaveError");
            }

            rower.Dispose();
        }

        [Test, Category("AC-WKT-02-11")]
        public void TargetsAndDistanceStepWithoutTheLapse()
        {
            // Los objetivos no reciben lecturas del lapso y el paso por distancia no salta: Final por distancia con R-4 termina a
            // los 35 s en marcha, durmiendo en el medio del paso de 100 m.
            using (var run = new Run())
            {
                run.Harness.RunUntil(0);
                run.Engine.StartWithRoutine(RoutineSeeds.FinalPorDistancia());
                run.Harness.Suspend(20.4);
                run.Harness.Wake(600, monotonicCounts: true, measured: true);
                Assert.That(run.Engine.State.Value.Step!.Value.StepIndex, Is.EqualTo(1));
                run.Harness.RunUntil(34.8);
                Assert.That(run.Engine.State.Value.Phase, Is.EqualTo(WorkoutPhase.Active));
                run.Harness.RunUntil(35.1);
                Assert.That(run.Engine.State.Value.Phase, Is.EqualTo(WorkoutPhase.Finished), "a los 35 s en marcha");
                Assert.That(run.Engine.State.Value.TotalElapsedSeconds, Is.EqualTo(35));
                Assert.That(run.Engine.Targets.ResultOf(0), Is.EqualTo(StepResult.Met));
            }
        }

        // Lo que quedaba hasta los 120 s en marcha (57,6 s del reloj despierto después de dormirse a los 62,4) y Terminar.
        private static void Finish(Run run)
        {
            run.Harness.RunUntil(run.Harness.Now + (120 - 62.4));
            run.Engine.FinishAsync();
        }

        private sealed class Run : IDisposable
        {
            public Run()
            {
                Harness = new LifecycleHarness(WorkoutHarness.SessionStart);
                Rower = new ScriptedRower(Harness.Scheduler, RowerScripts.R4, Harness.Logger);
                Engine = new WorkoutEngine(Rower, new ScriptedHeartRateService(Harness.Logger), Sessions, Harness.Scheduler, Harness.Lifecycle,
                    Harness.Logger);
                Notices = new StateRecorder<WorkoutNotice>(Engine.Notices);
                Rower.Start();
            }

            public LifecycleHarness Harness { get; }

            public ScriptedRower Rower { get; }

            public InMemorySessionRepository Sessions { get; } = new InMemorySessionRepository();

            public WorkoutEngine Engine { get; }

            public StateRecorder<WorkoutNotice> Notices { get; }

            public void Dispose()
            {
                Notices.Dispose();
                Engine.Dispose();
                Rower.Dispose();
            }
        }
    }
}
