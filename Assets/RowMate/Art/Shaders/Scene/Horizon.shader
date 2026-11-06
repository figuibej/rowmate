// RowMate/Scene/Horizon: las dos cordilleras del lago (06a-escena-lago/04-design-notes §6; 02-tech-design §2.9, §3.6, §7.5; WP-D1).
// Las tiras de HorizonStripMesh (Core): 260 columnas de −65° a +65°, posición (acimut en rad; p del perfil del legacy; 0) y color de
// vértice (r = la máscara de nieve de Core en la cresta, g = la pendiente, b = u, a = 0 abajo / 1 en la cresta). Un anillo de
// elevación fija por acimut centrado en la cámara (sin paralaje lateral), en la cola 1003 y sin profundidad:
//   tan(elevación) = tan Θ·k(d)·alto·p, con _RmHorizonScale = (tan Θ; k(d); alto: 1 el lejano, 0,7 el cercano; -) (P-11, Q-65).
// El color es el del contrato (_RmHorizonTone = HorizonModel.FarColor o NearColor de la hora, exacto en Core, en lineal), con los
// flancos (+12 % el que mira al sol y −10 % el otro, de día), la base (el 25 % de abajo, 0,15 más hacia fog), la nieve del lejano
// (sobre el 58 % del perfil con borde de ± 0,03 y lenguas de ± 0,04 cada 1,7°: la máscara de Core por columna y la línea de nieve
// local, DV-64) con su alpenglow (0,35·g hacia sunGlow) y su brillo de noche (× (0,55 + 0,45·il·vis)), y el borde encendido cerca del
// sol (P2, DN6a-12: el contraluz de la hora dorada, más fuerte hacia la cresta). Todo en gamma 2 y al cuadrado al final.
Shader "RowMate/Scene/Horizon"
{
    Properties
    {
        _RmHorizonTone ("Color de la cordillera (HorizonModel, de la hora)", Color) = (0.36, 0.43, 0.54, 1)
        _RmHorizonDepth ("Profundidad (1 el lejano, 0,3 el cercano)", Float) = 1
        _RmSnowEnabled ("Nieve (1 el lejano, 0 el cercano)", Float) = 1
        _RmHorizonScale ("Alto (tan Θ; k(d); alto relativo; -)", Vector) = (0.1583844, 1, 1, 0)
        _RmSnowParams ("Nieve (umbral; borde; ruido; período en grados)", Vector) = (0.58, 0.03, 0.04, 1.7)
        _RmRim ("Borde encendido (alto en grados; ancho en grados; fuerza; caída en grados)", Vector) = (0.35, 8, 0.6, 6)
        _RmSnowTone ("Color de la nieve (antes de la niebla)", Color) = (0.94902, 0.964706, 0.980392, 1)
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Background+3" "IgnoreProjector" = "True" }
        ZWrite Off
        ZTest Always
        Cull Off

        HLSLINCLUDE
        #include "../Include/RowMateScene.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _RmHorizonTone;
            float _RmHorizonDepth;
            float _RmSnowEnabled;
            float4 _RmHorizonScale;
            float4 _RmSnowParams;
            float4 _RmRim;
            float4 _RmSnowTone;
        CBUFFER_END

        static const float kRmFlankToSun = 0.12;
        static const float kRmFlankAway = -0.1;
        static const float kRmFlankRampRad = 0.104719755;
        static const float kRmHorizonBaseFog = 0.15;
        static const float kRmHorizonBaseFraction = 0.25;
        static const float kRmAlpenglow = 0.35;
        static const float kRmSnowFog = 0.3;
        static const float kRmSnowNightBase = 0.55;
        static const float kRmSnowNightPerIllumination = 0.45;
        ENDHLSL

        Pass
        {
            Name "Horizon"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex HorizonVertex
            #pragma fragment HorizonFragment

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 local : TEXCOORD0;       // (p·rampa de la nieve + corrimiento; fracción de la columna; máscara de Core × fracción; borde encendido × fracción)
                float3 tone : TEXCOORD1;        // el color con los flancos, en gamma 2
                float3 toFog : TEXCOORD2;       // fog − tono (gamma 2)
                float3 snow : TEXCOORD3;        // la nieve en gamma 2
                float3 rim : TEXCOORD4;         // sunGlow (gamma 2) × el borde encendido × la fracción de la columna
            };

            Varyings HorizonVertex(Attributes v)
            {
                Varyings o;
                float az = v.positionOS.x;
                float p = v.positionOS.y;
                float tanElevation = _RmHorizonScale.x * _RmHorizonScale.y * _RmHorizonScale.z * p;
                o.positionCS = TransformWorldToHClip(RmSkyPoint(float3(sin(az), tanElevation, cos(az))));

                // Los flancos: de día, la cara que mira al acimut del sol + 12 %, la otra − 10 % (× la pendiente y la visibilidad). El lado
                // cambia en ± 6° alrededor del acimut del sol (WP-D2: con el signo seco quedaba una costura vertical debajo del sol de las 12 h).
                float sunAz = atan2(_RmDiskDir.x, _RmDiskDir.z);
                float facing = v.color.g * clamp((az - sunAz) / kRmFlankRampRad, -1.0, 1.0);
                float daySun = (1.0 - _RmDiskParams.y) * _RmDiskParams.x;
                float flank = (facing > 0.0 ? kRmFlankToSun : kRmFlankAway) * abs(facing) * daySun;
                float3 tone = sqrt(max(_RmHorizonTone.rgb, 0.0)) * (1.0 + flank);
                float3 fogG = sqrt(max(_RmPaletteFog.rgb, 0.0));
                float3 glowG = sqrt(max(_RmPaletteSunGlow.rgb, 0.0));
                o.tone = tone;
                o.toFog = fogG - tone;

                // La nieve: lerp(#F2F6FA; fog; 0,3), el alpenglow y el brillo de noche con la luna.
                float3 snowG = lerp(sqrt(max(_RmSnowTone.rgb, 0.0)), fogG, kRmSnowFog);
                snowG = lerp(snowG, glowG, kRmAlpenglow * _RmGolden);
                float night = lerp(1.0, kRmSnowNightBase + kRmSnowNightPerIllumination * _RmDiskParams.z * _RmDiskParams.x, _RmDiskParams.y);
                o.snow = snowG * night;

                // La línea de nieve local: (p + ruido − (umbral − borde))/(2·borde), con las lenguas: ± 0,04 de p con un ruido de valor
                // cada 1,7° de acimut (el fragmento hace una sola rampa). p entra interpolado (es lineal en la columna).
                float noise = (RmValueNoise1(degrees(az) / _RmSnowParams.w) * 2.0 - 1.0) * _RmSnowParams.z;
                float ramp = 1.0 / (2.0 * _RmSnowParams.y);
                float isTop = v.color.a;
                o.local = float4((p + noise - (_RmSnowParams.x - _RmSnowParams.y)) * ramp, isTop, v.color.r * _RmSnowEnabled, 0.0);

                // El borde encendido (P2): con el sol de día cerca de su acimut, la cresta se enciende hacia arriba.
                float dAz = degrees(az - sunAz);
                float rim = _RmRim.z * _RmGolden * daySun * exp(-(dAz * dAz) / (_RmRim.w * _RmRim.w)) * step(abs(dAz), _RmRim.y);
                o.rim = glowG * (rim * isTop);
                o.local.w = rim * isTop;
                return o;
            }

            half4 HorizonFragment(Varyings i) : SV_Target
            {
                float v = i.local.y;
                // La base del cordón, hacia fog.
                float3 c = i.tone + i.toFog * max(kRmHorizonBaseFog - v * (kRmHorizonBaseFog / kRmHorizonBaseFraction), 0.0);
                // La nieve: la máscara de Core de la columna (r/a) × la línea de nieve local.
                float snow = (i.local.z / max(v, 1e-3)) * saturate(i.local.x);
                // El borde encendido lleva el color hacia sunGlow (WP-D2: sumado, la nieve con el alpenglow pasaba de 1 y el macizo de las 6 h
                // era una mancha blanca): lerp(c; sunGlow; rim·v) = c·(1 − rim·v) + sunGlow·rim·v.
                c = lerp(c, i.snow, snow);
                c = c * (1.0 - i.local.w) + i.rim;
                return half4(c * c, 1.0);
            }
            ENDHLSL
        }
    }
}
