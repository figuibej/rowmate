using UnityEngine;

namespace RowMate.Scene
{
    // El contrato de nombres con los shaders de la escena (06a-escena-lago/02-tech-design §7.5; 04-design-notes, "Cambios por
    // implementación de WP-D1" §3; QA-G6aT-01): TODOS los shaders, propiedades de material y de instancia, globales y palabras clave,
    // como Shader.PropertyToID estáticos (se calculan una vez: el Tick no asigna ni busca por texto). Los mismos nombres que
    // Core/Scene/Art/ShaderArt (SceneShaderContractTests los compara con los .shader; SceneAssetsTests con esta clase).
    public static class SceneShaderIds
    {
        // ── Los 13 shaders (RowMate/Scene/<Nombre>) ──
        public const string Sky = "RowMate/Scene/Sky";
        public const string Stars = "RowMate/Scene/Stars";
        public const string Clouds = "RowMate/Scene/Clouds";
        public const string Horizon = "RowMate/Scene/Horizon";
        public const string WaterDegraded = "RowMate/Scene/WaterDegraded";
        public const string Terrain = "RowMate/Scene/Terrain";
        public const string Prop = "RowMate/Scene/Prop";
        public const string Foliage = "RowMate/Scene/Foliage";
        public const string Grass = "RowMate/Scene/Grass";
        public const string Reeds = "RowMate/Scene/Reeds";
        public const string Hull = "RowMate/Scene/Hull";
        public const string BoatShadow = "RowMate/Scene/BoatShadow";
        public const string ScreenOverlay = "RowMate/Scene/ScreenOverlay";

        public static readonly string[] ShaderNames =
        {
            Sky, Stars, Clouds, Horizon, WaterDegraded, Terrain, Prop, Foliage, Grass, Reeds, Hull, BoatShadow, ScreenOverlay,
        };

        // ── Palabras clave (constantes durante la sesión, QA-G6aT-07) ──
        public const string WaterExtrasKeyword = "RM_WATER_EXTRAS";
        public const string TerrainLayersKeyword = "RM_TERRAIN_LAYERS";
        public const string SkyStarFieldKeyword = "RM_SKY_STARFIELD";
        public const string CloudLayerKeyword = "RM_CLOUD_LAYER";

        public static readonly string[] Keywords = { WaterExtrasKeyword, TerrainLayersKeyword, SkyStarFieldKeyword, CloudLayerKeyword };

