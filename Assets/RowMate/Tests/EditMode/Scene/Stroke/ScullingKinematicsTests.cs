using System;
using System.Collections.Generic;
using System.Globalization;
using NUnit.Framework;
using RowMate.Core.Scene;
using RowMate.Core.Scene.Stroke;

namespace RowMate.Tests.EditMode.Scene.Stroke
{
    // SKK · La pose 3D del remero y de los remos (AC-SCN-05-06 a -09, AC-SCN-08-03, -12; ANIMATION_BIBLE §3.3 a §3.6): las medidas
    // reales, las curvas del remo y de la pala, la continuidad cuadro a cuadro, los contactos de las manos, el cruce con la izquierda
    // arriba, la flexión y el tronco con el esfuerzo, y la hoja de las 10 poses de la tabla.
    [TestFixture]
    public sealed class ScullingKinematicsTests
    {
        private static readonly BoatSpec Boat = BoatSpec.Real;
        private static readonly RowerSpec Rower = RowerSpec.Mannequin;
        private static readonly RigFit Fit = RiggingFit.Solve(BoatSpec.Real, RowerSpec.Mannequin);

        private static ScullingPose PoseAt(double c, double effort = 0)
        {
            var pose = ScullingPose.Create();
            ScullingKinematics.Evaluate(c, effort, Boat, Rower, Fit, RowerIdleSample.None, ref pose);
            return pose;
        }

        [Test, Category("AC-SCN-05-06")]
        public void TheRealMeasures()
        {
            Assert.That(Boat.PinX, Is.EqualTo(0.80));
            Assert.That(Boat.PinY, Is.EqualTo(0.31));
            Assert.That(Boat.OarLength, Is.EqualTo(2.88));
            Assert.That(Boat.Inboard, Is.EqualTo(0.88));
            Assert.That(Boat.BladeLength, Is.EqualTo(0.47));
            Assert.That(Boat.BladeWidth, Is.EqualTo(0.22));
            Assert.That(Boat.SeatTopY, Is.EqualTo(0.14));
            Assert.That(Boat.RailLength, Is.EqualTo(0.72));
            Assert.That(Boat.HullStern, Is.EqualTo(5.3));
            Assert.That(Boat.HullBow, Is.EqualTo(13.5));
            Assert.That(Boat.HullBow - Boat.HullStern, Is.EqualTo(8.20).Within(1e-9));
            Assert.That(Boat.BeamWaterline, Is.EqualTo(0.29));
            Assert.That(Boat.BeamGunwale, Is.EqualTo(0.33));
            Assert.That(Rower.Height, Is.InRange(1.80, 1.88));
            var catchPose = PoseAt(0);
            Assert.That(catchPose.Left.Grip.Z, Is.EqualTo(-0.72).Within(0.01), "el puño en el catch (arte §4.3)");
            Assert.That(PoseAt(0.36).Left.Grip.Z, Is.EqualTo(0.55).Within(0.01), "el puño en el finish");
        }

        [Test, Category("AC-SCN-05-07")]
        public void TheSweepAndTheBladeCurves()
        {
            Assert.That(OarCurves.VisualSweepDeg(0), Is.EqualTo(-62).Within(1e-9));
            Assert.That(OarCurves.VisualSweepDeg(0.37), Is.EqualTo(42).Within(1e-9));
            Assert.That(OarCurves.BladeCenterY(0.2), Is.EqualTo(-0.13));
            Assert.That(OarCurves.BladeCenterY(0.395), Is.EqualTo(0.05).Within(1e-12));
            Assert.That(OarCurves.BladeCenterY(0.6), Is.EqualTo(0.09));
            Assert.That(OarCurves.BladeCenterY(0.95), Is.EqualTo(0.17));
            Assert.That(OarCurves.BladeCenterY(0.0149), Is.EqualTo(-0.13).Within(1e-5));
            Assert.That(OarCurves.BladeCenterY(0.999), Is.EqualTo(OarCurves.BladeCenterY(0.0) + 0).Within(0.02), "continua en el catch");
            Assert.That(OarCurves.FeatherDeg(0.2), Is.EqualTo(0));
            Assert.That(OarCurves.FeatherDeg(0.6), Is.EqualTo(90));
            Assert.That(OarCurves.FeatherDeg(0.96), Is.EqualTo(0));

            // "Pala en el agua" (centro ≤ 0) termina en c ≈ 0,38 y empieza en c ≈ 0,99 (± 0,01).
            double exit = double.NaN, entry = double.NaN;
            for (var i = 1; i <= 100000; i++)
            {
                var a = (i - 1) / 100000.0;
                var b = i / 100000.0;
                if (OarCurves.BladeCenterY(a) <= 0 && OarCurves.BladeCenterY(b) > 0)
                {
                    exit = b;
                }

                if (OarCurves.BladeCenterY(a) > 0 && OarCurves.BladeCenterY(b) <= 0)
                {
                    entry = b;
                }
            }

            Assert.That(exit, Is.EqualTo(0.38).Within(0.01));
            Assert.That(entry, Is.EqualTo(0.99).Within(0.01));
        }

