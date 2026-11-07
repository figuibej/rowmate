using System;
using System.Collections.Generic;
using UnityEditor;

namespace RowMate.EditorTools
{
    // El import de los FBX de Mixamo (07-remero-palada/02-tech-design §3.2; DT7-04; ASSET_SLOTS §2.5): todo FBX bajo
    // Assets/ThirdParty/Mixamo/ entra como Humanoid con el avatar del propio modelo, sin Optimize Game Objects (RowerRigDriver mueve los
    // Transform de los huesos), sin animaciones, cámaras ni luces (la palada es nuestra, por código), con Read/Write encendido (lo copia
    // RowerOutfitBaker) y sin materiales importados (el remero usa el shader RowMate/Scene/Rower). El usuario solo copia el archivo; si
    // el FBX ya estaba importado con otros ajustes (alguien abrió Unity antes), el bootstrapper lo reimporta (ReimportAll).
    public sealed class MixamoImportPostprocessor : AssetPostprocessor
    {
        public const string Folder = "Assets/ThirdParty/Mixamo/";

        public static bool Applies(string assetPath)
        {
            return assetPath != null && assetPath.StartsWith(Folder, StringComparison.Ordinal)
                                     && assetPath.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase);
        }

        private void OnPreprocessModel()
        {
            if (!Applies(assetPath) || !(assetImporter is ModelImporter importer))
            {
                return;
            }

            Configure(importer);
        }

        // Los ajustes de DT7-04 (true = alguno cambió).
        public static bool Configure(ModelImporter importer)
        {
            var changed = !IsConfigured(importer);
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.optimizeGameObjects = false;
            importer.importAnimation = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.isReadable = true;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            return changed;
        }

        public static bool IsConfigured(ModelImporter importer)
        {
            return importer.animationType == ModelImporterAnimationType.Human && importer.avatarSetup == ModelImporterAvatarSetup.CreateFromThisModel
                   && !importer.optimizeGameObjects && !importer.importAnimation && !importer.importCameras && !importer.importLights
                   && importer.isReadable && importer.materialImportMode == ModelImporterMaterialImportMode.None;
        }

        // Los FBX de Mixamo que hay en esta máquina (rutas del proyecto).
        public static List<string> FindModels()
        {
            var result = new List<string>();
            if (!AssetDatabase.IsValidFolder(Folder.TrimEnd('/')))
            {
                return result;
            }

            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { Folder.TrimEnd('/') }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (Applies(path))
                {
                    result.Add(path);
                }
            }

            result.Sort(StringComparer.Ordinal);
            return result;
        }

        // Reimporta los que quedaron con otros ajustes (el primer import antes del postprocesador). Devuelve cuántos.
        public static int ReimportAll()
        {
            var count = 0;
            foreach (var path in FindModels())
            {
                if (AssetImporter.GetAtPath(path) is ModelImporter importer && !IsConfigured(importer))
                {
                    Configure(importer);
                    importer.SaveAndReimport();
                    count++;
                }
            }

            return count;
        }
    }
}
