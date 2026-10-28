using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RowMate.Core.Common;
using RowMate.Core.Models;
using RowMate.Core.Ports;

namespace RowMate.App.Routines
{
    // La colección de rutinas (F-RUT-09): puerto de RoutinesProvider (lib/features/routines/routines_provider.dart:6-53)
    // con Q-13 (errores visibles) y Q-18 (fecha en UTC).
    // - Arranca en Loading; AppContext.Start() llama LoadAsync() sin esperar, aunque nadie vaya a Rutinas (main.dart:83,
    //   :12-14; AC-RUT-09-01, AC-APP-01-09). Una carga en curso no se duplica (Reintentar mientras carga = nada).
    // - Carga que falla → LoadFailed con la lista vacía y "[DB] carga de rutinas: <motivo>" como Warning (AC-RUT-09-04).
    // - Crear: createdAt = CreationClock.UtcNow truncado al milisegundo, UNA lectura por llamada; al volver, la rutina va
    //   primera (:27-38). Editar: en su lugar (:40-46). Borrar: la quita cuando termina el borrado (:48-52).
    // - Guardar o borrar que fallan: Warning "[DB] guardado de rutina: …" / "[DB] baja de rutina: …" y null / false; la
    //   lista no cambia (AC-RUT-09-05, -06). Son fallas esperadas: un Error queda para lo que no se espera (QA-G3-09).
    // - Una escritura que sale bien con la fase en LoadFailed vuelve a cargar: si la base volvió, aparece la lista entera.
    // - Después de Dispose, lo que vuelve tarde no cambia el estado ni emite (QA-G3-09).
    // Todo corre en el hilo principal: el repositorio hace su trabajo afuera y la continuación vuelve por el
    // SynchronizationContext (ADR 005). El orden de llegada es el de ejecución (el ejecutor de la base es FIFO).
    public sealed class RoutinesStore : IDisposable
    {
        public const string Tag = "DB";

        private static readonly IReadOnlyList<Routine> NoRoutines = new Routine[0];

        private readonly IRoutineRepository _repository;
        private readonly IClock _creationClock;
        private readonly IAppLogger _logger;
        private readonly ObservableValue<RoutinesState> _state;
        private Task? _loading;
        private bool _disposed;

        public RoutinesStore(IRoutineRepository repository, IClock creationClock, IAppLogger logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _creationClock = creationClock ?? throw new ArgumentNullException(nameof(creationClock));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _state = new ObservableValue<RoutinesState>(new RoutinesState(RoutinesPhase.Loading, NoRoutines), logger);
            State = new ReadOnlyObservable<RoutinesState>(_state);
        }

        public IReadOnlyObservable<RoutinesState> State { get; }

        // :19-25 + Q-13.
        public Task LoadAsync()
        {
            if (_disposed)
            {
                return Task.CompletedTask;
            }

            if (_loading != null)
            {
                return _loading;
            }

            var task = LoadCoreAsync();
            if (!task.IsCompleted)
            {
                _loading = task;
            }

            return task;
        }

        // :27-38. null = no se pudo guardar (el editor queda abierto con todo, AC-RUT-09-05).
        public async Task<Routine?> CreateAsync(string name, string description, IReadOnlyList<IntervalStep> steps)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (description == null)
            {
                throw new ArgumentNullException(nameof(description));
            }

            if (steps == null)
            {
                throw new ArgumentNullException(nameof(steps));
            }

            if (_disposed)
            {
                return null;
            }

            var routine = new Routine(name, TruncateToMilliseconds(_creationClock.UtcNow)) { Description = description, Steps = steps };
            Routine saved;
            try
            {
                saved = await _repository.InsertAsync(routine);
            }
            catch (Exception error)
            {
                _logger.Log(LogLevel.Warning, Tag, "guardado de rutina: " + error.Message, error);
                return null;
            }

            if (_disposed)
            {
                return saved;
            }

            if (ReloadIfTheLoadFailed())
            {
                return saved;
            }

