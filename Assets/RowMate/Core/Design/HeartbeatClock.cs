using System;

namespace RowMate.Core.Design
{
    // Latido del corazón de la tarjeta del pulsómetro a 60/bpm (AC-HRM-13-12; ANIMATION_BIBLE §2.9; 04-design-notes §5.3,
    // DN-10). Lógica pura con el reloj que le pasan (HeartbeatAnimator de Presentation le da MotionDriver/IClock): mismos
    // latidos a 60 fps en la app que cuadro a cuadro en los tests y los videos.
    // - El primer latido arranca en el instante del primer bpm > 0 (el corazón "despierta" con el número).
    // - Cada latido toma el período del bpm vigente al empezar: un bpm nuevo vale desde el latido siguiente, sin saltos de fase.
    // - bpm 0 (desconexión, caída, "--"): sin latidos nuevos y la escala vuelve a 1 en 120 ms (ease.move) desde donde esté.
    // La escala es la suma de dos golpes (MotionSpecs): 1 + 0,16·p₁ + 0,06·p₂; si T < 420 ms (bpm > 142,9) los tiempos se
    // comprimen por T/420, así un latido nunca se encima con el siguiente. Sin asignaciones.
    public sealed class HeartbeatClock
    {
        private int _bpm;
        private bool _beating;
        private double _beatStart;
        private double _beatPeriod;
        private bool _stopping;
        private double _stopStart;
        private double _stopFrom;

        // bpm vigente (0 = quieto).
        public int Bpm => _bpm;

        // Latidos empezados desde que se creó (para contar latidos en un intervalo).
        public int BeatsStarted { get; private set; }

        public bool IsBeating => _beating;

        public void SetBpm(int bpm, double nowSeconds)
        {
            Advance(nowSeconds);
            if (bpm > 0)
            {
                _bpm = bpm;
                if (!_beating)
                {
                    _stopping = false;
                    _beating = true;
                    StartBeat(nowSeconds);
                }

                return;
            }

            if (_beating)
            {
                _stopFrom = Shape(nowSeconds - _beatStart, _beatPeriod);
                _stopStart = nowSeconds;
                _stopping = true;
                _beating = false;
            }

            _bpm = 0;
        }

        // Escala del glifo en ese instante (1 en reposo). El tiempo no retrocede.
        public double ScaleAt(double nowSeconds)
        {
            if (_beating)
            {
                Advance(nowSeconds);
                return Shape(nowSeconds - _beatStart, _beatPeriod);
            }

            if (_stopping)
            {
                var elapsedMs = (nowSeconds - _stopStart) * 1000;
                if (elapsedMs >= MotionSpecs.BeatStopMs)
                {
                    _stopping = false;
                    return 1;
                }

                var e = Easing.Evaluate(Tokens.EaseMove, Math.Max(0, elapsedMs) / MotionSpecs.BeatStopMs);
                return _stopFrom + (1 - _stopFrom) * e;
            }

            return 1;
        }

        // |escala − 1| ≤ 0,002 (regla de toma de las capturas, DN-21).
        public bool IsAtRest(double nowSeconds)
        {
            return Math.Abs(ScaleAt(nowSeconds) - 1) <= MotionSpecs.BeatRestTolerance;
        }

        // Escala de un latido de período T a τ segundos de su inicio (fuera del latido, 1).
        public static double Shape(double tauSeconds, double periodSeconds)
        {
            if (!(tauSeconds >= 0))
            {
                return 1;
            }

            var compression = Math.Min(1.0, periodSeconds * 1000 / MotionSpecs.BeatFullCycleMs);
            if (!(compression > 0))
            {
                return 1;
            }

            var t = tauSeconds * 1000 / compression; // tiempo en la escala del latido sin comprimir
            return 1 + MotionSpecs.BeatMainGain * Pulse(t, 0, MotionSpecs.BeatRiseMs, MotionSpecs.BeatFallMs)
                     + MotionSpecs.SecondBeatGain * Pulse(t, MotionSpecs.SecondBeatOffsetMs, MotionSpecs.SecondBeatRiseMs, MotionSpecs.SecondBeatFallMs);
        }

        // Golpe: 0 → 1 en riseMs con ease.enter y 1 → 0 en fallMs con ease.move, a partir de startMs.
        private static double Pulse(double tMs, double startMs, double riseMs, double fallMs)
        {
            var local = tMs - startMs;
            if (local <= 0 || local >= riseMs + fallMs)
            {
                return 0;
            }

            if (local < riseMs)
            {
                return Easing.Evaluate(Tokens.EaseEnter, local / riseMs);
            }

            return 1 - Easing.Evaluate(Tokens.EaseMove, (local - riseMs) / fallMs);
        }

        private void StartBeat(double startSeconds)
        {
            _beatStart = startSeconds;
            _beatPeriod = 60.0 / _bpm;
            BeatsStarted++;
        }

        // Empieza los latidos que vencieron hasta "ahora", cada uno con el bpm vigente.
        private void Advance(double nowSeconds)
        {
            if (!_beating)
            {
                return;
            }

            while (nowSeconds >= _beatStart + _beatPeriod)
            {
                StartBeat(_beatStart + _beatPeriod);
            }
        }
    }
}
