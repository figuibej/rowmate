namespace RowMate.Tests.EditMode.Traceability
{
    // IDs de la spec de la It. 5 (05-series-parciales/01-po-spec, corregida por su gate QA): los 74 AC (46 nuevos, §4.1 a §4.4 y
    // §5.1, y 28 de iteraciones anteriores que cambian o se vuelven a verificar, §5.2 y §5.3) y los casos legacy LT-47 a LT-61
    // (§6), cada uno con su verificación de 02-tech-design §6.5:
    //   d = dotnet (y Unity EditMode) en archivos de WP-A · s = dotnet con el simulador · u = Unity EditMode solo
    //   (Tests/UnityEditMode) · p = PlayMode (Tests/PlayMode) · ps = PlayMode con el simulador (Tests/PlayModeSimulator) ·
    //   cap = captura del arnés (Snapshots) · eq = equipo (QA o el usuario) · qa = revisión de QA
    // Ningún ID queda solo con eq o qa: las partes sin módulo, sin teléfono o del usuario tienen su [Ignore] en
    // WorkoutDeviceChecksTests. Literales explícitos: tools/dotnet/run-tests.ps1 -Matrix lee este archivo (todos los It0*Ids.cs)
    // y sus bloques UnityOnlyIds y WorkPackageBIds.
    public static class It05Ids
    {
        public static readonly (string Id, string Checks)[] Acceptance =
        {
            ("AC-WKT-07-01", "d cap"), ("AC-WKT-07-02", "d"), ("AC-WKT-07-03", "d cap"), ("AC-WKT-07-04", "d cap"),
            ("AC-WKT-07-05", "d cap"), ("AC-WKT-07-06", "d"), ("AC-WKT-07-07", "d cap"), ("AC-WKT-07-08", "d"),
            ("AC-WKT-07-09", "d"), ("AC-WKT-07-10", "d"), ("AC-WKT-07-11", "d cap"), ("AC-WKT-07-12", "d"),
            ("AC-WKT-08-01", "d cap"), ("AC-WKT-08-02", "d cap"), ("AC-WKT-08-03", "d cap"), ("AC-WKT-08-04", "d cap"),
            ("AC-WKT-08-05", "d cap"), ("AC-WKT-08-06", "d cap"), ("AC-WKT-08-07", "d cap"), ("AC-WKT-08-08", "d cap"),
            ("AC-WKT-18-01", "d p cap"), ("AC-WKT-18-02", "p cap"), ("AC-WKT-18-03", "p cap"), ("AC-WKT-18-04", "d p cap"),
            ("AC-WKT-18-05", "d p cap"), ("AC-WKT-18-06", "d p cap"), ("AC-WKT-18-07", "d p cap"), ("AC-WKT-18-08", "d p cap"),
            ("AC-WKT-18-09", "d p cap"), ("AC-WKT-18-10", "d p eq cap"), ("AC-WKT-18-11", "d u p qa"), ("AC-WKT-18-12", "p eq cap"),
            ("AC-WKT-18-13", "d p eq cap"), ("AC-WKT-18-14", "d p cap"), ("AC-WKT-18-15", "d p cap"), ("AC-WKT-18-16", "d p cap"),
            ("AC-WKT-18-17", "d p cap"), ("AC-WKT-18-18", "d p cap"), ("AC-WKT-18-19", "d p cap"), ("AC-WKT-18-20", "p cap"),
            ("AC-WKT-18-21", "p eq"), ("AC-WKT-18-22", "d p cap"), ("AC-WKT-13-15", "p eq"), ("AC-WKT-13-16", "p"),
            ("AC-WKT-16-07", "p cap"), ("AC-APP-10-12", "d"),
            ("AC-WKT-13-06", "d p cap"), ("AC-WKT-13-08", "p cap"), ("AC-WKT-13-12", "p cap"), ("AC-WKT-13-13", "d p cap"),
            ("AC-WKT-14-18", "d p cap"), ("AC-WKT-17-06", "p"), ("AC-WKT-17-09", "d p cap"), ("AC-WKT-02-08", "p eq"),
            ("AC-APP-05-07", "p eq cap"), ("AC-APP-05-08", "d p cap"), ("AC-BLE-16-02", "s ps cap"), ("AC-BLE-16-03", "ps cap"),
            ("AC-HIS-20-04", "d u qa"),
            ("AC-WKT-01-05", "d"), ("AC-WKT-02-04", "d"), ("AC-WKT-02-11", "d eq"), ("AC-WKT-03-03", "d"),
            ("AC-WKT-11-09", "d cap"), ("AC-WKT-11-10", "d cap"), ("AC-WKT-11-11", "d cap"), ("AC-WKT-13-07", "p"),
            ("AC-WKT-13-09", "p"), ("AC-WKT-13-14", "d p"), ("AC-WKT-17-01", "p cap"), ("AC-WKT-17-07", "p"),
            ("AC-WKT-17-10", "p"), ("AC-APP-05-05", "p"), ("AC-BLE-15-09", "s ps cap"),
        };

        // Los 28 de §5.2 y §5.3 (deben estar en It01Ids a It04bIds).
        public static readonly string[] FromEarlier =
        {
            "AC-WKT-13-06", "AC-WKT-13-08", "AC-WKT-13-12", "AC-WKT-13-13", "AC-WKT-14-18", "AC-WKT-17-06", "AC-WKT-17-09", "AC-WKT-02-08",
            "AC-APP-05-07", "AC-APP-05-08", "AC-BLE-16-02", "AC-BLE-16-03", "AC-HIS-20-04", "AC-WKT-01-05", "AC-WKT-02-04", "AC-WKT-02-11",
            "AC-WKT-03-03", "AC-WKT-11-09", "AC-WKT-11-10", "AC-WKT-11-11", "AC-WKT-13-07", "AC-WKT-13-09", "AC-WKT-13-14", "AC-WKT-17-01",
            "AC-WKT-17-07", "AC-WKT-17-10", "AC-APP-05-05", "AC-BLE-15-09",
        };

        // Los casos de la spec §6: archivo y línea del legacy, el nombre exacto y la verificación.
        public static readonly (string Id, string File, int Line, string Name, string Checks)[] LegacyCases =
        {
            ("LT-47", "test/series_tracker_test.dart", 46, "interpola el tiempo de la vuelta entre lecturas", "d"),
            ("LT-48", "test/series_tracker_test.dart", 59, "dos vueltas dentro del mismo paso", "d"),
            ("LT-49", "test/series_tracker_test.dart", 65, "un paso de trabajo de menos de 500 m no genera vueltas", "d"),
            ("LT-50", "test/series_tracker_test.dart", 71, "el enfriamiento no genera vueltas pero suma metros a la repetición", "d"),
            ("LT-51", "test/series_tracker_test.dart", 85, "al cambiar de repetición guarda el resumen de la anterior", "d"),
            ("LT-52", "test/series_tracker_test.dart", 102, "guarda solo las últimas 3 repeticiones, la más reciente primero", "d"),
            ("LT-53", "test/series_tracker_test.dart", 112, "una serie nueva o un paso suelto vacían la comparación", "d"),
            ("LT-54", "test/series_tracker_test.dart", 126, "una repetición sin metros de trabajo no tiene split", "d"),
            ("LT-55", "test/series_tracker_test.dart", 155, "una lectura de 0 dentro del paso no genera metros negativos ni dobles", "d"),
            ("LT-56", "test/series_tracker_test.dart", 180, "un primer salto desde 0 no crea vueltas falsas", "d"),
            ("LT-57", "test/series_tracker_test.dart", 191, "un reinicio del monitor no genera valores negativos", "d"),
            ("LT-58", "test/series_tracker_test.dart", 211, "rebase no cuenta el salto al reanudar", "d"),
            ("LT-59", "test/series_panels_test.dart", 39, "arrancan colapsados y al tocar se expanden y se recuerda", "d p"),
            ("LT-60", "test/series_panels_test.dart", 62, "respeta la preferencia guardada", "d p"),
            ("LT-61", "test/series_panels_test.dart", 68, "sin serie no aparece el panel de repeticiones", "d p"),
        };

        // Sin verificación de dotnet (ni d ni s): solo PlayMode, Unity EditMode, captura o equipo (los 20 de 02-tech-design §6.5).
        // En run-tests.ps1 -Matrix figuran como PENDIENTE UNITY (no SIN TEST) mientras no se le pase -UnityResults.
        // TraceabilityTests controla que la lista sea exactamente la que sale de Acceptance.
        public static readonly string[] UnityOnlyIds =
        {
            "AC-WKT-18-02", "AC-WKT-18-03", "AC-WKT-18-12", "AC-WKT-18-20", "AC-WKT-18-21", "AC-WKT-13-15", "AC-WKT-13-16", "AC-WKT-16-07",
            "AC-WKT-13-08", "AC-WKT-13-12", "AC-WKT-17-06", "AC-WKT-02-08", "AC-APP-05-07", "AC-BLE-16-03", "AC-WKT-13-07", "AC-WKT-13-09",
            "AC-WKT-17-01", "AC-WKT-17-07", "AC-WKT-17-10", "AC-APP-05-05",
        };

        // Su única verificación automática es de dotnet pero en un archivo de WP-B: ninguno en la It. 5.
        public static readonly string[] WorkPackageBIds =
        {
        };
    }
}
