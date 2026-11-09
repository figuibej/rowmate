using System;
using System.Collections;
using NUnit.Framework;
using RowMate.App.Device;
using RowMate.Core.Ble;
using RowMate.Core.Common;
using RowMate.Presentation.Controls;
using RowMate.Tests.Support;
using RowMate.Tests.UiSupport;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace RowMate.Tests.PlayMode
{
    // HCV · La tarjeta del pulsómetro con el pulsómetro REAL (HeartRateService, Core) y el remo real sobre el adaptador falso con los
    // sensores ficticios K-11 (11-ble-pulsometro/02-tech-design §4.1, §6.3; HostBackends.FakeBle con MemoryPreferences). Ninguna prueba
    // toca la radio ni el prefs.json del usuario: las preferencias son una MemoryPreferences por prueba.
    public sealed class HrmCardRealTests : UiTestBase
    {
        private MemoryPreferences _prefs = new MemoryPreferences();

        private FakeBleAdapter Adapter => H.FakeBle!.Adapter;

        private IEnumerator CreateReal(Action<FakeBleAdapter>? setup = null, bool remembered = false, DeviceProfile? profile = null)
        {
            _prefs = new MemoryPreferences();
            if (remembered)
            {
                _prefs.SetString(HeartRateService.PrefDeviceId, FakeBleDevices.HrmId);
                _prefs.SetString(HeartRateService.PrefDeviceName, "Banda de prueba");
            }

            Create(new UiHostOptions { Profile = profile ?? DeviceProfile.TV }.WithRealHrm(_prefs, a =>
            {
                // El sensor recordado tiene que existir para el adaptador falso (como un equipo prendido y cerca).
                a.Advertise(FakeBleDevices.Hrm);
                setup?.Invoke(a);
            }));
            yield return Settle();
        }

        private IEnumerator SearchHrm(params FakeBleDevice[] devices)
        {
            H.ScrollTo("hrm-search");
            yield return Tap("hrm-search");
            foreach (var device in devices)
            {
                Adapter.Advertise(device);
            }

            yield return Settle();
        }

        private IEnumerator ConnectHrm()
        {
            yield return SearchHrm(FakeBleDevices.Hrm, FakeBleDevices.Watch);
            H.ScrollTo("hrm-device-" + FakeBleDevices.HrmId);
            yield return Tap("hrm-device-" + FakeBleDevices.HrmId);
            yield return Settle();
            Assert.That(H.App.HeartRate.State.Value.Status, Is.EqualTo(HrmStatus.Connected));
        }

        private HrmCardBody? Body => H.HeartRateCard!.Body;

        [UnityTest, Category("AC-APP-01-10")]
        public IEnumerator RememberedReconnectsOnStart()
        {
            yield return CreateReal(remembered: true);
            Assert.That(H.App.HeartRate.State.Value.Status, Is.EqualTo(HrmStatus.Connected), "se reconectó solo al abrir");
            Assert.That(Body, Is.EqualTo(HrmCardBody.Connected));
            Assert.That(H.TextOf("hrm-connected-name"), Is.EqualTo("Banda de prueba"));
            Assert.That(Adapter.ScanStarts, Is.EqualTo(0), "directo a la dirección recordada, sin buscar");
            Assert.That(H.Shell.Toasts.CurrentHrm?.Kind, Is.EqualTo(HrmNoticeKind.Connected), "aviso Pulsómetro conectado");
        }

        [UnityTest, Category("AC-HRM-07-03"), Category("AC-HRM-13-13")]
        public IEnumerator ReconnectingThreeButtonsConnectDisabled()
        {
            yield return CreateReal(a => a.Fail(FakeBleDevices.HrmId, new BleException("connect", "device not found")), true);
            yield return Advance(5);
            Assert.That(Body, Is.EqualTo(HrmCardBody.Remembered));
            Assert.That(H.TextOf("hrm-remembered-name") + H.TextOf("hrm-remembered-suffix"), Is.EqualTo("Banda de prueba · reconectando..."));
            Assert.That(H.Find("hrm-retry")!.enabledSelf, Is.False, "Conectar deshabilitado mientras reintenta");
            Assert.That(H.Find("hrm-search")!.enabledSelf, Is.True, "Buscar otro habilitado");
            Assert.That(H.Find("hrm-forget")!.enabledSelf, Is.True, "Olvidar habilitado");
            var indicator = H.HeartRateCard!.Indicator!;
            Assert.That(indicator.Current, Is.EqualTo(LinkIndicator.Mode.WaitingArc), "el arco de la espera de 10 s");
            Assert.That(indicator.Fraction, Is.InRange(0.45, 0.75), "cerca de la mitad a los 5 s");
            Assert.That(H.HeartRateCard.ClassListContains("rm-hrm-card--breathing"), Is.True);
            yield return Advance(51);
            Assert.That(H.TextOf("hrm-remembered-suffix"), Is.EqualTo(" · no encontrado"), "6 intentos y se rinde");
            Assert.That(H.Find("hrm-retry")!.enabledSelf, Is.True);
            Assert.That(H.HeartRateCard.Indicator!.Current, Is.EqualTo(LinkIndicator.Mode.LinkOff));
            AssertClean(H);
        }

        [UnityTest, Category("AC-HRM-13-13"), Category("LT-28")]
        public IEnumerator LT28_RealService()
        {
            yield return CreateReal();
            Assert.That(Body, Is.EqualTo(HrmCardBody.NoSensor));
            yield return SearchHrm(FakeBleDevices.Hp, FakeBleDevices.Hrm, FakeBleDevices.Row, FakeBleDevices.Watch, FakeBleDevices.HrAnon);
            Assert.That(Body, Is.EqualTo(HrmCardBody.Scanning));
            Assert.That(H.Find("hrm-device-" + FakeBleDevices.HrmId), Is.Not.Null);
            Assert.That(H.Find("hrm-device-" + FakeBleDevices.HpId), Is.Null, "los auriculares nunca");
            Adapter.NeverRespond(FakeBleDevices.HrmId);
            H.ScrollTo("hrm-device-" + FakeBleDevices.HrmId);
            yield return Tap("hrm-device-" + FakeBleDevices.HrmId);
            Assert.That(Body, Is.EqualTo(HrmCardBody.Connecting));
            Assert.That(H.TextOf("hrm-connecting-text"), Is.EqualTo("Conectando a Banda de prueba..."));
            Assert.That(H.HeartRateCard!.Query<Button>().ToList().Count, Is.EqualTo(0), "conectando: sin botones (Q-45)");
            yield return Advance(15.1);
            yield return Settle();
            Assert.That(H.TextOf("hrm-error-human"), Is.EqualTo("No se pudo conectar con Banda de prueba"));
            Adapter.Respond(FakeBleDevices.HrmId);
            yield return ConnectHrm();
            Assert.That(H.TextOf("hrm-bpm"), Is.EqualTo("--"), "antes de la primera lectura");
            Assert.That(H.IsShown("hrm-stale"), Is.False);
            Assert.That(H.IsShown("hrm-error"), Is.False, "conectar borra el error");
            Adapter.Notify(FakeBleDevices.HrmId, HeartRateParser.Measurement, HrPackets.Contact142);
            yield return Settle();
            Assert.That(H.TextOf("hrm-bpm"), Is.EqualTo("142"));
            yield return Tap("hrm-disconnect");
            Assert.That(Body, Is.EqualTo(HrmCardBody.NoSensor));
        }

        [UnityTest, Category("AC-HRM-05-03")]
        public IEnumerator StaleLineAppearsAndLeaves()
        {
            yield return CreateReal();
            yield return ConnectHrm();
            Adapter.Notify(FakeBleDevices.HrmId, HeartRateParser.Measurement, HrPackets.Contact142);
            yield return Settle();
            Assert.That(H.TextOf("hrm-bpm"), Is.EqualTo("142"));
            Assert.That(H.HeartRateCard!.Model!.HeartFull, Is.True);
            yield return Advance(10.1);
            yield return Settle();
            Assert.That(H.TextOf("hrm-bpm"), Is.EqualTo("--"));
            Assert.That(H.IsShown("hrm-stale"), Is.True);
            Assert.That(H.TextOf("hrm-stale"), Is.EqualTo("Sin señal del sensor"));
            Assert.That(H.HeartRateCard.Model!.HeartFull, Is.False, "corazón al 40 %");
            Assert.That(H.App.HeartRate.State.Value.Status, Is.EqualTo(HrmStatus.Connected), "no corta el enlace");
            Adapter.Notify(FakeBleDevices.HrmId, HeartRateParser.Measurement, HrPackets.Contact142);
            yield return Settle();
            Assert.That(H.IsShown("hrm-stale"), Is.False);
            Assert.That(H.TextOf("hrm-bpm"), Is.EqualTo("142"));
        }

        [UnityTest, Category("AC-HRM-10-01")]
        public IEnumerator DisconnectToNoSensor()
        {
            yield return CreateReal();
            yield return ConnectHrm();
            Assert.That(_prefs.TryGetString(HeartRateService.PrefDeviceId, out _), Is.True, "recordado al conectar");
            yield return Tap("hrm-disconnect");
            Assert.That(Body, Is.EqualTo(HrmCardBody.NoSensor));
            Assert.That(_prefs.TryGetString(HeartRateService.PrefDeviceId, out _), Is.False, "Desconectar olvida");
            Assert.That(_prefs.TryGetString(HeartRateService.PrefDeviceName, out _), Is.False);
            yield return Advance(10);
            Assert.That(H.App.HeartRate.State.Value.IsRetrying, Is.False, "sin cadencia de caída");
        }

        [UnityTest, Category("AC-HRM-13-14")]
        public IEnumerator ErrorThreeLinesMax()
        {
            var detail = "gatt error 133 " + new string('x', 160);
            yield return CreateReal(a => a.Fail(FakeBleDevices.HrmId, new BleException("connect", detail)));
            yield return SearchHrm(FakeBleDevices.Hrm);
            H.ScrollTo("hrm-device-" + FakeBleDevices.HrmId);
            yield return Tap("hrm-device-" + FakeBleDevices.HrmId);
            yield return Settle();
            yield return Frames(3);
            Assert.That(H.TextOf("hrm-error-human"), Is.EqualTo("No se pudo conectar con Banda de prueba"));
            var human = H.Find<Label>("hrm-error-human")!;
            var detailLabel = H.Find<Label>("hrm-error-detail")!;
            Assert.That(H.IsShown("hrm-error-detail"), Is.True);
            Assert.That(detailLabel.resolvedStyle.whiteSpace, Is.EqualTo(WhiteSpace.NoWrap), "el detalle en una línea");
            Assert.That(detailLabel.resolvedStyle.textOverflow, Is.EqualTo(TextOverflow.Ellipsis), "con …");
            var line = human.MeasureTextSize("Ag", human.contentRect.width, VisualElement.MeasureMode.Exactly, 0, VisualElement.MeasureMode.Undefined).y;
            Assert.That(human.layout.height, Is.LessThanOrEqualTo(line * 2 + 1f), "la frase hasta 2 líneas");
            var detailLine = detailLabel.MeasureTextSize("Ag", 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined).y;
            Assert.That(detailLabel.layout.height, Is.LessThanOrEqualTo(detailLine + 1f), "el detalle en 1 línea");

            // Incompatible: solo la frase.
            Adapter.Respond(FakeBleDevices.HrmId);
            yield return SearchHrm(FakeBleDevices.NoHr);
            H.ScrollTo("hrm-device-" + FakeBleDevices.NoHrId);
            yield return Tap("hrm-device-" + FakeBleDevices.NoHrId);
            yield return Settle();
            Assert.That(H.TextOf("hrm-error-human"), Is.EqualTo("No es un pulsómetro compatible (sin servicio Heart Rate)"));
            Assert.That(H.IsShown("hrm-error-detail"), Is.False, "incompatible sin detalle");
        }

        [UnityTest, Category("AC-HRM-13-13")]
        public IEnumerator AllBodiesRealService()
        {
            yield return CreateReal(a => a.NeverRespond(FakeBleDevices.HrmId), true);
            Assert.That(Body, Is.EqualTo(HrmCardBody.Remembered), "recordado e intentando");
            Assert.That(H.HeartRateCard!.Indicator!.Current, Is.EqualTo(LinkIndicator.Mode.Spinner), "spinner durante el intento");
            yield return Tap("hrm-forget");
            Assert.That(Body, Is.EqualTo(HrmCardBody.NoSensor), "Olvidar corta la cadencia y borra la memoria");
            Assert.That(_prefs.TryGetString(HeartRateService.PrefDeviceId, out _), Is.False);
            Adapter.Respond(FakeBleDevices.HrmId);
            yield return ConnectHrm();
            Assert.That(Body, Is.EqualTo(HrmCardBody.Connected));
            // La caída: reconectando con la cadencia de 3 s.
            Adapter.NeverRespond(FakeBleDevices.HrmId);
            Adapter.DropLink(FakeBleDevices.HrmId);
            yield return Settle();
            Assert.That(Body, Is.EqualTo(HrmCardBody.Remembered));
            Assert.That(H.TextOf("hrm-remembered-suffix"), Is.EqualTo(" · reconectando..."));
            Assert.That(H.Shell.Toasts.CurrentHrm, Is.Null, "fuera del entrenamiento no hay aviso de caída");
            Adapter.Respond(FakeBleDevices.HrmId);
            // El intento pendiente vence a los 15 s (su continuación corre en el cuadro siguiente) y el próximo sale 3 s después.
            yield return Advance(15.1);
            yield return Advance(3.1);
            yield return Settle();
            Assert.That(Body, Is.EqualTo(HrmCardBody.Connected), "reconectó");
        }

        [UnityTest, Category("AC-HRM-13-13"), Category("LT-30")]
        public IEnumerator LT30_RealService()
        {
            foreach (var profile in new[] { DeviceProfile.L640, DeviceProfile.TH })
            {
                yield return CreateReal(profile: profile);
                yield return SearchHrm(FakeBleDevices.Hrm, FakeBleDevices.Watch, FakeBleDevices.HrAnon);
                var layout = H.DeviceScreen!.Scan.Layout.Kind;
                Assert.That(layout, Is.EqualTo(profile.Code == DeviceProfile.L640.Code ? DeviceScanLayoutKind.SingleScroll : DeviceScanLayoutKind.TwoColumns),
                    profile.Code);
                var audit = H.Audit();
                Assert.That(audit.IsClean, Is.True, profile.Code + ": " + audit);
                H.ScrollTo("hrm-device-" + FakeBleDevices.HrAnonId);
                Assert.That(H.WhyNotTappable("hrm-device-" + FakeBleDevices.HrAnonId), Is.Null, "la última fila se alcanza (" + profile.Code + ")");
            }
        }
    }
}
