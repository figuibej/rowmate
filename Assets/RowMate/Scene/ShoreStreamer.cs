using System;
using RowMate.Core.Scene;
using RowMate.Core.Scene.Art;
using UnityEngine;
using UnityEngine.Rendering;

namespace RowMate.Scene
{
    // Las mallas de la orilla hechas al precargar (SceneWorldBuilder): los pinos del legacy (3 formas × 3 LOD, y las podadas por una
    // cabaña), el bosque cercano (3 × 2), las rocas (5 × 2), los juncos (4 × 2) y el pasto (2 LOD).
    public sealed class ShoreMeshes
    {
        public const int ReedVariants = 4;

        public readonly Mesh[,] Pine = new Mesh[3, 3];
        public readonly Mesh[,] PrunedPine = new Mesh[3, 3];
        public readonly Mesh[,] Forest = new Mesh[3, 2];
        public readonly Mesh[,] Rock = new Mesh[LakeArt.RockVariants, 2];
        public readonly Mesh[,] Reed = new Mesh[ReedVariants, 2];
        public readonly Mesh[] Grass = new Mesh[2];

        public void Destroy()
        {
            foreach (var mesh in Pine)
            {
                MeshUpload.Destroy(mesh);
            }

            foreach (var mesh in PrunedPine)
            {
                MeshUpload.Destroy(mesh);
            }

            foreach (var mesh in Forest)
            {
                MeshUpload.Destroy(mesh);
            }

            foreach (var mesh in Rock)
            {
                MeshUpload.Destroy(mesh);
            }

            foreach (var mesh in Reed)
            {
                MeshUpload.Destroy(mesh);
            }

            MeshUpload.Destroy(Grass[0]);
            MeshUpload.Destroy(Grass[1]);
        }
    }

    // Los objetos del legacy (06a-escena-lago/02-tech-design §3.6 "Objetos del legacy"; AC-SCN-15-02 a -06, AC-SCN-14-07; DV-62, DN6a-24):
    // la lista visible de ShoreSegments que trae el cuadro (z en [d + 1; d + 400], sin banda propia), con la colocación de SegmentStore.
    // Los pinos instanciados por (poda, forma, LOD) con su paleta (semilla mod 3), su viento y su fundido (aparición 400 → 360 m y
    // tramado de 2 a 1 m: MaterialArt.InstanceFade; el cambio de LOD complementario en el 10 % del límite con Bayer fijo); la cabaña y
    // el muelle de cada segmento con sus mallas propias (la ventana con el material encendido y el nivel de su luz: NightLights, DV-60).
    // Sin asignaciones: los lotes y los arreglos se reservan al armar.
    public sealed class ShoreStreamer
    {
        private const int PineBatches = 4;

        private SegmentStore _store;
        private readonly ShoreMeshes _meshes;
        private readonly Material _foliage;
        private readonly Material _prop;
        private readonly Material _window;
        private readonly Camera _camera;
        private readonly int _layer;
        private readonly InstanceBatchSet[,,] _pines = new InstanceBatchSet[2, 3, 3];
        private readonly InstanceBatch[,] _cabins = new InstanceBatch[SegmentStore.Slots, 2];
        private readonly InstanceBatch[,] _windows = new InstanceBatch[SegmentStore.Slots, 2];
        private readonly InstanceBatch[,] _piers = new InstanceBatch[SegmentStore.Slots, 2];
        private readonly Bounds _bounds = new Bounds(new Vector3(0, 10, 200), new Vector3(400, 60, 1000));

        // Los lotes de los pinos (forma, LOD; sin podar): las coníferas de la pradera de FillStreamer van en los mismos (la misma malla y el
        // mismo material: un dibujo menos por forma y LOD).
        public InstanceBatchSet PineSet(int form, int lod) => _pines[0, form, lod];

