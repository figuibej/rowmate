// Las funciones exportadas de RowMateBle.dll (rm_ble.h) y las ayudas comunes: reloj, búfer, ids, UUID y el hilo MTA. Nada de
// macros de archivo, aserciones ni ubicaciones de fuente (QA-G10a-01: native/windows/build.ps1 lo revisa en las fuentes y en el
// binario).
#define RM_BLE_EXPORTS
#include "rm_ble.h"

#include "internal.h"

#include <cctype>

namespace rm
{
    namespace
    {
        std::mutex g_init;
        bool g_initialized = false;
        std::atomic<int32_t> g_nextRequest{ 1 };

        int64_t Frequency()
        {
            static const int64_t frequency = []() {
                LARGE_INTEGER value;
                QueryPerformanceFrequency(&value);
                return static_cast<int64_t>(value.QuadPart);
            }();
            return frequency;
        }

        int HexValue(char c)
        {
            if (c >= '0' && c <= '9')
            {
                return c - '0';
            }

            if (c >= 'a' && c <= 'f')
            {
                return c - 'a' + 10;
            }

            if (c >= 'A' && c <= 'F')
            {
                return c - 'A' + 10;
            }

            return -1;
        }

        std::string Text(const char* value)
        {
            return value == nullptr ? std::string() : std::string(value);
        }
    }

    double NowSeconds()
    {
        LARGE_INTEGER counter;
        QueryPerformanceCounter(&counter);
        const int64_t f = Frequency();
        const int64_t c = static_cast<int64_t>(counter.QuadPart);
        return static_cast<double>(c / f) + static_cast<double>(c % f) / static_cast<double>(f);
    }

    EventRing& Ring()
    {
        // Montón sin destructor al descargar la DLL.
        static EventRing* ring = new EventRing();
        return *ring;
    }

    int32_t NextRequestId()
    {
        int32_t id = g_nextRequest.fetch_add(1);
        if (id <= 0)
        {
            g_nextRequest.store(2);
            id = 1;
        }

        return id;
    }

    bool ParseAddress(const std::string& text, uint64_t& address)
    {
        uint64_t value = 0;
        int digits = 0;
        for (const char c : text)
        {
            if (c == ':' || c == '-')
            {
                continue;
            }

            const int h = HexValue(c);
            if (h < 0)
            {
                return false;
            }

            value = (value << 4) | static_cast<uint64_t>(h);
            digits++;
        }

        if (digits != 12)
        {
            return false;
        }

        address = value;
        return true;
    }

    std::string FormatAddress(uint64_t address)
    {
        static const char hex[] = "0123456789ABCDEF";
        std::string text;
        text.reserve(17);
        for (int i = 5; i >= 0; i--)
        {
            const auto b = static_cast<unsigned>((address >> (8 * i)) & 0xFFu);
            text.push_back(hex[b >> 4]);
            text.push_back(hex[b & 0xFu]);
            if (i > 0)
            {
                text.push_back(':');
            }
        }

        return text;
    }

    bool ParseUuid(const char* text, Uuid& uuid)
    {
        if (text == nullptr)
        {
            return false;
        }

        int nibble = 0;
        for (const char* p = text; *p != '\0'; p++)
        {
            if (*p == '-' || *p == '{' || *p == '}')
            {
                continue;
            }

            const int h = HexValue(*p);
            if (h < 0 || nibble >= 32)
            {
                return false;
            }

            if ((nibble & 1) == 0)
            {
                uuid[static_cast<size_t>(nibble / 2)] = static_cast<uint8_t>(h << 4);
            }
            else
            {
                uuid[static_cast<size_t>(nibble / 2)] |= static_cast<uint8_t>(h);
            }

            nibble++;
        }

        return nibble == 32;
    }

