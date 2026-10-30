using System;
using RowMate.App.Workout;
using RowMate.Core.Design;
using RowMate.Core.Workout;

namespace RowMate.Presentation.Motion.Hud
{
    // Lo que dibuja la fila de píldoras en un cuadro (lo lee PillRowView en su generateVisualContent).
    // - Current, Fill: la en curso y su relleno continuo (PillProgress: nunca pasa del tick siguiente, nunca baja en el paso).
    // - HaloAlpha: el α del halo de la en curso, a·(0,30 + 0,40·p) (0 sin halo); en el borde se ve HaloAtEdge (0,65·α).
    // - Ending ≥ 0 mientras corre el cambio de paso: la que termina, si quedó sólida (EndingMet) o hueca, el relleno que tenía
    //   (EndingFillFrom: el relleno que se dibuja es EndingFillFrom + (1 − EndingFillFrom)·Change.CompletingFill), los valores
    //   del cambio (Change) y el halo que se apaga (EndingHaloAlpha).
    // - Thumb (las angostas, DN4b-18): el alto y la opacidad del pulgar que se va (el de Ending) y del que llega (el de Current);
    //   fuera del cambio, el que llega entero y el otro apagado. ThumbSwapping: corre el pulgar que pasa (0-150).
    public readonly struct PillRowFrame
    {
        public PillRowFrame(int current, double fill, double haloAlpha, int ending, bool endingMet, double endingFillFrom,
                            in PillChangeVisual change, double endingHaloAlpha, in ThumbVisual thumb, bool thumbSwapping)
        {
            Current = current;
            Fill = fill;
            HaloAlpha = haloAlpha;
            Ending = ending;
            EndingMet = endingMet;
            EndingFillFrom = endingFillFrom;
            Change = change;
            EndingHaloAlpha = endingHaloAlpha;
            Thumb = thumb;
            ThumbSwapping = thumbSwapping;
        }

        public int Current { get; }

        public double Fill { get; }

        public double HaloAlpha { get; }

        public int Ending { get; }

        public bool Changing => Ending >= 0;

        public bool EndingMet { get; }

        public double EndingFillFrom { get; }

        public PillChangeVisual Change { get; }

        public double EndingHaloAlpha { get; }

        public ThumbVisual Thumb { get; }

        public bool ThumbSwapping { get; }
    }

    // La fila de píldoras en movimiento (04b-hud-exr/02-tech-design §3.4, §7.4; 04-design-notes §4.3 a §4.5; AC-WKT-14-10 a
    // -13, -16): el relleno continuo de la en curso, el halo de los últimos 5 s, el cambio de paso y el pulgar que pasa. La vista
    // (PillRowView, WP-B1) le pasa cada estado con OnState y lee Frame; repaint le avisa cuándo volver a dibujar.
    // - El relleno es PillProgress (Progress: lo comparte con CenterDiscMotion para el aro de la cuenta grande).
    // - El halo: α = a·(0,30 + 0,40·p), con p el reloj común de la barra (StepPulse.PulseAt: el mismo de Próximo y de la cuenta) y
    //   a la rampa de 150 (ease.move) que sube en el tick que deja el restante en 5 y baja cuando deja de estar "por terminar".
    //   Es de la en curso: en el cambio de paso la nueva arranca sin halo y el de la que termina se apaga con la rampa del cambio.
    //   En pausa sigue latiendo. Con reducir movimiento, fijo en 0,70 con la rampa.
    // - El cambio de paso (t = 0 en el cuadro en que el estado trae el paso nuevo): StepBarTimeline.PillChange hasta los 340 (la
    //   sólida) o 440 (la hueca) y el pulgar que pasa (ThumbSwap) en 0-150. Con reducir movimiento, sin destello ni trazo
    //   (QA-G4bT-03). Dos cambios seguidos: lo que corría salta a su final y empieza lo nuevo. El último paso no cambia de índice
    //   (Q-52): completa su relleno con el motor y queda en curso.
    // - Fases: F en el cambio de paso (0-440, que incluye el pulgar que pasa); C mientras se ve el halo o se llena la en curso;
    //   fuera de eso, sale. Con Instant o sin armar, todo a su estado final en el acto.
    // - Se repinta solo si cambió algo (§3.4): el borde del relleno avanzó un cuarto de píxel (FillWidthPx: el ancho de la en
    //   curso en píxeles; 0 = cualquier avance), corre el cambio o la rampa, o cambió el halo.
    public sealed class PillRowMotion : HudMotion
    {
        private readonly StepPulse _pulse;
        private readonly Action _repaint;
        private HudPillRow? _row;
        private int _current = -1;
        private int _ending = -1;
        private bool _endingMet;
        private double _endingFillFrom;
        private double _endingRamp;
        private double _changeStart;
        private bool _changeReduced;
        private long _changeEndMs;
        private double _ramp;
        private double _rampFrom;
        private double _rampTarget;
        private double _rampStart = double.NaN;
        private double _lastRepaintFill = double.NaN;
        private double _lastRepaintHalo = double.NaN;
        private PillRowFrame _frame;

