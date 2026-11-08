// RowMate/Scene/Oar: los remos (07-remero-palada/02-tech-design §3.3, §7.4; 00-arte-remero §5.1; DA7-06, DA7-07). Opaco. La parte
// sale del rojo del color de vértice (parte / 4: puño, caña, manguito, pala, punta) y el color con su suavidad de _RmOarColors[5]
// (RowerArt.OarColors en lineal y OarSmoothness; los pone SceneWorldBuilder). En el vértice: la flexión de la caña (_RmOarLoad, m en
// la punta, hacia +z del remo: la pala queda atrás del agua que empuja; cuadrática desde el perno) y el ancho mínimo en pantalla de
// la caña (_RmMinWidthDp: con la focal de la escena, el radio crece hasta medir ese ancho; DA7-07). La pasada ShadowCaster la usa
// solo el renderer de la pala (la caña no proyecta: DA7-06). Revisado en WP-D2 (04-design-notes §3): sin cambios de forma.
Shader "RowMate/Scene/Oar"
{
    Properties
    {
        _RmOarLoad ("Flexión en la punta (m)", Float) = 0
        _RmMinWidthDp ("Ancho mínimo de la caña en pantalla (dp)", Float) = 1.2
        _RmPixelSize ("Tamaño de un dp en m a 1 m (0 = el de la focal de la escena)", Float) = 0
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry" "IgnoreProjector" = "True" }

        HLSLINCLUDE
        #include "../Include/RowMateScene.hlsl"
        #include "../Include/RowMateLighting.hlsl"
        #include "../Include/RowMateFog.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _RmOarColors[5];
            float _RmOarLoad;
            float _RmMinWidthDp;
            float _RmPixelSize;
        CBUFFER_END

        static const float kRmOarTip = 2.0;     // RowerArt.OarTipFromPin: la punta de la pala en el marco del remo (m desde el perno)

        struct Attributes
        {
            float4 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float4 color : COLOR;
        };

        // La flexión y el ancho mínimo, en el marco del remo (la caña sobre +x desde el perno).
        float3 RmOarPosition(float3 p, float part)
        {
            float t = saturate(p.x / kRmOarTip);
            p.z += _RmOarLoad * t * t;
            if (part < 2.5)
            {
                // La caña, el puño y el manguito son tornos alrededor del eje x: el radio crece hasta el mínimo en pantalla.
                float2 radial = p.yz - float2(0.0, _RmOarLoad * t * t);
                float r = length(radial);
                float3 positionWS = TransformObjectToWorld(p);
                float depth = max(dot(positionWS - _WorldSpaceCameraPos, -UNITY_MATRIX_V[2].xyz), 0.5);
                float dpAt = _RmPixelSize > 0.0 ? _RmPixelSize : 1.0 / max(_RmFocalDp, 1.0);
                float minR = 0.5 * _RmMinWidthDp * dpAt * depth;
                if (r > 1e-5 && r < minR)
                {
                    p.yz += radial * (minR / r - 1.0);
                }
            }

            return p;
        }
        ENDHLSL

        Pass
        {
            Name "Oar"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex OarVertex
            #pragma fragment OarFragment
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 ambientG : TEXCOORD2;
                float3 keyG : TEXCOORD3;
                float4 fog : TEXCOORD4;
                float4 specular : TEXCOORD5;
                float3 halfWS : TEXCOORD6;      // el medio vector de Blinn (WP-D2: en el vértice)
            };

            Varyings OarVertex(Attributes v)
            {
                Varyings o;
                float part = round(saturate(v.color.r) * 4.0);
                float3 positionWS = TransformObjectToWorld(RmOarPosition(v.positionOS.xyz, part));
                float3 normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.positionCS = TransformWorldToHClip(positionWS);
                o.positionWS = positionWS;
                o.normalWS = normalWS;
                float4 tone = _RmOarColors[(int)part];
                float3 albedoG = sqrt(max(tone.rgb, 0.0));
                float3 ambientG, keyG;
                RmVertexLight(normalWS, 0.0, ambientG, keyG);
                o.ambientG = ambientG * albedoG;
                o.keyG = keyG * albedoG;
                o.fog = RmVertexFog(positionWS.z, positionWS.y);
                float3 key = RmKeyLegacy();
                o.specular = float4(key * key * (0.5 * tone.a * _RmDiskParams.x), exp2(1.0 + 7.0 * tone.a));
                o.halfWS = _RmLightDir.xyz + normalize(_WorldSpaceCameraPos - positionWS);
                // It. 8: bajo el puente (DT8-07), por vértice.
                float underBridge = RmUnderBridgeGamma2(positionWS, normalWS);
                o.ambientG *= underBridge;
                o.keyG *= underBridge;
                o.specular.rgb *= underBridge * underBridge;
                return o;
            }

            half4 OarFragment(Varyings i, bool front : SV_IsFrontFace) : SV_Target
            {
                float3 n = normalize(i.normalWS) * (front ? 1.0 : -1.0);
                float shadow = RmMainShadow(i.positionWS);
                float specular = pow(saturate(dot(n, normalize(i.halfWS))), i.specular.w) * shadow;
                float3 c = i.ambientG + i.keyG * shadow;
                c = lerp(c, i.fog.rgb, i.fog.w);
                return half4(c * c + i.specular.rgb * specular, 1.0);
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
            Cull Off

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex OarShadowVertex
            #pragma fragment OarShadowFragment

            float4 OarShadowVertex(Attributes v) : SV_POSITION
            {
                float part = round(saturate(v.color.r) * 4.0);
                return RmShadowCasterClip(TransformObjectToWorld(RmOarPosition(v.positionOS.xyz, part)), TransformObjectToWorldNormal(v.normalOS));
            }

            half4 OarShadowFragment() : SV_Target
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
            Cull Off

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex OarDepthVertex
            #pragma fragment OarDepthFragment

            float4 OarDepthVertex(Attributes v) : SV_POSITION
            {
                float part = round(saturate(v.color.r) * 4.0);
                return TransformWorldToHClip(TransformObjectToWorld(RmOarPosition(v.positionOS.xyz, part)));
            }

            half OarDepthFragment() : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
}
