using System;
using System.Collections;
using System.Collections.Generic;
using RowMate.App.Device;
using RowMate.Core.Ble;
using RowMate.Core.Common;
using RowMate.Presentation.Controls;
using RowMate.Tests.Support;
using RowMate.Tests.UiSupport;
using UnityEngine.UIElements;

namespace RowMate.Snapshots
{
    // Catálogo it11 (11-ble-pulsometro/01-po-spec §7 → 02-tech-design §7): CAP-01 a -16 en los tamaños de la spec (a/b/c cuando la spec
    // pide más de uno). Cada toma arma su UiTestHost con el remo Y el pulsómetro REALES (RowerService y HeartRateService, Core) sobre el
    // adaptador FALSO con los equipos ficticios K-11 (HostBackends.FakeBle con una MemoryPreferences propia, sembrada por el guion para
    // CAP-07, -08 y -12), el reloj de prueba, ROWMATE_DATA_DIR temporal y ROWMATE_BLE=off (capture-snapshots.ps1): ninguna toma toca la
    // radio, el prefs.json ni las bases del usuario, ni muestra un equipo real. Los instantes de la spec van con ExactInstant.
    // El arte fino (corazón y corazón roto del aviso, respiración, seguir a la tarjeta) es de WP-D: hasta entonces el aviso usa el check y
    // link_off de la 10a.
    public static class HrmCatalog
    {
        public const string Backend = "Remo y pulsómetro reales + adaptador falso (K-11)";

        private const string HrmName = "Banda de prueba";
        private static readonly string HrmRow = "hrm-device-" + FakeBleDevices.HrmId;

        public static IReadOnlyList<SnapshotScenario> All
        {
            get
            {
                var all = new List<SnapshotScenario>();
                AddSearch(all);
                AddConnect(all);
                AddMemory(all);
                AddErrors(all);
                AddNotices(all);
                AddHud(all);
                AddCoexist(all);
                all.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
                return all;
            }
        }

        // ── CAP-01 a -03: sin sensor y la búsqueda ──

        private static void AddSearch(List<SnapshotScenario> all)
        {
            foreach (var (id, stem, profile) in new[] { ("CAP-01a", "CAP-01a-sin-sensor-tv", DeviceProfile.TV), ("CAP-01b", "CAP-01b-sin-sensor-w1280", DeviceProfile.W1280) })
            {
                all.Add(Expecting(Hrm(id, stem, profile, "Sin memoria; remo desconectado; búsqueda en reposo", Rest),
                    ("Sin sensor", h => h.TextOf("hrm-nosensor-text") == "Sin sensor"), ("Buscar habilitado", h => h.Find("hrm-search")!.enabledSelf),
                    ("sin desborde", h => h.Audit().IsClean)));
            }

            all.Add(Searching("CAP-02a", "CAP-02a-buscando-tv", DeviceProfile.TV, DeviceScanLayoutKind.Stacked));
            all.Add(Searching("CAP-02b", "CAP-02b-buscando-w1280", DeviceProfile.W1280, DeviceScanLayoutKind.Stacked));
            all.Add(Searching("CAP-03a", "CAP-03a-lista-l640", DeviceProfile.L640, DeviceScanLayoutKind.SingleScroll));
            all.Add(Searching("CAP-03b", "CAP-03b-lista-th", DeviceProfile.TH, DeviceScanLayoutKind.TwoColumns));
        }

        private static SnapshotScenario Searching(string id, string stem, DeviceProfile profile, DeviceScanLayoutKind layout)
        {
            var scenario = Expecting(Hrm(id, stem, profile, "Buscar; anuncios Hp, Hrm, Row, Watch, HrAnon, Hrm cada 300 ms; t = 3 s (" + layout + ")",
                    s => SearchAt(s, 3)),
                ("Buscando sensores...", h => h.TextOf("hrm-searching-text") == "Buscando sensores..."),
                ("las 3 filas en orden", h => Rows(h) == "Banda de prueba|Reloj de prueba|Sensor desconocido"),
                ("sin los auriculares ni el remo", h => !h.HasText("Auriculares") && !h.HasText("AMS-670B")),
                ("Buscar dispositivos deshabilitado", h => !h.Find("device-search")!.enabledSelf),
                ("disposición " + layout, h => h.DeviceScreen!.Scan.Layout.Kind == layout),
                ("sin desborde", h => h.Audit().IsClean));
            scenario.ExactInstant = true;
            return scenario;
        }

