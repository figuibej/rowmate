using RowMate.Core.Common;
using RowMate.Core.Ports;
using UnityEngine;

namespace RowMate.Platform.Power
{
    // Elige la pantalla encendida de la plataforma (02-tech-design §4.4 de la It. 4a): Windows (build y Editor de Windows)
    // SetThreadExecutionState; Android e iOS Screen.sleepTimeout; macOS y el resto, ninguna hasta la It. 10b.
    public static class WakeLockFactory
    {
        public static IWakeLock Create(IAppLogger logger)
        {
            switch (Application.platform)
            {
                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.WindowsEditor:
                    return new WindowsWakeLock(logger);
                case RuntimePlatform.Android:
                case RuntimePlatform.IPhonePlayer:
                    return new ScreenSleepWakeLock();
                default:
                    return NoWakeLock.Instance;
            }
        }
    }
}
