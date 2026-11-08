using UnityEngine.UIElements;

namespace RowMate.Presentation.Device
{
    // El banner rojo de Dispositivo (AC-BLE-05-08, AC-BLE-06-06; device_screen.dart:355-365): arriba de las tres vistas, fondo rojo al
    // 15 %, radio 12, texto 13 en state.dangerText, con deviceScanError / deviceConnectError y el detalle del sistema sin traducir. Se
    // borra al buscar o conectar otra vez (DeviceStore). El crecer y colapsar con la regla del empuje es de WP-D.
    //   #device-error-banner .rm-device-error > #device-error-text .rm-type-body-s
    public sealed class ErrorBanner : VisualElement
    {
        public ErrorBanner()
        {
            name = "device-error-banner";
            AddToClassList("rm-device-error");
            pickingMode = PickingMode.Ignore;
            var fill = new VisualElement();
            fill.AddToClassList("rm-device-error__fill");
            fill.pickingMode = PickingMode.Ignore;
            Add(fill);
            Text = new Label { name = "device-error-text" };
            Text.AddToClassList("rm-type-body-s");
            Text.AddToClassList("rm-device-error__text");
            Text.pickingMode = PickingMode.Ignore;
            Text.enableRichText = false;
            Add(Text);
            style.display = DisplayStyle.None;
        }

        public Label Text { get; }

        public bool IsShown => style.display != DisplayStyle.None;

        // null = sin error.
        public void Render(string? text)
        {
            if (string.IsNullOrEmpty(text))
            {
                style.display = DisplayStyle.None;
                return;
            }

            if (Text.text != text)
            {
                Text.text = text;
            }

            style.display = DisplayStyle.Flex;
        }
    }
}
