using System;

namespace RowMate.Core.Routines
{
    // Geometría de la mini-barra para un ancho W de columna (DN3-07; 04-design-notes §4.3). La calcula StructureBarView en
    // cada GeometryChangedEvent con el ancho RESUELTO; antes del primer layout (W ≤ 0) no hay nada que dibujar.
    public readonly struct StructureBarGeometry
    {
        public StructureBarGeometry(int capacity, int drawn, double segmentWidth, bool overflow, bool emptyTrack)
        {
            Capacity = capacity;
            Drawn = drawn;
            SegmentWidth = segmentWidth;
            Overflow = overflow;
            EmptyTrack = emptyTrack;
        }

        // ⌊(W + 2) / 4⌋: tramos de 2 dp con separación de 2 (64 en TV, W = 255).
        public int Capacity { get; }

        // min(Total, Capacity): tramos que se dibujan (0 con la pista vacía o antes del layout).
        public int Drawn { get; }

        // min(24; (W − 2·(n − 1)) / n), con mínimo 2; 2 con desborde.
        public double SegmentWidth { get; }

        // Total > Capacity: los últimos OverflowFadeSegments dibujados se apagan (alfa 6/7 … 1/7).
        public bool Overflow { get; }

        // Total = 0 (solo series ×0): una pista border.default de todo el ancho, del mismo alto.
        public bool EmptyTrack { get; }
    }

    // Tramos iguales alineados a la izquierda: el largo de la franja también cuenta cuántos pasos tiene la sesión
    // (spec §10.5). TV (W = 255): 1 → 24; 7 y 8 → 24 (8 × 24 + 14 = 206); 10 → 23,7; 14 → 16,36; 64 → 2,02;
    // 65 o ×2 000 000 000 → 64 tramos de 2 con desborde; 0 → pista vacía.
    public static class StructureBarLayout
    {
        public const double SegmentHeightDp = 4;
        public const double SegmentRadiusDp = 2;
        public const double SegmentGapDp = 2;
        public const double SegmentMaxWidthDp = 24;
        public const double SegmentMinWidthDp = 2;
        public const int OverflowFadeSegments = 6;

        public static StructureBarGeometry For(long total, double widthDp)
        {
            if (double.IsNaN(widthDp) || widthDp <= 0 || total < 0)
            {
                return new StructureBarGeometry(0, 0, 0, false, false);
            }

            var capacity = (int)Math.Floor((widthDp + SegmentGapDp) / (SegmentMinWidthDp + SegmentGapDp));
            if (total == 0)
            {
                return new StructureBarGeometry(capacity, 0, 0, false, true);
            }

            if (total > capacity)
            {
                return new StructureBarGeometry(capacity, capacity, SegmentMinWidthDp, true, false);
            }

            var n = (int)total;
            var width = (widthDp - SegmentGapDp * (n - 1)) / n;
            width = Math.Min(SegmentMaxWidthDp, Math.Max(SegmentMinWidthDp, width));
            return new StructureBarGeometry(capacity, n, width, false, false);
        }

        // Opacidad del tramo `index` (0…Drawn − 1): 1, salvo con desborde, donde los últimos 6 bajan 6/7, 5/7 … 1/7.
        public static double SegmentAlpha(StructureBarGeometry geometry, int index)
        {
            if (!geometry.Overflow)
            {
                return 1;
            }

            var fromEnd = geometry.Drawn - index;
            return fromEnd > OverflowFadeSegments ? 1 : fromEnd / (double)(OverflowFadeSegments + 1);
        }
    }
}
