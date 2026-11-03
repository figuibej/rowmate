using System;
using System.Collections.Generic;
using RowMate.Core.Models;

namespace RowMate.Core.Workout
{
    // Los dos paneles del inmersivo (F-WKT-18): parciales de 500 m y repeticiones anteriores.
    public enum SeriesPanelKind
    {
        Laps,
        Reps,
    }

    // Lo que muestran los paneles en este momento, sin textos (02-tech-design §2.4 de la It. 5). Laps: las 4 últimas vueltas
    // del paso, la más reciente primero (series_panels.dart:279-282): Lap0 es la más reciente; LapCount, todas las del paso
    // (el número de la más reciente es LapCount). Sin asignar (un struct).
    public readonly struct SeriesPanelsContent
    {
        public SeriesPanelsContent(bool visible, int stepIndex, bool isWork, int currentLapMeters, double currentLapSeconds, int lapCount,
                                   Lap lap0, Lap lap1, Lap lap2, Lap lap3, bool inSeries, int rep, int repCount, int repMeters,
                                   IReadOnlyList<RepSummary> previousReps)
        {
            Visible = visible;
            StepIndex = stepIndex;
            IsWork = isWork;
            CurrentLapMeters = currentLapMeters;
            CurrentLapSeconds = currentLapSeconds;
            LapCount = lapCount;
            Lap0 = lap0;
            Lap1 = lap1;
            Lap2 = lap2;
            Lap3 = lap3;
            InSeries = inSeries;
            Rep = rep;
            RepCount = repCount;
            RepMeters = repMeters;
            PreviousReps = previousReps;
        }

        // Con rutina (immersive_workout_screen.dart:105-106); en el libre, false.
        public bool Visible { get; }

        // El paso que muestran (el de la última lectura del tracker, o 0 con DV-48).
        public int StepIndex { get; }

        public bool IsWork { get; }

        public int CurrentLapMeters { get; }

        public double CurrentLapSeconds { get; }

        public int LapCount { get; }

        public Lap Lap0 { get; }

        public Lap Lap1 { get; }

        public Lap Lap2 { get; }

        public Lap Lap3 { get; }

        // min(LapCount; 4).
        public int VisibleLaps => LapCount < SeriesPanels.VisibleLaps ? LapCount : SeriesPanels.VisibleLaps;

        // El panel de repeticiones y la línea Rep (series_panels.dart:178, :313; LT-61).
        public bool InSeries { get; }

        public int Rep { get; }

        public int RepCount { get; }

        public int RepMeters { get; }

        // Hasta 3, la más reciente primero (AC-WKT-08-04).
        public IReadOnlyList<RepSummary> PreviousReps { get; }

        // La vuelta a la vista número i (0 = la más reciente).
        public Lap VisibleLap(int index)
        {
            switch (index)
            {
                case 0:
                    return Lap0;
                case 1:
                    return Lap1;
                case 2:
                    return Lap2;
                case 3:
                    return Lap3;
                default:
                    throw new ArgumentOutOfRangeException(nameof(index), index, "SeriesPanelsContent: hay 4 vueltas a la vista.");
            }
        }
    }

    // Qué muestra cada panel (02-tech-design §2.4; AC-WKT-18-01, -04 a -08). Una regla pura encima del tracker crudo (el
    // oráculo compara el tracker, no esto).
    // legacy: lib/features/workout/series_panels.dart:160-196 (los dos paneles), :279-317 (parciales), :330-344 (repeticiones);
    // immersive_workout_screen.dart:105-106 (solo con rutina).
    public static class SeriesPanels
    {
        // :281: las 4 más recientes.
        public const int VisibleLaps = 4;

        private static readonly RepSummary[] NoReps = new RepSummary[0];

        // steps / positions: la rutina expandida del motor (null en el libre).
        // - DV-48: con state null o StepIndex null (antes del primer segundo de la sesión) muestra el PRIMER paso: IsWork =
        //   steps[0] es trabajo, la vuelta en curso en 0 m y 0 s, sin vueltas; InSeries = positions[0].GroupId != null, Rep y
        //   RepCount de positions[0], RepMeters = 0, sin anteriores. El legacy muestra durante ese segundo "Sin parciales en este
        //   paso" y no muestra el panel de repeticiones (spec §15).
        // - Si no, lo del tracker tal cual: en el segundo en que termina un paso todavía muestra ese paso (P-01; :118), y recién
        //   en el siguiente la repetición que terminó entra en la lista (AC-WKT-18-08).
        public static SeriesPanelsContent For(SeriesState? state, IReadOnlyList<IntervalStep>? steps, IReadOnlyList<StepPosition>? positions)
        {
            if (steps == null)
            {
                return new SeriesPanelsContent(false, 0, false, 0, 0, 0, default, default, default, default, false, 0, 0, 0, NoReps);
            }

            if (state == null || !state.StepIndex.HasValue)
            {
                if (steps.Count == 0)
                {
                    return new SeriesPanelsContent(true, 0, false, 0, 0, 0, default, default, default, default, false, 0, 0, 0, NoReps);
                }

                var first = positions != null && positions.Count > 0 ? positions[0] : StepPosition.Loose;
                var inSeries = first.GroupId != null;
                return new SeriesPanelsContent(true, 0, steps[0].Type == StepType.Work, 0, 0, 0, default, default, default, default, inSeries,
                    first.Rep, first.RepCount, 0, NoReps);
            }

            var laps = state.Laps;
            var count = laps.Count;
            return new SeriesPanelsContent(true, state.StepIndex.Value, state.IsWorkStep, state.CurrentLapMeters, state.CurrentLapSeconds, count,
                LapFromEnd(laps, 0), LapFromEnd(laps, 1), LapFromEnd(laps, 2), LapFromEnd(laps, 3), state.InSeries, state.Rep, state.RepCount,
                state.RepMeters, state.PreviousReps);
        }

        // :279-282: (laps.length > 4 ? laps.sublist(laps.length - 4) : laps).reversed.
        private static Lap LapFromEnd(IReadOnlyList<Lap> laps, int fromEnd)
        {
            var index = laps.Count - 1 - fromEnd;
            return index >= 0 ? laps[index] : default;
        }
    }
}
