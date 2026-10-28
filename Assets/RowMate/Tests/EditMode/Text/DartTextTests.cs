using System;
using System.Collections.Generic;
using NUnit.Framework;
using RowMate.Core.Text;

namespace RowMate.Tests.EditMode.Text
{
    // DTT: String.trim() de Dart (AC-RUT-11-05, -06; <flutter-sdk>/bin/cache/dart-sdk/lib/_internal/vm/lib/string_patch.dart
    // :463-499). Los caracteres especiales se arman con su código (sin escapes en el texto de la fuente).
    [TestFixture]
    public sealed class DartTextTests
    {
        // _isTwoByteWhitespace: 9-13, 32, U+0085, U+00A0, U+1680, U+2000-U+200A, U+2028, U+2029, U+202F, U+205F, U+3000, U+FEFF.
        private static readonly int[] DartWhitespace =
        {
            0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x20, 0x85, 0xA0, 0x1680, 0x2000, 0x2001, 0x2002, 0x2003, 0x2004, 0x2005, 0x2006, 0x2007,
            0x2008, 0x2009, 0x200A, 0x2028, 0x2029, 0x202F, 0x205F, 0x3000, 0xFEFF,
        };

        [Test, Category("AC-RUT-11-06"), Category("AC-RUT-11-05")]
        public void TrimLikeDart()
        {
            Assert.That(DartText.Trim("  Mi rutina  "), Is.EqualTo("Mi rutina"), "spec AC-RUT-11-06");
            Assert.That(DartText.Trim("   "), Is.EqualTo(""), "una descripción de espacios queda vacía");
            Assert.That(DartText.Trim(""), Is.EqualTo(""));
            Assert.That(DartText.Trim("Mi  rutina"), Is.EqualTo("Mi  rutina"), "los del medio quedan");
            Assert.That(DartText.Trim("\tSerie\n"), Is.EqualTo("Serie"));
            Assert.That(DartText.Trim(C(0xFEFF) + "x" + C(0xFEFF)), Is.EqualTo("x"), "el BOM es espacio en Dart (no en string.Trim de .NET)");
            Assert.That(DartText.Trim(C(0x3000) + C(0x00A0) + "Remo" + C(0x2028)), Is.EqualTo("Remo"));
            Assert.That(DartText.Trim(C(0x200B) + "x"), Is.EqualTo(C(0x200B) + "x"), "U+200B no es espacio en Dart");
            Assert.That(DartText.Trim(C(0x180E) + "x"), Is.EqualTo(C(0x180E) + "x"), "U+180E tampoco");
            Assert.That(DartText.Trim("Pirámide ñ ¿¡"), Is.EqualTo("Pirámide ñ ¿¡"));
        }

        [Test, Category("AC-RUT-11-06")]
        public void EachDartWhitespaceIsTrimmedAndNothingElse()
        {
            var expected = new HashSet<int>(DartWhitespace);
            var wrong = new List<string>();
            for (var code = 0; code <= 0xFFFF; code++)
            {
                if (DartText.IsWhitespace((char)code) != expected.Contains(code))
                {
                    wrong.Add("U+" + code.ToString("X4", System.Globalization.CultureInfo.InvariantCulture));
                }
            }

            Assert.That(wrong, Is.Empty, "IsWhitespace distinto de Dart en: " + string.Join(", ", wrong));
            foreach (var code in DartWhitespace)
            {
                Assert.That(DartText.Trim(C(code) + "a" + C(code)), Is.EqualTo("a"), "U+" + code.ToString("X4", System.Globalization.CultureInfo.InvariantCulture));
            }
        }

        [Test, Category("AC-RUT-11-06")]
        public void SameStringWhenThereIsNothingToTrim()
        {
            var text = "Escalera SPM";
            Assert.That(DartText.Trim(text), Is.SameAs(text));
            Assert.Throws<ArgumentNullException>(() => DartText.Trim(null!));
        }

        private static string C(int code)
        {
            return ((char)code).ToString();
        }
    }
}
