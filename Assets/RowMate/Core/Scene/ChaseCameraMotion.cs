using System;
using RowMate.Core.Design;

namespace RowMate.Core.Scene
{
    // La respiración de la cámara (ANIMATION_BIBLE §5.2 sin los términos del bote, que llegan en la 7; AC-SCN-02-04).
    public readonly struct BreathingSpec
    {
        public BreathingSpec(double lateralA, double lateralFreqA, double lateralB, double lateralFreqB, double lateralPhaseB,
                             double vertical, double verticalFreq, double verticalPhase, double rollDeg, double rollFreq, double rollPhase,
                             double pitchDeg, double pitchFreq, double pitchPhase)
        {
            LateralA = lateralA;
            LateralFreqA = lateralFreqA;
            LateralB = lateralB;
            LateralFreqB = lateralFreqB;
            LateralPhaseB = lateralPhaseB;
            Vertical = vertical;
            VerticalFreq = verticalFreq;
            VerticalPhase = verticalPhase;
            RollDeg = rollDeg;
            RollFreq = rollFreq;
            RollPhase = rollPhase;
            PitchDeg = pitchDeg;
            PitchFreq = pitchFreq;
            PitchPhase = pitchPhase;
        }

        // El lago (§9.3): rolido 0,15° (DT6a-01).
        public static BreathingSpec Lake => new BreathingSpec(
            MotionSpecs.BreathLateralA, MotionSpecs.BreathLateralFreqA, MotionSpecs.BreathLateralB, MotionSpecs.BreathLateralFreqB,
            MotionSpecs.BreathLateralPhaseB, MotionSpecs.BreathVertical, MotionSpecs.BreathVerticalFreq, MotionSpecs.BreathVerticalPhase,
            MotionSpecs.BreathRollDeg, MotionSpecs.BreathRollFreq, MotionSpecs.BreathRollPhase, MotionSpecs.BreathPitchDeg,
            MotionSpecs.BreathPitchFreq, MotionSpecs.BreathPitchPhase);

        // It. 8 (AC-SCN-06-04): las cuatro amplitudes × gain (la costa, × 1,8); frecuencias y fases iguales. Con 1, idéntica.
        public BreathingSpec Scaled(double gain)
        {
            return new BreathingSpec(LateralA * gain, LateralFreqA, LateralB * gain, LateralFreqB, LateralPhaseB, Vertical * gain, VerticalFreq,
                VerticalPhase, RollDeg * gain, RollFreq, RollPhase, PitchDeg * gain, PitchFreq, PitchPhase);
        }

        public double LateralA { get; }
        public double LateralFreqA { get; }
        public double LateralB { get; }
        public double LateralFreqB { get; }
        public double LateralPhaseB { get; }
        public double Vertical { get; }
        public double VerticalFreq { get; }
        public double VerticalPhase { get; }
        public double RollDeg { get; }
        public double RollFreq { get; }
        public double RollPhase { get; }
        public double PitchDeg { get; }
        public double PitchFreq { get; }
        public double PitchPhase { get; }
    }

    // El vuelo de entrada (00-deuda-diseno §3.3 con DN6a-08; AC-SCN-02-05).
    public readonly struct FlightSpec
    {
        public FlightSpec(double p0x, double p0y, double p0z, double p1x, double p1y, double p1z, double p2x, double p2y, double p2z,
                          double lookAtY, double durationSeconds, double focalFrom, double horizonFrom)
        {
            P0X = p0x; P0Y = p0y; P0Z = p0z;
            P1X = p1x; P1Y = p1y; P1Z = p1z;
            P2X = p2x; P2Y = p2y; P2Z = p2z;
            LookAtY = lookAtY;
            DurationSeconds = durationSeconds;
            FocalFrom = focalFrom;
            HorizonFrom = horizonFrom;
        }

        public static FlightSpec Default => new FlightSpec(
            MotionSpecs.FlightP0X, MotionSpecs.FlightP0Y, MotionSpecs.FlightP0Z, MotionSpecs.FlightP1X, MotionSpecs.FlightP1Y,
            MotionSpecs.FlightP1Z, MotionSpecs.FlightP2X, MotionSpecs.FlightP2Y, MotionSpecs.FlightP2Z, MotionSpecs.FlightLookAtY,
            MotionSpecs.FlightDurationMs / 1000, MotionSpecs.FlightFocalFrom, MotionSpecs.FlightHorizonFrom);

        public double P0X { get; }
        public double P0Y { get; }
        public double P0Z { get; }
        public double P1X { get; }
        public double P1Y { get; }
        public double P1Z { get; }
        public double P2X { get; }
        public double P2Y { get; }
        public double P2Z { get; }

        // El punto al que mira al empezar: el remero (0; LookAtY; 9,5).
        public double LookAtY { get; }

        public double DurationSeconds { get; }

        // La focal arranca en FocalFrom × la del contrato.
        public double FocalFrom { get; }

        // El horizonte de la proyección empieza a moverse en esta fracción del recorrido.
        public double HorizonFrom { get; }
    }

