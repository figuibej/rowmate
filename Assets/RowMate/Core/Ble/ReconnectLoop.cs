using System;
using System.Threading.Tasks;
using RowMate.Core.Common;

namespace RowMate.Core.Ble
{
    // Reintenta attempt hasta que devuelva true, se agoten maxAttempts (null = sin límite) o se llame a Cancel (F-HRM-09). El primer
    // intento es inmediato al llamar a Start; los siguientes esperan interval DESDE EL FIN de cada intento fallido. Una excepción
    // en attempt cuenta como fallo. Sobre el IScheduler inyectado (FakeScheduler en las pruebas). Hilo principal.
    // Suma al legacy lo que la tarjeta necesita ver (A11-01): IsAttempting, NextAttemptAt, Run y el evento Changed.
    // legacy: lib/core/bluetooth/reconnect_loop.dart:8-71
    public sealed class ReconnectLoop : IDisposable
    {
        private const string Tag = "HRM";

        private readonly IScheduler _scheduler;
        private readonly TimeSpan _interval;
        private readonly Func<Task<bool>> _attempt;
        private readonly IAppLogger _logger;
        private readonly int? _maxAttempts;
        private readonly Action? _onGiveUp;

        private IDisposable? _timer;
        private bool _running;
        private bool _attempting;

        // rl:25-27: generación de la corrida actual. Un intento en vuelo de una corrida anterior (cancelada o reiniciada) descarta
        // su resultado.
        private int _generation;

        public ReconnectLoop(IScheduler scheduler, TimeSpan interval, Func<Task<bool>> attempt, IAppLogger logger, int? maxAttempts = null,
                             Action? onGiveUp = null)
        {
            _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
            _attempt = attempt ?? throw new ArgumentNullException(nameof(attempt));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            if (interval < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(interval), interval, "ReconnectLoop: el intervalo no puede ser negativo.");
            }

            _interval = interval;
            _maxAttempts = maxAttempts;
            _onGiveUp = onGiveUp;
        }

        // rl:29: hay corrida activa, también durante las esperas.
        public bool IsRunning => _running;

        // Hay un intento en vuelo de la corrida actual.
        public bool IsAttempting => _attempting;

        // rl:30
        public int Attempts { get; private set; }

        // Hora del IScheduler (MonotonicSeconds) del próximo intento; solo durante la espera.
        public double? NextAttemptAt { get; private set; }

        public TimeSpan Interval => _interval;

        // Sube en cada Start: quien mira el enlace distingue una corrida nueva (el aviso de caída sale una vez por corrida).
        public int Run { get; private set; }

        // Start, intento, espera, fin, rendición y Cancel.
        public event Action? Changed;

        // rl:33-38: arranca (o reinicia desde cero) la cadencia. El primer intento es inmediato.
        public void Start()
        {
            CancelCore();
            _running = true;
            Attempts = 0;
            Run++;
            TryOnceAsync(_generation).Forget(_logger, Tag);
        }

        // rl:42-47: frena los reintentos. Un intento en vuelo no se aborta, pero su resultado se descarta.
        public void Cancel()
        {
            var wasRunning = _running;
            CancelCore();
            if (wasRunning)
            {
                RaiseChanged();
            }
        }

        public void Dispose()
        {
            CancelCore();
            Changed = null;
        }

        private void CancelCore()
        {
            _generation++;
            _timer?.Dispose();
            _timer = null;
            _running = false;
            _attempting = false;
            NextAttemptAt = null;
        }

        // rl:49-70
        private async Task TryOnceAsync(int generation)
        {
            if (!_running || generation != _generation)
            {
                return;
            }

            Attempts++;
            _attempting = true;
            NextAttemptAt = null;
            RaiseChanged();
            bool ok;
            try
            {
                ok = await _attempt();
            }
            catch (Exception)
            {
                ok = false;
            }

            if (generation != _generation || !_running)
            {
                return; // corrida vieja o cancelada
            }

            _attempting = false;
            if (ok)
            {
                _running = false;
                RaiseChanged();
                return;
            }

            if (_maxAttempts.HasValue && Attempts >= _maxAttempts.Value)
            {
                _running = false;
                RaiseChanged();
                _onGiveUp?.Invoke();
                return;
            }

            NextAttemptAt = _scheduler.MonotonicSeconds + _interval.TotalSeconds;
            _timer = _scheduler.Schedule(_interval, () =>
            {
                _timer = null;
                TryOnceAsync(generation).Forget(_logger, Tag);
            });
            RaiseChanged();
        }

        private void RaiseChanged()
        {
            Changed?.Invoke();
        }
    }
}
