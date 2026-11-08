// La sesión GATT de un equipo y su cola (10a 02-tech-design §4.2; DT10-03, DT10-17, QA-G10a-03). Todo corre en el hilo
// "rm-ble-gatt" de RmBle: connectGatt recibe ese Handler, así los callbacks de Android llegan al mismo hilo que las operaciones
// y no hay estado compartido salvo el anillo de eventos. Una operación GATT a la vez (Android no admite dos en vuelo) con tope
// de 10 s: vencido, su RequestDone sale con error y sigue la próxima.
// - connect: connectGatt(autoConnect = false, TRANSPORT_LE); con highMtu, requestMtu(512) antes de dar por conectado (como FBP
//   con mtu: 512). Bien -> Connection(true) + RequestDone(reqId, 0); mal (p. ej. 133) -> RequestDone con el estado y se cierra.
// - Caída no pedida -> Connection(false, estado); falla lo encolado y cierra (Core llama disconnect después: ya no hay sesión).
// - close (disconnect de Core o un connect nuevo del mismo id): un connect pendiente sale con -5 (cancelado), lo encolado
//   también, y disconnect() + close() del BluetoothGatt. No se reescribe el CCCD: después de disconnect() no hay enlace y el
//   equipo lo reinicia al desconectarse (Windows lo intenta porque mantiene la sesión).
// Nunca empareja ni desempareja (AC-BLE-SEC-01).
package com.rowmate.ble

import android.bluetooth.BluetoothAdapter
import android.bluetooth.BluetoothDevice
import android.bluetooth.BluetoothGatt
import android.bluetooth.BluetoothGattCallback
import android.bluetooth.BluetoothGattCharacteristic
import android.bluetooth.BluetoothGattDescriptor
import android.bluetooth.BluetoothProfile
import android.bluetooth.BluetoothStatusCodes
import android.content.Context
import android.os.Build
import android.os.Handler
import java.util.UUID

// Una operación GATT en cola. start() devuelve null si quedó esperando su callback, o la falla inmediata.
internal class GattOp(
    val kind: Kind,
    val start: (BluetoothGatt) -> GattFailure?,
    val done: (status: Int, message: String?) -> Unit
) {
    enum class Kind(val label: String) {
        DISCOVER("discover"),
        DESCRIPTOR_WRITE("setNotify"),
        CHARACTERISTIC_WRITE("write")
    }
}

internal class GattFailure(val code: Int, val message: String)

internal class GattQueue(private val handler: Handler) {
    private val ops = ArrayDeque<GattOp>()
    private var current: GattOp? = null
    private val timeout = Runnable { onTimeout() }

    fun enqueue(gatt: BluetoothGatt, op: GattOp) {
        ops.addLast(op)
        pump(gatt)
    }

    // Callback del GATT: termina la operación en curso si es de ese tipo. Una respuesta tardía de una operación vencida puede
    // caer en la siguiente del mismo tipo (Android no identifica el pedido); el tope de 10 s casi nunca se alcanza.
    fun complete(gatt: BluetoothGatt, kind: GattOp.Kind, status: Int) {
        val op = current ?: return
        if (op.kind != kind) {
            return
        }
        handler.removeCallbacks(timeout)
        current = null
        op.done(status, if (status == BluetoothGatt.GATT_SUCCESS) null else "GATT error $status")
        pump(gatt)
    }

    fun failAll(status: Int, message: String) {
        handler.removeCallbacks(timeout)
        val op = current
        current = null
        op?.done(status, message)
        while (ops.isNotEmpty()) {
            ops.removeFirst().done(status, message)
        }
        gattForTimeout = null
    }

    private var gattForTimeout: BluetoothGatt? = null

    private fun pump(gatt: BluetoothGatt) {
        while (current == null && ops.isNotEmpty()) {
            val op = ops.removeFirst()
            val failure = try {
                op.start(gatt)
            } catch (e: SecurityException) {
                GattFailure(Wire.ERROR_PERMISSION, "permission denied")
            } catch (e: Exception) {
                GattFailure(Wire.ERROR_GENERIC, op.kind.label + " failed")
            }
            if (failure == null) {
                current = op
                gattForTimeout = gatt
                handler.postDelayed(timeout, TIMEOUT_MS)
            } else {
                op.done(failure.code, failure.message)
            }
        }
    }

    private fun onTimeout() {
        val op = current ?: return
        current = null
        op.done(Wire.ERROR_GENERIC, "timed out")
        val gatt = gattForTimeout ?: return
        pump(gatt)
    }

