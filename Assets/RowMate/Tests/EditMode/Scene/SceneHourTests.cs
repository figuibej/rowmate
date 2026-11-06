using System;
using NUnit.Framework;
using RowMate.Core.Design;
using RowMate.Core.Scene;

namespace RowMate.Tests.EditMode.Scene
{
    // SHR · La hora de la escena (scene_view.dart:81-86; 06a-escena-lago/02-tech-design §2.5; AC-SCN-09-01, AC-SCN-23-02,
    // AC-BLE-16-07; DV-58, DN6a-10).
    [TestFixture]
    public sealed class SceneHourTests
    {
        private static readonly TimeSpan Offset = TimeSpan.FromHours(-3);

        [Test, Category("AC-SCN-09-01")]
        public void TheRealHourHasMinuteResolution()
        {
            var t = new DateTimeOffset(2026, 10, 28, 10, 0, 0, Offset);
            Assert.That(SceneHour.Effective(null, t), Is.EqualTo(10.0));
            Assert.That(SceneHour.Effective(null, t.AddSeconds(59)), Is.EqualTo(10.0), "los segundos se ignoran");
            Assert.That(SceneHour.Effective(null, t.AddMinutes(1)), Is.EqualTo(10 + 1 / 60.0));
            Assert.That(SceneHour.Effective(null, new DateTimeOffset(2026, 10, 28, 18, 30, 0, Offset)), Is.EqualTo(18.5));
            Assert.That(SceneHour.Effective(null, new DateTimeOffset(2026, 10, 28, 6, 12, 0, Offset)), Is.EqualTo(6 + 12 / 60.0),
                "hora + minuto/60, como el legacy");
        }

        [Test, Category("AC-SCN-09-01"), Category("AC-BLE-16-07"), Category("AC-SCN-23-02")]
        public void TheForcedHourWins()
        {
            var t = new DateTimeOffset(2026, 10, 28, 10, 0, 0, Offset);
            Assert.That(SceneHour.Effective(6, t), Is.EqualTo(6.0));
            Assert.That(SceneHour.Effective(0, t), Is.EqualTo(0.0));
            Assert.That(SceneHour.Effective(23, t), Is.EqualTo(23.0));
        }

        [Test, Category("AC-SCN-23-02"), Category("AC-BLE-16-07")]
        public void ForcedTwelveToEighteenInOneAndAHalfSecondsThroughFifteen()
        {
            var h = new SceneHourTransition();
            h.Retarget(12, 0, HourChange.Instant, false);
            h.Retarget(18, 10, HourChange.Forced, false);
            Assert.That(h.IsRunning, Is.True);
            Assert.That(h.Evaluate(10.75), Is.EqualTo(15).Within(1e-9), "a los 0,75 s, las 15");
            Assert.That(h.Evaluate(10 + 0.3), Is.EqualTo(12 + 6 * Easing.Evaluate(Tokens.EaseMove, 0.2)).Within(1e-9), "ease.move");
            Assert.That(h.Evaluate(11.5), Is.EqualTo(18), "termina exacto en el objetivo");
            Assert.That(h.IsRunning, Is.False);
            Assert.That(SceneHourTransition.Seconds, Is.EqualTo(1.5));
        }

        [TestCase(23, 1, 2, Category = "AC-SCN-23-02")]
        [TestCase(1, 23, -2, Category = "AC-SCN-23-02")]
        [TestCase(0, 12, 12, Category = "AC-SCN-23-02")]
        [TestCase(12, 0, 12, Category = "AC-SCN-23-02")]
        [TestCase(22, 10, 12, Category = "AC-SCN-23-02")]
        [TestCase(10, 22, 12, Category = "AC-SCN-23-02")]
        [TestCase(6, 5, -1, Category = "AC-SCN-23-02")]
        public void TheShortestWayAroundTheClock(int from, int to, int delta)
        {
            // De 23 a 0 hacia adelante, de 0 a 23 hacia atrás; con 12 h de diferencia, hacia adelante.
            var h = new SceneHourTransition();
            h.Retarget(from, 0, HourChange.Instant, false);
            h.Retarget(to, 0, HourChange.Forced, false);
            var mid = h.Evaluate(0.75);
            var expected = ((from + delta * 0.5) % 24 + 24) % 24;
            Assert.That(mid, Is.EqualTo(expected).Within(1e-9));
            Assert.That(h.Evaluate(2), Is.EqualTo(to));
        }

