using System;
using RowMate.Core.Design;

namespace RowMate.Presentation.Motion.Hud
{
    // El HUD al cambiar de clase de ventana (06a-escena-lago/02-tech-design §3.9, §7.4; DT6a-06; 04-design-notes §11.7; AC-WKT-17-07;
    // WP-D1): cuando la ventana pasa de vertical a horizontal (o al revés), las piezas del HUD quedan en el layout nuevo con opacidad 0 y
    // entran sin foto del layout anterior. Un cambio de tamaño dentro de la misma clase no llama acá: se reacomoda en el acto, como hasta
    // la 5. La escena se adapta en el acto (no pasa por acá).
    // - It. 15 (#82, AC-D15-32, DT15-11; arte A14-05): en CASCADA de 3 grupos de piezas que ya existen (0 barra de pasos y barra superior,
    //   1 tarjetas, 2 banda, fila de abajo y controles), con retrasos de 0 / 40 / 80 ms; cada uno, 200 ms con ease.enter, opacidad 0 → 1 y
    //   traslado de 8 dp → 0 (SceneTimelines.HudReflowGroup / HudReflowShift; el dueño multiplica el traslado por la escala del HUD). Si
    //   llega otro cambio de clase en el medio, cada grupo arranca desde la opacidad que tiene (sin saltos).
    // - Reducir movimiento, sin armar o con Instant: en el acto (1, sin traslado).
    // - It. 14 (T-07, AC-APP-15-10; 00-arte-pulido §3.3 punto 4, la regla de los 400 ms): si llega otro cambio de clase antes de
    //   RepeatWindowMs desde el anterior, no se repite la entrada: queda en el acto hasta que los cambios se espacien.
    // - IsFinite mientras corre. Stop: sale del MotionDriver sin avisar y deja lo que se ve donde estaba (ROB-02).
    // - Sin asignaciones por cuadro.
    public sealed class HudReflowMotion : HudMotion
    {
        // La regla de los 400 ms (T-07).
        public const double RepeatWindowMs = 400;

        public const int Groups = MotionSpecs.HudReflowGroups;

        // apply(grupo, opacidad 0 a 1, traslado hacia abajo en dp base).
        private readonly Action<int, double, double> _apply;
        private readonly double[] _from = new double[Groups];
        private readonly double[] _opacity = new double[Groups];
        private readonly double[] _shift = new double[Groups];
        private double _start;
        private double _lastChange = double.NegativeInfinity;

        public HudReflowMotion(MotionDriver driver, Action<int, double, double> apply)
            : base(driver)
        {
            _apply = apply ?? throw new ArgumentNullException(nameof(apply));
            for (var g = 0; g < Groups; g++)
            {
                _opacity[g] = 1;
            }
        }

        public override bool IsFinite => IsRunning;

        // La opacidad del grupo más atrasado (0 a 1; 1 = todo a la vista).
        public double Opacity
        {
            get
            {
                var min = 1.0;
                for (var g = 0; g < Groups; g++)
                {
                    min = Math.Min(min, _opacity[g]);
                }

                return min;
            }
        }

        public double GroupOpacity(int group)
        {
            return _opacity[group];
        }

        // El traslado hacia abajo del grupo (dp base, sin la escala del HUD).
        public double GroupShift(int group)
        {
            return _shift[group];
        }

        public void OnOrientationClassChanged(bool instant = false)
        {
            var now = Now;
            var repeated = (now - _lastChange) * 1000.0 < RepeatWindowMs;
            _lastChange = now;
            if (!CanAnimate(instant) || ReduceMotion || repeated)
            {
                Leave();
                ApplyAll(1);
                return;
            }

            var running = IsRunning;
            for (var g = 0; g < Groups; g++)
            {
                _from[g] = running ? _opacity[g] : 0;
            }

            _start = Now;
            Advance(_start);
            Run();
        }

        protected override bool Advance(double nowSeconds)
        {
            var t = HudTimelines.Ms(nowSeconds - _start);
            var done = true;
            for (var g = 0; g < Groups; g++)
            {
                var p = SceneTimelines.HudReflowGroup(g, t, false);
                if (p < 1)
                {
                    done = false;
                }

                var opacity = _from[g] + (1 - _from[g]) * p;
                Apply(g, opacity, SceneTimelines.HudReflowShift(p, 1));
            }

            return !done;
        }

        private void ApplyAll(double opacity)
        {
            for (var g = 0; g < Groups; g++)
            {
                Apply(g, opacity, 0);
            }
        }

        private void Apply(int group, double opacity, double shift)
        {
            _opacity[group] = opacity;
            _shift[group] = shift;
            _apply(group, opacity, shift);
        }
    }
}
