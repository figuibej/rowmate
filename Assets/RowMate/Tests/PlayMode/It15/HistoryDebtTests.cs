using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using RowMate.App.Export;
using RowMate.Core.Design;
using RowMate.Core.History;
using RowMate.Core.Localization;
using RowMate.Presentation.Accessibility;
using RowMate.Presentation.Controls;
using RowMate.Presentation.History;
using RowMate.Presentation.Shell;
using RowMate.Tests.Support;
using RowMate.Tests.UiSupport;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace RowMate.Tests.PlayMode.It15
{
    // It. 15 (15-deuda-pc/02-tech-design §2.1, §6): el Historial y el Detalle. F-9 en la base temporal y el FakeFileExporter del anfitrión
    // (nunca Documentos ni el Explorador).

    // AC-D15-09, -10 (#67, DV-143): exportar desde la tarjeta.
    public sealed class HistoryCardExportTests : ExportUiTestBase
    {
        private IEnumerator OpenList(DeviceProfile? profile = null)
        {
            var p = profile ?? DeviceProfile.W1280;
            CreateHistory(HistorySeeds.F9, p, o => o.IsDesktop = p.Code == DeviceProfile.W1280.Code);
            yield return GoToHistory();
            yield return Frames();
            Screen.List.FinishBatches();
            yield return Settle();
        }

        [UnityTest, Category("AC-D15-09")]
        public IEnumerator ButtonOnlyOnExportableCards()
        {
            yield return OpenList();
            var card = Card(0);
            Assert.That(card.Export, Is.Not.Null);
            Assert.That(card.Export!.IsShown, Is.True, "n.º 1: el botón");
            Assert.That(H.IsShown("history-card-0-export"), Is.True);
            Assert.That(Card(3).Export!.IsShown, Is.False, "n.º 4 (sin puntos): sin botón");
            var box = H.BoundsOf("history-card-0-export");
            Assert.That(box.width, Is.EqualTo(ExportTcxButton.CardTouchDp).Within(0.5f), "área de 40");
            Assert.That(card.Export.GlyphView.resolvedStyle.fontSize, Is.EqualTo(ExportTcxButton.CardGlyphDp).Within(0.5f), "glifo de 18");
            Assert.That(card.Export.GlyphView.Codepoint, Is.EqualTo(Icons.Download), "el glifo de la fuente (Windows: download)");
            WpdGuards.FontHas(card.Export.GlyphView);
            var chevron = card.Q(className: HistoryCardView.UssClass + "__chevron");
            Assert.That(box.xMax, Is.LessThanOrEqualTo(chevron.worldBound.xMin + 0.5f), "a la izquierda del chevron");
            var top = card.Q(className: HistoryCardView.UssClass + "__top");
            Assert.That(top.layout.height, Is.LessThan(ExportTcxButton.CardTouchDp - 1), "el renglón no crece con el área de toque");
        }

        [UnityTest, Category("AC-D15-09")]
        public IEnumerator AccessibleNameAndHelp()
        {
            yield return OpenList();
            var card = Card(0);
            var texts = H.App.Texts.Value;
            var expected = texts.Format(L10nKeys.ExportTcxFor, MessageArg.Text("session", card.NameLabel.text + ", " + card.DateLabel.text));
            Assert.That(card.Export!.AccessibleName, Is.EqualTo(expected));
            Assert.That(card.Export.Button.Help!.Text, Is.EqualTo(texts.Get(L10nKeys.ExportTcx)), "la ayuda");
            Assert.That(expected, Does.StartWith("Exportar TCX: "));
        }

        [UnityTest, Category("AC-D15-09"), Category("AC-D15-10")]
        public IEnumerator TapExportsWithoutOpeningTheDetail()
        {
            yield return OpenList();
            Exporter.Hold();
            H.Tap("history-card-0-export");
            yield return WaitUntil(() => Exporter.Requests.Count > 0);
            yield return Frames();
            Assert.That(H.Shell.Pages.HasPages, Is.False, "no abre el Detalle");
            Assert.That(Card(0).Export!.IsBusy, Is.True, "indicador");
            Assert.That(Card(0).Export.Spinner.IsShown, Is.True);
            H.Tap("tab-history");
            yield return Frames();
            H.Card0ExportTapUnchecked();
            yield return Frames();
            Assert.That(Exporter.Requests.Count, Is.EqualTo(1), "otro toque mientras exporta no hace nada; volver a la pestaña no cancela");
            Exporter.Release();
            yield return FinishBusy();
            Assert.That(H.Shell.Toasts.CurrentAction?.Title, Is.EqualTo(SavedTitle));
            var toast = H.BoundsOf("toast");
            var bar = H.BoundsOf("tab-bar");
            Assert.That(toast.yMax, Is.LessThanOrEqualTo(bar.yMin + 0.5f), "sobre la barra de pestañas");
            Assert.That(H.Shell.Pages.HasPages, Is.False);
        }

        [UnityTest, Category("AC-D15-10")]
        public IEnumerator SameBytesAsTheDetail()
        {
            yield return OpenList();
            H.Tap("history-card-0-export");
            yield return WaitUntil(() => Exporter.Files.Count > 0);
            yield return FinishBusy();
            yield return OpenCard(0);
            yield return ExportNow(1);
            Assert.That(Exporter.Files.Count, Is.EqualTo(2));
            var fromCard = File.ReadAllBytes(Exporter.Files[0].FullPath);
            var fromDetail = File.ReadAllBytes(Exporter.Files[1].FullPath);
            Assert.That(fromCard, Is.EqualTo(fromDetail), "los mismos bytes");
        }

        [UnityTest, Category("AC-D15-09")]
        public IEnumerator TabReachesTheButtonAfterTheCard()
        {
            yield return OpenList();
            Card(0).Focus();
            yield return Frames();
            H.PressTab();
            yield return Frames();
            Assert.That(H.Focused, Is.SameAs(Card(0).Export!.Button), "Tab: de la tarjeta a su botón");
            H.PressEnter();
            yield return WaitUntil(() => Exporter.Requests.Count > 0);
            Assert.That(H.Shell.Pages.HasPages, Is.False, "Enter en el botón exporta y no abre el Detalle");
            yield return FinishBusy();
        }
    }

    // AC-D15-05 (#35): nombres únicos de las columnas.
    public sealed class HistoryCardNamesTests : History.HistoryUiTestBase
    {
        [UnityTest, Category("AC-D15-05")]
        public IEnumerator ColumnNamesAreUnique()
        {
            CreateHistory(HistorySeeds.F9, DeviceProfile.TV);
            yield return GoToHistory();
            Screen.List.FinishBatches();
            yield return Settle();
            var names = new HashSet<string>();
            for (var i = 0; i < Screen.List.Count; i++)
            {
                var stats = Card(i).Stats;
                for (var j = 0; j < stats.Count; j++)
                {
                    var name = stats.FitAt(j).parent.name;
                    Assert.That(name, Is.EqualTo("history-card-" + i + "-stat-" + j));
                    Assert.That(names.Add(name), Is.True, "repetido: " + name);
                }
            }

            Assert.That(names.Count, Is.GreaterThan(10));
        }

        [UnityTest, Category("AC-D15-05")]
        public IEnumerator SummaryColumnsNamed()
        {
            CreateHistory(HistorySeeds.F9, DeviceProfile.TV);
            yield return GoToHistory();
            yield return OpenCard(0);
            var stats = Detail.SummaryStats;
            for (var j = 0; j < stats.Count; j++)
            {
                Assert.That(stats.FitAt(j).parent.name, Is.EqualTo("session-summary-stat-" + j));
            }
        }
    }

    // AC-D15-02 (#33, DV-146): fila de "Rendimiento por paso" ↔ franjas.
    public sealed class StepSelectionTests : History.HistoryUiTestBase
    {
        private IEnumerator OpenEscalera(bool reduce = false)
        {
            CreateHistory(HistorySeeds.F9, DeviceProfile.TV, o => o.ReduceMotion = reduce);
            yield return GoToHistory();
            yield return OpenCard(0);
            Assert.That(Detail.StepRows.Count, Is.GreaterThan(2), "Escalera tiene pasos");
        }

        [UnityTest, Category("AC-D15-02")]
        public IEnumerator TapMarksRowAndBandsReach22In160()
        {
            yield return OpenEscalera();
            var step = Detail.StepRows.StepIndexAt(1);
            H.Tap("session-step-1");
            Assert.That(Detail.SelectedStep, Is.EqualTo(step));
            Assert.That(Detail.StepRows.RowAt(1).ClassListContains(StepBreakdownView.SelectedClass), Is.True);
            var border = Detail.StepRows.RowAt(1).resolvedStyle;
            yield return Frames();
            var color = TokenColors.ToColor(StepBandPalette.For(Detail.Store.State.Value.Steps[1].TypeName));
            Assert.That(Detail.StepRows.RowAt(1).resolvedStyle.borderTopWidth, Is.EqualTo(1).Within(0.2f), "borde de 1 dp (con el ajuste al píxel)");
            Assert.That(Detail.StepRows.RowAt(1).resolvedStyle.borderTopColor, Is.EqualTo(color), "en el color de su tipo");
            Assert.That(Detail.Chart.StepHighlight.IsRunning, Is.True, "anima");
            H.Advance(0.08);
            yield return Frames();
            var mid = ChartGeometry.BandAlphaFor(Detail.Chart.DrawnVisual.StepEmphasis(step));
            Assert.That(mid, Is.GreaterThan(ChartSpecs.BandAlpha).And.LessThan(ChartSpecs.BandSelectedAlpha), "a mitad de camino");
            H.Advance(0.1);
            yield return Frames();
            Assert.That(ChartGeometry.BandAlphaFor(Detail.Chart.DrawnVisual.StepEmphasis(step)), Is.EqualTo(ChartSpecs.BandSelectedAlpha).Within(1e-9), "22 %");
            Assert.That(Detail.Chart.DrawnVisual.StepEmphasis(Detail.StepRows.StepIndexAt(0)), Is.EqualTo(0), "las otras al 10 %");
            _ = border;
        }

        [UnityTest, Category("AC-D15-02")]
        public IEnumerator SameRowUnmarksOtherReplaces()
        {
            yield return OpenEscalera();
            H.Tap("session-step-1");
            yield return Settle();
            H.Tap("session-step-2");
            yield return Settle();
            Assert.That(Detail.SelectedStep, Is.EqualTo(Detail.StepRows.StepIndexAt(2)), "otra la reemplaza");
            Assert.That(Detail.StepRows.RowAt(1).ClassListContains(StepBreakdownView.SelectedClass), Is.False, "una sola marcada");
            H.Tap("session-step-2");
            yield return Settle();
            Assert.That(Detail.SelectedStep, Is.EqualTo(-1), "la misma desmarca");
            Assert.That(Detail.Chart.DrawnVisual.StepEmphasis(Detail.StepRows.StepIndexAt(2)), Is.EqualTo(0));
        }

        [UnityTest, Category("AC-D15-02")]
        public IEnumerator KeyboardEscapeAndMetricChange()
        {
            yield return OpenEscalera();
            Detail.StepRows.RowAt(0).Focus();
            yield return Frames();
            Assert.That(H.Focused, Is.SameAs(Detail.StepRows.RowAt(0)), "la fila se enfoca");
            H.PressEnter();
            yield return Settle();
            Assert.That(Detail.SelectedStep, Is.EqualTo(Detail.StepRows.StepIndexAt(0)), "Enter marca");
            H.PressKey(KeyCode.Space, ' ');
            yield return Settle();
            Assert.That(Detail.SelectedStep, Is.EqualTo(-1), "Espacio desmarca la misma");
            H.Tap("session-step-1");
            yield return Settle();
            Detail.Store.SelectMetric(ChartMetric.Split);
            yield return Settle();
            Assert.That(Detail.SelectedStep, Is.EqualTo(Detail.StepRows.StepIndexAt(1)), "sigue al cambiar de métrica");
            Assert.That(ChartGeometry.BandAlphaFor(Detail.Chart.DrawnVisual.StepEmphasis(Detail.SelectedStep)), Is.EqualTo(ChartSpecs.BandSelectedAlpha).Within(1e-9));
            H.PressBack(BackSource.Escape);
            yield return Settle();
            Assert.That(DetailOrNull, Is.Not.Null, "Esc con una fila marcada solo desmarca");
            Assert.That(Detail.SelectedStep, Is.EqualTo(-1));
            H.PressBack(BackSource.Escape);
            yield return Settle();
            Assert.That(DetailOrNull, Is.Null, "sin fila marcada, Esc cierra");
            yield return OpenCard(0);
            Assert.That(Detail.SelectedStep, Is.EqualTo(-1), "se pierde al salir");
        }

        [UnityTest, Category("AC-D15-02")]
        public IEnumerator ReducedMotionIsInstant()
        {
            yield return OpenEscalera(true);
            H.Tap("session-step-1");
            Assert.That(Detail.Chart.StepHighlight.IsRunning, Is.False);
            Assert.That(ChartGeometry.BandAlphaFor(Detail.Chart.DrawnVisual.StepEmphasis(Detail.StepRows.StepIndexAt(1))), Is.EqualTo(ChartSpecs.BandSelectedAlpha).Within(1e-9));
            yield return Frames();
        }

        [UnityTest, Category("AC-D15-02")]
        public IEnumerator ScreenReaderSaysSelected()
        {
            var row = (VisualElement?)null;
            yield return OpenEscalera();
            row = Detail.StepRows.RowAt(1);
            Assert.That(A11yRules.RoleOf(A11yRules.KindOf(row)), Is.EqualTo(RowMate.Core.Accessibility.A11yRole.Button));
            Assert.That((A11yRules.Describe(row, A11yRules.KindOf(row), H.App.Texts.Value)!.State & RowMate.Core.Accessibility.A11yState.Selected) != 0, Is.False);
            H.Tap("session-step-1");
            yield return Settle();
            Assert.That((A11yRules.Describe(row, A11yRules.KindOf(row), H.App.Texts.Value)!.State & RowMate.Core.Accessibility.A11yState.Selected) != 0, Is.True);
        }

        [UnityTest, Category("AC-D15-02")]
        public IEnumerator FreeSessionHasNoRows()
        {
            CreateHistory(HistorySeeds.F9, DeviceProfile.TV);
            yield return GoToHistory();
            for (var i = 0; i < Screen.List.Count; i++)
            {
                if (Card(i).Entry!.Session.RoutineName == null && Card(i).Export != null && Card(i).Export!.IsShown)
                {
                    yield return OpenCard(i);
                    Assert.That(Detail.StepRows.Count, Is.EqualTo(0));
                    Assert.That(Detail.TryHandleEscape(), Is.False, "nada que desmarcar");
                    yield break;
                }
            }

            Assert.Inconclusive("F-9 sin una libre con datos");
        }
    }

    // AC-D15-06 (#37): la sombra del tooltip.
    public sealed class TooltipShadowTests : History.HistoryUiTestBase
    {
        [UnityTest, Category("AC-D15-06")]
        public IEnumerator ShadowBehindTheTooltip()
        {
            CreateHistory(HistorySeeds.F9, DeviceProfile.W1280);
            yield return GoToHistory();
            yield return OpenCard(0);
            Detail.Chart.ShowCursorAt(300);
            yield return Frames(3);
            var cursor = Detail.Chart.Cursor;
            var tooltip = cursor.Tooltip;
            var shadow = cursor.TooltipShadow;
            Assert.That(shadow.parent, Is.SameAs(tooltip.parent));
            Assert.That(shadow.parent.IndexOf(shadow), Is.LessThan(tooltip.parent.IndexOf(tooltip)), "detrás");
            Assert.That(shadow.resolvedStyle.opacity, Is.EqualTo(tooltip.resolvedStyle.opacity).Within(1e-4));
            var t = tooltip.worldBound;
            var s = shadow.worldBound;
            Assert.That(s.center.x, Is.EqualTo(t.center.x).Within(0.5f));
            Assert.That(s.center.y - t.center.y, Is.EqualTo(ShadowTexture.OffsetYDp).Within(0.5f), "+2 dp");
            Assert.That(s.width - t.width, Is.EqualTo(2 * ShadowTexture.BlurDp).Within(0.5f), "8 dp de desenfoque por lado");
            Assert.That(shadow.resolvedStyle.unityBackgroundImageTintColor.a, Is.EqualTo(ShadowTexture.Alpha).Within(0.01f), "35 %");
            Assert.That(shadow.resolvedStyle.unityBackgroundImageTintColor.r + shadow.resolvedStyle.unityBackgroundImageTintColor.g, Is.EqualTo(0).Within(1e-3), "negro");
            Assert.That(shadow.pickingMode, Is.EqualTo(PickingMode.Ignore));
            Assert.That(t.width, Is.GreaterThanOrEqualTo((float)ChartSpecs.TooltipMinWidth - 0.5f), "el tooltip no cambia de tamaño");
        }

        [UnityTest, Category("AC-D15-06")]
        public IEnumerator ReducedMotionLooksTheSame()
        {
            CreateHistory(HistorySeeds.F9, DeviceProfile.W1280, o => o.ReduceMotion = true);
            yield return GoToHistory();
            yield return OpenCard(0);
            Detail.Chart.ShowCursorAt(300);
            yield return Frames(3);
            Assert.That(Detail.Chart.Cursor.TooltipShadow.resolvedStyle.opacity, Is.EqualTo(1).Within(1e-4));
        }
    }

    // AC-D15-13 (#70): "No se pudo abrir la carpeta".
    public sealed class ExportNoticeTests : ExportUiTestBase
    {
        [UnityTest, Category("AC-D15-13")]
        public IEnumerator OpenFolderFailedToast()
        {
            yield return OpenSession(0);
            yield return ExportNow();
            Exporter.FailReveals();
            var action = H.Shell.Toasts.CurrentAction!;
            action.OnAction();
            yield return Frames();
            Assert.That(H.Shell.Toasts.CurrentText, Is.EqualTo(H.App.Texts.Value.Get(L10nKeys.ExportTcxOpenFolderFailed)));
            Assert.That(H.Shell.Toasts.CurrentAction, Is.Null, "sin acción");
            H.Advance(ExportMotionSpecs.OpenFolderFailedMs / 1000.0 - 0.1);
            yield return Frames();
            Assert.That(H.Shell.Toasts.IsVisible, Is.True, "sigue antes de los 4 s");
            H.Advance(0.2);
            yield return Settle();
            Assert.That(H.Shell.Toasts.IsVisible, Is.False, "se va a los 4 s");
            Assert.That(Exporter.Reveals.Count, Is.EqualTo(1));
        }
    }

    // AC-D15-21 (#74): Alt+Enter con un botón enfocado solo cambia la pantalla completa.
    public sealed class AltEnterTests : History.HistoryUiTestBase
    {
        [UnityTest, Category("AC-D15-21")]
        public IEnumerator AltEnterDoesNotActivateTheFocusedButton()
        {
            CreateHistory(HistorySeeds.F9, DeviceProfile.W1280);
            yield return GoToHistory();
            yield return Frames();
            var card = Card(0);
            card.Focus();
            yield return Frames();
            using (var submit = NavigationSubmitEvent.GetPooled(EventModifiers.Alt))
            {
                submit.target = card;
                card.SendEvent(submit);
            }

            yield return Settle();
            Assert.That(H.Shell.Pages.HasPages, Is.False, "Alt+Enter no activa");
            Assert.That(H.Shell.AltSubmitsBlocked, Is.EqualTo(1));
            H.PressEnter();
            yield return Settle();
            Assert.That(H.Shell.Pages.HasPages, Is.True, "Enter solo sí");
        }
    }

    // AC-D15-33 (#82): el ancla de desplazamiento al cambiar el ancho.
    public sealed class ScrollAnchorTests : History.HistoryUiTestBase
    {
        private static float TopOf(VisualElement element, ScrollView scroll)
        {
            return element.worldBound.yMin - scroll.contentViewport.worldBound.yMin;
        }

        [UnityTest, Category("AC-D15-33")]
        public IEnumerator HistoryList()
        {
            CreateHistory(HistorySeeds.F9P, DeviceProfile.TV);
            yield return GoToHistory();
            Screen.List.FinishBatches();
            yield return Settle();
            var scroll = Screen.List.Scroll;
            scroll.scrollOffset = new Vector2(0, 900);
            yield return Frames(3);
            var anchor = Screen.List.Anchor.AnchorIndex;
            Assert.That(anchor, Is.GreaterThan(0));
            var element = Screen.List.Content[anchor];
            var before = TopOf(element, scroll);
            H.Resize(DeviceProfile.TH);
            yield return Frames(4);
            Assert.That(TopOf(element, scroll), Is.EqualTo(before).Within(1f), "el primero visible queda en su lugar");
            var offset = scroll.scrollOffset.y;
            yield return H.App.History.LoadAsync().AsIEnumerator();
            yield return Frames(3);
            Assert.That(scroll.scrollOffset.y, Is.EqualTo(offset).Within(0.5f), "una recarga sin cambios no mueve la lista");
        }

        [UnityTest, Category("AC-D15-33")]
        public IEnumerator DetailPage()
        {
            CreateHistory(HistorySeeds.F9, DeviceProfile.TV);
            yield return GoToHistory();
            yield return OpenCard(0);
            var page = Detail;
            page.Scroll.scrollOffset = new Vector2(0, 200);
            yield return Frames(3);
            var anchor = page.Anchor.AnchorIndex;
            Assert.That(anchor, Is.GreaterThanOrEqualTo(0));
            var element = page.Content[anchor];
            var before = TopOf(element, page.Scroll);
            page.Chart.ShowCursorAt(120);
            yield return Frames();
            H.Resize(DeviceProfile.TH);
            yield return Frames(4);
            Assert.That(TopOf(element, page.Scroll), Is.EqualTo(before).Within(1f));
            Assert.That(page.Chart.Cursor.IsShown, Is.False, "el cursor se oculta con el cambio de ancho");
        }
    }

    internal static class It15HostExtensions
    {
        // Un segundo toque al botón de la tarjeta 0 sin exigir que responda (mientras exporta está deshabilitado).
        public static void Card0ExportTapUnchecked(this UiTestHost host)
        {
            host.TapUnchecked("history-card-0-export");
        }

        public static IEnumerator AsIEnumerator(this System.Threading.Tasks.Task task)
        {
            for (var i = 0; i < 600 && !task.IsCompleted; i++)
            {
                yield return null;
            }
        }
    }
}
