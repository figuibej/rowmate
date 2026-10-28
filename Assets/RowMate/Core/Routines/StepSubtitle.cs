using System;
using RowMate.Core.Localization;
using RowMate.Core.Models;

namespace RowMate.Core.Routines
{
    // Subtítulo de una fila del editor (AC-RUT-12-06, AC-RUT-04-06; legacy: lib/features/routines/routine_editor_screen.dart
    // :345-349) partido para dibujar el 📈 como el ícono trending_up (DV-10, Q-10):
    // - Text: el texto completo, IGUAL al del legacy, con el 📈 (StepLabels.EditorSubtitle). Es lo que comparan los tests.
    // - Before, HasTrend, After: lo de antes del 📈, si está, y lo de después (con el espacio que sigue al emoji:
    //   "5m  ·  18 spm · " + ícono + " SPM +2/rep"). Sin progresiones: Before = Text, After = "".
    // El 📈 sale de stepTargetProgressions ("📈 {list}", igual en es y en) y el subtítulo no lleva texto del usuario, así
    // que el primer 📈 es siempre el de las progresiones.
    public sealed record StepSubtitle(string Text, string Before, bool HasTrend, string After)
    {
        // U+1F4C8 (CHART WITH UPWARDS TREND), un par sustituto en UTF-16.
        public const string TrendEmoji = "\U0001F4C8";

        public static StepSubtitle For(IntervalStep step, Localizer l10n)
        {
            var text = StepLabels.EditorSubtitle(step, l10n);
            var at = text.IndexOf(TrendEmoji, StringComparison.Ordinal);
            if (at < 0)
            {
                return new StepSubtitle(text, text, false, "");
            }

            return new StepSubtitle(text, text.Substring(0, at), true, text.Substring(at + TrendEmoji.Length));
        }
    }
}
