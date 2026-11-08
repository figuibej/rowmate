// El hilo MTA del adaptador (02-tech-design §3): el estado (BluetoothAdapter + Radio; nunca cambia el estado de la radio ni pide acceso a ella)
// y la búsqueda (BluetoothLEAdvertisementWatcher activo y sin filtro). Los callbacks de WinRT llegan en el pool de hilos y solo
// copian registros al búfer. Fusión por dirección (DT10-07): en Windows el nombre suele venir en la respuesta de escaneo y el UUID
// en el anuncio; se emite un ScanResult cuando el equipo es nuevo o cambian su nombre o sus UUID, y el RSSI como mucho 1 vez/s.
// Todos los equipos pasan al búfer (en memoria): Core descarta los que no anuncian FTMS antes de registrar nada (DT10-05).
#include "internal.h"

#include <unordered_map>

#include <winrt/Windows.Devices.Bluetooth.h>
#include <winrt/Windows.Devices.Bluetooth.Advertisement.h>
#include <winrt/Windows.Devices.Radios.h>
#include <winrt/Windows.Foundation.Metadata.h>
#include <winrt/Windows.Storage.Streams.h>

namespace rm
{
    namespace
    {
        namespace wf = winrt::Windows::Foundation;
        namespace bt = winrt::Windows::Devices::Bluetooth;
        namespace adv = winrt::Windows::Devices::Bluetooth::Advertisement;
        namespace radios = winrt::Windows::Devices::Radios;

        // Lo de WinRT del adaptador: solo se toca en el hilo del adaptador.
        struct AdapterContext
        {
            bt::BluetoothAdapter adapter{ nullptr };
            radios::Radio radio{ nullptr };
            winrt::event_token radioToken{};
            adv::BluetoothLEAdvertisementWatcher watcher{ nullptr };
            winrt::event_token receivedToken{};
            winrt::event_token stoppedToken{};
        };

        struct ScanEntry
        {
            std::string name;
            std::vector<Uuid> uuids;
            double lastEmit = -1;
        };

        // Montón sin destructor al descargar la DLL (nada de unir hilos en DllMain).
        std::mutex g_lifecycle;
        MtaWorker* g_worker = nullptr;
        AdapterContext* g_context = nullptr;
        std::atomic<int32_t> g_state{ kStateUnknown };

        std::mutex g_scanMutex;
        bool g_scanActive = false;
        uint32_t g_scanGeneration = 0;
        std::unordered_map<uint64_t, ScanEntry>* g_seen = new std::unordered_map<uint64_t, ScanEntry>();

        void SetState(int32_t state)
        {
            if (g_state.exchange(state) != state)
            {
                Emit(AdapterStateRecord(NowSeconds(), state));
            }
        }

        int32_t MapRadio(radios::RadioState state)
        {
            switch (state)
            {
                case radios::RadioState::On:
                    return kStateOn;
                case radios::RadioState::Off:
                case radios::RadioState::Disabled:
                    return kStateOff;
                default:
                    return kStateUnknown;
            }
        }

        const char* BluetoothErrorText(bt::BluetoothError error)
        {
            switch (error)
            {
                case bt::BluetoothError::RadioNotAvailable:
                    return "radio not available";
                case bt::BluetoothError::ResourceInUse:
                    return "resource in use";
                case bt::BluetoothError::DeviceNotConnected:
                    return "device not connected";
                case bt::BluetoothError::DisabledByPolicy:
                    return "disabled by policy";
                case bt::BluetoothError::NotSupported:
                    return "not supported";
                case bt::BluetoothError::DisabledByUser:
                    return "disabled by user";
                case bt::BluetoothError::ConsentRequired:
                    return "consent required";
                case bt::BluetoothError::TransportNotSupported:
                    return "transport not supported";
                default:
                    return "other error";
            }
        }

        template <typename Op>
        bool WaitOp(const Op& op)
        {
            const auto status = op.wait_for(kOperationTimeout);
            if (status == wf::AsyncStatus::Started)
            {
                op.Cancel();
                return false;
            }

            return status == wf::AsyncStatus::Completed;
        }

