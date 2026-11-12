using System;
using RowMate.Core.Scene;
using RowMate.Core.Scene.Art;
using RowMate.Core.Scene.Stroke;
using RowMate.Core.Scene.Water;
using UnityEngine;
using UnityEngine.Rendering;

namespace RowMate.Scene
{
    // El agua que responde (07-remero-palada/02-tech-design §2.7, §3.4; DT7-12; AC-SCN-07-02, -03): los charcos que se ven (PuddleField,
    // fijos en el mundo), los dos hervideros del drive (atados a las palas mientras c ∈ [0,02; 0,36]) y el anillo de la boya, en UN dibujo
    // instanciado de 16 quads horizontales a 2 mm del agua (shader WaterMarks, _RmMarks = (edad; giro; alfa; tipo)). Los charcos detrás de
    // la cámara (relZ ≤ 0,5) no se dibujan. Arreglos reservados: Apply no asigna.
    // It. 6b (06b-agua-avanzada/02-tech-design §4.3; DT6b-23; PA6b-10, -11; #12, #13): 40 marcas + 32 columnas = 72 en el mismo lote. Tipo 3 =
    // los micro-anillos de MicroRingField (con el agua avanzada en Alto y Medio), tipo 4 = la columna de luz de LightColumns (un quad
    // acostado desde el pie de la luz hacia la cámara, con su color en _RmMarkTint; con cualquier agua). #12: el charco × 1,3 con alfa
    // mínimo 0,35 hasta los 2 s, el par de remolinos girado 40° (SwirlOrientation, WaterLookArt) y el hervidero + 25 %.
    public sealed class WaterInteractionRenderer
    {
        public const int MarkCapacity = 40;

        // Dónde dibuja el anillo WaterMarks en el quad (r = √0,72).
        public const double RingQuadRadius = 0.8485;
        public const int Capacity = MarkCapacity + LightColumns.Capacity + 1;
        public const float WaterY = 0.002f;
        public const float BuoyRingRadius = 0.45f;

        private static readonly Quaternion Flat = Quaternion.Euler(90, 0, 0);

        private readonly InstanceBatch _batch = new InstanceBatch(SceneShaderIds.Marks, SceneShaderIds.MarkTint, -1, -1, Capacity);
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

        // It. 6b: los micro-anillos (con el agua avanzada en Alto y Medio) y las columnas de luz que entraron en este cuadro.
        public bool RingsEnabled { get; set; }

        public int Rings { get; private set; }

        public int Columns { get; private set; }

        // La estela (DT6b-23): una instancia más del lote (tipo 5), sobre la parte de la cinta de la malla (WakeRenderer.BuildCombined).
        public WakeRenderer? Wake { get; set; }

        public bool WakeDrawn { get; private set; }

        public void Apply(in SceneFrame frame)
        {
            _batch.Clear();
            Puddles = 0;
            Boils = 0;
            Rings = 0;
            Columns = 0;
            WakeDrawn = false;
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

                    // #12: × 1,3 y alfa mínimo 0,35 hasta los 2 s; el par girado 40° (hacia la popa del lado de la pala).
                    // Solo con el agua avanzada (RingsEnabled): con la degradada fija, los charcos de la 7 sin cambios (DT6b-24).
                    var enhanced = RingsEnabled;
                    var size = (float)(2 * PuddleField.Radius(p.Age) * (enhanced ? WaterLookArt.PuddleQuadScale : 1));
                    var alpha = enhanced && p.Age < WaterLookArt.PuddleMinAlphaUntil ? System.Math.Max(p.Alpha, WaterLookArt.PuddleMinAlpha) : p.Alpha;
                    var yaw = enhanced ? (float)((p.X >= 0 ? 1 : -1) * WaterLookArt.SwirlAngleDeg * System.Math.PI / 180) : 0f;
                    // QA-V6b-03: con la avanzada, el charco aplana el oleaje (_RmMarkTint.x = 1).
                    Add(new Vector3((float)p.X, WaterY, (float)relZ), size, size, yaw, new Vector4((float)p.Age, (float)p.Spin, (float)alpha, 0),
                        enhanced ? new Vector4(1, 0, 0, 0) : default);
                    Puddles++;
                }
            }

