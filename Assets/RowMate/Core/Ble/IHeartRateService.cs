using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RowMate.Core.Common;

namespace RowMate.Core.Ble
{
    // Puerto del pulsómetro: la superficie pública de HeartRateService (02-tech-design §2.1). Lo implementan el pulsómetro
    // simulado (Simulator), el del build normal de la It. 2 (Unavailable) y, en la It. 11, el real.
    // legacy: lib/core/bluetooth/heart_rate_service.dart:31-110
    public interface IHeartRateService : IDisposable
    {
        // statusStream + status. Reemite con ForceNotify cuando el legacy repite el estado (búsqueda repetida, forget).
        IReadOnlyObservable<HrmStatus> Status { get; }

        // bpmStream: cada lectura, aunque repita la anterior (AC-HRM-04-01); 0 = sin lectura válida.
        IEventSource<int> Bpm { get; }

        // bpm: el último publicado.
        int CurrentBpm { get; }

        // devicesStream: sensores encontrados.
        IEventSource<IReadOnlyList<BleScanResult>> Devices { get; }

        string? ConnectedDeviceName { get; }

        string? RememberedDeviceName { get; }

        bool IsRetrying { get; }

        // 10 s por defecto.
        Task StartScanAsync(TimeSpan? timeout = null);

        Task StopScanAsync();

        Task ConnectAsync(string deviceId);

        // Reconexión al sensor recordado (al arrancar, y "Conectar" de la tarjeta).
        Task AutoConnectAsync();

        Task DisconnectAsync();

        Task ForgetAsync();

        // It. 11 (A11-01, DT11-03): el enlace con el pulsómetro real (fase de reconexión, motivo, corrida, lectura vencida y quién
        // conectó). El simulado y Unavailable no lo tienen: siempre Idle (como IRowerService.Link), así el simulador no avisa nada.
        IReadOnlyObservable<HrmLink> Link => HrmLinkSource.Idle;
    }
}