        [Test, Category("AC-SCN-05-07"), Category("AC-SCN-08-03")]
        public void TheVisibleBladeIsBuriedInTheDriveAndOutInTheRecovery()
        {
            for (var i = 0; i <= 1000; i++)
            {
                var c = i / 1000.0;
                var pose = PoseAt(c);
                foreach (var oar in new[] { pose.Left, pose.Right })
                {
                    if (c >= 0.02 && c <= 0.35)
                    {
                        Assert.That(oar.BladeCenter.Y, Is.LessThanOrEqualTo(-0.10), "enterrada en c " + c);
                        Assert.That(oar.FeatherDeg, Is.EqualTo(0), "cuadrada en el drive");
                    }

                    if (c >= 0.42 && c <= 0.95)
                    {
                        Assert.That(oar.BladeCenter.Y, Is.GreaterThanOrEqualTo(0.06), "afuera en c " + c);
                    }

                    if (c >= 0.43 && c <= 0.84)
                    {
                        Assert.That(oar.FeatherDeg, Is.EqualTo(90), "plumeada en c " + c);
                    }

                    if (c >= 0.955)
                    {
                        Assert.That(oar.FeatherDeg, Is.LessThanOrEqualTo(2), "cuadrada antes del catch");
                    }
                }
            }
        }

        [Test, Category("AC-SCN-05-07"), Category("AC-SCN-05-09")]
        public void NothingJumpsBetweenFramesIncludingTheCatch()
        {
            // A 36 spm (lo más rápido) un cuadro de 60 fps avanza ≤ 0,0168 de c en el drive: con pasos de 0,001 de c ninguna
            // articulación ni punto del remo se mueve más de 2 cm, también de 0,999 a 0.
            var previous = PoseAt(0.999);
            var max = 0.0;
            var at = "";
            for (var i = 0; i <= 1000; i++)
            {
                var c = i / 1000.0;
                var pose = PoseAt(c >= 1 ? 0 : c);
                for (var j = 0; j < ScullingPose.JointCount; j++)
                {
                    var d = SceneVec3.Distance(pose.Joints[j], previous.Joints[j]);
                    if (d > max)
                    {
                        max = d;
                        at = ((RowerJoint)j) + " en c " + c.ToString("0.000", CultureInfo.InvariantCulture);
                    }
                }

                var blade = SceneVec3.Distance(pose.Left.BladeCenter, previous.Left.BladeCenter);
                Assert.That(blade, Is.LessThan(0.03), "la pala en c " + c);
                Assert.That(Math.Abs(pose.Left.FeatherDeg - previous.Left.FeatherDeg), Is.LessThan(4), "el plumeo en c " + c);
                previous = pose;
            }

            Assert.That(max, Is.LessThan(0.02), "el salto más grande: " + at);
        }

