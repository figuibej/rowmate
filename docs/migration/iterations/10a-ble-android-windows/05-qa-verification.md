# It. 10a — Bluetooth real en Android y Windows: verificación de QA

| Campo | Valor |
|---|---|
| Autor | rowmate-qa |
| Fecha | 2026-11-08 |
| Entrada | [01-po-spec](01-po-spec.md) (43 AC), [02-tech-design](02-tech-design.md), [03-qa-gate](03-qa-gate.md), [04-design-notes](04-design-notes.md), [snapshots/](snapshots/) (31 tomas) |
| Árbol | el repo de trabajo, sin worktrees ni copias. Legacy [rowmate-legacy](https://github.com/figuibej/rowmate-legacy) (`../rowmate-legacy`): `git status --porcelain` vacío antes y después |
| Artefactos | `<scratch>/it10a/qa-r1` (dotnet), `<scratch>/it10a/qa-r1-unity` (Unity), `<scratch>/it10a/qa-r1-build` (builds, carpetas de datos temporales, registros de los players), `<scratch>/it10a/qa-r1-android` y `qa-r1-probe` |
| Hardware | Adaptador Bluetooth LE de la PC, **encendido y sin tocar**: no se apagó ni se prendió la radio, no se emparejó ni se conectó nada. El remo no estuvo conectado |

## Veredicto: **APROBADO CON OBSERVACIONES**

No hay bloqueantes ni mayores. Quedan **37 AC en PASS, 0 en FAIL y 6 NO VERIFICABLE**: los [BLE-HW] necesitan el remo y los [Device-Android] necesitan el módulo "Android Build Support". Los 6 menores van a issues.

## 1. Corridas (salida real)

| Suite | Resultado |
|---|---|
| `run-tests.ps1 -Matrix` (dotnet 8) | **3 632 tests: 3 593 pasan, 39 omitidos, 0 fallan.** Herramientas: 192 de 192. Matriz: PASA 772, FALLA 0, IGNORADO 4, PENDIENTE UNITY 165, SIN TEST 6. Los 6 sin test son AC-BLE-04-07, -05-10, -06-08, -08-07, -09-03 y AC-APP-14-05, todos de dispositivo o de hardware (§3) |
| Unity EditMode | **3 736: 3 688 pasan, 48 ignorados, 0 fallan** (`WindowsDllContractTests` 5/5, `JniSignatureTests` 3/3, `AndroidManifestTests` 5/5, `NativeBleAdapterTests` 19/19). El puente C# de Android compila en el Editor de Windows (DT10-19) |
| Unity EditMode `-Simulator` | **3 928: 3 880 pasan, 48 ignorados, 0 fallan** |
| Unity PlayMode | **622 de 622** (`DeviceBleTests` 17, `HudLinkNoticeTests` 8, `BleAllocationTests` 3, `DeviceBleArtTests` 6, `HudBudgetTests` 11) |
| Unity PlayMode `-Simulator` | **697 de 697** |
| Guardia de `ProjectSettings` | "sin cambios" en las 4 corridas y en los 3 builds. `ProjectSettings.asset` queda igual byte a byte que antes de empezar |
| `native/android/check.ps1 -Golden` | 0. Las 7 fuentes Kotlin compilan contra android-36 con `-Werror` (kotlinc 2.3.10). Las firmas JNI, 24 métodos, coinciden con `jni-signatures.txt` y con `RmBleJniSignatures.cs`. Kotlin escribe `events-v1.bin` idéntico (456 B) |
| `native/windows/build.ps1 -Check` | 0. SDK 10.0.26100 y MSVC 14.51. La DLL recompilada pesa 340 480 B, no tiene rutas y es **idéntica** a la versionada (SHA-256 `C1AB7BE6…6BA3`) |
| `tools/ble-probe --selftest` ×3 | 0 las tres veces: `estado=On`, `ftms=0`, `pulso=0`, 4-5 equipos cercanos `descartados` (no se muestra nada de ellos), `fallas=0`, `formato=ok` |
| `build.ps1 -Target Windows` (normal) | 0 (Mono, release). `RowMateBle.dll en el build`; `BuildOutputInspector` no encontró rastros del simulador, de diagnósticos ni de tests |
| `build.ps1 -Target Windows -BleSmoke` | 0: `[BLE] humo: estado=On busquedas=3 ftms=0 fallas=0 colgado=no soloFtms=si` (la segunda búsqueda, con la ventana minimizada) |
| `build.ps1 -Target Windows -Simulator` | 0 (Development) en `Builds/Windows/simulator` |

## 2. Prueba en los builds (carpetas de datos temporales)

1. **Build normal** (W1280, `ROWMATE_DATA_DIR` temporal, `-logFile` en `<scratch>`): abre en la **vista de búsqueda** con el estado vacío y la tarjeta del pulsómetro. No muestra la vista de apagado ni la de sin soporte. El registro dice `[BLE] Adapter state: On` y `[BLE] Remo real (plugin nativo, adaptador On)`, sin excepciones. Se cerró con `CloseMainWindow` y salió con 0. La radio no cambió.
2. **Búsqueda real**: en el humo hubo 3 búsquedas de 10 s seguidas, con la segunda minimizada, sin colgarse y sin fallas. La lista solo tuvo FTMS (0). `ble-probe` confirma que los equipos cercanos que no son FTMS ni pulsómetro se descartan sin mostrarse. Ningún flujo llama a `connect`: en la DLL y en el Kotlin no hay `PairAsync`, `createBond`, `removeBond`, `SetStateAsync` ni `RequestAccessAsync` (grep y `AndroidManifestTests.KotlinNeverPairsNorTouchesTheRadio`). Los equipos ya emparejados no se tocaron.
3. **Build de simulador** (`-rowmate-perf-entries 2`, base temporal): una sesión libre y una rutina (`Medición de la entrada`) entran y salen del inmersivo con la escena `Ready`, el remero y 0 excepciones. `RowMate.SessionList --db` lista **2 sesiones guardadas** (libre y rutina), igual que en la 9.
4. **"Activar Bluetooth" en Windows** (AC-BLE-04-04): no se ejercitó en el build porque haría falta apagar la radio, y eso está prohibido. Se revisó en el código: `WindowsBleApi.RequestEnable` → `Application.OpenURL("ms-settings:bluetooth")` (`WindowsBleApi.cs:142`), nunca un error (`RSC.RequestEnableDelegates`).

## 3. Matriz AC → test → resultado

| AC | Resultado | Evidencia |
|---|---|---|
| AC-BLE-04-01 | PASS | `RSS.AdapterOffSetsStatusOff`, `ConnectedThenBluetoothOff…`, `NBA.AdapterEventsUpdateTheState`; PlayMode `DeviceBleTests`; CAP-01a/b, CAP-02 |
| AC-BLE-04-02 | PASS | `NBA.DeniedIsUnauthorized`, `NotAskedIsNotUnauthorized`, `RSS.PermissionDeniedIsNotABanner`; CAP-03 |
| AC-BLE-04-03 | PASS | `DRB.OtherStatesShowScan`; AUX-10a-02 (la transformación de 0 a 720 ms); `DBA` con reducir movimiento |
| AC-BLE-04-04 | PASS (Win por código) · Android NO VERIFICABLE | `NBA.RequestEnable*` (4); `ms-settings:bluetooth` (§2.4) |
| AC-BLE-04-05 | PASS | `RSS.WaitsFiveSecondsForOn`, `AdapterNeverOnSetsOffWithoutMessage` |
| AC-BLE-04-06 | PASS | `RSS.UnsupportedDoesNothing`, `DRB.UnsupportedHasNoButton`, `NBA.OtherApiVersionIsUnsupported`; CAP-04 |
| AC-BLE-04-07 | PASS [Device-Win] | build normal en la vista de búsqueda y `Adapter state: On` (§2.1); el humo con `estado=On` |
| AC-APP-13-01 | PASS (texto del `.androidlib`) · APK NO VERIFICABLE | `AMT.ExactPermissions`; igual a `am:4-19` del legacy línea por línea |
| AC-APP-13-02 | NO VERIFICABLE [Device-Android] | la lógica pasa en dotnet (8 tests `NBA`/`RSS`); el diálogo en el teléfono, no |
| AC-APP-13-03 | PASS | `CMP.NormalUsesRowerService`; Unity `-Simulator` en verde; el build de simulador hace libre y rutina (§2.3); el build normal usa el plugin |
| AC-BLE-05-04 | PASS | `RSS.TenSecondsFtmsOnly`, `ClearsErrorAndList`; CAP-05 (m-03) |
| AC-BLE-05-05 | PASS | `RSS.K10OrderDedupFirstSeen`, `ClientSideFilterDropsHeadphones`, `OnlyThisScanCounts`; CAP-06 |
| AC-BLE-05-06 | PASS | `RSS.EndsToDisconnectedUnlessConnecting`, `ListSurvivesConnectDisconnect`, `OldScanDoesNotClobberNew` |
| AC-BLE-05-07 | PASS | `DRB.ScanItemsNamesAddressesAndSteps`; CAP-06, -07, -08 (estado vacío) |
| AC-BLE-05-08 | PASS | `RSS.ScanFailureResetsStatus`, `AsyncScanFailureEndsTheScanWithABanner`; CAP-08 (m-04) |
| AC-BLE-05-09 | PASS | PlayMode `DeviceBleTests` (LT-29 y LT-31 con `RowerService`); CAP-07a/b (LT-30) |
| AC-BLE-05-10 | PASS [Device-Win] | el humo: 3 búsquedas, `ftms=0`, `fallas=0`, `colgado=no`, `soloFtms=si`; `ble-probe` ×3 |
| AC-BLE-06-01 | PASS | `RSC.ConnectingAndFifteenSecondCap`, `NBA.TimeoutCancelsNativeConnect`, `LateConnectAfterTimeoutIsClosed`; CAP-10 |
| AC-BLE-06-02 | PASS | `RSC.DiscoversAndLogs`, `BikeConnectsWithZeroMetricsNoWatchdog` |
| AC-BLE-06-03 | PASS | `RSC.ControlZeroThen300msThenSeven`, `WriteFailureContinues` (QA-G10a-07, paridad `bs:265-276`), `NoControlPointNoWrites` |
| AC-BLE-06-04 | PASS | `RSC.KeepAliveThirtyInSixtySeconds`, `KeepAliveStopsOnAnyDisconnect` |
| AC-BLE-06-05 | PASS | `RSC.ConnectedAfterHandshakeZeroesData`, `DSB.DataPublishedToApp`; CAP-13a/b |
| AC-BLE-06-06 | PASS | `RSC.TimeoutBannerClearedOnRetry`; CAP-12 |
| AC-BLE-06-07 | PASS · vibración NO VERIFICABLE | `DSB.PhasesAndHapticOnce`, `DeviceBleTests` (pasos y retención); CAP-09, -10, -11 |
| AC-BLE-06-08 | NO VERIFICABLE [BLE-HW] | demo del usuario |
| AC-BLE-07-05 | PASS | `RSC.ManualForgetsNoReconnect`; `DeviceBleTests` |
| AC-BLE-11-12 | PASS | `DRB.HeaderNameFallbacks`; CAP-13 |
| AC-BLE-08-02 | PASS | `RRC.FirstAttemptAtThreeSecondsTenSecondCap`, `NBA.UnrequestedDisconnectRaisesLinkLost` |
| AC-BLE-08-03 | PASS | `RRC.RetriesForeverEveryThreeSecondsAfterFailure`, `NBA.LinkLostClosesSession`, `RetriesDoNotLeakSessions` |
| AC-BLE-08-04 | PASS | `RRC.CancelledByDisconnectScanConnect`, `WatchdogAndManualNeverReconnect`, `AdapterOffThenOnReconnects` |
| AC-BLE-08-05 | PASS | `DRB.ReconnectBannerEnablesSearchBetweenAttempts`; CAP-14 (m-01) |
| AC-BLE-08-06 | PASS | `HLN.*` (8, los cinco perfiles sin tapar métricas), `HudBudgetTests` (59); CAP-15a/b, CAP-16, AUX-10a-01 |
| AC-BLE-08-07 | NO VERIFICABLE [BLE-HW] | demo del usuario |
| AC-BLE-09-01 | PASS | `RWD.FiresAtFiveWholeSeconds`, `NeverWithoutPacket`, `StopsOutsideConnected`, `RBG.TenSecondSilenceFiresAtItsVirtualInstant` |
| AC-BLE-09-02 | PASS | `DSB.MonitorStoppedRetrySameDevice`; `HLN.MonitorStoppedFourSeconds`; CAP-17, -18 |
| AC-BLE-09-03 | NO VERIFICABLE [BLE-HW] | demo del usuario (pausa de 10 s) |
| AC-BLE-12-01 | PASS | `DRG.ThreeDragsOver20`, `LongPressOpensPanel`, `PanelResetsOnReconnect` |
| AC-BLE-12-02 | PASS | `DSB.DiagnosticsLifetimeCounterAndHex`, `BHX.*`, `DRB.DebugPanelSpec`; CAP-19a/b, CAP-20 (en inglés, `(sin datos)`, hex en minúscula) |
| AC-BLE-91-01 | PASS | `RBG.NativeKeepAliveThirtyInSixty`, `SixtySecondsWithPacketsNoWatchdog`, `ResumeDrainDoesNotRecalibrate`, `BEC.*` |
| AC-BLE-91-02 | NO VERIFICABLE (parte Win: PASS) | el humo minimizado buscando no se cuelga; el teléfono bloqueado y el remo real necesitan dispositivo |
| AC-APP-14-05 | NO VERIFICABLE [Device-Android] | la regla ya la cubre `ScreenKeepAwake` de la 4a |
| AC-WKT-03-11 | PASS | `SDZ.*` (5), `MonitorCadenceStepTests.DistanceEveryTwoSecondsEndsTheStepAt20` (paso y parciales); los goldens de la 4a y la 5 en verde |
| AC-BLE-SEC-01 | PASS | `RSS.HeadphonesNeverListedOrConnected`, `RSC.RejectsUnknownId`, `BleSupportTests.AddressesAreMasked`, `AMT.KotlinNeverPairsNorTouchesTheRadio`, `NHY`; la revisión del repo público (§5) |

## 4. Revisión estática

- **`RowerService` contra `ble_service.dart` y `device_provider.dart`**: los nueve tiempos coinciden con el legacy. El permiso se pide antes de la espera de 5 s. La lista sale en el orden del primer descubrimiento, con reemplazo en el lugar. Si falla `0x00`, no se espera ni se escribe `0x07`, y el keepalive se arma igual (paridad). Al pasar a `Connected`, `_lastPacketAt` vuelve a null (`dp:65`). Apagar el Bluetooth con el remo conectado se trata como una caída. Todas las continuaciones comparan su generación. No encontré ningún escenario que rompa un AC.
- **Kotlin contra las APIs de Android**:
  - `connectGatt(…, TRANSPORT_LE, PHY_LE_1M_MASK, handler)` es API 26 y coincide con el mínimo.
  - `requestMtu(512)` tiene un tope de 10 s.
  - `writeCharacteristic` y `writeDescriptor` usan las firmas de API 33 con respaldo.
  - `onCharacteristicChanged` cubre los dos casos sin duplicar: la variante vieja solo con `SDK_INT < 33`.
  - `disconnect()` y `close()` se llaman siempre. Así se evita el agotamiento de clientes GATT, que lleva al error 133.
  - `ACTION_STATE_CHANGED` usa `RECEIVER_NOT_EXPORTED` en API 33+. Es válido para las emisiones del sistema.
  - La búsqueda usa `ScanFilter` del sistema y `LOW_LATENCY`.
  - Los permisos coinciden con los de `am:4-19`. R8 queda cubierto con `-keep class com.rowmate.ble.**`.
  - Solo queda el desvío de m-02.
  - Que el Gradle de Unity tome los `.kt` (R-01) y `UnityPlayer.currentActivity` en Unity 6 quedan NO VERIFICABLE hasta instalar el módulo.
- **Hilos**: ningún hilo nativo entra a C#. `AppRoot.Update` drena primero `MainThreadQueue`, después la bomba y después `Tick`. Al volver del segundo plano, la bomba drena con el desfase congelado **antes** de `ResumeFromBackground`. Los permisos de Android vuelven por `MainThreadQueue`.
- **Asignaciones**: `BleAllocationTests` (FrameAllocations) da 0 B por cuadro con el sonar, la caja de reconexión y el aviso del HUD. La bomba no llama a `Drain` con `Pending() == 0` (`NBA`).

## 5. Repo público, terceros, codificación y legacy

- `git grep --untracked` (case-sensitive) no encuentra rutas de esta PC, el usuario, el nombre del equipo, el scratchpad ni la marca del adaptador. Las únicas MAC del árbol son las ficticias `AA:BB:CC:…` de K-10. Las capturas usan solo K-10.
- `RowMateBle.dll`, en ASCII, UTF-16 y UTF-16 desfasado, no contiene `C:\`, `\Users\`, `Windows Kits`, `Program Files`, `Microsoft Visual Studio`, `.pdb`, `.cpp`, el usuario ni el nombre del equipo. La DLL es reproducible (§1). Los registros de las pruebas en los builds quedaron en `<scratch>` y no se versionan.
- No se suman terceros nuevos. JetBrains Mono (OFL, con su `OFL.txt`) ya estaba versionada; el `FontAsset` es generado. El C++, el Kotlin y C# son propios, y los encabezados de C++/WinRT no se copian.
- Los 292 archivos nuevos o modificados (sin binarios) son UTF-8 válido, sin BOM y sin doble codificación (`Ã`, `â€`). Hay 0 coincidencias.
- `.gitattributes`: `*.bin binary` y la DLL va sin LFS, como decidió el diseño.
- El legacy quedó con `git status --porcelain` vacío. No se leyó el módulo de credenciales.
- Al terminar no quedó ningún proceso mío: los players se cerraron, Unity terminó y los servidores de dotnet se apagaron.

## 6. Capturas (31 tomas, revisadas una por una)

Coinciden con la spec y el arte:

- **Vistas de estado** (CAP-01 a -04): ilustración de 64 en rojo y título en 18 negrita. Sin Bluetooth LE, el ícono va en gris, sin botón y sin pulsómetro.
- **Búsqueda** (CAP-05): sonar, ayuda y botón deshabilitado.
- **Lista** (CAP-06): `AMS-670B`, `Dispositivo desconocido` y `Bici FTMS`, una vez cada uno y sin `Auriculares`.
- **Errores** (CAP-08, CAP-12): banner rojo.
- **Pasos** (CAP-09 a -11): `Conectando…`, `Tomando control del monitor…` con todos los ítems con spinner, y `Listo` con el check.
- **Conectado** (CAP-13): tablero con el paquete de prueba.
- **Avisos del enlace**: la reconexión en Dispositivo (CAP-14) y sobre el HUD (CAP-15 y -16). El aviso va en el `#toast`, encima de Terminar/Pausa, sin tapar métricas, también en el peor caso de la 4b (AUX-10a-01).
- **Monitor apagado** (CAP-17, -18): naranja con `Reintentar`.
- **Diagnóstico** (CAP-19, -20): en inglés y mono, `RECEIVING` / `NO DATA`, `(sin datos)`.

Lo que no cierra va en m-01, m-03 y m-06.

## 7. Issues

| ID | Sev. | Resp. | Issue | Evidencia | Corrección |
|---|---|---|---|---|---|
| m-01 | menor | designer | En TV, la caja de reconexión corta el **nombre** del remo, `Reconectando con AMS-67…`, porque `Desconectar` le quita ancho. Con letra 1,3× se corta más. DT10-14 pedía que el corte fuera solo de nombres de más de 24 caracteres | CAP-14; `Base.uss:2815-2821` (`white-space: nowrap`, `text-overflow: ellipsis`) | Permitir 2 líneas en `.rm-link-banner__text`, o pasar `Desconectar` debajo cuando no entra |
| m-02 | menor | architect | En Android, `TURNING_ON` y `TURNING_OFF` se informan como `Off`. Mientras el sistema enciende la radio después de `Activar Bluetooth`, se ve `Bluetooth apagado` en lugar de la búsqueda, y AC-BLE-04-03 dice "encendiendo → búsqueda". El enum ya tiene `TurningOn` y `TurningOff` | `AdapterMonitor.kt:86-90`; `BleAdapterState.cs` | Mapear a `TurningOn` y `TurningOff` (`Wire` 4 y 6); la espera de 5 s ya los cubre |
| m-03 | menor | designer | Mientras busca, el botón deshabilitado muestra el spinner y `Buscando...` con muy poco contraste sobre el cian apagado. La spec y el legacy piden un spinner **blanco** de 16 | CAP-05; `ds:398-402` | Spinner blanco y etiqueta al 60-70 % en el estado de búsqueda (no en el deshabilitado común) |
| m-04 | menor | architect | El banner repite la operación: `Error al escanear: scan: error 2`. `BleException.Message` ya lleva `{op}:` | CAP-08; 02-tech-design §2.1 | Pasar al banner solo el detalle, o no anteponer `{op}` en el mensaje visible |
| m-05 | menor | architect | Con una `RowMateBle.dll` de otra versión, el remo queda en `UnavailableRowerService`, sin mensaje para el usuario. El diseño decía "estado `Unsupported` y registro" | `BleAdapterFactory.cs:59-64`; 02-tech-design §2.6 | Crear `NativeBleAdapter` en `Unsupported`, así se ve la vista de CAP-04, o anotar el desvío |
| m-06 | menor | designer | En TH, la lista queda en una franja de ≈ 1,5 tarjetas con desplazamiento propio, y la tarjeta del pulsómetro se corta contra la barra inferior. La auditoría dice "sin desbordes", pero se ve apretado | CAP-07b | Con alto compacto, un único desplazamiento para la lista y la tarjeta, sin altura fija para la lista |

## 8. Para la demo del usuario (NO VERIFICABLE)

- **Remo real** (demo de Windows de la spec §1, pasos 2 a 8): AC-BLE-06-08, -08-07, -09-03 y la parte [BLE-HW] de -91-02. Anotar la cadencia de `BleCadence` en el `Player.log`, que no se sube.
- **Android**: AC-APP-13-01 (manifiesto fusionado con `aapt2`), -13-02, AC-BLE-04-04, la vibración de -06-07, -91-02 y AC-APP-14-05. Antes hay que instalar "Android Build Support" (`tools/unity/README.md` §It. 10a) y confirmar que el Gradle de Unity toma los `.kt` (R-01).
