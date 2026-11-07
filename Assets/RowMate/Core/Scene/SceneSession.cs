using System;
using RowMate.Core.Scene.Art;

namespace RowMate.Core.Scene
{
    // Las constantes de una sesión de escena (06a-escena-lago/02-tech-design §2.15). Los valores por defecto son los de §9.3.
    public sealed class SceneSessionOptions
    {
        public static SceneSessionOptions Default { get; } = new SceneSessionOptions();

        public CelestialSpec Celestial { get; init; } = CelestialSpec.Default;
        public BreathingSpec Breathing { get; init; } = BreathingSpec.Lake;
        public FlightSpec Flight { get; init; } = FlightSpec.Default;
        public AerialSpec Aerial { get; init; } = AerialSpec.Default;
        public HorizonSpec Horizon { get; init; } = HorizonSpec.Lake;
        public HourLookTable Look { get; init; } = HourLookTable.Default;

        // La exposición del velo de pausa: −0,50 EV de partida (DN6a-19; WP-D2 la ajusta con la sonda).
        public double VeilEv { get; init; } = HourLookTable.Default.VeilEv;

        // El hemisferio de la luna (ISystemLocale; null = norte, también en las pruebas).
        public string? RegionCode { get; init; }

        // El tope de objetos visibles (dimensiona el cuadro).
        public int MaxVisibleProps { get; init; } = 120;
    }

    // Las entradas de un cuadro: el reloj monótono (s), la hora local y la UTC del reloj del equipo, la foto del motor, la hora
    // forzada del simulador (SceneSettings.HourOverride), reducir movimiento, la ventana en dp y el peso del velo (0-1).
    public readonly struct SceneTickInputs
    {
        public SceneTickInputs(double now, DateTimeOffset localNow, DateTimeOffset utcNow, in SceneInputs workout, int? forcedHour, bool reduceMotion,
                               double widthDp, double heightDp, double veilWeight)
        {
            Now = now;
            LocalNow = localNow;
            UtcNow = utcNow;
            Workout = workout;
            ForcedHour = forcedHour;
            ReduceMotion = reduceMotion;
            WidthDp = widthDp;
            HeightDp = heightDp;
            VeilWeight = veilWeight;
        }

        public double Now { get; }
        public DateTimeOffset LocalNow { get; }
        public DateTimeOffset UtcNow { get; }
        public SceneInputs Workout { get; }
        public int? ForcedHour { get; }
        public bool ReduceMotion { get; }
        public double WidthDp { get; }
        public double HeightDp { get; }
        public double VeilWeight { get; }

        public SceneTickInputs WithNow(double now)
        {
            return new SceneTickInputs(now, LocalNow, UtcNow, Workout, ForcedHour, ReduceMotion, WidthDp, HeightDp, VeilWeight);
        }
    }

    // El agregado de la escena (06a-escena-lago/02-tech-design §2.15): una sesión por inmersivo (AC-SCN-01-03). Se abre en el
    // toque (velocidad, distancia y tiempo en 0), sigue en pausa y con la sesión terminada (el tiempo corre) y se descarta al
    // cerrar; un Tick por cuadro, sin asignaciones (los arreglos se reservan al construir; la ventana nueva rehace las 80
    // direcciones en el mismo arreglo), con el paso acotado. Solo lee la foto del motor: nunca escribe en él, y la distancia de
    // escena no sale de acá (AC-SCN-03-05, AC-WKT-13-14). La geometría y los materiales del mundo NO son de la sesión.
    public sealed class SceneSession
    {
        private const int CabinSlots = 8;
        private const double FrameStep = 1.0 / 60;

        private readonly ShoreEnvironment _environment;
        private readonly SceneSessionOptions _options;
        private readonly ShoreSegments _segments;
        private readonly SceneHourTransition _hour = new SceneHourTransition();
        private readonly StarPoint[] _starPoints = new StarPoint[StarField.Count];
        private readonly DartRandom _scratch = new DartRandom(0);
        private readonly int[] _cabinSeeds = new int[CabinSlots];
        private readonly NightLightFader[] _cabinFaders = new NightLightFader[CabinSlots];
        private readonly long[] _cabinSeen = new long[CabinSlots];
        private readonly bool _southern;

        private StarFader _stars;
        private double _lastNow;
        private bool _opened;
        private bool _snapNext;
        private double _lastEffective = double.NaN;
        private int? _lastForced;
        private int _lastRealMinute = -1;
        private double _layoutWidth = double.NaN;
        private double _layoutHeight = double.NaN;
        private long _tickCount;

