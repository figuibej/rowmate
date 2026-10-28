using System;
using RowMate.Core.Text;

namespace RowMate.App.Routines
{
    // Qué hizo OK en el diálogo de repeticiones (AC-RUT-14-05, -06; 02-tech-design §3.5).
    public enum RepeatChangeKind
    {
        // ×Count en todos los pasos de la serie (también ×1 con un texto no numérico: :173 ?? 1). El diálogo se cierra.
        Applied,

        // ≤ 0, de cualquier largo: la serie queda como estaba (:179); Count = el N actual. El diálogo se cierra.
        Unchanged,

        // > 999 (P-01): el diálogo queda abierto con editorRepetitionsMax; Count = 999.
        TooMany,

        // Con ×Count algún objetivo progresado se pasa de su tope (AC-RUT-13-12): el diálogo queda abierto con
        // editorRepetitionsOutOfRange{Count}.
        TargetOutOfRange,
    }

    public readonly struct RepeatChange : IEquatable<RepeatChange>
    {
        public RepeatChange(RepeatChangeKind kind, int count)
        {
            Kind = kind;
            Count = count;
        }

        public RepeatChangeKind Kind { get; }

        public int Count { get; }

        // El diálogo se cierra (Applied, Unchanged) o queda abierto con su texto (TooMany, TargetOutOfRange).
        public bool ClosesTheDialog => Kind == RepeatChangeKind.Applied || Kind == RepeatChangeKind.Unchanged;

        public bool Equals(RepeatChange other)
        {
            return Kind == other.Kind && Count == other.Count;
        }

        public override bool Equals(object? obj)
        {
            return obj is RepeatChange other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Kind, Count);
        }

        public override string ToString()
        {
            return Kind + "(" + Fmt.Integer(Count) + ")";
        }
    }
}
