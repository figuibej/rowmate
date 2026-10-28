using System;
using System.IO;
using System.Threading.Tasks;
using RowMate.Core.Common;
using RowMate.Core.Models;
using RowMate.Core.Text;
using RowMate.Infrastructure.Database;
using RowMate.Infrastructure.Routines;
using RowMate.Infrastructure.Threading;
using SQLite;

namespace RowMate.Infrastructure.Diagnostics
{
    // Prueba de humo de la base en un player (R3-01; 02-tech-design §6.1): con -rowmate-db-smoke <carpeta> (solo en el Editor
    // y en builds de desarrollo), AppRoot la corre y sale. Crea una base v8 en una subcarpeta temporal propia de esa carpeta,
    // guarda y relee Escalera SPM con sus 4 pasos por el mismo repositorio de la app (con el hilo de la base real), la borra
    // y borra la subcarpeta. En Mono prueba que el binario nativo carga en un build; con IL2CPP, además, el mapeo por
    // reflexión y link.xml. La línea del resultado NUNCA lleva la carpeta ni una ruta (QA-G3-20): los informes la copian.
    public static class DatabaseSmoke
    {
        public const string Tag = "DB";
        public const string OkMessage = "humo: ok";

        // true = "[DB] humo: ok"; false = "[DB] humo: falla (<problema> <código de SQLite>)".
        public static async Task<bool> RunAsync(string folder, IAppLogger logger)
        {
            if (folder == null)
            {
                throw new ArgumentNullException(nameof(folder));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            var work = Path.Combine(folder, "rowmate-db-smoke-" + Guid.NewGuid().ToString("N"));
            string? failure = null;
            var repository = new SqliteRoutineRepository(
                new RowMateDatabase(Path.Combine(work, "smoke.db"), new SerialExecutor("RowMate.DbSmoke", logger), logger), logger);
            try
            {
                var saved = await repository.InsertAsync(Sample());
                var read = await repository.GetByIdAsync(saved.Id ?? -1);
                if (read == null || read.Name != "Escalera SPM" || read.Steps.Count != 4 || read.Steps[1].ProgressionSpm != 2
                    || read.Steps[2].GroupRepeatCount != 3)
                {
                    failure = "lectura distinta";
                }
                else
                {
                    await repository.DeleteAsync(saved.Id!.Value);
                    if (await repository.GetByIdAsync(saved.Id.Value) != null)
                    {
                        failure = "baja";
                    }
                }
            }
            catch (DatabaseOpenException error)
            {
                failure = error.Problem + (error.SqliteCode.HasValue ? " " + Fmt.Integer(error.SqliteCode.Value) : "");
            }
            catch (SQLiteException error)
            {
                failure = "SQLite " + Fmt.Integer((int)error.Result);
            }
            catch (Exception error)
            {
                failure = error.GetType().Name;
            }
            finally
            {
                repository.Dispose();
            }

            try
            {
                if (Directory.Exists(work))
                {
                    Directory.Delete(work, true);
                }
            }
            catch (Exception error)
            {
                failure = failure ?? "limpieza " + error.GetType().Name;
            }

            if (failure == null)
            {
                logger.Log(LogLevel.Info, Tag, OkMessage);
                return true;
            }

            // Sin la excepción: su mensaje o su traza podrían traer la ruta.
            logger.Log(LogLevel.Error, Tag, "humo: falla (" + failure + ")");
            return false;
        }

        // Escalera SPM de F-LISTA: calentamiento 300 s, serie ×3 [trabajo 300 s, SPM 18, +2/rep; descanso 120 s],
        // enfriamiento 300 s.
        private static Routine Sample()
        {
            return new Routine("Escalera SPM", new DateTimeOffset(2026, 10, 23, 13, 0, 0, TimeSpan.Zero))
            {
                Steps = new[]
                {
                    new IntervalStep(0, 0, StepType.Warmup) { DurationSeconds = 300 },
                    new IntervalStep(0, 1, StepType.Work)
                    {
                        DurationSeconds = 300, TargetSpm = 18, ProgressionSpm = 2, GroupId = "g_smoke", GroupRepeatCount = 3,
                    },
                    new IntervalStep(0, 2, StepType.Rest) { DurationSeconds = 120, GroupId = "g_smoke", GroupRepeatCount = 3 },
                    new IntervalStep(0, 3, StepType.Cooldown) { DurationSeconds = 300 },
                },
            };
        }
    }
}
