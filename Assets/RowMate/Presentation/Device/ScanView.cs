using System;
using RowMate.App.Device;
using RowMate.Core.Design;
using RowMate.Core.Localization;
using RowMate.Presentation.Controls;
using RowMate.Presentation.Shell;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Device
{
    // Vista de búsqueda de Dispositivo (AC-BLE-05-01, -02; AC-HRM-13-01, -08, -10; legacy _ScanView, device_screen.dart:
    // 390-445). Columna que estira con padding 16 (+ el alto real de la barra abajo, DN-23):
    //   #device-search (primario, ancho completo; buscando: spinner y deviceSearching)
    //   #device-search-hint (solo con el remo buscando)
    //   #device-empty-area (ScrollView, flex 1 1 0) > .rm-empty (ícono 48 @24 %, deviceNoResults, deviceNoResultsHint)
    //   #hrm-card-slot-scan (ScrollView, flex 1 1 0): la tarjeta del pulsómetro arriba de su mitad (Expanded + Flexible).
    // La lista de remos encontrados es de la It. 10a.
    public sealed class ScanView : VisualElement
    {
        public const float Padding = 16;

        private readonly UiContext _ui;
        private readonly VisualElement _empty;

        public ScanView(UiContext ui, ShellLayout layout, Action onSearch)
        {
            _ui = ui;
            name = "device-scan-view";
            AddToClassList("rm-device-scan");
            layout.RegisterBottomPadding(this, Padding);

            Search = new RmButton("device-search", "primary", Icons.BluetoothSearching, "", onSearch);
            Add(Search);

            Hint = new Label { name = "device-search-hint" };
            Hint.AddToClassList("rm-type-body-xs");
            Hint.AddToClassList("rm-text--tertiary");
            Hint.pickingMode = PickingMode.Ignore;
            Add(Hint);

            EmptyArea = new ScrollView(ScrollViewMode.Vertical) { name = "device-empty-area" };
            EmptyArea.AddToClassList("rm-device-empty");
            EmptyArea.AddToClassList("rm-scroll");
            ScrollPolicy.Apply(EmptyArea, ui.IsDesktop);
            _empty = new VisualElement();
            _empty.AddToClassList("rm-empty");
            _empty.pickingMode = PickingMode.Ignore;
            var icon = new IconView(Icons.BluetoothDisabled, false, 48);
            icon.AddToClassList("rm-empty__icon");
            _empty.Add(icon);
            EmptyTitle = new Label();
            EmptyTitle.AddToClassList("rm-empty__title");
            EmptyTitle.AddToClassList("rm-type-body");
            EmptyTitle.pickingMode = PickingMode.Ignore;
            _empty.Add(EmptyTitle);
            EmptyHint = new Label();
            EmptyHint.AddToClassList("rm-empty__hint");
            EmptyHint.AddToClassList("rm-type-body-xs");
            EmptyHint.AddToClassList("rm-text--tertiary");
            EmptyHint.pickingMode = PickingMode.Ignore;
            _empty.Add(EmptyHint);
            EmptyArea.Add(_empty);
            Add(EmptyArea);

            CardSlot = new ScrollView(ScrollViewMode.Vertical) { name = "hrm-card-slot-scan" };
            CardSlot.AddToClassList("rm-hrm-slot");
            CardSlot.AddToClassList("rm-scroll");
            ScrollPolicy.Apply(CardSlot, ui.IsDesktop);
            Add(CardSlot);
        }

        public RmButton Search { get; }

        public Label Hint { get; }

        public ScrollView EmptyArea { get; }

        public Label EmptyTitle { get; }

        public Label EmptyHint { get; }

        public ScrollView CardSlot { get; }

        public void Render(ScanViewSpec spec, Localizer texts)
        {
            Search.SetEnabled(spec.SearchEnabled);
            Search.SetLabel(texts.Get(spec.Searching ? L10nKeys.DeviceSearching : L10nKeys.DeviceSearch));
            Search.SetBusy(spec.Searching, _ui.Motion);
            Hint.style.display = spec.ShowSearchHint ? DisplayStyle.Flex : DisplayStyle.None;
            Hint.text = texts.Get(L10nKeys.DeviceSearchHint);
            _empty.style.display = spec.ShowEmptyState ? DisplayStyle.Flex : DisplayStyle.None;
            EmptyTitle.text = texts.Get(L10nKeys.DeviceNoResults);
            EmptyHint.text = texts.Get(L10nKeys.DeviceNoResultsHint);
        }
    }
}
