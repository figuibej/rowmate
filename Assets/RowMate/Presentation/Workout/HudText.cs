using RowMate.Presentation.Controls;
using UnityEngine;
using UnityEngine.UIElements;
using FontAsset = UnityEngine.TextCore.Text.FontAsset;

namespace RowMate.Presentation.Workout
{
    // Lo que el HUD necesita de las letras y que USS no tiene (QA-G4aT-14; 02-tech-design §6.5, §9.5): el tamaño EN LÍNEA (el
    // HUD no sigue la letra del sistema, AC-WKT-17-01; ART_BIBLE §10.4), medidas en em pasadas a px y la línea de base de una
    // etiqueta con las métricas de su FontAsset (faceInfo). La caja de línea de Inter mide 1,238 em (HudGeometry.LineBox).
    public static class HudText
    {
        // Una etiqueta del HUD: clase de tipo, tamaño en línea, sin eventos, sin texto enriquecido ni escapes.
        public static Label Label(string name, string typeClass, float sizeDp)
        {
            var label = new Label { name = name };
            label.AddToClassList(typeClass);
            label.pickingMode = PickingMode.Ignore;
            label.enableRichText = false;
            label.parseEscapeSequences = false;
            label.style.fontSize = sizeDp;
            return label;
        }

        // Distancia del borde de arriba de la etiqueta a su línea de base, en dp (la primera línea): el ascendente del FontAsset
        // al tamaño dado. Sin FontAsset todavía (antes de resolver estilos), 0,78 em, el de Inter.
        public static float BaselineFromTop(Label label, float sizeDp)
        {
            var font = label.resolvedStyle.unityFontDefinition.fontAsset;
            return BaselineFromTop(font, sizeDp);
        }

        public static float BaselineFromTop(FontAsset? font, float sizeDp)
        {
            if (font == null)
            {
                return 0.78f * sizeDp;
            }

            var face = font.faceInfo;
            if (!(face.pointSize > 0))
            {
                return 0.78f * sizeDp;
            }

            var scale = sizeDp / face.pointSize * (face.scale > 0 ? face.scale : 1f);
            var lineHeight = face.lineHeight * scale;
            var ascent = face.ascentLine * scale;
            var descent = -face.descentLine * scale;
            // TextCore centra la caja ascendente-descendente en la caja de línea (el sobrante, mitad arriba y mitad abajo).
            var extra = lineHeight - (ascent + descent);
            return (extra > 0 ? extra / 2f : 0f) + ascent;
        }

        // Color con alfa multiplicado (las unidades .hud @80 %, los íconos de la banda @70 %…), sin color literal.
        public static Color WithAlpha(Color color, float alpha)
        {
            color.a *= alpha;
            return color;
        }

        public static void SetColor(VisualElement element, Color color)
        {
            if (element.style.color.keyword != StyleKeyword.Undefined || element.style.color.value != color)
            {
                element.style.color = color;
            }
        }

        // Para el arnés y las pruebas: el ancho natural de un número tabular a tamaño base (sin el achique de su fila).
        public static float BaseWidth(TabularNumber number, float fit)
        {
            return fit > 0 ? number.NaturalWidth / fit : number.NaturalWidth;
        }
    }
}
