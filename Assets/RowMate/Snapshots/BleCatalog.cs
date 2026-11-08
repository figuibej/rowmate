using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using RowMate.App.Device;
using RowMate.Core.Ble;
using RowMate.Presentation.Device;
using RowMate.Tests.Support;
using RowMate.Tests.UiSupport;
using UnityEngine;

namespace RowMate.Snapshots
{
    // Catálogo it10a (10a-ble-android-windows/01-po-spec §6 → 02-tech-design §8.4): CAP-01 a -20 en los tamaños de la spec (a/b cuando la
    // spec pide dos) y AUX-10a-01 y -02. Cada toma arma su UiTestHost con el remo REAL (RowerService, Core) sobre el adaptador FALSO con los
    // equipos ficticios K-10 (HostBackends.FakeBle) y el paquete del oráculo ftms.json, el reloj de prueba, ROWMATE_DATA_DIR temporal y
    // ROWMATE_BLE=off (capture-snapshots.ps1): ninguna toma toca la radio ni muestra un equipo real. Los instantes de la spec (buscar a los
    // 1 200 ms, la espera de la reconexión a los 1 500 ms, "Listo" dentro de sus 240 ms) van con ExactInstant: la toma es en ese instante.
    // El arte fino (sonar, ilustración, check dibujado, "se despierta") es de WP-D: estas tomas muestran la versión funcional de WP-B.
    public static class BleCatalog
    {
        public const string Backend = "Remo real + adaptador falso (K-10)";

        public static IReadOnlyList<SnapshotScenario> All
        {
            get
            {
                var all = new List<SnapshotScenario>();
                AddStates(all);
                AddSearch(all);
                AddConnect(all);
                AddLink(all);
                AddDebug(all);
                AddAux(all);
                all.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
                return all;
            }
        }

        // ── CAP-01 a -04: la vista de estado ──

        private static void AddStates(List<SnapshotScenario> all)
        {
            all.Add(Expecting(Ble("CAP-01a", "CAP-01a-bt-apagado-tv", DeviceProfile.TV, "Adaptador apagado", Rest, initial: BleAdapterState.Off),
                ("Bluetooth apagado", h => h.TextOf("device-bt-title") == "Bluetooth apagado"), ("Activar Bluetooth", h => h.IsShown("device-bt-enable")),
                ("sin la tarjeta del pulsómetro", h => !h.IsShown("hrm-card"))));
            all.Add(Expecting(Ble("CAP-01b", "CAP-01b-bt-apagado-w1280", DeviceProfile.W1280, "Adaptador apagado en el escritorio", Rest,
                initial: BleAdapterState.Off), ("Bluetooth apagado", h => h.TextOf("device-bt-title") == "Bluetooth apagado")));
            all.Add(Expecting(Ble("CAP-02", "CAP-02-bt-apagado-tv-en", DeviceProfile.TV, "Adaptador apagado, en inglés", Rest, initial: BleAdapterState.Off,
                language: "en-US"), ("Bluetooth is off", h => h.TextOf("device-bt-title") == "Bluetooth is off"),
                ("Enable Bluetooth", h => h.TextOf("device-bt-enable") == "Enable Bluetooth")));
            all.Add(Expecting(Ble("CAP-03", "CAP-03-sin-permiso-tv", DeviceProfile.TV, "Sin permiso de Bluetooth", Rest, initial: BleAdapterState.Unauthorized),
                ("Permiso de Bluetooth denegado", h => h.TextOf("device-bt-title") == "Permiso de Bluetooth denegado")));
            all.Add(Expecting(Ble("CAP-04", "CAP-04-sin-ble-tv", DeviceProfile.TV, "Equipo sin Bluetooth LE", Rest, initial: BleAdapterState.Unsupported),
                ("Este equipo no tiene Bluetooth LE", h => h.TextOf("device-bt-title") == "Este equipo no tiene Bluetooth LE"),
                ("sin botón", h => !h.IsShown("device-bt-enable"))));
        }

