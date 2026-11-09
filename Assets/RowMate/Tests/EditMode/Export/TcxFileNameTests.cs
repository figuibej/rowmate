using System;
using System.Text;
using NUnit.Framework;
using RowMate.Core.Export;
using RowMate.Core.Localization;
using RowMate.Tests.EditMode.Localization;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Export
{
    // FN · El nombre del archivo (AC-STR-09-13, -15; 02-tech-design §2.4, DT12-15, m-03).
    [TestFixture]
    public sealed class TcxFileNameTests
    {
        // Inicio 05/11/2026 18:40 en UTC−03:00 (= 21:40Z).
        private static readonly DateTimeOffset Start = new DateTimeOffset(2026, 11, 5, 21, 40, 0, TimeSpan.Zero);

        private static string Name(string? routine, bool simulator = false)
        {
            return TcxFileName.Resolve(TcxFileName.Base(Start, TestZones.UtcMinus3, routine, "Libre", simulator), TcxDocument.Extension,
                new string[0]);
        }

        [TestCase("Escalera", "RowMate_2026-11-05_1840_Escalera.tcx")]
        [TestCase("Escalera & <Co> \"A\" 'B'", "RowMate_2026-11-05_1840_Escalera-Co-A-B.tcx")]
        [TestCase("Series 4×500 m", "RowMate_2026-11-05_1840_Series-4-500-m.tcx")]
        [TestCase("Ñandú 2K", "RowMate_2026-11-05_1840_Ñandú-2K.tcx")]
        [TestCase("***", "RowMate_2026-11-05_1840.tcx")]
        [TestCase("..\\..\\CON", "RowMate_2026-11-05_1840_CON.tcx")]
        [TestCase("  a__b--c  ", "RowMate_2026-11-05_1840_a__b-c.tcx")]
        [Category("AC-STR-09-13")]
        public void SpecExamples(string routine, string expected)
        {
            Assert.That(Name(routine), Is.EqualTo(expected));
        }

        [Test, Category("AC-STR-09-13")]
        public void NfcNormalized()
        {
            // "Ñandú" con letras y tildes combinantes (NFD) da el mismo nombre que en NFC.
            Assert.That(Name("N\u0303andu\u0301 2K"), Is.EqualTo("RowMate_2026-11-05_1840_Ñandú-2K.tcx"));
        }

        [Test, Category("AC-STR-09-13")]
        public void NeverForbiddenCharacters()
        {
            var all = new StringBuilder();
            for (var c = 0; c < 128; c++)
            {
                all.Append((char)c);
            }

            all.Append("\u0085\u00A0\u2028\uFFFE\uD800");
            var name = TcxFileName.Sanitize(all.ToString());
            Assert.That(name, Is.EqualTo("0123456789-ABCDEFGHIJKLMNOPQRSTUVWXYZ-_"), "recortado a 40 y sin el - del borde");
            Assert.That(name.IndexOfAny("\\/:*?\"<>|".ToCharArray()), Is.EqualTo(-1));
            foreach (var ch in name)
            {
                Assert.That(char.IsControl(ch), Is.False);
            }
        }

        [Test, Category("AC-STR-09-13")]
        public void MaxFortyWithoutSplittingPairs()
        {
            Assert.That(TcxFileName.Sanitize(new string('a', 50)), Is.EqualTo(new string('a', 40)));
            // 39 letras y un par sustituto (una letra de 2 unidades UTF-16) en el 40: el par no entra entero y no se corta.
            var withPair = new string('a', 39) + "\uD835\uDC00" + "bbb";
            Assert.That(char.IsLetterOrDigit(withPair, 39), Is.True, "\uD835\uDC00 es una letra");
            Assert.That(TcxFileName.Sanitize(withPair), Is.EqualTo(new string('a', 39)));
            var fits = new string('a', 38) + "\uD835\uDC00" + "bbb";
            Assert.That(TcxFileName.Sanitize(fits), Is.EqualTo(new string('a', 38) + "\uD835\uDC00"), "un par que entra queda entero");
            Assert.That(TcxFileName.Sanitize(fits).Length, Is.EqualTo(40));
        }

        [Test, Category("AC-STR-09-13")]
        public void TrimsDashAfterCut()
        {
            // m-03: un '-' que queda en la posición 40 al recortar se quita después.
            var name = new string('a', 39) + " b" + new string('c', 10);
            Assert.That(TcxFileName.Sanitize(name), Is.EqualTo(new string('a', 39)));
            Assert.That(TcxFileName.Sanitize("- -" + new string('x', 45)), Is.EqualTo(new string('x', 40)), "los bordes antes de recortar");
        }

        [Test, Category("AC-STR-09-13")]
        public void FreeUsesHistoryFree()
        {
            var es = LocalizationFixture.Es.Get(L10nKeys.HistoryFree);
            var en = LocalizationFixture.En.Get(L10nKeys.HistoryFree);
            Assert.That(TcxFileName.Base(Start, TestZones.UtcMinus3, null, es, false), Is.EqualTo("RowMate_2026-11-05_1840_Libre"));
            Assert.That(TcxFileName.Base(Start, TestZones.UtcMinus3, null, en, false), Is.EqualTo("RowMate_2026-11-05_1840_Free"));
            Assert.That(TcxFileName.Base(Start, TestZones.UtcMinus3, "   ", es, false), Is.EqualTo("RowMate_2026-11-05_1840"),
                "un nombre en blanco no es una libre: queda sin nombre");
        }

        [Test, Category("AC-STR-09-13")]
        public void LocalTimeOfExportZone()
        {
            var start = TcxSeeds.T2.Session.StartedAt; // 22:02Z
            Assert.That(TcxFileName.Base(start, TestZones.Kolkata, null, "Free", false), Is.EqualTo("RowMate_2026-11-06_0332_Free"));
            Assert.That(TcxFileName.Base(start, TestZones.UtcMinus3, null, "Free", false), Is.EqualTo("RowMate_2026-11-05_1902_Free"));
            Assert.That(TcxFileName.Base(start, TestZones.StJohns, null, "Free", false), Is.EqualTo("RowMate_2026-11-05_1832_Free"));
        }

        [Test, Category("AC-STR-09-15")]
        public void SimBeforeCollisionSuffix()
        {
            var baseName = TcxFileName.Base(Start, TestZones.UtcMinus3, "Escalera", "Libre", true);
            Assert.That(baseName, Is.EqualTo("RowMate_2026-11-05_1840_Escalera_SIM"));
            Assert.That(TcxFileName.Resolve(baseName, ".tcx", new string[0]), Is.EqualTo("RowMate_2026-11-05_1840_Escalera_SIM.tcx"));
            Assert.That(TcxFileName.Resolve(baseName, ".tcx", new[] { "rowmate_2026-11-05_1840_escalera_sim.TCX" }),
                Is.EqualTo("RowMate_2026-11-05_1840_Escalera_SIM_2.tcx"), "sin distinguir mayúsculas");
            Assert.That(Name("Escalera"), Does.Not.Contain("SIM"), "el build normal nunca la lleva");
        }

        [Test, Category("AC-STR-09-14")]
        public void ResolveSkipsTakenSuffixes()
        {
            Assert.That(TcxFileName.Resolve("a", ".tcx", new[] { "a.tcx", "A_2.TCX", "a_3.tcx", "b.tcx" }), Is.EqualTo("a_4.tcx"));
            Assert.That(TcxFileName.Resolve("a", ".tcx", new[] { "a.tcx.part", "~rowmate-1.part" }), Is.EqualTo("a.tcx"));
        }
    }
}
