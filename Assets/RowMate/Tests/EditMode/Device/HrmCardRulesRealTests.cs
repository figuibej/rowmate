using System;
using System.Collections.Generic;
using NUnit.Framework;
using RowMate.App.Device;
using RowMate.Core.Ble;
using RowMate.Tests.EditMode.Localization;

namespace RowMate.Tests.EditMode.Device
{
    // HrmCardRules de la It. 11 (02-tech-design §3.2, A11-02): lectura vencida, indicador del recordado, nombre y sufijo separados,
    // sensor sin nombre y el error en dos renglones. Los casos de la It. 2 siguen en HrmCardRulesTests.
    [TestFixture]
    public sealed class HrmCardRulesRealTests
    {
        private static readonly IReadOnlyList<BleScanResult> NoResults = new BleScanResult[0];

        private static HrmLink Stale => HrmLink.Idle with { IsStale = true };

        private static HeartRateState State(HrmStatus status, int bpm = 0, bool retrying = false, string? remembered = null, string? connected = null,
                                            string? chosen = null, HrmErrorKind error = HrmErrorKind.None, string? errorName = null,
                                            string? detail = null, HrmLink? link = null)
        {
            return new HeartRateState(status, bpm, NoResults, error, errorName, detail, chosen, retrying, connected, remembered, link ?? HrmLink.Idle);
        }

        private static HrmCardViewModel Es(HeartRateState state)
        {
            return HrmCardRules.Build(state, BleStatus.Disconnected, LocalizationFixture.Es);
        }

        // AC-HRM-05-03: con el watchdog disparado, "--" y "Sin señal del sensor"; el corazón quieto al 40 %.
        [Test, Category("AC-HRM-05-03")]
        public void StaleShowsNoSignalHeartDim()
        {
            var model = Es(State(HrmStatus.Connected, connected: "Banda de prueba", link: Stale));
            Assert.That(model.BpmText, Is.EqualTo("--"));
            Assert.That(model.StaleText, Is.EqualTo("Sin señal del sensor"));
            Assert.That(model.HeartFull, Is.False, "al 40 %");
            Assert.That(model.HeartBeating, Is.False);
            Assert.That(HrmCardRules.Build(State(HrmStatus.Connected, link: Stale), BleStatus.Disconnected, LocalizationFixture.En).StaleText,
                Is.EqualTo("No signal from the sensor"));
            var back = Es(State(HrmStatus.Connected, bpm: 142, connected: "Banda de prueba"));
            Assert.That(back.StaleText, Is.Null, "con la próxima lectura válida la leyenda desaparece");
            Assert.That(back.HeartBeating, Is.True);
            Assert.That(Es(State(HrmStatus.Disconnected, link: Stale)).StaleText, Is.Null, "solo conectado");
        }

        // AC-HRM-05-03 (paridad, LT-28): antes de la primera lectura solo "--", con el corazón quieto y pleno.
        [Test, Category("AC-HRM-05-03")]
        public void BeforeFirstReadingFullAndStill()
        {
            var model = Es(State(HrmStatus.Connected, connected: "Banda de prueba"));
            Assert.That(model.BpmText, Is.EqualTo("--"));
            Assert.That(model.StaleText, Is.Null);
            Assert.That(model.HeartFull, Is.True);
            Assert.That(model.HeartBeating, Is.False);
        }

        // AC-HRM-03-04 (Q-45): durante una conexión manual la tarjeta no ofrece botones (cuerpo Connecting, sin indicador).
        [Test, Category("AC-HRM-03-04")]
        public void ConnectingHasNoButtons()
        {
            var model = Es(State(HrmStatus.Connecting, chosen: "Banda de prueba", remembered: "Otra banda"));
            Assert.That(model.Body, Is.EqualTo(HrmCardBody.Connecting), "manual: sin reintentos");
            Assert.That(model.Indicator, Is.EqualTo(HrmIndicator.None));
            Assert.That(model.ConnectingText, Is.EqualTo("Conectando a Banda de prueba..."));
        }

        // AC-HRM-06-02 (Q-41, DT11-04): un sensor sin nombre ("") se muestra como "Sensor desconocido", nunca con la dirección.
        [Test, Category("AC-HRM-06-02")]
        public void EmptyNameIsUnknownSensor()
        {
            Assert.That(Es(State(HrmStatus.Connected, connected: "")).ConnectedName, Is.EqualTo("Sensor desconocido"));
            Assert.That(Es(State(HrmStatus.Disconnected, remembered: "")).RememberedLine, Is.EqualTo("Sensor desconocido · no encontrado"));
            Assert.That(Es(State(HrmStatus.Connecting, remembered: "")).ConnectingText, Is.EqualTo("Conectando a Sensor desconocido..."));
            Assert.That(HrmCardRules.Build(State(HrmStatus.Connected, connected: ""), BleStatus.Disconnected, LocalizationFixture.En).ConnectedName,
                Is.EqualTo("Unknown sensor"));
            Assert.That(Es(State(HrmStatus.Disconnected)).ConnectedName, Is.EqualTo(""), "sin conexión, vacío");
        }

