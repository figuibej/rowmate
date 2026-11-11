using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using RowMate.Core.Ble;
using RowMate.Core.Scene;
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
    // del lector forzado, A11yTreeDump). It. 15 (AC-D15-31, #81; QA-V15-03): se suman las 13 que habían quedado para issues (CAP-06, -08 a
    // -11, -13, -14, -21, -24 a -26 y VID-01/02, 01-po-spec §7 de la 14); las de la escena real (CAP-06, -10, -11, -14 y VID-02), con la
    // escena en nivel Alto como it6a/it8 (solo con el define del simulador, como DebtCatalog). CAP-26 usa una escena de prueba que no
    // queda lista (FakeImmersiveScene): el indicador de carga con el HUD andando, sin depender de cuánto tarde la escena real.
    public static class PolishCatalog
    {
        public const string LongRowerName = "AMS-670B RowMaster 2024";

        public static IReadOnlyList<SnapshotScenario> All
        {
            get
            {
                var all = new List<SnapshotScenario>();

                // It. 15 (AC-D15-31): CAP-01 y CAP-02 los escribe tools/icon (dotnet); el manifest los lista con ese origen, sin tomarlos.
                foreach (var (id, state) in new[] { ("CAP-01", "Ícono: maestro 1024 y 256 a 16 sobre claro y oscuro"), ("CAP-02", "Adaptativo de Android con las máscaras") })
                {
                    all.Add(new SnapshotScenario(id, id, new UiHostOptions { Profile = DeviceProfile.TV }, state, null)
                    {
                        BackendName = "—", SupersededBy = "tools/icon", SupersededIds = "dotnet run --project tools/icon",
                    });
                }

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
                    // It. 15 (AC-D15-31): con el gráfico desplazado a la vista (el 0, el eje X y los extremos redondos).
                    var shot = History(id, stem, profile, "Detalle de Escalera con el gráfico a la vista", s => ChartInView(s));
                    shot.Checks = h => Audit(h, "Detalle", ("detalle abierto", () => h.Shell.Pages.Top is SessionDetailPage),
                        ("el gráfico entero a la vista", () => h.BoundsOf("session-chart").yMax <= h.BoundsOf("session-scroll").yMax + 0.5f));
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

                AddItFifteen(all);
                all.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
                return all;
            }
        }

        // It. 15 (AC-D15-31): las tomas de la spec de la 14 que faltaban.
        private static void AddItFifteen(List<SnapshotScenario> all)
        {
            // CAP-08: Rutinas en TH, en inglés, con los resúmenes.
            var cap08 = Workout("CAP-08", "CAP-08-rutinas-th-en", DeviceProfile.TH, "Rutinas: la lista con los resúmenes en inglés",
                s => Steps(s.Tap("tab-routines"), s.Settle()), "en-US");
            cap08.Checks = h => AuditWith(h, "Rutinas", RoutineTexts(h), ("tarjetas", () => h.IsShown("routine-card-0")));
            all.Add(cap08);

            // CAP-09: el editor de Escalera (F-9) en TV, en inglés: duraciones y objetivos tabulares.
            var cap09 = History("CAP-09", "CAP-09-editor-escalera-tv-en", DeviceProfile.TV, "Editor de Escalera con duraciones y objetivos tabulares",
                s => Steps(s.Tap("tab-routines"), s.Tap("routine-card-0"), s.Settle()), "en-US");
            cap09.Checks = h => AuditWith(h, "Editor", RoutineTexts(h), ("el editor", () => h.Editor != null));
            all.Add(cap09);

            // CAP-13: L640 con letra 1,3×: el diálogo de Corta completada, centrado, entero y quieto (AC-POL-04).
            var cap13 = Workout("CAP-13", "CAP-13-completada-l640-13", DeviceProfile.L640, "Diálogo de rutina completada centrado (AC-POL-04)",
                s => Steps(s.StartRoutine("Corta"), s.AdvanceTo(31), s.Settle()), "es-AR", 1.3);
            cap13.Checks = h => AuditWith(h, "Completada", RoutineTexts(h), ("el diálogo", () => h.TopDialog != null),
                ("centrado", () => Math.Abs(h.TopDialog!.Box.worldBound.center.x - h.Root.worldBound.center.x) < 1f
                                   && Math.Abs(h.TopDialog.Box.worldBound.center.y - h.Root.worldBound.center.y) < 1f));
            all.Add(cap13);

            // CAP-21: Historial en P360 con letra 1,3×, en inglés: las columnas achicadas enteras (#36).
            var cap21 = History("CAP-21", "CAP-21-historial-p360-13-en", DeviceProfile.P360, "Historial: columnas achicadas enteras, 4 dp de aire (#36)",
                s => OpenHistory(s), "en-US");
            cap21.Host.TextScale = 1.3;
            cap21.Checks = h => Audit(h, "Historial", ("tarjetas", () => h.IsShown("history-card-0")), ("sin recortes", () => h.Audit().ClippedTexts.Count == 0));
            all.Add(cap21);

            // CAP-24: el cuadro a los 120 ms del push del editor (AC-POL-01): sin cuadro vacío.
            var cap24 = History("CAP-24", "CAP-24-push-editor-w1280", DeviceProfile.W1280, "Cuadro a mitad del push del editor (120 ms): sin cuadro vacío",
                s => Steps(s.Tap("tab-routines"), s.Do(h => h.Tap("routine-card-0")), s.Advance(0.12)));
            cap24.MotionMayRun = true;
            cap24.OverflowMayRun = true;
            cap24.Checks = h => AuditWith(h, "Editor", RoutineTexts(h), ("el editor entrando", () => h.Editor != null));
            all.Add(cap24);

            // CAP-25: el error del Historial con su ícono de 56 dp (#37), con una base de guion que falla.
            var cap25 = new SnapshotScenario("CAP-25", "CAP-25-historial-error-tv-en", new UiHostOptions
            {
                Profile = DeviceProfile.TV,
                Backend = HostBackends.TestDevices,
                LanguageTags = new[] { "en-US" },
                Sessions = (scheduler, logger, routines) =>
                {
                    var repository = new ScriptedSessionRepository(scheduler);
                    HistorySeeds.Apply(routines.Repository, repository.Store, HistorySeeds.F9);
                    repository.HistoryFailures = 2;
                    return repository;
                },
            }, "Error del Historial con su ícono de 56 dp (#37)", s => Steps(s.Tap("tab-history"), s.Settle())) { BackendName = "Prueba" };
            cap25.Checks = h => Audit(h, "Historial", ("el error", () => h.IsShown("history-error")),
                ("ícono de 56 dp", () => Math.Abs(h.Find("history-error-icon")!.resolvedStyle.width - 56) < 0.6f));
            all.Add(cap25);

            // CAP-26: el primer entrenamiento con la escena sin preparar: Preparando escena… con el HUD andando (#21).
            var cap26 = Workout("CAP-26", "CAP-26-preparando-escena-tv", DeviceProfile.TV, "Primer entrenamiento: Preparando escena… con el HUD andando (#21)",
                s => Steps(s.StartFree(), s.AdvanceTo(3.2)), "es-AR");
            cap26.Host.FakeScene = new FakeImmersiveScene(new RecordingLogger());
            cap26.MotionMayRun = true;
            cap26.Checks = h => Audit(h, "HUD", ("el indicador", () => h.Immersive!.View.SceneLoading != null && h.Immersive.View.SceneLoading.IsShown),
                ("el reloj andando", () => h.App.Workout.State.Value.Phase == RowMate.Core.Workout.WorkoutPhase.Active));
            all.Add(cap26);

            // VID-01: el recorrido de transiciones en W1280 (pestañas, push y pop del editor; AC-POL-01).
            var vid01 = History("VID-01", "VID-01-transiciones-w1280", DeviceProfile.W1280, "Pestañas, push y pop del editor (6 s)", s => s.Settle());
            vid01.Kind = SnapshotKind.Video;
            vid01.Video = new VideoSpec(6)
                .At(0.3, h => h.Tap("tab-routines"))
                .At(1.3, h => h.Tap("routine-card-0"))
                .At(2.6, h => h.PressBack(RowMate.Presentation.Shell.BackSource.SystemBack))
                .At(3.6, h => h.Tap("tab-history"))
                .At(4.6, h => h.Tap("tab-workout"));
            all.Add(vid01);

#if ROWMATE_SIMULATOR
            // CAP-06: la ventana mínima en inglés: Workout con el HUD y el Lago alpino, sin desbordes.
            var cap06 = Scene("CAP-06", "CAP-06-workout-l640-en", DeviceProfile.L640, EnvironmentId.Lake, "Ventana mínima: el HUD con el Lago alpino, sin desbordes",
                s => Steps(s.StartFree(), s.AdvanceTo(20.2)), "en-US");
            cap06.Inspect = (h, frame, values) => Audit(h, "HUD", ("la escena", () => h.Scene != null && h.Scene.World != null), ("sin desbordes", () => h.Audit().IsClean));
            all.Add(cap06);

            // CAP-10: el HUD en TV con el Río urbano y la barra de pasos (Prueba 4a).
            var cap10 = Scene("CAP-10", "CAP-10-hud-rio-tv", DeviceProfile.TV, EnvironmentId.River, "HUD con Río urbano y la barra de pasos",
                s => Steps(s.StartRoutine("Prueba 4a"), s.AdvanceTo(40.2)), "es-AR");
            cap10.Inspect = (h, frame, values) => AuditWith(h, "HUD", RoutineTexts(h), ("la barra de pasos", () => h.Immersive!.View.Steps != null),
                ("el río", () => h.App.SceneEnvironment.Selected.Value == EnvironmentId.River));
            all.Add(cap10);

            // CAP-11: TH en inglés con la Costa y Up next:.
            var cap11 = Scene("CAP-11", "CAP-11-hud-costa-th-en", DeviceProfile.TH, EnvironmentId.Coast, "HUD con Costa y Up next:",
                s => Steps(s.StartRoutine("Prueba 4a"), s.AdvanceTo(80.2)), "en-US");
            cap11.Inspect = (h, frame, values) => AuditWith(h, "HUD", RoutineTexts(h), ("Up next", () => h.IsShown("hud-up-next")),
                ("la costa", () => h.App.SceneEnvironment.Selected.Value == EnvironmentId.Coast));
            all.Add(cap11);

            // CAP-14: TH con el Canal de regata.
            var cap14 = Scene("CAP-14", "CAP-14-hud-regata-th", DeviceProfile.TH, EnvironmentId.Regatta, "HUD con Canal de regata",
                s => Steps(s.StartFree(), s.AdvanceTo(20.2)), "es-AR");
            cap14.Inspect = (h, frame, values) => Audit(h, "HUD", ("la regata", () => h.App.SceneEnvironment.Selected.Value == EnvironmentId.Regatta));
            all.Add(cap14);

            // VID-02: la rotación de TV a TH con el HUD y la escena (AC-APP-15-10), en un lienzo fijo.
            var vid02 = Scene("VID-02", "VID-02-rotar-tv-th", DeviceProfile.TV, EnvironmentId.Lake, "Libre a los 20 s: rotar de TV a TH (2 s)",
                s => Steps(s.StartFree(), s.AdvanceTo(20)), "es-AR");
            vid02.Kind = SnapshotKind.Video;
            vid02.Video = new VideoSpec(2).At(0.5, h => h.Resize(DeviceProfile.TH));
            vid02.Video.CanvasWidth = DeviceProfile.TH.WidthPx;
            vid02.Video.CanvasHeight = DeviceProfile.TV.HeightPx;
            all.Add(vid02);
#endif
        }

        private static SnapshotScenario Workout(string id, string stem, DeviceProfile profile, string state, Func<ScenarioScript, IEnumerator> arrange,
                                                string language, double textScale = 1)
        {
            var options = new UiHostOptions
            {
                Profile = profile,
                IsDesktop = profile.Code == DeviceProfile.W1280.Code || profile.Code == DeviceProfile.L640.Code,
                Backend = HostBackends.TestRower(RowerScripts.R4),
                LanguageTags = new[] { language },
                Routines = HostRoutines.Seeded(RoutineSeeds.F4A),
                StartUtc = UiHostOptions.WorkoutStartUtc,
                TextScale = textScale,
            };
            return new SnapshotScenario(id, stem, options, state, arrange) { BackendName = "R-4" };
        }

#if ROWMATE_SIMULATOR
        // La escena real en nivel Alto con el escenario forzado en las preferencias (como it8) y el maniquí.
        private static SnapshotScenario Scene(string id, string stem, DeviceProfile profile, EnvironmentId env, string state, Func<ScenarioScript, IEnumerator> arrange,
                                              string language)
        {
            var preferences = new RowMate.Core.Common.MemoryPreferences();
            if (env != EnvironmentId.Lake)
            {
                preferences.SetString(EnvironmentKeys.PreferenceKey, EnvironmentKeys.ToKey(env));
            }

            var options = new UiHostOptions
            {
                Profile = profile,
                IsDesktop = profile.Code == DeviceProfile.W1280.Code || profile.Code == DeviceProfile.L640.Code,
                Backend = HostBackends.TestRower(RowerScripts.R4),
                LanguageTags = new[] { language },
                Routines = HostRoutines.Seeded(RoutineSeeds.F4A),
                StartUtc = UiHostOptions.WorkoutStartUtc,
                Preferences = preferences,
                Scene = SceneMode.Real,
                SceneTier = QualityTier.High,
                SceneCharacter = RowMate.Scene.RowerCharacterKind.Mannequin,
            };
            return new SnapshotScenario(id, stem, options, state, arrange) { BackendName = "R-4", ExactInstant = true };
        }
#endif

        private static IEnumerator Steps(params IEnumerator[] steps)
        {
            foreach (var step in steps)
            {
                yield return step;
            }
        }

        private static string[] RoutineTexts(UiTestHost h)
        {
            var texts = new List<string>();
            foreach (var routine in h.App.Routines.State.Value.Routines)
            {
                texts.Add(routine.Name);
                if (!string.IsNullOrEmpty(routine.Description))
                {
                    texts.Add(routine.Description);
                }
            }

            return texts.ToArray();
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

        private static IEnumerator ChartInView(ScenarioScript s)
        {
            yield return OpenDetail(s, 0);
            yield return s.ScrollToEnd("session-scroll");
            yield return s.Settle();
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
            return AuditWith(h, screen, new string[0], checks);
        }

        // Con datos del usuario extra (los nombres y descripciones de las rutinas).
        private static IEnumerable<string> AuditWith(UiTestHost h, string screen, string[] userData, params (string What, Func<bool> Ok)[] checks)
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

            var data = new List<string> { LongRowerName, "Escalera", "Rutina borrada", ExportCatalog.FileName };
            data.AddRange(userData);
            problems.AddRange(TextAuditProbe.Violations(h, screen, "toma", data.ToArray()));
            return problems;
        }
    }
}
