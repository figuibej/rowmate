using System;

namespace RowMate.Core.Design
{
    // Los totales del diálogo de completado que cuentan de 0 a su valor exacto (ANIMATION_BIBLE §2.5; 02-tech-design §3.9 de la
    // It. 4b; 04-design-notes §4.12): ⌊final·ease.enter(u)⌋ en 700 ms desde su comienzo (220, 340 y 460 desde que entra el
    // diálogo, escalonados 120), y SIEMPRE el valor exacto al final. Son los únicos números que cuentan. Con reducir
    // movimiento, el valor final desde el primer cuadro. Pura y sin asignaciones.
    public static class CountUp
    {
        // El valor que se ve a los tMs (ms enteros desde que entra el diálogo) de un total que empieza a contar en startMs.
        public static long ValueAt(long final, long tMs, long startMs, bool reduced = false)
        {
            if (reduced || final == 0 || tMs >= startMs + (long)MotionSpecs.TotalsCountMs)
            {
                return final;
            }

            if (tMs <= startMs)
            {
                return 0;
            }

            var u = (tMs - startMs) / MotionSpecs.TotalsCountMs;
            var value = (long)Math.Floor(final * Easing.Evaluate(Tokens.EaseEnter, u));
            if (final > 0)
            {
                return value < 0 ? 0 : value > final ? final : value;
            }

            return value > 0 ? 0 : value < final ? final : value;
        }

        // El comienzo del total i (0 = tiempo, 1 = distancia, 2 = calorías), en ms desde que entra el diálogo.
        public static long StartMs(int index)
        {
            return (long)(MotionSpecs.TotalsStartMs + index * MotionSpecs.TotalsStaggerMs);
        }
    }
}