        private double? _frozenTimeForTest;
        private double? _forcedHourForTest;
        private bool _forcedHourChanged;
        private bool _breathingOffForTest;

        // It. 7: la palada (02-tech-design §2.12). Todo se reserva al construir; SetRower rehace el ajuste fuera del Tick.
        private readonly Stroke.VisualStrokeClock _clock = new Stroke.VisualStrokeClock();
        private readonly Stroke.RowerIdle _idle = new Stroke.RowerIdle();
        private readonly Stroke.PuddleField _puddles = new Stroke.PuddleField();
        private readonly Stroke.StrokeSpray _spray = new Stroke.StrokeSpray();
        private readonly Stroke.BuoyTracker _buoy = new Stroke.BuoyTracker();
        private readonly Stroke.BoatSpec _boat = Stroke.BoatSpec.Real;
        private Stroke.RowerSpec _rower = Stroke.RowerSpec.Mannequin;
        private Stroke.RigFit _fit = Stroke.RiggingFit.Solve(Stroke.BoatSpec.Real, Stroke.RowerSpec.Mannequin);
        private Stroke.RowerRenderTier _tier = Stroke.RowerRenderTier.High;
        private Stroke.EffortFilter _effort;
        private Stroke.ShadowSideFader _shadowSide;
        private BoatCameraFilter _boatCamera;
        private bool _strokeFrozenForTest;

        public SceneSession(ShoreEnvironment environment, SceneSessionOptions options)
        {
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _segments = new ShoreSegments(environment);
            _southern = MoonPhase.IsSouthernRegion(options.RegionCode);
            Camera = new ChaseCameraDirector(options.Breathing, options.Flight, options.Aerial);
            for (var i = 0; i < CabinSlots; i++)
            {
                _cabinSeeds[i] = int.MinValue;
            }
        }

        public SceneState State { get; } = new SceneState();

        public ChaseCameraDirector Camera { get; }

        public ShoreSegments Segments => _segments;

        public ShoreEnvironment Environment => _environment;

        public SceneSessionOptions Options => _options;

        public bool IsOpen => _opened;

        // El cuerpo del remero (el maniquí o el medido del rig del slot) y su ajuste al bote.
        public Stroke.RowerSpec Rower => _rower;

        public Stroke.RigFit Fit => _fit;

        public Stroke.BoatSpec Boat => _boat;

        // El remero que resolvió SceneHost (fuera del Tick: calcula el ajuste una vez).
        public void SetRower(in Stroke.RowerSpec rower)
        {
            _rower = rower;
            _fit = Stroke.RiggingFit.Solve(_boat, rower);
        }

        // El nivel de calidad para la sombra, las partículas y el hervidero (SceneHost, al armar y al cambiar de nivel).
        public void SetRenderTier(in Stroke.RowerRenderTier tier)
        {
            _tier = tier;
        }

        // La hora que dibuja la escena (NaN antes del primer Tick).
        public double VisibleHour => _hour.Visible;

        // El toque: la sesión arranca en 0 (AC-SCN-01-03) y corre desde ese cuadro aunque todavía no se dibuje.
        public void Open(double now)
        {
            State.Reset();
            Camera.Reset();
            _lastNow = now;
            _opened = true;
            _snapNext = true;
            _lastEffective = double.NaN;
            _lastForced = null;
            _lastRealMinute = -1;
            _layoutWidth = double.NaN;
            _layoutHeight = double.NaN;
            _stars = default;
            _clock.Reset();
            _idle.Reset();
            _puddles.Clear();
            _spray.Clear();
            _buoy.Reset();
            _effort = default;
            _shadowSide = default;
            _boatCamera = default;
            for (var i = 0; i < CabinSlots; i++)
            {
                _cabinSeeds[i] = int.MinValue;
                _cabinFaders[i] = default;
                _cabinSeen[i] = 0;
            }
        }

        // La vuelta del segundo plano o del equipo dormido (AC-SCN-01-04): el próximo cuadro avanza ≤ 0,1 s (el paso acotado) y
        // la hora llega en el acto, con los fundidos de las estrellas y de las luces en su valor final.
        public void NotifyResumed()
        {
            _snapNext = true;
        }

