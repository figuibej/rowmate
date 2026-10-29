using System;
using RowMate.Core.Common;
using RowMate.Core.Ports;
using RowMate.Presentation.Shell;
using RowMate.Presentation.Surface;

namespace RowMate.Bootstrap
{
    // Lo que ShellHost necesita además de la app (02-tech-design §1.4 paso 7 de la It. 2; 03-rutinas/02-tech-design §1.1): el
    // reloj de las animaciones (UnityScheduler en la app, FakeScheduler en tests y en el arnés), si es escritorio, la política
    // de "Atrás", la densidad y el área segura, la capa superior opcional (la del simulador; null en un build normal) y, desde
    // la It. 3, el portapapeles (SystemClipboard; FakeClipboard en pruebas: R3-10) y el teclado del teléfono.
    public sealed class ShellHostOptions
    {
        public ShellHostOptions(IClock clock, bool isDesktop, IBackButtonPolicy backButton, IDisplayMetrics display, TopLayerFactory? topLayer,
                                IClipboard clipboard, ISoftKeyboard keyboard)
        {
            Clock = clock ?? throw new ArgumentNullException(nameof(clock));
            IsDesktop = isDesktop;
            BackButton = backButton ?? throw new ArgumentNullException(nameof(backButton));
            Display = display ?? throw new ArgumentNullException(nameof(display));
            TopLayer = topLayer;
            Clipboard = clipboard ?? throw new ArgumentNullException(nameof(clipboard));
            Keyboard = keyboard ?? throw new ArgumentNullException(nameof(keyboard));
        }

        public IClock Clock { get; }

        public bool IsDesktop { get; }

        public IBackButtonPolicy BackButton { get; }

        public IDisplayMetrics Display { get; }

        public TopLayerFactory? TopLayer { get; }

        public IClipboard Clipboard { get; }

        public ISoftKeyboard Keyboard { get; }

        // Tubería de color del panel (D-01): con GammaRt, lo que se dibuja a mano compensa la conversión a lineal (MeshColor).
        public UiColorPipeline Pipeline { get; set; } = UiColorPipeline.GammaRt;

        // El contorno de depuración de la zona del bote en el inmersivo (It. 4a, AC-WKT-17-09): solo PlayMode y el arnés.
        public bool BoatZoneOutline { get; set; }
    }
}
