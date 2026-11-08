# It. 10a — Bluetooth real en Android y Windows: diseño técnico

| Campo | Valor |
|---|---|
| Autor | unity-architect |
| Fecha | 2026-11-08 |
| Iteración | 10a · `10a-ble-android-windows` ([ROADMAP §It. 10a](../../ROADMAP.md)) |
| Entrada | [01-po-spec](01-po-spec.md) (43 AC, CAP-01 a -20, DV-96 a -105); [00-arte-bluetooth](00-arte-bluetooth.md) (D10-01 a -13, A10-01 a -13); ADR [005](../../architecture/adr/005-hilos-y-asincronia.md), [009](../../architecture/adr/009-ble-nativo.md), [017](../../architecture/adr/017-segundo-plano.md); [ARCHITECTURE §6, §10, §19](../../architecture/ARCHITECTURE.md) |
| Legacy leído (solo lectura, `3ae0f3a`) | [rowmate-legacy](https://github.com/figuibej/rowmate-legacy) (`../rowmate-legacy`): `lib/core/bluetooth/ble_service.dart` (entero), `lib/features/device/device_provider.dart`. No se leyó el módulo de credenciales. `git -C ../rowmate-legacy status --porcelain` vacío |
| Código leído (este repo) | `Core/Ble/*` (`IRowerService`, `BleAdapterState`, `BleScanResult`, `ScanFilter`, `FtmsUuids`, `Unavailable/*`), `Core/Common/{FrameScheduler,IScheduler}.cs`, `Core/Ports/IAppLifecycle.cs`, `Core/Workout/SanitizedDistance.cs`, `App/Device/{DeviceStore,DeviceState,DeviceScreenRules,DeviceViewKind}.cs`, `App/Composition/{DeviceBackend,AppContext}.cs`, `Bootstrap/{DeviceBackendFactory,HardwareBackend,AppRoot}.cs`, `Platform/Time/UnityScheduler.cs`, `Platform/Power/WakeLockFactory.cs`, `Infrastructure/Threading/*`, `Presentation/Device/{DeviceScreenView,ScanView,ConnectedView}.cs`, `Presentation/Shell/ToastLayer.cs`, `Presentation/Workout/WorkoutScreenView.cs`, `Tests/PlayMode/HudBudgetTests.cs`, `Tests/UiSupport/HostBackends.cs`, `Snapshots/SnapshotCatalog.cs`, `tools/build/build.ps1` |
| Estado | Diseño compacto, **sin código**. Orden: **WP-A** → (**WP-B1** ∥ **WP-C**) → **WP-D** → **WP-B2** (§9) |

Convenciones: rutas relativas a `Assets/RowMate/` salvo `Assets/Plugins/`, `native/`, `tools/` y `docs/`. **N** nuevo, **C** cambia. `bs` = `ble_service.dart`, `dp` = `device_provider.dart`, `ds` = `device_screen.dart`.

---

## 0. Resumen

1. **Toda la lógica es Core puro** (`RowMate.Core.Ble`, dotnet): `RowerService` porta `bs` línea por línea (búsqueda 10 s, espera del adaptador 5 s, conexión 15 s, descubrimiento, `0x00` → 300 ms → `0x07`, keepalive 2 s, reconexión 3 s / 10 s sin límite, watchdog 1 s / ≥ 5 s) sobre el puerto **`IBleAdapter`** (nuevo; el de ARCHITECTURE §10.2, afinado). Se testea con **`FakeBleAdapter`** + `FakeScheduler` y los equipos ficticios `K-10`.
2. **El puente nativo también es Core**: `NativeBleAdapter` implementa `IBleAdapter` e `IBleEventPump` sobre `INativeBleApi` (funciones planas) y un **formato binario de eventos** común (§2.6) que Windows (C++) y Android (Kotlin) escriben igual. Platform solo tiene el P/Invoke o el JNI. Modelo *pull* del ADR 009: cada cuadro, `AppRoot` llama `pump.Drain`, que drena el *buffer* nativo **sin asignar** si no hay eventos; la conversión de hora y el reagendado del alcance son Core (`BleEventClock`, §5.2; QA-G10a-04). Ningún hilo nativo entra a C#. El permiso de Android también es lógica de Core (`IBlePermissions`, §2.1; QA-G10a-02).
3. **Windows**: `RowMateBle.dll` C++/WinRT, con hilos MTA propios (uno de adaptador y una cola GATT por equipo), compilada por `native/windows/build.ps1` (vswhere + `Launch-VsDevShell`, `/Brepro`, sin PDB) y versionada en `Assets/Plugins/Windows/x86_64/`.
4. **Android**: fuentes Kotlin en `Assets/Plugins/Android/RowMateBle/` (las compila el Gradle de Unity) y un `.androidlib` **solo con el manifiesto** (permisos de AC-APP-13-01), así no se toca `ProjectSettings`. La compilación de Kotlin se **verifica** con el `kotlinc` y el JBR de Android Studio contra `android.jar`; el APK y el teléfono quedan NO VERIFICABLE.
5. **Selección**: el único `#if ROWMATE_SIMULATOR` sigue en `DeviceBackendFactory`. El build normal usa `RowerService` + adaptador nativo (Windows y Android); en otras plataformas, el `UnavailableRowerService` de la It. 2 (hasta la 10b). `ROWMATE_BLE=off` lo fuerza en tests y arnés. `build.ps1 -BleSmoke` corre el player y busca con el adaptador real **sin conectar** (AC-BLE-04-07, -05-10).
6. **HUD**: el aviso del enlace **reutiliza el `#toast` del `ToastLayer`** (manda la spec, AC-BLE-08-06: "sin sumar otro"), con un modo fijo para `Reconectando…` y un indicador de 1 elemento. No se agrega nada bajo `#immersive`: el peor caso del HUD sigue en 59.
7. **QA-V4a-L01**: `SanitizedDistance` mide el tope de 10 m/s **desde el último cambio de distancia**; el paso y los parciales lo heredan (un solo saneador por sesión). `BleCadence` deja en el registro la cadencia real del AMS-670B para la demo.
8. **Herramienta manual**: `tools/ble-probe` (consola .NET 8 sobre la misma DLL) busca y lista **solo** FTMS (`0x1826`) y pulsómetros (`0x180D`), con la dirección enmascarada salvo `--show`. Nunca conecta.

### 0.1 Decisiones del Architect (una línea cada una)

| ID | Decisión |
|---|---|
| DT10-01 | El watchdog vive en `RowerService` (Core) y no en `DeviceStore` como en el legacy (`dp:69-86`): el resultado es el mismo y se testea con la hora de cada paquete (necesario para el alcance, AC-BLE-91-01). El simulador no lo tiene: manda cada segundo mientras está conectado, así que da lo mismo |
| DT10-02 | La reconexión agenda **3 s después de cada fallo** (AC-BLE-08-03) en lugar del periódico de 3 s de `bs:168`. Con el adaptador sin `On`, el intento se saltea y se reagenda a +3 s (`Waiting`): da lo mismo que DV-97 y no hace parpadear el estado |
| DT10-03 | El tope de 15 s (y 10 s en la reconexión) cubre solo `connect`, como `bs:144` y `:178`. Cada operación GATT nativa tiene su propio tope de 10 s en el plugin: nunca queda un pedido colgado |
| DT10-04 | `ConnectAsync` solo acepta un id que vino como resultado FTMS en esta sesión de la app o el último remo conectado; cualquier otro lanza `ArgumentException` (AC-BLE-SEC-01) |
| DT10-05 | El registro enmascara las direcciones (`AA:BB:…:70`, `BleIds.Mask`); los nombres de los equipos no FTMS nunca llegan a registrarse porque se descartan antes |
| DT10-06 | El aviso de "monitor apagado" recuerda el id del remo en `DeviceStore` (el servicio lo olvida, `bs:308`) para `Reintentar` |
| DT10-07 | Windows escanea activo y **sin filtro**, y la DLL **fusiona** el anuncio con la respuesta de escaneo por dirección (el nombre suele venir en la respuesta y el UUID en el anuncio); Core filtra (`ScanFilter`, `sf:12-20`) |
| DT10-08 | Las direcciones son el id opaco: MAC en mayúsculas `AA:BB:CC:DD:EE:FF` en las dos plataformas (como `remoteId` de FBP) |
| DT10-09 | Sin keepalive nativo en Windows (el *loop* sigue minimizado, `runInBackground`); en Android, `Handler` en un `HandlerThread`, activado por Core solo con `InBackground` (ADR 017) |
| DT10-10 | Permisos de Android con `UnityEngine.Android.Permission`; su respuesta pasa al hilo principal por `Core/Common/MainThreadQueue` (nuevo: `Infrastructure/Threading` solo tiene el ejecutor de la base, y Platform no lo referencia) |
| DT10-11 | Kotlin sin dependencia de `UnityPlayer`: C# le pasa la `Activity` en `init`, así compila contra `android.jar` solo y se verifica sin el módulo de Unity |
| DT10-12 | Sin servicio en primer plano en Android (spec §7, ADR 017): queda para cuando la demo muestre que el sistema mata el proceso |
| DT10-13 | El título de la vista de estado va en **18 negrita** (manda AC-BLE-04-01 sobre D10-06) |
| DT10-14 | `{name}` se corta a 24 caracteres con `…` en Core (`LinkNoticeText`), no midiendo: el texto fijo nunca se corta |
| DT10-15 | Android: `Unauthorized` **solo después de una negativa** (QA-G10a-02). El estado del adaptador sale de `BluetoothAdapter.getState()`, que no pide permiso en API 31+. La negativa se recuerda en un `SharedPreferences` propio del plugin (`rm_ble`, clave `permission_denied`), así "rechazar y reabrir la app" (demo de Android, paso 2) sigue mostrando la vista de AC-BLE-04-02; no es el `prefs.json` de la app |
| DT10-16 | El permiso se pide **antes** de la espera de 5 s a `On` (`EnsureScanPermissionAsync`). En el legacy, FBP lo pedía dentro de `startScan`, después de la espera; acá el estado no depende del permiso, así que con `On` da lo mismo, y con el orden del legacy el diálogo nunca aparecería (QA-G10a-02) |
| DT10-17 | Cancelar una conexión es **por id** en el adaptador: vencer el plazo, `LinkLost` y un `RequestDone` tardío de una generación superada llaman `api.Disconnect(id)`; en lo nativo, `connect` sobre un id con sesión primero la cierra y `disconnect` cancela un `connect` pendiente (QA-G10a-03; FBP desconecta al vencer el plazo) |
| DT10-18 | Reloj de los eventos (RA-19, QA-G10a-04): la DLL usa QPC y Kotlin `System.nanoTime()` (`CLOCK_MONOTONIC`), la misma base que el `Stopwatch` de `UnityScheduler`. **Decisión de RA-19 para la 10a: se queda el `Stopwatch`** (no cuenta el sueño profundo); si la medición en el teléfono pide `elapsedRealtime`, cambian juntos `UnityScheduler` y el reloj inyectado de `EventRing`. La medición queda NO VERIFICABLE [Device-Android] |
| DT10-19 | El puente C# de Android se compila en **todas** las plataformas (`UnityEngine.AndroidJNIModule` está en el Editor de Windows); el `#if UNITY_ANDROID` queda solo en `BleAdapterFactory` (QA-G10a-05) |

---

## 1. Árbol de archivos

### 1.1 WP-A — Core y App puros (dotnet)

```
Core/Ble/
  IBleAdapter.cs  IBlePeripheral.cs                    N  puerto (§2.1)
  BleGattService.cs  BleCharacteristic.cs  BleCharacteristicProperties.cs  BleNotification.cs  N
  BleException.cs  BlePermissionDeniedException.cs     N
  IBlePermissions.cs  BlePermissionState.cs            N  permiso de Android (DT10-15)
  IBleEventPump.cs                                     N  lo llama AppRoot (§5.2)
  RowerService.cs  RowerTimings.cs                     N  puerto de bs (§2.2-§2.5)
  RowerLink.cs                                         N  fase de conexión, reconexión y motivo (A10-01)
  BleHex.cs  BleIds.cs  BleCadence.cs                  N  hex del diagnóstico, máscara, cadencia (§7)
  IRowerService.cs                                     C  + Link y RequestEnableAsync con implementación por defecto
  Native/INativeBleApi.cs  NativeBleAdapter.cs  NativePeripheral.cs  N  puente puro (§2.6)
  Native/BleWire.cs  BleEventReader.cs                 N  formato binario de eventos
  Native/BleEventClock.cs                              N  desfase congelado y reagendado del alcance (§5.2)
  Native/RmBleJniSignatures.cs                         N  tabla de firmas JNI (§4.3; la compara check.ps1)
Core/Common/MainThreadQueue.cs                         N  (DT10-10)
Core/Ports/IHaptics.cs                                 N  (A10-06)
Core/Workout/SanitizedDistance.cs                      C  QA-V4a-L01 (§7)
Core/Localization/L10nKeys.cs                          C  12 claves
Localization/strings.es.json  strings.en.json          C  240 por idioma
App/Device/DeviceState.cs  DeviceStore.cs  DeviceScreenRules.cs  DeviceViewKind.cs  ScanViewSpec.cs  C
App/Device/BluetoothStateSpec.cs  ScanItemSpec.cs  LinkBannerSpec.cs  BleDebugSpec.cs  N
App/Device/BleDiagnostics.cs  LinkNotice.cs  LinkNoticeText.cs  DebugRevealGesture.cs  N
App/Composition/AppComposer.cs  AppServices.cs         C  IHaptics
Tests/Support/FakeBleAdapter.cs  FakeBlePeripheral.cs  FakeBleDevices.cs  FakeNativeBleApi.cs  FakeBlePermissions.cs  FakeHaptics.cs  N
Tests/EditMode/Ble/*  Device/*  Workout/SanitizedDistanceTests.cs  Traceability/It10aIds.cs  N/C
Tests/Golden/ble/events-v1.bin  events-v1.md           N  fijación del formato (§2.6)
```

### 1.2 WP-B — Windows, integración, vistas, arnés y build

```
native/windows/RowMateBle/{rm_ble.h, rm_ble.cpp, event_ring.h, adapter_worker.cpp, device_queue.cpp, wire.h}  N
native/windows/build.ps1  README.md                     N  (§3)
Assets/Plugins/Windows/x86_64/RowMateBle.dll (+ .meta: Editor Win64 + Standalone Win64)  N
Platform/Ble/WindowsBleApi.cs  BleAdapterFactory.cs   N  (la bomba es Core: IBleEventPump de NativeBleAdapter)
Platform/Haptics/HapticsFactory.cs  NoHaptics.cs        N
Bootstrap/HardwareBackend.cs  DeviceBackendFactory.cs  AppRoot.cs  C   (§5)
Bootstrap/BleSmoke.cs                                   N  -rowmate-ble-smoke
Presentation/Device/DeviceScreenView.cs  ScanView.cs  ConnectedView.cs  DeviceHeaderView.cs  C
Presentation/Device/BluetoothStateView.cs  ScanResultList.cs  ScanResultCard.cs  LinkBanner.cs  ErrorBanner.cs  BleDebugPanel.cs  N
Presentation/Shell/ToastLayer.cs                        C  modo del enlace (§6.3)
Presentation/Workout/WorkoutScreenView.cs               C  avisos del enlace sobre el HUD
Snapshots/BleCatalog.cs  SnapshotCatalog.cs             N/C  conjunto it10a
Tests/UiSupport/HostBackends.cs                         C  FakeBle(...)
Tests/PlayMode/DeviceBleTests.cs  HudLinkNoticeTests.cs  BleAllocationTests.cs  N; FourA…AC-WKT-03-10  C
Tests/EditMode/Ble/WindowsDllContractTests.cs           N  (solo Windows)
tools/ble-probe/{BleProbe.csproj, Program.cs, NativeMethods.cs}  N  (§8.4)
tools/build/build.ps1  tools/unity/run-unity-tests.ps1  tools/unity/capture-snapshots.ps1  tools/unity/README.md  C
```

### 1.3 WP-C — Android

```
Assets/Plugins/Android/RowMateBle/RmBle.kt  EventRing.kt  Wire.kt  GattQueue.kt  AdapterMonitor.kt  ScanSession.kt  KeepAlive.kt  N
Assets/Plugins/Android/RowMateBle.androidlib/{AndroidManifest.xml, build.gradle, project.properties}  N
Platform/Ble/AndroidBleApi.cs  AndroidBlePermissions.cs  N  sin #if: se compilan en todas las plataformas (DT10-19)
Platform/Haptics/AndroidHaptics.cs                       N  ídem
native/android/check.ps1  README.md  golden/EmitFixture.kt  golden/jni-signatures.txt  N  (§4.5)
Tests/EditMode/Ble/AndroidManifestTests.cs  JniSignatureTests.cs  N
```

### 1.4 WP-D — Arte

```
Core/Design/BleMotionSpecs.cs                           N  constantes de §1-§5 del arte
Core/Design/Tokens.cs  tools/tokens/Program.cs  Presentation/Styles/{Typography,Tokens}.uss  C  token mono (A10-10)
Presentation/Styles/Skin.uss  Motion.uss                C
Presentation/Device/SonarView.cs  BluetoothIllustration.cs  N
Presentation/Controls/LinkIndicator.cs                  N  arco / spinner / check / link_off en una malla
Presentation/Motion/BleMotion.cs                        N  coreografías y reducir movimiento
Editor/Bootstrap/FontAssetPaths.cs (+ bootstrapper)     C  FontAsset de Mono
docs/…/10a-ble-android-windows/04-design-notes.md       N
```

---

## 2. Core (WP-A)

### 2.1 Puerto `IBleAdapter` (ARCHITECTURE §10.2, afinado)

```csharp
public interface IBleAdapter : IDisposable
{
    IReadOnlyObservable<BleAdapterState> State { get; }       // Unsupported = sin BLE (Q-47)
    bool FiltersScanInOs { get; }                             // Windows false → ScanFilter en la app
    bool NeedsNativeKeepAlive { get; }                        // Android true (DT10-09)
    IEventSource<BleScanResult> ScanResults { get; }          // solo del escaneo en curso; sin reemitir cacheados
    IEventSource<BleException> ScanFailed { get; }
    Task<bool> EnsureScanPermissionAsync();                   // Windows/sin permisos: true; Android: lo pide si hace falta (DT10-16)
    Task StartScanAsync(IReadOnlyList<BleUuid> services);     // reemplaza el escaneo en curso; no pide permiso
    Task StopScanAsync();
    Task RequestEnableAsync();                                // Android: permiso → diálogo, o ajustes de la app; Windows: ms-settings:bluetooth
    Task<IBlePeripheral> ConnectAsync(string deviceId, bool requestHighMtu, CancellationToken ct);
    // sin tope propio: el plazo lo pone Core y lo aplica con ct. Cancelado → api.Disconnect(id) y OperationCanceledException (DT10-17)
}

// Core/Ble: el permiso de Android como lógica pura (DT10-15). Windows y el simulador no lo tienen (null).
public enum BlePermissionState { Granted, NotAsked, Denied, DeniedForever }
public interface IBlePermissions
{
    BlePermissionState State { get; }                         // NotAsked nunca da Unauthorized
    Task<BlePermissionState> RequestAsync();                  // respuesta por MainThreadQueue (DT10-10)
    void Refresh();                                           // al volver al frente: checkSelfPermission + rationale
    void OpenAppSettings();
}
// NativeBleAdapter.State = Unauthorized si permisos ∈ {Denied, DeniedForever}; si no, el estado nativo (getState()).

public interface IBlePeripheral : IDisposable
{
    string DeviceId { get; }
    string? Name { get; }
    IEventSource<BleException> LinkLost { get; }              // caída no pedida (bs:148-153)
    IEventSource<BleNotification> Notifications { get; }
    Task<IReadOnlyList<BleGattService>> DiscoverServicesAsync();
    Task SetNotifyAsync(BleUuid service, BleUuid characteristic, bool enable);
    Task WriteAsync(BleUuid service, BleUuid characteristic, byte[] data, bool withResponse);
    Task DisconnectAsync();
    void StartNativeKeepAlive(BleUuid service, BleUuid characteristic, byte[] data, TimeSpan period);
    void StopNativeKeepAlive();
}

public readonly struct BleNotification { BleUuid Characteristic; byte[] Value; double MonotonicSeconds; }
[Flags] public enum BleCharacteristicProperties { Read = 1, Write = 2, WriteNoResponse = 4, Notify = 8, Indicate = 16 }
public sealed record BleCharacteristic(BleUuid Uuid, BleCharacteristicProperties Properties);
public sealed record BleGattService(BleUuid Uuid, IReadOnlyList<BleCharacteristic> Characteristics);
public class BleException : Exception { string Operation; int Code; }      // Message = "{op}: {detalle}"
public sealed class BlePermissionDeniedException : BleException { }        // DV-100: no es banner
```

### 2.2 `RowerService : IRowerService` (puerto de `bs`)

```csharp
public sealed class RowerService : IRowerService
{
    public RowerService(IBleAdapter adapter, IScheduler scheduler, IAppLifecycle lifecycle, IAppLogger logger,
                        RowerTimings? timings = null);
    // IRowerService (sin cambios) + Link + RequestEnableAsync
    public IReadOnlyObservable<RowerLink> Link { get; }
}
public sealed record RowerTimings(TimeSpan Scan, TimeSpan AdapterWait, TimeSpan Connect, TimeSpan ReconnectConnect,
    TimeSpan ControlGap, TimeSpan KeepAlive, TimeSpan ReconnectDelay, TimeSpan WatchdogPeriod, TimeSpan WatchdogThreshold)
{ public static readonly RowerTimings Legacy = new(10 s, 5 s, 15 s, 10 s, 300 ms, 2 s, 3 s, 1 s, 5 s); }   // bs:65,77,144,178,269,280,168; dp:8,71

public enum ConnectPhase { None, Connecting, TakingControl, Ready }
public enum ReconnectPhase { None, Waiting, Attempting }
public enum DisconnectReason { None, Manual, Watchdog, Lost, ConnectFailed }
public sealed record RowerLink(string? DeviceId, string? DeviceName, ConnectPhase Phase, ReconnectPhase Reconnect,
                               double? NextAttemptAt, DisconnectReason LastDisconnect)
{ public static readonly RowerLink Idle; }
```

`IRowerService` suma `IReadOnlyObservable<RowerLink> Link => RowerLinkSource.Idle;` y `Task RequestEnableAsync() => Task.CompletedTask;` como miembros con implementación por defecto (como `IAppLifecycle.Woke`): el simulador y `Unavailable` no cambian.

| Paso | Comportamiento (fuente) | AC |
|---|---|---|
| Adaptador | Refleja `adapter.State`; `Off` → `Status = Off` (`bs:47-54`). Con el remo conectado, `Off` también llega como `LinkLost` → la reconexión espera el `On` (DT10-02) | 04-01, 08-04 |
| `StartScanAsync` | `Unsupported` → vuelve sin hacer nada (`bs:66-67`; la vista lo muestra, Q-47). `await adapter.EnsureScanPermissionAsync()` **antes** de la espera (DT10-16): `false` → `Status = Disconnected`, sin banner ni relanzar (el adaptador ya pasó a `Unauthorized`, DV-100). Sin `On`: espera hasta 5 s a `On`; si vence, `Status = Off` y vuelve (`bs:69-84`). Cancela la reconexión (Q-29, DV-96). `Status = Scanning`; nueva **generación** de búsqueda; `found` ordenado por primer descubrimiento con reemplazo en el lugar (`bs:87`, `:107`); `ScanFilter.Accept(r, FTMS, !adapter.FiltersScanInOs)`; publica la lista en cada resultado aceptado. A los 10 s (o si `StartScanAsync` del adaptador lanza): `StopScanAsync` si la generación sigue siendo la suya y `Status = Disconnected` solo si sigue `Scanning` (`bs:113-123`). `BlePermissionDeniedException` → `Disconnected` sin relanzar (DV-100); otra excepción → `Disconnected` y relanza (banner) | 04-05, 05-04 a -06, -08, SEC-01 |
| `ConnectAsync(id)` | DT10-04. Cancela reconexión (`bs:136-137`); `Status = Connecting`, `Link.Phase = Connecting`; recuerda `_last = id`. `adapter.ConnectAsync(id, true, ct)` con tope 15 s: `scheduler.Delay(15 s)` cancela `ct` (el adaptador cierra la conexión nativa por id, DT10-17) y lanza `BleException("connect", "timed out after 15 s")`. `Discover` (§2.3). Suscribe `LinkLost`. `Status = Connected`, `Phase = Ready`. Si algo lanza: limpia, `Status = Disconnected`, `LastDisconnect = ConnectFailed`, relanza (`bs:156-159`) | 06-01 a -06 |
| `DisconnectAsync()` | Manual (`bs:304-313`): cancela reconexión, **olvida** `_last`, `peripheral.DisconnectAsync()`, limpia, `Status = Disconnected`, `LastDisconnect = Manual` | 07-05, 08-04 |
| `LinkLost` | `Status = Disconnected`, limpia (`peripheral.Dispose()`; el adaptador ya cerró la sesión nativa al recibir la caída, DT10-17), `LastDisconnect = Lost`, agenda intento (§2.4) (`bs:148-153`) | 08-02 |
| Datos | Notificación de `0x2AD1`: `RawBytes`, `FtmsParser.Parse` → `Data`, `_lastPacketAt = n.MonotonicSeconds`, registro `[BLE] Raw…` y `[BLE] Parsed → SPM:… W:… dist:…m elapsed:…s` (`bs:291-302`); `BleCadence.Add` (§7) | 06-02, -05 |

Todas las continuaciones comparan la **generación de conexión** (`_connectGen`, como el pulsómetro del legacy, DC-26): un intento superado no toca el estado.

### 2.3 Descubrimiento y toma de control (`bs:204-289`)

1. `DiscoverServicesAsync`; registra cada servicio y característica con `READ,WRITE,NOTIFY,INDICATE` (`bs:207-216`).
2. Por cada característica con `Notify|Indicate`: `SetNotifyAsync(true)`; un error solo se registra (`bs:219-232`). Las notificaciones de las que no son `0x2AD1` se registran en hex (`[BLE-CHAR:…] len=… → …`).
3. Identifica `0x2AD1` y `0x2AD9`. Sin `0x2AD1`: aviso en el registro y sigue (`bs:251-253`; `K-BIKE` queda conectado con métricas en 0 y sin watchdog, Q-35).
4. Con `0x2AD9`: `Phase = TakingControl`; `Write([0x00], withResponse)`; `scheduler.Delay(300 ms)`; `Write([0x07], withResponse)`; cualquier error se registra y sigue (`bs:259-276`). Después, keepalive (§2.5). Sin `0x2AD9` (`K-NOCP`): nada (`bs:261-264`).

### 2.4 Reconexión y watchdog

- **Reconexión (Q-29, DV-96, DV-97)**: `Reconnect = Waiting`, `NextAttemptAt = now + 3 s`. Al vencer: si el adaptador no está `On`, reagenda +3 s; si no, `Attempting`, `Status = Connecting`, `connect` con tope **10 s** (vence → cancela `ct`, igual que en §2.2), descubrimiento y toma de control (§2.3) → `Connected`, `Reconnect = None` (las métricas vuelven a cero por `DeviceStore`). Si falla: `Status = Disconnected`, limpia, `Waiting` otra vez a +3 s, **sin límite**. La cancelan `DisconnectAsync`, `StartScanAsync` y `ConnectAsync(otro)`; el watchdog y la manual nunca la arman.
- **Watchdog (`dp:69-86`, DT10-01)**: periódico de 1 s solo con `Connected`; actúa solo si hubo un paquete de `0x2AD1` en esta conexión; si `now − _lastPacketAt ≥ 5 s` → desconexión manual con `LastDisconnect = Watchdog`. Se detiene en cualquier otro estado.

### 2.5 Keepalive y segundo plano (ADR 017)

- En primer plano: `SchedulePeriodic(2 s)` escribe `[0x07]` con respuesta; error → registro (`bs:278-288`). No escribe con `scheduler.IsCatchingUp`. Se cancela al limpiar (`bs:315-319`).
- Con `lifecycle.InBackground = true` y `adapter.NeedsNativeKeepAlive`: cancela su periódico y llama `StartNativeKeepAlive(FTMS, CP, [0x07], 2 s)`; al volver, `StopNativeKeepAlive()` y rearma el periódico. En Windows no hace falta (DT10-09).
- Los eventos guardados en el *buffer* nativo durante la pausa se reproducen con su hora en el alcance (§5.2, con el desfase congelado del último drenaje en primer plano): el watchdog ve los paquetes en su instante virtual; si el monitor dejó de mandar ≥ 5 s, dispara en ese instante virtual del alcance (AC-BLE-91-01).

### 2.6 Puente nativo puro (`Core/Ble/Native`)

```csharp
public interface INativeBleApi
{
    int Init(); void Shutdown(); int ApiVersion { get; }
    double MonotonicSeconds();                      // reloj de los eventos
    int AdapterState();                             // lectura inicial; después llegan eventos
    int StartScan(string servicesCsv);              // 0 bien; Windows ignora el filtro
    int StopScan();
    int Connect(string id, bool highMtu);           // reqId > 0
    int Disconnect(string id);
    int Discover(string id);  int SetNotify(string id, string svc, string chr, bool on);
    int Write(string id, string svc, string chr, byte[] data, int length, bool withResponse);
    int KeepAliveStart(string id, string svc, string chr, byte[] data, int length, int periodMs);  int KeepAliveStop(string id);
    int Pending();                                  // bytes pendientes; 0 → no se llama a Drain (sin asignar)
    int Drain(byte[] buffer, int capacity);         // registros enteros
    void RequestEnable();
}
```

`NativeBleAdapter(INativeBleApi api, IBlePermissions? permissions, IScheduler scheduler, IAppLifecycle lifecycle, IAppLogger logger) : IBleAdapter, IBleEventPump` traduce: `reqId` → `TaskCompletionSource` (se resuelven al leer `RequestDone`), un `NativePeripheral` por id, `Connection(connected = false)` → `LinkLost` si no fue pedido. `BleEventReader.Read(buffer, length, IBleEventSink)` decodifica sin asignar salvo los `byte[]`/`string` de cada evento real.

**Sesiones (DT10-17, QA-G10a-03).** Por id, una **generación de conexión** y el `reqId` vigente:

- `ct` cancelado (venció el plazo de Core) → `api.Disconnect(id)`, la generación sube y la tarea falla con `OperationCanceledException`.
- `RequestDone` correcto de un `reqId` de una generación superada (éxito tardío) → `api.Disconnect(id)` y nada más (ningún `NativePeripheral`).
- `Connection(connected = false)` no pedido → `api.Disconnect(id)` (cierra la sesión: en Windows `MaintainConnection = false`) **antes** de emitir `LinkLost`; los reintentos de Core abren una sesión nueva.
- `NativePeripheral.Dispose()` / `DisconnectAsync()` → `api.Disconnect(id)` si la sesión sigue abierta (idempotente).

**Permiso (DT10-15, DT10-16, QA-G10a-02).** Con `permissions == null` (Windows), `EnsureScanPermissionAsync` da `true`. Si no: `Granted` → `true`; `NotAsked` o `Denied` → `RequestAsync()`, `Granted` → `true`, otra cosa → `State` pasa a `Unauthorized` y `false`; `DeniedForever` → `false` sin diálogo. `RequestEnableAsync`: sin `Granted`, primero `RequestAsync()` (si queda `DeniedForever` o el sistema no muestra el diálogo, `OpenAppSettings()`); con `Granted` y el adaptador `Off`, `api.RequestEnable()`; un código de error de `RequestEnable` (en Kotlin, `SecurityException`) → `OpenAppSettings()`, nunca un error (AC-BLE-04-04). `ScanFailed` con el código `-2` de Kotlin (sin permiso) → `permissions.Refresh()` y `Unauthorized` si no quedó concedido, sin banner. `OnResumed` (lo llama la bomba al volver) → `permissions.Refresh()` y se recalcula `State` (AC-APP-13-02: al volver de los ajustes con el permiso dado, la vista pasa a la búsqueda).

**Formato de cada registro** (little endian; lo escriben igual la DLL y Kotlin; fijado en `Tests/Golden/ble/events-v1.bin`):

```
u8 kind | u8 flags (0) | u16 payloadLength | f64 monotonicSeconds | payload
str = u16 n + n bytes UTF-8 (n = 0 → null)      uuid = 16 bytes, orden canónico del texto
1 AdapterState  i32 state (orden de BleAdapterState)
2 ScanResult    str id | str name | i16 rssi | u8 n | n × uuid
3 ScanFailed    i32 code | str message
4 Connection    str id | u8 connected | i32 code
5 RequestDone   i32 reqId | i32 status (0 bien) | Discover: u8 nSvc {uuid | u8 nChr {uuid | u8 props}} ; error: str message
6 Notify        str id | uuid characteristic | u16 n | n bytes
7 Dropped       i32 count   (el buffer descartó los más viejos)
```

Tope del registro 64 KB; *buffer* nativo de 2 MB (6 h a 2 Hz, ADR 017); `rm_ble_api_version() = 1` y C# rechaza otra versión (estado `Unsupported` y registro).

---

## 3. Plugin de Windows (WP-B1)

**API C** (`extern "C" __declspec(dllexport)`, `__cdecl`, `int32_t`): `rm_ble_init`, `rm_ble_shutdown`, `rm_ble_api_version`, `rm_clock_monotonic` (QPC en segundos: la misma base que el `Stopwatch`, DT10-18), `rm_ble_adapter_state`, `rm_ble_start_scan(const char* csv)`, `rm_ble_stop_scan`, `rm_ble_connect(const char* id, int32_t highMtu)`, `rm_ble_disconnect`, `rm_ble_discover`, `rm_ble_set_notify`, `rm_ble_write(…, const uint8_t*, int32_t len, int32_t withResponse)`, `rm_ble_keepalive_start/stop` (no-op, devuelven 0), `rm_ble_pending`, `rm_ble_drain(uint8_t*, int32_t)`, `rm_ble_wire_fixture(uint8_t*, int32_t)` (escribe los eventos de `events-v1` con el codificador real, para el test de contrato). Cadenas UTF-8; nada lanza: todo error es un código o un `RequestDone` con mensaje.

**Hilos.** El hilo de Unity no toca WinRT (puede estar en STA). `rm_ble_init` (idempotente) arranca un **hilo de adaptador** MTA (`init_apartment(multi_threaded)`) con cola de comandos; cada equipo conectado tiene su **cola GATT** (un hilo MTA, una operación a la vez, `wait_for(10 s)` por operación, DT10-03). Los *callbacks* de WinRT (pool de hilos) solo copian bytes al anillo bajo `std::mutex`. `rm_ble_shutdown` detiene el watcher, cierra sesiones y une los hilos (lo llama `AppRoot.OnDestroy` y, en el Editor, `AssemblyReloadEvents.beforeAssemblyReload`).

| Función | WinRT |
|---|---|
| Estado | `BluetoothAdapter::GetDefaultAsync()`: nulo o `!IsLowEnergySupported()` → `Unsupported`; `GetRadioAsync()` → `Radio.State` (`On`, `Off`/`Disabled` → `Off`, `Unknown`) y `StateChanged` → evento 1. Sin `Radio` se relee en cada `start_scan`. **Nunca** `Radio.SetStateAsync` ni `RequestAccessAsync` |
| Búsqueda | `BluetoothLEAdvertisementWatcher` activo (`AllowExtendedAdvertisements` si existe), sin filtro; fusión por dirección (DT10-07); emite el evento 2 cuando el equipo es nuevo o cambian nombre o UUID, y el RSSI como mucho 1 vez/s; `Stopped` con error → evento 3 |
| Conexión | `BluetoothLEDevice::FromBluetoothAddressAsync` (nulo → error `device not found`), `GattSession::FromDeviceIdAsync` con `MaintainConnection = true`, `GetGattServicesAsync(Uncached)` para forzar el enlace; `ConnectionStatusChanged` → evento 4. `highMtu` se ignora (Windows negocia `MaxPduSize`). Un `connect` sobre un id que ya tiene sesión (o un `connect` pendiente) **primero la cierra** como en "Desconexión" (DT10-17): como mucho una sesión y una cola GATT por id |
| GATT | `GetCharacteristicsAsync(Uncached)`; `WriteClientCharacteristicConfigurationDescriptorAsync(Notify o Indicate)` + `ValueChanged` → evento 6; `WriteValueWithResultAsync` |
| Desconexión | Quita `ValueChanged`, CCCD a `None` (mejor esfuerzo), `MaintainConnection = false`, `Close` de servicios, sesión y equipo, y termina la cola GATT del id. Con un `connect` **pendiente**, lo cancela (`IAsyncOperation::Cancel` de la operación en curso, y cierra lo que ya se haya abierto) y su `RequestDone` sale con error `cancelled`. Idempotente: un id sin sesión devuelve 0 |
| Emparejado | **Nunca** `PairAsync`, `DeviceInformationPairing` ni `UnpairAsync` (AC-BLE-SEC-01) |
| Ajustes | `RequestEnable` no está en la DLL: `WindowsBleApi.RequestEnable` hace `Application.OpenURL("ms-settings:bluetooth")` (Q-32) |

**Compilación** (`native/windows/build.ps1`, ASCII): `vswhere -latest -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath` → `Common7/Tools/Launch-VsDevShell.ps1 -Arch amd64 -HostArch amd64 -SkipAutomaticLocation` (el SDK sale de `$env:WindowsSDKVersion`; C++/WinRT viene en el SDK). `cl /std:c++20 /EHsc /O2 /MT /permissive- /W4 /WX /utf-8 /DNDEBUG /DWINRT_NO_SOURCE_LOCATION /Brepro` + `link /DLL /DEBUG:NONE /Brepro WindowsApp.lib`, todo en `obj/` temporal, y copia a `Assets/Plugins/Windows/x86_64/RowMateBle.dll`. CRT estática: sin dependencia del redistribuible. **`/DWINRT_NO_SOURCE_LOCATION`** es obligatorio (QA-G10a-01): sin él, C++/WinRT 2.0.250303.1 en `/std:c++20` *release* guarda `__builtin_FILE()` en `slim_source_location` (`winrt/base.h:200-216`), es decir, la ruta absoluta de los encabezados del SDK y de los `.cpp` del clon. Antes de compilar, el script revisa las fuentes propias: ningún `__FILE__`, `__builtin_FILE`, `std::source_location` ni `assert(` en `native/windows/RowMateBle/` (código 3 si aparece). Después revisa el binario: ningún `C:\`, `\Users\`, `\Windows Kits\`, `$env:USERNAME` ni `$env:COMPUTERNAME` en ASCII ni UTF-16 (código 2 si aparece; nunca se saltea). `-Check` compila en temporal y compara el SHA-256 con el versionado. Se niega a copiar si `Temp/UnityLockfile` está tomado (la DLL queda cargada en el Editor). El binario (≈ 300 KB) se versiona **sin LFS** (el repo no lo usa).

---

## 4. Plugin de Android (WP-C)

### 4.1 Manifiesto (`RowMateBle.androidlib/AndroidManifest.xml`, se fusiona con el de Unity; AC-APP-13-01, `am:4-19`)

```xml
<uses-permission android:name="android.permission.BLUETOOTH_SCAN" android:usesPermissionFlags="neverForLocation" tools:targetApi="s"/>
<uses-permission android:name="android.permission.BLUETOOTH_CONNECT"/>
<uses-permission android:name="android.permission.BLUETOOTH" android:maxSdkVersion="30"/>
<uses-permission android:name="android.permission.BLUETOOTH_ADMIN" android:maxSdkVersion="30"/>
<uses-permission android:name="android.permission.ACCESS_FINE_LOCATION" android:maxSdkVersion="30"/>
<uses-permission android:name="android.permission.WAKE_LOCK"/>
<uses-feature android:name="android.hardware.bluetooth_le" android:required="true"/>
```

`minSdk 26` y solo ARM64 ya están en `ProjectSettings` (`AndroidMinSdkVersion: 26`, `AndroidTargetArchitectures: 2`): no se tocan. Sin `VIBRATE` (D10-11).

### 4.2 Kotlin (`RmBle` = `object` con `@JvmStatic`, mismos nombres que la API C)

- `init(activity)`: guarda el contexto de aplicación, registra `AdapterMonitor` (`ACTION_STATE_CHANGED` → evento 1) y calcula el estado: sin `FEATURE_BLUETOOTH_LE` → `Unsupported`; si no, `BluetoothAdapter.getState()` (`STATE_ON` → `On`; `OFF` y `TURNING_*` → `Off`), que en API 31+ no pide permiso. **Kotlin nunca emite `Unauthorized`**: lo decide Core con `IBlePermissions` (DT10-15, QA-G10a-02). `adapterState()` también se relee al volver al frente (por si se perdió un *broadcast*).
- Permiso (lo usa `AndroidBlePermissions`): `hasScanPermission()` (`checkSelfPermission` de `BLUETOOTH_SCAN` + `BLUETOOTH_CONNECT` en API 31+, `ACCESS_FINE_LOCATION` en ≤ 30), `shouldShowRationale(perm)`, y `wasDenied()` / `setDenied(bool)` en `SharedPreferences("rm_ble")` (DT10-15; se borra al quedar concedido).
- `startScan(csv)`: `BluetoothLeScanner.startScan([ScanFilter.setServiceUuid(FTMS)], LOW_LATENCY)` (filtro del sistema, `FiltersScanInOs = true`); `onScanFailed` → evento 3. Sin permiso (`SecurityException`) → evento 3 con el código propio `-2`, que Core trata como negativa (no banner).
- `connect(id, highMtu)`: `getRemoteDevice(id).connectGatt(ctx, false, cb, TRANSPORT_LE)`; con `highMtu`, `requestMtu(512)` antes de dar por conectado. `GattQueue` por equipo: una operación a la vez, tope 10 s. Un `connect` sobre un id con `BluetoothGatt` abierto (conectado o pendiente) primero hace `disconnect()` + `close()` de ese (DT10-17). Desconexión: `disconnect()` + `close()`, también si el `connect` sigue pendiente (lo cancela; su `RequestDone` sale con `cancelled`). Errores (p. ej. 133) → evento 4 con el código. Nunca `createBond` ni `removeBond` (AC-BLE-SEC-01).
- Notificaciones: `setCharacteristicNotification` + CCCD `0x2902` (firmas de API 33 con respaldo); `onCharacteristicChanged` copia el valor → evento 6.
- `keepAliveStart/Stop`: `Handler` en `HandlerThread("rm-ble-keepalive")` que encola la escritura en la `GattQueue` cada `periodMs` (DT10-09).
- `EventRing`: `ByteBuffer` de 2 MB con `synchronized`; `pending()` y `drain(): ByteArray`. Reloj **inyectado** (`() -> Long` en nanosegundos); `RmBle` le pasa `System::nanoTime` (`CLOCK_MONOTONIC`, la misma base que el `Stopwatch` de `UnityScheduler`, DT10-18) y `monotonicSeconds()` usa el mismo. Antes era `SystemClock.elapsedRealtimeNanos()`, que cuenta el sueño profundo y el `Stopwatch` no (QA-G10a-04).
- `requestEnable(): Int`: solo con `BLUETOOTH_CONNECT` concedido (Core pide el permiso antes, §2.6); `startActivity(ACTION_REQUEST_ENABLE)` dentro de `try`; `SecurityException` → devuelve `-2` y Core abre los ajustes de la app. `openAppSettings()`: `ACTION_APPLICATION_DETAILS_SETTINGS`; `haptic()`: `runOnUiThread { decorView.performHapticFeedback(API ≥ 30 ? CONFIRM : CONTEXT_CLICK) }`.

### 4.3 Puente C# (`Platform/Ble/AndroidBleApi.cs`)

`AndroidJNI` crudo con `jclass` y `jmethodID` en caché y `jvalue[]` reusados: `pending()` cada cuadro **sin asignar**; `drain()` solo con `pending > 0` (la única asignación: el `byte[]` de los eventos reales). Los nombres y firmas JNI salen **solo** de `Core/Ble/Native/RmBleJniSignatures` (constantes `Name` + `Signature`, p. ej. `("pending", "()I")`); ningún literal JNI suelto en Platform.

`AndroidBlePermissions : IBlePermissions` (delgado; la regla es Core, §2.6): API ≥ 31 `BLUETOOTH_SCAN` + `BLUETOOTH_CONNECT`, ≤ 30 `ACCESS_FINE_LOCATION`, con `UnityEngine.Android.Permission.RequestUserPermissions` y sus `PermissionCallbacks`; la respuesta pasa por `MainThreadQueue` (DT10-10). `State`: concedido → `Granted`; si no, `wasDenied()` falso → `NotAsked`; verdadero y `shouldShowRationale` → `Denied`; si no → `DeniedForever`. `PermissionDenied` → `setDenied(true)`, `Denied`; `PermissionDeniedAndDontAskAgain` → `DeniedForever`. `Refresh()` relee todo.

**Sin `#if UNITY_ANDROID`** (DT10-19, QA-G10a-05): `AndroidBleApi`, `AndroidBlePermissions` y `AndroidHaptics` se compilan en todas las plataformas (`AndroidJNI`, `AndroidJavaObject` y `UnityEngine.Android.Permission` están en `UnityEngine.AndroidJNIModule`, también en el Editor de Windows), así los compila el `run-unity-tests.ps1` de esta PC. Nunca se instancian fuera de Android: el único `#if` es la rama de `BleAdapterFactory` (§5.1). `JniSignatureTests` (dotnet) compara la tabla con `native/android/golden/jni-signatures.txt`, y `check.ps1` regenera ese archivo con `javap -s` de `RmBle` y falla si difiere (§4.5).

### 4.4 Segundo plano y pantalla (Q-91, AC-APP-14-05)

Lo del ADR 017: Unity pausa el *loop*, Kotlin sigue (keepalive nativo + anillo de eventos) y al volver se hace el alcance. La pantalla encendida ya está (`ScreenSleepWakeLock` + `ScreenKeepAwake` al volver, It. 4a). Sin servicio en primer plano (DT10-12).

### 4.5 Verificación sin el módulo de Unity

`native/android/check.ps1` encuentra Android Studio (`$env:ANDROID_STUDIO` o `$env:ProgramFiles\Android\Android Studio`) y el SDK (`$env:ANDROID_HOME` o `$env:LOCALAPPDATA\Android\Sdk`) sin rutas fijas, y compila todo el Kotlin con `kotlinc` + el JBR contra el `android.jar` más nuevo, en `<scratch>`. Con `-Golden` compila y corre en la JVM `golden/EmitFixture.kt` (usa `Wire.kt` y `EventRing.kt`, que no dependen de Android) y compara su salida con `Tests/Golden/ble/events-v1.bin`. Siempre corre `javap -s -public` sobre `RmBle.class`, normaliza la salida (nombre + descriptor, ordenados) y la compara con `native/android/golden/jni-signatures.txt` y con la tabla de `RmBleJniSignatures.cs` (leída por regex): cualquier diferencia → código 4 (QA-G10a-05). El APK, los permisos en el teléfono, la vibración y el segundo plano quedan **NO VERIFICABLE** hasta "Android Build Support".

---

## 5. Composición, bomba de eventos y build (WP-B1)

### 5.1 Selección

```csharp
// DeviceBackendFactory (único #if): simulador como hoy; si no:
HardwareBackend.Create(IScheduler scheduler, IAppLifecycle lifecycle, IAppLogger logger, out IBleEventPump? pump);
// BleAdapterFactory.Create(logger, out IBlePermissions? permissions) → INativeBleApi? (ÚNICO #if de plataforma, DT10-19):
//   WindowsPlayer/WindowsEditor → WindowsBleApi, permissions = null; #if UNITY_ANDROID → AndroidBleApi + AndroidBlePermissions;
//   otro, ROWMATE_BLE=off o falla al cargar (DllNotFoundException, versión) → null.
// null → DeviceBackend(UnavailableRowerService, UnavailableHeartRateService) (It. 2);
// si no → var adapter = new NativeBleAdapter(api, permissions, scheduler, lifecycle, logger); pump = adapter;
//         DeviceBackend(new RowerService(adapter, …), UnavailableHeartRateService) (el pulsómetro, It. 11).
```

- `HardwareBackend.Unavailable(logger)` queda para `HostBackends.Unavailable` (CAP-22 de la It. 2).
- El build `simulator` nunca crea `NativeBleAdapter`: no carga la DLL ni llama a `rm_ble_init` (contador estático en `WindowsBleApi`; AC-APP-13-03).
- `run-unity-tests.ps1` y `capture-snapshots.ps1` exportan `ROWMATE_BLE=off` (y lo quitan al salir): ninguna prueba toca la radio.

### 5.2 Bomba y alcance (Core: `IBleEventPump` de `NativeBleAdapter` + `Native/BleEventClock`; QA-G10a-04)

```csharp
public interface IBleEventPump { void Drain(bool resuming); }   // AppRoot; sin asignar si api.Pending() == 0
public sealed class BleEventClock                                // puro; lo usa NativeBleAdapter
{
    public void Calibrate(double appFrameSeconds, double nativeNowSeconds);   // solo en drenajes de primer plano
    public void Reset();                                                      // al iniciar y con IAppLifecycle.Woke (sueño del sistema)
    public double ToApp(double nativeSeconds);                                // t_nativo + desfase congelado
    public bool IsCalibrated { get; }
}
```

- **Orden en `AppRoot`** (sin cambios): `Update`: `pump.Drain(resuming: false)` **antes** de `scheduler.Tick()`; `OnApplicationPause(false)`: `pump.Drain(resuming: true)` **antes** de `ResumeFromBackground()`. `MainThreadQueue` se drena antes de la bomba (QA-G10a-17).
- **Desfase**: solo en los drenajes de **primer plano**, `Calibrate(scheduler.MonotonicSeconds, api.MonotonicSeconds())`. Como el drenaje va antes del `Tick`, la hora del cuadro es la del cuadro anterior y cada muestra `app − nativo` queda por debajo del desfase real (como mucho, un cuadro); se toma el **máximo** de las muestras, que converge al real. `Reset()` al iniciar y con `Woke` (en un sueño del sistema, el reloj despierto de la app descuenta lo dormido y el nativo no).
- **Congelado**: el drenaje de la vuelta (`resuming: true`) **no** calibra: usa el desfase del último drenaje en primer plano. Ahí `scheduler.MonotonicSeconds` sigue en el último cuadro antes de la pausa; medirlo correría todos los eventos hacia atrás lo que duró la pausa (el defecto del gate).
- **Despacho**: en primer plano, en el acto (`BleNotification.MonotonicSeconds = ToApp(t)`). Al volver, cada evento se agenda con `scheduler.Schedule(max(0, ToApp(t) − scheduler.MonotonicSeconds))`; el alcance lo intercala con los timers (keepalive, watchdog) en orden de hora y, en un empate, en orden de llegada (ADR 017). Así el watchdog ve cada paquete en su instante virtual: con paquetes cada segundo no dispara; con 10 s de silencio dentro de la pausa, dispara a los 5 s de ese silencio, en tiempo virtual.
- **Misma base de reloj** (DT10-18): QPC en Windows, `System.nanoTime()` en Android, `Stopwatch` en la app. Si cambian de base, el desfase deja de ser constante y el alcance se corre: RA-19 obliga a cambiar los dos juntos.
- `Dropped` → registro de advertencia. Búfer de drenaje de 64 KB reusado (Core). Antes de `Drain(resuming: true)`, la bomba llama `OnResumed` (permiso, §2.6).

### 5.3 `build.ps1` y humo

- Sin cambios de semántica: `-Simulator` → simulado; normal → Bluetooth real. `BuildOutputInspector` además exige `RowMateBle.dll` en `RowMate_Data/Plugins/x86_64/` del build normal de Windows.
- **`-BleSmoke`** (como `-DbSmoke`): build de desarrollo en `Builds/Windows/dev-smoke`, corre el player con `-rowmate-ble-smoke` y `ROWMATE_DATA_DIR` temporal. `BleSmoke` (Bootstrap) lee el estado, hace **3 búsquedas de 10 s** con `RowerService`, minimiza la ventana durante la segunda (`ShowWindow`) y la restaura, nunca conecta, y escribe solo contadores: `[BLE] humo: estado=On busquedas=3 ftms=N fallas=0 colgado=no`. Pasa si estado `On`, 3 búsquedas terminadas y lista solo con FTMS. Código 6 si falla. Si `Builds/` está en uso, reintenta cada 2 min hasta 20 veces.

---

## 6. Vistas (WP-B1 estructura y comportamiento; WP-D aspecto y movimiento)

### 6.1 Dispositivo

```
#screen-device
├── #device-appbar (deviceDisconnect solo Connected)
├── #device-error-banner (ErrorBanner: deviceScanError / deviceConnectError + detalle; arriba en las tres vistas)
└── #device-views
    ├── #device-state-view    BluetoothStateView: BluetoothIllustration, título 18 negrita, ayuda, #device-bt-enable
    ├── #device-scan-view     #device-link-banner (LinkBanner) · #device-search · hint · SonarView · #device-results (ScanResultList) · vacío · #hrm-card-slot-scan
    └── #device-connected-view  #device-header (nombre; presión ≥ 0,5 s) · grilla · #ble-debug-toggle · #ble-debug-panel · #hrm-card-slot-connected
```

`DeviceScreenRules.ViewFor(status, adapter)`: `Unsupported`, `Off` o `Unauthorized` (sin `Connected`) → `BluetoothState`; `Connected` → `Connected`; si no, `Scan` (`ds:33-38`, `:346-347`). `BluetoothStateSpec` (título, ayuda, botón sí/no, ícono rojo o terciario, respira sí/no). `ScanItemSpec` (clave = id; nombre o `deviceUnknown`; renglón 2 = dirección o paso; spinner y habilitado según `Status`). `LinkBannerSpec` (Reconnecting con nombre aparte / MonitorStopped / nada). La tarjeta del pulsómetro no está en la vista de estado. `#device-bt-enable` → `RowerService.RequestEnableAsync()`.

**Pasos** (A10-05): la tarjeta tocada lee `Link.Phase`; al pasar a `Connected`, `DeviceScreenView` retiene 240 ms el `ViewSwap` (check) y después corre el "encender" (≤ 600 ms); `Workout` ve `Connected` en el cuadro 0. `IHaptics.Confirm()` lo llama `DeviceStore` al pasar a `Ready` por una conexión elegida por el usuario.

**Diagnóstico** (`ds:52-74`, `:124-218`): `DebugRevealGesture` (App) cuenta 3 arrastres hacia abajo de > 20 dp en el `ScrollView` conectado; la presión de la cabecera ≥ 500 ms abre el panel directo (DV-104). El estado se reinicia al salir de `Connected`. `BleDebugSpec` arma `RECEIVING`/`NO DATA`, `Notifications` (contador de la vida de la app), `Last received` `HH:MM:SS` (hora local del paquete de **esta** conexión) o `--:--:--`, el hex (`BleHex`, `(sin datos)`), y `Parsed SPM/Watts/Distance/Split` crudos (Q-38).

### 6.2 `DeviceStore` (C)

`DeviceState` suma `Link`, `Diagnostics` (`BleDiagnostics(long Count, string Hex, DateTimeOffset? LastReceived)`), `LinkBanner` y `RetryDeviceId`. Diagnóstico: `RawBytes` → `Count++`, hex, `LastReceived = scheduler.LocalNow`; al pasar a `Connected`, `LastReceived = null` (`dp:64-67`; el hex y el contador quedan). Aviso: `LastDisconnect = Watchdog` → `MonitorStopped` con `RetryDeviceId`; se borra al reintentar, buscar o conectar (DV-98). `IEventSource<LinkNotice> LinkNotices` emite `Reconnecting(name)`, `Reconnected(name)` (solo si hubo reconexión), `MonitorStopped` y `Cleared`.

### 6.3 Aviso sobre el HUD (AC-BLE-08-06, -09-02; DV-98, DV-99)

`WorkoutScreenView` escucha `LinkNotices` solo con el inmersivo arriba y antes de confirmar `Terminar`, y llama `ToastLayer.ShowLink(LinkToast)`: `Reconnecting` **fijo** hasta `Reconnected` o `Cleared`; `Reconnected` 2 s; `MonitorStopped` 4 s. El `#toast` suma un hijo `#toast-indicator` (`LinkIndicator`, 1 elemento, malla propia) y la clase `.rm-toast--link`. Un aviso común que llega con el del enlace fijo lo reemplaza 4 s y después vuelve el fijo. El `ToastLayer` está fuera de `#immersive`, así que `HudBudgetTests` no cambia (59). No tapar métricas: `HudLinkNoticeTests` cruza el rectángulo del aviso con SPM, Split, Watts, BPM, kcal y la banda en los cinco perfiles; si cruza en alguno, `ImmersiveWorkoutPage` registra la banda como obstáculo inferior mientras el aviso está a la vista (plan B). Se actualiza el test de AC-WKT-03-10 (4a): ahora sí hay aviso.

### 6.4 Movimiento y presupuesto (WP-D)

Sonar (P1, D10-02: sin banda con alto compacto), entradas de tarjetas con clave estable, pasos con cruce 0-80 / 80-200, check, "se despierta", banner del enlace, indicador de 3 s, punto que late, y sus variantes con reducir movimiento: constantes en `BleMotionSpecs`. Presupuesto de arte §8: 0 B por cuadro con FrameAllocations y marcadores `RowMate.Device.Sonar` y `RowMate.Toast.LinkNotice`.

---

## 7. QA-V4a-L01: cadencia del monitor

**Cambio** (AC-WKT-03-11, DV-103): `SanitizedDistance` guarda `_lastChangeTime` (reloj activo de la última vez que la distancia cruda cambió o se fijó la referencia: `Begin`, un salto rechazado o `Rebase`, que toma `LastTime`). Tope: `Δ > 10 · max(t − _lastChangeTime; 1)` → no suma. `Dt`, `TimeBefore` y `LastTime` del `DistanceSample` no cambian (el `SeriesTracker` los usa para interpolar). Con una lectura nueva por segundo, `t − _lastChangeTime = dt` y todo da igual; los goldens de la 4a y la 5 se corren primero, y si algún vector cambia se reporta antes de seguir (riesgo R-07).

**Medición en la demo** (`BleCadence`, Core): con cada paquete de `0x2AD1` acumula, sin asignar, el intervalo entre paquetes y el intervalo entre cambios de distancia (cubetas < 0,75 s, 0,75-1,5, 1,5-2,5, > 2,5), el máximo Δdist/Δt, y cada cambio del contador de paladas con su hora y la potencia del paquete (D-ANI-06). Cada 60 s y al desconectar registra `[BLE] Cadencia: paquetes=… intervalo p50=… p95=… distancia cada p50=… p95=… maxVel=… paladas=…`. Va al `Player.log` (no se versiona).

**Si no es estable**: con la regla nueva el paso por distancia ya no se traba con cualquier cadencia. Si la distancia llega cada > 2,5 s, el HUD avanza a saltos (issue de interpolación visual); si el monitor deja de notificar en pausas de 10 s, el watchdog desconecta (issue de Q-31: distinguir pausa de apagado); si el contador de paladas sube en una fase estable, se puede encender D-ANI-06 en la It. 14.

---

## 8. Tests y verificación

### 8.1 Comandos

`powershell -NoProfile -File tools/dotnet/run-tests.ps1 -ArtifactsPath <scratch>/it10a` (WP-A, y el contrato de la DLL con `RowMate.Tests.Windows`); `tools/unity/run-unity-tests.ps1` y `-Simulator`; `native/windows/build.ps1 [-Check]`; `native/android/check.ps1 -Golden`; `tools/build/build.ps1 -Target Windows -BleSmoke`; `tools/unity/capture-snapshots.ps1 -Set it10a -NoSimulator` dos veces y comparación byte a byte.

### 8.2 Doble de prueba

`FakeBleAdapter(IScheduler, IAppLogger, filtersScanInOs: false, needsNativeKeepAlive: false)`: `SetState`, `Advertise(device)` / `AdvertiseAt(t, device)`, `FailNextScan(ex)`, permiso `PermissionNotAsked()` / `DenyPermission()` / `DenyPermissionForever()` / `AnswerPermission(granted)`, por equipo `Respond | NeverRespond | Fail(ex)`, `DropLink(id)`, `Notify(id, chr, bytes)` / `NotifyEvery(id, period, k → bytes)`, `EnqueueAt(t, …)` (solo para la semántica de `RowerService`; el alcance real se prueba con `FakeNativeBleApi`); registro `Writes`, `NativeKeepAlive{Starts,Stops,Writes}`, `ConnectCalls(id)`, `ConnectCancels(id)`, `ScanStarts`, `PermissionRequests`, `RequestEnableCalls`, `AppSettingsOpens`. `FakeBleDevices.K10` = `Row`, `Anon`, `Bike`, `NoCp`, `Hp` con los datos de la spec §0 y el paquete del oráculo `ftms.json`.

`FakeNativeBleApi` emite registros del formato §2.6 con **su propio reloj** (`NativeNow`, con un desfase arbitrario respecto del de la app), `EnqueueAtNative(t, record)`, conexiones `Respond | NeverRespond | RespondAfter(s)`, y cuenta `LiveSessions(id)` (un `Connect` correcto suma, `Disconnect` resta, un `connect` sobre una sesión abierta la reemplaza) y `Disconnects(id)`. `FakeBlePermissions` implementa `IBlePermissions` con respuesta guionada. Los tests del alcance usan `FrameScheduler` con `LifecycleHarness` (`Background` / `Foreground`).

### 8.3 Matriz AC → test (`It10aIds.cs`)

| AC | Marca | Tests |
|---|---|---|
| AC-BLE-04-01 | d e cap | `DRB.OffShowsStateView`, `RSS.AdapterOffSetsStatusOff`; `DBT.StateViewReplacesScanAndHrm`; CAP-01, -02 |
| AC-BLE-04-02 | d e cap | `DRB.UnauthorizedShowsDenied`, `NBA.NotAskedIsNotUnauthorized`, `NBA.DeniedIsUnauthorized`; `DBT.DeniedView`; CAP-03 |
| AC-BLE-04-03 | d e | `DRB.OtherStatesShowScan`; `DBT.OffToOnMorphsWithoutAutoScan` (y con reducir movimiento) |
| AC-BLE-04-04 | d dev | `RSC.RequestEnableDelegates`, `NBA.RequestEnableAsksPermissionFirst`, `NBA.RequestEnableDeniedForeverOpensAppSettings`, `NBA.RequestEnableSecurityErrorOpensAppSettings` (d); [Device-Win] QA manual: abre los ajustes; [Device-Android] |
| AC-BLE-04-05 | d | `RSS.WaitsFiveSecondsForOn`, `RSS.AdapterNeverOnSetsOffWithoutMessage` |
| AC-BLE-04-06 | d e cap | `RSS.UnsupportedDoesNothing`, `DRB.UnsupportedHasNoButton`; CAP-04 |
| AC-BLE-04-07 | win | `SMK` (`-BleSmoke`: estado `On`) |
| AC-APP-13-01 | d | `AMT.ExactPermissions` (texto del `.androidlib`); manifiesto fusionado [Device-Android] |
| AC-APP-13-02 | d android | `RSS.FirstLaunchShowsScanNotDenied`, `RSS.ScanAsksPermissionBeforeAdapterWait`, `RSS.PermissionGrantedScansWithoutAnotherTap`, `RSS.PermissionDeniedIsNotABanner`, `NBA.ResumeRereadsPermission` (d); el diálogo en el teléfono, NO VERIFICABLE |
| AC-APP-13-03 | d u win | `CMP.NormalUsesRowerService`; `CMP.SimulatorNeverInitsNative` (Unity `-Simulator`); suites de la It. 2 con `-Simulator` |
| AC-BLE-05-04 | d e cap | `RSS.TenSecondsFtmsOnly`, `RSS.ClearsErrorAndList`; `DBT.SearchingSpinnerAndHint`; CAP-05 |
| AC-BLE-05-05 | d | `RSS.K10OrderDedupFirstSeen`, `RSS.ClientSideFilterDropsHeadphones`, `RSS.OnlyThisScanCounts` |
| AC-BLE-05-06 | d | `RSS.EndsToDisconnectedUnlessConnecting`, `RSS.ListSurvivesConnectDisconnect`, `RSS.OldScanDoesNotClobberNew` |
| AC-BLE-05-07 | e cap | `DBT.ItemCardAndEmptyState`; CAP-06, -07 |
| AC-BLE-05-08 | d e cap | `RSS.ScanFailureResetsStatus`; `DBT.ScanErrorBanner`; CAP-08 |
| AC-BLE-05-09 | e cap | `DBT.LT29/LT30/LT31` con `RowerService` + falso; CAP-07 |
| AC-BLE-05-10 | win | `SMK` (3 búsquedas, solo FTMS, sin colgarse) |
| AC-BLE-06-01 | d e | `RSC.ConnectingAndFifteenSecondCap`, `RSC.TimeoutCancelsAdapterConnect`; `NBA.TimeoutCancelsNativeConnect`, `NBA.LateConnectAfterTimeoutIsClosed`; `DBT.AllItemsSpinAndBlock` |
| AC-BLE-06-02 | d | `RSC.DiscoversAndLogs`, `RSC.BikeConnectsWithZeroMetricsNoWatchdog` |
| AC-BLE-06-03 | d | `RSC.ControlZeroThen300msThenSeven`, `RSC.WriteFailureContinues`, `RSC.NoControlPointNoWrites` |
| AC-BLE-06-04 | d | `RSC.KeepAliveThirtyInSixtySeconds`, `RSC.KeepAliveStopsOnAnyDisconnect` |
| AC-BLE-06-05 | d | `RSC.ConnectedAfterHandshakeZeroesData`, `DSB.DataPublishedToApp` |
| AC-BLE-06-06 | d e cap | `RSC.TimeoutBannerClearedOnRetry`; CAP-12 |
| AC-BLE-06-07 | d e cap | `DSB.PhasesAndHapticOnce`; `DBT.StepsAndReadyHold` (+ reducir movimiento); CAP-09 a -11 |
| AC-BLE-06-08 | hw | NO VERIFICABLE (demo §1) |
| AC-BLE-07-05 | d e | `RSC.ManualForgetsNoReconnect`; `DBT.DisconnectReturnsToLastList` |
| AC-BLE-11-12 | e cap | `DRB.HeaderNameFallbacks`; CAP-13 |
| AC-BLE-08-02 | d | `RRC.FirstAttemptAtThreeSecondsTenSecondCap` |
| AC-BLE-08-03 | d | `RRC.RetriesForeverEveryThreeSecondsAfterFailure`; `NBA.LinkLostClosesSession`, `NBA.RetriesDoNotLeakSessions` (60 s de reintentos con `NeverRespond` → `LiveSessions ≤ 1`) |
| AC-BLE-08-04 | d | `RRC.CancelledByDisconnectScanConnect`, `RRC.WatchdogAndManualNeverReconnect`, `RRC.AdapterOffThenOnReconnects` |
| AC-BLE-08-05 | e cap | `DBT.ReconnectBannerEnablesSearchBetweenAttempts`; CAP-14 |
| AC-BLE-08-06 | e cap | `HLN.ReconnectingStickyReconnectedTwoSeconds`, `HLN.NoOverlapFiveProfiles`, `HudBudgetTests` (sin cambios); CAP-15, -16 |
| AC-BLE-08-07 | hw | NO VERIFICABLE |
| AC-BLE-09-01 | d | `RWD.FiresAtFiveWholeSeconds`, `RWD.NeverWithoutPacket`, `RWD.StopsOutsideConnected` |
| AC-BLE-09-02 | d e cap | `DSB.MonitorStoppedRetrySameDevice`; `HLN.MonitorStoppedFourSeconds`; CAP-17, -18 |
| AC-BLE-09-03 | hw | NO VERIFICABLE (anotar pausa de 10 s) |
| AC-BLE-12-01 | d e | `DRG.ThreeDragsOver20`, `DBT.LongPressOpensPanel`, `DBT.PanelResetsOnReconnect` |
| AC-BLE-12-02 | d e cap | `DSB.DiagnosticsLifetimeCounterAndHex`, `BHX.*`; CAP-19, -20 |
| AC-BLE-91-01 | d | `RBG.NativeKeepAliveThirtyInSixty`; con la bomba real (`NativeBleAdapter` + `FakeNativeBleApi` + `LifecycleHarness`): `RBG.SixtySecondsWithPacketsNoWatchdog`, `RBG.TenSecondSilenceFiresAtItsVirtualInstant`, `RBG.ResumeDrainDoesNotRecalibrate`; `BEC.MaxOfSamplesConvergesToOffset`, `BEC.ResetOnWoke` |
| AC-BLE-91-02 | win android hw | `SMK` (minimizar buscando); `DBT.FocusLossKeepsConnectedFake`; resto NO VERIFICABLE |
| AC-APP-14-05 | android | NO VERIFICABLE (la regla ya la cubre `ScreenKeepAwake` de la 4a) |
| AC-WKT-03-11 | d | `SDZ.EveryTwoSecondsPlusElevenEndsAt20`, `.SameSecondBothCount`, `.FiftyAfterFiveSeconds`, `.ResetToZero`, `.TwoHundredInOne`; goldens 4a/5 intactos |
| AC-BLE-SEC-01 | d qa | `RSS.HeadphonesNeverListedOrConnected`, `RSC.RejectsUnknownId`; `NHY.NoPathsInDllOrNative` (la DLL sin `C:\`, `\Users\`, `\Windows Kits\` en ASCII ni UTF-16; fuentes nativas sin `__FILE__`, `__builtin_FILE`, `source_location`, `assert(`, `PairAsync`, `createBond`, `removeBond`); revisión de QA de capturas y docs |

Además: `BER.*` (lector contra `events-v1.bin`), `NBA.*` (correlación de `reqId`, `LinkLost` no pedido, versión de API, `Pending()==0` sin drenar, y los de sesiones y permiso de arriba), `JniSignatureTests` (la tabla de `RmBleJniSignatures` contra `golden/jni-signatures.txt`), `WindowsDllContractTests` (la DLL escribe `events-v1.bin` idéntico; solo Windows), `native/android/check.ps1 -Golden` (Kotlin idéntico), `L10n` 240 claves, `BleAllocationTests` (0 B por cuadro con el sonar y el aviso, FrameAllocations).

### 8.4 Capturas (`it10a`, `BleCatalog`, adaptador falso, dos corridas idénticas)

`UiTestHost` con `HostBackends.FakeBle(K10, script)`, `FakeScheduler`, `ROWMATE_DATA_DIR` temporal y `ROWMATE_BLE=off`; salida en `docs/migration/iterations/10a-ble-android-windows/snapshots/`. Guion por captura:

| CAP | Estado |
|---|---|
| 01 / 02 / 03 / 04 | `SetState(Off)` es (TV, W1280) y en (TV); `Unauthorized`; `Unsupported` (TV) |
| 05 | Buscar, sin anuncios, t = 1 200 ms |
| 06 | Anuncios `Hp, Row, Anon, Bike, Row`; 10 s vencidos (TV, W1280) |
| 07 | Lo mismo + pulsómetro de prueba en L640 y TH |
| 08 | `FailNextScan(BleException("scan", "error 2"))` |
| 09 / 10 / 11 | Tocar `Row`: `Connecting`; `TakingControl` (tras `0x00`); `Connected` + 240 ms |
| 12 | `Row` en `NeverRespond`, 15 s |
| 13 | `Row` conectado + paquete `ftms.json` (TV 2 col., W1280 3 col.) |
| 14 | `DropLink(Row)` y t = 1 500 ms de la espera |
| 15 / 16 | Entrenamiento libre + `DropLink`: reconectando (W1280, TV); `Respond` → `Reconnected to AMS-670B` en (W1280) |
| 17 / 18 | 6 s sin paquetes tras el primero: Dispositivo (TV); HUD (W1280) |
| 19 / 20 | Gesto + panel con `Row` y paquetes (TV, W1280); `Bike` sin paquetes (TV) |
| AUX-10a-01 / -02 | HUD de TV en el peor caso de la 4b + aviso con el contorno de depuración; cuadros 0, 240, 480 y 720 de "se despierta" |

### 8.5 Herramienta manual (`tools/ble-probe`, .NET 8, `net8.0-windows`)

`dotnet run --project tools/ble-probe -- [--seconds 10] [--show] [--selftest]`: P/Invoke a `Assets/Plugins/Windows/x86_64/RowMateBle.dll` (referencia el proyecto de Core para `BleEventReader` y `ScanFilter`). Muestra el estado del adaptador y, al terminar, **solo** los equipos que anuncian `0x1826` (FTMS) o `0x180D` (pulso): nombre, tipo, RSSI y dirección enmascarada (`--show` la muestra entera, para la demo del usuario). Nunca conecta, empareja ni cambia la radio; no escribe archivos. `--selftest` (lo usa QA): init, estado, 3 s de búsqueda, stop y shutdown, e imprime solo contadores. Es el S-1 que había quedado pendiente.

---

## 9. Paquetes de trabajo (archivos disjuntos)

| WP | Dueño | Qué | Hecho cuando |
|---|---|---|---|
| **WP-A** | Architect | §1.1: puerto, `RowerService`, puente puro y formato, sesiones y permiso de `NativeBleAdapter`, `BleEventClock` y la bomba, `RmBleJniSignatures`, `RowerLink`, `DeviceStore`/reglas/specs, `LinkNotice`, gesto, `BleHex`, `BleCadence`, `SanitizedDistance`, `IHaptics`, `MainThreadQueue`, 12 claves, dobles de prueba y `events-v1.bin` | `run-tests.ps1` en verde con todos los `d` de §8.3 y los goldens de 4a/5 intactos |
| **WP-B1** | Architect | §1.2 sin capturas: DLL + `build.ps1` nativo (con `/DWINRT_NO_SOURCE_LOCATION` y las dos revisiones de rutas), `WindowsBleApi`, `BleAdapterFactory`, composición, `AppRoot`, vistas funcionales con estilos provisorios, `ToastLayer.ShowLink`, `ble-probe`, `-BleSmoke`, `ROWMATE_BLE=off` en los scripts | DLL compilada y revisada (`-Check`), contrato, `ble-probe --selftest` y `-BleSmoke` en verde en la PC; PlayMode `e` en verde |
| **WP-C** | Architect | §1.3: Kotlin, `.androidlib`, `AndroidBleApi`, permisos, `AndroidHaptics`, `check.ps1`, `jni-signatures.txt` | `check.ps1 -Golden` en verde (con `javap`); `AMT` y `JniSignatureTests` en verde; el puente C# compila en el `run-unity-tests.ps1` de Windows; resto NO VERIFICABLE |
| **WP-D** | Designer | §1.4: `BleMotionSpecs`, token `mono` + `FontAsset`, USS, `SonarView`, `BluetoothIllustration`, `LinkIndicator`, `BleMotion`, reducir movimiento; `04-design-notes.md` | Las vistas de WP-B1 con el arte; `TokensUssSyncTests` en verde |
| **WP-B2** | Architect | `BleCatalog`, 20 CAP + 2 AUX, dos corridas idénticas, `BleAllocationTests`, `HudBudgetTests`, build normal y `simulator` en `Builds/Windows/` | Capturas idénticas; 0 B por cuadro; builds en verde |

WP-B1 y WP-C solo comparten los nombres `AndroidBleApi` y `AndroidBlePermissions` (WP-B1 los instancia en la rama `#if UNITY_ANDROID` de `BleAdapterFactory`, el único `#if`; los archivos se compilan siempre, DT10-19).

---

## 10. Integración del arte y la spec

### 10.1 Pedidos al Architect (00-arte-bluetooth §11)

| ID | Decisión |
|---|---|
| A10-01 | **Adoptado**: `RowerLink` (fase, reconexión con `NextAttemptAt`, motivo), con tests `d` |
| A10-02 | **Rechazado para esta iteración** (P2): barras de RSSI y tipo de máquina van a issues; el ícono es siempre `rowing` (paridad). El RSSI ya viaja en `BleScanResult` |
| A10-03 | **Adoptado** (WP-D), con `BleMotionSpecs` y sin banda con alto compacto |
| A10-04 | **Adoptado**: clave estable por dirección (`ScanItemSpec.Key`), crecer y colapsar con la regla del empuje |
| A10-05 | **Adoptado**: retención de 240 ms solo en la vista; `Connected` no espera |
| A10-06 | **Adoptado**: `IHaptics` (Core/Ports), `AndroidHaptics` sin `VIBRATE`, `NoHaptics`, `FakeHaptics` cuenta |
| A10-07 | **Adoptado**: `BluetoothStateView` + `BluetoothIllustration`, primer estado en los primeros 500 ms sin animar; título en 18 negrita (DT10-13) |
| A10-08 | **Adoptado en Dispositivo, cambiado en el HUD**: manda AC-BLE-08-06 ("reutiliza el lugar de los avisos de 4 s, sin sumar otro"): es el `#toast` del `ToastLayer` con `LinkIndicator`, sin franja A/B ni elemento bajo `#immersive`; la regla de no tapar métricas se prueba en los cinco perfiles (§6.3). `{name}` aparte con `…` (DT10-14) |
| A10-09 | **Rechazado** (a issues): la escena sigue con la última lectura, como el legacy y Q-34; la inercia toca el contrato de la escena |
| A10-10 | **Adoptado** (WP-D): token `mono` y `FontAsset` de Mono, con `GeneratedAssetGuard` |
| A10-11 | **Adoptado**: alto mínimo de 2 líneas, corte en espacios, presión de 500 ms y punto que late; hex armado una vez por paquete |
| A10-12 | **Adoptado**: capturas fijas idénticas con y sin reducir movimiento (`DBT.ReducedMotionSameStill`) |
| A10-13 | **Adoptado**, con el marcador `RowMate.Toast.LinkNotice` en lugar de `RowMate.Hud.LinkNotice` |

### 10.2 Desvíos y aclaraciones

- El aviso del HUD va en el lugar del aviso común, no en una franja propia (manda la spec sobre D10-08 y D10-09).
- Reintentos a +3 s de cada fallo y salteados sin adaptador `On` (DT10-02); conexión solo a ids conocidos (DT10-04); direcciones enmascaradas en el registro (DT10-05).
- `deviceRowerFallback` y `deviceUnknown` reemplazan la constante `DeviceScreenRules.FallbackRowerName`.

---

## 11. Riesgos

| ID | Riesgo | Mitigación |
|---|---|---|
| R-01 | El Gradle de Unity no compila Kotlin como esperamos (o pide el plugin de Kotlin en el `.androidlib`) | Kotlin verificado con `kotlinc`; si Unity no lo toma, se pasa a un `.aar` compilado por `check.ps1 -Aar` (mismo código); NO VERIFICABLE hasta el módulo |
| R-02 | La DLL queda cargada en el Editor y no se puede reemplazar | `build.ps1` nativo se niega con el Editor abierto; `rm_ble_shutdown` al recargar el dominio |
| R-03 | Windows tarda en avisar la caída del enlace (tiempo de supervisión) | El watchdog de 5 s cubre el monitor apagado; la reconexión arranca con el aviso de `ConnectionStatusChanged` |
| R-04 | En Windows el nombre y el UUID llegan en paquetes distintos | Fusión por dirección en la DLL (DT10-07) |
| R-05 | Android: límite de 5 búsquedas cada 30 s y errores GATT 133 | La UI no permite búsquedas encimadas; la reconexión reintenta sola |
| R-06 | Desfase entre el reloj nativo y el de la app en el alcance | Misma base de reloj (DT10-18); desfase = máximo de las muestras de primer plano, congelado al volver (§5.2); `RBG.*` y `BEC.*` con la bomba real y `FakeNativeBleApi` |
| R-11 | El falso de permisos no reproduce un caso real de Android (p. ej. el sistema deja de mostrar el diálogo sin `DontAskAgain`) | `RequestEnableAsync` abre los ajustes de la app ante cualquier negativa sin diálogo; demo de Android, paso 2 |
| R-12 | `UnityEngine.AndroidJNIModule` no compila en el Editor de Windows como se espera | Se ve en el primer `run-unity-tests.ps1` de WP-C; si falla, `#if UNITY_ANDROID \|\| UNITY_EDITOR` en esos tres archivos y se anota |
| R-07 | La regla nueva de distancia cambia algún golden de la 4a o la 5 | WP-A corre los goldens primero; si cambia uno, se reporta con el vector antes de seguir |
| R-08 | El aviso del enlace tapa una métrica en algún perfil | Test en los cinco perfiles y plan B de obstáculo (§6.3) |
| R-09 | El AMS-670B no notifica en pausas, no reinicia contadores o manda la distancia cada > 1 s | `BleCadence` en el registro; issues listos (§12) |
| R-10 | Un escaneo real en la PC muestra equipos ajenos | Core los descarta antes de registrar; `ble-probe` solo lista FTMS y pulso; las capturas usan el falso |

---

## 12. Para issues

- Barras de RSSI e ícono por tipo de máquina en la lista (A10-02, D10-12).
- Inercia de la escena con el enlace caído (A10-09).
- Tope del descubrimiento y de la toma de control (el legacy solo acota `connect`).
- Detectar un adaptador de Windows que se conecta después de abrir la app (hoy se relee al buscar).
- Servicio en primer plano de Android si la demo muestra que el sistema mata el proceso (DT10-12).
- Pistas por plataforma, "Búsqueda terminada", sonido al quedar `Listo` y vibración al reconectar (spec §9, arte §12).
- Distinguir pausa de monitor apagado y revisar totales si el AMS-670B no reinicia contadores (según `BleCadence`).

---

## Cambios por gate

[03-qa-gate](03-qa-gate.md): se corrigen los 5 mayores. Los menores (QA-G10a-06 a -23) se resuelven al implementar, salvo lo que ya tocaron estos cambios (se anota).

| Issue | Qué cambió | Dónde |
|---|---|---|
| QA-G10a-01 | `cl` con `/DWINRT_NO_SOURCE_LOCATION` (verificado en `winrt/base.h:200-216` del SDK 10.0.26100: con la macro, `WINRT_IMPL_BUILTIN_FILE` es `nullptr`). Revisión previa de las fuentes propias (sin `__FILE__`, `__builtin_FILE`, `source_location` ni `assert(`; código 3) y la del binario suma `\Windows Kits\`, en ASCII y UTF-16, sin forma de saltearla. `NHY` lo repite | §3 (Compilación), §8.3 SEC-01, §9 WP-B1 |
| QA-G10a-02 | Kotlin lee `getState()` y nunca emite `Unauthorized`; el permiso es lógica de Core (`IBlePermissions`, `BlePermissionState`), con `Unauthorized` solo tras una negativa recordada en `SharedPreferences("rm_ble")` (DT10-15). `RowerService` pide el permiso **antes** de la espera de 5 s (`EnsureScanPermissionAsync`, DT10-16). `RequestEnableAsync` pide primero `BLUETOOTH_CONNECT`; `DeniedForever`, sin diálogo o `SecurityException` (`-2`) → ajustes de la app. Al volver al frente se relee el permiso. Tests `RSS.FirstLaunchShowsScanNotDenied`, `RSS.ScanAsksPermissionBeforeAdapterWait`, `NBA.NotAskedIsNotUnauthorized`, `NBA.RequestEnable*`, `NBA.ResumeRereadsPermission` | §0.1, §1.1, §2.1, §2.2, §2.6, §4.2, §4.3, §8.2, §8.3 |
| QA-G10a-03 | `ConnectAsync` lleva `CancellationToken`: el plazo de 15 s / 10 s lo cancela y el adaptador llama `api.Disconnect(id)`. También cierran la sesión: el `LinkLost` (antes de emitirlo) y un `RequestDone` tardío de una generación superada. En lo nativo, `connect` sobre un id con sesión la cierra primero y `disconnect` cancela un `connect` pendiente (Windows y Kotlin). Tests `RSC.TimeoutCancelsAdapterConnect`, `NBA.TimeoutCancelsNativeConnect`, `NBA.LateConnectAfterTimeoutIsClosed`, `NBA.LinkLostClosesSession`, `NBA.RetriesDoNotLeakSessions` (`LiveSessions ≤ 1`) | §0.1 DT10-17, §2.1, §2.2, §2.4, §2.6, §3, §4.2, §8.2, §8.3 |
| QA-G10a-04 | La bomba pasó a Core (`IBleEventPump` de `NativeBleAdapter` + `BleEventClock`; ya no hay `Platform/Ble/BleEventPump`). El desfase es el máximo de las muestras de primer plano y queda **congelado** en el drenaje de la vuelta; se reinicia con `Woke`. Kotlin pasa a `System.nanoTime()` (reloj inyectado en `EventRing`, que además deja de depender de `android.*`: cubre también QA-G10a-14). Decisión de RA-19 para la 10a: se queda el `Stopwatch`, y se mide en el teléfono (DT10-18). Tests `RBG.*` con `FakeNativeBleApi` + `LifecycleHarness` y `BEC.*` | §0, §0.1 DT10-18, §1.1, §1.2, §2.5, §4.2, §5.1, §5.2, §8.2, §8.3, R-06 |
| QA-G10a-05 | `AndroidBleApi`, `AndroidBlePermissions` y `AndroidHaptics` se compilan en todas las plataformas; el `#if UNITY_ANDROID` queda solo en `BleAdapterFactory` (DT10-19). Las firmas JNI salen de `Core/Ble/Native/RmBleJniSignatures`; `JniSignatureTests` (dotnet) las compara con `native/android/golden/jni-signatures.txt`, y `check.ps1` regenera ese archivo con `javap -s` y falla con código 4 si difiere. Riesgo R-12 si el módulo JNI no compila en el Editor | §0.1, §1.3, §4.3, §4.5, §5.1, §9, R-12 |

Sin cambios en el QUÉ ni en el arte: AC-APP-13-02, AC-BLE-04-04 y los pasos 1 y 2 de la demo de Android ya pedían lo que ahora hace el diseño; 01-po-spec y 00-arte-bluetooth no se tocan.
