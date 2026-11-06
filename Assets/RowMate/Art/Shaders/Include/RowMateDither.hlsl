// RowMateDither.hlsl: los tramados de la escena con un Bayer 4 × 4 fijo en la pantalla (06a-escena-lago/04-design-notes §7.6;
// DV-62, DN6a-24, DN6a-29; WP-D1). Sin tiempo: dos tomas del mismo cuadro son iguales byte a byte.
//
// _RmFade (por instancia) lo arma quien instancia con MaterialArt (Core): la aparición de 400 a 360 m y el tramado de 2 a 1 m
// (InstanceFade) y el cambio de LOD complementario en el 10 % del límite (LodFadePair: el LOD cercano con w y el lejano con w − 1).
// Con fade ≥ 0 se ve el píxel cuyo umbral es < fade; con fade < 0, el que tiene el umbral ≥ 1 + fade: exactamente el resto.
#ifndef ROWMATE_DITHER_INCLUDED
#define ROWMATE_DITHER_INCLUDED

// (M[y][x] + 0,5)/16 con la matriz de Bayer {0, 8, 2, 10; 12, 4, 14, 6; 3, 11, 1, 9; 15, 7, 13, 5} (MaterialArt.Bayer4).
static const float kRmBayer4[16] =
{
    0.03125, 0.53125, 0.15625, 0.65625,
    0.78125, 0.28125, 0.90625, 0.40625,
    0.21875, 0.71875, 0.09375, 0.59375,
    0.96875, 0.46875, 0.84375, 0.34375
};

// El umbral del píxel (las coordenadas de SV_Position).
float RmBayer(float2 pixel)
{
    uint2 p = (uint2)pixel & 3u;
    return kRmBayer4[p.y * 4u + p.x];
}

// En el vértice: el fundido de la instancia como un par (a; b) para que el fragmento haga clip(umbral·a + b) (MaterialArt.DitherKeeps):
// con fade ≥ 0 se ve si umbral < fade (a = −1, b = fade); con fade < 0, si umbral ≥ 1 + fade (a = 1, b = −(1 + fade)).
float2 RmDitherFade(float fade)
{
    return fade >= 0.0 ? float2(-1.0, fade - 1e-6) : float2(1.0, -(1.0 + fade));
}

// En el fragmento: descarta el píxel según el par de RmDitherFade.
void RmDitherClip(float2 fade, float2 pixel)
{
    clip(RmBayer(pixel) * fade.x + fade.y);
}

#endif
