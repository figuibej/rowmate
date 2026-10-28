using System.Collections.Generic;
using NUnit.Framework;
using RowMate.Core.Design;

namespace RowMate.Tests.EditMode.Design
{
    // BOT: la regla de los obstáculos inferiores (DN3-13, DN3-14) con las posiciones de 03-rutinas/04-design-notes §4.12 y
    // §4.13: barra de 64, aviso de 48, + de 56 a la derecha (right 16, barra + 16) y la llave de 40 (right 16).
    [TestFixture]
    public sealed class BottomObstaclesTests
    {
        private const double Bar = 64;
        private const double ToastHeight = 48;

        [Test, Category("AC-APP-03-05"), Category("AC-BLE-16-02"), Category("AC-RUT-10-09")]
        public void PositionsOfTheDesignNotes()
        {
            var tvPlus = Plus(411);
            var wrenchInEditor = Wrench(411, Bar + 16);
            var wrenchInRoutines = Wrench(411, Bar + 88);

            // Aviso en el editor (TV): la barra no se ve (base 16) y la llave (80-120) no se cruza: queda en 16.
            Assert.That(ToastTv(16, new BottomObstacle[0]), Is.EqualTo(16));
            Assert.That(ToastTv(16, new[] { wrenchInEditor }), Is.EqualTo(16), "64 + 8 < 80: no se cruza");

            // Aviso en Rutinas (TV): base barra + 16 = 80; cruza el + (80-136) → 144; con la llave encima (152-192) → 200.
            Assert.That(ToastTv(Bar + 16, new[] { tvPlus }), Is.EqualTo(144));
            Assert.That(ToastTv(Bar + 16, new[] { tvPlus, wrenchInRoutines }), Is.EqualTo(200));
            Assert.That(ToastTv(Bar + 16, new[] { wrenchInRoutines, tvPlus }), Is.EqualTo(200), "no depende del orden");

            // W1280: el aviso centrado de 344 (468-812) no se cruza con el + de la derecha: queda en 80.
            Assert.That(BottomObstacles.Lift(468, 812, Bar + 16, ToastHeight, new[] { Plus(1280), Wrench(1280, Bar + 88) }, MotionSpecs.BottomObstacleGapDp),
                Is.EqualTo(80));

            // La llave: sobre el + a barra + 88 (16 dp de aire, bordes derechos alineados); sin obstáculos, barra + 16.
            Assert.That(BottomObstacles.Lift(355, 395, Bar + 16, 40, new[] { tvPlus }, 16), Is.EqualTo(Bar + 88));
            Assert.That(BottomObstacles.Lift(355, 395, Bar + 16, 40, new BottomObstacle[0], 16), Is.EqualTo(Bar + 16));

            // Relleno inferior de la lista: barra + 88 = 152 con el +; con la llave en Rutinas, barra + 144 = 208.
            Assert.That(BottomObstacles.ClearanceAbove(0, 411, new[] { tvPlus }, 16, Bar + 16), Is.EqualTo(152));
            Assert.That(BottomObstacles.ClearanceAbove(0, 411, new[] { tvPlus, wrenchInRoutines }, 16, Bar + 16), Is.EqualTo(208));

            // Editor: con la llave (80-120) 136; sin obstáculos, el mínimo de 24.
            Assert.That(BottomObstacles.ClearanceAbove(0, 411, new[] { wrenchInEditor }, 16, 24), Is.EqualTo(136));
            Assert.That(BottomObstacles.ClearanceAbove(0, 411, new BottomObstacle[0], 16, 24), Is.EqualTo(24));
        }

        // Un obstáculo cuenta si SE CRUZA con el rectángulo agrandado; rozarse en el borde, o caer solo en su ancho, no.
        [Test, Category("AC-APP-03-05")]
        public void OnlyObstaclesThatCrossCount()
        {
            var o = new BottomObstacle(100, 140, 100, 40);
            Assert.That(o.Top, Is.EqualTo(140));
            Assert.That(BottomObstacles.Lift(0, 92, 100, 40, new[] { o }, 8), Is.EqualTo(100), "a 8 dp a la izquierda: roza, no cruza");
            Assert.That(BottomObstacles.Lift(0, 93, 100, 40, new[] { o }, 8), Is.EqualTo(148), "a 7 dp: cruza");
            Assert.That(BottomObstacles.Lift(100, 140, 148, 40, new[] { o }, 8), Is.EqualTo(148), "8 dp por encima: no cruza");
            Assert.That(BottomObstacles.Lift(100, 140, 0, 92, new[] { o }, 8), Is.EqualTo(0), "8 dp por debajo: no cruza");
            Assert.That(BottomObstacles.ClearanceAbove(200, 300, new[] { o }, 16, 10), Is.EqualTo(10), "fuera de su ancho");
            var chain = new List<BottomObstacle> { new BottomObstacle(0, 50, 0, 20), new BottomObstacle(0, 50, 24, 20), new BottomObstacle(0, 50, 48, 20) };
            Assert.That(BottomObstacles.Lift(0, 50, 0, 10, chain, 4), Is.EqualTo(72), "sube sobre cada uno, en cadena");
        }

        private static double ToastTv(double bottom, IReadOnlyList<BottomObstacle> obstacles)
        {
            return BottomObstacles.Lift(16, 395, bottom, ToastHeight, obstacles, MotionSpecs.BottomObstacleGapDp);
        }

        private static BottomObstacle Plus(double windowWidth)
        {
            return new BottomObstacle(windowWidth - 16 - 56, windowWidth - 16, Bar + 16, 56);
        }

        private static BottomObstacle Wrench(double windowWidth, double bottom)
        {
            return new BottomObstacle(windowWidth - 16 - 40, windowWidth - 16, bottom, 40);
        }
    }
}
