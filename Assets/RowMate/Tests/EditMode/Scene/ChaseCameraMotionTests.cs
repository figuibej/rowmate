using System;
using System.Collections.Generic;
using NUnit.Framework;
using RowMate.Core.Design;
using RowMate.Core.Scene;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Scene
{
    // CCM · La respiración, el vuelo, la salida y la toma aérea (06a-escena-lago/02-tech-design §2.2; AC-SCN-02-04 a -07; DN6a-07,
    // -08, -20; DT6a-01). Los valores salen de MotionSpecs (§9.3), nunca de un literal salvo los de la spec.
    [TestFixture]
    public sealed class ChaseCameraMotionTests
    {
        private const double TwoPi = 2 * Math.PI;

        [Test, Category("AC-SCN-02-04")]
        public void BreathingIsTheBibleFormulaWithTheRollOfTheGate()
        {
            var cam = SceneSeeds.Camera("TV");
            var spec = BreathingSpec.Lake;
            Assert.That(spec.RollDeg, Is.EqualTo(0.15), "DT6a-01");
            foreach (var t in new[] { 0.0, 1.3, 7.7, 59.1, 600.0 })
            {
                var pose = ChaseCameraMotion.Breathing(cam, t, 1, spec);
                Assert.That(pose.X, Is.EqualTo(0.035 * Math.Sin(TwoPi * 0.071 * t) + 0.015 * Math.Sin(TwoPi * 0.173 * t + 1.7)).Within(1e-12));
                Assert.That(pose.Y, Is.EqualTo(2.2 + 0.025 * Math.Sin(TwoPi * 0.093 * t + 0.4)).Within(1e-12));
                Assert.That(pose.Z, Is.EqualTo(0));
                Assert.That(pose.RollDeg, Is.EqualTo(0.15 * Math.Sin(TwoPi * 0.061 * t + 2.1)).Within(1e-12));
                Assert.That(pose.PitchDeg, Is.EqualTo(0.10 * Math.Sin(TwoPi * 0.083 * t + 0.9)).Within(1e-12));
                Assert.That(pose.YawDeg, Is.EqualTo(0));
                Assert.That(pose.FocalDp, Is.EqualTo(cam.Focal));
                Assert.That(pose.HorizonDp, Is.EqualTo(cam.HorizonY));
                var again = ChaseCameraMotion.Breathing(cam, t, 1, spec);
                Assert.That(again.X, Is.EqualTo(pose.X), "sin azar: el mismo tiempo da la misma pose");
            }
        }

        [Test, Category("AC-SCN-02-04"), Category("AC-APP-05-08")]
        public void MultiplierZeroIsTheContractPose()
        {
            var cam = SceneSeeds.Camera("TH");
            var pose = ChaseCameraMotion.Breathing(cam, 12.5, 0, BreathingSpec.Lake);
            Assert.That(pose.X, Is.EqualTo(0));
            Assert.That(pose.Y, Is.EqualTo(2.2));
            Assert.That(pose.RollDeg, Is.EqualTo(0));
            Assert.That(pose.PitchDeg, Is.EqualTo(0));
            var director = new ChaseCameraDirector();
            director.ShowInPlace(0);
            Assert.That(director.BreathingMultiplier(10, true, false), Is.EqualTo(0), "reducir movimiento: ×0");
            Assert.That(director.BreathingMultiplier(10, false, true), Is.EqualTo(0), "respiración en 0 (prueba)");
            Assert.That(director.BreathingMultiplier(10, false, false), Is.EqualTo(1));
        }

        [TestCaseSource(nameof(Sizes))]
        public void TheHorizonNeverMovesMoreThanThreeDpInTenMinutes(string size)
        {
            var cam = SceneSeeds.Camera(size);
            var spec = BreathingSpec.Lake;
            var max = 0.0;
            var zoneTop = cam.RowerY - 0.15 * cam.Height;
            var zoneBottom = cam.RowerY + 0.15 * cam.Height;
            var projection = ChaseCameraProjection.Contract(cam);
            for (var i = 0; i <= 600 * 200; i++)
            {
                var t = i / 200.0;
                var pose = ChaseCameraMotion.Breathing(cam, t, 1, spec);
                max = Math.Max(max, ChaseCameraMotion.HorizonDeviationDp(cam, pose));
                if (i % 50 == 0)
                {
                    Assert.That(ChaseCameraProjection.TryProjectWithPose(projection, pose, cam, 0, 0, SceneCamera.BoatZ, out var rx, out var ry), Is.True);
                    Assert.That(ry, Is.InRange(zoneTop, zoneBottom), "el remero en la zona del bote");
                    Assert.That(Math.Abs(rx - cam.CenterX), Is.LessThan(0.22 * cam.Width));
                }
            }

            TestContext.Out.WriteLine(size + ": " + max.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + " dp");
            Assert.That(max, Is.LessThanOrEqualTo(3.0), "AC-SCN-02-04: ±3 dp");
            if (size == "W1280")
            {
                Assert.That(max, Is.EqualTo(2.93).Within(0.01), "el peor caso, con 0,15° (DT6a-01)");
            }

            if (size == "TV")
            {
                Assert.That(max, Is.EqualTo(1.26).Within(0.01));
            }
        }

        [Test, Category("AC-SCN-02-04")]
        public void TheRollOfTheSpecBeforeTheGateBreaksTheThreeDp()
        {
            // DT6a-01: con 0,18° el horizonte se aparta 3,267 dp en W1280.
            var cam = SceneSeeds.Camera("W1280");
            var b = BreathingSpec.Lake;
            var old = new BreathingSpec(b.LateralA, b.LateralFreqA, b.LateralB, b.LateralFreqB, b.LateralPhaseB, b.Vertical, b.VerticalFreq,
                b.VerticalPhase, 0.18, b.RollFreq, b.RollPhase, b.PitchDeg, b.PitchFreq, b.PitchPhase);
            var max = 0.0;
            for (var i = 0; i <= 600 * 200; i++)
            {
                max = Math.Max(max, ChaseCameraMotion.HorizonDeviationDp(cam, ChaseCameraMotion.Breathing(cam, i / 200.0, 1, old)));
            }

            Assert.That(max, Is.EqualTo(3.267).Within(0.01));
        }

        [Test, Category("AC-SCN-02-04")]
        public void PauseScalesTheBreathingWithItsRamps()
        {
            var d = new ChaseCameraDirector();
            d.ShowInPlace(-10);
            Assert.That(d.BreathingMultiplier(0, false, false), Is.EqualTo(1));
            d.SetPaused(true, 0);
            Assert.That(d.BreathingMultiplier(0.15, false, false), Is.EqualTo(1 + (0.6 - 1) * Easing.Evaluate(Tokens.EaseMove, 0.5)).Within(1e-12));
            Assert.That(d.BreathingMultiplier(MotionSpecs.BreathingPauseInMs / 1000, false, false), Is.EqualTo(0.6).Within(1e-12));
            Assert.That(d.BreathingMultiplier(5, false, false), Is.EqualTo(0.6).Within(1e-12));
            d.SetPaused(false, 5);
            Assert.That(d.BreathingMultiplier(5 + MotionSpecs.BreathingPauseOutMs / 1000, false, false), Is.EqualTo(1).Within(1e-12));
            // Un cambio a mitad de camino arranca desde donde está.
            d.SetPaused(true, 10);
            var mid = d.BreathingMultiplier(10.1, false, false);
            d.SetPaused(false, 10.1);
            Assert.That(d.BreathingMultiplier(10.1, false, false), Is.EqualTo(mid).Within(1e-12));
            Assert.That(MotionSpecs.BreathingPauseInMs, Is.EqualTo(300));
            Assert.That(MotionSpecs.BreathingPauseOutMs, Is.EqualTo(240));
            Assert.That(MotionSpecs.BreathingPausedFactor, Is.EqualTo(0.6));
        }

        [Test, Category("AC-SCN-02-04"), Category("AC-SCN-02-05")]
        public void BreathingEntersWithItsRampAfterTheFlight()
        {
            // DN6a-20: S(0; 0,6 s) desde el fin del vuelo; en el cuadro de los 1 820 ms vale 0 y a los 2 420, 1.
            var cam = SceneSeeds.Camera("TV");
            var d = new ChaseCameraDirector();
            var start = MotionSpecs.FlightStartMs / 1000;
            d.BeginFlight(start, false);
            var end = start + MotionSpecs.FlightDurationMs / 1000;
            Assert.That(end, Is.EqualTo(1.82).Within(1e-12));
            var before = d.Evaluate(end - 1.0 / 60, 1.8, cam, false, false);
            Assert.That(d.Mode, Is.EqualTo(CameraMode.Flight));
            var atEnd = d.Evaluate(end, 1.82, cam, false, false);
            Assert.That(d.Mode, Is.EqualTo(CameraMode.Contract));
            Assert.That(d.LastBreathingMultiplier, Is.EqualTo(0), "el multiplicador vale 0 en el cuadro de los 1 820");
            var contract = CameraPose.Contract(cam);
            Assert.That(atEnd.X, Is.EqualTo(contract.X));
            Assert.That(atEnd.Y, Is.EqualTo(contract.Y));
            Assert.That(atEnd.Z, Is.EqualTo(contract.Z));
            Assert.That(atEnd.PitchDeg, Is.EqualTo(0));
            var jump = Math.Sqrt(Math.Pow(atEnd.X - before.X, 2) + Math.Pow(atEnd.Y - before.Y, 2) + Math.Pow(atEnd.Z - before.Z, 2));
            Assert.That(jump, Is.LessThan(0.01 * 15.9), "sin salto: Δ entre los dos últimos cuadros < 1 % del recorrido");
            d.Evaluate(end + 0.3, 2.12, cam, false, false);
            Assert.That(d.LastBreathingMultiplier, Is.EqualTo(0.5).Within(1e-12), "la mitad de S(0; 0,6)");
            d.Evaluate(end + 0.6, 2.42, cam, false, false);
            Assert.That(d.LastBreathingMultiplier, Is.EqualTo(1).Within(1e-12));
            Assert.That(ChaseCameraDirector.BreathingRampSeconds, Is.EqualTo(0.6));
        }

        [Test, Category("AC-SCN-02-04"), Category("AC-SCN-01-07")]
        public void ShowInPlaceHasTheRampToo()
        {
            var cam = SceneSeeds.Camera("TH");
            var d = new ChaseCameraDirector();
            d.ShowInPlace(5);
            Assert.That(d.Mode, Is.EqualTo(CameraMode.Contract));
            var pose = d.Evaluate(5, 40.3, cam, false, false);
            Assert.That(d.LastBreathingMultiplier, Is.EqualTo(0));
            Assert.That(pose.X, Is.EqualTo(0));
            d.Evaluate(5.6, 40.9, cam, false, false);
            Assert.That(d.LastBreathingMultiplier, Is.EqualTo(1));
        }

        [Test, Category("AC-SCN-02-05")]
        public void TheFlightPathNeverCrossesAnything()
        {
            var spec = FlightSpec.Default;
            var cam = SceneSeeds.Camera("TV");
            var maxSpeed = 0.0;
            var prev = ChaseCameraMotion.Flight(cam, 0, spec);
            for (var i = 1; i <= 1000; i++)
            {
                var u = i / 1000.0;
                var pose = ChaseCameraMotion.Flight(cam, u, spec);
                Assert.That(pose.Y, Is.GreaterThanOrEqualTo(2.2 - 1e-12), "sin pasar por debajo del agua");
                Assert.That(Math.Abs(pose.X), Is.LessThanOrEqualTo(3.0), "lejos de la orilla (|x| 9) y del casco");
                Assert.That(pose.Z, Is.LessThanOrEqualTo(1e-12), "detrás de la popa (z 5,3)");
                var dist = Math.Sqrt(Math.Pow(pose.X - prev.X, 2) + Math.Pow(pose.Y - prev.Y, 2) + Math.Pow(pose.Z - prev.Z, 2));
                maxSpeed = Math.Max(maxSpeed, dist / (spec.DurationSeconds / 1000));
                prev = pose;
            }

            TestContext.Out.WriteLine("velocidad máxima " + maxSpeed.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + " m/s");
            Assert.That(maxSpeed, Is.LessThanOrEqualTo(17.5), "sin acelerones (DN6a-08: ≈ 17,0)");
            Assert.That(maxSpeed, Is.GreaterThan(15.0));
        }

        [TestCaseSource(nameof(Sizes))]
        public void TheFlightStartsWideAndEndsExactlyInTheContract(string size)
        {
            var cam = SceneSeeds.Camera(size);
            var spec = FlightSpec.Default;
            var start = ChaseCameraMotion.Flight(cam, 0, spec);
            Assert.That(start.FocalDp, Is.EqualTo(1.25 * cam.Focal).Within(1e-12), "sin zoom brusco: ×1,25");
            Assert.That(start.HorizonDp, Is.EqualTo(cam.Height / 2).Within(1e-12));
            Assert.That(start.X, Is.EqualTo(3));
            Assert.That(start.Y, Is.EqualTo(9));
            Assert.That(start.Z, Is.EqualTo(-14));
            // Mirando al remero: guiñada −7,3° y cabeceo −20° (04-design-notes §11.3).
            Assert.That(start.YawDeg, Is.EqualTo(-7.27).Within(0.01));
            Assert.That(start.PitchDeg, Is.EqualTo(-19.95).Within(0.01));
            Assert.That(start.RollDeg, Is.EqualTo(0).Within(1e-9));
            var end = ChaseCameraMotion.Flight(cam, 1, spec);
            var contract = CameraPose.Contract(cam);
            Assert.That(end.X, Is.EqualTo(contract.X));
            Assert.That(end.Y, Is.EqualTo(contract.Y));
            Assert.That(end.Z, Is.EqualTo(contract.Z));
            Assert.That(end.YawDeg, Is.EqualTo(0));
            Assert.That(end.PitchDeg, Is.EqualTo(0));
            Assert.That(end.RollDeg, Is.EqualTo(0));
            Assert.That(end.FocalDp, Is.EqualTo(cam.Focal));
            Assert.That(end.HorizonDp, Is.EqualTo(cam.HorizonY));
            // El horizonte de la proyección empieza a moverse en 0,6 del recorrido.
            Assert.That(ChaseCameraMotion.Flight(cam, 0.6, spec).HorizonDp, Is.EqualTo(cam.Height / 2).Within(1e-12));
            Assert.That(ChaseCameraMotion.Flight(cam, 0.8, spec).HorizonDp, Is.EqualTo(cam.Height / 2 + (cam.HorizonY - cam.Height / 2) * 0.5).Within(1e-9));
        }

        [Test, Category("AC-SCN-02-05")]
        public void TheFlightUsesEaseInOutSineInPosition()
        {
            var cam = SceneSeeds.Camera("TV");
            var spec = FlightSpec.Default;
            foreach (var u in new[] { 0.1, 0.25, 0.5, 0.9 })
            {
                var s = (1 - Math.Cos(Math.PI * u)) / 2;
                ChaseCameraMotion.Bezier(spec, s, out var x, out var y, out var z);
                var pose = ChaseCameraMotion.Flight(cam, u, spec);
                Assert.That(pose.X, Is.EqualTo(x).Within(1e-12));
                Assert.That(pose.Y, Is.EqualTo(y).Within(1e-12));
                Assert.That(pose.Z, Is.EqualTo(z).Within(1e-12));
                Assert.That(pose.FocalDp, Is.EqualTo(cam.Focal * (1.25 - 0.25 * Easing.Evaluate(Tokens.EaseMove, u))).Within(1e-9), "la focal con ease.move");
            }

            Assert.That(MotionSpecs.FlightStartMs, Is.EqualTo(220));
            Assert.That(MotionSpecs.FlightDurationMs, Is.EqualTo(1600));
        }

        [Test, Category("AC-SCN-02-05"), Category("AC-APP-05-08")]
        public void WithReduceMotionThereIsNoFlight()
        {
            var cam = SceneSeeds.Camera("TV");
            var d = new ChaseCameraDirector();
            d.BeginFlight(0.22, true);
            Assert.That(d.Mode, Is.EqualTo(CameraMode.Contract));
            var pose = d.Evaluate(0.3, 0.3, cam, true, false);
            Assert.That(pose.Y, Is.EqualTo(2.2));
            Assert.That(pose.Z, Is.EqualTo(0));
            Assert.That(d.LastBreathingMultiplier, Is.EqualTo(0));
        }

        [Test, Category("AC-SCN-02-06")]
        public void TheExitRisesAndBacksOffFromTheCurrentPose()
        {
            var cam = SceneSeeds.Camera("TV");
            var d = new ChaseCameraDirector();
            d.ShowInPlace(0);
            var from = d.Evaluate(10, 10, cam, false, false);
            d.BeginExit(10, false);
            Assert.That(d.Mode, Is.EqualTo(CameraMode.Exit));
            var first = d.Evaluate(10, 10, cam, false, false);
            Assert.That(first.Y, Is.EqualTo(from.Y).Within(1e-12), "desde la pose de ese cuadro");
            var end = d.Evaluate(11.2, 11.2, cam, false, false);
            Assert.That(end.Y - from.Y, Is.EqualTo(1.5).Within(1e-12));
            Assert.That(end.Z - from.Z, Is.EqualTo(-4).Within(1e-12));
            var mid = d.Evaluate(10.6, 10.6, cam, false, false);
            Assert.That(mid.Y - from.Y, Is.EqualTo(1.5 * Easing.Evaluate(Tokens.EaseMove, 0.5)).Within(1e-12), "ease.move");
            Assert.That(d.LastBreathingMultiplier, Is.EqualTo(0));
            var at300 = ChaseCameraMotion.Exit(CameraPose.Contract(cam), 0.3);
            Assert.That(at300.Y - 2.2, Is.LessThan(0.1 * 1.5), "a los 300 ms recorrió poco: el negro la cubre a los 400");
        }

        [Test, Category("AC-SCN-02-06"), Category("AC-APP-05-08")]
        public void WithReduceMotionTheExitDoesNotMove()
        {
            var cam = SceneSeeds.Camera("TH");
            var d = new ChaseCameraDirector();
            d.ShowInPlace(0);
            var from = d.Evaluate(3, 3, cam, true, false);
            d.BeginExit(3, true);
            var later = d.Evaluate(4.5, 4.5, cam, true, false);
            Assert.That(later.Y, Is.EqualTo(from.Y));
            Assert.That(later.Z, Is.EqualTo(from.Z));
        }

        [Test, Category("AC-SCN-02-07")]
        public void TheAerialShotRisesBacksOffAndPitchesDown()
        {
            var cam = SceneSeeds.Camera("TV");
            var spec = AerialSpec.Default;
            Assert.That(spec.RiseM, Is.EqualTo(1.70));
            Assert.That(spec.BackM, Is.EqualTo(1.70));
            Assert.That(spec.PitchDeg, Is.EqualTo(-4));
            Assert.That(spec.Seconds, Is.EqualTo(6));
            var d = new ChaseCameraDirector();
            d.ShowInPlace(0);
            d.BeginAerial(30.3, false);
            Assert.That(d.Mode, Is.EqualTo(CameraMode.Aerial));
            var end = d.Evaluate(36.3, 36.3, cam, false, false);
            Assert.That(d.LastBreathingMultiplier, Is.EqualTo(0).Within(1e-12), "la respiración se apaga en esos 6 s");
            Assert.That(end.Y, Is.EqualTo(2.2 + 1.7).Within(1e-12));
            Assert.That(end.Z, Is.EqualTo(-1.7).Within(1e-12));
            Assert.That(end.PitchDeg, Is.EqualTo(-4).Within(1e-12));
            Assert.That(Math.Sqrt(1.7 * 1.7 + 1.7 * 1.7) / 6, Is.EqualTo(0.4).Within(0.001), "0,4 m/s de promedio");
            var later = d.Evaluate(50, 50, cam, false, false);
            var muchLater = d.Evaluate(80, 80, cam, false, false);
            Assert.That(later.Y, Is.EqualTo(end.Y).Within(1e-9));
            Assert.That(later.Z, Is.EqualTo(end.Z).Within(1e-9));
            Assert.That(muchLater.Y, Is.EqualTo(later.Y));
            Assert.That(muchLater.Z, Is.EqualTo(later.Z));
            Assert.That(muchLater.PitchDeg, Is.EqualTo(later.PitchDeg), "después, quieta");
            var mid = d.Evaluate(33.3, 33.3, cam, false, false);
            Assert.That(d.LastBreathingMultiplier, Is.EqualTo(1 - Easing.Evaluate(Tokens.EaseMove, 0.5)).Within(1e-12));
            Assert.That(mid.Y, Is.GreaterThan(2.2));

            // En TV el horizonte termina en 336,9 dp y la popa (0; 0,1; 5,3) en 552,9, dentro de la zona del bote.
            var projection = ChaseCameraProjection.Contract(cam);
            Assert.That(ChaseCameraProjection.TryProjectWithPose(projection, end, cam, 0, end.Y, 1e8, out _, out var horizon), Is.True);
            Assert.That(horizon, Is.EqualTo(336.9).Within(0.5));
            Assert.That(ChaseCameraProjection.TryProjectWithPose(projection, end, cam, 0, 0.1, LakeHull.Stern, out _, out var stern), Is.True);
            Assert.That(stern, Is.EqualTo(552.9).Within(1));
            Assert.That(stern, Is.LessThan(cam.RowerY + 0.15 * cam.Height), "dentro de la zona del bote");
        }

        [Test, Category("AC-SCN-02-07"), Category("AC-APP-05-08")]
        public void WithReduceMotionThereIsNoAerialShot()
        {
            var d = new ChaseCameraDirector();
            d.ShowInPlace(0);
            d.BeginAerial(10, true);
            Assert.That(d.Mode, Is.EqualTo(CameraMode.Contract));
        }

        [Test, Category("AC-SCN-02-08")]
        public void ThereIsOneCameraWithoutUserInput()
        {
            // El director no tiene ninguna entrada del usuario: su API pública son las órdenes de la coreografía.
            var methods = typeof(ChaseCameraDirector).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.DeclaredOnly);
            var names = new List<string>();
            foreach (var m in methods)
            {
                if (!m.IsSpecialName)
                {
                    names.Add(m.Name);
                }
            }

            Assert.That(names, Is.EquivalentTo(new[] { "Reset", "BeginFlight", "ShowInPlace", "BeginExit", "BeginAerial", "SetPaused", "BreathingMultiplier", "Evaluate" }));
        }

        private static IEnumerable<TestCaseData> Sizes()
        {
            foreach (var s in SceneSeeds.Sizes)
            {
                yield return new TestCaseData(s.Name).SetCategory("AC-SCN-02-04").SetCategory("AC-SCN-02-05");
            }
        }

        private static class LakeHull
        {
            public const double Stern = Core.Scene.Art.LakeArt.HullStern;
        }
    }
}
