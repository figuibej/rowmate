namespace RowMate.App.Device
{
    // El panel DEBUG BLE con los textos ya armados, fijo en inglés (Q-04; device_screen.dart:142-218; AC-BLE-12-02). Receiving =
    // llegó al menos 1 paquete en la vida de la app (notificationCount > 0, ds:177; QA-G10a-09). Los valores FTMS se muestran
    // crudos (Q-38).
    public sealed record BleDebugSpec(
        bool Receiving,
        string Badge,
        string Notifications,
        string LastReceived,
        string RawHex,
        string ParsedSpm,
        string ParsedWatts,
        string ParsedDistance,
        string ParsedSplit)
    {
        public const string Title = "DEBUG BLE";
        public const string ShowButton = "Show BLE debug";
        public const string ReceivingBadge = "RECEIVING";
        public const string NoDataBadge = "NO DATA";
        public const string NotificationsLabel = "Notifications";
        public const string LastReceivedLabel = "Last received";
        public const string RawBytesLabel = "Raw bytes (hex):";
        public const string ParsedSpmLabel = "Parsed SPM";
        public const string ParsedWattsLabel = "Parsed Watts";
        public const string ParsedDistanceLabel = "Parsed Distance";
        public const string ParsedSplitLabel = "Parsed Split";
        public const string NoTime = "--:--:--";
    }
}
