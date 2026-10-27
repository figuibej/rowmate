using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RowMate.Core.Ble;
using RowMate.Core.Common;
using RowMate.Core.Models;

namespace RowMate.App.Device
{
    // Estado del remo para Dispositivo (F-BLE-07, F-BLE-11). Puerto de DeviceProvider sin el vigilante de 5 s (:69-86,
    // F-BLE-09 → Core en la It. 10a, D-22) ni el diagnóstico (:19-22, :98-102, F-BLE-12 → It. 10a).
    // - Avisa en cada aviso del servicio, aunque el estado quede igual (el legacy llama a notifyListeners en cada evento:
    //   una lectura por segundo con el simulador, aunque repita los valores, AC-BLE-11-09).
    // - Al pasar a conectado reemplaza la lectura por RowingData.Empty (_resetSession, :38-42, :64-67): la grilla muestra
    //   ceros hasta la primera lectura de esa conexión.
    // - Las acciones capturan la excepción del servicio y la dejan en Error (:104-127). Después de Dispose no avisa nada.
    // legacy: lib/features/device/device_provider.dart:10-141
    public sealed class DeviceStore : IDisposable
    {
        private static readonly IReadOnlyList<BleScanResult> NoResults = new BleScanResult[0];

        private readonly IRowerService _rower;
        private readonly ObservableValue<DeviceState> _state;
        private readonly List<IDisposable> _subscriptions = new List<IDisposable>();
        private bool _disposed;

        public DeviceStore(IRowerService rower, IAppLogger logger)
        {
            _rower = rower ?? throw new ArgumentNullException(nameof(rower));
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            // El legacy arranca en disconnected / unknown, que es lo que valen los servicios de la It. 2 al crearse.
            _state = new ObservableValue<DeviceState>(
                new DeviceState(rower.Status.Value, rower.AdapterState.Value, RowingData.Empty, NoResults, null, rower.ConnectedDeviceName),
                logger);
            State = new ReadOnlyObservable<DeviceState>(_state);

            // Mismo orden de suscripción que el legacy (:34-61).
            _subscriptions.Add(rower.AdapterState.Subscribe(OnAdapterState, false));
            _subscriptions.Add(rower.Status.Subscribe(OnStatus, false));
            _subscriptions.Add(rower.Devices.Subscribe(OnDevices));
            _subscriptions.Add(rower.Data.Subscribe(OnData));
        }

        public IReadOnlyObservable<DeviceState> State { get; }

        // :104-114: borra el error y la lista, avisa y busca; una falla queda en Error.
        public async Task StartScanAsync()
        {
            if (_disposed)
            {
                return;
            }

            Publish(_state.Value with { Error = null, ScanResults = NoResults });
            try
            {
                await _rower.StartScanAsync();
            }
            catch (Exception error)
            {
                Publish(_state.Value with { Error = error });
            }
        }

        // :116
        public Task StopScanAsync()
        {
            return _rower.StopScanAsync();
        }

        // :118-127: borra el error, avisa y conecta; una falla queda en Error.
        public async Task ConnectAsync(string deviceId)
        {
            if (_disposed)
            {
                return;
            }

            Publish(_state.Value with { Error = null });
            try
            {
                await _rower.ConnectAsync(deviceId);
            }
            catch (Exception error)
            {
                Publish(_state.Value with { Error = error });
            }
        }

        // :129, sin confirmación (Q-33, AC-BLE-07-02).
        public Task DisconnectAsync()
        {
            return _rower.DisconnectAsync();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            for (var i = _subscriptions.Count - 1; i >= 0; i--)
            {
                _subscriptions[i].Dispose();
            }

            _subscriptions.Clear();
        }

        private void OnAdapterState(BleAdapterState adapterState)
        {
            Publish(_state.Value with { AdapterState = adapterState });
        }

        private void OnStatus(BleStatus status)
        {
            var current = _state.Value;
            Publish(current with { Status = status, Data = status == BleStatus.Connected ? RowingData.Empty : current.Data });
        }

        private void OnDevices(IReadOnlyList<BleScanResult> results)
        {
            Publish(_state.Value with { ScanResults = results ?? NoResults });
        }

        private void OnData(RowingData data)
        {
            Publish(_state.Value with { Data = data });
        }

        // Fotografía el nombre del servicio y avisa siempre (si el estado quedó igual, ForceNotify). Muerto, no avisa.
        private void Publish(DeviceState next)
        {
            if (_disposed)
            {
                return;
            }

            var before = _state.Value;
            _state.Value = next with { ConnectedDeviceName = _rower.ConnectedDeviceName };
            if (ReferenceEquals(before, _state.Value))
            {
                _state.ForceNotify();
            }
        }
    }
}
