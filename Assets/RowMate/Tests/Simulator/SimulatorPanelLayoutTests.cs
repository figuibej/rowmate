using NUnit.Framework;
using RowMate.Core.Design;
using RowMate.Core.Ports;
using RowMate.Simulator.Panel;

namespace RowMate.Tests.Simulator
{
    // SPL: la llave del simulador con obstáculos (DV-13, DN3-14; AC-BLE-16-02 con el texto de la It. 3) y el panel que se
    // abre siempre en el mismo lugar (AC-BLE-16-03 sin cambios). legacy: lib/core/dev/simulator_overlay.dart:40-49.
    [TestFixture]
    public sealed class SimulatorPanelLayoutTests
    {
        private const double Bar = 64;

        // En Rutinas, sin página encima, la llave queda 16 dp por encima del + (barra + 88), con el borde derecho alineado;
        // en las otras pestañas y con el editor abierto (el + no es obstáculo), a 16 sobre la barra.
        [Test, Category("AC-BLE-16-02")]
        public void FabBottomAvoidsTheFab()
        {
            Assert.That(SimulatorPanelLayout.FabBottom(411, Bar, new[] { Plus(411) }), Is.EqualTo(Bar + 88), "TV, Rutinas");
            Assert.That(SimulatorPanelLayout.FabBottom(1280, Bar, new[] { Plus(1280) }), Is.EqualTo(Bar + 88), "W1280, Rutinas");
            Assert.That(SimulatorPanelLayout.FabBottom(411, Bar, null), Is.EqualTo(Bar + 16), "otra pestaña");
            Assert.That(SimulatorPanelLayout.FabBottom(411, Bar, new BottomObstacle[0]), Is.EqualTo(Bar + 16), "editor: el alto conservado de la barra");
            Assert.That(SimulatorPanelLayout.FabBottom(Bar), Is.EqualTo(80.0), "la forma sin obstáculos sigue igual");
            var withInsets = Bar + 48;
            Assert.That(SimulatorPanelLayout.FabBottom(411, withInsets, new[] { Plus(411, withInsets) }), Is.EqualTo(withInsets + 88), "con margen inferior");
            var fab = SimulatorPanelLayout.FabObstacle(411, Bar + 88);
            Assert.That(fab.Right, Is.EqualTo(Plus(411).Right), "bordes derechos alineados");
            Assert.That(fab.Left, Is.EqualTo(355));
            Assert.That(fab.Bottom - Plus(411).Top, Is.EqualTo(16), "16 dp de aire");
            Assert.That(SimulatorPanelLayout.FabSize, Is.EqualTo(40));
            Assert.That(SimulatorPanelLayout.ObstacleGapDp, Is.EqualTo(16));
        }

        // El panel no sigue a la llave: barra + 16 siempre; en Rutinas tapa el + (como el legacy) y su alto máximo sigue la
        // regla de AC-BLE-16-03.
        [Test, Category("AC-BLE-16-02"), Category("AC-BLE-16-03")]
        public void PanelBottomNeverMoves()
        {
            Assert.That(SimulatorPanelLayout.PanelBottom(Bar), Is.EqualTo(80));
            Assert.That(SimulatorPanelLayout.PanelBottom(Bar + 48), Is.EqualTo(128));
            Assert.That(SimulatorPanelLayout.PanelBottom(Bar), Is.EqualTo(SimulatorPanelLayout.FabBottom(411, Bar, null)));
            Assert.That(SimulatorPanelLayout.PanelBottom(Bar), Is.LessThan(SimulatorPanelLayout.FabBottom(411, Bar, new[] { Plus(411) })));
            Assert.That(SimulatorPanelLayout.MaxHeight(914, SafeInsets.None), Is.EqualTo(802.0), "AC-BLE-16-03 no cambia");
        }

        private static BottomObstacle Plus(double windowWidth, double bar = Bar)
        {
            return new BottomObstacle(windowWidth - 16 - 56, windowWidth - 16, bar + 16, 56);
        }
    }
}
