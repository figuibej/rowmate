using System;
using RowMate.Core.Scene.Geometry;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace RowMate.Scene
{
    // MeshData (Core, arreglos puros) → Mesh de Unity sin GC (06a-escena-lago/02-tech-design §2.14, §4.4): los búferes nativos son
    // persistentes y se reservan con la capacidad más grande que se subió (los trozos de terreno y las cabañas se rehacen mientras se
    // rema: una subida en régimen no asigna memoria administrada). El terreno pide además las normales recalculadas (TerrainChunkMesh
    // escribe (0; 1; 0): 04-design-notes, "Cambios por implementación de WP-D1" §4).
    public sealed class MeshUpload : IDisposable
    {
        private NativeArray<Vector3> _positions;
        private NativeArray<Vector3> _normals;
        private NativeArray<Color> _colors;
        private NativeArray<Vector2> _uv;
        private NativeArray<int> _indices;
        private int _vertexCapacity;
        private int _indexCapacity;
        private bool _live;

        // Cuántos MeshUpload tienen búferes nativos reservados sin su Dispose (QA-V6a-REN-08: un armado cortado no deja ninguno). Solo
        // cuenta: no cambia nada del comportamiento.
        internal static int LiveForTest { get; private set; }

        private const MeshUpdateFlags Flags = MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontNotifyMeshUsers;

        public static Mesh NewMesh(string name)
        {
            var mesh = new Mesh { name = name, hideFlags = HideFlags.HideAndDontSave };
            mesh.MarkDynamic();
            return mesh;
        }

        // Copia la malla. recalculateNormals: el terreno. bounds: null = las de los vértices; si no, las dadas (el cielo y las
        // cordilleras, que el shader pone en otro lado: que el culling nunca las recorte).
        // orient: cada triángulo con el orden que da su cara de adelante del lado de la normal de sus vértices (Unity dibuja la cara de
        // adelante en sentido horario: cross(b − a; c − a) hacia la cámara). doubleSided: además, la cara de atrás (las hojas del pasto
        // y de los juncos son una sola cara y los shaders cortan la de atrás). Ver "Cambios por implementación de WP-B" (B6a-02).
        public void Upload(MeshData data, Mesh mesh, bool recalculateNormals = false, Bounds? bounds = null, bool orient = false, bool doubleSided = false)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            Ensure(data.VertexCount, doubleSided ? data.IndexCount * 2 : data.IndexCount);
            var p = data.Positions;
            var n = data.Normals;
            var c = data.Colors;
            var t = data.Uv;
            for (var i = 0; i < data.VertexCount; i++)
            {
                _positions[i] = new Vector3(p[i * 3], p[i * 3 + 1], p[i * 3 + 2]);
                _normals[i] = new Vector3(n[i * 3], n[i * 3 + 1], n[i * 3 + 2]);
                _colors[i] = new Color(c[i * 4], c[i * 4 + 1], c[i * 4 + 2], c[i * 4 + 3]);
                _uv[i] = new Vector2(t[i * 2], t[i * 2 + 1]);
            }

            var indices = data.Indices;
            for (var i = 0; i < data.IndexCount; i++)
            {
                _indices[i] = indices[i];
            }

            if (orient)
            {
                Orient(data, 0, data.IndexCount);
            }

            var indexCount = data.IndexCount;
            if (doubleSided)
            {
                for (var i = 0; i < data.IndexCount; i += 3)
                {
                    _indices[indexCount + i] = _indices[i];
                    _indices[indexCount + i + 1] = _indices[i + 2];
                    _indices[indexCount + i + 2] = _indices[i + 1];
                }

                indexCount *= 2;
            }

            mesh.Clear(false);
            mesh.indexFormat = data.VertexCount > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            mesh.SetVertices(_positions, 0, data.VertexCount, Flags);
            mesh.SetNormals(_normals, 0, data.VertexCount, Flags);
            mesh.SetColors(_colors, 0, data.VertexCount, Flags);
            mesh.SetUVs(0, _uv, 0, data.VertexCount, Flags);
            mesh.subMeshCount = data.SubmeshCount;
            if (doubleSided && data.SubmeshCount == 1)
            {
                mesh.SetIndices(_indices, 0, indexCount, MeshTopology.Triangles, 0, false);
            }
            else
            {
                for (var s = 0; s < data.SubmeshCount; s++)
                {
                    data.Submesh(s, out var start, out var count);
                    mesh.SetIndices(_indices, start, count, MeshTopology.Triangles, s, false);
                }
            }

            if (recalculateNormals)
            {
                mesh.RecalculateNormals(Flags);
            }

            if (bounds.HasValue)
            {
                mesh.bounds = bounds.Value;
            }
            else
            {
                mesh.RecalculateBounds(Flags);
            }
        }

        private void Orient(MeshData data, int start, int count)
        {
            var p = data.Positions;
            var n = data.Normals;
            for (var t = start; t < start + count; t += 3)
            {
                int a = _indices[t], b = _indices[t + 1], c = _indices[t + 2];
                float ux = p[b * 3] - p[a * 3], uy = p[b * 3 + 1] - p[a * 3 + 1], uz = p[b * 3 + 2] - p[a * 3 + 2];
                float vx = p[c * 3] - p[a * 3], vy = p[c * 3 + 1] - p[a * 3 + 1], vz = p[c * 3 + 2] - p[a * 3 + 2];
                var cx = uy * vz - uz * vy;
                var cy = uz * vx - ux * vz;
                var cz = ux * vy - uy * vx;
                var nx = n[a * 3] + n[b * 3] + n[c * 3];
                var ny = n[a * 3 + 1] + n[b * 3 + 1] + n[c * 3 + 1];
                var nz = n[a * 3 + 2] + n[b * 3 + 2] + n[c * 3 + 2];
                if (cx * nx + cy * ny + cz * nz < 0)
                {
                    _indices[t + 1] = c;
                    _indices[t + 2] = b;
                }
            }
        }

        private void Ensure(int vertices, int indices)
        {
            if (!_live)
            {
                _live = true;
                LiveForTest++;
            }

            if (vertices > _vertexCapacity)
            {
                Dispose(ref _positions);
                Dispose(ref _normals);
                Dispose(ref _colors);
                Dispose(ref _uv);
                _vertexCapacity = Math.Max(vertices, 256);
                _positions = new NativeArray<Vector3>(_vertexCapacity, Allocator.Persistent);
                _normals = new NativeArray<Vector3>(_vertexCapacity, Allocator.Persistent);
                _colors = new NativeArray<Color>(_vertexCapacity, Allocator.Persistent);
                _uv = new NativeArray<Vector2>(_vertexCapacity, Allocator.Persistent);
            }

            if (indices > _indexCapacity)
            {
                Dispose(ref _indices);
                _indexCapacity = Math.Max(indices, 768);
                _indices = new NativeArray<int>(_indexCapacity, Allocator.Persistent);
            }
        }

        private static void Dispose<T>(ref NativeArray<T> array) where T : struct
        {
            if (array.IsCreated)
            {
                array.Dispose();
            }

            array = default;
        }

        public void Dispose()
        {
            Dispose(ref _positions);
            Dispose(ref _normals);
            Dispose(ref _colors);
            Dispose(ref _uv);
            Dispose(ref _indices);
            _vertexCapacity = 0;
            _indexCapacity = 0;
            if (_live)
            {
                _live = false;
                LiveForTest--;
            }
        }

        // Un quad de 0 a 1 en la UV (el cielo, los degradés, las estrellas, las nubes y la sombra del bote: el shader arma la posición).
        public static Mesh Quad(string name, float boundsSize)
        {
            var mesh = new Mesh
            {
                name = name,
                hideFlags = HideFlags.HideAndDontSave,
                vertices = new[] { new Vector3(-0.5f, -0.5f, 0), new Vector3(0.5f, -0.5f, 0), new Vector3(0.5f, 0.5f, 0), new Vector3(-0.5f, 0.5f, 0) },
                uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) },
                normals = new[] { Vector3.back, Vector3.back, Vector3.back, Vector3.back },
                triangles = new[] { 0, 2, 1, 0, 3, 2 },
            };
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * boundsSize);
            return mesh;
        }

        public static void Destroy(Object? obj)
        {
            if (obj == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(obj);
            }
            else
            {
                Object.DestroyImmediate(obj);
            }
        }
    }
}
