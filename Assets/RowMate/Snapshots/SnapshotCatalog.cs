using System;
using System.Collections;
using System.Collections.Generic;
using RowMate.App.Device;
using RowMate.Core.Ble;
using RowMate.Core.Design;
using RowMate.Core.Models;
using RowMate.Presentation.Device;
using RowMate.Tests.Support;
using RowMate.Tests.UiSupport;
using UnityEngine;
using UnityEngine.UIElements;

namespace RowMate.Snapshots
{
    // Catálogo de capturas y videos del PO (01-po-spec §7.3) → escenarios (02-tech-design §4.3). "Sim" = simulador sin ruido
    // con la capa completa; "Prueba" = remo y pulsómetro de prueba con cinta y llave sin panel (QA-G2-05). Reloj de prueba a
    // las 13:45 hora local. Las sondas de aspecto del Designer (04-design-notes §7.2) se suman en WP-D.
    // Sin ROWMATE_SIMULATOR solo existen CAP-22 y CAP-23.
    public static class SnapshotCatalog
    {
        public const string HrmRow = "hrm-device-SIM:HRM";

        // El catálogo de un conjunto (03-rutinas/02-tech-design §5.1; 04a-motor-hud-funcional/02-tech-design §7.1): it2 = este,
        // it3 = RoutinesCatalog, it4a (por defecto desde la 4a) = WorkoutCatalog.
        public static IReadOnlyList<SnapshotScenario> For(string set)
        {
            if (string.Equals(set, "it2", StringComparison.OrdinalIgnoreCase))
            {
                return All;
            }

            if (string.Equals(set, "it3", StringComparison.OrdinalIgnoreCase))
            {
                return RoutinesCatalog.All;
            }

            // It. 4b: it4b (por defecto) = HudExrCatalog; it4a = WorkoutCatalog (con las tomas del HUD reemplazadas).
            return string.Equals(set, "it4a", StringComparison.OrdinalIgnoreCase) ? WorkoutCatalog.All : HudExrCatalog.All;
        }

        public static string TitleFor(string set)
        {
            if (string.Equals(set, "it2", StringComparison.OrdinalIgnoreCase))
            {
                return "It. 2";
            }

            if (string.Equals(set, "it3", StringComparison.OrdinalIgnoreCase))
            {
                return "It. 3 (Rutinas)";
            }

            return string.Equals(set, "it4a", StringComparison.OrdinalIgnoreCase) ? "It. 4a (Entrenar)" : "It. 4b (HUD EXR)";
        }

        public static IReadOnlyList<SnapshotScenario> All
        {
            get
            {
                var all = new List<SnapshotScenario>();
#if ROWMATE_SIMULATOR
                AddSimulatorScenarios(all);
#endif
                all.Add(Cap22());
                all.Add(new SnapshotScenario("CAP-23", "CAP-23-medicion-rt1", new UiHostOptions { Profile = DeviceProfile.Dp(400, 240) },
                    "Muestra de medición de R-T1 (AC-BASE-05): las dos composiciones lado a lado", null) { Kind = SnapshotKind.Rt1, BackendName = "—" });
                all.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
                return all;
            }
        }

