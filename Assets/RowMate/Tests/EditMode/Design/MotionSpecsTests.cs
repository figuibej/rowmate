using NUnit.Framework;
using RowMate.Core.Design;

namespace RowMate.Tests.EditMode.Design
{
    // Las constantes de motion de la It. 3 (03-rutinas/04-design-notes §5.11, DN3-16) y los tiempos de comportamiento de
    // Flutter (UiTimings). Los dueños de las animaciones son de WP-B; acá se fijan los números.
    [TestFixture]
    public sealed class MotionSpecsTests
    {
        [Test, Category("DN3-16")]
        public void PageDialogMenuAndToast()
        {
            Assert.That(MotionSpecs.PageInMs, Is.EqualTo(240));
            Assert.That(MotionSpecs.PageInFadeMs, Is.EqualTo(160));
            Assert.That(MotionSpecs.PageOutMs, Is.EqualTo(200));
            Assert.That(MotionSpecs.PageOutFadeStartMs, Is.EqualTo(40));
            Assert.That(MotionSpecs.PageShiftDp, Is.EqualTo(24), "shift.xl");
            Assert.That(MotionSpecs.PageShiftDp, Is.EqualTo(Tokens.ShiftXl.Value));
            Assert.That(MotionSpecs.PageDimOpacity, Is.EqualTo(0.08));
            Assert.That(MotionSpecs.ReducedPageFadeMs, Is.EqualTo(120));
            Assert.That(MotionSpecs.DialogScrimOpacity, Is.EqualTo(0.6));
            Assert.That(MotionSpecs.DialogScrimInMs, Is.EqualTo(200));
            Assert.That(MotionSpecs.DialogInMs, Is.EqualTo(220));
            Assert.That(MotionSpecs.DialogInScaleFrom, Is.EqualTo(0.96));
            Assert.That(MotionSpecs.DialogOutMs, Is.EqualTo(150));
            Assert.That(MotionSpecs.DialogOutScaleTo, Is.EqualTo(0.98));
            Assert.That(MotionSpecs.MenuInMs, Is.EqualTo(120));
            Assert.That(MotionSpecs.MenuInScaleFrom, Is.EqualTo(0.96));
            Assert.That(MotionSpecs.MenuOutMs, Is.EqualTo(90));
            Assert.That(MotionSpecs.TooltipInMs, Is.EqualTo(120));
            Assert.That(MotionSpecs.TooltipOutMs, Is.EqualTo(90));
            Assert.That(MotionSpecs.ToastInMs, Is.EqualTo(220));
            Assert.That(MotionSpecs.ToastOutMs, Is.EqualTo(160));
            Assert.That(MotionSpecs.ToastOutShiftDp, Is.EqualTo(8));
            Assert.That(MotionSpecs.ToastBumpScale, Is.EqualTo(1.02));
            Assert.That(MotionSpecs.ToastBumpPeakMs, Is.EqualTo(90));
            Assert.That(MotionSpecs.ToastBumpEndMs, Is.EqualTo(240));
            Assert.That(MotionSpecs.ToastTextOutMs, Is.EqualTo(80));
            Assert.That(MotionSpecs.ToastTextInEndMs, Is.EqualTo(200));
            Assert.That(MotionSpecs.BottomObstacleGapDp, Is.EqualTo(8));
        }

