using System;
using RowMate.App.Workout;
using RowMate.Core.Design;
using RowMate.Core.Localization;
using RowMate.Core.Models;
using RowMate.Presentation.Binding;
using RowMate.Presentation.Controls;
using RowMate.Presentation.Motion;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace RowMate.Presentation.Workout
{
    // El HUD funcional del inmersivo (F-WKT-10, -13, -14, -16, -17, -20; 02-tech-design §6.3 a §6.8 de la It. 4a). Dibuja un
    // WorkoutHudState (los textos ya formateados y el semáforo los arma WorkoutHudStore) y aplica HudGeometry por zona
    // (HudZones). Capas, de atrás hacia adelante (AC-WKT-13-02):
    //   #hud-backdrop                       el fondo provisorio (AC-SCN-01-01)
    //   #hud-boat-zone                      contorno de depuración de la zona del bote (solo si lo pide el anfitrión)
    //   #hud-topbar                         StepInfoBar (con rutina) o FreeTopBar (libre)
    //   #hud-spm                            la tarjeta grande (zona arriba a la izquierda)
    //   #hud-zone-right > #hud-split, #hud-watts      (zona arriba a la derecha)
    //   #hud-zone-vitals > #hud-kcal, #hud-bpm        (columna en vertical; fila a la altura de los controles en horizontal)
    //   #hud-band                           tiempo │ distancia (zona abajo a la izquierda)
    //   #hud-controls                       Terminar, Pausa | Reanudar (zona abajo al centro)
    // Valores en vivo (AC-WKT-17-06): SetValue en cada estado, también en pausa; con Instant (el primer estado después del
    // segundo plano) todo va a su estado final en el acto, sin rodillo ni empujón (AC-WKT-02-08). La tarjeta BPM aparece con
    // pulso > 0 (AC-WKT-17-05): funde, y su corazón late con HeartbeatAnimator.
    public sealed class HudView : VisualElement, IDisposable
    {
        public const float VitalsGapDp = 8;
        public const float RightGapDp = 8;

        private readonly UiContext _ui;
        private readonly Texture2D _glow;
        private readonly BpmMotion _bpmMotion;
        private WorkoutHudState? _state;
        private int? _stepIndex;
        private bool? _bpmShown;
        private bool _inRow;
        private bool _minWidth = true;
        private bool _disposed;

        public HudView(UiContext ui, bool isRoutine, Action finish, Action pause, Action resume)
        {
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
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
                Steps = new StepInfoBar(ui);
                TopBar = Steps;
            }
            else
            {
                Free = new FreeTopBar(ui);
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
            _bpmMotion = new BpmMotion(ui.Motion, Kcal, Bpm);

            Band = new HudBand(ui);
            Add(Band);

            Controls = new HudControls(ui, finish, pause, resume);
            Add(Controls);

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

                if (card.Unit != null)
                {
                    Watch(card.Unit);
                }
            }

            Watch(Band.Time);
            Watch(Band.Distance);
            Watch(Band.Unit);
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

        public HudControls Controls { get; }

        public WorkoutHudState? State => _state;

        public bool HasBpm => _bpmShown == true;

        public bool SpmHasTarget => Spm.HasTarget;

        public bool SplitHasTarget => Split.HasTarget;

        public bool WattsHasTarget => Watts.HasTarget;

        public bool VitalsInRow => _inRow;

        // La ventana en dp (el tamaño de la página).
        public Vector2 WindowSize => new Vector2(layout.width, layout.height);

        // Algo del HUD en movimiento (regla de toma): la aparición de BPM.
        public bool IsAnimating => _bpmMotion.IsRunning;

        // Un estado del HUD. stepIndex: el índice expandido del paso del motor (para "dentro del mismo paso", §5.3).
        public void Apply(WorkoutHudState state, int? stepIndex)
        {
            if (_disposed || state == null)
            {
                return;
            }

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
            Steps?.Apply(state.Step, stepChanged, instant);
            Controls.Apply(state.ControlsEnabled, state.ShowResume, state.Phase == RowMate.Core.Workout.WorkoutPhase.Finished);
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
            Controls.RenderTexts(texts);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            // QA-V4a-02 = QA-V4a-ROB-02: las animaciones CONTINUAS del HUD salen del MotionDriver con él. Settle no las espera y
            // nadie más las para: el latido de BPM (la foto de reposo conserva el pulso) y el pulso de la fila del paso.
            // Quedaban para siempre, con el árbol del HUD de esa sesión y una demanda de 60 fps cada una (legacy: el controlador
            // del pulso se libera con el widget, immersive_workout_screen.dart:326-328).
            Bpm.Heartbeat?.Stop();
            Steps?.Pulse.Stop();
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

        // La tarjeta BPM aparece y se va con el pulso (04-design-notes §5.6): con animación, funde (y kcal sigue en su lugar); sin
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
            if (shown)
            {
                Bpm.style.display = DisplayStyle.Flex;
                ApplyVitalsGap();
                if (animate)
                {
                    _bpmMotion.PlayIn();
                }
                else
                {
                    _bpmMotion.Finish();
                    Bpm.style.opacity = StyleKeyword.Null;
                }

                return;
            }

            if (animate)
            {
                _bpmMotion.PlayOut(() =>
                {
                    if (_bpmShown == false)
                    {
                        Bpm.style.display = DisplayStyle.None;
                        Bpm.style.opacity = StyleKeyword.Null;
                        ApplyVitalsGap();
                    }
                });
                return;
            }

            _bpmMotion.Finish();
            Bpm.style.display = DisplayStyle.None;
            Bpm.style.opacity = StyleKeyword.Null;
            ApplyVitalsGap();
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

        // BPM aparece: funde 120-360 (ease.enter); se va: funde 0-120 (ease.exit) en su lugar y después sale del árbol visible.
        // Reducir movimiento: 240 / 120.
        private sealed class BpmMotion : TimedMotion
        {
            private readonly HudCard _kcal;
            private readonly HudCard _bpm;
            private Action? _done;
            private bool _in;
            private bool _reduced;

            public BpmMotion(MotionDriver driver, HudCard kcal, HudCard bpm)
                : base(driver)
            {
                _kcal = kcal;
                _bpm = bpm;
            }

            protected override double TotalMs => _in ? (_reduced ? 240 : MotionSpecs.BpmFadeInEndMs) : MotionSpecs.BpmFadeOutMs;

            public void PlayIn()
            {
                Finish();
                _in = true;
                _done = null;
                _reduced = Driver.Settings.ReduceMotion;
                Begin();
            }

            public void PlayOut(Action done)
            {
                Finish();
                _in = false;
                _done = done;
                _reduced = Driver.Settings.ReduceMotion;
                Begin();
            }

            protected override void OnUpdate(double elapsedMs)
            {
                if (_in)
                {
                    var start = _reduced ? 0 : MotionSpecs.BpmFadeInStartMs;
                    var end = _reduced ? 240 : MotionSpecs.BpmFadeInEndMs;
                    _bpm.style.opacity = (float)Easing.Evaluate(Tokens.EaseEnter, Progress(elapsedMs, start, end));
                    return;
                }

                _bpm.style.opacity = (float)(1 - Easing.Evaluate(Tokens.EaseExit, Progress(elapsedMs, 0, MotionSpecs.BpmFadeOutMs)));
            }

            protected override void OnFinished()
            {
                if (_in)
                {
                    _bpm.style.opacity = StyleKeyword.Null;
                }

                var done = _done;
                _done = null;
                done?.Invoke();
            }
        }
    }
}
