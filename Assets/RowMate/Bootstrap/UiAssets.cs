using UnityEngine;
using UnityEngine.UIElements;
using FontAsset = UnityEngine.TextCore.Text.FontAsset;

namespace RowMate.Bootstrap
{
    // Referencias que necesita un player para armar la UI (02-tech-design §1.1, §1.3 paso ui.panel de la It. 2): el
    // PanelSettings del proyecto (tema RowMateTheme.tss; AppRoot y UiTestHost usan SIEMPRE una copia, R2-05), los dos catálogos
    // de textos, el material del presentador por blit (solo si S-B1 lo eligiera) y, desde la It. 3, los FontAsset de RESPALDO
    // del panel (RuntimePanelFactory, QA-G3-12): los íconos de contorno (el glifo trending_up del subtítulo) y Noto Emoji
    // (P-09). Lo crea UiAssetsBuilder en Settings/UI/UiAssets.asset y BootSceneBuilder lo asigna a AppRoot._assets; UiTestHost
    // y el arnés leen el MISMO asset (UiAssetsLocator.Assets).
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
        [SerializeField] private FontAsset? _iconsOutlineFallback;
        [SerializeField] private FontAsset? _emojiFallback;

        public PanelSettings? Panel => _panel;

        public TextAsset? StringsEs => _stringsEs;

        public TextAsset? StringsEn => _stringsEn;

        public Material? PresentMaterial => _presentMaterial;

        // Respaldo 1: RowMateIcons-Outline-SDF (sin él, el subtítulo dibuja □ en lugar del ícono de tendencia).
        public FontAsset? IconsOutlineFallback => _iconsOutlineFallback;

        // Respaldo 2: Noto Emoji (sin él, un emoji del usuario sale □ y el resto del texto no se corre).
        public FontAsset? EmojiFallback => _emojiFallback;

        public bool IsComplete => _panel != null && _stringsEs != null && _stringsEn != null;

        // Para UiAssetsBuilder (Editor): asigna todo de una vez.
        public void Set(PanelSettings panel, TextAsset stringsEs, TextAsset stringsEn, Material? presentMaterial)
        {
            _panel = panel;
            _stringsEs = stringsEs;
            _stringsEn = stringsEn;
            _presentMaterial = presentMaterial;
        }

        // Para UiAssetsBuilder (Editor, It. 3): los respaldos de fuente del panel.
        public void SetFallbacks(FontAsset? iconsOutline, FontAsset? emoji)
        {
            _iconsOutlineFallback = iconsOutline;
            _emojiFallback = emoji;
        }
    }
}