        public ShoreStreamer(SegmentStore store, ShoreMeshes meshes, Material foliage, Material prop, Material window, Camera camera, int layer)
        {
            _store = store;
            _meshes = meshes;
            _foliage = foliage;
            _prop = prop;
            _window = window;
            _camera = camera;
            _layer = layer;
            for (var pruned = 0; pruned < 2; pruned++)
            {
                for (var form = 0; form < 3; form++)
                {
                    for (var lod = 0; lod < 3; lod++)
                    {
                        _pines[pruned, form, lod] = new InstanceBatchSet(PineBatches, SceneShaderIds.Variant, SceneShaderIds.WindPhase, SceneShaderIds.Fade)
                        {
                            Mesh = pruned == 1 ? meshes.PrunedPine[form, lod] : meshes.Pine[form, lod],
                        };
                    }
                }
            }

            for (var i = 0; i < SegmentStore.Slots; i++)
            {
                for (var lod = 0; lod < 2; lod++)
                {
                    _cabins[i, lod] = new InstanceBatch(-1, -1, SceneShaderIds.Fade, SceneShaderIds.Lit, 1);
                    _windows[i, lod] = new InstanceBatch(-1, -1, SceneShaderIds.Fade, SceneShaderIds.Lit, 1);
                    _piers[i, lod] = new InstanceBatch(-1, -1, SceneShaderIds.Fade, SceneShaderIds.Lit, 1);
                }
            }
        }

        // It. 8: el almacén de otro escenario (el cambio de kit).
        public void Rebind(SegmentStore store)
        {
            _store = store;
            Clear();
        }

        // La escala de los LOD del nivel (Bajo × 0,7).
        public double LodScale { get; set; } = 1;

        // Cuántos LOD proyectan sombra (Alto 2: hasta 120 m, dentro de los 80 m del mapa; Medio 1: los cercanos, §20.1 "props cercanos";
        // Bajo 0: sin mapa). Lo lejano fuera del mapa no se vuelve a dibujar en la pasada de sombras (B6a-05).
        public int ShadowLods { get; set; } = 2;

        public int PinesDrawn { get; private set; }

        public int CabinsDrawn { get; private set; }

        public int PiersDrawn { get; private set; }

        // Para la sonda de colocación (SPL, SceneMarks): el ancla 3D de cada objeto visible, en metros de mundo (z absoluta).
        public bool TryAnchor(in ShoreProp prop, out double x, out double y, out double z)
        {
            x = prop.X;
            z = prop.Z;
            y = 0;
            var seg = _store.Find((long)Math.Floor(prop.Z / ShoreEnvironment.SegmentLength));
            if (seg == null || seg.Stage < SegmentStage.Placed)
            {
                return false;
            }

            var i = IndexOf(seg, prop);
            if (_store.IsKit && i >= 0)
            {
                // It. 8: río, costa y regata apoyan cada objeto en su pie (PropPlacement.FootY, guardado al colocar el segmento).
                y = seg.FootY[i];
            }
            else if (prop.Kind == PropKind.Pine && i >= 0)
            {
                y = seg.FootY[i];
            }
            else if (prop.Kind == PropKind.Cabin && seg.HasCabin)
            {
                y = seg.Cabin.FloorY;
            }

            return true;
        }

        public void Apply(in SceneFrame frame)
        {
            Clear();
            var d = frame.Distance;
            var props = frame.VisibleProps.Items;
            var pines = 0;
            for (var i = 0; i < props.Length; i++)
            {
                var p = props[i];
                var k = (long)Math.Floor(p.Z / ShoreEnvironment.SegmentLength);
                var seg = _store.Find(k);
                if (seg == null || seg.Stage < SegmentStage.PropsMeshed)
                {
                    continue;
                }

                var relZ = p.Z - d;
                var fade = MaterialArt.InstanceFade(relZ);
                if (fade <= 0)
                {
                    continue;
                }

                switch (p.Kind)
                {
                    case PropKind.Pine:
                    {
                        var index = IndexOf(seg, p);
                        if (index < 0)
                        {
                            break;
                        }

                        AddPine(p, seg.FootY[index], seg.Pruned[index], relZ, fade);
                        pines++;
                        break;
                    }

                    case PropKind.Cabin:
                        if (seg.HasCabin)
                        {
                            AddCabin(seg, relZ, fade, frame.PropLight[i]);
                        }

                        break;
                    case PropKind.Pier:
                        if (seg.HasPier)
                        {
                            AddPier(seg, relZ, fade);
                        }

                        break;
                }
            }

            PinesDrawn = pines;
        }

