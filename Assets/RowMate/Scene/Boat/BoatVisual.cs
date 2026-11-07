using System;
using RowMate.Core.Scene;
using RowMate.Core.Scene.Art;
using RowMate.Core.Scene.Stroke;
using UnityEngine;

namespace RowMate.Scene
{
    // El bote (07-remero-palada/02-tech-design §3.1, §3.3; D-ANI-03; AC-SCN-06-01, -02; AC-SCN-08-06): una jerarquía
    //   scene-boat-root   (cinta: z 9,5 + surge, quieto respecto de la cámara del contrato salvo el surge)
    //    ├── scene-boat-pitch   el pivote en el punto medio entre los pernos al nivel del agua: cabeceo, rolido y arfada (el marco del
    //    │    │                 bote de Core: la pose del remero y de los remos se da ahí)
    //    │    ├── scene-boat     el casco de la 6a (HullMesh viene en metros de cinta: se corre −9,5)
    //    │    ├── scene-boat-fittings   riggers, rieles y pedalera (BoatFittingsMesh; se rehace al cambiar el remero)
    //    │    ├── scene-boat-seat       el carro (SeatMesh), en SeatPosition
    //    │    ├── los remos (OarRig) y el remero (RowerCharacter)
    //    └── scene-boat-shadow  el óvalo del legacy (BoatShadow), solo con BoatShadowMode.Oval; no cabecea
    // Todo con el material Hull (los herrajes y el carro traen el color en el vértice). Apply copia RowerFrame sin asignar.
    public sealed class BoatVisual
    {
        public const float PinWorldZ = (float)SceneCamera.BoatZ;
        public const float HullCenterZ = (float)((LakeArt.HullStern + LakeArt.HullBow) / 2);

        private readonly Material _shadowMaterial;
        private Vector4 _shadowOffset;

        public BoatVisual(Transform parent, int layer, Mesh hull, Material hullMaterial, Mesh quad, Material shadowMaterial, Mesh fittings, Mesh seat)
        {
            _shadowMaterial = shadowMaterial;
            _shadowOffset = shadowMaterial.GetVector(SceneShaderIds.ShadowOffset);
            var root = new GameObject("scene-boat-root") { hideFlags = HideFlags.DontSave, layer = layer };
            root.transform.SetParent(parent, false);
            root.transform.localPosition = new Vector3(0, 0, PinWorldZ);
            Root = root.transform;
            var pitch = new GameObject("scene-boat-pitch") { hideFlags = HideFlags.DontSave, layer = layer };
            pitch.transform.SetParent(Root, false);
            Pitch = pitch.transform;
            Hull = SceneObjects.Create("scene-boat", Pitch, layer, hull, hullMaterial, true, true);
            Hull.transform.localPosition = new Vector3(0, 0, -PinWorldZ);
            Fittings = SceneObjects.Create("scene-boat-fittings", Pitch, layer, fittings, hullMaterial, true, true);
            Seat = SceneObjects.Create("scene-boat-seat", Pitch, layer, seat, hullMaterial, true, true);
            Shadow = SceneObjects.Create("scene-boat-shadow", Root, layer, quad, shadowMaterial, false, false);
            Shadow.transform.localPosition = new Vector3(0, 0.004f, HullCenterZ - PinWorldZ);
        }

        public Transform Root { get; }

        // El marco del bote de Core.
        public Transform Pitch { get; }

        public MeshRenderer Hull { get; }

        public MeshRenderer Fittings { get; }

        public MeshRenderer Seat { get; }

        public MeshRenderer Shadow { get; }

        public void Apply(in RowerFrame rower)
        {
            var boat = rower.Boat;
            Root.localPosition = new Vector3(0, 0, (float)(SceneCamera.BoatZ + boat.SurgeM));
            Pitch.localPosition = new Vector3(0, (float)boat.HeaveM, 0);
            // Cabeceo + = proa arriba (en Unity, + alrededor de x baja la proa); rolido alrededor del eje del bote.
            Pitch.localRotation = Quaternion.Euler((float)(-boat.PitchRad * 180 / Math.PI), 0, (float)(boat.RollRad * 180 / Math.PI));
            var seat = rower.Pose.SeatPosition;
            Seat.transform.localPosition = new Vector3((float)seat.X, (float)seat.Y, (float)seat.Z);
            var oval = rower.ShadowMode == BoatShadowMode.Oval;
            if (Shadow.enabled != oval)
            {
                Shadow.enabled = oval;
            }

            if (oval)
            {
                _shadowOffset.x = (float)rower.ShadowSide;
                _shadowMaterial.SetVector(SceneShaderIds.ShadowOffset, _shadowOffset);
            }
        }
    }
}
