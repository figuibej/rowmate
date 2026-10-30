using System;
using RowMate.Core.Design;
using RowMate.Core.Localization;

namespace RowMate.Core.Workout
{
    // El chip de fase de la barra libre (F-WKT-16; AC-WKT-16-03 a -05): la clave del texto, el color base (el tinte al 15 % y
    // el borde al 50 %), el color del texto y si lleva el punto en vivo.
    public readonly struct PhaseChipSpec
    {
        public PhaseChipSpec(string key, ColorToken @base, ColorToken text, bool liveDot)
        {
            Key = key ?? throw new ArgumentNullException(nameof(key));
            Base = @base ?? throw new ArgumentNullException(nameof(@base));
            Text = text ?? throw new ArgumentNullException(nameof(text));
            LiveDot = liveDot;
        }

        // hudPhaseActive / hudPhasePaused / hudPhaseFinished.
        public string Key { get; }

        // phase.running / phase.paused / phase.finished.
        public ColorToken Base { get; }

        // phase.running, phase.paused y, en FINALIZADO, phase.finished.hud (#FF7A92; DV-44, DN4b-15).
        public ColorToken Text { get; }

        // Solo en marcha (AC-WKT-16-05).
        public bool LiveDot { get; }
    }

    // immersive_workout_screen.dart:530-547 (_PhaseChip), con DV-41 y DV-44 (02-tech-design §2.7 de la It. 4b).
    public static class PhaseChip
    {
        // 8 × 3 de relleno; el punto de 6 y su aire de 4, solo en EN MARCHA (04-design-notes §3.5; DN4b-26).
        public const double PadH = 8;
        public const double DotSlot = 10;

        // Reposo → null: el "—" del legacy (:534) nunca se ve, porque la salida conserva la última foto (DV-41).
        public static PhaseChipSpec? For(WorkoutPhase phase)
        {
            switch (phase)
            {
                case WorkoutPhase.Active:
                    return new PhaseChipSpec(L10nKeys.HudPhaseActive, Tokens.PhaseRunning, Tokens.PhaseRunning, true);
                case WorkoutPhase.Paused:
                    return new PhaseChipSpec(L10nKeys.HudPhasePaused, Tokens.PhasePaused, Tokens.PhasePaused, false);
                case WorkoutPhase.Finished:
                    return new PhaseChipSpec(L10nKeys.HudPhaseFinished, Tokens.PhaseFinished, Tokens.PhaseFinishedHud, false);
                default:
                    return null;
            }
        }

        // El ancho fijo del chip en base (DT4b-11; DN4b-26): max(EN MARCHA + DotSlot; PAUSADO; FINALIZADO) + 2·PadH, con los
        // anchos de las tres etiquetas en el idioma de la app (11/700, tracking 0,08 em). es: 101,8; en: 74,1 (04-design-notes
        // §3.5). Así el reloj nunca se corre cuando cambia la fase.
        public static double FixedWidthBase(double active, double paused, double finished)
        {
            return Math.Max(Math.Max(active + DotSlot, paused), finished) + 2 * PadH;
        }
    }
}
