using System;
using RowMate.Core.Scene;
using RowMate.Core.Scene.Stroke;
using UnityEngine;

namespace RowMate.Scene
{
    // La boya de 500 m (07-remero-palada/02-tech-design §2.9, §3.4; AC-SCN-08-10; 00-arte-remero §9): la malla de BuoyMesh en x +4,5 m y
    // en la z de mundo de BuoyTracker (relativa a la cámara de la cinta), con su vaivén (0,08 rad · sin(1,7·t + semilla)) y su arfada
    // (0,02 m) en el tiempo de escena, y la aparición y el fundido con el tramado (_RmFade). El anillo del agua lo dibuja
    // WaterInteractionRenderer. Apply no asigna.
    public sealed class MarkerBuoy
    {
        public const double SwayRad = 0.08, SwayRate = 1.7, HeaveM = 0.02, HeaveRate = 1.3, Seed = 0.7;

        private readonly Material _material;

        public MarkerBuoy(Transform parent, int layer, Mesh mesh, Material material)
        {
            _material = material;
            Renderer = SceneObjects.Create("scene-buoy", parent, layer, mesh, material, true, true);
            Renderer.enabled = false;
        }

        public MeshRenderer Renderer { get; }

        public void Apply(in SceneFrame frame)
        {
            var buoy = frame.Rower.Buoy;
            var visible = buoy != null && buoy.Visible;
            if (Renderer.enabled != visible)
            {
                Renderer.enabled = visible;
            }

            if (!visible)
            {
                return;
            }

            var t = frame.SceneTime;
            var relZ = buoy!.WorldZ - frame.Distance;
            var sway = SwayRad * Math.Sin(SwayRate * t + Seed);
            // It. 6b (DT6b-15, AC-6B-19): en la costa, además, sube y baja con el mar de fondo, como el casco (CoastBuoySwell: 0,15·h(4,5;
            // relZ), con las fases de Core; 0 fuera de la costa).
            var heave = HeaveM * Math.Sin(HeaveRate * t + 2 * Seed) + frame.Water.BuoyHeave;
            Renderer.transform.localPosition = new Vector3((float)BuoyTracker.X, (float)heave, (float)relZ);
            Renderer.transform.localRotation = Quaternion.Euler((float)(0.5 * sway * 180 / Math.PI), 0, (float)(sway * 180 / Math.PI));
            _material.SetFloat(SceneShaderIds.Fade, (float)buoy.Alpha);
        }
    }
}
