using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RowMate.App.Device;
using RowMate.App.Routines;
using RowMate.App.Scene;
using RowMate.App.Shell;
using RowMate.Core.Common;
using RowMate.Core.Localization;
using RowMate.Core.Models;
using RowMate.Core.Ports;

namespace RowMate.App.Composition
{
    // La app armada (02-tech-design §2.8): los stores que leen las vistas y los puertos que comparten. Lo crea AppComposer.
    // Nombre: choca con System.AppContext; quien importe System y RowMate.App.Composition a la vez usa un alias
    // (using AppContext = RowMate.App.Composition.AppContext;).
    public sealed class AppContext : IDisposable
    {
        public const string HrmTag = "HRM";
        public const string DbTag = RoutinesStore.Tag;

        private readonly DeviceBackend _devices;
        private readonly DerivedObservable<AppLanguage, Localizer> _texts;
        private bool _started;
        private bool _disposed;

        internal AppContext(DeviceBackend devices, DeviceStore device, HeartRateStore heartRate, ShellStore shell, SceneSettings scene,
                            RoutinesStore routines, LanguageState language, DerivedObservable<AppLanguage, Localizer> texts,
                            IReadOnlyObservable<AccessibilitySnapshot> accessibility, IScheduler scheduler, IAppLogger logger)
        {
            _devices = devices;
            Device = device;
            HeartRate = heartRate;
            Shell = shell;
            Scene = scene;
            Routines = routines;
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

        // La colección de rutinas (F-RUT-09): la cargan Start() y Reintentar; la comparten Rutinas y el editor (y Workout en
        // la It. 4a).
        public RoutinesStore Routines { get; }

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

        // Dispone los stores en orden inverso a su creación. NO dispone el backend: lo dispone quien lo creó, después.
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
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
