using NUnit.Framework;
using RowMate.Core.Design;
using RowMate.Core.Ports;
using RowMate.Simulator.Panel;
using RowMate.Tests.Support;

namespace RowMate.Tests.Simulator
{
    // WPL: la regla de la llave de D-ART-10 (DD4a-01; 00-deuda-diseno §1.3 y §1.4 de la It. 4a; DV-39 en lugar de DV-13) y el
    // lugar del inmersivo de 04-design-notes §3.8 y §3.9 (DN4a-14), que WrenchPlacement usa tal cual. Rectángulos en dp de
    // ventana con y desde arriba, ± 0,1. Reemplaza a FabBottomAvoidsTheFab de la It. 3.
    [TestFixture]
    public sealed class WrenchPlacementTests
    {
        private const double Bar = 64;
        private const double Tolerance = 0.1;

        // 00-deuda-diseno §1.4: x 16-56 en las cuatro ventanas; en una pestaña, 16 sobre la barra (y 794-834 en TV…); en una
        // pantalla apilada, 16 sobre el borde de abajo (858-898 en TV…).
        [TestCase(411, 914, 794, 858, Category = "AC-BLE-16-02")]
        [TestCase(914, 411, 291, 355, Category = "AC-BLE-16-02")]
        [TestCase(640, 360, 240, 304, Category = "AC-BLE-16-02")]
        [TestCase(1280, 720, 600, 664, Category = "AC-BLE-16-02")]
        public void LeftLaneOutsideTheWorkout(double width, double height, double tabTop, double stackedTop)
        {
            var tab = WrenchPlacement.For(width, height, SafeInsets.None, OverlaySurface.Tab(Bar));
            Assert.That(tab.RightLane, Is.False);
            Assert.That(tab.Clear, Is.True);
            AssertRect(tab.RectIn(height), 16, tabTop, 56, tabTop + 40, "pestaña");
            var stacked = WrenchPlacement.For(width, height, SafeInsets.None, OverlaySurface.Stacked());
            Assert.That(stacked.RightLane, Is.False);
            AssertRect(stacked.RectIn(height), 16, stackedTop, 56, stackedTop + 40, "pantalla apilada");
        }

        [Test, Category("AC-BLE-16-02")]
        public void WithSafeInsets()
        {
            // Android con barra de estado y de gestos (24 / 48, D-26) y un recorte de 30 a la izquierda: el carril se corre por
            // el margen izquierdo; en la pestaña la barra ya trae su margen inferior; en la apilada lo suma la llave.
            var insets = new SafeInsets(30, 24, 0, 48);
            var tab = WrenchPlacement.For(411, 914, insets, OverlaySurface.Tab(Bar + 48));
            Assert.That(tab.Left, Is.EqualTo(46).Within(Tolerance));
            Assert.That(tab.Bottom, Is.EqualTo(Bar + 48 + 16).Within(Tolerance));
            var stacked = WrenchPlacement.For(411, 914, insets, OverlaySurface.Stacked());
            Assert.That(stacked.Left, Is.EqualTo(46).Within(Tolerance));
            Assert.That(stacked.Bottom, Is.EqualTo(16 + 48).Within(Tolerance));
            Assert.That(stacked.Clear, Is.True);
        }

        [Test, Category("AC-BLE-16-02")]
        public void TheFabIsNoLongerAnObstacleAndObstaclesAreAvoided()
        {
            // El + de Rutinas está en el carril derecho: no se cruza con la llave y la llave queda a barra + 16 (sale DV-13).
            var plus = new BottomObstacle(411 - 16 - 56, 411 - 16, Bar + 16, 56);
            var tab = WrenchPlacement.For(411, 914, SafeInsets.None, OverlaySurface.Tab(Bar, new[] { plus }));
            Assert.That(tab.Bottom, Is.EqualTo(Bar + 16));
            // Un obstáculo fijo en el carril izquierdo (R3): la llave sube 8 dp sobre él y nunca lo cruza agrandada 8.
            var low = new BottomObstacle(0, 120, Bar + 10, 30);
            var lifted = WrenchPlacement.For(411, 914, SafeInsets.None, OverlaySurface.Tab(Bar, new[] { low }));
            Assert.That(lifted.Bottom, Is.EqualTo(low.Top + 8));
            Assert.That(BottomObstacles.Lift(lifted.Left, lifted.Right, lifted.Bottom, 40, new[] { low }, 8), Is.EqualTo(lifted.Bottom));
            // Un obstáculo que la sube más allá de la ventana: ninguna altura cumple.
            var tall = new BottomObstacle(0, 120, 0, 900);
            Assert.That(WrenchPlacement.For(411, 914, SafeInsets.None, OverlaySurface.Tab(Bar, new[] { tall })).Clear, Is.False);
        }

