using System;
using RowMate.App.Device;
using RowMate.Core.Design;
using RowMate.Presentation.Controls;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Device
{
    // La caja del enlace arriba de "Buscar dispositivos" (AC-BLE-08-05, AC-BLE-09-02; 00-arte §4.1): Reconectando (surface.card, el arco
    // de espera de 3 s o el spinner del intento, deviceReconnecting con el nombre ya cortado y deviceDisconnect, que corta los
    // reintentos) o Monitor apagado (state.warningBg, link_off, deviceMonitorStopped y retry, que vuelve a conectar el mismo remo).
    //   #device-link-banner .rm-link-banner(--reconnecting|--stopped)
    //   ├── #device-link-indicator (LinkIndicator 20)
    //   ├── #device-link-text .rm-type-body-m (una línea)
    //   └── #device-link-action (botón de texto)
    // Lo dibuja LinkBannerSpec (App). El crecer y colapsar con la regla del empuje es de WP-D.
    public sealed class LinkBanner : VisualElement
    {
        private readonly Motion.BleMotion.Grow _grow;
        private readonly UiContext _ui;
        private LinkBannerKind _kind;

        public LinkBanner(UiContext ui, Action<LinkBannerKind> onAction)
        {
            _ui = ui;
            name = "device-link-banner";
            AddToClassList("rm-link-banner");
            pickingMode = PickingMode.Ignore;
            Indicator = new LinkIndicator(ui.Motion, 20) { name = "device-link-indicator" };
            Indicator.AddToClassList("rm-link-banner__indicator");
            Add(Indicator);
            Text = new Label { name = "device-link-text" };
            Text.AddToClassList("rm-type-body-m");
            Text.AddToClassList("rm-link-banner__text");
            Text.pickingMode = PickingMode.Ignore;
            Text.enableRichText = false;
            Add(Text);
            Action = new RmButton("device-link-action", "text", null, "", () => onAction(_kind));
            Action.AddToClassList("rm-link-banner__action");
            Add(Action);
            style.display = DisplayStyle.None;
            _grow = new Motion.BleMotion.Grow(ui.Motion, this);
        }

        public LinkIndicator Indicator { get; }

        public Label Text { get; }

        public RmButton Action { get; }

        public LinkBannerKind Kind => _kind;

        public void Render(LinkBannerSpec spec)
        {
            _kind = spec.Kind;
            if (spec.Kind == LinkBannerKind.None)
            {
                style.display = DisplayStyle.None;
                Indicator.Show(LinkIndicator.Mode.None);
                return;
            }

            // WP-D (00-arte §4.1): la caja crece con la regla del empuje al aparecer (alto 0 → 48 y el aire de abajo 0 → 12 en 0-160).
            if (_kind != LinkBannerKind.None && style.display == DisplayStyle.None && _ui.Motion.Armed)
            {
                _grow.Play(BleMotionSpecs.BannerMinHeightDp, BleMotionSpecs.BannerGapDp, 0, 0);
            }

            style.display = DisplayStyle.Flex;
            EnableInClassList("rm-link-banner--reconnecting", spec.Kind == LinkBannerKind.Reconnecting);
            EnableInClassList("rm-link-banner--stopped", spec.Kind == LinkBannerKind.MonitorStopped);
            Action.EnableInClassList("rm-button--danger", spec.Kind == LinkBannerKind.Reconnecting);
            if (Text.text != spec.Text)
            {
                Text.text = spec.Text;
            }

            Action.SetLabel(spec.ActionText);
            switch (spec.Indicator)
            {
                case LinkIndicatorKind.WaitingArc:
                    Indicator.Show(LinkIndicator.Mode.WaitingArc, spec.NextAttemptAt ?? 0);
                    break;
                case LinkIndicatorKind.Spinner:
                    Indicator.Show(LinkIndicator.Mode.Spinner);
                    break;
                case LinkIndicatorKind.LinkOff:
                    Indicator.Show(LinkIndicator.Mode.LinkOff);
                    break;
                default:
                    Indicator.Show(LinkIndicator.Mode.None);
                    break;
            }
        }
    }
}
