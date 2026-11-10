using System;
using System.Collections;
using NUnit.Framework;
using RowMate.Core.Ble;
using RowMate.Core.Design;
using RowMate.Presentation.Accessibility;
using RowMate.Presentation.Device;
using RowMate.Presentation.Shell;
using RowMate.Tests.Support;
using RowMate.Tests.UiSupport;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace RowMate.Tests.PlayMode.It14
{
    // It. 14 (14-pulido-plataformas/02-tech-design §2.3, §5, §7; AC-APP-16-10, AC-APP-03-12 a -15, AC-DEBT-01, -02, -04, -05): la capa de
    // arranque, el lector de pantalla forzado (el modelo propio, A11yTreeDump) y la deuda de Dispositivo y del aviso de exportación.
    public sealed class BootSplashTests : UiTestBase
    {
        [UnityTest, Category("AC-APP-16-10")]
        public IEnumerator LastChildFromFrameZeroThenFadesAndLeaves()
        {
            Create(new UiHostOptions { Profile = DeviceProfile.W1280, BootSplash = true });
            var shell = H.Shell;
            var splash = shell.BootSplash;
            Assert.That(splash, Is.Not.Null, "la capa desde el cuadro 0");
            Assert.That(shell.IndexOf(splash!), Is.EqualTo(shell.childCount - 1), "la última hija del shell (QA-G14-02)");
            Assert.That(splash!.resolvedStyle.opacity, Is.EqualTo(1f));
            yield return Frames(2);
            var picked = shell.panel.Pick(shell.worldBound.center);
            Assert.That(picked == splash || splash.Contains(picked), Is.True, "el centro resuelve en la capa (" + (picked?.name ?? "nada") + ", " + shell.worldBound + ")");
            shell.Add(new VisualElement { name = "capa-posterior" });
            H.Advance(1.0 / 60);
            Assert.That(shell.IndexOf(splash), Is.EqualTo(shell.childCount - 1), "sigue última tras un Add posterior");
            Assert.That(Math.Abs(splash.Mark.layout.width - BootSplashMotion.MarkDp), Is.LessThan(0.6f), "marca de 96 dp");
            Assert.That(Math.Abs(splash.Mark.worldBound.center.x - shell.worldBound.center.x), Is.LessThan(1f), "centrada");

            // El shell queda listo con su layout; desde ahí, 200 ms y la capa sale de la jerarquía (no agrega espera).
            for (var i = 0; i < 30 && !H.Handle.Ready.IsReady; i++)
            {
                yield return Frames(1);
                H.Advance(1.0 / 60);
            }

            Assert.That(H.Handle.Ready.IsReady, Is.True, "shell listo");
            H.Advance(0.1);
            Assert.That(splash.CurrentOpacity, Is.GreaterThan(0f).And.LessThan(1f), "a mitad de la salida");
            Assert.That(splash.pickingMode, Is.EqualTo(PickingMode.Ignore), "deja pasar la entrada desde que empieza a salir");
            H.Advance(0.15);
            Assert.That(splash.IsDone, Is.True);
            Assert.That(shell.BootSplash, Is.Null, "salió de la jerarquía");
        }

        [Test, Category("AC-APP-16-10")]
        public void WithoutTheOptionThereIsNoLayer()
        {
            Create(new UiHostOptions());
            Assert.That(H.Shell.BootSplash, Is.Null);
            Assert.That(H.Shell.Q("boot-splash"), Is.Null);
        }
    }

    public sealed class ScreenReaderTreeTests : UiTestBase
    {
        [UnityTest, Category("AC-APP-03-12")]
        public IEnumerator DeviceScreenIsReadInVisualOrder()
        {
            Create(new UiHostOptions { Profile = DeviceProfile.W1280, ScreenReader = true });
            yield return Settle();
            var dump = H.A11yDump();
            Assert.That(H.ScreenReader!.IsActive, Is.True);
            Assert.That(dump, Does.Contain("Header · Dispositivo"));
            Assert.That(dump, Does.Contain("Button · Buscar dispositivos"));
            Assert.That(dump, Does.Contain("TabButton · Dispositivo · [Selected]"), "la pestaña elegida");
            Assert.That(dump, Does.Contain("TabButton · Historial"));
            Assert.That(dump.IndexOf("Header · Dispositivo", StringComparison.Ordinal), Is.LessThan(dump.IndexOf("TabButton · Historial", StringComparison.Ordinal)),
                "arriba-abajo: el título antes de la barra de pestañas");
            Assert.That(H.Shell.Q("a11y"), Is.Null, "sin VisualElement agregados");
        }

        [UnityTest, Category("AC-APP-03-14")]
        public IEnumerator TitleOnTabChangeAndEachToastOnce()
        {
            Create(new UiHostOptions { Profile = DeviceProfile.W1280, ScreenReader = true });
            yield return Settle();
            H.A11yDump();
            H.Announcer.Clear();
            yield return Tap("tab-history");
            yield return Settle();
            H.A11yDump();
            Assert.That(H.Announcer.Screens, Has.Member("Historial"), "el título nuevo");
            H.Shell.Toasts.Show("Aviso de prueba");
            H.Shell.Toasts.Show("Aviso de prueba");
            Assert.That(H.Announcer.Announcements.FindAll(a => a == "Aviso de prueba").Count, Is.EqualTo(2), "dos Show son dos avisos");
            var before = H.Announcer.Announcements.Count;
            H.A11yDump();
            Assert.That(H.Announcer.Announcements.Count, Is.EqualTo(before), "repintar no vuelve a anunciar");
        }

        [UnityTest, Category("AC-APP-03-13")]
        public IEnumerator InvokeIsTheClickAndFocusIsStable()
        {
            Create(new UiHostOptions { Profile = DeviceProfile.W1280, ScreenReader = true });
            yield return Settle();
            H.A11yDump();
            var bridge = H.ScreenReader!;
            var item = System.Linq.Enumerable.FirstOrDefault(bridge.Items, i => i.Label == "Historial");
            Assert.That(item, Is.Not.Null);
            var element = (VisualElement)item!.Key;
            var node = bridge.NodeOf(element);
            bridge.Invoke(element);
            yield return Settle();
            Assert.That(H.Shell.Tabs.Screens[H.Shell.Tabs.Current], Is.SameAs(H.Shell.HistoryScreen), "invoked = el clic");
            var clears = bridge.ClearCount;
            H.A11yDump();
            Assert.That(bridge.NodeOf(element), Is.SameAs(node), "el mismo AccessibilityNode tras reconciliar");
            Assert.That(bridge.ClearCount, Is.EqualTo(clears), "sin Clear() sin cambio de raíz");
        }

        [Test, Category("AC-APP-03-15")]
        public void OffDoesNothing()
        {
            Create(new UiHostOptions());
            var previous = AssistiveSupport.screenReaderStatusOverride;
            AssistiveSupport.screenReaderStatusOverride = AssistiveSupport.ScreenReaderStatusOverride.ForceDisabled;
            try
            {
                var bridge = new ScreenReaderBridge(H.Shell, new FakeA11yAnnouncer());
                bridge.Tick(0);
                bridge.Tick(1);
                Assert.That(bridge.IsActive, Is.False);
                Assert.That(bridge.Hierarchy, Is.Null, "sin árbol con el lector apagado");
                Assert.That(bridge.Items.Count, Is.EqualTo(0));
                bridge.Dispose();
            }
            finally
            {
                AssistiveSupport.screenReaderStatusOverride = previous;
            }
        }
    }

    public sealed class DeviceDebtTests : UiTestBase
    {
        [UnityTest, Category("AC-DEBT-02")]
        public IEnumerator SearchingButtonWhiteSpinnerAndReadableLabel()
        {
            Create(new UiHostOptions { Profile = DeviceProfile.TV, Backend = HostBackends.FakeBle(null, BleAdapterState.On) });
            yield return Settle();
            H.Tap("device-search");
            yield return Frames(2);
            H.Advance(1);
            yield return Frames(2);
            var search = H.DeviceScreen!.Scan.Search;
            Assert.That(search.ClassListContains(ScanView.BusyClass), Is.True);
            Assert.That(search.BusySpinner!.IsShown, Is.True);
            Assert.That(search.BusySpinner.layout.width, Is.EqualTo(16f).Within(0.6f), "16 dp");
            Assert.That(search.BusySpinner.resolvedStyle.color, Is.EqualTo(Color.white), "blanco");
            Assert.That(search.Label.resolvedStyle.color.a, Is.EqualTo(ScanView.BusyLabelAlpha).Within(0.01f), "etiqueta al 70 %");
            Assert.That(search.resolvedStyle.opacity, Is.EqualTo(1f), "sin apagar el texto");
            var bg = search.resolvedStyle.backgroundColor;
            var label = search.Label.resolvedStyle.color;
            var blended = new Color(label.r * label.a + bg.r * (1 - label.a), label.g * label.a + bg.g * (1 - label.a), label.b * label.a + bg.b * (1 - label.a));
            Assert.That(Ratio(blended, bg), Is.GreaterThanOrEqualTo(4.5), "contraste ≥ 4,5:1");
        }

        [UnityTest, Category("AC-DEBT-04")]
        public IEnumerator ScanErrorWithoutTheOperation()
        {
            Create(new UiHostOptions { Profile = DeviceProfile.W1280, Backend = HostBackends.FakeBle(null, BleAdapterState.On) });
            yield return Settle();
            H.FakeBle!.Adapter.FailNextScan(new InvalidOperationException("error 2"));
            H.Tap("device-search");
            yield return Frames(2);
            H.Advance(0.5);
            yield return Settle();
            Assert.That(H.TextOf("device-error-text"), Is.EqualTo("Error al escanear: error 2"));
        }

        [UnityTest, Category("AC-DEBT-01")]
        public IEnumerator LongRowerNameWrapsAndTheActionKeepsItsSize()
        {
            var longRow = new FakeBleDevice(FakeBleDevices.RowId, "AMS-670B RowMaster 2024", FakeBleDevices.Row.Advertised, FakeBleDevices.Row.Services, -61);
            foreach (var scale in new[] { 1.0, 1.3 })
            {
                Host?.Dispose();
                Create(new UiHostOptions { Profile = DeviceProfile.TV, TextScale = scale, Backend = HostBackends.FakeBle(null, BleAdapterState.On) });
                yield return Settle();
                H.Tap("device-search");
                H.FakeBle!.Adapter.Advertise(longRow);
                yield return Frames(2);
                H.Advance(10.1);
                yield return Settle();
                H.Tap(ScanResultCard.NamePrefix + longRow.Id);
                yield return Frames(2);
                H.Advance(1);
                yield return Settle();
                H.FakeBle.Adapter.NeverRespond(longRow.Id);
                H.FakeBle.Adapter.DropLink(longRow.Id);
                yield return Frames(2);
                H.Advance(1);
                yield return Frames(4);
                var banner = H.DeviceScreen!.Scan.LinkBanner;
                Assert.That(banner.Text.text, Does.Contain("AMS-670B RowMaster 2024"), "nombre completo (" + scale + ")");
                Assert.That(banner.Text.resolvedStyle.whiteSpace, Is.EqualTo(WhiteSpace.Normal));
                Assert.That(banner.Action.resolvedStyle.flexShrink, Is.EqualTo(0f), "la acción no se achica");
                Assert.That(banner.Action.worldBound.xMax, Is.LessThanOrEqualTo(banner.worldBound.xMax + 0.5f), "la acción entera (" + scale + ")");
            }
        }

        private static double Ratio(Color a, Color b)
        {
            var la = Luminance(a);
            var lb = Luminance(b);
            return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
        }

        private static double Luminance(Color c)
        {
            return 0.2126 * Channel(c.r) + 0.7152 * Channel(c.g) + 0.0722 * Channel(c.b);
        }

        private static double Channel(float v)
        {
            return v <= 0.03928 ? v / 12.92 : Math.Pow((v + 0.055) / 1.055, 2.4);
        }
    }

    public sealed class ExportToastPolishTests : ExportUiTestBase
    {
        [UnityTest, Category("AC-DEBT-05")]
        public IEnumerator PathBreaksAndPauseWithHoverOrFocus()
        {
            yield return OpenSession(0, DeviceProfile.TV);
            yield return ExportNow();
            var layer = H.Shell.Toasts;
            Assert.That(layer.Text.text, Does.Contain("\\" + PathBreak.ZeroWidthSpace), "cortes después de cada barra");
            Assert.That(PathBreak.Strip(layer.Text.text), Is.EqualTo(SavedTitle));
            var clock = layer.ActionClock!;
            Assert.That(clock, Is.Not.Null);
            H.Advance(2);
            using (var enter = PointerEnterEvent.GetPooled())
            {
                enter.target = layer.Toast;
                layer.Toast.SendEvent(enter);
            }

            yield return Frames(1);
            Assert.That(clock.IsPaused, Is.True, "en pausa con el puntero encima");
            H.Advance(10);
            Assert.That(layer.IsVisible, Is.True, "no venció en pausa");
            using (var leave = PointerLeaveEvent.GetPooled())
            {
                leave.target = layer.Toast;
                layer.Toast.SendEvent(leave);
            }

            yield return Frames(1);
            Assert.That(clock.IsPaused, Is.False);
            H.Advance(3.5);
            Assert.That(layer.IsVisible, Is.True, "le quedaban unos 4 s");
            H.Advance(1);
            yield return Settle();
            Assert.That(layer.IsVisible, Is.False, "vence con lo que le quedaba");
        }
    }
}
