using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using RowMate.App.Composition;
using RowMate.App.History;
using RowMate.App.Shell;
using RowMate.Core.Common;
using RowMate.Core.Models;
using RowMate.Core.Workout;
using RowMate.Tests.EditMode.Localization;
using RowMate.Tests.Support;
using AppContext = RowMate.App.Composition.AppContext;

namespace RowMate.Tests.EditMode.History
{
    // HS · La lista del historial (F-HIS-06; 02-tech-design §3.1, DT9-02 a -06): carga al arrancar, los 4 disparadores, la
    // coalescencia, sin emitir si es igual, las fallas con y sin lista, Reintentar y Dispose.
    [TestFixture]
    public sealed class HistoryStoreTests
    {
        private FakeScheduler _scheduler = null!;
        private RecordingLogger _logger = null!;
        private ScriptedSessionRepository _sessions = null!;
        private ShellStore _shell = null!;
        private ObservableValue<WorkoutSnapshot> _workout = null!;

        [SetUp]
        public void SetUp()
        {
            _scheduler = new FakeScheduler();
            _logger = new RecordingLogger();
            _sessions = new ScriptedSessionRepository(_scheduler);
            _shell = new ShellStore(new ShellOptions(false), _logger);
            _workout = new ObservableValue<WorkoutSnapshot>(WorkoutSnapshot.Initial, _logger);
        }

        private HistoryStore Store()
        {
            return new HistoryStore(_sessions, _shell, _workout, _logger);
        }

        private long Add(int day)
        {
            return HistorySeeds.AddFinished(_sessions, HistorySeeds.Local(2026, 11, day, 10, 0));
        }

        [Test, Category("AC-HIS-06-01")]
        public void LoadsOnStart()
        {
            var memory = new InMemorySessionRepository();
            HistorySeeds.AddFinished(memory, HistorySeeds.Local(2026, 11, 1, 10, 0));
            var app = AppComposer.Compose(new AppServices(new DeviceBackend(new ControllableRowerService(_logger), new ScriptedHeartRateService(_logger), null),
                new InMemoryRoutineRepository(), memory, _scheduler, _logger, LocalizationFixture.Set, new FakeSystemLocale("es-AR"),
                new FakeAccessibilitySettings(1.0, reduceMotion: false), new ShellOptions(false)));
            Assert.That(app.History.State.Value.Phase, Is.EqualTo(HistoryPhase.Loading), "armar no carga nada");
            app.Start();
            Assert.That(app.History.State.Value.Phase, Is.EqualTo(HistoryPhase.Ready), "la carga del arranque (main.dart:84)");
            Assert.That(app.History.State.Value.Entries.Count, Is.EqualTo(1));
            using (var detail = app.CreateSessionDetail(app.History.State.Value.Entries[0]))
            {
                detail.LoadAsync();
                Assert.That(detail.State.Value.Phase, Is.EqualTo(SessionDetailPhase.Ready));
            }

            app.Dispose();
        }

