using System.IO;
using RowMate.Scene.Slots;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace RowMate.EditorTools
{
    // El binding del personaje (07-remero-palada/02-tech-design §1.3, §3.2; DT7-13, QA7-G-04): busca las rutas esperadas del slot
    // rower.character (el realista antes que Y Bot) con AssetDatabase y escribe Bootstrap/Generated/Resources/RowerCharacterBinding.asset
    // (ignorado por git) con el FBX encontrado, o lo borra si no hay ninguno. Corre en el bootstrapper (scene.assets) y antes de cada
    // build: un build lleva Y Bot solo si está en esa máquina. Nada de Mixamo entra al repo.
    public sealed class RowerCharacterBindingBuilder : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            Refresh();
        }

        [MenuItem("RowMate/Slots/Rehacer el binding del remero")]
        public static void RefreshFromMenu()
        {
            Debug.Log("[SLOT] " + SlotIdsRower + ": " + Refresh());
        }

        private const string SlotIdsRower = "rower.character";

        // Escribe o borra el binding. Devuelve qué hizo (sin rutas de la PC: solo las del proyecto).
        public static string Refresh()
        {
            var slot = AssetDatabase.LoadAssetAtPath<RowerCharacterSlot>(SceneAssetsBuilder.SlotPath(SlotIdsRower));
            var paths = slot != null ? slot.ExpectedPaths : RowerCharacterSlot.DefaultPaths;
            GameObject? model = null;
            var found = "";
            foreach (var path in paths)
            {
                model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (model != null)
                {
                    found = path;
                    break;
                }
            }

            var existing = AssetDatabase.LoadAssetAtPath<RowerCharacterBinding>(RowerCharacterBinding.AssetPath);
            if (model == null)
            {
                if (existing != null)
                {
                    AssetDatabase.DeleteAsset(RowerCharacterBinding.AssetPath);
                    return "sin FBX de Mixamo: binding borrado (la escena usa el maniquí)";
                }

                return "sin FBX de Mixamo: sin binding (la escena usa el maniquí)";
            }

            if (existing == null)
            {
                EnsureFolder(Path.GetDirectoryName(RowerCharacterBinding.AssetPath)!.Replace('\\', '/'));
                existing = ScriptableObject.CreateInstance<RowerCharacterBinding>();
                existing.name = RowerCharacterBinding.ResourceName;
                existing.Set(model, found);
                AssetDatabase.CreateAsset(existing, RowerCharacterBinding.AssetPath);
            }
            else if (existing.Model != model || existing.SourcePath != found)
            {
                existing.Set(model, found);
                EditorUtility.SetDirty(existing);
            }

            AssetDatabase.SaveAssets();
            return "binding con " + found;
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            var parent = Path.GetDirectoryName(folder)!.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }
    }
}