        private static SnapshotScenario Cap22()
        {
            var options = new UiHostOptions { Profile = DeviceProfile.W1280, Backend = HostBackends.Unavailable };
            var scenario = new SnapshotScenario("CAP-22", "CAP-22-build-normal-w1280", options,
                "Build normal (sin simulador): equipo sin Bluetooth LE, vista de búsqueda vacía", s => s.Frames(2)) { BackendName = "Unavailable" };
            scenario.Checks = host => Expect(host, ("vista de búsqueda", host.DeviceScreen?.View == DeviceViewKind.Scan),
                ("sin cinta", host.Find("sim-ribbon") == null), ("sin llave", host.Find("sim-open") == null),
                ("Sin sensor", host.IsTextVisible("Sin sensor")));
            return scenario.WithProbe(PixelProbe.Point("bg.app", "device-views", new Vector2(0.5f, 0.02f), Tokens.BgApp.Rgb.ToHex()));
        }

#if ROWMATE_SIMULATOR
        private static void AddSimulatorScenarios(List<SnapshotScenario> all)
        {
            var bgCard = Tokens.SurfaceCard.Rgb.ToHex();
            var bgApp = Tokens.BgApp.Rgb.ToHex();

            // CAP-01: el tablero a los 60 s.
            var cap01 = Sim("CAP-01", "CAP-01-dispositivo-tv", DeviceProfile.TV, "es-AR",
                "Dispositivo; remo simulado conectado; 60 s a 150 W / 24 spm; pulsómetro sin conectar", s => s.Advance(60));
            cap01.WithProbe(PixelProbe.Point("bg.app", "device-views", new Vector2(0.03f, 0.85f), bgApp))
                .WithProbe(PixelProbe.Point("surface.card", "metric-tile-split", new Vector2(0.5f, 0.94f), bgCard))
                .WithProbe(PixelProbe.Point("appbar", "device-appbar", new Vector2(0.55f, 0.12f), bgApp))
                .WithProbe(PixelProbe.Dominant("metric.split", "metric-tile-split .rm-metric-tile__value", Tokens.MetricSplit.Rgb.ToHex()))
                .WithProbe(PixelProbe.Dominant("metric.spm", "metric-tile-spm .rm-metric-tile__value", Tokens.MetricSpm.Rgb.ToHex()))
                .WithProbe(PixelProbe.Dominant("metric.watts", "metric-tile-watts .rm-metric-tile__value", Tokens.MetricWatts.Rgb.ToHex()))
                .WithProbe(PixelProbe.Dominant("metric.distance", "metric-tile-distance .rm-metric-tile__value", Tokens.MetricDistance.Rgb.ToHex()))
                .WithProbe(PixelProbe.Point("dev.simulator", "sim-ribbon", new Vector2(0.5f, 0.5f), Tokens.DevSimulator.Rgb.ToHex(), 2), true);
            cap01.Checks = host => Expect(host, ("4 tarjetas", host.DeviceScreen!.Connected.Grid.Tiles.Count == 4),
                ("2 columnas", host.DeviceScreen.Connected.Grid.Spec?.Columns == 2), ("02:13", Tile(host, MetricKind.Split) == "02:13"),
                ("24.0", Tile(host, MetricKind.Spm) == "24.0"), ("150", Tile(host, MetricKind.Watts) == "150"), ("226m", Tile(host, MetricKind.Distance) == "226m"),
                ("sin PULSO", host.Find("metric-tile-hr") == null), ("Desconectar", host.IsShown("device-disconnect")),
                ("Remo simulado", host.IsTextVisible("Remo simulado")), ("Sin sensor", host.IsTextVisible("Sin sensor")),
                ("cinta", host.IsShown("sim-ribbon")), ("llave", host.IsShown("sim-open")));
            all.Add(cap01);

            var cap02 = Sim("CAP-02", "CAP-02-dispositivo-tv-en", DeviceProfile.TV, "en-US", "Igual que CAP-01, con el sistema en inglés", s => s.Advance(60));
            cap02.Checks = host => Expect(host, ("Device", host.IsTextVisible("Device")), ("Disconnect", host.TextOf("device-disconnect") == "Disconnect"),
                ("Connected", host.IsTextVisible("Connected")), ("HEART RATE SENSOR", host.IsTextVisible("HEART RATE SENSOR")),
                ("No sensor", host.IsTextVisible("No sensor")), ("SIMULADOR", host.HasText("SIMULADOR")), ("Remo simulado", host.IsTextVisible("Remo simulado")));
            all.Add(cap02);

            var cap03 = Sim("CAP-03", "CAP-03-dispositivo-l640", DeviceProfile.L640, "es-AR", "Igual que CAP-01 en 640 × 360", s => s.Advance(60));
            cap03.Checks = host => Expect(host, ("2 columnas", host.DeviceScreen!.Connected.Grid.Spec?.Columns == 2),
                ("proporción 1,3", Math.Abs((host.DeviceScreen.Connected.Grid.Spec?.AspectRatio ?? 0) - 1.3) < 1e-6));
            // Desde la It. 4a la llave queda abajo a la izquierda, sobre el principio de la tarjeta de SPLIT (que no se toca, 00-deuda-
            // diseno §1.4): la sonda del fondo de la tarjeta va a su derecha.
            all.Add(cap03.WithProbe(PixelProbe.Point("surface.card", "metric-tile-split", new Vector2(0.96f, 0.5f), bgCard)));

            all.Add(Cap04("CAP-04a", "CAP-04a-dispositivo-w1280", false));
            all.Add(Cap04("CAP-04b", "CAP-04b-dispositivo-w1280-hrm", true));

            var cap05 = Sim("CAP-05", "CAP-05-dispositivo-th", DeviceProfile.TH, "es-AR", "Igual que CAP-01 en el teléfono horizontal", s => s.Advance(60));
            cap05.Checks = host => Expect(host, ("3 columnas", host.DeviceScreen!.Connected.Grid.Spec?.Columns == 3));
            all.Add(cap05);

            var cap06 = Prueba("CAP-06", "CAP-06-grilla-pulso", DeviceProfile.TV, "Remo de prueba conectado que manda 140 de pulso por FTMS",
                s => s.Do(host =>
                {
                    host.TestRower!.ConnectedName = "Remo de prueba";
                    host.TestRower.Emit(BleStatus.Connected);
                    host.TestRower.EmitData(new RowingData { StrokeRate = 23.5, PowerWatts = 149, DistanceMeters = 652, Pace500mSeconds = 133, HeartRate = 140 });
                }));
            cap06.WithProbe(PixelProbe.Dominant("metric.hr", "metric-tile-hr .rm-metric-tile__value", Tokens.MetricHr.Rgb.ToHex()))
                .WithProbe(PixelProbe.Dominant("metric.hr.hud:label", "metric-tile-hr .rm-metric-tile__label", Tokens.MetricHrHud.Rgb.ToHex(), 3));
            cap06.Checks = host => Expect(host, ("5 tarjetas", host.DeviceScreen!.Connected.Grid.Tiles.Count == 5), ("140", Tile(host, MetricKind.HeartRate) == "140"),
                ("23.5", Tile(host, MetricKind.Spm) == "23.5"), ("149", Tile(host, MetricKind.Watts) == "149"), ("652m", Tile(host, MetricKind.Distance) == "652m"));
            all.Add(cap06);

            var cap07 = Prueba("CAP-07", "CAP-07-valores-largos", DeviceProfile.TV, "Remo de prueba: split 6001 s, 40,5 spm, 1999 W, 123 456 m",
                s => s.Do(host =>
                {
                    host.TestRower!.Emit(BleStatus.Connected);
                    host.TestRower.EmitData(new RowingData { StrokeRate = 40.5, PowerWatts = 1999, DistanceMeters = 123456, Pace500mSeconds = 6001 });
                }));
            cap07.Checks = host => Expect(host, ("100:01", Tile(host, MetricKind.Split) == "100:01"), ("40.5", Tile(host, MetricKind.Spm) == "40.5"),
                ("1999", Tile(host, MetricKind.Watts) == "1999"), ("123456m", Tile(host, MetricKind.Distance) == "123456m"),
                ("123456m achicado", (host.DeviceScreen!.Connected.Grid.Find(MetricKind.Distance)?.ValueBox.FitScale ?? 1) < 1));
            all.Add(cap07);

            var cap08 = Sim("CAP-08", "CAP-08-busqueda-tv", DeviceProfile.TV, "es-AR", "Remo desconectado con Desconectar",
                s => Steps(s.Advance(5), s.Tap("device-disconnect")));
            cap08.WithProbe(PixelProbe.Dominant("text.tertiary:device-empty-hint", "device-empty-area .rm-empty__hint", PixelProbe.Composite(Tokens.TextTertiary, Tokens.BgApp), 3));
            cap08.Checks = host => Expect(host, ("sin Desconectar", !host.IsShown("device-disconnect")), ("Buscar habilitado", host.Find("device-search")!.enabledSelf),
                ("No se encontraron dispositivos", host.IsTextVisible("No se encontraron dispositivos")));
            all.Add(cap08);

            var cap09 = Sim("CAP-09", "CAP-09-busqueda-l640-hrm", DeviceProfile.L640, "es-AR", "Remo desconectado; pulsómetro buscando",
                s => Steps(s.Advance(2), s.Tap("device-disconnect"), s.ScrollTo("hrm-search"), s.Tap("hrm-search")));
            cap09.Checks = host => Expect(host, ("Buscar deshabilitado", !host.Find("device-search")!.enabledSelf), ("fila", host.Find(HrmRow) != null),
                ("fila en el área de la tarjeta", host.Find("hrm-card-slot-scan")!.Contains(host.Find(HrmRow)!)));
            all.Add(cap09);

            all.Add(Crop(Sim("CAP-10", "CAP-10-tarjeta-sin-sensor", DeviceProfile.TV, "es-AR", "Pulsómetro sin conectar", s => s.Advance(5)), "hrm-card")
                .WithProbe(PixelProbe.Dominant("text.tertiary:hrm-watch-hint", "hrm-watch-hint", PixelProbe.Composite(Tokens.TextTertiary, Tokens.SurfaceCard), 3)));
            all.Add(Crop(Sim("CAP-11", "CAP-11-tarjeta-buscando", DeviceProfile.TV, "es-AR", "Pulsómetro buscando", s => s.Tap("hrm-search")), "hrm-card")
                .WithProbe(PixelProbe.Dominant("accent.primary:spinner", "hrm-card .rm-spinner", Tokens.AccentPrimary.Rgb.ToHex(), 3), true));
            all.Add(Crop(Sim("CAP-12a", "CAP-12a-tarjeta-conectada", DeviceProfile.TV, "es-AR", "Pulsómetro recién conectado",
                s => Steps(s.Tap("hrm-search"), s.Tap(HrmRow))), "hrm-card"));
            all.Add(Crop(Sim("CAP-12b", "CAP-12b-tarjeta-conectada-60s", DeviceProfile.TV, "es-AR", "Pulsómetro conectado, 60 s después",
                s => Steps(s.Tap("hrm-search"), s.Tap(HrmRow), s.Advance(60))), "hrm-card")
                .WithProbe(PixelProbe.Dominant("metric.hr", "hrm-bpm", Tokens.MetricHr.Rgb.ToHex()))
                .WithProbe(PixelProbe.Dominant("metric.hr.hud:bpm", "hrm-bpm-unit", Tokens.MetricHrHud.Rgb.ToHex(), 3)));
            all.Add(Crop(Sim("CAP-13", "CAP-13-tarjeta-reconectando", DeviceProfile.TV, "es-AR", "Caída simulada del pulsómetro (dentro de los 3 s)",
                s => Steps(s.Tap("hrm-search"), s.Tap(HrmRow), s.Advance(2), s.Do(host => host.Simulator!.SimHeartRate.SimulateDisconnect()), s.Advance(1))), "hrm-card"));

            all.Add(Cap14("CAP-14a", "CAP-14a-tarjeta-conectando", "Conectando al recordado (queda conectando)", host =>
            {
                host.TestHeartRate!.ConnectBehavior = ScriptedConnect.StayConnecting;
                _ = host.App.HeartRate.ConnectAsync("7E:1A:2B:3C", ScriptedHeartRateService.RememberedSample);
            }));
            all.Add(Cap14("CAP-14b", "CAP-14b-tarjeta-no-encontrado", "Recordado, desconectado, sin reintentar",
                host => host.TestHeartRate!.Remember(ScriptedHeartRateService.RememberedSample, false)));
            all.Add(Cap14("CAP-14c", "CAP-14c-tarjeta-incompatible", "Conectar lanza HrmIncompatibleException", host =>
            {
                host.TestHeartRate!.ConnectBehavior = ScriptedConnect.ThrowIncompatible;
                _ = host.App.HeartRate.ConnectAsync("AA:BB", "Sensor X");
            }).WithProbe(PixelProbe.Dominant("state.dangerText:hrm-error", "hrm-error", Tokens.StateDangerText.Rgb.ToHex(), 3)));
            all.Add(Cap14("CAP-14d", "CAP-14d-tarjeta-error-largo", "Conectar lanza un error de 300 caracteres", host =>
            {
                host.TestHeartRate!.ConnectBehavior = ScriptedConnect.ThrowError;
                host.TestHeartRate.ErrorText = LongError();
                _ = host.App.HeartRate.ConnectAsync("AA:BB", "Sensor X");
            }));

            var cap15 = Crop(Prueba("CAP-15", "CAP-15-tarjeta-remo-buscando", DeviceProfile.TV, "Remo de prueba buscando; pulsómetro sin conectar",
                s => s.Do(host => host.TestRower!.Emit(BleStatus.Scanning))), "hrm-card");
            cap15.Checks = host => Expect(host, ("Buscar deshabilitado", !host.Find("hrm-search")!.enabledSelf));
            all.Add(cap15);

            var cap16 = Sim("CAP-16", "CAP-16-panel-p800", DeviceProfile.P800, "es-AR", "Panel recién abierto sobre Dispositivo", s => s.Tap("sim-open"));
            cap16.WithProbe(PixelProbe.Point("surface.overlay", "sim-panel", new Vector2(0.97f, 0.5f), Tokens.SurfaceOverlay.Rgb.ToHex(), 2), true);
            cap16.Checks = host => Expect(host, ("280 dp", Math.Abs(host.BoundsOf("sim-panel").width - 280) < 0.6f), ("150 W", host.IsTextVisible("150 W")),
                ("24 spm", host.IsTextVisible("24 spm")), ("real", host.IsTextVisible("real")), ("sim-close tocable", host.WhyNotTappable("sim-close") == null),
                ("sin llave", !host.IsShown("sim-open")));
            all.Add(cap16);

            // QA-V2-05 (aceptada por el PO): en P800 el renglón 14:00 queda arriba, fuera de la foto; el valor lo comprueba el catálogo.
            var cap17 = Sim("CAP-17", "CAP-17-panel-p800-final", DeviceProfile.P800, "es-AR", "Panel desplazado al final, con la hora forzada en 14:00",
                s => Steps(s.Tap("sim-open"), s.Tap("sim-hour-plus"), s.ScrollToEnd("sim-panel-scroll")));
            cap17.Checks = host => Expect(host, ("Hora escena 14:00", host.TextOf("sim-hour-value") == "14:00"),
                ("Hora real", host.IsShown("sim-hour-real")), ("Remando", host.IsShown("sim-rowing")),
                ("las dos fallas", host.IsShown("sim-drop-rower") && host.IsShown("sim-drop-hrm")));
            all.Add(cap17);
            var cap18 = Sim("CAP-18", "CAP-18-panel-tv", DeviceProfile.TV, "es-AR", "Panel abierto sobre Dispositivo", s => s.Tap("sim-open"));
            cap18.Checks = host => Expect(host, ("entra entero", host.WhyNotTappable("sim-drop-hrm") == null));
            all.Add(cap18);
            var cap19 = Sim("CAP-19", "CAP-19-panel-limites", DeviceProfile.TV, "es-AR", "Panel con 500 W, 40 spm, hora 0:00 y Remando apagado",
                s => Steps(s.Advance(60), s.Tap("sim-open"), s.TapTimes("sim-watts-plus", 40), s.TapTimes("sim-spm-plus", 20), s.TapTimes("sim-hour-minus", 13),
                    s.Tap("sim-rowing"), s.Advance(1)));
            cap19.Checks = host => Expect(host, ("500 W", host.IsTextVisible("500 W")), ("40 spm", host.IsTextVisible("40 spm")), ("0:00", host.IsTextVisible("0:00")),
                ("--:--", Tile(host, MetricKind.Split) == "--:--"), ("0.0", Tile(host, MetricKind.Spm) == "0.0"), ("0", Tile(host, MetricKind.Watts) == "0"));
            all.Add(cap19);

            var tabs = new[] { ("a", "tab-device", "dispositivo"), ("b", "tab-workout", "workout"), ("c", "tab-routines", "rutinas"), ("d", "tab-history", "historial") };
            foreach (var (suffix, tab, label) in tabs)
            {
                var name = tab;
                all.Add(Crop(Sim("CAP-20" + suffix, "CAP-20" + suffix + "-barra-" + label, DeviceProfile.TV, "es-AR", "Destino elegido: " + label,
                    s => s.Tap(name)), "tab-bar"));
            }

            all.Add(Crop(Sim("CAP-20e", "CAP-20e-barra-en", DeviceProfile.TV, "en-US", "Barra en inglés, Dispositivo elegido", s => s.Frames(2)), "tab-bar"));

            var places = new[] { ("c", "tab-history", "screen-history", "historial", "Historial") };

            // CAP-21a: desde la It. 4a Workout ya no es provisoria (AC-APP-02-08, WSC.WorkoutIsNotAPlaceholder): la toma muestra la
            // pantalla real en reposo, sin rutinas (las capturas del entrenamiento son las del conjunto it4a).
            var cap21a = Sim("CAP-21a", "CAP-21a-workout", DeviceProfile.TV, "es-AR", "Workout (desde la It. 4a la pantalla real, sin rutinas)",
                s => s.Tap("tab-workout"));
            cap21a.Checks = host => Expect(host, ("título Entrenamiento", host.IsTextVisible("Entrenamiento")),
                ("Entrenamiento libre", host.IsShown("workout-free")),
                ("sin ícono provisorio", host.Find("screen-workout")?.Q(className: "rm-placeholder__icon") == null));
            all.Add(cap21a);

            // CAP-21b: desde la It. 3 Rutinas ya no es provisoria (AC-APP-02-08): la toma muestra la pantalla real con la base
            // vacía (las capturas de Rutinas son las del conjunto it3, 03-rutinas/snapshots/).
            var cap21b = Sim("CAP-21b", "CAP-21b-rutinas", DeviceProfile.TV, "es-AR", "Rutinas (desde la It. 3 la pantalla real, base vacía)",
                s => s.Tap("tab-routines"));
            cap21b.Checks = host => Expect(host, ("título Rutinas", host.IsTextVisible("Rutinas")), ("vacío", host.IsShown("routines-empty")),
                ("sin ícono provisorio", host.Find("screen-routines")?.Q(className: "rm-placeholder__icon") == null), ("+", host.IsShown("routines-add")));
            all.Add(cap21b);
            foreach (var (suffix, tab, screen, label, title) in places)
            {
                var tabName = tab;
                var screenName = screen;
                var expected = title;
                var scenario = Sim("CAP-21" + suffix, "CAP-21" + suffix + "-" + label, DeviceProfile.TV, "es-AR", "Pestaña provisoria: " + title, s => s.Tap(tabName));
                scenario.Checks = host => Expect(host, ("título " + expected, host.IsTextVisible(expected)),
                    ("ningún otro texto", OnlyText(host, screenName, expected)));
                all.Add(scenario.WithProbe(PixelProbe.Dominant("placeholder.icon", screenName + " .rm-placeholder__icon", PixelProbe.Composite(TextAt24(), Tokens.BgApp), 3), true));
            }

            var cap24 = Sim("CAP-24", "CAP-24-letra-130", DeviceProfile.TV, "es-AR", "Letra del sistema en 1,3×, remo y pulsómetro conectados",
                s => Steps(s.Tap("hrm-search"), s.Tap(HrmRow), s.Advance(60)));
            cap24.Host.TextScale = 1.3;
            cap24.Checks = host => Expect(host, ("clase 1,3", host.Shell.ClassListContains("rm-text-scale-130")));
            all.Add(cap24);

            var cap25 = Sim("CAP-25", "CAP-25-strava-5-destinos", DeviceProfile.TV, "es-AR", "Configuración de prueba con Strava", s => s.Frames(2));
            cap25.Host.ProfileTab = true;
            cap25.Checks = host => Expect(host, ("5 destinos", host.App.Shell.Destinations.Count == 5), ("Perfil", host.Find("tab-profile") != null),
                ("Historial cuarto", host.App.Shell.Destinations[3].ElementName == "tab-history"));
            all.Add(cap25);

            var vid01 = Sim("VID-01", "VID-01-tablero", DeviceProfile.TV, "es-AR", "Remo y pulsómetro conectados, a 150 W y 24 spm, desde los 60 s; 10 s",
                s => Steps(s.Tap("hrm-search"), s.Tap(HrmRow), s.Advance(59.5)));
            vid01.Kind = SnapshotKind.Video;
            vid01.Video = new VideoSpec(10);
            all.Add(vid01);

            var vid02 = Sim("VID-02", "VID-02-pestanas", DeviceProfile.TV, "es-AR", "Recorrido por las 4 pestañas; 10 s", s => s.Advance(3));
            vid02.Kind = SnapshotKind.Video;
            vid02.Video = new VideoSpec(10).At(1, h => h.Tap("tab-workout")).At(3.5, h => h.Tap("tab-routines")).At(6, h => h.Tap("tab-history"))
                .At(8.5, h => h.Tap("tab-device"));
            all.Add(vid02);

            // QA-V2-15: la caída sale de tocar "Simular desconexión" en el panel (con hit-test, como el usuario) y el panel se
            // cierra en el mismo cuadro (cerrar lo oculta en el acto, DN-14) para que se vea entero el "apagar" de 120 ms.
            var vid03 = Sim("VID-03", "VID-03-caida-remo", DeviceProfile.TV, "es-AR", "Simular desconexión (desde el panel) y vuelta; 8 s",
                s => Steps(s.Advance(59.5), s.Tap("sim-open")));
            vid03.Kind = SnapshotKind.Video;
            vid03.Video = new VideoSpec(8).At(1, h =>
            {
                h.Tap("sim-drop-rower");
                h.Tap("sim-close");
            });
            all.Add(vid03);

            // Sin pedido del PO: útiles para QA.
            all.Add(Crop(Sim("AUX-01", "AUX-01-tarjeta-conectada-en", DeviceProfile.TV, "en-US", "La tarjeta con -- en inglés",
                s => Steps(s.Tap("hrm-search"), s.Tap(HrmRow))), "hrm-card"));
            var aux02 = Sim("AUX-02", "AUX-02-dispositivo-tv-reducir-movimiento", DeviceProfile.TV, "es-AR", "CAP-01 con reducir movimiento (debe verse igual)",
                s => s.Advance(60));
            aux02.Host.ReduceMotion = true;
            all.Add(aux02);
        }

