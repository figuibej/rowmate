// RowMateScene.hlsl: los globales de la escena del inmersivo y las cuentas de Core que copian los shaders
// (06a-escena-lago/02-tech-design §7.5; 04-design-notes §14.1; WP-D1). Ningún shader de la escena lee el reloj de Unity: el tiempo es
// _RmSceneTime (el tiempo de escena de Core, que sigue en pausa) y las fases que dependen de la distancia llegan acotadas desde Core
// en double (Q-68). Los nombres son los de SceneShaderIds (WP-B0) y los números, los de Core/Scene/Art (ShaderArt.Constants).
#ifndef ROWMATE_SCENE_INCLUDED
#define ROWMATE_SCENE_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

// ── Los globales (los pone SceneLighting en cada cuadro) ──
float  _RmSceneTime;                 // s de escena
float4 _RmPaletteSkyTop;             // la paleta de la hora, en lineal (Shader.SetGlobalColor)
float4 _RmPaletteSkyHorizon;
float4 _RmPaletteWaterNear;
float4 _RmPaletteWaterFar;
float4 _RmPaletteSunColor;
float4 _RmPaletteSunGlow;
float4 _RmPaletteFog;
float  _RmAmbient;                   // el ambiente del legacy (0,35 a 1)
float4 _RmAmbientSky;                // T(a) ⊙ norm(skyTop)·0,65: multiplicadores del espacio del legacy (SetGlobalVector, sin convertir)
float4 _RmAmbientEquator;            // T(a) ⊙ norm(skyHorizon)·0,65
float4 _RmAmbientGround;             // T(a) ⊙ norm(waterNear)·0,65
float4 _RmFogParams;                 // (80; 320; 0,8; 1 200): la niebla del contrato y la extendida
float  _RmLowMist;                   // m(h), la bruma baja de la mañana (0 a 0,55)
float  _RmGolden;                    // g, el dorado (0 a 1)
float4 _RmDiskDir;                   // la dirección del astro que se ve (unitaria, mundo)
float4 _RmDiskParams;                // (visibilidad; es luna 0/1; iluminación; ángulo de fase φ en rad)
float4 _RmDiskShape;                 // (radio del disco en rad; radio del halo en rad; espejo del sur ±1; intensidad HDR, 1 en Bajo)
float4 _RmLightDir;                  // (dirección hacia la luz principal, unitaria; intensidad de la luz I de CelestialPlacement)
float  _RmHorizonY;                  // la ventana del contrato: horizonte y focal en dp
float  _RmFocalDp;
float4 _RmViewDp;                    // (ancho; alto; −; −) en dp
float  _RmDensity;                   // la densidad de la vegetación de la adaptación (0 a 1)
float4 _RmZonePhase;                 // (frac(d/1,3); frac(d/7); −; −): la fase de cinta de d para el ruido del terreno (Q-68)
float4 _RmSkyTopSrgb;                // el telón en sRGB (SetGlobalVector, sin convertir): se mezcla como Color.lerp
float4 _RmSkyHorizonSrgb;
float4 _RmHazeSrgb;                  // lerp(skyHorizon; fog; 0,5), la bruma del horizonte
float  _RmEmissiveGain;              // la ganancia de lo emisivo (1,6; 1 en la pasada de contrato)
// It. 8 (08-entornos-selector/02-tech-design §8.4; DT8-05 a DT8-07): los dos del escenario, en 0 fuera de la costa y sin puente.
float4 _RmSwell;                     // la ola de la costa: (amplitud A; escala s; φ₁; φ₂), las fases en [0; 2π) desde Core (CoastSwell)
float4 _RmUnderBridge;               // el puente más cercano: (zIni; zFin; rampa; fuerza) en cinta (BridgeShade); fuerza 0 sin puente

// ── El halo del legacy (sky_painter.dart:73-82) y el borde del disco (SkyArt) ──
static const float kRmHaloAlpha0 = 0.7;
static const float kRmHaloAlpha1 = 0.25;
static const float kRmHaloAlpha2 = 0;
static const float kRmDiskEdgeRad = 0.0020943951;
static const float kRmMoonHaloBase = 0.4;
static const float kRmMoonHaloPerIllumination = 0.6;

// Técnicas (no son arte): dónde se dibujan el cielo, las estrellas, las nubes y las cordilleras (dentro del plano lejano de 2 000 m),
// y la z de clip de un triángulo de pantalla completa (entre el plano cercano y el lejano en D3D, Vulkan, Metal y GL).
#define RM_SKY_DISTANCE 1000.0
#define RM_MID_CLIP_Z ((UNITY_NEAR_CLIP_VALUE + UNITY_RAW_FAR_CLIP_VALUE) * 0.5)

// SceneMath.Smooth y SceneMath.SmoothStep (vale también con a > b).
float RmSmooth(float t)
{
    return t * t * (3.0 - 2.0 * t);
}

float RmSmoothStep(float a, float b, float x)
{
    return RmSmooth(saturate((x - a) / (b - a)));
}

