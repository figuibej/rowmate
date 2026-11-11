using System;
using RowMate.Core.Design;

namespace RowMate.Presentation.Motion
{
    // La marca de un paso del Detalle (It. 15, #33, AC-D15-02, DT15-02; MotionSpecs.StepHighlightMs): el paso que entra va de t 0 a 1 y
    // el que sale de su t a 0, los dos en 160 ms con ease.enter; con reducir movimiento (o sin reloj), al instante. Cada cuadro llama a
    // apply(paso marcado, su t, paso que sale, su t), que reescribe el alfa de las franjas (ChartVisual.WithSteps) sin reteselar.
    public sealed class StepHighlightMotion : IMotion
    {
        private readonly MotionDriver _driver;
        private readonly Action<int, double, int, double> _apply;
        private double _start;
        private double _selectedFrom;
        private double _leavingFrom;

        public StepHighlightMotion(MotionDriver driver, Action<int, double, int, double> apply)
        {
            _driver = driver ?? throw new ArgumentNullException(nameof(driver));
            _apply = apply ?? throw new ArgumentNullException(nameof(apply));
        }

        public bool IsFinite => true;

        // El paso marcado (−1 ninguno) y cuánto de la marca tiene.
        public int Selected { get; private set; } = -1;

        public double SelectedT { get; private set; }

        public int Leaving { get; private set; } = -1;

        public double LeavingT { get; private set; }

        public bool IsRunning => _driver.Contains(this);

        // Marca otro paso (−1 desmarca). El que estaba marcado sale desde su t actual.
        public void Select(int step)
        {
            if (step == Selected)
            {
                return;
            }

            var previous = Selected;
            var previousT = SelectedT;
            // Si vuelve el que estaba saliendo, sigue desde su t (sin salto).
            _selectedFrom = step >= 0 && step == Leaving ? LeavingT : 0;
            Leaving = previous;
            _leavingFrom = previous >= 0 ? previousT : 0;
            Selected = step;
            _start = _driver.Now;
            if (!_driver.Armed || _driver.Settings.ReduceMotion)
            {
                Finish();
                return;
            }

            SelectedT = _selectedFrom;
            LeavingT = _leavingFrom;
            Apply();
            _driver.Add(this);
        }

        // Al final en el acto (las pruebas, el arnés y reducir movimiento).
        public void Finish()
        {
            _driver.Remove(this);
            SelectedT = Selected >= 0 ? 1 : 0;
            LeavingT = 0;
            Leaving = -1;
            Apply();
        }

        public bool Tick(double nowSeconds)
        {
            var tMs = (nowSeconds - _start) * 1000;
            var u = tMs <= 0 ? 0 : tMs >= MotionSpecs.StepHighlightMs ? 1 : tMs / MotionSpecs.StepHighlightMs;
            var e = Easing.Evaluate(Tokens.EaseEnter, u);
            SelectedT = Selected >= 0 ? _selectedFrom + (1 - _selectedFrom) * e : 0;
            LeavingT = Leaving >= 0 ? _leavingFrom * (1 - e) : 0;
            if (u >= 1)
            {
                Leaving = -1;
                LeavingT = 0;
                Apply();
                return false;
            }

            Apply();
            return true;
        }

        private void Apply()
        {
            _apply(Selected, SelectedT, Leaving, LeavingT);
        }
    }
}
