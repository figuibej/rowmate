// Keepalive nativo para el segundo plano (10a 02-tech-design §2.5, §4.2; ADR 017, DT10-09): con Unity en pausa, un Handler en
// el HandlerThread "rm-ble-keepalive" encola cada periodMs la escritura (0x07 en el Control Point) en la cola GATT del equipo.
// Core lo arranca solo con la app en segundo plano y lo detiene al volver (ahí retoma su periódico de 2 s). La primera
// escritura sale a un período del arranque, sin deriva (postAtTime). Las escrituras del keepalive no emiten RequestDone.
// Riesgo anotado (NO VERIFICABLE sin teléfono): el reloj del Handler (uptimeMillis) no corre en el sueño profundo; con el
// remo conectado las notificaciones despiertan la CPU cada segundo.
package com.rowmate.ble

import android.os.Handler
import android.os.HandlerThread
import android.os.SystemClock
import java.util.UUID

internal class KeepAlive(private val write: (String, UUID, UUID, ByteArray) -> Unit) {
    private val lock = Any()
    private val thread = HandlerThread("rm-ble-keepalive").apply { start() }
    private val handler = Handler(thread.looper)
    private val tasks = HashMap<String, Runnable>()

    fun start(id: String, service: UUID, characteristic: UUID, data: ByteArray, periodMs: Int): Int {
        if (periodMs <= 0) {
            return Wire.ERROR_GENERIC
        }
        val payload = data.copyOf()
        val period = periodMs.toLong()
        val task = object : Runnable {
            private var next = SystemClock.uptimeMillis() + period

            override fun run() {
                synchronized(lock) {
                    if (tasks[id] !== this) {
                        return
                    }
                }
                write(id, service, characteristic, payload)
                val now = SystemClock.uptimeMillis()
                next += period
                if (next <= now) {
                    next = now + period
                }
                handler.postAtTime(this, next)
            }

            fun first() {
                handler.postAtTime(this, next)
            }
        }
        val previous = synchronized(lock) { tasks.put(id, task) }
        if (previous != null) {
            handler.removeCallbacks(previous)
        }
        task.first()
        return Wire.OK
    }

    fun stop(id: String): Int {
        val previous = synchronized(lock) { tasks.remove(id) }
        if (previous != null) {
            handler.removeCallbacks(previous)
        }
        return Wire.OK
    }

    fun shutdown() {
        synchronized(lock) {
            tasks.clear()
        }
        handler.removeCallbacksAndMessages(null)
        thread.quitSafely()
    }
}