        [Test, Category("AC-SCN-05-09")]
        public void HandsStayOnTheGripsHipOnTheSeatAndFeetOnTheStretcher()
        {
            for (var i = 0; i < 1000; i++)
            {
                var c = i / 1000.0;
                var pose = PoseAt(c, 0.5);
                Assert.That(pose.LeftReachError, Is.LessThanOrEqualTo(0.005), "la mano llega al puño (IK) en c " + c);
                Assert.That(pose.RightReachError, Is.LessThanOrEqualTo(0.005));
                Assert.That(SceneVec3.Distance(pose[RowerJoint.LeftGrip], pose.Left.Grip), Is.LessThanOrEqualTo(0.02), "mano izquierda sobre su puño");
                Assert.That(SceneVec3.Distance(pose[RowerJoint.RightGrip], pose.Right.Grip), Is.LessThanOrEqualTo(0.02), "mano derecha sobre su puño");
                var hip = PoseAngles.HipCenter(pose.Joints);
                Assert.That(Math.Abs(hip.Z - pose.SeatPosition.Z), Is.LessThanOrEqualTo(0.02), "cadera sobre el asiento");
                Assert.That(hip.Y - pose.SeatPosition.Y, Is.EqualTo(Rower.HipAboveSeat).Within(0.02));
                Assert.That(pose[RowerJoint.LeftFoot].Z, Is.EqualTo(Fit.AnkleZ).Within(1e-12), "pies en la pedalera");
                Assert.That(pose[RowerJoint.RightFoot].Y, Is.EqualTo(Fit.AnkleY).Within(1e-12));
                Assert.That(pose.SeatPosition.Z, Is.InRange(Fit.HipZCatch - ScullingKinematics.MaxHipBehindCatch - 1e-9, Fit.HipZFinish + 1e-9), "dentro del recorrido");
                Assert.That(PoseAngles.WristDeg(pose.Joints, true), Is.LessThanOrEqualTo(10), "muñeca plana");
            }
        }

        [Test, Category("AC-SCN-05-09")]
        public void TheGripsCrossWithTheLeftHandOnTop()
        {
            // |α| < 25°: la mano izquierda del remero (x > 0, D7-02) arriba y a popa; separación ≥ 4 cm (DA7-13).
            var crossed = 0;
            for (var i = 0; i < 1000; i++)
            {
                var c = i / 1000.0;
                var pose = PoseAt(c);
                if (Math.Abs(pose.Left.SweepDeg) <= ScullingKinematics.CrossStartDeg)
                {
                    crossed++;
                    var left = pose[RowerJoint.LeftGrip];
                    var right = pose[RowerJoint.RightGrip];
                    Assert.That(left.Y - right.Y, Is.GreaterThanOrEqualTo(0.04), "separación en c " + c);
                    Assert.That(left.Z, Is.LessThan(right.Z), "la izquierda va a popa");
                }
            }

            Assert.That(crossed, Is.GreaterThan(50), "el cruce pasa en el drive y en la recuperación");
            var alpha0 = PoseAt(0.18);
            Assert.That(Math.Abs(alpha0.Left.SweepDeg), Is.LessThan(12));
            Assert.That(alpha0[RowerJoint.LeftGrip].Y, Is.GreaterThan(alpha0[RowerJoint.RightGrip].Y), "en α ≈ 0 la mano de x > 0 arriba");
        }

        [Test, Category("AC-SCN-05-07")]
        public void TheGripHeightComesFromTheLever()
        {
            // ANIMATION_BIBLE §3.5: 0,515 enterrada, 0,413 en la recuperación y 0,375 antes del catch (sin el cruce).
            Assert.That(OarCurves.GripY(Boat, -0.13), Is.EqualTo(0.515).Within(0.001));
            Assert.That(OarCurves.GripY(Boat, 0.09), Is.EqualTo(0.413).Within(0.001));
            Assert.That(OarCurves.GripY(Boat, 0.17), Is.EqualTo(0.375).Within(0.001));
            var pose = PoseAt(0.96);
            Assert.That(pose.Left.Grip.Y, Is.EqualTo(0.375).Within(0.001));
            Assert.That(pose.Left.Grip.X, Is.EqualTo(-pose.Right.Grip.X).Within(1e-12), "espejados fuera del cruce");
        }

        [Test, Category("AC-SCN-08-03"), Category("AC-SCN-08-12")]
        public void TheShaftBendsWithTheEffortInTheDrive()
        {
            Assert.That(OarCurves.ShaftBendM(0.18, 0), Is.EqualTo(0.02).Within(1e-12));
            Assert.That(OarCurves.ShaftBendM(0.18, 1), Is.EqualTo(0.06).Within(1e-12));
            Assert.That(OarCurves.ShaftBendM(0.5, 1), Is.EqualTo(0));
            Assert.That(OarCurves.ShaftBendM(0, 1), Is.EqualTo(0));
            Assert.That(PoseAt(0.18, 0.917).Left.ShaftBendM, Is.GreaterThan(PoseAt(0.18, 0.167).Left.ShaftBendM), "Fuerte contra Suave");
        }

