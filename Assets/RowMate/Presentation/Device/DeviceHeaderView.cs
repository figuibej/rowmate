using RowMate.Core.Design;
using RowMate.Presentation.Controls;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Device
{
    // Cabecera de la vista conectada (AC-BLE-11-02; legacy device_screen.dart:92-118): fila centrada con el ícono de remo
    // (rowing 32, accent.primary), el nombre del remo (.rm-type-title, una línea con "…") y la insignia verde "Conectado"
    // (ícono bluetooth_connected 12 + texto .rm-type-body-xs). Ajusta en dos líneas si no entra (flex-wrap, DN-24: el
    // legacy desbordaba a 360 dp con letra grande).
    public sealed class DeviceHeaderView : VisualElement
    {
        public DeviceHeaderView()
        {
            name = "device-header";
            AddToClassList("rm-device-header");
            pickingMode = PickingMode.Ignore;
            var icon = new IconView(Icons.Rowing, false, 32);
            icon.AddToClassList("rm-device-header__icon");
            Add(icon);
            NameLabel = new Label { name = "device-name" };
            NameLabel.AddToClassList("rm-device-header__name");
            NameLabel.AddToClassList("rm-type-title");
            NameLabel.pickingMode = PickingMode.Ignore;
            Add(NameLabel);
            var badge = new VisualElement { name = "device-connected-badge" };
            badge.AddToClassList("rm-badge");
            badge.AddToClassList("rm-badge--connected");
            badge.pickingMode = PickingMode.Ignore;
            var badgeIcon = new IconView(Icons.BluetoothConnected, false, 12);
            badgeIcon.AddToClassList("rm-badge__icon");
            badge.Add(badgeIcon);
            BadgeLabel = new Label();
            BadgeLabel.AddToClassList("rm-badge__label");
            BadgeLabel.AddToClassList("rm-type-body-xs");
            BadgeLabel.pickingMode = PickingMode.Ignore;
            badge.Add(BadgeLabel);
            Add(badge);
        }

        public Label NameLabel { get; }

        public Label BadgeLabel { get; }

        public void Render(string deviceName, string connectedText)
        {
            if (NameLabel.text != deviceName)
            {
                NameLabel.text = deviceName;
            }

            if (BadgeLabel.text != connectedText)
            {
                BadgeLabel.text = connectedText;
            }
        }
    }
}