        // ── Globales (SceneLighting, en cada cuadro) ──
        public static readonly int SceneTime = Shader.PropertyToID("_RmSceneTime");
        public static readonly int PaletteSkyTop = Shader.PropertyToID("_RmPaletteSkyTop");
        public static readonly int PaletteSkyHorizon = Shader.PropertyToID("_RmPaletteSkyHorizon");
        public static readonly int PaletteWaterNear = Shader.PropertyToID("_RmPaletteWaterNear");
        public static readonly int PaletteWaterFar = Shader.PropertyToID("_RmPaletteWaterFar");
        public static readonly int PaletteSunColor = Shader.PropertyToID("_RmPaletteSunColor");
        public static readonly int PaletteSunGlow = Shader.PropertyToID("_RmPaletteSunGlow");
        public static readonly int PaletteFog = Shader.PropertyToID("_RmPaletteFog");
        public static readonly int Ambient = Shader.PropertyToID("_RmAmbient");
        public static readonly int AmbientSky = Shader.PropertyToID("_RmAmbientSky");
        public static readonly int AmbientEquator = Shader.PropertyToID("_RmAmbientEquator");
        public static readonly int AmbientGround = Shader.PropertyToID("_RmAmbientGround");
        public static readonly int FogParams = Shader.PropertyToID("_RmFogParams");
        public static readonly int LowMist = Shader.PropertyToID("_RmLowMist");
        public static readonly int Golden = Shader.PropertyToID("_RmGolden");
        public static readonly int DiskDir = Shader.PropertyToID("_RmDiskDir");
        public static readonly int DiskParams = Shader.PropertyToID("_RmDiskParams");
        public static readonly int DiskShape = Shader.PropertyToID("_RmDiskShape");
        public static readonly int LightDir = Shader.PropertyToID("_RmLightDir");
        public static readonly int HorizonY = Shader.PropertyToID("_RmHorizonY");
        public static readonly int FocalDp = Shader.PropertyToID("_RmFocalDp");
        public static readonly int ViewDp = Shader.PropertyToID("_RmViewDp");
        public static readonly int Density = Shader.PropertyToID("_RmDensity");
        public static readonly int ZonePhase = Shader.PropertyToID("_RmZonePhase");
        public static readonly int SkyTopSrgb = Shader.PropertyToID("_RmSkyTopSrgb");
        public static readonly int SkyHorizonSrgb = Shader.PropertyToID("_RmSkyHorizonSrgb");
        public static readonly int HazeSrgb = Shader.PropertyToID("_RmHazeSrgb");
        public static readonly int EmissiveGain = Shader.PropertyToID("_RmEmissiveGain");
        // It. 8 (08-entornos-selector/02-tech-design §8.4): la ola de la costa y el puente más cercano (en 0 en los demás).
        public static readonly int Swell = Shader.PropertyToID("_RmSwell");
        public static readonly int UnderBridge = Shader.PropertyToID("_RmUnderBridge");

        public static readonly string[] GlobalNames =
        {
            "_RmSceneTime", "_RmPaletteSkyTop", "_RmPaletteSkyHorizon", "_RmPaletteWaterNear", "_RmPaletteWaterFar", "_RmPaletteSunColor",
            "_RmPaletteSunGlow", "_RmPaletteFog", "_RmAmbient", "_RmAmbientSky", "_RmAmbientEquator", "_RmAmbientGround", "_RmFogParams",
            "_RmLowMist", "_RmGolden", "_RmDiskDir", "_RmDiskParams", "_RmDiskShape", "_RmLightDir", "_RmHorizonY", "_RmFocalDp", "_RmViewDp",
            "_RmDensity", "_RmZonePhase", "_RmSkyTopSrgb", "_RmSkyHorizonSrgb", "_RmHazeSrgb", "_RmEmissiveGain", "_RmSwell", "_RmUnderBridge",
        };

        // ── Sky ──
        public static readonly int MoonTex = Shader.PropertyToID("_RmMoonTex");
        public static readonly int GlowParams = Shader.PropertyToID("_RmGlowParams");
        public static readonly int StarField = Shader.PropertyToID("_RmStarField");
        public static readonly int MistBandDeg = Shader.PropertyToID("_RmMistBandDeg");

        // ── Stars (por instancia) ──
        public static readonly int StarDir = Shader.PropertyToID("_RmStarDir");
        public static readonly int StarAlpha = Shader.PropertyToID("_RmStarAlpha");

        // ── Clouds ──
        public static readonly int CloudAtlas = Shader.PropertyToID("_RmCloudAtlas");
        public static readonly int CloudLayer = Shader.PropertyToID("_RmCloudLayer");
        public static readonly int CloudDir = Shader.PropertyToID("_RmCloudDir");
        public static readonly int CloudVariant = Shader.PropertyToID("_RmCloudVariant");
        public static readonly int CloudAlpha = Shader.PropertyToID("_RmCloudAlpha");

