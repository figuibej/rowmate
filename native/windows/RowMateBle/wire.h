// Formato binario v1 de los eventos (10a 02-tech-design §2.6). Es el mismo que escriben Core/Ble/Native/BleWire.cs (C#) y
// Wire.kt (Android), fijado byte por byte en Assets/RowMate/Tests/Golden/ble/events-v1.bin. Little endian. Cada registro:
//   u8 kind | u8 flags (0) | u16 payloadLength | f64 monotonicSeconds | payload
//   str  = u16 n + n bytes UTF-8 (n = 0 -> null)
//   uuid = 16 bytes en el orden canónico del texto
//   1 AdapterState  i32 state
//   2 ScanResult    str id | str name | i16 rssi | u8 n | n x uuid
//   3 ScanFailed    i32 code | str message
//   4 Connection    str id | u8 connected | i32 code
//   5 RequestDone   i32 reqId | i32 status | Discover bien: u8 nSvc {uuid | u8 nChr {uuid | u8 props}} ; error: str message
//   6 Notify        str id | uuid characteristic | u16 n | n bytes
//   7 Dropped       i32 count
#pragma once

#include <array>
#include <cstddef>
#include <cstdint>
#include <cstring>
#include <string>
#include <vector>

namespace rm
{
    constexpr int32_t kApiVersion = 1;
    constexpr size_t kHeaderSize = 12;
    constexpr size_t kMaxRecordBytes = 65536;

    // Búfer de eventos: 4 MiB, como BleWire.NativeRingBytes (QA-G10a-13).
    constexpr size_t kRingBytes = 4u * 1024u * 1024u;

    constexpr uint8_t kKindAdapterState = 1;
    constexpr uint8_t kKindScanResult = 2;
    constexpr uint8_t kKindScanFailed = 3;
    constexpr uint8_t kKindConnection = 4;
    constexpr uint8_t kKindRequestDone = 5;
    constexpr uint8_t kKindNotify = 6;
    constexpr uint8_t kKindDropped = 7;

    constexpr int32_t kOk = 0;
    constexpr int32_t kErrorGeneric = -1;
    constexpr int32_t kErrorPermission = -2;
    constexpr int32_t kErrorUnsupported = -3;
    constexpr int32_t kErrorNotConnected = -4;
    constexpr int32_t kStatusCancelled = -5;

    // Orden de BleAdapterState (Core/Ble/BleAdapterState.cs).
    constexpr int32_t kStateUnknown = 0;
    constexpr int32_t kStateUnsupported = 1;
    constexpr int32_t kStateOff = 3;
    constexpr int32_t kStateOn = 5;

    // Propiedades de BleCharacteristicProperties.
    constexpr uint8_t kPropRead = 1;
    constexpr uint8_t kPropWrite = 2;
    constexpr uint8_t kPropWriteNoResponse = 4;
    constexpr uint8_t kPropNotify = 8;
    constexpr uint8_t kPropIndicate = 16;

    using Uuid = std::array<uint8_t, 16>;

    struct WireCharacteristic
    {
        Uuid uuid;
        uint8_t properties;
    };

    struct WireService
    {
        Uuid uuid;
        std::vector<WireCharacteristic> characteristics;
    };

    // Un UUID de 16 bits sobre la base de Bluetooth (0000xxxx-0000-1000-8000-00805f9b34fb).
    inline Uuid ShortUuid(uint16_t value)
    {
        Uuid u = { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x10, 0x00, 0x80, 0x00, 0x00, 0x80, 0x5f, 0x9b, 0x34, 0xfb };
        u[2] = static_cast<uint8_t>(value >> 8);
        u[3] = static_cast<uint8_t>(value & 0xFFu);
        return u;
    }

    class RecordWriter
    {
    public:
        RecordWriter(uint8_t kind, double t)
        {
            bytes_.reserve(64);
            U8(kind);
            U8(0);
            U16(0);
            uint64_t bits = 0;
            std::memcpy(&bits, &t, sizeof bits);
            for (int i = 0; i < 8; i++)
            {
                U8(static_cast<uint32_t>((bits >> (8 * i)) & 0xFFu));
            }
        }

        void U8(uint32_t value)
        {
            bytes_.push_back(static_cast<uint8_t>(value & 0xFFu));
        }

        void U16(uint32_t value)
        {
            U8(value);
            U8(value >> 8);
        }

        void I16(int32_t value)
        {
            U16(static_cast<uint32_t>(value) & 0xFFFFu);
        }

