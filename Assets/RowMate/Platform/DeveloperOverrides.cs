namespace RowMate.Platform
{
    // Simulaciones de desarrollo SOLO en el Editor (menús RowMate/Simular …, Editor/Menus/DeveloperMenus.cs): las guarda el
    // Editor en EditorPrefs con estas claves y los proveedores de Platform las leen bajo #if UNITY_EDITOR. En un player no
    // existen (02-tech-design §1.1, §3.10).
    public static class DeveloperOverrides
    {
        // bool: simular "reducir movimiento".
        public const string ReduceMotionKey = "RowMate.Dev.ReduceMotion";

        // float: escala de letra simulada (0 = la del sistema).
        public const string TextScaleKey = "RowMate.Dev.TextScale";

        // string: lista de idiomas simulada, separada por comas ("" = la del sistema).
        public const string LanguagesKey = "RowMate.Dev.Languages";

#if UNITY_EDITOR
        public static bool ReduceMotion => UnityEditor.EditorPrefs.GetBool(ReduceMotionKey, false);

        public static float TextScale => UnityEditor.EditorPrefs.GetFloat(TextScaleKey, 0f);

        public static string Languages => UnityEditor.EditorPrefs.GetString(LanguagesKey, "");
#endif
    }
}
