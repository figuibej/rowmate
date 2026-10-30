namespace RowMate.Tests.EditMode.Traceability
{
    // IDs de la spec de la It. 4b (04b-hud-exr/01-po-spec, corregida por su gate QA): los 101 AC (61 nuevos, §4.1 a §4.8, y 40
    // de iteraciones anteriores que se verifican o cambian acá, §5) y el caso legacy LT-46 (§6), cada uno con su verificación de
    // 02-tech-design §6.4:
    //   d = dotnet (y Unity EditMode) en archivos de WP-A · dB = dotnet en un archivo de WP-B · s = dotnet con el simulador
    //   u = Unity EditMode solo (Tests/UnityEditMode) · p = PlayMode (Tests/PlayMode) · ps = PlayMode con el simulador
    //   (Tests/PlayModeSimulator) · cap = captura del arnés (Snapshots) · eq = equipo (QA o el usuario) · qa = revisión de QA
    // Ningún ID queda solo con eq o qa: las partes sin módulo, sin teléfono o del usuario tienen su [Ignore] en
    // WorkoutDeviceChecksTests. Literales explícitos: tools/dotnet/run-tests.ps1 -Matrix lee este archivo (todos los It0*Ids.cs)
    // y sus bloques UnityOnlyIds y WorkPackageBIds.
    public static class It04bIds
    {
        public static readonly (string Id, string Checks)[] Acceptance =
        {
            ("AC-WKT-15-01", "d p cap"), ("AC-WKT-15-02", "d p cap"), ("AC-WKT-15-03", "d p cap"), ("AC-WKT-15-04", "d p cap"),
            ("AC-WKT-15-05", "d p cap"), ("AC-WKT-15-06", "d cap qa"), ("AC-WKT-14-07", "d p cap"), ("AC-WKT-14-08", "d p cap"),
            ("AC-WKT-14-09", "d p cap"), ("AC-WKT-14-10", "d p cap"), ("AC-WKT-14-11", "d p cap"), ("AC-WKT-14-12", "d p cap"),
            ("AC-WKT-14-13", "d p cap"), ("AC-WKT-14-14", "d p cap"), ("AC-WKT-14-15", "d p cap"), ("AC-WKT-14-16", "d p cap"),
            ("AC-WKT-14-17", "p"), ("AC-WKT-14-18", "d p cap"), ("AC-WKT-14-19", "p eq"), ("AC-WKT-19-01", "d p cap"),
            ("AC-WKT-19-02", "d p"), ("AC-WKT-19-03", "d p"), ("AC-WKT-19-04", "d p"), ("AC-WKT-19-05", "p"),
            ("AC-WKT-16-02", "p cap"), ("AC-WKT-16-03", "d p cap"), ("AC-WKT-16-04", "d p cap"), ("AC-WKT-16-05", "d p cap"),
            ("AC-WKT-16-06", "d p cap"), ("AC-WKT-21-01", "d p cap"), ("AC-WKT-21-02", "p eq"), ("AC-WKT-21-03", "d p cap"),
            ("AC-WKT-21-04", "d p"), ("AC-WKT-21-05", "d p cap"), ("AC-WKT-21-06", "d p cap"), ("AC-WKT-21-07", "d p"),
            ("AC-WKT-21-08", "d p"), ("AC-WKT-21-09", "d p eq"), ("AC-WKT-21-10", "p qa"), ("AC-WKT-21-11", "p eq"),
            ("AC-WKT-21-12", "p cap"), ("AC-WKT-21-13", "d"), ("AC-WKT-13-06", "d p cap"), ("AC-WKT-13-07", "p eq"),
            ("AC-WKT-13-08", "d p cap"), ("AC-WKT-13-09", "p eq"), ("AC-WKT-13-10", "p"), ("AC-WKT-13-11", "d p"),
            ("AC-WKT-13-12", "d p cap"), ("AC-WKT-13-13", "d p cap"), ("AC-WKT-13-14", "d p cap"), ("AC-WKT-17-11", "d p cap"),
            ("AC-WKT-17-12", "d p cap qa"), ("AC-WKT-09-08", "d p cap"), ("AC-WKT-09-09", "d p cap"), ("AC-WKT-02-11", "d p eq"),
            ("AC-APP-15-01", "d p eq"), ("AC-APP-15-02", "p eq"), ("AC-APP-15-03", "d eq"), ("AC-APP-15-04", "d p"),
            ("AC-APP-10-11", "d"), ("AC-WKT-17-04", "p cap"), ("AC-APP-05-07", "p eq"), ("AC-WKT-01-06", "d p"),
            ("AC-WKT-02-04", "d"), ("AC-WKT-02-06", "d p"), ("AC-WKT-02-08", "d p eq"), ("AC-WKT-02-09", "d p eq"),
            ("AC-WKT-03-05", "d p cap"), ("AC-WKT-06-12", "d p cap"), ("AC-WKT-13-01", "p"), ("AC-WKT-13-04", "p"),
            ("AC-WKT-14-01", "d p cap"), ("AC-WKT-14-06", "p cap"), ("AC-WKT-16-01", "p cap"), ("AC-WKT-20-07", "p"),
            ("AC-APP-03-09", "p cap"), ("AC-APP-03-10", "p cap"), ("AC-APP-03-11", "d p cap"), ("AC-APP-05-05", "p cap"),
            ("AC-APP-05-08", "d p"), ("AC-BLE-16-01", "ps cap"), ("AC-BLE-16-02", "s ps cap"), ("AC-BLE-16-03", "d s ps cap"),
            ("AC-WKT-13-02", "p cap"), ("AC-WKT-13-03", "p eq"), ("AC-WKT-13-05", "p"), ("AC-WKT-14-02", "p cap"),
            ("AC-WKT-14-03", "d p cap"), ("AC-WKT-14-04", "d p cap"), ("AC-WKT-14-05", "p cap"), ("AC-WKT-17-01", "p cap"),
            ("AC-WKT-17-02", "p"), ("AC-WKT-17-03", "p"), ("AC-WKT-17-05", "p cap"), ("AC-WKT-17-06", "p"),
            ("AC-WKT-17-07", "p"), ("AC-WKT-17-08", "d p cap"), ("AC-WKT-17-09", "p cap"), ("AC-WKT-17-10", "p cap"),
            ("AC-HIS-20-04", "d qa"),
        };

        // Los 40 de §5 (deben estar en It01Ids, It02Ids, It03Ids o It04aIds).
        public static readonly string[] FromEarlier =
        {
            "AC-WKT-17-04", "AC-APP-05-07", "AC-WKT-01-06", "AC-WKT-02-04", "AC-WKT-02-06", "AC-WKT-02-08", "AC-WKT-02-09", "AC-WKT-03-05",
            "AC-WKT-06-12", "AC-WKT-13-01", "AC-WKT-13-04", "AC-WKT-14-01", "AC-WKT-14-06", "AC-WKT-16-01", "AC-WKT-20-07", "AC-APP-03-09",
            "AC-APP-03-10", "AC-APP-03-11", "AC-APP-05-05", "AC-APP-05-08", "AC-BLE-16-01", "AC-BLE-16-02", "AC-BLE-16-03", "AC-WKT-13-02",
            "AC-WKT-13-03", "AC-WKT-13-05", "AC-WKT-14-02", "AC-WKT-14-03", "AC-WKT-14-04", "AC-WKT-14-05", "AC-WKT-17-01", "AC-WKT-17-02",
            "AC-WKT-17-03", "AC-WKT-17-05", "AC-WKT-17-06", "AC-WKT-17-07", "AC-WKT-17-08", "AC-WKT-17-09", "AC-WKT-17-10", "AC-HIS-20-04",
        };

        // El caso de la spec §6: archivo y línea del legacy, el nombre exacto y la verificación.
        public static readonly (string Id, string File, int Line, string Name, string Checks)[] LegacyCases =
        {
            ("LT-46", "test/step_pill_test.dart", 19, "pendiente, en curso, hecho y no cumplido se pintan distinto", "d p"),
        };

        // Sin verificación de dotnet (ni d ni s): solo PlayMode, Unity EditMode, captura o equipo (los 36 de 02-tech-design §6.4).
        // En run-tests.ps1 -Matrix figuran como PENDIENTE UNITY (no SIN TEST) mientras no se le pase -UnityResults.
        // TraceabilityTests controla que la lista sea exactamente la que sale de Acceptance.
        public static readonly string[] UnityOnlyIds =
        {
            "AC-WKT-14-17", "AC-WKT-14-19", "AC-WKT-19-05", "AC-WKT-16-02", "AC-WKT-21-02", "AC-WKT-21-10", "AC-WKT-21-11", "AC-WKT-21-12",
            "AC-WKT-13-07", "AC-WKT-13-09", "AC-WKT-13-10", "AC-APP-15-02", "AC-WKT-17-04", "AC-APP-05-07", "AC-WKT-13-01", "AC-WKT-13-04",
            "AC-WKT-14-06", "AC-WKT-16-01", "AC-WKT-20-07", "AC-APP-03-09", "AC-APP-03-10", "AC-APP-05-05", "AC-BLE-16-01", "AC-WKT-13-02",
            "AC-WKT-13-03", "AC-WKT-13-05", "AC-WKT-14-02", "AC-WKT-14-05", "AC-WKT-17-01", "AC-WKT-17-02", "AC-WKT-17-03", "AC-WKT-17-05",
            "AC-WKT-17-06", "AC-WKT-17-07", "AC-WKT-17-09", "AC-WKT-17-10",
        };

        // Su única verificación automática es de dotnet pero en un archivo de WP-B: ninguno en la It. 4b.
        public static readonly string[] WorkPackageBIds =
        {
        };
    }
}
