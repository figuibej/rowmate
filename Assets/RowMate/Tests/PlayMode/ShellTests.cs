using System.Collections;
using NUnit.Framework;
using RowMate.App.Device;
using RowMate.App.Shell;
using RowMate.Core.Ble;
using RowMate.Core.Design;
using RowMate.Presentation.Controls;
using RowMate.Presentation.Device;
using RowMate.Tests.UiSupport;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace RowMate.Tests.PlayMode
{
    // Shell y barra de pestañas (F-APP-01, F-APP-02; 02-tech-design §3.2) con el remo y el pulsómetro de prueba.
    public sealed class ShellTests : UiTestBase
    {
        [UnityTest, Category("AC-APP-01-01"), Category("AC-BLE-10-01")]
        public IEnumerator StartsOnDeviceTab()
        {
            Create(new UiHostOptions());
            yield return Frames();
            Assert.That(H.App.Shell.SelectedIndex.Value, Is.EqualTo(0));
            Assert.That(H.App.Shell.Destinations[0].Id, Is.EqualTo(TabDestinationId.Device), "Dispositivo es el primer destino");
            Assert.That(H.IsShown("screen-device"), Is.True);
            Assert.That(H.Find("tab-device")!.ClassListContains("rm-tab--selected"), Is.True);
        }

        [UnityTest, Category("AC-APP-02-01")]
        public IEnumerator TabBarHasFourDestinations()
        {
            Create(new UiHostOptions());
            yield return Frames();
            var bar = H.Find("tab-bar")!;
            var tabs = bar.Query<Button>(className: "rm-tab").ToList();
            Assert.That(tabs.Count, Is.EqualTo(4));
            Assert.That(tabs[0].name, Is.EqualTo("tab-device"));
            Assert.That(tabs[1].name, Is.EqualTo("tab-workout"));
            Assert.That(tabs[2].name, Is.EqualTo("tab-routines"));
            Assert.That(tabs[3].name, Is.EqualTo("tab-history"));
            Assert.That(H.IsTextVisible("Dispositivo"), Is.True);
            Assert.That(H.IsTextVisible("Workout"), Is.True);
            Assert.That(H.IsTextVisible("Rutinas"), Is.True);
            Assert.That(H.IsTextVisible("Historial"), Is.True);
            Assert.That(H.Find("tab-profile"), Is.Null, "sin Strava no hay Perfil");
        }

        [UnityTest, Category("AC-APP-02-02")]
        public IEnumerator FiveDestinationsWithStravaConfig()
        {
            Create(new UiHostOptions { ProfileTab = true });
            yield return Frames();
            var tabs = H.Find("tab-bar")!.Query<Button>(className: "rm-tab").ToList();
            Assert.That(tabs.Count, Is.EqualTo(5));
            Assert.That(tabs[3].name, Is.EqualTo("tab-history"), "Historial sigue cuarto");
            Assert.That(tabs[4].name, Is.EqualTo("tab-profile"));
            var icon = tabs[4].Q<IconView>();
            Assert.That(icon.Codepoint, Is.EqualTo(Icons.Person));
        }

        [UnityTest, Category("AC-APP-02-03")]
        public IEnumerator SelectionShowsPillGlyphAndLabels()
        {
            Create(new UiHostOptions());
            yield return Frames();
            yield return Tap("tab-workout");
            Assert.That(H.App.Shell.SelectedIndex.Value, Is.EqualTo(1));
            var workout = H.Find("tab-workout")!;
            Assert.That(workout.ClassListContains("rm-tab--selected"), Is.True);
            var icon = workout.Q<IconView>();
            Assert.That(icon.Codepoint, Is.EqualTo(Icons.Timer));
            Assert.That(icon.IsFilled, Is.True, "Workout elegido va relleno");
            var pill = H.Find("tab-bar-pill")!;
            var iconBox = workout.Q(className: "rm-tab__icon-box");
            Assert.That(Mathf.Abs(pill.worldBound.center.x - iconBox.worldBound.center.x), Is.LessThan(1f), "píldora detrás del ícono elegido");
            Assert.That(pill.worldBound.width, Is.EqualTo(56).Within(0.6f));
            Assert.That(pill.worldBound.height, Is.EqualTo(32).Within(0.6f));
            var label = workout.Q<Label>(className: "rm-tab__label");
            Assert.That(label.worldBound.yMin, Is.GreaterThanOrEqualTo(pill.worldBound.yMax - 0.5f), "la etiqueta va debajo de la píldora");
            foreach (var other in new[] { "tab-device", "tab-routines", "tab-history" })
            {
                Assert.That(H.Find(other)!.ClassListContains("rm-tab--selected"), Is.False, other);
                Assert.That(H.IsShown(other), Is.True, other + ": las etiquetas se ven siempre");
            }

            // Tocar el elegido no hace nada.
            yield return Tap("tab-workout");
            Assert.That(H.App.Shell.SelectedIndex.Value, Is.EqualTo(1));
        }

        [UnityTest, Category("AC-APP-02-05")]
        public IEnumerator DeviceIconIgnoresRowerStatus()
        {
            Create(new UiHostOptions());
            yield return Frames();
            var icon = H.Find("tab-device")!.Q<IconView>();
            Assert.That(icon.Codepoint, Is.EqualTo(Icons.BluetoothConnected), "elegido: bluetooth_connected");
            H.TestRower!.Emit(BleStatus.Connected);
            yield return Settle();
            Assert.That(icon.Codepoint, Is.EqualTo(Icons.BluetoothConnected));
            yield return Tap("tab-history");
            Assert.That(icon.Codepoint, Is.EqualTo(Icons.Bluetooth), "sin elegir: bluetooth, aunque el remo esté conectado");
        }

        [UnityTest, Category("AC-APP-02-04")]
        public IEnumerator ScrollPositionSurvivesTabSwitch()
        {
            Create(new UiHostOptions { Profile = DeviceProfile.L640 });
            H.TestRower!.Emit(BleStatus.Connected);
            yield return Settle();
            H.ScrollToEnd("device-connected-scroll");
            yield return Frames();
            var offset = H.ScrollOffsetOf("device-connected-scroll");
            Assert.That(offset, Is.GreaterThan(10), "en 640 × 360 el contenido se desplaza");
            yield return Tap("tab-routines");
            yield return Tap("tab-device");
            Assert.That(H.ScrollOffsetOf("device-connected-scroll"), Is.EqualTo(offset).Within(0.5f), "IndexedStack: la pantalla no se recrea");
        }

        [UnityTest, Category("AC-APP-02-06")]
        public IEnumerator BackAtRootGoesToThePolicyOnce()
        {
            var policy = new CountingBackButtonPolicy();
            Create(new UiHostOptions { BackButton = policy });
            yield return Tap("tab-routines");
            H.Handle.HandleBack();
            yield return Frames();
            Assert.That(policy.Calls, Is.EqualTo(1));
            Assert.That(H.App.Shell.SelectedIndex.Value, Is.EqualTo(2), "Atrás en la raíz no cambia de pestaña");
        }

        [UnityTest, Category("AC-APP-02-07")]
        public IEnumerator ResizeChangesColumnsWithoutRebuilding()
        {
            Create(new UiHostOptions { Profile = DeviceProfile.TV });
            H.TestRower!.Emit(BleStatus.Connected);
            yield return Settle();
            var screen = H.DeviceScreen!;
            var grid = screen.Connected.Grid;
            Assert.That(grid.Spec!.Columns, Is.EqualTo(2));
            H.Resize(DeviceProfile.TH);
            yield return Frames(3);
            Assert.That(H.DeviceScreen, Is.SameAs(screen), "rotar no recrea la pantalla");
            Assert.That(grid.Spec!.Columns, Is.EqualTo(3));
            Assert.That(H.App.Device.State.Value.IsConnected, Is.True);
        }

        [UnityTest, Category("AC-APP-02-08")]
        public IEnumerator PlaceholdersShowTitleAndDimIconOnly()
        {
            Create(new UiHostOptions());
            // Desde la It. 3 Rutinas ya no es provisoria (RLT.RoutinesIsNotAPlaceholder): siguen Workout e Historial.
            var cases = new[] { ("tab-workout", "screen-workout", "Entrenamiento"), ("tab-history", "screen-history", "Historial") };
            foreach (var (tab, screen, title) in cases)
            {
                yield return Tap(tab);
                var element = H.Find(screen)!;
                var texts = UiQuery.VisibleTexts(element);
                Assert.That(texts, Is.EqualTo(new[] { title }), screen + ": solo el título");
                var icon = element.Q<IconView>(className: "rm-placeholder__icon");
                Assert.That(icon.resolvedStyle.opacity, Is.EqualTo(0.24f).Within(0.001f));
                Assert.That(icon.resolvedStyle.width, Is.EqualTo(56).Within(0.5f));
                Assert.That(icon.IsFilled, Is.False, "glifo sin elegir");
            }
        }

        [UnityTest, Category("AC-APP-01-06")]
        public IEnumerator NoDebugMarksBesidesTheSimulatorRibbon()
        {
            Create(new UiHostOptions());
            yield return Frames();
            Assert.That(H.Find("sim-ribbon"), Is.Null, "sin la capa del simulador no hay cinta");
            Assert.That(H.HasText("DEBUG"), Is.False);
            Assert.That(H.HasText("Show BLE debug"), Is.False, "el diagnóstico es de la It. 10a");
        }

        [UnityTest, Category("AC-HRM-13-01")]
        public IEnumerator HrmCardIsInTheScanViewWhenTheRowerIsNotConnected()
        {
            Create(new UiHostOptions());
            yield return Frames();
            Assert.That(H.DeviceScreen!.View, Is.EqualTo(DeviceViewKind.Scan));
            Assert.That(H.Find("hrm-card-slot-scan")!.Contains(H.Find("hrm-card")!), Is.True);
        }
    }
}
