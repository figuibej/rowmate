using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using RowMate.Core.Common;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Common
{
    // RealtimeTimerQueue = los timers de UnityScheduler (02-tech-design §2.1, QA-G2-15), avanzada con números: a lo sumo un
    // disparo periódico por llamada y reanclado tras un corte del loop (las lecturas perdidas se pierden, no se comprimen).
    [TestFixture]
    public sealed class RealtimeTimerQueueTests
    {
        private const double Frame = 1.0 / 60;

        [Test]
        public void PeriodicFiresOnExactMultiplesFrameByFrame()
        {
            var queue = new RealtimeTimerQueue(NullAppLogger.Instance);
            var fired = new List<double>();
            var now = 0.0;
            queue.SchedulePeriodic(now, TimeSpan.FromSeconds(1), () => fired.Add(now));
            for (var frame = 1; frame <= 200; frame++)
            {
                now = frame * Frame;
                queue.RunDue(now);
            }

            Assert.That(fired.Count, Is.EqualTo(3), "1, 2 y 3 s en 3,33 s");
            for (var i = 0; i < fired.Count; i++)
            {
                Assert.That(fired[i], Is.EqualTo(i + 1.0).Within(Frame), "el primer cuadro después de cada segundo");
            }
        }

        [Test, Category("AC-APP-02-07")]
        public void JumpOfFiveSecondsFiresOnceAndReanchors()
        {
            // El loop cortado al arrastrar la ventana (Windows) o en pausa (Android): de 1 a 6 s, un solo disparo a los 6 s y el
            // siguiente a los 7 s; no cinco seguidos (la distancia y el pulso no "corren").
            var queue = new RealtimeTimerQueue(NullAppLogger.Instance);
            var fired = new List<double>();
            var now = 0.0;
            queue.SchedulePeriodic(now, TimeSpan.FromSeconds(1), () => fired.Add(now));
            now = 1.0;
            queue.RunDue(now);
            now = 6.0;
            Assert.That(queue.RunDue(now), Is.EqualTo(1));
            for (var frame = 1; frame <= 30; frame++)
            {
                now = 6.0 + frame * Frame;
                Assert.That(queue.RunDue(now), Is.EqualTo(0), "nada de ponerse al día un cuadro por lectura");
            }

            now = 7.0;
            queue.RunDue(now);
            Assert.That(fired, Is.EqualTo(new[] { 1.0, 6.0, 7.0 }));
        }

        [Test]
        public void LagBelowOnePeriodKeepsTheGrid()
        {
            var queue = new RealtimeTimerQueue(NullAppLogger.Instance);
            var fired = new List<double>();
            var now = 0.0;
            queue.SchedulePeriodic(now, TimeSpan.FromSeconds(1), () => fired.Add(now));
            now = 1.9; // 0,9 s tarde: dispara una vez y sigue en la grilla (2 s)
            queue.RunDue(now);
            now = 2.0;
            queue.RunDue(now);
            Assert.That(fired, Is.EqualTo(new[] { 1.9, 2.0 }));
        }

        [Test]
        public void OneShotDueDuringTheJumpFiresOnce()
        {
            var queue = new RealtimeTimerQueue(NullAppLogger.Instance);
            var count = 0;
            queue.Schedule(0, TimeSpan.FromSeconds(3), () => count++);
            Assert.That(queue.Pending, Is.EqualTo(1));
            queue.RunDue(10);
            queue.RunDue(11);
            Assert.That(count, Is.EqualTo(1));
            Assert.That(queue.Pending, Is.EqualTo(0));
        }

        [Test]
        public void OrderIsDueThenSchedulingOrder()
        {
            var queue = new RealtimeTimerQueue(NullAppLogger.Instance);
            var order = new List<string>();
            queue.Schedule(0, TimeSpan.FromSeconds(2), () => order.Add("b2"));
            queue.Schedule(0, TimeSpan.FromSeconds(1), () => order.Add("a1"));
            queue.Schedule(0, TimeSpan.FromSeconds(2), () => order.Add("c2"));
            queue.SchedulePeriodic(0, TimeSpan.FromSeconds(1), () => order.Add("p"));
            queue.RunDue(2.5);
            Assert.That(order, Is.EqualTo(new[] { "a1", "p", "b2", "c2" }), "el periódico, una vez por llamada");
        }

        [Test]
        public void CancelInsideACallback()
        {
            var queue = new RealtimeTimerQueue(NullAppLogger.Instance);
            var later = 0;
            IDisposable? second = null;
            IDisposable? periodic = null;
            var ticks = 0;
            queue.Schedule(0, TimeSpan.FromSeconds(1), () => second!.Dispose());
            second = queue.Schedule(0, TimeSpan.FromSeconds(1), () => later++);
            periodic = queue.SchedulePeriodic(0, TimeSpan.FromSeconds(1), () =>
            {
                ticks++;
                periodic!.Dispose();
            });
            queue.RunDue(1);
            queue.RunDue(5);
            Assert.That(later, Is.EqualTo(0), "cancelado por el anterior en la misma llamada");
            Assert.That(ticks, Is.EqualTo(1), "el periódico que se cancela solo no vuelve");
            Assert.That(queue.Pending, Is.EqualTo(0));
        }

        [Test]
        public void TimersScheduledDuringARunWaitForTheNext()
        {
            var queue = new RealtimeTimerQueue(NullAppLogger.Instance);
            var runs = 0;
            Action? again = null;
            again = () =>
            {
                runs++;
                queue.Schedule(1, TimeSpan.Zero, again!); // un timer de 0 que se reagenda no cuelga el cuadro
            };
            queue.Schedule(0, TimeSpan.FromSeconds(1), again);
            Assert.That(queue.RunDue(1), Is.EqualTo(1));
            Assert.That(queue.RunDue(1), Is.EqualTo(1));
            Assert.That(runs, Is.EqualTo(2));
            Assert.That(queue.Pending, Is.EqualTo(1));
        }

        [Test]
        public void DelayCompletesInTheRunThatMakesItDue()
        {
            var queue = new RealtimeTimerQueue(NullAppLogger.Instance);
            var task = queue.Delay(0, TimeSpan.FromSeconds(2), CancellationToken.None);
            queue.RunDue(1.9);
            Assert.That(task.IsCompleted, Is.False);
            queue.RunDue(2.0);
            Assert.That(task.IsCompleted, Is.True);
            Assert.That(task.IsCanceled, Is.False);

            using (var cts = new CancellationTokenSource())
            {
                var canceled = queue.Delay(2, TimeSpan.FromSeconds(1), cts.Token);
                cts.Cancel();
                Assert.That(canceled.IsCanceled, Is.True);
                Assert.That(queue.Pending, Is.EqualTo(0));
            }

            using (var already = new CancellationTokenSource())
            {
                already.Cancel();
                Assert.That(queue.Delay(0, TimeSpan.FromSeconds(1), already.Token).IsCanceled, Is.True);
            }
        }

        [Test]
        public void CallbackExceptionIsLoggedAndTheOthersRun()
        {
            var logger = new RecordingLogger();
            var queue = new RealtimeTimerQueue(logger);
            var ran = 0;
            queue.Schedule(0, TimeSpan.FromSeconds(1), () => throw new InvalidOperationException("roto"));
            queue.Schedule(0, TimeSpan.FromSeconds(1), () => ran++);
            Assert.DoesNotThrow(() => queue.RunDue(1));
            Assert.That(ran, Is.EqualTo(1));
            Assert.That(logger.Lines, Is.EqualTo(new[] { "[TIMER] Error: un timer lanzó InvalidOperationException: roto" }));
        }

        [Test]
        public void ArgumentsAreChecked()
        {
            var queue = new RealtimeTimerQueue(NullAppLogger.Instance);
            Assert.Throws<ArgumentNullException>(() => new RealtimeTimerQueue(null!));
            Assert.Throws<ArgumentNullException>(() => queue.Schedule(0, TimeSpan.Zero, null!));
            Assert.Throws<ArgumentOutOfRangeException>(() => queue.SchedulePeriodic(0, TimeSpan.Zero, () => { }));
            var negative = 0;
            queue.Schedule(5, TimeSpan.FromSeconds(-1), () => negative++);
            queue.RunDue(5);
            Assert.That(negative, Is.EqualTo(1), "un retraso negativo vale 0");
        }
    }
}
