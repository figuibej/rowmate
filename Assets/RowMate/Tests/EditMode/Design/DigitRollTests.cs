using NUnit.Framework;
using RowMate.Core.Design;

namespace RowMate.Tests.EditMode.Design
{
    // DigitRoll = el rodillo de dígitos (ANIMATION_BIBLE §2.4; 04-design-notes §5.1, DN-08): qué celdas cambian, hacia dónde
    // y con qué curvas. El número que se ve es siempre el real (AC-BLE-11-11).
    [TestFixture]
    public sealed class DigitRollTests
    {
        [Test, Category("AC-BLE-11-11")]
        public void EndsOnTheExactValue()
        {
            foreach (var direction in new[] { -1, 0, 1 })
            {
                foreach (var elapsed in new[] { 180.0, 181.0, 500.0, 1e6 })
                {
                    DigitRoll.Evaluate(elapsed, direction, false, out var outY, out var outA, out var inY, out var inA);
                    Assert.That(inY, Is.EqualTo(0.0), "el entrante en su lugar");
                    Assert.That(inA, Is.EqualTo(1.0), "y opaco");
                    Assert.That(outA, Is.EqualTo(0.0), "el saliente invisible: se ve exactamente la lectura");
                }
            }

            Assert.That(DigitRoll.IsFinished(180, false), Is.True);
            Assert.That(DigitRoll.IsFinished(179.9, false), Is.False);
            Assert.That(DigitRoll.DurationMs, Is.LessThanOrEqualTo(180.0), "≤ 180 ms");
        }

        [Test, Category("AC-BLE-11-11")]
        public void OnlyChangedCellsRoll()
        {
            // 149 → 151: el "1" de la izquierda no cambia (AC-APP-05-04: los dígitos que no cambian no se mueven).
            var plan = DigitRoll.Plan("149", "151", 149, 151);
            Assert.That(plan.Length, Is.EqualTo(3));
            Assert.That(plan.IsChanged(0), Is.False);
            Assert.That(plan.IsChanged(1), Is.True);
            Assert.That(plan.IsChanged(2), Is.True);
            Assert.That(plan.Direction, Is.EqualTo(1));

            var same = DigitRoll.Plan("02:13", "02:13", 133, 133);
            Assert.That(same.HasChanges, Is.False, "un valor que no cambió no se mueve");
            Assert.That(same.Direction, Is.EqualTo(0));

            // Largo distinto: alineado a la derecha; las celdas nuevas cambian (9m → 13m).
            var longer = DigitRoll.Plan("9m", "13m", 9, 13);
            Assert.That(longer.PreviousLength, Is.EqualTo(2));
            Assert.That(longer.IsChanged(0), Is.True, "el 1 es nuevo");
            Assert.That(longer.PreviousCellFor(0), Is.EqualTo(-1));
            Assert.That(longer.IsChanged(1), Is.True, "9 → 3");
            Assert.That(longer.OutgoingAt(1), Is.EqualTo('9'));
            Assert.That(longer.IncomingAt(1), Is.EqualTo('3'));
            Assert.That(longer.IsChanged(2), Is.False, "la m no se mueve");
            Assert.That(longer.RemovedCells, Is.EqualTo(0));

            var shorter = DigitRoll.Plan("100", "99", 100, 99);
            Assert.That(shorter.RemovedCells, Is.EqualTo(1), "la celda que se va solo sale");
            Assert.That(shorter.Direction, Is.EqualTo(-1));
            Assert.That(shorter.IsChanged(0), Is.True);
            Assert.That(shorter.IsChanged(1), Is.True);
        }

