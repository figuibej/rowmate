// El ContentProvider de "Exportar TCX" en Android (It. 12; 12-exportar-tcx/02-tech-design §3.4, DT12-18; AC-STR-09-24). Propio en
// lugar del FileProvider de las bibliotecas de soporte: no suma dependencias a Gradle. Autoridad ${applicationId}.rowmate.export (el manifiesto de
// RowMateExport.androidlib), exported=false y grantUriPermissions=true: solo la app elegida en la hoja de compartir, y solo
// mientras dura el permiso temporal del Intent.
// - Sirve SOLO archivos de cacheDir/exports, en una ruta de un segmento (content://<autoridad>/<archivo>), por su ruta canónica
//   (nada de "..", barras ni enlaces que salgan de la carpeta), y solo para leer.
// - query: OpenableColumns.DISPLAY_NAME y SIZE (lo que piden las apps de correo y de archivos para el nombre).
// - getType por la extensión (.tcx = application/vnd.garmin.tcx+xml).
// - insert, update y delete: UnsupportedOperationException.
package com.rowmate.export

import android.content.ContentProvider
import android.content.ContentValues
import android.content.Context
import android.database.Cursor
import android.database.MatrixCursor
import android.net.Uri
import android.os.ParcelFileDescriptor
import android.provider.OpenableColumns
import java.io.File
import java.io.FileNotFoundException

class RmExportProvider : ContentProvider() {
    companion object {
        private const val AUTHORITY_SUFFIX = ".rowmate.export"
        private const val TCX_EXTENSION = ".tcx"
        private const val TCX_MIME = "application/vnd.garmin.tcx+xml"
        private const val OTHER_MIME = "application/octet-stream"

        internal fun authority(context: Context): String = context.packageName + AUTHORITY_SUFFIX

        private fun root(context: Context): File = File(context.cacheDir, RmExport.FOLDER).canonicalFile

        // La Uri del archivo, o null si no está directamente en cacheDir/exports.
        internal fun uriFor(context: Context, file: File): Uri? {
            val canonical = file.canonicalFile
            if (canonical.parentFile != root(context) || !canonical.isFile) {
                return null
            }
            return Uri.Builder().scheme("content").authority(authority(context)).appendPath(canonical.name).build()
        }
    }

    override fun onCreate(): Boolean = true

    // El archivo de la Uri, o null si no es uno de cacheDir/exports.
    private fun fileFor(uri: Uri): File? {
        val context = context ?: return null
        if (uri.authority != authority(context)) {
            return null
        }

        val segments = uri.pathSegments
        if (segments.size != 1) {
            return null
        }

        val name = segments[0]
        if (name.isEmpty() || name == "." || name == ".." || name.contains('/') || name.contains('\\')) {
            return null
        }

        val root = root(context)
        val file = File(root, name).canonicalFile
        if (file.parentFile != root || !file.isFile) {
            return null
        }
        return file
    }

    override fun openFile(uri: Uri, mode: String): ParcelFileDescriptor? {
        if (mode != "r") {
            throw SecurityException("RmExportProvider: solo lectura")
        }

        val file = fileFor(uri) ?: throw FileNotFoundException("RmExportProvider: no está")
        return ParcelFileDescriptor.open(file, ParcelFileDescriptor.MODE_READ_ONLY)
    }

    override fun query(uri: Uri, projection: Array<out String>?, selection: String?, selectionArgs: Array<out String>?, sortOrder: String?): Cursor? {
        val file = fileFor(uri) ?: return null
        val wanted = projection ?: arrayOf(OpenableColumns.DISPLAY_NAME, OpenableColumns.SIZE)
        val columns = ArrayList<String>()
        val values = ArrayList<Any>()
        for (column in wanted) {
            if (column == OpenableColumns.DISPLAY_NAME) {
                columns.add(column)
                values.add(file.name)
            } else if (column == OpenableColumns.SIZE) {
                columns.add(column)
                values.add(file.length())
            }
        }

        val cursor = MatrixCursor(columns.toTypedArray(), 1)
        cursor.addRow(values.toTypedArray())
        return cursor
    }

    override fun getType(uri: Uri): String {
        val name = uri.lastPathSegment ?: return OTHER_MIME
        return if (name.endsWith(TCX_EXTENSION, ignoreCase = true)) TCX_MIME else OTHER_MIME
    }

    override fun insert(uri: Uri, values: ContentValues?): Uri? {
        throw UnsupportedOperationException("RmExportProvider: solo lectura")
    }

    override fun update(uri: Uri, values: ContentValues?, selection: String?, selectionArgs: Array<out String>?): Int {
        throw UnsupportedOperationException("RmExportProvider: solo lectura")
    }

    override fun delete(uri: Uri, selection: String?, selectionArgs: Array<out String>?): Int {
        throw UnsupportedOperationException("RmExportProvider: solo lectura")
    }
}
