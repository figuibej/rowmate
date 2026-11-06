using UnityEngine;
using UnityEngine.Rendering;

namespace RowMate.Scene
{
    // Un grupo de instancias de una malla y un material para Graphics.RenderMeshInstanced (06a-escena-lago/02-tech-design §3.6, §4.4;
    // §7.5 "Instanciado"): las matrices y las propiedades por instancia en arreglos reservados una vez (hasta Capacity por dibujo), con
    // su MaterialPropertyBlock propio (el bloque se copia al pedir el dibujo; uno por lote para no mezclar). Hasta dos vectores y dos
    // números por instancia, con los ids del contrato (SceneShaderIds; −1 = no se usa). Sin asignar en régimen.
    public sealed class InstanceBatch
    {
        // Debajo del tope de las constantes de instanciado de GLES 3 y D3D11 para matrices + 4 propiedades.
        public const int DefaultCapacity = 250;

        private readonly int _v0;
        private readonly int _v1;
        private readonly int _f0;
        private readonly int _f1;
        private readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();

        public InstanceBatch(int vector0 = -1, int vector1 = -1, int float0 = -1, int float1 = -1, int capacity = DefaultCapacity)
        {
            Capacity = capacity;
            _v0 = vector0;
            _v1 = vector1;
            _f0 = float0;
            _f1 = float1;
            Matrices = new Matrix4x4[capacity];
            V0 = new Vector4[capacity];
            V1 = new Vector4[capacity];
            F0 = new float[capacity];
            F1 = new float[capacity];
        }

        public int Capacity { get; }

        public Matrix4x4[] Matrices { get; }

        public Vector4[] V0 { get; }

        public Vector4[] V1 { get; }

        public float[] F0 { get; }

        public float[] F1 { get; }

        public int Count { get; private set; }

        public bool IsFull => Count >= Capacity;

        public void Clear()
        {
            Count = 0;
        }

        public void Add(in Matrix4x4 matrix, in Vector4 v0, in Vector4 v1, float f0, float f1)
        {
            if (Count >= Capacity)
            {
                return;
            }

            var i = Count++;
            Matrices[i] = matrix;
            V0[i] = v0;
            V1[i] = v1;
            F0[i] = f0;
            F1[i] = f1;
        }

        // Pide el dibujo para la cámara de la escena (que se dibuja a pedido en este mismo cuadro).
        public void Draw(Mesh mesh, int submesh, Material material, Camera camera, int layer, ShadowCastingMode shadows, in Bounds bounds)
        {
            if (Count == 0 || mesh == null || material == null)
            {
                return;
            }

            if (_v0 >= 0)
            {
                _block.SetVectorArray(_v0, V0);
            }

            if (_v1 >= 0)
            {
                _block.SetVectorArray(_v1, V1);
            }

            if (_f0 >= 0)
            {
                _block.SetFloatArray(_f0, F0);
            }

            if (_f1 >= 0)
            {
                _block.SetFloatArray(_f1, F1);
            }

            var rp = new RenderParams(material)
            {
                camera = camera,
                layer = layer,
                matProps = _block,
                shadowCastingMode = shadows,
                receiveShadows = true,
                worldBounds = bounds,
            };
            Graphics.RenderMeshInstanced(rp, mesh, submesh, Matrices, Count);
        }
    }

    // Varios lotes de la misma malla y material (más de Capacity instancias): se llenan en orden y se dibujan todos.
    public sealed class InstanceBatchSet
    {
        private readonly InstanceBatch[] _batches;
        private int _current;

        public InstanceBatchSet(int batches, int vector0 = -1, int vector1 = -1, int float0 = -1, int float1 = -1, int capacity = InstanceBatch.DefaultCapacity)
        {
            _batches = new InstanceBatch[batches];
            for (var i = 0; i < batches; i++)
            {
                _batches[i] = new InstanceBatch(vector0, vector1, float0, float1, capacity);
            }
        }

        public Mesh? Mesh { get; set; }

        public int Submesh { get; set; }

        public int Count { get; private set; }

        // Las instancias que no entraron (contador: la prueba de presupuesto lo mira).
        public int Overflow { get; private set; }

        public void Clear()
        {
            for (var i = 0; i <= _current && i < _batches.Length; i++)
            {
                _batches[i].Clear();
            }

            _current = 0;
            Count = 0;
            Overflow = 0;
        }

        public void Add(in Matrix4x4 matrix, in Vector4 v0, in Vector4 v1, float f0, float f1)
        {
            while (_current < _batches.Length && _batches[_current].IsFull)
            {
                _current++;
            }

            if (_current >= _batches.Length)
            {
                Overflow++;
                return;
            }

            _batches[_current].Add(matrix, v0, v1, f0, f1);
            Count++;
        }

        public void Draw(Material material, Camera camera, int layer, ShadowCastingMode shadows, in Bounds bounds)
        {
            if (Mesh == null)
            {
                return;
            }

            for (var i = 0; i <= _current && i < _batches.Length; i++)
            {
                _batches[i].Draw(Mesh, Submesh, material, camera, layer, shadows, bounds);
            }
        }

        // Cuántos dibujos pide (para el presupuesto).
        public int DrawCalls
        {
            get
            {
                var calls = 0;
                for (var i = 0; i <= _current && i < _batches.Length; i++)
                {
                    if (_batches[i].Count > 0)
                    {
                        calls++;
                    }
                }

                return calls;
            }
        }
    }
}
