namespace RowMate.Tests.EditMode.Traceability
{
    // IDs de la spec de la It. 10a (10a-ble-android-windows/01-po-spec §3): los 43 AC nuevos (BLE-04: 7, APP-13: 3, BLE-05: 7,
    // BLE-06: 8, BLE-07: 1, BLE-11: 1, BLE-08: 6, BLE-09: 3, BLE-12: 2, BLE-91: 2, APP-14: 1, WKT-03: 1, BLE-SEC: 1), sin casos legacy
    // directos, cada uno con su verificación de 02-tech-design §8.3:
    //   d = dotnet (y Unity EditMode) en archivos de WP-A · dB = dotnet en archivos de WP-B/WP-C · p = PlayMode (WP-B1, B2) ·
    //   ps = PlayMode del simulador · cap = captura del arnés (WP-B2) · eq = equipo ([Device-Win], [Device-Android], [BLE-HW]) ·
    //   qa = revisión
    // AC-BLE-SEC-01 tiene dos segmentos de letras: el patrón de It10aIdsAreComplete lo admite.
    // Literales explícitos: tools/dotnet/run-tests.ps1 -Matrix lee este archivo y sus bloques UnityOnlyIds y WorkPackageBIds.
    public static class It10aIds
    {
        public static readonly (string Id, string Checks)[] Acceptance =
        {
            ("AC-BLE-04-01", "d p cap"), ("AC-BLE-04-02", "d p cap"), ("AC-BLE-04-03", "d p"), ("AC-BLE-04-04", "d eq"), ("AC-BLE-04-05", "d"),
            ("AC-BLE-04-06", "d p cap"), ("AC-BLE-04-07", "eq"),
            ("AC-APP-13-01", "dB eq"), ("AC-APP-13-02", "d eq"), ("AC-APP-13-03", "d ps eq"),
            ("AC-BLE-05-04", "d p cap"), ("AC-BLE-05-05", "d"), ("AC-BLE-05-06", "d"), ("AC-BLE-05-07", "d p cap"), ("AC-BLE-05-08", "d p cap"),
            ("AC-BLE-05-09", "p cap"), ("AC-BLE-05-10", "eq"),
            ("AC-BLE-06-01", "d p"), ("AC-BLE-06-02", "d"), ("AC-BLE-06-03", "d"), ("AC-BLE-06-04", "d"), ("AC-BLE-06-05", "d"),
            ("AC-BLE-06-06", "d p cap"), ("AC-BLE-06-07", "d p cap eq"), ("AC-BLE-06-08", "eq"),
            ("AC-BLE-07-05", "d p"), ("AC-BLE-11-12", "d p cap"),
            ("AC-BLE-08-02", "d"), ("AC-BLE-08-03", "d"), ("AC-BLE-08-04", "d"), ("AC-BLE-08-05", "d p cap"), ("AC-BLE-08-06", "d p cap"),
            ("AC-BLE-08-07", "eq"),
            ("AC-BLE-09-01", "d"), ("AC-BLE-09-02", "d p cap"), ("AC-BLE-09-03", "eq"),
            ("AC-BLE-12-01", "d p"), ("AC-BLE-12-02", "d p cap"),
            ("AC-BLE-91-01", "d"), ("AC-BLE-91-02", "p eq"), ("AC-APP-14-05", "eq"),
            ("AC-WKT-03-11", "d"),
            ("AC-BLE-SEC-01", "d qa"),
        };

        // De una iteración anterior y cambia acá: AC-WKT-03-10 (4a) ahora sí muestra el aviso del enlace (AC-BLE-08-06; su test de
        // PlayMode lo actualiza WP-B2).
        public static readonly string[] FromEarlier =
        {
            "AC-WKT-03-10",
        };

        // Sin verificación de dotnet: solo PlayMode, captura o equipo (PENDIENTE UNITY en run-tests.ps1 -Matrix).
        public static readonly string[] UnityOnlyIds =
        {
            "AC-BLE-05-09", "AC-BLE-91-02",
        };

        // Solo de equipo (eq): NO VERIFICABLE en el workflow salvo [Device-Win] (la demo del usuario y el humo de WP-B1).
        public static readonly string[] DeviceOnlyIds =
        {
            "AC-BLE-04-07", "AC-BLE-05-10", "AC-BLE-06-08", "AC-BLE-08-07", "AC-BLE-09-03", "AC-APP-14-05",
        };

        // Su única verificación automática es de dotnet pero en un archivo de WP-C (AndroidManifestTests).
        public static readonly string[] WorkPackageBIds =
        {
            "AC-APP-13-01",
        };
    }
}
