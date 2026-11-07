namespace RowMate.Tests.EditMode.Traceability
{
    // IDs de la spec de la It. 7 (07-remero-palada/01-po-spec, corregida por su gate QA): los 38 ID (36 nuevos, §3.1 a §3.6, y 2 que
    // cambian, §4) y los casos legacy LT-88 a LT-98 (§5), cada uno con su verificación de 02-tech-design §6.4:
    //   d = dotnet (y Unity EditMode) en archivos de WP-A · u = Unity EditMode solo (Tests/UnityEditMode) · p = PlayMode
    //   (Tests/PlayMode) · cap = captura del arnés (Snapshots) · eq = equipo ([PERF], [Device-Win]) · qa = revisión
    // AC-SCN-01-16 queda sin "d" (QA7-G-09): el 0 B de dotnet es solo apoyo; se da por bueno con FrameAllocations y el Profiler.
    // Literales explícitos: tools/dotnet/run-tests.ps1 -Matrix lee este archivo y sus bloques UnityOnlyIds y WorkPackageBIds.
    public static class It07Ids
    {
        public static readonly (string Id, string Checks)[] Acceptance =
        {
            ("AC-SCN-04-01", "d"), ("AC-SCN-04-02", "d"), ("AC-SCN-04-03", "d"), ("AC-SCN-04-04", "d cap"), ("AC-SCN-04-05", "d cap"),
            ("AC-SCN-05-01", "d"), ("AC-SCN-05-02", "d"), ("AC-SCN-05-03", "d"), ("AC-SCN-05-04", "d"), ("AC-SCN-05-05", "d"),
            ("AC-SCN-05-06", "d cap"), ("AC-SCN-05-07", "d cap"), ("AC-SCN-05-08", "d p cap"), ("AC-SCN-05-09", "d p"), ("AC-SCN-05-10", "d cap"),
            ("AC-SCN-06-01", "d"), ("AC-SCN-06-02", "d"), ("AC-SCN-06-03", "d cap"),
            ("AC-SCN-07-01", "d"), ("AC-SCN-07-02", "d cap"), ("AC-SCN-07-03", "d cap"),
            ("AC-SCN-08-01", "d p cap"), ("AC-SCN-08-02", "cap"), ("AC-SCN-08-03", "d cap"), ("AC-SCN-08-04", "d cap"), ("AC-SCN-08-05", "d cap"),
            ("AC-SCN-08-06", "d p cap"), ("AC-SCN-08-07", "p"), ("AC-SCN-08-08", "p u cap"), ("AC-SCN-08-09", "d qa"), ("AC-SCN-08-10", "d cap"),
            ("AC-SCN-08-11", "d p"), ("AC-SCN-08-12", "d cap"),
            ("AC-WKT-17-14", "d p cap"), ("AC-WKT-18-23", "d p cap"), ("AC-SCN-01-16", "p eq"),
            ("AC-SCN-01-08", "d cap"), ("AC-SCN-02-04", "d cap"),
        };

        // Los 2 de §4 que cambian (deben estar en It06aIds).
        public static readonly string[] FromEarlier =
        {
            "AC-SCN-01-08", "AC-SCN-02-04",
        };

        // Los casos de la spec §5: archivo y línea del legacy, el nombre exacto y la verificación.
        public static readonly (string Id, string File, int Line, string Name, string Checks)[] LegacyCases =
        {
            ("LT-88", "test/scene/scene_state_test.dart", 37, "la fase de palada avanza a spm/60 ciclos por segundo", "d"),
            ("LT-89", "test/scene/scene_state_test.dart", 46, "sin paladas la fase termina el ciclo y queda en el catch", "d"),
            ("LT-90", "test/scene/scene_state_test.dart", 60, "en pausa no avanza la palada ni la velocidad", "d"),
            ("LT-91", "test/scene/scene_state_test.dart", 69, "al soltar la pala aparecen dos charcos que envejecen y desaparecen", "d"),
            ("LT-92", "test/scene/stroke_cycle_test.dart", 5, "catch: piernas plegadas, brazos estirados, pala en el agua", "d"),
            ("LT-93", "test/scene/stroke_cycle_test.dart", 14, "finish: piernas extendidas, brazos flexionados, pala sale", "d"),
            ("LT-94", "test/scene/stroke_cycle_test.dart", 23, "en el drive: piernas antes que espalda, espalda antes que brazos", "d"),
            ("LT-95", "test/scene/stroke_cycle_test.dart", 29, "la recuperación vuelve todo al catch", "d"),
            ("LT-96", "test/scene/boat_painter_test.dart", 10, "geometría de la palada: catch hacia la cámara, pala hacia la proa", "d"),
            ("LT-97", "test/scene/boat_painter_test.dart", 25, "390x844 pinta todas las fases sin excepciones", "p"),
            ("LT-98", "test/scene/boat_painter_test.dart", 25, "844x390 pinta todas las fases sin excepciones", "p"),
        };

        // Sin verificación de dotnet: solo PlayMode, Unity EditMode o captura (PENDIENTE UNITY en run-tests.ps1 -Matrix).
        public static readonly string[] UnityOnlyIds =
        {
            "AC-SCN-08-02", "AC-SCN-08-07", "AC-SCN-08-08", "AC-SCN-01-16", "LT-97", "LT-98",
        };

        // Solo de equipo (eq): ninguno solo (AC-SCN-01-16 tiene también PlayMode).
        public static readonly string[] DeviceOnlyIds =
        {
        };

        // Su única verificación automática es de dotnet pero en un archivo de WP-B: ninguno.
        public static readonly string[] WorkPackageBIds =
        {
        };
    }
}
