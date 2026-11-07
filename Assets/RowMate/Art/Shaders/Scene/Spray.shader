// RowMate/Scene/Spray: la salpicadura del ataque, la corona y las gotas de la recuperación (07-remero-palada/02-tech-design §2.8,
// §3.4, §7.4; DT7-05; DA7-07, DA7-09). Transparente (3100), instanciado: el quad de la escena orientado a la cámara en la posición de
// la matriz de cada instancia, con _RmSpray = (tamaño en m; alfa; tipo; largo del trazo en m). Tipo 0, la gota (redonda); 1, la corona
// (una lámina de 0,25 m de ancho y _RmSpray.w de alto, apoyada en el agua); 2, el trazo de la recuperación (estirado en vertical). El
// tamaño mínimo en pantalla (_RmMinDropDp: 1,5 dp las gotas, la mitad de eso más 0,25 los trazos) con el alfa compensado por el área
// (DA7-07). El color: el blanco del agua batida teñido por la luz de la hora. Sin el reloj de Unity (las partículas las mueve Core).
// Revisado en WP-D2 (04-design-notes §4.3): las gotas con un núcleo más denso para que el tamaño mínimo no las vuelva un disco gris.
Shader "RowMate/Scene/Spray"
{
    Properties
    {
        _RmMinDropDp ("Tamaño mínimo de una gota en pantalla (dp)", Float) = 1.5
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Transparent" "Queue" = "Transparent+100" "IgnoreProjector" = "True" }
        ZWrite Off
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha

        HLSLINCLUDE
        #include "../Include/RowMateScene.hlsl"
        #include "../Include/RowMateLighting.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float _RmMinDropDp;
        CBUFFER_END

        // RowerArt (04-design-notes §4.3): el núcleo de la gota (el disco llega a opaco en el 50 % del radio y el borde queda suave).
        static const float kRmDropCore = 2.0;
        ENDHLSL

        Pass
        {
            Name "Spray"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex SprayVertex
            #pragma fragment SprayFragment
            #pragma multi_compile_instancing

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _RmSpray)
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
                float4 local : TEXCOORD0;      // (u, v de −1 a 1; alfa; tipo)
                float3 color : TEXCOORD1;      // lineal
            };

            Varyings SprayVertex(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                float4 spray = UNITY_ACCESS_INSTANCED_PROP(Props, _RmSpray);
                float3 center = TransformObjectToWorld(float3(0.0, 0.0, 0.0));
                float3 right = UNITY_MATRIX_V[0].xyz;
                float3 up = UNITY_MATRIX_V[1].xyz;
                float depth = max(dot(center - _WorldSpaceCameraPos, -UNITY_MATRIX_V[2].xyz), 0.5);
                float dp = depth / max(_RmFocalDp, 1.0);
                float kind = spray.z;
                float minSize = (kind > 1.5 ? 0.5 * _RmMinDropDp + 0.25 : _RmMinDropDp) * dp;
                float size = max(spray.x, minSize);
                float alpha = spray.y * min(1.0, (spray.x * spray.x) / (size * size) * 1.6);
                float2 c = v.uv * 2.0 - 1.0;
                float3 positionWS;
                if (kind > 0.5 && kind < 1.5)
                {
                    // La corona: una lámina apoyada en el agua, de cara a la cámara.
                    positionWS = center + right * (c.x * 0.5 * spray.x) + float3(0.0, 1.0, 0.0) * ((c.y * 0.5 + 0.5) * max(spray.w, minSize));
                    alpha = spray.y;
                }
                else if (kind > 1.5)
                {
                    // El trazo: estirado en vertical (cae casi derecho).
                    positionWS = center + right * (c.x * 0.5 * size) + float3(0.0, 1.0, 0.0) * (c.y * 0.5 * max(spray.w, size));
                }
                else
                {
                    positionWS = center + (right * c.x + up * c.y) * (0.5 * size);
                }

                o.positionCS = TransformWorldToHClip(positionWS);
                o.local = float4(c, alpha, kind);
                float3 sky = _RmPaletteSkyHorizon.rgb;
                float3 sun = _RmPaletteSunColor.rgb;
                o.color = lerp(sky, float3(1.0, 1.0, 1.0), 0.55) * (0.55 + 0.45 * _RmAmbient) + sun * (0.08 * _RmDiskParams.x);
                return o;
            }

            half4 SprayFragment(Varyings i) : SV_Target
            {
                float2 c = i.local.xy;
                float h = 0.5 * c.y + 0.5;
                float shape = i.local.w > 0.5 && i.local.w < 1.5 ? saturate(1.0 - c.x * c.x) * (1.0 - h * h) : saturate(kRmDropCore * (1.0 - dot(c, c)));
                return half4(i.color, i.local.z * shape);
            }
            ENDHLSL
        }
    }
}
