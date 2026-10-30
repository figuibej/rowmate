using System;
using RowMate.Core.Design;
using RowMate.Presentation.Controls;
using UnityEngine;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Workout
{
    // El check del diálogo de rutina completada (F-WKT-21; AC-WKT-21-03, -05; legacy immersive_workout_screen.dart:955-963, el
    // check_circle de 28; 04b-hud-exr/02-tech-design §3.9; 04-design-notes §3.8, §4.12; DN4b-21):
    //   #completion-check .rm-completion__check   32 × 32: el disco state.success y el trazo del check en surface.overlay
    // Lo dibuja su generateVisualContent con Painter2D: el disco escalado (0,6 → 1 en 60-300 desde que entra el diálogo,
    // ease.emphasis) y el trazo de P0 a P2 en proporción al largo (180-500, ease.move; CheckGlyph), con 0,20·R de grosor, unión
    // en inglete y extremos rectos. Lo mueve CompletionMotion (CompletionDialog). Con reducir movimiento, entero desde el primer
    // cuadro.
    public sealed class CheckMarkView : VisualElement
    {
        private float _scale = 1;
        private float _stroke = 1;

        public CheckMarkView()
        {
            name = "completion-check";
            AddToClassList("rm-completion__check");
            pickingMode = PickingMode.Ignore;
            var size = (float)HudGeometry.CompletionCheckDp;
            style.width = size;
            style.height = size;
            style.minWidth = size;
            style.flexShrink = 0;
            generateVisualContent += Draw;
        }

        public float DiscScale => _scale;

        public float Stroke => _stroke;

        public void Set(double scale, double stroke)
        {
            var s = (float)Math.Max(0, scale);
            var k = (float)Math.Max(0, Math.Min(1, stroke));
            if (Math.Abs(s - _scale) < 1e-5f && Math.Abs(k - _stroke) < 1e-5f)
            {
                return;
            }

            _scale = s;
            _stroke = k;
            MarkDirtyRepaint();
        }

        private void Draw(MeshGenerationContext context)
        {
            var w = contentRect.width;
            var h = contentRect.height;
            if (!(w > 0) || !(h > 0))
            {
                return;
            }

            var painter = context.painter2D;
            var center = new Vector2(w / 2f, h / 2f);
            var radius = Math.Min(w, h) / 2f;
            var disc = radius * _scale;
            painter.fillColor = TokenColors.ToColor(Tokens.StateSuccess);
            painter.BeginPath();
            painter.Arc(center, disc, 0, 360);
            painter.ClosePath();
            painter.Fill();

            var points = CheckGlyph.Partial(_stroke, center.x, center.y, disc, out var x0, out var y0, out var x1, out var y1, out var x2, out var y2);
            if (points < 2)
            {
                return;
            }

            painter.strokeColor = TokenColors.ToColor(Tokens.SurfaceOverlay);
            painter.lineWidth = (float)(CheckGlyph.StrokeRadii * disc);
            painter.lineJoin = LineJoin.Miter;
            painter.lineCap = LineCap.Butt;
            painter.BeginPath();
            painter.MoveTo(new Vector2((float)x0, (float)y0));
            painter.LineTo(new Vector2((float)x1, (float)y1));
            if (points > 2)
            {
                painter.LineTo(new Vector2((float)x2, (float)y2));
            }

            painter.Stroke();
        }
    }
}
