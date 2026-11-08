using System;
using RowMate.Core.Scene;
using RowMate.Core.Scene.Art;
using RowMate.Core.Scene.Geometry;
using UnityEngine;
using UnityEngine.Rendering;

namespace RowMate.Scene
{
    // El relleno de la orilla (06a-escena-lago/02-tech-design §2.13, §3.6 "Relleno"; DD6a-11, DN6a-14 a -17; P-09): juncos, rocas,
    // bosque cercano, matas de la pradera, pasto y helechos de ShoreFill (semillas propias: el legacy es el mismo con y sin relleno),
    // como mallas opacas instanciadas, desde d − 25 m (el vuelo y la toma aérea). Por distancia: juncos hasta 120 m (fundido desde
    // 100), rocas hasta 200 (desde 180), bosque cercano hasta 120 (desde 110; no en Bajo), matas de la pradera con la aparición del
    // legacy, pasto con el raleo de 4/m² a 0 en el fin del nivel (60 / 40 / 30 m) y el achique del shader en el último 20 %. Los LOD
    // complementarios como los pinos. Determinista por distancia, se pidan los segmentos en el orden que se pidan. Sin asignaciones.
    public sealed class FillStreamer
    {
        private const double BehindCamera = 15;

        private SegmentStore _store;
        private readonly ShoreMeshes _meshes;
        private readonly Material _foliage;
        private readonly Material _prop;
        private readonly Material _grass;
        private readonly Material _reeds;
        private readonly Camera _camera;
        private readonly int _layer;
        private readonly InstanceBatchSet[,] _reedSets = new InstanceBatchSet[ShoreMeshes.ReedVariants, 2];
        private readonly InstanceBatchSet[,] _rockSets = new InstanceBatchSet[LakeArt.RockVariants, 2];
        private readonly InstanceBatchSet[,] _forestSets = new InstanceBatchSet[3, 2];
        private readonly InstanceBatchSet[,] _meadowPines = new InstanceBatchSet[3, 2];
        // Una forma por tipo de relleno (las rocas, los juncos y el bosque cercano varían por escala, giro, alto y paleta): cada forma y LOD es
        // un dibujo (B6a-05: el tope de 50 SetPass de §20.1 en Medio).
        private const int RockMeshes = 1;
        private const int ReedMeshes = 1;
        private const int ForestForms = 1;
        private readonly InstanceBatchSet[] _grassSets = new InstanceBatchSet[2];
        private readonly Bounds _bounds = new Bounds(new Vector3(0, 5, 100), new Vector3(400, 40, 500));

        // It. 8 (cierre de WP-B, SceneBudgetTests Alto): la base de cada objeto del relleno (el giro en y por la escala) no depende de la
        // distancia; se calcula una vez por objeto con el mismo TRS de antes (FillBasis) y por cuadro solo se escribe la traslación. Era
        // el 78 % de la CPU de la escena del lago en Alto (0,87 de 1,12 ms). Arreglos reservados al armar: 0 B por cuadro.
        private readonly FillBasisCache[] _fillBases = new FillBasisCache[SegmentStore.Slots];
        private readonly FillBasisCache?[] _grassBases = new FillBasisCache?[SegmentStore.GrassSlots];