    companion object {
        // DT10-03: cada operación GATT nativa tiene su tope.
        const val TIMEOUT_MS = 10_000L
    }
}

internal class GattSession(
    val id: String,
    private val connectReqId: Int,
    private val highMtu: Boolean,
    private val context: Context,
    private val ring: EventRing,
    private val handler: Handler,
    private val onClosed: (GattSession) -> Unit
) {
    @Volatile
    private var closing = false

    // Solo en el hilo del GATT.
    private var gatt: BluetoothGatt? = null
    private var pendingConnect = connectReqId
    private var mtuPending = false
    private var connected = false
    private var closed = false
    private val queue = GattQueue(handler)
    private val mtuTimeout = Runnable { if (mtuPending) finishConnect() }

    val isClosing: Boolean
        get() = closing

    // Cualquier hilo: desde acá la API rechaza operaciones nuevas; el cierre real lo hace close() en el hilo del GATT.
    fun markClosing() {
        closing = true
    }

    // ── Hilo del GATT ──

    fun open(adapter: BluetoothAdapter) {
        if (closing) {
            return close()
        }
        val device: BluetoothDevice = try {
            adapter.getRemoteDevice(id)
        } catch (e: IllegalArgumentException) {
            return failConnect(Wire.ERROR_GENERIC, "invalid address")
        }
        gatt = try {
            device.connectGatt(context, false, callback, BluetoothDevice.TRANSPORT_LE, BluetoothDevice.PHY_LE_1M_MASK, handler)
        } catch (e: SecurityException) {
            return failConnect(Wire.ERROR_PERMISSION, "permission denied")
        } catch (e: Exception) {
            return failConnect(Wire.ERROR_GENERIC, "connectGatt failed")
        }
        if (gatt == null) {
            failConnect(Wire.ERROR_GENERIC, "connectGatt failed")
        }
    }

    fun close() {
        closing = true
        if (closed) {
            return
        }
        handler.removeCallbacks(mtuTimeout)
        if (pendingConnect != 0) {
            val reqId = pendingConnect
            pendingConnect = 0
            done(reqId, Wire.STATUS_CANCELLED, "cancelled")
        }
        queue.failAll(Wire.STATUS_CANCELLED, "cancelled")
        release()
    }

    fun discover(reqId: Int) {
        val g = ready(reqId) ?: return
        queue.enqueue(g, GattOp(GattOp.Kind.DISCOVER, { it.discoverServices().failIfFalse("discoverServices failed") }) { status, message ->
            if (status == BluetoothGatt.GATT_SUCCESS) {
                ring.push(Wire.discoverDone(ring.nowSeconds(), reqId, describe(g)))
            } else {
                done(reqId, status, message)
            }
        })
    }

    fun setNotify(reqId: Int, service: UUID, characteristic: UUID, enable: Boolean) {
        val g = ready(reqId) ?: return
        queue.enqueue(g, GattOp(GattOp.Kind.DESCRIPTOR_WRITE, { startSetNotify(it, service, characteristic, enable) }) { status, message ->
            done(reqId, status, message)
        })
    }

    fun write(reqId: Int, service: UUID, characteristic: UUID, data: ByteArray, withResponse: Boolean) {
        val g = ready(reqId) ?: return
        queue.enqueue(g, GattOp(GattOp.Kind.CHARACTERISTIC_WRITE, { startWrite(it, service, characteristic, data, withResponse) }) { status, message ->
            done(reqId, status, message)
        })
    }

    // El keepalive del segundo plano: con respuesta, sin RequestDone (Core no lo espera).
    fun keepAliveWrite(service: UUID, characteristic: UUID, data: ByteArray) {
        val g = gatt
        if (!connected || closing || g == null) {
            return
        }
        queue.enqueue(g, GattOp(GattOp.Kind.CHARACTERISTIC_WRITE, { startWrite(it, service, characteristic, data, true) }) { _, _ -> })
    }

    // ── Callbacks (llegan en el hilo del GATT por el Handler de connectGatt) ──

    private val callback = object : BluetoothGattCallback() {
        override fun onConnectionStateChange(g: BluetoothGatt, status: Int, newState: Int) {
            if (closed || g !== gatt) {
                return
            }
            if (newState == BluetoothProfile.STATE_CONNECTED && status == BluetoothGatt.GATT_SUCCESS) {
                if (pendingConnect == 0 || connected) {
                    return
                }
                if (highMtu && requestMtu(g)) {
                    mtuPending = true
                    handler.postDelayed(mtuTimeout, GattQueue.TIMEOUT_MS)
                } else {
                    finishConnect()
                }
                return
            }
            if (newState == BluetoothProfile.STATE_DISCONNECTED || status != BluetoothGatt.GATT_SUCCESS) {
                val code = if (status == BluetoothGatt.GATT_SUCCESS) Wire.ERROR_GENERIC else status
                if (pendingConnect != 0) {
                    failConnect(code, "connect failed (status $status)")
                } else if (connected) {
                    // Caída no pedida: Core la ve como LinkLost y reintenta con una sesión nueva.
                    connected = false
                    ring.push(Wire.connection(ring.nowSeconds(), id, false, status))
                    queue.failAll(Wire.ERROR_NOT_CONNECTED, "not connected")
                    release()
                }
            }
        }

        override fun onMtuChanged(g: BluetoothGatt, mtu: Int, status: Int) {
            // El MTU que no se pudo subir no impide la conexión (el FTMS cabe en 20 bytes).
            if (!closed && g === gatt && mtuPending) {
                finishConnect()
            }
        }

        override fun onServicesDiscovered(g: BluetoothGatt, status: Int) {
            if (!closed && g === gatt) {
                queue.complete(g, GattOp.Kind.DISCOVER, status)
            }
        }

        override fun onDescriptorWrite(g: BluetoothGatt, descriptor: BluetoothGattDescriptor, status: Int) {
            if (!closed && g === gatt) {
                queue.complete(g, GattOp.Kind.DESCRIPTOR_WRITE, status)
            }
        }

        override fun onCharacteristicWrite(g: BluetoothGatt, characteristic: BluetoothGattCharacteristic, status: Int) {
            if (!closed && g === gatt) {
                queue.complete(g, GattOp.Kind.CHARACTERISTIC_WRITE, status)
            }
        }

        // API 33+: el valor llega como parámetro.
        override fun onCharacteristicChanged(g: BluetoothGatt, characteristic: BluetoothGattCharacteristic, value: ByteArray) {
            notifyValue(g, characteristic, value)
        }

        // API 26-32 (en 33+ Android llama la de arriba).
        @Suppress("DEPRECATION", "OVERRIDE_DEPRECATION")
        override fun onCharacteristicChanged(g: BluetoothGatt, characteristic: BluetoothGattCharacteristic) {
            if (Build.VERSION.SDK_INT < 33) {
                notifyValue(g, characteristic, characteristic.value)
            }
        }
    }

    // ── Ayudas (hilo del GATT) ──

    private fun notifyValue(g: BluetoothGatt, characteristic: BluetoothGattCharacteristic, value: ByteArray?) {
        if (closed || g !== gatt || !connected) {
            return
        }
        ring.push(Wire.notify(ring.nowSeconds(), id, Wire.uuidBytes(characteristic.uuid), value))
    }

    private fun requestMtu(g: BluetoothGatt): Boolean = try {
        g.requestMtu(MTU)
    } catch (e: SecurityException) {
        false
    }

    private fun finishConnect() {
        handler.removeCallbacks(mtuTimeout)
        mtuPending = false
        if (pendingConnect == 0 || closed) {
            return
        }
        val reqId = pendingConnect
        pendingConnect = 0
        connected = true
        val t = ring.nowSeconds()
        ring.push(Wire.connection(t, id, true, 0))
        ring.push(Wire.requestDone(t, reqId, Wire.OK, null))
    }

    private fun failConnect(code: Int, message: String) {
        handler.removeCallbacks(mtuTimeout)
        mtuPending = false
        if (pendingConnect != 0) {
            val reqId = pendingConnect
            pendingConnect = 0
            done(reqId, if (code == 0) Wire.ERROR_GENERIC else code, message)
        }
        release()
    }

    private fun ready(reqId: Int): BluetoothGatt? {
        val g = gatt
        if (connected && !closing && !closed && g != null) {
            return g
        }
        done(reqId, Wire.ERROR_NOT_CONNECTED, "not connected")
        return null
    }

    private fun done(reqId: Int, status: Int, message: String?) {
        if (reqId > 0) {
            ring.push(Wire.requestDone(ring.nowSeconds(), reqId, status, message ?: "error"))
        }
    }

    private fun describe(g: BluetoothGatt): List<Wire.Service> {
        val out = ArrayList<Wire.Service>()
        for (service in g.services ?: emptyList()) {
            val chars = ArrayList<Wire.Characteristic>()
            for (c in service.characteristics ?: emptyList()) {
                chars.add(Wire.Characteristic(Wire.uuidBytes(c.uuid), mapProperties(c.properties)))
            }
            out.add(Wire.Service(Wire.uuidBytes(service.uuid), chars))
        }
        return out
    }

    private fun find(g: BluetoothGatt, service: UUID, characteristic: UUID): BluetoothGattCharacteristic? =
        g.getService(service)?.getCharacteristic(characteristic)

    @Suppress("DEPRECATION")
    private fun startSetNotify(g: BluetoothGatt, service: UUID, characteristic: UUID, enable: Boolean): GattFailure? {
        val c = find(g, service, characteristic) ?: return GattFailure(Wire.ERROR_GENERIC, "characteristic not found")
        val props = c.properties
        val value = when {
            !enable -> BluetoothGattDescriptor.DISABLE_NOTIFICATION_VALUE
            props and BluetoothGattCharacteristic.PROPERTY_NOTIFY != 0 -> BluetoothGattDescriptor.ENABLE_NOTIFICATION_VALUE
            props and BluetoothGattCharacteristic.PROPERTY_INDICATE != 0 -> BluetoothGattDescriptor.ENABLE_INDICATION_VALUE
            else -> return GattFailure(Wire.ERROR_GENERIC, "notify not supported")
        }
        if (!g.setCharacteristicNotification(c, enable)) {
            return GattFailure(Wire.ERROR_GENERIC, "setCharacteristicNotification failed")
        }
        val cccd = c.getDescriptor(CCCD) ?: return GattFailure(Wire.ERROR_GENERIC, "CCCD not found")
        if (Build.VERSION.SDK_INT >= 33) {
            val code = g.writeDescriptor(cccd, value)
            if (code != BluetoothStatusCodes.SUCCESS) {
                return GattFailure(code, "writeDescriptor failed ($code)")
            }
        } else {
            cccd.value = value
            if (!g.writeDescriptor(cccd)) {
                return GattFailure(Wire.ERROR_GENERIC, "writeDescriptor failed")
            }
        }
        return null
    }

    @Suppress("DEPRECATION")
    private fun startWrite(g: BluetoothGatt, service: UUID, characteristic: UUID, data: ByteArray, withResponse: Boolean): GattFailure? {
        val c = find(g, service, characteristic) ?: return GattFailure(Wire.ERROR_GENERIC, "characteristic not found")
        val type = if (withResponse) BluetoothGattCharacteristic.WRITE_TYPE_DEFAULT else BluetoothGattCharacteristic.WRITE_TYPE_NO_RESPONSE
        if (Build.VERSION.SDK_INT >= 33) {
            val code = g.writeCharacteristic(c, data, type)
            if (code != BluetoothStatusCodes.SUCCESS) {
                return GattFailure(code, "writeCharacteristic failed ($code)")
            }
        } else {
            c.writeType = type
            c.value = data
            if (!g.writeCharacteristic(c)) {
                return GattFailure(Wire.ERROR_GENERIC, "writeCharacteristic failed")
            }
        }
        return null
    }

    // disconnect() + close(): suelta el cliente GATT (Android tiene pocos; dejarlos abiertos lleva al error 133).
    private fun release() {
        if (closed) {
            return
        }
        closed = true
        connected = false
        val g = gatt
        gatt = null
        if (g != null) {
            try {
                g.disconnect()
            } catch (e: SecurityException) {
                // Sin permiso no hay enlace que cortar; close() libera igual.
            }
            try {
                g.close()
            } catch (e: Exception) {
            }
        }
        onClosed(this)
    }

    private fun Boolean.failIfFalse(message: String): GattFailure? = if (this) null else GattFailure(Wire.ERROR_GENERIC, message)

    private fun mapProperties(properties: Int): Int {
        var result = 0
        if (properties and BluetoothGattCharacteristic.PROPERTY_READ != 0) result = result or Wire.PROP_READ
        if (properties and BluetoothGattCharacteristic.PROPERTY_WRITE != 0) result = result or Wire.PROP_WRITE
        if (properties and BluetoothGattCharacteristic.PROPERTY_WRITE_NO_RESPONSE != 0) result = result or Wire.PROP_WRITE_NO_RESPONSE
        if (properties and BluetoothGattCharacteristic.PROPERTY_NOTIFY != 0) result = result or Wire.PROP_NOTIFY
        if (properties and BluetoothGattCharacteristic.PROPERTY_INDICATE != 0) result = result or Wire.PROP_INDICATE
        return result
    }

    private companion object {
        const val MTU = 512
        val CCCD: UUID = UUID.fromString("00002902-0000-1000-8000-00805f9b34fb")
    }
}
