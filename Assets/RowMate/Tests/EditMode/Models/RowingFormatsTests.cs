using System.Globalization;
using System.IO;
using NUnit.Framework;
using RowMate.Core.Models;
using RowMate.Core.Text;
using RowMate.Tests.EditMode.Project;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Models
{
    // Textos de la grilla de Dispositivo: lib/features/device/device_screen.dart:253-258 (la regla; la grilla es de la It. 2).
    [TestFixture]
    public sealed class RowingFormatsTests
    {
        // AC-BLE-01-05: SPM con 1 decimal y punto; vatios entero sin unidad; distancia con "m" pegada; pulso entero.
        [Test, Category("AC-BLE-01-05")]
        public void DeviceGridTexts()
        {
            Assert.That(RowingFormats.Spm(24.0), Is.EqualTo("24.0"));
            Assert.That(RowingFormats.Spm(23.5), Is.EqualTo("23.5"));
            Assert.That(RowingFormats.Spm(24.25), Is.EqualTo("24.3"));
            Assert.That(RowingFormats.Watts(149), Is.EqualTo("149"));
            Assert.That(RowingFormats.Watts(-10), Is.EqualTo("-10"));
            Assert.That(RowingFormats.Meters(652), Is.EqualTo("652m"));
            Assert.That(RowingFormats.Meters(10000), Is.EqualTo("10000m"));
            Assert.That(RowingFormats.HeartRate(140), Is.EqualTo("140"));
            Assert.That(RowingFormats.Split(133), Is.EqualTo("02:13"));
            Assert.That(RowingFormats.Split(0), Is.EqualTo("--:--"));
            Assert.That(RowingFormats.Elapsed(300), Is.EqualTo("05:00"));
            Assert.That(RowingFormats.Elapsed(3661), Is.EqualTo("1:01:01"));
        }

        [Test, Category("AC-BLE-01-05"), Ignore("NO VERIFICABLE → [Editor] It. 2 (F-BLE-11): los textos en la grilla de Dispositivo en pantalla")]
        public void DeviceGridShowsTheTextsOnScreen()
        {
        }

        [Test, Category("AC-BLE-01-05"), Category("AC-BASE-03")]
        public void MatchesOracle_GridTexts()
        {
            // toStringAsFixed(1) del ritmo, toString() de vatios y pulso, '${m}m' de la distancia, sobre el RowingData del legacy.
            var spm = GoldenVectors.CheckRows("rowing_data.json", "spmText", row =>
                Diff(row.Text("out"), RowingFormats.Spm(row.F64("x"))));
            var watts = GoldenVectors.CheckRows("rowing_data.json", "wattsText", row =>
                Diff(row.Text("out"), RowingFormats.Watts((int)row.Int("w"))));
            var heartRate = GoldenVectors.CheckRows("rowing_data.json", "heartRateText", row =>
                Diff(row.Text("out"), RowingFormats.HeartRate((int)row.Int("bpm"))));
            var meters = GoldenVectors.CheckRows("rowing_data.json", "metersText", row =>
                Diff(row.Text("out"), RowingFormats.Meters((int)row.Int("m"))));
            Assert.That(spm, Is.GreaterThan(1200));
            Assert.That(watts, Is.GreaterThan(1500));
            Assert.That(heartRate, Is.GreaterThan(250));
            Assert.That(meters, Is.GreaterThan(2000));
        }

        // AC-BLE-01-06: el split y el tiempo de la lectura y de la grilla son LA regla compartida de TimeFormats.
        [Test, Category("AC-BLE-01-06")]
        public void UsesSharedTimeRules()
        {
            for (var s = -120; s <= 100000; s += s < 8000 ? 1 : 7)
            {
                var reading = new RowingData { Pace500mSeconds = s, ElapsedSeconds = s };
                Assert.That(reading.Pace500mFormatted, Is.EqualTo(TimeFormats.SplitOrDashes(s)), "split " + s);
                Assert.That(RowingFormats.Split(s), Is.EqualTo(TimeFormats.SplitOrDashes(s)), "split " + s);
                Assert.That(reading.ElapsedFormatted, Is.EqualTo(TimeFormats.Elapsed(s)), "tiempo " + s);
                Assert.That(RowingFormats.Elapsed(s), Is.EqualTo(TimeFormats.Elapsed(s)), "tiempo " + s);
            }
        }

        [Test, Category("AC-BLE-01-06")]
        public void NoOwnTimeFormattingInRowingSources()
        {
            // Ni RowingData ni RowingFormats arman MM:SS por su cuenta: llaman a TimeFormats (única implementación).
            // Se lee el código sin comentarios (los comentarios citan los formatos).
            foreach (var name in new[] { "RowingData.cs", "RowingFormats.cs" })
            {
                var code = SourceScan.StripComments(File.ReadAllText(RepoPaths.Combine("Assets/RowMate/Core/Models/" + name)));
                Assert.That(code, Does.Contain("TimeFormats.SplitOrDashes("), name);
                Assert.That(code, Does.Contain("TimeFormats.Elapsed("), name);
                Assert.That(code, Does.Not.Contain("PadLeft"), name);
                Assert.That(code, Does.Not.Contain("\"--:--\""), name);
            }
        }

        // AC-BASE-02 (Q-06): con coma decimal y punto de miles en el equipo, se ve lo mismo que con en-US.
        [TestCase("es-AR", Category = "AC-BASE-02")]
        [TestCase("de-DE", Category = "AC-BASE-02")]
        [TestCase("fr-FR", Category = "AC-BASE-02")]
        [TestCase("sv-SE", Category = "AC-BASE-02")]
        [TestCase("en-US", Category = "AC-BASE-02")]
        public void CultureIndependent(string culture)
        {
            var previous = CultureInfo.CurrentCulture;
            var previousUi = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo(culture);
                CultureInfo.CurrentUICulture = new CultureInfo(culture);
                Assert.That(RowingFormats.Spm(24.25), Is.EqualTo("24.3"));
                Assert.That(RowingFormats.Meters(10000), Is.EqualTo("10000m"));
                Assert.That(RowingFormats.Watts(-10), Is.EqualTo("-10"));
                Assert.That(RowingFormats.HeartRate(140), Is.EqualTo("140"));
                Assert.That(RowingFormats.Spm(-0.5), Is.EqualTo("-0.5"));
                Assert.That(new RowingData { Pace500mSeconds = 133, ElapsedSeconds = 3661 }.Pace500mFormatted, Is.EqualTo("02:13"));
                Assert.That(new RowingData { Pace500mSeconds = 133, ElapsedSeconds = 3661 }.ElapsedFormatted, Is.EqualTo("1:01:01"));
            }
            finally
            {
                CultureInfo.CurrentCulture = previous;
                CultureInfo.CurrentUICulture = previousUi;
            }
        }

        private static string? Diff(string expected, string actual)
        {
            return actual == expected ? null : "esperado \"" + expected + "\", obtenido \"" + actual + "\"";
        }
    }
}
