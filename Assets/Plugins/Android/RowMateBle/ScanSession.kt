// La búsqueda (10a 02-tech-design §4.2): BluetoothLeScanner con el filtro de servicio DEL SISTEMA (FiltersScanInOs = true; Core
// pasa FTMS 0x1826) y LOW_LATENCY. Cada resultado -> evento ScanResult cuando el equipo es nuevo en esta búsqueda o cambian su
// nombre o sus UUID, y el RSSI como mucho una vez por segundo (como la DLL de Windows). onScanFailed -> evento ScanFailed.
// Sin permiso: startScan devuelve ERROR_PERMISSION (-2), y una SecurityException tardía sale como ScanFailed -2; Core lo trata
// como negativa, no como banner (DV-100). Nunca conecta ni empareja (AC-BLE-SEC-01).
package com.rowmate.ble

import android.bluetooth.BluetoothAdapter
import android.bluetooth.le.ScanCallback
import android.bluetooth.le.ScanFilter
import android.bluetooth.le.ScanResult
import android.bluetooth.le.ScanSettings
import android.os.ParcelUuid

internal class ScanSession(private val ring: EventRing) {
    private class Seen(var name: String?, var services: List<String>, var lastEmitNanos: Long)

    private val lock = Any()
    private val seen = HashMap<String, Seen>()
    private var callback: ScanCallback? = null
    private var adapter: BluetoothAdapter? = null

    // 0 bien; ERROR_PERMISSION sin permiso; ERROR_GENERIC sin escáner (adaptador apagado).
    fun start(adapter: BluetoothAdapter, servicesCsv: String?): Int {
        stop()
        val scanner = adapter.bluetoothLeScanner ?: return Wire.ERROR_GENERIC
        val filters = ArrayList<ScanFilter>()
        for (part in (servicesCsv ?: "").split(',')) {
            val uuid = Wire.parseUuid(part) ?: continue
            filters.add(ScanFilter.Builder().setServiceUuid(ParcelUuid(uuid)).build())
        }
        val settings = ScanSettings.Builder()
            .setScanMode(ScanSettings.SCAN_MODE_LOW_LATENCY)
            .build()
        val cb = object : ScanCallback() {
            override fun onScanResult(callbackType: Int, result: ScanResult?) {
                if (result != null) {
                    onResult(this, result)
                }
            }

            override fun onBatchScanResults(results: MutableList<ScanResult>?) {
                if (results == null) {
                    return
                }
                for (result in results) {
                    onResult(this, result)
                }
            }

            override fun onScanFailed(errorCode: Int) {
                if (!isCurrent(this)) {
                    return
                }
                synchronized(lock) {
                    callback = null
                }
                ring.push(Wire.scanFailed(ring.nowSeconds(), errorCode, "scan failed (code $errorCode)"))
            }
        }
        synchronized(lock) {
            seen.clear()
            callback = cb
            this.adapter = adapter
        }
        return try {
            scanner.startScan(filters, settings, cb)
            Wire.OK
        } catch (e: SecurityException) {
            synchronized(lock) {
                callback = null
            }
            Wire.ERROR_PERMISSION
        } catch (e: Exception) {
            synchronized(lock) {
                callback = null
            }
            Wire.ERROR_GENERIC
        }
    }

    fun stop(): Int {
        val cb: ScanCallback?
        val owner: BluetoothAdapter?
        synchronized(lock) {
            cb = callback
            owner = adapter
            callback = null
            seen.clear()
        }
        if (cb == null || owner == null) {
            return Wire.OK
        }
        return try {
            // Con el adaptador apagado el escáner es null y la búsqueda ya terminó.
            owner.bluetoothLeScanner?.stopScan(cb)
            Wire.OK
        } catch (e: SecurityException) {
            Wire.ERROR_PERMISSION
        } catch (e: Exception) {
            Wire.OK
        }
    }

    private fun isCurrent(cb: ScanCallback): Boolean = synchronized(lock) { callback === cb }

    private fun onResult(cb: ScanCallback, result: ScanResult) {
        val address = result.device?.address ?: return
        val id = address.uppercase()
        val record = result.scanRecord
        var name: String? = record?.deviceName
        if (name.isNullOrEmpty()) {
            name = try {
                result.device?.name
            } catch (e: SecurityException) {
                null
            }
        }
        val uuids: List<ParcelUuid> = record?.serviceUuids ?: emptyList()
        val texts = ArrayList<String>(uuids.size)
        for (u in uuids) {
            texts.add(u.uuid.toString())
        }
        val now = System.nanoTime()
        synchronized(lock) {
            if (callback !== cb) {
                return
            }
            val previous = seen[id]
            if (previous == null) {
                seen[id] = Seen(name, texts, now)
            } else {
                // Conserva el nombre si este anuncio no lo trae (llega en la respuesta de escaneo).
                if (name.isNullOrEmpty()) {
                    name = previous.name
                }
                val merged = if (texts.isEmpty()) previous.services else texts
                val changed = name != previous.name || merged != previous.services
                if (!changed && now - previous.lastEmitNanos < RSSI_PERIOD_NANOS) {
                    return
                }
                previous.name = name
                previous.services = merged
                previous.lastEmitNanos = now
                texts.clear()
                texts.addAll(merged)
            }
        }
        val services = ArrayList<ByteArray>(texts.size)
        for (text in texts) {
            val uuid = Wire.parseUuid(text) ?: continue
            services.add(Wire.uuidBytes(uuid))
        }
        ring.push(Wire.scanResult(ring.nowSeconds(), id, name, result.rssi, services))
    }

    private companion object {
        const val RSSI_PERIOD_NANOS = 1_000_000_000L
    }
}
