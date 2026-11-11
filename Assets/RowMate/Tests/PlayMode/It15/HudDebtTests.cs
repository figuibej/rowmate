using System.Collections;
using NUnit.Framework;
using RowMate.Core.Accessibility;
using RowMate.Core.Design;
using RowMate.Core.Text;
using RowMate.Presentation.Accessibility;
using RowMate.Presentation.Workout;
using RowMate.Tests.Support;
using RowMate.Tests.UiSupport;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace RowMate.Tests.PlayMode.It15
{
    // It. 15 (15-deuda-pc/02-tech-design §2.3): el HUD (tracking, cascada) y el lector en el inmersivo.

    // AC-D15-24 (#76, DT15-10): 0,04 em en los rótulos con minúsculas; 0,08 en los de mayúsculas. Sin elementos nuevos.
    public sealed class HudLabelTrackingTests : WorkoutUiTestBase
    {
        [UnityTest, Category("AC-D15-24")]
        public IEnumerator MixedLabelsAtPointZeroFour()
        {
            CreateWorkout(configure: o => o.Profile = DeviceProfile.L640);
            yield return StartFree();
            yield return Settle();
            var hud = Hud;
            Assert.That(hud.Watts.Label.text, Is.EqualTo("Vatios"));
            Assert.That(hud.Split.Label.text, Is.EqualTo("Split 500m"));
            foreach (var card in new[] { hud.Watts, hud.Split, hud.Kcal })
            {
                Assert.That(LabelTracking.HasLowercase(card.Label.text), Is.True, card.Label.text);
                Assert.That(card.Label.ClassListContains(LabelTracking.MixedClass), Is.True, card.Label.text + ": la clase");
            }

            foreach (var card in new[] { hud.Spm, hud.Bpm })
            {
                Assert.That(card.Label.ClassListContains(LabelTracking.MixedClass), Is.False, card.Label.text);
            }

            WpdGuards.UssHas(LabelTracking.MixedClass);
            foreach (var card in new[] { hud.Watts, hud.Split, hud.Kcal })
            {
                Assert.That(card.Label.resolvedStyle.letterSpacing, Is.EqualTo(4).Within(0.01f), card.Label.text + ": 0,04 em (USS de WP-D)");
            }

            foreach (var card in new[] { hud.Spm, hud.Bpm })
            {
                Assert.That(card.Label.ClassListContains(LabelTracking.MixedClass), Is.False, card.Label.text);
                Assert.That(card.Label.resolvedStyle.letterSpacing, Is.EqualTo(8).Within(0.01f), card.Label.text + ": 0,08 em");
            }

            Assert.That(hud.Watts.Label.text, Is.EqualTo("Vatios"));
            Assert.That(hud.Split.Label.text, Is.EqualTo("Split 500m"));
        }

        [UnityTest, Category("AC-D15-24")]
        public IEnumerator EnglishAndLanguageSwitch()
        {
            CreateWorkout(configure: o => o.LanguageTags = new[] { "en-US" });
            yield return StartFree();
            yield return Settle();
            Assert.That(Hud.Watts.Label.text, Is.EqualTo("Watts"));
            Assert.That(Hud.Watts.Label.ClassListContains(LabelTracking.MixedClass), Is.True);
            Assert.That(Hud.Spm.Label.ClassListContains(LabelTracking.MixedClass), Is.False);
        }
    }

    // AC-D15-32 (#82, DT15-11): la cascada de 3 grupos al cambiar de clase de ventana.
    public sealed class HudReflowCascadeTests : WorkoutUiTestBase
    {
        [UnityTest, Category("AC-D15-32")]
        public IEnumerator ThreeGroupsWithDelaysAndShift()
        {
            CreateWorkout(configure: o => o.Profile = DeviceProfile.TV);
            yield return StartFree();
            yield return Settle();
            H.Advance(1);
            yield return Settle();
            var reflow = Immersive.Reflow;
            reflow.OnOrientationClassChanged();
            var scale = Immersive.Layout?.Scale ?? 1;
            var top = Hud.PieceOf(HudPiece.TopBar)!;
            Assert.That(reflow.GroupOpacity(0), Is.EqualTo(0));
            Assert.That(top.resolvedStyle.opacity, Is.EqualTo(0).Within(1e-4).Or.GreaterThanOrEqualTo(0f));
            Assert.That(top.style.translate.value.y.value, Is.EqualTo((float)(MotionSpecs.HudReflowShiftDp * scale)).Within(0.01f), "8 dp × la escala");
            H.Advance(0.04);
            yield return Frames();
            Assert.That(reflow.GroupOpacity(0), Is.GreaterThan(0));
            Assert.That(reflow.GroupOpacity(1), Is.EqualTo(0).Within(1e-6), "las tarjetas arrancan a los 40");
            Assert.That(reflow.GroupOpacity(2), Is.EqualTo(0), "la banda y los controles a los 80");
            H.Advance(0.08);
            yield return Frames();
            Assert.That(reflow.GroupOpacity(0), Is.GreaterThan(reflow.GroupOpacity(1)));
            Assert.That(reflow.GroupOpacity(1), Is.GreaterThan(reflow.GroupOpacity(2)));
            Assert.That(reflow.GroupShift(2), Is.GreaterThan(reflow.GroupShift(0)));
            H.Advance(0.17);
            yield return Frames();
            Assert.That(reflow.Opacity, Is.EqualTo(1), "todo a la vista a los 290");
            Assert.That(top.style.translate.keyword, Is.EqualTo(StyleKeyword.Null), "sin traslado al final");
            reflow.OnOrientationClassChanged();
            Assert.That(reflow.Opacity, Is.EqualTo(1), "otro cambio antes de 400 ms: en el acto");
        }

        [UnityTest, Category("AC-D15-32")]
        public IEnumerator ReducedMotionDoesNotAnimate()
        {
            CreateWorkout(configure: o => { o.Profile = DeviceProfile.TV; o.ReduceMotion = true; });
            yield return StartFree();
            yield return Settle();
            Immersive.Reflow.OnOrientationClassChanged();
            Assert.That(Immersive.Reflow.Opacity, Is.EqualTo(1));
            Assert.That(Immersive.Reflow.IsRunning, Is.False);
        }
    }

    // AC-D15-29 (#80): el foco vuelve al que abrió el diálogo.
    public sealed class ScreenReaderActionTests : WorkoutUiTestBase
    {
        [UnityTest, Category("AC-D15-29")]
        public IEnumerator DialogReturnsFocusToOpener()
        {
            CreateWorkout(configure: o => o.ScreenReader = true);
            yield return StartFree();
            yield return Settle();
            var bridge = H.ScreenReader!;
            bridge.RebuildNow();
            var finish = H.Find("hud-finish")!;
            // Un toque fuera de un diálogo (otro elemento) no pisa al que abre después.
            bridge.Invoke(H.Find("hud-pause")!);
            yield return Settle();
            bridge.Invoke(H.Find("hud-pause")!);
            yield return Settle();
            bridge.Invoke(finish);
            yield return Settle();
            Assert.That(H.TopDialog, Is.Not.Null, "Terminar abre el diálogo");
            Assert.That(bridge.DialogOpener, Is.SameAs(finish));
            bridge.RebuildNow();
            bridge.Invoke(H.Find("dialog-cancel")!);
            yield return Settle();
            bridge.RebuildNow();
            Assert.That(H.TopDialog, Is.Null);
            Assert.That(bridge.FocusedElement, Is.SameAs(finish), "el foco vuelve a Terminar");
        }
    }

    // AC-D15-28 (#80): los paneles informan si están expandidos.
    public sealed class SeriesPanelA11yTests : SeriesUiTestBase
    {
        [UnityTest, Category("AC-D15-28")]
        public IEnumerator PanelsReportExpanded()
        {
            CreateSeries(DeviceProfile.W1280, laps: true, reps: false);
            yield return StartAt("Fondo 3000", 30);
            var series = Hud.Series!;
            var texts = H.App.Texts.Value;
            var laps = A11yRules.Describe(series.Laps, A11yRules.KindOf(series.Laps), texts);
            Assert.That(laps, Is.Not.Null, "el panel de parciales se lee");
            Assert.That(laps!.Label, Is.EqualTo(series.Laps.TitleText));
            Assert.That((laps.State & A11yState.Expanded) != 0, Is.True, "abierto: Expanded");
            if (series.Reps.resolvedStyle.display != DisplayStyle.None)
            {
                var reps = A11yRules.Describe(series.Reps, A11yRules.KindOf(series.Reps), texts);
                Assert.That(reps == null || (reps.State & A11yState.Expanded) == 0, Is.True, "cerrado: sin Expanded");
            }
        }
    }
}
