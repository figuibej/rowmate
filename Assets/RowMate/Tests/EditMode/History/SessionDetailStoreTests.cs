using System;
using System.Linq;
using NUnit.Framework;
using RowMate.App.History;
using RowMate.Core.History;
using RowMate.Core.Models;
using RowMate.Tests.EditMode.Localization;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.History
{
    // SD · El detalle de una sesión (F-HIS-09; 02-tech-design §3.2; sd:35-54): relee por id o usa la de la tarjeta, todos los
    // puntos, la rutina ACTUAL (editada o borrada), NoTelemetry, LoadFailed con Reintentar y Vatios por defecto.
    [TestFixture]
    public sealed class SessionDetailStoreTests
    {
        private FakeScheduler _scheduler = null!;
        private RecordingLogger _logger = null!;
        private ScriptedSessionRepository _sessions = null!;
        private InMemoryRoutineRepository _routines = null!;
        private HistorySeedResult _seed = null!;

        [SetUp]
        public void SetUp()
        {
            _scheduler = new FakeScheduler();
            _logger = new RecordingLogger();
            _sessions = new ScriptedSessionRepository(_scheduler);
            _routines = new InMemoryRoutineRepository();
            _seed = HistorySeeds.Apply(_routines, _sessions, HistorySeeds.F9);
        }

        private HistoryEntry Entry(int number)
        {
            return _sessions.Store.History().Single(e => e.Session.Id == _seed.Session(number));
        }

        private SessionDetailState Load(HistoryEntry entry)
        {
            using (var store = new SessionDetailStore(entry, _sessions, _routines, _logger))
            {
                store.LoadAsync();
                return store.State.Value;
            }
        }

        [Test, Category("AC-HIS-09-02")]
        public void RereadsById()
        {
            var entry = Entry(3);
            var stale = entry with { Session = entry.Session with { RoutineName = "Nombre viejo" } };
            var state = Load(stale);
            Assert.That(state.Session.RoutineName, Is.EqualTo("Escalera"), "la sesión de la base, no la de la tarjeta");
            Assert.That(state.Chart!.Count, Is.EqualTo(36), "todos sus puntos");
            Assert.That(state.Chart.X, Is.Ordered.Ascending, "por el tiempo del monitor (AC-HIS-19-02)");
        }

        [Test, Category("AC-HIS-09-02")]
        public void FallbackSession()
        {
            var ghost = new HistoryEntry(new WorkoutSession(HistorySeeds.Local(2026, 1, 1, 10, 0)) { Id = 999, RoutineName = "Ya no está" }, SessionStats.Empty);
            var state = Load(ghost);
            Assert.That(state.Session.RoutineName, Is.EqualTo("Ya no está"), "si ya no existe, la de la tarjeta");
            Assert.That(state.Phase, Is.EqualTo(SessionDetailPhase.NoTelemetry));
        }

        [Test, Category("AC-HIS-09-02"), Category("AC-RUT-20-01")]
        public void CurrentRoutineSteps()
        {
            var state = Load(Entry(1));
            Assert.That(state.FlatSteps.Count, Is.EqualTo(8), "los pasos expandidos de la rutina actual");
            Assert.That(state.FlatSteps[5].TargetWattsMin, Is.EqualTo(160), "con progresiones (+10 por repetición)");
        }

        [Test, Category("AC-RUT-20-01")]
        public void EditedRoutine()
        {
            var routine = _routines.Snapshot.Single();
            var edited = routine.Steps.Select(s => s.Type == StepType.Work ? s with { TargetWattsMin = 200, TargetWattsMax = null } : s).ToList();
            _routines.UpdateAsync(routine with { Name = "Renombrada", Steps = edited });
            var state = Load(Entry(1));
            Assert.That(state.Chart!.View(ChartMetric.Watts).Targets.Select(t => t.RawTarget), Is.EqualTo(new double[] { 200, 210, 220 }),
                "las líneas objetivo usan la rutina de hoy (Q-16)");
            Assert.That(state.Session.RoutineName, Is.EqualTo("Escalera"), "renombrada: la sesión conserva su nombre");
        }

        [Test, Category("AC-RUT-20-01")]
        public void DeletedRoutine()
        {
            _routines.DeleteAsync(_seed.RoutineId);
            var state = Load(Entry(1));
            Assert.That(state.FlatSteps, Is.Empty);
            Assert.That(state.Chart!.View(ChartMetric.Watts).Targets, Is.Empty, "sin líneas");
            Assert.That(state.Chart.Bands.Count, Is.EqualTo(8), "las franjas siguen (el tipo guardado en cada punto)");
            Assert.That(state.Steps.Count, Is.EqualTo(8), "y el Rendimiento por paso");
            Assert.That(state.Title(LocalizationFixture.Es), Is.EqualTo("Escalera"));
        }

        [Test, Category("AC-HIS-09-03")]
        public void NoTelemetry()
        {
            var state = Load(Entry(4));
            Assert.That(state.Phase, Is.EqualTo(SessionDetailPhase.NoTelemetry));
            Assert.That(state.Chart, Is.Null);
            Assert.That(state.HasSteps, Is.False);
            var free = Load(Entry(2));
            Assert.That(free.Phase, Is.EqualTo(SessionDetailPhase.Ready));
            Assert.That(free.HasSteps, Is.False, "sin índices de paso no hay Rendimiento por paso");
            Assert.That(Load(Entry(1)).HasSteps, Is.True);
        }

        [Test, Category("AC-HIS-09-04")]
        public void LoadFailedAndRetry()
        {
            _sessions.PointsFailures = 1;
            using (var store = new SessionDetailStore(Entry(1), _sessions, _routines, _logger))
            {
                store.LoadAsync();
                Assert.That(store.State.Value.Phase, Is.EqualTo(SessionDetailPhase.LoadFailed), "no queda en el indicador (DV-95)");
                Assert.That(_logger.Lines.Last(), Does.StartWith("[DB] Warning: carga de la sesión: "));
                store.LoadAsync();
                Assert.That(store.State.Value.Phase, Is.EqualTo(SessionDetailPhase.Ready), "Reintentar");
            }

            _sessions.SessionFailures = 1;
            Assert.That(Load(Entry(1)).Phase, Is.EqualTo(SessionDetailPhase.LoadFailed));
        }

        [Test, Category("AC-HIS-09-03")]
        public void LoadingWhileTheReadIsInFlight()
        {
            _sessions.ReadDelay = TimeSpan.FromSeconds(1);
            using (var store = new SessionDetailStore(Entry(1), _sessions, _routines, _logger))
            {
                var task = store.LoadAsync();
                Assert.That(store.State.Value.Phase, Is.EqualTo(SessionDetailPhase.Loading));
                Assert.That(store.LoadAsync(), Is.SameAs(task), "una carga en curso no se duplica");
                _scheduler.Advance(TimeSpan.FromSeconds(1));
                _scheduler.Advance(TimeSpan.FromSeconds(1));
                Assert.That(store.State.Value.Phase, Is.EqualTo(SessionDetailPhase.Ready));
            }
        }

        [Test, Category("AC-HIS-12-01")]
        public void DefaultsToWatts()
        {
            using (var store = new SessionDetailStore(Entry(1), _sessions, _routines, _logger))
            {
                Assert.That(store.Metric.Value, Is.EqualTo(ChartMetric.Watts), "arranca en Vatios (Q-21)");
                store.SelectMetric(ChartMetric.Split);
                Assert.That(store.Metric.Value, Is.EqualTo(ChartMetric.Split));
            }

            using (var again = new SessionDetailStore(Entry(1), _sessions, _routines, _logger))
            {
                Assert.That(again.Metric.Value, Is.EqualTo(ChartMetric.Watts), "la elección no se guarda");
            }

            Assert.That(ChartMetrics.SelectorOrder, Is.EqualTo(new[]
            {
                ChartMetric.Watts, ChartMetric.Spm, ChartMetric.Split, ChartMetric.Distance, ChartMetric.HeartRate,
            }));
            Assert.That(ChartMetrics.SelectorOrder.Select(m => LocalizationFixture.Es.Get(ChartMetrics.LabelKey(m))),
                Is.EqualTo(new[] { "Vatios", "SPM", "Split 500m", "Distancia", "Pulso" }));
            Assert.That(ChartMetrics.SelectorOrder.Select(m => LocalizationFixture.En.Get(ChartMetrics.LabelKey(m))),
                Is.EqualTo(new[] { "Watts", "SPM", "Split 500m", "Distance", "HR" }));
        }

        [Test, Category("AC-HIS-09-02")]
        public void AfterDisposeLateResultsAreIgnored()
        {
            _sessions.ReadDelay = TimeSpan.FromSeconds(1);
            var store = new SessionDetailStore(Entry(1), _sessions, _routines, _logger);
            store.LoadAsync();
            store.Dispose();
            _scheduler.Advance(TimeSpan.FromSeconds(2));
            Assert.That(store.State.Value.Phase, Is.EqualTo(SessionDetailPhase.Loading));
            store.SelectMetric(ChartMetric.Spm);
            Assert.That(store.Metric.Value, Is.EqualTo(ChartMetric.Watts));
        }
    }
}
