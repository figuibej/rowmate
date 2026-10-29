using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using RowMate.Core.Models;
using RowMate.Core.Ports;
using RowMate.Presentation.Controls;
using RowMate.Simulator.Panel;
using RowMate.Tests.Support;
using RowMate.Tests.UiSupport;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace RowMate.Tests.PlayMode.Simulator
{
    // Capa del simulador: cinta, llave y panel (F-BLE-16; 02-tech-design §3.5). Los 2 casos legacy de
    // test/simulator_overlay_test.dart y las reglas de posición y contenido. Desde la It. 4a, con D-ART-10 (DV-39): la llave en el
    // carril izquierdo fuera del entrenamiento y en el lugar del HUD adentro, y el panel que crece desde ella y la sigue.
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

        [UnityTest, Category("AC-BLE-16-02"), Category("AC-BLE-16-03")]
        public IEnumerator WrenchPositionAndToggle()
        {
            CreateSim(DeviceProfile.TV);
            yield return Settle();
            var fab = H.BoundsOf("sim-open");
            var window = H.Root.worldBound;
            var bar = H.BoundsOf("tab-bar");
            Assert.That(fab.width, Is.EqualTo(40).Within(0.5f));
            Assert.That(fab.xMin - window.xMin, Is.EqualTo(16).Within(0.6f), "D-ART-10: a 16 del borde izquierdo");
            Assert.That(bar.yMin - fab.yMax, Is.EqualTo(16).Within(0.6f), "16 sobre la barra real");
            yield return Tap("sim-open");
            Assert.That(H.IsShown("sim-open"), Is.False, "abierto: la llave se oculta");
            var panel = H.BoundsOf("sim-panel");
            Assert.That(panel.width, Is.EqualTo(280).Within(0.6f));
            Assert.That(panel.xMin - window.xMin, Is.EqualTo(16).Within(0.6f), "el panel abre en el lugar de la llave, anclado abajo a la izquierda");
            Assert.That(bar.yMin - panel.yMax, Is.EqualTo(16).Within(0.6f));
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

        // ── D-ART-10 (It. 4a, DV-39; 04a-motor-hud-funcional/02-tech-design §6.10; 04-design-notes §3.9): el carril izquierdo fuera
        // del entrenamiento, el lugar del HUD adentro, el cambio de lugar con fundido, el panel que crece desde la llave y la sigue,
        // el tope de Watts, los menús que la esquivan y la llave en el anillo de Tab del inmersivo. Reemplaza "la llave sube sobre
        // el +" de la It. 3 (DV-13). ──

        private UiTestHost CreateSimWith(HostRoutineFactory routines, bool reduceMotion = false, DeviceProfile? profile = null)
        {
            return Create(new UiHostOptions
            {
                Profile = profile ?? DeviceProfile.TV,
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

        private float WrenchLeft()
        {
            return H.BoundsOf("sim-open").xMin - H.Root.worldBound.xMin;
        }

        // La caja en coordenadas de la ventana (y desde arriba), como 04-design-notes §3.8.
        private Rect InWindow(string name)
        {
            var b = H.BoundsOf(name);
            var w = H.Root.worldBound;
            return new Rect(b.xMin - w.xMin, b.yMin - w.yMin, b.width, b.height);
        }

        private static bool Overlap(Rect a, Rect b)
        {
            return a.xMin < b.xMax - 0.01f && b.xMin < a.xMax - 0.01f && a.yMin < b.yMax - 0.01f && b.yMin < a.yMax - 0.01f;
        }

        private IEnumerator StartFree()
        {
            yield return Tap("tab-workout");
            yield return Tap("workout-free");
            yield return Settle();
            Assert.That(H.Immersive, Is.Not.Null, "el inmersivo arriba");
        }

        [UnityTest, Category("AC-BLE-16-02"), Category("AC-RUT-10-04")]
        public IEnumerator WrenchLeftLaneOutsideTheWorkout()
        {
            var states = new (string Name, HostRoutineFactory Routines)[]
            {
                ("cargando", HostRoutines.Scripted(r => r.LoadDelay = ScriptedRoutineRepository.Never)),
                ("vacío", HostRoutines.Seeded(RoutineSeeds.Empty)),
                ("lista", HostRoutines.Seeded(RoutineSeeds.Lista)),
                ("error", HostRoutines.Scripted(r => r.FailLoad = true)),
            };
            foreach (var state in states)
            {
                CreateSimWith(state.Routines);
                yield return Tap("tab-routines");
                yield return Settle();
                Assert.That(WrenchLeft(), Is.EqualTo(16).Within(0.6f), state.Name + ": abajo a la izquierda");
                Assert.That(WrenchBottom(), Is.EqualTo(64 + 16).Within(0.6f), state.Name + ": barra + 16");
                var add = H.BoundsOf("routines-add");
                Assert.That(Overlap(add, H.BoundsOf("sim-open")), Is.False, state.Name + ": la llave no toca el +");
                Assert.That(H.Root.worldBound.xMax - add.xMax, Is.EqualTo(16).Within(0.6f), state.Name + ": el + solo en su rincón");
                Assert.That(H.BoundsOf("tab-bar").yMin - add.yMax, Is.EqualTo(16).Within(0.6f));
                Assert.That(H.WhyNotTappable("routines-add"), Is.Null, state.Name + ": el + se toca");
            }
        }

        [UnityTest, Category("AC-BLE-16-02")]
        public IEnumerator WrenchStaysOnOtherTabsAndWithTheEditor()
        {
            CreateSimWith(HostRoutines.Seeded(RoutineSeeds.Lista));
            yield return Settle();
            Assert.That(WrenchBottom(), Is.EqualTo(64 + 16).Within(0.6f), "Dispositivo: barra + 16");
            Assert.That(WrenchLeft(), Is.EqualTo(16).Within(0.6f));
            foreach (var tab in new[] { "tab-workout", "tab-history", "tab-routines" })
            {
                yield return Tap(tab);
                yield return Settle();
                Assert.That(WrenchBottom(), Is.EqualTo(64 + 16).Within(0.6f), tab + ": entre pestañas no se mueve");
                Assert.That(WrenchLeft(), Is.EqualTo(16).Within(0.6f), tab);
            }

            yield return Tap("routine-card-0");
            yield return Settle();
            Assert.That(WrenchBottom(), Is.EqualTo(16).Within(0.6f), "con el editor (tapa la barra): 16 + margen inferior");
            Assert.That(WrenchLeft(), Is.EqualTo(16).Within(0.6f), "en el mismo carril");
            yield return Tap("editor-back");
            yield return Settle();
            Assert.That(WrenchBottom(), Is.EqualTo(64 + 16).Within(0.6f), "y al volver, barra + 16");
        }

        [UnityTest, Category("AC-BLE-16-02")]
        public IEnumerator WrenchMovesWithoutJumpsAndStaysTappable()
        {
            CreateSimWith(HostRoutines.Seeded(RoutineSeeds.Lista));
            yield return Tap("tab-routines");
            yield return Settle();
            yield return Frames();
            H.Tap("routine-card-0");
            var previous = WrenchBottom();
            var sawMoving = false;
            for (var i = 0; i < 40; i++)
            {
                H.Step();
                yield return null;
                var now = WrenchBottom();
                Assert.That(now, Is.LessThanOrEqualTo(previous + 0.6f), "baja sin volver atrás");
                Assert.That(previous - now, Is.LessThan(30f), "sin saltos de un cuadro a otro");
                if (H.SimulatorLayer!.IsMoving)
                {
                    sawMoving = true;
                    Assert.That(H.WhyNotTappable("sim-open"), Is.Null, "se puede tocar mientras se mueve");
                }

                previous = now;
            }

            Assert.That(sawMoving, Is.True, "pestaña → pantalla apilada: con el resorte ω 22");
            Assert.That(WrenchBottom(), Is.EqualTo(16).Within(0.6f));
        }

        [UnityTest, Category("AC-BLE-16-02"), Category("AC-APP-05-08")]
        public IEnumerator ReducedMotionMovesTheWrenchAtOnce()
        {
            CreateSimWith(HostRoutines.Seeded(RoutineSeeds.Lista), true);
            yield return Tap("tab-routines");
            yield return Settle();
            yield return Frames();
            H.Tap("routine-card-0");
            var positions = new List<float>();
            for (var i = 0; i < 30; i++)
            {
                H.Step();
                yield return null;
                Assert.That(H.SimulatorLayer!.IsMoving && !H.SimulatorLayer.IsSwappingLane, Is.False, "sin resorte");
                var now = WrenchBottom();
                if (positions.Count == 0 || Math.Abs(positions[positions.Count - 1] - now) > 0.01f)
                {
                    positions.Add(now);
                }
            }

            Assert.That(WrenchBottom(), Is.EqualTo(16).Within(0.6f), "con reducir movimiento, en el acto");
            Assert.That(positions.Count, Is.LessThanOrEqualTo(2), "un solo salto, sin posiciones intermedias: " + string.Join(", ", positions));
        }

        [UnityTest, Category("AC-RUT-10-04"), Category("AC-BLE-16-03")]
        public IEnumerator PlusIsFreeWithTheWrenchOnTheLeft()
        {
            foreach (var width in new[] { 411, 368, 360 })
            {
                var profile = width == 411 ? DeviceProfile.TV : DeviceProfile.Dp(width, 800);
                CreateSimWith(HostRoutines.Seeded(RoutineSeeds.Lista), profile: profile);
                yield return Tap("tab-routines");
                yield return Settle();
                Assert.That(Overlap(H.BoundsOf("routines-add"), H.BoundsOf("sim-open")), Is.False, width + ": con el panel cerrado, la llave no toca el +");
                Assert.That(H.WhyNotTappable("routines-add"), Is.Null, width + ": el + se toca");
                yield return Tap("sim-open");
                var panel = H.BoundsOf("sim-panel");
                var add = H.BoundsOf("routines-add");
                if (width >= 368)
                {
                    Assert.That(Overlap(panel, add), Is.False, width + ": con el panel abierto, el + entero (desde 368 dp)");
                    Assert.That(H.WhyNotTappable("routines-add"), Is.Null, width + ": y se toca");
                }
                else
                {
                    Assert.That(panel.xMax - add.xMin, Is.EqualTo(8).Within(0.6f), "en 360 dp el panel tapa 8 dp de la izquierda del +");
                }

                yield return Tap("sim-close");
            }
        }

        [UnityTest, Category("AC-RUT-10-09")]
        public IEnumerator LastCardAbovePlusAndWrench()
        {
            CreateSimWith(HostRoutines.Seeded(RoutineSeeds.ListaPlus(26)), profile: DeviceProfile.L640);
            yield return Tap("tab-routines");
            H.ScrollToEnd("routines-list");
            yield return Frames(3);
            yield return Settle();
            var last = H.BoundsOf("routine-card-29");
            Assert.That(last.yMax, Is.LessThanOrEqualTo(H.BoundsOf("sim-open").yMin - 16 + 1), "la última tarjeta termina por encima de la llave");
            Assert.That(last.yMax, Is.LessThanOrEqualTo(H.BoundsOf("routines-add").yMin - 16 + 1), "y del +");
            Assert.That(Overlap(H.BoundsOf("sim-open"), H.BoundsOf("routines-add")), Is.False);
        }

        // En el entrenamiento, el lugar que calcula HudGeometry (04-design-notes §3.8, fila "Llave", ± 1).
        public static IEnumerable<TestCaseData> WorkoutSlots()
        {
            yield return Slot("TV", 355.0, 395.0, 575.9, 615.9);
            yield return Slot("P360", 304.0, 344.0, 497.9, 537.9);
            yield return Slot("TH", 618.9, 658.9, 344.6, 384.6);
            yield return Slot("L640", 414.8, 454.8, 245.5, 285.5);
            yield return Slot("W1280", 881.6, 921.6, 629.6, 669.6);
        }

        private static TestCaseData Slot(string profile, double x0, double x1, double y0, double y1)
        {
            return new TestCaseData(profile, x0, x1, y0, y1).Returns(null).SetName("WrenchInTheWorkoutSlot(" + profile + ")").SetCategory("AC-BLE-16-02");
        }

        private static DeviceProfile ProfileByCode(string code)
        {
            foreach (var profile in new[] { DeviceProfile.TV, DeviceProfile.P360, DeviceProfile.TH, DeviceProfile.L640, DeviceProfile.W1280 })
            {
                if (profile.Code == code)
                {
                    return profile;
                }
            }

            throw new ArgumentException(code);
        }

        [TestCaseSource(nameof(WorkoutSlots))]
        public IEnumerator WrenchInTheWorkoutSlot(string profile, double x0, double x1, double y0, double y1)
        {
            CreateSim(ProfileByCode(profile));
            yield return Settle();
            yield return StartFree();
            var box = InWindow("sim-open");
            Assert.That(box.xMin, Is.EqualTo((float)x0).Within(1f), profile + ": x");
            Assert.That(box.xMax, Is.EqualTo((float)x1).Within(1f));
            Assert.That(box.yMin, Is.EqualTo((float)y0).Within(1f), profile + ": y");
            Assert.That(box.yMax, Is.EqualTo((float)y1).Within(1f));
            Assert.That(H.SimulatorLayer!.Spot!.Value.RightLane, Is.True, "el lugar del HUD");
            Assert.That(H.SimulatorLayer.Spot.Value.Clear, Is.True);
            // No se mueve con los valores ni con BPM.
            yield return Advance(5);
            yield return Frames();
            Assert.That(InWindow("sim-open").position, Is.EqualTo(box.position), "quieta con los valores");
            Assert.That(H.WhyNotTappable("sim-open"), Is.Null);
        }

        [UnityTest, Category("AC-BLE-16-02")]
        public IEnumerator WrenchChangesLaneWithAFade()
        {
            CreateSimWith(HostRoutines.Seeded(RoutineSeeds.Lista));
            yield return Tap("tab-workout");
            yield return Settle();
            yield return Frames();
            var wrench = H.Find("sim-open")!;
            var lastPosition = wrench.layout.position;
            var lastOpacity = wrench.resolvedStyle.opacity;
            var jumps = 0;
            var sawFade = false;
            H.Tap("workout-free");
            for (var i = 0; i < 45; i++)
            {
                H.Step();
                yield return null;
                var position = wrench.layout.position;
                var opacity = wrench.resolvedStyle.opacity;
                sawFade |= opacity < 0.99f;
                if ((position - lastPosition).magnitude > 0.5f)
                {
                    jumps++;
                    Assert.That(lastOpacity, Is.LessThanOrEqualTo(0.01f), "cambia de lugar solo cuando no se ve (sin desplazarse): cuadro " + i + ", "
                                                                          + lastPosition + " → " + position + ", opacidad " + lastOpacity);
                }

                lastPosition = position;
                lastOpacity = opacity;
            }

            yield return Settle();
            Assert.That(sawFade, Is.True, "sale en 90 y entra en 160");
            Assert.That(jumps, Is.EqualTo(1), "un solo salto, invisible");
            Assert.That(wrench.resolvedStyle.opacity, Is.EqualTo(1).Within(0.001f));
            Assert.That(H.SimulatorLayer!.Spot!.Value.RightLane, Is.True);
        }

        [UnityTest, Category("AC-BLE-16-03")]
        public IEnumerator PanelGrowsFromTheWrench()
        {
            // Fuera del entrenamiento: desde la esquina de abajo a la izquierda, hacia arriba y a la derecha.
            CreateSim(DeviceProfile.TV);
            yield return Settle();
            yield return Frames();
            H.Tap("sim-open");
            yield return Frames();
            H.Step();
            H.Step();
            H.Step();
            yield return Frames();
            var window = H.Root.worldBound;
            var growing = H.BoundsOf("sim-panel");
            Assert.That(growing.width, Is.LessThan(279.5f), "todavía crece (0,92 → 1 en 240)");
            Assert.That(growing.xMin - window.xMin, Is.EqualTo(16).Within(0.6f), "su borde izquierdo, quieto");
            Assert.That(H.BoundsOf("tab-bar").yMin - growing.yMax, Is.EqualTo(16).Within(0.6f), "su borde de abajo, quieto");
            yield return Settle();
            yield return Tap("sim-close");

            // En el entrenamiento: desde la esquina de abajo a la derecha del lugar de la llave, hacia arriba y a la izquierda.
            yield return StartFree();
            var slot = H.Immersive!.Layout!.Wrench!.Value;
            H.Tap("sim-open");
            yield return Frames();
            H.Step();
            H.Step();
            H.Step();
            yield return Frames();
            growing = InWindow("sim-panel");
            Assert.That(growing.width, Is.LessThan(279.5f), "todavía crece");
            Assert.That(growing.xMax, Is.EqualTo((float)slot.Right).Within(0.6f), "su borde derecho, el de la llave");
            Assert.That(growing.yMax, Is.EqualTo((float)slot.Bottom).Within(0.6f), "su borde de abajo, el de la llave");
            Assert.That(H.SimulatorLayer!.PanelAnchor.Corner, Is.EqualTo(PanelCorner.BottomRight));
            yield return Settle();
            Assert.That(H.BoundsOf("sim-panel").width, Is.EqualTo(280).Within(0.6f));
        }

        [UnityTest, Category("AC-BLE-16-03")]
        public IEnumerator PanelKeepsSplitAndWattsInView()
        {
            CreateSim(DeviceProfile.TV);
            yield return Settle();
            yield return StartFree();
            yield return Tap("sim-open");
            var panel = H.BoundsOf("sim-panel");
            Assert.That(Overlap(panel, H.BoundsOf("hud-split")), Is.False, "Split a la vista");
            Assert.That(Overlap(panel, H.BoundsOf("hud-watts")), Is.False, "Watts a la vista");
            var layout = H.Immersive!.Layout!;
            Assert.That(layout.PanelCeiling.HasValue, Is.True, "en vertical, el techo del panel");
            Assert.That((float)layout.PanelCeiling!.Value, Is.EqualTo((float)(layout.WattsWithTargetBottom + 8)).Within(0.01f), "8 dp debajo de Watts con su objetivo");
            Assert.That(InWindow("sim-panel").yMin, Is.GreaterThanOrEqualTo((float)layout.PanelCeiling.Value - 0.5f));
            Assert.That(H.Find("sim-panel")!.resolvedStyle.maxHeight.value, Is.EqualTo(316f).Within(0.5f), "TV: 316 dp, con el contenido desplazable adentro");
            Assert.That(H.WhyNotTappable("sim-close"), Is.Null, "el botón de cerrar a la vista");
        }

        [UnityTest, Category("AC-BLE-16-02"), Category("AC-WKT-20-07")]
        public IEnumerator PanelFollowsTheWrench()
        {
            CreateSimWith(HostRoutines.Seeded(RoutineSeeds.F4A));
            yield return Tap("tab-workout");
            yield return Tap("sim-open");
            Assert.That(H.BoundsOf("sim-panel").xMin - H.Root.worldBound.xMin, Is.EqualTo(16).Within(0.6f), "abierto en el carril izquierdo");

            // Empieza un entrenamiento con el panel abierto: sigue abierto y pasa al lugar del HUD.
            yield return Tap("workout-free");
            yield return Settle();
            Assert.That(H.Immersive, Is.Not.Null);
            Assert.That(H.IsShown("sim-panel"), Is.True, "el panel sigue abierto");
            var slot = H.Immersive!.Layout!.Wrench!.Value;
            var panel = InWindow("sim-panel");
            Assert.That(panel.xMax, Is.EqualTo((float)slot.Right).Within(0.6f), "en el lugar nuevo de la llave");
            Assert.That(panel.yMax, Is.EqualTo((float)slot.Bottom).Within(0.6f));
            yield return Tap("hud-finish");
            // El panel abierto queda encima del diálogo (la capa del simulador está arriba de todo): Terminar sin hit-test.
            H.TapUnchecked("dialog-confirm");
            yield return Settle();
            Assert.That(H.Immersive, Is.Null);
            Assert.That(H.IsShown("sim-panel"), Is.True, "al salir, sigue abierto");
            Assert.That(H.BoundsOf("sim-panel").xMin - H.Root.worldBound.xMin, Is.EqualTo(16).Within(0.6f), "y vuelve al carril izquierdo");
            Assert.That(H.BoundsOf("tab-bar").yMin - H.BoundsOf("sim-panel").yMax, Is.EqualTo(16).Within(0.6f));
            yield return Tap("sim-close");

            // El inmersivo se cierra solo (fin de la rutina) con el panel abierto: el panel no queda en el lugar del HUD.
            CreateSimWith(HostRoutines.Seeded(RoutineSeeds.F4A));
            yield return Tap("tab-workout");
            yield return Frames();
            H.TapRoutineNamed("Corta");
            yield return Settle();
            yield return Tap("dialog-confirm");
            Assert.That(H.Immersive, Is.Not.Null);
            yield return Tap("sim-open");
            yield return Advance(31);
            yield return Settle();
            Assert.That(H.Immersive, Is.Null, "la rutina terminó y el inmersivo salió solo");
            Assert.That(H.IsShown("sim-panel"), Is.True, "el panel sigue abierto");
            Assert.That(H.BoundsOf("sim-panel").xMin - H.Root.worldBound.xMin, Is.EqualTo(16).Within(0.6f), "en el carril izquierdo");
            Assert.That(H.BoundsOf("tab-bar").yMin - H.BoundsOf("sim-panel").yMax, Is.EqualTo(16).Within(0.6f));
        }

        [UnityTest, Category("AC-BLE-16-02")]
        public IEnumerator MenusAvoidTheWrench()
        {
            // DD4a-02: PopupMenu trata la caja de la llave agrandada 8 dp como un borde.
            var window = new Rect(0, 0, 400, 800);
            var wrench = new Rect(16, 704, 40, 40);
            var crossing = new Rect(16, 690, 200, 100);
            var flippedMenu = PopupMenu.AvoidWrench(crossing, wrench, 500, window, 8, out var flipped);
            Assert.That(flipped, Is.True, "se abre del otro lado del ancla");
            Assert.That(flippedMenu.yMin, Is.EqualTo(500));
            var shifted = PopupMenu.AvoidWrench(crossing, wrench, 660, window, 8, out flipped);
            Assert.That(flipped, Is.False);
            Assert.That(shifted.xMin, Is.GreaterThanOrEqualTo(wrench.xMax + PopupMenu.WrenchMarginDp - 0.01f), "si tampoco entra, se corre en x");
            Assert.That(PopupMenu.AvoidWrench(new Rect(200, 100, 150, 100), wrench, 0, window, 8, out flipped), Is.EqualTo(new Rect(200, 100, 150, 100)),
                "lejos de la llave, no cambia");
            Assert.That(PopupMenu.AvoidWrench(crossing, null, 500, window, 8, out flipped), Is.EqualTo(crossing), "sin la llave a la vista, tampoco");

            // En pantalla: ningún ⋮ de Rutinas se abre sobre la llave agrandada.
            CreateSimWith(HostRoutines.Seeded(RoutineSeeds.Lista), profile: DeviceProfile.Dp(300, 640));
            yield return Tap("tab-routines");
            for (var i = 0; i < 4; i++)
            {
                if (H.WhyNotTappable("routine-menu-" + i) != null)
                {
                    continue;
                }

                yield return Tap("routine-menu-" + i);
                var menu = H.Root.Q(className: PopupMenu.UssClass);
                Assert.That(menu, Is.Not.Null, "el menú " + i);
                var fab = H.BoundsOf("sim-open");
                var grown = new Rect(fab.xMin - 8, fab.yMin - 8, fab.width + 16, fab.height + 16);
                Assert.That(Overlap(menu!.worldBound, grown), Is.False, "el menú " + i + " no se cruza con la llave agrandada 8");
                H.PressBack(RowMate.Presentation.Shell.BackSource.Escape);
                yield return Settle();
            }
        }

        [UnityTest, Category("AC-APP-05-07"), Category("AC-BLE-16-02")]
        public IEnumerator WrenchInTheImmersiveTabRing()
        {
            CreateSim(DeviceProfile.TV);
            yield return Settle();
            yield return StartFree();
            var expected = new[] { "hud-finish", "hud-pause", "sim-open", "hud-finish" };
            foreach (var name in expected)
            {
                H.PressTab();
                yield return Frames();
                Assert.That((H.Focused as VisualElement)?.name, Is.EqualTo(name), "Tab → " + name);
            }

            foreach (var name in new[] { "sim-open", "hud-pause", "hud-finish" })
            {
                H.PressTab(true);
                yield return Frames();
                Assert.That((H.Focused as VisualElement)?.name, Is.EqualTo(name), "Mayús+Tab → " + name);
            }
        }
    }
}
