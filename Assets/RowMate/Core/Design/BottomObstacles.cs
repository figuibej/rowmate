using System;
using System.Collections.Generic;

namespace RowMate.Core.Design
{
    // Un obstáculo cerca del borde inferior (el botón +, la llave del simulador), en dp: Left/Right en x desde el borde
    // izquierdo de la ventana y Bottom/Height en y medidos desde ABAJO (DN3-13, DN3-14).
    public readonly struct BottomObstacle
    {
        public BottomObstacle(double left, double right, double bottom, double height)
        {
            Left = left;
            Right = right;
            Bottom = bottom;
            Height = height;
        }

        public double Left { get; }

        public double Right { get; }

        public double Bottom { get; }

        public double Height { get; }

        // Borde superior, medido desde abajo.
        public double Top => Bottom + Height;
    }

    // Regla pura de los obstáculos inferiores (04-design-notes §4.12-§4.13). Un rectángulo cuenta un obstáculo solo si SE
    // CRUZA con él agrandado `gap` dp (no si el obstáculo solo cae en su ancho): en el editor el aviso queda abajo y la llave
    // (80-120) no lo toca. La usan el aviso (gap 8: MotionSpecs.BottomObstacleGapDp), la llave (gap 16:
    // SimulatorPanelLayout.ObstacleGapDp) y los rellenos inferiores de los contenidos desplazables.
    // Posiciones de las notas (barra 64, aviso de 48): editor TV 16; Rutinas TV 144 (por el +) y 200 (con la llave);
    // W1280 80 (el + no se cruza); llave a barra + 88 sobre el + y a barra + 16 sin obstáculos.
    public static class BottomObstacles
    {
        // Distancia desde abajo del rectángulo [left, right] × [bottom, bottom + height] después de apartarse: sube hasta
        // `gap` dp sobre cada obstáculo que se cruza con él agrandado y repite hasta no cruzar ninguno. Termina siempre:
        // cada vuelta sube el rectángulo por encima de un obstáculo, que ya no se vuelve a cruzar.
        public static double Lift(double left, double right, double bottom, double height, IReadOnlyList<BottomObstacle> obstacles, double gap)
        {
            if (obstacles == null)
            {
                throw new ArgumentNullException(nameof(obstacles));
            }

            var current = bottom;
            for (var round = 0; round <= obstacles.Count; round++)
            {
                var moved = false;
                for (var i = 0; i < obstacles.Count; i++)
                {
                    var o = obstacles[i];
                    if (Crosses(left, right, current, current + height, o, gap))
                    {
                        current = o.Top + gap;
                        moved = true;
                    }
                }

                if (!moved)
                {
                    break;
                }
            }

            return current;
        }

        // Relleno inferior de un contenido desplazable de ancho [left, right] (alto completo): el borde superior del obstáculo
        // más alto que se cruza con él en x, más `gap`; como mínimo `minimum`. Lista en TV: + → barra + 88 = 152; con la
        // llave en Rutinas, barra + 144 = 208. Editor: con la llave 136, sin obstáculos el mínimo (24).
        public static double ClearanceAbove(double left, double right, IReadOnlyList<BottomObstacle> obstacles, double gap, double minimum)
        {
            if (obstacles == null)
            {
                throw new ArgumentNullException(nameof(obstacles));
            }

            var result = minimum;
            for (var i = 0; i < obstacles.Count; i++)
            {
                var o = obstacles[i];
                if (o.Left < right && left < o.Right)
                {
                    result = Math.Max(result, o.Top + gap);
                }
            }

            return result;
        }

        // Cruce estricto: rozarse en un borde no cuenta.
        private static bool Crosses(double left, double right, double bottom, double top, BottomObstacle o, double gap)
        {
            return left - gap < o.Right && o.Left < right + gap && bottom - gap < o.Top && o.Bottom < top + gap;
        }
    }
}
