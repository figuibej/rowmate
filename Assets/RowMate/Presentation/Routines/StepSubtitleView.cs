using System;
using RowMate.Core.Design;
using RowMate.Core.Routines;
using RowMate.Presentation.Controls;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Routines
{
    // Subtítulo de una fila de paso con el ícono de tendencia (AC-RUT-12-06, AC-RUT-04-06, DV-10; 03-rutinas/02-tech-design
    // §4.4; 04-design-notes §4.6). Camino principal: un solo Label con Before + U+E8E5 (trending_up) + After; el glifo sale de
    // los respaldos de fuente de la copia de PanelTextSettings (RuntimePanelFactory) y va en texto enriquecido GENERADO por
    // código desde Tokens (<color=metric.distance><size=117%> … </size></color>: 14 dp sobre 12). Así queda en la línea y el
    // texto ajusta como cualquier párrafo.
    // Color (sonda S-B3a, punto 1): TextCore dibuja cada carácter con el alfa MÁS CHICO entre el color del elemento y el de la
    // etiqueta, así que con la base en text.secondary (blanco al 70 %) el glifo salía en metric.distance al 70 % (#3994B4 en
    // lugar de #4CC9F0). Por eso la base es opaca (text.primary, en línea: una regla de USS no la cambia) y el texto lleva su
    // propio <color> con text.secondary; medido: el glifo sale #4CC9F0 y el texto #B6BABF, igual que con la base por USS.
    // El subtítulo no lleva texto del usuario: es la única etiqueta con enableRichText (si alguna vez trajera '<', se
    // muestra sin formato). parseEscapeSequences en false y white-space: pre-wrap (los dos espacios del separador).
    // Text es el texto del legacy con el 📈 (lo que comparan los tests, AC-RUT-12-06).
    public sealed class StepSubtitleView : Label
    {
        public const string UssClass = "rm-step-row__subtitle";
        // trending_up (U+E8E5), del respaldo de íconos.
        public const string TrendGlyph = Icons.TrendingUp;

        private string _text = "";

        public StepSubtitleView()
        {
            AddToClassList(UssClass);
            AddToClassList("rm-type-body-xs");
            pickingMode = PickingMode.Ignore;
            enableRichText = true;
            parseEscapeSequences = false;
            style.color = TokenColors.ToColor(Tokens.TextPrimary);
        }

        // El texto del legacy, con el 📈 (StepSubtitle.Text).
        public string Text => _text;

        public bool HasTrend { get; private set; }

        public void SetSubtitle(StepSubtitle subtitle)
        {
            if (subtitle == null)
            {
                throw new ArgumentNullException(nameof(subtitle));
            }

            _text = subtitle.Text;
            HasTrend = subtitle.HasTrend;
            var plain = subtitle.Text.IndexOf('<') >= 0;
            enableRichText = !plain;
            var shown = plain ? subtitle.Text
                : subtitle.HasTrend ? TextMarkup(subtitle.Before) + TrendMarkup() + TextMarkup(subtitle.After)
                : TextMarkup(subtitle.Text);
            if (text != shown)
            {
                text = shown;
            }
        }

        // <color=#4CC9F0><size=117%>U+E8E5</size></color>, con el color del token (sin literales: ADR 016).
        public static string TrendMarkup()
        {
            return "<color=" + Tokens.MetricDistance.Rgb.ToHex() + "><size=117%>" + TrendGlyph + "</size></color>";
        }

        // El texto en text.secondary (#FFFFFF con alfa B2, como lo redondea el motor desde rgba(255, 255, 255, 0.7)).
        public static string TextMarkup(string text)
        {
            return string.IsNullOrEmpty(text) ? "" : "<color=" + SecondaryHex() + ">" + text + "</color>";
        }

        public static string SecondaryHex()
        {
            var token = Tokens.TextSecondary;
            return token.Rgb.ToHex() + ((int)Math.Round(token.Alpha * 255)).ToString("X2");
        }
    }
}