        // ── CAP-04 a -06: la conexión manual ──

        private static void AddConnect(List<SnapshotScenario> all)
        {
            var cap04 = Expecting(Hrm("CAP-04", "CAP-04-conectando-tv", DeviceProfile.TV, "Buscar + tocar Banda de prueba (NeverRespond); t = 1 s",
                    s => TapHrmAt(s, 1), a => a.NeverRespond(FakeBleDevices.HrmId)),
                ("Conectando a Banda de prueba...", h => h.TextOf("hrm-connecting-text") == "Conectando a Banda de prueba..."),
                ("sin botones", h => h.HeartRateCard!.Query<Button>().ToList().Count == 0));
            cap04.ExactInstant = true;
            all.Add(cap04);

            all.Add(Connected("CAP-05a", "CAP-05a-conectado-tv", DeviceProfile.TV, "es-AR"));
            all.Add(Connected("CAP-05b", "CAP-05b-conectado-w1280", DeviceProfile.W1280, "es-AR"));
            all.Add(Connected("CAP-05c", "CAP-05c-conectado-tv-en", DeviceProfile.TV, "en-US"));

            all.Add(Expecting(Hrm("CAP-06", "CAP-06-sin-senal-tv", DeviceProfile.TV, "Conectado con 142 y 10 s sin lecturas (watchdog)", s => Stale(s)),
                ("--", h => h.TextOf("hrm-bpm") == "--"), ("Sin señal del sensor", h => h.IsShown("hrm-stale") && h.TextOf("hrm-stale") == "Sin señal del sensor"),
                ("corazón al 40 %", h => !h.HeartRateCard!.Model!.HeartFull), ("sigue conectado", h => h.App.HeartRate.State.Value.IsConnected)));
        }

        private static SnapshotScenario Connected(string id, string stem, DeviceProfile profile, string language)
        {
            var en = language != "es-AR";
            return Expecting(Hrm(id, stem, profile, "Buscar + tocar Banda de prueba (Respond) + Contact142; + 240 ms y asentado", ConnectedWith142, language: language),
                ("142", h => h.TextOf("hrm-bpm") == "142"), ("Banda de prueba", h => h.TextOf("hrm-connected-name") == HrmName),
                (en ? "Disconnect" : "Desconectar", h => h.HasText(en ? "Disconnect" : "Desconectar")), ("sin la línea sin señal", h => !h.IsShown("hrm-stale")));
        }

        // ── CAP-07 y -08: el recordado al abrir ──

        private static void AddMemory(List<SnapshotScenario> all)
        {
            all.Add(Reconnecting("CAP-07a", "CAP-07a-reconectando-tv", "es-AR"));
            all.Add(Reconnecting("CAP-07b", "CAP-07b-reconectando-tv-en", "en-US"));

            foreach (var (id, stem, profile) in new[] { ("CAP-08a", "CAP-08a-no-encontrado-tv", DeviceProfile.TV), ("CAP-08b", "CAP-08b-no-encontrado-th", DeviceProfile.TH) })
            {
                var cap = Expecting(Hrm(id, stem, profile, "Memoria Banda de prueba, Fail en cada intento; t = 51 s (6 intentos)", s => AtSecond(s, 51),
                        FailHrm, remembered: true),
                    ("Banda de prueba · no encontrado", h => h.TextOf("hrm-remembered-name") + h.TextOf("hrm-remembered-suffix") == "Banda de prueba · no encontrado"),
                    ("Conectar habilitado", h => h.Find("hrm-retry")!.enabledSelf), ("link_off", h => h.HeartRateCard!.Indicator!.Current == LinkIndicator.Mode.LinkOff),
                    ("6 intentos", h => h.FakeBle!.Adapter.ConnectCalls(FakeBleDevices.HrmId) == 6), ("sin desborde", h => h.Audit().IsClean));
                cap.ExactInstant = true;
                all.Add(cap);
            }
        }

