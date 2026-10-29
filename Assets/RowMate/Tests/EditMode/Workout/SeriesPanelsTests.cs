using NUnit.Framework;
using RowMate.Core.Workout;

namespace RowMate.Tests.EditMode.Workout
{
    // LT-45: los formatos de los paneles de series (lib/features/workout/series_panels.dart:9-24).
    [TestFixture]
    public sealed class SeriesPanelsTests
    {
        // legacy: test/series_panels_test.dart:32 'formatos'
        [TestCase(TestName = "formatos", Category = "LT-45,AC-WKT-11-09,AC-WKT-11-10,AC-WKT-11-11")]
        public void Formats()
        {
            Assert.That(WorkoutFormats.LapTime(166.667), Is.EqualTo("2:46.7"));
            Assert.That(WorkoutFormats.LapTime(65), Is.EqualTo("1:05.0"));
            Assert.That(WorkoutFormats.RepSplit(114.4), Is.EqualTo("1:54"));
            Assert.That(WorkoutFormats.PanelMeters(1245), Is.EqualTo("1.245 m"));
        }
    }
}