        public FillStreamer(SegmentStore store, ShoreMeshes meshes, ShoreStreamer shore, Material foliage, Material prop, Material grass, Material reeds, Camera camera, int layer)
        {
            _store = store;
            _meshes = meshes;
            _foliage = foliage;
            _prop = prop;
            _grass = grass;
            _reeds = reeds;
            _camera = camera;
            _layer = layer;
            for (var s = 0; s < _fillBases.Length; s++)
            {
                _fillBases[s] = new FillBasisCache(ShoreFill.SegmentCapacity);
            }

            ReserveGrassBases(store);
            for (var lod = 0; lod < 2; lod++)
            {
                for (var v = 0; v < ShoreMeshes.ReedVariants; v++)
                {
                    _reedSets[v, lod] = new InstanceBatchSet(4, SceneShaderIds.WindPhase, -1, SceneShaderIds.Fade) { Mesh = meshes.Reed[v, lod] };
                }

                for (var v = 0; v < LakeArt.RockVariants; v++)
                {
                    _rockSets[v, lod] = new InstanceBatchSet(1, -1, -1, SceneShaderIds.Fade, SceneShaderIds.Lit) { Mesh = meshes.Rock[v, lod] };
                }

                for (var form = 0; form < 3; form++)
                {
                    _forestSets[form, lod] = new InstanceBatchSet(2, SceneShaderIds.Variant, SceneShaderIds.WindPhase, SceneShaderIds.Fade)
                    {
                        Mesh = meshes.Forest[form, lod],
                    };
                    _meadowPines[form, lod] = shore.PineSet(form, lod + 1);
                }

                _grassSets[lod] = new InstanceBatchSet(12, SceneShaderIds.Tint, SceneShaderIds.WindPhase, -1, -1, 1000) { Mesh = meshes.Grass[lod] };
            }
        }

        public QualityTier Tier { get; set; } = QualityTier.High;

        // Como ShoreStreamer.ShadowLods: el bosque cercano proyecta sombra solo con su LOD 0 (≤ 40 m) y con mapa.
        public int ShadowLods { get; set; } = 2;

        public bool NearForest { get; set; } = true;

        public double GrassEnd { get; set; } = MaterialArt.GrassEndHigh;

        // La densidad del pasto del nivel (Bajo: la mitad, por el número fijo de cada mata).
        public double GrassDensity { get; set; } = 1;

        public int Instances { get; private set; }

        public int GrassInstances { get; private set; }

        public int Overflow { get; private set; }

        // cameraZ: la z de la cámara en la cinta (atrás del origen en el vuelo, la salida y la toma aérea: ahí se ve el relleno de atrás).
        // It. 8: el almacén de otro escenario (el cambio de kit; el relleno de los kits va en su malla y acá queda vacío).
        public void Rebind(SegmentStore store)
        {
            _store = store;
            for (var s = 0; s < _fillBases.Length; s++)
            {
                _fillBases[s].Reset();
            }

            ReserveGrassBases(store);
            for (var s = 0; s < _grassBases.Length; s++)
            {
                _grassBases[s]?.Reset();
            }
        }

        public void Apply(double distance, double cameraZ = 0)
        {
            Clear();
            _behind = cameraZ < -1 ? BehindCamera : 0;
            var instances = 0;
            for (var s = 0; s < SegmentStore.Slots; s++)
            {
                var seg = _store.SlotAt(s);
                if (seg.K == long.MinValue || seg.Stage < SegmentStage.Reeds)
                {
                    continue;
                }

                var items = seg.Fill.Items;
                var bases = _fillBases[s].For(seg.K, seg.Fill, items);
                for (var i = 0; i < items.Length; i++)
                {
                    instances += Add(items[i], bases[i], distance) ? 1 : 0;
                }
            }

            var grass = 0;
            for (var s = 0; s < SegmentStore.Slots; s++)
            {
                var seg = _store.SlotAt(s);
                if (seg.K == long.MinValue || seg.Grass == null)
                {
                    continue;
                }

                if (seg.Z0 - distance > GrassEnd || seg.Z0 + ShoreEnvironment.SegmentLength - distance < -BehindCamera)
                {
                    continue;
                }

                var items = seg.Grass.Items;
                var cache = GrassBases(seg.Grass);
                var bases = cache.For(seg.K, seg.Grass, items);
                for (var from = 0; from < items.Length; from += FillBasisCache.Block)
                {
                    // Un bloque entero que AddGrass descartaría se salta (las matas van por filas de 0,5 m en z): detrás de la franja, o
                    // con la cota de la distancia en el plano (el menor |x| y el menor |relZ| del bloque) en el fin o más allá, que
                    // KeepGrass descarta. Las operaciones son monótonas: los mismos objetos y en el mismo orden que recorriéndolas todas.
                    var block = from / FillBasisCache.Block;
                    var lo = cache.MinZ[block] - distance;
                    var hi = cache.MaxZ[block] - distance;
                    if (hi < -_behind)
                    {
                        continue;
                    }

                    var nearZ = lo > 0 ? lo : hi < 0 ? -hi : 0;
                    var nearX = cache.MinAbsX[block];
                    if (Math.Sqrt(nearX * nearX + nearZ * nearZ) >= GrassEnd)
                    {
                        continue;
                    }

                    var to = Math.Min(items.Length, from + FillBasisCache.Block);
                    for (var i = from; i < to; i++)
                    {
                        grass += AddGrass(items[i], bases[i], distance) ? 1 : 0;
                    }
                }
            }

            Instances = instances + grass;
            GrassInstances = grass;
            var overflow = 0;
            for (var lod = 0; lod < 2; lod++)
            {
                overflow += _grassSets[lod].Overflow;
            }

            Overflow = overflow;
        }

