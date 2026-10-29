using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RowMate.Core.Ble;
using RowMate.Core.Common;
using RowMate.Core.Models;
using RowMate.Core.Ports;

namespace RowMate.Core.Workout
{
    // El motor de la sesión (F-WKT-01 a -06, -09; 02-tech-design §2 de la It. 4a): puerto de WorkoutProvider
    // (lib/features/workout/workout_provider.dart:44-328) sin Flutter ni Unity. Hilo principal. Lo arma AppComposer y vive con
    // la app (legacy: el WorkoutProvider del MultiProvider, lib/main.dart:82).
    // - Recibe las lecturas del remo y del pulsómetro en TODAS las fases (AC-WKT-01-01) y no mira la conexión del remo
    //   (AC-BLE-10-07): sin lecturas, el reloj y los pasos por tiempo siguen y los puntos repiten la última (AC-WKT-03-10).
    // - Reloj por tiempo monótono (ActiveClock): un timer de una vez armado al próximo vencimiento procesa en orden todos los
    //   segundos vencidos, cada uno con el instante de su vencimiento (AC-WKT-02-04). Arranca cuando vuelve el alta, o a los
    //   100 ms del toque, lo que llegue primero (AC-WKT-01-02; DT4a-02).
    // - El segundo, en el orden del legacy (:151-190; AC-WKT-02-02): relojes y muestreo; un punto cada 5 s (también con el
    //   alta en vuelo: el grabador lo escribe cuando vuelve, o lo descarta si falló); con rutina, los metros saneados (Q-55),
    //   los objetivos y el fin del paso; el aviso.
    // - Al terminar (a mano o sola): punto final, estadísticas y cierre en una transacción (SessionRecorder); vuelve a reposo
    //   SOLO cuando el cierre volvió, o a los 15 s de primer plano (ForegroundTimeout; DT4a-13, DT4a-15).
    // - En segundo plano (o durante el alcance) no publica fotos: una sola al volver, con Instant (QA-G4aT-07). Lo mismo con un
    //   cuadro de primer plano que llega con varios segundos vencidos (QA-V4a-ROB-05). Al pasar a segundo plano con una sesión
    //   en curso manda lo pendiente y espera la base hasta 1 s (AC-WKT-06-15).
    public sealed class WorkoutEngine : IDisposable
    {
        public const string Tag = "WORKOUT";

        // Un timer que dispara hasta 1 ms antes de su vencimiento (redondeos de la hora del cuadro) igual procesa su segundo.
        private const long EarlyToleranceTicks = TimeSpan.TicksPerMillisecond;

        private readonly ISessionRepository _sessions;
        private readonly IScheduler _scheduler;
        private readonly IAppLifecycle _lifecycle;
        private readonly IAppLogger _logger;
        private readonly ObservableValue<WorkoutSnapshot> _state;
        private readonly EventStream<WorkoutNotice> _notices;
        private readonly HeartRateFusion _fusion = new HeartRateFusion();
        private readonly ActiveClock _clock = new ActiveClock();
        private readonly SanitizedDistance _meter = new SanitizedDistance();
        private readonly List<DataPoint> _points = new List<DataPoint>();
        private readonly IDisposable _dataSubscription;
        private readonly IDisposable _bpmSubscription;
        private readonly IDisposable _statusSubscription;
        private readonly IDisposable _backgroundSubscription;
        private readonly Action _onClock;

        private WorkoutPhase _phase = WorkoutPhase.Idle;
        private IReadOnlyList<IntervalStep>? _steps;
        private IReadOnlyList<StepPosition>? _positions;
        private int _stepIndex;
        private int _stepElapsed;
        private int _totalElapsed;
        private int _sampleTick;
        private double _stepStartMeters;
        private WorkoutSession? _session;
        private SessionRecorder? _recorder;
        private IDisposable? _timer;
        private IDisposable? _graceTimer;
        private bool _clockOpened;
        private bool _saving;
        private long _generation;
        private long _revision;
        private bool _pendingSnapshot;
        // Un cuadro tarde que procesa varios segundos: las fotos esperan al final (QA-V4a-ROB-05).
        private bool _coalescing;
        private Task _finishTask = Task.CompletedTask;
        private bool _disposed;

