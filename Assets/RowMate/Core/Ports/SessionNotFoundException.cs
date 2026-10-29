using System;
using RowMate.Core.Text;

namespace RowMate.Core.Ports
{
    // ISessionRepository.FinishAsync de una sesión que no está en la base (0 filas en el UPDATE, o sin id): la transacción del
    // cierre se deshace y la sesión, si existía, queda como estaba (02-tech-design §3.3 de la It. 4a).
    public sealed class SessionNotFoundException : Exception
    {
        public SessionNotFoundException(long? id)
            : base(id.HasValue ? "No existe la sesión " + Fmt.Integer(id.Value) + "." : "La sesión no tiene id: no se puede cerrar.")
        {
            SessionId = id;
        }

        public long? SessionId { get; }
    }
}
