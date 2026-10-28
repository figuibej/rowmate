using System;
using System.Collections.Generic;
using NUnit.Framework;
using RowMate.App.Routines;
using RowMate.Core.Localization;
using RowMate.Core.Models;
using RowMate.Core.Routines;
using RowMate.Tests.EditMode.Localization;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Routines
{
    // SFT: el diálogo de paso sin la vista (F-RUT-13; _StepDialogState, lib/features/routines/routine_editor_screen.dart
    // :474-739): precarga, _build() y los topes de Q-92.
    [TestFixture]
    public sealed class StepDialogFormTests
    {
        // AC-RUT-13-01: nuevo = Trabajo, Por tiempo, todo vacío; sin progresiones salvo en una serie.
        [Test, Category("AC-RUT-13-01")]
        public void NewFormDefaults()
        {
            var form = StepDialogForm.ForNew(false, 1);
            Assert.That(form.IsNew, Is.True);
            Assert.That(form.Type, Is.EqualTo(StepType.Work));
            Assert.That(form.ByDistance, Is.False);
            Assert.That(form.ShowsProgressions, Is.False, "+ Paso: sin progresiones (AC-RUT-12-01)");
            foreach (var text in Texts(form))
            {
                Assert.That(text, Is.EqualTo(""));
            }

            Assert.That(Label(form.Preview()), Is.EqualTo("0s"), "la vista previa de un paso nuevo");
            var inSeries = StepDialogForm.ForNew(true, 3);
            Assert.That(inSeries.ShowsProgressions, Is.True, "+ Agregar paso a serie: con progresiones");
            Assert.That(inSeries.SeriesRepeatCount, Is.EqualTo(3));
        }

        // AC-RUT-13-03: los 4 tipos en el orden del selector, con sus nombres.
        [Test, Category("AC-RUT-13-03")]
        public void TypeSelectorOffersTheFourTypesInOrder()
        {
            Assert.That(StepTypeInfo.SelectorOrder, Is.EqualTo(new[] { StepType.Warmup, StepType.Work, StepType.Rest, StepType.Cooldown }));
            var es = new List<string>();
            var en = new List<string>();
            foreach (var type in StepTypeInfo.SelectorOrder)
            {
                es.Add(LocalizationFixture.Es.Get(StepTypeInfo.NameKey(type)));
                en.Add(LocalizationFixture.En.Get(StepTypeInfo.NameKey(type)));
            }

            Assert.That(es, Is.EqualTo(new[] { "Calentamiento", "Trabajo", "Descanso", "Enfriamiento" }));
            Assert.That(en, Is.EqualTo(new[] { "Warmup", "Work", "Rest", "Cooldown" }));
        }

        // AC-RUT-13-04: al confirmar solo cuenta el modo elegido; cambiar de modo no borra lo escrito.
        [Test, Category("AC-RUT-13-04")]
        public void OnlyTheChosenModeCounts()
        {
            var form = StepDialogForm.ForNew(false, 1);
            form.Minutes = "5";
            form.ByDistance = true;
            form.Meters = "500";
            var byDistance = Confirmed(form);
            Assert.That(byDistance.DistanceMeters, Is.EqualTo(500));
            Assert.That(byDistance.DurationSeconds, Is.Null, "sin duración");
            form.ByDistance = false;
            Assert.That(form.Minutes, Is.EqualTo("5"), "Min sigue diciendo 5");
            var byTime = Confirmed(form);
            Assert.That(byTime.DurationSeconds, Is.EqualTo(300));
            Assert.That(byTime.DistanceMeters, Is.Null, "sin distancia");
        }

        // AC-RUT-13-06 (caso 20): (Min ?? 0) × 60 + (Seg ?? 0), siempre una duración.
        [TestCase("5", "", 300, "5m", Category = "AC-RUT-13-06")]
        [TestCase("", "90", 90, "1m 30s", Category = "AC-RUT-13-06")]
        [TestCase("1", "-30", 30, "30s", Category = "AC-RUT-13-06")]
        [TestCase("abc", "45", 45, "45s", Category = "AC-RUT-13-06")]
        [TestCase("", "", 0, "0s", Category = "AC-RUT-13-06")]
        [TestCase(" 2 ", "05", 125, "2m 5s", Category = "AC-RUT-13-06")]
        public void ByTimeTable(string minutes, string seconds, int expected, string label)
        {
            var form = StepDialogForm.ForNew(false, 1);
            form.Minutes = minutes;
            form.Seconds = seconds;
            var step = Confirmed(form);
            Assert.That(step.DurationSeconds, Is.EqualTo(expected));
            Assert.That(step.DistanceMeters, Is.Null);
            Assert.That(Label(step), Is.EqualTo(label));
        }

        // AC-RUT-13-07 (caso 20): Metros o sin meta, sin duración.
        [TestCase("500", 500, "500m", Category = "AC-RUT-13-07")]
        [TestCase("0", 0, "0m", Category = "AC-RUT-13-07")]
        [TestCase("-100", -100, "-100m", Category = "AC-RUT-13-07")]
        [TestCase("", null, "?", Category = "AC-RUT-13-07")]
        [TestCase("abc", null, "?", Category = "AC-RUT-13-07")]
        public void ByDistanceTable(string meters, int? expected, string label)
        {
            var form = StepDialogForm.ForNew(false, 1);
            form.ByDistance = true;
            form.Meters = meters;
            var step = Confirmed(form);
            Assert.That(step.DistanceMeters, Is.EqualTo(expected));
            Assert.That(step.DurationSeconds, Is.Null);
            Assert.That(Label(step), Is.EqualTo(label));
        }

        // AC-RUT-13-08 (caso 20): (min ?? 0) × 60 + (seg ?? 0); ≤ 0 → sin split.
        [TestCase("2", "5", 125, Category = "AC-RUT-13-08")]
        [TestCase("0", "90", 90, Category = "AC-RUT-13-08")]
        [TestCase("1", "", 60, Category = "AC-RUT-13-08")]
        [TestCase("abc", "59", 59, Category = "AC-RUT-13-08")]
        [TestCase("0", "0", null, Category = "AC-RUT-13-08")]
        [TestCase("", "", null, Category = "AC-RUT-13-08")]
        [TestCase("-1", "30", null, Category = "AC-RUT-13-08")]
        public void SplitTable(string splitMin, string splitSec, int? expected)
        {
            var form = StepDialogForm.ForNew(false, 1);
            form.SplitMin = splitMin;
            form.SplitSec = splitSec;
            Assert.That(Confirmed(form).TargetSplitSeconds, Is.EqualTo(expected));
        }

        // AC-RUT-13-08 + QA-G3-13 a: "≤ 0 = sin split" gana sobre el tope, por largo que sea el número.
        [TestCase("-200", "", Category = "AC-RUT-13-08")]
        [TestCase("-99999999999999999999", "0", Category = "AC-RUT-13-08")]
        [TestCase("0", "-6000", Category = "AC-RUT-13-08")]
        [TestCase("-99999999999999999999", "99999999999999999999", Category = "AC-RUT-13-08")]
        public void NonPositiveSplitIsNoSplitEvenPastTheCap(string splitMin, string splitSec)
        {
            var form = StepDialogForm.ForNew(false, 1);
            form.SplitMin = splitMin;
            form.SplitSec = splitSec;
            var result = form.Confirm();
            Assert.That(result.IsValid, Is.True, "sin error");
            Assert.That(result.Step!.TargetSplitSeconds, Is.Null, "sin split");
        }

        // AC-RUT-13-09 (Q-25): sin validaciones de sentido.
        [Test, Category("AC-RUT-13-09")]
        public void NoSenseValidation()
        {
            Assert.That(Targets(Set(f => { f.WattsMin = "200"; f.WattsMax = "100"; })), Is.EqualTo("200-100W"));
            Assert.That(Targets(Set(f => f.WattsMin = "0")), Is.EqualTo(">0W"));
            Assert.That(Targets(Set(f => { f.WattsMin = "-10"; f.WattsMax = "5"; })), Is.EqualTo("-10-5W"));
            Assert.That(Targets(Set(f => f.Spm = "0")), Is.EqualTo("0 spm"));
            Assert.That(Targets(Set(f => f.Spm = "-5")), Is.EqualTo("-5 spm"));
            Assert.That(Targets(Set(f => f.WattsMax = "180")), Is.EqualTo(""), "solo vatios máx.: nada");
            var series = StepDialogForm.ForNew(true, 3);
            series.ProgressionSpm = "-1";
            Assert.That(Targets(Confirmed(series)), Is.EqualTo(StepSubtitle.TrendEmoji + " SPM -1/rep"));
            Assert.That(Label(Confirmed(StepDialogForm.ForNew(false, 1))), Is.EqualTo("0s"), "un paso de 0 s");
            var noGoal = StepDialogForm.ForNew(false, 1);
            noGoal.ByDistance = true;
            Assert.That(Label(Confirmed(noGoal)), Is.EqualTo("?"), "sin meta");
            Assert.That(Targets(Set(f => { f.WattsMin = "9999"; f.WattsMax = "-9999"; })), Is.EqualTo("9999--9999W"), "los dos topes se aceptan");
        }

        // AC-RUT-13-10: la precarga del legacy (:498-521) y lo que da Confirmar sin tocar nada.
        [Test, Category("AC-RUT-13-10")]
        public void PrefillTable()
        {
            Prefill(Step(dur: 90), false, "1", "30", "", 90);
            Prefill(Step(dur: 45), false, "0", "45", "", 45);
            Prefill(Step(dur: 0), false, "0", "0", "", 0);
            Prefill(Step(dur: 3600), false, "60", "0", "", 3600);
            Prefill(Step(dur: -90), false, "-1", "30", "", -30);
            Assert.That(Label(Step(dur: -90)), Is.EqualTo("30s"), "−90 s se ve 30s antes…");
            Assert.That(Label(Step(dur: -30)), Is.EqualTo("30s"), "… y −30 s también (paridad)");
            Prefill(Step(dur: -30), false, "0", "30", "", 30);
            Prefill(Step(dist: 500), true, "", "", "500", null);
            Prefill(Step(), false, "", "", "", 0);
            var targets = StepDialogForm.ForEdit(Step(dur: 60, split: 125, wMin: 150, wMax: 180, spm: 22), false, 1);
            Assert.That(new[] { targets.SplitMin, targets.SplitSec, targets.WattsMin, targets.WattsMax, targets.Spm },
                Is.EqualTo(new[] { "2", "5", "150", "180", "22" }));
            var confirmed = Confirmed(targets);
            Assert.That(confirmed.TargetSplitSeconds, Is.EqualTo(125));
            Assert.That(confirmed.TargetWattsMin, Is.EqualTo(150));
            Assert.That(confirmed.TargetWattsMax, Is.EqualTo(180));
            Assert.That(confirmed.TargetSpm, Is.EqualTo(22));
            var series = StepDialogForm.ForEdit(Step(dur: 60, spm: 20, pSpm: 2, pW: 0), true, 3);
            Assert.That(new[] { series.ProgressionSpm, series.ProgressionWatts, series.ProgressionSplit }, Is.EqualTo(new[] { "2", "0", "" }), "un 0 se ve 0");
            var again = Confirmed(series);
            Assert.That(again.ProgressionSpm, Is.EqualTo(2));
            Assert.That(again.ProgressionWatts, Is.EqualTo(0));
            Assert.That(again.ProgressionSplitSeconds, Is.Null);
            var both = StepDialogForm.ForEdit(Step(dur: 300, dist: 500), false, 1);
            Assert.That(both.ByDistance, Is.True, "distancia si tiene distancia");
            Assert.That(both.Minutes, Is.EqualTo("5"), "la duración también se precarga");
            Assert.That(StepDialogForm.ForEdit(Step(dur: 60), false, 1).IsNew, Is.False);
        }

        // AC-RUT-12-03: un paso suelto conserva las progresiones que trae aunque no se vean (:517-519, :551-553).
        [Test, Category("AC-RUT-12-03")]
        public void LooseStepKeepsHiddenProgressions()
        {
            var form = StepDialogForm.ForEdit(Step(dur: 300, spm: 18, pSpm: 2, pW: 5, pSplit: -1), false, 1);
            Assert.That(form.ShowsProgressions, Is.False);
            var step = Confirmed(form);
            Assert.That(step.ProgressionSpm, Is.EqualTo(2));
            Assert.That(step.ProgressionWatts, Is.EqualTo(5));
            Assert.That(step.ProgressionSplitSeconds, Is.EqualTo(-1));
        }

        // AC-RUT-12-03 + QA-G3-13 c: …y no se controlan, ni en Confirmar (el diálogo nunca marca un campo que no se ve).
        [Test, Category("AC-RUT-12-03"), Category("AC-RUT-13-12")]
        public void LooseStepHiddenProgressionsAreNotChecked()
        {
            var form = StepDialogForm.ForEdit(Step(dur: 300, spm: 18, pSpm: 5000), false, 1);
            var result = form.Confirm();
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Step!.ProgressionSpm, Is.EqualTo(5000), "se guarda tal cual (F-RARA)");
        }

        // AC-RUT-13-12: un paso suelto no se progresa: sus progresiones no se controlan por repetición.
        [Test, Category("AC-RUT-13-12")]
        public void LooseStepProgressionsAreNotChecked()
        {
            var form = StepDialogForm.ForNew(false, 1);
            form.Spm = "998";
            form.ProgressionSpm = "900";
            Assert.That(form.Confirm().IsValid, Is.True);
        }

        // AC-RUT-13-11: topes por campo (en valor absoluto) con su {max}; el error va al campo (o al par).
        [Test, Category("AC-RUT-13-11")]
        public void FieldErrorsAndMaxTexts()
        {
            Errors(f => f.WattsMin = "10000", Error(StepField.WattsMin, "9999"));
            Errors(f => f.WattsMax = "-10000", Error(StepField.WattsMax, "9999"));
            Errors(f => f.Minutes = "6000", Error(StepField.Duration, "99:59:59"));
            Errors(f => { f.Minutes = "5999"; f.Seconds = "60"; }, Error(StepField.Duration, "99:59:59"));
            Errors(f => { f.Minutes = "5999"; f.Seconds = "59"; });
            Errors(f => { f.ByDistance = true; f.Meters = "1000000"; }, Error(StepField.Meters, "999999"));
            Errors(f => f.Spm = "1000", Error(StepField.Spm, "999"));
            Errors(f => f.SplitMin = "100", Error(StepField.Split, "99:59"));
            Errors(f => { f.SplitMin = "99"; f.SplitSec = "59"; });
            Errors(f => { f.WattsMin = "10000"; f.Spm = "1000"; f.Minutes = "6000"; },
                Error(StepField.Duration, "99:59:59"), Error(StepField.WattsMin, "9999"), Error(StepField.Spm, "999"));
            var series = StepDialogForm.ForNew(true, 2);
            series.ProgressionSpm = "1000";
            series.ProgressionWatts = "10000";
            series.ProgressionSplit = "-6000";
            Assert.That(series.Confirm().Errors, Is.EqualTo(new[]
            {
                Error(StepField.ProgressionSpm, "999"), Error(StepField.ProgressionWatts, "9999"), Error(StepField.ProgressionSplit, "5999"),
            }));
            Assert.That(RoutineTexts.FieldErrorText(Error(StepField.WattsMin, "9999"), LocalizationFixture.Es), Is.EqualTo("Fuera de rango: hasta ±9999"));
            Assert.That(RoutineTexts.FieldErrorText(Error(StepField.Duration, "99:59:59"), LocalizationFixture.En),
                Is.EqualTo("Out of range: up to ±99:59:59"));
        }

        // AC-RUT-13-11 + QA-G3-13 d: los campos del modo que no se eligió no se leen ni se controlan.
        [Test, Category("AC-RUT-13-11")]
        public void HiddenModeFieldsAreNotChecked()
        {
            Errors(f => { f.Minutes = "6000"; f.ByDistance = true; f.Meters = "500"; });
            Errors(f => { f.Meters = "99999999"; f.Minutes = "5"; });
        }

        // AC-RUT-13-05 + spec §12.3: un número de cualquier largo es "fuera de rango" y nunca desborda.
        [Test, Category("AC-RUT-13-05")]
        public void HugeNumbersAreOutOfRange()
        {
            Errors(f => f.WattsMin = "99999999999999999999", Error(StepField.WattsMin, "9999"));
            Errors(f => f.Minutes = "99999999999999999999", Error(StepField.Duration, "99:59:59"));
            Errors(f => { f.Minutes = "40000000"; f.Seconds = "0"; }, Error(StepField.Duration, "99:59:59"));
            var form = StepDialogForm.ForNew(false, 1);
            form.WattsMin = "99999999999999999999";
            form.Minutes = "99999999999999999999";
            Assert.That(form.Preview().TargetWattsMin, Is.Null, "en la vista previa lo que no entra en un int se ve vacío");
            Assert.That(form.Preview().DurationSeconds, Is.Null);
        }

        // AC-RUT-13-12: el objetivo progresado en cada repetición de la serie (r = 0…N−1); el error va debajo de la progresión.
        [Test, Category("AC-RUT-13-12")]
        public void RepetitionErrors()
        {
            RepErrors(10, f => { f.Spm = "18"; f.ProgressionSpm = "200"; }, new FieldError(StepField.ProgressionSpm, 6, "999"));
            RepErrors(10, f => { f.WattsMax = "9990"; f.ProgressionWatts = "10"; }, new FieldError(StepField.ProgressionWatts, 2, "9999"));
            RepErrors(10, f => { f.Spm = "18"; f.ProgressionSpm = "100"; });
            RepErrors(10, f => { f.WattsMin = "9000"; f.WattsMax = "9990"; f.ProgressionWatts = "500"; },
                new FieldError(StepField.ProgressionWatts, 2, "9999"));
            RepErrors(10, f => { f.WattsMin = "9000"; f.WattsMax = "100"; f.ProgressionWatts = "500"; },
                new FieldError(StepField.ProgressionWatts, 3, "9999"));
            RepErrors(3064, f => { f.SplitMin = "2"; f.SplitSec = "5"; f.ProgressionSplit = "-2"; },
                new FieldError(StepField.ProgressionSplit, 3064, "99:59"));
            RepErrors(10, f => f.ProgressionSpm = "900", null);
            RepErrors(0, f => { f.Spm = "18"; f.ProgressionSpm = "900"; }, null);
            RepErrors(491, f => { f.Spm = "18"; f.ProgressionSpm = "2"; });
            RepErrors(492, f => { f.Spm = "18"; f.ProgressionSpm = "2"; }, new FieldError(StepField.ProgressionSpm, 492, "999"));
            Assert.That(RoutineTexts.FieldErrorText(new FieldError(StepField.ProgressionSpm, 6, "999"), LocalizationFixture.Es),
                Is.EqualTo("Fuera de rango en la repetición 6: hasta ±999"));
            Assert.That(RoutineTexts.FieldErrorText(new FieldError(StepField.ProgressionWatts, 2, "9999"), LocalizationFixture.En),
                Is.EqualTo("Out of range in repetition 2: up to ±9999"));
        }

        // Primero los campos y después las repeticiones: una progresión fuera de su propio tope da su error de campo, y un
        // objetivo con error no cuenta para las repeticiones.
        [Test, Category("AC-RUT-13-12")]
        public void FieldErrorsComeBeforeRepetitionErrors()
        {
            RepErrors(10, f => { f.Spm = "18"; f.ProgressionSpm = "1000"; }, new FieldError(StepField.ProgressionSpm, null, "999"));
            RepErrors(10, f => { f.Spm = "5000"; f.ProgressionSpm = "200"; }, new FieldError(StepField.Spm, null, "999"));
            RepErrors(10, f => { f.WattsMin = "10000"; f.WattsMax = "9990"; f.ProgressionWatts = "10"; },
                new FieldError(StepField.WattsMin, null, "9999"), new FieldError(StepField.ProgressionWatts, 2, "9999"));
        }

        // AC-RUT-13-16: la vista previa es exactamente la fila que queda al confirmar.
        [Test, Category("AC-RUT-13-16")]
        public void PreviewIsTheConfirmedRow()
        {
            var forms = new List<StepDialogForm>();
            var a = StepDialogForm.ForNew(false, 1);
            a.Minutes = "5";
            a.Spm = "18";
            forms.Add(a);
            var b = StepDialogForm.ForEdit(RoutineSeeds.EscaleraSpm().Steps[1], true, 3);
            forms.Add(b);
            var c = StepDialogForm.ForNew(true, 4);
            c.Type = StepType.Cooldown;
            c.ByDistance = true;
            c.Meters = "250";
            c.SplitMin = "2";
            c.SplitSec = "5";
            c.ProgressionSplit = "-2";
            forms.Add(c);
            foreach (var form in forms)
            {
                Assert.That(form.Preview(), Is.EqualTo(Confirmed(form)), "misma fila");
            }

            Assert.That(Label(a.Preview()), Is.EqualTo("5m"));
            a.Minutes = "";
            Assert.That(Label(a.Preview()), Is.EqualTo("0s"), "cambia con cada tecla");
            Assert.That(b.Preview().Type, Is.EqualTo(StepType.Work));
            Assert.That(StepSubtitle.For(b.Preview(), LocalizationFixture.Es).Text, Is.EqualTo("5m  ·  18 spm · " + StepSubtitle.TrendEmoji + " SPM +2/rep"));
        }

        private static IntervalStep Step(int? dur = null, int? dist = null, int? wMin = null, int? wMax = null, int? spm = null, int? split = null,
            int? pSpm = null, int? pW = null, int? pSplit = null)
        {
            return RoutineSeeds.Step(StepType.Work, dur, dist, wMin, wMax, spm, split, null, null, pSpm, pW, pSplit);
        }

        private static void Prefill(IntervalStep step, bool byDistance, string minutes, string seconds, string meters, int? confirmedDuration)
        {
            var form = StepDialogForm.ForEdit(step, false, 1);
            Assert.That(form.ByDistance, Is.EqualTo(byDistance), Label(step));
            Assert.That(form.Minutes, Is.EqualTo(minutes), Label(step) + ": Min");
            Assert.That(form.Seconds, Is.EqualTo(seconds), Label(step) + ": Seg");
            Assert.That(form.Meters, Is.EqualTo(meters), Label(step) + ": Metros");
            var confirmed = Confirmed(form);
            Assert.That(confirmed.DurationSeconds, Is.EqualTo(confirmedDuration), Label(step) + ": confirmar sin tocar nada");
            if (byDistance)
            {
                Assert.That(confirmed.DistanceMeters, Is.EqualTo(step.DistanceMeters));
            }
        }

        private static IntervalStep Set(Action<StepDialogForm> edit)
        {
            var form = StepDialogForm.ForNew(false, 1);
            form.Minutes = "1";
            edit(form);
            return Confirmed(form);
        }

        private static void Errors(Action<StepDialogForm> edit, params FieldError[] expected)
        {
            var form = StepDialogForm.ForNew(false, 1);
            edit(form);
            var result = form.Confirm();
            Assert.That(result.Errors, Is.EqualTo(expected));
            Assert.That(result.IsValid, Is.EqualTo(expected.Length == 0));
            if (expected.Length > 0)
            {
                Assert.That(result.Step, Is.Null, "el diálogo queda abierto");
            }
        }

        private static void RepErrors(int repeatCount, Action<StepDialogForm> edit, params FieldError[]? expected)
        {
            var form = StepDialogForm.ForNew(true, repeatCount);
            edit(form);
            Assert.That(form.Confirm().Errors, Is.EqualTo(expected ?? new FieldError[0]));
        }

        private static FieldError Error(StepField field, string max)
        {
            return new FieldError(field, null, max);
        }

        private static IntervalStep Confirmed(StepDialogForm form)
        {
            var result = form.Confirm();
            Assert.That(result.IsValid, Is.True, "errores: " + string.Join(", ", result.Errors));
            return result.Step!;
        }

        private static string Label(IntervalStep step)
        {
            return StepLabels.Duration(step, LocalizationFixture.Es);
        }

        private static string Targets(IntervalStep step)
        {
            return StepLabels.Targets(step, LocalizationFixture.Es);
        }

        private static IEnumerable<string> Texts(StepDialogForm form)
        {
            return new[]
            {
                form.Minutes, form.Seconds, form.Meters, form.WattsMin, form.WattsMax, form.Spm, form.SplitMin, form.SplitSec,
                form.ProgressionSpm, form.ProgressionWatts, form.ProgressionSplit,
            };
        }
    }
}
