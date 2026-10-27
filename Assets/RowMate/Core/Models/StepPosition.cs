using System;
using RowMate.Core.Text;

namespace RowMate.Core.Models
{
    // Posición de un paso de la rutina expandida dentro de su serie (F-RUT-07; legacy: routine.dart:3-5, el record
    // StepPosition = ({String? groupId, int rep, int repCount})): serie, repetición desde 1 y total de repeticiones.
    // Un paso suelto es (null, 1, 1). Igualdad por valor, como los records de Dart.
    public readonly struct StepPosition : IEquatable<StepPosition>
    {
        public static readonly StepPosition Loose = new StepPosition(null, 1, 1);

        public StepPosition(string? groupId, int rep, int repCount)
        {
            GroupId = groupId;
            Rep = rep;
            RepCount = repCount;
        }

        public string? GroupId { get; }

        public int Rep { get; }

        public int RepCount { get; }

        public bool Equals(StepPosition other)
        {
            return string.Equals(GroupId, other.GroupId, StringComparison.Ordinal) && Rep == other.Rep && RepCount == other.RepCount;
        }

        public override bool Equals(object? obj)
        {
            return obj is StepPosition other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(GroupId, Rep, RepCount);
        }

        public static bool operator ==(StepPosition left, StepPosition right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(StepPosition left, StepPosition right)
        {
            return !left.Equals(right);
        }

        // Como el record de Dart: "(groupId: a, rep: 1, repCount: 3)".
        public override string ToString()
        {
            return "(groupId: " + (GroupId ?? "null") + ", rep: " + Fmt.Integer(Rep) + ", repCount: " + Fmt.Integer(RepCount) + ")";
        }
    }
}
