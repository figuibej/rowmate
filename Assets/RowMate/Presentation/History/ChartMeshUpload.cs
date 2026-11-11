using System;
using RowMate.Core.History;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace RowMate.Presentation.History
{
    // It. 15 (#34, AC-D15-04; QA-V15-01; 15-deuda-pc/02-tech-design, "Cambios por verificación"): la malla grande del gráfico SIN basura por
    // cuadro. Lo que midió el Profiler con las pilas (AllocationProfile + ProfileAllocations.Dump):
    // - MeshGenerationContext.Allocate saca un MeshWriteData de un pool estático de UI Toolkit que crece con cada trozo de cada dibujo
    //   (≈ 2,5 por dibujo en la entrada: 100 asignaciones de 104 B en 42 dibujos), y su asignador temporal suma páginas;
    // - una malla que cambia de tamaño en cada cuadro (la curva que se revela, el fundido cruzado con dos curvas) obliga a UIR a pedir otro
    //   bloque de GPU en cada cuadro (UIRenderDevice.Allocate → BestFitAllocator/BlockPool/new Page) en lugar de actualizarlo en su lugar.
    // Acá: un par de NativeArray propios (persistentes, del tamaño de la capacidad del ChartMeshBuffer, que crece solo al preparar) que se
    // entregan con MeshGenerationContext.DrawMesh (sin MeshWriteData ni asignador temporal) SIEMPRE con el mismo tamaño: lo que sobra va
    // como triángulos degenerados (índices 0, 0, 0), así UIR actualiza la misma asignación cuadro a cuadro. Los mismos vértices, colores y
    // triángulos (en el sentido del reloj) que el Copy por trozos de antes: los mismos píxeles. Se libera al salir del panel.
    public sealed class ChartMeshUpload : IDisposable
    {
        private NativeArray<Vertex> _vertices;
        private NativeArray<ushort> _indices;
        private int _dirtyIndices;

        public int VertexCapacity => _vertices.IsCreated ? _vertices.Length : 0;

        public int IndexCapacity => _indices.IsCreated ? _indices.Length : 0;

        // Cuántas veces se crearon los arreglos (pruebas: solo al preparar o al volver al panel).
        public int Allocations { get; private set; }

        public void Draw(MeshGenerationContext context, ChartMeshBuffer buffer)
        {
            if (context == null || buffer == null)
            {
                return;
            }

            Ensure(buffer.VertexCapacity, buffer.IndexCapacity);
            var vertexCount = Math.Min(buffer.VertexCount, _vertices.Length);
            var indexCount = Math.Min(buffer.IndexCount - buffer.IndexCount % 3, _indices.Length);
            var xs = buffer.X;
            var ys = buffer.Y;
            var rgba = buffer.Rgba;
            var source = buffer.Indices;
            for (var i = 0; i < vertexCount; i++)
            {
                var color = rgba[i];
                _vertices[i] = new Vertex
                {
                    position = new Vector3(xs[i], ys[i], Vertex.nearZ),
                    tint = new Color32((byte)(color >> 24), (byte)(color >> 16), (byte)(color >> 8), (byte)color),
                };
            }

            // UI Toolkit solo dibuja los triángulos en el sentido del reloj (en pantalla, y hacia abajo): los que vienen al revés (el área de
            // la curva) se dan vuelta acá. Un índice fuera de los vértices de este cuadro no debería venir; si viene, el triángulo se anula.
            for (var k = 0; k < indexCount; k += 3)
            {
                int a = source[k], b = source[k + 1], c = source[k + 2];
                if (a >= vertexCount || b >= vertexCount || c >= vertexCount)
                {
                    a = b = c = 0;
                }
                else
                {
                    var cross = (xs[b] - xs[a]) * (ys[c] - ys[a]) - (ys[b] - ys[a]) * (xs[c] - xs[a]);
                    if (cross < 0)
                    {
                        var swap = b;
                        b = c;
                        c = swap;
                    }
                }

                _indices[k] = (ushort)a;
                _indices[k + 1] = (ushort)b;
                _indices[k + 2] = (ushort)c;
            }

            // Lo que quedó de un cuadro anterior más grande: degenerado.
            for (var k = indexCount; k < _dirtyIndices; k++)
            {
                _indices[k] = 0;
            }

            _dirtyIndices = indexCount;
            context.DrawMesh(_vertices.Slice(), _indices.Slice());
        }

        private void Ensure(int vertices, int indices)
        {
            vertices = Math.Max(3, Math.Min(vertices, ChartMeshBuffer.MaxVertices));
            indices = Math.Max(3, indices - indices % 3);
            if (_vertices.IsCreated && _vertices.Length >= vertices && _indices.Length >= indices)
            {
                return;
            }

            Dispose();
            _vertices = new NativeArray<Vertex>(vertices, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            _indices = new NativeArray<ushort>(indices, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            _dirtyIndices = 0;
            Allocations++;
        }

        public void Dispose()
        {
            if (_vertices.IsCreated)
            {
                _vertices.Dispose();
            }

            if (_indices.IsCreated)
            {
                _indices.Dispose();
            }

            _dirtyIndices = 0;
        }
    }
}
