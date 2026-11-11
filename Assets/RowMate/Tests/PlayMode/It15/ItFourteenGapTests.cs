using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using RowMate.App.Device;
using RowMate.Core.Ble;
using RowMate.Core.Design;
using RowMate.Core.Ports;
using RowMate.Presentation.Accessibility;
using RowMate.Presentation.Shell;
using RowMate.Presentation.Workout;
using RowMate.Tests.Support;
using RowMate.Tests.UiSupport;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace RowMate.Tests.PlayMode.It15
{
    // It. 15 (#81, #82; AC-D15-30; 15-deuda-pc/02-tech-design §2.3, §6, DT15-20, gate QA-G15-03; QA-V15-02): las pruebas de la It. 14 que
    // faltaban (14-pulido-plataformas/02-tech-design §10, QA-V14-08). WindowMode está en Tests/UnityEditMode/It15GapTests.cs: AsmdefLintTests
    // no deja que PlayMode referencie RowMate.Platform (ahí vive WindowModeController) y las teclas se leen con el Input System de prueba.
    public sealed class ItFourteenGapTests : WorkoutUiTestBase
    {
        // AC-APP-15-06: a 640 × 360 (L640, la ventana mínima) las 6 pantallas sin desbordes ni textos cortados, y sin perder el estado
        // (la pestaña, la página de arriba y el entrenamiento siguen siendo los mismos objetos).
        [UnityTest, Category("AC-D15-30"), Category("AC-APP-15-06")]
        public IEnumerator MinWindowLayout()
        {
            CreateWorkout(configure: o =>
            {
                o.Profile = DeviceProfile.W1280;
                o.IsDesktop = true;
                o.UseHistory(HistorySeeds.F9);
            });
            yield return Settle();
            var problems = new List<string>();

            yield return AtMinimum("Dispositivo", problems);

            yield return Tap("tab-routines");
            yield return AtMinimum("Rutinas", problems);
            yield return Tap("routine-card-0");
            Assert.That(H.Editor, Is.Not.Null, "el editor abierto");
            yield return AtMinimum("Editor", problems);
            H.PressBack(BackSource.SystemBack);
            yield return Settle();
            Assert.That(H.Editor, Is.Null, "el editor se cerró sin cambios (diálogo: " + (H.TopDialog?.GetType().Name ?? "ninguno") + ")");

            yield return Tap("tab-history");
            H.Shell.HistoryScreen!.List.FinishBatches();
            yield return Settle();
            yield return AtMinimum("Historial", problems);
            yield return Tap("history-card-0");
            yield return AtMinimum("Detalle", problems);
            H.PressBack(BackSource.SystemBack);
            yield return Settle();

            yield return StartFree();
            yield return AdvanceTo(5);
            yield return AtMinimum("Workout (HUD)", problems);
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        private IEnumerator AtMinimum(string screen, List<string> problems)
        {
            var tab = H.App.Shell.SelectedIndex.Value;
            var top = H.Shell.Pages.Top;
            var session = H.App.Workout.State.Value.Session;
            H.Resize(DeviceProfile.L640);
            yield return Settle();
            var audit = H.Audit();
            if (!audit.IsClean)
            {
                problems.Add(screen + " a 640 × 360: " + audit);
            }

            if (H.App.Shell.SelectedIndex.Value != tab || !ReferenceEquals(H.Shell.Pages.Top, top) || H.App.Workout.State.Value.Session != session)
            {
                problems.Add(screen + ": el estado cambió al achicar la ventana");
            }

            H.Resize(DeviceProfile.W1280);
            yield return Settle();
        }

        // AC-POL-03, AC-DEBT-10: los 5 eventos de la 14 con el háptico falso (la política de HapticGate en el medio): leve al pausar y
        // al reanudar, medio al cambiar de paso, éxito al completar la rutina y la de "Listo" al reconectar el remo (además de la de la
        // conexión del usuario de la 10a).
        [UnityTest, Category("AC-D15-30"), Category("AC-POL-03"), Category("AC-DEBT-10")]
        public IEnumerator HapticEvents()
        {
            var haptics = new FakeHaptics();
            CreateWorkout(RoutineSeeds.F4A, o => o.Haptics = haptics);
            yield return StartRoutine("Corta");
            yield return AdvanceTo(3);
            Assert.That(haptics.Kinds, Is.Empty, "empezar no vibra");
            yield return Tap("hud-pause");
            Assert.That(haptics.Kinds, Is.EqualTo(new[] { HapticKind.Light }), "pausar: leve");
            H.Advance(1);
            yield return Frames();
            yield return Tap("hud-resume");
            Assert.That(haptics.Kinds, Is.EqualTo(new[] { HapticKind.Light, HapticKind.Light }), "reanudar: leve");
            yield return AdvanceTo(16);
            Assert.That(haptics.Kinds.Skip(2).FirstOrDefault(), Is.EqualTo(HapticKind.Medium), "cambio de paso: medio");
            yield return AdvanceTo(40);
            Assert.That(Phase, Is.EqualTo(RowMate.Core.Workout.WorkoutPhase.Finished), "Corta completada");
            Assert.That(haptics.Kinds[haptics.Kinds.Count - 1], Is.EqualTo(HapticKind.Success), "completada: éxito");
            Assert.That(haptics.Kinds.Count(k => k == HapticKind.Success), Is.EqualTo(1), "una vez por sesión");
            Assert.That(haptics.Kinds.Skip(2).Take(haptics.Kinds.Count - 3), Is.All.EqualTo(HapticKind.Medium), string.Join(", ", haptics.Kinds));

            var link = new FakeHaptics();
            Create(new UiHostOptions { Profile = DeviceProfile.TV, Backend = HostBackends.FakeBle(), Haptics = link });
            yield return Settle();
            var adapter = H.FakeBle!.Adapter;
            _ = H.App.Device.StartScanAsync();
            adapter.Advertise(FakeBleDevices.Row);
            yield return Frames();
            _ = H.App.Device.ConnectAsync(FakeBleDevices.RowId);
            H.Advance(1);
            yield return Settle();
            Assert.That(H.App.Device.State.Value.Status, Is.EqualTo(BleStatus.Connected));
            Assert.That(link.Kinds, Is.EqualTo(new[] { HapticKind.Confirm }), "Listo (10a)");
            H.Advance(10);
            yield return Settle();
            adapter.DropLink(FakeBleDevices.RowId);
            yield return Settle(0.5);
            Assert.That(link.Kinds.Count, Is.EqualTo(1), "la caída no vibra");
            H.Advance(4);
            yield return Settle();
            Assert.That(H.App.Device.State.Value.Status, Is.EqualTo(BleStatus.Connected), "reconectado");
            Assert.That(link.Kinds, Is.EqualTo(new[] { HapticKind.Confirm, HapticKind.Confirm }), "Reconectado con … vibra como Listo, una vez");
        }

        // AC-DEBT-03 (#42): TH buscando: la lista y la tarjeta del pulsómetro comparten UN SOLO desplazamiento, la lista sin alto fijo, y
        // "Buscar" del pulsómetro se alcanza desplazando (no queda bajo la barra de pestañas).
        [UnityTest, Category("AC-D15-30"), Category("AC-DEBT-03")]
        public IEnumerator DeviceLandscapeScroll()
        {
            var prefs = new RowMate.Core.Common.MemoryPreferences();
            Create(new UiHostOptions { Profile = DeviceProfile.TH }.WithRealHrm(prefs));
            yield return Settle();
            H.Tap("device-search");
            var adapter = H.FakeBle!.Adapter;
            adapter.Advertise(FakeBleDevices.Row);
            adapter.Advertise(FakeBleDevices.Anon);
            adapter.Advertise(FakeBleDevices.Bike);
            yield return Frames();
            H.Advance(3);
            yield return Frames(3);
            var scan = H.DeviceScreen!.Scan;
            Assert.That(scan.Layout.Kind, Is.EqualTo(DeviceScanLayoutKind.TwoColumns), "TH: dos columnas");
            Assert.That(scan.Results.childCount, Is.GreaterThan(0), "con resultados");
            var scrolls = scan.Query<ScrollView>().ToList().Where(UiQuery.IsShown).ToList();
            Assert.That(scrolls.Select(s => s.name), Is.EqualTo(new[] { scan.CardSlot.name }), "un solo desplazamiento");
            Assert.That(scan.CardSlot.contentContainer.Contains(scan.Results), Is.True, "la lista adentro del mismo desplazamiento");
            Assert.That(scan.Results.style.height.keyword, Is.EqualTo(StyleKeyword.Null), "la lista sin alto fijo");
            Assert.That(scan.Results.resolvedStyle.height, Is.GreaterThan(0));
            var search = H.Find("hrm-search");
            Assert.That(search, Is.Not.Null, "Buscar del pulsómetro");
            Assert.That(scan.CardSlot.contentContainer.Contains(search!), Is.True, "en el mismo desplazamiento");
            H.ScrollTo("hrm-search");
            yield return Frames(3);
            var bounds = search!.worldBound;
            var viewport = scan.CardSlot.contentViewport.worldBound;
            Assert.That(bounds.yMin, Is.GreaterThanOrEqualTo(viewport.yMin - 0.5f), "Buscar a la vista (arriba)");
            Assert.That(bounds.yMax, Is.LessThanOrEqualTo(Math.Min(viewport.yMax, H.BoundsOf("tab-bar").yMin) + 0.5f), "Buscar no queda bajo la barra");
            Assert.That(H.WhyNotTappable("hrm-search"), Is.Null, "se puede tocar");
        }

        // #21 (arte 82-c, gate QA-G15-03, caso a): la escena queda lista ANTES de que termine el negro de la entrada (220 ms): el vuelo de
        // 1 600 ms entero, anclado a los 220 del toque, sin "en su lugar" y sin el indicador.
        [UnityTest, Category("AC-D15-30"), Category("AC-DEBT-09")]
        public IEnumerator SceneLoadingBeforeBlackEnds()
        {
            var fake = new FakeImmersiveScene(new RecordingLogger());
            CreateWorkout(configure: o => o.FakeScene = fake);
            yield return GoToWorkout();
            yield return Frames();
            H.Tap("workout-free");
            var tap = H.Scheduler.MonotonicSeconds;
            var indicator = 0.0;
            var ready = false;
            while (H.Scheduler.MonotonicSeconds < tap + 2.2)
            {
                if (!ready && H.Scheduler.MonotonicSeconds >= tap + 0.1 - 1e-9)
                {
                    fake.SetReadiness(ImmersiveSceneReadiness.Ready);
                    ready = true;
                }

                H.Step();
                yield return null;
                indicator = Math.Max(indicator, IndicatorOpacity());
            }

            Assert.That(fake.Flights, Is.EqualTo(1), "con el vuelo");
            Assert.That(fake.InPlace, Is.EqualTo(0), "sin aparecer en su lugar");
            Assert.That(fake.LastFlightStart, Is.EqualTo(SceneTimelines.FlightStartSeconds(tap)).Within(1e-6), "el vuelo desde los 220 ms del toque: entero");
            Assert.That(Hud.SceneShown, Is.True);
            Assert.That(indicator, Is.EqualTo(0), "lista antes de la gracia de 120 ms: el indicador no se ve");
        }

        // #21 (arte 82-c, gate QA-G15-03, caso b): la escena queda lista DESPUÉS (a los 2 s): "Preparando escena…" a la vista mientras
        // tanto; en el mismo cuadro en que queda lista, en su lugar (sin vuelo) con el fundido de 600 ms sobre el provisorio, y el indicador
        // a 0 en 160 ms. Ningún cuadro con los dos opacos a la vez.
        [UnityTest, Category("AC-D15-30"), Category("AC-DEBT-09"), Category("AC-SCN-01-07")]
        public IEnumerator SceneLoadingDelayed()
        {
            var fake = new FakeImmersiveScene(new RecordingLogger());
            CreateWorkout(configure: o => o.FakeScene = fake);
            yield return GoToWorkout();
            yield return Frames();
            H.Tap("workout-free");
            var tap = H.Scheduler.MonotonicSeconds;
            var t0 = double.NaN;
            var both = new List<string>();
            var indicatorBefore = 0.0;
            var indicatorAfter = 0.0;
            var sceneAtReady = double.NaN;
            var sceneMid = double.NaN;
            while (H.Scheduler.MonotonicSeconds < tap + 3.0)
            {
                var now = H.Scheduler.MonotonicSeconds;
                if (double.IsNaN(t0) && now >= tap + 2.0 - 1e-9)
                {
                    fake.SetReadiness(ImmersiveSceneReadiness.Ready);
                    t0 = now;
                    Assert.That(fake.InPlace, Is.EqualTo(1), "en su lugar en el mismo cuadro en que queda lista");
                    Assert.That(Hud.SceneShown, Is.True);
                    sceneAtReady = SceneOpacity();
                }

                H.Step();
                yield return null;
                now = H.Scheduler.MonotonicSeconds;
                var scene = SceneOpacity();
                var indicator = IndicatorOpacity();
                if (double.IsNaN(t0))
                {
                    indicatorBefore = Math.Max(indicatorBefore, indicator);
                    Assert.That(scene, Is.EqualTo(0), "la escena no se ve antes de estar lista");
                }
                else
                {
                    if (now - t0 > SceneLoadingSpecs.FadeMs / 1000.0 + 1e-6)
                    {
                        indicatorAfter = Math.Max(indicatorAfter, indicator);
                    }

                    if (double.IsNaN(sceneMid) && now - t0 >= 0.3 - 1e-6)
                    {
                        sceneMid = scene;
                    }
                }

                if (scene >= 0.99 && indicator >= 0.99)
                {
                    both.Add(((now - tap) * 1000).ToString("0") + " ms");
                }
            }

            Assert.That(fake.Flights, Is.EqualTo(0), "sin vuelo (AC-SCN-01-07)");
            Assert.That(indicatorBefore, Is.GreaterThanOrEqualTo(0.99), "Preparando escena… a la vista mientras espera");
            Assert.That(indicatorAfter, Is.EqualTo(0), "el indicador a 0 a los 160 ms");
            Assert.That(sceneAtReady, Is.EqualTo(0).Within(1e-6), "el fundido arranca en 0");
            Assert.That(sceneMid, Is.GreaterThan(0.05).And.LessThan(0.99), "a mitad del fundido de 600 ms");
            Assert.That(SceneOpacity(), Is.EqualTo(1), "entera después de 600 ms");
            Assert.That(Hud.Backdrop.style.display.value, Is.EqualTo(DisplayStyle.None), "el provisorio se va al terminar el fundido");
            Assert.That(both, Is.Empty, "cuadros con los dos opacos");
        }

        private double SceneOpacity()
        {
            var surface = Immersive.HasView ? Hud.SceneSurface : null;
            if (surface == null || surface.style.display == DisplayStyle.None)
            {
                return 0;
            }

            var opacity = surface.style.opacity;
            return opacity.keyword == StyleKeyword.Null ? 1 : opacity.value;
        }

        private double IndicatorOpacity()
        {
            var indicator = Immersive.HasView ? Hud.SceneLoading : null;
            if (indicator == null || !indicator.IsShown)
            {
                return 0;
            }

            var opacity = indicator.style.opacity;
            return opacity.keyword == StyleKeyword.Null ? 1 : opacity.value;
        }
    }

    // AC-D15-29 (#80; QA-V15-04): lo que cuesta el lector. Apagado, el HUD no asigna en 120 cuadros con el puente vivo (0 B en el paso y en
    // los dibujos; el reloj de prueba, solo las lecturas de 1 Hz). Encendido, las métricas que cambian actualizan nombres y valores de los
    // nodos que ya están, sin reconstruir el árbol (sin Clear ni reconciliaciones, los mismos AccessibilityNode).
    public sealed class ScreenReaderCostTests : WorkoutUiTestBase
    {
        private const int WindowFrames = 120;

        [UnityTest, Category("AC-D15-29"), Category("AC-APP-03-15")]
        public IEnumerator ReaderOffHudZeroBytesPerFrame()
        {
            CreateWorkout();
            var previous = AssistiveSupport.screenReaderStatusOverride;
            AssistiveSupport.screenReaderStatusOverride = AssistiveSupport.ScreenReaderStatusOverride.ForceDisabled;
            var bridge = new ScreenReaderBridge(H.Shell, new FakeA11yAnnouncer());
            try
            {
                yield return StartFree();
                yield return AdvanceTo(6.2);
                var dt = TimeSpan.FromTicks((long)Math.Round(UiTestHost.FrameSeconds * TimeSpan.TicksPerSecond));
                Action advance = () => H.Scheduler.Advance(dt);
                Action tick = () =>
                {
                    H.Handle.Tick();
                    bridge.Tick(H.Scheduler.MonotonicSeconds);
                };
                // Calentamiento: unos cuadros con el puente y el HUD (los caminos por primera vez).
                for (var i = 0; i < 30; i++)
                {
                    advance();
                    tick();
                    yield return null;
                }

                using (var measured = FrameAllocations.WatchTree(Immersive.Root))
                {
                    AllocationProfile.Begin("reader-off-hud");
                    for (var i = 0; i < WindowFrames; i++)
                    {
                        measured.Clock(advance);
                        measured.Step(tick);
                        yield return null;
                        measured.Sample();
                    }

                    AllocationProfile.End();
                    measured.Stop();
                    Debug.Log("[SRC] lector apagado, HUD: " + measured);
                    Assert.That(measured.Frames, Is.EqualTo(WindowFrames));
                    Assert.That(measured.DrawAllocations, Is.EqualTo(0), "los dibujos del HUD no asignan (" + measured.AllocatingElements() + ")");
                    Assert.That(measured.StepAllocations, Is.EqualTo(0), "el paso del cuadro (HUD y puente) no asigna: " + measured);
                    // Lo de 1 Hz (§20.2) en el reloj de prueba: la lectura del remo (L(k) en el instante k) y el segundo del motor con su Apply
                    // caen en cuadros distintos, así que son 2 cuadros por segundo: 4 en los 2 s de la ventana (medido: 4).
                    Assert.That(measured.ClockFramesWithAllocations, Is.LessThanOrEqualTo(4), "el reloj asigna solo a 1 Hz: " + measured);
                }

                Assert.That(bridge.IsActive, Is.False);
                Assert.That(bridge.Hierarchy, Is.Null, "sin árbol con el lector apagado");
                Assert.That(GcAllocations.During(() =>
                {
                    for (var i = 0; i < WindowFrames; i++)
                    {
                        bridge.Tick(i);
                    }
                }), Is.EqualTo(0), "el puente apagado: una lectura de un bool");
            }
            finally
            {
                bridge.Dispose();
                AssistiveSupport.screenReaderStatusOverride = previous;
            }
        }

        [UnityTest, Category("AC-D15-29"), Category("AC-APP-03-13")]
        public IEnumerator ReaderOnMetricsDoNotRebuildTheTree()
        {
            CreateWorkout(configure: o => o.ScreenReader = true);
            yield return StartFree();
            yield return AdvanceTo(3.2);
            var bridge = H.ScreenReader!;
            bridge.RebuildNow();
            Assert.That(bridge.IsActive, Is.True);
            var watts = bridge.NodeOf(Hud.Watts);
            var spm = bridge.NodeOf(Hud.Spm);
            Assert.That(watts, Is.Not.Null, "Vatios es un nodo");
            Assert.That(spm, Is.Not.Null, "SPM es un nodo");
            var clears = bridge.ClearCount;
            var reconciles = bridge.ReconcileCount;
            var labels = bridge.LabelRefreshCount;
            var frames = bridge.FrameRefreshCount;
            var items = bridge.Items.Count;
            var label = watts!.label;
            for (var second = 0; second < 6; second++)
            {
                for (var i = 0; i < 60; i++)
                {
                    H.Step();
                    if (i % 15 == 0)
                    {
                        yield return null;
                    }
                }

                yield return null;
            }

            Assert.That(bridge.FrameRefreshCount, Is.GreaterThan(frames), "el recorrido de 250 ms siguió corriendo");
            Assert.That(bridge.LabelRefreshCount, Is.GreaterThan(labels), "los nombres de las métricas se actualizaron");
            Assert.That(bridge.ClearCount, Is.EqualTo(clears), "sin Clear()");
            Assert.That(bridge.ReconcileCount, Is.EqualTo(reconciles), "sin nodos nuevos, borrados ni movidos");
            Assert.That(bridge.Items.Count, Is.EqualTo(items));
            Assert.That(bridge.NodeOf(Hud.Watts), Is.SameAs(watts), "el mismo nodo de Vatios");
            Assert.That(bridge.NodeOf(Hud.Spm), Is.SameAs(spm), "el mismo nodo de SPM");
            Assert.That(label, Is.Not.Empty);
        }
    }
}