        public void Draw()
        {
            for (var lod = 0; lod < 2; lod++)
            {
                for (var v = 0; v < ShoreMeshes.ReedVariants; v++)
                {
                    _reedSets[v, lod].Draw(_reeds, _camera, _layer, ShadowCastingMode.Off, _bounds);
                }

                for (var v = 0; v < LakeArt.RockVariants; v++)
                {
                    _rockSets[v, lod].Draw(_prop, _camera, _layer, ShadowCastingMode.Off, _bounds);
                }

                for (var form = 0; form < 3; form++)
                {
                    _forestSets[form, lod].Draw(_foliage, _camera, _layer, lod == 0 && ShadowLods > 0 ? ShadowCastingMode.On : ShadowCastingMode.Off, _bounds);
                }

                _grassSets[lod].Draw(_grass, _camera, _layer, ShadowCastingMode.Off, _bounds);
            }
        }

        private void Clear()
        {
            for (var lod = 0; lod < 2; lod++)
            {
                for (var v = 0; v < ShoreMeshes.ReedVariants; v++)
                {
                    _reedSets[v, lod].Clear();
                }

                for (var v = 0; v < LakeArt.RockVariants; v++)
                {
                    _rockSets[v, lod].Clear();
                }

                for (var form = 0; form < 3; form++)
                {
                    _forestSets[form, lod].Clear();
                }

                _grassSets[lod].Clear();
            }
        }

