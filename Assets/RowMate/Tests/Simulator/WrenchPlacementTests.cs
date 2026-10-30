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

        // It. 4b (R10; DN4b-22; 04-design-notes §3.9): con el diálogo de completado abierto, si el lugar del HUD agrandado 8 se
        // cruza con el diálogo, la llave va al hueco libre más bajo del carril izquierdo (x 16-56), a 8 dp de las piezas del HUD y
        // del diálogo. W1280 no se cruza y queda en su lugar. A 1,3× es igual (el diálogo no llega al carril izquierdo).
        [TestCase("TV", 40.0, 298.0, 371.0, 616.0, 790.1, 830.1, Category = "AC-BLE-16-02")]
        [TestCase("P360", 40.0, 231.7, 320.0, 568.3, 685.5, 725.5, Category = "AC-BLE-16-02")]
        [TestCase("TH", 257.0, 63.2, 657.0, 347.8, 270.3, 310.3, Category = "AC-BLE-16-02")]
        [TestCase("L640", 120.0, 37.7, 520.0, 322.3, 230.8, 270.8, Category = "AC-BLE-16-02")]
        public void WrenchAvoidsTheCompletionDialog(string profile, double dx0, double dy0, double dx1, double dy1, double y0, double y1)
        {
            var p = HudGeometryCases.Profile(profile);
            var layout = HudGeometry.Layout(HudGeometryCases.Input(p[0], p[1], SafeInsets.None, true, HudGeometryCases.RoutineWithBpm));
            var dialog = DpRect.FromEdges(dx0, dy0, dx1, dy1);
            Assert.That(layout.Wrench!.Value.Intersects(dialog, 8), Is.True, "su lugar del HUD se cruza con el diálogo");
            var surface = OverlaySurface.Workout(layout.Wrench.Value, layout.PanelCeiling, layout.LeftLaneObstacles, dialog);
            var spot = WrenchPlacement.For(p[0], p[1], SafeInsets.None, surface);
            Assert.That(spot.Clear, Is.True);
            Assert.That(spot.RightLane, Is.False, "el carril izquierdo");
            AssertRect(spot.RectIn(p[1]), 16, y0, 56, y1, profile);
            var box = spot.RectIn(p[1]);
            foreach (var piece in layout.LeftLaneObstacles)
            {
                Assert.That(box.Intersects(piece, 8 - 1e-6), Is.False, profile + ": a 8 dp de toda pieza del HUD");
            }

            Assert.That(box.Intersects(dialog, 8 - 1e-6), Is.False, profile + ": y del diálogo");
            var panel = WrenchPlacement.PanelFor(p[0], p[1], SafeInsets.None, spot, surface);
            Assert.That(panel.GrowsRight, Is.True, "desde el carril izquierdo el panel crece hacia la derecha");
        }

        [Test, Category("AC-BLE-16-02")]
        public void WrenchStaysWhenTheDialogDoesNotCross()
        {
            var layout = HudGeometry.Layout(HudGeometryCases.Input(1280, 720, SafeInsets.None, true, HudGeometryCases.RoutineWithBpm));
            var dialog = DpRect.FromEdges(440.0, 209.7, 840.0, 510.3);
            var surface = OverlaySurface.Workout(layout.Wrench!.Value, layout.PanelCeiling, layout.LeftLaneObstacles, dialog);
            var spot = WrenchPlacement.For(1280, 720, SafeInsets.None, surface);
            Assert.That(spot.RightLane, Is.True);
            AssertRect(spot.RectIn(720), 881.6, 629.6, 921.6, 669.6, "W1280");
            var none = OverlaySurface.Workout(layout.Wrench.Value, layout.PanelCeiling);
            Assert.That(none.LeftLane, Is.Null, "sin diálogo, lo de la 4a");
            Assert.That(none.Dialog, Is.Null);
        }

        [Test, Category("AC-BLE-16-02")]
        public void WrenchSlotClearOfTheNewPieces()
        {
            // El lugar de la llave del HUD sigue libre de las piezas nuevas (la barra de 74·s y el disco del lugar centro alto) en los
            // cinco perfiles.
            foreach (var profile in HudGeometryCases.Profiles)
            {
                foreach (var values in HudGeometryCases.All)
                {
                    var layout = HudGeometry.Layout(HudGeometryCases.Input(profile.Value[0], profile.Value[1], SafeInsets.None, true, values));
                    var slot = layout.Wrench!.Value;
                    Assert.That(slot.Intersects(layout.TopBarMax, 8), Is.False, profile.Key + " " + values);
                    if (layout.BigCountFits)
                    {
                        Assert.That(slot.Intersects(layout.BigCount, 8), Is.False, profile.Key + " " + values);
                    }
                }
            }
        }

        [Test, Category("AC-BLE-16-02")]
        public void NoRoomFallsBackToTheCorner()
        {
            // Si no hay hueco (no pasa en los cinco perfiles), "16 + margen inferior" con Clear = false: la vista lo anota.
            var slot = DpRect.FromEdges(300, 300, 340, 340);
            var wall = new[] { DpRect.FromEdges(0, 0, 100, 400) };
            var surface = OverlaySurface.Workout(slot, null, wall, DpRect.FromEdges(0, 200, 400, 400));
            var spot = WrenchPlacement.For(400, 400, SafeInsets.None, surface);
            Assert.That(spot.Clear, Is.False);
            Assert.That(spot.Left, Is.EqualTo(16));
            Assert.That(spot.Bottom, Is.EqualTo(16));
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
