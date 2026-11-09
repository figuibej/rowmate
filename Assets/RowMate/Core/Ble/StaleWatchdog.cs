using System;
using RowMate.Core.Common;

namespace RowMate.Core.Ble
{
    // Llama a onStale si pasan timeout sin un Touch (F-HRM-05). Cada Touch rearma el plazo; Stop lo desactiva. Dispara una sola vez
    // por Touch. Sobre el IScheduler inyectado; hilo principal.
    // DT11-02: por PLAZO y no con un timer nuevo por lectura. Touch solo mueve _deadline y deja vivo el único temporizador; al vencer,
    // si el plazo se movió, se reagenda por lo que falta. Misma semántica que el legacy (LT-145) sin reservar memoria por lectura.
    // legacy: lib/core/bluetooth/reconnect_loop.dart:75-91
    public sealed class StaleWatchdog : IDisposable
    {
        private readonly IScheduler _scheduler;
        private readonly long _timeoutTicks;
        private readonly Action _onStale;
        private readonly Action _onTimer;

        private IDisposable? _timer;
        private long _deadlineTicks;
        private bool _armed;

        public StaleWatchdog(IScheduler scheduler, TimeSpan timeout, Action onStale)
        {
            _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
            _onStale = onStale ?? throw new ArgumentNullException(nameof(onStale));
            if (timeout <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(timeout), timeout, "StaleWatchdog: el plazo tiene que ser positivo.");
            }

            _timeoutTicks = timeout.Ticks;
            _onTimer = OnTimer;
        }

        public bool IsArmed => _armed;

        // rl:82-85
        public void Touch()
        {
            _deadlineTicks = NowTicks() + _timeoutTicks;
            _armed = true;
            if (_timer == null)
            {
                _timer = _scheduler.Schedule(TimeSpan.FromTicks(_timeoutTicks), _onTimer);
            }
        }

        // rl:87-90
        public void Stop()
        {
            _armed = false;
            _timer?.Dispose();
            _timer = null;
        }

        public void Dispose()
        {
            Stop();
        }

        private void OnTimer()
        {
            _timer = null;
            if (!_armed)
            {
                return;
            }

            var remaining = _deadlineTicks - NowTicks();
            if (remaining > 0)
            {
                _timer = _scheduler.Schedule(TimeSpan.FromTicks(remaining), _onTimer);
                return;
            }

            _armed = false;
            _onStale();
        }

        private long NowTicks()
        {
            // Al tick más cercano (el monótono no es negativo).
            return (long)(_scheduler.MonotonicSeconds * TimeSpan.TicksPerSecond + 0.5);
        }
    }
}
