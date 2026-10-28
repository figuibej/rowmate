using System;
using RowMate.Core.Design;

namespace RowMate.Presentation.Motion
{
    // Un valor que sigue a su objetivo con un resorte crítico (CriticalSpring, ANIMATION_BIBLE §1.2): la llave que se aparta
    // del + (ω 22), el aviso que sigue a un obstáculo (ω 22), el desplazamiento hasta lo nuevo (ω 12) o hasta el foco y el
    // error (ω 22) (03-rutinas/04-design-notes §5.8). Parte de la posición y la velocidad actuales si el objetivo cambia en
    // medio. Finita: cuenta para Settle y para la regla de toma hasta que llega. Reducir movimiento o sin armar: en el acto.
    public sealed class SpringMotion : IMotion
    {
        private readonly MotionDriver _driver;
        private readonly Action<double> _apply;
        private CriticalSpring _spring;
        private double _target;
        private double _omega;
        private double _last;
        private bool _running;
        private bool _hasValue;

        public SpringMotion(MotionDriver driver, Action<double> apply)
        {
            _driver = driver ?? throw new ArgumentNullException(nameof(driver));
            _apply = apply ?? throw new ArgumentNullException(nameof(apply));
        }

        public bool IsFinite => true;

        public bool IsRunning => _running;

        public double Value => _spring.Position;

        public double Target => _target;

        public bool HasValue => _hasValue;

        // Salta al valor (estado inicial, reducir movimiento).
        public void Snap(double value)
        {
            Stop();
            _spring.SnapTo(value);
            _target = value;
            _hasValue = true;
            _apply(value);
        }

        // Va hacia target con ω; sin valor previo, sin armar o con reducir movimiento, salta.
        public void MoveTo(double target, double omega)
        {
            if (!_hasValue || !_driver.Armed || _driver.Settings.ReduceMotion)
            {
                Snap(target);
                return;
            }

            if (Math.Abs(target - _target) < 1e-4 && (_running || _spring.IsSettled(target, 0.01)))
            {
                return;
            }

            _target = target;
            _omega = omega;
            if (!_running)
            {
                _running = true;
                _last = _driver.Now;
                _driver.Add(this);
            }
        }

        public void Stop()
        {
            if (!_running)
            {
                return;
            }

            _running = false;
            _driver.Remove(this);
        }

        public bool Tick(double nowSeconds)
        {
            if (!_running)
            {
                return false;
            }

            var dt = nowSeconds - _last;
            _last = nowSeconds;
            _spring.Step(dt, _target, _omega);
            if (_spring.IsSettled(_target, 0.05))
            {
                _spring.SnapTo(_target);
                _running = false;
                _apply(_target);
                return false;
            }

            _apply(_spring.Position);
            return true;
        }
    }
}
