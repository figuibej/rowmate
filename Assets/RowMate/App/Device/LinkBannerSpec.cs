namespace RowMate.App.Device
{
    // Qué indicador lleva la caja del enlace (00-arte §4.1, D10-10).
    public enum LinkIndicatorKind
    {
        None,
        // Esperando el próximo intento: el arco que se llena en 3 s (hasta NextAttemptAt).
        WaitingArc,
        // Intento en curso.
        Spinner,
        // Monitor apagado: link_off.
        LinkOff
    }

    // La caja del enlace en Dispositivo (AC-BLE-08-05, AC-BLE-09-02): Reconnecting con el nombre APARTE (cortado con …, A10-08)
    // y el botón deviceDisconnect (corta los reintentos), o MonitorStopped con retry (vuelve a conectar el mismo remo). None = no
    // hay caja.
    public sealed record LinkBannerSpec(
        LinkBannerKind Kind,
        string Text,
        string? Name,
        LinkIndicatorKind Indicator,
        double? NextAttemptAt,
        string ActionKey,
        string ActionText)
    {
        public static readonly LinkBannerSpec None = new LinkBannerSpec(LinkBannerKind.None, "", null, LinkIndicatorKind.None, null, "", "");
    }
}
