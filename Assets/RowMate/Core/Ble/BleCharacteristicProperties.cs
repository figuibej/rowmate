using System;

namespace RowMate.Core.Ble
{
    // Propiedades GATT de una característica (las que registra el descubrimiento, ble_service.dart:211-216). Mismos valores en
    // el formato de eventos (BleWire, u8 props) que escriben la DLL de Windows y el Kotlin de Android.
    [Flags]
    public enum BleCharacteristicProperties
    {
        None = 0,
        Read = 1,
        Write = 2,
        WriteNoResponse = 4,
        Notify = 8,
        Indicate = 16
    }
}