        // ── CAP-05 a -08: la búsqueda ──

        private static void AddSearch(List<SnapshotScenario> all)
        {
            var cap05 = Expecting(Ble("CAP-05", "CAP-05-buscando-tv", DeviceProfile.TV, "Buscar, sin anuncios, a los 1 200 ms", s => SearchFor(s, 1.2)),
                ("Buscando...", h => h.TextOf("device-search") == "Buscando..."), ("la ayuda", h => h.IsShown("device-search-hint")),
                ("sin el vacío", h => h.DeviceScreen!.Scan.EmptyTitle.resolvedStyle.display == UnityEngine.UIElements.DisplayStyle.None
                                      || !UiQuery.IsShown(h.DeviceScreen!.Scan.EmptyTitle)));
            cap05.ExactInstant = true;
            all.Add(cap05);

            all.Add(ListShot("CAP-06a", "CAP-06a-lista-tv", DeviceProfile.TV));
            all.Add(ListShot("CAP-06b", "CAP-06b-lista-w1280", DeviceProfile.W1280));
            all.Add(ListShot("CAP-07a", "CAP-07a-lista-pulsometro-l640", DeviceProfile.L640));
            all.Add(ListShot("CAP-07b", "CAP-07b-lista-pulsometro-th", DeviceProfile.TH));

            all.Add(Expecting(Ble("CAP-08", "CAP-08-error-escanear-tv", DeviceProfile.TV, "FailNextScan(BleException(\"scan\", \"error 2\"))", s => Tap(s, "device-search"),
                    a => a.FailNextScan(new BleException("scan", "error 2", 2))),
                ("Error al escanear", h => (h.TextOf("device-error-text") ?? "").StartsWith("Error al escanear: ", StringComparison.Ordinal)),
                ("Buscar habilitado", h => h.Find("device-search")!.enabledSelf)));
        }

        private static SnapshotScenario ListShot(string id, string stem, DeviceProfile profile)
        {
            return Expecting(Ble(id, stem, profile, "Anuncios Hp, Row, Anon, Bike, Row; 10 s vencidos" + (id.StartsWith("CAP-07", StringComparison.Ordinal)
                    ? "; tarjeta del pulsómetro de prueba" : ""), s => SearchDone(s, FakeBleDevices.Hp, FakeBleDevices.Row, FakeBleDevices.Anon,
                    FakeBleDevices.Bike, FakeBleDevices.Row)),
                ("3 tarjetas", h => h.DeviceScreen!.Scan.Results.Count == 3),
                ("AMS-670B, Dispositivo desconocido, Bici FTMS", h => Names(h) == "AMS-670B|Dispositivo desconocido|Bici FTMS"),
                ("sin los auriculares", h => !h.HasText("Auriculares")), ("sin desborde", h => h.Audit().IsClean));
        }

        // ── CAP-09 a -13: la conexión ──

