using System;
using System.Runtime.InteropServices;
using System.Threading;
using RowMate.Core.Common;
using RowMate.Core.Ports;

namespace RowMate.Platform.Power
{
    // Pantalla siempre encendida en Windows (F-APP-14, Q-56; 02-tech-design §4.4 de la It. 4a): SetThreadExecutionState por
    // P/Invoke a kernel32.dll, DESDE EL HILO PRINCIPAL: el pedido es del hilo que lo hace y el de Unity vive todo el proceso,
    // así vale también con la ventana minimizada (AC-APP-14-02). Pide ES_CONTINUOUS | ES_DISPLAY_REQUIRED | ES_SYSTEM_REQUIRED
    // (el legacy pide las dos primeras, wakelock_plus_windows_plugin.dart:23; ARCHITECTURE §13 suma la tercera para que
    // tampoco se suspenda el equipo: DT4a-09). Disable vuelve a ES_CONTINUOUS. 0 = rechazo.
    // El pedido es del hilo (R4a-07): guarda el id del hilo que pidió; un Disable desde otro hilo no llama al sistema (no
    // liberaría nada) y lo anota como Warning. En el Editor también se libera en AppDomain.DomainUnload (salir del modo Play
    // recarga el dominio), que corre en el hilo principal: WakeLockTests.ReleaseRunsOnTheRequestingThread lo comprueba.
    public sealed class WindowsWakeLock : IWakeLock
    {
        public const uint EsContinuous = 0x80000000;
        public const uint EsSystemRequired = 0x00000001;
        public const uint EsDisplayRequired = 0x00000002;
        public const uint RequestFlags = EsContinuous | EsDisplayRequired | EsSystemRequired;

        private readonly IAppLogger _logger;
        private int _requestThread = -1;
        private bool _held;
        private bool _unloadHooked;

        public WindowsWakeLock(IAppLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // El último pedido fue aceptado y no se liberó.
        public bool IsHeld => _held;

        // El hilo que hizo el último pedido (-1 si ninguno).
        public int RequestThreadId => _requestThread;

        // La última respuesta del sistema a un pedido o una liberación (para las pruebas).
        public uint LastResult { get; private set; }

        // Liberaciones que no llamaron al sistema por venir de otro hilo.
        public int CrossThreadReleases { get; private set; }

        public bool Enable()
        {
            _requestThread = Thread.CurrentThread.ManagedThreadId;
            var result = Call(RequestFlags);
            LastResult = result;
            _held = result != 0;
            if (!_unloadHooked)
            {
                _unloadHooked = true;
                AppDomain.CurrentDomain.DomainUnload += OnDomainUnload;
            }

            return _held;
        }

        public void Disable()
        {
            if (_requestThread >= 0 && Thread.CurrentThread.ManagedThreadId != _requestThread)
            {
                CrossThreadReleases++;
                _logger.Log(LogLevel.Warning, "APP", "pantalla encendida: la liberación llegó desde otro hilo y no se hizo");
                return;
            }

            if (_unloadHooked)
            {
                _unloadHooked = false;
                AppDomain.CurrentDomain.DomainUnload -= OnDomainUnload;
            }

            LastResult = Call(EsContinuous);
            _held = false;
        }

        private void OnDomainUnload(object? sender, EventArgs e)
        {
            if (_held)
            {
                Disable();
            }
        }

        private static uint Call(uint flags)
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            return SetThreadExecutionState(flags);
#else
            return 0;
#endif
        }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint SetThreadExecutionState(uint esFlags);
#endif
    }
}
