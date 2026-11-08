using System;
using RowMate.Core.Ble;

namespace RowMate.App.Device
{
    // El diagnóstico oculto (F-BLE-12; device_provider.dart:19-22, :56-61, :98-102): el contador de paquetes de TODA la vida de la
    // app (no se reinicia al reconectar; QA-G10a-09), el último paquete en hex (armado una vez por paquete, A10-11; "(sin datos)"
    // sin paquete; no se borra al reconectar) y la hora local del último paquete de ESTA conexión (null al pasar a conectado,
    // dp:64-67).
    public sealed record BleDiagnostics(long Count, string Hex, DateTimeOffset? LastReceived)
    {
        public static readonly BleDiagnostics Empty = new BleDiagnostics(0, BleHex.NoData, null);
    }
}
