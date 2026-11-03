using System;
using System.Collections;
using NUnit.Framework;
using RowMate.Presentation.Workout;
using RowMate.Tests.Support;
using RowMate.Tests.UiSupport;
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
            Assert.That(chip.NormalWidth, Is.EqualTo(101.8f).Within(1f), "el ancho fijo normal, medido (04-design-notes §3.5: 101,8 en es)");
            // It. 5 (DD5-01): en TV en español la barra está en el modo compacto: el chip con relleno 6 (97,3 base ± 1).
            Assert.That(bar.IsCompact, Is.True, "TV en español: el título no entra a su tamaño");
            Assert.That(chip.FixedWidth, Is.EqualTo(97.3f).Within(1f), "el ancho fijo compacto (00-deuda-diseno de la 5 §1.3)");
            Assert.That(chip.FixedWidth, Is.EqualTo(chip.NormalWidth - 4).Within(0.01f), "4 base menos: 2 de relleno de cada lado");
            Assert.That(Bounds("hud-phase-chip").width / s, Is.EqualTo(chip.FixedWidth).Within(0.5f), "el chip mide su ancho fijo");
            Assert.That(chip.Label.layout.width + chip.Dot.layout.width + 4 + 2 * 6, Is.LessThanOrEqualTo(chip.FixedWidth + 0.5f), "la etiqueta entra");
            // En TV y en español el título se achica (DN4b-27): las notas, con fontTools y sin aire, daban 0,818; con el chip a su ancho
            // y el aire mínimo de 2 con el reloj (QA-V4b-02), ≈ 0,805, entero.
            Assert.That(bar.TitleScale, Is.GreaterThanOrEqualTo(0.8f).And.LessThan(1f), "en TV el título se achica, entero (DN4b-27)");
            Assert.That(bar.TitleScale, Is.EqualTo(0.805f).Within(0.004f), "≈ 0,805 (InfoRowFitTests.FreeTitleFitsInTvWithTheChipAtItsTracking)");
            Assert.That(bar.TitleTruncated, Is.False, "sin …");
            Assert.That(Bounds("hud-wall-clock").xMin - Bounds("hud-free-title").xMax, Is.GreaterThanOrEqualTo(FreeTopBar.TitleClockGapDp * s - 0.5f),
                "el título achicado no queda pegado al reloj");
        }

        // It. 5 (QA-V4b-R2-01, AC-WKT-16-07; 05-series-parciales/00-deuda-diseno §1.3, DD5-01): en TV en español, el modo compacto
        // separa el título del reloj (10·s ± 0,5 dp, más que el espacio entre las palabras del título), con el ícono a 3·s de sus
        // cifras y el chip a 8·s, y el título sigue entero a ≈ 0,805.
        [UnityTest, Category("AC-WKT-16-07"), Category("AC-WKT-16-06"), Category("QA-V4b-R2-01")]
        public IEnumerator CompactBarSeparatesTheTitleFromTheClock()
        {
            CreateWorkout(RoutineSeeds.Lista);
            yield return StartFree();
            yield return AdvanceTo(3.2);
            var bar = Hud.Free!;
            var s = (float)Immersive.Layout!.Scale;
            Assert.That(bar.IsCompact, Is.True);
            Assert.That(bar.TitleScale, Is.EqualTo(0.805f).Within(0.004f), "la misma escala que en la 4b: lo demás ocupa lo mismo");
            Assert.That(bar.TitleTruncated, Is.False);
            var gap = Bounds("hud-wall-clock").xMin - Bounds("hud-free-title").xMax;
            // La caja del título que da el layout es ≈ 0,5 base más ancha que lo que mide MeasureTextSize (el redondeo del texto): la
            // caja del reloj queda a 10·s − 0,5 dp; a la vista (de tinta a tinta) son ≈ 12,6 dp (00-deuda-diseno de la 5 §1.4).
            Assert.That(gap, Is.InRange((float)RowMate.Core.Design.InfoRowFit.FreeCompactTitleClockGap * s - 1f, (float)RowMate.Core.Design.InfoRowFit.FreeCompactTitleClockGap * s + 0.5f), "10·s del título al reloj");
            var space = H.Find("hud-free-title-text") as UnityEngine.UIElements.Label;
            var words = space!.MeasureTextSize("Entrenamiento Libre", 0, UnityEngine.UIElements.VisualElement.MeasureMode.Undefined, 0,
                            UnityEngine.UIElements.VisualElement.MeasureMode.Undefined).x
                        - space.MeasureTextSize("EntrenamientoLibre", 0, UnityEngine.UIElements.VisualElement.MeasureMode.Undefined, 0,
                            UnityEngine.UIElements.VisualElement.MeasureMode.Undefined).x;
            Assert.That(gap, Is.GreaterThan(words * bar.TitleScale * s), "más que el espacio entre las palabras del título");
            Assert.That(bar.Clock.IconGapDp, Is.EqualTo((float)RowMate.Core.Design.InfoRowFit.FreeCompactClockIconGap), "el ícono a 3 de sus cifras");
            Assert.That(bar.Chip.resolvedStyle.marginLeft, Is.EqualTo((float)RowMate.Core.Design.InfoRowFit.FreeCompactChipGap).Within(0.01f), "el chip a 8 del reloj");
            Assert.That(Bounds("hud-phase-chip").xMax, Is.LessThanOrEqualTo(Bounds("hud-topbar").xMax + 0.5f), "nada sale de la barra");
        }

        // Fuera de TV y P360 en español (en inglés, o en horizontal), la barra de siempre (DD5-01: nada cambia).
        [UnityTest, Category("AC-WKT-16-07"), Category("QA-V4b-R2-01")]
        public IEnumerator EnglishAndLandscapeKeepTheNormalBar()
        {
            foreach (var landscape in new[] { false, true })
            {
                CreateWorkout(RoutineSeeds.Lista, o =>
                {
                    if (landscape)
                    {
                        o.Profile = DeviceProfile.TH;
                    }
                    else
                    {
                        o.LanguageTags = new[] { "en-US" };
                    }
                });
                yield return StartFree();
                yield return AdvanceTo(3.2);
                var bar = Hud.Free!;
                Assert.That(bar.IsCompact, Is.False, landscape ? "en horizontal" : "en inglés");
                Assert.That(bar.TitleScale, Is.EqualTo(1f).Within(0.001f));
                Assert.That(bar.Chip.FixedWidth, Is.EqualTo(bar.Chip.NormalWidth).Within(0.01f));
                Assert.That(bar.Clock.IconGapDp, Is.EqualTo((float)RowMate.Core.Design.HudGeometry.WallClockIconGap));
                DisposeHost();
            }
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
