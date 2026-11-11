using System.Collections;
using NUnit.Framework;
using RowMate.Presentation.Shell;
using RowMate.Simulator.Presentation;
using RowMate.Tests.Support;
using RowMate.Tests.UiSupport;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace RowMate.Tests.PlayMode.Simulator
{
    // It. 15 (#82, AC-D15-35, DT15-13): la cinta SIMULADOR (80 × 12 a −45°, centro en (24; 24) dp de la ventana) cubre la franja
    // x + y ∈ [39,5; 56,5]. Con .rm-has-sim-ribbon en la raíz del panel, la AppBar corre el título y la flecha a 44 dp: ninguna caja corta
    // la franja (todas sus esquinas con x + y > 56,5). En W1280, TV y L640. El panel del simulador es exento de la auditoría (AC-D15-26).
    public sealed class SimRibbonTitleTests : SimulatorUiTestBase
    {
        public const float BandMin = 39.5f;
        public const float BandMax = 56.5f;

        private static void AssertOutside(Rect box, string what)
        {
            var near = box.xMin + box.yMin;
            var far = box.xMax + box.yMax;
            Assert.That(near > BandMax || far < BandMin, Is.True, what + " corta la cinta: " + box);
        }

        [UnityTest, Category("AC-D15-35")]
        public IEnumerator TitlesClearTheRibbon([Values("W1280", "TV", "L640")] string code)
        {
            var profile = code == "W1280" ? DeviceProfile.W1280 : code == "TV" ? DeviceProfile.TV : DeviceProfile.L640;
            Create(new UiHostOptions
            {
                Profile = profile,
                IsDesktop = code != "TV",
                Backend = HostBackends.Simulator(false),
                SimulatorLayer = SimulatorLayerMode.Full,
                Routines = HostRoutines.Seeded(RoutineSeeds.Lista),
            });
            yield return Frames(3);
            Assert.That(H.Root.panel.visualTree.ClassListContains(SimulatorLayerView.HasRibbonClass), Is.True, "la clase en la raíz del panel");
            SimWpdGuards.UssHas(SimulatorLayerView.HasRibbonClass);
            foreach (var (tab, bar) in new[] { ("tab-routines", "routines-appbar"), ("tab-device", "device-appbar"), ("tab-history", "history-appbar"), ("tab-workout", "workout-appbar") })
            {
                H.Tap(tab);
                yield return Settle();
                var title = H.Find(bar)!.Q<Label>(className: "rm-appbar__title");
                Assert.That(title, Is.Not.Null, bar);
                AssertOutside(title!.worldBound, code + " " + bar);
            }

            H.Tap("tab-routines");
            yield return Settle();
            H.Tap("routine-card-0");
            yield return Settle();
            // La flecha = su glifo de 24 (AC-D15-35 habla de la flecha; la caja de toque de 48 puede pasar bajo la cinta, que no recibe
            // toques). WP-D: con 28 dp de relleno el glifo queda en x 44-68.
            AssertOutside(BackGlyph(), code + " la flecha del editor");
        }

        // El glifo de la flecha de volver del editor (no la caja de 48 del botón).
        private Rect BackGlyph()
        {
            var icon = H.Find("editor-back")!.Q(className: "rm-icon-button__icon");
            Assert.That(icon, Is.Not.Null, "el glifo de la flecha");
            return icon!.worldBound;
        }

        [Test, Category("AC-D15-26")]
        public void SimulatorPanelIsExempt()
        {
            Assert.That(TextAuditProbe.ExemptClass, Is.EqualTo("rm-audit-exempt"));
        }

    }
}
