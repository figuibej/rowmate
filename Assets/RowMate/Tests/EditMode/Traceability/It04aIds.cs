namespace RowMate.Tests.EditMode.Traceability
{
    // IDs de la spec de la It. 4a (04a-motor-hud-funcional/01-po-spec, corregida por su gate QA): los 162 AC (147 nuevos,
    // §4.1 a §4.15, y 15 de iteraciones anteriores que se verifican o cambian acá, §5) y los 11 casos legacy (§6), cada uno con
    // su verificación de 02-tech-design §8.4:
    //   d = dotnet (y Unity EditMode) en archivos de WP-A · dB = dotnet en un archivo de WP-B (ItFourAToolingTests, ItFourAFontTests)
    //   s = dotnet con el simulador · u = Unity EditMode solo (Tests/UnityEditMode) · p = PlayMode (Tests/PlayMode)
    //   ps = PlayMode con el simulador (Tests/PlayModeSimulator) · cap = captura del arnés (Snapshots) · eq = equipo (QA)
    //   qa = revisión de QA
    // Ningún ID queda solo con eq o qa (QA-G4aT-09): las partes de equipo sin módulo ni teléfono tienen su [Ignore] en
    // WorkoutDeviceChecksTests. Literales explícitos: tools/dotnet/run-tests.ps1 -Matrix lee este archivo (todos los
    // It0*Ids.cs) y sus bloques UnityOnlyIds y WorkPackageBIds.
    public static class It04aIds
    {
        public static readonly (string Id, string Checks)[] Acceptance =
        {
            // F-WKT-01 (11)
            ("AC-WKT-01-01", "d"), ("AC-WKT-01-02", "d p"), ("AC-WKT-01-03", "d"), ("AC-WKT-01-04", "d"), ("AC-WKT-01-05", "d"),
            ("AC-WKT-01-06", "d p"), ("AC-WKT-01-07", "d"), ("AC-WKT-01-08", "d"), ("AC-WKT-01-09", "p"), ("AC-WKT-01-10", "d p"),
            ("AC-WKT-01-11", "d p cap"),
            // F-WKT-02 (10)
            ("AC-WKT-02-01", "d"), ("AC-WKT-02-02", "d"), ("AC-WKT-02-03", "d p"), ("AC-WKT-02-04", "d"), ("AC-WKT-02-05", "d"),
            ("AC-WKT-02-06", "d p"), ("AC-WKT-02-07", "d"), ("AC-WKT-02-08", "d p eq"), ("AC-WKT-02-09", "d eq"), ("AC-WKT-02-10", "d eq"),
            // F-WKT-03 (10)
            ("AC-WKT-03-01", "d"), ("AC-WKT-03-02", "d"), ("AC-WKT-03-03", "d"), ("AC-WKT-03-04", "d"), ("AC-WKT-03-05", "d p cap"),
            ("AC-WKT-03-06", "d"), ("AC-WKT-03-07", "d p"), ("AC-WKT-03-08", "d p cap"), ("AC-WKT-03-09", "d"), ("AC-WKT-03-10", "d ps"),
            // F-WKT-04 (7)
            ("AC-WKT-04-01", "d"), ("AC-WKT-04-02", "d"), ("AC-WKT-04-03", "d"), ("AC-WKT-04-04", "d"), ("AC-WKT-04-05", "d"),
            ("AC-WKT-04-06", "d p cap"), ("AC-WKT-04-07", "d"),
            // F-WKT-05 (6)
            ("AC-WKT-05-01", "d"), ("AC-WKT-05-02", "d"), ("AC-WKT-05-03", "d"), ("AC-WKT-05-04", "d"), ("AC-WKT-05-05", "d"),
            ("AC-WKT-05-06", "ps cap"),
            // F-WKT-06 (15)
            ("AC-WKT-06-01", "d"), ("AC-WKT-06-02", "d"), ("AC-WKT-06-03", "d"), ("AC-WKT-06-04", "d"), ("AC-WKT-06-05", "d eq"),
            ("AC-WKT-06-06", "d"), ("AC-WKT-06-07", "d"), ("AC-WKT-06-08", "d"), ("AC-WKT-06-09", "d"), ("AC-WKT-06-10", "d eq qa"),
            ("AC-WKT-06-11", "d"), ("AC-WKT-06-12", "d p cap"), ("AC-WKT-06-13", "d p"), ("AC-WKT-06-14", "d qa"), ("AC-WKT-06-15", "d"),
            // F-WKT-09 (7)
            ("AC-WKT-09-01", "d"), ("AC-WKT-09-02", "d"), ("AC-WKT-09-03", "d"), ("AC-WKT-09-04", "d"), ("AC-WKT-09-05", "d"),
            ("AC-WKT-09-06", "d"), ("AC-WKT-09-07", "d"),
            // F-WKT-10 (9)
            ("AC-WKT-10-01", "d p"), ("AC-WKT-10-02", "d"), ("AC-WKT-10-03", "d"), ("AC-WKT-10-04", "d"), ("AC-WKT-10-05", "p cap"),
            ("AC-WKT-10-06", "d cap"), ("AC-WKT-10-07", "p"), ("AC-WKT-10-08", "p"), ("AC-WKT-10-09", "ps cap"),
            // F-WKT-11 (16)
            ("AC-WKT-11-01", "d"), ("AC-WKT-11-02", "d"), ("AC-WKT-11-03", "d"), ("AC-WKT-11-04", "d"), ("AC-WKT-11-05", "d"),
            ("AC-WKT-11-06", "d"), ("AC-WKT-11-07", "d"), ("AC-WKT-11-08", "d"), ("AC-WKT-11-09", "d"), ("AC-WKT-11-10", "d"),
            ("AC-WKT-11-11", "d"), ("AC-WKT-11-12", "d"), ("AC-WKT-11-13", "d"), ("AC-WKT-11-14", "d"), ("AC-WKT-11-15", "d"),
            ("AC-WKT-11-16", "d"),
            // F-WKT-12 (9)
            ("AC-WKT-12-01", "p cap"), ("AC-WKT-12-02", "p"), ("AC-WKT-12-03", "d p cap"), ("AC-WKT-12-04", "p cap"), ("AC-WKT-12-05", "d p"),
            ("AC-WKT-12-06", "p cap"), ("AC-WKT-12-07", "d p cap"), ("AC-WKT-12-08", "d p cap"), ("AC-WKT-12-09", "p cap"),
            // F-WKT-20 (7), F-WKT-23 (1)
            ("AC-WKT-20-01", "p cap"), ("AC-WKT-20-02", "p"), ("AC-WKT-20-03", "p cap"), ("AC-WKT-20-04", "p"), ("AC-WKT-20-05", "p"),
            ("AC-WKT-20-06", "p"), ("AC-WKT-20-07", "p"),
            ("AC-WKT-23-01", "d qa"),
            // F-BLE-10 (5), F-APP-14 (4)
            ("AC-BLE-10-03", "d p cap"), ("AC-BLE-10-04", "d p cap"), ("AC-BLE-10-05", "ps cap"), ("AC-BLE-10-06", "d p"), ("AC-BLE-10-07", "d"),
            ("AC-APP-14-01", "u eq"), ("AC-APP-14-02", "d eq"), ("AC-APP-14-03", "u eq"), ("AC-APP-14-04", "d qa"),
            // F-WKT-13 (5), F-WKT-14 (6), F-WKT-16 (1), F-WKT-17 (10)
            ("AC-WKT-13-01", "p"), ("AC-WKT-13-02", "p cap"), ("AC-WKT-13-03", "p eq"), ("AC-WKT-13-04", "p"), ("AC-WKT-13-05", "p"),
            ("AC-WKT-14-01", "p cap"), ("AC-WKT-14-02", "p cap"), ("AC-WKT-14-03", "p cap"), ("AC-WKT-14-04", "p cap"), ("AC-WKT-14-05", "p cap"),
            ("AC-WKT-14-06", "p cap"),
            ("AC-WKT-16-01", "p cap"),
            ("AC-WKT-17-01", "d p cap"), ("AC-WKT-17-02", "p"), ("AC-WKT-17-03", "p"), ("AC-WKT-17-04", "p"), ("AC-WKT-17-05", "p cap"),
            ("AC-WKT-17-06", "p"), ("AC-WKT-17-07", "p"), ("AC-WKT-17-08", "d p cap"), ("AC-WKT-17-09", "d p cap"), ("AC-WKT-17-10", "p"),
            // F-HIS-17 (1), F-HIS-19 (2), F-SCN-01 (1), F-APP-03 (3), F-APP-10 (1)
            ("AC-HIS-17-07", "d qa"), ("AC-HIS-19-01", "d"), ("AC-HIS-19-02", "d"), ("AC-SCN-01-01", "p cap"),
            ("AC-APP-03-09", "p cap"), ("AC-APP-03-10", "p cap"), ("AC-APP-03-11", "p cap"), ("AC-APP-10-10", "d"),
            // Criterios de iteraciones anteriores que se verifican o cambian acá (15, §5; D-ART-10 en AC-BLE-16-02 y -03,
            // AC-RUT-10-04 y -09).
            ("AC-BLE-16-01", "ps cap"), ("AC-BLE-16-02", "d s ps cap"), ("AC-BLE-16-03", "s ps cap"), ("AC-APP-02-08", "p cap"),
            ("AC-APP-06-01", "p cap"), ("AC-APP-05-05", "p cap"), ("AC-APP-05-07", "p ps eq"), ("AC-APP-05-08", "p"), ("AC-RUT-06-08", "d p"),
            ("AC-RUT-10-04", "ps cap"), ("AC-RUT-10-09", "ps cap"), ("AC-RUT-10-11", "p cap"), ("AC-HIS-17-02", "d qa"), ("AC-HIS-20-04", "d qa"),
            ("AC-APP-01-09", "p"),
        };

        // Los 15 de §5 (deben estar en It01Ids, It02Ids o It03Ids).
        public static readonly string[] FromEarlier =
        {
            "AC-BLE-16-01", "AC-BLE-16-02", "AC-BLE-16-03", "AC-APP-02-08", "AC-APP-06-01", "AC-APP-05-05", "AC-APP-05-07", "AC-APP-05-08",
            "AC-RUT-06-08", "AC-RUT-10-04", "AC-RUT-10-09", "AC-RUT-10-11", "AC-HIS-17-02", "AC-HIS-20-04", "AC-APP-01-09",
        };

        // Los 11 de la spec §6 (ROADMAP §5): archivo y línea del legacy, el nombre exacto y la verificación.
        public static readonly (string Id, string File, int Line, string Name, string Checks)[] LegacyCases =
        {
            ("LT-35", "test/workout_provider_hr_test.dart", 80, "el pulsómetro manda sobre el FTMS y se actualiza sin paquetes del remo", "d"),
            ("LT-36", "test/workout_provider_hr_test.dart", 125, "orden real de caída: bpm 0 y después disconnected", "d"),
            ("LT-37", "test/workout_provider_hr_test.dart", 148, "un bpm recibido durante connecting se conserva al pasar a connected", "d"),
            ("LT-38", "test/workout_provider_hr_test.dart", 167, "connected del sensor sin bpm previo no toca los datos", "d"),
            ("LT-39", "test/step_target_tracker_test.dart", 20, "cumplido cuando el promedio de vatios alcanza el mínimo", "d"),
            ("LT-40", "test/step_target_tracker_test.dart", 28, "no cumplido si falla cualquiera de los objetivos definidos", "d"),
            ("LT-41", "test/step_target_tracker_test.dart", 35, "el split se cumple por debajo del objetivo (más rápido)", "d"),
            ("LT-42", "test/step_target_tracker_test.dart", 46, "un paso sin objetivos no tiene resultado", "d"),
            ("LT-43", "test/step_target_tracker_test.dart", 53, "sin lecturas remando el objetivo no se cumple; los ceros no cuentan", "d"),
            ("LT-44", "test/step_target_tracker_test.dart", 65, "un paso no completado no tiene resultado y reset borra todo", "d"),
            ("LT-45", "test/series_panels_test.dart", 32, "formatos", "d"),
        };

        // Sin verificación de dotnet (ni d ni s): solo PlayMode, Unity EditMode, captura o equipo (los 54 de 02-tech-design §8.5).
        // En run-tests.ps1 -Matrix figuran como PENDIENTE UNITY (no SIN TEST) mientras no se le pase -UnityResults.
        // TraceabilityTests controla que la lista sea exactamente la que sale de Acceptance.
        public static readonly string[] UnityOnlyIds =
        {
            "AC-WKT-01-09", "AC-WKT-05-06", "AC-WKT-10-05", "AC-WKT-10-07", "AC-WKT-10-08", "AC-WKT-10-09", "AC-WKT-12-01", "AC-WKT-12-02",
            "AC-WKT-12-04", "AC-WKT-12-06", "AC-WKT-12-09", "AC-WKT-20-01", "AC-WKT-20-02", "AC-WKT-20-03", "AC-WKT-20-04", "AC-WKT-20-05",
            "AC-WKT-20-06", "AC-WKT-20-07", "AC-BLE-10-05", "AC-APP-14-01", "AC-APP-14-03", "AC-WKT-13-01", "AC-WKT-13-02", "AC-WKT-13-03",
            "AC-WKT-13-04", "AC-WKT-13-05", "AC-WKT-14-01", "AC-WKT-14-02", "AC-WKT-14-03", "AC-WKT-14-04", "AC-WKT-14-05", "AC-WKT-14-06",
            "AC-WKT-16-01", "AC-WKT-17-02", "AC-WKT-17-03", "AC-WKT-17-04", "AC-WKT-17-05", "AC-WKT-17-06", "AC-WKT-17-07", "AC-WKT-17-10",
            "AC-SCN-01-01", "AC-APP-03-09", "AC-APP-03-10", "AC-APP-03-11", "AC-BLE-16-01", "AC-APP-02-08", "AC-APP-06-01", "AC-APP-05-05",
            "AC-APP-05-07", "AC-APP-05-08", "AC-RUT-10-04", "AC-RUT-10-09", "AC-RUT-10-11", "AC-APP-01-09",
        };

        // Su única verificación automática es de dotnet pero en un archivo de WP-B: ninguno en la It. 4a (QA-G4aT-09).
        public static readonly string[] WorkPackageBIds =
        {
        };
    }
}