        private static void AddConnect(List<SnapshotScenario> all)
        {
            var slow = (Action<FakeBleAdapter>)(a => a.RespondAfter(FakeBleDevices.RowId, TimeSpan.FromSeconds(1)));
            var cap09 = Expecting(Ble("CAP-09", "CAP-09-conectando-tv", DeviceProfile.TV, "Tocar AMS-670B: Conectando… (el connect tarda 1 s)",
                    s => TapRowAt(s, 0.4), slow),
                ("Conectando…", h => Card(h, FakeBleDevices.RowId).Line2.text == "Conectando…"), ("todas con spinner", h => Card(h, FakeBleDevices.AnonId).ShowsSpinner));
            cap09.ExactInstant = true;
            all.Add(cap09);

            var cap10 = Expecting(Ble("CAP-10", "CAP-10-tomando-control-tv", DeviceProfile.TV, "Tomando control (tras 0x00, a los 250 de los 300 ms; el cruce de 200 ya terminó)",
                    s => TapRowAt(s, 1.25), slow),
                ("Tomando control del monitor…", h => Card(h, FakeBleDevices.RowId).Line2.text == "Tomando control del monitor…"));
            cap10.ExactInstant = true;
            all.Add(cap10);

            var cap11 = Expecting(Ble("CAP-11", "CAP-11-listo-tv", DeviceProfile.TV, "Connected + 235 ms de los 240 de \"Listo\": el check entero", s => TapRowAt(s, 1.535), slow),
                ("Listo", h => Card(h, FakeBleDevices.RowId).Line2.text == "Listo"), ("la app ya conectada", h => h.App.Device.State.Value.IsConnected),
                ("la vista retiene", h => h.DeviceScreen!.HoldingReady));
            cap11.ExactInstant = true;
            // WP-D (00-arte §9): el check casi entero (t₀ + 235 de 240); la tilde todavía termina su trazo.
            cap11.MotionMayRun = true;
            all.Add(cap11);

            all.Add(Expecting(Ble("CAP-12", "CAP-12-error-conectar-tv", DeviceProfile.TV, "AMS-670B en NeverRespond, 15 s", s => TapRowAt(s, 15.1),
                    a => a.NeverRespond(FakeBleDevices.RowId)),
                ("Error al conectar", h => (h.TextOf("device-error-text") ?? "").StartsWith("Error al conectar: ", StringComparison.Ordinal))));

            all.Add(Connected("CAP-13a", "CAP-13a-conectado-tv", DeviceProfile.TV));
            all.Add(Connected("CAP-13b", "CAP-13b-conectado-w1280", DeviceProfile.W1280));
        }

        private static SnapshotScenario Connected(string id, string stem, DeviceProfile profile)
        {
            return Expecting(Ble(id, stem, profile, "AMS-670B conectado con el paquete de ftms.json", s => ConnectedWithPacket(s)),
                ("AMS-670B", h => h.TextOf("device-name") == "AMS-670B"), ("150 W", h => h.HasText("150")),
                ("2 o 3 columnas", h => h.DeviceScreen!.Connected.Grid.Tiles.Count >= 4));
        }

        // ── CAP-14 a -18: la reconexión y el monitor apagado ──

        private static void AddLink(List<SnapshotScenario> all)
        {
            var cap14 = Expecting(Ble("CAP-14", "CAP-14-reconectando-tv", DeviceProfile.TV, "DropLink(AMS-670B) y 1 500 ms de la espera", s => Reconnecting(s, 1.5)),
                ("Reconectando con AMS-670B…", h => h.TextOf("device-link-text") == "Reconectando con AMS-670B…"),
                ("Buscar habilitado entre intentos", h => h.Find("device-search")!.enabledSelf),
                ("el arco a la mitad", h => Math.Abs(h.DeviceScreen!.Scan.LinkBanner.Indicator.Fraction - 0.5) < 0.02));
            cap14.ExactInstant = true;
            all.Add(cap14);

            all.Add(Expecting(Ble("CAP-15a", "CAP-15a-hud-reconectando-w1280", DeviceProfile.W1280, "Libre + DropLink: reconectando sobre el HUD",
                    s => HudReconnecting(s, false)), ("el aviso fijo", h => h.Shell.Toasts.CurrentLink?.Kind == LinkNoticeKind.Reconnecting)));
            all.Add(Expecting(Ble("CAP-15b", "CAP-15b-hud-reconectando-tv", DeviceProfile.TV, "Libre + DropLink: reconectando sobre el HUD",
                    s => HudReconnecting(s, false)), ("el aviso fijo", h => h.Shell.Toasts.CurrentLink?.Kind == LinkNoticeKind.Reconnecting)));
            all.Add(Expecting(Ble("CAP-16", "CAP-16-hud-reconectado-w1280-en", DeviceProfile.W1280, "Respond → Reconnected to AMS-670B (2 s), en inglés",
                    s => HudReconnecting(s, true), language: "en-US"),
                ("Reconnected to AMS-670B", h => h.Shell.Toasts.CurrentText == "Reconnected to AMS-670B")));

            all.Add(Expecting(Ble("CAP-17", "CAP-17-monitor-apagado-tv", DeviceProfile.TV, "6 s sin paquetes tras el primero: Dispositivo", MonitorStoppedDevice),
                ("El monitor dejó de enviar datos", h => h.TextOf("device-link-text") == "El monitor dejó de enviar datos"),
                ("Reintentar", h => h.TextOf("device-link-action") == "Reintentar")));
            all.Add(Expecting(Ble("CAP-18", "CAP-18-hud-monitor-apagado-w1280", DeviceProfile.W1280, "6 s sin paquetes tras el primero: HUD", MonitorStoppedHud),
                ("El monitor dejó de enviar datos", h => h.Shell.Toasts.CurrentText == "El monitor dejó de enviar datos")));
        }

