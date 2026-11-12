using System;
using System.Collections;
using NUnit.Framework;
using RowMate.Core.Scene;
using RowMate.Scene;
using RowMate.Tests.UiSupport;
using UnityEngine;
using UnityEngine.TestTools;

namespace RowMate.Tests.PlayMode.Scene
{
    // SPR · La cámara de la escena es la del legacy (06a-escena-lago/02-tech-design §2.1, §3.5; F-SCN-02; sonda S6a-6; AC-SCN-02-01 a
    // -03, -08): con Camera.projectionMatrix = la de Core, WorldToViewportPoint × el tamaño de la RT (no WorldToScreenPoint, que usa el
    // de la pantalla) da el píxel del legacy (SceneCamera.TryProject) ± 1 dp en los siete tamaños; el plano cercano es 0,5 m; una sola
    // cámara, sin entrada de usuario.
    [Category("GPU")]
    public sealed class SceneProjectionTests : SceneTestBase
    {
        private static readonly DeviceProfile[] Sizes =
        {
            DeviceProfile.V390, DeviceProfile.H844, DeviceProfile.TV, DeviceProfile.P360, DeviceProfile.TH, DeviceProfile.L640, DeviceProfile.W1280,
        };

        // Las marcas de AC-SCN-02-02: la orilla a 20, 50, 175 y 400 m, el centro a 9,5 y a 100 m, y puntos altos.
        private static readonly double[,] Marks =
        {
            { -9.0, 0, 20 }, { 9.0, 0, 20 }, { -9.0, 0, 50 }, { 9.0, 0, 175 }, { -13.0, 0, 400 }, { 0, 0, 9.5 }, { 0, 0, 100 }, { 12, 6, 60 }, { -9, 0.4, 25 },
        };

        [UnityTest, Category("AC-SCN-02-01"), Category("AC-SCN-02-02"), Category("AC-SCN-02-03"), Category("AC-SCN-02-08")]
        public IEnumerator MarksLandOnTheLegacyPixel()
        {
            AssumeGpu();
            CreateScene(DeviceProfile.TV);
            yield return EnterFree();
            var host = SceneHost;
            host.BreathingOffForTest(true);
            foreach (var profile in Sizes)
            {
                H.Resize(profile);
                H.Step();
                yield return Frames();
                var camera = host.World!.Rig!.Camera;
                var cam = new SceneCamera(profile.WidthDp, profile.HeightDp);
                Assert.That(host.Frame.Window.Width, Is.EqualTo(profile.WidthDp).Within(1e-6), profile.Code);
                Assert.That(camera.nearClipPlane, Is.EqualTo((float)SceneCamera.MinZ), "AC-SCN-02-03: nada con z ≤ 0,5 m");
                var d = host.Frame.Distance;
                for (var i = 0; i < Marks.GetLength(0); i++)
                {
                    double x = Marks[i, 0], y = Marks[i, 1], z = Marks[i, 2];
                    Assert.That(cam.TryProject(x, y, z, out var lx, out var ly), Is.True);
                    // El mundo de la escena es de cinta: z − d (la cámara del contrato en el origen).
                    var v = camera.WorldToViewportPoint(new Vector3((float)x, (float)y, (float)z));
                    var sx = v.x * profile.WidthDp;
                    var sy = (1 - v.y) * profile.HeightDp;
                    Assert.That(sx, Is.EqualTo(lx).Within(1.0), profile.Code + " x de (" + x + "; " + y + "; " + z + ")");
                    Assert.That(sy, Is.EqualTo(ly).Within(1.0), profile.Code + " y de (" + x + "; " + y + "; " + z + ")");
                }

                Assert.That(d, Is.GreaterThanOrEqualTo(0));
            }

            // AC-SCN-02-08: una sola cámara de escena, apagada (se dibuja a pedido), sin entrada de usuario (PureCodeRulesTests).
            var cameras = Resources.FindObjectsOfTypeAll<Camera>();
            var sceneCameras = 0;
            foreach (var c in cameras)
            {
                if ((c.cullingMask & (1 << host.World!.Layer)) != 0)
                {
                    sceneCameras++;
                    Assert.That(c.enabled, Is.False);
                }
            }

            Assert.That(sceneCameras, Is.EqualTo(1));
        }

