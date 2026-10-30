using NUnit.Framework;
using RowMate.App.Workout;
using RowMate.Core.Design;
using RowMate.Core.Routines;

namespace RowMate.Tests.EditMode.Design
{
    // IRF: qué cede en la fila de información y en la barra libre (02-tech-design §2.15 de la It. 4b; 04-design-notes §3.3, §3.5;
    // DN4b-16, -27; DV-45 a DV-47), con los anchos de las notas (tablas K2, dp base). El legacy reparte con un Flexible que recibe
    // a lo sumo la mitad del espacio libre y se corta por carácter (immersive_workout_screen.dart:394-416).
    [TestFixture]
    public sealed class InfoRowFitTests
    {
        private const double Tv = 339.9;
        private const double Countdown = 37.7;
        private const double Clock = 69.1;

        [Test, Category("AC-WKT-14-04"), Category("AC-WKT-14-14")]
        public void ModesGapsAndUpNextPadding()
        {
            // Normal 6 · 8 · 10 con Próximo 7 × 3 (:395, :452, :473); compacto 1 con el aire antes de la cuenta y el de la cuenta al
            // reloj en 6; compacto 2, además, Próximo con 4 × 3.
            var normal = new InfoRowPlan(InfoRowMode.Normal, 1, 1);
            Assert.That(normal.CountdownGap, Is.EqualTo(8));
            Assert.That(normal.ClockGap, Is.EqualTo(10));
            Assert.That(normal.UpNextPadH, Is.EqualTo(7));
            var compact1 = new InfoRowPlan(InfoRowMode.Compact1, 1, 1);
            Assert.That(compact1.CountdownGap, Is.EqualTo(6));
            Assert.That(compact1.ClockGap, Is.EqualTo(6));
            Assert.That(compact1.UpNextPadH, Is.EqualTo(7));
            var compact2 = new InfoRowPlan(InfoRowMode.Compact2, 1, 1);
            Assert.That(compact2.UpNextPadH, Is.EqualTo(4));
            Assert.That(InfoRowFit.TypeGap, Is.EqualTo(6), "el aire del tipo a la pastilla no cambia");
            Assert.That(HudGeometry.WallClockGap, Is.EqualTo(InfoRowFit.ClockGap), "el reloj, a 10 de la cuenta en el modo normal");
            Assert.That(InfoRowFit.FitMarginDp, Is.EqualTo(1));
        }

        [Test, Category("AC-WKT-14-15")]
        public void CasesOfTheDesignNotes()
        {
            // CAP-01: Estados a los 40,2 s, `18 spm` (un fragmento): compacto 1 (en normal sobran 0,1 y la regla pide 1 de margen),
            // con la pastilla entera junto a Próximo; sobran ≈ 6.
            var cap01 = InfoRowFit.Plan(Tv, 51.6, 108.2, Countdown, Clock, new[] { 49.3 }, 1);
            Assert.That(cap01.Mode, Is.EqualTo(InfoRowMode.Compact1));
            Assert.That(cap01.FragmentsAlone, Is.EqualTo(1));
            Assert.That(cap01.FragmentsWithUpNext, Is.EqualTo(1));
            Assert.That(Tv - InfoRowFit.Width(InfoRowMode.Compact1, 51.6, 108.2, true, Countdown, Clock, 49.3), Is.EqualTo(6.1).Within(0.15));

            // CAP-31: Texto largo a los 2,2 s, es (tres fragmentos con su relleno de 6 × 2: `>150W…` 59,8, dos 108,4, tres 173,9): no entra
            // junto a Próximo en ningún modo y el resto entra recién en compacto 2: dos fragmentos sin Próximo y ninguno con él.
            var prefixes = new[] { 59.8, 108.4, 173.9 };
            var cap31 = InfoRowFit.Plan(Tv, 99.5, 122.3, Countdown, Clock, prefixes, 3);
            Assert.That(cap31.Mode, Is.EqualTo(InfoRowMode.Compact2));
            Assert.That(cap31.FragmentsAlone, Is.EqualTo(2));
            Assert.That(cap31.FragmentsWithUpNext, Is.EqualTo(0), "CAP-19: se va con un fundido de 120 al entrar Próximo");

            // CAP-20: el mismo paso en en (`Warmup`, `Up next: Cooldown`): normal (`>150W…` pide 59,8 y quedan 44,7 con Up next).
            var cap20 = InfoRowFit.Plan(Tv, 56.9, 107.5, Countdown, Clock, prefixes, 3);
            Assert.That(cap20.Mode, Is.EqualTo(InfoRowMode.Normal));
            Assert.That(cap20.FragmentsWithUpNext, Is.EqualTo(0));
            Assert.That(cap20.FragmentsAlone, Is.EqualTo(2));

            // Prueba 4a, un trabajo en sus últimos 15 s (AUX-01 de las notas, CAP-31 de la spec): compacto 2, `>160W…` (48,1 + 12)
            // junto a Próximo: 60,1 de 60,3 (el margen más justo).
            var prueba = InfoRowFit.Plan(Tv, 51.6, 108.2, Countdown, Clock, new[] { 60.1, 110.0, 200.0 }, 3);
            Assert.That(prueba.Mode, Is.EqualTo(InfoRowMode.Compact2));
            Assert.That(prueba.FragmentsWithUpNext, Is.EqualTo(1));
            Assert.That(prueba.FragmentsAlone, Is.EqualTo(2));
        }

