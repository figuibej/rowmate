using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using RowMate.Core.Ble;
using RowMate.Presentation.Device;
using RowMate.Presentation.History;
using RowMate.Tests.Support;
using RowMate.Tests.UiSupport;

namespace RowMate.Snapshots
{
    // Catálogo it14 (14-pulido-plataformas/01-po-spec §7 → 02-tech-design §9): el pulido, la identidad, el lector y la deuda. Reglas de la
    // 6a §7.1: reloj de prueba, ROWMATE_DATA_DIR temporal (lo pone capture-snapshots.ps1), F-9 en una base TEMPORAL, FakeFileExporter
    // del anfitrión y FakeBle (K-10/K-11). CAP-01 y CAP-02 las escribe tools/icon (dotnet): este catálogo no las toma. Toda toma pasa por
    // TextAuditProbe (AC-APP-11-01, D-01): un texto fuera del catálogo hace fallar la toma. CAP-27 deja además CAP-27-a11y.txt (el árbol
    // del lector forzado, A11yTreeDump). Las tomas con la escena real (CAP-06, -10, -11, -14, -26), CAP-08, -09, -13, -21, -24, -25 y los
    // videos VID-01/02 quedan para issues (02-tech-design §9; ver el REPORT de la 14).
    public static class PolishCatalog
    {
        public const string LongRowerName = "AMS-670B RowMaster 2024";

