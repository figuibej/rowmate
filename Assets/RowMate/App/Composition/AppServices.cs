using RowMate.App.Shell;
using RowMate.Core.Common;
using RowMate.Core.Localization;
using RowMate.Core.Ports;

namespace RowMate.App.Composition
{
    // Lo que necesita AppComposer para armar la app (02-tech-design §2.8): los servicios del equipo, el reloj, el registro,
    // los catálogos, los puertos del sistema (idioma y accesibilidad) y las opciones del shell. Lo arman AppRoot (la escena
    // Boot), UiTestHost (PlayMode y el arnés) y los tests de dotnet, cada uno con lo suyo.
    public sealed record AppServices(DeviceBackend Devices, IScheduler Scheduler, IAppLogger Logger, LocalizationSet Texts,
                                     ISystemLocale Locale, IAccessibilitySettings Accessibility, ShellOptions Shell);
}