        private static SnapshotScenario Cap04(string id, string stem, bool scrolledToEnd)
        {
            var scenario = Sim(id, stem, DeviceProfile.W1280, "es-AR",
                scrolledToEnd ? "Pulsómetro conectado (60 s); Dispositivo desplazada hasta el final" : "Pulsómetro conectado (60 s); Dispositivo sin desplazar",
                s => Steps(s.ScrollTo("hrm-search"), s.Tap("hrm-search"), s.Tap(HrmRow), s.Advance(60),
                    scrolledToEnd ? s.ScrollToEnd("device-connected-scroll") : s.Do(h => h.Find<UnityEngine.UIElements.ScrollView>("device-connected-scroll")!.scrollOffset = Vector2.zero)));
            if (scrolledToEnd)
            {
                // Sin la sonda de "bpm" (QA-V2-04): a densidad 1 el trazo de Inter 14/400 no tiene píxeles de cobertura
                // plena y el color más frecuente es un borde suavizado. El color de "bpm" se mide en CAP-12b (2,625).
                scenario.WithProbe(PixelProbe.Dominant("metric.hr", "hrm-bpm", Tokens.MetricHr.Rgb.ToHex()));
                scenario.Checks = host => Expect(host, ("142", host.TextOf("hrm-bpm") == "142"),
                    ("64 dp sobre la barra", Math.Abs(host.BoundsOf("tab-bar").yMin - host.BoundsOf("hrm-card").yMax - 64) <= 1.01f));
            }
            else
            {
                scenario.Checks = host => Expect(host, ("3 columnas", host.DeviceScreen!.Connected.Grid.Spec?.Columns == 3),
                    ("proporción 1,6", Math.Abs((host.DeviceScreen.Connected.Grid.Spec?.AspectRatio ?? 0) - 1.6) < 1e-6),
                    ("tarjeta del pulsómetro bajo la barra", host.BoundsOf("hrm-card").yMin >= host.BoundsOf("tab-bar").yMin - 1));
            }

            return scenario;
        }

