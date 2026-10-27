using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using RowMate.Core.Common;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Common
{
    // TestMainThreadContext (QA-P-26): las continuaciones de un await vuelven al hilo del test, como en Unity.
    [TestFixture]
    public sealed class TestMainThreadContextTests
    {
        [Test]
        public void ContinuationsRunOnTheTestThread()
        {
            var testThread = Environment.CurrentManagedThreadId;
            var afterYield = -1;
            var afterThreadPool = -1;
            TestMainThreadContext.Run(async () =>
            {
                await Task.Yield();
                afterYield = Environment.CurrentManagedThreadId;
                await Task.Run(() => Thread.Sleep(1));
                afterThreadPool = Environment.CurrentManagedThreadId;
            });
            Assert.That(afterYield, Is.EqualTo(testThread));
            Assert.That(afterThreadPool, Is.EqualTo(testThread));
        }

        [Test]
        public void RunPropagatesTheException()
        {
            var error = Assert.Throws<InvalidOperationException>(() => TestMainThreadContext.Run(async () =>
            {
                await Task.Yield();
                throw new InvalidOperationException("boom");
            }));
            Assert.That(error!.Message, Is.EqualTo("boom"));
        }

        [Test]
        public void RunRestoresThePreviousContext()
        {
            var before = SynchronizationContext.Current;
            TestMainThreadContext.Run(async () => await Task.Yield());
            Assert.That(SynchronizationContext.Current, Is.SameAs(before));
        }

        [Test]
        public void WorksWithFakeSchedulerDelays()
        {
            var scheduler = new FakeScheduler();
            var steps = 0;
            TestMainThreadContext.Run(async () =>
            {
                var waiting = WaitTwice(scheduler, () => steps++);
                await Task.Yield();
                scheduler.Advance(TimeSpan.FromSeconds(1));
                await Task.Yield();
                scheduler.Advance(TimeSpan.FromSeconds(1));
                await waiting;
            });
            Assert.That(steps, Is.EqualTo(2));
            Assert.That(scheduler.MonotonicSeconds, Is.EqualTo(2.0));
        }

        [Test]
        public void RunUntilIdleExecutesWhatWasPosted()
        {
            var context = new TestMainThreadContext();
            var log = 0;
            context.Post(_ => { log++; context.Post(__ => log += 10, null); }, null);
            Assert.That(context.PendingCount, Is.EqualTo(1));
            context.RunUntilIdle();
            Assert.That(log, Is.EqualTo(11));
            Assert.That(context.PendingCount, Is.EqualTo(0));
        }

        [Test]
        public void ForgetLogsFaultsThroughTheLogger()
        {
            var logger = new RecordingLogger();
            TestMainThreadContext.Run(async () =>
            {
                Fail().Forget(logger, "BLE");
                await Task.Yield();
                await Task.Yield();
            });
            Assert.That(logger.Lines, Is.EqualTo(new[] { "[BLE] Error: fallo esperado" }));
            Assert.That(logger.Errors[0], Is.TypeOf<InvalidOperationException>());

            var done = new RecordingLogger();
            Task.CompletedTask.Forget(done, "HRM");
            Assert.That(done.Lines, Is.Empty);
        }

        private static async Task WaitTwice(FakeScheduler scheduler, Action step)
        {
            await scheduler.Delay(TimeSpan.FromSeconds(1));
            step();
            await scheduler.Delay(TimeSpan.FromSeconds(1));
            step();
        }

        private static async Task Fail()
        {
            await Task.Yield();
            throw new InvalidOperationException("fallo esperado");
        }
    }
}
