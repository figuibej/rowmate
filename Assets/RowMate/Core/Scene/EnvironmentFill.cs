using System;
using RowMate.Core.Scene.Art;

namespace RowMate.Core.Scene
{
    // El relleno del río, la costa y la regata (02-tech-design §1.1, §2.4; 00-arte-entornos §2.2, §3.1, §4.1, §4.3): manzanas de
    // ciudad (río, |x| 22–120), rocas al pie del acantilado y matas de la meseta o de la duna (costa), álamos (regata, |x| 30–40)
    // y el público de las tribunas. Determinista por distancia con sus propias semillas (ShoreFill.SeedFor con sales 11 a 15):
    // NUNCA consume el generador del legacy; cada segmento se rellena solo, en cualquier orden. En |x| < 18 solo lo bajo
    // (≤ 1,2 m) salvo el público, que es parte de la tribuna del legacy (DV-84, DT8-12).
    public static class EnvironmentFill
    {
        public const int SegmentCapacity = 200;
        public const int SaltCityBlocks = 11, SaltCoastRocks = 12, SaltCoastShrubs = 13, SaltPoplars = 14, SaltSpectators = 15;
        public const double LowMaxHeight = 1.2, LowZoneX = 18;
        public const double CityStreet = 6, CityAlley = 1.5;
        public const double RockMinHeight = 0.4, RockMaxHeight = 1.2, RockSink = 0.3;
        public const double ShrubMinHeight = 0.4;
        public const double PoplarMinHeight = 14, PoplarMaxHeight = 18;
        public const int SeatsPerRow = 12;
        public const double SpectatorMinPeriod = 1.5, SpectatorMaxPeriod = 4;

        // El relleno del segmento k del escenario (el lago tiene el suyo en ShoreFill). legacy = los objetos del legacy del segmento
        // (las tribunas, para el público).
        public static void Segment(EnvironmentId id, long k, ReadOnlySpan<ShoreProp> legacy, DartRandom rnd, FillBuffer into)
        {
            if (rnd == null || into == null)
            {
                throw new ArgumentNullException(rnd == null ? nameof(rnd) : nameof(into));
            }

            if (k < 0)
            {
                return;
            }

            switch (id)
            {
                case EnvironmentId.River:
                    CityBlocks(k, rnd, into);
                    break;
                case EnvironmentId.Coast:
                    CoastRocks(k, rnd, into);
                    CoastShrubs(k, rnd, into);
                    break;
                case EnvironmentId.Regatta:
                    Poplars(k, rnd, into);
                    Spectators(legacy, into);
                    break;
            }
        }

        // Manzanas: por orilla, filas desde |x| 22 hacia afuera (fondo 15–30 m, calle de 6 m entre filas) hasta 120; en cada fila,
        // bloques de 12–30 m de frente con pasajes de 1,5 m, adentro del segmento; 3 a 9 pisos de 3 m. Variante = tono (0–2).
        public static void CityBlocks(long k, DartRandom rnd, FillBuffer into)
        {
            var z0 = k * ShoreEnvironment.SegmentLength;
            for (var s = 0; s < 2; s++)
            {
                var side = s == 0 ? -1 : 1;
                rnd.Reseed(ShoreFill.SeedFor(EnvironmentId.River, k, SaltCityBlocks) + s);
                var inner = RiverArt.FillFromX;
                while (inner < RiverArt.CityEndX)
                {
                    var depth = RiverArt.BlockDepthMin + rnd.NextDouble() * (RiverArt.BlockDepthMax - RiverArt.BlockDepthMin);
                    var outer = Math.Min(inner + depth, RiverArt.CityEndX);
                    var z = z0 + rnd.NextDouble() * CityAlley;
                    while (z0 + ShoreEnvironment.SegmentLength - z >= RiverArt.BlockFrontMin)
                    {
                        var front = RiverArt.BlockFrontMin + rnd.NextDouble() * (RiverArt.BlockFrontMax - RiverArt.BlockFrontMin);
                        front = Math.Min(front, z0 + ShoreEnvironment.SegmentLength - CityAlley - z);
                        var floors = RiverArt.BlockFloorsMin + rnd.NextInt(RiverArt.BlockFloorsMax - RiverArt.BlockFloorsMin + 1);
                        var tone = rnd.NextInt(3);
                        var cx = side * (inner + outer) / 2;
                        into.TryAdd(new FillItem(FillKind.CityBlock, cx, z + front / 2, RiverArt.QuayHeight, floors * RiverArt.FloorHeight, tone,
                            rnd.NextDouble(), 0, outer - inner, front));
                        z += front + CityAlley;
                    }

                    inner = outer + CityStreet;
                }
            }
        }

        // Rocas al pie del acantilado (|x| 12,5–14), 1 cada ≈ 3,5 m, de 0,4 a 1,2 m, hundidas el 30 %; no en las playas.
        public static void CoastRocks(long k, DartRandom rnd, FillBuffer into)
        {
            if (TerrainProfile.IsBeach(k))
            {
                return;
            }

            var z0 = k * ShoreEnvironment.SegmentLength;
            rnd.Reseed(ShoreFill.SeedFor(EnvironmentId.Coast, k, SaltCoastRocks));
            var z = z0 + rnd.NextDouble() * CoastArt.RockSpacing;
            while (z < z0 + ShoreEnvironment.SegmentLength)
            {
                var ax = -CoastArt.RockBaseX + rnd.NextDouble() * (-CoastEnvironment.CliffX + CoastArt.RockBaseX);
                var height = RockMinHeight + rnd.NextDouble() * (RockMaxHeight - RockMinHeight);
                var variant = rnd.NextInt(5);
                var x = -ax;
                var ground = TerrainProfile.Height(EnvironmentId.Coast, x, z);
                into.TryAdd(new FillItem(FillKind.CoastRock, x, z, ground - RockSink * height, height, variant, 0, 0));
                z += CoastArt.RockSpacing * (0.7 + 0.6 * rnd.NextDouble());
            }
        }