        // 04-design-notes §3.8, fila "Llave" (DN4a-14): el lugar que calcula HudGeometry, devuelto tal cual, en el carril
        // derecho; Clear en los cinco perfiles.
        [TestCase(411, 914, 355.0, 575.9, 395.0, 615.9, Category = "AC-BLE-16-02")]
        [TestCase(360, 800, 304.0, 497.9, 344.0, 537.9, Category = "AC-BLE-16-02")]
        [TestCase(914, 411, 618.9, 344.6, 658.9, 384.6, Category = "AC-BLE-16-02")]
        [TestCase(640, 360, 414.8, 245.5, 454.8, 285.5, Category = "AC-BLE-16-02")]
        [TestCase(1280, 720, 881.6, 629.6, 921.6, 669.6, Category = "AC-BLE-16-02")]
        public void InTheWorkoutTheHudSlotIsUsedAsIs(double width, double height, double x0, double y0, double x1, double y1)
        {
            var slot = DpRect.FromEdges(x0, y0, x1, y1);
            var spot = WrenchPlacement.For(width, height, SafeInsets.None, OverlaySurface.Workout(slot, null));
            Assert.That(spot.RightLane, Is.True);
            Assert.That(spot.Clear, Is.True);
            AssertRect(spot.RectIn(height), x0, y0, x1, y1, "lugar del HUD");
            // Y coincide con lo que calcula HudGeometry para esa ventana (la parte de la llave de DN4a-14).
            var layout = HudGeometry.Layout(HudGeometryCases.Input(width, height, SafeInsets.None, true, HudGeometryCases.RoutineWithBpm));
            Assert.That(layout.Wrench.HasValue, Is.True);
            AssertRect(layout.Wrench!.Value, x0, y0, x1, y1, "HudGeometry");
        }

        [Test, Category("AC-BLE-16-02")]
        public void WorkoutSlotOutsideTheWindowIsNotClear()
        {
            var slot = DpRect.FromEdges(400, 10, 440, 50);
            Assert.That(WrenchPlacement.For(411, 914, SafeInsets.None, OverlaySurface.Workout(slot, null)).Clear, Is.False);
            var fallback = WrenchPlacement.For(411, 914, SafeInsets.None, new OverlaySurface(OverlayContextKind.Workout, 0,
                new BottomObstacle[0], null, null));
            Assert.That(fallback.RightLane, Is.True);
            Assert.That(fallback.Right, Is.EqualTo(411 - 16));
            Assert.That(fallback.Bottom, Is.EqualTo(16));
        }

        [Test]
        public void ConstantsOfTheRule()
        {
            Assert.That(WrenchPlacement.Size, Is.EqualTo(40));
            Assert.That(WrenchPlacement.EdgeMargin, Is.EqualTo(16));
            Assert.That(WrenchPlacement.ObstacleGap, Is.EqualTo(MotionSpecs.BottomObstacleGapDp), "el aire del aviso");
            Assert.That(HudGeometry.WrenchSize, Is.EqualTo(WrenchPlacement.Size));
            Assert.That(HudGeometry.WrenchEdge, Is.EqualTo(WrenchPlacement.EdgeMargin));
            Assert.That(HudGeometry.WrenchGap, Is.EqualTo(WrenchPlacement.ObstacleGap));
            var spot = new WrenchSpot(false, 16, 80, true);
            Assert.That(spot.Obstacle.Left, Is.EqualTo(16));
            Assert.That(spot.Obstacle.Top, Is.EqualTo(120));
        }

        private static void AssertRect(DpRect rect, double x0, double y0, double x1, double y1, string what)
        {
            Assert.That(rect.Left, Is.EqualTo(x0).Within(Tolerance), what + ": x0 de " + rect);
            Assert.That(rect.Top, Is.EqualTo(y0).Within(Tolerance), what + ": y0 de " + rect);
            Assert.That(rect.Right, Is.EqualTo(x1).Within(Tolerance), what + ": x1 de " + rect);
            Assert.That(rect.Bottom, Is.EqualTo(y1).Within(Tolerance), what + ": y1 de " + rect);
        }
    }
}
