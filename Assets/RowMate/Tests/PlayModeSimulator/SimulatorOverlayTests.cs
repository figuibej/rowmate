using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using RowMate.Core.Models;
using RowMate.Core.Ports;
using RowMate.Simulator.Panel;
using RowMate.Tests.UiSupport;
using UnityEngine;
using UnityEngine.TestTools;

namespace RowMate.Tests.PlayMode.Simulator
{
    // Capa del simulador: cinta, llave y panel (F-BLE-16; 02-tech-design §3.5). Los 2 casos legacy de
    // test/simulator_overlay_test.dart y las reglas de posición y contenido.
    public sealed class SimulatorOverlayTests : SimulatorUiTestBase
    {
        public static IEnumerable<TestCaseData> Lt26()
        {
            yield return new TestCaseData().Returns(null).SetName("el panel se abre fuera del Navigator y + sube los watts")
                .SetCategory("LT-26").SetCategory("AC-BLE-16-02").SetCategory("AC-BLE-16-04").SetCategory("AC-BLE-16-05").SetCategory("AC-BLE-16-06")
                .SetCategory("AC-BLE-16-07").SetCategory("AC-BLE-16-08").SetCategory("AC-BLE-16-09").SetCategory("AC-HRM-15-08");
        }

        public static IEnumerable<TestCaseData> Lt27()
        {
            yield return new TestCaseData().Returns(null).SetName("en una ventana baja el botón de cerrar sigue accesible")
                .SetCategory("LT-27").SetCategory("AC-BLE-16-03").SetCategory("AC-BLE-16-12");
        }

        // legacy: test/simulator_overlay_test.dart:17 'el panel se abre fuera del Navigator y + sube los watts'
        [TestCaseSource(nameof(Lt26))]
        public IEnumerator PanelOpensAboveNavigationAndPlusRaisesWatts()
        {
            CreateSim(DeviceProfile.Dp(800, 600));
            yield return Settle();
            var root = H.Find("app-root")!;
            Assert.That(root.hierarchy[root.hierarchy.childCount - 1].name, Is.EqualTo("simulator-layer"), "la capa es el último hijo de la raíz");
            Assert.That(H.IsTextVisible("150 W"), Is.False);
            yield return Tap("sim-open");
            Assert.That(H.IsTextVisible("150 W"), Is.True);
            Assert.That(H.IsTextVisible("24 spm"), Is.True);
            yield return Tap("sim-watts-plus");
            Assert.That(Sim.Simulator.TargetWatts, Is.EqualTo(160));
            Assert.That(H.IsTextVisible("160 W"), Is.True);
            yield return Tap("sim-spm-plus");
            Assert.That(Sim.Simulator.TargetSpm, Is.EqualTo(25));
            yield return Tap("sim-hour-plus");
            Assert.That(H.HasText("Hora real") && H.Find("sim-hour-real")!.resolvedStyle.display == UnityEngine.UIElements.DisplayStyle.Flex, Is.True);
            yield return ScrollAndTap("sim-drop-rower");
            Assert.That(H.HasErrors(), Is.False, "Simular desconexión sin excepción");
            yield return Advance(3.5);
            _ = H.App.HeartRate.StartScanAsync();
            _ = H.App.HeartRate.ConnectAsync("SIM:HRM", "Pulsómetro simulado");
            yield return Settle();
            yield return ScrollAndTap("sim-drop-hrm");
            Assert.That(H.App.HeartRate.State.Value.IsConnected, Is.False);
            Assert.That(H.App.HeartRate.State.Value.IsRetrying, Is.True);
            yield return ScrollAndTap("sim-preset-fuerte");
            Assert.That(Sim.Simulator.TargetWatts, Is.EqualTo(280));
            Assert.That(Sim.Simulator.TargetSpm, Is.EqualTo(30));
            yield return ScrollAndTap("sim-rowing");
            Assert.That(Sim.Simulator.Rowing, Is.False);
            yield return Tap("sim-close");
            Assert.That(H.IsTextVisible("280 W"), Is.False, "cerrar oculta el panel en el acto");
            Assert.That(H.HasErrors(), Is.False);
        }

