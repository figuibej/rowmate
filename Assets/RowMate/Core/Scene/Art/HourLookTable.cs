using System;

namespace RowMate.Core.Scene.Art
{
    // Un keyframe de la luz y el post de la hora (04-design-notes §10.2; ART_BIBLE §6.3 con 00-deuda-diseno §4.6 y el gate).
    // Sombras: fuerza en el nivel Medio y en el Alto (de noche, 0 en Medio sin apagar el mapa: DT6a-19). Exposición en EV;
    // balance de blancos (temperatura, tinte), saturación y contraste en las unidades de URP (−100 a 100); bloom (intensidad,
    // umbral).
    public readonly struct HourLookKey
    {
        public HourLookKey(double hour, double shadowMedium, double shadowHigh, double exposureEv, double whiteBalanceTemp,
                           double whiteBalanceTint, double saturation, double contrast, double bloomIntensity, double bloomThreshold)
        {
            Hour = hour;
            ShadowMedium = shadowMedium;
            ShadowHigh = shadowHigh;
            ExposureEv = exposureEv;
            WhiteBalanceTemp = whiteBalanceTemp;
            WhiteBalanceTint = whiteBalanceTint;
            Saturation = saturation;
            Contrast = contrast;
            BloomIntensity = bloomIntensity;
            BloomThreshold = bloomThreshold;
        }

        public double Hour { get; }
        public double ShadowMedium { get; }
        public double ShadowHigh { get; }
        public double ExposureEv { get; }
        public double WhiteBalanceTemp { get; }
        public double WhiteBalanceTint { get; }
        public double Saturation { get; }
        public double Contrast { get; }
        public double BloomIntensity { get; }
        public double BloomThreshold { get; }
    }

    // La tabla de la hora (P-17; lineal entre keyframes, con el mismo cierre que la paleta: 22 → 28 (= 4 h, noche) → 29 (= 5 h)).
    // La intensidad de la luz no va acá: sale del astro (CelestialPlacement). La crea WP-A; desde WP-D1 es del Designer (§7.6).
    public sealed class HourLookTable
    {
        private readonly HourLookKey[] _keys;

        public HourLookTable(HourLookKey[] keys, double veilEv, double veilSaturationLoss)
        {
            if (keys == null || keys.Length < 2)
            {
                throw new ArgumentException("HourLookTable: al menos dos keyframes.", nameof(keys));
            }

            for (var i = 1; i < keys.Length; i++)
            {
                if (!(keys[i].Hour > keys[i - 1].Hour))
                {
                    throw new ArgumentException("HourLookTable: las horas tienen que crecer.", nameof(keys));
                }
            }

            _keys = (HourLookKey[])keys.Clone();
            VeilEv = veilEv;
            VeilSaturationLoss = veilSaturationLoss;
        }

        // WP-D2 (con las tomas it6a): sin el tonemapping Neutral (ver NeutralTonemapping) la exposición vuelve a 0 a toda hora (−0,10 a
        // las 5): la paleta del legacy sale tal cual. De noche +0,60 subía la ventana a (1; 1; ·) antes del ajuste de color (el post de
        // URP gradúa en LDR: recorta en 1 y después aplica el balance), y el balance frío la volvía verde amarilla (matiz 114°). El
        // contraste baja a la mitad (con la paleta sin comprimir, +8 recortaba el horizonte de las 12 h) y el bloom de las 12 h sube a
        // 1,30 con umbral 1,4 (el sol mide ≥ 30 dp con luminancia > 0,9 en TV y el casco blanco no florece).
        public static HourLookTable Default { get; } = new HourLookTable(new[]
        {
            new HourLookKey(5, 0.55, 0.55, -0.10, 15, 5, 0, 0, 0.70, 1.0),
            new HourLookKey(7, 0.75, 0.75, 0, 8, 2, 2, 2, 0.45, 1.1),
            new HourLookKey(12, 0.85, 0.85, 0, 0, 0, 4, 4, 1.30, 1.4),
            new HourLookKey(18, 0.65, 0.65, 0, 15, 4, 6, 2, 0.75, 1.0),
            new HourLookKey(20, 0, 0.35, 0, -5, -1, -5, 0, 0.85, 0.9),
            new HourLookKey(22, 0, 0.25, 0, -10, -2, -10, -5, 0.95, 0.8),
            new HourLookKey(28, 0, 0.25, 0, -10, -2, -10, -5, 0.95, 0.8),
            new HourLookKey(29, 0.55, 0.55, -0.10, 15, 5, 0, 0, 0.70, 1.0),
        }, -0.17, 0.30);

