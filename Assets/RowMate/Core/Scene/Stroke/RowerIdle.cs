using System;
using RowMate.Core.Design;

namespace RowMate.Core.Scene.Stroke
{
    // Lo que el reposo le suma a la pose en un cuadro (pesado ya por Weight).
    public readonly struct RowerIdleSample
    {
        public static readonly RowerIdleSample None = new RowerIdleSample(0, 0, 0, 0);

        public RowerIdleSample(double weight, double chestOffsetM, double shoulderOffsetM, double gazeYawDeg)
        {
            Weight = weight;
            ChestOffsetM = chestOffsetM;
            ShoulderOffsetM = shoulderOffsetM;
            GazeYawDeg = gazeYawDeg;
        }

        public double Weight { get; }
        public double ChestOffsetM { get; }
        public double ShoulderOffsetM { get; }
        public double GazeYawDeg { get; }
    }

    // El reposo (AC-SCN-05-10; ANIMATION_BIBLE §3.9): después de 1,5 s quieto (pausa o catch sin remar) el peso entra en 0,4 s y el
    // remero respira (15/min: pecho ± 0,6 cm, hombros ± 0,5 cm, en fase) y mira de a ratos (± 2°, un objetivo nuevo cada 4–7 s con
    // DartRandom(7), resorte crítico de 0,6 s); al moverse sale en 0,2 s. Se suma a la pose: nunca toca p ni φ. Sin asignar.
    public sealed class RowerIdle
    {
        private readonly DartRandom _random = new DartRandom(7);
        private double _gazeTarget;
        private double _gazeVelocity;
        private double _nextGazeAt;

        public RowerIdle()
        {
            Reset();
        }

        public double Weight { get; private set; }

        public double ChestOffsetM { get; private set; }

        public double ShoulderOffsetM { get; private set; }

        public double GazeYawDeg { get; private set; }

        public RowerIdleSample Sample => new RowerIdleSample(Weight, ChestOffsetM * Weight, ShoulderOffsetM * Weight, GazeYawDeg * Weight);

        public void Reset()
        {
            _random.Reseed(7);
            Weight = 0;
            ChestOffsetM = 0;
            ShoulderOffsetM = 0;
            GazeYawDeg = 0;
            _gazeTarget = 0;
            _gazeVelocity = 0;
            _nextGazeAt = double.NaN;
        }

        public void Step(double dt, double stillSeconds, double sceneTime)
        {
            var step = dt < 0 || double.IsNaN(dt) ? 0 : (dt > SceneState.MaxStep ? SceneState.MaxStep : dt);
            if (stillSeconds >= MotionSpecs.IdleDelaySeconds)
            {
                Weight = Math.Min(1, Weight + step / MotionSpecs.IdleFadeInSeconds);
            }
            else
            {
                Weight = Math.Max(0, Weight - step / MotionSpecs.IdleFadeOutSeconds);
            }

            var breath = Math.Sin(2 * Math.PI * sceneTime * MotionSpecs.IdleBreathsPerMinute / 60);
            ChestOffsetM = MotionSpecs.IdleChestM * breath;
            ShoulderOffsetM = MotionSpecs.IdleShoulderM * breath;

            if (double.IsNaN(_nextGazeAt))
            {
                _nextGazeAt = sceneTime + NextGap();
            }
            else if (sceneTime >= _nextGazeAt)
            {
                _gazeTarget = (_random.NextDouble() * 2 - 1) * MotionSpecs.IdleGazeDeg;
                _nextGazeAt = sceneTime + NextGap();
            }

            // Resorte crítico con ω = 4 / 0,6 s⁻¹ (llega al 90 % en ≈ 0,6 s), integración semi-implícita.
            var omega = 4.0 / MotionSpecs.IdleGazeSpringSeconds;
            var acc = omega * omega * (_gazeTarget - GazeYawDeg) - 2 * omega * _gazeVelocity;
            _gazeVelocity += acc * step;
            GazeYawDeg += _gazeVelocity * step;
        }

        private double NextGap()
        {
            return MotionSpecs.IdleGazeMinSeconds + _random.NextDouble() * (MotionSpecs.IdleGazeMaxSeconds - MotionSpecs.IdleGazeMinSeconds);
        }
    }
}
