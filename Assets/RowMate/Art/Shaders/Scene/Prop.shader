// RowMate/Scene/Prop: la cabaña, el muelle y las rocas (06a-escena-lago/04-design-notes §7.3, §10.1; 02-tech-design §3.6, §7.5;
// AC-SCN-15-06, AC-SCN-09-06, AC-SCN-20-02; WP-D1). Instanciado (Graphics.RenderMeshInstanced), en la cola 2000, opaco.
// Las mallas de Core (CabinMesh, PierMesh, SmallMeshes.Rock) traen el color de referencia del legacy en el color de vértice (rgb, en
// sRGB) y el material en a (0 madera: la cabaña y el muelle, 0,5 liso). La madera lleva sus tablas por shader: en las caras
// verticales, una línea oscura de 1 cm cada 0,18 m en y; en las horizontales (el tablero del muelle), una junta de 2 cm cada 0,20 m
// en x (_RmPlankParams), con el borde del ancho de un píxel y el contraste que se apaga cuando la línea mide menos de medio píxel
// (lejos se ve el promedio de la madera: a 40 m no titilan).
// La ventana de la cabaña es la submalla 1, que WP-B1 dibuja con el mismo shader y _RmWindowMode = 1 (otro material): de día
// #90CAF9 × (0,6 + 0,4·Fresnel hacia skyHorizon), con la luz y la niebla; de noche (_RmLit, el nivel de la luz de esa cabaña con su
// fundido de 0,4 s, DV-60) #FFE082 × _RmEmissiveGain, sin tinte, sin luz y sin niebla (AC-SCN-09-06): en la pasada de contrato, con
// la ganancia en 1, da exactamente #FFE082 (04-design-notes §10.4).
// _RmFade (por instancia): la aparición de 400 a 360 m, el tramado de 2 a 1 m y el cambio de LOD (RowMateDither, MaterialArt).
// It. 8 (08-entornos-selector/00-arte-entornos §1.4; 02-tech-design §8.4; WP-D1): los objetos de río, costa y regata van en una
// malla por segmento (KitSegmentMesh), así que lo que se mueve se marca en el vértice, no en el material: uv.x < 0 = vida
// (−1 − u el paño de una bandera, u = 0 en el mástil; −3 − w la tela de una sombrilla, w = 0 en la punta y 1 en el borde; −5 una
// boya) y uv.y = la fase de ese objeto (rad). Banderas: z += u²·(0,2·sin(4t + φ − 2,5u) + 0,06·sin(9t + 2,1u + φ)); sombrillas:
// y += 0,03·w·sin(5t + φ + 6·(x + z)); boyas: arfada 0,02·sin(1,3t + φ) y cabeceo 0,08·sin(1,7t + φ) rad sobre la línea de
// flotación (z += y·θ); público (QA-V8-01, −7 − w, w la altura en la persona; uv.y = período en cs + fase en vueltas): se mece
// 0,04·w·sin(a) en x y se levanta 0,06·w·máx(sin 2a; 0) con a = 2π(t/período + fase). Las mallas de la 6a traen uv en [0; 1]: no se mueven. Todo con _RmSceneTime (sigue en pausa).
// La sombra del puente (RmUnderBridgeGamma2, por fragmento) baja la luz del muro, del intradós y de lo que pasa por debajo.
Shader "RowMate/Scene/Prop"
{
    Properties
    {
        _RmPlankParams ("Tablas (paso en paredes; línea; paso en el tablero; junta) en m", Vector) = (0.18, 0.01, 0.2, 0.02)
        _RmWindowDay ("Ventana de día", Color) = (0.564706, 0.792157, 0.976471, 1)
        _RmWindowNight ("Ventana encendida (sin HDR: × _RmEmissiveGain)", Color) = (1, 0.878431, 0.509804, 1)
        _RmWindowMode ("Ventana (0 el resto de la malla, 1 la submalla de la ventana)", Float) = 0
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
            float4 _RmPlankParams;
            float4 _RmWindowDay;
            float4 _RmWindowNight;
            float _RmWindowMode;
        CBUFFER_END

        static const float kRmPlankShade = 0.55;
        static const float kRmWindowFresnel = 0.4;
        static const float kRmFlagWave = 0.2;
        static const float kRmFlagWaveRate = 4;
        static const float kRmBuoyPitch = 0.08;
        static const float kRmBuoyPitchRate = 1.7;
        static const float kRmBuoyHeave = 0.02;
        static const float kRmBuoyHeaveRate = 1.3;
        static const float kRmCrowdSway = 0.04;
        static const float kRmCrowdBob = 0.06;

        UNITY_INSTANCING_BUFFER_START(Props)
            UNITY_DEFINE_INSTANCED_PROP(float, _RmFade)     // el fundido del tramado (RowMateDither)
            UNITY_DEFINE_INSTANCED_PROP(float, _RmLit)      // el nivel de la luz de la noche de esta cabaña (0 a 1, con su fundido)
        UNITY_INSTANCING_BUFFER_END(Props)

        struct Attributes
        {
            float4 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float4 color : COLOR;
            float2 uv : TEXCOORD0;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        // La vida de banderas, sombrillas y boyas (arriba): la posición de objeto movida. Sin marca (uv.x ≥ 0), la misma.
        float3 PropMotion(float3 p, float2 uv)
        {
            float t = _RmSceneTime;
            float phase = uv.y;
            float code = -uv.x;
            if (code > 0.5 && code < 2.5)
            {
                float u = saturate(code - 1.0);
                p.z += u * u * (kRmFlagWave * sin(kRmFlagWaveRate * t + phase - 2.5 * u) + 0.06 * sin(9.0 * t + 2.1 * u + phase));
            }
            else if (code >= 2.5 && code < 4.5)
            {
                float w = saturate(code - 3.0);
                p.y += 0.03 * w * sin(5.0 * t + phase + 6.0 * (p.x + p.z));
            }
            else if (code >= 4.5 && code < 6.5)
            {
                float pitch = kRmBuoyPitch * sin(kRmBuoyPitchRate * t + phase);
                p.z += p.y * pitch;
                p.y += kRmBuoyHeave * sin(kRmBuoyHeaveRate * t + phase);
            }
            else if (code >= 6.5)
            {
                // QA-V8-01: el público. ⌊uv.y⌋ = el período en centésimas de s; la fracción, la fase en vueltas; w = la altura en la persona.
                float w = saturate(code - 7.0);
                float period = max(floor(uv.y), 1.0) * 0.01;
                float a = 6.2831853 * (t / period + frac(uv.y));
                p.x += kRmCrowdSway * w * sin(a);
                p.y += kRmCrowdBob * w * max(sin(2.0 * a), 0.0);
            }

            return p;
        }
        ENDHLSL

        Pass
        {
            Name "Prop"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex PropVertex
            #pragma fragment PropFragment
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 ambientG : TEXCOORD1;    // ya con el albedo
                float3 keyG : TEXCOORD2;        // ya con el albedo
                float4 fog : TEXCOORD3;
                float3 plank : TEXCOORD4;       // (coordenada en tablas; −1/píxel en tablas; medio ancho de la línea/píxel + 0,5)
                float4 window : TEXCOORD5;      // (lo encendido en lineal: #FFE082 × ganancia; nivel de la luz)
                float contrast : TEXCOORD6;     // lo que oscurece la línea (0 en lo liso y lejos)
                float2 fade : TEXCOORD7;        // el tramado (RmDitherFade)
                float3 normalWS : TEXCOORD8;    // It. 8: bajo el puente (por fragmento: el muro y el malecón son quads largos)
            };

            Varyings PropVertex(Attributes v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                Varyings o;
                float3 positionOS = PropMotion(v.positionOS.xyz, v.uv);
                float3 positionWS = TransformObjectToWorld(positionOS);
                float3 normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.positionCS = TransformWorldToHClip(positionWS);
                o.positionWS = positionWS;
                o.normalWS = normalWS;

                float3 albedo = RmSrgbToLinear(v.color.rgb);
                if (_RmWindowMode > 0.5)
                {
                    // La ventana de día: #90CAF9 que refleja skyHorizon por Fresnel.
                    float3 view = normalize(_WorldSpaceCameraPos - positionWS);
                    float fresnel = pow(1.0 - saturate(dot(normalWS, view)), 5.0);
                    albedo = _RmWindowDay.rgb * (1.0 - kRmWindowFresnel) + lerp(_RmWindowDay.rgb, _RmPaletteSkyHorizon.rgb, fresnel) * kRmWindowFresnel;
                }

                float3 albedoG = sqrt(max(albedo, 0.0));
                float3 ambientG, keyG;
                RmVertexLight(normalWS, 0.0, ambientG, keyG);
                o.ambientG = ambientG * albedoG;
                o.keyG = keyG * albedoG;
                o.fog = RmVertexFog(positionWS.z, positionWS.y);

                // Las tablas: en las caras verticales cada 0,18 m en y; en las horizontales cada 0,20 m en x (coordenadas de objeto). El
                // tamaño de un píxel en tablas sale de la distancia (≈ distancia/focal en m por dp): el borde de la línea mide un píxel y
                // la línea se apaga cuando mide menos de medio.
                bool horizontal = abs(v.normalOS.y) > 0.5;
                float spacing = horizontal ? _RmPlankParams.z : _RmPlankParams.x;
                float lineWidth = horizontal ? _RmPlankParams.w : _RmPlankParams.y;
                float coordinate = (horizontal ? positionOS.x : positionOS.y) / spacing;
                float pixel = max(distance(positionWS, _WorldSpaceCameraPos) / max(_RmFocalDp, 1.0), 1e-4) / spacing;
                float halfLine = 0.5 * lineWidth / spacing;
                float wood = (v.color.a < 0.25 && _RmWindowMode < 0.5) ? 1.0 : 0.0;
                o.plank = float3(coordinate, -1.0 / pixel, halfLine / pixel + 0.5);
                o.fade = RmDitherFade(UNITY_ACCESS_INSTANCED_PROP(Props, _RmFade));
                o.contrast = kRmPlankShade * wood * saturate(4.0 * halfLine / pixel - 1.0);
                o.window = float4(_RmWindowNight.rgb * _RmEmissiveGain, _RmWindowMode > 0.5 ? UNITY_ACCESS_INSTANCED_PROP(Props, _RmLit) : 0.0);
                return o;
            }

            half4 PropFragment(Varyings i) : SV_Target
            {
                RmDitherClip(i.fade, i.positionCS.xy);

                // La línea entre tablas (en las juntas, en la coordenada entera), con un píxel de borde.
                float f = frac(i.plank.x);
                float cover = saturate(min(f, 1.0 - f) * i.plank.y + i.plank.z);
                float3 c = (i.ambientG + i.keyG * RmMainShadow(i.positionWS)) * (1.0 - i.contrast * cover);
                c *= RmUnderBridgeGamma2(i.positionWS, i.normalWS);
                c = lerp(c, i.fog.rgb, i.fog.w);
                // La ventana encendida: sin tinte, sin luz y sin niebla.
                float3 lit = lerp(c * c, i.window.rgb, i.window.w);
                return half4(lit, 1.0);
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
            #pragma vertex PropShadowVertex
            #pragma fragment PropShadowFragment
            #pragma multi_compile_instancing

            struct ShadowVaryings
            {
                float4 positionCS : SV_POSITION;
                float2 fade : TEXCOORD0;
            };

            ShadowVaryings PropShadowVertex(Attributes v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                ShadowVaryings o;
                o.positionCS = RmShadowCasterClip(TransformObjectToWorld(PropMotion(v.positionOS.xyz, v.uv)), TransformObjectToWorldNormal(v.normalOS));
                o.fade = RmDitherFade(UNITY_ACCESS_INSTANCED_PROP(Props, _RmFade));
                return o;
            }

            half4 PropShadowFragment(ShadowVaryings i) : SV_Target
            {
                RmDitherClip(i.fade, i.positionCS.xy);
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
            #pragma vertex PropDepthVertex
            #pragma fragment PropDepthFragment
            #pragma multi_compile_instancing

            struct DepthVaryings
            {
                float4 positionCS : SV_POSITION;
                float2 fade : TEXCOORD0;
            };

            DepthVaryings PropDepthVertex(Attributes v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                DepthVaryings o;
                o.positionCS = TransformObjectToHClip(PropMotion(v.positionOS.xyz, v.uv));
                o.fade = RmDitherFade(UNITY_ACCESS_INSTANCED_PROP(Props, _RmFade));
                return o;
            }

            half PropDepthFragment(DepthVaryings i) : SV_Target
            {
                RmDitherClip(i.fade, i.positionCS.xy);
                return 0;
            }
            ENDHLSL
        }
    }
}
