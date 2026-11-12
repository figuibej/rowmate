// Solo de prueba (WaterFallbackTests.UnsupportedSubShaderIsDetected; G6b-01): un agua "avanzada" con el tag RmWater = Advanced pero con un
// único SubShader que no corre en URP (pide otro pipeline) y sin Fallback: Shader.isSupported no lo garantiza y el tag del SubShader activo
// no es el avanzado. La escena tiene que caer a la degradada con su línea. Nunca llega a un build.
Shader "Hidden/RowMate/Tests/WaterUnsupportedTest"
{
    SubShader
    {
        Tags { "RenderPipeline" = "HDRenderPipeline" "RenderType" = "Opaque" "Queue" = "AlphaTest" "RmWater" = "Advanced" }

        Pass
        {
            Name "WaterUnsupported"
            Tags { "LightMode" = "ForwardOnly" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma only_renderers vulkan
            #pragma vertex Vertex
            #pragma fragment Fragment

            float4 Vertex(float4 positionOS : POSITION) : SV_POSITION
            {
                return positionOS;
            }

            half4 Fragment() : SV_Target
            {
                return half4(1.0, 0.0, 1.0, 1.0);
            }
            ENDHLSL
        }
    }
}