        private bool Add(in FillItem item, in Vector4 basis, double distance)
        {
            var relZ = item.Z - distance;
            if (relZ < -LakeArt.FillCoverageBehind)
            {
                return false;
            }

            // La aparición lejana del legacy (400 → 360 m); el relleno no lleva el tramado cercano (va también detrás de la cámara).
            var appear = SceneMath.Clamp01((MaterialArt.AppearFar - relZ) / (MaterialArt.AppearFar - MaterialArt.AppearNear));
            switch (item.Kind)
            {
                case FillKind.Reed:
                {
                    var end = MaterialArt.TierDistance(MaterialArt.ReedLods[1], Tier);
                    var from = MaterialArt.TierDistance(MaterialArt.ReedFadeFrom, Tier);
                    if (relZ >= end)
                    {
                        return false;
                    }

                    var fade = appear * SceneMath.Clamp01((end - relZ) / (end - from));
                    var variant = ((item.Variant % ReedMeshes) + ReedMeshes) % ReedMeshes;
                    var matrix = FillBasis.Compose(basis, (float)item.Height, (float)item.X, (float)item.BaseY, (float)relZ);
                    var wind = new Vector4((float)MaterialArt.ReedWindPhase(item.Z, item.Variant), 1, 0, 0);
                    AddLod(_reedSets, variant, MaterialArt.TierDistance(MaterialArt.ReedLods[0], Tier), relZ, fade, matrix, wind, Vector4.zero);
                    return true;
                }

                case FillKind.Rock:
                {
                    var end = MaterialArt.TierDistance(MaterialArt.RockLods[1], Tier);
                    var from = MaterialArt.TierDistance(MaterialArt.RockFadeFrom, Tier);
                    if (relZ >= end)
                    {
                        return false;
                    }

                    var fade = appear * SceneMath.Clamp01((end - relZ) / (end - from));
                    var h = (float)item.Height;
                    var matrix = FillBasis.Compose(basis, h, (float)item.X, (float)item.BaseY, (float)relZ);
                    var boundary = MaterialArt.TierDistance(MaterialArt.RockLods[0], Tier);
                    var variant = item.Variant % RockMeshes;
                    MaterialArt.LodFadePair(relZ, boundary, out var near, out var far);
                    if (near > 0 && near < 1)
                    {
                        _rockSets[variant, 0].Add(matrix, Vector4.zero, Vector4.zero, (float)(fade * near), 0);
                        _rockSets[variant, 1].Add(matrix, Vector4.zero, Vector4.zero, (float)(fade * far), 0);
                    }
                    else
                    {
                        _rockSets[variant, relZ < boundary ? 0 : 1].Add(matrix, Vector4.zero, Vector4.zero, (float)fade, 0);
                    }

                    return true;
                }

                case FillKind.ForestPine:
                {
                    if (!NearForest)
                    {
                        return false;
                    }

                    var end = MaterialArt.TierDistance(MaterialArt.ForestLods[1], Tier);
                    var from = MaterialArt.TierDistance(MaterialArt.ForestFadeFrom, Tier);
                    if (relZ >= end)
                    {
                        return false;
                    }

                    var fade = appear * SceneMath.Clamp01((end - relZ) / (end - from));
                    var s = (float)(item.Height / LakeArt.PineHeight);
                    var matrix = FillBasis.Compose(basis, s, (float)item.X, (float)item.BaseY, (float)relZ);
                    var variant = new Vector4(PineMesh.PaletteOf(item.Variant), (float)MaterialArt.ForestLuminance, 0, 0);
                    var wind = new Vector4((float)MaterialArt.PineWindPhase(item.X), 1, 0, 0);
                    AddLod(_forestSets, PineMesh.FormOf(item.Variant) % ForestForms, MaterialArt.TierDistance(MaterialArt.ForestLods[0], Tier), relZ, fade, matrix, variant, wind);
                    return true;
                }

                case FillKind.MeadowConifer:
                case FillKind.MeadowBush:
                {
                    var end = MaterialArt.TierDistance(MaterialArt.MeadowLods[1], Tier);
                    if (relZ >= end || appear <= 0)
                    {
                        return false;
                    }

                    var bush = item.Kind == FillKind.MeadowBush;
                    var s = (float)(item.Height / LakeArt.PineHeight);
                    var matrix = FillBasis.Compose(basis, s, (float)item.X, (float)item.BaseY, (float)relZ);
                    var variant = new Vector4(PineMesh.PaletteOf(item.Variant), 1, bush ? 1 : 0, 0);
                    var wind = new Vector4((float)MaterialArt.PineWindPhase(item.X), (float)MaterialArt.MeadowWindScale, 0, 0);
                    var form = PineMesh.FormOf(item.Variant);
                    if (bush)
                    {
                        AddLod(_forestSets, form % ForestForms, MaterialArt.TierDistance(MaterialArt.MeadowLods[0], Tier), relZ, appear, matrix, variant, wind);
                    }
                    else
                    {
                        AddLod(_meadowPines, form, MaterialArt.TierDistance(MaterialArt.MeadowLods[0], Tier), relZ, appear, matrix, variant, wind);
                    }

                    return true;
                }
            }

            return false;
        }

