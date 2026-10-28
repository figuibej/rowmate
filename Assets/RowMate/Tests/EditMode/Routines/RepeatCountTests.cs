using System;
using NUnit.Framework;
using RowMate.App.Routines;
using RowMate.Core.Models;
using RowMate.Core.Routines;
using RowMate.Tests.EditMode.Localization;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Routines
{
    // RCT: OK del diálogo de repeticiones (AC-RUT-14-05, -06; routine_editor_screen.dart:151-188 + Q-92).
    [TestFixture]
    public sealed class RepeatCountTests
    {
        private FakeScheduler _scheduler = null!;
        private RecordingLogger _logger = null!;
        private RoutinesStore _routines = null!;

        [SetUp]
        public void SetUp()
        {
            _scheduler = new FakeScheduler();
            _logger = new RecordingLogger();
            _routines = new RoutinesStore(new InMemoryRoutineRepository(), new SequenceClock(), _logger);
        }

        // AC-RUT-14-05 (caso 18), sobre la serie por defecto (Trabajo 120 s + Descanso 60 s ×3, sin objetivos).
        [TestCase("5", RepeatChangeKind.Applied, 5, Category = "AC-RUT-14-05")]
        [TestCase("+5", RepeatChangeKind.Applied, 5, Category = "AC-RUT-14-05")]
        [TestCase("05", RepeatChangeKind.Applied, 5, Category = "AC-RUT-14-05")]
        [TestCase(" 5 ", RepeatChangeKind.Applied, 5, Category = "AC-RUT-14-05")]
        [TestCase("0x5", RepeatChangeKind.Applied, 5, Category = "AC-RUT-14-05")]
        [TestCase("abc", RepeatChangeKind.Applied, 1, Category = "AC-RUT-14-05")]
        [TestCase("", RepeatChangeKind.Applied, 1, Category = "AC-RUT-14-05")]
        [TestCase("5.0", RepeatChangeKind.Applied, 1, Category = "AC-RUT-14-05")]
        [TestCase("1e3", RepeatChangeKind.Applied, 1, Category = "AC-RUT-14-05")]
        [TestCase("0", RepeatChangeKind.Unchanged, 3, Category = "AC-RUT-14-05")]
        [TestCase("-2", RepeatChangeKind.Unchanged, 3, Category = "AC-RUT-14-05")]
        [TestCase("999", RepeatChangeKind.Applied, 999, Category = "AC-RUT-14-05")]
        [TestCase("1000", RepeatChangeKind.TooMany, 999, Category = "AC-RUT-14-05")]
        [TestCase("3000000000", RepeatChangeKind.TooMany, 999, Category = "AC-RUT-14-05")]
        [TestCase("99999999999999999999", RepeatChangeKind.TooMany, 999, Category = "AC-RUT-14-05")]
        public void Table(string text, RepeatChangeKind kind, int count)
        {
            var editor = NewEditorWithDefaultSeries(out var groupId);
            var change = editor.ChangeRepeatCount(groupId, text);
            Assert.That(change, Is.EqualTo(new RepeatChange(kind, count)));
            var expected = kind == RepeatChangeKind.Applied ? count : 3;
            Assert.That(editor.RepeatCountOf(groupId), Is.EqualTo(expected));
            foreach (var step in editor.Template)
            {
                Assert.That(step.GroupRepeatCount, Is.EqualTo(expected), "todos los pasos de la serie");
            }

            Assert.That(change.ClosesTheDialog, Is.EqualTo(kind == RepeatChangeKind.Applied || kind == RepeatChangeKind.Unchanged));
        }

        // El menos tipográfico (U+2212) no es un número: ×1.
        [Test, Category("AC-RUT-14-05")]
        public void TypographicMinusIsOne()
        {
            var editor = NewEditorWithDefaultSeries(out var groupId);
            Assert.That(editor.ChangeRepeatCount(groupId, (char)0x2212 + "2"), Is.EqualTo(new RepeatChange(RepeatChangeKind.Applied, 1)));
        }

        // QA-G3-13 b: un negativo de cualquier largo no cambia nada (en el legacy el de 20 cifras no entra y da ×1: DV-17).
        [Test, Category("AC-RUT-14-05")]
        public void HugeNegativeIsUnchanged()
        {
            var editor = NewEditorWithDefaultSeries(out var groupId);
            Assert.That(editor.ChangeRepeatCount(groupId, "-99999999999999999999"), Is.EqualTo(new RepeatChange(RepeatChangeKind.Unchanged, 3)));
            Assert.That(editor.RepeatCountOf(groupId), Is.EqualTo(3));
        }

        // AC-RUT-14-06: de 1 a 999 y, al subir N, ningún objetivo progresado fuera de su tope. Escalera SPM (18, +2).
        [Test, Category("AC-RUT-14-06")]
        public void MaxAndProgressedTargets()
        {
            var editor = new RoutineEditorStore(RoutineSeeds.EscaleraSpm(), _routines, _scheduler, _logger);
            var groupId = editor.Template[1].GroupId!;
            Assert.That(editor.ChangeRepeatCount(groupId, "492"), Is.EqualTo(new RepeatChange(RepeatChangeKind.TargetOutOfRange, 492)));
            Assert.That(editor.RepeatCountOf(groupId), Is.EqualTo(3), "la serie no cambia");
            Assert.That(editor.ChangeRepeatCount(groupId, "999"), Is.EqualTo(new RepeatChange(RepeatChangeKind.TargetOutOfRange, 999)), "18 + 2 × 998 = 2014");
            Assert.That(editor.ChangeRepeatCount(groupId, "491"), Is.EqualTo(new RepeatChange(RepeatChangeKind.Applied, 491)), "la última da 998");
            Assert.That(editor.ChangeRepeatCount(groupId, "2"), Is.EqualTo(new RepeatChange(RepeatChangeKind.Applied, 2)), "bajar siempre se acepta");
            Assert.That(RoutineTexts.RepeatError(new RepeatChange(RepeatChangeKind.TargetOutOfRange, 492), LocalizationFixture.Es),
                Is.EqualTo("Con ×492, un objetivo queda fuera de rango"));
            Assert.That(RoutineTexts.RepeatError(new RepeatChange(RepeatChangeKind.TargetOutOfRange, 492), LocalizationFixture.En),
                Is.EqualTo("With ×492, a target goes out of range"));
            Assert.That(RoutineTexts.RepeatError(new RepeatChange(RepeatChangeKind.TooMany, 999), LocalizationFixture.Es), Is.EqualTo("Máximo 999"));
            Assert.That(RoutineTexts.RepeatError(new RepeatChange(RepeatChangeKind.TooMany, 999), LocalizationFixture.En), Is.EqualTo("Maximum 999"));
            Assert.That(RoutineTexts.RepeatError(new RepeatChange(RepeatChangeKind.Applied, 4), LocalizationFixture.Es), Is.Null);
        }

        // Bajar N siempre se acepta, también desde una serie imposible (F-RARA ×2 000 000 000 → ×5 la arregla).
        [Test, Category("AC-RUT-14-06")]
        public void LoweringAnImpossibleSeriesIsAccepted()
        {
            var editor = new RoutineEditorStore(RoutineSeeds.RaraHuge.Routines[0], _routines, _scheduler, _logger);
            var groupId = editor.Template[0].GroupId!;
            Assert.That(editor.ChangeRepeatCount(groupId, "5"), Is.EqualTo(new RepeatChange(RepeatChangeKind.Applied, 5)));
            Assert.That(editor.RepeatCountOf(groupId), Is.EqualTo(5));
        }

        // AC-RUT-14-11: el cambio toca solo su serie.
        [Test, Category("AC-RUT-14-11")]
        public void OnlyItsSeries()
        {
            var editor = new RoutineEditorStore(RoutineSeeds.PiramideQuinientos(withSecondSeries: true), _routines, _scheduler, _logger);
            var first = editor.Template[1].GroupId!;
            var second = editor.Template[4].GroupId!;
            Assert.That(editor.ChangeRepeatCount(second, "7").Kind, Is.EqualTo(RepeatChangeKind.Applied));
            Assert.That(editor.RepeatCountOf(first), Is.EqualTo(4));
            Assert.That(editor.RepeatCountOf(second), Is.EqualTo(7));
            Assert.That(editor.State.Value.Layout[1].RepeatCount, Is.EqualTo(4));
            Assert.That(editor.State.Value.Layout[6].RepeatCount, Is.EqualTo(7));
        }

        private RoutineEditorStore NewEditorWithDefaultSeries(out string groupId)
        {
            var editor = new RoutineEditorStore(null, _routines, _scheduler, _logger);
            editor.AddSeries();
            groupId = editor.Template[0].GroupId!;
            Assert.That(editor.RepeatCountOf(groupId), Is.EqualTo(3));
            return editor;
        }
    }
}
