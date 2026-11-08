using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using RowMate.Core.Common;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Common
{
    // MTQ: la cola al hilo principal (DT10-10, QA-G10a-17 de la It. 10a): Post desde cualquier hilo, Drain en el principal (AppRoot,
    // antes de la bomba y del Tick), en orden, sin cortarse por una acción que lanza, y lo encolado durante un Drain va al siguiente.
    [TestFixture]
    public sealed class MainThreadQueueTests
    {
        [Test]
        public void RunsInOrderOnTheDrainingThread()
        {
            var queue = new MainThreadQueue(new RecordingLogger());
            var ran = new List<int>();
            var main = Thread.CurrentThread.ManagedThreadId;
            var threads = new List<int>();
            var worker = new Thread(() =>
            {
                for (var i = 0; i < 100; i++)
                {
                    var n = i;
                    queue.Post(() =>
                    {
                        ran.Add(n);
                        threads.Add(Thread.CurrentThread.ManagedThreadId);
                    });
                }
            });
            worker.Start();
            worker.Join();
            Assert.That(ran, Is.Empty, "nada corre hasta el Drain");
            Assert.That(queue.Pending, Is.EqualTo(100));
            Assert.That(queue.Drain(), Is.EqualTo(100));
            Assert.That(ran.Count, Is.EqualTo(100));
            for (var i = 0; i < 100; i++)
            {
                Assert.That(ran[i], Is.EqualTo(i));
                Assert.That(threads[i], Is.EqualTo(main));
            }

            Assert.That(queue.Drain(), Is.EqualTo(0));
        }

        [Test]
        public void AThrowingActionIsLoggedAndTheRestRun()
        {
            var logger = new RecordingLogger();
            var queue = new MainThreadQueue(logger);
            var ran = 0;
            queue.Post(() => throw new InvalidOperationException("boom"));
            queue.Post(() => ran++);
            Assert.That(queue.Drain(), Is.EqualTo(2));
            Assert.That(ran, Is.EqualTo(1));
            Assert.That(logger.Lines, Has.Some.Contains("boom"));
        }

        [Test]
        public void PostedDuringDrainRunsNextDrain()
        {
            var queue = new MainThreadQueue(new RecordingLogger());
            var ran = new List<string>();
            queue.Post(() =>
            {
                ran.Add("a");
                queue.Post(() => ran.Add("b"));
            });
            queue.Drain();
            Assert.That(ran, Is.EqualTo(new[] { "a" }));
            queue.Drain();
            Assert.That(ran, Is.EqualTo(new[] { "a", "b" }));
        }
    }
}
