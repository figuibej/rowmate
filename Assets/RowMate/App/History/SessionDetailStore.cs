using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RowMate.Core.Common;
using RowMate.Core.History;
using RowMate.Core.Models;
using RowMate.Core.Ports;

namespace RowMate.App.History
{
    // El detalle de una sesión (F-HIS-09 a -15; 02-tech-design §3.2 de la It. 9; legacy: _load, session_detail_screen.dart:35-54):
    // 1. Relee la sesión por su id; si ya no existe, usa la de la tarjeta (AC-HIS-09-02).
    // 2. Lee TODOS sus puntos, por el tiempo del monitor (AC-HIS-19-02).
    // 3. Si tiene rutina y la rutina existe, sus pasos expandidos ACTUALES, con progresiones (Q-16, AC-RUT-20-01); si no, ninguno.
    // 4. Sin puntos → NoTelemetry. Con puntos → Ready con SessionStats.Compute, StepBreakdown.Compute (× 5 s, Q-23) y
    //    SessionChartData.Build (DT9-13: en el hilo principal al volver la lectura).
    // 5. Cualquier excepción → LoadFailed (Q-13, AC-HIS-09-04) y "[DB] carga de la sesión: <motivo>" como Warning. Reintentar =
    //    LoadAsync. Una carga en curso no se duplica.
    // La métrica del gráfico arranca SIEMPRE en Vatios y no se guarda (Q-21, AC-HIS-12-01). Lo crea
    // AppContext.CreateSessionDetail y lo dispone la página al salir; lo que vuelve después no cambia nada.
    public sealed class SessionDetailStore : IDisposable
    {
        public const string Tag = "DB";

        private static readonly IReadOnlyList<StepBreakdownRow> NoRows = new StepBreakdownRow[0];
        private static readonly IReadOnlyList<IntervalStep> NoSteps = new IntervalStep[0];

        private readonly HistoryEntry _entry;
        private readonly ISessionRepository _sessions;
        private readonly IRoutineRepository _routines;
        private readonly IAppLogger _logger;
        private readonly ObservableValue<SessionDetailState> _state;
        private readonly ObservableValue<ChartMetric> _metric;
        private Task? _loading;
        private bool _disposed;

        public SessionDetailStore(HistoryEntry entry, ISessionRepository sessions, IRoutineRepository routines, IAppLogger logger)
        {
            _entry = entry ?? throw new ArgumentNullException(nameof(entry));
            _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
            _routines = routines ?? throw new ArgumentNullException(nameof(routines));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _state = new ObservableValue<SessionDetailState>(
                new SessionDetailState(SessionDetailPhase.Loading, entry.Session, entry.Stats, NoRows, NoSteps, null), logger);
            State = new ReadOnlyObservable<SessionDetailState>(_state);
            _metric = new ObservableValue<ChartMetric>(ChartMetric.Watts, logger);
            Metric = new ReadOnlyObservable<ChartMetric>(_metric);
        }

        public HistoryEntry Entry => _entry;

        public IReadOnlyObservable<SessionDetailState> State { get; }

        public IReadOnlyObservable<ChartMetric> Metric { get; }

        public void SelectMetric(ChartMetric metric)
        {
            if (!_disposed)
            {
                _metric.Value = metric;
            }
        }

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

        public void Dispose()
        {
            _disposed = true;
        }

        private async Task LoadCoreAsync()
        {
            var current = _state.Value;
            Publish(new SessionDetailState(SessionDetailPhase.Loading, current.Session, current.Stats, NoRows, NoSteps, null));
            try
            {
                var session = _entry.Session;
                if (session.Id.HasValue)
                {
                    session = await _sessions.GetByIdAsync(session.Id.Value) ?? _entry.Session;
                }

                IReadOnlyList<DataPoint> points = session.Id.HasValue ? await _sessions.GetPointsAsync(session.Id.Value) : new DataPoint[0];
                var flatSteps = NoSteps;
                if (session.RoutineId.HasValue)
                {
                    var routine = await _routines.GetByIdAsync(session.RoutineId.Value);
                    if (routine != null)
                    {
                        flatSteps = RoutineExpansion.Flatten(routine.Steps, ProgressionOverflow.Saturate);
                    }
                }

                if (_disposed)
                {
                    return;
                }

                if (points == null || points.Count == 0)
                {
                    Publish(new SessionDetailState(SessionDetailPhase.NoTelemetry, session, SessionStats.Empty, NoRows, flatSteps, null));
                    return;
                }

                var stats = SessionStats.Compute(points);
                var steps = StepBreakdown.Compute(points, HistoryRules.AppSampleIntervalSeconds);
                var chart = SessionChartData.Build(points, flatSteps);
                Publish(new SessionDetailState(SessionDetailPhase.Ready, session, stats, steps, flatSteps, chart));
            }
            catch (Exception error)
            {
                if (_disposed)
                {
                    return;
                }

                _logger.Log(LogLevel.Warning, Tag, "carga de la sesión: " + error.Message, error);
                var state = _state.Value;
                Publish(new SessionDetailState(SessionDetailPhase.LoadFailed, state.Session, state.Stats, NoRows, NoSteps, null));
            }
            finally
            {
                _loading = null;
            }
        }

        private void Publish(SessionDetailState state)
        {
            if (!_disposed)
            {
                _state.Value = state;
            }
        }
    }
}
