using RowMate.Core.Scene;
using RowMate.Core.Scene.Water;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace RowMate.Scene
{
    // El agua avanzada (F-SCN-12; 06b-agua-avanzada/02-tech-design §4.1, §5; DT6b-03 a -05, -22): la malla en anillos de WaterRingMesh
    // (Core) con la copia del material WaterAdvanced en la variante del nivel (RM_WATER_HIGH / Medio sin palabra / RM_WATER_LITE). Por
    // cuadro, lo del escenario y de la hora en el material (las bandas, el aspecto, la huella del casco, la silueta del horizonte y, para
    // el salto degradado del fundido, lo mismo que WaterSurface le pone a la degradada). La malla queda quieta en cinta (DT6b-27, ronda del
    // salto: WaterVertexField.MeshOffsetZ; antes se anclaba cada 16 m y el agua saltaba). En Medio, el perfil del horizonte 256 × 1 se rehace cada 50 m o al cambiar
    // de escenario, sin asignar. Apply no asigna.
    public sealed class AdvancedWaterSurface
    {
        private readonly Material _material;
        private readonly Texture2D _profileTexture;
        private readonly byte[] _profileBytes = new byte[HorizonProfile.Width];
        private HorizonProfile? _profile;
        private double _profileDistance = double.NaN;
        private QualityTier _tier;
        private bool _openRight;

        public AdvancedWaterSurface(Transform parent, int layer, Mesh mesh, Material material, Texture? normal, QualityTier tier)
        {
            _material = material;
            Renderer = SceneObjects.Create("scene-water-advanced", parent, layer, mesh, material, false, false);
            Renderer.enabled = false;
            Renderer.transform.localPosition = new Vector3(0, 0, (float)WaterVertexField.MeshOffsetZ(0));
            Filter = Renderer.GetComponent<MeshFilter>();
            _profileTexture = new Texture2D(HorizonProfile.Width, 1, GraphicsFormat.R8_UNorm, TextureCreationFlags.None)
            {
                name = "scene-water-horizon-profile",
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            _material.SetTexture(SceneShaderIds.HorizonProfileTex, _profileTexture);
            if (normal != null)
            {
                _material.SetTexture(SceneShaderIds.WaterNormal, normal);
            }

            SetTier(tier);
        }

        public MeshRenderer Renderer { get; }

        public MeshFilter Filter { get; }

        public Material Material => _material;

        public Texture2D ProfileTexture => _profileTexture;

        public QualityTier Tier => _tier;

        // La variante del nivel (las palabras clave del material: las tres están en la colección de variantes).
        public void SetTier(QualityTier tier)
        {
            _tier = tier;
            Keywords(_material, tier);
        }

        public static void Keywords(Material material, QualityTier tier)
        {
            if (tier == QualityTier.High)
            {
                material.EnableKeyword(SceneShaderIds.WaterHighKeyword);
            }
            else
            {
                material.DisableKeyword(SceneShaderIds.WaterHighKeyword);
            }

            if (tier == QualityTier.Low)
            {
                material.EnableKeyword(SceneShaderIds.WaterLiteKeyword);
            }
            else
            {
                material.DisableKeyword(SceneShaderIds.WaterLiteKeyword);
            }
        }

        // DT6b-22 (G6b-01): hay soporte solo si el shader corre en este equipo Y el SubShader activo es el avanzado (sin Fallback: si no, la
        // degradada adentro daría isSupported en un equipo que no puede).
        public static bool IsSupported(Material material)
        {
            return material != null && material.shader != null && material.shader.isSupported
                   && material.GetTag(SceneShaderIds.WaterTag, false) == SceneShaderIds.WaterTagValue;
        }

        // El peso del reflejo planar (Alto) o de la silueta (Medio): LowerReflection lo baja a 0 en 0,6 s.
        public void SetReflectionMix(double mix)
        {
            _material.SetFloat(SceneShaderIds.ReflectionMix, (float)mix);
        }

        public void Apply(in SceneFrame frame)
        {
            var water = frame.Water;
            var p = water.Profile;
            // Lo de la degradada (el salto del fundido, DT6b-12): lo mismo que WaterSurface.
            _material.SetColor(SceneShaderIds.WaterBandTop, SceneLighting.Srgb(frame.WaterTop));
            _material.SetColor(SceneShaderIds.WaterBandMid, SceneLighting.Srgb(frame.WaterMid));
            _material.SetColor(SceneShaderIds.WaterBandBottom, SceneLighting.Srgb(frame.WaterBottom));
            _material.SetVector(SceneShaderIds.Shimmer,
                new Vector4((float)frame.WaterPhase, (float)frame.ShimmerCenterX, (float)frame.Celestial.LegacyStrength, (float)frame.ShimmerVisibility));
            _material.SetColor(SceneShaderIds.ShimmerColor, SceneLighting.Srgb(frame.Palette.SunColor));
            var openRight = frame.Environment == EnvironmentId.Coast;
            if (openRight != _openRight)
            {
                _openRight = openRight;
                var extras = _material.GetVector(SceneShaderIds.WaterExtras);
                extras.w = openRight ? 1f : 0f;
                _material.SetVector(SceneShaderIds.WaterExtras, extras);
            }

            // El escenario (WaterProfiles, WaterLookArt).
            _material.SetVector(SceneShaderIds.WaterBands, new Vector4((float)p.B2.Tile, (float)p.B3.Tile, (float)p.B2.Strength, (float)p.B3.Strength));
            _material.SetVector(SceneShaderIds.WaterLook, new Vector4((float)p.Roughness, (float)p.Distortion, (float)p.B1Slope, p.Swell ? 1f : 0f));
            _material.SetVector(SceneShaderIds.HullFootprint,
                new Vector4((float)water.HullStartRelZ, (float)water.HullEndRelZ, (float)water.HullHalfWidth, 0));
            _material.SetColor(SceneShaderIds.HorizonReflectTone, SceneLighting.Srgb(frame.HorizonFar));
            _material.SetFloat(SceneShaderIds.HorizonReflectTan, (float)(frame.HorizonPeakTan * frame.HorizonFarScale));

            // WP-D: la banda de orilla reflejada (00-arte-agua §4.2): x y alto de cada lado (sin orilla: lejísimos y sin alto), con el tono
            // × la luz de la hora que da Core (WaterFrame); el carácter: (calma mínima; turbidez del río; −; −).
            _material.SetVector(SceneShaderIds.ShoreGeometry, new Vector4(BandX(p.Left.X), BandHeight(p.Left.X, p.Left.Height), BandX(p.Right.X), BandHeight(p.Right.X, p.Right.Height)));
            _material.SetVector(SceneShaderIds.ShoreLowGeometry,
                new Vector4(BandX(p.Left.LowX), BandHeight(p.Left.LowX, p.Left.LowHeight), BandX(p.Right.LowX), BandHeight(p.Right.LowX, p.Right.LowHeight)));
            _material.SetColor(SceneShaderIds.ShoreLeftTone, SceneLighting.Srgb(water.LeftBand));
            _material.SetColor(SceneShaderIds.ShoreRightTone, SceneLighting.Srgb(water.RightBand));
            _material.SetColor(SceneShaderIds.ShoreLowLeftTone, SceneLighting.Srgb(water.LeftLowBand));
            _material.SetColor(SceneShaderIds.ShoreLowRightTone, SceneLighting.Srgb(water.RightLowBand));
            _material.SetVector(SceneShaderIds.WaterCharacter, new Vector4((float)p.CalmMin, frame.Environment == EnvironmentId.River ? 1f : 0f, 0, 0));
            if (_tier == QualityTier.Medium)
            {
                RefreshProfile(frame.Environment, frame.Distance);
            }
        }

        // Una banda ausente (NaN: la costa a la derecha, o sin tramo bajo) queda a 10 km y sin alto: el rayo nunca la toca.
        private static float BandX(double x)
        {
            return double.IsNaN(x) ? 1e4f : (float)x;
        }

        private static float BandHeight(double x, double height)
        {
            return double.IsNaN(x) ? 0f : (float)height;
        }

        // El perfil del horizonte (PA6b-05): al cambiar de escenario o cada 50 m (HorizonProfile.RebuildEveryMeters).
        public void RefreshProfile(EnvironmentId environment, double distance)
        {
            if (_profile == null || _profile.Environment != environment)
            {
                _profile = new HorizonProfile(environment);
                _profileDistance = double.NaN;
            }

            if (!double.IsNaN(_profileDistance) && System.Math.Abs(distance - _profileDistance) < HorizonProfile.RebuildEveryMeters)
            {
                return;
            }

            _profileDistance = distance;
            _profile.Fill(distance, _profileBytes);
            _profileTexture.SetPixelData(_profileBytes, 0);
            _profileTexture.Apply(false, false);
        }

        public void Destroy()
        {
            MeshUpload.Destroy(_profileTexture);
        }
    }
}
