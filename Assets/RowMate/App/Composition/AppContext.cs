using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RowMate.App.Device;
using RowMate.App.Routines;
using RowMate.App.Scene;
using RowMate.App.Shell;
using RowMate.App.Workout;
using RowMate.Core.Common;
using RowMate.Core.Localization;
using RowMate.Core.Models;
using RowMate.Core.Ports;
using RowMate.Core.Workout;

namespace RowMate.App.Composition
{
    // La app armada (02-tech-design §2.8): los stores que leen las vistas y los puertos que comparten. Lo crea AppComposer.
    // Nombre: choca con System.AppContext; quien importe System y RowMate.App.Composition a la vez usa un alias
    // (using AppContext = RowMate.App.Composition.AppContext;).
    public sealed class AppContext : IDisposable
    {
        public const string HrmTag = "HRM";
        public const string DbTag = RoutinesStore.Tag;

        // El tag del legacy para el remo ("[BLE]"): lo usan las acciones de la pestaña Dispositivo al registrar una falla.
        public const string BleTag = "BLE";

        private readonly DeviceBackend _devices;
        private readonly DerivedObservable<AppLanguage, Localizer> _texts;
        private bool _started;
        private bool _disposed;

        internal AppContext(DeviceBackend devices, DeviceStore device, HeartRateStore heartRate, ShellStore shell, SceneSettings scene,
                            RoutinesStore routines, WorkoutEngine workout, WorkoutStore workoutRest, LanguageState language,
                            DerivedObservable<AppLanguage, Localizer> texts, IReadOnlyObservable<AccessibilitySnapshot> accessibility,
                            IScheduler scheduler, IAppLifecycle lifecycle, IAppLogger logger, ImmersiveSystemBars systemBars)
        {
            _devices = devices;
            SystemBars = systemBars;
            Device = device;
            HeartRate = heartRate;
            Shell = shell;
            Scene = scene;
            Routines = routines;
            Workout = workout;
            WorkoutRest = workoutRest;
            Lifecycle = lifecycle;
            Language = language;
            _texts = texts;
            Accessibility = accessibility;
            Scheduler = scheduler;
            Logger = logger;
        }

        public DeviceStore Device { get; }

        public HeartRateStore HeartRate { get; }

        public ShellStore Shell { get; }

        public SceneSettings Scene { get; }

        // La colección de rutinas (F-RUT-09): la cargan Start() y Reintentar; la comparten Rutinas, el editor y Workout
        // (AC-WKT-12-05).
        public RoutinesStore Routines { get; }

        // El motor de la sesión (It. 4a): vive con la app (legacy: WorkoutProvider en el MultiProvider, main.dart:82).
        public WorkoutEngine Workout { get; }

        // Workout en reposo (F-WKT-12): el remo conectado, la colección compartida, iniciar y los avisos.
        public WorkoutStore WorkoutRest { get; }

        // El ciclo de vida con que se armó (ForegroundLifecycle si AppServices no trajo uno).
        public IAppLifecycle Lifecycle { get; }

        // It. 4b (F-APP-15; §4.1): la regla de las barras del sistema, una por app; la usa cada inmersivo.
        public ImmersiveSystemBars SystemBars { get; }

        public LanguageState Language { get; }

        // El Localizer del idioma vigente (DerivedObservable sobre Language.Current): cambia en vivo (AC-APP-09-05).
        public IReadOnlyObservable<Localizer> Texts => _texts;

        public IReadOnlyObservable<AccessibilitySnapshot> Accessibility { get; }

        public IScheduler Scheduler { get; }

        public IAppLogger Logger { get; }

        // El backend con el que se armó (para la capa del simulador y los tests); AppContext no lo dispone.
        public DeviceBackend Devices => _devices;

        // = main.dart:68-83 y el primer oyente del remo simulado (D-05). Se llama una vez, después de armar la UI:
        // el remo empieza a escuchar (el simulado se conecta), las rutinas se cargan sin esperar aunque nadie vaya a Rutinas
        // (AC-RUT-09-01, AC-APP-01-09; una falla queda en la pantalla y como "[DB] carga de rutinas: …") y el pulsómetro
        // intenta la reconexión al sensor recordado sin esperar; si falla, solo queda en el registro como
        // "[HRM] autoConnect: <error>" (AC-APP-01-04).
        public void Start()
        {
            if (_disposed || _started)
            {
                return;
            }

            _started = true;
            _devices.Rower.Start();
            Routines.LoadAsync().Forget(Logger, DbTag);

            Task autoConnect;
            try
            {
                autoConnect = _devices.HeartRate.AutoConnectAsync();
            }
            catch (Exception error)
            {
                LogAutoConnectFailure(error);
                return;
            }

            if (autoConnect.IsCompleted)
            {
                ReportOutcome(autoConnect);
                return;
            }

            _ = ObserveAutoConnect(autoConnect);
        }

        // El editor de una rutina (null = nueva). Los ids de serie usan el Scheduler; lo dispone la página del editor.
        public RoutineEditorStore CreateEditor(Routine? existing)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(AppContext));
            }

            return new RoutineEditorStore(existing, Routines, Scheduler, Logger);
        }

        // El HUD de un inmersivo (F-WKT-10, -13, -14, -16, -17, -20): uno por página; lo dispone la página al salir.
        public WorkoutHudStore CreateHud()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(AppContext));
            }

            // It. 4b: con el programador y el ciclo de vida (el reloj de pared, WallClockTicker).
            return new WorkoutHudStore(Workout, Texts, Scheduler, Lifecycle, Logger);
        }

        // Al recuperar el foco (AC-APP-09-05): la lista de idiomas del sistema se vuelve a resolver; si el idioma cambia,
        // Texts avisa y las vistas cambian de texto sin rearmarse. La accesibilidad la relee Platform (su observable avisa solo).
        public void RefreshSystemSettings(IReadOnlyList<string> languageTags)
        {
            if (_disposed)
            {
                return;
            }

            Language.Refresh(languageTags);
        }

        // Dispone los stores en orden inverso a su creación: Workout en reposo y el motor primero (una sesión en curso queda
        // cortada con lo que ya tenía, P-03), después las rutinas y el resto. NO dispone el backend ni los repositorios: los
        // dispone quien los creó, después.
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            // It. 4b: si quedaron ocultas, las barras del sistema se muestran.
            SystemBars.Dispose();
            WorkoutRest.Dispose();
            Workout.Dispose();
            Routines.Dispose();
            _texts.Dispose();
            HeartRate.Dispose();
            Device.Dispose();
        }

        private async Task ObserveAutoConnect(Task autoConnect)
        {
            try
            {
                await autoConnect;
            }
            catch (Exception error)
            {
                LogAutoConnectFailure(error);
            }
        }

        private void ReportOutcome(Task autoConnect)
        {
            if (autoConnect.IsCanceled)
            {
                LogAutoConnectFailure(new TaskCanceledException(autoConnect));
                return;
            }

            var aggregate = autoConnect.Exception;
            if (autoConnect.IsFaulted && aggregate != null)
            {
                LogAutoConnectFailure(aggregate.InnerExceptions.Count == 1 ? aggregate.InnerExceptions[0] : aggregate);
            }
        }

        private void LogAutoConnectFailure(Exception error)
        {
            Logger.Log(LogLevel.Warning, HrmTag, "autoConnect: " + error.Message, error);
        }
    }
}
