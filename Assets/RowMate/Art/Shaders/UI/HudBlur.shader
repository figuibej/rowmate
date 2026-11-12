// RowMate/UI/HudBlur: el vidrio desenfocado del HUD y del diálogo de completado (F-WKT-17; 06b-agua-avanzada/02-tech-design §4.4;
// DT6b-08 a -11; AC-6B-23 a -27). WP-D (04-design-notes §5). Lo corre HudBlurPass con un CommandBuffer sobre la
// RT de la escena, después de dibujarla (no es un renderer feature de URP):
//   pasada 0: la bajada (4 lecturas bilineales en las esquinas del texel de destino);
//   pasada 1: un paso de Kawase (4 lecturas a ± _RmBlurOffset texeles de la fuente);
//   pasada 2: la composición opaca _RmHudGlass: el tinte glass.fallback (#070D16) con α(L) = lerp(0,72; 0,86; S(0,10; 0,70; L)) POR
//             PÍXEL, con L la luma del fondo desenfocado en el espacio del panel (sRGB), la mezcla también en sRGB (IA6b-01: así el
//             compuesto es monótono en L y con L = 1 es el respaldo plano de hoy), y el tramado Bayer 4 × 4 de ± 0,5/255 de la escena
//             (RowMateDither: fijo en la pantalla, dos tomas del mismo cuadro son iguales byte a byte; el cielo de la noche desenfocado
//             se bandeaba en 8 bits).
// Los números del tinte llegan desde GlassTint (Core) en _RmGlassAlpha y _RmGlassTint: el shader no tiene constantes propias de arte.
Shader "RowMate/UI/HudBlur"
{
    Properties
    {
        _MainTex ("Fuente", 2D) = "black" {}
        _RmGlassAlpha ("α del tinte (mín; máx; L desde; L hasta)", Vector) = (0.72, 0.86, 0.1, 0.7)
        _RmGlassTint ("Tinte del vidrio (sRGB)", Color) = (0.027451, 0.05098, 0.086275, 1)
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "IgnoreProjector" = "True" }
        ZWrite Off
        ZTest Always
        Cull Off
        Blend Off

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "../Include/RowMateDither.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _RmGlassAlpha;
            float4 _RmGlassTint;
        CBUFFER_END

        // QA-V6b-04: la zona del α (GlassTint.RegionTexels y RegionLumaSlack).
        static const float kRmGlassRegionTexels = 8;
        static const float kRmGlassRegionSlack = 0.25;

        // Globales del CommandBuffer (HudBlurPass los pone antes de cada pasada): el tamaño del texel de la fuente y el desplazamiento.
        float4 _RmBlurTexel;
        float _RmBlurOffset;

        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);

        struct Attributes
        {
            float4 positionOS : POSITION;
            float2 uv : TEXCOORD0;
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float2 uv : TEXCOORD0;
        };

        Varyings BlurVertex(Attributes v)
        {
            Varyings o;
            o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
            o.uv = v.uv;
            return o;
        }

        float4 Four(float2 uv, float offset)
        {
            float2 d = _RmBlurTexel.xy * offset;
            float4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(-d.x, -d.y));
            c += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(d.x, -d.y));
            c += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(-d.x, d.y));
            c += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(d.x, d.y));
            return c * 0.25;
        }
        ENDHLSL

        Pass
        {
            Name "Down"
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex BlurVertex
            #pragma fragment DownFragment

            half4 DownFragment(Varyings i) : SV_Target
            {
                return half4(Four(i.uv, 0.5).rgb, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "Kawase"
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex BlurVertex
            #pragma fragment KawaseFragment

            half4 KawaseFragment(Varyings i) : SV_Target
            {
                return half4(Four(i.uv, _RmBlurOffset).rgb, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "Compose"
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex BlurVertex
            #pragma fragment ComposeFragment

            half4 ComposeFragment(Varyings i) : SV_Target
            {
                float3 background = LinearToSRGB(max(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).rgb, 0.0));
                float l = saturate(dot(background, float3(0.2126, 0.7152, 0.0722)));
                // QA-V6b-04 (GlassTint.RegionalAlpha): lo más claro que la zona (± 8 texeles en diagonal) toma el α de la zona salvo que sea mucho
                // más claro (L − 0,25): lo claro de atrás (la popa del bote) no se aplasta; lo más oscuro sigue con su α; el compuesto nunca pasa
                // el del respaldo sobre blanco.
                float2 d = _RmBlurTexel.xy * kRmGlassRegionTexels;
                float3 k = float3(0.2126, 0.7152, 0.0722);
                float region = dot(LinearToSRGB(max(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(-d.x, -d.y)).rgb, 0.0)), k);
                region += dot(LinearToSRGB(max(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(d.x, -d.y)).rgb, 0.0)), k);
                region += dot(LinearToSRGB(max(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(-d.x, d.y)).rgb, 0.0)), k);
                region += dot(LinearToSRGB(max(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(d.x, d.y)).rgb, 0.0)), k);
                region = saturate((region + l) * 0.2);
                float la = l > region ? max(region, l - kRmGlassRegionSlack) : l;
                float t = saturate((la - _RmGlassAlpha.z) / (_RmGlassAlpha.w - _RmGlassAlpha.z));
                float a = _RmGlassAlpha.x + (_RmGlassAlpha.y - _RmGlassAlpha.x) * t * t * (3.0 - 2.0 * t);
                float3 srgb = lerp(background, _RmGlassTint.rgb, a);
                float dither = RmBayer(i.positionCS.xy) - 0.5;
                srgb = saturate(srgb + dither / 255.0);
                return half4(SRGBToLinear(srgb), 1.0);
            }
            ENDHLSL
        }
    }
}
