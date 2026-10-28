using System;
using System.Collections.Generic;
using System.Globalization;
using RowMate.App.Routines;
using RowMate.Core.Design;
using RowMate.Core.Localization;
using RowMate.Core.Routines;
using RowMate.Presentation.Motion;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Routines
{
    // La lista de pasos del editor (F-RUT-12, F-RUT-14; legacy _buildStepsList, routine_editor_screen.dart:275-445;
    // 03-rutinas/02-tech-design §4.4; 04-design-notes §5.6):
    //   #editor-steps .rm-editor__steps > .rm-slot > (#step-row-<i> paso suelto | #series-<k> bloque de serie)
    // Arma los bloques de RoutineEditorState.Layout (EditorLayout) y REUSA los elementos: las filas por EditorStep.Key y los
    // bloques por groupId (y su aparición). Lo nuevo crece (alto y márgenes con spring.smooth, fundido 60-260; la serie nueva
    // funde sus piezas en escalera: encabezado 0-200, fila 1 40-240, fila 2 80-280, pie y hojas 120-320); lo borrado se
    // desvanece y colapsa (0-120 / 80-280); si una serie se queda sin pasos, colapsa el bloque entero (AC-RUT-14-10). Lo
    // editado se ilumina (MarkEdited). Los name de contrato se renumeran después de cada cambio. La primera vez, sin animar.
    public sealed class EditorStepsView : VisualElement
    {
        private readonly UiContext _ui;
        private readonly Action<int> _onEdit;
        private readonly Action<int> _onDelete;
        private readonly Action<string> _onRepeat;
        private readonly Action<string> _onDeleteSeries;
        private readonly Action<string> _onAddToSeries;
        private readonly Dictionary<int, Row> _rows = new Dictionary<int, Row>();
        private readonly Dictionary<string, Block> _blocks = new Dictionary<string, Block>(StringComparer.Ordinal);
        private readonly List<int> _highlight = new List<int>();
        private bool _rendered;

        public EditorStepsView(UiContext ui, Action<int> onEdit, Action<int> onDelete, Action<string> onRepeat, Action<string> onDeleteSeries,
                               Action<string> onAddToSeries)
        {
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            _onEdit = onEdit;
            _onDelete = onDelete;
            _onRepeat = onRepeat;
            _onDeleteSeries = onDeleteSeries;
            _onAddToSeries = onAddToSeries;
            name = "editor-steps";
            AddToClassList("rm-editor__steps");
            pickingMode = PickingMode.Ignore;
        }

        // La última fila o bloque agregados (para llevarlos a la vista, DN3-18).
        public VisualElement? LastAdded { get; private set; }

        public bool IsAnimating
        {
            get
            {
                foreach (var row in _rows.Values)
                {
                    if (row.Grow.IsRunning || row.Collapse.IsRunning)
                    {
                        return true;
                    }
                }

                foreach (var block in _blocks.Values)
                {
                    if (block.Grow.IsRunning || block.Collapse.IsRunning)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        // La fila de la plantilla en el índice i (o null).
        public StepRowView? RowAt(int index)
        {
            foreach (var row in _rows.Values)
            {
                if (row.Index == index && row.Live)
                {
                    return row.View;
                }
            }

            return null;
        }

        public SeriesBlockView? BlockAt(int ordinal)
        {
            foreach (var block in _blocks.Values)
            {
                if (block.View.Ordinal == ordinal && block.Live)
                {
                    return block.View;
                }
            }

            return null;
        }

        // La fila de esa clave se ilumina en el próximo Render (lápiz → Confirmar).
        public void MarkEdited(int key)
        {
            if (!_highlight.Contains(key))
            {
                _highlight.Add(key);
            }
        }

        public void Render(RoutineEditorState state, Localizer texts)
        {
            var animate = _rendered && _ui.Motion.Armed;
            _rendered = true;
            LastAdded = null;
            var layout = state.Layout;
            var steps = state.Steps;
            var liveRows = new HashSet<int>();
            var liveBlocks = new HashSet<string>(StringComparer.Ordinal);
            var occurrences = new Dictionary<string, int>(StringComparer.Ordinal);
            var topOrder = new List<VisualElement>(layout.Count);
            Block? current = null;
            var blockOrder = new List<VisualElement>();

            for (var i = 0; i < layout.Count; i++)
            {
                var item = layout[i];
                switch (item.Kind)
                {
                    case EditorItemKind.LooseStep:
                    {
                        var row = EnsureRow(steps[item.StepIndex], item.StepIndex, false, texts, animate);
                        liveRows.Add(row.Key);
                        topOrder.Add(row.Slot);
                        break;
                    }

                    case EditorItemKind.SeriesHeader:
                    {
                        var gid = item.GroupId ?? "";
                        occurrences.TryGetValue(gid, out var occurrence);
                        occurrences[gid] = occurrence + 1;
                        var blockKey = gid + "#" + occurrence.ToString(CultureInfo.InvariantCulture);
                        current = EnsureBlock(blockKey, gid, item.SeriesOrdinal, item.RepeatCount, texts, animate);
                        liveBlocks.Add(blockKey);
                        topOrder.Add(current.Slot);
                        blockOrder = new List<VisualElement>();
                        break;
                    }

                    case EditorItemKind.SeriesStep:
                    {
                        var row = EnsureRow(steps[item.StepIndex], item.StepIndex, true, texts, animate && !(current?.Born ?? false));
                        liveRows.Add(row.Key);
                        if (current != null)
                        {
                            if (row.Slot.parent != current.View.Steps)
                            {
                                current.View.Steps.Add(row.Slot);
                            }

                            blockOrder.Add(row.Slot);
                        }

                        break;
                    }

                    case EditorItemKind.SeriesFooter:
                        if (current != null)
                        {
                            Reorder(current.View.Steps, blockOrder);
                            if (current.Born && animate)
                            {
                                // La serie nueva se despliega de arriba abajo (04-design-notes §5.6).
                                current.Grow.Stagger(current.View.Header, 0, 200);
                                for (var r = 0; r < blockOrder.Count; r++)
                                {
                                    var start = MotionSpecs.SeriesStaggerMs * (r + 1);
                                    current.Grow.Stagger(blockOrder[r], start, start + 200);
                                }

                                var last = MotionSpecs.SeriesStaggerMs * (blockOrder.Count + 1);
                                current.Grow.Stagger(current.View.Footer, last, last + 200);
                                current.Grow.Stagger(current.View.Stack, last, last + 200);
                                current.Grow.Play();
                                LastAdded = current.View;
                            }

                            current.Born = false;
                        }

                        current = null;
                        break;
                }
            }

            // Bajas: las filas que ya no están; si su bloque también se fue, colapsa el bloque entero.
            var goneRows = new List<Row>();
            foreach (var row in _rows.Values)
            {
                if (!liveRows.Contains(row.Key))
                {
                    goneRows.Add(row);
                }
            }

            var goneBlocks = new List<Block>();
            foreach (var pair in _blocks)
            {
                if (!liveBlocks.Contains(pair.Key))
                {
                    goneBlocks.Add(pair.Value);
                }
            }

            foreach (var block in goneBlocks)
            {
                _blocks.Remove(block.BlockKey);
                block.Live = false;
                block.View.SetOrdinal(-1);
                var removed = block;
                block.Grow.Finish();
                if (animate)
                {
                    block.Collapse.Play(block.Slot, block.View, () => removed.Slot.RemoveFromHierarchy());
                }
                else
                {
                    block.Slot.RemoveFromHierarchy();
                }
            }

            foreach (var row in goneRows)
            {
                _rows.Remove(row.Key);
                row.Live = false;
                row.View.SetIndex(-1);
                var inGoneBlock = false;
                foreach (var block in goneBlocks)
                {
                    if (block.View.Steps.Contains(row.Slot))
                    {
                        inGoneBlock = true;
                        break;
                    }
                }

                row.Grow.Finish();
                if (inGoneBlock)
                {
                    continue;
                }

                var removed = row;
                if (animate)
                {
                    row.Collapse.Play(row.Slot, row.View, () => removed.Slot.RemoveFromHierarchy());
                }
                else
                {
                    row.Slot.RemoveFromHierarchy();
                }
            }

            Reorder(this, topOrder);
            for (var i = 0; i < _highlight.Count; i++)
            {
                if (_rows.TryGetValue(_highlight[i], out var row))
                {
                    row.View.HighlightMotion.Play();
                }
            }

            _highlight.Clear();
        }

        public void RenderTexts(Localizer texts)
        {
            foreach (var row in _rows.Values)
            {
                if (row.View.Step != null)
                {
                    row.View.Render(row.View.Step, texts);
                }
            }

            foreach (var block in _blocks.Values)
            {
                block.View.RenderTexts(texts);
            }
        }

        private Row EnsureRow(EditorStep step, int index, bool inSeries, Localizer texts, bool animate)
        {
            if (!_rows.TryGetValue(step.Key, out var row))
            {
                var slot = Slot();
                Row? created = null;
                var view = new StepRowView(_ui, false, () => _onEdit(created!.Index), () => _onDelete(created!.Index));
                slot.Add(view);
                created = new Row(step.Key, slot, view, new GrowMotion(_ui.Motion, slot, view), new CollapseMotion(_ui.Motion));
                row = created;
                _rows[step.Key] = row;
                if (animate)
                {
                    row.Grow.Play();
                    LastAdded = view;
                }
            }

            row.Index = index;
            row.View.SetIndex(index);
            row.View.SetInSeries(inSeries);
            if (!ReferenceEquals(row.View.Step, step.Step))
            {
                row.View.Render(step.Step, texts);
            }

            return row;
        }

        private Block EnsureBlock(string blockKey, string groupId, int ordinal, int repeat, Localizer texts, bool animate)
        {
            if (!_blocks.TryGetValue(blockKey, out var block))
            {
                var slot = Slot();
                Block? created = null;
                var view = new SeriesBlockView(_ui, b => _onRepeat(b.GroupId), b => _onDeleteSeries(b.GroupId), b => _onAddToSeries(b.GroupId));
                slot.Add(view);
                created = new Block(blockKey, slot, view, new GrowMotion(_ui.Motion, slot, view), new CollapseMotion(_ui.Motion));
                created.Born = animate;
                block = created;
                _blocks[blockKey] = block;
            }

            block.View.GroupId = groupId;
            block.View.SetOrdinal(ordinal);
            block.View.SetRepeat(repeat, texts);
            return block;
        }

        private static VisualElement Slot()
        {
            var slot = new VisualElement();
            slot.AddToClassList("rm-slot");
            slot.pickingMode = PickingMode.Ignore;
            return slot;
        }

        // Deja los elementos de order en ese orden dentro de parent (los que colapsan quedan donde estaban).
        private static void Reorder(VisualElement parent, List<VisualElement> order)
        {
            VisualElement? previous = null;
            for (var i = 0; i < order.Count; i++)
            {
                var element = order[i];
                if (element.parent != parent)
                {
                    if (previous == null)
                    {
                        parent.Insert(0, element);
                    }
                    else
                    {
                        parent.Add(element);
                        element.PlaceInFront(previous);
                    }
                }
                else if (previous != null && parent.IndexOf(element) < parent.IndexOf(previous))
                {
                    element.PlaceInFront(previous);
                }

                previous = element;
            }
        }

        private sealed class Row
        {
            public Row(int key, VisualElement slot, StepRowView view, GrowMotion grow, CollapseMotion collapse)
            {
                Key = key;
                Slot = slot;
                View = view;
                Grow = grow;
                Collapse = collapse;
            }

            public int Key { get; }

            public VisualElement Slot { get; }

            public StepRowView View { get; }

            public GrowMotion Grow { get; }

            public CollapseMotion Collapse { get; }

            public int Index { get; set; }

            public bool Live { get; set; } = true;
        }

        private sealed class Block
        {
            public Block(string blockKey, VisualElement slot, SeriesBlockView view, GrowMotion grow, CollapseMotion collapse)
            {
                BlockKey = blockKey;
                Slot = slot;
                View = view;
                Grow = grow;
                Collapse = collapse;
            }

            public string BlockKey { get; }

            public VisualElement Slot { get; }

            public SeriesBlockView View { get; }

            public GrowMotion Grow { get; }

            public CollapseMotion Collapse { get; }

            public bool Born { get; set; }

            public bool Live { get; set; } = true;
        }
    }
}
