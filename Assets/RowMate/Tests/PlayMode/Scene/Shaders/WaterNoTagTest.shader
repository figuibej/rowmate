// Solo de prueba (WaterFallbackTests.UnsupportedSubShaderIsDetected; 06b-agua-avanzada/02-tech-design §10 AC-06, G6b-01): un agua
// "avanzada" SIN el tag RmWater = Advanced y con Fallback a la degradada. Shader.isSupported da true (por el Fallback), pero el SubShader
// activo no es el avanzado: la escena tiene que caer a la degradada con su línea, no dibujar esto en silencio. Nunca llega a un build
// (nada fuera de las pruebas lo referencia).
Shader "Hidden/RowMate/Tests/WaterNoTagTest"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "AlphaTest" }

        Pass
        {
            Name "WaterNoTag"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vertex
            #pragma fragment Fragment
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float4 Vertex(float4 positionOS : POSITION) : SV_POSITION
            {
                return TransformObjectToHClip(positionOS.xyz);
            }

            half4 Fragment() : SV_Target
            {
                return half4(0.1, 0.3, 0.5, 1.0);
            }
            ENDHLSL
        }
    }

    Fallback "RowMate/Scene/WaterDegraded"
}
