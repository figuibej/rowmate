using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RowMate.Core.Common;
using RowMate.Core.Localization;
using RowMate.Core.Models;
using RowMate.Core.Ports;
using RowMate.Core.Routines;
using RowMate.Core.Text;
using RowMate.Core.Workout;

namespace RowMate.App.Workout
{
    // El HUD del inmersivo (F-WKT-10, -13, -14, -16, -17, -20; 02-tech-design §2.11 de la It. 4a): rehace un WorkoutHudState con
    // los textos ya formateados y el semáforo con cada foto del motor y con cada cambio de idioma (unas pocas cadenas por
    // lectura, a 1-3 Hz). En segundo plano el motor no publica fotos, así que durante el alcance no rehace nada: arma UN
    // estado al volver, con Instant (QA-G4aT-07). Uno por inmersivo: lo crea AppContext.CreateHud() y lo dispone la página.
    // It. 4b (02-tech-design §2.11): las píldoras (con una caché que se rehace solo en el cambio de paso), la cuenta grande, el
    // sello, el chip de fase, los totales del diálogo, el reloj de pared (WallClockTicker, uno por inmersivo) y el Terminar del
    // diálogo (AcknowledgeCompletionAsync).
    // legacy: lib/features/workout/immersive_workout_screen.dart:330-475 (fila del paso), :560-733 (tarjetas y banda),
    // :893-953 (controles), :955-1019 (el resumen del diálogo).
    public sealed class WorkoutHudStore : IDisposable
    {
        private readonly WorkoutEngine _engine;
        private readonly IReadOnlyObservable<Localizer> _texts;
        private readonly ObservableValue<WorkoutHudState> _state;
        private readonly IDisposable _engineSubscription;
        private readonly IDisposable _textsSubscription;
        private readonly WallClockTicker _wallClock;
        private WorkoutSnapshot _last;
        private bool _disposed;

        // Cachés (§2.11; 0 B por cuadro): los tipos por rutina expandida, los aspectos por paso en curso, la pastilla por paso
        // y el sello por paso cerrado.
        private IReadOnlyList<IntervalStep>? _pillSteps;
        private IReadOnlyList<StepType>? _pillTypes;
        private int _looksCurrent = -1;
        private IReadOnlyList<StepPillLook>? _pillLooks;
        private IntervalStep? _labelStep;
        private Localizer? _labelTexts;
        private HudTargetLabel? _label;
        private long _sealSession = -1;
        private int _sealStep = -1;
        private Localizer? _sealTexts;
        private HudSeal? _seal;

        // It. 5 (§2.5): los paneles, por (sesión, revisión del tracker o 0 con DV-48, idioma); las filas que siguen a la vista se
        // reusan (la HudLapRow del #3 es la misma instancia mientras se ve).
        private long _seriesSession = -1;
        private long _seriesRevision = -1;
        private Localizer? _seriesTexts;
        private HudSeries? _series;

        // QA-V5-P01 (verificación de la 5): la lista del tracker de la que salieron las filas de repeticiones de _series. El
        // tracker la REEMPLAZA en cada cambio (copia al escribir, SeriesTracker.EnterStep), así que la misma instancia es el mismo
        // contenido: las filas se reusan solo así, nunca por Rep y metros (dos series de la misma forma pueden coincidir en los
        // dos, y en segundo plano no hay un segundo con la lista vacía entre ellas).
        private IReadOnlyList<RepSummary>? _seriesRepsSource;

        public WorkoutHudStore(WorkoutEngine engine, IReadOnlyObservable<Localizer> texts, IScheduler scheduler, IAppLifecycle lifecycle, IAppLogger logger)
        {
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));
            _texts = texts ?? throw new ArgumentNullException(nameof(texts));
            if (scheduler == null)
            {
                throw new ArgumentNullException(nameof(scheduler));
            }