        // Hilo del adaptador. Sin adaptador o sin Bluetooth LE: Unsupported (Q-47). Sin radio: Unknown (se relee al buscar).
        void RefreshAdapter(AdapterContext& context)
        {
            try
            {
                const auto adapterOp = bt::BluetoothAdapter::GetDefaultAsync();
                if (!WaitOp(adapterOp))
                {
                    SetState(kStateUnknown);
                    return;
                }

                const auto adapter = adapterOp.GetResults();
                if (adapter == nullptr || !adapter.IsLowEnergySupported())
                {
                    SetState(kStateUnsupported);
                    return;
                }

                context.adapter = adapter;
                const auto radioOp = adapter.GetRadioAsync();
                if (!WaitOp(radioOp))
                {
                    SetState(kStateUnknown);
                    return;
                }

                const auto radio = radioOp.GetResults();
                if (radio == nullptr)
                {
                    SetState(kStateUnknown);
                    return;
                }

                if (context.radio != nullptr)
                {
                    context.radio.StateChanged(context.radioToken);
                }

                context.radio = radio;
                context.radioToken = radio.StateChanged([](const radios::Radio& sender, const wf::IInspectable&) {
                    try
                    {
                        SetState(MapRadio(sender.State()));
                    }
                    catch (...)
                    {
                    }
                });
                SetState(MapRadio(radio.State()));
            }
            catch (const winrt::hresult_error&)
            {
                SetState(kStateUnknown);
            }
        }

        void StopWatcher(AdapterContext& context)
        {
            {
                std::lock_guard<std::mutex> lock(g_scanMutex);
                g_scanActive = false;
            }

            if (context.watcher == nullptr)
            {
                return;
            }

            try
            {
                context.watcher.Stop();
            }
            catch (...)
            {
            }

            try
            {
                context.watcher.Received(context.receivedToken);
                context.watcher.Stopped(context.stoppedToken);
            }
            catch (...)
            {
            }

            context.watcher = nullptr;
        }

        // Pool de hilos: la fusión por dirección y el registro.
        void OnReceived(uint32_t generation, const adv::BluetoothLEAdvertisementReceivedEventArgs& args)
        {
            const auto address = args.BluetoothAddress();
            const auto advertisement = args.Advertisement();
            const std::string name = winrt::to_string(advertisement.LocalName());
            std::vector<Uuid> uuids;
            for (const auto& guid : advertisement.ServiceUuids())
            {
                uuids.push_back(UuidFromGuid(guid));
            }

            // Service Data de 16 bits (AD 0x16): el FTMS lo exige en el anuncio, con el UUID adelante.
            for (const auto& section : advertisement.DataSections())
            {
                if (section.DataType() != 0x16)
                {
                    continue;
                }

                const auto buffer = section.Data();
                if (buffer.Length() >= 2)
                {
                    const uint8_t* data = buffer.data();
                    uuids.push_back(ShortUuid(static_cast<uint16_t>(data[0] | (data[1] << 8))));
                }
            }

            const int32_t rssi = args.RawSignalStrengthInDBm();
            const double t = NowSeconds();
            std::vector<uint8_t> record;
            {
                std::lock_guard<std::mutex> lock(g_scanMutex);
                if (!g_scanActive || generation != g_scanGeneration)
                {
                    return;
                }

                auto& entry = (*g_seen)[address];
                bool changed = entry.lastEmit < 0;
                if (!name.empty() && name != entry.name)
                {
                    entry.name = name;
                    changed = true;
                }

                for (const auto& uuid : uuids)
                {
                    bool known = false;
                    for (const auto& seen : entry.uuids)
                    {
                        if (seen == uuid)
                        {
                            known = true;
                            break;
                        }
                    }

                    if (!known)
                    {
                        entry.uuids.push_back(uuid);
                        changed = true;
                    }
                }

                if (!changed && t - entry.lastEmit < 1.0)
                {
                    return;
                }

                entry.lastEmit = t;
                record = ScanResultRecord(t, FormatAddress(address), entry.name, rssi, entry.uuids);
            }

            Emit(std::move(record));
        }

        void OnStopped(uint32_t generation, bt::BluetoothError error)
        {
            {
                std::lock_guard<std::mutex> lock(g_scanMutex);
                if (!g_scanActive || generation != g_scanGeneration || error == bt::BluetoothError::Success)
                {
                    return;
                }

                g_scanActive = false;
            }

            Emit(ScanFailedRecord(NowSeconds(), static_cast<int32_t>(error), BluetoothErrorText(error)));
        }

