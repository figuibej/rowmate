using RowMate.Core.Design;
using UnityEngine;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Motion
{
    // Corazón de la tarjeta del pulsómetro (AC-HRM-13-02, -12; 04-design-notes §5.3, DN-10, DN-13).
    // - Opacidad: 1 con el pulsómetro conectado (con o sin lectura) y 0,40 en cualquier otro estado, SOLO por código, en línea,
    //   escrita siempre: en el acto al armar la tarjeta y con un fundido de dur.fast (160) ease.move en cada cambio.
    // - Latido: solo conectado y con bpm > 0, con HeartbeatClock (dos golpes, compresión sobre 142,9 bpm, parada en 120 ms).
    //   Escala desde el centro de la caja del glifo. Reducir movimiento: quieto (la opacidad sí funde).
    // Mientras late es una animación continua (no cuenta para Settle); el fundido y la parada son finitos.
    public sealed class HeartbeatAnimator : IMotion
    {
        private readonly MotionDriver _driver;
        private readonly VisualElement _heart;
        private readonly HeartbeatClock _clock = new HeartbeatClock();
        private bool _full;
        private bool _initialized;
        private double _fadeStart = double.NegativeInfinity;
        private float _fadeFrom = 1;
        private float _fadeTo = 1;
        private bool _beatingRequested;

        public HeartbeatAnimator(MotionDriver driver, VisualElement heart)
        {
            _driver = driver;
            _heart = heart;
            _heart.style.transformOrigin = new TransformOrigin(new Length(50, LengthUnit.Percent), new Length(50, LengthUnit.Percent));
        }

        public bool IsFinite => !_clock.IsBeating;

        public HeartbeatClock Clock => _clock;

        public float TargetOpacity => _fadeTo;

        public bool IsAtRest(double nowSeconds)
        {
            return _driver.Settings.ReduceMotion || _clock.IsAtRest(nowSeconds);
        }

        // full: conectado (pleno); bpm: el último pulso (late si beating).
        public void SetState(bool full, bool beating, int bpm)
        {
            var now = _driver.Now;
            var target = full ? 1f : (float)MotionSpecs.DimHeartOpacity;
            if (!_initialized || !_driver.Armed)
            {
                _initialized = true;
                _full = full;
                _fadeFrom = target;
                _fadeTo = target;
                _fadeStart = double.NegativeInfinity;
                _heart.style.opacity = target;
            }
            else if (full != _full)
            {
                _full = full;
                _fadeFrom = _heart.resolvedStyle.opacity;
                _fadeTo = target;
                _fadeStart = now;
            }

            _beatingRequested = beating && bpm > 0;
            _clock.SetBpm(_beatingRequested ? bpm : 0, now);
            if (NeedsTicks(now))
            {
                _driver.Add(this);
            }
            else
            {
                ApplyScale(1);
            }
        }

        public bool Tick(double nowSeconds)
        {
            var fadeMs = (nowSeconds - _fadeStart) * 1000.0;
            if (fadeMs >= 0 && fadeMs < Tokens.DurFast.Milliseconds)
            {
                var e = Easing.Evaluate(Tokens.EaseMove, fadeMs / Tokens.DurFast.Milliseconds);
                _heart.style.opacity = _fadeFrom + (_fadeTo - _fadeFrom) * (float)e;
            }
            else
            {
                _heart.style.opacity = _fadeTo;
            }

            ApplyScale(_driver.Settings.ReduceMotion ? 1 : _clock.ScaleAt(nowSeconds));
            var keep = NeedsTicks(nowSeconds);
            if (!keep)
            {
                ApplyScale(1);
            }

            return keep;
        }

        public void Stop()
        {
            _driver.Remove(this);
            _clock.SetBpm(0, _driver.Now);
            ApplyScale(1);
            _heart.style.opacity = _fadeTo;
        }

        private bool NeedsTicks(double now)
        {
            var fading = (now - _fadeStart) * 1000.0 < Tokens.DurFast.Milliseconds;
            return fading || _clock.IsBeating || !_clock.IsAtRest(now);
        }

        private void ApplyScale(double scale)
        {
            var s = (float)scale;
            _heart.style.scale = new Scale(new Vector2(s, s));
        }
    }
}