            if (lifecycle == null)
            {
                throw new ArgumentNullException(nameof(lifecycle));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _last = engine.State.Value;
            _wallClock = new WallClockTicker(scheduler, lifecycle, logger);
            WallClock = _wallClock.State;
            _state = new ObservableValue<WorkoutHudState>(Compose(_last, texts.Value), logger, HudStateIdentity.Instance);
            State = new ReadOnlyObservable<WorkoutHudState>(_state);
            _engineSubscription = engine.State.Subscribe(OnSnapshot, emitCurrent: false);
            _textsSubscription = texts.Subscribe(_ => Rebuild(), emitCurrent: false);
        }

        public IReadOnlyObservable<WorkoutHudState> State { get; }

        // El reloj de pared de este inmersivo (F-WKT-19).
        public IReadOnlyObservable<WallClockState> WallClock { get; }

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

        // El Terminar del diálogo de rutina completada (AC-WKT-21-06): la tarea se completa cuando el motor vuelve a reposo.
        public Task AcknowledgeCompletionAsync()
        {
            return _disposed ? Task.CompletedTask : _engine.AcknowledgeCompletion();
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
            _wallClock.Dispose();
        }

        // El estado del HUD de una foto (la regla de cada campo: 02-tech-design §2.11 de la It. 4a, tabla "Campo del HUD"). Lo de
        // la 4a; los campos de la It. 4b los suma Compose con el motor (las píldoras y el sello leen la rutina y los objetivos).
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
                progress.HasValue ? StepRow(progress.Value, l10n, null) : null, controls, snapshot.Phase == WorkoutPhase.Paused, snapshot.Instant);
        }

        // Los fragmentos de la pastilla (AC-WKT-14-03; DN4b-16): StepLabels.Targets partido por " · " (las progresiones van
        // juntas con ", " dentro de su 📈: la tendencia es un fragmento).
        public static IReadOnlyList<HudTargetFragment> Fragments(string targets)
        {
            if (targets == null)
            {
                throw new ArgumentNullException(nameof(targets));
            }

            if (targets.Length == 0)
            {
                return Array.Empty<HudTargetFragment>();
            }

            var parts = targets.Split(new[] { StepLabels.PartSeparator }, StringSplitOptions.None);
            var fragments = new HudTargetFragment[parts.Length];
            for (var i = 0; i < parts.Length; i++)
            {
                fragments[i] = new HudTargetFragment(parts[i], parts[i].StartsWith(TrendText.TrendEmoji, StringComparison.Ordinal));
            }

            return fragments;
        }

        // Los totales del diálogo con el formato del legacy (:965-991).
        public static HudCompletion CompletionTexts(CompletionSummary summary)
        {
            return new HudCompletion(WorkoutFormats.SessionClock(summary.ActiveSeconds), summary.ActiveSeconds,
                WorkoutFormats.FinalDistance(summary.DistanceMeters), summary.DistanceMeters, WorkoutFormats.FinalCalories(summary.Calories),
                summary.Calories);
        }

        // El texto del sello (§2.5): hudSealMet / hudSealMissed y cada fragmento con hudSealWatts, hudSealSpm o hudSealSplit
        // ({avg}, {target}), unidos por " · ". Los mismos textos en es y en.
        public static HudSeal SealTexts(SealContent content, double at, Localizer l10n)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            if (l10n == null)
            {
                throw new ArgumentNullException(nameof(l10n));
            }

            var parts = new string[content.Fragments.Count];
            for (var i = 0; i < parts.Length; i++)
            {
                var fragment = content.Fragments[i];
                var key = fragment.Metric == SealMetric.Watts ? L10nKeys.HudSealWatts : fragment.Metric == SealMetric.Spm ? L10nKeys.HudSealSpm : L10nKeys.HudSealSplit;
                parts[i] = l10n.Format(key, MessageArg.Text("avg", fragment.Average), MessageArg.Text("target", fragment.Target));
            }

            var title = l10n.Get(content.Verdict == StepResult.Met ? L10nKeys.HudSealMet : L10nKeys.HudSealMissed);
            return new HudSeal(content.StepIndex, content.Verdict, title, string.Join(StepLabels.PartSeparator, parts), at);
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
        // "por terminar" y "a continuación". cachedLabel: la pastilla ya armada para este paso (se arma una vez por paso).
        private static HudStepRow StepRow(StepProgress progress, Localizer l10n, HudTargetLabel? cachedLabel)
        {
            var step = progress.Step;
            var label = cachedLabel ?? TargetLabel(step, l10n);
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

        private static HudTargetLabel? TargetLabel(IntervalStep step, Localizer l10n)
        {
            var targets = WorkoutFormats.TargetLabel(step, l10n);
            if (targets.Length == 0)
            {
                return null;
            }

            var parts = TrendText.Split(targets);
            return new HudTargetLabel(parts.Text, parts.Before, parts.HasTrend, parts.After) { Fragments = Fragments(targets) };
        }

        // Lo de la 4a más los campos de la It. 4b (§2.11).
        private WorkoutHudState Compose(WorkoutSnapshot snapshot, Localizer l10n)
        {
            var progress = snapshot.Step;
            var baseState = Build(snapshot, l10n);
            if (progress.HasValue)
            {
                var step = progress.Value.Step;
                if (!ReferenceEquals(step, _labelStep) || !ReferenceEquals(l10n, _labelTexts))
                {
                    _labelStep = step;
                    _labelTexts = l10n;
                    _label = TargetLabel(step, l10n);
                }

                baseState = baseState with { Step = StepRow(progress.Value, l10n, _label) };
            }

            HudPhaseChip? chip = null;
            if (!snapshot.IsRoutine)
            {
                var spec = PhaseChip.For(snapshot.Phase);
                if (spec.HasValue)
                {
                    chip = new HudPhaseChip(l10n.Get(spec.Value.Key), spec.Value.Base, spec.Value.Text, spec.Value.LiveDot);
                }
            }

            return baseState with
            {
                Session = snapshot.Session,
                EndedBy = snapshot.EndedBy,
                Pills = Pills(snapshot),
                BigCount = BigCountdown.ValueOf(progress, snapshot.Phase),
                Seal = Seal(snapshot, l10n),
                Chip = chip,
                Completion = snapshot.EndedBy == WorkoutEndKind.Completed && snapshot.Completion.HasValue ? CompletionTexts(snapshot.Completion.Value) : null,
                CloseReturned = snapshot.Close == CloseStatus.Saved || snapshot.Close == CloseStatus.Failed,
                Series = SeriesFor(snapshot, l10n),
            };
        }

        // It. 5 (§2.5; AC-WKT-18-04 a -08, -22): los paneles de un estado del tracker, con los textos hechos. Pura (las pruebas
        // de LT-59 y LT-60 arman el tracker a mano y pasan por acá: QA-G5P-05). Pasa por SeriesPanels.For (la visibilidad, las
        // 4 últimas, DV-48). null sin rutina (el libre: sin paneles ni lugar reservado, AC-WKT-18-01).
        // Textos: Label = hudLapNumber, Time = LapTime; la vuelta en curso con "{metros} m" (hudUnitMeters); RepProgress =
        // hudRepProgress con PanelMeters solo en serie; las repeticiones con hudRepLabel, RepSplit y PanelMeters (el punto de
        // miles también en inglés: Q-06, P-08).
        public static HudSeries? ComposeSeries(SeriesState? state, IReadOnlyList<IntervalStep>? steps, IReadOnlyList<StepPosition>? positions,
                                               Localizer localizer, long session)
        {
            return ComposeSeries(state, steps, positions, localizer, session, null, null);
        }

        // previous: el estado anterior con el mismo idioma (o null); previousRepsSource: la lista del tracker de la que salieron sus
        // filas de repeticiones (QA-V5-P01).
        private static HudSeries? ComposeSeries(SeriesState? state, IReadOnlyList<IntervalStep>? steps, IReadOnlyList<StepPosition>? positions,
                                                Localizer localizer, long session, HudSeries? previous, IReadOnlyList<RepSummary>? previousRepsSource)
        {
            if (localizer == null)
            {
                throw new ArgumentNullException(nameof(localizer));
            }

            var content = SeriesPanels.For(state, steps, positions);
            if (!content.Visible)
            {
                return null;
            }

            var reuse = previous != null && previous.Session == session && previous.StepIndex == content.StepIndex ? previous : null;
            var laps = LapRows(content, localizer, reuse?.Laps);
            var sameReps = previous != null && previous.Session == session && ReferenceEquals(content.PreviousReps, previousRepsSource);
            var reps = RepRows(content.PreviousReps, localizer, sameReps ? previous!.Reps : null);
            var meters = Fmt.Integer(content.CurrentLapMeters) + " " + localizer.Get(L10nKeys.HudUnitMeters);
            string? progress = null;
            if (content.InSeries)
            {
                progress = localizer.Format(L10nKeys.HudRepProgress, MessageArg.Text("rep", Fmt.Integer(content.Rep)),
                    MessageArg.Text("count", Fmt.Integer(content.RepCount)), MessageArg.Text("meters", WorkoutFormats.PanelMeters(content.RepMeters)));
            }

            return new HudSeries(session, content.StepIndex, content.IsWork, WorkoutFormats.LapTime(content.CurrentLapSeconds), content.CurrentLapSeconds,
                meters, content.CurrentLapMeters, laps, content.LapCount, content.InSeries, content.Rep, progress, content.RepMeters, reps);
        }

        // Las filas de vueltas a la vista (la más reciente primero). Reusa cada fila del estado anterior del mismo paso con el
        // mismo número, y la lista entera si no cambió nada.
        private static IReadOnlyList<HudLapRow> LapRows(in SeriesPanelsContent content, Localizer l10n, IReadOnlyList<HudLapRow>? previous)
        {
            var count = content.VisibleLaps;
            if (count == 0)
            {
                return Array.Empty<HudLapRow>();
            }

            if (previous != null && previous.Count == count && previous[0].Number == content.Lap0.Number)
            {
                return previous;
            }

            var rows = new HudLapRow[count];
            for (var i = 0; i < count; i++)
            {
                var lap = content.VisibleLap(i);
                HudLapRow? kept = null;
                if (previous != null)
                {
                    for (var j = 0; j < previous.Count; j++)
                    {
                        if (previous[j].Number == lap.Number)
                        {
                            kept = previous[j];
                            break;
                        }
                    }
                }

                rows[i] = kept ?? new HudLapRow(lap.Number, l10n.Format(L10nKeys.HudLapNumber, MessageArg.Text("number", Fmt.Integer(lap.Number))),
                    WorkoutFormats.LapTime(lap.Seconds), lap.Seconds);
            }

            return rows;
        }

        // Las filas de repeticiones anteriores (hasta 3, la más reciente primero). sameSource: las filas que salieron de ESTA
        // misma lista del tracker (la misma instancia: el tracker reemplaza su arreglo en cada cambio), que se reusan enteras; si
        // no, se arman de nuevo desde el tracker, como el legacy en cada dibujo (series_panels.dart:330-345). Nunca se reusan por
        // Rep y metros: dos series de la misma forma pueden coincidir en los dos con otro split (QA-V5-P01).
        private static IReadOnlyList<HudRepRow> RepRows(IReadOnlyList<RepSummary> reps, Localizer l10n, IReadOnlyList<HudRepRow>? sameSource)
        {
            if (reps == null || reps.Count == 0)
            {
                return Array.Empty<HudRepRow>();
            }

            if (sameSource != null && sameSource.Count == reps.Count)
            {
                return sameSource;
            }

            var rows = new HudRepRow[reps.Count];
            for (var i = 0; i < rows.Length; i++)
            {
                var rep = reps[i];
                var split = rep.WorkSplitSeconds;
                rows[i] = new HudRepRow(rep.Rep, l10n.Format(L10nKeys.HudRepLabel, MessageArg.Text("rep", Fmt.Integer(rep.Rep))),
                    WorkoutFormats.RepSplit(split), split ?? 0, WorkoutFormats.PanelMeters(rep.TotalMeters), rep.TotalMeters);
            }

            return rows;
        }

        // La caché (§2.5): entre dos segundos (una lectura del remo en k − 0,5, un pulso, el reloj de pared) devuelve la misma
        // instancia sin armar nada; por segundo, tres cadenas (el tiempo y los metros de la vuelta en curso y la línea Rep) y el
        // record (lo que ARCHITECTURE §20.2 permite a 1 Hz).
        internal HudSeries? SeriesFor(WorkoutSnapshot snapshot, Localizer l10n)
        {
            var state = snapshot.Series;
            if (state == null || !snapshot.IsRoutine || snapshot.Phase == WorkoutPhase.Idle)
            {
                return null;
            }

            var revision = state.StepIndex.HasValue ? state.Revision : 0;
            if (_series != null && _seriesSession == snapshot.Session && _seriesRevision == revision && ReferenceEquals(_seriesTexts, l10n))
            {
                return _series;
            }

            var previous = _series != null && ReferenceEquals(_seriesTexts, l10n) ? _series : null;
            _series = ComposeSeries(state, _engine.Steps, _engine.Positions, l10n, snapshot.Session, previous, previous != null ? _seriesRepsSource : null);
            _seriesRepsSource = state.PreviousReps;
            _seriesSession = snapshot.Session;
            _seriesRevision = revision;
            _seriesTexts = l10n;
            return _series;
        }

        // Con rutina: los tipos de engine.Steps (una vez por sesión); Looks[i] = StepPills.LookOf(i, en curso, Targets.ResultOf(i))
        // (immersive_workout_screen.dart:363-375), rehechos solo en el cambio de paso (los resultados cambian ahí).
        private HudPillRow? Pills(WorkoutSnapshot snapshot)
        {
            var progress = snapshot.Step;
            var steps = _engine.Steps;
            if (!progress.HasValue || steps == null || steps.Count == 0)
            {
                return null;
            }

            if (!ReferenceEquals(steps, _pillSteps))
            {
                _pillSteps = steps;
                var types = new StepType[steps.Count];
                for (var i = 0; i < types.Length; i++)
                {
                    types[i] = steps[i].Type;
                }

                _pillTypes = types;
                _looksCurrent = -1;
            }

            var current = progress.Value.StepIndex;
            if (current != _looksCurrent || _pillLooks == null)
            {
                var looks = new StepPillLook[steps.Count];
                for (var i = 0; i < looks.Length; i++)
                {
                    looks[i] = StepPills.LookOf(i, current, _engine.Targets.ResultOf(i));
                }

                _pillLooks = looks;
                _looksCurrent = current;
            }

            var step = progress.Value.Step;
            int? duration = StepPills.Fills(step) ? step.DurationSeconds : null;
            return new HudPillRow(_pillTypes!, _pillLooks, current, progress.Value.ElapsedInStep, duration, snapshot.Phase == WorkoutPhase.Active,
                progress.Value.IsEndingSoon);
        }

        // El sello (§2.5): StepSeal.Applies con el último paso cerrado con objetivos; el texto se arma una vez por paso cerrado.
        private HudSeal? Seal(WorkoutSnapshot snapshot, Localizer l10n)
        {
            var outcome = snapshot.LastOutcome;
            var steps = _engine.Steps;
            if (steps == null || !StepSeal.Applies(outcome, steps.Count))
            {
                return null;
            }

            var index = outcome!.Value.StepIndex;
            if (_seal == null || _sealSession != snapshot.Session || _sealStep != index || !ReferenceEquals(_sealTexts, l10n))
            {
                _sealSession = snapshot.Session;
                _sealStep = index;
                _sealTexts = l10n;
                _seal = SealTexts(StepSeal.For(outcome.Value, steps[index]), snapshot.LastOutcomeAt, l10n);
            }

            return _seal;
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

            _state.Value = Compose(_last, _texts.Value);
        }

        // Cada estado es nuevo (sale de una foto nueva o de un idioma nuevo): sin comparar campo por campo.
        private sealed class HudStateIdentity : IEqualityComparer<WorkoutHudState>
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
