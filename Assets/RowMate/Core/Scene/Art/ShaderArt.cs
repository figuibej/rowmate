using System;
using System.Collections.Generic;

namespace RowMate.Core.Scene.Art
{
    // Un shader de la escena (06a-escena-lago/02-tech-design §7.5): su nombre (RowMate/Scene/<Name>), su cola, su presupuesto de
    // fragmento en el nivel Medio (instrucciones de cálculo del código compilado para Direct3D 11 fuera de los bloques condicionales,
    // sin las sombras de URP ni lo que es solo del nivel Alto) y de texturas, lo que puede costar un bloque condicional que solo
    // corre en una parte chica de la pantalla (el disco y el halo del astro: MaxBranchAlu), si se instancia, y las propiedades del
    // material y de instancia del contrato con WP-B1.
    public readonly struct SceneShaderSpec
    {
        public SceneShaderSpec(string name, int queue, int maxAlu, int maxTextures, bool instanced, string[] properties, string[] instanceProperties,
                               int maxBranchAlu = 0)
        {
            Name = name;
            Queue = queue;
            MaxAlu = maxAlu;
            MaxTextures = maxTextures;
            Instanced = instanced;
            Properties = properties;
            InstanceProperties = instanceProperties;
            MaxBranchAlu = maxBranchAlu;
        }

        public string Name { get; }
        public int Queue { get; }
        public int MaxAlu { get; }
        public int MaxTextures { get; }
        public int MaxBranchAlu { get; }
        public bool Instanced { get; }
        public string[] Properties { get; }
        public string[] InstanceProperties { get; }

        public string FullName => ShaderArt.Prefix + Name;
    }

    // El contrato de los shaders de la escena en un solo lugar (02-tech-design §7.5 con 04-design-notes §14 y lo que WP-D1 precisó en
    // "Cambios por implementación de WP-D1"): las pruebas de dotnet comparan los .shader con esto, la de Unity mide el costo con
    // MaxAlu, y SceneShaderIds (WP-B0) tiene los mismos nombres.
    public static class ShaderArt
    {
        public const string Prefix = "RowMate/Scene/";
        public const string ShaderFolder = "Art/Shaders/Scene";
        public const string IncludeFolder = "Art/Shaders/Include";
        public const string WaterExtrasKeyword = "RM_WATER_EXTRAS";
        public const string TerrainLayersKeyword = "RM_TERRAIN_LAYERS";

        // Lo que es solo del nivel Alto (04-design-notes §15.2; P-10, DN6a-22), como palabras clave del material fijas en la sesión (las
        // pone SceneQualityApplier con el nivel, como las de sombras): el fondo tenue de estrellas y la capa lejana de nubes.
        public const string SkyStarFieldKeyword = "RM_SKY_STARFIELD";
        public const string CloudLayerKeyword = "RM_CLOUD_LAYER";

        public static readonly string[] Keywords = { WaterExtrasKeyword, TerrainLayersKeyword, SkyStarFieldKeyword, CloudLayerKeyword };

        public static readonly string[] Includes = { "RowMateScene", "RowMateFog", "RowMateDither", "RowMateLighting" };

        // Ningún shader lee el reloj de Unity (todo va con _RmSceneTime y las fases de Core) ni se limita a unos renderers.
        public static readonly string[] ForbiddenIdentifiers =
        {
            "_Time", "_SinTime", "_CosTime", "unity_DeltaTime", "_TimeParameters", "_LastTimeParameters", "only_renderers", "exclude_renderers",
        };

        // Los globales que pone SceneLighting en cada cuadro (§7.5; los colores _RmPalette* en lineal; los *Srgb con SetGlobalVector,
        // sin convertir; _RmAmbient* como multiplicadores del espacio del legacy, también sin convertir).
        public static readonly string[] Globals =
        {
            "_RmSceneTime", "_RmPaletteSkyTop", "_RmPaletteSkyHorizon", "_RmPaletteWaterNear", "_RmPaletteWaterFar", "_RmPaletteSunColor",
            "_RmPaletteSunGlow", "_RmPaletteFog", "_RmAmbient", "_RmAmbientSky", "_RmAmbientEquator", "_RmAmbientGround", "_RmFogParams",
            "_RmLowMist", "_RmGolden", "_RmDiskDir", "_RmDiskParams", "_RmDiskShape", "_RmLightDir", "_RmHorizonY", "_RmFocalDp", "_RmViewDp",
            "_RmDensity", "_RmZonePhase", "_RmSkyTopSrgb", "_RmSkyHorizonSrgb", "_RmHazeSrgb", "_RmEmissiveGain",
        };

