using System;
using RowMate.Core.Ports;
using RowMate.Core.Text;

namespace RowMate.Core.Common
{
    // Tope de espera que NO cuenta el tiempo en segundo plano (02-tech-design §4.1 de la It. 4a; spec §12 punto 2;
    // QA-G4aT-03). El alcance (FrameScheduler.ResumeFromBackground) dispara en tiempo virtual todo lo que vence, también lo
    // que se agenda mientras dura: es lo correcto para el motor y el remo, pero no para un tope que espera algo de AFUERA (la
    // base, en su hilo), cuya respuesta vuelve al hilo principal recién después del alcance. Por eso:
    // - con InBackground = false corre como un Schedule;
    // - al pasar a true se detiene y guarda lo que le queda;
    // - al volver a false (en la app, DESPUÉS del alcance: AppRoot llama primero ResumeFromBackground) se rearma con eso,
    //   desde la hora de la vuelta;
    // - creado durante el segundo plano (el cierre de una rutina que termina en el alcance), arranca entero al volver.
    // Dispose lo cancela (la espera terminó antes). Hilo principal. Dispara una sola vez.
    public sealed class ForegroundTimeout : IDisposable
    {
        private readonly IScheduler _scheduler;
        private readonly Action _onTimeout;
        private readonly IDisposable _subscription;
        private IDisposable? _timer;
        private TimeSpan _remaining;
        private double _armedAt;
        private bool _disposed;

        public ForegroundTimeout(IScheduler scheduler, IAppLifecycle lifecycle, TimeSpan timeout, Action onTimeout)
        {
            _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
            if (lifecycle == null)
            {
                throw new ArgumentNullException(nameof(lifecycle));
            }

            _onTimeout = onTimeout ?? throw new ArgumentNullException(nameof(onTimeout));
            _remaining = timeout < TimeSpan.Zero ? TimeSpan.Zero : timeout;
            _subscription = lifecycle.InBackground.Subscribe(OnBackgroundChanged, emitCurrent: false);
            if (!lifecycle.InBackground.Value)
            {
                Arm();
            }
        }

        public bool HasFired { get; private set; }

        // Lo que falta, en tiempo de primer plano (con el tope corriendo, descontado lo que ya pasó desde que se armó).
        public TimeSpan Remaining
        {
            get
            {
                if (HasFired)
                {
                    return TimeSpan.Zero;
                }

                if (_timer == null)
                {
                    return _remaining;
                }

                // En ticks (TimeSpan.FromSeconds redondea al milisegundo en Mono).
                var elapsed = DartMath.Round((_scheduler.MonotonicSeconds - _armedAt) * TimeSpan.TicksPerSecond);
                var left = _remaining - TimeSpan.FromTicks(elapsed);
                return left < TimeSpan.Zero ? TimeSpan.Zero : left;
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _timer?.Dispose();
            _timer = null;
            _subscription.Dispose();
        }

        private void Arm()
        {
            _armedAt = _scheduler.MonotonicSeconds;
            _timer = _scheduler.Schedule(_remaining, Fire);
        }

        private void OnBackgroundChanged(bool inBackground)
        {
            if (_disposed || HasFired)
            {
                return;
            }

            if (inBackground)
            {
                if (_timer != null)
                {
                    _remaining = Remaining;
                    _timer.Dispose();
                    _timer = null;
                }

                return;
            }

            if (_timer == null)
            {
                Arm();
            }
        }

        private void Fire()
        {
            _timer = null;
            if (_disposed || HasFired)
            {
                return;
            }

            HasFired = true;
            _subscription.Dispose();
            _onTimeout();
        }
    }
}
