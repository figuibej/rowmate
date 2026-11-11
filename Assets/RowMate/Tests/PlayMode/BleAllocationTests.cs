using System;
using System.Collections;
using NUnit.Framework;
using RowMate.App.Device;
using RowMate.Core.Ble;
using RowMate.Presentation.Device;
using RowMate.Tests.Support;
using RowMate.Tests.UiSupport;
using UnityEngine;
using UnityEngine.TestTools;

namespace RowMate.Tests.PlayMode
{
    // BAT · 0 asignaciones por cuadro con lo que se mueve del Bluetooth (10a 02-tech-design §6.4; 00-arte §8; FrameAllocations, nunca
    // GC.GetAllocatedBytesForCurrentThread): la caja del enlace en Dispositivo con el arco de espera y el spinner de "Buscar", y el aviso
    // del enlace sobre el HUD con su spinner (LinkIndicator). Mide en cuadros reales el paso del cuadro (programador, MotionDriver,
    // Apply) y TODOS los generateVisualContent del árbol (WatchTree), sin eventos del remo en la ventana (el adaptador falso no
    // manda nada mientras reconecta). El sonar y el punto que late son de WP-D: se suman acá cuando existan.
    public sealed class BleAllocationTests : WorkoutUiTestBase
    {
        private const int WindowFrames = 120;

        private FakeBleAdapter Adapter => H.FakeBle!.Adapter;

        private IEnumerator Measure(UnityEngine.UIElements.VisualElement root, string what, bool engineClock = false)
        {
            var dt = TimeSpan.FromTicks((long)Math.Round(UiTestHost.FrameSeconds * TimeSpan.TicksPerSecond));
            Action advance = () => H.Scheduler.Advance(dt);
            Action tick = H.Handle.Tick;
            // Un cuadro de calentamiento (el JIT de los caminos del dibujo).
            advance();
            tick();
            yield return null;
            using (var measured = FrameAllocations.WatchTree(root))
            {
                AllocationProfile.Begin("bat-" + AllocationProfile.Slug(what));
                for (var i = 0; i < WindowFrames; i++)
                {
                    measured.Clock(advance);
                    measured.Step(tick);
                    yield return null;
                    measured.Sample();
                }

                AllocationProfile.End();
                measured.Stop();
                Debug.Log("[BAT] " + what + ": " + measured);
                Assert.That(measured.Frames, Is.EqualTo(WindowFrames));
                Assert.That(measured.DrawAllocations, Is.EqualTo(0), what + ": los dibujos no asignan (" + measured.AllocatingElements() + ")");
                Assert.That(measured.StepAllocations, Is.EqualTo(0), what + ": el paso del cuadro no asigna: " + measured);
                if (engineClock)
                {
                    // El segundo del motor (Apply del HUD a 1 Hz, como en HudBudgetTests): a lo sumo los cuadros de esos segundos.
                    Assert.That(measured.ClockFramesWithAllocations, Is.LessThanOrEqualTo(3), what + ": el reloj asigna solo a 1 Hz: " + measured);
                }
                else
                {
                    Assert.That(measured.ClockAllocations, Is.EqualTo(0), what + ": el reloj no asigna: " + measured);
                }
            }
        }

        private IEnumerator ConnectRow()
        {
            var scan = H.App.Device.StartScanAsync();
            Adapter.Advertise(FakeBleDevices.Row);
            yield return Frames();
            _ = H.App.Device.ConnectAsync(FakeBleDevices.RowId);
            H.Advance(1);
            yield return Frames();
            Assert.That(H.App.Device.State.Value.Status, Is.EqualTo(BleStatus.Connected));
            _ = scan;
        }

        [UnityTest, Category("AC-BLE-08-05")]
        public IEnumerator TheReconnectBannerDoesNotAllocatePerFrame()
        {
            Create(new UiHostOptions { Profile = DeviceProfile.TV, Backend = HostBackends.FakeBle(a => a.NeverRespond(FakeBleDevices.RowId)) });
            yield return Settle();
            Adapter.Respond(FakeBleDevices.RowId);
            yield return ConnectRow();
            H.Advance(10);
            yield return Settle();
            Adapter.NeverRespond(FakeBleDevices.RowId);
            Adapter.DropLink(FakeBleDevices.RowId);
            // #78 (QA-V14-15): la caída agenda el intento a los 3 s. Con el tope normal de 2 s, Settle más la ventana medida
            // (1 + WindowFrames cuadros, ≈ 2,02 s) podían pasar los 3 s según el layout, y el intento asignaba dentro de la medición.
            yield return Settle(maxSeconds: 0.5);
            var banner = H.DeviceScreen!.Scan.LinkBanner;
            Assert.That(banner.Kind, Is.EqualTo(LinkBannerKind.Reconnecting));
            var due = H.App.Device.State.Value.Link.NextAttemptAt;
            Assert.That(due, Is.Not.Null, "esperando el intento");
            var window = (WindowFrames + 1) * UiTestHost.FrameSeconds;
            Assert.That(due!.Value - H.Scheduler.MonotonicSeconds, Is.GreaterThan(window + 0.1),
                "la ventana medida termina antes del intento");
            // Dentro de la espera de 3 s: el arco se llena cuadro a cuadro (sin intentos en la ventana de 2 s).
            yield return Measure(H.DeviceScreen.Scan, "caja del enlace (arco de espera)");
            Assert.That(banner.Indicator.Fraction, Is.GreaterThan(0.5), "el arco avanzó");
        }