        private static readonly string[] None = new string[0];

        public static readonly SceneShaderSpec[] Scene =
        {
            new SceneShaderSpec("Sky", 1000, 41, 1, false, new[] { "_RmMoonTex", "_RmGlowParams", "_RmStarField", "_RmMistBandDeg" }, None,
                maxBranchAlu: 48),
            new SceneShaderSpec("Stars", 1001, 8, 0, true, None, new[] { "_RmStarDir", "_RmStarAlpha" }),
            new SceneShaderSpec("Clouds", 1002, 12, 2, true, new[] { "_RmCloudAtlas", "_RmCloudLayer" },
                new[] { "_RmCloudDir", "_RmCloudVariant", "_RmCloudAlpha" }),
            new SceneShaderSpec("Horizon", 1003, 15, 0, false,
                new[] { "_RmHorizonTone", "_RmHorizonDepth", "_RmSnowEnabled", "_RmHorizonScale", "_RmSnowParams", "_RmRim", "_RmSnowTone" }, None),
            new SceneShaderSpec("WaterDegraded", 2450, WaterArt.MaxAlu, 0, false,
                new[] { "_RmWaterBandTop", "_RmWaterBandMid", "_RmWaterBandBottom", "_RmShimmer", "_RmShimmerColor", "_RmWaterExtras", "_RmShallowColor" },
                None),
            new SceneShaderSpec("Terrain", 2000, 35, 2, false,
                new[]
                {
                    "_RmNoiseTex", "_RmTerrainPebbles", "_RmTerrainWet", "_RmTerrainContact", "_RmTerrainGrass", "_RmTerrainGrassAlt",
                    "_RmTerrainForest", "_RmTerrainNeedles", "_RmTerrainRock", "_RmTerrainCanopy", "_RmLayerAlbedo", "_RmLayerNormal",
                }, None),
            new SceneShaderSpec("Prop", 2000, 30, 0, true, new[] { "_RmPlankParams", "_RmWindowDay", "_RmWindowNight", "_RmWindowMode" },
                new[] { "_RmFade", "_RmLit" }),
            new SceneShaderSpec("Foliage", 2000, 25, 0, true, new[] { "_RmTrunkColor", "_RmBushColor", "_RmWind" },
                new[] { "_RmVariant", "_RmWindPhase", "_RmFade" }),
            new SceneShaderSpec("Grass", 2000, 18, 0, true, new[] { "_RmGrassColorA", "_RmGrassColorB", "_RmFernColor", "_RmGrassEnd", "_RmWind" },
                new[] { "_RmTint", "_RmWindPhase" }),
            new SceneShaderSpec("Reeds", 2000, 18, 0, true, new[] { "_RmReedBase", "_RmReedTip", "_RmWind" }, new[] { "_RmWindPhase", "_RmFade" }),
            new SceneShaderSpec("Hull", 2000, 25, 0, false, new[] { "_RmGelcoat" }, None),
            new SceneShaderSpec("BoatShadow", 2451, 5, 0, false, new[] { "_RmShadowOffset", "_RmShadowAlpha" }, None),
            new SceneShaderSpec("ScreenOverlay", 3500, 4, 0, false, new[] { "_RmScrimColor", "_RmScrimTop", "_RmScrimBottom" }, None),
        };

        public static SceneShaderSpec Find(string name)
        {
            foreach (var spec in Scene)
            {
                if (spec.Name == name)
                {
                    return spec;
                }
            }

            throw new ArgumentException("ShaderArt: no hay un shader " + name, nameof(name));
        }

