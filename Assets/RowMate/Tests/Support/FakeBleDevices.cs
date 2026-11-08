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
