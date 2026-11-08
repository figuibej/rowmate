using RowMate.Core.Common;
using RowMate.Core.Ports;

namespace RowMate.Platform.Haptics
{
    // La respuesta táctil de la plataforma (A10-06, D10-11): Android, AndroidHaptics (WP-C: performHapticFeedback, sin el permiso
    // VIBRATE); el resto, NoHaptics.
    public static class HapticsFactory
    {
        public static IHaptics Create(IAppLogger logger)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return new AndroidHaptics(logger);
#else
            return NoHaptics.Instance;
#endif
        }
    }
}
