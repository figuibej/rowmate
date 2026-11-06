using System;
using RowMate.Core.Design;
using RowMate.Core.Text;

namespace RowMate.Core.Scene
{
    // La hora de la escena (scene_view.dart:81-86; AC-SCN-09-01, F-SCN-23): la forzada del simulador o la local con resolución de
    // minuto, hora + minuto/60 (los segundos se ignoran), armada como el legacy.
    public static class SceneHour
    {
        public static double Effective(int? forcedHour, DateTimeOffset localNow)
        {
            if (forcedHour.HasValue)
            {
                return forcedHour.Value;
            }

            return localNow.Hour + localNow.Minute / 60.0;
        }
    }

    // Qué cambió la hora: el simulador (−, +, Hora real) · la hora real pasó al minuto siguiente · cualquier otra cosa (un
    // cambio de zona o de la hora del equipo, la vuelta del segundo plano, la apertura, ForceHourForTest).
    public enum HourChange
    {
        Forced,
        MinuteTick,
        Instant,
    }

    // La hora que dibuja la escena (06a-escena-lago/02-tech-design §2.5; DV-58, DN6a-10): Forced va desde la visible por el
    // camino más corto del reloj (Δ = Mod(objetivo − visible + 12; 24) − 12; con Δ = −12, +12) en 1,5 s con ease.move; MinuteTick
    // va del minuto viejo al nuevo en 1 s lineal (también de 23:59 a 0:00); Instant, en el acto. Con reducir movimiento, todo en
    // el acto. Un cambio durante otro sale de la visible. El dato es el del minuto: al terminar, la visible es el objetivo exacto.
    public sealed class SceneHourTransition
    {
        public const double Seconds = MotionSpecs.ForcedHourSeconds;
        public const double MinuteSeconds = MotionSpecs.MinuteTickSeconds;

        private double _origin;
        private double _delta;
        private double _target;
        private double _start;
        private double _duration;
        private bool _linear;

        public SceneHourTransition()
        {
            Visible = double.NaN;
        }

        // NaN antes del primer Retarget.
        public double Visible { get; private set; }

        public double Target => _target;

        public bool IsRunning { get; private set; }

        public void Retarget(double target, double now, HourChange kind, bool reduceMotion)
        {
            target = DartMath.Mod(target, 24);
            if (double.IsNaN(Visible) || kind == HourChange.Instant || reduceMotion)
            {
                Visible = target;
                _target = target;
                IsRunning = false;
                return;
            }

            var visible = Evaluate(now);
            var delta = DartMath.Mod(target - visible + 12, 24) - 12;
            if (delta == -12)
            {
                delta = 12;
            }

            _origin = visible;
            _delta = delta;
            _target = target;
            _start = now;
            _linear = kind == HourChange.MinuteTick;
            _duration = _linear ? MinuteSeconds : Seconds;
            IsRunning = delta != 0;
            if (!IsRunning)
            {
                Visible = target;
            }
        }

        public double Evaluate(double now)
        {
            if (!IsRunning)
            {
                return Visible;
            }

            var u = (now - _start) / _duration;
            if (u >= 1)
            {
                IsRunning = false;
                Visible = _target;
                return Visible;
            }

            var curve = _linear ? (u < 0 ? 0 : u) : Easing.Evaluate(Tokens.EaseMove, u);
            Visible = DartMath.Mod(_origin + _delta * curve, 24);
            return Visible;
        }
    }
}
