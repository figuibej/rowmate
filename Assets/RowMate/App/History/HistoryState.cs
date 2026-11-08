using System;
using System.Collections.Generic;
using RowMate.Core.Models;

namespace RowMate.App.History
{
    // Estado de la lista del historial (F-HIS-06, F-HIS-07): la fase y las sesiones con sus estadísticas, la más nueva arriba.
    // Con LoadFailed la lista está vacía. Igualdad POR VALOR, también de la lista (DT9-02, gate m-01: SequenceEqual sobre las
    // HistoryEntry): una recarga que trae lo mismo no emite y la lista no se toca (Q-22).
    public sealed record HistoryState(HistoryPhase Phase, IReadOnlyList<HistoryEntry> Entries)
    {
        public bool Equals(HistoryState? other)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (other is null || Phase != other.Phase)
            {
                return false;
            }

            if (ReferenceEquals(Entries, other.Entries))
            {
                return true;
            }

            if (Entries == null || other.Entries == null || Entries.Count != other.Entries.Count)
            {
                return false;
            }

            for (var i = 0; i < Entries.Count; i++)
            {
                if (!Equals(Entries[i], other.Entries[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public override int GetHashCode()
        {
            var hash = HashCode.Combine(Phase, Entries?.Count ?? 0);
            if (Entries != null && Entries.Count > 0)
            {
                hash = HashCode.Combine(hash, Entries[0]);
            }

            return hash;
        }
    }
}
