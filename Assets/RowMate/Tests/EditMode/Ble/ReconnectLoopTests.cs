using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using RowMate.Core.Ble;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Ble
{
    // ReconnectLoop = lib/core/bluetooth/reconnect_loop.dart:8-71 con el reloj de prueba (FakeScheduler = fake_async). LT-139 a LT-144
    // de test/reconnect_loop_test.dart, más la espera contada desde el FIN del intento y lo que suma el port (Run, NextAttemptAt).
    [TestFixture]
    public sealed class ReconnectLoopTests
    {
        private FakeScheduler _scheduler = null!;
        private RecordingLogger _logger = null!;
        private InlineContinuations _inline = null!;

        [SetUp]
        public void SetUp()
        {
            _inline = new InlineContinuations();
            _scheduler = new FakeScheduler();
            _logger = new RecordingLogger();
        }

        [TearDown]
        public void TearDown()
        {
            _inline.Dispose();
        }

        private void Elapse(double seconds)
        {
            _scheduler.Advance(TimeSpan.FromTicks((long)(seconds * TimeSpan.TicksPerSecond + 0.5)));
        }

        // legacy: test/reconnect_loop_test.dart:9 'intenta de inmediato, reintenta cada intervalo y para al tener éxito'
        [TestCase(TestName = "intenta de inmediato, reintenta cada intervalo y para al tener éxito", Category = "LT-139,AC-HRM-09-01")]
        public void LT139_ImmediateThenIntervalStopsOnSuccess()
        {
            var calls = 0;
            var loop = new ReconnectLoop(_scheduler, TimeSpan.FromSeconds(3), () => Task.FromResult(++calls >= 3), _logger);
            loop.Start();
            Assert.That(calls, Is.EqualTo(1));
            Assert.That(loop.IsRunning, Is.True);

            Elapse(3);
            Assert.That(calls, Is.EqualTo(2));
            Elapse(3);
            Assert.That(calls, Is.EqualTo(3));
            Assert.That(loop.IsRunning, Is.False);

            Elapse(30);
            Assert.That(calls, Is.EqualTo(3), "no sigue intentando tras el éxito");
            Assert.That(_scheduler.PendingTimers, Is.EqualTo(0));
        }

        // legacy: test/reconnect_loop_test.dart:32 'se rinde tras maxAttempts y avisa'
        [TestCase(TestName = "se rinde tras maxAttempts y avisa", Category = "LT-140,AC-HRM-09-01")]
        public void LT140_GivesUpAfterMaxAndNotifiesOnce()
        {
            var calls = 0;
            var gaveUp = 0;
            var loop = new ReconnectLoop(_scheduler, TimeSpan.FromSeconds(10), () =>
            {
                calls++;
                return Task.FromResult(false);
            }, _logger, 3, () => gaveUp++);
            loop.Start();
            Elapse(25);
            Assert.That(calls, Is.EqualTo(3));
            Assert.That(gaveUp, Is.EqualTo(1), "avisa una vez");
            Assert.That(loop.IsRunning, Is.False);
            Elapse(60);
            Assert.That(calls, Is.EqualTo(3));
            Assert.That(gaveUp, Is.EqualTo(1));
        }

        // legacy: test/reconnect_loop_test.dart:55 'cancel frena los reintentos y una excepción cuenta como fallo'
        [TestCase(TestName = "cancel frena los reintentos y una excepción cuenta como fallo", Category = "LT-141,AC-HRM-09-01")]
        public void LT141_CancelStopsAndExceptionIsFailure()
        {
            var calls = 0;
            var loop = new ReconnectLoop(_scheduler, TimeSpan.FromSeconds(1), () =>
            {
                calls++;
                return Task.FromException<bool>(new InvalidOperationException("sin BT"));
            }, _logger);
            loop.Start();
            Elapse(2);
            Assert.That(calls, Is.EqualTo(3));
            loop.Cancel();
            Assert.That(loop.IsRunning, Is.False);
            Elapse(10);
            Assert.That(calls, Is.EqualTo(3));

            // Una excepción sincrónica también es un fallo (el legacy: un throw dentro del async).
            var syncCalls = 0;
            var sync = new ReconnectLoop(_scheduler, TimeSpan.FromSeconds(1), () =>
            {
                syncCalls++;
                throw new InvalidOperationException("sin BT");
            }, _logger);
            sync.Start();
            Elapse(1);
            Assert.That(syncCalls, Is.EqualTo(2));
            sync.Dispose();
            Assert.That(_scheduler.PendingTimers, Is.EqualTo(0));
        }

        // legacy: test/reconnect_loop_test.dart:75 'start() durante un intento en vuelo descarta ese resultado y arranca de cero'
        [TestCase(TestName = "start() durante un intento en vuelo descarta ese resultado y arranca de cero", Category = "LT-142,AC-HRM-09-01,AC-HRM-11-01")]
        public void LT142_StartDuringFlightDiscardsOld()
        {
            var completers = new List<TaskCompletionSource<bool>>();
            var loop = new ReconnectLoop(_scheduler, TimeSpan.FromSeconds(3), () =>
            {
                var c = new TaskCompletionSource<bool>();
                completers.Add(c);
                return c.Task;
            }, _logger);
            loop.Start();
            Assert.That(completers.Count, Is.EqualTo(1));

            loop.Start(); // reinicio mientras el primer intento sigue en vuelo
            Assert.That(completers.Count, Is.EqualTo(2), "la nueva corrida intenta de inmediato");
            Assert.That(loop.Attempts, Is.EqualTo(1));

            completers[0].SetResult(true); // éxito tardío de la corrida vieja
            Assert.That(loop.IsRunning, Is.True, "el éxito viejo no termina la corrida nueva");

            completers[1].SetResult(false);
            Elapse(3);
            Assert.That(completers.Count, Is.EqualTo(3));
            Assert.That(loop.Attempts, Is.EqualTo(2));
            loop.Dispose();
        }

        // legacy: test/reconnect_loop_test.dart:105 'cancel() durante un intento en vuelo no programa otro intento'
        [TestCase(TestName = "cancel() durante un intento en vuelo no programa otro intento", Category = "LT-143,AC-HRM-09-01,AC-HRM-11-01")]
        public void LT143_CancelDuringFlightSchedulesNothing()
        {
            var completers = new List<TaskCompletionSource<bool>>();
            var loop = new ReconnectLoop(_scheduler, TimeSpan.FromSeconds(1), () =>
            {
                var c = new TaskCompletionSource<bool>();
                completers.Add(c);
                return c.Task;
            }, _logger);
            loop.Start();
            loop.Cancel();
            completers[0].SetResult(false);
            Elapse(10);
            Assert.That(completers.Count, Is.EqualTo(1));
            Assert.That(loop.IsRunning, Is.False);
            Assert.That(_scheduler.PendingTimers, Is.EqualTo(0));
        }

        // legacy: test/reconnect_loop_test.dart:126 'start() desde onGiveUp arranca una corrida nueva'
        [TestCase(TestName = "start() desde onGiveUp arranca una corrida nueva", Category = "LT-144,AC-HRM-09-01")]
        public void LT144_StartFromOnGiveUp()
        {
            var calls = 0;
            var giveUps = 0;
            ReconnectLoop? loop = null;
            loop = new ReconnectLoop(_scheduler, TimeSpan.FromSeconds(1), () =>
            {
                calls++;
                return Task.FromResult(false);
            }, _logger, 2, () =>
            {
                if (++giveUps == 1)
                {
                    loop!.Start();
                }
            });
            loop.Start();
            Elapse(1); // 2 fallos → se rinde → Start → intento 3 inmediato
            Assert.That(giveUps, Is.EqualTo(1));
            Assert.That(calls, Is.EqualTo(3));
            Assert.That(loop.IsRunning, Is.True);
            Assert.That(loop.Run, Is.EqualTo(2), "corrida nueva");

            Elapse(1);
            Assert.That(calls, Is.EqualTo(4));
            Assert.That(giveUps, Is.EqualTo(2));
            Assert.That(loop.IsRunning, Is.False);
        }

        // rl:49-70: la espera del intervalo empieza al TERMINAR el intento (un intento de 2 s y 3 s de espera → el siguiente a los 5 s).
        [Test, Category("AC-HRM-09-01")]
        public void WaitCountsFromAttemptEnd()
        {
            var starts = new List<double>();
            var loop = new ReconnectLoop(_scheduler, TimeSpan.FromSeconds(3), async () =>
            {
                starts.Add(_scheduler.MonotonicSeconds);
                await _scheduler.Delay(TimeSpan.FromSeconds(2));
                return false;
            }, _logger);
            loop.Start();
            Elapse(12);
            Assert.That(starts, Is.EqualTo(new[] { 0.0, 5.0, 10.0 }));
            loop.Dispose();
            Elapse(2); // termina el intento en vuelo: no agenda otro
            Assert.That(_scheduler.PendingTimers, Is.EqualTo(0));
        }

        // Reintentando = hay corrida activa, también durante las esperas (AC-HRM-09-01). Lo que suma el port para la tarjeta: la fase
        // (IsAttempting), NextAttemptAt (solo esperando), Run y Changed.
        [Test, Category("AC-HRM-09-01")]
        public void IsRunningDuringWaits()
        {
            var flight = new TaskCompletionSource<bool>();
            var changes = 0;
            var loop = new ReconnectLoop(_scheduler, TimeSpan.FromSeconds(10), () => flight.Task, _logger, 6);
            loop.Changed += () => changes++;
            Assert.That(loop.IsRunning, Is.False);
            Assert.That(loop.Run, Is.EqualTo(0));
            loop.Start();
            Assert.That(loop.IsRunning && loop.IsAttempting, Is.True);
            Assert.That(loop.NextAttemptAt, Is.Null, "intentando: sin próximo");
            Assert.That(loop.Run, Is.EqualTo(1));
            Elapse(4);
            flight.SetResult(false);
            Assert.That(loop.IsRunning, Is.True, "esperando también es reintentar");
            Assert.That(loop.IsAttempting, Is.False);
            Assert.That(loop.NextAttemptAt, Is.EqualTo(14.0), "10 s desde el fin del intento");
            Assert.That(changes, Is.GreaterThanOrEqualTo(2));
            flight = new TaskCompletionSource<bool>();
            Elapse(10);
            Assert.That(loop.IsAttempting, Is.True);
            Assert.That(loop.Attempts, Is.EqualTo(2));
            loop.Cancel();
            Assert.That(loop.IsRunning || loop.IsAttempting, Is.False);
            Assert.That(loop.NextAttemptAt, Is.Null);
        }
    }
}
