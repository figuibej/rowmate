using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using RowMate.App.Device;
using RowMate.App.Scene;
using RowMate.Core.Ble;
using RowMate.Core.Common;
using RowMate.Core.Localization;
using RowMate.Core.Models;
using RowMate.Simulator;
using RowMate.Simulator.Panel;
using RowMate.Tests.Support;

namespace RowMate.Tests.Simulator
{
    // Flujos de Dispositivo con los servicios simulados, en lógica pura: simulador sin ruido + DeviceStore + DeviceScreenRules
    // (los textos que mostraría la grilla) + el panel. Lo de pantalla (transiciones, toques) va en PlayMode (WP-B).
    [TestFixture]
    public sealed class DeviceSimulatorFlowTests
    {
        private static readonly object Gate = new object();
        private static Localizer? _es;

        private FakeScheduler _scheduler = null!;
        private SimulatorDeviceBackend _backend = null!;
        private DeviceStore _device = null!;
        private HeartRateStore _heartRate = null!;
        private SimulatorPanelStore _panel = null!;

        // Catálogo español real (Assets/RowMate/Localization), como lo carga la app.
        private static Localizer Es
        {
            get
            {
                lock (Gate)
                {
                    if (_es == null)
                    {
                        var set = LocalizationSet.Load(File.ReadAllText(RepoPaths.Catalog("es")), File.ReadAllText(RepoPaths.Catalog("en")));
                        _es = set.Es;
                    }

                    return _es;
                }
            }
        }

        [SetUp]
        public void SetUp()
        {
            _scheduler = new FakeScheduler();
            _backend = SimulatorBackend.Create(_scheduler, NullAppLogger.Instance, noise: false, random: new SequenceRandom());
            _device = new DeviceStore(_backend.Rower, NullAppLogger.Instance);
            _heartRate = new HeartRateStore(_backend.HeartRate, NullAppLogger.Instance);
            _panel = new SimulatorPanelStore(_backend, new SceneSettings(NullAppLogger.Instance), _scheduler, NullAppLogger.Instance);
            _backend.Rower.Start(); // = AppContext.Start
        }

        [TearDown]
        public void TearDown()
        {
            _panel.Dispose();
            _heartRate.Dispose();
            _device.Dispose();
            _backend.Dispose();
            Assert.That(_scheduler.PendingTimers, Is.EqualTo(0));
        }

        // Textos de la grilla para el estado actual (SPLIT 500m, SPM, VATIOS, DISTANCIA y, si hay, PULSO).
        private string[] GridTexts()
        {
            var tiles = DeviceScreenRules.Tiles(_device.State.Value.Data, Es);
            var texts = new string[tiles.Count];
            for (var i = 0; i < tiles.Count; i++)
            {
                texts[i] = tiles[i].ValueText;
            }

            return texts;
        }

