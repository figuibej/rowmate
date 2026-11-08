using System;
using System.Collections.Generic;
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
    // It. 10a (02-tech-design §6.1): arriba, #device-link-banner (LinkBanner: reconectando o monitor apagado); la lista de remos
    // encontrados (#device-results, ScanResultList) va en #device-empty-area, arriba, en lugar del estado vacío.
    public sealed class ScanView : VisualElement
    {
        public const float Padding = 16;

        private readonly UiContext _ui;
        private readonly VisualElement _empty;
        private int _lastCount;

        public ScanView(UiContext ui, ShellLayout layout, Action onSearch, Action<string>? onTapResult = null, Action<LinkBannerKind>? onLinkAction = null)
        {
            _ui = ui;
            name = "device-scan-view";
            AddToClassList("rm-device-scan");
            layout.RegisterBottomPadding(this, Padding);

            LinkBanner = new LinkBanner(ui, onLinkAction ?? (_ => { }));
            Add(LinkBanner);

            Search = new RmButton("device-search", "primary", Icons.BluetoothSearching, "", onSearch);
            Add(Search);

            Hint = new Label { name = "device-search-hint" };
            Hint.AddToClassList("rm-type-body-xs");
            Hint.AddToClassList("rm-text--tertiary");
            Hint.pickingMode = PickingMode.Ignore;
            Add(Hint);

            // WP-D (00-arte §1.2, D10-02): la banda del sonar entre la ayuda y la lista, solo buscando y sin alto compacto.
            Sonar = new SonarView(ui.Motion);
            Add(Sonar);

            EmptyArea = new ScrollView(ScrollViewMode.Vertical) { name = "device-empty-area" };
            EmptyArea.AddToClassList("rm-device-empty");
            EmptyArea.AddToClassList("rm-scroll");
            ScrollPolicy.Apply(EmptyArea, ui.IsDesktop);
            Results = new ScanResultList(ui, onTapResult ?? (_ => { }));
            EmptyArea.Add(Results);
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
            RegisterCallback<GeometryChangedEvent>(evt =>
            {
                if (evt.newRect.height > 0)
                {
                    Sonar.SetCompact(evt.newRect.height < RowMate.Core.Design.BleMotionSpecs.CompactHeightDp);
                }
            });
        }

        public SonarView Sonar { get; }

        public LinkBanner LinkBanner { get; }

        public ScanResultList Results { get; }

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

        // It. 10a: la lista de remos (AC-BLE-05-07) y la caja del enlace (AC-BLE-08-05, AC-BLE-09-02).
        public void Render(ScanViewSpec spec, IReadOnlyList<ScanItemSpec> items, LinkBannerSpec link, Localizer texts)
        {
            Render(spec, texts);
            var searching = spec.ShowSearchHint;
            Sonar.SetSearching(searching);
            if (searching && items.Count > _lastCount)
            {
                Sonar.Echo();
            }

            _lastCount = items.Count;
            Results.Render(items);
            Results.style.display = items.Count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            EmptyArea.EnableInClassList("rm-device-empty--list", items.Count > 0);
            LinkBanner.Render(link);
        }
    }
}
