using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using RowMate.App.Device;
using RowMate.Core.Ble;
using RowMate.Core.Design;
using RowMate.Presentation.Controls;
using RowMate.Tests.Support;
using RowMate.Tests.UiSupport;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace RowMate.Tests.PlayMode
{
    // Tarjeta del pulsómetro con el pulsómetro de prueba (F-HRM-13; 02-tech-design §3.4).
    public sealed class HeartRateCardStatesTests : UiTestBase
    {
        private ScriptedHeartRateService Hrm => H.TestHeartRate!;

        private IEnumerator Open()
        {
            Create(new UiHostOptions());
            yield return Settle();
        }

        [UnityTest, Category("AC-HRM-13-02")]
        public IEnumerator HeaderAndStructure()
        {
            yield return Open();
            Assert.That(H.TextOf("hrm-title"), Is.EqualTo("PULSÓMETRO"));
            var heart = H.Find<IconView>("hrm-heart")!;
            Assert.That(heart.IsFilled, Is.True);
            Assert.That(heart.resolvedStyle.opacity, Is.EqualTo(0.40f).Within(0.01f), "sin conexión: 40 %");
            var card = H.Find("hrm-card")!;
            Assert.That(card.style.borderTopColor.value.a, Is.EqualTo(0.6f).Within(0.01f), "acento fijo al 60 %");
            Hrm.SetStatus(HrmStatus.Connected);
            yield return Settle();
            Assert.That(heart.resolvedStyle.opacity, Is.EqualTo(1f).Within(0.01f), "conectado: pleno (aun con --)");
            Assert.That(H.TextOf("hrm-bpm"), Is.EqualTo("--"));
        }

        [UnityTest, Category("AC-HRM-13-03")]
        public IEnumerator BodiesWithScriptedHrm()
        {
            yield return Open();
            var card = H.HeartRateCard!;
            Assert.That(card.Body, Is.EqualTo(HrmCardBody.NoSensor));
            Assert.That(H.IsTextVisible("Sin sensor"), Is.True);
            Hrm.SetStatus(HrmStatus.Scanning);
            yield return Settle();
            Assert.That(card.Body, Is.EqualTo(HrmCardBody.Scanning));
            Assert.That(H.IsTextVisible("Buscando sensores..."), Is.True);
            Hrm.ConnectBehavior = ScriptedConnect.StayConnecting;
            _ = H.App.HeartRate.ConnectAsync("7E:1A:2B:3C", ScriptedHeartRateService.RememberedSample);
            yield return Settle();
            Assert.That(card.Body, Is.EqualTo(HrmCardBody.Connecting));
            Assert.That(H.TextOf("hrm-connecting-text"), Is.EqualTo("Conectando a Polar H10 7E1A2B3C..."));
            Hrm.Remember(ScriptedHeartRateService.RememberedSample, true);
            yield return Settle();
            Assert.That(card.Body, Is.EqualTo(HrmCardBody.Remembered));
            Assert.That(H.TextOf("hrm-remembered-text"), Is.EqualTo("Polar H10 7E1A2B3C · reconectando..."));
            Hrm.Remember(ScriptedHeartRateService.RememberedSample, false);
            yield return Settle();
            Assert.That(H.TextOf("hrm-remembered-text"), Is.EqualTo("Polar H10 7E1A2B3C · no encontrado"));
            Hrm.SetStatus(HrmStatus.Connected);
            yield return Settle();
            Assert.That(card.Body, Is.EqualTo(HrmCardBody.Connected));
        }

        [UnityTest, Category("AC-HRM-13-04")]
        public IEnumerator DisabledButtonsDoNotRespond()
        {
            yield return Open();
            H.TestRower!.Emit(BleStatus.Scanning);
            yield return Settle();
            var search = H.Find("hrm-search")!;
            Assert.That(search.enabledSelf, Is.False, "remo buscando: Buscar deshabilitado");
            Assert.That(search.resolvedStyle.opacity, Is.EqualTo(0.38f).Within(0.01f));
            H.TapUnchecked("hrm-search");
            yield return Frames();
            Assert.That(Hrm.StartScanCalls, Is.EqualTo(0), "deshabilitado no responde");
            H.TestRower.Emit(BleStatus.Disconnected);
            yield return Settle();
            Hrm.Remember(ScriptedHeartRateService.RememberedSample, true);
            yield return Settle();
            Assert.That(H.Find("hrm-retry")!.enabledSelf, Is.False, "reintentando: Conectar deshabilitado");
            Assert.That(H.Find("hrm-forget")!.enabledSelf, Is.True);
            Assert.That(H.Find("hrm-search")!.enabledSelf, Is.True, "Buscar otro habilitado");
        }

        [UnityTest, Category("AC-HRM-13-05")]
        public IEnumerator DeviceRowContent()
        {
            yield return Open();
            Hrm.ScanResults = new List<BleScanResult>
            {
                new BleScanResult("AA:01", "Polar H10", "Polar adv", -60, new BleUuid[0]),
                new BleScanResult("AA:02", "", "Solo anunciado", -70, new BleUuid[0]),
                new BleScanResult("AA:03", null, null, -80, new BleUuid[0]),
            };
            yield return Tap("hrm-search");
            Assert.That(UiQuery.TextOf(H.Find("hrm-device-AA:01")!.Q<Label>(className: "rm-hrm-device-row__title")), Is.EqualTo("Polar H10"));
            Assert.That(UiQuery.TextOf(H.Find("hrm-device-AA:02")!.Q<Label>(className: "rm-hrm-device-row__title")), Is.EqualTo("Solo anunciado"));
            Assert.That(UiQuery.TextOf(H.Find("hrm-device-AA:03")!.Q<Label>(className: "rm-hrm-device-row__title")), Is.EqualTo("Sensor desconocido"));
            Assert.That(UiQuery.TextOf(H.Find("hrm-device-AA:01")!.Q<Label>(className: "rm-hrm-device-row__subtitle")), Is.EqualTo("AA:01"));
            Hrm.ConnectBehavior = ScriptedConnect.StayConnecting;
            yield return Tap("hrm-device-AA:01");
            Assert.That(Hrm.ConnectCalls, Is.EqualTo(new[] { "AA:01" }));
            Assert.That(H.TextOf("hrm-connecting-text"), Is.EqualTo("Conectando a Polar H10..."));
        }

        [UnityTest, Category("AC-HRM-13-06")]
        public IEnumerator ConnectedBodyLayout()
        {
            yield return Open();
            Hrm.ConnectedName = "Un nombre de sensor larguísimo que no entra en una sola línea de la tarjeta";
            Hrm.SetStatus(HrmStatus.Connected);
            Hrm.EmitBpm(142);
            yield return Settle();
            Assert.That(H.TextOf("hrm-bpm"), Is.EqualTo("142"));
            var name = H.Find<Label>("hrm-connected-name")!;
            Assert.That(name.resolvedStyle.whiteSpace, Is.EqualTo(WhiteSpace.NoWrap), "una línea");
            Assert.That(name.resolvedStyle.textOverflow, Is.EqualTo(TextOverflow.Ellipsis), "con …");
            var bpm = H.HeartRateCard!.Bpm!;
            Assert.That(bpm.FontSize, Is.EqualTo(52).Within(0.01f));
            var disconnect = H.BoundsOf("hrm-disconnect");
            Assert.That(disconnect.xMin, Is.GreaterThan(H.BoundsOf("hrm-bpm-box").xMax - 0.5f), "Desconectar a la derecha");
            var unit = H.HeartRateCard.BpmUnit!;
            Assert.That(unit.text, Is.EqualTo("bpm"));
            var number = H.BoundsOf("hrm-bpm");
            var unitBaseline = unit.worldBound.yMin + unit.resolvedStyle.paddingTop
                + unit.resolvedStyle.unityFontDefinition.fontAsset.faceInfo.ascentLine * unit.resolvedStyle.fontSize / unit.resolvedStyle.unityFontDefinition.fontAsset.faceInfo.pointSize;
            var numberBaseline = number.yMin + bpm.BaselineFromTop * bpm.worldTransform.lossyScale.y;
            Assert.That(unitBaseline, Is.EqualTo(numberBaseline).Within(1f), "bpm por la línea de base del número");
            AssertClean(H);
        }

        [UnityTest, Category("AC-HRM-13-07")]
        public IEnumerator ErrorClampedToThreeLines()
        {
            yield return Open();
            Hrm.ConnectBehavior = ScriptedConnect.ThrowIncompatible;
            _ = H.App.HeartRate.ConnectAsync("AA", "X");
            yield return Settle();
            Assert.That(H.TextOf("hrm-error"), Is.EqualTo("No es un pulsómetro compatible (sin servicio Heart Rate)"));
            Hrm.ConnectBehavior = ScriptedConnect.ThrowError;
            Hrm.ErrorText = new string('x', 120) + " " + new string('y', 120) + " " + new string('z', 120);
            _ = H.App.HeartRate.ConnectAsync("AA", "X");
            yield return Settle();
            yield return Frames(3);
            var error = H.Find<Label>("hrm-error")!;
            Assert.That(H.HeartRateCard!.ErrorIsClamped, Is.True, "texto largo: cortado");
            Assert.That(error.text, Does.EndWith("…"));
            var lineHeight = error.MeasureTextSize("Ag", error.contentRect.width, VisualElement.MeasureMode.Exactly, 0, VisualElement.MeasureMode.Undefined).y;
            Assert.That(error.layout.height, Is.LessThanOrEqualTo(lineHeight * 3 + 1f), "hasta 3 líneas");
            AssertColor(error.resolvedStyle.color, Tokens.StateDangerText);
        }

        [UnityTest, Category("AC-HRM-13-11")]
        public IEnumerator NoExtraControls()
        {
            yield return Open();
            Hrm.SetStatus(HrmStatus.Scanning);
            yield return Settle();
            var card = H.Find("hrm-card")!;
            Assert.That(card.Query<Button>().ToList().Count, Is.EqualTo(0), "buscando sin lista: sin botones (ni Cancelar ni Detener)");
            Assert.That(H.HasText("Cancelar"), Is.False);
            Hrm.ConnectBehavior = ScriptedConnect.StayConnecting;
            _ = H.App.HeartRate.ConnectAsync("AA", "X");
            yield return Settle();
            Assert.That(card.Query<Button>().ToList().Count, Is.EqualTo(0), "conectando: sin botones");
        }

        private static void AssertColor(Color actual, ColorToken token)
        {
            var expected = TokenColors.ToColor(token);
            Assert.That(Mathf.Abs(actual.r - expected.r) + Mathf.Abs(actual.g - expected.g) + Mathf.Abs(actual.b - expected.b), Is.LessThan(0.01f), token.Name);
        }
    }
}
