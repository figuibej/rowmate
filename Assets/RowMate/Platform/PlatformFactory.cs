using RowMate.Core.Common;
using RowMate.Platform.Accessibility;
using RowMate.Platform.Display;
using RowMate.Platform.Input;
using RowMate.Platform.Locale;
using RowMate.Platform.Power;
using RowMate.Platform.Storage;
using UnityEngine;

namespace RowMate.Platform
{
    // Arma PlatformServices para la plataforma en la que corre (02-tech-design §1.4 paso 3). Sin estáticos guardados: lo
    // llama una vez AppRoot.Awake y el resultado vive en el composition root.
    public static class PlatformFactory
    {
        public static PlatformServices Create(IAppLogger logger)
        {
            return new PlatformServices(new SystemLocaleProvider(), new AccessibilitySettingsProvider(logger), new DisplayMetricsProvider(),
                BackButtonPolicy.ForCurrentPlatform(), CurrentKind(), IsDesktop(), new WindowsTitleBar(logger), new AppDataLocation(),
                new SystemClipboard(), new SoftKeyboard(Application.isMobilePlatform), WakeLockFactory.Create(logger))
            {
                // It. 4b: las barras del sistema (F-APP-15) y la fuente de sueño del equipo (P-17).
                SystemUi = RowMate.Platform.SystemUi.SystemUiFactory.Create(logger),
                SleepSource = RowMate.Platform.Power.SleepSourceFactory.Create(logger),
            };
        }

        public static PlatformKind CurrentKind()
        {
            switch (Application.platform)
            {
                case RuntimePlatform.Android:
                    return PlatformKind.Android;
                case RuntimePlatform.IPhonePlayer:
                    return PlatformKind.Ios;
                case RuntimePlatform.OSXPlayer:
                case RuntimePlatform.OSXEditor:
                    return PlatformKind.MacOs;
                default:
                    return PlatformKind.Windows;
            }
        }

        public static bool IsDesktop()
        {
            return !Application.isMobilePlatform;
        }
    }
}
