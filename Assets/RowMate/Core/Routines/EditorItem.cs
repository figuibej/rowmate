using System;
using RowMate.Core.Text;

namespace RowMate.Core.Routines
{
    // Tipo de bloque de la lista del editor (legacy: _ItemType de lib/features/routines/routine_editor_screen.dart:463).
    public enum EditorItemKind
    {
        LooseStep,
        SeriesHeader,
        SeriesStep,
        SeriesFooter,
    }

    // Un bloque de la lista del editor (= _EditorItem, :465-470).
    // - StepIndex: índice en la plantilla (encabezado y pie: el primer paso de su serie, como el legacy).
    // - GroupId: el de la serie (null en un paso suelto).
    // - SeriesOrdinal: 0, 1… por serie en orden de aparición (nombres de contrato series-<k>); −1 en un paso suelto.
    // - RepeatCount: el ×N que muestra el chip = firstWhere(groupId).groupRepeatCount ?? 1 (:372-373); 1 en un paso suelto.
    public readonly struct EditorItem : IEquatable<EditorItem>
    {
        public EditorItem(EditorItemKind kind, int stepIndex, string? groupId, int seriesOrdinal, int repeatCount)
        {
            Kind = kind;
            StepIndex = stepIndex;
            GroupId = groupId;
            SeriesOrdinal = seriesOrdinal;
            RepeatCount = repeatCount;
        }

        public EditorItemKind Kind { get; }

        public int StepIndex { get; }

        public string? GroupId { get; }

        public int SeriesOrdinal { get; }

        public int RepeatCount { get; }

        public bool Equals(EditorItem other)
        {
            return Kind == other.Kind && StepIndex == other.StepIndex && string.Equals(GroupId, other.GroupId, StringComparison.Ordinal)
                && SeriesOrdinal == other.SeriesOrdinal && RepeatCount == other.RepeatCount;
        }

        public override bool Equals(object? obj)
        {
            return obj is EditorItem other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Kind, StepIndex, GroupId, SeriesOrdinal, RepeatCount);
        }

        public override string ToString()
        {
            return Kind + "(" + Fmt.Integer(StepIndex) + (GroupId == null ? "" : ", " + GroupId + " #" + Fmt.Integer(SeriesOrdinal)
                + " ×" + Fmt.Integer(RepeatCount)) + ")";
        }
    }
}
