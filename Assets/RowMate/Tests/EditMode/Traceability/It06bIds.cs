namespace RowMate.Tests.EditMode.Traceability
{
    // IDs de la spec de la It. 6b (06b-agua-avanzada/01-po-spec §3): los 38 AC (AC-6B-01 a -38), sin casos legacy nuevos, cada uno con su
    // verificación de 02-tech-design §10:
    //   d = dotnet en archivos de WP-A · p = PlayMode · u = Unity EditMode · cap = captura del arnés · eq = equipo ([Device-Win] con el
    //   build de desarrollo o de simulador; [Device-Android] NO VERIFICABLE) · qa = lectura de QA (git status, ProjectSettingsGuard).
    // Literales explícitos: tools/dotnet/run-tests.ps1 -Matrix lee este archivo y sus bloques UnityOnlyIds, WorkPackageBIds y DeviceOnlyIds.
    public static class It06bIds
    {
        public static readonly (string Id, string Checks)[] Acceptance =
        {
            ("AC-6B-01", "d p eq"), ("AC-6B-02", "d p eq"), ("AC-6B-03", "d u cap"), ("AC-6B-04", "d cap"), ("AC-6B-05", "p"),
            ("AC-6B-06", "d p u"), ("AC-6B-07", "d p cap"), ("AC-6B-08", "d p eq"), ("AC-6B-09", "d p cap"), ("AC-6B-10", "d"),
            ("AC-6B-11", "d cap"), ("AC-6B-12", "d p cap"), ("AC-6B-13", "d cap"), ("AC-6B-14", "d p cap"), ("AC-6B-15", "d cap"),
            ("AC-6B-16", "d p cap"), ("AC-6B-17", "d cap"), ("AC-6B-18", "d cap"), ("AC-6B-19", "d cap"), ("AC-6B-20", "d cap"),
            ("AC-6B-21", "d cap"), ("AC-6B-22", "p"), ("AC-6B-23", "d p cap"), ("AC-6B-24", "d p"), ("AC-6B-25", "p"),
            ("AC-6B-26", "p eq"), ("AC-6B-27", "p cap"), ("AC-6B-28", "d eq"), ("AC-6B-29", "d p u"), ("AC-6B-30", "p eq"),
            ("AC-6B-31", "d eq"), ("AC-6B-32", "eq"), ("AC-6B-33", "d cap"), ("AC-6B-34", "p cap"), ("AC-6B-35", "d p u"),
            ("AC-6B-36", "d u"), ("AC-6B-37", "d u"), ("AC-6B-38", "qa"),
        };

        // Sin verificación de dotnet: solo PlayMode, Unity EditMode o captura (PENDIENTE UNITY en run-tests.ps1 -Matrix).
        public static readonly string[] UnityOnlyIds =
        {
            "AC-6B-05", "AC-6B-22", "AC-6B-25", "AC-6B-26", "AC-6B-27", "AC-6B-30", "AC-6B-34",
        };

        // Su verificación de dotnet está en archivos de WP-B (ninguno: los de dotnet son de WP-A; los de WP-B que leen texto, como
        // ItSixBToolingTests, están en archivos de WP-A con su guarda "pendiente WP-B").
        public static readonly string[] WorkPackageBIds =
        {
        };

        // Sin prueba automática: el Android de gama media (NO VERIFICABLE, #7) y el repo limpio que revisa QA.
        public static readonly string[] DeviceOnlyIds =
        {
            "AC-6B-32", "AC-6B-38",
        };
    }
}
