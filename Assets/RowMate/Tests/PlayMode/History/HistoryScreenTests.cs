using System.Collections;
using NUnit.Framework;
using RowMate.App.History;
using RowMate.Core.Design;
using RowMate.Tests.Support;
using RowMate.Tests.UiSupport;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace RowMate.Tests.PlayMode.History
{
    // HSC · Pestaña Historial (F-HIS-06, F-HIS-07; 09-historial/02-tech-design §5.1, §8.2).
    public sealed class HistoryScreenTests : HistoryUiTestBase
    {
        private static readonly string[] Names = { "Escalera", "Libre", "Escalera", "Libre", "Libre", "Rutina borrada" };

        private static readonly string[] Dates =
        {
            "05/11/2026  18:40", "04/11/2026  07:15", "03/11/2026  19:02", "02/11/2026  12:00", "02/10/2026  13:51", "01/10/2026  21:05",
        };

        [UnityTest, Category("AC-HIS-06-01")]
        public IEnumerator NoSpinnerAfterStartupLoad()
        {
            CreateHistory(HistorySeeds.F9);
            Assert.That(H.App.History.State.Value.Phase, Is.EqualTo(HistoryPhase.Ready), "la carga del arranque ya terminó");
            yield return GoToHistory();
            Assert.That(H.IsShown("history-loading"), Is.False, "sin indicador");
            Assert.That(H.IsShown("history-list"), Is.True);
            Assert.That(Screen.AppBar.Title.text, Is.EqualTo("Historial"), "historyTitle");
            Assert.That(Screen.Q(className: "rm-placeholder__icon"), Is.Null, "sin el contenido provisorio");
        }

        [UnityTest, Category("AC-HIS-07-02")]
        public IEnumerator Order()
        {
            CreateHistory(HistorySeeds.F9);
            yield return GoToHistory();
            Assert.That(Screen.List.Count, Is.EqualTo(6), "todas: terminadas, cortadas y sin puntos");
            for (var i = 0; i < Names.Length; i++)
            {
                Assert.That(Card(i).NameLabel.text, Is.EqualTo(Names[i]), "nombre " + i);
                Assert.That(Card(i).DateLabel.text, Is.EqualTo(Dates[i]), "fecha " + i + " (UTC−03:00, dos espacios)");
                Assert.That(Card(i).name, Is.EqualTo("history-card-" + i));
            }
        }

        [UnityTest, Category("AC-HIS-07-03"), Category("AC-HIS-07-04")]
        public IEnumerator CardRows()
        {
            CreateHistory(HistorySeeds.F9);
            yield return GoToHistory();
            var card = Card(0);
            Assert.That(Texts(card.Stats), Is.EqualTo(new[] { "11:00", "2253m", "150", "24.1", "03:00", "139/150" }));
            Assert.That(card.Stats.LabelAt(0).text, Is.EqualTo("Tiempo"));
            Assert.That(card.Stats.LabelAt(1).text, Is.EqualTo("Distancia"));
            Assert.That(card.Stats.LabelAt(2).text, Is.EqualTo("Watts"));
            Assert.That(card.Stats.LabelAt(3).text, Is.EqualTo("SPM"));
            Assert.That(card.Stats.LabelAt(4).text, Is.EqualTo("Split"));
            Assert.That(card.Stats.LabelAt(5).text, Is.EqualTo("Pulso"));
            AssertColor(card.Stats.ValueAt(0), Tokens.TextPrimary, "Tiempo en blanco");
            AssertColor(card.Stats.ValueAt(1), Tokens.MetricDistance, "Distancia");
            AssertColor(card.Stats.ValueAt(2), Tokens.MetricWatts, "Watts");
            AssertColor(card.Stats.ValueAt(3), Tokens.MetricSpm, "SPM");
            AssertColor(card.Stats.ValueAt(4), Tokens.MetricSplit, "Split");
            AssertColor(card.Stats.ValueAt(5), Tokens.MetricHr, "Pulso");
            Assert.That(Texts(Card(1).Stats), Is.EqualTo(new[] { "33:44", "7923m", "160", "22.0", "02:10", "145/145" }));
            Assert.That(Texts(Card(2).Stats), Is.EqualTo(new[] { "03:00", "651m", "150", "24.1", "02:30", "140/150" }), "la cortada, con sus puntos");
            Assert.That(Texts(Card(3).Stats), Is.EqualTo(new[] { "00:00", "0m", "0", "0.0", "--:--" }), "sin puntos: 5 columnas");
            Assert.That(Texts(Card(4).Stats), Is.EqualTo(new[] { "1:02:05", "13796m", "140", "20.0", "02:15" }), "sin pulso: 5 columnas");
            Assert.That(Texts(Card(5).Stats), Is.EqualTo(new[] { "04:00", "851m", "150", "24.1", "02:30", "136/150" }));
            Assert.That(card.Q(className: "rm-history-card__icon").resolvedStyle.color, Is.EqualTo(TokenColors(Tokens.AccentPrimary)).Using(ColorTolerance.Instance));
            Assert.That(card.Q(className: "rm-history-card__chevron").resolvedStyle.opacity, Is.EqualTo(0.24f).Within(0.001f));
            Assert.That(card.NameLabel.resolvedStyle.fontSize, Is.EqualTo(15).Within(0.01f), "nombre 15/600");
            var gap = card.Stats.worldBound.yMin - card.Q(className: "rm-history-card__top").worldBound.yMax;
            Assert.That(gap, Is.EqualTo(12).Within(0.6f), "12 debajo de la fila superior");
        }

        [UnityTest, Category("AC-HIS-07-01")]
        public IEnumerator Empty()
        {
            CreateHistory(HistorySeeds.F9V);
            yield return GoToHistory();
            Assert.That(H.IsShown("history-empty"), Is.True);
            Assert.That(H.IsShown("history-list"), Is.False);
            Assert.That(H.TextOf("history-empty-text"), Is.EqualTo("No hay sesiones guardadas"));
            var icon = H.Find("history-empty-icon")!;
            Assert.That(icon.resolvedStyle.opacity, Is.EqualTo(0.12f).Within(0.001f));
            Assert.That(icon.resolvedStyle.width, Is.EqualTo(56).Within(0.5f));
            var gap = H.BoundsOf("history-empty-text").yMin - H.BoundsOf("history-empty-icon").yMax;
            Assert.That(gap, Is.EqualTo(12).Within(0.6f), "12 de separación");
            AssertColor(H.Find<Label>("history-empty-text")!, Tokens.TextTertiary, "text.tertiary");
        }

        [UnityTest, Category("AC-HIS-07-06")]
        public IEnumerator FitsAllProfiles()
        {
            var profiles = new[] { DeviceProfile.TV, DeviceProfile.TH, DeviceProfile.W1280, DeviceProfile.P360 };
            foreach (var profile in profiles)
            {
                var big = profile.Code == DeviceProfile.P360.Code;
                CreateHistory(HistorySeeds.F9, profile, o =>
                {
                    if (big)
                    {
                        o.LanguageTags = new[] { "en-US" };
                        o.TextScale = 1.3;
                    }
                });
                yield return GoToHistory();
                yield return Frames(3);
                var window = H.Root.worldBound.width;
                for (var i = 0; i < Screen.List.Count; i++)
                {
                    var card = Card(i);
                    Assert.That(card.worldBound.width, Is.EqualTo(window - 24).Within(0.6f), profile.Code + ": una columna con margen 12 (" + i + ")");
                    for (var c = 0; c < card.Stats.Count; c++)
                    {
                        Assert.That(card.Stats.FitAt(c).Fits, Is.True, profile.Code + ": la columna " + c + " de la tarjeta " + i + " entra");
                    }

                    Assert.That(card.DateLabel.worldBound.xMin, Is.GreaterThanOrEqualTo(card.NameLabel.worldBound.xMax - 0.5f), profile.Code + ": la fecha no tapa el nombre");
                }

                if (IsP360(profile))
                {
                    Assert.That(Card(4).Stats.FitAt(0).FitScale, Is.LessThan(1f), "1:02:05 se achica en P360 a 1,3×");
                    Assert.That(Card(4).Stats.FitAt(1).FitScale, Is.LessThan(1f), "13796m se achica");
                    Assert.That(Card(0).Stats.FitAt(5).FitScale, Is.LessThan(1f), "139/150 se achica");
                    Assert.That(H.IsTextVisible("History"), Is.True);
                    Assert.That(Card(1).NameLabel.text, Is.EqualTo("Free"));
                    Assert.That(Card(0).Stats.LabelAt(5).text, Is.EqualTo("HR"));
                }

                AssertClean(H);
            }
        }

        [UnityTest, Category("AC-HIS-07-07")]
        public IEnumerator OpenByTapAndKeyboard()
        {
            CreateHistory(HistorySeeds.F9);
            yield return GoToHistory();
            yield return OpenCard(0);
            Assert.That(Detail.AppBar.Title.text, Is.EqualTo("Escalera"));
            Detail.Close();
            yield return Settle();
            Assert.That(DetailOrNull, Is.Null);
            Card(1).Focus();
            yield return Frames();
            H.PressEnter();
            yield return Settle();
            Assert.That(Detail.AppBar.Title.text, Is.EqualTo("Entrenamiento libre"), "Enter abre la enfocada");
            Detail.Close();
            yield return Settle();
            Card(2).Focus();
            H.PressKey(KeyCode.Space, ' ');
            yield return Settle();
            Assert.That(Detail.AppBar.Title.text, Is.EqualTo("Escalera"), "Espacio también");
        }

        [UnityTest, Category("AC-HIS-07-07")]
        public IEnumerator TabWalksTheCards()
        {
            CreateHistory(HistorySeeds.F9, DeviceProfile.W1280);
            yield return GoToHistory();
            Card(0).Focus();
            yield return Frames();
            H.PressTab();
            yield return Frames();
            Assert.That(H.Focused, Is.SameAs(Card(1)), "Tab pasa a la tarjeta siguiente");
            Assert.That(Card(0).focusable && Card(1).focusable, Is.True);
        }

        [UnityTest, Category("AC-HIS-06-04")]
        public IEnumerator ErrorAndRetry()
        {
            CreateScriptedHistory(r => r.HistoryFailures = 2); // el arranque y el toque de la pestaña
            yield return GoToHistory();
            Assert.That(H.IsShown("history-error"), Is.True, "no queda en el indicador");
            Assert.That(H.IsShown("history-loading"), Is.False);
            Assert.That(H.TextOf("history-error-text"), Is.EqualTo("No se pudo cargar el historial"));
            Assert.That(H.TextOf("history-retry"), Is.EqualTo("Reintentar"));
            yield return Tap("history-retry");
            Assert.That(H.IsShown("history-error"), Is.False);
            Assert.That(H.IsShown("history-list"), Is.True, "la base responde: aparece la lista");
            Assert.That(Screen.List.Count, Is.EqualTo(6));
        }

        [UnityTest, Category("AC-HIS-06-04")]
        public IEnumerator RotaShowsTheError()
        {
            Create(new UiHostOptions { Routines = HostRoutines.Rota() });
            yield return GoToHistory();
            Assert.That(H.IsShown("history-error"), Is.True);
            Assert.That(H.TextOf("history-error-text"), Is.EqualTo("No se pudo cargar el historial"));
        }

        private static bool IsP360(DeviceProfile profile)
        {
            return profile.Code == DeviceProfile.P360.Code;
        }

        private static Color TokenColors(ColorToken token)
        {
            return RowMate.Presentation.Controls.TokenColors.ToColor(token);
        }

        private static void AssertColor(VisualElement element, ColorToken token, string what)
        {
            Assert.That(element.resolvedStyle.color, Is.EqualTo(TokenColors(token)).Using(ColorTolerance.Instance), what);
        }

        private sealed class ColorTolerance : System.Collections.Generic.IEqualityComparer<Color>
        {
            public static readonly ColorTolerance Instance = new ColorTolerance();

            public bool Equals(Color a, Color b)
            {
                return Mathf.Abs(a.r - b.r) < 0.01f && Mathf.Abs(a.g - b.g) < 0.01f && Mathf.Abs(a.b - b.b) < 0.01f && Mathf.Abs(a.a - b.a) < 0.01f;
            }

            public int GetHashCode(Color c)
            {
                return 0;
            }
        }
    }
}