        [Test, Category("AC-BLE-11-11")]
        public void DirectionFollowsTheValueAndMarkersAreZero()
        {
            Assert.That(DigitRoll.Plan("226m", "229m", 226, 229).Direction, Is.EqualTo(1), "sube: los dígitos suben");
            Assert.That(DigitRoll.Plan("02:13", "02:32", 133, 152).Direction, Is.EqualTo(1), "split más lento = valor mayor");
            Assert.That(DigitRoll.Plan("150", "100", 150, 100).Direction, Is.EqualTo(-1));
            // Un marcador sin dígitos vale 0: al apagarse bajan, al encenderse suben (aunque el número pasado diga otra cosa).
            Assert.That(DigitRoll.Plan("02:13", "--:--", 133, 999).Direction, Is.EqualTo(-1));
            Assert.That(DigitRoll.Plan("--", "142", -5, 142).Direction, Is.EqualTo(1));
            Assert.That(DigitRoll.Plan("--", "--", 0, 0).HasChanges, Is.False);
        }

        [Test, Category("AC-BLE-11-11")]
        public void CurvesPerCell()
        {
            // 04-design-notes §5.1: saliente −dir·0,35·E(u) y alfa 1 − E(min(1, u/0,6)) (desaparece a los 108 ms); entrante
            // +dir·0,35·(1 − E(u)) y alfa E((u − 0,15)/0,85) (empieza a los 27 ms).
            DigitRoll.Evaluate(0, 1, false, out var outY, out var outA, out var inY, out var inA);
            Assert.That(outY, Is.EqualTo(0.0));
            Assert.That(outA, Is.EqualTo(1.0));
            Assert.That(inY, Is.EqualTo(0.35).Within(1e-12), "el dígito nuevo entra desde abajo cuando el valor sube");
            Assert.That(inA, Is.EqualTo(0.0));

            DigitRoll.Evaluate(27, 1, false, out _, out _, out _, out inA);
            Assert.That(inA, Is.EqualTo(0.0).Within(1e-9), "el entrante empieza a aparecer a los 27 ms");
            DigitRoll.Evaluate(108, -1, false, out outY, out outA, out inY, out _);
            Assert.That(outA, Is.EqualTo(0.0).Within(1e-9), "el saliente desaparece a los 108 ms");
            Assert.That(outY, Is.GreaterThan(0.0), "bajando, el saliente se va hacia abajo");
            Assert.That(inY, Is.LessThan(0.0), "y el entrante llega desde arriba");

            DigitRoll.Evaluate(90, 1, false, out outY, out outA, out inY, out inA);
            Assert.That(outY, Is.InRange(-0.35, 0.0));
            Assert.That(inY, Is.InRange(0.0, 0.35));
            Assert.That(outA, Is.InRange(0.0, 1.0));
            Assert.That(inA, Is.InRange(0.0, 1.0));

            DigitRoll.Evaluate(90, 0, false, out outY, out _, out inY, out _);
            Assert.That(outY, Is.EqualTo(0.0), "sin sentido, sin desplazamiento (solo fundido)");
            Assert.That(inY, Is.EqualTo(0.0));
        }

        [Test, Category("AC-APP-05-08")]
        public void ReducedMotionIsAnEightyMsFade()
        {
            DigitRoll.Evaluate(40, 1, true, out var outY, out var outA, out var inY, out var inA);
            Assert.That(outY, Is.EqualTo(0.0), "sin desplazamiento");
            Assert.That(inY, Is.EqualTo(0.0));
            Assert.That(outA, Is.EqualTo(0.5).Within(1e-12), "fundido lineal");
            Assert.That(inA, Is.EqualTo(0.5).Within(1e-12));
            DigitRoll.Evaluate(80, 1, true, out _, out outA, out _, out inA);
            Assert.That(outA, Is.EqualTo(0.0));
            Assert.That(inA, Is.EqualTo(1.0), "a los 80 ms, el valor exacto");
            Assert.That(DigitRoll.IsFinished(80, true), Is.True);
            Assert.That(MotionSpecs.ReducedFadeMs, Is.EqualTo(80.0));
        }

        [Test]
        public void WidthInterpolatesWithTheRoll()
        {
            Assert.That(DigitRoll.WidthAt(0, 80, 120), Is.EqualTo(80.0));
            Assert.That(DigitRoll.WidthAt(180, 80, 120), Is.EqualTo(120.0));
            Assert.That(DigitRoll.WidthAt(90, 80, 120), Is.InRange(100.0, 120.0), "ease.enter: más de la mitad a la mitad");
        }
    }
}
