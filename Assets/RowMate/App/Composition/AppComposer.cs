using System;
using RowMate.App.Device;
using RowMate.App.Export;
using RowMate.App.History;
using RowMate.App.Routines;
using RowMate.App.Scene;
using RowMate.App.Shell;
using RowMate.App.Workout;
using RowMate.Core.Common;
using RowMate.Core.Localization;
using RowMate.Core.Workout;

namespace RowMate.App.Composition
{
    // Composición pura de la app (02-tech-design §2.8; ARCHITECTURE §4.1, ADR 003): arma los stores desde un DeviceBackend,
    // un IRoutineRepository (It. 3) y un ISessionRepository con el motor de la sesión (It. 4a, §2.13).
    // La usan igual AppRoot (la escena Boot), UiTestHost (PlayMode y el arnés) y los tests de dotnet: las pantallas nunca
    // saben si los servicios son simulados, reales o de prueba (R-06, AC-BLE-13-04). Sin estáticos ni singletons.
    // No arranca nada: AppContext.Start() se llama después de armar la UI.
    // legacy: lib/main.dart:58-92 (MultiProvider)
    public static class AppComposer
    {
        public static AppContext Compose(AppServices services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (services.Devices == null || services.Routines == null || services.Sessions == null || services.Scheduler == null
                || services.Logger == null || services.Texts == null || services.Locale == null || services.Accessibility == null
                || services.Shell == null)
            {
                throw new ArgumentException("AppComposer: AppServices incompleto.", nameof(services));
            }

            var logger = services.Logger;
            var language = new LanguageState(services.Locale.PreferredLanguageTags, logger);
            var texts = new DerivedObservable<AppLanguage, Localizer>(language.Current, services.Texts.For, logger);
            // It. 14 (DT14-23): el ciclo de vida va antes, porque la respuesta táctil lo usa (nada en segundo plano).
            var lifecycle = services.Lifecycle ?? ForegroundLifecycle.Instance;
            // It. 14 (DT14-23): la política de la háptica (250 ms, Success sobre Medium, nada en segundo plano), compartida por el
            // remo y el entrenamiento; sin puerto, ninguna.
            var haptics = services.Haptics == null ? null : new HapticGate(services.Haptics, services.Scheduler, lifecycle);
            // It. 10a (§6.2): la hora del diagnóstico con el programador y la vibración de "Listo" (y de la reconexión, #51).
            var device = new DeviceStore(services.Devices.Rower, logger, services.Scheduler, haptics);
            var heartRate = new HeartRateStore(services.Devices.HeartRate, logger);
            var shell = new ShellStore(services.Shell, logger);
            var scene = new SceneSettings(logger);
            // legacy: main.dart:83 (RoutinesProvider sobre DatabaseService). La fecha de creación, con su propio reloj.
            var routines = new RoutinesStore(services.Routines, services.CreationClock ?? services.Scheduler, logger);
            // legacy: main.dart:82 (WorkoutProvider sobre BleService, HeartRateService y DatabaseService): vive con la app.
            var workout = new WorkoutEngine(services.Devices.Rower, services.Devices.HeartRate, services.Sessions, services.Scheduler,
                lifecycle, logger);
            var workoutRest = new WorkoutStore(workout, device, routines, lifecycle, logger);
            // It. 4b (F-APP-15): una regla de barras del sistema por app; sin puerto, sin efecto.
            var systemBars = new ImmersiveSystemBars(services.SystemUi ?? NoSystemUi.Instance, lifecycle, logger);
            // It. 5 (§2.8): las preferencias (en memoria si AppServices no trajo unas) y los dos paneles de series, uno por app.
            var preferences = services.Preferences ?? new MemoryPreferences();
            var seriesPanels = new SeriesPanelPrefs(preferences, logger);
            // It. 8 (§2.8, DT8-14): el escenario elegido, después de las preferencias (si ya están leídas, el primer cuadro lo ve).
            var sceneEnvironment = new SceneEnvironmentChoice(preferences, logger);
            // It. 9 (§3.1): la lista del historial, una por app; recarga con los toques de la pestaña y con el cierre de una sesión.
            var history = new HistoryStore(services.Sessions, shell, workout.State, logger);
            // It. 12 (§4, DT12-11): "Exportar TCX", uno por app (salir del Detalle no cancela). Solo lee la base.
            var export = new TcxExportService(services.Sessions, services.Exporter ?? NoFileExporter.Instance, services.Scheduler,
                services.ExportZone ?? LocalZone, texts, services.SimulatorBuild, logger);
            // It. 14 (AC-POL-03): pausa, reanudar, cambio de paso y rutina completada; sin puerto, ninguna.
            var workoutHaptics = haptics == null ? null : new WorkoutHaptics(workout, haptics);
            return new AppContext(services.Devices, device, heartRate, shell, scene, routines, workout, workoutRest, language, texts,
                services.Accessibility.Current, services.Scheduler, lifecycle, logger, systemBars, preferences, seriesPanels, sceneEnvironment,
                history, services.Sessions, services.Routines, export)
            {
                WorkoutHaptics = workoutHaptics,
            };
        }

        // La zona del equipo sin exportador de la plataforma (AppServices.ExportZone = null).
        private static TimeZoneInfo LocalZone()
        {
            return TimeZoneInfo.Local;
        }

        // Las barras del sistema sin efecto (AppServices.SystemUi = null).
        private sealed class NoSystemUi : RowMate.Core.Ports.ISystemUi
        {
            public static readonly NoSystemUi Instance = new NoSystemUi();

            public void SetImmersive(bool immersive)
            {
            }
        }
    }
}
