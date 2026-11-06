namespace RowMate.Tests.EditMode.Traceability
{
    // IDs de la spec de la It. 6a (06a-escena-lago/01-po-spec, corregida por su gate QA): los 101 ID (78 nuevos, §4.1 a §4.9, y 23 de
    // iteraciones anteriores, §5.2 y §5.3) y los casos legacy LT-62 a LT-87 (§6), cada uno con su verificación de 02-tech-design §6.5:
    //   d = dotnet (y Unity EditMode) en archivos de WP-A · s = dotnet con el simulador · u = Unity EditMode solo
    //   (Tests/UnityEditMode) · p = PlayMode (Tests/PlayMode) · ps = PlayMode con el simulador (Tests/PlayModeSimulator) ·
    //   cap = captura del arnés (Snapshots) · eq = equipo (QA o el usuario; [Ignore] en WorkoutDeviceChecksTests) · qa = revisión
    // Cambios por implementación (WP-A, A6a-02 y A6a-03 de 02-tech-design): AC-APP-05-08, AC-SCN-01-04, -01-07, -01-08, -01-12,
    // -14-07, AC-WKT-13-06, -13-08, -17-07 y AC-BLE-15-09 suman "d": sus reglas puras (reducir movimiento, la hora al volver, el
    // fundido de la escena demorada, el casco en la zona, 0 B en Tick, la aparición, las líneas de tiempo, la última lectura) tienen
    // prueba de dotnet además de la de Unity. AC-SCN-01-09 y -01-10 son solo de equipo (eq, DeviceOnlyIds): sus [Ignore] los suma
    // WP-B2 en WorkoutDeviceChecksTests.
    // Literales explícitos: tools/dotnet/run-tests.ps1 -Matrix lee este archivo (todos los It0*Ids.cs) y sus bloques UnityOnlyIds y
    // WorkPackageBIds.
    public static class It06aIds
    {
        public static readonly (string Id, string Checks)[] Acceptance =
        {
            ("AC-SCN-02-01", "d p"), ("AC-SCN-02-02", "d p"), ("AC-SCN-02-03", "d p cap"), ("AC-SCN-02-04", "d cap"),
            ("AC-SCN-02-05", "d p cap"), ("AC-SCN-02-06", "d p cap"), ("AC-SCN-02-07", "d p cap"), ("AC-SCN-02-08", "d p"),
            ("AC-SCN-03-01", "d cap"), ("AC-SCN-03-02", "d cap"), ("AC-SCN-03-03", "d cap"), ("AC-SCN-03-04", "d p"),
            ("AC-SCN-03-05", "d p cap"), ("AC-SCN-03-06", "d"), ("AC-SCN-03-07", "d ps"),
            ("AC-SCN-09-01", "d p"), ("AC-SCN-09-02", "d"), ("AC-SCN-09-03", "d"), ("AC-SCN-09-04", "d cap"), ("AC-SCN-09-05", "d cap qa"),
            ("AC-SCN-09-06", "d cap"),
            ("AC-SCN-10-01", "d"), ("AC-SCN-10-02", "d"), ("AC-SCN-10-03", "d"), ("AC-SCN-10-04", "d cap"), ("AC-SCN-10-05", "d cap"),
            ("AC-SCN-10-06", "d cap"), ("AC-SCN-10-07", "d cap"),
            ("AC-SCN-11-01", "d p cap"), ("AC-SCN-11-02", "d cap qa"), ("AC-SCN-11-03", "d cap"), ("AC-SCN-11-04", "d cap"),
            ("AC-SCN-11-05", "d cap qa"), ("AC-SCN-11-06", "d cap"), ("AC-SCN-11-07", "p"),
            ("AC-SCN-14-01", "d"), ("AC-SCN-14-02", "d"), ("AC-SCN-14-03", "d"), ("AC-SCN-14-04", "d"), ("AC-SCN-14-05", "d"),
            ("AC-SCN-14-06", "d"), ("AC-SCN-14-07", "d p cap"),
            ("AC-SCN-15-01", "d"), ("AC-SCN-15-02", "d"), ("AC-SCN-15-03", "d"), ("AC-SCN-15-04", "d"), ("AC-SCN-15-05", "d p cap"),
            ("AC-SCN-15-06", "d p cap"), ("AC-SCN-15-07", "d p cap qa"), ("AC-SCN-15-08", "d p qa"),
            ("AC-SCN-23-01", "ps eq"), ("AC-SCN-23-02", "d ps cap"), ("AC-SCN-23-03", "ps eq"), ("AC-SCN-23-04", "ps cap"),
            ("AC-SCN-01-02", "p cap"), ("AC-SCN-01-03", "d p cap"), ("AC-SCN-01-04", "d p eq"), ("AC-SCN-01-05", "p eq"),
            ("AC-SCN-01-06", "p eq"), ("AC-SCN-01-07", "d p cap"), ("AC-SCN-01-08", "d p cap"), ("AC-SCN-01-09", "eq"),
            ("AC-SCN-01-10", "eq"), ("AC-SCN-01-11", "p"), ("AC-SCN-01-12", "d p"), ("AC-SCN-01-13", "d p eq"),
            ("AC-SCN-01-14", "d u"), ("AC-SCN-01-15", "d p cap"),
            ("AC-SCN-13-01", "p"), ("AC-SCN-13-02", "d p cap"), ("AC-SCN-13-03", "d p cap"), ("AC-SCN-13-04", "p cap"),
            ("AC-SCN-19-01", "d cap"), ("AC-SCN-19-02", "d cap"),
            ("AC-SCN-20-01", "d cap"), ("AC-SCN-20-02", "d cap"),
            ("AC-SCN-21-01", "p qa"),
            ("AC-WKT-17-13", "d cap"),
            ("AC-WKT-13-02", "p cap"), ("AC-WKT-13-06", "d p cap"), ("AC-WKT-13-08", "d p cap"), ("AC-WKT-13-12", "d p cap"),
            ("AC-WKT-13-15", "d p eq"), ("AC-WKT-14-19", "p eq"), ("AC-WKT-17-07", "d p cap"), ("AC-WKT-17-09", "p cap"),
            ("AC-BLE-16-07", "d ps cap"), ("AC-SCN-01-01", "p cap"), ("AC-APP-05-08", "d p cap"),
            ("AC-WKT-13-09", "p"), ("AC-WKT-13-13", "p"), ("AC-WKT-13-14", "d p"), ("AC-WKT-19-03", "ps"), ("AC-WKT-17-01", "p cap"),
            ("AC-WKT-02-08", "p"), ("AC-WKT-18-21", "p"), ("AC-BLE-16-02", "ps cap"), ("AC-BLE-16-03", "ps cap"), ("AC-BLE-15-09", "d ps"),
            ("AC-HIS-20-04", "d u qa"), ("AC-WKT-21-01", "p"),
        };

        // Los 23 de §5.2 y §5.3 (deben estar en It01Ids a It05Ids; AC-SCN-01-01, el fondo provisorio, está en It04aIds).
        public static readonly string[] FromEarlier =
        {
            "AC-WKT-13-02", "AC-WKT-13-06", "AC-WKT-13-08", "AC-WKT-13-12", "AC-WKT-13-15", "AC-WKT-14-19", "AC-WKT-17-07", "AC-WKT-17-09",
            "AC-BLE-16-07", "AC-SCN-01-01", "AC-APP-05-08",
            "AC-WKT-13-09", "AC-WKT-13-13", "AC-WKT-13-14", "AC-WKT-19-03", "AC-WKT-17-01", "AC-WKT-02-08", "AC-WKT-18-21", "AC-BLE-16-02",
            "AC-BLE-16-03", "AC-BLE-15-09", "AC-HIS-20-04", "AC-WKT-21-01",
        };

        // Los casos de la spec §6: archivo y línea del legacy, el nombre exacto y la verificación.
        public static readonly (string Id, string File, int Line, string Name, string Checks)[] LegacyCases =
        {
            ("LT-62", "test/scene/scene_camera_test.dart", 9, "horizonte y focal según orientación", "d"),
            ("LT-63", "test/scene/scene_camera_test.dart", 16, "lo lejano queda más cerca del horizonte, centrado y más chico", "d"),
            ("LT-64", "test/scene/scene_camera_test.dart", 30, "detrás de la cámara no se proyecta", "d"),
            ("LT-65", "test/scene/scene_camera_test.dart", 34, "unprojectWater invierte project para y = 0", "d"),
            ("LT-66", "test/scene/scene_state_test.dart", 14, "la velocidad sale del split y se alcanza en unos segundos", "d"),
            ("LT-67", "test/scene/scene_state_test.dart", 26, "al dejar de remar desacelera hasta detenerse", "d"),
            ("LT-68", "test/scene/scene_state_test.dart", 55, "shaderDistance se acota a 4096 m", "d"),
            ("LT-69", "test/scene/time_of_day_test.dart", 9, "12:00 es día con el sol arriba", "d"),
            ("LT-70", "test/scene/time_of_day_test.dart", 17, "23:00 es noche con luna", "d"),
            ("LT-71", "test/scene/time_of_day_test.dart", 25, "18:30 interpola entre atardecer y anochecer", "d"),
            ("LT-72", "test/scene/time_of_day_test.dart", 32, "de 22 h a 4 h la paleta es noche cerrada, sin falso amanecer", "d"),
            ("LT-73", "test/scene/time_of_day_test.dart", 37, "a las 19 h sigue el atardecer y a las 20 h ya sale la luna", "d"),
            ("LT-74", "test/scene/time_of_day_test.dart", 44, "la paleta es continua a medianoche y al amanecer", "d"),
            ("LT-75", "test/scene/environment_test.dart", 10, "EnvironmentId.lake: propsForSegment es determinista", "d"),
            ("LT-76", "test/scene/environment_test.dart", 17, "EnvironmentId.lake: cada segmento tiene objetos y quedan dentro del segmento", "d"),
            ("LT-77", "test/scene/environment_test.dart", 30, "EnvironmentId.lake: visibleProps ordena de lejos a cerca y respeta el máximo", "d"),
            ("LT-78", "test/scene/sky_shore_painter_test.dart", 16, "EnvironmentId.lake 390x844 12.0h pinta sin excepciones", "p"),
            ("LT-79", "test/scene/sky_shore_painter_test.dart", 16, "EnvironmentId.lake 390x844 23.0h pinta sin excepciones", "p"),
            ("LT-80", "test/scene/sky_shore_painter_test.dart", 16, "EnvironmentId.lake 844x390 12.0h pinta sin excepciones", "p"),
            ("LT-81", "test/scene/sky_shore_painter_test.dart", 16, "EnvironmentId.lake 844x390 23.0h pinta sin excepciones", "p"),
            ("LT-82", "test/scene/sky_shore_painter_test.dart", 39, "las estrellas aparecen apenas oscurece (19 h), no solo de noche cerrada", "d"),
            ("LT-83", "test/scene/water_painter_test.dart", 11, "sin shader pinta el fallback sin excepciones", "p"),
            ("LT-84", "test/scene/scene_view_test.dart", 11, "EnvironmentId.lake 390x844 12.0h anima sin excepciones", "p"),
            ("LT-85", "test/scene/scene_view_test.dart", 11, "EnvironmentId.lake 390x844 23.0h anima sin excepciones", "p"),
            ("LT-86", "test/scene/scene_view_test.dart", 11, "EnvironmentId.lake 844x390 12.0h anima sin excepciones", "p"),
            ("LT-87", "test/scene/scene_view_test.dart", 11, "EnvironmentId.lake 844x390 23.0h anima sin excepciones", "p"),
        };

        // Sin verificación de dotnet (ni d ni s): solo PlayMode, Unity EditMode o captura. En run-tests.ps1 -Matrix figuran como
        // PENDIENTE UNITY (no SIN TEST) mientras no se le pase -UnityResults. TraceabilityTests controla que la lista sea
        // exactamente la que sale de Acceptance.
        public static readonly string[] UnityOnlyIds =
        {
            "AC-SCN-11-07", "AC-SCN-23-01", "AC-SCN-23-03", "AC-SCN-23-04", "AC-SCN-01-02", "AC-SCN-01-05", "AC-SCN-01-06", "AC-SCN-01-11",
            "AC-SCN-13-01", "AC-SCN-13-04", "AC-SCN-21-01", "AC-WKT-13-02", "AC-WKT-14-19", "AC-WKT-17-09", "AC-SCN-01-01", "AC-WKT-13-09",
            "AC-WKT-13-13", "AC-WKT-19-03", "AC-WKT-17-01", "AC-WKT-02-08", "AC-WKT-18-21", "AC-BLE-16-02", "AC-BLE-16-03", "AC-WKT-21-01",
            // QA-V6a-PAR-05 y QA-V6a-11 (ronda 1 de la verificación): los LT que se verifican solo en PlayMode.
            "LT-78", "LT-79", "LT-80", "LT-81", "LT-83", "LT-84", "LT-85", "LT-86", "LT-87",
        };

        // Solo de equipo (eq): sus [Ignore] con el motivo van en WorkoutDeviceChecksTests (WP-B2).
        public static readonly string[] DeviceOnlyIds =
        {
            "AC-SCN-01-09", "AC-SCN-01-10",
        };

        // Su única verificación automática es de dotnet pero en un archivo de WP-B: ninguno en la It. 6a.
        public static readonly string[] WorkPackageBIds =
        {
        };
    }
}
