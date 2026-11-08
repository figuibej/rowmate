using System.Collections.Generic;
using RowMate.Core.Design;
using RowMate.Core.History;
using RowMate.Presentation.Controls;
using UnityEngine.UIElements;

namespace RowMate.Presentation.History
{
    // Las filas del "Rendimiento por paso" (AC-HIS-11-01, -02; legacy session_detail_screen.dart:226-268; manda la spec sobre el
    // arte, §10.2 D9-05/D9-06):
    //   #session-step-<n> .rm-step-perf           blanco @4 %, r 8, relleno 10/8, 6 de aire con la siguiente
    //   ├── #session-step-<n>-badge .rm-step-perf__badge   28 × 28 r 6, #00B4D8 @16 %, el número (índice + 1) en #00B4D8 12/700
    //   └── .rm-step-perf__text
    //       ├── #session-step-<n>-line1   "{dur}  ·  {dist}m" 11 text.secondary
    //       └── .rm-step-perf__values      "{W}W", "{spm} spm", "{split}/500m" 12/600 blanco, separados por 12
    // Sin tipo, nombre, objetivo ni cumplimiento (paridad).
    public sealed class StepBreakdownView : VisualElement
    {
        private readonly List<Row> _rows = new List<Row>();

        public StepBreakdownView()
        {
            name = "session-steps-list";
            AddToClassList("rm-step-perf-rows");
            pickingMode = PickingMode.Ignore;
        }

        public int Count { get; private set; }

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

        public void Bind(IReadOnlyList<StepBreakdownRow> rows)
        {
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
        }

        private Row CreateRow(int index)
        {
            var prefix = "session-step-" + index;
            var root = new VisualElement { name = prefix };
            root.AddToClassList("rm-step-perf");
            // Las transparencias de un token van por código (ADR 016): fila blanca @4 %, insignia #00B4D8 @16 % (AC-HIS-11-02).
            root.style.backgroundColor = TokenColors.WithAlpha(Tokens.TextPrimary, 0.04);
            root.pickingMode = PickingMode.Ignore;
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
            Add(root);
            return new Row(root, badge, line1, watts, spm, split);
        }

        private static Label Value(string name)
        {
            var label = Text(name, "rm-step-perf__value");
            label.AddToClassList("rm-type-button-s");
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