    // La toma aérea del completado (AC-SCN-02-07; DN6a-07).
    public readonly struct AerialSpec
    {
        public AerialSpec(double riseM, double backM, double pitchDeg, double seconds)
        {
            RiseM = riseM;
            BackM = backM;
            PitchDeg = pitchDeg;
            Seconds = seconds;
        }

        public static AerialSpec Default => new AerialSpec(MotionSpecs.AerialRiseM, MotionSpecs.AerialBackM, MotionSpecs.AerialPitchDeg,
            MotionSpecs.AerialSeconds);

        public double RiseM { get; }
        public double BackM { get; }
        public double PitchDeg { get; }
        public double Seconds { get; }
    }

    // Las líneas de tiempo puras de la cámara (06a-escena-lago/02-tech-design §2.2). Sin azar ni reloj: todo por argumento.
    public static class ChaseCameraMotion
    {
        // La pose del contrato con la respiración × multiplier, en el tiempo de escena t (sigue en pausa, AC-SCN-01-03).
        public static CameraPose Breathing(in SceneCamera cam, double sceneTime, double multiplier, in BreathingSpec spec)
        {
            return AddBreathing(CameraPose.Contract(cam), sceneTime, multiplier, spec);
        }

        // Suma la respiración a una pose (la del contrato o la de la toma aérea).
        public static CameraPose AddBreathing(in CameraPose pose, double t, double multiplier, in BreathingSpec spec)
        {
            if (multiplier == 0)
            {
                return pose;
            }

            const double TwoPi = 2 * Math.PI;
            var lateral = spec.LateralA * Math.Sin(TwoPi * spec.LateralFreqA * t)
                + spec.LateralB * Math.Sin(TwoPi * spec.LateralFreqB * t + spec.LateralPhaseB);
            var vertical = spec.Vertical * Math.Sin(TwoPi * spec.VerticalFreq * t + spec.VerticalPhase);
            var roll = spec.RollDeg * Math.Sin(TwoPi * spec.RollFreq * t + spec.RollPhase);
            var pitch = spec.PitchDeg * Math.Sin(TwoPi * spec.PitchFreq * t + spec.PitchPhase);
            return new CameraPose(pose.X + multiplier * lateral, pose.Y + multiplier * vertical, pose.Z, pose.YawDeg,
                pose.PitchDeg + multiplier * pitch, pose.RollDeg + multiplier * roll, pose.FocalDp, pose.HorizonDp);
        }

        // Los términos del bote (It. 7; AC-SCN-06-03; ANIMATION_BIBLE §5.2; DT7-06), ya filtrados (BoatCameraFilter) y por el mismo
        // multiplicador de la respiración (pausa × 0,6, reducir movimiento × 0, la entrada S(0; 0,6 s)): + 25 % de la arfada en y,
        // + 30 % del surge en z y + 15 % del cabeceo del casco, este con un tope suave B·tanh(x/B), B = lo que le queda al horizonte
        // hasta 2,95 dp (en rad, con la focal de la pose): el ± 3 dp es la regla dura. Las traslaciones no mueven el horizonte.
        public static CameraPose AddBoatTerms(in CameraPose pose, in SceneCamera cam, in Stroke.BoatMotionSample filtered, double multiplier)
        {
            return AddBoatTerms(pose, cam, filtered, multiplier, MotionSpecs.CameraHorizonCapDp);
        }

