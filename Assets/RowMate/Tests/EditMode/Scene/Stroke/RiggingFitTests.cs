using NUnit.Framework;
using RowMate.Core.Scene.Stroke;

namespace RowMate.Tests.EditMode.Scene.Stroke
{
    // RGF · El ajuste del bote al cuerpo (DA7-01; DT7-02; AC-SCN-05-06; ANIMATION_BIBLE §7.4): con el maniquí de 1,85 m y con un rig de
    // 1,75 m, canillas verticales y brazos casi estirados en el catch, piernas a 178° en el finish, el puño a la altura de las
    // costillas bajas y el recorrido dentro de los rieles.
    [TestFixture]
    public sealed class RiggingFitTests
    {
        [Test, Category("AC-SCN-05-06")]
        public void TheMannequinOf185()
        {
            var fit = RiggingFit.Solve(BoatSpec.Real, RowerSpec.Mannequin);
            Assert.That(RowerSpec.Mannequin.Height, Is.EqualTo(1.85).Within(0.001));
            Assert.That(fit.HeelZ, Is.EqualTo(-0.17).Within(0.05), "talones");
            Assert.That(fit.HipZCatch, Is.EqualTo(0.18).Within(0.05), "cadera en el catch");
            Assert.That(fit.HipZFinish, Is.EqualTo(0.72).Within(0.05), "cadera en el finish");
            Assert.That(fit.Travel, Is.EqualTo(0.54).Within(0.03), "recorrido ≈ 0,54 (entre 0,52 y 0,62)");
            Check(RowerSpec.Mannequin, fit);
        }

        [Test, Category("AC-SCN-05-06")]
        public void ARigOf175()
        {
            var rower = RowerSpec.Mannequin.Scaled(1.75 / 1.85);
            Assert.That(rower.Height, Is.EqualTo(1.75).Within(0.001));
            var fit = RiggingFit.Solve(BoatSpec.Real, rower);
            Check(rower, fit);
            var tall = RiggingFit.Solve(BoatSpec.Real, RowerSpec.Mannequin);
            Assert.That(fit.Travel, Is.LessThan(tall.Travel), "un cuerpo más chico recorre menos");
        }

        [Test, Category("AC-SCN-05-06")]
        public void TheLegacyStretcherIsImpossibleWithRealOars()
        {
            // DT7-02: con la pedalera a 1,15 m del perno (z −1,15) la cadera en el catch quedaría detrás del puño (−0,72).
            var rower = RowerSpec.Mannequin;
            var thigh = rower.Thigh;
            var hipFromLegacyFeet = -1.15 + 0.36;
            var grip = ScullingKinematics.GripTarget(0, BoatSpec.Real, 1, false);
            Assert.That(thigh, Is.GreaterThan(0.36));
            Assert.That(hipFromLegacyFeet, Is.LessThan(grip.Z), "la cadera detrás del puño");
        }

        private static void Check(in RowerSpec rower, in RigFit fit)
        {
            var boat = BoatSpec.Real;
            Assert.That(fit.ArmExtensionCatch, Is.GreaterThanOrEqualTo(0.97), "brazos ≥ 97 %");
            Assert.That(fit.GripToRibsFinish, Is.EqualTo(0).Within(0.03), "puño a la altura de las costillas bajas");
            Assert.That(fit.Travel, Is.LessThanOrEqualTo(boat.RailLength - 0.10), "el carro entra en los rieles");
            Assert.That(fit.HeelY, Is.EqualTo(boat.SeatTopY - 0.17).Within(1e-12), "talón 0,17 bajo el asiento");
            var pose = ScullingPose.Create();
            ScullingKinematics.Evaluate(0, 0, boat, rower, fit, RowerIdleSample.None, ref pose);
            Assert.That(PoseAngles.ShinTiltDeg(pose.Joints, true), Is.EqualTo(0).Within(8), "canillas verticales en el catch");
            Assert.That(pose.LeftReachError, Is.LessThan(0.005), "las manos llegan al puño en el catch");
            ScullingKinematics.Evaluate(0.36, 0, boat, rower, fit, RowerIdleSample.None, ref pose);
            Assert.That(PoseAngles.KneeDeg(pose.Joints, true), Is.EqualTo(RiggingFit.FinishKneeDeg).Within(0.5), "piernas a 178° en el finish");
        }
    }
}
