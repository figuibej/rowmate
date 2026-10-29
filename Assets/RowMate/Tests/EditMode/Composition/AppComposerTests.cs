using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using RowMate.App.Composition;
using RowMate.App.Device;
using RowMate.App.Routines;
using RowMate.App.Shell;
using RowMate.Core.Ble;
using RowMate.Core.Ble.Unavailable;
using RowMate.Core.Common;
using RowMate.Core.Localization;
using RowMate.Core.Models;
using RowMate.Core.Ports;
using RowMate.Core.Workout;
using RowMate.Tests.EditMode.Localization;
using RowMate.Tests.Support;
using AppContext = RowMate.App.Composition.AppContext;

namespace RowMate.Tests.EditMode.Composition
{
    // AppComposer + AppContext = el MultiProvider de lib/main.dart:58-92 (composición pura, 02-tech-design §2.8).
    [TestFixture]
    public sealed class AppComposerTests
    {
        private FakeScheduler _scheduler = null!;
        private RecordingLogger _logger = null!;
        private ControllableRowerService _rower = null!;
        private ScriptedHeartRateService _hrm = null!;
        private DeviceBackend _backend = null!;
        private FakeSystemLocale _locale = null!;
        private FakeAccessibilitySettings _accessibility = null!;
        private InMemoryRoutineRepository _routines = null!;
        private InMemorySessionRepository _sessions = null!;

        [SetUp]
        public void SetUp()
        {
            _scheduler = new FakeScheduler();
            _logger = new RecordingLogger();
            _rower = new ControllableRowerService(_logger);
            _hrm = new ScriptedHeartRateService(_logger);
            _backend = new DeviceBackend(_rower, _hrm, null);
            _locale = new FakeSystemLocale("es-AR");
            _accessibility = new FakeAccessibilitySettings(1.15, reduceMotion: true);
            _routines = new InMemoryRoutineRepository(RoutineSeeds.Lista.Routines);
            _sessions = new InMemorySessionRepository();
        }

        private AppContext Compose(bool profileTab = false, IRoutineRepository? routines = null, IClock? creationClock = null,
                                   IAppLifecycle? lifecycle = null)
        {
            return AppComposer.Compose(new AppServices(_backend, routines ?? _routines, _sessions, _scheduler, _logger, LocalizationFixture.Set,
                _locale, _accessibility, new ShellOptions(profileTab)) { CreationClock = creationClock, Lifecycle = lifecycle });
        }

        [Test]
        public void ComposesTheStoresFromTheBackend()
        {
            var app = Compose(profileTab: true);
            Assert.That(app.Devices, Is.SameAs(_backend));
            Assert.That(app.Device.State.Value.Status, Is.EqualTo(BleStatus.Disconnected));
            Assert.That(app.HeartRate.State.Value.Status, Is.EqualTo(HrmStatus.Disconnected));
            Assert.That(app.Shell.Destinations.Count, Is.EqualTo(5));
            Assert.That(app.Scene.HourOverride.Value, Is.Null);
            Assert.That(app.Language.Current.Value, Is.EqualTo(AppLanguage.Es));
            Assert.That(app.Texts.Value.LanguageCode, Is.EqualTo("es"));
            Assert.That(app.Accessibility.Value, Is.EqualTo(new AccessibilitySnapshot(1.15, true)));
            Assert.That(app.Scheduler, Is.SameAs(_scheduler));
            Assert.That(app.Logger, Is.SameAs(_logger));
            Assert.That(_rower.StartCalls, Is.EqualTo(0), "armar no arranca nada");
            Assert.That(_hrm.AutoConnectCalls, Is.EqualTo(0));
            app.Dispose();
        }

        [Test, Category("AC-APP-01-04")]
        public void StartLaunchesAutoConnectWithoutWaiting()
        {
            var app = Compose();
            app.Start();
            Assert.That(_rower.StartCalls, Is.EqualTo(1), "el remo empieza a escuchar (el simulado se conecta)");
            Assert.That(_hrm.AutoConnectCalls, Is.EqualTo(1), "el pulsómetro arranca activo e intenta reconectar");
            app.Start();
            Assert.That(_rower.StartCalls, Is.EqualTo(1), "una sola vez");
            Assert.That(_hrm.AutoConnectCalls, Is.EqualTo(1));
            Assert.That(_logger.Lines, Is.Empty);
            app.Dispose();
        }

