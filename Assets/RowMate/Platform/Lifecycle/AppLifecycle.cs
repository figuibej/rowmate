using System;
using RowMate.Core.Common;
using RowMate.Core.Ports;

namespace RowMate.Platform.Lifecycle
{
    // El ciclo de vida de la app (ARCHITECTURE §13; ADR 017; 02-tech-design §4.2 de la It. 4a): dos ObservableValue<bool> que
    // mueve AppRoot desde OnApplicationPause (segundo plano: Android e iOS con la actividad pausada) y OnApplicationFocus (la
    // ventana con foco; en Windows minimizar también lo saca). En escritorio, minimizada o sin foco NO es segundo plano: el
    // loop sigue (runInBackground). Arranca adelante y con foco. Hilo principal.
    public sealed class AppLifecycle : IAppLifecycle
    {
        private readonly ObservableValue<bool> _inBackground;
        private readonly ObservableValue<bool> _hasFocus;

        public AppLifecycle(IAppLogger logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _inBackground = new ObservableValue<bool>(false, logger);
            _hasFocus = new ObservableValue<bool>(true, logger);
            InBackground = new ReadOnlyObservable<bool>(_inBackground);
            HasFocus = new ReadOnlyObservable<bool>(_hasFocus);
        }

        public IReadOnlyObservable<bool> InBackground { get; }

        public IReadOnlyObservable<bool> HasFocus { get; }

        public bool IsUserFacing => !_inBackground.Value && _hasFocus.Value;

        // OnApplicationPause(paused).
        public void SetBackground(bool inBackground)
        {
            _inBackground.Value = inBackground;
        }

        // OnApplicationFocus(focus).
        public void SetFocus(bool hasFocus)
        {
            _hasFocus.Value = hasFocus;
        }
    }
}
