using System;
using RowMate.Core.Ports;

namespace RowMate.Core.Common
{
    // La regla de la pantalla siempre encendida (F-APP-14, Q-56; AC-APP-14-01 a -04; 02-tech-design §4.4 de la It. 4a):
    // pedida al arrancar (AppRoot.Awake, antes de la primera pantalla) y otra vez al volver (fin del segundo plano o foco
    // recuperado: AC-APP-14-02); liberada al cerrar (AC-APP-14-03). lib/main.dart:42 la pide una vez y no la suelta: el
    // proceso que termina la libera. Un rechazo no se le muestra al usuario (paridad) y se registra UNA vez como Warning:
    // "[APP] pantalla encendida: el sistema la rechazó" (AC-APP-14-04). Escucha el ciclo de vida, así toda la regla se prueba en
    // dotnet con FakeAppLifecycle y FakeWakeLock (QA-G4aT-09). Después de Release no vuelve a pedir. Hilo principal.
    public sealed class ScreenKeepAwake : IDisposable
    {
        public const string Tag = "APP";

        private readonly IWakeLock _wakeLock;
        private readonly IAppLogger _logger;
        private readonly IDisposable _backgroundSubscription;
        private readonly IDisposable _focusSubscription;
        private bool _started;
        private bool _released;
        private bool _rejectionLogged;
        private bool _disposed;

        public ScreenKeepAwake(IWakeLock wakeLock, IAppLifecycle lifecycle, IAppLogger logger)
        {
            _wakeLock = wakeLock ?? throw new ArgumentNullException(nameof(wakeLock));
            if (lifecycle == null)
            {
                throw new ArgumentNullException(nameof(lifecycle));
            }

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _backgroundSubscription = lifecycle.InBackground.Subscribe(OnBackgroundChanged, emitCurrent: false);
            _focusSubscription = lifecycle.HasFocus.Subscribe(OnFocusChanged, emitCurrent: false);
        }

        // El último pedido fue aceptado y no se liberó.
        public bool IsHeld { get; private set; }

        // El primer pedido (AppRoot.Awake). Idempotente.
        public void Start()
        {
            if (_started || _released)
            {
                return;
            }

            _started = true;
            Request();
        }

        // Vuelve a pedir (también si ya estaba pedida: el sistema puede haberla soltado en segundo plano).
        public void Request()
        {
            if (_released || _disposed)
            {
                return;
            }

            bool accepted;
            try
            {
                accepted = _wakeLock.Enable();
            }
            catch (Exception error)
            {
                accepted = false;
                _logger.Log(LogLevel.Debug, Tag, "pantalla encendida: " + error.GetType().Name, error);
            }

            IsHeld = accepted;
            if (!accepted && !_rejectionLogged)
            {
                _rejectionLogged = true;
                _logger.Log(LogLevel.Warning, Tag, "pantalla encendida: el sistema la rechazó");
            }
        }

        // Libera el pedido (AppRoot.OnDestroy). Después ya no se vuelve a pedir.
        public void Release()
        {
            if (_released)
            {
                return;
            }

            _released = true;
            IsHeld = false;
            try
            {
                _wakeLock.Disable();
            }
            catch (Exception error)
            {
                _logger.Log(LogLevel.Warning, Tag, "pantalla encendida: no se pudo liberar (" + error.GetType().Name + ")", error);
            }
        }

        // = Release y se desuscribe.
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            Release();
            _disposed = true;
            _backgroundSubscription.Dispose();
            _focusSubscription.Dispose();
        }

        private void OnBackgroundChanged(bool inBackground)
        {
            if (!inBackground && _started)
            {
                Request();
            }
        }

        private void OnFocusChanged(bool hasFocus)
        {
            if (hasFocus && _started)
            {
                Request();
            }
        }
    }
}