        public WorkoutEngine(IRowerService rower, IHeartRateService heartRate, ISessionRepository sessions, IScheduler scheduler,
                             IAppLifecycle lifecycle, IAppLogger logger)
        {
            if (rower == null)
            {
                throw new ArgumentNullException(nameof(rower));
            }

            if (heartRate == null)
            {
                throw new ArgumentNullException(nameof(heartRate));
            }

            _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
            _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
            _lifecycle = lifecycle ?? throw new ArgumentNullException(nameof(lifecycle));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _state = new ObservableValue<WorkoutSnapshot>(WorkoutSnapshot.Initial, logger, SnapshotIdentity.Instance);
            State = new ReadOnlyObservable<WorkoutSnapshot>(_state);
            _notices = new EventStream<WorkoutNotice>(logger);
            _onClock = OnClock;
            // :82-86. El statusStream del legacy es broadcast: el estado actual no llega (emitCurrent: false).
            _dataSubscription = rower.Data.Subscribe(OnRowerReading);
            _bpmSubscription = heartRate.Bpm.Subscribe(OnSensorBpm);
            _statusSubscription = heartRate.Status.Subscribe(OnSensorStatus, emitCurrent: false);
            _backgroundSubscription = lifecycle.InBackground.Subscribe(OnBackgroundChanged, emitCurrent: false);
        }

        // Arranca en reposo con RowingData.Empty. Una foto por aviso (Revision sube siempre).
        public IReadOnlyObservable<WorkoutSnapshot> State { get; }

        public IEventSource<WorkoutNotice> Notices => _notices;

        // :78-80, :296: la última sesión cerrada bien (la usa la subida a Strava de la It. 12). También si el cierre volvió
        // después del tope.
        public long? LastFinishedSessionId { get; private set; }

        // La rutina expandida en curso (null en libre y en reposo).
        public IReadOnlyList<IntervalStep>? Steps => _steps;

        // :61, :127: la posición de cada paso en su serie (para la It. 5).
        public IReadOnlyList<StepPosition>? Positions => _positions;

        // :60, :92: el cumplimiento de objetivos por paso (para las píldoras de la 4b). No se guarda (AC-WKT-06-14).
        public StepTargetTracker Targets { get; } = new StepTargetTracker();

        public WorkoutPhase Phase => _phase;

        // La tarea del cierre en curso (o la del último): FinishAsync la devuelve.
        public Task FinishTask => _finishTask;

        // :115-117: un entrenamiento libre.
        public StartOutcome StartFree()
        {
            if (_disposed || _phase != WorkoutPhase.Idle)
            {
                return StartOutcome.Busy;
            }

            Begin(null);
            return StartOutcome.Started;
        }

        // :120-131 + R3-08: con una rutina que se puede iniciar, la expande (saturando, DT4a-16), paso 0 con 0 s, la
        // distancia de inicio, las posiciones y los objetivos en 0; después, como el libre, con el id y el nombre de la
        // PLANTILLA copiados (AC-WKT-01-03).
        public StartOutcome StartWithRoutine(Routine template)
        {
            if (template == null)
            {
                throw new ArgumentNullException(nameof(template));
            }

            if (_disposed || _phase != WorkoutPhase.Idle)
            {
                return StartOutcome.Busy;
            }

            if (!WorkoutStartRule.CanStart(template))
            {
                return StartOutcome.CannotStart;
            }

            _steps = RoutineExpansion.Flatten(template.Steps, ProgressionOverflow.Saturate);
            _positions = RoutineExpansion.Positions(template.Steps);
            _stepIndex = 0;
            _stepElapsed = 0;
            _meter.Begin(_fusion.Data.DistanceMeters, 0);
            _stepStartMeters = 0;
            Targets.Reset();
            Begin(template);
            return StartOutcome.Started;
        }

