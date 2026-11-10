namespace RowMate.Core.Design
{
    // Los tamaños del ícono por plataforma (It. 14, DT14-05; 00-arte §1.5), versionados. Un test de EditMode de Unity (WP-B1,
    // IdentityStepTests) compara StandaloneUnity con PlayerSettings.GetIconSizes(Standalone, Application) de 6000.3.
    public static class AppIconSizes
    {
        // Los que pide Unity para el .ico del .exe (se esperan; los confirma el test de Unity).
        public static readonly int[] StandaloneUnity = { 1024, 512, 256, 128, 48, 32, 16 };

        // Los que se generan para Windows: los de Unity más 24 y 64 (CAP-01 y la spec los piden).
        public static readonly int[] Standalone = { 16, 24, 32, 48, 64, 128, 256, 512, 1024 };

        // Android adaptativo (108 dp de xxxhdpi a ldpi) y redondo/legacy (48 dp de xxxhdpi a ldpi).
        public static readonly int[] AndroidAdaptive = { 432, 324, 216, 162, 108, 81 };

        public static readonly int[] AndroidLegacy = { 192, 144, 96, 72, 48, 36 };

        // macOS: 16 a 1024.
        public static readonly int[] Mac = { 16, 32, 64, 128, 256, 512, 1024 };

        // La marca del launch screen de iOS: 96 pt a @1x, @2x y @3x.
        public static readonly int[] LaunchMark = { 96, 192, 288 };

        // La marca del arranque (96 dp × 4, DT14-10) y la imagen estática de Android 8-11.
        public const int BootMark = 384;

        // iOS: un único 1024 universal, RGB sin alfa (D-03).
        public const int Ios = 1024;

        // El maestro.
        public const int Master = 1024;
    }
}
