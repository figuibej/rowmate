using System;
using NUnit.Framework;
using RowMate.App.Scene;
using RowMate.Core.Ble;
using RowMate.Core.Common;
using RowMate.Core.Ports;
using RowMate.Simulator;
using RowMate.Simulator.Panel;
using RowMate.Tests.Support;

namespace RowMate.Tests.Simulator
{
    // SimulatorPanelStore y SimulatorPanelLayout = la lógica de lib/core/dev/simulator_overlay.dart sin widgets (F-BLE-16).
    // Lo de pantalla (LT-26, LT-27) va en PlayMode con el simulador (WP-B).
    [TestFixture]
    public sealed class SimulatorPanelStoreTests
    {
        // 2026-10-27 13:45 hora local (UTC−3): con el reloj a las 13:45, + → 14:00 y − → 12:00 (AC-BLE-16-07).
        private static readonly DateTimeOffset At1345Local = new DateTimeOffset(2026, 10, 27, 16, 45, 0, TimeSpan.Zero);

        private FakeScheduler _scheduler = null!;
        private RecordingLogger _logger = null!;
        private SimulatorDeviceBackend _backend = null!;
        private SceneSettings _scene = null!;
        private SimulatorPanelStore _panel = null!;

        [SetUp]
        public void SetUp()
        {
            _scheduler = new FakeScheduler(At1345Local, TimeSpan.FromHours(-3));
            _logger = new RecordingLogger();
            _backend = SimulatorBackend.Create(_scheduler, _logger, noise: false, random: new SequenceRandom());
            _scene = new SceneSettings(_logger);
            _panel = new SimulatorPanelStore(_backend, _scene, _scheduler, _logger);
        }

        [TearDown]
        public void TearDown()
        {
            _panel.Dispose();
            _backend.Dispose();
        }

        [TestCase(360, 248.0, Category = "AC-BLE-16-03")]
        [TestCase(914, 802.0, Category = "AC-BLE-16-03")]
        [TestCase(200, 120.0, Category = "AC-BLE-16-03")]
        [TestCase(232, 120.0, Category = "AC-BLE-16-03")]
        [TestCase(233, 121.0, Category = "AC-BLE-16-03")]
        [TestCase(600, 488.0, Category = "AC-BLE-16-03")]
        public void MaxHeightRule(double windowHeight, double expected)
        {
            // Sin márgenes = la fórmula de la spec: alto − 96 − 16, mínimo 120.
            Assert.That(SimulatorPanelLayout.MaxHeight(windowHeight, SafeInsets.None), Is.EqualTo(expected));
        }

        [Test, Category("AC-BLE-16-03")]
        public void MaxHeightCountsEachInsetOnce()
        {
            // D-26 (QA-G2-06): Android con barra de estado 24 y de gestos 48: (914 − 48) − 96 − 24 − 16 = 730.
            Assert.That(SimulatorPanelLayout.MaxHeight(914, new SafeInsets(0, 24, 0, 48)), Is.EqualTo(730.0));
            Assert.That(SimulatorPanelLayout.MaxHeight(914, new SafeInsets(32, 0, 32, 0)), Is.EqualTo(802.0), "los laterales no cuentan");
            Assert.That(SimulatorPanelLayout.MaxHeight(200, new SafeInsets(0, 24, 0, 48)), Is.EqualTo(120.0), "mínimo 120");
            Assert.That(SimulatorPanelLayout.Width, Is.EqualTo(280.0));
            Assert.That(SimulatorPanelLayout.MinMaxHeight, Is.EqualTo(120.0));
        }

        [Test]
        public void FabSitsSixteenAboveTheTabBar()
        {
            // AC-BLE-16-02 (llave a 16 de la derecha y 16 sobre la barra de 64 + margen inferior) se mira en PlayMode.
            Assert.That(SimulatorPanelLayout.FabBottom(64), Is.EqualTo(80.0));
            Assert.That(SimulatorPanelLayout.FabBottom(64 + 48), Is.EqualTo(128.0));
            Assert.That(SimulatorPanelLayout.FabRight, Is.EqualTo(16.0));
        }

        [Test, Category("AC-BLE-16-06"), Category("AC-BLE-16-07"), Category("AC-BLE-16-08")]
        public void StartsClosedAt150And24RealAndRowing()
        {
            var state = _panel.State.Value;
            Assert.That(state.IsOpen, Is.False, "arranca cerrado");
            Assert.That(state.WattsText, Is.EqualTo("150 W"));
            Assert.That(state.SpmText, Is.EqualTo("24 spm"));
            Assert.That(state.HourText, Is.EqualTo("real"));
            Assert.That(state.ShowRealHourButton, Is.False);
            Assert.That(state.Rowing, Is.True);
        }

