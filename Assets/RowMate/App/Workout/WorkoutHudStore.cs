using System;
using System.Threading.Tasks;
using RowMate.Core.Common;
using RowMate.Core.Localization;
using RowMate.Core.Models;
using RowMate.Core.Routines;
using RowMate.Core.Workout;

namespace RowMate.App.Workout
{
    // El HUD del inmersivo (F-WKT-10, -13, -14, -16, -17, -20; 02-tech-design §2.11 de la It. 4a): rehace un WorkoutHudState con
    // los textos ya formateados y el semáforo con cada foto del motor y con cada cambio de idioma (unas pocas cadenas por
    // lectura, a 1-3 Hz). En segundo plano el motor no publica fotos, así que durante el alcance no rehace nada: arma UN
    // estado al volver, con Instant (QA-G4aT-07). Uno por inmersivo: lo crea AppContext.CreateHud() y lo dispone la página.
    // legacy: lib/features/workout/immersive_workout_screen.dart:330-475 (fila del paso), :560-733 (tarjetas y banda),
    // :893-953 (controles).
    public sealed class WorkoutHudStore : IDisposable
    {
        private readonly WorkoutEngine _engine;
        private readonly IReadOnlyObservable<Localizer> _texts;
        private readonly ObservableValue<WorkoutHudState> _state;
        private readonly IDisposable _engineSubscription;
        private readonly IDisposable _textsSubscription;
        private WorkoutSnapshot _last;
        private bool _disposed;

