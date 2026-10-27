using System;
using NUnit.Framework;
using RowMate.App.Shell;
using RowMate.Core.Design;
using RowMate.Tests.EditMode.Localization;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Shell
{
    // ShellStore = destinos y selección de MainShell (lib/main.dart:116-181).
    [TestFixture]
    public sealed class ShellStoreTests
    {
        [Test, Category("AC-APP-01-01")]
        public void InitialIndexIsDeviceAndNotPersisted()
        {
            var shell = new ShellStore(new ShellOptions(false), new RecordingLogger());
            Assert.That(shell.SelectedIndex.Value, Is.EqualTo(0), "abre en Dispositivo");
            Assert.That(shell.Selected.Id, Is.EqualTo(TabDestinationId.Device));
            shell.Select(ShellStore.HistoryIndex);
            Assert.That(shell.SelectedIndex.Value, Is.EqualTo(3));

            // "Volver a abrir la app" = un store nuevo: la pestaña no se guarda.
            var reopened = new ShellStore(new ShellOptions(false), new RecordingLogger());
            Assert.That(reopened.SelectedIndex.Value, Is.EqualTo(0));
        }

        [Test, Category("AC-APP-02-01")]
        public void FourDestinationsInOrderWithIcons()
        {
            var shell = new ShellStore(new ShellOptions(false), new RecordingLogger());
            Assert.That(shell.Destinations.Count, Is.EqualTo(4), "exactamente 4 sin Strava");
            var expected = new[]
            {
                (TabDestinationId.Device, "navDevice", "deviceTitle", Icons.Bluetooth, Icons.BluetoothConnected, false, "tab-device"),
                (TabDestinationId.Workout, "navWorkout", "workoutTitle", Icons.Timer, Icons.Timer, true, "tab-workout"),
                (TabDestinationId.Routines, "navRoutines", "routinesTitle", Icons.FitnessCenter, Icons.FitnessCenter, false, "tab-routines"),
                (TabDestinationId.History, "navHistory", "historyTitle", Icons.History, Icons.History, false, "tab-history"),
            };
            for (var i = 0; i < expected.Length; i++)
            {
                var d = shell.Destinations[i];
                Assert.That(d.Id, Is.EqualTo(expected[i].Item1));
                Assert.That(d.LabelKey, Is.EqualTo(expected[i].Item2));
                Assert.That(d.TitleKey, Is.EqualTo(expected[i].Item3));
                Assert.That(d.Icon, Is.EqualTo(expected[i].Item4), "sin elegir");
                Assert.That(d.SelectedIcon, Is.EqualTo(expected[i].Item5), "elegido");
                Assert.That(d.SelectedFilled, Is.EqualTo(expected[i].Item6), "timer relleno al elegir Workout");
                Assert.That(d.ElementName, Is.EqualTo(expected[i].Item7));
            }

            var es = new string[4];
            var en = new string[4];
            for (var i = 0; i < 4; i++)
            {
                es[i] = LocalizationFixture.Es.Get(shell.Destinations[i].LabelKey);
                en[i] = LocalizationFixture.En.Get(shell.Destinations[i].LabelKey);
            }

            Assert.That(es, Is.EqualTo(new[] { "Dispositivo", "Workout", "Rutinas", "Historial" }), "Workout también en español (Q-08)");
            Assert.That(en, Is.EqualTo(new[] { "Device", "Workout", "Routines", "History" }));
        }

        [Test, Category("AC-APP-02-02")]
        public void ProfileOnlyWithStravaAndHistoryStaysThird()
        {
            var withoutStrava = new ShellStore(new ShellOptions(false), new RecordingLogger());
            Assert.That(withoutStrava.IndexOf(TabDestinationId.Profile), Is.EqualTo(-1), "ni oculto ni deshabilitado: no existe");

            var withStrava = new ShellStore(new ShellOptions(true), new RecordingLogger());
            Assert.That(withStrava.Destinations.Count, Is.EqualTo(5));
            var profile = withStrava.Destinations[4];
            Assert.That(profile.Id, Is.EqualTo(TabDestinationId.Profile), "al final");
            Assert.That(profile.Icon, Is.EqualTo(Icons.Person));
            Assert.That(profile.FilledFor(false), Is.False, "person sin relleno");
            Assert.That(profile.FilledFor(true), Is.True, "y relleno al elegirlo");
            Assert.That(LocalizationFixture.Es.Get(profile.LabelKey), Is.EqualTo("Perfil"));
            Assert.That(LocalizationFixture.En.Get(profile.LabelKey), Is.EqualTo("Profile"));
            Assert.That(withStrava.IndexOf(TabDestinationId.History), Is.EqualTo(ShellStore.HistoryIndex), "Historial sigue en el 3");
            Assert.That(withoutStrava.IndexOf(TabDestinationId.History), Is.EqualTo(ShellStore.HistoryIndex));
        }

        [Test, Category("AC-APP-02-03")]
        public void SelectingTheSelectedDoesNothing()
        {
            var shell = new ShellStore(new ShellOptions(false), new RecordingLogger());
            var recorder = new StateRecorder<int>(shell.SelectedIndex);
            shell.Select(2);
            shell.Select(2);
            shell.Select(0);
            shell.Select(0);
            Assert.That(recorder.Values, Is.EqualTo(new[] { 2, 0 }), "elige en el acto; tocar el elegido no cambia nada");
        }

        [Test, Category("AC-APP-02-05")]
        public void DeviceIconDependsOnSelectionOnly()
        {
            // La barra no refleja el estado del remo: el ícono sale solo de la selección (main.dart:151-155).
            var shell = new ShellStore(new ShellOptions(false), new RecordingLogger());
            var device = shell.Destinations[0];
            Assert.That(device.IconFor(selected: true), Is.EqualTo(Icons.BluetoothConnected), "elegida (aunque el remo esté desconectado)");
            Assert.That(device.IconFor(selected: false), Is.EqualTo(Icons.Bluetooth), "otra elegida (aunque el remo esté conectado)");
            Assert.That(device.FilledFor(true), Is.False);
            var parameters = typeof(TabDestination).GetMethod(nameof(TabDestination.IconFor))!.GetParameters();
            Assert.That(parameters.Length, Is.EqualTo(1), "IconFor no recibe el estado del remo");
        }

        [Test]
        public void TitlesOfThePlaceholderTabs()
        {
            // AC-APP-02-08 (las pestañas provisorias con el título del catálogo) se ve en PlayMode; acá, los títulos.
            var shell = new ShellStore(new ShellOptions(true), new RecordingLogger());
            Assert.That(LocalizationFixture.Es.Get(shell.Destinations[1].TitleKey), Is.EqualTo("Entrenamiento"));
            Assert.That(LocalizationFixture.Es.Get(shell.Destinations[2].TitleKey), Is.EqualTo("Rutinas"));
            Assert.That(LocalizationFixture.Es.Get(shell.Destinations[3].TitleKey), Is.EqualTo("Historial"));
            Assert.That(LocalizationFixture.Es.Get(shell.Destinations[4].TitleKey), Is.EqualTo("Perfil"));
            Assert.That(LocalizationFixture.En.Get(shell.Destinations[1].TitleKey), Is.EqualTo("Workout"));
        }

        [Test]
        public void SelectOutOfRangeThrows()
        {
            var shell = new ShellStore(new ShellOptions(false), new RecordingLogger());
            Assert.Throws<ArgumentOutOfRangeException>(() => shell.Select(4), "sin Perfil no hay índice 4");
            Assert.Throws<ArgumentOutOfRangeException>(() => shell.Select(-1));
            Assert.That(shell.SelectedIndex.Value, Is.EqualTo(0));
        }
    }
}
