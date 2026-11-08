using System.Collections;
using NUnit.Framework;
using RowMate.Core.Common;
using RowMate.Core.Scene;
using RowMate.Presentation.Workout;
using RowMate.Tests.UiSupport;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace RowMate.Tests.PlayMode.Workout
{
    // EPK · El selector de escenario en Workout en reposo (08-entornos-selector/02-tech-design §4; AC-SCN-22-01 a -03, -05; AC-SCN-21-02
    // a -04; LT-125, LT-126): 4 tarjetas en el orden del legacy con sus medidas, el lago elegido por defecto sin escribir, tocar otra
    // elige y guarda (la misma no escribe), Enter elige la que tiene el foco, la preferencia guardada se ve desde el primer cuadro, con
    // las miniaturas horneadas (sin [SLOT]) y sin dibujar la escena.
    public sealed class EnvironmentPickerTests : WorkoutUiTestBase
    {
        private static readonly string[] Names = { "Lago alpino", "Río urbano", "Costa", "Canal de regata" };

        [UnityTest, Category("AC-SCN-22-02"), Category("LT-125"), Category("AC-SCN-21-02")]
        public IEnumerator FourCardsInOrderLakeByDefault()
        {
            var preferences = new MemoryPreferences();
            CreateWorkout(null, o => o.Preferences = preferences);
            yield return GoToWorkout();
            var picker = Screen.Picker;
            Assert.That(picker.Count, Is.EqualTo(4));
            for (var i = 0; i < 4; i++)
            {
                var card = picker.Card(i);
                Assert.That(card.Environment, Is.EqualTo(EnvironmentCatalog.All[i]));
                Assert.That(card.Text, Is.EqualTo(Names[i]));
                Assert.That(card.IsSelected, Is.EqualTo(i == 0), "el lago por defecto");
                Assert.That(card.Thumbnail.image, Is.Not.Null, "la miniatura horneada");
                var frame = Bounds(card.name + "-frame");
                Assert.That(frame.width, Is.EqualTo(152).Within(0.6f));
                Assert.That(frame.height, Is.EqualTo(96).Within(0.6f));
                if (i > 0)
                {
                    var previous = Bounds(picker.Card(i - 1).name);
                    Assert.That(Bounds(card.name).xMin - previous.xMax, Is.EqualTo(10).Within(0.6f), "separadas 10");
                }
            }

            Assert.That(preferences.TryGetString(EnvironmentKeys.PreferenceKey, out _), Is.False, "el lago por defecto no se escribe");
            Assert.That(H.Logger.Contains("ui.thumbnail"), Is.False, "sin [SLOT] de miniaturas");
            AssertClean(H);
        }

        [UnityTest, Category("AC-SCN-21-03"), Category("AC-SCN-22-03"), Category("LT-125")]
        public IEnumerator TappingChoosesAndSaves()
        {
            var preferences = new MemoryPreferences();
            CreateWorkout(null, o => o.Preferences = preferences);
            yield return GoToWorkout();
            yield return Tap("env-card-river");
            Assert.That(H.App.SceneEnvironment.Selected.Value, Is.EqualTo(EnvironmentId.River));
            Assert.That(preferences.TryGetString(EnvironmentKeys.PreferenceKey, out var saved), Is.True);
            Assert.That(saved, Is.EqualTo("river"));
            Assert.That(Screen.Picker.Card(1).IsSelected, Is.True);
            Assert.That(Screen.Picker.Card(0).IsSelected, Is.False);

            // Enter sobre la tarjeta con el foco.
            var regatta = Screen.Picker.Card(3);
            regatta.Focus();
            using (var submit = NavigationSubmitEvent.GetPooled())
            {
                submit.target = regatta;
                regatta.SendEvent(submit);
            }

            yield return Settle();
            Assert.That(H.App.SceneEnvironment.Selected.Value, Is.EqualTo(EnvironmentId.Regatta));
            Assert.That(preferences.TryGetString(EnvironmentKeys.PreferenceKey, out saved) && saved == "regatta", Is.True);
            AssertClean(H);
        }

        [UnityTest, Category("AC-SCN-21-04"), Category("LT-126")]
        public IEnumerator TheSavedChoiceShowsFromTheFirstFrame()
        {
            var preferences = new MemoryPreferences();
            preferences.SetString(EnvironmentKeys.PreferenceKey, "coast");
            CreateWorkout(null, o => o.Preferences = preferences);
            Assert.That(H.App.SceneEnvironment.Selected.Value, Is.EqualTo(EnvironmentId.Coast));
            yield return GoToWorkout();
            Assert.That(Screen.Picker.Card(2).IsSelected, Is.True, "costa elegida en el primer cuadro de Workout");
            Assert.That(Screen.Picker.Card(0).IsSelected, Is.False);
            AssertClean(H);
        }

        [UnityTest, Category("AC-SCN-21-02")]
        public IEnumerator AnUnknownValueIsTheLakeWithoutWriting()
        {
            var preferences = new MemoryPreferences();
            preferences.SetString(EnvironmentKeys.PreferenceKey, "marte");
            CreateWorkout(null, o => o.Preferences = preferences);
            yield return GoToWorkout();
            Assert.That(Screen.Picker.Card(0).IsSelected, Is.True);
            Assert.That(preferences.TryGetString(EnvironmentKeys.PreferenceKey, out var value) && value == "marte", Is.True, "no se pisa");
            AssertClean(H);
        }
    }
}
