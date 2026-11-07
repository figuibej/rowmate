using System;
using RowMate.Core.Scene;
using RowMate.Core.Scene.Art;
using RowMate.Core.Scene.Stroke;
using UnityEngine;
using UnityEngine.Rendering;

namespace RowMate.Scene
{
    // El agua que responde (07-remero-palada/02-tech-design §2.7, §3.4; DT7-12; AC-SCN-07-02, -03): los charcos que se ven (PuddleField,
    // fijos en el mundo), los dos hervideros del drive (atados a las palas mientras c ∈ [0,02; 0,36]) y el anillo de la boya, en UN dibujo
    // instanciado de 16 quads horizontales a 2 mm del agua (shader WaterMarks, _RmMarks = (edad; giro; alfa; tipo)). Los charcos detrás de
    // la cámara (relZ ≤ 0,5) no se dibujan. Arreglos reservados: Apply no asigna.
    public sealed class WaterInteractionRenderer
    {
        public const int Capacity = 16;
        public const float WaterY = 0.002f;
        public const float BuoyRingRadius = 0.45f;

        private static readonly Quaternion Flat = Quaternion.Euler(90, 0, 0);

        private readonly InstanceBatch _batch = new InstanceBatch(SceneShaderIds.Marks, -1, -1, -1, Capacity);
        private readonly Mesh _quad;
        private readonly Material _material;
        private readonly Camera _camera;
        private readonly int _layer;
        private Bounds _bounds = new Bounds(Vector3.zero, Vector3.one * 4000f);

        public WaterInteractionRenderer(Mesh quad, Material material, Camera camera, int layer)
        {
            _quad = quad;
            _material = material;
            _camera = camera;
            _layer = layer;
        }

        public Material Material => _material;

        // Cuántas marcas hay en este cuadro y de qué tipo (las sondas del arnés).
        public int Count => _batch.Count;

        public int Puddles { get; private set; }

        public int Boils { get; private set; }

        public bool BuoyRing { get; private set; }

        public bool BoilsEnabled { get; set; } = true;

        public void Apply(in SceneFrame frame)
        {
            _batch.Clear();
            Puddles = 0;
            Boils = 0;
            BuoyRing = false;
            var rower = frame.Rower;
            var distance = frame.Distance;
            var puddles = rower.Puddles;
            if (puddles != null)
            {
                for (var i = 0; i < puddles.Count && !_batch.IsFull; i++)
                {
                    ref readonly var p = ref puddles[i];
                    var relZ = p.WorldZ - distance;
                    if (PuddleField.IsBehindCamera(relZ) || p.Alpha <= 0)
                    {
                        continue;
                    }

                    var size = (float)(2 * PuddleField.Radius(p.Age));
                    Add(new Vector3((float)p.X, WaterY, (float)relZ), size, size, 0, new Vector4((float)p.Age, (float)p.Spin, (float)p.Alpha, 0));
                    Puddles++;
                }
            }

            if (BoilsEnabled)
            {
                var boatZ = rower.BoatWorldZ - distance;
                var alpha = (float)(RowerArt.BoilAlphaBase + RowerArt.BoilAlphaEffort * rower.Effort);
                if (rower.BoilLeft)
                {
                    AddBoil(rower.Pose.Left, boatZ, alpha);
                }

                if (rower.BoilRight)
                {
                    AddBoil(rower.Pose.Right, boatZ, alpha);
                }
            }

            var buoy = rower.Buoy;
            if (buoy != null && buoy.Visible && !_batch.IsFull)
            {
                var relZ = buoy.WorldZ - distance;
                Add(new Vector3((float)BuoyTracker.X, WaterY, (float)relZ), 2 * BuoyRingRadius, 2 * BuoyRingRadius, 0,
                    new Vector4(0, 0, (float)(0.6 * buoy.Alpha), 2));
                BuoyRing = true;
            }

            _material.SetFloat(SceneShaderIds.Effort, (float)rower.Effort);
        }

        private void AddBoil(in OarState oar, double boatZ, float alpha)
        {
            if (_batch.IsFull)
            {
                return;
            }

            var center = oar.BladeCenter;
            // Alargado a lo largo de la pala (el barrido), un poco a popa de su centro (el agua que empuja).
            var along = (float)oar.SweepDeg * (oar.BladeCenter.X > 0 ? 1f : -1f);
            Add(new Vector3((float)center.X, WaterY, (float)(boatZ + center.Z - 0.1)), (float)RowerArt.BoilLength, (float)RowerArt.BoilWidth,
                along * Mathf.Deg2Rad, new Vector4(0, 0, alpha, 1));
            Boils++;
        }

        private void Add(Vector3 position, float sizeX, float sizeZ, float yawRad, Vector4 marks)
        {
            var rotation = Quaternion.AngleAxis(yawRad * Mathf.Rad2Deg, Vector3.up) * Flat;
            _batch.Add(Matrix4x4.TRS(position, rotation, new Vector3(sizeX, sizeZ, 1)), marks, default, 0, 0);
        }

        public void Draw()
        {
            _batch.Draw(_quad, 0, _material, _camera, _layer, ShadowCastingMode.Off, _bounds);
        }
    }
}
