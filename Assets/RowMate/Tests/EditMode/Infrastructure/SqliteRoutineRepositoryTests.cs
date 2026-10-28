using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using RowMate.Core.Models;
using RowMate.Core.Ports;
using RowMate.Core.Routines;
using RowMate.Infrastructure.Routines;
using RowMate.Tests.Support;
using static RowMate.Tests.EditMode.Infrastructure.DatabaseFileState;

namespace RowMate.Tests.EditMode.Infrastructure
{
    // RRT: el repositorio de rutinas sobre SQLite real (F-RUT-08 + F-HIS-18; lib/core/database/database_service.dart:141-212),
    // en una carpeta temporal con ñá. Las mismas fuentes corren en dotnet (sqlite-net-pcl, SQLite 3.49.1) y en Unity EditMode
    // (gilzoide, SQLite 3.53.4): AC-BASE-08.
    [TestFixture]
    public sealed class SqliteRoutineRepositoryTests
    {
        private RecordingLogger _logger = null!;
        private TempDatabase _db = null!;
        private SqliteRoutineRepository _repository = null!;

        [SetUp]
        public void SetUp()
        {
            _logger = new RecordingLogger();
            _db = new TempDatabase();
            _repository = _db.Repository(_logger);
        }

        [TearDown]
        public void TearDown()
        {
            _repository.Dispose();
            _db.Dispose();
        }

        // AC-RUT-08-01: nombre y descripción al carácter, una fecha, y los pasos en orden con los 14 atributos; 0 ≠ vacío.
        [Test, Category("AC-RUT-08-01")]
        public void InsertKeepsTheFourteenAttributesAndText()
        {
            var full = new IntervalStep(0, 0, StepType.Work)
            {
                DurationSeconds = 0, DistanceMeters = 500, TargetWattsMin = 150, TargetWattsMax = 180, TargetSpm = 0, TargetSplitSeconds = 125,
                GroupId = "g1793106000000_0", GroupRepeatCount = 3, ProgressionSpm = 0, ProgressionWatts = -10, ProgressionSplitSeconds = -2,
            };
            var empty = new IntervalStep(0, 1, StepType.Rest);
            var routine = new Routine("Pirámide ñ ¿¡ 🚣", new DateTimeOffset(2026, 10, 27, 13, 0, 0, TimeSpan.Zero))
            {
                Description = "Sube 2 spm\n2.ª vuelta",
                Steps = new[] { full, empty },
            };
            var saved = Done(_repository.InsertAsync(routine));
            var read = Done(_repository.GetByIdAsync(saved.Id!.Value))!;
            Assert.That(read.Name, Is.EqualTo("Pirámide ñ ¿¡ 🚣"), "al carácter (P-09: el emoji se guarda tal cual)");
            Assert.That(read.Description, Is.EqualTo("Sube 2 spm\n2.ª vuelta"));
            Assert.That(read.CreatedAt, Is.EqualTo(routine.CreatedAt));
            Assert.That(read.Steps.Count, Is.EqualTo(2));
            Assert.That(read.Steps[0], Is.EqualTo(full with { Id = read.Steps[0].Id, RoutineId = saved.Id.Value, Order = 0 }), "los 14 atributos");
            Assert.That(read.Steps[1], Is.EqualTo(empty with { Id = read.Steps[1].Id, RoutineId = saved.Id.Value, Order = 1 }), "todo vacío vuelve vacío");
            Assert.That(read.Steps[0].DurationSeconds, Is.EqualTo(0), "duración 0 ≠ sin duración");
            Assert.That(read.Steps[1].DurationSeconds, Is.Null);
            Assert.That(read.Steps[0].ProgressionSpm, Is.EqualTo(0), "progresión 0 ≠ sin progresión");
            Assert.That(read.Steps[1].ProgressionSpm, Is.Null);
            var raw = Done(_repository.Database.QueryRawForTestsAsync(
                "SELECT duration_seconds, progression_spm, type, group_id FROM interval_steps WHERE routine_id = ? ORDER BY step_order", saved.Id.Value));
            Assert.That(raw[0], Is.EqualTo(new object?[] { 0L, 0L, "work", "g1793106000000_0" }));
            Assert.That(raw[1], Is.EqualTo(new object?[] { null, null, "rest", null }), "NULL en la base");
            Assert.That(saved.Steps[0].Id, Is.EqualTo(read.Steps[0].Id), "la rutina devuelta trae los ids nuevos");
        }