        public static IReadOnlyList<SnapshotScenario> All
        {
            get
            {
                var all = new List<SnapshotScenario>();

                // CAP-03, CAP-04: el primer cuadro con la capa de arranque (bg.app y la marca de 96 dp centrada).
                foreach (var (id, stem, profile) in new[] { ("CAP-03", "CAP-03-arranque-w1280", DeviceProfile.W1280), ("CAP-04", "CAP-04-arranque-tv", DeviceProfile.TV) })
                {
                    var shot = Plain(id, stem, profile, "Primer cuadro del arranque: #0D1B2A y la marca centrada",
                    s => s.Do(h => h.Shell.BootSplash?.HoldForCapture()));
                    shot.Host.BootSplash = true;
                    shot.MotionMayRun = true;
                    shot.Checks = h => Audit(h, id, ("la capa de arranque", () => h.Shell.BootSplash != null && h.Shell.BootSplash.CurrentOpacity >= 0.999f),
                        ("última hija del shell", () => h.Shell.IndexOf(h.Shell.BootSplash!) == h.Shell.childCount - 1),
                        ("marca de 96 dp", () => Math.Abs(h.Shell.BootSplash!.Mark.layout.width - 96) < 0.6f),
                        ("marca centrada", () => Math.Abs(h.Shell.BootSplash!.Mark.worldBound.center.x - h.Root.worldBound.center.x) < 1f));
                    all.Add(shot);
                }

                // CAP-05 (L640, ventana mínima) y CAP-07 (TV): Dispositivo conectado con métricas.
                all.Add(Device("CAP-05", "CAP-05-dispositivo-l640", DeviceProfile.L640, "Ventana mínima 640 × 360: Dispositivo conectado, sin desbordes",
                    s => ConnectedWithPacket(s), h => Audit(h, "Dispositivo", ("conectado", () => h.IsShown("device-connected-view")),
                        ("sin desbordes", () => h.Audit().IsClean))));
                all.Add(Device("CAP-07", "CAP-07-dispositivo-tv", DeviceProfile.TV, "Dispositivo conectado con las métricas tabulares", s => ConnectedWithPacket(s),
                    h => Audit(h, "Dispositivo", ("conectado", () => h.IsShown("device-connected-view")))));

                // CAP-12: Historial en inglés.
                var cap12 = History("CAP-12", "CAP-12-historial-tv-en", DeviceProfile.TV, "Historial: tarjetas con fecha, duración y distancia", s => OpenHistory(s), "en-US");
                cap12.Checks = h => Audit(h, "Historial", ("tarjetas", () => h.IsShown("history-card-0")));
                all.Add(cap12);

                // CAP-15 (#40): reconectando con un nombre de 23 caracteres, TV y letra 1,3×.
                var longRow = new FakeBleDevice(FakeBleDevices.RowId, LongRowerName, FakeBleDevices.Row.Advertised, FakeBleDevices.Row.Services, -61);
                var cap15 = Device("CAP-15", "CAP-15-reconectando-tv-13", DeviceProfile.TV, "Reconectando con " + LongRowerName + "… completo (#40)",
                    s => Reconnecting(s, longRow), h => Audit(h, "Dispositivo",
                        ("nombre completo", () => (h.TextOf("device-link-text") ?? "").Contains(LongRowerName)),
                        ("la acción entera", () => h.DeviceScreen!.Scan.LinkBanner.Action.worldBound.xMax <= h.DeviceScreen.Scan.LinkBanner.worldBound.xMax + 0.5f)),
                    1.3);
                cap15.MotionMayRun = true;
                all.Add(cap15);

                // CAP-16 (#41): Buscando... con el indicador blanco; CAP-17 (#42): TH buscando con la lista y el pulsómetro en un solo desplazamiento.
                var cap16 = Device("CAP-16", "CAP-16-buscando-tv", DeviceProfile.TV, "Buscando... con el indicador blanco (#41)", s => SearchFor(s, 1),
                    h => Audit(h, "Dispositivo", ("busy", () => h.DeviceScreen!.Scan.Search.ClassListContains(ScanView.BusyClass)),
                        ("indicador de 16 dp", () => h.DeviceScreen!.Scan.Search.BusySpinner != null && h.DeviceScreen.Scan.Search.BusySpinner.IsShown)));
                cap16.MotionMayRun = true;
                all.Add(cap16);
                var cap17 = Device("CAP-17", "CAP-17-buscando-th", DeviceProfile.TH, "Buscando en TH: lista y pulsómetro con un solo desplazamiento (#42)",
                    s => SearchFor(s, 3, FakeBleDevices.Row, FakeBleDevices.Anon, FakeBleDevices.Bike),
                    h => Audit(h, "Dispositivo", ("sin el área vacía aparte", () => !h.IsShown("device-empty-area"))));
                cap17.MotionMayRun = true;
                all.Add(cap17);

                // CAP-18 (#44): la falla de la búsqueda sin el prefijo de la operación.
                all.Add(Device("CAP-18", "CAP-18-error-w1280", DeviceProfile.W1280, "Error al escanear: error 2 (#44)", s => ScanFails(s),
                    h => Audit(h, "Dispositivo", ("sin scan:", () => (h.TextOf("device-error-text") ?? "x").StartsWith("Error al escanear: ", StringComparison.Ordinal)
                                                                    && !(h.TextOf("device-error-text") ?? "").Contains("scan:")))));

                // CAP-19, CAP-20 (#69): el aviso de exportación opaco sobre el gráfico; CAP-20 con el puntero encima (en pausa).
                var cap19 = History("CAP-19", "CAP-19-aviso-tv", DeviceProfile.TV, "Guardado en Documentos\\RowMate\\TCX sobre el gráfico (#69)", s => Exported(s, false));
                cap19.Checks = h => Audit(h, "Detalle", ("aviso a la vista", () => h.Shell.Toasts.CurrentAction != null));
                all.Add(cap19);
                var cap20 = History("CAP-20", "CAP-20-aviso-hover-w1280-en", DeviceProfile.W1280, "Saved to Documents\\RowMate\\TCX con el puntero encima (#69)",
                    s => Exported(s, true), "en-US");
                cap20.Checks = h => Audit(h, "Detalle", ("en pausa", () => h.Shell.Toasts.ActionClock != null && h.Shell.Toasts.ActionClock.IsPaused));
                all.Add(cap20);

                // CAP-22, CAP-23: el Detalle (TV y W1280).
                foreach (var (id, stem, profile) in new[] { ("CAP-22", "CAP-22-detalle-tv", DeviceProfile.TV), ("CAP-23", "CAP-23-detalle-w1280", DeviceProfile.W1280) })
                {
                    var shot = History(id, stem, profile, "Detalle de Escalera con el gráfico", s => OpenDetail(s, 0));
                    shot.Checks = h => Audit(h, "Detalle", ("detalle abierto", () => h.Shell.Pages.Top is SessionDetailPage));
                    all.Add(shot);
                }

                // CAP-27: el Detalle con el lector forzado; el árbol va a CAP-27-a11y.txt.
                var cap27 = History("CAP-27", "CAP-27-lector-w1280", DeviceProfile.W1280, "Detalle con el lector forzado: CAP-27-a11y.txt (AC-APP-03-12)", s => OpenDetail(s, 0));
                cap27.Host.ScreenReader = true;
                cap27.Checks = h =>
                {
                    var dump = h.A11yDump();
                    var output = SnapshotRunner.CurrentOutput;
                    if (output != null)
                    {
                        File.WriteAllText(Path.Combine(output, "CAP-27-a11y.txt"), dump, new UTF8Encoding(false));
                    }

                    return Audit(h, "Detalle", ("el título como encabezado", () => dump.Contains("Header · Escalera")),
                        ("Exportar TCX como botón", () => dump.Contains("Button · Exportar TCX")),
                        ("el resumen del gráfico", () => dump.Contains("Image · ")));
                };
                all.Add(cap27);

                all.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
                return all;
            }
        }

