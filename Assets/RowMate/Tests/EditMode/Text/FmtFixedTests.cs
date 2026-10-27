using System;
using System.Globalization;
using NUnit.Framework;
using RowMate.Core.Common;
using RowMate.Core.Text;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Text
{
    // Fmt.Fixed = double.toStringAsFixed de Dart (ADR 012 §1, 02-tech-design §3.1). AC-BASE-01, AC-BASE-03.
    [TestFixture]
    public sealed class FmtFixedTests
    {
        // Tabla de AC-BASE-01 (01-po-spec §4.18), QA_PLAN_REVIEW §3.2 y ADR 012 §5.
        [TestCase(0.15, 1, "0.1", Category = "AC-BASE-01")]
        [TestCase(0.25, 1, "0.3", Category = "AC-BASE-01")]
        [TestCase(0.35, 1, "0.3", Category = "AC-BASE-01")]
        [TestCase(24.45, 1, "24.4", Category = "AC-BASE-01")]
        [TestCase(24.25, 1, "24.3", Category = "AC-BASE-01")]
        [TestCase(23.95, 1, "23.9", Category = "AC-BASE-01")]
        [TestCase(1.05, 1, "1.1", Category = "AC-BASE-01")]
        [TestCase(-0.05, 1, "-0.1", Category = "AC-BASE-01")]
        [TestCase(2.5, 0, "3", Category = "AC-BASE-01")]
        [TestCase(0.5, 0, "1", Category = "AC-BASE-01")]
        [TestCase(1.5, 0, "2", Category = "AC-BASE-01")]
        [TestCase(-0.5, 0, "-1", Category = "AC-BASE-01")]
        [TestCase(0.45, 1, "0.5", Category = "AC-BASE-01")]
        [TestCase(99.95, 1, "100.0", Category = "AC-BASE-01")]
        [TestCase(-2.5, 0, "-3", Category = "AC-BASE-01")]
        [TestCase(1.005, 2, "1.00", Category = "AC-BASE-01")]
        [TestCase(123.456, 2, "123.46", Category = "AC-BASE-01")]
        [TestCase(-0.04, 1, "-0.0", Category = "AC-BASE-01")]
        [TestCase(-0.0, 1, "-0.0", Category = "AC-BASE-01")]
        [TestCase(-0.0, 0, "-0", Category = "AC-BASE-01")]
        [TestCase(0.0001, 3, "0.000", Category = "AC-BASE-01")]
        [TestCase(5e-324, 3, "0.000", Category = "AC-BASE-01")]
        [TestCase(1e20, 2, "100000000000000000000.00", Category = "AC-BASE-01")]
        [TestCase(1e21, 2, "1e+21", Category = "AC-BASE-01")]
        [TestCase(double.NaN, 1, "NaN", Category = "AC-BASE-01")]
        [TestCase(double.PositiveInfinity, 1, "Infinity", Category = "AC-BASE-01")]
        [TestCase(double.NegativeInfinity, 1, "-Infinity", Category = "AC-BASE-01")]
        [TestCase(24.0, 1, "24.0", Category = "AC-BASE-01")]
        [TestCase(23.5, 1, "23.5", Category = "AC-BASE-01")]
        [TestCase(0.0, 0, "0", Category = "AC-BASE-01")]
        [TestCase(0.0, 2, "0.00", Category = "AC-BASE-01")]
        [TestCase(0.1, 20, "0.10000000000000000555", Category = "AC-BASE-01")]
        public void SpecTable(double x, int fractionDigits, string expected)
        {
            Assert.That(Fmt.Fixed(x, fractionDigits), Is.EqualTo(expected));
        }

        [Test, Category("AC-BASE-01")]
        public void NegativeZeroLiteralKeepsItsSign()
        {
            // Control del caso de la tabla: el literal -0.0 de C# es el cero negativo de IEEE-754.
            Assert.That(BitConverter.DoubleToInt64Bits(-0.0), Is.LessThan(0L));
        }

        [TestCase(-1, Category = "AC-BASE-01")]
        [TestCase(21, Category = "AC-BASE-01")]
        public void DigitsOutsideZeroToTwentyThrow(int fractionDigits)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Fmt.Fixed(1.0, fractionDigits));
            var buffer = new char[64];
            Assert.Throws<ArgumentOutOfRangeException>(() => Fmt.TryFormatFixed(1.0, fractionDigits, buffer, out _));
        }

        [Test, Category("AC-BASE-01")]
        public void TryFormatFixedWritesTheSameText()
        {
            var values = new[] { 24.25, -0.04, 0.0, 99.95, 1e20, 1e21, double.NaN, -0.0, 123.456, 5e-324, 0.1 };
            var buffer = new char[64];
            for (var i = 0; i < values.Length; i++)
            {
                for (var n = 0; n <= 20; n++)
                {
                    Assert.That(Fmt.TryFormatFixed(values[i], n, buffer, out var written), Is.True);
                    Assert.That(new string(buffer, 0, written), Is.EqualTo(Fmt.Fixed(values[i], n)), "x = " + values[i].ToString("R", CultureInfo.InvariantCulture) + ", n = " + n);
                }
            }
        }

        [Test, Category("AC-BASE-01")]
        public void TryFormatFixedReturnsFalseWhenItDoesNotFit()
        {
            var small = new char[3];
            Assert.That(Fmt.TryFormatFixed(24.25, 1, small, out var written), Is.False);
            Assert.That(written, Is.EqualTo(0));
            var exact = new char[4];
            Assert.That(Fmt.TryFormatFixed(24.25, 1, exact, out written), Is.True);
            Assert.That(new string(exact, 0, written), Is.EqualTo("24.3"));
        }

