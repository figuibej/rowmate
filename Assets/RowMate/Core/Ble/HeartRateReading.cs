namespace RowMate.Core.Ble
{
    // Lectura de la característica Heart Rate Measurement 0x2A37 (F-HRM-01).
    // SensorContact: null = el sensor no informa contacto; false = sin contacto; true = con contacto.
    // Sin validación de rango (Q-48): el bpm es el valor crudo.
    // legacy: lib/core/bluetooth/heart_rate_parser.dart:4-11
    public sealed record HeartRateReading(int Bpm, bool? SensorContact);
}