        // legacy: test/simulator_overlay_test.dart:90 'en una ventana baja el botón de cerrar sigue accesible'
        [TestCaseSource(nameof(Lt27))]
        public IEnumerator CloseButtonStaysReachableInAShortWindow()
        {
            CreateSim(DeviceProfile.P800);
            yield return Settle();
            yield return Tap("sim-open");
            Assert.That(H.IsTextVisible("150 W"), Is.True);
            Assert.That(H.WhyNotTappable("sim-close"), Is.Null, "sim-close dentro de la ventana");
            Assert.That(H.BoundsOf("sim-panel").height, Is.LessThanOrEqualTo((float)SimulatorPanelLayout.MaxHeight(360, SafeInsets.None) + 0.5f));
            yield return Tap("sim-close");
            Assert.That(H.IsTextVisible("150 W"), Is.False);
        }

        [UnityTest, Category("AC-BLE-16-01")]
        public IEnumerator RibbonIsAlwaysOnTop()
        {
            foreach (var insets in new[] { SafeInsets.None, new SafeInsets(0, 24, 0, 48) })
            {
                Create(new UiHostOptions { Backend = HostBackends.Simulator(false), SimulatorLayer = SimulatorLayerMode.Full, Insets = insets });
                yield return Settle();
                var layer = H.Find("simulator-layer")!;
                Assert.That(layer.hierarchy[layer.hierarchy.childCount - 1].name, Is.EqualTo("sim-ribbon"), "la cinta es el último hijo");
                var center = H.BoundsOf("sim-ribbon").center;
                Assert.That(center.x, Is.EqualTo(24).Within(0.6f), "centro en x = 24 desde la esquina de la pantalla");
                Assert.That(center.y, Is.EqualTo(24).Within(0.6f), "centro en y = 24, sin sumar insets");
                Assert.That(H.TextOf("sim-ribbon"), Is.EqualTo("SIMULADOR"));
            }
        }

        [UnityTest, Category("AC-BLE-16-02")]
        public IEnumerator WrenchPositionAndToggle()
        {
            CreateSim(DeviceProfile.TV);
            yield return Settle();
            var fab = H.BoundsOf("sim-open");
            var window = H.Root.worldBound;
            var bar = H.BoundsOf("tab-bar");
            Assert.That(fab.width, Is.EqualTo(40).Within(0.5f));
            Assert.That(window.xMax - fab.xMax, Is.EqualTo(16).Within(0.6f), "a 16 del borde derecho");
            Assert.That(bar.yMin - fab.yMax, Is.EqualTo(16).Within(0.6f), "16 sobre la barra real");
            yield return Tap("sim-open");
            Assert.That(H.IsShown("sim-open"), Is.False, "abierto: la llave se oculta");
            var panel = H.BoundsOf("sim-panel");
            Assert.That(panel.width, Is.EqualTo(280).Within(0.6f));
            Assert.That(bar.yMin - panel.yMax, Is.EqualTo(16).Within(0.6f), "el panel abre en el lugar de la llave");
            yield return Tap("sim-close");
            Assert.That(H.IsShown("sim-open"), Is.True);
        }

        [UnityTest, Category("AC-BLE-16-03"), Category("QA-G2-06")]
        public IEnumerator InsetsAreCountedOnce()
        {
            Create(new UiHostOptions { Backend = HostBackends.Simulator(false), SimulatorLayer = SimulatorLayerMode.Full, Insets = new SafeInsets(0, 24, 0, 48) });
            yield return Settle();
            yield return Frames(3);
            Assert.That(H.Root.worldBound.yMax - H.BoundsOf("sim-open").yMax, Is.EqualTo(112 + 16).Within(0.6f), "llave a 112 + 16 del borde inferior");
            yield return Tap("sim-open");
            var expected = (float)SimulatorPanelLayout.MaxHeight(DeviceProfile.TV.HeightDp, new SafeInsets(0, 24, 0, 48));
            Assert.That(expected, Is.EqualTo(730.29f).Within(0.01f));
            Assert.That(H.Find("sim-panel")!.resolvedStyle.maxHeight.value, Is.EqualTo(expected).Within(0.05f));
            Assert.That(H.BoundsOf("sim-panel").yMin, Is.GreaterThanOrEqualTo(40f), "el borde superior queda debajo de la barra de estado");
        }