        private static SnapshotScenario Reconnecting(string id, string stem, string language)
        {
            var en = language != "es-AR";
            var line = en ? "Banda de prueba · reconnecting..." : "Banda de prueba · reconectando...";
            var cap = Expecting(Hrm(id, stem, DeviceProfile.TV, "Memoria Banda de prueba, Fail en cada intento; al abrir, t = 5 s (arco a la mitad de 10 s)",
                    s => AtSecond(s, 5), FailHrm, remembered: true, language: language),
                (line, h => h.TextOf("hrm-remembered-name") + h.TextOf("hrm-remembered-suffix") == line),
                ("Conectar deshabilitado", h => !h.Find("hrm-retry")!.enabledSelf), ("Buscar otro y Olvidar", h => h.Find("hrm-search")!.enabledSelf && h.Find("hrm-forget")!.enabledSelf),
                ("el arco a la mitad", h => Math.Abs(h.HeartRateCard!.Indicator!.Fraction - 0.5) < 0.02));
            cap.ExactInstant = true;
            return cap;
        }

        // ── CAP-09 a -11: los errores ──

        private static void AddErrors(List<SnapshotScenario> all)
        {
            all.Add(Expecting(Hrm("CAP-09", "CAP-09-incompatible-tv", DeviceProfile.TV, "Buscar + tocar Sensor sin pulso (sin 0x2A37)", s => TapRow(s, FakeBleDevices.NoHr)),
                ("incompatible", h => h.TextOf("hrm-error-human") == "No es un pulsómetro compatible (sin servicio Heart Rate)"),
                ("sin detalle", h => !h.IsShown("hrm-error-detail")), ("no se recuerda", h => h.App.HeartRate.State.Value.RememberedDeviceName == null)));
            all.Add(Expecting(Hrm("CAP-10", "CAP-10-error-conectar-tv", DeviceProfile.TV, "Buscar + tocar Banda de prueba con Fail(BleException(\"connect\", \"gatt error 133\"))",
                    s => TapRow(s, FakeBleDevices.Hrm), a => a.Fail(FakeBleDevices.HrmId, new BleException("connect", "gatt error 133", 133))),
                ("No se pudo conectar con Banda de prueba", h => h.TextOf("hrm-error-human") == "No se pudo conectar con Banda de prueba"),
                ("detalle", h => (h.TextOf("hrm-error-detail") ?? "").Contains("gatt error 133"))));
            all.Add(Expecting(Hrm("CAP-11", "CAP-11-error-buscar-tv", DeviceProfile.TV, "FailNextScan(BleException(\"scan\", \"error 2\")) + Buscar",
                    s => SearchThenSettle(s), a => a.FailNextScan(new BleException("scan", "error 2", 2))),
                ("No se pudo buscar sensores", h => h.TextOf("hrm-error-human") == "No se pudo buscar sensores"),
                ("detalle", h => (h.TextOf("hrm-error-detail") ?? "").Contains("error 2")), ("Sin sensor", h => h.TextOf("hrm-nosensor-text") == "Sin sensor")));
        }

        // ── CAP-12: Pulsómetro conectado (Dispositivo y HUD) ──

        private static void AddNotices(List<SnapshotScenario> all)
        {
            var cap12a = Expecting(Hrm("CAP-12a", "CAP-12a-aviso-conectado-tv", DeviceProfile.TV, "Memoria Banda de prueba, RespondAfter(2 s) al abrir; aviso + 650 ms (check de 240 y el cambio de cuerpo de 360 terminados)",
                    s => AtSecond(s, 2.65), a =>
                    {
                        a.Advertise(FakeBleDevices.Hrm);
                        a.RespondAfter(FakeBleDevices.HrmId, TimeSpan.FromSeconds(2));
                    }, remembered: true),
                ("Pulsómetro conectado", h => h.Shell.Toasts.CurrentText == "Pulsómetro conectado"),
                ("del pulsómetro, no del remo", h => h.Shell.Toasts.CurrentHrm != null && h.Shell.Toasts.CurrentLink == null));
            cap12a.ExactInstant = true;
            cap12a.MotionMayRun = true;
            all.Add(cap12a);

            var cap12b = Expecting(Hrm("CAP-12b", "CAP-12b-hud-aviso-conectado-w1280", DeviceProfile.W1280,
                    "Libre con Banda de prueba conectada; DropLink + RespondAfter(1 s): Pulsómetro conectado + 300 ms", HudReconnected),
                ("Pulsómetro conectado", h => h.Shell.Toasts.CurrentText == "Pulsómetro conectado"), ("sobre el HUD", h => h.Immersive != null));
            cap12b.ExactInstant = true;
            cap12b.MotionMayRun = true;
            all.Add(cap12b);
        }

        // ── CAP-13 y -14: el HUD ──