        // ── Horizon ──
        public static readonly int HorizonTone = Shader.PropertyToID("_RmHorizonTone");
        public static readonly int HorizonDepth = Shader.PropertyToID("_RmHorizonDepth");
        public static readonly int SnowEnabled = Shader.PropertyToID("_RmSnowEnabled");
        public static readonly int HorizonScale = Shader.PropertyToID("_RmHorizonScale");
        public static readonly int SnowParams = Shader.PropertyToID("_RmSnowParams");
        public static readonly int Rim = Shader.PropertyToID("_RmRim");
        public static readonly int SnowTone = Shader.PropertyToID("_RmSnowTone");
        public static readonly int SkylineWindows = Shader.PropertyToID("_RmSkylineWindows");
        public static readonly int SkylineWindow = Shader.PropertyToID("_RmSkylineWindow");
        // QA-V8-01: las banderas de la tira lejana de la regata (el tono de la hora de cada una y el del mástil).
        public static readonly int[] FlagTones =
        {
            Shader.PropertyToID("_RmFlagTone0"), Shader.PropertyToID("_RmFlagTone1"), Shader.PropertyToID("_RmFlagTone2"), Shader.PropertyToID("_RmFlagTone3"),
        };

        public static readonly int FlagMast = Shader.PropertyToID("_RmFlagMast");

        // ── WaterDegraded ──
        public static readonly int WaterBandTop = Shader.PropertyToID("_RmWaterBandTop");
        public static readonly int WaterBandMid = Shader.PropertyToID("_RmWaterBandMid");
        public static readonly int WaterBandBottom = Shader.PropertyToID("_RmWaterBandBottom");
        public static readonly int Shimmer = Shader.PropertyToID("_RmShimmer");
        public static readonly int ShimmerColor = Shader.PropertyToID("_RmShimmerColor");
        public static readonly int WaterExtras = Shader.PropertyToID("_RmWaterExtras");
        public static readonly int ShallowColor = Shader.PropertyToID("_RmShallowColor");

        // ── Terrain ──
        public static readonly int NoiseTex = Shader.PropertyToID("_RmNoiseTex");
        public static readonly int TerrainPebbles = Shader.PropertyToID("_RmTerrainPebbles");
        public static readonly int TerrainWet = Shader.PropertyToID("_RmTerrainWet");
        public static readonly int TerrainContact = Shader.PropertyToID("_RmTerrainContact");
        public static readonly int TerrainGrass = Shader.PropertyToID("_RmTerrainGrass");
        public static readonly int TerrainGrassAlt = Shader.PropertyToID("_RmTerrainGrassAlt");
        public static readonly int TerrainForest = Shader.PropertyToID("_RmTerrainForest");
        public static readonly int TerrainNeedles = Shader.PropertyToID("_RmTerrainNeedles");
        public static readonly int TerrainRock = Shader.PropertyToID("_RmTerrainRock");
        public static readonly int TerrainCanopy = Shader.PropertyToID("_RmTerrainCanopy");
        public static readonly int LayerAlbedo = Shader.PropertyToID("_RmLayerAlbedo");
        public static readonly int LayerNormal = Shader.PropertyToID("_RmLayerNormal");
        public static readonly int TerrainKind = Shader.PropertyToID("_RmTerrainKind");
        public static readonly int TerrainCobble = Shader.PropertyToID("_RmTerrainCobble");
        public static readonly int TerrainWall = Shader.PropertyToID("_RmTerrainWall");
        public static readonly int TerrainSand = Shader.PropertyToID("_RmTerrainSand");
        public static readonly int TerrainCliff = Shader.PropertyToID("_RmTerrainCliff");
        public static readonly int TerrainCliffGrass = Shader.PropertyToID("_RmTerrainCliffGrass");
        public static readonly int TerrainLawn = Shader.PropertyToID("_RmTerrainLawn");

        // ── Prop ──
        public static readonly int PlankParams = Shader.PropertyToID("_RmPlankParams");
        public static readonly int WindowDay = Shader.PropertyToID("_RmWindowDay");
        public static readonly int WindowNight = Shader.PropertyToID("_RmWindowNight");
        public static readonly int WindowMode = Shader.PropertyToID("_RmWindowMode");
        public static readonly int Fade = Shader.PropertyToID("_RmFade");
        public static readonly int Lit = Shader.PropertyToID("_RmLit");

