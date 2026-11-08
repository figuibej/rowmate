namespace RowMate.Core.Ble
{
    // Identificadores de equipos en el registro (DT10-05; AC-BLE-SEC-01): la dirección se enmascara, así un Player.log que el
    // usuario comparte no lleva la dirección completa de un equipo real. "AA:BB:CC:DD:EE:FF" → "AA:BB:…:FF".
    public static class BleIds
    {
        public const char Ellipsis = '…';

        public static string Mask(string? id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return "?";
            }

            var parts = id!.Split(':');
            if (parts.Length >= 3)
            {
                return parts[0] + ":" + parts[1] + ":" + Ellipsis + ":" + parts[parts.Length - 1];
            }

            return id.Length > 4 ? id.Substring(0, 2) + Ellipsis + id.Substring(id.Length - 2) : Ellipsis.ToString();
        }
    }
}