        public void Draw()
        {
            for (var pruned = 0; pruned < 2; pruned++)
            {
                for (var form = 0; form < 3; form++)
                {
                    for (var lod = 0; lod < 3; lod++)
                    {
                        _pines[pruned, form, lod].Draw(_foliage, _camera, _layer, lod < ShadowLods ? ShadowCastingMode.On : ShadowCastingMode.Off, _bounds);
                    }
                }
            }

            var cabins = 0;
            var piers = 0;
            for (var i = 0; i < SegmentStore.Slots; i++)
            {
                var seg = _store.SlotAt(i);
                for (var lod = 0; lod < 2; lod++)
                {
                    if (_cabins[i, lod].Count > 0)
                    {
                        var mesh = lod == 0 ? seg.CabinLod0 : seg.CabinLod1;
                        if (mesh != null)
                        {
                            _cabins[i, lod].Draw(mesh, 0, _prop, _camera, _layer, lod < ShadowLods ? ShadowCastingMode.On : ShadowCastingMode.Off, _bounds);
                            _windows[i, lod].Draw(mesh, 1, _window, _camera, _layer, ShadowCastingMode.Off, _bounds);
                            cabins++;
                        }
                    }

                    if (_piers[i, lod].Count > 0)
                    {
                        var mesh = lod == 0 ? seg.PierLod0 : seg.PierLod1;
                        if (mesh != null)
                        {
                            _piers[i, lod].Draw(mesh, 0, _prop, _camera, _layer, lod < ShadowLods ? ShadowCastingMode.On : ShadowCastingMode.Off, _bounds);
                            piers++;
                        }
                    }
                }
            }

            CabinsDrawn = cabins;
            PiersDrawn = piers;
        }

        private void Clear()
        {
            for (var pruned = 0; pruned < 2; pruned++)
            {
                for (var form = 0; form < 3; form++)
                {
                    for (var lod = 0; lod < 3; lod++)
                    {
                        _pines[pruned, form, lod].Clear();
                    }
                }
            }

            for (var i = 0; i < SegmentStore.Slots; i++)
            {
                for (var lod = 0; lod < 2; lod++)
                {
                    _cabins[i, lod].Clear();
                    _windows[i, lod].Clear();
                    _piers[i, lod].Clear();
                }
            }
        }

        private void AddPine(in ShoreProp p, double footY, bool pruned, double relZ, double fade)
        {
            var form = Core.Scene.Geometry.PineMesh.FormOf(p.Seed);
            var palette = Core.Scene.Geometry.PineMesh.PaletteOf(p.Seed);
            var s = (float)p.Scale;
            var matrix = Matrix4x4.TRS(new Vector3((float)p.X, (float)footY, (float)relZ), Quaternion.Euler(0, (p.Seed % 360 + 360) % 360, 0),
                new Vector3(s, s, s));
            var variant = new Vector4(palette, 1f, 0f, 0f);
            var wind = new Vector4((float)MaterialArt.PineWindPhase(p.X), 1f, 0f, 0f);
            var set = pruned ? 1 : 0;
            var b0 = MaterialArt.TierDistance(MaterialArt.PineLods[0], TierFor());
            var b1 = MaterialArt.TierDistance(MaterialArt.PineLods[1], TierFor());
            // El LOD de la distancia y, cerca de un límite, los dos con el patrón complementario (DN6a-24).
            if (AddLodPair(set, form, 0, 1, b0, relZ, fade, matrix, variant, wind))
            {
                return;
            }

            if (AddLodPair(set, form, 1, 2, b1, relZ, fade, matrix, variant, wind))
            {
                return;
            }

            var lod = relZ < b0 ? 0 : relZ < b1 ? 1 : 2;
            _pines[set, form, lod].Add(matrix, variant, wind, (float)fade, 0);
        }

