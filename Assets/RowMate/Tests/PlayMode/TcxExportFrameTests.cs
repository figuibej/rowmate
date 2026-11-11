using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using NUnit.Framework;
using RowMate.Bootstrap;
using RowMate.Core.Common;
using RowMate.Core.Ports;
using RowMate.Presentation.History;
using RowMate.Tests.Support;
using RowMate.Tests.UiSupport;
using UnityEngine.TestTools;
using Debug = UnityEngine.Debug;

namespace RowMate.Tests.PlayMode
{
    // TEF · "Exportar TCX" no traba la UI (12-exportar-tcx/02-tech-design DT12-08, §6.3; AC-STR-09-12): con el exportador REAL de
    // Windows (Bootstrap.ExportLocations.CreateExporter) sobre una carpeta TEMPORAL, la sesión más larga de F-9 (n.º 5, 1:02:05, 746
    // puntos). El reloj de prueba avanza lo mismo que el reloj real en cada cuadro: el aviso llega en ≤ 1 s desde el toque y el C# de
    // ningún cuadro (el toque, el Tick y lo que vuelve al hilo principal) pasa de 50 ms: el armado y el disco van en Task.Run.
    // En batchmode los cuadros del Editor dependen del equipo: se mide NUESTRO trabajo en el hilo principal (como HistoryPerformanceTests).
    public sealed class TcxExportFrameTests : ExportUiTestBase
    {
        private string? _folder;

        [TearDown]
        public void DeleteFolder()
        {
            if (_folder != null && Directory.Exists(_folder))
            {
                Directory.Delete(_folder, true);
            }

            _folder = null;
        }

        [UnityTest, Category("AC-STR-09-12")]
        public IEnumerator NoticeUnderOneSecondNoFrameOver50Ms()
        {
            _folder = Path.Combine(Path.GetTempPath(), "RowMate.Tests", "tef-" + Guid.NewGuid().ToString("N"));
            var exporter = ExportLocations.CreateExporter(PlatformKind.Windows, new ExportLocation(_folder, null), new RecordingLogger());
            Assert.That(exporter, Is.Not.Null.And.Not.InstanceOf<FakeFileExporter>(), "el exportador real de Windows");
            yield return OpenSession(4, DeviceProfile.W1280, o => o.Exporter = exporter);
            Assert.That(Detail.Store.State.Value.Session.TotalTimeSeconds, Is.GreaterThan(3600), "la sesión de 1 h");
            yield return Frames(3);

            var sinceTap = Stopwatch.StartNew();
            var work = Stopwatch.StartNew();
            H.Tap(ExportTcxButton.ElementName);
            work.Stop();
            var worst = work.Elapsed.TotalMilliseconds;
            var last = sinceTap.Elapsed;
            var frames = 0;
            while (H.Shell.Toasts.CurrentAction == null && sinceTap.Elapsed.TotalSeconds < 5)
            {
                yield return null;
                frames++;
                var now = sinceTap.Elapsed;
                work.Restart();
                H.Advance(now - last);
                work.Stop();
                last = now;
                worst = Math.Max(worst, work.Elapsed.TotalMilliseconds);
            }

            sinceTap.Stop();
            Debug.Log("[TEF] aviso en " + sinceTap.Elapsed.TotalMilliseconds.ToString("0") + " ms (" + frames + " cuadros); peor trabajo del hilo principal "
                      + worst.ToString("0.0") + " ms");
            Assert.That(H.Shell.Toasts.CurrentAction, Is.Not.Null, "llegó el aviso");
            Assert.That(H.Shell.Toasts.CurrentAction!.Title, Is.EqualTo(SavedTitle));
            Assert.That(sinceTap.Elapsed.TotalSeconds, Is.LessThanOrEqualTo(1.0), "el aviso en ≤ 1 s desde el toque");
            Assert.That(worst, Is.LessThanOrEqualTo(50), "ningún cuadro > 50 ms de trabajo en el hilo principal");
            var files = Directory.GetFiles(_folder);
            Assert.That(files.Length, Is.EqualTo(1), "un archivo, sin temporales");
            Assert.That(Path.GetFileName(files[0]), Does.StartWith("RowMate_2026-10-").And.EndWith(".tcx"));
        }

        // It. 15 (#71, AC-D15-16b): la sesión de 3 h (2 160 puntos, TcxSeeds.ThreeHours) con el exportador real: el aviso en ≤ 1 s y ningún
        // cuadro con más de 50 ms de trabajo en el hilo principal (AC-STR-09-12). Las sesiones en memoria (InMemorySessionRepository).
        [UnityTest, Category("AC-D15-16"), Category("AC-STR-09-12")]
        public IEnumerator ThreeHoursNoticeUnderOneSecond()
        {
            _folder = Path.Combine(Path.GetTempPath(), "RowMate.Tests", "tef3-" + Guid.NewGuid().ToString("N"));
            var exporter = ExportLocations.CreateExporter(PlatformKind.Windows, new ExportLocation(_folder, null), new RecordingLogger());
            Create(new UiHostOptions
            {
                Profile = DeviceProfile.W1280,
                IsDesktop = true,
                Exporter = exporter,
                Sessions = (scheduler, logger, routines) =>
                {
                    var repository = new InMemorySessionRepository();
                    var seed = TcxSeeds.ThreeHours;
                    var id = repository.CreateAsync(seed.Session with { Id = null }).Result;
                    var points = new System.Collections.Generic.List<RowMate.Core.Models.DataPoint>(seed.Points.Count);
                    foreach (var point in seed.Points)
                    {
                        points.Add(point with { SessionId = id });
                    }

                    repository.FinishAsync(points, seed.Session with { Id = id }).Wait();
                    return repository;
                },
            });
            yield return GoToHistory();
            yield return OpenCard(0);
            Assert.That(Detail.Store.State.Value.Session.TotalTimeSeconds, Is.EqualTo(3 * 3600), "la sesión de 3 h");
            yield return Frames(3);

            var sinceTap = Stopwatch.StartNew();
            var work = Stopwatch.StartNew();
            H.Tap(ExportTcxButton.ElementName);
            work.Stop();
            var worst = work.Elapsed.TotalMilliseconds;
            var last = sinceTap.Elapsed;
            while (H.Shell.Toasts.CurrentAction == null && sinceTap.Elapsed.TotalSeconds < 5)
            {
                yield return null;
                var now = sinceTap.Elapsed;
                work.Restart();
                H.Advance(now - last);
                work.Stop();
                last = now;
                worst = Math.Max(worst, work.Elapsed.TotalMilliseconds);
            }

            sinceTap.Stop();
            Debug.Log("[TEF] 3 h: aviso en " + sinceTap.Elapsed.TotalMilliseconds.ToString("0") + " ms; peor trabajo del hilo principal " + worst.ToString("0.0") + " ms");
            Assert.That(H.Shell.Toasts.CurrentAction, Is.Not.Null, "llegó el aviso");
            Assert.That(sinceTap.Elapsed.TotalSeconds, Is.LessThanOrEqualTo(1.0), "el aviso en ≤ 1 s desde el toque");
            Assert.That(worst, Is.LessThanOrEqualTo(50), "ningún cuadro > 50 ms de trabajo en el hilo principal");
            Assert.That(Directory.GetFiles(_folder).Length, Is.EqualTo(1));
        }
    }
}
