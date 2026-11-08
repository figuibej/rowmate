// Una sesión por equipo con su cola GATT (02-tech-design §3): un hilo MTA propio, una operación a la vez y tope de 10 s por
// operación (DT10-03). Sesiones (DT10-17, QA-G10a-03): un connect sobre un id con sesión primero la cierra (como mucho una sesión
// y una cola por id); disconnect cancela un connect pendiente (IAsyncInfo::Cancel de la operación en curso) y su RequestDone
// sale con -5; cerrar quita ValueChanged, pone el CCCD en None (mejor esfuerzo), MaintainConnection = false y cierra servicios,
// sesión y equipo. Nunca empareja ni desempareja (AC-BLE-SEC-01; build.ps1 busca esas APIs).
#include "internal.h"

#include <unordered_map>

#include <winrt/Windows.Devices.Bluetooth.h>
#include <winrt/Windows.Devices.Bluetooth.GenericAttributeProfile.h>
#include <winrt/Windows.Storage.Streams.h>

namespace rm
{
    namespace
    {
        namespace wf = winrt::Windows::Foundation;
        namespace bt = winrt::Windows::Devices::Bluetooth;
        namespace gatt = winrt::Windows::Devices::Bluetooth::GenericAttributeProfile;
        namespace streams = winrt::Windows::Storage::Streams;

        // El CCCD en None al cerrar: corto, para no demorar el cierre con un equipo que ya no responde.
        constexpr std::chrono::milliseconds kCloseTimeout{ 2000 };

        enum class OpResult
        {
            Ok,
            Cancelled,
            Failed
        };

        uint8_t MapProperties(gatt::GattCharacteristicProperties properties)
        {
            const auto has = [properties](gatt::GattCharacteristicProperties flag) {
                return (properties & flag) == flag;
            };
            uint8_t result = 0;
            if (has(gatt::GattCharacteristicProperties::Read))
            {
                result |= kPropRead;
            }

            if (has(gatt::GattCharacteristicProperties::Write))
            {
                result |= kPropWrite;
            }

            if (has(gatt::GattCharacteristicProperties::WriteWithoutResponse))
            {
                result |= kPropWriteNoResponse;
            }

            if (has(gatt::GattCharacteristicProperties::Notify))
            {
                result |= kPropNotify;
            }

            if (has(gatt::GattCharacteristicProperties::Indicate))
            {
                result |= kPropIndicate;
            }

            return result;
        }

        const char* StatusText(gatt::GattCommunicationStatus status)
        {
            switch (status)
            {
                case gatt::GattCommunicationStatus::Unreachable:
                    return "unreachable";
                case gatt::GattCommunicationStatus::ProtocolError:
                    return "protocol error";
                case gatt::GattCommunicationStatus::AccessDenied:
                    return "access denied";
                default:
                    return "error";
            }
        }

        class Session : public std::enable_shared_from_this<Session>
        {
        public:
            Session(std::string id, uint64_t address)
                : id_(std::move(id)), address_(address)
            {
            }

            MtaWorker worker;
            std::shared_ptr<Session> predecessor;

            // Cualquier hilo: desde acá las operaciones se rechazan y la que está en curso se cancela.
            void BeginClose()
            {
                std::lock_guard<std::mutex> lock(mutex_);
                closing_ = true;
                if (current_ != nullptr)
                {
                    try
                    {
                        current_.Cancel();
                    }
                    catch (...)
                    {
                    }
                }
            }

            bool Closing()
            {
                std::lock_guard<std::mutex> lock(mutex_);
                return closing_;
            }

            // ── Tareas (hilo del equipo) ──

