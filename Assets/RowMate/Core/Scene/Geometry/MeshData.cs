using System;

namespace RowMate.Core.Scene.Geometry
{
    // Una malla en arreglos puros (06a-escena-lago/02-tech-design §2.14): posiciones, normales, colores de vértice (r = oclusión,
    // g = peso del viento, b = grada o material, a = libre), UV y triángulos, en arreglos que se reservan una vez con su
    // capacidad; las submallas (la ventana de la cabaña se enciende aparte) son tramos del arreglo de índices. Unity solo la copia
    // a un Mesh (búferes nativos persistentes: sin GC). Las coordenadas son locales, en metros, en float (lo que usa la GPU).
    public sealed class MeshData
    {
        private readonly int[] _submeshStart = new int[4];
        private readonly int[] _submeshCount = new int[4];

        public MeshData(int vertexCapacity, int indexCapacity)
        {
            if (vertexCapacity <= 0 || indexCapacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(vertexCapacity));
            }

            Positions = new float[vertexCapacity * 3];
            Normals = new float[vertexCapacity * 3];
            Colors = new float[vertexCapacity * 4];
            Uv = new float[vertexCapacity * 2];
            Indices = new int[indexCapacity];
        }

        public float[] Positions { get; }
        public float[] Normals { get; }
        public float[] Colors { get; }
        public float[] Uv { get; }
        public int[] Indices { get; }

        public int VertexCount { get; private set; }
        public int IndexCount { get; private set; }
        public int TriangleCount => IndexCount / 3;
        public int SubmeshCount { get; private set; } = 1;

        public int VertexCapacity => Positions.Length / 3;

        public void Clear()
        {
            VertexCount = 0;
            IndexCount = 0;
            SubmeshCount = 1;
            _submeshStart[0] = 0;
            _submeshCount[0] = 0;
        }

        // Empieza una submalla nueva (los índices que siguen van a ella).
        public void BeginSubmesh()
        {
            if (SubmeshCount >= _submeshStart.Length)
            {
                throw new InvalidOperationException("MeshData: a lo sumo " + _submeshStart.Length + " submallas.");
            }

            _submeshStart[SubmeshCount] = IndexCount;
            _submeshCount[SubmeshCount] = 0;
            SubmeshCount++;
        }

        public void Submesh(int i, out int start, out int count)
        {
            start = _submeshStart[i];
            count = _submeshCount[i];
        }

        public int AddVertex(double x, double y, double z, double nx, double ny, double nz, double r, double g, double b, double a, double u, double v)
        {
            if (VertexCount >= VertexCapacity)
            {
                throw new InvalidOperationException("MeshData: se superó la capacidad de " + VertexCapacity + " vértices.");
            }

            var i = VertexCount++;
            Positions[i * 3] = (float)x;
            Positions[i * 3 + 1] = (float)y;
            Positions[i * 3 + 2] = (float)z;
            var len = Math.Sqrt(nx * nx + ny * ny + nz * nz);
            if (len > 1e-12)
            {
                nx /= len;
                ny /= len;
                nz /= len;
            }

            Normals[i * 3] = (float)nx;
            Normals[i * 3 + 1] = (float)ny;
            Normals[i * 3 + 2] = (float)nz;
            Colors[i * 4] = (float)r;
            Colors[i * 4 + 1] = (float)g;
            Colors[i * 4 + 2] = (float)b;
            Colors[i * 4 + 3] = (float)a;
            Uv[i * 2] = (float)u;
            Uv[i * 2 + 1] = (float)v;
            return i;
        }

        public void AddTriangle(int a, int b, int c)
        {
            if (IndexCount + 3 > Indices.Length)
            {
                throw new InvalidOperationException("MeshData: se superó la capacidad de " + Indices.Length + " índices.");
            }

            Indices[IndexCount++] = a;
            Indices[IndexCount++] = b;
            Indices[IndexCount++] = c;
            _submeshCount[SubmeshCount - 1] += 3;
        }

        // La caja que contiene a los vértices.
        public void Bounds(out double minX, out double minY, out double minZ, out double maxX, out double maxY, out double maxZ)
        {
            minX = minY = minZ = double.MaxValue;
            maxX = maxY = maxZ = double.MinValue;
            for (var i = 0; i < VertexCount; i++)
            {
                double x = Positions[i * 3], y = Positions[i * 3 + 1], z = Positions[i * 3 + 2];
                minX = Math.Min(minX, x);
                minY = Math.Min(minY, y);
                minZ = Math.Min(minZ, z);
                maxX = Math.Max(maxX, x);
                maxY = Math.Max(maxY, y);
                maxZ = Math.Max(maxZ, z);
            }
        }

        public double X(int i) => Positions[i * 3];
        public double Y(int i) => Positions[i * 3 + 1];
        public double Z(int i) => Positions[i * 3 + 2];
    }
}
