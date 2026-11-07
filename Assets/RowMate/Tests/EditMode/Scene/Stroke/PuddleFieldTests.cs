using System;
using NUnit.Framework;
using RowMate.Core.Scene;
using RowMate.Core.Scene.Stroke;

namespace RowMate.Tests.EditMode.Scene.Stroke
{
    // PFD · Los charcos que se ven (D-ANI-02; AC-SCN-07-02, -03; ANIMATION_BIBLE §4.3): las reglas del legacy sobre la pala visible
    // (2 por palada, el izquierdo primero, 4 s, 0,3 → 0,9, fijos en el mundo, nada en pausa, sí a 12 spm, detrás de la cámara no) y
    // LT-91 en paralelo con la lista del contrato.
    [TestFixture]
    public sealed class PuddleFieldTests
    {
        [Test, Category("AC-SCN-07-02")]
        public void TwoPerExitLeftFirstWhereEachBladeIs()
        {
            var pose = ScullingPose.Create();
            var fit = RiggingFit.Solve(BoatSpec.Real, RowerSpec.Mannequin);
            ScullingKinematics.Evaluate(0.381, 0.375, BoatSpec.Real, RowerSpec.Mannequin, fit, RowerIdleSample.None, ref pose);
            var field = new PuddleField();
            field.OnBladeExit(pose, 1000 + SceneCamera.BoatZ);
            Assert.That(field.Count, Is.EqualTo(2));
            Assert.That(field[0].X, Is.LessThan(0), "primero el de x < 0");
            Assert.That(field[1].X, Is.GreaterThan(0));
            Assert.That(field[0].X, Is.EqualTo(pose.Right.BladeCenter.X).Within(0.1), "donde está la pala (± 0,1)");
            Assert.That(field[1].WorldZ - 1000 - SceneCamera.BoatZ, Is.EqualTo(pose.Left.BladeCenter.Z).Within(0.1));
            Assert.That(Math.Abs(field[1].X), Is.EqualTo(2.11).Within(0.02), "x ± 2,11 (arte §8.3)");
            Assert.That(field[1].WorldZ - 1000 - SceneCamera.BoatZ, Is.EqualTo(-1.18).Within(0.02), "z relativa −1,18");
        }

        [Test, Category("AC-SCN-07-02"), Category("AC-SCN-07-03")]
        public void TheyLiveFourSecondsGrowAndFade()
        {
            Assert.That(PuddleField.Radius(0), Is.EqualTo(0.3));
            Assert.That(PuddleField.Radius(4), Is.EqualTo(0.9).Within(1e-12));
            Assert.That(PuddleField.AlphaOf(0, 0.5), Is.EqualTo(0), "entrada de 0,15 s");
            Assert.That(PuddleField.AlphaOf(0.15, 0), Is.EqualTo(Math.Pow(1 - 0.15 / 4, 1.5) * 0.30).Within(1e-12));
            Assert.That(PuddleField.AlphaOf(2, 1), Is.EqualTo(Math.Pow(0.5, 1.5) * 0.65).Within(1e-12));
            Assert.That(PuddleField.AlphaOf(2, 0.917), Is.GreaterThan(PuddleField.AlphaOf(2, 0.167)), "más marcados con más vatios");
            Assert.That(PuddleField.SpinOf(4), Is.EqualTo(1.5 * 2).Within(1e-12), "1,5 rad/s decayendo");
            var field = new PuddleField();
            field.InjectForTest(-3, 6.3, 3.9);
            field.Step(0.05, 0);
            Assert.That(field.Count, Is.EqualTo(1));
            field.Step(0.06, 0);
            Assert.That(field.Count, Is.EqualTo(0), "se borran a los 4 s");
            Assert.That(PuddleField.IsBehindCamera(0.5), Is.True);
            Assert.That(PuddleField.IsBehindCamera(0.51), Is.False);
        }

        [Test, Category("AC-SCN-07-02")]
        public void FixedInTheWorldNoneInPauseAndAlsoAtTwelveSpm()
        {
            var s = new StrokeTestSession();
            s.Run(4, StrokeTestSession.Rowing(24));
            Assert.That(s.Rower.Puddles.Count, Is.GreaterThan(0));
            var z = s.Rower.Puddles[s.Rower.Puddles.Count - 1].WorldZ;
            var d = s.Frame.Distance;
            s.Run(0.3, StrokeTestSession.Rowing(24));
            Assert.That(s.Frame.Distance, Is.GreaterThan(d + 1), "el bote avanza");
            var stillThere = false;
            for (var i = 0; i < s.Rower.Puddles.Count; i++)
            {
                stillThere |= s.Rower.Puddles[i].WorldZ == z;
            }

            Assert.That(stillThere, Is.True, "su z de mundo no cambia: la cámara los pasa");

            // En pausa no nacen, pero los que hay envejecen.
            s.Run(1, StrokeTestSession.Pause());
            var count = s.Rower.Puddles.Count;
            var created = 0;
            for (var i = 0; i < 300; i++)
            {
                s.Step(StrokeTestSession.Pause());
                created += (s.Rower.Events & StrokeEvents.BladeExit) != 0 ? 1 : 0;
            }

            Assert.That(created, Is.EqualTo(0));
            Assert.That(s.Rower.Puddles.Count, Is.LessThanOrEqualTo(count));
            Assert.That(s.Rower.Puddles.Count, Is.EqualTo(0), "a los 5 s ya se apagaron todos");

            // A 12 spm (la palada que se cierra sola) también nacen.
            var closing = new StrokeTestSession();
            closing.Run(3, StrokeTestSession.Rowing(24));
            while (closing.Rower.Canonical < 0.05 || closing.Rower.Canonical > 0.3)
            {
                closing.Step(StrokeTestSession.Rowing(24));
            }

            var exits = 0;
            for (var i = 0; i < 360; i++)
            {
                closing.Step(StrokeTestSession.Rowing(0));
                exits += (closing.Rower.Events & StrokeEvents.BladeExit) != 0 ? 1 : 0;
            }

            Assert.That(exits, Is.EqualTo(1));
        }

        // legacy (en paralelo, LT-91): test/scene/scene_state_test.dart:69 con la lista visible.
        [Test, Category("AC-SCN-07-02"), Category("AC-SCN-07-01")]
        public void Lt91HoldsInParallelWithTheVisibleList()
        {
            var s = new StrokeTestSession();
            void Run(double seconds, double spm)
            {
                var n = (int)Math.Round(seconds / 0.05, MidpointRounding.AwayFromZero);
                for (var i = 0; i < n * 3; i++)
                {
                    s.Step(StrokeTestSession.Rowing(spm, 150, 107));
                }
            }

            Run(0.8, 24);
            Assert.That(s.Session.State.Puddles.Count, Is.EqualTo(0));
            Assert.That(s.Rower.Puddles.Count, Is.EqualTo(0));
            Run(0.3, 24);
            Assert.That(s.Session.State.Puddles.Count, Is.EqualTo(2));
            Assert.That(s.Rower.Puddles.Count, Is.EqualTo(2));
            Assert.That(s.Rower.Puddles[0].X, Is.LessThan(0));
            Assert.That(s.Rower.Puddles[1].X, Is.GreaterThan(0));
            Run(5, 0);
            Assert.That(s.Session.State.Puddles.Count, Is.EqualTo(0));
            Assert.That(s.Rower.Puddles.Count, Is.EqualTo(0));
        }
    }
}
