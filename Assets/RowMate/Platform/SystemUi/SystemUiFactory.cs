using RowMate.Core.Common;
using RowMate.Core.Ports;
using UnityEngine;

namespace RowMate.Platform.SystemUi
{
    // Elige las barras del sistema por plataforma (04b-hud-exr/02-tech-design §4.2): Android por JNI; escritorio, Editor e iOS
    // (hasta la 10b), sin efecto.
    public static class SystemUiFactory
    {
        public static ISystemUi Create(IAppLogger logger)
        {
            return Application.platform == RuntimePlatform.Android ? new AndroidSystemUi(logger) : (ISystemUi)DesktopSystemUi.Instance;
        }
    }
}
