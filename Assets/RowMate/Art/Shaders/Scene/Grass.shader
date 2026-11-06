// RowMate/Scene/Grass: el pasto y los helechos del relleno (06a-escena-lago/04-design-notes §5.5, §7.4, §10.1; 02-tech-design §3.6,
// §7.5; DN6a-16; WP-D1). Instanciado, en la cola 2000, opaco: mallas de hojas opacas sin alpha test (SmallMeshes.GrassTuft: r = la
// oclusión, g = el peso del viento), así el sobre-dibujo de la franja de vegetación queda ≤ 2,5×.
// El color: el verde del pasto del terreno (#4F7A3A ↔ #6B8A3E, _RmTint.y) o el del helecho (_RmTint.z = 1), × _RmTint.x (± 8 %).
// El fin (_RmGrassEnd = (fin en m; 0,2; -; -): 60 / 40 / 30 m por nivel, MaterialArt.GrassEnd): en el último 20 % la mata se achica
// en el vértice hasta desaparecer, sin cortarla (el terreno lleva el mismo verde). Con la densidad de la adaptación (_RmDensity < 1)
// se achican también las matas cuyo número fijo (_RmTint.w, de 0 a 1) la supera: la densidad baja sin rearmar las instancias.
// El viento: 0,08·sin(2,3t + fase) × el peso (_RmWind; _RmWindPhase.x = x·0,9 + z·0,7 en double desde quien instancia).
Shader "RowMate/Scene/Grass"
{
    Properties
    {
        _RmGrassColorA ("Pasto", Color) = (0.309804, 0.478431, 0.227451, 1)
        _RmGrassColorB ("Pasto (el otro verde)", Color) = (0.419608, 0.541176, 0.243137, 1)
        _RmFernColor ("Helecho", Color) = (0.247059, 0.415686, 0.207843, 1)
        _RmGrassEnd ("Fin (m; fracción del tramado; -; -)", Vector) = (40, 0.2, 0, 0)
        _RmWind ("Viento (amplitud en rad; frecuencia en rad/s; ráfaga en rad; período de la ráfaga en s)", Vector) = (0.08, 2.3, 0, 9)
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry" "IgnoreProjector" = "True" }
        Cull Off

        HLSLINCLUDE
        #include "../Include/RowMateScene.hlsl"
        #include "../Include/RowMateLighting.hlsl"
        #include "../Include/RowMateFog.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _RmGrassColorA;
            float4 _RmGrassColorB;
            float4 _RmFernColor;
            float4 _RmGrassEnd;
            float4 _RmWind;
        CBUFFER_END

        UNITY_INSTANCING_BUFFER_START(Props)
            UNITY_DEFINE_INSTANCED_PROP(float4, _RmTint)        // (brillo; verde A → B; helecho 0/1; número fijo de la mata 0 a 1)
            UNITY_DEFINE_INSTANCED_PROP(float4, _RmWindPhase)   // (fase en rad; escala de la amplitud; -; -)
        UNITY_INSTANCING_BUFFER_END(Props)

        struct Attributes
        {
            float4 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float4 color : COLOR;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        // La posición de mundo con el viento, achicada en el fin y por la densidad.
        float3 GrassPosition(Attributes v)
        {
            float3 positionWS = TransformObjectToWorld(v.positionOS.xyz);
            float3 originWS = TransformObjectToWorld(float3(0.0, 0.0, 0.0));
            float4 tint = UNITY_ACCESS_INSTANCED_PROP(Props, _RmTint);
            float4 phase = UNITY_ACCESS_INSTANCED_PROP(Props, _RmWindPhase);
            float t = _RmSceneTime;
            float gust = (RmValueNoise1(t / _RmWind.w) * 2.0 - 1.0) * _RmWind.z;
            float angle = (_RmWind.x * sin(_RmWind.y * t + phase.x) + gust) * phase.y;
            positionWS.x += angle * v.color.g * (positionWS.y - originWS.y);
            float distance = length(originWS.xz - _WorldSpaceCameraPos.xz);
            float end = _RmGrassEnd.x;
            // _RmDensity sin poner (0) cuenta como 1: la adaptación nunca la baja de 0,5.
            float density = _RmDensity > 0.0 ? _RmDensity : 1.0;
            float scale = saturate((end - distance) / (end * _RmGrassEnd.y)) * step(tint.w, density);
            return originWS + (positionWS - originWS) * scale;
        }
        ENDHLSL

        Pass
        {
            Name "Grass"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex GrassVertex
            #pragma fragment GrassFragment
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 ambientG : TEXCOORD1;    // ya con el albedo y la oclusión
                float3 keyG : TEXCOORD2;
                float4 fog : TEXCOORD3;
            };

            Varyings GrassVertex(Attributes v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                Varyings o;
                float3 positionWS = GrassPosition(v);
                float3 normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.positionCS = TransformWorldToHClip(positionWS);
                o.positionWS = positionWS;

                float4 tint = UNITY_ACCESS_INSTANCED_PROP(Props, _RmTint);
                float3 albedo = lerp(lerp(_RmGrassColorA.rgb, _RmGrassColorB.rgb, saturate(tint.y)), _RmFernColor.rgb, saturate(tint.z)) * tint.x;
                float3 ambientG, keyG;
                RmVertexLight(normalWS, kRmGrassWrap, ambientG, keyG);
                float3 albedoG = sqrt(max(albedo, 0.0)) * pow(saturate(v.color.r), 1.1);
                o.ambientG = ambientG * albedoG;
                o.keyG = keyG * albedoG;
                o.fog = RmVertexFog(positionWS.z, positionWS.y);
                return o;
            }

            half4 GrassFragment(Varyings i) : SV_Target
            {
                float3 c = i.ambientG + i.keyG * RmMainShadow(i.positionWS);
                c = lerp(c, i.fog.rgb, i.fog.w);
                return half4(c * c, 1.0);
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
            #pragma vertex GrassDepthVertex
            #pragma fragment GrassDepthFragment
            #pragma multi_compile_instancing

            float4 GrassDepthVertex(Attributes v) : SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(v);
                return TransformWorldToHClip(GrassPosition(v));
            }

            half GrassDepthFragment() : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
}
