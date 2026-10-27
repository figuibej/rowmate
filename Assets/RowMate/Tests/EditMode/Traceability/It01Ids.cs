namespace RowMate.Tests.EditMode.Traceability
{
    // IDs de la spec de la It. 1 (01-po-spec): los 139 AC con los rangos expandidos y los 19 casos legacy (§5).
    // Literales explícitos: tools/dotnet/run-tests.ps1 -Matrix lee este archivo para listar los AC sin test.
    public static class It01Ids
    {
        public static readonly string[] AcceptanceIds =
        {
            // F-BLE-01 (6)
            "AC-BLE-01-01", "AC-BLE-01-02", "AC-BLE-01-03", "AC-BLE-01-04", "AC-BLE-01-05", "AC-BLE-01-06",
            // F-BLE-02 (16)
            "AC-BLE-02-01", "AC-BLE-02-02", "AC-BLE-02-03", "AC-BLE-02-04", "AC-BLE-02-05", "AC-BLE-02-06", "AC-BLE-02-07", "AC-BLE-02-08",
            "AC-BLE-02-09", "AC-BLE-02-10", "AC-BLE-02-11", "AC-BLE-02-12", "AC-BLE-02-13", "AC-BLE-02-14", "AC-BLE-02-15", "AC-BLE-02-16",
            // F-BLE-03 (5)
            "AC-BLE-03-01", "AC-BLE-03-02", "AC-BLE-03-03", "AC-BLE-03-04", "AC-BLE-03-05",
            // F-BLE-14 (10)
            "AC-BLE-14-01", "AC-BLE-14-02", "AC-BLE-14-03", "AC-BLE-14-04", "AC-BLE-14-05", "AC-BLE-14-06", "AC-BLE-14-07", "AC-BLE-14-08",
            "AC-BLE-14-09", "AC-BLE-14-10",
            // F-HRM-01 (9)
            "AC-HRM-01-01", "AC-HRM-01-02", "AC-HRM-01-03", "AC-HRM-01-04", "AC-HRM-01-05", "AC-HRM-01-06", "AC-HRM-01-07", "AC-HRM-01-08",
            "AC-HRM-01-09",
            // F-RUT-01 (5)
            "AC-RUT-01-01", "AC-RUT-01-02", "AC-RUT-01-03", "AC-RUT-01-04", "AC-RUT-01-05",
            // F-RUT-02 (4)
            "AC-RUT-02-01", "AC-RUT-02-02", "AC-RUT-02-03", "AC-RUT-02-04",
            // F-RUT-03 (5)
            "AC-RUT-03-01", "AC-RUT-03-02", "AC-RUT-03-03", "AC-RUT-03-04", "AC-RUT-03-05",
            // F-RUT-04 (6)
            "AC-RUT-04-01", "AC-RUT-04-02", "AC-RUT-04-03", "AC-RUT-04-04", "AC-RUT-04-05", "AC-RUT-04-06",
            // F-RUT-05 (4)
            "AC-RUT-05-01", "AC-RUT-05-02", "AC-RUT-05-03", "AC-RUT-05-04",
            // F-RUT-06 (13)
            "AC-RUT-06-01", "AC-RUT-06-02", "AC-RUT-06-03", "AC-RUT-06-04", "AC-RUT-06-05", "AC-RUT-06-06", "AC-RUT-06-07", "AC-RUT-06-08",
            "AC-RUT-06-09", "AC-RUT-06-10", "AC-RUT-06-11", "AC-RUT-06-12", "AC-RUT-06-13",
            // F-RUT-07 (5)
            "AC-RUT-07-01", "AC-RUT-07-02", "AC-RUT-07-03", "AC-RUT-07-04", "AC-RUT-07-05",
            // F-RUT-16 (5)
            "AC-RUT-16-01", "AC-RUT-16-02", "AC-RUT-16-03", "AC-RUT-16-04", "AC-RUT-16-05",
            // F-RUT-17 (2)
            "AC-RUT-17-01", "AC-RUT-17-02",
            // F-RUT-18 (9)
            "AC-RUT-18-01", "AC-RUT-18-02", "AC-RUT-18-03", "AC-RUT-18-04", "AC-RUT-18-05", "AC-RUT-18-06", "AC-RUT-18-07", "AC-RUT-18-08",
            "AC-RUT-18-09",
            // F-APP-09 (7)
            "AC-APP-09-01", "AC-APP-09-02", "AC-APP-09-03", "AC-APP-09-04", "AC-APP-09-05", "AC-APP-09-06", "AC-APP-09-07",
            // F-APP-10 (9)
            "AC-APP-10-01", "AC-APP-10-02", "AC-APP-10-03", "AC-APP-10-04", "AC-APP-10-05", "AC-APP-10-06", "AC-APP-10-07", "AC-APP-10-08",
            "AC-APP-10-09",
            // F-APP-16 (5)
            "AC-APP-16-01", "AC-APP-16-02", "AC-APP-16-03", "AC-APP-16-04", "AC-APP-16-05",
            // F-APP-19 (4)
            "AC-APP-19-01", "AC-APP-19-02", "AC-APP-19-03", "AC-APP-19-04",
            // Piezas base (10)
            "AC-BASE-01", "AC-BASE-02", "AC-BASE-03", "AC-BASE-04", "AC-BASE-05", "AC-BASE-06", "AC-BASE-07", "AC-BASE-08", "AC-BASE-09",
            "AC-BASE-10",
        };

        // Los AC que cubre WP-A (02-tech-design §8): los suyos y su parte de AC-BLE-01-02/-03 y AC-RUT-16-04.
        public static readonly string[] WorkPackageAIds =
        {
            "AC-BASE-01", "AC-BASE-02", "AC-BASE-03", "AC-BASE-04", "AC-BASE-05", "AC-BASE-06", "AC-BASE-07", "AC-BASE-08", "AC-BASE-09",
            "AC-BASE-10",
            "AC-APP-16-01", "AC-APP-16-02", "AC-APP-16-03", "AC-APP-16-04", "AC-APP-16-05",
            "AC-APP-19-01", "AC-APP-19-02", "AC-APP-19-03", "AC-APP-19-04",
            "AC-BLE-03-04", "AC-BLE-01-02", "AC-BLE-01-03", "AC-RUT-16-04",
        };

        // 01-po-spec §5: id, archivo del legacy, línea de la declaración test( y nombre exacto (TestName en NUnit).
        public static readonly (string Id, string File, int Line, string Name)[] LegacyCases =
        {
            ("LT-01", "test/rowing_simulator_test.dart", 7, "más watts → pace más rápido"),
            ("LT-02", "test/rowing_simulator_test.dart", 14, "203 W ≈ 2:00 /500m (fórmula Concept2)"),
            ("LT-03", "test/rowing_simulator_test.dart", 18, "acumula distancia, remadas y tiempo mientras rema"),
            ("LT-04", "test/rowing_simulator_test.dart", 36, "sin remar: 0 spm/watts/pace y totales congelados"),
            ("LT-05", "test/rowing_simulator_test.dart", 54, "los objetivos se limitan a su rango"),
            ("LT-06", "test/rowing_simulator_test.dart", 62, "con ruido, los watts quedan dentro de ±5 %"),
            ("LT-07", "test/rowing_simulator_test.dart", 70, "heartRate expone el último pulso calculado por tick()"),
            ("LT-08", "test/scan_filter_test.dart", 24, "con filtro en el cliente solo pasan los que anuncian el servicio"),
            ("LT-09", "test/scan_filter_test.dart", 32, "sin filtro en el cliente (el sistema ya filtró) pasa todo"),
            ("LT-10", "test/heart_rate_parser_test.dart", 5, "bpm en uint8 sin información de contacto"),
            ("LT-11", "test/heart_rate_parser_test.dart", 11, "bpm en uint16 little-endian"),
            ("LT-12", "test/heart_rate_parser_test.dart", 16, "bits de contacto: 01 = no soportado, 10 = sin contacto, 11 = con contacto"),
            ("LT-13", "test/heart_rate_parser_test.dart", 23, "ignora Energy Expended y RR intervals"),
            ("LT-14", "test/heart_rate_parser_test.dart", 29, "bytes vacíos o truncados devuelven null"),
            ("LT-15", "test/heart_rate_parser_test.dart", 35, "UUIDs del perfil"),
            ("LT-16", "test/routine_positions_test.dart", 6, "las posiciones quedan alineadas con flattenedSteps"),
            ("LT-17", "test/session_stats_test.dart", 7, "avgHeartRate y maxHeartRate ignoran los ceros"),
            ("LT-18", "test/session_stats_test.dart", 14, "sin pulso quedan en 0"),
            ("LT-19", "test/session_stats_test.dart", 21, "sin puntos quedan en 0"),
        };

        // Tests de la demo (01-po-spec §1 punto 2, 02-tech-design §8.7): clase y método con categoría Demo.
        public static readonly (string Class, string Method)[] DemoTests =
        {
            ("FtmsParserTests", "ReferencePackets"),
            ("RowingSimulatorTests", "RowingWithoutNoise"),
            ("RoutineSummaryTests", "SummaryTable"),
            ("SessionStatsTests", "P99Examples"),
        };
    }
}
