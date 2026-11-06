using System;
using RowMate.Core.Scene.Art;

namespace RowMate.Core.Scene
{
    // La única verdad de la altura del terreno (06a-escena-lago/02-tech-design §2.13; 04-design-notes §7.1; 00-deuda-diseno §6.2):
    // la usan la malla y la colocación. Pura, en double y continua: dos segmentos vecinos comparten exactamente el borde.
    // El corte, desde la orilla (|x| = 9 + 0,5·n(z/41), que serpentea solo hacia afuera): el fondo bajo el agua (de −0,55 a 0 en
    // 2 m), la playa de canto rodado (0 → 0,27 en 1 m), el talud de pasto y roca (0,27 → 2,50 en 6 m, con ± 0,25 m de ruido que
    // se anula en los dos extremos: siempre entre 0,27 y 2,50) y, más allá, la ladera boscosa a la izquierda (+0,12 por metro)
    // y la pradera alta a la derecha (+0,02 por metro).
    public static class TerrainProfile
    {
        private const long MeanderSalt = 0x4D45414E44L;   // por lado: sal ± 1
        private const long SlopeSalt = 0x534C4F5045L;

        // −1 = orilla izquierda, +1 = derecha.
        public static int SideOf(double x)
        {
            return x < 0 ? -1 : 1;
        }

        // |x| de la orilla en una z de mundo: 9 + 0,5·n(z/41), n ∈ [0; 1].
        public static double ShoreX(int side, double zWorld)
        {
            var n = SceneHash.Value1(MeanderSalt + side, zWorld / LakeArt.ShoreMeanderPeriod);
            return ShoreEnvironment.BankX + LakeArt.ShoreMeanderAmplitude * n;
        }

        public static double Height(EnvironmentId environment, double x, double zWorld)
        {
            if (environment != EnvironmentId.Lake)
            {
                throw new NotSupportedException("TerrainProfile: en la 6a solo existe el lago (AC-SCN-21-01).");
            }

            var side = SideOf(x);
            var ax = Math.Abs(x);
            var d = ax - ShoreX(side, zWorld);
            if (d <= -LakeArt.BottomWidth)
            {
                return LakeArt.BottomDepth;
            }

            if (d <= 0)
            {
                return LakeArt.BottomDepth * (-d / LakeArt.BottomWidth);
            }

            if (d <= LakeArt.BeachWidth)
            {
                return LakeArt.BeachTop * (d / LakeArt.BeachWidth);
            }

            var slopeEnd = LakeArt.BeachWidth + LakeArt.SlopeWidth;
            if (d <= slopeEnd)
            {
                var s = (d - LakeArt.BeachWidth) / LakeArt.SlopeWidth;
                var n = SceneHash.Value2(SlopeSalt, x / LakeArt.SlopeNoisePeriod, zWorld / LakeArt.SlopeNoisePeriod) * 2 - 1;
                return LakeArt.BeachTop + (LakeArt.SlopeTop - LakeArt.BeachTop) * s + LakeArt.SlopeNoise * n * Math.Sin(Math.PI * s);
            }

            var outer = d - slopeEnd;
            return LakeArt.SlopeTop + (side < 0 ? LakeArt.LeftRise : LakeArt.RightRise) * outer;
        }

        // La altura más baja bajo un círculo (el pie de un tronco): el centro y cuatro puntos del borde.
        public static double MinUnder(EnvironmentId environment, double x, double zWorld, double radius)
        {
            var h = Height(environment, x, zWorld);
            h = Math.Min(h, Height(environment, x - radius, zWorld));
            h = Math.Min(h, Height(environment, x + radius, zWorld));
            h = Math.Min(h, Height(environment, x, zWorld - radius));
            return Math.Min(h, Height(environment, x, zWorld + radius));
        }

        // La envolvente de lo agregado (DN6a-14): a la izquierda la copa a ≤ 2,2 + 0,20·|x| desde |x| = 18; a la derecha
        // ≤ 2,2 + 0,06·x desde x = 30; más adentro, solo lo bajo (≤ 1,2 m sobre el terreno). NaN = sin envolvente (lo bajo).
        public static double Envelope(double x)
        {
            var ax = Math.Abs(x);
            if (x < 0)
            {
                return ax >= LakeArt.LeftEnvelopeFrom ? LakeArt.LeftEnvelopeBase + LakeArt.LeftEnvelopeSlope * ax : double.NaN;
            }

            return ax >= LakeArt.RightEnvelopeFrom ? LakeArt.RightEnvelopeBase + LakeArt.RightEnvelopeSlope * ax : double.NaN;
        }

        // La copa lejana (vegetation.canopyFar, 04-design-notes §7.4): a la izquierda, la envolvente menos 1 a 3 m de ruido.
        public static double CanopyHeight(double x, double zWorld)
        {
            var env = Envelope(x);
            if (double.IsNaN(env) || x > 0)
            {
                return double.NaN;
            }

            var n = SceneHash.Value2(0x43414E4FL, x / 8, zWorld / 8);
            return env - (1 + 2 * n);
        }

        // La |x| donde el terreno llega a una altura (desde la orilla hacia afuera; bisección sobre el corte monótono cerca del agua).
        public static double AbsXAtHeight(EnvironmentId environment, int side, double zWorld, double height)
        {
            var lo = ShoreX(side, zWorld);
            var hi = lo + LakeArt.BeachWidth + LakeArt.SlopeWidth;
            for (var i = 0; i < 60; i++)
            {
                var mid = (lo + hi) / 2;
                if (Height(environment, side * mid, zWorld) < height)
                {
                    lo = mid;
                }
                else
                {
                    hi = mid;
                }
            }

            return (lo + hi) / 2;
        }
    }
}
