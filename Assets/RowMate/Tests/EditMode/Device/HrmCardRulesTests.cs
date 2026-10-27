using System;
using System.Collections.Generic;
using NUnit.Framework;
using RowMate.App.Device;
using RowMate.Core.Ble;
using RowMate.Tests.EditMode.Localization;

namespace RowMate.Tests.EditMode.Device
{
    // HrmCardRules = lib/features/device/heart_rate_card.dart sin widgets (F-HRM-13), con los textos ya resueltos.
    [TestFixture]
    public sealed class HrmCardRulesTests
    {
        private static readonly IReadOnlyList<BleScanResult> NoResults = new BleScanResult[0];

        private static HeartRateState State(HrmStatus status, int bpm = 0, bool retrying = false, string? remembered = null,
                                            string? connected = null, string? chosen = null, Exception? error = null,
                                            IReadOnlyList<BleScanResult>? results = null)
        {
            return new HeartRateState(status, bpm, results ?? NoResults, error, chosen, retrying, connected, remembered);
        }

        [Test, Category("AC-HRM-13-02")]
        public void TitleIsUpperAndHeartState()
        {
            var es = HrmCardRules.Build(State(HrmStatus.Disconnected), BleStatus.Connected, LocalizationFixture.Es);
            var en = HrmCardRules.Build(State(HrmStatus.Disconnected), BleStatus.Connected, LocalizationFixture.En);
            Assert.That(es.Title, Is.EqualTo("PULSÓMETRO"), "toUpperCase de Dart, sin depender del idioma");
            Assert.That(en.Title, Is.EqualTo("HEART RATE SENSOR"));

            foreach (HrmStatus status in Enum.GetValues(typeof(HrmStatus)))
            {
                var model = HrmCardRules.Build(State(status, bpm: 0), BleStatus.Disconnected, LocalizationFixture.Es);
                Assert.That(model.HeartFull, Is.EqualTo(status == HrmStatus.Connected), "pleno solo conectado: " + status);
            }

            var connectedWithoutReading = HrmCardRules.Build(State(HrmStatus.Connected, bpm: 0), BleStatus.Connected, LocalizationFixture.Es);
            Assert.That(connectedWithoutReading.HeartFull, Is.True, "conectado con -- también pleno");
            var reconnecting = HrmCardRules.Build(State(HrmStatus.Disconnected, retrying: true, remembered: "Polar"), BleStatus.Connected,
                LocalizationFixture.Es);
            Assert.That(reconnecting.HeartFull, Is.False, "reconectando: al 40 %");
        }

        [Test, Category("AC-HRM-13-12")]
        public void HeartBeatsOnlyConnectedWithAReading()
        {
            Assert.That(HrmCardRules.Build(State(HrmStatus.Connected, bpm: 120), BleStatus.Connected, LocalizationFixture.Es).HeartBeating,
                Is.True);
            Assert.That(HrmCardRules.Build(State(HrmStatus.Connected, bpm: 0), BleStatus.Connected, LocalizationFixture.Es).HeartBeating,
                Is.False, "con -- queda quieto y pleno");
            Assert.That(HrmCardRules.Build(State(HrmStatus.Disconnected, bpm: 120), BleStatus.Connected, LocalizationFixture.Es).HeartBeating,
                Is.False, "sin conexión, quieto");
        }

        [Test, Category("AC-HRM-13-03")]
        public void BodyTable()
        {
            // Las 6 filas de AC-HRM-13-03, en orden de prioridad (heart_rate_card.dart:68-94).
            Assert.That(HrmCardRules.BodyFor(State(HrmStatus.Connected, retrying: true, remembered: "R")), Is.EqualTo(HrmCardBody.Connected),
                "1: conectado");
            Assert.That(HrmCardRules.BodyFor(State(HrmStatus.Connecting, retrying: true, remembered: "R")), Is.EqualTo(HrmCardBody.Remembered),
                "2: conectando, reintentando y con recordado → igual que la 5");
            Assert.That(HrmCardRules.BodyFor(State(HrmStatus.Connecting, retrying: true)), Is.EqualTo(HrmCardBody.Connecting),
                "3: conectando sin recordado");
            Assert.That(HrmCardRules.BodyFor(State(HrmStatus.Connecting, remembered: "R")), Is.EqualTo(HrmCardBody.Connecting),
                "3: conectando sin reintentar (conexión manual)");
            Assert.That(HrmCardRules.BodyFor(State(HrmStatus.Scanning, remembered: "R")), Is.EqualTo(HrmCardBody.Scanning), "4: buscando");
            Assert.That(HrmCardRules.BodyFor(State(HrmStatus.Disconnected, remembered: "R")), Is.EqualTo(HrmCardBody.Remembered),
                "5: desconectado con recordado");
            Assert.That(HrmCardRules.BodyFor(State(HrmStatus.Disconnected)), Is.EqualTo(HrmCardBody.NoSensor), "6: desconectado sin recordado");
        }

