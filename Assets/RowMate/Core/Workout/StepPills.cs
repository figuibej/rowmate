using System;
using RowMate.Core.Models;

namespace RowMate.Core.Workout
{
    // Los cuatro estados de la píldora (F-WKT-15; step_pill.dart:6, :10-12; legacy
    // docs/superpowers/specs/2026-10-02-step-timeline-states-design.md:14-21): tenue = falta, llenándose = en curso, sólida =
    // cumplida o sin objetivo, hueca = intentada y no cumplida.
    public enum StepPillLook
    {
        Pending,
        Current,
        Solid,
        Hollow,
    }

    // Lo que se pinta de una píldora, sobre el color BASE de su tipo (StepTypeInfo.Color; es un gráfico: ART_BIBLE §2.3).
    // Struct sin asignaciones: la fila la pide por píldora en cada repintado (02-tech-design §2.1 de la It. 4b).
    public readonly struct PillPaint
    {
        public PillPaint(double trackAlpha, double borderAlpha, double borderWidthBase, double fillFraction, double highlightAlpha)
        {
            TrackAlpha = trackAlpha;
            BorderAlpha = borderAlpha;
            BorderWidthBase = borderWidthBase;
            FillFraction = fillFraction;
            HighlightAlpha = highlightAlpha;
        }

        // El fondo: 0,18 pendiente y hueca; 0,30 en curso; 0,85 sólida (step_pill.dart:30-32, :44-54).
        public double TrackAlpha { get; }

        // 0 sin borde; 0,85 la hueca (:49-51).
        public double BorderAlpha { get; }

        // 0 o 1,5 (× s).
        public double BorderWidthBase { get; }

        // Solo en curso con progreso > 0, acotado a [0; 1] (:71-76); si no, 0 = sin relleno.
        public double FillFraction { get; }

        // 0,25 en curso con relleno (el brillo de 1·s, DN4b-19); si no, 0.
        public double HighlightAlpha { get; }

        public bool HasFill => FillFraction > 0;

        public bool HasBorder => BorderWidthBase > 0;
    }

    // La regla de cada píldora (02-tech-design §2.1 de la It. 4b; AC-WKT-14-08, -09, -13, AC-WKT-15-01 a -05; Q-51, Q-52; LT-46).
    public static class StepPills
    {
        // step_pill.dart:30-32 y :51.
        public const double PendingAlpha = 0.18;
        public const double CurrentTrackAlpha = 0.30;
        public const double DoneAlpha = 0.85;
        public const double MissedBorderWidth = 1.5;

        // El brillo del relleno de la en curso (04-design-notes §3.2, DN4b-19): una línea de 1·s blanca al 25 %.
        public const double HighlightAlpha = 0.25;

        // immersive_workout_screen.dart:367-371: antes del paso en curso, hecha con su resultado (Met → sólida, Missed → hueca,
        // null = sin objetivo → sólida, step_pill.dart:52-54); el paso en curso; después, pendiente. Q-52 sale solo: con la rutina
        // terminada el índice queda en el último (workout_provider.dart:203-216), así que el último sigue "en curso" y su
        // resultado, que el motor sí calculó (:204), no se ve. El resultado de un paso que todavía no se hizo tampoco se ve.
        public static StepPillLook LookOf(int index, int currentIndex, StepResult? result)
        {
            if (index < currentIndex)
            {
                return result == StepResult.Missed ? StepPillLook.Hollow : StepPillLook.Solid;
            }

            return index == currentIndex ? StepPillLook.Current : StepPillLook.Pending;
        }

        // workout_provider.dart:31-36 (Q-51): solo se llena un paso por tiempo con duración > 0. Por distancia, sin meta, 0 s o
        // duración negativa: en curso con la pista sola.
        public static bool Fills(IntervalStep step)
        {
            if (step == null)
            {
                throw new ArgumentNullException(nameof(step));
            }

            return step.DurationSeconds.HasValue && step.DurationSeconds.Value > 0;
        }

        // step_pill.dart:30-55 y :71-76. progress solo cuenta para Current (LT-46): el relleno es clamp(progress; 0; 1) y con 0
        // no hay relleno (`if (state == active && progress > 0)`, :71). Sin marco en la en curso (commit 3ae0f3a).
        public static PillPaint Paint(StepPillLook look, double progress)
        {
            switch (look)
            {
                case StepPillLook.Pending:
                    return new PillPaint(PendingAlpha, 0, 0, 0, 0);
                case StepPillLook.Current:
                    var fill = double.IsNaN(progress) || progress <= 0 ? 0 : progress > 1 ? 1 : progress;
                    return new PillPaint(CurrentTrackAlpha, 0, 0, fill, fill > 0 ? HighlightAlpha : 0);
                case StepPillLook.Solid:
                    return new PillPaint(DoneAlpha, 0, 0, 0, 0);
                case StepPillLook.Hollow:
                    return new PillPaint(PendingAlpha, DoneAlpha, MissedBorderWidth, 0, 0);
                default:
                    throw new ArgumentOutOfRangeException(nameof(look), look, "StepPills: aspecto desconocido.");
            }
        }
    }
}