        private bool AddLodPair(int set, int form, int nearLod, int farLod, double boundary, double relZ, double fade, in Matrix4x4 matrix,
                                in Vector4 variant, in Vector4 wind)
        {
            MaterialArt.LodFadePair(relZ, boundary, out var near, out var far);
            if (near >= 1 || near <= 0)
            {
                return false;
            }

            _pines[set, form, nearLod].Add(matrix, variant, wind, (float)(fade * near), 0);
            _pines[set, form, farLod].Add(matrix, variant, wind, (float)(fade * far), 0);
            return true;
        }

        private void AddCabin(SegmentData seg, double relZ, double fade, double lit)
        {
            var slot = SlotOf(seg);
            if (slot < 0)
            {
                return;
            }

            var cabin = seg.Cabin;
            var matrix = Matrix4x4.Translate(new Vector3((float)cabin.AnchorX, (float)cabin.FloorY, (float)relZ));
            var boundary = MaterialArt.TierDistance(MaterialArt.CabinLods[0], TierFor());
            MaterialArt.LodFadePair(relZ, boundary, out var near, out var far);
            if (near > 0 && near < 1)
            {
                AddProp(_cabins[slot, 0], _windows[slot, 0], matrix, fade * near, lit);
                AddProp(_cabins[slot, 1], _windows[slot, 1], matrix, fade * far, lit);
                return;
            }

            var lod = relZ < boundary ? 0 : 1;
            AddProp(_cabins[slot, lod], _windows[slot, lod], matrix, fade, lit);
        }

        private void AddPier(SegmentData seg, double relZ, double fade)
        {
            var slot = SlotOf(seg);
            if (slot < 0)
            {
                return;
            }

            var matrix = Matrix4x4.Translate(new Vector3((float)seg.Pier.AnchorX, 0, (float)relZ));
            var boundary = MaterialArt.TierDistance(MaterialArt.CabinLods[0], TierFor());
            MaterialArt.LodFadePair(relZ, boundary, out var near, out var far);
            if (near > 0 && near < 1)
            {
                _piers[slot, 0].Add(matrix, Vector4.zero, Vector4.zero, (float)(fade * near), 0);
                _piers[slot, 1].Add(matrix, Vector4.zero, Vector4.zero, (float)(fade * far), 0);
                return;
            }

            _piers[slot, relZ < boundary ? 0 : 1].Add(matrix, Vector4.zero, Vector4.zero, (float)fade, 0);
        }

        private static void AddProp(InstanceBatch body, InstanceBatch window, in Matrix4x4 matrix, double fade, double lit)
        {
            body.Add(matrix, Vector4.zero, Vector4.zero, (float)fade, (float)lit);
            window.Add(matrix, Vector4.zero, Vector4.zero, (float)fade, (float)lit);
        }

        private int SlotOf(SegmentData seg)
        {
            for (var i = 0; i < SegmentStore.Slots; i++)
            {
                if (ReferenceEquals(_store.SlotAt(i), seg))
                {
                    return i;
                }
            }

            return -1;
        }

        private QualityTier TierFor()
        {
            return LodScale < 0.99 ? QualityTier.Low : QualityTier.High;
        }

        private static int IndexOf(SegmentData seg, in ShoreProp p)
        {
            for (var i = 0; i < seg.PropCount; i++)
            {
                var q = seg.Props[i];
                if (q.Kind == p.Kind && q.Z == p.Z && q.X == p.X)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
