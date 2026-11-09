using NUnit.Framework;
using RowMate.Core.Ble;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Ble
{
    // HeartRateService: el watchdog de lectura vencida (hrs:37-38, :86-89, :274, :321-326; DT11-02) con el reloj de prueba.
    [TestFixture]
    public sealed class HeartRateServiceWatchdogTests
    {
        // AC-HRM-05-02: 10 s sin lectura válida → pulso 0 UNA vez; la conexión no se corta y no arranca ningún reintento.
        [Test, Category("AC-HRM-05-02")]
        public void StaleEmitsZeroOnceKeepsLink()
        {
            using (var rig = new HrmRig())
            {
                rig.Connected142();
                rig.At(9.9);
                Assert.That(rig.Link.IsStale, Is.False);
                rig.At(10);
                Assert.That(rig.Bpms.Values, Is.EqualTo(new[] { 142, 0 }));
                Assert.That(rig.Link.IsStale, Is.True);
                Assert.That(rig.Status, Is.EqualTo(HrmStatus.Connected), "no se corta");
                Assert.That(rig.Hrm.IsRetrying, Is.False);
                Assert.That(rig.Adapter.LiveSessions(FakeBleDevices.HrmId), Is.EqualTo(1));
                rig.At(60);
                Assert.That(rig.Bpms.Values, Is.EqualTo(new[] { 142, 0 }), "una sola vez");
                Assert.That(rig.Adapter.ConnectCalls(FakeBleDevices.HrmId), Is.EqualTo(1));
            }
        }

        // AC-HRM-05-02: solo lecturas inválidas tampoco rearman el plazo.
        [Test, Category("AC-HRM-05-02")]
        public void InvalidDoesNotRearm()
        {
            using (var rig = new HrmRig())
            {
                rig.Connected142();
                rig.At(5);
                rig.Measure(HrPackets.Invalid);
                rig.At(9);
                rig.Measure(HrPackets.Invalid);
                rig.At(10);
                Assert.That(rig.Link.IsStale, Is.True);
                Assert.That(rig.Hrm.CurrentBpm, Is.EqualTo(0));
            }
        }

        // AC-HRM-05-02: la próxima lectura válida devuelve el pulso, limpia la lectura vencida y rearma el plazo.
        [Test, Category("AC-HRM-05-02")]
        public void ValidReadingRearmsAndClearsStale()
        {
            using (var rig = new HrmRig())
            {
                rig.Connected142();
                rig.At(15);
                Assert.That(rig.Link.IsStale, Is.True);
                rig.Measure(HrPackets.Contact142);
                Assert.That(rig.Hrm.CurrentBpm, Is.EqualTo(142));
                Assert.That(rig.Link.IsStale, Is.False);
                rig.At(24.9);
                Assert.That(rig.Link.IsStale, Is.False);
                rig.At(25);
                Assert.That(rig.Link.IsStale, Is.True, "rearmado: otra vez a los 10 s");
                Assert.That(rig.Bpms.Values, Is.EqualTo(new[] { 142, 0, 142, 0 }));
            }
        }

        // AC-HRM-05-02: se detiene al desconectar, al caerse, al abortar un intento y al cerrar (no queda temporizador vivo).
        [TestCase("disconnect")]
        [TestCase("drop")]
        [TestCase("abort")]
        [TestCase("dispose")]
        [Category("AC-HRM-05-02")]
        public void StoppedOnDisconnectDropAbortDispose(string how)
        {
            using (var rig = new HrmRig())
            {
                switch (how)
                {
                    case "disconnect":
                        rig.Connected142();
                        rig.Hrm.DisconnectAsync();
                        break;
                    case "drop":
                        rig.Connected142();
                        rig.Adapter.Fail(FakeBleDevices.HrmId, new BleException("connect", "gatt error 133", 133));
                        rig.Adapter.DropLink(FakeBleDevices.HrmId);
                        rig.Hrm.ForgetAsync(); // corta también la cadencia de caída
                        break;
                    case "abort":
                        var release = rig.Adapter.HoldNextDiscover(FakeBleDevices.HrmId);
                        rig.Connect();
                        rig.Hrm.StartScanAsync();
                        rig.Hrm.StopScanAsync();
                        release();
                        break;
                    default:
                        rig.Connected142();
                        rig.Hrm.Dispose();
                        break;
                }

                Assert.That(rig.Scheduler.PendingTimers, Is.EqualTo(0), how);
                var bpms = rig.Bpms.Count;
                rig.Wait(30);
                Assert.That(rig.Bpms.Count, Is.EqualTo(bpms), "el watchdog no dispara: " + how);
                Assert.That(rig.Link.IsStale, Is.False, how);
            }
        }
    }
}