        // AC-HRM-13-13 (A11-02, AC-HRM-07-02, -03): el indicador del recordado y el nombre y el sufijo en dos etiquetas de una fila.
        [Test, Category("AC-HRM-13-13")]
        public void RememberedIndicatorAndSplitName()
        {
            var waitingLink = new HrmLink(ReconnectPhase.Waiting, HrmRetryReason.Startup, 1, 25.0, TimeSpan.FromSeconds(10), false, HrmConnectOrigin.None);
            var waiting = Es(State(HrmStatus.Disconnected, retrying: true, remembered: "Banda de prueba", link: waitingLink));
            Assert.That(waiting.Indicator, Is.EqualTo(HrmIndicator.WaitingArc));
            Assert.That(waiting.ArcStartAt, Is.EqualTo(15.0), "NextAttemptAt − Wait");
            Assert.That(waiting.ArcDuration, Is.EqualTo(TimeSpan.FromSeconds(10)));
            Assert.That(waiting.RememberedName, Is.EqualTo("Banda de prueba"));
            Assert.That(waiting.RememberedSuffix, Is.EqualTo(" · reconectando..."));
            Assert.That(waiting.HeartBreathing, Is.True);
            Assert.That(waiting.HeartFull, Is.False);

            var attemptingLink = waitingLink with { Reconnect = ReconnectPhase.Attempting, NextAttemptAt = null };
            var attempting = Es(State(HrmStatus.Connecting, retrying: true, remembered: "Banda de prueba", link: attemptingLink));
            Assert.That(attempting.Body, Is.EqualTo(HrmCardBody.Remembered));
            Assert.That(attempting.Indicator, Is.EqualTo(HrmIndicator.Spinner));
            Assert.That(attempting.ArcStartAt, Is.Null);

            var notFound = Es(State(HrmStatus.Disconnected, remembered: "Banda de prueba"));
            Assert.That(notFound.Indicator, Is.EqualTo(HrmIndicator.LinkOff));
            Assert.That(notFound.RememberedSuffix, Is.EqualTo(" · no encontrado"));
            Assert.That(notFound.HeartBreathing, Is.False);

            Assert.That(Es(State(HrmStatus.Connected, connected: "Banda de prueba")).Indicator, Is.EqualTo(HrmIndicator.None));
            Assert.That(Es(State(HrmStatus.Disconnected)).Indicator, Is.EqualTo(HrmIndicator.None));
            var english = HrmCardRules.Build(State(HrmStatus.Disconnected, retrying: true, remembered: "Banda de prueba"), BleStatus.Disconnected,
                LocalizationFixture.En);
            Assert.That(english.RememberedLine, Is.EqualTo("Banda de prueba · reconnecting..."));
        }

        // AC-HRM-13-14 (Q-42, DT11-15): la frase humana por tipo y, debajo, el detalle técnico sin traducir.
        [Test, Category("AC-HRM-13-14")]
        public void ErrorHumanAndDetailPerKind()
        {
            var scan = Es(State(HrmStatus.Disconnected, error: HrmErrorKind.Scan, detail: "scan: error 2"));
            Assert.That(scan.ErrorHuman, Is.EqualTo("No se pudo buscar sensores"));
            Assert.That(scan.ErrorDetail, Is.EqualTo("scan: error 2"));
            Assert.That(scan.ErrorText, Is.EqualTo("No se pudo buscar sensores\nscan: error 2"));

            var connect = Es(State(HrmStatus.Disconnected, error: HrmErrorKind.Connect, errorName: "Banda de prueba", detail: "connect: gatt error 133"));
            Assert.That(connect.ErrorHuman, Is.EqualTo("No se pudo conectar con Banda de prueba"));
            Assert.That(connect.ErrorDetail, Is.EqualTo("connect: gatt error 133"), "sin traducir");

            var remembered = Es(State(HrmStatus.Disconnected, remembered: "Banda de prueba", error: HrmErrorKind.Connect, detail: "x"));
            Assert.That(remembered.ErrorHuman, Is.EqualTo("No se pudo conectar con Banda de prueba"), "si no hay elegido, el recordado");
            var unknown = Es(State(HrmStatus.Disconnected, error: HrmErrorKind.Connect, errorName: "", detail: "x"));
            Assert.That(unknown.ErrorHuman, Is.EqualTo("No se pudo conectar con Sensor desconocido"));

            var english = HrmCardRules.Build(State(HrmStatus.Disconnected, error: HrmErrorKind.Connect, errorName: "Banda de prueba", detail: "x"),
                BleStatus.Disconnected, LocalizationFixture.En);
            Assert.That(english.ErrorHuman, Is.EqualTo("Couldn't connect to Banda de prueba"));
            Assert.That(Es(State(HrmStatus.Disconnected)).ErrorHuman, Is.Null);
            Assert.That(Es(State(HrmStatus.Disconnected)).ErrorDetail, Is.Null);
        }

        // AC-HRM-13-14: incompatible, sin detalle.
        [Test, Category("AC-HRM-13-14")]
        public void IncompatibleNoDetail()
        {
            var model = Es(State(HrmStatus.Disconnected, error: HrmErrorKind.Incompatible, errorName: "Sensor sin pulso",
                detail: new HrmIncompatibleException().Message));
            Assert.That(model.ErrorHuman, Is.EqualTo("No es un pulsómetro compatible (sin servicio Heart Rate)"));
            Assert.That(model.ErrorDetail, Is.Null);
            Assert.That(model.ErrorText, Is.EqualTo("No es un pulsómetro compatible (sin servicio Heart Rate)"));
        }
    }
}
