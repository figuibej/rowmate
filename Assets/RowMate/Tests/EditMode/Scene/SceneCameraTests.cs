using System;
using System.Collections.Generic;
using NUnit.Framework;
using RowMate.Core.Scene;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Scene
{
    // SCC · La cámara del legacy y su matriz (scene/scene_camera.dart; 06a-escena-lago/02-tech-design §2.1; F-SCN-02; LT-62 a LT-65):
    // los siete tamaños (horizonte, focal, remero) y la matriz 4 × 4 de Core contra TryProject (≤ 1e-9) en las marcas de
    // AC-SCN-02-02.
    [TestFixture]
    public sealed class SceneCameraTests
    {
        private static readonly (double X, double Y, double Z)[] Marks =
        {
            (0, 0, 9.5), (-4, 0, 6), (0, 0, 30), (7.5, 0, 120), (3, 0, 200),
        };

        // legacy: test/scene/scene_camera_test.dart:9 'horizonte y focal según orientación'
        [TestCase(TestName = "horizonte y focal según orientación", Category = "LT-62,AC-SCN-02-01")]
        public void HorizonAndFocalByOrientation()
        {
            var portrait = new SceneCamera(390, 844);
            var landscape = new SceneCamera(844, 390);
            Assert.That(portrait.HorizonY, Is.EqualTo(844 * 0.40).Within(0.001));
            Assert.That(landscape.HorizonY, Is.EqualTo(390 * 0.42).Within(0.001));
            Assert.That(portrait.Focal, Is.EqualTo(390));
            Assert.That(landscape.Focal, Is.EqualTo(390));
        }

        // legacy: test/scene/scene_camera_test.dart:16 'lo lejano queda más cerca del horizonte, centrado y más chico'
        [TestCase(TestName = "lo lejano queda más cerca del horizonte, centrado y más chico", Category = "LT-63,AC-SCN-02-02")]
        public void FarIsCloserToTheHorizonCenteredAndSmaller()
        {
            var cam = new SceneCamera(390, 844);
            Assert.That(cam.TryProject(3, 0, 10, out var nx, out var ny), Is.True);
            Assert.That(cam.TryProject(3, 0, 200, out var fx, out var fy), Is.True);
            Assert.That(fy, Is.LessThan(ny));
            Assert.That(fy, Is.GreaterThan(cam.HorizonY));
            Assert.That(Math.Abs(fx - cam.CenterX), Is.LessThan(Math.Abs(nx - cam.CenterX)));
            Assert.That(cam.ScaleAt(200), Is.LessThan(cam.ScaleAt(10)));
            Assert.That(cam.TryProject(3, 0, 1e9, out var vx, out var vy), Is.True);
            Assert.That(vx, Is.EqualTo(cam.CenterX).Within(0.01));
            Assert.That(vy, Is.EqualTo(cam.HorizonY).Within(0.01));
        }

        // legacy: test/scene/scene_camera_test.dart:30 'detrás de la cámara no se proyecta'
        [TestCase(TestName = "detrás de la cámara no se proyecta", Category = "LT-64,AC-SCN-02-03")]
        public void BehindTheCameraDoesNotProject()
        {
            Assert.That(new SceneCamera(390, 844).TryProject(0, 0, 0.2, out _, out _), Is.False);
        }

        // legacy: test/scene/scene_camera_test.dart:34 'unprojectWater invierte project para y = 0'
        [TestCase(TestName = "unprojectWater invierte project para y = 0", Category = "LT-65,AC-SCN-02-02")]
        public void UnprojectWaterInvertsProject()
        {
            var cam = new SceneCamera(844, 390);
            foreach (var (x, z) in new[] { (-4.0, 6.0), (0.0, 30.0), (7.5, 120.0) })
            {
                Assert.That(cam.TryProject(x, 0, z, out var sx, out var sy), Is.True);
                cam.UnprojectWater(sx, sy, out var wx, out var wz);
                Assert.That(wx, Is.EqualTo(x).Within(1e-6));
                Assert.That(wz, Is.EqualTo(z).Within(1e-6));
            }
        }

        [TestCase("V390", 337.6, 390, Category = "AC-SCN-02-01")]
        [TestCase("H844", 163.8, 390, Category = "AC-SCN-02-01")]
        [TestCase("TV", 365.6, 411, Category = "AC-SCN-02-01")]
        [TestCase("P360", 320, 360, Category = "AC-SCN-02-01")]
        [TestCase("TH", 172.62, 411, Category = "AC-SCN-02-01")]
        [TestCase("L640", 151.2, 360, Category = "AC-SCN-02-01")]
        [TestCase("W1280", 302.4, 720, Category = "AC-SCN-02-01")]
        public void ContractHorizonAndFocalInTheSevenSizes(string size, double horizon, double focal)
        {
            var cam = SceneSeeds.Camera(size);
            Assert.That(cam.HorizonY, Is.EqualTo(horizon).Within(1e-9));
            Assert.That(cam.Focal, Is.EqualTo(focal));
            Assert.That(cam.CenterX, Is.EqualTo(cam.Width / 2));
        }

        [TestCase("TV", 0.504, Category = "AC-SCN-02-01")]
        [TestCase("P360", 0.504, Category = "AC-SCN-02-01")]
        [TestCase("V390", 0.507, Category = "AC-SCN-02-01")]
        [TestCase("H844", 0.652, Category = "AC-SCN-02-01")]
        [TestCase("TH", 0.652, Category = "AC-SCN-02-01")]
        [TestCase("L640", 0.652, Category = "AC-SCN-02-01")]
        [TestCase("W1280", 0.652, Category = "AC-SCN-02-01")]
        public void TheRowerProjectsAtTheCenterOfTheBoatZone(string size, double fraction)
        {
            var cam = SceneSeeds.Camera(size);
            Assert.That(cam.RowerY / cam.Height, Is.EqualTo(fraction).Within(0.0005));
            Assert.That(cam.TryProject(0, 0, SceneCamera.BoatZ, out _, out var sy), Is.True);
            Assert.That(sy, Is.EqualTo(cam.RowerY).Within(1e-9));
            // La zona del bote de la 4a (HudGeometry) tiene su centro en el mismo punto.
            Assert.That(RowMate.Core.Design.HudGeometry.BoatCenterY(cam.Width, cam.Height) * cam.Height, Is.EqualTo(cam.RowerY).Within(1e-9));
        }

        [TestCaseSource(nameof(SizeNames))]
        public void TheMatrixReproducesTryProject(string size)
        {
            // AC-SCN-02-02: con la respiración en 0 la proyección de Core da el píxel de las fórmulas del legacy (≤ 1e-9 dp).
            var cam = SceneSeeds.Camera(size);
            var projection = ChaseCameraProjection.Contract(cam);
            var pose = CameraPose.Contract(cam);
            var inside = 0;
            foreach (var m in Marks)
            {
                Assert.That(cam.TryProject(m.X, m.Y, m.Z, out var lx, out var ly), Is.True);
                Assert.That(ChaseCameraProjection.TryProjectWithPose(projection, pose, cam, m.X, m.Y, m.Z, out var ux, out var uy), Is.True);
                Assert.That(ux, Is.EqualTo(lx).Within(1e-9), size + " x de " + m);
                Assert.That(uy, Is.EqualTo(ly).Within(1e-9), size + " y de " + m);
                inside += lx >= 0 && lx <= cam.Width && ly >= 0 && ly <= cam.Height ? 1 : 0;
            }

            Assert.That(inside, Is.GreaterThanOrEqualTo(4), "(−4; 0; 6) no entra en vertical; las demás sí");
            // Y en una grilla de puntos del mundo, también fuera del agua.
            for (var x = -40.0; x <= 40; x += 7.3)
            {
                for (var y = 0.0; y <= 30; y += 4.1)
                {
                    for (var z = 0.6; z < 1900; z = z * 1.7 + 1)
                    {
                        Assert.That(cam.TryProject(x, y, z, out var lx, out var ly), Is.True);
                        Assert.That(ChaseCameraProjection.TryProjectWithPose(projection, pose, cam, x, y, z, out var ux, out var uy), Is.True);
                        Assert.That(ux, Is.EqualTo(lx).Within(1e-9 * Math.Max(1, Math.Abs(lx))));
                        Assert.That(uy, Is.EqualTo(ly).Within(1e-9 * Math.Max(1, Math.Abs(ly))));
                    }
                }
            }
        }

        [TestCase("V390", -0.20, Category = "AC-SCN-02-02")]
        [TestCase("TV", -0.20, Category = "AC-SCN-02-02")]
        [TestCase("H844", -0.16, Category = "AC-SCN-02-02")]
        [TestCase("W1280", -0.16, Category = "AC-SCN-02-02")]
        public void TheOffCenterTermIsTheLensShift(string size, double term)
        {
            var m = ChaseCameraProjection.Contract(SceneSeeds.Camera(size));
            Assert.That(m.M12, Is.EqualTo(term).Within(1e-12), "2h/H − 1 (lensShift.y = la mitad)");
            Assert.That(m.M32, Is.EqualTo(-1.0));
            Assert.That(m.M33, Is.EqualTo(0.0));
        }

        [Test, Category("AC-SCN-02-03")]
        public void NearAndFarPlanes()
        {
            Assert.That(ChaseCameraProjection.NearPlane, Is.EqualTo(SceneCamera.MinZ).And.EqualTo(0.5));
            Assert.That(ChaseCameraProjection.FarPlane, Is.EqualTo(2000.0));
            var cam = SceneSeeds.Camera("TV");
            var projection = ChaseCameraProjection.Contract(cam);
            var pose = CameraPose.Contract(cam);
            Assert.That(ChaseCameraProjection.TryProjectWithPose(projection, pose, cam, 0, 0, 0.5, out _, out _), Is.False, "z ≤ 0,5: no");
            Assert.That(ChaseCameraProjection.TryProjectWithPose(projection, pose, cam, 0, 0, 0.51, out _, out _), Is.True);
            // El plano cercano y el lejano en profundidad de clip (−1 y 1).
            projection.Transform(0, 0, -0.5, 1, out _, out _, out var nz, out var nw);
            projection.Transform(0, 0, -2000, 1, out _, out _, out var fz, out var fw);
            Assert.That(nz / nw, Is.EqualTo(-1.0).Within(1e-12));
            Assert.That(fz / fw, Is.EqualTo(1.0).Within(1e-9));
        }

        [Test, Category("AC-SCN-02-02")]
        public void TheMatrixInverseAndRotationsAreConsistent()
        {
            var cam = SceneSeeds.Camera("TH");
            var p = ChaseCameraProjection.Blend(cam, cam.Focal * 1.25, cam.Height / 2);
            var i = SceneMatrix4.Multiply(p, p.Inverse());
            for (var r = 0; r < 4; r++)
            {
                for (var c = 0; c < 4; c++)
                {
                    Assert.That(i[r, c], Is.EqualTo(r == c ? 1.0 : 0.0).Within(1e-9));
                }
            }

            var rot = ChaseCameraProjection.Rotation(-7.3, -20, 3);
            ChaseCameraProjection.Angles(rot, out var yaw, out var pitch, out var roll);
            Assert.That(yaw, Is.EqualTo(-7.3).Within(1e-9));
            Assert.That(pitch, Is.EqualTo(-20).Within(1e-9));
            Assert.That(roll, Is.EqualTo(3).Within(1e-9));
            var q = SceneQuaternion.FromAngles(-7.3, -20, 3).ToMatrix();
            for (var r = 0; r < 3; r++)
            {
                for (var c = 0; c < 3; c++)
                {
                    Assert.That(q[r, c], Is.EqualTo(rot[r, c]).Within(1e-12), "el cuaternión y la matriz con la misma convención");
                }
            }

            // Guiñada + = mira a la derecha; cabeceo + = mira arriba (convención de Unity: Quaternion.Euler(−cabeceo; guiñada; rolido)).
            var right = ChaseCameraProjection.Rotation(90, 0, 0);
            Assert.That(right.M02, Is.EqualTo(1.0).Within(1e-12));
            var up = ChaseCameraProjection.Rotation(0, 90, 0);
            Assert.That(up.M12, Is.EqualTo(1.0).Within(1e-12));
        }

        [TestCaseSource(nameof(SizeNames))]
        public void ThePoseMovesTheHorizonAsExpected(string size)
        {
            // Mirar 4° abajo sube el horizonte f·tan 4° (la toma aérea: 28,7 dp en TV).
            var cam = SceneSeeds.Camera(size);
            var pose = CameraPose.Contract(cam).WithRotation(0, -4, 0);
            var projection = ChaseCameraProjection.Contract(cam);
            Assert.That(ChaseCameraProjection.TryProjectWithPose(projection, pose, cam, 0, 2.2, 1e7, out _, out var sy), Is.True);
            Assert.That(cam.HorizonY - sy, Is.EqualTo(cam.Focal * Math.Tan(SceneMath.DegToRad(4))).Within(1e-3));
        }

        private static IEnumerable<TestCaseData> SizeNames()
        {
            foreach (var s in SceneSeeds.Sizes)
            {
                yield return new TestCaseData(s.Name).SetCategory("AC-SCN-02-02").SetName("Size " + s.Name);
            }
        }
    }
}
