namespace RowMate.Core.Scene
{
    // La clave de la preferencia y el nombre de cada escenario (scene/scene_settings.dart:11, :28; AC-SCN-21-02): lake, river,
    // coast y regatta, el EnvironmentId.name de Dart. La lectura es la de asNameMap(): exacta y sensible a mayúsculas.
    public static class EnvironmentKeys
    {
        public const string PreferenceKey = "scene.environment";   // scene_settings.dart:11

        public static string ToKey(EnvironmentId id)
        {
            switch (id)
            {
                case EnvironmentId.Lake:
                    return "lake";
                case EnvironmentId.River:
                    return "river";
                case EnvironmentId.Coast:
                    return "coast";
                case EnvironmentId.Regatta:
                    return "regatta";
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(id), id, "EnvironmentKeys: escenario desconocido.");
            }
        }

        // false (y el lago) con null, vacío o un nombre desconocido ("marte", "Lake").
        public static bool TryParse(string? key, out EnvironmentId id)
        {
            switch (key)
            {
                case "lake":
                    id = EnvironmentId.Lake;
                    return true;
                case "river":
                    id = EnvironmentId.River;
                    return true;
                case "coast":
                    id = EnvironmentId.Coast;
                    return true;
                case "regatta":
                    id = EnvironmentId.Regatta;
                    return true;
                default:
                    id = EnvironmentId.Lake;
                    return false;
            }
        }
    }
}
