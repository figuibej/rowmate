using System;
using System.Collections.Generic;
using RowMate.App.History;
using RowMate.Core.Design;
using RowMate.Core.Models;
using RowMate.Core.Localization;
using RowMate.Presentation.Controls;
using RowMate.Presentation.Motion;
using RowMate.Presentation.Motion.History;
using UnityEngine.UIElements;

namespace RowMate.Presentation.History
{
    // La lista del historial (AC-HIS-06-03, AC-HIS-07-02, -06, -07; 02-tech-design §5.2 de la It. 9):
    //   #history-list (ScrollView, ScrollPolicy, FocusScroller) > .rm-history__list > .rm-slot > #history-card-<n>
    // Una columna a todo el ancho con margen 12 y 10 entre tarjetas (5 + 5 del legacy). Desvío del diseño (§11, riesgo previsto):
    // ScrollView con todas las tarjetas en lugar de ListView virtual: Tab y el anillo de foco funcionan solos, la entrada de la
    // nueva es el GrowMotion de Rutinas y el relleno inferior es el de ShellLayout. Con F-9P (200) se arma en tandas de
    // BatchSize tarjetas por cuadro (las primeras, en el acto).
    // Recarga sin parpadeo (Q-22): diferencia por id de sesión. Las que siguen se vuelven a enlazar EN SU LUGAR (solo cambian los
    // textos distintos; nada se anima); una nueva arriba CRECE (spring.smooth, fundido 60-260) si la lista está arriba de todo, o
    // entra sin animar y el desplazamiento suma su alto (lo que se ve no se mueve) si está desplazada. Con reducir movimiento,
    // entra sin animar. Si el detalle tapa la lista, la entrada espera a que la página termine de salir (PageStack.Settled). Las
    // que ya no están se quitan (esta iteración no borra sesiones). La primera carga con la pestaña a la vista escalona 35 ms
    // hasta 8 tarjetas; si llegó antes, entra con la transición de la pestaña.
    public sealed class HistoryListView : VisualElement
    {
        public const int BatchSize = 16;

        private readonly UiContext _ui;
        private readonly Action<HistoryCardView> _open;
        private readonly Dictionary<long, Item> _items = new Dictionary<long, Item>();
        private readonly List<Item> _order = new List<Item>();
        private readonly List<Item> _pendingGrow = new List<Item>();
        private readonly List<Item> _unbuilt = new List<Item>();
        private IVisualElementScheduledItem? _batch;
        private Item? _anchor;
        private float _anchorY;
        private float _anchorOffset;
        private bool _rendered;

        public HistoryListView(UiContext ui, Action<HistoryCardView> open)
        {
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            _open = open ?? throw new ArgumentNullException(nameof(open));
            AddToClassList("rm-history__list-host");
            pickingMode = PickingMode.Ignore;
            Scroll = new ScrollView(ScrollViewMode.Vertical) { name = "history-list" };
            Scroll.AddToClassList("rm-scroll");
            Scroll.AddToClassList("rm-history__scroll");
            ScrollPolicy.Apply(Scroll, ui.IsDesktop);
            Content = Scroll.contentContainer;
            Content.AddToClassList("rm-history__list");
            Scroller = new FocusScroller(ui, Scroll);
            Scroller.SetBottomInset(() => _ui.Layout.TabBarVisible ? _ui.Layout.CurrentTabBarHeight : 0);
            Add(Scroll);
            _ui.Pages.Settled += OnPagesSettled;
            Content.RegisterCallback<GeometryChangedEvent>(_ => FixAnchor());
            // It. 15 (#82, AC-D15-33, DT15-12): el ancla de desplazamiento SOLO ante un cambio de ancho (ScrollAnchor no repone con el ancho
            // fijo): el ancla de recarga (FixAnchor) sigue mandando y no compiten.
            Anchor = ScrollAnchor.Attach(Scroll);
            Scroll.RegisterCallback<WheelEvent>(_ => _anchor = null, TrickleDown.TrickleDown);
            Scroll.RegisterCallback<PointerDownEvent>(_ => _anchor = null, TrickleDown.TrickleDown);
        }

        public ScrollView Scroll { get; }

        public VisualElement Content { get; }

        public FocusScroller Scroller { get; }

        public ScrollAnchor Anchor { get; }

        public int Count => _order.Count;

        // Cuántas tarjetas se armaron (con F-9P, las tandas pendientes no cuentan).
        public int BuiltCount => _order.Count - _unbuilt.Count;

        // Veces que se creó una tarjeta (una recarga igual no crea ninguna: HistoryReloadTests).
        public int CreatedCount { get; private set; }

