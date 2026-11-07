using System;
using RowMate.Core.Text;

namespace RowMate.Core.Scene.Geometry
{
    // Una malla con skinning en arreglos puros (It. 7, 02-tech-design §2.13): la MeshData más dos huesos por vértice (índices en el
    // esqueleto y pesos que suman 1). Unity la copia a un Mesh con sus BoneWeight y calcula las bindposes del esqueleto en reposo.
    public sealed class SkinnedMeshData
    {
        public SkinnedMeshData(int vertexCapacity, int indexCapacity)
        {
            Mesh = new MeshData(vertexCapacity, indexCapacity);
            Bone0 = new int[vertexCapacity];
            Bone1 = new int[vertexCapacity];
            Weight0 = new float[vertexCapacity];
            Weight1 = new float[vertexCapacity];
        }

        public MeshData Mesh { get; }
        public int[] Bone0 { get; }
        public int[] Bone1 { get; }
        public float[] Weight0 { get; }
        public float[] Weight1 { get; }

        public int VertexCount => Mesh.VertexCount;

        public void Clear()
        {
            Mesh.Clear();
        }

        // Un vértice con sus dos huesos: weight0 para bone0 y el resto para bone1.
        public int AddVertex(double x, double y, double z, double nx, double ny, double nz, double red, double u, double v, int bone0, int bone1,
                             double weight0)
        {
            var i = Mesh.AddVertex(x, y, z, nx, ny, nz, red, 0, 0, 1, u, v);
            var w = weight0 < 0 ? 0 : (weight0 > 1 ? 1 : weight0);
            if (bone1 == bone0 || w >= 1)
            {
                Bone0[i] = bone0;
                Bone1[i] = bone0;
                Weight0[i] = 1;
                Weight1[i] = 0;
            }
            else
            {
                Bone0[i] = bone0;
                Bone1[i] = bone1;
                Weight0[i] = (float)w;
                Weight1[i] = 1 - (float)w;
            }

            return i;
        }

        public void AddTriangle(int a, int b, int c)
        {
            Mesh.AddTriangle(a, b, c);
        }

        public static int RegionOf(float red)
        {
            return (int)DartMath.Round(red * 8.0);
        }
    }
}
