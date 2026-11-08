using System;
using System.Diagnostics;
using RowMate.Core.Scene;
using RowMate.Core.Scene.Art;
using RowMate.Core.Scene.Geometry;
using UnityEngine;

namespace RowMate.Scene
{
    // Lo que la escena guarda de un segmento de 50 m de la orilla (06a-escena-lago/02-tech-design §2.11 a §2.13, §3.6): los objetos del
    // legacy con su colocación (el pie de cada pino, la cabaña con su huella y los pinos podados, el muelle hasta la playa), las mallas
    // propias de su cabaña y su muelle, el relleno (juncos, rocas, bosque, matas), el pasto y los helechos (solo cerca) y los dos trozos
    // de terreno. Todo en búferes reservados una vez (el pool de SegmentStore): un segmento que se olvida devuelve su lugar.
    public sealed class SegmentData
    {
        public const int MaxProps = 24;

        public SegmentData()
        {
            Props = new ShoreProp[MaxProps];
            FootY = new double[MaxProps];
            Pruned = new bool[MaxProps];
            Fill = new FillBuffer(ShoreFill.SegmentCapacity);
            Terrain = new Mesh?[2];
            TerrainCanopyFrom = new double[2];
            Kit = new Mesh?[KitSegmentMesh.Lods];
        }

        public long K { get; internal set; } = long.MinValue;

        public double Z0 => K * ShoreEnvironment.SegmentLength;

        public SegmentStage Stage { get; internal set; }

        public ShoreProp[] Props { get; }

        public int PropCount { get; internal set; }

        public double[] FootY { get; }

        public bool[] Pruned { get; }

        public bool HasCabin { get; internal set; }

        public CabinLayout Cabin { get; internal set; }

        public bool HasPier { get; internal set; }

        public PierLayout Pier { get; internal set; }

        // Las mallas de la cabaña y del muelle de este segmento (LOD 0 y 1), hechas al entrar.
        public Mesh? CabinLod0 { get; internal set; }

        public Mesh? CabinLod1 { get; internal set; }

        public Mesh? PierLod0 { get; internal set; }

        public Mesh? PierLod1 { get; internal set; }

        public FillBuffer Fill { get; }

        // El pasto y los helechos (de un pool aparte: solo los segmentos cerca de la cámara), con su avance en z.
        public FillBuffer? Grass { get; internal set; }

        public double GrassBuiltTo { get; internal set; } = double.NaN;

        public bool GrassDone => Grass != null && GrassBuiltTo >= Z0 + ShoreEnvironment.SegmentLength;

        // Los trozos de terreno izquierdo (0) y derecho (1), con la copa desde qué |x| (45 con el bosque cercano, 18 sin él).
        public Mesh?[] Terrain { get; }

        public double[] TerrainCanopyFrom { get; }

        // It. 8 (río, costa y regata): la malla del segmento (KitSegmentMesh) en sus dos LOD (0 cercano, 1 lejano), hecha al entrar.
        public Mesh?[] Kit { get; }

        public bool TerrainReady(int side) => Terrain[side] != null && Stage >= (side == 0 ? SegmentStage.TerrainLeft : SegmentStage.TerrainRight);
    }

    public enum SegmentStage
    {
        Empty,
        Placed,
        PropsMeshed,
        TerrainLeft,
        TerrainRight,
        Reeds,
        Rocks,
        Forest,
        Meadow,
        Ready,
    }

    // El pool de segmentos y su armado por tramos (06a-escena-lago/02-tech-design §2.16, §3.6; DN6a-11): la ventana de segmentos va de
    // d − 25 m a d + 450 m; cada cuadro corre trabajos de costo acotado (un trozo de terreno, una malla, un relleno, 5 m de pasto)
    // mientras quede tiempo, del más cercano al más lejano y por pasadas (lo de los objetos del legacy primero, después el terreno, el
    // relleno y el pasto). El trozo lejano (de la ventana a 1,5 km, sin la franja sumergida y con la orilla a +0,4 m, DT6a-18) se rehace
    // cuando la ventana avanza un segmento, antes que nada. Con Synchronous (el arnés, las pruebas, un salto de distancia) todo en el
    // acto: el resultado es el mismo, solo cambia cuándo. Sin asignaciones en régimen (pool, búferes y mallas reutilizados).
    public sealed class SegmentStore : IDisposable
    {
        public const int Slots = 16;
        public const int GrassSlots = 6;

