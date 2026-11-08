using System;
using RowMate.App.Device;
using RowMate.Core.Design;
using RowMate.Presentation.Controls;
using RowMate.Presentation.Shell;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Device
{
    // La vista de estado del Bluetooth (AC-BLE-04-01, -02, -06; device_screen.dart:346-394; 00-arte §3.1): apagado, sin permiso o sin
    // Bluetooth LE. Reemplaza la búsqueda, la lista y la tarjeta del pulsómetro; el banner de error sigue arriba (DeviceScreenView).
    //   #device-state-view .rm-device-state (absoluta; la columna centrada en los dos ejes, desplazable si no entra)
    //   └── #device-state-scroll > .rm-device-state__column
    //       ├── #device-bt-art      BluetoothIllustration (WP-D): anillos quietos, disco y bluetooth_disabled 64 (state.danger; text.tertiary
    //       │                       sin Bluetooth LE), que respira en apagado y sin permiso; "se despierta" al volver el Bluetooth
    //       ├── #device-bt-title    18 negrita (DT10-13), centrado, hasta 2 líneas
    //       ├── #device-bt-hint     14 text.tertiary, centrado, ancho máximo 320 (los \n del catálogo se respetan)
    //       └── #device-bt-enable   primario con el ícono bluetooth (no sin Bluetooth LE) → RequestEnableAsync
    // Lo dibuja BluetoothStateSpec (App). Con alto compacto (< 560 dp: L640, TH) la ilustración baja a 112 y los aires a 16, 8 y 16.
    public sealed class BluetoothStateView : VisualElement
    {
        public BluetoothStateView(UiContext ui, ShellLayout layout, Action onEnable)
        {
            name = "device-state-view";
            AddToClassList("rm-device-state");
            Scroll = new ScrollView(ScrollViewMode.Vertical) { name = "device-state-scroll" };
            Scroll.AddToClassList("rm-scroll");
            Scroll.AddToClassList("rm-device-state__scroll");
            ScrollPolicy.Apply(Scroll, ui.IsDesktop);
            layout.RegisterBottomPadding(Scroll.contentContainer, 16);
            Add(Scroll);
            var column = new VisualElement();
            column.AddToClassList("rm-device-state__column");
            column.pickingMode = PickingMode.Ignore;
            Scroll.Add(column);

            Art = new BluetoothIllustration(ui.Motion);
            column.Add(Art);
            Title = new Label { name = "device-bt-title" };
            Title.AddToClassList("rm-type-title");
            Title.AddToClassList("rm-device-state__title");
            Title.pickingMode = PickingMode.Ignore;
            column.Add(Title);
            Hint = new Label { name = "device-bt-hint" };
            Hint.AddToClassList("rm-type-body-m");
            Hint.AddToClassList("rm-text--tertiary");
            Hint.AddToClassList("rm-device-state__hint");
            Hint.pickingMode = PickingMode.Ignore;
            column.Add(Hint);
            Enable = new RmButton("device-bt-enable", "primary", Icons.Bluetooth, "", onEnable);
            Enable.AddToClassList("rm-device-state__enable");
            column.Add(Enable);
            RegisterCallback<GeometryChangedEvent>(OnGeometry);
        }

        public ScrollView Scroll { get; }

        public BluetoothIllustration Art { get; }

        public IconView Icon => Art.Icon;

        public Label Title { get; }

        public Label Hint { get; }

        public RmButton Enable { get; }

        // La clave del título que se ve (pruebas y arnés).
        public string? TitleKey { get; private set; }

        public void Render(BluetoothStateSpec spec)
        {
            TitleKey = spec.TitleKey;
            if (Title.text != spec.Title)
            {
                Title.text = spec.Title;
            }

            if (Hint.text != spec.Hint)
            {
                Hint.text = spec.Hint;
            }

            Art.SetIcon(spec.Icon, spec.IconDanger, spec.Breathes);
            Enable.SetLabel(spec.EnableText);
            Enable.style.display = spec.ShowEnableButton ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void OnGeometry(GeometryChangedEvent evt)
        {
            var height = evt.newRect.height;
            if (!(height > 0))
            {
                return;
            }

            var compact = height < BleMotionSpecs.CompactHeightDp;
            if (compact != Art.IsCompact)
            {
                Art.SetCompact(compact);
                EnableInClassList("rm-device-state--compact", compact);
            }
        }
    }
}
