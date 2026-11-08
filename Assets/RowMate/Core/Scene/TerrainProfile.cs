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
            switch (environment)
            {
                case EnvironmentId.Lake:
                    return LakeHeight(x, zWorld);
                case EnvironmentId.River:
                    return RiverHeight(x);
                case EnvironmentId.Coast:
                    return CoastHeight(x, zWorld);
                case EnvironmentId.Regatta:
                    return RegattaHeight(x);
                default:
                    throw new ArgumentOutOfRangeException(nameof(environment), environment, "TerrainProfile: escenario desconocido.");
            }
        }

        // ── It. 8 (02-tech-design §2.4) ──

        // Río: el agua hasta el muro (|x| 9, plomo) con el fondo a −0,5 (lo que se ve del muro bajo el agua), y el malecón y la
        // ciudad planos a 1,20. Las rampas de los clubes cortan el muro y el malecón: RiverHeight(x, z, rampas).
        public static double RiverHeight(double x)
        {
            return Math.Abs(x) < RiverArt.WallX ? -RiverArt.WallUnderwater : RiverArt.QuayHeight;
        }

        // Con las rampas de los clubes (RiverPlacement.Ramps de los segmentos vecinos de z): en |x| 8,5–10 y su tramo de z, la
        // superficie de la rampa (de 1,20 en su arranque a −0,2 en su pie, lineal) reemplaza al muro y al malecón.
        public static double RiverHeight(double x, double zWorld, ReadOnlySpan<RampCut> ramps)
        {
            var h = RiverHeight(x);
            var ax = Math.Abs(x);
            var side = SideOf(x);
            for (var i = 0; i < ramps.Length; i++)
            {
                var r = ramps[i];
                if (r.Side == side && ax >= RiverArt.RampMinX && ax <= RiverArt.RampMaxX && zWorld >= r.ZStart && zWorld <= r.ZEnd)
                {
                    return r.SurfaceAt(zWorld);
                }
            }

            return h;
        }

        // Costa: a la derecha, el mar (fondo a −7). A la izquierda, por segmento: playa (k mod 5 = 2: arena desde −9,5 con 6° hasta
        // −20, duna hasta 2 m en −30 y el acantilado que vuelve detrás) o acantilado (pie de rocas de −12,5 a −14, el suelo bajo la
        // pared y, desde −21, la meseta al alto del acantilado de esa z; el bloque del faro a 2,5 m, DT8-10). Entre un tramo y
        // otro, 10 m de transición en z (lineal, continua).
        public static double CoastHeight(double x, double zWorld)
        {
            if (x >= 0)
            {
                return CoastArt.SeaFloor;
            }

            var ax = -x;
            var k = (long)Math.Floor(zWorld / ShoreEnvironment.SegmentLength);
            var z0 = k * ShoreEnvironment.SegmentLength;
            var here = CoastProfile(k, ax, zWorld);
            var half = CoastArt.BeachTransition / 2;
            if (zWorld - z0 < half && k > 0 && IsBeach(k) != IsBeach(k - 1))
            {
                var t = (zWorld - z0 + half) / CoastArt.BeachTransition;
                return CoastProfile(k - 1, ax, zWorld) + (here - CoastProfile(k - 1, ax, zWorld)) * t;
            }

            if (z0 + ShoreEnvironment.SegmentLength - zWorld < half && IsBeach(k) != IsBeach(k + 1))
            {
                var t = (zWorld - (z0 + ShoreEnvironment.SegmentLength - half)) / CoastArt.BeachTransition;
                return here + (CoastProfile(k + 1, ax, zWorld) - here) * t;
            }

            return here;
        }

        public static bool IsBeach(long k)
        {
            return k >= 0 && k % 5 == 2;
        }

        // El corte de la costa del segmento k en |x| (sin transición).
        private static double CoastProfile(long k, double ax, double zWorld)
        {
            if (IsBeach(k))
            {
                // El agua baja de 0 en −9,5 al fondo en 6 m; la arena con 6°; la duna; detrás, la pared vuelve (una cala).
                if (ax < -CoastArt.BeachFromX)
                {
                    var d = -CoastArt.BeachFromX - ax;
                    return Math.Max(CoastArt.SeaFloor, -d * (-CoastArt.SeaFloor / 6));
                }

                var sandTop = (-CoastArt.BeachToX + CoastArt.BeachFromX) * Math.Tan(SceneMath.DegToRad(CoastArt.BeachSlopeDeg));
                if (ax < -CoastArt.BeachToX)
                {
                    return (ax + CoastArt.BeachFromX) * Math.Tan(SceneMath.DegToRad(CoastArt.BeachSlopeDeg));
                }

                if (ax < -CoastArt.DuneToX)
                {
                    return sandTop + (CoastArt.DuneHeight - sandTop) * (ax + CoastArt.BeachToX) / (CoastArt.BeachToX - CoastArt.DuneToX);
                }

                return CoastArt.CliffHeight;
            }

            // Acantilado: el agua hasta −12,5 (el fondo baja en 6 m), el pie de rocas, el suelo bajo la pared y la meseta.
            var foot = -CoastArt.RockBaseX;
            if (ax < foot)
            {
                var d = foot - ax;
                return Math.Max(CoastArt.SeaFloor, CoastArt.CliffFootDepth - d * (-CoastArt.SeaFloor / 6));
            }

            var wall = -CoastEnvironment.CliffX;
            if (ax < wall)
            {
                return CoastArt.CliffFootDepth + (CoastArt.RockTop - CoastArt.CliffFootDepth) * (ax - foot) / (wall - foot);
            }

            if (ax < wall + CoastArt.CliffDepth)
            {
                return CoastArt.RockTop;
            }

            return CoastPlateau(k, zWorld);
        }

        // La meseta: el alto del acantilado que cubre esa z (8·escala; el de z₀ cubre [z₀; z₀ + 25), el de z₀ + 25 el resto),
        // con las escalas del generador del legacy recalculadas (sin tocar la caché de nadie); en el segmento del faro, el bloque
        // de z₀ + 25 a 2,5 m (DT8-10).
        // Para que no haya escalones (costura < 1 mm), el alto pasa de un bloque al siguiente en rampa a lo largo del solape de los
        // acantilados (3 m): en [z₀; z₀ + 3) desde el segundo bloque del segmento anterior y en [z₀ + 25; z₀ + 28) desde el primero.
        public static double CoastPlateau(long k, double zWorld)
        {
            var u = zWorld - ShoreEnvironment.SegmentLength * k;
            PlateauBlocks(k, out var first, out var second);
            if (u >= 25)
            {
                var t = Math.Min(1, (u - 25) / CoastArt.CliffOverlap);
                return first + (second - first) * t;
            }

            if (u < CoastArt.CliffOverlap)
            {
                var previous = CoastArt.CliffHeight;   // detrás de una playa, la pared que vuelve
                if (k > 0 && !IsBeach(k - 1))
                {
                    PlateauBlocks(k - 1, out _, out previous);
                }

                var t = Math.Max(0, u / CoastArt.CliffOverlap);
                return previous + (first - previous) * t;
            }

            return first;
        }

        // Los altos de los dos bloques del segmento k: 8·escala de cada acantilado, con las escalas del generador del legacy
        // recalculadas (sin tocar la caché de nadie); en el segmento del faro, el segundo a 2,5 m (DT8-10).
        private static void PlateauBlocks(long k, out double first, out double second)
        {
            var rnd = Scratch;
            rnd.Reseed(ShoreEnvironment.SeedFor(EnvironmentId.Coast, k));
            var scale0 = 0.8 + rnd.NextDouble() * 0.5;
            rnd.NextInt(1000);
            var scale1 = 0.8 + rnd.NextDouble() * 0.5;
            first = CoastArt.CliffHeight * scale0;
            second = k % 20 == 10 ? CoastArt.Promontory : CoastArt.CliffHeight * scale1;
        }

        // Regata: el canal con el fondo a −0,8, el talud de 25° que sube de 0 en |x| 9,8 a 1,0 en 11,95 (sigue bajo el agua hasta el
        // fondo) y el césped plano a 1,0 (DT8-13).
        public static double RegattaHeight(double x)
        {
            var ax = Math.Abs(x);
            var tan = Math.Tan(SceneMath.DegToRad(RegattaArt.BankSlopeDeg));
            var h = (ax - RegattaArt.BankFromX) * tan;
            return Math.Max(RegattaArt.ChannelBed, Math.Min(RegattaArt.BankHeight, h));
        }

        // El |x| donde termina el talud de la regata (1,0 m a 25°: 11,94).
        public static double RegattaBankTopX => RegattaArt.BankFromX + RegattaArt.BankHeight / Math.Tan(SceneMath.DegToRad(RegattaArt.BankSlopeDeg));

        [ThreadStatic]
        private static DartRandom? _scratch;

        private static DartRandom Scratch => _scratch ??= new DartRandom(0);

        private static double LakeHeight(double x, double zWorld)
        {
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
