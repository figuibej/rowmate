using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RowMate.Core.Common;
using RowMate.Core.Models;
using RowMate.Core.Ports;
using RowMate.Infrastructure.Database;
using SQLite;

namespace RowMate.Infrastructure.Routines
{
    // Rutinas sobre la base propia (F-RUT-08 + F-HIS-18; legacy: lib/core/database/database_service.dart:141-212), con lo
    // que el legacy no tiene: todo o nada (cada operación en UNA transacción, AC-RUT-08-07), la baja de los pasos (Q-17), el
    // orden estable (created_at DESC, id DESC: Q-18) y la carga en dos consultas (sin N+1).
    // Con ownsDatabase (por defecto, la conducta de la It. 3) dispone la base (y su ejecutor) al disponerse; desde la It. 4a
    // LocalStore lo arma sin ser dueño (la base es de las rutinas y de las sesiones, 02-tech-design §3.5).
    public sealed class SqliteRoutineRepository : IRoutineRepository, IDisposable
    {
        private const string SelectRoutines = "SELECT id, name, description, created_at FROM routines";
        private const string SelectSteps = "SELECT * FROM interval_steps";

        private readonly RowMateDatabase _database;
        private readonly bool _ownsDatabase;

        public SqliteRoutineRepository(RowMateDatabase database, IAppLogger logger, bool ownsDatabase = true)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
            _ownsDatabase = ownsDatabase;
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }
        }

        // La base de este repositorio (los tests leen su archivo y la consultan con QueryRawForTestsAsync).
        public RowMateDatabase Database => _database;

        // :143-152 y :190-199: todas, la más nueva arriba, con sus pasos por step_order.
        public Task<IReadOnlyList<Routine>> GetAllAsync()
        {
            return _database.RunAsync<IReadOnlyList<Routine>>(connection =>
            {
                List<RoutineRow> routines = null!;
                List<IntervalStepRow> steps = null!;
                connection.RunInTransaction(() =>
                {
                    routines = connection.Query<RoutineRow>(SelectRoutines + " ORDER BY created_at DESC, id DESC");
                    steps = connection.Query<IntervalStepRow>(SelectSteps + " ORDER BY routine_id, step_order, id");
                });

                var byRoutine = new Dictionary<long, List<IntervalStep>>();
                for (var i = 0; i < steps.Count; i++)
                {
                    var step = RoutineRowMapper.ToStep(steps[i]);
                    if (!byRoutine.TryGetValue(step.RoutineId, out var list))
                    {
                        list = new List<IntervalStep>();
                        byRoutine[step.RoutineId] = list;
                    }

                    list.Add(step);
                }

                var result = new Routine[routines.Count];
                for (var i = 0; i < routines.Count; i++)
                {
                    byRoutine.TryGetValue(routines[i].Id, out var list);
                    result[i] = RoutineRowMapper.ToRoutine(routines[i], list ?? (IReadOnlyList<IntervalStep>)new IntervalStep[0]);
                }

                return result;
            });
        }

        // :206-212: la rutina con sus pasos en orden, o null si no existe (AC-RUT-08-04).
        public Task<Routine?> GetByIdAsync(long id)
        {
            return _database.RunAsync(connection =>
            {
                List<RoutineRow> routines = null!;
                List<IntervalStepRow> steps = null!;
                connection.RunInTransaction(() =>
                {
                    routines = connection.Query<RoutineRow>(SelectRoutines + " WHERE id = ?", id);
                    steps = connection.Query<IntervalStepRow>(SelectSteps + " WHERE routine_id = ? ORDER BY step_order, id", id);
                });

                if (routines.Count == 0)
                {
                    return (Routine?)null;
                }

                return RoutineRowMapper.ToRoutine(routines[0], MapSteps(steps));
            });
        }

        // :154-163: la rutina y sus pasos con step_order = 0…n−1, en una transacción. Devuelve los ids nuevos.
        public Task<Routine> InsertAsync(Routine routine)
        {
            if (routine == null)
            {
                throw new ArgumentNullException(nameof(routine));
            }

            var createdAt = RoutineRowMapper.StoredCreatedAt(routine.CreatedAt);
            return _database.RunAsync(connection =>
            {
                Routine saved = null!;
                connection.RunInTransaction(() =>
                {
                    connection.Execute("INSERT INTO routines (name, description, created_at) VALUES (?, ?, ?)",
                        routine.Name, routine.Description, RoutineRowMapper.CreatedAtText(createdAt));
                    var id = connection.ExecuteScalar<long>("SELECT last_insert_rowid()");
                    saved = routine with { Id = id, CreatedAt = createdAt, Steps = InsertSteps(connection, id, routine.Steps) };
                });

                return saved;
            });
        }

        // :165-181: nombre y descripción, y TODOS los pasos reemplazados (borrar y reinsertar con step_order 0…n−1), en una
        // transacción. created_at no se escribe (el legacy lo reescribía con el mismo valor). Si la rutina no existe:
        // RoutineNotFoundException y nada cambia.
        public Task<Routine> UpdateAsync(Routine routine)
        {
            if (routine == null)
            {
                throw new ArgumentNullException(nameof(routine));
            }

            if (!routine.Id.HasValue)
            {
                return Task.FromException<Routine>(new RoutineNotFoundException(null));
            }

            var id = routine.Id.Value;
            return _database.RunAsync(connection =>
            {
                Routine saved = null!;
                connection.RunInTransaction(() =>
                {
                    var changed = connection.Execute("UPDATE routines SET name = ?, description = ? WHERE id = ?",
                        routine.Name, routine.Description, id);
                    if (changed == 0)
                    {
                        throw new RoutineNotFoundException(id);
                    }

                    connection.Execute("DELETE FROM interval_steps WHERE routine_id = ?", id);
                    saved = routine with { Steps = InsertSteps(connection, id, routine.Steps) };
                });

                return saved;
            });
        }

        // :183-186 + Q-17: los pasos y la rutina, en una transacción. Borrar una que no existe no es un error.
        public Task DeleteAsync(long id)
        {
            return _database.RunAsync(connection =>
            {
                connection.RunInTransaction(() =>
                {
                    connection.Execute("DELETE FROM interval_steps WHERE routine_id = ?", id);
                    connection.Execute("DELETE FROM routines WHERE id = ?", id);
                });

                return true;
            });
        }

        public void Dispose()
        {
            if (_ownsDatabase)
            {
                _database.Dispose();
            }
        }

        private static IReadOnlyList<IntervalStep> InsertSteps(SQLiteConnection connection, long routineId, IReadOnlyList<IntervalStep> steps)
        {
            var saved = new IntervalStep[steps.Count];
            for (var i = 0; i < steps.Count; i++)
            {
                connection.Execute("INSERT INTO interval_steps (" + RoutineRowMapper.StepColumns + ") VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)",
                    RoutineRowMapper.StepArguments(steps[i], routineId, i));
                var stepId = connection.ExecuteScalar<long>("SELECT last_insert_rowid()");
                saved[i] = steps[i] with { Id = stepId, RoutineId = routineId, Order = i };
            }

            return saved;
        }

        private static IReadOnlyList<IntervalStep> MapSteps(List<IntervalStepRow> rows)
        {
            var steps = new IntervalStep[rows.Count];
            for (var i = 0; i < rows.Count; i++)
            {
                steps[i] = RoutineRowMapper.ToStep(rows[i]);
            }

            return steps;
        }
    }
}
