namespace RowMate.Core.History
{
    // Reglas sueltas del historial (It. 9; 02-tech-design DT9-07).
    public static class HistoryRules
    {
        // Intervalo de muestreo de las sesiones grabadas por la app (Q-23, AC-HIS-11-04): la duración de un paso es puntos × 5,
        // como el legacy ("cada punto = 5 segundos", session_detail_screen.dart:218). Las importadas (It. 13) traerán el suyo.
        public const int AppSampleIntervalSeconds = 5;

        // _xInterval (sd:525-532): según el tiempo del ÚLTIMO punto, < 300 → 30, < 600 → 60, < 1800 → 120, si no 300. Sin
        // puntos, 60 (el legacy no llega a dibujar).
        public static double XIntervalFor(int? lastElapsedSeconds)
        {
            if (!lastElapsedSeconds.HasValue)
            {
                return 60;
            }

            var total = lastElapsedSeconds.Value;
            if (total < 300)
            {
                return 30;
            }

            if (total < 600)
            {
                return 60;
            }

            if (total < 1800)
            {
                return 120;
            }

            return 300;
        }
    }
}