        [Test, Category("AC-WKT-14-15")]
        public void HorizontalProfilesShowEverything()
        {
            // TH 817,2, L640 643,4, W1280 750,0: normal, todo entero.
            foreach (var available in new[] { 817.2, 643.4, 750.0 })
            {
                var plan = InfoRowFit.Plan(available, 99.5, 122.3, Countdown, Clock, new[] { 59.8, 108.4, 173.9 }, 3);
                Assert.That(plan.Mode, Is.EqualTo(InfoRowMode.Normal));
                Assert.That(plan.FragmentsAlone, Is.EqualTo(3));
                Assert.That(plan.FragmentsWithUpNext, Is.EqualTo(3));
            }
        }

        [Test, Category("AC-WKT-14-15")]
        public void PlanHoldsWithinTheStep()
        {
            // El plan se decide al empezar el paso con Próximo si el paso lo va a tener: cuando entra Próximo no cambia el modo (nada
            // salta, DV-46); sin paso siguiente (o sin restante por tiempo) cuenta como sin Próximo.
            var before = InfoRowFit.Plan(Tv, 51.6, 108.2, Countdown, Clock, new[] { 49.3 }, 1);
            var after = InfoRowFit.Plan(Tv, 51.6, 108.2, Countdown, Clock, new[] { 49.3 }, 1);
            Assert.That(after, Is.EqualTo(before));
            var last = InfoRowFit.Plan(Tv, 51.6, 0, Countdown, Clock, new[] { 49.3 }, 1);
            Assert.That(last.Mode, Is.EqualTo(InfoRowMode.Normal), "sin Próximo entra en normal");
            Assert.That(last.FragmentsWithUpNext, Is.EqualTo(last.FragmentsAlone));
            var none = InfoRowFit.Plan(Tv, 51.6, 108.2, Countdown, Clock, null, 0);
            Assert.That(none.FragmentsAlone, Is.EqualTo(0), "sin pastilla");
            Assert.That(none.Mode, Is.EqualTo(InfoRowMode.Normal));
        }

        [Test, Category("AC-WKT-14-03")]
        public void WholeFragmentsAndWhiteEllipsis()
        {
            // DN4b-16: la pastilla se corta por fragmentos enteros (cada parte separada por " · "); la tendencia con su texto es UNO
            // (sus progresiones van juntas con ", "); nunca se corta un número (">15…" diría "más de 15").
            var plain = WorkoutHudStore.Fragments(">150W · 24 spm · 02:05/500m");
            Assert.That(plain.Count, Is.EqualTo(3));
            Assert.That(plain[0].Text, Is.EqualTo(">150W"));
            Assert.That(plain[1].Text, Is.EqualTo("24 spm"));
            Assert.That(plain[2].Text, Is.EqualTo("02:05/500m"));
            var trend = WorkoutHudStore.Fragments(">160W · 22 spm · " + TrendText.TrendEmoji + " SPM +2/rep, W +10/rep");
            Assert.That(trend.Count, Is.EqualTo(3));
            Assert.That(trend[2].IsTrend, Is.True);
            Assert.That(trend[2].Text, Is.EqualTo(TrendText.TrendEmoji + " SPM +2/rep, W +10/rep"), "la tendencia es un fragmento");
            Assert.That(trend[0].IsTrend, Is.False);
            Assert.That(WorkoutHudStore.Fragments("18 spm").Count, Is.EqualTo(1));
            Assert.That(WorkoutHudStore.Fragments("").Count, Is.EqualTo(0));
            Assert.That(StepLabelsSeparator(), Is.EqualTo(" · "), "U+00B7 con un espacio a cada lado");
        }