        public bool IsAnimating
        {
            get
            {
                for (var i = 0; i < _order.Count; i++)
                {
                    if (_order[i].Grow.IsRunning || _order[i].Enter.IsRunning)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public HistoryCardView? CardAt(int index)
        {
            return index >= 0 && index < _order.Count ? _order[index].Card : null;
        }

        // tabSelected: Historial es la pestaña elegida; onScreen: además ninguna página la tapa (escalonar la primera carga).
        public void Render(IReadOnlyList<HistoryEntry> entries, bool tabSelected, bool onScreen)
        {
            var texts = _ui.Texts;
            var first = !_rendered;
            _rendered = true;
            var keep = new HashSet<long>();
            for (var i = 0; i < entries.Count; i++)
            {
                keep.Add(Key(entries[i], i));
            }

            for (var i = _order.Count - 1; i >= 0; i--)
            {
                var item = _order[i];
                if (keep.Contains(item.Key))
                {
                    continue;
                }

                _order.RemoveAt(i);
                _items.Remove(item.Key);
                _pendingGrow.Remove(item);
                _unbuilt.Remove(item);
                item.Grow.Finish();
                item.Card.SetIndex(-1);
                item.Slot.RemoveFromHierarchy();
            }

            var covered = _ui.Pages.CoversShell || _ui.Pages.HasPages;
            var scrolled = Scroll.scrollOffset.y > 0.5f;
            var list = new List<Item>(entries.Count);
            var added = new List<Item>();
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                var key = Key(entry, i);
                if (!_items.TryGetValue(key, out var item))
                {
                    item = Create(key);
                    if (!first)
                    {
                        added.Add(item);
                    }
                }

                item.Entry = entry;
                list.Add(item);
            }

            // Desplazada: lo que se ve no se mueve (el ancla es la primera tarjeta a la vista; ver FixAnchor).
            _anchor = scrolled && added.Count > 0 ? FirstVisible() : null;
            _anchorY = _anchor != null ? _anchor.Slot.layout.y : 0;
            _anchorOffset = Scroll.scrollOffset.y;
            PlaceInOrder(list);
            _order.Clear();
            _order.AddRange(list);

            // Enlazar: las primeras en el acto; con muchas, el resto en tandas (solo la primera vez: después hay pocas nuevas).
            _unbuilt.Clear();
            for (var i = 0; i < _order.Count; i++)
            {
                var item = _order[i];
                item.Card.SetIndex(i);
                if (first && i >= BatchSize)
                {
                    _unbuilt.Add(item);
                    continue;
                }

                Bind(item, texts);
            }

            if (_unbuilt.Count > 0)
            {
                _batch?.Pause();
                _batch = schedule.Execute(BuildBatch).Every(0);
            }

            for (var i = 0; i < added.Count; i++)
            {
                var item = added[i];
                if (scrolled)
                {
                    // Entra sin animar: el ancla corrige el desplazamiento.
                }
                else if (!tabSelected || _ui.Motion.Settings.ReduceMotion || !_ui.Motion.Armed)
                {
                    // Fuera de la vista o con reducir movimiento: aparece en su lugar, sin animar.
                }
                else if (covered)
                {
                    item.Slot.style.height = 0;
                    item.Slot.style.overflow = Overflow.Hidden;
                    if (!_pendingGrow.Contains(item))
                    {
                        _pendingGrow.Add(item);
                    }
                }
                else
                {
                    HistoryMotion.PlayTopInsert(item.Grow);
                }
            }

            if (first && onScreen && _ui.Motion.Armed)
            {
                var count = HistoryMotion.FirstLoadCount(_order.Count);
                for (var i = 0; i < count; i++)
                {
                    HistoryMotion.PlayFirstLoadCard(_order[i].Enter, _order[i].Card, i);
                }
            }
        }

        // El idioma cambió en caliente (gate m-03): las tarjetas se vuelven a enlazar con los textos nuevos.
        public void RenderTexts()
        {
            var texts = _ui.Texts;
            for (var i = 0; i < _order.Count; i++)
            {
                if (!_unbuilt.Contains(_order[i]))
                {
                    Bind(_order[i], texts);
                }
            }
        }

        // Arma todas las tandas pendientes en el acto (las pruebas y las tomas).
        public void FinishBatches()
        {
            while (_unbuilt.Count > 0)
            {
                BuildBatch();
            }
        }

        private void BuildBatch()
        {
            var texts = _ui.Texts;
            var n = Math.Min(BatchSize, _unbuilt.Count);
            for (var i = 0; i < n; i++)
            {
                Bind(_unbuilt[i], texts);
            }

            _unbuilt.RemoveRange(0, n);
            if (_unbuilt.Count == 0)
            {
                _batch?.Pause();
                _batch = null;
            }
        }

        private static void Bind(Item item, Localizer texts)
        {
            if (item.Entry == null)
            {
                return;
            }

            // Lo mismo (por valor) con los mismos textos: no se toca nada (Q-22).
            if (item.BoundEntry != null && ReferenceEquals(item.BoundTexts, texts) && Equals(item.BoundEntry, item.Entry))
            {
                item.BoundEntry = item.Entry;
                item.Card.SetEntry(item.Entry);
                return;
            }

            item.BoundEntry = item.Entry;
            item.BoundTexts = texts;
            item.Card.Bind(item.Entry, HistoryCardModel.From(item.Entry, texts));
        }

        // Orden del árbol = orden de la lista. Una nueva sin anterior va arriba de todo.
        private void PlaceInOrder(List<Item> list)
        {
            VisualElement? previous = null;
            for (var i = 0; i < list.Count; i++)
            {
                var slot = list[i].Slot;
                if (slot.parent != Content)
                {
                    if (previous == null)
                    {
                        Content.Insert(0, slot);
                    }
                    else
                    {
                        Content.Insert(Content.IndexOf(previous) + 1, slot);
                    }
                }
                else if (previous != null && Content.IndexOf(slot) < Content.IndexOf(previous))
                {
                    slot.PlaceInFront(previous);
                }
                else if (previous == null && Content.IndexOf(slot) != 0)
                {
                    slot.SendToBack();
                }

                previous = slot;
            }
        }

        // La lista está desplazada y entra una nueva arriba (AC-HIS-06-03): la primera tarjeta a la vista es el ancla; cada vez que
        // el layout la corre (la nueva toma su alto, la FitBox ajusta el suyo), el desplazamiento suma lo mismo, así lo que se ve no
        // se mueve. El ancla se suelta con la rueda, un toque o la próxima recarga.
        private Item? FirstVisible()
        {
            var top = Scroll.scrollOffset.y;
            for (var i = 0; i < _order.Count; i++)
            {
                var layout = _order[i].Slot.layout;
                if (layout.yMax > top)
                {
                    return _order[i];
                }
            }

            return null;
        }

        private void FixAnchor()
        {
            var anchor = _anchor;
            if (anchor == null)
            {
                return;
            }

            if (anchor.Slot.panel == null)
            {
                _anchor = null;
                return;
            }

            // El desplazamiento que deja el ancla donde estaba (se vuelve a aplicar si la ScrollView lo recortó antes de crecer).
            var desired = _anchorOffset + (anchor.Slot.layout.y - _anchorY);
            var offset = Scroll.scrollOffset;
            if (Math.Abs(offset.y - desired) > 0.01f)
            {
                Scroll.scrollOffset = new UnityEngine.Vector2(offset.x, desired);
            }
        }

        private Item Create(long key)
        {
            var slot = new VisualElement();
            slot.AddToClassList("rm-slot");
            slot.AddToClassList("rm-history__slot");
            slot.pickingMode = PickingMode.Ignore;
            slot.RegisterCallback<GeometryChangedEvent>(_ => FixAnchor());
            var card = new HistoryCardView(_open, _ui);
            slot.Add(card);
            CreatedCount++;
            var item = new Item(key, slot, card, new GrowMotion(_ui.Motion, slot, card), new EnterMotion(_ui.Motion));
            _items[key] = item;
            return item;
        }

        private void OnPagesSettled()
        {
            if (_ui.Pages.HasPages)
            {
                return;
            }

            for (var i = 0; i < _pendingGrow.Count; i++)
            {
                HistoryMotion.PlayTopInsert(_pendingGrow[i].Grow);
            }

            _pendingGrow.Clear();
        }

        private static long Key(HistoryEntry entry, int index)
        {
            return entry.Session.Id ?? -1 - index;
        }

        private sealed class Item
        {
            public Item(long key, VisualElement slot, HistoryCardView card, GrowMotion grow, EnterMotion enter)
            {
                Key = key;
                Slot = slot;
                Card = card;
                Grow = grow;
                Enter = enter;
            }

            public long Key { get; }

            public VisualElement Slot { get; }

            public HistoryCardView Card { get; }

            public GrowMotion Grow { get; }

            public EnterMotion Enter { get; }

            public HistoryEntry? Entry { get; set; }

            public HistoryEntry? BoundEntry { get; set; }

            public Localizer? BoundTexts { get; set; }
        }
    }
}
