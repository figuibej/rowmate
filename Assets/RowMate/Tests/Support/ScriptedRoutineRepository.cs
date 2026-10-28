using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RowMate.Core.Common;
using RowMate.Core.Models;
using RowMate.Core.Ports;

namespace RowMate.Tests.Support
{
    // La "base que falla a pedido" de la spec (§0.4; 02-tech-design §5.2): sobre un InMemoryRoutineRepository, con demoras
    // por el scheduler (FakeScheduler: avanzan con Advance; Never = no termina nunca) y fallas a pedido al cargar, guardar o
    // borrar. Cuenta las llamadas. La falla llega después de la demora, como una base lenta que termina mal.
    public sealed class ScriptedRoutineRepository : IRoutineRepository
    {
        // Demora que no termina: el indicador de carga para siempre (CAP-02) o un guardado que nunca vuelve.
        public static readonly TimeSpan Never = TimeSpan.MaxValue;

        private readonly IScheduler _scheduler;
        private readonly InMemoryRoutineRepository _data;

        public ScriptedRoutineRepository(IScheduler scheduler, IEnumerable<Routine>? initial = null)
        {
            _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
            _data = new InMemoryRoutineRepository(initial);
        }

        public TimeSpan LoadDelay { get; set; } = TimeSpan.Zero;

        public TimeSpan SaveDelay { get; set; } = TimeSpan.Zero;

        public TimeSpan DeleteDelay { get; set; } = TimeSpan.Zero;

        public bool FailLoad { get; set; }

        public bool FailSave { get; set; }

        public bool FailDelete { get; set; }

        public int LoadCalls { get; private set; }

        public int GetByIdCalls { get; private set; }

        public int InsertCalls { get; private set; }

        public int UpdateCalls { get; private set; }

        public int DeleteCalls { get; private set; }

        // Lo guardado de verdad, en el orden de la lista.
        public IReadOnlyList<Routine> Snapshot => _data.Snapshot;

        public Task<IReadOnlyList<Routine>> GetAllAsync()
        {
            LoadCalls++;
            return After(LoadDelay, () => FailLoad ? throw new InvalidOperationException("falla de prueba al cargar") : _data.GetAllAsync());
        }

        public Task<Routine?> GetByIdAsync(long id)
        {
            GetByIdCalls++;
            return After(LoadDelay, () => FailLoad ? throw new InvalidOperationException("falla de prueba al cargar") : _data.GetByIdAsync(id));
        }

        public Task<Routine> InsertAsync(Routine routine)
        {
            InsertCalls++;
            return After(SaveDelay, () => FailSave ? throw new InvalidOperationException("falla de prueba al guardar") : _data.InsertAsync(routine));
        }

        public Task<Routine> UpdateAsync(Routine routine)
        {
            UpdateCalls++;
            return After(SaveDelay, () => FailSave ? throw new InvalidOperationException("falla de prueba al guardar") : _data.UpdateAsync(routine));
        }

        public Task DeleteAsync(long id)
        {
            DeleteCalls++;
            return After(DeleteDelay, async () =>
            {
                if (FailDelete)
                {
                    throw new InvalidOperationException("falla de prueba al borrar");
                }

                await _data.DeleteAsync(id);
                return true;
            });
        }

        // La operación corre DESPUÉS de la demora (la falla también): así lo que se lee o se escribe es lo del momento en que
        // la "base" responde.
        private async Task<T> After<T>(TimeSpan delay, Func<Task<T>> operation)
        {
            if (delay == Never)
            {
                await new TaskCompletionSource<bool>().Task;
            }
            else if (delay > TimeSpan.Zero)
            {
                await _scheduler.Delay(delay);
            }

            return await operation();
        }
    }
}
