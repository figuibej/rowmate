using System;

namespace RowMate.Core.Ble
{
    // Los tiempos de HeartRateService (It. 11, 02-tech-design §2.2), con los del legacy por defecto. Los tests pueden pasar otros.
    public sealed record HrmTimings(
        TimeSpan Scan,
        TimeSpan Connect,
        TimeSpan Stale,
        TimeSpan StartupInterval,
        int StartupMaxAttempts,
        TimeSpan DropInterval)
    {
        // hrs:108 (búsqueda 10 s), :245 (conexión 15 s), :38 (watchdog 10 s), :41-42 (arranque cada 10 s, 6 intentos), :45 (caída
        // cada 3 s, sin límite).
        public static readonly HrmTimings Legacy = new HrmTimings(
            TimeSpan.FromSeconds(10),
            TimeSpan.FromSeconds(15),
            TimeSpan.FromSeconds(10),
            TimeSpan.FromSeconds(10),
            6,
            TimeSpan.FromSeconds(3));
    }
}
