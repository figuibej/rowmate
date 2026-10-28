using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using RowMate.App.Routines;
using RowMate.Core.Common;
using RowMate.Core.Models;
using RowMate.Core.Ports;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Routines
{
    // RST: la colección de rutinas (F-RUT-09; RoutinesProvider, lib/features/routines/routines_provider.dart:6-53, con Q-13 y
    // Q-18). Repositorios en memoria o guionados (demoras por FakeScheduler); SQLite real donde importa la fecha guardada.
    [TestFixture]
    public sealed class RoutinesStoreTests
    {
        private FakeScheduler _scheduler = null!;
        private RecordingLogger _logger = null!;
        private SequenceClock _clock = null!;

        [SetUp]
        public void SetUp()
        {
            _scheduler = new FakeScheduler();
            _logger = new RecordingLogger();
            _clock = new SequenceClock();
        }

        // AC-RUT-09-01: arranca en Loading; una carga en curso no se duplica; después, la lista en orden.
        [Test, Category("AC-RUT-09-01")]
        public void LoadsOnceAtStart()
        {
            var repository = new ScriptedRoutineRepository(_scheduler, RoutineSeeds.Lista.Routines) { LoadDelay = TimeSpan.FromSeconds(2) };
            var store = new RoutinesStore(repository, _clock, _logger);
            Assert.That(store.State.Value.Phase, Is.EqualTo(RoutinesPhase.Loading), "= _loading del constructor (:12-14)");
            Assert.That(store.State.Value.Routines, Is.Empty);
            var first = store.LoadAsync();
            var second = store.LoadAsync();
            Assert.That(second, Is.SameAs(first), "Reintentar mientras carga = nada");
            Assert.That(repository.LoadCalls, Is.EqualTo(1));
            _scheduler.Advance(TimeSpan.FromSeconds(2));
            Assert.That(first.IsCompleted, Is.True);
            Assert.That(store.State.Value.Phase, Is.EqualTo(RoutinesPhase.Ready));
            Assert.That(Names(store), Is.EqualTo(new[] { "Escalera SPM", "Pirámide 500", "Solo calentamiento", "Intervalos 4×" }));
            Assert.That(_logger.Lines, Is.Empty);
        }

        // AC-RUT-09-02 (caso 21): alta arriba.
        [Test, Category("AC-RUT-09-02")]
        public void CreateGoesFirst()
        {
            var store = Loaded(new InMemoryRoutineRepository(RoutineSeeds.Lista.Routines));
            var created = Done(store.CreateAsync("Nueva", "", new[] { RoutineSeeds.Step(StepType.Work, dur: 60) }));
            Assert.That(created, Is.Not.Null);
            Assert.That(Names(store), Is.EqualTo(new[] { "Nueva", "Escalera SPM", "Pirámide 500", "Solo calentamiento", "Intervalos 4×" }));
            Assert.That(store.State.Value.Routines[0].Id, Is.EqualTo(created!.Id));
            Assert.That(store.Find(created.Id!.Value), Is.SameAs(store.State.Value.Routines[0]));
        }

        // AC-RUT-09-02: edición en su lugar (:41-43).
        [Test, Category("AC-RUT-09-02")]
        public void UpdateKeepsPosition()
        {
            var store = Loaded(new InMemoryRoutineRepository(RoutineSeeds.Lista.Routines));
            var solo = store.State.Value.Routines[2];
            Assert.That(solo.Name, Is.EqualTo("Solo calentamiento"));
            Assert.That(Done(store.UpdateAsync(solo with { Name = "Solo calentamiento 2" })), Is.Not.Null);
            Assert.That(Names(store), Is.EqualTo(new[] { "Escalera SPM", "Pirámide 500", "Solo calentamiento 2", "Intervalos 4×" }), "sigue tercera");
        }

        // AC-RUT-09-02: la baja la quita y las otras no se mueven.
        [Test, Category("AC-RUT-09-02")]
        public void DeleteKeepsOrder()
        {
            var store = Loaded(new InMemoryRoutineRepository(RoutineSeeds.Lista.Routines));
            var piramide = store.State.Value.Routines[1];
            Assert.That(Done(store.DeleteAsync(piramide.Id!.Value)), Is.True);
            Assert.That(Names(store), Is.EqualTo(new[] { "Escalera SPM", "Solo calentamiento", "Intervalos 4×" }));
            Assert.That(store.Find(piramide.Id.Value), Is.Null);
        }

        // AC-RUT-09-04 (Q-13): la carga que falla deja LoadFailed (nunca un indicador eterno) y Reintentar vuelve a cargar.
        [Test, Category("AC-RUT-09-04")]
        public void LoadFailureThenRetry()
        {
            var repository = new ScriptedRoutineRepository(_scheduler, RoutineSeeds.Lista.Routines) { FailLoad = true };
            var store = new RoutinesStore(repository, _clock, _logger);
            Done(store.LoadAsync());
            Assert.That(store.State.Value.Phase, Is.EqualTo(RoutinesPhase.LoadFailed));
            Assert.That(store.State.Value.Routines, Is.Empty);
            Assert.That(_logger.Lines, Is.EqualTo(new[] { "[DB] Warning: carga de rutinas: falla de prueba al cargar" }));
            repository.FailLoad = false;
            var states = new StateRecorder<RoutinesState>(store.State);
            Done(store.LoadAsync());
            Assert.That(states.Values[0].Phase, Is.EqualTo(RoutinesPhase.Loading), "Reintentar muestra el indicador");
            Assert.That(store.State.Value.Phase, Is.EqualTo(RoutinesPhase.Ready));
            Assert.That(store.State.Value.Routines.Count, Is.EqualTo(4));
        }

        // AC-RUT-09-06: el borrado que falla deja la rutina en la lista.
        [Test, Category("AC-RUT-09-06")]
        public void DeleteFailureKeepsTheRoutine()
        {
            var repository = new ScriptedRoutineRepository(_scheduler, RoutineSeeds.Lista.Routines);
            var store = Loaded(repository);
            repository.FailDelete = true;
            var states = new StateRecorder<RoutinesState>(store.State);
            Assert.That(Done(store.DeleteAsync(store.State.Value.Routines[0].Id!.Value)), Is.False);
            Assert.That(states.Count, Is.EqualTo(0), "la lista no cambia");
            Assert.That(store.State.Value.Routines.Count, Is.EqualTo(4));
            Assert.That(repository.Snapshot.Count, Is.EqualTo(4));
        }

        // AC-RUT-09-05, -06 (QA-G3-09): las fallas esperadas van al registro como Warning, nunca como Error.
        [Test, Category("AC-RUT-09-05"), Category("AC-RUT-09-06")]
        public void ExpectedFailuresAreWarnings()
        {
            var repository = new ScriptedRoutineRepository(_scheduler, RoutineSeeds.Lista.Routines) { FailLoad = true };
            var store = new RoutinesStore(repository, _clock, _logger);
            Done(store.LoadAsync());
            repository.FailLoad = false;
            Done(store.LoadAsync());
            repository.FailSave = true;
            repository.FailDelete = true;
            Assert.That(Done(store.CreateAsync("X", "", new IntervalStep[0])), Is.Null, "null = no se pudo guardar");
            Assert.That(Done(store.UpdateAsync(store.State.Value.Routines[0])), Is.Null);
            Assert.That(Done(store.DeleteAsync(store.State.Value.Routines[0].Id!.Value)), Is.False);
            Assert.That(_logger.Lines, Is.EqualTo(new[]
            {
                "[DB] Warning: carga de rutinas: falla de prueba al cargar",
                "[DB] Warning: guardado de rutina: falla de prueba al guardar",
                "[DB] Warning: guardado de rutina: falla de prueba al guardar",
                "[DB] Warning: baja de rutina: falla de prueba al borrar",
            }));
            foreach (var level in _logger.Levels)
            {
                Assert.That(level, Is.EqualTo(LogLevel.Warning));
            }

            Assert.That(_logger.Errors[1], Is.InstanceOf<InvalidOperationException>(), "con la excepción, para el detalle");
        }

        // AC-RUT-08-05 (Q-18): la fecha se guarda en UTC; con el equipo pasado de UTC−3 a UTC−5, la rutina creada 30 minutos
        // después queda ARRIBA (en el legacy, con la hora local sin zona, quedaría abajo). Con la base real.
        [Test, Category("AC-RUT-08-05")]
        public void CreatedAtIsUtcAcrossATimeZoneChange()
        {
            using (var db = new TempDatabase())
            using (var repository = db.Repository(_logger))
            {
                var clock = new ManualClock(new DateTimeOffset(2026, 10, 27, 10, 0, 0, TimeSpan.FromHours(-3)));
                var store = Loaded(repository, clock);
                var a = Done(store.CreateAsync("A", "", new[] { RoutineSeeds.Step(StepType.Work, dur: 60) }));
                clock.Now = new DateTimeOffset(2026, 10, 27, 8, 30, 0, TimeSpan.FromHours(-5));
                var b = Done(store.CreateAsync("B", "", new[] { RoutineSeeds.Step(StepType.Work, dur: 60) }));
                Assert.That(a!.CreatedAt, Is.EqualTo(new DateTimeOffset(2026, 10, 27, 13, 0, 0, TimeSpan.Zero)));
                Assert.That(b!.CreatedAt, Is.EqualTo(new DateTimeOffset(2026, 10, 27, 13, 30, 0, TimeSpan.Zero)));
                Assert.That(Names(store), Is.EqualTo(new[] { "B", "A" }));
                var reloaded = Loaded(repository, clock);
                Assert.That(Names(reloaded), Is.EqualTo(new[] { "B", "A" }), "también al volver a leer la base");
                var texts = Done(repository.Database.QueryRawForTestsAsync("SELECT created_at FROM routines ORDER BY id"));
                Assert.That(texts[0][0], Is.EqualTo("2026-10-27T13:00:00.000Z"));
                Assert.That(texts[1][0], Is.EqualTo("2026-10-27T13:30:00.000Z"));
            }
        }

        // AC-RUT-08-05: editar nunca cambia la fecha ni el lugar.
        [Test, Category("AC-RUT-08-05")]
        public void EditKeepsDateAndPlace()
        {
            var repository = new InMemoryRoutineRepository(RoutineSeeds.Lista.Routines);
            var store = Loaded(repository);
            var piramide = store.State.Value.Routines[1];
            var updated = Done(store.UpdateAsync(piramide with { Name = "Pirámide 600", CreatedAt = DateTimeOffset.MinValue }));
            Assert.That(updated!.CreatedAt, Is.EqualTo(piramide.CreatedAt), "el repositorio no toca la fecha");
            Assert.That(store.State.Value.Routines[1].Name, Is.EqualTo("Pirámide 600"));
            Assert.That(store.State.Value.Routines[1].CreatedAt, Is.EqualTo(RoutineSeeds.Day(22)));
        }

        // spec §0.4 + QA-G3-19: una lectura del reloj de creación por alta (un alta que falla toma igual su minuto).
        [Test, Category("AC-RUT-08-05")]
        public void CreationClockIsReadOncePerCreate()
        {
            var repository = new ScriptedRoutineRepository(_scheduler);
            var store = Loaded(repository);
            var first = Done(store.CreateAsync("1", "", new IntervalStep[0]));
            repository.FailSave = true;
            Assert.That(Done(store.CreateAsync("2", "", new IntervalStep[0])), Is.Null);
            repository.FailSave = false;
            var third = Done(store.CreateAsync("3", "", new IntervalStep[0]));
            Assert.That(_clock.Reads, Is.EqualTo(3));
            Assert.That(first!.CreatedAt, Is.EqualTo(SequenceClock.DefaultStart));
            Assert.That(third!.CreatedAt, Is.EqualTo(SequenceClock.DefaultStart.AddMinutes(2)), "el alta que falló tomó 13:01");
        }

        // AC-RUT-08-08: un tipo desconocido hace fallar la carga entera (P-07) y la pantalla lo muestra (no carga eterna).
        [Test, Category("AC-RUT-08-08")]
        public void UnknownTypeGivesLoadFailed()
        {
            using (var db = new TempDatabase())
            using (var repository = db.Repository(_logger))
            {
                RoutineSeeds.Apply(repository, RoutineSeeds.RaraSprint);
                _logger.Clear();
                var store = new RoutinesStore(repository, _clock, _logger);
                Done(store.LoadAsync());
                Assert.That(store.State.Value.Phase, Is.EqualTo(RoutinesPhase.LoadFailed));
                Assert.That(_logger.Lines.Count, Is.EqualTo(1));
                Assert.That(_logger.Lines[0], Does.StartWith("[DB] Warning: carga de rutinas: "));
                Assert.That(_logger.Lines[0], Does.Contain("sprint"));
            }
        }

        // QA-G3-09: lo que vuelve después de Dispose no cambia el estado ni emite.
        [Test, Category("AC-RUT-09-01")]
        public void LateResultsAfterDisposeChangeNothing()
        {
            var repository = new ScriptedRoutineRepository(_scheduler, RoutineSeeds.Lista.Routines) { LoadDelay = TimeSpan.FromSeconds(1) };
            var store = new RoutinesStore(repository, _clock, _logger);
            var load = store.LoadAsync();
            var states = new StateRecorder<RoutinesState>(store.State);
            store.Dispose();
            _scheduler.Advance(TimeSpan.FromSeconds(1));
            Assert.That(load.IsCompleted, Is.True);
            Assert.That(states.Count, Is.EqualTo(0));
            Assert.That(store.State.Value.Phase, Is.EqualTo(RoutinesPhase.Loading));
            Assert.That(Done(store.CreateAsync("X", "", new IntervalStep[0])), Is.Null, "un store dispuesto no escribe");
            Assert.That(repository.InsertCalls, Is.EqualTo(0));
            Assert.That(store.LoadAsync().IsCompleted, Is.True);
            Assert.That(repository.LoadCalls, Is.EqualTo(1));
            var other = new ScriptedRoutineRepository(_scheduler) { SaveDelay = TimeSpan.FromSeconds(1) };
            var live = Loaded(other);
            var save = live.CreateAsync("Tarde", "", new IntervalStep[0]);
            var liveStates = new StateRecorder<RoutinesState>(live.State);
            live.Dispose();
            _scheduler.Advance(TimeSpan.FromSeconds(1));
            Assert.That(save.IsCompleted, Is.True);
            Assert.That(save.Result, Is.Not.Null, "el guardado ya pedido termina igual");
            Assert.That(liveStates.Count, Is.EqualTo(0));
            Assert.That(other.Snapshot.Count, Is.EqualTo(1));
        }

        // Una escritura que sale bien con la fase en LoadFailed vuelve a cargar: si la base volvió, aparece la lista entera.
        [Test, Category("AC-RUT-09-04")]
        public void SuccessfulWriteAfterAFailedLoadReloads()
        {
            var repository = new ScriptedRoutineRepository(_scheduler, RoutineSeeds.Lista.Routines) { FailLoad = true };
            var store = new RoutinesStore(repository, _clock, _logger);
            Done(store.LoadAsync());
            Assert.That(store.State.Value.Phase, Is.EqualTo(RoutinesPhase.LoadFailed));
            repository.FailLoad = false;
            Assert.That(Done(store.CreateAsync("Nueva", "", new IntervalStep[0])), Is.Not.Null);
            Assert.That(store.State.Value.Phase, Is.EqualTo(RoutinesPhase.Ready));
            Assert.That(Names(store), Is.EqualTo(new[] { "Nueva", "Escalera SPM", "Pirámide 500", "Solo calentamiento", "Intervalos 4×" }));
            Assert.That(repository.LoadCalls, Is.EqualTo(2));
        }

        private RoutinesStore Loaded(IRoutineRepository repository, IClock? clock = null)
        {
            var store = new RoutinesStore(repository, clock ?? _clock, _logger);
            Done(store.LoadAsync());
            Assert.That(store.State.Value.Phase, Is.EqualTo(RoutinesPhase.Ready));
            return store;
        }

        private static string[] Names(RoutinesStore store)
        {
            var routines = store.State.Value.Routines;
            var names = new string[routines.Count];
            for (var i = 0; i < names.Length; i++)
            {
                names[i] = routines[i].Name;
            }

            return names;
        }

        private static T Done<T>(Task<T> task)
        {
            Assert.That(task.IsCompleted, Is.True, "la tarea terminó (repositorio en memoria o en línea)");
            return task.GetAwaiter().GetResult();
        }

        private static void Done(Task task)
        {
            Assert.That(task.IsCompleted, Is.True, "la tarea terminó (repositorio en memoria o en línea)");
            task.GetAwaiter().GetResult();
        }

        // Reloj con la hora local del equipo (la zona puede cambiar entre dos lecturas).
        private sealed class ManualClock : IClock
        {
            public ManualClock(DateTimeOffset now)
            {
                Now = now;
            }

            public DateTimeOffset Now { get; set; }

            public DateTimeOffset UtcNow => Now.ToUniversalTime();

            public DateTimeOffset LocalNow => Now;

            public double MonotonicSeconds => 0;
        }
    }
}
