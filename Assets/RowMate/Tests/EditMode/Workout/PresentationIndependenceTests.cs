using System;
using System.Collections.Generic;
using NUnit.Framework;
using RowMate.App.Workout;
using RowMate.Core.Common;
using RowMate.Core.Localization;
using RowMate.Core.Models;
using RowMate.Core.Text;
using RowMate.Core.Workout;
using RowMate.Tests.EditMode.Localization;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Workout
{
    // PIN: la animación nunca adelanta, demora ni frena el reloj ni las métricas (ANIMATION_BIBLE §0.1; AC-WKT-13-14; 02-tech-design
    // §3.0 de la It. 4b). Con y sin el store del HUD (y su reloj de pared, que tiene su propio periódico), el motor da la misma
    // secuencia: fase, reloj, paso, restante, puntos con sus valores, resultados de los objetivos e instante del fin.
    [TestFixture]
    public sealed class PresentationIndependenceTests
    {
        [TestCase("Estados", Category = "AC-WKT-13-14")]
        [TestCase("Corta", Category = "AC-WKT-13-14")]
        [TestCase("Prueba 4a", Category = "AC-WKT-13-14")]
        public void EngineSequenceIsTheSameWithAndWithoutTheHud(string routine)
        {
            var without = Run(routine, false);
            var with = Run(routine, true);
            Assert.That(with.Count, Is.EqualTo(without.Count));
            Assert.That(string.Join("\n", with), Is.EqualTo(string.Join("\n", without)));
        }

        private static List<string> Run(string name, bool withHud)
        {
            var template = name == "Estados" ? RoutineSeeds.Estados() : name == "Corta" ? RoutineSeeds.Corta()
                : name == "Series 600" ? RoutineSeeds.SeriesSeiscientos() : RoutineSeeds.PruebaCuatroA();
            var end = name == "Series 600" ? 600 : 250;
            var lines = new List<string>();
            using (var h = new WorkoutHarness())
            {
                WorkoutHudStore? hud = null;
                SeriesPanelPrefs? panels = null;
                IDisposable? toggles = null;
                if (withHud)
                {
                    var texts = new ReadOnlyObservable<Localizer>(new ObservableValue<Localizer>(LocalizationFixture.Es, NullAppLogger.Instance));
                    hud = new WorkoutHudStore(h.Engine, texts, h.Scheduler, h.Lifecycle, h.Logger);
                    // It. 5: los paneles se abren y se cierran cada 3 s (por las preferencias, como un toque).
                    panels = new SeriesPanelPrefs(new MemoryPreferences());
                    var p = panels;
                    var n = 0;
                    toggles = h.Scheduler.SchedulePeriodic(TimeSpan.FromSeconds(3), () => p.Toggle(n++ % 2 == 0 ? SeriesPanelKind.Laps : SeriesPanelKind.Reps));
                }

                using (h.Engine.State.Subscribe(s => lines.Add(Describe(h.Now, s, h.Engine)), false))
                {
                    h.Routine(template);
                    h.At(end);
                }

                toggles?.Dispose();
                panels?.Dispose();

                lines.Add("puntos " + WorkoutHarness.Describe(h.Points()));
                var session = h.Session()!;
                lines.Add("fin " + (session.FinishedAt.HasValue ? session.FinishedAt.Value.ToUnixTimeMilliseconds().ToString(System.Globalization.CultureInfo.InvariantCulture) : "-"));
                for (var i = 0; i < 8; i++)
                {
                    var result = h.Engine.Targets.ResultOf(i);
                    lines.Add("objetivo " + Fmt.Integer(i) + " " + (result.HasValue ? result.Value.ToString() : "-"));
                }

                hud?.Dispose();
            }

            return lines;
        }

        private static string Describe(double now, WorkoutSnapshot s, WorkoutEngine engine)
        {
            var step = s.Step;
            var series = s.Series;
            var tracker = series == null ? "-" : Fmt.Integer(series.Revision) + ":" + Fmt.Integer(series.CurrentLapMeters) + ":" + Fmt.Integer(series.Laps.Count) + ":"
                + Fmt.Integer(series.RepMeters) + ":" + Fmt.Integer(series.PreviousReps.Count);
            return Fmt.Fixed(now, 3) + " " + s.Phase + " " + Fmt.Integer(s.TotalElapsedSeconds) + " "
                + (step.HasValue ? Fmt.Integer(step.Value.StepIndex) + "/" + (step.Value.RemainingInStep.HasValue ? Fmt.Integer(step.Value.RemainingInStep.Value) : "-") : "-")
                + " " + Fmt.Integer(s.Data.DistanceMeters) + " " + (s.EndedBy.HasValue ? s.EndedBy.Value.ToString() : "-") + " " + s.Close + " " + tracker;
        }
    }
}
