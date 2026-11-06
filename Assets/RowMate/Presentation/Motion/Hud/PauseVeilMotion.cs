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
    // It. 6a (06a-escena-lago/02-tech-design §3.3, §7.4; DT6a-10; AC-WKT-13-12; WP-D1): el mismo alfa va también a la escena como un
    // peso de 0 a 1 (alfa/0,15, SceneTimelines.VeilWeight) en el segundo callback, que HudView pasa a la escena (IImmersiveScene.
    // SetPauseVeil): Core lo compone con la luz de la hora en el único Volume (saturación × (1 − 0,3·w) y exposición + w·VeilEv). Los
    // dos callbacks se llaman en el mismo cuadro, con el mismo valor: el fondo provisorio y la escena nunca se separan.
    public sealed class PauseVeilMotion : HudMotion
    {
        private readonly Action<double> _veilAlpha;
        private readonly Action<double>? _sceneWeight;
        private bool _initialized;
        private bool _paused;
        private double _from;
        private double _start;
        private bool _reduced;

        public PauseVeilMotion(MotionDriver driver, Action<double> veilAlpha, Action<double>? sceneWeight = null)
            : base(driver)
        {
            _veilAlpha = veilAlpha ?? throw new ArgumentNullException(nameof(veilAlpha));
            _sceneWeight = sceneWeight;
        }

        public override bool IsFinite => IsRunning;

        // Lo que oscurece el velo ahora (0 a 0,15: el tinte del fondo es 1 − Alpha).
        public double Alpha { get; private set; }

        // El peso del velo para la escena (0 a 1): Alpha/0,15.
        public double SceneWeight => SceneTimelines.VeilWeight(Alpha);

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
            _sceneWeight?.Invoke(SceneTimelines.VeilWeight(alpha));
        }
    }
}
