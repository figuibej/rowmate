// El plugin de Bluetooth LE de Android (10a 02-tech-design §4; ADR 009). Mismas funciones que la API C de la DLL de Windows
// (rm_ble_*), como métodos @JvmStatic de un object: los llama Platform/Ble/AndroidBleApi por JNI crudo, con los nombres y firmas
// de Core/Ble/Native/RmBleJniSignatures (native/android/check.ps1 compara esa tabla con javap de esta clase: acá NO puede haber
// otro miembro público). Modelo pull: los hilos de Android solo escriben en el anillo de eventos (EventRing) y Unity lo drena
// cada cuadro; ningún hilo nativo entra a C#. Nada lanza hacia Unity: todo error es un código <= 0 o un RequestDone con mensaje.
// - Sin dependencia del reproductor de Unity (DT10-11): C# pasa la Activity en init, así compila contra android.jar solo.
// - Reloj de los eventos: System.nanoTime() (CLOCK_MONOTONIC, la base del Stopwatch de la app; DT10-18).
// - Permiso (DT10-15): la regla es de Core (NativeBleAdapter + IBlePermissions); acá solo checkSelfPermission, la explicación
//   del sistema y la negativa recordada en SharedPreferences("rm_ble"). Kotlin nunca emite Unauthorized.
// - Nunca enciende ni apaga la radio, nunca empareja ni desempareja (AC-BLE-SEC-01): "Activar Bluetooth" es el pedido del sistema.
package com.rowmate.ble

import android.Manifest
import android.app.Activity
import android.bluetooth.BluetoothAdapter
import android.content.Context
import android.content.Intent
import android.content.pm.PackageManager
import android.net.Uri
import android.os.Build
import android.os.Handler
import android.os.HandlerThread
import android.provider.Settings
import android.view.HapticFeedbackConstants
import java.lang.ref.WeakReference
import java.util.UUID
import java.util.concurrent.ConcurrentHashMap
import java.util.concurrent.atomic.AtomicInteger

// Lo que vive entre init y shutdown.
internal class BleRuntime(
    val context: Context,
    val thread: HandlerThread,
    val handler: Handler,
    val monitor: AdapterMonitor,
    val scan: ScanSession,
    val keepAlive: KeepAlive
) {
    val sessions = ConcurrentHashMap<String, GattSession>()
}

object RmBle {
    private const val PREFS = "rm_ble"
    private const val PREF_DENIED = "permission_denied"

    private val lock = Any()
    private val ring = EventRing(Wire.RING_BYTES) { System.nanoTime() }
    private val requestIds = AtomicInteger(0)

    @Volatile
    private var runtime: BleRuntime? = null

    @Volatile
    private var activity: WeakReference<Activity>? = null

    // ── Ciclo de vida ──

    // 0 bien. Idempotente (actualiza la Activity).
    @JvmStatic
    fun init(activity: Activity?): Int {
        if (activity == null) {
            return Wire.ERROR_GENERIC
        }
        synchronized(lock) {
            this.activity = WeakReference(activity)
            if (runtime != null) {
                return Wire.OK
            }
            return try {
                val context = activity.applicationContext
                val thread = HandlerThread("rm-ble-gatt")
                thread.start()
                val handler = Handler(thread.looper)
                val monitor = AdapterMonitor(context, ring)
                monitor.start()
                val created = arrayOfNulls<BleRuntime>(1)
                val keepAlive = KeepAlive { id, service, characteristic, data ->
                    val rt = created[0]
                    val session = rt?.sessions?.get(id)
                    if (rt != null && session != null && !session.isClosing) {
                        rt.handler.post { session.keepAliveWrite(service, characteristic, data) }
                    }
                }
                val rt = BleRuntime(context, thread, handler, monitor, ScanSession(ring), keepAlive)
                created[0] = rt
                runtime = rt
                Wire.OK
            } catch (e: Exception) {
                Wire.ERROR_GENERIC
            }
        }
    }

    @JvmStatic
    fun shutdown() {
        val rt: BleRuntime
        synchronized(lock) {
            rt = runtime ?: return
            runtime = null
        }
        try {
            rt.scan.stop()
            rt.keepAlive.shutdown()
            rt.monitor.stop()
            val open = ArrayList(rt.sessions.values)
            rt.sessions.clear()
            for (session in open) {
                session.markClosing()
            }
            rt.handler.post {
                for (session in open) {
                    session.close()
                }
            }
            rt.thread.quitSafely()
        } catch (e: Exception) {
        }
        ring.clear()
    }

    @JvmStatic
    fun apiVersion(): Int = Wire.API_VERSION

    @JvmStatic
    fun monotonicSeconds(): Double = ring.nowSeconds()

    // Orden de BleAdapterState; sin init, Unknown.
    @JvmStatic
    fun adapterState(): Int = try {
        runtime?.monitor?.read() ?: Wire.STATE_UNKNOWN
    } catch (e: Exception) {
        Wire.STATE_UNKNOWN
    }

