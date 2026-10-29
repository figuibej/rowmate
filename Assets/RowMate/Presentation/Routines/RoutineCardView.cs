using System;
using RowMate.App.Routines;
using RowMate.Core.Design;
using RowMate.Core.Localization;
using RowMate.Core.Models;
using RowMate.Presentation.Controls;
using RowMate.Presentation.Motion;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Routines
{
    // Tarjeta de una rutina (AC-RUT-10-03, -05, -06, -10, -11; legacy: routines_screen.dart:41-84; 04-design-notes §4.2):
    //   #routine-card-<n> .rm-routine-card                 enfocable; un toque o Enter/Espacio abre el editor
    //   ├── .rm-button__state, .rm-highlight              capas absolutas, sin eventos (DN3-06)
    //   ├── .rm-routine-card__icon > .rm-icon             caja 36 accent.tonalBg, fitness_center 22 accent.primary
    //   ├── .rm-routine-card__text
    //   │   ├── .rm-routine-card__name .rm-type-title-s          el nombre TAL CUAL (sin marcas ni escapes, pre-wrap)
    //   │   ├── .rm-routine-card__description .rm-type-body-xs   solo si no está vacía (tal cual)
    //   │   ├── .rm-routine-card__summary .rm-type-body-s        RoutineSummary.Text (la plantilla)
    //   │   └── StructureBarView
    //   ├── #routine-menu-<n> IconButton more_vert          ayuda menuShow; frena el toque (no abre el editor)
    //   └── .rm-focus-ring
    // n = posición en la lista (0 = la más nueva); RoutineListView lo renumera después de cada cambio.
    // It. 4a (AC-WKT-12-04, DV-21; DN4a-19; 02-tech-design §6.1): la misma tarjeta en Workout, con RoutineCardTrailing.Chevron:
    // en lugar del ⋮, .rm-routine-card__chevron, una caja de 48 × 48 en la misma columna de 28 dp del borde derecho (la columna
    // de texto mide igual que en Rutinas) con chevron_right 18 en text.tertiary, centrada en el alto de la tarjeta y sin
    // toques (la tarjeta entera es la acción). La nombra quien la usa (SetName: workout-routine-<n>).
    public enum RoutineCardTrailing
    {
        Menu,
        Chevron,
    }

    public sealed class RoutineCardView : VisualElement
    {
        public const string UssClass = "rm-routine-card";

        private readonly UiContext _ui;
        private readonly Action<Routine> _open;
        private readonly Action<Routine, VisualElement>? _menu;
        private readonly IconButton? _menuButton;
        private readonly Label _name;
        private readonly Label _description;
        private readonly Label _summary;
        private Routine _routine;

        public RoutineCardView(UiContext ui, Routine routine, Action<Routine> open, Action<Routine, VisualElement> menu)
            : this(ui, routine, open, menu ?? throw new ArgumentNullException(nameof(menu)), RoutineCardTrailing.Menu)
        {
        }

        // La tarjeta de Workout: un toque o Enter/Espacio la elige; sin menú.
        public RoutineCardView(UiContext ui, Routine routine, Action<Routine> open)
            : this(ui, routine, open, null, RoutineCardTrailing.Chevron)
        {
        }

        private RoutineCardView(UiContext ui, Routine routine, Action<Routine> open, Action<Routine, VisualElement>? menu, RoutineCardTrailing trailing)
        {
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            _routine = routine ?? throw new ArgumentNullException(nameof(routine));
            _open = open ?? throw new ArgumentNullException(nameof(open));
            _menu = menu;
            Trailing = trailing;
            AddToClassList(UssClass);
            focusable = true;
            Add(Controls.RmButton.StateLayer());
            Highlight = new VisualElement();
            Highlight.AddToClassList("rm-highlight");
            Highlight.pickingMode = PickingMode.Ignore;
            Highlight.style.backgroundColor = TokenColors.ToColor(Tokens.TextPrimary);
            Highlight.style.opacity = 0;
            Add(Highlight);

            var iconBox = new VisualElement();
            iconBox.AddToClassList(UssClass + "__icon");
            iconBox.pickingMode = PickingMode.Ignore;
            var icon = new IconView(Icons.FitnessCenter, false, 22);
            iconBox.Add(icon);
            Add(iconBox);

            var text = new VisualElement();
            text.AddToClassList(UssClass + "__text");
            text.pickingMode = PickingMode.Ignore;
            _name = UserText(UssClass + "__name", "rm-type-title-s");
            _description = UserText(UssClass + "__description", "rm-type-body-xs");
            _summary = new Label();
            _summary.AddToClassList(UssClass + "__summary");
            _summary.AddToClassList("rm-type-body-s");
            _summary.pickingMode = PickingMode.Ignore;
            Bar = new StructureBarView();
            text.Add(_name);
            text.Add(_description);
            text.Add(_summary);
            text.Add(Bar);
            Add(text);

            if (trailing == RoutineCardTrailing.Menu)
            {
                var menuButton = new IconButton("", Icons.MoreVert, () => _menu!(_routine, _menuButton!));
                _menuButton = menuButton;
                menuButton.AddToClassList(UssClass + "__menu");
                menuButton.SetTooltip(ui, () => _ui.Texts.Get(L10nKeys.MenuShow));
                // El ⋮ abre SOLO el menú: el toque no llega a la tarjeta (RLT.MenuButtonDoesNotOpenTheEditor).
                menuButton.RegisterCallback<PointerDownEvent>(evt => evt.StopPropagation());
                menuButton.RegisterCallback<PointerUpEvent>(evt => evt.StopPropagation());
                menuButton.RegisterCallback<ClickEvent>(evt => evt.StopPropagation());
                menuButton.RegisterCallback<NavigationSubmitEvent>(evt => evt.StopPropagation());
                Add(menuButton);
            }
            else
            {
                Chevron = new VisualElement();
                Chevron.AddToClassList(UssClass + "__chevron");
                Chevron.pickingMode = PickingMode.Ignore;
                var chevronIcon = new IconView(Icons.ChevronRight, false, 18);
                chevronIcon.pickingMode = PickingMode.Ignore;
                Chevron.Add(chevronIcon);
                Add(Chevron);
            }

            Add(Controls.RmButton.FocusRing());

            this.AddManipulator(new Clickable(Activate));
            RegisterCallback<NavigationSubmitEvent>(evt =>
            {
                evt.StopPropagation();
                Activate();
            });
            RegisterCallback<KeyDownEvent>(OnKeyDown);
            HighlightMotion = new HighlightMotion(ui.Motion, Highlight);
            Render(routine, ui.Texts);
        }

        public Routine Routine => _routine;

        public RoutineCardTrailing Trailing { get; }

        // El ⋮ (solo en Rutinas).
        public IconButton MenuButton => _menuButton ?? throw new InvalidOperationException("RoutineCardView: la tarjeta de Workout no tiene ⋮.");

        // La caja del chevron (solo en Workout).
        public VisualElement? Chevron { get; }

        public StructureBarView Bar { get; }

        public VisualElement Highlight { get; }

        public HighlightMotion HighlightMotion { get; }

        public Label NameLabel => _name;

        public Label DescriptionLabel => _description;

        public Label SummaryLabel => _summary;

        // n = posición en la lista; "" para la que se está yendo (los nombres de contrato son solo de las vivas).
        public void SetIndex(int index)
        {
            name = index >= 0 ? "routine-card-" + index : "";
            if (_menuButton != null)
            {
                _menuButton.name = index >= 0 ? "routine-menu-" + index : "";
            }
        }

        // El nombre de contrato que le pone quien la usa (Workout: workout-routine-<n>).
        public void SetName(string elementName)
        {
            if (name != elementName)
            {
                name = elementName;
            }
        }

        public void Render(Routine routine, Localizer texts)
        {
            _routine = routine;
            var model = RoutineCardModel.For(routine, texts);
            SetText(_name, model.Name);
            SetText(_description, model.Description);
            _description.style.display = model.ShowsDescription ? DisplayStyle.Flex : DisplayStyle.None;
            SetText(_summary, model.Summary);
            Bar.SetTemplate(model.Template);
        }

        private void Activate()
        {
            if (enabledInHierarchy && pickingMode != PickingMode.Ignore)
            {
                _open(_routine);
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

        // Texto del usuario tal cual (AC-RUT-10-03; DN3-22, QA-G3-10): sin texto enriquecido ni secuencias de escape.
        internal static Label UserText(string className, string typeClass)
        {
            var label = new Label();
            label.AddToClassList(className);
            label.AddToClassList(typeClass);
            label.AddToClassList("rm-user-text");
            label.pickingMode = PickingMode.Ignore;
            label.enableRichText = false;
            label.parseEscapeSequences = false;
            return label;
        }

        private static void SetText(Label label, string text)
        {
            var shown = DisplayText.ForLabel(text);
            if (label.text != shown)
            {
                label.text = shown;
            }
        }
    }
}
