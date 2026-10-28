using System.Collections.Generic;
using NUnit.Framework;
using RowMate.Core.Models;
using RowMate.Core.Routines;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Routines
{
    // RSB: la secuencia de la mini-barra sin expandir (AC-RUT-10-11): un tramo por paso de la rutina expandida, en orden.
    [TestFixture]
    public sealed class RoutineStructureTests
    {
        private const string W = "Warmup";
        private const string K = "Work";
        private const string R = "Rest";
        private const string C = "Cooldown";

        [Test, Category("AC-RUT-10-11")]
        public void FixtureSegments()
        {
            Assert.That(Types(RoutineSeeds.EscaleraSpm(), 64), Is.EqualTo(new[] { W, K, R, K, R, K, R, C }), "Escalera SPM ×3 → 8");
            var escaleraX4 = RoutineSeeds.EscaleraSpm();
            escaleraX4 = escaleraX4 with { Steps = RoutineTemplate.SetRepeatCount(escaleraX4.Steps, escaleraX4.Steps[1].GroupId!, 4) };
            Assert.That(Types(escaleraX4, 64), Is.EqualTo(new[] { W, K, R, K, R, K, R, K, R, C }), "×4 → 10");
            Assert.That(Types(RoutineSeeds.PiramideQuinientos(), 64), Is.EqualTo(new[] { W, K, K, R, K, K, R, K, K, R, K, K, R, C }), "Pirámide 500 → 14");
            Assert.That(Types(RoutineSeeds.SoloCalentamiento(), 64), Is.EqualTo(new[] { W }), "Solo calentamiento → 1");
            Assert.That(Types(RoutineSeeds.IntervalosCuatro(), 64), Is.EqualTo(new[] { K, R, K, R, K, R, K }), "Intervalos 4× → 7");
            Assert.That(RoutineStructure.Segments(RoutineSeeds.PiramideQuinientos().Steps, 64).Total, Is.EqualTo(14));
            Assert.That(RoutineStructure.Segments(RoutineSeeds.PiramideQuinientos().Steps, 5).Total, Is.EqualTo(14), "Total no depende de max");
            Assert.That(Types(RoutineSeeds.PiramideQuinientos(), 5), Is.EqualTo(new[] { W, K, K, R, K }), "Leading corta en max");
            foreach (var routine in new[] { RoutineSeeds.EscaleraSpm(), RoutineSeeds.PiramideQuinientos(), RoutineSeeds.CasosBorde() })
            {
                var flattened = RoutineExpansion.Flatten(routine.Steps);
                var segments = RoutineStructure.Segments(routine.Steps, 1000);
                Assert.That(segments.Total, Is.EqualTo(flattened.Count), routine.Name + ": igual que la expansión");
                for (var i = 0; i < flattened.Count; i++)
                {
                    Assert.That(segments.Leading[i], Is.EqualTo(flattened[i].Type), routine.Name + " tramo " + i);
                }
            }
        }

        // F-RARA: ×2 000 000 000 no expande (ni bucles ni memoria); ×0 y ×−3 no aportan tramos.
        [Test, Category("AC-RUT-10-11")]
        public void ImpossibleRepeatsNeverExpand()
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var huge = RoutineStructure.Segments(RoutineSeeds.RaraHuge.Routines[0].Steps, 64);
            Assert.That(huge.Total, Is.EqualTo(4000000000L), "2 pasos × 2 000 000 000");
            Assert.That(huge.Leading.Count, Is.EqualTo(64));
            Assert.That(huge.Leading[0], Is.EqualTo(StepType.Work));
            Assert.That(huge.Leading[1], Is.EqualTo(StepType.Rest));
            Assert.That(huge.Leading[63], Is.EqualTo(StepType.Rest));
            Assert.That(watch.ElapsedMilliseconds, Is.LessThan(500));
            var zero = RoutineStructure.Segments(RoutineSeeds.RaraZero.Routines[0].Steps, 64);
            Assert.That(zero.Total, Is.EqualTo(0));
            Assert.That(zero.Leading, Is.Empty);
            Assert.That(RoutineStructure.Segments(RoutineSeeds.RaraNegativeRepeat.Routines[0].Steps, 64).Total, Is.EqualTo(0));
            Assert.That(RoutineStructure.Segments(new IntervalStep[0], 64).Total, Is.EqualTo(0));
            Assert.That(RoutineStructure.Segments(RoutineSeeds.EscaleraSpm().Steps, 0).Leading, Is.Empty);
            Assert.That(RoutineStructure.Segments(RoutineSeeds.EscaleraSpm().Steps, -5).Leading, Is.Empty);
        }

        private static string[] Types(Routine routine, int max)
        {
            var leading = RoutineStructure.Segments(routine.Steps, max).Leading;
            var result = new List<string>(leading.Count);
            foreach (var type in leading)
            {
                result.Add(type.ToString());
            }

            return result.ToArray();
        }
    }
}
