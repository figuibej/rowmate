using RowMate.Core.Scene;
using RowMate.Core.Scene.Stroke;
using UnityEngine;
using UnityEngine.Rendering;

namespace RowMate.Scene
{
    // La salpicadura, la corona y las gotas (07-remero-palada/02-tech-design §2.8, §3.4; DT7-05; AC-SCN-08-04, -05): las partículas de
    // StrokeSpray (Core, deterministas, ≤ 128) en UN dibujo instanciado del quad de la escena, orientado a la cámara en el shader Spray
    // (_RmSpray = (tamaño; alfa; tipo; largo del trazo)). Sin ParticleSystem. Arreglos reservados: Apply no asigna.
    public sealed class StrokeVfx
    {
        private readonly InstanceBatch _batch = new InstanceBatch(SceneShaderIds.SprayParams, -1, -1, -1, StrokeSpray.Capacity);
        private readonly Mesh _quad;
        private readonly Material _material;
        private readonly Camera _camera;
        private readonly int _layer;
        private Bounds _bounds = new Bounds(Vector3.zero, Vector3.one * 4000f);

        public StrokeVfx(Mesh quad, Material material, Camera camera, int layer)
        {
            _quad = quad;
            _material = material;
            _camera = camera;
            _layer = layer;
        }

        public Material Material => _material;

        public int Count => _batch.Count;

        public void Apply(in SceneFrame frame)
        {
            _batch.Clear();
            var spray = frame.Rower.Spray;
            if (spray == null)
            {
                return;
            }

            var items = spray.Items;
            var distance = frame.Distance;
            for (var i = 0; i < spray.Count && !_batch.IsFull; i++)
            {
                ref readonly var p = ref items[i];
                var alpha = (float)StrokeSpray.AlphaOf(p);
                if (alpha <= 0)
                {
                    continue;
                }

                var position = new Vector3((float)p.X, (float)p.Y, (float)(p.Z - distance));
                var streak = p.Kind == 1 ? (float)p.Vy : (p.Kind == 2 ? (float)StrokeSpray.DripStrokeLength : 0f);
                _batch.Add(Matrix4x4.Translate(position), new Vector4((float)p.Size, alpha, p.Kind, streak), default, 0, 0);
            }
        }

        public void Draw()
        {
            _batch.Draw(_quad, 0, _material, _camera, _layer, ShadowCastingMode.Off, _bounds);
        }
    }
}
