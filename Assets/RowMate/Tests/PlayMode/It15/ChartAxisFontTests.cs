using System.Collections;
using NUnit.Framework;
using RowMate.Core.Design;
using RowMate.Presentation.Controls;
using RowMate.Presentation.History;
using RowMate.Tests.Support;
using RowMate.Tests.UiSupport;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace RowMate.Tests.PlayMode.It15
{
    // AC-D15-25 (#37/#76, DT15-22; arte D15-01): las cifras de los ejes del gráfico en RowMate Numeric SC 600 a 11 dp en text.tertiary
    // (.rm-chart-axis__num, Skin.uss de WP-D), con las 10 cifras del mismo avance; la fecha de la tarjeta sigue en la misma cara (TY-02).
    public sealed class ChartAxisFontTests : History.HistoryUiTestBase
    {
        private const string NumericFace = "RowMateNumericSC-SemiBold-SDF";

        private static void AssertNumeric(Label label, string what)
        {
            Assert.That(label.ClassListContains(ChartAxisLabels.NumClass), Is.True, what + ": la clase");
            var font = label.resolvedStyle.unityFontDefinition.fontAsset;
            Assert.That(font, Is.Not.Null, what + ": la cara");
            Assert.That(font!.name, Is.EqualTo(NumericFace), what);
            Assert.That(label.resolvedStyle.fontSize, Is.EqualTo(11).Within(0.01f), what + ": 11 dp");
            var expected = TokenColors.ToColor(Tokens.TextTertiary);
            var color = label.resolvedStyle.color;
            Assert.That(color.r, Is.EqualTo(expected.r).Within(0.01f), what + ": text.tertiary");
            Assert.That(color.a, Is.EqualTo(expected.a).Within(0.01f), what + ": text.tertiary (alfa)");
        }

        [UnityTest, Category("AC-D15-25")]
        public IEnumerator AxisDigitsUseNumericSc()
        {
            CreateHistory(HistorySeeds.F9, DeviceProfile.W1280, o => o.IsDesktop = true);
            yield return GoToHistory();
            yield return OpenCard(0);
            yield return Frames(3);
            var labels = Detail.Chart.Labels;
            Assert.That(labels.XCount, Is.GreaterThan(0));
            Assert.That(labels.YCount, Is.GreaterThan(0));
            for (var i = 0; i < labels.XCount; i++)
            {
                AssertNumeric(labels.XAt(i), "X " + i);
            }

            for (var i = 0; i < labels.YCount; i++)
            {
                AssertNumeric(labels.YAt(i), "Y " + i);
            }

            // Cifras tabulares: "1111" mide lo mismo que "0000", "8888", etc. (el avance de ChartSpecs.AxisDigitAdvanceEm).
            var probe = labels.XAt(0);
            var reference = probe.MeasureTextSize("0000", 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined).x;
            for (var d = 1; d <= 9; d++)
            {
                var text = new string((char)('0' + d), 4);
                var width = probe.MeasureTextSize(text, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined).x;
                Assert.That(width, Is.EqualTo(reference).Within(0.05f), text + " con el mismo avance que 0000");
            }

            Assert.That(reference / 4f, Is.EqualTo((float)(ChartSpecs.AxisDigitAdvanceEm * 11)).Within(0.3f), "el avance que usa AxisCrowding");
        }

        [UnityTest, Category("AC-D15-25")]
        public IEnumerator CardDateKeepsItsFace()
        {
            CreateHistory(HistorySeeds.F9, DeviceProfile.W1280, o => o.IsDesktop = true);
            yield return GoToHistory();
            yield return Frames(3);
            var date = H.Find("history-card-0")!.Q<Label>(className: HistoryCardView.UssClass + "__date");
            Assert.That(date, Is.Not.Null);
            Assert.That(date!.resolvedStyle.unityFontDefinition.fontAsset!.name, Is.EqualTo(NumericFace), "TY-02 sin cambios");
            Assert.That(date.ClassListContains(ChartAxisLabels.NumClass), Is.False, "la fecha no toma la clase de los ejes");
        }
    }
}
