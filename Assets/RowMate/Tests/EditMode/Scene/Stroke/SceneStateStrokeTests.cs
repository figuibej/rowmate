using System;
using NUnit.Framework;
using RowMate.Core.Scene;

namespace RowMate.Tests.EditMode.Scene.Stroke
{
    // SSK · La fase del contrato, los charcos del contrato y el cabeceo (scene/scene_state.dart:59-85; F-SCN-04, F-SCN-07; LT-88 a
    // LT-91): los casos de test/scene/scene_state_test.dart con su run() de pasos de 0,05 s, sin cambiar un número.
    [TestFixture]
    public sealed class SceneStateStrokeTests
    {
        // scene_state_test.dart:5-13: avanza `seconds` en pasos de 0,05 s ((seconds / 0.05).round()).
        private static void Run(SceneState s, double seconds, double pace = 107, double spm = 24, bool active = true)
        {
            var steps = (int)Math.Round(seconds / 0.05, MidpointRounding.AwayFromZero);
            for (var i = 0; i < steps; i++)
            {
                s.Update(0.05, pace, spm, active);
            }
        }

        // legacy: test/scene/scene_state_test.dart:37 'la fase de palada avanza a spm/60 ciclos por segundo'
        [TestCase(TestName = "la fase de palada avanza a spm/60 ciclos por segundo", Category = "LT-88,AC-SCN-04-01")]
        public void PhaseAdvancesAtSpmOverSixty()
        {
            var s = new SceneState();
            Run(s, 1, spm: 24);
            Assert.That(s.StrokePhase, Is.EqualTo(0.4).Within(1e-6));
            Run(s, 1.5, spm: 24);
            // Un ciclo completo: por redondeo puede quedar en 0.0 o en 0.999…
            Assert.That(Math.Abs(s.StrokePhase - 0.0) <= 1e-6 || Math.Abs(s.StrokePhase - 1.0) <= 1e-6, Is.True, "fase " + s.StrokePhase);
        }

        // legacy: test/scene/scene_state_test.dart:46 'sin paladas la fase termina el ciclo y queda en el catch'
        [TestCase(TestName = "sin paladas la fase termina el ciclo y queda en el catch", Category = "LT-89,AC-SCN-04-02")]
        public void WithoutStrokesThePhaseFinishesTheCycle()
        {
            var s = new SceneState();
            Run(s, 0.5, spm: 24);
            Assert.That(s.StrokePhase, Is.EqualTo(0.2).Within(1e-6));
            Run(s, 6, spm: 0, pace: 107);
            Assert.That(s.StrokePhase, Is.EqualTo(0.0));
            Assert.That(s.Speed, Is.LessThan(0.1));
        }

        // legacy: test/scene/scene_state_test.dart:60 'en pausa no avanza la palada ni la velocidad'
        [TestCase(TestName = "en pausa no avanza la palada ni la velocidad", Category = "LT-90,AC-SCN-04-03")]
        public void PauseFreezesThePhase()
        {
            var s = new SceneState();
            Run(s, 0.5);
            var phase = s.StrokePhase;
            Run(s, 2, active: false);
            Assert.That(s.StrokePhase, Is.EqualTo(phase));
            Assert.That(s.Speed, Is.LessThan(0.5));
        }

        // legacy: test/scene/scene_state_test.dart:69 'al soltar la pala aparecen dos charcos que envejecen y desaparecen'
        [TestCase(TestName = "al soltar la pala aparecen dos charcos que envejecen y desaparecen", Category = "LT-91,AC-SCN-07-01,AC-SCN-07-02")]
        public void ReleasingTheBladeLeavesTwoPuddles()
        {
            var s = new SceneState();
            Run(s, 0.8, spm: 24); // fase ≈ 0.32, pala en el agua
            Assert.That(s.Puddles.Count, Is.EqualTo(0));
            Run(s, 0.3, spm: 24); // cruza 0.38
            Assert.That(s.Puddles.Count, Is.EqualTo(2));
            Assert.That(s.Puddles[0].X, Is.LessThan(0));
            Assert.That(s.Puddles[s.Puddles.Count - 1].X, Is.GreaterThan(0));
            Run(s, 5, spm: 0);
            Assert.That(s.Puddles.Count, Is.EqualTo(0));
        }

        [Test, Category("AC-SCN-07-01")]
        public void ThePuddlesOfTheContractAreWhereTheLegacyPutsThem()
        {
            // x = ±(0,85 + cos α·2,9) ≈ ±3,23 y z = distancia + 9,5 − sin α·2,9 ≈ distancia + 7,84, con el α de la fase del cruce.
            var s = new SceneState();
            while (s.Puddles.Count == 0)
            {
                s.Update(0.05, 107, 24, true);
            }

            var sweep = StrokeCycle.OarSweep(s.StrokePhase);
            Assert.That(s.Puddles[0].X, Is.EqualTo(-(0.85 + Math.Cos(sweep) * 2.9)));
            Assert.That(s.Puddles[1].X, Is.EqualTo(0.85 + Math.Cos(sweep) * 2.9));
            Assert.That(s.Puddles[0].X, Is.EqualTo(-3.23).Within(0.02));
            Assert.That(s.Puddles[0].WorldZ - s.Distance, Is.EqualTo(7.84).Within(0.02));
            Assert.That(s.Puddles[0].Age, Is.EqualTo(0.05).Within(1e-12), "envejece en el mismo paso en que nace");
        }

        [Test, Category("AC-SCN-07-01")]
        public void AtTheStartTheBladeCountsAsInAndPuddlesAgeInPause()
        {
            // :31: _bladeWasIn = true; si la sesión arranca con la pala afuera no hay charco hasta la próxima salida. En pausa los
            // charcos siguen envejeciendo (:79-81).
            var s = new SceneState();
            s.ForceStrokeForTest(0.5);
            s.Update(0.05, 107, 24, true);
            Assert.That(s.Puddles.Count, Is.EqualTo(2), "la pala estaba adentro (al empezar) y en 0,52 está afuera");
            var age = s.Puddles[0].Age;
            s.Update(0.05, 107, 24, false);
            Assert.That(s.Puddles[0].Age, Is.EqualTo(age + 0.05).Within(1e-12));
            s.Reset();
            Assert.That(s.Puddles.Count, Is.EqualTo(0));
            Assert.That(s.StrokePhase, Is.EqualTo(0));
        }

        [Test, Category("AC-SCN-06-01")]
        public void ThePitchIsTheLegacyFormulaOnThePhase()
        {
            // :85: 0,02·sin(2πp) + 0,006·sin(0,8·t).
            var s = new SceneState();
            for (var i = 0; i < 37; i++)
            {
                s.Update(0.05, 107, 24, true);
                Assert.That(s.Pitch, Is.EqualTo(0.02 * Math.Sin(s.StrokePhase * 2 * Math.PI) + 0.006 * Math.Sin(s.Time * 0.8)));
            }
        }
    }
}
