using System;
using System.Globalization;
using System.Threading;
using NUnit.Framework;
using RowMate.Core.Models;
using RowMate.Core.Workout;
using RowMate.Tests.EditMode.Localization;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Workout
{
    // WFT: los 15 formatos de display de F-WKT-11 (+ F-APP-12; AC-WKT-11-01 a -16; 02-tech-design §2.12 de la It. 4a), con los
    // ejemplos de cada AC. Iguales en es y en, sin la cultura del equipo (Q-06).
    [TestFixture]
    public sealed class WorkoutFormatsTests
    {
        [TestCase(133, "02:13", Category = "AC-WKT-11-01")]
        [TestCase(59, "00:59", Category = "AC-WKT-11-01")]
        [TestCase(6000, "100:00", Category = "AC-WKT-11-01")]
        [TestCase(0, "--:--", Category = "AC-WKT-11-01")]
        [TestCase(-5, "--:--", Category = "AC-WKT-11-01")]
        public void LiveSplit(int seconds, string expected)
        {
            Assert.That(WorkoutFormats.LiveSplit(seconds), Is.EqualTo(expected));
        }

        [TestCase(24.0, "24.0", Category = "AC-WKT-11-02")]
        [TestCase(23.5, "23.5", Category = "AC-WKT-11-02")]
        [TestCase(24.25, "24.3", Category = "AC-WKT-11-02")]
        [TestCase(0.0, "0.0", Category = "AC-WKT-11-02")]
        public void Spm(double spm, string expected)
        {
            Assert.That(WorkoutFormats.Spm(spm), Is.EqualTo(expected));
        }

        [Test, Category("AC-WKT-11-03")]
        public void Watts()
        {
            Assert.That(WorkoutFormats.Watts(149), Is.EqualTo("149"));
            Assert.That(WorkoutFormats.Watts(-10), Is.EqualTo("-10"), "Q-38: crudo");
            Assert.That(LocalizationFixture.Es.Get(RowMate.Core.Localization.L10nKeys.HudUnitWatts), Is.EqualTo("W"), "la unidad aparte");
        }

        [Test, Category("AC-WKT-11-04")]
        public void HudDistance()
        {
            Assert.That(WorkoutFormats.HudDistance(1484), Is.EqualTo("1484"));
            Assert.That(WorkoutFormats.HudDistance(123456), Is.EqualTo("123456"), "sin separador de miles");
            Assert.That(LocalizationFixture.En.Get(RowMate.Core.Localization.L10nKeys.HudUnitMeters), Is.EqualTo("m"));
        }

        [Test, Category("AC-WKT-11-05")]
        public void KcalAndPulse()
        {
            Assert.That(WorkoutFormats.Count(89), Is.EqualTo("89"));
            Assert.That(WorkoutFormats.Count(142), Is.EqualTo("142"));
        }

        [TestCase(0, "00:00", Category = "AC-WKT-11-06")]
        [TestCase(175, "02:55", Category = "AC-WKT-11-06")]
        [TestCase(3725, "1:02:05", Category = "AC-WKT-11-06")]
        [TestCase(36000, "10:00:00", Category = "AC-WKT-11-06")]
        public void SessionClock(long seconds, string expected)
        {
            Assert.That(WorkoutFormats.SessionClock(seconds), Is.EqualTo(expected));
        }

        [TestCase(65, "1:05", Category = "AC-WKT-11-07")]
        [TestCase(5, "0:05", Category = "AC-WKT-11-07")]
        [TestCase(720, "12:00", Category = "AC-WKT-11-07")]
        [TestCase(0, "0:00", Category = "AC-WKT-11-07")]
        [TestCase(359999, "5999:59", Category = "AC-WKT-11-07")]
        public void Countdown(long remaining, string expected)
        {
            Assert.That(WorkoutFormats.Countdown(remaining), Is.EqualTo(expected));
        }

        [Test, Category("AC-WKT-11-08"), Category("AC-WKT-03-08")]
        public void DistanceStepAndNoGoal()
        {
            Assert.That(WorkoutFormats.StepDistance(500, LocalizationFixture.Es), Is.EqualTo("500m"));
            Assert.That(WorkoutFormats.StepDistance(1000, LocalizationFixture.En), Is.EqualTo("1000m"), "sin separador de miles");
            Assert.That(WorkoutFormats.StepNoGoal(LocalizationFixture.Es), Is.EqualTo("?"), "DV-25: el legacy, nullm");
            Assert.That(WorkoutFormats.StepNoGoal(LocalizationFixture.En), Is.EqualTo("?"));
        }

        [TestCase(166.667, "2:46.7", Category = "AC-WKT-11-09")]
        [TestCase(65.0, "1:05.0", Category = "AC-WKT-11-09")]
        [TestCase(55.0, "0:55.0", Category = "AC-WKT-11-09")]
        [TestCase(59.96, "1:00.0", Category = "AC-WKT-11-09")]
        public void LapTime(double seconds, string expected)
        {
            Assert.That(WorkoutFormats.LapTime(seconds), Is.EqualTo(expected));
        }

        [Test, Category("AC-WKT-11-10")]
        public void RepSplit()
        {
            Assert.That(WorkoutFormats.RepSplit(114.4), Is.EqualTo("1:54"));
            Assert.That(WorkoutFormats.RepSplit(133.5), Is.EqualTo("2:14"), "la mitad hacia arriba");
            Assert.That(WorkoutFormats.RepSplit(null), Is.EqualTo("—"), "sin metros de trabajo");
        }

        [TestCase(1245, "1.245 m", Category = "AC-WKT-11-11")]
        [TestCase(300, "300 m", Category = "AC-WKT-11-11")]
        [TestCase(1000000, "1.000.000 m", Category = "AC-WKT-11-11")]
        [TestCase(0, "0 m", Category = "AC-WKT-11-11")]
        [TestCase(-1245, "-1.245 m", Category = "AC-WKT-11-11")]
        public void PanelMeters(long meters, string expected)
        {
            Assert.That(WorkoutFormats.PanelMeters(meters), Is.EqualTo(expected));
        }

        [TestCase(9, 5, "09:05", Category = "AC-WKT-11-12")]
        [TestCase(13, 54, "13:54", Category = "AC-WKT-11-12")]
        [TestCase(0, 0, "00:00", Category = "AC-WKT-11-12")]
        [TestCase(23, 59, "23:59", Category = "AC-WKT-11-12")]
        public void WallClock(int hour, int minute, string expected)
        {
            var local = new DateTimeOffset(2026, 10, 28, hour, minute, 41, TimeSpan.FromHours(-3));
            Assert.That(WorkoutFormats.WallClock(local), Is.EqualTo(expected));
        }

        [Test, Category("AC-WKT-11-13")]
        public void TargetLabelOfTheExpandedStep()
        {
            // Prueba 4a, trabajo de la repetición 2 (el paso expandido 3): el objetivo ya progresado, con su 📈.
            var prueba = RoutineExpansion.Flatten(RoutineSeeds.PruebaCuatroA().Steps);
            foreach (var l10n in new[] { LocalizationFixture.Es, LocalizationFixture.En })
            {
                Assert.That(WorkoutFormats.TargetLabel(prueba[3], l10n), Is.EqualTo(">160W · 24 spm · \U0001F4C8 SPM +2/rep"));
                var piramide = RoutineExpansion.Flatten(RoutineSeeds.PiramideQuinientos().Steps);
                Assert.That(WorkoutFormats.TargetLabel(piramide[1], l10n), Is.EqualTo("150-180W · 02:05/500m · \U0001F4C8 W +10/rep, Split -2s/rep"));
                Assert.That(WorkoutFormats.TargetLabel(prueba[0], l10n), Is.Empty, "sin objetivos, vacía");
            }
        }

        [Test, Category("AC-WKT-11-14")]
        public void RoutineSummary()
        {
            Assert.That(WorkoutFormats.RoutineSummaryText(RoutineSeeds.EscaleraSpm(), LocalizationFixture.Es), Is.EqualTo("1 serie · 1 descanso"));
            Assert.That(WorkoutFormats.RoutineSummaryText(RoutineSeeds.PruebaCuatroA(), LocalizationFixture.Es), Is.EqualTo("1 serie · 1 descanso"));
            Assert.That(WorkoutFormats.RoutineSummaryText(RoutineSeeds.PruebaCuatroA(), LocalizationFixture.En), Is.EqualTo("1 interval · 1 rest"));
            Assert.That(WorkoutFormats.RoutineSummaryText(RoutineSeeds.SoloCalentamiento(), LocalizationFixture.Es), Is.EqualTo("1 paso"));
            Assert.That(WorkoutFormats.RoutineSummaryText(RoutineSeeds.SoloCalentamiento(), LocalizationFixture.En), Is.EqualTo("1 step"));
        }

        [Test, Category("AC-WKT-11-15")]
        public void FinalSummary()
        {
            Assert.That(WorkoutFormats.FinalDistance(1484), Is.EqualTo("1484 m"));
            Assert.That(WorkoutFormats.FinalDistance(123456), Is.EqualTo("123456 m"), "sin separador de miles");
            Assert.That(WorkoutFormats.FinalCalories(89), Is.EqualTo("89"));
        }

        [TestCase("de-DE", Category = "AC-WKT-11-16")]
        [TestCase("ar-SA", Category = "AC-WKT-11-16")]
        [TestCase("es-AR", Category = "AC-WKT-11-16")]
        public void SameUnderAnyCulture(string culture)
        {
            // Q-06: punto decimal, miles con punto solo en los paneles y 24 h, con cualquier cultura del equipo.
            var previous = Thread.CurrentThread.CurrentCulture;
            var previousUi = Thread.CurrentThread.CurrentUICulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo(culture);
                Thread.CurrentThread.CurrentUICulture = new CultureInfo(culture);
                Assert.That(WorkoutFormats.Spm(24.25), Is.EqualTo("24.3"));
                Assert.That(WorkoutFormats.Watts(-10), Is.EqualTo("-10"));
                Assert.That(WorkoutFormats.HudDistance(123456), Is.EqualTo("123456"));
                Assert.That(WorkoutFormats.LiveSplit(133), Is.EqualTo("02:13"));
                Assert.That(WorkoutFormats.SessionClock(3725), Is.EqualTo("1:02:05"));
                Assert.That(WorkoutFormats.Countdown(65), Is.EqualTo("1:05"));
                Assert.That(WorkoutFormats.LapTime(166.667), Is.EqualTo("2:46.7"));
                Assert.That(WorkoutFormats.RepSplit(114.4), Is.EqualTo("1:54"));
                Assert.That(WorkoutFormats.PanelMeters(1245), Is.EqualTo("1.245 m"));
                Assert.That(WorkoutFormats.WallClock(new DateTimeOffset(2026, 10, 28, 21, 7, 0, TimeSpan.Zero)), Is.EqualTo("21:07"));
                Assert.That(WorkoutFormats.FinalDistance(1484), Is.EqualTo("1484 m"));
                Assert.That(WorkoutFormats.SplitTargetValue(125), Is.EqualTo("02:05"));
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = previous;
                Thread.CurrentThread.CurrentUICulture = previousUi;
            }
        }

        [Test, Category("AC-WKT-11-13")]
        public void TargetValueOfTheHud()
        {
            // hudTarget (D-ART-09): SPM y vatios mín. con su entero; el split con el formato de su objetivo (MinSec).
            Assert.That(WorkoutFormats.TargetValue(22), Is.EqualTo("22"));
            Assert.That(WorkoutFormats.SplitTargetValue(125), Is.EqualTo("02:05"));
            Assert.That(WorkoutFormats.SplitTargetValue(0), Is.EqualTo("00:00"));
            Assert.That(WorkoutFormats.SplitTargetValue(-5), Is.EqualTo("00:55"), "sin piso, como AC-RUT-04-04");
        }

        // QA-V4b-MOT-01: el conteo del diálogo de completado escribe los totales en un búfer propio, sin cadena nueva por cuadro.
        // Los caracteres tienen que ser los mismos que los de SessionClock y Count (Fmt.Integer), también en los bordes.
        [Test, Category("AC-WKT-21-05")]
        public void WrittenTotalsMatchTheStringsWithoutAllocating()
        {
            var buffer = new char[RowMate.Core.Text.TimeFormats.ElapsedMaxChars + 2];
            var clocks = new long[] { 0, 1, 9, 59, 60, 61, 599, 600, 3599, 3600, 3601, 3725, 35999, 36000, 359999, 360000, -1, -5, -65, -3600, -3601, long.MaxValue, long.MinValue + 1 };
            foreach (var seconds in clocks)
            {
                var length = WorkoutFormats.WriteSessionClock(seconds, buffer, 1);
                Assert.That(new string(buffer, 1, length), Is.EqualTo(WorkoutFormats.SessionClock(seconds)), "el reloj de " + seconds);
            }

            var counts = new long[] { 0, 7, 10, 99, 100, 1120, 65535, 16777215, int.MaxValue, -1, -10, -2147483648L, long.MaxValue, long.MinValue };
            var integer = new char[RowMate.Core.Text.Fmt.IntegerMaxChars];
            foreach (var value in counts)
            {
                var length = RowMate.Core.Text.Fmt.WriteInteger(value, integer, 0);
                Assert.That(new string(integer, 0, length), Is.EqualTo(RowMate.Core.Text.Fmt.Integer(value)), "la cuenta " + value);
            }

            for (long seconds = 0; seconds < 7300; seconds += 7)
            {
                var length = WorkoutFormats.WriteSessionClock(seconds, buffer, 0);
                Assert.That(new string(buffer, 0, length), Is.EqualTo(WorkoutFormats.SessionClock(seconds)));
            }

            Assert.Throws<ArgumentOutOfRangeException>(() => RowMate.Core.Text.Fmt.WriteInteger(12345, new char[4], 0), "no entra");
            WorkoutFormats.WriteSessionClock(3725, buffer, 0); // JIT
            RowMate.Core.Text.Fmt.WriteInteger(1120, integer, 0);
            var before = AllocatedBytes.Now();
            for (var i = 0; i < 70; i++)
            {
                WorkoutFormats.WriteSessionClock(i * 3, buffer, 0);
                RowMate.Core.Text.Fmt.WriteInteger(i * 16, integer, 0);
            }

            var allocated = AllocatedBytes.Since(before);
            if (allocated.HasValue)
            {
                Assert.That(allocated.Value, Is.EqualTo(0), "sin asignar: " + allocated.Value + " B en 70 cuadros");
            }
        }
    }
}
