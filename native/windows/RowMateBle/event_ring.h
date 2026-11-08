// El búfer de eventos (ADR 009, modelo pull): los callbacks de WinRT (pool de hilos) y las colas GATT solo copian registros acá,
// bajo un mutex; el hilo de Unity los drena con rm_ble_pending / rm_ble_drain. Con el tope lleno descarta los MÁS VIEJOS enteros
// y, en el próximo drenaje, el primer registro es un Dropped con la cuenta.
#pragma once

#include <cstring>
#include <deque>
#include <mutex>
#include <vector>

#include "wire.h"

namespace rm
{
    class EventRing
    {
    public:
        explicit EventRing(size_t capacity = kRingBytes)
            : capacity_(capacity)
        {
        }

        void Push(std::vector<uint8_t>&& record)
        {
            if (record.empty())
            {
                return;
            }

            std::lock_guard<std::mutex> lock(mutex_);
            bytes_ += record.size();
            records_.push_back(std::move(record));
            while (bytes_ > capacity_ && records_.size() > 1)
            {
                bytes_ -= records_.front().size();
                records_.pop_front();
                dropped_++;
            }
        }

        int32_t Pending()
        {
            std::lock_guard<std::mutex> lock(mutex_);
            const size_t n = bytes_ + (dropped_ > 0 ? kDroppedSize : 0);
            return n > 0x7FFFFFFFu ? 0x7FFFFFFF : static_cast<int32_t>(n);
        }

        int32_t Drain(uint8_t* buffer, int32_t capacity, double now)
        {
            if (buffer == nullptr || capacity <= 0)
            {
                return 0;
            }

            std::lock_guard<std::mutex> lock(mutex_);
            const size_t cap = static_cast<size_t>(capacity);
            size_t written = 0;
            if (dropped_ > 0 && cap >= kDroppedSize)
            {
                const auto record = DroppedRecord(now, dropped_);
                std::memcpy(buffer, record.data(), record.size());
                written += record.size();
                dropped_ = 0;
            }

            while (!records_.empty() && records_.front().size() <= cap - written)
            {
                const auto& record = records_.front();
                std::memcpy(buffer + written, record.data(), record.size());
                written += record.size();
                bytes_ -= record.size();
                records_.pop_front();
            }

            return static_cast<int32_t>(written);
        }

        void Clear()
        {
            std::lock_guard<std::mutex> lock(mutex_);
            records_.clear();
            bytes_ = 0;
            dropped_ = 0;
        }

    private:
        static constexpr size_t kDroppedSize = kHeaderSize + 4;

        std::mutex mutex_;
        std::deque<std::vector<uint8_t>> records_;
        size_t capacity_;
        size_t bytes_ = 0;
        int32_t dropped_ = 0;
    };
}
