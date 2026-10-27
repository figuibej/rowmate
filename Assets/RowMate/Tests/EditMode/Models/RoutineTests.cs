using System;
using NUnit.Framework;
using RowMate.Core.Models;

namespace RowMate.Tests.EditMode.Models
{
    // F-RUT-05: modelo de rutina (legacy: lib/core/models/routine.dart:8-21).
    [TestFixture]
    public sealed class RoutineTests
    {
        // AC-RUT-05-01: nombre obligatorio, descripción vacía por defecto, fecha de creación y la PLANTILLA de pasos
        // sin expandir (por defecto, ninguno).
        [Test, Category("AC-RUT-05-01")]
        public void AttributesAndDefaults()
        {
            var created = new DateTimeOffset(2026, 10, 27, 9, 0, 0, TimeSpan.FromHours(-3));
            var routine = new Routine("Escalera SPM", created);
            Assert.That(routine.Name, Is.EqualTo("Escalera SPM"));
            Assert.That(routine.Description, Is.EqualTo(""));
            Assert.That(routine.CreatedAt, Is.EqualTo(created));
            Assert.That(routine.Steps, Is.Empty);
            Assert.That(routine.Id, Is.Null);
            Assert.That(routine.FlattenedSteps(), Is.Empty);
            Assert.That(routine.FlattenedStepPositions(), Is.Empty);

            Assert.Throws<ArgumentNullException>(() => new Routine(null!, created), "el nombre es obligatorio");
            Assert.Throws<ArgumentNullException>(() => _ = routine with { Name = null! });
            Assert.Throws<ArgumentNullException>(() => _ = routine with { Description = null! });
            Assert.Throws<ArgumentNullException>(() => _ = routine with { Steps = null! });
        }

        [Test, Category("AC-RUT-05-01")]
        public void StepsAreTheTemplateNotTheExpandedRoutine()
        {
            var routine = RoutineFixtures.MakeRoutine("T1", RoutineFixtures.T1) with { Id = 5, Description = "test legacy" };
            Assert.That(routine.Steps.Count, Is.EqualTo(4), "la plantilla");
            Assert.That(routine.FlattenedSteps().Count, Is.EqualTo(8), "la rutina expandida sale aparte");
            Assert.That(routine.FlattenedStepPositions().Count, Is.EqualTo(8));
            Assert.That(routine.Steps, Is.SameAs(RoutineFixtures.T1), "no se copia ni se expande al construir");
            Assert.That(routine.Id, Is.EqualTo(5L));
            Assert.That(routine.Description, Is.EqualTo("test legacy"));
            Assert.That(routine with { }, Is.EqualTo(routine), "igualdad por valor");
        }
    }
}
