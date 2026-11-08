namespace RowMate.App.Device
{
    // Qué aviso del enlace con el remo (Q-31, Q-34; 10a 02-tech-design §6.2, §6.3).
    public enum LinkNoticeKind
    {
        // Empezó la reconexión: "Reconectando con {name}…", fijo hasta Reconnected o Cleared.
        Reconnecting,
        // La reconexión salió bien: "Reconectado con {name}", 2 s (DV-99).
        Reconnected,
        // El watchdog desconectó: "El monitor dejó de enviar datos", 4 s sobre el HUD (DV-98).
        MonitorStopped,
        // La reconexión terminó sin volver (Desconectar, Buscar o elegir otro remo): se quita el aviso fijo.
        Cleared
    }

    // Un aviso del enlace que emite DeviceStore.LinkNotices; Name es el nombre del remo (null si no tiene).
    public sealed record LinkNotice(LinkNoticeKind Kind, string? Name);

    // Qué caja muestra Dispositivo arriba de "Buscar dispositivos" (AC-BLE-08-05, AC-BLE-09-02).
    public enum LinkBannerKind
    {
        None,
        Reconnecting,
        MonitorStopped
    }

    // De qué operación es el error del banner rojo (AC-BLE-05-08, AC-BLE-06-06).
    public enum DeviceErrorKind
    {
        None,
        Scan,
        Connect
    }
}