        [Test, Category("AC-HRM-13-03")]
        public void BodyTextsInSpanishAndEnglish()
        {
            var es = LocalizationFixture.Es;
            var en = LocalizationFixture.En;
            var retrying = State(HrmStatus.Disconnected, retrying: true, remembered: "Polar H10 7E1A2B3C");
            Assert.That(HrmCardRules.Build(retrying, BleStatus.Connected, es).RememberedLine, Is.EqualTo("Polar H10 7E1A2B3C · reconectando..."));
            Assert.That(HrmCardRules.Build(State(HrmStatus.Disconnected, remembered: "Polar H10 7E1A2B3C"), BleStatus.Connected, es).RememberedLine,
                Is.EqualTo("Polar H10 7E1A2B3C · no encontrado"));
            Assert.That(HrmCardRules.Build(State(HrmStatus.Connecting, chosen: "Polar H10 7E1A2B3C"), BleStatus.Connected, es).ConnectingText,
                Is.EqualTo("Conectando a Polar H10 7E1A2B3C..."));
            var noSensor = HrmCardRules.Build(State(HrmStatus.Disconnected), BleStatus.Connected, es);
            Assert.That(noSensor.NoSensorText, Is.EqualTo("Sin sensor"));
            Assert.That(noSensor.WatchHint, Does.Contain("Heart for Bluetooth"));
            Assert.That(noSensor.SearchingText, Is.EqualTo("Buscando sensores..."));
            Assert.That(noSensor.SearchLabel, Is.EqualTo("Buscar"));
            Assert.That(noSensor.SearchOtherLabel, Is.EqualTo("Buscar otro"));
            Assert.That(noSensor.ConnectLabel, Is.EqualTo("Conectar"));
            Assert.That(noSensor.ForgetLabel, Is.EqualTo("Olvidar"));
            Assert.That(noSensor.DisconnectLabel, Is.EqualTo("Desconectar"));
            Assert.That(noSensor.BpmUnit, Is.EqualTo("bpm"));
            var english = HrmCardRules.Build(State(HrmStatus.Disconnected), BleStatus.Connected, en);
            Assert.That(english.NoSensorText, Is.EqualTo("No sensor"));
            Assert.That(english.WatchHint, Does.EndWith("Heart for Bluetooth on Wear OS."));
            Assert.That(english.SearchLabel, Is.EqualTo("Search"));
        }

        [Test, Category("AC-HRM-13-04"), Category("AC-HRM-13-08")]
        public void EnabledTable()
        {
            var es = LocalizationFixture.Es;
            foreach (BleStatus rower in Enum.GetValues(typeof(BleStatus)))
            {
                var busy = rower == BleStatus.Scanning || rower == BleStatus.Connecting;
                var idle = HrmCardRules.Build(State(HrmStatus.Disconnected, remembered: "R"), rower, es);
                Assert.That(idle.SearchEnabled, Is.EqualTo(!busy), "Buscar y Buscar otro con el remo " + rower);
                Assert.That(idle.ConnectEnabled, Is.EqualTo(!busy), "Conectar con el remo " + rower);
                var retrying = HrmCardRules.Build(State(HrmStatus.Disconnected, retrying: true, remembered: "R"), rower, es);
                Assert.That(retrying.ConnectEnabled, Is.False, "Conectar reintentando, con el remo " + rower);
                Assert.That(retrying.SearchEnabled, Is.EqualTo(!busy), "reintentar no frena Buscar otro");
            }

            // La regla es asimétrica: que el pulsómetro esté conectando no frena al remo (DeviceScreenRules.Scan solo mira el buscando).
            Assert.That(DeviceScreenRules.Scan(BleStatus.Disconnected, hrmScanning: false, 0).SearchEnabled, Is.True);
        }