        // :239-243: se cancela el timer y el tramo se cierra (la fracción del segundo se pierde, Q-58). Solo en marcha.
        public void Pause()
        {
            if (_disposed || _phase != WorkoutPhase.Active)
            {
                return;
            }

            _phase = WorkoutPhase.Paused;
            CancelTimer();
            _clock.Stop();
            Notify();
        }

        // :245-250: tramo nuevo desde ahora; la distancia de ese momento es la referencia nueva de los metros del paso. Si se
        // pausó antes de que abriera el primer tramo, lo abre.
        public void Resume()
        {
            if (_disposed || _phase != WorkoutPhase.Paused)
            {
                return;
            }

            _phase = WorkoutPhase.Active;
            _clockOpened = true;
            _graceTimer?.Dispose();
            _graceTimer = null;
            _clock.Start(NowTicks());
            Arm();
            _meter.Rebase(_fusion.Data.DistanceMeters);
            Notify();
        }

        // :252-303: terminar a mano, desde en marcha o en pausa. Idempotente: fuera de esas fases devuelve el cierre en curso
        // (dos Terminar seguidos cierran una vez, AC-WKT-20-06).
        public Task FinishAsync()
        {
            if (_disposed || (_phase != WorkoutPhase.Active && _phase != WorkoutPhase.Paused))
            {
                return _finishTask;
            }

            EndSession(WorkoutEndKind.Manual, _scheduler.UtcNow);
            return _finishTask;
        }

        // :320-327: la app se cierra. Da de baja las suscripciones y el timer; una sesión en curso queda cortada con lo que ya
        // tenía (manda lo pendiente y no la cierra, P-03).
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _dataSubscription.Dispose();
            _bpmSubscription.Dispose();
            _statusSubscription.Dispose();
            _backgroundSubscription.Dispose();
            CancelTimer();
            _graceTimer?.Dispose();
            _graceTimer = null;
            var recorder = _recorder;
            _recorder = null;
            if (recorder != null)
            {
                recorder.Abandon();
                recorder.Dispose();
            }
        }

        // ── Inicio y reloj ──

        // :133-149: en marcha, hora de inicio, puntos en memoria vacíos, muestreo en 0, alta de la sesión; el reloj espera el
        // alta, a lo sumo 100 ms.
        private void Begin(Routine? template)
        {
            var generation = ++_generation;
            _phase = WorkoutPhase.Active;
            _saving = false;
            var startedAt = _scheduler.UtcNow;
            _points.Clear();
            _sampleTick = 0;
            _session = new WorkoutSession(startedAt) { RoutineId = template?.Id, RoutineName = template?.Name };
            var recorder = new SessionRecorder(_sessions, _logger);
            recorder.Failed += () => OnRecorderFailed(generation);
            _recorder = recorder;
            _clockOpened = false;
            Notify();
            var creation = recorder.BeginAsync(_session);
            if (creation.IsCompleted)
            {
                OpenFirstSegment(generation);
                return;
            }

            _graceTimer = _scheduler.Schedule(WorkoutTimings.ClockStartGrace, () => OpenFirstSegment(generation));
            _ = OpenWhenCreated(creation, generation);
        }

        private async Task OpenWhenCreated(Task<bool> creation, long generation)
        {
            try
            {
                await creation;
            }
            catch (Exception error)
            {
                _logger.Log(LogLevel.Error, Tag, "alta: " + error.Message, error);
            }

            OpenFirstSegment(generation);
        }