        private static SnapshotScenario Plain(string id, string stem, DeviceProfile profile, string state, Func<ScenarioScript, IEnumerator>? arrange)
        {
            var options = new UiHostOptions
            {
                Profile = profile,
                IsDesktop = profile.Code == DeviceProfile.W1280.Code || profile.Code == DeviceProfile.L640.Code,
                Backend = HostBackends.TestDevices,
            };
            return new SnapshotScenario(id, stem, options, state, arrange) { BackendName = "Prueba" };
        }

        private static SnapshotScenario Device(string id, string stem, DeviceProfile profile, string state, Func<ScenarioScript, IEnumerator> arrange,
                                               Func<UiTestHost, IEnumerable<string>> checks, double textScale = 1)
        {
            var options = new UiHostOptions
            {
                Profile = profile,
                IsDesktop = profile.Code == DeviceProfile.W1280.Code || profile.Code == DeviceProfile.L640.Code,
                Backend = HostBackends.FakeBle(null, BleAdapterState.On),
                StartUtc = UiHostOptions.WorkoutStartUtc,
                TextScale = textScale,
            };
            return new SnapshotScenario(id, stem, options, state, arrange) { BackendName = "FakeBle", SkipReadingWindow = true, Checks = checks };
        }

        private static SnapshotScenario History(string id, string stem, DeviceProfile profile, string state, Func<ScenarioScript, IEnumerator> arrange,
                                                string language = "es-AR")
        {
            var options = new UiHostOptions
            {
                Profile = profile,
                IsDesktop = profile.Code == DeviceProfile.W1280.Code || profile.Code == DeviceProfile.L640.Code,
                Backend = HostBackends.TestDevices,
                LanguageTags = new[] { language },
            };
            options.UseHistory(HistorySeeds.F9);
            return new SnapshotScenario(id, stem, options, state, arrange) { BackendName = "Prueba" };
        }

        private static IEnumerator SearchFor(ScenarioScript s, double t, params FakeBleDevice[] devices)
        {
            yield return s.Settle();
            s.Host.Tap("device-search");
            foreach (var device in devices)
            {
                s.Host.FakeBle!.Adapter.Advertise(device);
            }

            yield return s.Frames(2);
            s.Host.Advance(t);
            yield return s.Frames(2);
        }

