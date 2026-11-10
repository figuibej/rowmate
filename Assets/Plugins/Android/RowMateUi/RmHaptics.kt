package com.rowmate.ui

import android.app.Activity
import android.os.Build
import android.view.HapticFeedbackConstants
import java.lang.ref.WeakReference

// Respuesta táctil de la interfaz (It. 14, DT14-24; AC-POL-03, AC-DEBT-10; ANIMATION_BIBLE §2.12). Sin manifiesto ni permisos:
// View.performHapticFeedback no pide el permiso de vibración y respeta el ajuste "respuesta táctil" del sistema (D10-11). No depende del Bluetooth
// (RmBle.haptic queda como está, por su golden). C# llama a bind(actividad) una vez (con la actividad de Unity: este archivo no
// conoce al reproductor) y después perform(tipo) en cualquier hilo; el toque va en el hilo de UI de la actividad.
// Tipos (iguales a Core/Ports/HapticKind): 0 Confirm, 1 Light (CLOCK_TICK), 2 Medium (VIRTUAL_KEY), 3 Success (CONFIRM en API 30+;
// antes, VIRTUAL_KEY + 120 ms + VIRTUAL_KEY). NO VERIFICABLE sin el módulo de Android y un teléfono.
object RmHaptics {
    const val CONFIRM = 0
    const val LIGHT = 1
    const val MEDIUM = 2
    const val SUCCESS = 3

    private const val SECOND_TAP_MS = 120L

    @Volatile
    private var activity: WeakReference<Activity>? = null

    @JvmStatic
    fun bind(host: Activity) {
        activity = WeakReference(host)
    }

    @JvmStatic
    fun isBound(): Boolean = activity?.get() != null

    // true si se pidió (el sistema decide si vibra según su ajuste).
    @JvmStatic
    fun perform(kind: Int): Boolean {
        val host = activity?.get() ?: return false
        try {
            host.runOnUiThread {
                try {
                    val view = host.window?.decorView ?: return@runOnUiThread
                    when (kind) {
                        LIGHT -> view.performHapticFeedback(HapticFeedbackConstants.CLOCK_TICK)
                        MEDIUM -> view.performHapticFeedback(HapticFeedbackConstants.VIRTUAL_KEY)
                        SUCCESS -> {
                            if (Build.VERSION.SDK_INT >= 30) {
                                view.performHapticFeedback(HapticFeedbackConstants.CONFIRM)
                            } else {
                                view.performHapticFeedback(HapticFeedbackConstants.VIRTUAL_KEY)
                                view.postDelayed({
                                    try {
                                        view.performHapticFeedback(HapticFeedbackConstants.VIRTUAL_KEY)
                                    } catch (e: Exception) {
                                    }
                                }, SECOND_TAP_MS)
                            }
                        }
                        else -> {
                            // Confirm: el mismo toque corto de "Listo" de la It. 10a (CONFIRM en API 30+, CONTEXT_CLICK antes).
                            val constant = if (Build.VERSION.SDK_INT >= 30) HapticFeedbackConstants.CONFIRM else HapticFeedbackConstants.CONTEXT_CLICK
                            view.performHapticFeedback(constant)
                        }
                    }
                } catch (e: Exception) {
                }
            }
        } catch (e: Exception) {
            return false
        }

        return true
    }
}