        public void Tick(in SceneTickInputs inputs, ref SceneFrame frame)
        {
            if (!_opened)
            {
                throw new InvalidOperationException("SceneSession: Open antes del primer Tick.");
            }

            EnsureFrame(ref frame);
            _tickCount++;
            var dt = inputs.Now - _lastNow;
            _lastNow = inputs.Now;
            var realDt = dt < 0 ? 0 : (dt > 1 ? 1 : dt);
            var w = inputs.Workout;
            State.Update(dt, w.Pace500m, w.Spm, w.IsActive);
            var sceneTime = _frozenTimeForTest ?? State.Time;
            var snap = _snapNext;
            _snapNext = false;
            var step = dt < 0 || double.IsNaN(dt) ? 0 : (dt > SceneState.MaxStep ? SceneState.MaxStep : dt);
            TickRower(step, w, sceneTime, snap, ref frame.Rower);

            // La hora: la efectiva del cuadro y qué cambió (§2.5).
            var visible = UpdateHour(inputs, snap || _forcedHourChanged);
            if (_forcedHourChanged)
            {
                snap = true;
                _forcedHourChanged = false;
            }

            var palette = SceneTimeOfDay.PaletteFor(visible);
            var ambient = palette.Ambient;
            var moon = MoonPhase.At(inputs.UtcNow);
            var celestial = CelestialPlacement.Evaluate(visible, moon.Illumination, _options.Celestial);
            var look = HourLighting.Evaluate(visible, palette, celestial, _options.Look);

            var cam = new SceneCamera(inputs.WidthDp, inputs.HeightDp);
            if (cam.Width != _layoutWidth || cam.Height != _layoutHeight)
            {
                StarField.Layout(cam.Width, cam.HorizonY, _starPoints, _scratch);
                _layoutWidth = cam.Width;
                _layoutHeight = cam.Height;
            }

            var pose = Camera.Evaluate(inputs.Now, sceneTime, cam, inputs.ReduceMotion, _breathingOffForTest);

            // Los términos del bote (AC-SCN-06-03), filtrados y por el multiplicador de la respiración, con el tope de DT7-06.
            if (snap)
            {
                _boatCamera.Snap(frame.Rower.Boat);
            }
            else
            {
                _boatCamera.Step(step, frame.Rower.Boat);
            }

            pose = ChaseCameraMotion.AddBoatTerms(pose, cam, _boatCamera.Value, Camera.LastBreathingMultiplier);
            frame.Window = cam;
            frame.Pose = pose;
            frame.Projection = ChaseCameraProjection.Blend(cam, pose.FocalDp, pose.HorizonDp);
            frame.CameraMode = Camera.Mode;
            frame.BreathingMultiplier = Camera.LastBreathingMultiplier;
            frame.HorizonDeviationDp = ChaseCameraMotion.HorizonDeviationDp(cam, pose);

            frame.VisibleHour = visible;
            frame.Palette = palette;
            frame.Celestial = celestial;
            frame.LegacyLight = SceneTimeOfDay.LightFor(visible, cam);

            // La sombra del bote (AC-SCN-08-06; DT7-08): mapa u óvalo, el lado del óvalo con su fundido y su alfa.
            frame.Rower.ShadowMode = Stroke.BoatShadowRule.Mode(_tier.HasShadows, look.ShadowStrength(_tier.HighTier));
            _shadowSide.Step(Stroke.BoatShadowRule.LegacySide(frame.LegacyLight.X, cam.CenterX), realDt, snap);
            frame.Rower.ShadowSide = _shadowSide.Value;
            frame.Rower.ShadowAlpha = Stroke.BoatShadowRule.OvalAlpha(ambient);
            frame.Moon = moon;
            frame.SouthernMirror = _southern;
            frame.DiskRadiusRad = CelestialPlacement.DiskRadiusRad(celestial.DiskIsMoon);
            frame.HaloRadiusRad = CelestialPlacement.HaloRadiusRad(celestial.DiskIsMoon);
            frame.HaloAlphaScale = CelestialPlacement.HaloFactor(celestial.DiskIsMoon, moon.Illumination) * celestial.DiskVisibility;
            frame.DiskHdr = CelestialPlacement.DiskHdr(celestial);
            frame.Look = look;
            frame.Post = HourLighting.Compose(look, inputs.VeilWeight, _options.VeilEv, _options.Look.VeilSaturationLoss);
            frame.LowMist = look.LowMist;
            frame.Golden = look.Golden;

            // Las estrellas, desproyectadas con la cámara del contrato (con la respiración en 0 caen en su píxel del legacy).
            var shown = StarField.IsShown(ambient);
            if (snap)
            {
                _stars.Snap(shown);
            }
            else
            {
                _stars.Step(shown, realDt);
            }

            frame.StarFade = _stars.Level;
            for (var i = 0; i < StarField.Count; i++)
            {
                var p = _starPoints[i];
                Unproject(cam, p.X, p.Y, out var dx, out var dy, out var dz);
                var alpha = frame.WarmUpAll ? 1 : StarField.Alpha(i, sceneTime, ambient, _stars.Level);
                frame.Stars[i] = new StarInstance(dx, dy, dz, p.Radius / cam.Focal, alpha);
            }

            // Las nubes: la x del legacy sin la distancia (D-ART-06), con el fundido de la vuelta (DV-56).
            var bodyAlpha = CloudModel.BodyAlpha(ambient);
            var shadeAlpha = CloudModel.ShadeAlpha(ambient);
            frame.CloudBody = CloudModel.BodyColor(palette);
            var specs = CloudModel.Legacy;
            for (var i = 0; i < CloudModel.Count; i++)
            {
                var x = CloudModel.X(i, cam.Width, sceneTime);
                var y = CloudModel.Y(i, cam.HorizonY);
                Unproject(cam, x, y, out var dx, out var dy, out var dz);
                var fade = frame.WarmUpAll ? 1 : CloudModel.WrapFade(i, cam.Width, sceneTime, SkyArt.CloudWrapRight, SkyArt.CloudWrapLeft);
                frame.Clouds[i] = new CloudInstance(dx, dy, dz, specs[i].SizeDp / cam.Focal, i % 4, bodyAlpha * fade, shadeAlpha * fade, x, y);
            }

            // Las cordilleras: el alto por ventana y el acercamiento por distancia (en double).
            var distance = State.Distance;
            frame.HorizonPeakTan = HorizonModel.PeakTan(cam, _options.Horizon);
            frame.HorizonFarScale = HorizonModel.ApproachScale(distance, true, _options.Horizon);
            frame.HorizonNearScale = HorizonModel.ApproachScale(distance, false, _options.Horizon);
            frame.HorizonFar = HorizonModel.FarColor(palette);
            frame.HorizonNear = HorizonModel.NearColor(palette);
            frame.Snow = HorizonModel.SnowColor(palette);

            // El agua degradada: los tres tramos y las 10 líneas del legacy, centradas bajo el astro que se ve (DV-59).
            DegradedWater.Bands(palette, out var top, out var mid, out var bottom);
            frame.WaterTop = _environment.TintWater(top);
            frame.WaterMid = _environment.TintWater(mid);
            frame.WaterBottom = _environment.TintWater(bottom);
            var centerX = DegradedWater.DiskScreenX(cam, celestial.DiskAzimuthDeg);
            var waterHeight = DegradedWater.WaterHeight(cam);
            for (var i = 0; i < DegradedWater.LineCount; i++)
            {
                frame.Lines[i] = DegradedWater.Line(i, distance, cam.Width, cam.HorizonY, waterHeight, centerX, celestial.LegacyStrength);
            }

            frame.WaterPhase = DegradedWater.Phase(distance);
            frame.ShimmerVisibility = frame.WarmUpAll ? 1 : celestial.DiskVisibility;
            frame.ShimmerCenterX = centerX;

            // La orilla: la lista del legacy y la luz de cada cabaña.
            _segments.VisibleProps(distance, frame.VisibleProps);
            var props = frame.VisibleProps.Items;
            for (var i = 0; i < props.Length; i++)
            {
                frame.PropLight[i] = props[i].Kind == PropKind.Cabin ? CabinLight(props[i].Seed, visible, ambient, realDt, snap, frame.WarmUpAll) : 0;
            }

            frame.Distance = distance;
            frame.Speed = State.Speed;
            frame.SceneTime = sceneTime;
            frame.Draw = true;
        }

