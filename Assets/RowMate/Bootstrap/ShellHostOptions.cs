using System;
using RowMate.Core.Common;
using RowMate.Core.Ports;
using RowMate.Presentation.Shell;
using RowMate.Presentation.Surface;

namespace RowMate.Bootstrap
{
    // Lo que ShellHost necesita además de la app (02-tech-design §1.4 paso 7): el reloj de las animaciones (UnityScheduler en
    // la app, FakeScheduler en tests y en el arnés), si es escritorio, la política de "Atrás", la densidad y el área segura, y
    // la capa superior opcional (la del simulador; null en un build normal).
    public sealed class ShellHostOptions
    {
        public ShellHostOptions(IClock clock, bool isDesktop, IBackButtonPolicy backButton, IDisplayMetrics display, TopLayerFactory? topLayer)
        {
            Clock = clock ?? throw new ArgumentNullException(nameof(clock));
            IsDesktop = isDesktop;
            BackButton = backButton ?? throw new ArgumentNullException(nameof(backButton));
            Display = display ?? throw new ArgumentNullException(nameof(display));
            TopLayer = topLayer;
        }

        public IClock Clock { get; }

        public bool IsDesktop { get; }

        public IBackButtonPolicy BackButton { get; }

        public IDisplayMetrics Display { get; }

        public TopLayerFactory? TopLayer { get; }

        // Tubería de color del panel (D-01): con GammaRt, lo que se dibuja a mano compensa la conversión a lineal (MeshColor).
        public UiColorPipeline Pipeline { get; set; } = UiColorPipeline.GammaRt;
    }
}
