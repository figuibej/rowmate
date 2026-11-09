using System;
using System.Collections;
using NUnit.Framework;
using RowMate.App.Device;
using RowMate.Core.Ble;
using RowMate.Core.Common;
using RowMate.Tests.Support;
using RowMate.Tests.UiSupport;
using UnityEngine.TestTools;

namespace RowMate.Tests.PlayMode
{
    // HTN · Los avisos breves del pulsómetro (11-ble-pulsometro/02-tech-design §4.3; DT11-12, DT11-13, QA-G11-03; AC-HRM-07-05,
    // AC-HRM-08-03): HeartRateStore.Notices → HrmNoticePresenter → ToastLayer.ShowHrm, con el remo y el pulsómetro REALES sobre el
    // adaptador falso K-11. El aviso es el #toast del ToastLayer (fuera de #immersive: HudBudgetTests no cambia).
    public sealed class HrmToastTests : WorkoutUiTestBase
    {
        private const string Connected = "Pulsómetro conectado";
        private const string Lost = "Se perdió el pulsómetro, reconectando…";

        private MemoryPreferences _prefs = new MemoryPreferences();
        private IDisposable? _rowerPackets;
        private IDisposable? _hrmPackets;

        private FakeBleAdapter Adapter => H.FakeBle!.Adapter;

        private HrmToast? Hrm => H.Shell.Toasts.CurrentHrm;

        [TearDown]
        public void StopPackets()
        {
            _rowerPackets?.Dispose();
            _rowerPackets = null;
            _hrmPackets?.Dispose();
            _hrmPackets = null;
        }

        private void Options(UiHostOptions o, DeviceProfile profile, bool remembered, Action<FakeBleAdapter>? setup)
        {
            _prefs = new MemoryPreferences();
            if (remembered)
            {
                _prefs.SetString(HeartRateService.PrefDeviceId, FakeBleDevices.HrmId);
                _prefs.SetString(HeartRateService.PrefDeviceName, "Banda de prueba");
            }

            o.Profile = profile;
            o.IsDesktop = profile.Code == DeviceProfile.W1280.Code;
            o.WithRealHrm(_prefs, a =>
            {
                a.Advertise(FakeBleDevices.Hrm);
                setup?.Invoke(a);
            });
        }

        private IEnumerator AdvanceAbs(double target)
        {
            var now = H.Scheduler.MonotonicSeconds;
            if (target > now + 1e-9)
            {
                H.Advance(target - now);
            }

            yield return Frames();
        }

        // El remo conectado (AMS-670B sin pulso FTMS, un paquete por segundo) y el pulsómetro con 142; después, libre.
        private IEnumerator RowingFreeWithHrm(DeviceProfile profile)
        {
            CreateWorkout(configure: o => Options(o, profile, false, null));
            yield return Settle();
            _ = H.App.Device.StartScanAsync();
            Adapter.Advertise(FakeBleDevices.Row);
            yield return Frames();
            _ = H.App.Device.ConnectAsync(FakeBleDevices.RowId);
            H.Advance(1);
            yield return Frames();
            Assert.That(H.App.Device.State.Value.Status, Is.EqualTo(BleStatus.Connected));
            _ = H.App.HeartRate.StartScanAsync();
            Adapter.Advertise(FakeBleDevices.Hrm);
            yield return Frames();
            _ = H.App.HeartRate.ConnectAsync(FakeBleDevices.HrmId, "Banda de prueba");
            yield return Settle();
            Assert.That(H.App.HeartRate.State.Value.Status, Is.EqualTo(HrmStatus.Connected));
            Assert.That(Hrm, Is.Null, "la conexión manual no avisa");
            _rowerPackets = Adapter.NotifyEvery(FakeBleDevices.RowId, TimeSpan.FromSeconds(1), _ => FakeBleDevices.RowerData);
            _hrmPackets = Adapter.NotifyEvery(FakeBleDevices.HrmId, TimeSpan.FromSeconds(1), _ => HrPackets.Contact142, HeartRateParser.Measurement);
            yield return StartFree();
            yield return AdvanceTo(5);
            Assert.That(H.Immersive, Is.Not.Null);
        }