            void Connect(int32_t reqId)
            {
                if (predecessor != nullptr)
                {
                    // La sesión anterior del mismo id termina de cerrarse antes de abrir esta.
                    predecessor->worker.WaitFinished(std::chrono::seconds(15));
                    predecessor.reset();
                }

                std::string error;
                try
                {
                    const auto deviceOp = bt::BluetoothLEDevice::FromBluetoothAddressAsync(address_);
                    auto result = Await(deviceOp, kOperationTimeout, error);
                    if (result != OpResult::Ok)
                    {
                        return Fail(reqId, result, kErrorGeneric, error);
                    }

                    device_ = deviceOp.GetResults();
                    if (device_ == nullptr)
                    {
                        return Fail(reqId, OpResult::Failed, kErrorGeneric, "device not found");
                    }

                    std::weak_ptr<Session> weak = shared_from_this();
                    const std::string id = id_;
                    statusToken_ = device_.ConnectionStatusChanged([weak, id](const bt::BluetoothLEDevice& sender, const wf::IInspectable&) {
                        try
                        {
                            const auto self = weak.lock();
                            if (self == nullptr || self->Closing())
                            {
                                return;
                            }

                            const bool connected = sender.ConnectionStatus() == bt::BluetoothConnectionStatus::Connected;
                            Emit(ConnectionRecord(NowSeconds(), id, connected, 0));
                        }
                        catch (...)
                        {
                        }
                    });

                    const auto sessionOp = gatt::GattSession::FromDeviceIdAsync(device_.BluetoothDeviceId());
                    result = Await(sessionOp, kOperationTimeout, error);
                    if (result != OpResult::Ok)
                    {
                        return Fail(reqId, result, kErrorGeneric, error);
                    }

                    session_ = sessionOp.GetResults();
                    if (session_ != nullptr)
                    {
                        session_.MaintainConnection(true);
                    }

                    // Sin caché: obliga a abrir el enlace (el connect de FBP).
                    const auto servicesOp = device_.GetGattServicesAsync(bt::BluetoothCacheMode::Uncached);
                    result = Await(servicesOp, kOperationTimeout, error);
                    if (result != OpResult::Ok)
                    {
                        return Fail(reqId, result, kErrorGeneric, error);
                    }

                    const auto services = servicesOp.GetResults();
                    if (services.Status() != gatt::GattCommunicationStatus::Success)
                    {
                        return Fail(reqId, OpResult::Failed, static_cast<int32_t>(services.Status()), StatusText(services.Status()));
                    }

                    services_.clear();
                    for (const auto& service : services.Services())
                    {
                        services_.push_back(service);
                    }

                    if (Closing())
                    {
                        return Fail(reqId, OpResult::Cancelled, kStatusCancelled, "cancelled");
                    }

                    connected_ = true;
                    const double t = NowSeconds();
                    Emit(ConnectionRecord(t, id_, true, 0));
                    Emit(RequestDoneRecord(t, reqId, kOk, ""));
                }
                catch (const winrt::hresult_error& e)
                {
                    Fail(reqId, OpResult::Failed, static_cast<int32_t>(e.code()), ErrorText(e));
                }
                catch (...)
                {
                    Fail(reqId, OpResult::Failed, kErrorGeneric, "connect failed");
                }
            }

            void Discover(int32_t reqId)
            {
                if (!Ready(reqId))
                {
                    return;
                }

                std::string error;
                try
                {
                    ReleaseCharacteristics();
                    std::vector<WireService> out;
                    for (const auto& service : services_)
                    {
                        WireService wire{ UuidFromGuid(service.Uuid()), {} };
                        const auto op = service.GetCharacteristicsAsync(bt::BluetoothCacheMode::Uncached);
                        const auto result = Await(op, kOperationTimeout, error);
                        if (result == OpResult::Cancelled)
                        {
                            return Done(reqId, kStatusCancelled, "cancelled");
                        }

                        // Windows niega algunos servicios (p. ej. HID): quedan sin características, como si estuvieran vacíos.
                        if (result == OpResult::Ok)
                        {
                            const auto characteristics = op.GetResults();
                            if (characteristics.Status() == gatt::GattCommunicationStatus::Success)
                            {
                                for (const auto& characteristic : characteristics.Characteristics())
                                {
                                    Characteristic entry;
                                    entry.service = wire.uuid;
                                    entry.uuid = UuidFromGuid(characteristic.Uuid());
                                    entry.properties = MapProperties(characteristic.CharacteristicProperties());
                                    entry.handle = characteristic;
                                    wire.characteristics.push_back({ entry.uuid, entry.properties });
                                    characteristics_.push_back(std::move(entry));
                                }
                            }
                        }

                        out.push_back(std::move(wire));
                    }

                    Emit(DiscoverDoneRecord(NowSeconds(), reqId, out));
                }
                catch (const winrt::hresult_error& e)
                {
                    Done(reqId, static_cast<int32_t>(e.code()), ErrorText(e));
                }
                catch (...)
                {
                    Done(reqId, kErrorGeneric, "discover failed");
                }
            }

