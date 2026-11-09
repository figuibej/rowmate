# El plugin de "Exportar TCX" de RowMate se llama por JNI desde C# (Platform/Export/AndroidFileExporter, por nombre): R8 no puede
# renombrar ni quitar sus clases ni sus metodos. RmExportProvider lo nombra el manifiesto.
-keep class com.rowmate.export.** { *; }
