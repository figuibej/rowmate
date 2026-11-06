// RowMate/Scene/Reeds: los juncos de la orilla (06a-escena-lago/04-design-notes §5.5, §7.4, §10.1; 02-tech-design §3.6, §7.5;
// DT6a-09: de 0,6 a 1,2 m; WP-D1). Instanciado, en la cola 2000, opaco (cintas de 2 triángulos, sin alpha test).
// La malla de Core (SmallMeshes.ReedTuft) trae r = la oclusión (0,7 en la raíz → 1 en la punta), g = el peso del viento (altura²) y
// la UV0 con v de 0 (la raíz) a 1 (la punta): el color va de #6E7F4A en la base a #A39E6A en las puntas. La luz envolvente es la del
// pasto (0,2). El viento: 0,06·sin(1,7t + fase) + la ráfaga × 2 (_RmWind; _RmWindPhase.x = z·0,4 + semilla en double desde quien
// instancia). _RmFade: el tramado del fin (de 100 a 120 m) y del LOD (RowMateDither).
Shader "RowMate/Scene/Reeds"
{
    Properties
    {
        _RmReedBase ("Junco: la base", Color) = (0.431373, 0.498039, 0.290196, 1)
        _RmReedTip ("Junco: las puntas", Color) = (0.639216, 0.619608, 0.415686, 1)
        _RmWind ("Viento (amplitud en rad; frecuencia en rad/s; ráfaga en rad; período de la ráfaga en s)", Vector) = (0.06, 1.7, 0.024, 9)
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry" "IgnoreProjector" = "True" }
        Cull Off

        HLSLINCLUDE
        #include "../Include/RowMateScene.hlsl"
        #include "../Include/RowMateLighting.hlsl"
        #include "../Include/RowMateFog.hlsl"
        #include "../Include/RowMateDither.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _RmReedBase;
            float4 _RmReedTip;
            float4 _RmWind;
        CBUFFER_END

        UNITY_INSTANCING_BUFFER_START(Props)
            UNITY_DEFINE_INSTANCED_PROP(float4, _RmWindPhase)   // (fase en rad; escala de la amplitud; -; -)
            UNITY_DEFINE_INSTANCED_PROP(float, _RmFade)
        UNITY_INSTANCING_BUFFER_END(Props)

        struct Attributes
        {
            float4 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float4 color : COLOR;
            float2 uv : TEXCOORD0;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        float3 ReedPosition(Attributes v)
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
            Name "Reeds"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex ReedsVertex
            #pragma fragment ReedsFragment
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

            Varyings ReedsVertex(Attributes v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                Varyings o;
                float3 positionWS = ReedPosition(v);
                float3 normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.positionCS = TransformWorldToHClip(positionWS);
                o.positionWS = positionWS;

                float3 albedo = lerp(_RmReedBase.rgb, _RmReedTip.rgb, saturate(v.uv.y));
                float3 ambientG, keyG;
                RmVertexLight(normalWS, kRmGrassWrap, ambientG, keyG);
                float3 albedoG = sqrt(max(albedo, 0.0)) * pow(saturate(v.color.r), 1.1);
                o.ambientG = ambientG * albedoG;
                o.keyG = keyG * albedoG;
                o.fog = RmVertexFog(positionWS.z, positionWS.y);
                o.fade = RmDitherFade(UNITY_ACCESS_INSTANCED_PROP(Props, _RmFade));
                return o;
            }

            half4 ReedsFragment(Varyings i) : SV_Target
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
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex ReedsDepthVertex
            #pragma fragment ReedsDepthFragment
            #pragma multi_compile_instancing

            struct DepthVaryings
            {
                float4 positionCS : SV_POSITION;
                float2 fade : TEXCOORD0;
            };

            DepthVaryings ReedsDepthVertex(Attributes v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                DepthVaryings o;
                o.positionCS = TransformWorldToHClip(ReedPosition(v));
                o.fade = RmDitherFade(UNITY_ACCESS_INSTANCED_PROP(Props, _RmFade));
                return o;
            }

            half ReedsDepthFragment(DepthVaryings i) : SV_Target
            {
                RmDitherClip(i.fade, i.positionCS.xy);
                return 0;
            }
            ENDHLSL
        }
    }
}
