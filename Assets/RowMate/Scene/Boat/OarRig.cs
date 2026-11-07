using RowMate.Core.Scene.Stroke;
using UnityEngine;
using UnityEngine.Rendering;

namespace RowMate.Scene
{
    // Los dos remos (07-remero-palada/02-tech-design §3.3; AC-SCN-05-07; AC-SCN-08-03; DA7-06, DA7-07): hijos del marco del bote con el
    // pivote en su perno (± PinX; PinY; 0). La malla de OarMesh (la caña sobre +x hacia la pala, la cara de empuje hacia −z) gira por el
    // barrido (eje y), el ángulo vertical (eje z: el puño arriba = la pala abajo) y el plumeo (eje de la caña: la cara de empuje queda
    // arriba). El remo derecho (x < 0) es el espejo (escala x −1: la cara de empuje sigue mirando a popa). Cada remo, dos renderers: la caña
    // (sin sombra) y la pala (proyecta: DA7-06). La flexión por remo (_RmOarLoad) en un material por lado (sin MaterialPropertyBlock: así
    // los cuatro renderers quedan en el SRP Batcher y no suman SetPass).
    public sealed class OarRig
    {
        private readonly Material _left;
        private readonly Material _right;
        private readonly float _pinX;
        private readonly float _pinY;

        // left y right: dos copias del material Oar (una por lado).
        public OarRig(Transform boatFrame, int layer, Mesh shaft, Mesh blade, Material left, Material right, in BoatSpec boat)
        {
            _pinX = (float)boat.PinX;
            _pinY = (float)boat.PinY;
            _left = left;
            _right = right;
            Left = Create("scene-oar-left", boatFrame, layer, shaft, blade, left, out var leftShaft, out var leftBlade);
            Right = Create("scene-oar-right", boatFrame, layer, shaft, blade, right, out var rightShaft, out var rightBlade);
            LeftShaft = leftShaft;
            LeftBlade = leftBlade;
            RightShaft = rightShaft;
            RightBlade = rightBlade;
            Left.localPosition = new Vector3(_pinX, _pinY, 0);
            Right.localPosition = new Vector3(-_pinX, _pinY, 0);
            Right.localScale = new Vector3(-1, 1, 1);
        }

        public Transform Left { get; }

        public Transform Right { get; }

        public MeshRenderer LeftShaft { get; }

        public MeshRenderer LeftBlade { get; }

        public MeshRenderer RightShaft { get; }

        public MeshRenderer RightBlade { get; }

        private static Transform Create(string name, Transform parent, int layer, Mesh shaft, Mesh blade, Material material, out MeshRenderer shaftRenderer,
                                        out MeshRenderer bladeRenderer)
        {
            var go = new GameObject(name) { hideFlags = HideFlags.DontSave, layer = layer };
            go.transform.SetParent(parent, false);
            shaftRenderer = SceneObjects.Create(name + "-shaft", go.transform, layer, shaft, material, false, true);
            bladeRenderer = SceneObjects.Create(name + "-blade", go.transform, layer, blade, material, true, true);
            bladeRenderer.shadowCastingMode = ShadowCastingMode.On;
            return go.transform;
        }

        public void Apply(in ScullingPose pose)
        {
            Orient(Left, pose.Left, 1);
            Orient(Right, pose.Right, -1);
            _left.SetFloat(SceneShaderIds.OarLoad, (float)pose.Left.ShaftBendM);
            _right.SetFloat(SceneShaderIds.OarLoad, (float)pose.Right.ShaftBendM);
        }

        // Solo las palas proyectan (DA7-06), y solo con el mapa de sombras.
        public void CastShadows(bool on)
        {
            var mode = on ? ShadowCastingMode.On : ShadowCastingMode.Off;
            LeftBlade.shadowCastingMode = mode;
            RightBlade.shadowCastingMode = mode;
        }

        // La rotación local de un remo: barrido · vertical · plumeo (side +1 = el remo de la mano izquierda, x > 0).
        public static Quaternion Rotation(in OarState oar, int side)
        {
            var sweep = (float)oar.SweepDeg;
            var pitch = (float)oar.PitchDeg;
            var feather = (float)oar.FeatherDeg;
            return Quaternion.AngleAxis(side * sweep, Vector3.up) * Quaternion.AngleAxis(-side * pitch, Vector3.forward)
                                                                 * Quaternion.AngleAxis(feather, Vector3.right);
        }

        private static void Orient(Transform oar, in OarState state, int side)
        {
            oar.localRotation = Rotation(state, side);
        }
    }
}