        // It. 8 (AC-SCN-06-04): el mismo con el tope suave del escenario (2,95 dp; 5,35 en la costa, cuyo tope duro es ± 5,4).
        public static CameraPose AddBoatTerms(in CameraPose pose, in SceneCamera cam, in Stroke.BoatMotionSample filtered, double multiplier,
                                              double softCapDp)
        {
            if (multiplier == 0)
            {
                return pose;
            }

            var y = pose.Y + multiplier * MotionSpecs.CameraHeaveShare * filtered.HeaveM;
            var z = pose.Z + multiplier * MotionSpecs.CameraSurgeShare * filtered.SurgeM;
            var pitch = SoftPitchRad(pose, cam, multiplier * MotionSpecs.CameraPitchShare * filtered.PitchRad, softCapDp);
            return new CameraPose(pose.X, y, z, pose.YawDeg, pose.PitchDeg + pitch * 180 / Math.PI, pose.RollDeg, pose.FocalDp, pose.HorizonDp);
        }

        // El tope suave del cabeceo (DT7-06): en TV, V390 y P360 entra casi entero; en W1280, TH y L640, casi en 0.
        public static double SoftPitchRad(in CameraPose pose, in SceneCamera cam, double pitchRad)
        {
            return SoftPitchRad(pose, cam, pitchRad, MotionSpecs.CameraHorizonCapDp);
        }

        public static double SoftPitchRad(in CameraPose pose, in SceneCamera cam, double pitchRad, double softCapDp)
        {
            var room = softCapDp - HorizonDeviationDp(cam, pose);
            if (!(room > 0) || pose.FocalDp <= 0)
            {
                return 0;
            }

            var cap = room / pose.FocalDp;
            return cap * Math.Tanh(pitchRad / cap);
        }

        // Cuánto se aparta el horizonte de su línea del contrato en los bordes de la pantalla, en dp (el barrido de AC-SCN-02-04 y
        // la marca de VID-10). La traslación no lo mueve (está en el infinito); lo mueven el cabeceo, el rolido y la proyección.
        public static double HorizonDeviationDp(in SceneCamera cam, in CameraPose pose)
        {
            // La normal del plano del horizonte (0; 1; 0) en las coordenadas de la cámara: la fila 1 de la rotación.
            var r = ChaseCameraProjection.Rotation(pose.YawDeg, pose.PitchDeg, pose.RollDeg);
            double nx = r.M10, ny = r.M11, nz = r.M12;
            if (Math.Abs(ny) < 1e-12)
            {
                return double.PositiveInfinity;
            }

            // Un punto de pantalla (sx; sy) mira en la dirección (sx − W/2; h − sy; f): el horizonte es n·d = 0.
            var left = pose.HorizonDp + (nx * (0 - cam.CenterX) + nz * pose.FocalDp) / ny;
            var right = pose.HorizonDp + (nx * (cam.Width - cam.CenterX) + nz * pose.FocalDp) / ny;
            return Math.Max(Math.Abs(left - cam.HorizonY), Math.Abs(right - cam.HorizonY));
        }

