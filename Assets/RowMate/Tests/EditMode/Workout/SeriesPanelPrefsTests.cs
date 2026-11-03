using NUnit.Framework;
using RowMate.App.Composition;
using RowMate.App.Device;
using RowMate.App.Shell;
using RowMate.App.Workout;
using RowMate.Core.Common;
using RowMate.Core.Localization;
using RowMate.Core.Models;
using RowMate.Core.Workout;
using RowMate.Tests.EditMode.Localization;
using RowMate.Tests.Support;
using AppContext = RowMate.App.Composition.AppContext;

namespace RowMate.Tests.EditMode.Workout
{
    // SPP: los paneles con las preferencias (05-series-parciales/02-tech-design §2.7; AC-WKT-18-10, -11): cerrados por defecto,
    // con la clave en verdadero abiertos desde que se ven, el toque antes de la lectura gana, cada toque guarda en el acto, el de
    // repeticiones conserva su estado aunque no se vea, el guardado que falla no cambia lo que ve el usuario, y la parte de reglas
    // de LT-59 y LT-60 con el tracker armado como el legacy (test/series_panels_test.dart:12-22: una sola lectura de 300 m a los
    // 60 s, que suma). La parte montada (los textos a la vista, el toque por la selección real) es SPV (PlayMode).
    // legacy: lib/features/workout/series_panels.dart:113-156.
    [TestFixture]
    public sealed class SeriesPanelPrefsTests
    {
        // legacy: test/series_panels_test.dart:39 'arrancan colapsados y al tocar se expanden y se recuerda'
        [TestCase(TestName = "arrancan colapsados y al tocar se expanden y se recuerda",
            Category = "LT-59,AC-WKT-18-10,AC-WKT-18-09,AC-WKT-18-06,AC-WKT-18-07")]
        public void StartCollapsedTapExpandsAndRemembers()
        {
            var l10n = LocalizationFixture.Es;
            var series = TrackerAt300m("g");
            var preferences = new RecordingPreferences();
            using (var panels = new SeriesPanelPrefs(preferences))
            {
                // find.text('Parciales 500 m') y ('Series anteriores'): nada; find.text('500 m') y ('Reps'): uno cada uno.
                Assert.That(panels.State.Value.Laps, Is.False);
                Assert.That(panels.State.Value.Reps, Is.False);
                Assert.That(series!.InSeries, Is.True, "con serie se ve el chip Reps");
                Assert.That(l10n.Get(L10nKeys.HudLapsChip), Is.EqualTo("500 m"));
                Assert.That(l10n.Get(L10nKeys.HudRepsChip), Is.EqualTo("Reps"));

                // tap(find.text('500 m')): Parciales 500 m y Rep 1/3 · 300 m; prefs.getBool(lapsKey) en verdadero.
                panels.Toggle(SeriesPanelKind.Laps);
                Assert.That(panels.State.Value.Laps, Is.True);
                Assert.That(l10n.Get(L10nKeys.HudLapsTitle), Is.EqualTo("Parciales 500 m"));
                Assert.That(series.RepProgress, Is.EqualTo("Rep 1/3 · 300 m"));
                Assert.That(preferences.TryGetBool(SeriesPanelPrefs.LapsKey, out var laps) && laps, Is.True);
                Assert.That(preferences.Writes, Is.EqualTo(new[] { "set immersive.lapsExpanded=true" }), "guarda en el acto");

                // tap(find.text('Reps')): Sin repeticiones completas.
                panels.Toggle(SeriesPanelKind.Reps);
                Assert.That(panels.State.Value.Reps, Is.True);
                Assert.That(series.Reps, Is.Empty);
                Assert.That(l10n.Get(L10nKeys.HudRepsEmpty), Is.EqualTo("Sin repeticiones completas"));
                Assert.That(preferences.WriteCount, Is.EqualTo(2));
            }
        }