        // Las matas de pasto y helechos que caben en el buffer de un segmento.
        public const int GrassCapacity = 12800;
        public const double GrassStripMeters = 5;
        public const double FarEnd = 1500;
        public const double FarRowStep = 8;
        public const double NearRowStep = 1;
        public const double NearRange = 450;

        private readonly EnvironmentId _environment;
        private readonly ShoreSegments _segments;
        private readonly SegmentData[] _slots = new SegmentData[Slots];
        private readonly FillBuffer[] _grass = new FillBuffer[GrassSlots];
        private readonly bool[] _grassUsed = new bool[GrassSlots];
        private readonly DartRandom _rnd = new DartRandom(1);
        private readonly MeshUpload _upload = new MeshUpload();
        private readonly MeshData _nearData;
        private readonly MeshData _farData;
        private readonly MeshData _cabinData;
        private readonly MeshData _pierData;
        // It. 8: el kit de río, costa y regata (null en el lago).
        private readonly KitSegmentMesh? _kit;
        private readonly RiverPlacement? _placement;
        private readonly MeshData? _kitData;
        private readonly HouseLayout[] _houses = new HouseLayout[RiverPlacement.MaxHousesPerSegment];
        private readonly FillBuffer _kitFill = new FillBuffer(EnvironmentFill.SegmentCapacity);
        private readonly Stopwatch _watch = new Stopwatch();
        private long _farStartK = long.MinValue;
        private long _farBuiltK = long.MinValue;

        public SegmentStore(ShoreEnvironment environment)
        {
            if (environment == null)
            {
                throw new ArgumentNullException(nameof(environment));
            }

            _environment = environment.Id;
            _segments = new ShoreSegments(environment);
            for (var i = 0; i < Slots; i++)
            {
                _slots[i] = new SegmentData();
            }

            for (var i = 0; i < (environment.Id == EnvironmentId.Lake ? GrassSlots : 0); i++)
            {
                // La pradera de un segmento: hasta x = 60 a la derecha y el talud a la izquierda, 4 por m², y los helechos.
                _grass[i] = new FillBuffer(GrassCapacity);
            }

            var nearRows = (int)Math.Round(ShoreEnvironment.SegmentLength / NearRowStep);
            _nearData = new MeshData(TerrainChunkMesh.VertexCapacity(nearRows, false), TerrainChunkMesh.IndexCapacity(nearRows, false));
            var farRows = (int)Math.Ceiling((FarEnd - NearRange + ShoreEnvironment.SegmentLength) / FarRowStep) + 1;
            _farData = new MeshData(TerrainChunkMesh.VertexCapacity(farRows, true), TerrainChunkMesh.IndexCapacity(farRows, true));
            _cabinData = new MeshData(CabinMesh.VertexCapacity(0), CabinMesh.IndexCapacity(0));
            _pierData = new MeshData(PierMesh.VertexCapacity(0), PierMesh.IndexCapacity(0));
            FarMeshes = new[] { MeshUpload.NewMesh("scene-terrain-far-left"), MeshUpload.NewMesh("scene-terrain-far-right") };
            if (environment.Id != EnvironmentId.Lake)
            {
                _kit = new KitSegmentMesh(environment.Id);
                _kitData = new MeshData(KitSegmentMesh.VertexCapacity, KitSegmentMesh.IndexCapacity);
                _placement = environment.Id == EnvironmentId.River ? new RiverPlacement(_segments) : null;
            }
        }

        public EnvironmentId Environment => _environment;

        public bool IsKit => _kit != null;

        // Las mallas del trozo lejano (izquierda, derecha) y desde qué z de mundo arrancan.
        public Mesh[] FarMeshes { get; }

        public double FarStartZ => _farBuiltK == long.MinValue ? double.NaN : _farBuiltK * ShoreEnvironment.SegmentLength;

        public bool FarReady => _farBuiltK != long.MinValue;

