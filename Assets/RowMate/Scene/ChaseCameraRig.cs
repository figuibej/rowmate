using System;
using RowMate.Core.Scene;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace RowMate.Scene
{
    // La cámara de persecución (06a-escena-lago/02-tech-design §2.1, §3.5; F-SCN-02; sonda S6a-6): en cada cuadro, la pose del cuadro
    // (posición en metros de cinta: la cámara queda cerca del origen y el mundo se dibuja en z − d) y la matriz de proyección que
    // calcula Core (ChaseCameraProjection: la del contrato, o la mezcla del vuelo) tal cual en Camera.projectionMatrix; plano cercano
    // 0,5 m y lejano 2 000 m. Nunca se dibuja sola (enabled = false: SceneRenderer la dibuja a pedido). Sin entrada de usuario (una sola
    // cámara, AC-SCN-02-08). Determinista (§5.1): sin TAA, sin el dithering de URP (rota su ruido en cada cuadro), sin grano.
    public sealed class ChaseCameraRig
    {
        public ChaseCameraRig(Transform parent, int layer)
        {
            var cameraObject = new GameObject("scene-camera") { hideFlags = HideFlags.DontSave, layer = layer };
            cameraObject.transform.SetParent(parent, false);
            Camera = cameraObject.AddComponent<Camera>();
            Camera.enabled = false;
            Camera.cullingMask = 1 << layer;
            Camera.clearFlags = CameraClearFlags.SolidColor;
            Camera.backgroundColor = Color.black;
            Camera.nearClipPlane = (float)ChaseCameraProjection.NearPlane;
            Camera.farClipPlane = (float)ChaseCameraProjection.FarPlane;
            Camera.useOcclusionCulling = false;
            Camera.depth = -100;
            Data = cameraObject.AddComponent<UniversalAdditionalCameraData>();
            Data.renderType = CameraRenderType.Base;
            Data.renderPostProcessing = true;
            Data.antialiasing = AntialiasingMode.None;
            Data.dithering = false;
            Data.stopNaN = false;
            Data.requiresDepthOption = CameraOverrideOption.Off;
            Data.requiresColorOption = CameraOverrideOption.Off;
            Data.volumeLayerMask = 1 << layer;
            Data.renderShadows = true;
        }

        public Camera Camera { get; }

        public UniversalAdditionalCameraData Data { get; }

        public void Apply(in SceneFrame frame)
        {
            var pose = frame.Pose;
            Camera.transform.SetPositionAndRotation(new Vector3((float)pose.X, (float)pose.Y, (float)pose.Z),
                Quaternion.Euler((float)-pose.PitchDeg, (float)pose.YawDeg, (float)pose.RollDeg));
            // El campo de visión vertical equivalente (lo usan las cuentas internas de Unity que no leen la matriz), antes de la matriz.
            var h = frame.Window.Height;
            var f = pose.FocalDp > 0 ? pose.FocalDp : frame.Window.Focal;
            if (h > 0 && f > 0)
            {
                Camera.fieldOfView = (float)(2 * Math.Atan(h / (2 * f)) * 180 / Math.PI);
            }

            Camera.projectionMatrix = ToUnity(frame.Projection);
        }

        public void Reset()
        {
            Camera.ResetProjectionMatrix();
        }

        public static Matrix4x4 ToUnity(in SceneMatrix4 m)
        {
            var u = new Matrix4x4();
            u.m00 = (float)m.M00;
            u.m01 = (float)m.M01;
            u.m02 = (float)m.M02;
            u.m03 = (float)m.M03;
            u.m10 = (float)m.M10;
            u.m11 = (float)m.M11;
            u.m12 = (float)m.M12;
            u.m13 = (float)m.M13;
            u.m20 = (float)m.M20;
            u.m21 = (float)m.M21;
            u.m22 = (float)m.M22;
            u.m23 = (float)m.M23;
            u.m30 = (float)m.M30;
            u.m31 = (float)m.M31;
            u.m32 = (float)m.M32;
            u.m33 = (float)m.M33;
            return u;
        }
    }
}