        [Test, Category("AC-HIS-06-02")]
        public void ReloadTriggers()
        {
            using (var store = Store())
            {
                store.LoadAsync();
                Assert.That(_sessions.HistoryCalls, Is.EqualTo(1));
                _shell.Tap(ShellStore.HistoryIndex);
                _shell.Tap(ShellStore.HistoryIndex);
                Assert.That(_sessions.HistoryCalls, Is.EqualTo(3), "cada toque, también con la pestaña ya elegida");
                _shell.Tap(1);
                _shell.Select(ShellStore.HistoryIndex);
                Assert.That(_sessions.HistoryCalls, Is.EqualTo(3), "las otras pestañas y Select no recargan");

                _workout.Value = WorkoutSnapshot.Initial with { Revision = 1, Phase = WorkoutPhase.Finished, Close = CloseStatus.Pending };
                Assert.That(_sessions.HistoryCalls, Is.EqualTo(3), "con el cierre en vuelo todavía no");
                _workout.Value = WorkoutSnapshot.Initial with { Revision = 2, Phase = WorkoutPhase.Finished, Close = CloseStatus.Saved };
                Assert.That(_sessions.HistoryCalls, Is.EqualTo(4), "el cierre volvió: la sesión nueva ya está");
                _workout.Value = WorkoutSnapshot.Initial with { Revision = 3, Phase = WorkoutPhase.Finished, Close = CloseStatus.Saved };
                Assert.That(_sessions.HistoryCalls, Is.EqualTo(4), "una foto más con el mismo cierre no recarga");
                _workout.Value = WorkoutSnapshot.Initial with { Revision = 4 };
                Assert.That(_sessions.HistoryCalls, Is.EqualTo(5), "volver a reposo (Terminar del diálogo de completado) recarga otra vez");
                _workout.Value = WorkoutSnapshot.Initial with { Revision = 5, Phase = WorkoutPhase.Finished, Close = CloseStatus.Failed };
                Assert.That(_sessions.HistoryCalls, Is.EqualTo(6), "un cierre fallado deja la sesión cortada, que también se lista (DT9-05)");

                // Terminar a mano (WP-B): el motor vuelve a reposo en cuanto el cierre vuelve, sin publicar Saved.
                _workout.Value = WorkoutSnapshot.Initial with { Revision = 6 };
                Assert.That(_sessions.HistoryCalls, Is.EqualTo(7));
                _workout.Value = WorkoutSnapshot.Initial with { Revision = 7, Phase = WorkoutPhase.Finished, Close = CloseStatus.Pending };
                Assert.That(_sessions.HistoryCalls, Is.EqualTo(7), "con el cierre en vuelo todavía no");
                _workout.Value = WorkoutSnapshot.Initial with { Revision = 8 };
                Assert.That(_sessions.HistoryCalls, Is.EqualTo(8), "Terminar a mano: el cierre volvió y el motor quedó en reposo");
                _workout.Value = WorkoutSnapshot.Initial with { Revision = 9 };
                Assert.That(_sessions.HistoryCalls, Is.EqualTo(8), "otra foto en reposo no recarga");
            }
        }

        [Test, Category("AC-HIS-06-02")]
        public void NewSessionIsOnTopAfterTheReload()
        {
            Add(1);
            using (var store = Store())
            {
                store.LoadAsync();
                var id = Add(2);
                _shell.Tap(ShellStore.HistoryIndex);
                Assert.That(store.State.Value.Entries[0].Session.Id, Is.EqualTo(id));
                Assert.That(store.State.Value.Entries.Count, Is.EqualTo(2));
            }
        }

        [Test, Category("AC-HIS-06-03")]
        public void CoalescesToOneInFlightAndOnePending()
        {
            Add(1);
            _sessions.HistoryDelay = TimeSpan.FromSeconds(1);
            using (var store = Store())
            {
                var first = store.LoadAsync();
                store.LoadAsync();
                var id = Add(2);
                var last = store.LoadAsync();
                Assert.That(_sessions.HistoryCalls, Is.EqualTo(1), "una en vuelo");
                Assert.That(last, Is.SameAs(first));
                _scheduler.Advance(TimeSpan.FromSeconds(1));
                Assert.That(_sessions.HistoryCalls, Is.EqualTo(2), "y UNA pendiente");
                _scheduler.Advance(TimeSpan.FromSeconds(1));
                Assert.That(last.IsCompleted, Is.True);
                Assert.That(_sessions.HistoryCalls, Is.EqualTo(2));
                Assert.That(store.State.Value.Entries[0].Session.Id, Is.EqualTo(id), "la última lectura es posterior al último pedido");
                Assert.That(store.Loads, Is.EqualTo(2));
            }
        }

        [Test, Category("AC-HIS-06-03"), Category("AC-D15-07")]
        public void EqualListDoesNotEmitAndTheSpinnerIsOnlyForTheFirstLoad()
        {
            Add(1);
            Add(2);
            _sessions.HistoryDelay = TimeSpan.FromSeconds(1);
            using (var store = Store())
            {
                var phases = new List<HistoryPhase>();
                using (store.State.Subscribe(s => phases.Add(s.Phase), emitCurrent: false))
                {
                    store.LoadAsync();
                    _scheduler.Advance(TimeSpan.FromSeconds(1));
                    Assert.That(phases, Is.EqualTo(new[] { HistoryPhase.Ready }));
                    var before = store.State.Value;
                    _shell.Tap(ShellStore.HistoryIndex);
                    Assert.That(store.State.Value.Phase, Is.EqualTo(HistoryPhase.Ready), "con lista, la recarga no muestra el indicador");
                    _scheduler.Advance(TimeSpan.FromSeconds(1));
                    Assert.That(phases.Count, Is.EqualTo(1), "la misma lista (por valor) no emite (gate m-01)");
                    Assert.That(store.State.Value, Is.SameAs(before), "la lista no se toca (Q-22)");
                    Add(3);
                    _shell.Tap(ShellStore.HistoryIndex);
                    _scheduler.Advance(TimeSpan.FromSeconds(1));
                    Assert.That(phases.Count, Is.EqualTo(2));
                    Assert.That(phases.Distinct(), Is.EqualTo(new[] { HistoryPhase.Ready }), "nunca Loading después de la primera");
                }
            }
        }

