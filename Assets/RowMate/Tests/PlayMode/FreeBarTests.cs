using System;
using System.Collections;
using NUnit.Framework;
using RowMate.Presentation.Workout;
using RowMate.Tests.Support;
using UnityEngine;
using UnityEngine.TestTools;

namespace RowMate.Tests.PlayMode
{
    // FBT · La barra libre: el reloj de pared y el chip de fase (AC-WKT-16-01 a -06, AC-WKT-19-01 a -05; 04b-hud-exr/02-tech-design
    // §2.7, §2.8, §2.15, §3.7; DN4b-26, -27).
    public sealed class FreeBarTests : WorkoutUiTestBase
    {
        [UnityTest, Category("AC-WKT-16-01"), Category("AC-WKT-16-02"), Category("AC-WKT-16-03"), Category("AC-WKT-16-05"), Category("AC-WKT-19-01")]
        public IEnumerator ContentInOrderWithClockAndChip()
        {
            CreateWorkout(RoutineSeeds.Lista);
            yield return StartFree();
            yield return AdvanceTo(42.2);
            var bar = Hud.Free!;
            Assert.That(H.TextOf("hud-free-title-text"), Is.EqualTo("Entrenamiento Libre"));
            Assert.That(H.TextOf("hud-wall-clock"), Is.EqualTo("10:00"), "la hora del equipo (UTC−03:00), 24 h");
            Assert.That(H.TextOf("hud-phase-label"), Is.EqualTo("EN MARCHA"));
            Assert.That(H.IsShown("hud-phase-dot"), Is.True, "el punto en vivo en marcha");
            Assert.That(Bounds("hud-free-title").xMax, Is.LessThanOrEqualTo(Bounds("hud-wall-clock").xMin + 0.5f), "el título, el reloj y el chip en orden");
            Assert.That(Bounds("hud-wall-clock").xMax, Is.LessThanOrEqualTo(Bounds("hud-phase-chip").xMin + 0.5f));
            Assert.That(Bounds("hud-phase-chip").xMax, Is.LessThanOrEqualTo(Bounds("hud-topbar").xMax + 0.5f), "nada sale de la barra");
            var s = (float)Immersive.Layout!.Scale;
            // El ancho fijo medido en Unity (R4b-09): el de la etiqueta más ancha con el lugar del punto en EN MARCHA, más el relleno;
            // nunca más angosto que lo que muestra. QA-V4b-02: con el tracking de 0,08 em (8 en la unidad de TextCore, N = N/100 em)
            // da el 101,8 de las notas (04-design-notes §3.5); con 0,88 (el tracking escrito en dp) quedaba en 95,3.
            var chip = bar.Chip;
            Assert.That(chip.Label.resolvedStyle.letterSpacing, Is.EqualTo(8f).Within(0.001f), "tracking 0,08 em = 8 en la unidad de TextCore");
            Assert.That(chip.FixedWidth, Is.EqualTo(101.8f).Within(1f), "el ancho fijo, medido (04-design-notes §3.5: 101,8 en es)");
            Assert.That(Bounds("hud-phase-chip").width / s, Is.EqualTo(chip.FixedWidth).Within(0.5f), "el chip mide su ancho fijo");
            Assert.That(chip.Label.layout.width + chip.Dot.layout.width + 4 + 2 * 8, Is.LessThanOrEqualTo(chip.FixedWidth + 0.5f), "la etiqueta entra");
            // En TV y en español el título se achica (DN4b-27): las notas, con fontTools y sin aire, daban 0,818; con el chip a su ancho
            // y el aire mínimo de 2 con el reloj (QA-V4b-02), ≈ 0,805, entero.
            Assert.That(bar.TitleScale, Is.GreaterThanOrEqualTo(0.8f).And.LessThan(1f), "en TV el título se achica, entero (DN4b-27)");
            Assert.That(bar.TitleScale, Is.EqualTo(0.805f).Within(0.004f), "≈ 0,805 (InfoRowFitTests.FreeTitleFitsInTvWithTheChipAtItsTracking)");
            Assert.That(bar.TitleTruncated, Is.False, "sin …");
            Assert.That(Bounds("hud-wall-clock").xMin - Bounds("hud-free-title").xMax, Is.GreaterThanOrEqualTo(FreeTopBar.TitleClockGapDp * s - 0.5f),
                "el título achicado no queda pegado al reloj");
        }

        [UnityTest, Category("AC-WKT-16-03"), Category("AC-WKT-16-04")]
        public IEnumerator ChipChangesWithThePhaseAndKeepsItsWidth()
        {
            CreateWorkout(RoutineSeeds.Lista);
            yield return StartFree();
            yield return AdvanceTo(5.2);
            var width = Bounds("hud-phase-chip").width;
            var clock = Bounds("hud-wall-clock");
            yield return Tap("hud-pause");
            Assert.That(H.TextOf("hud-phase-label"), Is.EqualTo("PAUSADO"), "en el cuadro de la fase");
            Assert.That(H.IsShown("hud-phase-dot"), Is.False, "sin punto en pausa");
            yield return Settle();
            Assert.That(Bounds("hud-phase-chip").width, Is.EqualTo(width).Within(0.5f), "el mismo ancho");
            Assert.That(Bounds("hud-wall-clock").xMin, Is.EqualTo(clock.xMin).Within(0.5f), "el reloj no se corre");
        }

        [UnityTest, Category("AC-WKT-16-06")]
        public IEnumerator EnglishTitleFitsAtItsSize()
        {
            CreateWorkout(RoutineSeeds.Lista, o => o.LanguageTags = new[] { "en-US" });
            yield return StartFree();
            yield return AdvanceTo(3.2);
            Assert.That(H.TextOf("hud-free-title-text"), Is.EqualTo("Free Workout"));
            Assert.That(Hud.Free!.TitleScale, Is.EqualTo(1f).Within(0.001f), "en inglés entra a su tamaño");
            Assert.That(H.TextOf("hud-phase-label"), Is.EqualTo("ACTIVE"));
            Assert.That(Hud.Free!.Chip.FixedWidth, Is.EqualTo(74.1f).Within(1f), "el ancho fijo en inglés (04-design-notes §3.5: 74,1; QA-V4b-02)");
        }

        [UnityTest, Category("AC-WKT-19-02"), Category("AC-WKT-19-04")]
        public IEnumerator WallClockChangesOnTheMinuteAndWithTheZone()
        {
            CreateWorkout(RoutineSeeds.Lista);
            yield return StartFree();
            // Hasta medio segundo antes del minuto siguiente de la hora del equipo (la sesión no empieza en un minuto entero).
            var local = H.Scheduler.LocalNow;
            var toMinute = 60 - local.Second - local.Millisecond / 1000.0;
            yield return Advance(toMinute - 0.5);
            Assert.That(H.TextOf("hud-wall-clock"), Is.EqualTo("10:00"));
            yield return Advance(1.0);
            Assert.That(H.TextOf("hud-wall-clock"), Is.EqualTo("10:01"), "el minuto nuevo antes de 1 s");
            H.SetLocalOffset(TimeSpan.FromHours(-5));
            yield return Advance(1);
            Assert.That(H.TextOf("hud-wall-clock"), Is.EqualTo("08:01"), "la zona nueva antes de 1 s");
            Assert.That(H.Find("hud-wall-clock")!.focusable, Is.False, "sin foco");
        }
    }
}