        [UnityTest, Category("AC-HRM-07-05")]
        public IEnumerator ConnectedTwoSecondsAnyTab()
        {
            CreateWorkout(configure: o => Options(o, DeviceProfile.TV, true, a => a.RespondAfter(FakeBleDevices.HrmId, TimeSpan.FromSeconds(2))));
            // El primer intento salió al abrir (AppContext.Start, sin avance del reloj): conecta a los 2 s de este instante.
            var opened = H.Scheduler.MonotonicSeconds;
            yield return Frames();
            Assert.That(Hrm, Is.Null);
            yield return AdvanceAbs(opened + 2.05);
            Assert.That(H.App.HeartRate.State.Value.Status, Is.EqualTo(HrmStatus.Connected));
            Assert.That(Hrm?.Kind, Is.EqualTo(HrmNoticeKind.Connected), "en Dispositivo también (CAP-12)");
            Assert.That(H.Shell.Toasts.CurrentText, Is.EqualTo(Connected));
            Assert.That(H.Shell.Toasts.CurrentLink, Is.Null, "no ocupa el lugar del remo");
            yield return AdvanceAbs(opened + 3.9);
            Assert.That(Hrm, Is.Not.Null, "todavía dentro de sus 2 s");
            yield return AdvanceAbs(opened + 4.2);
            Assert.That(Hrm, Is.Null, "se va a los 2 s");

            // En otra pestaña, una caída fuera del entrenamiento no avisa; la reconexión sí (Connected en cualquier pestaña).
            yield return Tap("tab-history");
            Adapter.NeverRespond(FakeBleDevices.HrmId);
            Adapter.DropLink(FakeBleDevices.HrmId);
            yield return Settle();
            Assert.That(Hrm, Is.Null, "Lost solo durante el entrenamiento");
            Adapter.Respond(FakeBleDevices.HrmId);
            // El intento pendiente vence a los 15 s (su continuación corre en el cuadro siguiente) y el próximo sale 3 s después.
            yield return Advance(15.1);
            yield return Advance(3.1);
            yield return Frames();
            Assert.That(H.App.HeartRate.State.Value.Status, Is.EqualTo(HrmStatus.Connected));
            Assert.That(H.Shell.Toasts.CurrentText, Is.EqualTo(Connected), "Connected en cualquier pestaña");
        }

        [UnityTest, Category("AC-HRM-08-03")]
        public IEnumerator LostFourSecondsOverHud()
        {
            yield return RowingFreeWithHrm(DeviceProfile.W1280);
            Assert.That(H.IsShown("hud-bpm"), Is.True, "el pulso del sensor en el HUD");
            Adapter.NeverRespond(FakeBleDevices.HrmId);
            Adapter.DropLink(FakeBleDevices.HrmId);
            yield return Settle();
            Assert.That(Hrm?.Kind, Is.EqualTo(HrmNoticeKind.Lost));
            Assert.That(H.Shell.Toasts.CurrentText, Is.EqualTo(Lost));
            Assert.That(H.Shell.Toasts.CurrentLink, Is.Null);
            Assert.That(H.Shell.Toasts.StickyLink, Is.Null);
            yield return AdvanceTo(7);
            Assert.That(H.IsShown("hud-bpm"), Is.False, "el monitor no manda pulso: la tarjeta BPM desaparece");
            Assert.That(H.App.Workout.State.Value.Phase, Is.EqualTo(RowMate.Core.Workout.WorkoutPhase.Active), "la sesión sigue");
            Assert.That(Hrm, Is.Not.Null, "dentro de sus 4 s");
            yield return AdvanceTo(9.2);
            Assert.That(Hrm, Is.Null, "se va a los 4 s");
        }

        [UnityTest, Category("AC-HRM-08-03")]
        public IEnumerator ConnectedReplacesLost()
        {
            yield return RowingFreeWithHrm(DeviceProfile.W1280);
            Adapter.RespondAfter(FakeBleDevices.HrmId, TimeSpan.FromSeconds(1));
            Adapter.DropLink(FakeBleDevices.HrmId);
            yield return Frames();
            Assert.That(H.Shell.Toasts.CurrentText, Is.EqualTo(Lost));
            yield return AdvanceTo(6.05);
            Assert.That(H.App.HeartRate.State.Value.Status, Is.EqualTo(HrmStatus.Connected));
            Assert.That(Hrm?.Kind, Is.EqualTo(HrmNoticeKind.Connected), "Connected pisa a Lost");
            Assert.That(H.Shell.Toasts.CurrentText, Is.EqualTo(Connected));
            yield return AdvanceTo(7.9);
            Assert.That(Hrm?.Kind, Is.EqualTo(HrmNoticeKind.Connected));
            yield return AdvanceTo(8.2);
            Assert.That(Hrm, Is.Null, "se va a los 2 s del reemplazo, no a los 4 s del Lost");
        }

