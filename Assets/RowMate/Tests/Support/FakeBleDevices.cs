using System.Collections.Generic;
using RowMate.Core.Ble;

namespace RowMate.Tests.Support
{
    // Un equipo ficticio del adaptador falso (10a 01-po-spec §0, K-10): dirección inventada, nombre, lo que anuncia y su GATT.
    public sealed class FakeBleDevice
    {
        public FakeBleDevice(string id, string? name, IReadOnlyList<BleUuid> advertised, IReadOnlyList<BleGattService> services, int rssi)
        {
            Id = id;
            Name = name;
            Advertised = advertised;
            Services = services;
            Rssi = rssi;
        }

        public string Id { get; }

        public string? Name { get; }

        public IReadOnlyList<BleUuid> Advertised { get; }

        public IReadOnlyList<BleGattService> Services { get; }

        public int Rssi { get; }

        public BleScanResult ScanResult => new BleScanResult(Id, Name, Name, Rssi, Advertised);
    }

    // Los equipos K-10 de la spec (§0): ninguno es real. K-ROW = AMS-670B con Rower Data y Control Point; K-ANON sin nombre; K-BIKE
    // FTMS sin Rower Data (Q-35); K-NOCP con Rower Data y sin Control Point; K-HP auriculares que no anuncian FTMS ni 0x180D.
    public static class FakeBleDevices
    {
        public const string RowId = "AA:BB:CC:00:06:70";
        public const string AnonId = "AA:BB:CC:00:00:02";
        public const string BikeId = "AA:BB:CC:00:00:03";
        public const string NoCpId = "AA:BB:CC:00:00:04";
        public const string HpId = "AA:BB:CC:00:00:05";

        // El paquete de Rower Data del oráculo ftms.json (AC-BLE-02-02): 24 SPM, 298 paladas, 482 m, 2:13, 150 W.
        public const string RowerDataHex = "2C 00 30 2A 01 E2 01 00 85 00 96 00";

        public static readonly BleUuid DeviceInformation = BleUuid.FromShort(0x180A);
        public static readonly BleUuid ManufacturerName = BleUuid.FromShort(0x2A29);
        public static readonly BleUuid IndoorBikeData = BleUuid.FromShort(0x2AD2);
        public static readonly BleUuid FitnessMachineStatus = BleUuid.FromShort(0x2ADA);
        public static readonly BleUuid AudioSink = BleUuid.FromShort(0x110B);

        public static readonly FakeBleDevice Row = new FakeBleDevice(RowId, "AMS-670B", Ftms(), RowerGatt(true, true), -61);
        public static readonly FakeBleDevice Anon = new FakeBleDevice(AnonId, null, Ftms(), RowerGatt(true, true), -70);
        public static readonly FakeBleDevice Bike = new FakeBleDevice(BikeId, "Bici FTMS", Ftms(), RowerGatt(false, true), -66);
        public static readonly FakeBleDevice NoCp = new FakeBleDevice(NoCpId, "Remo sin control", Ftms(), RowerGatt(true, false), -72);
        public static readonly FakeBleDevice Hp = new FakeBleDevice(HpId, "Auriculares", new[] { AudioSink }, new BleGattService[0], -50);

        public static readonly IReadOnlyList<FakeBleDevice> K10 = new[] { Row, Anon, Bike, NoCp, Hp };

        public static byte[] RowerData => HexBytes.Parse(RowerDataHex);

        // ── K-11 (11-ble-pulsometro/01-po-spec §0): los sensores ficticios de la It. 11, con direcciones inventadas ──
        public const string HrmId = "AA:BB:CC:00:18:0D";
        public const string WatchId = "AA:BB:CC:00:18:0E";
        public const string HrAnonId = "AA:BB:CC:00:18:0F";
        public const string NoHrId = "AA:BB:CC:00:18:10";

        // El mismo paquete del oráculo con la bandera de pulso (bit 9) y 90 bpm al final (AC-WKT-05-07, CAP-13).
        public const string RowerDataHr90Hex = "2C 02 30 2A 01 E2 01 00 85 00 96 00 5A";

