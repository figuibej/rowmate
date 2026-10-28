using System;
using NUnit.Framework;
using RowMate.Core.Models;
using RowMate.Core.Routines;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Routines
{
    // SIG: 'g${DateTime.now().millisecondsSinceEpoch}_${_idCounter++}' (lib/features/routines/routine_editor_screen.dart:111).
    [TestFixture]
    public sealed class SeriesIdGeneratorTests
    {
        // AC-RUT-08-06: dos series en el mismo milisegundo (reloj fijo) tienen ids distintos por el contador.
        [Test, Category("AC-RUT-08-06")]
        public void SameMillisecondGivesDistinctIds()
        {
            var clock = new FakeScheduler(new DateTimeOffset(2026, 10, 27, 13, 0, 0, TimeSpan.Zero));
            var ids = new SeriesIdGenerator(clock);
            var template = new IntervalStep[0];
            var first = ids.Next(template);
            var second = ids.Next(template);
            Assert.That(first, Is.EqualTo("g" + new DateTimeOffset(2026, 10, 27, 13, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds() + "_0"));
            Assert.That(second, Is.EqualTo("g1793106000000_1"));
            Assert.That(first, Is.Not.EqualTo(second));
            clock.Advance(TimeSpan.FromMilliseconds(5));
            Assert.That(ids.Next(template), Is.EqualTo("g1793106000005_2"), "el contador es del editor, no del milisegundo");
        }

        // Si el id ya está en la plantilla (una rutina guardada en otra sesión con el mismo milisegundo), sigue contando.
        [Test, Category("AC-RUT-08-06")]
        public void SkipsIdsAlreadyInTheTemplate()
        {
            var clock = new FakeScheduler(new DateTimeOffset(2026, 10, 27, 13, 0, 0, TimeSpan.Zero));
            var template = RoutineSeeds.Make("t", RoutineSeeds.Day(20),
                RoutineSeeds.Step(StepType.Work, dur: 60, g: "g1793106000000_0"), RoutineSeeds.Step(StepType.Work, dur: 60, g: "g1793106000000_1")).Steps;
            Assert.That(new SeriesIdGenerator(clock).Next(template), Is.EqualTo("g1793106000000_2"));
        }
    }
}
