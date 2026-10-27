using System;
using System.Globalization;
using NUnit.Framework;
using RowMate.Core.Common;
using RowMate.Core.Text;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Text
{
    // DartMath = round(), double.parse(toStringAsFixed(n)), % y ~/ de Dart (ADR 012 §2-§3). AC-BASE-01, AC-BASE-03.
    [TestFixture]
    public sealed class DartMathTests
    {
        // Tabla de AC-BASE-01 ("redondeo a entero") y ADR 012 §5.
        [TestCase(0.5, 1L, Category = "AC-BASE-01")]
        [TestCase(2.5, 3L, Category = "AC-BASE-01")]
        [TestCase(-0.5, -1L, Category = "AC-BASE-01")]
        [TestCase(120.5, 121L, Category = "AC-BASE-01")]
        [TestCase(-0.05, 0L, Category = "AC-BASE-01")]
        [TestCase(0.49999999999999994, 0L, Category = "AC-BASE-01")]
        [TestCase(1.5, 2L, Category = "AC-BASE-01")]
        [TestCase(-1.5, -2L, Category = "AC-BASE-01")]
        [TestCase(4503599627370495.5, 4503599627370496L, Category = "AC-BASE-01")]
        [TestCase(-0.0, 0L, Category = "AC-BASE-01")]
        [TestCase(142.4, 142L, Category = "AC-BASE-01")]
        [TestCase(1e18, 1000000000000000000L, Category = "AC-BASE-01")]
        public void SpecTable(double x, long expected)
        {
            Assert.That(DartMath.Round(x), Is.EqualTo(expected));
        }

        [Test, Category("AC-BASE-01")]
        public void RoundRejectsValuesWithoutAnInteger()
        {
            Assert.Throws<ArgumentException>(() => DartMath.Round(double.NaN));
            Assert.Throws<ArgumentException>(() => DartMath.Round(double.PositiveInfinity));
            Assert.Throws<ArgumentException>(() => DartMath.Round(double.NegativeInfinity));
            Assert.Throws<ArgumentOutOfRangeException>(() => DartMath.Round(1e19));
            Assert.Throws<ArgumentOutOfRangeException>(() => DartMath.Round(-1e19));
        }

        // Tabla de AC-BASE-01 ("1 decimal y de vuelta a número", rowing_simulator.dart:60) y ADR 012 §5.
        [TestCase(24.25, 1, 24.3, Category = "AC-BASE-01")]
        [TestCase(24.35, 1, 24.4, Category = "AC-BASE-01")]
        [TestCase(23.95, 1, 23.9, Category = "AC-BASE-01")]
        [TestCase(23.99999999999998, 1, 24.0, Category = "AC-BASE-01")]
        [TestCase(2.5, 0, 3.0, Category = "AC-BASE-01")]
        [TestCase(1e21, 2, 1e21, Category = "AC-BASE-01")]
        [TestCase(1.005, 2, 1.0, Category = "AC-BASE-01")]
        public void RoundToFixed(double x, int fractionDigits, double expected)
        {
            Assert.That(BitConverter.DoubleToInt64Bits(DartMath.RoundToFixed(x, fractionDigits)), Is.EqualTo(BitConverter.DoubleToInt64Bits(expected)));
        }

        [Test, Category("AC-BASE-01")]
        public void RoundToFixedKeepsTheSignOfZeroAndNonFinite()
        {
            Assert.That(BitConverter.DoubleToInt64Bits(DartMath.RoundToFixed(-0.04, 1)), Is.EqualTo(BitConverter.DoubleToInt64Bits(-0.0)));
            Assert.That(double.IsNaN(DartMath.RoundToFixed(double.NaN, 1)), Is.True);
            Assert.That(DartMath.RoundToFixed(double.PositiveInfinity, 1), Is.EqualTo(double.PositiveInfinity));
            Assert.Throws<ArgumentOutOfRangeException>(() => DartMath.RoundToFixed(1.0, 21));
        }

        [TestCase(-7L, 3L, 2L)]
        [TestCase(-65L, 60L, 55L)]
        [TestCase(7L, -3L, 1L)]
        [TestCase(-7L, -3L, 2L)]
        [TestCase(0L, 5L, 0L)]
        [TestCase(-5L, 60L, 55L)]
        [TestCase(long.MinValue, -1L, 0L)]
        public void ModIntegerIsNeverNegative(long a, long b, long expected)
        {
            Assert.That(DartMath.Mod(a, b), Is.EqualTo(expected));
        }

        [TestCase(-7.5, 4096.0, 4088.5)]
        [TestCase(7.5, -2.0, 1.5)]
        [TestCase(-7.5, -2.0, 0.5)]
        [TestCase(-0.0, 5.0, 0.0)]
        [TestCase(-6.0, 3.0, 0.0)]
        public void ModDoubleLikeTheDartVm(double a, double b, double expected)
        {
            Assert.That(BitConverter.DoubleToInt64Bits(DartMath.Mod(a, b)), Is.EqualTo(BitConverter.DoubleToInt64Bits(expected)));
        }

        [TestCase(-65L, 60L, -1L)]
        [TestCase(65L, 60L, 1L)]
        [TestCase(-7L, 2L, -3L)]
        [TestCase(-90L, 60L, -1L)]
        public void TruncDivTruncatesTowardZero(long a, long b, long expected)
        {
            Assert.That(DartMath.TruncDiv(a, b), Is.EqualTo(expected));
        }

        [Test]
        public void TruncDivWithDoubles()
        {
            Assert.That(DartMath.TruncDiv(7.5, 2.0), Is.EqualTo(3L));
            Assert.That(DartMath.TruncDiv(-7.5, 2.0), Is.EqualTo(-3L));
            Assert.Throws<ArgumentException>(() => DartMath.TruncDiv(1.0, 0.0));
            Assert.Throws<DivideByZeroException>(() => DartMath.TruncDiv(1L, 0L));
            Assert.Throws<DivideByZeroException>(() => DartMath.Mod(1L, 0L));
        }

        [Test, Category("AC-BASE-01"), Category("AC-BASE-03")]
        public void MatchesOracle_Round()
        {
            GoldenVectors.CheckRows("fmt.json", "round", row =>
            {
                var expected = row.Int("out");
                var actual = DartMath.Round(row.F64("x"));
                return actual == expected ? null : "esperado " + expected + ", obtenido " + actual;
            });
        }

        [Test, Category("AC-BASE-01"), Category("AC-BASE-03")]
        public void MatchesOracle_ParseFixed()
        {
            GoldenVectors.CheckRows("fmt.json", "parseFixed", row =>
            {
                var expected = row.F64("out");
                var actual = DartMath.RoundToFixed(row.F64("x"), (int)row.Int("n"));
                return SameDouble(expected, actual) ? null : "esperado " + Bits(expected) + ", obtenido " + Bits(actual);
            });
        }

        [Test, Category("AC-BASE-01"), Category("AC-BASE-03")]
        public void MatchesOracle_Mod()
        {
            GoldenVectors.CheckRows("fmt.json", "mod", row =>
            {
                var expected = row.F64("out");
                var actual = DartMath.Mod(row.F64("a"), row.F64("b"));
                return SameDouble(expected, actual) ? null : "esperado " + Bits(expected) + ", obtenido " + Bits(actual);
            });
        }

        [Test, Category("AC-BASE-01"), Category("AC-BASE-03")]
        public void MatchesOracle_ModInt()
        {
            GoldenVectors.CheckRows("fmt.json", "modInt", row =>
            {
                var expected = row.Int("out");
                var actual = DartMath.Mod(row.Int("a"), row.Int("b"));
                return actual == expected ? null : "esperado " + expected + ", obtenido " + actual;
            });
        }

        [Test, Category("AC-BASE-01"), Category("AC-BASE-03")]
        public void MatchesOracle_TruncDiv()
        {
            GoldenVectors.CheckRows("fmt.json", "truncDiv", row =>
            {
                var expected = row.Int("out");
                var actual = DartMath.TruncDiv(row.Int("a"), row.Int("b"));
                return actual == expected ? null : "esperado " + expected + ", obtenido " + actual;
            });
            GoldenVectors.CheckRows("fmt.json", "truncDivF64", row =>
            {
                var expected = row.Int("out");
                var actual = DartMath.TruncDiv(row.F64("a"), row.F64("b"));
                return actual == expected ? null : "esperado " + expected + ", obtenido " + actual;
            });
        }

#if NET5_0_OR_GREATER
        [Test, Category("AC-BASE-01")]
        public void RoundToFixedEqualsParsingTheFixedText()
        {
            // Propiedad interna (02-tech-design §3.1, punto 3): RoundToFixed(x, n) == double.Parse(Fixed(x, n)) en .NET 8
            // (el parser de .NET 8 es correctamente redondeado; el de Mono no se usa para esto, R-T5).
            var random = new SystemRandom(99);
            for (var i = 0; i < 10000; i++)
            {
                var x = (random.NextDouble() * 2 - 1) * Math.Pow(10, random.NextDouble() * 10 - 3);
                var n = i % 4;
                var expected = double.Parse(Fmt.Fixed(x, n), NumberStyles.Float, CultureInfo.InvariantCulture);
                Assert.That(Bits(DartMath.RoundToFixed(x, n)), Is.EqualTo(Bits(expected)), "x = " + x.ToString("R", CultureInfo.InvariantCulture) + ", n = " + n);
            }
        }
#endif

        private static bool SameDouble(double a, double b)
        {
            return (double.IsNaN(a) && double.IsNaN(b)) || BitConverter.DoubleToInt64Bits(a) == BitConverter.DoubleToInt64Bits(b);
        }

        private static string Bits(double value)
        {
            return GoldenJsonExtensions.ToF64Bits(value) + " (" + value.ToString("R", CultureInfo.InvariantCulture) + ")";
        }
    }
}
