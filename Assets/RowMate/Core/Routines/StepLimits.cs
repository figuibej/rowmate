namespace RowMate.Core.Routines
{
    // Topes de Q-92 (AC-RUT-13-11, -12; AC-RUT-14-05, -06; P-01, P-02), en valor absoluto: los negativos se siguen
    // aceptando (Q-25). El legacy no tiene topes (DV-17): debajo de ellos todo es idéntico. {max} es el texto que muestra
    // editorErrorRange / editorErrorRangeRep.
    public static class StepLimits
    {
        // Min × 60 + Seg (s) y split (s): 99:59:59 y 99:59.
        public const int MaxDurationSeconds = 359_999;
        public const int MaxDistanceMeters = 999_999;
        public const int MaxWatts = 9_999;
        public const int MaxSpm = 999;

        // Solo si el split da más de 0: ≤ 0 es "sin split" y gana sobre el tope (AC-RUT-13-08, QA-G3-13 a).
        public const int MaxSplitSeconds = 5_999;
        public const int MaxProgressionSpm = 999;
        public const int MaxProgressionWatts = 9_999;
        public const int MaxProgressionSplitSeconds = 5_999;

        // Repeticiones de una serie: de 1 a 999 (P-01 sobre Q-26).
        public const int MaxRepeatCount = 999;

        public const string DurationMaxText = "99:59:59";
        public const string DistanceMaxText = "999999";
        public const string WattsMaxText = "9999";
        public const string SpmMaxText = "999";
        public const string SplitMaxText = "99:59";
        public const string ProgressionSpmMaxText = "999";
        public const string ProgressionWattsMaxText = "9999";
        public const string ProgressionSplitMaxText = "5999";

        // Primera repetición r (desde 1, hasta repeatCount) en la que |target + progression × (r − 1)| > max, o null si
        // ninguna se pasa (AC-RUT-13-12; la progresión del legacy es lineal en r: lib/core/models/routine.dart:50-63).
        // Forma cerrada, sin bucles: con ×2 000 000 000 cuesta lo mismo que con ×2 (AC-RUT-10-11, spec §12.3).
        // Escalera SPM (18, +2, tope 999): ×491 → null; ×492 → 492 (18 + 2 × 491 = 1000). ×10 con 18 y +200 → 6 (1018).
        public static int? FirstRepetitionOutOfRange(long target, long progression, long repeatCount, long max)
        {
            if (repeatCount < 1)
            {
                return null;
            }

            if (target > max || target < -max)
            {
                return 1;
            }

            if (progression == 0)
            {
                return null;
            }

            // Con |target| ≤ max, el valor cruza el tope por el lado hacia el que avanza: primer k = r − 1 con
            // target + p·k > max (p > 0) o target + p·k < −max (p < 0). room ∈ [0; 2·max]: nunca desborda.
            var room = progression > 0 ? max - target : max + target;
            var step = progression > 0 ? progression : progression == long.MinValue ? long.MaxValue : -progression;
            var k = room / step + 1;
            if (k > repeatCount - 1)
            {
                return null;
            }

            var repetition = k + 1;
            return repetition > int.MaxValue ? int.MaxValue : (int)repetition;
        }
    }
}