        private void OpenFirstSegment(long generation)
        {
            if (_disposed || generation != _generation || _clockOpened)
            {
                return;
            }

            _clockOpened = true;
            _graceTimer?.Dispose();
            _graceTimer = null;
            if (_phase != WorkoutPhase.Active)
            {
                return;
            }

            _clock.Start(NowTicks());
            Arm();
        }

        private void Arm()
        {
            CancelTimer();
            var delay = _clock.NextDueTicks - NowTicks();
            _timer = _scheduler.Schedule(TimeSpan.FromTicks(delay < 0 ? 0 : delay), _onClock);
        }

        private void CancelTimer()
        {
            _timer?.Dispose();
            _timer = null;
        }

        // Los segundos vencidos, en orden, cada uno con el instante de su vencimiento; después vuelve a armar. Un cuadro muy tarde
        // en primer plano (el loop frenado, o el equipo suspendido con un monótono que siguió contando) procesa varios: se
        // publica UNA foto al final, con Instant, como al volver del segundo plano (QA-V4a-ROB-05), y no una por segundo. Los
        // puntos, los pasos y los avisos salen igual, en orden, cada uno en su segundo.
        private void OnClock()
        {
            _timer = null;
            if (_disposed || _phase != WorkoutPhase.Active || !_clock.IsRunning)
            {
                return;
            }

            var due = _clock.Due(NowTicks() + EarlyToleranceTicks);
            var coalesce = due > 1 && !_coalescing;
            if (coalesce)
            {
                _coalescing = true;
            }

            try
            {
                for (var i = 0; i < due && _phase == WorkoutPhase.Active; i++)
                {
                    var at = _clock.NextDueTicks;
                    _clock.MarkProcessed();
                    ProcessSecond(at);
                }
            }
            finally
            {
                if (coalesce)
                {
                    _coalescing = false;
                    if (_pendingSnapshot && !_disposed && !_lifecycle.InBackground.Value && !_scheduler.IsCatchingUp)
                    {
                        Publish(true);
                    }
                }
            }

            if (_phase == WorkoutPhase.Active && _clock.IsRunning)
            {
                Arm();
            }
        }

        // ── El segundo (:151-190) ──

        private void ProcessSecond(long at)
        {
            if (_phase != WorkoutPhase.Active)
            {
                return;
            }

            // 1 · :154-156.
            _stepElapsed++;
            _totalElapsed++;
            _sampleTick++;

            // 2 · :158-173: un punto cada 5 s activos, con la lectura fusionada y el paso en curso (antes del fin del paso).
            var recorder = _recorder;
            if (_sampleTick % WorkoutTimings.SampleEverySeconds == 0 && recorder != null && !recorder.CreateFailed)
            {
                var point = TelemetryPoint.From(0, _fusion.Data, _steps != null ? _stepIndex : (int?)null, CurrentStep());
                _points.Add(point);
                recorder.Add(point);
            }

            // 3 · :176-187: con rutina, los metros saneados (Q-55), los objetivos y el fin del paso.
            if (_steps != null)
            {
                _meter.Update(_fusion.Data.DistanceMeters, _totalElapsed);
                Targets.Update(_stepIndex, _fusion.Data);
                CheckStepCompletion(at);
            }

            // 4 · :189 (también cuando el paso cerró la rutina).
            Notify();
        }

        // :192-218, con Q-55 en el paso por distancia (DV-26).
        private void CheckStepCompletion(long at)
        {
            var steps = _steps!;
            var step = steps[_stepIndex];
            bool completed;
            if (step.DurationSeconds.HasValue)
            {
                completed = _stepElapsed >= step.DurationSeconds.Value;
            }
            else if (step.DistanceMeters.HasValue)
            {
                completed = _meter.Meters - _stepStartMeters >= step.DistanceMeters.Value;
            }
            else
            {
                completed = false;
            }

            if (!completed)
            {
                return;
            }

            Targets.Complete(_stepIndex, step);
            if (_stepIndex + 1 < steps.Count)
            {
                _stepIndex++;
                _stepElapsed = 0;
                _stepStartMeters = _meter.Meters;
                return;
            }

            // Último paso: terminada en el mismo segundo; el índice queda en el último paso.
            EndSession(WorkoutEndKind.Completed, WallAt(at));
        }

