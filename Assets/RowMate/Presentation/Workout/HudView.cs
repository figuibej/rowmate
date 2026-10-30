using System;
using RowMate.App.Workout;
using RowMate.Core.Common;
using RowMate.Core.Design;
using RowMate.Core.Localization;
using RowMate.Core.Models;
using RowMate.Core.Workout;
using RowMate.Presentation.Binding;
using RowMate.Presentation.Controls;
using RowMate.Presentation.Motion;
using RowMate.Presentation.Motion.Hud;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace RowMate.Presentation.Workout
{
    // El HUD del inmersivo (F-WKT-10, -13 a -17, -19, -20; 02-tech-design §6.3 a §6.8 de la It. 4a; 04b-hud-exr/02-tech-design
    // §3): dibuja un WorkoutHudState (los textos ya formateados y el semáforo los arma WorkoutHudStore) y aplica HudGeometry por
    // zona (HudZones). Capas, de atrás hacia adelante (AC-WKT-13-02):
    //   #hud-backdrop                       el fondo provisorio (AC-SCN-01-01); el velo de pausa es su tinte (It. 4b)
    //   #hud-boat-zone                      contorno de depuración de la zona del bote (solo si lo pide el anfitrión)
    //   #hud-topbar                         StepInfoBar (con rutina: píldoras, información y sello) o FreeTopBar (libre: reloj y chip)
    //   #hud-spm                            la tarjeta grande (zona arriba a la izquierda)
    //   #hud-zone-right > #hud-split, #hud-watts      (zona arriba a la derecha)
    //   #hud-zone-vitals > #hud-kcal, #hud-bpm        (columna en vertical; fila a la altura de los controles en horizontal)
    //   #hud-band                           tiempo │ distancia (zona abajo a la izquierda)
    //   #hud-big-countdown, #hud-paused-label  el disco del lugar "centro alto": la cuenta grande o PAUSA (CenterCallouts, It. 4b)
    //   #hud-controls                       Terminar, Pausa | Reanudar (zona abajo al centro)
    // Valores en vivo (AC-WKT-17-06): SetValue en cada estado, también en pausa; con Instant (el primer estado después del
    // segundo plano o del despertar) todo va a su estado final en el acto, sin rodillo ni empujón (AC-WKT-02-08). La tarjeta BPM
    // aparece con pulso > 0 (AC-WKT-17-05) y kcal se desliza (FLIP, KcalSlideMotion; AC-WKT-17-11).
    // It. 4b: la última foto (Freeze, O-01): desde el primer estado de reposo el HUD no aplica datos (la salida muestra lo último
    // de Finished); el velo de pausa (PauseVeilMotion) es el tinte del fondo; la cascada de entrada y la salida las hace la página
    // con ImmersiveCascadeMotion e ImmersiveExitMotion sobre las zonas (Pieces). Dispose detiene TODO lo que se mueve (§3.12).
    public sealed class HudView : VisualElement, IDisposable
    {
        public const float VitalsGapDp = 8;
        public const float RightGapDp = 8;

        private readonly UiContext _ui;
        private readonly Texture2D _glow;
        private readonly KcalSlideMotion _kcalSlide;
        private readonly PauseVeilMotion _veil;
        private readonly System.Diagnostics.Stopwatch _work = new System.Diagnostics.Stopwatch();
        private WorkoutHudState? _state;
        private int? _stepIndex;
        private bool? _bpmShown;
        private bool _inRow;
        private bool _minWidth = true;
        private bool _frozen;
        private bool _disposed;

        public HudView(UiContext ui, bool isRoutine, IReadOnlyObservable<WallClockState> clock, Action finish, Action pause, Action resume)
        {
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            if (clock == null)
            {
                throw new ArgumentNullException(nameof(clock));
            }

            name = "hud";
            AddToClassList("rm-hud");
            pickingMode = PickingMode.Ignore;
            _glow = GlowTexture.Create();

            Backdrop = new HudBackdrop();
            Add(Backdrop);

            if (ui.BoatZoneOutline)
            {
                BoatZone = new VisualElement { name = "hud-boat-zone" };
                BoatZone.AddToClassList("rm-hud-boat-zone");
                BoatZone.pickingMode = PickingMode.Ignore;
                var outline = TokenColors.ToColor(Tokens.DevSimulator);
                BoatZone.style.borderLeftColor = outline;
                BoatZone.style.borderRightColor = outline;
                BoatZone.style.borderTopColor = outline;
                BoatZone.style.borderBottomColor = outline;
                Add(BoatZone);
            }

            IsRoutine = isRoutine;
            if (isRoutine)
            {
                Steps = new StepInfoBar(ui, clock);
                TopBar = Steps;
            }
            else
            {
                Free = new FreeTopBar(ui, clock);
                TopBar = Free;
            }

            Add(TopBar);

            Spm = new HudCard(ui, "hud-spm", MetricKind.Spm, HudCardSize.Large, true, false, false);
            Spm.SetGlowTexture(_glow);
            Add(Spm);

            RightZone = new VisualElement { name = "hud-zone-right" };
            RightZone.AddToClassList("rm-hud-zone-right");
            RightZone.pickingMode = PickingMode.Ignore;
            Split = new HudCard(ui, "hud-split", MetricKind.Split, HudCardSize.Medium, true, false, false);
            Split.SetGlowTexture(_glow);
            Split.style.marginBottom = RightGapDp;
            Watts = new HudCard(ui, "hud-watts", MetricKind.Watts, HudCardSize.Medium, true, true, false);
            Watts.SetGlowTexture(_glow);
            RightZone.Add(Split);
            RightZone.Add(Watts);
            Add(RightZone);

            VitalsZone = new VisualElement { name = "hud-zone-vitals" };
            VitalsZone.AddToClassList("rm-hud-zone-vitals");
            VitalsZone.pickingMode = PickingMode.Ignore;
            Kcal = new HudCard(ui, "hud-kcal", MetricKind.Calories, HudCardSize.Small, false, false, false);
            Bpm = new HudCard(ui, "hud-bpm", MetricKind.HeartRate, HudCardSize.Small, false, false, true);
            Bpm.style.display = DisplayStyle.None;
            VitalsZone.Add(Kcal);
            VitalsZone.Add(Bpm);
            Add(VitalsZone);
            _kcalSlide = new KcalSlideMotion(ui.Motion, ApplyKcalSlide, OnBpmGone);

            Band = new HudBand(ui);
            Add(Band);

            Callouts = new CenterCallouts(ui, Steps?.PillRow.Progress ?? new PillProgress());
            Add(Callouts.Count);
            Add(Callouts.Paused);

            Controls = new HudControls(ui, finish, pause, resume);
            Add(Controls);

            _veil = new PauseVeilMotion(ui.Motion, a => Backdrop.SetVeil(a));

            SetVitalsArrangement(false, true);
            // Lo que se mide para HudGeometry (GeometryChangedEvent no se propaga: cada elemento avisa).
            foreach (var card in new[] { Spm, Split, Watts, Kcal, Bpm })
            {
                Watch(card.Number);
                Watch(card.Label);
                if (card.Target != null)
                {
                    Watch(card.Target);
                }
            }

            Watch(Band.Time);
            Watch(Band.Distance);
            this.Bind(ui.App.Texts, RenderTexts);
        }

        // Un ancho natural pudo cambiar (un número más largo, otro idioma, un obj. nuevo): HudZones decide si rehace la
        // disposición.
        public event Action? NaturalChanged;

        public bool IsRoutine { get; }

        public HudBackdrop Backdrop { get; }

        public VisualElement? BoatZone { get; }

        public VisualElement TopBar { get; }

        public StepInfoBar? Steps { get; }

        public FreeTopBar? Free { get; }

        public HudCard Spm { get; }

        public VisualElement RightZone { get; }

        public HudCard Split { get; }

        public HudCard Watts { get; }

        public VisualElement VitalsZone { get; }

        public HudCard Kcal { get; }

        public HudCard Bpm { get; }

        public HudBand Band { get; }

        public CenterCallouts Callouts { get; }

        public HudControls Controls { get; }

        public WorkoutHudState? State => _state;

        // El reloj de pared de la barra que corresponde.
        public WallClockView WallClock => Steps != null ? Steps.Clock : Free!.Clock;

        public KcalSlideMotion KcalSlide => _kcalSlide;

        public PauseVeilMotion Veil => _veil;

        public bool HasBpm => _bpmShown == true;

        public bool SpmHasTarget => Spm.HasTarget;

        public bool SplitHasTarget => Split.HasTarget;

        public bool WattsHasTarget => Watts.HasTarget;

        public bool VitalsInRow => _inRow;

        // La última foto (O-01): ya no aplica estados.
        public bool IsFrozen => _frozen;

        // El trabajo del último Apply, en ms (HudBudgetTests.LongSeriesStaysSmooth).
        public double LastApplyMs { get; private set; }

        // La ventana en dp (el tamaño de la página).
        public Vector2 WindowSize => new Vector2(layout.width, layout.height);

        // Algo del HUD en movimiento (regla de toma): kcal y BPM.
        public bool IsAnimating => _kcalSlide.IsRunning;

        // Las piezas de la cascada y de la salida (ImmersiveCascadeMotion, ImmersiveExitMotion: HudPiece).
        public VisualElement? PieceOf(HudPiece piece)
        {
            switch (piece)
            {
                case HudPiece.TopBar:
                    return TopBar;
                case HudPiece.Spm:
                    return Spm;
                case HudPiece.SplitWatts:
                    return RightZone;
                case HudPiece.Band:
                    return Band;
                case HudPiece.Vitals:
                    return VitalsZone;
                case HudPiece.Controls:
                    return Controls;
                default:
                    return null;
            }
        }

        // Un estado del HUD. stepIndex: el índice expandido del paso del motor (para "dentro del mismo paso", §5.3).
        public void Apply(WorkoutHudState state, int? stepIndex)
        {
            if (_disposed || _frozen || state == null)
            {
                return;
            }

            _work.Restart();
            var instant = state.Instant || _state == null;
            var stepChanged = _state != null && stepIndex != _stepIndex;
            _state = state;
            _stepIndex = stepIndex;

            Spm.SetMetric(state.Spm, stepChanged, instant);
            Split.SetMetric(state.Split, stepChanged, instant);
            Watts.SetMetric(state.Watts, stepChanged, instant);
            Kcal.SetValue(state.Kcal, state.KcalValue, instant);
            var hasBpm = state.Bpm != null;
            if (hasBpm)
            {
                Bpm.SetValue(state.Bpm!, state.BpmValue, instant);
            }

            SetBpm(hasBpm, instant);
            Bpm.Heartbeat?.SetState(true, hasBpm, state.BpmValue);
            Band.SetValues(state.Clock, state.ClockValue, state.Distance, state.DistanceValue, instant);
            Steps?.Apply(state, stepChanged, instant);
            Free?.Apply(state, instant);
            var paused = state.Phase == WorkoutPhase.Paused;
            Callouts.Apply(state.BigCount, paused, state.Step?.Type, instant);
            _veil.SetPaused(paused, instant);
            // Guardando (el spinner de Terminar) solo con un Terminar a mano: con la rutina terminada sola, el diálogo de completado
            // tiene su propio Terminar (§3.9).
            Controls.Apply(state.ControlsEnabled, state.ShowResume, state.Phase == WorkoutPhase.Finished && state.EndedBy != WorkoutEndKind.Completed);
            _work.Stop();
            LastApplyMs = _work.Elapsed.TotalMilliseconds;
        }

        // La última foto (O-01; DD4b-05): desde el primer estado de reposo, nada más cambia (los valores, la barra con sus píldoras
        // o la libre con su chip, el sello, la cuenta y el reloj de la banda y el de pared). Lo finito que corría termina con su
        // reloj; lo que se repite sigue hasta que la página sale del árbol.
        public void Freeze()
        {
            _frozen = true;
            WallClock.Freeze();
        }

        // Natural a tamaño base de lo que va en cada zona (HudNaturalWidths, DN4a-13).
        public HudNaturalWidths MeasureNatural()
        {
            return new HudNaturalWidths(Spm.NaturalValueWidth, Split.NaturalValueWidth, Watts.NaturalValueWidth, Kcal.NaturalValueWidth,
                Bpm.NaturalValueWidth, Band.NaturalTimeWidth, Band.NaturalDistanceWidth, Spm.NaturalLabelWidth, Split.NaturalLabelWidth,
                Watts.NaturalLabelWidth, Kcal.NaturalLabelWidth, Bpm.NaturalLabelWidth, Spm.NaturalTargetWidth, Split.NaturalTargetWidth,
                Watts.NaturalTargetWidth);
        }

        // kcal y BPM en columna (vertical: kcal arriba, BPM abajo, el ancho del mayor con mínimo 96) o en fila a la altura de los
        // controles (horizontal: kcal en el rincón, BPM a su izquierda; con los mínimos solo si entran los dos, DV-33).
        public void SetVitalsArrangement(bool inRow, bool minWidth)
        {
            _inRow = inRow;
            _minWidth = minWidth;
            VitalsZone.style.flexDirection = inRow ? FlexDirection.RowReverse : FlexDirection.Column;
            VitalsZone.style.alignItems = inRow ? Align.FlexEnd : Align.Stretch;
            var min = minWidth ? (float)HudGeometry.SmallCardMinWidth : 0f;
            Kcal.style.minWidth = min;
            Bpm.style.minWidth = min;
            ApplyVitalsGap();
        }

        public void RenderTexts(Localizer texts)
        {
            Spm.SetLabel(texts.Get(L10nKeys.HudSpm));
            Split.SetLabel(texts.Get(L10nKeys.HudSplit));
            Watts.SetLabel(texts.Get(L10nKeys.HudWatts));
            Watts.SetUnit(texts.Get(L10nKeys.HudUnitWatts));
            Kcal.SetLabel(texts.Get(L10nKeys.HudKcal));
            Bpm.SetLabel(texts.Get(L10nKeys.HudBpm));
            Band.SetUnit(texts.Get(L10nKeys.HudUnitMeters));
            Free?.RenderTexts(texts);
            Steps?.RenderTexts(texts);
            Callouts.RenderTexts(texts);
            Controls.RenderTexts(texts);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            // QA-V4a-02 = QA-V4a-ROB-02 y It. 4b §3.12: las animaciones del HUD salen del MotionDriver con él (Settle no espera las
            // continuas y nadie más las para): el latido de BPM, el pulso de la fila, la fila de píldoras, el plan de la fila de
            // información, el sello, el disco del centro, el velo, el chip con su punto y kcal.
            Bpm.Heartbeat?.Stop();
            Steps?.Dispose();
            Free?.Dispose();
            Callouts.Dispose();
            _veil.Stop();
            _kcalSlide.Stop();
            Controls.Dispose();
            Backdrop.Dispose();
            if (Application.isPlaying)
            {
                Object.Destroy(_glow);
            }
            else
            {
                Object.DestroyImmediate(_glow);
            }
        }

        // La tarjeta BPM aparece y se va con el pulso (04b-hud-exr/02-tech-design §3.8, FLIP): aparece en el árbol en el acto y
        // kcal se desliza desde su lugar viejo; al irse, BPM funde en su lugar y después sale del árbol (ahí kcal se desliza). Sin
        // armar o con Instant, en el acto.
        private void SetBpm(bool shown, bool instant)
        {
            if (_bpmShown == shown)
            {
                return;
            }

            var first = _bpmShown == null;
            _bpmShown = shown;
            var animate = !first && !instant && _ui.Motion.Armed;
            // En vertical, kcal (arriba) sube o baja el alto de BPM más su separación; en horizontal está en el rincón.
            // Sin layout todavía (el primer estado ya trae BPM) el alto es NaN: sin deslizarse (QA-V4b-07).
            var kcalHeight = Kcal.layout.height;
            var distance = _inRow || float.IsNaN(kcalHeight) ? 0 : kcalHeight + VitalsGapDp;
            if (shown)
            {
                Bpm.style.display = DisplayStyle.Flex;
                ApplyVitalsGap();
                _kcalSlide.Appear(distance, !animate);
                return;
            }

            if (!animate)
            {
                _kcalSlide.Disappear(true);
                return;
            }

            _kcalSlide.Disappear(false, -distance);
        }

        private void ApplyKcalSlide(KcalVisual visual)
        {
            var shift = _kcalSlide.ShiftDp;
            Kcal.style.translate = Math.Abs(shift) < 1e-4 ? (StyleTranslate)StyleKeyword.Null : new Translate(0, (float)shift);
            var opacity = visual.BpmOpacity;
            Bpm.style.opacity = opacity >= 0.9995 ? (StyleFloat)StyleKeyword.Null : (float)opacity;
        }

        private void OnBpmGone()
        {
            if (_bpmShown == false)
            {
                Bpm.style.display = DisplayStyle.None;
                Bpm.style.opacity = StyleKeyword.Null;
                ApplyVitalsGap();
            }
        }

        private void Watch(VisualElement element)
        {
            element.RegisterCallback<GeometryChangedEvent>(OnWatchedGeometry);
        }

        private void OnWatchedGeometry(GeometryChangedEvent evt)
        {
            if (!_disposed && Math.Abs(evt.newRect.width - evt.oldRect.width) > 0.01f)
            {
                NaturalChanged?.Invoke();
            }
        }

        private void ApplyVitalsGap()
        {
            var withBpm = Bpm.style.display != DisplayStyle.None;
            Kcal.style.marginBottom = !_inRow && withBpm ? VitalsGapDp : 0f;
            Kcal.style.marginLeft = _inRow && withBpm ? VitalsGapDp : 0f;
        }
    }
}
