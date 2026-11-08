// Lo compartido entre rm_ble.cpp, adapter_worker.cpp y device_queue.cpp (no se exporta).
#pragma once

#ifndef WIN32_LEAN_AND_MEAN
#define WIN32_LEAN_AND_MEAN
#endif
#ifndef NOMINMAX
#define NOMINMAX
#endif
#include <windows.h>

#include <atomic>
#include <chrono>
#include <condition_variable>
#include <deque>
#include <cstdio>
#include <functional>
#include <future>
#include <memory>
#include <mutex>
#include <string>
#include <thread>
#include <vector>

#include <winrt/base.h>
#include <winrt/Windows.Foundation.h>
#include <winrt/Windows.Foundation.Collections.h>

#include "event_ring.h"
#include "wire.h"

namespace rm
{
    // Tope de cada operación de WinRT (DT10-03): nunca queda un pedido colgado.
    constexpr std::chrono::milliseconds kOperationTimeout{ 10000 };

    double NowSeconds();

    EventRing& Ring();

    inline void Emit(std::vector<uint8_t>&& record)
    {
        Ring().Push(std::move(record));
    }

    int32_t NextRequestId();

    // "AA:BB:CC:DD:EE:FF" <-> la dirección de 48 bits (DT10-08: mayúsculas, como remoteId de FBP).
    bool ParseAddress(const std::string& text, uint64_t& address);
    std::string FormatAddress(uint64_t address);

    // "00001826-0000-1000-8000-00805f9b34fb" <-> 16 bytes en el orden del texto <-> winrt::guid.
    bool ParseUuid(const char* text, Uuid& uuid);
    Uuid UuidFromGuid(const winrt::guid& value);
    winrt::guid GuidFromUuid(const Uuid& value);

    // El mensaje de un error de WinRT en UTF-8, sin saltos de línea.
    std::string ErrorText(const winrt::hresult_error& error);

    // Un hilo MTA propio con una cola de tareas (una a la vez). request_stop termina lo encolado y sale; los hilos se unen desde
    // el hilo que llama a la API (nunca desde el propio hilo ni en DllMain).
    class MtaWorker
    {
    public:
        MtaWorker() = default;
        MtaWorker(const MtaWorker&) = delete;
        MtaWorker& operator=(const MtaWorker&) = delete;
        ~MtaWorker();

        void Start();

        // false si ya se pidió detenerlo (la tarea no corre).
        bool Post(std::function<void()> task);

        void RequestStop();

        bool WaitFinished(std::chrono::milliseconds timeout);

        bool Finished();

        void Join();

    private:
        void Run();

        std::thread thread_;
        std::mutex mutex_;
        std::condition_variable wake_;
        std::condition_variable done_;
        std::deque<std::function<void()>> tasks_;
        bool stopping_ = false;
        bool finished_ = false;
    };

    // adapter_worker.cpp: el adaptador y la búsqueda, en su hilo MTA.
    bool AdapterStart();
    void AdapterStop();
    int32_t AdapterState();
    int32_t AdapterStartScan();
    int32_t AdapterStopScan();

    // device_queue.cpp: una sesión y una cola GATT por equipo.
    int32_t DeviceConnect(const std::string& id);
    int32_t DeviceDisconnect(const std::string& id);
    int32_t DeviceDiscover(const std::string& id);
    int32_t DeviceSetNotify(const std::string& id, const Uuid& service, const Uuid& characteristic, bool enable);
    int32_t DeviceWrite(const std::string& id, const Uuid& service, const Uuid& characteristic, std::vector<uint8_t> data, bool withResponse);
    void DevicesShutdown();
}
