using System;

namespace RowMate.Core.Workout
{
    // Los tiempos del motor (02-tech-design §2.10 de la It. 4a).
    public static class WorkoutTimings
    {
        // Un punto cada 5 s en marcha (workout_provider.dart:159; AC-WKT-06-02).
        public const int SampleEverySeconds = 5;

        // El reloj arranca cuando vuelve el alta o, a más tardar, 100 ms después del toque (AC-WKT-01-02; DT4a-02).
        public static readonly TimeSpan ClockStartGrace = TimeSpan.FromMilliseconds(100);

        // Tope del cierre, de PRIMER PLANO (ForegroundTimeout: el tiempo en segundo plano no cuenta; AC-WKT-06-12, -02-06;
        // spec §12 punto 2; DT4a-13).
        public static readonly TimeSpan FinishSaveTimeout = TimeSpan.FromSeconds(15);

        // Al pasar a segundo plano se espera a lo sumo 1 s a que se escriba lo pendiente (AC-WKT-06-15; ADR 017, decisión 5).
        public static readonly TimeSpan BackgroundFlushTimeout = TimeSpan.FromSeconds(1);
    }
}
