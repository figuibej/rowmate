using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using RowMate.Core.Common;

namespace RowMate.Platform.Time
{
    // IScheduler de la app (ADR 005, 02-tech-design §1.1 y §2.1): reloj monótono con Stopwatch y una RealtimeTimerQueue
    // (Core, pura) que avanza en Tick(), llamado una vez por cuadro desde AppRoot.Update, en el hilo principal.
    // - A lo sumo un disparo de cada periódico por cuadro; si el atraso llega a un período (el loop se cortó al arrastrar o
    //   redimensionar la ventana en Windows, o una pausa en Android), la cola lo reancla en ahora + período (QA-G2-15):
    //   las lecturas perdidas se pierden, no llegan comprimidas una por cuadro.
    // - IsCatchingUp = false: el alcance del tiempo perdido en segundo plano es de la It. 4a (ADR 017).
    // - Hora local del sistema (UtcNow/LocalNow): DateTimeOffset.Now, solo acá (el Core no toca el reloj del sistema).
    public sealed class UnityScheduler : IScheduler
    {
        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
        private readonly RealtimeTimerQueue _queue;

        public UnityScheduler(IAppLogger logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _queue = new RealtimeTimerQueue(logger);
        }

        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

        public DateTimeOffset LocalNow => DateTimeOffset.Now;

        public double MonotonicSeconds => _stopwatch.Elapsed.TotalSeconds;

        public bool IsCatchingUp => false;

        public int Pending => _queue.Pending;

        public IDisposable Schedule(TimeSpan delay, Action callback)
        {
            return _queue.Schedule(MonotonicSeconds, delay, callback);
        }

        public IDisposable SchedulePeriodic(TimeSpan period, Action callback)
        {
            return _queue.SchedulePeriodic(MonotonicSeconds, period, callback);
        }

        public Task Delay(TimeSpan delay, CancellationToken ct = default)
        {
            return _queue.Delay(MonotonicSeconds, delay, ct);
        }

        // Corre lo vencido hasta ahora. Devuelve cuántos callbacks corrieron.
        public int Tick()
        {
            return _queue.RunDue(MonotonicSeconds);
        }
    }
}