// SceneColor.ToLinear: la EOTF sRGB exacta (la del cielo, que tiene sondas de contrato). Para c ≥ 0 (los colores de la paleta y de
// vértice): la base de la potencia es ≥ 0,052.
float3 RmSrgbToLinear(float3 c)
{
    float3 lo = c * (1.0 / 12.92);
    float3 hi = pow((c + 0.055) * (1.0 / 1.055), 2.4);
    return lerp(hi, lo, step(c, 0.04045));
}

// La luma de Rec. 709 (técnica: los pesos de la luminancia de un color).
float RmLuma(float3 c)
{
    return dot(c, float3(0.2126, 0.7152, 0.0722));
}

// El alfa lineal que da sobre la escena lo mismo que el alfa a del legacy mezclado en sRGB (Color.lerp sobre el lienzo; WP-D2):
// con fondo y color en "gamma 2" (√ del lineal, ≈ sRGB), (fondo + a·(color − fondo))² − fondo² = α·(color² − fondo²), así
// α = a·(2·fondo + a·(color − fondo))/(color + fondo). Sobre un fondo oscuro es a² (una nube del 14 % de noche pesa el 2 %, como en el
// legacy, y no el 14 % que en lineal la volvía gris clara); con el color igual al fondo, a; con un color más oscuro (la sombra), más que a.
float RmDisplayAlpha(float a, float background, float color)
{
    return saturate(a * (2.0 * background + a * (color - background)) / max(color + background, 1e-4));
}

// SkyBackdrop.Gradient (DV-66, DN6a-09): 1 − (1 − q)², 0 → skyHorizon, 1 → skyTop.
float RmBackdropGradient(float q)
{
    float r = 1.0 - saturate(q);
    return 1.0 - r * r;
}

// CelestialMask.HaloAlpha: 0,7 / 0,25 / 0 en 0, la mitad y el borde del radio, lineal entre ellos (la poligonal es convexa: el máximo
// de los dos tramos es la poligonal).
float RmHaloAlpha(float r)
{
    r = saturate(r);
    float first = kRmHaloAlpha0 + 2.0 * (kRmHaloAlpha1 - kRmHaloAlpha0) * r;
    float second = kRmHaloAlpha1 + 2.0 * (kRmHaloAlpha2 - kRmHaloAlpha1) * (r - 0.5);
    return max(first, second);
}

// El factor del halo: la visibilidad y, la luna, × (0,4 + 0,6·iluminación) (DV-57).
float RmHaloScale()
{
    return _RmDiskParams.x * lerp(1.0, kRmMoonHaloBase + kRmMoonHaloPerIllumination * _RmDiskParams.z, _RmDiskParams.y);
}

// CelestialMask.StarAttenuation (QA-G6aT-08, DT6a-17): 0 adentro del disco (también del lado oscuro de la luna), 1 − el alfa del halo
// adentro del halo, 1 afuera.
float RmStarAttenuation(float angle, float diskRad, float haloRad, float haloScale)
{
    float outside = RmSmoothStep(diskRad - kRmDiskEdgeRad, diskRad, angle);
    return outside * (1.0 - RmHaloAlpha(angle / haloRad) * haloScale);
}

// El ángulo entre dos direcciones unitarias para ángulos chicos (la cuerda, 2·sin(θ/2): el error a 20° es de 0,03°).
float RmAngle(float cosine)
{
    return sqrt(abs(2.0 - 2.0 * cosine));
}

// La base del plano tangente de una dirección del cielo: derecha (horizontal) y arriba.
void RmSkyBasis(float3 d, out float3 right, out float3 up)
{
    right = normalize(float3(d.z, 0.0, -d.x) + float3(1e-6, 0.0, 0.0));
    up = cross(d, right);
}

// Un ruido de valor 1D determinista y barato (un hash entero, sin seno), en [0; 1].
float RmHash(int i)
{
    uint h = (uint)i * 747796405u + 2891336453u;
    h = ((h >> ((h >> 28u) + 4u)) ^ h) * 277803737u;
    h = (h >> 22u) ^ h;
    return (float)(h & 0xFFFFu) * (1.0 / 65535.0);
}

float RmValueNoise1(float x)
{
    float i = floor(x);
    float f = x - i;
    return lerp(RmHash((int)i), RmHash((int)i + 1), RmSmooth(f));
}

// Un triángulo o un quad de pantalla completa armado con la UV0 de la malla (0 a 1): el clip, con la y que da vuelta URP al dibujar
// en una RT (_ProjectionParams.x), así uv.y = 1 es el borde de arriba del cuadro.
float4 RmFullScreenClip(float2 uv)
{
    float2 ndc = uv * 2.0 - 1.0;
    return float4(ndc.x, ndc.y * _ProjectionParams.x, RM_MID_CLIP_Z, 1.0);
}

// Un punto del cielo en la dirección d (puntos en el infinito: giran con la cámara y no se trasladan).
float3 RmSkyPoint(float3 d)
{
    return _WorldSpaceCameraPos + d * RM_SKY_DISTANCE;
}

#endif
