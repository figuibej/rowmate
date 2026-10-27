using System;
using RowMate.Core.Common;
using RowMate.Platform.Accessibility;
using RowMate.Platform.Display;
using RowMate.Platform.Input;
using RowMate.Platform.Locale;

namespace RowMate.Platform
{
    // Lo que la app necesita del sistema, ya elegido por plataforma (PlatformFactory): idioma, accesibilidad, densidad y
    // área segura, y "Atrás". Refresh() vuelve a leer idioma, accesibilidad y pantalla (al recuperar el foco, AC-APP-09-05).
    public sealed class PlatformServices
    {
        public PlatformServices(SystemLocaleProvider locale, AccessibilitySettingsProvider accessibility, DisplayMetricsProvider display,
                                BackButtonPolicy backButton, PlatformKind kind, bool isDesktop)
        {
            Locale = locale ?? throw new ArgumentNullException(nameof(locale));
            Accessibility = accessibility ?? throw new ArgumentNullException(nameof(accessibility));
            Display = display ?? throw new ArgumentNullException(nameof(display));
            BackButton = backButton ?? throw new ArgumentNullException(nameof(backButton));
            Kind = kind;
            IsDesktop = isDesktop;
        }

        public SystemLocaleProvider Locale { get; }

        public AccessibilitySettingsProvider Accessibility { get; }

        public DisplayMetricsProvider Display { get; }

        public BackButtonPolicy BackButton { get; }

        public PlatformKind Kind { get; }

        // Windows y macOS (y el Editor): foco de teclado y hover (FocusHighlight, .rm-desktop).
        public bool IsDesktop { get; }

        public void Refresh()
        {
            Locale.Refresh();
            Accessibility.Refresh();
            Display.Refresh();
        }
    }
}
