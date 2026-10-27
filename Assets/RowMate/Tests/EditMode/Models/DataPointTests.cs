using System.Reflection;
using NUnit.Framework;
using RowMate.Core.Models;

namespace RowMate.Tests.EditMode.Models
{
    // F-RUT-17: punto de telemetría (legacy: workout_session.dart:140-182; D-12, QA-11).
    [TestFixture]
    public sealed class DataPointTests
    {
        // AC-RUT-17-01: sesión y tiempo transcurrido obligatorios y sin valor por defecto; los siete valores en 0; índice y
        // tipo del paso vacíos; id vacío hasta guardarse.
        [Test, Category("AC-RUT-17-01")]
        public void AttributesAndDefaults()
        {
            var constructors = typeof(DataPoint).GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            Assert.That(constructors.Length, Is.EqualTo(1));
            var parameters = constructors[0].GetParameters();
            Assert.That(parameters.Length, Is.EqualTo(2), "sesión y tiempo, sin valores por defecto");
            Assert.That(parameters[0].Name, Is.EqualTo("sessionId"));
            Assert.That(parameters[1].Name, Is.EqualTo("elapsedSeconds"));
            Assert.That(parameters[0].HasDefaultValue || parameters[1].HasDefaultValue, Is.False);

            var point = new DataPoint(12, 345);
            Assert.That(point.SessionId, Is.EqualTo(12L));
            Assert.That(point.ElapsedSeconds, Is.EqualTo(345), "el tiempo del monitor");
            Assert.That(point.StrokeRate, Is.EqualTo(0.0));
            Assert.That(point.StrokeCount, Is.EqualTo(0));
            Assert.That(point.DistanceMeters, Is.EqualTo(0));
            Assert.That(point.Pace500mSeconds, Is.EqualTo(0));
            Assert.That(point.PowerWatts, Is.EqualTo(0));
            Assert.That(point.Calories, Is.EqualTo(0));
            Assert.That(point.HeartRate, Is.EqualTo(0));
            Assert.That(point.StepIndex, Is.Null);
            Assert.That(point.StepTypeName, Is.Null, "vacío en entrenamiento libre");
            Assert.That(point.Id, Is.Null);
            Assert.That(typeof(DataPoint).GetProperty("StrokeRate")!.PropertyType, Is.EqualTo(typeof(double)), "SPM decimal");
            Assert.That(typeof(DataPoint).GetProperty("StepTypeName")!.PropertyType, Is.EqualTo(typeof(string)), "el tipo se guarda como texto");
        }

        [TestCase(StepType.Warmup, "warmup")]
        [TestCase(StepType.Work, "work")]
        [TestCase(StepType.Rest, "rest")]
        [TestCase(StepType.Cooldown, "cooldown")]
        [Category("AC-RUT-17-01")]
        public void StepTypeIsStoredByName(StepType type, string name)
        {
            var point = new DataPoint(1, 0).WithStep(2, type);
            Assert.That(point.StepTypeName, Is.EqualTo(name));
            Assert.That(point.StepIndex, Is.EqualTo(2));
        }

        // AC-RUT-17-02: el índice es el de la rutina EXPANDIDA: en T1, el segundo trabajo es el índice 3 (no el 1 de la plantilla).
        [Test, Category("AC-RUT-17-02")]
        public void StepIndexRefersToExpandedRoutine()
        {
            var flat = RoutineExpansion.Flatten(RoutineFixtures.T1);
            var secondWork = -1;
            var works = 0;
            for (var i = 0; i < flat.Count; i++)
            {
                if (flat[i].Type == StepType.Work && ++works == 2)
                {
                    secondWork = i;
                    break;
                }
            }

            Assert.That(secondWork, Is.EqualTo(3));
            var point = new DataPoint(1, 420) { PowerWatts = 180, HeartRate = 150 }.WithStep(secondWork, flat[secondWork].Type);
            Assert.That(point.StepIndex, Is.EqualTo(3));
            Assert.That(point.StepTypeName, Is.EqualTo("work"));
            Assert.That(RoutineFixtures.T1[1].Type, Is.EqualTo(StepType.Work), "en la plantilla el trabajo es el índice 1");
            Assert.That(point.PowerWatts, Is.EqualTo(180), "WithStep no toca el resto");
            Assert.That(point.ElapsedSeconds, Is.EqualTo(420));
        }
    }
}
