// RowMate/Scene/Stars: las 80 estrellas del legacy (06a-escena-lago/02-tech-design §2.8, §3.6, §7.5; 04-design-notes §4.5; WP-D1).
// 80 quads instanciados (Graphics.RenderMeshInstanced con el Quad de Unity: UV0 de 0 a 1) en la cola 1001, aditivos y sin
// profundidad. Cada uno en su dirección del cielo (la del píxel del legacy desproyectada por Core: ± 1 dp con la respiración en 0) y
// a RM_SKY_DISTANCE, así giran con la cámara y no se trasladan. Un disco blanco suave del radio del legacy (0,6 a 1,6 dp) con 0,5 dp
// de borde; el alfa es el de Core (StarField.Alpha, ya con el fundido de 0,9 s) × CelestialMask.StarAttenuation: detrás del disco del
// sol o de la luna no se ven, y en el halo se apagan como con el halo del legacy encima (QA-G6aT-08, DT6a-17). La máscara se evalúa
// en el vértice (la estrella mide menos que el borde del disco). Titilan con el tiempo de escena (en el alfa de Core).
Shader "RowMate/Scene/Stars"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Transparent" "Queue" = "Background+1" "IgnoreProjector" = "True" }
        ZWrite Off
        ZTest Always
        Cull Off
        Blend One One

        HLSLINCLUDE
        #include "../Include/RowMateScene.hlsl"

        static const float kRmStarEdgeDp = 0.5;
        ENDHLSL

        Pass
        {
            Name "Stars"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex StarsVertex
            #pragma fragment StarsFragment
            #pragma multi_compile_instancing

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _RmStarDir)     // (dirección; radio en rad = radio dp / focal)
                UNITY_DEFINE_INSTANCED_PROP(float, _RmStarAlpha)    // el alfa de Core, con el fundido
            UNITY_INSTANCING_BUFFER_END(Props)

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 star : TEXCOORD0;    // (x, y en dp desde el centro; radio en dp; alfa con la máscara)
            };

            Varyings StarsVertex(Attributes v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                Varyings o;
                float4 sd = UNITY_ACCESS_INSTANCED_PROP(Props, _RmStarDir);
                float alpha = UNITY_ACCESS_INSTANCED_PROP(Props, _RmStarAlpha);
                float3 d = normalize(sd.xyz);
                float3 right, up;
                RmSkyBasis(d, right, up);
                float extent = sd.w + 1.0 / max(_RmFocalDp, 1.0);   // el radio y un dp de borde
                float2 c = v.uv * 2.0 - 1.0;
                o.positionCS = TransformWorldToHClip(RmSkyPoint(d + (right * c.x + up * c.y) * extent));
                float mask = RmStarAttenuation(RmAngle(dot(d, _RmDiskDir.xyz)), _RmDiskShape.x, _RmDiskShape.y, RmHaloScale());
                o.star = float4(c * (extent * _RmFocalDp), sd.w * _RmFocalDp, alpha * mask);
                return o;
            }

            half4 StarsFragment(Varyings i) : SV_Target
            {
                float coverage = saturate((i.star.z - length(i.star.xy)) / kRmStarEdgeDp + 0.5);
                return half4((i.star.w * coverage).xxx, 0.0);
            }
            ENDHLSL
        }
    }
}
