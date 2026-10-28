using System.Collections.Generic;
using RowMate.Core.Models;

namespace RowMate.App.Routines
{
    // Estado de la colección (F-RUT-09): la fase y las rutinas en el orden de la lista (la más nueva arriba, Q-18). Con
    // LoadFailed la lista está vacía. Cada cambio trae una lista nueva (nunca se modifica la que ya se emitió).
    public sealed record RoutinesState(RoutinesPhase Phase, IReadOnlyList<Routine> Routines);
}