        [Test, Category("AC-HRM-13-05")]
        public void RowTitleFallbacks()
        {
            var es = LocalizationFixture.Es;
            var uuids = new[] { HeartRateParser.Service };
            Assert.That(HrmCardRules.RowTitle(new BleScanResult("A", "Polar H10", "Adv", -50, uuids), es), Is.EqualTo("Polar H10"),
                "el nombre del sistema");
            Assert.That(HrmCardRules.RowTitle(new BleScanResult("A", "", "Adv", -50, uuids), es), Is.EqualTo("Adv"), "si no, el anunciado");
            Assert.That(HrmCardRules.RowTitle(new BleScanResult("A", null, "Adv", -50, uuids), es), Is.EqualTo("Adv"));
            Assert.That(HrmCardRules.RowTitle(new BleScanResult("A", null, "", -50, uuids), es), Is.EqualTo("Sensor desconocido"));
            Assert.That(HrmCardRules.RowTitle(new BleScanResult("A", null, null, -50, uuids), LocalizationFixture.En), Is.EqualTo("Unknown sensor"));

            var simulated = new BleScanResult("SIM:HRM", null, "Pulsómetro simulado", -50, uuids);
            var model = HrmCardRules.Build(State(HrmStatus.Scanning, results: new[] { simulated }), BleStatus.Connected, es);
            Assert.That(model.Rows.Count, Is.EqualTo(1));
            Assert.That(model.Rows[0].Title, Is.EqualTo("Pulsómetro simulado"));
            Assert.That(model.Rows[0].Subtitle, Is.EqualTo("SIM:HRM"), "el subtítulo es el id del dispositivo");
            Assert.That(model.Rows[0].DeviceId, Is.EqualTo("SIM:HRM"));
            Assert.That(model.Rows[0].ElementName, Is.EqualTo("hrm-device-SIM:HRM"), "el key del legacy");
        }

        [Test, Category("AC-HRM-13-07")]
        public void ErrorTexts()
        {
            var es = LocalizationFixture.Es;
            Assert.That(HrmCardRules.Build(State(HrmStatus.Disconnected), BleStatus.Connected, es).ErrorText, Is.Null);
            Assert.That(HrmCardRules.Build(State(HrmStatus.Disconnected, error: new HrmIncompatibleException()), BleStatus.Connected, es).ErrorText,
                Is.EqualTo("No es un pulsómetro compatible (sin servicio Heart Rate)"));
            Assert.That(HrmCardRules.ErrorText(new HrmIncompatibleException(), LocalizationFixture.En),
                Is.EqualTo("Not a compatible heart rate sensor (no Heart Rate service)"));
            // D-25: el texto técnico es el Message (el legacy, el toString() de Dart con el tipo adelante).
            Assert.That(HrmCardRules.ErrorText(new InvalidOperationException("GATT 133"), es), Is.EqualTo("Error al conectar: GATT 133"));
            Assert.That(HrmCardRules.ErrorText(new InvalidOperationException("GATT 133"), LocalizationFixture.En), Is.EqualTo("Connection error: GATT 133"));
        }

        [Test, Category("AC-HRM-04-03")]
        public void ZeroShowsDashes()
        {
            Assert.That(HrmCardRules.BpmText(0), Is.EqualTo("--"));
            Assert.That(HrmCardRules.BpmText(142), Is.EqualTo("142"));
            Assert.That(HrmCardRules.BpmText(1000), Is.EqualTo("1000"), "entero sin separadores");
            var connected = HrmCardRules.Build(State(HrmStatus.Connected, connected: "Pulsómetro simulado"), BleStatus.Connected,
                LocalizationFixture.Es);
            Assert.That(connected.BpmText, Is.EqualTo("--"), "todavía sin lectura");
            Assert.That(connected.ConnectedName, Is.EqualTo("Pulsómetro simulado"));
        }

        [Test, Category("AC-HRM-13-11")]
        public void NoCancelSignalOrStop()
        {
            // El modelo no tiene Cancelar, intensidad de señal, mensaje de lista vacía ni "detener búsqueda" (Q-39, Q-45, Q-46).
            foreach (var property in typeof(HrmCardViewModel).GetProperties())
            {
                foreach (var forbidden in new[] { "Cancel", "Rssi", "Signal", "Stop", "Empty" })
                {
                    Assert.That(property.Name.IndexOf(forbidden, StringComparison.Ordinal), Is.LessThan(0), property.Name);
                }
            }

            foreach (var property in typeof(HrmDeviceRow).GetProperties())
            {
                Assert.That(property.Name, Is.Not.EqualTo("Rssi"), "la fila no muestra la señal");
            }
        }
    }
}