        // El vuelo en la fracción u ∈ [0; 1] de su duración: la posición y la orientación con ease-in-out-sine; la focal y el
        // horizonte con ease.move. u ≥ 1 devuelve EXACTAMENTE la pose del contrato (las constantes, sin redondeo).
        public static CameraPose Flight(in SceneCamera cam, double u, in FlightSpec spec)
        {
            if (u >= 1)
            {
                return CameraPose.Contract(cam);
            }

            if (u < 0 || double.IsNaN(u))
            {
                u = 0;
            }

            var s = (1 - Math.Cos(Math.PI * u)) / 2;
            Bezier(spec, s, out var x, out var y, out var z);

            // La orientación: slerp desde mirar al remero (desde P0) hasta nivelada (la identidad), con la misma curva.
            LookAt(spec.P0X, spec.P0Y, spec.P0Z, 0, spec.LookAtY, SceneCamera.BoatZ, out var yaw0, out var pitch0);
            var q0 = SceneQuaternion.FromAngles(yaw0, pitch0, 0);
            var q = SceneQuaternion.Slerp(q0, SceneQuaternion.Identity, s);
            q.ToAngles(out var yaw, out var pitch, out var roll);

            var move = Easing.Evaluate(Tokens.EaseMove, u);
            var focal = cam.Focal * (spec.FocalFrom + (1 - spec.FocalFrom) * move);
            var hu = (u - spec.HorizonFrom) / (1 - spec.HorizonFrom);
            var hMove = Easing.Evaluate(Tokens.EaseMove, hu < 0 ? 0 : (hu > 1 ? 1 : hu));
            var mid = cam.Height / 2;
            var horizon = mid + (cam.HorizonY - mid) * hMove;
            return new CameraPose(x, y, z, yaw, pitch, roll, focal, horizon);
        }

        // La posición del vuelo en el parámetro de la curva s ∈ [0; 1] (sin la curva de tiempo).
        public static void Bezier(in FlightSpec spec, double s, out double x, out double y, out double z)
        {
            var a = 1 - s;
            var b0 = a * a * a;
            var b1 = 3 * a * a * s;
            var b2 = 3 * a * s * s;
            var b3 = s * s * s;
            x = b0 * spec.P0X + b1 * spec.P1X + b2 * spec.P2X + b3 * 0;
            y = b0 * spec.P0Y + b1 * spec.P1Y + b2 * spec.P2Y + b3 * SceneCamera.CamHeight;
            z = b0 * spec.P0Z + b1 * spec.P1Z + b2 * spec.P2Z + b3 * 0;
        }

        // La salida (AC-SCN-02-06): desde la pose de ese cuadro, +1,5 m y −4 m en 1,2 s con ease.move.
        public static CameraPose Exit(in CameraPose from, double secondsSinceStart)
        {
            var e = Easing.Evaluate(Tokens.EaseMove, secondsSinceStart / MotionSpecs.ExitSeconds);
            return from.WithPosition(from.X, from.Y + MotionSpecs.ExitRiseM * e, from.Z - MotionSpecs.ExitBackM * e);
        }

        // La toma aérea (AC-SCN-02-07): mitad arriba y mitad atrás y el cabeceo, con ease.move; después, quieta.
        public static CameraPose Aerial(in CameraPose from, double secondsSinceStart, in AerialSpec spec)
        {
            var e = AerialProgress(secondsSinceStart, spec);
            return new CameraPose(from.X, from.Y + spec.RiseM * e, from.Z - spec.BackM * e, from.YawDeg, from.PitchDeg + spec.PitchDeg * e,
                from.RollDeg, from.FocalDp, from.HorizonDp);
        }

        public static double AerialProgress(double secondsSinceStart, in AerialSpec spec)
        {
            return Easing.Evaluate(Tokens.EaseMove, secondsSinceStart / spec.Seconds);
        }

        // La guiñada y el cabeceo (grados) para mirar desde (fx; fy; fz) hacia (tx; ty; tz).
        public static void LookAt(double fx, double fy, double fz, double tx, double ty, double tz, out double yawDeg, out double pitchDeg)
        {
            var dx = tx - fx;
            var dy = ty - fy;
            var dz = tz - fz;
            yawDeg = Math.Atan2(dx, dz) * 180 / Math.PI;
            pitchDeg = Math.Atan2(dy, Math.Sqrt(dx * dx + dz * dz)) * 180 / Math.PI;
        }
    }

    // Los términos del bote filtrados para la cámara (AC-SCN-06-03): arfada τ 0,2 s, surge τ 0,35 s y cabeceo τ 0,5 s. Sin asignar.
    public struct BoatCameraFilter
    {
        public Stroke.BoatMotionSample Value;

