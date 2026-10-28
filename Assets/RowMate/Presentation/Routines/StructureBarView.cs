using System;
using System.Collections.Generic;
using RowMate.Core.Design;
using RowMate.Core.Models;
using RowMate.Core.Routines;
using RowMate.Presentation.Controls;
using UnityEngine;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Routines
{
    // Mini-barra de estructura de la tarjeta (AC-RUT-10-11, Mejora EXR de ART_BIBLE §3.7; 03-rutinas/02-tech-design §4.3;
    // 04-design-notes §4.3, DN3-07): un solo elemento .rm-structure-bar (alto 4) que dibuja con Painter2D un tramo por paso de
    // la rutina EXPANDIDA, alineados a la izquierda, en el color BASE de su tipo (ART_BIBLE §3.7; Painter2D no necesita la
    // compensación de MeshColor). En cada GeometryChangedEvent toma el ancho resuelto W: pide
    // RoutineStructure.Segments(t, ⌊(W + 2) / 4⌋) y con su Total calcula StructureBarLayout.For(Total, W). Con desborde, los
    // primeros Capacity tramos a 2 dp y los últimos 6 que se apagan; con 0, la pista vacía (border.default) de todo el ancho.
    // La malla se rearma solo si cambian la rutina o el ancho; antes del primer layout no dibuja nada. Nunca sale de un renglón.
    public sealed class StructureBarView : VisualElement
    {
        public const string UssClass = "rm-structure-bar";

        private static readonly IReadOnlyList<StepType> NoTypes = new StepType[0];

        private IReadOnlyList<IntervalStep> _template = new IntervalStep[0];
        private StructureBarGeometry _geometry;
        private IReadOnlyList<StepType> _types = NoTypes;
        private float _width = -1;

        public StructureBarView()
        {
            AddToClassList(UssClass);
            pickingMode = PickingMode.Ignore;
            style.height = (float)StructureBarLayout.SegmentHeightDp;
            style.flexShrink = 0;
            generateVisualContent += Draw;
            RegisterCallback<GeometryChangedEvent>(OnGeometry);
        }

        // Tramos dibujados (0 antes del primer layout o con la pista vacía).
        public int SegmentCount => _geometry.Drawn;

        // Tipos de los tramos dibujados, en orden.
        public IReadOnlyList<StepType> SegmentTypes => _types;

        public double SegmentWidth => _geometry.SegmentWidth;

        public bool Overflow => _geometry.Overflow;

        public bool EmptyTrack => _geometry.EmptyTrack;

        public long Total { get; private set; }

        public void SetTemplate(IReadOnlyList<IntervalStep> template)
        {
            _template = template ?? throw new ArgumentNullException(nameof(template));
            Recalculate(contentRect.width);
        }

        private void OnGeometry(GeometryChangedEvent evt)
        {
            var width = evt.newRect.width;
            if (Math.Abs(width - _width) < 0.01f)
            {
                return;
            }

            Recalculate(width);
        }

        private void Recalculate(float width)
        {
            _width = width;
            if (!(width > 0))
            {
                _geometry = default;
                _types = NoTypes;
                Total = 0;
                MarkDirtyRepaint();
                return;
            }

            var capacity = (int)Math.Floor((width + StructureBarLayout.SegmentGapDp) / (StructureBarLayout.SegmentMinWidthDp + StructureBarLayout.SegmentGapDp));
            var segments = RoutineStructure.Segments(_template, capacity);
            Total = segments.Total;
            _geometry = StructureBarLayout.For(segments.Total, width);
            _types = segments.Leading;
            MarkDirtyRepaint();
        }

        private void Draw(MeshGenerationContext context)
        {
            var rect = contentRect;
            if (!(rect.width > 0))
            {
                return;
            }

            var painter = context.painter2D;
            var height = (float)StructureBarLayout.SegmentHeightDp;
            var radius = (float)StructureBarLayout.SegmentRadiusDp;
            if (_geometry.EmptyTrack)
            {
                painter.fillColor = TokenColors.ToColor(Tokens.BorderDefault);
                Rounded(painter, rect.x, rect.y, rect.width, height, radius);
                return;
            }

            var drawn = Math.Min(_geometry.Drawn, _types.Count);
            var width = (float)_geometry.SegmentWidth;
            var gap = (float)StructureBarLayout.SegmentGapDp;
            for (var i = 0; i < drawn; i++)
            {
                var color = TokenColors.ToColor(StepTypeInfo.Color(_types[i]));
                color.a *= (float)StructureBarLayout.SegmentAlpha(_geometry, i);
                painter.fillColor = color;
                Rounded(painter, rect.x + i * (width + gap), rect.y, width, height, Math.Min(radius, width / 2));
            }
        }

        private static void Rounded(Painter2D painter, float x, float y, float w, float h, float r)
        {
            painter.BeginPath();
            painter.MoveTo(new Vector2(x + r, y));
            painter.ArcTo(new Vector2(x + w, y), new Vector2(x + w, y + h), r);
            painter.ArcTo(new Vector2(x + w, y + h), new Vector2(x, y + h), r);
            painter.ArcTo(new Vector2(x, y + h), new Vector2(x, y), r);
            painter.ArcTo(new Vector2(x, y), new Vector2(x + w, y), r);
            painter.ClosePath();
            painter.Fill();
        }
    }
}
