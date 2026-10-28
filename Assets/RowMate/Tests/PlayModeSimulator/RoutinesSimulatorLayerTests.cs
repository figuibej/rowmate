using System.Collections;
using NUnit.Framework;
using RowMate.Presentation.Shell;
using RowMate.Tests.Support;
using RowMate.Tests.UiSupport;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace RowMate.Tests.PlayMode.Simulator
{
    // RSL · Rutinas con la capa del simulador (AC-APP-03-07, AC-BLE-16-01, AC-RUT-10-04, -09, AC-HIS-20-03, AC-APP-03-05;
    // 03-rutinas/02-tech-design §4.8; 04-design-notes §4.12-§4.13): la capa queda encima de todo, la cinta no recibe
    // toques, la llave sube sobre el + y ni la llave ni el aviso se pisan con nada.
    public sealed class RoutinesSimulatorLayerTests : SimulatorUiTestBase
    {
        private UiTestHost CreateWithRoutines(RoutineSeed seed, DeviceProfile? profile = null)
        {
            return Create(new UiHostOptions
            {
                Profile = profile ?? DeviceProfile.TV,
                Backend = HostBackends.Simulator(false),
                SimulatorLayer = SimulatorLayerMode.Full,
                Routines = HostRoutines.Seeded(seed),
            });
        }

        private static bool Overlap(Rect a, Rect b)
        {
            return a.xMin < b.xMax && b.xMin < a.xMax && a.yMin < b.yMax && b.yMin < a.yMax;
        }

        [UnityTest, Category("AC-APP-03-07"), Category("AC-BLE-16-01")]
        public IEnumerator SimulatorLayerAboveEverything()
        {
            CreateWithRoutines(RoutineSeeds.Lista);
            yield return Tap("tab-routines");
            yield return Tap("routine-menu-0");
            var root = H.Find("app-root")!;
            Assert.That(root.hierarchy[root.hierarchy.childCount - 1].name, Is.EqualTo("simulator-layer"), "la capa del simulador, el último hijo");
            Assert.That(root.hierarchy.IndexOf(H.Find("popup-layer")!), Is.EqualTo(root.hierarchy.childCount - 2), "el menú justo debajo");
            Assert.That(H.IsShown("sim-ribbon"), Is.True, "la cinta sigue a la vista con el menú abierto");
            yield return Tap("routine-menu-delete");
            Assert.That(H.TopDialog, Is.Not.Null);
            Assert.That(H.IsShown("sim-ribbon"), Is.True, "y con el diálogo");
            Assert.That(H.WhyNotTappable("sim-open"), Is.Null, "la llave se toca con el diálogo abierto");
        }

        [UnityTest, Category("AC-APP-03-07")]
        public IEnumerator BackArrowUnderTheRibbonWorks()
        {
            CreateWithRoutines(RoutineSeeds.Lista);
            yield return Tap("tab-routines");
            yield return Tap("routine-card-0");
            Assert.That(H.Editor, Is.Not.Null);
            var ribbon = H.BoundsOf("sim-ribbon");
            var back = H.BoundsOf("editor-back");
            Assert.That(Overlap(ribbon, back), Is.True, "la flecha queda en parte bajo la cinta");
            Assert.That(H.Find("sim-ribbon")!.pickingMode, Is.EqualTo(PickingMode.Ignore), "la cinta no recibe toques");
            yield return Tap("editor-back");
            Assert.That(H.Editor, Is.Null, "la flecha funciona");
        }

        [UnityTest, Category("AC-APP-03-07")]
        public IEnumerator PanelWorksWithADialogOpen()
        {
            CreateWithRoutines(RoutineSeeds.Lista);
            yield return Tap("tab-routines");
            yield return Tap("routine-card-0");
            yield return ScrollAndTap("series-repeat-0");
            Assert.That(H.TopDialog, Is.Not.Null);
            yield return Tap("sim-open");
            Assert.That(H.IsTextVisible("150 W"), Is.True, "el panel se abre encima del diálogo");
            yield return Tap("sim-watts-plus");
            Assert.That(Sim.Simulator.TargetWatts, Is.EqualTo(160));
            yield return Tap("sim-close");
            Assert.That(H.TopDialog, Is.Not.Null, "el diálogo sigue abierto");
        }

        [UnityTest, Category("AC-RUT-10-04")]
        public IEnumerator WrenchNeverCoversTheFab()
        {
            CreateWithRoutines(RoutineSeeds.Lista);
            yield return Tap("tab-routines");
            yield return Settle();
            var fab = H.BoundsOf("routines-add");
            var wrench = H.BoundsOf("sim-open");
            Assert.That(Overlap(fab, wrench), Is.False, "la llave no tapa el +");
            Assert.That(fab.yMin - wrench.yMax, Is.EqualTo(16).Within(0.6f), "16 dp sobre el +");
            Assert.That(H.Root.worldBound.yMax - wrench.yMax, Is.EqualTo(64 + 88).Within(0.6f), "barra + 88");
        }

        [UnityTest, Category("AC-RUT-10-04")]
        public IEnumerator FabTappableEverywhere()
        {
            foreach (var profile in new[] { DeviceProfile.TV, DeviceProfile.W1280, DeviceProfile.L640 })
            {
                CreateWithRoutines(RoutineSeeds.Lista, profile);
                yield return Tap("tab-routines");
                yield return Settle();
                Assert.That(H.WhyNotTappable("routines-add"), Is.Null, "el + se toca en " + profile.Code);
                Assert.That(H.WhyNotTappable("sim-open"), Is.Null, "y la llave también");
                yield return Tap("routines-add");
                Assert.That(H.Editor, Is.Not.Null, profile.Code);
            }
        }

        [UnityTest, Category("AC-RUT-10-09")]
        public IEnumerator LastCardEndsAboveTheWrench()
        {
            CreateWithRoutines(RoutineSeeds.ListaPlus(26));
            yield return Tap("tab-routines");
            H.ScrollToEnd("routines-list");
            yield return Frames(3);
            var last = H.Find("routine-card-29")!;
            var wrench = H.BoundsOf("sim-open");
            Assert.That(last.worldBound.yMax, Is.LessThanOrEqualTo(wrench.yMin - 16 + 1), "la última tarjeta termina por encima de la llave");
        }

        [UnityTest, Category("AC-HIS-20-03")]
        public IEnumerator SameScreensOnlyTheRibbon()
        {
            CreateWithRoutines(RoutineSeeds.Lista);
            yield return Tap("tab-routines");
            Assert.That(H.RoutinesScreen!.List.Count, Is.EqualTo(4), "la misma pantalla Rutinas");
            Assert.That(H.IsShown("routines-add"), Is.True);
            Assert.That(H.TextOf("sim-ribbon"), Is.EqualTo("SIMULADOR"), "lo único distinto: la cinta (y la llave)");
            Assert.That(H.Find("screen-routines")!.Q("sim-ribbon"), Is.Null, "la pantalla no sabe del simulador");
        }

        [UnityTest, Category("AC-APP-03-05")]
        public IEnumerator ToastAndWrenchDoNotOverlap()
        {
            foreach (var profile in new[] { DeviceProfile.TV, DeviceProfile.W1280 })
            {
                CreateWithRoutines(RoutineSeeds.Lista, profile);
                yield return Tap("tab-routines");
                H.Shell.Toasts.Show("Aviso de prueba");
                yield return Settle();
                var toast = H.Shell.Toasts.Toast.worldBound;
                Assert.That(Overlap(toast, H.BoundsOf("sim-open")), Is.False, "Rutinas " + profile.Code + ": el aviso no pisa la llave");
                Assert.That(Overlap(toast, H.BoundsOf("routines-add")), Is.False, "ni el +");
                if (profile.Code == "TV")
                {
                    Assert.That(H.Root.worldBound.yMax - toast.yMax, Is.EqualTo(200).Within(0.6f), "TV con la llave: 200 (04-design-notes §4.13)");
                }
                else
                {
                    Assert.That(H.Root.worldBound.yMax - toast.yMax, Is.EqualTo(80).Within(0.6f), "W1280: 80 (el + no se cruza)");
                }

                yield return Tap("routine-card-0");
                H.Shell.Toasts.Show("Aviso en el editor");
                yield return Settle();
                toast = H.Shell.Toasts.Toast.worldBound;
                Assert.That(Overlap(toast, H.BoundsOf("sim-open")), Is.False, "editor " + profile.Code + ": tampoco");
            }
        }
    }
}