        // Las pruebas y el arnés (§2.3): un salto grande en pasos de 1/60 s desde el último cuadro, con el resto de las entradas
        // fijas (el arnés no "salta" 60 s en un cuadro: la escena de CAP-13 tiene la distancia de un remo real).
        public void Advance(in SceneTickInputs inputs, double seconds, ref SceneFrame frame)
        {
            var steps = (int)Math.Ceiling(seconds / FrameStep - 1e-9);
            if (steps <= 0)
            {
                Tick(inputs.WithNow(_lastNow), ref frame);
                return;
            }

            var step = seconds / steps;
            var start = _lastNow;
            for (var i = 1; i <= steps; i++)
            {
                Tick(inputs.WithNow(start + i * step), ref frame);
            }
        }

        // ── Controles de prueba (internal: solo las pruebas, el arnés y los controles *ForTest de SceneHost; §0.4 de la spec) ──

        // E-0, E-920, E-1234, E-42K: la distancia de escena (la velocidad y el tiempo siguen).
        internal void ForceDistanceForTest(double distance)
        {
            State.ForceForTest(distance, State.Speed, State.Time);
        }

        // El tiempo de escena fijo de las tomas (3,2 s): las nubes y el titileo quedan en ese instante. null = el de la sesión.
        internal void FreezeSceneTimeForTest(double? sceneTime)
        {
            _frozenTimeForTest = sceneTime;
        }

