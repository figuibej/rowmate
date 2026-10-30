using NUnit.Framework;
using RowMate.App.Workout;
using RowMate.Core.Design;
using RowMate.Core.Localization;
using RowMate.Core.Workout;
using RowMate.Tests.EditMode.Localization;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Workout
{
    // PCH: el chip de fase de la barra libre (F-WKT-16; AC-WKT-16-03 a -05; DV-41, DV-44; DN4b-15, -26; 02-tech-design §2.7 de la
    // It. 4b). legacy: lib/features/workout/immersive_workout_screen.dart:530-547 (_PhaseChip).
    [TestFixture]
    public sealed class PhaseChipTests
    {
        [Test, Category("AC-WKT-16-03")]
        public void TextAndColorsPerPhase()
        {
            var active = PhaseChip.For(WorkoutPhase.Active)!.Value;
            Assert.That(active.Key, Is.EqualTo(L10nKeys.HudPhaseActive));
            Assert.That(active.Base, Is.SameAs(Tokens.PhaseRunning));
            Assert.That(active.Text, Is.SameAs(Tokens.PhaseRunning));
            var paused = PhaseChip.For(WorkoutPhase.Paused)!.Value;
            Assert.That(paused.Key, Is.EqualTo(L10nKeys.HudPhasePaused));
            Assert.That(paused.Base, Is.SameAs(Tokens.PhasePaused));
            Assert.That(paused.Text, Is.SameAs(Tokens.PhasePaused));
            var finished = PhaseChip.For(WorkoutPhase.Finished)!.Value;
            Assert.That(finished.Key, Is.EqualTo(L10nKeys.HudPhaseFinished));
            Assert.That(finished.Base, Is.SameAs(Tokens.PhaseFinished), "el tinte y el borde en el color base (#EF476F)");
            Assert.That(finished.Text, Is.SameAs(Tokens.PhaseFinishedHud), "el texto en la variante legible (DV-44)");
            Assert.That(Tokens.PhaseFinishedHud.Rgb.ToHex(), Is.EqualTo("#FF7A92"));
            Assert.That(Tokens.PhaseFinishedHud.Role, Is.EqualTo(ColorRole.HudText));

            // Los textos del catálogo, con el español del legacy (:531-533; Q-01).
            Assert.That(LocalizationFixture.Es.Get(active.Key), Is.EqualTo("EN MARCHA"));
            Assert.That(LocalizationFixture.Es.Get(paused.Key), Is.EqualTo("PAUSADO"));
            Assert.That(LocalizationFixture.Es.Get(finished.Key), Is.EqualTo("FINALIZADO"));
            Assert.That(LocalizationFixture.En.Get(active.Key), Is.EqualTo("ACTIVE"));
            Assert.That(LocalizationFixture.En.Get(paused.Key), Is.EqualTo("PAUSED"));
            Assert.That(LocalizationFixture.En.Get(finished.Key), Is.EqualTo("FINISHED"));
            Assert.That(HudGeometry.PhaseChipTintAlpha, Is.EqualTo(0.15), ":543 withOpacity(0.15)");
            Assert.That(HudGeometry.PhaseChipBorderAlpha, Is.EqualTo(0.50), ":545 withOpacity(0.5)");
        }

        [Test, Category("AC-WKT-16-03")]
        public void FinishedTextContrast()
        {
            // El par B1 de 04-design-notes §8.1 (DN4b-29), con Contrast: el texto de cada fase sobre su tinte al 15 % sobre el peor
            // vidrio: 5,324 · 6,432 · 4,677 (± 0,001). #FF5D7F (step.work.hud) daba 3,942 (DV-44).
            Assert.That(OverItsTint(Tokens.PhaseRunning, "#06D6A0"), Is.EqualTo(5.324).Within(0.001));
            Assert.That(OverItsTint(Tokens.PhasePaused, "#FFD166"), Is.EqualTo(6.432).Within(0.001));
            Assert.That(OverItsTint(Tokens.PhaseFinishedHud, "#EF476F"), Is.EqualTo(4.677).Within(0.001));
            Assert.That(OverItsTint(Tokens.StepWorkHud, "#EF476F"), Is.EqualTo(3.942).Within(0.001), "la de la spec no llegaba");
        }

        [Test, Category("AC-WKT-16-04")]
        public void IdleHasNoChip()
        {
            // Reposo → null: el "—" del legacy (:534) nunca se ve, porque la salida conserva la última foto (DV-41). FINALIZADO
            // aparece mientras se guarda un libre terminado a mano (la fase es Finished).
            Assert.That(PhaseChip.For(WorkoutPhase.Idle).HasValue, Is.False);
            using (var h = new WorkoutHarness(sessionsFor: s => new ScriptedSessionRepository(s) { FinishDelay = System.TimeSpan.FromSeconds(2) }))
            using (var hud = new WorkoutHudStore(h.Engine, Texts(), h.Scheduler, h.Lifecycle, h.Logger))
            {
                h.Free();
                h.At(10);
                Assert.That(hud.State.Value.Chip!.Text, Is.EqualTo("EN MARCHA"));
                h.Engine.FinishAsync();
                Assert.That(hud.State.Value.Chip!.Text, Is.EqualTo("FINALIZADO"), "mientras se guarda");
                Assert.That(hud.State.Value.Chip!.TextColor, Is.SameAs(Tokens.PhaseFinishedHud));
                h.At(12.5);
                Assert.That(hud.State.Value.Phase, Is.EqualTo(WorkoutPhase.Idle));
                Assert.That(hud.State.Value.Chip, Is.Null, "en reposo, sin chip");
            }
        }

        [Test, Category("AC-WKT-16-05")]
        public void LiveDotOnlyWhenActive()
        {
            Assert.That(PhaseChip.For(WorkoutPhase.Active)!.Value.LiveDot, Is.True);
            Assert.That(PhaseChip.For(WorkoutPhase.Paused)!.Value.LiveDot, Is.False);
            Assert.That(PhaseChip.For(WorkoutPhase.Finished)!.Value.LiveDot, Is.False);
        }

        [Test, Category("AC-WKT-16-03")]
        public void FixedWidth()
        {
            // DT4b-11, DN4b-26: max(EN MARCHA + 10 del punto, PAUSADO, FINALIZADO) + 16, con los anchos de 04-design-notes §3.5
            // (11/700, 0,08 em): 101,8 base en es y 74,1 en en; el lugar del punto solo en EN MARCHA.
            Assert.That(PhaseChip.FixedWidthBase(75.8, 60.1, 76.2), Is.EqualTo(101.8).Within(0.1));
            Assert.That(PhaseChip.FixedWidthBase(46.5, 49.4, 58.1), Is.EqualTo(74.1).Within(0.1));
            Assert.That(PhaseChip.FixedWidthBase(80, 10, 10), Is.EqualTo(80 + 10 + 16), "el punto solo suma en EN MARCHA");
            Assert.That(PhaseChip.DotSlot, Is.EqualTo(HudGeometry.LiveDotSize + HudGeometry.LiveDotGap));
            Assert.That(PhaseChip.PadH, Is.EqualTo(HudGeometry.PhaseChipPadH));
        }

        private static double OverItsTint(ColorToken text, string baseHex)
        {
            var tint = ColorToken.HexAlpha("test.tint", baseHex, 15);
            var background = Contrast.CompositeSrgb(tint, Contrast.WorstGlassBackground());
            return Contrast.RatioOver(text, background);
        }

        private static RowMate.Core.Common.IReadOnlyObservable<Localizer> Texts()
        {
            return new RowMate.Core.Common.ReadOnlyObservable<Localizer>(new RowMate.Core.Common.ObservableValue<Localizer>(LocalizationFixture.Es,
                RowMate.Core.Common.NullAppLogger.Instance));
        }
    }
}
