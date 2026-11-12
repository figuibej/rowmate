// RowMate/Scene/Foam: la espuma de la orilla de la costa (08-entornos-selector/00-arte-entornos §3.3, §3.4; 02-tech-design §3.3, §8.4;
// WP-D1). Transparente (3020), instanciado sobre la tira de un segmento (FoamStripMesh de Core: 50 m en z, de x = 0, el lado del mar,
// a x = −1, el de la tierra, con u = z/50 y v de 0 a 1), escalada en x al ancho de la espuma y puesta en la cinta por FoamRenderer.
// _RmFoam = (código: 0 al pie del acantilado, 1 en la playa, + 2 si se apaga al empezar y + 4 al terminar el segmento (la costa que
// cambia de acantilado a playa); z₀ del segmento mod 256 m (para que el ruido quede quieto en el mundo); ancho en m; alfa del fundido
// de aparición).
//   acantilado: densa contra las rocas (v → 1), late con el mar de fondo: alfa 0,5 ↔ 0,9 cada _RmFoamPulse s (6,5);
//   playa: la línea avanza y retrocede sobre el agua con el mismo período (la mitad del ancho), con la estela que deja al bajar.
// El ruido es el de la escena (_RmNoiseTex, A = fBm, 256² sin costura) a 2,5 m, que se corre despacio con el tiempo de escena: la
// espuma se rompe en jirones. Color #F4F6F2 × la luz de la hora (RmVertexLight con la normal hacia arriba) y la niebla del contrato.
// Sin el reloj de Unity.
// It. 6b (#22; AC-6B-29; PROVISORIO de WP-B hasta la ronda de WP-D): ≤ 30 ALU. Lo que no depende del ancho de la tira va en el vértice (la
// tira tiene un vértice por metro a lo largo): el latido, el apagado de las puntas, el color con la niebla y las coordenadas del ruido. En
// el fragmento, las rampas del ancho son lineales (antes smoothstep) y la playa y el acantilado se eligen sin rama.
Shader "RowMate/Scene/Foam"
{
    Properties
    {
        _RmNoiseTex ("Ruido (256², RGBA, sin costura)", 2D) = "gray" {}
        _RmFoamPulse ("Latido de la espuma (s)", Float) = 6.5
        _RmFoamColor ("Espuma", Color) = (0.956863, 0.964706, 0.94902, 1)
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Transparent" "Queue" = "Transparent+20" "IgnoreProjector" = "True" }
        ZWrite Off
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha

        HLSLINCLUDE
        #include "../Include/RowMateScene.hlsl"
        #include "../Include/RowMateLighting.hlsl"
        #include "../Include/RowMateFog.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _RmNoiseTex_ST;
            float _RmFoamPulse;
            float4 _RmFoamColor;
        CBUFFER_END

        TEXTURE2D(_RmNoiseTex);
        SAMPLER(sampler_RmNoiseTex);

        static const float kRmFoamNoiseM = 2.5;
        static const float kRmFoamTaperM = 6;
        ENDHLSL

        Pass
        {
            Name "Foam"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex FoamVertex
            #pragma fragment FoamFragment
            #pragma multi_compile_instancing

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _RmFoam)
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
                float4 local : TEXCOORD0;      // (v de 0 a 1; el borde de la línea de la playa; la ganancia del acantilado; 1 = playa)
                float4 shade : TEXCOORD1;      // (la espuma iluminada con la niebla, en gamma 2; alfa: fundido × niebla × puntas)
                float2 uv : TEXCOORD2;         // el ruido, quieto en el mundo
            };

            Varyings FoamVertex(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                float4 foam = UNITY_ACCESS_INSTANCED_PROP(Props, _RmFoam);
                float3 positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(positionWS);
                float3 ambientG, keyG;
                RmVertexLight(float3(0.0, 1.0, 0.0), 0.0, ambientG, keyG);
                float3 lit = sqrt(max(_RmFoamColor.rgb, 0.0)) * (ambientG + keyG);
                float4 fog = RmVertexFog(positionWS.z, positionWS.y);
                float t = _RmSceneTime;
                float zw = foam.y + v.positionOS.z;
                float code = foam.x;
                float taperStart = step(1.5, fmod(code, 4.0));
                float taperEnd = step(3.5, code);
                // El latido, un poco corrido a lo largo de la orilla (la ola no llega a todas partes a la vez).
                float pulse = 0.5 + 0.5 * sin(6.2831853 * t / max(_RmFoamPulse, 0.1) + zw * 0.045);
                float ends = lerp(1.0, saturate(v.positionOS.z / kRmFoamTaperM), taperStart)
                           * lerp(1.0, saturate((50.0 - v.positionOS.z) / kRmFoamTaperM), taperEnd);
                o.shade = float4(lerp(lit, fog.rgb, fog.w), foam.w * (1.0 - 0.6 * fog.w) * ends);
                o.local = float4(v.uv.y, lerp(0.35, 0.9, pulse), lerp(0.5, 0.9, pulse), fmod(code, 2.0));
                o.uv = float2(v.positionOS.x * foam.z, zw) / kRmFoamNoiseM + float2(0.013 * t, -0.021 * t);
                return o;
            }

            half4 FoamFragment(Varyings i) : SV_Target
            {
                float v = i.local.x;
                float edge = i.local.y;
                // Playa: la línea que sube y baja (del medio de la tira al borde de la arena) con la estela detrás; acantilado: densa
                // contra las rocas, con el latido. Sin rama: las dos, y se elige con el código.
                float beach = max(saturate((v - edge) / 0.35 + 1.0) * saturate((edge + 0.08 - v) / 0.08), 0.35 * saturate(2.0 * v - 1.0));
                float cliff = saturate(v / 0.75) * i.local.z;
                float density = lerp(cliff, beach, i.local.w);
                float n = SAMPLE_TEXTURE2D(_RmNoiseTex, sampler_RmNoiseTex, i.uv).a;
                float breakUp = RmSmoothStep(0.25, 0.65, n + 0.45 * density);
                return half4(i.shade.rgb * i.shade.rgb, density * breakUp * i.shade.w);
            }
            ENDHLSL
        }
    }
}
