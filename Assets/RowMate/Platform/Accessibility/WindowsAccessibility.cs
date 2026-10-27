using System;
using System.Runtime.InteropServices;

namespace RowMate.Platform.Accessibility
{
    // Accesibilidad de Windows (ARCHITECTURE §13.2): "Mostrar animaciones en Windows" (SPI_GETCLIENTAREAANIMATION; apagado =
    // reducir movimiento) y el tamaño del texto (HKCU\Software\Microsoft\Accessibility\TextScaleFactor, 100-225 %; si falta,
    // 100). Si el legacy no respeta la escala de Windows se apaga acá (D-23, se compara en equipo).
    internal static class WindowsAccessibility
    {
        private const uint SpiGetClientAreaAnimation = 0x1042;
        private const uint RrfRtRegDword = 0x00000010;
        private static readonly UIntPtr HkeyCurrentUser = new UIntPtr(0x80000001u);

        public static bool ReduceMotion()
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            try
            {
                var enabled = 1;
                return SystemParametersInfo(SpiGetClientAreaAnimation, 0, ref enabled, 0) && enabled == 0;
            }
            catch (Exception)
            {
                return false;
            }
#else
            return false;
#endif
        }

        public static double TextScale()
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            try
            {
                uint value = 0;
                uint size = 4;
                uint type;
                var status = RegGetValue(HkeyCurrentUser, @"Software\Microsoft\Accessibility", "TextScaleFactor", RrfRtRegDword,
                    out type, ref value, ref size);
                return status == 0 && value >= 100 && value <= 500 ? value / 100.0 : 1.0;
            }
            catch (Exception)
            {
                return 1.0;
            }
#else
            return 1.0;
#endif
        }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SystemParametersInfo(uint action, uint param, ref int value, uint winIni);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
        private static extern int RegGetValue(UIntPtr key, string subKey, string valueName, uint flags, out uint type, ref uint data,
            ref uint dataSize);
#endif
    }
}
