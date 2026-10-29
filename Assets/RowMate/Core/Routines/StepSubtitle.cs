using RowMate.Core.Localization;
using RowMate.Core.Models;

namespace RowMate.Core.Routines
{
    // Subtítulo de una fila del editor (AC-RUT-12-06, AC-RUT-04-06; legacy: lib/features/routines/routine_editor_screen.dart
    // :345-349) partido para dibujar el 📈 como el ícono trending_up (DV-10, Q-10):
    // - Text: el texto completo, IGUAL al del legacy, con el 📈 (StepLabels.EditorSubtitle). Es lo que comparan los tests.
    // - Before, HasTrend, After: lo de antes del 📈, si está, y lo de después (con el espacio que sigue al emoji:
    //   "5m  ·  18 spm · " + ícono + " SPM +2/rep"). Sin progresiones: Before = Text, After = "".
    // La partición es la de TrendText (la misma del HUD de la It. 4a, DV-35).
    public sealed record StepSubtitle(string Text, string Before, bool HasTrend, string After)
    {
        // U+1F4C8 (CHART WITH UPWARDS TREND), un par sustituto en UTF-16.
        public const string TrendEmoji = TrendText.TrendEmoji;

        public static StepSubtitle For(IntervalStep step, Localizer l10n)
        {
            var parts = TrendText.Split(StepLabels.EditorSubtitle(step, l10n));
            return new StepSubtitle(parts.Text, parts.Before, parts.HasTrend, parts.After);
        }
    }
}
