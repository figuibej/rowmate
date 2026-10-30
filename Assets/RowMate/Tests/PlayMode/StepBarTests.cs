using System;
using System.Collections;
using NUnit.Framework;
using RowMate.Core.Design;
using RowMate.Tests.Support;
using UnityEngine;
using UnityEngine.TestTools;

namespace RowMate.Tests.PlayMode
{
    // SBT · La barra de pasos de tres filas (AC-WKT-14-01 a -06, -14, -15; AC-WKT-19-01, -05; 04b-hud-exr/02-tech-design §2.15,
    // §3.4): las píldoras, la fila de información con su plan (InfoRowFit, con los anchos medidos en Unity) y el sello.
    public sealed class StepBarTests : WorkoutUiTestBase
    {
        [UnityTest, Category("AC-WKT-14-01")]
        public IEnumerator ThreeRowsAndCardsDoNotMove()
        {
            CreateWorkout(RoutineSeeds.F4B);
            yield return StartRoutine("Estados");
            yield return AdvanceTo(2.2);
            var bar = Hud.Steps!;
            var s = (float)Immersive.Layout!.Scale;
            Assert.That(bar.childCount, Is.EqualTo(3), "las píldoras, la fila de información y el sello");
            Assert.That(bar[0], Is.SameAs(bar.PillRow));
            Assert.That(bar[2], Is.SameAs(bar.Seal));
            Assert.That(Bounds("hud-topbar").height / s, Is.EqualTo(52f).Within(0.5f), "52 base sin sello");
            var cardsTop = Bounds("hud-spm").yMin - H.Root.worldBound.yMin;
            Assert.That(cardsTop, Is.EqualTo(90 * s).Within(1f), "las tarjetas a 90·s (no se mueven con la barra)");
            yield return AdvanceTo(10.6);
            Assert.That(Bounds("hud-topbar").height / s, Is.EqualTo(74f).Within(0.5f), "74 con el sello");
            Assert.That(Bounds("hud-spm").yMin - H.Root.worldBound.yMin, Is.EqualTo(cardsTop).Within(0.5f), "las tarjetas no se mueven");
            Assert.That(Bounds("hud-topbar").yMax + 8 * s, Is.LessThanOrEqualTo(Bounds("hud-spm").yMin + 1f), "a 8·s de las tarjetas");
        }

        [UnityTest, Category("AC-WKT-14-14"), Category("AC-WKT-14-15"), Category("AC-WKT-19-01"), Category("AC-WKT-19-05")]
        public IEnumerator InfoRowOfCapOne()
        {
            CreateWorkout(RoutineSeeds.F4B);
            yield return StartRoutine("Estados");
            yield return AdvanceTo(40.2);
            var bar = Hud.Steps!;
            Assert.That(H.TextOf("hud-step-type"), Is.EqualTo("Trabajo"));
            Assert.That(bar.PillText, Is.EqualTo("18 spm"), "la pastilla entera");
            Assert.That(H.IsShown("hud-up-next"), Is.True);
            Assert.That(H.TextOf("hud-countdown"), Is.EqualTo("0:10"));
            Assert.That(H.TextOf("hud-wall-clock"), Is.EqualTo("10:00"), "la hora del equipo en UTC−03:00");
            Assert.That(bar.Clock.Icon, Is.EqualTo(Icons.Schedule), "con schedule de prefijo");
            Assert.That(H.Find("hud-wall-clock")!.focusable, Is.False, "sin foco");
            var row = Bounds("hud-topbar");
            var s = (float)Immersive.Layout!.Scale;
            Assert.That(row.xMax - Bounds("hud-wall-clock").xMax, Is.EqualTo((12 + 1) * s).Within(1.5f), "el reloj pegado al borde derecho");
            Assert.That(Bounds("hud-wall-clock").xMin, Is.GreaterThanOrEqualTo(Bounds("hud-countdown").xMax - 0.5f), "a la derecha de la cuenta");
            Assert.That(bar.Plan.Mode, Is.Not.EqualTo(InfoRowMode.Compact2), "en TV, CAP-01 entra en normal o compacto 1");
        }

        [UnityTest, Category("AC-WKT-14-03"), Category("AC-WKT-14-15")]
        public IEnumerator LongestRowCutsWholeFragmentsAndTheTargetFadesWithUpNext()
        {
            CreateWorkout(RoutineSeeds.F4B);
            yield return StartRoutine("Texto largo");
            yield return AdvanceTo(2.2);
            var bar = Hud.Steps!;
            Assert.That(H.TextOf("hud-step-type"), Is.EqualTo("Calentamiento"));
            Assert.That(bar.PillText, Does.EndWith(StepInfoBarEllipsis), "fragmentos enteros con el … pegado");
            Assert.That(bar.PillText, Does.Not.Contain(">15…"), "nunca un número cortado");
            Assert.That(Bounds("hud-wall-clock").xMax, Is.LessThanOrEqualTo(Bounds("hud-topbar").xMax + 0.5f), "nada sale de la barra");
            yield return AdvanceTo(8.2);
            yield return Settle();
            Assert.That(H.IsShown("hud-up-next"), Is.True, "entró Próximo");
            Assert.That(H.IsShown("hud-step-target"), Is.False, "la pastilla se fue al entrar Próximo");
            Assert.That(H.TextOf("hud-countdown"), Is.EqualTo("0:12"));
            Assert.That(H.TextOf("hud-wall-clock"), Is.EqualTo("10:00"));
        }

        private const string StepInfoBarEllipsis = RowMate.Presentation.Workout.StepInfoBar.Ellipsis;

        [UnityTest, Category("AC-WKT-14-06")]
        public IEnumerator PausedAndFinishedBar()
        {
            CreateWorkout(RoutineSeeds.F4A);
            yield return StartRoutine("Corta");
            yield return AdvanceTo(5.2);
            yield return Tap("hud-pause");
            var countdown = H.TextOf("hud-countdown");
            yield return Advance(3);
            Assert.That(H.TextOf("hud-countdown"), Is.EqualTo(countdown), "en pausa la cuenta queda quieta");
            yield return Tap("hud-resume");
            yield return AdvanceTo(40);
            Assert.That(H.TextOf("hud-countdown"), Is.EqualTo("0:00"), "terminada: el último paso con 0:00");
            Assert.That(H.IsShown("hud-up-next"), Is.False, "sin Próximo");
        }
    }
}