        [Test, Category("AC-BLE-11-10")]
        public void ReferenceValuesTable()
        {
            // Valores del oráculo de la It. 1 (AC-BLE-14-03 a -06), recalculados en 03-qa-harmony §6 #18.
            _scheduler.Advance(TimeSpan.FromSeconds(60));
            Assert.That(GridTexts(), Is.EqualTo(new[] { "02:13", "24.0", "150", "226m" }), "60 s a 150 W y 24 spm");
            var distance = _device.State.Value.Data.DistanceMeters;

            var expected = new[]
            {
                (Preset: 0, Split: "02:32", Spm: "20.0", Watts: "100"),
                (Preset: 1, Split: "02:05", Spm: "24.0", Watts: "180"),
                (Preset: 2, Split: "01:48", Spm: "30.0", Watts: "280"),
            };
            foreach (var row in expected)
            {
                _panel.ApplyPreset(SimulatorPanelStore.Presets[row.Preset]);
                _scheduler.Advance(TimeSpan.FromSeconds(1)); // la lectura siguiente
                var texts = GridTexts();
                Assert.That(texts[0], Is.EqualTo(row.Split), SimulatorPanelStore.Presets[row.Preset].Name);
                Assert.That(texts[1], Is.EqualTo(row.Spm));
                Assert.That(texts[2], Is.EqualTo(row.Watts));
                Assert.That(_device.State.Value.Data.DistanceMeters, Is.GreaterThan(distance), "la distancia sigue sumando");
                distance = _device.State.Value.Data.DistanceMeters;
            }

            for (var i = 0; i < 60; i++)
            {
                _panel.WattsMinus();
            }

            _scheduler.Advance(TimeSpan.FromSeconds(1));
            Assert.That(GridTexts()[0], Is.EqualTo("03:47"), "30 W (mínimo)");
            Assert.That(GridTexts()[2], Is.EqualTo("30"));
            for (var i = 0; i < 60; i++)
            {
                _panel.WattsPlus();
            }

            _scheduler.Advance(TimeSpan.FromSeconds(1));
            Assert.That(GridTexts()[0], Is.EqualTo("01:29"), "500 W (máximo)");
            Assert.That(GridTexts()[2], Is.EqualTo("500"));

            _panel.SetRowing(false);
            _scheduler.Advance(TimeSpan.FromSeconds(1));
            var frozen = _device.State.Value.Data.DistanceMeters;
            Assert.That(GridTexts(), Is.EqualTo(new[] { "--:--", "0.0", "0", RowingFormats.Meters(frozen) }),
                "Remando apagado");
            _scheduler.Advance(TimeSpan.FromSeconds(5));
            Assert.That(_device.State.Value.Data.DistanceMeters, Is.EqualTo(frozen), "la distancia queda congelada");
        }

        [Test, Category("AC-BLE-15-09")]
        public void PhysicsDoesNotRestartOnReconnect()
        {
            _scheduler.Advance(TimeSpan.FromSeconds(10));
            Assert.That(_device.State.Value.Data.DistanceMeters, Is.EqualTo(37), "10 lecturas");
            _backend.SimRower.SimulateDisconnect();
            _scheduler.Advance(TimeSpan.FromSeconds(3));
            Assert.That(_device.State.Value.IsConnected, Is.True, "volvió a los 3 s");
            _scheduler.Advance(TimeSpan.FromSeconds(1));
            var first = _device.State.Value.Data;
            Assert.That(first.DistanceMeters, Is.EqualTo(41), "no vuelve a 0 ni cuenta los 3 s caído");
            Assert.That(first.ElapsedSeconds, Is.EqualTo(11));
        }

        [Test, Category("AC-BLE-11-09")]
        public void ReconnectShowsZerosThenValues()
        {
            _scheduler.Advance(TimeSpan.FromSeconds(10));
            _backend.SimRower.SimulateDisconnect();
            _scheduler.Advance(TimeSpan.FromSeconds(3));
            Assert.That(GridTexts(), Is.EqualTo(new[] { "--:--", "0.0", "0", "0m" }), "ceros hasta la primera lectura de la conexión");
            _scheduler.Advance(TimeSpan.FromSeconds(1));
            Assert.That(GridTexts(), Is.EqualTo(new[] { "02:13", "24.0", "150", "41m" }));
        }

        [Test, Category("AC-BLE-16-08")]
        public void RowingOffZeroesAndFreezes()
        {
            _scheduler.Advance(TimeSpan.FromSeconds(20));
            _panel.SetRowing(false);
            var before = _device.State.Value.Data;
            var hrBefore = _backend.Simulator.HeartRate;
            _scheduler.Advance(TimeSpan.FromSeconds(1));
            var off = _device.State.Value.Data;
            Assert.That(off.PowerWatts, Is.EqualTo(0));
            Assert.That(off.StrokeRate, Is.EqualTo(0.0));
            Assert.That(off.Pace500mFormatted, Is.EqualTo("--:--"));
            Assert.That(off.DistanceMeters, Is.EqualTo(before.DistanceMeters), "distancia congelada");
            Assert.That(off.StrokeCount, Is.EqualTo(before.StrokeCount), "paladas congeladas");
            Assert.That(off.ElapsedSeconds, Is.EqualTo(before.ElapsedSeconds), "tiempo congelado");
            Assert.That(off.TotalCalories, Is.EqualTo(before.TotalCalories), "calorías congeladas");
            _scheduler.Advance(TimeSpan.FromSeconds(10));
            Assert.That(_backend.Simulator.HeartRate, Is.LessThan(hrBefore), "el pulso simulado baja hacia 70");

            _panel.SetRowing(true);
            _scheduler.Advance(TimeSpan.FromSeconds(1));
            Assert.That(_device.State.Value.Data.PowerWatts, Is.EqualTo(150), "vuelve a remar con los objetivos actuales");
            Assert.That(_device.State.Value.Data.DistanceMeters, Is.GreaterThan(before.DistanceMeters));
        }

