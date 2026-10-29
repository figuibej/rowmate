using System;

namespace RowMate.Core.Routines
{
    // Un texto partido en el 📈 de las progresiones, para dibujarlo como el ícono trending_up (DV-10 en el editor, DV-35 en la
    // pastilla de objetivo del HUD; 02-tech-design §2.11 de la It. 4a). Text es el texto completo, IGUAL al del legacy (con
    // el 📈): es lo que comparan los tests. Before, HasTrend, After: lo de antes del primer 📈, si está, y lo de después (con
    // el espacio que sigue al emoji). Sin 📈: Before = Text, After = "". El 📈 sale de stepTargetProgressions ("📈 {list}",
    // igual en es y en) y estos textos no llevan texto del usuario, así que el primer 📈 es siempre el de las progresiones.
    public readonly struct TrendText
    {
        // U+1F4C8 (CHART WITH UPWARDS TREND), un par sustituto en UTF-16.
        public const string TrendEmoji = "\U0001F4C8";

        private TrendText(string text, string before, bool hasTrend, string after)
        {
            Text = text;
            Before = before;
            HasTrend = hasTrend;
            After = after;
        }

        public string Text { get; }

        public string Before { get; }

        public bool HasTrend { get; }

        public string After { get; }

        public static TrendText Split(string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            var at = text.IndexOf(TrendEmoji, StringComparison.Ordinal);
            if (at < 0)
            {
                return new TrendText(text, text, false, "");
            }

            return new TrendText(text, text.Substring(0, at), true, text.Substring(at + TrendEmoji.Length));
        }
    }
}