        [Test, Category("AC-SCN-08-12"), Category("AC-SCN-05-08")]
        public void TheTrunkGoesFurtherBackWithEffort()
        {
            Assert.That(PoseAt(0.36, 0).TrunkDeg, Is.EqualTo(-18).Within(1e-9));
            Assert.That(PoseAt(0.36, 1).TrunkDeg, Is.EqualTo(-20).Within(1e-9));
            Assert.That(PoseAt(0.36, 0.5).TrunkDeg, Is.EqualTo(-19).Within(1e-9));
            Assert.That(PoseAt(0, 1).TrunkDeg, Is.EqualTo(28).Within(1e-9));
        }

        // La tabla de las 10 poses (ANIMATION_BIBLE §3.4; AC-SCN-05-08): c, tronco, rodilla, cadera, hombro, codo, barrido. Desde
        // QA7-V-01 es la hoja medida de A7-01 (DV-78): rodilla, cadera, hombro y codo redondeados de lo que da Core con las medidas
        // reales y las manos en el puño del remo real; tronco y barrido, los de diseño. Maniquí de 1,85 m (arte §2.1).
        private static readonly double[][] Table =
        {
            new[] { 0.000, 28, 50, 29, 98, 171, -62 },
            new[] { 0.090, 28, 76, 33, 103, 171, -46 },
            new[] { 0.180, 12, 124, 62, 81, 166, -11 },
            new[] { 0.240, -8, 175, 100, 45, 130, 14 },
            new[] { 0.360, -18, 175, 107, -9, 70, 42 },
            new[] { 0.395, -18, 175, 107, -12, 80, 42 },
            new[] { 0.600, -2, 175, 95, 39, 128, 14 },
            new[] { 0.750, 28, 109, 44, 85, 147, -24 },
            new[] { 0.850, 28, 70, 31, 93, 171, -46 },
            new[] { 0.960, 28, 46, 29, 89, 171, -61 },
        };

        // La misma tabla para Y Bot (1,786 m, brazo 0,274 contra 0,344 del maniquí): con brazos más cortos el finish lleva el hombro
        // a −43° y el codo a 54°, fuera de ± 8° de la del maniquí, así que tiene su columna propia en §3.4.
        private static readonly double[][] YBotTable =
        {
            new[] { 0.000, 28, 46, 25, 99, 171, -62 },
            new[] { 0.090, 28, 74, 29, 103, 171, -46 },
            new[] { 0.180, 12, 131, 64, 81, 167, -11 },
            new[] { 0.240, -8, 175, 99, 35, 114, 14 },
            new[] { 0.360, -18, 175, 107, -43, 54, 42 },
            new[] { 0.395, -18, 175, 107, -41, 66, 42 },
            new[] { 0.600, -2, 175, 95, 28, 114, 14 },
            new[] { 0.750, 28, 107, 41, 79, 137, -24 },
            new[] { 0.850, 28, 68, 28, 92, 171, -46 },
            new[] { 0.960, 28, 42, 25, 88, 171, -61 },
        };

        // Y Bot de Mixamo con los largos medidos en su T-pose (02-tech-design B7-02: 1,786 m; la cadera sobre el asiento, la del
        // maniquí × 1,786 / 1,85, y el agarre al 75 % de la muñeca al nudillo, como RowerRigDriver.MeasureSpec). AC-SCN-05-08 vale
        // igual para el maniquí y para Y Bot.
        private static readonly RowerSpec YBotMeasured = new RowerSpec(
            hipHalfWidth: 0.091, hipAboveSeat: 0.09 * 1.786 / 1.85, hipsAboveJoints: 0.067, pelvisLength: 0.166, spineLength: 0.116,
            spine1Length: 0.134, spine2Length: 0.150, shoulderDrop: 0.062, clavicleHalfWidth: 0.061, shoulderHalfWidth: 0.188,
            neckLength: 0.103, headTopAbove: 0.185, upperArm: 0.274, forearm: 0.276, handToGrip: 0.75 * 0.128, thigh: 0.406, shin: 0.421,
            ankleHeight: 0.105, footLength: 0.300, footHalfSpread: 0.091);

