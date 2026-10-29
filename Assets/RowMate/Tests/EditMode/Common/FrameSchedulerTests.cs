using System;
using System.Collections.Generic;
using NUnit.Framework;
using RowMate.Core.Common;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Common
{
    // FSC: FrameScheduler, el IScheduler de la app (02-tech-design §4.1 de la It. 4a; ADR 017, decisión 3). En primer plano
    // es la conducta de la It. 2 (un disparo por cuadro, reanclado); al volver del segundo plano, el alcance en tiempo virtual.
    [TestFixture]
    public sealed class FrameSchedulerTests
    {
        private const double Frame = 1.0 / 60;
        private static readonly DateTimeOffset Start = new DateTimeOffset(2026, 10, 28, 13, 0, 0, TimeSpan.Zero);
        private static readonly TimeSpan Offset = TimeSpan.FromHours(-3);

        [Test, Category("AC-WKT-02-04")]
        public void TickIsTheRunDueOfIterationTwo()
        {
            // Un periódico de 1 s: cuadro a cuadro dispara en el primer cuadro que pasa cada segundo; un salto de 5 s dispara
            // una vez y se reancla (QA-G2-15): las lecturas perdidas en primer plano se pierden, como en la It. 2 (DT4a-10).
            var scheduler = Build(out var _);
            var fired = new List<double>();
            scheduler.SchedulePeriodic(TimeSpan.FromSeconds(1), () => fired.Add(scheduler.MonotonicSeconds));
            for (var frame = 1; frame <= 130; frame++)
            {
                scheduler.Tick(frame * Frame, Start.AddSeconds(frame * Frame), Offset);
            }

            Assert.That(fired.Count, Is.EqualTo(2));
            Assert.That(scheduler.Tick(7.0, Start.AddSeconds(7), Offset), Is.EqualTo(1), "el salto: un solo disparo");
            Assert.That(scheduler.Tick(7.5, Start.AddSeconds(7.5), Offset), Is.EqualTo(0));
            Assert.That(scheduler.Tick(8.0, Start.AddSeconds(8), Offset), Is.EqualTo(1), "reanclado en 7 + 1");
            Assert.That(scheduler.IsCatchingUp, Is.False);
        }

        [Test]
        public void TheTimeIsTheFrameTime()
        {
            // La hora es la del cuadro (03-qa-harmony-tecnica §5): entre dos Tick no cambia aunque pase tiempo real.
            var scheduler = Build(out var _);
            scheduler.Tick(2.5, Start.AddSeconds(2.5), Offset);
            Assert.That(scheduler.MonotonicSeconds, Is.EqualTo(2.5));
            Assert.That(scheduler.UtcNow, Is.EqualTo(Start.AddSeconds(2.5)));
            Assert.That(scheduler.LocalNow.Offset, Is.EqualTo(Offset));
            Assert.That(scheduler.LocalNow.UtcDateTime, Is.EqualTo(Start.AddSeconds(2.5).UtcDateTime));
            scheduler.Tick(2.0, Start.AddSeconds(3), Offset);
            Assert.That(scheduler.MonotonicSeconds, Is.EqualTo(2.5), "el monótono nunca retrocede");
        }

        [Test, Category("AC-WKT-02-05")]
        public void CatchUpFiresEverythingInOrderWithVirtualTime()
        {
            // Un periódico de 1 s (el remo) y un timer de una vez que se rearma cada segundo (el motor): en el alcance de 0 a
            // 10 s disparan intercalados por vencimiento, cada uno con su hora virtual, y los periódicos no se reanclan.
            var scheduler = Build(out var _);
            var log = new List<string>();
            scheduler.SchedulePeriodic(TimeSpan.FromSeconds(1), () => log.Add("R" + Fmt(scheduler.MonotonicSeconds)));
            Action? engine = null;
            engine = () =>
            {
                log.Add("E" + Fmt(scheduler.MonotonicSeconds) + (scheduler.IsCatchingUp ? "*" : ""));
                scheduler.Schedule(TimeSpan.FromSeconds(1), engine!);
            };
            scheduler.Schedule(TimeSpan.FromSeconds(0.5), engine);

            scheduler.EnterBackground();
            Assert.That(scheduler.InBackground, Is.True);
            var fired = scheduler.ResumeFromBackground(10.0, Start.AddSeconds(10), Offset);

            Assert.That(fired, Is.EqualTo(20));
            Assert.That(log[0], Is.EqualTo("E0.5*"));
            Assert.That(log[1], Is.EqualTo("R1"));
            Assert.That(log[log.Count - 2], Is.EqualTo("E9.5*"));
            Assert.That(log[log.Count - 1], Is.EqualTo("R10"));
            for (var i = 0; i < log.Count; i += 2)
            {
                Assert.That(log[i], Does.StartWith("E"), "intercalados: " + string.Join(" ", log));
            }

            Assert.That(scheduler.IsCatchingUp, Is.False);
            Assert.That(scheduler.InBackground, Is.False);
            Assert.That(scheduler.MonotonicSeconds, Is.EqualTo(10.0), "al terminar, la hora de la vuelta");
            log.Clear();
            scheduler.Tick(10.5, Start.AddSeconds(10.5), Offset);
            Assert.That(log, Is.EqualTo(new[] { "E10.5" }), "después, en tiempo real y sin alcance");
        }

        [Test, Category("AC-WKT-02-06")]
        public void VirtualWallClockDuringTheCatchUp()
        {
            // UtcNow = hora de la vuelta − (ahora − virtual): la hora de fin de una rutina que termina en el alcance es la de su
            // segundo (AC-WKT-02-06), no la de la vuelta.
            var scheduler = Build(out var _);
            var seen = new List<DateTimeOffset>();
            scheduler.Schedule(TimeSpan.FromSeconds(30), () => seen.Add(scheduler.UtcNow));
            scheduler.Tick(8, Start.AddSeconds(8), Offset);
            scheduler.EnterBackground();
            scheduler.ResumeFromBackground(40, Start.AddSeconds(40), Offset);
            Assert.That(seen, Is.EqualTo(new[] { Start.AddSeconds(30) }));
            Assert.That(scheduler.UtcNow, Is.EqualTo(Start.AddSeconds(40)));
        }

        [Test]
        public void ResumeWithoutEnterIsANoOp()
        {
            // Unity llama OnApplicationPause(false) una vez al arrancar, después de Awake: sin EnterBackground no hay alcance
            // ni cambio de hora.
            var scheduler = Build(out var _);
            var fired = 0;
            scheduler.Schedule(TimeSpan.FromSeconds(1), () => fired++);
            Assert.That(scheduler.ResumeFromBackground(50, Start.AddSeconds(50), Offset), Is.EqualTo(0));
            Assert.That(fired, Is.EqualTo(0));
            Assert.That(scheduler.MonotonicSeconds, Is.EqualTo(0));
            Assert.That(scheduler.UtcNow, Is.EqualTo(Start));
        }

        [Test, Category("QA-G4aT-10")]
        public void ResumeWithASmallerMonotonicJumpKeepsOrder()
        {
            // RA-19 (R4a-12): en Android el Stopwatch no cuenta el sueño profundo. Con 60 s de monótono y 120 s de pared, el
            // alcance cubre los 60 s del monótono, todo dispara en orden y la hora virtual nunca retrocede.
            var scheduler = Build(out var _);
            var times = new List<DateTimeOffset>();
            var monos = new List<double>();
            scheduler.SchedulePeriodic(TimeSpan.FromSeconds(1), () =>
            {
                times.Add(scheduler.UtcNow);
                monos.Add(scheduler.MonotonicSeconds);
            });
            scheduler.Tick(10, Start.AddSeconds(10), Offset);
            times.Clear();
            monos.Clear();
            scheduler.EnterBackground();
            scheduler.ResumeFromBackground(70, Start.AddSeconds(130), Offset);
            Assert.That(monos.Count, Is.EqualTo(60));
            for (var i = 1; i < times.Count; i++)
            {
                Assert.That(times[i], Is.GreaterThan(times[i - 1]));
                Assert.That(monos[i], Is.GreaterThan(monos[i - 1]));
            }

            Assert.That(times[0], Is.GreaterThanOrEqualTo(Start.AddSeconds(10)), "no antes del último cuadro");
            Assert.That(times[times.Count - 1], Is.EqualTo(Start.AddSeconds(130)));

            // Y con la hora de pared que vuelve menos que el monótono (un cambio de hora): nunca antes del último cuadro.
            var other = Build(out var _);
            var first = DateTimeOffset.MaxValue;
            other.Schedule(TimeSpan.FromSeconds(1), () => first = other.UtcNow);
            other.EnterBackground();
            other.ResumeFromBackground(100, Start.AddSeconds(5), Offset);
            Assert.That(first, Is.EqualTo(Start), "el piso es la hora del último cuadro");
        }

        [Test]
        public void CatchUpIsBoundedAndReanchors()
        {
            // Un timer de 0 que se reagenda sin fin: el alcance corta en el tope, lo registra como Error y la app sigue.
            var logger = new RecordingLogger();
            var queue = new RealtimeTimerQueue(logger);
            var count = 0;
            Action? loop = null;
            var now = 0.0;
            loop = () =>
            {
                count++;
                queue.Schedule(now, TimeSpan.Zero, loop!);
            };
            queue.Schedule(now, TimeSpan.Zero, loop);
            var periodic = new List<double>();
            queue.SchedulePeriodic(now, TimeSpan.FromSeconds(1), () => periodic.Add(now));
            var fired = queue.CatchUp(0, 5, t => now = t, 1000);
            Assert.That(fired, Is.EqualTo(1000));
            Assert.That(now, Is.EqualTo(5), "al terminar, el reloj en el destino");
            Assert.That(logger.Lines, Has.Some.StartsWith("[TIMER] Error: el alcance cortó"));
            Assert.That(queue.RunDue(5), Is.GreaterThanOrEqualTo(1), "lo de una vez queda para el cuadro siguiente");
            Assert.That(periodic, Is.Empty, "el periódico se reancló en el destino + período");
            now = 6;
            queue.RunDue(6);
            Assert.That(periodic, Is.EqualTo(new[] { 6.0 }));
        }

        [Test]
        public void CatchUpRunsWhatIsScheduledDuringIt()
        {
            // A diferencia de RunDue, lo agendado durante el alcance corre si vence antes del destino.
            var queue = new RealtimeTimerQueue(NullAppLogger.Instance);
            var now = 0.0;
            var log = new List<double>();
            queue.Schedule(now, TimeSpan.FromSeconds(1), () =>
            {
                log.Add(now);
                queue.Schedule(now, TimeSpan.FromSeconds(2), () => log.Add(now));
                queue.Schedule(now, TimeSpan.FromSeconds(20), () => log.Add(now));
            });
            Assert.That(queue.CatchUp(0, 10, t => now = t, 100), Is.EqualTo(2));
            Assert.That(log, Is.EqualTo(new[] { 1.0, 3.0 }));
            Assert.That(queue.Pending, Is.EqualTo(1));
        }

        [Test]
        public void ArgumentsAreChecked()
        {
            Assert.Throws<ArgumentNullException>(() => new FrameScheduler(0, Start, Offset, null!));
            var queue = new RealtimeTimerQueue(NullAppLogger.Instance);
            Assert.Throws<ArgumentNullException>(() => queue.CatchUp(0, 1, null!, 10));
            Assert.Throws<ArgumentOutOfRangeException>(() => queue.CatchUp(0, 1, _ => { }, 0));
        }

        private static FrameScheduler Build(out RecordingLogger logger)
        {
            logger = new RecordingLogger();
            return new FrameScheduler(0, Start, Offset, logger);
        }

        private static string Fmt(double seconds)
        {
            return seconds.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