        // Las constantes kRm* de los .hlsl y los .shader con su valor en Art (un valor por constante).
        public static Dictionary<string, double> Constants()
        {
            return new Dictionary<string, double>(StringComparer.Ordinal)
            {
                // RowMateScene.hlsl: el halo del legacy (sky_painter.dart:79-81) y el borde del disco.
                ["kRmHaloAlpha0"] = SkyArt.HaloAlphas[0],
                ["kRmHaloAlpha1"] = SkyArt.HaloAlphas[1],
                ["kRmHaloAlpha2"] = SkyArt.HaloAlphas[2],
                ["kRmDiskEdgeRad"] = SceneMath.DegToRad(SkyArt.DiskEdgeDeg),
                ["kRmMoonHaloBase"] = SkyArt.MoonHaloBase,
                ["kRmMoonHaloPerIllumination"] = SkyArt.MoonHaloPerIllumination,

                // RowMateLighting.hlsl (04-design-notes §10.1).
                ["kRmLegacyBlueLift"] = MaterialArt.LegacyBlueLift,
                ["kRmKeyWeight"] = MaterialArt.KeyWeight,
                ["kRmNoonIntensity"] = MaterialArt.NoonIntensity,
                ["kRmFactorGamma"] = MaterialArt.FactorGamma,
                ["kRmFoliageWrap"] = MaterialArt.FoliageWrap,
                ["kRmGrassWrap"] = MaterialArt.GrassWrap,

                // RowMateFog.hlsl (§10.3) y los agregados del agua (§8.1).
                ["kRmMistHeight"] = MaterialArt.MistHeight,
                ["kRmMistNear"] = WaterArt.MistNear,
                ["kRmMistFar"] = WaterArt.MistFar,
                ["kRmMistGolden"] = WaterArt.MistGolden,

                // Sky (§3.3, §4.2, §4.3, §4.5).
                ["kRmSunLimb"] = SkyArt.SunLimbDarkening,
                ["kRmMoonTerminator"] = SkyArt.MoonTerminator,
                ["kRmMoonEarthshine"] = SkyArt.MoonEarthshine,
                ["kRmDitherAmplitude"] = SkyArt.DitherAmplitude,
                ["kRmStarFieldMinTan"] = Math.Tan(SceneMath.DegToRad(SkyArt.StarFieldCellDeg)),
                ["kRmStarFieldRadiusMin"] = SkyArt.StarFieldRadiusMinDp,
                ["kRmStarFieldRadiusMax"] = SkyArt.StarFieldRadiusMaxDp,

                // Stars: el borde suave de 0,5 dp.
                ["kRmStarEdgeDp"] = 0.5,

                // Clouds (§5.2 a §5.4).
                ["kRmCloudQuadHalfWidth"] = SkyTextures.CloudQuadHalfWidth,
                ["kRmCloudQuadTop"] = SkyTextures.CloudQuadTop,
                ["kRmCloudQuadBottom"] = SkyTextures.CloudQuadBottom,
                ["kRmCloudTopFrom"] = SkyArt.CloudTopFrom,
                ["kRmCloudTopLight"] = SkyArt.CloudTopLight,
                ["kRmCloudSunSide"] = SkyArt.CloudSunSide,
                ["kRmCloudBottom"] = SkyArt.CloudBottom,
                ["kRmCloudBottomGlow"] = SkyArt.CloudBottomGlow,
                ["kRmCloudNightRim"] = SkyArt.CloudNightRim,
                ["kRmCloudNightRimCos"] = Math.Cos(SceneMath.DegToRad(SkyArt.CloudNightRimDeg)),
                ["kRmCloudLayerScale"] = SkyArt.CloudLayerScale,
                ["kRmCloudLayerDrift"] = SkyArt.CloudLayerDrift,
                ["kRmCloudLayerEdge"] = SkyArt.CloudLayerEdge,
                ["kRmCloudLayerAlpha"] = SkyArt.CloudLayerAlpha,
                ["kRmCloudLayerMinTan"] = Math.Tan(SceneMath.DegToRad(SkyArt.CloudLayerMinDeg)),
                ["kRmCloudLayerMaxTan"] = Math.Tan(SceneMath.DegToRad(SkyArt.CloudLayerMaxDeg)),
                ["kRmCloudLayerHalfSpanTan"] = Math.Tan(SceneMath.DegToRad(SkyArt.CloudLayerHalfSpanDeg)),

                // Horizon (§6.4, §6.5).
                ["kRmFlankToSun"] = LakeArt.FlankToSun,
                ["kRmFlankAway"] = LakeArt.FlankAway,
                ["kRmFlankRampRad"] = SceneMath.DegToRad(LakeArt.FlankRampDeg),
                ["kRmHorizonBaseFog"] = LakeArt.BaseFog,
                ["kRmHorizonBaseFraction"] = LakeArt.BaseFraction,
                ["kRmAlpenglow"] = LakeArt.Alpenglow,
                ["kRmSnowFog"] = LakeArt.SnowFog,
                ["kRmSnowNightBase"] = LakeArt.SnowNightBase,
                ["kRmSnowNightPerIllumination"] = LakeArt.SnowNightPerIllumination,

                // WaterDegraded (water_painter.dart:92-103; §8.1).
                ["kRmLineSpacing"] = WaterArt.LineSpacing,
                ["kRmLineHalfWidthBase"] = WaterArt.LineHalfWidthBase,
                ["kRmLineHalfWidthSlope"] = WaterArt.LineHalfWidthSlope,
                ["kRmShimmerBase"] = WaterArt.ShimmerBaseAlpha,
                ["kRmShimmerGain"] = WaterArt.ShimmerStrengthAlpha,
                ["kRmLineHalfThicknessDp"] = WaterArt.LineThicknessDp / 2,
                ["kRmLineSoftDp"] = WaterArt.LineSoftDp,
                ["kRmReflectNear"] = WaterArt.ReflectNear,
                ["kRmReflectFar"] = WaterArt.ReflectFar,
                ["kRmShallowInner"] = WaterArt.ShallowInner,
                ["kRmShallowOuter"] = WaterArt.ShallowOuter,

                // Terrain (§7.5).
                ["kRmWetTop"] = MaterialArt.WetTop,
                ["kRmWetNoise"] = MaterialArt.WetNoise,
                ["kRmWetReflect"] = MaterialArt.WetReflect,
                ["kRmContactTop"] = MaterialArt.ContactTop,
                ["kRmContactAlpha"] = MaterialArt.ContactAlpha,
                ["kRmPebbleLum"] = MaterialArt.PebbleLuminance,
                ["kRmRockSlopeCos"] = Math.Cos(SceneMath.DegToRad(MaterialArt.RockSlopeDeg)),
                ["kRmNoiseFineM"] = MaterialArt.NoiseFineMeters,
                ["kRmNoiseCoarseM"] = MaterialArt.NoiseCoarseMeters,
                ["kRmCanopyOcclusion"] = MaterialArt.CanopyOcclusion,
                ["kRmForestFromX"] = LakeArt.ForestInnerX,
                ["kRmBeachWidth"] = LakeArt.BeachWidth,
                ["kRmBankX"] = ShoreEnvironment.BankX,

                // Prop (§7.3).
                ["kRmPlankShade"] = MaterialArt.PlankShade,
                ["kRmWindowFresnel"] = MaterialArt.WindowFresnel,

                // Hull (§9; WP-D2).
                ["kRmHullExposure"] = MaterialArt.HullExposure,
                ["kRmHullAmbientChroma"] = MaterialArt.HullAmbientChroma,
            };
        }

