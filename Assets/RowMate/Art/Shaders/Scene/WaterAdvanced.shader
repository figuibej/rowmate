// RowMate/Scene/WaterAdvanced: el agua avanzada de la 6b (F-SCN-12, -13; 06b-agua-avanzada/02-tech-design §4.1, §5; DT6b-03, -04, -12,
// -22; 00-arte-agua §2; 04-design-notes §2). El contrato es el de water.frag del legacy (shaders/water.frag:55-98), en cinta y en
// "gamma 2" como la degradada. Lo que es suave en el agua va en el VÉRTICE (la malla en anillos tiene 0,5-1 m de paso cerca):
//   el color base lerp(waterNear; waterFar; S(2; 120; z))·(1 + h·A·0,25) con la ola del legacy sin ruido (fases de Core, Q-68), la
//   absorción bajo la cámara, la turbidez del río, el peso del reflejo del legacy sobre la normal PLANA (0,6·(1 − cos)³, DT6b-03), la
//   niebla S(80; 400) y la luz bajo el puente, doblados en c = base·kB + cielo·kS + niebla; la columna de brillo del legacy
//   (exp(−(Δx/w)²)·0,4, w = 25 + 4z dp, × cresta × luz) y los parches de calma (el ruido de 256 m de Core, sin ver la distancia).
//   La malla queda quieta en cinta y lo que el paso del anillo no alcanza se apaga hacia su media (DT6b-27, ronda del salto:
//   WaterVertexField en Core es la referencia; el color b de la malla trae el paso máximo del lugar).
// El fragmento pone lo que cambia por píxel, en el orden del arte (§2):
//   1. la normal perturbada (B2; B3 en Alto y Medio; B4 en Alto) y el sombreado de las facetas cerca;
//   2. el cielo reflejado (el telón analítico); Alto: el planar (_RmWaterReflectionTex) más la banda de orilla (los pinos, la cabaña y
//      las fachadas son instancias que el espejo no dibuja: 04-design-notes §2.3); Medio: la silueta del horizonte (_RmHorizonProfile) y
//      la banda de orilla; Lite: solo el cielo (DT6b-05);
//   3. el brillo: la columna del legacy con sus destellos (piso en todos los niveles, DT6b-04) y, en Alto y Medio, el astro sobre la
//      normal perturbada (exponente 1/α², destellos en las facetas del rizo; el velo ancho solo en Alto), sin recorte (el bloom);
//   4. la costura del horizonte (los últimos 3 dp hacia skyHorizon);
//   5. en ramas: la espuma de las crestas de la costa (≤ 0,35), la línea de contacto del casco (que sigue el huso del casco) y el salto
//      degradado (DT6b-12: con _RmDegradedBlend > 0 dibuja también la degradada de la 6a y mezcla: el fundido de 0,6 s es un solo dibujo).
// SIN Fallback y con el tag RmWater = Advanced (DT6b-22, G6b-01): un equipo sin soporte no dibuja esto en silencio; lo detecta
// AdvancedWaterSurface.IsSupported y la degradada la dibuja su propio renderer.
Shader "RowMate/Scene/WaterAdvanced"
{
    Properties
    {
        _RmWaterNormal ("Mapa de normales (water.normalDetail: RG chop, BA rizo)", 2D) = "bump" {}
        _RmHorizonProfile ("Perfil del horizonte 256 × 1 (Medio)", 2D) = "black" {}
        _RmHullFootprint ("Huella del casco en cinta (popa; proa; semiancho + caída; −)", Vector) = (5.3, 13.7, 0.25, 0)
        _RmReflectionMix ("Peso del reflejo planar o de la silueta (LowerReflection lo baja a 0)", Float) = 1
        _RmWaterBands ("Bandas (tile B2; tile B3; fuerza B2; fuerza B3)", Vector) = (3.5, 1.3, 0.3, 0.2)
        _RmWaterLook ("Aspecto (rugosidad; distorsión; pendiente B1; espuma de crestas 0/1)", Vector) = (0.05, 0.035, 0.05, 0)
        _RmWaterCharacter ("Carácter (calma mínima; turbidez del río 0/1; −; −)", Vector) = (1, 0, 0, 0)
        _RmShoreGeo ("Banda de orilla (x izq.; alto izq.; x der.; alto der.)", Vector) = (10000, 0, 10000, 0)
        _RmShoreLowGeo ("Tramo bajo de la orilla (x izq.; alto izq.; x der.; alto der.)", Vector) = (10000, 0, 10000, 0)
        _RmShoreLeft ("Tono de la orilla izquierda (lineal)", Color) = (0.1, 0.15, 0.1, 1)
        _RmShoreRight ("Tono de la orilla derecha (lineal)", Color) = (0.1, 0.15, 0.1, 1)
        _RmShoreLowLeft ("Tono del tramo bajo izquierdo (lineal)", Color) = (0.2, 0.2, 0.2, 1)
        _RmShoreLowRight ("Tono del tramo bajo derecho (lineal)", Color) = (0.2, 0.2, 0.2, 1)
        _RmHorizonTone ("Tono de la silueta reflejada (lineal)", Color) = (0.2, 0.25, 0.3, 1)
        _RmHorizonTan ("tan Θ × k de la silueta (decodifica el perfil)", Float) = 0.05
        _RmWaterBandTop ("Degradada: tramo de arriba", Color) = (0.5, 0.74, 0.92, 1)
        _RmWaterBandMid ("Degradada: tramo del medio", Color) = (0.18, 0.56, 0.84, 1)
        _RmWaterBandBottom ("Degradada: tramo de abajo", Color) = (0.05, 0.31, 0.56, 1)
        _RmShimmer ("Líneas (fase; x del disco en dp; fuerza; visibilidad del astro)", Vector) = (0, 0, 1, 1)
        _RmShimmerColor ("Color de las líneas y del brillo (sunColor)", Color) = (1, 0.96, 0.84, 1)
        _RmWaterExtras ("Degradada: agregados (reflejo; somera; bruma; mar abierto a la derecha)", Vector) = (0.25, 0.25, 0.6, 0)
        _RmShallowColor ("Degradada: franja somera (× ambiente)", Color) = (0.352941, 0.4, 0.337255, 1)
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "AlphaTest" "IgnoreProjector" = "True" "RmWater" = "Advanced" }

        HLSLINCLUDE
        #include "../Include/RowMateScene.hlsl"
        #include "../Include/RowMateFog.hlsl"
        #include "../Include/RowMateLighting.hlsl"
        #include "../Include/RowMateWater.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _RmWaterNormal_ST;
            float4 _RmHullFootprint;
            float _RmReflectionMix;
            float4 _RmWaterBands;
            float4 _RmWaterLook;
            float4 _RmWaterCharacter;
            float4 _RmShoreGeo;
            float4 _RmShoreLowGeo;
            float4 _RmShoreLeft;
            float4 _RmShoreRight;
            float4 _RmShoreLowLeft;
            float4 _RmShoreLowRight;
            float4 _RmHorizonTone;
            float _RmHorizonTan;
            float4 _RmWaterBandTop;
            float4 _RmWaterBandMid;
            float4 _RmWaterBandBottom;
            float4 _RmShimmer;
            float4 _RmShimmerColor;
            float4 _RmWaterExtras;
            float4 _RmShallowColor;
        CBUFFER_END

        TEXTURE2D(_RmWaterNormal);
        SAMPLER(sampler_RmWaterNormal);
        TEXTURE2D(_RmHorizonProfile);
        SAMPLER(sampler_RmHorizonProfile);

        // La degradada (WaterDegraded, water_painter.dart:79-103): los mismos números (ShaderArt.Constants).
        static const float kRmLineSpacing = 0.1;
        static const float kRmLineHalfWidthBase = 0.15;
        static const float kRmLineHalfWidthSlope = 0.35;
        static const float kRmShimmerBase = 0.05;
        static const float kRmShimmerGain = 0.1;
        static const float kRmLineHalfThicknessDp = 0.75;
        static const float kRmLineSoftDp = 0.5;
        static const float kRmReflectNear = 30;
        static const float kRmReflectFar = 400;

        // WP-D (WaterLookArt, ShaderArt.WaterConstants): las capas de la avanzada.
        static const float kRmWaterAbsorb = 0.18;
        static const float kRmWaterAbsorbNear = 3;
        static const float kRmWaterAbsorbFar = 25;
        static const float kRmFacetShade = 0.8;
        static const float kRmRiverTurbidity = 0.2;
        static const float kRmShoreSoft = 2.0;
        static const float kRmShoreLowSoft = 0.25;
        static const float kRmGlintGain = 2.0;
        static const float kRmSheenGain = 0.1;
        static const float kRmSheenExponentScale = 0.06;
        static const float kRmSeamDp = 3;
        static const float kRmCalmWavesA = 6;
        static const float kRmCalmWavesB = 11;
        // DT6b-27 (WaterVertexField): el apagado de las ondas por el paso de la malla y lo que queda en su lugar.
        static const float kRmBandKeep = 1.0471976;
        static const float kRmBandDrop = 2.0943951;
        static const float kRmWave2K = 1.4764823;
        static const float kRmCalmKA = 0.1964336;
        static const float kRmCalmKB = 0.3420373;
        static const float kRmCrestMean = 0.3753837;
        static const float kRmFoamCrestMean = 0.1069487;
        static const float kRmFoamOctave = 2;
        static const float kRmRingMaxStep = 256;
        // #3E5A3A (WaterLookArt.RiverTurbidTone) en gamma 2: √ del lineal.
        static const float3 kRmRiverTurbidToneG = float3(0.2195, 0.3197, 0.2057);

        float RmScreenYDpOf(float3 direction)
        {
            float4 c = mul(UNITY_MATRIX_VP, float4(direction, 0.0));
            return _RmViewDp.y * (0.5 - 0.5 * _ProjectionParams.x * c.y / c.w);
        }

        float RmScreenXDpOf(float3 direction)
        {
            float4 c = mul(UNITY_MATRIX_VP, float4(direction, 0.0));
            return _RmViewDp.x * (0.5 + 0.5 * c.x / c.w);
        }

        // Cuánto queda de una onda con k·paso (WaterVertexField.Band).
        float RmBand(float kStep)
        {
            return 1.0 - RmSmoothStep(kRmBandKeep, kRmBandDrop, kStep);
        }
        ENDHLSL

        Pass
        {
            Name "WaterAdvanced"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex AdvancedVertex
            #pragma fragment AdvancedFragment
            #pragma shader_feature_local _ RM_WATER_HIGH RM_WATER_LITE

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;           // b: el paso máximo de la malla en ese lugar / 256 (WaterRingMesh.MaxStep)
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 uv23 : TEXCOORD0;        // las coordenadas de B2 y B3 en el mapa de normales
                float4 baseK : TEXCOORD1;       // xyz: el color base × (1 − F)(1 − niebla)·puente (gamma 2); w: el peso del cielo F·(1 − niebla)·puente
                float4 fogAdd : TEXCOORD2;      // xyz: la niebla × su peso × puente; w: la pendiente de B1 en z
                float4 view : TEXCOORD3;        // xyz: la dirección de la mirada (unitaria); w: la columna Δx/w
                float4 sunK : TEXCOORD4;        // xyz: sunColor × 0,9 × luz × (1 − niebla)·puente; w: la columna × cresta (0,4 en el pico)
                float4 fades : TEXCOORD5;       // (B2 × fuerza × calma; B3 × fuerza × calma; B4; espuma de crestas cerca)
                float4 skyH : TEXCOORD6;        // xyz: skyHorizon (gamma 2); w: el exponente del astro 1/α²
                float4 skyD : TEXCOORD7;        // xyz: skyTop − skyHorizon (gamma 2); w: z (cinta)
                float4 screen : TEXCOORD8;      // (y·w; x·w en dp; acimut de la mirada en u del perfil; w): afines en el clip
                float4 frame : TEXCOORD9;       // (horizonte en dp; alto del agua en dp; x del disco en dp; x del mundo)
                float4 degBand : TEXCOORD10;    // la degradada: xyz el tramo con el reflejo leve (gamma 2); w el alfa de la línea en t = 0
                float4 degSun : TEXCOORD11;     // la degradada: xyz sunColor (gamma 2); w lo que baja el alfa con t
            };

            Varyings AdvancedVertex(Attributes v)
            {
                Varyings o;
                float3 positionWS = TransformObjectToWorld(v.positionOS.xyz);
                // La ola del legacy (RmSwellHeight) en sus dos ondas: entera para la geometría (DT6b-21: en la costa la superficie sube con
                // el mar de fondo, y lo apoyado sube con la misma fórmula) y apagada por el paso de la malla (hb) para lo que se pinta.
                float s = max(_RmWaterPhases.w, 1e-3);
                float stepM = v.color.b * kRmRingMaxStep;
                float a1 = RmBand(stepM / s);
                float w1 = sin(positionWS.z / s + _RmWaterPhases.x);
                float w2 = sin((0.7 * positionWS.z + 1.3 * positionWS.x) / s + _RmWaterPhases.y);
                float h = w1 * 0.6 + w2 * 0.4;
                float hb = w1 * (0.6 * a1) + w2 * (0.4 * RmBand(kRmWave2K * stepM / s));
                positionWS.y += _RmWaterLift * h;
                float4 clip = TransformWorldToHClip(positionWS);
                o.positionCS = clip;
                float z = max(positionWS.z - _WorldSpaceCameraPos.z, 0.1);
                float x = positionWS.x;

                // Lo suave: el color base (con la turbidez del río y la absorción bajo la cámara), el peso del reflejo, la niebla y la luz
                // bajo el puente (en gamma 2).
                float3 baseColor = lerp(sqrt(max(_RmPaletteWaterNear.rgb, 0.0)), sqrt(max(_RmPaletteWaterFar.rgb, 0.0)), RmSmoothStep(kRmWaterBaseNear, kRmWaterBaseFar, z));
                baseColor = lerp(baseColor, kRmRiverTurbidToneG * sqrt(_RmAmbient), kRmRiverTurbidity * _RmWaterCharacter.y);
                baseColor *= 1.0 + hb * _RmWaterPhases.z * kRmWaterColorGain;
                float f = RmReflectionWeight(z, _WorldSpaceCameraPos.y);
                baseColor *= 1.0 - kRmWaterAbsorb * (1.0 - f) * (1.0 - RmSmoothStep(kRmWaterAbsorbNear, kRmWaterAbsorbFar, z));
                float fog = RmSmoothStep(kRmWaterFogNear, kRmWaterFogFar, z);
                float bridge = RmUnderBridgeGamma2(positionWS, float3(0.0, 1.0, 0.0));
                float keep = (1.0 - fog) * bridge;
                o.baseK = float4(baseColor * (1.0 - f) * keep, f * keep);
                o.fogAdd = float4(sqrt(max(_RmPaletteFog.rgb, 0.0)) * fog * bridge, _RmWaterLook.z * a1 * cos(positionWS.z / s + _RmWaterPhases.x));

                // La mirada, la columna del legacy (en dp) y la cresta.
                float3 view = normalize(positionWS - _WorldSpaceCameraPos);
                float3 forward = -UNITY_MATRIX_V[2].xyz;
                forward = normalize(float3(forward.x, 0.0, forward.z) + float3(0.0, 0.0, 1e-5));
                float horizon = RmScreenYDpOf(forward);
                float diskX = RmScreenXDpOf(normalize(float3(_RmDiskDir.x, 0.0, _RmDiskDir.z) + float3(0.0, 0.0, 1e-5)));
                float yW = _RmViewDp.y * (0.5 * clip.w - 0.5 * _ProjectionParams.x * clip.y);
                float xW = _RmViewDp.x * (0.5 * clip.x + 0.5 * clip.w);
                float xDp = xW / clip.w;
                o.view = float4(view, (xDp - diskX) / (kRmColumnBaseDp + kRmColumnPerMeterDp * z));
                float light = _RmShimmer.z * _RmShimmer.w;
                o.sunK = float4(sqrt(max(_RmShimmerColor.rgb, 0.0)) * (0.9 * light) * keep,
                    kRmColumnPeak * lerp(kRmCrestMean, RmSmoothStep(0.3, 0.9, hb * 0.5 + 0.5), a1));

                // Los parches de calma (00-arte-agua §3.2): el ruido de 256 m que Core reduce en double (_RmWaterOffsets2.z), con 6 y 11
                // vueltas y un corrimiento lateral; la calma baja la fuerza de B2 y B3 (el espejo de los parches lisos).
                float u = 6.2831853 * (positionWS.z / 256.0 + _RmWaterOffsets2.z);
                float n = 0.5 + 0.25 * RmBand(kRmCalmKA * stepM) * sin(kRmCalmWavesA * u + 0.13 * x)
                    + 0.25 * RmBand(kRmCalmKB * stepM) * sin(kRmCalmWavesB * u - 0.21 * x + 1.7);
                float calm = lerp(_RmWaterCharacter.x, 1.0, RmSmoothStep(0.3, 0.7, n));

                // El mapa de normales en B2 y B3 (con su fundido por distancia y la fuerza del escenario).
                // B3 va girado 37° respecto de B2 (las dos tramas del mapa son periódicas y, alineadas, se leían como una grilla de rombos a 20-60 m).
                o.uv23 = float4(float2(x, positionWS.z) / _RmWaterBands.x + _RmWaterOffsets.xy,
                    float2(0.7986 * x - 0.6018 * positionWS.z, 0.6018 * x + 0.7986 * positionWS.z) / _RmWaterBands.y + _RmWaterOffsets.zw);
                o.fades = float4(2.0 * _RmWaterBands.z * calm * (1.0 - RmSmoothStep(60.0, 250.0, z)), 2.0 * _RmWaterBands.w * calm * (1.0 - RmSmoothStep(25.0, 80.0, z)),
                    0.2 * calm * (1.0 - RmSmoothStep(10.0, 25.0, z)), _RmWaterLook.w * kRmCrestFoamMax * (1.0 - RmSmoothStep(15.0, 150.0, z)) * keep);
                // w: el exponente del astro, 1/α² (DT6b-04), por material: el fragmento no lo recalcula.
                o.skyH = float4(sqrt(max(_RmPaletteSkyHorizon.rgb, 0.0)), 1.0 / max(_RmWaterLook.x * _RmWaterLook.x, 1e-3));
                o.skyD = float4(sqrt(max(_RmPaletteSkyTop.rgb, 0.0)) - o.skyH.xyz, z);
                o.screen = float4(yW, xW, (atan2(view.x, max(view.z, 1e-3)) * 57.29578 / 130.0 + 0.5) * clip.w, clip.w);
                o.frame = float4(horizon, max(_RmViewDp.y - horizon, 1.0), diskX, x);

                // Lo de la degradada del salto (DT6b-12) que es suave: el tramo (con tp del vértice) y el reflejo leve; los alfas de las líneas.
                float tp = (yW / clip.w - horizon) / o.frame.y;
                float3 top = sqrt(max(_RmWaterBandTop.rgb, 0.0));
                float3 mid = sqrt(max(_RmWaterBandMid.rgb, 0.0));
                float3 band = top + (mid - top) * saturate(2.0 * tp) + (sqrt(max(_RmWaterBandBottom.rgb, 0.0)) - mid) * saturate(2.0 * tp - 1.0);
                band = lerp(band, o.skyH.xyz, _RmWaterExtras.x * RmSmoothStep(kRmReflectNear, kRmReflectFar, z));
                o.degBand = float4(band, (kRmShimmerBase + kRmShimmerGain * _RmShimmer.z) * _RmShimmer.w);
                o.degSun = float4(sqrt(max(_RmShimmerColor.rgb, 0.0)), kRmShimmerGain * _RmShimmer.z * _RmShimmer.w);
                // La espuma de las crestas ya con la cresta de la ola (la de la costa): un umbral angosto, se apaga una octava antes.
                o.fades.w *= lerp(kRmFoamCrestMean, RmSmoothStep(0.55, 0.85, hb), RmBand(kRmFoamOctave * kRmWave2K * stepM / s));
                return o;
            }

            half4 AdvancedFragment(Varyings i) : SV_Target
            {
                // 1: la normal perturbada (como pendientes; la del mapa va de −1 a 1).
                float4 m2 = SAMPLE_TEXTURE2D(_RmWaterNormal, sampler_RmWaterNormal, i.uv23.xy);
                float2 slope = (m2.rg - 0.5) * i.fades.x;
                float sparkle = m2.b;
#if !defined(RM_WATER_LITE)
                float4 m3 = SAMPLE_TEXTURE2D(_RmWaterNormal, sampler_RmWaterNormal, i.uv23.zw);
                slope += (m3.rg - 0.5) * i.fades.y;
                sparkle = m3.b;
#endif
#if defined(RM_WATER_HIGH)
                float4 m4 = SAMPLE_TEXTURE2D(_RmWaterNormal, sampler_RmWaterNormal, i.uv23.zw * 2.5 + _RmWaterOffsets2.xy);
                slope += (m4.ba - 0.5) * i.fades.z;
#endif
                slope.y += i.fogAdd.w;

                // 2: el cielo reflejado. Con pendientes chicas, la y del reflejo de la mirada es −v.y − 2·(pendiente · v.xz).
                float reflectedY = -i.view.y - 2.0 * dot(slope, i.view.xz);
                float3 sky = i.skyH.xyz + i.skyD.xyz * RmBackdropGradient(saturate(reflectedY * 3.0));
#if defined(RM_WATER_HIGH)
                float4 planar = SAMPLE_TEXTURE2D(_RmWaterReflectionTex, sampler_RmWaterReflectionTex, i.positionCS.xy / _ScaledScreenParams.xy + slope * _RmWaterLook.y);
                sky = lerp(sky, sqrt(max(planar.rgb, 0.0)), planar.a * _RmReflectionMix);
#elif !defined(RM_WATER_LITE)
                float profile = SAMPLE_TEXTURE2D(_RmHorizonProfile, sampler_RmHorizonProfile, float2(i.screen.z / i.screen.w + slope.x * 0.1, 0.5)).r;
                sky = lerp(sky, sqrt(_RmHorizonTone.rgb), step(reflectedY, profile * 2.0 * _RmHorizonTan) * _RmReflectionMix);
#endif
                float rx = i.view.x - 2.0 * slope.x;
#if !defined(RM_WATER_LITE)
                // La banda de orilla (00-arte-agua §4.2): el rayo reflejado cruza |x| = X a la distancia d (en x); la cubre si su alto
                // allí, reflectedY·d/|rx|, queda debajo del de la orilla. En Alto, además, el tramo bajo (muro o talud), más cerca y más
                // duro (en Medio no entra en el tope de 70: el planar no lo tiene, y la silueta de Medio ya da la orilla lejana).
                bool right = rx >= 0.0;
                float side = right ? -i.frame.w : i.frame.w;
                float k = reflectedY / max(abs(rx), 1e-3);
                float2 geo = right ? _RmShoreGeo.zw : _RmShoreGeo.xy;
                float3 tone = right ? _RmShoreRight.rgb : _RmShoreLeft.rgb;
                sky = lerp(sky, sqrt(tone), saturate((geo.y - k * (geo.x + side)) / kRmShoreSoft) * _RmReflectionMix);
#if defined(RM_WATER_HIGH)
                float2 low = right ? _RmShoreLowGeo.zw : _RmShoreLowGeo.xy;
                float3 lowTone = right ? _RmShoreLowRight.rgb : _RmShoreLowLeft.rgb;
                sky = lerp(sky, sqrt(lowTone), saturate((low.y - k * (low.x + side)) / kRmShoreLowSoft) * _RmReflectionMix);
#endif
#endif
                // Las facetas cerca: la pendiente hacia el astro aclara y la contraria oscurece (el agua bajo la cámara, con F ≈ 0, deja de
                // ser un piso liso).
#if defined(RM_WATER_LITE)
                float3 c = i.baseK.xyz + sky * i.baseK.w + i.fogAdd.xyz;
#else
                float3 c = i.baseK.xyz * (1.0 + kRmFacetShade * dot(slope, _RmDiskDir.xz)) + sky * i.baseK.w + i.fogAdd.xyz;
#endif

                // 3: la columna del legacy con sus destellos (el piso, DT6b-04); en Alto y Medio, el astro sobre la normal perturbada.
                float mask = saturate(2.0 * sparkle - 0.9);
                float glitter = exp(-i.view.w * i.view.w) * i.sunK.w * (0.25 + 0.75 * mask);
#if !defined(RM_WATER_LITE)
                float3 r = float3(rx, reflectedY, i.view.z - 2.0 * slope.y);
                // Normalizado: cerca de la cámara las pendientes llegan a 0,3 y |r| pasa de 1 (sin esto el astro se "veía" en manchas
                // blancas por toda el agua).
                float rl = saturate(dot(r, _RmDiskDir.xyz) * rsqrt(dot(r, r)));
                float e = i.skyH.w;
                glitter += pow(rl, e) * (1.0 + kRmGlintGain * mask);
#if defined(RM_WATER_HIGH)
                glitter += pow(rl, e * kRmSheenExponentScale) * kRmSheenGain;
#endif
#endif
                c += i.sunK.xyz * glitter;

                // 4: la costura del horizonte: en los últimos 3 dp, hacia skyHorizon.
                c = lerp(c, i.skyH.xyz, saturate(1.0 - (i.screen.x / i.screen.w - i.frame.x) / kRmSeamDp));

                // 5: las ramas (la espuma de las crestas de la costa, la línea de contacto y el salto degradado).
                UNITY_BRANCH
                if (i.fades.w > 0.001)
                {
                    c = lerp(c, float3(0.96, 0.97, 0.96) * sqrt(_RmAmbient), saturate(2.0 * m2.a - 0.9) * i.fades.w);
                }

                // La huella va en z de cinta (la z desde la cámara + la de la cámara): con la cámara del contrato es lo mismo; en el vuelo y la
                // toma aérea, sin esto el huso quedaba corrido detrás de la popa (QA-V6b-15, el óvalo oscuro de CAP-29).
                float zTape = i.skyD.w + _WorldSpaceCameraPos.z;
                UNITY_BRANCH
                if (zTape > _RmHullFootprint.x && zTape < _RmHullFootprint.y)
                {
                    // El huso del casco: el semiancho de la huella × √(1,6·4t(1 − t)), así la línea sigue la popa y la proa afinadas (#85).
                    float t = (zTape - _RmHullFootprint.x) / (_RmHullFootprint.y - _RmHullFootprint.x);
                    float hw = _RmHullFootprint.z * sqrt(saturate(6.4 * (t - t * t)));
                    c *= lerp(1.0, kRmContactDarken, saturate(1.0 - abs(abs(i.frame.w) - hw) / kRmContactFalloff));
                }

                UNITY_BRANCH
                if (_RmDegradedBlend > 0.001)
                {
                    float2 sdp = i.screen.xy / i.screen.w;
                    float tp = (sdp.x - i.frame.x) / i.frame.y;
                    float n = RmDegradedLineIndex((sqrt(abs(tp)) - _RmShimmer.x) / kRmLineSpacing);
                    float t = n * kRmLineSpacing + _RmShimmer.x;
                    float coverY = saturate((kRmLineHalfThicknessDp + 0.5 * kRmLineSoftDp) / kRmLineSoftDp - abs(tp - t * t) * i.frame.y / kRmLineSoftDp);
                    float coverX = saturate((_RmViewDp.x * (kRmLineHalfWidthBase + kRmLineHalfWidthSlope * t) - abs(sdp.y - i.frame.z)) / kRmLineSoftDp + 0.5);
                    float alpha = (i.degBand.w - i.degSun.w * t) * coverX * coverY;
                    c = lerp(c, lerp(i.degBand.xyz, i.degSun.xyz, alpha), _RmDegradedBlend);
                }

                return half4(c * c, 1.0);
            }
            ENDHLSL
        }
    }
}
