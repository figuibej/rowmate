using System;
using RowMate.Core.Design;

namespace RowMate.Presentation.Motion.Hud
{
    // La entrada "sumergirse" del HUD (04b-hud-exr/02-tech-design §3.2, §7.4; 04-design-notes §4.1; AC-WKT-13-06, -07; P-15):
    // el fondo funde sobre el negro en 220-400 y las seis zonas (y las piezas del centro, con el tramo de SPM) entran en cascada
    // desde los 400, cada una en 280 (ease.enter) de opacidad 0 a 1 y de +8 a 0 dp base de translate (nunca layout). La página
    // negra la hace PageTransition (WP-B2): acá solo el fondo y las piezas. Los valores son los de ImmersiveTimeline.Entry*.
    // - La sesión ya corre (AC-WKT-13-01): las piezas muestran el valor del momento; la banda entra con el reloj real.
    // - interactive(pieza) avisa una vez cuando empieza el tramo de cada pieza (los controles responden desde los 840, al toque
    //   y al teclado: HudControls.SetInteractive, QA-G4bT-09 (a)); done, a los 1120, cuando todo quedó quieto (la llave entra).
    // - Fases: F de 0 a 1120 y sale. Reducir movimiento: todo a 1 desde el cuadro 0 (los controles responden ahí), y el fundido
    //   lineal de la página dura 120: done a los 120.
    // - Sin armar o con Instant: todo en su lugar y los avisos en el acto.
    // - Stop: la cascada se detiene donde está (si la sesión vuelve a reposo durante la entrada, la salida arranca desde las
    //   opacidades de PieceOpacity y BackdropOpacity, sin saltos: AC-WKT-13-07). Finish: salta al final con sus avisos.
    public sealed class ImmersiveCascadeMotion : HudMotion
    {
        private static readonly HudPiece[] Pieces =
        {
            HudPiece.TopBar, HudPiece.Spm, HudPiece.SplitWatts, HudPiece.Band, HudPiece.Vitals, HudPiece.Controls, HudPiece.Center,
        };

        private readonly Action<HudPiece, PieceVisual> _piece;
        private readonly Action<double> _backdrop;
        private readonly Action<HudPiece> _interactive;
        private readonly Action _done;
        private readonly double[] _opacity = new double[Pieces.Length];
        private readonly double[] _shift = new double[Pieces.Length];
        private readonly bool[] _responding = new bool[Pieces.Length];
        private readonly bool[] _pending = new bool[Pieces.Length];
        private double _backdropOpacity;
        private double _start;
        private bool _reduced;
        private bool _applied;

        public ImmersiveCascadeMotion(MotionDriver driver, Action<HudPiece, PieceVisual> piece, Action<double> backdrop,
                                      Action<HudPiece> interactive, Action done)
            : base(driver)
        {
            _piece = piece ?? throw new ArgumentNullException(nameof(piece));
            _backdrop = backdrop ?? throw new ArgumentNullException(nameof(backdrop));
            _interactive = interactive ?? throw new ArgumentNullException(nameof(interactive));
            _done = done ?? throw new ArgumentNullException(nameof(done));
            for (var i = 0; i < Pieces.Length; i++)
            {
                _shift[i] = MotionSpecs.ImmersivePieceShiftDp;
            }
        }

        public override bool IsFinite => IsRunning;

        // La opacidad que tiene cada pieza (de dónde arranca la salida si la entrada se corta).
        public double PieceOpacity(HudPiece piece)
        {
            return _opacity[(int)piece];
        }

        // La subida que tiene cada pieza, en dp base (× hudScale en la vista).
        public double PieceShiftDp(HudPiece piece)
        {
            return _shift[(int)piece];
        }

        public double BackdropOpacity => _backdropOpacity;

        // Si el tramo de la pieza ya empezó (responde: P-15).
        public bool IsInteractive(HudPiece piece)
        {
            return _responding[(int)piece];
        }

        // Los ms desde el toque del último cuadro aplicado.
        public long ElapsedMs { get; private set; }

        public void Play(bool instant = false)
        {
            _reduced = ReduceMotion;
            _start = Now;
            _applied = false;
            for (var i = 0; i < Pieces.Length; i++)
            {
                _responding[i] = false;
            }

            if (!CanAnimate(instant))
            {
                Leave();
                Evaluate(ImmersiveTimeline.EntryEndMs(_reduced));
                Notify(true);
                return;
            }

            Run();
            Evaluate(0);
            Notify(false);
        }

        // Salta al final: todo en su lugar, los avisos que faltaban y done.
        public void Finish()
        {
            if (!IsRunning)
            {
                return;
            }

            Leave();
            Evaluate(ImmersiveTimeline.EntryEndMs(_reduced));
            Notify(true);
        }

        protected override bool Advance(double nowSeconds)
        {
            var end = ImmersiveTimeline.EntryEndMs(_reduced);
            var t = HudTimelines.Ms(nowSeconds - _start);
            var ended = HudTimelines.Reached(t, end);
            Evaluate(ended ? end : t);
            Notify(ended);
            return !ended;
        }

        private void Evaluate(long tMs)
        {
            ElapsedMs = tMs;
            for (var i = 0; i < Pieces.Length; i++)
            {
                var piece = Pieces[i];
                var visual = ImmersiveTimeline.Entry(piece, tMs, _reduced);
                if (!_applied || visual.Opacity != _opacity[i] || visual.ShiftDp != _shift[i])
                {
                    _opacity[i] = visual.Opacity;
                    _shift[i] = visual.ShiftDp;
                    _piece(piece, visual);
                }

                if (!_responding[i] && ImmersiveTimeline.EntryInteractive(piece, tMs, _reduced))
                {
                    _responding[i] = true;
                    _pending[i] = true;
                }
            }

            var backdrop = ImmersiveTimeline.EntryBackdrop(tMs, _reduced);
            if (!_applied || backdrop != _backdropOpacity)
            {
                _backdropOpacity = backdrop;
                _backdrop(backdrop);
            }

            _applied = true;
        }

        // Los avisos van después de aplicar el cuadro: los controles que empiezan a responder y, al final, done.
        private void Notify(bool ended)
        {
            for (var i = 0; i < Pieces.Length; i++)
            {
                if (_pending[i])
                {
                    _pending[i] = false;
                    _interactive(Pieces[i]);
                }
            }

            if (ended)
            {
                _done();
            }
        }
    }
}