            var current = _state.Value.Routines;
            var list = new List<Routine>(current.Count + 1) { saved };
            list.AddRange(current);
            Publish(new RoutinesState(_state.Value.Phase, list));
            return saved;
        }

        // :40-46. null = no se pudo guardar.
        public async Task<Routine?> UpdateAsync(Routine routine)
        {
            if (routine == null)
            {
                throw new ArgumentNullException(nameof(routine));
            }

            if (_disposed)
            {
                return null;
            }

            Routine saved;
            try
            {
                saved = await _repository.UpdateAsync(routine);
            }
            catch (Exception error)
            {
                _logger.Log(LogLevel.Warning, Tag, "guardado de rutina: " + error.Message, error);
                return null;
            }

            if (_disposed || ReloadIfTheLoadFailed())
            {
                return saved;
            }

            var current = _state.Value.Routines;
            var index = IndexOf(current, routine.Id);
            if (index >= 0)
            {
                var list = new List<Routine>(current);
                list[index] = saved;
                Publish(new RoutinesState(_state.Value.Phase, list));
            }

            return saved;
        }

        // :48-52 (+ Q-13). false = no se pudo borrar: la tarjeta sigue (AC-RUT-09-06).
        public async Task<bool> DeleteAsync(long id)
        {
            if (_disposed)
            {
                return false;
            }

            try
            {
                await _repository.DeleteAsync(id);
            }
            catch (Exception error)
            {
                _logger.Log(LogLevel.Warning, Tag, "baja de rutina: " + error.Message, error);
                return false;
            }

            if (_disposed || ReloadIfTheLoadFailed())
            {
                return true;
            }

            var current = _state.Value.Routines;
            var index = IndexOf(current, id);
            if (index >= 0)
            {
                var list = new List<Routine>(current);
                list.RemoveAt(index);
                Publish(new RoutinesState(_state.Value.Phase, list));
            }

            return true;
        }

        // La rutina con ese id en la lista actual, o null.
        public Routine? Find(long id)
        {
            var current = _state.Value.Routines;
            var index = IndexOf(current, id);
            return index >= 0 ? current[index] : null;
        }

        public void Dispose()
        {
            _disposed = true;
        }

        // Q-18: la fecha de creación se guarda con milisegundos fijos (DT-08), así el orden del texto es el del tiempo.
        internal static DateTimeOffset TruncateToMilliseconds(DateTimeOffset value)
        {
            var utc = value.ToUniversalTime();
            return new DateTimeOffset(utc.Ticks - utc.Ticks % TimeSpan.TicksPerMillisecond, TimeSpan.Zero);
        }

        private async Task LoadCoreAsync()
        {
            Publish(new RoutinesState(RoutinesPhase.Loading, _state.Value.Routines));
            try
            {
                var routines = await _repository.GetAllAsync();
                if (!_disposed)
                {
                    Publish(new RoutinesState(RoutinesPhase.Ready, routines ?? NoRoutines));
                }
            }
            catch (Exception error)
            {
                if (!_disposed)
                {
                    _logger.Log(LogLevel.Warning, Tag, "carga de rutinas: " + error.Message, error);
                    Publish(new RoutinesState(RoutinesPhase.LoadFailed, NoRoutines));
                }
            }
            finally
            {
                _loading = null;
            }
        }

        // Una escritura que sale bien con la fase en LoadFailed: la base volvió, se carga todo de nuevo (no se arma una
        // lista parcial con lo que se acaba de escribir).
        private bool ReloadIfTheLoadFailed()
        {
            if (_state.Value.Phase != RoutinesPhase.LoadFailed)
            {
                return false;
            }

            LoadAsync().Forget(_logger, Tag);
            return true;
        }

        private void Publish(RoutinesState state)
        {
            if (!_disposed)
            {
                _state.Value = state;
            }
        }

        private static int IndexOf(IReadOnlyList<Routine> routines, long? id)
        {
            if (!id.HasValue)
            {
                return -1;
            }

            for (var i = 0; i < routines.Count; i++)
            {
                if (routines[i].Id == id)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
