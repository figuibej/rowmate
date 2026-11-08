// RowMate/Scene/Glow: las luces de la noche de río, costa y regata (08-entornos-selector/02-tech-design §3.3, §8.4; 00-arte-entornos
// §2.4, §3.4; 04-design-notes §5; AC-SCN-20-04; DT8-11; WP-D1). Instanciado (GlowRenderer, un dibujo) sobre el quad de la escena, aditivo
// en la cola 3010, sin profundidad. _RmGlow por instancia = (radio o largo; alfa con el nivel de la luz de NightLights; tipo; fase):
//   tipo 0: el halo de un farol (r 1,5 m, #FFE082 al 35 %, shore_painter.dart:191-200) o de la sala de la torre;
//   tipo 1: el halo de la linterna del faro (r 3 m, #FFF59D al 35 %, shore_painter.dart:217-227) con el destello: × 2,2 cuando el haz
//           apunta a la cámara (± 6°);
//   tipo 2: medio haz del faro: un cono plano de 60 m y 4° de apertura que gira 360° cada 12 s de escena (la fase: 0 o π), de cara a la
//           cámara alrededor de su eje, del alfa en la linterna a 0 en la punta.
// El halo es un cartel que mira a la cámara, adelantado 0,6 × el radio hacia ella (la linterna y el globo no lo cortan), con caída
// (1 − r)². Todo con la niebla extendida (el aditivo se apaga con la distancia) y × _RmEmissiveGain. Sin columnas de luz en el agua
// (DT8-11: son de la 6b). Nada usa _Time: el giro va con _RmSceneTime (sigue en pausa, como el resto de la vida de la escena).
Shader "RowMate/Scene/Glow"
{
    Properties
    {
        _RmGlowLamp ("Halo de farol y torre (#FFE082)", Color) = (1, 0.878431, 0.509804, 1)
        _RmGlowBeacon ("Halo y haz del faro (#FFF59D)", Color) = (1, 0.960784, 0.615686, 1)
        _RmGlowBeam ("Haz (largo en m; apertura en grados; período en s; destello)", Vector) = (60, 4, 12, 2.2)
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Transparent" "Queue" = "Transparent+10" "IgnoreProjector" = "True" }
        Blend One One
        ZWrite Off
        ZTest LEqual
        Cull Off

        HLSLINCLUDE
        #include "../Include/RowMateScene.hlsl"
        #include "../Include/RowMateFog.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _RmGlowLamp;
            float4 _RmGlowBeacon;
            float4 _RmGlowBeam;
        CBUFFER_END

        UNITY_INSTANCING_BUFFER_START(Props)
            UNITY_DEFINE_INSTANCED_PROP(float4, _RmGlow)    // (radio en m; alfa × nivel de la luz; tipo 0/1/2; fase del haz en rad)
        UNITY_INSTANCING_BUFFER_END(Props)

        static const float kRmGlowPull = 0.6;
        static const float kRmBeamFlashCos = 0.994521895;
        static const float kRmBeamRootWidth = 0.4;

        struct Attributes
        {
            float4 positionOS : POSITION;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };
        ENDHLSL

        Pass
        {
            Name "Glow"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex GlowVertex
            #pragma fragment GlowFragment
            #pragma multi_compile_instancing

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 uv : TEXCOORD0;          // (x; y; tipo): radial en el halo, (de costado; a lo largo) en el haz
                float3 color : TEXCOORD1;       // lineal, con el alfa, la niebla y la ganancia
            };

            Varyings GlowVertex(Attributes v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                Varyings o;
                float4 g = UNITY_ACCESS_INSTANCED_PROP(Props, _RmGlow);
                float3 center = TransformObjectToWorld(float3(0.0, 0.0, 0.0));
                float2 q = v.positionOS.xy * 2.0;
                float3 toCamera = _WorldSpaceCameraPos - center;
                float distanceToCamera = max(length(toCamera), 1e-3);
                float3 view = toCamera / distanceToCamera;
                float beamAngle = 6.2831853 * frac(_RmSceneTime / max(_RmGlowBeam.z, 1e-3)) + g.w;
                float3 axis = float3(cos(beamAngle), 0.0, sin(beamAngle));
                float alpha = g.y;
                float3 color = _RmGlowBeacon.rgb;
                float3 positionWS;
                float2 uv;
                if (g.z < 1.5)
                {
                    // El halo: un cartel de cara a la cámara, adelantado hacia ella.
                    float3 right = UNITY_MATRIX_V[0].xyz;
                    float3 up = UNITY_MATRIX_V[1].xyz;
                    float3 c = center + view * min(g.x * kRmGlowPull, 0.5 * distanceToCamera);
                    positionWS = c + (right * q.x + up * q.y) * g.x;
                    uv = q;
                    if (g.z < 0.5)
                    {
                        color = _RmGlowLamp.rgb;
                    }
                    else
                    {
                        // El destello: el haz (cualquiera de los dos) apunta a la cámara.
                        float2 h = normalize(view.xz + float2(1e-5, 0.0));
                        float facing = abs(dot(h, axis.xz));
                        alpha *= 1.0 + (_RmGlowBeam.w - 1.0) * RmSmoothStep(kRmBeamFlashCos, 1.0, facing);
                    }
                }
                else
                {
                    // Medio haz: de la linterna hacia afuera, ensanchándose con la apertura.
                    float along = (q.y * 0.5 + 0.5) * _RmGlowBeam.x;
                    float3 side = normalize(cross(axis, view) + float3(0.0, 1e-4, 0.0));
                    float halfWidth = kRmBeamRootWidth + tan(radians(_RmGlowBeam.y * 0.5)) * along;
                    positionWS = center + axis * along + side * (q.x * halfWidth);
                    uv = float2(q.x, q.y * 0.5 + 0.5);
                }

                o.positionCS = TransformWorldToHClip(positionWS);
                float fog = RmExtendedFog(max(positionWS.z, 0.0));
                o.color = color * (alpha * (1.0 - fog) * _RmEmissiveGain);
                o.uv = float3(uv, g.z);
                return o;
            }

            half4 GlowFragment(Varyings i) : SV_Target
            {
                // El halo cae como (1 − r)²; el haz, (1 − |x|)² de costado y lineal a lo largo.
                float halo = saturate(1.0 - length(i.uv.xy));
                float across = saturate(1.0 - abs(i.uv.x));
                float a = i.uv.z < 1.5 ? halo * halo : across * across * (1.0 - i.uv.y);
                return half4(i.color * a, 0.0);
            }
            ENDHLSL
        }
    }
}
