using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using RowMate.App.Device;
using RowMate.Core.Ble;
using RowMate.Core.Workout;
using RowMate.Tests.Support;
using RowMate.Tests.UiSupport;
using UnityEngine;
using UnityEngine.TestTools;

namespace RowMate.Tests.PlayMode
{
    // HLN · El aviso del enlace sobre el HUD (10a 02-tech-design §6.3; AC-BLE-08-06, AC-BLE-09-02; DV-98, DV-99; AC-WKT-03-10 de la 4a,
    // que ahora sí muestra el aviso): el remo real (RowerService) sobre el adaptador falso, con un paquete de Rower Data por segundo. El
    // aviso es el #toast del ToastLayer (no suma elementos bajo #immersive: HudBudgetTests no cambia) y no tapa SPM, Split, Watts,
    // BPM, kcal ni la banda en los cinco perfiles.
    public sealed class HudLinkNoticeTests : WorkoutUiTestBase
    {
        private static readonly string[] Metrics = { "hud-spm", "hud-split", "hud-watts", "hud-bpm", "hud-kcal", "hud-band" };

        private FakeBleAdapter Adapter => H.FakeBle!.Adapter;

        private IDisposable? _packets;

        [TearDown]
        public void StopPackets()
        {
            _packets?.Dispose();
            _packets = null;
        }

        // El remo conectado y mandando un paquete por segundo; después, libre.
        private IEnumerator RowingFree(DeviceProfile profile, bool packets = true)
        {
            CreateWorkout(configure: o =>
            {
                o.Backend = HostBackends.FakeBle();
                o.Profile = profile;
                o.IsDesktop = profile.Code == DeviceProfile.W1280.Code;
            });
            yield return Settle();
            var scan = H.App.Device.StartScanAsync();
            Adapter.Advertise(FakeBleDevices.Row);
            yield return Frames();
            _ = H.App.Device.ConnectAsync(FakeBleDevices.RowId);
            H.Advance(1);
            yield return Frames();
            Assert.That(H.App.Device.State.Value.Status, Is.EqualTo(BleStatus.Connected));
            if (packets)
            {
                _packets = Adapter.NotifyEvery(FakeBleDevices.RowId, TimeSpan.FromSeconds(1), _ => FakeBleDevices.RowerData);
            }
            else
            {
                Adapter.Notify(FakeBleDevices.RowId, FtmsUuids.RowerData, FakeBleDevices.RowerData);
            }

            yield return StartFree();
            Assert.That(H.Immersive, Is.Not.Null);
            _ = scan;
        }

        private LinkToast? Link => H.Shell.Toasts.CurrentLink;

        [UnityTest, Category("AC-BLE-08-06"), Category("AC-WKT-03-10")]
        public IEnumerator ReconnectingIsStickyAndReconnectedLastsTwoSeconds()
        {
            yield return RowingFree(DeviceProfile.W1280);
            yield return AdvanceTo(5);
            var spm = H.TextOf("hud-spm-value");
            Adapter.NeverRespond(FakeBleDevices.RowId);
            Adapter.DropLink(FakeBleDevices.RowId);
            yield return Settle();
            Assert.That(Link, Is.Not.Null);
            Assert.That(Link!.Kind, Is.EqualTo(LinkNoticeKind.Reconnecting));
            Assert.That(H.Shell.Toasts.CurrentText, Is.EqualTo("Reconectando con AMS-670B…"));
            Assert.That(H.Shell.Toasts.Indicator.Current, Is.EqualTo(RowMate.Presentation.Controls.LinkIndicator.Mode.Spinner));
            yield return AdvanceTo(16);
            Assert.That(Link?.Kind, Is.EqualTo(LinkNoticeKind.Reconnecting), "fijo mientras reconecta (no se va a los 4 s)");
            Assert.That(Phase, Is.EqualTo(WorkoutPhase.Active), "la sesión no se pausa ni se corta");
            Assert.That(H.TextOf("hud-time-value"), Is.EqualTo("00:16"), "el reloj sigue");
            Assert.That(H.TextOf("hud-spm-value"), Is.EqualTo(spm), "las métricas quedan en la última lectura");
            // El intento de los 8 s vence a los 18 (tope de 10 s); el siguiente, a los 21, ya responde. El vencimiento cancela un token y su
            // continuación la corre el contexto de Unity en el cuadro siguiente (como en la app): se avanza en dos tramos.
            Adapter.Respond(FakeBleDevices.RowId);
            yield return AdvanceTo(18.2);
            yield return Frames();
            yield return AdvanceTo(21.5);
            yield return Settle();
            Assert.That(H.App.Device.State.Value.Status, Is.EqualTo(BleStatus.Connected));
            Assert.That(Link?.Kind, Is.EqualTo(LinkNoticeKind.Reconnected));
            Assert.That(H.Shell.Toasts.CurrentText, Is.EqualTo("Reconectado con AMS-670B"));
            H.Advance(2.05);
            yield return Settle();
            Assert.That(H.Shell.Toasts.IsVisible, Is.False, "Reconectado dura 2 s (DV-99)");
        }

