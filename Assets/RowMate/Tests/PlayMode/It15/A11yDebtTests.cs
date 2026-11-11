using System.Collections;
using NUnit.Framework;
using RowMate.Presentation.Accessibility;
using RowMate.Tests.Support;
using RowMate.Tests.UiSupport;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace RowMate.Tests.PlayMode.It15
{
    // AC-D15-28 (#80): el marco de cada nodo coincide con el rectángulo del elemento en la pantalla y las listas informan el
    // desplazamiento. La convención (fijada acá): píxeles de pantalla, origen ARRIBA a la izquierda, y hacia abajo (la del panel × la escala
    // panel → pantalla); TryGetNodeAt en el centro del rectángulo devuelve el nodo.
    public sealed class ScreenReaderFrameTests : History.HistoryUiTestBase
    {
        [UnityTest, Category("AC-D15-28")]
        public IEnumerator FramesMatchTheScreenAndHitTest()
        {
            CreateHistory(HistorySeeds.F9, DeviceProfile.W1280, o => o.ScreenReader = true);
            yield return GoToHistory();
            yield return Settle();
            var bridge = H.ScreenReader!;
            bridge.RebuildNow();
            var card = Card(1);
            var node = bridge.NodeOf(card);
            Assert.That(node, Is.Not.Null, "la tarjeta tiene nodo");
            bridge.Hierarchy!.RefreshNodeFrames();
            var scale = ScreenReaderBridge.PanelToScreenScale(card, 1);
            var expected = new Rect(card.worldBound.x * scale, card.worldBound.y * scale, card.worldBound.width * scale, card.worldBound.height * scale);
            var frame = node!.frame;
            Assert.That(frame.x, Is.EqualTo(expected.x).Within(1f));
            Assert.That(frame.y, Is.EqualTo(expected.y).Within(1f));
            Assert.That(frame.width, Is.EqualTo(expected.width).Within(1f));
            Assert.That(frame.height, Is.EqualTo(expected.height).Within(1f));
            Assert.That(scale, Is.EqualTo(UnityEngine.Screen.width / card.panel.visualTree.worldBound.width).Within(1e-4f), "la escala real del panel");
            // TryGetNodeAt devuelve el nodo más adentro: en la tarjeta, una de sus cifras (Metric); el título de la barra no tiene hijos.
            var found = bridge.Hierarchy.TryGetNodeAt(frame.center.x, frame.center.y, out var inCard);
            Assert.That(found, Is.True, "TryGetNodeAt en el centro de la tarjeta");
            Assert.That(inCard!.frame.Overlaps(frame), Is.True, "un nodo de la tarjeta (ella o una cifra suya)");
            var title = H.Find("history-appbar")!.Q<Label>(className: "rm-appbar__title")!;
            var titleNode = bridge.NodeOf(title)!;
            var titleFrame = ScreenReaderBridge.ScreenFrame(title, 1);
            Assert.That(bridge.Hierarchy.TryGetNodeAt(titleFrame.center.x, titleFrame.center.y, out var hit), Is.True);
            Assert.That(hit, Is.SameAs(titleNode), "en el centro del título, su nodo");
        }
    }

    public sealed class ScreenReaderScrollTests : History.HistoryUiTestBase
    {
        [UnityTest, Category("AC-D15-28")]
        public IEnumerator ListsReportScrolling()
        {
            CreateHistory(HistorySeeds.F9P, DeviceProfile.TV, o => o.ScreenReader = true);
            yield return GoToHistory();
            Screen.List.FinishBatches();
            yield return Settle();
            var bridge = H.ScreenReader!;
            bridge.RebuildNow();
            Assert.That(ScreenReaderBridge.ListOf(Card(0)), Is.SameAs(Screen.List.Scroll));
            var before = Screen.List.Scroll.scrollOffset.y;
            Assert.That(bridge.ScrollPage(Card(0), AccessibilityScrollDirection.Down), Is.True);
            yield return Frames();
            Assert.That(Screen.List.Scroll.scrollOffset.y, Is.GreaterThan(before + 100));
            Assert.That(H.Announcer.PagesScrolled.Count, Is.EqualTo(1), "SendPageScrolledAnnouncement");
            Assert.That(H.Announcer.PagesScrolled[0], Is.Not.Empty);
            Assert.That(bridge.ScrollPage(Card(0), AccessibilityScrollDirection.Up), Is.True);
            Assert.That(ScreenReaderBridge.ListOf(H.Find("tab-history")!), Is.Null, "fuera de una lista, nada");
        }
    }

    // AC-D15-26 (#79): la auditoría de textos encuentra lo que no es del catálogo y no mira los paneles de diagnóstico.
    public sealed class TextAuditProbeTests : History.HistoryUiTestBase
    {
        [UnityTest, Category("AC-D15-26")]
        public IEnumerator UnknownTextIsOneViolation()
        {
            CreateHistory(HistorySeeds.F9, DeviceProfile.W1280);
            yield return GoToHistory();
            yield return Settle();
            var baseline = TextAuditProbe.Violations(H, "Historial", "prueba", "Escalera", "Rutina borrada").Count;
            var probe = new Label("zz-no-catalog") { name = "zz-probe" };
            probe.style.position = Position.Absolute;
            probe.style.left = 100;
            probe.style.top = 100;
            H.Shell.Add(probe);
            yield return Frames(3);
            var violations = TextAuditProbe.Violations(H, "Historial", "prueba", "Escalera", "Rutina borrada");
            Assert.That(violations.Count, Is.EqualTo(baseline + 1), string.Join("\n", violations));
            Assert.That(string.Join("\n", violations), Does.Contain("zz-no-catalog"));
            probe.AddToClassList(TextAuditProbe.ExemptClass);
            yield return Frames(2);
            Assert.That(TextAuditProbe.Violations(H, "Historial", "prueba", "Escalera", "Rutina borrada").Count, Is.EqualTo(baseline), "exento");
            probe.RemoveFromHierarchy();
        }

        [Test, Category("AC-D15-26")]
        public void ExemptPanels()
        {
            var debug = new RowMate.Presentation.Device.BleDebugPanel();
            Assert.That(debug.ClassListContains(TextAuditProbe.ExemptClass), Is.True, "diagnóstico Bluetooth");
        }
    }
}
