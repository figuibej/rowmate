namespace RowMate.Core.Ble
{
    // Una característica descubierta: UUID y propiedades (02-tech-design §2.1 de la It. 10a).
    public sealed record BleCharacteristic(BleUuid Uuid, BleCharacteristicProperties Properties)
    {
        public bool CanNotify => (Properties & (BleCharacteristicProperties.Notify | BleCharacteristicProperties.Indicate)) != 0;
    }
}
