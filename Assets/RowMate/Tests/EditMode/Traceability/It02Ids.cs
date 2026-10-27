namespace RowMate.Tests.EditMode.Traceability
{
    // IDs de la spec de la It. 2 (02-dispositivo-simulador/01-po-spec): los 125 AC (113 nuevos + 12 de la It. 1 que se
    // verifican en pantalla o en equipo, §5) y los 15 casos legacy (§6), cada uno con su verificación de 02-tech-design §5.2:
    //   d = dotnet (y Unity EditMode) en archivos de WP-A · dB = dotnet en un archivo de WP-B (ItTwoToolingTests, AsmdefLintTests)
    //   s = dotnet con el simulador · u = Unity EditMode solo (Tests/UnityEditMode) · p = PlayMode (Tests/PlayMode)
    //   ps = PlayMode con el simulador (Tests/PlayModeSimulator) · cap = captura del arnés (Snapshots) · eq = equipo (QA)
    //   qa = revisión de QA · ci = CI de Unity
    // Literales explícitos: tools/dotnet/run-tests.ps1 -Matrix lee este archivo (todos los It0*Ids.cs) para listar los IDs y
    // los bloques UnityOnlyIds y WorkPackageBIds para marcar los que esperan a Unity o a WP-B (QA-G2-16).
    public static class It02Ids
    {
        public static readonly (string Id, string Checks)[] Acceptance =
        {
            // F-APP-01 (8)
            ("AC-APP-01-01", "d p eq"), ("AC-APP-01-02", "s u"), ("AC-APP-01-03", "d p ps eq"), ("AC-APP-01-04", "d s ps"),
            ("AC-APP-01-05", "ps"), ("AC-APP-01-06", "p u cap eq"), ("AC-APP-01-07", "u p eq"), ("AC-APP-01-08", "d s"),
            // F-APP-02 (8)
            ("AC-APP-02-01", "d p cap"), ("AC-APP-02-02", "d p cap"), ("AC-APP-02-03", "d p cap"), ("AC-APP-02-04", "p ps"),
            ("AC-APP-02-05", "d p"), ("AC-APP-02-06", "u p eq"), ("AC-APP-02-07", "d p ps eq"), ("AC-APP-02-08", "p cap"),
            // F-APP-05 (9)
            ("AC-APP-05-01", "p eq"), ("AC-APP-05-02", "p cap"), ("AC-APP-05-03", "d u cap"), ("AC-APP-05-04", "p"),
            ("AC-APP-05-05", "d p cap eq"), ("AC-APP-05-06", "p cap"), ("AC-APP-05-07", "p eq"), ("AC-APP-05-08", "d p eq"),
            ("AC-APP-05-09", "p eq"),
            // F-APP-06 (2), F-APP-08 (2), F-APP-19 (1)
            ("AC-APP-06-01", "d p cap"), ("AC-APP-06-02", "d"),
            ("AC-APP-08-01", "d qa"), ("AC-APP-08-02", "p cap qa"),
            ("AC-APP-19-05", "dB ci"),
            // F-BLE-05 (3), F-BLE-07 (4), F-BLE-08 (1), F-BLE-10 (2)
            ("AC-BLE-05-01", "d p cap"), ("AC-BLE-05-02", "d p cap"), ("AC-BLE-05-03", "ps"),
            ("AC-BLE-07-01", "d p cap"), ("AC-BLE-07-02", "s ps"), ("AC-BLE-07-03", "s ps"), ("AC-BLE-07-04", "s"),
            ("AC-BLE-08-01", "s ps cap"),
            ("AC-BLE-10-01", "p"), ("AC-BLE-10-02", "ps"),
            // F-BLE-11 (11)
            ("AC-BLE-11-01", "d p"), ("AC-BLE-11-02", "p cap"), ("AC-BLE-11-03", "d p cap"), ("AC-BLE-11-04", "d p cap"),
            ("AC-BLE-11-05", "d p cap"), ("AC-BLE-11-06", "d p cap"), ("AC-BLE-11-07", "p ps cap"), ("AC-BLE-11-08", "d p"),
            ("AC-BLE-11-09", "d ps cap"), ("AC-BLE-11-10", "s ps cap"), ("AC-BLE-11-11", "d p cap"),
            // F-BLE-13 (5)
            ("AC-BLE-13-01", "u dB qa"), ("AC-BLE-13-02", "p u dB cap qa eq"), ("AC-BLE-13-03", "s"), ("AC-BLE-13-04", "ps dB qa"),
            ("AC-BLE-13-05", "d p cap eq"),
            // F-BLE-15 (10)
            ("AC-BLE-15-01", "s"), ("AC-BLE-15-02", "s"), ("AC-BLE-15-03", "s"), ("AC-BLE-15-04", "s"), ("AC-BLE-15-05", "s"),
            ("AC-BLE-15-06", "s"), ("AC-BLE-15-07", "s"), ("AC-BLE-15-08", "s"), ("AC-BLE-15-09", "s"), ("AC-BLE-15-10", "ps cap"),
            // F-BLE-16 (12)
            ("AC-BLE-16-01", "ps cap"), ("AC-BLE-16-02", "ps cap"), ("AC-BLE-16-03", "s ps"), ("AC-BLE-16-04", "ps cap"),
            ("AC-BLE-16-05", "s ps"), ("AC-BLE-16-06", "s ps"), ("AC-BLE-16-07", "s ps"), ("AC-BLE-16-08", "s ps"),
            ("AC-BLE-16-09", "s ps"), ("AC-BLE-16-10", "s ps"), ("AC-BLE-16-11", "s ps eq"), ("AC-BLE-16-12", "ps cap"),
            // F-HRM-04 (5), F-HRM-12 (6)
            ("AC-HRM-04-01", "s"), ("AC-HRM-04-02", "d"), ("AC-HRM-04-03", "d ps"), ("AC-HRM-04-04", "s"), ("AC-HRM-04-05", "s ps"),
            ("AC-HRM-12-01", "s d"), ("AC-HRM-12-02", "s d"), ("AC-HRM-12-03", "s d"), ("AC-HRM-12-04", "s"), ("AC-HRM-12-05", "d"),
            ("AC-HRM-12-06", "d"),
            // F-HRM-13 (12)
            ("AC-HRM-13-01", "p ps"), ("AC-HRM-13-02", "d p cap"), ("AC-HRM-13-03", "d p cap"), ("AC-HRM-13-04", "d p ps cap"),
            ("AC-HRM-13-05", "d p cap"), ("AC-HRM-13-06", "p cap"), ("AC-HRM-13-07", "d p cap"), ("AC-HRM-13-08", "d ps cap"),
            ("AC-HRM-13-09", "ps"), ("AC-HRM-13-10", "ps cap"), ("AC-HRM-13-11", "d p"), ("AC-HRM-13-12", "d ps cap"),
            // F-HRM-15 (12)
            ("AC-HRM-15-01", "s ps"), ("AC-HRM-15-02", "s"), ("AC-HRM-15-03", "s"), ("AC-HRM-15-04", "s"), ("AC-HRM-15-05", "s"),
            ("AC-HRM-15-06", "s"), ("AC-HRM-15-07", "s ps"), ("AC-HRM-15-08", "s ps cap"), ("AC-HRM-15-09", "s"),
            ("AC-HRM-15-10", "s"), ("AC-HRM-15-11", "s"), ("AC-HRM-15-12", "s ps"),
            // Criterios de la It. 1 que se verifican en la It. 2 (12, §5): ya tienen tests de dotnet de la It. 1 (d); acá se suma
            // lo que faltaba ver en Unity, en pantalla o en equipo.
            ("AC-BLE-01-05", "d p cap"), ("AC-APP-09-04", "d ps eq"), ("AC-APP-09-05", "d ps eq"), ("AC-APP-09-06", "d u eq"),
            ("AC-APP-16-01", "d u eq"), ("AC-APP-16-02", "d u eq"), ("AC-APP-16-03", "d u eq"), ("AC-APP-16-04", "d u eq"),
            ("AC-BASE-01", "d eq"), ("AC-BASE-05", "d cap"), ("AC-BASE-08", "d u"), ("AC-BASE-10", "d u"),
        };

        // Los 12 de la It. 1 (deben estar también en It01Ids).
        public static readonly string[] FromIt01 =
        {
            "AC-BLE-01-05", "AC-APP-09-04", "AC-APP-09-05", "AC-APP-09-06", "AC-APP-16-01", "AC-APP-16-02", "AC-APP-16-03",
            "AC-APP-16-04", "AC-BASE-01", "AC-BASE-05", "AC-BASE-08", "AC-BASE-10",
        };

        // 01-po-spec §6: id, archivo del legacy, línea de la declaración test( / testWidgets(, nombre exacto y verificación.
        public static readonly (string Id, string File, int Line, string Name, string Checks)[] LegacyCases =
        {
            ("LT-20", "test/simulated_ble_service_test.dart", 13, "emite connected y adapter on al primer listener, y datos cada segundo", "s"),
            ("LT-21", "test/simulated_ble_service_test.dart", 39, "simulateDisconnect emite disconnected y reconecta a los 3 s", "s"),
            ("LT-22", "test/simulated_ble_service_test.dart", 64, "disconnect no reconecta solo; startScan reconecta", "s"),
            ("LT-23", "test/simulated_heart_rate_service_test.dart", 10, "startScan emite el sensor sintético y connect emite bpm cada segundo", "s"),
            ("LT-24", "test/simulated_heart_rate_service_test.dart", 49, "el escaneo expira a disconnected si no se conecta", "s"),
            ("LT-25", "test/simulated_heart_rate_service_test.dart", 62, "simulateDisconnect cae y reconecta a los 3 s; disconnect no reconecta", "s"),
            ("LT-26", "test/simulator_overlay_test.dart", 17, "el panel se abre fuera del Navigator y + sube los watts", "ps"),
            ("LT-27", "test/simulator_overlay_test.dart", 90, "en una ventana baja el botón de cerrar sigue accesible", "ps"),
            ("LT-28", "test/heart_rate_card_test.dart", 83, "sin sensor → buscar → conectar → bpm → desconectar", "ps"),
            ("LT-29", "test/heart_rate_card_test.dart", 121, "los escaneos del remo y del pulsómetro se excluyen", "ps"),
            ("LT-30", "test/heart_rate_card_test.dart", 142, "en landscape la vista de escaneo no desborda", "ps"),
            ("LT-31", "test/heart_rate_card_test.dart", 161, "mientras el remo escanea no se puede buscar pulsómetro", "ps"),
            ("LT-32", "test/heart_rate_provider_test.dart", 54, "refleja escaneo, conexión, bpm y desconexión del servicio", "s"),
            ("LT-33", "test/heart_rate_provider_test.dart", 107, "una excepción del servicio queda en error y se limpia al reintentar", "s"),
            ("LT-34", "test/heart_rate_provider_test.dart", 122, "isIncompatible distingue el sensor incompatible", "s"),
        };

        // Sin verificación de dotnet (ni d ni s): solo PlayMode, Unity EditMode, captura o equipo. En run-tests.ps1 -Matrix
        // figuran como PENDIENTE UNITY (no SIN TEST) mientras no se le pase -UnityResults. TraceabilityTests controla que la
        // lista sea exactamente la que sale de Acceptance y LegacyCases.
        public static readonly string[] UnityOnlyIds =
        {
            "AC-APP-01-05", "AC-APP-01-06", "AC-APP-01-07", "AC-APP-02-04", "AC-APP-02-06", "AC-APP-02-08", "AC-APP-05-01",
            "AC-APP-05-02", "AC-APP-05-04", "AC-APP-05-06", "AC-APP-05-07", "AC-APP-05-09", "AC-APP-08-02",
            "AC-BLE-05-03", "AC-BLE-10-01", "AC-BLE-10-02", "AC-BLE-11-02", "AC-BLE-11-07", "AC-BLE-13-01", "AC-BLE-13-02",
            "AC-BLE-13-04", "AC-BLE-15-10", "AC-BLE-16-01", "AC-BLE-16-02", "AC-BLE-16-04", "AC-BLE-16-12",
            "AC-HRM-13-01", "AC-HRM-13-06", "AC-HRM-13-09", "AC-HRM-13-10",
            "LT-26", "LT-27", "LT-28", "LT-29", "LT-30", "LT-31",
        };

        // Su única verificación automática es de dotnet pero en un archivo de WP-B (ItTwoToolingTests): PENDIENTE WP-B.
        public static readonly string[] WorkPackageBIds =
        {
            "AC-APP-19-05",
        };
    }
}
