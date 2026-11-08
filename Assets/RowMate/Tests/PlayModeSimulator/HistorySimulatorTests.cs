using System;
using System.Collections;
using NUnit.Framework;
using RowMate.App.History;
using RowMate.Presentation.History;
using RowMate.Tests.Support;
using RowMate.Tests.UiSupport;
using UnityEngine.TestTools;

namespace RowMate.Tests.PlayMode.Simulator
{
    // HSIM · El historial con el simulador (09-historial/02-tech-design §8.2; demo de la spec §1): una sesión libre terminada queda
    // arriba al tocar Historial; una rutina cortada por cerrar la app aparece al volver a abrir, con sus números y su gráfico hasta
    // el corte. Base temporal (TempDatabase), simulador sin ruido.
    public sealed class HistorySimulatorTests : SimulatorUiTestBase
    {
        private UiTestHost CreateHistorySim(RoutineSeed seed)
        {
            return Create(new UiHostOptions
            {
                Profile = DeviceProfile.W1280,
                Backend = HostBackends.Simulator(false),
                SimulatorLayer = SimulatorLayerMode.Full,
                Routines = HostRoutines.Seeded(seed),
                StartUtc = UiHostOptions.WorkoutStartUtc,
            });
        }

        private HistoryScreenView Screen => H.Shell.HistoryScreen ?? throw new InvalidOperationException("sin Historial");

        [UnityTest, Category("AC-HIS-06-02")]
        public IEnumerator FinishedSessionOnTop()
        {
            CreateHistorySim(RoutineSeeds.Empty);
            yield return Tap("tab-history");
            Assert.That(H.IsShown("history-empty"), Is.True, "base vacía");
            yield return Tap("tab-workout");
            yield return Frames();
            H.Tap("workout-free");
            yield return Settle();
            Assert.That(H.Immersive, Is.Not.Null, "empezó la sesión libre");
            for (var s = 0; s < 30; s++)
            {
                H.Advance(1);
                yield return null;
            }

            yield return Tap("hud-finish");
            yield return Tap("dialog-confirm");
            yield return Settle();
            Assert.That(H.Immersive, Is.Null, "terminó");
            H.Advance(1);
            yield return Settle();
            Assert.That(H.App.History.State.Value.Entries.Count, Is.EqualTo(1), "el cierre recargó el historial");
            yield return Tap("tab-history");
            Assert.That(Screen.List.Count, Is.EqualTo(1));
            var card = Screen.List.CardAt(0)!;
            Assert.That(card.NameLabel.text, Is.EqualTo("Libre"));
            Assert.That(card.Stats.ValueAt(2).text, Is.EqualTo("150"), "150 W del simulador");
            Assert.That(H.IsShown("history-loading"), Is.False);
        }

        [UnityTest, Category("AC-HIS-19-04")]
        public IEnumerator CutSessionAfterReopen()
        {
            CreateHistorySim(RoutineSeeds.F4A);
            yield return Tap("tab-workout");
            yield return Frames();
            H.TapRoutineNamed("Prueba 4a");
            yield return Settle();
            yield return Frames();
            H.Tap("dialog-confirm");
            yield return Settle();
            H.Advance(50);
            yield return Frames();
            // Se cierra la app a mitad del trabajo (sin Terminar) y se vuelve a abrir sobre la misma base.
            H.Reopen();
            yield return Settle();
            yield return Tap("tab-history");
            Assert.That(Screen.List.Count, Is.EqualTo(1), "la cortada aparece");
            var card = Screen.List.CardAt(0)!;
            Assert.That(card.NameLabel.text, Is.EqualTo("Prueba 4a"));
            Assert.That(card.Stats.ValueAt(1).text, Is.Not.EqualTo("0m"), "con los números de sus puntos");
            H.Tap("history-card-0");
            yield return Settle();
            var detail = H.Shell.Pages.Top as SessionDetailPage;
            Assert.That(detail, Is.Not.Null);
            Assert.That(detail!.Phase, Is.EqualTo(SessionDetailPhase.Ready));
            Assert.That(detail.StepRows.Count, Is.GreaterThanOrEqualTo(1), "rendimiento por paso");
            Assert.That(detail.Chart.Current, Is.Not.Null, "gráfico hasta el corte");
        }
    }
}