            void SetNotify(int32_t reqId, const Uuid& service, const Uuid& characteristic, bool enable)
            {
                if (!Ready(reqId))
                {
                    return;
                }

                auto* entry = Find(service, characteristic);
                if (entry == nullptr)
                {
                    return Done(reqId, kErrorGeneric, "characteristic not found");
                }

                auto value = gatt::GattClientCharacteristicConfigurationDescriptorValue::None;
                if (enable)
                {
                    if ((entry->properties & kPropNotify) != 0)
                    {
                        value = gatt::GattClientCharacteristicConfigurationDescriptorValue::Notify;
                    }
                    else if ((entry->properties & kPropIndicate) != 0)
                    {
                        value = gatt::GattClientCharacteristicConfigurationDescriptorValue::Indicate;
                    }
                    else
                    {
                        return Done(reqId, kErrorGeneric, "notify not supported");
                    }
                }

                std::string error;
                try
                {
                    if (enable && !entry->subscribed)
                    {
                        std::weak_ptr<Session> weak = shared_from_this();
                        const std::string id = id_;
                        const Uuid uuid = entry->uuid;
                        entry->token = entry->handle.ValueChanged([weak, id, uuid](const gatt::GattCharacteristic&, const gatt::GattValueChangedEventArgs& args) {
                            try
                            {
                                const auto self = weak.lock();
                                if (self == nullptr || self->Closing())
                                {
                                    return;
                                }

                                const auto buffer = args.CharacteristicValue();
                                Emit(NotifyRecord(NowSeconds(), id, uuid, buffer.data(), buffer.Length()));
                            }
                            catch (...)
                            {
                            }
                        });
                        entry->subscribed = true;
                    }

                    const auto op = entry->handle.WriteClientCharacteristicConfigurationDescriptorAsync(value);
                    const auto result = Await(op, kOperationTimeout, error);
                    if (result != OpResult::Ok)
                    {
                        return Done(reqId, result == OpResult::Cancelled ? kStatusCancelled : kErrorGeneric, error);
                    }

                    const auto status = op.GetResults();
                    if (status != gatt::GattCommunicationStatus::Success)
                    {
                        return Done(reqId, static_cast<int32_t>(status), StatusText(status));
                    }

                    if (!enable && entry->subscribed)
                    {
                        entry->handle.ValueChanged(entry->token);
                        entry->subscribed = false;
                    }

                    Done(reqId, kOk, "");
                }
                catch (const winrt::hresult_error& e)
                {
                    Done(reqId, static_cast<int32_t>(e.code()), ErrorText(e));
                }
                catch (...)
                {
                    Done(reqId, kErrorGeneric, "setNotify failed");
                }
            }

            void Write(int32_t reqId, const Uuid& service, const Uuid& characteristic, const std::vector<uint8_t>& data, bool withResponse)
            {
                if (!Ready(reqId))
                {
                    return;
                }

                auto* entry = Find(service, characteristic);
                if (entry == nullptr)
                {
                    return Done(reqId, kErrorGeneric, "characteristic not found");
                }

                std::string error;
                try
                {
                    streams::DataWriter writer;
                    writer.WriteBytes(winrt::array_view<const uint8_t>(data.data(), data.data() + data.size()));
                    const auto buffer = writer.DetachBuffer();
                    const auto option = withResponse ? gatt::GattWriteOption::WriteWithResponse : gatt::GattWriteOption::WriteWithoutResponse;
                    const auto op = entry->handle.WriteValueWithResultAsync(buffer, option);
                    const auto result = Await(op, kOperationTimeout, error);
                    if (result != OpResult::Ok)
                    {
                        return Done(reqId, result == OpResult::Cancelled ? kStatusCancelled : kErrorGeneric, error);
                    }

                    const auto written = op.GetResults();
                    if (written.Status() != gatt::GattCommunicationStatus::Success)
                    {
                        int32_t code = static_cast<int32_t>(written.Status());
                        const auto protocol = written.ProtocolError();
                        if (protocol != nullptr)
                        {
                            code = protocol.Value();
                        }

                        return Done(reqId, code, StatusText(written.Status()));
                    }

                    Done(reqId, kOk, "");
                }
                catch (const winrt::hresult_error& e)
                {
                    Done(reqId, static_cast<int32_t>(e.code()), ErrorText(e));
                }
                catch (...)
                {
                    Done(reqId, kErrorGeneric, "write failed");
                }
            }

            void Close()
            {
                for (auto& entry : characteristics_)
                {
                    if (!entry.subscribed)
                    {
                        continue;
                    }

                    try
                    {
                        entry.handle.ValueChanged(entry.token);
                        entry.subscribed = false;
                        if (connected_)
                        {
                            const auto op = entry.handle.WriteClientCharacteristicConfigurationDescriptorAsync(
                                gatt::GattClientCharacteristicConfigurationDescriptorValue::None);
                            if (op.wait_for(kCloseTimeout) == wf::AsyncStatus::Started)
                            {
                                op.Cancel();
                            }
                        }
                    }
                    catch (...)
                    {
                    }
                }

                Release();
            }

