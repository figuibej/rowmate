namespace RowMate.Core.Common
{
    // Identidad de la app (F-APP-16, R-14, Q-85, Q-90). Fuente única: la usan el bootstrapper del Editor
    // (Editor/Bootstrap) y los tests; nadie repite estos literales.
    public static class AppIdentity
    {
        public const string ProductName = "RowMate";
        public const string CompanyName = "iguisoft";

        // legacy: pubspec.yaml:4 (version: 1.0.0+1).
        public const string Version = "1.0.0";
        public const int BuildNumber = 1;

        // Android (el legacy usa el id de ejemplo de Flutter, android/app/build.gradle.kts:24).
        public const string AndroidApplicationId = "com.iguisoft.rowmate";

        // RESERVADO para el paquete MSIX de la It. 14 (AC-APP-16-02, D-02): Unity no tiene identificador
        // de aplicación para Windows y ningún paso del bootstrapper lo aplica.
        public const string WindowsApplicationId = "com.iguisoft.rowmate";

        // iOS y macOS (R-14: distinto del legacy com.iguisoft.rowmate, que un build con ese id pisaría).
        // Es también el identificador único de Standalone que comparten macOS y Windows (D-02).
        public const string AppleBundleId = "com.iguisoft.rowmate.unity";

        // Android 8.0 (API 26) o más nuevo, solo ARM64 (Q-90).
        public const int AndroidMinSdk = 26;

        // Como el legacy (ios/Runner.xcodeproj/project.pbxproj:461).
        public const string IosMinimumVersion = "15.0";
    }
}