    Uuid UuidFromGuid(const winrt::guid& value)
    {
        Uuid u{};
        u[0] = static_cast<uint8_t>(value.Data1 >> 24);
        u[1] = static_cast<uint8_t>(value.Data1 >> 16);
        u[2] = static_cast<uint8_t>(value.Data1 >> 8);
        u[3] = static_cast<uint8_t>(value.Data1);
        u[4] = static_cast<uint8_t>(value.Data2 >> 8);
        u[5] = static_cast<uint8_t>(value.Data2);
        u[6] = static_cast<uint8_t>(value.Data3 >> 8);
        u[7] = static_cast<uint8_t>(value.Data3);
        for (size_t i = 0; i < 8; i++)
        {
            u[8 + i] = value.Data4[i];
        }

        return u;
    }

    winrt::guid GuidFromUuid(const Uuid& u)
    {
        winrt::guid value{};
        value.Data1 = (static_cast<uint32_t>(u[0]) << 24) | (static_cast<uint32_t>(u[1]) << 16) | (static_cast<uint32_t>(u[2]) << 8) | u[3];
        value.Data2 = static_cast<uint16_t>((u[4] << 8) | u[5]);
        value.Data3 = static_cast<uint16_t>((u[6] << 8) | u[7]);
        for (size_t i = 0; i < 8; i++)
        {
            value.Data4[i] = u[8 + i];
        }

        return value;
    }

    std::string ErrorText(const winrt::hresult_error& error)
    {
        std::string text;
        try
        {
            text = winrt::to_string(error.message());
        }
        catch (...)
        {
        }

        std::string clean;
        clean.reserve(text.size());
        for (const char c : text)
        {
            if (c == '\r' || c == '\n')
            {
                if (!clean.empty() && clean.back() != ' ')
                {
                    clean.push_back(' ');
                }

                continue;
            }

            clean.push_back(c);
        }

        while (!clean.empty() && clean.back() == ' ')
        {
            clean.pop_back();
        }

        if (clean.empty())
        {
            char buffer[32];
            snprintf(buffer, sizeof buffer, "hresult 0x%08X", static_cast<unsigned>(error.code()));
            clean = buffer;
        }

        return clean;
    }

    // ── MtaWorker ──

    MtaWorker::~MtaWorker()
    {
        RequestStop();
        if (thread_.joinable())
        {
            if (thread_.get_id() == std::this_thread::get_id())
            {
                thread_.detach();
            }
            else
            {
                thread_.join();
            }
        }
    }

    void MtaWorker::Start()
    {
        thread_ = std::thread([this]() { Run(); });
    }

    bool MtaWorker::Post(std::function<void()> task)
    {
        {
            std::lock_guard<std::mutex> lock(mutex_);
            if (stopping_)
            {
                return false;
            }

            tasks_.push_back(std::move(task));
        }

        wake_.notify_one();
        return true;
    }

    void MtaWorker::RequestStop()
    {
        {
            std::lock_guard<std::mutex> lock(mutex_);
            stopping_ = true;
        }

        wake_.notify_all();
    }

    bool MtaWorker::WaitFinished(std::chrono::milliseconds timeout)
    {
        std::unique_lock<std::mutex> lock(mutex_);
        return done_.wait_for(lock, timeout, [this]() { return finished_; });
    }

    bool MtaWorker::Finished()
    {
        std::lock_guard<std::mutex> lock(mutex_);
        return finished_;
    }

    void MtaWorker::Join()
    {
        if (thread_.joinable() && thread_.get_id() != std::this_thread::get_id())
        {
            thread_.join();
        }
    }

    void MtaWorker::Run()
    {
        bool apartment = false;
        try
        {
            winrt::init_apartment(winrt::apartment_type::multi_threaded);
            apartment = true;
        }
        catch (...)
        {
        }

        for (;;)
        {
            std::function<void()> task;
            {
                std::unique_lock<std::mutex> lock(mutex_);
                wake_.wait(lock, [this]() { return stopping_ || !tasks_.empty(); });
                if (tasks_.empty())
                {
                    break;
                }

                task = std::move(tasks_.front());
                tasks_.pop_front();
            }

            try
            {
                task();
            }
            catch (...)
            {
            }
        }

        if (apartment)
        {
            winrt::uninit_apartment();
        }

        {
            std::lock_guard<std::mutex> lock(mutex_);
            finished_ = true;
        }

        done_.notify_all();
    }
}

