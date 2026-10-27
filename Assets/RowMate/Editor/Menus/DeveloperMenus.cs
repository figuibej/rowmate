using RowMate.Platform;
using UnityEditor;

namespace RowMate.EditorTools
{
    // Simulaciones de desarrollo del Editor (02-tech-design §1.1, §3.10): reducir movimiento, letra 1,3× e idioma, guardadas en
    // EditorPrefs (solo en esta PC, nunca en un build). Las leen los proveedores de Platform (DeveloperOverrides) al arrancar
    // y al recuperar el foco.
    public static class DeveloperMenus
    {
        private const string ReduceMotionMenu = "RowMate/Simular/Reducir movimiento";
        private const string TextScaleMenu = "RowMate/Simular/Letra 1,3×";
        private const string LanguageEsMenu = "RowMate/Simular/Idioma/es-AR";
        private const string LanguageEnMenu = "RowMate/Simular/Idioma/en-US";
        private const string LanguageSystemMenu = "RowMate/Simular/Idioma/Del sistema";

        [MenuItem(ReduceMotionMenu)]
        private static void ToggleReduceMotion()
        {
            EditorPrefs.SetBool(DeveloperOverrides.ReduceMotionKey, !EditorPrefs.GetBool(DeveloperOverrides.ReduceMotionKey, false));
        }

        [MenuItem(ReduceMotionMenu, true)]
        private static bool ToggleReduceMotionCheck()
        {
            Menu.SetChecked(ReduceMotionMenu, EditorPrefs.GetBool(DeveloperOverrides.ReduceMotionKey, false));
            return true;
        }

        [MenuItem(TextScaleMenu)]
        private static void ToggleTextScale()
        {
            var on = EditorPrefs.GetFloat(DeveloperOverrides.TextScaleKey, 0f) > 0;
            EditorPrefs.SetFloat(DeveloperOverrides.TextScaleKey, on ? 0f : 1.3f);
        }

        [MenuItem(TextScaleMenu, true)]
        private static bool ToggleTextScaleCheck()
        {
            Menu.SetChecked(TextScaleMenu, EditorPrefs.GetFloat(DeveloperOverrides.TextScaleKey, 0f) > 0);
            return true;
        }

        [MenuItem(LanguageEsMenu)]
        private static void LanguageEs()
        {
            EditorPrefs.SetString(DeveloperOverrides.LanguagesKey, "es-AR");
        }

        [MenuItem(LanguageEnMenu)]
        private static void LanguageEn()
        {
            EditorPrefs.SetString(DeveloperOverrides.LanguagesKey, "en-US");
        }

        [MenuItem(LanguageSystemMenu)]
        private static void LanguageSystem()
        {
            EditorPrefs.DeleteKey(DeveloperOverrides.LanguagesKey);
        }
    }
}
