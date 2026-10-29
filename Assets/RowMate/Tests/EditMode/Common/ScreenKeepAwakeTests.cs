using System;
using NUnit.Framework;
using RowMate.Core.Common;
using RowMate.Core.Ports;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Common
{
    // SKA: la regla de la pantalla siempre encendida (F-APP-14, Q-56; 02-tech-design §4.4 de la It. 4a) con FakeWakeLock y
    // FakeAppLifecycle: pedida al arrancar y al volver (fin del segundo plano o foco recuperado), liberada al cerrar, y un
    // rechazo que solo se registra, una vez. El pedido real de cada plataforma lo prueba WLK en Unity EditMode.
    // legacy: lib/main.dart:42 (WakelockPlus.enable(), una vez y sin soltarla).
    [TestFixture]
    public sealed class ScreenKeepAwakeTests
    {
        private const string Rejected = "[APP] Warning: pantalla encendida: el sistema la rechazó";

        [Test, Category("AC-APP-14-04")]
        public void RejectedRequestOnlyLogsOnce()
        {
            // Paridad: el usuario no ve nada; el registro dice UNA vez que el sistema la rechazó, aunque se vuelva a pedir.
            var logger = new RecordingLogger();
            var lifecycle = new FakeAppLifecycle(logger);
            var wakeLock = new FakeWakeLock { Accepts = false };
            using (var keepAwake = new ScreenKeepAwake(wakeLock, lifecycle, logger))
            {
                keepAwake.Start();
                Assert.That(keepAwake.IsHeld, Is.False);
                Assert.That(logger.Lines, Is.EqualTo(new[] { Rejected }));
                lifecycle.SetBackground(true);
                lifecycle.SetBackground(false);
                lifecycle.SetFocus(false);
                lifecycle.SetFocus(true);
                Assert.That(wakeLock.EnableCalls, Is.EqualTo(3), "se vuelve a pedir");
                Assert.That(logger.Lines, Is.EqualTo(new[] { Rejected }), "una sola vez");
                wakeLock.Accepts = true;
                keepAwake.Request();
                Assert.That(keepAwake.IsHeld, Is.True);
            }
        }

        [Test, Category("AC-APP-14-04")]
        public void ThrowingWakeLockCountsAsARejection()
        {
            var logger = new RecordingLogger();
            using (var keepAwake = new ScreenKeepAwake(new ThrowingWakeLock(), new FakeAppLifecycle(logger), logger))
            {
                keepAwake.Start();
                Assert.That(keepAwake.IsHeld, Is.False);
                Assert.That(logger.Lines, Has.Some.EqualTo(Rejected));
                keepAwake.Release();
                Assert.That(logger.Lines, Has.Some.EqualTo("[APP] Warning: pantalla encendida: no se pudo liberar (InvalidOperationException)"));
            }
        }

        [Test, Category("AC-APP-14-02")]
        public void RequestsAgainOnResumeAndFocus()
        {
            // QA-G4aT-09: vuelve a pedir cuando termina el segundo plano y cuando vuelve el foco (Android puede soltarla con la
            // app atrás; Windows, con la ventana minimizada), nunca al irse; y después de Release no vuelve a pedir.
            var logger = new RecordingLogger();
            var lifecycle = new FakeAppLifecycle(logger);
            var wakeLock = new FakeWakeLock();
            var keepAwake = new ScreenKeepAwake(wakeLock, lifecycle, logger);
            lifecycle.SetBackground(true);
            lifecycle.SetBackground(false);
            Assert.That(wakeLock.EnableCalls, Is.EqualTo(0), "antes de Start no pide");
            keepAwake.Start();
            keepAwake.Start();
            Assert.That(wakeLock.EnableCalls, Is.EqualTo(1), "Start es idempotente");
            Assert.That(keepAwake.IsHeld, Is.True);
            lifecycle.SetBackground(true);
            Assert.That(wakeLock.EnableCalls, Is.EqualTo(1), "al irse no pide");
            lifecycle.SetBackground(false);
            Assert.That(wakeLock.EnableCalls, Is.EqualTo(2), "al volver del segundo plano");
            lifecycle.SetFocus(false);
            Assert.That(wakeLock.EnableCalls, Is.EqualTo(2));
            lifecycle.SetFocus(true);
            Assert.That(wakeLock.EnableCalls, Is.EqualTo(3), "al recuperar el foco");
            Assert.That(wakeLock.IsHeld, Is.True);
            keepAwake.Release();
            Assert.That(wakeLock.DisableCalls, Is.EqualTo(1));
            Assert.That(keepAwake.IsHeld, Is.False);
            lifecycle.SetBackground(true);
            lifecycle.SetBackground(false);
            lifecycle.SetFocus(false);
            lifecycle.SetFocus(true);
            keepAwake.Request();
            keepAwake.Start();
            Assert.That(wakeLock.EnableCalls, Is.EqualTo(3), "después de Release no vuelve a pedir");
            keepAwake.Dispose();
            keepAwake.Dispose();
            Assert.That(wakeLock.DisableCalls, Is.EqualTo(1), "Dispose = Release, una vez");
            Assert.That(logger.Lines, Is.Empty);
        }

        [Test, Category("AC-APP-14-02")]
        public void DisposeUnsubscribes()
        {
            var logger = new RecordingLogger();
            var lifecycle = new FakeAppLifecycle(logger);
            var wakeLock = new FakeWakeLock();
            var keepAwake = new ScreenKeepAwake(wakeLock, lifecycle, logger);
            keepAwake.Start();
            keepAwake.Dispose();
            lifecycle.SetBackground(true);
            lifecycle.SetBackground(false);
            Assert.That(wakeLock.EnableCalls, Is.EqualTo(1));
            Assert.That(wakeLock.DisableCalls, Is.EqualTo(1));
        }

        [Test]
        public void ArgumentsAreChecked()
        {
            var logger = new RecordingLogger();
            var lifecycle = new FakeAppLifecycle(logger);
            Assert.Throws<ArgumentNullException>(() => new ScreenKeepAwake(null!, lifecycle, logger));
            Assert.Throws<ArgumentNullException>(() => new ScreenKeepAwake(new FakeWakeLock(), null!, logger));
            Assert.Throws<ArgumentNullException>(() => new ScreenKeepAwake(new FakeWakeLock(), lifecycle, null!));
        }

        // Un pedido que lanza (un plugin roto): cuenta como rechazo.
        private sealed class ThrowingWakeLock : IWakeLock
        {
            public bool Enable()
            {
                throw new InvalidOperationException("roto");
            }

            public void Disable()
            {
                throw new InvalidOperationException("roto");
            }
        }
    }
}
