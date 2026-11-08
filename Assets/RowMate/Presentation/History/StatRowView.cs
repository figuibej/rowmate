using System.Collections.Generic;
using RowMate.App.History;
using RowMate.Presentation.Controls;
using UnityEngine.UIElements;

namespace RowMate.Presentation.History
{
    // La fila de números de la tarjeta del historial y del Resumen del detalle (AC-HIS-07-04, -06, AC-HIS-10-01; legacy
    // _StatColumn, history_screen.dart:109-152):
    //   .rm-stat-row                             5 o 6 columnas del mismo ancho (flex 1 1 0)
    //   └── .rm-stat > FitBox (valor) + FitBox (etiqueta): cada uno se achica hasta entrar (FittedBox del legacy)
    //       ├── .rm-stat__value .rm-type-num-stat   16/700 en el color BASE de la métrica (§10.2: el Tiempo en text.primary)
    //       └── .rm-stat__label .rm-type-caption    11/500 text.tertiary
    // Las columnas se reusan: Bind cambia solo los textos que cambiaron (una recarga con lo mismo no toca nada, Q-22).
    public sealed class StatRowView : VisualElement
    {
        public const string UssClass = "rm-stat-row";

        private readonly List<Column> _columns = new List<Column>();
        private readonly string _namePrefix;

        // namePrefix: el prefijo de los nombres de contrato de las columnas ("history-card-0-stat", "session-summary-stat").
        public StatRowView(string namePrefix)
        {
            _namePrefix = namePrefix;
            AddToClassList(UssClass);
            pickingMode = PickingMode.Ignore;
        }

        public int Count => CountVisible();

        public FitBox FitAt(int index)
        {
            return _columns[index].Box;
        }

        public Label ValueAt(int index)
        {
            return _columns[index].Value;
        }

        public Label LabelAt(int index)
        {
            return _columns[index].Label;
        }

        public void SetNamePrefix(string prefix)
        {
            for (var i = 0; i < _columns.Count; i++)
            {
                var name = prefix + "-" + i;
                if (_columns[i].Root.name != name)
                {
                    _columns[i].Root.name = name;
                }
            }
        }

        public void Bind(IReadOnlyList<HistoryCardColumn> columns)
        {
            while (_columns.Count < columns.Count)
            {
                _columns.Add(CreateColumn(_columns.Count));
            }

            for (var i = 0; i < _columns.Count; i++)
            {
                var column = _columns[i];
                if (i >= columns.Count)
                {
                    if (column.Root.style.display != DisplayStyle.None)
                    {
                        column.Root.style.display = DisplayStyle.None;
                    }

                    continue;
                }

                if (column.Root.style.display == DisplayStyle.None)
                {
                    column.Root.style.display = DisplayStyle.Flex;
                }

                var source = columns[i];
                if (column.Value.text != source.Value)
                {
                    column.Value.text = source.Value;
                }

                if (column.Label.text != source.Label)
                {
                    column.Label.text = source.Label;
                }

                if (!ReferenceEquals(column.Color, source.Color))
                {
                    column.Color = source.Color;
                    column.Value.style.color = TokenColors.ToColor(source.Color);
                }
            }
        }

        private int CountVisible()
        {
            var count = 0;
            for (var i = 0; i < _columns.Count; i++)
            {
                if (_columns[i].Root.style.display != DisplayStyle.None)
                {
                    count++;
                }
            }

            return count;
        }

        private Column CreateColumn(int index)
        {
            // El valor y la etiqueta en dos FitBox, cada una del alto natural de su texto y con el contenido centrado: un valor que
            // se achica queda en la misma línea media que los demás y su etiqueta, en su lugar (sin MotionDriver: en el acto).
            var root = new VisualElement { name = _namePrefix + "-" + index };
            root.AddToClassList("rm-stat");
            root.pickingMode = PickingMode.Ignore;
            var value = new Label();
            value.AddToClassList("rm-stat__value");
            value.AddToClassList("rm-type-num-stat");
            value.pickingMode = PickingMode.Ignore;
            value.enableRichText = false;
            var label = new Label();
            label.AddToClassList("rm-stat__label");
            label.AddToClassList("rm-type-caption");
            label.pickingMode = PickingMode.Ignore;
            label.enableRichText = false;
            var valueBox = Fit(value);
            var labelBox = Fit(label);
            labelBox.AddToClassList("rm-stat__label-box");
            root.Add(valueBox);
            root.Add(labelBox);
            Add(root);
            return new Column(root, valueBox, labelBox, value, label);
        }

        private static FitBox Fit(Label text)
        {
            var box = new FitBox(false, false, null);
            box.AddToClassList("rm-stat__box");
            box.Add(text);
            // La caja toma el alto natural del texto (sin escala: el contenido es absoluto y su layout no cambia con la escala).
            box.Content.RegisterCallback<GeometryChangedEvent>(evt =>
            {
                var height = evt.newRect.height;
                if (height > 0 && System.Math.Abs(box.resolvedStyle.height - height) > 0.05f)
                {
                    box.style.height = height;
                }
            });
            return box;
        }

        private sealed class Column
        {
            public Column(VisualElement root, FitBox box, FitBox labelBox, Label value, Label label)
            {
                Root = root;
                Box = box;
                LabelBox = labelBox;
                Value = value;
                Label = label;
            }

            public VisualElement Root { get; }

            // La del valor (FitAt).
            public FitBox Box { get; }

            public FitBox LabelBox { get; }

            public Label Value { get; }

            public Label Label { get; }

            public RowMate.Core.Design.ColorToken? Color { get; set; }
        }
    }
}
