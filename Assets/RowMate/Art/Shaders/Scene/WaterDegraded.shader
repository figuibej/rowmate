// RowMate/Scene/WaterDegraded: el agua degradada del legacy (water_painter.dart:79-103; 06a-escena-lago/04-design-notes §8.1;
// 02-tech-design §2.12, §3.6, §7.5; AC-SCN-13-02 a -04; DT6a-07: ≤ 24 ALU y 0 texturas; WP-D1).
// El plano de WaterArt (1,6 × 1,5 km centrado en la cámara, con la grilla GridX × GridZ) en la cola 2450, opaco. Lo del legacy, en
// coordenadas de pantalla contra el horizonte real del cuadro (el que dan las matrices de la cámara: con la respiración, el vuelo y
// la toma aérea las líneas siguen al agua):
//   1. los tres tramos de arriba a abajo: lerp(waterFar; fog; 0,5) → waterFar → waterNear (_RmWaterBand*: DegradedWater.Bands, en
//      lineal), en tp = (y − horizonte)/(alto − horizonte) 0, 0,5 y 1;
//   2. las 10 líneas: la más cercana al píxel en O(1), t* = fase + n·0,1 (n = redondeo de (√tp − fase)/0,1, sin bajar de 0), en
//      y = horizonte + alto del agua·t*², de 1,5 dp de grueso con 0,5 dp de borde, medio ancho ancho·(0,15 + 0,35·t*) centrado en la
//      x del astro que se ve, color sunColor con alfa (0,05 + 0,1·(1 − t*)·fuerza)·vis (DegradedWater.ScreenAlpha, DV-59, DN6a-06).
//      _RmShimmer = (fase de Core en double, Mod(d·0,02; 0,1); x del disco en dp de la ventana del contrato; fuerza del legacy; vis).
//      El shader centra las líneas en la x del disco del cuadro (la de _RmDiskDir proyectada), que con la cámara del contrato es la
//      misma que _RmShimmer.y;
//   3. con RM_WATER_EXTRAS (prendida en los tres niveles): el reflejo leve del cielo (0,25·S(30; 400; z) hacia skyHorizon), la franja
//      somera (0,25·S(8,4; 9,0; |x|) hacia #5A6656·ambiente) y la bruma baja (0,6·m·S(10; 150; z) hacia lerp(fog; sunGlow; 0,25·g)),
//      pesados en el vértice (la grilla tiene filas y columnas donde cambian) y doblados en un solo c·k + C en el fragmento.
// Todo en gamma 2 (√ del lineal; al cuadrado al final). Sin niebla propia, sin olas ni estela (AC-SCN-13-01): eso es de la 6b.
Shader "RowMate/Scene/WaterDegraded"
{
    Properties
    {
        _RmWaterBandTop ("Tramo de arriba (lerp(waterFar; fog; 0,5))", Color) = (0.5, 0.74, 0.92, 1)
        _RmWaterBandMid ("Tramo del medio (waterFar)", Color) = (0.18, 0.56, 0.84, 1)
        _RmWaterBandBottom ("Tramo de abajo (waterNear)", Color) = (0.05, 0.31, 0.56, 1)
        _RmShimmer ("Líneas (fase; x del disco en dp; fuerza; visibilidad del astro)", Vector) = (0, 0, 1, 1)
        _RmShimmerColor ("Color de las líneas (sunColor)", Color) = (1, 0.96, 0.84, 1)
        _RmWaterExtras ("Agregados (reflejo; somera; bruma; -)", Vector) = (0.25, 0.25, 0.6, 0)
        _RmShallowColor ("Franja somera (× ambiente)", Color) = (0.352941, 0.4, 0.337255, 1)
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "AlphaTest" "IgnoreProjector" = "True" }

        HLSLINCLUDE
        #include "../Include/RowMateScene.hlsl"
        #include "../Include/RowMateFog.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _RmWaterBandTop;
            float4 _RmWaterBandMid;
            float4 _RmWaterBandBottom;
            float4 _RmShimmer;
            float4 _RmShimmerColor;
            float4 _RmWaterExtras;
            float4 _RmShallowColor;
        CBUFFER_END

        static const float kRmLineSpacing = 0.1;
        static const float kRmLineHalfWidthBase = 0.15;
        static const float kRmLineHalfWidthSlope = 0.35;
        static const float kRmShimmerBase = 0.05;
        static const float kRmShimmerGain = 0.1;
        static const float kRmLineHalfThicknessDp = 0.75;
        static const float kRmLineSoftDp = 0.5;
        static const float kRmReflectNear = 30;
        static const float kRmReflectFar = 400;
        static const float kRmShallowInner = 8.4;
        static const float kRmShallowOuter = 9;

        // DegradedWater.ScreenAlpha: lo que se ve de una línea = el alfa del legacy × la visibilidad del astro (0 en el cambio de astro).
        float RmShimmerScreenAlpha(float legacyAlpha, float diskVisibility)
        {
            return legacyAlpha * diskVisibility;
        }

        // La y de pantalla en dp (desde arriba) de una dirección en el infinito, con las matrices del cuadro.
        float RmScreenYDp(float3 direction)
        {
            float4 c = mul(UNITY_MATRIX_VP, float4(direction, 0.0));
            return _RmViewDp.y * (0.5 - 0.5 * _ProjectionParams.x * c.y / c.w);
        }

        float RmScreenXDp(float3 direction)
        {
            float4 c = mul(UNITY_MATRIX_VP, float4(direction, 0.0));
            return _RmViewDp.x * (0.5 + 0.5 * c.x / c.w);
        }
        ENDHLSL

        Pass
        {
            Name "Water"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex WaterVertex
            #pragma fragment WaterFragment
            #pragma shader_feature_local RM_WATER_EXTRAS

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 screen : TEXCOORD0;      // (2·tp·w; 2·(x − x del disco)·w en dp; −; w): afines en el clip, se dividen por w
                float3 top : TEXCOORD1;         // el tramo de arriba en gamma 2
                float3 d1 : TEXCOORD2;          // del medio − de arriba
                float3 d2 : TEXCOORD3;          // de abajo − del medio
                float3 sun : TEXCOORD4;         // sunColor en gamma 2
                float4 line0 : TEXCOORD5;       // (−fase/0,1; fase; 2·(alto del agua) en dp; −)
                float4 line1 : TEXCOORD6;       // (0,7·ancho; 0,3·ancho + 0,5; alfa en t = 0; caída del alfa con t)
                float4 extra : TEXCOORD7;       // los agregados: c·w + xyz
            };

            Varyings WaterVertex(Attributes v)
            {
                Varyings o;
                float3 positionWS = TransformObjectToWorld(v.positionOS.xyz);
                float4 clip = TransformWorldToHClip(positionWS);
                o.positionCS = clip;

                // El horizonte del cuadro (la columna del centro) y la x del disco, con las matrices de la cámara.
                float3 forward = -UNITY_MATRIX_V[2].xyz;
                forward = normalize(float3(forward.x, 0.0, forward.z) + float3(0.0, 0.0, 1e-5));
                float horizon = RmScreenYDp(forward);
                float waterHeight = max(_RmViewDp.y - horizon, 1.0);
                float diskX = RmScreenXDp(normalize(float3(_RmDiskDir.x, 0.0, _RmDiskDir.z) + float3(0.0, 0.0, 1e-5)));

                // y y x de pantalla en dp, multiplicadas por w (afines en el clip: se interpolan bien y el fragmento divide).
                float yW = _RmViewDp.y * (0.5 * clip.w - 0.5 * _ProjectionParams.x * clip.y);
                float xW = _RmViewDp.x * (0.5 * clip.x + 0.5 * clip.w);
                o.screen = float4(2.0 * (yW - horizon * clip.w) / waterHeight, 2.0 * (xW - diskX * clip.w), 0.0, clip.w);

                float3 top = sqrt(max(_RmWaterBandTop.rgb, 0.0));
                float3 mid = sqrt(max(_RmWaterBandMid.rgb, 0.0));
                float3 bottom = sqrt(max(_RmWaterBandBottom.rgb, 0.0));
                o.top = top;
                o.d1 = mid - top;
                o.d2 = bottom - mid;
                o.sun = sqrt(max(_RmShimmerColor.rgb, 0.0));

                float phase = _RmShimmer.x;
                float width = _RmViewDp.x;
                o.line0 = float4(-phase / kRmLineSpacing, phase, 2.0 * waterHeight / (2.0 * kRmLineSoftDp), 0.0);
                float strength = _RmShimmer.z;
                float vis = _RmShimmer.w;
                o.line1 = float4(
                    2.0 * kRmLineHalfWidthSlope * width / (2.0 * kRmLineSoftDp),
                    2.0 * kRmLineHalfWidthBase * width / (2.0 * kRmLineSoftDp) + 0.5,
                    RmShimmerScreenAlpha(kRmShimmerBase + kRmShimmerGain * strength, vis),
                    RmShimmerScreenAlpha(kRmShimmerGain * strength, vis));

#if defined(RM_WATER_EXTRAS)
                float z = positionWS.z;
                float wR = _RmWaterExtras.x * RmSmoothStep(kRmReflectNear, kRmReflectFar, z);
                float wS = _RmWaterExtras.y * RmSmoothStep(kRmShallowInner, kRmShallowOuter, abs(positionWS.x));
                float wM = _RmWaterExtras.z * _RmLowMist * RmSmoothStep(kRmMistNear, kRmMistFar, z);
                float3 sky = sqrt(max(_RmPaletteSkyHorizon.rgb, 0.0));
                float3 shallow = sqrt(max(_RmShallowColor.rgb * _RmAmbient, 0.0));
                float3 mist = sqrt(max(RmMistColor(), 0.0));
                // lerp(lerp(lerp(c; sky; wR); shallow; wS); mist; wM) = c·k + C.
                float k = (1.0 - wR) * (1.0 - wS) * (1.0 - wM);
                float3 add = ((sky * wR) * (1.0 - wS) + shallow * wS) * (1.0 - wM) + mist * wM;
                o.extra = float4(add, k);
#else
                o.extra = float4(0.0, 0.0, 0.0, 1.0);
#endif
                return o;
            }

            half4 WaterFragment(Varyings i) : SV_Target
            {
                float2 s = i.screen.xy / i.screen.w;     // (2·tp; 2·(x − x del disco))
                float tp = 0.5 * s.x;

                // 1: los tres tramos.
                float3 c = i.top + i.d1 * saturate(s.x) + i.d2 * saturate(s.x - 1.0);

                // 2: la línea más cercana.
                float n = max(round(sqrt(abs(tp)) / kRmLineSpacing + i.line0.x), 0.0);
                float t = n * kRmLineSpacing + i.line0.y;
                float coverY = saturate((kRmLineHalfThicknessDp + 0.5 * kRmLineSoftDp) / kRmLineSoftDp - abs(tp - t * t) * i.line0.z);
                float coverX = saturate(t * i.line1.x + i.line1.y - abs(s.y) / (2.0 * kRmLineSoftDp));
                float alpha = (i.line1.z - t * i.line1.w) * coverX * coverY;
                c = lerp(c, i.sun, alpha);

#if defined(RM_WATER_EXTRAS)
                // 3: los agregados.
                c = c * i.extra.w + i.extra.xyz;
#endif
                return half4(c * c, 1.0);
            }
            ENDHLSL
        }
    }
}