        [UnityTest, Category("AC-BLE-16-04")]
        public IEnumerator PanelContentAndOrder()
        {
            CreateSim(DeviceProfile.TV);
            yield return Settle();
            yield return Tap("sim-open");
            var order = new[] { "sim-preset-suave", "sim-watts-row", "sim-spm-row", "sim-hour-row", "sim-rowing", "sim-drop-rower", "sim-drop-hrm" };
            var previous = float.MinValue;
            foreach (var name in order)
            {
                var y = H.BoundsOf(name).yMin;
                Assert.That(y, Is.GreaterThan(previous), name + " en orden");
                previous = y;
            }

            foreach (var text in new[] { "Simulador", "Suave", "Medio", "Fuerte", "Watts", "Ritmo", "Hora escena", "real", "Remando", "Simular desconexión", "Simular caída del pulsómetro" })
            {
                Assert.That(H.HasText(text), Is.True, text);
            }

            Assert.That(H.IsShown("sim-hour-real"), Is.False, "Hora real solo con la hora forzada");
            Assert.That(H.WhyNotTappable("sim-drop-hrm"), Is.Null, "en TV entra entero");
        }

        [UnityTest, Category("AC-BLE-16-10")]
        public IEnumerator ChangesVisibleWithinOneSecond()
        {
            CreateSim(DeviceProfile.TV);
            yield return Advance(60);
            yield return Tap("sim-open");
            yield return Tap("sim-preset-fuerte");
            yield return Advance(1);
            var watts = H.DeviceScreen!.Connected.Grid.Find(MetricKind.Watts)!.Value.Text;
            Assert.That(watts, Is.EqualTo("280"), "se ve en la lectura siguiente");
        }

        [UnityTest, Category("AC-BLE-16-11")]
        public IEnumerator ReopenShowsCurrentValues()
        {
            CreateSim(DeviceProfile.TV);
            yield return Settle();
            yield return Tap("sim-open");
            yield return Tap("sim-watts-plus");
            yield return Tap("sim-close");
            yield return Tap("sim-open");
            Assert.That(H.IsTextVisible("160 W"), Is.True);
            CreateSim(DeviceProfile.TV);
            yield return Settle();
            yield return Tap("sim-open");
            Assert.That(H.IsTextVisible("150 W"), Is.True, "nada se guarda al reiniciar");
        }

        // ── AC-BLE-16-02 con el texto nuevo de la It. 3 (DV-13; 03-rutinas/02-tech-design §4.8): la llave sube sobre el + ──

        private UiTestHost CreateSimWith(HostRoutineFactory routines, bool reduceMotion = false)
        {
            return Create(new UiHostOptions
            {
                Backend = HostBackends.Simulator(false),
                SimulatorLayer = SimulatorLayerMode.Full,
                Routines = routines,
                ReduceMotion = reduceMotion,
            });
        }

        private float WrenchBottom()
        {
            return H.Root.worldBound.yMax - H.BoundsOf("sim-open").yMax;
        }

        [UnityTest, Category("AC-BLE-16-02")]
        public IEnumerator WrenchSitsAboveTheFabInRoutines()
        {
            var states = new (string Name, HostRoutineFactory Routines)[]
            {
                ("cargando", HostRoutines.Scripted(r => r.LoadDelay = RowMate.Tests.Support.ScriptedRoutineRepository.Never)),
                ("vacío", HostRoutines.Seeded(RowMate.Tests.Support.RoutineSeeds.Empty)),
                ("lista", HostRoutines.Seeded(RowMate.Tests.Support.RoutineSeeds.Lista)),
                ("error", HostRoutines.Scripted(r => r.FailLoad = true)),
            };
            foreach (var state in states)
            {
                CreateSimWith(state.Routines);
                yield return Tap("tab-routines");
                yield return Settle();
                Assert.That(WrenchBottom(), Is.EqualTo(64 + 88).Within(0.6f), state.Name + ": la llave a barra + 88, sobre el +");
                Assert.That(H.BoundsOf("routines-add").yMin - H.BoundsOf("sim-open").yMax, Is.EqualTo(16).Within(0.6f), state.Name + ": 16 sobre el +");
            }
        }

