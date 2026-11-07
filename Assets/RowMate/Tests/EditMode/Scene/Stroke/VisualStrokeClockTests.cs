using System;
using NUnit.Framework;
using RowMate.Core.Scene;
using RowMate.Core.Scene.Stroke;

namespace RowMate.Tests.EditMode.Scene.Stroke
{
    // VSC · La fase visual φ (D-ANI-01; AC-SCN-04-04, -05; AC-SCN-05-10; ANIMATION_BIBLE §3.1, §3.7, §3.9; QA7-G-03, -07): sigue a p
    // sin saltos ni retrocesos, frena en pausa, cierra la palada sin paladas y queda trabada en el catch, fija r solo en el catch y da
    // cada evento una vez por palada. Con SceneState real (el contrato) a 60 fps.
    [TestFixture]
    public sealed class VisualStrokeClockTests
    {
        private const double Frame = 1.0 / 60;

        private sealed class Rig
        {
            public readonly SceneState State = new SceneState();
            public readonly VisualStrokeClock Clock = new VisualStrokeClock();
            public StrokeEvents Events;
            public double LastIncrement;

            public void Step(double spm, bool active = true, double pace = 125)
            {
                var before = Clock.Phase;
                State.Update(Frame, pace, spm, active);
                Events = Clock.Step(Frame, State.StrokePhase, spm, active);
                var inc = Clock.Phase - before;
                LastIncrement = inc < -0.5 ? inc + 1 : inc;
            }

            public void Run(double seconds, double spm, bool active = true)
            {
                var n = (int)Math.Round(seconds * 60);
                for (var i = 0; i < n; i++)
                {
                    Step(spm, active);
                }
            }

            public double Error => Math.Abs(VisualStrokeClock.Wrap(State.StrokePhase - Clock.Phase));
        }

        [Test, Category("AC-SCN-04-04")]
        public void InSteadyStateItIsTheContractPhase()
        {
            var rig = new Rig();
            rig.Run(3, 24);
            for (var i = 0; i < 600; i++)
            {
                rig.Step(24);
                Assert.That(rig.Error, Is.LessThan(0.001));
            }
        }

        [Test, Category("AC-SCN-04-04")]
        public void AStepFrom24To30IsSmoothAndAbsorbedInOneStroke()
        {
            var rig = new Rig();
            rig.Run(5, 24);
            var max = 0.0;
            var previous = rig.LastIncrement;
            for (var i = 0; i < 120; i++)
            {
                rig.Step(30);
                max = Math.Max(max, rig.Error);
                Assert.That(Math.Abs(rig.LastIncrement - previous), Is.LessThan(0.02 * 30.0 / 60 * Frame + 1e-12), "sin salto de velocidad (< 2 % por cuadro)");
                previous = rig.LastIncrement;
            }

            Assert.That(max, Is.LessThanOrEqualTo(0.05));
            Assert.That(rig.Error, Is.LessThan(0.002), "absorbido en una palada (2 s a 30 spm)");
        }

        [Test, Category("AC-SCN-04-04")]
        public void ItNeverGoesBack()
        {
            var rig = new Rig();
            var script = new[] { (2.0, 24.0, true), (1.0, 36.0, true), (0.7, 36.0, false), (1.3, 18.0, true), (2.0, 0.0, true), (1.0, 30.0, true), (0.5, 30.0, false) };
            foreach (var (seconds, spm, active) in script)
            {
                for (var i = 0; i < (int)(seconds * 60); i++)
                {
                    rig.Step(spm, active);
                    Assert.That(rig.LastIncrement, Is.GreaterThanOrEqualTo(0));
                }
            }
        }

        [Test, Category("AC-SCN-04-05")]
        public void TheRatioOnlyChangesAtTheCatch()
        {
            var rig = new Rig();
            rig.Run(3, 20);
            var r = rig.Clock.LatchedRatio;
            var changes = 0;
            for (var i = 0; i < 600; i++)
            {
                rig.Step(i < 100 ? 20 : 30);
                if (rig.Clock.LatchedRatio != r)
                {
                    Assert.That((rig.Events & StrokeEvents.Catch) != 0, Is.True, "r cambió fuera del catch");
                    r = rig.Clock.LatchedRatio;
                    changes++;
                }
            }

            Assert.That(changes, Is.GreaterThan(0));
            Assert.That(r, Is.EqualTo(StrokeRhythm.DriveRatio(30)).Within(0.002), "después de unas paladas a 30");
        }

        [Test, Category("AC-SCN-04-04"), Category("AC-SCN-05-10")]
        public void InPauseItStopsWithDeceleration()
        {
            var rig = new Rig();
            rig.Run(4, 24);
            var start = rig.Clock.Phase;
            double at400 = 0;
            var stopped = double.NaN;
            for (var i = 1; i <= 120; i++)
            {
                rig.Step(24, false);
                if (i == 24)
                {
                    at400 = rig.Clock.Phase;
                }

                if (double.IsNaN(stopped) && rig.LastIncrement < 1e-6)
                {
                    stopped = i * Frame;
                }

                Assert.That(rig.Events, Is.EqualTo(StrokeEvents.None), "en pausa no hay eventos");
            }

            Assert.That(rig.Clock.Phase - at400, Is.LessThan(0.005), "quieta a los 0,4 s");
            Assert.That(stopped, Is.GreaterThan(0.1).And.LessThan(0.4), "frena en ≈ 0,25 s, no de golpe");
            Assert.That(rig.Clock.Phase - start, Is.GreaterThan(0.005).And.LessThan(0.06));
            Assert.That(rig.Clock.PhaseSpeed, Is.LessThan(0.01));
            Assert.That(rig.Clock.StillSeconds, Is.GreaterThan(1.0), "quieto en pausa: cuenta para el reposo");
        }