        // AC-SCN-02-01: la pose del contrato (2,2 m, nivelada) después del vuelo, con la respiración en 0.
        [UnityTest, Category("AC-SCN-02-01")]
        public IEnumerator TheContractPoseAfterTheFlight()
        {
            AssumeGpu();
            CreateScene(DeviceProfile.TV);
            yield return EnterFree();
            SceneHost.BreathingOffForTest(true);
            H.Step();
            var pose = SceneHost.Frame.Pose;
            Assert.That(SceneHost.Frame.CameraMode, Is.EqualTo(CameraMode.Contract));
            Assert.That(pose.Y, Is.EqualTo(SceneCamera.CamHeight).Within(1e-9));
            Assert.That(pose.X, Is.EqualTo(0).Within(1e-9));
            Assert.That(pose.PitchDeg, Is.EqualTo(0).Within(1e-9));
            var t = SceneHost.World!.Rig!.Camera.transform;
            Assert.That(t.position.y, Is.EqualTo(2.2f).Within(1e-5f));
        }
    }

    // SPL · Los objetos del legacy en su lugar (06a-escena-lago/02-tech-design §2.13, §3.6; AC-SCN-15-05 a -07, AC-SCN-14-07): el ancla 3D
    // de cada objeto visible proyectada por la cámara real cae en la x del legacy ± 2 dp (la y es la del pie sobre el terreno: nada flota),
    // en los siete tamaños a E-0, E-920 y E-1234; el terreno y el relleno desde d − 25 m (sin huecos en el primer cuadro del vuelo).
    [Category("GPU")]
    public sealed class ScenePlacementTests : SceneTestBase
    {
        [UnityTest, Category("AC-SCN-15-05"), Category("AC-SCN-15-07"), Category("AC-SCN-14-07")]
        public IEnumerator AnchorsLandOnTheLegacyX()
        {
            AssumeGpu();
            CreateScene(DeviceProfile.TV);
            yield return EnterFree();
            var host = SceneHost;
            host.BreathingOffForTest(true);
            var checkedProps = 0;
            foreach (var profile in new[] { DeviceProfile.V390, DeviceProfile.H844, DeviceProfile.TV, DeviceProfile.P360, DeviceProfile.TH, DeviceProfile.L640, DeviceProfile.W1280 })
            {
                H.Resize(profile);
                foreach (var distance in new[] { 0.0, 920.0, 1234.0 })
                {
                    host.ForceDistanceForTest(distance);
                    host.FreezeSceneTimeForTest(3.2);
                    H.Step();
                    yield return Frames(1);
                    var camera = host.World!.Rig!.Camera;
                    var cam = new SceneCamera(profile.WidthDp, profile.HeightDp);
                    var props = host.Frame.VisibleProps.Items;
                    var d = host.Frame.Distance;
                    for (var i = 0; i < props.Length; i++)
                    {
                        var p = props[i];
                        var relZ = p.Z - d;
                        if (!host.World.Director!.Shore.TryAnchor(p, out var ax, out var ay, out _))
                        {
                            Assert.Fail(profile.Code + " " + distance + ": " + p + " sin colocar");
                        }

                        Assert.That(cam.TryProject(p.X, 0, relZ, out var lx, out _), Is.True);
                        var v = camera.WorldToViewportPoint(new Vector3((float)ax, (float)ay, (float)relZ));
                        Assert.That(v.x * profile.WidthDp, Is.EqualTo(lx).Within(2.0), profile.Code + " " + distance + ": " + p);
                        if (p.Kind == PropKind.Pine)
                        {
                            // Nada flota: el pie del pino bajo el terreno de su tronco (la cabaña va sobre su zócalo, el muelle sobre sus pilotes).
                            Assert.That(ay, Is.LessThanOrEqualTo(TerrainProfile.Height(EnvironmentId.Lake, p.X, p.Z)), "nada flota: " + p);
                        }
                        checkedProps++;
                    }
                }
            }

            Assert.That(checkedProps, Is.GreaterThan(1000));
        }

