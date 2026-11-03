using System.Collections.Generic;
using RowMate.Core.Workout;

namespace RowMate.App.Workout
{
    // Qué le pasa a la parte de arriba de un panel cuando cambia su contenido (04-design-notes §6.3 de la It. 5; DD5-18): solo
    // funde lo que cambia de clase (la vuelta en curso ↔ el vacío, la lista de repeticiones ↔ su vacío). Roll: de un trabajo a
    // otro, la vuelta en curso cambia con su rodillo, sin fundir.
    public enum SeriesBodyChange
    {
        None,
        Roll,
        ToEmpty,
        FromEmpty,
    }

    // Qué cambió entre dos estados de los paneles, POR PIEZA (lo que anima la vista; 02-tech-design §2.5 de la It. 5;
    // 04-design-notes §6.2 a §6.4). Puro. Sale de los dos HudSeries: el paso, si es trabajo, la cantidad de vueltas y el número
    // de la más reciente, si está en serie, la cantidad de repeticiones y el Rep de la primera fila (dentro de una serie los
    // números de repetición no se repiten y una serie nueva siempre pasa por la lista vacía: series_tracker.dart:149-170).
    // Instant no importa acá: lo decide la vista (con Instant no anima nada).
    public readonly struct HudSeriesChange
    {
        private HudSeriesChange(bool stepChanged, SeriesBodyChange lapsBody, int newLaps, bool lapDropped, int clearedLaps, bool newRep,
                                bool repDropped, SeriesBodyChange repsBody, bool seriesStarted, bool seriesEnded)
        {
            StepChanged = stepChanged;
            LapsBody = lapsBody;
            NewLaps = newLaps;
            LapDropped = lapDropped;
            ClearedLaps = clearedLaps;
            NewRep = newRep;
            RepDropped = repDropped;
            RepsBody = repsBody;
            SeriesStarted = seriesStarted;
            SeriesEnded = seriesEnded;
        }

        // Parciales muestra otro paso (el segundo siguiente al del cambio: P-01).
        public bool StepChanged { get; }

        // Con StepChanged: trabajo → trabajo Roll; trabajo → fuera ToEmpty; fuera → trabajo FromEmpty; fuera → fuera None (el
        // vacío no se mueve).
        public SeriesBodyChange LapsBody { get; }

        // Vueltas cerradas en este segundo (1, o más: AC-WKT-07-04).
        public int NewLaps { get; }

        // Había 4 a la vista y se va la más vieja.
        public bool LapDropped { get; }

        // Con StepChanged: las filas que había (0 a 4), que salen en su lugar (0-120).
        public int ClearedLaps { get; }

        // Una repetición entró al frente de la lista (con su marca).
        public bool NewRep { get; }

        // Había 3 y se va la más vieja.
        public bool RepDropped { get; }

        // FromEmpty: la primera fila reemplaza al vacío; ToEmpty: la lista se vacía (una serie nueva sigue a otra); None. Nunca
        // Roll. Con la serie que empieza o termina, None (el panel entero aparece o se va).
        public SeriesBodyChange RepsBody { get; }

        // InSeries false → true: aparece repeticiones y entra la línea Rep (120-280).
        public bool SeriesStarted { get; }

        // InSeries true → false: se va repeticiones y sale la línea Rep (0-120).
        public bool SeriesEnded { get; }

        public bool IsNone => !StepChanged && LapsBody == SeriesBodyChange.None && NewLaps == 0 && !LapDropped && ClearedLaps == 0 && !NewRep
                              && !RepDropped && RepsBody == SeriesBodyChange.None && !SeriesStarted && !SeriesEnded;

        // before null u otra sesión: sin cambios (el primer estado, que la vista aplica en su estado final).
        public static HudSeriesChange Between(HudSeries? before, HudSeries after)
        {
            if (before == null || after == null || before.Session != after.Session)
            {
                return default;
            }

            var stepChanged = before.StepIndex != after.StepIndex;
            var lapsBody = SeriesBodyChange.None;
            if (stepChanged)
            {
                lapsBody = before.IsWork && after.IsWork ? SeriesBodyChange.Roll
                    : before.IsWork ? SeriesBodyChange.ToEmpty
                    : after.IsWork ? SeriesBodyChange.FromEmpty
                    : SeriesBodyChange.None;
            }

            var beforeVisible = Count(before.Laps);
            int newLaps;
            if (stepChanged)
            {
                newLaps = after.LapCount;
            }
            else
            {
                var newest = Count(after.Laps) > 0 ? after.Laps[0].Number : 0;
                var previous = beforeVisible > 0 ? before.Laps[0].Number : 0;
                newLaps = newest > previous ? newest - previous : 0;
            }

            var lapDropped = !stepChanged && newLaps > 0 && beforeVisible + newLaps > SeriesPanels.VisibleLaps;
            var clearedLaps = stepChanged ? beforeVisible : 0;

            var seriesStarted = !before.InSeries && after.InSeries;
            var seriesEnded = before.InSeries && !after.InSeries;
            var beforeReps = Count(before.Reps);
            var afterReps = Count(after.Reps);
            var newRep = afterReps > 0 && (beforeReps == 0 || after.Reps[0].Rep != before.Reps[0].Rep);
            var repDropped = newRep && beforeReps >= SeriesTracker.MaxPreviousReps;
            var repsBody = SeriesBodyChange.None;
            if (!seriesStarted && !seriesEnded)
            {
                if (beforeReps == 0 && afterReps > 0)
                {
                    repsBody = SeriesBodyChange.FromEmpty;
                }
                else if (beforeReps > 0 && afterReps == 0)
                {
                    repsBody = SeriesBodyChange.ToEmpty;
                }
            }

            return new HudSeriesChange(stepChanged, lapsBody, newLaps, lapDropped, clearedLaps, newRep, repDropped, repsBody, seriesStarted,
                seriesEnded);
        }

        private static int Count<T>(IReadOnlyList<T>? list)
        {
            return list == null ? 0 : list.Count;
        }
    }
}