        private double _behind;

        private bool AddGrass(in FillItem item, in Vector4 basis, double distance)
        {
            var relZ = item.Z - distance;
            if (relZ < -_behind || item.Rank >= GrassDensity)
            {
                return false;
            }

            // El raleo por la distancia a la cámara en el plano (la misma que usa el shader para el fin): 4/m² junto a la cámara.
            var planar = Math.Sqrt(item.X * item.X + relZ * relZ);
            if (!ShoreFill.KeepGrass(item.Rank, planar, GrassEnd))
            {
                return false;
            }

            var fern = item.Kind == FillKind.Fern;
            var h = (float)item.Height;
            var w = fern ? 1.6f : 1f;
            var matrix = FillBasis.Compose(basis, h, (float)item.X, (float)item.BaseY, (float)relZ);
            // _RmTint = (brillo ± 8 %; verde A → B; helecho; número fijo de la mata para la densidad).
            var tint = new Vector4((float)(1 + 0.08 * item.Tint), (float)(0.5 + 0.5 * item.Tint), fern ? 1 : 0, (float)item.Rank);
            var wind = new Vector4((float)MaterialArt.GrassWindPhase(item.X, item.Z), 1, 0, 0);
            _grassSets[relZ < MaterialArt.GrassLod0 ? 0 : 1].Add(matrix, tint, wind, 0, 0);
            return true;
        }

        // Las bases de las matas del buffer de pasto (uno de los GrassSlots del almacén; null en río, costa y regata).
        private FillBasisCache GrassBases(FillBuffer buffer)
        {
            FillBasisCache? free = null;
            for (var i = 0; i < _grassBases.Length; i++)
            {
                var cache = _grassBases[i]!;
                if (ReferenceEquals(cache.Source, buffer))
                {
                    return cache;
                }

                if (free == null && cache.Source == null)
                {
                    free = cache;
                }
            }

            // Un buffer que no se vio todavía: el primero libre (hay uno por GrassSlots; si no, el primero, que se rehace).
            return free ?? _grassBases[0]!;
        }

        // El lago (el único con pasto) reserva las bases de sus GrassSlots una sola vez, al armar o al volver a él.
        private void ReserveGrassBases(SegmentStore store)
        {
            if (store.IsKit || _grassBases[0] != null)
            {
                return;
            }

            for (var i = 0; i < _grassBases.Length; i++)
            {
                _grassBases[i] = new FillBasisCache(SegmentStore.GrassCapacity);
            }
        }

        private static void AddLod(InstanceBatchSet[,] sets, int variant, double boundary, double relZ, double fade, in Matrix4x4 matrix,
                                   in Vector4 v0, in Vector4 v1)
        {
            MaterialArt.LodFadePair(relZ, boundary, out var near, out var far);
            if (near > 0 && near < 1)
            {
                sets[variant, 0].Add(matrix, v0, v1, (float)(fade * near), 0);
                sets[variant, 1].Add(matrix, v0, v1, (float)(fade * far), 0);
                return;
            }

            sets[variant, relZ < boundary ? 0 : 1].Add(matrix, v0, v1, (float)fade, 0);
        }
    }