        // AC-RUT-08-01: posiciones 0…n−1 (step_order), sin importar el Order que traigan.
        [Test, Category("AC-RUT-08-01")]
        public void InsertNumbersStepsFromZero()
        {
            var routine = RoutineSeeds.Make("R", RoutineSeeds.Day(20), RoutineSeeds.Step(StepType.Work, dur: 1), RoutineSeeds.Step(StepType.Rest, dur: 2),
                RoutineSeeds.Step(StepType.Cooldown, dur: 3));
            routine = routine with { Steps = new[] { routine.Steps[0] with { Order = 9 }, routine.Steps[1] with { Order = 9 }, routine.Steps[2] with { Order = 4 } } };
            var saved = Done(_repository.InsertAsync(routine));
            for (var i = 0; i < 3; i++)
            {
                Assert.That(saved.Steps[i].Order, Is.EqualTo(i));
                Assert.That(saved.Steps[i].RoutineId, Is.EqualTo(saved.Id));
                Assert.That(saved.Steps[i].Id, Is.Not.Null);
            }

            var orders = Done(_repository.Database.QueryRawForTestsAsync("SELECT step_order, duration_seconds FROM interval_steps ORDER BY id"));
            Assert.That(orders[0], Is.EqualTo(new object?[] { 0L, 1L }));
            Assert.That(orders[2], Is.EqualTo(new object?[] { 2L, 3L }));
        }

        // AC-RUT-08-02: la edición reemplaza TODOS los pasos (ni uno viejo de más), renumera y conserva la fecha.
        [Test, Category("AC-RUT-08-02")]
        public void UpdateReplacesStepsAndKeepsCreatedAt()
        {
            var saved = Done(_repository.InsertAsync(RoutineSeeds.EscaleraSpm()));
            var steps = new List<IntervalStep>(saved.Steps);
            steps.RemoveAt(0);
            steps.Add(RoutineSeeds.Step(StepType.Cooldown, dur: 120));
            var updated = Done(_repository.UpdateAsync(saved with { Name = "Escalera SPM 2", Steps = steps, CreatedAt = DateTimeOffset.MinValue }));
            var read = Done(_repository.GetByIdAsync(saved.Id!.Value))!;
            Assert.That(read.Name, Is.EqualTo("Escalera SPM 2"));
            Assert.That(read.CreatedAt, Is.EqualTo(RoutineSeeds.Day(23)), "la misma fecha de creación (no se escribe)");
            Assert.That(read.Steps.Count, Is.EqualTo(4), "exactamente los pasos nuevos");
            Assert.That(read.Steps[0].Type, Is.EqualTo(StepType.Work));
            Assert.That(read.Steps[3].DurationSeconds, Is.EqualTo(120));
            for (var i = 0; i < read.Steps.Count; i++)
            {
                Assert.That(read.Steps[i].Order, Is.EqualTo(i));
            }

            Assert.That(Done(_repository.Database.QueryRawForTestsAsync("SELECT count(*) FROM interval_steps"))[0][0], Is.EqualTo(4L), "sin duplicados");
            Assert.That(updated.Steps[0].Id, Is.EqualTo(read.Steps[0].Id));
        }

        // AC-RUT-08-02: guardar sin tocar nada deja la rutina igual.
        [Test, Category("AC-RUT-08-02")]
        public void UpdateWithoutChangesIsIdentical()
        {
            var saved = Done(_repository.InsertAsync(RoutineSeeds.PiramideQuinientos()));
            Done(_repository.UpdateAsync(saved));
            var read = Done(_repository.GetByIdAsync(saved.Id!.Value))!;
            Assert.That(read.Name, Is.EqualTo(saved.Name));
            Assert.That(read.Description, Is.EqualTo(saved.Description));
            Assert.That(read.CreatedAt, Is.EqualTo(saved.CreatedAt));
            Assert.That(read.Steps.Count, Is.EqualTo(saved.Steps.Count));
            for (var i = 0; i < read.Steps.Count; i++)
            {
                Assert.That(read.Steps[i] with { Id = null }, Is.EqualTo(saved.Steps[i] with { Id = null }), "paso " + i);
            }
        }

