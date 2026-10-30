using System;
using System.Collections.Generic;
using NUnit.Framework;
using RowMate.Core.Common;
using RowMate.Platform.Power;
using RowMate.Platform.Time;
using RowMate.Tests.Support;

namespace RowMate.Tests.UnityEditMode
{
    // USC · El programador de la app sobre FrameScheduler, con un reloj inyectado (04a-motor-hud-funcional/02-tech-design §4.1;
    // ADR 017): un cuadro corre lo vencido con la hora del cuadro; el paso a segundo plano solo anota y la vuelta corre el
    // alcance en orden y en tiempo virtual, con IsCatchingUp mientras dura; volver sin haberse ido no hace nada.
    public sealed class UnitySchedulerTests
    {
        private double _monotonic;
        private DateTimeOffset _utc;

        private static readonly DateTimeOffset Start = new DateTimeOffset(2026, 10, 28, 13, 0, 0, TimeSpan.Zero);

        private UnityScheduler NewScheduler()
        {
            _monotonic = 100;
            _utc = Start;
            return new UnityScheduler(new RecordingLogger(), () => _monotonic, () => _utc, _ => TimeSpan.FromHours(-3));
        }

        private void Move(double seconds)
        {
            _monotonic += seconds;
            _utc = _utc.AddSeconds(seconds);
        }

        [Test, Category("AC-WKT-02-05")]
        public void TickRunsWhatIsDueWithTheFrameTime()
        {
            var scheduler = NewScheduler();
            var seen = new List<double>();
            scheduler.Schedule(TimeSpan.FromSeconds(1), () => seen.Add(scheduler.MonotonicSeconds));
            Move(0.5);
            Assert.That(scheduler.Tick(), Is.EqualTo(0), "todavía no");
            Move(0.6);
            Assert.That(scheduler.Tick(), Is.EqualTo(1));
            Assert.That(seen, Is.EqualTo(new[] { 101.1 }).Within(1e-9), "la hora del cuadro");
            Assert.That(scheduler.LocalNow.Offset, Is.EqualTo(TimeSpan.FromHours(-3)));
            Assert.That(scheduler.UtcNow, Is.EqualTo(Start.AddSeconds(1.1)));
        }

        [Test, Category("AC-WKT-02-05"), Category("AC-WKT-02-06")]
        public void EnterAndResumeRunTheCatchUpInOrder()
        {
            var scheduler = NewScheduler();
            var ticks = new List<(double At, bool CatchingUp)>();
            scheduler.SchedulePeriodic(TimeSpan.FromSeconds(1), () => ticks.Add((scheduler.MonotonicSeconds, scheduler.IsCatchingUp)));
            Move(1);
            scheduler.Tick();
            Assert.That(ticks.Count, Is.EqualTo(1));
            scheduler.EnterBackground();
            Assert.That(scheduler.InBackground, Is.True);
            Move(5);
            // En segundo plano no hay cuadros: nada corre hasta la vuelta.
            Assert.That(ticks.Count, Is.EqualTo(1));
            var ran = scheduler.ResumeFromBackground();
            Assert.That(ran, Is.EqualTo(5), "el alcance: cada segundo que faltaba, en orden");
            Assert.That(scheduler.InBackground, Is.False);
            Assert.That(scheduler.IsCatchingUp, Is.False, "terminó");
            for (var i = 1; i < ticks.Count; i++)
            {
                Assert.That(ticks[i].At, Is.EqualTo(101 + i).Within(1e-9), "en tiempo virtual, sin reanclar");
                Assert.That(ticks[i].CatchingUp, Is.True, "con IsCatchingUp mientras dura");
            }

            Move(1);
            Assert.That(scheduler.Tick(), Is.EqualTo(1), "después, un cuadro = un disparo");
        }

        [Test, Category("AC-WKT-02-05")]
        public void ResumeWithoutEnterIsANoOp()
        {
            var scheduler = NewScheduler();
            var count = 0;
            scheduler.SchedulePeriodic(TimeSpan.FromSeconds(1), () => count++);
            Move(3);
            Assert.That(scheduler.ResumeFromBackground(), Is.EqualTo(0), "Unity llama OnApplicationPause(false) al arrancar: nada");
            Assert.That(count, Is.EqualTo(0));
            Assert.That(scheduler.Tick(), Is.EqualTo(1), "en primer plano, a lo sumo un disparo de cada periódico por cuadro");
        }