        [Test, Category("AC-BLE-16-05")]
        public void PresetsSetTargetsOnly()
        {
            Assert.That(SimulatorPanelStore.Presets.Count, Is.EqualTo(3));
            Assert.That(SimulatorPanelStore.Presets[0].Name, Is.EqualTo("Suave"));
            Assert.That(SimulatorPanelStore.Presets[1].Name, Is.EqualTo("Medio"));
            Assert.That(SimulatorPanelStore.Presets[2].Name, Is.EqualTo("Fuerte"));

            _panel.SetRowing(false);
            _panel.HourPlus();
            var expected = new[] { (100, 20), (180, 24), (280, 30) };
            for (var i = 0; i < expected.Length; i++)
            {
                _panel.ApplyPreset(SimulatorPanelStore.Presets[i]);
                Assert.That(_backend.Simulator.TargetWatts, Is.EqualTo(expected[i].Item1));
                Assert.That(_backend.Simulator.TargetSpm, Is.EqualTo(expected[i].Item2));
                Assert.That(_panel.State.Value.Rowing, Is.False, "no toca Remando");
                Assert.That(_panel.State.Value.HourText, Is.EqualTo("14:00"), "ni la hora");
            }

            Assert.That(_panel.State.Value.WattsText, Is.EqualTo("280 W"));
            Assert.That(_panel.State.Value.SpmText, Is.EqualTo("30 spm"));
        }

        [Test, Category("AC-BLE-16-06")]
        public void WattsAndSpmStepsAndLimits()
        {
            _panel.WattsPlus();
            Assert.That(_panel.State.Value.WattsText, Is.EqualTo("160 W"), "desde 150, + → 160");
            Assert.That(_backend.Simulator.TargetWatts, Is.EqualTo(160));
            _panel.SpmPlus();
            Assert.That(_backend.Simulator.TargetSpm, Is.EqualTo(25), "+ de ritmo deja 25");

            for (var i = 0; i < 60; i++)
            {
                _panel.WattsPlus();
                _panel.SpmPlus();
            }

            Assert.That(_panel.State.Value.WattsText, Is.EqualTo("500 W"), "en 500 W, + deja 500 W");
            Assert.That(_panel.State.Value.SpmText, Is.EqualTo("40 spm"), "en 40 spm, + deja 40 spm");

            for (var i = 0; i < 60; i++)
            {
                _panel.WattsMinus();
                _panel.SpmMinus();
            }

            Assert.That(_panel.State.Value.WattsText, Is.EqualTo("30 W"), "en 30 W, − deja 30 W");
            Assert.That(_panel.State.Value.SpmText, Is.EqualTo("14 spm"), "en 14 spm, − deja 14 spm");
            _panel.WattsPlus();
            Assert.That(_backend.Simulator.TargetWatts, Is.EqualTo(40), "de a 10");
        }

        [Test, Category("AC-BLE-16-07")]
        public void HourStepsWrapAndRealHour()
        {
            _panel.HourPlus();
            Assert.That(_panel.State.Value.HourText, Is.EqualTo("14:00"), "13:45 → + → 14:00");
            Assert.That(_panel.State.Value.ShowRealHourButton, Is.True);
            Assert.That(_scene.HourOverride.Value, Is.EqualTo(14));

            _panel.RealHour();
            Assert.That(_panel.State.Value.HourText, Is.EqualTo("real"));
            Assert.That(_panel.State.Value.ShowRealHourButton, Is.False, "Hora real desaparece");

            _panel.HourMinus();
            Assert.That(_panel.State.Value.HourText, Is.EqualTo("12:00"), "13:45 → − → 12:00");

            _scene.HourOverride.Value = 23;
            _panel.HourPlus();
            Assert.That(_panel.State.Value.HourText, Is.EqualTo("0:00"), "desde 23:00, + → 0:00");
            _panel.HourMinus();
            Assert.That(_panel.State.Value.HourText, Is.EqualTo("23:00"), "desde 0:00, − → 23:00");
            for (var i = 0; i < 30; i++)
            {
                _panel.HourPlus();
            }

            Assert.That(_scene.HourOverride.Value, Is.EqualTo(5), "(23 + 30) mod 24");
        }

        [Test, Category("AC-BLE-16-07")]
        public void HourStartsFromTheClockHourWithoutMinutes()
        {
            // 00:59 local: − → 23:00 (solo la hora del reloj, con el % de Dart).
            var scheduler = new FakeScheduler(new DateTimeOffset(2026, 10, 27, 3, 59, 0, TimeSpan.Zero), TimeSpan.FromHours(-3));
            var scene = new SceneSettings(_logger);
            var panel = new SimulatorPanelStore(_backend, scene, scheduler, _logger);
            panel.HourMinus();
            Assert.That(panel.State.Value.HourText, Is.EqualTo("23:00"));
            panel.Dispose();
        }

        [Test, Category("AC-BLE-16-08")]
        public void RowingToggle()
        {
            _panel.ToggleRowing();
            Assert.That(_panel.State.Value.Rowing, Is.False);
            Assert.That(_backend.Simulator.Rowing, Is.False);
            _panel.ToggleRowing();
            Assert.That(_backend.Simulator.Rowing, Is.True, "tocar el interruptor o su rótulo lo cambia");
            _panel.SetRowing(false);
            _panel.SetRowing(false);
            Assert.That(_backend.Simulator.Rowing, Is.False);
        }

