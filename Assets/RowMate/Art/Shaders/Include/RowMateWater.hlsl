// RowMateWater.hlsl: lo compartido por el agua de la 6b (06b-agua-avanzada/02-tech-design §3.4, §4, §5; 00-arte-agua §2; DT6b-12,
// DT6b-21, DT6b-23). Lo incluyen WaterDegraded (el tope de las 10 líneas, #2), WaterAdvanced (el contrato de water.frag, el salto
// degradado del fundido) y WaterMarks (la estela, las columnas de luz y la ola de la costa en lo que flota).
//
// PROVISORIO (WP-B, mientras WP-D no escribió su versión): los números son los del contrato del legacy (WaterModel, water.frag:55-98)
// y de la tabla del arte (WaterLookArt), con su fuente en Core (ShaderArt.WaterConstants, WaterShaderContractTests). El agua trabaja en
// cinta: nunca ve la distancia de escena (Q-68); las fases y los corrimientos llegan reducidos en double desde Core (WavePhases).
#ifndef ROWMATE_WATER_INCLUDED
#define ROWMATE_WATER_INCLUDED

#include "RowMateScene.hlsl"

// ── El contrato (WaterModel) y el arte (WaterLookArt) ──
static const float kRmWaterBaseNear = 2;
static const float kRmWaterBaseFar = 120;
static const float kRmWaterColorGain = 0.25;
static const float kRmWaterFresnelGain = 0.6;
static const float kRmWaterFogNear = 80;
static const float kRmWaterFogFar = 400;
static const float kRmColumnBaseDp = 25;
static const float kRmColumnPerMeterDp = 4;
static const float kRmColumnPeak = 0.4;
static const float kRmWakeFoamMax = 0.35;
static const float kRmCrestFoamMax = 0.35;
static const float kRmContactDarken = 0.75;
static const float kRmContactFalloff = 0.06;
static const float kRmDegradedLineMax = 9;

// ── Los globales del agua (los pone SceneHost por cuadro, SceneShaderIds) ──
float4 _RmWaterPhases;               // (φ₁; φ₂; amplitud A; escala s) de B0/B1 (WavePhases, CoastSwell generalizado)
float4 _RmWaterOffsets;              // (B2 u; B2 v; B3 u; B3 v): los corrimientos de los mapas, módulo 1
float4 _RmWaterOffsets2;             // (B4 u; B4 v; ruido v; 0: la malla ya no se ancla, DT6b-27)
float  _RmWaterLift;                 // cuánto sube lo que flota con la ola (CoastSwell.HullScale en la costa; 0 en los demás)
float  _RmDegradedBlend;             // 1 = se ve la degradada, 0 = la avanzada (WaterFade, DT6b-12)
float  _RmWakeFade;                  // la estela que se apaga al caer a la degradada (1 → 0 en 0,6 s)
float4 _RmWake[96];                  // la estela (WakeRenderer): [k] = (relZ; semiancho de la V; ancho del brazo; alfa), [48 + k] = (ancho del centro; alfa del centro; −; −)
TEXTURE2D(_RmWaterReflectionTex);    // el reflejo planar (Alto)
SAMPLER(sampler_RmWaterReflectionTex);

// La ola del legacy sin ruido (water.frag:46-49) en cinta, con las fases de Core: h ∈ [−1; 1].
float RmSwellHeight(float x, float z)
{
    float s = max(_RmWaterPhases.w, 1e-3);
    return sin(z / s + _RmWaterPhases.x) * 0.6 + sin((0.7 * z + 1.3 * x) / s + _RmWaterPhases.y) * 0.4;
}

// #2 (QA-V6a-PAR-01): el índice de la línea de brillo de la degradada, con tope: n ∈ [0; 9] (10 líneas; la 11.ª salía en la fila de
// abajo, con t = 1).
float RmDegradedLineIndex(float v)
{
    return clamp(round(v), 0.0, kRmDegradedLineMax);
}

// El peso del reflejo del legacy sobre la normal plana (DT6b-03): 0,6·(1 − 2,2/√(z² + 2,2²))³.
float RmReflectionWeight(float z, float camHeight)
{
    float c = saturate(camHeight * rsqrt(z * z + camHeight * camHeight));
    float k = 1.0 - c;
    return kRmWaterFresnelGain * k * k * k;
}

#endif
