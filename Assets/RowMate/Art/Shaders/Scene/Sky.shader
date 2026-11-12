// RowMate/Scene/Sky: el cielo de la escena (06a-escena-lago/04-design-notes §3.3, §4.2 a §4.5; 02-tech-design §3.6, §7.5; WP-D1).
// Un quad o triángulo de pantalla completa (la UV0 de 0 a 1 arma el clip; WP-B1 le da bounds que no recorta el culling) en la cola
// 1000, sin profundidad, que reconstruye la dirección de vista con la inversa de la cámara:
//   1. el telón de skyHorizon a skyTop con SkyBackdrop.Gradient(q), q = τ/τ_top, mezclado en sRGB como Color.lerp (DV-66, DN6a-09);
//   2. la bruma del horizonte (1,5°) hacia lerp(skyHorizon; fog; 0,5), que también pinta lo que queda debajo del horizonte;
//   3. el tramado Bayer fijo de ± 0,5/255 sobre el telón en sRGB (DN6a-29) y el paso a lineal exacto (las sondas de contrato);
//   4. el fondo tenue de estrellas (solo Alto, RM_SKY_STARFIELD, P-10): una estrella por celda de 3° en (tan acimut; τ);
//   5. el resplandor de la hora dorada alrededor del sol, aunque esté detrás del paisaje (con la caída lineal hasta 20°);
//   6. adentro del halo (un bloque que solo corre cerca del astro): el halo del legacy (0,7 / 0,25 / 0 hasta 4 × el disco) × la
//      visibilidad (la luna × (0,4 + 0,6·iluminación)), y el disco: el sol en HDR (3 + 5·e)·vis con el limbo a 0,85; la luna con su
//      albedo, su fase (el lado oscuro con la luz cenicienta) y el espejo del sur (DV-57, C-19).
// Las estrellas (1001) van encima y se apagan solas detrás del disco y del halo (Stars, CelestialMask).
Shader "RowMate/Scene/Sky"
{
    Properties
    {
        _RmMoonTex ("Luna (sky.moon: el albedo en R)", 2D) = "white" {}
        _RmGlowParams ("Resplandor (fuerza; caída en grados; tope en grados; extra con la hora dorada)", Vector) = (0.35, 14, 20, 0.1)
        _RmStarField ("Fondo de estrellas, solo Alto (celda en grados; alfa mín.; alfa máx.; -)", Vector) = (3, 0.12, 0.3, 0)
        _RmMistBandDeg ("Bruma del horizonte (grados)", Float) = 1.5
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Background" "Queue" = "Background" "PreviewType" = "Skybox" }
        ZWrite Off
        ZTest Always
        Cull Off

        HLSLINCLUDE
        #include "../Include/RowMateScene.hlsl"
        #include "../Include/RowMateDither.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _RmMoonTex_ST;
            float4 _RmGlowParams;
            float4 _RmStarField;
            float _RmMistBandDeg;
        CBUFFER_END

        TEXTURE2D(_RmMoonTex);
        SAMPLER(sampler_RmMoonTex);

        static const float kRmSunLimb = 0.85;
        static const float kRmMoonTerminator = 0.06;
        static const float kRmMoonEarthshine = 0.06;
        static const float kRmDitherAmplitude = 0.0019607843;
        static const float kRmGlowGoldenFrom = 0.5;
        static const float kRmStarFieldMinTan = 0.052407779;
        static const float kRmStarFieldRadiusMin = 0.5;
        static const float kRmStarFieldRadiusMax = 0.9;
        ENDHLSL

        Pass
        {
            Name "Sky"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex SkyVertex
            #pragma fragment SkyFragment
            #pragma shader_feature_local_fragment RM_SKY_STARFIELD

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 dirWS : TEXCOORD0;       // sin normalizar: de la cámara al plano de clip
                float4 frame : TEXCOORD1;       // (focal/horizonte; 1/tan de la bruma; caída del resplandor en log2/rad; −1/tan del tope)
                float3 delta : TEXCOORD2;       // skyTop − skyHorizon, en sRGB
                float3 fromHaze : TEXCOORD9;    // skyHorizon − la bruma, en sRGB
                float4 glow : TEXCOORD3;        // (sunGlow × 0,35·g en lineal; radio del halo)
                float4 disk : TEXCOORD4;        // (escala del halo; radio del disco; opacidad del disco; es luna)
                float3 diskColor : TEXCOORD5;   // sunColor × HDR × vis (lineal)
                float3 right : TEXCOORD6;       // la base del disco / radio (con el espejo del sur en la derecha)
                float3 up : TEXCOORD7;
                float4 moon : TEXCOORD8;        // (L.x; L.z; luz cenicienta; brillo del fondo de estrellas)
                float3 haloG : TEXCOORD10;      // sunGlow en gamma 2 (√ del lineal): el halo se mezcla como en el legacy
            };

            Varyings SkyVertex(Attributes v)
            {
                Varyings o;
                float4 clip = RmFullScreenClip(v.uv);
                o.positionCS = clip;
                float4 p = mul(UNITY_MATRIX_I_VP, clip);
                o.dirWS = p.xyz / p.w - _WorldSpaceCameraPos;

                o.frame.x = _RmFocalDp / max(_RmHorizonY, 1e-3);
                o.frame.y = 1.0 / tan(radians(_RmMistBandDeg));
                o.frame.z = 1.4426950409 / radians(_RmGlowParams.y);
                o.frame.w = -1.0 / tan(radians(_RmGlowParams.z));
                o.delta = _RmSkyTopSrgb.rgb - _RmSkyHorizonSrgb.rgb;
                o.fromHaze = _RmSkyHorizonSrgb.rgb - _RmHazeSrgb.rgb;
                // It. 6b (#3): la fuerza sube de 0,35 a 0,45 con la hora dorada (g de 0,5 a 1), sin saltos (SkyArt.GlowStrengthAt).
                float glowStrength = _RmGlowParams.x + _RmGlowParams.w * saturate((_RmGolden - kRmGlowGoldenFrom) / (1.0 - kRmGlowGoldenFrom));
                o.glow = float4(_RmPaletteSunGlow.rgb * (glowStrength * _RmGolden), _RmDiskShape.y);
                o.haloG = sqrt(max(_RmPaletteSunGlow.rgb, 0.0));

                float vis = _RmDiskParams.x;
                float isMoon = _RmDiskParams.y;
                o.disk = float4(RmHaloScale(), _RmDiskShape.x, saturate(2.0 * vis), isMoon);
                o.diskColor = _RmPaletteSunColor.rgb * (_RmDiskShape.w * vis);
                float3 right, up;
                RmSkyBasis(_RmDiskDir.xyz, right, up);
                float invRadius = 1.0 / max(_RmDiskShape.x, 1e-5);
                o.right = right * (_RmDiskShape.z * invRadius);
                o.up = up * invRadius;

                // La luz de la fase: L = (sin φ; 0; −cos φ) en el disco (φ = 0 luna nueva, π llena; la creciente con la luz a la derecha).
                float phase = _RmDiskParams.w;
                float a = _RmAmbient;
                float stars = a <= 0.85 ? saturate((1.0 - a) / 0.65) : 0.0;
                o.moon = float4(sin(phase), -cos(phase), kRmMoonEarthshine * isMoon, stars);
                return o;
            }

#if defined(RM_SKY_STARFIELD)
            // El fondo tenue (P-10): una estrella por celda de 3° × 3° en (tan acimut; τ), radio 0,5 a 0,9 dp, alfa 0,12 a 0,30 × la
            // oscuridad, con titileo propio y nunca más brillante que las 80 del legacy (el máximo 0,30 contra 0,40 de la más tenue).
            float StarField(float3 d, float tau, float brightness)
            {
                float cell = radians(_RmStarField.x);
                float2 g = float2(d.x / d.z, tau) / cell;
                float2 c = floor(g);
                float h = RmHash((int)c.x * 7919 + (int)c.y * 104729);
                float2 star = c + 0.2 + 0.6 * frac(float2(h * 13.37, h * 71.13));
                float rDp = lerp(kRmStarFieldRadiusMin, kRmStarFieldRadiusMax, frac(h * 31.7));
                float distDp = length(g - star) * cell * _RmFocalDp;
                float twinkle = 0.5 + 0.5 * sin(2.0 * _RmSceneTime + h * 6.2831853);
                float alpha = lerp(_RmStarField.y, _RmStarField.z, frac(h * 5.3)) * (0.6 + 0.4 * twinkle);
                return alpha * brightness * saturate(rDp - distDp + 0.5) * step(kRmStarFieldMinTan, tau);
            }
#endif

            half4 SkyFragment(Varyings i) : SV_Target
            {
                float3 d = i.dirWS;
                float tau = d.y / d.z;

                // 1 a 3: el telón y la bruma en sRGB (lerp(bruma; horizonte + Δ·gradiente; m)), el tramado y el paso a lineal.
                float3 telon = i.fromHaze + i.delta * RmBackdropGradient(saturate(tau * i.frame.x));
                float3 s = _RmHazeSrgb.rgb + telon * RmSmooth(saturate(tau * i.frame.y));
                s += (RmBayer(i.positionCS.xy) - 0.5) * kRmDitherAmplitude;
                float3 col = RmSrgbToLinear(s);

                float3 dn = normalize(d);
#if defined(RM_SKY_STARFIELD)
                // 4: el fondo tenue (solo Alto).
                col += StarField(dn, tau, i.moon.w);
#endif

                // 5: el resplandor dorado.
                float ang = RmAngle(dot(dn, _RmDiskDir.xyz));
                col += i.glow.rgb * (exp2(-ang * i.frame.z) * saturate(1.0 + tau * i.frame.w));

                // 6: el halo y el disco, solo adentro del halo.
                [branch] if (ang < i.glow.w)
                {
                    // El halo del legacy se pintaba en sRGB sobre el cielo: mezclado en gamma 2 su borde se apaga suave (en lineal, el 25 %
                    // de sunGlow sobre el cielo de la noche era un disco gris de borde duro; WP-D2).
                    float3 haloMix = lerp(sqrt(col), i.haloG, RmHaloAlpha(ang / i.glow.w) * i.disk.x);
                    col = haloMix * haloMix;
                    float diskRad = i.disk.y;
                    float inside = 1.0 - RmSmoothStep(diskRad - kRmDiskEdgeRad, diskRad, ang);
                    float3 rel = dn - _RmDiskDir.xyz;
                    float2 uv = float2(dot(rel, i.right), dot(rel, i.up));
                    float r2 = saturate(dot(uv, uv));
                    float lit = RmSmoothStep(-kRmMoonTerminator, kRmMoonTerminator, uv.x * i.moon.x + sqrt(1.0 - r2) * i.moon.y);
                    float albedo = SAMPLE_TEXTURE2D(_RmMoonTex, sampler_RmMoonTex, uv * 0.5 + 0.5).r;
                    float3 disk = i.diskColor * lerp(lerp(1.0, kRmSunLimb, r2), albedo, i.disk.w);
                    float opacity = inside * i.disk.z * lerp(1.0, lit, i.disk.w);
                    col = lerp(col, disk, opacity) + _RmPaletteSunColor.rgb * (i.moon.z * inside * (1.0 - lit) * i.disk.z);
                }

                return half4(col, 1.0);
            }
            ENDHLSL
        }
    }
}
