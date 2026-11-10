namespace RowMate.Tests.EditMode.Traceability
{
    // IDs de la spec de la It. 14 (14-pulido-plataformas/01-po-spec §3): los 44 AC (34 de producto + 10 de deuda), sin casos legacy,
    // cada uno con su verificación de 02-tech-design §10:
    //   d = dotnet en archivos de WP-A · dB = dotnet en archivos de WP-B (ProjectSettingsIt14Tests, CiUnityWorkflowTests,
    //   WorkflowYamlTests, ItFourteenToolingTests) · p = PlayMode · u = Unity EditMode · cap = captura del arnés ·
    //   eq = equipo o demo ([Device-Win], [Device-Android] y [Apple] NO VERIFICABLES) · qa = revisión de QA · ci = corrida real del CI
    //   (NO VERIFICABLE hasta que el usuario cargue los secretos).
    // Literales explícitos: tools/dotnet/run-tests.ps1 -Matrix lee este archivo y sus bloques UnityOnlyIds y WorkPackageBIds.
    public static class It14Ids
    {
        public static readonly (string Id, string Checks)[] Acceptance =
        {
            ("AC-APP-16-06", "d dB eq"), ("AC-APP-16-07", "dB"), ("AC-APP-16-08", "d u cap eq"), ("AC-APP-16-09", "d dB cap eq"),
            ("AC-APP-16-10", "d dB p cap eq"), ("AC-APP-16-11", "dB u eq"), ("AC-APP-16-12", "dB eq"),
            ("AC-APP-15-05", "d u eq"), ("AC-APP-15-06", "d p cap eq"), ("AC-APP-15-07", "d p eq"), ("AC-APP-15-08", "eq"),
            ("AC-APP-15-09", "d dB u eq"), ("AC-APP-15-10", "p cap eq"),
            ("AC-APP-11-01", "d p cap"), ("AC-APP-11-02", "p"), ("AC-APP-11-03", "d"),
            ("AC-APP-03-12", "d p cap eq"), ("AC-APP-03-13", "p eq"), ("AC-APP-03-14", "d p eq"), ("AC-APP-03-15", "p eq"),
            ("AC-POL-01", "p cap"), ("AC-POL-02", "p cap"), ("AC-POL-03", "d p eq"), ("AC-POL-04", "p cap"), ("AC-POL-05", "p qa"),
            ("AC-POL-06", "eq"), ("AC-POL-07", "dB u qa"),
            ("AC-APP-19-06", "dB ci"), ("AC-APP-19-07", "dB ci"), ("AC-APP-19-08", "dB ci eq"), ("AC-APP-19-09", "dB ci"),
            ("AC-APP-19-10", "d dB ci"), ("AC-APP-19-11", "dB"),
            ("AC-NV-01", "qa"),
            ("AC-DEBT-01", "p cap"), ("AC-DEBT-02", "p cap"), ("AC-DEBT-03", "p cap"), ("AC-DEBT-04", "d p cap"), ("AC-DEBT-05", "d p cap"),
            ("AC-DEBT-06", "p cap"), ("AC-DEBT-07", "d cap"), ("AC-DEBT-08", "p cap"), ("AC-DEBT-09", "d p cap"), ("AC-DEBT-10", "d p eq"),
        };

        // Sin verificación de dotnet: solo PlayMode, Unity EditMode o captura (PENDIENTE UNITY en run-tests.ps1 -Matrix).
        public static readonly string[] UnityOnlyIds =
        {
            "AC-APP-15-10", "AC-APP-11-02", "AC-APP-03-13", "AC-APP-03-15", "AC-POL-01", "AC-POL-02", "AC-POL-04", "AC-POL-05",
            "AC-DEBT-01", "AC-DEBT-02", "AC-DEBT-03", "AC-DEBT-06", "AC-DEBT-08",
        };

        // Su verificación de dotnet está en archivos de WP-B (dB), sin una de WP-A (PENDIENTE WP-B).
        public static readonly string[] WorkPackageBIds =
        {
            "AC-APP-16-07", "AC-APP-16-11", "AC-APP-16-12", "AC-POL-07", "AC-APP-19-06", "AC-APP-19-07", "AC-APP-19-08", "AC-APP-19-09",
            "AC-APP-19-11",
        };

        // Solo de equipo, demo, QA o CI real: el DPI con dos monitores, el rendimiento en la PC y el arrastre de lo no verificable.
        public static readonly string[] DeviceOnlyIds =
        {
            "AC-APP-15-08", "AC-POL-06", "AC-NV-01",
        };
    }
}