        // legacy: test/series_panels_test.dart:62 'respeta la preferencia guardada'
        [TestCase(TestName = "respeta la preferencia guardada", Category = "LT-60,AC-WKT-18-10")]
        public void RespectsTheSavedPreference()
        {
            var preferences = new RecordingPreferences(false, RecordingPreferences.Seed(SeriesPanelPrefs.LapsKey, true));
            using (var panels = new SeriesPanelPrefs(preferences))
            {
                Assert.That(panels.State.Value.Laps, Is.True, "abierto desde el primer estado, sin pasar por cerrado");
                Assert.That(panels.State.Value.Reps, Is.False);
                Assert.That(preferences.WriteCount, Is.EqualTo(0), "leer no escribe");
            }
        }

        [Test, Category("AC-WKT-18-10")]
        public void TheKeysAreTheLegacyOnes()
        {
            Assert.That(SeriesPanelPrefs.LapsKey, Is.EqualTo("immersive.lapsExpanded"));
            Assert.That(SeriesPanelPrefs.RepsKey, Is.EqualTo("immersive.repsExpanded"));
        }

        [Test, Category("AC-WKT-18-10")]
        public void ATapBeforeTheReadWins()
        {
            // :130-132, :144-149: si el usuario tocó antes de que termine la lectura, su elección gana; el que no tocó, la lectura.
            var preferences = new RecordingPreferences(true, RecordingPreferences.Seed(SeriesPanelPrefs.LapsKey, false),
                RecordingPreferences.Seed(SeriesPanelPrefs.RepsKey, true));
            using (var panels = new SeriesPanelPrefs(preferences))
            {
                var states = new StateRecorder<SeriesPanelsExpanded>(panels.State);
                Assert.That(panels.State.Value, Is.EqualTo(new SeriesPanelsExpanded(false, false)), "sin leer: cerrados");
                panels.Toggle(SeriesPanelKind.Laps);
                Assert.That(panels.State.Value.Laps, Is.True);
                preferences.CompleteLoad();
                Assert.That(panels.State.Value.Laps, Is.True, "lo que eligió el usuario gana a lo leído (false)");
                Assert.That(panels.State.Value.Reps, Is.True, "el que no tocó, lo leído");
                Assert.That(states.Count, Is.GreaterThanOrEqualTo(2));
                states.Dispose();
            }
        }

        [Test, Category("AC-WKT-18-10")]
        public void TheRepsPanelKeepsItsStateWhileHidden()
        {
            // El de repeticiones conserva su estado aunque no se vea (vuelve con la serie siguiente en el estado que tenía): el
            // estado es de la app, no del panel a la vista.
            var preferences = new MemoryPreferences();
            using (var panels = new SeriesPanelPrefs(preferences))
            {
                panels.Toggle(SeriesPanelKind.Reps);
                Assert.That(panels.State.Value.Reps, Is.True);
                Assert.That(panels.State.Value.Laps, Is.False);
            }

            using (var again = new SeriesPanelPrefs(preferences))
            {
                Assert.That(again.State.Value.Reps, Is.True, "la sesión siguiente, igual");
            }
        }

        [Test, Category("AC-WKT-18-11")]
        public void AFailedSaveKeepsWhatTheUserTouchedForThisRun()
        {
            // Si guardar falla, el panel cambia igual y no hay aviso; las sesiones siguientes de la misma ejecución abren como lo dejó
            // el usuario; después de reiniciar, como quedó guardado (AC-WKT-18-11; QA-G5P-07).
            var preferences = new RecordingPreferences { FailWrites = true };
            using (var panels = new SeriesPanelPrefs(preferences))
            {
                panels.Toggle(SeriesPanelKind.Laps);
                Assert.That(panels.State.Value.Laps, Is.True);
            }

            using (var sameRun = new SeriesPanelPrefs(preferences))
            {
                Assert.That(sameRun.State.Value.Laps, Is.True, "la misma ejecución");
            }

            Assert.That(preferences.Disk.ContainsKey(SeriesPanelPrefs.LapsKey), Is.False, "después de reiniciar, lo último guardado (nada)");
            Assert.That(preferences.Writes, Is.EqualTo(new[] { "set immersive.lapsExpanded=true!" }));
        }

