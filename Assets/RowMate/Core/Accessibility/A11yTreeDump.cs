using System;
using System.Collections.Generic;
using System.Text;

namespace RowMate.Core.Accessibility
{
    // El árbol del lector como texto (It. 14, DT14-22; CAP-27-a11y.txt): una línea por elemento en el orden de recorrido,
    // "{n}. {rol} · {nombre}[ · {valor}][ · [{estados}]]", con \n. Es el mismo modelo que alimenta la jerarquía de Unity, así que
    // las pruebas y las capturas comparan este texto.
    public static class A11yTreeDump
    {
        public static string Dump(IReadOnlyList<A11yItem> items)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            var builder = new StringBuilder();
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                builder.Append(i + 1).Append(". ").Append(item.Role).Append(" · ").Append(OneLine(item.Label));
                if (item.Value.Length > 0)
                {
                    builder.Append(" · ").Append(OneLine(item.Value));
                }

                if (item.State != A11yState.None)
                {
                    builder.Append(" · [").Append(States(item.State)).Append(']');
                }

                builder.Append('\n');
            }

            return builder.ToString();
        }

        private static string States(A11yState state)
        {
            var parts = new List<string>(3);
            if ((state & A11yState.Selected) != 0)
            {
                parts.Add("Selected");
            }

            if ((state & A11yState.Disabled) != 0)
            {
                parts.Add("Disabled");
            }

            if ((state & A11yState.Expanded) != 0)
            {
                parts.Add("Expanded");
            }

            return string.Join(", ", parts);
        }

        private static string OneLine(string text)
        {
            return text.Replace("\r", "").Replace('\n', ' ');
        }
    }
}