        [Test, Category("AC-APP-01-04")]
        public void StartDoesNotWaitForASlowAutoConnect()
        {
            var slow = new SlowHeartRateService();
            _backend = new DeviceBackend(_rower, slow, null);
            var app = Compose();
            var previous = SynchronizationContext.Current;
            var context = new TestMainThreadContext();
            SynchronizationContext.SetSynchronizationContext(context);
            try
            {
                app.Start(); // vuelve en el acto aunque la reconexión siga
                Assert.That(slow.Calls, Is.EqualTo(1));
                slow.Fail(new InvalidOperationException("sin sensor recordado"));
                context.RunUntilIdle();
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(previous);
            }

            Assert.That(_logger.Lines, Is.EqualTo(new[] { "[HRM] Warning: autoConnect: sin sensor recordado" }));
            app.Dispose();
        }

        [Test, Category("AC-APP-01-04")]
        public void AutoConnectFailureGoesOnlyToTheLog()
        {
            _hrm.AutoConnectError = new InvalidOperationException("GATT 133");
            var app = Compose();
            var states = new StateRecorder<HeartRateState>(app.HeartRate.State);
            Assert.DoesNotThrow(() => app.Start());
            Assert.That(_logger.Lines, Is.EqualTo(new[] { "[HRM] Warning: autoConnect: GATT 133" }));
            Assert.That(_logger.Errors[0], Is.InstanceOf<InvalidOperationException>());
            Assert.That(app.HeartRate.State.Value.Error, Is.Null, "nada en pantalla");
            Assert.That(states.Count, Is.EqualTo(0));
            app.Dispose();
        }

        [Test, Category("AC-APP-01-03")]
        public void NormalBuildBackendOpensOnTheSearchView()
        {
            _backend = new DeviceBackend(new UnavailableRowerService(_logger), new UnavailableHeartRateService(_logger), null);
            var app = Compose();
            app.Start();
            _scheduler.Advance(TimeSpan.FromSeconds(10));
            Assert.That(DeviceScreenRules.ViewFor(app.Device.State.Value.Status), Is.EqualTo(DeviceViewKind.Scan),
                "el remo arranca desconectado y Dispositivo muestra la vista de búsqueda");
            Assert.That(HrmCardRules.BodyFor(app.HeartRate.State.Value), Is.EqualTo(HrmCardBody.NoSensor));
            Assert.That(_logger.Lines, Is.Empty);
            app.Dispose();
            _backend.Dispose();
        }

        [Test, Category("AC-APP-01-08")]
        public void DisposeLeavesNoTimers()
        {
            var app = Compose();
            app.Start();
            _rower.Emit(BleStatus.Connected);
            app.Dispose();
            var deviceStates = new StateRecorder<DeviceState>(app.Device.State);
            _rower.Emit(BleStatus.Disconnected);
            _hrm.EmitBpm(80);
            Assert.That(deviceStates.Count, Is.EqualTo(0), "los stores ya no escuchan");
            Assert.That(_rower.Disposed, Is.False, "AppContext no dispone el backend: lo dispone quien lo creó");
            _backend.Dispose();
            Assert.That(_rower.Disposed, Is.True);
            Assert.That(_hrm.Disposed, Is.True);
            Assert.That(_scheduler.PendingTimers, Is.EqualTo(0));
            Assert.DoesNotThrow(() => app.Dispose());
            Assert.DoesNotThrow(() => _backend.Dispose());
            app.Start();
            Assert.That(_rower.StartCalls, Is.EqualTo(1), "una app dispuesta no arranca");
        }

        [Test, Category("AC-APP-09-05")]
        public void LanguageChangeWithTheAppOpenChangesTheTexts()
        {
            var app = Compose();
            var texts = new StateRecorder<Localizer>(app.Texts);
            _rower.Emit(BleStatus.Connected);
            app.RefreshSystemSettings(new[] { "en-US" });
            Assert.That(texts.Count, Is.EqualTo(1));
            Assert.That(app.Texts.Value.Get(L10nKeys.NavDevice), Is.EqualTo("Device"), "sin reiniciar la app");
            Assert.That(app.Device.State.Value.Status, Is.EqualTo(BleStatus.Connected), "y nada más cambia");
            app.RefreshSystemSettings(new[] { "en-GB" });
            Assert.That(texts.Count, Is.EqualTo(1), "mismo idioma: sin aviso");
            app.RefreshSystemSettings(new[] { "fr-FR", "es-AR" });
            Assert.That(app.Texts.Value.LanguageCode, Is.EqualTo("es"), "la lista entera (AC-APP-09-06)");
            app.Dispose();
        }

