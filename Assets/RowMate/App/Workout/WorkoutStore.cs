using System;
using System.Collections.Generic;
using RowMate.App.Device;
using RowMate.App.Routines;
using RowMate.Core.Common;
using RowMate.Core.Models;
using RowMate.Core.Ports;
using RowMate.Core.Workout;

namespace RowMate.App.Workout
{
    // Workout en reposo (F-WKT-12, F-BLE-10; 02-tech-design §2.11 de la It. 4a): el remo conectado, la colección compartida,
    // iniciar y los avisos. Hilo principal. Lo arma AppComposer con el motor de la app.
    // - "Conectado" = DeviceState.IsConnected; cualquier otro estado (Off, Scanning, Connecting, Disconnected) es "sin remo"
    //   (device_provider.dart:94; ble_service.dart:8; AC-BLE-10-03). Sigue al remo en vivo (AC-BLE-10-05).
    // - StartFree: sin remo o fuera de reposo → false, sin sesión (workout_screen.dart:86-91; AC-BLE-10-04, AC-WKT-01-10).
    // - TapRoutine: sin remo o fuera de reposo → Ignored; R3-08 → CannotStart con el aviso, sin sesión ni diálogo; si no,
    //   Confirm (:118-120; AC-WKT-12-07, -08; P-09).
    // - StartRoutine (al confirmar): solo mira que el motor esté en reposo, no la conexión (:163-165; AC-BLE-10-06).
    // - Avisos del motor (WorkoutToast.For). Sin el usuario delante (segundo plano, ventana minimizada o sin foco) no emite:
    //   guarda solo el último y lo emite UNA vez cuando el usuario vuelve a estar delante (AC-WKT-02-06, -08, -09;
    //   QA-G4a-P08; spec §12 punto 13).
    // - It. 4b (AC-APP-03-11, AC-WKT-02-08; 02-tech-design §2.11): con el diálogo de completado abierto (la foto del motor en
    //   Finished con EndedBy == Completed) tampoco emite: retiene el último y lo emite al volver a reposo, una vez. El
    //   workoutSaveError del final sale en ese momento. Cuándo se VE es de la presentación (WorkoutScreenView, con el
    //   RevealStarted del inmersivo que sale, DN4b-24).
    public sealed class WorkoutStore : IDisposable
    {
        public const string Tag = WorkoutEngine.Tag;

        private readonly WorkoutEngine _engine;
        private readonly RoutinesStore _routines;
        private readonly IAppLifecycle _lifecycle;
        private readonly IAppLogger _logger;
        private readonly ObservableValue<WorkoutRestState> _state;
        private readonly EventStream<string> _toasts;
        private readonly IDisposable _deviceSubscription;
        private readonly IDisposable _routinesSubscription;
        private readonly IDisposable _engineSubscription;
        private readonly IDisposable _noticeSubscription;
        private readonly IDisposable _backgroundSubscription;
        private readonly IDisposable _focusSubscription;
        private bool _rowerConnected;
        private string? _held;
        private bool _completionOpen;
        private bool _disposed;

        public WorkoutStore(WorkoutEngine engine, DeviceStore device, RoutinesStore routines, IAppLifecycle lifecycle, IAppLogger logger)
        {
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));
            if (device == null)
            {
                throw new ArgumentNullException(nameof(device));
            }

