using System;
using RowMate.Core.Scene;
using RowMate.Core.Scene.Art;
using RowMate.Core.Scene.Geometry;
using UnityEngine;
using UnityEngine.Rendering;

namespace RowMate.Scene
{
    // Los objetos de río, costa y regata (It. 8, WP-B1): la malla de cada segmento (KitSegmentMesh, armada por SegmentStore) en la
    // cinta (z₀ − d), con el LOD cercano hasta KitSegmentMesh.NearRange; la submalla 1 (lo que se enciende) con el material de la
    // ventana de noche y el nivel de luz más alto de los objetos del segmento (NightLights, DV-60), o con el de Prop de día. El
    // fundido de aparición (400 → 360 m) es por segmento. Respaldo del instanciado con float4 de §3.3 mientras falten Facade, Glow y
    // MarkerText (WP-D1). Sin asignar: un lote por segmento y LOD, reservado al armar.
    public sealed class KitPropRenderer
    {
        private SegmentStore _store;
        private readonly Material _prop;
        private readonly Material _window;
        private readonly Camera _camera;
        private readonly int _layer;
        private readonly InstanceBatch[,] _body = new InstanceBatch[SegmentStore.Slots, KitSegmentMesh.Lods];
        private readonly InstanceBatch[,] _glow = new InstanceBatch[SegmentStore.Slots, KitSegmentMesh.Lods];
        private readonly bool[] _lit = new bool[SegmentStore.Slots];
        private readonly Bounds _bounds = new Bounds(new Vector3(0, 20, 250), new Vector3(400, 80, 1000));

        public KitPropRenderer(SegmentStore store, Material prop, Material window, Camera camera, int layer)
        {
            _store = store;
            _prop = prop;
            _window = window;
            _camera = camera;
            _layer = layer;
            for (var i = 0; i < SegmentStore.Slots; i++)
            {
                for (var lod = 0; lod < KitSegmentMesh.Lods; lod++)
                {
                    _body[i, lod] = new InstanceBatch(-1, -1, SceneShaderIds.Fade, SceneShaderIds.Lit, 1);
                    _glow[i, lod] = new InstanceBatch(-1, -1, SceneShaderIds.Fade, SceneShaderIds.Lit, 1);
                }
            }
        }

        // Proyecta sombra el LOD cercano (con sombras en el nivel).
        public bool Shadows { get; set; } = true;

        public int SegmentsDrawn { get; private set; }

        // La miniatura (ThumbnailRule, relZ < 150): los segmentos que empiezan más lejos no se dibujan (el corte es por segmento).
        public double MaxRelZ { get; set; } = double.PositiveInfinity;

        public void Rebind(SegmentStore store)
        {
            _store = store;
            Clear();
        }

        public void Apply(in SceneFrame frame)
        {
            Clear();
            if (!_store.IsKit)
            {
                SegmentsDrawn = 0;
                return;
            }

            var d = frame.Distance;
            var drawn = 0;
            var props = frame.VisibleProps.Items;
            for (var i = 0; i < SegmentStore.Slots; i++)
            {
                var seg = _store.SlotAt(i);
                if (seg.K == long.MinValue || seg.Stage < SegmentStage.PropsMeshed)
                {
                    continue;
                }

                var relZ = seg.Z0 - d;
                // El segmento entero detrás de la cámara (más de 15 m: el puente que sigue, DT8-08) o todavía en la niebla: nada.
                if (relZ + ShoreEnvironment.SegmentLength < -RiverArt.TrailingBehind)
                {
                    continue;
                }

                if (relZ >= MaxRelZ)
                {
                    continue;
                }

                var fade = MaterialArt.InstanceFade(Math.Max(relZ, 3.0));
                if (fade <= 0)
                {
                    continue;
                }

                // La luz del segmento: la más alta de sus objetos visibles (NightLights por objeto, con su fundido).
                var level = 0.0;
                var zEnd = seg.Z0 + ShoreEnvironment.SegmentLength;
                for (var p = 0; p < props.Length; p++)
                {
                    if (props[p].Z >= seg.Z0 && props[p].Z < zEnd && frame.PropLight[p] > level)
                    {
                        level = frame.PropLight[p];
                    }
                }

                var lod = relZ < KitSegmentMesh.NearRange ? 0 : 1;
                var matrix = Matrix4x4.Translate(new Vector3(0, 0, (float)relZ));
                _body[i, lod].Add(matrix, Vector4.zero, Vector4.zero, (float)fade, 0);
                _glow[i, lod].Add(matrix, Vector4.zero, Vector4.zero, (float)fade, (float)level);
                _lit[i] = level > 1e-3;
                drawn++;
            }

            SegmentsDrawn = drawn;
        }

        public void Draw()
        {
            if (!_store.IsKit)
            {
                return;
            }

            for (var i = 0; i < SegmentStore.Slots; i++)
            {
                var seg = _store.SlotAt(i);
                for (var lod = 0; lod < KitSegmentMesh.Lods; lod++)
                {
                    if (_body[i, lod].Count == 0)
                    {
                        continue;
                    }

                    var mesh = seg.Kit[lod];
                    if (mesh == null)
                    {
                        continue;
                    }

                    _body[i, lod].Draw(mesh, 0, _prop, _camera, _layer, Shadows && lod == 0 ? ShadowCastingMode.On : ShadowCastingMode.Off, _bounds);
                    if (mesh.subMeshCount > 1 && mesh.GetIndexCount(1) > 0)
                    {
                        _glow[i, lod].Draw(mesh, 1, _lit[i] ? _window : _prop, _camera, _layer, ShadowCastingMode.Off, _bounds);
                    }
                }
            }
        }

        private void Clear()
        {
            for (var i = 0; i < SegmentStore.Slots; i++)
            {
                _lit[i] = false;
                for (var lod = 0; lod < KitSegmentMesh.Lods; lod++)
                {
                    _body[i, lod].Clear();
                    _glow[i, lod].Clear();
                }
            }
        }
    }
}