        [UnityTest, Category("AC-HRM-07-05")]
        public IEnumerator DiscardedWhenRowerNotice()
        {
            yield return RowingFreeWithHrm(DeviceProfile.W1280);
            // El remo se cae: "Reconectando con AMS-670B…" fijo.
            Adapter.NeverRespond(FakeBleDevices.RowId);
            Adapter.DropLink(FakeBleDevices.RowId);
            yield return Settle();
            Assert.That(H.Shell.Toasts.CurrentLink?.Kind, Is.EqualTo(LinkNoticeKind.Reconnecting));
            var discarded = H.Shell.HrmNotices.Discarded;
            Adapter.RespondAfter(FakeBleDevices.HrmId, TimeSpan.FromSeconds(1));
            Adapter.DropLink(FakeBleDevices.HrmId);
            yield return Advance(1.05);
            Assert.That(H.App.HeartRate.State.Value.Status, Is.EqualTo(HrmStatus.Connected));
            Assert.That(H.Shell.Toasts.CurrentLink?.Kind, Is.EqualTo(LinkNoticeKind.Reconnecting), "el aviso del remo manda");
            Assert.That(Hrm, Is.Null);
            Assert.That(H.Shell.HrmNotices.Discarded, Is.EqualTo(discarded + 2), "Lost y Connected descartados");
            Assert.That(H.Logger.Contains("aviso descartado"), Is.True);
        }

        [UnityTest, Category("AC-HRM-08-03")]
        public IEnumerator HrmDoesNotCountAsCurrentLink()
        {
            CreateWorkout(configure: o => Options(o, DeviceProfile.TV, false, null));
            yield return Settle();
            var toasts = H.Shell.Toasts;
            Assert.That(toasts.ShowHrm(new HrmToast(HrmNoticeKind.Lost, Lost, TimeSpan.FromSeconds(4))), Is.True);
            yield return Frames();
            Assert.That(toasts.CurrentHrm, Is.Not.Null);
            Assert.That(toasts.CurrentLink, Is.Null);
            Assert.That(toasts.StickyLink, Is.Null);
            Assert.That(toasts.VisibleSlot, Is.EqualTo(ToastSlot.Hrm));
            Assert.That(toasts.ShowHrm(new HrmToast(HrmNoticeKind.Connected, Connected, TimeSpan.FromSeconds(2))), Is.True, "reemplaza al suyo");
            Assert.That(toasts.CurrentText, Is.EqualTo(Connected));
            // Un aviso del remo lo reemplaza; después, el del pulsómetro cede.
            toasts.ShowLink(new LinkToast(LinkNoticeKind.Reconnecting, "Reconectando con AMS-670B…", null));
            Assert.That(toasts.CurrentHrm, Is.Null);
            Assert.That(toasts.VisibleSlot, Is.EqualTo(ToastSlot.Rower));
            Assert.That(toasts.ShowHrm(new HrmToast(HrmNoticeKind.Connected, Connected, TimeSpan.FromSeconds(2))), Is.False);
            toasts.Show("Un aviso común");
            Assert.That(toasts.VisibleSlot, Is.EqualTo(ToastSlot.Common));
            Assert.That(toasts.ShowHrm(new HrmToast(HrmNoticeKind.Connected, Connected, TimeSpan.FromSeconds(2))), Is.False,
                "con el fijo del remo pendiente, cede aunque se vea uno común");
            toasts.ShowLink(null);
            Assert.That(toasts.ShowHrm(new HrmToast(HrmNoticeKind.Connected, Connected, TimeSpan.FromSeconds(2))), Is.True, "reemplaza uno común");
        }
    }
}