        // Los valores por defecto de las propiedades del material ("Shader._Propiedad"): los colores en sRGB (el hex/255, como los
        // escribe ShaderLab; Unity los pasa a lineal), los vectores y los números tal cual.
        public static Dictionary<string, double[]> MaterialDefaults()
        {
            return new Dictionary<string, double[]>(StringComparer.Ordinal)
            {
                ["Sky._RmGlowParams"] = new[] { SkyArt.GlowStrength, SkyArt.GlowFalloffDeg, SkyArt.GlowTopDeg, 0 },
                ["Sky._RmStarField"] = new[] { SkyArt.StarFieldCellDeg, SkyArt.StarFieldAlphaMin, SkyArt.StarFieldAlphaMax, 0 },
                ["Sky._RmMistBandDeg"] = new[] { SkyArt.MistBandDeg },
                ["Horizon._RmSnowParams"] = new[] { LakeArt.SnowThreshold, LakeArt.SnowEdge, LakeArt.SnowNoise, LakeArt.SnowNoisePeriodDeg },
                ["Horizon._RmRim"] = new[] { LakeArt.RimHeightDeg, LakeArt.RimAzimuthDeg, LakeArt.RimStrength, LakeArt.RimFalloffDeg },
                ["Horizon._RmSnowTone"] = Srgb(LakeArt.SnowTone),
                ["Horizon._RmHorizonDepth"] = new[] { LakeArt.FarDepth },
                ["Horizon._RmSnowEnabled"] = new[] { 1.0 },
                ["Horizon._RmHorizonScale"] = new[] { Math.Tan(SceneMath.DegToRad(LakeArt.MaxPeakDeg)), 1, 1, 0 },
                ["WaterDegraded._RmWaterExtras"] = new[] { WaterArt.ReflectStrength, WaterArt.ShallowStrength, WaterArt.MistStrength, 0 },
                ["WaterDegraded._RmShallowColor"] = Srgb(WaterArt.ShallowColor),
                ["Terrain._RmTerrainPebbles"] = Srgb(LakeArt.Pebbles),
                ["Terrain._RmTerrainWet"] = Srgb(LakeArt.WetBand),
                ["Terrain._RmTerrainContact"] = Srgb(LakeArt.Contact),
                ["Terrain._RmTerrainGrass"] = Srgb(LakeArt.Grass),
                ["Terrain._RmTerrainGrassAlt"] = Srgb(LakeArt.GrassAlt),
                ["Terrain._RmTerrainForest"] = Srgb(LakeArt.ForestFloor),
                ["Terrain._RmTerrainNeedles"] = Srgb(LakeArt.Needles),
                ["Terrain._RmTerrainRock"] = Srgb(LakeArt.Rock),
                ["Terrain._RmTerrainCanopy"] = Srgb(LakeArt.Canopy),
                ["Prop._RmPlankParams"] = (double[])MaterialArt.PlankParams.Clone(),
                ["Prop._RmWindowDay"] = Srgb(LakeArt.WindowDay),
                ["Prop._RmWindowNight"] = Srgb(LakeArt.WindowNight),
                ["Prop._RmWindowMode"] = new[] { 0.0 },
                ["Foliage._RmTrunkColor"] = Srgb(LakeArt.Trunk),
                ["Foliage._RmBushColor"] = Srgb(LakeArt.Bush),
                ["Foliage._RmWind"] = (double[])MaterialArt.PineWind.Clone(),
                ["Grass._RmGrassColorA"] = Srgb(LakeArt.Grass),
                ["Grass._RmGrassColorB"] = Srgb(LakeArt.GrassAlt),
                ["Grass._RmFernColor"] = Srgb(LakeArt.Fern),
                ["Grass._RmGrassEnd"] = new[] { MaterialArt.GrassEndMedium, MaterialArt.GrassFadeFraction, 0, 0 },
                ["Grass._RmWind"] = (double[])MaterialArt.GrassWind.Clone(),
                ["Reeds._RmReedBase"] = Srgb(LakeArt.ReedBase),
                ["Reeds._RmReedTip"] = Srgb(LakeArt.ReedTip),
                ["Reeds._RmWind"] = (double[])MaterialArt.ReedWind.Clone(),
                ["Hull._RmGelcoat"] = new[] { MaterialArt.Gelcoat[0], MaterialArt.Gelcoat[1], MaterialArt.Gelcoat[2], 0 },
                ["BoatShadow._RmShadowOffset"] =
                    new[] { MaterialArt.BoatShadow[2], MaterialArt.BoatShadow[0], MaterialArt.BoatShadow[1], MaterialArt.BoatShadowFalloff },
                ["BoatShadow._RmShadowAlpha"] = new[] { MaterialArt.BoatShadow[3] },
                ["ScreenOverlay._RmScrimColor"] = Srgb(MaterialArt.ScrimColor),
                ["ScreenOverlay._RmScrimTop"] = new[] { MaterialArt.ScrimTop[0], MaterialArt.ScrimTop[1], 0, 0 },
                ["ScreenOverlay._RmScrimBottom"] = new[] { MaterialArt.ScrimBottom[0], MaterialArt.ScrimBottom[1], 0, 0 },
            };
        }

        private static double[] Srgb(uint argb)
        {
            var c = SceneColor.FromArgb(argb);
            return new[] { c.R, c.G, c.B, 1 };
        }
    }
}
