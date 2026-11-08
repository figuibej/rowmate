using System.Collections;
using NUnit.Framework;
using RowMate.App.History;
using RowMate.Core.History;
using RowMate.Presentation.Controls;
using RowMate.Presentation.Shell;
using RowMate.Tests.Support;
using RowMate.Tests.UiSupport;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace RowMate.Tests.PlayMode.History
{
    // SDT · Detalle de sesión (F-HIS-09 a -12; AC-HIS-09-01 a -04, AC-HIS-10-01, AC-HIS-11-02, AC-HIS-12-01; 02-tech-design §5.3).
    public sealed class SessionDetailTests : HistoryUiTestBase
    {
        [UnityTest, Category("AC-HIS-09-01")]
        public IEnumerator PushOverTabsAndTitleFree()
        {
            CreateHistory(HistorySeeds.F9);
            yield return GoToHistory();
            yield return OpenCard(1);
            Assert.That(Detail.AppBar.Title.text, Is.EqualTo("Entrenamiento libre"), "workoutFree, no el Libre de la tarjeta (Q-09)");
            Assert.That(H.Shell.Pages.CoversShell, Is.True, "por encima de la barra de pestañas");
            Assert.That(Detail.Root.worldBound.height, Is.EqualTo(H.Root.worldBound.height).Within(0.5f), "pantalla completa");
            Assert.That(H.Find("session-back"), Is.Not.Null);
        }

        [UnityTest, Category("AC-HIS-09-01")]
        public IEnumerator BackAndEscape()
        {
            CreateHistory(HistorySeeds.F9);
            yield return GoToHistory();
            yield return OpenCard(0);
            yield return Tap("session-back");
            Assert.That(DetailOrNull, Is.Null, "la flecha vuelve");
            yield return OpenCard(0);
            H.PressBack(BackSource.Escape);
            yield return Settle();
            Assert.That(DetailOrNull, Is.Null, "Escape vuelve (AC-HIS-09-01)");
            yield return OpenCard(0);
            H.PressBack(BackSource.SystemBack);
            yield return Settle();
            Assert.That(DetailOrNull, Is.Null, "Atrás de Android vuelve");
            Assert.That(H.App.Shell.SelectedIndex.Value, Is.EqualTo(3), "en Historial");
        }

        [UnityTest, Category("AC-HIS-09-03")]
        public IEnumerator SectionOrder()
        {
            CreateHistory(HistorySeeds.F9);
            yield return GoToHistory();
            yield return OpenCard(0);
            var summary = H.BoundsOf("session-summary");
            var steps = H.BoundsOf("session-steps");
            var metric = H.BoundsOf("session-metric");
            var chart = H.BoundsOf("session-chart");
            Assert.That(steps.yMin - summary.yMax, Is.EqualTo(20).Within(0.6f), "20 entre el Resumen y los pasos");
            Assert.That(metric.yMin - steps.yMax, Is.EqualTo(20).Within(0.6f),"20 hasta el selector");
            Assert.That(chart.yMin - metric.yMax, Is.EqualTo(8).Within(0.6f), "8 hasta el gráfico");
            Assert.That(chart.height, Is.EqualTo(220).Within(0.5f), "220 de alto");
            Assert.That(summary.xMin - H.BoundsOf("session-scroll").xMin, Is.EqualTo(16).Within(0.6f), "relleno 16");
            Assert.That(Detail.StepRows.Count, Is.EqualTo(8), "Escalera: 8 filas");
        }

        [UnityTest, Category("AC-HIS-09-03")]
        public IEnumerator FreeHasNoSteps()
        {
            CreateHistory(HistorySeeds.F9);
            yield return GoToHistory();
            yield return OpenCard(1);
            Assert.That(H.IsShown("session-steps"), Is.False, "sin índices de paso: sin Rendimiento por paso");
            Assert.That(Detail.Chart.Labels.XTexts()[0], Is.EqualTo("3:44"));
            Assert.That(Detail.Chart.Current!.BandCount, Is.EqualTo(0));
        }

        [UnityTest, Category("AC-HIS-09-03")]
        public IEnumerator NoTelemetry()
        {
            CreateHistory(HistorySeeds.F9);
            yield return GoToHistory();
            yield return OpenCard(3);
            Assert.That(Detail.Phase, Is.EqualTo(SessionDetailPhase.NoTelemetry));
            Assert.That(H.IsShown("session-no-telemetry"), Is.True);
            Assert.That(H.TextOf("session-no-telemetry-text"), Is.EqualTo("No hay datos de telemetría"));
            Assert.That(H.IsShown("session-scroll"), Is.False, "sin resumen");
        }

        [UnityTest, Category("AC-HIS-09-04")]
        public IEnumerator ErrorRetryBack()
        {
            ScriptedSessionRepository? repository = null;
            CreateScriptedHistory(r => repository = r);
            yield return GoToHistory();
            repository!.PointsFailures = 1;
            yield return OpenCard(0);
            Assert.That(Detail.Phase, Is.EqualTo(SessionDetailPhase.LoadFailed));
            Assert.That(H.IsShown("session-error"), Is.True, "no queda en el indicador");
            Assert.That(H.TextOf("session-error-text"), Is.EqualTo("No se pudo cargar la sesión"));
            yield return Tap("session-retry");
            Assert.That(Detail.Phase, Is.EqualTo(SessionDetailPhase.Ready), "Reintentar");
            yield return Tap("session-back");
            Assert.That(DetailOrNull, Is.Null, "la flecha sigue volviendo");
        }

        [UnityTest, Category("AC-HIS-10-01")]
        public IEnumerator SummaryEqualsCard()
        {
            CreateHistory(HistorySeeds.F9);
            yield return GoToHistory();
            var card = Texts(Card(0).Stats);
            yield return OpenCard(0);
            Assert.That(Texts(Detail.SummaryStats), Is.EqualTo(card));
            Assert.That(H.TextOf("session-summary-title"), Is.EqualTo("Resumen"));
            Assert.That(H.Find("session-summary-title")!.resolvedStyle.fontSize, Is.EqualTo(16).Within(0.01f));
        }

        [UnityTest, Category("AC-HIS-11-02")]
        public IEnumerator StepRowLayout()
        {
            CreateHistory(HistorySeeds.F9);
            yield return GoToHistory();
            yield return OpenCard(0);
            var rows = Detail.StepRows;
            Assert.That(rows.Line1At(0).text, Is.EqualTo("1:00  ·  183m"));
            Assert.That(rows.Line2At(0), Is.EqualTo(new[] { "100W", "20.0 spm", "2:30/500m" }));
            Assert.That(rows.Line1At(1).text, Is.EqualTo("2:00  ·  432m"));
            Assert.That(rows.Line2At(1), Is.EqualTo(new[] { "150W", "24.1 spm", "2:13/500m" }));
            Assert.That(rows.Line1At(5).text, Is.EqualTo("2:00  ·  433m"));
            Assert.That(rows.Line2At(5), Is.EqualTo(new[] { "138W", "22.1 spm", "2:02/500m" }));
            Assert.That(rows.Line1At(7).text, Is.EqualTo("1:05  ·  183m"));
            Assert.That(rows.BadgeAt(0).text, Is.EqualTo("1"));
            var badge = H.BoundsOf("session-step-0-badge");
            Assert.That(badge.width, Is.EqualTo(28).Within(0.5f));
            Assert.That(badge.height, Is.EqualTo(28).Within(0.5f));
            var gap = H.BoundsOf("session-step-1").yMin - H.BoundsOf("session-step-0").yMax;
            Assert.That(gap, Is.EqualTo(6).Within(0.6f), "6 entre filas");
            Assert.That(H.Find("session-step-0")!.resolvedStyle.borderTopLeftRadius, Is.EqualTo(8).Within(0.01f));
        }

        [UnityTest, Category("AC-HIS-12-01")]
        public IEnumerator SelectorOptions()
        {
            CreateHistory(HistorySeeds.F9);
            yield return GoToHistory();
            yield return OpenCard(0);
            var selector = Detail.Selector;
            Assert.That(selector.Count, Is.EqualTo(5));
            var labels = new[] { "Vatios", "SPM", "Split 500m", "Distancia", "Pulso" };
            for (var i = 0; i < labels.Length; i++)
            {
                Assert.That(selector.OptionAt(i).Q<Label>(className: "rm-segmented__label").text, Is.EqualTo(labels[i]));
            }

            Assert.That(selector.Selected, Is.EqualTo(0), "siempre arranca en Vatios");
            Assert.That(selector.CheckOf(0)!.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex), "la elegida con tilde");
            Assert.That(selector.CheckOf(1)!.resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
            Assert.That(selector.OptionAt(0).ClassListContains(SegmentedControl.SelectedClass), Is.True);
            yield return Tap("session-metric-split");
            Assert.That(Detail.Store.Metric.Value, Is.EqualTo(ChartMetric.Split));
            Assert.That(selector.CheckOf(2)!.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(selector.CheckOf(0)!.resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
            yield return Tap("session-back");
            yield return OpenCard(0);
            Assert.That(Detail.Store.Metric.Value, Is.EqualTo(ChartMetric.Watts), "no se recuerda (Q-21)");
        }

        [UnityTest, Category("AC-HIS-12-01")]
        public IEnumerator SelectorFitsP360()
        {
            CreateHistory(HistorySeeds.F9, DeviceProfile.P360, o =>
            {
                o.LanguageTags = new[] { "en-US" };
                o.TextScale = 1.3;
            });
            yield return GoToHistory();
            yield return OpenCard(0);
            yield return Frames(3);
            var selector = Detail.Selector;
            Assert.That(selector.ChartScale, Is.GreaterThanOrEqualTo(0.70).And.LessThanOrEqualTo(1.0));
            for (var i = 0; i < selector.Count; i++)
            {
                var label = selector.OptionAt(i).Q<Label>(className: "rm-segmented__label");
                var needed = label.MeasureTextSize(label.text, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined).x;
                Assert.That(label.contentRect.width, Is.GreaterThanOrEqualTo(needed - 0.5f), "sin cortes ni …: " + label.text);
            }

            Assert.That(selector.worldBound.xMax, Is.LessThanOrEqualTo(H.Root.worldBound.xMax - 16 + 0.6f), "dentro del relleno");
        }

        [UnityTest, Category("AC-HIS-12-02")]
        public IEnumerator ArrowsAndEnter()
        {
            CreateHistory(HistorySeeds.F9, DeviceProfile.W1280);
            yield return GoToHistory();
            yield return OpenCard(0);
            var selector = Detail.Selector;
            selector.OptionAt(0).Focus();
            yield return Frames();
            H.PressKey(KeyCode.RightArrow);

            yield return Frames();
            Assert.That(H.Focused, Is.SameAs(selector.OptionAt(1)), "la flecha mueve el foco");
            H.PressEnter();
            yield return Settle();
            Assert.That(Detail.Store.Metric.Value, Is.EqualTo(ChartMetric.Spm), "Enter elige");
        }
    }
}
