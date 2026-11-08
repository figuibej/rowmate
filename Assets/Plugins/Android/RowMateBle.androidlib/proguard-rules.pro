# El plugin de Bluetooth de RowMate se llama por JNI desde C# (Platform/Ble/AndroidBleApi, con los nombres de
# Core/Ble/Native/RmBleJniSignatures): R8 no puede renombrar ni quitar sus clases ni sus metodos.
-keep class com.rowmate.ble.** { *; }