        // ── CAP-19 y -20: el diagnóstico ──

        private static void AddDebug(List<SnapshotScenario> all)
        {
            all.Add(Debug("CAP-19a", "CAP-19a-diagnostico-tv", DeviceProfile.TV, FakeBleDevices.Row, true));
            all.Add(Debug("CAP-19b", "CAP-19b-diagnostico-w1280", DeviceProfile.W1280, FakeBleDevices.Row, true));
            all.Add(Debug("CAP-20", "CAP-20-diagnostico-sin-datos-tv", DeviceProfile.TV, FakeBleDevices.Bike, false));
        }

        private static SnapshotScenario Debug(string id, string stem, DeviceProfile profile, FakeBleDevice device, bool packets)
        {
            var scenario = Ble(id, stem, profile, (packets ? "Gesto + panel con AMS-670B y paquetes" : "Bici FTMS sin paquetes") + " (presión de la cabecera)",
                s => OpenDebug(s, device, packets));
            return packets
                ? Expecting(scenario, ("RECEIVING", h => h.TextOf("ble-debug-badge") == "RECEIVING"),
                    ("hex", h => h.TextOf("ble-debug-hex") == "2c 00 30 2a 01 e2 01 00 85 00 96 00"), ("contador", h => int.Parse(h.TextOf("ble-debug-count") ?? "0", CultureInfo.InvariantCulture) >= 3))
                : Expecting(scenario, ("NO DATA", h => h.TextOf("ble-debug-badge") == "NO DATA"), ("sin hora", h => h.TextOf("ble-debug-last") == "--:--:--"));
        }

        // ── AUX-10a-01 y -02 ──

        private static void AddAux(List<SnapshotScenario> all)
        {
            // El peor caso de la 4b (HudBudgetTests: tres flechas, BPM, Próximo, la cuenta grande y el sello) con el aviso del enlace encima y
            // su contorno de depuración. El remo es el de prueba de la 4b (las flechas necesitan sus lecturas); el aviso, el de ShowLink.
            var worst = new UiHostOptions
            {
                Profile = DeviceProfile.TV,
                IsDesktop = false,
                Backend = HostBackends.TestRower(k => RowerScripts.R4(k).CopyWith(heartRate: 140)),
                Routines = HostRoutines.Seeded(RoutineSeeds.Budget),
                StartUtc = UiHostOptions.WorkoutStartUtc,
            };
            var aux01 = new SnapshotScenario("AUX-10a-01", "AUX-10a-01-hud-tv-peor-caso-aviso", worst,
                "HUD de TV en el peor caso de la 4b + \"Reconectando con AMS-670B…\" con el contorno de depuración", WorstCaseWithNotice)
            {
                BackendName = "Prueba (4b)", ExactInstant = true, MotionMayRun = true,
            };
            aux01.Inspect = (h, image, values) => OutlineToast(h, image, values);
            all.Add(aux01);

            var aux02 = Ble("AUX-10a-02", "AUX-10a-02-se-despierta", DeviceProfile.TV, "Apagado → encendido: cuadros 0, 240, 480 y 720 ms", Wake,
                initial: BleAdapterState.Off);
            aux02.Kind = SnapshotKind.Sequence;
            aux02.ExactInstant = true;
            aux02.MotionMayRun = true;
            aux02.OverflowMayRun = true;
            aux02.Shot("AUX-10a-02-0", "AUX-10a-02a-0ms", "0 ms: la vista de estado")
                .Shot("AUX-10a-02-240", "AUX-10a-02b-240ms", "240 ms")
                .Shot("AUX-10a-02-480", "AUX-10a-02c-480ms", "480 ms")
                .Shot("AUX-10a-02-720", "AUX-10a-02d-720ms", "720 ms: la búsqueda");
            all.Add(aux02);
        }