    // ── Búsqueda ──

    @JvmStatic
    fun startScan(servicesCsv: String?): Int {
        val rt = runtime ?: return Wire.ERROR_UNSUPPORTED
        return try {
            if (!hasScanPermission()) {
                return Wire.ERROR_PERMISSION
            }
            val adapter = rt.monitor.adapter() ?: return Wire.ERROR_UNSUPPORTED
            rt.scan.start(adapter, servicesCsv)
        } catch (e: SecurityException) {
            Wire.ERROR_PERMISSION
        } catch (e: Exception) {
            Wire.ERROR_GENERIC
        }
    }

    @JvmStatic
    fun stopScan(): Int {
        val rt = runtime ?: return Wire.OK
        return rt.scan.stop()
    }

    // ── Conexión (DT10-17: como mucho una sesión por id) ──

    // reqId > 0 (termina con RequestDone) o un código <= 0. Sobre un id con sesión (conectada o pendiente), primero la cierra.
    @JvmStatic
    fun connect(id: String?, highMtu: Boolean): Int {
        val rt = runtime ?: return Wire.ERROR_UNSUPPORTED
        val key = normalize(id) ?: return Wire.ERROR_GENERIC
        return try {
            val adapter = rt.monitor.adapter() ?: return Wire.ERROR_UNSUPPORTED
            val reqId = nextRequestId()
            val session = GattSession(key, reqId, highMtu, rt.context, ring, rt.handler) { closed ->
                rt.sessions.remove(closed.id, closed)
                rt.keepAlive.stop(closed.id)
            }
            val previous = rt.sessions.put(key, session)
            if (previous != null) {
                previous.markClosing()
                rt.keepAlive.stop(key)
                rt.handler.post { previous.close() }
            }
            rt.handler.post { session.open(adapter) }
            reqId
        } catch (e: Exception) {
            Wire.ERROR_GENERIC
        }
    }

    // Idempotente: sin sesión, 0. Con un connect pendiente, lo cancela (su RequestDone sale con -5).
    @JvmStatic
    fun disconnect(id: String?): Int {
        val rt = runtime ?: return Wire.OK
        val key = normalize(id) ?: return Wire.OK
        val session = rt.sessions.remove(key) ?: return Wire.OK
        session.markClosing()
        rt.keepAlive.stop(key)
        rt.handler.post { session.close() }
        return Wire.OK
    }

    @JvmStatic
    fun discover(id: String?): Int {
        val (rt, session) = live(id) ?: return Wire.ERROR_NOT_CONNECTED
        val reqId = nextRequestId()
        rt.handler.post { session.discover(reqId) }
        return reqId
    }

    @JvmStatic
    fun setNotify(id: String?, service: String?, characteristic: String?, enable: Boolean): Int {
        val (rt, session) = live(id) ?: return Wire.ERROR_NOT_CONNECTED
        val svc = Wire.parseUuid(service) ?: return Wire.ERROR_GENERIC
        val chr = Wire.parseUuid(characteristic) ?: return Wire.ERROR_GENERIC
        val reqId = nextRequestId()
        rt.handler.post { session.setNotify(reqId, svc, chr, enable) }
        return reqId
    }

    @JvmStatic
    fun write(id: String?, service: String?, characteristic: String?, data: ByteArray?, withResponse: Boolean): Int {
        val (rt, session) = live(id) ?: return Wire.ERROR_NOT_CONNECTED
        val svc = Wire.parseUuid(service) ?: return Wire.ERROR_GENERIC
        val chr = Wire.parseUuid(characteristic) ?: return Wire.ERROR_GENERIC
        val payload = data?.copyOf() ?: ByteArray(0)
        val reqId = nextRequestId()
        rt.handler.post { session.write(reqId, svc, chr, payload, withResponse) }
        return reqId
    }

    // Keepalive del segundo plano (DT10-09): lo arranca Core con InBackground y lo detiene al volver.
    @JvmStatic
    fun keepAliveStart(id: String?, service: String?, characteristic: String?, data: ByteArray?, periodMs: Int): Int {
        val (rt, _) = live(id) ?: return Wire.ERROR_NOT_CONNECTED
        val svc = Wire.parseUuid(service) ?: return Wire.ERROR_GENERIC
        val chr = Wire.parseUuid(characteristic) ?: return Wire.ERROR_GENERIC
        return rt.keepAlive.start(normalize(id)!!, svc, chr, data ?: ByteArray(0), periodMs)
    }

    @JvmStatic
    fun keepAliveStop(id: String?): Int {
        val rt = runtime ?: return Wire.OK
        val key = normalize(id) ?: return Wire.OK
        return rt.keepAlive.stop(key)
    }

    // ── Eventos (hilo de Unity) ──

    @JvmStatic
    fun pending(): Int = ring.pending()

