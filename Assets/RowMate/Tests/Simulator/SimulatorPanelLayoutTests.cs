using NUnit.Framework;
using RowMate.Core.Design;
using RowMate.Core.Ports;
using RowMate.Simulator.Panel;
using RowMate.Tests.Support;

namespace RowMate.Tests.Simulator
{
    // SPL: el panel del simulador anclado en la llave (DD4a-03; AC-BLE-16-03 con D-ART-10, DV-39): la esquina y el lado de
    // crecimiento, y el alto máximo medido desde su borde de abajo. legacy: lib/core/dev/simulator_overlay.dart:40-49, :83-95.
    [TestFixture]
    public sealed class SimulatorPanelLayoutTests
    {
        private const double Bar = 64;

        // En las pestañas, el alto de la It. 2 (sin margen seguro: 360 → 248, 914 → 802, 200 → 120); con una pantalla apilada
        // que tapa la barra, 64 más (312, 866, 152).
        [TestCase(360, 248, 312, Category = "AC-BLE-16-03")]
        [TestCase(914, 802, 866, Category = "AC-BLE-16-03")]
        [TestCase(200, 120, 152, Category = "AC-BLE-16-03")]
        public void MaxHeightFromThePanelBottom(double height, double tab, double stacked)
        {
            Assert.That(Panel(411, height, SafeInsets.None, OverlaySurface.Tab(Bar)).MaxHeight, Is.EqualTo(tab));
            Assert.That(Panel(411, height, SafeInsets.None, OverlaySurface.Stacked()).MaxHeight, Is.EqualTo(stacked));
            Assert.That(SimulatorPanelLayout.MaxHeight(height, SafeInsets.None), Is.EqualTo(tab), "la fórmula de la It. 2");
        }

        [Test, Category("AC-BLE-16-03")]
        public void WithSafeInsetsTheItTwoValuesStay()
        {
            // Android con 24 / 48 (D-26, QA-G2-06): 914 → 730 y el borde de arriba del panel en 24 + 32 = 56.
            var insets = new SafeInsets(0, 24, 0, 48);
            var anchor = Panel(411, 914, insets, OverlaySurface.Tab(Bar + 48));
            Assert.That(anchor.MaxHeight, Is.EqualTo(730));
            Assert.That(914 - anchor.Bottom - anchor.MaxHeight, Is.EqualTo(56));
            Assert.That(SimulatorPanelLayout.MaxHeight(914, insets), Is.EqualTo(730));
        }

        [Test, Category("AC-BLE-16-03")]
        public void OutsideTheWorkoutItGrowsUpAndRightFromTheWrench()
        {
            // Abajo a la izquierda, a la altura de la llave: en Rutinas termina en x 296 y deja el + entero desde 368 dp.
            var tab = Panel(411, 914, SafeInsets.None, OverlaySurface.Tab(Bar));
            Assert.That(tab.Corner, Is.EqualTo(PanelCorner.BottomLeft));
            Assert.That(tab.GrowsRight, Is.True);
            Assert.That(tab.Left, Is.EqualTo(16));
            Assert.That(tab.Bottom, Is.EqualTo(Bar + 16));
            Assert.That(tab.Right, Is.EqualTo(296));
            Assert.That(tab.Width, Is.EqualTo(280));
            Assert.That(368 - 16 - 56, Is.EqualTo(tab.Right), "el + empieza donde termina el panel en 368 dp");
            var stacked = Panel(640, 360, SafeInsets.None, OverlaySurface.Stacked());
            Assert.That(stacked.Bottom, Is.EqualTo(16));
            Assert.That(stacked.Left, Is.EqualTo(16));
        }

        // 04-design-notes §3.9 de la 4a: en el entrenamiento crece desde la esquina de abajo a la DERECHA de la llave, hacia
        // arriba y a la izquierda. It. 4b (DN4b-23; 04-design-notes §3.9): el techo en las dos orientaciones, 8 dp debajo del
        // disco del lugar centro alto en vertical (TV 298,0; P360 258,5; en la 4a 316,4 y 274,6) y 8 dp debajo de la barra de
        // 74·s en horizontal (TH 290,2; L640 201,8; W1280 530,4; en la 4a 368,6, 269,5 y 653,6).
        [TestCase(411, 914, 298.0, 115.0, Category = "AC-BLE-16-03")]
        [TestCase(360, 800, 258.5, 64.0, Category = "AC-BLE-16-03")]
        [TestCase(914, 411, 290.2, 378.9, Category = "AC-BLE-16-03")]
        [TestCase(640, 360, 201.8, 174.8, Category = "AC-BLE-16-03")]
        [TestCase(1280, 720, 530.4, 641.6, Category = "AC-BLE-16-03")]
        public void InTheWorkoutItGrowsUpAndLeftWithTheWattsCeiling(double width, double height, double maxHeight, double left)
        {
            var layout = HudGeometry.Layout(HudGeometryCases.Input(width, height, SafeInsets.None, true, HudGeometryCases.RoutineWithBpm));
            var surface = OverlaySurface.Workout(layout.Wrench!.Value, layout.PanelCeiling);
            var anchor = Panel(width, height, SafeInsets.None, surface);
            Assert.That(anchor.Corner, Is.EqualTo(PanelCorner.BottomRight));
            Assert.That(anchor.GrowsRight, Is.False);
            Assert.That(anchor.MaxHeight, Is.EqualTo(maxHeight).Within(0.1));
            Assert.That(anchor.Left, Is.EqualTo(left).Within(0.1));
            Assert.That(anchor.Right, Is.EqualTo(layout.Wrench.Value.Right).Within(1e-9), "la esquina de la llave");
            Assert.That(layout.PanelCeiling.HasValue, Is.True, "It. 4b: el techo en las dos orientaciones");
            var expected = height > width ? layout.BigCount.Bottom + 8 : layout.TopBarMax.Bottom + 8;
            Assert.That(layout.PanelCeiling!.Value, Is.EqualTo(expected).Within(1e-9));
        }