        void I32(int32_t value)
        {
            const auto u = static_cast<uint32_t>(value);
            U8(u);
            U8(u >> 8);
            U8(u >> 16);
            U8(u >> 24);
        }

        // Vacío = null (como BleWireWriter: string.IsNullOrEmpty).
        void Str(const std::string& value)
        {
            const size_t n = value.size() > 0xFFFFu ? 0xFFFFu : value.size();
            U16(static_cast<uint32_t>(n));
            bytes_.insert(bytes_.end(), value.begin(), value.begin() + static_cast<std::ptrdiff_t>(n));
        }

        void UuidBytes(const Uuid& value)
        {
            bytes_.insert(bytes_.end(), value.begin(), value.end());
        }

        void Raw(const uint8_t* data, size_t count)
        {
            if (count > 0)
            {
                bytes_.insert(bytes_.end(), data, data + count);
            }
        }

        // Cierra el registro con el largo del payload. Uno de más de 64 KiB no se emite (vacío).
        std::vector<uint8_t> Finish()
        {
            if (bytes_.size() > kMaxRecordBytes)
            {
                return {};
            }

            const size_t payload = bytes_.size() - kHeaderSize;
            bytes_[2] = static_cast<uint8_t>(payload & 0xFFu);
            bytes_[3] = static_cast<uint8_t>((payload >> 8) & 0xFFu);
            return std::move(bytes_);
        }

    private:
        std::vector<uint8_t> bytes_;
    };

    inline std::vector<uint8_t> AdapterStateRecord(double t, int32_t state)
    {
        RecordWriter w(kKindAdapterState, t);
        w.I32(state);
        return w.Finish();
    }

    inline std::vector<uint8_t> ScanResultRecord(double t, const std::string& id, const std::string& name, int32_t rssi,
                                                 const std::vector<Uuid>& services)
    {
        RecordWriter w(kKindScanResult, t);
        w.Str(id);
        w.Str(name);
        w.I16(rssi);
        const size_t n = services.size() > 255 ? 255 : services.size();
        w.U8(static_cast<uint32_t>(n));
        for (size_t i = 0; i < n; i++)
        {
            w.UuidBytes(services[i]);
        }

        return w.Finish();
    }

    inline std::vector<uint8_t> ScanFailedRecord(double t, int32_t code, const std::string& message)
    {
        RecordWriter w(kKindScanFailed, t);
        w.I32(code);
        w.Str(message);
        return w.Finish();
    }

    inline std::vector<uint8_t> ConnectionRecord(double t, const std::string& id, bool connected, int32_t code)
    {
        RecordWriter w(kKindConnection, t);
        w.Str(id);
        w.U8(connected ? 1u : 0u);
        w.I32(code);
        return w.Finish();
    }

    // status 0: sin más datos (connect, setNotify, write); si no, el mensaje.
    inline std::vector<uint8_t> RequestDoneRecord(double t, int32_t reqId, int32_t status, const std::string& message)
    {
        RecordWriter w(kKindRequestDone, t);
        w.I32(reqId);
        w.I32(status);
        if (status != 0)
        {
            w.Str(message);
        }

        return w.Finish();
    }

    inline std::vector<uint8_t> DiscoverDoneRecord(double t, int32_t reqId, const std::vector<WireService>& services)
    {
        RecordWriter w(kKindRequestDone, t);
        w.I32(reqId);
        w.I32(0);
        const size_t nSvc = services.size() > 255 ? 255 : services.size();
        w.U8(static_cast<uint32_t>(nSvc));
        for (size_t s = 0; s < nSvc; s++)
        {
            w.UuidBytes(services[s].uuid);
            const auto& chars = services[s].characteristics;
            const size_t nChr = chars.size() > 255 ? 255 : chars.size();
            w.U8(static_cast<uint32_t>(nChr));
            for (size_t c = 0; c < nChr; c++)
            {
                w.UuidBytes(chars[c].uuid);
                w.U8(chars[c].properties);
            }
        }

        return w.Finish();
    }

    inline std::vector<uint8_t> NotifyRecord(double t, const std::string& id, const Uuid& characteristic, const uint8_t* data, size_t count)
    {
        RecordWriter w(kKindNotify, t);
        w.Str(id);
        w.UuidBytes(characteristic);
        const size_t n = count > 0xFFFFu ? 0xFFFFu : count;
        w.U16(static_cast<uint32_t>(n));
        w.Raw(data, n);
        return w.Finish();
    }

    inline std::vector<uint8_t> DroppedRecord(double t, int32_t count)
    {
        RecordWriter w(kKindDropped, t);
        w.I32(count);
        return w.Finish();
    }
}
