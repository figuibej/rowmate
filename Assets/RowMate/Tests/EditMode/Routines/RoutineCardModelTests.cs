using NUnit.Framework;
using RowMate.App.Routines;
using RowMate.Core.Models;
using RowMate.Core.Routines;
using RowMate.Tests.EditMode.Localization;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Routines
{
    // RCM: la tarjeta de una rutina (AC-RUT-10-03, -10; lib/features/routines/routines_screen.dart:41-84).
    [TestFixture]
    public sealed class RoutineCardModelTests
    {
        // AC-RUT-10-03: la descripción solo si no está vacía (:56-58, isNotEmpty sin recortar); nombre y descripción tal cual.
        [Test, Category("AC-RUT-10-03")]
        public void DescriptionOnlyWhenNotEmpty()
        {
            var piramide = RoutineCardModel.For(RoutineSeeds.PiramideQuinientos(), LocalizationFixture.Es);
            Assert.That(piramide.ShowsDescription, Is.True);
            Assert.That(piramide.Description, Is.EqualTo("Bajar 2 s el split en cada vuelta"));
            var escalera = RoutineCardModel.For(RoutineSeeds.EscaleraSpm(), LocalizationFixture.Es);
            Assert.That(escalera.ShowsDescription, Is.False);
            var marked = RoutineSeeds.EscaleraSpm() with { Name = "<b>A</b>", Description = "Serie\\n2\nsegunda línea" };
            var card = RoutineCardModel.For(marked, LocalizationFixture.Es);
            Assert.That(card.Name, Is.EqualTo("<b>A</b>"), "sin interpretar marcas");
            Assert.That(card.Description, Is.EqualTo("Serie\\n2\nsegunda línea"), "la barra y la n escritas a mano quedan; el salto real también");
            Assert.That(RoutineCardModel.For(marked with { Description = " " }, LocalizationFixture.Es).ShowsDescription, Is.True, "isNotEmpty: un espacio cuenta");
            Assert.That(card.Template, Is.SameAs(marked.Steps), "la plantilla para la mini-barra");
            Assert.That(card.Id, Is.EqualTo(marked.Id));
        }

        // AC-RUT-10-10 (caso 11, AC-RUT-05-02): los 5 resúmenes de la spec, en es y en; la plantilla, no la expandida.
        [TestCase("Escalera SPM", "1 serie · 1 descanso", "1 interval · 1 rest", Category = "AC-RUT-10-10")]
        [TestCase("Pirámide 500", "2 series · 1 descanso", "2 intervals · 1 rest", Category = "AC-RUT-10-10")]
        [TestCase("Solo calentamiento", "1 paso", "1 step", Category = "AC-RUT-10-10")]
        [TestCase("Intervalos 4×", "4 series · 3 descansos", "4 intervals · 3 rests", Category = "AC-RUT-10-10")]
        [TestCase("Casos borde", "7 series · 2 descansos", "7 intervals · 2 rests", Category = "AC-RUT-10-10")]
        public void SummariesEsEn(string name, string es, string en)
        {
            var routine = Find(name);
            Assert.That(RoutineCardModel.For(routine, LocalizationFixture.Es).Summary, Is.EqualTo(es));
            Assert.That(RoutineCardModel.For(routine, LocalizationFixture.En).Summary, Is.EqualTo(en));
            Assert.That(RoutineCardModel.For(routine, LocalizationFixture.Es).Name, Is.EqualTo(name));
        }

        // ×4 en Escalera SPM no cambia el resumen (cuenta la plantilla).
        [Test, Category("AC-RUT-10-10")]
        public void RepeatsDoNotChangeTheSummary()
        {
            var escalera = RoutineSeeds.EscaleraSpm();
            var x4 = escalera with { Steps = RoutineTemplate.SetRepeatCount(escalera.Steps, escalera.Steps[1].GroupId!, 4) };
            Assert.That(RoutineCardModel.For(x4, LocalizationFixture.Es).Summary, Is.EqualTo("1 serie · 1 descanso"));
        }

        private static Routine Find(string name)
        {
            foreach (var routine in RoutineSeeds.Lista.Routines)
            {
                if (routine.Name == name)
                {
                    return routine;
                }
            }

            return RoutineSeeds.CasosBorde();
        }
    }
}
