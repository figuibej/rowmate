# It. 11 — Bluetooth real: pulsómetro (diseño técnico)

| Campo | Valor |
|---|---|
| Autor | unity-architect |
| Fecha | 2026-11-08 |
| Iteración | 11 · `11-ble-pulsometro` ([ROADMAP §It. 11](../../ROADMAP.md)) |
| Entrada | [01-po-spec](01-po-spec.md) (41 AC, LT-139 a -145, CAP-01 a -16, DV-106 a -115); [00-arte-pulsometro](00-arte-pulsometro.md) (D11-01 a -13, A11-01 a -08); [10a/02-tech-design](../10a-ble-android-windows/02-tech-design.md) (puerto `IBleAdapter`, puente nativo, DT10-01 a -19) |
| Legacy leído (solo lectura, `3ae0f3a`) | [rowmate-legacy](https://github.com/figuibej/rowmate-legacy) (`../rowmate-legacy`): `lib/core/bluetooth/heart_rate_service.dart` y `reconnect_loop.dart` enteros; `lib/features/workout/workout_provider.dart:60-75`, `:220-237`. No se leyó el módulo de credenciales. `git status --porcelain` del legacy, vacío |
| Código leído (este repo) | `Core/Ble/{IBleAdapter,IBlePeripheral,IHeartRateService,HrmStatus,HeartRateParser,ScanFilter,RowerService,Unavailable/*}`, `Core/Ble/Native/NativeBleAdapter.cs`, `Core/Workout/HeartRateFusion.cs`, `Core/Ports/IPreferences.cs`, `App/Device/{HeartRateState,HeartRateStore,HrmCardRules,HrmCardViewModel,LinkNotice,LinkNoticeText}.cs`, `App/Composition/{AppComposer,AppContext,DeviceBackend}.cs`, `Bootstrap/{HardwareBackend,DeviceBackendFactory,AppRoot}.cs`, `Presentation/Shell/ToastLayer.cs`, `Presentation/Workout/WorkoutScreenView.cs`, `Tests/Support/{FakeBleAdapter,FakeBleDevices,FakeScheduler}.cs`, `Tests/EditMode/Workout/WorkoutProviderHrTests.cs`, plugins `Assets/Plugins/Android/RowMateBle/{RmBle,ScanSession,GattQueue}.kt` y `native/windows/RowMateBle/rm_ble.cpp`, `tools/unity/capture-snapshots.ps1` |
| Estado | Diseño compacto, **sin código**, corregido por el [gate](03-qa-gate.md) (§12). Orden: **WP-A** → (**WP-B1** ∥ **WP-D**) → **WP-B2** (§8) |

Convenciones: rutas relativas a `Assets/RowMate/` salvo `Assets/Plugins/`, `native/`, `tools/` y `docs/`. **N** nuevo, **C** cambia. `hrs` = `heart_rate_service.dart`, `rl` = `reconnect_loop.dart`, `rlt` = `test/reconnect_loop_test.dart`, `wp` = `workout_provider.dart`.

---

## 0. Resumen

1. **Todo es Core puro y corre con dotnet.** `HeartRateService : IHeartRateService` porta `hrs` sobre el **mismo** `IBleAdapter` del remo, con `ReconnectLoop` y `StaleWatchdog` (`rl`) sobre el `IScheduler`. Se prueba con `FakeBleAdapter` + `FakeScheduler` + `MemoryPreferences` y los sensores ficticios `K-11`.
2. **Plugins nativos: ningún cambio.** La búsqueda recibe los servicios como CSV (Android arma un `ScanFilter` del sistema por cada UUID, `ScanSession.kt:27-31`; Windows no filtra y filtra Core), `connect(id, highMtu)` ya admite `highMtu = false` (Android no pide MTU, `GattQueue.kt:224`; Windows lo ignora), y conectar por dirección sin buscar ya existe (`FromBluetoothAddressAsync`, `getRemoteDevice`). Las notificaciones de `0x2A37` usan el mismo CCCD genérico. No se recompila la DLL ni el Kotlin. `native/windows/build.ps1 -Check` y `native/android/check.ps1 -Golden` se corren igual, para confirmar que no cambió nada.
3. **App**: `HeartRateStore` suma el estado del enlace (`HrmLink`: fase de reconexión, motivo, lectura vencida), el **tipo** de error y su detalle, y los **avisos** (`Pulsómetro conectado`, `Se perdió el pulsómetro, reconectando…`). `HrmCardRules` arma los estados nuevos de la tarjeta.
4. **Composición**: `HardwareBackend.Create` crea `HeartRateService` sobre el adaptador nativo y con las preferencias de la app. Para eso `AppRoot` lee `prefs.json` **antes** de armar el backend. El build `simulator` no cambia: no ve las preferencias ni escribe `hrm.*`.
5. **Vistas**: la tarjeta suma un indicador (arco, spinner, `link_off`), la línea `Sin señal del sensor`, el error en dos renglones y la respiración del corazón. Con alto compacto, la vista de búsqueda pasa a dos columnas o a un único desplazamiento (cierra m-06 de la 10a). Los avisos van en la cápsula del `ToastLayer`, con prioridad del remo. El HUD no suma elementos: queda en 59.

### 0.1 Decisiones del Architect (una línea cada una)

| ID | Decisión |
|---|---|
| DT11-01 | Plugins sin cambios (§0, punto 2). Si la demo muestra que Windows no encuentra una dirección recordada sin buscar, va a issues (R-01), no a esta iteración |
| DT11-02 | `StaleWatchdog` por **plazo**: `Touch` mueve `_deadline` y deja un solo temporizador, que al vencer se reagenda por lo que falta. La semántica es la de `rl:75-91`, sin asignar memoria en cada lectura (AC-HRM-04-06) |
| DT11-03 | `IHeartRateService` suma `Link` como **miembro con implementación por defecto** (`HrmLinkSource.Idle`), como `IRowerService.Link` en la 10a. El simulado y `Unavailable` no cambian, así que el simulador no muestra avisos del pulsómetro |
| DT11-04 | Q-41: el servicio devuelve `""` cuando hay un sensor recordado o conectado sin nombre (`null` = no hay sensor). `HrmCardRules` cambia `""` por `hrmUnknownSensor`. Ya no aparece la dirección como nombre (`hrs:94`, `:98`) |
| DT11-05 | Al recordar: nombre = `Name` del periférico, o el anunciado en esta búsqueda, o ninguno. **Otro id sin nombre** → `Remove("hrm.deviceName")` (Q-41). **El mismo id sin nombre**, como al reconectar sin buscar, conserva el nombre guardado; si no, cada arranque lo borraría |
| DT11-06 | AC-HRM-SEC-01: `ConnectAsync(id)` solo acepta un id que llegó en una búsqueda del pulsómetro de esta ejecución o el recordado. Cualquier otro lanza `ArgumentException`, como DT10-04 |
| DT11-07 | Un intento reemplazado se cierra solo (cancela su conexión o descarta su periférico), **salvo** que la acción nueva apunte al mismo id (`hrs:286-291`). En ese caso no cancela su `ct`, porque el adaptador cerraría por id el intento nuevo (DT10-17). El `connect` nativo sobre el mismo id ya reemplaza la sesión vieja |
| DT11-08 | Buscar pide el permiso primero (`EnsureScanPermissionAsync`, el mismo que el remo, AC-HRM-16-02). Si se niega: desconectado y sin error (DV-100). Sin esperar 5 s a `On`: el legacy no espera. Con el adaptador sin `On`, `BleException("scan", "adapter off")` → error de búsqueda. No se ve en la app, porque la tarjeta se oculta |
| DT11-09 | Apagar el Bluetooth con el sensor conectado cuenta como **caída** (como QA-G10a-11 del remo). A diferencia del remo (DT10-02), las cadencias **no se saltean** con el adaptador apagado: intentan y fallan a su ritmo (DV-111, DV-115, paridad) |
| DT11-10 | En el registro, las direcciones van enmascaradas con `BleIds.Mask` (DT10-05): `[HRM] Encontrado AA:BB:…:0D "Banda de prueba" rssi=-58 uuids=[180d]`. El nombre va completo, solo en el `Player.log` local |
| DT11-11 | Fusión con paridad (AC-WKT-05-07 corregido por QA-G11-01): en una **caída**, `wp:232-237` suelta el sensor, el pulso pasa a 0 y el próximo paquete FTMS repone el del monitor (LT-36). Si el monitor no manda pulso (AMS-670B), sigue en 0 y la tarjeta BPM desaparece. Con el **watchdog** el sensor sigue conectado: el pulso queda en 0 y el FTMS no lo pisa (`_hrmBpm = 0`, no `null`). `HeartRateFusion` no cambia |
| DT11-12 | Avisos: `HeartRateStore.Notices` emite `Connected` cuando el estado pasa a conectado con `ConnectedBy ∈ {Startup, Drop}`, y `Lost` en cada **corrida nueva** de la cadencia de caída. Los muestra `HrmNoticePresenter` (Shell): `Connected` en **cualquier** pestaña, también Dispositivo (manda la spec, CAP-12, sobre D11-11); `Lost` solo con el entrenamiento a la vista (el mismo predicado que el aviso del remo) |
| DT11-13 | Prioridad en el `ToastLayer` (QA-G11-03): `ShowHrm` **cede solo ante un aviso del remo**, a la vista (`CurrentLink`) o fijo pendiente (`StickyLink`): descarta (`false`, línea en el registro). Un aviso anterior **del pulsómetro** o uno común a la vista se **reemplaza** (`Connected` pisa a `Lost` y arranca sus 2 s). El aviso del pulsómetro **no** ocupa `_link`/`CurrentLink`: va en su propio campo (`CurrentHrm`). Un aviso del remo o uno común reemplaza al del pulsómetro (DV-14) |
| DT11-14 | Disposición compacta D11-06 **adoptada**: CAP-03 exige "sin desborde" y cierra m-06. La regla es Core (`DeviceScanLayout`), con tests en los cinco perfiles |
| DT11-15 | El error lleva el **tipo** según la acción: `Scan` (Buscar), `Connect` (conectar, `Conectar`, `Desconectar`, `Olvidar`) o `Incompatible` (según el tipo de excepción), más `ErrorName` (el nombre al empezar la acción) y `ErrorDetail` (`Message`, sin traducir). Un `Desconectar` u `Olvidar` que falla muestra `No se pudo conectar con {name}` (aceptado; el legacy muestra "Error al conectar", m-07) |
| DT11-16 | `-BleSmoke` suma una búsqueda de pulsómetros de 10 s con `HeartRateService`, sin conectar a nada. Imprime solo contadores: `[HRM] humo: busquedas=1 sensores=N ajenos=0 fallas=0` (AC-HRM-02-06) |

---

## 1. Árbol de archivos

### 1.1 WP-A: Core y App puros (dotnet)

```
Core/Ble/ReconnectLoop.cs  StaleWatchdog.cs            N  puerto de rl (§2.1)
Core/Ble/HeartRateService.cs  HrmTimings.cs            N  puerto de hrs (§2.2-§2.6)
Core/Ble/HrmLink.cs                                    N  HrmLink, HrmRetryReason, HrmConnectOrigin, HrmLinkSource
Core/Ble/IHeartRateService.cs                          C  + Link (miembro con implementación por defecto)
Core/Localization/L10nKeys.cs                          C  5 claves (245)
Localization/strings.es.json  strings.en.json          C
App/Device/HeartRateState.cs  HeartRateStore.cs        C  Link, ErrorKind/ErrorName, Notices (§3.1)
App/Device/HrmErrorKind.cs  HrmNotice.cs  HrmNoticeText.cs  HrmNoticeRules.cs  N
App/Device/HrmCardRules.cs  HrmCardViewModel.cs        C  estados 6-8, indicador, error en dos renglones (§3.2)
App/Device/HrmIndicator.cs                             N  None, Spinner, WaitingArc, LinkOff
App/Device/DeviceScanLayout.cs                         N  Stacked | SingleScroll | TwoColumns (§4.2)
Tests/Support/FakeBleAdapter.cs  FakeBleDevices.cs     C  K-11 y fallas por paso (§6.2)
Tests/EditMode/Ble/ReconnectLoopTests.cs  StaleWatchdogTests.cs  N  LT-139 a -145
Tests/EditMode/Ble/HeartRateService{Scan,Connect,Reconnect,Memory,Actions,Watchdog,Coexist}Tests.cs  N
Tests/EditMode/Device/HeartRateStoreTests.cs  HrmCardRulesTests.cs  C;  HrmNoticeRulesTests.cs  DeviceScanLayoutTests.cs  N
Tests/EditMode/Workout/WorkoutProviderHrTests.cs       C  LT-35 a -38 también sobre K-11 + AC-WKT-05-07
Tests/EditMode/Composition/AppComposerTests.cs         C
Tests/EditMode/Traceability/It11Ids.cs                 N
```

### 1.2 WP-B: integración, vistas, arnés y builds (Unity)

```
Bootstrap/HardwareBackend.cs  AppRoot.cs  BleSmoke.cs   C  (§5)
Presentation/Device/HeartRateCardView.cs               C  estructura de los estados nuevos (§4.1)
Presentation/Device/ScanView.cs  DeviceScreenView.cs   C  disposición compacta (§4.2)
Presentation/Shell/ToastLayer.cs                       C  ShowHrm + prioridad (§4.3)
Presentation/Shell/HrmNoticePresenter.cs  ShellHost.cs N/C
Presentation/Workout/WorkoutScreenView.cs              C  expone ShowsLinkNotices al presentador
Snapshots/HrmCatalog.cs  SnapshotCatalog.cs            N/C  conjunto it11 (§7)
Tests/UiSupport/HostBackends.cs                        C  FakeBle(...) con HeartRateService + MemoryPreferences
Tests/PlayMode/HrmCardRealTests.cs  HrmToastTests.cs   N
Tests/PlayMode/DeviceBleTests.cs  BleAllocationTests.cs  HudLinkNoticeTests.cs  C
tools/unity/capture-snapshots.ps1  tools/unity/README.md  C  -Set it11 → docs/…/11-ble-pulsometro/snapshots
```

Plugins nativos (`native/`, `Assets/Plugins/`): **ninguno** (DT11-01).

### 1.3 WP-D: arte

```
Presentation/Controls/LinkIndicator.cs                 C  modos Heart (con un latido de entrada) y HeartBroken
Core/Design/BleMotionSpecs.cs                          C  constantes HRM (respiración, línea sin señal, filas, check)
Presentation/Motion/HrmMotion.cs                       N  coreografías de la tarjeta y reducir movimiento
Presentation/Styles/Skin.uss  Motion.uss               C  .rm-hrm-card__identity, __stale, __error-detail, columnas
docs/…/11-ble-pulsometro/04-design-notes.md            N
```

---

## 2. Core (WP-A)

### 2.1 `ReconnectLoop` y `StaleWatchdog` (`rl:8-91`)

```csharp
public sealed class ReconnectLoop : IDisposable
{
    public ReconnectLoop(IScheduler scheduler, TimeSpan interval, Func<Task<bool>> attempt, IAppLogger logger,
                         int? maxAttempts = null, Action? onGiveUp = null);
    public bool IsRunning { get; }          // rl:29, también durante las esperas
    public bool IsAttempting { get; }       // hay un intento en vuelo de la corrida actual
    public int Attempts { get; }            // rl:30
    public double? NextAttemptAt { get; }   // hora del IScheduler del próximo intento (solo esperando)
    public int Run { get; }                 // sube en cada Start (para detectar "corrida nueva")
    public event Action? Changed;           // Start, intento, espera, fin, rendición, Cancel
    public void Start();                    // rl:33-38: Cancel + intento inmediato
    public void Cancel();                   // rl:42-47: generación++, timer fuera, sin abortar el vuelo
    public void Dispose();                  // = Cancel
}
public sealed class StaleWatchdog : IDisposable
{
    public StaleWatchdog(IScheduler scheduler, TimeSpan timeout, Action onStale);
    public bool IsArmed { get; }
    public void Touch();                    // _deadline = now + timeout; un solo temporizador (DT11-02)
    public void Stop();
    public void Dispose();
}
```

`_tryOnce` es línea por línea `rl:49-70`: corrida vieja o cancelada → descarta; una excepción cuenta como fallo; la espera de `interval` empieza **al terminar** el intento; `onGiveUp` puede llamar `Start` (LT-144). Al disponer, no queda temporizador vivo (`FakeScheduler.PendingTimers == 0`).

### 2.2 `HeartRateService : IHeartRateService`

```csharp
public sealed record HrmTimings(TimeSpan Scan, TimeSpan Connect, TimeSpan Stale, TimeSpan StartupInterval,
                                int StartupMaxAttempts, TimeSpan DropInterval)
{ public static readonly HrmTimings Legacy = new(10 s, 15 s, 10 s, 10 s, 6, 3 s); }   // hrs:38-45, :108, :245

public enum HrmRetryReason { None, Startup, Drop }
public enum HrmConnectOrigin { None, Manual, Startup, Drop }
public sealed record HrmLink(ReconnectPhase Reconnect, HrmRetryReason Reason, int Run, double? NextAttemptAt,
                             TimeSpan Wait, bool IsStale, HrmConnectOrigin ConnectedBy)
{ public static readonly HrmLink Idle; }          // HrmLinkSource.Idle = observable constante

public sealed class HeartRateService : IHeartRateService
{
    public const string Tag = "HRM";
    public const string PrefDeviceId = "hrm.deviceId", PrefDeviceName = "hrm.deviceName";   // hrs:34-35
    public HeartRateService(IBleAdapter adapter, IPreferences preferences, IScheduler scheduler, IAppLogger logger,
                            HrmTimings? timings = null);
    // IHeartRateService sin cambios + Link
}
```

Estado interno: `_connectGen` (`hrs:66-69`), `_scanGen` + `_scanDone` (un `CancellationTokenSource`, `hrs:114`, `:179-183`; ver `EndScanWait` en §2.3), `_attempt` (`HrmAttempt { Id, Cts, Peripheral?, NotifySub?, LinkLostSub? }`, que equivale al `_device` + subs del legacy), `_rememberedId` y `_rememberedName`, `_known` (ids de las búsquedas, DT11-06), `_isDisconnecting`, `_startupLoop`, `_dropLoop` y `_watchdog`. Todo en el hilo principal: los eventos del plugin llegan por la bomba de la 10a (`AppRoot.Update`), así que una lectura nativa ya llega publicada en el hilo principal.

`IsRetrying = _startupLoop.IsRunning || _dropLoop.IsRunning` (`hrs:100`). `ConnectedDeviceName`: `null` si no está conectado; si no, `Name` del periférico, o el recordado, o `""` (DT11-04). `RememberedDeviceName`: `null` sin id; si no, el nombre guardado o `""`. `Link` se publica con cada `Changed` de los lazos, al disparar el watchdog, con una lectura válida tras el disparo y al conectar o limpiar.

### 2.3 Búsqueda (`hrs:108-183`)

| Paso | Comportamiento | AC |
|---|---|---|
| Arranque | `_connectGen++`, `CancelLoops()`, `AbortPendingConnect()` **antes del primer `await`** (`hrs:110-112`). Con `Unsupported`, vuelve. Todavía sin `await`: `EndScanWait()` (termina la búsqueda anterior, que ya no es dueña de nada), `var gen = _scanGen; _scanDone = new CTS()`. Pide el permiso; si se niega y `gen` sigue vigente: `_scanDone = null`, `StopScanAsync` del adaptador si la búsqueda vieja buscaba, desconectado y sin error (DT11-08). `Status = Scanning`, `Devices.Emit([])`. Una búsqueda anterior **no** pasa por `Disconnected`: el adaptador la reinicia con el `StartScanAsync` nuevo | 02-01 |
| Escaneo | `adapter.StartScanAsync([0x180D])` y log `[HRM] Escaneando sensores 0x180D durante 10s`. Se suscribe a `ScanResults` y descarta lo que sea de otra `_scanGen`; `ScanFilter.Accept(r, HeartRateParser.Service, !adapter.FiltersScanInOs)`. Orden del primer descubrimiento, reemplazo en el lugar, `_known.Add`, `_advNames[id] = r.AdvName`, log `[HRM] Encontrado …` solo la primera vez (DT11-10). Emite la lista en cada resultado aceptado | 02-02, 02-05 |
| Fin | `scheduler.Delay(10 s, done.Token)`. `ScanFailed` de esta generación termina en **silencio** en la UI, con log `[HRM] Error en el escaneo: …` (`hrs:147-152`). Si `StartScanAsync` lanza: log `[HRM] Error escaneando: …`, `Status = Disconnected` y relanza: el store muestra el error de búsqueda. En `finally` dispone **su** `done` y, **solo si `gen == _scanGen`** (nadie la terminó desde afuera): `_scanDone = null`, `StopScanAsync` y `Status = Disconnected` solo si sigue `Scanning` (`hrs:162-166`). **No** reanuda la reconexión (Q-43) | 02-01, 02-03 |
| `StopScanAsync` | `hrs:170-177`: si `Status != Scanning`, no toca nada (el escaneo en curso puede ser del remo). Si no: `EndScanWait()`, detiene el escaneo del adaptador y `Status = Disconnected` | 02-04 |

**`EndScanWait()`** (QA-G11-02). `FakeScheduler` cancela en línea: el `finally` de la búsqueda corre **dentro** de `Cancel()`. Por eso el orden es fijo, como `RowerService` (`++_scanGeneration` primero, `RowerService.cs:126`): `_scanGen++`; `var done = _scanDone; _scanDone = null;` y **recién entonces** `done?.Cancel()`. El `finally` viejo ve otra generación y no toca el adaptador, `_scanDone` ni `Status`: lo que haga falta lo hace quien llamó. La usan `StartScanAsync`, `ConnectAsync`, `StopScanAsync`, `DisconnectAsync`, `ForgetAsync` (las dos, solo con `Status == Scanning`) y `Dispose`. En el legacy no hacía falta: `done.complete()` reanuda en una microtarea (`hrs:179-183`).

Exclusión con el remo: la UI no deja buscar con uno y con el otro a la vez (LT-29, LT-31). Además, cada servicio solo escucha `ScanResults` **durante su propia generación**, y el remo filtra FTMS: un pulsómetro nunca entra en la lista del remo, y al revés tampoco (`RSS.K11RowerListNeverShowsHrm`).

### 2.4 Conexión (`hrs:188-319`)

`ConnectAsync(id)`: DT11-06. Si `Status == Connecting`: log `[HRM] connect ignorado: …` y vuelve (`hrs:189-192`). `_connectGen++`, `CancelLoops()`, `var wasScanning = Status == Scanning`, `EndScanWait()` **antes** de cualquier `await` (QA-G11-02), `StopScanAsync` del adaptador si `wasScanning`, `ConnectToAsync(id, Manual)`. `Status` va directo de `Scanning` a `Connecting`, sin `Disconnected` en el medio, y el store conserva el nombre elegido (`HeartRateStore.OnStatus` lo borra con cualquier estado que no sea `Connecting`). Un `Superseded` solo va al registro (`hrs:199-202`).

`ConnectToAsync(id, origin)` (`hrs:229-306`), con `EnsureCurrent()` después de cada `await`:

1. `_isDisconnecting = false`; `Status = Connecting`; `_attempt = new HrmAttempt(id)`.
2. `adapter.ConnectAsync(id, requestHighMtu: false, cts.Token)` con tope de **15 s**: `scheduler.Delay(15 s)` cancela `cts` y da `BleException("connect", "timed out after 15 s")` (`hrs:241-246`; DT10-17). El plazo se dispone apenas `ConnectAsync` termina, bien o mal (m-05): conectado, solo queda pendiente el temporizador del watchdog.
3. `DiscoverServicesAsync` y buscar `0x180D`/`0x2A37` (`hrs:308-319`). Si no está: `DisconnectAsync` en silencio y lanza `HrmIncompatibleException` (`hrs:250-253`).
4. `SetNotifyAsync(0x180D, 0x2A37, true)` y suscripción a `Notifications`, solo `0x2A37` (`hrs:254-258`).
5. `Remember(id, name)` (DT11-05).
6. Suscripción a `LinkLost` (§2.5).
7. `_watchdog.Touch()`, `Link.ConnectedBy = origin`, `Status = Connected` y log `[HRM] Conectado a {nombre}` (`hrs:274-276`).

`catch` (`hrs:277-305`):

- **Reemplazado** (`Superseded` o la generación cambió): suelta sus suscripciones y cierra lo suyo (cancela `cts` o descarta su periférico), salvo que el nuevo dueño sea el mismo id (DT11-07). Log `[HRM] Intento de conexión reemplazado: …` y lanza `Superseded`.
- **Falla**: log `[HRM] Error al conectar: …`, cierra lo suyo en silencio. Si mientras tanto cambió la generación, lanza `Superseded` (AC-HRM-11-02). Si no: `Cleanup()` (pulso a 0 si no lo era), `Status = Disconnected` y relanza. No recuerda nada ni programa reintentos.

`OnMeasurement(n)` (`hrs:321-326`): `HeartRateParser.Parse(n.Value)`. Si es `null`, se descarta sin tocar el watchdog. Si no: `Touch()`; si estaba vencida, `IsStale = false`; emite `contact == false ? 0 : bpm`. Sin asignar memoria: `Bpm` es un `EventStream<int>`, `Parse` trabaja sobre un span y el `Link` solo se rearma al cambiar `IsStale`.

### 2.5 Reconexión, caída y watchdog

- **`AutoConnectAsync`** (`hrs:207-215`): lee la memoria (§2.6). Vuelve sin hacer nada si no hay id, si el estado no es desconectado o si hay una búsqueda del usuario (`_scanDone != null`). Si no: `_dropLoop.Cancel()`, `_startupLoop.Start()` (motivo `Startup`, 10 s, máximo 6, `onGiveUp` reemite el estado) y reemite el estado.
- **Intento** (`hrs:217-227`): sin id → `true`. Si no, `ConnectToAsync(id, motivo del lazo)`: `true`, o log `[HRM] Reintento fallido: …` y `false`. Incompatible también es `false`. Los fallos nunca llegan a la tarjeta.
- **Caída** (`LinkLost` sin `_isDisconnecting`, o el adaptador deja de estar `On` con el sensor conectado, DT11-09): log `[HRM] Conexión perdida, reintentando cada 3s`, `Cleanup()` (primero el pulso a 0), `Status = Disconnected` y `_dropLoop.Start()` (motivo `Drop`, 3 s, sin límite) (`hrs:263-272`).
- **Watchdog** (`hrs:86-89`): se arma al conectar. Al disparar: `IsStale = true` y emite 0 **una vez**. No corta ni reintenta. Se detiene en `Cleanup`, `AbortPendingConnect` y `Dispose`.
- **`Link`**: `Reconnect = Attempting` con el intento en vuelo y `Waiting` con `NextAttemptAt`/`Wait` (10 s o 3 s) en la espera. `Reason` es el del lazo que corre; `Run` es el `Run` del lazo.

### 2.6 Desconectar, Olvidar, memoria y cierre

| Acción | Comportamiento (fuente) | AC |
|---|---|---|
| `DisconnectAsync` | `hrs:337-349`: `_connectGen++`, `CancelLoops()`, con `Status == Scanning` `EndScanWait()` + `StopScanAsync` del adaptador, `_isDisconnecting = true`, `Cleanup()`, cierra el periférico, o la conexión pendiente con `cts.Cancel()` (sin esperar 15 s), `_isDisconnecting = false` (explícito, `hrs:346`, m-10), `ForgetAsync()` y `Status = Disconnected` | 10-01 |
| `ForgetAsync` | `hrs:353-363`: `_connectGen++`, `CancelLoops()`, con `Status == Scanning` `EndScanWait()` + `StopScanAsync` del adaptador + `Status = Disconnected`, `AbortPendingConnect()` (`hrs:372-383`: con `Connecting`, suelta notificaciones, para el watchdog, `Status = Disconnected` y cierra en silencio), borra la memoria y las dos claves, reemite el estado. Una conexión activa no se toca. (La UI no ofrece las dos acciones mientras se busca; si llegan igual, cortan la búsqueda en vez de dejarla huérfana: valor por defecto) | 10-02 |
| Memoria | `LoadRemembered` (`hrs:414-421`): `TryGetString(id)`; el nombre solo vale con id. `Remember` (`hrs:405-412`, DT11-05): `SetString(id)` y, con nombre, `SetString(name)`; si es otro id y no tiene nombre, `Remove(name)` | 06-01 a -03 |
| `Dispose` | `hrs:435-447`: generación++, lazos y watchdog fuera, aborta lo pendiente, `EndScanWait()` (con `Status == Scanning`, `StopScanAsync` del adaptador sin esperar), suelta las suscripciones (incluido el adaptador) y descarta el periférico. `PendingTimers == 0` | 11-03 |

---

## 3. App (WP-A)

### 3.1 `HeartRateStore` y `HeartRateState`

- `HeartRateState` suma `HrmLink Link`, `HrmErrorKind ErrorKind`, `string? ErrorName` y `string? ErrorDetail`, y saca `Exception? Error` (los tests lo leen como `ErrorKind != None`). `IsStale => IsConnected && Link.IsStale`.
- `Run(kind, name, action)`: toda acción captura el tipo (DT11-15) y el nombre al empezar: `ConnectAsync(id, name)` usa `name`; `RetryAsync` usa el recordado. `HrmIncompatibleException` siempre da `Incompatible`. El error se borra al empezar una acción y al conectar, como en la It. 2.
- `IEventSource<HrmNotice> Notices`, con `HrmNotice(HrmNoticeKind Kind, string? Name)` y `Kind ∈ {Connected, Lost}`. `Connected` sale al pasar a `Connected` con `Link.ConnectedBy ∈ {Startup, Drop}`; `Lost`, cuando `Link.Reason == Drop` con un `Run` distinto del último avisado. Se suscribe a `Link` del servicio y no emite después de `Dispose`.
- `HrmNoticeRules.ShouldShow(HrmNoticeKind kind, bool workoutNoticesVisible)`: `Connected`, siempre; `Lost`, solo con `workoutNoticesVisible`. `HrmNoticeText.Toast(notice, texts)` → `HrmToast(Kind, Text, Duration)`, con `Connected` de 2 s y `Lost` de 4 s.
- `HrmNoticeRules.Admit(ToastSlot visible, bool rowerStickyPending) → bool` (QA-G11-03, DT11-13), con `ToastSlot ∈ {None, Common, Rower, Hrm}`: `false` con `Rower` o con `rowerStickyPending`; `true` con `None`, `Common` o `Hrm` (el aviso del pulsómetro reemplaza al suyo). `ToastLayer.ShowHrm` la consulta: la regla queda en App (pura) y se prueba con dotnet. `ToastSlot` va en `HrmNotice.cs`.

### 3.2 `HrmCardRules` y `HrmCardViewModel` (A11-02)

| Campo | Regla | AC |
|---|---|---|
| `HeartFull` | conectado **y** no vencido (con `IsStale`, al 40 %, manda la spec sobre D11-04) | 05-03 |
| `HeartBeating` | conectado, bpm > 0 y no vencido | 13-12 |
| `HeartBreathing` | `Body == Remembered && IsRetrying` (D11-05) | — |
| `StaleText` | `hrmNoSignal` si `IsStale`, si no `null` | 05-03 |
| `Indicator`, `ArcStartAt`, `ArcDuration` | `Remembered`: con reintentos, `WaitingArc` (`NextAttemptAt − Wait`, `Wait`) en la espera y `Spinner` en el intento; sin reintentos, `LinkOff`. En el resto de los cuerpos, `None` | 07-02, -03 |
| `RememberedName` + `RememberedSuffix` | nombre (`""` → `hrmUnknownSensor`) y `" · reconectando..."` o `" · no encontrado"` en **dos etiquetas de la misma fila**: se lee exactamente `{nombre} · sufijo` y el `…` corta solo el nombre (D11-02 adaptado a la spec) | 07-02, -03, 13-13 |
| `ConnectedName`, `ConnectingText` | `""` → `hrmUnknownSensor` (Q-41) | 06-02 |
| `ErrorHuman` + `ErrorDetail` | `Scan` → `hrmScanFailed`; `Connect` → `hrmConnectFailed{name}` (`ErrorName`, o el recordado, o `hrmUnknownSensor`); `Incompatible` → `hrmIncompatible` sin detalle. El detalle va sin traducir. `hrmConnectError` queda en el catálogo, sin uso en la tarjeta | 13-14 |
| `SearchEnabled`, `ConnectEnabled` | sin cambios (`hc:22-24`, `:212`, `:218`) | 02-04 |

---

## 4. Vistas (WP-B1 la estructura y el comportamiento; WP-D el aspecto y el movimiento)

### 4.1 Tarjeta del pulsómetro

```
#hrm-card (sin cambios arriba)
├── header: #hrm-heart (opacidad: 1 / 0,40 / respiración) + #hrm-title
├── #hrm-body
│   ├── Remembered: .rm-hrm-card__identity > #hrm-indicator (LinkIndicator 16) + #hrm-remembered-name (…) + #hrm-remembered-suffix
│   │               + acciones hrm-retry / hrm-search-other / hrm-forget (paridad)
│   ├── Connected:  nombre · #hrm-bpm "142"/"--" · bpm · hrm-disconnect · #hrm-stale (.rm-hrm-card__stale, solo con StaleText)
│   └── Scanning / Connecting / NoSensor: sin cambios (filas hrm-device-<id> con clave estable = id)
└── #hrm-error > #hrm-error-human (state.dangerText 12, hasta 2 líneas) + #hrm-error-detail (text.tertiary 11, 1 línea con …)
```

- Check con retención de 240 ms antes de cambiar el cuerpo a `Connected`, solo en la vista (A10-05): `Status` y el HUD ven el sensor conectado en el mismo cuadro. La respiración y el arco animan solo con Dispositivo a la vista.
- Marcador de rendimiento `RowMate.Device.HrmCard`.

### 4.2 Disposición de la vista de búsqueda (A11-05, DT11-14)

`DeviceScanLayout.For(double contentWidthDp, double contentHeightDp)` → `(Kind, RightColumnDp)`:

- Alto ≥ `BleMotionSpecs.CompactHeightDp` (560): `Stacked`, como hoy, en TV, P360 y W1280.
- Alto compacto y ancho ≥ 840: `TwoColumns`, con la columna derecha en `clamp(0,4 W; 360; 440)` (TH).
- Si no: `SingleScroll` (L640), con la lista a su alto natural y la tarjeta debajo, en un único `ScrollView`. Si la tarjeta crece y sale de la vista, el desplazamiento la sigue (240 ms; en el acto con reducir movimiento).

`ScanView` reubica `#device-results` y `#hrm-card-slot-scan` sin recrearlos. El cambio de disposición es en el acto.

### 4.3 Avisos (A11-07, DT11-12, DT11-13)

- `ToastLayer.ShowHrm(HrmToast toast) → bool` (QA-G11-03): cede **solo ante el remo**. `HrmNoticeRules.Admit(Slot, StickyLink != null)` con `Slot = Rower` si `CurrentLink != null`, `Hrm` si `CurrentHrm != null`, `Common` si hay otro aviso a la vista y `None` si no: con `false` vuelve `false`. Si no, `Present(text, duration, link: null, hrm: toast)`: el aviso del pulsómetro va en `_hrm`, **no** en `_link`, así que `CurrentLink` y `StickyLink` siguen siendo solo del remo. Sobre un aviso anterior del pulsómetro hace el `PlayBump` de siempre y reinicia el temporizador con la duración nueva (`Lost` 4 s → `Connected` 2 s desde el reemplazo). `CurrentHrm => _visible ? _hrm : null`; `Present` de `Show`/`ShowLink` pone `_hrm = null`. Clase `.rm-toast--link` e indicador `Heart` (`Connected`) o `HeartBroken` (`Lost`). `ShowLink` y `Show` lo reemplazan.
- `HrmNoticePresenter(UiContext, ToastLayer, Func<bool> workoutNoticesVisible)` lo crea `ShellHost`. El predicado es `WorkoutScreenView.ShowsLinkNotices` (el inmersivo arriba, sin `Completion`, fase `Active` o `Paused`; queda `internal`). Lo que se descarta va al registro: `[HRM] aviso descartado: …`.
- HUD: nada bajo `#immersive` (`HudBudgetTests` = 59). La tarjeta BPM ya desaparece con 0 (Q-46, It. 4b).

---

## 5. Composición y builds (WP-B1)

- `AppRoot.Awake`: `JsonFilePreferences` se crea **antes** de `DeviceBackendFactory.Create(..., preferences, ...)` (paso 4c → 4). La ruta sigue sin ir al registro.
- `HardwareBackend.Create(scheduler, lifecycle, logger, mainThread, preferences, out pump)`: con plugin, `new DeviceBackend(rower, new HeartRateService(adapter, preferences, scheduler, logger), adapter)`. Sin plugin, `Unavailable` (It. 2). El simulador no cambia: `DeviceBackendFactory` sigue siendo el único `#if ROWMATE_SIMULATOR`, y su rama no recibe preferencias (AC-HRM-15-13).
- `AppContext.Start` ya llama `AutoConnectAsync` y deja `[HRM] autoConnect: <error>` en el registro (AC-APP-01-10): sin cambios.
- Orden de `Dispose` del backend (ya existe): pulsómetro, remo y adaptador.
- `BleSmoke` (DT11-16): después de las 3 búsquedas del remo, hace 1 de pulsómetros. Pasa si termina, si `ajenos = 0` (todo lo listado anuncia `0x180D`) y si no conectó (`ConnectCalls = 0`; el falso nativo no aplica). Se corre con `tools/build/build.ps1 -Target Windows -BleSmoke`. Si encuentra un pulsómetro real, su nombre y dirección quedan solo en el `Player.log` de `<scratch>`.
- Builds: `Builds/Windows/normal`, `Builds/Windows/simulator` y `dev-smoke`. Si la carpeta está en uso, se reintenta cada 2 min, hasta 20 veces.

---

## 6. Tests y verificación

### 6.1 Comandos

`powershell -NoProfile -File tools/dotnet/run-tests.ps1 -ArtifactsPath <scratch>/it11`; `tools/unity/run-unity-tests.ps1` y `-Simulator`; `native/windows/build.ps1 -Check` (SHA igual al versionado); `native/android/check.ps1 -Golden`; `tools/build/build.ps1 -Target Windows -BleSmoke`; `tools/unity/capture-snapshots.ps1 -Set it11` dos veces y comparación byte a byte. Ninguna prueba toca `prefs.json` ni las bases del usuario (`ROWMATE_DATA_DIR` temporal, `MemoryPreferences`); los scripts exportan `ROWMATE_BLE=off`. Si el bootstrapper reescribe `HorizonFar.mat` o `HorizonNear.mat`, se revierten.

### 6.2 Doble de prueba (`FakeBleAdapter`, C)

- `FakeBleDevices.K11 = K10 + Hrm, Watch, HrAnon, NoHr`, con los datos de la spec §0: `Watch` anuncia `0x180D` en 128 bits más otro servicio; `NoHr` tiene `0x180D` sin `0x2A37`. Paquetes `HrPackets.{B72, Contact142, U16_142, NoContact, Invalid}`. `RowerDataHr90`: el paquete del oráculo con la bandera de pulso (bit 9) y 90 bpm, para AC-WKT-05-07 y CAP-13.
- Nuevos:
  - `ConnectRequests` (id, `requestHighMtu`).
  - `Steps`: registro ordenado `connect`, `discover`, `notify:<chr>`, `disconnect`.
  - `FailDiscover(id, ex)` y `FailSetNotify(id, ex)`.
  - `ConnectAsync` con el adaptador sin `On` → `BleException("connect", "adapter off")` (DT11-09).
  - `LiveSessions(id)`.
  - La cancelación de un connect pendiente actúa **por pedido** (`reqId`), no por id, como `NativeBleAdapter.CancelConnect` (`:284-299`), y un connect nuevo al mismo id reemplaza el pendiente (`:255-260`), para que `HSA.SameSensorNewAttemptNotCut` pruebe lo real (m-04).

### 6.3 Matriz AC → test (`It11Ids.cs`)

Abreviaturas: `RLP` ReconnectLoopTests, `SWD` StaleWatchdogTests, `HSS` / `HSC` / `HSR` / `HSM` / `HSA` / `HSW` / `HSX` = HeartRateService {Scan, Connect, Reconnect, Memory, Actions, Watchdog, Coexist}Tests, `HRS` HeartRateStoreTests, `HCR` HrmCardRulesTests, `HNR` HrmNoticeRulesTests, `DSL` DeviceScanLayoutTests, `WHR` WorkoutProviderHrTests, `CMP` AppComposerTests, `HCV` HrmCardRealTests (PlayMode), `HTN` HrmToastTests (PlayMode), `DBT` DeviceBleTests, `SMK` `-BleSmoke`. Marcas: d = dotnet, e = Editor/PlayMode, cap = captura, win = PC, hw = NO VERIFICABLE.

| AC | Marca | Tests |
|---|---|---|
| AC-HRM-09-01 | d | `RLP.LT139_ImmediateThenIntervalStopsOnSuccess`, `.LT140_GivesUpAfterMaxAndNotifiesOnce`, `.LT141_CancelStopsAndExceptionIsFailure`, `.LT142_StartDuringFlightDiscardsOld`, `.LT143_CancelDuringFlightSchedulesNothing`, `.LT144_StartFromOnGiveUp`, `.WaitCountsFromAttemptEnd`, `.IsRunningDuringWaits` |
| AC-HRM-05-01 | d | `SWD.LT145_FiresAt19OnceStopPrevents`, `.TouchDoesNotAllocateTimers` |
| AC-HRM-05-02 | d | `HSW.StaleEmitsZeroOnceKeepsLink`, `.InvalidDoesNotRearm`, `.ValidReadingRearmsAndClearsStale`, `.StoppedOnDisconnectDropAbortDispose` (`PendingTimers == 0`) |
| AC-HRM-05-03 | d e cap | `HCR.StaleShowsNoSignalHeartDim`, `.BeforeFirstReadingFullAndStill`; `HCV.StaleLineAppearsAndLeaves`; CAP-06 |
| AC-HRM-04-06 | d e | `HSC.EveryNotificationPublished_72_72_142_0_InvalidDropped`; `BleAllocationTests.HrmConnectedZeroBytesPerFrame` |
| AC-HRM-02-01 | d | `HSS.CancelsLoopsAndInFlightThenEmptyList`, `.TenSecondsBackToNoSensorOrNotFound`, `.ExpiryDoesNotResumeReconnect`, `.PreviousScanEndsNow` (QA-G11-02: después de la búsqueda nueva el adaptador sigue buscando, `ScanStops` no sube, `Status` sigue en `Scanning` sin pasar por `Disconnected` y vence a los 10 s de la nueva), `.StopScanEndsWaitOnce` |
| AC-HRM-02-02 | d | `HSS.K11OrderDedupOnlyHr`, `.OnlyThisScanCounts`; `HSX.RowerListNeverShowsHrm`, `.HrmListNeverShowsRower` |
| AC-HRM-02-03 | d e cap | `HSS.NativeScanErrorEndsSilently`, `.StartFailureDisconnectsAndThrows`; `HRS.ScanErrorKind`; CAP-11 |
| AC-HRM-02-04 | e cap | `DBT.LT29_RealServices`, `DBT.LT31_RealServices` (con `RowerService` + `HeartRateService` sobre K-11); CAP-16 |
| AC-HRM-02-05 | d | `HSS.LogsScanStartAndFoundMasked` |
| AC-HRM-02-06 | win | `SMK` (búsqueda de 10 s, `ajenos=0`, sin conexiones) |
| AC-HRM-03-01 | d | `HSC.SequenceOrderNoMtuFifteenSecondCap` (`Steps` + `ConnectRequests`), `.ManualConnectNoDisconnectedFlashKeepsChosenName` (QA-G11-02: buscar, tocar `Hrm` con `NeverRespond`; la secuencia de `Status` es `Scanning → Connecting`, sin `Disconnected`, y `ConnectingText` = `Conectando a Banda de prueba...`, con `HeartRateService` + `HeartRateStore` + `FakeScheduler`), `.ConnectedClearsErrorAndChosenName` (`HRS`) |
| AC-HRM-03-02 | d e cap | `HSC.NoMeasurementIsIncompatibleNotRemembered`, `.PreviousRememberedKept`; CAP-09 |
| AC-HRM-03-03 | d e cap | `HSC.TimeoutRejectDiscoverNotifyFailuresCloseLink` (4 casos), `.NoRetriesAfterManualFailure`; CAP-10 |
| AC-HRM-03-04 | d | `HSC.ConnectWhileConnectingIgnored`; `HCR.ConnectingHasNoButtons` |
| AC-HRM-06-01 | d | `HSM.SavesIdAndName`, `.AnonAfterHrmRemovesName` (Q-41), `.SameIdWithoutNameKeepsName` (DT11-05) |
| AC-HRM-06-02 | d | `HSM.LoadedOnAutoConnect`, `.OrphanNameIgnored`, `.NameFallbacks` (+ `HCR.EmptyNameIsUnknownSensor`) |
| AC-HRM-06-03 | d | `HSM.NeverRememberedIncompatibleFailedOrAborted`, `.SearchOtherDoesNotForget`; `CMP.SimulatorNeverWritesHrmKeys` |
| AC-APP-01-10 | d e | `CMP.NormalComposesHeartRateService`, `HRS.AutoConnectFailureOnlyLogged` (ya existe: se corre con el real); `HCV.RememberedReconnectsOnStart` |
| AC-HRM-07-01 | d | `HSR.PreconditionsAndImmediateDirectAttempt` (sin `ScanStarts`) |
| AC-HRM-07-02 | d cap | `HSR.SixAttemptsAt0To50ThenNone`, `.FifteenSecondAttemptsEndAt140`, `.IncompatibleCountsAsFailure`, `.FailuresOnlyLogged`; CAP-08 |
| AC-HRM-07-03 | e cap | `HCV.ReconnectingThreeButtonsConnectDisabled`; CAP-07 |
| AC-HRM-07-04 | d | `HRS.RetryRestartsSameCadence` |
| AC-HRM-07-05 | d e cap | `HRS.ConnectedNoticeOnlyForAutomatic`; `HTN.ConnectedTwoSecondsAnyTab`, `.DiscardedWhenRowerNotice`, `.ReducedMotionNoBeat`; CAP-12 |
| AC-HRM-08-01 | d | `HSR.DropZeroBeforeStatusThenEveryThreeSecondsForever` (100 fallos), `.StaysRemembered` |
| AC-HRM-08-02 | d | `HSR.DropEndsOnReconnectUserActionDispose`, `.DisconnectNeverDrops`, `.AdapterOffKeepsFailingThenReconnects`, `.NewDropRelaunches` |
| AC-HRM-08-03 | d e cap | `HNR.LostOnlyDuringWorkout`, `.AdmitYieldsOnlyToRower` (QA-G11-03: tabla de `Admit` con los 4 `ToastSlot` × fijo sí/no), `HRS.LostOncePerRun`; `HTN.LostFourSecondsOverHud`, `.ConnectedReplacesLost` (QA-G11-03: `Lost` a la vista, a 1 s `Connected` → texto `Pulsómetro conectado`, indicador `Heart`, se va a los 2 s del reemplazo y no a los 4 s del `Lost`), `.HrmDoesNotCountAsCurrentLink`, `HudBudgetTests` (59); CAP-14 |
| AC-HRM-10-01 | d e | `HSA.DisconnectForgetsCancelsPendingNoDropLoop`, `.NoReconnectAfterReopen`; `HCV.DisconnectToNoSensor` |
| AC-HRM-10-02 | d e | `HSA.ForgetDuringReconnectAbortsAndClears`, `.ErrorStaysOnFailure` (`HRS`) |
| AC-HRM-11-01 | d | `HSA.SupersededAttemptSilent` (búsqueda, otro sensor, desconectar, olvidar y `Dispose`), `.SameSensorNewAttemptNotCut` (DT11-07), `.ForgottenStaysForgotten` |
| AC-HRM-11-02 | d | `HSA.FailureWhileNewActionStartedIsSilent`, `.AutoConnectIgnoredDuringUserScan` |
| AC-HRM-11-03 | d | `HSA.DisposeReleasesEverything` (`PendingTimers == 0`, `LiveSessions == 0`) |
| AC-HRM-16-01 | d | `HSX.BothPublishKeepAliveEveryTwoSeconds`, `.HrmDropDoesNotTouchRower`, `.RowerDropWatchdogDisconnectDoNotTouchHrm` |
| AC-HRM-16-02 | d e | `HSR.AdapterOffLoopsKeepRhythm`, `HSS.SamePermissionAsRower`; `DBT.StateViewHidesHrmCard` (ya existe); [Device-Android] hw |
| AC-HRM-13-13 | d e cap | `HCR.*` (tabla de AC-HRM-13-03 con el real); `HCV.AllBodiesRealService`, `.LT28_RealService`, `.LT30_RealService` (m-08); CAP-01 a -08 |
| AC-HRM-13-14 | d e cap | `HCR.ErrorHumanAndDetailPerKind`, `.IncompatibleNoDetail`; `HCV.ErrorThreeLinesMax`; CAP-09 a -11 |
| AC-WKT-05-07 | d e cap | `WHR.SensorWinsOverFtms90`, `.PointsSave142`, `.StaleZeroFtmsDoesNotOverride` (puntos con 0), `.DropZeroThenNextFtmsRestores` (LT-36), `.DropWithoutFtmsHrStaysZero` (puntos con 0) (DT11-11); `WHR.LT35..LT38_RealService`; CAP-13 |
| AC-HRM-15-13 | d u | `CMP.SimulatorUsesSimulatedHrm`; suites de la It. 2 con `-Simulator` (AC-HRM-15-01 a -12, LT-32) |
| AC-HRM-SEC-01 | d qa | `HSC.RejectsUnknownId`, `HSS.HeadphonesNeverListedOrConnected`; revisión de QA: nada real en código, docs ni capturas |
| AC-HRM-HW-01 / -02 | hw | NO VERIFICABLE (demo §1 de la spec) |

Además se vuelven a correr, sin cambios: `L10n` (245 claves), `HudBudgetTests` (59), `WindowsDllContractTests`, `JniSignatureTests` y `NativeBleAdapterTests`.

---

## 7. Capturas (`it11`, `HrmCatalog`, adaptador falso K-11, dos corridas idénticas)

Se generan con `UiTestHost` + `HostBackends.FakeBle(K11, script)` + `FakeScheduler` + `MemoryPreferences` (sembradas por guion) + `ROWMATE_DATA_DIR` temporal + `ROWMATE_BLE=off`. Salida en `docs/migration/iterations/11-ble-pulsometro/snapshots/` (`capture-snapshots.ps1 -Set it11`: hay que sumar el caso a `Get-SetFolder` y a `ValidateSet`, y QA revisa que no escriba en la carpeta de la 10a). Nombres `CAP-NN[x]-<slug>-<tamaño>[-en].png`.

| CAP | Guion | Tamaños |
|---|---|---|
| 01 | Sin memoria; remo desconectado; vista de búsqueda en reposo | TV, W1280 |
| 02 | `hrm-search`; anuncios `Hp, Hrm, Row, Watch, HrAnon, Hrm` cada 300 ms; t = 3 s (todavía buscando) | TV, W1280 |
| 03 | Lo mismo que 02, en disposición compacta (`SingleScroll` / `TwoColumns`) | L640, TH |
| 04 | 02 + tocar `hrm-device-…:0D` con `NeverRespond`; t = 1 s | TV |
| 05 | `Respond` + `Contact142`; +240 ms + asentado | TV, W1280; TV `en` |
| 06 | 05 + 10 s sin lecturas | TV |
| 07 | Memoria `Hrm` sembrada, `Hrm` con `Fail`; al abrir: t = 5 s (arco a la mitad de la espera de 10 s) | TV; TV `en` |
| 08 | Lo mismo que 07, t = 51 s (6 intentos) | TV, TH |
| 09 | Buscar + tocar `NoHr` | TV |
| 10 | Buscar + tocar `Hrm` con `Fail(BleException("connect", "gatt error 133"))` | TV |
| 11 | `FailNextScan(BleException("scan", "error 2"))` + `hrm-search` | TV |
| 12 | Dispositivo: memoria `Hrm`, `RespondAfter(2 s)` al abrir → aviso + 300 ms (TV). HUD: entrenamiento libre con `Hrm` conectado, `DropLink` + `RespondAfter(1 s)` → aviso `Pulsómetro conectado` + 300 ms (W1280) | TV; W1280 |
| 13 | `Row` conectado con `RowerDataHr90` cada 1 s + `Hrm` con `Contact142`; entrenamiento libre a los 20 s | W1280, TV, TH |
| 14 | `Row` conectado **sin pulso FTMS** (como el AMS-670B; con `RowerDataHr90` la tarjeta BPM volvería con 90, m-01) + `Hrm` con `Contact142`; entrenamiento libre a los 20 s; `DropLink(Hrm)` con `NeverRespond`; +500 ms | W1280, TV |
| 15 | `Row` y `Hrm` conectados; vista conectada (en W1280, desplazada hasta la tarjeta, D11-07) | TV, W1280 |
| 16 | `device-search` del remo, con anuncios del remo; t = 2 s; `hrm-search` deshabilitado | TV |

AUX del arte (§9): AUX-11-01 a -15 entran solo si no suman tiempo de arnés; si no, a issues.

---

## 8. Paquetes de trabajo (archivos disjuntos)

| WP | Dueño | Qué | Hecho cuando |
|---|---|---|---|
| **WP-A** | Architect | §1.1: `ReconnectLoop`, `StaleWatchdog`, `HeartRateService`, `HrmTimings`, `HrmLink`, `IHeartRateService.Link`, store, reglas, avisos, `DeviceScanLayout`, 5 claves, K-11 y el falso, todos los tests `d` | `run-tests.ps1` en verde con los `d` de §6.3, LT-35 a -38 y LT-139 a -145 |
| **WP-B1** | Architect | §1.2 sin capturas: composición (preferencias antes del backend), `HardwareBackend`, `BleSmoke`, estructura de la tarjeta, `ScanView` compacta, `ToastLayer.ShowHrm`, `HrmNoticePresenter`, `HostBackends`, PlayMode `e`. Plugins: confirmar con `-Check` y `check.ps1 -Golden` que no cambió nada | PlayMode en verde (normal y `-Simulator`); `-BleSmoke` en verde en la PC |
| **WP-D** | Designer | §1.3: `LinkIndicator` (`Heart`, `HeartBroken`), `BleMotionSpecs` HRM, `HrmMotion` (respiración, línea sin señal, filas, check, seguir a la tarjeta), USS, reducir movimiento, `04-design-notes.md` | Vistas de WP-B1 con el arte; capturas fijas idénticas con y sin reducir movimiento; `TokensUssSyncTests` en verde |
| **WP-B2** | Architect | `HrmCatalog`, CAP-01 a -16 (dos corridas idénticas), `BleAllocationTests` (0 B por cuadro con `RowMate.Device.HrmCard` y `RowMate.Toast.LinkNotice`), `HudBudgetTests` (59), builds `normal` y `simulator` en `Builds/Windows/` | Capturas idénticas; 0 B por cuadro; builds en verde |

Contrato entre WP-B1 y WP-D: los nombres `LinkIndicator.Mode.Heart` y `.HeartBroken`, y las clases `.rm-hrm-card__identity`, `__stale`, `__error-human`, `__error-detail`, `.rm-device-scan--single-scroll` y `--two-columns`. WP-D entrega primero los dos modos. Hasta entonces, WP-B1 usa `Check` y `LinkOff`.

---

## 9. Integración del arte y la spec

### 9.1 Pedidos al Architect (00-arte §10)

| ID | Decisión |
|---|---|
| A11-01 | **Adoptado**: `HrmLink` (fase, `NextAttemptAt`/`Wait`, motivo, `IsStale`, `ConnectedBy`) y el error con tipo y detalle, con tests `d` |
| A11-02 | **Adoptado con la spec**: indicador, línea sin señal y error en dos renglones. El nombre y el sufijo van en dos etiquetas de **una** fila (el texto exacto de la spec, `{nombre} · reconectando...`), no en dos renglones |
| A11-03 | **Adoptado**: identidad con `LinkIndicator`, check de 240 ms, respiración y filas con clave estable. Lectura vencida: corazón al 40 % y `text.tertiary` sin punto (manda AC-HRM-05-03 sobre D11-04) |
| A11-04 | **Adoptado**: `Heart` y `HeartBroken` en el mismo elemento, 0 B por cuadro |
| A11-05 | **Adoptado** (DT11-14): `DeviceScanLayout` en Core, con tests en los cinco perfiles |
| A11-06 | **Rechazado para esta iteración** (a issues): la fuente del pulso con el corazón relleno o contorno y el antiparpadeo de 1 200 ms. La spec no los pide y tocan el estado del motor y la salida de la 4b. La tarjeta BPM sigue apareciendo y desapareciendo como en la 4b (Q-46) |
| A11-07 | **Adoptado con la spec**: `Connected` en cualquier pestaña (CAP-12), `Lost` solo durante el entrenamiento, 2 s y 4 s, prioridad del remo. **Rechazados**: el aviso de lectura vencida y la regla de "no repetir en 10 s" (no están en la spec; van a issues) |
| A11-08 | **Adoptado**: reducir movimiento de §7 del arte y el presupuesto de §8 |

### 9.2 Desvíos y aclaraciones

- **DT11-11**: ya no es desvío. La spec corrigió AC-WKT-05-07 (QA-G11-01): en una caída el FTMS repone su pulso (LT-36); con el watchdog queda en 0.
- **DT11-04**: un sensor sin nombre se muestra como `Sensor desconocido`, nunca con la dirección (Q-41).
- **DT11-10**: el registro enmascara la dirección del `[HRM] Encontrado`.
- Textos de §6 del arte: manda §4 de la spec. Solo entran las 5 claves nuevas.

---

## 10. Riesgos

| ID | Riesgo | Mitigación |
|---|---|---|
| R-01 | Windows no encuentra una dirección recordada sin buscar antes (`FromBluetoothAddressAsync` devuelve nulo para un equipo LE que no se vio hace poco) | Cuenta como intento fallido (DV-113). Si la demo lo muestra: issue "buscar 10 s el id recordado antes de conectar" en la DLL o en Core |
| R-02 | Un reloj con dirección privada rotativa no reconecta | Spec §10 (issue); la demo lo anota |
| R-03 | Android: dos GATT a la vez, error 133 al reconectar | Los lazos reintentan solos; `LiveSessions ≤ 1` por id (prueba de 60 s) |
| R-04 | Cancelar un intento reemplazado cierra por id el intento nuevo al mismo sensor | DT11-07 + `HSA.SameSensorNewAttemptNotCut` |
| R-05 | Un evento de escaneo atrasado (búfer nativo) entra en la búsqueda del otro servicio | Generación propia de cada servicio + filtro del remo + exclusión en la UI; `HSX.*` |
| R-06 | `ReconnectLoop` con continuaciones `async` bajo `FakeScheduler` (orden de intentos y esperas) | Mismo patrón que `RowerService` (`TestMainThreadContext`); LT-139 a -144 fijan el orden |
| R-07 | Reordenar `AppRoot` (preferencias antes del backend) cambia el arranque | Solo cambia el orden de dos constructores sin efectos cruzados; `BootstrapV3Tests` y `-BleSmoke` |
| R-08 | El HUD se acerca a su tope | Nada nuevo bajo `#immersive`; `HudBudgetTests` = 59 |
| R-09 | La radio real o equipos ajenos en la PC | Las pruebas usan `ROWMATE_BLE=off`; el humo nunca conecta y solo cuenta; nada real se versiona |

---

## 11. Para issues

- Fuente del pulso en el HUD (corazón relleno o contorno) y antiparpadeo de 1 200 ms (A11-06).
- Aviso de lectura vencida fuera de Dispositivo y regla de "no repetir en 10 s" (A11-07).
- Buscar el id recordado antes de conectar si Windows no lo encuentra (R-01).
- Textos humanos de §6 del arte para `· no encontrado` y `· reconectando...`.
- Columna lateral para la tarjeta del pulsómetro en W1280 con el remo conectado (D11-07).
- `Cancelar` durante la conexión manual (Q-45), indicador "sin contacto" (Q-46) y vibración al reconectar.

---

## 12. Cambios por gate ([03-qa-gate](03-qa-gate.md))

| Issue | Cambio | Dónde |
|---|---|---|
| QA-G11-01 (po) | La spec corrigió AC-WKT-05-07, AC-HRM-08-03 y el paso 7 de la demo. DT11-11 deja de ser desvío: caída → 0 y el FTMS repone (LT-36); watchdog → 0 sin que el FTMS lo pise. Tests `WHR` renombrados por caso | DT11-11, §6.3, §9.2; spec §3, §1 y §11 |
| QA-G11-02 | `EndScanWait()`: `_scanGen++` y `_scanDone = null` **antes** de `Cancel()`, como `RowerService`. El `finally` de la búsqueda solo limpia si sigue siendo dueña; quien la termina hace su parte. Lo usan `StartScanAsync` (antes del primer `await`), `ConnectAsync` (antes de cualquier `await`), `StopScanAsync`, `DisconnectAsync`/`ForgetAsync` (si se busca) y `Dispose`. Tests `HSC.ManualConnectNoDisconnectedFlashKeepsChosenName` y `HSS.PreviousScanEndsNow` | §2.2, §2.3, §2.4, §2.6, §6.3 |
| QA-G11-03 | `ShowHrm` cede solo ante el remo (`CurrentLink` o `StickyLink`); reemplaza un aviso del pulsómetro o uno común. El del pulsómetro va en `_hrm`/`CurrentHrm`, no en `_link`. Regla pura `HrmNoticeRules.Admit(ToastSlot, bool)`. Tests `HNR.AdmitYieldsOnlyToRower`, `HTN.ConnectedReplacesLost` y `.HrmDoesNotCountAsCurrentLink` | DT11-13, §3.1, §4.3, §6.3 |
| m-01 | CAP-14 con el remo sin pulso FTMS | §7 |
| m-02 | `ScanFailed` deja `[HRM] Error en el escaneo: …` en el registro | §2.3 |
| m-04 | `FakeBleAdapter` cancela por `reqId` | §6.2 |
| m-05 | El plazo de 15 s se dispone al conectar o fallar | §2.4 |
| m-07 | Error de `Desconectar`/`Olvidar` anotado | DT11-15 |
| m-08 | `HCV.LT28_RealService` y `.LT30_RealService` | §6.3 |
| m-10 | `_isDisconnecting = false` explícito al cerrar | §2.6 |

Decisión por defecto (una línea): `DisconnectAsync` y `ForgetAsync` que llegan durante una búsqueda la cortan (el legacy la dejaba correr hasta su plazo); la UI no las ofrece en ese estado.

Quedan para quien corresponde: m-03, m-06 y m-11 (po), m-09 (designer).
