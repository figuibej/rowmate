using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RowMate.App.Shell;
using RowMate.Core.Common;
using RowMate.Core.Models;
using RowMate.Core.Ports;
using RowMate.Core.Workout;

namespace RowMate.App.History
{
    // La lista del historial (F-HIS-06, F-HIS-07; 02-tech-design §3.1 de la It. 9): puerto de HistoryProvider
    // (lib/features/history/history_provider.dart) con Q-13 (errores visibles) y Q-22 (sin parpadeo).
    // - Arranca en Loading con la lista vacía. Recarga (LoadAsync) al arrancar (AppContext.Start, main.dart:84), al TOCAR la
    //   pestaña Historial aunque ya esté elegida (ShellStore.Tapped, main.dart:146-148), al volver del detalle (lo llama la
    //   página al salir, hs:53-56) y cuando el cierre de una sesión vuelve (Close → Saved o Failed: una sesión con el cierre
    //   fallado queda cortada con sus puntos y también se lista, DT9-05). La cortada por cerrar la app aparece con la carga del
    //   arranque.
    // - Coalescente (DT9-03): si hay una lectura en vuelo, se marca UNA pendiente que corre al terminar; así la última lectura
    //   siempre es posterior al último pedido.
    // - Con una lista a la vista la fase nunca vuelve a Loading (el indicador sale solo en la primera carga, AC-HIS-06-03). Si
    //   la lista que llega es igual (por valor), el estado no cambia y no emite.
    // - Falla sin lista → LoadFailed (texto y Reintentar) y "[DB] carga del historial: <motivo>" como Warning; falla con lista →
    //   la lista queda igual y ReloadFailed (el aviso de 4 s, DT9-06).
    // - Después de Dispose, lo que vuelve tarde no cambia el estado ni avisa (patrón de RoutinesStore).
    // Todo corre en el hilo principal: el repositorio calcula en el hilo de la base y la continuación vuelve por el
    // SynchronizationContext (ADR 005).
    public sealed class HistoryStore : IDisposable
    {
        public const string Tag = "DB";

        private static readonly IReadOnlyList<HistoryEntry> NoEntries = new HistoryEntry[0];

        private readonly ISessionRepository _sessions;
        private readonly ShellStore _shell;
        private readonly IAppLogger _logger;
        private readonly ObservableValue<HistoryState> _state;
        private readonly IDisposable _workoutSubscription;
        private CloseStatus _lastClose;
        private WorkoutPhase _lastPhase;
        private Task? _inFlight;
        private bool _pending;
        private bool _disposed;

        public HistoryStore(ISessionRepository sessions, ShellStore shell, IReadOnlyObservable<WorkoutSnapshot> workout, IAppLogger logger)
        {
            _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
            _shell = shell ?? throw new ArgumentNullException(nameof(shell));
            if (workout == null)
            {
                throw new ArgumentNullException(nameof(workout));
            }

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _state = new ObservableValue<HistoryState>(new HistoryState(HistoryPhase.Loading, NoEntries), logger);
            State = new ReadOnlyObservable<HistoryState>(_state);
            _lastClose = workout.Value.Close;
            _lastPhase = workout.Value.Phase;
            _workoutSubscription = workout.Subscribe(OnWorkout, emitCurrent: false);
            _shell.Tapped += OnTapped;
        }

        public IReadOnlyObservable<HistoryState> State { get; }

        // Una recarga falló con una lista a la vista (la lista quedó igual): el aviso historyLoadError de 4 s.
        public event Action? ReloadFailed;

        // Cuántas lecturas de la base se hicieron (las pruebas de coalescencia).
        public int Loads { get; private set; }

        public Task LoadAsync()
        {
            if (_disposed)
            {
                return Task.CompletedTask;
            }

            if (_inFlight != null)
            {
                _pending = true;
                return _inFlight;
            }

            var task = RunAsync();
            if (!task.IsCompleted)
            {
                _inFlight = task;
            }

            return task;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _shell.Tapped -= OnTapped;
            _workoutSubscription.Dispose();
        }

        private async Task RunAsync()
        {
            try
            {
                do
                {
                    _pending = false;
                    await LoadOnceAsync();
                }
                while (_pending && !_disposed);
            }
            finally
            {
                _inFlight = null;
            }
        }

        private async Task LoadOnceAsync()
        {
            var hadList = _state.Value.Phase == HistoryPhase.Ready;
            if (!hadList)
            {
                Publish(new HistoryState(HistoryPhase.Loading, NoEntries));
            }

            IReadOnlyList<HistoryEntry> entries;
            Loads++;
            try
            {
                entries = await _sessions.GetHistoryAsync();
            }
            catch (Exception error)
            {
                if (_disposed)
                {
                    return;
                }

                _logger.Log(LogLevel.Warning, Tag, "carga del historial: " + error.Message, error);
                if (_state.Value.Phase == HistoryPhase.Ready)
                {
                    ReloadFailed?.Invoke();
                }
                else
                {
                    Publish(new HistoryState(HistoryPhase.LoadFailed, NoEntries));
                }

                return;
            }

            Publish(new HistoryState(HistoryPhase.Ready, entries ?? NoEntries));
        }

        private void OnTapped(int index)
        {
            if (index == ShellStore.HistoryIndex)
            {
                LoadAsync().Forget(_logger, Tag);
            }
        }

        // DT9-05: el cierre volvió (Saved o Failed) → la sesión ya está en la base, con su fin o cortada.
        private void OnWorkout(WorkoutSnapshot snapshot)
        {
            var close = snapshot.Close;
            // WP-B: Terminar a mano vuelve a reposo en cuanto el cierre vuelve (ReturnToIdle) sin publicar Saved (la foto pasa de
            // Finished/Pending, o de Running con una base inmediata, a Idle): volver a reposo también cuenta (AC-HIS-06-02).
            var toIdle = snapshot.Phase == WorkoutPhase.Idle && _lastPhase != WorkoutPhase.Idle;
            var returned = ((close == CloseStatus.Saved || close == CloseStatus.Failed) && close != _lastClose) || toIdle;
            _lastPhase = snapshot.Phase;
            _lastClose = close;
            if (returned)
            {
                LoadAsync().Forget(_logger, Tag);
            }
        }

        private void Publish(HistoryState state)
        {
            if (!_disposed)
            {
                _state.Value = state; // igual por valor → no emite (DT9-02)
            }
        }
    }
}