        [Test, Category("AC-SCN-04-04"), Category("AC-SCN-05-10")]
        public void ResumingContinuesFromWhereItWas()
        {
            var rig = new Rig();
            rig.Run(4, 24);
            rig.Run(2, 24, false);
            var phase = rig.Clock.Phase;
            rig.Step(24);
            Assert.That(rig.Clock.Phase - phase, Is.LessThan(0.01), "sin salto al reanudar");
            rig.Run(4, 24);
            Assert.That(rig.Error, Is.LessThan(0.001));
        }

        [TestCase(0.3, Category = "AC-SCN-04-04,AC-SCN-05-10")]
        [TestCase(0.99, Category = "AC-SCN-04-04,AC-SCN-05-10")]
        public void WithoutStrokesItClosesTheStrokeAndLatchesAtTheCatch(double phase)
        {
            // QA7-G-03: desde 24 spm con p en 0,3 o 0,99, spm 0: después de 6 s φ = 0 y ω_s = 0 exactos, un solo Catch y el reposo crece.
            var rig = new Rig();
            rig.Run(3, 24);
            while (Math.Abs(rig.State.StrokePhase - phase) > 0.01)
            {
                rig.Step(24);
            }

            var catches = 0;
            for (var i = 0; i < 360; i++)
            {
                rig.Step(0);
                if ((rig.Events & StrokeEvents.Catch) != 0)
                {
                    catches++;
                }
            }

            Assert.That(rig.Clock.Phase, Is.EqualTo(0.0));
            Assert.That(rig.Clock.PhaseSpeed, Is.EqualTo(0.0));
            Assert.That(rig.Clock.Canonical, Is.EqualTo(0.0));
            Assert.That(catches, Is.EqualTo(1));
            Assert.That(rig.State.StrokePhase, Is.EqualTo(0.0), "el contrato también terminó");
            var still = rig.Clock.StillSeconds;
            rig.Run(1, 0);
            Assert.That(rig.Clock.StillSeconds, Is.EqualTo(still + 1).Within(1e-9));
            rig.Step(24);
            Assert.That(rig.Clock.IsLatched, Is.False, "con spm > 0 se suelta");
        }

        [Test, Category("AC-SCN-04-04")]
        public void TheClosingStrokeKeepsItsRatio()
        {
            // QA7-G-07: la palada que se cierra sin paladas conserva su r; el siguiente se fija en el próximo catch.
            var rig = new Rig();
            rig.Run(3, 30);
            while (rig.State.StrokePhase > 0.05 || rig.State.StrokePhase < 0.02)
            {
                rig.Step(30);
            }

            var r = rig.Clock.LatchedRatio;
            for (var i = 0; i < 300 && !rig.Clock.IsLatched; i++)
            {
                rig.Step(0);
                if (!rig.Clock.IsLatched)
                {
                    Assert.That(rig.Clock.LatchedRatio, Is.EqualTo(r));
                }
            }

            Assert.That(rig.Clock.LatchedRatio, Is.GreaterThanOrEqualTo(StrokeRhythm.DriveRatio(12) - 1e-12));
        }

        [TestCase(20, Category = "AC-SCN-04-05")]
        [TestCase(24, Category = "AC-SCN-04-05")]
        [TestCase(30, Category = "AC-SCN-04-05")]
        public void EachEventOncePerStrokeAndTheDriveLastsWhatTheTableSays(double spm)
        {
            var rig = new Rig();
            rig.Run(4, spm);
            int catches = 0, entries = 0, exits = 0, finishes = 0;
            double lastCatch = double.NaN, lastFinish = double.NaN, drive = double.NaN;
            for (var i = 0; i < 60 * 30; i++)
            {
                rig.Step(spm);
                var t = i * Frame;
                if ((rig.Events & StrokeEvents.Catch) != 0)
                {
                    catches++;
                    lastCatch = t;
                }

                if ((rig.Events & StrokeEvents.BladeEntry) != 0)
                {
                    entries++;
                }

                if ((rig.Events & StrokeEvents.BladeExit) != 0)
                {
                    exits++;
                }

                if ((rig.Events & StrokeEvents.Finish) != 0)
                {
                    finishes++;
                    lastFinish = t;
                    if (!double.IsNaN(lastCatch))
                    {
                        drive = lastFinish - lastCatch;
                    }
                }
            }

            var strokes = 30 * spm / 60;
            Assert.That(catches, Is.EqualTo(strokes).Within(1));
            Assert.That(entries, Is.EqualTo(catches).Within(1));
            Assert.That(exits, Is.EqualTo(catches).Within(1));
            Assert.That(finishes, Is.EqualTo(catches).Within(1));
            // El drive visible: del catch al finish (c 0 → 0,36) = 0,9·r·ciclo.
            Assert.That(drive, Is.EqualTo(0.9 * StrokeRhythm.DriveRatio(spm) * 60 / spm).Within(0.05));
        }

        [Test, Category("AC-SCN-04-04")]
        public void TheForcedPhaseIsFixedAndSilent()
        {
            var rig = new Rig();
            rig.Run(2, 24);
            rig.Clock.ForceCanonicalForTest(0.18);
            for (var i = 0; i < 60; i++)
            {
                rig.Step(24);
                Assert.That(rig.Clock.Canonical, Is.EqualTo(0.18));
                Assert.That(rig.Events, Is.EqualTo(StrokeEvents.None));
            }

            Assert.That(rig.Clock.Phase, Is.EqualTo(StrokeRhythm.FromCanonical(0.18, rig.Clock.LatchedRatio)).Within(1e-12));
            rig.Clock.ForceCanonicalForTest(null);
            rig.Step(24);
            Assert.That(rig.Clock.PhaseSpeed, Is.GreaterThan(0));
        }
    }
}
