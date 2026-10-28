using System.Collections.Generic;
using System.Threading.Tasks;
using RowMate.Core.Models;

namespace RowMate.Core.Ports
{
    // Persistencia de rutinas (F-RUT-08 + F-HIS-18; legacy: lib/core/database/database_service.dart:141-212). Lo implementa
    // Infrastructure (SqliteRoutineRepository, SQLite real en Unity y en dotnet); en pruebas, InMemoryRoutineRepository y
    // ScriptedRoutineRepository (Tests/Support). Todas las operaciones son atómicas (todo o nada, AC-RUT-08-07) y las de
    // escritura devuelven la rutina tal como quedó guardada: ids nuevos, RoutineId de cada paso y Order 0…n−1.
    // Una falla llega como la excepción de la tarea (la carga falla entera ante una fila que no se puede leer: P-07).
    public interface IRoutineRepository
    {
        // La más nueva arriba (created_at DESC, id DESC; Q-18) y los pasos de cada una por step_order.
        Task<IReadOnlyList<Routine>> GetAllAsync();

        // null si no existe o se borró (AC-RUT-08-04).
        Task<Routine?> GetByIdAsync(long id);

        // Con el CreatedAt de la rutina (truncado al milisegundo); el Id que traiga se ignora.
        Task<Routine> InsertAsync(Routine routine);

        // Nombre y descripción; reemplaza TODOS los pasos; no toca la fecha de creación (AC-RUT-08-02).
        // Una rutina que no existe → RoutineNotFoundException, sin cambiar nada.
        Task<Routine> UpdateAsync(Routine routine);

        // La rutina y sus pasos (Q-17). Borrar una que no existe no es un error.
        Task DeleteAsync(long id);
    }
}
