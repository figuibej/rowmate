using System.Threading;
using NUnit.Framework;
using RowMate.Core.Common;
using RowMate.Core.Ports;
using RowMate.Platform.Power;
using RowMate.Tests.Support;
using UnityEngine;

namespace RowMate.Tests.UnityEditMode
{
    // WLK · La pantalla encendida de cada plataforma (F-APP-14, Q-56; 04a-motor-hud-funcional/02-tech-design §4.4; R4a-07):
    // en el Editor de Windows, SetThreadExecutionState de verdad (pedido y liberado en el mismo hilo); Android e iOS por el
    // camino de código (Screen.sleepTimeout); el resto, NoWakeLock hasta la It. 10b.
    public sealed class WakeLockTests
    {
        private static RecordingLogger NewLogger()
        {
            return new RecordingLogger();
        }

        [Test, Category("AC-APP-14-01")]
        public void WindowsRequestIsAcceptedAndReleased()
        {
            if (Application.platform != RuntimePlatform.WindowsEditor)
            {
                Assert.Ignore("NO VERIFICABLE fuera del Editor de Windows: SetThreadExecutionState es de kernel32");
            }

            var wakeLock = new WindowsWakeLock(NewLogger());
            try
            {
                Assert.That(wakeLock.Enable(), Is.True, "el sistema acepta ES_CONTINUOUS | ES_DISPLAY_REQUIRED | ES_SYSTEM_REQUIRED");
                Assert.That(wakeLock.IsHeld, Is.True);
                Assert.That(wakeLock.LastResult, Is.Not.EqualTo(0u), "≠ 0: aceptado");
                Assert.That(WindowsWakeLock.RequestFlags, Is.EqualTo(0x80000003u));
                Assert.That(wakeLock.RequestThreadId, Is.EqualTo(Thread.CurrentThread.ManagedThreadId));
            }
            finally
            {
                wakeLock.Disable();
            }

            Assert.That(wakeLock.IsHeld, Is.False, "liberada");
            Assert.That(wakeLock.LastResult, Is.Not.EqualTo(0u), "la liberación también vuelve ≠ 0");
        }

        [Test, Category("AC-APP-14-03")]
        public void ReleaseRunsOnTheRequestingThread()
        {
            if (Application.platform != RuntimePlatform.WindowsEditor)
            {
                Assert.Ignore("NO VERIFICABLE fuera del Editor de Windows");
            }

            var logger = NewLogger();
            var wakeLock = new WindowsWakeLock(logger);
            try
            {
                Assert.That(wakeLock.Enable(), Is.True);
                var other = new Thread(() => wakeLock.Disable());
                other.Start();
                other.Join();
                Assert.That(wakeLock.CrossThreadReleases, Is.EqualTo(1), "un Disable desde otro hilo no llama al sistema (no liberaría nada)");
                Assert.That(wakeLock.IsHeld, Is.True, "sigue pedida");
                Assert.That(logger.Levels, Has.Member(LogLevel.Warning), "y queda anotado como Warning");
            }
            finally
            {
                wakeLock.Disable();
            }

            Assert.That(wakeLock.IsHeld, Is.False, "desde el hilo del pedido, sí");
        }

        [Test, Category("AC-APP-14-01")]
        public void AndroidAndIosKeepTheScreenOnWithSleepTimeout()
        {
            var previous = Screen.sleepTimeout;
            try
            {
                IWakeLock wakeLock = new ScreenSleepWakeLock();
                Assert.That(wakeLock.Enable(), Is.True);
                Assert.That(Screen.sleepTimeout, Is.EqualTo(SleepTimeout.NeverSleep), "FLAG_KEEP_SCREEN_ON en Android");
                wakeLock.Disable();
                Assert.That(Screen.sleepTimeout, Is.EqualTo(SleepTimeout.SystemSetting));
            }
            finally
            {
                Screen.sleepTimeout = previous;
            }
        }

        [Test, Category("AC-APP-14-03")]
        public void OtherPlatformsRejectUntil10b()
        {
            Assert.That(NoWakeLock.Instance.Enable(), Is.False, "macOS y el resto: rechazo (queda el aviso del registro)");
            Assert.DoesNotThrow(() => NoWakeLock.Instance.Disable());
            var created = WakeLockFactory.Create(NewLogger());
            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                Assert.That(created, Is.InstanceOf<WindowsWakeLock>(), "en el Editor de Windows, la de Windows");
            }
        }
    }
}
