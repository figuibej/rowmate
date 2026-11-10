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
            // WP-D (§3; 02-tech-design §4.2): con un único desplazamiento, si la tarjeta del pulsómetro crece y sale de la vista, la sigue.
            _follow = new RowMate.Presentation.Motion.HrmMotion.Follow(ui.Motion, CardSlot);
            CardSlot.contentContainer.RegisterCallback<GeometryChangedEvent>(_ => FollowCard());

            // It. 11 (A11-05): las columnas de TwoColumns (se usan solo en esa disposición).
            _columns = new VisualElement { name = "device-scan-columns" };
            _columns.AddToClassList("rm-device-scan__columns");
            _columns.pickingMode = PickingMode.Ignore;
            _main = new VisualElement { name = "device-scan-main" };
            _main.AddToClassList("rm-device-scan__main");
            _main.pickingMode = PickingMode.Ignore;
            _columns.Add(_main);

            RegisterCallback<GeometryChangedEvent>(evt =>
            {
                if (evt.newRect.height > 0)
                {
                    Sonar.SetCompact(evt.newRect.height < RowMate.Core.Design.BleMotionSpecs.CompactHeightDp);
                    ApplyLayout(DeviceScanLayout.For(contentRect.width, evt.newRect.height));
                }
            });
        }

        private readonly VisualElement _columns;
        private readonly VisualElement _main;
        private readonly RowMate.Presentation.Motion.HrmMotion.Follow _follow;
        private VisualElement? _followedCard;
        private float _followedHeight;

        // La tarjeta es el último hijo del contenido del slot (DeviceScreenView la mueve ahí). Solo en SingleScroll y solo si creció.
        private void FollowCard()
        {
            var content = CardSlot.contentContainer;
            var card = CardIn(content);
            SizeCard(card);
            if (card == null)
            {
                _followedCard = null;
                return;
            }

            var height = card.layout.height;
            var grew = ReferenceEquals(card, _followedCard) && height > _followedHeight + 0.5f;
            _followedCard = card;
            _followedHeight = height;
            if (grew && Layout.Kind == DeviceScanLayoutKind.SingleScroll)
            {
                _follow.Track(card);
            }
        }

        // It. 11 (A11-05, DT11-14): la disposición vigente (Stacked hasta el primer layout).
        public DeviceScanLayout Layout { get; private set; } = new DeviceScanLayout(DeviceScanLayoutKind.Stacked, 0);

        // Reubica la lista, el vacío y el slot de la tarjeta SIN recrearlos (en el acto: es un cambio de ventana, no de dato):
        // - Stacked: como hasta la 10a (búsqueda, ayuda, sonar, la lista en #device-empty-area y el slot, hermanos).
        // - SingleScroll (L640): un único desplazamiento: la lista y el vacío pasan al slot de la tarjeta, arriba de ella (a su alto natural),
        //   y #device-empty-area se oculta.
        // - TwoColumns (TH): búsqueda, ayuda y sonar arriba; debajo, UN SOLO desplazamiento (el slot de la tarjeta, It. 14 #42,
        //   AC-DEBT-03): adentro, en fila, la lista y el vacío a la izquierda (a su alto natural, sin alto fijo) y la tarjeta a la derecha
        //   con ancho RightColumnDp. "Buscar" del pulsómetro se alcanza desplazando; #device-empty-area se oculta.
        public void ApplyLayout(DeviceScanLayout layout)
        {
            if (layout.Equals(Layout) && (layout.Kind != DeviceScanLayoutKind.Stacked || _columns.parent == null))
            {
                return;
            }

            Layout = layout;
            EnableInClassList("rm-device-scan--single-scroll", layout.Kind == DeviceScanLayoutKind.SingleScroll);
            EnableInClassList("rm-device-scan--two-columns", layout.Kind == DeviceScanLayoutKind.TwoColumns);

            // 1. Todo vuelve a la columna plana (orden de la 10a).
            if (_columns.parent == this)
            {
                Remove(_columns);
            }

            if (_main.parent != _columns)
            {
                _columns.Add(_main);
            }

            Add(Search);
            Add(Hint);
            Add(Sonar);
            Add(EmptyArea);
            Add(CardSlot);
            if (Results.parent != EmptyArea.contentContainer)
            {
                EmptyArea.Add(Results);
            }

            if (_empty.parent != EmptyArea.contentContainer)
            {
                EmptyArea.Add(_empty);
            }

            EmptyArea.style.display = StyleKeyword.Null;
            CardSlot.style.width = StyleKeyword.Null;
            CardSlot.contentContainer.style.flexDirection = StyleKeyword.Null;
            CardSlot.contentContainer.style.alignItems = StyleKeyword.Null;
            SizeCard(null);

            // 2. La disposición pedida.
            if (layout.Kind == DeviceScanLayoutKind.SingleScroll)
            {
                EmptyArea.style.display = DisplayStyle.None;
                CardSlot.contentContainer.Insert(0, Results);
                CardSlot.contentContainer.Insert(1, _empty);
            }
            else if (layout.Kind == DeviceScanLayoutKind.TwoColumns)
            {
                // #42: un solo desplazamiento (el del slot), con la lista y la tarjeta en fila adentro.
                EmptyArea.style.display = DisplayStyle.None;
                _main.Add(Results);
                _main.Add(_empty);
                CardSlot.contentContainer.Insert(0, _main);
                CardSlot.contentContainer.style.flexDirection = FlexDirection.Row;
                CardSlot.contentContainer.style.alignItems = Align.FlexStart;
                SizeCard(CardIn(CardSlot.contentContainer));
            }
        }

        // La tarjeta del pulsómetro dentro del slot (el último hijo, la pone DeviceScreenView), o null.
        private static VisualElement? CardIn(VisualElement content)
        {
            var card = content.childCount > 0 ? content[content.childCount - 1] : null;
            return card != null && card.name == "hrm-card" ? card : null;
        }

        private VisualElement? _sizedCard;

        // En TwoColumns la tarjeta toma el ancho de la columna derecha; fuera de esa disposición (o en otro slot) vuelve a su ancho.
        private void SizeCard(VisualElement? card)
        {
            if (_sizedCard != null && !ReferenceEquals(_sizedCard, card))
            {
                _sizedCard.style.width = StyleKeyword.Null;
                _sizedCard.style.flexShrink = StyleKeyword.Null;
                _sizedCard.style.marginLeft = StyleKeyword.Null;
                _sizedCard = null;
            }

            if (card == null || Layout.Kind != DeviceScanLayoutKind.TwoColumns)
            {
                return;
            }

            _sizedCard = card;
            card.style.width = (float)Layout.RightColumnDp;
            card.style.flexShrink = 0;
            card.style.marginLeft = Padding;
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
            ApplySearchingLook(spec.Searching);
            Hint.style.display = spec.ShowSearchHint ? DisplayStyle.Flex : DisplayStyle.None;
            Hint.text = texts.Get(L10nKeys.DeviceSearchHint);
            _empty.style.display = spec.ShowEmptyState ? DisplayStyle.Flex : DisplayStyle.None;
            EmptyTitle.text = texts.Get(L10nKeys.DeviceNoResults);
            EmptyHint.text = texts.Get(L10nKeys.DeviceNoResultsHint);
        }

        // It. 14 (#41, AC-DEBT-02; legacy device_screen.dart:398-402): "Buscando..." con el indicador BLANCO de 16 dp y la etiqueta blanca
        // al 70 %, sobre el fondo del primario deshabilitado (accent.primary al 38 % sobre bg.app, el mismo color que da hoy la opacidad
        // 0,38) pero sin apagar el texto: contraste ≥ 4,5:1. El deshabilitado genérico no cambia (solo este botón y solo buscando).
        public const string BusyClass = "rm-scan-button--busy";
        public const float BusyLabelAlpha = 0.7f;
        public const double BusyBackgroundAlpha = 0.38;

        private void ApplySearchingLook(bool searching)
        {
            Search.EnableInClassList(BusyClass, searching);
            Search.style.opacity = searching ? 1f : (StyleFloat)StyleKeyword.Null;
            Search.style.backgroundColor = searching
                ? TokenColors.Lerp(TokenColors.ToColor(Tokens.BgApp), TokenColors.ToColor(Tokens.AccentPrimary), BusyBackgroundAlpha)
                : (StyleColor)StyleKeyword.Null;
            var white = TokenColors.ToColor(Tokens.TextPrimary);
            Search.Label.style.color = searching ? new UnityEngine.Color(white.r, white.g, white.b, BusyLabelAlpha) : (StyleColor)StyleKeyword.Null;
            var spinner = Search.BusySpinner;
            if (spinner != null)
            {
                spinner.style.color = searching ? white : (StyleColor)StyleKeyword.Null;
            }
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