        private:
            struct Characteristic
            {
                Uuid service{};
                Uuid uuid{};
                uint8_t properties = 0;
                gatt::GattCharacteristic handle{ nullptr };
                winrt::event_token token{};
                bool subscribed = false;
            };

            // Espera la operación con su tope; con la sesión cerrándose no la empieza a esperar.
            template <typename Op>
            OpResult Await(const Op& op, std::chrono::milliseconds timeout, std::string& error)
            {
                {
                    std::lock_guard<std::mutex> lock(mutex_);
                    if (closing_)
                    {
                        try
                        {
                            op.Cancel();
                        }
                        catch (...)
                        {
                        }

                        error = "cancelled";
                        return OpResult::Cancelled;
                    }

                    current_ = op;
                }

                wf::AsyncStatus status = wf::AsyncStatus::Error;
                try
                {
                    status = op.wait_for(timeout);
                }
                catch (...)
                {
                }

                bool closing;
                {
                    std::lock_guard<std::mutex> lock(mutex_);
                    current_ = nullptr;
                    closing = closing_;
                }

                switch (status)
                {
                    case wf::AsyncStatus::Completed:
                        return OpResult::Ok;
                    case wf::AsyncStatus::Started:
                        try
                        {
                            op.Cancel();
                        }
                        catch (...)
                        {
                        }

                        error = "timed out";
                        return closing ? OpResult::Cancelled : OpResult::Failed;
                    case wf::AsyncStatus::Canceled:
                        error = "cancelled";
                        return OpResult::Cancelled;
                    default:
                        try
                        {
                            error = ErrorText(winrt::hresult_error(op.ErrorCode()));
                        }
                        catch (...)
                        {
                            error = "error";
                        }

                        return closing ? OpResult::Cancelled : OpResult::Failed;
                }
            }

            bool Ready(int32_t reqId)
            {
                if (connected_ && !Closing())
                {
                    return true;
                }

                Done(reqId, kErrorNotConnected, "not connected");
                return false;
            }

            void Done(int32_t reqId, int32_t status, const std::string& message)
            {
                Emit(RequestDoneRecord(NowSeconds(), reqId, status, message));
            }

            // Un connect que no terminó: suelta lo abierto y avisa (cancelado si la sesión se está cerrando).
            void Fail(int32_t reqId, OpResult result, int32_t code, const std::string& message)
            {
                Release();
                if (result == OpResult::Cancelled || Closing())
                {
                    return Done(reqId, kStatusCancelled, "cancelled");
                }

                Done(reqId, code == 0 ? kErrorGeneric : code, message);
            }

            Characteristic* Find(const Uuid& service, const Uuid& characteristic)
            {
                for (auto& entry : characteristics_)
                {
                    if (entry.service == service && entry.uuid == characteristic)
                    {
                        return &entry;
                    }
                }

                return nullptr;
            }

            void ReleaseCharacteristics()
            {
                for (auto& entry : characteristics_)
                {
                    if (entry.subscribed)
                    {
                        try
                        {
                            entry.handle.ValueChanged(entry.token);
                        }
                        catch (...)
                        {
                        }
                    }
                }

                characteristics_.clear();
            }

            void Release()
            {
                ReleaseCharacteristics();
                if (session_ != nullptr)
                {
                    try
                    {
                        session_.MaintainConnection(false);
                    }
                    catch (...)
                    {
                    }
                }

                for (auto& service : services_)
                {
                    try
                    {
                        service.Close();
                    }
                    catch (...)
                    {
                    }
                }

                services_.clear();
                if (session_ != nullptr)
                {
                    try
                    {
                        session_.Close();
                    }
                    catch (...)
                    {
                    }

                    session_ = nullptr;
                }

                if (device_ != nullptr)
                {
                    try
                    {
                        device_.ConnectionStatusChanged(statusToken_);
                        device_.Close();
                    }
                    catch (...)
                    {
                    }

                    device_ = nullptr;
                }

                connected_ = false;
            }

            const std::string id_;
            const uint64_t address_;
            std::mutex mutex_;
            bool closing_ = false;
            wf::IAsyncInfo current_{ nullptr };

            // Solo en el hilo del equipo.
            bt::BluetoothLEDevice device_{ nullptr };
            winrt::event_token statusToken_{};
            gatt::GattSession session_{ nullptr };
            std::vector<gatt::GattDeviceService> services_;
            std::vector<Characteristic> characteristics_;
            bool connected_ = false;
        };

