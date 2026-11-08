// Formato binario v1 de los eventos (10a 02-tech-design §2.6). Es el mismo que escriben Core/Ble/Native/BleWire.cs (C#) y
// native/windows/RowMateBle/wire.h (C++), fijado byte por byte en Assets/RowMate/Tests/Golden/ble/events-v1.bin
// (native/android/check.ps1 -Golden lo compara con lo que escribe este archivo en la JVM). Little endian. Cada registro:
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
// Sin dependencias de android.*: compila y corre en la JVM (EmitFixture).
package com.rowmate.ble

import java.io.ByteArrayOutputStream
import java.util.UUID

internal object Wire {
    const val API_VERSION = 1
    const val HEADER_SIZE = 12
    const val MAX_RECORD_BYTES = 65536

    // Búfer de eventos: 4 MiB, como BleWire.NativeRingBytes (QA-G10a-13).
    const val RING_BYTES = 4 * 1024 * 1024

    const val KIND_ADAPTER_STATE = 1
    const val KIND_SCAN_RESULT = 2
    const val KIND_SCAN_FAILED = 3
    const val KIND_CONNECTION = 4
    const val KIND_REQUEST_DONE = 5
    const val KIND_NOTIFY = 6
    const val KIND_DROPPED = 7

    // Códigos de la API (los de BleWire.cs).
    const val OK = 0
    const val ERROR_GENERIC = -1
    const val ERROR_PERMISSION = -2
    const val ERROR_UNSUPPORTED = -3
    const val ERROR_NOT_CONNECTED = -4
    const val STATUS_CANCELLED = -5

    // Orden de BleAdapterState (Core/Ble/BleAdapterState.cs). Kotlin nunca emite Unauthorized (2): lo decide Core (DT10-15).
    const val STATE_UNKNOWN = 0
    const val STATE_UNSUPPORTED = 1
    const val STATE_OFF = 3
    const val STATE_ON = 5

    // BleCharacteristicProperties.
    const val PROP_READ = 1
    const val PROP_WRITE = 2
    const val PROP_WRITE_NO_RESPONSE = 4
    const val PROP_NOTIFY = 8
    const val PROP_INDICATE = 16

    // Tamaño del registro Dropped (cabecera + i32).
    const val DROPPED_SIZE = HEADER_SIZE + 4

    class Characteristic(val uuid: ByteArray, val properties: Int)

    class Service(val uuid: ByteArray, val characteristics: List<Characteristic>)

    // Un UUID de 16 bits sobre la base de Bluetooth (0000xxxx-0000-1000-8000-00805f9b34fb).
    fun shortUuid(value: Int): ByteArray {
        val u = byteArrayOf(
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x10, 0x00,
            0x80.toByte(), 0x00, 0x00, 0x80.toByte(), 0x5f, 0x9b.toByte(), 0x34, 0xfb.toByte()
        )
        u[2] = ((value shr 8) and 0xFF).toByte()
        u[3] = (value and 0xFF).toByte()
        return u
    }

    // Los 128 bits de izquierda a derecha (el orden del texto).
    fun uuidBytes(uuid: UUID): ByteArray {
        val out = ByteArray(16)
        val high = uuid.mostSignificantBits
        val low = uuid.leastSignificantBits
        for (i in 0 until 8) {
            out[i] = ((high ushr (56 - 8 * i)) and 0xFF).toByte()
            out[8 + i] = ((low ushr (56 - 8 * i)) and 0xFF).toByte()
        }
        return out
    }

    // Texto canónico ("0000180d-0000-1000-8000-00805f9b34fb", el de BleUuid.ToString) o null si no es un UUID.
    fun parseUuid(text: String?): UUID? {
        if (text == null) {
            return null
        }
        return try {
            UUID.fromString(text.trim())
        } catch (e: IllegalArgumentException) {
            null
        }
    }

    fun adapterState(t: Double, state: Int): ByteArray {
        val w = Writer(KIND_ADAPTER_STATE, t)
        w.i32(state)
        return w.finish()
    }

