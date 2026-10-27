using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RowMate.Core.Common;

namespace RowMate.Tests.Support
{
    // Programador falso y determinista (ADR 005, AC-BASE-06; equivalente a fake_async del legacy).
    // El tiempo solo avanza con Advance/RunDue: 3 s simulados corren en microsegundos reales.
    // Advance(by) ejecuta en orden (vencimiento, después orden de alta) todos los timers que vencen hasta el
    // destino, incluidos los que agendan los propios callbacks; al terminar, el reloj queda en el destino.
    // Los periódicos disparan en múltiplos exactos del período. Delay se completa EN LÍNEA dentro de Advance,
    // así las continuaciones de un await corren ahí mismo (sin SynchronizationContext) y el test es sincrónico.
    public sealed class FakeScheduler : IScheduler
    {
        // Tope de callbacks por Advance: un timer de 0 que se reagenda a sí mismo no cuelga el test.
        public const int MaxCallbacksPerAdvance = 1000000;

        private static readonly DateTimeOffset DefaultStart = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        private static readonly TimeSpan DefaultLocalOffset = TimeSpan.FromHours(-3);

        private readonly DateTimeOffset _startUtc;
        private readonly TimeSpan _localOffset;
        private readonly List<Timer> _timers = new List<Timer>();
        private long _elapsedTicks;
        private long _nextSequence;

        // Por defecto arranca el 2026-01-01T00:00Z con hora local UTC−3.
        public FakeScheduler(DateTimeOffset? startUtc = null, TimeSpan? localOffset = null)
        {
            _startUtc = (startUtc ?? DefaultStart).ToUniversalTime();
            _localOffset = localOffset ?? DefaultLocalOffset;
        }

        public DateTimeOffset UtcNow => _startUtc.AddTicks(_elapsedTicks);

        public DateTimeOffset LocalNow => UtcNow.ToOffset(_localOffset);

        // Arranca en 0.
        public double MonotonicSeconds => _elapsedTicks / (double)TimeSpan.TicksPerSecond;

        public TimeSpan Elapsed => TimeSpan.FromTicks(_elapsedTicks);

        public bool IsCatchingUp { get; set; }

        public int PendingTimers
        {
            get
            {
                var count = 0;
                for (var i = 0; i < _timers.Count; i++)
                {
                    if (!_timers[i].Cancelled)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public IDisposable Schedule(TimeSpan delay, Action callback)
        {
            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            var ticks = Math.Max(0L, delay.Ticks);
            return Add(_elapsedTicks + ticks, 0, callback);
        }

        public IDisposable SchedulePeriodic(TimeSpan period, Action callback)
        {
            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            if (period <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(period), period, "FakeScheduler: el período tiene que ser positivo.");
            }

            return Add(_elapsedTicks + period.Ticks, period.Ticks, callback);
        }

        public Task Delay(TimeSpan delay, CancellationToken ct = default)
        {
            if (ct.IsCancellationRequested)
            {
                var canceled = new TaskCompletionSource<bool>();
                canceled.SetCanceled();
                return canceled.Task;
            }

            // Sin RunContinuationsAsynchronously: la continuación corre en línea dentro de Advance.
            var source = new TaskCompletionSource<bool>();
            CancellationTokenRegistration registration = default;
            var timer = (Timer)Schedule(delay, () =>
            {
                registration.Dispose();
                source.TrySetResult(true);
            });

            if (ct.CanBeCanceled)
            {
                registration = ct.Register(() =>
                {
                    timer.Dispose();
                    source.TrySetCanceled();
                });
            }

            return source.Task;
        }

        public void Advance(TimeSpan by)
        {
            if (by < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(by), by, "FakeScheduler: el tiempo no retrocede.");
            }

            var target = _elapsedTicks + by.Ticks;
            var executed = 0;
            while (true)
            {
                var next = NextDue(target);
                if (next == null)
                {
                    break;
                }

                if (++executed > MaxCallbacksPerAdvance)
                {
                    throw new InvalidOperationException("FakeScheduler: más de " + MaxCallbacksPerAdvance
                        + " callbacks en un Advance (¿un timer de 0 que se reagenda sin fin?).");
                }

                _elapsedTicks = next.DueTicks;
                if (next.PeriodTicks > 0)
                {
                    next.DueTicks += next.PeriodTicks; // múltiplos exactos del período; conserva su orden de alta
                }
                else
                {
                    next.Cancelled = true;
                    _timers.Remove(next);
                }

                next.Callback();
            }

            _elapsedTicks = target;
            _timers.RemoveAll(t => t.Cancelled);
        }

        // = Advance(TimeSpan.Zero): ejecuta lo vencido sin mover el reloj.
        public void RunDue()
        {
            Advance(TimeSpan.Zero);
        }

        private Timer? NextDue(long target)
        {
            Timer? best = null;
            for (var i = 0; i < _timers.Count; i++)
            {
                var timer = _timers[i];
                if (timer.Cancelled || timer.DueTicks > target)
                {
                    continue;
                }

                if (best == null || timer.DueTicks < best.DueTicks
                    || (timer.DueTicks == best.DueTicks && timer.Sequence < best.Sequence))
                {
                    best = timer;
                }
            }

            return best;
        }

        private Timer Add(long dueTicks, long periodTicks, Action callback)
        {
            var timer = new Timer(this, dueTicks, periodTicks, _nextSequence++, callback);
            _timers.Add(timer);
            return timer;
        }

        private sealed class Timer : IDisposable
        {
            private readonly FakeScheduler _owner;

            public Timer(FakeScheduler owner, long dueTicks, long periodTicks, long sequence, Action callback)
            {
                _owner = owner;
                DueTicks = dueTicks;
                PeriodTicks = periodTicks;
                Sequence = sequence;
                Callback = callback;
            }

            public long DueTicks { get; set; }

            public long PeriodTicks { get; }

            public long Sequence { get; }

            public Action Callback { get; }

            public bool Cancelled { get; set; }

            public void Dispose()
            {
                if (Cancelled)
                {
                    return;
                }

                Cancelled = true;
                _owner._timers.Remove(this);
            }
        }
    }
}