// ── API C ──

RM_BLE_API int32_t __cdecl rm_ble_init(void)
{
    try
    {
        std::lock_guard<std::mutex> lock(rm::g_init);
        if (rm::g_initialized)
        {
            return rm::kOk;
        }

        rm::Ring().Clear();
        if (!rm::AdapterStart())
        {
            return rm::kErrorGeneric;
        }

        rm::g_initialized = true;
        return rm::kOk;
    }
    catch (...)
    {
        return rm::kErrorGeneric;
    }
}

RM_BLE_API void __cdecl rm_ble_shutdown(void)
{
    try
    {
        std::lock_guard<std::mutex> lock(rm::g_init);
        if (!rm::g_initialized)
        {
            return;
        }

        rm::AdapterStopScan();
        rm::DevicesShutdown();
        rm::AdapterStop();
        rm::Ring().Clear();
        rm::g_initialized = false;
    }
    catch (...)
    {
    }
}

RM_BLE_API int32_t __cdecl rm_ble_api_version(void)
{
    return rm::kApiVersion;
}

RM_BLE_API double __cdecl rm_clock_monotonic(void)
{
    return rm::NowSeconds();
}

RM_BLE_API int32_t __cdecl rm_ble_adapter_state(void)
{
    return rm::AdapterState();
}

RM_BLE_API int32_t __cdecl rm_ble_start_scan(const char* servicesCsv)
{
    (void)servicesCsv;
    try
    {
        return rm::AdapterStartScan();
    }
    catch (...)
    {
        return rm::kErrorGeneric;
    }
}

RM_BLE_API int32_t __cdecl rm_ble_stop_scan(void)
{
    try
    {
        return rm::AdapterStopScan();
    }
    catch (...)
    {
        return rm::kErrorGeneric;
    }
}

RM_BLE_API int32_t __cdecl rm_ble_connect(const char* id, int32_t highMtu)
{
    (void)highMtu;
    try
    {
        {
            std::lock_guard<std::mutex> lock(rm::g_init);
            if (!rm::g_initialized)
            {
                return rm::kErrorUnsupported;
            }
        }

        return rm::DeviceConnect(rm::Text(id));
    }
    catch (...)
    {
        return rm::kErrorGeneric;
    }
}

RM_BLE_API int32_t __cdecl rm_ble_disconnect(const char* id)
{
    try
    {
        return rm::DeviceDisconnect(rm::Text(id));
    }
    catch (...)
    {
        return rm::kErrorGeneric;
    }
}

RM_BLE_API int32_t __cdecl rm_ble_discover(const char* id)
{
    try
    {
        return rm::DeviceDiscover(rm::Text(id));
    }
    catch (...)
    {
        return rm::kErrorGeneric;
    }
}

RM_BLE_API int32_t __cdecl rm_ble_set_notify(const char* id, const char* service, const char* characteristic, int32_t enable)
{
    try
    {
        rm::Uuid svc{};
        rm::Uuid chr{};
        if (!rm::ParseUuid(service, svc) || !rm::ParseUuid(characteristic, chr))
        {
            return rm::kErrorGeneric;
        }

        return rm::DeviceSetNotify(rm::Text(id), svc, chr, enable != 0);
    }
    catch (...)
    {
        return rm::kErrorGeneric;
    }
}

