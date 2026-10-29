using System;
using System.Collections.Generic;
using NUnit.Framework;
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
    }
}
