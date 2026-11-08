namespace RowMate.App.Device
{
    // Una tarjeta de remo encontrado (AC-BLE-05-07, AC-BLE-06-01, -07; device_screen.dart:444-471; A10-04, A10-05). Key = la
    // dirección (clave estable). Name = el nombre de plataforma o deviceUnknown. Line2 = la dirección, o el paso en curso en la
    // tarjeta tocada (IsActive: conexión elegida por el usuario, no la reconexión). Conectando, todas muestran el spinner de 20 en
    // lugar de la flecha y no responden.
    public sealed record ScanItemSpec(
        string Key,
        string Name,
        string Line2,
        bool IsActive,
        bool ShowSpinner,
        bool Enabled);
}
