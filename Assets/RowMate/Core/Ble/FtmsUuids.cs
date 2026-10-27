namespace RowMate.Core.Ble
{
    // Identificadores del Fitness Machine Service (F-BLE-02). La característica que se decodifica es Rower Data
    // 0x2AD1 (no 0x2AD2, DC-01: el comentario del legacy está mal, la constante no). Control Point lo usa la It. 10a.
    // legacy: lib/core/bluetooth/ftms_parser.dart:22-24
    public static class FtmsUuids
    {
        public const string ServiceText = "00001826-0000-1000-8000-00805f9b34fb";
        public const string RowerDataText = "00002ad1-0000-1000-8000-00805f9b34fb";
        public const string ControlPointText = "00002ad9-0000-1000-8000-00805f9b34fb";

        public static readonly BleUuid Service = BleUuid.Parse(ServiceText);
        public static readonly BleUuid RowerData = BleUuid.Parse(RowerDataText);
        public static readonly BleUuid ControlPoint = BleUuid.Parse(ControlPointText);
    }
}