        // ── Foliage, Grass, Reeds ──
        public static readonly int TrunkColor = Shader.PropertyToID("_RmTrunkColor");
        public static readonly int BushColor = Shader.PropertyToID("_RmBushColor");
        public static readonly int Wind = Shader.PropertyToID("_RmWind");
        public static readonly int Variant = Shader.PropertyToID("_RmVariant");
        public static readonly int WindPhase = Shader.PropertyToID("_RmWindPhase");
        public static readonly int GrassColorA = Shader.PropertyToID("_RmGrassColorA");
        public static readonly int GrassColorB = Shader.PropertyToID("_RmGrassColorB");
        public static readonly int FernColor = Shader.PropertyToID("_RmFernColor");
        public static readonly int GrassEnd = Shader.PropertyToID("_RmGrassEnd");
        public static readonly int Tint = Shader.PropertyToID("_RmTint");
        public static readonly int ReedBase = Shader.PropertyToID("_RmReedBase");
        public static readonly int ReedTip = Shader.PropertyToID("_RmReedTip");

        // ── Hull, BoatShadow, ScreenOverlay ──
        public static readonly int Gelcoat = Shader.PropertyToID("_RmGelcoat");
        public static readonly int ShadowOffset = Shader.PropertyToID("_RmShadowOffset");
        public static readonly int ShadowAlpha = Shader.PropertyToID("_RmShadowAlpha");
        public static readonly int ScrimColor = Shader.PropertyToID("_RmScrimColor");
        public static readonly int ScrimTop = Shader.PropertyToID("_RmScrimTop");
        public static readonly int ScrimBottom = Shader.PropertyToID("_RmScrimBottom");

        // Las propiedades de material y de instancia de cada shader (las de ShaderArt.Scene): SceneAssetsTests las compara.
        public static readonly string[] MaterialPropertyNames =
        {
            "_RmMoonTex", "_RmGlowParams", "_RmStarField", "_RmMistBandDeg", "_RmCloudAtlas", "_RmCloudLayer", "_RmHorizonTone", "_RmHorizonDepth",
            "_RmSnowEnabled", "_RmHorizonScale", "_RmSnowParams", "_RmRim", "_RmSnowTone", "_RmWaterBandTop", "_RmWaterBandMid", "_RmWaterBandBottom",
            "_RmShimmer", "_RmShimmerColor", "_RmWaterExtras", "_RmShallowColor", "_RmNoiseTex", "_RmTerrainPebbles", "_RmTerrainWet",
            "_RmTerrainContact", "_RmTerrainGrass", "_RmTerrainGrassAlt", "_RmTerrainForest", "_RmTerrainNeedles", "_RmTerrainRock",
            "_RmTerrainCanopy", "_RmLayerAlbedo", "_RmLayerNormal", "_RmPlankParams", "_RmWindowDay", "_RmWindowNight", "_RmWindowMode",
            "_RmTrunkColor", "_RmBushColor", "_RmWind", "_RmGrassColorA", "_RmGrassColorB", "_RmFernColor", "_RmGrassEnd", "_RmReedBase",
            "_RmReedTip", "_RmGelcoat", "_RmShadowOffset", "_RmShadowAlpha", "_RmScrimColor", "_RmScrimTop", "_RmScrimBottom",
            // It. 8 (Horizon, Terrain).
            "_RmSkylineWindows", "_RmSkylineWindow", "_RmTerrainKind", "_RmTerrainCobble", "_RmTerrainWall", "_RmTerrainSand", "_RmTerrainCliff",
            "_RmTerrainCliffGrass", "_RmTerrainLawn", "_RmFlagTone0", "_RmFlagTone1", "_RmFlagTone2", "_RmFlagTone3", "_RmFlagMast",
        };

