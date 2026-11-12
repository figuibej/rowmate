// RowMate/Scene/WaterMarks: el agua que responde (07-remero-palada/02-tech-design §2.7, §3.4, §7.4; DT7-12; DA7-08; 00-arte-remero
// §8.3; 04-design-notes §4). Los charcos de las palas, los hervideros del drive y el anillo de la boya en UN dibujo instanciado de quads
// horizontales a ras del agua (el quad de la escena, UV0 de 0 a 1). Por instancia _RmMarks = (edad en s; giro en rad; alfa de Core;
// tipo: 0 charco, 1 hervidero, 2 anillo de la boya). Transparente después del agua (3050), sin profundidad, 0 texturas, sin el reloj de
// Unity (la edad y el giro los da Core; el batido del hervidero usa _RmSceneTime).
//
//   El charco (WP-D2): lo que deja la pala al salir es un PAR de remolinos separados 0,45 m (RowerArt.SwirlSeparation) a lo largo del
//   eje de la pala, que giran en sentidos opuestos (el espejo en x da la contrarrotación). Cada uno es un ojo oscuro y liso (agua sin
//   ondas que refleja menos cielo) con una media luna de espuma que gira con _RmMarks.y; el radio del ojo crece con la edad. Alrededor,
//   el borde del charco (el radio de Core, 0,3 → 0,9 m, viene en la escala del quad) como un anillo de espuma tenue. El alfa de Core es
//   el de la mancha media; la espuma llega a × kRmMarkFoamGain (a 47 dp, desde la cámara del contrato, el quad se ve de canto: sin
//   esto el charco no se lee).
//   El hervidero: espuma blanca batida bajo la pala durante el drive, que se mueve con el tiempo de escena.
//   El anillo de la boya: una línea de espuma.
// It. 6b (06b-agua-avanzada/02-tech-design §4.3; DT6b-23, G6b-02; PROVISORIO de WP-B hasta WP-D): mezcla premultiplicada (los tipos de
// antes devuelven (col·a; a) y se ven igual) y tres tipos más en el MISMO material y la misma variante (0 SetPass nuevos):
//   3 = los micro-anillos de las gotas y del ataque (_RmMarks = (−; −; alfa; 3)): el vértice los pasa al dibujo del anillo de la boya;
//   4 = la columna de luz de un farol o una ventana, acostada sobre el agua (_RmMarks = (−; −; alfa; 4), _RmMarkTint el color): el vértice
//       la pasa al dibujo del hervidero con el color de la luz y la vuelve aditiva, (col·w; 0), como el Glow, con el alfa que se apaga
//       hacia la cámara;
//   5 = la estela: una cinta fija de 3 tiras × 48 segmentos que el vértice expande desde _RmWake (WakeRenderer: la posición del objeto es
//       (tira; −; segmento) y la UV0.x el lado), con su apagado _RmWakeFade, y la pasa al dibujo del hervidero (espuma batida).
// Así el fragmento no suma ramas (MaxBranchAlu): lo único nuevo es el alfa de salida × _RmMarkTint.w del vértice (0 = aditivo).
// En la costa todo lo que se apoya en el agua sube con el mar de fondo (DT6b-21, RmSwellHeight × _RmWaterLift).
Shader "RowMate/Scene/WaterMarks"
{
    Properties
    {
        _RmEffort ("Esfuerzo (0 a 1)", Float) = 0
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Transparent" "Queue" = "Transparent+50" "IgnoreProjector" = "True" }
        ZWrite Off
        Cull Off
        Blend One OneMinusSrcAlpha

        HLSLINCLUDE
        #include "../Include/RowMateScene.hlsl"
        #include "../Include/RowMateWater.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float _RmEffort;
        CBUFFER_END

        // RowerArt (04-design-notes §4): media separación de los remolinos (m), el ojo (radio inicial y lo que crece por segundo, m), la
        // ganancia de la espuma sobre el alfa de Core y cuánto oscurece el ojo.
        static const float kRmSwirlHalf = 0.225;
        static const float kRmSwirlEye = 0.1;
        static const float kRmSwirlEyeGrowth = 0.035;
        static const float kRmMarkFoamGain = 1.6;
        static const float kRmMarkEyeAlpha = 0.55;
        // QA-V6b-03 (#12; AC-6B-17): con el agua avanzada (_RmMarkTint.x = 1 en el charco) el charco APLANA el oleaje adentro: un disco liso
        // (sin destellos ni rizo: el agua cercana, oscura) con alfa kRmPuddleFlatAlpha, los ojos de los remolinos más oscuros todavía y el
        // borde de espuma × kRmPuddleEdgeGain. Con la degradada (_RmMarkTint.x = 0), el charco de la 7 igual que antes.
        static const float kRmPuddleFlatAlpha = 1;
        // QA-V6b-07 (AC-6B-18): la columna de luz arranca a 3 m de la cámara y su alfa crece hacia el pie (0 cerca); cortada por las olas en
        // franjas de 0,9 m de mundo (WaterLookArt.ColumnStripeMeters) que corren con el agua (el ruido de 256 m de Core, con la corriente del río).
        static const float kRmColumnNear = 3;
        static const float kRmColumnNearAlpha = 0;
        static const float kRmColumnStripeMeters = 0.9;
        static const float kRmPuddleEyeDarken = 0.85;
        static const float kRmPuddleEdgeGain = 0.25;
        static const float kRmPuddleFlatDarken = 0.6;
        // La estela se apaga hacia la cámara como en el legacy (WaterModel.SternRelZ).
        static const float kRmWakeCameraFade = 5.3;
        // WP-D (WaterLookArt): el centro turbulento × 0,6 (se leía como una franja translúcida), el corte de la espuma por punto (97 vueltas
        // en 256 m ≈ 2,6 m) y la fase del batido a lo largo (171 vueltas ≈ 1,5 m).
        static const float kRmWakeCenterGain = 0.6;
        static const float kRmWakeBreakWaves = 97;
        static const float kRmWakeFoamWaves = 171;
        ENDHLSL

        Pass
        {
            Name "WaterMarks"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex MarksVertex
            #pragma fragment MarksFragment
            #pragma multi_compile_instancing

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _RmMarks)
                UNITY_DEFINE_INSTANCED_PROP(float4, _RmMarkTint)
            UNITY_INSTANCING_BUFFER_END(Props)

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float2 uv2 : TEXCOORD1;       // 0 = el quad de las marcas, 1 = la cinta de la estela (la misma malla: un solo dibujo)
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 local : TEXCOORD0;     // (u, v de −1 a 1; alfa de Core; tipo)
                float4 meters : TEXCOORD1;    // (x, z en m desde el centro; 1/radio² del ojo; −)
                float4 spin : TEXCOORD2;      // (cos, sin del giro; fase del batido en x, en y)
                float3 light : TEXCOORD3;     // la espuma (el cielo que refleja, blanqueado), lineal
                float3 dark : TEXCOORD4;      // el ojo (el agua cercana sin ondas), lineal
                float4 tint : TEXCOORD5;      // la columna de luz: su color (lineal)
            };

            Varyings MarksVertex(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                float4 m = UNITY_ACCESS_INSTANCED_PROP(Props, _RmMarks);
                float4 tint = UNITY_ACCESS_INSTANCED_PROP(Props, _RmMarkTint);
                float3 positionWS = TransformObjectToWorld(v.positionOS.xyz);
                float2 c = v.uv * 2.0 - 1.0;
                float additive = 0.0;
                float wakeLight = 1.0;
                float wakeZ = 0.0;
                bool isWake = m.w > 4.5;
                UNITY_BRANCH
                if (isWake)
                {
                    // La estela: la tira (0 brazo izquierdo, 1 derecho, 2 centro) y el punto k de la historia.
                    int k = (int)round(v.positionOS.z);
                    float4 p = _RmWake[k];
                    float4 q = _RmWake[48 + k];
                    float strip = v.positionOS.x;
                    float side = strip < 0.5 ? -1.0 : 1.0;
                    float center = strip > 1.5 ? 1.0 : 0.0;
                    float width = lerp(p.z, q.x, center);
                    float x = lerp(side * p.y, 0.0, center) + (v.uv.x - 0.5) * width;
                    positionWS = float3(x, 0.0025, p.x);
                    // WP-D (04-design-notes §3): el apagado del legacy hacia la cámara, S(0; 5,3; z) (water.frag:88, fade = 1 −
                    // S(0; sternZ; back)), AL CUADRADO (QA-V6b-02: en TV los brazos salen por los costados a z ≈ 2,5 m, donde S vale 0,45,
                    // y se leían como dos haces hasta el borde); con la cámara alta (el vuelo, la toma aérea) la V entera queda a la vista.
                    // La cabeza (alfa 0 en la popa que se dibuja, a pleno a 1,2 m) ya viene en p.w desde Core (WakeTrail.ShapedAlpha).
                    float toCamera = RmSmoothStep(0.0, kRmWakeCameraFade, p.x - _WorldSpaceCameraPos.z);
                    // La fase de MUNDO del punto (el ruido de 256 m que Core reduce en double, _RmWaterOffsets2.z; Q-68): la espuma queda
                    // quieta en el agua mientras el bote avanza. Cada punto (cada 2 m) toma un alfa de 0,55 a 1 (la espuma se corta).
                    float world = p.x / 256.0 + _RmWaterOffsets2.z;
                    float broken = 0.775 + 0.225 * sin(6.2831853 * kRmWakeBreakWaves * world + 2.1 * strip);
                    m.z = lerp(p.w, q.y * kRmWakeCenterGain, center) * _RmWakeFade * toCamera * toCamera * broken;
                    c = float2(v.uv.x * 2.0 - 1.0, 0.0);
                    m.w = 1.0;
                    // (c) La espuma con la luz de la hora (de noche ≈ 0,25 de luminancia, plateada) y rota a lo largo: la fase del batido
                    // en la z de mundo (≈ 1,5 m), así la línea no es una franja pareja.
                    wakeLight = _RmAmbient;
                    wakeZ = 6.2831853 * kRmWakeFoamWaves * world;
                }
                else if (m.w > 3.5)
                {
                    // La columna de luz: la espuma batida con el color de la luz, aditiva, que se apaga hacia la cámara.
                    // QA-V6b-07 (AC-6B-18, ronda de verificación): "debajo de su x de PANTALLA". El reflejo de una luz alta cae en el agua sobre la
                    // recta cámara-pie, no al pie: la columna va de kRmColumnNear m de la cámara hasta el pie (zF = el centro del quad + medio
                    // largo), con x = cámara + (X − cámara)·z/zF y el ancho × z/zF (los dos constantes en pantalla). Con la x de mundo, las de
                    // las fachadas y los faroles del muelle caían sobre el muelle y no se veían.
                    float3 center = float3(UNITY_MATRIX_M[0].w, UNITY_MATRIX_M[1].w, UNITY_MATRIX_M[2].w);
                    float halfLength = 0.5 * length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y));
                    float halfWidth = 0.5 * length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x));
                    float zFoot = max(center.z + halfLength - _WorldSpaceCameraPos.z, kRmColumnNear + 0.5);
                    float u = 0.5 + 0.5 * c.y;
                    float k = lerp(kRmColumnNear, zFoot, u) / zFoot;
                    positionWS = float3(_WorldSpaceCameraPos.x + (center.x - _WorldSpaceCameraPos.x + c.x * halfWidth) * k, positionWS.y,
                        _WorldSpaceCameraPos.z + k * zFoot);
                    m.z *= kRmColumnNearAlpha + (1.0 - kRmColumnNearAlpha) * u;
                    wakeZ = 6.2831853 * (positionWS.z + 256.0 * _RmWaterOffsets2.z) / kRmColumnStripeMeters;
                    m.w = 1.0;
                    additive = 1.0;
                }
                else if (m.w > 2.5)
                {
                    m.w = 2.0;
                }

                positionWS.y += _RmWaterLift * RmSwellHeight(positionWS.x, positionWS.z);
                o.positionCS = TransformWorldToHClip(positionWS);
                // El radio del quad en m (la escala del objeto en x es el diámetro: WaterInteractionRenderer).
                float radius = 0.5 * length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x));
                float eye = kRmSwirlEye + kRmSwirlEyeGrowth * m.x;
                o.local = float4(c, m.z, m.w);
                o.meters = float4(c * radius, 1.0 / (eye * eye), 0.0);
                o.spin = float4(cos(m.y), sin(m.y), 8.0 * _RmSceneTime, wakeZ - 6.0 * _RmSceneTime);
                float ambient = (0.55 + 0.45 * _RmAmbient) * wakeLight;
                o.light = lerp(_RmPaletteSkyHorizon.rgb, float3(1.0, 1.0, 1.0), 0.6) * ambient;
                o.dark = _RmPaletteWaterNear.rgb * 0.6;
                o.light = lerp(o.light, tint.rgb, additive);
                o.dark = lerp(o.dark, tint.rgb, additive);
                // xyz: el color de la columna (ya en light/dark); x en el charco: el aplanado (1 con la avanzada).
                o.tint = float4(tint.rgb, 1.0 - additive);
                // La malla junta el quad y la cinta (WakeRenderer.BuildCombined): cada instancia usa solo su parte; la otra queda degenerada
                // fuera del recorte (la estela es una instancia más del lote de marcas: 0 dibujos y 0 SetPass propios).
                if (abs(v.uv2.x - (isWake ? 1.0 : 0.0)) > 0.5)
                {
                    o.positionCS = float4(0.0, 0.0, -2.0, 1.0);
                }

                return o;
            }

            half4 MarksFragment(Varyings i) : SV_Target
            {
                float2 c = i.local.xy;
                float r2 = dot(c, c);
                float kind = i.local.w;
                float foam;
                float eye = 0.0;
                float darken = 0.0;
                // Ramas de verdad: una instancia es de un solo tipo y cubre una parte chica de la pantalla (MaxBranchAlu).
                [branch] if (kind < 0.5)
                {
                    // Los dos remolinos: el ojo liso y la media luna de espuma que gira (en el ojo derecho, al revés por el espejo).
                    float2 q = float2(abs(i.meters.x) - kRmSwirlHalf, i.meters.y);
                    float q2 = dot(q, q) * i.meters.z;
                    eye = saturate(1.0 - q2);
                    float crescent = saturate(0.5 + 2.0 * dot(q, i.spin.xy) * sqrt(i.meters.z));
                    float flat = i.tint.x;
                    foam = saturate(1.0 - abs(q2 - 1.2) * 2.5) * crescent + (0.35 + kRmPuddleEdgeGain * flat) * saturate(1.0 - abs(r2 - 0.72) * 6.0);
                    // El aplanado: el disco liso y más oscuro (sin el cielo que reflejan las ondas) hasta el borde de espuma (r² 0,62), suave
                    // en 0,25 de r²; los ojos, más oscuros todavía.
                    float disk = flat * saturate((0.62 - r2) * 4.0);
                    darken = flat * max(kRmPuddleEyeDarken * eye, kRmPuddleFlatDarken * disk);
                    eye = max(eye * kRmMarkEyeAlpha, kRmPuddleFlatAlpha * disk) / kRmMarkEyeAlpha;
                }
                else [branch] if (kind < 1.5)
                {
                    // El hervidero: espuma batida (dos senos que se mueven con el tiempo de escena), más densa en el centro.
                    float churn = 0.65 + 0.35 * sin(c.x * 6.0 + i.spin.z) * sin(c.y * 5.0 + i.spin.w);
                    foam = saturate(1.0 - r2) * churn * (0.8 + 0.4 * _RmEffort);
                }
                else
                {
                    // El anillo de la boya.
                    foam = saturate(1.0 - abs(r2 - 0.72) * 7.0);
                }

                float3 color = lerp(i.dark * (1.0 - darken), i.light, saturate(foam * 3.0));
                float a = saturate(kRmMarkFoamGain * foam + kRmMarkEyeAlpha * eye) * i.local.z * step(r2, 1.0);
                return half4(color * a, a * i.tint.w);
            }
            ENDHLSL
        }
    }
}