        // Los 60 valores (10 poses × tronco, rodilla, cadera, hombro, codo y barrido) a ± 8° de la tabla, sin excepciones y con los
        // dos cuerpos (QA7-V-01).
        [TestCase("maniqui", Category = "AC-SCN-05-08")]
        [TestCase("ybot", Category = "AC-SCN-05-08")]
        public void TheTenPosesOfTheTable(string who)
        {
            var rower = who == "ybot" ? YBotMeasured : Rower;
            var fit = RiggingFit.Solve(Boat, rower);
            var report = new List<string>();
            var failures = new List<string>();
            foreach (var row in who == "ybot" ? YBotTable : Table)
            {
                var pose = ScullingPose.Create();
                ScullingKinematics.Evaluate(row[0], 0, Boat, rower, fit, RowerIdleSample.None, ref pose);
                var key = row[0].ToString("0.000", CultureInfo.InvariantCulture);
                var measured = new Dictionary<string, double>
                {
                    ["trunk"] = PoseAngles.TrunkDeg(pose.Joints),
                    ["knee"] = PoseAngles.KneeDeg(pose.Joints, true),
                    ["hip"] = PoseAngles.HipDeg(pose.Joints, true),
                    ["shoulder"] = PoseAngles.ShoulderDeg(pose.Joints, true),
                    ["elbow"] = PoseAngles.ElbowDeg(pose.Joints, true),
                    ["sweep"] = pose.Left.SweepDeg,
                };
                var expected = new Dictionary<string, double>
                {
                    ["trunk"] = row[1], ["knee"] = row[2], ["hip"] = row[3], ["shoulder"] = row[4], ["elbow"] = row[5], ["sweep"] = row[6],
                };
                foreach (var name in measured.Keys)
                {
                    var diff = measured[name] - expected[name];
                    report.Add(who + " " + key + " " + name + " " + measured[name].ToString("0.0", CultureInfo.InvariantCulture) + " (tabla "
                        + expected[name].ToString("0", CultureInfo.InvariantCulture) + ")");
                    if (Math.Abs(diff) > 8)
                    {
                        failures.Add(who + " " + key + " " + name + ": " + measured[name].ToString("0.0", CultureInfo.InvariantCulture) + " contra " + expected[name]);
                    }
                }

                Assert.That(PoseAngles.TrunkDeg(pose.Joints), Is.EqualTo(pose.TrunkDeg).Within(3), "el tronco medido ≈ el pedido");
            }

            TestContext.Out.WriteLine(string.Join("\n", report));
            Assert.That(report.Count, Is.EqualTo(60), "los 60 valores");
            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        [Test, Category("AC-SCN-05-08")]
        public void TheHeadLooksAstern()
        {
            foreach (var c in new[] { 0.0, 0.18, 0.36, 0.6, 0.85 })
            {
                var pose = PoseAt(c);
                var up = (pose[RowerJoint.HeadTop] - pose[RowerJoint.Head]).Normalized(SceneVec3.UnitY);
                var tilt = Math.Atan2(-up.Z, up.Y) * 180 / Math.PI;
                Assert.That(Math.Abs(tilt), Is.LessThanOrEqualTo(0.15 * 28 + 1), "la cabeza casi vertical (peso 0,85) en c " + c);
                Assert.That(pose.GazeTarget.Z, Is.EqualTo(-30));
            }
        }

        [Test, Category("AC-SCN-05-06")]
        public void ThePoseNeverHasNaN()
        {
            for (var i = 0; i < 400; i++)
            {
                var pose = PoseAt(i / 400.0, (i % 5) / 4.0);
                foreach (var j in pose.Joints)
                {
                    Assert.That(double.IsNaN(j.X) || double.IsNaN(j.Y) || double.IsNaN(j.Z), Is.False);
                }

                foreach (var l in pose.Lateral)
                {
                    Assert.That(l.Length, Is.EqualTo(1).Within(1e-6), "ejes laterales unitarios");
                }
            }
        }
    }
}