        [UnityTest, Category("AC-BLE-16-02")]
        public IEnumerator WrenchStaysOnOtherTabsAndWithTheEditor()
        {
            CreateSimWith(HostRoutines.Seeded(RowMate.Tests.Support.RoutineSeeds.Lista));
            yield return Settle();
            Assert.That(WrenchBottom(), Is.EqualTo(64 + 16).Within(0.6f), "Dispositivo: barra + 16");
            foreach (var tab in new[] { "tab-workout", "tab-history" })
            {
                yield return Tap(tab);
                yield return Settle();
                Assert.That(WrenchBottom(), Is.EqualTo(64 + 16).Within(0.6f), tab + ": en su lugar");
            }

            yield return Tap("tab-routines");
            yield return Settle();
            Assert.That(WrenchBottom(), Is.EqualTo(64 + 88).Within(0.6f));
            yield return Tap("routine-card-0");
            yield return Settle();
            Assert.That(WrenchBottom(), Is.EqualTo(64 + 16).Within(0.6f), "con el editor (el + tapado), vuelve a su lugar");
            yield return Tap("editor-back");
            yield return Settle();
            Assert.That(WrenchBottom(), Is.EqualTo(64 + 88).Within(0.6f), "y al volver, sobre el +");
        }

        [UnityTest, Category("AC-BLE-16-02")]
        public IEnumerator WrenchMovesWithoutJumpsAndStaysTappable()
        {
            CreateSimWith(HostRoutines.Seeded(RowMate.Tests.Support.RoutineSeeds.Lista));
            yield return Settle();
            yield return Frames();
            H.Tap("tab-routines");
            var previous = WrenchBottom();
            var sawMoving = false;
            for (var i = 0; i < 40; i++)
            {
                H.Step();
                yield return null;
                var now = WrenchBottom();
                Assert.That(now, Is.GreaterThanOrEqualTo(previous - 0.6f), "sube sin volver atrás");
                Assert.That(now - previous, Is.LessThan(30f), "sin saltos de un cuadro a otro");
                if (H.SimulatorLayer!.IsMoving)
                {
                    sawMoving = true;
                    Assert.That(H.WhyNotTappable("sim-open"), Is.Null, "se puede tocar mientras se mueve");
                }

                previous = now;
            }

            Assert.That(sawMoving, Is.True, "se movió con el resorte");
            Assert.That(WrenchBottom(), Is.EqualTo(64 + 88).Within(0.6f));
        }

        [UnityTest, Category("AC-BLE-16-02")]
        public IEnumerator PanelOpensInTheSamePlaceInRoutines()
        {
            CreateSimWith(HostRoutines.Seeded(RowMate.Tests.Support.RoutineSeeds.Lista));
            yield return Tap("tab-routines");
            yield return Settle();
            yield return Tap("sim-open");
            Assert.That(H.IsShown("sim-open"), Is.False, "abierto: la llave no se ve");
            var panel = H.BoundsOf("sim-panel");
            Assert.That(H.BoundsOf("tab-bar").yMin - panel.yMax, Is.EqualTo(16).Within(0.6f), "el panel abre siempre en el mismo lugar (barra + 16)");
            Assert.That(panel.Overlaps(H.BoundsOf("routines-add")), Is.True, "y tapa el + mientras está abierto");
            yield return Tap("sim-close");
            yield return Settle();
            Assert.That(WrenchBottom(), Is.EqualTo(64 + 88).Within(0.6f), "al cerrar, la llave vuelve sobre el +");
        }

        [UnityTest, Category("AC-BLE-16-02"), Category("AC-APP-05-08")]
        public IEnumerator ReducedMotionMovesTheWrenchAtOnce()
        {
            CreateSimWith(HostRoutines.Seeded(RowMate.Tests.Support.RoutineSeeds.Lista), true);
            yield return Settle();
            yield return Frames();
            H.Tap("tab-routines");
            H.Step();
            yield return Frames();
            Assert.That(H.SimulatorLayer!.IsMoving, Is.False);
            Assert.That(WrenchBottom(), Is.EqualTo(64 + 88).Within(0.6f), "con reducir movimiento, en el acto");
        }
    }
}
