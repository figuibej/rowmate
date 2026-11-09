using System;
using System.Collections.Generic;
using NUnit.Framework;
using RowMate.App.Device;
using RowMate.Core.Ble;
using RowMate.Tests.EditMode.Localization;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Ble
{
    // HeartRateService: la reconexión al abrir la app y tras una caída (hrs:207-227, :263-272; 11-ble-pulsometro/02-tech-design §2.5).
    [TestFixture]
    public sealed class HeartRateServiceReconnectTests
    {
        private static readonly BleException Gatt133 = new BleException("connect", "gatt error 133", 133);

        // Las horas (segundos del reloj de prueba) en que el servicio pidió conectar el equipo.
        private static List<double> AttemptTimes(HrmRig rig, Action run, double until)
        {
            var times = new List<double>();
            var seen = 0;
            run();
            while (true)
            {
                var calls = rig.Adapter.ConnectCalls(FakeBleDevices.HrmId);
                for (; seen < calls; seen++)
                {
                    times.Add(rig.Now);
                }

                if (rig.Now >= until)
                {
                    break;
                }

                rig.Wait(0.5);
            }

            return times;
        }

        // AC-HRM-07-01: solo con recordado, desconectado y sin búsqueda del usuario; primer intento inmediato, directo, sin buscar.
        [Test, Category("AC-HRM-07-01")]
        public void PreconditionsAndImmediateDirectAttempt()
        {
            using (var rig = new HrmRig())
            {
                rig.Hrm.AutoConnectAsync();
                Assert.That(rig.Adapter.TotalConnectCalls, Is.EqualTo(0), "sin recordado no hace nada");
                Assert.That(rig.Hrm.IsRetrying, Is.False);
            }

            using (var rig = new HrmRig(seed: HrmRig.Remembered(FakeBleDevices.Hrm)))
            {
                rig.Adapter.NeverRespond(FakeBleDevices.HrmId);
                rig.Hrm.AutoConnectAsync();
                Assert.That(rig.Adapter.ConnectCalls(FakeBleDevices.HrmId), Is.EqualTo(1), "el primer intento, en el acto");
                Assert.That(rig.Adapter.ScanStarts, Is.EqualTo(0), "sin buscar");
                Assert.That(rig.Hrm.IsRetrying, Is.True);
                Assert.That(rig.Link.Reason, Is.EqualTo(HrmRetryReason.Startup));
                Assert.That(rig.Link.Reconnect, Is.EqualTo(ReconnectPhase.Attempting));
                rig.Hrm.AutoConnectAsync();
                Assert.That(rig.Adapter.ConnectCalls(FakeBleDevices.HrmId), Is.EqualTo(1), "conectando: no hace nada");
            }

            using (var rig = new HrmRig(seed: HrmRig.Remembered(FakeBleDevices.Hrm)))
            {
                rig.Hrm.StartScanAsync();
                rig.Hrm.AutoConnectAsync();
                Assert.That(rig.Adapter.TotalConnectCalls, Is.EqualTo(0), "una búsqueda del usuario tiene prioridad");
                Assert.That(rig.Hrm.IsRetrying, Is.False);
            }
        }

        // AC-HRM-07-02: con el sensor apagado (cada intento falla al instante), intentos a los 0, 10, 20, 30, 40 y 50 s y ninguno más;
        // después, "{nombre} · no encontrado" con Conectar habilitado.
        [Test, Category("AC-HRM-07-02")]
        public void SixAttemptsAt0To50ThenNone()
        {
            using (var rig = new HrmRig(seed: HrmRig.Remembered(FakeBleDevices.Hrm)))
            using (var store = rig.Store())
            {
                rig.Adapter.Fail(FakeBleDevices.HrmId, Gatt133);
                var times = AttemptTimes(rig, () => rig.Hrm.AutoConnectAsync(), 170);
                Assert.That(times, Is.EqualTo(new[] { 0.0, 10, 20, 30, 40, 50 }));
                Assert.That(rig.Hrm.IsRetrying, Is.False);
                var card = HrmCardRules.Build(store.State.Value, BleStatus.Disconnected, LocalizationFixture.Es);
                Assert.That(card.RememberedLine, Is.EqualTo("Banda de prueba · no encontrado"));
                Assert.That(card.ConnectEnabled, Is.True);
                Assert.That(card.Indicator, Is.EqualTo(HrmIndicator.LinkOff));
                Assert.That(rig.Scheduler.PendingTimers, Is.EqualTo(0));
            }
        }

        // AC-HRM-07-02: con intentos que vencen a los 15 s, el último termina a los 140 s (6 × 15 + 5 × 10).
        [Test, Category("AC-HRM-07-02")]
        public void FifteenSecondAttemptsEndAt140()
        {
            using (var rig = new HrmRig(seed: HrmRig.Remembered(FakeBleDevices.Hrm)))
            {
                rig.Adapter.NeverRespond(FakeBleDevices.HrmId);
                var times = AttemptTimes(rig, () => rig.Hrm.AutoConnectAsync(), 139.5);
                Assert.That(times, Is.EqualTo(new[] { 0.0, 25, 50, 75, 100, 125 }));
                Assert.That(rig.Hrm.IsRetrying, Is.True);
                rig.At(140);
                Assert.That(rig.Hrm.IsRetrying, Is.False);
                Assert.That(rig.Adapter.ConnectCancels(FakeBleDevices.HrmId), Is.EqualTo(6));
                Assert.That(rig.Status, Is.EqualTo(HrmStatus.Disconnected));
            }
        }

        // AC-HRM-07-02: un sensor incompatible cuenta como fallo.
        [Test, Category("AC-HRM-07-02")]
        public void IncompatibleCountsAsFailure()
        {
            using (var rig = new HrmRig(seed: HrmRig.Remembered(FakeBleDevices.NoHr)))
            {
                rig.Hrm.AutoConnectAsync();
                rig.At(120);
                Assert.That(rig.Adapter.ConnectCalls(FakeBleDevices.NoHrId), Is.EqualTo(6));
                Assert.That(rig.Hrm.IsRetrying, Is.False);
                Assert.That(rig.StoredId, Is.EqualTo(FakeBleDevices.NoHrId), "no lo olvida");
            }
        }

        // AC-HRM-07-02: los fallos nunca llegan a la tarjeta, solo al registro.
        [Test, Category("AC-HRM-07-02")]
        public void FailuresOnlyLogged()
        {
            using (var rig = new HrmRig(seed: HrmRig.Remembered(FakeBleDevices.Hrm)))
            using (var store = rig.Store())
            using (var states = new StateRecorder<HeartRateState>(store.State))
            {
                rig.Adapter.Fail(FakeBleDevices.HrmId, Gatt133);
                rig.Hrm.AutoConnectAsync();
                rig.At(60);
                foreach (var state in states.Values)
                {
                    Assert.That(state.HasError, Is.False);
                }

                Assert.That(rig.HrmLines("Reintento fallido: connect: gatt error 133"), Is.EqualTo(6));
            }
        }

        // AC-HRM-08-01: caída → primero el pulso a 0 y después desconectado; intento inmediato y uno cada 3 s desde el fin del
        // anterior, sin límite (sigue después de 100 fallos).
        [Test, Category("AC-HRM-08-01")]
        public void DropZeroBeforeStatusThenEveryThreeSecondsForever()
        {
            using (var rig = new HrmRig())
            {
                rig.Connected142();
                rig.At(5);
                rig.Adapter.Fail(FakeBleDevices.HrmId, Gatt133);
                rig.Timeline.Clear();
                rig.Adapter.DropLink(FakeBleDevices.HrmId);
                Assert.That(rig.Timeline.GetRange(0, 2), Is.EqualTo(new[] { "bpm:0", "status:Disconnected" }));
                Assert.That(rig.HrmLines("Conexión perdida, reintentando cada 3s"), Is.EqualTo(1));
                Assert.That(rig.Adapter.ConnectCalls(FakeBleDevices.HrmId), Is.EqualTo(2), "intento inmediato");
                Assert.That(rig.Link.Reason, Is.EqualTo(HrmRetryReason.Drop));
                Assert.That(rig.Link.Reconnect, Is.EqualTo(ReconnectPhase.Waiting));
                Assert.That(rig.Link.NextAttemptAt, Is.EqualTo(8.0));
                Assert.That(rig.Link.Wait, Is.EqualTo(TimeSpan.FromSeconds(3)));
                rig.At(8);
                Assert.That(rig.Adapter.ConnectCalls(FakeBleDevices.HrmId), Is.EqualTo(3));
                rig.At(5 + 3 * 120);
                Assert.That(rig.Adapter.ConnectCalls(FakeBleDevices.HrmId), Is.EqualTo(2 + 120), "sin límite");
                Assert.That(rig.Hrm.IsRetrying, Is.True);
            }
        }

        // AC-HRM-08-01: el sensor sigue recordado y la tarjeta dice "{nombre} · reconectando...".
        [Test, Category("AC-HRM-08-01")]
        public void StaysRemembered()
        {
            using (var rig = new HrmRig())
            using (var store = rig.Store())
            {
                rig.Connected142();
                rig.Adapter.Fail(FakeBleDevices.HrmId, Gatt133);
                rig.Adapter.DropLink(FakeBleDevices.HrmId);
                rig.Wait(30);
                Assert.That(rig.StoredId, Is.EqualTo(FakeBleDevices.HrmId));
                var card = HrmCardRules.Build(store.State.Value, BleStatus.Disconnected, LocalizationFixture.Es);
                Assert.That(card.Body, Is.EqualTo(HrmCardBody.Remembered));
                Assert.That(card.RememberedLine, Is.EqualTo("Banda de prueba · reconectando..."));
                Assert.That(card.ConnectEnabled, Is.False);
            }
        }

        // AC-HRM-08-02: la cadencia de caída termina al reconectar, con una acción del usuario o al cerrar.
        [TestCase("reconnect")]
        [TestCase("disconnect")]
        [TestCase("forget")]
        [TestCase("scan")]
        [TestCase("dispose")]
        [Category("AC-HRM-08-02")]
        public void DropEndsOnReconnectUserActionDispose(string how)
        {
            using (var rig = new HrmRig())
            {
                rig.Connected142();
                rig.Adapter.Fail(FakeBleDevices.HrmId, Gatt133);
                rig.Adapter.DropLink(FakeBleDevices.HrmId);
                rig.Wait(10);
                switch (how)
                {
                    case "reconnect":
                        rig.Adapter.Respond(FakeBleDevices.HrmId);
                        rig.Wait(3);
                        Assert.That(rig.Status, Is.EqualTo(HrmStatus.Connected));
                        Assert.That(rig.Link.ConnectedBy, Is.EqualTo(HrmConnectOrigin.Drop));
                        rig.Measure(HrPackets.Contact142);
                        Assert.That(rig.Hrm.CurrentBpm, Is.EqualTo(142), "vuelven las lecturas sin tocar nada");
                        break;
                    case "disconnect":
                        rig.Hrm.DisconnectAsync();
                        break;
                    case "forget":
                        rig.Hrm.ForgetAsync();
                        break;
                    case "scan":
                        rig.Hrm.StartScanAsync();
                        rig.Hrm.StopScanAsync();
                        break;
                    default:
                        rig.Hrm.Dispose();
                        break;
                }

                Assert.That(rig.Hrm.IsRetrying, Is.False, how);
                var calls = rig.Adapter.ConnectCalls(FakeBleDevices.HrmId);
                rig.Wait(60);
                Assert.That(rig.Adapter.ConnectCalls(FakeBleDevices.HrmId), Is.EqualTo(calls), how);
                if (how != "reconnect")
                {
                    Assert.That(rig.Scheduler.PendingTimers, Is.EqualTo(0), how);
                }
            }
        }

        // AC-HRM-08-02: Desconectar nunca dispara la cadencia de caída.
        [Test, Category("AC-HRM-08-02")]
        public void DisconnectNeverDrops()
        {
            using (var rig = new HrmRig())
            {
                rig.Connected142();
                rig.Hrm.DisconnectAsync();
                Assert.That(rig.Hrm.IsRetrying, Is.False);
                Assert.That(rig.HrmLines("Conexión perdida"), Is.EqualTo(0));
                rig.Wait(30);
                Assert.That(rig.Adapter.ConnectCalls(FakeBleDevices.HrmId), Is.EqualTo(1));
            }
        }

        // AC-HRM-08-02 (DT11-09, DV-111): apagar el Bluetooth con el sensor conectado es una caída; los intentos siguen fallando cada
        // 3 s y reconecta al encenderlo.
        [Test, Category("AC-HRM-08-02")]
        public void AdapterOffKeepsFailingThenReconnects()
        {
            using (var rig = new HrmRig())
            {
                rig.Connected142();
                rig.Adapter.SetState(BleAdapterState.Off);
                Assert.That(rig.Status, Is.EqualTo(HrmStatus.Disconnected));
                Assert.That(rig.Hrm.CurrentBpm, Is.EqualTo(0));
                Assert.That(rig.Hrm.IsRetrying, Is.True);
                rig.At(9);
                Assert.That(rig.Adapter.ConnectCalls(FakeBleDevices.HrmId), Is.EqualTo(1 + 4), "0, 3, 6 y 9 s: no se saltean");
                Assert.That(rig.HrmLines("Reintento fallido: connect: adapter off"), Is.EqualTo(4));
                rig.Adapter.SetState(BleAdapterState.On);
                rig.At(12);
                Assert.That(rig.Status, Is.EqualTo(HrmStatus.Connected));
                Assert.That(rig.Hrm.IsRetrying, Is.False);
            }
        }

        // AC-HRM-08-02: cada caída nueva relanza la cadencia (corrida nueva).
        [Test, Category("AC-HRM-08-02")]
        public void NewDropRelaunches()
        {
            using (var rig = new HrmRig())
            {
                rig.Connected142();
                rig.Adapter.DropLink(FakeBleDevices.HrmId);
                Assert.That(rig.Status, Is.EqualTo(HrmStatus.Connected), "reconecta en el intento inmediato");
                var firstRun = 0;
                foreach (var link in rig.Links.Values)
                {
                    if (link.Reason == HrmRetryReason.Drop)
                    {
                        firstRun = link.Run;
                    }
                }

                rig.Adapter.DropLink(FakeBleDevices.HrmId);
                var secondRun = 0;
                foreach (var link in rig.Links.Values)
                {
                    if (link.Reason == HrmRetryReason.Drop)
                    {
                        secondRun = link.Run;
                    }
                }

                Assert.That(firstRun, Is.EqualTo(1));
                Assert.That(secondRun, Is.EqualTo(2));
                Assert.That(rig.HrmLines("Conexión perdida"), Is.EqualTo(2));
                Assert.That(rig.Status, Is.EqualTo(HrmStatus.Connected));
            }
        }

        // AC-HRM-16-02 (DT11-09): con el Bluetooth apagado al abrir, la cadencia del arranque intenta y falla a su ritmo (no saltea).
        [Test, Category("AC-HRM-16-02")]
        public void AdapterOffLoopsKeepRhythm()
        {
            using (var rig = new HrmRig(initial: BleAdapterState.Off, seed: HrmRig.Remembered(FakeBleDevices.Hrm)))
            {
                var times = AttemptTimes(rig, () => rig.Hrm.AutoConnectAsync(), 120);
                Assert.That(times, Is.EqualTo(new[] { 0.0, 10, 20, 30, 40, 50 }));
                Assert.That(rig.Hrm.IsRetrying, Is.False);
            }
        }

        // AC-HRM-13-13: los cuerpos de la tarjeta con el servicio real sobre K-11 (Sin sensor, buscando, conectando manual, conectado,
        // "· reconectando..." con arco y spinner, "· no encontrado").
        [Test, Category("AC-HRM-13-13")]
        public void CardBodiesWithRealService()
        {
            using (var rig = new HrmRig())
            using (var store = rig.Store())
            {
                var es = LocalizationFixture.Es;
                HrmCardViewModel Card() => HrmCardRules.Build(store.State.Value, BleStatus.Disconnected, es);

                Assert.That(Card().Body, Is.EqualTo(HrmCardBody.NoSensor));
                store.StartScanAsync();
                rig.Adapter.Advertise(FakeBleDevices.Hrm);
                Assert.That(Card().Body, Is.EqualTo(HrmCardBody.Scanning));
                Assert.That(Card().Rows[0].Title, Is.EqualTo("Banda de prueba"));
                rig.Adapter.RespondAfter(FakeBleDevices.HrmId, TimeSpan.FromSeconds(1));
                store.ConnectAsync(FakeBleDevices.HrmId, "Banda de prueba");
                Assert.That(Card().Body, Is.EqualTo(HrmCardBody.Connecting));
                Assert.That(Card().ConnectingText, Is.EqualTo("Conectando a Banda de prueba..."));
                rig.Wait(1);
                rig.Measure(HrPackets.Contact142);
                Assert.That(Card().Body, Is.EqualTo(HrmCardBody.Connected));
                Assert.That(Card().BpmText, Is.EqualTo("142"));
                Assert.That(Card().ConnectedName, Is.EqualTo("Banda de prueba"));

                rig.Adapter.Fail(FakeBleDevices.HrmId, Gatt133);
                rig.Adapter.DropLink(FakeBleDevices.HrmId);
                var waiting = Card();
                Assert.That(waiting.Body, Is.EqualTo(HrmCardBody.Remembered));
                Assert.That(waiting.RememberedName + waiting.RememberedSuffix, Is.EqualTo("Banda de prueba · reconectando..."));
                Assert.That(waiting.Indicator, Is.EqualTo(HrmIndicator.WaitingArc));
                Assert.That(waiting.ArcStartAt, Is.EqualTo(rig.Now));
                Assert.That(waiting.ArcDuration, Is.EqualTo(TimeSpan.FromSeconds(3)));
                Assert.That(waiting.HeartBreathing, Is.True);
                rig.Adapter.NeverRespond(FakeBleDevices.HrmId);
                rig.Wait(3);
                Assert.That(Card().Indicator, Is.EqualTo(HrmIndicator.Spinner), "intento en curso");
                Assert.That(Card().Body, Is.EqualTo(HrmCardBody.Remembered), "conectando con reintentos: igual que recordado");
                rig.Hrm.ForgetAsync();
                Assert.That(Card().Body, Is.EqualTo(HrmCardBody.NoSensor));
            }
        }
    }
}
