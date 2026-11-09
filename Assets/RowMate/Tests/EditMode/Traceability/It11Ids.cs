namespace RowMate.Tests.EditMode.Traceability
{
    // IDs de la spec de la It. 11 (11-ble-pulsometro/01-po-spec §3): los 41 AC (HRM-09: 1, HRM-05: 3, HRM-04: 1, HRM-02: 6, HRM-03: 4,
    // HRM-06: 3, APP-01: 1, HRM-07: 5, HRM-08: 3, HRM-10: 2, HRM-11: 3, HRM-16: 2, HRM-13: 2, WKT-05: 1, HRM-15: 1, HRM-SEC: 1,
    // HRM-HW: 2) y los 7 casos legacy de test/reconnect_loop_test.dart (LT-139 a LT-145), cada uno con su verificación de
    // 02-tech-design §6.3:
    //   d = dotnet (y Unity EditMode) en archivos de WP-A · p = PlayMode / Editor (WP-B1, B2) · ps = PlayMode del simulador ·
    //   cap = captura del arnés (WP-B2) · eq = equipo ([Device-Win] -BleSmoke, [Device-Android], [BLE-HW]) · qa = revisión
    // Literales explícitos: tools/dotnet/run-tests.ps1 -Matrix lee este archivo y sus bloques UnityOnlyIds y WorkPackageBIds.
    public static class It11Ids
    {
        public static readonly (string Id, string Checks)[] Acceptance =
        {
            ("AC-HRM-09-01", "d"),
            ("AC-HRM-05-01", "d"), ("AC-HRM-05-02", "d"), ("AC-HRM-05-03", "d p cap"),
            ("AC-HRM-04-06", "d p"),
            ("AC-HRM-02-01", "d"), ("AC-HRM-02-02", "d"), ("AC-HRM-02-03", "d p cap"), ("AC-HRM-02-04", "p cap"), ("AC-HRM-02-05", "d"),
            ("AC-HRM-02-06", "eq"),
            ("AC-HRM-03-01", "d"), ("AC-HRM-03-02", "d p cap"), ("AC-HRM-03-03", "d p cap"), ("AC-HRM-03-04", "d"),
            ("AC-HRM-06-01", "d"), ("AC-HRM-06-02", "d"), ("AC-HRM-06-03", "d"),
            ("AC-APP-01-10", "d p"),
            ("AC-HRM-07-01", "d"), ("AC-HRM-07-02", "d cap"), ("AC-HRM-07-03", "p cap"), ("AC-HRM-07-04", "d"), ("AC-HRM-07-05", "d p cap"),
            ("AC-HRM-08-01", "d"), ("AC-HRM-08-02", "d"), ("AC-HRM-08-03", "d p cap"),
            ("AC-HRM-10-01", "d p"), ("AC-HRM-10-02", "d p"),
            ("AC-HRM-11-01", "d"), ("AC-HRM-11-02", "d"), ("AC-HRM-11-03", "d"),
            ("AC-HRM-16-01", "d"), ("AC-HRM-16-02", "d p eq"),
            ("AC-HRM-13-13", "d p cap"), ("AC-HRM-13-14", "d p cap"),
            ("AC-WKT-05-07", "d p cap"),
            ("AC-HRM-15-13", "d ps"),
            ("AC-HRM-SEC-01", "d qa"),
            ("AC-HRM-HW-01", "eq"), ("AC-HRM-HW-02", "eq"),
        };

        public static readonly (string Id, string File, int Line, string Name, string Checks)[] LegacyCases =
        {
            ("LT-139", "test/reconnect_loop_test.dart", 9, "intenta de inmediato, reintenta cada intervalo y para al tener éxito", "d"),
            ("LT-140", "test/reconnect_loop_test.dart", 32, "se rinde tras maxAttempts y avisa", "d"),
            ("LT-141", "test/reconnect_loop_test.dart", 55, "cancel frena los reintentos y una excepción cuenta como fallo", "d"),
            ("LT-142", "test/reconnect_loop_test.dart", 75, "start() durante un intento en vuelo descarta ese resultado y arranca de cero", "d"),
            ("LT-143", "test/reconnect_loop_test.dart", 105, "cancel() durante un intento en vuelo no programa otro intento", "d"),
            ("LT-144", "test/reconnect_loop_test.dart", 126, "start() desde onGiveUp arranca una corrida nueva", "d"),
            ("LT-145", "test/reconnect_loop_test.dart", 157, "dispara tras el timeout sin touch, y touch lo rearma", "d"),
        };

        // Sin verificación de dotnet: solo PlayMode o captura (PENDIENTE UNITY en run-tests.ps1 -Matrix).
        public static readonly string[] UnityOnlyIds =
        {
            "AC-HRM-02-04", "AC-HRM-07-03",
        };

        // Solo de equipo (eq): NO VERIFICABLE en el workflow salvo [Device-Win] (-BleSmoke de WP-B1).
        public static readonly string[] DeviceOnlyIds =
        {
            "AC-HRM-02-06", "AC-HRM-HW-01", "AC-HRM-HW-02",
        };

        // Su única verificación automática es de dotnet pero en un archivo de WP-B: ninguno.
        public static readonly string[] WorkPackageBIds =
        {
        };
    }
}
