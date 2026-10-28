using System;
using System.Collections.Generic;
using NUnit.Framework;
using RowMate.Core.Models;
using RowMate.Core.Routines;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Routines
{
    // RTT: operaciones sobre la plantilla del editor (lib/features/routines/routine_editor_screen.dart:75-200).
    [TestFixture]
    public sealed class RoutineTemplateTests
    {
        private const StepType W = StepType.Warmup;
        private const StepType K = StepType.Work;
        private const StepType R = StepType.Rest;
        private const StepType C = StepType.Cooldown;

        // AC-RUT-11-03: Pasos (N) = pasos de la plantilla (la serie de 2 ×3 suma 2).
        [Test, Category("AC-RUT-11-03")]
        public void CountIsTheTemplate()
        {
            Assert.That(RoutineSeeds.EscaleraSpm().Steps.Count, Is.EqualTo(4), "Escalera SPM → Pasos (4)");
            Assert.That(RoutineSeeds.CasosBorde().Steps.Count, Is.EqualTo(11), "F-BORDE → Pasos (11)");
            Assert.That(RoutineExpansion.Flatten(RoutineSeeds.EscaleraSpm().Steps).Count, Is.EqualTo(8), "la expandida no cuenta");
        }

        // AC-RUT-12-01 (:80-86): al final, suelto, aunque lo último sea una serie.
        [Test, Category("AC-RUT-12-01")]
        public void AppendLooseGoesLastEvenAfterASeries()
        {
            var template = RoutineTemplate.AppendDefaultSeries(new IntervalStep[0], "g1_0");
            var result = RoutineTemplate.AppendLoose(template, RoutineSeeds.Step(C, dur: 300));
            Assert.That(result.Count, Is.EqualTo(3));
            Assert.That(result[2].Type, Is.EqualTo(C));
            Assert.That(result[2].GroupId, Is.Null, "suelto");
            Assert.That(result[2].GroupRepeatCount, Is.Null);
            Assert.That(result[2].Order, Is.EqualTo(2), "order = largo anterior (:85)");
            Assert.That(template.Count, Is.EqualTo(2), "la plantilla de antes no cambia");
            var layout = EditorLayout.Build(result);
            Assert.That(layout[layout.Count - 1].Kind, Is.EqualTo(EditorItemKind.LooseStep), "después del pie de la serie");
            Assert.That(layout[layout.Count - 2].Kind, Is.EqualTo(EditorItemKind.SeriesFooter));
        }

        // AC-RUT-14-01 (:110-133): Trabajo 120 s + Descanso 60 s, ×3, sin objetivos, al final; dos toques, dos series.
        [Test, Category("AC-RUT-14-01")]
        public void DefaultSeries()
        {
            var start = new[] { RoutineSeeds.Step(W, dur: 300) };
            var once = RoutineTemplate.AppendDefaultSeries(start, "g1_0");
            var twice = RoutineTemplate.AppendDefaultSeries(once, "g1_1");
            Assert.That(twice.Count, Is.EqualTo(5));
            Assert.That(Describe(twice[1]), Is.EqualTo("Work 120 s g1_0 ×3"));
            Assert.That(Describe(twice[2]), Is.EqualTo("Rest 60 s g1_0 ×3"));
            Assert.That(Describe(twice[3]), Is.EqualTo("Work 120 s g1_1 ×3"));
            Assert.That(Describe(twice[4]), Is.EqualTo("Rest 60 s g1_1 ×3"));
            Assert.That(twice[1].Order, Is.EqualTo(1));
            Assert.That(twice[2].Order, Is.EqualTo(2));
            foreach (var step in new[] { twice[1], twice[2] })
            {
                Assert.That(step.TargetSpm, Is.Null);
                Assert.That(step.TargetWattsMin, Is.Null);
                Assert.That(step.TargetWattsMax, Is.Null);
                Assert.That(step.TargetSplitSeconds, Is.Null);
                Assert.That(step.ProgressionSpm, Is.Null);
                Assert.That(step.DistanceMeters, Is.Null);
            }

            Assert.That(RoutineTemplate.DefaultSeriesWorkSeconds, Is.EqualTo(120));
            Assert.That(RoutineTemplate.DefaultSeriesRestSeconds, Is.EqualTo(60));
            Assert.That(RoutineTemplate.DefaultSeriesRepeatCount, Is.EqualTo(3));
            Assert.That(EditorLayout.Build(twice).Count, Is.EqualTo(1 + 4 + 4), "dos bloques distintos");
        }

        // AC-RUT-14-07 (:135-149): después del ÚLTIMO paso de esa serie, con sus repeticiones; con dos series contiguas, en la
        // que se tocó.
        [Test, Category("AC-RUT-14-07")]
        public void InsertIntoSeriesAfterItsLast()
        {
            var template = Template(
                RoutineSeeds.Step(W, dur: 300),
                RoutineSeeds.Step(K, dur: 60, g: "a", rep: 4),
                RoutineSeeds.Step(R, dur: 30, g: "a", rep: 4),
                RoutineSeeds.Step(K, dur: 60, g: "b", rep: 2),
                RoutineSeeds.Step(C, dur: 300));
            var fromDialog = RoutineSeeds.Step(K, dist: 250, spm: 30);
            var intoA = RoutineTemplate.InsertIntoSeries(template, "a", fromDialog);
            Assert.That(intoA.Count, Is.EqualTo(6));
            Assert.That(intoA[3].DistanceMeters, Is.EqualTo(250), "después del último de a, antes de b");
            Assert.That(intoA[3].GroupId, Is.EqualTo("a"));
            Assert.That(intoA[3].GroupRepeatCount, Is.EqualTo(4), "con las repeticiones de la serie");
            Assert.That(intoA[4].GroupId, Is.EqualTo("b"));
            var intoB = RoutineTemplate.InsertIntoSeries(template, "b", fromDialog);
            Assert.That(intoB[4].GroupId, Is.EqualTo("b"));
            Assert.That(intoB[4].GroupRepeatCount, Is.EqualTo(2));
            Assert.That(intoB[5].Type, Is.EqualTo(C));
            var withoutCount = Template(RoutineSeeds.Step(K, dur: 60, g: "c"));
            Assert.That(RoutineTemplate.InsertIntoSeries(withoutCount, "c", fromDialog)[1].GroupRepeatCount, Is.Null, "sin repeticiones: ×1, igual que la serie");
            Assert.Throws<ArgumentException>(() => RoutineTemplate.InsertIntoSeries(template, "nope", fromDialog));
        }

        // AC-RUT-12-03 (:98-106): en su lugar, conservando serie y ×N; un paso suelto sigue suelto.
        [Test, Category("AC-RUT-12-03")]
        public void ReplaceKeepsSeriesAndCount()
        {
            var template = RoutineSeeds.EscaleraSpm().Steps;
            var edited = RoutineSeeds.Step(K, dur: 240, spm: 20, pSpm: 1);
            var result = RoutineTemplate.Replace(template, 1, edited);
            Assert.That(result[1].DurationSeconds, Is.EqualTo(240));
            Assert.That(result[1].GroupId, Is.EqualTo(template[1].GroupId), "misma serie");
            Assert.That(result[1].GroupRepeatCount, Is.EqualTo(3), "mismo ×N");
            Assert.That(result[1].Order, Is.EqualTo(1));
            Assert.That(result.Count, Is.EqualTo(4));
            var loose = RoutineTemplate.Replace(template, 0, RoutineSeeds.Step(W, dur: 120));
            Assert.That(loose[0].GroupId, Is.Null);
            Assert.That(loose[0].GroupRepeatCount, Is.Null);
            Assert.That(template[1].DurationSeconds, Is.EqualTo(300), "la plantilla de antes no cambia");
        }

        // AC-RUT-14-08: editar un paso de serie lo deja en la misma serie, con las mismas repeticiones y en el mismo lugar.
        [Test, Category("AC-RUT-14-08")]
        public void ReplaceInSeries()
        {
            var template = RoutineSeeds.PiramideQuinientos().Steps;
            var result = RoutineTemplate.Replace(template, 2, RoutineSeeds.Step(R, dur: 45, pSpm: 1));
            Assert.That(result[2].Type, Is.EqualTo(R));
            Assert.That(result[2].GroupId, Is.EqualTo(template[2].GroupId));
            Assert.That(result[2].GroupRepeatCount, Is.EqualTo(4));
            Assert.That(EditorLayout.Build(result).Count, Is.EqualTo(EditorLayout.Build(template).Count), "mismos bloques");
        }

        // AC-RUT-12-04 (:190-194): al instante.
        [Test, Category("AC-RUT-12-04")]
        public void RemoveAt()
        {
            var template = RoutineSeeds.EscaleraSpm().Steps;
            var result = RoutineTemplate.RemoveAt(template, 0);
            Assert.That(result.Count, Is.EqualTo(3));
            Assert.That(result[0].Type, Is.EqualTo(K));
            Assert.Throws<ArgumentOutOfRangeException>(() => RoutineTemplate.RemoveAt(template, 4));
        }

        // AC-RUT-14-09 (:196-200): la serie entera (removeWhere).
        [Test, Category("AC-RUT-14-09")]
        public void RemoveSeries()
        {
            var template = RoutineSeeds.PiramideQuinientos().Steps;
            var result = RoutineTemplate.RemoveSeries(template, template[1].GroupId!);
            Assert.That(result.Count, Is.EqualTo(2), "Pasos (5) baja 3");
            Assert.That(result[0].Type, Is.EqualTo(W));
            Assert.That(result[1].Type, Is.EqualTo(C));
        }

        // AC-RUT-14-11: cambiar el ×N de una serie no toca la otra (aunque sean contiguas).
        [Test, Category("AC-RUT-14-11")]
        public void RepeatChangeTouchesOnlyItsSeries()
        {
            var template = RoutineSeeds.PiramideQuinientos(withSecondSeries: true).Steps;
            var first = template[1].GroupId!;
            var second = template[4].GroupId!;
            var result = RoutineTemplate.SetRepeatCount(template, first, 6);
            Assert.That(RoutineTemplate.RepeatCountOf(result, first), Is.EqualTo(6));
            Assert.That(RoutineTemplate.RepeatCountOf(result, second), Is.EqualTo(2), "la otra serie no cambia");
            for (var i = 1; i <= 3; i++)
            {
                Assert.That(result[i].GroupRepeatCount, Is.EqualTo(6), "todos los pasos de la serie");
            }

            Assert.That(RoutineTemplate.RepeatCountOf(Template(RoutineSeeds.Step(K, dur: 60, g: "x")), "x"), Is.EqualTo(1), "?? 1 (:153)");
        }

        // :56-60: al guardar, order 0…n−1 y el routineId de la rutina (0 si es nueva).
        [Test, Category("AC-RUT-11-06")]
        public void Renumber()
        {
            var template = new List<IntervalStep>(RoutineSeeds.EscaleraSpm().Steps);
            template.Insert(0, RoutineSeeds.Step(W, dur: 30) with { Order = 7, RoutineId = 99 });
            var result = RoutineTemplate.Renumber(template, 12);
            for (var i = 0; i < result.Count; i++)
            {
                Assert.That(result[i].Order, Is.EqualTo(i));
                Assert.That(result[i].RoutineId, Is.EqualTo(12));
            }

            Assert.That(RoutineTemplate.Renumber(template, 0)[0].RoutineId, Is.EqualTo(0), "una rutina nueva");
        }

        private static IReadOnlyList<IntervalStep> Template(params IntervalStep[] steps)
        {
            return RoutineSeeds.Make("t", RoutineSeeds.Day(20), steps).Steps;
        }

        private static string Describe(IntervalStep step)
        {
            return step.Type + " " + step.DurationSeconds + " s " + step.GroupId + " ×" + step.GroupRepeatCount;
        }
    }
}
