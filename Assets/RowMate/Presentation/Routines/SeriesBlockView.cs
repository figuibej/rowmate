using System;
using RowMate.App.Routines;
using RowMate.Core.Design;
using RowMate.Core.Localization;
using RowMate.Presentation.Controls;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Routines
{
    // Bloque de una serie en el editor (AC-RUT-14-02, -03, -09; legacy _buildGroupHeader / _buildGroupFooter,
    // routine_editor_screen.dart:371-445; 04-design-notes §4.7, DN3-04, DN3-05):
    //   #series-<k> .rm-series                          8 dp arriba y 8 abajo (16 entre dos series contiguas, AC-RUT-14-11)
    //   ├── #series-header-<k> .rm-series__header       min-height 48; riel izquierdo 3 brand.seed; fondo seed @12 % (código)
    //   │   ├── .rm-icon (repeat 16) + .rm-series__title (editorSeries, .rm-type-header-s)
    //   │   ├── #series-repeat-<k> .rm-chip --repeat    caja táctil ≥ 48 × 48, transparente
    //   │   │   └── .rm-chip__pill                      cápsula 28: ×N (.rm-type-num-stat, accent.onTonal), capa y anillo
    //   │   └── #series-delete-<k> .rm-icon-button --danger   delete 20; ayuda editorDeleteSeries
    //   ├── .rm-series__steps                           riel continuo, fondo seed @6 %: las filas con --in-series
    //   ├── #series-footer-<k> .rm-series__footer > #series-add-step-<k> (add 16 + editorAddStepToSeries, .rm-type-button-s)
    //   └── .rm-series__stack > 2 .rm-series__stack-layer   hojas bajo el pie: ×2 una, ×3 o más dos (sin eventos)
    // k = ordinal de la serie (EditorItem.SeriesOrdinal). El número del chip rueda al cambiar (TabularNumber + DigitRoll).
    public sealed class SeriesBlockView : VisualElement
    {
        public const string UssClass = "rm-series";

        private readonly UiContext _ui;
        private readonly Label _title;
        private readonly VisualElement _layerOne;
        private readonly VisualElement _layerTwo;
        private int _repeat = int.MinValue;

        public SeriesBlockView(UiContext ui, Action<SeriesBlockView> onRepeat, Action<SeriesBlockView> onDelete, Action<SeriesBlockView> onAddStep)
        {
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            AddToClassList(UssClass);
            pickingMode = PickingMode.Ignore;

            Header = new VisualElement();
            Header.AddToClassList(UssClass + "__header");
            Header.style.backgroundColor = TokenColors.WithAlpha(Tokens.BrandSeed, 0.12);
            Header.pickingMode = PickingMode.Ignore;
            var icon = new IconView(Icons.Repeat, false, 16);
            icon.AddToClassList(UssClass + "__icon");
            Header.Add(icon);
            _title = new Label();
            _title.AddToClassList(UssClass + "__title");
            _title.AddToClassList("rm-type-header-s");
            _title.pickingMode = PickingMode.Ignore;
            Header.Add(_title);

            Chip = new Button(() => onRepeat(this)) { text = "" };
            RmButton.StripUnityChrome(Chip);
            Chip.AddToClassList("rm-chip");
            Chip.AddToClassList("rm-chip--repeat");
            Chip.focusable = true;
            Pill = new VisualElement();
            Pill.AddToClassList("rm-chip__pill");
            Pill.pickingMode = PickingMode.Ignore;
            Pill.style.backgroundColor = TokenColors.WithAlpha(Tokens.AccentPrimary, 0.2);
            Pill.Add(RmButton.StateLayer());
            Count = new TabularNumber(ui.Motion);
            Count.AddToClassList("rm-chip__count");
            Count.AddToClassList("rm-type-num-stat");
            Count.pickingMode = PickingMode.Ignore;
            Pill.Add(Count);
            Pill.Add(RmButton.FocusRing());
            Chip.Add(Pill);
            Header.Add(Chip);

            var spacer = new VisualElement();
            spacer.AddToClassList(UssClass + "__spacer");
            spacer.pickingMode = PickingMode.Ignore;
            Header.Add(spacer);
            DeleteButton = new IconButton("", Icons.Delete, () => onDelete(this), 20, true);
            DeleteButton.AddToClassList(UssClass + "__delete");
            DeleteButton.SetTooltip(ui, () => _ui.Texts.Get(L10nKeys.EditorDeleteSeries));
            Header.Add(DeleteButton);
            Add(Header);

            Steps = new VisualElement();
            Steps.AddToClassList(UssClass + "__steps");
            Steps.style.backgroundColor = TokenColors.WithAlpha(Tokens.BrandSeed, 0.06);
            Steps.pickingMode = PickingMode.Ignore;
            Add(Steps);

            Footer = new VisualElement();
            Footer.AddToClassList(UssClass + "__footer");
            Footer.style.backgroundColor = TokenColors.WithAlpha(Tokens.BrandSeed, 0.06);
            Footer.pickingMode = PickingMode.Ignore;
            AddStep = new RmButton("", "text", Icons.Add, "", () => onAddStep(this), 16, "rm-type-button-s");
            AddStep.AddToClassList(UssClass + "__add-step");
            Footer.Add(AddStep);
            Add(Footer);

            Stack = new VisualElement();
            Stack.AddToClassList(UssClass + "__stack");
            Stack.pickingMode = PickingMode.Ignore;
            _layerOne = Layer(0.20, 8, 0);
            _layerTwo = Layer(0.12, 16, 3);
            Stack.Add(_layerOne);
            Stack.Add(_layerTwo);
            Add(Stack);
            RenderTexts(ui.Texts);
        }

        public string GroupId { get; set; } = "";

        public int Ordinal { get; private set; } = -1;

        public VisualElement Header { get; }

        public Button Chip { get; }

        public VisualElement Pill { get; }

        public TabularNumber Count { get; }

        public IconButton DeleteButton { get; }

        public VisualElement Steps { get; }

        public VisualElement Footer { get; }

        public RmButton AddStep { get; }

        public VisualElement Stack { get; }

        public int RepeatCount => _repeat;

        // Hojas visibles (0, 1 o 2: AC-RUT-14-02).
        public int VisibleLayers => (_layerOne.style.display == DisplayStyle.None ? 0 : 1) + (_layerTwo.style.display == DisplayStyle.None ? 0 : 1);

        public void SetOrdinal(int ordinal)
        {
            Ordinal = ordinal;
            var live = ordinal >= 0;
            var k = live ? ordinal.ToString(System.Globalization.CultureInfo.InvariantCulture) : "";
            name = live ? "series-" + k : "";
            Header.name = live ? "series-header-" + k : "";
            Chip.name = live ? "series-repeat-" + k : "";
            DeleteButton.name = live ? "series-delete-" + k : "";
            Footer.name = live ? "series-footer-" + k : "";
            AddStep.name = live ? "series-add-step-" + k : "";
        }

        public void SetRepeat(int repeat, Localizer texts)
        {
            if (repeat == _repeat)
            {
                return;
            }

            _repeat = repeat;
            Count.SetValue(RoutineTexts.RepeatChip(repeat, texts), repeat);
            _layerOne.style.display = repeat >= 2 ? DisplayStyle.Flex : DisplayStyle.None;
            _layerTwo.style.display = repeat >= 3 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void RenderTexts(Localizer texts)
        {
            _title.text = texts.Get(L10nKeys.EditorSeries);
            AddStep.SetLabel(texts.Get(L10nKeys.EditorAddStepToSeries));
            if (_repeat != int.MinValue)
            {
                Count.SetValue(RoutineTexts.RepeatChip(_repeat, texts), _repeat);
            }
        }

        // Hoja 1: left 8, right 8, top 0, seed @20 %; hoja 2: left 16, right 16, top 3, seed @12 % (04-design-notes §4.7).
        private VisualElement Layer(double alpha, float inset, float top)
        {
            var layer = new VisualElement();
            layer.AddToClassList(UssClass + "__stack-layer");
            layer.pickingMode = PickingMode.Ignore;
            layer.style.left = inset;
            layer.style.right = inset;
            layer.style.top = top;
            layer.style.backgroundColor = TokenColors.WithAlpha(Tokens.BrandSeed, alpha);
            layer.style.display = DisplayStyle.None;
            return layer;
        }
    }
}
