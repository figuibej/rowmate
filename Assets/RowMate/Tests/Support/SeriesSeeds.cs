using System;
using RowMate.App.Workout;
using RowMate.Core.Localization;
using RowMate.Core.Text;
using RowMate.Core.Workout;

namespace RowMate.Tests.Support
{
    // Estados de los paneles armados a mano, sin correr el motor (05-series-parciales/01-po-spec §0.4; 02-tech-design §6.2), como
    // la fila de 1998 píldoras de la 4b. Los textos salen de los formatos de la app y del catálogo, como en WorkoutHudStore.
    public static class SeriesSeeds
    {
        // 5999:59.9 (359 999,9 s): el tiempo más largo de M:SS.d con cuatro cifras de minutos.
        public const double LongLapSeconds = 359999.9;

        // 5999:59 (359 999 s).
        public const double LongSplitSeconds = 359999;

        public const int LongMeters = 999999;

        // S-LARGO: en un trabajo de una serie, la vuelta en curso en 5999:59.9 y 499 m; las vueltas #1999 a #1996, cada una con
        // 5999:59.9; la repetición 999 de 999 con 999.999 m; y las anteriores 999, 998 y 997, cada una con split 5999:59 y
        // 999.999 m (AC-WKT-18-17; CAP-25).
        public static HudSeries Largo(Localizer l10n, long session = 1, int stepIndex = 1)
        {
            if (l10n == null)
            {
                throw new ArgumentNullException(nameof(l10n));
            }

            var laps = new HudLapRow[4];
            for (var i = 0; i < laps.Length; i++)
            {
                laps[i] = Lap(l10n, 1999 - i, LongLapSeconds);
            }

            var reps = new HudRepRow[3];
            for (var i = 0; i < reps.Length; i++)
            {
                reps[i] = Rep(l10n, 999 - i, LongSplitSeconds, LongMeters);
            }

            return new HudSeries(session, stepIndex, true, WorkoutFormats.LapTime(LongLapSeconds), LongLapSeconds,
                Fmt.Integer(499) + " " + l10n.Get(L10nKeys.HudUnitMeters), 499, laps, 1999, true, 999,
                Progress(l10n, 999, 999, LongMeters), LongMeters, reps);
        }

        // ExtremeSplit: S-LARGO con la repetición anterior Rep 999 de un trabajo de 86 400 s y 1 m: split 86 400 / 1 × 500 =
        // 43 200 000 s, más largo que 5999:59 (AC-WKT-18-17; QA-G5T-16): la escala común de las filas de repeticiones llega a su
        // piso (0,6) y el split se recorta con … al final.
        public static HudSeries ExtremeSplit(Localizer l10n, long session = 1, int stepIndex = 1)
        {
            var largo = Largo(l10n, session, stepIndex);
            var summary = new RepSummary(999, LongMeters, 1, 86400);
            var split = summary.WorkSplitSeconds!.Value;
            var reps = new[]
            {
                Rep(l10n, 999, split, LongMeters),
                largo.Reps[1],
                largo.Reps[2],
            };

            return largo with { Reps = reps };
        }

        public static HudLapRow Lap(Localizer l10n, int number, double seconds)
        {
            return new HudLapRow(number, l10n.Format(L10nKeys.HudLapNumber, MessageArg.Text("number", Fmt.Integer(number))),
                WorkoutFormats.LapTime(seconds), seconds);
        }

        public static HudRepRow Rep(Localizer l10n, int rep, double? split, int meters)
        {
            return new HudRepRow(rep, l10n.Format(L10nKeys.HudRepLabel, MessageArg.Text("rep", Fmt.Integer(rep))), WorkoutFormats.RepSplit(split),
                split ?? 0, WorkoutFormats.PanelMeters(meters), meters);
        }

        public static string Progress(Localizer l10n, int rep, int count, int meters)
        {
            return l10n.Format(L10nKeys.HudRepProgress, MessageArg.Text("rep", Fmt.Integer(rep)), MessageArg.Text("count", Fmt.Integer(count)),
                MessageArg.Text("meters", WorkoutFormats.PanelMeters(meters)));
        }
    }
}
