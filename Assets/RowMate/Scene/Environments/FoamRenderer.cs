using System;
using RowMate.Core.Scene;
using RowMate.Core.Scene.Art;
using RowMate.Core.Scene.Geometry;
using UnityEngine;
using UnityEngine.Rendering;

namespace RowMate.Scene
{
    // La espuma de la orilla de la costa (It. 8, WP-D1; 08-entornos-selector/02-tech-design §3.3; 00-arte-entornos §3.3, §3.4): la tira
    // de FoamStripMesh (una, armada al crear) por cada segmento armado de la costa dentro de los 400 m, al pie del acantilado (de
    // x −11,4 a −13,0) o en la línea del agua de la playa (k mod 5 = 2, de −8,0 a −9,6); la que pasa de una a otra se apaga en sus 6 m
    // del borde. Un dibujo instanciado del shader Foam, sin sombras. Fuera de la costa no dibuja. Sin asignar en régimen.
    public sealed class FoamRenderer
    {
        private static readonly Bounds DrawBounds = new Bounds(new Vector3(0, 0, 250), new Vector3(400, 20, 1000));

        private SegmentStore _store;
        private readonly Mesh _strip;
        private readonly Material _material;
        private readonly Camera _camera;
        private readonly int _layer;
        private readonly InstanceBatch _batch = new InstanceBatch(SceneShaderIds.FoamInstance, -1, -1, -1, SegmentStore.Slots);

        // strip: la de BuildStrip (la destruye el dueño del mundo).
        public FoamRenderer(SegmentStore store, Mesh strip, Material material, Camera camera, int layer)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _strip = strip ?? throw new ArgumentNullException(nameof(strip));
            _material = material ?? throw new ArgumentNullException(nameof(material));
            _camera = camera;
            _layer = layer;
        }

        // La tira de un segmento, de 1 m de ancho (FoamRenderer la escala), una vez al armar el mundo.
        public static Mesh BuildStrip()
        {
            var data = new MeshData(FoamStripMesh.VertexCapacity, FoamStripMesh.IndexCapacity);
            FoamStripMesh.Build(1.0, data);
            var strip = MeshUpload.NewMesh("scene-foam-strip");
            using (var upload = new MeshUpload())
            {
                upload.Upload(data, strip, false, DrawBounds, true);
            }

            return strip;
        }

        public bool Enabled { get; set; } = true;

        // La miniatura (ThumbnailRule, relZ < 150): como los objetos del kit, por segmento.
        public double MaxRelZ { get; set; } = double.PositiveInfinity;

        public int Count => _batch.Count;

        public void Rebind(SegmentStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _batch.Clear();
        }

        public void Apply(in SceneFrame frame)
        {
            _batch.Clear();
            if (!Enabled || _store.Environment != EnvironmentId.Coast)
            {
                return;
            }

            var d = frame.Distance;
            for (var i = 0; i < SegmentStore.Slots; i++)
            {
                var seg = _store.SlotAt(i);
                if (seg.K == long.MinValue || seg.Stage < SegmentStage.PropsMeshed)
                {
                    continue;
                }

                var relZ = seg.Z0 - d;
                if (relZ + ShoreEnvironment.SegmentLength < 0 || relZ >= MaxRelZ)
                {
                    continue;
                }

                var fade = MaterialArt.InstanceFade(Math.Max(relZ, 3.0));
                if (fade <= 0)
                {
                    continue;
                }

                var beach = IsBeach(seg.K);
                double code;
                double x0, width;
                if (beach)
                {
                    code = 1 + 2 + 4;
                    x0 = CoastArt.FoamBeachX;
                    width = CoastArt.FoamBeachWidth;
                }
                else
                {
                    code = (IsBeach(seg.K - 1) ? 2 : 0) + (IsBeach(seg.K + 1) ? 4 : 0);
                    x0 = CoastArt.FoamCliffX;
                    width = CoastArt.FoamCliffWidth;
                }

                // El ruido queda quieto en el mundo: z₀ mod 256 en double.
                var zPhase = seg.Z0 - Math.Floor(seg.Z0 / 256.0) * 256.0;
                var matrix = Matrix4x4.TRS(new Vector3((float)x0, (float)CoastArt.FoamLift, (float)relZ), Quaternion.identity, new Vector3((float)width, 1, 1));
                _batch.Add(matrix, new Vector4((float)code, (float)zPhase, (float)width, (float)fade), Vector4.zero, 0, 0);
            }
        }

        public static bool IsBeach(long k)
        {
            var m = k % 5;
            return (m < 0 ? m + 5 : m) == 2;
        }

        public void Draw()
        {
            if (Enabled)
            {
                _batch.Draw(_strip, 0, _material, _camera, _layer, ShadowCastingMode.Off, DrawBounds);
            }
        }
    }
}
