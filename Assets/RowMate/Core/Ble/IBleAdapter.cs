using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RowMate.Core.Common;

namespace RowMate.Core.Ble
{
    // Puerto del adaptador Bluetooth LE (ARCHITECTURE §10.2, afinado en 10a 02-tech-design §2.1): lo que RowerService (y el
    // pulsómetro de la It. 11) necesitan de flutter_blue_plus. Lo implementan NativeBleAdapter (Windows y Android, sobre
    // INativeBleApi) y FakeBleAdapter (pruebas). Todo en el hilo principal; ningún método empareja, desempareja ni cambia el
    // estado de la radio (AC-BLE-SEC-01).
    public interface IBleAdapter : IDisposable
    {
        // Unsupported = el equipo no tiene Bluetooth LE (Q-47). Unauthorized solo después de una negativa (DT10-15).
        IReadOnlyObservable<BleAdapterState> State { get; }

        // Android filtra la búsqueda por servicio en el sistema; Windows no (ScanFilter en la app, scan_filter.dart:12).
        bool FiltersScanInOs { get; }

        // Android necesita el keepalive nativo en segundo plano (DT10-09, ADR 017); Windows no (el loop sigue minimizado).
        bool NeedsNativeKeepAlive { get; }

        // Solo los resultados del escaneo en curso, sin reemitir los de uno anterior (ble_service.dart:97-99).
        IEventSource<BleScanResult> ScanResults { get; }

        // El fallo asíncrono del escaneo (onScanFailed de Android, Stopped con error de Windows).
        IEventSource<BleException> ScanFailed { get; }

        // Windows y sin permisos: true. Android: lo pide si hace falta (DT10-16); false = negado (State pasa a Unauthorized).
        Task<bool> EnsureScanPermissionAsync();

        // Reemplaza el escaneo en curso. No pide permiso.
        Task StartScanAsync(IReadOnlyList<BleUuid> services);

        Task StopScanAsync();

        // Android: permiso → diálogo de encender, o ajustes de la app; Windows: ajustes de Bluetooth del sistema (Q-32).
        Task RequestEnableAsync();

        // Sin tope propio: el plazo lo pone Core y lo aplica con ct. Cancelado → cierra la sesión nativa por id y la tarea
        // termina con OperationCanceledException (DT10-17).
        Task<IBlePeripheral> ConnectAsync(string deviceId, bool requestHighMtu, CancellationToken ct);
    }
}