        [Test, Category("AC-WKT-16-06")]
        public void FreeTitleScale()
        {
            // DN4b-27: el título libre se achica (escala pareja) hasta 0,8 y solo por debajo se recorta con "…". TV y 360 en español:
            // 127,0 de lugar para `Entrenamiento Libre` (155,4) → ≈ 0,818; en inglés entra a su tamaño; el tope de 0,8.
            var others = 20 + 8 + 69.1 + 10 + 101.8;
            Assert.That(InfoRowFit.FreeTitleScale(335.9, 155.4, others), Is.EqualTo(0.818).Within(0.002));
            Assert.That(InfoRowFit.FreeTitleTruncates(335.9, 155.4, others), Is.False, "CAP-13: entero");
            Assert.That(InfoRowFit.FreeTitleScale(335.9, 104.4, 20 + 8 + 69.1 + 10 + 74.1), Is.EqualTo(1), "en: entra a su tamaño");
            Assert.That(InfoRowFit.FreeTitleScale(817.2, 155.4, others), Is.EqualTo(1), "en horizontal entra");
            Assert.That(InfoRowFit.FreeTitleScale(250, 155.4, others), Is.EqualTo(0.8), "el tope");
            Assert.That(InfoRowFit.FreeTitleTruncates(250, 155.4, others), Is.True, "por debajo de 0,8, con …");
            Assert.That(InfoRowFit.FreeTitleMinScale, Is.EqualTo(0.8));
        }

        [Test, Category("AC-WKT-16-06"), Category("AC-WKT-16-03")]
        public void FreeTitleFitsInTvWithTheChipAtItsTracking()
        {
            // QA-V4b-02: con el tracking de 0,08 em el chip vuelve a su ancho de las notas (Unity mide 101,4; fontTools, 101,8) y en TV
            // en español el título (155,4) tiene 127,1 − aire entre el título y el reloj. Con el aire de 6 de B-14 (calibrado con el chip
            // angosto, 95,3) quedaba en 0,779 y se recortaba; con el aire mínimo de 2 entra entero a ≈ 0,805 (el ícono del reloj y el
            // lado de la última letra suman ≈ 3 más a la vista).
            const double available = 335.6, title = 155.4, clock = 69.1;
            foreach (var chip in new[] { 101.4, 101.8 })
            {
                var others = 20 + 8 + InfoRowFit.FreeTitleClockGap + clock + 10 + chip;
                Assert.That(InfoRowFit.FreeTitleTruncates(available, title, others), Is.False, "entero con el chip de " + chip);
                Assert.That(InfoRowFit.FreeTitleScale(available, title, others), Is.InRange(0.8, 0.81), "achicado, con el chip de " + chip);
                Assert.That(InfoRowFit.FreeTitleTruncates(available, title, others - InfoRowFit.FreeTitleClockGap + 6), Is.True,
                    "con el aire de 6 se recortaba (chip de " + chip + ")");
            }

            Assert.That(InfoRowFit.FreeTitleClockGap, Is.EqualTo(2));
            Assert.That(InfoRowFit.FreeTitleScale(available, 104.4, 20 + 8 + InfoRowFit.FreeTitleClockGap + clock + 10 + 74.1), Is.EqualTo(1),
                "en inglés entra a su tamaño");
        }

        private static string StepLabelsSeparator()
        {
            return RowMate.Core.Models.StepLabels.PartSeparator;
        }
    }
}
