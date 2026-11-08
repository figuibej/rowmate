using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RowMate.Core.Common;
using RowMate.Core.Models;

namespace RowMate.Core.Ble
{
    // Puerto del remo: la superficie de BleService que usa la UI (02-tech-design §2.1). Lo implementan el remo simulado
    // (Simulator), el remo sin Bluetooth del build normal de la It. 2 (Unavailable) y, en la It. 10a, el remo real.
    // It. 10a: RowerService (Core, sobre IBleAdapter) es el remo real; suma Link y RequestEnableAsync.
    // Todo corre en el hilo principal (los callbacks nativos se despachan ahí). Los avisos llegan en el acto (D-04).
    // legacy: lib/core/bluetooth/ble_service.dart:10-60
    public interface IRowerService : IDisposable
    {
        // statusStream + status.
        IReadOnlyObservable<BleStatus> Status { get; }

        // adapterStateStream + adapterState.
        IReadOnlyObservable<BleAdapterState> AdapterState { get; }

        // dataStream: una lectura por notificación FTMS.
        IEventSource<RowingData> Data { get; }

        // devicesStream: remos encontrados en la búsqueda.
        IEventSource<IReadOnlyList<BleScanResult>> Devices { get; }

        // rawBytesStream: paquetes crudos para el diagnóstico (It. 10a).
        IEventSource<byte[]> RawBytes { get; }

        // Nombre del remo conectado, o null.
        string? ConnectedDeviceName { get; }

        // El momento en que la app empieza a escuchar (= onListen del legacy, D-05). Idempotente.
        void Start();

        // 10 s por defecto.
        Task StartScanAsync(TimeSpan? timeout = null);

        Task StopScanAsync();

        Task ConnectAsync(string deviceId);

        Task DisconnectAsync();

        // It. 10a (A10-01): el enlace con el remo real (fase, reconexión y motivo). El simulador y el remo sin Bluetooth no lo
        // tienen: siempre Idle (como IAppLifecycle.Woke, la implementación por defecto los deja igual).
        IReadOnlyObservable<RowerLink> Link => RowerLinkSource.Idle;

        // It. 10a (AC-BLE-04-04, Q-32): "Activar Bluetooth". Android: el pedido del sistema (o los ajustes de la app); Windows:
        // los ajustes de Bluetooth. Nunca lanza ni cambia la radio por su cuenta. Por defecto no hace nada.
        Task RequestEnableAsync() => Task.CompletedTask;
    }
}
