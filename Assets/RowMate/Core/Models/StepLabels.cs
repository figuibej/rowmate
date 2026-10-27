using System;
using System.Collections.Generic;
using RowMate.Core.Localization;
using RowMate.Core.Text;

namespace RowMate.Core.Models
{
    // Textos de un paso (F-RUT-03, F-RUT-04), con las claves stepDuration*, stepTarget* y stepProgression* del catálogo
    // (Q-01): mismo texto que el legacy en es y en (AC-RUT-03-05). Las uniones (" · ", ", ") son formato fijo y no se
    // traducen. Los números con ~/ y % de Dart (DartMath) y Fmt.Integer; los splits con TimeFormats.MinSec.
    public static class StepLabels
    {
        // Une vatios, spm, split y progresiones (interval_step.dart:108).
        public const string PartSeparator = " · ";

        // Une las progresiones dentro de stepTargetProgressions (interval_step.dart:105).
        public const string ProgressionSeparator = ", ";

        // Subtítulo del editor: dos espacios a cada lado del punto medio (routine_editor_screen.dart:345-349).
        public const string SubtitleSeparator = "  ·  ";

        // legacy: interval_step.dart:67-77 (durationLabel). Sin horas; negativos como Dart: -30 → "30s", -60 → "0s",
        // -90 → "30s" (m = -2 no es > 0).
        public static string Duration(IntervalStep step, Localizer l10n)
        {
            Check(step, l10n);
            if (step.DurationSeconds.HasValue)
            {
                long d = step.DurationSeconds.Value;
                var m = DartMath.TruncDiv(d, 60);
                var s = DartMath.Mod(d, 60);
                if (m > 0 && s > 0)
                {
                    return l10n.Format(L10nKeys.StepDurationMinSec, MessageArg.Number("m", m), MessageArg.Number("s", s));
                }

                if (m > 0)
                {
                    return l10n.Format(L10nKeys.StepDurationMin, MessageArg.Number("m", m));
                }

                return l10n.Format(L10nKeys.StepDurationSec, MessageArg.Number("s", s));
            }

            if (step.DistanceMeters.HasValue)
            {
                return l10n.Format(L10nKeys.StepDurationMeters, MessageArg.Number("meters", step.DistanceMeters.Value));
            }

            return l10n.Get(L10nKeys.StepDurationNone);
        }

        // legacy: interval_step.dart:79-109 (targetLabel). Vacío si no hay nada que mostrar.
        public static string Targets(IntervalStep step, Localizer l10n)
        {
            Check(step, l10n);
            var parts = new List<string>(4);
            if (step.TargetWattsMin.HasValue && step.TargetWattsMax.HasValue)
            {
                parts.Add(l10n.Format(L10nKeys.StepTargetWattsRange,
                    MessageArg.Number("min", step.TargetWattsMin.Value), MessageArg.Number("max", step.TargetWattsMax.Value)));
            }
            else if (step.TargetWattsMin.HasValue)
            {
                parts.Add(l10n.Format(L10nKeys.StepTargetWattsMin, MessageArg.Number("min", step.TargetWattsMin.Value)));
            }

            if (step.TargetSpm.HasValue)
            {
                parts.Add(l10n.Format(L10nKeys.StepTargetSpm, MessageArg.Number("spm", step.TargetSpm.Value)));
            }

            if (step.TargetSplitSeconds.HasValue)
            {
                // Sin piso: 0 → "00:00", -65 → "-1:55" (AC-RUT-04-04).
                parts.Add(l10n.Format(L10nKeys.StepTargetSplit, MessageArg.Text("split", TimeFormats.MinSec(step.TargetSplitSeconds.Value))));
            }

            // Progresiones ≠ 0 en orden SPM, vatios, split; se muestran aunque falte el objetivo que modifican.
            var progressions = new List<string>(3);
            AddProgression(progressions, l10n, L10nKeys.StepProgressionSpm, step.ProgressionSpm);
            AddProgression(progressions, l10n, L10nKeys.StepProgressionWatts, step.ProgressionWatts);
            AddProgression(progressions, l10n, L10nKeys.StepProgressionSplit, step.ProgressionSplitSeconds);
            if (progressions.Count > 0)
            {
                parts.Add(l10n.Format(L10nKeys.StepTargetProgressions,
                    MessageArg.Text("list", string.Join(ProgressionSeparator, progressions))));
            }

            return string.Join(PartSeparator, parts);
        }

        // legacy: routine_editor_screen.dart:345-349: solo la duración si no hay objetivos; si no "{duración}  ·  {objetivos}".
        public static string EditorSubtitle(IntervalStep step, Localizer l10n)
        {
            var duration = Duration(step, l10n);
            var targets = Targets(step, l10n);
            return targets.Length == 0 ? duration : duration + SubtitleSeparator + targets;
        }

        // '${p > 0 ? '+' : ''}$p' (interval_step.dart:96, :99, :102): "+2", "-1".
        private static void AddProgression(List<string> progressions, Localizer l10n, string key, int? progression)
        {
            if (!progression.HasValue || progression.Value == 0)
            {
                return;
            }

            var p = progression.Value;
            var delta = (p > 0 ? "+" : "") + Fmt.Integer(p);
            progressions.Add(l10n.Format(key, MessageArg.Text("delta", delta)));
        }

        private static void Check(IntervalStep step, Localizer l10n)
        {
            if (step == null)
            {
                throw new ArgumentNullException(nameof(step));
            }

            if (l10n == null)
            {
                throw new ArgumentNullException(nameof(l10n));
            }
        }
    }
}
