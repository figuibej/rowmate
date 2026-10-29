using System;
using System.Collections.Generic;
using RowMate.Core.Models;
using RowMate.Presentation.Routines;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Workout
{
    // La lista de rutinas de Workout en reposo (AC-WKT-12-04, AC-RUT-10-11, DV-21; 02-tech-design §6.1 de la It. 4a):
    //   #workout-routines .rm-workout__list [--disabled] > #workout-routine-<n> .rm-routine-card (RoutineCardTrailing.Chevron)
    // Una tarjeta de Rutinas por rutina, en el orden de la colección compartida (AC-WKT-12-05), reusadas por Routine.Id, con el
    // chevron en lugar del ⋮; se toca entera. Los nombres workout-routine-<n> se renumeran después de cada cambio. Sin
    // animaciones de alta y baja: los cambios llegan desde otra pestaña, y la primera carga entra con la pestaña
    // (ANIMATION_BIBLE §2.3). Sin remo (AC-BLE-10-04): la lista al 50 % (opacidad por código, [CONTRATO]) y sin eventos
    // (pickingMode Ignore y focusable false en cada tarjeta).
    public sealed class WorkoutRoutineListView : VisualElement
    {
        public const string UssClass = "rm-workout__list";
        public const float DisabledOpacity = 0.5f;

        private readonly UiContext _ui;
        private readonly Action<Routine> _tap;
        private readonly Dictionary<long, RoutineCardView> _cards = new Dictionary<long, RoutineCardView>();
        private readonly List<RoutineCardView> _order = new List<RoutineCardView>();
        private bool _enabled = true;

        public WorkoutRoutineListView(UiContext ui, Action<Routine> tap)
        {
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            _tap = tap ?? throw new ArgumentNullException(nameof(tap));
            name = "workout-routines";
            AddToClassList(UssClass);
            pickingMode = PickingMode.Ignore;
        }

        public int Count => _order.Count;

        public bool IsEnabled => _enabled;

        public RoutineCardView? CardAt(int index)
        {
            return index >= 0 && index < _order.Count ? _order[index] : null;
        }

        // La colección (en su orden) y si las tarjetas responden (remo conectado y en reposo).
        public void Render(IReadOnlyList<Routine> routines, bool enabled)
        {
            var texts = _ui.Texts;
            var keep = new HashSet<long>();
            for (var i = 0; i < routines.Count; i++)
            {
                keep.Add(Key(routines[i], i));
            }

            for (var i = _order.Count - 1; i >= 0; i--)
            {
                var card = _order[i];
                var key = KeyOf(card);
                if (!keep.Contains(key))
                {
                    _cards.Remove(key);
                    card.RemoveFromHierarchy();
                }
            }

            _order.Clear();
            for (var i = 0; i < routines.Count; i++)
            {
                var routine = routines[i];
                var key = Key(routine, i);
                if (_cards.TryGetValue(key, out var card))
                {
                    if (!ReferenceEquals(card.Routine, routine))
                    {
                        card.Render(routine, texts);
                    }
                }
                else
                {
                    card = new RoutineCardView(_ui, routine, _tap);
                    _cards[key] = card;
                }

                if (card.parent != this)
                {
                    Insert(Math.Min(i, childCount), card);
                }
                else if (IndexOf(card) != i)
                {
                    card.RemoveFromHierarchy();
                    Insert(Math.Min(i, childCount), card);
                }

                card.SetName("workout-routine-" + i);
                _order.Add(card);
            }

            SetEnabled(enabled, true);
        }

        // Los textos del catálogo cambiaron de idioma (el resumen).
        public void RenderTexts()
        {
            var texts = _ui.Texts;
            for (var i = 0; i < _order.Count; i++)
            {
                _order[i].Render(_order[i].Routine, texts);
            }
        }

        private void SetEnabled(bool enabled, bool force)
        {
            if (!force && enabled == _enabled)
            {
                return;
            }

            _enabled = enabled;
            EnableInClassList(UssClass + "--disabled", !enabled);
            if (enabled)
            {
                style.opacity = StyleKeyword.Null;
            }
            else
            {
                style.opacity = DisabledOpacity;
            }
            for (var i = 0; i < _order.Count; i++)
            {
                var card = _order[i];
                card.pickingMode = enabled ? PickingMode.Position : PickingMode.Ignore;
                card.focusable = enabled;
                if (!enabled && card.panel?.focusController?.focusedElement == card)
                {
                    card.Blur();
                }
            }
        }

        private static long Key(Routine routine, int index)
        {
            return routine.Id ?? -1 - index;
        }

        private long KeyOf(RoutineCardView card)
        {
            foreach (var pair in _cards)
            {
                if (ReferenceEquals(pair.Value, card))
                {
                    return pair.Key;
                }
            }

            return long.MinValue;
        }
    }
}
