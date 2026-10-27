using System;
using System.Collections.Generic;
using NUnit.Framework;
using RowMate.Core.Common;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Common
{
    // ObservableValue (ADR 004): semántica de ChangeNotifier del legacy, sincrónica y sin reservar memoria al emitir.
    [TestFixture]
    public sealed class ObservableValueTests
    {
        [Test]
        public void SubscribeEmitsTheCurrentValueByDefault()
        {
            var value = new ObservableValue<int>(7, NullAppLogger.Instance);
            var seen = new List<int>();
            value.Subscribe(seen.Add);
            Assert.That(seen, Is.EqualTo(new[] { 7 }));

            var quiet = new List<int>();
            value.Subscribe(quiet.Add, emitCurrent: false);
            Assert.That(quiet, Is.Empty);
        }

        [Test]
        public void NotifiesOnlyWhenTheValueChanges()
        {
            var value = new ObservableValue<int>(1, NullAppLogger.Instance);
            var seen = new List<int>();
            value.Subscribe(seen.Add, emitCurrent: false);
            value.Value = 1;
            value.Value = 2;
            value.Value = 2;
            value.Value = 3;
            Assert.That(seen, Is.EqualTo(new[] { 2, 3 }));
            Assert.That(value.Value, Is.EqualTo(3));
        }

        [Test]
        public void UsesTheGivenComparer()
        {
            var value = new ObservableValue<string>("a", NullAppLogger.Instance, StringComparer.OrdinalIgnoreCase);
            var seen = new List<string>();
            value.Subscribe(seen.Add, emitCurrent: false);
            value.Value = "A";
            value.Value = "b";
            Assert.That(seen, Is.EqualTo(new[] { "b" }));
            Assert.That(value.Value, Is.EqualTo("b"));
        }

        [Test]
        public void ForceNotifyNotifiesWithoutAChange()
        {
            // = _reemitStatus() del legacy.
            var value = new ObservableValue<int>(4, NullAppLogger.Instance);
            var seen = new List<int>();
            value.Subscribe(seen.Add, emitCurrent: false);
            value.ForceNotify();
            Assert.That(seen, Is.EqualTo(new[] { 4 }));
        }

        [Test]
        public void DisposeStopsNotificationsAndIsIdempotent()
        {
            var value = new ObservableValue<int>(0, NullAppLogger.Instance);
            var seen = new List<int>();
            var subscription = value.Subscribe(seen.Add, emitCurrent: false);
            value.Value = 1;
            subscription.Dispose();
            subscription.Dispose();
            value.Value = 2;
            Assert.That(seen, Is.EqualTo(new[] { 1 }));
            Assert.That(value.SubscriberCount, Is.EqualTo(0));
        }

        [Test]
        public void DisposingDuringAnEmissionDoesNotBreakIt()
        {
            var value = new ObservableValue<int>(0, NullAppLogger.Instance);
            var log = new List<string>();
            IDisposable? third = null;
            value.Subscribe(v => { log.Add("A" + v); third?.Dispose(); }, emitCurrent: false);
            value.Subscribe(v => log.Add("B" + v), emitCurrent: false);
            third = value.Subscribe(v => log.Add("C" + v), emitCurrent: false);
            value.Value = 1;
            value.Value = 2;
            Assert.That(log, Is.EqualTo(new[] { "A1", "B1", "A2", "B2" }));
        }

        [Test]
        public void NestedSetNotifiesImmediatelyWithTheCurrentValue()
        {
            // Como ChangeNotifier: un set dentro de un callback notifica en el acto; cada suscriptor recibe el
            // valor vigente al llamarlo, así nadie se queda con un valor viejo.
            var value = new ObservableValue<int>(0, NullAppLogger.Instance);
            var log = new List<string>();
            value.Subscribe(v =>
            {
                log.Add("A" + v);
                if (v == 1)
                {
                    value.Value = 2;
                }
            }, emitCurrent: false);
            value.Subscribe(v => log.Add("B" + v), emitCurrent: false);
            value.Value = 1;
            Assert.That(log, Is.EqualTo(new[] { "A1", "A2", "B2", "B2" }));
            Assert.That(value.Value, Is.EqualTo(2));
        }

        // QA-V-01: como ChangeNotifier.notifyListeners, la excepción de un suscriptor se reporta y se sigue con el resto.
        // Escenario de QA: la vista A lanza al recibir "connected" y el servicio B igual se entera; el setter no lanza.
        [Test, Category("QA-V-01")]
        public void ThrowingSubscriberDoesNotStopTheOthers()
        {
            var logger = new RecordingLogger();
            var value = new ObservableValue<string>("idle", logger);
            var log = new List<string>();
            value.Subscribe(v => log.Add("A" + v), emitCurrent: false);
            value.Subscribe(v =>
            {
                log.Add("vista" + v);
                throw new InvalidOperationException("vista rota");
            }, emitCurrent: false);
            value.Subscribe(v => log.Add("B" + v), emitCurrent: false);

            Assert.DoesNotThrow(() => value.Value = "connected");

            Assert.That(value.Value, Is.EqualTo("connected"));
            Assert.That(log, Is.EqualTo(new[] { "Aconnected", "vistaconnected", "Bconnected" }));
            Assert.That(logger.Lines, Is.EqualTo(new[]
            {
                "[STATE] Error: un suscriptor de ObservableValue<String> lanzó InvalidOperationException: vista rota",
            }));
            Assert.That(logger.Levels, Is.EqualTo(new[] { LogLevel.Error }));
            Assert.That(logger.Errors[0], Is.InstanceOf<InvalidOperationException>());
            Assert.That(SubscriberErrors.Tag, Is.EqualTo("STATE"));
        }

        [Test, Category("QA-V-01")]
        public void ThrowingSubscriberStaysSubscribedAndEveryFailureIsReported()
        {
            // Flutter no da de baja al listener que lanzó: en la próxima notificación se lo vuelve a llamar.
            var logger = new RecordingLogger();
            var value = new ObservableValue<int>(0, logger);
            var calls = 0;
            var seen = new List<int>();
            value.Subscribe(v =>
            {
                calls++;
                throw new ArgumentException("falla " + v);
            }, emitCurrent: false);
            value.Subscribe(seen.Add, emitCurrent: false);

            value.Value = 1;
            value.ForceNotify();
            value.Value = 2;

            Assert.That(calls, Is.EqualTo(3));
            Assert.That(seen, Is.EqualTo(new[] { 1, 1, 2 }));
            Assert.That(logger.Lines.Count, Is.EqualTo(3));
            Assert.That(logger.Lines[2], Does.EndWith("ArgumentException: falla 2"));
            Assert.That(value.SubscriberCount, Is.EqualTo(2));
        }

        [Test, Category("QA-V-01")]
        public void ThrowingSubscriberDuringANestedSetKeepsTheNestedSemantics()
        {
            // La excepción no rompe el set anidado: los demás reciben el valor vigente, igual que sin excepción.
            var logger = new RecordingLogger();
            var value = new ObservableValue<int>(0, logger);
            var log = new List<string>();
            value.Subscribe(v =>
            {
                log.Add("A" + v);
                if (v == 1)
                {
                    value.Value = 2;
                    throw new InvalidOperationException("después del set");
                }
            }, emitCurrent: false);
            value.Subscribe(v => log.Add("B" + v), emitCurrent: false);

            Assert.DoesNotThrow(() => value.Value = 1);

            Assert.That(log, Is.EqualTo(new[] { "A1", "A2", "B2", "B2" }));
            Assert.That(value.Value, Is.EqualTo(2));
            Assert.That(logger.Lines.Count, Is.EqualTo(1));
        }

        [Test, Category("QA-V-01")]
        public void ThrowingInTheInitialEmissionStillSubscribes()
        {
            // emitCurrent: la excepción va al log y la suscripción queda hecha (el que se suscribe puede darla de baja).
            var logger = new RecordingLogger();
            var value = new ObservableValue<int>(5, logger);
            var calls = 0;
            IDisposable? subscription = null;
            Assert.DoesNotThrow(() => subscription = value.Subscribe(v =>
            {
                calls++;
                throw new InvalidOperationException("al suscribirse");
            }));

            Assert.That(subscription, Is.Not.Null);
            Assert.That(calls, Is.EqualTo(1));
            Assert.That(logger.Lines.Count, Is.EqualTo(1));
            Assert.That(value.SubscriberCount, Is.EqualTo(1));
            subscription!.Dispose();
            value.Value = 6;
            Assert.That(calls, Is.EqualTo(1));
        }

        [Test, Category("QA-V-01")]
        public void LoggerIsRequired()
        {
            Assert.Throws<ArgumentNullException>(() => new ObservableValue<int>(0, null!));
        }

        [Test]
        public void ReadOnlyInterfaceExposesValueAndSubscribe()
        {
            IReadOnlyObservable<int> observable = new ObservableValue<int>(5, NullAppLogger.Instance);
            var seen = new List<int>();
            using (observable.Subscribe(seen.Add))
            {
                Assert.That(observable.Value, Is.EqualTo(5));
            }

            Assert.That(seen, Is.EqualTo(new[] { 5 }));
        }

#if NET5_0_OR_GREATER
        [Test]
        public void EmittingDoesNotAllocate()
        {
            var value = new ObservableValue<int>(0, NullAppLogger.Instance);
            var sum = 0;
            value.Subscribe(v => sum += v, emitCurrent: false);
            value.Subscribe(v => sum -= 1, emitCurrent: false);
            SetMany(value, 1); // calentamiento: JIT y estáticos fuera de la medición
            Assert.That(SetMany(value, 2000), Is.EqualTo(0L));
            Assert.That(sum, Is.Not.EqualTo(0));
        }

        // Optimizado de entrada (sin tiering ni OSR): así la medición ve solo lo que reserva ObservableValue.
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveOptimization)]
        private static long SetMany(ObservableValue<int> value, int start)
        {
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = start; i < start + 1000; i++)
            {
                value.Value = i;
            }

            return GC.GetAllocatedBytesForCurrentThread() - before;
        }
#endif
    }
}
