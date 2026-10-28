namespace RowMate.Tests.EditMode.Traceability
{
    // IDs de la spec de la It. 3 (03-rutinas/01-po-spec): los 102 AC (91 nuevos + 11 de iteraciones anteriores que se
    // verifican, se reabren o cambian acá, §5; AC-BLE-16-02 cambia por DV-13), cada uno con su verificación de
    // 02-tech-design §6.3:
    //   d = dotnet (y Unity EditMode) en archivos de WP-A · dB = dotnet en un archivo de WP-B (ItThreeToolingTests)
    //   s = dotnet con el simulador · u = Unity EditMode solo (Tests/UnityEditMode) · p = PlayMode (Tests/PlayMode)
    //   ps = PlayMode con el simulador (Tests/PlayModeSimulator) · cap = captura del arnés (Snapshots) · eq = equipo (QA)
    //   qa = revisión de QA
    // El área no tiene casos de test legacy (spec §6). Literales explícitos: tools/dotnet/run-tests.ps1 -Matrix lee este
    // archivo (todos los It0*Ids.cs) y sus bloques UnityOnlyIds y WorkPackageBIds.
    public static class It03Ids
    {
        public static readonly (string Id, string Checks)[] Acceptance =
        {
            // F-RUT-08 + F-HIS-18 (9)
            ("AC-RUT-08-01", "d"), ("AC-RUT-08-02", "d"), ("AC-RUT-08-03", "d"), ("AC-RUT-08-04", "d"), ("AC-RUT-08-05", "d"),
            ("AC-RUT-08-06", "d p"), ("AC-RUT-08-07", "d eq"), ("AC-RUT-08-08", "d p"), ("AC-RUT-08-09", "p cap eq"),
            // F-RUT-09 (6)
            ("AC-RUT-09-01", "d p"), ("AC-RUT-09-02", "d p"), ("AC-RUT-09-03", "d p"), ("AC-RUT-09-04", "d p cap"), ("AC-RUT-09-05", "d p cap"),
            ("AC-RUT-09-06", "d p"),
            // F-RUT-10 (11)
            ("AC-RUT-10-01", "p cap"), ("AC-RUT-10-02", "p cap"), ("AC-RUT-10-03", "d p cap"), ("AC-RUT-10-04", "p ps cap"), ("AC-RUT-10-05", "p"),
            ("AC-RUT-10-06", "p cap"), ("AC-RUT-10-07", "p cap"), ("AC-RUT-10-08", "qa p"), ("AC-RUT-10-09", "d p ps cap"),
            ("AC-RUT-10-10", "d p cap"), ("AC-RUT-10-11", "d p cap"),
            // F-RUT-11 (10)
            ("AC-RUT-11-01", "p cap"), ("AC-RUT-11-02", "p cap"), ("AC-RUT-11-03", "d p"), ("AC-RUT-11-04", "p cap"), ("AC-RUT-11-05", "d p cap"),
            ("AC-RUT-11-06", "d p"), ("AC-RUT-11-07", "d p"), ("AC-RUT-11-08", "p eq"), ("AC-RUT-11-09", "p eq cap"), ("AC-RUT-11-10", "p eq"),
            // F-RUT-12 (6)
            ("AC-RUT-12-01", "d p"), ("AC-RUT-12-02", "d p cap"), ("AC-RUT-12-03", "d p"), ("AC-RUT-12-04", "d p"), ("AC-RUT-12-05", "qa p"),
            ("AC-RUT-12-06", "d p cap"),
            // F-RUT-13 (16)
            ("AC-RUT-13-01", "d p cap"), ("AC-RUT-13-02", "p cap"), ("AC-RUT-13-03", "d p"), ("AC-RUT-13-04", "d p"), ("AC-RUT-13-05", "d"),
            ("AC-RUT-13-06", "d p"), ("AC-RUT-13-07", "d p"), ("AC-RUT-13-08", "d"), ("AC-RUT-13-09", "d"), ("AC-RUT-13-10", "d p cap"),
            ("AC-RUT-13-11", "d p cap"), ("AC-RUT-13-12", "d p cap"), ("AC-RUT-13-13", "p eq"), ("AC-RUT-13-14", "p"), ("AC-RUT-13-15", "p cap"),
            ("AC-RUT-13-16", "d p cap"),
            // F-RUT-14 (12)
            ("AC-RUT-14-01", "d p cap"), ("AC-RUT-14-02", "p cap"), ("AC-RUT-14-03", "d p cap"), ("AC-RUT-14-04", "p cap"), ("AC-RUT-14-05", "d p"),
            ("AC-RUT-14-06", "d p cap"), ("AC-RUT-14-07", "d p"), ("AC-RUT-14-08", "d p"), ("AC-RUT-14-09", "d p"), ("AC-RUT-14-10", "d p"),
            ("AC-RUT-14-11", "d p cap"), ("AC-RUT-14-12", "qa p"),
            // F-HIS-17 (6), F-HIS-20 (4)
            ("AC-HIS-17-01", "u qa eq"), ("AC-HIS-17-02", "d qa"), ("AC-HIS-17-03", "d qa eq"), ("AC-HIS-17-04", "d p"), ("AC-HIS-17-05", "d p"),
            ("AC-HIS-17-06", "d p eq"),
            ("AC-HIS-20-01", "d dB qa eq"), ("AC-HIS-20-02", "p eq"), ("AC-HIS-20-03", "ps cap"), ("AC-HIS-20-04", "d dB p qa"),
            // F-APP-03 (8), F-APP-01 (1), F-APP-05 (2)
            ("AC-APP-03-01", "p cap"), ("AC-APP-03-02", "p eq"), ("AC-APP-03-03", "p cap"), ("AC-APP-03-04", "p cap"), ("AC-APP-03-05", "d p ps cap"),
            ("AC-APP-03-06", "p"), ("AC-APP-03-07", "ps cap"), ("AC-APP-03-08", "p cap"),
            ("AC-APP-01-09", "d p"),
            ("AC-APP-05-10", "u eq cap"), ("AC-APP-05-11", "p eq cap"),
            // Criterios de iteraciones anteriores que se verifican acá (11, §5).
            ("AC-BLE-11-02", "p ps cap"), ("AC-BLE-16-01", "ps cap"), ("AC-BLE-16-02", "d s ps cap"), ("AC-RUT-01-04", "p cap"),
            ("AC-RUT-01-05", "d p"), ("AC-RUT-04-06", "d p cap"), ("AC-APP-09-07", "p eq"), ("AC-APP-02-08", "p cap"), ("AC-APP-05-05", "p cap"),
            ("AC-APP-05-07", "p eq"), ("AC-APP-05-08", "p"),
        };

        // Los 11 de §5 (deben estar en It01Ids o en It02Ids).
        public static readonly string[] FromEarlier =
        {
            "AC-BLE-11-02", "AC-BLE-16-01", "AC-BLE-16-02", "AC-RUT-01-04", "AC-RUT-01-05", "AC-RUT-04-06", "AC-APP-09-07", "AC-APP-02-08",
            "AC-APP-05-05", "AC-APP-05-07", "AC-APP-05-08",
        };

        // Sin verificación de dotnet (ni d ni s): solo PlayMode, Unity EditMode, captura o equipo. En run-tests.ps1 -Matrix
        // figuran como PENDIENTE UNITY (no SIN TEST) mientras no se le pase -UnityResults. TraceabilityTests controla que la
        // lista sea exactamente la que sale de Acceptance.
        public static readonly string[] UnityOnlyIds =
        {
            "AC-RUT-08-09", "AC-RUT-10-01", "AC-RUT-10-02", "AC-RUT-10-04", "AC-RUT-10-05", "AC-RUT-10-06", "AC-RUT-10-07", "AC-RUT-10-08",
            "AC-RUT-11-01", "AC-RUT-11-02", "AC-RUT-11-04", "AC-RUT-11-08", "AC-RUT-11-09", "AC-RUT-11-10", "AC-RUT-12-05", "AC-RUT-13-02",
            "AC-RUT-13-13", "AC-RUT-13-14", "AC-RUT-13-15", "AC-RUT-14-02", "AC-RUT-14-04", "AC-RUT-14-12", "AC-HIS-17-01", "AC-HIS-20-02",
            "AC-HIS-20-03", "AC-APP-03-01", "AC-APP-03-02", "AC-APP-03-03", "AC-APP-03-04", "AC-APP-03-06", "AC-APP-03-07", "AC-APP-03-08",
            "AC-APP-05-10", "AC-APP-05-11", "AC-BLE-11-02", "AC-BLE-16-01", "AC-RUT-01-04", "AC-APP-09-07", "AC-APP-02-08", "AC-APP-05-05",
            "AC-APP-05-07", "AC-APP-05-08",
        };

        // Su única verificación automática es de dotnet pero en un archivo de WP-B: ninguno en la It. 3 (los dB de AC-HIS-20-01
        // y -04 tienen además su test de WP-A).
        public static readonly string[] WorkPackageBIds =
        {
        };
    }
}
