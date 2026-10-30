using System;
using RowMate.Core.Design;

namespace RowMate.Presentation.Motion.Hud
{
    // El punto en vivo del chip EN MARCHA (04b-hud-exr/02-tech-design §3.7, §7.4; 04-design-notes §4.9; DN4b-26;
    // AC-WKT-16-05): late entre 0,35 y 1 (q de 1 a 0 en 800 y de vuelta en 800, ease.pulse; opacidad 0,35 + 0,65·q) y empieza en
    // 1, bajando, cada vez que el chip pasa a EN MARCHA (SetActive(true) desde inactivo: t = 0 ahí). Va con la etiqueta: en
    // PAUSADO y FINALIZADO no hay punto (SetActive(false)). Los valores son los de LiveDot.At.
    // - Lo que corre late: sigue el reloj del MotionDriver (en las tomas, la fase depende solo del instante: 0,444 a los 42,2 s
    //   con el origen en el instante 0, CAP-13).
    // - Fases: C mientras está activo (no bloquea una toma); sale al desactivarse. Reducir movimiento: quieto en 1, fuera del
    //   MotionDriver.
    // - QA-V4b-MOT-03: su "fase" dura toda la sesión en marcha, así que reducir movimiento se lee en CADA cuadro, como el corazón,
    //   el pulso de Próximo y el halo: si se prende a mitad de sesión, queda en 1 y sale del MotionDriver en el cuadro siguiente; si
    //   se apaga, vuelve a latir (desde 1) con el SetActive(true) del estado siguiente, que la vista manda en cada estado.
    public sealed class LiveDotMotion : HudMotion
    {
        private readonly Action<double> _opacity;
        private double _origin;
        private bool _active;

        public LiveDotMotion(MotionDriver driver, Action<double> opacity)
            : base(driver)
        {
            _opacity = opacity ?? throw new ArgumentNullException(nameof(opacity));
        }

        public override bool IsFinite => false;

        public bool IsActive => _active;

        public double Opacity { get; private set; } = 1;

        public void SetActive(bool active)
        {
            if (active == _active)
            {
                if (active && !IsRunning && !ReduceMotion)
                {
                    // Sigue EN MARCHA y se apagó reducir movimiento: vuelve a latir desde 1.
                    Start();
                }

                return;
            }

            _active = active;
            if (!active)
            {
                Leave();
                return;
            }

            Start();
        }

        protected override bool Advance(double nowSeconds)
        {
            if (!_active)
            {
                return false;
            }

            if (ReduceMotion)
            {
                // Se prendió a mitad de sesión: quieto en 1, fuera del MotionDriver.
                if (Opacity != 1)
                {
                    Apply(1);
                }

                return false;
            }

            var opacity = LiveDot.At(HudTimelines.Ms(nowSeconds - _origin), false);
            if (opacity != Opacity)
            {
                Apply(opacity);
            }

            return true;
        }

        protected override void OnStopped()
        {
            _active = false;
        }

        private void Start()
        {
            _origin = Now;
            Apply(1);
            if (ReduceMotion)
            {
                Leave();
                return;
            }

            Run();
        }

        private void Apply(double opacity)
        {
            Opacity = opacity;
            _opacity(opacity);
        }
    }
}