        [Test]
        public void ComposerRejectsIncompleteServices()
        {
            Assert.Throws<ArgumentNullException>(() => AppComposer.Compose(null!));
            Assert.Throws<ArgumentException>(() => AppComposer.Compose(new AppServices(_backend, _routines, _sessions, _scheduler, _logger,
                LocalizationFixture.Set, null!, _accessibility, new ShellOptions(false))));
            Assert.Throws<ArgumentException>(() => AppComposer.Compose(new AppServices(_backend, null!, _sessions, _scheduler, _logger,
                LocalizationFixture.Set, _locale, _accessibility, new ShellOptions(false))), "sin repositorio de rutinas");
            Assert.Throws<ArgumentException>(() => AppComposer.Compose(new AppServices(_backend, _routines, null!, _scheduler, _logger,
                LocalizationFixture.Set, _locale, _accessibility, new ShellOptions(false))), "sin repositorio de sesiones (It. 4a)");
        }

        // AC-RUT-09-01, AC-APP-01-09 (main.dart:83, routines_provider.dart:12-14): Start() carga las rutinas sin esperar, aunque
        // nadie vaya a Rutinas; una sola vez. Armar no carga nada.
        [Test, Category("AC-RUT-09-01"), Category("AC-APP-01-09")]
        public void StartLoadsRoutines()
        {
            var app = Compose();
            Assert.That(app.Routines.State.Value.Phase, Is.EqualTo(RoutinesPhase.Loading));
            Assert.That(_routines.GetAllCalls, Is.EqualTo(0), "armar no lee la base");
            app.Start();
            Assert.That(_routines.GetAllCalls, Is.EqualTo(1));
            Assert.That(app.Routines.State.Value.Phase, Is.EqualTo(RoutinesPhase.Ready), "la lista ya está antes de la primera visita");
            Assert.That(app.Routines.State.Value.Routines.Count, Is.EqualTo(4));
            app.Start();
            Assert.That(_routines.GetAllCalls, Is.EqualTo(1), "cambiar de pestaña o volver a llamar no vuelve a leer");
            Assert.That(_logger.Lines, Is.Empty);
            app.Dispose();
        }

        // Una carga lenta no demora el arranque ni cambia lo que muestra Dispositivo (AC-APP-01-09).
        [Test, Category("AC-APP-01-09")]
        public void SlowLoadDoesNotBlockTheStart()
        {
            var slow = new ScriptedRoutineRepository(_scheduler, RoutineSeeds.Lista.Routines) { LoadDelay = TimeSpan.FromSeconds(2) };
            var app = Compose(routines: slow);
            app.Start();
            Assert.That(_rower.StartCalls, Is.EqualTo(1), "el remo arrancó igual");
            Assert.That(app.Routines.State.Value.Phase, Is.EqualTo(RoutinesPhase.Loading));
            _scheduler.Advance(TimeSpan.FromSeconds(2));
            Assert.That(app.Routines.State.Value.Phase, Is.EqualTo(RoutinesPhase.Ready));
            app.Dispose();
        }

        // El editor sale de la app armada: los ids de serie con el Scheduler y la fecha de creación con CreationClock
        // (QA-G3-19); sin CreationClock, el Scheduler. Dispose de la app dispone la colección.
        [Test, Category("AC-RUT-08-06")]
        public void EditorUsesTheSchedulerAndCreationsTheCreationClock()
        {
            var clock = new SequenceClock();
            var app = Compose(creationClock: clock);
            app.Start();
            var editor = app.CreateEditor(null);
            Assert.That(editor.IsNew, Is.True);
            editor.Name = "Nueva";
            editor.AddSeries();
            Assert.That(editor.Template[0].GroupId, Is.EqualTo("g" + _scheduler.UtcNow.ToUnixTimeMilliseconds() + "_0"));
            Assert.That(editor.SaveAsync().Result, Is.EqualTo(SaveOutcome.Saved));
            Assert.That(clock.Reads, Is.EqualTo(1), "una lectura por alta");
            Assert.That(app.Routines.State.Value.Routines[0].CreatedAt, Is.EqualTo(SequenceClock.DefaultStart));
            var edit = app.CreateEditor(app.Routines.State.Value.Routines[1]);
            Assert.That(edit.IsNew, Is.False);
            app.Dispose();
            Assert.That(app.Routines.LoadAsync().IsCompleted, Is.True, "dispuesta, no carga");
            Assert.Throws<ObjectDisposedException>(() => app.CreateEditor(null));
            var withScheduler = Compose(routines: new InMemoryRoutineRepository());
            withScheduler.Start();
            var created = withScheduler.Routines.CreateAsync("X", "", new IntervalStep[0]).Result;
            Assert.That(created!.CreatedAt, Is.EqualTo(_scheduler.UtcNow), "sin CreationClock, el Scheduler");
            withScheduler.Dispose();
        }

