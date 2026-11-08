using System;

namespace RowMate.Core.Ble
{
    // Los tiempos de RowerService (02-tech-design §2.2), con los del legacy por defecto. Los tests pueden pasar otros.
    public sealed record RowerTimings(
        TimeSpan Scan,
        TimeSpan AdapterWait,
        TimeSpan Connect,
        TimeSpan ReconnectConnect,
        TimeSpan ControlGap,
        TimeSpan KeepAlive,
        TimeSpan ReconnectDelay,
        TimeSpan WatchdogPeriod,
        TimeSpan WatchdogThreshold)
    {
        // bs:65 (búsqueda 10 s), :77 (espera del adaptador 5 s), :144 (conexión 15 s), :178 (reconexión 10 s), :269 (300 ms
        // entre 0x00 y 0x07), :280 (keepalive 2 s), :168 (reconexión a los 3 s); dp:71 (watchdog cada 1 s) y dp:8, :75 (≥ 5 s).
        public static readonly RowerTimings Legacy = new RowerTimings(
            TimeSpan.FromSeconds(10),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(15),
            TimeSpan.FromSeconds(10),
            TimeSpan.FromMilliseconds(300),
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(3),
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(5));
    }
}
