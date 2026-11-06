// RowMateFog.hlsl: la niebla de la orilla (06a-escena-lago/04-design-notes §10.3; ShoreShading de Core; AC-SCN-20-01; WP-D1).
//
//   f = 0,8·clamp((z − 80)/320) + 0,2·clamp((z − 400)/800)        hasta 400 m es el _c del legacy (shore_painter.dart:48-49)
//   bruma baja: máx(f; m(h)·exp(−máx(y; 0)/1,2)·S(10; 150; z)), con el color lerp(fog; sunGlow; 0,25·g)
//
// z es la distancia hacia adelante desde la cámara del contrato (la z de cinta) e y la altura sobre el agua. Se evalúa en el vértice
// (las dos son suaves) y el fragmento solo mezcla, en gamma 2 (RowMateLighting.hlsl). Las luces de la noche no pasan por acá
// (AC-SCN-09-06).
#ifndef ROWMATE_FOG_INCLUDED
#define ROWMATE_FOG_INCLUDED

#include "RowMateScene.hlsl"

static const float kRmMistHeight = 1.2;
static const float kRmMistNear = 10;
static const float kRmMistFar = 150;
static const float kRmMistGolden = 0.25;

// ShoreShading.ExtendedFog con _RmFogParams = (80; 320; 0,8; 1 200).
float RmExtendedFog(float z)
{
    float farStart = _RmFogParams.x + _RmFogParams.y;
    return _RmFogParams.z * saturate((z - _RmFogParams.x) / _RmFogParams.y)
         + (1.0 - _RmFogParams.z) * saturate((z - farStart) / (_RmFogParams.w - farStart));
}

// El color de la bruma baja en lineal: lerp(fog; sunGlow; 0,25·g).
float3 RmMistColor()
{
    return lerp(_RmPaletteFog.rgb, _RmPaletteSunGlow.rgb, kRmMistGolden * _RmGolden);
}

// El peso de la bruma baja (AC-SCN-15-07): solo de 5 a 9:30 (m), pegada al agua y desde los 10 m.
float RmLowMist(float z, float y)
{
    return _RmLowMist * exp(-max(y, 0.0) / kRmMistHeight) * RmSmoothStep(kRmMistNear, kRmMistFar, z);
}

// La niebla de un vértice: el color en gamma 2 (xyz) y el peso (w) para el fragmento: c = lerp(c; xyz; w).
float4 RmVertexFog(float z, float y)
{
    float fog = RmExtendedFog(z);
    float mist = RmLowMist(z, y);
    float weight = max(fog, mist);
    float share = mist / max(fog + mist, 1e-4);
    float3 color = lerp(sqrt(max(_RmPaletteFog.rgb, 0.0)), sqrt(max(RmMistColor(), 0.0)), share);
    return float4(color, weight);
}

#endif