        [Test, Category("DN3-16")]
        public void ListEditorAndScrolls()
        {
            Assert.That(MotionSpecs.ListStaggerMs, Is.EqualTo(35));
            Assert.That(MotionSpecs.ListStaggerMax, Is.EqualTo(8));
            Assert.That(MotionSpecs.ListItemInMs, Is.EqualTo(240));
            Assert.That(MotionSpecs.EmptyInShiftDp, Is.EqualTo(Tokens.ShiftS.Value), "shift.s");
            Assert.That(MotionSpecs.LoadingGraceMs, Is.EqualTo(120), "DN3-15");
            Assert.That(MotionSpecs.GrowOmega, Is.EqualTo(CriticalSpring.SmoothOmega));
            Assert.That(MotionSpecs.GrowFadeStartMs, Is.EqualTo(60));
            Assert.That(MotionSpecs.GrowFadeEndMs, Is.EqualTo(260));
            Assert.That(MotionSpecs.CollapseFadeMs, Is.EqualTo(120));
            Assert.That(MotionSpecs.EmptyOutMs, Is.EqualTo(120), "el vacío del editor sale en 120 (§5.6, QA-V3-02)");
            Assert.That(MotionSpecs.CollapseStartMs, Is.EqualTo(80));
            Assert.That(MotionSpecs.CollapseEndMs, Is.EqualTo(280));
            Assert.That(MotionSpecs.SeriesStaggerMs, Is.EqualTo(40));
            Assert.That(MotionSpecs.HighlightPeak, Is.EqualTo(0.08));
            Assert.That(MotionSpecs.HighlightMs, Is.EqualTo(600));
            Assert.That(MotionSpecs.StackLayerMs, Is.EqualTo(160));
            Assert.That(MotionSpecs.FieldLabelScale, Is.EqualTo(0.8));
            Assert.That(MotionSpecs.ModeSwapInMs, Is.EqualTo(120));
            Assert.That(MotionSpecs.KeyOmega, Is.EqualTo(22));
            Assert.That(MotionSpecs.KeyboardOmega, Is.EqualTo(22));
            Assert.That(MotionSpecs.ScrollToNewOmega, Is.EqualTo(12));
            Assert.That(MotionSpecs.ScrollToFocusOmega, Is.EqualTo(22));
            Assert.That(MotionSpecs.ScrollMarginDp, Is.EqualTo(16));
            Assert.That(MotionSpecs.SaveSpinnerDelayMs, Is.EqualTo(300));
        }

        // It. 4a (04-design-notes §5.13; DN4a-06, DN4a-17): las constantes del HUD funcional.
        [Test, Category("DN4a-17")]
        public void HudOfIterationFourA()
        {
            Assert.That(MotionSpecs.HudColorMs, Is.EqualTo(150));
            Assert.That(MotionSpecs.OffTargetArrowMs, Is.EqualTo(120));
            Assert.That(MotionSpecs.OffTargetArrowShiftDp, Is.EqualTo(4));
            Assert.That(MotionSpecs.OffTargetBumpScale, Is.EqualTo(1.04));
            Assert.That(MotionSpecs.OffTargetBumpPeakMs, Is.EqualTo(90));
            Assert.That(MotionSpecs.OffTargetBumpEndMs, Is.EqualTo(240));
            Assert.That(MotionSpecs.OffTargetBumpCooldownMs, Is.EqualTo(4000));
            Assert.That(MotionSpecs.StepPulseHalfMs, Is.EqualTo(600), "600 + 600, como el legacy (immersive_workout_screen.dart:319-322)");
            Assert.That(MotionSpecs.UpNextMinOpacity, Is.EqualTo(0.6));
            Assert.That(MotionSpecs.UpNextFadeInMs, Is.EqualTo(200));
            Assert.That(MotionSpecs.UpNextWidthOmega, Is.EqualTo(CriticalSpring.SmoothOmega));
            Assert.That(MotionSpecs.EndingSoonScale, Is.EqualTo(1.1));
            Assert.That(MotionSpecs.EndingSoonOmega, Is.EqualTo(CriticalSpring.SnappyOmega));
            Assert.That(MotionSpecs.StepSwapOutMs, Is.EqualTo(120));
            Assert.That(MotionSpecs.StepSwapInStartMs, Is.EqualTo(60));
            Assert.That(MotionSpecs.StepSwapInEndMs, Is.EqualTo(260));
            Assert.That(MotionSpecs.TargetDecorMs, Is.EqualTo(240));
            Assert.That(MotionSpecs.TargetTextInStartMs, Is.EqualTo(60));
            Assert.That(MotionSpecs.TargetTextInEndMs, Is.EqualTo(240));
            Assert.That(MotionSpecs.BpmMoveMs, Is.EqualTo(240));
            Assert.That(MotionSpecs.BpmFadeInStartMs, Is.EqualTo(120));
            Assert.That(MotionSpecs.BpmFadeInEndMs, Is.EqualTo(360));
            Assert.That(MotionSpecs.BpmFadeOutMs, Is.EqualTo(120));
            Assert.That(MotionSpecs.BpmMoveOutStartMs, Is.EqualTo(120));
            Assert.That(MotionSpecs.BpmMoveOutEndMs, Is.EqualTo(360));
            Assert.That(MotionSpecs.ImmersiveInMs, Is.EqualTo(240));
            Assert.That(MotionSpecs.ImmersiveOutMs, Is.EqualTo(200));
            Assert.That(MotionSpecs.ReducedImmersiveFadeMs, Is.EqualTo(120));
            Assert.That(MotionSpecs.FinishSpinnerDelayMs, Is.EqualTo(MotionSpecs.SaveSpinnerDelayMs));
            Assert.That(MotionSpecs.FinishSpinnerDelayMs, Is.EqualTo(300));
            // Se reusan sin cambios (§5.13): el rodillo de 180 (también el reloj y la cuenta), el fundido de 80 y los resortes.
            Assert.That(DigitRoll.DurationMs, Is.EqualTo(180));
            Assert.That(MotionSpecs.ReducedFadeMs, Is.EqualTo(80));
            Assert.That(MotionSpecs.FitScaleOmega, Is.EqualTo(CriticalSpring.SmoothOmega));
            // Las de geometría, en HudGeometry (§5.13).
            Assert.That(HudGeometry.TopBarHeight, Is.EqualTo(40));
            Assert.That(HudGeometry.TopBarRowHeight, Is.EqualTo(22));
            Assert.That(HudGeometry.TopBarPadV, Is.EqualTo(8));
            Assert.That(HudGeometry.StepRowPadH, Is.EqualTo(12));
            Assert.That(HudGeometry.FreeRowPadH, Is.EqualTo(14));
            Assert.That(HudGeometry.BandPadHPortrait, Is.EqualTo(20));
            Assert.That(HudGeometry.BandPadHLandscape, Is.EqualTo(14));
            Assert.That(HudGeometry.BandDividerGapPortrait, Is.EqualTo(16));
            Assert.That(HudGeometry.BandDividerGapLandscape, Is.EqualTo(10));
            Assert.That(HudGeometry.ArrowEm, Is.EqualTo(0.4));
            Assert.That(HudGeometry.ArrowGutterEm, Is.EqualTo(0.48));
            Assert.That(HudGeometry.ArrowUpBaselineEm, Is.EqualTo(0.196));
            Assert.That(HudGeometry.ArrowDownBaselineEm, Is.EqualTo(0.214));
            Assert.That(HudGeometry.UnitAlpha, Is.EqualTo(0.8));
            Assert.That(HudGeometry.HorizonPortrait, Is.EqualTo(0.40));
            Assert.That(HudGeometry.HorizonLandscape, Is.EqualTo(0.42));
            Assert.That(HudGeometry.CameraHeightM, Is.EqualTo(2.2));
            Assert.That(HudGeometry.RowerDistanceM, Is.EqualTo(9.5));
            Assert.That(HudGeometry.WrenchGap, Is.EqualTo(8));
        }

