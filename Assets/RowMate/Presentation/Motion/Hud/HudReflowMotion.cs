using System;
using RowMate.Core.Design;

namespace RowMate.Presentation.Motion.Hud
{
    // El HUD al cambiar de clase de ventana (06a-escena-lago/02-tech-design §3.9, §7.4; DT6a-06; 04-design-notes §11.7; AC-WKT-17-07;
    // WP-D1): cuando la ventana pasa de vertical a horizontal (o al revés), las piezas del HUD quedan en el layout nuevo con opacidad 0 y
    // entran en 200 ms (ease.enter, SceneTimelines.HudReflow), sin foto del layout anterior. Si llega otro cambio de clase en el medio,
    // arranca desde la opacidad que tiene (sin saltos). Un cambio de tamaño dentro de la misma clase no llama acá: se reacomoda en el
    // acto, como hasta la 5. La escena se adapta en el acto (no pasa por acá).
    // - Reducir movimiento, sin armar o con Instant: en el acto (1).
    // - It. 14 (T-07, AC-APP-15-10; 00-arte-pulido §3.3 punto 4, la regla de los 400 ms): si llega otro cambio de clase antes de
    //   RepeatWindowMs desde el anterior (arrastrar el borde de la ventana de un lado al otro del cuadrado), no se repite la entrada:
    //   queda en el acto (1) hasta que los cambios se espacien. Esta es la LayoutClassEntrance de 02-tech-design §3 (ya existía
    //   desde la 6a; WP-D le suma la regla).
    // - IsFinite mientras corre. Stop: sale del MotionDriver sin avisar y deja lo que se ve donde estaba (ROB-02).
    // - Sin asignaciones por cuadro.
    public sealed class HudReflowMotion : HudMotion
    {
        private readonly Action<double> _apply;
        // La regla de los 400 ms (T-07).
        public const double RepeatWindowMs = 400;

        private double _from;
        private double _start;
        private double _lastChange = double.NegativeInfinity;

        public HudReflowMotion(MotionDriver driver, Action<double> apply)
            : base(driver)
        {
            _apply = apply ?? throw new ArgumentNullException(nameof(apply));
            Opacity = 1;
        }

        public override bool IsFinite => IsRunning;

        // La opacidad común de las piezas del HUD (0 a 1).
        public double Opacity { get; private set; }

        public void OnOrientationClassChanged(bool instant = false)
        {
            var now = Now;
            var repeated = (now - _lastChange) * 1000.0 < RepeatWindowMs;
            _lastChange = now;
            if (!CanAnimate(instant) || ReduceMotion || repeated)
            {
                Leave();
                Apply(1);
                return;
            }

            _from = IsRunning ? Opacity : 0;
            _start = Now;
            Apply(_from);
            Run();
        }

        protected override bool Advance(double nowSeconds)
        {
            var t = HudTimelines.Ms(nowSeconds - _start);
            if (HudTimelines.Reached(t, MotionSpecs.HudReflowMs))
            {
                Apply(1);
                return false;
            }

            Apply(_from + (1 - _from) * SceneTimelines.HudReflow(t, false));
            return true;
        }

        private void Apply(double opacity)
        {
            Opacity = opacity;
            _apply(opacity);
        }
    }
}
