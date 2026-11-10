using System;
using NUnit.Framework;
using RowMate.App.Composition;
using RowMate.App.Device;
using RowMate.Core.Ble;
using RowMate.Core.Ports;
using RowMate.Tests.EditMode.Ble;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Device
{
    // RH · "Reconectado con {name}" vibra como "Listo" (It. 14, #51, AC-DEBT-10), una vez por reconexión; la política compartida
    // (HapticGate) la deja pasar o no según los 250 ms y el segundo plano.
    [TestFixture]
    public sealed class DeviceStoreReconnectHapticTests
    {
        [Test, Category("AC-DEBT-10")]
        public void OneConfirmPerReconnection()
        {
            using (var rig = new BleRig())
            {
                var haptics = new FakeHaptics();
                var store = rig.Store(haptics: haptics);
                var notices = new StateRecorder<LinkNotice>(store.LinkNotices);
                rig.Connected();
                Assert.That(haptics.Confirms, Is.EqualTo(1), "Listo");

                rig.Adapter.DropLink(FakeBleDevices.RowId);
                rig.Wait(3.3);
                Assert.That(rig.Status, Is.EqualTo(BleStatus.Connected));
                Assert.That(notices.Last.Kind, Is.EqualTo(LinkNoticeKind.Reconnected));
                Assert.That(haptics.Confirms, Is.EqualTo(2), "una por la reconexión");
                rig.Wait(5);
                Assert.That(haptics.Confirms, Is.EqualTo(2), "nada más mientras sigue conectado");

                rig.Adapter.DropLink(FakeBleDevices.RowId);
                rig.Wait(3.3);
                Assert.That(haptics.Confirms, Is.EqualTo(3), "otra reconexión, otra vibración");
                Assert.That(haptics.Kinds, Is.All.EqualTo(HapticKind.Confirm));
                store.Dispose();
            }
        }

        [Test, Category("AC-DEBT-10")]
        public void TheGateAppliesThePolicy()
        {
            using (var rig = new BleRig())
            {
                var platform = new FakeHaptics();
                var gate = new HapticGate(platform, rig.Scheduler, rig.Lifecycle);
                gate.Play(HapticKind.Medium);
                gate.Confirm();
                Assert.That(platform.Kinds, Is.EqualTo(new[] { HapticKind.Medium }), "un Confirm a menos de 250 ms se pierde (QA-G14-23)");
                rig.Wait(0.3);
                gate.Confirm();
                Assert.That(platform.Confirms, Is.EqualTo(1));
                rig.Lifecycle.SetBackground(true);
                rig.Wait(1);
                gate.Play(HapticKind.Success);
                Assert.That(platform.Kinds.Count, Is.EqualTo(2), "en segundo plano, nada");
                Assert.Throws<ArgumentNullException>(() => new HapticGate(null!, rig.Scheduler, rig.Lifecycle));
            }
        }
    }
}
