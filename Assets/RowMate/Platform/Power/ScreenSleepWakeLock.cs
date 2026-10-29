using RowMate.Core.Ports;
using UnityEngine;

namespace RowMate.Platform.Power
{
    // Pantalla siempre encendida en Android e iOS (F-APP-14; 02-tech-design §4.4 de la It. 4a): Screen.sleepTimeout =
    // NeverSleep (en Android Unity pone FLAG_KEEP_SCREEN_ON en la ventana de la actividad, como el legacy, Wakelock.kt:24;
    // en iOS, idleTimerDisabled): vale mientras la app está adelante. Disable: SleepTimeout.SystemSetting. Siempre acepta.
    public sealed class ScreenSleepWakeLock : IWakeLock
    {
        public bool Enable()
        {
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            return true;
        }

        public void Disable()
        {
            Screen.sleepTimeout = SleepTimeout.SystemSetting;
        }
    }
}
