using System.Collections.Generic;
using RowMate.Core.Design;
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

    // Un fragmento de la pastilla de objetivo (It. 4b; AC-WKT-14-03; DN4b-16): cada parte separada por " · " (U+00B7 con un
    // espacio a cada lado); la tendencia con su texto ("📈 SPM +2/rep") es UNO, con IsTrend. La pastilla se corta por
    // fragmentos enteros, nunca en medio de un número (DV-45).
    public sealed record HudTargetFragment(string Text, bool IsTrend);

    // La pastilla de objetivo del paso, partida en su 📈 (DV-35): Text es el texto completo del legacy. It. 4b: + Fragments, los
    // fragmentos en su orden (se arma una vez por paso, no por cuadro).
    public sealed record HudTargetLabel(string Text, string Before, bool HasTrend, string After)
    {
        public IReadOnlyList<HudTargetFragment> Fragments { get; init; } = System.Array.Empty<HudTargetFragment>();
    }

    // La fila de información de la barra de pasos (F-WKT-14; AC-WKT-14-01 a -06).
    public sealed record HudStepRow(StepType Type, string TypeText, HudTargetLabel? Target, string Countdown, double CountdownValue,
                                    bool EndingSoon, StepType? UpNextType, string? UpNextTypeText);

    // La fila de píldoras (It. 4b; F-WKT-15; §2.11): los tipos de la rutina expandida (el mismo objeto toda la sesión) y los
    // aspectos (el mismo objeto mientras no cambian el paso ni un resultado: rehacerlo cuesta O(n) solo ahí). Elapsed y Duration
    // (null si el paso no se llena, Q-51) alimentan PillProgress; Running = en marcha; EndingSoon, el halo de los últimos 5 s.
    public sealed record HudPillRow(IReadOnlyList<StepType> Types, IReadOnlyList<StepPillLook> Looks, int Current, int Elapsed, int? Duration,
                                    bool Running, bool EndingSoon);

    // El sello del último paso que cerró solo con objetivos (It. 4b; §2.5): Title = hudSealMet / hudSealMissed; Detail = los
    // fragmentos unidos por " · "; At = el instante monótono del cierre (la vista decide si está en sus 2 s).
    public sealed record HudSeal(int StepIndex, StepResult Verdict, string Title, string Detail, double At);

    // El chip de fase de la barra libre (It. 4b; §2.7): el texto del catálogo, el color base (tinte y borde), el del texto y el
    // punto en vivo.
    public sealed record HudPhaseChip(string Text, ColorToken Base, ColorToken TextColor, bool LiveDot);

    // Los totales del diálogo de rutina completada con el formato del legacy (immersive_workout_screen.dart:965-991): el reloj
    // en MM:SS o H:MM:SS, la distancia "{n} m" sin miles y las kcal; con sus valores para el conteo (CountUp).
    public sealed record HudCompletion(string Time, int TimeValue, string Distance, int DistanceValue, string Calories, int CaloriesValue);

    // El HUD con todo ya formateado (F-WKT-10, -13, -14, -16, -17, -20). Instant: copia de WorkoutSnapshot.Instant, true en el
    // primer estado después del segundo plano (la vista lo aplica sin animar: AC-WKT-02-08). Los rótulos fijos (hudSpm,
    // hudFreeTitle…) los pone la vista desde el Localizer.
    // It. 4b (§2.11): + init Session, EndedBy, Pills, BigCount, Seal, Chip, Completion y CloseReturned (el cierre volvió: el
    // Terminar del diálogo vuelve a reposo en el acto).
    public sealed record WorkoutHudState(long Revision, WorkoutPhase Phase, bool IsRoutine, HudMetric Spm, HudMetric Split, HudMetric Watts,
                                         string Kcal, int KcalValue, string? Bpm, int BpmValue, string Clock, int ClockValue,
                                         string Distance, int DistanceValue, HudStepRow? Step, bool ControlsEnabled, bool ShowResume,
                                         bool Instant)
    {
        public long Session { get; init; }

        public WorkoutEndKind? EndedBy { get; init; }

        public HudPillRow? Pills { get; init; }

        public int? BigCount { get; init; }

        public HudSeal? Seal { get; init; }

        public HudPhaseChip? Chip { get; init; }

        public HudCompletion? Completion { get; init; }

        public bool CloseReturned { get; init; }
    }
}
