using System;

namespace RowMate.Core.Common
{
    // Lo que el que avanza el programador (UnityScheduler en la app, LifecycleHarness en las pruebas) le pasa en cada cuadro y
    // al volver del segundo plano (02-tech-design §2.9.2 de la It. 4b): el monótono crudo (el Stopwatch, que puede contar la
    // suspensión del equipo o no), la hora de pared, el offset local, lo dormido según el sistema y los avisos de "se
    // suspende" y "reanuda". Sin medida ni avisos (Plain) es lo de la 4a.
    // It. 5 (05-series-parciales/00-deuda-4b §1): también lo que durmió el equipo según los avisos (de "se suspende" a "reanuda",
    // con la hora de cada aviso), para que el registro de cada despertar diga lo que vio cada fuente.
    public readonly struct FrameClock
    {
        public FrameClock(double rawMonotonic, DateTimeOffset utc, TimeSpan localOffset, double systemAsleepSeconds = double.NaN,
                          int suspendSignals = 0, int resumeSignals = 0, double signalAsleepSeconds = double.NaN)
        {
            RawMonotonic = rawMonotonic;
            Utc = utc;
            LocalOffset = localOffset;
            SystemAsleepSeconds = systemAsleepSeconds;
            SuspendSignals = suspendSignals;
            ResumeSignals = resumeSignals;
            SignalAsleepSeconds = signalAsleepSeconds;
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

        // Lo que duró el último ciclo completo de avisos (del último "se suspende" al "reanuda" que lo siguió), con la hora de cada
        // aviso (Windows: GetTickCount64, que cuenta el sueño); NaN si todavía no hubo un ciclo completo. Solo va al registro: el
        // reloj despierto no lo usa (SuspendAccounting).
        public double SignalAsleepSeconds { get; }

        public bool HasMeasure => !double.IsNaN(SystemAsleepSeconds);

        // Lo de la 4a: sin medida ni avisos.
        public static FrameClock Plain(double rawMonotonic, DateTimeOffset utc, TimeSpan localOffset)
        {
            return new FrameClock(rawMonotonic, utc, localOffset);
        }
    }
}
