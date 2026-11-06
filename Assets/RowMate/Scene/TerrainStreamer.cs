using UnityEngine;

namespace RowMate.Scene
{
    // El terreno de la orilla (06a-escena-lago/02-tech-design §2.13, §2.14, §3.6 "Terreno"; DN6a-11; DT6a-18): un trozo por orilla y por
    // segmento desde d − 25 m hasta 450 m (las mallas las arma SegmentStore por tramos) y el trozo lejano hasta 1,5 km, cada uno en su
    // objeto con el material de terreno compartido (sin bloque de propiedades: entra en el SRP Batcher). Cada cuadro solo los ubica en
    // la cinta (z₀ − d, en double) y prende los que están listos; un trozo cercano que ya cubre el lejano no se dibuja (sin superficies
    // dobles en la costura). Sin asignaciones.
    public sealed class TerrainStreamer
    {
        private readonly SegmentStore _store;
        private readonly MeshRenderer[] _near = new MeshRenderer[SegmentStore.Slots * 2];
        private readonly MeshFilter[] _nearFilters = new MeshFilter[SegmentStore.Slots * 2];
        private readonly MeshRenderer[] _far = new MeshRenderer[2];

        public TerrainStreamer(Transform parent, int layer, SegmentStore store, Material material)
        {
            _store = store;
            var root = new GameObject("scene-terrain") { hideFlags = HideFlags.DontSave, layer = layer };
            root.transform.SetParent(parent, false);
            for (var i = 0; i < _near.Length; i++)
            {
                _near[i] = SceneObjects.Create("terrain-" + i, root.transform, layer, null!, material, false, true);
                _nearFilters[i] = _near[i].GetComponent<MeshFilter>();
                _near[i].enabled = false;
            }

            for (var side = 0; side < 2; side++)
            {
                _far[side] = SceneObjects.Create(side == 0 ? "terrain-far-left" : "terrain-far-right", root.transform, layer, store.FarMeshes[side], material, false, true);
                _far[side].enabled = false;
            }
        }

        public int VisibleChunks { get; private set; }

        public void Apply(double distance)
        {
            var visible = 0;
            var farK = _store.FarStartK;
            for (var i = 0; i < SegmentStore.Slots; i++)
            {
                var seg = _store.SlotAt(i);
                for (var side = 0; side < 2; side++)
                {
                    var renderer = _near[i * 2 + side];
                    var show = seg.K != long.MinValue && seg.TerrainReady(side) && (farK == long.MinValue || seg.K < farK);
                    if (show)
                    {
                        var mesh = seg.Terrain[side];
                        if (!ReferenceEquals(_nearFilters[i * 2 + side].sharedMesh, mesh))
                        {
                            _nearFilters[i * 2 + side].sharedMesh = mesh;
                        }

                        renderer.transform.localPosition = new Vector3(0, 0, (float)(seg.Z0 - distance));
                        visible++;
                    }

                    if (renderer.enabled != show)
                    {
                        renderer.enabled = show;
                    }
                }
            }

            for (var side = 0; side < 2; side++)
            {
                var show = _store.FarReady;
                if (show)
                {
                    _far[side].transform.localPosition = new Vector3(0, 0, (float)(_store.FarStartZ - distance));
                    visible++;
                }

                if (_far[side].enabled != show)
                {
                    _far[side].enabled = show;
                }
            }

            VisibleChunks = visible;
        }

        public void Hide()
        {
            for (var i = 0; i < _near.Length; i++)
            {
                _near[i].enabled = false;
            }

            _far[0].enabled = false;
            _far[1].enabled = false;
        }
    }
}