        // ── Cierre (:252-303) y vuelta a reposo (:305-318) ──

        // Terminada (a mano o sola): se cancela el timer, se detiene el reloj, se avisa (los controles se apagan: AC-WKT-20-06)
        // y empieza el cierre.
        private void EndSession(WorkoutEndKind kind, DateTimeOffset finishedAt)
        {
            _phase = WorkoutPhase.Finished;
            CancelTimer();
            _graceTimer?.Dispose();
            _graceTimer = null;
            _clock.Stop();
            _saving = true;
            Notify();
            _finishTask = Close(kind, finishedAt);
        }

        private Task Close(WorkoutEndKind kind, DateTimeOffset finishedAt)
        {
            var generation = _generation;
            var recorder = _recorder;
            var session = _session;
            if (recorder == null || session == null || recorder.CreateFailed)
            {
                // :256: sin alta no hay punto final ni cierre; vuelve a reposo en el acto, con el error (AC-WKT-01-11).
                ReturnToIdle(kind, false);
                return Task.CompletedTask;
            }

            // :259-273: el punto final, con la lectura de ese momento y el paso en curso (tipo vacío si el índice quedara fuera).
            int? index = _steps != null ? _stepIndex : (int?)null;
            var final = TelemetryPoint.From(0, _fusion.Data, index, CurrentStep());
            _points.Add(final);
            var stats = SessionStats.Compute(_points);
            var closed = session.Closed(stats, finishedAt);
            Task<bool> save;
            try
            {
                save = recorder.FinishAsync(final, closed);
            }
            catch (Exception error)
            {
                save = Task.FromException<bool>(error);
            }

            return AwaitClose(save, kind, generation, recorder);
        }

        private async Task AwaitClose(Task<bool> save, WorkoutEndKind kind, long generation, SessionRecorder recorder)
        {
            var timeout = new ForegroundTimeout(_scheduler, _lifecycle, WorkoutTimings.FinishSaveTimeout, () => OnCloseTimeout(kind, generation));
            bool saved;
            try
            {
                saved = await save;
            }
            catch (Exception error)
            {
                _logger.Log(LogLevel.Warning, Tag, "cierre: " + error.GetType().Name + ": " + error.Message, error);
                saved = false;
            }
            finally
            {
                timeout.Dispose();
            }

            if (_disposed)
            {
                return;
            }

            if (saved)
            {
                LastFinishedSessionId = recorder.SessionId;
            }

            if (generation != _generation || _phase != WorkoutPhase.Finished)
            {
                if (saved)
                {
                    _logger.Log(LogLevel.Info, Tag, "cierre tardío");
                }

                return;
            }

            ReturnToIdle(kind, saved);
        }

        private void OnCloseTimeout(WorkoutEndKind kind, long generation)
        {
            if (_disposed || generation != _generation || _phase != WorkoutPhase.Finished)
            {
                return;
            }

            _logger.Log(LogLevel.Warning, Tag, "cierre sin respuesta");
            ReturnToIdle(kind, false);
        }

        // :305-318: borra la rutina, el índice, los relojes, la sesión, el inicio, los puntos y las posiciones; los metros y
        // los objetivos vuelven a 0. NO borra la lectura, el pulso del sensor ni la última terminada (AC-WKT-01-07).
        private void ReturnToIdle(WorkoutEndKind kind, bool saved)
        {
            _phase = WorkoutPhase.Idle;
            _saving = false;
            _steps = null;
            _positions = null;
            _stepIndex = 0;
            _stepElapsed = 0;
            _totalElapsed = 0;
            _session = null;
            _points.Clear();
            _recorder = null;
            _meter.Begin(0, 0);
            Targets.Reset();
            Notify();
            _notices.Emit(WorkoutNotice.Ended(kind, saved));
        }

