using System;
using System.Collections.Generic;
using System.Diagnostics;
using RowMate.Core.Common;
using RowMate.Core.Scene;
using RowMate.Core.Scene.Art;
using RowMate.Core.Workout;
using RowMate.Scene.Quality;
using RowMate.Scene.Slots;
using UnityEngine;
using Unity.Profiling;
using UnityEngine.Rendering;
using AppContext = RowMate.App.Composition.AppContext;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace RowMate.Scene
{
    // En qué está la escena (06a-escena-lago/02-tech-design §3.1).
    public enum SceneReadiness
    {
        NotLoaded,
        Loading,
        Ready,
        Failed,
    }

    // Cómo se arma un SceneHost: el nivel, lo que vive en el asset de URP (IRenderAssetQuality; en el Editor uno que solo anota), la
    // región de la luna (ISystemLocale; null = norte) y, para las pruebas y el arnés, el armado de la orilla en el acto y la escena
    // avanzada en pasos de 1/60 s mientras corre el reloj de prueba (§5.1).
    public sealed class SceneHostOptions
    {
        public QualityTier Tier { get; set; } = QualityTier.High;

        public IRenderAssetQuality? AssetQuality { get; set; }

        public string? RegionCode { get; set; }

        public bool SynchronousStreaming { get; set; }

        public bool FixedSteps { get; set; }

        // El tramo de la orilla por cuadro en la app (ms): un trabajo acotado (un trozo de terreno, un relleno, 5 m de pasto).
        public double StreamingBudgetMs { get; set; } = 1.0;
    }

    // El ciclo de vida de la escena 3D (06a-escena-lago/02-tech-design §3.1 a §3.4, §3.9, §4; ADR 022): Preload (Workout en reposo,
    // P-14) arma el mundo por tramos (SceneWorldBuilder, SceneShaderWarmup, los Tick sintéticos y el único dibujo de calentamiento,
    // fuera de pantalla y antes de Ready); Open (el toque) crea la SceneSession en 0, que corre desde ese cuadro aunque no se dibuje;
    // Tick (una vez por cuadro: AppRoot.LateUpdate o el Tick del anfitrión de pruebas, envuelto por el observador de memoria) avanza la
    // sesión, aplica el cuadro (SceneDirector) y pide el dibujo a la RT sRGB (SceneRenderer) solo con la cámara a la vista; Close
    // (detrás del negro) descarta la sesión y desactiva la raíz: después no se dibuja nada. Output es la RT (OutputChanged al rehacerla).
    // Nunca un error que frene la sesión: una falla deja Failed, un aviso, y el inmersivo sigue con el fondo provisorio.
    public sealed class SceneHost : IDisposable
    {
        public const string Tag = "SCENE";

        private readonly SceneWorldDefinition _definition;
        private readonly AppContext _app;
        private readonly IAppLogger _logger;
        private readonly SceneHostOptions _options;
        private readonly ObservableValue<SceneReadiness> _readiness;
        private readonly SceneRenderTarget _target = new SceneRenderTarget();
        private readonly SlotLog _slotLog;
        private readonly Stopwatch _watch = new Stopwatch();
        private SceneFrame _frame;
        private SceneWorldBuilder? _builder;
        private IEnumerator<bool>? _units;
        private SceneShaderWarmup? _warmup;
        private int _phase;
        private int _syntheticStep;
        private SceneWorld? _world;
        private SceneSession? _session;
        private QualityTierProfile _profile;
        private bool _ownsProfile;
        private double _lastNow;
        private bool _paused;
        private double _veil;
        private int _widthPx;
        private int _heightPx;
        private double _density = 1;
        private double _resolution;
        private double _vegetationDensity = 1;
        private bool _disposed;
        private bool _closing;
        private bool _lowMemoryHooked;
        private readonly Stopwatch _unitWatch = new Stopwatch();
        private readonly Stopwatch _cpuWatch = new Stopwatch();

        // Controles de prueba (internal).
        private double _delayReadiness;
        private double _readyNotBefore = double.NegativeInfinity;
        private string? _regionForTest;
        private bool _regionOverridden;
        private bool _scrims = true;
        private bool _stars = true;
        private bool _stampFrames;
        private Texture2D? _stamp;
        private long _stampCounter;

        private static readonly ProfilerMarker TickMarker = new ProfilerMarker("RowMate.Scene.Tick");
        private static readonly ProfilerMarker WarmupMarker = new ProfilerMarker("RowMate.Scene.Warmup");
        // La CPU de la escena SIN el dibujo de URP (§4.1: ≤ 1,5 ms; QA-V6a-REN-07, QA-V6a-09): la sesión de Core, la orilla, aplicar el cuadro
        // y armar las matrices del instanciado. El dibujo (SubmitRenderRequest, que dibuja adentro de la llamada) queda afuera.
        public const string UpdateMarkerName = "RowMate.Scene.Update";
        private static readonly ProfilerMarker UpdateMarker = new ProfilerMarker(UpdateMarkerName);

        // Las fases del armado (RunUnit), con su nombre para [PERF] y las pruebas (QA-V6a-REN-06: el costo de cada unidad, medido).
        private static readonly string[] PhaseNames =
        {
            "mundo", "variantes", "orilla de 920 m", "Tick sintético", "RT de la sesión", "post de URP (RT chica)", "post de URP",
            "dibujo de calentamiento", "orilla de la sesión", "Ready",
        };

        public SceneHost(SceneWorldDefinition definition, AppContext app, SceneHostOptions? options = null)
        {
            _definition = definition != null ? definition : throw new ArgumentNullException(nameof(definition));
            _app = app ?? throw new ArgumentNullException(nameof(app));
            _logger = app.Logger;
            _options = options ?? new SceneHostOptions();
            _readiness = new ObservableValue<SceneReadiness>(SceneReadiness.NotLoaded, _logger);
            _slotLog = new SlotLog(_logger);
            _profile = ProfileFor(_options.Tier, out _ownsProfile);
            _resolution = _profile.Resolution;
            _frame = SceneFrame.Create();
        }

        public IReadOnlyObservable<SceneReadiness> Readiness => _readiness;

        public RenderTexture? Output => _target.Texture;

        public event Action? OutputChanged;

        public SceneHostOptions Options => _options;

        public QualityTier Tier => _profile.Tier;

        public QualityTierProfile Profile => _profile;

        public SlotLog SlotLog => _slotLog;

        public SceneWorld? World => _world;

        public SceneSession? Session => _session;

        public ref readonly SceneFrame Frame => ref _frame;

        public SceneRenderTarget Target => _target;

        public bool IsOpen => _session != null && !_closing;

        // Se dibuja: abierta, lista y con la cámara a la vista (después del primer cuadro de la entrada o de ShowInPlace).
        public bool IsShowing => IsOpen && _readiness.Value == SceneReadiness.Ready && _session!.Camera.Mode != CameraMode.Hidden;

        public double Resolution => _resolution;

        public int WidthPx => _widthPx;

        public int HeightPx => _heightPx;

        public double Density => _density;

        public double WidthDp => _widthPx / _density;

        public double HeightDp => _heightPx / _density;

        // Los dibujos de la escena después de Ready (DEMO-01: 0 fuera del inmersivo) y los de calentamiento (uno, antes de Ready).
        public long Draws { get; private set; }

        public long WarmupDraws { get; private set; }

        public long Ticks { get; private set; }

        // El peor cuadro del armado (ms) desde el último Open (la escena demorada: SLC, VID-11, [PERF]).
        public double WorstWarmupMs { get; private set; }

        public double LastWarmupMs { get; private set; }

        // La unidad más cara del armado desde el último Preload u Open (ms) y su fase (QA-V6a-REN-06; SceneWarmupPolicy.MaxUnitCostMs).
        public double WorstUnitMs { get; private set; }

        public string WorstUnitPhase { get; private set; } = "";

        // Con la fase del mundo, qué armaba esa unidad (SceneWorldBuilder.Step).
        public string WorstUnitStep { get; private set; } = "";

        // La unidad más cara de cada fase (ms; PhaseNames), desde el último armado: el desglose de [PERF].
        private readonly double[] _worstByPhase = new double[10];

        public double WorstUnitMsOf(int phase) => phase >= 0 && phase < _worstByPhase.Length ? _worstByPhase[phase] : 0;

        public static string PhaseName(int phase) => phase >= 0 && phase < PhaseNames.Length ? PhaseNames[phase] : "";

        public static int PhaseCount => PhaseNames.Length;

        // La CPU de la escena del último cuadro sin el dibujo de URP (ms; el marcador RowMate.Scene.Update; QA-V6a-REN-07).
        public double LastCpuMs { get; private set; }

        // Las veces que la orilla se armó entera, en el acto, en un cuadro con la escena a la vista (un salto de más de un segmento:
        // QA-V6a-REN-04). En la app tiene que quedar en 0: el rebobinado y la escena demorada terminan detrás del negro.
        public long InViewShoreRebuilds { get; private set; }

        // La última vez que se dibujó (en el reloj de la escena).
        public double LastDrawNow { get; private set; } = double.NaN;

        // ¿Queda armado de la orilla pendiente? (el arnés espera a que no antes de una toma).
        public bool IsStreamingIdle => _world?.Store == null || !_world.Store.HasPendingWork;

        // La medida de memoria (§4.4; QA-G6aT-06): true al empezar el Tick, false al terminarlo.
        public Action<bool>? TickObserver { get; set; }

        // Lo de la adaptación que no vive en la escena (el nivel y la distancia de sombras del asset de URP).
        public IRenderAssetQuality? AssetQuality => _options.AssetQuality;

        // ── El ciclo de vida ──

        public void SetViewport(int widthPx, int heightPx, double density)
        {
            if (widthPx <= 0 || heightPx <= 0 || !(density > 0))
            {
                return;
            }

            _widthPx = widthPx;
            _heightPx = heightPx;
            _density = density;
        }

        public void Preload()
        {
            if (_disposed || _readiness.Value != SceneReadiness.NotLoaded)
            {
                return;
            }

            StartBuild();
        }

        public void Open(double now)
        {
            if (_disposed)
            {
                return;
            }

            if (_delayReadiness > 0)
            {
                // La escena demorada (AC-SCN-01-07): el mundo sin armar y Ready no antes de `seconds` desde el toque.
                DisposeWorld();
                _readyNotBefore = now + _delayReadiness;
                _delayReadiness = 0;
            }

            if (_readiness.Value == SceneReadiness.NotLoaded)
            {
                StartBuild();
            }

            WorstWarmupMs = 0;
            var region = _regionOverridden ? _regionForTest : _options.RegionCode;
            if (_nextSession != null && string.Equals(_nextRegion, region, StringComparison.Ordinal))
            {
                // La que se armó en reposo (QA-V6a-REN-02): en el cuadro del toque, sin armar la sesión ni sus arreglos.
                _session = _nextSession;
                _frame = _nextFrame;
            }
            else
            {
                _session = new SceneSession(new LakeEnvironment(), new SceneSessionOptions { RegionCode = region });
                _frame = SceneFrame.Create();
            }

            _nextSession = null;
            _nextFrame = default;
            _session.Open(now);
            _lastNow = now;
            _closing = false;
            _paused = false;
            _veil = 0;
        }

        public void BeginFlight(double startTime)
        {
            _session?.Camera.BeginFlight(startTime, ReduceMotion);
        }

        public void ShowInPlace(double now)
        {
            _session?.Camera.ShowInPlace(now);
        }

        public void SetPauseVeil(double weight)
        {
            _veil = weight < 0 ? 0 : weight > 1 ? 1 : weight;
        }

        public void BeginExit(double now)
        {
            _session?.Camera.BeginExit(now, ReduceMotion);
        }

        public void BeginAerial(double now)
        {
            _session?.Camera.BeginAerial(now, ReduceMotion);
        }

        public void Close()
        {
            if (_session == null)
            {
                return;
            }

            _session = null;
            _closing = false;
            _frame.Draw = false;
            if (_world != null)
            {
                _world.Root.SetActive(false);
                _world.Rig?.Reset();
            }
        }

        // La adaptación térmica (Bootstrap la aplica): la resolución de la RT (DT6a-16), nunca arriba de la del nivel (después de LowerTier
        // la adaptación puede volver a subirla), y la densidad de la vegetación.
        public void SetResolution(double resolution)
        {
            var r = resolution > 0 && resolution <= 1 ? resolution : 1;
            _resolution = Math.Min(r, _profile.Resolution);
        }

        // El último paso de la adaptación (AC-SCN-01-13: "por último, el nivel"; QA-V6a-REN-09; ADR 018: cada nivel es un perfil con su
        // asset): baja la escena ENTERA un nivel, no solo el asset de URP. El perfil de abajo (HDR y formato de la RT, MSAA, sombras,
        // bloom, campo de estrellas, capa de nubes, LOD, bosque cercano y pasto) se aplica al mundo armado SIN rehacerlo (las palabras
        // clave de los tres niveles están en la colección de variantes); lo del asset de URP, por IRenderAssetQuality. La resolución no
        // sube: queda la menor entre la que dejó la adaptación y la del nivel nuevo. false = ya estaba en Bajo.
        public bool LowerTier()
        {
            if (_disposed || _profile.Tier == QualityTier.Low)
            {
                return false;
            }

            var tier = _profile.Tier == QualityTier.High ? QualityTier.Medium : QualityTier.Low;
            var previous = _profile;
            var ownedPrevious = _ownsProfile;
            _profile = ProfileFor(tier, out _ownsProfile);
            _resolution = Math.Min(_resolution, _profile.Resolution);
            _options.AssetQuality?.ApplyTier(tier);
            if (_world?.Director != null)
            {
                ApplyProfile(_world);
            }

            if (ownedPrevious && !ReferenceEquals(previous, _builderProfile))
            {
                MeshUpload.Destroy(previous);
            }

            return true;
        }

        // El perfil del nivel en el mundo ya armado (las mismas piezas que aplica SceneWorldBuilder al terminar).
        private void ApplyProfile(SceneWorld world)
        {
            var director = world.Director!;
            SceneQualityApplier.Apply(_profile, world.Rig!.Camera, world.Rig.Data, world.Lighting!, world.Sky!, world.Clouds!, world.Grass!, director.Sky,
                director.Shore, director.Fill, world.Store!);
        }

        // El perfil con el que armó el SceneWorldBuilder en curso (si LowerTier llega a mitad del armado, se aplica el nuevo al terminar).
        private QualityTierProfile? _builderProfile;

        public void SetVegetationDensity(double density)
        {
            _vegetationDensity = density > 0 && density <= 1 ? density : 1;
            if (_world?.Lighting != null)
            {
                _world.Lighting.Density = _vegetationDensity;
            }
        }

        // Un cuadro: el armado (con su tramo, SceneWarmupPolicy), la sesión, el cuadro aplicado y el dibujo. warmupSliceMs:
        // double.PositiveInfinity = todo en el acto (las pruebas y el arnés).
        public void Tick(double now, double warmupSliceMs)
        {
            if (_disposed)
            {
                return;
            }

            TickObserver?.Invoke(true);
            try
            {
                using (TickMarker.Auto())
                {
                    TickCore(now, warmupSliceMs);
                }
            }
            finally
            {
                TickObserver?.Invoke(false);
            }
        }

        private void TickCore(double now, double warmupSliceMs)
        {
            Ticks++;
            if (AllocateInTickForTest)
            {
                _allocationSink = new byte[64];
            }

            if (_readiness.Value == SceneReadiness.Loading)
            {
                RunBuild(now, warmupSliceMs);
            }

            var session = _session;
            if (session == null)
            {
                // En reposo, con tramo: la orilla vuelve a la de 0 m para la próxima sesión (sin el salto en su primer cuadro) y, si no
                // está, se arma la sesión siguiente (QA-V6a-REN-02: el cuadro del toque no paga sus arreglos).
                if (_readiness.Value == SceneReadiness.Ready && warmupSliceMs > 0 && _world?.Store != null)
                {
                    if (_lastStreamDistance != 0 || _world.Store.HasPendingWork)
                    {
                        _world.Store.Step(0, double.IsPositiveInfinity(warmupSliceMs) ? warmupSliceMs : Math.Min(warmupSliceMs, 1.0));
                        if (!_world.Store.HasPendingWork)
                        {
                            _lastStreamDistance = 0;
                        }
                    }
                    else if (_nextSession == null)
                    {
                        PrepareNextSession();
                    }
                }

                LastCpuMs = 0;
                return;
            }

            _cpuWatch.Restart();
            UpdateMarker.Begin();
            var inputs = Inputs(now);
            UpdatePause(session, now);
            if (_options.FixedSteps)
            {
                session.Advance(inputs, Math.Max(0, now - _lastNow), ref _frame);
            }
            else
            {
                session.Tick(inputs, ref _frame);
            }

            _lastNow = now;
            var showing = IsShowing && _world?.Director != null;
            _frame.Draw = showing;
            if (!showing)
            {
                // Detrás del negro (0-220 ms de la entrada, o la escena demorada): la orilla sigue a la sesión por tramos, así el primer
                // cuadro a la vista no la arma entera (QA-V6a-REN-04).
                StreamHidden();
                UpdateMarker.End();
                LastCpuMs = _cpuWatch.Elapsed.TotalMilliseconds;
                return;
            }

            if (Application.isPlaying && !OnDemandRendering.willCurrentFrameRender)
            {
                UpdateMarker.End();
                LastCpuMs = _cpuWatch.Elapsed.TotalMilliseconds;
                return;
            }

            DrawFrame(now, false, null);
        }

        // La orilla hacia la distancia de la sesión con el tramo de la orilla, mientras la escena no está a la vista (lista y abierta).
        private void StreamHidden()
        {
            var store = _world?.Store;
            if (_readiness.Value != SceneReadiness.Ready || store == null)
            {
                return;
            }

            var distance = _frame.Distance;
            if (!double.IsNaN(_lastStreamDistance) && Math.Abs(distance - _lastStreamDistance) < 1e-9 && !store.HasPendingWork)
            {
                return;
            }

            store.Step(distance, _options.SynchronousStreaming ? double.PositiveInfinity : _options.StreamingBudgetMs);
            if (!store.HasPendingWork)
            {
                _lastStreamDistance = distance;
            }
        }

        // El cuadro del armado del HUD (negro y quieto; IImmersiveScene.CatchUp): si la orilla de la sesión no quedó armada detrás del
        // negro, se termina acá, en el acto (QA-V6a-REN-04). Con la orilla al día no hace nada.
        public void CatchUp()
        {
            var store = _world?.Store;
            if (_disposed || _session == null || _readiness.Value != SceneReadiness.Ready || store == null)
            {
                return;
            }

            var distance = _frame.Distance;
            var behind = double.IsNaN(_lastStreamDistance) || Math.Abs(distance - _lastStreamDistance) > ShoreEnvironment.SegmentLength;
            if (!behind && !store.HasPendingWork)
            {
                return;
            }

            store.Step(distance, double.PositiveInfinity);
            _lastStreamDistance = distance;
            CatchUps++;
        }

        // Las veces que CatchUp tuvo que terminar la orilla en el cuadro del armado del HUD (lo normal es 0: alcanza con lo de detrás
        // del negro).
        public long CatchUps { get; private set; }

        // Aplica el cuadro y lo dibuja en la RT de la escena (o en `target`, las sondas).
        private void DrawFrame(double now, bool withoutPost, RenderTexture? target)
        {
            var world = _world!;
            if (!world.Root.activeSelf)
            {
                world.Root.SetActive(true);
            }

            if (EnsureTarget())
            {
                OutputChanged?.Invoke();
            }

            var store = world.Store!;
            var jump = double.IsNaN(_lastStreamDistance) || Math.Abs(_frame.Distance - _lastStreamDistance) > ShoreEnvironment.SegmentLength;
            if (jump && target == null)
            {
                InViewShoreRebuilds++;
            }

            store.Step(_frame.Distance, _options.SynchronousStreaming || jump ? double.PositiveInfinity : _options.StreamingBudgetMs);
            _lastStreamDistance = _frame.Distance;
            var director = world.Director!;
            director.Overlay.Enabled = _scrims;
            director.Sky.StarsEnabled = _stars;
            director.Lighting.EmissiveGain = withoutPost ? 1 : SkyArt.EmissiveGain;
            director.Apply(_frame);
            director.QueueInstancedDraws();
            if (target == null)
            {
                // La CPU de la escena termina acá: el pedido de render (URP dibuja adentro de la llamada) no es de la escena (§4.1).
                UpdateMarker.End();
                LastCpuMs = _cpuWatch.Elapsed.TotalMilliseconds;
            }

            var data = world.Rig!.Data;
            var post = data.renderPostProcessing;
            if (withoutPost)
            {
                data.renderPostProcessing = false;
            }

            try
            {
                world.Renderer!.Render(target ?? _target.Texture!);
            }
            finally
            {
                data.renderPostProcessing = post;
                director.Lighting.EmissiveGain = SkyArt.EmissiveGain;
            }

            if (target == null)
            {
                Draws++;
                LastDrawNow = now;
                if (_stampFrames)
                {
                    Stamp();
                }
            }
        }

        // La RT de la sesión con la ventana, la resolución y el formato del nivel (true = es otra RT: quien la muestra la vuelve a tomar).
        private bool EnsureTarget()
        {
            return _target.Ensure(_widthPx > 0 ? _widthPx : 256, _heightPx > 0 ? _heightPx : 256, _resolution, _profile.Hdr, _profile.Tier);
        }

        private double _lastStreamDistance = double.NaN;
        private byte[]? _allocationSink;

        private SceneTickInputs Inputs(double now)
        {
            var clock = _app.Scheduler;
            var workout = SceneInputs.From(_app.Workout.State.Value);
            return new SceneTickInputs(now, clock.LocalNow, clock.UtcNow, workout, _app.Scene.HourOverride.Value, ReduceMotion,
                _widthPx > 0 ? WidthDp : 411, _heightPx > 0 ? HeightDp : 914, _veil);
        }

        private bool ReduceMotion => _app.Accessibility.Value.ReduceMotion;

        // La respiración ×0,6 en pausa (con sus rampas): la fase del motor.
        private void UpdatePause(SceneSession session, double now)
        {
            var paused = _app.Workout.State.Value.Phase == WorkoutPhase.Paused;
            if (paused != _paused)
            {
                _paused = paused;
                session.Camera.SetPaused(paused, now);
            }
        }

        // ── El armado ──

        private void StartBuild()
        {
            if (!_definition.HasSlotsAndTiers)
            {
                Fail("SceneWorld.asset sin slots o sin niveles (correr RowMate/Bootstrap/Reaplicar)", null);
                return;
            }

            _builder = new SceneWorldBuilder(_definition, _profile, _slotLog, _logger);
            _builderProfile = _profile;
            _units = _builder.Build();
            _phase = 0;
            WorstUnitMs = 0;
            WorstUnitPhase = "";
            WorstUnitStep = "";
            Array.Clear(_worstByPhase, 0, _worstByPhase.Length);
            _syntheticStep = 0;
            _warmup = new SceneShaderWarmup(_definition.Variants);
            _readiness.Value = SceneReadiness.Loading;
            HookLowMemory();
        }

        private void RunBuild(double now, double sliceMs)
        {
            if (!(sliceMs > 0))
            {
                return;
            }

            using (WarmupMarker.Auto())
            {
                _watch.Restart();
                var units = 0;
                try
                {
                    while (_readiness.Value == SceneReadiness.Loading
                           && (double.IsPositiveInfinity(sliceMs) || SceneWarmupPolicy.RunAnother(sliceMs, _watch.Elapsed.TotalMilliseconds, units)))
                    {
                        // QA-V6a-REN-06: cada unidad se mide (la peor, con su fase, va a [PERF] y a SET).
                        var phase = _phase;
                        _unitWatch.Restart();
                        var ran = RunUnit(now);
                        var unitMs = _unitWatch.Elapsed.TotalMilliseconds;
                        if (ran && phase < _worstByPhase.Length && unitMs > _worstByPhase[phase])
                        {
                            _worstByPhase[phase] = unitMs;
                        }

                        if (ran && unitMs > WorstUnitMs)
                        {
                            WorstUnitMs = unitMs;
                            WorstUnitPhase = PhaseNames[Math.Min(phase, PhaseNames.Length - 1)];
                            WorstUnitStep = phase == 0 && _builder != null ? _builder.Step : "";
                        }

                        if (!ran)
                        {
                            break;
                        }

                        units++;
                    }
                }
                catch (Exception error)
                {
                    Fail("el armado de la escena falló", error);
                }

                LastWarmupMs = _watch.Elapsed.TotalMilliseconds;
                if (LastWarmupMs > WorstWarmupMs)
                {
                    WorstWarmupMs = LastWarmupMs;
                }
            }
        }

        // Una unidad. false = no hay nada que hacer en este cuadro (esperando la compilación o la demora de prueba).
        // Fases (PhaseNames; "Cambios por verificación (ronda 1)" de 02-tech-design): 0 el mundo (SceneWorldBuilder), 1 las variantes,
        // 2 la orilla de 920 m, 3 los Tick sintéticos, 4 la RT de la sesión, 5 y 6 el post de URP (en una RT chica y en la de la sesión),
        // 7 el dibujo de calentamiento (en dos unidades: su cuadro y el dibujo), 8 la orilla donde empieza a verse (la de la sesión abierta o
        // la de 0 m), 9 Ready (no antes de la demora de prueba).
        private bool RunUnit(double now)
        {
            switch (_phase)
            {
                case 0:
                    if (_units!.MoveNext())
                    {
                        if (_units.Current)
                        {
                            _world = _builder!.World;
                            _world!.Lighting!.Density = _vegetationDensity;
                            if (!ReferenceEquals(_builderProfile, _profile))
                            {
                                // LowerTier llegó a mitad del armado: el perfil nuevo.
                                ApplyProfile(_world);
                            }

                            _phase = 1;
                        }

                        return true;
                    }

                    _phase = 1;
                    return true;
                case 1:
                    if (_warmup!.Step())
                    {
                        _phase = 2;
                        return true;
                    }

                    // En el Editor, la espera de la compilación asíncrona: nada más en este cuadro. En el player, otra variante.
                    return !Application.isEditor;
                case 2:
                    return StreamUnit(SyntheticDistance, 3);
                case 3:
                    if (SyntheticTick(_syntheticStep++))
                    {
                        _phase = 4;
                    }

                    return true;
                case 4:
                    // QA-V6a-REN-05 (§3.1: "Ready: la RT existe"): la RT de la sesión, con la ventana y el formato del nivel, en su unidad.
                    if (EnsureTarget())
                    {
                        OutputChanged?.Invoke();
                    }

                    _phase = 5;
                    return true;
                case 5:
                    if (SceneShaderWarmup.AnythingCompiling)
                    {
                        return false;
                    }

                    // QA-V6a-REN-06: el primer pedido de render con la cámara de la escena en una RT chica (el código de URP corre por
                    // primera vez: el JIT de Mono), separado de la RT del tamaño de la sesión (sus intermedios, la unidad siguiente).
                    WarmupPostDraw(true);
                    _phase = 6;
                    return true;
                case 6:
                    if (SceneShaderWarmup.AnythingCompiling)
                    {
                        return false;
                    }

                    // QA-V6a-REN-01 y -06: el post de URP (UberPost, Bloom, el LUT) y sus intermedios del tamaño y el formato de la sesión,
                    // sin la escena (la máscara de la cámara en 0), en una unidad propia antes del dibujo de calentamiento.
                    WarmupPostDraw(false);
                    _phase = 7;
                    return true;
                case 7:
                    if (SceneShaderWarmup.AnythingCompiling)
                    {
                        return false;
                    }

                    if (!_warmupFrameReady)
                    {
                        PrepareWarmupFrame();
                        return true;
                    }

                    WarmupDraw();
                    _phase = 8;
                    return true;
                case 8:
                    // QA-V6a-REN-04 (B): la orilla donde va a empezar a verse la escena: la de la sesión abierta (la escena demorada, con el
                    // bote ya lejos de 0 m) o la de 0 m (la próxima sesión).
                    return StreamUnit(_session != null ? _frame.Distance : 0, 9);
                default:
                    if (now < _readyNotBefore)
                    {
                        return false;
                    }

                    if (_session != null && Math.Abs(_frame.Distance - _lastStreamDistance) > ShoreEnvironment.SegmentLength / 2)
                    {
                        // La sesión siguió avanzando mientras se esperaba: la orilla otra vez al día antes de Ready.
                        _phase = 8;
                        return true;
                    }

                    _readyNotBefore = double.NegativeInfinity;
                    _readiness.Value = SceneReadiness.Ready;
                    return false;
            }
        }

        // La sesión siguiente, armada en reposo (QA-V6a-REN-02): Open la toma en el cuadro del toque en lugar de armar la sesión y sus
        // arreglos ahí. Con otra región (el control de prueba), Open arma una nueva.
        private void PrepareNextSession()
        {
            var region = _regionOverridden ? _regionForTest : _options.RegionCode;
            _nextSession = new SceneSession(new LakeEnvironment(), new SceneSessionOptions { RegionCode = region });
            _nextRegion = region;
            _nextFrame = SceneFrame.Create();
        }

        private SceneSession? _nextSession;
        private string? _nextRegion;
        private SceneFrame _nextFrame;

        // Un trabajo de la orilla en `distance` (acotado: un trozo de terreno, un relleno, 5 m de pasto). Al terminar, la fase siguiente.
        private bool StreamUnit(double distance, int next)
        {
            var store = _world!.Store!;
            store.Step(distance, 0.5);
            if (!store.HasPendingWork)
            {
                _phase = next;
                _lastStreamDistance = distance;
            }

            return true;
        }

        // Los Tick sintéticos (QA-G6aT-07): una sesión de usar y tirar avanza, sin dibujar, por las 12 h, las 22 h con el velo, un paso
        // del vuelo, uno de la salida y uno de la toma aérea, con la orilla de 920 m (muelle, cabaña y relleno a la vista, ya armada en
        // la fase 2): la primera vez del C# (el JIT de Mono) no cae en el primer cuadro de la escena. Un Tick por unidad. true al terminar.
        private bool SyntheticTick(int step)
        {
            var world = _world!;
            var scratch = _scratch ??= new SceneSession(new LakeEnvironment(), new SceneSessionOptions());
            var frame = _scratchFrame;
            if (frame.Stars == null)
            {
                frame = SceneFrame.Create();
            }

            var now = 1000.0 + step;
            if (step == 0)
            {
                scratch.Open(now);
            }

            int? hour = step == 1 ? 22 : 12;
            var veil = step == 1 ? 1.0 : 0.0;
            switch (step)
            {
                case 2:
                    scratch.Camera.BeginFlight(now, false);
                    break;
                case 3:
                    scratch.Camera.BeginExit(now - 0.5, false);
                    break;
                case 4:
                    scratch.Camera.BeginAerial(now - 0.5, false);
                    break;
            }

            var inputs = new SceneTickInputs(now + 0.4, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, new SceneInputs(120, 24, true), hour, false, 411, 914, veil);
            scratch.Tick(inputs, ref frame);
            frame.Distance = SyntheticDistance;
            scratch.Segments.VisibleProps(SyntheticDistance, frame.VisibleProps);
            world.Store!.Step(SyntheticDistance, 0.5);
            world.Director!.Apply(frame);
            _scratchFrame = frame;
            if (step < 4)
            {
                return false;
            }

            _scratch = null;
            return true;
        }

        private const double SyntheticDistance = 920;
        private SceneSession? _scratch;
        private SceneFrame _scratchFrame;

        // El cuadro del único dibujo de calentamiento (su propia unidad: la sesión de usar y tirar y su Tick): a las 12 h con WarmUpAll
        // (estrellas, ventanas, nubes y velo a la vista), así el dibujo toca todas las variantes de la sesión.
        private void PrepareWarmupFrame()
        {
            var scratch = new SceneSession(new LakeEnvironment(), new SceneSessionOptions());
            var frame = SceneFrame.Create();
            frame.WarmUpAll = true;
            scratch.Open(0);
            scratch.Camera.ShowInPlace(0);
            var inputs = new SceneTickInputs(0.5, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, new SceneInputs(120, 24, true), 12, false, 411, 914, 1);
            scratch.Tick(inputs, ref frame);
            frame.Distance = SyntheticDistance;
            scratch.Segments.VisibleProps(SyntheticDistance, frame.VisibleProps);
            for (var i = 0; i < frame.VisibleProps.Count; i++)
            {
                frame.PropLight[i] = 1;
            }

            _warmupFrame = frame;
            _warmupFrameReady = true;
        }

        private SceneFrame _warmupFrame;
        private bool _warmupFrameReady;

        // El post de URP antes del dibujo de calentamiento (QA-V6a-REN-01, -05, -06): un pedido de render a la RT de la sesión con la
        // máscara de la cámara en 0 (sin la escena, con el Volume del último Tick sintético): URP sube UberPost, el bloom y el LUT con las
        // palabras clave de la sesión (el formato de la RT decide las de HDR) y arma sus intermedios de ese tamaño. Fuera de pantalla.
        // small: a una RT chica del mismo formato (el primer pedido: el código de URP por primera vez), descartada al terminar.
        private void WarmupPostDraw(bool small)
        {
            var world = _world!;
            if (EnsureTarget())
            {
                OutputChanged?.Invoke();
            }

            RenderTexture? scratch = null;
            var camera = world.Rig!.Camera;
            var mask = camera.cullingMask;
            world.Root.SetActive(true);
            try
            {
                var target = _target.Texture!;
                if (small)
                {
                    scratch = new RenderTexture(new RenderTextureDescriptor(64, 64, target.graphicsFormat, target.depthStencilFormat))
                    {
                        name = "RowMate.SceneWarmupSmall",
                        hideFlags = HideFlags.HideAndDontSave,
                    };
                    scratch.Create();
                    target = scratch;
                }

                camera.cullingMask = 0;
                world.Renderer!.Render(target);
            }
            finally
            {
                camera.cullingMask = mask;
                world.Root.SetActive(false);
                if (scratch != null)
                {
                    scratch.Release();
                    MeshUpload.Destroy(scratch);
                }
            }
        }

        // El único dibujo de calentamiento (AC-SCN-01-06): un cuadro normal de la escena, fuera de pantalla, EN LA RT DE LA SESIÓN (su
        // tamaño y su formato: QA-V6a-REN-05), con el cuadro de PrepareWarmupFrame, así toca todas las variantes de la sesión (las
        // palabras clave son constantes) y los estados de pipeline del destino de verdad (Vulkan, D3D12 y Metal los atan al formato y al
        // MSAA del destino).
        private void WarmupDraw()
        {
            var world = _world!;
            if (EnsureTarget())
            {
                OutputChanged?.Invoke();
            }

            world.Root.SetActive(true);
            world.Store!.Step(SyntheticDistance, double.PositiveInfinity);
            world.Director!.Apply(_warmupFrame);
            world.Director.QueueInstancedDraws();
            world.Renderer!.Render(_target.Texture!);
            WarmupDraws++;
            world.Root.SetActive(false);
            _warmupFrame = default;
            _warmupFrameReady = false;
        }

        private void Fail(string what, Exception? error)
        {
            _logger.Log(LogLevel.Warning, Tag, what + ": el inmersivo sigue con el fondo provisorio", error);
            _readiness.Value = SceneReadiness.Failed;
            DropBuild();
        }

        // Suelta el iterador del armado CON su Dispose (QA-V6a-REN-08): un armado cortado en el tramo de las mallas corre el finally del
        // using del MeshUpload y suelta sus búferes nativos. Sin esto (_units = null) quedaban vivos hasta que los juntara el GC (en el
        // Editor, "Leak Detected" al recargar el dominio).
        private void DropBuild()
        {
            var units = _units;
            _units = null;
            units?.Dispose();
            _warmupFrame = default;
            _warmupFrameReady = false;
        }

        private void DisposeWorld()
        {
            // Un armado a medias (la escena demorada o la memoria baja en plena precarga): su mundo todavía no es _world, pero ya tiene
            // la cámara, la luz y lo que se creó hasta acá. Se desarma igual (B6a-19: si no, quedaba una segunda cámara de la escena).
            DropBuild();
            var partial = _builder?.World;
            if (partial != null && !ReferenceEquals(partial, _world))
            {
                partial.Dispose();
            }

            _builder = null;
            _world?.Dispose();
            _world = null;
            _scratch = null;
            _scratchFrame = default;
            _nextSession = null;
            _nextFrame = default;
            _lastStreamDistance = double.NaN;
            if (_readiness.Value != SceneReadiness.Failed)
            {
                _readiness.Value = SceneReadiness.NotLoaded;
            }
        }

        private void HookLowMemory()
        {
            if (_lowMemoryHooked)
            {
                return;
            }

            _lowMemoryHooked = true;
            Application.lowMemory += OnLowMemory;
        }

        // Application.lowMemory: el mundo se desarma si no hay un inmersivo abierto (se rearma en la próxima precarga).
        private void OnLowMemory()
        {
            if (_session == null && !_disposed)
            {
                DisposeWorld();
                _target.Release();
                OutputChanged?.Invoke();
            }
        }

        private QualityTierProfile ProfileFor(QualityTier tier, out bool owned)
        {
            var profile = _definition.Profile(tier);
            owned = profile == null;
            return profile != null ? profile : QualityTierProfile.CreateDefault(tier);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            if (_lowMemoryHooked)
            {
                Application.lowMemory -= OnLowMemory;
            }

            _session = null;
            DropBuild();
            var partial = _builder?.World;
            if (partial != null && !ReferenceEquals(partial, _world))
            {
                // Cerrada en plena precarga: el mundo a medias también se desarma.
                partial.Dispose();
            }

            _builder = null;
            _world?.Dispose();
            _world = null;
            _nextSession = null;
            _target.Dispose();
            MeshUpload.Destroy(_stamp);
            if (_ownsProfile)
            {
                MeshUpload.Destroy(_profile);
            }
        }

        // ── Controles de prueba (internal: las pruebas, el anfitrión de pruebas y el arnés; §3.1) ──

        // "Escena demorada" (AC-SCN-01-07, CAP-24, VID-11): el próximo Open encuentra el mundo sin armar y Ready no llega antes de
        // `seconds` desde el Open; el armado corre por tramos como en producción.
        internal void DelayReadinessForTest(double seconds)
        {
            _delayReadiness = seconds;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // -rowmate-scene-delay (solo desarrollo, 02-tech-design §4.5): la escena demorada en el build de desarrollo, como la de prueba.
        public void DelayReadinessForPerf(double seconds)
        {
            _delayReadiness = seconds;
        }
#endif

        // La región de la luna para la sesión siguiente (§0.4 de la spec).
        internal void ForceRegionForTest(string? region)
        {
            _regionForTest = region;
            _regionOverridden = true;
        }

        // Los degradés de pantalla apagados en la escena sola (DV-67).
        internal void SetScrimsForTest(bool on)
        {
            _scrims = on;
        }

        // Las estrellas prendidas o apagadas (la sonda de las estrellas detrás del astro).
        internal void SetStarsForTest(bool on)
        {
            _stars = on;
        }

        // El mismo cuadro sin el Volume de la hora ni el bloom (y con la ganancia de lo emisivo en 1): las sondas de color "antes del
        // ajuste de la hora" (AC-SCN-09-06; QA-G6aT-13).
        internal void RenderWithoutPostForTest(RenderTexture target)
        {
            if (_world?.Director != null && _session != null)
            {
                DrawFrame(_lastNow, true, target);
            }
        }

        // La sonda S6a-7: el número de cuadro pintado en la esquina de arriba a la izquierda de la RT de la escena.
        internal void StampFrameForTest(bool on)
        {
            _stampFrames = on;
        }

        internal long StampedFrameForTest => _stampCounter;

        internal bool AllocateInTickForTest { get; set; }

        // Un nivel para las pruebas de presupuesto (lo del asset de URP, con TierScope): rehace el mundo con ese perfil.
        internal void ForceTierForTest(QualityTier tier)
        {
            if (_ownsProfile)
            {
                MeshUpload.Destroy(_profile);
            }

            _profile = ProfileFor(tier, out _ownsProfile);
            _resolution = _profile.Resolution;
            DisposeWorld();
        }

        // El armado está en el tramo de las mallas (QA-V6a-REN-08) y Application.lowMemory a pedido (no se puede disparar desde código).
        internal bool BuildingMeshesForTest => _builder != null && _builder.UploadOpen;

        internal void SimulateLowMemoryForTest()
        {
            OnLowMemory();
        }

        // La distancia de la ventana de la orilla ya armada (QA-V6a-REN-04: la prueba de que el rebobinado todavía no terminó).
        internal double StreamDistanceForTest => _lastStreamDistance;

        internal void ForceDistanceForTest(double distance)
        {
            _session?.ForceDistanceForTest(distance);
        }

        internal void FreezeSceneTimeForTest(double? sceneTime)
        {
            _session?.FreezeSceneTimeForTest(sceneTime);
        }

        internal void ForceHourForTest(double? hour)
        {
            _session?.ForceHourForTest(hour);
        }

        internal void BreathingOffForTest(bool off)
        {
            _session?.BreathingOffForTest(off);
        }

        // El armado entero en el acto (las pruebas que no miden la precarga).
        internal void CompleteLoadingForTest(double now)
        {
            if (_readiness.Value == SceneReadiness.NotLoaded)
            {
                StartBuild();
            }

            var guard = 0;
            while (_readiness.Value == SceneReadiness.Loading && guard++ < 100000)
            {
                RunBuild(now, double.PositiveInfinity);
                if (_readiness.Value == SceneReadiness.Loading && (SceneShaderWarmup.AnythingCompiling || now < _readyNotBefore))
                {
                    break;
                }
            }
        }

        private void Stamp()
        {
            var rt = _target.Texture;
            if (rt == null)
            {
                return;
            }

            if (_stamp == null || _stamp.graphicsFormat != rt.graphicsFormat)
            {
                // Del formato de la RT (8 bits sRGB o media precisión en los niveles con HDR): CopyTexture no convierte.
                MeshUpload.Destroy(_stamp);
                _stamp = new Texture2D(8, 8, rt.graphicsFormat, UnityEngine.Experimental.Rendering.TextureCreationFlags.None)
                {
                    hideFlags = HideFlags.HideAndDontSave, name = "RowMate.SceneStamp"
                };
            }

            _stampCounter++;
            var color = new Color32((byte)(_stampCounter & 0xFF), (byte)((_stampCounter >> 8) & 0xFF), 0, 255);
            var pixels = _stampPixels ??= new Color32[64];
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }

            if (rt.graphicsFormat == UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_SRGB)
            {
                _stamp.SetPixels32(pixels);
            }
            else
            {
                // La RT de media precisión guarda valores lineales: el mismo color, decodificado (el panel lo lee igual).
                var linear = _stampLinear ??= new Color[64];
                var value = ((Color)color).linear;
                for (var i = 0; i < linear.Length; i++)
                {
                    linear[i] = value;
                }

                _stamp.SetPixels(linear);
            }

            _stamp.Apply(false, false);
            Graphics.CopyTexture(_stamp, 0, 0, 0, 0, 8, 8, rt, 0, 0, 0, rt.height - 8);
        }

        private Color32[]? _stampPixels;
        private Color[]? _stampLinear;
    }
}
