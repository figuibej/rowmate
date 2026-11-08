// RowMate/Scene/Buoy: la boya de 500 m (07-remero-palada/02-tech-design §2.9, §3.4, §7.4; 00-arte-remero §9). Opaca, naranja
// (_RmBuoyColor, #FF9F1C = metric.split) con la franja blanca marcada en el rojo del color de vértice (BuoyMesh). La aparición y el
// fundido de 0,6 s con el tramado de la orilla (_RmFade, RowMateDither: determinista). La luz de la escena (RowMateLighting) con un
// brillo de plástico. Proyecta sombra. Sin el reloj de Unity. WP-D2: el brillo de plástico (suavidad 0,7).
Shader "RowMate/Scene/Buoy"
{
    Properties
    {
        _RmBuoyColor ("Naranja de la boya", Color) = (1, 0.623529, 0.109804, 1)
        _RmFade ("Fundido (RowMateDither)", Float) = 1
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry" "IgnoreProjector" = "True" }

        HLSLINCLUDE
        #include "../Include/RowMateScene.hlsl"
        #include "../Include/RowMateLighting.hlsl"
        #include "../Include/RowMateFog.hlsl"
        #include "../Include/RowMateDither.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _RmBuoyColor;
            float _RmFade;
        CBUFFER_END

        // RowerArt (00-arte-remero §9; 04-design-notes §5): el plástico de la boya (suavidad 0,7 → exponente de Blinn) y su peso, y el blanco
        // de la franja (lineal).
        static const float kRmBuoyGloss = 59.7;
        static const float kRmBuoySpecular = 0.45;
        static const float kRmBuoyWhite = 0.94;

        struct Attributes
        {
            float4 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float4 color : COLOR;
        };
        ENDHLSL

        Pass
        {
            Name "Buoy"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex BuoyVertex
            #pragma fragment BuoyFragment
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 ambientG : TEXCOORD1;
                float3 keyG : TEXCOORD2;
                float4 fog : TEXCOORD3;
                float2 fade : TEXCOORD4;
                float3 normalWS : TEXCOORD5;
                float4 halfWS : TEXCOORD6;      // (medio vector de Blinn; peso del brillo: luz clave² × kRmBuoySpecular × visibilidad)
            };

            Varyings BuoyVertex(Attributes v)
            {
                Varyings o;
                float3 positionWS = TransformObjectToWorld(v.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.positionCS = TransformWorldToHClip(positionWS);
                o.positionWS = positionWS;
                o.normalWS = normalWS;
                float3 albedo = lerp(_RmBuoyColor.rgb, float3(kRmBuoyWhite, kRmBuoyWhite, kRmBuoyWhite), saturate(v.color.r));
                float3 albedoG = sqrt(max(albedo, 0.0));
                float3 ambientG, keyG;
                RmVertexLight(normalWS, 0.2, ambientG, keyG);
                // It. 8: bajo el puente (DT8-07), por vértice, como el casco y los remos.
                float underBridge = RmUnderBridgeGamma2(positionWS, normalWS);
                ambientG *= underBridge;
                keyG *= underBridge;
                o.ambientG = ambientG * albedoG;
                o.keyG = keyG * albedoG;
                o.fog = RmVertexFog(positionWS.z, positionWS.y);
                o.fade = RmDitherFade(_RmFade);
                float3 key = RmKeyLegacy();
                float3 view = normalize(_WorldSpaceCameraPos - positionWS);
                o.halfWS = float4(_RmLightDir.xyz + view, RmLuma(key * key) * kRmBuoySpecular * _RmDiskParams.x);
                return o;
            }

            half4 BuoyFragment(Varyings i) : SV_Target
            {
                RmDitherClip(i.fade, i.positionCS.xy);
                float shadow = RmMainShadow(i.positionWS);
                float3 c = i.ambientG + i.keyG * shadow;
                c = lerp(c, i.fog.rgb, i.fog.w);
                // El brillo del plástico: un Blinn chico con la luz clave (de noche, con la visibilidad del astro, casi nada).
                float specular = pow(saturate(dot(normalize(i.normalWS), normalize(i.halfWS.xyz))), kRmBuoyGloss) * shadow * i.halfWS.w;
                return half4(c * c + specular, 1.0);
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
            #pragma vertex BuoyShadowVertex
            #pragma fragment BuoyShadowFragment

            float4 BuoyShadowVertex(Attributes v) : SV_POSITION
            {
                return RmShadowCasterClip(TransformObjectToWorld(v.positionOS.xyz), TransformObjectToWorldNormal(v.normalOS));
            }

            half4 BuoyShadowFragment() : SV_Target
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
            #pragma vertex BuoyDepthVertex
            #pragma fragment BuoyDepthFragment

            float4 BuoyDepthVertex(Attributes v) : SV_POSITION
            {
                return TransformObjectToHClip(v.positionOS.xyz);
            }

            half BuoyDepthFragment() : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
}
