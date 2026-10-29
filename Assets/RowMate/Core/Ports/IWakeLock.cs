namespace RowMate.Core.Ports
{
    // Pantalla siempre encendida (F-APP-14, Q-56; 02-tech-design §4.4 de la It. 4a). Platform/Power lo implementa por
    // plataforma (Windows: SetThreadExecutionState; Android e iOS: Screen.sleepTimeout; macOS: nada hasta la 10b). La regla de
    // cuándo pedirla y liberarla es ScreenKeepAwake (Core). En pruebas, FakeWakeLock.
    // legacy: lib/main.dart:42 (WakelockPlus.enable()).
    public interface IWakeLock
    {
        // true = el sistema aceptó el pedido.
        bool Enable();

        void Disable();
    }
}
