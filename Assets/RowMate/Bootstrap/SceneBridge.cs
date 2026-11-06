using System;
using RowMate.Core.Common;
using RowMate.Core.Scene;
using RowMate.Core.Workout;
using RowMate.Presentation.Motion;
using RowMate.Presentation.Workout;
using RowMate.Scene;
using UnityEngine;

namespace RowMate.Bootstrap
{
    // El puerto del inmersivo sobre la escena (06a-escena-lago/02-tech-design §3.3; DT6a-03): implementa IImmersiveScene (Presentation)
    // sobre SceneHost (Scene), que no se ven entre sí; Bootstrap es el único que ve a los dos. Además arma el tramo del armado de cada
    // cuadro (SceneWarmupPolicy: 3 ms con Workout en reposo y nada moviéndose, 2 ms con el inmersivo abierto sin movimiento finito, 0
    // en cualquier otro caso) y avisa a la superficie en cada cuadro dibujado.
    public sealed class SceneBridge : IImmersiveScene, IDisposable
    {
        private readonly SceneHost _host;
        private readonly ObservableValue<ImmersiveSceneReadiness> _readiness;
        private readonly IDisposable _subscription;
        private readonly IAppLogger _logger;
        private long _lastDraws;

        public SceneBridge(SceneHost host, IAppLogger logger)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _readiness = new ObservableValue<ImmersiveSceneReadiness>(Map(host.Readiness.Value), logger);
            _subscription = host.Readiness.Subscribe(r => _readiness.Value = Map(r), false);
            _host.OutputChanged += OnHostOutputChanged;
        }

        public SceneHost Host => _host;

        public IReadOnlyObservable<ImmersiveSceneReadiness> Readiness => _readiness;

        public Texture? Output => _host.Output;

        public event Action? OutputChanged;

        public bool IsShowing => _host.IsShowing;

        public void Preload()
        {
            _host.Preload();
        }

        public void Open(double now)
        {
            _host.Open(now);
        }

        public void CatchUp()
        {
            _host.CatchUp();
        }

        public void BeginFlight(double now)
        {
            _host.BeginFlight(now);
        }

        public void ShowInPlace(double now)
        {
            _host.ShowInPlace(now);
        }

        public void SetPauseVeil(double weight)
        {
            _host.SetPauseVeil(weight);
        }

        public void BeginExit(double now)
        {
            _host.BeginExit(now);
        }

        public void BeginAerial(double now)
        {
            _host.BeginAerial(now);
        }

        public void Close()
        {
            _host.Close();
        }

        // Un cuadro (AppRoot.LateUpdate; el anfitrión de pruebas): el tramo del armado y el Tick de la escena. Si se dibujó, la
        // superficie vuelve a mostrar la RT (S6a-7).
        public void Tick(double now, MotionDriver motion, WorkoutPhase phase, bool forceUnboundedWarmup = false)
        {
            var slice = forceUnboundedWarmup
                ? double.PositiveInfinity
                // B6a-19: los rodillos y los destellos de cada lectura no cuentan (con lecturas cada segundo siempre hay uno corriendo y la
                // escena que se prepara en el inmersivo no avanzaba nunca); la cascada, las transiciones y los diálogos, sí. QA-V6a-REN-06:
                // el toque, el arrastre y la rueda del usuario también (no son IMotion). El cuadro del armado del HUD siempre tiene la
                // cascada corriendo (finita): no hace falta pasarlo aparte (hudBuildFrame) ni el negro quieto (screenBlackAndStill).
                : SceneWarmupPolicy.SliceMs(phase == WorkoutPhase.Idle && !_host.IsOpen, false, _host.IsOpen,
                    motion.HasFiniteNonRecurringMotions || motion.UserInputActive ? 1 : 0, false);
            if (_host.Readiness.Value == SceneReadiness.Loading)
            {
                // El diagnóstico de [PERF]: cuántos cuadros se preparó la escena y cuántos no tuvo tramo, y qué lo retuvo (sin asignar).
                if (slice > 0)
                {
                    WarmupFrames++;
                }
                else
                {
                    StarvedFrames++;
                    LastStarvedBy = motion.FirstFiniteNonRecurring?.GetType();
                }
            }

            _host.Tick(now, slice);
            if (_host.Draws != _lastDraws)
            {
                _lastDraws = _host.Draws;
                Drawn?.Invoke();
            }
        }

        // Los cuadros con tramo y sin tramo mientras la escena se preparaba, y el tipo de lo último que la retuvo ([PERF]).
        public long WarmupFrames { get; private set; }

        public long StarvedFrames { get; private set; }

        public Type? LastStarvedBy { get; private set; }

        // La escena se dibujó en este cuadro (la superficie pide el repintado del panel).
        public event Action? Drawn;

        public void Dispose()
        {
            _subscription.Dispose();
            _host.OutputChanged -= OnHostOutputChanged;
            _host.Dispose();
        }

        private void OnHostOutputChanged()
        {
            try
            {
                OutputChanged?.Invoke();
            }
            catch (Exception error)
            {
                _logger.Log(LogLevel.Warning, SceneHost.Tag, "un suscriptor de la RT de la escena falló", error);
            }
        }

        private static ImmersiveSceneReadiness Map(SceneReadiness readiness)
        {
            switch (readiness)
            {
                case SceneReadiness.Loading:
                    return ImmersiveSceneReadiness.Loading;
                case SceneReadiness.Ready:
                    return ImmersiveSceneReadiness.Ready;
                case SceneReadiness.Failed:
                    return ImmersiveSceneReadiness.Failed;
                default:
                    return ImmersiveSceneReadiness.NotLoaded;
            }
        }
    }
}
