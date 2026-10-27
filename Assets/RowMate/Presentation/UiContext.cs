using System;
using RowMate.Core.Common;
using RowMate.Core.Localization;
using RowMate.Core.Ports;
using RowMate.Presentation.Motion;
using UnityEngine;
using AppContext = RowMate.App.Composition.AppContext;

namespace RowMate.Presentation
{
    // Lo que comparten las vistas de la app (02-tech-design §3): la app armada (stores, textos, accesibilidad), el reloj de
    // las animaciones, la textura de degradé de los brillos (DN-06), la densidad y el área segura, y si es escritorio.
    // Lo arma ShellHost; ninguna vista sabe si los servicios son simulados, reales o de prueba (AC-BLE-13-04).
    public sealed class UiContext
    {
        public UiContext(AppContext app, MotionDriver motion, Texture2D fade, IDisplayMetrics display, bool isDesktop)
        {
            App = app ?? throw new ArgumentNullException(nameof(app));
            Motion = motion ?? throw new ArgumentNullException(nameof(motion));
            Fade = fade;
            Display = display ?? throw new ArgumentNullException(nameof(display));
            IsDesktop = isDesktop;
        }

        public AppContext App { get; }

        public MotionDriver Motion { get; }

        public Texture2D Fade { get; }

        public IDisplayMetrics Display { get; }

        public bool IsDesktop { get; }

        // El panel dibuja con forceGammaRendering (GammaRt): ver MeshColor.
        public bool GammaPanel { get; set; }

        public IAppLogger Logger => App.Logger;

        public Localizer Texts => App.Texts.Value;
    }
}
