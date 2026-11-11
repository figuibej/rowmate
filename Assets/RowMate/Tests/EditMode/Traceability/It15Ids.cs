namespace RowMate.Tests.EditMode.Traceability
{
    // IDs de la spec de la It. 15 (15-deuda-pc/01-po-spec §3): los 40 AC de deuda (AC-D15-01 a -40), sin casos legacy, cada uno con su
    // verificación de 02-tech-design §6:
    //   d = dotnet en archivos de WP-A · p = PlayMode · u = Unity EditMode · cap = captura del arnés · eq = equipo o demo
    //   ([Device-Win] con el build de desarrollo; el Explorador y el Narrador reales, demo del usuario) · qa = lectura de QA.
    // Literales explícitos: tools/dotnet/run-tests.ps1 -Matrix lee este archivo y sus bloques UnityOnlyIds, WorkPackageBIds y
    // DeviceOnlyIds (este último, en la sección "EN EQUIPO" desde la It. 15, AC-D15-38).
    public static class It15Ids
    {
        public static readonly (string Id, string Checks)[] Acceptance =
        {
            ("AC-D15-01", "d cap"), ("AC-D15-02", "d p cap"), ("AC-D15-03", "d eq"), ("AC-D15-04", "p"), ("AC-D15-05", "p u cap"),
            ("AC-D15-06", "p cap"), ("AC-D15-07", "d p"), ("AC-D15-08", "qa"), ("AC-D15-09", "d p cap"), ("AC-D15-10", "p cap"),
            ("AC-D15-11", "d p cap"), ("AC-D15-12", "d"), ("AC-D15-13", "d p cap eq"), ("AC-D15-14", "d"), ("AC-D15-15", "d"),
            ("AC-D15-16", "d p u"), ("AC-D15-17", "d p"), ("AC-D15-18", "d qa cap"), ("AC-D15-19", "d u eq"), ("AC-D15-20", "d"),
            ("AC-D15-21", "p"), ("AC-D15-22", "d cap"), ("AC-D15-23", "d cap"), ("AC-D15-24", "d p cap"), ("AC-D15-25", "d p cap"),
            ("AC-D15-26", "p cap"), ("AC-D15-27", "d"), ("AC-D15-28", "p"), ("AC-D15-29", "p eq"), ("AC-D15-30", "p"),
            ("AC-D15-31", "cap"), ("AC-D15-32", "d p"), ("AC-D15-33", "p"), ("AC-D15-34", "p u cap"), ("AC-D15-35", "p cap"),
            ("AC-D15-36", "p"), ("AC-D15-37", "eq"), ("AC-D15-38", "d"), ("AC-D15-39", "p"), ("AC-D15-40", "d p u qa"),
        };

        // Sin verificación de dotnet: solo PlayMode, Unity EditMode o captura (PENDIENTE UNITY en run-tests.ps1 -Matrix).
        public static readonly string[] UnityOnlyIds =
        {
            "AC-D15-04", "AC-D15-05", "AC-D15-06", "AC-D15-10", "AC-D15-21", "AC-D15-26", "AC-D15-28", "AC-D15-29",
            "AC-D15-30", "AC-D15-31", "AC-D15-33", "AC-D15-34", "AC-D15-35", "AC-D15-36", "AC-D15-39",
        };

        // Su verificación de dotnet está en archivos de WP-B (ninguno en la It. 15: los tests de dotnet son todos de WP-A).
        public static readonly string[] WorkPackageBIds =
        {
        };

        // Los de equipo (02-tech-design §6): la sonda del Historial y el humo de la ventana en el build de desarrollo, el [PERF] de
        // 120 s y la lectura de QA de los documentos de la 9. Con un test de dotnet que pasa salen PASA; sin resultado, EN EQUIPO.
        public static readonly string[] DeviceOnlyIds =
        {
            "AC-D15-03", "AC-D15-08", "AC-D15-19", "AC-D15-37",
        };
    }
}
