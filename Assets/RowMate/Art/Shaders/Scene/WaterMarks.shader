// RowMate/Scene/WaterMarks: el agua que responde (07-remero-palada/02-tech-design §2.7, §3.4, §7.4; DT7-12; DA7-08; 00-arte-remero
// §8.3; 04-design-notes §4). Los charcos de las palas, los hervideros del drive y el anillo de la boya en UN dibujo instanciado de quads
// horizontales a ras del agua (el quad de la escena, UV0 de 0 a 1). Por instancia _RmMarks = (edad en s; giro en rad; alfa de Core;
// tipo: 0 charco, 1 hervidero, 2 anillo de la boya). Transparente después del agua (3050), sin profundidad, 0 texturas, sin el reloj de
// Unity (la edad y el giro los da Core; el batido del hervidero usa _RmSceneTime).
//
//   El charco (WP-D2): lo que deja la pala al salir es un PAR de remolinos separados 0,45 m (RowerArt.SwirlSeparation) a lo largo del
//   eje de la pala, que giran en sentidos opuestos (el espejo en x da la contrarrotación). Cada uno es un ojo oscuro y liso (agua sin
//   ondas que refleja menos cielo) con una media luna de espuma que gira con _RmMarks.y; el radio del ojo crece con la edad. Alrededor,
//   el borde del charco (el radio de Core, 0,3 → 0,9 m, viene en la escala del quad) como un anillo de espuma tenue. El alfa de Core es
//   el de la mancha media; la espuma llega a × kRmMarkFoamGain (a 47 dp, desde la cámara del contrato, el quad se ve de canto: sin
//   esto el charco no se lee).
//   El hervidero: espuma blanca batida bajo la pala durante el drive, que se mueve con el tiempo de escena.
//   El anillo de la boya: una línea de espuma.
Shader "RowMate/Scene/WaterMarks"
{
    Properties
    {
        _RmEffort ("Esfuerzo (0 a 1)", Float) = 0
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Transparent" "Queue" = "Transparent+50" "IgnoreProjector" = "True" }
        ZWrite Off
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha

        HLSLINCLUDE
        #include "../Include/RowMateScene.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float _RmEffort;
        CBUFFER_END

        // RowerArt (04-design-notes §4): media separación de los remolinos (m), el ojo (radio inicial y lo que crece por segundo, m), la
        // ganancia de la espuma sobre el alfa de Core y cuánto oscurece el ojo.
        static const float kRmSwirlHalf = 0.225;
        static const float kRmSwirlEye = 0.1;
        static const float kRmSwirlEyeGrowth = 0.035;
        static const float kRmMarkFoamGain = 1.6;
        static const float kRmMarkEyeAlpha = 0.55;
        ENDHLSL

        Pass
        {
            Name "WaterMarks"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex MarksVertex
            #pragma fragment MarksFragment
            #pragma multi_compile_instancing

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _RmMarks)
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
                float4 local : TEXCOORD0;     // (u, v de −1 a 1; alfa de Core; tipo)
                float4 meters : TEXCOORD1;    // (x, z en m desde el centro; 1/radio² del ojo; −)
                float4 spin : TEXCOORD2;      // (cos, sin del giro; fase del batido en x, en y)
                float3 light : TEXCOORD3;     // la espuma (el cielo que refleja, blanqueado), lineal
                float3 dark : TEXCOORD4;      // el ojo (el agua cercana sin ondas), lineal
            };

            Varyings MarksVertex(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                float4 m = UNITY_ACCESS_INSTANCED_PROP(Props, _RmMarks);
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                float2 c = v.uv * 2.0 - 1.0;
                // El radio del quad en m (la escala del objeto en x es el diámetro: WaterInteractionRenderer).
                float radius = 0.5 * length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x));
                float eye = kRmSwirlEye + kRmSwirlEyeGrowth * m.x;
                o.local = float4(c, m.z, m.w);
                o.meters = float4(c * radius, 1.0 / (eye * eye), 0.0);
                o.spin = float4(cos(m.y), sin(m.y), 8.0 * _RmSceneTime, -6.0 * _RmSceneTime);
                float ambient = 0.55 + 0.45 * _RmAmbient;
                o.light = lerp(_RmPaletteSkyHorizon.rgb, float3(1.0, 1.0, 1.0), 0.6) * ambient;
                o.dark = _RmPaletteWaterNear.rgb * 0.6;
                return o;
            }

            half4 MarksFragment(Varyings i) : SV_Target
            {
                float2 c = i.local.xy;
                float r2 = dot(c, c);
                float kind = i.local.w;
                float foam;
                float eye = 0.0;
                // Ramas de verdad: una instancia es de un solo tipo y cubre una parte chica de la pantalla (MaxBranchAlu).
                [branch] if (kind < 0.5)
                {
                    // Los dos remolinos: el ojo liso y la media luna de espuma que gira (en el ojo derecho, al revés por el espejo).
                    float2 q = float2(abs(i.meters.x) - kRmSwirlHalf, i.meters.y);
                    float q2 = dot(q, q) * i.meters.z;
                    eye = saturate(1.0 - q2);
                    float crescent = saturate(0.5 + 2.0 * dot(q, i.spin.xy) * sqrt(i.meters.z));
                    foam = saturate(1.0 - abs(q2 - 1.2) * 2.5) * crescent + 0.35 * saturate(1.0 - abs(r2 - 0.72) * 6.0);
                }
                else [branch] if (kind < 1.5)
                {
                    // El hervidero: espuma batida (dos senos que se mueven con el tiempo de escena), más densa en el centro.
                    float churn = 0.65 + 0.35 * sin(c.x * 6.0 + i.spin.z) * sin(c.y * 5.0 + i.spin.w);
                    foam = saturate(1.0 - r2) * churn * (0.8 + 0.4 * _RmEffort);
                }
                else
                {
                    // El anillo de la boya.
                    foam = saturate(1.0 - abs(r2 - 0.72) * 7.0);
                }

                float3 color = lerp(i.dark, i.light, saturate(foam * 3.0));
                float a = saturate(kRmMarkFoamGain * foam + kRmMarkEyeAlpha * eye) * i.local.z * step(r2, 1.0);
                return half4(color, a);
            }
            ENDHLSL
        }
    }
}
