using System;
using System.Collections.Generic;
using RowMate.Core.Design;
using RowMate.Core.History;
using RowMate.Presentation.Controls;
using UnityEngine;
using UnityEngine.UIElements;

namespace RowMate.Presentation.History
{
    // Las filas del "Rendimiento por paso" (AC-HIS-11-01, -02; legacy session_detail_screen.dart:226-268; manda la spec sobre el
    // arte, §10.2 D9-05/D9-06):
    //   #session-step-<n> .rm-step-perf           blanco @4 %, r 8, relleno 10/8, 6 de aire con la siguiente
    //   ├── #session-step-<n>-badge .rm-step-perf__badge   28 × 28 r 6, #00B4D8 @16 %, el número (índice + 1) en #00B4D8 12/700
    //   └── .rm-step-perf__text
    //       ├── #session-step-<n>-line1   "{dur}  ·  {dist}m" 11 text.secondary
    //       └── .rm-step-perf__values      "{W}W", "{spm} spm", "{split}/500m" blanco, separados por 12
    // Sin tipo, nombre, objetivo ni cumplimiento (paridad).
    // It. 15 (#33, AC-D15-02, DV-146): cada fila se puede elegir (toque, o Tab + Enter/Espacio) y avisa con RowActivated; la marcada
    // lleva .rm-step-perf--selected y un borde de 1 dp en el color de su tipo (StepBandPalette), con el relleno 1 dp menor para que
    // el contenido no se mueva. El estado (qué fila) es del Detalle (DT15-02); acá solo se dibuja. Un anillo de foco por fila.
    // It. 14 (TY-01, AC-POL-02; 00-arte-pulido §3.2): las cifras de las dos líneas en RowMate Numeric SC (cifras tabulares de la fuente:
    // la línea 1 en 600 al tamaño de caption, los valores en num.xs 13/700) y los tres valores en COLUMNAS de ancho fijo por lista (el
    // máximo de la columna en todas las filas, alineados a la derecha): de una fila a otra las cifras y las unidades caen en el mismo
    // lugar. Sin elementos nuevos; el ancho se recalcula con un dato nuevo o un cambio de letra (GeometryChangedEvent), nunca por cuadro.
    public sealed class StepBreakdownView : VisualElement
    {
        private const int Columns = 3;

        private readonly List<Row> _rows = new List<Row>();
        private readonly float[] _columnWidths = new float[Columns];
        private IReadOnlyList<StepBreakdownRow> _source = Array.Empty<StepBreakdownRow>();

        public const string SelectedClass = "rm-step-perf--selected";

        // La fila (posición en la lista) que se tocó o se activó con el teclado.
        public event Action<int>? RowActivated;

        // La fila marcada (−1 ninguna).
        public int SelectedRow { get; private set; } = -1;

        public StepBreakdownView()
        {
            name = "session-steps-list";
            AddToClassList("rm-step-perf-rows");
            pickingMode = PickingMode.Ignore;
            RegisterCallback<GeometryChangedEvent>(_ => EqualizeColumns());
        }

        public int Count { get; private set; }

        // El ancho común de la columna c (0 W, 1 spm, 2 split), en dp (0 antes del primer layout).
        public float ColumnWidth(int column)
        {
            return _columnWidths[column];
        }

        public Label ValueAt(int index, int column)
        {
            var row = _rows[index];
            return column == 0 ? row.Watts : column == 1 ? row.Spm : row.Split;
        }

        public Label BadgeAt(int index)
        {
            return _rows[index].Badge;
        }

        public Label Line1At(int index)
        {
            return _rows[index].Line1;
        }

        // Los tres valores de la línea 2.
        public string[] Line2At(int index)
        {
            var row = _rows[index];
            return new[] { row.Watts.text, row.Spm.text, row.Split.text };
        }

        public VisualElement RowAt(int index)
        {
            return _rows[index].Root;
        }

        // El índice del paso de la fila (−1 fuera de rango).
        public int StepIndexAt(int row)
        {
            return row >= 0 && row < _source.Count ? _source[row].StepIndex : -1;
        }

        // La fila de un paso (−1 si no está).
        public int RowOfStep(int stepIndex)
        {
            for (var i = 0; i < _source.Count; i++)
            {
                if (_source[i].StepIndex == stepIndex)
                {
                    return i;
                }
            }

            return -1;
        }

        // Marca una fila (−1 ninguna): la clase (borde de 1 dp y relleno compensado, Skin.uss) y el borde en el color del tipo.
        public void SetSelected(int row)
        {
            if (row >= Count)
            {
                row = -1;
            }

            for (var i = 0; i < _rows.Count; i++)
            {
                var selected = i == row;
                var root = _rows[i].Root;
                if (root.ClassListContains(SelectedClass) == selected && (i != row || i != SelectedRow))
                {
                    continue;
                }

                root.EnableInClassList(SelectedClass, selected);
                if (selected)
                {
                    // El ancho del borde y el relleno que lo compensa son de la USS (.rm-step-perf--selected, Skin.uss); el color, del tipo.
                    var color = TokenColors.ToColor(StepBandPalette.For(_source[i].TypeName));
                    root.style.borderTopColor = color;
                    root.style.borderBottomColor = color;
                    root.style.borderLeftColor = color;
                    root.style.borderRightColor = color;
                }
                else
                {
                    root.style.borderTopColor = StyleKeyword.Null;
                    root.style.borderBottomColor = StyleKeyword.Null;
                    root.style.borderLeftColor = StyleKeyword.Null;
                    root.style.borderRightColor = StyleKeyword.Null;
                }
            }

            SelectedRow = row;
        }