        // AC-RUT-08-02: las demás rutinas no cambian; una que no existe → RoutineNotFoundException y nada cambia.
        [Test, Category("AC-RUT-08-02")]
        public void UpdateLeavesTheOthers()
        {
            var lista = RoutineSeeds.Apply(_repository, RoutineSeeds.Lista);
            var before = Done(_repository.GetAllAsync());
            Done(_repository.UpdateAsync(lista[1] with { Name = "Otro nombre", Steps = new[] { RoutineSeeds.Step(StepType.Work, dur: 1) } }));
            var after = Done(_repository.GetAllAsync());
            foreach (var index in new[] { 0, 1, 3 })
            {
                Assert.That(SameRoutine(after[index], before[index]), Is.True, after[index].Name);
            }

            var missing = Failure(_repository.UpdateAsync(lista[0] with { Id = 999 }));
            Assert.That(missing, Is.InstanceOf<RoutineNotFoundException>());
            Assert.That(Failure(_repository.UpdateAsync(lista[0] with { Id = null })), Is.InstanceOf<RoutineNotFoundException>());
            Assert.That(Done(_repository.GetAllAsync()).Count, Is.EqualTo(4));
            Assert.That(Done(_repository.Database.QueryRawForTestsAsync("SELECT count(*) FROM interval_steps WHERE routine_id = 999"))[0][0], Is.EqualTo(0L));
        }

        // AC-RUT-08-03 (Q-17): la baja borra la rutina con todos sus pasos; las otras quedan.
        [Test, Category("AC-RUT-08-03")]
        public void DeleteRemovesTheRoutineAndItsSteps()
        {
            var lista = RoutineSeeds.Apply(_repository, RoutineSeeds.Lista);
            var piramide = lista[2];
            Assert.That(piramide.Name, Is.EqualTo("Pirámide 500"));
            Done(_repository.DeleteAsync(piramide.Id!.Value));
            var names = new List<string>();
            foreach (var routine in Done(_repository.GetAllAsync()))
            {
                names.Add(routine.Name);
            }

            Assert.That(names, Is.EqualTo(new[] { "Escalera SPM", "Solo calentamiento", "Intervalos 4×" }));
            Assert.That(Done(_repository.Database.QueryRawForTestsAsync("SELECT count(*) FROM interval_steps WHERE routine_id = ?", piramide.Id.Value))[0][0],
                Is.EqualTo(0L), "sin pasos huérfanos");
            Done(_repository.DeleteAsync(piramide.Id.Value));
            Done(_repository.DeleteAsync(12345));
        }

        // AC-RUT-08-03: una rutina creada después nunca muestra pasos de una borrada.
        [Test, Category("AC-RUT-08-03")]
        public void NewRoutineNeverSeesDeletedSteps()
        {
            var old = Done(_repository.InsertAsync(RoutineSeeds.PiramideQuinientos()));
            Done(_repository.DeleteAsync(old.Id!.Value));
            var fresh = Done(_repository.InsertAsync(RoutineSeeds.SoloCalentamiento()));
            Assert.That(Done(_repository.GetByIdAsync(fresh.Id!.Value))!.Steps.Count, Is.EqualTo(1));
            Assert.That(Done(_repository.GetAllAsync()).Count, Is.EqualTo(1));
        }

        // AC-RUT-08-04: lectura por id con los pasos en orden.
        [Test, Category("AC-RUT-08-04")]
        public void GetByIdReturnsStepsInOrder()
        {
            var lista = RoutineSeeds.Apply(_repository, RoutineSeeds.Lista);
            var piramide = Done(_repository.GetByIdAsync(lista[2].Id!.Value))!;
            Assert.That(piramide.Name, Is.EqualTo("Pirámide 500"));
            Assert.That(piramide.Description, Is.EqualTo("Bajar 2 s el split en cada vuelta"));
            Assert.That(piramide.Steps.Count, Is.EqualTo(5));
            Assert.That(piramide.Steps[0].DurationSeconds, Is.EqualTo(600));
            Assert.That(piramide.Steps[1].DistanceMeters, Is.EqualTo(500));
            Assert.That(piramide.Steps[4].Type, Is.EqualTo(StepType.Cooldown));
            for (var i = 0; i < piramide.Steps.Count; i++)
            {
                Assert.That(piramide.Steps[i].Order, Is.EqualTo(i));
            }
        }