        // ── Guiones ──

        private static IEnumerator Rest(ScenarioScript s)
        {
            yield return s.Settle();
        }

        private static IEnumerator Tap(ScenarioScript s, string element)
        {
            yield return s.Settle();
            yield return s.Tap(element);
            yield return s.Settle();
        }

        // Buscar y quedarse en el instante t (s) desde el toque.
        private static IEnumerator SearchFor(ScenarioScript s, double t)
        {
            yield return s.Settle();
            s.Host.Tap("device-search");
            yield return s.Frames(2);
            s.Host.Advance(t);
            yield return s.Frames(2);
        }

        private static IEnumerator SearchDone(ScenarioScript s, params FakeBleDevice[] devices)
        {
            yield return s.Settle();
            s.Host.Tap("device-search");
            foreach (var device in devices)
            {
                s.Host.FakeBle!.Adapter.Advertise(device);
            }

            yield return s.Frames(2);
            s.Host.Advance(10.1);
            yield return s.Settle();
        }

        // La lista con AMS-670B, el anónimo y la bici; tocar AMS-670B y quedarse en el instante t (s) desde el toque.
        private static IEnumerator TapRowAt(ScenarioScript s, double t)
        {
            yield return SearchDone(s, FakeBleDevices.Row, FakeBleDevices.Anon, FakeBleDevices.Bike);
            s.Host.Tap(ScanResultCard.NamePrefix + FakeBleDevices.RowId);
            yield return s.Frames(2);
            s.Host.Advance(t);
            yield return s.Frames(2);
        }

        private static IEnumerator Connect(ScenarioScript s, FakeBleDevice device)
        {
            yield return SearchDone(s, device, FakeBleDevices.Anon);
            s.Host.Tap(ScanResultCard.NamePrefix + device.Id);
            yield return s.Frames(2);
            s.Host.Advance(1);
            yield return s.Settle();
        }

        private static IEnumerator ConnectedWithPacket(ScenarioScript s)
        {
            yield return Connect(s, FakeBleDevices.Row);
            s.Host.FakeBle!.Adapter.Notify(FakeBleDevices.RowId, FtmsUuids.RowerData, FakeBleDevices.RowerData);
            yield return s.Settle();
        }

        private static IEnumerator Reconnecting(ScenarioScript s, double t)
        {
            yield return Connect(s, FakeBleDevices.Row);
            var adapter = s.Host.FakeBle!.Adapter;
            adapter.NeverRespond(FakeBleDevices.RowId);
            adapter.DropLink(FakeBleDevices.RowId);
            yield return s.Frames(2);
            s.Host.Advance(t);
            yield return s.Frames(2);
        }

