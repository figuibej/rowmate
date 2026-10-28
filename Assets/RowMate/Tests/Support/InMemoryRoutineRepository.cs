using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RowMate.Core.Models;
using RowMate.Core.Ports;

namespace RowMate.Tests.Support
{
    // IRoutineRepository en memoria, con las mismas reglas que SqliteRoutineRepository (orden created_at DESC, id DESC; ids
    // nuevos; pasos con RoutineId y Order 0…n−1; created_at truncado al milisegundo; UpdateAsync de una que no existe →
    // RoutineNotFoundException). Devuelve tareas ya terminadas. Para los tests de stores y de composición.
    public sealed class InMemoryRoutineRepository : IRoutineRepository
    {
        private readonly List<Routine> _routines = new List<Routine>();
        private long _nextRoutineId = 1;
        private long _nextStepId = 1;

        public InMemoryRoutineRepository(IEnumerable<Routine>? initial = null)
        {
            if (initial == null)
            {
                return;
            }

            foreach (var routine in initial)
            {
                Store(routine);
            }
        }

        public int GetAllCalls { get; private set; }

        public int Writes { get; private set; }

        // Lo guardado, en el orden de GetAllAsync.
        public IReadOnlyList<Routine> Snapshot => Sorted();

        public Task<IReadOnlyList<Routine>> GetAllAsync()
        {
            GetAllCalls++;
            return Task.FromResult(Sorted());
        }

        public Task<Routine?> GetByIdAsync(long id)
        {
            var index = IndexOf(id);
            return Task.FromResult(index >= 0 ? _routines[index] : (Routine?)null);
        }

        public Task<Routine> InsertAsync(Routine routine)
        {
            if (routine == null)
            {
                throw new ArgumentNullException(nameof(routine));
            }

            Writes++;
            return Task.FromResult(Store(routine));
        }

        public Task<Routine> UpdateAsync(Routine routine)
        {
            if (routine == null)
            {
                throw new ArgumentNullException(nameof(routine));
            }

            var index = routine.Id.HasValue ? IndexOf(routine.Id.Value) : -1;
            if (index < 0)
            {
                return Task.FromException<Routine>(new RoutineNotFoundException(routine.Id));
            }

            Writes++;
            var id = routine.Id!.Value;
            var saved = routine with { CreatedAt = _routines[index].CreatedAt, Steps = Number(routine.Steps, id) };
            _routines[index] = saved;
            return Task.FromResult(saved);
        }

        public Task DeleteAsync(long id)
        {
            Writes++;
            var index = IndexOf(id);
            if (index >= 0)
            {
                _routines.RemoveAt(index);
            }

            return Task.CompletedTask;
        }

        private Routine Store(Routine routine)
        {
            var id = _nextRoutineId++;
            var utc = routine.CreatedAt.ToUniversalTime();
            var createdAt = new DateTimeOffset(utc.Ticks - utc.Ticks % TimeSpan.TicksPerMillisecond, TimeSpan.Zero);
            var saved = routine with { Id = id, CreatedAt = createdAt, Steps = Number(routine.Steps, id) };
            _routines.Add(saved);
            return saved;
        }

        private IReadOnlyList<IntervalStep> Number(IReadOnlyList<IntervalStep> steps, long routineId)
        {
            var result = new IntervalStep[steps.Count];
            for (var i = 0; i < steps.Count; i++)
            {
                result[i] = steps[i] with { Id = _nextStepId++, RoutineId = routineId, Order = i };
            }

            return result;
        }

        private IReadOnlyList<Routine> Sorted()
        {
            var result = new List<Routine>(_routines);
            result.Sort((a, b) =>
            {
                var byDate = b.CreatedAt.UtcTicks.CompareTo(a.CreatedAt.UtcTicks);
                return byDate != 0 ? byDate : (b.Id ?? 0).CompareTo(a.Id ?? 0);
            });
            return result.ToArray();
        }

        private int IndexOf(long id)
        {
            for (var i = 0; i < _routines.Count; i++)
            {
                if (_routines[i].Id == id)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