        void StartWatcher(AdapterContext& context)
        {
            if (context.adapter == nullptr || context.radio == nullptr)
            {
                RefreshAdapter(context);
            }

            if (g_state.load() == kStateUnsupported)
            {
                Emit(ScanFailedRecord(NowSeconds(), kErrorUnsupported, "Bluetooth LE not supported"));
                return;
            }

            StopWatcher(context);
            uint32_t generation;
            {
                std::lock_guard<std::mutex> lock(g_scanMutex);
                generation = ++g_scanGeneration;
                g_scanActive = true;
                g_seen->clear();
            }

            try
            {
                adv::BluetoothLEAdvertisementWatcher watcher;
                watcher.ScanningMode(adv::BluetoothLEScanningMode::Active);
                if (wf::Metadata::ApiInformation::IsPropertyPresent(L"Windows.Devices.Bluetooth.Advertisement.BluetoothLEAdvertisementWatcher",
                                                                     L"AllowExtendedAdvertisements"))
                {
                    watcher.AllowExtendedAdvertisements(true);
                }

                context.receivedToken = watcher.Received([generation](const adv::BluetoothLEAdvertisementWatcher&,
                                                                      const adv::BluetoothLEAdvertisementReceivedEventArgs& args) {
                    try
                    {
                        OnReceived(generation, args);
                    }
                    catch (...)
                    {
                    }
                });
                context.stoppedToken = watcher.Stopped([generation](const adv::BluetoothLEAdvertisementWatcher&,
                                                                    const adv::BluetoothLEAdvertisementWatcherStoppedEventArgs& args) {
                    try
                    {
                        OnStopped(generation, args.Error());
                    }
                    catch (...)
                    {
                    }
                });
                context.watcher = watcher;
                watcher.Start();
            }
            catch (const winrt::hresult_error& error)
            {
                {
                    std::lock_guard<std::mutex> lock(g_scanMutex);
                    g_scanActive = false;
                }

                Emit(ScanFailedRecord(NowSeconds(), static_cast<int32_t>(error.code()), ErrorText(error)));
            }
        }
    }

    bool AdapterStart()
    {
        std::lock_guard<std::mutex> lock(g_lifecycle);
        if (g_worker != nullptr)
        {
            return true;
        }

        g_state.store(kStateUnknown);
        g_worker = new MtaWorker();
        g_worker->Start();
        auto ready = std::make_shared<std::promise<void>>();
        auto future = ready->get_future();
        g_worker->Post([ready]() {
            g_context = new AdapterContext();
            RefreshAdapter(*g_context);
            ready->set_value();
        });
        // La primera lectura del estado antes de volver (como mucho 3 s): el primer cuadro ya muestra la vista correcta.
        future.wait_for(std::chrono::seconds(3));
        return true;
    }

    void AdapterStop()
    {
        std::lock_guard<std::mutex> lock(g_lifecycle);
        if (g_worker == nullptr)
        {
            return;
        }

        g_worker->Post([]() {
            if (g_context == nullptr)
            {
                return;
            }

            StopWatcher(*g_context);
            try
            {
                if (g_context->radio != nullptr)
                {
                    g_context->radio.StateChanged(g_context->radioToken);
                }
            }
            catch (...)
            {
            }

            delete g_context;
            g_context = nullptr;
        });
        g_worker->RequestStop();
        g_worker->Join();
        delete g_worker;
        g_worker = nullptr;
        {
            std::lock_guard<std::mutex> scan(g_scanMutex);
            g_scanActive = false;
            g_seen->clear();
        }

        g_state.store(kStateUnknown);
    }

    int32_t AdapterState()
    {
        return g_state.load();
    }

    int32_t AdapterStartScan()
    {
        std::lock_guard<std::mutex> lock(g_lifecycle);
        if (g_worker == nullptr)
        {
            return kErrorUnsupported;
        }

        // Desde acá no hay más resultados de la búsqueda anterior (aunque el hilo tarde en reemplazar el watcher).
        {
            std::lock_guard<std::mutex> scan(g_scanMutex);
            g_scanActive = false;
        }

        return g_worker->Post([]() {
            if (g_context != nullptr)
            {
                StartWatcher(*g_context);
            }
        }) ? kOk : kErrorGeneric;
    }

    int32_t AdapterStopScan()
    {
        std::lock_guard<std::mutex> lock(g_lifecycle);
        if (g_worker == nullptr)
        {
            return kOk;
        }

        {
            std::lock_guard<std::mutex> scan(g_scanMutex);
            g_scanActive = false;
        }

        g_worker->Post([]() {
            if (g_context != nullptr)
            {
                StopWatcher(*g_context);
            }
        });
        return kOk;
    }
}
