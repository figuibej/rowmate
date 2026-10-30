using System;
using RowMate.Core.Design;

namespace RowMate.Presentation.Motion.Hud
{
    // La salida del inmersivo con la última foto (04b-hud-exr/02-tech-design §3.3, §7.4; 04-design-notes §4.2; AC-WKT-13-08,
    // -09; O-01): el HUD se va junto en 0-200 (ease.exit, multiplica la opacidad de cada pieza desde la que tenga), el fondo
    // funde a negro en 200-400 (ease.exit) y la página negra se va en 400-640 (ease.enter): vuelve Workout. Los valores son los
    // de ImmersiveTimeline.Exit, desde ExitStart (la entrada puede estar a medias, AC-WKT-13-07).
    // - Volver a reposo es interno y no espera a la salida: la vista ya dejó de aplicar estados (HudView.Freeze).
    // - revealStarted avisa una vez a los 400 (RevealStartMs; con reducir movimiento, en el cuadro 0): empieza a verse Workout y
    //   entran los avisos del final (DN4b-24). done, a los 640 (con reducir movimiento, 120): la página sale del árbol.
    // - Fases: F de 0 a 640 y sale. Reducir movimiento: la página funde lineal en 120 con la misma última foto.
    // - Sin armar o con Instant: el final y los dos avisos en el acto. Finish: salta al final con los avisos que faltaban.
    //   Stop: sale sin avisar (la página se dispone).
    public sealed class ImmersiveExitMotion : HudMotion
    {
        private readonly Action<ExitVisual> _apply;
        private readonly Action _revealStarted;
        private readonly Action _done;
        private ExitStart _from = ExitStart.Full;
        private double _start;
        private bool _reduced;
        private bool _revealPending;

        public ImmersiveExitMotion(MotionDriver driver, Action<ExitVisual> apply, Action revealStarted, Action done)
            : base(driver)
        {
            _apply = apply ?? throw new ArgumentNullException(nameof(apply));
            _revealStarted = revealStarted ?? throw new ArgumentNullException(nameof(revealStarted));
            _done = done ?? throw new ArgumentNullException(nameof(done));
        }

        public override bool IsFinite => IsRunning;

        // Lo último que se aplicó.
        public ExitVisual Current { get; private set; } = new ExitVisual(1, 1, 1);

        // Ya avisó que empieza a verse Workout.
        public bool RevealStarted { get; private set; }

        public long ElapsedMs { get; private set; }

        public void Play(in ExitStart from, bool instant = false)
        {
            _from = from;
            _reduced = ReduceMotion;
            _start = Now;
            RevealStarted = false;
            _revealPending = false;
            if (!CanAnimate(instant))
            {
                Leave();
                Evaluate(ImmersiveTimeline.ExitEndMs(_reduced));
                Notify(true);
                return;
            }

            Run();
            Evaluate(0);
            Notify(false);
        }

        public void Finish()
        {
            if (!IsRunning)
            {
                return;
            }

            Leave();
            Evaluate(ImmersiveTimeline.ExitEndMs(_reduced));
            Notify(true);
        }

        protected override bool Advance(double nowSeconds)
        {
            var end = ImmersiveTimeline.ExitEndMs(_reduced);
            var t = HudTimelines.Ms(nowSeconds - _start);
            var ended = HudTimelines.Reached(t, end);
            Evaluate(ended ? end : t);
            Notify(ended);
            return !ended;
        }

        private void Evaluate(long tMs)
        {
            ElapsedMs = tMs;
            var visual = ImmersiveTimeline.Exit(tMs, in _from, _reduced);
            Current = visual;
            _apply(visual);
            if (!RevealStarted && HudTimelines.Reached(tMs, ImmersiveTimeline.RevealStartMs(_reduced)))
            {
                RevealStarted = true;
                _revealPending = true;
            }
        }

        private void Notify(bool ended)
        {
            if (_revealPending)
            {
                _revealPending = false;
                _revealStarted();
            }

            if (ended)
            {
                _done();
            }
        }
    }
}