        private static void AddHud(List<SnapshotScenario> all)
        {
            foreach (var (id, stem, profile) in new[]
                     {
                         ("CAP-13a", "CAP-13a-hud-pulso-sensor-w1280", DeviceProfile.W1280), ("CAP-13b", "CAP-13b-hud-pulso-sensor-tv", DeviceProfile.TV),
                         ("CAP-13c", "CAP-13c-hud-pulso-sensor-th", DeviceProfile.TH),
                     })
            {
                var cap = Expecting(Hrm(id, stem, profile, "AMS-670B con RowerDataHr90 (90 bpm FTMS) + Banda de prueba con 142; libre a los 20 s", s => HudWithSensor(s, true)),
                    ("142 en el HUD, no 90", h => h.HudState != null && h.HudState.BpmValue == 142), ("la tarjeta BPM", h => h.IsShown("hud-bpm")));
                cap.ExactInstant = true;
                cap.MotionMayRun = true;
                all.Add(cap);
            }

            foreach (var (id, stem, profile) in new[] { ("CAP-14a", "CAP-14a-hud-pulsometro-perdido-w1280", DeviceProfile.W1280), ("CAP-14b", "CAP-14b-hud-pulsometro-perdido-tv", DeviceProfile.TV) })
            {
                var cap = Expecting(Hrm(id, stem, profile, "AMS-670B sin pulso FTMS + Banda de prueba con 142; libre a los 20 s; DropLink(Banda) NeverRespond; + 500 ms",
                        HudLost),
                    ("Se perdió el pulsómetro, reconectando…", h => h.Shell.Toasts.CurrentText == "Se perdió el pulsómetro, reconectando…"),
                    ("la sesión sigue", h => h.App.Workout.State.Value.Phase == RowMate.Core.Workout.WorkoutPhase.Active));
                cap.ExactInstant = true;
                cap.MotionMayRun = true;
                all.Add(cap);
            }
        }

        // ── CAP-15 y -16: remo y pulsómetro juntos ──

        private static void AddCoexist(List<SnapshotScenario> all)
        {
            all.Add(Expecting(Hrm("CAP-15a", "CAP-15a-remo-y-pulsometro-tv", DeviceProfile.TV, "AMS-670B y Banda de prueba conectados; vista conectada", s => BothConnected(s, false)),
                ("AMS-670B", h => h.TextOf("device-name") == "AMS-670B"), ("la tarjeta bajo la grilla", h => h.Find("hrm-card-slot-connected")!.Contains(h.Find("hrm-card")!)),
                ("142", h => h.TextOf("hrm-bpm") == "142")));
            all.Add(Expecting(Hrm("CAP-15b", "CAP-15b-remo-y-pulsometro-w1280", DeviceProfile.W1280, "AMS-670B y Banda de prueba conectados; desplazada hasta la tarjeta",
                    s => BothConnected(s, true)),
                ("AMS-670B", h => h.TextOf("device-name") == "AMS-670B"), ("la tarjeta a la vista", h => h.WhyNotTappable("hrm-disconnect") == null)));

            var cap16 = Expecting(Hrm("CAP-16", "CAP-16-remo-buscando-tv", DeviceProfile.TV, "Buscar dispositivos con anuncios del remo; t = 2 s", s => RowerSearchAt(s, 2)),
                ("Buscar del pulsómetro deshabilitado", h => !h.Find("hrm-search")!.enabledSelf), ("el remo busca", h => h.App.Device.State.Value.Status == BleStatus.Scanning));
            cap16.ExactInstant = true;
            all.Add(cap16);
        }

        // ── Guiones ──

        private static void FailHrm(FakeBleAdapter a)
        {
            a.Advertise(FakeBleDevices.Hrm);
            a.Fail(FakeBleDevices.HrmId, new BleException("connect", "device not found"));
        }

        private static IEnumerator Rest(ScenarioScript s)
        {
            yield return s.Settle();
        }

        // Hasta el instante t (s) desde que se abrió la app (AppContext.Start corre en el instante 0 del reloj de prueba).
        private static IEnumerator AtSecond(ScenarioScript s, double t)
        {
            var now = s.Host.Scheduler.MonotonicSeconds;
            if (t > now)
            {
                s.Host.Advance(t - now);
            }

            yield return s.Frames(2);
        }

