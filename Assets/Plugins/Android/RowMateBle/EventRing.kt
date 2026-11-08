// El búfer de eventos (ADR 009, modelo pull; 10a 02-tech-design §4.2): los callbacks de Android (hilo del GATT, del escaneo y
// del receptor del adaptador) solo copian registros acá, bajo un lock; el hilo de Unity los drena con RmBle.pending() y
// RmBle.drain(). Con el tope lleno descarta los MÁS VIEJOS enteros y, en el próximo drenaje, el primer registro es un Dropped
// con la cuenta (como event_ring.h de Windows). El reloj es INYECTADO (nanosegundos): RmBle le pasa System.nanoTime()
// (CLOCK_MONOTONIC, la base del Stopwatch de la app, DT10-18). Sin dependencias de android.*: corre en la JVM (EmitFixture).
package com.rowmate.ble

internal class EventRing(
    private val capacity: Int = Wire.RING_BYTES,
    private val clockNanos: () -> Long = { System.nanoTime() }
) {
    private val lock = Any()
    private val records = ArrayDeque<ByteArray>()
    private var bytes = 0
    private var dropped = 0

    // La hora de los eventos, en segundos.
    fun nowSeconds(): Double = clockNanos() / 1e9

    fun push(record: ByteArray) {
        if (record.isEmpty()) {
            return
        }
        synchronized(lock) {
            records.addLast(record)
            bytes += record.size
            while (bytes > capacity && records.size > 1) {
                bytes -= records.removeFirst().size
                dropped++
            }
        }
    }

    // Bytes pendientes (con el Dropped que saldría primero).
    fun pending(): Int {
        synchronized(lock) {
            return bytes + if (dropped > 0) Wire.DROPPED_SIZE else 0
        }
    }

    // Registros ENTEROS hasta maxBytes (BleWire.MaxRecordBytes: el búfer de drenaje de NativeBleAdapter); el resto queda.
    fun drain(maxBytes: Int = Wire.MAX_RECORD_BYTES): ByteArray {
        synchronized(lock) {
            var total = 0
            var droppedRecord: ByteArray? = null
            if (dropped > 0 && maxBytes >= Wire.DROPPED_SIZE) {
                droppedRecord = Wire.dropped(nowSeconds(), dropped)
                total += droppedRecord.size
            }
            var count = 0
            for (record in records) {
                if (record.size > maxBytes - total) {
                    break
                }
                total += record.size
                count++
            }
            val out = ByteArray(total)
            var at = 0
            if (droppedRecord != null) {
                System.arraycopy(droppedRecord, 0, out, 0, droppedRecord.size)
                at += droppedRecord.size
                dropped = 0
            }
            for (i in 0 until count) {
                val record = records.removeFirst()
                System.arraycopy(record, 0, out, at, record.size)
                at += record.size
                bytes -= record.size
            }
            return out
        }
    }

    fun clear() {
        synchronized(lock) {
            records.clear()
            bytes = 0
            dropped = 0
        }
    }
}
