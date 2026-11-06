// RowMate/Scene/Foliage: los pinos del legacy, el bosque cercano y las matas de la pradera (06a-escena-lago/04-design-notes §5.5,
// §7.3, §7.4, §10.1; 02-tech-design §3.6, §7.5; AC-SCN-15-06; WP-D1). Instanciado, en la cola 2000, opaco (sin alpha test: las
// copas son mallas opacas de lóbulos, DN6a-16).
// La malla de Core (PineMesh, también la receta "bosque") trae en el color de vértice r = la oclusión (0,55 adentro y abajo → 1 en
// las puntas), g = el peso del viento (altura²), b = la grada (0; 0,5; 1) y a = 1 follaje / 0 tronco. El color de cada grada es el
// verde del legacy de su paleta (shore_painter.dart:89-93: greens[0] abajo, [1] al medio, [2] arriba), con la paleta por instancia
// (_RmVariant.x = semilla mod 3; .y = la luminancia: 1 el legacy, 0,85 el bosque; .z = 1 las matas de arbustos, #4F6B35); el tronco
// #5D4037. La luz envolvente es 0,35 en el follaje (04-design-notes §10.1).
// El viento (uno solo, hacia +x): 0,03·sin(1,1t + fase) + una ráfaga de 0,012 con un ruido de valor de t/9 s, × el peso del
// vértice, como un giro desde la base (_RmWind; _RmWindPhase.x = x·0,13 en double desde quien instancia, .y = 1 o 1,5 las matas).
// t es el tiempo de escena: sigue en pausa y con reducir movimiento (es el mundo). _RmFade: RowMateDither.
Shader "RowMate/Scene/Foliage"
{
    Properties
    {
        _RmTrunkColor ("Tronco", Color) = (0.364706, 0.25098, 0.215686, 1)
        _RmBushColor ("Arbustos de la pradera", Color) = (0.309804, 0.419608, 0.207843, 1)
        _RmWind ("Viento (amplitud en rad; frecuencia en rad/s; ráfaga en rad; período de la ráfaga en s)", Vector) = (0.03, 1.1, 0.012, 9)
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry" "IgnoreProjector" = "True" }

        HLSLINCLUDE
        #include "../Include/RowMateScene.hlsl"
        #include "../Include/RowMateLighting.hlsl"
        #include "../Include/RowMateFog.hlsl"
        #include "../Include/RowMateDither.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _RmTrunkColor;
            float4 _RmBushColor;
            float4 _RmWind;
        CBUFFER_END

        // Los verdes del legacy en lineal, por paleta (semilla mod 3) y grada (LakeArt.PineGreens).
        static const float3 kRmPineGreens[9] =
        {
            float3(0.010960, 0.111932, 0.014444),
            float3(0.027321, 0.205079, 0.031896),
            float3(0.039546, 0.270498, 0.045186),
            float3(0.006995, 0.102242, 0.031896),
            float3(0.012983, 0.230740, 0.066626),
            float3(0.020289, 0.423268, 0.116971),
            float3(0.027321, 0.111932, 0.023153),
            float3(0.048172, 0.198069, 0.039546),
            float3(0.078187, 0.323143, 0.064803)
        };

        UNITY_INSTANCING_BUFFER_START(Props)
            UNITY_DEFINE_INSTANCED_PROP(float4, _RmVariant)     // (paleta 0 a 2; luminancia; arbusto 0/1; -)
            UNITY_DEFINE_INSTANCED_PROP(float4, _RmWindPhase)   // (fase en rad; escala de la amplitud; -; -)
            UNITY_DEFINE_INSTANCED_PROP(float, _RmFade)
        UNITY_INSTANCING_BUFFER_END(Props)

        struct Attributes
        {
            float4 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float4 color : COLOR;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        // La posición de mundo con el viento: un giro desde la base hacia +x, con el peso del vértice.
        float3 FoliagePosition(Attributes v)
        {
            float3 positionWS = TransformObjectToWorld(v.positionOS.xyz);
            float3 originWS = TransformObjectToWorld(float3(0.0, 0.0, 0.0));
            float4 phase = UNITY_ACCESS_INSTANCED_PROP(Props, _RmWindPhase);
            float t = _RmSceneTime;
            float gust = (RmValueNoise1(t / _RmWind.w) * 2.0 - 1.0) * _RmWind.z;
            float angle = (_RmWind.x * sin(_RmWind.y * t + phase.x) + gust) * phase.y;
            positionWS.x += angle * v.color.g * (positionWS.y - originWS.y);
            return positionWS;
        }
        ENDHLSL

        Pass
        {
            Name "Foliage"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex FoliageVertex
            #pragma fragment FoliageFragment
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 ambientG : TEXCOORD1;    // ya con el albedo y la oclusión
                float3 keyG : TEXCOORD2;
                float4 fog : TEXCOORD3;
                float2 fade : TEXCOORD4;
            };

            Varyings FoliageVertex(Attributes v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                Varyings o;
                float3 positionWS = FoliagePosition(v);
                float3 normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.positionCS = TransformWorldToHClip(positionWS);
                o.positionWS = positionWS;

                float4 variant = UNITY_ACCESS_INSTANCED_PROP(Props, _RmVariant);
                int tier = (int)round(saturate(v.color.b) * 2.0);
                int palette = (int)round(clamp(variant.x, 0.0, 2.0));
                float3 green = lerp(kRmPineGreens[palette * 3 + tier] * variant.y, _RmBushColor.rgb, saturate(variant.z));
                bool foliage = v.color.a > 0.5;
                float3 albedo = foliage ? green : _RmTrunkColor.rgb;

                float3 ambientG, keyG;
                RmVertexLight(normalWS, foliage ? kRmFoliageWrap : 0.0, ambientG, keyG);
                float3 tint = sqrt(max(albedo, 0.0)) * pow(saturate(v.color.r), 1.1);
                o.ambientG = ambientG * tint;
                o.keyG = keyG * tint;
                o.fog = RmVertexFog(positionWS.z, positionWS.y);
                o.fade = RmDitherFade(UNITY_ACCESS_INSTANCED_PROP(Props, _RmFade));
                return o;
            }

            half4 FoliageFragment(Varyings i) : SV_Target
            {
                RmDitherClip(i.fade, i.positionCS.xy);
                float3 c = i.ambientG + i.keyG * RmMainShadow(i.positionWS);
                c = lerp(c, i.fog.rgb, i.fog.w);
                return half4(c * c, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex FoliageShadowVertex
            #pragma fragment FoliageShadowFragment
            #pragma multi_compile_instancing

            struct ShadowVaryings
            {
                float4 positionCS : SV_POSITION;
                float2 fade : TEXCOORD0;
            };

            ShadowVaryings FoliageShadowVertex(Attributes v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                ShadowVaryings o;
                o.positionCS = RmShadowCasterClip(FoliagePosition(v), TransformObjectToWorldNormal(v.normalOS));
                o.fade = RmDitherFade(UNITY_ACCESS_INSTANCED_PROP(Props, _RmFade));
                return o;
            }

            half4 FoliageShadowFragment(ShadowVaryings i) : SV_Target
            {
                RmDitherClip(i.fade, i.positionCS.xy);
                return 0;
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
            #pragma vertex FoliageDepthVertex
            #pragma fragment FoliageDepthFragment
            #pragma multi_compile_instancing

            struct DepthVaryings
            {
                float4 positionCS : SV_POSITION;
                float2 fade : TEXCOORD0;
            };

            DepthVaryings FoliageDepthVertex(Attributes v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                DepthVaryings o;
                o.positionCS = TransformWorldToHClip(FoliagePosition(v));
                o.fade = RmDitherFade(UNITY_ACCESS_INSTANCED_PROP(Props, _RmFade));
                return o;
            }

            half FoliageDepthFragment(DepthVaryings i) : SV_Target
            {
                RmDitherClip(i.fade, i.positionCS.xy);
                return 0;
            }
            ENDHLSL
        }
    }
}