        [Test, Category("AC-SCN-23-02")]
        public void AChangeDuringAnotherStartsFromTheVisibleHour()
        {
            var h = new SceneHourTransition();
            h.Retarget(12, 0, HourChange.Instant, false);
            h.Retarget(18, 0, HourChange.Forced, false);
            var visible = h.Evaluate(0.75);
            h.Retarget(19, 0.75, HourChange.Forced, false);
            Assert.That(h.Evaluate(0.75), Is.EqualTo(visible).Within(1e-12), "sin salto");
            Assert.That(h.Evaluate(0.75 + 1.5), Is.EqualTo(19));
        }

        [Test, Category("AC-SCN-23-02"), Category("AC-APP-05-08")]
        public void WithReduceMotionEverythingIsInstant()
        {
            var h = new SceneHourTransition();
            h.Retarget(12, 0, HourChange.Instant, true);
            h.Retarget(18, 1, HourChange.Forced, true);
            Assert.That(h.Visible, Is.EqualTo(18));
            Assert.That(h.IsRunning, Is.False);
            h.Retarget(18 + 1 / 60.0, 2, HourChange.MinuteTick, true);
            Assert.That(h.Visible, Is.EqualTo(18 + 1 / 60.0));
        }

        [Test, Category("AC-SCN-09-01")]
        public void TheRealMinuteMovesLinearlyInOneSecond()
        {
            // DN6a-10 (P2): de 10:00 a 10:01 en 1 s lineal; a los 0,5 s, 10:00:30.
            var h = new SceneHourTransition();
            h.Retarget(10, 0, HourChange.Instant, false);
            h.Retarget(10 + 1 / 60.0, 5, HourChange.MinuteTick, false);
            Assert.That(h.Evaluate(5.5), Is.EqualTo(10 + 0.5 / 60).Within(1e-9));
            Assert.That(h.Evaluate(5.25), Is.EqualTo(10 + 0.25 / 60).Within(1e-9), "lineal");
            Assert.That(h.Evaluate(6), Is.EqualTo(10 + 1 / 60.0), "el dato es el del minuto");
            Assert.That(SceneHourTransition.MinuteSeconds, Is.EqualTo(1.0));
        }

        [Test, Category("AC-SCN-09-01")]
        public void TheMinuteTickAlsoCrossesMidnight()
        {
            var h = new SceneHourTransition();
            h.Retarget(23 + 59 / 60.0, 0, HourChange.Instant, false);
            h.Retarget(0, 1, HourChange.MinuteTick, false);
            Assert.That(h.Evaluate(1.5), Is.EqualTo(23 + 59.5 / 60).Within(1e-9), "hacia adelante, no 24 h atrás");
            Assert.That(h.Evaluate(2), Is.EqualTo(0));
        }

        [Test, Category("AC-SCN-09-01"), Category("AC-SCN-01-04")]
        public void ZoneChangesComingBackAndTestForcingAreInstant()
        {
            var h = new SceneHourTransition();
            h.Retarget(10, 0, HourChange.Instant, false);
            h.Retarget(13, 1, HourChange.Instant, false);
            Assert.That(h.Visible, Is.EqualTo(13), "un cambio de zona llega en el acto");
            h.Retarget(15, 2, HourChange.Forced, false);
            h.Retarget(20.5, 2.1, HourChange.Instant, false);
            Assert.That(h.Visible, Is.EqualTo(20.5), "la vuelta del segundo plano corta una transición");
            Assert.That(h.IsRunning, Is.False);
        }

        [Test, Category("AC-SCN-09-01")]
        public void TheFirstRetargetIsAlwaysInstant()
        {
            var h = new SceneHourTransition();
            Assert.That(double.IsNaN(h.Visible), Is.True);
            h.Retarget(7, 0, HourChange.Forced, false);
            Assert.That(h.Visible, Is.EqualTo(7));
        }

        [Test, Category("AC-SCN-23-02")]
        public void EveryIntermediateHourIsVisitedWithoutJumps()
        {
            // Como un time-lapse: entre dos cuadros de 1/60 s la hora visible nunca se corre más de lo que pide la curva.
            var h = new SceneHourTransition();
            h.Retarget(22, 0, HourChange.Instant, false);
            h.Retarget(10, 0, HourChange.Forced, false);
            var prev = h.Evaluate(0);
            var maxStep = 0.0;
            for (var i = 1; i <= 120; i++)
            {
                var v = h.Evaluate(i / 60.0);
                var step = ((v - prev) % 24 + 24) % 24;
                maxStep = Math.Max(maxStep, step);
                prev = v;
            }

            Assert.That(prev, Is.EqualTo(10));
            Assert.That(maxStep, Is.LessThan(0.5), "de 22 a 10 pasa por la noche y el amanecer en 1,5 s");
        }
    }
}