        // It. 4b (04b-hud-exr/02-tech-design §3.1; 04-design-notes §4.15): los números de la spec, que el Designer confirmó
        // (DN4b-01 a -03), con lo que suman las notas. Las líneas de tiempo que los usan se prueban en HudTimelinesTests.
        [Test, Category("AC-WKT-13-06"), Category("AC-WKT-13-08")]
        public void HudOfIterationFourB()
        {
            // La entrada "sumergirse" (§3.2): negro en 220, el fondo en 220-400, la cascada de 280 con +8 (la spec pide ≤ 12).
            Assert.That(MotionSpecs.ImmersiveBlackInMs, Is.EqualTo(220));
            Assert.That(MotionSpecs.ImmersiveBackdropInStartMs, Is.EqualTo(220));
            Assert.That(MotionSpecs.ImmersiveBackdropInEndMs, Is.EqualTo(400));
            Assert.That(new[] { MotionSpecs.ImmersiveCascadeTopBarMs, MotionSpecs.ImmersiveCascadeSpmMs, MotionSpecs.ImmersiveCascadeSplitWattsMs,
                MotionSpecs.ImmersiveCascadeBandMs, MotionSpecs.ImmersiveCascadeVitalsMs, MotionSpecs.ImmersiveCascadeControlsMs },
                Is.EqualTo(new double[] { 400, 520, 600, 680, 760, 840 }));
            Assert.That(MotionSpecs.ImmersivePieceInMs, Is.EqualTo(280));
            Assert.That(MotionSpecs.ImmersivePieceShiftDp, Is.EqualTo(8));
            Assert.That(MotionSpecs.ImmersivePieceShiftDp, Is.LessThanOrEqualTo(12));
            Assert.That(MotionSpecs.ImmersiveEntryEndMs, Is.EqualTo(MotionSpecs.ImmersiveCascadeControlsMs + MotionSpecs.ImmersivePieceInMs));
            Assert.That(MotionSpecs.ImmersiveEntryEndMs, Is.EqualTo(1120));
            Assert.That(MotionSpecs.ImmersiveWrenchInMs, Is.EqualTo(160));
            Assert.That(MotionSpecs.ReducedImmersiveFadeMs, Is.EqualTo(120), "la de la 4a, lineal");

            // La salida con la última foto (§3.3): el HUD 0-200, el negro 200-400, Workout 400-640 (total < 1 s); los avisos a los 400.
            Assert.That(MotionSpecs.ImmersiveHudOutMs, Is.EqualTo(200));
            Assert.That(MotionSpecs.ImmersiveBlackOutStartMs, Is.EqualTo(200));
            Assert.That(MotionSpecs.ImmersiveBlackOutEndMs, Is.EqualTo(400));
            Assert.That(MotionSpecs.ImmersiveRevealStartMs, Is.EqualTo(400));
            Assert.That(MotionSpecs.ImmersiveRevealMs, Is.EqualTo(240));
            Assert.That(MotionSpecs.ImmersiveRevealStartMs + MotionSpecs.ImmersiveRevealMs, Is.EqualTo(640));
            Assert.That(MotionSpecs.EndToastStartMs, Is.EqualTo(MotionSpecs.ImmersiveRevealStartMs), "DN4b-24");

            // Las píldoras (§3.4): el relleno que faltaba, la sólida con su destello, la hueca con su trazo, la pista de la nueva.
            Assert.That(MotionSpecs.PillFillCompleteMs, Is.EqualTo(120));
            Assert.That(MotionSpecs.PillSolidifyStartMs, Is.EqualTo(120));
            Assert.That(MotionSpecs.PillSolidifyEndMs, Is.EqualTo(340));
            Assert.That(MotionSpecs.PillFlashPeak, Is.EqualTo(0.40));
            Assert.That(MotionSpecs.PillMissedStartMs, Is.EqualTo(120));
            Assert.That(MotionSpecs.PillMissedEndMs, Is.EqualTo(440));
            Assert.That(MotionSpecs.PillTrackInMs, Is.EqualTo(150));
            Assert.That(MotionSpecs.ReducedPillBorderFadeMs, Is.EqualTo(160));
            Assert.That(MotionSpecs.PillRepaintPx, Is.EqualTo(0.25));

            // El halo (DN4b-17), con la referencia del BoxShadow del legacy (step_pill.dart:88-94), el brillo (DN4b-19) y el
            // pulgar que pasa (DN4b-18).
            Assert.That(MotionSpecs.PillGlowMin, Is.EqualTo(0.30));
            Assert.That(MotionSpecs.PillGlowMax, Is.EqualTo(0.70));
            Assert.That(MotionSpecs.PillGlowRampMs, Is.EqualTo(150));
            Assert.That(MotionSpecs.PillGlowEdgeFactor, Is.EqualTo(0.65));
            Assert.That(new[] { MotionSpecs.PillGlowExtentSideDp, MotionSpecs.PillGlowExtentTopDp, MotionSpecs.PillGlowExtentBottomDp },
                Is.EqualTo(new double[] { 10, 8, 4 }));
            Assert.That(new[] { MotionSpecs.PillGlowBlurDp, MotionSpecs.PillGlowSpreadDp, MotionSpecs.PillGlowRadiusDp },
                Is.EqualTo(new double[] { 8, 2, 5 }));
            Assert.That(MotionSpecs.PillHighlightAlpha, Is.EqualTo(0.25));
            Assert.That(MotionSpecs.PillHighlightDp, Is.EqualTo(1));
            Assert.That(MotionSpecs.PillThumbSwapMs, Is.EqualTo(150));

            // La fila de información (DN4b-16).
            Assert.That(MotionSpecs.InfoRowModeChangeMs, Is.EqualTo(240));
            Assert.That(MotionSpecs.TargetPillOutMs, Is.EqualTo(120));

            // La cuenta grande y PAUSA, un disco por vez (§3.5; DN4b-14, -20).
            Assert.That(MotionSpecs.BigCountInMs, Is.EqualTo(200));
            Assert.That(MotionSpecs.BigCountInScaleFrom, Is.EqualTo(0.85));
            Assert.That(MotionSpecs.BigCountOutMs, Is.EqualTo(200));
            Assert.That(MotionSpecs.BigCountOutScaleTo, Is.EqualTo(1.10));
            Assert.That(MotionSpecs.ReducedBigCountInMs, Is.EqualTo(160));
            Assert.That(MotionSpecs.ReducedBigCountOutMs, Is.EqualTo(120));
            Assert.That(MotionSpecs.CalloutSwapOutMs, Is.EqualTo(120));
            Assert.That(MotionSpecs.CalloutSwapInStartMs, Is.EqualTo(120));
            Assert.That(MotionSpecs.BigCountRingClockwise, Is.True);

            // El sello (§3.6): todo dentro de los 2 s.
            Assert.That(MotionSpecs.SealDurationMs, Is.EqualTo(2000));
            Assert.That(MotionSpecs.SealGrowMs, Is.EqualTo(240));
            Assert.That(MotionSpecs.SealContentInStartMs, Is.EqualTo(40));
            Assert.That(MotionSpecs.SealContentInEndMs, Is.EqualTo(240));
            Assert.That(MotionSpecs.SealIconScaleFrom, Is.EqualTo(0.6));
            Assert.That(MotionSpecs.SealIconStartMs, Is.EqualTo(60));
            Assert.That(MotionSpecs.SealIconEndMs, Is.EqualTo(300));
            Assert.That(MotionSpecs.SealContentOutStartMs, Is.EqualTo(1800));
            Assert.That(MotionSpecs.SealContentOutEndMs, Is.EqualTo(1920));
            Assert.That(MotionSpecs.SealCollapseStartMs, Is.EqualTo(1800));
            Assert.That(MotionSpecs.SealCollapseEndMs, Is.EqualTo(2000));
            Assert.That(MotionSpecs.SealCollapseEndMs, Is.LessThanOrEqualTo(MotionSpecs.SealDurationMs), "dentro de los 2 s");
            Assert.That(MotionSpecs.SealSwapOutMs, Is.EqualTo(80));
            Assert.That(MotionSpecs.SealSwapInEndMs, Is.EqualTo(200));
            Assert.That(MotionSpecs.SealSwapIconEndMs, Is.EqualTo(320));
            Assert.That(MotionSpecs.ReducedSealContentInMs, Is.EqualTo(160));
            Assert.That(MotionSpecs.ReducedSealContentOutMs, Is.EqualTo(120));

            // El diálogo de completado (§3.9): a los 300 del fin; el conteo termina a los 1160 del diálogo.
            Assert.That(MotionSpecs.CompletedDialogDelayMs, Is.EqualTo(300));
            Assert.That(MotionSpecs.CompletedCheckScaleFrom, Is.EqualTo(0.6));
            Assert.That(MotionSpecs.CompletedCheckStartMs, Is.EqualTo(60));
            Assert.That(MotionSpecs.CompletedCheckEndMs, Is.EqualTo(300));
            Assert.That(MotionSpecs.CompletedCheckStrokeStartMs, Is.EqualTo(180));
            Assert.That(MotionSpecs.CompletedCheckStrokeEndMs, Is.EqualTo(500));
            Assert.That(MotionSpecs.TotalsCountMs, Is.EqualTo(700));
            Assert.That(MotionSpecs.TotalsStaggerMs, Is.EqualTo(120));
            Assert.That(MotionSpecs.TotalsStartMs, Is.EqualTo(220));
            Assert.That(MotionSpecs.TotalsStartMs + 2 * MotionSpecs.TotalsStaggerMs + MotionSpecs.TotalsCountMs, Is.EqualTo(1160));

            // La pausa (§3.7): el velo y PAUSA.
            Assert.That(MotionSpecs.PauseVeilAlpha, Is.EqualTo(0.15));
            Assert.That(MotionSpecs.PauseVeilInMs, Is.EqualTo(300));
            Assert.That(MotionSpecs.PauseVeilOutMs, Is.EqualTo(240));
            Assert.That(MotionSpecs.PausedLabelInMs, Is.EqualTo(240));
            Assert.That(MotionSpecs.PausedLabelScaleFrom, Is.EqualTo(0.96));
            Assert.That(MotionSpecs.PausedLabelOutMs, Is.EqualTo(240));
            Assert.That(MotionSpecs.PausedLabelOutScaleTo, Is.EqualTo(0.96));

            // El chip y el punto en vivo (§3.7; DN4b-26).
            Assert.That(MotionSpecs.PhaseChipColorMs, Is.EqualTo(MotionSpecs.HudColorMs));
            Assert.That(MotionSpecs.LiveDotHalfMs, Is.EqualTo(800));
            Assert.That(MotionSpecs.LiveDotMinOpacity, Is.EqualTo(0.35));

            // Se reusan sin cambios (§3.1).
            Assert.That(DigitRoll.DurationMs, Is.EqualTo(180));
            Assert.That(MotionSpecs.ReducedFadeMs, Is.EqualTo(80));

            // Las de geometría de la 4b, en HudGeometry (§3.1, el último párrafo).
            Assert.That(HudGeometry.TopBarHeightSteps, Is.EqualTo(52));
            Assert.That(HudGeometry.TopBarHeightSeal, Is.EqualTo(74));
            Assert.That(HudGeometry.TopBarHeightFree, Is.EqualTo(40));
            Assert.That(new[] { HudGeometry.SealRowGap, HudGeometry.SealRowHeight, HudGeometry.SealRowPadH, HudGeometry.InfoRowPadBottom },
                Is.EqualTo(new double[] { 5, 17, 12, 8 }));
            Assert.That(new[] { HudGeometry.WallClockGap, HudGeometry.WallClockIcon, HudGeometry.WallClockIconGap, HudGeometry.WallClockIconAlpha },
                Is.EqualTo(new[] { 10, 16, 5, 0.70 }));
            Assert.That(new[] { HudGeometry.PhaseChipGap, HudGeometry.PhaseChipPadH, HudGeometry.PhaseChipPadV, HudGeometry.PhaseChipRadius,
                HudGeometry.PhaseChipText }, Is.EqualTo(new double[] { 10, 8, 3, 8, 11 }));
            Assert.That(new[] { HudGeometry.PhaseChipTrackingEm, HudGeometry.PhaseChipTintAlpha, HudGeometry.PhaseChipBorderAlpha, HudGeometry.PhaseChipDotSlot },
                Is.EqualTo(new[] { 0.08, 0.15, 0.50, 10 }));
            Assert.That(HudGeometry.PhaseChipDotSlot, Is.EqualTo(HudGeometry.LiveDotSize + HudGeometry.LiveDotGap), "6 + 4, solo en EN MARCHA");
            Assert.That(new[] { HudGeometry.BigCountDiameter, HudGeometry.BigCountDigit, HudGeometry.BigCountRingStroke, HudGeometry.BigCountRingTrackAlpha,
                HudGeometry.BigCountGap }, Is.EqualTo(new[] { 72, 64, 3, 0.20, 8 }));
            Assert.That(HudGeometry.BigCountRingRadius, Is.EqualTo(34.5), "la línea central del aro");
            Assert.That(HudGeometry.BigCountDigit, Is.EqualTo(Tokens.NumHero.SizeDp), "num.hero, la cifra de SPM (DT4b-04)");
            Assert.That(new[] { HudGeometry.PausedGlyphDp, HudGeometry.PausedGlyphGap, HudGeometry.PausedLabelSize, HudGeometry.PausedLabelTrackingEm },
                Is.EqualTo(new[] { 28, 4, 12, 0.08 }));
            Assert.That(new[] { HudGeometry.PanelCeilingGap, HudGeometry.CompletionCheckDp, HudGeometry.CompletionUnitGapDp },
                Is.EqualTo(new double[] { 8, 32, 3 }));
        }

        // snack_bar.dart:29 (4 s), tooltip.dart:392-394 (1,5 s, 0,1 s, 0) y kLongPressTimeout (0,5 s).
        [Test, Category("AC-APP-03-05")]
        public void BehaviourTimingsOfFlutter()
        {
            Assert.That(UiTimings.ToastVisibleMs, Is.EqualTo(4000));
            Assert.That(UiTimings.TooltipWaitMs, Is.EqualTo(0));
            Assert.That(UiTimings.TooltipExitMs, Is.EqualTo(100));
            Assert.That(UiTimings.TooltipShowAfterLongPressMs, Is.EqualTo(1500));
            Assert.That(UiTimings.LongPressMs, Is.EqualTo(500));
        }
    }
}
