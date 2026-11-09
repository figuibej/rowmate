// "Exportar TCX" en Android (It. 12; 12-exportar-tcx/02-tech-design §3.4, DT12-18; AC-STR-09-24, DV-122): la carpeta de la caché
// donde C# escribe el archivo y la hoja de compartir del sistema. Métodos @JvmStatic de un object: los llama
// Platform/Export/AndroidFileExporter por AndroidJavaClass (native/android/check.ps1 compara javap de esta clase con
// golden/export-signatures.txt: acá NO puede haber otro miembro público).
// - Sin dependencias (ni bibliotecas de soporte ni el reproductor de Unity): C# pasa la Activity. Sin permisos: el archivo queda en la caché
//   privada y RmExportProvider da el permiso de lectura TEMPORAL a la app elegida (FLAG_GRANT_READ_URI_PERMISSION).
// - Sin red: solo arma el Intent ACTION_SEND; lo que haga la app elegida es cosa del usuario.
package com.rowmate.export

import android.app.Activity
import android.content.ClipData
import android.content.Intent
import android.util.Log
import java.io.File

object RmExport {
    internal const val TAG = "RowMateExport"

    // La subcarpeta de la caché (la misma que sirve RmExportProvider).
    internal const val FOLDER = "exports"

    // cacheDir/exports (la crea si falta). La lee AndroidFileExporter una vez, en el hilo principal.
    @JvmStatic
    fun exportDir(activity: Activity): String {
        val dir = File(activity.cacheDir, FOLDER)
        if (!dir.isDirectory) {
            dir.mkdirs()
        }
        return dir.absolutePath
    }

    // Abre la hoja de compartir con el archivo (que tiene que estar en cacheDir/exports). false si el archivo no está o no es de
    // la carpeta; true si se pidió la hoja. El pedido corre en el hilo de UI de la actividad: un error ahí (ninguna app que
    // reciba el tipo, la actividad cerrándose) solo va al registro de Android (m-08, NO VERIFICABLE).
    @JvmStatic
    fun share(activity: Activity, path: String, mime: String, title: String): Boolean {
        val file = File(path)
        if (!file.isFile) {
            return false
        }

        val uri = RmExportProvider.uriFor(activity, file) ?: return false
        activity.runOnUiThread {
            try {
                val send = Intent(Intent.ACTION_SEND)
                send.type = mime
                send.putExtra(Intent.EXTRA_STREAM, uri)
                send.clipData = ClipData.newRawUri(file.name, uri)
                send.addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION)
                val chooser = Intent.createChooser(send, title)
                chooser.addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION)
                activity.startActivity(chooser)
            } catch (error: Exception) {
                Log.w(TAG, "no se pudo abrir la hoja de compartir: " + error.javaClass.simpleName)
            }
        }
        return true
    }
}
