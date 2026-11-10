using System;
using System.Collections.Generic;

namespace RowMate.Core.Accessibility
{
    // El orden de recorrido del lector (It. 14, AC-APP-03-12; 02-tech-design §5): orden visual, arriba-abajo e izquierda-derecha,
    // con filas que toleran 8 dp de diferencia en el borde de arriba (una fila de botones con alturas distintas se lee de
    // izquierda a derecha). Estable: a igual posición, el orden en que llegaron. Inserción sobre la misma lista (pocas decenas
    // de elementos; sin asignar).
    public static class A11yOrder
    {
        public const double RowToleranceDp = 8;

        public static void Sort(List<A11yItem> items)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            // 1) Por el borde de arriba (estable).
            for (var i = 1; i < items.Count; i++)
            {
                var current = items[i];
                var j = i - 1;
                while (j >= 0 && items[j].Y > current.Y)
                {
                    items[j + 1] = items[j];
                    j--;
                }

                items[j + 1] = current;
            }

            // 2) Cada fila (los que arrancan a ≤ 8 dp del primero de la fila), de izquierda a derecha (estable).
            var start = 0;
            while (start < items.Count)
            {
                var end = start + 1;
                while (end < items.Count && items[end].Y - items[start].Y <= RowToleranceDp)
                {
                    end++;
                }

                for (var i = start + 1; i < end; i++)
                {
                    var current = items[i];
                    var j = i - 1;
                    while (j >= start && items[j].X > current.X)
                    {
                        items[j + 1] = items[j];
                        j--;
                    }

                    items[j + 1] = current;
                }

                start = end;
            }
        }
    }
}
