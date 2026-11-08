// El estado del adaptador (10a 02-tech-design §4.2; DT10-15, QA-G10a-02). Sin FEATURE_BLUETOOTH_LE o sin adaptador ->
// Unsupported; si no, BluetoothAdapter.getState(), que en API 31+ no pide permiso: STATE_ON -> On; OFF y TURNING_* -> Off.
// ACTION_STATE_CHANGED -> evento AdapterState. Kotlin NUNCA emite Unauthorized (lo decide Core con IBlePermissions) ni cambia
// la radio: solo lee (AC-BLE-SEC-01).
package com.rowmate.ble

import android.bluetooth.BluetoothAdapter
import android.bluetooth.BluetoothManager
import android.content.BroadcastReceiver
import android.content.Context
import android.content.Intent
import android.content.IntentFilter
import android.content.pm.PackageManager
import android.os.Build

internal class AdapterMonitor(private val context: Context, private val ring: EventRing) {
    private var registered = false
    private var last = Int.MIN_VALUE

    private val receiver = object : BroadcastReceiver() {
        override fun onReceive(ctx: Context?, intent: Intent?) {
            if (intent?.action != BluetoothAdapter.ACTION_STATE_CHANGED) {
                return
            }
            val raw = intent.getIntExtra(BluetoothAdapter.EXTRA_STATE, BluetoothAdapter.ERROR)
            publish(map(raw))
        }
    }

    fun adapter(): BluetoothAdapter? {
        val manager = context.getSystemService(Context.BLUETOOTH_SERVICE) as? BluetoothManager
        return manager?.adapter
    }

    // Lectura directa: la inicial y la de Core al volver al frente (por si se perdió un broadcast).
    fun read(): Int {
        if (!context.packageManager.hasSystemFeature(PackageManager.FEATURE_BLUETOOTH_LE)) {
            return Wire.STATE_UNSUPPORTED
        }
        val adapter = adapter() ?: return Wire.STATE_UNSUPPORTED
        return try {
            map(adapter.state)
        } catch (e: SecurityException) {
            Wire.STATE_UNKNOWN
        }
    }

    fun start() {
        if (registered) {
            return
        }
        val filter = IntentFilter(BluetoothAdapter.ACTION_STATE_CHANGED)
        if (Build.VERSION.SDK_INT >= 33) {
            // Un broadcast del sistema: llega igual con NOT_EXPORTED (obligatorio declarar uno desde API 34).
            context.registerReceiver(receiver, filter, Context.RECEIVER_NOT_EXPORTED)
        } else {
            context.registerReceiver(receiver, filter)
        }
        registered = true
        last = Int.MIN_VALUE
    }

    fun stop() {
        if (!registered) {
            return
        }
        registered = false
        try {
            context.unregisterReceiver(receiver)
        } catch (e: IllegalArgumentException) {
            // Ya no estaba registrado.
        }
    }

    private fun publish(state: Int) {
        synchronized(this) {
            if (state == last) {
                return
            }
            last = state
        }
        ring.push(Wire.adapterState(ring.nowSeconds(), state))
    }

    private fun map(raw: Int): Int = when (raw) {
        BluetoothAdapter.STATE_ON -> Wire.STATE_ON
        BluetoothAdapter.STATE_OFF,
        BluetoothAdapter.STATE_TURNING_ON,
        BluetoothAdapter.STATE_TURNING_OFF -> Wire.STATE_OFF
        else -> Wire.STATE_UNKNOWN
    }
}
