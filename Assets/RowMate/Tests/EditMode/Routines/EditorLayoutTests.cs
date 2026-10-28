using System.Collections.Generic;
using NUnit.Framework;
using RowMate.Core.Models;
using RowMate.Core.Routines;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Routines
{
    // ELT: bloques de la lista del editor (_buildStepsList, lib/features/routines/routine_editor_screen.dart:275-295).
    [TestFixture]
    public sealed class EditorLayoutTests
    {
        private const StepType W = StepType.Warmup;
        private const StepType K = StepType.Work;
        private const StepType R = StepType.Rest;
        private const StepType C = StepType.Cooldown;

        [Test, Category("AC-RUT-14-03")]
        public void EscaleraIsLooseSeriesLoose()
        {
            var items = EditorLayout.Build(RoutineSeeds.EscaleraSpm().Steps);
            Assert.That(Kinds(items), Is.EqualTo(new[] { "LooseStep", "SeriesHeader", "SeriesStep", "SeriesStep", "SeriesFooter", "LooseStep" }));
            Assert.That(items[0].StepIndex, Is.EqualTo(0));
            Assert.That(items[1].StepIndex, Is.EqualTo(1), "el encabezado apunta al primer paso de la serie");
            Assert.That(items[2].StepIndex, Is.EqualTo(1));
            Assert.That(items[3].StepIndex, Is.EqualTo(2));
            Assert.That(items[4].StepIndex, Is.EqualTo(1), "el pie también (:293)");
            Assert.That(items[5].StepIndex, Is.EqualTo(3));
            Assert.That(items[1].SeriesOrdinal, Is.EqualTo(0));
            Assert.That(items[1].RepeatCount, Is.EqualTo(3));
            Assert.That(items[0].SeriesOrdinal, Is.EqualTo(-1));
            Assert.That(items[0].GroupId, Is.Null);
        }

        // AC-RUT-14-03 (:372-373): un encabezado sin repeticiones guardadas muestra ×1.
        [Test, Category("AC-RUT-14-03")]
        public void HeaderRepeatDefaultsToOne()
        {
            var items = EditorLayout.Build(RoutineSeeds.CasosBorde().Steps);
            var headers = new List<EditorItem>();
            foreach (var item in items)
            {
                if (item.Kind == EditorItemKind.SeriesHeader)
                {
                    headers.Add(item);
                }
            }

            Assert.That(headers.Count, Is.EqualTo(2));
            Assert.That(headers[0].RepeatCount, Is.EqualTo(1), "paso 8: serie sin repeticiones → ×1");
            Assert.That(headers[1].RepeatCount, Is.EqualTo(2));
            Assert.That(headers[1].SeriesOrdinal, Is.EqualTo(1));
        }

        // AC-RUT-14-03: ×0 se muestra (paridad); también un negativo.
        [Test, Category("AC-RUT-14-03")]
        public void ZeroRepeatShowsZero()
        {
            var zero = EditorLayout.Build(RoutineSeeds.RaraZero.Routines[0].Steps);
            Assert.That(zero[0].Kind, Is.EqualTo(EditorItemKind.SeriesHeader));
            Assert.That(zero[0].RepeatCount, Is.EqualTo(0));
            var negative = EditorLayout.Build(RoutineSeeds.RaraNegativeRepeat.Routines[0].Steps);
            Assert.That(negative[0].RepeatCount, Is.EqualTo(-3));
        }

        // AC-RUT-14-10: una serie que se queda sin pasos desaparece entera; una serie de un solo paso es un bloque.
        [Test, Category("AC-RUT-14-10")]
        public void EmptiedSeriesDisappears()
        {
            var template = RoutineTemplate.AppendDefaultSeries(new[] { RoutineSeeds.Step(W, dur: 60) }, "g1_0");
            var oneLeft = RoutineTemplate.RemoveAt(template, 1);
            Assert.That(Kinds(EditorLayout.Build(oneLeft)), Is.EqualTo(new[] { "LooseStep", "SeriesHeader", "SeriesStep", "SeriesFooter" }));
            var none = RoutineTemplate.RemoveAt(oneLeft, 1);
            Assert.That(Kinds(EditorLayout.Build(none)), Is.EqualTo(new[] { "LooseStep" }), "sin encabezado ni pie vacíos");
            Assert.That(EditorLayout.Build(new IntervalStep[0]), Is.Empty);
        }

        [Test, Category("AC-RUT-14-10")]
        public void OneStepSeries()
        {
            var template = RoutineSeeds.Make("t", RoutineSeeds.Day(20), RoutineSeeds.Step(K, dur: 60, g: "solo", rep: 5)).Steps;
            var items = EditorLayout.Build(template);
            Assert.That(Kinds(items), Is.EqualTo(new[] { "SeriesHeader", "SeriesStep", "SeriesFooter" }));
            Assert.That(items[0].RepeatCount, Is.EqualTo(5));
        }

        // AC-RUT-14-11: dos series seguidas son dos bloques, cada uno con su encabezado, su ×N y su pie.
        [Test, Category("AC-RUT-14-11")]
        public void ContiguousSeriesAreTwoBlocks()
        {
            var items = EditorLayout.Build(RoutineSeeds.PiramideQuinientos(withSecondSeries: true).Steps);
            Assert.That(Kinds(items), Is.EqualTo(new[]
            {
                "LooseStep", "SeriesHeader", "SeriesStep", "SeriesStep", "SeriesStep", "SeriesFooter", "SeriesHeader", "SeriesStep",
                "SeriesFooter", "LooseStep",
            }));
            Assert.That(items[1].RepeatCount, Is.EqualTo(4));
            Assert.That(items[6].RepeatCount, Is.EqualTo(2));
            Assert.That(items[6].SeriesOrdinal, Is.EqualTo(1));
            Assert.That(items[1].GroupId, Is.Not.EqualTo(items[6].GroupId));
        }

        // DT-14: el mismo id en dos bloques separados (solo a mano) da dos bloques con el ×N del primero, como el legacy.
        [Test, Category("AC-RUT-14-11")]
        public void SameIdTwiceIsTwoBlocksWithTheFirstCount()
        {
            var template = RoutineSeeds.Make("t", RoutineSeeds.Day(20),
                RoutineSeeds.Step(K, dur: 60, g: "x", rep: 2), RoutineSeeds.Step(R, dur: 30), RoutineSeeds.Step(K, dur: 60, g: "x", rep: 7),
                RoutineSeeds.Step(C, dur: 60)).Steps;
            var items = EditorLayout.Build(template);
            Assert.That(Kinds(items), Is.EqualTo(new[] { "SeriesHeader", "SeriesStep", "SeriesFooter", "LooseStep", "SeriesHeader", "SeriesStep",
                "SeriesFooter", "LooseStep" }));
            Assert.That(items[4].RepeatCount, Is.EqualTo(2), "firstWhere(groupId)");
            Assert.That(items[4].SeriesOrdinal, Is.EqualTo(1));
        }

        private static string[] Kinds(IReadOnlyList<EditorItem> items)
        {
            var kinds = new string[items.Count];
            for (var i = 0; i < kinds.Length; i++)
            {
                kinds[i] = items[i].Kind.ToString();
            }

            return kinds;
        }
    }
}
