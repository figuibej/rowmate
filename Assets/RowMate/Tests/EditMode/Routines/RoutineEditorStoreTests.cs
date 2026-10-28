using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using RowMate.App.Routines;
using RowMate.Core.Models;
using RowMate.Core.Ports;
using RowMate.Core.Routines;
using RowMate.Tests.EditMode.Localization;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Routines
{
    // RET: el editor sin la vista (_RoutineEditorScreenState, lib/features/routines/routine_editor_screen.dart:17-200): las
    // operaciones, las validaciones al guardar en orden, el recorte, Q-19 (un toque, una rutina) y el cierre después de Saved.
    [TestFixture]
    public sealed class RoutineEditorStoreTests
    {
        private FakeScheduler _scheduler = null!;
        private RecordingLogger _logger = null!;
        private SequenceClock _clock = null!;

        [SetUp]
        public void SetUp()
        {
            _scheduler = new FakeScheduler(new DateTimeOffset(2026, 10, 27, 13, 0, 0, TimeSpan.Zero));
            _logger = new RecordingLogger();
            _clock = new SequenceClock();
        }

        // AC-RUT-11-05 (caso 16): nombre vacío (aunque tampoco haya pasos) → sin pasos → paso fuera de rango.
        [Test, Category("AC-RUT-11-05")]
        public void ValidationOrder()
        {
            var repository = new InMemoryRoutineRepository();
            var routines = Loaded(repository);
            var editor = new RoutineEditorStore(null, routines, _scheduler, _logger);
            Assert.That(Done(editor.SaveAsync()), Is.EqualTo(SaveOutcome.NameEmpty), "nombre vacío y sin pasos: solo este aviso");
            editor.Name = "   ";
            Assert.That(Done(editor.SaveAsync()), Is.EqualTo(SaveOutcome.NameEmpty), "␣␣␣ recortado");
            editor.Name = (char)0xFEFF + "  " + (char)0x3000;
            Assert.That(Done(editor.SaveAsync()), Is.EqualTo(SaveOutcome.NameEmpty), "espacios de Dart");
            editor.Name = "Prueba";
            Assert.That(Done(editor.SaveAsync()), Is.EqualTo(SaveOutcome.NoSteps));
            editor.AddLooseStep(RoutineSeeds.Step(StepType.Work, dur: 60, wMin: 50000));
            Assert.That(Done(editor.SaveAsync()), Is.EqualTo(SaveOutcome.StepOutOfRange));
            Assert.That(repository.Writes, Is.EqualTo(0), "nada se guarda");
            Assert.That(editor.IsClosed, Is.False);
            Assert.That(RoutineTexts.SaveToast(SaveOutcome.NameEmpty, LocalizationFixture.Es), Is.EqualTo("El nombre no puede estar vacío"));
            Assert.That(RoutineTexts.SaveToast(SaveOutcome.NoSteps, LocalizationFixture.Es), Is.EqualTo("Agregá al menos un paso"));
            Assert.That(RoutineTexts.SaveToast(SaveOutcome.StepOutOfRange, LocalizationFixture.Es), Is.EqualTo("Hay un paso con valores fuera de rango"));
            Assert.That(RoutineTexts.SaveToast(SaveOutcome.StepOutOfRange, LocalizationFixture.En), Is.EqualTo("A step has out-of-range values"));
            Assert.That(RoutineTexts.SaveToast(SaveOutcome.Failed, LocalizationFixture.Es), Is.EqualTo("No se pudo guardar la rutina"));
            Assert.That(RoutineTexts.SaveToast(SaveOutcome.Saved, LocalizationFixture.Es), Is.Null);
            Assert.That(RoutineTexts.SaveToast(SaveOutcome.Ignored, LocalizationFixture.Es), Is.Null);
        }

        // AC-RUT-11-05: F-RARA (vatios 50 000, ×2 000 000 000) no se deja guardar.
        [Test, Category("AC-RUT-11-05")]
        public void OutOfRangeStepBlocksTheSave()
        {
            foreach (var seed in new[] { RoutineSeeds.RaraWatts, RoutineSeeds.RaraHuge })
            {
                var repository = new InMemoryRoutineRepository(seed.Routines);
                var routines = Loaded(repository);
                var editor = new RoutineEditorStore(routines.State.Value.Routines[0], routines, _scheduler, _logger);
                Assert.That(Done(editor.SaveAsync()), Is.EqualTo(SaveOutcome.StepOutOfRange), seed.Name);
                Assert.That(repository.Writes, Is.EqualTo(0), seed.Name);
            }
        }

        // AC-RUT-11-05 + QA-G3-13 c: las progresiones de un paso suelto no frenan el guardado.
        [Test, Category("AC-RUT-11-05")]
        public void LooseStepProgressionsDoNotBlockTheSave()
        {
            var repository = new InMemoryRoutineRepository(RoutineSeeds.RaraLooseProgressions.Routines);
            var routines = Loaded(repository);
            var editor = new RoutineEditorStore(routines.State.Value.Routines[0], routines, _scheduler, _logger);
            Assert.That(Done(editor.SaveAsync()), Is.EqualTo(SaveOutcome.Saved));
            Assert.That(repository.Snapshot[0].Steps[0].ProgressionSpm, Is.EqualTo(5000), "se guarda tal cual");
        }

        // AC-RUT-11-05 + DT-13: una serie ×0 o con repeticiones negativas se guarda tal cual.
        [Test, Category("AC-RUT-11-05")]
        public void ZeroAndNegativeRepeatsDoNotBlockTheSave()
        {
            foreach (var seed in new[] { RoutineSeeds.RaraZero, RoutineSeeds.RaraNegativeRepeat })
            {
                var repository = new InMemoryRoutineRepository(seed.Routines);
                var routines = Loaded(repository);
                var editor = new RoutineEditorStore(routines.State.Value.Routines[0], routines, _scheduler, _logger);
                Assert.That(Done(editor.SaveAsync()), Is.EqualTo(SaveOutcome.Saved), seed.Name);
            }
        }

        // AC-RUT-11-06: recortado en sus extremos, pasos 0…n−1; al crear la fecha del reloj, al editar se conservan id y fecha.
        [Test, Category("AC-RUT-11-06")]
        public void SaveTrimsRenumbersAndKeepsId()
        {
            var repository = new InMemoryRoutineRepository(RoutineSeeds.Lista.Routines);
            var routines = Loaded(repository);
            var editor = new RoutineEditorStore(null, routines, _scheduler, _logger);
            editor.Name = "  Mi rutina  ";
            editor.Description = "   ";
            editor.AddSeries();
            editor.AddLooseStep(RoutineSeeds.Step(StepType.Cooldown, dur: 300));
            Assert.That(Done(editor.SaveAsync()), Is.EqualTo(SaveOutcome.Saved));
            Assert.That(editor.IsClosed, Is.True);
            var created = routines.State.Value.Routines[0];
            Assert.That(created.Name, Is.EqualTo("Mi rutina"));
            Assert.That(created.Description, Is.EqualTo(""), "una descripción de espacios queda vacía");
            Assert.That(created.CreatedAt, Is.EqualTo(SequenceClock.DefaultStart));
            for (var i = 0; i < created.Steps.Count; i++)
            {
                Assert.That(created.Steps[i].Order, Is.EqualTo(i));
                Assert.That(created.Steps[i].RoutineId, Is.EqualTo(created.Id));
            }

            var escalera = routines.State.Value.Routines[1];
            var edit = new RoutineEditorStore(escalera, routines, _scheduler, _logger);
            Assert.That(edit.IsNew, Is.False);
            Assert.That(edit.Name, Is.EqualTo("Escalera SPM"), "precargado");
            edit.Name = " Escalera SPM 2 ";
            edit.DeleteStep(0);
            edit.AddLooseStep(RoutineSeeds.Step(StepType.Cooldown, dur: 120));
            Assert.That(Done(edit.SaveAsync()), Is.EqualTo(SaveOutcome.Saved));
            var updated = routines.State.Value.Routines[1];
            Assert.That(updated.Id, Is.EqualTo(escalera.Id), "mismo id");
            Assert.That(updated.CreatedAt, Is.EqualTo(escalera.CreatedAt), "misma fecha");
            Assert.That(updated.Name, Is.EqualTo("Escalera SPM 2"));
            Assert.That(updated.Steps.Count, Is.EqualTo(4));
            Assert.That(updated.Steps[0].Type, Is.EqualTo(StepType.Work), "sin el calentamiento");
            Assert.That(updated.Steps[3].DurationSeconds, Is.EqualTo(120), "el nuevo al final");
        }

        // AC-RUT-11-07: sin unicidad ni largo máximo.
        [Test, Category("AC-RUT-11-07")]
        public void NoUniquenessNoMaxLength()
        {
            var repository = new InMemoryRoutineRepository(RoutineSeeds.Lista.Routines);
            var routines = Loaded(repository);
            foreach (var name in new[] { "Escalera SPM", new string('x', 500) })
            {
                var editor = new RoutineEditorStore(null, routines, _scheduler, _logger);
                editor.Name = name;
                editor.AddSeries();
                Assert.That(Done(editor.SaveAsync()), Is.EqualTo(SaveOutcome.Saved), name.Length + " caracteres");
            }

            Assert.That(repository.Snapshot.Count, Is.EqualTo(6));
            Assert.That(routines.State.Value.Routines[0].Name.Length, Is.EqualTo(500));
            Assert.That(routines.State.Value.Routines[1].Name, Is.EqualTo("Escalera SPM"));
        }

        // AC-RUT-09-03 (Q-19): los toques que llegan mientras se guarda no hacen nada.
        [Test, Category("AC-RUT-09-03")]
        public void SaveWhileSavingIsIgnored()
        {
            var repository = new ScriptedRoutineRepository(_scheduler) { SaveDelay = TimeSpan.FromMilliseconds(500) };
            var routines = Loaded(repository);
            var editor = Valid(routines);
            var first = editor.SaveAsync();
            Assert.That(first.IsCompleted, Is.False);
            Assert.That(editor.State.Value.IsSaving, Is.True);
            Assert.That(Done(editor.SaveAsync()), Is.EqualTo(SaveOutcome.Ignored), "segundo toque");
            Assert.That(Done(editor.SaveAsync()), Is.EqualTo(SaveOutcome.Ignored), "Enter sobre el botón");
            _scheduler.Advance(TimeSpan.FromMilliseconds(500));
            Assert.That(Done(first), Is.EqualTo(SaveOutcome.Saved));
            Assert.That(repository.InsertCalls, Is.EqualTo(1), "una sola rutina");
            Assert.That(repository.Snapshot.Count, Is.EqualTo(1));
        }

        // AC-RUT-09-03 + QA-G3-01: después de Saved el editor queda cerrado (está saliendo): nada más guarda ni cambia.
        [Test, Category("AC-RUT-09-03")]
        public void SaveAfterSavedIsIgnored()
        {
            var repository = new InMemoryRoutineRepository();
            var routines = Loaded(repository);
            var editor = Valid(routines);
            Assert.That(Done(editor.SaveAsync()), Is.EqualTo(SaveOutcome.Saved));
            var states = new StateRecorder<RoutineEditorState>(editor.State);
            Assert.That(Done(editor.SaveAsync()), Is.EqualTo(SaveOutcome.Ignored), "un toque 50 ms después, con el editor saliendo");
            editor.AddSeries();
            editor.AddLooseStep(RoutineSeeds.Step(StepType.Work, dur: 1));
            editor.DeleteStep(0);
            editor.Name = "Otro";
            Assert.That(editor.ChangeRepeatCount(editor.Template[0].GroupId!, "7").Kind, Is.EqualTo(RepeatChangeKind.Unchanged));
            Assert.That(states.Count, Is.EqualTo(0), "nada cambia");
            Assert.That(editor.Name, Is.EqualTo("Prueba"));
            Assert.That(repository.Writes, Is.EqualTo(1));
            Assert.That(editor.IsClosed, Is.True);
        }

        // AC-RUT-09-05: el guardado que falla deja el editor abierto con todo; con la base bien, se guarda UNA rutina.
        [Test, Category("AC-RUT-09-05")]
        public void SaveFailureKeepsEverything()
        {
            var repository = new ScriptedRoutineRepository(_scheduler) { FailSave = true };
            var routines = Loaded(repository);
            var editor = Valid(routines);
            editor.Description = "  algo  ";
            Assert.That(Done(editor.SaveAsync()), Is.EqualTo(SaveOutcome.Failed));
            Assert.That(editor.IsClosed, Is.False);
            Assert.That(editor.State.Value.IsSaving, Is.False);
            Assert.That(editor.Name, Is.EqualTo("Prueba"), "lo escrito sigue (sin recortar)");
            Assert.That(editor.Description, Is.EqualTo("  algo  "));
            Assert.That(editor.State.Value.StepCount, Is.EqualTo(2));
            repository.FailSave = false;
            Assert.That(Done(editor.SaveAsync()), Is.EqualTo(SaveOutcome.Saved));
            Assert.That(repository.Snapshot.Count, Is.EqualTo(1), "una sola rutina");
            Assert.That(repository.Snapshot[0].Description, Is.EqualTo("algo"));
        }

        // QA-G3-09: el editor cerrado con "Atrás" durante un guardado lento no cambia nada; el guardado termina igual y la
        // rutina aparece en la lista (paridad con `if (mounted) pop()`, :63-72).
        [Test, Category("AC-RUT-09-05")]
        public void LateSaveAfterDisposeTouchesNothing()
        {
            var repository = new ScriptedRoutineRepository(_scheduler) { SaveDelay = TimeSpan.FromSeconds(2) };
            var routines = Loaded(repository);
            var editor = Valid(routines);
            var save = editor.SaveAsync();
            var states = new StateRecorder<RoutineEditorState>(editor.State);
            editor.Dispose();
            _scheduler.Advance(TimeSpan.FromSeconds(2));
            Assert.That(Done(save), Is.EqualTo(SaveOutcome.Saved));
            Assert.That(states.Count, Is.EqualTo(0));
            Assert.That(routines.State.Value.Routines.Count, Is.EqualTo(1), "la lista ya la muestra");
            Assert.That(editor.IsClosed, Is.True);
        }

        // AC-RUT-11-03, AC-RUT-12-01, -04, AC-RUT-14-01, -07, -09: las operaciones reemiten el estado en el acto; cada fila
        // conserva su clave mientras dura el editor (editar no la cambia; lo nuevo trae otra).
        [Test, Category("AC-RUT-11-03"), Category("AC-RUT-12-01"), Category("AC-RUT-12-04"), Category("AC-RUT-14-09")]
        public void OperationsUpdateTheStateAtOnce()
        {
            var routines = Loaded(new InMemoryRoutineRepository());
            var editor = new RoutineEditorStore(null, routines, _scheduler, _logger);
            Assert.That(editor.State.Value.StepCount, Is.EqualTo(0));
            editor.AddLooseStep(RoutineSeeds.Step(StepType.Warmup, dur: 300));
            editor.AddSeries();
            Assert.That(editor.State.Value.StepCount, Is.EqualTo(3), "Pasos (3)");
            var keys = Keys(editor);
            var groupId = editor.Template[1].GroupId!;
            editor.AddStepToSeries(groupId, RoutineSeeds.Step(StepType.Work, dist: 250));
            Assert.That(editor.State.Value.StepCount, Is.EqualTo(4));
            Assert.That(editor.Template[3].DistanceMeters, Is.EqualTo(250), "después del último de la serie");
            Assert.That(editor.Template[3].GroupId, Is.EqualTo(groupId));
            editor.ReplaceStep(1, RoutineSeeds.Step(StepType.Work, dur: 300, spm: 18, pSpm: 2));
            var afterEdit = Keys(editor);
            Assert.That(afterEdit[1], Is.EqualTo(keys[1]), "editar conserva la clave");
            Assert.That(afterEdit[3], Is.Not.EqualTo(keys[0]).And.Not.EqualTo(keys[1]).And.Not.EqualTo(keys[2]), "lo nuevo trae otra");
            editor.AddLooseStep(RoutineSeeds.Step(StepType.Cooldown, dur: 300));
            Assert.That(editor.Template[4].Type, Is.EqualTo(StepType.Cooldown), "después de la serie");
            editor.DeleteStep(0);
            Assert.That(editor.State.Value.StepCount, Is.EqualTo(4), "la cruz baja Pasos (N) en 1");
            Assert.That(Keys(editor)[0], Is.EqualTo(keys[1]));
            editor.DeleteSeries(groupId);
            Assert.That(editor.State.Value.StepCount, Is.EqualTo(1), "el tacho borra la serie entera");
            Assert.That(editor.State.Value.Layout.Count, Is.EqualTo(1));
            Assert.That(RoutineTexts.StepsCount(editor.State.Value.StepCount, LocalizationFixture.Es), Is.EqualTo("Pasos (1)"));
            Assert.That(RoutineTexts.StepsCount(4, LocalizationFixture.En), Is.EqualTo("Steps (4)"));
        }

        // AC-RUT-08-06 + AC-RUT-14-01: dos toques en Serie en el mismo milisegundo dan dos series distintas (el contador).
        [Test, Category("AC-RUT-08-06"), Category("AC-RUT-14-01")]
        public void TwoSeriesInTheSameMillisecond()
        {
            var routines = Loaded(new InMemoryRoutineRepository());
            var editor = new RoutineEditorStore(null, routines, _scheduler, _logger);
            editor.AddSeries();
            editor.AddSeries();
            Assert.That(editor.Template[0].GroupId, Is.EqualTo("g1793106000000_0"));
            Assert.That(editor.Template[2].GroupId, Is.EqualTo("g1793106000000_1"));
            Assert.That(editor.State.Value.Layout.Count, Is.EqualTo(8), "dos bloques");
            Assert.That(_clock.Reads, Is.EqualTo(0), "los ids no leen el reloj de creación (QA-G3-19)");
        }

        // AC-RUT-12-01, -03, AC-RUT-14-07, -08: qué diálogo abre cada acción.
        [Test, Category("AC-RUT-12-03"), Category("AC-RUT-14-08")]
        public void FormsForTheDialogs()
        {
            var routines = Loaded(new InMemoryRoutineRepository());
            var editor = new RoutineEditorStore(RoutineSeeds.EscaleraSpm(), routines, _scheduler, _logger);
            var add = editor.NewStepForm();
            Assert.That(add.IsNew && !add.ShowsProgressions, Is.True, "+ Paso");
            var groupId = editor.Template[1].GroupId!;
            var addToSeries = editor.NewSeriesStepForm(groupId);
            Assert.That(addToSeries.IsNew && addToSeries.ShowsProgressions, Is.True, "+ Agregar paso a serie");
            Assert.That(addToSeries.SeriesRepeatCount, Is.EqualTo(3));
            var editLoose = editor.EditStepForm(0);
            Assert.That(editLoose.IsNew || editLoose.ShowsProgressions, Is.False, "lápiz de un paso suelto");
            var editSeries = editor.EditStepForm(1);
            Assert.That(editSeries.ShowsProgressions, Is.True, "lápiz de un paso de serie");
            Assert.That(editSeries.SeriesRepeatCount, Is.EqualTo(3));
            Assert.That(editSeries.Spm, Is.EqualTo("18"));
            editSeries.Minutes = "4";
            editor.ReplaceStep(1, editSeries.Confirm().Step!);
            Assert.That(editor.Template[1].DurationSeconds, Is.EqualTo(240));
            Assert.That(editor.Template[1].GroupId, Is.EqualTo(groupId), "en la misma serie");
            Assert.That(editor.Template[1].GroupRepeatCount, Is.EqualTo(3));
        }

        private RoutinesStore Loaded(IRoutineRepository repository)
        {
            var store = new RoutinesStore(repository, _clock, _logger);
            Assert.That(store.LoadAsync().IsCompleted, Is.True);
            return store;
        }

        // Nombre "Prueba" + la serie por defecto (2 pasos).
        private RoutineEditorStore Valid(RoutinesStore routines)
        {
            var editor = new RoutineEditorStore(null, routines, _scheduler, _logger);
            editor.Name = "Prueba";
            editor.AddSeries();
            return editor;
        }

        private static int[] Keys(RoutineEditorStore editor)
        {
            var steps = editor.State.Value.Steps;
            var keys = new int[steps.Count];
            for (var i = 0; i < keys.Length; i++)
            {
                keys[i] = steps[i].Key;
            }

            return keys;
        }

        private static T Done<T>(Task<T> task)
        {
            Assert.That(task.IsCompleted, Is.True, "la tarea terminó");
            return task.GetAwaiter().GetResult();
        }
    }
}
