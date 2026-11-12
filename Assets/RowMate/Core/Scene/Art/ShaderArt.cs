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
            // It. 8 (08-entornos-selector/02-tech-design §8.4): la ola de la costa y el puente más cercano (0 en los demás).
            "_RmSwell", "_RmUnderBridge",
        };

        // It. 8 (08-entornos-selector/04-design-notes §6): lo que cuesta lo que solo corre en río, costa o regata, en un salto por un uniforme
        // (el lago y las tomas de la 6a y la 7 no lo pagan): la luz bajo el puente (Prop; WaterArt.MaxBranchAlu el agua con la ola), el
        // suelo de cada escenario con el puente (Terrain) y las ventanas del skyline de noche (Horizon).
        public const int UnderBridgeBranchAlu = 32, TerrainBranchAlu = 76, HorizonBranchAlu = 56;
        // El horizonte suma la comparación del salto de las ventanas (15 de la 6a + 1).
        public const int HorizonAlu = 16;

        private static readonly string[] None = new string[0];

        public static readonly SceneShaderSpec[] Scene =
        {
            new SceneShaderSpec("Sky", 1000, 41, 1, false, new[] { "_RmMoonTex", "_RmGlowParams", "_RmStarField", "_RmMistBandDeg" }, None,
                maxBranchAlu: 48),
            new SceneShaderSpec("Stars", 1001, 8, 0, true, None, new[] { "_RmStarDir", "_RmStarAlpha" }),
            new SceneShaderSpec("Clouds", 1002, 12, 2, true, new[] { "_RmCloudAtlas", "_RmCloudLayer" },
                new[] { "_RmCloudDir", "_RmCloudVariant", "_RmCloudAlpha" }),
            new SceneShaderSpec("Horizon", 1003, HorizonAlu, 0, false,
                new[] { "_RmHorizonTone", "_RmHorizonDepth", "_RmSnowEnabled", "_RmHorizonScale", "_RmSnowParams", "_RmRim", "_RmSnowTone", "_RmSkylineWindows", "_RmSkylineWindow",
                        "_RmFlagTone0", "_RmFlagTone1", "_RmFlagTone2", "_RmFlagTone3", "_RmFlagMast" },
                None, maxBranchAlu: HorizonBranchAlu),
            new SceneShaderSpec("WaterDegraded", 2450, WaterArt.MaxAlu, 0, false,
                new[] { "_RmWaterBandTop", "_RmWaterBandMid", "_RmWaterBandBottom", "_RmShimmer", "_RmShimmerColor", "_RmWaterExtras", "_RmShallowColor" },
                None, maxBranchAlu: WaterArt.MaxBranchAlu),
            new SceneShaderSpec("Terrain", 2000, 35, 2, false,
                new[]
                {
                    "_RmNoiseTex", "_RmTerrainPebbles", "_RmTerrainWet", "_RmTerrainContact", "_RmTerrainGrass", "_RmTerrainGrassAlt",
                    "_RmTerrainForest", "_RmTerrainNeedles", "_RmTerrainRock", "_RmTerrainCanopy", "_RmLayerAlbedo", "_RmLayerNormal",
                    "_RmTerrainKind", "_RmTerrainCobble", "_RmTerrainWall", "_RmTerrainSand", "_RmTerrainCliff", "_RmTerrainCliffGrass", "_RmTerrainLawn",
                }, None, maxBranchAlu: TerrainBranchAlu),
            new SceneShaderSpec("Prop", 2000, 30, 0, true, new[] { "_RmPlankParams", "_RmWindowDay", "_RmWindowNight", "_RmWindowMode" },
                new[] { "_RmFade", "_RmLit" }, maxBranchAlu: UnderBridgeBranchAlu),
            new SceneShaderSpec("Foliage", 2000, 25, 0, true, new[] { "_RmTrunkColor", "_RmBushColor", "_RmWind" },
                new[] { "_RmVariant", "_RmWindPhase", "_RmFade" }),
            new SceneShaderSpec("Grass", 2000, 18, 0, true, new[] { "_RmGrassColorA", "_RmGrassColorB", "_RmFernColor", "_RmGrassEnd", "_RmWind" },
                new[] { "_RmTint", "_RmWindPhase" }),
            new SceneShaderSpec("Reeds", 2000, 18, 0, true, new[] { "_RmReedBase", "_RmReedTip", "_RmWind" }, new[] { "_RmWindPhase", "_RmFade" }),
            new SceneShaderSpec("Hull", 2000, 25, 0, false, new[] { "_RmGelcoat" }, None),
            new SceneShaderSpec("BoatShadow", 2451, 5, 0, false, new[] { "_RmShadowOffset", "_RmShadowAlpha" }, None),
            new SceneShaderSpec("ScreenOverlay", 3500, 4, 0, false, new[] { "_RmScrimColor", "_RmScrimTop", "_RmScrimBottom" }, None),
        };

        // It. 7 (07-remero-palada/02-tech-design §7.4; 04-design-notes §6): los cinco del remero, aparte de los 13 de la 6a
        // (SceneShaderIds.RowerShaderNames). El presupuesto de fragmento es el de Medio sin las sombras de URP, como en la 6a; los
        // charcos ≤ 20 por contrato (DA7-08; el hervidero y el anillo, en bloques condicionales).
        public static readonly SceneShaderSpec[] Rower =
        {
            new SceneShaderSpec("Rower", 2000, 30, 0, false, new[] { "_RmRowerColors", "_RmRimFill" }, None),
            new SceneShaderSpec("Oar", 2000, 20, 0, false, new[] { "_RmOarColors", "_RmOarLoad", "_RmMinWidthDp", "_RmPixelSize" }, None),
            new SceneShaderSpec("Spray", 3100, 16, 0, true, new[] { "_RmMinDropDp" }, new[] { "_RmSpray" }),
            // It. 6b (DT6b-23): + las ramas por tipo de la calma del charco, la columna de luz (4) y la estela (5); el salto más caro ≤ 40.
            new SceneShaderSpec("WaterMarks", 3050, 20, 0, true, new[] { "_RmEffort" }, new[] { "_RmMarks" }, maxBranchAlu: 40),
            new SceneShaderSpec("Buoy", 2000, 25, 0, false, new[] { "_RmBuoyColor", "_RmFade" }, None),
        };

        // It. 8 (08-entornos-selector/02-tech-design §8.4; 04-design-notes §5): las luces de la noche de los escenarios (halos de faroles,
        // faro y torre; el haz del faro) y la espuma de la costa (Foam: transparente, instanciada, con el ruido de la escena). Aparte de
        // los 13 de la 6a y los 5 de la 7: los cubre EnvironmentShaderContractTests, la colección de variantes y SceneShaderTests.
        // (Facade, Crowd y MarkerText quedaron fuera: los objetos de los kits van horneados por segmento con Prop, B8-01 y B8-05.)
        public static readonly SceneShaderSpec[] Environment =
        {
            new SceneShaderSpec("Glow", 3010, 14, 0, true, new[] { "_RmGlowLamp", "_RmGlowBeacon", "_RmGlowBeam" }, new[] { "_RmGlow" }),
            // Foam: 62 ALU medidos por SceneShaderTests (D3D11, Medio) en la reconciliación de la It. 8. It. 6b (#22; AC-6B-29): el tope vuelve a
            // 30 (el latido y los extremos de la franja pasan al vértice, WP-D).
            new SceneShaderSpec("Foam", 3020, FoamMaxAlu, 1, true, new[] { "_RmNoiseTex", "_RmFoamPulse", "_RmFoamColor" }, new[] { "_RmFoam" }),
        };

        public const int FoamMaxAlu = 30;

        // ── It. 6b (06b-agua-avanzada/02-tech-design §3.4, §4.1; 00-arte-agua §2, §11) ──
        // WaterAdvanced: un shader con tres variantes por palabra clave del material (RM_WATER_HIGH, la de Medio sin palabra y RM_WATER_LITE),
        // cola 2450 opaca, un solo pase UniversalForward, SIN Fallback y con el tag RmWater = Advanced en cada SubShader (DT6b-22). El
        // presupuesto de cada variante va en WaterTiers; el de la spec es el de Medio, con MaxBranchAlu para el salto degradado (el fundido,
        // DT6b-12) y la espuma de las crestas de la costa. La estela y las columnas de luz son tipos de WaterMarks (DT6b-23): no hay
        // WaterWake. El vidrio desenfocado es RowMate/UI/HudBlur (aparte de la escena).
        public const string WaterHighKeyword = "RM_WATER_HIGH";
        public const string WaterLiteKeyword = "RM_WATER_LITE";
        public const string WaterTag = "RmWater";
        public const string WaterTagValue = "Advanced";
        public const string WaterInclude = "RowMateWater";
        public const string HudBlurShader = "RowMate/UI/HudBlur";
        public const string HudBlurFolder = "Art/Shaders/UI";

        public static readonly string[] WaterKeywords = { WaterHighKeyword, WaterLiteKeyword };

        public static readonly SceneShaderSpec[] WaterShaders =
        {
            new SceneShaderSpec("WaterAdvanced", 2450, 70, 5, false,
                new[] { "_RmWaterNormal", "_RmHorizonProfile", "_RmHullFootprint", "_RmReflectionMix" }, None, maxBranchAlu: 40),
        };

        // El presupuesto de fragmento de cada variante (ALU y texturas, D3D11, sin las sombras de URP): Alto 110/6, Medio 70/5, Lite 35/2.
        public static readonly WaterTierBudget[] WaterTiers =
        {
            new WaterTierBudget(QualityTier.High, WaterHighKeyword, 110, 6),
            new WaterTierBudget(QualityTier.Medium, "", 70, 5),
            new WaterTierBudget(QualityTier.Low, WaterLiteKeyword, 35, 2),
        };

        // Los globales del agua avanzada (los pone SceneHost por cuadro): las fases y los corrimientos de Core en double reducidos
        // (WavePhases), la RT del reflejo planar (Alto), el fundido a la degradada, la estela empaquetada (WakeTrail) y su apagado.
        public static readonly string[] WaterGlobals =
        {
            "_RmWaterPhases", "_RmWaterOffsets", "_RmWaterReflectionTex", "_RmDegradedBlend", "_RmWake", "_RmWakeFade",
        };

        public static SceneShaderSpec FindWater(string name)
        {
            foreach (var spec in WaterShaders)
            {
                if (spec.Name == name)
                {
                    return spec;
                }
            }

            throw new ArgumentException("ShaderArt: no hay un shader de agua " + name, nameof(name));
        }

        // Las constantes kRm* del agua avanzada (RowMateWater.hlsl y WaterAdvanced.shader) con su fuente en Core: el contrato del legacy
        // (WaterModel) y la tabla del arte (WaterLookArt). Las comprueba WaterShaderContractTests.
        public static Dictionary<string, double> WaterConstants()
        {
            return new Dictionary<string, double>(StringComparer.Ordinal)
            {
                ["kRmWaterBaseNear"] = Water.WaterModel.BaseNear,
                ["kRmWaterBaseFar"] = Water.WaterModel.BaseFar,
                ["kRmWaterColorGain"] = Water.WaterModel.ColorGain,
                ["kRmWaterFresnelGain"] = Water.WaterModel.FresnelGain,
                ["kRmWaterFogNear"] = Water.WaterModel.FogNear,
                ["kRmWaterFogFar"] = Water.WaterModel.FogFar,
                ["kRmColumnBaseDp"] = Water.WaterModel.ColumnBaseDp,
                ["kRmColumnPerMeterDp"] = Water.WaterModel.ColumnPerMeterDp,
                ["kRmColumnPeak"] = Water.WaterModel.ColumnPeak,
                ["kRmWakeFoamMax"] = Water.WaterModel.WakeFoamMax,
                ["kRmCrestFoamMax"] = WaterLookArt.CrestFoamMax,
                ["kRmContactDarken"] = WaterLookArt.ContactDarken,
                ["kRmContactFalloff"] = WaterLookArt.ContactFalloff,
                ["kRmDegradedLineMax"] = WaterArt.Lines - 1,
                ["kRmWaterAbsorb"] = WaterLookArt.Absorb,
                ["kRmWaterAbsorbNear"] = WaterLookArt.AbsorbNear,
                ["kRmWaterAbsorbFar"] = WaterLookArt.AbsorbFar,
                ["kRmFacetShade"] = WaterLookArt.FacetShade,
                ["kRmRiverTurbidity"] = WaterLookArt.RiverTurbidity,
                ["kRmShoreSoft"] = WaterLookArt.ShoreSoftMeters,
                ["kRmShoreLowSoft"] = WaterLookArt.ShoreLowSoftMeters,
                ["kRmGlintGain"] = WaterLookArt.GlintGain,
                ["kRmSheenGain"] = WaterLookArt.SheenGain,
                ["kRmSheenExponentScale"] = WaterLookArt.SheenExponentScale,
                ["kRmSeamDp"] = WaterLookArt.SeamDp,
                ["kRmCalmWavesA"] = WaterLookArt.CalmWavesA,
                ["kRmCalmWavesB"] = WaterLookArt.CalmWavesB,
                ["kRmBandKeep"] = Water.WaterVertexField.BandKeep,
                ["kRmBandDrop"] = Water.WaterVertexField.BandDrop,
                ["kRmWave2K"] = Water.WaterVertexField.Wave2K,
                ["kRmCalmKA"] = Water.WaterVertexField.CalmKA,
                ["kRmCalmKB"] = Water.WaterVertexField.CalmKB,
                ["kRmCrestMean"] = Water.WaterVertexField.CrestMean,
                ["kRmFoamCrestMean"] = Water.WaterVertexField.FoamCrestMean,
                ["kRmFoamOctave"] = Water.WaterVertexField.FoamOctave,
                ["kRmRingMaxStep"] = Geometry.WaterRingMesh.MaxRingStep,
            };
        }

        // Las constantes kRm* de los cinco del remero con su valor en RowerArt (las comprueba RowerShaderContractTests).
        public static Dictionary<string, double> RowerConstants()
        {
            return new Dictionary<string, double>(StringComparer.Ordinal)
            {
                ["kRmClothWrap"] = RowerArt.ClothWrap,
                ["kRmSkinWrap"] = RowerArt.SkinWrap,
                ["kRmSkinScatter"] = RowerArt.SkinScatter,
                ["kRmSuitSheen"] = RowerArt.SuitSheen,
                ["kRmRowerSpecular"] = RowerArt.RowerSpecular,
                ["kRmGlassesReflect"] = RowerArt.GlassesReflect,
                ["kRmOarTip"] = RowerArt.OarTipFromPin,
                ["kRmDropCore"] = RowerArt.DropCore,
                ["kRmSwirlHalf"] = RowerArt.SwirlSeparation / 2,
                ["kRmSwirlEye"] = RowerArt.SwirlEye,
                ["kRmSwirlEyeGrowth"] = RowerArt.SwirlEyeGrowth,
                ["kRmMarkFoamGain"] = RowerArt.MarkFoamGain,
                ["kRmMarkEyeAlpha"] = RowerArt.MarkEyeAlpha,
                ["kRmWakeCameraFade"] = Water.WaterModel.SternRelZ,
                ["kRmPuddleFlatAlpha"] = WaterLookArt.PuddleFlatAlpha,
                ["kRmColumnNear"] = WaterLookArt.ColumnNear,
                ["kRmColumnNearAlpha"] = WaterLookArt.ColumnNearAlpha,
                ["kRmColumnStripeMeters"] = WaterLookArt.ColumnStripeMeters,
                ["kRmPuddleEyeDarken"] = WaterLookArt.PuddleEyeDarken,
                ["kRmPuddleEdgeGain"] = WaterLookArt.PuddleEdgeGain,
                ["kRmPuddleFlatDarken"] = WaterLookArt.PuddleFlatDarken,
                ["kRmWakeCenterGain"] = WaterLookArt.WakeCenterGain,
                ["kRmWakeBreakWaves"] = WaterLookArt.WakeBreakWaves,
                ["kRmWakeFoamWaves"] = WaterLookArt.WakeFoamWaves,
                ["kRmBuoyGloss"] = RowerArt.BuoyGloss,
                ["kRmBuoySpecular"] = RowerArt.BuoySpecular,
                ["kRmBuoyWhite"] = RowerArt.BuoyWhite,
            };
        }

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
                ["kRmGlowGoldenFrom"] = SkyArt.GoldenGlowFrom,
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
                ["kRmBacklight"] = LakeArt.Backlight,
                ["kRmBacklightLobeRad"] = SceneMath.DegToRad(LakeArt.BacklightLobeDeg),
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
                ["kRmShadowDarken"] = RowerArt.ShadowDarken,

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
                ["kRmHullDeckGloss"] = MaterialArt.HullDeckGloss,
                ["kRmHullAmbientChroma"] = MaterialArt.HullAmbientChroma,

                // It. 8 · Prop: la vida de banderas, sombrillas y boyas marcada en el uv por KitSegmentMesh (00-arte-entornos §1.4).
                ["kRmFlagWave"] = RegattaArt.FlagWave,
                ["kRmFlagWaveRate"] = RegattaArt.FlagWaveRate,
                ["kRmBuoyPitch"] = RegattaArt.BuoyPitch,
                ["kRmBuoyPitchRate"] = RegattaArt.BuoyPitchRate,
                ["kRmBuoyHeave"] = RegattaArt.BuoyHeave,
                ["kRmBuoyHeaveRate"] = RegattaArt.BuoyHeaveRate,
                ["kRmCrowdSway"] = RegattaArt.CrowdSway,
                ["kRmCrowdBob"] = RegattaArt.CrowdBob,

                // It. 8 · Horizon: las banderas de la tira lejana de la regata (QA-V8-01; RegattaBackdrop).
                ["kRmFarFlagWave"] = RegattaBackdrop.FlagWaveP,
                ["kRmFarFlagRate"] = RegattaBackdrop.FlagWaveRate,

                // It. 8 · RowMateLighting.hlsl: bajo el puente (DT8-07; RiverArt).
                ["kRmBridgeHalfSpan"] = RiverArt.BridgeEndX,
                ["kRmBridgeDeckBottom"] = RiverArt.DeckBottom,

                // It. 8 · WaterDegraded: la ola de la costa (CoastSwell, CoastArt).
                ["kRmSwellColorGain"] = CoastSwell.ColorGain,
                ["kRmSwellFadeNear"] = CoastArt.SwellFadeNear,
                ["kRmSwellFadeFar"] = CoastArt.SwellFadeFar,

                // It. 8 · Terrain: los suelos de río, costa y regata.
                ["kRmCobbleM"] = RiverArt.CobbleSize,
                ["kRmCobbleJoint"] = RiverArt.CobbleJoint,
                ["kRmCobbleTone"] = RiverArt.CobbleTone,
                ["kRmLawnStripeM"] = RegattaArt.LawnStripeWidth,
                ["kRmLawnStripe"] = RegattaArt.LawnStripeContrast,
                ["kRmCoastGrassFrom"] = CoastArt.PlateauGrassFrom,
                ["kRmSandWet"] = CoastArt.WetSandShade,

                // It. 8 · Horizon: las ventanas del skyline del río en px de TV (river_environment.dart:58-66; Skyline).
                ["kRmLegacyFocalPx"] = LegacyTv.Focal,
                ["kRmSkylineLeftPx"] = LegacyTv.Width / 2,
                ["kRmSkylineMaxPx"] = LegacyTv.Height * RiverArt.HorizonFraction,
                ["kRmSkylineColumnPx"] = Skyline.ColumnPx,
                ["kRmSkylineCellXPx"] = Skyline.CellWidthPx,
                ["kRmSkylineCellYPx"] = Skyline.CellHeightPx,
                ["kRmSkylineWindowXPx"] = Skyline.WindowWidthPx,
                ["kRmSkylineWindowYPx"] = Skyline.WindowHeightPx,
                ["kRmSkylineWindowAlpha"] = RiverArt.SkylineWindowAlpha,
                ["kRmSkylineWindowThreshold"] = RiverArt.SkylineWindowThreshold,
                ["kRmSkylineNightAmbient"] = RiverArt.WindowLitThreshold,

                // It. 8 · Glow (ShaderArt.Environment).
                ["kRmGlowPull"] = RiverArt.GlowPull,
                ["kRmBeamFlashCos"] = Math.Cos(SceneMath.DegToRad(CoastArt.BeamFlashDeg)),
                ["kRmBeamRootWidth"] = CoastArt.BeamRootWidth,

                // It. 8 · Foam (ShaderArt.Environment): el ruido que la rompe en jirones y el apagado en el cambio de orilla.
                ["kRmFoamNoiseM"] = CoastArt.FoamNoiseMeters,
                ["kRmFoamTaperM"] = CoastArt.FoamTaperMeters,
            };
        }

        // Los valores por defecto de las propiedades del material ("Shader._Propiedad"): los colores en sRGB (el hex/255, como los
        // escribe ShaderLab; Unity los pasa a lineal), los vectores y los números tal cual.
        public static Dictionary<string, double[]> MaterialDefaults()
        {
            return new Dictionary<string, double[]>(StringComparer.Ordinal)
            {
                ["Sky._RmGlowParams"] = new[] { SkyArt.GlowStrength, SkyArt.GlowFalloffDeg, SkyArt.GlowTopDeg, SkyArt.GoldenGlowStrength - SkyArt.GlowStrength },
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

                // It. 8 (04-design-notes §3 a §5).
                ["Terrain._RmTerrainKind"] = new[] { 0.0 },
                ["Terrain._RmTerrainCobble"] = Srgb(RiverArt.Cobble),
                ["Terrain._RmTerrainWall"] = Srgb(RiverArt.WallStone),
                ["Terrain._RmTerrainSand"] = Srgb(CoastArt.Sand),
                ["Terrain._RmTerrainCliff"] = Srgb(CoastArt.CliffRock),
                ["Terrain._RmTerrainCliffGrass"] = Srgb(CoastArt.CliffGrass),
                ["Terrain._RmTerrainLawn"] = Srgb(RegattaArt.Lawn),
                ["Horizon._RmSkylineWindows"] = new[] { 0.0 },
                ["Horizon._RmSkylineWindow"] = Srgb(RiverArt.SkylineWindow),
                ["Horizon._RmFlagTone0"] = Srgb(RegattaArt.FlagTones[0]),
                ["Horizon._RmFlagTone1"] = Srgb(RegattaArt.FlagTones[1]),
                ["Horizon._RmFlagTone2"] = Srgb(RegattaArt.FlagTones[2]),
                ["Horizon._RmFlagTone3"] = Srgb(RegattaArt.FlagTones[3]),
                ["Horizon._RmFlagMast"] = Srgb(RegattaArt.BackdropTower),
                ["Glow._RmGlowLamp"] = Srgb(RiverArt.LampHalo),
                ["Glow._RmGlowBeacon"] = Srgb(CoastArt.LighthouseHalo),
                ["Glow._RmGlowBeam"] = new[] { CoastArt.BeamLength, CoastArt.BeamApertureDeg, CoastArt.BeamPeriodSeconds, CoastArt.BeamFlash },
                ["Foam._RmFoamPulse"] = new[] { CoastArt.FoamPulseSeconds },
                ["Foam._RmFoamColor"] = Srgb(CoastArt.FoamColor),
            };
        }

        private static double[] Srgb(uint argb)
        {
            var c = SceneColor.FromArgb(argb);
            return new[] { c.R, c.G, c.B, 1 };
        }
    }

    // El presupuesto de una variante de WaterAdvanced (la palabra clave vacía es la de Medio).
    public readonly struct WaterTierBudget
    {
        public WaterTierBudget(QualityTier tier, string keyword, int maxAlu, int maxTextures)
        {
            Tier = tier;
            Keyword = keyword;
            MaxAlu = maxAlu;
            MaxTextures = maxTextures;
        }

        public QualityTier Tier { get; }
        public string Keyword { get; }
        public int MaxAlu { get; }
        public int MaxTextures { get; }
    }
}
