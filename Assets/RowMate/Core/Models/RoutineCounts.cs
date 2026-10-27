using System;

namespace RowMate.Core.Models
{
    // Conteos de la PLANTILLA de una rutina (no de la expandida) para su resumen (F-RUT-05; legacy: routine.dart:100-102).
    public readonly struct RoutineCounts : IEquatable<RoutineCounts>
    {
        public RoutineCounts(int total, int work, int rest)
        {
            Total = total;
            Work = work;
            Rest = rest;
        }

        // Todos los pasos.
        public int Total { get; }

        // Pasos de trabajo ("series" en el resumen).
        public int Work { get; }

        // Pasos de descanso.
        public int Rest { get; }

        public bool Equals(RoutineCounts other)
        {
            return Total == other.Total && Work == other.Work && Rest == other.Rest;
        }

        public override bool Equals(object? obj)
        {
            return obj is RoutineCounts other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Total, Work, Rest);
        }
    }
}
