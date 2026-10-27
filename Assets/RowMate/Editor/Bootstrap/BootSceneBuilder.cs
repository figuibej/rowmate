using RowMate.Bootstrap;
using RowMate.Core.Design;
using RowMate.Presentation.Controls;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace RowMate.EditorTools
{
    // Paso scene.boot del bootstrapper v3 (02-tech-design §1.3, QA-G2-01): crea o repara Scenes/Boot.unity sin YAML a mano y
    // la registra como la ÚNICA escena de EditorBuildSettings.
    //   AppRoot   (GameObject con AppRoot + UIDocument SIN panelSettings: el panel lo pone AppRoot.Awake con una copia)
    //   UiCamera  (Camera SolidColor bg.app, cullingMask 0, datos de cámara de URP)
    // La escena se abre en modo aditivo (nunca cierra la escena del usuario) y se cierra al guardar. Hallazgo de la sonda
    // (§8.3): crear una escena descarga los assets en memoria sin referencias, así que UiAssets se vuelve a cargar DESPUÉS.
    internal static class BootSceneBuilder
    {
        public const string Folder = "Assets/RowMate/Scenes";
        public const string ScenePath = Folder + "/Boot.unity";

        public static BootstrapState.Step Apply(string id)
        {
            FontAssetBuilder.EnsureFolder(Folder);
            var existed = System.IO.File.Exists(ScenePath);
            var alreadyOpen = SceneManager.GetSceneByPath(ScenePath);
            // Con una sola escena sin título y sin cambios (batchmode, proyecto recién abierto) no hay nada que conservar: se
            // reemplaza. Si no, aditiva (Unity no deja crear una escena aditiva con una sin título sin guardar).
            var active = SceneManager.GetActiveScene();
            var replaceUntitled = SceneManager.sceneCount == 1 && string.IsNullOrEmpty(active.path) && (!active.isDirty || Application.isBatchMode);
            UnityEngine.SceneManagement.Scene scene;
            if (alreadyOpen.IsValid() && alreadyOpen.isLoaded)
            {
                scene = alreadyOpen;
            }
            else if (existed)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, replaceUntitled ? OpenSceneMode.Single : OpenSceneMode.Additive);
            }
            else
            {
                scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, replaceUntitled ? NewSceneMode.Single : NewSceneMode.Additive);
            }

            // Después de crear o abrir la escena (la referencia de antes puede quedar "nula de Unity").
            var assets = AssetDatabase.LoadAssetAtPath<UiAssets>(UiAssets.AssetPath);
            if (assets == null)
            {
                throw new System.IO.FileNotFoundException("falta " + UiAssets.AssetPath + " (paso ui.panel)");
            }

            var repaired = "";
            var root = Find(scene, "AppRoot");
            if (root == null)
            {
                root = new GameObject("AppRoot");
                SceneManager.MoveGameObjectToScene(root, scene);
                repaired += " AppRoot";
            }

            if (!root.TryGetComponent<UIDocument>(out var document))
            {
                document = root.AddComponent<UIDocument>();
            }

            if (document.panelSettings != null)
            {
                document.panelSettings = null;
                repaired += " (panel del documento vaciado)";
            }

            if (!root.TryGetComponent<AppRoot>(out var appRoot))
            {
                appRoot = root.AddComponent<AppRoot>();
            }

            var serialized = new SerializedObject(appRoot);
            var property = serialized.FindProperty("_assets");
            if (property == null)
            {
                throw new System.InvalidOperationException("AppRoot no tiene el campo _assets");
            }

            if (property.objectReferenceValue != assets)
            {
                property.objectReferenceValue = assets;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                repaired += " _assets";
            }

            var cameraObject = Find(scene, "UiCamera");
            if (cameraObject == null)
            {
                cameraObject = new GameObject("UiCamera");
                SceneManager.MoveGameObjectToScene(cameraObject, scene);
                repaired += " UiCamera";
            }

            if (!cameraObject.TryGetComponent<Camera>(out var camera))
            {
                camera = cameraObject.AddComponent<Camera>();
            }

            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = TokenColors.ToColor(Tokens.BgApp);
            camera.cullingMask = 0;
            if (!cameraObject.TryGetComponent<UniversalAdditionalCameraData>(out _))
            {
                cameraObject.AddComponent<UniversalAdditionalCameraData>();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new System.InvalidOperationException("no se pudo guardar " + ScenePath);
            }

            if (!(alreadyOpen.IsValid() && alreadyOpen.isLoaded) && SceneManager.sceneCount > 1)
            {
                EditorSceneManager.CloseScene(scene, true);
            }

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            return BootstrapState.Step.Ok(id, (existed ? "reparada" : "creada") + (repaired.Length > 0 ? ":" + repaired : "") + "; única escena del build");
        }

        private static GameObject? Find(UnityEngine.SceneManagement.Scene scene, string name)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == name)
                {
                    return root;
                }
            }

            return null;
        }
    }
}