            _routines = routines ?? throw new ArgumentNullException(nameof(routines));
            _lifecycle = lifecycle ?? throw new ArgumentNullException(nameof(lifecycle));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _rowerConnected = device.State.Value.IsConnected;
            _state = new ObservableValue<WorkoutRestState>(Build(), logger);
            State = new ReadOnlyObservable<WorkoutRestState>(_state);
            _toasts = new EventStream<string>(logger);
            _deviceSubscription = device.State.Subscribe(OnDevice, emitCurrent: false);
            _routinesSubscription = routines.State.Subscribe(_ => Refresh(), emitCurrent: false);
            _engineSubscription = engine.State.Subscribe(OnEngine, emitCurrent: false);
            _noticeSubscription = engine.Notices.Subscribe(OnNotice);
            _backgroundSubscription = lifecycle.InBackground.Subscribe(_ => ReleaseHeld(), emitCurrent: false);
            _focusSubscription = lifecycle.HasFocus.Subscribe(_ => ReleaseHeld(), emitCurrent: false);
        }

        public IReadOnlyObservable<WorkoutRestState> State { get; }

        // La clave del catálogo de cada aviso (workoutRoutineCannotStart, workoutSaveError).
        public IEventSource<string> Toasts => _toasts;

        // It. 4b: el motor espera al diálogo de completado (Finished con EndedBy == Completed): los avisos se retienen.
        public bool CompletionDialogOpen => _completionOpen;

        // El aviso retenido mientras el usuario no está delante (null si no hay).
        public string? HeldToast => _held;

        public bool StartFree()
        {
            if (_disposed || !_rowerConnected || _engine.Phase != WorkoutPhase.Idle)
            {
                return false;
            }

            return _engine.StartFree() == StartOutcome.Started;
        }

        public RoutineTapOutcome TapRoutine(Routine routine)
        {
            if (routine == null)
            {
                throw new ArgumentNullException(nameof(routine));
            }

            if (_disposed || !_rowerConnected || _engine.Phase != WorkoutPhase.Idle)
            {
                return RoutineTapOutcome.Ignored;
            }

            if (!WorkoutStartRule.CanStart(routine))
            {
                Show(WorkoutToast.CannotStart);
                return RoutineTapOutcome.CannotStart;
            }

            return RoutineTapOutcome.Confirm;
        }

        public bool StartRoutine(Routine routine)
        {
            if (routine == null)
            {
                throw new ArgumentNullException(nameof(routine));
            }

            if (_disposed || _engine.Phase != WorkoutPhase.Idle)
            {
                return false;
            }

            var outcome = _engine.StartWithRoutine(routine);
            if (outcome == StartOutcome.CannotStart)
            {
                Show(WorkoutToast.CannotStart);
            }

            return outcome == StartOutcome.Started;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _deviceSubscription.Dispose();
            _routinesSubscription.Dispose();
            _engineSubscription.Dispose();
            _noticeSubscription.Dispose();
            _backgroundSubscription.Dispose();
            _focusSubscription.Dispose();
        }

        private WorkoutRestState Build()
        {
            var routines = _routines.State.Value;
            return new WorkoutRestState(_rowerConnected, routines.Phase, routines.Routines, _engine.Phase);
        }

        private void Refresh()
        {
            if (_disposed)
            {
                return;
            }

            var next = Build();
            var current = _state.Value;
            if (current.RowerConnected == next.RowerConnected && current.RoutinesPhase == next.RoutinesPhase
                && ReferenceEquals(current.Routines, next.Routines) && current.SessionPhase == next.SessionPhase)
            {
                return;
            }

            _state.Value = next;
        }

        private void OnDevice(DeviceState device)
        {
            _rowerConnected = device.IsConnected;
            Refresh();
        }

        private void OnEngine(WorkoutSnapshot snapshot)
        {
            var completionOpen = snapshot.Phase == WorkoutPhase.Finished && snapshot.EndedBy == WorkoutEndKind.Completed;
            var closed = _completionOpen && !completionOpen;
            _completionOpen = completionOpen;
            if (snapshot.Phase != _state.Value.SessionPhase)
            {
                Refresh();
            }

            if (closed)
            {
                ReleaseHeld();
            }
        }

        private void OnNotice(WorkoutNotice notice)
        {
            var key = WorkoutToast.For(notice);
            if (key != null)
            {
                Show(key);
            }
        }

        private void Show(string key)
        {
            if (_disposed)
            {
                return;
            }

            if (!_lifecycle.IsUserFacing || _completionOpen)
            {
                // Sin el usuario delante, o con el diálogo de completado abierto: solo el último, una vez al volver
                // (AC-WKT-02-08, AC-APP-03-11).
                _held = key;
                _logger.Log(LogLevel.Debug, Tag, "aviso retenido: " + key);
                return;
            }

            _toasts.Emit(key);
        }

        private void ReleaseHeld()
        {
            if (_disposed || _held == null || !_lifecycle.IsUserFacing || _completionOpen)
            {
                return;
            }

            var key = _held;
            _held = null;
            _toasts.Emit(key);
        }
    }
}
