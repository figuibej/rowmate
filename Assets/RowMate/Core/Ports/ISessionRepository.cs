using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RowMate.Core.Models;

namespace RowMate.Core.Ports
{
    // Sesiones y puntos (F-WKT-06 + F-HIS-01 a -03, F-HIS-17, F-HIS-19; 02-tech-design §3.1 de la It. 4a). Contrato: las
    // operaciones se EJECUTAN EN EL ORDEN EN QUE SE LLAMAN (el SerialExecutor de la It. 3 es FIFO; los dobles de prueba
    // también lo cumplen). Cada escritura es atómica. Las fechas van en UTC con IsoDate.FormatUtc (AC-RUT-16-04) y vuelven en
    // hora local (IsoDate.Parse). Lo implementa Infrastructure (SqliteSessionRepository, SQLite real en Unity y en dotnet);
    // en pruebas, InMemorySessionRepository y ScriptedSessionRepository (Tests/Support).
    public interface ISessionRepository
    {
        // Alta (AC-WKT-06-01): el id nuevo. La tarea es la del ejecutor (sin async/await en el medio): se completa EN EL HILO
        // DE LA BASE, antes del trabajo siguiente (lo usa AddPointsAfterCreateAsync).
        Task<long> CreateAsync(WorkoutSession session);

        // Un lote de puntos, en una transacción, en orden. El SessionId de cada punto es el de la sesión.
        Task AddPointsAsync(IReadOnlyList<DataPoint> points);

        // Un lote de una sesión cuyo id todavía no volvió al hilo principal: se encola en el acto, DETRÁS del alta (FIFO), y
        // toma el id que dejó la tarea del alta; si el alta falló, no escribe nada (AC-WKT-06-15; QA-G4aT-06). Si encuentra la
        // tarea sin terminar, el contrato FIFO se rompió: falla y el lote cuenta como fallado.
        Task AddPointsAfterCreateAsync(Task<long> creation, IReadOnlyList<DataPoint> points);

        // En UNA transacción: los puntos (los de lotes que fallaron, una vez más, y el final al último) y el cierre de la fila;
        // SessionNotFoundException si no está. Cualquier falla → rollback: la sesión queda cortada con lo que ya tenía
        // (AC-WKT-06-12; QA-G4aT-13).
        Task FinishAsync(IReadOnlyList<DataPoint> points, WorkoutSession closed);

        // AC-HIS-19-01 (database_service.dart:241-246): la sesión con sus fechas en hora local, o null.
        Task<WorkoutSession?> GetByIdAsync(long id);

        // AC-HIS-19-02 (:263-287): por el tiempo del monitor, de menor a mayor (con el id para desempatar, DT4a-05).
        Task<IReadOnlyList<DataPoint>> GetPointsAsync(long sessionId);

        // La herramienta de la demo y QA (§3.6): la más nueva arriba.
        Task<IReadOnlyList<SessionListing>> ListAsync();

        // Espera acotada a que se escriba lo encolado (bloquea el hilo que llama; solo al pasar a segundo plano, con tope). No
        // espera ninguna continuación del hilo principal, así que no se traba. true = quedó todo escrito.
        bool WaitForPendingWork(TimeSpan timeout);
    }
}
