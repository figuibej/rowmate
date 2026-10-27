using System;
using NUnit.Framework;
using RowMate.Core.Text;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Text
{
    // Fmt.DartToString = '$x' / toString() de un double en Dart (02-tech-design §3.1, QA-04) y Fmt.Integer.
    [TestFixture]
    public sealed class FmtDartToStringTests
    {
        // ADR 012 §5 y los vectores de 02-tech-design §3.1.
        [TestCase(24.0, "24.0", Category = "AC-BASE-01")]
        [TestCase(0.1, "0.1", Category = "AC-BASE-01")]
        [TestCase(1e21, "1e+21", Category = "AC-BASE-01")]
        [TestCase(1e-7, "1e-7", Category = "AC-BASE-01")]
        [TestCase(0.000001, "0.000001", Category = "AC-BASE-01")]
        [TestCase(-0.0, "-0.0", Category = "AC-BASE-01")]
        [TestCase(0.0, "0.0", Category = "AC-BASE-01")]
        [TestCase(100.0 / 3, "33.333333333333336", Category = "AC-BASE-01")]
        [TestCase(5e-324, "5e-324", Category = "AC-BASE-01")]
        [TestCase(5.684341886080802e-14, "5.684341886080802e-14", Category = "AC-BASE-01")]
        [TestCase(6.189700196426902e+26, "6.189700196426902e+26", Category = "AC-BASE-01")]
        [TestCase(2.9802322387695312e-8, "2.9802322387695312e-8", Category = "AC-BASE-01")]
        [TestCase(1.7976931348623157e308, "1.7976931348623157e+308", Category = "AC-BASE-01")]
        [TestCase(73786976294838206464.0, "73786976294838210000.0", Category = "AC-BASE-01")]
        [TestCase(1125899906842624.25, "1125899906842624.2", Category = "AC-BASE-01")]
        [TestCase(0.86, "0.86", Category = "AC-BASE-01")]
        [TestCase(1.5, "1.5", Category = "AC-BASE-01")]
        [TestCase(-1.5, "-1.5", Category = "AC-BASE-01")]
        [TestCase(1e22, "1e+22", Category = "AC-BASE-01")]
        [TestCase(double.NaN, "NaN", Category = "AC-BASE-01")]
        [TestCase(double.PositiveInfinity, "Infinity", Category = "AC-BASE-01")]
        [TestCase(double.NegativeInfinity, "-Infinity", Category = "AC-BASE-01")]
        public void SpecTable(double x, string expected)
        {
            Assert.That(Fmt.DartToString(x), Is.EqualTo(expected));
        }

        [Test, Category("AC-BASE-01")]
        public void PowersOfTwoUsedInTheTableAreExact()
        {
            // Control de los literales de la tabla: son exactamente 2^-44, 2^89, 2^-25 y 2^66.
            Assert.That(5.684341886080802e-14, Is.EqualTo(Math.Pow(2, -44)));
            Assert.That(6.189700196426902e+26, Is.EqualTo(Math.Pow(2, 89)));
            Assert.That(2.9802322387695312e-8, Is.EqualTo(Math.Pow(2, -25)));
            Assert.That(73786976294838206464.0, Is.EqualTo(Math.Pow(2, 66)));
        }

        [TestCase(0L, "0")]
        [TestCase(-10L, "-10")]
        [TestCase(10000L, "10000")]
        [TestCase(long.MinValue, "-9223372036854775808")]
        [TestCase(long.MaxValue, "9223372036854775807")]
        public void IntegerIsInvariantWithoutGrouping(long value, string expected)
        {
            Assert.That(Fmt.Integer(value), Is.EqualTo(expected));
        }

        [Test, Category("AC-BASE-01"), Category("AC-BASE-03")]
        public void MatchesOracle()
        {
            var rows = GoldenVectors.CheckRows("fmt.json", "toString", Compare);
            Assert.That(rows, Is.GreaterThanOrEqualTo(1000));
        }

        [Test, Category("AC-BASE-01"), Category("AC-BASE-03")]
        public void MatchesOracle_PowersOfTwo()
        {
            // QA-04: las 2 098 potencias de 2 (intervalo de redondeo asimétrico).
            var rows = GoldenVectors.CheckRows("fmt.json", "toStringPow2", Compare);
            Assert.That(rows, Is.EqualTo(2098));
        }

        [Test, Category("AC-BASE-01"), Category("AC-BASE-03")]
        public void MatchesOracle_Ties()
        {
            // QA-04: empates exactos en la cifra 17 (BignumDtoa desempata al par).
            var rows = GoldenVectors.CheckRows("fmt.json", "toStringTies", Compare);
            Assert.That(rows, Is.GreaterThanOrEqualTo(500));
        }

        private static string? Compare(GoldenRow row)
        {
            var expected = row.Text("out");
            var actual = Fmt.DartToString(row.F64("x"));
            return actual == expected ? null : "esperado \"" + expected + "\", obtenido \"" + actual + "\"";
        }
    }
}
