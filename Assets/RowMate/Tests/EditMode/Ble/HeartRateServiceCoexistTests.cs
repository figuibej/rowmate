using System;
using System.Collections.Generic;
using NUnit.Framework;
using RowMate.Core.Ble;
using RowMate.Core.Models;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Ble
{
    // El remo (RowerService) y el pulsómetro (HeartRateService) reales sobre el MISMO adaptador falso K-11 (F-HRM-16;
    // 11-ble-pulsometro/02-tech-design §2.3, R-05).
    [TestFixture]
    public sealed class HeartRateServiceCoexistTests
    {
        private static IReadOnlyList<BleScanResult> Last(StateRecorder<IReadOnlyList<BleScanResult>> lists)
        {
            return lists.Count == 0 ? new BleScanResult[0] : lists.Last;
        }

        // AC-HRM-02-02: en la lista del remo nunca aparece un pulsómetro (con el filtro de la app y con el del sistema).
        [TestCase(false)]
        [TestCase(true)]
        [Category("AC-HRM-02-02")]
        public void RowerListNeverShowsHrm(bool filtersScanInOs)
        {
            using (var rig = new HrmRig(filtersScanInOs))
            using (var rowerLists = new StateRecorder<IReadOnlyList<BleScanResult>>(rig.Rower.Devices))
            {
                rig.Rower.StartScanAsync();
                rig.Adapter.Advertise(FakeBleDevices.Hrm);
                rig.Adapter.Advertise(FakeBleDevices.Row);
                rig.Adapter.Advertise(FakeBleDevices.Watch);
                var list = Last(rowerLists);
                Assert.That(list.Count, Is.EqualTo(1));
                Assert.That(list[0].DeviceId, Is.EqualTo(FakeBleDevices.RowId));
                Assert.That(rig.Lists.Count, Is.EqualTo(0), "el pulsómetro no escucha la búsqueda del remo");
            }
        }

        // AC-HRM-02-02: en la lista del pulsómetro nunca aparece el remo, ni un resultado que llegue después de otra búsqueda.
        [TestCase(false)]
        [TestCase(true)]
        [Category("AC-HRM-02-02")]
        public void HrmListNeverShowsRower(bool filtersScanInOs)
        {
            using (var rig = new HrmRig(filtersScanInOs))
            using (var rowerLists = new StateRecorder<IReadOnlyList<BleScanResult>>(rig.Rower.Devices))
            {
                rig.Scan(FakeBleDevices.Row, FakeBleDevices.Hrm);
                Assert.That(rig.Lists.Last.Count, Is.EqualTo(1));
                Assert.That(rig.Lists.Last[0].DeviceId, Is.EqualTo(FakeBleDevices.HrmId));
                rig.Hrm.StopScanAsync();
                rig.Rower.StartScanAsync();
                var count = rig.Lists.Count;
                rig.Adapter.Advertise(FakeBleDevices.Watch); // un anuncio atrasado de la búsqueda vieja (R-05)
                rig.Adapter.Advertise(FakeBleDevices.Row);
                Assert.That(rig.Lists.Count, Is.EqualTo(count), "terminada la suya, no escucha la del remo");
                Assert.That(Last(rowerLists).Count, Is.EqualTo(1));
            }
        }

        // AC-HRM-16-01: los dos conectados publican a la vez y el keepalive del remo sigue cada 2 s.
        [Test, Category("AC-HRM-16-01")]
        public void BothPublishKeepAliveEveryTwoSeconds()
        {
            using (var rig = Both())
            using (var data = new StateRecorder<RowingData>(rig.Rower.Data))
            {
                var writes = rig.Adapter.Writes.Count;
                for (var i = 0; i < 5; i++)
                {
                    rig.Adapter.Notify(FakeBleDevices.RowId, FtmsUuids.RowerData, FakeBleDevices.RowerData);
                    rig.Measure(HrPackets.Contact142);
                    rig.Wait(2);
                }

                Assert.That(data.Count, Is.EqualTo(5));
                Assert.That(rig.Bpms.Count, Is.EqualTo(6), "142 al conectar + 5");
                Assert.That(rig.Adapter.Writes.Count - writes, Is.EqualTo(5), "0x07 cada 2 s");
            }
        }

        // AC-HRM-16-01: una caída del pulsómetro no toca al remo.
        [Test, Category("AC-HRM-16-01")]
        public void HrmDropDoesNotTouchRower()
        {
            using (var rig = Both())
            {
                rig.Adapter.Fail(FakeBleDevices.HrmId, new BleException("connect", "gatt error 133", 133));
                rig.Adapter.DropLink(FakeBleDevices.HrmId);
                rig.Wait(10);
                Assert.That(rig.Rower.Status.Value, Is.EqualTo(BleStatus.Connected));
                Assert.That(rig.Adapter.Peripheral(FakeBleDevices.RowId), Is.Not.Null);
                Assert.That(rig.Rower.Link.Value.IsReconnecting, Is.False);
                Assert.That(rig.Hrm.IsRetrying, Is.True);
            }
        }

        // AC-HRM-16-01: una caída, el watchdog o Desconectar del remo no tocan al pulsómetro.
        [TestCase("drop")]
        [TestCase("watchdog")]
        [TestCase("disconnect")]
        [Category("AC-HRM-16-01")]
        public void RowerDropWatchdogDisconnectDoNotTouchHrm(string how)
        {
            using (var rig = Both())
            {
                switch (how)
                {
                    case "drop":
                        rig.Adapter.DropLink(FakeBleDevices.RowId);
                        break;
                    case "watchdog":
                        rig.Adapter.Notify(FakeBleDevices.RowId, FtmsUuids.RowerData, FakeBleDevices.RowerData);
                        for (var i = 0; i < 8; i++)
                        {
                            rig.Measure(HrPackets.Contact142);
                            rig.Wait(1);
                        }

                        Assert.That(rig.Rower.Status.Value, Is.EqualTo(BleStatus.Disconnected), "el watchdog del remo cortó");
                        break;
                    default:
                        rig.Rower.DisconnectAsync();
                        break;
                }

                Assert.That(rig.Status, Is.EqualTo(HrmStatus.Connected), how);
                Assert.That(rig.Adapter.Peripheral(FakeBleDevices.HrmId), Is.Not.Null, how);
                rig.Measure(HrPackets.U16_142);
                Assert.That(rig.Hrm.CurrentBpm, Is.EqualTo(142), how);
                Assert.That(rig.Hrm.IsRetrying, Is.False, how);
            }
        }

        // K-ROW y K-HRM conectados (el remo con su toma de control de 300 ms).
        private static HrmRig Both()
        {
            var rig = new HrmRig();
            rig.Rower.StartScanAsync();
            rig.Adapter.Advertise(FakeBleDevices.Row);
            rig.Rower.ConnectAsync(FakeBleDevices.RowId);
            rig.Wait(0.3);
            Assert.That(rig.Rower.Status.Value, Is.EqualTo(BleStatus.Connected));
            rig.Connected142();
            Assert.That(rig.Status, Is.EqualTo(HrmStatus.Connected));
            return rig;
        }
    }
}