        public long FarStartK => _farBuiltK;

        // La densidad del bosque cercano: sin él (Bajo), la copa de la malla arranca en 18.
        public bool NearForest { get; set; } = true;

        public double GrassEnd { get; set; } = MaterialArt.GrassEndHigh;

        // ¿Queda trabajo pendiente en la ventana actual? (el arnés espera a que no quede antes de una toma).
        public bool HasPendingWork { get; private set; } = true;

        public int JobsLastStep { get; private set; }

        public int Count(out int ready)
        {
            var n = 0;
            ready = 0;
            for (var i = 0; i < Slots; i++)
            {
                if (_slots[i].K != long.MinValue)
                {
                    n++;
                    if (_slots[i].Stage == SegmentStage.Ready)
                    {
                        ready++;
                    }
                }
            }

            return n;
        }

        public SegmentData? Find(long k)
        {
            for (var i = 0; i < Slots; i++)
            {
                if (_slots[i].K == k)
                {
                    return _slots[i];
                }
            }

            return null;
        }

        public SegmentData SlotAt(int i) => _slots[i];

        public static void Window(double distance, out long kMin, out long kMax)
        {
            kMin = Math.Max(0, (long)Math.Floor((distance - LakeArt.FillCoverageBehind) / ShoreEnvironment.SegmentLength));
            kMax = Math.Max(0, (long)Math.Floor((distance + NearRange) / ShoreEnvironment.SegmentLength));
        }

        // Avanza el armado de la ventana de `distance` con un tope de tiempo (ms; double.PositiveInfinity = todo en el acto).
        public void Step(double distance, double budgetMs)
        {
            Window(distance, out var kMin, out var kMax);
            Release(kMin, kMax, distance);
            _watch.Restart();
            var jobs = 0;
            var synchronous = double.IsPositiveInfinity(budgetMs);

            // 0. El trozo lejano, desde el segmento siguiente al último cercano.
            _farStartK = kMax + 1;
            if (_farBuiltK != _farStartK && Within(budgetMs, jobs))
            {
                BuildFar(_farStartK);
                jobs++;
            }

            // 1. Los objetos del legacy (colocación y mallas propias), del más cercano al más lejano.
            for (var k = kMin; k <= kMax && Within(budgetMs, jobs); k++)
            {
                var seg = Acquire(k);
                while (seg.Stage < SegmentStage.PropsMeshed && Within(budgetMs, jobs))
                {
                    RunStage(seg, distance);
                    jobs++;
                }
            }

            // 2. El terreno, 3. el relleno.
            for (var pass = 0; pass < 2; pass++)
            {
                var target = pass == 0 ? SegmentStage.TerrainRight : SegmentStage.Ready;
                for (var k = kMin; k <= kMax && Within(budgetMs, jobs); k++)
                {
                    var seg = Find(k);
                    while (seg != null && seg.Stage < target && Within(budgetMs, jobs))
                    {
                        RunStage(seg, distance);
                        jobs++;
                    }
                }
            }

            // El terreno con la copa equivocada (el bosque cercano llega a los 120 m): se rehace.
            for (var k = kMin; k <= kMax && Within(budgetMs, jobs); k++)
            {
                var seg = Find(k);
                if (seg != null && seg.Stage >= SegmentStage.TerrainLeft && seg.TerrainCanopyFrom[0] != CanopyFrom(seg, distance))
                {
                    BuildTerrain(seg, 0, distance);
                    jobs++;
                }
            }

            // 4. El pasto y los helechos de los segmentos cercanos, de a 5 m.
            var grassTo = _kit != null ? double.NegativeInfinity : distance + GrassEnd + ShoreEnvironment.SegmentLength;
            for (var k = kMin; k <= kMax && Within(budgetMs, jobs); k++)
            {
                var seg = Find(k);
                if (seg == null || seg.Z0 > grassTo)
                {
                    continue;
                }

                if (seg.Grass == null && !AttachGrass(seg))
                {
                    continue;
                }

                while (!seg.GrassDone && Within(budgetMs, jobs))
                {
                    var from = double.IsNaN(seg.GrassBuiltTo) ? seg.Z0 : seg.GrassBuiltTo;
                    var to = Math.Min(seg.Z0 + ShoreEnvironment.SegmentLength, from + GrassStripMeters);
                    ShoreFill.Grass(_environment, from, to, seg.Grass!);
                    ShoreFill.Ferns(_environment, from, to, seg.Grass!);
                    seg.GrassBuiltTo = to;
                    jobs++;
                }
            }

            JobsLastStep = jobs;
            HasPendingWork = ComputePending(kMin, kMax, grassTo);
            if (synchronous && HasPendingWork)
            {
                // No debería pasar (el pool alcanza para la ventana): se anota para que el arnés no espere para siempre.
                HasPendingWork = false;
            }
        }

