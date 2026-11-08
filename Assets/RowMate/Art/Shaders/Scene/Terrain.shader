// RowMate/Scene/Terrain: el terreno procedural del lago (06a-escena-lago/04-design-notes §7.1, §7.5, §7.7; 02-tech-design §2.13,
// §3.6, §7.5; AC-SCN-15-07; WP-D1). Los trozos de TerrainChunkMesh (Core): posición de cinta (z − d), color de vértice r = oclusión,
// b = la distancia a la orilla / 300 (negativa bajo el agua), a = el peso de la copa lejana; la normal la recalcula WP-B1 al subir
// el trozo. En la cola 2000, opaco, con la luz de RowMateLighting y la niebla de RowMateFog.
// Las zonas (cada una con su color de Art, ShaderArt.MaterialDefaults): canto rodado en la playa (de la orilla a 1 m, con cantos de
// ± 12 % de luminancia por ruido), pasto en el talud y la pradera (de un verde al otro por un ruido de ≈ 4 m), suelo de bosque con
// agujas a la izquierda desde |x| 18, roca donde la pendiente pasa de 35°, la copa lejana (vegetation.canopyFar) oscurecida en los
// valles del ruido; encima, la franja húmeda (y de 0 a 0,35 m con ± 5 cm de ruido, que refleja un poco de skyHorizon) y la línea de
// contacto (y de 0 a 0,04 m, #C9D2D4 al 20 %·ambiente): ahí termina el agua, nada flota. Los pesos se arman en el vértice y se
// doblan en cinco coeficientes (c = A0 + A1·cantos + A2·pasto + A3·agujas + A4·copa), así el fragmento hace cuatro productos.
// El ruido (_RmNoiseTex, SkyTextures.Noise: R valor, G fBm grueso, B cantos, A fBm) se lee dos veces, a 1,3 m y a 7 m, en
// coordenadas de cinta con la fase de d de Core (_RmZonePhase): a 42 km no tiembla (Q-68).
// Con el Terrain Sample Asset Pack (RM_TERRAIN_LAYERS, fija en el proceso): las dos capas con más peso del Texture2DArray
// (_RmLayerAlbedo; la oclusión de _RmLayerNormal.a) en lugar de los colores procedurales.
Shader "RowMate/Scene/Terrain"
{
    Properties
    {
        _RmNoiseTex ("Ruido (256², RGBA, sin costura)", 2D) = "gray" {}
        _RmTerrainPebbles ("Canto rodado", Color) = (0.478431, 0.454902, 0.419608, 1)
        _RmTerrainWet ("Franja húmeda", Color) = (0.305882, 0.290196, 0.270588, 1)
        _RmTerrainContact ("Línea de contacto", Color) = (0.788235, 0.823529, 0.831373, 1)
        _RmTerrainGrass ("Pasto", Color) = (0.309804, 0.478431, 0.227451, 1)
        _RmTerrainGrassAlt ("Pasto (el otro verde)", Color) = (0.419608, 0.541176, 0.243137, 1)
        _RmTerrainForest ("Suelo de bosque", Color) = (0.290196, 0.231373, 0.172549, 1)
        _RmTerrainNeedles ("Agujas", Color) = (0.352941, 0.27451, 0.196078, 1)
        _RmTerrainRock ("Roca", Color) = (0.419608, 0.415686, 0.4, 1)
        _RmTerrainCanopy ("Copa lejana", Color) = (0.164706, 0.290196, 0.172549, 1)
        _RmLayerAlbedo ("Capas del pack: albedo (Texture2DArray)", 2DArray) = "" {}
        _RmLayerNormal ("Capas del pack: normal y oclusión (Texture2DArray)", 2DArray) = "" {}
        _RmTerrainKind ("Escenario (0 lago, 1 río, 2 costa, 3 regata; lo pone SceneWorld)", Float) = 0
        _RmTerrainCobble ("Adoquín del malecón (río)", Color) = (0.541176, 0.513726, 0.470588, 1)
        _RmTerrainWall ("Piedra del muro (río)", Color) = (0.619608, 0.619608, 0.619608, 1)
        _RmTerrainSand ("Arena (costa)", Color) = (0.847059, 0.764706, 0.627451, 1)
        _RmTerrainCliff ("Roca del acantilado (costa)", Color) = (0.364706, 0.290196, 0.258824, 1)
        _RmTerrainCliffGrass ("Pasto de la meseta (costa)", Color) = (0.333333, 0.419608, 0.184314, 1)
        _RmTerrainLawn ("Césped cortado (regata)", Color) = (0.368627, 0.54902, 0.243137, 1)
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry" "IgnoreProjector" = "True" }

        HLSLINCLUDE
        #include "../Include/RowMateScene.hlsl"
        #include "../Include/RowMateLighting.hlsl"
        #include "../Include/RowMateFog.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _RmNoiseTex_ST;
            float4 _RmTerrainPebbles;
            float4 _RmTerrainWet;
            float4 _RmTerrainContact;
            float4 _RmTerrainGrass;
            float4 _RmTerrainGrassAlt;
            float4 _RmTerrainForest;
            float4 _RmTerrainNeedles;
            float4 _RmTerrainRock;
            float4 _RmTerrainCanopy;
            float _RmTerrainKind;
            float4 _RmTerrainCobble;
            float4 _RmTerrainWall;
            float4 _RmTerrainSand;
            float4 _RmTerrainCliff;
            float4 _RmTerrainCliffGrass;
            float4 _RmTerrainLawn;
        CBUFFER_END

        TEXTURE2D(_RmNoiseTex);
        SAMPLER(sampler_RmNoiseTex);
        TEXTURE2D_ARRAY(_RmLayerAlbedo);
        SAMPLER(sampler_RmLayerAlbedo);
        TEXTURE2D_ARRAY(_RmLayerNormal);
        SAMPLER(sampler_RmLayerNormal);

        static const float kRmWetTop = 0.35;
        static const float kRmWetNoise = 0.05;
        static const float kRmWetReflect = 0.15;
        static const float kRmContactTop = 0.04;
        static const float kRmContactAlpha = 0.2;
        static const float kRmPebbleLum = 0.12;
        static const float kRmRockSlopeCos = 0.819152044;
        static const float kRmNoiseFineM = 1.3;
        static const float kRmNoiseCoarseM = 7;
        static const float kRmCanopyOcclusion = 0.35;
        static const float kRmForestFromX = 18;
        static const float kRmBeachWidth = 1;
        static const float kRmBankX = 9;
        // It. 8 (08-entornos-selector/04-design-notes §3): los suelos de río, costa y regata.
        static const float kRmCobbleM = 0.216666667;
        static const float kRmCobbleJoint = 0.3;
        static const float kRmCobbleTone = 0.12;
        static const float kRmLawnStripeM = 3;
        static const float kRmLawnStripe = 0.04;
        static const float kRmCoastGrassFrom = 3;
        static const float kRmSandWet = 0.62;

        struct Attributes
        {
            float4 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float4 color : COLOR;
        };
        ENDHLSL

        Pass
        {
            Name "Terrain"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex TerrainVertex
            #pragma fragment TerrainFragment
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma shader_feature_local RM_TERRAIN_LAYERS

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 a0 : TEXCOORD1;          // el color base de las zonas (lineal)
                float3 a1 : TEXCOORD2;          // × (cantos − 0,5)
                float3 a2 : TEXCOORD3;          // × fBm grueso (de un verde al otro)
                float3 a3 : TEXCOORD4;          // × ruido fino (las agujas)
                float3 a4 : TEXCOORD5;          // × fBm (los valles de la copa)
                float3 ambientG : TEXCOORD6;
                float3 keyG : TEXCOORD7;
                float4 fog : TEXCOORD8;
                float4 wet : TEXCOORD9;         // (reflejo de skyHorizon en gamma 2; peso de la franja húmeda por la zona)
#if defined(RM_TERRAIN_LAYERS)
                float4 layers : TEXCOORD10;     // (capa 1; capa 2; peso de la 2; -)
#endif
                float3 wetColor : TEXCOORD11;   // la franja húmeda del escenario (arena mojada en la costa)
                float2 pattern : TEXCOORD12;    // It. 8: (peso del adoquín, río; peso del césped con franjas, regata)
            };

            Varyings TerrainVertex(Attributes v)
            {
                Varyings o;
                float3 positionWS = TransformObjectToWorld(v.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.positionCS = TransformWorldToHClip(positionWS);
                o.positionWS = positionWS;

                float fromShore = v.color.b * 300.0;
                float ax = abs(positionWS.x);
                float grassW = RmSmoothStep(kRmBeachWidth - 0.2, kRmBeachWidth + 0.2, fromShore);
                float forestW = positionWS.x < 0.0 ? RmSmoothStep(kRmForestFromX - 1.0, kRmForestFromX + 1.0, ax) : 0.0;
                float rockW = 1.0 - RmSmoothStep(kRmRockSlopeCos - 0.05, kRmRockSlopeCos + 0.05, normalWS.y);
                // It. 8: los colores de cada escenario (el lago queda igual). Río: adoquín en el malecón y la vereda, piedra del muro en lo
                // vertical. Costa: arena abajo, pasto de la meseta desde 3 m, roca del acantilado en lo empinado. Regata: césped cortado
                // con franjas y la escollera de canto rodado al borde del agua. Sin bosque fuera del lago.
                float3 pebbles = _RmTerrainPebbles.rgb;
                float3 grass = _RmTerrainGrass.rgb;
                float3 grassAlt = _RmTerrainGrassAlt.rgb;
                float3 rock = _RmTerrainRock.rgb;
                float3 wetColor = _RmTerrainWet.rgb;
                float isRiver = 0.0;
                float isRegatta = 0.0;
                if (_RmTerrainKind > 0.5)
                {
                    forestW = 0.0;
                    if (_RmTerrainKind < 1.5)
                    {
                        isRiver = 1.0;
                        pebbles = _RmTerrainCobble.rgb;
                        grass = pebbles;
                        grassAlt = pebbles;
                        rock = _RmTerrainWall.rgb;
                    }
                    else if (_RmTerrainKind < 2.5)
                    {
                        grassW = RmSmoothStep(kRmCoastGrassFrom, kRmCoastGrassFrom + 1.5, positionWS.y);
                        pebbles = _RmTerrainSand.rgb;
                        grass = _RmTerrainCliffGrass.rgb;
                        grassAlt = _RmTerrainCliffGrass.rgb * (1.0 + kRmCobbleTone);
                        rock = _RmTerrainCliff.rgb;
                        wetColor = _RmTerrainSand.rgb * kRmSandWet;
                    }
                    else
                    {
                        isRegatta = 1.0;
                        grass = _RmTerrainLawn.rgb;
                        grassAlt = _RmTerrainLawn.rgb * (1.0 + kRmLawnStripe);
                    }
                }

                float canopyW = v.color.a;
                // Los pesos de cada zona (suman 1): la copa tapa todo, la roca lo que queda, y el resto es playa o ladera (pasto o
                // bosque).
                float wC = canopyW;
                float wR = rockW * (1.0 - wC);
                float rest = 1.0 - wC - wR;
                float wP = (1.0 - grassW) * rest;
                float wG = grassW * (1.0 - forestW) * rest;
                float wF = grassW * forestW * rest;
                o.a0 = wP * pebbles + wG * grass + wF * _RmTerrainForest.rgb + wR * rock
                     + wC * _RmTerrainCanopy.rgb * (1.0 - kRmCanopyOcclusion);
                o.a1 = wP * pebbles * (2.0 * kRmPebbleLum);
                o.a2 = wG * (grassAlt - grass);
                o.wetColor = wetColor;
                o.pattern = float2(isRiver * (wP + wG), isRegatta * wG);
                o.a3 = wF * (_RmTerrainNeedles.rgb - _RmTerrainForest.rgb);
                o.a4 = wC * _RmTerrainCanopy.rgb * kRmCanopyOcclusion;

                float3 ambientG, keyG;
                RmVertexLight(normalWS, 0.0, ambientG, keyG);
                float occlusion = pow(saturate(v.color.r), 1.1);
                o.ambientG = ambientG * occlusion;
                o.keyG = keyG * occlusion;
                o.fog = RmVertexFog(positionWS.z, positionWS.y);

                // La franja húmeda refleja un poco de skyHorizon por Fresnel (smoothness 0,55).
                float3 view = normalize(_WorldSpaceCameraPos - positionWS);
                float fresnel = pow(1.0 - saturate(dot(normalWS, view)), 5.0);
                o.wet = float4(sqrt(max(_RmPaletteSkyHorizon.rgb, 0.0)) * (kRmWetReflect * (0.2 + 0.8 * fresnel)), (1.0 - wC) * (1.0 - wR));
#if defined(RM_TERRAIN_LAYERS)
                // Las dos capas con más peso: 0 cantos, 1 pasto, 2 tierra de bosque, 3 roca.
                float second = wR > max(wG, wF) ? 3.0 : (wF > wG ? 2.0 : 1.0);
                float wSecond = max(max(wG, wF), wR);
                o.layers = float4(0.0, second, saturate(wSecond / max(wSecond + wP, 1e-4)), 0.0);
#endif
                return o;
            }

            half4 TerrainFragment(Varyings i) : SV_Target
            {
                float3 p = i.positionWS;
                float4 fine = SAMPLE_TEXTURE2D(_RmNoiseTex, sampler_RmNoiseTex, p.xz * (1.0 / kRmNoiseFineM) + float2(0.0, _RmZonePhase.x));
                float4 coarse = SAMPLE_TEXTURE2D(_RmNoiseTex, sampler_RmNoiseTex, p.xz * (1.0 / kRmNoiseCoarseM) + float2(0.0, _RmZonePhase.y));
#if defined(RM_TERRAIN_LAYERS)
                float2 uvLayer = p.xz * (1.0 / kRmNoiseFineM) + float2(0.0, _RmZonePhase.x);
                float3 l0 = SAMPLE_TEXTURE2D_ARRAY(_RmLayerAlbedo, sampler_RmLayerAlbedo, uvLayer, i.layers.x).rgb;
                float3 l1 = SAMPLE_TEXTURE2D_ARRAY(_RmLayerAlbedo, sampler_RmLayerAlbedo, uvLayer, i.layers.y).rgb;
                float ao = SAMPLE_TEXTURE2D_ARRAY(_RmLayerNormal, sampler_RmLayerNormal, uvLayer, i.layers.y).a;
                float3 albedo = lerp(l0, l1 * ao, i.layers.z);
#else
                float3 albedo = i.a0 + i.a1 * (fine.b - 0.5) + i.a2 * coarse.g + i.a3 * fine.r + i.a4 * coarse.a;
#endif
                // It. 8: el adoquín (aparejo trabado de 1,3/6 m con la fase de d: no nada) y las franjas del césped (3 m a lo largo del
                // canal), con el borde del ancho de un píxel y el contraste que se apaga cuando la piedra mide menos de 4 píxeles. El píxel sale de
                // la distancia (m por dp = distancia/focal; a lo largo de z, × distancia/alto por lo rasante), sin derivadas: es un salto por el
                // uniforme del escenario (el lago no lo paga).
                UNITY_BRANCH
                if (_RmTerrainKind > 0.5)
                {
                    float pixelM = distance(p, _WorldSpaceCameraPos) / max(_RmFocalDp, 1.0);
                    float grazing = distance(p, _WorldSpaceCameraPos) / max(abs(_WorldSpaceCameraPos.y - p.y), 0.5);
                    float2 cell = float2(p.x, p.z + _RmZonePhase.x * kRmNoiseFineM) / kRmCobbleM;
                    float row = floor(cell.y);
                    cell.x += 0.5 * (row - 2.0 * floor(row * 0.5));
                    float2 g = abs(frac(cell) - 0.5);
                    float w = max(pixelM * grazing / kRmCobbleM, 1e-3);
                    float joint = saturate((max(g.x, g.y) - 0.44) / w) * saturate(1.0 - 4.0 * w);
                    float tone = (RmHash((int)(row * 131.0 + floor(cell.x))) - 0.5) * kRmCobbleTone * saturate(1.0 - 4.0 * w);
                    albedo *= 1.0 + i.pattern.x * (tone - kRmCobbleJoint * joint);
                    float sx = p.x / (2.0 * kRmLawnStripeM);
                    float sw = max(pixelM / (2.0 * kRmLawnStripeM), 1e-3);
                    float stripe = saturate((abs(frac(sx) - 0.5) - 0.25) / sw + 0.5) * 2.0 - 1.0;
                    albedo *= 1.0 + i.pattern.y * kRmLawnStripe * stripe;
                }

                // La franja húmeda y la línea de contacto.
                float wet = saturate((kRmWetTop + (fine.r - 0.5) * (2.0 * kRmWetNoise) - p.y) / kRmWetNoise) * i.wet.w;
                albedo = lerp(albedo, i.wetColor, wet);
                float contact = saturate(1.0 - p.y / kRmContactTop) * (kRmContactAlpha * _RmAmbient);
                albedo = lerp(albedo, _RmTerrainContact.rgb, contact);

                float3 c = sqrt(albedo) * (i.ambientG + i.keyG * RmMainShadow(p)) + i.wet.rgb * wet;
                c *= RmUnderBridgeGamma2(p, float3(0.0, 1.0, 0.0));
                c = lerp(c, i.fog.rgb, i.fog.w);
                return half4(c * c, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DepthVertex
            #pragma fragment DepthFragment

            float4 DepthVertex(Attributes v) : SV_POSITION
            {
                return TransformObjectToHClip(v.positionOS.xyz);
            }

            half DepthFragment() : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
}