        public void Bind(IReadOnlyList<StepBreakdownRow> rows)
        {
            _source = rows ?? throw new ArgumentNullException(nameof(rows));
            while (_rows.Count < rows.Count)
            {
                _rows.Add(CreateRow(_rows.Count));
            }

            for (var i = 0; i < _rows.Count; i++)
            {
                var row = _rows[i];
                if (i >= rows.Count)
                {
                    row.Root.style.display = DisplayStyle.None;
                    continue;
                }

                row.Root.style.display = DisplayStyle.Flex;
                // 6 de aire con la SIGUIENTE: la última no lo lleva (el relleno de la tarjeta queda en 16).
                row.Root.style.marginBottom = i == rows.Count - 1 ? new StyleLength(0f) : new StyleLength(StyleKeyword.Null);
                var source = rows[i];
                Set(row.Badge, source.Badge);
                Set(row.Line1, source.Line1);
                Set(row.Watts, source.Watts);
                Set(row.Spm, source.Spm);
                Set(row.Split, source.Split);
            }

            Count = rows.Count;
            if (SelectedRow >= Count)
            {
                SetSelected(-1);
            }

            EqualizeColumns();
        }

        // TY-01: cada columna toma el ancho de su valor más ancho en todas las filas visibles.
        private void EqualizeColumns()
        {
            if (panel == null || Count == 0)
            {
                return;
            }

            for (var c = 0; c < Columns; c++)
            {
                var max = 0f;
                for (var i = 0; i < Count; i++)
                {
                    var label = ValueAt(i, c);
                    var size = label.MeasureTextSize(label.text, 0, MeasureMode.Undefined, 0, MeasureMode.Undefined);
                    if (size.x > max)
                    {
                        max = size.x;
                    }
                }

                max = Mathf.Ceil(max);
                if (!(max > 0) || Math.Abs(max - _columnWidths[c]) < 0.5f)
                {
                    continue;
                }

                _columnWidths[c] = max;
                for (var i = 0; i < _rows.Count; i++)
                {
                    ValueAt(i, c).style.minWidth = max;
                }
            }
        }

        private Row CreateRow(int index)
        {
            var prefix = "session-step-" + index;
            var root = new VisualElement { name = prefix };
            root.AddToClassList("rm-step-perf");
            // Las transparencias de un token van por código (ADR 016): fila blanca @4 %, insignia #00B4D8 @16 % (AC-HIS-11-02).
            root.style.backgroundColor = TokenColors.WithAlpha(Tokens.TextPrimary, 0.04);
            // It. 15: la fila se elige (toque, Enter o Espacio) y se enfoca con Tab.
            root.pickingMode = PickingMode.Position;
            root.focusable = true;
            var at = index;
            root.AddManipulator(new Clickable(() => RowActivated?.Invoke(at)));
            root.RegisterCallback<NavigationSubmitEvent>(evt =>
            {
                if (evt.target != root)
                {
                    return;
                }

                evt.StopPropagation();
                RowActivated?.Invoke(at);
            });
            root.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode != KeyCode.Space || evt.target != root)
                {
                    return;
                }

                evt.StopPropagation();
                RowActivated?.Invoke(at);
            });
            var badgeBox = new VisualElement { name = prefix + "-badge" };
            badgeBox.AddToClassList("rm-step-perf__badge");
            badgeBox.style.backgroundColor = TokenColors.WithAlpha(Tokens.AccentPrimary, 0.16);
            badgeBox.pickingMode = PickingMode.Ignore;
            var badge = Text(prefix + "-number", "rm-step-perf__number");
            badgeBox.Add(badge);
            root.Add(badgeBox);
            var text = new VisualElement();
            text.AddToClassList("rm-step-perf__text");
            text.pickingMode = PickingMode.Ignore;
            var line1 = Text(prefix + "-line1", "rm-step-perf__line1");
            line1.AddToClassList("rm-type-caption");
            line1.AddToClassList("rm-step-perf__line1--tabular");
            text.Add(line1);
            var values = new VisualElement();
            values.AddToClassList("rm-step-perf__values");
            values.pickingMode = PickingMode.Ignore;
            var watts = Value(prefix + "-watts");
            var spm = Value(prefix + "-spm");
            var split = Value(prefix + "-split");
            values.Add(watts);
            values.Add(spm);
            values.Add(split);
            text.Add(values);
            root.Add(text);
            root.Add(RmButton.FocusRing());
            Add(root);
            return new Row(root, badge, line1, watts, spm, split);
        }

        private static Label Value(string name)
        {
            var label = Text(name, "rm-step-perf__value");
            label.AddToClassList("rm-type-num-xs");
            return label;
        }

        private static Label Text(string name, string className)
        {
            var label = new Label { name = name };
            label.AddToClassList(className);
            label.pickingMode = PickingMode.Ignore;
            label.enableRichText = false;
            return label;
        }

        private static void Set(Label label, string text)
        {
            if (label.text != text)
            {
                label.text = text;
            }
        }

        private sealed class Row
        {
            public Row(VisualElement root, Label badge, Label line1, Label watts, Label spm, Label split)
            {
                Root = root;
                Badge = badge;
                Line1 = line1;
                Watts = watts;
                Spm = spm;
                Split = split;
            }

            public VisualElement Root { get; }

            public Label Badge { get; }

            public Label Line1 { get; }

            public Label Watts { get; }

            public Label Spm { get; }

            public Label Split { get; }
        }
    }
}
