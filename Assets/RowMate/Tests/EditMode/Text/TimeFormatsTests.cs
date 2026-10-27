using NUnit.Framework;
using RowMate.Core.Text;

namespace RowMate.Tests.EditMode.Text
{
    // TimeFormats: única implementación de MM:SS y H:MM:SS (AC-BLE-01-06; legacy: rowing_data.dart:24-40,
    // workout_session.dart:29-37 y :94-109, interval_step.dart:87-91). Los textos de RowingData/SessionStats son de B y C.
    [TestFixture]
    public sealed class TimeFormatsTests
    {
        [TestCase(0L, "00")]
        [TestCase(5L, "05")]
        [TestCase(59L, "59")]
        [TestCase(100L, "100")]
        [TestCase(-1L, "-1")]
        [TestCase(-5L, "-5")]
        [TestCase(-10L, "-10")]
        public void Pad2IsPadLeftTwoZeros(long value, string expected)
        {
            Assert.That(TimeFormats.Pad2(value), Is.EqualTo(expected));
        }

        // Con ~/ y % de Dart: -65 → "-1:55", -5 → "00:55" (interval_step.dart:87-91, AC-RUT-04-04).
        [TestCase(0L, "00:00")]
        [TestCase(59L, "00:59")]
        [TestCase(125L, "02:05")]
        [TestCase(-65L, "-1:55")]
        [TestCase(-5L, "00:55")]
        [TestCase(6000L, "100:00")]
        public void MinSecUsesDartDivisionAndModulo(long seconds, string expected)
        {
            Assert.That(TimeFormats.MinSec(seconds), Is.EqualTo(expected));
        }

        // Tabla de AC-BLE-01-02 (caso 15).
        [TestCase(-5L, "--:--", Category = "AC-BLE-01-02")]
        [TestCase(0L, "--:--", Category = "AC-BLE-01-02")]
        [TestCase(1L, "00:01", Category = "AC-BLE-01-02")]
        [TestCase(59L, "00:59", Category = "AC-BLE-01-02")]
        [TestCase(60L, "01:00", Category = "AC-BLE-01-02")]
        [TestCase(120L, "02:00", Category = "AC-BLE-01-02")]
        [TestCase(125L, "02:05", Category = "AC-BLE-01-02")]
        [TestCase(133L, "02:13", Category = "AC-BLE-01-02")]
        [TestCase(599L, "09:59", Category = "AC-BLE-01-02")]
        [TestCase(600L, "10:00", Category = "AC-BLE-01-02")]
        [TestCase(5999L, "99:59", Category = "AC-BLE-01-02")]
        [TestCase(6000L, "100:00", Category = "AC-BLE-01-02")]
        [TestCase(6001L, "100:01", Category = "AC-BLE-01-02")]
        public void SplitOrDashes(long seconds, string expected)
        {
            Assert.That(TimeFormats.SplitOrDashes(seconds), Is.EqualTo(expected));
        }

        // Tabla de AC-BLE-01-03 (caso 15).
        [TestCase(0L, "00:00", Category = "AC-BLE-01-03")]
        [TestCase(1L, "00:01", Category = "AC-BLE-01-03")]
        [TestCase(59L, "00:59", Category = "AC-BLE-01-03")]
        [TestCase(60L, "01:00", Category = "AC-BLE-01-03")]
        [TestCase(61L, "01:01", Category = "AC-BLE-01-03")]
        [TestCase(3599L, "59:59", Category = "AC-BLE-01-03")]
        [TestCase(3600L, "1:00:00", Category = "AC-BLE-01-03")]
        [TestCase(3661L, "1:01:01", Category = "AC-BLE-01-03")]
        [TestCase(36000L, "10:00:00", Category = "AC-BLE-01-03")]
        [TestCase(86400L, "24:00:00", Category = "AC-BLE-01-03")]
        public void Elapsed(long seconds, string expected)
        {
            Assert.That(TimeFormats.Elapsed(seconds), Is.EqualTo(expected));
        }
    }
}