        [Test, Category("AC-BLE-16-03")]
        public void TheMinimumIsAlways120()
        {
            Assert.That(SimulatorPanelLayout.MaxHeight(150, SafeInsets.None, 100, 16, 140), Is.EqualTo(SimulatorPanelLayout.MinMaxHeight));
            Assert.That(SimulatorPanelLayout.MinMaxHeight, Is.EqualTo(120));
            Assert.That(SimulatorPanelLayout.Width, Is.EqualTo(280));
            Assert.That(SimulatorPanelLayout.AppTopReserve, Is.EqualTo(32));
            Assert.That(SimulatorPanelLayout.WorkoutTopReserve, Is.EqualTo(16));
        }

        [Test, Category("AC-BLE-16-03")]
        public void WorkoutCeilingFiveHeights()
        {
            // DN4b-23 (04-design-notes §3.9): los altos máximos del panel en el entrenamiento: TV 298,0 (era 316,4), P360 258,5
            // (274,6), TH 290,2 (368,6), L640 201,8 (269,5) y W1280 530,4 (653,6). Fuera del entrenamiento, sin cambios.
            var expected = new[] { ("TV", 298.0), ("P360", 258.5), ("TH", 290.2), ("L640", 201.8), ("W1280", 530.4) };
            foreach (var e in expected)
            {
                var p = HudGeometryCases.Profile(e.Item1);
                foreach (var values in new[] { HudGeometryCases.Free, HudGeometryCases.RoutineWithBpm, HudGeometryCases.PiramideWattsSplit })
                {
                    var layout = HudGeometry.Layout(HudGeometryCases.Input(p[0], p[1], SafeInsets.None, true, values));
                    var anchor = Panel(p[0], p[1], SafeInsets.None, OverlaySurface.Workout(layout.Wrench!.Value, layout.PanelCeiling));
                    Assert.That(anchor.MaxHeight, Is.EqualTo(e.Item2).Within(0.1), e.Item1 + " " + values + ": no cambia con el paso");
                }
            }

            Assert.That(SimulatorPanelLayout.MaxHeight(360, SafeInsets.None), Is.EqualTo(248), "fuera del entrenamiento, la It. 2");
        }

        [Test, Category("AC-BLE-16-02")]
        public void PanelFollowsTheWrenchWithTheFrameBottom()
        {
            // DD4b-04 (04b/00-deuda-diseno §3.2): el alto máximo se calcula en CADA CUADRO con el borde de abajo de ese cuadro; con un
            // contenido de 409 que no entra, en L640 el borde de arriba queda quieto en 32 mientras la llave baja de 80 a 16 (de una
            // pestaña a una pantalla apilada), y el alto termina en 312 (era 248). Nunca sale de la ventana ni salta.
            var wrenchBottoms = new[] { 80.00, 69.30, 52.40, 38.69, 20.24, 16.35, 16.01, 16.0 };
            foreach (var bottom in wrenchBottoms)
            {
                var top = SimulatorPanelLayout.FrameTop(409, 360, SafeInsets.None, bottom, SimulatorPanelLayout.AppTopReserve, null);
                Assert.That(top, Is.EqualTo(32).Within(0.5), "borde de abajo " + bottom);
            }

            Assert.That(SimulatorPanelLayout.FrameHeight(409, 360, SafeInsets.None, 80, SimulatorPanelLayout.AppTopReserve, null), Is.EqualTo(248));
            Assert.That(SimulatorPanelLayout.FrameHeight(409, 360, SafeInsets.None, 16, SimulatorPanelLayout.AppTopReserve, null), Is.EqualTo(312));
            // Si entra, se traslada entero: en TV (914) el borde de arriba sigue a la llave.
            Assert.That(SimulatorPanelLayout.FrameTop(409, 914, SafeInsets.None, 80, SimulatorPanelLayout.AppTopReserve, null), Is.EqualTo(425));
            Assert.That(SimulatorPanelLayout.FrameTop(409, 914, SafeInsets.None, 16, SimulatorPanelLayout.AppTopReserve, null), Is.EqualTo(489));
        }

        private static PanelAnchor Panel(double width, double height, SafeInsets insets, OverlaySurface surface)
        {
            var wrench = WrenchPlacement.For(width, height, insets, surface);
            return WrenchPlacement.PanelFor(width, height, insets, wrench, surface);
        }
    }
}