        // La hora a un minuto cualquiera, en el acto y con los fundidos en su valor final. null = la efectiva otra vez.
        internal void ForceHourForTest(double? hour)
        {
            _forcedHourForTest = hour;
            _forcedHourChanged = true;
        }

        // La respiración en 0 (la pose del contrato, quieta).
        internal void BreathingOffForTest(bool off)
        {
            _breathingOffForTest = off;
        }

        // La fase forzada (CAP-01, -02, -05, -12, -13, -17 a -21): c fija, sin eventos ni efectos nuevos. null = la de la sesión.
        internal void ForceCanonicalForTest(double? canonical)
        {
            _clock.ForceCanonicalForTest(canonical);
            _strokeFrozenForTest = canonical.HasValue;
        }

        // LT-97/98: un charco visible armado a mano (x y z de mundo, edad).
        internal void InjectPuddleForTest(double x, double worldZ, double age)
        {
            _puddles.InjectForTest(x, worldZ, age);
        }

        // La palada de un cuadro (02-tech-design §2.12), después de State.Update: el reloj visual, el esfuerzo, el reposo, la pose, el
        // bote, los eventos (charcos, salpicadura, gotas), los efectos, la boya y los hervideros. Sin asignar. La distancia de escena
        // no cambia (el surge es un desfase de dibujo).
        private void TickRower(double step, in SceneInputs w, double sceneTime, bool snap, ref Stroke.RowerFrame rower)
        {
            var events = _clock.Step(step, State.StrokePhase, w.Spm, w.IsActive);
            if (snap && _effort.Value == 0)
            {
                _effort.Snap(w.PowerWatts);
            }
            else
            {
                _effort.Step(step, w.PowerWatts);
            }

            var effort = _effort.Value;
            _idle.Step(step, _clock.StillSeconds, sceneTime);
            var c = _clock.Canonical;
            Stroke.ScullingKinematics.Evaluate(c, effort, _boat, _rower, _fit, _idle.Sample, ref rower.Pose);

            var phi = _clock.Phase;
            var spm = _clock.SmoothedSpm;
            var amplitude = Stroke.BoatSurge.Amplitude(spm, effort);
            var surge = Stroke.BoatSurge.OffsetMeters(phi, _clock.LatchedRatio, amplitude, State.Speed, spm);
            rower.Boat = new Stroke.BoatMotionSample(Stroke.BoatMotion.Pitch(phi, sceneTime), Stroke.BoatMotion.Heave(phi),
                Stroke.BoatMotion.Roll(sceneTime, Stroke.BoatMotion.LakeRollK), surge);
            var boatWorldZ = State.Distance + SceneCamera.BoatZ + surge;

            if (!_strokeFrozenForTest)
            {
                if ((events & Stroke.StrokeEvents.BladeExit) != 0)
                {
                    _puddles.OnBladeExit(rower.Pose, boatWorldZ);
                }

                if ((events & Stroke.StrokeEvents.BladeEntry) != 0)
                {
                    _spray.OnBladeEntry(rower.Pose, boatWorldZ, effort, _tier.ParticleScale);
                }

                // Las gotas: de la salida de la pala (c ≈ 0,381) a c = 0,55, con la palada en marcha.
                if (w.IsActive && _clock.PhaseSpeed > 0 && c >= DripStart && c < DripEnd)
                {
                    _spray.EmitDrips(rower.Pose, boatWorldZ, (c - DripStart) / (DripEnd - DripStart), step, _tier.ParticleScale);
                }
            }

            _puddles.Step(step, effort);
            _spray.Step(step);
            _buoy.Step(step, w.Buoy, State.Distance, State.Speed, sceneTime);

            rower.ContractPhase = State.StrokePhase;
            rower.Phase = phi;
            rower.Canonical = c;
            rower.Ratio = _clock.LatchedRatio;
            rower.PhaseSpeed = _clock.PhaseSpeed;
            rower.Effort = effort;
            rower.IdleWeight = _idle.Weight;
            rower.Events = _strokeFrozenForTest ? Stroke.StrokeEvents.None : events;
            rower.Puddles = _puddles;
            rower.Spray = _spray;
            rower.Buoy = _buoy;
            rower.BoatWorldZ = boatWorldZ;
            var boil = _tier.Boils && c >= 0.02 && c <= 0.36;
            rower.BoilLeft = boil;
            rower.BoilRight = boil;
        }

