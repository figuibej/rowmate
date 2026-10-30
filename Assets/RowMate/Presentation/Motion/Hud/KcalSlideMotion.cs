using System;
using RowMate.Core.Design;

namespace RowMate.Presentation.Motion.Hud
{
    // kcal que se desliza cuando BPM aparece o se va (04b-hud-exr/02-tech-design §3.8, §7.4; 04a/04-design-notes §5.6;
    // AC-WKT-17-11): un FLIP. El layout de la columna cambia en el acto y kcal se dibuja con un translate desde su lugar viejo
    // hasta 0 (KcalShift de 1 a 0, KcalSlideTimeline.At); BPM funde. Reemplaza a HudView.BpmMotion de la 4a.
    // - Appear(fromShiftDp): BPM ya está en el árbol visible (lo pone la vista) y kcal arranca desde fromShiftDp (su lugar viejo
    //   menos el nuevo; en vertical, el alto de BPM más su separación: 71,7·s) en 0-240 (ease.move); BPM funde de 0 a 1 en 120-360
    //   (ease.enter).
    // - Disappear(fromShiftDp): BPM funde a 0 en 0-120 (ease.exit) en su lugar; bpmGone avisa a los 120 que salga del árbol
    //   visible (ahí cambia el layout) y kcal se mueve desde fromShiftDp en 120-360.
    // - ShiftDp es el translate de kcal en dp desde su lugar del layout actual (KcalShift × fromShiftDp desde que el layout
    //   cambió; 0 antes). En horizontal kcal está en el rincón: fromShiftDp = 0. Una distancia que no es finita (sin layout todavía)
    //   vale 0: el translate nunca es NaN (QA-V4b-07).
    // - Uno que llega con el otro en curso sigue desde la opacidad que BPM tenía (sin saltos); el bpmGone pendiente de un
    //   Disappear cortado por un Appear no se avisa (BPM nunca salió del árbol).
    // - Fases: F en 0-360; sale. Reducir movimiento: kcal en el acto, BPM funde (240 / 120). Sin armar o con Instant: el final en
    //   el acto (bpmGone incluido). Finish: salta al final con el aviso que faltaba; Stop: sale sin avisar.
    public sealed class KcalSlideMotion : HudMotion
    {
        private readonly Action<KcalVisual> _apply;
        private readonly Action _bpmGone;
        private bool _appearing;
        private bool _gonePending;
        private double _distance;
        private double _bpmFrom;
        private double _start;
        private bool _reduced;

        public KcalSlideMotion(MotionDriver driver, Action<KcalVisual> apply, Action bpmGone)
            : base(driver)
        {
            _apply = apply ?? throw new ArgumentNullException(nameof(apply));
            _bpmGone = bpmGone ?? throw new ArgumentNullException(nameof(bpmGone));
            Current = new KcalVisual(0, 1);
        }

        public override bool IsFinite => IsRunning;

        public KcalVisual Current { get; private set; }

        // El translate de kcal en dp (0 mientras el layout todavía no cambió).
        public double ShiftDp { get; private set; }

        // El layout de la columna ya es el nuevo (Appear: desde el cuadro 0; Disappear: desde bpmGone).
        public bool LayoutChanged { get; private set; } = true;

        public bool IsAppearing => _appearing;

        public void Appear(double fromShiftDp, bool instant)
        {
            var bpm = IsRunning && !_appearing ? Current.BpmOpacity : 0;
            Leave();
            _gonePending = false;
            _appearing = true;
            _distance = Finite(fromShiftDp);
            _bpmFrom = bpm;
            LayoutChanged = true;
            Start(instant);
        }

        public void Disappear(bool instant, double fromShiftDp = 0)
        {
            var bpm = IsRunning && _appearing ? Current.BpmOpacity : 1;
            Leave();
            if (_gonePending)
            {
                // Un Disappear que corría: su BPM ya se iba; el aviso sigue pendiente.
                bpm = Current.BpmOpacity;
            }

            _appearing = false;
            _gonePending = true;
            _distance = Finite(fromShiftDp);
            _bpmFrom = bpm;
            LayoutChanged = false;
            Start(instant);
        }

        public void Finish()
        {
            if (!IsRunning)
            {
                return;
            }

            Leave();
            Evaluate(KcalSlideTimeline.EndMs(_appearing, _reduced));
            NotifyGone();
        }

        protected override bool Advance(double nowSeconds)
        {
            var end = KcalSlideTimeline.EndMs(_appearing, _reduced);
            var t = HudTimelines.Ms(nowSeconds - _start);
            var ended = HudTimelines.Reached(t, end);
            Evaluate(ended ? end : t);
            NotifyGone();
            return !ended;
        }

        private void Start(bool instant)
        {
            _reduced = ReduceMotion;
            _start = Now;
            if (!CanAnimate(instant))
            {
                Evaluate(KcalSlideTimeline.EndMs(_appearing, _reduced));
                NotifyGone();
                return;
            }

            Run();
            Evaluate(0);
            NotifyGone();
        }

        private void Evaluate(long tMs)
        {
            var visual = KcalSlideTimeline.At(tMs, _appearing, _reduced);
            double bpm;
            if (_appearing)
            {
                bpm = _bpmFrom + (1 - _bpmFrom) * visual.BpmOpacity;
            }
            else
            {
                bpm = _bpmFrom * visual.BpmOpacity;
                if (_gonePending && HudTimelines.Reached(tMs, (long)MotionSpecs.BpmFadeOutMs))
                {
                    LayoutChanged = true;
                }
            }

            ShiftDp = LayoutChanged ? visual.KcalShift * _distance : 0;
            var applied = new KcalVisual(visual.KcalShift, bpm);
            Current = applied;
            _apply(applied);
        }

        // La distancia del FLIP: 0 si la vista todavía no tiene layout (el alto de kcal es NaN en el primer estado con BPM; QA-V4b-07).
        private static double Finite(double distance)
        {
            return double.IsNaN(distance) || double.IsInfinity(distance) ? 0 : distance;
        }

        // bpmGone, una vez, cuando el layout cambió (después de aplicar el cuadro).
        private void NotifyGone()
        {
            if (!_gonePending || !LayoutChanged)
            {
                return;
            }

            _gonePending = false;
            _bpmGone();
        }
    }
}