        // AC-RUT-08-04: un id que no existe (o el de una borrada) → "no existe", sin error.
        [Test, Category("AC-RUT-08-04")]
        public void GetByIdOfMissingOrDeletedIsNull()
        {
            Assert.That(Done(_repository.GetByIdAsync(1)), Is.Null);
            var saved = Done(_repository.InsertAsync(RoutineSeeds.SoloCalentamiento()));
            Done(_repository.DeleteAsync(saved.Id!.Value));
            Assert.That(Done(_repository.GetByIdAsync(saved.Id.Value)), Is.Null);
        }

        // AC-RUT-08-05: la más nueva arriba; con la misma fecha, la de id mayor (created_at DESC, id DESC).
        [Test, Category("AC-RUT-08-05")]
        public void ListIsNewestFirst()
        {
            RoutineSeeds.Apply(_repository, RoutineSeeds.Lista);
            Done(_repository.InsertAsync(RoutineSeeds.Make("Misma fecha", RoutineSeeds.Day(23))));
            var names = new List<string>();
            foreach (var routine in Done(_repository.GetAllAsync()))
            {
                names.Add(routine.Name);
            }

            Assert.That(names, Is.EqualTo(new[] { "Misma fecha", "Escalera SPM", "Pirámide 500", "Solo calentamiento", "Intervalos 4×" }));
            var texts = Done(_repository.Database.QueryRawForTestsAsync("SELECT created_at FROM routines ORDER BY id"));
            Assert.That(texts[0][0], Is.EqualTo("2026-10-20T13:00:00.000Z"), "UTC con 3 decimales (Q-18, DT-08)");
        }

        // AC-RUT-08-06: dos series contiguas del mismo milisegundo y una serie de un paso vuelven como los mismos bloques.
        [Test, Category("AC-RUT-08-06")]
        public void SeriesSurviveTheRoundTrip()
        {
            var a = "g1793106000000_0";
            var b = "g1793106000000_1";
            var routine = RoutineSeeds.Make("Series", RoutineSeeds.Day(27),
                RoutineSeeds.Step(StepType.Work, dur: 300, spm: 18, g: a, rep: 3, pSpm: 2), RoutineSeeds.Step(StepType.Rest, dur: 120, g: a, rep: 3),
                RoutineSeeds.Step(StepType.Work, dur: 60, wMin: 150, g: b, rep: 2, pW: 10),
                RoutineSeeds.Step(StepType.Cooldown, dur: 300),
                RoutineSeeds.Step(StepType.Work, dur: 30, g: "g1793106000000_2", rep: 5, pSplit: -1, split: 120));
            var saved = Done(_repository.InsertAsync(routine));
            _repository.Dispose();
            _repository = _db.Repository(_logger);
            var read = Done(_repository.GetByIdAsync(saved.Id!.Value))!;
            Assert.That(EditorLayout.Build(read.Steps), Is.EqualTo(EditorLayout.Build(routine.Steps)), "los mismos bloques al reabrir");
            Assert.That(RoutineTemplate.RepeatCountOf(read.Steps, a), Is.EqualTo(3));
            Assert.That(RoutineTemplate.RepeatCountOf(read.Steps, b), Is.EqualTo(2));
            Assert.That(read.Steps[0].ProgressionSpm, Is.EqualTo(2));
            Assert.That(read.Steps[2].ProgressionWatts, Is.EqualTo(10));
            Assert.That(read.Steps[4].ProgressionSplitSeconds, Is.EqualTo(-1));
        }

