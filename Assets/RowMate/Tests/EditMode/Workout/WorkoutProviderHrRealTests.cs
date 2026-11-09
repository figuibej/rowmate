using System;
using NUnit.Framework;
using RowMate.Core.Ble;
using RowMate.Core.Common;
using RowMate.Core.Workout;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Workout
{
    // La fusión del pulso (LT-35 a LT-38, AC-WKT-05-07) con el remo y el pulsómetro REALES sobre el mismo adaptador falso K-11 (It. 11,
    // DT11-11): RowerService + HeartRateService + WorkoutEngine con el reloj de prueba. HeartRateFusion no cambia: en una caída el pulso
    // pasa a 0 y el próximo paquete FTMS repone el del monitor (LT-36); con el watchdog el sensor sigue conectado, el pulso queda en 0
    // y el FTMS no lo pisa.
    [TestFixture]
    public sealed class WorkoutProviderHrRealTests
    {
        private InlineContinuations _inline = null!;
        private RecordingLogger _logger = null!;
        private FakeScheduler _scheduler = null!;
        private FakeAppLifecycle _lifecycle = null!;
        private FakeBleAdapter _adapter = null!;
        private RowerService _rower = null!;
        private HeartRateService _hrm = null!;
        private InMemorySessionRepository _sessions = null!;
        private WorkoutEngine _engine = null!;

        [SetUp]
        public void SetUp()
        {
            _inline = new InlineContinuations();
            _logger = new RecordingLogger();
            _scheduler = new FakeScheduler();
            _lifecycle = new FakeAppLifecycle(_logger);
            _adapter = new FakeBleAdapter(_scheduler, _logger, devices: FakeBleDevices.K11);
            _rower = new RowerService(_adapter, _scheduler, _lifecycle, _logger);
            _hrm = new HeartRateService(_adapter, new MemoryPreferences(), _scheduler, _logger);
            _sessions = new InMemorySessionRepository();
            _engine = new WorkoutEngine(_rower, _hrm, _sessions, _scheduler, _lifecycle, _logger);
            _rower.Start();
            _rower.StartScanAsync();
            _adapter.Advertise(FakeBleDevices.Row);
            _rower.ConnectAsync(FakeBleDevices.RowId);
            Wait(0.3);
            Assert.That(_rower.Status.Value, Is.EqualTo(BleStatus.Connected));
        }

        [TearDown]
        public void TearDown()
        {
            _engine.Dispose();
            _hrm.Dispose();
            _rower.Dispose();
            _inline.Dispose();
        }

        private int Hr => _engine.State.Value.Data.HeartRate;

        private void Wait(double seconds)
        {
            _scheduler.Advance(TimeSpan.FromTicks((long)(seconds * TimeSpan.TicksPerSecond + 0.5)));
        }

        private void Ftms90()
        {
            _adapter.Notify(FakeBleDevices.RowId, FtmsUuids.RowerData, FakeBleDevices.RowerDataHr90);
        }

        private void Sensor(byte[] packet)
        {
            _adapter.Notify(FakeBleDevices.HrmId, HeartRateParser.Measurement, packet);
        }

        private void ConnectHrm()
        {
            _hrm.StartScanAsync();
            _adapter.Advertise(FakeBleDevices.Hrm);
            _hrm.ConnectAsync(FakeBleDevices.HrmId);
            Assert.That(_hrm.Status.Value, Is.EqualTo(HrmStatus.Connected));
        }

        // LT-35 con el servicio real: el sensor manda sobre el FTMS y se actualiza sin paquetes del remo; un 0 (sin contacto) también
        // manda; al desconectarse, 0 y el FTMS repone el suyo.
        [Test, Category("AC-WKT-05-07")]
        public void LT35_RealService()
        {
            Ftms90();
            Assert.That(Hr, Is.EqualTo(90), "sin sensor, el del monitor");
            ConnectHrm();
            Sensor(HrPackets.Contact142);
            Assert.That(Hr, Is.EqualTo(142));
            Ftms90();
            Assert.That(Hr, Is.EqualTo(142), "el paquete del remo no lo pisa");
            Sensor(HrPackets.NoContact);
            Assert.That(Hr, Is.EqualTo(0));
            Sensor(HrPackets.B72);
            _hrm.DisconnectAsync();
            Assert.That(Hr, Is.EqualTo(0));
            Ftms90();
            Assert.That(Hr, Is.EqualTo(90), "el FTMS repone su valor");
        }

        // LT-36 con el servicio real (DT11-11): caída real = bpm 0 y después desconectado; el próximo FTMS repone el del monitor.
        [Test, Category("AC-WKT-05-07")]
        public void DropZeroThenNextFtmsRestores()
        {
            Ftms90();
            ConnectHrm();
            Sensor(HrPackets.Contact142);
            _adapter.Fail(FakeBleDevices.HrmId, new BleException("connect", "gatt error 133", 133));
            _adapter.DropLink(FakeBleDevices.HrmId);
            Assert.That(Hr, Is.EqualTo(0));
            Ftms90();
            Assert.That(Hr, Is.EqualTo(90), "LT-36: el FTMS repone su valor");
            Wait(3); // un reintento fallido más no cambia nada
            Ftms90();
            Assert.That(Hr, Is.EqualTo(90));
        }

        // LT-37 con el servicio real: el sensor real solo publica después de activar sus notificaciones, ya en el mismo turno que
        // "conectado"; lo que se comprueba es la vuelta tras una caída: mientras reconecta manda el FTMS y, con la primera lectura,
        // otra vez el sensor.
        [Test, Category("AC-WKT-05-07")]
        public void LT37_RealService()
        {
            ConnectHrm();
            Sensor(HrPackets.Contact142);
            using (_adapter.NotifyEvery(FakeBleDevices.RowId, TimeSpan.FromSeconds(1), k => FakeBleDevices.RowerDataHr90))
            {
                _adapter.NeverRespond(FakeBleDevices.HrmId);
                _adapter.DropLink(FakeBleDevices.HrmId);
                Assert.That(_hrm.Status.Value, Is.EqualTo(HrmStatus.Connecting));
                Wait(1);
                Assert.That(Hr, Is.EqualTo(90), "reconectando: el del monitor");
                // El intento en vuelo vence a los 15 s de la caída; el siguiente, 3 s después, responde en 1 s.
                _adapter.RespondAfter(FakeBleDevices.HrmId, TimeSpan.FromSeconds(1));
                Wait(18.5);
                Assert.That(_hrm.Status.Value, Is.EqualTo(HrmStatus.Connected));
                Assert.That(Hr, Is.EqualTo(90), "conectado sin lectura: todavía el del monitor (LT-38)");
                Sensor(new byte[] { 0x06, 120 });
                Assert.That(Hr, Is.EqualTo(120));
                Wait(1);
                Assert.That(Hr, Is.EqualTo(120), "el sensor sigue mandando");
            }
        }

        // LT-38 con el servicio real: conectar y desconectar el sensor sin ninguna lectura no toca los datos.
        [Test, Category("AC-WKT-05-07")]
        public void LT38_RealService()
        {
            using (var notifications = new StateRecorder<WorkoutSnapshot>(_engine.State))
            {
                ConnectHrm();
                _hrm.DisconnectAsync();
                Assert.That(notifications.Count, Is.EqualTo(0));
            }
        }

        // AC-WKT-05-07: el monitor manda 90 y el sensor 142: la tarjeta BPM y los puntos guardados tienen 142.
        [Test, Category("AC-WKT-05-07")]
        public void SensorWinsOverFtms90AndPointsSave142()
        {
            ConnectHrm();
            using (_adapter.NotifyEvery(FakeBleDevices.RowId, TimeSpan.FromSeconds(1), k => FakeBleDevices.RowerDataHr90))
            using (_adapter.NotifyEvery(FakeBleDevices.HrmId, TimeSpan.FromSeconds(1), k => HrPackets.Contact142, HeartRateParser.Measurement))
            {
                Wait(1);
                Assert.That(Hr, Is.EqualTo(142));
                _engine.StartFree();
                Wait(10.5);
                Assert.That(Hr, Is.EqualTo(142));
                var points = _sessions.AllPoints;
                Assert.That(points.Count, Is.EqualTo(2));
                Assert.That(points[0].HeartRate, Is.EqualTo(142));
                Assert.That(points[1].HeartRate, Is.EqualTo(142));
            }
        }

        // AC-WKT-05-07 (DT11-11): con el watchdog disparado el sensor sigue conectado: el pulso queda en 0 y el FTMS no lo pisa.
        [Test, Category("AC-WKT-05-07")]
        public void StaleZeroFtmsDoesNotOverride()
        {
            ConnectHrm();
            Sensor(HrPackets.Contact142);
            using (_adapter.NotifyEvery(FakeBleDevices.RowId, TimeSpan.FromSeconds(1), k => FakeBleDevices.RowerDataHr90))
            {
                _engine.StartFree();
                Wait(10);
                Assert.That(_hrm.Link.Value.IsStale, Is.True);
                Assert.That(_hrm.Status.Value, Is.EqualTo(HrmStatus.Connected));
                Assert.That(Hr, Is.EqualTo(0), "el FTMS no lo pisa");
                Wait(5);
                Assert.That(Hr, Is.EqualTo(0));
                var points = _sessions.AllPoints;
                Assert.That(points[points.Count - 1].HeartRate, Is.EqualTo(0), "los puntos guardan 0");
            }
        }

        // AC-WKT-05-07: si el monitor no manda pulso (como el AMS-670B), en una caída queda en 0 y los puntos guardan 0.
        [Test, Category("AC-WKT-05-07")]
        public void DropWithoutFtmsHrStaysZero()
        {
            ConnectHrm();
            Sensor(HrPackets.Contact142);
            using (_adapter.NotifyEvery(FakeBleDevices.RowId, TimeSpan.FromSeconds(1), k => FakeBleDevices.RowerData))
            using (_adapter.NotifyEvery(FakeBleDevices.HrmId, TimeSpan.FromSeconds(1), k => HrPackets.Contact142, HeartRateParser.Measurement))
            {
                _engine.StartFree();
                Wait(5);
                Assert.That(_sessions.AllPoints[0].HeartRate, Is.EqualTo(142));
                _adapter.Fail(FakeBleDevices.HrmId, new BleException("connect", "gatt error 133", 133));
                _adapter.DropLink(FakeBleDevices.HrmId);
                Wait(5);
                Assert.That(Hr, Is.EqualTo(0));
                Assert.That(_sessions.AllPoints[1].HeartRate, Is.EqualTo(0));
            }
        }
    }
}