    fun scanResult(t: Double, id: String, name: String?, rssi: Int, services: List<ByteArray>): ByteArray {
        val w = Writer(KIND_SCAN_RESULT, t)
        w.str(id)
        w.str(name)
        w.i16(rssi)
        val n = minOf(services.size, 255)
        w.u8(n)
        for (i in 0 until n) {
            w.raw(services[i])
        }
        return w.finish()
    }

    fun scanFailed(t: Double, code: Int, message: String?): ByteArray {
        val w = Writer(KIND_SCAN_FAILED, t)
        w.i32(code)
        w.str(message)
        return w.finish()
    }

    fun connection(t: Double, id: String, connected: Boolean, code: Int): ByteArray {
        val w = Writer(KIND_CONNECTION, t)
        w.str(id)
        w.u8(if (connected) 1 else 0)
        w.i32(code)
        return w.finish()
    }

    // status 0: sin más datos (connect, setNotify, write); si no, el mensaje.
    fun requestDone(t: Double, reqId: Int, status: Int, message: String?): ByteArray {
        val w = Writer(KIND_REQUEST_DONE, t)
        w.i32(reqId)
        w.i32(status)
        if (status != 0) {
            w.str(message)
        }
        return w.finish()
    }

    fun discoverDone(t: Double, reqId: Int, services: List<Service>): ByteArray {
        val w = Writer(KIND_REQUEST_DONE, t)
        w.i32(reqId)
        w.i32(0)
        val nSvc = minOf(services.size, 255)
        w.u8(nSvc)
        for (s in 0 until nSvc) {
            w.raw(services[s].uuid)
            val chars = services[s].characteristics
            val nChr = minOf(chars.size, 255)
            w.u8(nChr)
            for (c in 0 until nChr) {
                w.raw(chars[c].uuid)
                w.u8(chars[c].properties)
            }
        }
        return w.finish()
    }

    fun notify(t: Double, id: String, characteristic: ByteArray, value: ByteArray?): ByteArray {
        val w = Writer(KIND_NOTIFY, t)
        w.str(id)
        w.raw(characteristic)
        val data = value ?: ByteArray(0)
        val n = minOf(data.size, 0xFFFF)
        w.u16(n)
        w.raw(data, n)
        return w.finish()
    }

    fun dropped(t: Double, count: Int): ByteArray {
        val w = Writer(KIND_DROPPED, t)
        w.i32(count)
        return w.finish()
    }

    // Un registro: la cabecera con el largo en 0 hasta finish().
    class Writer(kind: Int, t: Double) {
        private val out = ByteArrayOutputStream(64)

        init {
            u8(kind)
            u8(0)
            u16(0)
            val bits = java.lang.Double.doubleToRawLongBits(t)
            for (i in 0 until 8) {
                u8(((bits ushr (8 * i)) and 0xFF).toInt())
            }
        }

        fun u8(value: Int) {
            out.write(value and 0xFF)
        }

        fun u16(value: Int) {
            u8(value)
            u8(value shr 8)
        }

        fun i16(value: Int) {
            u16(value and 0xFFFF)
        }

        fun i32(value: Int) {
            u8(value)
            u8(value shr 8)
            u8(value shr 16)
            u8(value shr 24)
        }

        // Vacío = null (como BleWireWriter: string.IsNullOrEmpty).
        fun str(value: String?) {
            if (value.isNullOrEmpty()) {
                u16(0)
                return
            }
            var bytes = value.toByteArray(Charsets.UTF_8)
            if (bytes.size > 0xFFFF) {
                bytes = bytes.copyOf(0xFFFF)
            }
            u16(bytes.size)
            out.write(bytes, 0, bytes.size)
        }

        fun raw(bytes: ByteArray, count: Int = bytes.size) {
            if (count > 0) {
                out.write(bytes, 0, count)
            }
        }

        // Cierra el registro con el largo del payload. Uno de más de 64 KiB no se emite (vacío), como en la DLL.
        fun finish(): ByteArray {
            val bytes = out.toByteArray()
            if (bytes.size > MAX_RECORD_BYTES) {
                return ByteArray(0)
            }
            val payload = bytes.size - HEADER_SIZE
            bytes[2] = (payload and 0xFF).toByte()
            bytes[3] = ((payload shr 8) and 0xFF).toByte()
            return bytes
        }
    }
}
