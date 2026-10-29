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

        // 04-design-notes §3.9: en el entrenamiento crece desde la esquina de abajo a la DERECHA de la llave, hacia arriba y a
        // la izquierda; en vertical hasta 8 dp debajo de Watts con su objetivo (TV 316,4; P360 274,6), en horizontal hasta
        // 16 dp debajo del margen superior (TH 368,6; L640 269,5; W1280 653,6).
        [TestCase(411, 914, 316.4, 115.0, Category = "AC-BLE-16-03")]
        [TestCase(360, 800, 274.6, 64.0, Category = "AC-BLE-16-03")]
        [TestCase(914, 411, 368.6, 378.9, Category = "AC-BLE-16-03")]
        [TestCase(640, 360, 269.5, 174.8, Category = "AC-BLE-16-03")]
        [TestCase(1280, 720, 653.6, 641.6, Category = "AC-BLE-16-03")]
        public void InTheWorkoutItGrowsUpAndLeftWithTheWattsCeiling(double width, double height, double maxHeight, double left)
        {
            var layout = HudGeometry.Layout(HudGeometryCases.Input(width, height, SafeInsets.None, true, HudGeometryCases.RoutineWithBpm));
            var surface = OverlaySurface.Workout(layout.Wrench!.Value, layout.PanelCeiling);
            var anchor = Panel(width, height, SafeInsets.None, surface);
            Assert.That(anchor.Corner, Is.EqualTo(PanelCorner.BottomRight));
            Assert.That(anchor.GrowsRight, Is.False);
            Assert.That(anchor.MaxHeight, Is.EqualTo(maxHeight).Within(0.5));
            Assert.That(anchor.Left, Is.EqualTo(left).Within(0.1));
            Assert.That(anchor.Right, Is.EqualTo(layout.Wrench.Value.Right).Within(1e-9), "la esquina de la llave");
            Assert.That(layout.PanelCeiling.HasValue, Is.EqualTo(height > width), "el techo de Watts solo en vertical");
            if (layout.PanelCeiling.HasValue)
            {
                Assert.That(layout.PanelCeiling.Value, Is.EqualTo(layout.WattsWithTargetBottom + 8).Within(1e-9));
            }
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

        private static PanelAnchor Panel(double width, double height, SafeInsets insets, OverlaySurface surface)
        {
            var wrench = WrenchPlacement.For(width, height, insets, surface);
            return WrenchPlacement.PanelFor(width, height, insets, wrench, surface);
        }
    }
}
