using System;
using RowMate.App.Device;
using RowMate.App.Routines;
using RowMate.App.Scene;
using RowMate.App.Shell;
using RowMate.Core.Common;
using RowMate.Core.Localization;

namespace RowMate.App.Composition
{
    // Composición pura de la app (02-tech-design §2.8; ARCHITECTURE §4.1, ADR 003): arma los stores desde un DeviceBackend
    // y un IRoutineRepository (It. 3).
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

            if (services.Devices == null || services.Routines == null || services.Scheduler == null || services.Logger == null
                || services.Texts == null || services.Locale == null || services.Accessibility == null || services.Shell == null)
            {
                throw new ArgumentException("AppComposer: AppServices incompleto.", nameof(services));
            }

            var logger = services.Logger;
            var language = new LanguageState(services.Locale.PreferredLanguageTags, logger);
            var texts = new DerivedObservable<AppLanguage, Localizer>(language.Current, services.Texts.For, logger);
            var device = new DeviceStore(services.Devices.Rower, logger);
            var heartRate = new HeartRateStore(services.Devices.HeartRate, logger);
            var shell = new ShellStore(services.Shell, logger);
            var scene = new SceneSettings(logger);
            // legacy: main.dart:83 (RoutinesProvider sobre DatabaseService). La fecha de creación, con su propio reloj.
            var routines = new RoutinesStore(services.Routines, services.CreationClock ?? services.Scheduler, logger);
            return new AppContext(services.Devices, device, heartRate, shell, scene, routines, language, texts,
                services.Accessibility.Current, services.Scheduler, logger);
        }
    }
}
