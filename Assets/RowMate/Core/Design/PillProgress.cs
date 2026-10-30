namespace RowMate.Core.Design
{
    // El relleno continuo de la píldora en curso y el aro de la cuenta grande (02-tech-design §2.3 de la It. 4b; ANIMATION_BIBLE
    // §2.6, §7.1; 04b/00-deuda-diseno §4.5; DD4b-09; QA-G4bT-04). Mejora EXR: el legacy llena por segundo entero
    // (StepProgress.progress, workout_provider.dart:31-36). Lo alimenta la vista con cada estado del HUD y lo lee una vez por
    // cuadro, con el reloj del MotionDriver (el despierto del programador, el mismo del motor). Pura y sin asignaciones.
    //   objetivo = (transcurrido + clamp(ahora − tick; 0; 1)) / D;
    //   relleno = max(anterior; min(objetivo; (transcurrido + 1) / D)), en [0; 1].
    // Nunca pasa del valor del tick siguiente (la animación nunca adelanta el reloj, ANIMATION_BIBLE §0.1; AC-WKT-13-14) y,
    // dentro de un paso, nunca baja: en pausa queda quieto y al reanudar espera a que el motor lo alcance (devuelve la fracción
    // que Q-58 descarta). En el cambio de paso vale transcurrido / D en el acto. Con Instant en el MISMO paso vale
    // max(anterior; transcurrido / D) y el tick queda donde estaba si no llegó un segundo nuevo (con el reloj despierto,
    // ahora − tick sigue continuo después de despertar). Con cada segundo nuevo, también max(anterior; transcurrido / D): el
    // presente del motor, así el último segundo deja la píldora llena aunque llegue un cuadro antes (QA-V4b-P02).
    public sealed class PillProgress
    {
        private bool _hasState;
        private int _elapsed;
        private int _duration;
        private bool _running;
        private double _tick;
        private double _value;
        private double _lastNow;

        // "Por llenar": paso por tiempo con duración > 0 (Q-51).
        public bool Fills { get; private set; }

        public int StepIndex { get; private set; } = -1;

        // Un estado del HUD: el paso, los segundos que contó el motor en él, su duración (null o ≤ 0: no se llena), si el reloj
        // corre (fase en marcha), el instante de ahora y si el estado es Instant (segundo plano, despertar, cuadro tarde).
        // "tick" = el instante en que llegó el último segundo, o el comienzo del tramo al reanudar (Q-58). Primero avanza el
        // relleno hasta `now` con el estado anterior (como StepPulse.Update): un estado nuevo nunca le roba el tramo del cuadro,
        // sea cual sea el orden en que la vista llama a OnState y a ValueAt.
        public void OnState(int stepIndex, int elapsedSeconds, int? durationSeconds, bool running, double now, bool instant)
        {
            if (_hasState && stepIndex == StepIndex)
            {
                Advance(now);
            }

            var duration = durationSeconds.HasValue && durationSeconds.Value > 0 ? durationSeconds.Value : 0;
            var fills = duration > 0;
            if (!_hasState || stepIndex != StepIndex || duration != _duration)
            {
                // El cambio de paso (o el primer estado): en el acto.
                _hasState = true;
                StepIndex = stepIndex;
                _duration = duration;
                Fills = fills;
                _elapsed = elapsedSeconds;
                _running = running;
                _tick = now;
                _value = fills ? Clamp((double)elapsedSeconds / duration) : 0;
                _lastNow = now;
                return;
            }

            var newSecond = elapsedSeconds != _elapsed;
            var resumed = running && !_running;
            if (newSecond || resumed)
            {
                _tick = now;
            }

            _elapsed = elapsedSeconds;
            _running = running;
            if ((instant || newSecond) && fills)
            {
                // Con Instant, o con un segundo nuevo, al menos el presente del motor (transcurrido / D): nunca adelanta el reloj y
                // nunca baja. QA-V4b-P02 (Q-52): el motor entrega cada segundo en el primer cuadro con hora ≥ su vencimiento, así
                // que dos segundos pueden llegar a menos de 1 s (un cuadro menos); la foto final trae el reloj quieto y sin esto la
                // última píldora quedaba al 98,4-99,9 %.
                var present = Clamp((double)elapsedSeconds / duration);
                if (present > _value)
                {
                    _value = present;
                }
            }

            if (now > _lastNow)
            {
                _lastNow = now;
            }
        }

        // El relleno en [0; 1] en `now`. Avanza el estado: se llama una vez por cuadro con un `now` que no baja.
        public double ValueAt(double now)
        {
            Advance(now);
            return _value;
        }

        // Lo que falta del paso, en segundos visuales: D·(1 − relleno). El aro de la cuenta grande es RemainingAt / 3 (§3.5).
        public double RemainingAt(double now)
        {
            var value = ValueAt(now);
            return Fills ? _duration * (1 - value) : 0;
        }

        private void Advance(double now)
        {
            if (!_hasState || !Fills)
            {
                return;
            }

            if (now > _lastNow)
            {
                _lastNow = now;
            }

            if (!_running)
            {
                return;
            }

            var since = now - _tick;
            since = since < 0 ? 0 : since > 1 ? 1 : since;
            var target = (_elapsed + since) / _duration;
            var cap = (_elapsed + 1.0) / _duration;
            if (target > cap)
            {
                target = cap;
            }

            target = Clamp(target);
            if (target > _value)
            {
                _value = target;
            }
        }

        private static double Clamp(double value)
        {
            return value < 0 ? 0 : value > 1 ? 1 : value;
        }
    }
}
