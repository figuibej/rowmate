using System;
using System.Collections;
using System.Globalization;
using System.Text;
using NUnit.Framework;
using RowMate.Core.Scene;
using RowMate.Core.Scene.Water;
using RowMate.Tests.UiSupport;
using UnityEngine;
using UnityEngine.TestTools;

namespace RowMate.Tests.PlayMode.Scene
{
    // WRH · La estela con la historia REAL (QA-V6b-02; AC-6B-14): sin PrimeWakeForTest, remando con R-4 a los 30 s, en TV y Alto, con el
    // agua avanzada y la escena sola (la hora en 12, sin congelar el tiempo: los puntos nacen como en una sesión). Se lee la RT de la
    // escena y se proyecta con la cámara real:
    //   (a) la cabeza de la cinta (_RmWake[0], lo que dibuja el vértice) es la popa que se dibuja, con alfa 0 (sin el hueco de hasta 1 m del
    //       punto más nuevo, que se emite cada 0,25 s, ni su borde recto). Antes del arreglo: la cinta arrancaba en 4,998 con la popa en 5,309;
    //   (b) se apaga hacia la cámara: el contraste del brazo a 2,8 m de la cámara es ≤ 50 % del de 4 m (antes: 0,072 contra 0,093, 77 %).
    [Category("GPU")]
    public sealed class WakeRealHistoryTests : SceneTestBase
    {
        [UnityTest, Category("AC-6B-14")]
        public IEnumerator TheRealWakeStartsAtTheSternAndFadesTowardTheCamera()
        {
            AssumeGpu();
            CreateScene(DeviceProfile.TV, o =>
            {
                o.SceneTier = QualityTier.High;
                o.SceneWater = WaterForce.Auto;
            });
            yield return EnterFree();
            yield return AdvancedWaterTests.UntilAdvanced(this, SceneHost, H);
            yield return AdvanceTo(30);
            var host = SceneHost;
            host.ForceHourForTest(12);
            host.BreathingOffForTest(true);
            host.SetScrimsForTest(false);
            H.Immersive?.View.HideForCapture();
            for (var i = 0; i < 20; i++)
            {
                H.Step();
                yield return null;
            }

            yield return Frames(3);
            var camera = host.World!.Rig!.Camera;
            var frame = host.Frame;
            var image = RtReader.Read(host.Target.Texture!);
            try
            {
                Dump(image, "wrh-real-wake");
                var log = new StringBuilder("[WRH] cámara z " + F(camera.transform.position.z) + "; popa " + F(frame.Water.HullStartRelZ) + "; puntos " + frame.Water.WakeCount + "\n");
                var packed = frame.Water.WakePacked!;
                for (var k = 0; k < 8; k++)
                {
                    var o = k * WakeTrail.PackedStride;
                    log.Append("  k " + k + ": relZ " + F(packed[o]) + ", I " + F(packed[o + 1]) + ", edad " + F(packed[o + 2]) + ", back " + F(packed[o + 3]) + "\n");
                }

                var stern = frame.Water.HullStartRelZ;
                double Contrast(double z)
                {
                    var back = WaterModel.SternRelZ - z;
                    var hw = WaterModel.WakeHalfWidth(Math.Max(0, back));
                    var arm = Lum(image, camera, -hw, z, 2);
                    var outside = Lum(image, camera, -hw - 0.7, z, 2);
                    log.Append("  z " + F(z) + ": brazo " + F(arm) + ", afuera " + F(outside) + "\n");
                    return Math.Abs(arm - outside);
                }

                var mid = Contrast(4.0);
                var near = Contrast(2.8);
                for (var z = 6.0; z >= 1.6; z -= 0.4)
                {
                    Contrast(z);
                }

                var wake = Shader.GetGlobalVectorArray(RowMate.Scene.SceneShaderIds.Wake);
                log.Append("  _RmWake[0] " + wake[0] + "; [2] " + wake[2]);
                Debug.Log(log.ToString());
                Assert.That(wake[0].x, Is.EqualTo((float)stern).Within(1e-3f), "la cinta arranca en la popa que se dibuja");
                Assert.That(wake[0].w, Is.EqualTo(0f).Within(1e-6f), "con alfa 0: sin borde recto");
                Assert.That(wake[2].w, Is.GreaterThan(0f), "y a ≈ 2 m ya hay estela");
                Assert.That(mid, Is.GreaterThan(0.02), "a 4 m la estela se ve");
                Assert.That(near, Is.LessThanOrEqualTo(0.5 * mid), "se apaga hacia la cámara (a 2,8 m ≤ 50 % de a 4 m)");
            }
            finally
            {
                Destroy(image);
            }

            AssertClean(H);
        }

        // La luminancia media de ± r px alrededor del punto (x; 0; z) del agua (en cinta).
        private static double Lum(Texture2D image, Camera camera, double x, double z, int r)
        {
            var vp = camera.WorldToViewportPoint(new Vector3((float)x, 0, (float)z));
            var px = (int)Math.Round(vp.x * image.width);
            var py = (int)Math.Round((1 - vp.y) * image.height);
            var sum = 0.0;
            var n = 0;
            for (var dy = -r; dy <= r; dy++)
            {
                for (var dx = -r; dx <= r; dx++)
                {
                    var c = RtReader.PixelTopLeft(image, Math.Max(0, Math.Min(image.width - 1, px + dx)), Math.Max(0, Math.Min(image.height - 1, py + dy)));
                    sum += (0.2126 * c.r + 0.7152 * c.g + 0.0722 * c.b) / 255.0;
                    n++;
                }
            }

            return sum / n;
        }

        private static string F(double v)
        {
            return v.ToString("0.###", CultureInfo.InvariantCulture);
        }
    }
}
