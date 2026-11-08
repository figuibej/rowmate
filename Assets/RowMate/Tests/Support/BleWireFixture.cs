using System.Collections.Generic;
using RowMate.Core.Ble;
using RowMate.Core.Ble.Native;

namespace RowMate.Tests.Support
{
    // La fijación del formato binario v1 (10a 02-tech-design §2.6; Tests/Golden/ble/events-v1.bin y events-v1.md): once registros,
    // uno o más de cada tipo, solo con ids de K-10 (QA-G10a-23). La escriben igual el codificador de C# (BleWireWriter), la DLL de
    // Windows (rm_ble_wire_fixture, WindowsDllContractTests) y el Kotlin de Android (golden/EmitFixture.kt, check.ps1 -Golden).
    // Si cambia, cambian los tres y events-v1.md.
    public static class BleWireFixture
    {
        public const string RelativePath = "Assets/RowMate/Tests/Golden/ble/events-v1.bin";

        public static byte[] Encode()
        {
            var w = new BleWireWriter();
            w.AdapterState(1.5, (int)BleAdapterState.On);
            w.ScanResult(2.0, FakeBleDevices.RowId, "AMS-670B", -61, new[] { FtmsUuids.Service });
            w.ScanResult(2.25, FakeBleDevices.AnonId, null, -75, new[] { FtmsUuids.Service, BleUuid.FromShort(0x180D) });
            w.ScanFailed(3.0, BleWire.ErrorPermission, "permission denied");
            w.Connection(4.0, FakeBleDevices.RowId, true, 0);
            w.RequestDone(4.5, 1, 0);
            w.DiscoverDone(5.0, 2, new[]
            {
                new BleGattService(FtmsUuids.Service, new[]
                {
                    new BleCharacteristic(FtmsUuids.RowerData, BleCharacteristicProperties.Notify),
                    new BleCharacteristic(FtmsUuids.ControlPoint, BleCharacteristicProperties.Write | BleCharacteristicProperties.Indicate),
                }),
            });
            w.RequestDone(5.5, 3, 133, "GATT error 133");
            w.Notify(6.0, FakeBleDevices.RowId, FtmsUuids.RowerData, FakeBleDevices.RowerData);
            w.Connection(7.0, FakeBleDevices.RowId, false, 8);
            w.Dropped(8.0, 3);
            return w.ToArray();
        }

        // Lo que tiene que leer BleEventReader, registro por registro (el formato de FixtureSink en BleWireTests).
        public static readonly IReadOnlyList<string> Expected = new[]
        {
            "AdapterState t=1.5 state=5",
            "ScanResult t=2 id=AA:BB:CC:00:06:70 name=AMS-670B rssi=-61 services=00001826-0000-1000-8000-00805f9b34fb",
            "ScanResult t=2.25 id=AA:BB:CC:00:00:02 name=(null) rssi=-75 services=00001826-0000-1000-8000-00805f9b34fb,0000180d-0000-1000-8000-00805f9b34fb",
            "ScanFailed t=3 code=-2 message=permission denied",
            "Connection t=4 id=AA:BB:CC:00:06:70 connected=True code=0",
            "RequestDone t=4.5 reqId=1 status=0 rest=0",
            "RequestDone t=5 reqId=2 status=0 rest=52",
            "RequestDone t=5.5 reqId=3 status=133 rest=16",
            "Notify t=6 id=AA:BB:CC:00:06:70 chr=00002ad1-0000-1000-8000-00805f9b34fb value=2c 00 30 2a 01 e2 01 00 85 00 96 00",
            "Connection t=7 id=AA:BB:CC:00:06:70 connected=False code=8",
            "Dropped t=8 count=3",
        };
    }
}
