using System;

namespace RowMate.Core.Models
{
    // Una fila de la lista de sesiones de la herramienta de la demo y de QA (02-tech-design §3.6 de la It. 4a; spec §12
    // punto 9): id, inicio (hora local), fin (null = cortada, F-HIS-21), rutina (null = libre), origen y cantidad de puntos.
    // La arma ISessionRepository.ListAsync, la más nueva arriba.
    public sealed record SessionListing(long Id, DateTimeOffset StartedAt, DateTimeOffset? FinishedAt, string? RoutineName, string Origin,
                                        long PointCount)
    {
        public bool IsCut => !FinishedAt.HasValue;
    }
}