        [Test, Category("AC-BLE-16-10")]
        public void PanelChangesShowOnTheNextReading()
        {
            _heartRate.ConnectAsync(SimulatedHeartRateService.DeviceId, SimulatedHeartRateService.DeviceName);
            _scheduler.Advance(TimeSpan.FromSeconds(5));
            var hrAt5 = _heartRate.State.Value.Bpm;
            _panel.WattsPlus();
            Assert.That(_device.State.Value.Data.PowerWatts, Is.EqualTo(150), "todavía la lectura anterior");
            _scheduler.Advance(TimeSpan.FromSeconds(1));
            Assert.That(_device.State.Value.Data.PowerWatts, Is.EqualTo(160), "en la lectura siguiente (≤ 1 s), en el tablero");
            _panel.ApplyPreset(SimulatorPanelStore.Presets[2]);
            _scheduler.Advance(TimeSpan.FromSeconds(1));
            Assert.That(_device.State.Value.Data.PowerWatts, Is.EqualTo(280));
            Assert.That(_heartRate.State.Value.Bpm, Is.GreaterThan(hrAt5), "y en la tarjeta del pulsómetro");
        }

        [Test, Category("AC-BLE-08-01")]
        public void SimulatedDropShowsSearchAndSearchReconnects()
        {
            _scheduler.Advance(TimeSpan.FromSeconds(5));
            Assert.That(DeviceScreenRules.ViewFor(_device.State.Value.Status), Is.EqualTo(DeviceViewKind.Connected));
            _backend.SimRower.SimulateDisconnect();
            var state = _device.State.Value;
            Assert.That(DeviceScreenRules.ViewFor(state.Status), Is.EqualTo(DeviceViewKind.Scan), "durante la caída, la vista de búsqueda");
            var scan = DeviceScreenRules.Scan(state.Status, _heartRate.State.Value.IsScanning, state.ScanResults.Count);
            Assert.That(scan.SearchEnabled, Is.True, "Buscar dispositivos habilitado");
            Assert.That(scan.ShowEmptyState, Is.True, "No se encontraron dispositivos, sin texto de reconectando");
            Assert.That(scan.Searching, Is.False);

            _device.StartScanAsync(); // tocar Buscar dispositivos reconecta en el acto y cancela la espera
            Assert.That(_device.State.Value.IsConnected, Is.True);
            Assert.That(_backend.SimRower.IsReconnectPending, Is.False);

            _backend.SimRower.SimulateDisconnect();
            _scheduler.Advance(TimeSpan.FromSeconds(3));
            Assert.That(DeviceScreenRules.ViewFor(_device.State.Value.Status), Is.EqualTo(DeviceViewKind.Connected),
                "si nadie toca nada, a los 3 s vuelve la vista conectada");
        }

        [Test, Category("AC-HRM-04-05")]
        public void BpmDoesNotDependOnTheRower()
        {
            _heartRate.ConnectAsync(SimulatedHeartRateService.DeviceId, SimulatedHeartRateService.DeviceName);
            _scheduler.Advance(TimeSpan.FromSeconds(5));
            _device.DisconnectAsync();
            var bpm = new StateRecorder<int>(_backend.HeartRate.Bpm);
            _scheduler.Advance(TimeSpan.FromSeconds(4));
            Assert.That(bpm.Count, Is.EqualTo(4), "el pulso llega aunque el remo esté desconectado");
            Assert.That(_heartRate.State.Value.IsConnected, Is.True);
            Assert.That(DeviceScreenRules.Tiles(_device.State.Value.Data, Es).Count, Is.EqualTo(4),
                "el pulso del pulsómetro nunca entra a la grilla");
        }