        public void Step(double dt, in Stroke.BoatMotionSample target)
        {
            var step = dt < 0 || double.IsNaN(dt) ? 0 : (dt > SceneState.MaxStep ? SceneState.MaxStep : dt);
            Value.HeaveM += (target.HeaveM - Value.HeaveM) * (1 - Math.Exp(-step / MotionSpecs.CameraHeaveTauSeconds));
            Value.SurgeM += (target.SurgeM - Value.SurgeM) * (1 - Math.Exp(-step / MotionSpecs.CameraSurgeTauSeconds));
            Value.PitchRad += (target.PitchRad - Value.PitchRad) * (1 - Math.Exp(-step / MotionSpecs.CameraPitchTauSeconds));
            Value.RollRad = 0;
        }

        public void Snap(in Stroke.BoatMotionSample target)
        {
            Value = new Stroke.BoatMotionSample(target.PitchRad, target.HeaveM, 0, target.SurgeM);
        }
    }

    public enum CameraMode
    {
        Hidden,
        Flight,
        Contract,
        Exit,
        Aerial,
    }

    // La cámara de una sesión (06a-escena-lago/02-tech-design §2.2): una sola, sin entrada del usuario (AC-SCN-02-08). El
    // multiplicador de la respiración es el producto de cuatro factores: la pausa (×0,6 con sus rampas), la entrada
    // (S(0; 0,6 s) desde el fin del vuelo o desde ShowInPlace), la toma aérea (de 1 a 0 en 6 s) y reducir movimiento o
    // "respiración en 0" (×0): no hay salto de pose en el cuadro de los 1 820 ms ni al aparecer la escena demorada.
    public sealed class ChaseCameraDirector
    {
        public const double BreathingRampSeconds = MotionSpecs.BreathingRampSeconds;

        private readonly BreathingSpec _breathing;
        private readonly FlightSpec _flight;
        private readonly AerialSpec _aerial;

        private double _flightStart;
        private double _rampStart = double.NaN;
        private double _exitStart;
        private bool _exitFrozen;
        private bool _exitFromCaptured;
        private CameraPose _exitFrom;
        private double _aerialStart = double.NaN;
        private CameraPose _last;
        private bool _hasLast;

        // La pausa: el factor va de _pauseFrom a _pauseTo entre _pauseStart y _pauseStart + _pauseDuration (ease.move).
        private double _pauseFrom = 1;
        private double _pauseTo = 1;
        private double _pauseStart;
        private double _pauseDuration;

        public ChaseCameraDirector()
            : this(BreathingSpec.Lake, FlightSpec.Default, AerialSpec.Default)
        {
        }

        public ChaseCameraDirector(in BreathingSpec breathing, in FlightSpec flight, in AerialSpec aerial)
        {
            _breathing = breathing;
            _flight = flight;
            _aerial = aerial;
            Reset();
        }

        public CameraMode Mode { get; private set; }

        // El multiplicador de la respiración del último Evaluate (para los valores por cuadro del arnés).
        public double LastBreathingMultiplier { get; private set; }

        // Al abrir: oculta, sin pausa, sin rampa.
        public void Reset()
        {
            Mode = CameraMode.Hidden;
            _rampStart = double.NaN;
            _aerialStart = double.NaN;
            _exitFrozen = false;
            _exitFromCaptured = false;
            _hasLast = false;
            _pauseFrom = 1;
            _pauseTo = 1;
            _pauseDuration = 0;
            LastBreathingMultiplier = 0;
        }

        // El vuelo, anclado al instante en que empieza (el toque + 220 ms, no el primer cuadro: el último cae en 1 820 aunque un
        // cuadro se demore). Con reducir movimiento: el contrato en el acto (sin respiración: la apaga reducir movimiento).
        public void BeginFlight(double startTime, bool reduceMotion)
        {
            if (reduceMotion)
            {
                Mode = CameraMode.Contract;
                _rampStart = startTime;
                return;
            }

            Mode = CameraMode.Flight;
            _flightStart = startTime;
            _rampStart = startTime + _flight.DurationSeconds;
        }

        // La escena demorada (AC-SCN-01-07): el contrato sin vuelo y la respiración entrando en 0,6 s.
        public void ShowInPlace(double now)
        {
            Mode = CameraMode.Contract;
            _rampStart = now;
        }

