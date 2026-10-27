namespace RowMate.App.Device
{
    // Una fila de la lista de sensores encontrados (AC-HRM-13-05): título (nombre del sistema, el anunciado o
    // hrmUnknownSensor), subtítulo (el id del dispositivo) y el nombre de contrato del elemento ("hrm-device-<id>", el key
    // del legacy). Tocarla conecta con el título como nombre elegido.
    // legacy: lib/features/device/heart_rate_card.dart:131-145
    public sealed record HrmDeviceRow(string DeviceId, string Title, string Subtitle, string ElementName);
}
