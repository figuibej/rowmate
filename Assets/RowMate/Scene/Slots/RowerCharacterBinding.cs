using UnityEngine;

namespace RowMate.Scene.Slots
{
    // El binding del personaje del remero (07-remero-palada/02-tech-design §1.2, §3.2; DT7-13): la referencia al FBX de Mixamo que
    // encontró RowerCharacterBindingBuilder (Editor) en las rutas esperadas del slot. Vive en Bootstrap/Generated/Resources/ (ignorado
    // por git, como StravaSecrets.g.cs): solo existe en una máquina que tiene el FBX, y un build hecho ahí lleva el personaje. La escena
    // lo carga con Resources.Load (sin binding: el maniquí, sin aviso).
    public sealed class RowerCharacterBinding : ScriptableObject
    {
        public const string ResourceName = "RowerCharacterBinding";
        public const string AssetPath = "Assets/RowMate/Bootstrap/Generated/Resources/" + ResourceName + ".asset";

        [SerializeField] private GameObject? _model;
        [SerializeField] private string _sourcePath = "";

        public GameObject? Model => _model;

        public string SourcePath => _sourcePath;

        public void Set(GameObject? model, string sourcePath)
        {
            _model = model;
            _sourcePath = sourcePath ?? "";
        }

        // El binding de la app (null si esta máquina no tenía un FBX de Mixamo al armar).
        public static RowerCharacterBinding? Load()
        {
            return Resources.Load<RowerCharacterBinding>(ResourceName);
        }
    }
}
