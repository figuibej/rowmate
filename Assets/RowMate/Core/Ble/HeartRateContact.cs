using System;

namespace RowMate.Core.Ble
{
    // Regla de contacto con la piel del pulso publicado (AC-HRM-04-02): "sin contacto" → 0; "con contacto" o "no informa"
    // → el pulso leído, aunque sea 0. El servicio real la aplica a cada notificación en la It. 11.
    // legacy: lib/core/bluetooth/heart_rate_service.dart:321-326 (_onMeasurement)
    public static class HeartRateContact
    {
        public static int PublishedBpm(HeartRateReading reading)
        {
            if (reading == null)
            {
                throw new ArgumentNullException(nameof(reading));
            }

            return reading.SensorContact == false ? 0 : reading.Bpm;
        }
    }
}