        // Buscar y anunciar Hp, Hrm, Row, Watch, HrAnon y Hrm cada 300 ms; quedarse en el instante t (s) desde el toque.
        private static IEnumerator SearchAt(ScenarioScript s, double t)
        {
            yield return s.Settle();
            s.Host.ScrollTo("hrm-search");
            yield return s.Frames(2);
            s.Host.Tap("hrm-search");
            var start = s.Host.Scheduler.MonotonicSeconds;
            var adapter = s.Host.FakeBle!.Adapter;
            var devices = new[] { FakeBleDevices.Hp, FakeBleDevices.Hrm, FakeBleDevices.Row, FakeBleDevices.Watch, FakeBleDevices.HrAnon, FakeBleDevices.Hrm };
            for (var i = 0; i < devices.Length; i++)
            {
                adapter.AdvertiseAt(TimeSpan.FromMilliseconds(300 * (i + 1)), devices[i]);
            }

            // De a cuadros de 1/60 s, como la app (las filas entran con su animación).
            while (s.Host.Scheduler.MonotonicSeconds < start + t - 1e-9)
            {
                s.Host.Advance(Math.Min(UiTestHost.FrameSeconds, start + t - s.Host.Scheduler.MonotonicSeconds));
                yield return null;
            }

            // Con un único desplazamiento (L640), la lista del pulsómetro a la vista (la última fila), como lo hace el usuario.
            if (s.Host.DeviceScreen!.Scan.Layout.Kind == DeviceScanLayoutKind.SingleScroll)
            {
                s.Host.ScrollTo("hrm-device-" + FakeBleDevices.HrAnonId);
            }

            yield return s.Frames(2);
        }

        private static IEnumerator SearchList(ScenarioScript s, params FakeBleDevice[] devices)
        {
            yield return s.Settle();
            s.Host.ScrollTo("hrm-search");
            yield return s.Frames(2);
            s.Host.Tap("hrm-search");
            foreach (var device in devices)
            {
                s.Host.FakeBle!.Adapter.Advertise(device);
            }

            yield return s.Settle();
        }

        private static IEnumerator SearchThenSettle(ScenarioScript s)
        {
            yield return s.Settle();
            s.Host.ScrollTo("hrm-search");
            yield return s.Frames(2);
            s.Host.Tap("hrm-search");
            yield return s.Settle();
        }

        private static IEnumerator TapHrmAt(ScenarioScript s, double t)
        {
            yield return SearchList(s, FakeBleDevices.Hrm, FakeBleDevices.Watch, FakeBleDevices.HrAnon);
            s.Host.ScrollTo(HrmRow);
            yield return s.Frames(2);
            s.Host.Tap(HrmRow);
            yield return s.Frames(2);
            s.Host.Advance(t);
            yield return s.Frames(2);
        }

        private static IEnumerator TapRow(ScenarioScript s, FakeBleDevice device)
        {
            yield return SearchList(s, FakeBleDevices.Hrm, device == FakeBleDevices.Hrm ? FakeBleDevices.Watch : device);
            var row = "hrm-device-" + device.Id;
            s.Host.ScrollTo(row);
            yield return s.Frames(2);
            s.Host.Tap(row);
            yield return s.Settle();
            s.Host.ScrollTo("hrm-card");
            yield return s.Settle();
        }

        private static IEnumerator ConnectedWith142(ScenarioScript s)
        {
            yield return TapRow(s, FakeBleDevices.Hrm);
            s.Host.FakeBle!.Adapter.Notify(FakeBleDevices.HrmId, HeartRateParser.Measurement, HrPackets.Contact142);
            yield return s.Settle();
        }

        private static IEnumerator Stale(ScenarioScript s)
        {
            yield return ConnectedWith142(s);
            s.Host.Advance(10.1);
            yield return s.Settle();
        }

        // El remo por la app (buscar, anunciar y conectar) y el pulsómetro por la app (buscar, anunciar y conectar), sin pasar por la UI.
        private static IEnumerator ConnectBoth(ScenarioScript s, bool ftmsHr, bool hrmPackets)
        {
            yield return s.Settle();
            var h = s.Host;
            var adapter = h.FakeBle!.Adapter;
            _ = h.App.Device.StartScanAsync();
            adapter.Advertise(FakeBleDevices.Row);
            yield return s.Frames(2);
            _ = h.App.Device.ConnectAsync(FakeBleDevices.RowId);
            h.Advance(1);
            yield return s.Frames(2);
            adapter.NotifyEvery(FakeBleDevices.RowId, TimeSpan.FromSeconds(1), _ => ftmsHr ? FakeBleDevices.RowerDataHr90 : FakeBleDevices.RowerData);
            _ = h.App.HeartRate.StartScanAsync();
            adapter.Advertise(FakeBleDevices.Hrm);
            yield return s.Frames(2);
            _ = h.App.HeartRate.ConnectAsync(FakeBleDevices.HrmId, HrmName);
            yield return s.Settle();
            if (hrmPackets)
            {
                adapter.NotifyEvery(FakeBleDevices.HrmId, TimeSpan.FromSeconds(1), _ => HrPackets.Contact142, HeartRateParser.Measurement);
            }
        }

