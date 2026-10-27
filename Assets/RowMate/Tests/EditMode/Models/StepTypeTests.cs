using System;
using NUnit.Framework;
using RowMate.Core.Design;
using RowMate.Core.Models;
using RowMate.Tests.EditMode.Localization;

namespace RowMate.Tests.EditMode.Models
{
    // F-RUT-01 (+ F-APP-07): tipos de paso (legacy: interval_step.dart:1, :115, :133; theme.dart:42-69;
    // routine_editor_screen.dart:447-458).
    [TestFixture]
    public sealed class StepTypeTests
    {
        // AC-RUT-01-01: exactamente 4 tipos, en el orden del selector, y su nombre guardado en minúsculas.
        [Test, Category("AC-RUT-01-01")]
        public void FourTypesInSelectorOrderWithStorageNames()
        {
            Assert.That(Enum.GetValues(typeof(StepType)), Is.EqualTo(new[] { StepType.Warmup, StepType.Work, StepType.Rest, StepType.Cooldown }));
            Assert.That(StepTypeInfo.SelectorOrder, Is.EqualTo(new[] { StepType.Warmup, StepType.Work, StepType.Rest, StepType.Cooldown }));
            Assert.That(StepTypeNames.ToStorage(StepType.Warmup), Is.EqualTo("warmup"));
            Assert.That(StepTypeNames.ToStorage(StepType.Work), Is.EqualTo("work"));
            Assert.That(StepTypeNames.ToStorage(StepType.Rest), Is.EqualTo("rest"));
            Assert.That(StepTypeNames.ToStorage(StepType.Cooldown), Is.EqualTo("cooldown"));
            foreach (var type in StepTypeInfo.SelectorOrder)
            {
                Assert.That(StepTypeNames.Parse(StepTypeNames.ToStorage(type)), Is.EqualTo(type));
                Assert.That(StepTypeNames.TryParse(StepTypeNames.ToStorage(type), out var parsed) && parsed == type, Is.True);
            }
        }

        // AC-RUT-01-02: el nombre visible sale del catálogo.
        [TestCase(StepType.Warmup, "stepTypeWarmup", "Calentamiento", "Warmup")]
        [TestCase(StepType.Work, "stepTypeWork", "Trabajo", "Work")]
        [TestCase(StepType.Rest, "stepTypeRest", "Descanso", "Rest")]
        [TestCase(StepType.Cooldown, "stepTypeCooldown", "Enfriamiento", "Cooldown")]
        [Category("AC-RUT-01-02")]
        public void NamesFromCatalog(StepType type, string key, string es, string en)
        {
            Assert.That(StepTypeInfo.NameKey(type), Is.EqualTo(key));
            Assert.That(LocalizationFixture.Es.Get(StepTypeInfo.NameKey(type)), Is.EqualTo(es));
            Assert.That(LocalizationFixture.En.Get(StepTypeInfo.NameKey(type)), Is.EqualTo(en));
        }

        // AC-RUT-01-03: colores de la fuente única (AC-BASE-04): misma instancia de Tokens y hex de contrato.
        [Test, Category("AC-RUT-01-03")]
        public void ColorsComeFromTokens()
        {
            Assert.That(StepTypeInfo.Color(StepType.Work), Is.SameAs(Tokens.StepWork));
            Assert.That(StepTypeInfo.Color(StepType.Rest), Is.SameAs(Tokens.StepRest));
            Assert.That(StepTypeInfo.Color(StepType.Warmup), Is.SameAs(Tokens.StepWarmup));
            Assert.That(StepTypeInfo.Color(StepType.Cooldown), Is.SameAs(Tokens.StepCooldown));
            Assert.That(StepTypeInfo.Color(StepType.Work).Rgb.ToHex(), Is.EqualTo("#EF476F"));
            Assert.That(StepTypeInfo.Color(StepType.Rest).Rgb.ToHex(), Is.EqualTo("#06D6A0"));
            Assert.That(StepTypeInfo.Color(StepType.Warmup).Rgb.ToHex(), Is.EqualTo("#FFD166"));
            Assert.That(StepTypeInfo.Color(StepType.Cooldown).Rgb.ToHex(), Is.EqualTo("#118AB2"));

            Assert.That(StepTypeInfo.ColorForStorageName("work"), Is.SameAs(Tokens.StepWork));
            Assert.That(StepTypeInfo.ColorForStorageName("rest"), Is.SameAs(Tokens.StepRest));
            Assert.That(StepTypeInfo.ColorForStorageName("warmup"), Is.SameAs(Tokens.StepWarmup));
            Assert.That(StepTypeInfo.ColorForStorageName("cooldown"), Is.SameAs(Tokens.StepCooldown));
            // Cualquier otro valor: texto vacío, "sprint", un punto de telemetría sin tipo (null), mayúsculas.
            foreach (var other in new[] { "", "sprint", null, "Work", " work" })
            {
                Assert.That(StepTypeInfo.ColorForStorageName(other), Is.SameAs(Tokens.StepUnknown), "\"" + other + "\"");
            }

            Assert.That(Tokens.StepUnknown.Rgb.ToHex(), Is.EqualTo("#607D8B"));
            Assert.That(Tokens.StepUnknown.IsContract && Tokens.StepWork.IsContract, Is.True);
        }

        // AC-RUT-01-04: el ícono de cada tipo (dato).
        [TestCase(StepType.Warmup, "whatshot")]
        [TestCase(StepType.Work, "fitness_center")]
        [TestCase(StepType.Rest, "pause_circle_outline")]
        [TestCase(StepType.Cooldown, "ac_unit")]
        [Category("AC-RUT-01-04")]
        public void IconNames(StepType type, string icon)
        {
            Assert.That(StepTypeInfo.IconName(type), Is.EqualTo(icon));
        }

        [Test, Category("AC-RUT-01-04")]
        [Ignore("NO VERIFICABLE → It. 3: [Editor] el ícono de cada tipo en el editor de rutinas (glifo sin relleno para descanso, P-07)")]
        public void IconsInEditor()
        {
        }

        // AC-RUT-01-05: un nombre que no es ninguno de los 4 es un error (no cae en un tipo por defecto).
        [TestCase("sprint")]
        [TestCase("")]
        [TestCase("Work")]
        [TestCase("WARMUP")]
        [TestCase(" rest")]
        [TestCase("cool down")]
        [Category("AC-RUT-01-05")]
        public void UnknownNameIsError(string name)
        {
            Assert.Throws<FormatException>(() => StepTypeNames.Parse(name));
            Assert.That(StepTypeNames.TryParse(name, out _), Is.False);
        }

        [Test, Category("AC-RUT-01-05")]
        public void NullNameIsError()
        {
            Assert.Throws<FormatException>(() => StepTypeNames.Parse(null!));
            Assert.That(StepTypeNames.TryParse(null, out _), Is.False);
            Assert.Throws<ArgumentOutOfRangeException>(() => StepTypeNames.ToStorage((StepType)99));
        }
    }
}
