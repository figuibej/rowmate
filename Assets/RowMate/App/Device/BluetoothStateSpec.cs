namespace RowMate.App.Device
{
    // La vista de estado del Bluetooth (AC-BLE-04-01, -02, -06; device_screen.dart:346-394; 00-arte §3): título (18 negrita,
    // DT10-13), ayuda, botón "Activar Bluetooth" (no sin Bluetooth LE), el ícono bluetooth_disabled en rojo (state.danger) o en
    // text.tertiary sin Bluetooth LE (D10-07) y si respira (solo apagado y sin permiso). Reemplaza la búsqueda, la lista y la
    // tarjeta del pulsómetro; el banner de error sigue arriba.
    public sealed record BluetoothStateSpec(
        string TitleKey,
        string Title,
        string Hint,
        bool ShowEnableButton,
        string EnableText,
        string Icon,
        bool IconDanger,
        bool Breathes);
}
