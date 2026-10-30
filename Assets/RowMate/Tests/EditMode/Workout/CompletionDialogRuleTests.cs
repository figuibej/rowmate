using NUnit.Framework;
using RowMate.App.Workout;
using RowMate.Core.Common;
using RowMate.Core.Localization;
using RowMate.Core.Workout;
using RowMate.Tests.EditMode.Localization;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Workout
{
    // CDR: la regla del diálogo de rutina completada (F-WKT-21; 02-tech-design §2.6 de la It. 4b), sobre el estado del HUD que
    // arma el store con el motor de verdad. legacy: immersive_workout_screen.dart:48-54 (_hasShownCompletionDialog).
    [TestFixture]
    public sealed class CompletionDialogRuleTests
    {
        [Test, Category("AC-WKT-21-01"), Category("AC-WKT-13-11")]
        public void OncePerSession()
        {
            // A lo sumo una vez por sesión: más estados de la misma sesión (lecturas, rotar, minimizar) no lo vuelven a pedir; una
            // sesión nueva tiene el suyo.
            using (var h = new WorkoutHarness())
            using (var hud = Hud(h))
            {
                h.Routine(RoutineSeeds.Corta());
                h.At(29.9);
                Assert.That(CompletionDialogRule.ShouldOpen(hud.State.Value, 0), Is.False, "todavía no");
                h.At(30);
                var first = hud.State.Value;
                Assert.That(CompletionDialogRule.ShouldOpen(first, 0), Is.True);
                var shown = first.Session;
                h.At(35);
                Assert.That(CompletionDialogRule.ShouldOpen(hud.State.Value, shown), Is.False, "una vez");
                h.Engine.AcknowledgeCompletion();
                Assert.That(CompletionDialogRule.ShouldOpen(hud.State.Value, shown), Is.False, "en reposo");
                h.Engine.StartWithRoutine(RoutineSeeds.Corta());
                h.At(66);
                Assert.That(hud.State.Value.Session, Is.EqualTo(shown + 1));
                Assert.That(CompletionDialogRule.ShouldOpen(hud.State.Value, shown), Is.True, "la sesión nueva tiene el suyo");
            }
        }

        [Test, Category("AC-WKT-21-07"), Category("AC-WKT-21-01")]
        public void NeverOnManual()
        {
            // Q-53: nunca al terminar a mano, ni en libre, ni en reposo.
            using (var h = new WorkoutHarness(sessionsFor: s => new ScriptedSessionRepository(s) { FinishDelay = System.TimeSpan.FromSeconds(2) }))
            using (var hud = Hud(h))
            {
                h.Routine(RoutineSeeds.Corta());
                h.At(12);
                h.Engine.FinishAsync();
                Assert.That(hud.State.Value.Phase, Is.EqualTo(WorkoutPhase.Finished));
                Assert.That(hud.State.Value.EndedBy, Is.EqualTo(WorkoutEndKind.Manual));
                Assert.That(CompletionDialogRule.ShouldOpen(hud.State.Value, 0), Is.False, "a mano");
                h.At(15);
                Assert.That(CompletionDialogRule.ShouldOpen(hud.State.Value, 0), Is.False, "en reposo");
                h.Engine.StartFree();
                h.At(30);
                h.Engine.FinishAsync();
                Assert.That(CompletionDialogRule.ShouldOpen(hud.State.Value, 0), Is.False, "en libre");
            }
        }

        [Test, Category("AC-WKT-21-09")]
        public void InstantStateOpensWithoutItsEntry()
        {
            // AC-WKT-21-09: si el primer estado que lo pide es Instant (vuelta del segundo plano), el diálogo ya está.
            using (var h = new WorkoutHarness())
            using (var hud = Hud(h))
            {
                h.Routine(RoutineSeeds.Corta());
                h.At(8);
                h.Lifecycle.SetBackground(true);
                h.At(40);
                h.Lifecycle.SetBackground(false);
                var state = hud.State.Value;
                Assert.That(CompletionDialogRule.ShouldOpen(state, 0), Is.True);
                Assert.That(CompletionDialogRule.OpensInstantly(state), Is.True);
                Assert.That(state.Completion!.Time, Is.EqualTo("00:30"), "los totales finales");
            }

            using (var h = new WorkoutHarness())
            using (var hud = Hud(h))
            {
                h.Routine(RoutineSeeds.Corta());
                h.At(30);
                Assert.That(CompletionDialogRule.OpensInstantly(hud.State.Value), Is.False, "en primer plano, con su entrada");
            }
        }

        private static WorkoutHudStore Hud(WorkoutHarness h)
        {
            var texts = new ReadOnlyObservable<Localizer>(new ObservableValue<Localizer>(LocalizationFixture.Es, NullAppLogger.Instance));
            return new WorkoutHudStore(h.Engine, texts, h.Scheduler, h.Lifecycle, h.Logger);
        }
    }
}
