using RowMate.App.Shell;
using RowMate.Core.Common;
using RowMate.Core.Localization;
using RowMate.Core.Ports;

namespace RowMate.App.Composition
{
    // Lo que necesita AppComposer para armar la app (02-tech-design §2.8 de la It. 2; §3.7 de la It. 3; §2.13 de la It. 4a):
    // los servicios del equipo, los repositorios de rutinas y de sesiones, el reloj, el registro, los catálogos, los puertos del
    // sistema (idioma y accesibilidad) y las opciones del shell. Lo arman AppRoot (la escena Boot), UiTestHost (PlayMode y el
    // arnés) y los tests de dotnet, cada uno con lo suyo. Los repositorios los dispone quien los creó (LocalStore), como el
    // DeviceBackend.
    public sealed record AppServices(DeviceBackend Devices, IRoutineRepository Routines, ISessionRepository Sessions, IScheduler Scheduler,
                                     IAppLogger Logger, LocalizationSet Texts, ISystemLocale Locale, IAccessibilitySettings Accessibility,
                                     ShellOptions Shell)
    {
        // Reloj de la fecha de creación de las rutinas: lo lee SOLO RoutinesStore, una vez por alta. null = el Scheduler.
        // Las pruebas pasan SequenceClock (13:00:00.000Z y un minuto más por rutina creada, spec §0.4); los ids de serie
        // siguen usando el Scheduler (QA-G3-19).
        public IClock? CreationClock { get; init; }

        // El ciclo de vida (segundo plano y foco: It. 4a, §4.2). null = ForegroundLifecycle (siempre adelante y con foco).
        public IAppLifecycle? Lifecycle { get; init; }
    }
}