        public static readonly BleUuid BodySensorLocation = BleUuid.FromShort(0x2A38);
        public static readonly BleUuid BatteryService = BleUuid.FromShort(0x180F);

        // Un servicio propio del fabricante del reloj (128 bits, sin la base del SIG).
        public static readonly BleUuid WatchVendorService = BleUuid.Parse("6e400001-b5a3-f393-e0a9-e50e24dcca9e");

        // K-HRM: anuncia 180d (16 bits) y tiene 0x180D con 0x2A37.
        public static readonly FakeBleDevice Hrm = new FakeBleDevice(HrmId, "Banda de prueba", new[] { BleUuid.FromShort(0x180D) },
            HeartRateGatt(true), -58);

        // K-WATCH: anuncia 0x180D en su forma de 128 bits junto a otro servicio; tiene 0x2A37.
        public static readonly FakeBleDevice Watch = new FakeBleDevice(WatchId, "Reloj de prueba",
            new[] { WatchVendorService, BleUuid.Parse("0000180d-0000-1000-8000-00805f9b34fb") }, HeartRateGatt(true), -64);

        // K-HRANON: sin nombre (como un reloj en Windows), anuncia 180d, tiene 0x2A37.
        public static readonly FakeBleDevice HrAnon = new FakeBleDevice(HrAnonId, null, new[] { BleUuid.FromShort(0x180D) }, HeartRateGatt(true), -70);

        // K-NOHR: anuncia 180d pero no tiene 0x2A37 (incompatible).
        public static readonly FakeBleDevice NoHr = new FakeBleDevice(NoHrId, "Sensor sin pulso", new[] { BleUuid.FromShort(0x180D) },
            HeartRateGatt(false), -66);

        public static readonly IReadOnlyList<FakeBleDevice> K11 = new[] { Row, Anon, Bike, NoCp, Hp, Hrm, Watch, HrAnon, NoHr };

        public static byte[] RowerDataHr90 => HexBytes.Parse(RowerDataHr90Hex);

        // 0x180A (información) y 0x180D con 0x2A38 (ubicación, solo lectura) y, si tiene, 0x2A37 (notificaciones).
        private static IReadOnlyList<BleGattService> HeartRateGatt(bool measurement)
        {
            var hr = new List<BleCharacteristic>();
            if (measurement)
            {
                hr.Add(new BleCharacteristic(HeartRateParser.Measurement, BleCharacteristicProperties.Notify));
            }

            hr.Add(new BleCharacteristic(BodySensorLocation, BleCharacteristicProperties.Read));
            return new[]
            {
                new BleGattService(DeviceInformation, new[] { new BleCharacteristic(ManufacturerName, BleCharacteristicProperties.Read) }),
                new BleGattService(HeartRateParser.Service, hr),
                new BleGattService(BatteryService, new BleCharacteristic[0]),
            };
        }

        private static BleUuid[] Ftms()
        {
            return new[] { FtmsUuids.Service };
        }

        // 0x180A (información, solo lectura) y 0x1826 con Rower Data (o Indoor Bike Data), el estado y, si tiene, el Control Point.
        private static IReadOnlyList<BleGattService> RowerGatt(bool rowerData, bool controlPoint)
        {
            var ftms = new List<BleCharacteristic>
            {
                new BleCharacteristic(rowerData ? FtmsUuids.RowerData : IndoorBikeData, BleCharacteristicProperties.Notify),
                new BleCharacteristic(FitnessMachineStatus, BleCharacteristicProperties.Notify),
            };
            if (controlPoint)
            {
                ftms.Add(new BleCharacteristic(FtmsUuids.ControlPoint, BleCharacteristicProperties.Write | BleCharacteristicProperties.Indicate));
            }

            return new[]
            {
                new BleGattService(DeviceInformation, new[] { new BleCharacteristic(ManufacturerName, BleCharacteristicProperties.Read) }),
                new BleGattService(FtmsUuids.Service, ftms),
            };
        }
    }
}