        private static IEnumerator HudWithSensor(ScenarioScript s, bool ftmsHr)
        {
            yield return ConnectBoth(s, ftmsHr, true);
            yield return s.StartFree();
            yield return s.AdvanceTo(20);
        }

        private static IEnumerator HudLost(ScenarioScript s)
        {
            yield return HudWithSensor(s, false);
            var adapter = s.Host.FakeBle!.Adapter;
            adapter.NeverRespond(FakeBleDevices.HrmId);
            adapter.DropLink(FakeBleDevices.HrmId);
            yield return s.AdvanceTo(20.5);
        }

        private static IEnumerator HudReconnected(ScenarioScript s)
        {
            yield return HudWithSensor(s, false);
            var adapter = s.Host.FakeBle!.Adapter;
            adapter.RespondAfter(FakeBleDevices.HrmId, TimeSpan.FromSeconds(1));
            adapter.DropLink(FakeBleDevices.HrmId);
            yield return s.AdvanceTo(21.3);
        }

        private static IEnumerator BothConnected(ScenarioScript s, bool scrollToCard)
        {
            yield return ConnectBoth(s, false, false);
            s.Host.FakeBle!.Adapter.Notify(FakeBleDevices.RowId, FtmsUuids.RowerData, FakeBleDevices.RowerData);
            s.Host.FakeBle.Adapter.Notify(FakeBleDevices.HrmId, HeartRateParser.Measurement, HrPackets.Contact142);
            yield return s.Settle();
            if (scrollToCard)
            {
                yield return s.ScrollTo("hrm-card");
            }
        }

        private static IEnumerator RowerSearchAt(ScenarioScript s, double t)
        {
            yield return s.Settle();
            s.Host.Tap("device-search");
            var start = s.Host.Scheduler.MonotonicSeconds;
            var adapter = s.Host.FakeBle!.Adapter;
            adapter.AdvertiseAt(TimeSpan.FromMilliseconds(500), FakeBleDevices.Row);
            adapter.AdvertiseAt(TimeSpan.FromMilliseconds(900), FakeBleDevices.Hrm);
            adapter.AdvertiseAt(TimeSpan.FromMilliseconds(1300), FakeBleDevices.Anon);
            while (s.Host.Scheduler.MonotonicSeconds < start + t - 1e-9)
            {
                s.Host.Advance(Math.Min(UiTestHost.FrameSeconds, start + t - s.Host.Scheduler.MonotonicSeconds));
                yield return null;
            }

            yield return s.Frames(2);
        }

        // ── Ayudas ──

        private static SnapshotScenario Hrm(string id, string stem, DeviceProfile profile, string state, Func<ScenarioScript, IEnumerator>? arrange,
                                            Action<FakeBleAdapter>? setup = null, bool remembered = false, string language = "es-AR")
        {
            var prefs = new MemoryPreferences();
            if (remembered)
            {
                prefs.SetString(HeartRateService.PrefDeviceId, FakeBleDevices.HrmId);
                prefs.SetString(HeartRateService.PrefDeviceName, HrmName);
            }

            var options = new UiHostOptions
            {
                Profile = profile,
                IsDesktop = profile.Code == DeviceProfile.W1280.Code,
                LanguageTags = new[] { language },
                StartUtc = UiHostOptions.WorkoutStartUtc,
            }.WithRealHrm(prefs, setup);
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

        private static string Rows(UiTestHost h)
        {
            var model = h.HeartRateCard?.Model;
            if (model == null)
            {
                return "";
            }

            var names = new List<string>();
            for (var i = 0; i < model.Rows.Count; i++)
            {
                names.Add(model.Rows[i].Title);
            }

            return string.Join("|", names);
        }
    }
}