        [Test, Category("AC-BLE-16-09")]
        public void FaultsWithoutErrors()
        {
            // Sin conectar: no hace nada ni falla.
            Assert.DoesNotThrow(() => _panel.SimulateRowerDrop());
            Assert.DoesNotThrow(() => _panel.SimulateHeartRateDrop());
            Assert.That(_scheduler.PendingTimers, Is.EqualTo(0));

            _backend.Rower.Start();
            _backend.HeartRate.ConnectAsync(SimulatedHeartRateService.DeviceId);
            _panel.SimulateRowerDrop();
            Assert.That(_backend.Rower.Status.Value, Is.EqualTo(BleStatus.Disconnected), "la caída de 3 s del remo");
            _panel.SimulateHeartRateDrop();
            Assert.That(_backend.HeartRate.Status.Value, Is.EqualTo(HrmStatus.Disconnected));
            Assert.That(_backend.HeartRate.IsRetrying, Is.True, "la caída del pulsómetro, reintentando");
            _scheduler.Advance(TimeSpan.FromSeconds(3));
            Assert.That(_backend.Rower.Status.Value, Is.EqualTo(BleStatus.Connected));
            Assert.That(_backend.HeartRate.Status.Value, Is.EqualTo(HrmStatus.Connected));
            foreach (var level in _logger.Levels)
            {
                Assert.That(level, Is.Not.EqualTo(LogLevel.Error), "ninguna produce un error");
            }
        }

        [Test, Category("AC-BLE-16-11")]
        public void NothingPersists()
        {
            _panel.Open();
            _panel.WattsPlus();
            _panel.SpmPlus();
            _panel.HourPlus();
            _panel.SetRowing(false);
            _panel.Close();
            _panel.Open();
            var reopened = _panel.State.Value;
            Assert.That(reopened.WattsText, Is.EqualTo("160 W"), "cerrar y volver a abrir muestra los valores actuales");
            Assert.That(reopened.SpmText, Is.EqualTo("25 spm"));
            Assert.That(reopened.HourText, Is.EqualTo("14:00"));
            Assert.That(reopened.Rowing, Is.False);

            // "Reiniciar la app" = un backend, una escena y un panel nuevos: todo vuelve a los valores iniciales.
            var backend = SimulatorBackend.Create(_scheduler, _logger, noise: false, random: new SequenceRandom());
            var panel = new SimulatorPanelStore(backend, new SceneSettings(_logger), _scheduler, _logger);
            var fresh = panel.State.Value;
            Assert.That(fresh.IsOpen, Is.False);
            Assert.That(fresh.WattsText, Is.EqualTo("150 W"));
            Assert.That(fresh.SpmText, Is.EqualTo("24 spm"));
            Assert.That(fresh.HourText, Is.EqualTo("real"));
            Assert.That(fresh.Rowing, Is.True);
            panel.Dispose();
            backend.Dispose();
        }

        [Test]
        public void FixedTextsInSpanish()
        {
            // Los textos del panel son fijos en español en los dos idiomas (Q-03). AC-BLE-16-04 (contenido y orden en pantalla) se
            // verifica en PlayMode con el simulador (LT-26, SOT.PanelContentAndOrder).
            Assert.That(SimulatorTexts.Title, Is.EqualTo("Simulador"));
            Assert.That(SimulatorTexts.Watts, Is.EqualTo("Watts"));
            Assert.That(SimulatorTexts.Spm, Is.EqualTo("Ritmo"));
            Assert.That(SimulatorTexts.SceneHour, Is.EqualTo("Hora escena"));
            Assert.That(SimulatorTexts.RealHour, Is.EqualTo("Hora real"));
            Assert.That(SimulatorTexts.Rowing, Is.EqualTo("Remando"));
            Assert.That(SimulatorTexts.SimulateDisconnect, Is.EqualTo("Simular desconexión"));
            Assert.That(SimulatorTexts.SimulateHeartRateDrop, Is.EqualTo("Simular caída del pulsómetro"));
            Assert.That(SimulatorTexts.Ribbon, Is.EqualTo("SIMULADOR"));
        }

        [Test]
        public void OpenAndCloseOnlyChangeIsOpen()
        {
            var recorder = new StateRecorder<SimulatorPanelState>(_panel.State);
            _panel.Open();
            _panel.Open();
            _panel.Close();
            Assert.That(recorder.Count, Is.EqualTo(2), "abrir dos veces no vuelve a avisar");
            Assert.That(recorder.Values[0].IsOpen, Is.True);
            Assert.That(recorder.Values[1].IsOpen, Is.False);
            Assert.That(recorder.Values[0].WattsText, Is.EqualTo("150 W"));
        }

        [Test]
        public void HourSetElsewhereIsReflected()
        {
            _scene.HourOverride.Value = 7;
            Assert.That(_panel.State.Value.HourText, Is.EqualTo("7:00"), "sin cero a la izquierda");
            _panel.Dispose();
            _scene.HourOverride.Value = 9;
            Assert.That(_panel.State.Value.HourText, Is.EqualTo("7:00"), "después de Dispose ya no escucha");
        }
    }
}