        [UnityTest, Category("AC-BLE-09-02")]
        public IEnumerator MonitorStoppedLastsFourSecondsAndTheSessionGoesOn()
        {
            yield return RowingFree(DeviceProfile.W1280, packets: false);
            yield return AdvanceTo(6);
            yield return Settle();
            Assert.That(Link?.Kind, Is.EqualTo(LinkNoticeKind.MonitorStopped));
            Assert.That(H.Shell.Toasts.CurrentText, Is.EqualTo("El monitor dejó de enviar datos"));
            Assert.That(H.Shell.Toasts.Indicator.Current, Is.EqualTo(RowMate.Presentation.Controls.LinkIndicator.Mode.LinkOff));
            H.Advance(4.05);
            yield return Settle();
            Assert.That(H.Shell.Toasts.IsVisible, Is.False, "4 s (DV-98)");
            Assert.That(Phase, Is.EqualTo(WorkoutPhase.Active), "la sesión sigue, como en el legacy");
        }

        [UnityTest, Category("AC-BLE-08-06")]
        public IEnumerator ACommonToastReplacesTheStickyOneForFourSeconds()
        {
            yield return RowingFree(DeviceProfile.W1280);
            yield return AdvanceTo(3);
            Adapter.NeverRespond(FakeBleDevices.RowId);
            Adapter.DropLink(FakeBleDevices.RowId);
            yield return Settle();
            H.Shell.Toasts.Show("Aviso común");
            yield return Settle();
            Assert.That(H.Shell.Toasts.CurrentText, Is.EqualTo("Aviso común"));
            Assert.That(Link, Is.Null);
            H.Advance(4.05);
            yield return Settle();
            Assert.That(Link?.Kind, Is.EqualTo(LinkNoticeKind.Reconnecting), "después vuelve el fijo");
        }

        [UnityTest, Category("AC-BLE-08-06")]
        public IEnumerator TheNoticeCoversNoMetricTv()
        {
            yield return NoMetricCovered(DeviceProfile.TV);
        }

        [UnityTest, Category("AC-BLE-08-06")]
        public IEnumerator TheNoticeCoversNoMetricP360()
        {
            yield return NoMetricCovered(DeviceProfile.P360);
        }

        [UnityTest, Category("AC-BLE-08-06")]
        public IEnumerator TheNoticeCoversNoMetricTh()
        {
            yield return NoMetricCovered(DeviceProfile.TH);
        }

        [UnityTest, Category("AC-BLE-08-06")]
        public IEnumerator TheNoticeCoversNoMetricL640()
        {
            yield return NoMetricCovered(DeviceProfile.L640);
        }

        [UnityTest, Category("AC-BLE-08-06")]
        public IEnumerator TheNoticeCoversNoMetricW1280()
        {
            yield return NoMetricCovered(DeviceProfile.W1280);
        }

        private IEnumerator NoMetricCovered(DeviceProfile profile)
        {
            yield return RowingFree(profile);
            yield return AdvanceTo(3);
            Adapter.NeverRespond(FakeBleDevices.RowId);
            Adapter.DropLink(FakeBleDevices.RowId);
            yield return Settle();
            yield return Frames(3);
            Assert.That(H.Shell.Toasts.IsVisible, Is.True);
            var toast = H.Shell.Toasts.Toast.worldBound;
            var crossed = new List<string>();
            foreach (var metric in Metrics)
            {
                var element = H.Find(metric);
                if (element == null || !H.IsShown(metric))
                {
                    continue;
                }

                if (Overlap(toast, element.worldBound))
                {
                    crossed.Add(metric + " " + element.worldBound);
                }
            }

            Debug.Log("[HLN] " + profile.Code + ": aviso en " + toast);
            Assert.That(crossed, Is.Empty, profile.Code + ": el aviso " + toast + " tapa " + string.Join(", ", crossed));
        }
    }
}
