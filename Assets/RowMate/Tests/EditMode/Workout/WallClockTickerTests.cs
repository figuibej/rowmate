using System;
using NUnit.Framework;
using RowMate.App.Workout;
using RowMate.Core.Common;
using RowMate.Core.Workout;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Workout
{
    // WCL: el reloj de pared (F-WKT-19; AC-WKT-19-01 a -04; 02-tech-design §2.8 de la It. 4b), con su propio ritmo. La zona de
    // prueba es UTC−03:00 (spec §0.4): 13:00:00.000Z se ve 10:00. legacy: lib/features/workout/series_panels.dart:57-82
    // (WallClock: Timer.periodic de 1 s, redibuja solo si cambió el minuto; DateFormat('HH:mm')).
    [TestFixture]
    public sealed class WallClockTickerTests
    {
        private static readonly DateTimeOffset Start = new DateTimeOffset(2026, 10, 28, 13, 0, 0, TimeSpan.Zero);

        [Test, Category("AC-WKT-19-01")]
        public void Format24Hours()
        {
            // HH:mm de 24 h, siempre (Q-06; AC-WKT-11-12 de la 4a), aunque el sistema use 12 h.
            Assert.That(WorkoutFormats.WallClock(new DateTimeOffset(2026, 10, 28, 9, 5, 0, TimeSpan.FromHours(-3))), Is.EqualTo("09:05"));
            Assert.That(WorkoutFormats.WallClock(new DateTimeOffset(2026, 10, 28, 0, 0, 0, TimeSpan.FromHours(-3))), Is.EqualTo("00:00"));
            Assert.That(WorkoutFormats.WallClock(new DateTimeOffset(2026, 10, 28, 21, 30, 0, TimeSpan.FromHours(-3))), Is.EqualTo("21:30"));
            using (var f = new Fixture(Start))
            {
                Assert.That(f.Ticker.State.Value.Text, Is.EqualTo("10:00"));
                Assert.That(f.Ticker.State.Value.MinuteOfDay, Is.EqualTo(600));
                Assert.That(f.Ticker.State.Value.Instant, Is.True, "el primer estado, sin rodillo");
            }
        }

        [Test, Category("AC-WKT-19-02")]
        public void MinuteChangesWithinASecond()
        {
            // A los 13:00:59.5Z dice 10:00 y a más tardar desde los 13:01:00.25Z, 10:01 (la spec pide desde los 13:01:01.0Z), con
            // rodillo (no Instant).
            using (var f = new Fixture(Start.AddSeconds(59.5)))
            {
                Assert.That(f.Ticker.State.Value.Text, Is.EqualTo("10:00"));
                f.Scheduler.Advance(TimeSpan.FromSeconds(0.75));
                var state = f.Ticker.State.Value;
                Assert.That(state.Text, Is.EqualTo("10:01"));
                Assert.That(state.MinuteOfDay, Is.EqualTo(601));
                Assert.That(state.Instant, Is.False, "el minuto siguiente: con rodillo");
                Assert.That(WallClockTicker.CheckPeriodSeconds, Is.EqualTo(0.25));
            }
        }

        [Test, Category("AC-WKT-19-02")]
        public void DoesNotRedrawBetweenMinutes()
        {
            using (var f = new Fixture(Start))
            {
                f.Scheduler.Advance(TimeSpan.FromSeconds(180));
                Assert.That(f.States.Count, Is.EqualTo(3), "10:01, 10:02 y 10:03: no late cada segundo");
                Assert.That(f.States.Last.Text, Is.EqualTo("10:03"));
            }
        }

        [Test, Category("AC-WKT-19-02")]
        public void MidnightRollsDown()
        {
            // A medianoche el minuto del día vuelve a 0: el siguiente de 23:59 es 00:00, con rodillo (hacia abajo, en la vista).
            using (var f = new Fixture(new DateTimeOffset(2026, 10, 29, 2, 59, 59, TimeSpan.Zero)))
            {
                Assert.That(f.Ticker.State.Value.Text, Is.EqualTo("23:59"));
                f.Scheduler.Advance(TimeSpan.FromSeconds(1));
                Assert.That(f.Ticker.State.Value.Text, Is.EqualTo("00:00"));
                Assert.That(f.Ticker.State.Value.MinuteOfDay, Is.EqualTo(0));
                Assert.That(f.Ticker.State.Value.Instant, Is.False);
            }
        }

        [Test, Category("AC-WKT-19-03")]
        public void RunsInPauseFinishedAndDialog()
        {
            // Sigue en pausa, terminada y con el diálogo de completado abierto: no depende del reloj de la sesión.
            using (var h = new WorkoutHarness())
            using (var hud = new WorkoutHudStore(h.Engine, Texts(), h.Scheduler, h.Lifecycle, h.Logger))
            {
                h.Routine(RoutineSeeds.Corta());
                h.At(5);
                h.Engine.Pause();
                h.At(61);
                Assert.That(hud.WallClock.Value.Text, Is.EqualTo("10:01"), "en pausa");
                h.Engine.Resume();
                h.At(100);
                Assert.That(h.Engine.Phase, Is.EqualTo(WorkoutPhase.Finished), "terminada, con el diálogo");
                h.At(125);
                Assert.That(hud.WallClock.Value.Text, Is.EqualTo("10:02"));
            }
        }

        [Test, Category("AC-WKT-19-03")]
        public void WakeAndForegroundPublishInstant()
        {
            // Al volver del segundo plano y al despertar el equipo (P-17 de la 4a), una vez, con Instant (sin rodillo).
            using (var f = new Fixture(Start))
            {
                f.Lifecycle.SetBackground(true);
                f.Scheduler.Advance(TimeSpan.FromMinutes(5));
                f.Lifecycle.SetBackground(false);
                Assert.That(f.States.Last.Text, Is.EqualTo("10:05"));
                Assert.That(f.States.Last.Instant, Is.True);
                f.Scheduler.Advance(TimeSpan.FromMinutes(3));
                Assert.That(f.States.Last.Instant, Is.False, "después, con rodillo");
                var before = f.States.Count;
                f.Lifecycle.SignalWake(new SystemWake(600, 600, SleepEvidence.Measure));
                Assert.That(f.States.Count, Is.EqualTo(before + 1));
                Assert.That(f.States.Last.Instant, Is.True, "al despertar");
            }
        }

        [Test, Category("AC-WKT-19-03")]
        public void NoPeriodicInBackground()
        {
            // QA-G4bT-08 (c): sin periódico en segundo plano, así el alcance no lo dispara (si no, 4 disparos por segundo virtual
            // que no publican nada: 14 400 en una hora). Al volver se arma otra vez.
            using (var f = new Fixture(Start))
            {
                Assert.That(f.Ticker.IsTicking, Is.True);
                f.Lifecycle.SetBackground(true);
                Assert.That(f.Ticker.IsTicking, Is.False);
                Assert.That(f.Scheduler.PendingTimers, Is.EqualTo(0), "ningún periódico durante el alcance");
                f.Scheduler.Advance(TimeSpan.FromHours(1));
                Assert.That(f.States.Count, Is.EqualTo(0), "0 disparos en una hora de segundo plano");
                f.Lifecycle.SetBackground(false);
                Assert.That(f.Ticker.IsTicking, Is.True);
                Assert.That(f.States.Count, Is.EqualTo(1));
                Assert.That(f.States.Last.Text, Is.EqualTo("11:00"));
                f.Ticker.Dispose();
                Assert.That(f.Ticker.IsTicking, Is.False);
                Assert.That(f.Scheduler.PendingTimers, Is.EqualTo(0), "Dispose para el periódico");
            }
        }

        [Test, Category("AC-WKT-19-04")]
        public void ZoneChangeShowsTheNewHourWithinASecond()
        {
            // La zona del equipo de UTC−03:00 a UTC−05:00 a mitad de la sesión: 10:00 → 08:00 antes de 1 s, sin rodillo. El
            // cambio de hora a mano también.
            using (var f = new Fixture(Start.AddSeconds(10)))
            {
                f.Scheduler.SetLocalOffset(TimeSpan.FromHours(-5));
                f.Scheduler.Advance(TimeSpan.FromSeconds(0.25));
                Assert.That(f.Ticker.State.Value.Text, Is.EqualTo("08:00"));
                Assert.That(f.Ticker.State.Value.Instant, Is.True, "no es el minuto siguiente");
                f.Scheduler.ShiftUtc(TimeSpan.FromMinutes(90));
                f.Scheduler.Advance(TimeSpan.FromSeconds(0.25));
                Assert.That(f.Ticker.State.Value.Text, Is.EqualTo("09:30"));
                Assert.That(f.Ticker.State.Value.Instant, Is.True);
            }
        }

        [Test, Category("AC-WKT-19-04")]
        public void SessionClockUnaffected()
        {
            // El reloj de la sesión, la cuenta, los pasos y los puntos no cambian: cuentan con el monótono.
            using (var h = new WorkoutHarness())
            using (var hud = new WorkoutHudStore(h.Engine, Texts(), h.Scheduler, h.Lifecycle, h.Logger))
            {
                h.Routine(RoutineSeeds.PruebaCuatroA());
                h.At(20);
                h.Scheduler.SetLocalOffset(TimeSpan.FromHours(-5));
                h.Scheduler.ShiftUtc(TimeSpan.FromHours(3));
                h.At(40.2);
                Assert.That(hud.WallClock.Value.Text, Is.EqualTo("11:00"), "la hora nueva");
                Assert.That(h.State.TotalElapsedSeconds, Is.EqualTo(40));
                Assert.That(h.State.Step!.Value.StepIndex, Is.EqualTo(1));
                Assert.That(h.State.Step!.Value.ElapsedInStep, Is.EqualTo(10));
                Assert.That(h.Points().Count, Is.EqualTo(8));
            }
        }

        private static IReadOnlyObservable<RowMate.Core.Localization.Localizer> Texts()
        {
            return new ReadOnlyObservable<RowMate.Core.Localization.Localizer>(
                new ObservableValue<RowMate.Core.Localization.Localizer>(RowMate.Tests.EditMode.Localization.LocalizationFixture.Es, NullAppLogger.Instance));
        }

        private sealed class Fixture : IDisposable
        {
            public Fixture(DateTimeOffset utc)
            {
                Logger = new RecordingLogger();
                Scheduler = new FakeScheduler(utc, TimeSpan.FromHours(-3));
                Lifecycle = new FakeAppLifecycle(Logger);
                Ticker = new WallClockTicker(Scheduler, Lifecycle, Logger);
                States = new StateRecorder<WallClockState>(Ticker.State);
            }

            public RecordingLogger Logger { get; }

            public FakeScheduler Scheduler { get; }

            public FakeAppLifecycle Lifecycle { get; }

            public WallClockTicker Ticker { get; }

            public StateRecorder<WallClockState> States { get; }

            public void Dispose()
            {
                States.Dispose();
                Ticker.Dispose();
            }
        }
    }
}