        private bool Within(double budgetMs, int jobs)
        {
            if (double.IsPositiveInfinity(budgetMs))
            {
                return true;
            }

            // Al menos un trabajo por cuadro con tramo > 0 (SceneWarmupPolicy.RunAnother).
            return budgetMs > 0 && (jobs == 0 || _watch.Elapsed.TotalMilliseconds < budgetMs);
        }

        private bool ComputePending(long kMin, long kMax, double grassTo)
        {
            if (_farBuiltK != _farStartK)
            {
                return true;
            }

            for (var k = kMin; k <= kMax; k++)
            {
                var seg = Find(k);
                if (seg == null || seg.Stage < SegmentStage.Ready)
                {
                    return true;
                }

                if (seg.Z0 <= grassTo && !seg.GrassDone)
                {
                    return true;
                }
            }

            return false;
        }

        private void RunStage(SegmentData seg, double distance)
        {
            switch (seg.Stage)
            {
                case SegmentStage.Empty:
                    Place(seg);
                    seg.Stage = SegmentStage.Placed;
                    break;
                case SegmentStage.Placed:
                    BuildPropMeshes(seg);
                    seg.Stage = SegmentStage.PropsMeshed;
                    break;
                case SegmentStage.PropsMeshed:
                    BuildTerrain(seg, 0, distance);
                    seg.Stage = SegmentStage.TerrainLeft;
                    break;
                case SegmentStage.TerrainLeft:
                    BuildTerrain(seg, 1, distance);
                    seg.Stage = SegmentStage.TerrainRight;
                    break;
                case SegmentStage.TerrainRight when _kit != null:
                    // El relleno de río, costa y regata ya está en la malla del kit.
                    seg.Stage = SegmentStage.Ready;
                    break;
                case SegmentStage.TerrainRight:
                    ShoreFill.Reeds(_environment, seg.K, new ReadOnlySpan<ShoreProp>(seg.Props, 0, seg.PropCount), _rnd, seg.Fill);
                    seg.Stage = SegmentStage.Reeds;
                    break;
                case SegmentStage.Reeds:
                    ShoreFill.Rocks(_environment, seg.K, _rnd, seg.Fill);
                    seg.Stage = SegmentStage.Rocks;
                    break;
                case SegmentStage.Rocks:
                    ShoreFill.Forest(_environment, seg.K, _rnd, seg.Fill);
                    seg.Stage = SegmentStage.Forest;
                    break;
                case SegmentStage.Forest:
                    ShoreFill.Meadow(_environment, seg.K, _rnd, seg.Fill);
                    seg.Stage = SegmentStage.Ready;
                    break;
            }
        }

        // Los objetos del legacy del segmento y su colocación (PropPlacement: nada flota, la cabaña nunca se corre, DT6a-08).
        private void Place(SegmentData seg)
        {
            var props = _segments.PropsForSegment(seg.K);
            var n = Math.Min(props.Length, SegmentData.MaxProps);
            seg.PropCount = n;
            seg.HasCabin = false;
            seg.HasPier = false;
            for (var i = 0; i < n; i++)
            {
                seg.Props[i] = props[i];
                seg.Pruned[i] = false;
                seg.FootY[i] = 0;
            }

            var span = new ReadOnlySpan<ShoreProp>(seg.Props, 0, n);
            if (_kit != null)
            {
                for (var i = 0; i < n; i++)
                {
                    seg.FootY[i] = PropPlacement.FootY(_environment, seg.Props[i]);
                }

                return;
            }

            for (var i = 0; i < n; i++)
            {
                var p = seg.Props[i];
                if (p.Kind == PropKind.Cabin)
                {
                    seg.Cabin = PropPlacement.Cabin(_environment, p, span);
                    seg.HasCabin = true;
                }
                else if (p.Kind == PropKind.Pier)
                {
                    seg.Pier = PropPlacement.Pier(_environment, p);
                    seg.HasPier = true;
                }
            }

            for (var i = 0; i < n; i++)
            {
                var p = seg.Props[i];
                if (p.Kind != PropKind.Pine)
                {
                    continue;
                }

                seg.FootY[i] = PropPlacement.PineFootY(_environment, p);
                seg.Pruned[i] = seg.HasCabin && PropPlacement.IsPrunedBy(seg.Cabin, p);
            }
        }

