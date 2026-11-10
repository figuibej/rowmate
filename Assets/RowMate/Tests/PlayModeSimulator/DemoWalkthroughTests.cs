using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using RowMate.Core.Scene;
using RowMate.Presentation.History;
using RowMate.Presentation.Workout;
using RowMate.Tests.Support;
using RowMate.Tests.UiSupport;
using UnityEngine.TestTools;

namespace RowMate.Tests.PlayMode.Simulator
{
    // AC-POL-05 (It. 14, 05-qa-verification QA-V14-05; 01-po-spec §1, demo pasos 2 a 5): el recorrido de la demo con el simulador, en es y
    // en, con el exportador falso del anfitrión (carpeta temporal, nunca Documentos) y los 4 escenarios:
    // 2. Dispositivo con el remo simulado y las métricas en vivo.
    // 3. Rutinas: Escalera en el editor y volver; Workout: Escalera, 1 minuto de remo y Terminar.
    // 4. Un entrenamiento libre corto en cada escenario (Lago alpino, Río urbano, Costa, Canal de regata).
    // 5. Historial → la sesión → el gráfico en otra métrica → Exportar TCX y el aviso.
    // En cada pantalla, TextAuditProbe (D-01); al final, el registro de la app sin errores (y Unity sin LogError, que el Test Framework
    // ya hace fallar).
    public sealed class DemoWalkthroughTests : SimulatorUiTestBase
    {
        private readonly List<string> _problems = new List<string>();
        private readonly List<string> _userData = new List<string>();
        private double _start = double.NaN;

        [UnityTest, Category("AC-POL-05"), Category("AC-APP-11-01")]
        public IEnumerator TheDemoWithoutErrors([Values("es-AR", "en-US")] string language)
        {
            _problems.Clear();
            _userData.Clear();
            _userData.Add(HistorySeeds.EscaleraName);
            _userData.Add(HistorySeeds.DeletedRoutineName);
            var options = new UiHostOptions
            {
                Profile = DeviceProfile.W1280,
                IsDesktop = true,
                Backend = HostBackends.Simulator(false),
                SimulatorLayer = SimulatorLayerMode.Full,
                LanguageTags = new[] { language },
                StartUtc = UiHostOptions.WorkoutStartUtc,
            };
            options.UseHistory(HistorySeeds.F9);
            Create(options);
            yield return Settle();

            // 2. Dispositivo: el remo simulado conectado, con métricas.
            yield return Advance(3);
            yield return Settle();
            Assert.That(H.IsShown("device-connected-view"), Is.True, "el remo simulado conectado");
            Probe("Dispositivo", "conectado");

            // 3. Rutinas: Escalera en el editor y volver.
            yield return Tap("tab-routines");
            Probe("Rutinas", "lista");
            var routines = H.App.Routines.State.Value.Routines;
            var index = -1;
            for (var i = 0; i < routines.Count; i++)
            {
                if (routines[i].Name == HistorySeeds.EscaleraName)
                {
                    index = i;
                }
            }

            Assert.That(index, Is.GreaterThanOrEqualTo(0), "Escalera en la lista de Rutinas");
            yield return ScrollAndTap("routine-card-" + index);
            Assert.That(H.Editor, Is.Not.Null, "el editor de Escalera");
            Probe("Editor", "Escalera");
            H.PressBack(RowMate.Presentation.Shell.BackSource.SystemBack);
            yield return Settle();
            Assert.That(H.Editor, Is.Null, "volvió");

            // Workout: Escalera, 1 minuto y Terminar.
            yield return Tap("tab-workout");
            Probe("Workout", "lista");
            H.TapRoutineNamed(HistorySeeds.EscaleraName);
            yield return Settle();
            Probe("Workout", "inicio");
            H.Tap("dialog-confirm");
            _start = H.Scheduler.MonotonicSeconds;
            yield return Settle();
            yield return AdvanceTo(60);
            yield return Settle();
            Assert.That(H.Immersive, Is.Not.Null, "el inmersivo");
            Probe("HUD", "Escalera al minuto");
            yield return Finish();

            // 4. Un libre corto en cada escenario.
            for (var i = 0; i < EnvironmentCatalog.All.Length; i++)
            {
                yield return Tap("tab-workout");
                var picker = H.WorkoutScreen!.Picker;
                var card = picker.Card(i);
                yield return Tap(card.name);
                Assert.That(H.App.SceneEnvironment.Selected.Value, Is.EqualTo(card.Environment), "el escenario elegido");
                Probe("Workout", "escenario " + card.Environment);
                H.Tap("workout-free");
                _start = H.Scheduler.MonotonicSeconds;
                yield return Settle();
                yield return AdvanceTo(15);
                yield return Settle();
                Probe("HUD", "libre en " + card.Environment);
                yield return Finish();
            }

            // 5. Historial → la sesión → otra métrica → Exportar TCX.
            yield return Tap("tab-history");
            H.Shell.HistoryScreen!.List.FinishBatches();
            yield return Settle();
            Probe("Historial", "lista");
            H.Tap("history-card-0");
            yield return Settle();
            var page = H.Shell.Pages.Top as SessionDetailPage;
            Assert.That(page, Is.Not.Null, "el detalle");
            page!.Chart.FinishEntry();
            yield return Frames();
            Probe("Detalle", "gráfico");
            yield return ScrollAndTap("session-metric-split");
            page.Chart.FinishEntry();
            yield return Frames();
            Probe("Detalle", "split");
            yield return ScrollAndTap(ExportTcxButton.ElementName);
            for (var i = 0; i < 240 && !(H.App.Export.Busy.Value.Count == 0 && H.Shell.Toasts.IsVisible); i++)
            {
                yield return null;
            }

            yield return Settle();
            Assert.That(H.FakeExporter, Is.Not.Null, "el exportador falso del anfitrión");
            Assert.That(H.FakeExporter!.Files.Count, Is.EqualTo(1), "un archivo, en la carpeta temporal");
            _userData.Add(H.FakeExporter.Files[0].FileName);
            Probe("Detalle", "exportado");

            Assert.That(_problems, Is.Empty, "textos fuera del catálogo:\n" + string.Join("\n", _problems));
            Assert.That(H.HasErrors(), Is.False, "el registro sin errores:\n" + string.Join("\n", H.Logger.Lines));
        }

        private void Probe(string screen, string state)
        {
            _problems.AddRange(TextAuditProbe.Violations(H, screen, state, _userData.ToArray()));
        }

        private IEnumerator AdvanceTo(double seconds)
        {
            var target = _start + seconds;
            var now = H.Scheduler.MonotonicSeconds;
            if (target > now)
            {
                H.Advance(target - now);
            }

            yield return Frames();
        }

        // Terminar → confirmar → de vuelta en Workout (el completado no aplica: la sesión se termina a mano).
        private IEnumerator Finish()
        {
            yield return Tap("hud-finish");
            Probe("HUD", "terminar");
            yield return Tap("dialog-confirm");
            for (var i = 0; i < 120 && H.Immersive != null; i++)
            {
                H.Step();
                yield return null;
            }

            yield return Settle();
            Assert.That(H.Immersive, Is.Null, "salió del inmersivo");
        }
    }
}
