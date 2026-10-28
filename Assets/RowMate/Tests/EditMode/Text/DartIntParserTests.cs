using System;
using System.Numerics;
using NUnit.Framework;
using RowMate.Core.Text;

namespace RowMate.Tests.EditMode.Text
{
    // DIP: int.tryParse de Dart, sin base y EXACTO (AC-RUT-13-05; <flutter-sdk>/bin/cache/dart-sdk/lib/_internal/vm_shared/lib/
    // integers_patch.dart:84-125, :153-294). La tabla es la de la spec; los de más de 64 bits son DV-17 y DT-11.
    [TestFixture]
    public sealed class DartIntParserTests
    {
        [TestCase("", null, Category = "AC-RUT-13-05")]
        [TestCase("  ", null, Category = "AC-RUT-13-05")]
        [TestCase("150", "150", Category = "AC-RUT-13-05")]
        [TestCase("+150", "150", Category = "AC-RUT-13-05")]
        [TestCase(" 150 ", "150", Category = "AC-RUT-13-05")]
        [TestCase("0150", "150", Category = "AC-RUT-13-05")]
        [TestCase("-10", "-10", Category = "AC-RUT-13-05")]
        [TestCase("0x10", "16", Category = "AC-RUT-13-05")]
        [TestCase("0X10", "16", Category = "AC-RUT-13-05")]
        [TestCase("-0x10", "-16", Category = "AC-RUT-13-05")]
        [TestCase("+0x1f", "31", Category = "AC-RUT-13-05")]
        [TestCase("abc", null, Category = "AC-RUT-13-05")]
        [TestCase("15a", null, Category = "AC-RUT-13-05")]
        [TestCase("1 5", null, Category = "AC-RUT-13-05")]
        [TestCase("+", null, Category = "AC-RUT-13-05")]
        [TestCase("-", null, Category = "AC-RUT-13-05")]
        [TestCase("--2", null, Category = "AC-RUT-13-05")]
        [TestCase("+-2", null, Category = "AC-RUT-13-05")]
        [TestCase("5.0", null, Category = "AC-RUT-13-05")]
        [TestCase("1,5", null, Category = "AC-RUT-13-05")]
        [TestCase("1e3", null, Category = "AC-RUT-13-05")]
        [TestCase("0", "0", Category = "AC-RUT-13-05")]
        [TestCase("-0", "0", Category = "AC-RUT-13-05")]
        [TestCase("000", "0", Category = "AC-RUT-13-05")]
        [TestCase("0x", null, Category = "AC-RUT-13-05")]
        [TestCase("00x10", null, Category = "AC-RUT-13-05")]
        [TestCase("0xg", null, Category = "AC-RUT-13-05")]
        [TestCase("- 5", null, Category = "AC-RUT-13-05")]
        [TestCase("10000", "10000", Category = "AC-RUT-13-05")]
        [TestCase("99999999999999999999", "99999999999999999999", Category = "AC-RUT-13-05")]
        [TestCase("-99999999999999999999", "-99999999999999999999", Category = "AC-RUT-13-05")]
        [TestCase("0xFFFFFFFFFFFFFFFF", "18446744073709551615", Category = "AC-RUT-13-05")]
        public void LegacyTable(string text, string? expected)
        {
            var value = DartIntParser.TryParse(text);
            if (expected == null)
            {
                Assert.That(value.HasValue, Is.False, "\"" + text + "\" cuenta como vacío");
                return;
            }

            Assert.That(value.HasValue, Is.True, "\"" + text + "\"");
            Assert.That(value!.Value, Is.EqualTo(BigInteger.Parse(expected, System.Globalization.CultureInfo.InvariantCulture)), "\"" + text + "\"");
        }

        // Los que no se pueden escribir como literal sin un escape: el menos tipográfico, el dígito de ancho completo, los
        // espacios de Dart a los costados y el BOM.
        [Test, Category("AC-RUT-13-05")]
        public void UnicodeLookAlikesAreNotNumbers()
        {
            Assert.That(DartIntParser.TryParse(C(0x2212) + "2").HasValue, Is.False, "−2 con U+2212");
            Assert.That(DartIntParser.TryParse(C(0xFF15)).HasValue, Is.False, "５ de ancho completo");
            Assert.That(DartIntParser.TryParse("1" + C(0x0663)).HasValue, Is.False, "dígito arábigo");
            Assert.That(DartIntParser.TryParse(C(0x3000) + "42" + C(0xFEFF)), Is.EqualTo((BigInteger?)new BigInteger(42)), "espacios de Dart a los costados");
            Assert.That(DartIntParser.TryParse("4" + C(0x00A0) + "2").HasValue, Is.False, "un espacio en el medio");
            Assert.That(DartIntParser.TryParse(null).HasValue, Is.False);
        }

        // spec §12.3: un número de cualquier largo nunca desborda ni lanza; el valor es exacto.
        [Test, Category("AC-RUT-13-05")]
        public void AnyLengthNeverOverflows()
        {
            var digits = new string('9', 5000);
            var value = DartIntParser.TryParse(digits);
            Assert.That(value.HasValue, Is.True);
            Assert.That(value!.Value, Is.EqualTo(BigInteger.Pow(10, 5000) - 1));
            Assert.That(DartIntParser.TryParse("-" + new string('0', 3000) + "7"), Is.EqualTo((BigInteger?)new BigInteger(-7)));
            Assert.That(DartIntParser.TryParse("0x" + new string('f', 40))!.Value, Is.EqualTo(BigInteger.Pow(16, 40) - 1));
            Assert.That(DartIntParser.TryParse(new string('1', 3000) + "x").HasValue, Is.False);
            Assert.That(DartIntParser.TryParse("123456789012345678901234567890"), Is.EqualTo((BigInteger?)BigInteger.Parse("123456789012345678901234567890",
                System.Globalization.CultureInfo.InvariantCulture)));
        }

        private static string C(int code)
        {
            return ((char)code).ToString();
        }
    }
}
