using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RowMate.Core.Common;

namespace RowMate.Core.Ble.Native
{
    // Un equipo conectado por el plugin nativo (10a 02-tech-design §2.6). Delgado: todo pasa por su NativeBleAdapter, que lleva
    // las sesiones por id, los pedidos (reqId) y el despacho de eventos. Dispose y DisconnectAsync cierran la sesión nativa si
    // sigue abierta (idempotentes, DT10-17). Hilo principal.
    public sealed class NativePeripheral : IBlePeripheral
    {
        private readonly NativeBleAdapter _adapter;
        private readonly EventStream<BleException> _linkLost;
        private readonly EventStream<BleNotification> _notifications;

        internal NativePeripheral(NativeBleAdapter adapter, string deviceId, string? name, int generation, IAppLogger logger)
        {
            _adapter = adapter;
            DeviceId = deviceId;
            Name = name;
            Generation = generation;
            _linkLost = new EventStream<BleException>(logger);
            _notifications = new EventStream<BleNotification>(logger);
        }

        public string DeviceId { get; }

        public string? Name { get; }

        public IEventSource<BleException> LinkLost => _linkLost;

        public IEventSource<BleNotification> Notifications => _notifications;

        // La generación de la sesión del id que lo creó.
        internal int Generation { get; }

        internal bool IsClosed { get; private set; }

        public Task<IReadOnlyList<BleGattService>> DiscoverServicesAsync()
        {
            return _adapter.DiscoverAsync(this);
        }

        public Task SetNotifyAsync(BleUuid service, BleUuid characteristic, bool enable)
        {
            return _adapter.SetNotifyAsync(this, service, characteristic, enable);
        }

        public Task WriteAsync(BleUuid service, BleUuid characteristic, byte[] data, bool withResponse)
        {
            return _adapter.WriteAsync(this, service, characteristic, data, withResponse);
        }

        public Task DisconnectAsync()
        {
            _adapter.Close(this);
            return Task.CompletedTask;
        }

        public void StartNativeKeepAlive(BleUuid service, BleUuid characteristic, byte[] data, TimeSpan period)
        {
            _adapter.StartKeepAlive(this, service, characteristic, data, period);
        }

        public void StopNativeKeepAlive()
        {
            _adapter.StopKeepAlive(this);
        }

        public void Dispose()
        {
            _adapter.Close(this);
        }

        internal void MarkClosed()
        {
            IsClosed = true;
        }

        internal void RaiseLinkLost(BleException error)
        {
            _linkLost.Emit(error);
        }

        internal void RaiseNotification(BleNotification notification)
        {
            _notifications.Emit(notification);
        }
    }
}
