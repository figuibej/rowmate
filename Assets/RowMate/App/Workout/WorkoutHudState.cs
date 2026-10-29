using RowMate.Core.Models;
using RowMate.Core.Workout;

namespace RowMate.App.Workout
{
    // Hacia dónde ir fuera de objetivo (AC-WKT-10-05, D-ART-01): ▲ en SPM y Watts (hay que subir), ▼ en Split (hay que bajar).
    public enum TargetArrow
    {
        None,
        Up,
        Down,
    }

    // Una tarjeta con semáforo posible (SPM, Split, Watts), con los textos ya formateados (02-tech-design §2.11 de la It. 4a).
    public readonly struct HudMetric
    {
        public HudMetric(string value, double rollValue, bool hasTarget, bool offTarget, TargetArrow arrow, string? targetText)
        {
            Value = value;
            RollValue = rollValue;
            HasTarget = hasTarget;
            OffTarget = offTarget;
            Arrow = arrow;
            TargetText = targetText;
        }

        // El texto exacto (WorkoutFormats).
        public string Value { get; }

        // Para el sentido del rodillo (TabularNumber).
        public double RollValue { get; }

        public bool HasTarget { get; }

        public bool OffTarget { get; }

        // None si cumple o no tiene objetivo.
        public TargetArrow Arrow { get; }

        // hudTarget ("obj. 22", "target 160", "obj. 02:05"); null sin objetivo (D-ART-09).
        public string? TargetText { get; }
    }

    // La pastilla de objetivo del paso, partida en su 📈 (DV-35): Text es el texto completo del legacy.
    public sealed record HudTargetLabel(string Text, string Before, bool HasTrend, string After);

    // La fila de información de la barra de pasos (F-WKT-14; AC-WKT-14-01 a -06).
    public sealed record HudStepRow(StepType Type, string TypeText, HudTargetLabel? Target, string Countdown, double CountdownValue,
                                    bool EndingSoon, StepType? UpNextType, string? UpNextTypeText);

    // El HUD con todo ya formateado (F-WKT-10, -13, -14, -16, -17, -20). Instant: copia de WorkoutSnapshot.Instant, true en el
    // primer estado después del segundo plano (la vista lo aplica sin animar: AC-WKT-02-08). Los rótulos fijos (hudSpm,
    // hudFreeTitle…) los pone la vista desde el Localizer.
    public sealed record WorkoutHudState(long Revision, WorkoutPhase Phase, bool IsRoutine, HudMetric Spm, HudMetric Split, HudMetric Watts,
                                         string Kcal, int KcalValue, string? Bpm, int BpmValue, string Clock, int ClockValue,
                                         string Distance, int DistanceValue, HudStepRow? Step, bool ControlsEnabled, bool ShowResume,
                                         bool Instant);
}
