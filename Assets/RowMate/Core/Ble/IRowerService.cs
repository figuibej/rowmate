using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RowMate.Core.Common;
using RowMate.Core.Models;

namespace RowMate.Core.Ble
{
    // Puerto del remo: la superficie de BleService que usa la UI (02-tech-design §2.1). Lo implementan el remo simulado
    // (Simulator), el remo sin Bluetooth del build normal de la It. 2 (Unavailable) y, en la It. 10a, el remo real.
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
    }
}
