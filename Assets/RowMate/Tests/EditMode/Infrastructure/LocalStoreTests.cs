using System;
using System.Threading;
using NUnit.Framework;
using RowMate.Core.Models;
using RowMate.Infrastructure.Database;
using RowMate.Infrastructure.Threading;
using RowMate.Tests.EditMode.Workout;
using RowMate.Tests.Support;
using static RowMate.Tests.EditMode.Infrastructure.DatabaseFileState;

namespace RowMate.Tests.EditMode.Infrastructure
{
    // LST: la base local de la app (02-tech-design §3.5 de la It. 4a): UNA RowMateDatabase (una conexión, un ejecutor) para
    // rutinas y sesiones. Los repositorios no son dueños de la base; Dispose espera lo encolado y cierra la conexión.
    [TestFixture]
    public sealed class LocalStoreTests
    {
        private RecordingLogger _logger = null!;
        private TempDatabase _temp = null!;

        [SetUp]
        public void SetUp()
        {
            _logger = new RecordingLogger();
            _temp = new TempDatabase();
        }

        [TearDown]
        public void TearDown()
        {
            _temp.Dispose();
        }

        [Test, Category("AC-HIS-17-07")]
        public void OneDatabaseForBothRepositories()
        {
            using (var store = _temp.Store(_logger))
            {
                Assert.That(store.Routines.Database, Is.SameAs(store.Database));
                Assert.That(store.Sessions.Database, Is.SameAs(store.Database));
                var routine = Done(store.Routines.InsertAsync(RoutineSeeds.Corta()));
                var id = Done(store.Sessions.CreateAsync(new WorkoutSession(WorkoutHarness.SessionStart)
                {
                    RoutineId = routine.Id, RoutineName = routine.Name,
                }));
                Assert.That(Done(store.Database.QueryRawForTestsAsync("SELECT r.name, s.routine_name FROM routines r, workout_sessions s WHERE s.id = ?", id))[0],
                    Is.EqualTo(new object?[] { "Corta", "Corta" }), "el mismo archivo y la misma conexión");

                // El repositorio de rutinas no es dueño: disponerlo no cierra la base de las sesiones.
                store.Routines.Dispose();
                Assert.That(Done(store.Sessions.GetByIdAsync(id))!.RoutineName, Is.EqualTo("Corta"));
            }
        }

        [Test, Category("AC-WKT-06-10")]
        public void DisposeFinishesTheQueuedWork()
        {
            // Lo que un motor dispuesto dejó encolado (el alta y un lote, detrás de un trabajo que ocupa la base 200 ms) queda
            // escrito: Dispose espera el ejecutor y cierra la conexión en su hilo. Otra conexión lo lee después.
            var executor = new SerialExecutor("RowMate.Db.Test", _logger);
            var store = _temp.Store(_logger, executor);
            using (var gate = new ManualResetEventSlim(false))
            {
                Assert.That(store.Sessions.ListAsync().Wait(TimeSpan.FromSeconds(5)), Is.True);
                executor.Run(() => gate.Wait(TimeSpan.FromSeconds(5)));
                var creation = store.Sessions.CreateAsync(new WorkoutSession(WorkoutHarness.SessionStart));
                store.Sessions.AddPointsAfterCreateAsync(creation, new[] { new DataPoint(0, 305), new DataPoint(0, 310) });
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    Thread.Sleep(200);
                    gate.Set();
                });
                store.Dispose();
                Assert.That(creation.IsCompleted, Is.True, "Dispose esperó lo encolado");
                store.Dispose();
            }

            using (var other = _temp.Open(_logger))
            {
                Assert.That(Done(other.QueryRawForTestsAsync("SELECT count(*) FROM workout_sessions"))[0][0], Is.EqualTo(1L));
                Assert.That(Done(other.QueryRawForTestsAsync("SELECT count(*) FROM data_points WHERE session_id = 1"))[0][0], Is.EqualTo(2L));
            }
        }

        [Test]
        public void ArgumentsAreChecked()
        {
            Assert.Throws<ArgumentNullException>(() => new LocalStore(null!, _logger));
            using (var database = _temp.Open(_logger))
            {
                Assert.Throws<ArgumentNullException>(() => new LocalStore(database, null!));
            }
        }
    }
}
