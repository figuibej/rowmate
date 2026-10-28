using System;
using RowMate.Core.Text;

namespace RowMate.Core.Ports
{
    // IRoutineRepository.UpdateAsync de una rutina que no está en la base (borrada en otro lado, o sin Id): el reemplazo
    // de los pasos se deshace y la base queda como estaba (02-tech-design §3.1).
    public sealed class RoutineNotFoundException : Exception
    {
        public RoutineNotFoundException(long? id)
            : base(id.HasValue ? "No existe la rutina " + Fmt.Integer(id.Value) + "." : "La rutina no tiene id: no se puede actualizar.")
        {
            RoutineId = id;
        }

        public long? RoutineId { get; }
    }
}
