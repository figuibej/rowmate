using System;
using RowMate.Core.Ble;

namespace RowMate.App.Composition
{
    // El par remo + pulsómetro con el que se arma la app (02-tech-design §2.8) y su dueño. Lo crea quien elige la variante
    // (DeviceBackendFactory en Bootstrap: el único #if ROWMATE_SIMULATOR; los tests y el arnés con sus fábricas) y lo dispone
    // él mismo, después de la app (AppContext no lo dispone). Dispose libera los dos servicios (cancela todos sus timers,
    // AC-APP-01-08) y el dueño, una sola vez.
    // legacy: lib/main.dart:51-54 (ble / hrm según kSimulator), :60-79 (dispose de los Provider)
    public class DeviceBackend : IDisposable
    {
        private IDisposable? _owner;
        private bool _disposed;

        public DeviceBackend(IRowerService rower, IHeartRateService heartRate, IDisposable? owner)
        {
            Rower = rower ?? throw new ArgumentNullException(nameof(rower));
            HeartRate = heartRate ?? throw new ArgumentNullException(nameof(heartRate));
            _owner = owner;
        }

        public IRowerService Rower { get; }

        public IHeartRateService HeartRate { get; }

        public bool IsDisposed => _disposed;

        public virtual void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            HeartRate.Dispose();
            Rower.Dispose();
            var owner = _owner;
            _owner = null;
            owner?.Dispose();
        }
    }
}