        [Test, Category("AC-HRM-15-10")]
        public void BpmKeepsPublishingWithTheRowerStopped()
        {
            _heartRate.ConnectAsync(SimulatedHeartRateService.DeviceId, SimulatedHeartRateService.DeviceName);
            _scheduler.Advance(TimeSpan.FromSeconds(10));
            _device.DisconnectAsync(); // el remo deja de avanzar
            _scheduler.Advance(TimeSpan.FromSeconds(1));
            var last = _heartRate.State.Value.Bpm;
            var bpm = new StateRecorder<int>(_backend.HeartRate.Bpm);
            _scheduler.Advance(TimeSpan.FromSeconds(5));
            Assert.That(bpm.Values, Is.EqualTo(new[] { last, last, last, last, last }), "queda en el último valor y se publica cada segundo");
        }

        [Test, Category("AC-BLE-15-03")]
        public void DeviceGridNeverShowsThePulseOfTheSimulatedRower()
        {
            _heartRate.ConnectAsync(SimulatedHeartRateService.DeviceId, SimulatedHeartRateService.DeviceName);
            _scheduler.Advance(TimeSpan.FromSeconds(30));
            Assert.That(_device.State.Value.Data.HeartRate, Is.EqualTo(0));
            Assert.That(GridTexts().Length, Is.EqualTo(4), "sin tarjeta PULSO con el remo simulado");
        }

        [Test, Category("AC-BLE-07-03")]
        public void ManualDisconnectStaysDisconnected()
        {
            _scheduler.Advance(TimeSpan.FromSeconds(5));
            _device.DisconnectAsync();
            _scheduler.Advance(TimeSpan.FromSeconds(10));
            var state = _device.State.Value;
            Assert.That(state.IsConnected, Is.False);
            Assert.That(state.ConnectedDeviceName, Is.Null);
            var scan = DeviceScreenRules.Scan(state.Status, false, state.ScanResults.Count);
            Assert.That(scan.ShowEmptyState, Is.True, "con el simulador, sin lista: No se encontraron dispositivos");
            _device.StartScanAsync();
            Assert.That(_device.State.Value.IsConnected, Is.True, "solo vuelve tocando Buscar dispositivos, al instante");
        }

        [Test]
        public void DeviceStoreNotifiesOncePerSimulatedReading()
        {
            var notifications = new List<int>();
            _device.State.Subscribe(s => notifications.Add(s.Data.ElapsedSeconds), false);
            _scheduler.Advance(TimeSpan.FromSeconds(5));
            Assert.That(notifications, Is.EqualTo(new[] { 1, 2, 3, 4, 5 }), "exactamente una por segundo");
        }

        [Test]
        public void HeartRateCardShowsRememberedDuringTheDrop()
        {
            _heartRate.ConnectAsync(SimulatedHeartRateService.DeviceId, SimulatedHeartRateService.DeviceName);
            _scheduler.Advance(TimeSpan.FromSeconds(1));
            _panel.SimulateHeartRateDrop();
            var model = HrmCardRules.Build(_heartRate.State.Value, _device.State.Value.Status, Es);
            Assert.That(model.Body, Is.EqualTo(HrmCardBody.Remembered));
            Assert.That(model.RememberedLine, Is.EqualTo("Pulsómetro simulado · reconectando..."));
            Assert.That(model.ConnectEnabled, Is.False, "Conectar deshabilitado mientras reintenta");
            Assert.That(model.SearchEnabled, Is.True, "Buscar otro y Olvidar habilitados");
            Assert.That(model.HeartFull, Is.False, "corazón al 40 %");
            _scheduler.Advance(TimeSpan.FromSeconds(3));
            Assert.That(HrmCardRules.BodyFor(_heartRate.State.Value), Is.EqualTo(HrmCardBody.Connected));
            Assert.That(_heartRate.State.Value.Status, Is.EqualTo(HrmStatus.Connected));
        }
    }
}
