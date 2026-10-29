using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RowMate.Core.Common
{
    // Timers de tiempo real del programador de la app (FrameScheduler, Core, desde la It. 4a; antes UnityScheduler;
    // 02-tech-design §2.1 de la It. 2, QA-G2-15). Pura y determinista: el tiempo lo pasa quien la avanza (FrameScheduler con
    // la hora del cuadro; los tests con números). Un solo hilo (el principal), sin locks.
    //
    // RunDue(ahora) corre los vencidos hasta "ahora", en orden (vencimiento, después orden de alta). Cada periódico dispara a
    // lo sumo una vez por llamada; su próximo vencimiento es vencimiento + período, salvo que eso ya haya pasado (atraso de un
    // período o más: el loop se cortó al arrastrar o redimensionar la ventana en Windows, o la app estuvo en pausa): entonces
    // se reancla en ahora + período. Las lecturas perdidas se pierden, no llegan comprimidas una por cuadro (es la conducta
    // de la It. 2 en primer plano; al volver del segundo plano, CatchUp hace el alcance de ADR 017, It. 4a). Lo que un
    // callback agenda durante un RunDue corre en el siguiente, aunque ya esté vencido: así un timer de 0 que se reagenda no
    // cuelga el cuadro.
    // La excepción de un callback no corta a los demás: se registra (tag TIMER), como un error de Timer en la zona de Dart.
    public sealed class RealtimeTimerQueue
    {
        public const string Tag = "TIMER";

        private readonly IAppLogger _logger;
        private readonly List<Entry> _entries = new List<Entry>();
        private long _nextSequence;
        private long _runGeneration;

        public RealtimeTimerQueue(IAppLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // Timers vivos (agendados y no cancelados ni disparados, si son de una vez).
        public int Pending
        {
            get
            {
                var count = 0;
                for (var i = 0; i < _entries.Count; i++)
                {
                    if (!_entries[i].Cancelled)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        // = Timer(delay, callback). Disponer el resultado lo cancela.
        public IDisposable Schedule(double nowSeconds, TimeSpan delay, Action callback)
        {
            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            var seconds = Math.Max(0.0, delay.TotalSeconds);
            return Add(nowSeconds + seconds, 0.0, callback);
        }

        // = Timer.periodic(period, callback). Disponer el resultado lo detiene.
        public IDisposable SchedulePeriodic(double nowSeconds, TimeSpan period, Action callback)
        {
            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            if (period <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(period), period, "RealtimeTimerQueue: el período tiene que ser positivo.");
            }

            return Add(nowSeconds + period.TotalSeconds, period.TotalSeconds, callback);
        }

        // = Future.delayed(delay): la tarea se completa en el RunDue que la vence. Cancelar el token la cancela.
        public Task Delay(double nowSeconds, TimeSpan delay, CancellationToken ct)
        {
            if (ct.IsCancellationRequested)
            {
                var canceled = new TaskCompletionSource<bool>();
                canceled.SetCanceled();
                return canceled.Task;
            }

            var source = new TaskCompletionSource<bool>();
            CancellationTokenRegistration registration = default;
            var entry = (Entry)Schedule(nowSeconds, delay, () =>
            {
                registration.Dispose();
                source.TrySetResult(true);
            });

            if (ct.CanBeCanceled)
            {
                registration = ct.Register(() =>
                {
                    entry.Dispose();
                    source.TrySetCanceled();
                });
            }

            return source.Task;
        }

        // Corre los vencidos hasta nowSeconds y devuelve cuántos callbacks corrieron.
        public int RunDue(double nowSeconds)
        {
            var generation = ++_runGeneration;
            var executed = 0;
            while (true)
            {
                var next = NextDue(nowSeconds, generation);
                if (next == null)
                {
                    break;
                }

                next.LastRun = generation;
                if (next.PeriodSeconds > 0)
                {
                    var following = next.DueSeconds + next.PeriodSeconds;
                    next.DueSeconds = following > nowSeconds ? following : nowSeconds + next.PeriodSeconds;
                }
                else
                {
                    next.Cancelled = true;
                }

                executed++;
                try
                {
                    next.Callback();
                }
                catch (Exception error)
                {
                    _logger.Log(LogLevel.Error, Tag, "un timer lanzó " + error.GetType().Name + ": " + error.Message, error);
                }
            }

            _entries.RemoveAll(e => e.Cancelled);
            return executed;
        }

        // Alcance al volver del segundo plano (ADR 017, decisión 3; 02-tech-design §4.1 de la It. 4a): repite "el timer vivo
        // de vencimiento más chico ≤ toSeconds" (empate: orden de alta), pone el reloj virtual en su vencimiento (setNow) y lo
        // dispara. Un periódico vuelve a vencer en vencimiento + período, SIN reanclar (dispara una vez por período perdido);
        // uno de una vez se va. A diferencia de RunDue, lo que se agenda DURANTE el alcance también corre si vence antes de
        // toSeconds: el timer del motor que se rearma cada segundo y las lecturas del remo simulado se intercalan en orden.
        // El reloj virtual nunca retrocede: un vencimiento anterior a fromSeconds (agendado entre cuadros) corre en
        // fromSeconds. Con más de maxCallbacks corta, lo registra como Error (TIMER) y reancla lo que queda en toSeconds +
        // período (los de una vez, en toSeconds: corren en el cuadro siguiente). Al terminar, el reloj queda en toSeconds.
        // Devuelve cuántos callbacks corrieron.
        public int CatchUp(double fromSeconds, double toSeconds, Action<double> setNow, int maxCallbacks)
        {
            if (setNow == null)
            {
                throw new ArgumentNullException(nameof(setNow));
            }

            if (maxCallbacks < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxCallbacks), maxCallbacks, "RealtimeTimerQueue: el tope tiene que ser positivo.");
            }

            var to = toSeconds < fromSeconds ? fromSeconds : toSeconds;
            var now = fromSeconds;
            setNow(now);
            ++_runGeneration;
            var executed = 0;
            while (true)
            {
                var index = NextCatchUp(to);
                if (index < 0)
                {
                    break;
                }

                if (executed >= maxCallbacks)
                {
                    _logger.Log(LogLevel.Error, Tag, "el alcance cortó después de " + maxCallbacks + " disparos; lo que quedaba vence después de la vuelta");
                    Reanchor(to);
                    break;
                }

                var next = _entries[index];
                if (next.DueSeconds > now)
                {
                    now = next.DueSeconds;
                    setNow(now);
                }

                if (next.PeriodSeconds > 0)
                {
                    next.DueSeconds += next.PeriodSeconds;
                }
                else
                {
                    // Se va de la lista en el acto (el último ocupa su lugar: el orden de la lista no importa, decide la
                    // secuencia): el timer de una vez que se rearma cada segundo no la hace crecer durante 8 h de alcance.
                    next.Cancelled = true;
                    var last = _entries.Count - 1;
                    _entries[index] = _entries[last];
                    _entries.RemoveAt(last);
                }

                executed++;
                try
                {
                    next.Callback();
                }
                catch (Exception error)
                {
                    _logger.Log(LogLevel.Error, Tag, "un timer lanzó " + error.GetType().Name + ": " + error.Message, error);
                }
            }

            setNow(to);
            _entries.RemoveAll(e => e.Cancelled);
            return executed;
        }

        // El índice del vivo de vencimiento más chico ≤ to (empate: orden de alta), sin mirar en qué llamada se creó; -1 si no hay.
        private int NextCatchUp(double to)
        {
            var best = -1;
            for (var i = 0; i < _entries.Count; i++)
            {
                var entry = _entries[i];
                if (entry.Cancelled || entry.DueSeconds > to)
                {
                    continue;
                }

                if (best < 0 || entry.DueSeconds < _entries[best].DueSeconds
                    || (entry.DueSeconds == _entries[best].DueSeconds && entry.Sequence < _entries[best].Sequence))
                {
                    best = i;
                }
            }

            return best;
        }

        private void Reanchor(double to)
        {
            for (var i = 0; i < _entries.Count; i++)
            {
                var entry = _entries[i];
                if (!entry.Cancelled && entry.DueSeconds <= to)
                {
                    entry.DueSeconds = to + entry.PeriodSeconds;
                }
            }
        }

        private Entry? NextDue(double nowSeconds, long generation)
        {
            Entry? best = null;
            for (var i = 0; i < _entries.Count; i++)
            {
                var entry = _entries[i];
                // Los creados durante esta llamada (CreatedIn == generation) y los periódicos que ya corrieron esperan al siguiente.
                if (entry.Cancelled || entry.LastRun == generation || entry.CreatedIn == generation || entry.DueSeconds > nowSeconds)
                {
                    continue;
                }

                if (best == null || entry.DueSeconds < best.DueSeconds
                    || (entry.DueSeconds == best.DueSeconds && entry.Sequence < best.Sequence))
                {
                    best = entry;
                }
            }

            return best;
        }

        private Entry Add(double dueSeconds, double periodSeconds, Action callback)
        {
            // Si se agenda dentro de un RunDue, queda marcado con esa llamada y no corre en ella.
            var entry = new Entry(this, dueSeconds, periodSeconds, _nextSequence++, callback, _runGeneration);
            _entries.Add(entry);
            return entry;
        }

        private sealed class Entry : IDisposable
        {
            private readonly RealtimeTimerQueue _owner;

            public Entry(RealtimeTimerQueue owner, double dueSeconds, double periodSeconds, long sequence, Action callback, long createdIn)
            {
                _owner = owner;
                DueSeconds = dueSeconds;
                PeriodSeconds = periodSeconds;
                Sequence = sequence;
                Callback = callback;
                CreatedIn = createdIn;
                LastRun = -1;
            }

            public double DueSeconds { get; set; }

            public double PeriodSeconds { get; }

            public long Sequence { get; }

            public Action Callback { get; }

            public long CreatedIn { get; }

            public long LastRun { get; set; }

            public bool Cancelled { get; set; }

            public void Dispose()
            {
                if (Cancelled)
                {
                    return;
                }

                Cancelled = true;
                _owner._entries.Remove(this);
            }
        }
    }
}