        // El tramo de las gotas de la recuperación (ANIMATION_BIBLE §4.2): de la salida de la pala visible a c = 0,55.
        private const double DripStart = 0.381, DripEnd = 0.55;

        private double UpdateHour(in SceneTickInputs inputs, bool instant)
        {
            double effective;
            HourChange kind;
            if (_forcedHourForTest.HasValue)
            {
                effective = _forcedHourForTest.Value;
                kind = HourChange.Instant;
            }
            else
            {
                effective = SceneHour.Effective(inputs.ForcedHour, inputs.LocalNow);
                var realMinute = inputs.LocalNow.Hour * 60 + inputs.LocalNow.Minute;
                if (double.IsNaN(_lastEffective))
                {
                    kind = HourChange.Instant;
                }
                else if (inputs.ForcedHour != _lastForced)
                {
                    kind = HourChange.Forced;
                }
                else if (!inputs.ForcedHour.HasValue && realMinute == (_lastRealMinute + 1) % 1440)
                {
                    kind = HourChange.MinuteTick;
                }
                else
                {
                    kind = HourChange.Instant;
                }

                _lastForced = inputs.ForcedHour;
                _lastRealMinute = realMinute;
            }

            if (instant)
            {
                kind = HourChange.Instant;
            }

            if (double.IsNaN(_lastEffective) || effective != _lastEffective || kind == HourChange.Instant && _hour.IsRunning && instant)
            {
                _hour.Retarget(effective, inputs.Now, kind, inputs.ReduceMotion);
            }

            _lastEffective = effective;
            return _hour.Evaluate(inputs.Now);
        }

        private double CabinLight(int seed, double hour, double ambient, double dt, bool snap, bool warmUpAll)
        {
            if (warmUpAll)
            {
                return 1;
            }

            var lit = NightLights.IsLit(seed, hour, ambient);
            var slot = -1;
            var oldest = 0;
            for (var i = 0; i < CabinSlots; i++)
            {
                if (_cabinSeeds[i] == seed)
                {
                    slot = i;
                    break;
                }

                if (_cabinSeen[i] < _cabinSeen[oldest])
                {
                    oldest = i;
                }
            }

            if (slot < 0)
            {
                // Una cabaña que entra en el rango ya tiene la luz de su estado (aparece a 400 m, en la niebla).
                slot = oldest;
                _cabinSeeds[slot] = seed;
                _cabinFaders[slot].Snap(lit);
            }
            else if (snap)
            {
                _cabinFaders[slot].Snap(lit);
            }
            else
            {
                _cabinFaders[slot].Step(lit, dt);
            }

            _cabinSeen[slot] = _tickCount;
            return _cabinFaders[slot].Level;
        }

        // La dirección del mundo de un punto de la pantalla del legacy con la cámara del contrato (unitaria).
        private static void Unproject(in SceneCamera cam, double sx, double sy, out double x, out double y, out double z)
        {
            var dx = (sx - cam.CenterX) / cam.Focal;
            var dy = (cam.HorizonY - sy) / cam.Focal;
            var len = Math.Sqrt(dx * dx + dy * dy + 1);
            x = dx / len;
            y = dy / len;
            z = 1 / len;
        }

        private void EnsureFrame(ref SceneFrame frame)
        {
            if (frame.Stars == null || frame.Clouds == null || frame.Lines == null || frame.VisibleProps == null || frame.PropLight == null
                || frame.Rower.Pose.Joints == null)
            {
                throw new ArgumentException("SceneSession: el cuadro se arma con SceneFrame.Create (los arreglos se reservan una vez).", nameof(frame));
            }
        }
    }
}