    // Registros enteros, hasta 64 KiB (el búfer de drenaje de NativeBleAdapter); el resto, en el próximo.
    @JvmStatic
    fun drain(): ByteArray = ring.drain(Wire.MAX_RECORD_BYTES)

    // ── Adaptador y permiso ──

    // "Activar Bluetooth" (AC-BLE-04-04): el diálogo del sistema. Sin BLUETOOTH_CONNECT (API 31+) o con SecurityException ->
    // ERROR_PERMISSION y Core abre los ajustes de la app.
    @JvmStatic
    fun requestEnable(): Int {
        val context = context() ?: return Wire.ERROR_UNSUPPORTED
        if (Build.VERSION.SDK_INT >= 31 && !granted(context, Manifest.permission.BLUETOOTH_CONNECT)) {
            return Wire.ERROR_PERMISSION
        }
        return try {
            val intent = Intent(BluetoothAdapter.ACTION_REQUEST_ENABLE)
            val host = activity?.get()
            if (host != null) {
                host.startActivity(intent)
            } else {
                intent.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK)
                context.startActivity(intent)
            }
            Wire.OK
        } catch (e: SecurityException) {
            Wire.ERROR_PERMISSION
        } catch (e: Exception) {
            Wire.ERROR_GENERIC
        }
    }

    // API 31+: BLUETOOTH_SCAN y BLUETOOTH_CONNECT; 26-30: ACCESS_FINE_LOCATION.
    @JvmStatic
    fun hasScanPermission(): Boolean {
        val context = context() ?: return false
        return if (Build.VERSION.SDK_INT >= 31) {
            granted(context, Manifest.permission.BLUETOOTH_SCAN) && granted(context, Manifest.permission.BLUETOOTH_CONNECT)
        } else {
            granted(context, Manifest.permission.ACCESS_FINE_LOCATION)
        }
    }

    @JvmStatic
    fun shouldShowRationale(permission: String?): Boolean {
        val host = activity?.get() ?: return false
        if (permission.isNullOrEmpty()) {
            return false
        }
        return try {
            host.shouldShowRequestPermissionRationale(permission)
        } catch (e: Exception) {
            false
        }
    }

    // La negativa recordada (DT10-15): "rechazar y reabrir la app" sigue en la vista de permiso denegado.
    @JvmStatic
    fun wasDenied(): Boolean {
        val context = context() ?: return false
        return context.getSharedPreferences(PREFS, Context.MODE_PRIVATE).getBoolean(PREF_DENIED, false)
    }

    @JvmStatic
    fun setDenied(denied: Boolean) {
        val context = context() ?: return
        context.getSharedPreferences(PREFS, Context.MODE_PRIVATE).edit().putBoolean(PREF_DENIED, denied).apply()
    }

    @JvmStatic
    fun openAppSettings() {
        val context = context() ?: return
        try {
            val intent = Intent(Settings.ACTION_APPLICATION_DETAILS_SETTINGS, Uri.fromParts("package", context.packageName, null))
            intent.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK)
            context.startActivity(intent)
        } catch (e: Exception) {
        }
    }

    // Respuesta táctil al quedar "Listo" (A10-06, D10-11): CONFIRM en API 30+, CONTEXT_CLICK en 26-29. Sin el permiso VIBRATE.
    @JvmStatic
    fun haptic() {
        val host = activity?.get() ?: return
        try {
            host.runOnUiThread {
                try {
                    val constant = if (Build.VERSION.SDK_INT >= 30) HapticFeedbackConstants.CONFIRM else HapticFeedbackConstants.CONTEXT_CLICK
                    host.window?.decorView?.performHapticFeedback(constant)
                } catch (e: Exception) {
                }
            }
        } catch (e: Exception) {
        }
    }

    @JvmStatic
    fun sdkInt(): Int = Build.VERSION.SDK_INT

    // ── Ayudas privadas (no son parte de la API JNI) ──

    private fun context(): Context? = runtime?.context ?: activity?.get()?.applicationContext

    private fun granted(context: Context, permission: String): Boolean =
        context.checkSelfPermission(permission) == PackageManager.PERMISSION_GRANTED

    private fun nextRequestId(): Int {
        while (true) {
            val id = requestIds.incrementAndGet()
            if (id > 0) {
                return id
            }
            requestIds.compareAndSet(id, 0)
        }
    }

    // MAC en mayúsculas (DT10-08); null si no es una dirección.
    private fun normalize(id: String?): String? {
        val key = id?.trim()?.uppercase() ?: return null
        return if (BluetoothAdapter.checkBluetoothAddress(key)) key else null
    }

    private fun live(id: String?): Pair<BleRuntime, GattSession>? {
        val rt = runtime ?: return null
        val key = normalize(id) ?: return null
        val session = rt.sessions[key] ?: return null
        if (session.isClosing) {
            return null
        }
        return Pair(rt, session)
    }
}