        // El velo de pausa (DN6a-19, DT6a-10): exposición + w·VeilEv y saturación × (1 − VeilSaturationLoss·w).
        public double VeilEv { get; }

        public double VeilSaturationLoss { get; }

        // Viñeta (a toda hora) y la luz ambiente de URP para lo de terceros (04-design-notes §10.1-§10.2).
        public const double VignetteIntensity = 0.18, VignetteSmoothness = 0.4;
        public const double AmbientScale = 0.80, KeyLightScale = 0.45, NoonIntensity = 1.75;

        // Cuánto del matiz del cielo entra en el ambiente (WP-D2): Hemi usa lerp(blanco; c/luminancia(c); AmbientHue). Con el matiz entero
        // (1) un cielo saturado multiplicaba el azul por 2,4 y el rojo por 0,16: la cabaña salía azul marino y los pinos verde petróleo
        // (CAP-02 contra LEG-02). Con 0,2 el ambiente sigue frío a la sombra y la madera y el pasto conservan su color.
        public const double AmbientHue = 0.2;

        // El tonemapping del Volume de la escena (WP-D2): sin curva (None). La paleta del legacy ya es un color de pantalla: Neutral la
        // comprimía (el horizonte de las 12 h #8FD3FF salía #90BED4 y las nubes grises) y lo oscuro lo compensaba la exposición de la
        // noche. Lo HDR (el disco, la ventana, el brillo del casco) se recorta en 1 con el bloom alrededor; el panel de la UI escribe en
        // una RT de 8 bits, así que el vidrio nunca recibe más que blanco.
        public const bool NeutralTonemapping = false;

        // La niebla lineal de Unity para lo de terceros (a 400 m da el 80 % del legacy) y la del contrato (80-400 m, 80 %; la
        // extendida hasta 1 en 1 200 m).
        public const double ThirdPartyFogStart = 80, ThirdPartyFogEnd = 480;

        // La bruma baja m(h) (AC-SCN-15-07; 04-design-notes §3.2): 0 hasta las 5; 0,55·S(5; 6); 0,55 de 6 a 7; 0,55·(1 − S(7; 9,5)).
        public const double MistPeak = 0.55, MistRiseStart = 5, MistRiseEnd = 6, MistFallStart = 7, MistFallEnd = 9.5;

        public int Count => _keys.Length;

        public HourLookKey this[int index] => _keys[index];

        // Lineal entre los dos keyframes que encierran h (h mod 24; si h < la primera hora, + 24).
        public HourLookKey Evaluate(double hour)
        {
            var h = hour % 24;
            if (h < 0)
            {
                h += 24;
            }

            if (h < _keys[0].Hour)
            {
                h += 24;
            }

            for (var i = 0; i < _keys.Length - 1; i++)
            {
                var a = _keys[i];
                var b = _keys[i + 1];
                if (h >= a.Hour && h <= b.Hour)
                {
                    var t = (h - a.Hour) / (b.Hour - a.Hour);
                    return new HourLookKey(h,
                        a.ShadowMedium + (b.ShadowMedium - a.ShadowMedium) * t,
                        a.ShadowHigh + (b.ShadowHigh - a.ShadowHigh) * t,
                        a.ExposureEv + (b.ExposureEv - a.ExposureEv) * t,
                        a.WhiteBalanceTemp + (b.WhiteBalanceTemp - a.WhiteBalanceTemp) * t,
                        a.WhiteBalanceTint + (b.WhiteBalanceTint - a.WhiteBalanceTint) * t,
                        a.Saturation + (b.Saturation - a.Saturation) * t,
                        a.Contrast + (b.Contrast - a.Contrast) * t,
                        a.BloomIntensity + (b.BloomIntensity - a.BloomIntensity) * t,
                        a.BloomThreshold + (b.BloomThreshold - a.BloomThreshold) * t);
                }
            }

            return _keys[_keys.Length - 1];
        }
    }
}