        // ── Lecturas (:220-237) ──

        private void OnRowerReading(RowingData reading)
        {
            if (_disposed)
            {
                return;
            }

            _fusion.OnRowerReading(reading);
            Notify();
        }

        private void OnSensorBpm(int bpm)
        {
            if (_disposed)
            {
                return;
            }

            _fusion.OnSensorBpm(bpm);
            Notify();
        }

        private void OnSensorStatus(HrmStatus status)
        {
            if (_disposed)
            {
                return;
            }

            if (_fusion.OnSensorStatus(status))
            {
                Notify();
            }
        }

        private void OnRecorderFailed(long generation)
        {
            if (_disposed || generation != _generation || (_phase != WorkoutPhase.Active && _phase != WorkoutPhase.Paused))
            {
                return;
            }

            _notices.Emit(WorkoutNotice.SaveFailed());
        }

        // ── Segundo plano (AC-WKT-06-15; QA-G4aT-07) ──

        private void OnBackgroundChanged(bool inBackground)
        {
            if (_disposed)
            {
                return;
            }

            if (inBackground)
            {
                var recorder = _recorder;
                if (recorder != null && _phase != WorkoutPhase.Idle)
                {
                    recorder.FlushAndWait(WorkoutTimings.BackgroundFlushTimeout);
                }

                return;
            }

            if (_pendingSnapshot && !_scheduler.IsCatchingUp)
            {
                Publish(true);
            }
        }

        // ── Fotos ──

        private void Notify()
        {
            _revision++;
            if (_lifecycle.InBackground.Value || _scheduler.IsCatchingUp || _coalescing)
            {
                _pendingSnapshot = true;
                return;
            }

            Publish(_pendingSnapshot);
        }

        private void Publish(bool instant)
        {
            _pendingSnapshot = false;
            _state.Value = new WorkoutSnapshot(_revision, _phase, _fusion.Data, _totalElapsed, CurrentProgress(), _steps != null, _saving, instant);
        }

        private IntervalStep? CurrentStep()
        {
            var steps = _steps;
            return steps != null && _stepIndex >= 0 && _stepIndex < steps.Count ? steps[_stepIndex] : null;
        }

        // :98-112: solo con rutina y fuera de reposo.
        private StepProgress? CurrentProgress()
        {
            var steps = _steps;
            if (steps == null || _phase == WorkoutPhase.Idle || _stepIndex < 0 || _stepIndex >= steps.Count)
            {
                return null;
            }

            var next = _stepIndex + 1 < steps.Count ? steps[_stepIndex + 1] : null;
            return new StepProgress(steps[_stepIndex], _stepIndex, steps.Count, _stepElapsed, next);
        }

        private long NowTicks()
        {
            return ActiveClock.ToTicks(_scheduler.MonotonicSeconds);
        }

        // Hora de pared de un instante monótono: fuera de un alcance, la de ahora; durante el alcance, la del instante virtual
        // (la hora de fin de una rutina que terminó en segundo plano es la de su segundo, AC-WKT-02-06).
        private DateTimeOffset WallAt(long atTicks)
        {
            var behind = NowTicks() - atTicks;
            return behind > 0 ? _scheduler.UtcNow - TimeSpan.FromTicks(behind) : _scheduler.UtcNow;
        }

        // Cada foto es nueva (Revision sube siempre): sin comparar campo por campo.
        private sealed class SnapshotIdentity : IEqualityComparer<WorkoutSnapshot>
        {
            public static readonly SnapshotIdentity Instance = new SnapshotIdentity();

            public bool Equals(WorkoutSnapshot? x, WorkoutSnapshot? y)
            {
                return ReferenceEquals(x, y);
            }

            public int GetHashCode(WorkoutSnapshot obj)
            {
                return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
            }
        }
    }
}
