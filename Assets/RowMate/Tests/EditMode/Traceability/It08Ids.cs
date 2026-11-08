namespace RowMate.Tests.EditMode.Traceability
{
    // IDs de la spec de la It. 8 (08-entornos-selector/01-po-spec, corregida por su gate QA): los 40 ID (37 nuevos, §3.1 a §3.10, y
    // 3 que cambian, §4) y los casos legacy LT-99 a LT-138 (§5), cada uno con su verificación de 02-tech-design §7.4:
    //   d = dotnet (y Unity EditMode) en archivos de WP-A · u = Unity EditMode solo (Tests/UnityEditMode) · p = PlayMode
    //   (Tests/PlayMode) · cap = captura del arnés (Snapshots) · eq = equipo ([PERF], [Device-Win]) · qa = revisión
    // Los "d" de los AC visuales (AC-SCN-16-04, -17-04, -18-04) son las reglas puras: cortes, colocación sin cruces, medidas.
    // Literales explícitos: tools/dotnet/run-tests.ps1 -Matrix lee este archivo y sus bloques UnityOnlyIds y WorkPackageBIds.
    public static class It08Ids
    {
        public static readonly (string Id, string Checks)[] Acceptance =
        {
            ("AC-SCN-14-08", "d"), ("AC-SCN-14-09", "d"), ("AC-SCN-14-10", "d p cap"),
            ("AC-SCN-16-01", "d cap"), ("AC-SCN-16-02", "d cap"), ("AC-SCN-16-03", "d p cap"), ("AC-SCN-16-04", "d p cap"),
            ("AC-SCN-17-01", "d cap"), ("AC-SCN-17-02", "d cap"), ("AC-SCN-17-03", "d cap"), ("AC-SCN-17-04", "d p cap"),
            ("AC-SCN-12-01", "d cap"), ("AC-SCN-06-04", "d cap"),
            ("AC-SCN-18-01", "d cap"), ("AC-SCN-18-02", "d cap"), ("AC-SCN-18-03", "d cap"), ("AC-SCN-18-04", "d p cap"),
            ("AC-SCN-19-03", "d cap"), ("AC-SCN-19-04", "d cap"), ("AC-SCN-19-05", "d cap"),
            ("AC-SCN-20-03", "d p"), ("AC-SCN-20-04", "d cap"), ("AC-SCN-20-05", "p"), ("AC-SCN-20-06", "d cap"), ("AC-SCN-20-07", "d u p qa"),
            ("AC-SCN-21-02", "d p"), ("AC-SCN-21-03", "d p"), ("AC-SCN-21-04", "d p eq"), ("AC-SCN-21-05", "p cap"),
            ("AC-SCN-22-01", "p cap"), ("AC-SCN-22-02", "p cap"), ("AC-SCN-22-03", "p cap"), ("AC-SCN-22-04", "d u cap"), ("AC-SCN-22-05", "p u eq"),
            ("AC-SCN-01-17", "p"), ("AC-SCN-01-18", "p eq"), ("AC-SCN-01-19", "p eq"),
            ("AC-WKT-12-02", "p cap"), ("AC-SCN-21-01", "d"), ("AC-SCN-14-05", "d"),
        };

        // Los 3 de §4 que cambian (deben estar en It01Ids a It07Ids).
        public static readonly string[] FromEarlier =
        {
            "AC-WKT-12-02", "AC-SCN-21-01", "AC-SCN-14-05",
        };

        // Los casos de la spec §5: archivo y línea del legacy, el nombre exacto y la verificación.
        public static readonly (string Id, string File, int Line, string Name, string Checks)[] LegacyCases =
        {
            ("LT-99", "test/scene/environment_test.dart", 10, "EnvironmentId.river: propsForSegment es determinista", "d"),
            ("LT-100", "test/scene/environment_test.dart", 17, "EnvironmentId.river: cada segmento tiene objetos y quedan dentro del segmento", "d"),
            ("LT-101", "test/scene/environment_test.dart", 30, "EnvironmentId.river: visibleProps ordena de lejos a cerca y respeta el máximo", "d"),
            ("LT-102", "test/scene/environment_test.dart", 10, "EnvironmentId.coast: propsForSegment es determinista", "d"),
            ("LT-103", "test/scene/environment_test.dart", 17, "EnvironmentId.coast: cada segmento tiene objetos y quedan dentro del segmento", "d"),
            ("LT-104", "test/scene/environment_test.dart", 30, "EnvironmentId.coast: visibleProps ordena de lejos a cerca y respeta el máximo", "d"),
            ("LT-105", "test/scene/environment_test.dart", 10, "EnvironmentId.regatta: propsForSegment es determinista", "d"),
            ("LT-106", "test/scene/environment_test.dart", 17, "EnvironmentId.regatta: cada segmento tiene objetos y quedan dentro del segmento", "d"),
            ("LT-107", "test/scene/environment_test.dart", 30, "EnvironmentId.regatta: visibleProps ordena de lejos a cerca y respeta el máximo", "d"),
            ("LT-108", "test/scene/environment_test.dart", 42, "el canal de regata tiene boyas cada 10 m", "d"),
            ("LT-109", "test/scene/environment_test.dart", 54, "en la regata los hitos lejanos se ven aunque haya muchas boyas", "d"),
            ("LT-110", "test/scene/environment_test.dart", 60, "el río urbano tiene un puente cada 800 m", "d"),
            ("LT-111", "test/scene/sky_shore_painter_test.dart", 16, "EnvironmentId.river 390x844 12.0h pinta sin excepciones", "p"),
            ("LT-112", "test/scene/sky_shore_painter_test.dart", 16, "EnvironmentId.river 390x844 23.0h pinta sin excepciones", "p"),
            ("LT-113", "test/scene/sky_shore_painter_test.dart", 16, "EnvironmentId.river 844x390 12.0h pinta sin excepciones", "p"),
            ("LT-114", "test/scene/sky_shore_painter_test.dart", 16, "EnvironmentId.river 844x390 23.0h pinta sin excepciones", "p"),
            ("LT-115", "test/scene/sky_shore_painter_test.dart", 16, "EnvironmentId.coast 390x844 12.0h pinta sin excepciones", "p"),
            ("LT-116", "test/scene/sky_shore_painter_test.dart", 16, "EnvironmentId.coast 390x844 23.0h pinta sin excepciones", "p"),
            ("LT-117", "test/scene/sky_shore_painter_test.dart", 16, "EnvironmentId.coast 844x390 12.0h pinta sin excepciones", "p"),
            ("LT-118", "test/scene/sky_shore_painter_test.dart", 16, "EnvironmentId.coast 844x390 23.0h pinta sin excepciones", "p"),
            ("LT-119", "test/scene/sky_shore_painter_test.dart", 16, "EnvironmentId.regatta 390x844 12.0h pinta sin excepciones", "p"),
            ("LT-120", "test/scene/sky_shore_painter_test.dart", 16, "EnvironmentId.regatta 390x844 23.0h pinta sin excepciones", "p"),
            ("LT-121", "test/scene/sky_shore_painter_test.dart", 16, "EnvironmentId.regatta 844x390 12.0h pinta sin excepciones", "p"),
            ("LT-122", "test/scene/sky_shore_painter_test.dart", 16, "EnvironmentId.regatta 844x390 23.0h pinta sin excepciones", "p"),
            ("LT-123", "test/scene/sky_shore_painter_test.dart", 57, "objetos detrás de la cámara se omiten y un puente encima no rompe", "p"),
            ("LT-124", "test/scene/water_painter_test.dart", 27, "WaterPainter.uniforms devuelve 28 floats en el orden del shader", "d"),
            ("LT-125", "test/scene/environment_picker_test.dart", 11, "tocar una tarjeta cambia el escenario y lo persiste", "d p"),
            ("LT-126", "test/scene/environment_picker_test.dart", 38, "arranca con el escenario guardado", "d p"),
            ("LT-127", "test/scene/scene_view_test.dart", 11, "EnvironmentId.river 390x844 12.0h anima sin excepciones", "p"),
            ("LT-128", "test/scene/scene_view_test.dart", 11, "EnvironmentId.river 390x844 23.0h anima sin excepciones", "p"),
            ("LT-129", "test/scene/scene_view_test.dart", 11, "EnvironmentId.river 844x390 12.0h anima sin excepciones", "p"),
            ("LT-130", "test/scene/scene_view_test.dart", 11, "EnvironmentId.river 844x390 23.0h anima sin excepciones", "p"),
            ("LT-131", "test/scene/scene_view_test.dart", 11, "EnvironmentId.coast 390x844 12.0h anima sin excepciones", "p"),
            ("LT-132", "test/scene/scene_view_test.dart", 11, "EnvironmentId.coast 390x844 23.0h anima sin excepciones", "p"),
            ("LT-133", "test/scene/scene_view_test.dart", 11, "EnvironmentId.coast 844x390 12.0h anima sin excepciones", "p"),
            ("LT-134", "test/scene/scene_view_test.dart", 11, "EnvironmentId.coast 844x390 23.0h anima sin excepciones", "p"),
            ("LT-135", "test/scene/scene_view_test.dart", 11, "EnvironmentId.regatta 390x844 12.0h anima sin excepciones", "p"),
            ("LT-136", "test/scene/scene_view_test.dart", 11, "EnvironmentId.regatta 390x844 23.0h anima sin excepciones", "p"),
            ("LT-137", "test/scene/scene_view_test.dart", 11, "EnvironmentId.regatta 844x390 12.0h anima sin excepciones", "p"),
            ("LT-138", "test/scene/scene_view_test.dart", 11, "EnvironmentId.regatta 844x390 23.0h anima sin excepciones", "p"),
        };

        // Sin verificación de dotnet: solo PlayMode, Unity EditMode o captura (PENDIENTE UNITY en run-tests.ps1 -Matrix).
        public static readonly string[] UnityOnlyIds =
        {
            "AC-SCN-20-05", "AC-SCN-21-05", "AC-SCN-22-01", "AC-SCN-22-02", "AC-SCN-22-03", "AC-SCN-22-05", "AC-SCN-01-17", "AC-SCN-01-18",
            "AC-SCN-01-19", "AC-WKT-12-02",
            "LT-111", "LT-112", "LT-113", "LT-114", "LT-115", "LT-116", "LT-117", "LT-118", "LT-119", "LT-120", "LT-121", "LT-122", "LT-123",
            "LT-127", "LT-128", "LT-129", "LT-130", "LT-131", "LT-132", "LT-133", "LT-134", "LT-135", "LT-136", "LT-137", "LT-138",
        };

        // Solo de equipo (eq): ninguno solo (los de [PERF] y [Device-Win] tienen también PlayMode).
        public static readonly string[] DeviceOnlyIds =
        {
        };

        // Su única verificación automática es de dotnet pero en un archivo de WP-B: ninguno.
        public static readonly string[] WorkPackageBIds =
        {
        };
    }
}