        // Montón sin destructor al descargar la DLL (nada de unir hilos en DllMain). Las sesiones cerradas esperan en el
        // "cementerio" hasta que su hilo termina y se une desde el hilo de la API.
        std::mutex g_mutex;
        auto* g_sessions = new std::unordered_map<std::string, std::shared_ptr<Session>>();
        auto* g_graveyard = new std::vector<std::shared_ptr<Session>>();

        // Bajo g_mutex.
        void Bury(const std::shared_ptr<Session>& session)
        {
            session->BeginClose();
            const auto self = session;
            session->worker.Post([self]() { self->Close(); });
            session->worker.RequestStop();
            g_graveyard->push_back(session);
        }

        // Bajo g_mutex: une los hilos que ya terminaron.
        void Reap()
        {
            for (size_t i = 0; i < g_graveyard->size();)
            {
                auto& session = (*g_graveyard)[i];
                if (session->worker.Finished())
                {
                    session->worker.Join();
                    g_graveyard->erase(g_graveyard->begin() + static_cast<std::ptrdiff_t>(i));
                }
                else
                {
                    i++;
                }
            }
        }

        template <typename Task>
        int32_t PostOp(const std::string& id, Task task)
        {
            std::lock_guard<std::mutex> lock(g_mutex);
            const auto it = g_sessions->find(id);
            if (it == g_sessions->end() || it->second->Closing())
            {
                return kErrorNotConnected;
            }

            const auto session = it->second;
            const int32_t reqId = NextRequestId();
            if (!session->worker.Post([session, reqId, task]() { task(*session, reqId); }))
            {
                return kErrorNotConnected;
            }

            return reqId;
        }
    }

    int32_t DeviceConnect(const std::string& id)
    {
        uint64_t address = 0;
        if (!ParseAddress(id, address))
        {
            return kErrorGeneric;
        }

        const std::string key = FormatAddress(address);
        std::lock_guard<std::mutex> lock(g_mutex);
        Reap();
        auto session = std::make_shared<Session>(key, address);
        const auto it = g_sessions->find(key);
        if (it != g_sessions->end())
        {
            session->predecessor = it->second;
            Bury(it->second);
        }

        (*g_sessions)[key] = session;
        session->worker.Start();
        const int32_t reqId = NextRequestId();
        session->worker.Post([session, reqId]() { session->Connect(reqId); });
        return reqId;
    }

    int32_t DeviceDisconnect(const std::string& id)
    {
        uint64_t address = 0;
        if (!ParseAddress(id, address))
        {
            return kOk;
        }

        const std::string key = FormatAddress(address);
        std::lock_guard<std::mutex> lock(g_mutex);
        Reap();
        const auto it = g_sessions->find(key);
        if (it == g_sessions->end())
        {
            return kOk;
        }

        const auto session = it->second;
        g_sessions->erase(it);
        Bury(session);
        return kOk;
    }

    int32_t DeviceDiscover(const std::string& id)
    {
        uint64_t address = 0;
        if (!ParseAddress(id, address))
        {
            return kErrorNotConnected;
        }

        return PostOp(FormatAddress(address), [](Session& session, int32_t reqId) { session.Discover(reqId); });
    }

    int32_t DeviceSetNotify(const std::string& id, const Uuid& service, const Uuid& characteristic, bool enable)
    {
        uint64_t address = 0;
        if (!ParseAddress(id, address))
        {
            return kErrorNotConnected;
        }

        return PostOp(FormatAddress(address), [service, characteristic, enable](Session& session, int32_t reqId) {
            session.SetNotify(reqId, service, characteristic, enable);
        });
    }

    int32_t DeviceWrite(const std::string& id, const Uuid& service, const Uuid& characteristic, std::vector<uint8_t> data, bool withResponse)
    {
        uint64_t address = 0;
        if (!ParseAddress(id, address))
        {
            return kErrorNotConnected;
        }

        return PostOp(FormatAddress(address), [service, characteristic, data = std::move(data), withResponse](Session& session, int32_t reqId) {
            session.Write(reqId, service, characteristic, data, withResponse);
        });
    }

    void DevicesShutdown()
    {
        std::lock_guard<std::mutex> lock(g_mutex);
        for (auto& entry : *g_sessions)
        {
            Bury(entry.second);
        }

        g_sessions->clear();
        for (auto& session : *g_graveyard)
        {
            session->worker.Join();
        }

        // Ya unidos: las sesiones se sueltan acá (sin hilos vivos).
        for (auto& session : *g_graveyard)
        {
            session->predecessor.reset();
        }

        g_graveyard->clear();
    }
}
