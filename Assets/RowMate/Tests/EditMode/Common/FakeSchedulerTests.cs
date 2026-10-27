using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Common
{
    // AC-BASE-06: el tiempo se controla en los tests (3 s simulados en menos de 1 s real) y el resultado es el mismo en cada corrida.
    [TestFixture]
    public sealed class FakeSchedulerTests
    {
        [Test, Category("AC-BASE-06")]
        public void ThreeSimulatedSecondsInUnderOneRealSecond()
        {
            var scheduler = new FakeScheduler();
            var ticks = 0;
            var fired = false;
            scheduler.SchedulePeriodic(TimeSpan.FromSeconds(1), () => ticks++);
            scheduler.Schedule(TimeSpan.FromSeconds(3), () => fired = true);
            var watch = Stopwatch.StartNew();
            scheduler.Advance(TimeSpan.FromSeconds(3));
            watch.Stop();
            Assert.That(ticks, Is.EqualTo(3));
            Assert.That(fired, Is.True);
            Assert.That(scheduler.MonotonicSeconds, Is.EqualTo(3.0));
            Assert.That(scheduler.UtcNow, Is.EqualTo(new DateTimeOffset(2026, 1, 1, 0, 0, 3, TimeSpan.Zero)));
            Assert.That(watch.Elapsed, Is.LessThan(TimeSpan.FromSeconds(1)));
        }

        [Test, Category("AC-BASE-06")]
        public void DeterministicOrder()
        {
            // Por vencimiento y, a igual vencimiento, por orden de alta. Dos corridas dan exactamente lo mismo.
            Assert.That(RunScenario(), Is.EqualTo(new[] { "B@1", "C@1", "D@1", "A@2", "D@2", "E@2.5" }));
            Assert.That(RunScenario(), Is.EqualTo(RunScenario()));
        }

        [Test, Category("AC-BASE-06")]
        public void CallbacksScheduledDuringAdvanceRunInTheSameAdvance()
        {
            var scheduler = new FakeScheduler();
            var log = new List<double>();
            scheduler.Schedule(TimeSpan.FromSeconds(1), () =>
            {
                log.Add(scheduler.MonotonicSeconds);
                scheduler.Schedule(TimeSpan.FromSeconds(0.5), () => log.Add(scheduler.MonotonicSeconds));
                scheduler.Schedule(TimeSpan.FromSeconds(2), () => log.Add(scheduler.MonotonicSeconds));
            });
            scheduler.Advance(TimeSpan.FromSeconds(2));
            Assert.That(log, Is.EqualTo(new[] { 1.0, 1.5 }));
            Assert.That(scheduler.PendingTimers, Is.EqualTo(1));
            Assert.That(scheduler.MonotonicSeconds, Is.EqualTo(2.0));
            scheduler.Advance(TimeSpan.FromSeconds(1));
            Assert.That(log, Is.EqualTo(new[] { 1.0, 1.5, 3.0 }));
            Assert.That(scheduler.PendingTimers, Is.EqualTo(0));
        }

        [Test, Category("AC-BASE-06")]
        public void PeriodicFiresAtExactMultiplesOfThePeriod()
        {
            var scheduler = new FakeScheduler();
            var at = new List<TimeSpan>();
            scheduler.SchedulePeriodic(TimeSpan.FromMilliseconds(300), () => at.Add(scheduler.Elapsed));
            scheduler.Advance(TimeSpan.FromMilliseconds(500));
            scheduler.Advance(TimeSpan.FromMilliseconds(500));
            Assert.That(at, Is.EqualTo(new[] { TimeSpan.FromMilliseconds(300), TimeSpan.FromMilliseconds(600), TimeSpan.FromMilliseconds(900) }));
        }

        [Test, Category("AC-BASE-06")]
        public void DisposeCancelsTimers()
        {
            var scheduler = new FakeScheduler();
            var count = 0;
            var once = scheduler.Schedule(TimeSpan.FromSeconds(1), () => count += 100);
            IDisposable? periodic = null;
            periodic = scheduler.SchedulePeriodic(TimeSpan.FromSeconds(1), () =>
            {
                count++;
                if (count == 2)
                {
                    periodic!.Dispose();
                }
            });
            once.Dispose();
            scheduler.Advance(TimeSpan.FromSeconds(10));
            Assert.That(count, Is.EqualTo(2));
            Assert.That(scheduler.PendingTimers, Is.EqualTo(0));
        }

        [Test, Category("AC-BASE-06")]
        public void ZeroDelayRunsOnlyWhenDue()
        {
            var scheduler = new FakeScheduler();
            var ran = false;
            scheduler.Schedule(TimeSpan.Zero, () => ran = true);
            Assert.That(ran, Is.False);
            scheduler.RunDue();
            Assert.That(ran, Is.True);
            Assert.That(scheduler.MonotonicSeconds, Is.EqualTo(0.0));
        }

        [Test, Category("AC-BASE-06")]
        public void DelayCompletesInlineInsideAdvance()
        {
            var scheduler = new FakeScheduler();
            var continued = false;
            var delay = scheduler.Delay(TimeSpan.FromSeconds(1));
            delay.ContinueWith(_ => continued = true, TaskContinuationOptions.ExecuteSynchronously);
            scheduler.Advance(TimeSpan.FromMilliseconds(999));
            Assert.That(delay.IsCompleted, Is.False);
            scheduler.Advance(TimeSpan.FromMilliseconds(1));
            Assert.That(delay.Status, Is.EqualTo(TaskStatus.RanToCompletion));
            Assert.That(continued, Is.True);
        }

        [Test, Category("AC-BASE-06")]
        public void DelayCanBeCanceled()
        {
            var scheduler = new FakeScheduler();
            using (var source = new CancellationTokenSource())
            {
                var delay = scheduler.Delay(TimeSpan.FromSeconds(5), source.Token);
                Assert.That(scheduler.PendingTimers, Is.EqualTo(1));
                source.Cancel();
                Assert.That(delay.IsCanceled, Is.True);
                Assert.That(scheduler.PendingTimers, Is.EqualTo(0));
                var already = scheduler.Delay(TimeSpan.FromSeconds(1), source.Token);
                Assert.That(already.IsCanceled, Is.True);
            }
        }

        [Test, Category("AC-BASE-06")]
        public void DefaultsAndClockOffsets()
        {
            var scheduler = new FakeScheduler();
            Assert.That(scheduler.UtcNow, Is.EqualTo(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)));
            Assert.That(scheduler.UtcNow.Offset, Is.EqualTo(TimeSpan.Zero));
            Assert.That(scheduler.LocalNow.Offset, Is.EqualTo(TimeSpan.FromHours(-3)));
            Assert.That(scheduler.LocalNow.DateTime, Is.EqualTo(new DateTime(2025, 12, 31, 21, 0, 0)));
            Assert.That(scheduler.MonotonicSeconds, Is.EqualTo(0.0));
            Assert.That(scheduler.IsCatchingUp, Is.False);
            scheduler.IsCatchingUp = true;
            Assert.That(scheduler.IsCatchingUp, Is.True);

            var custom = new FakeScheduler(new DateTimeOffset(2030, 5, 6, 7, 8, 9, TimeSpan.FromHours(2)), TimeSpan.Zero);
            Assert.That(custom.UtcNow, Is.EqualTo(new DateTimeOffset(2030, 5, 6, 5, 8, 9, TimeSpan.Zero)));
            Assert.That(custom.UtcNow.Offset, Is.EqualTo(TimeSpan.Zero));
        }

        [Test]
        public void RejectsInvalidArguments()
        {
            var scheduler = new FakeScheduler();
            Assert.Throws<ArgumentOutOfRangeException>(() => scheduler.Advance(TimeSpan.FromSeconds(-1)));
            Assert.Throws<ArgumentOutOfRangeException>(() => scheduler.SchedulePeriodic(TimeSpan.Zero, () => { }));
            Assert.Throws<ArgumentNullException>(() => scheduler.Schedule(TimeSpan.Zero, null!));
        }

        private static string[] RunScenario()
        {
            var scheduler = new FakeScheduler();
            var log = new List<string>();
            scheduler.Schedule(TimeSpan.FromSeconds(2), () => log.Add("A@" + scheduler.MonotonicSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            scheduler.Schedule(TimeSpan.FromSeconds(1), () => log.Add("B@" + scheduler.MonotonicSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            scheduler.Schedule(TimeSpan.FromSeconds(1), () =>
            {
                log.Add("C@" + scheduler.MonotonicSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture));
                scheduler.Schedule(TimeSpan.FromSeconds(1.5), () => log.Add("E@" + scheduler.MonotonicSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            });
            scheduler.SchedulePeriodic(TimeSpan.FromSeconds(1), () => log.Add("D@" + scheduler.MonotonicSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            scheduler.Advance(TimeSpan.FromSeconds(2.5));
            return log.ToArray();
        }
    }
}