        // Libre con un paquete por segundo y la caída; con reconnect, vuelve a los 3 s.
        private static IEnumerator HudReconnecting(ScenarioScript s, bool reconnect)
        {
            yield return Connect(s, FakeBleDevices.Row);
            var adapter = s.Host.FakeBle!.Adapter;
            adapter.NotifyEvery(FakeBleDevices.RowId, TimeSpan.FromSeconds(1), _ => FakeBleDevices.RowerData);
            yield return s.StartFree();
            yield return s.Advance(5);
            adapter.NeverRespond(FakeBleDevices.RowId);
            adapter.DropLink(FakeBleDevices.RowId);
            yield return s.Settle();
            if (reconnect)
            {
                adapter.Respond(FakeBleDevices.RowId);
                s.Host.Advance(3.05);
                yield return s.Settle();
            }
        }

        private static IEnumerator MonitorStoppedDevice(ScenarioScript s)
        {
            yield return Connect(s, FakeBleDevices.Row);
            s.Host.FakeBle!.Adapter.Notify(FakeBleDevices.RowId, FtmsUuids.RowerData, FakeBleDevices.RowerData);
            s.Host.Advance(6.1);
            yield return s.Settle();
        }

        private static IEnumerator MonitorStoppedHud(ScenarioScript s)
        {
            yield return Connect(s, FakeBleDevices.Row);
            s.Host.FakeBle!.Adapter.Notify(FakeBleDevices.RowId, FtmsUuids.RowerData, FakeBleDevices.RowerData);
            yield return s.StartFree();
            s.Host.Advance(6.1);
            yield return s.Settle();
        }

        private static IEnumerator OpenDebug(ScenarioScript s, FakeBleDevice device, bool packets)
        {
            yield return Connect(s, device);
            if (packets)
            {
                s.Host.FakeBle!.Adapter.NotifyEvery(device.Id, TimeSpan.FromSeconds(1), _ => FakeBleDevices.RowerData);
                s.Host.Advance(3);
                yield return s.Frames(2);
            }

            s.Host.PointerDown("device-header");
            s.Host.Advance(0.55);
            yield return s.Frames(2);
            s.Host.PointerUp("device-header");
            yield return s.Frames(2);
            yield return s.Do(h => h.ScrollTo("ble-debug-panel"));
        }

        private static IEnumerator WorstCaseWithNotice(ScenarioScript s)
        {
            yield return s.StartRoutine("Presupuesto 4b");
            yield return s.AdvanceTo(5.5);
            yield return s.Do(h => h.Shell.Toasts.ShowLink(LinkNoticeText.Toast(new LinkNotice(LinkNoticeKind.Reconnecting, "AMS-670B"), h.App.Texts.Value)));
            yield return s.Frames(2);
        }

        private static IEnumerator Wake(ScenarioScript s)
        {
            yield return s.Settle();
            s.Host.Advance(1);
            yield return s.Frames(2);
            s.Host.FakeBle!.Adapter.SetState(BleAdapterState.On);
            yield return s.Frames(2);
            yield return s.Shot("AUX-10a-02-0");
            foreach (var (ms, id) in new[] { (240, "AUX-10a-02-240"), (480, "AUX-10a-02-480"), (720, "AUX-10a-02-720") })
            {
                // De a cuadros de 1/60 s, como la app (el ViewSwap avanza con el MotionDriver de cada Tick).
                var target = s.Host.Scheduler.MonotonicSeconds + 0.24;
                while (s.Host.Scheduler.MonotonicSeconds < target - 1e-9)
                {
                    s.Host.Step();
                    yield return null;
                }

                yield return s.Frames(2);
                yield return s.Shot(id, h => ms < 720 || h.IsShown("device-scan-view") ? new string[0] : new[] { "a los 720 ms no se ve la búsqueda" });
            }
        }

