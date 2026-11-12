using RowMate.Core.Scene;
using RowMate.Core.Scene.Water;
using UnityEngine;
using UnityEngine.Rendering;

namespace RowMate.Scene
{
    // La estela (F-SCN-12; 06b-agua-avanzada/02-tech-design §4.3; DT6b-23, G6b-02; PA6b-09; AC-6B-14): una cinta fija de 3 tiras (brazo
    // izquierdo, derecho y centro) × 48 segmentos que el vértice de WaterMarks (tipo 5) expande desde _RmWake: por punto de la historia de
    // WakeTrail (Core), su z de cinta, el semiancho de la V (0,3 + 0,35·retroceso, water.frag:85), el ancho del brazo y el alfa por la edad,
    // y el ancho y el alfa del centro. La cinta va en LA MISMA malla que el quad de las marcas (BuildCombined) y la estela es UNA INSTANCIA
    // MÁS del lote de WaterInteractionRenderer: 0 dibujos y 0 SetPass propios (medido: como dibujo aparte, Medio en el lago daba 51).
    // Con la degradada se apaga (_RmWakeFade = 1 − blend). Sin subir mallas por cuadro: solo el arreglo global (reservado). Apply no asigna.
    public sealed class WakeRenderer
    {
        public const int Strips = 3;

        // La cinta toma uno de cada dos puntos de la historia (24 de 48: un tramo cada 0,5 s, ≈ 2 m a 4 m/s): cada instancia del lote de marcas
        // lleva la malla entera (la parte que no es suya, degenerada), así la cinta cuesta 138 triángulos por instancia y no 282
        // (SceneAdaptationTests: el tope de triángulos de Medio con el maniquí).
        public const int PointStride = 2;

        // PROVISORIO (WP-D lo ajusta): con el alfa de Core entero la estela se leía como tres franjas blancas de borde a borde.
        public const double WakeShaderGain = 0.45;

        private readonly Vector4[] _packed = new Vector4[2 * WakeTrail.Capacity];

        public WakeRenderer()
        {
        }

        // Hay estela que dibujar en este cuadro (la instancia tipo 5 del lote de marcas).
        public bool Visible => Enabled && LivePoints > 0;

        // La intensidad de la popa del último cuadro y cuántos puntos vivos (las pruebas y las sondas).
        public double HeadIntensity { get; private set; }

        public int LivePoints { get; private set; }

        public bool Enabled { get; set; } = true;

        public void Apply(in SceneFrame frame)
        {
            var water = frame.Water;
            var packed = water.WakePacked;
            HeadIntensity = water.WakeIntensity;
            LivePoints = 0;
            if (packed == null)
            {
                return;
            }

            // La cabeza viva (el lugar 0, la popa que se dibuja): la rampa de la cabeza cuenta desde ahí (con el surge), no desde la popa nominal.
            var headZ = packed[0];
            for (var k = 0; k < WakeTrail.Capacity; k++)
            {
                var o = k * WakeTrail.PackedStride;
                var relZ = packed[o];
                var intensity = packed[o + 1];
                var age = packed[o + 2];
                var back = packed[o + 3];
                // El alfa de Core × WakeShaderGain: el dibujo del hervidero ya multiplica la espuma × 1,6 (kRmMarkFoamGain). Con la cabeza
                // que nace en la popa (QA-V6b-02: el lugar 0 es la popa que se dibuja, con alfa 0).
                var alpha = intensity > 0 ? WakeShaderGain * WakeTrail.ShapedAlpha(intensity, age, headZ - relZ) : 0;
                if (alpha > 0)
                {
                    LivePoints++;
                }

                _packed[k] = new Vector4(relZ, (float)WaterModel.WakeHalfWidth(back), (float)WakeTrail.ArmWidth(age), (float)alpha);
                _packed[WakeTrail.Capacity + k] = new Vector4((float)WakeTrail.CenterWidth(age), (float)(0.6 * alpha), 0, 0);
            }

            Shader.SetGlobalVectorArray(SceneShaderIds.Wake, _packed);
        }

        // La malla del lote de marcas: el quad de la escena (UV2 = 0) y la cinta (UV2 = 1): por tira y por punto k, dos vértices (los dos
        // lados, UV0.x 0 y 1) en (tira; 0; k), dos triángulos por tramo.
        public static Mesh BuildCombined()
        {
            var n = WakeTrail.Capacity / PointStride;
            var ribbon = Strips * n * 2;
            var vertices = new Vector3[4 + ribbon];
            var uv = new Vector2[vertices.Length];
            var uv2 = new Vector2[vertices.Length];
            var triangles = new int[6 + Strips * (n - 1) * 6];
            vertices[0] = new Vector3(-0.5f, -0.5f, 0);
            vertices[1] = new Vector3(0.5f, -0.5f, 0);
            vertices[2] = new Vector3(0.5f, 0.5f, 0);
            vertices[3] = new Vector3(-0.5f, 0.5f, 0);
            uv[0] = new Vector2(0, 0);
            uv[1] = new Vector2(1, 0);
            uv[2] = new Vector2(1, 1);
            uv[3] = new Vector2(0, 1);
            var t = 0;
            triangles[t++] = 0;
            triangles[t++] = 2;
            triangles[t++] = 1;
            triangles[t++] = 0;
            triangles[t++] = 3;
            triangles[t++] = 2;
            for (var s = 0; s < Strips; s++)
            {
                for (var k = 0; k < n; k++)
                {
                    var i = 4 + (s * n + k) * 2;
                    vertices[i] = new Vector3(s, 0, k * PointStride);
                    vertices[i + 1] = new Vector3(s, 0, k * PointStride);
                    uv[i] = new Vector2(0, 0);
                    uv[i + 1] = new Vector2(1, 0);
                    uv2[i] = new Vector2(1, 0);
                    uv2[i + 1] = new Vector2(1, 0);
                    if (k + 1 < n)
                    {
                        triangles[t++] = i;
                        triangles[t++] = i + 2;
                        triangles[t++] = i + 1;
                        triangles[t++] = i + 1;
                        triangles[t++] = i + 2;
                        triangles[t++] = i + 3;
                    }
                }
            }

            var mesh = new Mesh { name = "scene-marks-and-wake", hideFlags = HideFlags.HideAndDontSave, vertices = vertices, uv = uv, uv2 = uv2, triangles = triangles };
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 4000f);
            return mesh;
        }
    }
}
