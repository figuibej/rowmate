using RowMate.Core.Design;
using UnityEngine;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Controls
{
    // El glifo de "Exportar TCX" (It. 12; 12-exportar-tcx/02-tech-design §1.3, §8: contrato WP-B1 ↔ WP-D): download o share de
    // ExportGlyph dibujados con Painter2D en el color resuelto del elemento (el del ícono del botón: .rm-icon-button__icon), en una caja
    // de sizeDp. Sin fuente ni asignaciones al dibujar. SetShown lo muestra u oculta (el indicador de 18/2 lo reemplaza al exportar).
    public sealed class ExportGlyphView : VisualElement
    {
        public const string UssClass = "rm-export-glyph";

        private bool _shown = true;

        public ExportGlyphView(ExportGlyph.Kind kind, float sizeDp)
        {
            Kind = kind;
            SizeInDp = sizeDp;
            AddToClassList(UssClass);
            pickingMode = PickingMode.Ignore;
            style.width = sizeDp;
            style.height = sizeDp;
            style.flexShrink = 0;
            generateVisualContent += Draw;
        }

        public ExportGlyph.Kind Kind { get; }

        public float SizeInDp { get; }

        public bool IsShown => _shown;

        public void SetShown(bool shown)
        {
            _shown = shown;
            style.display = shown ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void Draw(MeshGenerationContext context)
        {
            var rect = contentRect;
            var size = Mathf.Min(rect.width, rect.height);
            if (size <= 0)
            {
                return;
            }

            var k = size / (float)ExportGlyph.ViewBox;
            var ox = rect.x + (rect.width - size) / 2;
            var oy = rect.y + (rect.height - size) / 2;
            var painter = context.painter2D;
            painter.fillColor = resolvedStyle.color;
            var contours = ExportGlyph.Contours(Kind);
            for (var c = 0; c < contours.Count; c++)
            {
                var points = contours[c];
                painter.BeginPath();
                painter.MoveTo(new Vector2(ox + (float)points[0] * k, oy + (float)points[1] * k));
                for (var i = 2; i < points.Length; i += 2)
                {
                    painter.LineTo(new Vector2(ox + (float)points[i] * k, oy + (float)points[i + 1] * k));
                }

                painter.ClosePath();
                painter.Fill();
            }
        }
    }
}