        private void BuildPropMeshes(SegmentData seg)
        {
            if (_kit != null)
            {
                BuildKit(seg);
                return;
            }

            if (seg.HasCabin)
            {
                var cabin = seg.Cabin;
                var plinth = Math.Max(0, cabin.FloorY - cabin.PlinthBottomY);
                seg.CabinLod0 ??= MeshUpload.NewMesh("scene-cabin-lod0");
                seg.CabinLod1 ??= MeshUpload.NewMesh("scene-cabin-lod1");
                CabinMesh.Build(cabin.Depth, plinth, 0, _cabinData);
                _upload.Upload(_cabinData, seg.CabinLod0, false, null, true);
                CabinMesh.Build(cabin.Depth, plinth, 1, _cabinData);
                _upload.Upload(_cabinData, seg.CabinLod1, false, null, true);
            }

            if (seg.HasPier)
            {
                seg.PierLod0 ??= MeshUpload.NewMesh("scene-pier-lod0");
                seg.PierLod1 ??= MeshUpload.NewMesh("scene-pier-lod1");
                PierMesh.Build(seg.Pier, 0, _pierData);
                _upload.Upload(_pierData, seg.PierLod0, false, null, true);
                PierMesh.Build(seg.Pier, 1, _pierData);
                _upload.Upload(_pierData, seg.PierLod1, false, null, true);
            }
        }

        // La malla del kit del segmento (KitSegmentMesh) en sus dos LOD: los objetos con su colocación y el relleno.
        private void BuildKit(SegmentData seg)
        {
            var span = new ReadOnlySpan<ShoreProp>(seg.Props, 0, seg.PropCount);
            var houses = _placement != null ? _placement.Houses(seg.K, _houses) : 0;
            _kitFill.Clear();
            EnvironmentFill.Segment(_environment, seg.K, span, _rnd, _kitFill);
            for (var lod = 0; lod < KitSegmentMesh.Lods; lod++)
            {
                _kit!.Build(seg.K, span, new ReadOnlySpan<double>(seg.FootY, 0, seg.PropCount), new ReadOnlySpan<HouseLayout>(_houses, 0, houses),
                    _kitFill.Items, lod, _kitData!);
                var mesh = seg.Kit[lod] ??= MeshUpload.NewMesh("scene-kit-" + _environment + "-lod" + lod);
                _upload.Upload(_kitData!, mesh, false, null, true);
            }
        }

        // La copa de la malla: desde 45 m si el bosque cercano se dibuja en ese segmento (a menos de 120 m), si no desde 18.
        private double CanopyFrom(SegmentData seg, double distance)
        {
            var nearForest = NearForest && seg.Z0 - distance <= LakeArt.ForestMaxRelZ;
            return nearForest ? LakeArt.ForestOuterX : LakeArt.ForestInnerX;
        }

        private void BuildTerrain(SegmentData seg, int side, double distance)
        {
            var canopy = CanopyFrom(seg, distance);
            var z0 = seg.Z0;
            TerrainChunkMesh.Build(_environment, side == 0 ? -1 : 1, z0, z0 + ShoreEnvironment.SegmentLength, NearRowStep, false, true, canopy, z0, _nearData);
            var mesh = seg.Terrain[side] ??= MeshUpload.NewMesh(side == 0 ? "scene-terrain-left" : "scene-terrain-right");
            _upload.Upload(_nearData, mesh, true);
            seg.TerrainCanopyFrom[side] = canopy;
        }