RM_BLE_API int32_t __cdecl rm_ble_write(const char* id, const char* service, const char* characteristic, const uint8_t* data,
                                        int32_t length, int32_t withResponse)
{
    try
    {
        rm::Uuid svc{};
        rm::Uuid chr{};
        if (!rm::ParseUuid(service, svc) || !rm::ParseUuid(characteristic, chr) || length < 0 || (length > 0 && data == nullptr))
        {
            return rm::kErrorGeneric;
        }

        std::vector<uint8_t> bytes(data, data + length);
        return rm::DeviceWrite(rm::Text(id), svc, chr, std::move(bytes), withResponse != 0);
    }
    catch (...)
    {
        return rm::kErrorGeneric;
    }
}

RM_BLE_API int32_t __cdecl rm_ble_keepalive_start(const char* id, const char* service, const char* characteristic, const uint8_t* data,
                                                  int32_t length, int32_t periodMs)
{
    (void)id;
    (void)service;
    (void)characteristic;
    (void)data;
    (void)length;
    (void)periodMs;
    return rm::kOk;
}

RM_BLE_API int32_t __cdecl rm_ble_keepalive_stop(const char* id)
{
    (void)id;
    return rm::kOk;
}

RM_BLE_API int32_t __cdecl rm_ble_pending(void)
{
    return rm::Ring().Pending();
}

RM_BLE_API int32_t __cdecl rm_ble_drain(uint8_t* buffer, int32_t capacity)
{
    try
    {
        return rm::Ring().Drain(buffer, capacity, rm::NowSeconds());
    }
    catch (...)
    {
        return 0;
    }
}

RM_BLE_API int32_t __cdecl rm_ble_wire_fixture(uint8_t* buffer, int32_t capacity)
{
    try
    {
        // Lo mismo que Tests/Support/BleWireFixture.Encode() (solo los ids ficticios de K-10).
        const std::string row = "AA:BB:CC:00:06:70";
        const std::string anon = "AA:BB:CC:00:00:02";
        const rm::Uuid ftms = rm::ShortUuid(0x1826);
        const rm::Uuid rowerData = rm::ShortUuid(0x2AD1);
        const rm::Uuid controlPoint = rm::ShortUuid(0x2AD9);
        const uint8_t packet[] = { 0x2c, 0x00, 0x30, 0x2a, 0x01, 0xe2, 0x01, 0x00, 0x85, 0x00, 0x96, 0x00 };

        std::vector<std::vector<uint8_t>> records;
        records.push_back(rm::AdapterStateRecord(1.5, rm::kStateOn));
        records.push_back(rm::ScanResultRecord(2.0, row, "AMS-670B", -61, { ftms }));
        records.push_back(rm::ScanResultRecord(2.25, anon, "", -75, { ftms, rm::ShortUuid(0x180D) }));
        records.push_back(rm::ScanFailedRecord(3.0, rm::kErrorPermission, "permission denied"));
        records.push_back(rm::ConnectionRecord(4.0, row, true, 0));
        records.push_back(rm::RequestDoneRecord(4.5, 1, 0, ""));
        std::vector<rm::WireService> services;
        services.push_back({ ftms, { { rowerData, rm::kPropNotify }, { controlPoint, static_cast<uint8_t>(rm::kPropWrite | rm::kPropIndicate) } } });
        records.push_back(rm::DiscoverDoneRecord(5.0, 2, services));
        records.push_back(rm::RequestDoneRecord(5.5, 3, 133, "GATT error 133"));
        records.push_back(rm::NotifyRecord(6.0, row, rowerData, packet, sizeof packet));
        records.push_back(rm::ConnectionRecord(7.0, row, false, 8));
        records.push_back(rm::DroppedRecord(8.0, 3));

        size_t total = 0;
        for (const auto& record : records)
        {
            total += record.size();
        }

        if (buffer == nullptr || capacity < 0 || static_cast<size_t>(capacity) < total)
        {
            return -static_cast<int32_t>(total);
        }

        size_t at = 0;
        for (const auto& record : records)
        {
            std::memcpy(buffer + at, record.data(), record.size());
            at += record.size();
        }

        return static_cast<int32_t>(total);
    }
    catch (...)
    {
        return rm::kErrorGeneric;
    }
}
