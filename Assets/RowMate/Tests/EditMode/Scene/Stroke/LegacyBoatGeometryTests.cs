using NUnit.Framework;
using RowMate.Core.Scene;
using RowMate.Core.Scene.Stroke;

namespace RowMate.Tests.EditMode.Scene.Stroke
{
    // LBG · La geometría exagerada del legacy (scene/painters/boat_painter.dart:25-56; LT-96; DV-68, DV-77): solo contrato.
    [TestFixture]
    public sealed class LegacyBoatGeometryTests
    {
        // legacy: test/scene/boat_painter_test.dart:10 'geometría de la palada: catch hacia la cámara, pala hacia la proa'
        [TestCase(TestName = "geometría de la palada: catch hacia la cámara, pala hacia la proa", Category = "LT-96,AC-SCN-05-05")]
        public void CatchTowardTheCameraBladeTowardTheBow()
        {
            const double bz = SceneCamera.BoatZ;
            // Carro: en el catch está más cerca de la cámara (z menor) que en el finish
            Assert.That(LegacyBoatGeometry.SeatZ(0), Is.LessThan(LegacyBoatGeometry.SeatZ(0.36)));
            // Mango: en el catch más cerca de la cámara; en el finish hacia la proa
            Assert.That(LegacyBoatGeometry.Handle(1, 0).Z, Is.LessThan(bz));
            Assert.That(LegacyBoatGeometry.Handle(1, 0.36).Z, Is.GreaterThan(bz));
            // Pala: en el catch hacia la proa, en el finish hacia la popa
            Assert.That(LegacyBoatGeometry.BladeTip(1, 0).Z, Is.GreaterThan(bz + 2));
            Assert.That(LegacyBoatGeometry.BladeTip(1, 0.36).Z, Is.LessThan(bz));
            // Lados espejados
            Assert.That(LegacyBoatGeometry.BladeTip(-1, 0.1).X, Is.EqualTo(-LegacyBoatGeometry.BladeTip(1, 0.1).X));
        }

        [Test, Category("AC-SCN-05-05")]
        public void TheLegacyMeasuresStayOnlyInTheContract()
        {
            // :30-35: pedalera a 1,15 m del perno y carro de 0,6 m; el mango a 0,45 m; la punta a −0,05 / 0,35.
            Assert.That(LegacyBoatGeometry.FeetZ, Is.EqualTo(SceneCamera.BoatZ - 1.15));
            Assert.That(LegacyBoatGeometry.SeatZ(0.36) - LegacyBoatGeometry.SeatZ(0), Is.EqualTo(0.6).Within(1e-12));
            Assert.That(LegacyBoatGeometry.Handle(-1, 0.2).Y, Is.EqualTo(0.45));
            Assert.That(LegacyBoatGeometry.BladeTip(1, 0.2).Y, Is.EqualTo(-0.05));
            Assert.That(LegacyBoatGeometry.BladeTip(1, 0.5).Y, Is.EqualTo(0.35));
        }
    }
}
