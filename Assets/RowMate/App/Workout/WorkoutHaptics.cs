using System;
using RowMate.Core.Ports;
using RowMate.Core.Workout;

namespace RowMate.App.Workout
{
    // La respuesta táctil del entrenamiento (It. 14, AC-POL-03; DT14-23; 00-arte §3.4, ANIMATION_BIBLE §2.12). Escucha las fotos
    // del motor y pide:
    //   Light    al pasar de en marcha a pausa y de pausa a en marcha (solo el usuario pausa: el motor no tiene pausa automática);
    //   Medium   al cambiar de paso dentro de la misma sesión (también el automático; no al empezar);
    //   Success  al terminar la rutina completada (EndedBy == Completed), una vez por sesión.
    // La política (250 ms, Success sobre Medium, nada en segundo plano) la aplica el IHaptics que recibe (HapticGate, compartido
    // con DeviceStore). Hilo principal; sin reloj propio.
    public sealed class WorkoutHaptics : IDisposable
    {
        private readonly IHaptics _haptics;
        private readonly IDisposable _subscription;
        private WorkoutPhase _phase;
        private long _session;
        private int _stepIndex = -1;
        private long _completedSession = -1;
        private bool _disposed;

        public WorkoutHaptics(WorkoutEngine engine, IHaptics haptics)
        {
            if (engine == null)
            {
                throw new ArgumentNullException(nameof(engine));
            }

            _haptics = haptics ?? throw new ArgumentNullException(nameof(haptics));
            var current = engine.State.Value;
            _phase = current.Phase;
            _session = current.Session;
            _stepIndex = current.Step.HasValue ? current.Step.Value.StepIndex : -1;
            _subscription = engine.State.Subscribe(OnSnapshot, false);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _subscription.Dispose();
        }

        // Lo mismo sin motor (las pruebas de la regla y quien ya tiene las fotos).
        public void OnSnapshot(WorkoutSnapshot snapshot)
        {
            if (_disposed || snapshot == null)
            {
                return;
            }

            var sameSession = snapshot.Session == _session;
            var stepIndex = snapshot.Step.HasValue ? snapshot.Step.Value.StepIndex : -1;
            HapticKind? kind = null;
            if (sameSession && _phase == WorkoutPhase.Active && snapshot.Phase == WorkoutPhase.Paused)
            {
                kind = HapticKind.Light;
            }
            else if (sameSession && _phase == WorkoutPhase.Paused && snapshot.Phase == WorkoutPhase.Active)
            {
                kind = HapticKind.Light;
            }

            if (snapshot.Phase == WorkoutPhase.Finished && snapshot.EndedBy == WorkoutEndKind.Completed && _completedSession != snapshot.Session)
            {
                _completedSession = snapshot.Session;
                kind = HapticKind.Success;
            }
            else if (kind == null && sameSession && _stepIndex >= 0 && stepIndex >= 0 && stepIndex != _stepIndex
                     && (snapshot.Phase == WorkoutPhase.Active || snapshot.Phase == WorkoutPhase.Paused))
            {
                kind = HapticKind.Medium;
            }

            _phase = snapshot.Phase;
            _session = snapshot.Session;
            _stepIndex = stepIndex;
            if (kind.HasValue)
            {
                _haptics.Play(kind.Value);
            }
        }
    }
}
