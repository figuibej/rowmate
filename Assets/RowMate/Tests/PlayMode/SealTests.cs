using System;
using System.Collections;
using NUnit.Framework;
using RowMate.Core.Design;
using RowMate.Tests.Support;
using UnityEngine;
using UnityEngine.TestTools;

namespace RowMate.Tests.PlayMode
{
    // SVT · El sello del paso que cerró solo con objetivos (AC-WKT-09-08, -09; AC-WKT-13-05; 04b-hud-exr/02-tech-design §2.5,
    // §3.6): la tercera fila de la barra, en una línea, durante 2 s.
    public sealed class SealTests : WorkoutUiTestBase
    {
        [UnityTest, Category("AC-WKT-09-08"), Category("AC-WKT-09-09")]
        public IEnumerator MetAndMissedOnScreenWithinTwoSeconds()
        {
            CreateWorkout(RoutineSeeds.F4B);
            yield return StartRoutine("Estados");
            yield return AdvanceTo(10.6);
            yield return Settle();
            var seal = Hud.Steps!.Seal;
            Assert.That(seal.IsShowing, Is.True, "el sello del paso 0");
            Assert.That(seal.Title.text, Is.EqualTo("Objetivo cumplido"));
            Assert.That(seal.Detail.text, Is.EqualTo(" · 24.0 / 18 spm"));
            Assert.That(seal.Icon.Codepoint, Is.EqualTo(Icons.CheckCircle));
            Assert.That(SameColor(seal.Title.resolvedStyle.color, Tokens.StateSuccess), Is.True, "en state.success");
            var s = (float)Immersive.Layout!.Scale;
            Assert.That(Bounds("hud-topbar").height / s, Is.EqualTo(74f).Within(0.5f), "la barra en 74");
            Assert.That(seal.Title.worldBound.yMin, Is.GreaterThanOrEqualTo(Bounds("hud-step-type").yMax - 0.5f), "debajo de la fila de información");
            Assert.That(Math.Abs(seal.Title.worldBound.center.y - seal.Detail.worldBound.center.y), Is.LessThan(2f), "en una línea");
            yield return AdvanceTo(12.6);
            yield return Settle();
            Assert.That(seal.IsShowing, Is.False, "se fue a los 2 s");
            Assert.That(Bounds("hud-topbar").height / s, Is.EqualTo(52f).Within(0.5f), "la barra vuelve a 52");
            yield return AdvanceTo(20.6);
            yield return Settle();
            Assert.That(seal.Title.text, Is.EqualTo("No alcanzado"), "el paso 1 no se cumplió");
            Assert.That(seal.Detail.text, Is.EqualTo(" · 24.0 / 30 spm"));
            Assert.That(seal.Icon.Codepoint, Is.EqualTo(Icons.Error));
            Assert.That(SameColor(seal.Title.resolvedStyle.color, Tokens.PhasePaused), Is.True, "en #FFD166");
        }

        [UnityTest, Category("AC-WKT-09-08"), Category("AC-WKT-13-05")]
        public IEnumerator NoSealWithoutTargetsNorOnTheLastStepAndNoRoutineName()
        {
            CreateWorkout(RoutineSeeds.F4B);
            yield return StartRoutine("Estados");
            yield return AdvanceTo(30.6);
            Assert.That(Hud.Steps!.Seal.IsShowing, Is.False, "el descanso sin objetivo no tiene sello");
            Assert.That(H.IsTextVisible("Estados"), Is.False, "sin el nombre de la rutina (AC-WKT-13-05)");
            yield return AdvanceTo(60.6);
            Assert.That(Hud.Steps.Seal.IsShowing, Is.False, "el último paso no tiene sello (Q-52)");
        }

        [UnityTest, Category("AC-WKT-09-08")]
        public IEnumerator SealKeepsItsTwoSecondsInPause()
        {
            CreateWorkout(RoutineSeeds.F4B);
            yield return StartRoutine("Estados");
            yield return AdvanceTo(10.4);
            yield return Tap("hud-pause");
            yield return Advance(2.5);
            yield return Settle();
            Assert.That(Hud.Steps!.Seal.IsShowing, Is.False, "en pausa sigue sus 2 s (el reloj de la interfaz)");
        }
    }
}
