using System;
using RowMate.Core.Design;

namespace RowMate.Presentation.Motion.Hud
{
    // La escena demorada (06a-escena-lago/02-tech-design §3.9, §7.4; 04-design-notes §11.6; AC-SCN-01-07; WP-D1): cuando la escena
    // queda lista después de la entrada, scene-surface entra con su opacidad de 0 a 1 en 0,6 s (ease.enter, SceneTimelines.LateScene)
    // sobre el fondo provisorio, en la pose del contrato y ya al día (eso lo hace la escena: acá es solo cómo se ve). Al terminar avisa
    // una vez (la página pasa el provisorio a display: none).
    // - Reducir movimiento: el mismo fundido (es un fundido, no movimiento: 04-design-notes §13).
    // - Sin armar o con Instant: 1 en el acto, y avisa.
    // - IsFinite mientras corre (bloquea una toma y la espera Settle). Stop: sale del MotionDriver en el acto sin avisar (ROB-02: nada
    //   queda animando después de detenerse; la vista lo llama en su Dispose y en su DetachFromPanelEvent).
    // - Sin asignaciones por cuadro.
    public sealed class SceneFadeMotion : HudMotion
    {
        private readonly Action<double> _apply;
        private readonly Action? _finished;
        private double _start;

        public SceneFadeMotion(MotionDriver driver, Action<double> apply, Action? finished = null)
            : base(driver)
        {
            _apply = apply ?? throw new ArgumentNullException(nameof(apply));
            _finished = finished;
        }

        public override bool IsFinite => IsRunning;

        // La opacidad de scene-surface (0 a 1).
        public double Opacity { get; private set; }

        public void Play(bool instant = false)
        {
            if (!CanAnimate(instant))
            {
                Leave();
                Apply(1);
                _finished?.Invoke();
                return;
            }

            _start = Now;
            Apply(SceneTimelines.LateScene(0));
            Run();
        }

        protected override bool Advance(double nowSeconds)
        {
            var t = HudTimelines.Ms(nowSeconds - _start);
            if (HudTimelines.Reached(t, MotionSpecs.SceneLateFadeMs))
            {
                Apply(1);
                _finished?.Invoke();
                return false;
            }

            Apply(SceneTimelines.LateScene(t));
            return true;
        }

        private void Apply(double opacity)
        {
            Opacity = opacity;
            _apply(opacity);
        }
    }
}
