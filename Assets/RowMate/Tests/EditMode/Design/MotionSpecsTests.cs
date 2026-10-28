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
