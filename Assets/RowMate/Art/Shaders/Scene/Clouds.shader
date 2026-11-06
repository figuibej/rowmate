// RowMate/Scene/Clouds: las 7 nubes del legacy y la capa lejana (06a-escena-lago/04-design-notes §5; 02-tech-design §3.6, §7.5;
// WP-D1). Quads instanciados (el Quad de Unity: UV0 de 0 a 1) en la cola 1002, con alfa premultiplicado y sin profundidad.
// - Cada nube: el quad va de x − 1,4·s a x + 1,4·s y de y − 0,85·s a y + 0,70·s alrededor de su punto del legacy (CloudModel: la x y
//   la y del legacy, desproyectadas), con s en radianes (_RmCloudDir.w = s/focal). El atlas (SkyTextures: R = cuerpo, G = sombra,
//   la variante k en la celda (k mod 2; k / 2) desde arriba) da las manchas del legacy con el borde comido por fBm.
//   El cuerpo: lerp(blanco; skyHorizon; 0,25) con el alfa del legacy (_RmCloudAlpha.x, ya con WrapFade). La luz va de abajo hacia
//   arriba en el quad: arriba + 0,10·sunColor de día; abajo hacia sunGlow con 0,45·g (se encienden por abajo en la hora dorada); el
//   lado del sol + 0,12·sunColor·máx(0; cos Δacimut); de noche, el borde plateado de 0,15·sunColor·vis si la luna está a menos de
//   25°. La sombra del legacy (skyTop al 0,12·a, _RmCloudAlpha.y) va encima, como la pinta el legacy.
// - La capa lejana (solo Alto: RM_CLOUD_LAYER y una instancia más con _RmCloudVariant.x < 0): un plano vertical de ± 65° de acimut
//   entre τ = tan 2° y tan 9°, con la textura _RmCloudLayer (cirros estirados) que deriva a la mitad de la velocidad de las 7 y el
//   alfa × 0,6.
// Los colores se arman por instancia en el vértice (en lineal) y el fragmento solo los compone con el atlas. Sin _Time: el viento es
// el tiempo de escena.
Shader "RowMate/Scene/Clouds"
{
    Properties
    {
        _RmCloudAtlas ("Atlas de nubes (sky.clouds: R cuerpo, G sombra)", 2D) = "black" {}
        _RmCloudLayer ("Capa lejana (sky.clouds, solo Alto: R densidad)", 2D) = "black" {}
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Transparent" "Queue" = "Background+2" "IgnoreProjector" = "True" }
        ZWrite Off
        ZTest Always
        Cull Off
        Blend One OneMinusSrcAlpha

        HLSLINCLUDE
        #include "../Include/RowMateScene.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _RmCloudAtlas_ST;
            float4 _RmCloudLayer_ST;
        CBUFFER_END

        TEXTURE2D(_RmCloudAtlas);
        SAMPLER(sampler_RmCloudAtlas);
        TEXTURE2D(_RmCloudLayer);
        SAMPLER(sampler_RmCloudLayer);

        static const float kRmCloudQuadHalfWidth = 1.4;
        static const float kRmCloudQuadTop = -0.85;
        static const float kRmCloudQuadBottom = 0.7;
        static const float kRmCloudTopFrom = 0.6;
        static const float kRmCloudTopLight = 0.1;
        static const float kRmCloudSunSide = 0.12;
        static const float kRmCloudBottom = 0.4;
        static const float kRmCloudBottomGlow = 0.45;
        static const float kRmCloudNightRim = 0.15;
        static const float kRmCloudNightRimCos = 0.906307787;
        static const float kRmCloudLayerScale = 0.5;
        static const float kRmCloudLayerDrift = 0.0011;
        static const float kRmCloudLayerEdge = 0.15;
        static const float kRmCloudLayerAlpha = 0.6;
        static const float kRmCloudLayerMinTan = 0.0349207695;
        static const float kRmCloudLayerMaxTan = 0.1583844403;
        static const float kRmCloudLayerHalfSpanTan = 2.1445069205;
        ENDHLSL

        Pass
        {
            Name "Clouds"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex CloudsVertex
            #pragma fragment CloudsFragment
            #pragma multi_compile_instancing
            #pragma shader_feature_local RM_CLOUD_LAYER

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _RmCloudDir)      // (dirección del punto del legacy; tamaño s en rad)
                UNITY_DEFINE_INSTANCED_PROP(float, _RmCloudVariant)   // 0 a 3: la celda del atlas; < 0: la capa lejana (solo Alto)
                UNITY_DEFINE_INSTANCED_PROP(float4, _RmCloudAlpha)    // (cuerpo; sombra; WrapFade, ya aplicado en x e y; -)
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
                float4 uv : TEXCOORD0;          // (u, v del atlas o de la capa; v del quad; capa 0/1)
                float4 body : TEXCOORD1;        // (el cuerpo con la luz de esta altura, lineal; alfa del cuerpo)
                float4 shade : TEXCOORD2;       // (skyTop lineal; alfa de la sombra)
            };

            Varyings CloudsVertex(Attributes v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                Varyings o;
                float4 cd = UNITY_ACCESS_INSTANCED_PROP(Props, _RmCloudDir);
                float variant = UNITY_ACCESS_INSTANCED_PROP(Props, _RmCloudVariant);
                float4 alphas = UNITY_ACCESS_INSTANCED_PROP(Props, _RmCloudAlpha);
                float3 d = normalize(cd.xyz);
                float3 dirWS;
                float bodyAlpha = alphas.x;
                if (variant < 0.0)
                {
                    // La capa lejana: un plano vertical a RM_SKY_DISTANCE adelante, de ± 65° y de tan 2° a tan 9°.
                    float x = (v.uv.x * 2.0 - 1.0) * kRmCloudLayerHalfSpanTan;
                    float y = lerp(kRmCloudLayerMinTan, kRmCloudLayerMaxTan, v.uv.y);
                    dirWS = float3(x, y, 1.0);
                    o.uv = float4(x * kRmCloudLayerScale - kRmCloudLayerDrift * _RmSceneTime, v.uv.y, v.uv.y, 1.0);
                    bodyAlpha *= kRmCloudLayerAlpha;
                }
                else
                {
                    // El quad en el plano de pantalla de la cámara del contrato (z = 1, donde x e y son las τ del legacy): la nube mide en
                    // dp lo del legacy a cualquier elevación. Con el plano tangente al cielo se estiraba 1/cos e de ancho y 1/cos² e de
                    // alto (×1,22 y ×1,49 a los 35° de las nubes en vertical: B6a-22, "más grandes que en el legacy"; WP-D2).
                    float xs = lerp(-kRmCloudQuadHalfWidth, kRmCloudQuadHalfWidth, v.uv.x);
                    float ys = lerp(kRmCloudQuadBottom, kRmCloudQuadTop, v.uv.y);   // la y del legacy va hacia abajo
                    dirWS = float3(d.xy / max(d.z, 1e-3) + float2(xs, -ys) * cd.w, 1.0);
                    float k = floor(variant + 0.5);
                    float cellX = fmod(k, 2.0);
                    float cellY = floor(k * 0.5);
                    float vCell = 1.0 - v.uv.y;   // desde arriba
                    o.uv = float4((cellX + v.uv.x) * 0.5, 1.0 - (cellY + vCell) * 0.5, v.uv.y, 0.0);
                }

                o.positionCS = TransformWorldToHClip(RmSkyPoint(dirWS));

                // La luz de la nube en este borde del quad (arriba o abajo): en gamma 2 y al cuadrado acá.
                float day = 1.0 - _RmDiskParams.y;
                float vis = _RmDiskParams.x;
                float3 sunG = sqrt(max(_RmPaletteSunColor.rgb, 0.0));
                float3 bodyG = lerp(float3(1.0, 1.0, 1.0), sqrt(max(_RmPaletteSkyHorizon.rgb, 0.0)), 0.25);
                float2 az = normalize(d.xz + float2(0.0, 1e-5));
                float2 sunAz = normalize(_RmDiskDir.xz + float2(0.0, 1e-5));
                float side = max(0.0, dot(az, sunAz)) * day * vis;
                float rim = kRmCloudNightRim * _RmDiskParams.y * vis * saturate((dot(d, _RmDiskDir.xyz) - kRmCloudNightRimCos) / (1.0 - kRmCloudNightRimCos));
                float h = v.uv.y;
                float3 c = bodyG + sunG * (kRmCloudSunSide * side + rim + kRmCloudTopLight * day * vis * saturate((h - kRmCloudTopFrom) / (1.0 - kRmCloudTopFrom)));
                c = lerp(c, sqrt(max(_RmPaletteSunGlow.rgb, 0.0)), kRmCloudBottomGlow * _RmGolden * saturate((kRmCloudBottom - h) / kRmCloudBottom));

                // Los alfas del legacy se mezclaban en sRGB sobre el cielo: acá, el alfa lineal equivalente contra el telón de este vértice
                // (SkyBackdrop.Gradient de su τ, en sRGB ≈ gamma 2). De noche la nube del 14 % queda casi invisible, como en el legacy.
                float q = saturate(dirWS.y / dirWS.z * _RmFocalDp / max(_RmHorizonY, 1e-3));
                float bg = RmLuma(_RmSkyHorizonSrgb.rgb + (_RmSkyTopSrgb.rgb - _RmSkyHorizonSrgb.rgb) * RmBackdropGradient(q));
                float3 shadeG = sqrt(max(_RmPaletteSkyTop.rgb, 0.0));
                o.body = float4(c * c, RmDisplayAlpha(bodyAlpha, bg, RmLuma(c)));
                o.shade = float4(_RmPaletteSkyTop.rgb, RmDisplayAlpha(alphas.y, bg, RmLuma(shadeG)));
                return o;
            }

            half4 CloudsFragment(Varyings i) : SV_Target
            {
#if defined(RM_CLOUD_LAYER)
                float2 density;
                [branch] if (i.uv.w > 0.5)
                {
                    float edge = saturate(min(i.uv.z, 1.0 - i.uv.z) / kRmCloudLayerEdge);
                    density = float2(SAMPLE_TEXTURE2D(_RmCloudLayer, sampler_RmCloudLayer, i.uv.xy).r * edge, 0.0);
                }
                else
                {
                    density = SAMPLE_TEXTURE2D(_RmCloudAtlas, sampler_RmCloudAtlas, i.uv.xy).rg;
                }
#else
                float2 density = SAMPLE_TEXTURE2D(_RmCloudAtlas, sampler_RmCloudAtlas, i.uv.xy).rg;
#endif
                float aBody = density.x * i.body.w;
                float aShade = density.y * i.shade.w;
                // La sombra encima del cuerpo, con alfa premultiplicado.
                float3 rgb = lerp(i.body.rgb * aBody, i.shade.rgb, aShade);
                return half4(rgb, aBody + aShade - aBody * aShade);
            }
            ENDHLSL
        }
    }
}