        [Test, Category("AC-WKT-18-11")]
        public void AKeyOfAnotherTypeIsClosed()
        {
            // Con una clave de otro tipo (immersive.lapsExpanded = "sí"), cerrado sin aviso; el otro se lee.
            var preferences = new RecordingPreferences(false, RecordingPreferences.Seed(SeriesPanelPrefs.LapsKey, "sí"),
                RecordingPreferences.Seed(SeriesPanelPrefs.RepsKey, true));
            using (var panels = new SeriesPanelPrefs(preferences))
            {
                Assert.That(panels.State.Value.Laps, Is.False);
                Assert.That(panels.State.Value.Reps, Is.True);
                panels.Toggle(SeriesPanelKind.Laps);
                Assert.That(preferences.TryGetBool(SeriesPanelPrefs.LapsKey, out var laps) && laps, Is.True, "el toque la reemplaza por un booleano");
            }
        }

        [Test, Category("AC-WKT-18-11"), Category("AC-HIS-20-04")]
        public void TheLocalExpansionNeverWrites()
        {
            // QA-G5T-05: el precalentado arma los paneles abiertos con un LocalSeriesPanelsExpansion propio: no pasa por las
            // preferencias de la app ni se suscribe a su State.
            var preferences = new RecordingPreferences();
            using (var app = new SeriesPanelPrefs(preferences))
            {
                var subscribers = app.SubscriberCount;
                var local = new LocalSeriesPanelsExpansion(true, true);
                Assert.That(local.State.Value, Is.EqualTo(new SeriesPanelsExpanded(true, true)));
                local.Toggle(SeriesPanelKind.Laps);
                local.Toggle(SeriesPanelKind.Reps);
                Assert.That(local.State.Value, Is.EqualTo(new SeriesPanelsExpanded(false, false)));
                Assert.That(preferences.WriteCount, Is.EqualTo(0));
                Assert.That(app.SubscriberCount, Is.EqualTo(subscribers));
                Assert.That(app.State.Value, Is.EqualTo(new SeriesPanelsExpanded(false, false)));
            }
        }

        [Test, Category("AC-WKT-18-10"), Category("AC-HIS-20-04")]
        public void TheAppHasOnePanelsStateOverItsPreferences()
        {
            // §2.8: AppContext.SeriesPanels, uno por app, sobre AppServices.Preferences (en memoria si no vino ninguna: nunca el
            // archivo del usuario); AppContext.Dispose lo dispone.
            var logger = new RecordingLogger();
            var scheduler = new FakeScheduler();
            var backend = new DeviceBackend(new ControllableRowerService(logger), new ScriptedHeartRateService(logger), null);
            var preferences = new RecordingPreferences();
            var services = new AppServices(backend, new InMemoryRoutineRepository(RoutineSeeds.Lista.Routines), new InMemorySessionRepository(), scheduler,
                logger, LocalizationFixture.Set, new FakeSystemLocale("es-AR"), new FakeAccessibilitySettings(1, false), new ShellOptions(false))
            {
                Preferences = preferences,
            };
            AppContext app = AppComposer.Compose(services);
            Assert.That(app.Preferences, Is.SameAs(preferences));
            app.SeriesPanels.Toggle(SeriesPanelKind.Laps);
            Assert.That(preferences.Writes, Is.EqualTo(new[] { "set immersive.lapsExpanded=true" }));
            app.Dispose();

            var memory = AppComposer.Compose(services with { Preferences = null });
            Assert.That(memory.Preferences, Is.InstanceOf<MemoryPreferences>());
            Assert.That(memory.SeriesPanels.State.Value, Is.EqualTo(new SeriesPanelsExpanded(false, false)));
            memory.Dispose();
        }

        // El tracker de test/series_panels_test.dart:12-22, por la composición del store (QA-G5P-05).
        private static HudSeries? TrackerAt300m(string? group)
        {
            var t = new SeriesTracker();
            t.Begin(0, 0);
            var position = new StepPosition(group, 1, group == null ? 1 : 3);
            var step = group == null ? RoutineSeeds.Lt59LooseWork : RoutineSeeds.Lt59Work;
            t.Update(0, step, position, 300, 60);
            return WorkoutHudStore.ComposeSeries(t.State, new[] { step }, new[] { position }, LocalizationFixture.Es, 1);
        }
    }
}
