using System;
using System.Collections.Generic;
using RowMate.Core.Design;
using RowMate.Core.Models;
using RowMate.Presentation.Controls;
using RowMate.Presentation.Motion;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Routines
{
    // La lista de Rutinas (AC-RUT-10-03, -09; 03-rutinas/02-tech-design §4.3; 04-design-notes §5.5):
    //   #routines-list (ScrollView, ScrollPolicy, FocusScroller) > .rm-routines__list > .rm-slot > #routine-card-<n>
    // Hace diff por Routine.Id: una tarjeta nueva CRECE (alto y margen con spring.smooth, GrowMotion), una borrada se
    // desvanece y colapsa (CollapseMotion; las de abajo suben sin salto, VID-02), una editada cambia sus textos en el lugar y
    // se ILUMINA. Si la lista cambia con el editor encima (la lista no se ve), el crecimiento y el resaltado esperan a que la
    // página termine de salir (PageStack.Settled). La primera carga escalona 35 ms por tarjeta, hasta 8, SOLO si la pestaña
    // está a la vista; si la lista llega antes (lo normal, AC-APP-01-09), entra con la transición de la pestaña.
    // El relleno inferior es el del obstáculo más alto que se cruza + 16 (ShellLayout.RegisterClearance, DN3-14).
    public sealed class RoutineListView : VisualElement
    {
        private readonly UiContext _ui;
        private readonly Action<Routine> _open;
        private readonly Action<Routine, VisualElement> _menu;
        private readonly Dictionary<long, Item> _items = new Dictionary<long, Item>();
        private readonly List<Item> _order = new List<Item>();
        private readonly List<Item> _pendingGrow = new List<Item>();
        private readonly List<Item> _pendingHighlight = new List<Item>();
        private bool _rendered;

        public RoutineListView(UiContext ui, Action<Routine> open, Action<Routine, VisualElement> menu)
        {
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            _open = open ?? throw new ArgumentNullException(nameof(open));
            _menu = menu ?? throw new ArgumentNullException(nameof(menu));
            pickingMode = PickingMode.Ignore;
            Scroll = new ScrollView(ScrollViewMode.Vertical) { name = "routines-list" };
            Scroll.AddToClassList("rm-scroll");
            Scroll.AddToClassList("rm-routines__scroll");
            ScrollPolicy.Apply(Scroll, ui.IsDesktop);
            Content = Scroll.contentContainer;
            Content.AddToClassList("rm-routines__list");
            Scroller = new FocusScroller(ui, Scroll);
            Scroller.SetBottomInset(() => _ui.Layout.TabBarVisible ? _ui.Layout.CurrentTabBarHeight : 0);
            Add(Scroll);
            AddToClassList("rm-routines__list-host");
            _ui.Pages.Settled += OnPagesSettled;
        }

        public ScrollView Scroll { get; }

        public VisualElement Content { get; }

        public FocusScroller Scroller { get; }

        public int Count => _order.Count;

        public RoutineCardView? CardAt(int index)
        {
            return index >= 0 && index < _order.Count ? _order[index].Card : null;
        }

        public bool IsAnimating
        {
            get
            {
                for (var i = 0; i < _order.Count; i++)
                {
                    if (_order[i].Grow.IsRunning)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        // tabVisible: Rutinas es la pestaña a la vista y ninguna página la tapa (escalonar la primera carga).
        public void Render(IReadOnlyList<Routine> routines, bool tabVisible)
        {
            var texts = _ui.Texts;
            var first = !_rendered;
            _rendered = true;
            var keep = new HashSet<long>();
            for (var i = 0; i < routines.Count; i++)
            {
                keep.Add(routines[i].Id ?? -1 - i);
            }

            // Bajas: se desvanecen y colapsan.
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
                _pendingHighlight.Remove(item);
                item.Card.SetIndex(-1);
                var removed = item;
                item.Grow.Finish();
                item.Collapse.Play(item.Slot, item.Card, () => removed.Slot.RemoveFromHierarchy());
            }

            var covered = _ui.Pages.CoversShell || _ui.Pages.HasPages;
            var list = new List<Item>(routines.Count);
            for (var i = 0; i < routines.Count; i++)
            {
                var routine = routines[i];
                var key = routine.Id ?? -1 - i;
                if (_items.TryGetValue(key, out var item))
                {
                    if (!ReferenceEquals(item.Card.Routine, routine))
                    {
                        item.Card.Render(routine, texts);
                        if (!first)
                        {
                            if (covered)
                            {
                                Enqueue(_pendingHighlight, item);
                            }
                            else
                            {
                                item.Card.HighlightMotion.Play();
                            }
                        }
                    }
                }
                else
                {
                    item = Create(routine, key);
                    if (!first)
                    {
                        if (covered)
                        {
                            item.Slot.style.height = 0;
                            item.Slot.style.overflow = Overflow.Hidden;
                            Enqueue(_pendingGrow, item);
                        }
                        else
                        {
                            item.Grow.Play();
                        }
                    }
                }

                list.Add(item);
            }

            // Orden del árbol = orden de la lista; las que colapsan quedan donde estaban hasta irse. Una nueva sin anterior va
            // arriba de todo (el alta va primera); una existente solo se mueve si quedó antes de la anterior viva.
            VisualElement? previous = null;
            for (var i = 0; i < list.Count; i++)
            {
                var item = list[i];
                var slot = item.Slot;
                if (slot.parent != Content)
                {
                    if (previous == null)
                    {
                        Content.Insert(0, slot);
                    }
                    else
                    {
                        Content.Add(slot);
                        slot.PlaceInFront(previous);
                    }
                }
                else if (previous != null && Content.IndexOf(slot) < Content.IndexOf(previous))
                {
                    slot.PlaceInFront(previous);
                }

                previous = slot;
                item.Card.SetIndex(i);
            }

            _order.Clear();
            _order.AddRange(list);
            if (first && tabVisible && _ui.Motion.Armed)
            {
                for (var i = 0; i < _order.Count; i++)
                {
                    var delay = MotionSpecs.ListStaggerMs * Math.Min(i, MotionSpecs.ListStaggerMax - 1);
                    _order[i].Enter.Play(_order[i].Card, delay, MotionSpecs.ListItemInMs, Tokens.ShiftM.Value);
                }
            }
        }

        public void RenderTexts()
        {
            var texts = _ui.Texts;
            for (var i = 0; i < _order.Count; i++)
            {
                _order[i].Card.Render(_order[i].Card.Routine, texts);
            }
        }

        private Item Create(Routine routine, long key)
        {
            var slot = new VisualElement();
            slot.AddToClassList("rm-slot");
            slot.pickingMode = PickingMode.Ignore;
            var card = new RoutineCardView(_ui, routine, _open, _menu);
            slot.Add(card);
            var item = new Item(key, slot, card, new GrowMotion(_ui.Motion, slot, card), new CollapseMotion(_ui.Motion), new EnterMotion(_ui.Motion));
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
                _pendingGrow[i].Grow.Play();
            }

            for (var i = 0; i < _pendingHighlight.Count; i++)
            {
                _pendingHighlight[i].Card.HighlightMotion.Play();
            }

            _pendingGrow.Clear();
            _pendingHighlight.Clear();
        }

        private static void Enqueue(List<Item> list, Item item)
        {
            if (!list.Contains(item))
            {
                list.Add(item);
            }
        }

        private sealed class Item
        {
            public Item(long key, VisualElement slot, RoutineCardView card, GrowMotion grow, CollapseMotion collapse, EnterMotion enter)
            {
                Key = key;
                Slot = slot;
                Card = card;
                Grow = grow;
                Collapse = collapse;
                Enter = enter;
            }

            public long Key { get; }

            public VisualElement Slot { get; }

            public RoutineCardView Card { get; }

            public GrowMotion Grow { get; }

            public CollapseMotion Collapse { get; }

            public EnterMotion Enter { get; }
        }
    }
}
