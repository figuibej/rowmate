# It. 10a — Gate de QA (armonía spec ↔ diseño ↔ arte)

| Campo | Valor |
|---|---|
| Autor | rowmate-qa |
| Fecha | 2026-11-08 |
| Entrada | [01-po-spec](01-po-spec.md) (43 AC), [02-tech-design](02-tech-design.md), [00-arte-bluetooth](00-arte-bluetooth.md) |
| Legacy verificado | [rowmate-legacy](https://github.com/figuibej/rowmate-legacy) `3ae0f3a` (`../rowmate-legacy`): `lib/core/bluetooth/ble_service.dart`, `scan_filter.dart`, `lib/features/device/device_screen.dart`, `device_provider.dart`, `android/app/src/main/AndroidManifest.xml`, `lib/features/workout/workout_provider.dart:82-86`. `git status --porcelain` vacío antes y después |
| Línea base | `run-tests.ps1 -ArtifactsPath <scratch>/it10a/gate`: **3500 tests, 3461 pasan, 39 omitidos, 0 fallan** |
| Veredicto | **APROBADO CON OBSERVACIONES**: sin bloqueantes; 5 mayores que se corrigen al implementar (WP-A, WP-B1 y WP-C), sin rehacer el diseño |

## 1. Lo que está bien (verificado contra el código)

- **Tiempos**: los nueve de `RowerTimings.Legacy` coinciden con el legacy: 10 s (`bs:65`), 5 s (`bs:77`), 15 s (`bs:144`), 10 s (`bs:178`), 300 ms (`bs:269`), 2 s (`bs:280`), 3 s (`bs:168`), 1 s (`dp:71`) y 5 s (`dp:8`, `:75`, `inSeconds` ≥ 5).
- **Paridad**: el legacy hace un solo intento de reconexión (`_cleanup` cancela el periódico, `bs:196`, `:318`). Sin Control Point no hay keepalive (`bs:261-264`, antes de armar el timer). La lista sale en el orden del primer descubrimiento y reemplaza el ítem en su lugar (`bs:107`). El fin de la búsqueda baja el estado solo si sigue en `scanning` (`bs:121`). La vista de estado depende del **adaptador**, no del estado del remo (`ds:346-347`), y `DeviceScreenRules.ViewFor(status, adapter)` lo respeta. Tras el plazo de 5 s, el remo queda `Off` pero la vista sigue en búsqueda (AC-BLE-04-05).
- **Textos**: las 12 claves son nuevas (ninguna existe hoy); el catálogo tiene hoy 228 claves, así que quedan 240. `deviceScanError` y `deviceConnectError` salen de `dp:111` y `:125`, y `Rower` y `Unknown device` de `ds:98` y `:452`. El diagnóstico queda en inglés, igual que en `ds:142-218`.
- **Cobertura**: los 43 AC tienen un componente y un test (`d`, `e` o `cap`) o figuran como NO VERIFICABLE con su motivo (§8.3 del diseño). No falta ningún AC.
- **Contradicciones resueltas en el diseño**: el título va en 18 negrita (spec) y no en 20/600 (arte); el aviso del HUD va en el `#toast` (spec, "sin sumar otro") y no en un elemento propio dentro de `#immersive` (arte).
- **Factibilidad en esta PC**: hay SDK 10.0.26100 con C++/WinRT 2.0.250303.1, Android Studio con `plugins/Kotlin/kotlinc` y JBR, y `platforms/android-36`. En `ProjectSettings` ya están `runInBackground: 1`, `AndroidMinSdkVersion: 26` y `AndroidTargetArchitectures: 2`.
- **Hilos**: modelo *pull*, ningún hilo nativo entra a C#; WinRT corre solo en hilos MTA propios; la respuesta de los permisos de Android pasa por `MainThreadQueue`. Correcto.
- **Reglas de Bluetooth**: `ConnectAsync` solo acepta ids conocidos (DT10-04). Se prohíben `SetStateAsync` y `RequestAccessAsync`. `-BleSmoke` y `ble-probe` nunca conectan. Las direcciones del registro se enmascaran. Las capturas usan solo `K-10`.

## 2. Issues

| ID | Sev. | Resp. | Issue | Evidencia | Corrección |
|---|---|---|---|---|---|
| QA-G10a-01 | mayor | architect | La DLL con C++/WinRT en `/std:c++20` *release* guarda la **ruta absoluta** de cada archivo fuente (`__builtin_FILE()` en `slim_source_location`). Incluye los encabezados del SDK (`C:\Program Files (x86)\Windows Kits\…`) y los `.cpp` del clon (`…\Users\<usuario>\…`). La revisión de `build.ps1` corta con código 2 y WP-B1 no puede cerrar; si se saltea esa revisión, la ruta de la PC termina en el repo público | `Include/10.0.26100.0/cppwinrt/winrt/base.h:200-216` (rama *release*: `WINRT_IMPL_BUILTIN_FILE __builtin_FILE()`); 02-tech-design §3 | Compilar con `/DWINRT_NO_SOURCE_LOCATION`; nada de `__FILE__` ni `assert` en el código propio; mantener la revisión de rutas (ASCII y UTF-16) y sumarle `\Windows Kits\` |
| QA-G10a-02 | mayor | architect | Android: `init` marca `Unauthorized` **si todavía no se pidió el permiso**. En la primera instalación, Dispositivo muestra `Permiso de Bluetooth denegado` en lugar de la búsqueda (o de `Bluetooth apagado`). Además, `RowerService.StartScanAsync` espera 5 s a `On` **antes** de llamar al adaptador, que es quien pide el permiso, así que el diálogo de "Dispositivos cercanos" nunca aparece al tocar `Buscar`. Y en API 31+, `ACTION_REQUEST_ENABLE` necesita `BLUETOOTH_CONNECT`: sin pedirlo antes, lanza `SecurityException` | 02-tech-design §4.2 (`init`: "sin permiso → Unauthorized"), §2.2 (el plazo de 5 s antes de llamar al adaptador), §4.2 `requestEnable`; contra AC-APP-13-02, AC-BLE-04-04 y los pasos 1 y 2 de la demo de Android | Leer el estado real con `BluetoothAdapter.getState()` (en API 31+ no pide permiso). Dar `Unauthorized` **solo después de una negativa** (recordarla en el proceso; al volver al frente, releer `checkSelfPermission` y `shouldShowRequestPermissionRationale`). `RequestEnableAsync` en API 31+ pide primero `BLUETOOTH_CONNECT` y después lanza `ACTION_REQUEST_ENABLE`; si recibe `SecurityException`, abre los ajustes de la app, nunca un error. Tests `d` con `FakeBleAdapter`: primer arranque sin pedir el permiso → vista de búsqueda, `Buscar` → se pide el permiso, negado → `Unauthorized` |
| QA-G10a-03 | mayor | architect | **Conexiones nativas huérfanas.** Cuando vencen los 15 s (o los 10 s de un reintento) antes de que `adapter.ConnectAsync` devuelva el periférico, `peripheral?.DisconnectAsync()` no hace nada y el pedido nativo sigue vivo. Con `LinkLost`, "limpia" no cierra la sesión nativa. En Windows, con `MaintainConnection = true`, el sistema sigue conectándose solo; con reintentos sin límite cada ~13 s se acumulan sesiones y colas GATT con un hilo por equipo, y un monitor FTMS que acepta una sola conexión puede rechazar la nueva. El legacy (FBP) desconecta al vencer el plazo | 02-tech-design §2.2 (fila `ConnectAsync`: "vence → `peripheral?.DisconnectAsync()`"; fila `LinkLost`), §3 (`MaintainConnection = true`, una cola GATT por equipo); DT10-03 | `NativeBleAdapter` cancela **por id**: llama `api.Disconnect(id)` al vencer el plazo, ante `LinkLost` y ante un `RequestDone` tardío de una generación superada. `rm_ble_connect` / `connect` sobre un id que ya tiene sesión primero la cierra. `rm_ble_disconnect` cancela un connect pendiente. Tests `NBA.TimeoutCancelsNativeConnect`, `NBA.LateConnectAfterTimeoutIsClosed`, `NBA.LinkLostClosesSession` y `NBA.RetriesDoNotLeakSessions` (60 s de reintentos → ≤ 1 sesión viva en `FakeNativeBleApi`) |
| QA-G10a-04 | mayor | architect | **Reloj de los eventos al volver del segundo plano (AC-BLE-91-01).** El desfase se mide en cada drenaje, pero en el drenaje de la vuelta `scheduler.MonotonicSeconds` todavía es la hora del último cuadro antes de la pausa (`FrameScheduler`: la hora es la del cuadro y el alcance todavía no corrió). Ese desfase corre todos los eventos hacia atrás lo que duró la pausa, `max(0, t − now)` los aplica juntos al principio y el watchdog dispara aunque los paquetes hayan seguido llegando. Además, Kotlin usa `elapsedRealtimeNanos` (cuenta el sueño profundo) y `UnityScheduler` usa el `Stopwatch` (`CLOCK_MONOTONIC`, no lo cuenta), y la elección de RA-19, que la 4a dejó a cargo de la 10a, no aparece. Toda esa cuenta vive en `Platform/Ble/BleEventPump`, sin dotnet, y los tests `RBG.*` usan `FakeBleAdapter.EnqueueAt`, que se saltea la bomba | 02-tech-design §5.2, §4.2 ("Reloj: `SystemClock.elapsedRealtimeNanos()`"); `Core/Common/FrameScheduler.cs:82`; `Platform/Time/UnityScheduler.cs` (comentario RA-19); ARCHITECTURE RA-19; 04a 02-tech-design §826 | Pasar la conversión y el reagendado a Core (`BleEventClock` o dentro de `NativeBleAdapter`). **Congelar el desfase** del último drenaje en primer plano y no medirlo en el drenaje de la vuelta. Que el reloj nativo tenga la misma base que el de la app: en Android, `System.nanoTime()` (`CLOCK_MONOTONIC`) mientras RA-19 no cambie la fuente de `UnityScheduler`, y anotar la decisión de RA-19 en una línea. Tests `d` con `FakeNativeBleApi` + `FakeScheduler`: 60 s de pausa con un `Notify` por segundo no disparan el watchdog, y 10 s de silencio dentro de la pausa sí lo disparan, en su instante virtual |
| QA-G10a-05 | mayor | architect | El puente C# de Android (`AndroidBleApi`, `AndroidBlePermissions`, `AndroidHaptics`) no se compila nunca en esta PC si va entero bajo `#if UNITY_ANDROID`: no está el módulo de Android. Sus firmas JNI tampoco se comparan con el Kotlin. El primer error aparecería en el APK del usuario | 02-tech-design §1.3, §4.3, §9 ("bajo `#if UNITY_ANDROID` en `BleAdapterFactory`") | Compilar esos archivos en todas las plataformas (`AndroidJNI` y `UnityEngine.Android.Permission` están en `UnityEngine.AndroidJNIModule`, que existe también en el Editor de Windows). El `#if` queda **solo** en la selección de `BleAdapterFactory`. Agregar a `check.ps1` un `javap -s` de `RmBle` y compararlo con las firmas que declara C# (una tabla de constantes) |

### Menores (una línea cada uno)

| ID | Resp. | Issue → corrección |
|---|---|---|
| QA-G10a-06 | po | AC-APP-13-01 dice "declara **exactamente**", pero el APK de Unity suma otros permisos (p. ej. `INTERNET` para Strava). Precisar: "los de Bluetooth y ubicación, exactamente estos". |
| QA-G10a-07 | architect | AC-BLE-06-03 en paridad: si falla `0x00`, el legacy **no espera los 300 ms ni escribe `0x07`** (un solo `try`, `bs:265-276`) y arma el keepalive igual. Fijarlo en §2.3 y en `RSC.WriteFailureContinues`, con los dos casos. |
| QA-G10a-08 | architect | Falta la regla de `ScanFailed` (el `onScanFailed` asíncrono de Android). El legacy solo lo registra (`bs:111`). Definir qué hace `RowerService` y cómo cubre AC-BLE-05-08 en Android. |
| QA-G10a-09 | architect | `RECEIVING` / `NO DATA` usa el contador de toda la vida de la app (`notificationCount > 0`, `ds:177`). Fijarlo en `BleDebugSpec`; CAP-20 necesita que `K-BIKE` sea la primera conexión de la corrida. |
| QA-G10a-10 | architect | Watchdog: el legacy borra la hora del último paquete al pasar a `connected` (`dp:65`), así que no cuentan los paquetes que llegan durante la toma de control. Hacer lo mismo en `RowerService` (§2.4). |
| QA-G10a-11 | architect | Con el Bluetooth apagado y el remo conectado, no depender solo del `LinkLost` nativo (R-03): tratar `Off` como caída en Core y sumar un test de AC-BLE-04-01 "conectado + apagar → vista de estado". |
| QA-G10a-12 | architect | AC-WKT-03-11 pide el paso **y** los parciales a la vez: sumar un test del motor (no solo de `SanitizedDistance`) con +11 m cada 2 s → fin del paso en el segundo 20 y parcial coherente. |
| QA-G10a-13 | architect | El *buffer* de 2 MB alcanza para ≈ 3,5-4 h a 2 Hz, no para 6 h: un `Notify` ocupa ≈ 70 B y se suma el `RequestDone` del keepalive. Corregir el texto o usar 4 MB. |
| QA-G10a-14 | architect | `EventRing.kt` lee `SystemClock`: en la JVM, `EmitFixture` falla con "Stub!". Inyectar la hora y dejar `Wire.kt` y `EventRing.kt` sin `android.*`. |
| QA-G10a-15 | architect | Windows emite todos los equipos cercanos y su RSSI 1 vez/s aunque A10-02 quedó rechazado: no emitir cambios que sean solo de RSSI en la 10a (menos tráfico y menos asignaciones en el hilo principal). |
| QA-G10a-16 | architect | AC-BLE-SEC-01: dejar explícito en la DLL y en Kotlin que nunca se usan `PairAsync`, `createBond` ni `removeBond`, y sumarlo al grep de `NHY`. |
| QA-G10a-17 | architect | `MainThreadQueue`: decir quién lo drena (`AppRoot.Update`, antes de la bomba y del `Tick`) y testearlo. |
| QA-G10a-18 | architect | §12 dice "hoy se relee al buscar", pero con `Unsupported` el remo vuelve sin llamar al adaptador y la vista no tiene botón: nunca se relee. Corregir el texto del issue. |
| QA-G10a-19 | architect | El manifiesto del `.androidlib` usa `tools:targetApi`, que necesita `xmlns:tools`. Que `AMT` lo valide. |
| QA-G10a-20 | architect | `-Check` por SHA-256 depende de la versión exacta de MSVC y del SDK: documentar que un cambio de *toolset* obliga a recompilar y versionar la DLL. |
| QA-G10a-21 | designer | El 00-arte §3.1 usa `deviceEnableSettings`, que no existe ni está entre las 12 claves (§10: "el arte no agrega claves"). Sacarlo o que el PO lo sume (241). |
| QA-G10a-22 | designer | El arte quedó desactualizado frente al diseño: el título en 20/600 (va 18 negrita), el aviso propio del HUD con franja A/B y 60 elementos (va el `#toast`, 59) y el marcador `RowMate.Hud.LinkNotice` (va `RowMate.Toast.LinkNotice`). Reflejarlo en `04-design-notes.md`. |
| QA-G10a-23 | architect | `events-v1.bin` y sus fijaciones: usar solo ids de `K-10` (`AA:BB:CC:…`), nunca una dirección escaneada. |

## 3. Matriz AC → test (resumen del gate)

| Grupo | AC | Test planificado | Estado en el gate |
|---|---|---|---|
| Adaptador | AC-BLE-04-01 a -07 | `RSS`, `DRB`, `DBT`, `RSC.RequestEnableDelegates`, `SMK`; CAP-01 a -04 | Cubierto; -01 suma el caso "conectado + apagar" (QA-G10a-11); -04 depende de QA-G10a-02 en Android |
| Permisos | AC-APP-13-01 a -03 | `AMT`, `RSS.PermissionDeniedIsNotABanner`, `CMP.*` | -02 queda NO VERIFICABLE en el teléfono; sus tests `d` se agregan con QA-G10a-02 |
| Búsqueda | AC-BLE-05-04 a -10 | `RSS.*`, `DBT.*`, LT-29 a LT-31, `SMK`; CAP-05 a -08 | Cubierto; QA-G10a-08 para el fallo asíncrono |
| Conexión | AC-BLE-06-01 a -08 | `RSC.*`, `DSB.*`, `DBT.*`; CAP-09 a -12 | Cubierto; QA-G10a-03 y QA-G10a-07; -08 NO VERIFICABLE [BLE-HW] |
| Desconexión y cabecera | AC-BLE-07-05, AC-BLE-11-12 | `RSC.ManualForgetsNoReconnect`, `DBT`, `DRB`; CAP-13 | Cubierto |
| Reconexión | AC-BLE-08-02 a -07 | `RRC.*`, `DBT`, `HLN.*`, `HudBudgetTests`; CAP-14 a -16 | Cubierto; QA-G10a-03; -07 NO VERIFICABLE [BLE-HW] |
| Watchdog | AC-BLE-09-01 a -03 | `RWD.*`, `DSB`, `HLN`; CAP-17, -18 | Cubierto; QA-G10a-10; -03 NO VERIFICABLE [BLE-HW] |
| Diagnóstico | AC-BLE-12-01, -02 | `DRG`, `DBT`, `DSB`, `BHX`; CAP-19, -20 | Cubierto; QA-G10a-09 |
| Segundo plano | AC-BLE-91-01, -02, AC-APP-14-05 | `RBG.*`, `SMK`, `DBT.FocusLossKeepsConnectedFake` | -01 necesita los tests de la bomba real de QA-G10a-04; el resto NO VERIFICABLE en el teléfono y [BLE-HW] |
| Distancia | AC-WKT-03-11 | `SDZ.*` y los goldens de la 4a y la 5 | Le falta el test del motor (QA-G10a-12) |
| Seguridad | AC-BLE-SEC-01 | `RSS.HeadphonesNeverListedOrConnected`, `RSC.RejectsUnknownId`, `NHY` | Cubierto; QA-G10a-01 y QA-G10a-16 |

## 4. Para issues

- Si el AMS-670B no notifica en las pausas o no pone sus contadores en cero, revisarlo con `BleCadence`; ya está en la spec y en el diseño (§12).
- Un adaptador de Windows conectado después de abrir la app: hoy hay que reiniciar la app (ver QA-G10a-18).
