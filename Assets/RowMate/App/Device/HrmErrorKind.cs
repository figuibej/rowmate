namespace RowMate.App.Device
{
    // De qué acción es el error de la tarjeta del pulsómetro (It. 11, DT11-15; AC-HRM-13-14): Scan = Buscar; Connect = conectar,
    // Conectar, Desconectar u Olvidar; Incompatible = el sensor no tiene 0x180D/0x2A37 (HrmIncompatibleException).
    public enum HrmErrorKind
    {
        None,
        Scan,
        Connect,
        Incompatible
    }
}