        private static SnapshotScenario Cap14(string id, string stem, string state, Action<UiTestHost> action)
        {
            return Crop(Prueba(id, stem, DeviceProfile.TV, state, s => s.Do(action)), "hrm-card");
        }

        private static SnapshotScenario Sim(string id, string stem, DeviceProfile profile, string language, string state, Func<ScenarioScript, IEnumerator> arrange)
        {
            var options = new UiHostOptions
            {
                Profile = profile,
                IsDesktop = !profile.IsPhone,
                Backend = HostBackends.Simulator(false),
                SimulatorLayer = SimulatorLayerMode.Full,
                LanguageTags = new[] { language },
            };
            return new SnapshotScenario(id, stem, options, state, arrange) { BackendName = "Sim" };
        }

        private static SnapshotScenario Prueba(string id, string stem, DeviceProfile profile, string state, Func<ScenarioScript, IEnumerator> arrange)
        {
            var options = new UiHostOptions { Profile = profile, IsDesktop = !profile.IsPhone, Backend = HostBackends.TestDevices, SimulatorLayer = SimulatorLayerMode.ChromeOnly };
            return new SnapshotScenario(id, stem, options, state, arrange) { BackendName = "Prueba", SkipReadingWindow = true };
        }
#endif

        private static SnapshotScenario Crop(SnapshotScenario scenario, string element)
        {
            scenario.CropElement = element;
            return scenario;
        }

