using System;
using RowMate.Core.Scene.Art;

namespace RowMate.Core.Scene
{
    public enum FillKind
    {
        Reed,
        Rock,
        ForestPine,
        MeadowConifer,
        MeadowBush,
        Grass,
        Fern,
    }

    // Una pieza del relleno (lo agregado al legacy): x y z de mundo, la y del pie, el alto, una variante, un rango ∈ [0; 1)
    // para el raleo por distancia (pasto y helechos) y un tinte ∈ [−1; 1].
    public readonly struct FillItem
    {
        public FillItem(FillKind kind, double x, double z, double baseY, double height, int variant, double rank, double tint)
        {
            Kind = kind;
            X = x;
            Z = z;
            BaseY = baseY;
            Height = height;
            Variant = variant;
            Rank = rank;
            Tint = tint;
        }

        public FillKind Kind { get; }
        public double X { get; }
        public double Z { get; }
        public double BaseY { get; }
        public double Height { get; }
        public int Variant { get; }
        public double Rank { get; }
        public double Tint { get; }

        public double TopY => BaseY + Height;
    }

    public sealed class FillBuffer
    {
        private readonly FillItem[] _items;

        public FillBuffer(int capacity)
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }

            _items = new FillItem[capacity];
        }

        public int Count { get; private set; }

        public int Capacity => _items.Length;

        public ReadOnlySpan<FillItem> Items => new ReadOnlySpan<FillItem>(_items, 0, Count);

        public void Clear()
        {
            Count = 0;
        }

        // false si no entra (el que llama decide; los rellenos por segmento caben siempre en SegmentCapacity).
        public bool TryAdd(in FillItem item)
        {
            if (Count >= _items.Length)
            {
                return false;
            }

            _items[Count++] = item;
            return true;
        }
    }

    // Lo que se suma al legacy en la orilla (06a-escena-lago/02-tech-design §2.13; 04-design-notes §7.4; DD6a-11): juncos, rocas,
    // bosque cercano, matas de la pradera, pasto y helechos. Determinista por distancia y con sus propias semillas
    // (SeedFor(id; k) + sal·104 729, sal 1 a 6): NUNCA consume el generador del legacy, así que el paisaje del legacy es el
    // mismo con y sin relleno. Cada segmento se rellena solo (QA-G6aT-15 b): el disco de Poisson del bosque se muestrea en
    // [z₀ + 3; z₀ + 47] sin leer al vecino, así el resultado no depende del orden de los pedidos. En |x| < 18, solo lo bajo
    // (≤ 1,2 m, P-09); más afuera, nada pasa la envolvente.
    public static class ShoreFill
    {
        // Juncos (≤ 2·50/0,8), rocas (≤ 2·50/5), bosque (≤ 90 candidatos) y matas (≤ 9 por grupo, ≤ 2 grupos).
        public const int SegmentCapacity = 400;

        public static long SeedFor(EnvironmentId id, long k, int salt)
        {
            return unchecked(ShoreEnvironment.SeedFor(id, k) + salt * LakeArt.FillSaltPrime);
        }

        // El relleno del segmento k (juncos, rocas, bosque cercano y matas de la pradera). legacy = los objetos del legacy de
        // ese segmento (para dejar libres el muelle y el frente de la cabaña); rnd = un generador propio que se reutiliza.
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

            Reeds(id, k, legacy, rnd, into);
            Rocks(id, k, rnd, into);
            Forest(id, k, rnd, into);
            Meadow(id, k, rnd, into);
        }

        // Juncos: |x| de 8,6 a 9,6 con la raíz en y ≤ 0,05; 1 cada 0,8 m con huecos de 2 a 6 m cada 10 a 25 m; nunca a menos de
        // 3 m de un muelle ni frente a una cabaña (± 4 m en z); de 0,6 a 1,2 m (DT6a-09).
        public static void Reeds(EnvironmentId id, long k, ReadOnlySpan<ShoreProp> legacy, DartRandom rnd, FillBuffer into)
        {
            var z0 = k * ShoreEnvironment.SegmentLength;
            for (var s = 0; s < 2; s++)
            {
                var side = s == 0 ? -1 : 1;
                rnd.Reseed(SeedFor(id, k, LakeArt.SaltReeds) + s);
                var z = z0 + rnd.NextDouble() * LakeArt.ReedSpacing;
                var nextGap = z0 + LakeArt.ReedGapEveryMin + rnd.NextDouble() * (LakeArt.ReedGapEveryMax - LakeArt.ReedGapEveryMin);
                while (z < z0 + ShoreEnvironment.SegmentLength)
                {
                    if (z >= nextGap)
                    {
                        z += LakeArt.ReedGapMin + rnd.NextDouble() * (LakeArt.ReedGapMax - LakeArt.ReedGapMin);
                        nextGap = z + LakeArt.ReedGapEveryMin + rnd.NextDouble() * (LakeArt.ReedGapEveryMax - LakeArt.ReedGapEveryMin);
                        continue;
                    }

                    var u = rnd.NextDouble();
                    var height = LakeArt.ReedMinHeight + rnd.NextDouble() * (LakeArt.ReedMaxHeight - LakeArt.ReedMinHeight);
                    var variant = rnd.NextInt(1000);
                    var shore = TerrainProfile.ShoreX(side, z);
                    var ax = SceneMath.Clamp(shore - 0.4 + u * 0.55, LakeArt.ReedInnerX, LakeArt.ReedOuterX);
                    var root = TerrainProfile.Height(id, side * ax, z);
                    if (root > LakeArt.ReedRootMaxY)
                    {
                        ax = Math.Max(LakeArt.ReedInnerX, shore + 0.1);
                        root = TerrainProfile.Height(id, side * ax, z);
                    }

                    if (root <= LakeArt.ReedRootMaxY && Clear(legacy, side, z))
                    {
                        into.TryAdd(new FillItem(FillKind.Reed, side * ax, z, root, height, variant, 0, 0));
                    }

                    z += LakeArt.ReedSpacing;
                }
            }
        }

        // Rocas: en la playa y al pie del talud, 1 cada ≈ 7 m por orilla, de 0,2 a 0,7 m, hundidas el 30 % de su alto.
        public static void Rocks(EnvironmentId id, long k, DartRandom rnd, FillBuffer into)
        {
            var z0 = k * ShoreEnvironment.SegmentLength;
            for (var s = 0; s < 2; s++)
            {
                var side = s == 0 ? -1 : 1;
                rnd.Reseed(SeedFor(id, k, LakeArt.SaltRocks) + s);
                var z = z0 + rnd.NextDouble() * LakeArt.RockSpacing;
                while (z < z0 + ShoreEnvironment.SegmentLength)
                {
                    var ax = TerrainProfile.ShoreX(side, z) + rnd.NextDouble() * LakeArt.RockBeachSpan;
                    var height = LakeArt.RockMinHeight + rnd.NextDouble() * (LakeArt.RockMaxHeight - LakeArt.RockMinHeight);
                    var variant = rnd.NextInt(LakeArt.RockVariants);
                    var ground = TerrainProfile.MinUnder(id, side * ax, z, height * 0.6);
                    into.TryAdd(new FillItem(FillKind.Rock, side * ax, z, ground - LakeArt.RockSink * height, height, variant, 0, 0));
                    z += LakeArt.RockSpacing * (0.7 + 0.6 * rnd.NextDouble());
                }
            }
        }

        // El bosque cercano: a la izquierda, |x| de 18 a 45, Poisson de 6 m en [z₀ + 3; z₀ + 47]; alto
        // mín(envolvente − pie; 6 + 0,12·(|x| − 18)). Lo que no llega a 1,5 m no se planta.
        public static void Forest(EnvironmentId id, long k, DartRandom rnd, FillBuffer into)
        {
            var z0 = k * ShoreEnvironment.SegmentLength;
            rnd.Reseed(SeedFor(id, k, LakeArt.SaltForest));
            var start = into.Count;
            var span = ShoreEnvironment.SegmentLength - 2 * LakeArt.ForestMargin;
            for (var c = 0; c < LakeArt.ForestCandidates; c++)
            {
                var ax = LakeArt.ForestInnerX + rnd.NextDouble() * (LakeArt.ForestOuterX - LakeArt.ForestInnerX);
                var z = z0 + LakeArt.ForestMargin + rnd.NextDouble() * span;
                var variant = rnd.NextInt(1000);
                var x = -ax;
                if (!FarFromAccepted(into, start, x, z, LakeArt.ForestPoissonRadius))
                {
                    continue;
                }

                var foot = TerrainProfile.MinUnder(id, x, z, 0.2) - LakeArt.PineSink;
                var height = Math.Min(TerrainProfile.Envelope(x) - foot, LakeArt.ForestBaseHeight + LakeArt.ForestHeightSlope * (ax - LakeArt.ForestInnerX));
                if (height < LakeArt.ForestMinHeight)
                {
                    continue;
                }

                into.TryAdd(new FillItem(FillKind.ForestPine, x, z, foot, height, variant, 0, 0));
            }
        }

        // Las matas de la pradera (derecha, x de 30 a 120): un grupo cada 90 a 150 m en z (centros en j·120 ± 15, por su propia
        // semilla), de 3 a 9 coníferas chicas (el pino a escala 0,4-0,7) y arbustos (1 a 1,5 m), siempre bajo la envolvente. El
        // grupo es del segmento de su centro.
        public static void Meadow(EnvironmentId id, long k, DartRandom rnd, FillBuffer into)
        {
            var z0 = k * ShoreEnvironment.SegmentLength;
            var z1 = z0 + ShoreEnvironment.SegmentLength;
            var half = LakeArt.MeadowJitter / 2;
            var jFrom = (long)Math.Floor((z0 - half) / LakeArt.MeadowSpacing) - 1;
            var jTo = (long)Math.Floor((z1 + half) / LakeArt.MeadowSpacing) + 1;
            for (var j = Math.Max(0, jFrom); j <= jTo; j++)
            {
                rnd.Reseed(SeedFor(id, j, LakeArt.SaltMeadow));
                var zc = j * LakeArt.MeadowSpacing + (rnd.NextDouble() - 0.5) * LakeArt.MeadowJitter;
                if (zc < z0 || zc >= z1)
                {
                    continue;
                }

                var xc = LakeArt.MeadowInnerX + LakeArt.MeadowSpread
                    + rnd.NextDouble() * (LakeArt.MeadowOuterX - LakeArt.MeadowInnerX - 2 * LakeArt.MeadowSpread);
                var items = LakeArt.MeadowMinItems + rnd.NextInt(LakeArt.MeadowMaxItems - LakeArt.MeadowMinItems + 1);
                for (var i = 0; i < items; i++)
                {
                    var x = Math.Max(LakeArt.MeadowInnerX, xc + (rnd.NextDouble() * 2 - 1) * LakeArt.MeadowSpread);
                    var z = zc + (rnd.NextDouble() * 2 - 1) * LakeArt.MeadowSpread;
                    var conifer = rnd.NextBool();
                    var size = rnd.NextDouble();
                    var variant = rnd.NextInt(1000);
                    var desired = conifer
                        ? LakeArt.PineHeight * (LakeArt.MeadowConiferMinScale + size * (LakeArt.MeadowConiferMaxScale - LakeArt.MeadowConiferMinScale))
                        : LakeArt.MeadowBushMinHeight + size * (LakeArt.MeadowBushMaxHeight - LakeArt.MeadowBushMinHeight);
                    var foot = TerrainProfile.MinUnder(id, x, z, 0.3) - LakeArt.PineSink;
                    var height = Math.Min(desired, TerrainProfile.Envelope(x) - foot);
                    if (height < 0.5)
                    {
                        continue;
                    }

                    into.TryAdd(new FillItem(conifer ? FillKind.MeadowConifer : FillKind.MeadowBush, x, z, foot, height, variant, 0, 0));
                }
            }
        }

        // El pasto de una franja de z de mundo (talud |x| 10-16 y la pradera hasta x = 60): una mata por celda de 0,5 m
        // (4 por m²), con su lugar, alto (0,2-0,6 m), tinte (± 8 %) y rango por hash de la celda: no depende del orden ni de
        // nada más. El raleo por distancia (4/m² junto a la cámara, a 0 en el fin) lo hace KeepGrass con el rango.
        public static int Grass(EnvironmentId id, double zMin, double zMax, FillBuffer into)
        {
            var added = 0;
            var cell = LakeArt.GrassCell;
            var izFrom = (long)Math.Floor(zMin / cell);
            var izTo = (long)Math.Floor(zMax / cell);
            for (var iz = izFrom; iz <= izTo; iz++)
            {
                for (var s = 0; s < 2; s++)
                {
                    var side = s == 0 ? -1 : 1;
                    var outer = side < 0 ? LakeArt.GrassSlopeOuterX : LakeArt.GrassMeadowOuterX;
                    var ixFrom = (long)Math.Floor(LakeArt.GrassInnerX / cell);
                    var ixTo = (long)Math.Floor(outer / cell);
                    for (var ix = ixFrom; ix < ixTo; ix++)
                    {
                        var salt = SeedFor(id, 0, LakeArt.SaltGrass) + s;
                        var h = SceneHash.Combine(salt, ix, iz);
                        var ax = (ix + SceneHash.Unit(h)) * cell;
                        var z = (iz + SceneHash.Unit(SceneHash.Mix(h ^ 1))) * cell;
                        if (z < zMin || z >= zMax)
                        {
                            continue;
                        }

                        var shore = TerrainProfile.ShoreX(side, z);
                        if (ax < shore + LakeArt.BeachWidth)
                        {
                            continue;
                        }

                        var height = LakeArt.GrassMinHeight + SceneHash.Unit(SceneHash.Mix(h ^ 2)) * (LakeArt.GrassMaxHeight - LakeArt.GrassMinHeight);
                        var rank = SceneHash.Unit(SceneHash.Mix(h ^ 3));
                        var tint = SceneHash.Unit(SceneHash.Mix(h ^ 4)) * 2 - 1;
                        var x = side * ax;
                        if (!into.TryAdd(new FillItem(FillKind.Grass, x, z, TerrainProfile.Height(id, x, z), height, 0, rank, tint)))
                        {
                            return added;
                        }

                        added++;
                    }
                }
            }

            return added;
        }

        // Los helechos: al pie del bosque (izquierda, |x| de 16 a 20), 1 por m², de 0,4 a 0,8 m.
        public static int Ferns(EnvironmentId id, double zMin, double zMax, FillBuffer into)
        {
            var added = 0;
            var cell = LakeArt.FernCell;
            var izFrom = (long)Math.Floor(zMin / cell);
            var izTo = (long)Math.Floor(zMax / cell);
            var ixFrom = (long)Math.Floor(LakeArt.FernInnerX / cell);
            var ixTo = (long)Math.Floor(LakeArt.FernOuterX / cell);
            var salt = SeedFor(id, 1, LakeArt.SaltGrass);
            for (var iz = izFrom; iz <= izTo; iz++)
            {
                for (var ix = ixFrom; ix < ixTo; ix++)
                {
                    var h = SceneHash.Combine(salt, ix, iz);
                    var ax = (ix + SceneHash.Unit(h)) * cell;
                    var z = (iz + SceneHash.Unit(SceneHash.Mix(h ^ 1))) * cell;
                    if (z < zMin || z >= zMax)
                    {
                        continue;
                    }

                    var height = LakeArt.FernMinHeight + SceneHash.Unit(SceneHash.Mix(h ^ 2)) * (LakeArt.FernMaxHeight - LakeArt.FernMinHeight);
                    var rank = SceneHash.Unit(SceneHash.Mix(h ^ 3));
                    var x = -ax;
                    if (!into.TryAdd(new FillItem(FillKind.Fern, x, z, TerrainProfile.Height(id, x, z), height, 0, rank, 0)))
                    {
                        return added;
                    }

                    added++;
                }
            }

            return added;
        }

        // ¿Se dibuja esta mata a relZ m de la cámara? 4/m² junto a la cámara que bajan linealmente a 0 en el fin del nivel.
        public static bool KeepGrass(double rank, double relZ, double end)
        {
            if (relZ >= end)
            {
                return false;
            }

            var density = relZ <= 0 ? 1 : 1 - relZ / end;
            return rank < density;
        }

        // Lejos del muelle (3 m más medio tablero) y del frente de la cabaña (± 4 m en z alrededor de su huella).
        private static bool Clear(ReadOnlySpan<ShoreProp> legacy, int side, double z)
        {
            for (var i = 0; i < legacy.Length; i++)
            {
                var p = legacy[i];
                if (p.Kind == PropKind.Pier && side < 0 && Math.Abs(z - p.Z) < LakeArt.ReedPierClearance + LakeArt.PierDeckWidth / 2)
                {
                    return false;
                }

                if (p.Kind == PropKind.Cabin && side > 0 && z > p.Z - LakeArt.ReedCabinClearance
                    && z < p.Z + LakeArt.CabinDepths[0] + LakeArt.ReedCabinClearance)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool FarFromAccepted(FillBuffer into, int start, double x, double z, double radius)
        {
            var items = into.Items;
            var r2 = radius * radius;
            for (var i = start; i < items.Length; i++)
            {
                var dx = items[i].X - x;
                var dz = items[i].Z - z;
                if (dx * dx + dz * dz < r2)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
