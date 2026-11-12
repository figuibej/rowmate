namespace RowMate.Core.Scene.Water
{
    // La línea del registro del legacy (water_shader.dart:18; DT6b-13): "[Scene] Shader de agua no disponible, usando fallback:
    // <motivo>", una vez por sesión de la app (la lleva WaterAvailability).
    public static class WaterFallbackLog
    {
        public const string Tag = "Scene";
        public const string Prefix = "Shader de agua no disponible, usando fallback: ";

        public static string Line(string reason)
        {
            return Prefix + (string.IsNullOrEmpty(reason) ? "desconocido" : reason);
        }

        // Como sale en el registro, con la etiqueta.
        public static string Tagged(string reason)
        {
            return "[" + Tag + "] " + Line(reason);
        }
    }
}
