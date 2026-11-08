# native/android — el plugin de Bluetooth LE de Android

El plugin de Android de la It. 10a ([02-tech-design §4](../../docs/migration/iterations/10a-ble-android-windows/02-tech-design.md)): fuentes Kotlin en [`Assets/Plugins/Android/RowMateBle/`](../../Assets/Plugins/Android/RowMateBle/) que compila el Gradle de Unity, y una biblioteca [`RowMateBle.androidlib`](../../Assets/Plugins/Android/RowMateBle.androidlib/) con el manifiesto de permisos (AC-APP-13-01) y la regla de R8 que conserva `com.rowmate.ble`. Unity lo usa por JNI crudo (`Platform/Ble/AndroidBleApi`, `AndroidBlePermissions`, `Platform/Haptics/AndroidHaptics`). Esta carpeta solo tiene la verificación sin el módulo de Unity.

## Qué hace

- **`RmBle`** (`object` con `@JvmStatic`): las mismas funciones que la API C de la DLL de Windows (`init`, `startScan`, `connect`, `discover`, `setNotify`, `write`, `keepAliveStart/Stop`, `pending`, `drain`, …) más el permiso (`hasScanPermission`, `shouldShowRationale`, `wasDenied`/`setDenied`, `openAppSettings`), la vibración (`haptic`) y `sdkInt`. No depende del reproductor de Unity: C# le pasa la `Activity` en `init`.
- **Hilos**: un `HandlerThread` para el GATT (`connectGatt` recibe su `Handler`, así los *callbacks* llegan al mismo hilo que la cola), otro para el keepalive del segundo plano. Ningún hilo entra a C#: escriben en **`EventRing`** (4 MiB, formato v1 de `Wire.kt`, el mismo de `BleWire.cs` y de la DLL) y Unity lo drena cada cuadro.
- **Búsqueda** (`ScanSession`): filtro FTMS del sistema y `LOW_LATENCY`; un evento por equipo nuevo o con cambios, y el RSSI como mucho 1 vez/s. Sin permiso, código `-2` (Core lo trata como negativa, no como error).
- **Conexión** (`GattQueue.kt`): `connectGatt(TRANSPORT_LE)` + `requestMtu(512)`; una operación GATT a la vez con tope de 10 s; como mucho una sesión por id (un `connect` nuevo cierra la anterior y `disconnect` cancela uno pendiente con `-5`); `disconnect()` + `close()` siempre. Nunca empareja ni desempareja.
- **Adaptador** (`AdapterMonitor`): `getState()` y `ACTION_STATE_CHANGED`; sin BLE, `Unsupported`. Kotlin nunca emite `Unauthorized`: el permiso lo decide Core.
- **Reloj**: `System.nanoTime()` (la base del `Stopwatch` de la app).

## Verificar sin el módulo de Unity

```
powershell -NoProfile -File native/android/check.ps1 -Golden [-OutDir <scratch>/android-check]
```

Encuentra Android Studio (`$env:ANDROID_STUDIO` o `Program Files\Android\Android Studio`: su JBR y el `kotlinc` del plugin de Kotlin) y el SDK (`$env:ANDROID_HOME`, `$env:ANDROID_SDK_ROOT` o `%LOCALAPPDATA%\Android\Sdk`) sin rutas fijas, y no instala nada:

1. Revisa las fuentes: ni emparejado ni cambios de la radio ni el reproductor de Unity (código 3).
2. Compila todo el Kotlin contra el `android.jar` más nuevo con `-Werror` (código 1 si falla).
3. Compara `javap -s` de `RmBle` con [`golden/jni-signatures.txt`](golden/jni-signatures.txt) y con `Core/Ble/Native/RmBleJniSignatures.cs` (código 4). `-Update` reescribe el archivo cuando cambia la API (junto con la tabla de C#).
4. `-Golden`: corre [`golden/EmitFixture.kt`](golden/EmitFixture.kt) en la JVM (con `Wire.kt` y `EventRing.kt`, que no usan `android.*`) y compara su salida con `Assets/RowMate/Tests/Golden/ble/events-v1.bin` byte a byte (código 5); también prueba el descarte del anillo.

En dotnet, `AndroidManifestTests` revisa el manifiesto (permisos exactos), la biblioteca, que `ProjectSettings` siga en API 26 y ARM64 y la higiene del Kotlin, y `JniSignatureTests` la tabla de C# contra `jni-signatures.txt`.

El APK, los permisos en el teléfono, la vibración, el segundo plano y la pantalla encendida quedan NO VERIFICABLE hasta instalar "Android Build Support": los pasos están en [tools/unity/README.md](../../tools/unity/README.md) (sección "It. 10a: el plugin de Android").
