// RowMate/Scene/ScreenOverlay: los degradés de pantalla de la capa del HUD (06a-escena-lago/04-design-notes §12.3; 02-tech-design
// §3.3, §3.6, §7.5; C-18, DV-67, DN6a-25; WP-D1). Un quad de pantalla completa (la UV0 de 0 a 1 arma el clip) en la cola 3500, lo
// último de la escena y antes del post, así los alcanza el velo de pausa; 0 elementos de UI. Arriba #06122A del 38 % a 0 en el 24 %
// alto de la pantalla; abajo, del 32 % a 0 en el 18 % bajo (no llegan a la zona del bote). Son del HUD: nunca en la escena sola
// (SetScrimsForTest(false) apaga el renderer en las tomas y en la pasada de contrato) y sin animación propia.
// El HUD se compone en sRGB (ADR 019); acá la mezcla es lineal: el alfa pasa a 1 − (1 − α)^2,2 (en el vértice), que sobre la
// escena oscurece como el mismo degradé en sRGB.
Shader "RowMate/Scene/ScreenOverlay"
{
    Properties
    {
        _RmScrimColor ("Color de los degradés", Color) = (0.023529, 0.070588, 0.164706, 1)
        _RmScrimTop ("Arriba (alfa; fracción del alto; -; -)", Vector) = (0.38, 0.24, 0, 0)
        _RmScrimBottom ("Abajo (alfa; fracción del alto; -; -)", Vector) = (0.32, 0.18, 0, 0)
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Transparent" "Queue" = "Transparent+500" "IgnoreProjector" = "True" }
        ZWrite Off
        ZTest Always
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha

        HLSLINCLUDE
        #include "../Include/RowMateScene.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _RmScrimColor;
            float4 _RmScrimTop;
            float4 _RmScrimBottom;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "ScreenOverlay"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex OverlayVertex
            #pragma fragment OverlayFragment

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 ramp : TEXCOORD0;        // (α'·(1 − y/0,24); α'·(1 − (1 − y)/0,18)), con y = 0 arriba y α' ya en lineal
            };

            Varyings OverlayVertex(Attributes v)
            {
                Varyings o;
                o.positionCS = RmFullScreenClip(v.uv);
                float y = 1.0 - v.uv.y;
                float2 alpha = 1.0 - pow(max(1.0 - float2(_RmScrimTop.x, _RmScrimBottom.x), 0.0), 2.2);
                // Lineal en y: se interpola exacto. Cada rampa vale ≤ α' en la pantalla (y de 0 a 1): alcanza con recortarla en 0.
                o.ramp = alpha * float2(1.0 - y / _RmScrimTop.y, 1.0 - (1.0 - y) / _RmScrimBottom.y);
                return o;
            }

            half4 OverlayFragment(Varyings i) : SV_Target
            {
                return half4(_RmScrimColor.rgb, max(max(i.ramp.x, i.ramp.y), 0.0));
            }
            ENDHLSL
        }
    }
}