        public static readonly string[] InstancePropertyNames =
        {
            "_RmStarDir", "_RmStarAlpha", "_RmCloudDir", "_RmCloudVariant", "_RmCloudAlpha", "_RmFade", "_RmLit", "_RmVariant", "_RmWindPhase", "_RmTint",
        };

        // ── It. 7 (07-remero-palada/02-tech-design §7.4; WP-B0): el remero, los remos, la salpicadura, el agua que responde y la boya.
        // Van aparte de los 13 de la 6a (ShaderNames = ShaderArt.Scene): los cubre RowerMaterialsTests y la colección de variantes. ──
        public const string Rower = "RowMate/Scene/Rower";
        public const string Oar = "RowMate/Scene/Oar";
        public const string Spray = "RowMate/Scene/Spray";
        public const string WaterMarks = "RowMate/Scene/WaterMarks";
        public const string Buoy = "RowMate/Scene/Buoy";

        public static readonly string[] RowerShaderNames = { Rower, Oar, Spray, WaterMarks, Buoy };

        // Rower: el color (lineal) y la suavidad de las 9 regiones (RowerArt) y el relleno del frente.
        public static readonly int RowerColors = Shader.PropertyToID("_RmRowerColors");
        public static readonly int RimFill = Shader.PropertyToID("_RmRimFill");

        // Oar: las 5 partes, la flexión (por remo, MaterialPropertyBlock) y el ancho mínimo de la caña.
        public static readonly int OarColors = Shader.PropertyToID("_RmOarColors");
        public static readonly int OarLoad = Shader.PropertyToID("_RmOarLoad");
        public static readonly int MinWidthDp = Shader.PropertyToID("_RmMinWidthDp");
        public static readonly int PixelSize = Shader.PropertyToID("_RmPixelSize");

        // Spray (por instancia): (tamaño; alfa; tipo; largo del trazo); el tamaño mínimo de una gota.
        public static readonly int SprayParams = Shader.PropertyToID("_RmSpray");
        public static readonly int MinDropDp = Shader.PropertyToID("_RmMinDropDp");

        // WaterMarks (por instancia): (edad; giro; alfa; tipo); el esfuerzo.
        public static readonly int Marks = Shader.PropertyToID("_RmMarks");
        public static readonly int Effort = Shader.PropertyToID("_RmEffort");

        // Buoy: el naranja (el fundido es _RmFade).
        public static readonly int BuoyColor = Shader.PropertyToID("_RmBuoyColor");

        // ── It. 8 (08-entornos-selector/02-tech-design §8.4; WP-D1): las luces de la noche de los escenarios y la espuma de la costa
        // (ShaderArt.Environment). Aparte de los 13 de la 6a y los 5 de la 7: los cubre la colección de variantes y SceneShaderTests. ──
        public const string Glow = "RowMate/Scene/Glow";
        public const string Foam = "RowMate/Scene/Foam";

        public static readonly string[] EnvironmentShaderNames = { Glow, Foam };

        // Foam (por instancia): (código; z₀ mod 256; ancho; fundido); el latido y el color.
        public static readonly int FoamInstance = Shader.PropertyToID("_RmFoam");
        public static readonly int FoamPulse = Shader.PropertyToID("_RmFoamPulse");
        public static readonly int FoamColor = Shader.PropertyToID("_RmFoamColor");

        // Glow (por instancia): (radio o largo; alfa × nivel de la luz; tipo 0 farol / 1 faro / 2 haz; fase del haz).
        public static readonly int GlowInstance = Shader.PropertyToID("_RmGlow");
        public static readonly int GlowLamp = Shader.PropertyToID("_RmGlowLamp");
        public static readonly int GlowBeacon = Shader.PropertyToID("_RmGlowBeacon");
        public static readonly int GlowBeam = Shader.PropertyToID("_RmGlowBeam");

