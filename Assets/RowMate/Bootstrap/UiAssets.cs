using UnityEngine;
using UnityEngine.UIElements;

namespace RowMate.Bootstrap
{
    // Referencias que necesita un player para armar la UI (02-tech-design §1.1, §1.3 paso ui.panel): el PanelSettings del
    // proyecto (tema RowMateTheme.tss; AppRoot y UiTestHost usan SIEMPRE una copia, R2-05), los dos catálogos de textos y el
    // material del presentador por blit (solo si S-B1 lo eligiera). Lo crea UiAssetsBuilder en Settings/UI/UiAssets.asset y
    // BootSceneBuilder lo asigna a AppRoot._assets.
    [CreateAssetMenu(menuName = "RowMate/UI Assets", fileName = "UiAssets")]
    public sealed class UiAssets : ScriptableObject
    {
        public const string AssetPath = "Assets/RowMate/Settings/UI/UiAssets.asset";
        public const string PanelPath = "Assets/RowMate/Settings/UI/RowMatePanel.asset";
        public const string ThemePath = "Assets/RowMate/Presentation/Styles/RowMateTheme.tss";
        public const string StringsEsPath = "Assets/RowMate/Localization/strings.es.json";
        public const string StringsEnPath = "Assets/RowMate/Localization/strings.en.json";

        [SerializeField] private PanelSettings? _panel;
        [SerializeField] private TextAsset? _stringsEs;
        [SerializeField] private TextAsset? _stringsEn;
        [SerializeField] private Material? _presentMaterial;

        public PanelSettings? Panel => _panel;

        public TextAsset? StringsEs => _stringsEs;

        public TextAsset? StringsEn => _stringsEn;

        public Material? PresentMaterial => _presentMaterial;

        public bool IsComplete => _panel != null && _stringsEs != null && _stringsEn != null;

        // Para UiAssetsBuilder (Editor): asigna todo de una vez.
        public void Set(PanelSettings panel, TextAsset stringsEs, TextAsset stringsEn, Material? presentMaterial)
        {
            _panel = panel;
            _stringsEs = stringsEs;
            _stringsEn = stringsEn;
            _presentMaterial = presentMaterial;
        }
    }
}
