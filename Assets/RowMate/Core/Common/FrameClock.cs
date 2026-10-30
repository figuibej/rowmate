using System;

namespace RowMate.Core.Common
{
    // Lo que el que avanza el programador (UnityScheduler en la app, LifecycleHarness en las pruebas) le pasa en cada cuadro y
    // al volver del segundo plano (02-tech-design §2.9.2 de la It. 4b): el monótono crudo (el Stopwatch, que puede contar la
    // suspensión del equipo o no), la hora de pared, el offset local, lo dormido según el sistema y los avisos de "se
    // suspende" y "reanuda". Sin medida ni avisos (Plain) es lo de la 4a.
    public readonly struct FrameClock
    {
        public FrameClock(double rawMonotonic, DateTimeOffset utc, TimeSpan localOffset, double systemAsleepSeconds = double.NaN,
                          int suspendSignals = 0, int resumeSignals = 0)
        {
            RawMonotonic = rawMonotonic;
            Utc = utc;
            LocalOffset = localOffset;
            SystemAsleepSeconds = systemAsleepSeconds;
            SuspendSignals = suspendSignals;
            ResumeSignals = resumeSignals;
        }

        // El Stopwatch, en segundos.
        public double RawMonotonic { get; }

        public DateTimeOffset Utc { get; }

        public TimeSpan LocalOffset { get; }

        // Lo dormido según el sistema desde el arranque (Windows: GetTickCount64 − QueryUnbiasedInterruptTime); NaN sin medida.
        public double SystemAsleepSeconds { get; }

        // Cuántos avisos de "se suspende" llegaron desde el arranque (PBT_APMSUSPEND).
        public int SuspendSignals { get; }

        // Cuántos de "reanuda" (PBT_APMRESUMEAUTOMATIC o PBT_APMRESUMESUSPEND).
        public int ResumeSignals { get; }

        public bool HasMeasure => !double.IsNaN(SystemAsleepSeconds);

        // Lo de la 4a: sin medida ni avisos.
        public static FrameClock Plain(double rawMonotonic, DateTimeOffset utc, TimeSpan localOffset)
        {
            return new FrameClock(rawMonotonic, utc, localOffset);
        }
    }
}
