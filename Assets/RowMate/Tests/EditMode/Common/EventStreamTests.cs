using System;
using System.Collections.Generic;
using NUnit.Framework;
using RowMate.Core.Common;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Common
{
    // EventStream (ADR 004) = StreamController.broadcast del legacy: sin buffer, a todos los suscriptores.
    [TestFixture]
    public sealed class EventStreamTests
    {
        [Test]
        public void BroadcastsToEverySubscriberInOrder()
        {
            var stream = new EventStream<int>(NullAppLogger.Instance);
            var log = new List<string>();
            stream.Subscribe(v => log.Add("A" + v));
            stream.Subscribe(v => log.Add("B" + v));
            stream.Emit(1);
            stream.Emit(2);
            Assert.That(log, Is.EqualTo(new[] { "A1", "B1", "A2", "B2" }));
            Assert.That(stream.SubscriberCount, Is.EqualTo(2));
        }

        [Test]
        public void HasNoBuffer()
        {
            var stream = new EventStream<int>(NullAppLogger.Instance);
            stream.Emit(1);
            var seen = new List<int>();
            stream.Subscribe(seen.Add);
            stream.Emit(2);
            Assert.That(seen, Is.EqualTo(new[] { 2 }));
        }

        [Test]
        public void DisposeUnsubscribes()
        {
            var stream = new EventStream<int>(NullAppLogger.Instance);
            var seen = new List<int>();
            var subscription = stream.Subscribe(seen.Add);
            stream.Emit(1);
            subscription.Dispose();
            subscription.Dispose();
            stream.Emit(2);
            Assert.That(seen, Is.EqualTo(new[] { 1 }));
            Assert.That(stream.SubscriberCount, Is.EqualTo(0));
        }

        [Test]
        public void UnsubscribingDuringAnEmissionDoesNotBreakIt()
        {
            var stream = new EventStream<int>(NullAppLogger.Instance);
            var log = new List<string>();
            IDisposable? second = null;
            stream.Subscribe(v => { log.Add("A" + v); second?.Dispose(); });
            second = stream.Subscribe(v => log.Add("B" + v));
            stream.Subscribe(v => log.Add("C" + v));
            stream.Emit(1);
            Assert.That(log, Is.EqualTo(new[] { "A1", "C1" }));
        }

        [Test]
        public void ObserverReceivesOnlyOnNext()
        {
            var stream = new EventStream<string>(NullAppLogger.Instance);
            var observer = new Recorder();
            using (stream.Subscribe(observer))
            {
                stream.Emit("x");
            }

            stream.Emit("y");
            Assert.That(observer.Values, Is.EqualTo(new[] { "x" }));
        }

        // QA-V-01: como un Stream broadcast, el error de un listener no corta a los demás ni sube a quien emite.
        [Test, Category("QA-V-01")]
        public void ThrowingSubscriberDoesNotStopTheOthers()
        {
            var logger = new RecordingLogger();
            var stream = new EventStream<int>(logger);
            var log = new List<string>();
            stream.Subscribe(v => log.Add("A" + v));
            stream.Subscribe(v =>
            {
                log.Add("vista" + v);
                throw new InvalidOperationException("vista rota");
            });
            stream.Subscribe(v => log.Add("B" + v));

            Assert.DoesNotThrow(() => stream.Emit(1));
            Assert.DoesNotThrow(() => stream.Emit(2));

            Assert.That(log, Is.EqualTo(new[] { "A1", "vista1", "B1", "A2", "vista2", "B2" }));
            Assert.That(logger.Lines, Is.EqualTo(new[]
            {
                "[STATE] Error: un suscriptor de EventStream<Int32> lanzó InvalidOperationException: vista rota",
                "[STATE] Error: un suscriptor de EventStream<Int32> lanzó InvalidOperationException: vista rota",
            }));
            Assert.That(logger.Errors[0], Is.InstanceOf<InvalidOperationException>());
            Assert.That(stream.SubscriberCount, Is.EqualTo(3), "el que lanzó sigue suscripto");
        }

        [Test, Category("QA-V-01")]
        public void ThrowingObserverDoesNotStopTheOthers()
        {
            var logger = new RecordingLogger();
            var stream = new EventStream<string>(logger);
            var broken = new Recorder { ThrowOnNext = true };
            var healthy = new Recorder();
            stream.Subscribe(broken);
            stream.Subscribe(healthy);

            Assert.DoesNotThrow(() => stream.Emit("x"));

            Assert.That(broken.Values, Is.EqualTo(new[] { "x" }));
            Assert.That(healthy.Values, Is.EqualTo(new[] { "x" }));
            Assert.That(logger.Lines.Count, Is.EqualTo(1));
        }

        [Test, Category("QA-V-01")]
        public void LoggerIsRequired()
        {
            Assert.Throws<ArgumentNullException>(() => new EventStream<int>(null!));
        }

        [Test]
        public void EmitWithoutSubscribersIsANoOp()
        {
            var stream = new EventStream<int>(NullAppLogger.Instance);
            Assert.DoesNotThrow(() => stream.Emit(1));
        }

#if NET5_0_OR_GREATER
        [Test]
        public void EmittingDoesNotAllocate()
        {
            var stream = new EventStream<int>(NullAppLogger.Instance);
            var sum = 0;
            stream.Subscribe(v => sum += v);
            EmitMany(stream); // calentamiento: JIT y estáticos fuera de la medición
            Assert.That(EmitMany(stream), Is.EqualTo(0L));
            Assert.That(sum, Is.GreaterThan(0));
        }

        // Optimizado de entrada (sin tiering ni OSR): así la medición ve solo lo que reserva EventStream.
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveOptimization)]
        private static long EmitMany(EventStream<int> stream)
        {
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 1000; i++)
            {
                stream.Emit(i);
            }

            return GC.GetAllocatedBytesForCurrentThread() - before;
        }
#endif

        private sealed class Recorder : IObserver<string>
        {
            public List<string> Values { get; } = new List<string>();

            public bool ThrowOnNext { get; set; }

            public void OnCompleted()
            {
                throw new InvalidOperationException("un flujo broadcast no se completa");
            }

            public void OnError(Exception error)
            {
                throw new InvalidOperationException("un flujo broadcast no falla");
            }

            public void OnNext(string value)
            {
                Values.Add(value);
                if (ThrowOnNext)
                {
                    throw new InvalidOperationException("observer roto");
                }
            }
        }
    }
}
