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
    // It. 14 (#40, AC-DEBT-01; 02-tech-design §7): el nombre se ve COMPLETO en hasta 2 líneas (white-space normal) y la acción no se
    // achica (flex-shrink 0); si ni en 2 líneas entra al lado de la acción, la acción baja debajo (.rm-link-banner--stacked, según el
    // ancho medido). Solo un nombre de más de 24 caracteres se corta con "…" (DT10-14, en LinkNoticeText). Los estilos del ajuste van
    // en línea (el Designer puede sumar la clase).
    public sealed class LinkBanner : VisualElement
    {
        public const string StackedClass = "rm-link-banner--stacked";

        // Hasta 2 líneas al lado de la acción (con un 5 % de margen por la partición de palabras).
        public const double MaxLines = 2;

        private bool _stacked;

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
            Text.style.whiteSpace = WhiteSpace.Normal;
            Text.style.overflow = Overflow.Visible;
            Text.style.textOverflow = TextOverflow.Clip;
            Add(Text);
            Action = new RmButton("device-link-action", "text", null, "", () => onAction(_kind));
            Action.AddToClassList("rm-link-banner__action");
            Action.style.flexShrink = 0;
            Add(Action);
            RegisterCallback<GeometryChangedEvent>(_ => Relayout());
            style.display = DisplayStyle.None;
            _grow = new Motion.BleMotion.Grow(ui.Motion, this);
        }

        public LinkIndicator Indicator { get; }

        public Label Text { get; }

        public RmButton Action { get; }

        public LinkBannerKind Kind => _kind;

        // #40: la acción quedó debajo del texto.
        public bool IsStacked => _stacked;

        // ¿Entra el texto en hasta 2 líneas al lado de la acción? Si no, la acción baja (y el texto toma todo el ancho al lado del
        // indicador). Solo cambia estilos cuando cambia el estado (sin bucles de layout).
        private void Relayout()
        {
            var width = contentRect.width;
            if (!(width > 0) || string.IsNullOrEmpty(Text.text))
            {
                return;
            }

            var indicator = Indicator.layout.width + Indicator.resolvedStyle.marginRight;
            var action = Action.layout.width + Action.resolvedStyle.marginLeft;
            var beside = width - indicator - action;
            var natural = Text.MeasureTextSize(Text.text, 0, MeasureMode.Undefined, 0, MeasureMode.Undefined).x;
            var stacked = natural > beside * MaxLines * 0.95;
            if (stacked == _stacked)
            {
                if (stacked)
                {
                    Text.style.width = Math.Max(0, width - indicator);
                }

                return;
            }

            _stacked = stacked;
            EnableInClassList(StackedClass, stacked);
            style.flexWrap = stacked ? Wrap.Wrap : StyleKeyword.Null;
            Text.style.width = stacked ? Math.Max(0, width - indicator) : StyleKeyword.Null;
            Action.style.marginLeft = stacked ? StyleKeyword.Auto : StyleKeyword.Null;
        }

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
                Relayout();
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