        // AC-RUT-08-07: una falla a la mitad (trigger después del primer paso) deja la base como estaba, sin pasos parciales.
        [Test, Category("AC-RUT-08-07")]
        public void InsertIsAllOrNothing()
        {
            RoutineSeeds.Apply(_repository, RoutineSeeds.Lista);
            InjectFailureFromTheSecondStep();
            var error = Failure(_repository.InsertAsync(RoutineSeeds.EscaleraSpm()));
            Assert.That(error.GetType().Name, Is.EqualTo("SQLiteException"), "la falla real de SQLite (Constraint)");
            Assert.That(error.Message, Does.Contain("falla inyectada"));
            Assert.That(Done(_repository.GetAllAsync()).Count, Is.EqualTo(4), "no existe la nueva");
            Assert.That(Done(_repository.Database.QueryRawForTestsAsync("SELECT count(*) FROM interval_steps"))[0][0], Is.EqualTo(17L), "ningún paso de más (los 17 de F-LISTA)");
        }

        // AC-RUT-08-07: lo mismo al editar: la rutina queda como estaba.
        [Test, Category("AC-RUT-08-07")]
        public void UpdateIsAllOrNothing()
        {
            var saved = Done(_repository.InsertAsync(RoutineSeeds.EscaleraSpm()));
            InjectFailureFromTheSecondStep();
            var error = Failure(_repository.UpdateAsync(saved with { Name = "Cambiada", Steps = RoutineSeeds.PiramideQuinientos().Steps }));
            Assert.That(error.Message, Does.Contain("falla inyectada"));
            var read = Done(_repository.GetByIdAsync(saved.Id!.Value))!;
            Assert.That(read.Name, Is.EqualTo("Escalera SPM"), "como estaba antes");
            Assert.That(read.Steps.Count, Is.EqualTo(4), "con sus pasos (nunca sin pasos ni a medias)");
        }

        // AC-RUT-08-08, AC-RUT-01-05: un tipo desconocido hace fallar la carga entera; la base no se modifica.
        [Test, Category("AC-RUT-08-08"), Category("AC-RUT-01-05")]
        public void UnknownTypeFailsTheLoadWithoutWriting()
        {
            RoutineSeeds.Apply(_repository, RoutineSeeds.Lista);
            RoutineSeeds.Apply(_repository, RoutineSeeds.RaraSprint);
            AssertLoadFailsWithoutWriting<FormatException>("sprint");
        }

        // AC-RUT-08-08 (DT-10): un entero guardado que no entra en 32 bits hace fallar la carga igual; la base no se toca.
        [Test, Category("AC-RUT-08-08")]
        public void Int32OverflowFailsTheLoadWithoutWriting()
        {
            RoutineSeeds.Apply(_repository, RoutineSeeds.RaraInt32);
            AssertLoadFailsWithoutWriting<OverflowException>(null);
        }

        private void AssertLoadFailsWithoutWriting<TException>(string? messagePart)
            where TException : Exception
        {
            _repository.Dispose();
            var before = Of(_db.Folder);
            _repository = _db.Repository(_logger);
            var error = Failure(_repository.GetAllAsync());
            Assert.That(error, Is.InstanceOf<TException>());
            if (messagePart != null)
            {
                Assert.That(error.Message, Does.Contain(messagePart));
            }

            Assert.That(Failure(_repository.GetAllAsync()), Is.InstanceOf<TException>(), "falla cada vez, no queda a medias");
            _repository.Dispose();
            Assert.That(Describe(Of(_db.Folder)), Is.EqualTo(Describe(before)), "mismos bytes");
            _repository = _db.Repository(_logger);
        }

        private void InjectFailureFromTheSecondStep()
        {
            Done(_repository.Database.ExecuteRawForTestsAsync(
                "CREATE TRIGGER rm_fault BEFORE INSERT ON interval_steps WHEN (SELECT count(*) FROM interval_steps WHERE routine_id = NEW.routine_id) >= 1 "
                + "BEGIN SELECT RAISE(ABORT, 'falla inyectada'); END"));
        }

        private static bool SameRoutine(Routine a, Routine b)
        {
            if (a.Id != b.Id || a.Name != b.Name || a.Description != b.Description || a.CreatedAt != b.CreatedAt || a.Steps.Count != b.Steps.Count)
            {
                return false;
            }

            for (var i = 0; i < a.Steps.Count; i++)
            {
                if (!a.Steps[i].Equals(b.Steps[i]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
