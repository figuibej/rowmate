using System;
using NUnit.Framework;
using RowMate.Core.Ble;
using RowMate.Core.Ble.Native;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Ble
{
    // RBG: el remo en segundo plano (Q-91, ADR 017; AC-BLE-91-01; 10a 02-tech-design §2.5, §5.2, QA-G10a-04). Con el adaptador falso:
    // el keepalive pasa al plugin. Con la bomba REAL (NativeBleAdapter + FakeNativeBleApi con su propio reloj) y el ciclo de vida de
    // prueba (LifecycleHarness, el FrameScheduler de la app con el orden de AppRoot): los paquetes de la pausa se aplican con su hora
    // en el alcance, el watchdog no dispara si siguieron llegando y sí en su instante virtual si el monitor dejó de mandar ≥ 5 s.
    [TestFixture]
    public sealed class RowerBackgroundTests
    {
        private static readonly DateTimeOffset Start = new DateTimeOffset(2026, 11, 8, 13, 0, 0, TimeSpan.Zero);

        // El desfase del reloj nativo: arbitrario y no entero.
        private const double NativeOffset = 1000.25;

        [Test, Category("AC-BLE-91-01")]
        public void NativeKeepAliveThirtyInSixty()
        {
            // DT10-09: en segundo plano (Android) el plugin escribe 0x07 cada 2 s; Core no; al volver, Core retoma.
            using (var rig = new BleRig(nativeKeepAlive: true))
            {
                rig.Connected();
                rig.Wait(1);
                var core = rig.WritesOf(0x07);
                rig.Lifecycle.SetBackground(true);
                Assert.That(rig.Adapter.NativeKeepAliveStarts, Is.EqualTo(1));
                rig.Wait(60);
                Assert.That(rig.Adapter.NativeKeepAliveWrites, Is.InRange(29, 31));
                Assert.That(rig.WritesOf(0x07), Is.EqualTo(core), "Core no escribe mientras escribe el plugin");
                rig.Lifecycle.SetBackground(false);
                Assert.That(rig.Adapter.NativeKeepAliveStops, Is.EqualTo(1));
                rig.Wait(10);
                Assert.That(rig.WritesOf(0x07) - core, Is.EqualTo(5), "al volver, el periódico de Core");
                Assert.That(rig.Adapter.NativeKeepAliveWrites, Is.InRange(29, 31));
            }

            using (var rig = new BleRig(nativeKeepAlive: false))
            {
                // Windows: el loop sigue minimizado; no hay keepalive nativo.
                rig.Connected();
                rig.Lifecycle.SetBackground(true);
                rig.Wait(10);
                Assert.That(rig.Adapter.NativeKeepAliveStarts, Is.EqualTo(0));
            }
        }

        private sealed class PumpRig : IDisposable
        {
            private readonly InlineContinuations _inline = new InlineContinuations();

            public PumpRig()
            {
                Harness = new LifecycleHarness(Start);
                Api = new FakeNativeBleApi(() => Harness.RawMonotonic + NativeOffset, Harness.Scheduler, true, true);
                Adapter = new NativeBleAdapter(Api, null, Harness.Scheduler, Harness.Lifecycle, Harness.Logger);
                Harness.BeforeFrame = () => Adapter.Drain(false);
                Harness.BeforeResume = () => Adapter.Drain(true);
                Rower = new RowerService(Adapter, Harness.Scheduler, Harness.Lifecycle, Harness.Logger);
                Rower.Status.Subscribe(s =>
                {
                    if (s == BleStatus.Disconnected && !DisconnectedAt.HasValue)
                    {
                        DisconnectedAt = Harness.Scheduler.MonotonicSeconds;
                    }
                }, false);
            }

            public LifecycleHarness Harness { get; }

            public FakeNativeBleApi Api { get; }

            public NativeBleAdapter Adapter { get; }

            public RowerService Rower { get; }

            public double? DisconnectedAt { get; private set; }

            // La hora nativa del instante k de la spec.
            public static double Native(double k)
            {
                return LifecycleHarness.Instant(k) + NativeOffset;
            }

            // Busca, ve K-ROW, conecta y queda conectado antes del instante 2.
            public void Connect()
            {
                Rower.StartScanAsync();
                Harness.RunUntil(0.1);
                Api.AdvertiseNow(FakeBleDevices.Row);
                Harness.RunUntil(0.2);
                Rower.ConnectAsync(FakeBleDevices.RowId);
                Harness.RunUntil(2);
                Assert.That(Rower.Status.Value, Is.EqualTo(BleStatus.Connected), "conectado por la bomba real");
            }

            // Un paquete por segundo en primer plano, de from a to (inclusive).
            public void PacketsInForeground(int from, int to)
            {
                for (var k = from; k <= to; k++)
                {
                    Harness.RunUntil(k);
                    Api.NotifyNow(FakeBleDevices.RowId, FtmsUuids.RowerData, FakeBleDevices.RowerData);
                }
            }

            // Los paquetes que guardó el plugin mientras la app estaba atrás.
            public void PacketsInBackground(int from, int to)
            {
                for (var k = from; k <= to; k++)
                {
                    Api.NotifyAt(Native(k), FakeBleDevices.RowId, FtmsUuids.RowerData, FakeBleDevices.RowerData);
                }
            }

            public void Dispose()
            {
                Rower.Dispose();
                Adapter.Dispose();
                _inline.Dispose();
            }
        }

        [Test, Category("AC-BLE-91-01")]
        public void SixtySecondsWithPacketsNoWatchdog()
        {
            using (var rig = new PumpRig())
            {
                rig.Connect();
                rig.PacketsInForeground(3, 10);
                rig.Harness.Background(10.5);
                Assert.That(rig.Api.KeepAliveStarts, Is.EqualTo(1), "el plugin escribe el keepalive");
                rig.PacketsInBackground(11, 70);
                rig.Harness.Foreground(70.5);
                Assert.That(rig.Rower.Status.Value, Is.EqualTo(BleStatus.Connected), "los paquetes siguieron: el watchdog no dispara");
                Assert.That(rig.Api.KeepAliveStops, Is.EqualTo(1));
                Assert.That(rig.Api.NativeKeepAliveWrites, Is.InRange(29, 31), "30 escrituras ± 1 en 60 s");
                rig.PacketsInForeground(71, 80);
                Assert.That(rig.Rower.Status.Value, Is.EqualTo(BleStatus.Connected));
                Assert.That(rig.DisconnectedAt, Is.Null);
            }
        }

        [Test, Category("AC-BLE-91-01"), Category("AC-BLE-09-01")]
        public void TenSecondSilenceFiresAtItsVirtualInstant()
        {
            // El monitor deja de mandar en el instante 30 de la pausa: el watchdog dispara entre 5 y 6 s después, en tiempo virtual
            // del alcance (no todo junto al principio ni al final).
            using (var rig = new PumpRig())
            {
                rig.Connect();
                rig.PacketsInForeground(3, 10);
                rig.Harness.Background(10.5);
                rig.PacketsInBackground(11, 30);
                rig.PacketsInBackground(41, 70);
                rig.Harness.Foreground(70.5);
                Assert.That(rig.Rower.Status.Value, Is.EqualTo(BleStatus.Disconnected));
                Assert.That(rig.Rower.Link.Value.LastDisconnect, Is.EqualTo(DisconnectReason.Watchdog));
                Assert.That(rig.DisconnectedAt, Is.Not.Null);
                Assert.That(rig.DisconnectedAt!.Value, Is.InRange(LifecycleHarness.Instant(35), LifecycleHarness.Instant(36.1)));
            }
        }

        [Test, Category("AC-BLE-91-01")]
        public void ResumeDrainDoesNotRecalibrate()
        {
            // QA-G10a-04: en el drenaje de la vuelta la hora del programador sigue en el último cuadro antes de la pausa; medir ahí
            // correría todo hacia atrás lo que duró la pausa. El desfase queda congelado.
            using (var rig = new PumpRig())
            {
                rig.Connect();
                var offset = rig.Adapter.ClockOffset;
                Assert.That(offset, Is.EqualTo(-NativeOffset).Within(1.0 / 60 + 1e-6), "converge al real con el error de un cuadro");
                rig.Harness.Background(10.5);
                rig.PacketsInBackground(11, 20);
                rig.Harness.Foreground(70.5);
                Assert.That(rig.Adapter.ClockOffset, Is.EqualTo(offset), "congelado en la vuelta");
            }
        }
    }
}