        // ── It. 6b (06b-agua-avanzada/02-tech-design §3.4, §4; ShaderArt.WaterShaders, WaterGlobals): el agua avanzada (3 variantes por
        // palabra clave del material), sus globales, la estela y las columnas como tipos de WaterMarks, y el vidrio desenfocado. ──
        public const string WaterAdvanced = "RowMate/Scene/WaterAdvanced";
        public const string HudBlur = "RowMate/UI/HudBlur";
        public const string WaterHighKeyword = "RM_WATER_HIGH";
        public const string WaterLiteKeyword = "RM_WATER_LITE";
        public const string WaterTag = "RmWater";
        public const string WaterTagValue = "Advanced";

        // En la colección de variantes, aparte de los de la 6a, la 7 y la 8: las tres variantes del agua (el vidrio no es de la cámara de la
        // escena: lo lleva su material en SceneWorld.asset).
        public static readonly string[] WaterShaderNames = { WaterAdvanced };

        public static readonly int WaterNormal = Shader.PropertyToID("_RmWaterNormal");
        public static readonly int HorizonProfileTex = Shader.PropertyToID("_RmHorizonProfile");
        public static readonly int HullFootprint = Shader.PropertyToID("_RmHullFootprint");
        public static readonly int ReflectionMix = Shader.PropertyToID("_RmReflectionMix");
        public static readonly int WaterBands = Shader.PropertyToID("_RmWaterBands");
        public static readonly int WaterLook = Shader.PropertyToID("_RmWaterLook");
        public static readonly int HorizonReflectTone = Shader.PropertyToID("_RmHorizonTone");
        public static readonly int HorizonReflectTan = Shader.PropertyToID("_RmHorizonTan");

        // WP-D (It. 6b): la banda de orilla reflejada (00-arte-agua §4.2; PA6b-06) y el carácter del escenario (calma y turbidez).
        public static readonly int ShoreGeometry = Shader.PropertyToID("_RmShoreGeo");
        public static readonly int ShoreLowGeometry = Shader.PropertyToID("_RmShoreLowGeo");
        public static readonly int ShoreLeftTone = Shader.PropertyToID("_RmShoreLeft");
        public static readonly int ShoreRightTone = Shader.PropertyToID("_RmShoreRight");
        public static readonly int ShoreLowLeftTone = Shader.PropertyToID("_RmShoreLowLeft");
        public static readonly int ShoreLowRightTone = Shader.PropertyToID("_RmShoreLowRight");
        public static readonly int WaterCharacter = Shader.PropertyToID("_RmWaterCharacter");

        public static readonly int WaterPhases = Shader.PropertyToID("_RmWaterPhases");
        public static readonly int WaterOffsets = Shader.PropertyToID("_RmWaterOffsets");
        public static readonly int WaterOffsets2 = Shader.PropertyToID("_RmWaterOffsets2");
        public static readonly int WaterLift = Shader.PropertyToID("_RmWaterLift");
        public static readonly int WaterReflectionTex = Shader.PropertyToID("_RmWaterReflectionTex");
        public static readonly int DegradedBlend = Shader.PropertyToID("_RmDegradedBlend");
        public static readonly int Wake = Shader.PropertyToID("_RmWake");
        public static readonly int WakeFade = Shader.PropertyToID("_RmWakeFade");

        // WaterMarks tipo 4 (la columna de luz): su color por instancia.
        public static readonly int MarkTint = Shader.PropertyToID("_RmMarkTint");

        // HudBlur.
        public static readonly int MainTex = Shader.PropertyToID("_MainTex");
        public static readonly int BlurOffset = Shader.PropertyToID("_RmBlurOffset");
        public static readonly int GlassAlpha = Shader.PropertyToID("_RmGlassAlpha");
        public static readonly int GlassTint = Shader.PropertyToID("_RmGlassTint");

        // La capa de la escena (la cámara de la UI no la ve): la crea UrpProvisioner.
        public const string LayerName = "RowMateScene";
    }
}