        // Matas bajas (≤ 1,2 m) cada ≈ 6 m: en la meseta (|x| 22–40) o, en las playas, sobre la duna (|x| 21–29).
        public static void CoastShrubs(long k, DartRandom rnd, FillBuffer into)
        {
            var z0 = k * ShoreEnvironment.SegmentLength;
            var beach = TerrainProfile.IsBeach(k);
            var from = beach ? -CoastArt.BeachToX + 1 : -CoastEnvironment.CliffX + CoastArt.CliffDepth + 1;
            var to = beach ? -CoastArt.DuneToX - 1 : 40;
            rnd.Reseed(ShoreFill.SeedFor(EnvironmentId.Coast, k, SaltCoastShrubs));
            for (var row = from; row < to; row += CoastArt.ShrubSpacing)
            {
                var z = z0 + rnd.NextDouble() * CoastArt.ShrubSpacing;
                while (z < z0 + ShoreEnvironment.SegmentLength)
                {
                    var x = -(row + rnd.NextDouble() * CoastArt.ShrubSpacing * 0.8);
                    var height = ShrubMinHeight + rnd.NextDouble() * (CoastArt.ShrubMaxHeight - ShrubMinHeight);
                    var variant = rnd.NextInt(1000);
                    var ground = TerrainProfile.Height(EnvironmentId.Coast, x, z);
                    into.TryAdd(new FillItem(FillKind.CoastShrub, x, z, ground, height, variant, 0, rnd.NextDouble() * 2 - 1));
                    z += CoastArt.ShrubSpacing * (0.8 + 0.4 * rnd.NextDouble());
                }
            }
        }

        // Álamos: por orilla, uno cada 12 m en z (± 1 m) con |x| 30–40, de 14 a 18 m, sobre el césped (1,0).
        public static void Poplars(long k, DartRandom rnd, FillBuffer into)
        {
            var z0 = k * ShoreEnvironment.SegmentLength;
            for (var s = 0; s < 2; s++)
            {
                var side = s == 0 ? -1 : 1;
                rnd.Reseed(ShoreFill.SeedFor(EnvironmentId.Regatta, k, SaltPoplars) + s);
                // Los de z = 12·j + 6 que caen en el segmento (la grilla es global: no se pisan entre segmentos).
                var first = (long)Math.Ceiling((z0 - RegattaArt.PoplarStep / 2) / RegattaArt.PoplarStep);
                for (var j = first; ; j++)
                {
                    var zc = j * RegattaArt.PoplarStep + RegattaArt.PoplarStep / 2;
                    if (zc >= z0 + ShoreEnvironment.SegmentLength)
                    {
                        break;
                    }

                    var z = zc + (rnd.NextDouble() * 2 - 1);
                    var x = side * (RegattaArt.PoplarFromX + rnd.NextDouble() * (RegattaArt.PoplarToX - RegattaArt.PoplarFromX));
                    var height = PoplarMinHeight + rnd.NextDouble() * (PoplarMaxHeight - PoplarMinHeight);
                    into.TryAdd(new FillItem(FillKind.Poplar, x, z, RegattaArt.BankHeight, height, rnd.NextInt(1000), 0, rnd.NextDouble() * 2 - 1));
                }
            }
        }

        // El público de cada tribuna (DT8-03): 4 gradas × 12 asientos de 1 m, ocupados al 70 % por la semilla del asiento. La
        // tribuna crece desde el ancla (borde del agua, x 12) hacia afuera: grada i de x 12 + 1,5·i, con el asiento a
        // 1,0 + 1,2·(i + 1). Variante = color de ropa (0–5)·10 + ropa del atlas (0–2); Rank = período (1,5–4 s); Tint = fase (0–1).
        public static void Spectators(ReadOnlySpan<ShoreProp> legacy, FillBuffer into)
        {
            for (var p = 0; p < legacy.Length; p++)
            {
                var stand = legacy[p];
                if (stand.Kind != PropKind.Grandstand)
                {
                    continue;
                }

                var side = stand.X < 0 ? -1 : 1;
                var seed = (long)stand.Z;
                for (var row = 0; row < RegattaArt.Steps; row++)
                {
                    for (var seat = 0; seat < SeatsPerRow; seat++)
                    {
                        var key = row * SeatsPerRow + seat;
                        if (SceneHash.Unit(SaltSpectators, seed, key) >= RegattaArt.CrowdOccupancy)
                        {
                            continue;
                        }

                        var x = stand.X + side * (1.5 * row + 0.75);
                        var z = stand.Z - RegattaArt.StandLength / 2 + 0.5 + seat;
                        var y = RegattaArt.BankHeight + RegattaArt.StepHeight * (row + 1);
                        var color = (int)(SceneHash.Unit(SaltSpectators + 1, seed, key) * RegattaArt.CrowdClothes.Length);
                        var clothes = (int)(SceneHash.Unit(SaltSpectators + 2, seed, key) * 3);
                        var period = SpectatorMinPeriod + SceneHash.Unit(SaltSpectators + 3, seed, key) * (SpectatorMaxPeriod - SpectatorMinPeriod);
                        var phase = SceneHash.Unit(SaltSpectators + 4, seed, key);
                        into.TryAdd(new FillItem(FillKind.Spectator, x, z, y, RegattaArt.CrowdHeight, color * 10 + clothes, period, phase));
                    }
                }
            }
        }
    }
}