#if NET5_0_OR_GREATER
        [Test, Category("AC-BASE-01")]
        public void TryFormatFixedDoesNotAllocateOnTheFastPath()
        {
            // 0 B por llamada para los valores de la app (n ≤ 3, magnitudes de métricas): lo usa el HUD.
            var buffer = new char[32];
            var values = new[] { 24.25, 150.0, 23.95, -0.04, 482.0, 0.5, 9999.95 };
            FormatMany(values, buffer); // calentamiento: JIT y estáticos fuera de la medición
            Assert.That(FormatMany(values, buffer), Is.EqualTo(0L));
        }

        // Optimizado de entrada (sin tiering ni OSR): así la medición ve solo lo que reserva Fmt.
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveOptimization)]
        private static long FormatMany(double[] values, char[] buffer)
        {
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var round = 0; round < 1000; round++)
            {
                for (var i = 0; i < values.Length; i++)
                {
                    for (var n = 0; n <= 3; n++)
                    {
                        Fmt.TryFormatFixed(values[i], n, buffer, out _);
                    }
                }
            }

            return GC.GetAllocatedBytesForCurrentThread() - before;
        }
#endif

        [Test, Category("AC-BASE-01")]
        public void FastPathEqualsBigIntegerPath()
        {
            // Propiedad interna (02-tech-design §3.1, punto 3): el camino de ulong da el mismo q que el de BigInteger.
            var random = new SystemRandom(2026);
            var fast = 0;
            for (var i = 0; i < 10000; i++)
            {
                var magnitude = Math.Pow(10, random.NextDouble() * 24 - 12);
                var x = (random.NextDouble() * 2 - 1) * magnitude;
                var n = i % 5 == 4 ? (int)(random.NextDouble() * 21) : i % 4;
                if (Fmt.TryQuotientFast(x, n, out var q))
                {
                    fast++;
                    Assert.That(q.ToString(CultureInfo.InvariantCulture), Is.EqualTo(Fmt.QuotientBig(x, n).ToString(CultureInfo.InvariantCulture)),
                        "x = " + x.ToString("R", CultureInfo.InvariantCulture) + ", n = " + n);
                }
            }

            Assert.That(fast, Is.GreaterThan(5000), "el camino rápido tiene que cubrir los casos comunes");
        }

        [Test, Category("AC-BASE-01")]
        public void ZeroDigitsAgreesWithDartRound()
        {
            // long.Parse(Fixed(x, 0)) == Round(x) para |x| < 1e15 (02-tech-design §3.1, punto 3).
            var random = new SystemRandom(7);
            for (var i = 0; i < 10000; i++)
            {
                var x = (random.NextDouble() * 2 - 1) * Math.Pow(10, random.NextDouble() * 15);
                if (i % 3 == 0)
                {
                    x = Math.Truncate(x) + 0.5 * (i % 2 == 0 ? 1 : -1);
                }

                var text = Fmt.Fixed(x, 0);
                Assert.That(long.Parse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture), Is.EqualTo(DartMath.Round(x)),
                    "x = " + x.ToString("R", CultureInfo.InvariantCulture));
            }
        }

        [Test, Category("AC-BASE-01"), Category("AC-BASE-03")]
        public void MatchesOracle()
        {
            var rows = GoldenVectors.CheckRows("fmt.json", "toStringAsFixed", row =>
            {
                var expected = row.Text("out");
                var actual = Fmt.Fixed(row.F64("x"), (int)row.Int("n"));
                return actual == expected ? null : "esperado \"" + expected + "\", obtenido \"" + actual + "\"";
            });
            Assert.That(rows, Is.GreaterThan(8000));
        }
    }
}