        public WorkoutHudStore(WorkoutEngine engine, IReadOnlyObservable<Localizer> texts, IAppLogger logger)
        {
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));
            _texts = texts ?? throw new ArgumentNullException(nameof(texts));
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _last = engine.State.Value;
            _state = new ObservableValue<WorkoutHudState>(Build(_last, texts.Value), logger, HudStateIdentity.Instance);
            State = new ReadOnlyObservable<WorkoutHudState>(_state);
            _engineSubscription = engine.State.Subscribe(OnSnapshot, emitCurrent: false);
            _textsSubscription = texts.Subscribe(_ => Rebuild(), emitCurrent: false);
        }

        public IReadOnlyObservable<WorkoutHudState> State { get; }

        public void Pause()
        {
            if (!_disposed)
            {
                _engine.Pause();
            }
        }

        public void Resume()
        {
            if (!_disposed)
            {
                _engine.Resume();
            }
        }

        public Task FinishAsync()
        {
            return _disposed ? Task.CompletedTask : _engine.FinishAsync();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _engineSubscription.Dispose();
            _textsSubscription.Dispose();
        }

        // El estado del HUD de una foto (la regla de cada campo: 02-tech-design §2.11, tabla "Campo del HUD").
        public static WorkoutHudState Build(WorkoutSnapshot snapshot, Localizer l10n)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            if (l10n == null)
            {
                throw new ArgumentNullException(nameof(l10n));
            }

            var data = snapshot.Data;
            var progress = snapshot.Step;
            var step = progress.HasValue ? progress.Value.Step : null;
            var lights = TargetLight.Evaluate(step, data);
            var spm = Metric(WorkoutFormats.Spm(data.StrokeRate), data.StrokeRate, lights.Spm, TargetArrow.Up,
                step?.TargetSpm.HasValue == true ? WorkoutFormats.TargetValue(step.TargetSpm!.Value) : null, l10n);
            var split = Metric(WorkoutFormats.LiveSplit(data.Pace500mSeconds), data.Pace500mSeconds, lights.Split, TargetArrow.Down,
                step?.TargetSplitSeconds.HasValue == true ? WorkoutFormats.SplitTargetValue(step.TargetSplitSeconds!.Value) : null, l10n);
            var watts = Metric(WorkoutFormats.Watts(data.PowerWatts), data.PowerWatts, lights.Watts, TargetArrow.Up,
                step?.TargetWattsMin.HasValue == true ? WorkoutFormats.TargetValue(step.TargetWattsMin!.Value) : null, l10n);
            var bpm = data.HeartRate > 0 ? WorkoutFormats.Count(data.HeartRate) : null;
            var controls = snapshot.Phase == WorkoutPhase.Active || snapshot.Phase == WorkoutPhase.Paused;
            return new WorkoutHudState(snapshot.Revision, snapshot.Phase, snapshot.IsRoutine, spm, split, watts,
                WorkoutFormats.Count(data.TotalCalories), data.TotalCalories, bpm, data.HeartRate,
                WorkoutFormats.SessionClock(snapshot.TotalElapsedSeconds), snapshot.TotalElapsedSeconds,
                WorkoutFormats.HudDistance(data.DistanceMeters), data.DistanceMeters,
                progress.HasValue ? StepRow(progress.Value, l10n) : null, controls, snapshot.Phase == WorkoutPhase.Paused, snapshot.Instant);
        }

        private static HudMetric Metric(string value, double rollValue, TargetState state, TargetArrow offArrow, string? target, Localizer l10n)
        {
            if (state == TargetState.None)
            {
                return new HudMetric(value, rollValue, false, false, TargetArrow.None, null);
            }

            var off = state == TargetState.Off;
            var text = target != null ? l10n.Format(L10nKeys.HudTarget, MessageArg.Text("value", target)) : null;
            return new HudMetric(value, rollValue, true, off, off ? offArrow : TargetArrow.None, text);
        }

        // :385-471: el tipo, la pastilla de objetivo (DV-35), la cuenta (por tiempo M:SS; por distancia su largo; sin meta ?),
        // "por terminar" y "a continuación".
        private static HudStepRow StepRow(StepProgress progress, Localizer l10n)
        {
            var step = progress.Step;
            var targets = WorkoutFormats.TargetLabel(step, l10n);
            HudTargetLabel? label = null;
            if (targets.Length > 0)
            {
                var parts = TrendText.Split(targets);
                label = new HudTargetLabel(parts.Text, parts.Before, parts.HasTrend, parts.After);
            }

            string countdown;
            double countdownValue;
            var remaining = progress.RemainingInStep;
            if (remaining.HasValue)
            {
                countdown = WorkoutFormats.Countdown(remaining.Value);
                countdownValue = remaining.Value;
            }
            else if (step.DistanceMeters.HasValue)
            {
                countdown = WorkoutFormats.StepDistance(step.DistanceMeters.Value, l10n);
                countdownValue = step.DistanceMeters.Value;
            }
            else
            {
                countdown = WorkoutFormats.StepNoGoal(l10n);
                countdownValue = 0;
            }

            StepType? upNext = null;
            string? upNextText = null;
            if (progress.IsUpNext && progress.NextStep != null)
            {
                upNext = progress.NextStep.Type;
                upNextText = l10n.Get(StepTypeInfo.NameKey(progress.NextStep.Type));
            }

            return new HudStepRow(step.Type, l10n.Get(StepTypeInfo.NameKey(step.Type)), label, countdown, countdownValue,
                progress.IsEndingSoon, upNext, upNextText);
        }

        private void OnSnapshot(WorkoutSnapshot snapshot)
        {
            _last = snapshot;
            Rebuild();
        }

        private void Rebuild()
        {
            if (_disposed)
            {
                return;
            }

            _state.Value = Build(_last, _texts.Value);
        }

        // Cada estado es nuevo (sale de una foto nueva o de un idioma nuevo): sin comparar campo por campo.
        private sealed class HudStateIdentity : System.Collections.Generic.IEqualityComparer<WorkoutHudState>
        {
            public static readonly HudStateIdentity Instance = new HudStateIdentity();

            public bool Equals(WorkoutHudState? x, WorkoutHudState? y)
            {
                return ReferenceEquals(x, y);
            }

            public int GetHashCode(WorkoutHudState obj)
            {
                return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
            }
        }
    }
}
