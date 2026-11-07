using System;
using RowMate.Core.Design;

namespace RowMate.Core.Scene.Stroke
{
    // Lo que la boya lee de la foto del motor (snapshot.Series): si el paso es de trabajo, su índice, las vueltas cerradas del paso,
    // los metros del paso y la revisión de la foto.
    public readonly struct BuoyInputs
    {
        public static readonly BuoyInputs None = new BuoyInputs(false, -1, 0, 0, -1);

        public BuoyInputs(bool isWorkStep, int stepIndex, int lapCount, double stepMeters, long revision)
        {
            IsWorkStep = isWorkStep;
            StepIndex = stepIndex;
            LapCount = lapCount;
            StepMeters = stepMeters;
            Revision = revision;
        }

        public bool IsWorkStep { get; }
        public int StepIndex { get; }
        public int LapCount { get; }
        public double StepMeters { get; }
        public long Revision { get; }
    }

    // La boya de 500 m (P-06; AC-SCN-08-10; 00-arte-remero §9; DT7-11): solo en los pasos de trabajo de una rutina, a x = +4,5 m. Su
    // objetivo en z de mundo es D + 9,5 + (500·(vueltas + 1) − d_paso), con d_paso = los metros del paso de la última foto más lo que
    // avanzó la escena desde esa foto (llegan cada segundo): así pasa a la altura del remero (relZ 9,5) cuando el panel cierra la
    // vuelta. Mientras está adelante sigue al objetivo con un resorte de τ 0,6 s y una corrección ≤ 0,5 m/s (sin saltos). Al cerrar la
    // vuelta queda fija en el mundo y se recicla a 30 m detrás de la cámara; la siguiente aparece con el tramado de la orilla (400 →
    // 360 m). Si el paso termina sin cerrar la vuelta, se funde en 0,6 s; si la vuelta cierra justo al terminar el paso (la boya ya
    // está a la altura del remero, ± 2 m), queda fija. Sin asignar.
    public sealed class BuoyTracker
    {
        public const double X = 4.5;
        public const double RecycleRelZ = -30;
        public const double AppearFarRelZ = 400, AppearNearRelZ = 360;
        public const double PassTolerance = 2;

        private long _revision = long.MinValue;
        private int _stepIndex = -1;
        private int _lapCount;
        private double _snapMeters;
        private double _advanced;
        private bool _tracking;
        private double _fade = 1;
        private bool _fading;

        public bool Visible => Alpha > 0;

        public double WorldZ { get; private set; }

        public double Alpha { get; private set; }

        public bool Fixed { get; private set; }

        public void Reset()
        {
            _revision = long.MinValue;
            _stepIndex = -1;
            _lapCount = 0;
            _snapMeters = 0;
            _advanced = 0;
            _tracking = false;
            _fading = false;
            _fade = 1;
            Fixed = false;
            Alpha = 0;
            WorldZ = 0;
        }

        public void Step(double dt, in BuoyInputs inputs, double sceneDistance, double sceneSpeed, double sceneTime)
        {
            var step = dt < 0 || double.IsNaN(dt) ? 0 : (dt > SceneState.MaxStep ? SceneState.MaxStep : dt);
            var stepChanged = inputs.StepIndex != _stepIndex;
            if (inputs.Revision != _revision)
            {
                _revision = inputs.Revision;
                _snapMeters = inputs.StepMeters;
                _advanced = 0;
            }
            else
            {
                _advanced += sceneSpeed * step;
            }

            var stepMeters = _snapMeters + _advanced;

            if (stepChanged)
            {
                // Terminó el paso: la boya en camino queda fija si ya está a la altura del remero; si no, se funde.
                if (_tracking && !Fixed)
                {
                    if (WorldZ - sceneDistance <= SceneCamera.BoatZ + PassTolerance)
                    {
                        Fixed = true;
                    }
                    else
                    {
                        _fading = true;
                    }
                }

                _stepIndex = inputs.StepIndex;
                _lapCount = inputs.LapCount;
                if (!Fixed && !_fading)
                {
                    _tracking = false;
                }
            }
            else if (inputs.LapCount > _lapCount)
            {
                // El panel cerró la vuelta: queda fija donde esté.
                _lapCount = inputs.LapCount;
                if (_tracking)
                {
                    Fixed = true;
                }
            }

            if (_fading)
            {
                _fade -= step / MotionSpecs.BuoyFadeSeconds;
                if (_fade <= 0)
                {
                    _fade = 1;
                    _fading = false;
                    _tracking = false;
                    Fixed = false;
                }
            }

            if (Fixed && WorldZ - sceneDistance < RecycleRelZ)
            {
                Fixed = false;
                _tracking = false;
            }

            if (!Fixed && !_fading && inputs.IsWorkStep)
            {
                var target = sceneDistance + SceneCamera.BoatZ + (SeriesTrackerLap * (_lapCount + 1) - stepMeters);
                if (!_tracking)
                {
                    _tracking = true;
                    WorldZ = target;
                }
                else
                {
                    var move = (target - WorldZ) * (1 - Math.Exp(-step / MotionSpecs.BuoySpringSeconds));
                    var max = MotionSpecs.BuoyMaxCorrectionMps * step;
                    WorldZ += move > max ? max : (move < -max ? -max : move);
                }
            }

            if (!_tracking)
            {
                Alpha = 0;
                return;
            }

            var relZ = WorldZ - sceneDistance;
            var appear = 1 - SceneMath.Clamp01((relZ - AppearNearRelZ) / (AppearFarRelZ - AppearNearRelZ));
            Alpha = appear * (_fading ? Math.Max(0, _fade) : 1);
        }

        private const double SeriesTrackerLap = Workout.SeriesTracker.LapMeters;
    }
}
