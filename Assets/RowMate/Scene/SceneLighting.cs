using System;
using RowMate.Core.Scene;
using RowMate.Core.Scene.Art;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace RowMate.Scene
{
    // La luz de la escena (06a-escena-lago/02-tech-design §3.6 "Luz" y "Post"; §7.5 "Globales"; 04-design-notes §10, "Cambios por
    // implementación de WP-D1" §2 y §3): en cada cuadro, los globales de los shaders (la paleta en lineal con SetGlobalColor, que pasa
    // de sRGB a lineal; los *Srgb y los multiplicadores del legacy con SetGlobalVector, sin convertir), la luz direccional (dirección e
    // intensidad del astro de CelestialPlacement; la fuerza de las sombras del nivel y de la hora, 0 de noche en Medio SIN apagar el
    // mapa: DT6a-19), el ambiente Trilight de URP para lo de terceros (solo si cambió más de 1/1 024) y el ÚNICO Volume de la escena
    // (DT6a-10): Tonemapping (el modo de HourLookTable), Color Adjustments, White Balance, Bloom (Alto y Medio) y Vignette con los valores de PostLook (la
    // hora y el velo de pausa). Sin grano ni TAA (§5.1). Sin asignaciones.
    public sealed class SceneLighting : IDisposable
    {
        private readonly Light _light;
        private readonly Volume _volume;
        private readonly VolumeProfile _profile;
        private readonly Tonemapping _tonemapping;
        private readonly ColorAdjustments _color;
        private readonly WhiteBalance _whiteBalance;
        private readonly Bloom _bloom;
        private readonly Vignette _vignette;
        private Color _lastSky = new Color(-1, -1, -1);
        private Color _lastEquator = new Color(-1, -1, -1);
        private Color _lastGround = new Color(-1, -1, -1);

        public SceneLighting(Transform parent, int layer)
        {
            var lightObject = new GameObject("scene-key-light") { hideFlags = HideFlags.DontSave, layer = layer };
            lightObject.transform.SetParent(parent, false);
            _light = lightObject.AddComponent<Light>();
            _light.type = LightType.Directional;
            _light.shadows = LightShadows.Soft;
            _light.cullingMask = 1 << layer;
            _light.renderMode = LightRenderMode.ForcePixel;
            var lightData = lightObject.AddComponent<UniversalAdditionalLightData>();
            lightData.usePipelineSettings = true;

            var volumeObject = new GameObject("scene-volume") { hideFlags = HideFlags.DontSave, layer = layer };
            volumeObject.transform.SetParent(parent, false);
            _volume = volumeObject.AddComponent<Volume>();
            _volume.isGlobal = true;
            _volume.priority = 100;
            _profile = ScriptableObject.CreateInstance<VolumeProfile>();
            _profile.name = "RowMate.SceneVolume";
            _profile.hideFlags = HideFlags.HideAndDontSave;
            _tonemapping = _profile.Add<Tonemapping>(true);
            // El modo es del Designer (HourLookTable.NeutralTonemapping; WP-D2: None).
            _tonemapping.mode.Override(HourLookTable.NeutralTonemapping ? TonemappingMode.Neutral : TonemappingMode.None);
            _color = _profile.Add<ColorAdjustments>(true);
            _whiteBalance = _profile.Add<WhiteBalance>(true);
            _bloom = _profile.Add<Bloom>(true);
            _bloom.highQualityFiltering.Override(false);
            // El bloom a media resolución y con 2 pasos de la cadena (04-design-notes §15.1: el disco HDR y las ventanas; con 6 la cadena
            // sumaba ≈ 12 pasadas más sin cambio visible a esta escala). Ver "Cambios por implementación de WP-B" (B6a-05).
            _bloom.downscale.Override(BloomDownscaleMode.Half);
            _bloom.maxIterations.Override(2);
            _bloom.dirtIntensity.Override(0f);
            _vignette = _profile.Add<Vignette>(true);
            _vignette.color.Override(Color.black);
            // Sin grano, sin aberración ni distorsión (§5.1): se anulan por si el perfil por defecto del proyecto los trae.
            var grain = _profile.Add<FilmGrain>(true);
            grain.intensity.Override(0f);
            var chromatic = _profile.Add<ChromaticAberration>(true);
            chromatic.intensity.Override(0f);
            var lens = _profile.Add<LensDistortion>(true);
            lens.intensity.Override(0f);
            var motion = _profile.Add<MotionBlur>(true);
            motion.intensity.Override(0f);
            _volume.sharedProfile = _profile;
        }

        public Light Light => _light;

        public Volume Volume => _volume;

        public VolumeProfile Profile => _profile;

        // El bloom del nivel (Alto y Medio) y el HDR del disco (en Bajo, plano).
        public bool BloomEnabled { get; set; } = true;

        public bool DiskHdr { get; set; } = true;

        public bool LowMistEnabled { get; set; } = true;

        public double NightShadowStrength { get; set; } = 0.25;

        public bool HighTier { get; set; } = true;

        public double Density { get; set; } = 1;

        // La ganancia de lo emisivo (1,6; 1 en la pasada de contrato, RenderWithoutPostForTest).
        public double EmissiveGain { get; set; } = SkyArt.EmissiveGain;

        public void Apply(in SceneFrame frame)
        {
            var p = frame.Palette;
            Shader.SetGlobalFloat(SceneShaderIds.SceneTime, (float)frame.SceneTime);
            Shader.SetGlobalColor(SceneShaderIds.PaletteSkyTop, Srgb(p.SkyTop));
            Shader.SetGlobalColor(SceneShaderIds.PaletteSkyHorizon, Srgb(p.SkyHorizon));
            Shader.SetGlobalColor(SceneShaderIds.PaletteWaterNear, Srgb(p.WaterNear));
            Shader.SetGlobalColor(SceneShaderIds.PaletteWaterFar, Srgb(p.WaterFar));
            Shader.SetGlobalColor(SceneShaderIds.PaletteSunColor, Srgb(p.SunColor));
            Shader.SetGlobalColor(SceneShaderIds.PaletteSunGlow, Srgb(p.SunGlow));
            Shader.SetGlobalColor(SceneShaderIds.PaletteFog, Srgb(p.Fog));
            Shader.SetGlobalFloat(SceneShaderIds.Ambient, (float)p.Ambient);
            var look = frame.Look;
            Shader.SetGlobalVector(SceneShaderIds.AmbientSky, Raw(look.AmbientSky));
            Shader.SetGlobalVector(SceneShaderIds.AmbientEquator, Raw(look.AmbientEquator));
            Shader.SetGlobalVector(SceneShaderIds.AmbientGround, Raw(look.AmbientGround));
            var fog = MaterialArt.FogParams;
            Shader.SetGlobalVector(SceneShaderIds.FogParams, new Vector4((float)fog[0], (float)fog[1], (float)fog[2], (float)fog[3]));
            Shader.SetGlobalFloat(SceneShaderIds.LowMist, LowMistEnabled ? (float)frame.LowMist : 0f);
            Shader.SetGlobalFloat(SceneShaderIds.Golden, (float)frame.Golden);

            var sky = frame.Celestial;
            SceneMath.Direction(sky.DiskAzimuthDeg, sky.DiskElevationDeg, out var dx, out var dy, out var dz);
            Shader.SetGlobalVector(SceneShaderIds.DiskDir, new Vector4((float)dx, (float)dy, (float)dz, 0));
            Shader.SetGlobalVector(SceneShaderIds.DiskParams,
                new Vector4((float)sky.DiskVisibility, sky.DiskIsMoon ? 1f : 0f, (float)frame.Moon.Illumination, (float)frame.Moon.PhaseAngle));
            Shader.SetGlobalVector(SceneShaderIds.DiskShape,
                new Vector4((float)frame.DiskRadiusRad, (float)frame.HaloRadiusRad, frame.SouthernMirror ? -1f : 1f, DiskHdr ? (float)frame.DiskHdr : 1f));
            SceneMath.Direction(sky.LightAzimuthDeg, sky.LightElevationDeg, out var lx, out var ly, out var lz);
            Shader.SetGlobalVector(SceneShaderIds.LightDir, new Vector4((float)lx, (float)ly, (float)lz, (float)sky.LightIntensity));

            var window = frame.Window;
            Shader.SetGlobalFloat(SceneShaderIds.HorizonY, (float)window.HorizonY);
            Shader.SetGlobalFloat(SceneShaderIds.FocalDp, (float)window.Focal);
            Shader.SetGlobalVector(SceneShaderIds.ViewDp, new Vector4((float)window.Width, (float)window.Height, 0, 0));
            Shader.SetGlobalFloat(SceneShaderIds.Density, (float)Density);
            // La fase de cinta de d en double (Q-68): (frac(d/1,3); frac(d/7)).
            var d = frame.Distance;
            Shader.SetGlobalVector(SceneShaderIds.ZonePhase, new Vector4((float)Frac(d / MaterialArt.NoiseFineMeters), (float)Frac(d / MaterialArt.NoiseCoarseMeters), 0, 0));
            var haze = SceneColor.Lerp(p.SkyHorizon, p.Fog, 0.5);
            Shader.SetGlobalVector(SceneShaderIds.SkyTopSrgb, Raw(p.SkyTop));
            Shader.SetGlobalVector(SceneShaderIds.SkyHorizonSrgb, Raw(p.SkyHorizon));
            Shader.SetGlobalVector(SceneShaderIds.HazeSrgb, Raw(haze));
            Shader.SetGlobalFloat(SceneShaderIds.EmissiveGain, (float)EmissiveGain);

            // La luz direccional: hacia el astro (la dirección de la luz va del astro a la escena).
            _light.transform.rotation = Quaternion.LookRotation(new Vector3((float)-lx, (float)-ly, (float)-lz), Vector3.up);
            _light.color = Srgb(look.KeyColor);
            _light.intensity = (float)Math.Max(0, sky.LightIntensity);
            var strength = HighTier ? look.ShadowStrengthHigh : look.ShadowStrengthMedium;
            if (sky.IsNight)
            {
                strength = Math.Min(strength, NightShadowStrength);
            }

            _light.shadowStrength = (float)SceneMath.Clamp01(strength);

            // El ambiente de URP (lo de terceros), solo si cambió.
            var skyAmbient = Srgb(look.AmbientSky);
            var equator = Srgb(look.AmbientEquator);
            var ground = Srgb(look.AmbientGround);
            if (Changed(skyAmbient, _lastSky) || Changed(equator, _lastEquator) || Changed(ground, _lastGround))
            {
                RenderSettings.ambientMode = AmbientMode.Trilight;
                RenderSettings.ambientSkyColor = skyAmbient;
                RenderSettings.ambientEquatorColor = equator;
                RenderSettings.ambientGroundColor = ground;
                _lastSky = skyAmbient;
                _lastEquator = equator;
                _lastGround = ground;
            }

            // El Volume: la hora con el velo (HourLighting.Compose, en Core).
            var post = frame.Post;
            _color.postExposure.Override((float)post.ExposureEv);
            _color.contrast.Override((float)post.Contrast);
            _color.saturation.Override((float)post.Saturation);
            _whiteBalance.temperature.Override((float)post.WhiteBalanceTemp);
            _whiteBalance.tint.Override((float)post.WhiteBalanceTint);
            _bloom.active = BloomEnabled;
            _bloom.intensity.Override(BloomEnabled ? (float)post.BloomIntensity : 0f);
            _bloom.threshold.Override((float)post.BloomThreshold);
            _vignette.intensity.Override((float)post.VignetteIntensity);
            _vignette.smoothness.Override((float)post.VignetteSmoothness);
        }

        public void Dispose()
        {
            if (_volume != null)
            {
                _volume.sharedProfile = null!;
            }

            MeshUpload.Destroy(_profile);
        }

        private static bool Changed(Color a, Color b)
        {
            const float Step = 1f / 1024f;
            return Math.Abs(a.r - b.r) > Step || Math.Abs(a.g - b.g) > Step || Math.Abs(a.b - b.b) > Step;
        }

        private static double Frac(double x)
        {
            return x - Math.Floor(x);
        }

        // Un color sRGB de Core como Color de Unity (SetColor/SetGlobalColor lo pasan a lineal en un proyecto Linear).
        internal static Color Srgb(in SceneColor c)
        {
            return new Color((float)c.R, (float)c.G, (float)c.B, 1f);
        }

        // Sin conversión (SetVector).
        internal static Vector4 Raw(in SceneColor c)
        {
            return new Vector4((float)c.R, (float)c.G, (float)c.B, 1f);
        }
    }
}
