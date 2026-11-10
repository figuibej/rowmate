using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using RowMate.App.Workout;
using RowMate.Core.Ble;
using RowMate.Core.Common;
using RowMate.Core.Design;
using RowMate.Core.Workout;
using RowMate.Presentation.Device;
using RowMate.Presentation.History;
using RowMate.Presentation.Workout;
using RowMate.Tests.PlayMode.History;
using RowMate.Tests.Support;
using RowMate.Tests.UiSupport;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace RowMate.Tests.PlayMode.It14
{
    // It. 14, correcciones de la verificación (05-qa-verification QA-V14-04, -06, -07): el recorrido de la auditoría de textos en es y en
    // (TextAuditTourTests, AC-APP-11-01), los fijos del legacy (LegacyFixedTextsTests, AC-APP-11-02), la continuidad al rotar o
    // redimensionar (ResizeContinuityTests, AC-APP-15-10) y el split constante en el gráfico (SplitRangeChartTests, AC-DEBT-07).
    // El recorrido de la demo con el simulador (AC-POL-05) está en Tests/PlayModeSimulator/DemoWalkthroughTests.cs.

    // El recorrido de textos (02-tech-design §4, ampliado por QA-G14-08): Dispositivo (sin buscar, buscando, resultados, conectando,
    // conectado, reconectando, error, Bluetooth apagado y sin permiso, y la tarjeta del pulsómetro en sus 6 cuerpos), Workout (lista, los 4
    // escenarios del EnvironmentPicker, el diálogo de inicio de cada rutina de F-4B, el HUD con rutina, la cuenta grande con Próximo,
    // pausa, terminar, completado, libre y libre en pausa), los paneles de series (abiertos sin y con parciales, repeticiones y cerrados),
    // Rutinas (lista, menú, borrar, editor nuevo, diálogo de paso, serie, repeticiones y una rutina con series) e Historial (vacío, error,
    // lista, Detalle en las 5 métricas, exportando, guardado y error al exportar). En cada parada, TextAuditProbe con los datos del
    // usuario de las semillas; además se guarda todo texto visible para LegacyFixedTextsTests. El recorrido de un idioma se corre una vez
    // y lo comparten las dos clases (Tour).
    public abstract class TextTourBase : WorkoutUiTestBase
    {
        protected sealed class TourResult
        {
            public readonly List<string> Problems = new List<string>();
            public readonly List<KeyValuePair<string, string>> Seen = new List<KeyValuePair<string, string>>();
            public readonly List<string> UserData = new List<string>();
            public int Stops;
        }

        private static readonly Dictionary<string, TourResult> Cache = new Dictionary<string, TourResult>();

        private TourResult _tour = new TourResult();
        private string _language = "es-AR";

        protected TourResult Tour => _tour;

        // El recorrido entero en ese idioma (una sola vez por corrida de pruebas).
        protected IEnumerator RunTour(string language)
        {
            if (Cache.TryGetValue(language, out var cached))
            {
                _tour = cached;
                yield break;
            }

            _language = language;
            _tour = new TourResult();
            _tour.UserData.Add(ScriptedHeartRateService.RememberedSample);
            _tour.UserData.Add(HistorySeeds.EscaleraName);
            _tour.UserData.Add(HistorySeeds.DeletedRoutineName);
            yield return DeviceSegment();
            yield return HeartRateSegment();
            yield return WorkoutSegment();
            yield return CompletionAndFreeSegment();
            yield return SeriesSegment();
            yield return RoutinesSegment();
            yield return HistorySegment();
            Cache[language] = _tour;
        }

        private UiHostOptions Options(DeviceProfile? profile = null)
        {
            return new UiHostOptions
            {
                Profile = profile ?? DeviceProfile.TV,
                LanguageTags = new[] { _language },
                StartUtc = UiHostOptions.WorkoutStartUtc,
            };
        }

        private void AddUserData(RoutineSeed seed)
        {
            foreach (var routine in seed.Routines)
            {
                _tour.UserData.Add(routine.Name);
                if (!string.IsNullOrEmpty(routine.Description))
                {
                    _tour.UserData.Add(routine.Description);
                }
            }
        }

        private void Stop(string screen, string state)
        {
            _tour.Stops++;
            _tour.Problems.AddRange(TextAuditProbe.Violations(H, screen, state, _tour.UserData.ToArray()));
            foreach (var text in TextAuditProbe.Collect(H.Root))
            {
                _tour.Seen.Add(new KeyValuePair<string, string>(screen + " · " + state, PathBreak.Strip(text).Trim()));
            }
        }

        private IEnumerator ScrollAndTap(string elementName)
        {
            yield return Frames();
            var element = H.Find(elementName);
            if (element != null && H.WhyNotTappable(elementName) != null && UiQuery.ScrollViewOf(element) != null)
            {
                H.ScrollTo(elementName);
                yield return Frames();
            }

            H.Tap(elementName);
            yield return Settle();
        }

        private IEnumerator CloseDialog()
        {
            if (H.TopDialog != null && H.Find("dialog-cancel") != null)
            {
                yield return ScrollAndTap("dialog-cancel");
            }
        }

        private IEnumerator DeviceSegment()
        {
            var options = Options();
            options.Backend = HostBackends.FakeBle(null, BleAdapterState.On);
            Create(options);
            yield return Settle();
            Stop("Dispositivo", "sin buscar");
            var adapter = H.FakeBle!.Adapter;
            H.Tap("device-search");
            adapter.Advertise(FakeBleDevices.Row);
            adapter.Advertise(FakeBleDevices.Anon);
            adapter.Advertise(FakeBleDevices.Bike);
            yield return Frames(2);
            H.Advance(1);
            yield return Frames(2);
            Stop("Dispositivo", "buscando");
            H.Advance(9.1);
            yield return Settle();
            Stop("Dispositivo", "resultados");
            H.Tap(ScanResultCard.NamePrefix + FakeBleDevices.Row.Id);
            yield return Frames(2);
            Stop("Dispositivo", "conectando");
            H.Advance(1);
            yield return Settle();
            adapter.Notify(FakeBleDevices.RowId, FtmsUuids.RowerData, FakeBleDevices.RowerData);
            yield return Settle();
            Stop("Dispositivo", "conectado");
            adapter.NeverRespond(FakeBleDevices.RowId);
            adapter.DropLink(FakeBleDevices.RowId);
            yield return Frames(2);
            H.Advance(1);
            yield return Frames(2);
            Stop("Dispositivo", "reconectando");

            // El remo sin nombre conectado: el nombre de respaldo (deviceRowerFallback).
            options = Options();
            options.Backend = HostBackends.FakeBle(null, BleAdapterState.On);
            Create(options);
            yield return Settle();
            H.Tap("device-search");
            H.FakeBle!.Adapter.Advertise(FakeBleDevices.Anon);
            yield return Frames(2);
            H.Advance(10.1);
            yield return Settle();
            H.Tap(ScanResultCard.NamePrefix + FakeBleDevices.Anon.Id);
            yield return Frames(2);
            H.Advance(1);
            yield return Settle();
            Stop("Dispositivo", "conectado sin nombre");

            options = Options();
            options.Backend = HostBackends.FakeBle(null, BleAdapterState.On);
            Create(options);
            yield return Settle();
            H.FakeBle!.Adapter.FailNextScan(new InvalidOperationException("error 2"));
            H.Tap("device-search");
            yield return Frames(2);
            H.Advance(0.5);
            yield return Settle();
            Stop("Dispositivo", "error al buscar");

            foreach (var state in new[] { BleAdapterState.Off, BleAdapterState.Unauthorized })
            {
                options = Options();
                options.Backend = HostBackends.FakeBle(null, state);
                Create(options);
                yield return Settle();
                Stop("Dispositivo", "adaptador " + state);
            }
        }

        private IEnumerator HeartRateSegment()
        {
            Create(Options());
            yield return Settle();
            var hrm = H.TestHeartRate!;
            Stop("Pulsómetro", "sin sensor");
            hrm.SetStatus(HrmStatus.Scanning);
            yield return Settle();
            Stop("Pulsómetro", "buscando");
            hrm.ConnectBehavior = ScriptedConnect.StayConnecting;
            _ = H.App.HeartRate.ConnectAsync("7E:1A:2B:3C", ScriptedHeartRateService.RememberedSample);
            yield return Settle();
            Stop("Pulsómetro", "conectando");
            hrm.Remember(ScriptedHeartRateService.RememberedSample, true);
            yield return Settle();
            Stop("Pulsómetro", "reconectando");
            hrm.Remember(ScriptedHeartRateService.RememberedSample, false);
            yield return Settle();
            Stop("Pulsómetro", "no encontrado");
            hrm.SetStatus(HrmStatus.Connected);
            yield return Settle();
            Stop("Pulsómetro", "conectado");
        }

        private IEnumerator WorkoutSegment()
        {
            var seed = RoutineSeeds.F4B;
            AddUserData(seed);
            var options = Options();
            options.Backend = HostBackends.TestRower(RowerScripts.R4);
            options.Routines = HostRoutines.Seeded(seed);
            Create(options);
            yield return GoToWorkout();
            Stop("Workout", "lista");
            var picker = Screen.Picker;
            for (var i = 0; i < picker.Count; i++)
            {
                // Como EnvironmentPickerTests: con el foco y Enter (las tarjetas de la derecha quedan fuera de la ventana en TV).
                var card = picker.Card(i);
                card.Focus();
                using (var submit = NavigationSubmitEvent.GetPooled())
                {
                    submit.target = card;
                    card.SendEvent(submit);
                }

                yield return Settle();
                Assert.That(H.App.SceneEnvironment.Selected.Value, Is.EqualTo(card.Environment), "el escenario elegido");
                Stop("Workout", "escenario " + card.Environment);
            }

            foreach (var routine in seed.Routines)
            {
                yield return OpenStartDialog(routine.Name);
                Stop("Workout", "inicio · " + routine.Name);
                yield return CloseDialog();
            }

            yield return StartRoutine("Estados");
            yield return AdvanceTo(2);
            yield return Settle();
            Stop("HUD", "rutina");
            yield return AdvanceTo(7.6);
            yield return Settle();
            Stop("HUD", "cuenta grande y próximo");
            yield return Tap("hud-pause");
            Stop("HUD", "pausa");
            yield return Tap("hud-resume");
            yield return Tap("hud-finish");
            Stop("HUD", "terminar");
            yield return CloseDialog();
        }

        private IEnumerator CompletionAndFreeSegment()
        {
            var seed = RoutineSeeds.F4A;
            AddUserData(seed);
            var options = Options();
            options.Backend = HostBackends.TestRower(RowerScripts.R4);
            options.Routines = HostRoutines.Seeded(seed);
            Create(options);
            yield return StartRoutine("Corta");
            yield return AdvanceTo(31);
            yield return Settle();
            Stop("HUD", "completado");

            options = Options();
            options.Backend = HostBackends.TestRower(RowerScripts.R4);
            Create(options);
            yield return StartFree();
            yield return AdvanceTo(5);
            yield return Settle();
            Stop("HUD", "libre");
            yield return Tap("hud-pause");
            Stop("HUD", "libre en pausa");
        }

        private IEnumerator SeriesSegment()
        {
            var seed = RoutineSeeds.F5;
            AddUserData(seed);
            foreach (var open in new[] { true, false })
            {
                var options = Options();
                options.Backend = HostBackends.TestRower(RowerScripts.R4);
                options.Routines = HostRoutines.Seeded(seed);
                var preferences = new MemoryPreferences();
                preferences.SetBool(SeriesPanelPrefs.LapsKey, open);
                preferences.SetBool(SeriesPanelPrefs.RepsKey, open);
                options.Preferences = preferences;
                Create(options);
                yield return StartRoutine("Series 600");
                if (open)
                {
                    yield return AdvanceTo(2);
                    yield return Settle();
                    Stop("Series", "abiertas sin parciales");
                }

                yield return AdvanceTo(60.2);
                yield return Settle();
                Stop("Series", open ? "abiertas con parciales" : "cerradas");
            }

            var reps = Options();
            reps.Backend = HostBackends.TestRower(RowerScripts.R4);
            reps.Routines = HostRoutines.Seeded(seed);
            var repsPrefs = new MemoryPreferences();
            repsPrefs.SetBool(SeriesPanelPrefs.LapsKey, true);
            repsPrefs.SetBool(SeriesPanelPrefs.RepsKey, true);
            reps.Preferences = repsPrefs;
            Create(reps);
            yield return StartRoutine("Cinco reps");
            yield return AdvanceTo(140);
            yield return Settle();
            Stop("Series", "repeticiones");
        }

        private IEnumerator RoutinesSegment()
        {
            var seed = RoutineSeeds.F5;
            AddUserData(seed);
            var options = Options();
            options.Routines = HostRoutines.Seeded(seed);
            Create(options);
            yield return Tap("tab-routines");
            Stop("Rutinas", "lista");
            yield return Tap("routine-menu-0");
            Stop("Rutinas", "menú");
            yield return Tap("routine-menu-delete");
            Stop("Rutinas", "borrar");
            yield return CloseDialog();
            yield return Tap("routine-card-0");
            Stop("Editor", "rutina existente");
            yield return Back();
            yield return CloseDialog();

            yield return Tap("routines-add");
            Stop("Editor", "nueva");
            yield return ScrollAndTap("editor-add-step");
            Stop("Editor", "diálogo de paso");
            yield return CloseDialog();
            yield return ScrollAndTap("editor-add-series");
            Stop("Editor", "con serie");
            yield return ScrollAndTap("series-repeat-0");
            Stop("Editor", "repeticiones");
            yield return CloseDialog();
        }

        private IEnumerator Back()
        {
            yield return Frames();
            H.PressBack(RowMate.Presentation.Shell.BackSource.SystemBack);
            yield return Settle();
        }

        private IEnumerator HistorySegment()
        {
            Create(Options());
            yield return Tap("tab-history");
            Stop("Historial", "vacío");

            var failing = Options();
            failing.Sessions = (scheduler, logger, routines) =>
            {
                var repository = new ScriptedSessionRepository(scheduler);
                HistorySeeds.Apply(routines.Repository, repository.Store, HistorySeeds.F9);
                repository.HistoryFailures = 2;
                return repository;
            };
            Create(failing);
            yield return Tap("tab-history");
            Stop("Historial", "error");

            var seeded = Options();
            seeded.UseHistory(HistorySeeds.F9);
            Create(seeded);
            yield return Tap("tab-history");
            H.Shell.HistoryScreen!.List.FinishBatches();
            yield return Settle();
            Stop("Historial", "lista");
            H.Tap("history-card-0");
            yield return Settle();
            var page = (SessionDetailPage)H.Shell.Pages.Top!;
            page.Chart.FinishEntry();
            yield return Frames();
            Stop("Detalle", "watts");
            foreach (var metric in new[] { "spm", "split", "hr", "distance" })
            {
                yield return Tap("session-metric-" + metric);
                page.Chart.FinishEntry();
                yield return Frames();
                Stop("Detalle", metric);
            }

            var exporter = H.FakeExporter!;
            exporter.Hold();
            yield return Tap(ExportTcxButton.ElementName);
            yield return Frames(2);
            Stop("Detalle", "exportando");
            exporter.Release();
            for (var i = 0; i < 240 && !(H.App.Export.Busy.Value.Count == 0 && H.Shell.Toasts.IsVisible); i++)
            {
                yield return null;
            }

            yield return Settle();
            foreach (var file in exporter.Files)
            {
                _tour.UserData.Add(file.FileName);
            }

            Stop("Detalle", "guardado");
            H.Advance(10);
            yield return Settle();
            exporter.FailNext(new System.IO.IOException("disco lleno"));
            yield return Tap(ExportTcxButton.ElementName);
            for (var i = 0; i < 240 && !(H.App.Export.Busy.Value.Count == 0 && H.Shell.Toasts.IsVisible); i++)
            {
                yield return null;
            }

            yield return Settle();
            Stop("Detalle", "error al exportar");
        }
    }

    // AC-APP-11-01 (QA-V14-04): ningún texto visible fuera del catálogo, en es y en, en todo el recorrido.
    public sealed class TextAuditTourTests : TextTourBase
    {
        [UnityTest, Category("AC-APP-11-01")]
        public IEnumerator EveryStopOnlyShowsCatalogTexts([Values("es-AR", "en-US")] string language)
        {
            yield return RunTour(language);
            Assert.That(Tour.Stops, Is.GreaterThanOrEqualTo(50), "el recorrido entero");
            Assert.That(Tour.Problems, Is.Empty, "textos fuera del catálogo:\n" + string.Join("\n", Tour.Problems));
        }
    }

    // AC-APP-11-02 (QA-V14-04; la tabla de F-APP-11 de app-shell-platform.md): en en, ninguno de los fijos del legacy se ve en español; en
    // es, el texto del legacy (salvo Q-02, el plural) y ni "Up next:" ni "Unknown device".
    public sealed class LegacyFixedTextsTests : TextTourBase
    {
        // Los textos fijos del legacy que alcanzan una pantalla viva (los del simulador y del diagnóstico BLE quedan fijos, Q-03/Q-04).
        private static readonly string[] SpanishOnly =
        {
            "Terminar entrenamiento", "Se guardará la sesión. ¿Continuar?", "Cancelar", "Terminar", "Reanudar", "Pausa", "Entrenamiento Libre",
            "EN MARCHA", "PAUSADO", "FINALIZADO", "Tiempo", "Distancia", "Calorías", "Parciales 500 m", "Series anteriores",
            "Sin parciales en este paso", "Sin repeticiones completas", "Eliminar", "Dispositivo desconocido", "Próximo:",
        };

        private static readonly string[] SpanishFragments = { " descansos", " descanso", " pasos", "Error al escanear", "Error al conectar" };

        [UnityTest, Category("AC-APP-11-02")]
        public IEnumerator InEnglishNoneOfTheLegacyFixedTextsIsSpanish()
        {
            yield return RunTour("en-US");
            var found = new List<string>();
            foreach (var pair in Tour.Seen)
            {
                if (Array.IndexOf(SpanishOnly, pair.Value) >= 0)
                {
                    found.Add(pair.Key + " · \"" + pair.Value + "\"");
                    continue;
                }

                foreach (var fragment in SpanishFragments)
                {
                    if (pair.Value.Contains(fragment))
                    {
                        found.Add(pair.Key + " · \"" + pair.Value + "\"");
                        break;
                    }
                }
            }

            Assert.That(found, Is.Empty, "fijos del legacy en español con la app en inglés:\n" + string.Join("\n", found));
            Assert.That(SeenExactly("Finish workout"), Is.True, "el diálogo de terminar, localizado");
            Assert.That(SeenExactly("500 m splits"), Is.True, "el panel de parciales, localizado");
            Assert.That(SeenContaining(" rest"), Is.True, "el resumen de la rutina, localizado (1 interval · 0 rests)");
        }

        [UnityTest, Category("AC-APP-11-02")]
        public IEnumerator InSpanishTheLegacyTextsAndNoEnglishLeftovers()
        {
            yield return RunTour("es-AR");
            foreach (var pair in Tour.Seen)
            {
                Assert.That(pair.Value, Does.Not.Contain("Up next"), pair.Key);
                Assert.That(pair.Value, Is.Not.EqualTo("Unknown device"), pair.Key);
            }

            foreach (var legacy in new[] { "Terminar entrenamiento", "Entrenamiento Libre", "EN MARCHA", "PAUSADO", "Parciales 500 m", "Dispositivo desconocido" })
            {
                Assert.That(SeenExactly(legacy), Is.True, "el texto del legacy: " + legacy);
            }

            Assert.That(SeenContaining(" descanso"), Is.True, "el resumen de la rutina (Q-02: con plural)");
            // "Rower" (deviceRowerFallback) sale solo sin remo asociado (el simulador desconectado); el remo conectado sin nombre muestra
            // "Dispositivo desconocido" (DV-101), que es lo que alcanza este recorrido.
        }

        private bool SeenExactly(string text)
        {
            foreach (var pair in Tour.Seen)
            {
                if (pair.Value == text)
                {
                    return true;
                }
            }

            return false;
        }

        private bool SeenContaining(string fragment)
        {
            foreach (var pair in Tour.Seen)
            {
                if (pair.Value.Contains(fragment))
                {
                    return true;
                }
            }

            return false;
        }
    }

    // AC-APP-15-10 (QA-V14-06): al rotar (TV ↔ TH) o redimensionar (W1280 ↔ L640) no se pierde nada: el reloj de la sesión, el texto y el
    // foco del editor, el diálogo abierto y la pestaña. El primer cuadro ya tiene el layout nuevo; en el HUD, las piezas entran en 200 ms
    // (ease.enter) y un segundo cambio antes de 400 ms queda en el acto; con reducir movimiento, todo en el acto.
    public sealed class ResizeContinuityTests : WorkoutUiTestBase
    {
        private float SpmOpacity => Hud.PieceOf(HudPiece.Spm)!.resolvedStyle.opacity;

        // En la ventana, aunque esté en opacidad 0 (Inside exige que se vea).
        private bool InWindow(string name)
        {
            var window = H.Root.worldBound;
            var b = H.BoundsOf(name);
            return b.width > 0 && b.xMin >= window.xMin - 0.5f && b.yMin >= window.yMin - 0.5f && b.xMax <= window.xMax + 0.5f && b.yMax <= window.yMax + 0.5f;
        }

        [UnityTest, Category("AC-APP-15-10")]
        public IEnumerator HudKeepsTheSessionAndEntersInTheNewLayout()
        {
            CreateWorkout(RoutineSeeds.F4B);
            yield return StartRoutine("Estados");
            yield return AdvanceTo(5);
            yield return Settle();
            var page = Immersive;
            var clock = H.TextOf("hud-time-value");
            var phase = Phase;
            Assert.That(Immersive.Layout!.IsLandscape, Is.False);
            var portraitControls = Bounds("hud-controls");
            H.Resize(DeviceProfile.TH);
            yield return null;
            Assert.That(H.Immersive, Is.SameAs(page), "el mismo inmersivo");
            Assert.That(H.Root.worldBound.width, Is.GreaterThan(H.Root.worldBound.height), "el primer cuadro ya es horizontal");
            Assert.That(Immersive.Layout!.IsLandscape, Is.True, "el layout horizontal desde el primer cuadro");
            Assert.That(Bounds("hud-controls"), Is.Not.EqualTo(portraitControls), "los controles ya en su lugar del layout nuevo");
            Assert.That(InWindow("hud-controls"), Is.True, "los controles adentro de la ventana nueva");
            Assert.That(InWindow("hud-spm"), Is.True, "SPM adentro de la ventana nueva (con opacidad 0: todavía no 'se ve')");
            Assert.That(H.TextOf("hud-time-value"), Is.EqualTo(clock), "el reloj de la sesión sigue igual");
            Assert.That(Phase, Is.EqualTo(phase), "sin reiniciar la sesión");
            Assert.That(SpmOpacity, Is.LessThan(0.05f), "las piezas arrancan en 0 en el layout nuevo");
            H.Advance(0.1);
            yield return null;
            Assert.That(SpmOpacity, Is.GreaterThan(0.05f).And.LessThan(0.999f), "a mitad de los 200 ms");
            H.Advance(0.12);
            yield return Frames();
            Assert.That(SpmOpacity, Is.EqualTo(1f).Within(0.001f), "entraron a los 200 ms");

            // La regla de los 400 ms: ida y vuelta rápida, en el acto.
            H.Resize(DeviceProfile.TV);
            yield return null;
            H.Advance(0.1);
            H.Resize(DeviceProfile.TH);
            yield return null;
            Assert.That(SpmOpacity, Is.EqualTo(1f).Within(0.001f), "otro cambio antes de 400 ms: en el acto");
            Assert.That(H.Immersive, Is.SameAs(page));
            Assert.That(Phase, Is.EqualTo(phase), "la sesión sigue");
        }

        [UnityTest, Category("AC-APP-15-10")]
        public IEnumerator ReduceMotionIsAtOnce()
        {
            CreateWorkout(RoutineSeeds.F4B, o => o.ReduceMotion = true);
            yield return StartRoutine("Estados");
            yield return AdvanceTo(3);
            yield return Settle();
            H.Resize(DeviceProfile.TH);
            yield return null;
            Assert.That(SpmOpacity, Is.EqualTo(1f).Within(0.001f), "reducir movimiento: sin entrada");
            Assert.That(Inside("hud-spm"), Is.True);
        }

        [UnityTest, Category("AC-APP-15-10")]
        public IEnumerator EditorKeepsTextFocusDialogAndTab()
        {
            CreateWorkout(RoutineSeeds.F5, o =>
            {
                o.Profile = DeviceProfile.W1280;
                o.IsDesktop = true;
            });
            yield return Tap("tab-routines");
            yield return Tap("routines-add");
            var editor = H.Editor;
            Assert.That(editor, Is.Not.Null);
            yield return Frames();
            H.Type("editor-name", "Mi rutina de prueba");
            yield return Frames();
            var field = H.FocusedField;
            Assert.That(field, Is.Not.Null, "el campo con foco");
            var tab = H.App.Shell.SelectedIndex.Value;
            H.Resize(DeviceProfile.L640);
            yield return Frames(3);
            Assert.That(H.Editor, Is.SameAs(editor), "el mismo editor");
            Assert.That(H.Find<RowMate.Presentation.Controls.RmTextField>("editor-name")!.Value, Is.EqualTo("Mi rutina de prueba"), "el texto");
            Assert.That(H.FocusedField, Is.SameAs(field), "el foco");
            Assert.That(H.App.Shell.SelectedIndex.Value, Is.EqualTo(tab), "la pestaña");
            AssertClean(H);

            H.Tap("editor-add-step");
            yield return Settle();
            var dialog = H.TopDialog;
            Assert.That(dialog, Is.Not.Null, "el diálogo de paso");
            H.Resize(DeviceProfile.W1280);
            yield return Frames(3);
            Assert.That(H.TopDialog, Is.SameAs(dialog), "el mismo diálogo, abierto");
            Assert.That(H.Root.worldBound.Contains(dialog!.Box.worldBound.center), Is.True, "a la vista en la ventana nueva");
            H.Resize(DeviceProfile.L640);
            yield return Frames(3);
            Assert.That(H.TopDialog, Is.SameAs(dialog), "y de vuelta");
            Assert.That(H.App.Shell.SelectedIndex.Value, Is.EqualTo(tab));
            Assert.That(H.Editor, Is.SameAs(editor));
        }
    }

    // AC-DEBT-07 (QA-V14-07, WD-03): con un split constante (la sesión libre de 133 s), el eje del split se abre a ±5 s: la curva queda
    // en el medio y las etiquetas cerca de 2:13, no de 2:13 a 0:33 con la curva en el piso.
    public sealed class SplitRangeChartTests : HistoryUiTestBase
    {
        [UnityTest, Category("AC-DEBT-07")]
        public IEnumerator ConstantSplitOpensTheAxis()
        {
            CreateScriptedHistory(r => HistorySeeds.AddFinished(r.Store, HistorySeeds.Local(2026, 11, 6, 9, 0)));
            yield return GoToHistory();
            yield return OpenCard(0);
            var chart = Detail.Chart;
            yield return Tap("session-metric-split");
            chart.FinishEntry();
            yield return Frames();
            var labels = chart.Labels.YTexts();
            Assert.That(labels.Length, Is.GreaterThanOrEqualTo(2), string.Join(", ", labels));
            Assert.That(labels[0], Is.EqualTo("2:18"), "el piso a 5 s del ritmo (133 + 5)");
            foreach (var label in labels)
            {
                Assert.That(label, Does.StartWith("2:"), "todas cerca del ritmo real (" + string.Join(", ", labels) + ")");
            }

            Assert.That(chart.Visual.YMax, Is.EqualTo(11).Within(1e-6), "10 s de rango con el 10 % de aire");
        }
    }
}
