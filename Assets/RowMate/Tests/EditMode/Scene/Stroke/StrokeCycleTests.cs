using NUnit.Framework;
using RowMate.Core.Scene;

namespace RowMate.Tests.EditMode.Scene.Stroke
{
    // SCY · La secuencia de la palada del contrato (scene/stroke_cycle.dart:5-51; F-SCN-05; LT-92 a LT-95): los cuatro casos de
    // test/scene/stroke_cycle_test.dart portados sin cambiar un número.
    [TestFixture]
    public sealed class StrokeCycleTests
    {
        // legacy: test/scene/stroke_cycle_test.dart:5 'catch: piernas plegadas, brazos estirados, pala en el agua'
        [TestCase(TestName = "catch: piernas plegadas, brazos estirados, pala en el agua", Category = "LT-92,AC-SCN-05-01")]
        public void Catch()
        {
            Assert.That(StrokeCycle.Legs(0), Is.EqualTo(0));
            Assert.That(StrokeCycle.Arms(0), Is.EqualTo(0));
            Assert.That(StrokeCycle.Back(0), Is.EqualTo(0));
            Assert.That(StrokeCycle.BladeInWater(0), Is.True);
            Assert.That(StrokeCycle.OarSweep(0), Is.LessThan(0));
            Assert.That(StrokeCycle.TorsoLean(0), Is.GreaterThan(0), "inclinado hacia la cámara (popa)");
        }

        // legacy: test/scene/stroke_cycle_test.dart:14 'finish: piernas extendidas, brazos flexionados, pala sale'
        [TestCase(TestName = "finish: piernas extendidas, brazos flexionados, pala sale", Category = "LT-93,AC-SCN-05-02")]
        public void Finish()
        {
            Assert.That(StrokeCycle.Legs(0.36), Is.EqualTo(1));
            Assert.That(StrokeCycle.Arms(0.36), Is.EqualTo(1));
            Assert.That(StrokeCycle.Back(0.36), Is.EqualTo(1));
            Assert.That(StrokeCycle.OarSweep(0.36), Is.GreaterThan(0));
            Assert.That(StrokeCycle.BladeInWater(0.39), Is.False);
            Assert.That(StrokeCycle.TorsoLean(0.36), Is.LessThan(0));
        }

        // legacy: test/scene/stroke_cycle_test.dart:23 'en el drive: piernas antes que espalda, espalda antes que brazos'
        [TestCase(TestName = "en el drive: piernas antes que espalda, espalda antes que brazos", Category = "LT-94,AC-SCN-05-03")]
        public void DriveOrder()
        {
            Assert.That(StrokeCycle.Legs(0.12), Is.GreaterThan(StrokeCycle.Back(0.12)));
            Assert.That(StrokeCycle.Back(0.12), Is.GreaterThan(StrokeCycle.Arms(0.12)));
            Assert.That(StrokeCycle.Arms(0.12), Is.EqualTo(0));
        }

        // legacy: test/scene/stroke_cycle_test.dart:29 'la recuperación vuelve todo al catch'
        [TestCase(TestName = "la recuperación vuelve todo al catch", Category = "LT-95,AC-SCN-05-04")]
        public void RecoveryReturnsToTheCatch()
        {
            Assert.That(StrokeCycle.Legs(0.999), Is.EqualTo(0).Within(1e-3));
            Assert.That(StrokeCycle.Arms(0.999), Is.EqualTo(0));
            Assert.That(StrokeCycle.Back(0.999), Is.EqualTo(0));
            Assert.That(StrokeCycle.OarSweep(0.999), Is.EqualTo(StrokeCycle.CatchSweep).Within(1e-3));
            Assert.That(StrokeCycle.BladeFeathered(0.7), Is.True);
        }

        [Test, Category("AC-SCN-05-02"), Category("AC-SCN-05-07")]
        public void TheConstantsAndTheStillFinishAreTheLegacyOnes()
        {
            // stroke_cycle.dart:9-12, :41-45: −55° / +35°, quieto entre 0,36 y 0,40; la pala adentro mientras p < 0,38.
            Assert.That(StrokeCycle.CatchSweep, Is.EqualTo(-55 * System.Math.PI / 180));
            Assert.That(StrokeCycle.FinishSweep, Is.EqualTo(35 * System.Math.PI / 180));
            Assert.That(StrokeCycle.OarSweep(0.37), Is.EqualTo(StrokeCycle.FinishSweep));
            Assert.That(StrokeCycle.BladeInWater(0.3799), Is.True);
            Assert.That(StrokeCycle.BladeInWater(0.38), Is.False);
            Assert.That(StrokeCycle.TorsoLean(0), Is.EqualTo(25 * System.Math.PI / 180), "el +25/−20 del contrato (DV-74)");
        }
    }
}
