using System;
using RowMate.Core.Design;
using RowMate.Presentation.Controls;
using UnityEngine;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Motion
{
    // Los pulsos de la fila del paso (AC-WKT-14-04, -05, -06; 02-tech-design §6.6, §6.11; 04-design-notes §5.4, DN4a-17): UN
    // solo reloj de pulso p(t) por barra, que sube de 0 a 1 en 600 ms y baja en 600 (ease.pulse), como el único
    // AnimationController(600)..repeat(reverse: true) del legacy (immersive_workout_screen.dart:319-322). Su origen es el
    // primer cuadro con "a continuación" o "por terminar" después de uno sin ninguno de los dos: con pasos de 15 s o menos
    // ("a continuación" de paso a paso) no se reinicia. El reloj es el del MotionDriver (cuadros reales): con el reloj de
    // prueba, la fase de una toma depende solo del instante (CAP-21, CAP-22 a los 87,6 s). Después de un alcance, el origen es
    // el primer cuadro de la vuelta (p = 0).
    // - Próximo: su fondo y su borde (.rm-hud-up-next__bg, DV-38) laten a 0,6 + 0,4·p; el texto, siempre entero. La entrada y
    //   la salida de la pastilla (ancho y fundido) las hace StepInfoBar.
    // - Por terminar: la cuenta escala 1 → 1,1 con ω 22 (origen en su borde derecho) y su color va de blanco al del paso en
    //   a·p, con la rampa a de 0 a 1 en 150 (ease.move) al empezar y de vuelta a 0 al terminar (el color no salta aunque el
    //   reloj esté a mitad de ciclo).
    // - Reducir movimiento: realce fijo (Próximo a 1; la cuenta a 1,1 en el acto y el color del paso, que llega en 150).
    // - Mientras solo late es continua (no cuenta para Settle, como el corazón); la rampa y la escala son finitas.
    // - Sin paso (la foto de reposo) se apaga con Release; cuando la fila se va del panel, Stop la saca del MotionDriver en el
    //   acto (QA-V4a-02).
    public sealed class StepPulse : IMotion
    {
        private readonly MotionDriver _driver;
        private readonly VisualElement _upNextBg;
        private readonly TabularNumber _countdown;
        private Color _rest;
        private Color _accent;
        private bool _active;
        private double _origin = double.NaN;
        private bool _upNext;
        private bool _endingSoon;
        private double _rampFrom;
        private double _rampStart = double.NaN;
        private double _ramp;
        private CriticalSpring _scale = new CriticalSpring(1);
        private double _scaleTarget = 1;
        private double _last;
        private bool _running;

        public StepPulse(MotionDriver driver, VisualElement upNextBg, TabularNumber countdown, Color restColor)
        {
            _driver = driver ?? throw new ArgumentNullException(nameof(driver));
            _upNextBg = upNextBg ?? throw new ArgumentNullException(nameof(upNextBg));
            _countdown = countdown ?? throw new ArgumentNullException(nameof(countdown));
            _rest = restColor;
            _accent = restColor;
            _countdown.style.transformOrigin = new TransformOrigin(new Length(100, LengthUnit.Percent), new Length(50, LengthUnit.Percent));
        }

        // Finita mientras la rampa, la escala o la entrada de Próximo se mueven; si solo late, continua.
        public bool IsFinite
        {
            get
            {
                if (!double.IsNaN(_rampStart))
                {
                    return true;
                }

                return !_scale.IsSettled(_scaleTarget, 1e-3);
            }
        }

        public double Origin => _origin;

        public bool IsPulsing => _active;

        public double Ramp => _ramp;

        public double Scale => _scale.Position;

        // p en un instante (0 sin pulso).
        public double PulseAt(double nowSeconds)
        {
            if (!_active || double.IsNaN(_origin))
            {
                return 0;
            }

            if (_driver.Settings.ReduceMotion)
            {
                return 1;
            }

            var half = MotionSpecs.StepPulseHalfMs / 1000.0;
            var t = nowSeconds - _origin;
            if (t < 0)
            {
                t = 0;
            }

            var cycle = t % (2 * half);
            var u = cycle <= half ? cycle / half : 2 - cycle / half;
            return Easing.Evaluate(Tokens.EasePulse, u);
        }

        // El estado de la fila: "a continuación", "por terminar" y el color del paso. instant: todo en su estado final.
        public void Update(bool upNext, bool endingSoon, Color accent, bool instant)
        {
            var now = _driver.Now;
            var reduced = _driver.Settings.ReduceMotion;
            var animate = !instant && _driver.Armed;
            // El resorte avanza hasta ahora con el objetivo que tenía antes de cambiarlo: un estado nuevo en el mismo cuadro que
            // el Tick (o varios seguidos) no le roba tiempo, y no queda a mitad de camino (el arnés lo veía finito, DEMO-12).
            var elapsed = now - _last;
            if (elapsed > 0 && _running)
            {
                _scale.Step(elapsed, _scaleTarget, MotionSpecs.EndingSoonOmega);
                if (_scale.IsSettled(_scaleTarget, 1e-3))
                {
                    _scale.SnapTo(_scaleTarget);
                }
            }

            _accent = accent;
            var active = upNext || endingSoon;
            if (active && (!_active || double.IsNaN(_origin)))
            {
                _origin = now;
            }

            _active = active;

            _upNext = upNext;

            if (endingSoon != _endingSoon)
            {
                _endingSoon = endingSoon;
                _rampFrom = _ramp;
                _rampStart = animate ? now : double.NaN;
                if (!animate)
                {
                    _ramp = endingSoon ? 1 : 0;
                }
            }

            _scaleTarget = endingSoon ? MotionSpecs.EndingSoonScale : 1;
            if (!animate || reduced)
            {
                _scale.SnapTo(_scaleTarget);
            }

            _last = now;
            Apply(now);
            if (!_running && (active || !IsSettledNow()))
            {
                _running = true;
                _driver.Add(this);
            }
        }

        // Sin paso (StepInfoBar.Apply(null), la foto de reposo; QA-V4a-02): como un estado sin "a continuación" ni "por
        // terminar", con el color del paso que tenía. Lo que quede de la rampa o de la escala termina solo (finito) y sale del
        // MotionDriver.
        public void Release(bool instant)
        {
            Update(false, false, _accent, instant);
        }

        // La fila se va del panel (HudView.Dispose; QA-V4a-02 = QA-V4a-ROB-02): sale del MotionDriver en el acto, sin rampa
        // ni resorte, y la cuenta queda en reposo (escala 1 y su color de reposo). Sin esto el pulso, continuo, quedaba para
        // siempre con el HUD de esa sesión y una demanda de 60 fps (legacy: _pulseCtrl.dispose(),
        // immersive_workout_screen.dart:326-328). Un Update posterior lo vuelve a arrancar.
        public void Stop()
        {
            _active = false;
            _upNext = false;
            _endingSoon = false;
            _origin = double.NaN;
            _rampStart = double.NaN;
            _ramp = 0;
            _scaleTarget = 1;
            _scale.SnapTo(1);
            _last = _driver.Now;
            _countdown.style.color = _rest;
            _countdown.style.scale = StyleKeyword.Null;
            _running = false;
            _driver.Remove(this);
        }

        public bool Tick(double nowSeconds)
        {
            var dt = nowSeconds - _last;
            _last = nowSeconds;
            if (dt > 0)
            {
                _scale.Step(dt, _scaleTarget, MotionSpecs.EndingSoonOmega);
                if (_scale.IsSettled(_scaleTarget, 1e-3))
                {
                    _scale.SnapTo(_scaleTarget);
                }
            }

            Apply(nowSeconds);
            if (!_active && IsSettledNow())
            {
                _running = false;
                return false;
            }

            return true;
        }

        private bool IsSettledNow()
        {
            return double.IsNaN(_rampStart) && _scale.IsSettled(_scaleTarget, 1e-3);
        }

        private void Apply(double now)
        {
            var reduced = _driver.Settings.ReduceMotion;
            var p = PulseAt(now);

            // Próximo: fondo y borde.
            if (_upNext)
            {
                var beat = reduced ? 1.0 : MotionSpecs.UpNextMinOpacity + (1 - MotionSpecs.UpNextMinOpacity) * p;
                _upNextBg.style.opacity = (float)beat;
            }

            // La rampa del color de la cuenta.
            if (!double.IsNaN(_rampStart))
            {
                var u = (now - _rampStart) * 1000.0 / MotionSpecs.HudColorMs;
                var target = _endingSoon ? 1.0 : 0.0;
                if (u >= 1)
                {
                    _rampStart = double.NaN;
                    _ramp = target;
                }
                else
                {
                    _ramp = _rampFrom + (target - _rampFrom) * Easing.Evaluate(Tokens.EaseMove, u < 0 ? 0 : u);
                }
            }

            var mix = reduced ? _ramp : _ramp * p;
            _countdown.style.color = TokenColors.Lerp(_rest, _accent, mix);
            var s = (float)_scale.Position;
            if (Mathf.Abs(s - 1f) < 1e-4f)
            {
                _countdown.style.scale = StyleKeyword.Null;
            }
            else
            {
                _countdown.style.scale = new UnityEngine.UIElements.Scale(new Vector3(s, s, 1f));
            }
        }
    }
}