        // El contorno de depuración del aviso (dev.simulator, como el de la zona del bote) y la prueba de que no tapa ninguna métrica.
        private static IEnumerable<string> OutlineToast(UiTestHost h, Texture2D image, List<string> values)
        {
            var problems = new List<string>();
            var toast = h.Shell.Toasts.Toast.worldBound;
            var color = (Color)RowMate.Presentation.Controls.TokenColors.ToColor(RowMate.Core.Design.Tokens.DevSimulator);
            var d = h.Profile.Density;
            var x0 = (int)Math.Round(toast.xMin * d);
            var x1 = (int)Math.Round(toast.xMax * d) - 1;
            var y0 = (int)Math.Round(toast.yMin * d);
            var y1 = (int)Math.Round(toast.yMax * d) - 1;
            var stroke = Math.Max(1, (int)Math.Round(d));
            for (var k = 0; k < stroke; k++)
            {
                for (var x = x0; x <= x1; x++)
                {
                    Set(image, x, y0 + k, color);
                    Set(image, x, y1 - k, color);
                }

                for (var y = y0; y <= y1; y++)
                {
                    Set(image, x0 + k, y, color);
                    Set(image, x1 - k, y, color);
                }
            }

            image.Apply(false);
            values.Add("aviso: " + R(toast));
            foreach (var metric in new[] { "hud-spm", "hud-split", "hud-watts", "hud-bpm", "hud-kcal", "hud-band" })
            {
                var element = h.Find(metric);
                if (element == null || !h.IsShown(metric))
                {
                    continue;
                }

                var b = element.worldBound;
                if (toast.xMin < b.xMax - 0.01f && b.xMin < toast.xMax - 0.01f && toast.yMin < b.yMax - 0.01f && b.yMin < toast.yMax - 0.01f)
                {
                    problems.Add("el aviso tapa " + metric + " " + R(b));
                }
            }

            return problems;
        }

        // ── Ayudas ──

        private static SnapshotScenario Ble(string id, string stem, DeviceProfile profile, string state, Func<ScenarioScript, IEnumerator>? arrange,
                                            Action<FakeBleAdapter>? setup = null, BleAdapterState initial = BleAdapterState.On, string language = "es-AR")
        {
            var options = new UiHostOptions
            {
                Profile = profile,
                IsDesktop = profile.Code == DeviceProfile.W1280.Code,
                Backend = HostBackends.FakeBle(setup, initial),
                LanguageTags = new[] { language },
                StartUtc = UiHostOptions.WorkoutStartUtc,
            };
            return new SnapshotScenario(id, stem, options, state, arrange) { BackendName = Backend, SkipReadingWindow = true };
        }

        private static SnapshotScenario Expecting(SnapshotScenario scenario, params (string What, Func<UiTestHost, bool> Ok)[] checks)
        {
            scenario.Checks = h =>
            {
                var problems = new List<string>();
                foreach (var check in checks)
                {
                    bool ok;
                    try
                    {
                        ok = check.Ok(h);
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

                return problems;
            };
            return scenario;
        }

        private static ScanResultCard Card(UiTestHost h, string id)
        {
            return h.DeviceScreen!.Scan.Results.CardFor(id) ?? throw new InvalidOperationException("no hay tarjeta " + id);
        }

        private static string Names(UiTestHost h)
        {
            var list = h.DeviceScreen!.Scan.Results;
            var names = new List<string>();
            for (var i = 0; i < list.Count; i++)
            {
                names.Add(list.CardAt(i)!.NameLabel.text);
            }

            return string.Join("|", names);
        }

        private static void Set(Texture2D image, int x, int yTop, Color color)
        {
            if (x >= 0 && yTop >= 0 && x < image.width && yTop < image.height)
            {
                image.SetPixel(x, image.height - 1 - yTop, color);
            }
        }

        private static string R(Rect r)
        {
            return "(" + r.xMin.ToString("0.#", CultureInfo.InvariantCulture) + ", " + r.yMin.ToString("0.#", CultureInfo.InvariantCulture) + ", "
                   + r.width.ToString("0.#", CultureInfo.InvariantCulture) + " × " + r.height.ToString("0.#", CultureInfo.InvariantCulture) + ")";
        }
    }
}
