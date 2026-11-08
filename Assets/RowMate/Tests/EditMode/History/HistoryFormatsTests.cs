using System;
using System.Globalization;
using System.Threading;
using NUnit.Framework;
using RowMate.Core.History;
using RowMate.Core.Models;
using RowMate.Core.Text;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.History
{
    // HF · Los formatos del historial y del detalle (01-po-spec §4; F-HIS-05), sin cultura: con el equipo en es-AR (coma
    // decimal) salen igual. legacy: history_screen.dart:74, session_detail_screen.dart:218-224, :254-261, :434-461, :501-510.
    [TestFixture]
    public sealed class HistoryFormatsTests
    {
        private CultureInfo _culture = null!;

        [SetUp]
        public void SetUp()
        {
            _culture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = new CultureInfo("es-AR");
        }

        [TearDown]
        public void TearDown()
        {
            Thread.CurrentThread.CurrentCulture = _culture;
        }

        [TestCase("2026-10-02T16:51:00Z", "02/10/2026  13:51")]
        [TestCase("2026-01-05T12:07:00Z", "05/01/2026  09:07")]
        [TestCase("2026-10-02T00:05:00Z", "01/10/2026  21:05")]
        [Category("AC-HIS-05-01")]
        public void CardDateIsLocalWithTwoSpaces(string utc, string expected)
        {
            var local = IsoDate.Parse(utc, HistorySeeds.Zone);
            Assert.That(HistoryFormats.CardDate(local), Is.EqualTo(expected));
        }

        [TestCase(0, "00:00")]
        [TestCase(440, "07:20")]
        [TestCase(3725, "1:02:05")]
        [TestCase(36000, "10:00:00")]
        [Category("AC-HIS-05-02")]
        public void SessionDuration(int seconds, string expected)
        {
            Assert.That(new SessionStats { TotalTimeSeconds = seconds }.DurationFormatted, Is.EqualTo(expected));
        }

        [TestCase(135, "02:15")]
        [TestCase(605, "10:05")]
        [TestCase(0, "--:--")]
        [TestCase(-3, "--:--")]
        [Category("AC-HIS-05-02")]
        public void SessionSplit(int seconds, string expected)
        {
            Assert.That(new SessionStats { P99SplitSeconds = seconds }.SplitFormatted, Is.EqualTo(expected));
        }

        [TestCase(120, "2:00")]
        [TestCase(45, "0:45")]
        [TestCase(3600, "60:00")]
        [TestCase(0, "0:00")]
        [Category("AC-HIS-05-03")]
        public void StepDurationWithoutPadding(int seconds, string expected)
        {
            Assert.That(HistoryFormats.StepDuration(seconds), Is.EqualTo(expected));
        }

        [Test, Category("AC-HIS-05-03")]
        public void StepTexts()
        {
            Assert.That(HistoryFormats.StepSplit(133), Is.EqualTo("2:13/500m"));
            Assert.That(HistoryFormats.StepSplit(0), Is.EqualTo("0:00/500m"), "sin caso especial para 0");
            Assert.That(HistoryFormats.StepWatts(150), Is.EqualTo("150W"));
            Assert.That(HistoryFormats.StepSpm(24.1), Is.EqualTo("24.1 spm"));
            Assert.That(HistoryFormats.StepLine1(120, 432), Is.EqualTo("2:00  ·  432m"), "dos espacios a cada lado del punto medio");
            Assert.That(HistoryFormats.StepLine1(65, -12), Is.EqualTo("1:05  ·  -12m"), "la distancia puede dar negativa");
        }

        [TestCase(24.04, "24.0")]
        [TestCase(0.0, "0.0")]
        [TestCase(24.96, "25.0")]
        [Category("AC-HIS-05-04")]
        public void SpmWithOneDecimalAndAPoint(double spm, string expected)
        {
            var columns = StatColumns.Build(new SessionStats { P99Spm = spm });
            Assert.That(columns[3].Value, Is.EqualTo(expected));
        }

        [Test, Category("AC-HIS-05-04")]
        public void IntegersWithoutThousandsSeparator()
        {
            var columns = StatColumns.Build(new SessionStats { TotalDistance = 13796, P99Watts = 1500, AvgHeartRate = 139, MaxHeartRate = 150 });
            Assert.That(columns[1].Value, Is.EqualTo("13796m"));
            Assert.That(columns[2].Value, Is.EqualTo("1500"));
            Assert.That(columns[5].Value, Is.EqualTo("139/150"));
        }

        [Test, Category("AC-HIS-13-05")]
        public void AxisTimeWithoutHours()
        {
            Assert.That(HistoryFormats.AxisTime(3900), Is.EqualTo("65:00"));
            Assert.That(HistoryFormats.AxisTime(224), Is.EqualTo("3:44"));
            Assert.That(HistoryFormats.AxisTime(5), Is.EqualTo("0:05"));
            Assert.That(HistoryFormats.AxisTime(3725), Is.EqualTo("62:05"));
            Assert.That(HistoryFormats.AxisTime(119.9), Is.EqualTo("1:59"), "(value % 60).toInt() trunca");
        }

        [Test, Category("AC-HIS-13-04")]
        public void AxisValuesAndPace()
        {
            Assert.That(HistoryFormats.AxisValue(198.00000000000003), Is.EqualTo("198"));
            Assert.That(HistoryFormats.AxisValue(26.51), Is.EqualTo("26"), "entero truncado");
            Assert.That(HistoryFormats.AxisPace(180, 0), Is.EqualTo("3:00"));
            Assert.That(HistoryFormats.AxisPace(180, 50), Is.EqualTo("2:10"));
            Assert.That(HistoryFormats.AxisPace(180, 59.400000000000006), Is.EqualTo("2:00"), "(lento − y).toInt()");
        }

        [TestCase(ChartMetric.Watts, 150, "150.0 W")]
        [TestCase(ChartMetric.Spm, 24.1, "24.1 spm")]
        [TestCase(ChartMetric.Distance, 651, "651.0 m")]
        [TestCase(ChartMetric.HeartRate, 150, "150.0 bpm")]
        [TestCase(ChartMetric.Split, 133, "2:13 s")]
        [TestCase(ChartMetric.Split, 0, "--:-- s")]
        [TestCase(ChartMetric.Watts, 0, "0.0 W")]
        [Category("AC-HIS-13-07")]
        public void Tooltip(ChartMetric metric, double raw, string expected)
        {
            Assert.That(HistoryFormats.Tooltip(metric, raw), Is.EqualTo(expected));
        }

        [Test, Category("AC-HIS-13-07")]
        public void WriteTooltipUsesTheBufferFromTheOffset()
        {
            var buffer = new char[HistoryFormats.TooltipMaxChars + 4];
            buffer[0] = '#';
            var count = HistoryFormats.WriteTooltip(ChartMetric.Spm, 24.1, buffer, 1);
            Assert.That(new string(buffer, 0, 1 + count), Is.EqualTo("#24.1 spm"));
            Assert.Throws<ArgumentException>(() => HistoryFormats.WriteTooltip(ChartMetric.HeartRate, 150, new char[5], 0));
        }
    }
}