        // WP-D (00-arte §8; A10-03, A10-13): el sonar de la búsqueda con 3 remos falsos en la lista (marcador RowMate.Device.Sonar).
        [UnityTest, Category("AC-BLE-05-04"), Category("A10-13")]
        public IEnumerator TheSonarDoesNotAllocatePerFrame()
        {
            Create(new UiHostOptions { Profile = DeviceProfile.TV, Backend = HostBackends.FakeBle() });
            yield return Settle();
            _ = H.App.Device.StartScanAsync();
            Adapter.Advertise(FakeBleDevices.Row);
            Adapter.Advertise(FakeBleDevices.Anon);
            Adapter.Advertise(FakeBleDevices.Bike);
            yield return Settle();
            var sonar = H.DeviceScreen!.Scan.Sonar;
            Assert.That(sonar.IsSearching && sonar.IsShown, Is.True, "el sonar a la vista");
            yield return Measure(H.DeviceScreen.Scan, "sonar con 3 remos en la lista");
            Assert.That(sonar.ElapsedMs, Is.GreaterThan(1000), "los anillos avanzaron");
        }

        // It. 11 (AC-HRM-04-06; 00-arte §8; marcador RowMate.Device.HrmCard): la tarjeta del pulsómetro REAL conectada y latiendo con una
        // lectura por segundo (cada lectura rearma el estado a 1 Hz, como el motor: a lo sumo los cuadros de esos segundos), y reconectando
        // con el arco de la espera de 3 s (sin intentos en la ventana).
        [UnityTest, Category("AC-HRM-04-06")]
        public IEnumerator HrmConnectedZeroBytesPerFrame()
        {
            var prefs = new RowMate.Core.Common.MemoryPreferences();
            Create(new UiHostOptions { Profile = DeviceProfile.TV }.WithRealHrm(prefs));
            yield return Settle();
            _ = H.App.HeartRate.StartScanAsync();
            Adapter.Advertise(FakeBleDevices.Hrm);
            yield return Frames();
            _ = H.App.HeartRate.ConnectAsync(FakeBleDevices.HrmId, "Banda de prueba");
            yield return Settle();
            Assert.That(H.App.HeartRate.State.Value.Status, Is.EqualTo(HrmStatus.Connected));
            using (Adapter.NotifyEvery(FakeBleDevices.HrmId, TimeSpan.FromSeconds(1), _ => HrPackets.Contact142, HeartRateParser.Measurement))
            {
                H.Advance(1.5);
                yield return Settle();
                Assert.That(H.HeartRateCard!.Model!.HeartBeating, Is.True, "late con 142");
                // Calentamiento: unos latidos cuadro a cuadro (los caminos del latido y de la lectura por primera vez).
                for (var i = 0; i < 90; i++)
                {
                    H.Step();
                    yield return null;
                }

                yield return Measure(H.DeviceScreen!, "tarjeta del pulsómetro conectada y latiendo", true);
            }

            Adapter.NeverRespond(FakeBleDevices.HrmId);
            Adapter.DropLink(FakeBleDevices.HrmId);
            H.Advance(15.2);
            yield return Settle();
            var indicator = H.HeartRateCard!.Indicator!;
            Assert.That(indicator.Current, Is.EqualTo(RowMate.Presentation.Controls.LinkIndicator.Mode.WaitingArc), "en la espera de 3 s");
            yield return Measure(H.DeviceScreen!, "tarjeta del pulsómetro reconectando (arco de espera)");
        }

        [UnityTest, Category("AC-BLE-08-06")]
        public IEnumerator TheHudLinkNoticeDoesNotAllocatePerFrame()
        {
            CreateWorkout(configure: o =>
            {
                o.Backend = HostBackends.FakeBle();
                o.Profile = DeviceProfile.W1280;
                o.IsDesktop = true;
            });
            yield return Settle();
            yield return ConnectRow();
            yield return StartFree();
            yield return AdvanceTo(12);
            Adapter.NeverRespond(FakeBleDevices.RowId);
            Adapter.DropLink(FakeBleDevices.RowId);
            yield return Settle();
            Assert.That(H.Shell.Toasts.CurrentLink?.Kind, Is.EqualTo(LinkNoticeKind.Reconnecting));
            yield return Measure(H.Shell.Toasts, "aviso del enlace sobre el HUD (spinner)", true);
        }
    }
}
