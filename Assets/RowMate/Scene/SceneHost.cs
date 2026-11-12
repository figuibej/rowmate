using System;
using System.Collections.Generic;
using System.Diagnostics;
using RowMate.Core.Common;
using RowMate.Core.Scene;
using RowMate.Core.Scene.Art;
using RowMate.Core.Scene.Water;
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

        // It. 7 (07-remero-palada/02-tech-design §3.2, §3.6): qué remero se arma (Auto = el binding de la app o el maniquí) y, para las
        // pruebas, un binding en lugar del de Resources.
        public RowerCharacterKind Character { get; set; } = RowerCharacterKind.Auto;

        public RowMate.Scene.Slots.RowerCharacterBinding? CharacterBinding { get; set; }

        // It. 6b (06b-agua-avanzada/02-tech-design §3.2, §5; DT6b-14, DT6b-24): el agua forzada (Auto = la regla de AC-6B-06) y si se permite
        // forzar (solo desarrollo, el Editor y las pruebas: el build normal nunca la fuerza); la falla de prueba a los N s de una sesión
        // (-rowmate-water-fail) y la carga de GPU de prueba (-rowmate-perf-gpu-load, ms aproximados por cuadro).
        public RowMate.Core.Scene.Water.WaterForce Water { get; set; } = RowMate.Core.Scene.Water.WaterForce.Auto;

        public bool WaterForceAllowed { get; set; }

        public double WaterFailAfterSeconds { get; set; } = double.NaN;

        public double GpuLoadMs { get; set; }
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
        // It. 6b (#1; 06b-agua-avanzada/02-tech-design §2.1): primero el preámbulo de primera vez del proceso (0-2, pesadas: la cámara y la
        // luz, el primer pedido de URP a 64² y la sesión de usar y tirar de Core), después la sesión preparada (3), el mundo (4-11, como en
        // la 6a, con el post al tamaño de la sesión pesado y el dibujo de calentamiento partido) y, después de Ready, el agua avanzada (13).
        internal const int PhaseCamera = 0, PhaseFirstUrp = 1, PhaseCoreJit = 2, PhaseNextSession = 3, PhaseWorld = 4, PhaseVariants = 5,
            PhaseShore920 = 6, PhaseSynthetic = 7, PhaseTarget = 8, PhasePost = 9, PhaseWarmupDraw = 10, PhaseSessionShore = 11, PhaseReady = 12,
            PhaseWater = 13;

        private static readonly string[] PhaseNames =
        {
            "preámbulo: cámara y luz", "preámbulo: primer pedido de URP", "preámbulo: JIT de Core", "sesión preparada", "mundo", "variantes",
            "orilla de 920 m", "Tick sintético", "RT de la sesión", "post de URP", "dibujo de calentamiento", "orilla de la sesión", "Ready",
            "agua avanzada",
        };

        // Los sub-dibujos del calentamiento (cada uno liviano, ≤ 4 ms): cielo y horizonte; terreno; la orilla y el relleno instanciados;
        // objetos del kit e instancias; bote, remero y remos; agua.
        internal const int SubDrawGroups = 6;

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
        private readonly double[] _worstByPhase = new double[PhaseNames.Length];

        public double WorstUnitMsOf(int phase) => phase >= 0 && phase < _worstByPhase.Length ? _worstByPhase[phase] : 0;

        public static string PhaseName(int phase) => phase >= 0 && phase < PhaseNames.Length ? PhaseNames[phase] : "";

        public static int PhaseCount => PhaseNames.Length;

        // La CPU de la escena del último cuadro sin el dibujo de URP (ms; el marcador RowMate.Scene.Update; QA-V6a-REN-07).
        public double LastCpuMs { get; private set; }

        // It. 8 (la causa de SceneBudgetTests en Alto): la parte de LastCpuMs que fue armar la orilla (SegmentStore.Step) y la que fue la
        // sesión de Core (Tick). El resto es aplicar el cuadro y las matrices.
        public double LastStreamMs { get; private set; }

        public double LastTickMs { get; private set; }

        private readonly Stopwatch _partWatch = new Stopwatch();

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
            if (_viewportPinned || widthPx <= 0 || heightPx <= 0 || !(density > 0))
            {
                return;
            }

            _widthPx = widthPx;
            _heightPx = heightPx;
            _density = density;
        }

        public void Preload()
        {
            if (_disposed)
            {
                return;
            }

            if (_readiness.Value != SceneReadiness.NotLoaded)
            {
                // It. 8 (§3.2): el escenario elegido cambió en reposo: el kit nuevo por tramos (la base común no se rehace).
                if (_session == null && _world?.Director != null && _phase > PhaseWorld && _world.Environment != SelectedEnvironment)
                {
                    SwitchKit(SelectedEnvironment);
                }

                return;
            }

            StartBuild();
        }

        // It. 8 (AC-SCN-21-05): el escenario elegido (SceneEnvironmentChoice; el control de prueba lo pisa).
        public EnvironmentId SelectedEnvironment => _environmentForTest ?? _app.SceneEnvironment.Selected.Value;

        // El escenario del kit armado (el lago hasta que haya mundo).
        public EnvironmentId KitEnvironment => _world?.Environment ?? EnvironmentId.Lake;

        // Las veces que se cambió de kit (pruebas y [PERF]).
        public int KitSwitches { get; private set; }

        private EnvironmentId? _environmentForTest;

        // Descarta el kit del escenario anterior y arma el nuevo desde la orilla de 920 m (fase 2): los Tick sintéticos, el dibujo de
        // calentamiento y la orilla de la sesión vuelven a correr con el escenario nuevo antes de Ready (§3.2; DA8-16).
        private void SwitchKit(EnvironmentId id)
        {
            var world = _world!;
            world.SetEnvironment(id);
            ApplyProfile(world);
            KitSwitches++;
            _scratch = null;
            _scratchFrame = default;
            _syntheticStep = 0;
            _nextSession = null;
            _nextFrame = default;
            _warmupFrame = default;
            _warmupFrameReady = false;
            _subDraw = 0;
            _postDone = false;
            _lastStreamDistance = double.NaN;
            WorstUnitMs = 0;
            WorstUnitPhase = "";
            WorstUnitStep = "";
            _phase = PhaseShore920;
            _readiness.Value = SceneReadiness.Loading;
        }

        // Lo que tardó el último Open (ms; [PERF] de la entrada temprana: el cuadro del toque).
        public double LastOpenMs { get; private set; }

        public void Open(double now)
        {
            if (_disposed)
            {
                return;
            }

            _partWatch.Restart();
            OpenCore(now);
            LastOpenMs = _partWatch.Elapsed.TotalMilliseconds;
        }

        private void OpenCore(double now)
        {
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
            else if (_world?.Director != null && _phase > PhaseWorld && _world.Environment != SelectedEnvironment)
            {
                // El escenario cambió y el kit no llegó a armarse en reposo: la escena demorada hasta que esté (AC-SCN-01-07).
                SwitchKit(SelectedEnvironment);
            }

            WorstWarmupMs = 0;
            _closing = false;
            _paused = false;
            _veil = 0;
            if (_nextSession == null && _readiness.Value == SceneReadiness.Loading && _phase <= PhaseCoreJit)
            {
                // QA-V6b-01 (AC-6B-02): el toque llegó antes del preámbulo y no hay sesión preparada. Armar la sesión, su Open y su primer
                // Tick (el JIT de Core por primera vez: 4,6 + 11,9 ms medidos en el build) iría en el cuadro del toque; va al negro de la
                // entrada (RunPendingHeavyWork, después del preámbulo), con la hora del toque. Hasta ahí no hay nada que dibujar: la escena
                // no está lista.
                _pendingOpenAt = now;
                _lastNow = now;
                return;
            }

            OpenSession(now);
        }

        // El toque sin sesión preparada antes del preámbulo (NaN = ninguno).
        private double _pendingOpenAt = double.NaN;

        // ¿Hay un Open pendiente para el negro de la entrada? (las pruebas y [PERF]).
        public bool OpenPending => !double.IsNaN(_pendingOpenAt);

        // El Open pendiente: en el negro (RunPendingHeavyWork) o, si el armado ya pasó el preámbulo por otro camino (el arnés de pruebas,
        // con el tramo sin tope), en el primer Tick.
        private void OpenPendingSession()
        {
            if (double.IsNaN(_pendingOpenAt))
            {
                return;
            }

            var at = _pendingOpenAt;
            _pendingOpenAt = double.NaN;
            OpenSession(at);
        }

        private void OpenSession(double now)
        {
            var region = _regionOverridden ? _regionForTest : _options.RegionCode;
            var environment = SelectedEnvironment;
            if (_nextSession != null && string.Equals(_nextRegion, region, StringComparison.Ordinal) && _nextEnvironment == environment)
            {
                // La que se armó en reposo (QA-V6a-REN-02): en el cuadro del toque, sin armar la sesión ni sus arreglos.
                _session = _nextSession;
                _frame = _nextFrame;
            }
            else
            {
                _session = new SceneSession(EnvironmentCatalog.Create(environment), new SceneSessionOptions { RegionCode = region });
                _frame = SceneFrame.Create();
            }

            _nextSession = null;
            _nextFrame = default;
            ConfigureRower(_session);
            _session.Open(now);
            _lastNow = now;
            // It. 6b: el agua de la sesión arranca sin fundido (la primera vez que se dibuja, detrás del negro) y el reflejo sin bajar.
            _waterFadeStarted = false;
            _sessionOpenedAt = now;
            _failInjected = false;
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
            _pendingOpenAt = double.NaN;
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
            ConfigureRower(_session);
            ConfigureRower(_nextSession);
            if (_world?.Director?.WaterMarks != null)
            {
                _world.Director.WaterMarks.BoilsEnabled = tier != QualityTier.Low;
            }
            if (_world?.Director != null)
            {
                ApplyProfile(_world);
            }

            // It. 6b (§5): el agua avanzada cambia de variante (las tres están en la colección); la malla de Bajo, en la próxima sesión.
            _world?.AdvancedWater?.SetTier(tier);

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
            if (director.Kit != null)
            {
                director.Kit.Shadows = _profile.Shadows;
            }

            if (director.Glow != null)
            {
                director.Glow.Beam = _profile.Tier != QualityTier.Low;
            }
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
            Tick(now, warmupSliceMs, AnyFrame);
        }

        // It. 6b (DT6b-25): con el cuadro de la precarga (SceneBridge lo arma desde el Input System): una unidad pesada solo corre en un
        // cuadro que lo permite. Sin cuadro (las pruebas y el arnés), cualquier unidad.
        public void Tick(double now, double warmupSliceMs, in WarmupFrame frame)
        {
            if (_disposed)
            {
                return;
            }

            _warmupFrameState = frame;
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

        private static readonly WarmupFrame AnyFrame = new WarmupFrame(true, true, false);
        private WarmupFrame _warmupFrameState = AnyFrame;

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

            if (OpenPending && (_phase > PhaseCoreJit || _readiness.Value != SceneReadiness.Loading))
            {
                // El preámbulo terminó por otro camino que el negro (el arnés, con el tramo sin tope): el Open pendiente, acá.
                OpenPendingSession();
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
                    else if (WaterWarmupPending)
                    {
                        WaterWarmupUnit(now);
                    }
                }

                UpdateGlass(now, false);
                LastCpuMs = 0;
                return;
            }

            _cpuWatch.Restart();
            UpdateMarker.Begin();
            var inputs = Inputs(now);
            UpdatePause(session, now);
            LastStreamMs = 0;
            _partWatch.Restart();
            if (_options.FixedSteps)
            {
                session.Advance(inputs, Math.Max(0, now - _lastNow), ref _frame);
            }
            else
            {
                session.Tick(inputs, ref _frame);
            }

            LastTickMs = _partWatch.Elapsed.TotalMilliseconds;

            _lastNow = now;
            // It. 6b: las unidades del agua avanzada también con la sesión abierta (livianas, con tramo: la avanzada llega con su fundido).
            if (warmupSliceMs > 0 && WaterWarmupPending)
            {
                WaterWarmupUnit(now);
            }

            InjectWaterFailure(now);
            var showing = IsShowing && _world?.Director != null;
            _frame.Draw = showing;
            if (!showing)
            {
                UpdateGlass(now, false);
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

            _partWatch.Restart();
            store.Step(_frame.Distance, _options.SynchronousStreaming || jump ? double.PositiveInfinity : _options.StreamingBudgetMs);
            if (target == null)
            {
                LastStreamMs = _partWatch.Elapsed.TotalMilliseconds;
            }

            _lastStreamDistance = _frame.Distance;
            var director = world.Director!;
            director.HideBoat = _thumbnail;
            if (director.Kit != null)
            {
                director.Kit.MaxRelZ = _thumbnail ? ThumbnailRule.NearRange : double.PositiveInfinity;
            }

            if (director.Foam != null)
            {
                director.Foam.MaxRelZ = _thumbnail ? ThumbnailRule.NearRange : double.PositiveInfinity;
            }

            if (_thumbnail)
            {
                // ThumbnailRule.Filter: solo lo que está a menos de 150 m (thumbnail_painter.dart:33-36).
                var kept = _thumbnailProps ??= new ShorePropBuffer(_frame.VisibleProps.Capacity);
                ThumbnailRule.Filter(_frame.VisibleProps.Items, kept);
                _frame.VisibleProps.Clear();
                for (var i = 0; i < kept.Count; i++)
                {
                    _frame.VisibleProps.Add(kept.Items[i]);
                }
            }

            director.Overlay.Enabled = _scrims;
            director.Sky.StarsEnabled = _stars;
            director.Lighting.EmissiveGain = withoutPost ? 1 : SkyArt.EmissiveGain;
            UpdateWater(now);
            director.Apply(_frame);
            if (_sideCamera)
            {
                ApplySideCamera(world);
            }
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
                RenderReflection(world);
                world.Renderer!.Render(target ?? _target.Texture!);
            }
            finally
            {
                data.renderPostProcessing = post;
                director.Lighting.EmissiveGain = SkyArt.EmissiveGain;
            }

            if (target == null)
            {
                UpdateGlass(now, true);
                GpuLoad();
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

            _builder = new SceneWorldBuilder(_definition, _profile, _slotLog, _logger)
            {
                Character = _options.Character,
                Binding = _options.CharacterBinding,
                Environment = SelectedEnvironment,
            };
            _builderProfile = _profile;
            _units = _builder.Build();
            _phase = PhaseCamera;
            WorstUnitMs = 0;
            WorstUnitPhase = "";
            WorstUnitStep = "";
            WorstSubDrawMs = 0;
            WorstLightUnitMs = 0;
            WorstLightUnitPhase = "";
            Array.Clear(_worstByPhase, 0, _worstByPhase.Length);
            _syntheticStep = 0;
            _coreJitStep = 0;
            _subDraw = 0;
            _postDone = false;
            _jitScratch = null;
            _jitFrame = default;
            _groupRenderers = null;
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
                           && (double.IsPositiveInfinity(sliceMs)
                               || SceneWarmupPolicy.RunAnother(sliceMs, _watch.Elapsed.TotalMilliseconds, units, IsHeavyPhase(_phase), _warmupFrameState)))
                    {
                        // QA-V6a-REN-06: cada unidad se mide (la peor, con su fase, va a [PERF] y a SET).
                        var heavy = IsHeavyPhase(_phase);
                        var ran = MeasuredUnit(now);
                        if (ran && heavy)
                        {
                            NoteHeavy(false);
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

        // Una unidad medida (QA-V6a-REN-06): la peor de cada fase y la peor del armado, con su paso del mundo.
        private bool MeasuredUnit(double now)
        {
            var phase = _phase;
            _unitWatch.Restart();
            var ran = RunUnit(now);
            NoteUnit(phase, ran, _unitWatch.Elapsed.TotalMilliseconds);
            return ran;
        }

        private void NoteUnit(int phase, bool ran, double unitMs)
        {
            if (!ran)
            {
                return;
            }

            if (phase < _worstByPhase.Length && unitMs > _worstByPhase[phase])
            {
                _worstByPhase[phase] = unitMs;
            }

            if (unitMs > WorstUnitMs)
            {
                WorstUnitMs = unitMs;
                WorstUnitPhase = PhaseNames[Math.Min(phase, PhaseNames.Length - 1)];
                WorstUnitStep = (phase == PhaseCamera || phase == PhaseWorld) && _builder != null ? _builder.Step : "";
            }

            if (phase == PhaseWarmupDraw && unitMs > WorstSubDrawMs)
            {
                WorstSubDrawMs = unitMs;
            }

            if (!IsHeavyPhase(phase) && unitMs > WorstLightUnitMs && phase != PhaseCamera)
            {
                WorstLightUnitMs = unitMs;
                WorstLightUnitPhase = PhaseNames[Math.Min(phase, PhaseNames.Length - 1)];
            }
        }

        // It. 6b (#1; DT6b-25): el preámbulo (0-2) y el post al tamaño de la sesión son pesados: solo corren en un cuadro quieto, o en el
        // negro de la entrada (RunPendingHeavyWork).
        private bool IsHeavyPhase(int phase)
        {
            return phase <= PhaseCoreJit || phase == PhasePost && !_postDone;
        }

        private void NoteHeavy(bool blackFrame)
        {
            LastHeavyFrame = Time.frameCount;
            HeavyUnits++;
            if (_session != null && !blackFrame)
            {
                HeavyUnitsWhileOpen++;
            }
        }

        // El cuadro (Time.frameCount) de la última unidad pesada, cuántas corrieron y cuántas con el inmersivo abierto fuera del negro de
        // la entrada (EarlyEntryTests: 0 con el tramo del player).
        public int LastHeavyFrame { get; private set; } = -1;

        public long HeavyUnits { get; private set; }

        public long HeavyUnitsWhileOpen { get; private set; }

        // El sub-dibujo de calentamiento más caro y la unidad liviana más cara desde el último armado (ms; VisibleUnitMaxMs = 4).
        public double WorstSubDrawMs { get; private set; }

        public double WorstLightUnitMs { get; private set; }

        public string WorstLightUnitPhase { get; private set; } = "";

        // Lo que costó el último RunPendingHeavyWork (ms) y cuántas veces corrió algo.
        public double LastBlackFrameHeavyMs { get; private set; }

        public long BlackFrameHeavyRuns { get; private set; }

        // La fase del armado (las pruebas y [PERF]).
        public int BuildPhase => _phase;

        // It. 6b (#1, AC-6B-01; 02-tech-design §2.1 (3)): el cuadro del armado del HUD es negro y quieto: ahí corren TODAS las unidades
        // pesadas que quedaron pendientes (el preámbulo, si el toque llegó antes, y el post al tamaño de la sesión, fuera del orden de las
        // fases: no depende del mundo, G6b-18). Devuelve lo que tardó (ms): la entrada corre su reloj eso mismo (el negro dura un poco más,
        // sin saltos).
        public double RunPendingHeavyWork(double now)
        {
            if (_disposed || _readiness.Value != SceneReadiness.Loading)
            {
                OpenPendingSession();
                LastBlackFrameHeavyMs = 0;
                return 0;
            }

            _watch.Restart();
            var ran = false;
            try
            {
                var guard = 0;
                while (_readiness.Value == SceneReadiness.Loading && _phase <= PhaseCoreJit && guard++ < 64)
                {
                    if (!MeasuredUnit(now))
                    {
                        break;
                    }

                    ran = true;
                    NoteHeavy(true);
                }

                // QA-V6b-01: el Open que llegó antes del preámbulo, ya con el JIT de Core hecho, y su primer Tick (al día con el toque).
                if (OpenPending)
                {
                    OpenPendingSession();
                    var opened = _session;
                    if (opened != null)
                    {
                        // Como TickCore: con pasos fijos (el arnés), Advance desde el toque; si no, Tick con la hora de ahora.
                        if (_options.FixedSteps)
                        {
                            opened.Advance(Inputs(now), Math.Max(0, now - _lastNow), ref _frame);
                        }
                        else
                        {
                            opened.Tick(Inputs(now), ref _frame);
                        }

                        _lastNow = now;
                    }

                    ran = true;
                }

                var partial = _world ?? _builder?.World;
                if (_readiness.Value == SceneReadiness.Loading && !_postDone && _phase > PhaseCoreJit && partial?.Renderer != null
                    && !SceneShaderWarmup.AnythingCompiling)
                {
                    _unitWatch.Restart();
                    WarmupPostDraw(false);
                    _postDone = true;
                    NoteUnit(PhasePost, true, _unitWatch.Elapsed.TotalMilliseconds);
                    ran = true;
                    NoteHeavy(true);
                }
            }
            catch (Exception error)
            {
                Fail("el armado de la escena falló", error);
            }

            LastBlackFrameHeavyMs = ran ? _watch.Elapsed.TotalMilliseconds : 0;
            if (ran)
            {
                BlackFrameHeavyRuns++;
            }

            return LastBlackFrameHeavyMs;
        }

        // Una unidad. false = no hay nada que hacer en este cuadro (esperando la compilación o la demora de prueba).
        // Fases (PhaseNames; It. 6b, 02-tech-design §2.1): 0-2 el preámbulo (la cámara y la luz, que son las dos primeras unidades del
        // mundo; el primer pedido de URP a 64²; la sesión de Core de usar y tirar en 5 unidades), 3 la sesión preparada, 4 el resto del mundo,
        // 5 las variantes, 6 la orilla de 920 m, 7 los Tick sintéticos, 8 la RT de la sesión, 9 el post de URP del tamaño de la sesión
        // (pesado; si ya corrió en el negro, se salta), 10 el dibujo de calentamiento (su cuadro y 5 sub-dibujos), 11 la orilla donde empieza
        // a verse (la de la sesión abierta o la de 0 m), 12 Ready (no antes de la demora de prueba).
        private bool RunUnit(double now)
        {
            switch (_phase)
            {
                case PhaseCamera:
                    if (_units!.MoveNext())
                    {
                        if (_builder!.World?.Lighting != null)
                        {
                            _phase = PhaseFirstUrp;
                        }

                        return true;
                    }

                    _phase = PhaseFirstUrp;
                    return true;
                case PhaseFirstUrp:
                    FirstUrpRequest();
                    _phase = PhaseCoreJit;
                    return true;
                case PhaseCoreJit:
                    if (CoreJitStep(_coreJitStep++))
                    {
                        _coreJitStep = 0;
                        _phase = PhaseNextSession;
                    }

                    return true;
                case PhaseNextSession:
                    if (_nextSession == null && _session == null)
                    {
                        PrepareNextSession(SelectedEnvironment);
                    }

                    _phase = PhaseWorld;
                    return true;
                case PhaseWorld:
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

                            // It. 7: el remero que quedó armado (sus largos) y el nivel, a las sesiones que ya existen.
                            ConfigureRower(_session);
                            ConfigureRower(_nextSession);
                            _phase = PhaseVariants;
                            if (_world.Environment != SelectedEnvironment)
                            {
                                // It. 8: el escenario cambió mientras se armaba el mundo: el kit nuevo antes de seguir (sin orilla armada).
                                _world.SetEnvironment(SelectedEnvironment);
                                ApplyProfile(_world);
                            }

                            if (_session != null && _session.Environment.Id != _world.Environment)
                            {
                                // La sesión abierta con otro escenario (el toque llegó antes que el mundo): la del kit armado.
                                _session = new SceneSession(EnvironmentCatalog.Create(_world.Environment),
                                    new SceneSessionOptions { RegionCode = _regionOverridden ? _regionForTest : _options.RegionCode });
                                _frame = SceneFrame.Create();
                                ConfigureRower(_session);
                                _session.Open(_lastNow);
                            }
                        }

                        return true;
                    }

                    _phase = PhaseVariants;
                    return true;
                case PhaseVariants:
                    if (_warmup!.Step())
                    {
                        _phase = PhaseShore920;
                        return true;
                    }

                    // En el Editor, la espera de la compilación asíncrona: nada más en este cuadro. En el player, otra variante.
                    return !Application.isEditor;
                case PhaseShore920:
                    return StreamUnit(SyntheticDistance, PhaseSynthetic);
                case PhaseSynthetic:
                    if (SyntheticTick(_syntheticStep++))
                    {
                        _phase = PhaseTarget;
                    }

                    return true;
                case PhaseTarget:
                    // QA-V6a-REN-05 (§3.1: "Ready: la RT existe"): la RT de la sesión, con la ventana y el formato del nivel, en su unidad.
                    if (EnsureTarget())
                    {
                        OutputChanged?.Invoke();
                    }

                    _phase = PhasePost;
                    return true;
                case PhasePost:
                    if (_postDone)
                    {
                        // Ya corrió en el negro de la entrada (RunPendingHeavyWork).
                        _phase = PhaseWarmupDraw;
                        return true;
                    }

                    if (SceneShaderWarmup.AnythingCompiling)
                    {
                        return false;
                    }

                    // QA-V6a-REN-01 y -06: el post de URP (UberPost, Bloom, el LUT) y sus intermedios del tamaño y el formato de la sesión,
                    // sin la escena (la máscara de la cámara en 0), en una unidad propia antes del dibujo de calentamiento. It. 6b: pesada.
                    WarmupPostDraw(false);
                    _postDone = true;
                    _phase = PhaseWarmupDraw;
                    return true;
                case PhaseWarmupDraw:
                    if (SceneShaderWarmup.AnythingCompiling)
                    {
                        return false;
                    }

                    if (!_warmupFrameReady)
                    {
                        PrepareWarmupFrame();
                        _subDraw = 0;
                        return true;
                    }

                    // It. 6b (#1): el dibujo de calentamiento partido en sub-dibujos por grupo de renderers, cada uno liviano.
                    WarmupSubDraw(_subDraw++);
                    if (_subDraw >= SubDrawGroups)
                    {
                        FinishWarmupDraw();
                        _phase = PhaseSessionShore;
                    }

                    return true;
                case PhaseSessionShore:
                    // QA-V6a-REN-04 (B): la orilla donde va a empezar a verse la escena: la de la sesión abierta (la escena demorada, con el
                    // bote ya lejos de 0 m) o la de 0 m (la próxima sesión).
                    return StreamUnit(_session != null ? _frame.Distance : 0, PhaseReady);
                default:
                    if (now < _readyNotBefore)
                    {
                        return false;
                    }

                    if (_session != null && Math.Abs(_frame.Distance - _lastStreamDistance) > ShoreEnvironment.SegmentLength / 2)
                    {
                        // La sesión siguió avanzando mientras se esperaba: la orilla otra vez al día antes de Ready.
                        _phase = PhaseSessionShore;
                        return true;
                    }

                    _readyNotBefore = double.NegativeInfinity;
                    _readiness.Value = SceneReadiness.Ready;
                    return false;
            }
        }

        // (0b) El primer pedido de URP del proceso (02-tech-design §2.1): la cámara del mundo a medio armar, con la máscara en 0, a una RT de
        // 64² del formato de la sesión (el código de URP, el post y sus estados por primera vez), descartada al terminar.
        private void FirstUrpRequest()
        {
            var partial = _world ?? _builder?.World;
            if (partial?.Renderer == null)
            {
                return;
            }

            RenderMaskZero(partial, true);
        }

        // (0c) La sesión de Core de usar y tirar SIN mundo (02-tech-design §2.1; G6b-18: en IL2CPP es la inicialización de metadatos y de
        // estáticos): Open y Tick a las 12 h, a las 22 h con el velo, un paso del vuelo, uno de la salida y uno de la toma aérea, en 5
        // unidades. true al terminar.
        private bool CoreJitStep(int step)
        {
            var scratch = _jitScratch ??= new SceneSession(EnvironmentCatalog.Create(SelectedEnvironment), new SceneSessionOptions());
            if (_jitFrame.Stars == null)
            {
                _jitFrame = SceneFrame.Create();
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
            scratch.Tick(inputs, ref _jitFrame);
            scratch.Segments.VisibleProps(SyntheticDistance, _jitFrame.VisibleProps);
            if (step < SceneWarmupPolicy.CoreJitPreambleUnits - 1)
            {
                return false;
            }

            _jitScratch = null;
            _jitFrame = default;
            return true;
        }

        private SceneSession? _jitScratch;
        private SceneFrame _jitFrame;
        private int _coreJitStep;
        private int _subDraw;
        private bool _postDone;

        // La sesión siguiente, armada en reposo (QA-V6a-REN-02): Open la toma en el cuadro del toque en lugar de armar la sesión y sus
        // arreglos ahí. Con otra región (el control de prueba), Open arma una nueva. It. 6b (#1): también en la fase 3 del armado, con
        // el escenario elegido, antes del mundo.
        private void PrepareNextSession()
        {
            PrepareNextSession(KitEnvironment);
        }

        private void PrepareNextSession(EnvironmentId environment)
        {
            var region = _regionOverridden ? _regionForTest : _options.RegionCode;
            _nextEnvironment = environment;
            _nextSession = new SceneSession(EnvironmentCatalog.Create(_nextEnvironment), new SceneSessionOptions { RegionCode = region });
            _nextRegion = region;
            _nextFrame = SceneFrame.Create();
            ConfigureRower(_nextSession);
        }

        private SceneSession? _nextSession;
        private string? _nextRegion;
        private EnvironmentId _nextEnvironment;
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
            var scratch = _scratch ??= new SceneSession(EnvironmentCatalog.Create(KitEnvironment), new SceneSessionOptions());
            var frame = _scratchFrame;
            if (frame.Stars == null)
            {
                frame = SceneFrame.Create();
            }

            var now = 1000.0 + step;
            if (step == 0)
            {
                ConfigureRower(scratch);
            }

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
            var scratch = new SceneSession(EnvironmentCatalog.Create(KitEnvironment), new SceneSessionOptions());
            var frame = SceneFrame.Create();
            frame.WarmUpAll = true;
            ConfigureRower(scratch);
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
            RenderMaskZero((_world ?? _builder?.World)!, small);
        }

        private void RenderMaskZero(SceneWorld world, bool small)
        {
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
        // It. 6b (#1; 02-tech-design §2.1 (4)): el dibujo partido en sub-dibujos por grupo de renderers (SceneDirector.WarmupGroupOf), cada
        // uno en su unidad, liviana. El primero aplica el cuadro; cada uno deja a la vista solo los renderers y los dibujos instanciados de
        // su grupo, dibuja en la RT de la sesión y vuelve a dejar los renderers como estaban. Entre todos tocan las mismas variantes y
        // estados de pipeline que el dibujo entero de la 6a. WarmupDraws cuenta el dibujo entero (uno), SubDraws los sub-dibujos.
        private void WarmupSubDraw(int group)
        {
            var world = _world!;
            if (EnsureTarget())
            {
                OutputChanged?.Invoke();
            }

            world.Root.SetActive(true);
            var director = world.Director!;
            if (group == 0)
            {
                world.Store!.Step(SyntheticDistance, double.PositiveInfinity);
                director.Apply(_warmupFrame);
            }

            var renderers = _groupRenderers ??= world.Root.GetComponentsInChildren<Renderer>(true);
            if (_groupEnabled == null || _groupEnabled.Length != renderers.Length)
            {
                _groupEnabled = new bool[renderers.Length];
            }

            for (var i = 0; i < renderers.Length; i++)
            {
                var r = renderers[i];
                _groupEnabled[i] = r.enabled;
                r.enabled = r.enabled && director.WarmupGroupOf(r) == group;
            }

            try
            {
                director.QueueInstancedDraws(group);
                world.Renderer!.Render(_target.Texture!);
                SubDraws++;
            }
            finally
            {
                for (var i = 0; i < renderers.Length; i++)
                {
                    renderers[i].enabled = _groupEnabled[i];
                }

                world.Root.SetActive(false);
            }
        }

        private void FinishWarmupDraw()
        {
            WarmupDraws++;
            _warmupFrame = default;
            _warmupFrameReady = false;
            _groupRenderers = null;
            _subDraw = 0;
        }

        private Renderer[]? _groupRenderers;
        private bool[]? _groupEnabled;

        // Los sub-dibujos de calentamiento (el dibujo de la 6a son SubDrawGroups).
        public long SubDraws { get; private set; }

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
            _groupRenderers = null;
            _subDraw = 0;
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
            ResetWaterWorld();
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
            ReleaseWaterProbe();
            MeshUpload.Destroy(_stamp);
            if (_ownsProfile)
            {
                MeshUpload.Destroy(_profile);
            }
        }

        // ── It. 6b: el agua avanzada, la caída a la degradada y el vidrio (06b-agua-avanzada/02-tech-design §5, §4.4) ──

        // El agua de la sesión de la APP (DT6b-01): un equipo sin soporte, una falla o el último escalón térmico la dejan degradada hasta
        // reiniciar, sin reintento; una sola línea del registro (DT6b-13). Vive más que el mundo y que las sesiones de escena.
        private readonly WaterAvailability _waterAvailability = new WaterAvailability();
        private WaterFade _waterFade;
        private bool _waterFadeStarted;
        private bool _advancedReady;
        private bool _waterWarmupDone;
        private int _waterStep;
        private double _advancedNotBefore = double.NegativeInfinity;
        private double _sessionOpenedAt;
        private bool _failInjected;
        private double _reflectionLoweredAt = double.NaN;
        private double _glassWeight;
        private double _glassFrom;
        private double _glassStart = double.NegativeInfinity;
        private bool _glassTarget;
        private int _glassGeneration = -1;
        private RenderTexture? _waterProbeTarget;
        private bool _forceUnsupportedForTest;
        private readonly Stopwatch _waterWatch = new Stopwatch();

        public const double GlassFadeSeconds = 0.2;

        public WaterAvailability WaterAvailability => _waterAvailability;

        // La elección del último cuadro dibujado (Choice: la regla; Showing: la que se ve) y si la avanzada ya está lista.
        public WaterChoice WaterChoice => _frame.Water.Choice;

        public bool AdvancedWaterReady => _advancedReady;

        public double DegradedBlend => _frame.Water.DegradedBlend;

        // Lo que se dibujó de verdad en el último cuadro (el renderer avanzado prendido) y las líneas del registro del agua.
        public bool ShowingAdvancedWater => _world?.Director?.WaterSurfaces?.ShowingAdvanced == true;

        public int WaterLogLines { get; private set; }

        public double WorstWaterUnitMs => _worstByPhase[PhaseWater];

        // El reflejo planar se dibujó en el último cuadro (Alto) y su peso (LowerReflection lo baja a 0 en 0,6 s).
        public bool ReflectionDrawn { get; private set; }

        public double ReflectionMix { get; private set; } = 1;

        // ¿Hay unidades del agua avanzada pendientes? (después de Ready, fase 13).
        public bool WaterWarmupPending => _readiness.Value == SceneReadiness.Ready && !_waterWarmupDone && _world?.AdvancedWater != null;

        // El vidrio (IImmersiveScene): el compuesto desenfocado, su peso con el fundido de 200 ms y el aviso de cambio.
        public Texture? GlassTexture => _world?.Blur?.Output;

        public double GlassWeight => _glassWeight;

        public event Action? GlassChanged;

        public bool GlassInUse { get; set; }

        public long BlurRuns => _world?.Blur?.Runs ?? 0;

        // DT6b-11: el vidrio corre en Alto y Medio; en Bajo, el respaldo plano.
        public bool GlassTier => _profile.Tier != QualityTier.Low;

        // La elección del cuadro y el fundido (antes de aplicar el cuadro): la regla (WaterModeSelector) con lo pegado de la app; la
        // primera vez de cada sesión (detrás del negro) y en la miniatura, en el acto; después, con el fundido de 0,6 s (en el acto con
        // reducir movimiento). Si el agua avanzada no pudo dibujar, el corte es en el acto (DT6b-12).
        private void UpdateWater(double now)
        {
            var world = _world!;
            var controller = world.Director!.WaterSurfaces;
            if (controller == null)
            {
                return;
            }

            if (controller.Failed)
            {
                controller.Failed = false;
                ReportWaterFailure(controller.FailureReason ?? WaterModeSelector.FailureReason);
                _waterFade.Snap(false);
            }

            var hasAdvanced = world.AdvancedWater != null;
            var ready = _advancedReady && hasAdvanced && now >= _advancedNotBefore && !_advancedHeld;
            var choice = WaterModeSelector.Choose(_waterAvailability.Inputs(_profile.Tier, _options.Water, _options.WaterForceAllowed, _thumbnail, ready));
            var wantAdvanced = choice.Showing == WaterKind.Advanced;
            if (!_waterFadeStarted || _thumbnail)
            {
                _waterFade.Snap(wantAdvanced);
                _waterFadeStarted = true;
            }
            else if (wantAdvanced != _waterFade.TowardAdvanced)
            {
                _waterFade.Start(now, wantAdvanced, ReduceMotion);
            }

            controller.AdvancedAvailable = hasAdvanced && _advancedReady;
            _frame.Water.Choice = choice;
            _frame.Water.DegradedBlend = _waterFade.DegradedBlend(now);
            if (world.AdvancedWater != null)
            {
                ReflectionMix = double.IsNaN(_reflectionLoweredAt) ? 1 : Math.Max(0, 1 - (now - _reflectionLoweredAt) / WaterFade.Seconds);
                world.AdvancedWater.SetReflectionMix(ReflectionMix);
            }

            var marks = world.Director.WaterMarks;
            if (marks != null)
            {
                // Los micro-anillos y los charcos de #12 van con la avanzada (los anillos, en Alto y Medio: §4.1); con la degradada, los de la 7.
                marks.RingsEnabled = _frame.Water.DegradedBlend < 0.5 && _profile.Tier != QualityTier.Low;
            }
        }

        // Una falla en medio de la sesión (excepción del agua o del espejo, la RT del reflejo perdida, -rowmate-water-fail): pegada por la
        // app, una sola línea y el fundido a la degradada (el reloj, el HUD, el guardado y la sesión de escena no se tocan).
        public void ReportWaterFailure(string reason)
        {
            if (_waterAvailability.ReportFailure(reason))
            {
                LogWaterFallback(reason);
            }
        }

        // DegradeWater de la adaptación térmica (DV-152): igual que una falla, con su motivo.
        public void DegradeWater()
        {
            if (_waterAvailability.ReportThermal())
            {
                LogWaterFallback(WaterModeSelector.ThermalReason);
            }
        }

        // LowerReflection de la adaptación (§5): el peso del planar (Alto) o de la silueta (Medio) baja a 0 en 0,6 s.
        public void LowerReflection()
        {
            if (double.IsNaN(_reflectionLoweredAt))
            {
                _reflectionLoweredAt = _lastNow;
            }
        }

        private void LogWaterFallback(string reason)
        {
            WaterLogLines++;
            _logger.Log(LogLevel.Info, WaterFallbackLog.Tag, WaterFallbackLog.Line(reason));
        }

        // Las unidades del agua avanzada (fase 13, después de Ready; todas livianas, DT6b-02): (0) el soporte (DT6b-22: el shader corre Y su
        // SubShader activo es el avanzado, con la palabra clave del nivel), (1) el perfil del horizonte (Medio), (2) la RT del reflejo
        // (Alto), (3) el sub-dibujo del agua avanzada en una RT de 64² (sus estados de pipeline; las variantes ya están en la colección) y
        // (4) lista. Con el agua forzada a la degradada (las pruebas y tomas viejas) no se prepara.
        private void WaterWarmupUnit(double now)
        {
            if (_waterStep >= _waterStepLimitForTest)
            {
                // AC-6B-22 (QA-V6b-06): la prueba deja la avanzada a mitad de la preparación para cerrarla así.
                return;
            }

            var world = _world!;
            var advanced = world.AdvancedWater!;
            _waterWatch.Restart();
            try
            {
                switch (_waterStep)
                {
                    case 0:
                        if (_options.WaterForceAllowed && _options.Water == WaterForce.Degraded)
                        {
                            _waterWarmupDone = true;
                            return;
                        }

                        advanced.SetTier(_profile.Tier);
                        if (_forceUnsupportedForTest || !AdvancedWaterSurface.IsSupported(advanced.Material))
                        {
                            if (_waterAvailability.ReportUnsupported(WaterModeSelector.UnsupportedReason))
                            {
                                LogWaterFallback(WaterModeSelector.UnsupportedReason);
                            }

                            _waterWarmupDone = true;
                            return;
                        }

                        break;
                    case 1:
                        if (_profile.Tier == QualityTier.Medium)
                        {
                            advanced.RefreshProfile(KitEnvironment, 0);
                        }

                        break;
                    case 2:
                        // El espejo solo existe en Alto y con la avanzada en camino (con la degradada fija no hay segunda cámara).
                        if (_profile.Tier == QualityTier.High && world.Reflection == null)
                        {
                            world.Reflection = new PlanarReflection(world.Root.transform, world.Layer);
                        }

                        if (world.Reflection != null && _target.Texture != null && !world.Reflection.Ensure(_target.Texture))
                        {
                            ReportWaterFailure("RT del reflejo");
                        }

                        break;
                    case 3:
                        WaterSubDraw(world);
                        break;
                    default:
                        _advancedReady = true;
                        _waterWarmupDone = true;
                        break;
                }

                _waterStep++;
            }
            catch (Exception error)
            {
                _waterWarmupDone = true;
                ReportWaterFailure("falla de carga: " + error.GetType().Name);
            }
            finally
            {
                var ms = _waterWatch.Elapsed.TotalMilliseconds;
                if (ms > _worstByPhase[PhaseWater])
                {
                    _worstByPhase[PhaseWater] = ms;
                }
            }
        }

        // El agua avanzada sola en una RT de 64² del formato de la sesión, sin post: sus estados de pipeline antes del primer cuadro.
        private void WaterSubDraw(SceneWorld world)
        {
            var advanced = world.AdvancedWater!;
            var target = _target.Texture;
            if (target == null)
            {
                return;
            }

            if (_waterProbeTarget == null || _waterProbeTarget.graphicsFormat != target.graphicsFormat)
            {
                ReleaseWaterProbe();
                _waterProbeTarget = new RenderTexture(new RenderTextureDescriptor(64, 64, target.graphicsFormat, target.depthStencilFormat))
                {
                    name = "RowMate.WaterWarmup",
                    hideFlags = HideFlags.HideAndDontSave,
                };
                _waterProbeTarget.Create();
            }

            var wasActive = world.Root.activeSelf;
            world.Root.SetActive(true);
            var renderers = world.Root.GetComponentsInChildren<Renderer>(false);
            var enabled = new bool[renderers.Length];
            for (var i = 0; i < renderers.Length; i++)
            {
                enabled[i] = renderers[i].enabled;
                renderers[i].enabled = ReferenceEquals(renderers[i], advanced.Renderer);
            }

            advanced.Renderer.enabled = true;
            var data = world.Rig!.Data;
            var post = data.renderPostProcessing;
            data.renderPostProcessing = false;
            try
            {
                world.Renderer!.Render(_waterProbeTarget);
            }
            finally
            {
                data.renderPostProcessing = post;
                for (var i = 0; i < renderers.Length; i++)
                {
                    renderers[i].enabled = enabled[i];
                }

                advanced.Renderer.enabled = false;
                world.Root.SetActive(wasActive);
            }
        }

        private void ReleaseWaterProbe()
        {
            if (_waterProbeTarget != null)
            {
                _waterProbeTarget.Release();
                MeshUpload.Destroy(_waterProbeTarget);
                _waterProbeTarget = null;
            }
        }

        private void ResetWaterWorld()
        {
            _advancedReady = false;
            _waterWarmupDone = false;
            _waterStep = 0;
            _glassGeneration = -1;
        }

        // El reflejo planar del cuadro (Alto, con la avanzada a la vista y el reflejo sin bajar del todo): el agua, los degradés y la sombra
        // del bote no se ven en el espejo.
        private void RenderReflection(SceneWorld world)
        {
            ReflectionDrawn = false;
            var reflection = world.Reflection;
            var surfaces = world.Director!.WaterSurfaces;
            if (reflection == null || surfaces == null || !surfaces.ShowingAdvanced || _profile.Tier != QualityTier.High || ReflectionMix <= 0)
            {
                return;
            }

            if (reflection.Lost)
            {
                if (_target.Texture != null && !reflection.Ensure(_target.Texture))
                {
                    ReportWaterFailure("RT del reflejo perdida");
                    return;
                }
            }
            else if (_target.Texture != null)
            {
                reflection.Ensure(_target.Texture);
            }

            var overlay = world.Director.Overlay.Renderer;
            var advanced = world.AdvancedWater!.Renderer;
            var shadow = world.Boat!.Shadow;
            var overlayOn = overlay.enabled;
            var shadowOn = shadow.enabled;
            overlay.enabled = false;
            advanced.enabled = false;
            shadow.enabled = false;
            try
            {
                reflection.Render(world.Rig!.Camera);
                Shader.SetGlobalTexture(SceneShaderIds.WaterReflectionTex, reflection.Texture);
                ReflectionDrawn = true;
            }
            catch (Exception error)
            {
                ReportWaterFailure("el reflejo falló: " + error.GetType().Name);
            }
            finally
            {
                overlay.enabled = overlayOn;
                advanced.enabled = true;
                shadow.enabled = shadowOn;
            }
        }

        // El vidrio después de dibujar la escena (DT6b-08): con piezas de vidrio a la vista y un nivel con desenfoque. El peso sigue con
        // su fundido de 200 ms (GlassChanged en cada cuadro del fundido; fuera de él, solo cuando cambia la textura).
        private void UpdateGlass(double now, bool showing)
        {
            var world = _world;
            var blur = world?.Blur;
            var on = showing && GlassInUse && GlassTier && blur != null && !_thumbnail && _target.Texture != null;
            var changed = false;
            if (on)
            {
                if (blur!.Ensure(_target.Texture!, _density) || blur.Generation != _glassGeneration)
                {
                    _glassGeneration = blur.Generation;
                    changed = true;
                }

                blur.Execute();
            }

            if (on != _glassTarget)
            {
                _glassTarget = on;
                _glassFrom = _glassWeight;
                _glassStart = now;
            }

            var target = _glassTarget ? 1.0 : 0.0;
            var weight = ReduceMotion ? target : _glassFrom + (target - _glassFrom) * Math.Min(1, Math.Max(0, (now - _glassStart) / GlassFadeSeconds));
            if (Math.Abs(weight - _glassWeight) > 1e-6)
            {
                _glassWeight = weight;
                changed = true;
            }

            if (changed)
            {
                GlassChanged?.Invoke();
            }
        }

        // -rowmate-perf-gpu-load (solo desarrollo, AC-6B-31): ms aproximados de GPU por cuadro con pasadas de Kawase a la RT de la escena
        // (sin un shader propio: el del vidrio), para ver los pasos de la adaptación en el registro.
        private void GpuLoad()
        {
            var blur = _world?.Blur;
            if (!(_options.GpuLoadMs > 0) || blur == null || _target.Texture == null)
            {
                return;
            }

            blur.Ensure(_target.Texture, _density);
            var passes = (int)Math.Ceiling(_options.GpuLoadMs * GpuLoadPassesPerMs);
            for (var i = 0; i < passes; i++)
            {
                blur.Execute();
            }
        }

        public const double GpuLoadPassesPerMs = 4;

        // -rowmate-water-fail <s> (solo desarrollo; VID-02, AC-6B-08): una falla a los s segundos de abierta la sesión.
        private void InjectWaterFailure(double now)
        {
            if (_failInjected || double.IsNaN(_options.WaterFailAfterSeconds) || now - _sessionOpenedAt < _options.WaterFailAfterSeconds)
            {
                return;
            }

            _failInjected = true;
            ReportWaterFailure("falla forzada de prueba");
        }

        // Las tomas con la distancia forzada (G6b-15): la estela de 12 s a la velocidad estable.
        internal void PrimeWakeForTest(double steadySpeedMps)
        {
            _session?.PrimeWakeForTest(steadySpeedMps);
        }

        // La sesión preparada (QA-V6a-REN-02; It. 6b: desde la fase 3 del armado): Open la toma en el cuadro del toque.
        internal SceneSession? PreparedSessionForTest => _nextSession;

        // ── Controles de prueba del agua (internal) ──

        // La avanzada no se muestra mientras la prueba la retiene (AdvancedArrivesWithFadeKeepingTheScene: la que llega tarde, con su fundido).
        internal void HoldAdvancedWaterForTest(bool hold)
        {
            _advancedHeld = hold;
        }

        private bool _advancedHeld;

        // Las unidades del agua avanzada no pasan del paso `step` (0 el soporte, 1 el perfil, 2 la RT del reflejo, 3 el sub-dibujo, 4 lista):
        // la avanzada queda a medio preparar (AC-6B-22, el cierre mientras se prepara). int.MaxValue = sin límite.
        internal void StopWaterWarmupAtForTest(int step)
        {
            _waterStepLimitForTest = step;
        }

        private int _waterStepLimitForTest = int.MaxValue;

        // El paso de la preparación del agua avanzada (las pruebas).
        internal int WaterWarmupStepForTest => _waterStep;

        // La avanzada no se pone lista antes de `seconds` desde ahora.
        internal void DelayAdvancedWaterForTest(double seconds)
        {
            _advancedNotBefore = _lastNow + seconds;
        }

        // El próximo soporte se informa como "sin SubShader avanzado" (con el agua real; los shaders de prueba van por ForceWaterMaterialForTest).
        internal void ForceUnsupportedWaterForTest(bool on)
        {
            _forceUnsupportedForTest = on;
        }

        // Cambia el material del agua avanzada por otro (los shaders de prueba sin el tag o sin soporte, G6b-01) y vuelve a preparar.
        internal void ForceWaterMaterialForTest(Material material)
        {
            var advanced = _world?.AdvancedWater;
            if (advanced == null)
            {
                return;
            }

            advanced.Renderer.sharedMaterial = material;
            _world!.WaterAdvancedMaterial = material;
            ResetWaterWorld();
            _forceUnsupportedForTest = !AdvancedWaterSurface.IsSupported(material);
        }

        // Una excepción en el próximo dibujo del agua avanzada (el corte en el acto, DT6b-12).
        internal void FailNextWaterDrawForTest()
        {
            var surfaces = _world?.Director?.WaterSurfaces;
            if (surfaces != null)
            {
                surfaces.FailNextApplyForTest = true;
            }
        }

        // El agua de la próxima elección (lo forzado de las pruebas y del arnés: Degraded por defecto en las de antes, DT6b-24).
        internal void ForceWaterForTest(WaterForce force)
        {
            _options.Water = force;
            _options.WaterForceAllowed = true;
            if (force != WaterForce.Degraded && _waterWarmupDone && !_advancedReady)
            {
                // Estaba forzada a la degradada y no se preparó: ahora sí.
                ResetWaterWorld();
            }
        }

        // Las unidades del agua avanzada en el acto (las pruebas y el arnés esperan AdvancedReady; G6b-16).
        internal void CompleteWaterForTest()
        {
            var guard = 0;
            while (WaterWarmupPending && guard++ < 32)
            {
                WaterWarmupUnit(_lastNow);
            }
        }

        // La sesión de app de vuelta a "todo disponible" (cada prueba arranca de cero: el host es de la prueba, esto es para el arnés).
        internal void ResetWaterAvailabilityForTest()
        {
            ResetWaterWorld();
        }

        // ── It. 7: el remero ──

        // El remero que quedó armado (sus largos: SetRower calcula el ajuste fuera del Tick) y el nivel (sombras, partículas, hervidero),
        // a una sesión (07-remero-palada/02-tech-design §2.12, §3.2).
        private void ConfigureRower(SceneSession? session)
        {
            if (session == null)
            {
                return;
            }

            var rower = _world?.Rower;
            session.SetRower(rower != null ? rower.Spec : RowMate.Core.Scene.Stroke.RowerSpec.Mannequin);
            session.SetRenderTier(RenderTierOf(_profile));
        }

        public static RowMate.Core.Scene.Stroke.RowerRenderTier RenderTierOf(QualityTierProfile profile)
        {
            switch (profile.Tier)
            {
                case QualityTier.High:
                    return new RowMate.Core.Scene.Stroke.RowerRenderTier(profile.Shadows, true, 1, true);
                case QualityTier.Medium:
                    return new RowMate.Core.Scene.Stroke.RowerRenderTier(profile.Shadows, false, 1, true);
                default:
                    return new RowMate.Core.Scene.Stroke.RowerRenderTier(false, false, 0.5, false);
            }
        }

        // La cámara lateral de prueba (§3.6): 4 m a la derecha del remero (x −4), 1,0 m de alto, mirando al punto medio de los pernos,
        // perspectiva de 40°. Pisa la cámara del cuadro después de aplicarlo (solo en el dibujo).
        private void ApplySideCamera(SceneWorld world)
        {
            var camera = world.Rig!.Camera;
            var boat = world.Boat;
            var pin = boat != null ? boat.Pitch.TransformPoint(new Vector3(0, (float)RowMate.Core.Scene.Stroke.BoatSpec.Real.PinY, 0))
                : new Vector3(0, 0.31f, (float)SceneCamera.BoatZ);
            var eye = new Vector3(-SideCameraDistance, SideCameraHeight, pin.z);
            camera.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(pin - eye, Vector3.up));
            camera.ResetProjectionMatrix();
            camera.fieldOfView = SideCameraFovDeg;
            camera.aspect = _heightPx > 0 ? (float)_widthPx / _heightPx : 1f;
        }

        public const float SideCameraDistance = 4f, SideCameraHeight = 1f, SideCameraFovDeg = 40f;

        // ── Controles de prueba (internal: las pruebas, el anfitrión de pruebas y el arnés; §3.1) ──

        // It. 7 (§3.6): la fase canónica forzada (congela la pose y los efectos nuevos; null la suelta).
        internal void ForceCanonicalForTest(double? canonical)
        {
            _session?.ForceCanonicalForTest(canonical);
        }

        internal void InjectPuddleForTest(double x, double worldZ, double age)
        {
            _session?.InjectPuddleForTest(x, worldZ, age);
        }

        internal void InjectRingForTest(double x, double worldZ, double age, int kind)
        {
            _session?.InjectRingForTest(x, worldZ, age, kind);
        }

        // El remero: el maniquí, el del slot (el binding o CharacterBinding) o uno inválido (cae al maniquí con un aviso). Rehace el remero
        // y los herrajes en el mundo armado y pasa sus largos a las sesiones; vale también para los armados que siguen.
        internal void ForceCharacterForTest(RowerCharacterKind kind)
        {
            _options.Character = kind;
            var world = _world;
            if (world?.Director == null || world.Boat == null || world.RowerMaterial == null)
            {
                return;
            }

            world.Rower?.Dispose();
            world.Rower = RowerCharacterResolver.Resolve(kind, _options.CharacterBinding, world.Boat.Pitch, world.Layer, world.RowerMaterial, _slotLog);
            RowerCharacterResolver.Dress(world.Rower, world.RowerMaterial);
            SceneWorldBuilder.BuildFittings(world.Fittings!, world.Rower.Spec);
            world.Director.Rower = world.Rower;
            world.Director.RowerChanged();
            ConfigureRower(_session);
            ConfigureRower(_nextSession);
        }

        // La cámara lateral de prueba (apaga la respiración).
        internal void SideCameraForTest(bool on)
        {
            _sideCamera = on;
            _session?.BreathingOffForTest(on);
        }

        internal bool SideCameraOnForTest => _sideCamera;

        // La regla de 1 m en el perno: la dibuja el arnés (RowerMarks) sobre la toma con la cámara de este cuadro.
        internal bool ShowRulerForTest { get; set; }

        // Las articulaciones del remero medidas en el rig después del último Apply (marco del bote; índice = RowerJoint).
        internal Vector3[]? RowerJointsForTest => _world?.Rower?.Driver.Measured;

        private bool _sideCamera;

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

        // It. 8: el escenario de una prueba sin pasar por la preferencia (null = el elegido). Con el mundo armado, el kit cambia en el
        // próximo Preload u Open.
        internal void ForceEnvironmentForTest(EnvironmentId? environment)
        {
            _environmentForTest = environment;
        }

        // La miniatura (ThumbnailRule; AC-SCN-22-04): sin bote, remero, efectos ni boya, y los objetos a menos de 150 m.
        internal void ThumbnailModeForTest(bool on)
        {
            _thumbnail = on;
        }

        internal bool ThumbnailMode => _thumbnail;

        // La ventana fija de una prueba (la miniatura: 1 824 × 1 152 px a densidad 12 = 152 × 96 dp).
        internal void ViewportForTest(int widthPx, int heightPx, double density)
        {
            _viewportPinned = false;
            SetViewport(widthPx, heightPx, density);
            _viewportPinned = true;
        }

        internal void ReleaseViewportForTest()
        {
            _viewportPinned = false;
        }

        private bool _viewportPinned;

        // El cuadro actual dibujado en `target` con el post de la hora (la miniatura horneada).
        internal void RenderToForTest(RenderTexture target)
        {
            if (_world?.Director != null && _session != null)
            {
                DrawFrame(_lastNow, false, target);
            }
        }

        private bool _thumbnail;
        private ShorePropBuffer? _thumbnailProps;

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

        internal void StopBoatForTest()
        {
            _session?.StopBoatForTest();
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
