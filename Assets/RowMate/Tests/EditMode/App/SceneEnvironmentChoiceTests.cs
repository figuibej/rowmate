using System.Collections.Generic;
using NUnit.Framework;
using RowMate.App.Scene;
using RowMate.Core.Common;
using RowMate.Core.Scene;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.App
{
    // SEC · La preferencia del escenario (scene_settings.dart:15-41; AC-SCN-21-01 a -04; LT-125, LT-126; DT8-14).
    [TestFixture]
    public sealed class SceneEnvironmentChoiceTests
    {
        // legacy: test/scene/environment_picker_test.dart:11 'tocar una tarjeta cambia el escenario y lo persiste'
        [TestCase(TestName = "tocar una tarjeta cambia el escenario y lo persiste", Category = "LT-125,AC-SCN-21-02,AC-SCN-21-03,AC-SCN-21-01")]
        public void TouchingACardChangesAndPersists()
        {
            var prefs = new RecordingPreferences();
            using var choice = new SceneEnvironmentChoice(prefs);
            Assert.That(choice.Selected.Value, Is.EqualTo(EnvironmentId.Lake), "lago por defecto");
            Assert.That(prefs.WriteCount, Is.EqualTo(0), "arrancar no escribe");
            choice.Select(EnvironmentId.River);
            Assert.That(choice.Selected.Value, Is.EqualTo(EnvironmentId.River));
            Assert.That(prefs.Disk[EnvironmentKeys.PreferenceKey], Is.EqualTo("river"));
        }

        // legacy: test/scene/environment_picker_test.dart:38 'arranca con el escenario guardado'
        [TestCase(TestName = "arranca con el escenario guardado", Category = "LT-126,AC-SCN-21-04")]
        public void StartsWithTheSavedOne()
        {
            var prefs = new RecordingPreferences(true, RecordingPreferences.Seed(EnvironmentKeys.PreferenceKey, "coast"));
            using var choice = new SceneEnvironmentChoice(prefs);
            prefs.CompleteLoad();
            Assert.That(choice.Selected.Value, Is.EqualTo(EnvironmentId.Coast));
        }

        [Test, Category("AC-SCN-21-04")]
        public void AlreadyLoadedPreferencesAreReadInTheConstructorForTheFirstFrame()
        {
            foreach (var id in new[] { EnvironmentId.Lake, EnvironmentId.River, EnvironmentId.Coast, EnvironmentId.Regatta })
            {
                var prefs = new MemoryPreferences();
                prefs.SetString(EnvironmentKeys.PreferenceKey, EnvironmentKeys.ToKey(id));
                using var choice = new SceneEnvironmentChoice(prefs);
                Assert.That(choice.Selected.Value, Is.EqualTo(id), "sin mostrar antes el lago");
            }
        }

        [TestCase("marte", Category = "AC-SCN-21-02")]
        [TestCase("Coast", Category = "AC-SCN-21-02")]
        [TestCase("", Category = "AC-SCN-21-02")]
        [TestCase(null, Category = "AC-SCN-21-02")]
        public void UnknownOrMissingIsTheLakeWithoutWriting(string? value)
        {
            var seed = value == null ? new KeyValuePair<string, object>[0] : new[] { RecordingPreferences.Seed(EnvironmentKeys.PreferenceKey, value) };
            var prefs = new RecordingPreferences(true, seed);
            using var choice = new SceneEnvironmentChoice(prefs);
            prefs.CompleteLoad();
            Assert.That(choice.Selected.Value, Is.EqualTo(EnvironmentId.Lake));
            Assert.That(prefs.WriteCount, Is.EqualTo(0), "el archivo no se toca al arrancar");
            Assert.That(EnvironmentKeys.TryParse(value, out _), Is.False);
        }

        [TestCase(EnvironmentId.Lake, Category = "AC-SCN-21-04")]
        [TestCase(EnvironmentId.Regatta, Category = "AC-SCN-21-04")]
        public void ATouchBeforeTheReadWinsEvenTheLake(EnvironmentId touched)
        {
            var prefs = new RecordingPreferences(true, RecordingPreferences.Seed(EnvironmentKeys.PreferenceKey, "coast"));
            using var choice = new SceneEnvironmentChoice(prefs);
            choice.Select(touched);
            prefs.CompleteLoad();
            Assert.That(choice.Selected.Value, Is.EqualTo(touched), "lo leído se ignora");
        }

        [Test, Category("AC-SCN-21-03")]
        public void TheSameCardDoesNotWrite()
        {
            var prefs = new RecordingPreferences();
            using var choice = new SceneEnvironmentChoice(prefs);
            var changes = 0;
            using var sub = choice.Selected.Subscribe(_ => changes++);
            choice.Select(EnvironmentId.Lake);
            Assert.That(prefs.WriteCount, Is.EqualTo(0));
            choice.Select(EnvironmentId.Regatta);
            choice.Select(EnvironmentId.Regatta);
            Assert.That(prefs.Writes, Is.EqualTo(new[] { "set scene.environment=regatta" }));
            Assert.That(changes, Is.GreaterThanOrEqualTo(1));
        }

        [Test, Category("AC-SCN-21-03")]
        public void AFailedWriteKeepsTheChoiceForThisSession()
        {
            var prefs = new RecordingPreferences { FailWrites = true };
            using var choice = new SceneEnvironmentChoice(prefs);
            choice.Select(EnvironmentId.Coast);
            Assert.That(choice.Selected.Value, Is.EqualTo(EnvironmentId.Coast));
            Assert.That(prefs.Writes, Is.EqualTo(new[] { "set scene.environment=coast!" }));
            Assert.That(prefs.Disk.ContainsKey(EnvironmentKeys.PreferenceKey), Is.False);
        }

        [Test, Category("AC-SCN-21-02")]
        public void KeysAreTheLegacyNamesInTheSelectorOrder()
        {
            Assert.That(EnvironmentKeys.PreferenceKey, Is.EqualTo("scene.environment"));
            var keys = new List<string>();
            foreach (var id in EnvironmentCatalog.All)
            {
                keys.Add(EnvironmentKeys.ToKey(id));
                Assert.That(EnvironmentKeys.TryParse(EnvironmentKeys.ToKey(id), out var back) && back == id, Is.True);
            }

            Assert.That(keys, Is.EqualTo(new[] { "lake", "river", "coast", "regatta" }));
        }

        [Test, Category("AC-SCN-21-04")]
        public void DisposeStopsListening()
        {
            var prefs = new RecordingPreferences(true, RecordingPreferences.Seed(EnvironmentKeys.PreferenceKey, "river"));
            var choice = new SceneEnvironmentChoice(prefs);
            choice.Dispose();
            prefs.CompleteLoad();
            Assert.That(choice.Selected.Value, Is.EqualTo(EnvironmentId.Lake));
            choice.Select(EnvironmentId.Coast);
            Assert.That(prefs.WriteCount, Is.EqualTo(0));
        }
    }
}