        public PillRowMotion(MotionDriver driver, StepPulse pulse, Action repaint, PillProgress? progress = null)
            : base(driver)
        {
            _pulse = pulse ?? throw new ArgumentNullException(nameof(pulse));
            _repaint = repaint ?? throw new ArgumentNullException(nameof(repaint));
            Progress = progress ?? new PillProgress();
            _frame = new PillRowFrame(-1, 0, 0, -1, false, 0, default, 0, StepBarTimeline.ThumbSwap((long)MotionSpecs.PillThumbSwapMs, false), false);
        }

        public override bool IsFinite => IsRunning && _ending >= 0;

        // El relleno continuo de la en curso (y el aro de la cuenta grande: CenterDiscMotion).
        public PillProgress Progress { get; }

        public PillRowFrame Frame => _frame;

        // El ancho de la píldora en curso en píxeles (lo pone la vista con su geometría): el repintado por ¼ px.
        public double FillWidthPx { get; set; }

        // El último estado que recibió.
        public HudPillRow? Row => _row;

        public void OnState(HudPillRow row, bool instant)
        {
            if (row == null)
            {
                throw new ArgumentNullException(nameof(row));
            }

            var now = Now;
            var first = _row == null;
            var animate = !first && CanAnimate(instant);
            var stepChanged = !first && row.Current != _current;
            AdvanceRamp(now);
            if (stepChanged)
            {
                var fillBefore = Progress.Fills ? Progress.ValueAt(now) : 0;
                if (animate)
                {
                    _ending = _current;
                    var look = _current >= 0 && _current < row.Looks.Count ? row.Looks[_current] : StepPillLook.Solid;
                    _endingMet = look != StepPillLook.Hollow;
                    _endingFillFrom = fillBefore;
                    _endingRamp = _ramp;
                    _changeStart = now;
                    _changeReduced = ReduceMotion;
                    _changeEndMs = StepBarTimeline.PillChangeEndMs(_endingMet);
                }
                else
                {
                    _ending = -1;
                }

                // La nueva arranca sin halo (sube con su rampa si ya está "por terminar").
                _ramp = 0;
                _rampFrom = 0;
                _rampTarget = 0;
                _rampStart = double.NaN;
            }
            else if (!animate)
            {
                _ending = -1;
            }

            Progress.OnState(row.Current, row.Elapsed, row.Duration, row.Running, now, instant);

            var target = row.EndingSoon ? 1.0 : 0.0;
            if (target != _rampTarget)
            {
                _rampTarget = target;
                if (animate)
                {
                    _rampFrom = _ramp;
                    _rampStart = now;
                }
                else
                {
                    _ramp = target;
                    _rampFrom = target;
                    _rampStart = double.NaN;
                }
            }
            else if (!animate && !double.IsNaN(_rampStart))
            {
                _ramp = _rampTarget;
                _rampStart = double.NaN;
            }

            var changed = first || stepChanged || !ReferenceEquals(row.Looks, _row!.Looks) || row.Running != _row.Running
                          || row.EndingSoon != _row.EndingSoon || row.Duration != _row.Duration;
            _row = row;
            _current = row.Current;
            Compose(now);
            RepaintIfNeeded(changed);
            if (NeedsTicks())
            {
                Run();
            }
            else
            {
                Leave();
            }
        }

