using System;
using RowMate.App.History;
using RowMate.Core.Design;
using RowMate.Core.Models;
using RowMate.Presentation.Controls;
using UnityEngine.UIElements;

namespace RowMate.Presentation.History
{
    // La tarjeta de una sesión (AC-HIS-07-03, -04, -07; legacy: _SessionCard, history_screen.dart:63-124):
    //   #history-card-<n> .rm-history-card              enfocable; un toque, Enter o Espacio abren el detalle (AC-HIS-07-07)
    //   ├── .rm-button__state                           capa de estado (hover 4 %, presionado 8 %: Skin.uss)
    //   ├── .rm-history-card__top
    //   │   ├── rowing 18 accent.primary (#00B4D8)
    //   │   ├── #history-card-<n>-name .rm-history-card__name   15/600, una línea con "…" (el nombre de la rutina o historyFree)
    //   │   ├── #history-card-<n>-date .rm-history-card__date   12 text.tertiary, "dd/MM/yyyy  HH:mm"
    //   │   └── chevron_right 16 blanco @24 %
    //   ├── StatRowView (12 debajo)                     #history-card-<n>-stat-<i>
    //   └── .rm-focus-ring
    // Sin el ícono de Strava (It. 12). n = posición en la lista (0 = la más nueva); HistoryListView la renumera.
    public sealed class HistoryCardView : VisualElement
    {
        public const string UssClass = "rm-history-card";

        private readonly Action<HistoryCardView> _open;
        private readonly Label _name;
        private readonly Label _date;

        public HistoryCardView(Action<HistoryCardView> open)
        {
            _open = open ?? throw new ArgumentNullException(nameof(open));
            AddToClassList(UssClass);
            focusable = true;
            Add(RmButton.StateLayer());

            var top = new VisualElement();
            top.AddToClassList(UssClass + "__top");
            top.pickingMode = PickingMode.Ignore;
            var icon = new IconView(Icons.Rowing, false, 18);
            icon.AddToClassList(UssClass + "__icon");
            icon.pickingMode = PickingMode.Ignore;
            top.Add(icon);
            _name = new Label();
            _name.AddToClassList(UssClass + "__name");
            _name.AddToClassList("rm-user-text");
            _name.pickingMode = PickingMode.Ignore;
            _name.enableRichText = false;
            _name.parseEscapeSequences = false;
            top.Add(_name);
            _date = new Label();
            _date.AddToClassList(UssClass + "__date");
            _date.AddToClassList("rm-type-body-xs");
            _date.pickingMode = PickingMode.Ignore;
            top.Add(_date);
            var chevron = new IconView(Icons.ChevronRight, false, 16);
            chevron.AddToClassList(UssClass + "__chevron");
            chevron.pickingMode = PickingMode.Ignore;
            top.Add(chevron);
            Add(top);

            Stats = new StatRowView("history-card-stat");
            Stats.AddToClassList(UssClass + "__stats");
            Add(Stats);
            Add(RmButton.FocusRing());

            this.AddManipulator(new Clickable(Activate));
            RegisterCallback<NavigationSubmitEvent>(evt =>
            {
                evt.StopPropagation();
                Activate();
            });
            RegisterCallback<KeyDownEvent>(OnKeyDown);
        }

        public HistoryCardModel? Model { get; private set; }

        public HistoryEntry? Entry { get; private set; }

        public int Index { get; private set; } = -1;

        public Label NameLabel => _name;

        public Label DateLabel => _date;

        public StatRowView Stats { get; }

        // n = posición en la lista; -1 para la que se va (los nombres de contrato son solo de las vivas).
        public void SetIndex(int index)
        {
            if (index == Index)
            {
                return;
            }

            Index = index;
            var prefix = index >= 0 ? "history-card-" + index : "";
            name = prefix;
            _name.name = index >= 0 ? prefix + "-name" : "";
            _date.name = index >= 0 ? prefix + "-date" : "";
            Stats.SetNamePrefix(index >= 0 ? prefix + "-stat" : "");
        }

        // La misma sesión (igual por valor) de una recarga: solo la referencia nueva.
        public void SetEntry(HistoryEntry entry)
        {
            Entry = entry;
        }

        // Cambia solo los textos que cambiaron (una recarga con lo mismo no toca nada, Q-22).
        public void Bind(HistoryEntry entry, HistoryCardModel model)
        {
            Entry = entry;
            Model = model;
            if (_name.text != model.Name)
            {
                _name.text = model.Name;
            }

            if (_date.text != model.Date)
            {
                _date.text = model.Date;
            }

            Stats.Bind(model.Columns);
        }

        private void Activate()
        {
            if (enabledInHierarchy && pickingMode != PickingMode.Ignore && Entry != null)
            {
                _open(this);
            }
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode != UnityEngine.KeyCode.Space || evt.target != this)
            {
                return;
            }

            evt.StopPropagation();
            Activate();
        }
    }
}