        // La salida (AC-SCN-02-06): desde la pose que tenga en ese cuadro. Con reducir movimiento la cámara no se mueve.
        public void BeginExit(double now, bool reduceMotion)
        {
            Mode = CameraMode.Exit;
            _exitStart = now;
            _exitFrozen = reduceMotion;
            _exitFromCaptured = false;
        }

        // La toma aérea (solo con el diálogo de completado). Con reducir movimiento, nada.
        public void BeginAerial(double now, bool reduceMotion)
        {
            if (reduceMotion || Mode == CameraMode.Exit || Mode == CameraMode.Hidden)
            {
                return;
            }

            Mode = CameraMode.Aerial;
            _aerialStart = now;
        }

        // La respiración ×0,6 en 300 ms (ease.move) al pausar y ×1 en 240 al reanudar, desde donde esté.
        public void SetPaused(bool paused, double now)
        {
            var current = PauseFactor(now);
            _pauseFrom = current;
            _pauseTo = paused ? MotionSpecs.BreathingPausedFactor : 1;
            _pauseStart = now;
            _pauseDuration = (paused ? MotionSpecs.BreathingPauseInMs : MotionSpecs.BreathingPauseOutMs) / 1000;
        }

        // El multiplicador de la respiración en ese instante (los cuatro factores).
        public double BreathingMultiplier(double now, bool reduceMotion, bool breathingOff)
        {
            if (reduceMotion || breathingOff)
            {
                return 0;
            }

            switch (Mode)
            {
                case CameraMode.Contract:
                    return PauseFactor(now) * Ramp(now);
                case CameraMode.Aerial:
                    return PauseFactor(now) * Ramp(now) * (1 - ChaseCameraMotion.AerialProgress(now - _aerialStart, _aerial));
                default:
                    return 0;
            }
        }

        public CameraPose Evaluate(double now, double sceneTime, in SceneCamera cam, bool reduceMotion, bool breathingOff)
        {
            CameraPose pose;
            if (Mode == CameraMode.Flight && now - _flightStart >= _flight.DurationSeconds)
            {
                Mode = CameraMode.Contract;
            }

            switch (Mode)
            {
                case CameraMode.Flight:
                    pose = ChaseCameraMotion.Flight(cam, (now - _flightStart) / _flight.DurationSeconds, _flight);
                    LastBreathingMultiplier = 0;
                    break;
                case CameraMode.Contract:
                    LastBreathingMultiplier = BreathingMultiplier(now, reduceMotion, breathingOff);
                    pose = ChaseCameraMotion.Breathing(cam, sceneTime, LastBreathingMultiplier, _breathing);
                    break;
                case CameraMode.Aerial:
                    LastBreathingMultiplier = BreathingMultiplier(now, reduceMotion, breathingOff);
                    var aerial = ChaseCameraMotion.Aerial(CameraPose.Contract(cam), now - _aerialStart, _aerial);
                    pose = ChaseCameraMotion.AddBreathing(aerial, sceneTime, LastBreathingMultiplier, _breathing);
                    break;
                case CameraMode.Exit:
                    if (!_exitFromCaptured)
                    {
                        _exitFrom = _hasLast ? _last : CameraPose.Contract(cam);
                        _exitFromCaptured = true;
                    }

                    LastBreathingMultiplier = 0;
                    pose = _exitFrozen ? _exitFrom : ChaseCameraMotion.Exit(_exitFrom, now - _exitStart);
                    break;
                default:
                    LastBreathingMultiplier = 0;
                    pose = CameraPose.Contract(cam);
                    break;
            }

            _last = pose;
            _hasLast = true;
            return pose;
        }

        private double Ramp(double now)
        {
            if (double.IsNaN(_rampStart))
            {
                return 1;
            }

            return SceneMath.SmoothStep(0, BreathingRampSeconds, now - _rampStart);
        }

        private double PauseFactor(double now)
        {
            if (_pauseDuration <= 0)
            {
                return _pauseTo;
            }

            var e = Easing.Evaluate(Tokens.EaseMove, (now - _pauseStart) / _pauseDuration);
            return _pauseFrom + (_pauseTo - _pauseFrom) * e;
        }
    }
}
