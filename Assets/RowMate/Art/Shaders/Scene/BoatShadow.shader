// RowMate/Scene/BoatShadow: la sombra ovalada del bote sobre el agua (06a-escena-lago/04-design-notes §8.3; 02-tech-design §3.6,
// §7.5; AC-SCN-01-08; WP-D1). Un quad (el de Unity, UV0 de 0 a 1) que WP-B1 pone en el centro del casco, a ras del agua; el vértice
// lo arma: un óvalo de 8,6 × 0,9 m corrido 0,35 m al lado opuesto del astro (el signo de su acimut, contrato del legacy), en la cola
// 2451, con alfa y sin profundidad. El alfa: 0,08·ambiente en el centro, exp(−r²·4) hacia el borde. En la 6a va en todos los niveles
// (el agua no recibe el mapa de sombras). El alfa se pasa al lineal como el legacy oscurece en sRGB: 1 − (1 − α)^2,2.
Shader "RowMate/Scene/BoatShadow"
{
    Properties
    {
        _RmShadowOffset ("Óvalo (corrimiento al lado opuesto al astro; largo; ancho; caída) en m", Vector) = (0.35, 8.6, 0.9, 4)
        _RmShadowAlpha ("Alfa en el centro (× ambiente)", Float) = 0.08
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Transparent" "Queue" = "AlphaTest+1" "IgnoreProjector" = "True" }
        ZWrite Off
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha

        HLSLINCLUDE
        #include "../Include/RowMateScene.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _RmShadowOffset;
            float _RmShadowAlpha;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "BoatShadow"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex ShadowVertex
            #pragma fragment ShadowFragment

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 local : TEXCOORD0;       // (x, z del óvalo de −1 a 1 × √(caída·log2 e); alfa del centro ya en lineal)
            };

            Varyings ShadowVertex(Attributes v)
            {
                Varyings o;
                float2 c = v.uv * 2.0 - 1.0;
                float side = _RmDiskDir.x > 0.0 ? -1.0 : 1.0;
                float3 positionOS = float3(c.x * 0.5 * _RmShadowOffset.z + side * _RmShadowOffset.x, 0.0, c.y * 0.5 * _RmShadowOffset.y);
                o.positionCS = TransformObjectToHClip(positionOS);
                float alpha = _RmShadowAlpha * _RmAmbient;
                // exp(−r²·caída) = exp2(−(r·√(caída·log2 e))²): la escala va acá y el fragmento hace un producto escalar y un exp2.
                o.local = float3(c * sqrt(_RmShadowOffset.w * 1.4426950409), 1.0 - pow(max(1.0 - alpha, 0.0), 2.2));
                return o;
            }

            half4 ShadowFragment(Varyings i) : SV_Target
            {
                return half4(0.0, 0.0, 0.0, i.local.z * exp2(-dot(i.local.xy, i.local.xy)));
            }
            ENDHLSL
        }
    }
}
