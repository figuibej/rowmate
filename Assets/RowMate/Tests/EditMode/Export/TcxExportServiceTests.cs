using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using RowMate.App.Export;
using RowMate.Core.Common;
using RowMate.Core.Design;
using RowMate.Core.Export;
using RowMate.Core.Localization;
using RowMate.Core.Models;
using RowMate.Core.Ports;
using RowMate.Tests.EditMode.Localization;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Export
{
    // ES · "Exportar TCX" (F-STR-09; 02-tech-design §4, DT12-06, -11, -12, -13, -19): relee los puntos, solo lee, una exportación
    // por sesión, el indicador ≥ 300 ms, avisos Saved/Failed (Share no avisa) y el registro sin rutas del equipo. Con F-9, el
    // programador falso y FakeFileExporter en una carpeta TEMPORAL (nunca Documentos).
    [TestFixture]
    public sealed class TcxExportServiceTests
    {
        private FakeScheduler _scheduler = null!;
        private RecordingLogger _logger = null!;
        private ScriptedSessionRepository _sessions = null!;
        private HistorySeedResult _seed = null!;
        private ObservableValue<Localizer> _texts = null!;
        private string _root = null!;
        private TimeZoneInfo _zone = null!;
        private readonly List<TcxExportService> _services = new List<TcxExportService>();

        [SetUp]
        public void SetUp()
        {
            _scheduler = new FakeScheduler();
            _logger = new RecordingLogger();
            _sessions = new ScriptedSessionRepository(_scheduler);
            _seed = HistorySeeds.Apply(new InMemoryRoutineRepository(), _sessions, HistorySeeds.F9);
            _texts = new ObservableValue<Localizer>(LocalizationFixture.Es, _logger);
            _root = Path.Combine(Path.GetTempPath(), "rowmate-export-service-" + Guid.NewGuid().ToString("N"));
            _zone = HistorySeeds.Zone;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var service in _services)
            {
                service.Dispose();
            }

            _services.Clear();
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, true);
            }
        }

        private FakeFileExporter Exporter(ExportDelivery delivery = ExportDelivery.SaveToFolder, string? label = null)
        {
            return new FakeFileExporter(Path.Combine(_root, "exports"), label, delivery);
        }

        private TcxExportService Service(IFileExporter exporter, bool simulator = false)
        {
            var service = new TcxExportService(_sessions, exporter, _scheduler, () => _zone, new ReadOnlyObservable<Localizer>(_texts), simulator,
                _logger);
            _services.Add(service);
            return service;
        }

        private WorkoutSession Session(int number)
        {
            return _sessions.Store.Session(_seed.Session(number))!;
        }

        private static List<TcxExportNotice> Record(TcxExportService service)
        {
            var notices = new List<TcxExportNotice>();
            service.Notices.Subscribe(notices.Add);
            return notices;
        }

        private void Settle()
        {
            _scheduler.Advance(TimeSpan.FromMilliseconds(ExportMotionSpecs.MinBusyMs));
        }

        [Test, Category("AC-STR-09-19"), Category("AC-STR-09-26")]
        public void SavedNoticeHasFileAndLocation()
        {
            var exporter = Exporter();
            var service = Service(exporter);
            var notices = Record(service);
            var task = service.ExportAsync(Session(1));
            Assert.That(notices, Is.Empty, "el aviso llega después del mínimo del indicador");
            Settle();
            Assert.That(task.IsCompleted && !task.IsFaulted, Is.True);
            Assert.That(notices.Count, Is.EqualTo(1));
            var notice = notices[0];
            Assert.That(notice.Kind, Is.EqualTo(TcxExportNoticeKind.Saved));
            Assert.That(notice.Session.Id, Is.EqualTo(_seed.Session(1)));
            Assert.That(notice.File!.FileName, Is.EqualTo("RowMate_2026-11-05_1840_Escalera.tcx"));
            Assert.That(notice.File.Location, Is.SameAs(exporter.Location));
            Assert.That(ExportFolderLabel.For(notice.File.Location, LocalizationFixture.Es), Is.EqualTo("Documentos\\RowMate\\TCX"));
            var expected = TcxDocument.Encode(TcxBuilder.Build(Session(1), HistorySeeds.Points(1, _seed.Session(1)), new TcxOptions(_zone, false)));
            Assert.That(File.ReadAllBytes(notice.File.FullPath), Is.EqualTo(expected), "el mismo archivo que arma TcxBuilder");
            Assert.That(_logger.Lines, Does.Contain("[EXPORT] Info: TCX guardado: RowMate_2026-11-05_1840_Escalera.tcx en Documentos\\RowMate\\TCX"));
            // Exportar otra vez no pisa: _2 (AC-STR-09-26 con dotnet).
            service.ExportAsync(Session(1));
            Settle();
            Assert.That(notices[1].File!.FileName, Is.EqualTo("RowMate_2026-11-05_1840_Escalera_2.tcx"));
        }

        [Test, Category("AC-STR-09-19"), Category("AC-STR-09-24")]
        public void ShareHasNoNotice()
        {
            var exporter = Exporter(ExportDelivery.Share);
            var service = Service(exporter);
            var notices = Record(service);
            Assert.That(service.Delivery, Is.EqualTo(ExportDelivery.Share));
            service.ExportAsync(Session(2));
            Settle();
            Assert.That(notices, Is.Empty, "la hoja del sistema es la confirmación (A12-07)");
            Assert.That(exporter.Files.Count, Is.EqualTo(1));
            Assert.That(exporter.Files[0].FileName, Is.EqualTo("RowMate_2026-11-04_0715_Libre.tcx"), "una libre usa historyFree");
            Assert.That(service.Busy.Value, Is.Empty);
        }

        [Test, Category("AC-STR-09-18")]
        public void BusyAtLeast300Ms()
        {
            var service = Service(Exporter());
            var notices = Record(service);
            var changes = 0;
            service.Busy.Subscribe(_ => changes++, false);
            var id = _seed.Session(1);
            service.ExportAsync(Session(1));
            Assert.That(service.Busy.Value, Is.EqualTo(new[] { id }));
            Assert.That(service.IsBusy(id), Is.True);
            _scheduler.Advance(TimeSpan.FromMilliseconds(299));
            Assert.That(service.IsBusy(id), Is.True, "a los 299 ms sigue ocupado");
            Assert.That(notices, Is.Empty);
            _scheduler.Advance(TimeSpan.FromMilliseconds(1));
            Assert.That(service.IsBusy(id), Is.False);
            Assert.That(notices.Count, Is.EqualTo(1));
            Assert.That(changes, Is.EqualTo(2), "Busy cambia dos veces por exportación");
            Assert.That(ExportMotionSpecs.MinBusyMs, Is.EqualTo(300));
            Assert.That(ExportMotionSpecs.NoticeMs, Is.EqualTo(6000));
            Assert.That(ExportMotionSpecs.SpinnerDp, Is.EqualTo(18));
            Assert.That(ExportMotionSpecs.StrokeDp, Is.EqualTo(2));
        }

        [Test, Category("AC-STR-09-18")]
        public void SlowExportHasNoExtraWait()
        {
            // Si el exportador ya tardó más de 300 ms, el aviso llega al terminar (sin espera extra).
            var exporter = Exporter();
            exporter.Hold();
            var service = Service(exporter);
            var notices = Record(service);
            service.ExportAsync(Session(1));
            _scheduler.Advance(TimeSpan.FromMilliseconds(500));
            Assert.That(notices, Is.Empty, "retenido");
            Assert.That(service.IsBusy(_seed.Session(1)), Is.True);
            exporter.Release();
            Assert.That(notices.Count, Is.EqualTo(1), "en el acto");
            Assert.That(service.Busy.Value, Is.Empty);
        }

        [Test, Category("AC-STR-09-18")]
        public void SecondRequestSameTask()
        {
            var exporter = Exporter();
            var service = Service(exporter);
            var notices = Record(service);
            var first = service.ExportAsync(Session(1));
            var second = service.ExportAsync(Session(1));
            Assert.That(second, Is.SameAs(first), "una exportación en curso por sesión");
            var other = service.ExportAsync(Session(2));
            Assert.That(other, Is.Not.SameAs(first), "otra sesión, otra exportación");
            Settle();
            Assert.That(notices.Count, Is.EqualTo(2));
            Assert.That(exporter.Requests.Count, Is.EqualTo(2));
            Assert.That(service.ExportAsync(Session(1)), Is.Not.SameAs(first), "terminada, una nueva");
        }

        [Test, Category("AC-STR-09-20")]
        public void RevealDelegates()
        {
            var exporter = Exporter();
            var service = Service(exporter);
            var notices = Record(service);
            service.ExportAsync(Session(1));
            Settle();
            service.Reveal(notices[0].File!);
            Assert.That(exporter.Reveals.Count, Is.EqualTo(1));
            Assert.That(exporter.Reveals[0], Is.SameAs(notices[0].File));
        }

        [Test, Category("AC-D15-13")]
        public void RevealFailureNotifies()
        {
            // It. 15 (#70): el Explorador y el respaldo fallan → aviso OpenFolderFailed con la sesión y el archivo del Saved, y el
            // detalle técnico en el registro con la ruta abreviada. Reveal no lanza.
            var exporter = Exporter(label: null);
            var folder = exporter.Location.Directory;
            var service = Service(exporter);
            var notices = Record(service);
            service.ExportAsync(Session(1));
            Settle();
            Assert.That(notices.Single().Kind, Is.EqualTo(TcxExportNoticeKind.Saved));
            service.Reveal(notices[0].File!);
            Assert.That(notices.Count, Is.EqualTo(1), "si abre, no avisa nada");
            exporter.FailReveals();
            Assert.DoesNotThrow(() => service.Reveal(notices[0].File!));
            Assert.That(notices.Count, Is.EqualTo(2));
            Assert.That(notices[1].Kind, Is.EqualTo(TcxExportNoticeKind.OpenFolderFailed));
            Assert.That(notices[1].Session.Id, Is.EqualTo(_seed.Session(1)));
            Assert.That(notices[1].File, Is.SameAs(notices[0].File));
            var line = _logger.Lines.Last(l => l.StartsWith("[EXPORT] Warning", StringComparison.Ordinal));
            Assert.That(line, Does.Contain("RevealFailedException"));
            Assert.That(line, Does.Not.Contain(folder), "la ruta abreviada (AC-STR-09-19)");
            Assert.That(ExportMotionSpecs.OpenFolderFailedMs, Is.EqualTo(4000));
            service.Dispose();
            service.Reveal(notices[0].File!);
            Assert.That(notices.Count, Is.EqualTo(2), "después de Dispose, nada");
        }

        [Test, Category("AC-D15-15")]
        public void AppVersionReachesTheCreator()
        {
            var exporter = Exporter();
            var service = new TcxExportService(_sessions, exporter, _scheduler, () => _zone, new ReadOnlyObservable<Localizer>(_texts), false,
                _logger, "2.3.4");
            _services.Add(service);
            service.ExportAsync(Session(1));
            Settle();
            var text = Encoding.UTF8.GetString(File.ReadAllBytes(exporter.Files.Single().FullPath));
            Assert.That(text, Does.Contain("          <VersionMajor>2</VersionMajor>\n          <VersionMinor>3</VersionMinor>\n"
                + "          <BuildMajor>4</BuildMajor>\n          <BuildMinor>0</BuildMinor>\n"));
        }

        [Test, Category("AC-STR-09-21")]
        public void FailureNoticeAndScrubbedLog()
        {
            var exporter = Exporter();
            var folder = exporter.Location.Directory;
            var user = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            exporter.FailNext(new UnauthorizedAccessException("Access to the path '" + folder + Path.DirectorySeparatorChar + "~rowmate-1.part' is denied."));
            var service = Service(exporter);
            var notices = Record(service);
            service.ExportAsync(Session(1));
            Assert.That(service.IsBusy(_seed.Session(1)), Is.True, "también una falla dura el mínimo");
            Settle();
            Assert.That(notices.Count, Is.EqualTo(1));
            Assert.That(notices[0].Kind, Is.EqualTo(TcxExportNoticeKind.Failed));
            Assert.That(notices[0].File, Is.Null);
            Assert.That(service.Busy.Value, Is.Empty, "el botón vuelve a su ícono");
            var line = _logger.Lines.Single(l => l.StartsWith("[EXPORT]", StringComparison.Ordinal));
            Assert.That(line, Is.EqualTo("[EXPORT] Warning: no se pudo exportar: UnauthorizedAccessException: Access to the path 'Documentos\\RowMate\\TCX"
                + Path.DirectorySeparatorChar + "~rowmate-1.part' is denied."));
            Assert.That(line, Does.Not.Contain(folder));
            if (user.Length > 0)
            {
                Assert.That(line, Does.Not.Contain(user));
            }

            Assert.That(Directory.Exists(folder) ? Directory.GetFiles(folder) : new string[0], Is.Empty, "nada escrito");
            // Reintentar = exportar otra vez la misma sesión.
            service.ExportAsync(notices[0].Session);
            Settle();
            Assert.That(notices[1].Kind, Is.EqualTo(TcxExportNoticeKind.Saved));
        }

        [Test, Category("AC-STR-09-21")]
        public void UserFolderBecomesUserProfileInTheLog()
        {
            var user = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (user.Length == 0)
            {
                Assert.Ignore("este equipo no tiene carpeta del usuario");
            }

            var exporter = Exporter();
            exporter.FailNext(new IOException("en " + Path.Combine(user, "otra") + " no"));
            var service = Service(exporter);
            service.ExportAsync(Session(1));
            Settle();
            Assert.That(_logger.Lines, Does.Contain("[EXPORT] Warning: no se pudo exportar: IOException: en %USERPROFILE%" + Path.DirectorySeparatorChar + "otra no"));
        }

        [Test, Category("AC-STR-09-10"), Category("AC-STR-09-21")]
        public void NotExportableFails()
        {
            var exporter = Exporter();
            var service = Service(exporter);
            var notices = Record(service);
            service.ExportAsync(Session(4));
            Settle();
            Assert.That(notices.Single().Kind, Is.EqualTo(TcxExportNoticeKind.Failed), "n.º 4 sin puntos");
            Assert.That(exporter.Requests, Is.Empty, "no llega al exportador");
        }

        [Test, Category("AC-STR-09-07"), Category("AC-STR-09-11")]
        public void NeverWritesTheRepository()
        {
            var calls = _sessions.Calls.Count;
            var before = _sessions.Store.Session(_seed.Session(3));
            var pointsBefore = _sessions.Store.PointsOf(_seed.Session(3)).Count;
            var service = Service(Exporter());
            service.ExportAsync(Session(3));
            Settle();
            Assert.That(_sessions.Calls.Count, Is.EqualTo(calls), "ninguna escritura (la n.º 3 cortada no se repara en la base)");
            Assert.That(_sessions.Store.Session(_seed.Session(3)), Is.EqualTo(before));
            Assert.That(_sessions.Store.Session(_seed.Session(3))!.FinishedAt, Is.Null);
            Assert.That(_sessions.Store.PointsOf(_seed.Session(3)).Count, Is.EqualTo(pointsBefore));
        }

        [Test, Category("AC-STR-09-11")]
        public void SameContentTwice()
        {
            var exporter = Exporter();
            var service = Service(exporter);
            service.ExportAsync(Session(1));
            Settle();
            service.ExportAsync(Session(1));
            Settle();
            Assert.That(File.ReadAllBytes(exporter.Files[1].FullPath), Is.EqualTo(File.ReadAllBytes(exporter.Files[0].FullPath)));
        }

        [Test, Category("AC-STR-09-15")]
        public void SimulatorBuildMarksNameAndNotes()
        {
            var exporter = Exporter();
            var service = Service(exporter, simulator: true);
            service.ExportAsync(Session(1));
            Settle();
            service.ExportAsync(Session(1));
            Settle();
            Assert.That(exporter.Files.Select(f => f.FileName), Is.EqualTo(new[]
            {
                "RowMate_2026-11-05_1840_Escalera_SIM.tcx", "RowMate_2026-11-05_1840_Escalera_SIM_2.tcx",
            }));
            Assert.That(Encoding.UTF8.GetString(File.ReadAllBytes(exporter.Files[0].FullPath)), Does.Contain("      <Notes>[SIM] Escalera</Notes>\n"));
        }

        [Test, Category("AC-STR-09-24"), Category("AC-STR-09-23")]
        public void RequestHasTcxMimeAndShareTitle()
        {
            var exporter = Exporter(ExportDelivery.Share);
            var service = Service(exporter);
            var notices = Record(service);
            service.ExportAsync(Session(1));
            Settle();
            _texts.Value = LocalizationFixture.En;
            service.ExportAsync(Session(2));
            Settle();
            var requests = exporter.Requests;
            Assert.That(requests.Count, Is.EqualTo(2));
            Assert.That(requests[0].MimeType, Is.EqualTo("application/vnd.garmin.tcx+xml"));
            Assert.That(requests[0].Extension, Is.EqualTo(".tcx"));
            Assert.That(requests[0].BaseName, Is.EqualTo("RowMate_2026-11-05_1840_Escalera"));
            Assert.That(requests[0].ShareTitle, Is.EqualTo("Compartir sesión"));
            Assert.That(requests[1].ShareTitle, Is.EqualTo("Share session"), "el idioma vigente al pedir");
            Assert.That(requests[1].BaseName, Is.EqualTo("RowMate_2026-11-04_0715_Free"));
            Assert.That(notices, Is.Empty, "sin aviso de éxito");
            Assert.That(service.Busy.Value, Is.Empty);
        }

        [Test, Category("AC-STR-09-02"), Category("AC-STR-09-13")]
        public void ZoneAtExportTime()
        {
            var exporter = Exporter();
            var service = Service(exporter);
            _zone = TestZones.Kolkata;
            service.ExportAsync(Session(1));
            Settle();
            Assert.That(exporter.Files[0].FileName, Is.EqualTo("RowMate_2026-11-06_0310_Escalera.tcx"), "21:40Z en +05:30");
            Assert.That(Encoding.UTF8.GetString(File.ReadAllBytes(exporter.Files[0].FullPath)), Does.Contain("<Id>2026-11-06T03:10:00+05:30</Id>"));
        }

        [Test, Category("AC-STR-09-18")]
        public void DisposeStopsNoticesAndNeverThrows()
        {
            var exporter = Exporter();
            var service = Service(exporter);
            var notices = Record(service);
            service.ExportAsync(Session(1));
            service.Dispose();
            Settle();
            Assert.That(notices, Is.Empty);
            Assert.That(service.ExportAsync(Session(1)).IsCompleted, Is.True, "dispuesto: no hace nada");
            var broken = new TcxExportService(_sessions, exporter, _scheduler, () => throw new InvalidOperationException("zona"),
                new ReadOnlyObservable<Localizer>(_texts), false, _logger);
            _services.Add(broken);
            var brokenNotices = Record(broken);
            var task = broken.ExportAsync(Session(1));
            Settle();
            Assert.That(task.IsFaulted, Is.False, "nunca lanza");
            Assert.That(brokenNotices.Single().Kind, Is.EqualTo(TcxExportNoticeKind.Failed));
            Assert.That(broken.ExportAsync(new WorkoutSession(HistorySeeds.Local(2026, 1, 1, 0, 0))).IsCompleted, Is.True, "sin id: nada");
        }

        [Test, Category("AC-STR-09-23")]
        public void NoExporterMeansNoDelivery()
        {
            var service = Service(NoFileExporter.Instance);
            Assert.That(service.Delivery, Is.EqualTo(ExportDelivery.None));
            Assert.That(NoFileExporter.Instance.Location, Is.Null);
            Assert.That(LocalizationFixture.Es.Get(L10nKeys.ExportTcx), Is.EqualTo("Exportar TCX"));
        }
    }
}
