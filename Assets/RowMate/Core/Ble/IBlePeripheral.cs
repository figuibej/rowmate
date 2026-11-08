using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RowMate.Core.Common;

namespace RowMate.Core.Ble
{
    // Un equipo conectado (= BluetoothDevice conectado de flutter_blue_plus; 02-tech-design §2.1). Dispose cierra la sesión
    // nativa si sigue abierta (idempotente). Hilo principal.
    public interface IBlePeripheral : IDisposable
    {
        string DeviceId { get; }

        string? Name { get; }

        // La caída no pedida del enlace (connectionState == disconnected sin desconexión manual, ble_service.dart:148-153).
        IEventSource<BleException> LinkLost { get; }

        IEventSource<BleNotification> Notifications { get; }

        Task<IReadOnlyList<BleGattService>> DiscoverServicesAsync();

        Task SetNotifyAsync(BleUuid service, BleUuid characteristic, bool enable);

        Task WriteAsync(BleUuid service, BleUuid characteristic, byte[] data, bool withResponse);

        Task DisconnectAsync();

        // Keepalive en el plugin mientras la app está en segundo plano (Android, ADR 017). En Windows no hace nada.
        void StartNativeKeepAlive(BleUuid service, BleUuid characteristic, byte[] data, TimeSpan period);

        void StopNativeKeepAlive();
    }
}