    // La base de un objeto del relleno (FillStreamer): el giro en y y la escala (ancho, alto, ancho) con el mismo Matrix4x4.TRS que se
    // usaba por cuadro. Con un giro solo en y, el TRS deja el 3 × 3 en m00, m02, m20, m22 (giro por ancho) y m11 (= alto), y la
    // traslación tal cual en la cuarta columna; Compose arma la misma matriz con la traslación del cuadro (FillBasisTests lo compara).
    internal static class FillBasis
    {
        public static Vector4 Of(float angleDeg, float width, float height)
        {
            var m = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0, angleDeg, 0), new Vector3(width, height, width));
            return new Vector4(m.m00, m.m02, m.m20, m.m22);
        }

        // Los mismos giros y escalas que los casos de FillStreamer.Add y AddGrass (los tipos que no se dibujan, cero).
        public static Vector4 Of(in FillItem item)
        {
            switch (item.Kind)
            {
                case FillKind.Reed:
                    return Of(item.Variant % 360, 1, (float)item.Height);
                case FillKind.Rock:
                {
                    var h = (float)item.Height;
                    return Of(item.Variant * 71 % 360, h, h);
                }

                case FillKind.ForestPine:
                {
                    var s = (float)(item.Height / LakeArt.PineHeight);
                    return Of(item.Variant % 360, s, s);
                }

                case FillKind.MeadowConifer:
                case FillKind.MeadowBush:
                {
                    var s = (float)(item.Height / LakeArt.PineHeight);
                    return Of(item.Variant % 360, item.Kind == FillKind.MeadowBush ? s * 3.2f : s, s);
                }

                case FillKind.Grass:
                case FillKind.Fern:
                    return Of((float)(item.Rank * 360), item.Kind == FillKind.Fern ? 1.6f : 1f, (float)item.Height);
                default:
                    return Vector4.zero;
            }
        }

        public static Matrix4x4 Compose(in Vector4 basis, float height, float x, float y, float z)
        {
            var m = new Matrix4x4();
            m.m00 = basis.x;
            m.m02 = basis.y;
            m.m20 = basis.z;
            m.m22 = basis.w;
            m.m11 = height;
            m.m03 = x;
            m.m13 = y;
            m.m23 = z;
            m.m33 = 1;
            return m;
        }
    }

    // Las bases de un buffer de relleno (por segmento, o uno de los buffers de pasto): se completan a medida que el buffer crece (el
    // pasto se arma de a 5 m) y se rehacen si el buffer pasa a otro segmento o se vacía. Reservado al armar: 0 B por cuadro.
    internal sealed class FillBasisCache
    {
        // Los objetos por bloque de las cotas de z (MinZ, MaxZ) con que FillStreamer salta los bloques fuera de su franja.
        public const int Block = 32;

        private readonly Vector4[] _bases;
        private long _k = long.MinValue;
        private int _count;

        public FillBasisCache(int capacity)
        {
            _bases = new Vector4[capacity];
            var blocks = (capacity + Block - 1) / Block;
            MinZ = new double[blocks];
            MaxZ = new double[blocks];
            MinAbsX = new double[blocks];
        }

        public FillBuffer? Source { get; private set; }

        // La menor y la mayor z de mundo de cada bloque de Block objetos (válidas hasta el último objeto visto).
        public double[] MinZ { get; }

        public double[] MaxZ { get; }

        // El menor |x| de cada bloque (la cota de la distancia en el plano).
        public double[] MinAbsX { get; }

        public void Reset()
        {
            Source = null;
            _k = long.MinValue;
            _count = 0;
        }

        public Vector4[] For(long k, FillBuffer source, ReadOnlySpan<FillItem> items)
        {
            if (_k != k || !ReferenceEquals(Source, source) || items.Length < _count)
            {
                Source = source;
                _k = k;
                _count = 0;
            }

            var n = Math.Min(items.Length, _bases.Length);
            for (var i = _count; i < n; i++)
            {
                _bases[i] = FillBasis.Of(items[i]);
                var block = i / Block;
                var z = items[i].Z;
                var ax = Math.Abs(items[i].X);
                if (i % Block == 0)
                {
                    MinZ[block] = z;
                    MaxZ[block] = z;
                    MinAbsX[block] = ax;
                }
                else
                {
                    MinZ[block] = Math.Min(MinZ[block], z);
                    MaxZ[block] = Math.Max(MaxZ[block], z);
                    MinAbsX[block] = Math.Min(MinAbsX[block], ax);
                }
            }

            _count = Math.Max(_count, n);
            return _bases;
        }
    }
}
