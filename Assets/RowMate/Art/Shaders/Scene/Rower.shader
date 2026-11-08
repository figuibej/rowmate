// RowMate/Scene/Rower: el remero (07-remero-palada/02-tech-design §7.4; 00-arte-remero §2.3; DT7-09; DA7-05; 04-design-notes §2).
// Opaco, con el skinning de Unity (SkinnedMeshRenderer: el maniquí o Y Bot vestido por RowerOutfitBaker). La región sale del rojo del
// color de vértice (región / 8: mono, franjas, panel, piel, medias, anteojos, gorra, articulaciones, pelo) y el color con su suavidad
// de _RmRowerColors[9] (RowerArt.RegionColors en lineal y RegionSmoothness; los pone SceneWorldBuilder).
//
// Bordes nítidos (WP-D2): cada región tiene una celda en una grilla de 3 × 3 (kRmRegionCell) armada para que las regiones que se tocan
// en la malla sean vecinas (también en diagonal): el vértice manda la celda, la interpolación va en línea recta de una celda a la
// vecina y el fragmento redondea. Así la franja, el borde del mono sobre la rodilla, las medias, el pelo y los anteojos se cortan en
// seco a mitad de la cara en vez de degradarse a lo largo de una cara entera (8 cm en el tronco del maniquí), por 4 ALU.
//
//           y = 0      y = 1      y = 2
//   x = 0   franjas    mono       panel
//   x = 1   medias     piel       articulaciones
//   x = 2   anteojos   pelo       gorra
//
// La luz es la de la escena (RowMateLighting: el tinte de la hora, la sombra de la luz principal) más:
//   - el relleno del frente (ART_BIBLE §7.9: _RmRimFill·sunColor·saturate(N·V));
//   - la piel: envolvente kRmSkinWrap y el terminador teñido de #E89A80 (luz que pasa por la piel, kRmSkinScatter);
//   - el mono de lycra: brillo de borde blanco al kRmSuitSheen (× el tinte de la hora);
//   - los anteojos: el cielo de la hora con el Fresnel (kRmGlassesReflect);
//   - un especular de Blinn por la suavidad, apagado (kRmRowerSpecular: tela y piel, no plástico).
// Lo que depende de la vista y no de la región del píxel (el brillo de borde, el Fresnel, el medio vector) va en el vértice. Sin el
// reloj de Unity.
Shader "RowMate/Scene/Rower"
{
    Properties
    {
        _RmRimFill ("Relleno del frente (× sunColor)", Float) = 0.12
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry" "IgnoreProjector" = "True" }

        HLSLINCLUDE
        #include "../Include/RowMateScene.hlsl"
        #include "../Include/RowMateLighting.hlsl"
        #include "../Include/RowMateFog.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _RmRowerColors[9];
            float _RmRimFill;
        CBUFFER_END

        // RowerArt (04-design-notes §2): la envolvente de la tela y de la piel, el terminador de la piel, el brillo de borde del mono, el
        // peso del especular y el reflejo de los anteojos.
        static const float kRmClothWrap = 0.25;
        static const float kRmSkinWrap = 0.4;
        static const float kRmSkinScatter = 0.22;
        static const float kRmSuitSheen = 0.12;
        static const float kRmRowerSpecular = 0.25;
        static const float kRmGlassesReflect = 0.7;

        // La celda de cada región (x, y) y, al revés, la región de cada celda (x·3 + y): técnica, no arte.
        static const float2 kRmRegionCell[9] =
        {
            float2(0, 1), float2(0, 0), float2(0, 2), float2(1, 1), float2(1, 0), float2(2, 0), float2(2, 2), float2(1, 2), float2(2, 1),
        };
        static const int kRmCellRegion[9] = { 1, 0, 2, 4, 3, 7, 5, 8, 6 };

        struct Attributes
        {
            float4 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float4 color : COLOR;
        };
        ENDHLSL

        Pass
        {
            Name "Rower"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex RowerVertex
            #pragma fragment RowerFragment
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 ambientG : TEXCOORD2;    // sin el albedo: el fragmento elige la región
                float3 keyG : TEXCOORD3;
                float4 fog : TEXCOORD4;
                float3 scatterG : TEXCOORD5;    // el terminador de la piel (cero fuera de la piel)
                float3 halfWS : TEXCOORD6;      // el medio vector de Blinn
                float3 extra : TEXCOORD7;       // lo que se suma en lineal: el brillo de borde del mono y el cielo en los anteojos
                float4 cell : TEXCOORD8;        // (celda x, y; el peso del especular: luz clave² × kRmRowerSpecular × visibilidad; −)
            };

            Varyings RowerVertex(Attributes v)
            {
                Varyings o;
                float3 positionWS = TransformObjectToWorld(v.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.positionCS = TransformWorldToHClip(positionWS);
                o.positionWS = positionWS;
                o.normalWS = normalWS;

                int region = (int)round(saturate(v.color.r) * 8.0);
                float skin = region == 3 ? 1.0 : 0.0;
                float3 ambientG, keyG;
                RmVertexLight(normalWS, lerp(kRmClothWrap, kRmSkinWrap, skin), ambientG, keyG);
                float3 view = normalize(_WorldSpaceCameraPos - positionWS);
                o.ambientG = ambientG + RmFrontFill(normalWS, view, _RmRimFill);
                o.keyG = keyG;
                float3 key = RmKeyLegacy();
                // El terminador: una banda alrededor de N·L = 0 teñida de #E89A80 (en lineal, (0,807; 0,323; 0,216)), con la clave.
                float band = saturate(1.0 - abs(dot(normalWS, _RmLightDir.xyz) - 0.1) * 3.0);
                o.scatterG = sqrt(key * float3(0.807, 0.323, 0.216) * (kRmSkinScatter * band * skin));
                o.halfWS = _RmLightDir.xyz + view;

                float rim = 1.0 - saturate(dot(normalWS, view));
                float rim4 = rim * rim * rim * rim;
                float3 tint = RmLegacyTint();
                float suit = region <= 2 ? 1.0 : 0.0;
                float glasses = region == 5 ? 1.0 : 0.0;
                o.extra = tint * tint * (kRmSuitSheen * rim4 * suit)
                    + _RmPaletteSkyHorizon.rgb * ((0.04 + 0.96 * rim4 * rim) * kRmGlassesReflect * glasses);
                o.fog = RmVertexFog(positionWS.z, positionWS.y);
                o.cell = float4(kRmRegionCell[region], RmLuma(key * key) * kRmRowerSpecular * _RmDiskParams.x, 0.0);
                // It. 8: bajo el puente (DT8-07), por vértice.
                float underBridge = RmUnderBridgeGamma2(positionWS, normalWS);
                o.ambientG *= underBridge;
                o.keyG *= underBridge;
                o.scatterG *= underBridge;
                o.extra *= underBridge * underBridge;
                o.cell.z *= underBridge * underBridge;
                return o;
            }

            half4 RowerFragment(Varyings i) : SV_Target
            {
                float2 cell = round(i.cell.xy);
                float4 tone = _RmRowerColors[kRmCellRegion[(int)(cell.x * 3.0 + cell.y)]];
                float shadow = RmMainShadow(i.positionWS);
                float3 c = (i.ambientG + i.keyG * shadow) * sqrt(tone.rgb);
                c += i.scatterG * shadow;
                c = lerp(c, i.fog.rgb, i.fog.w);
                float nh = saturate(dot(normalize(i.normalWS), normalize(i.halfWS)));
                float specular = pow(nh, exp2(1.0 + 7.0 * tone.a)) * tone.a * i.cell.z * shadow;
                return half4(c * c + i.extra + specular, 1.0);
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
            #pragma vertex RowerShadowVertex
            #pragma fragment RowerShadowFragment

            float4 RowerShadowVertex(Attributes v) : SV_POSITION
            {
                return RmShadowCasterClip(TransformObjectToWorld(v.positionOS.xyz), TransformObjectToWorldNormal(v.normalOS));
            }

            half4 RowerShadowFragment() : SV_Target
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
            #pragma vertex RowerDepthVertex
            #pragma fragment RowerDepthFragment

            float4 RowerDepthVertex(Attributes v) : SV_POSITION
            {
                return TransformObjectToHClip(v.positionOS.xyz);
            }

            half RowerDepthFragment() : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
}