        protected override bool Advance(double nowSeconds)
        {
            var wasChanging = _ending >= 0;
            var wasRamping = !double.IsNaN(_rampStart);
            Compose(nowSeconds);
            RepaintIfNeeded(wasChanging || wasRamping);
            return NeedsTicks();
        }

        protected override void OnStopped()
        {
            _ending = -1;
            if (!double.IsNaN(_rampStart))
            {
                _ramp = _rampTarget;
                _rampStart = double.NaN;
            }
        }

        private bool NeedsTicks()
        {
            if (_ending >= 0 || !double.IsNaN(_rampStart) || _ramp > 0)
            {
                return true;
            }

            return _row != null && _row.Running && Progress.Fills && _frame.Fill < 1;
        }

        private void AdvanceRamp(double now)
        {
            if (double.IsNaN(_rampStart))
            {
                return;
            }

            var t = HudTimelines.Ms(now - _rampStart);
            if (HudTimelines.Reached(t, (long)MotionSpecs.PillGlowRampMs))
            {
                _ramp = _rampTarget;
                _rampStart = double.NaN;
                return;
            }

            _ramp = _rampFrom + (_rampTarget - _rampFrom) * StepBarTimeline.HaloRamp(t, true);
        }

        private void Compose(double now)
        {
            AdvanceRamp(now);
            var fill = Progress.Fills ? Progress.ValueAt(now) : 0;
            var reduced = ReduceMotion;
            var pulse = _pulse.PulseAt(now);
            var halo = _ramp > 0 ? StepBarTimeline.Halo(_ramp, pulse, reduced) : 0;
            long t = 0;
            if (_ending >= 0)
            {
                t = HudTimelines.Ms(now - _changeStart);
                if (HudTimelines.Reached(t, _changeEndMs))
                {
                    _ending = -1;
                }
            }

            if (_ending < 0)
            {
                _frame = new PillRowFrame(_current, fill, halo, -1, false, 0, default,
                    0, StepBarTimeline.ThumbSwap((long)MotionSpecs.PillThumbSwapMs, reduced), false);
                return;
            }

            var change = StepBarTimeline.PillChange(t, _endingMet, _changeReduced);
            var thumbMs = (long)MotionSpecs.PillThumbSwapMs;
            var thumb = StepBarTimeline.ThumbSwap(t < thumbMs ? t : thumbMs, _changeReduced);
            var endingHalo = _endingRamp > 0 ? StepBarTimeline.Halo(change.EndingHaloRamp * _endingRamp, pulse, _changeReduced) : 0;
            _frame = new PillRowFrame(_current, fill, halo, _ending, _endingMet, _endingFillFrom, in change, endingHalo, in thumb, t < thumbMs);
        }

        private void RepaintIfNeeded(bool force)
        {
            var fill = _frame.Fill;
            var halo = _frame.HaloAlpha + _frame.EndingHaloAlpha;
            var repaint = force || _frame.Changing || double.IsNaN(_lastRepaintFill) || Math.Abs(halo - _lastRepaintHalo) > 1e-4;
            if (!repaint)
            {
                var moved = Math.Abs(fill - _lastRepaintFill);
                repaint = FillWidthPx > 0 ? moved * FillWidthPx >= MotionSpecs.PillRepaintPx : moved > 0;
            }

            if (!repaint)
            {
                return;
            }

            _lastRepaintFill = fill;
            _lastRepaintHalo = halo;
            _repaint();
        }
    }
}