            if (BoilsEnabled)
            {
                var boatZ = rower.BoatWorldZ - distance;
                var alpha = (float)((RingsEnabled ? WaterLookArt.BoilAlphaGain : 1) * (RowerArt.BoilAlphaBase + RowerArt.BoilAlphaEffort * rower.Effort));
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

            var rings = frame.Water.Rings;
            if (RingsEnabled && rings != null)
            {
                for (var i = 0; i < rings.Count && _batch.Count < MarkCapacity; i++)
                {
                    ref readonly var r = ref rings[i];
                    var relZ = r.WorldZ - distance;
                    var alpha = MicroRingField.Alpha(r.Kind, r.Age);
                    if (relZ <= 0.5 || alpha <= 0)
                    {
                        continue;
                    }

                    var radius = MicroRingField.Radius(r.Kind, r.Age);
                    // El dibujo del anillo de la boya (r² = 0,72 en el quad): el quad a la medida del radio de Core.
                    var size = (float)(2 * radius / RingQuadRadius);
                    Add(new Vector3((float)r.X, WaterY, (float)relZ), size, size, 0, new Vector4(0, 0, (float)alpha, 3));
                    Rings++;
                }
            }

            // Las columnas de luz (tipo 4; DT6b-23): acostadas desde el pie de la luz hacia la cámara, con su color.
            var columns = frame.Water.Columns;
            if (columns != null)
            {
                var phase = (float)(frame.Water.Phases.B2V * 2 * System.Math.PI);
                for (var i = 0; i < columns.Count && !_batch.IsFull; i++)
                {
                    ref readonly var c = ref columns[i];
                    if (c.Alpha <= 0 || c.RelZ - c.Length <= 0.5)
                    {
                        continue;
                    }

                    var tint = SceneLighting.Srgb(SceneColor.FromArgb(c.Tone)).linear;
                    AddTinted(new Vector3((float)c.X, WaterY, (float)(c.RelZ - c.Length / 2)), (float)c.Width, (float)c.Length,
                        new Vector4(phase, 0, (float)c.Alpha, 4), new Vector4(tint.r, tint.g, tint.b, 1));
                    Columns++;
                }
            }

            if (Wake != null && Wake.Visible && frame.Water.DegradedBlend < 0.9999)
            {
                _batch.Add(Matrix4x4.identity, new Vector4(0, 0, 1, 5), default, 0, 0);
                WakeDrawn = true;
            }

            _material.SetFloat(SceneShaderIds.Effort, (float)rower.Effort);
        }

        private void AddTinted(Vector3 position, float sizeX, float sizeZ, Vector4 marks, Vector4 tint)
        {
            _batch.Add(Matrix4x4.TRS(position, Flat, new Vector3(sizeX, sizeZ, 1)), marks, tint, 0, 0);
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
            // QA-V6b-03: con la avanzada, 0,2 m a popa (se ve debajo de la pala); con la degradada, el 0,1 de la 7.
            var aft = RingsEnabled ? WaterLookArt.BoilAftOffset : 0.1;
            Add(new Vector3((float)center.X, WaterY, (float)(boatZ + center.Z - aft)), (float)RowerArt.BoilLength, (float)RowerArt.BoilWidth,
                along * Mathf.Deg2Rad, new Vector4(0, 0, alpha, 1));
            Boils++;
        }

        private void Add(Vector3 position, float sizeX, float sizeZ, float yawRad, Vector4 marks, Vector4 tint = default)
        {
            var rotation = Quaternion.AngleAxis(yawRad * Mathf.Rad2Deg, Vector3.up) * Flat;
            _batch.Add(Matrix4x4.TRS(position, rotation, new Vector3(sizeX, sizeZ, 1)), marks, tint, 0, 0);
        }

        public void Draw()
        {
            _batch.Draw(_quad, 0, _material, _camera, _layer, ShadowCastingMode.Off, _bounds);
        }
    }
}