        // It. 4a (02-tech-design §2.13): el motor vive con la app (main.dart:82), Workout en reposo usa la colección compartida y
        // cada inmersivo pide su HUD. Sin ciclo de vida, ForegroundLifecycle. Dispose deja cortada una sesión en curso, con lo que
        // tenía, antes que las rutinas.
        [Test, Category("AC-WKT-01-01"), Category("AC-WKT-12-05")]
        public void ComposesTheWorkoutEngineAndItsStores()
        {
            var app = Compose();
            Assert.That(app.Workout.State.Value.Phase, Is.EqualTo(WorkoutPhase.Idle));
            Assert.That(app.Lifecycle, Is.SameAs(ForegroundLifecycle.Instance), "sin ciclo de vida: siempre adelante y con foco");
            app.Start();
            Assert.That(app.WorkoutRest.State.Value.Routines, Is.SameAs(app.Routines.State.Value.Routines), "la colección compartida");
            Assert.That(app.WorkoutRest.State.Value.RowerConnected, Is.False);
            _rower.Emit(BleStatus.Connected);
            Assert.That(app.WorkoutRest.State.Value.RowerConnected, Is.True);
            using (var hud = app.CreateHud())
            {
                Assert.That(hud.State.Value.Phase, Is.EqualTo(WorkoutPhase.Idle));
                Assert.That(app.WorkoutRest.StartFree(), Is.True);
                Assert.That(hud.State.Value.Phase, Is.EqualTo(WorkoutPhase.Active));
                Assert.That(hud.State.Value.ControlsEnabled, Is.True);
            }

            Assert.That(_sessions.Sessions.Count, Is.EqualTo(1));
            _scheduler.Advance(TimeSpan.FromSeconds(5));
            app.Dispose();
            Assert.That(_sessions.Session(1)!.FinishedAt, Is.Null, "la sesión en curso queda cortada");
            Assert.Throws<ObjectDisposedException>(() => app.CreateHud());
            var lifecycle = new FakeAppLifecycle();
            var withLifecycle = Compose(lifecycle: lifecycle);
            Assert.That(withLifecycle.Lifecycle, Is.SameAs(lifecycle));
            withLifecycle.Dispose();
        }

        // Pulsómetro cuya reconexión tarda (como la real, hasta 15 s) y falla cuando el test quiere.
        private sealed class SlowHeartRateService : IHeartRateService
        {
            private readonly TaskCompletionSource<bool> _pending = new TaskCompletionSource<bool>();
            private readonly EventStream<int> _bpm = new EventStream<int>(NullAppLogger.Instance);
            private readonly EventStream<IReadOnlyList<BleScanResult>> _devices =
                new EventStream<IReadOnlyList<BleScanResult>>(NullAppLogger.Instance);

            public SlowHeartRateService()
            {
                Status = new ReadOnlyObservable<HrmStatus>(new ObservableValue<HrmStatus>(HrmStatus.Disconnected, NullAppLogger.Instance));
            }

            public int Calls { get; private set; }

            public IReadOnlyObservable<HrmStatus> Status { get; }

            public IEventSource<int> Bpm => _bpm;

            public int CurrentBpm => 0;

            public IEventSource<IReadOnlyList<BleScanResult>> Devices => _devices;

            public string? ConnectedDeviceName => null;

            public string? RememberedDeviceName => null;

            public bool IsRetrying => false;

            public void Fail(Exception error)
            {
                _pending.SetException(error);
            }

            public Task AutoConnectAsync()
            {
                Calls++;
                return _pending.Task;
            }

            public Task StartScanAsync(TimeSpan? timeout = null)
            {
                return Task.CompletedTask;
            }

            public Task StopScanAsync()
            {
                return Task.CompletedTask;
            }

            public Task ConnectAsync(string deviceId)
            {
                return Task.CompletedTask;
            }

            public Task DisconnectAsync()
            {
                return Task.CompletedTask;
            }

            public Task ForgetAsync()
            {
                return Task.CompletedTask;
            }

            public void Dispose()
            {
            }
        }
    }
}
