using System;

namespace RowMate.Core.Ble
{
    // El dispositivo conectó pero no expone el servicio Heart Rate (0x180D). La tarjeta lo distingue de cualquier otro
    // error y muestra hrmIncompatible (AC-HRM-12-04, AC-HRM-13-07).
    // legacy: lib/core/bluetooth/heart_rate_service.dart:20-24
    public sealed class HrmIncompatibleException : Exception
    {
        public HrmIncompatibleException()
            : base("El dispositivo no expone el servicio Heart Rate (0x180D)")
        {
        }
    }
}
