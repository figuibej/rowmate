// RowMate/Scene/Hull: el casco provisorio (06a-escena-lago/04-design-notes §9; 02-tech-design §3.6, §7.5; AC-SCN-01-08; WP-D1).
// La malla de Core (HullMesh) trae los cuatro colores del casco en el color de vértice (sRGB): #F5F5F5 arriba, #B0BEC5 en la obra
// viva, la línea de borda #00B4D8 y el cockpit #263238. En la cola 2000, opaco, quieto respecto de la cámara del contrato (cinta).
// El gelcoat (_RmGelcoat = (smoothness 0,85; exponente de Blinn 64; Fresnel 0,3)): el especular de la luz clave × la visibilidad del
// astro y el Fresnel hacia skyHorizon en los costados, así el casco se ve brillante y blanco a cualquier hora, teñido por la luz.
Shader "RowMate/Scene/Hull"
{
    Properties
    {
        _RmGelcoat ("Gelcoat (smoothness; exponente de Blinn; Fresnel hacia skyHorizon; -)", Vector) = (0.85, 64, 0.3, 0)
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry" "IgnoreProjector" = "True" }

        HLSLINCLUDE
        #include "../Include/RowMateScene.hlsl"
        #include "../Include/RowMateLighting.hlsl"
        #include "../Include/RowMateFog.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _RmGelcoat;
        CBUFFER_END

        // WP-D2: la exposición del casco (en gamma 2) y el croma del ambiente que le llega (MaterialArt.HullExposure, HullAmbientChroma):
        // sin tonemapping la cubierta del mediodía se recortaba en una silueta blanca plana.
        static const float kRmHullExposure = 0.78;
        static const float kRmHullAmbientChroma = 0.5;
        // It. 6b (#85): el brillo del gelcoat en la cubierta, recortado (MaterialArt.HullDeckGloss).
        static const float kRmHullDeckGloss = 0.15;

        struct Attributes
        {
            float4 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float4 color : COLOR;
        };
        ENDHLSL

        Pass
        {
            Name "Hull"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex HullVertex
            #pragma fragment HullFragment
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 ambientG : TEXCOORD2;    // ya con el albedo, más el Fresnel hacia skyHorizon
                float3 keyG : TEXCOORD3;        // ya con el albedo
                float4 fog : TEXCOORD4;
                float3 specular : TEXCOORD5;    // el color del especular (lineal): la luz clave × la visibilidad × la smoothness
            };

            Varyings HullVertex(Attributes v)
            {
                Varyings o;
                float3 positionWS = TransformObjectToWorld(v.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.positionCS = TransformWorldToHClip(positionWS);
                o.positionWS = positionWS;
                o.normalWS = normalWS;

                float3 albedoG = RmSrgbToGamma2(v.color.rgb) * kRmHullExposure;
                float3 ambientG, keyG;
                RmVertexLight(normalWS, 0.0, ambientG, keyG);
                float ambientMean = dot(ambientG, float3(1.0, 1.0, 1.0) / 3.0);
                ambientG = lerp(float3(ambientMean, ambientMean, ambientMean), ambientG, kRmHullAmbientChroma);
                float3 view = normalize(_WorldSpaceCameraPos - positionWS);
                float fresnel = pow(1.0 - saturate(dot(normalWS, view)), 5.0) * _RmGelcoat.z;
                // #85: en la cubierta (N.y > 0,8) el Fresnel y el especular se recortan a 0,15 (00-arte-agua §9).
                float deck = RmSmoothStep(0.7, 0.9, normalWS.y);
                fresnel = lerp(fresnel, min(fresnel, kRmHullDeckGloss), deck);
                o.ambientG = lerp(ambientG * albedoG, sqrt(max(_RmPaletteSkyHorizon.rgb, 0.0)), fresnel);
                o.keyG = keyG * albedoG;
                o.fog = RmVertexFog(positionWS.z, positionWS.y);
                float3 key = RmKeyLegacy();
                o.specular = key * key * (lerp(_RmGelcoat.x, min(_RmGelcoat.x, kRmHullDeckGloss), deck) * _RmDiskParams.x);
                // It. 8: bajo el puente (DT8-07), por vértice (el casco tiene secciones cada pocos cm; la rampa es de 1,5 m).
                float underBridge = RmUnderBridgeGamma2(positionWS, normalWS);
                o.ambientG *= underBridge;
                o.keyG *= underBridge;
                o.specular *= underBridge * underBridge;
                return o;
            }

            half4 HullFragment(Varyings i) : SV_Target
            {
                float3 n = normalize(i.normalWS);
                float3 view = normalize(_WorldSpaceCameraPos - i.positionWS);
                float3 h = normalize(_RmLightDir.xyz + view);
                float shadow = RmMainShadow(i.positionWS);
                float specular = pow(saturate(dot(n, h)), _RmGelcoat.y) * shadow;
                float3 c = i.ambientG + i.keyG * shadow;
                c = lerp(c, i.fog.rgb, i.fog.w);
                return half4(c * c + i.specular * specular, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex HullShadowVertex
            #pragma fragment HullShadowFragment

            float4 HullShadowVertex(Attributes v) : SV_POSITION
            {
                return RmShadowCasterClip(TransformObjectToWorld(v.positionOS.xyz), TransformObjectToWorldNormal(v.normalOS));
            }

            half4 HullShadowFragment() : SV_Target
            {
                return 0;
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
            #pragma vertex HullDepthVertex
            #pragma fragment HullDepthFragment

            float4 HullDepthVertex(Attributes v) : SV_POSITION
            {
                return TransformObjectToHClip(v.positionOS.xyz);
            }

            half HullDepthFragment() : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
}
