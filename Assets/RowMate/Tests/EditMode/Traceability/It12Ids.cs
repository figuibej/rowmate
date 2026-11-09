namespace RowMate.Tests.EditMode.Traceability
{
    // IDs de la spec de la It. 12 (12-exportar-tcx/01-po-spec §3): los 28 AC de F-STR-09 (AC-STR-09-01 a -28), sin casos legacy (el
    // legacy no tiene tests del TCX), cada uno con su verificación de 02-tech-design §6.3:
    //   d = dotnet (y Unity EditMode) en archivos de WP-A · p = PlayMode / Unity EditMode (WP-B1) · cap = captura del arnés (WP-B2) ·
    //   eq = equipo o demo ([Device-Win] -ExportSmoke, [Device-Android] NO VERIFICABLE, [Apple] 10b, [Manual] demo del usuario)
    // Literales explícitos: tools/dotnet/run-tests.ps1 -Matrix lee este archivo y sus bloques UnityOnlyIds y WorkPackageBIds.
    public static class It12Ids
    {
        public static readonly (string Id, string Checks)[] Acceptance =
        {
            ("AC-STR-09-01", "d"), ("AC-STR-09-02", "d"), ("AC-STR-09-03", "d p"), ("AC-STR-09-04", "d"), ("AC-STR-09-05", "d"),
            ("AC-STR-09-06", "d"), ("AC-STR-09-07", "d"), ("AC-STR-09-08", "d"), ("AC-STR-09-09", "d"), ("AC-STR-09-10", "d p"),
            ("AC-STR-09-11", "d p"), ("AC-STR-09-12", "d p"), ("AC-STR-09-13", "d"), ("AC-STR-09-14", "d"), ("AC-STR-09-15", "d"),
            ("AC-STR-09-16", "d p eq"), ("AC-STR-09-17", "p cap"), ("AC-STR-09-18", "d p cap"), ("AC-STR-09-19", "d p cap"),
            ("AC-STR-09-20", "d p"), ("AC-STR-09-21", "d p cap"), ("AC-STR-09-22", "p cap"), ("AC-STR-09-23", "d p"),
            ("AC-STR-09-24", "d eq"), ("AC-STR-09-25", "eq"), ("AC-STR-09-26", "d eq"), ("AC-STR-09-27", "eq"), ("AC-STR-09-28", "d p eq"),
        };

        // Sin verificación de dotnet: solo PlayMode o captura (PENDIENTE UNITY en run-tests.ps1 -Matrix).
        public static readonly string[] UnityOnlyIds =
        {
            "AC-STR-09-17", "AC-STR-09-22",
        };

        // Solo de equipo o demo (eq): -25 a la 10b ([Apple]), -27 la subida a mano del usuario ([Manual]).
        public static readonly string[] DeviceOnlyIds =
        {
            "AC-STR-09-25", "AC-STR-09-27",
        };

        // Su única verificación automática es de dotnet pero en un archivo de WP-B: ninguno.
        public static readonly string[] WorkPackageBIds =
        {
        };
    }
}
