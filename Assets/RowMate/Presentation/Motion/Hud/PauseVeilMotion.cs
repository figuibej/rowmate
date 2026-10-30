using System;
using RowMate.Core.Design;

namespace RowMate.Presentation.Motion.Hud
{
    // El velo de pausa (04b-hud-exr/02-tech-design §3.7, §7.4; 04-design-notes §4.8; AC-WKT-13-12): al pausar, el fondo se
    // oscurece un 15 % en 300 (ease.move); al reanudar vuelve en 240, siempre desde el valor que tenga (PauseTimeline.Veil). La
    // vista (HudView, WP-B1) lo aplica como el tinte de la textura del fondo (unityBackgroundImageTintColor de 1 a 0,85), sin
    // elementos nuevos; las tarjetas, la barra y el chip no bajan de opacidad. PAUSA va en su disco (CenterDiscMotion).
    // - Pausa y Reanudar actúan en el acto (AC-WKT-20-02): esto es solo el aspecto del fondo.
    // - Fases: F en 300 al pausar y 240 al reanudar; sale. Reducir movimiento: el mismo fundido. El primer estado, sin armar o
    //   con Instant: en el acto.
    public sealed class PauseVeilMotion : HudMotion
    {
        private readonly Action<double> _veilAlpha;
        private bool _initialized;
        private bool _paused;
        private double _from;
        private double _start;
        private bool _reduced;

        public PauseVeilMotion(MotionDriver driver, Action<double> veilAlpha)
            : base(driver)
        {
            _veilAlpha = veilAlpha ?? throw new ArgumentNullException(nameof(veilAlpha));
        }

        public override bool IsFinite => IsRunning;

        // Lo que oscurece el velo ahora (0 a 0,15: el tinte del fondo es 1 − Alpha).
        public double Alpha { get; private set; }

        public bool IsPaused => _paused;

        public void SetPaused(bool paused, bool instant)
        {
            if (_initialized && paused == _paused && !(instant && IsRunning))
            {
                return;
            }

            var animate = _initialized && CanAnimate(instant);
            _initialized = true;
            _paused = paused;
            if (!animate)
            {
                Leave();
                Apply(paused ? MotionSpecs.PauseVeilAlpha : 0);
                return;
            }

            _from = Alpha;
            _start = Now;
            _reduced = ReduceMotion;
            Apply(PauseTimeline.Veil(0, paused, _from, _reduced));
            Run();
        }

        protected override bool Advance(double nowSeconds)
        {
            var end = PauseTimeline.EndMs(_paused);
            var t = HudTimelines.Ms(nowSeconds - _start);
            if (HudTimelines.Reached(t, end))
            {
                Apply(_paused ? MotionSpecs.PauseVeilAlpha : 0);
                return false;
            }

            Apply(PauseTimeline.Veil(t, _paused, _from, _reduced));
            return true;
        }

        private void Apply(double alpha)
        {
            Alpha = alpha;
            _veilAlpha(alpha);
        }
    }
}
