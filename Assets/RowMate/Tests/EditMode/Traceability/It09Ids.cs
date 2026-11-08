namespace RowMate.Tests.EditMode.Traceability
{
    // IDs de la spec de la It. 9 (09-historial/01-po-spec, corregida por su gate QA): los 43 AC nuevos de §3 (HIS-05: 4, HIS-06: 4,
    // HIS-07: 7, HIS-09: 4, HIS-10: 1, HIS-11: 4, HIS-12: 2, HIS-13: 7, HIS-14: 3, HIS-15: 3, RUT-20: 1, HIS-19: 3), sin casos legacy
    // directos (spec §0), cada uno con su verificación de 02-tech-design §8.3:
    //   d = dotnet (y Unity EditMode) en archivos de WP-A · p = PlayMode (Tests/PlayMode, WP-B1) · ps = PlayMode del simulador ·
    //   cap = captura del arnés (Snapshots, WP-B2) · eq = equipo ([PERF], [Device-Win], [Device-And]) · qa = revisión
    // Los "Se vuelven a verificar" de §8.3 (AC-RUT-18-01 a -09, AC-HIS-19-01, -02, AC-APP-02-03, -08, AC-HIS-20-04) siguen en sus
    // iteraciones: no se repiten acá.
    // Literales explícitos: tools/dotnet/run-tests.ps1 -Matrix lee este archivo y sus bloques UnityOnlyIds y WorkPackageBIds.
    public static class It09Ids
    {
        public static readonly (string Id, string Checks)[] Acceptance =
        {
            ("AC-HIS-05-01", "d"), ("AC-HIS-05-02", "d"), ("AC-HIS-05-03", "d"), ("AC-HIS-05-04", "d"),
            ("AC-HIS-06-01", "d p"), ("AC-HIS-06-02", "d p ps"), ("AC-HIS-06-03", "d p cap"), ("AC-HIS-06-04", "d p cap"),
            ("AC-HIS-07-01", "p cap"), ("AC-HIS-07-02", "d p cap"), ("AC-HIS-07-03", "d p cap"), ("AC-HIS-07-04", "d p cap"),
            ("AC-HIS-07-05", "d cap"), ("AC-HIS-07-06", "p cap"), ("AC-HIS-07-07", "p eq"),
            ("AC-HIS-09-01", "p eq"), ("AC-HIS-09-02", "d"), ("AC-HIS-09-03", "d p cap"), ("AC-HIS-09-04", "d p"),
            ("AC-HIS-10-01", "d p cap"),
            ("AC-HIS-11-01", "d cap"), ("AC-HIS-11-02", "p cap"), ("AC-HIS-11-03", "d cap"), ("AC-HIS-11-04", "d"),
            ("AC-HIS-12-01", "d p cap"), ("AC-HIS-12-02", "d p cap"),
            ("AC-HIS-13-01", "d cap"), ("AC-HIS-13-02", "d cap"), ("AC-HIS-13-03", "d cap"), ("AC-HIS-13-04", "d cap"), ("AC-HIS-13-05", "d cap"),
            ("AC-HIS-13-06", "d p cap"), ("AC-HIS-13-07", "d p eq cap"),
            ("AC-HIS-14-01", "d"), ("AC-HIS-14-02", "d cap"), ("AC-HIS-14-03", "d cap"),
            ("AC-HIS-15-01", "d"), ("AC-HIS-15-02", "d cap"), ("AC-HIS-15-03", "d cap"),
            ("AC-RUT-20-01", "d p cap"),
            ("AC-HIS-19-03", "d"), ("AC-HIS-19-04", "d ps cap"), ("AC-HIS-19-05", "p eq"),
        };

        // Ninguno de una iteración anterior cambia (§8.3: los que se vuelven a verificar quedan en sus listas).
        public static readonly string[] FromEarlier =
        {
        };

        // Sin verificación de dotnet: solo PlayMode, captura o equipo (PENDIENTE UNITY en run-tests.ps1 -Matrix).
        public static readonly string[] UnityOnlyIds =
        {
            "AC-HIS-07-01", "AC-HIS-07-06", "AC-HIS-07-07", "AC-HIS-09-01", "AC-HIS-11-02", "AC-HIS-19-05",
        };

        // Solo de equipo (eq): ninguno solo (los de [PERF] y [Device-*] tienen también PlayMode).
        public static readonly string[] DeviceOnlyIds =
        {
        };

        // Su única verificación automática es de dotnet pero en un archivo de WP-B: ninguno.
        public static readonly string[] WorkPackageBIds =
        {
        };
    }
}