        private static IEnumerator Steps(params IEnumerator[] steps)
        {
            foreach (var step in steps)
            {
                yield return step;
            }
        }

        private static string? Tile(UiTestHost host, MetricKind kind)
        {
            return host.DeviceScreen?.Connected.Grid.Find(kind)?.Value.Text;
        }

        private static IEnumerable<string> Expect(UiTestHost host, params (string What, bool Ok)[] checks)
        {
            foreach (var check in checks)
            {
                if (!check.Ok)
                {
                    yield return "no se cumple: " + check.What;
                }
            }
        }

        // CAP-21: dentro de la pantalla solo el título (la barra de pestañas y la cinta son de la app).
        private static bool OnlyText(UiTestHost host, string screenName, string title)
        {
            var screen = host.Find(screenName);
            if (screen == null)
            {
                return false;
            }

            var texts = UiQuery.VisibleTexts(screen);
            return texts.Count == 1 && texts[0] == title;
        }

        // Blanco al 24 % (el ícono apagado): no es un token, es text.primary con opacidad 0,24.
        private static ColorToken TextAt24()
        {
            return ColorToken.HexAlpha("snapshot.white24", Tokens.TextPrimary.Rgb.ToHex(), 24);
        }

        private static string LongError()
        {
            var text = "PlatformException(connect_failed, GATT error 133: android.bluetooth.BluetoothGatt.connect() devolvió un estado inesperado ";
            while (text.Length < 300)
            {
                text += "· reintento sin respuesta del periférico ";
            }

            return text.Substring(0, 300);
        }
    }
}