        // It. 4b (P-17 de la 4a; 04b-hud-exr/02-tech-design §2.9; QA-G4bT-05 (a)): la fuente de sueño entra en el FrameClock del
        // cuadro Y de la vuelta del segundo plano; lo dormido no cuenta para el reloj de la app, y Woke sale después de lo vencido.
        private sealed class TestSleep : ISystemSleepSource
        {
            public double AsleepSeconds { get; set; }

            public int SuspendSignals { get; set; }

            public int ResumeSignals { get; set; }

            public bool SignalsRegistered => false;

            public void Dispose()
            {
            }
        }

        private UnityScheduler NewScheduler(ISystemSleepSource sleep)
        {
            _monotonic = 100;
            _utc = Start;
            return new UnityScheduler(new RecordingLogger(), () => _monotonic, () => _utc, _ => TimeSpan.FromHours(-3), sleep);
        }

        [Test, Category("AC-WKT-02-11")]
        public void TheSleepMeasureIsDiscountedInTheFrame()
        {
            var sleep = new TestSleep { AsleepSeconds = 10 };
            var scheduler = NewScheduler(sleep);
            var woke = new List<SystemWake>();
            scheduler.Woke += woke.Add;
            var fired = new List<double>();
            scheduler.SchedulePeriodic(TimeSpan.FromSeconds(1), () => fired.Add(scheduler.MonotonicSeconds));
            Move(1);
            scheduler.Tick();
            Assert.That(fired.Count, Is.EqualTo(1));
            // El equipo duerme 600 s y el crudo los cuenta: el reloj de la app no.
            sleep.AsleepSeconds += 600;
            Move(600);
            scheduler.Tick();
            Assert.That(scheduler.MonotonicSeconds, Is.EqualTo(101).Within(0.05), "el lapso dormido no existe para la app");
            Assert.That(scheduler.Frames.SleepBias, Is.EqualTo(600).Within(0.05));
            Assert.That(woke.Count, Is.EqualTo(1), "un despertar");
            Assert.That(woke[0].Evidence & SleepEvidence.Measure, Is.EqualTo(SleepEvidence.Measure));
            Assert.That(fired.Count, Is.EqualTo(1), "ningún segundo del lapso");
        }

        [Test, Category("AC-WKT-02-11")]
        public void TheSleepSourceAlsoGoesThroughTheBackgroundReturn()
        {
            var sleep = new TestSleep { AsleepSeconds = 0 };
            var scheduler = NewScheduler(sleep);
            var woke = 0;
            scheduler.Woke += _ => woke++;
            var count = 0;
            scheduler.SchedulePeriodic(TimeSpan.FromSeconds(1), () => count++);
            Move(1);
            scheduler.Tick();
            scheduler.EnterBackground();
            sleep.AsleepSeconds += 300;
            Move(305);
            var ran = scheduler.ResumeFromBackground();
            Assert.That(ran, Is.EqualTo(5), "el alcance hasta el monótono despierto: 5 s, no 305");
            Assert.That(woke, Is.EqualTo(1), "Woke después del alcance");
        }

        [Test, Category("AC-WKT-19-04")]
        public void TheLocalOffsetIsReadEveryHalfSecond()
        {
            var offset = TimeSpan.FromHours(-3);
            _monotonic = 100;
            _utc = Start;
            var scheduler = new UnityScheduler(new RecordingLogger(), () => _monotonic, () => _utc, _ => offset, NullSleepSource.Instance);
            Assert.That(UnityScheduler.OffsetRefreshSeconds, Is.EqualTo(0.5));
            offset = TimeSpan.FromHours(-5);
            Move(0.3);
            scheduler.Tick();
            Assert.That(scheduler.LocalNow.Offset, Is.EqualTo(TimeSpan.FromHours(-3)), "todavía no");
            Move(0.3);
            scheduler.Tick();
            Assert.That(scheduler.LocalNow.Offset, Is.EqualTo(TimeSpan.FromHours(-5)), "a los 0,5 s");
        }
    }
}