        private static IEnumerator ConnectTo(ScenarioScript s, FakeBleDevice device)
        {
            yield return s.Settle();
            s.Host.Tap("device-search");
            s.Host.FakeBle!.Adapter.Advertise(device);
            yield return s.Frames(2);
            s.Host.Advance(10.1);
            yield return s.Settle();
            s.Host.Tap(ScanResultCard.NamePrefix + device.Id);
            yield return s.Frames(2);
            s.Host.Advance(1);
            yield return s.Settle();
        }

        private static IEnumerator ConnectedWithPacket(ScenarioScript s)
        {
            yield return ConnectTo(s, FakeBleDevices.Row);
            s.Host.FakeBle!.Adapter.Notify(FakeBleDevices.RowId, FtmsUuids.RowerData, FakeBleDevices.RowerData);
            yield return s.Settle();
        }

        private static IEnumerator Reconnecting(ScenarioScript s, FakeBleDevice device)
        {
            yield return ConnectTo(s, device);
            var adapter = s.Host.FakeBle!.Adapter;
            adapter.NeverRespond(device.Id);
            adapter.DropLink(device.Id);
            yield return s.Frames(2);
            s.Host.Advance(1);
            yield return s.Frames(2);
        }

        private static IEnumerator ScanFails(ScenarioScript s)
        {
            yield return s.Settle();
            s.Host.FakeBle!.Adapter.FailNextScan(new InvalidOperationException("error 2"));
            s.Host.Tap("device-search");
            yield return s.Frames(2);
            s.Host.Advance(0.5);
            yield return s.Settle();
        }

        private static IEnumerator OpenHistory(ScenarioScript s)
        {
            yield return s.Tap("tab-history");
            yield return s.Do(h => h.Shell.HistoryScreen!.List.FinishBatches());
            yield return s.Settle();
        }

        private static IEnumerator OpenDetail(ScenarioScript s, int card)
        {
            yield return OpenHistory(s);
            yield return s.Tap("history-card-" + card);
            yield return s.Settle();
            yield return s.Do(h =>
            {
                if (h.Shell.Pages.Top is SessionDetailPage page && page.Chart.Data != null)
                {
                    page.Chart.FinishEntry();
                }
            });
            yield return s.Frames(2);
        }

        private static IEnumerator Exported(ScenarioScript s, bool hover)
        {
            yield return OpenDetail(s, 0);
            yield return s.Tap(ExportTcxButton.ElementName);
            for (var i = 0; i < 240 && s.Host.FakeExporter!.Requests.Count == 0; i++)
            {
                yield return null;
            }

            yield return s.Frames(2);
            yield return s.Advance(0.3);
            for (var i = 0; i < 240 && !(s.Host.App.Export.Busy.Value.Count == 0 && s.Host.Shell.Toasts.IsVisible); i++)
            {
                yield return null;
            }

            yield return s.Frames(2);
            yield return s.Settle();
            if (hover)
            {
                yield return s.Hover("toast");
            }
        }

        // Lo propio de cada toma y la auditoría de textos (D-01).
        private static IEnumerable<string> Audit(UiTestHost h, string screen, params (string What, Func<bool> Ok)[] checks)
        {
            var problems = new List<string>();
            foreach (var check in checks)
            {
                bool ok;
                try
                {
                    ok = check.Ok();
                }
                catch (Exception error)
                {
                    problems.Add("no se cumple: " + check.What + " (" + error.GetType().Name + ": " + error.Message + ")");
                    continue;
                }

                if (!ok)
                {
                    problems.Add("no se cumple: " + check.What);
                }
            }

            problems.AddRange(TextAuditProbe.Violations(h, screen, "toma", LongRowerName, "Escalera", "Rutina borrada", ExportCatalog.FileName));
            return problems;
        }
    }
}
