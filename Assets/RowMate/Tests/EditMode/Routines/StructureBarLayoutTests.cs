using NUnit.Framework;
using RowMate.Core.Routines;

namespace RowMate.Tests.EditMode.Routines
{
    // SBL: geometría de la mini-barra (DN3-07; 03-rutinas/04-design-notes §3 y §4.3), con el ancho de columna de TV (255).
    [TestFixture]
    public sealed class StructureBarLayoutTests
    {
        private const double Tv = 255;

        [Test, Category("AC-RUT-10-11")]
        public void WidthCapacityOverflowAndEmptyTrack()
        {
            Assert.That(StructureBarLayout.For(1, Tv).Capacity, Is.EqualTo(64), "⌊(255 + 2) / 4⌋");
            Check(1, 1, 24, false);
            Check(7, 7, 24, false);
            Check(8, 8, 24, false);
            Assert.That(8 * 24 + 7 * 2, Is.EqualTo(206), "Escalera SPM: 206 dp");
            Check(10, 10, 23.7, false);
            Check(14, 14, 16.357142857142858, false);
            Check(64, 64, 2.015625, false);
            Check(65, 64, 2, true);
            Check(2000000000, 64, 2, true);
            var empty = StructureBarLayout.For(0, Tv);
            Assert.That(empty.EmptyTrack, Is.True, "solo series ×0: pista vacía");
            Assert.That(empty.Drawn, Is.EqualTo(0));
            Assert.That(empty.Overflow, Is.False);
            Assert.That(StructureBarLayout.For(5, Tv).EmptyTrack, Is.False);
        }

        // Capacidades de 04-design-notes §3 en los cuatro perfiles (columna de texto de la tarjeta).
        [TestCase(255, 64, Category = "AC-RUT-10-11")]
        [TestCase(758, 190, Category = "AC-RUT-10-11")]
        [TestCase(484, 121, Category = "AC-RUT-10-11")]
        [TestCase(1124, 281, Category = "AC-RUT-10-11")]
        public void CapacityByProfile(double width, int capacity)
        {
            Assert.That(StructureBarLayout.For(1, width).Capacity, Is.EqualTo(capacity));
        }

        // Antes del primer layout (W = 0) no hay nada que dibujar, ni siquiera la pista.
        [Test, Category("AC-RUT-10-11")]
        public void NothingBeforeTheFirstLayout()
        {
            foreach (var total in new long[] { 0, 1, 2000000000 })
            {
                var g = StructureBarLayout.For(total, 0);
                Assert.That(g.Capacity, Is.EqualTo(0));
                Assert.That(g.Drawn, Is.EqualTo(0));
                Assert.That(g.Overflow, Is.False);
                Assert.That(g.EmptyTrack, Is.False);
                Assert.That(g.SegmentWidth, Is.EqualTo(0));
            }

            Assert.That(StructureBarLayout.For(3, double.NaN).Drawn, Is.EqualTo(0));
        }

        // Con desborde, los últimos 6 dibujados se apagan 6/7 … 1/7; sin desborde, todos enteros.
        [Test, Category("AC-RUT-10-11")]
        public void OverflowFadesTheLastSix()
        {
            var g = StructureBarLayout.For(2000000000, Tv);
            Assert.That(StructureBarLayout.SegmentAlpha(g, 0), Is.EqualTo(1));
            Assert.That(StructureBarLayout.SegmentAlpha(g, 57), Is.EqualTo(1));
            Assert.That(StructureBarLayout.SegmentAlpha(g, 58), Is.EqualTo(6 / 7.0).Within(1e-12));
            Assert.That(StructureBarLayout.SegmentAlpha(g, 63), Is.EqualTo(1 / 7.0).Within(1e-12));
            var fits = StructureBarLayout.For(14, Tv);
            Assert.That(StructureBarLayout.SegmentAlpha(fits, 13), Is.EqualTo(1));
            Assert.That(StructureBarLayout.SegmentHeightDp, Is.EqualTo(4));
            Assert.That(StructureBarLayout.SegmentRadiusDp, Is.EqualTo(2));
            Assert.That(StructureBarLayout.SegmentGapDp, Is.EqualTo(2));
            Assert.That(StructureBarLayout.SegmentMaxWidthDp, Is.EqualTo(24));
            Assert.That(StructureBarLayout.SegmentMinWidthDp, Is.EqualTo(2));
            Assert.That(StructureBarLayout.OverflowFadeSegments, Is.EqualTo(6));
        }

        private static void Check(long total, int drawn, double width, bool overflow)
        {
            var g = StructureBarLayout.For(total, Tv);
            Assert.That(g.Drawn, Is.EqualTo(drawn), total + " tramos: dibujados");
            Assert.That(g.SegmentWidth, Is.EqualTo(width).Within(1e-9), total + " tramos: ancho");
            Assert.That(g.Overflow, Is.EqualTo(overflow), total + " tramos: desborde");
            Assert.That(g.EmptyTrack, Is.False);
            Assert.That(g.Drawn * g.SegmentWidth + (g.Drawn - 1) * StructureBarLayout.SegmentGapDp, Is.LessThanOrEqualTo(Tv + 1e-9), "nunca sale de la columna");
        }
    }
}