        // DN6a-11: en el primer cuadro del vuelo (la cámara 14 m atrás) el terreno y el relleno ya cubren desde d − 25 m.
        [UnityTest, Category("AC-SCN-15-07")]
        public IEnumerator TerrainCoversBehindTheCameraOnTheFirstFlightFrame()
        {
            AssumeGpu();
            CreateScene(DeviceProfile.TV);
            yield return StartFree();
            yield return AdvanceTo(0.25);
            H.Step();
            var store = SceneHost.World!.Store!;
            SegmentStore.Window(SceneHost.Frame.Distance, out var kMin, out _);
            Assert.That(kMin * 50.0, Is.LessThanOrEqualTo(Math.Max(0, SceneHost.Frame.Distance - 25)));
            var segment = store.Find(kMin);
            Assert.That(segment, Is.Not.Null);
            Assert.That(segment!.TerrainReady(0) && segment.TerrainReady(1), Is.True, "los dos trozos del primer segmento");
            Assert.That(SceneHost.Frame.CameraMode, Is.EqualTo(CameraMode.Flight));
        }
    }

    // SLS · Una sesión larga (06a-escena-lago/02-tech-design §2.1, §3.5; ADR 015; AC-SCN-01-15; CAP-12): a 42 195 m la cámara sigue cerca
    // del origen (cinta), los objetos son los del legacy y dos cuadros seguidos con el bote quieto y el tiempo de escena fijo son iguales
    // píxel a píxel (sin temblor).
    [Category("GPU")]
    public sealed class SceneLongSessionTests : SceneTestBase
    {
        [UnityTest, Category("AC-SCN-01-15")]
        public IEnumerator AtTheMarathonNothingShakes()
        {
            AssumeGpu();
            CreateScene(DeviceProfile.W1280);
            yield return EnterFree();
            yield return SceneAlone(42195, 12);
            var host = SceneHost;
            Assert.That(host.Frame.Distance, Is.EqualTo(42195).Within(1.0));
            Assert.That(host.World!.Rig!.Camera.transform.position.z, Is.EqualTo(0).Within(0.5f), "la cámara cerca del origen (cinta)");
            Assert.That(host.Frame.VisibleProps.Count, Is.GreaterThan(50));
            var segments = new ShoreSegments(new LakeEnvironment());
            var buffer = new ShorePropBuffer(120);
            segments.VisibleProps(host.Frame.Distance, buffer);
            Assert.That(buffer.Count, Is.EqualTo(host.Frame.VisibleProps.Count), "los del legacy a esa distancia");
            // El bote quieto: en pausa la velocidad baja a 0 (≈ 9 s) y el tiempo de escena está fijo; dos cuadros seguidos iguales.
            H.App.Workout.Pause();
            H.Advance(20);
            host.ForceDistanceForTest(42195);
            // It. 6b (F-WKT-17): la placa de pausa es de vidrio y su desenfoque entra con un fundido de 200 ms (un movimiento finito): se
            // espera a que termine antes de comparar dos cuadros quietos.
            for (var i = 0; i < 20; i++)
            {
                H.Step();
                yield return null;
            }

            H.Step();
            yield return Frames(2);
            var a = Read();
            H.Step();
            yield return Frames(2);
            var b = Read();
            var differ = 0;
            var pa = a.GetPixels32();
            var pb = b.GetPixels32();
            for (var i = 0; i < pa.Length; i++)
            {
                if (pa[i].r != pb[i].r || pa[i].g != pb[i].g || pa[i].b != pb[i].b)
                {
                    differ++;
                }
            }

            Dump(a, "e42k");
            Assert.That(differ, Is.EqualTo(0), "píxeles distintos entre dos cuadros quietos a 42 km");
            Destroy(a);
            Destroy(b);
        }
    }
}
