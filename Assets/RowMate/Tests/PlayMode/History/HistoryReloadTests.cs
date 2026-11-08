using System.Collections;
using NUnit.Framework;
using RowMate.App.History;
using RowMate.Tests.Support;
using RowMate.Tests.UiSupport;
using UnityEngine;
using UnityEngine.TestTools;

namespace RowMate.Tests.PlayMode.History
{
    // HRL · Recargas sin parpadeo (F-HIS-06; AC-HIS-06-02 a -04; Q-22, DV-92; 02-tech-design §5.2).
    public sealed class HistoryReloadTests : HistoryUiTestBase
    {
        [UnityTest, Category("AC-HIS-06-02")]
        public IEnumerator TapTwiceReloads()
        {
            CreateHistory(HistorySeeds.F9);
            var loads = H.App.History.Loads;
            yield return GoToHistory();
            Assert.That(H.App.History.Loads, Is.EqualTo(loads + 1), "tocar Historial recarga");
            yield return GoToHistory();
            Assert.That(H.App.History.Loads, Is.EqualTo(loads + 2), "también si ya estaba elegida");
            yield return Tap("tab-routines");
            Assert.That(H.App.History.Loads, Is.EqualTo(loads + 2), "las otras pestañas no recargan");
        }

        [UnityTest, Category("AC-HIS-06-02")]
        public IEnumerator BackFromTheDetailReloads()
        {
            CreateHistory(HistorySeeds.F9);
            yield return GoToHistory();
            yield return OpenCard(0);
            var loads = H.App.History.Loads;
            yield return Tap("session-back");
            Assert.That(DetailOrNull, Is.Null);
            Assert.That(H.App.History.Loads, Is.EqualTo(loads + 1), "volver del detalle recarga");
        }

        [UnityTest, Category("AC-HIS-06-03")]
        public IEnumerator NoFlicker()
        {
            CreateHistory(HistorySeeds.F9);
            yield return GoToHistory();
            var created = Screen.List.CreatedCount;
            var first = Card(0);
            for (var i = 0; i < 3; i++)
            {
                H.Tap("tab-history");
                for (var f = 0; f < 6; f++)
                {
                    Assert.That(H.IsShown("history-loading"), Is.False, "nunca el indicador");
                    Assert.That(H.IsShown("history-list"), Is.True, "la lista sigue a la vista");
                    H.Step();
                    yield return null;
                }
            }

            Assert.That(Screen.List.CreatedCount, Is.EqualTo(created), "lo mismo: ninguna tarjeta nueva");
            Assert.That(Card(0), Is.SameAs(first), "las tarjetas no se rehacen");
            Assert.That(Screen.List.IsAnimating, Is.False, "ni se reaniman");
        }

        [UnityTest, Category("AC-HIS-06-03")]
        public IEnumerator NewOnTopEnters()
        {
            CreateHistory(HistorySeeds.F9);
            yield return GoToHistory();
            var created = Screen.List.CreatedCount;
            var old = Card(0);
            HistorySeeds.AddFinished(H.Sessions, HistorySeeds.Local(2026, 11, 6, 9, 0));
            H.Tap("tab-history");
            H.Step();
            yield return Frames();
            Assert.That(Screen.List.Count, Is.EqualTo(7));
            Assert.That(Card(0).NameLabel.text, Is.EqualTo("Libre"), "la nueva, arriba");
            Assert.That(Card(1), Is.SameAs(old), "las demás siguen siendo las mismas");
            Assert.That(Screen.List.CreatedCount, Is.EqualTo(created + 1), "una sola tarjeta nueva");
            Assert.That(Screen.List.IsAnimating, Is.True, "la nueva entra con su animación");
            Assert.That(H.IsShown("history-loading"), Is.False);
            yield return Settle();
            Assert.That(Screen.List.IsAnimating, Is.False);
            Assert.That(Card(0).resolvedStyle.opacity, Is.EqualTo(1).Within(0.001f));
        }

        [UnityTest, Category("AC-HIS-06-03")]
        public IEnumerator ReduceMotion()
        {
            CreateHistory(HistorySeeds.F9, null, o => o.ReduceMotion = true);
            yield return GoToHistory();
            HistorySeeds.AddFinished(H.Sessions, HistorySeeds.Local(2026, 11, 6, 9, 0));
            H.Tap("tab-history");
            H.Step();
            yield return Frames();
            Assert.That(Screen.List.Count, Is.EqualTo(7));
            Assert.That(Screen.List.IsAnimating, Is.False, "con reducir movimiento aparece sin animar");
        }

        [UnityTest, Category("AC-HIS-06-03")]
        public IEnumerator KeepsScroll()
        {
            CreateHistory(HistorySeeds.F9P);
            yield return GoToHistory();
            Screen.List.FinishBatches();
            yield return Frames(3);
            Screen.List.Scroll.scrollOffset = new Vector2(0, 1000);
            yield return Frames(3);
            var watched = Card(10);
            var before = watched.worldBound.yMin;
            HistorySeeds.AddFinished(H.Sessions, HistorySeeds.Local(2026, 11, 6, 9, 0));
            H.Tap("tab-history");
            H.Step();
            yield return Frames(4);
            Assert.That(Screen.List.Count, Is.EqualTo(201));
            Assert.That(watched.worldBound.yMin, Is.EqualTo(before).Within(1f), "lo que se ve no se mueve");
            Assert.That(Screen.List.Scroll.scrollOffset.y, Is.GreaterThan(1000), "el desplazamiento sumó el alto de la nueva");
        }

        [UnityTest, Category("AC-HIS-06-04")]
        public IEnumerator ReloadFailureToast()
        {
            ScriptedSessionRepository? repository = null;
            CreateScriptedHistory(r => repository = r);
            yield return GoToHistory();
            Assert.That(Screen.List.Count, Is.EqualTo(6));
            repository!.HistoryFailures = 1;
            yield return Tap("tab-history");
            Assert.That(Screen.List.Count, Is.EqualTo(6), "la lista queda como estaba");
            Assert.That(H.IsShown("history-list"), Is.True);
            Assert.That(H.App.History.State.Value.Phase, Is.EqualTo(HistoryPhase.Ready));
            Assert.That(H.Shell.Toasts.CurrentText, Is.EqualTo("No se pudo cargar el historial"), "el aviso abajo");
            H.Advance(4.5);
            yield return Settle();
            Assert.That(H.Shell.Toasts.IsVisible, Is.False, "4 s y se va");
        }
    }
}
