using System;
using RowMate.Core.Scene;
using RowMate.Core.Scene.Geometry;
using UnityEngine;
using UnityEngine.Rendering;

namespace RowMate.Scene
{
    // Las 5 gaviotas de la costa (QA-V8-01; AC-SCN-19-04, -20-06; 00-arte-entornos §3.4; slot prop.seagull en Procedural): la malla de
    // SeagullFlock (Core: las 5 copias puestas por cuadro con el recorrido del legacy, SeagullFlight) en un solo dibujo con el material
    // Prop (sin vida en el uv), con la opacidad del legacy 0,5 + 0,4·ambiente por el tramado (_RmFade). Sin sombras (00-arte-entornos
    // §1: finas, titilan). Fuera de la costa, en la miniatura o sin gaviotas a la vista, no dibuja. Sin asignar en régimen: los arreglos y
    // la malla se reservan al crear y por cuadro solo se copian las posiciones.
    public sealed class SeagullRenderer
    {
        private static readonly Bounds DrawBounds = new Bounds(new Vector3(0, 40, 0), new Vector3(400, 120, 400));

        private readonly SeagullFlock _flock = new SeagullFlock();
        private readonly Vector3[] _vertices;
        private readonly Mesh _mesh;
        private readonly Material _material;
        private readonly Camera _camera;
        private readonly int _layer;
        private readonly InstanceBatch _batch = new InstanceBatch(-1, -1, SceneShaderIds.Fade, SceneShaderIds.Lit, 1);

        public SeagullRenderer(Material prop, Camera camera, int layer)
        {
            _material = prop ?? throw new ArgumentNullException(nameof(prop));
            _camera = camera;
            _layer = layer;
            var n = _flock.VertexCount;
            _vertices = new Vector3[n];
            var normals = new Vector3[n];
            var colors = new Color[n];
            var uv = new Vector2[n];
            for (var v = 0; v < n; v++)
            {
                normals[v] = new Vector3(_flock.Normals[v * 3], _flock.Normals[v * 3 + 1], _flock.Normals[v * 3 + 2]);
                colors[v] = new Color(_flock.Colors[v * 4], _flock.Colors[v * 4 + 1], _flock.Colors[v * 4 + 2], _flock.Colors[v * 4 + 3]);
            }

            _mesh = MeshUpload.NewMesh("scene-seagulls");
            _mesh.vertices = _vertices;
            _mesh.normals = normals;
            _mesh.colors = colors;
            _mesh.uv = uv;
            _mesh.triangles = _flock.Indices;
            _mesh.bounds = DrawBounds;
        }

        public bool Enabled { get; set; } = true;

        // Cuántas gaviotas se ven en este cuadro (pruebas y presupuesto).
        public int Count => _batch.Count == 0 ? 0 : _flock.Visible;

        // La malla (la destruye el dueño del mundo: SceneWorld.Own).
        public Mesh Mesh => _mesh;

        // Las posiciones del último cuadro (pruebas).
        public SeagullFlock Flock => _flock;

        public void Apply(in SceneFrame frame)
        {
            _batch.Clear();
            if (!Enabled || frame.Environment != EnvironmentId.Coast || frame.SeagullOpacity <= 0 || frame.Seagulls == null)
            {
                return;
            }

            _flock.Pose(frame.Seagulls, frame.Pose);
            if (_flock.Visible == 0)
            {
                return;
            }

            var p = _flock.Positions;
            for (var v = 0; v < _vertices.Length; v++)
            {
                _vertices[v] = new Vector3(p[v * 3], p[v * 3 + 1], p[v * 3 + 2]);
            }

            _mesh.SetVertices(_vertices, 0, _vertices.Length, MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontValidateIndices);
            _batch.Add(Matrix4x4.identity, Vector4.zero, Vector4.zero, (float)Math.Min(1, frame.SeagullOpacity), 0);
        }

        public void Draw()
        {
            _batch.Draw(_mesh, 0, _material, _camera, _layer, ShadowCastingMode.Off, DrawBounds);
        }
    }
}
