// RowMateLighting.hlsl: el modelo de luz de la escena (06a-escena-lago/04-design-notes §10.1; DN6a-13; WP-D1).
//
//   luz(N) = T(a) ⊙ [ 0,65·Hemi(N) + 0,55·Kn·envolvente(N·L; w)·sombra ]
//   T(a) = (a; a; mín(1; a + 0,12))                         el tinte del legacy (shore_painter.dart:47): manda en la escena
//   Hemi(N) = lerp(norm(waterNear); lerp(norm(skyHorizon); norm(skyTop); máx(N.y; 0)); 0,5 + 0,5·N.y)
//   Kn = sunColor/máx(sunColor)·(I/1,75)                    la luz clave relativa al mediodía
//
// La luz es un multiplicador del espacio del legacy (sRGB, donde el tinte multiplica): T(a)·{1,2; 0,65} × el albedo en la sonda de la
// cabaña. Se calcula en el vértice (la parte que no depende de la sombra) y pasa a "gamma 2" (√ del lineal) con la potencia 2,2/2: el
// fragmento sombrea en gamma 2, donde un producto y un lerp reproducen los del legacy (Color.lerp de la niebla) a ± 1 nivel, y eleva
// al cuadrado una sola vez al final (RmFromGamma2). Los _RmAmbient* ya traen T(a) y el 0,65 (HourLook de Core).
#ifndef ROWMATE_LIGHTING_INCLUDED
#define ROWMATE_LIGHTING_INCLUDED

#include "RowMateScene.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

static const float kRmLegacyBlueLift = 0.12;
static const float kRmKeyWeight = 0.45;
static const float kRmNoonIntensity = 1.75;
static const float kRmFactorGamma = 2.2;
static const float kRmFoliageWrap = 0.5;
static const float kRmGrassWrap = 0.2;

// T(a), el tinte del legacy.
float3 RmLegacyTint()
{
    float a = _RmAmbient;
    return float3(a, a, min(1.0, a + kRmLegacyBlueLift));
}

// Un color lineal en gamma 2 (√) y de vuelta.
float3 RmToGamma2(float3 linearColor)
{
    return sqrt(max(linearColor, 0.0));
}

float3 RmFromGamma2(float3 g)
{
    return g * g;
}

// Un color sRGB (un color de vértice: Unity no los convierte) en gamma 2.
float3 RmSrgbToGamma2(float3 srgb)
{
    return sqrt(RmSrgbToLinear(srgb));
}

// La luz clave en el espacio del legacy: T(a) ⊙ Kn·0,55.
float3 RmKeyLegacy()
{
    float3 sun = sqrt(max(_RmPaletteSunColor.rgb, 0.0));
    float m = max(max(sun.r, sun.g), max(sun.b, 1e-4));
    return RmLegacyTint() * (sun / m) * (kRmKeyWeight * _RmLightDir.w / kRmNoonIntensity);
}

// La luz de un vértice con normal N y envolvente w, en gamma 2: la que llega sin sombra (ambientG) y lo que suma la clave con la
// sombra en 1 (keyG). En el fragmento: luz = ambientG + keyG·sombra.
void RmVertexLight(float3 N, float wrap, out float3 ambientG, out float3 keyG)
{
    float ny = N.y;
    float3 hemi = lerp(_RmAmbientGround.rgb, lerp(_RmAmbientEquator.rgb, _RmAmbientSky.rgb, saturate(ny)), 0.5 + 0.5 * ny);
    float envelope = max(0.0, (dot(N, _RmLightDir.xyz) + wrap) / (1.0 + wrap));
    float3 full = hemi + RmKeyLegacy() * envelope;
    float e = kRmFactorGamma * 0.5;
    ambientG = pow(max(hemi, 1e-6), e);
    keyG = pow(max(full, 1e-6), e) - ambientG;
}

// It. 7 · El relleno del frente del remero (ART_BIBLE §7.9): amount·sunColor·saturate(N·V), en gamma 2. El astro casi siempre queda
// adelante del remero (detrás de la cámara no hay luz clave), así que sin esto el frente es solo ambiente y se lee plano.
float3 RmFrontFill(float3 N, float3 V, float amount)
{
    return sqrt(max(_RmPaletteSunColor.rgb, 0.0)) * (amount * saturate(dot(N, V)));
}

// La sombra de la luz principal de URP (la fuerza la pone SceneLighting en la luz: de noche 0 en Medio sin apagar el mapa, DT6a-19).
float RmMainShadow(float3 positionWS)
{
#if defined(_MAIN_LIGHT_SHADOWS) || defined(_MAIN_LIGHT_SHADOWS_CASCADE)
    return MainLightRealtimeShadow(TransformWorldToShadowCoord(positionWS));
#else
    return 1.0;
#endif
}

// La posición de la pasada de sombras (la de URP: ShadowCasterPass.hlsl) para una posición y una normal de mundo.
float3 _LightDirection;

float4 RmShadowCasterClip(float3 positionWS, float3 normalWS)
{
    float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));
    return ApplyShadowClamping(positionCS);
}

#endif