        [Test, Category("AC-HIS-06-04")]
        public void Failures()
        {
            _sessions.HistoryFailures = 1;
            using (var store = Store())
            {
                var reloadFailed = 0;
                store.ReloadFailed += () => reloadFailed++;
                store.LoadAsync();
                Assert.That(store.State.Value.Phase, Is.EqualTo(HistoryPhase.LoadFailed), "no queda en el indicador (DV-95)");
                Assert.That(store.State.Value.Entries, Is.Empty);
                Assert.That(_logger.Lines.Last(), Does.StartWith("[DB] Warning: carga del historial: "));
                Assert.That(reloadFailed, Is.EqualTo(0), "sin lista, la pantalla de error (no el aviso)");

                Add(1);
                store.LoadAsync(); // Reintentar
                Assert.That(store.State.Value.Phase, Is.EqualTo(HistoryPhase.Ready));
                Assert.That(store.State.Value.Entries.Count, Is.EqualTo(1));

                var list = store.State.Value;
                _sessions.HistoryFailures = 1;
                _shell.Tap(ShellStore.HistoryIndex);
                Assert.That(store.State.Value, Is.SameAs(list), "con lista, queda como estaba");
                Assert.That(reloadFailed, Is.EqualTo(1), "y el aviso de 4 s (DT9-06)");
                Assert.That(_logger.Levels.Last(), Is.EqualTo(LogLevel.Warning));
            }
        }

        [Test, Category("AC-HIS-06-04")]
        public void RetryShowsLoadingAgain()
        {
            _sessions.HistoryFailures = 1;
            _sessions.HistoryDelay = TimeSpan.FromSeconds(1);
            using (var store = Store())
            {
                store.LoadAsync();
                _scheduler.Advance(TimeSpan.FromSeconds(1));
                Assert.That(store.State.Value.Phase, Is.EqualTo(HistoryPhase.LoadFailed));
                store.LoadAsync();
                Assert.That(store.State.Value.Phase, Is.EqualTo(HistoryPhase.Loading), "Reintentar sin lista vuelve al indicador");
                _scheduler.Advance(TimeSpan.FromSeconds(1));
                Assert.That(store.State.Value.Phase, Is.EqualTo(HistoryPhase.Ready));
            }
        }

        [Test, Category("AC-HIS-06-02")]
        public void AfterDisposeNothingChanges()
        {
            _sessions.HistoryDelay = TimeSpan.FromSeconds(1);
            var store = Store();
            store.LoadAsync();
            store.Dispose();
            _scheduler.Advance(TimeSpan.FromSeconds(1));
            Assert.That(store.State.Value.Phase, Is.EqualTo(HistoryPhase.Loading), "lo que vuelve tarde se ignora");
            _shell.Tap(ShellStore.HistoryIndex);
            _workout.Value = WorkoutSnapshot.Initial with { Revision = 1, Phase = WorkoutPhase.Finished, Close = CloseStatus.Saved };
            Assert.That(_sessions.HistoryCalls, Is.EqualTo(1), "ya no escucha la barra ni el motor");
            Assert.That(store.LoadAsync().IsCompleted, Is.True);
        }

        [Test, Category("AC-HIS-06-03")]
        public void StateEqualityIsByValue()
        {
            var session = new WorkoutSession(HistorySeeds.Local(2026, 11, 1, 10, 0)) { Id = 1 };
            var a = new HistoryState(HistoryPhase.Ready, new[] { new HistoryEntry(session, SessionStats.Empty) });
            var b = new HistoryState(HistoryPhase.Ready, new List<HistoryEntry> { new HistoryEntry(session with { }, SessionStats.Empty with { }) });
            Assert.That(a, Is.EqualTo(b));
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
            Assert.That(a, Is.Not.EqualTo(b with { Phase = HistoryPhase.Loading }));
            Assert.That(a, Is.Not.EqualTo(new HistoryState(HistoryPhase.Ready, new[] { new HistoryEntry(session with { Id = 2 }, SessionStats.Empty) })));
        }
    }
}
