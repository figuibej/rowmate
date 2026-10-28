namespace RowMate.Infrastructure.Database
{
    // Nombre del archivo de la base según la variante (F-HIS-17, F-HIS-20; legacy: lib/core/database/database_service.dart
    // :10-12): el simulador usa su propia base, así las pruebas no ensucian las rutinas reales (AC-HIS-20-01). El único
    // lugar del código que elige el archivo es AppRoot, con BuildFlags.IsSimulator (ItThreeToolingTests). Ningún otro
    // archivo de producción nombra rower_app (LegacyIsolationTests).
    public static class DatabaseFiles
    {
        public const string NormalName = "rower_app.db";
        public const string SimulatorName = "rower_app_dev.db";

        // Esquema propio v8 = el v7 del legacy + workout_sessions.origin + el índice de data_points (ADR 008, P-02).
        public const int SchemaVersion = 8;

        public static string NameFor(bool simulator)
        {
            return simulator ? SimulatorName : NormalName;
        }
    }
}
