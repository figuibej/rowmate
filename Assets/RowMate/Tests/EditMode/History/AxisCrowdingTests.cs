using System;
using NUnit.Framework;
using RowMate.Core.History;

namespace RowMate.Tests.EditMode.History
{
    // AX · Etiquetas de eje sin encimarse y rango mínimo del split (It. 14, #32, AC-DEBT-07, DV-137; DT14-28).
    [TestFixture]
    public sealed class AxisCrowdingTests
    {
        [Test, Category("AC-DEBT-07")]
        public void IntermediateTouchingAnEdgeIsDropped()
        {
            // X de 0:00 a 5:00 con marcas cada 1:00 y "3:44" al lado de "5:00" (CAP-06 de la 9): 3:44 cae a 2 dp del borde → se va.
            var centers = new double[] { 0, 60, 120, 180, 224, 250 };
            var widths = new double[] { 24, 24, 24, 24, 24, 24 };
            var visible = new bool[6];
            var kept = AxisTicks.DropCrowded(centers, widths, visible, 6);
            Assert.That(visible, Is.EqualTo(new[] { true, true, true, true, false, true }));
            Assert.That(kept, Is.EqualTo(5));
        }

        [Test, Category("AC-DEBT-07")]
        public void ExactlyFourDpIsKeptAndEdgesAlwaysStay()
        {
            var visible = new bool[3];
            AxisCrowding.DropCrowded(new double[] { 0, 28, 56 }, new double[] { 24, 24, 24 }, visible, 3);
            Assert.That(visible, Is.EqualTo(new[] { true, true, true }), "28 − 24 = 4 dp: alcanza");
            AxisCrowding.DropCrowded(new double[] { 0, 27.9, 200 }, new double[] { 24, 24, 24 }, visible, 3);
            Assert.That(visible, Is.EqualTo(new[] { true, false, true }), "pegado al primero");
            var two = new bool[2];
            AxisCrowding.DropCrowded(new double[] { 0, 5 }, new double[] { 24, 24 }, two, 2);
            Assert.That(two, Is.EqualTo(new[] { true, true }), "los bordes nunca se sacan");
            // Eje Y: "25" con "26" (alto de etiqueta 14 dp; el 25 a 6 dp del máximo).
            var y = new bool[4];
            AxisCrowding.DropCrowded(new double[] { 0, 60, 120, 126 }, new double[] { 14, 14, 14, 14 }, y, 4);
            Assert.That(y, Is.EqualTo(new[] { true, true, false, true }));
            Assert.Throws<ArgumentOutOfRangeException>(() => AxisCrowding.DropCrowded(new double[1], new double[1], new bool[1], 2));
        }

        [Test, Category("AC-DEBT-07")]
        public void ConstantSplitOpensToPlusMinusFive()
        {
            SplitRange.Widen(133, 133, out var min, out var max);
            Assert.That(min, Is.EqualTo(128));
            Assert.That(max, Is.EqualTo(138));
            SplitRange.Widen(130, 136, out min, out max);
            Assert.That(min, Is.EqualTo(128));
            Assert.That(max, Is.EqualTo(138));
            SplitRange.Widen(120, 140, out min, out max);
            Assert.That(min, Is.EqualTo(120), "≥ 10 s: sin cambios");
            Assert.That(max, Is.EqualTo(140));
            SplitRange.Widen(2, 3, out min, out max);
            Assert.That(min, Is.EqualTo(0), "no baja de 0");
            Assert.That(max, Is.EqualTo(10));
            Assert.Throws<ArgumentException>(() => SplitRange.Widen(5, 1, out _, out _));
        }
    }
}
