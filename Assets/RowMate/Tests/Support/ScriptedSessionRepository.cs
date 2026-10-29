using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RowMate.Core.Common;
using RowMate.Core.Models;
using RowMate.Core.Ports;

namespace RowMate.Tests.Support
{
    // Base de sesiones de guion (spec §0.4 "base que falla o tarda a pedido"; 02-tech-design §8.1 de la It. 4a): sobre
    // InMemorySessionRepository, ejecuta las operaciones de a una y en el orden en que se llaman (el contrato FIFO del
    // puerto), cada una con su demora en el programador (FakeScheduler: avanzan con Advance) y fallas a pedido: al crear, en
    // el lote n (una vez o desde ahí siempre) o al cerrar; Never deja la cola colgada (una base que no contesta). Con demora 0
    // y la cola vacía, la operación vuelve ya terminada (como la base en línea). Cuenta las llamadas y el orden.
    public sealed class ScriptedSessionRepository : ISessionRepository
    {
        private readonly IScheduler _scheduler;
        private readonly Queue<Operation> _queue = new Queue<Operation>();
        private readonly List<string> _calls = new List<string>();
        private bool _running;
        private int _batches;

        public ScriptedSessionRepository(IScheduler scheduler)
        {
            _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
        }

        // Lo que quedó escrito.
        public InMemorySessionRepository Store { get; } = new InMemorySessionRepository();

        public TimeSpan CreateDelay { get; set; }

        public TimeSpan WriteDelay { get; set; }

        public TimeSpan FinishDelay { get; set; }

        public bool FailOnCreate { get; set; }

        public bool FailOnFinish { get; set; }

        // El cierre falla solo si trae algún punto a reintentar (el mismo punto que falla otra vez, QA-G4aT-13).
        public bool FailFinishWithRetriedPoints { get; set; }

        // El lote n (desde 1) falla; con FailBatchesFrom, ese y todos los siguientes.
        public int FailBatch { get; set; }

        public bool FailBatchesFrom { get; set; }

        // La operación que llega con esto en true no termina nunca, y la cola queda colgada detrás.
        public bool Never { get; set; }

        // "create", "points:n", "after-create:n", "finish:n" en el orden en que se LLAMARON.
        public IReadOnlyList<string> Calls => _calls.ToArray();

        public int Pending => _queue.Count;

        public int WaitCalls { get; private set; }

        public Task<long> CreateAsync(WorkoutSession session)
        {
            _calls.Add("create");
            var copy = session;
            return Enqueue(CreateDelay, () =>
            {
                if (FailOnCreate)
                {
                    throw new InvalidOperationException("base de guion: falla el alta");
                }

                return Store.Create(copy);
            });
        }

        public Task AddPointsAsync(IReadOnlyList<DataPoint> points)
        {
            var copy = new List<DataPoint>(points);
            _calls.Add("points:" + copy.Count);
            var number = ++_batches;
            return Enqueue(WriteDelay, () =>
            {
                CheckBatch(number);
                Store.AddPoints(copy, null, "points");
                return true;
            });
        }

        public Task AddPointsAfterCreateAsync(Task<long> creation, IReadOnlyList<DataPoint> points)
        {
            var copy = new List<DataPoint>(points);
            _calls.Add("after-create:" + copy.Count);
            var number = ++_batches;
            return Enqueue(WriteDelay, () =>
            {
                if (!creation.IsCompleted)
                {
                    throw new InvalidOperationException("base de guion: el alta todavía no terminó (FIFO)");
                }

                if (creation.IsFaulted || creation.IsCanceled)
                {
                    return false;
                }

                CheckBatch(number);
                Store.AddPoints(copy, creation.Result, "after-create");
                return true;
            });
        }

        public Task FinishAsync(IReadOnlyList<DataPoint> points, WorkoutSession closed)
        {
            var copy = new List<DataPoint>(points);
            _calls.Add("finish:" + copy.Count);
            return Enqueue(FinishDelay, () =>
            {
                if (FailOnFinish || (FailFinishWithRetriedPoints && copy.Count > 1))
                {
                    throw new InvalidOperationException("base de guion: falla el cierre");
                }

                Store.Finish(copy, closed);
                return true;
            });
        }

        public Task<WorkoutSession?> GetByIdAsync(long id)
        {
            return Store.GetByIdAsync(id);
        }

        public Task<IReadOnlyList<DataPoint>> GetPointsAsync(long sessionId)
        {
            return Store.GetPointsAsync(sessionId);
        }

        public Task<IReadOnlyList<SessionListing>> ListAsync()
        {
            return Store.ListAsync();
        }

        // No puede esperar (un solo hilo, el programador falso no avanza solo): dice si la cola quedó vacía.
        public bool WaitForPendingWork(TimeSpan timeout)
        {
            WaitCalls++;
            return _queue.Count == 0;
        }

        private void CheckBatch(int number)
        {
            if (FailBatch > 0 && (number == FailBatch || (FailBatchesFrom && number > FailBatch)))
            {
                throw new InvalidOperationException("base de guion: falla el lote " + number);
            }
        }

        private Task<T> Enqueue<T>(TimeSpan delay, Func<T> work)
        {
            var source = new TaskCompletionSource<T>();
            _queue.Enqueue(new Operation(delay, Never, () =>
            {
                try
                {
                    source.TrySetResult(work());
                }
                catch (Exception error)
                {
                    source.TrySetException(error);
                }
            }));
            if (!_running)
            {
                RunNext();
            }

            return source.Task;
        }

        private void RunNext()
        {
            _running = true;
            while (_queue.Count > 0)
            {
                var operation = _queue.Peek();
                if (operation.Never)
                {
                    return;
                }

                if (operation.Delay > TimeSpan.Zero && !operation.Waited)
                {
                    operation.Waited = true;
                    _scheduler.Schedule(operation.Delay, RunNext);
                    return;
                }

                _queue.Dequeue();
                operation.Run();
            }

            _running = false;
        }

        private sealed class Operation
        {
            public Operation(TimeSpan delay, bool never, Action run)
            {
                Delay = delay;
                Never = never;
                Run = run;
            }

            public TimeSpan Delay { get; }

            public bool Never { get; }

            public Action Run { get; }

            public bool Waited { get; set; }
        }
    }
}
