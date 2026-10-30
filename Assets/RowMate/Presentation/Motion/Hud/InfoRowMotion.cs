using System;
using RowMate.Core.Design;

namespace RowMate.Presentation.Motion.Hud
{
    // La fila de información de la barra de pasos (04b-hud-exr/02-tech-design §2.15, §7.4; 04-design-notes §3.3; DN4b-16;
    // AC-WKT-14-15): el cambio de modo entre pasos y la pastilla que funde al entrar Próximo. El plan (InfoRowFit) lo decide la
    // vista al empezar el paso; esto solo mueve cómo se ve.
    // - ChangeMode: los dos aires y el relleno de Próximo pasan del plan viejo al nuevo en 0-240 (ease.move), con el cambio de
    //   paso (InfoRowTimeline.ModeChange). Un cambio que llega con otro en curso: el que corría salta a su final.
    // - HideTarget: la pastilla que no entra junto a Próximo funde a 0 en 120 (ease.exit, InfoRowTimeline.TargetOut) y
    //   targetGone avisa al terminar (sale del árbol con su aire). ShowTarget: un paso nuevo con su pastilla (cancela el fundido).
    // - Fases: F en el cambio de modo (0-240) y en la pastilla que se va (0-120); sale. Reducir movimiento: los aires en el
    //   acto; la pastilla, con su fundido de 120. Sin armar o con Instant, en el acto.
    public sealed class InfoRowMotion : HudMotion
    {
        private readonly Action<InfoRowVisual> _gaps;
        private readonly Action<double> _targetOpacity;
        private readonly Action _targetGone;
        private InfoRowPlan _from;
        private InfoRowPlan _to;
        private bool _modeRunning;
        private double _modeStart;
        private bool _modeReduced;
        private bool _targetRunning;
        private double _targetStart;
        private bool _targetReduced;

        public InfoRowMotion(MotionDriver driver, Action<InfoRowVisual> gaps, Action<double> targetOpacity, Action targetGone)
            : base(driver)
        {
            _gaps = gaps ?? throw new ArgumentNullException(nameof(gaps));
            _targetOpacity = targetOpacity ?? throw new ArgumentNullException(nameof(targetOpacity));
            _targetGone = targetGone ?? throw new ArgumentNullException(nameof(targetGone));
        }

        public override bool IsFinite => IsRunning && (_modeRunning || _targetRunning);

        // Los aires y el relleno que se ven (dp base).
        public InfoRowVisual Gaps { get; private set; }

        public double TargetOpacity { get; private set; } = 1;

        // La pastilla se fue (o se está yendo).
        public bool TargetHidden { get; private set; }

        public bool IsChangingMode => _modeRunning;

        public bool IsHidingTarget => _targetRunning;

        public void ChangeMode(in InfoRowPlan from, in InfoRowPlan to, bool instant)
        {
            if (_modeRunning)
            {
                // Lo que corría salta a su final.
                _modeRunning = false;
                ApplyGaps(InfoRowTimeline.ModeChange((long)MotionSpecs.InfoRowModeChangeMs, in _from, in _to, _modeReduced));
            }

            _from = from;
            _to = to;
            _modeReduced = ReduceMotion;
            var same = from.CountdownGap == to.CountdownGap && from.ClockGap == to.ClockGap && from.UpNextPadH == to.UpNextPadH;
            if (same || _modeReduced || !CanAnimate(instant))
            {
                ApplyGaps(InfoRowTimeline.ModeChange((long)MotionSpecs.InfoRowModeChangeMs, in from, in to, true));
                UpdateDriver();
                return;
            }

            _modeRunning = true;
            _modeStart = Now;
            ApplyGaps(InfoRowTimeline.ModeChange(0, in from, in to, false));
            UpdateDriver();
        }

        public void HideTarget(bool instant)
        {
            if (TargetHidden && !(instant && _targetRunning))
            {
                return;
            }

            TargetHidden = true;
            if (!CanAnimate(instant))
            {
                _targetRunning = false;
                ApplyTarget(0);
                UpdateDriver();
                _targetGone();
                return;
            }

            _targetRunning = true;
            _targetStart = Now;
            _targetReduced = ReduceMotion;
            ApplyTarget(InfoRowTimeline.TargetOut(0, _targetReduced));
            UpdateDriver();
        }

        // Un paso nuevo con su pastilla: deja de irse y vuelve a 1.
        public void ShowTarget()
        {
            _targetRunning = false;
            TargetHidden = false;
            ApplyTarget(1);
            UpdateDriver();
        }

        protected override bool Advance(double nowSeconds)
        {
            if (_modeRunning)
            {
                var end = (long)MotionSpecs.InfoRowModeChangeMs;
                var t = HudTimelines.Ms(nowSeconds - _modeStart);
                if (HudTimelines.Reached(t, end))
                {
                    _modeRunning = false;
                    t = end;
                }

                ApplyGaps(InfoRowTimeline.ModeChange(t, in _from, in _to, _modeReduced));
            }

            var gone = false;
            if (_targetRunning)
            {
                var end = (long)MotionSpecs.TargetPillOutMs;
                var t = HudTimelines.Ms(nowSeconds - _targetStart);
                if (HudTimelines.Reached(t, end))
                {
                    _targetRunning = false;
                    gone = true;
                    t = end;
                }

                ApplyTarget(InfoRowTimeline.TargetOut(t, _targetReduced));
            }

            var keep = _modeRunning || _targetRunning;
            if (gone)
            {
                _targetGone();
            }

            return keep;
        }

        protected override void OnStopped()
        {
            _modeRunning = false;
            _targetRunning = false;
        }

        private void UpdateDriver()
        {
            if (_modeRunning || _targetRunning)
            {
                Run();
            }
            else
            {
                Leave();
            }
        }

        private void ApplyGaps(in InfoRowVisual visual)
        {
            Gaps = visual;
            _gaps(visual);
        }

        private void ApplyTarget(double opacity)
        {
            TargetOpacity = opacity;
            _targetOpacity(opacity);
        }
    }
}
