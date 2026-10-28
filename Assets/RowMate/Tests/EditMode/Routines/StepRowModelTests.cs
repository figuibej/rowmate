using NUnit.Framework;
using RowMate.App.Routines;
using RowMate.Core.Design;
using RowMate.Core.Models;
using RowMate.Tests.EditMode.Localization;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Routines
{
    // SRM: la fila de un paso (AC-RUT-12-02, AC-RUT-01-04; routine_editor_screen.dart:316-369, :447-458) con DV-12 ampliada
    // (DN3-03): título e ícono en step.<tipo>.hud; borde e insignia en el color base del tipo.
    [TestFixture]
    public sealed class StepRowModelTests
    {
        [TestCase(StepType.Warmup, "Calentamiento", "Warmup", Category = "AC-RUT-12-02")]
        [TestCase(StepType.Work, "Trabajo", "Work", Category = "AC-RUT-12-02")]
        [TestCase(StepType.Rest, "Descanso", "Rest", Category = "AC-RUT-12-02")]
        [TestCase(StepType.Cooldown, "Enfriamiento", "Cooldown", Category = "AC-RUT-12-02")]
        public void TitleColorAndIcon(StepType type, string es, string en)
        {
            var step = RoutineSeeds.Step(type, dur: 300, spm: 18);
            var row = StepRowModel.For(step, LocalizationFixture.Es);
            Assert.That(row.Title, Is.EqualTo(es));
            Assert.That(StepRowModel.For(step, LocalizationFixture.En).Title, Is.EqualTo(en));
            Assert.That(row.Type, Is.EqualTo(type));
            Assert.That(row.Subtitle.Text, Is.EqualTo("5m  ·  18 spm"));
            Assert.That(row.BorderColor, Is.SameAs(StepTypeInfo.Color(type)), "borde en el color base");
            Assert.That(row.BadgeColor, Is.SameAs(StepTypeInfo.Color(type)), "fondo de la insignia en el color base (la vista lo pone al 20 %)");
            Assert.That(row.TitleColor, Is.SameAs(row.IconColor), "título e ícono en el mismo color");
            Assert.That(row.TitleColor.Name, Is.EqualTo(StepTypeInfo.Color(type).Name + ".hud"), "la variante legible");
        }

        // DN3-03: trabajo #FF5D7F y enfriamiento #37A2CB (DV-12 ampliada); descanso y calentamiento no cambian.
        [Test, Category("AC-RUT-12-02")]
        public void HudVariantsOfTheSpec()
        {
            Assert.That(StepRowModel.HudColor(StepType.Work), Is.SameAs(Tokens.StepWorkHud));
            Assert.That(StepRowModel.HudColor(StepType.Cooldown), Is.SameAs(Tokens.StepCooldownHud));
            Assert.That(StepRowModel.HudColor(StepType.Rest), Is.SameAs(Tokens.StepRestHud));
            Assert.That(StepRowModel.HudColor(StepType.Warmup), Is.SameAs(Tokens.StepWarmupHud));
            Assert.That(Tokens.StepWorkHud.Name, Is.EqualTo("step.work.hud"));
            Assert.That(Tokens.StepCooldownHud.Name, Is.EqualTo("step.cooldown.hud"));
        }

        // AC-RUT-01-04: whatshot, fitness_center, pause_circle (contorno) y ac_unit.
        [Test, Category("AC-RUT-12-02")]
        public void IconsByType()
        {
            Assert.That(StepRowModel.IconFor(StepType.Warmup), Is.EqualTo(Icons.Whatshot));
            Assert.That(StepRowModel.IconFor(StepType.Work), Is.EqualTo(Icons.FitnessCenter));
            Assert.That(StepRowModel.IconFor(StepType.Rest), Is.EqualTo(Icons.PauseCircle));
            Assert.That(StepRowModel.IconFor(StepType.Cooldown), Is.EqualTo(Icons.AcUnit));
            Assert.That(Icons.Get("pause_circle").InOutline, Is.True, "descanso en contorno (P-07)");
        }
    }
}