        private void BuildFar(long startK)
        {
            var z0 = startK * ShoreEnvironment.SegmentLength;
            var zEnd = Math.Max(z0 + ShoreEnvironment.SegmentLength, z0 - NearRange + FarEnd);
            for (var side = 0; side < 2; side++)
            {
                TerrainChunkMesh.Build(_environment, side == 0 ? -1 : 1, z0, zEnd, FarRowStep, true, true, LakeArt.ForestInnerX, z0, _farData);
                _upload.Upload(_farData, FarMeshes[side], true);
            }

            _farBuiltK = startK;
        }

        private SegmentData Acquire(long k)
        {
            var found = Find(k);
            if (found != null)
            {
                return found;
            }

            for (var i = 0; i < Slots; i++)
            {
                if (_slots[i].K == long.MinValue)
                {
                    var seg = _slots[i];
                    seg.K = k;
                    seg.Stage = SegmentStage.Empty;
                    seg.PropCount = 0;
                    seg.Fill.Clear();
                    seg.GrassBuiltTo = double.NaN;
                    return seg;
                }
            }

            // Sin lugar (no debería pasar: 16 > 11 segmentos de la ventana): el más lejano sale.
            var far = 0;
            for (var i = 1; i < Slots; i++)
            {
                if (_slots[i].K > _slots[far].K)
                {
                    far = i;
                }
            }

            Free(_slots[far]);
            return Acquire(k);
        }

        private void Release(long kMin, long kMax, double distance)
        {
            for (var i = 0; i < Slots; i++)
            {
                var seg = _slots[i];
                if (seg.K != long.MinValue && (seg.K < kMin || seg.K > kMax))
                {
                    Free(seg);
                }
                else if (seg.Grass != null && seg.Z0 + ShoreEnvironment.SegmentLength < distance - LakeArt.FillCoverageBehind)
                {
                    DetachGrass(seg);
                }
            }
        }

        private void Free(SegmentData seg)
        {
            DetachGrass(seg);
            seg.K = long.MinValue;
            seg.Stage = SegmentStage.Empty;
            seg.PropCount = 0;
            seg.Fill.Clear();
            seg.HasCabin = false;
            seg.HasPier = false;
        }

        private bool AttachGrass(SegmentData seg)
        {
            for (var i = 0; i < GrassSlots; i++)
            {
                if (!_grassUsed[i])
                {
                    _grassUsed[i] = true;
                    _grass[i].Clear();
                    seg.Grass = _grass[i];
                    seg.GrassBuiltTo = double.NaN;
                    return true;
                }
            }

            return false;
        }

        private void DetachGrass(SegmentData seg)
        {
            if (seg.Grass == null)
            {
                return;
            }

            for (var i = 0; i < GrassSlots; i++)
            {
                if (ReferenceEquals(_grass[i], seg.Grass))
                {
                    _grassUsed[i] = false;
                }
            }

            seg.Grass = null;
            seg.GrassBuiltTo = double.NaN;
        }

        // Olvida todo (una sesión nueva o un salto de distancia grande: las pruebas y el arnés).
        public void Reset()
        {
            for (var i = 0; i < Slots; i++)
            {
                Free(_slots[i]);
            }

            _farBuiltK = long.MinValue;
            HasPendingWork = true;
        }

        public void Dispose()
        {
            for (var i = 0; i < Slots; i++)
            {
                var seg = _slots[i];
                MeshUpload.Destroy(seg.CabinLod0);
                MeshUpload.Destroy(seg.CabinLod1);
                MeshUpload.Destroy(seg.PierLod0);
                MeshUpload.Destroy(seg.PierLod1);
                MeshUpload.Destroy(seg.Terrain[0]);
                MeshUpload.Destroy(seg.Terrain[1]);
                MeshUpload.Destroy(seg.Kit[0]);
                MeshUpload.Destroy(seg.Kit[1]);
            }

            MeshUpload.Destroy(FarMeshes[0]);
            MeshUpload.Destroy(FarMeshes[1]);
            _upload.Dispose();
        }
    }
}
