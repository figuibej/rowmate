using RowMate.Core.Scene;
using RowMate.Core.Scene.Art;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace RowMate.Scene.Quality
{
    // Un nivel de calidad (ADR 018; 06a-escena-lago/02-tech-design §4.2, §9.3; 04-design-notes §15.2). Los valores por defecto son
    // los de la tabla de §9.3 (SceneAssetsBuilder los pone con SetDefaults). Lo que es de la cámara, de nuestra RT, del Volume y de los
    // materiales lo aplica SceneQualityApplier; lo que solo vive en el asset de URP (la distancia de sombras, las cascadas, la
    // resolución del mapa, el MSAA) lo trae el asset RowMate_URP_<nivel> del nivel de QualitySettings (los crea UrpProvisioner).
    [CreateAssetMenu(menuName = "RowMate/Scene/Quality tier", fileName = "Tier")]
    public sealed class QualityTierProfile : ScriptableObject
    {
        [SerializeField] private QualityTier _tier = QualityTier.High;
        [SerializeField] private UniversalRenderPipelineAsset? _urpAsset;
        [SerializeField] private string _qualityLevelName = "High";

        // La resolución de nuestra RT (1 / 0,85 / 0,75): DT6a-16.
        [SerializeField] private float _resolution = 1f;

        // MSAA del asset (4 / 2 / 1) y FXAA en Bajo (espacial, determinista: sin TAA, §5.1).
        [SerializeField] private int _msaa = 4;
        [SerializeField] private bool _fxaa;
        [SerializeField] private bool _hdr = true;

        // Sombras: mapa (no en Bajo: el óvalo del bote), distancia, cascadas, resolución; la fuerza de noche (0,25 / 0: DT6a-19).
        [SerializeField] private bool _shadows = true;
        [SerializeField] private float _shadowDistance = 80f;
        [SerializeField] private int _shadowCascades = 2;
        [SerializeField] private int _shadowResolution = 2048;
        [SerializeField] private float _nightShadowStrength = 0.25f;

        // Post: bloom (Alto y Medio); el disco del sol en HDR (en Bajo, plano: _RmDiskShape.w = 1).
        [SerializeField] private bool _bloom = true;

        // Solo Alto (P-10, DN6a-22): el fondo tenue de estrellas y la capa lejana de nubes.
        [SerializeField] private bool _starField = true;
        [SerializeField] private bool _cloudLayer = true;

        // El relleno: densidad, fin del pasto (60 / 40 / 30 m), escala de los LOD (1 / 1 / 0,7), bosque cercano y bruma baja.
        [SerializeField] private float _fillDensity = 1f;
        [SerializeField] private float _grassEnd = 60f;
        [SerializeField] private float _lodScale = 1f;
        [SerializeField] private bool _nearForest = true;
        [SerializeField] private bool _lowMist = true;

        public QualityTier Tier => _tier;

        public UniversalRenderPipelineAsset? UrpAsset => _urpAsset;

        public string QualityLevelName => _qualityLevelName;

        public double Resolution => _resolution;

        public int Msaa => _msaa;

        public bool Fxaa => _fxaa;

        public bool Hdr => _hdr;

        public bool Shadows => _shadows;

        public double ShadowDistance => _shadowDistance;

        public int ShadowCascades => _shadowCascades;

        public int ShadowResolution => _shadowResolution;

        public double NightShadowStrength => _nightShadowStrength;

        public bool Bloom => _bloom;

        public bool StarField => _starField;

        public bool CloudLayer => _cloudLayer;

        public double FillDensity => _fillDensity;

        public double GrassEnd => _grassEnd;

        public double LodScale => _lodScale;

        public bool NearForest => _nearForest;

        public bool LowMist => _lowMist;

        // Los valores de §9.3 / 04-design-notes §15.2 para un nivel (SceneAssetsBuilder; las pruebas sin assets).
        public void SetDefaults(QualityTier tier, UniversalRenderPipelineAsset? urpAsset, string qualityLevelName)
        {
            _tier = tier;
            _urpAsset = urpAsset;
            _qualityLevelName = qualityLevelName;
            switch (tier)
            {
                case QualityTier.High:
                    Set(1f, 4, false, true, true, 80f, 2, 2048, 0.25f, true, true, true, 1f, 1f, true, true);
                    break;
                case QualityTier.Medium:
                    Set(0.85f, 2, false, true, true, 40f, 1, 1024, 0f, true, false, false, 1f, 1f, true, true);
                    break;
                default:
                    Set(0.75f, 1, true, false, false, 0f, 1, 512, 0f, false, false, false, (float)MaterialArt.LowTierGrassDensity,
                        (float)MaterialArt.LowTierDistanceScale, false, false);
                    break;
            }

            _grassEnd = (float)MaterialArt.GrassEnd(tier);
        }

        private void Set(float resolution, int msaa, bool fxaa, bool hdr, bool shadows, float shadowDistance, int cascades, int shadowResolution,
                         float nightShadow, bool bloom, bool starField, bool cloudLayer, float fillDensity, float lodScale, bool nearForest, bool lowMist)
        {
            _resolution = resolution;
            _msaa = msaa;
            _fxaa = fxaa;
            _hdr = hdr;
            _shadows = shadows;
            _shadowDistance = shadowDistance;
            _shadowCascades = cascades;
            _shadowResolution = shadowResolution;
            _nightShadowStrength = nightShadow;
            _bloom = bloom;
            _starField = starField;
            _cloudLayer = cloudLayer;
            _fillDensity = fillDensity;
            _lodScale = lodScale;
            _nearForest = nearForest;
            _lowMist = lowMist;
        }

        // Un perfil en memoria (pruebas sin assets).
        public static QualityTierProfile CreateDefault(QualityTier tier)
        {
            var profile = CreateInstance<QualityTierProfile>();
            profile.hideFlags = HideFlags.HideAndDontSave;
            profile.SetDefaults(tier, null, tier.ToString());
            return profile;
        }
    }
}
