# It. 11 — Verificación de QA (Bluetooth real: pulsómetro)

| Campo | Valor |
|---|---|
| Autor | rowmate-qa |
| Fecha | 2026-11-09 |
| Entrada | [01-po-spec](01-po-spec.md) (41 AC, LT-139 a LT-145, CAP-01 a CAP-16), [02-tech-design](02-tech-design.md), [03-qa-gate](03-qa-gate.md), [04-design-notes](04-design-notes.md), [capturas](snapshots/INDEX.md) |
| Legacy | [rowmate-legacy](https://github.com/figuibej/rowmate-legacy) (solo lectura): `heart_rate_service.dart`, `reconnect_loop.dart`, `device_screen.dart`, `heart_rate_card.dart`, `app_es.arb`. `git status --porcelain` vacío antes y después |
| Hardware | Adaptador Bluetooth LE de la PC, sin pulsómetro ni remo. Sin teléfono ni "Android Build Support" |

## Veredicto: **APROBADO CON OBSERVACIONES**

No hay bloqueantes ni mayores. Todo pasa: dotnet, Unity con y sin simulador, el humo de Bluetooth en el build y la sesión con pulso simulado en el build del simulador. Quedan 3 menores (§4): dos detalles de capturas o del desplazamiento en L640 y un test planificado que falta. Ninguno rompe un AC, la conexión, el entrenamiento ni la paridad.

---

## 1. Corridas (salida real)

| Suite | Resultado |
|---|---|
| `run-tests.ps1 -Matrix` (dotnet 8) | Build: 0 advertencias, 0 errores. Herramientas: **194 de 194**. Core: **3 776 tests: 3 737 pasan, 39 omitidos (equipo o plataforma), 0 fallan** |
| Matriz con los XML de Unity (`-UnityResults`) | **PASA 1 003, FALLA 0, IGNORADO 4, PENDIENTE UNITY 0, SIN TEST 13**. Los 13 sin test son de equipo: los de la It. 11 son AC-HRM-02-06 (lo cubre el humo, §2), AC-HRM-HW-01 y AC-HRM-HW-02. Los otros 10 son de la 10a, la 8 y la 4b y ya estaban así |
| LT-139 a LT-145 (`reconnect_loop_test`) | **7 de 7 PASAN**: `ReconnectLoopTests` (LT-139 a LT-144) y `StaleWatchdogTests` (LT-145), con los mismos casos y números de `rlt:9-157` |
| Unity EditMode | **3 880: 3 832 pasan, 48 ignorados, 0 fallan** |
| Unity PlayMode | **638 de 638**: `HrmCardRealTests` 8/8, `HrmToastTests` 5/5, `DeviceBleTests` 19/19 (con `LT29_RealServices` y `LT31_RealServices`), `BleAllocationTests` 4/4 (con `HrmConnectedZeroBytesPerFrame`), `HudBudgetTests` 11/11 y `HudLinkNoticeTests` 8/8 |
| Unity EditMode `-Simulator` | **4 074: 4 026 pasan, 48 ignorados, 0 fallan** |
| Unity PlayMode `-Simulator` | **713 de 713** |
| Guardia de `ProjectSettings` | "sin cambios" en las 4 corridas y en los 2 builds. No cambió `HorizonFar.mat` ni `HorizonNear.mat` |
| Plugins nativos | **Sin cambios** (`git status` vacío en `native/` y `Assets/Plugins/`, DT11-01). La DLL y el Kotlin son los de la 10a. No hizo falta recompilar ni repetir sus revisiones |

## 2. Builds de Windows (carpetas de datos temporales)

1. **`build.ps1 -Target Windows -BleSmoke`** (`Builds/Windows/dev-smoke`, `ROWMATE_DATA_DIR` temporal, registro en `<scratch>`) sale con **0**:
   - `[BLE] humo: estado=On busquedas=3 ftms=0 fallas=0 colgado=no soloFtms=si`
   - `[HRM] Escaneando sensores 0x180D durante 10s`
   - `[HRM] humo: busquedas=1 sensores=0 ajenos=0 fallas=0`

   No hubo líneas `Encontrado`, porque no había un pulsómetro cerca. No hubo conexiones ni excepciones. La radio y los equipos emparejados no se tocaron. El registro arranca con `Remo y pulsómetro reales (plugin nativo, adaptador On)`.
2. **`build.ps1 -Target Windows -Simulator`** (`Builds/Windows/simulator`) sale con **0**. Lo manejé a mano en W1280, con `ROWMATE_DATA_DIR` temporal, y seguí el recorrido completo:
   - Dispositivo → `Search` → `Pulsómetro simulado` → la tarjeta muestra **143 bpm**.
   - Workout → `Free workout`: el HUD muestra **BPM 143**.
   - A los 37 s: `Finish` y luego confirmar.

   La base quedó con **1 sesión libre de 10 puntos, los 10 con pulso: promedio 143, máximo 143**. Lo leí con una herramienta propia en `<scratch>` (`SqliteSessionRepository` sobre una copia de la base). El simulador **no escribió `prefs.json`**: no hay claves `hrm.*` (AC-HRM-15-13). El registro no tiene excepciones.
3. Corrí además `-rowmate-perf-entries 1` en el simulador. Guarda una sesión libre sin pulso, porque el pulsómetro simulado arranca en `Sin sensor` (AC-APP-01-04, paridad).

## 3. Matriz AC → test → resultado

**39 PASS, 0 FAIL, 2 NO VERIFICABLE.** En la tabla, d = dotnet, u = Unity (Edit o PlayMode) y cap = captura revisada.

| AC | Resultado | Evidencia |
|---|---|---|
| AC-HRM-09-01 | PASS | d: `ReconnectLoopTests` LT-139 a LT-144, `WaitCountsFromAttemptEnd`, `IsRunningDuringWaits` |
| AC-HRM-05-01 | PASS | d: `StaleWatchdogTests` LT-145, `TouchDoesNotAllocateTimers` |
| AC-HRM-05-02 | PASS | d: `HeartRateServiceWatchdogTests` (8 casos) |
| AC-HRM-05-03 | PASS | d+u: `HrmCardRulesRealTests`, `HrmCardRealTests.StaleLineAppearsAndLeaves`. En CAP-06 se ven `--`, `Sin señal del sensor` y el corazón atenuado |
| AC-HRM-04-06 | PASS | d: `EveryNotificationPublished_72_72_142_0_Invalid…`. u: `BleAllocationTests.HrmConnectedZeroBytesPerFrame` |
| AC-HRM-02-01 | PASS | d: `HeartRateServiceScanTests` (5 casos, con `PreviousScanEndsNow` de QA-G11-02) |
| AC-HRM-02-02 | PASS | d: `K11OrderDedupOnlyHr`, `OnlyThisScanCounts`, `HrmListNeverShowsRower`, `RowerListNeverShowsHrm`. En CAP-02 y CAP-03 hay 3 filas en orden, sin `Auriculares` ni `AMS-670B` |
| AC-HRM-02-03 | PASS | d: `NativeScanErrorEndsSilently`, `StartFailureDisconnectsAndThrows`. En CAP-11 se ve `No se pudo buscar sensores` / `scan: error 2` |
| AC-HRM-02-04 | PASS | u: `DeviceBleTests.LT29_RealServices`, `LT31_RealServices`. En CAP-02, `Buscar dispositivos` está deshabilitado; en CAP-16, `Buscar` del pulsómetro. Paridad con `ds:347` y `hc:22-24` |
| AC-HRM-02-05 | PASS | d: `LogsScanStartAndFoundMasked` (dirección enmascarada, DT11-10). En el build: `[HRM] Escaneando sensores 0x180D durante 10s` |
| AC-HRM-02-06 | PASS | Build `-BleSmoke` (§2.1): 10 s de búsqueda real, `ajenos=0`, `fallas=0`, sin conexiones |
| AC-HRM-03-01 | PASS | d: `SequenceOrderNoMtuFifteenSecondCap`, `ManualConnectNoDisconnectedFlashKeepsChosenName`. En CAP-04 se ve `Conectando a Banda de prueba...`, sin botones |
| AC-HRM-03-02 | PASS | d: `NoMeasurementIsIncompatibleNotRemembered`, `PreviousRememberedKept`. En CAP-09, el texto es igual al de `app_es.arb:41` |
| AC-HRM-03-03 | PASS | d: `TimeoutRejectDiscoverNotifyFailuresCloseLink` (4 casos), `NoRetriesAfterManualFailure`. CAP-10 |
| AC-HRM-03-04 | PASS | d: `ConnectWhileConnectingIgnored`, `HrmCardRulesRealTests.ConnectingHasNoButtons` |
| AC-HRM-06-01 | PASS | d: `SavesIdAndName`, `AnonAfterHrmRemovesName` (Q-41), `SameIdWithoutNameKeepsName` |
| AC-HRM-06-02 | PASS | d: `LoadedOnAutoConnect`, `OrphanNameIgnored`, `NameFallbacks` |
| AC-HRM-06-03 | PASS | d: `NeverRememberedIncompatibleFailedOrAborted`, `SearchOtherDoesNotForget`, `SimulatorNeverWritesHrmKeys`. En el build del simulador no hay `prefs.json` |
| AC-APP-01-10 | PASS | d: `NormalComposesHeartRateService`, `AutoConnectFailureOnlyLogged`. u: `HrmCardRealTests.RememberedReconnectsOnStart` |
| AC-HRM-07-01 | PASS | d: `PreconditionsAndImmediateDirectAttempt` |
| AC-HRM-07-02 | PASS | d: `SixAttemptsAt0To50ThenNone`, `FifteenSecondAttemptsEndAt140`, `IncompatibleCountsAsFailure`, `FailuresOnlyLogged`. En CAP-08 se ve `· no encontrado`, con `link_off` y `Conectar` habilitado |
| AC-HRM-07-03 | PASS | u: `ReconnectingThreeButtonsConnectDisabled`. En CAP-07a/b se ve el arco a la mitad, con `Conectar` deshabilitado y `Buscar otro` y `Olvidar` habilitados |
| AC-HRM-07-04 | PASS | d: `HeartRateStoreRealTests.RetryRestartsSameCadence` |
| AC-HRM-07-05 | PASS | d: `ConnectedNoticeOnlyForAutomatic`, `ToastHeartBeatsOnce`. u: `HrmToastTests.ConnectedTwoSecondsAnyTab`, `DiscardedWhenRowerNotice`. CAP-12. Ver m-01 y m-03 |
| AC-HRM-08-01 | PASS | d: `DropZeroBeforeStatusThenEveryThreeSecondsForever` (100 fallos), `StaysRemembered` |
| AC-HRM-08-02 | PASS | d: `HeartRateServiceReconnectTests` (8 casos, con `AdapterOffKeepsFailingThenReconnects`) |
| AC-HRM-08-03 | PASS | d: `HrmNoticeRulesTests` (tabla de `Admit`), `LostOncePerRun`. u: `LostFourSecondsOverHud`, `ConnectedReplacesLost`, `HrmDoesNotCountAsCurrentLink`, `HudBudgetTests` (59). En CAP-14 la tarjeta BPM no está y la sesión sigue |
| AC-HRM-10-01 | PASS | d: `DisconnectForgetsCancelsPendingNoDropLoop`, `NoReconnectAfterReopen`. u: `DisconnectToNoSensor` |
| AC-HRM-10-02 | PASS | d: `ForgetDuringReconnectAbortsAndClears`, `ErrorStaysOnFailure` |
| AC-HRM-11-01 | PASS | d: `SupersededAttemptSilent` (5 acciones), `SameSensorNewAttemptNotCut`, `ForgottenStaysForgotten` |
| AC-HRM-11-02 | PASS | d: `FailureWhileNewActionStartedIsSilent`, `AutoConnectIgnoredDuringUserScan` |
| AC-HRM-11-03 | PASS | d: `DisposeReleasesEverything` (4 estados, `PendingTimers == 0`) |
| AC-HRM-16-01 | PASS | d: `HeartRateServiceCoexistTests` (5 casos). CAP-15 |
| AC-HRM-16-02 | PASS | d: `AdapterOffLoopsKeepRhythm`, `SamePermissionAsRower`. u: `OffShowsTheStateViewInsteadOfScanAndHrm`. La parte de Android queda NO VERIFICABLE (sin teléfono) |
| AC-HRM-13-13 | PASS | d+u: 32 resultados (`CardBodiesWithRealService`, `DeviceScanLayoutTests`, `AllBodiesRealService`, `LT28_RealService`, `LT30_RealService`). CAP-01 a CAP-08 |
| AC-HRM-13-14 | PASS | d: `ErrorHumanAndDetailPerKind`, `IncompatibleNoDetail`. u: `ErrorThreeLinesMax`. CAP-09 a CAP-11 |
| AC-WKT-05-07 | PASS | d: `WorkoutProviderHrRealTests` (7 casos: el sensor gana sobre 90 FTMS, puntos con 142, el watchdog en 0, la caída repone el FTMS (LT-36), la caída sin FTMS da 0). En CAP-13 el HUD muestra 142 y no 90 |
| AC-HRM-15-13 | PASS | d: `SimulatorUsesSimulatedHrm`, `SimulatorNeverWritesHrmKeys`. Las suites del simulador están en verde. En el build del simulador: 143 bpm guardados y sin `hrm.*` (§2.2) |
| AC-HRM-SEC-01 | PASS | d: `RejectsUnknownId` (DT11-06), `HeadphonesNeverListedOrConnected`. Revisión de QA (§5): nada real en lo versionado |
| AC-HRM-HW-01 | NO VERIFICABLE | [BLE-HW]: hace falta una banda o un reloj real y el AMS-670B. Queda para la demo del usuario (spec §1) |
| AC-HRM-HW-02 | NO VERIFICABLE | [Device-Android] y [BLE-HW]: falta el teléfono y "Android Build Support" |

## 4. Issues

| ID | Sev. | Resp. | Issue | Evidencia | Corrección |
|---|---|---|---|---|---|
| m-01 | menor | architect | **CAP-12a muestra la tarjeta del pulsómetro vacía.** Se ve solo el encabezado y el aviso `Pulsómetro conectado`. El guion la toma a t = 2,65 s absolutos (`AtSecond(s, 2.65)`), justo cuando arranca el cambio de cuerpo, con el contenido todavía en opacidad 0. Es la toma la que queda mal, no la app: las pruebas del cuerpo conectado pasan | `HrmCatalog.cs:157-158`. En `manifest.json` de CAP-12a, `hrm-connected-name` y `hrm-bpm` existen pero no se ven, y `hrm-body` mide 77,33 cuando el final es 73,14 (sigue en `HeightTween`). `HeightTween.cs:47` | Tomarla después de la conexión + 240 ms de retención + 360 ms del cuerpo, con margen, o asentar las animaciones. Sumar un chequeo en la captura de que el nombre se ve |
| m-02 | menor | architect/designer | **En L640 (`SingleScroll`), el desplazamiento automático pasa de largo el borde superior de la tarjeta.** Con la lista de 3 sensores se esconden el título `PULSÓMETRO`, `Buscando sensores...` y casi toda la primera fila. Pasa cuando la tarjeta (258 dp) es más alta que el área visible (148 dp); la nota de arte dice "sin esconder su borde superior" | CAP-03a. En `manifest.json`, `hrm-card` y = 30 y `hrm-card-slot-scan` y = 132. `HrmMotion.cs:372-381` (`Follow`) y `ScanView.cs:112-129` | Si la tarjeta no entra entera, seguir hasta su borde superior y no hasta el inferior. Revisar la suma de `top` en el contenido del `ScrollView`. Sumar un chequeo en CAP-03a: `hrm-title` visible |
| m-03 | menor | architect | **Falta el test planificado `HrmToastTests.ReducedMotionNoBeat`** (AC-HRM-07-05: con reducir movimiento, sin animación). El código existe, pero ningún test de la 11 lo ejercita | `02-tech-design.md:333`. `HrmToastTests` tiene 5 tests y este no está. `LinkIndicator.cs:231`, `ToastLayer.cs:183` | Agregar el test en PlayMode |

### Para issues

- m-01, m-02 y m-03 de esta tabla.
- Los 7 puntos de la spec §10: indicador "sin contacto", búsqueda con radar, guía para smartwatch, direcciones rotativas, nombre elegido sin nombre del sistema, `Cancelar` en la conexión manual y sonido al conectar.
- Riesgo R-01 del gate: Windows podría no reconectar a una dirección recordada sin buscar antes. Se confirma en la demo con un sensor real.

## 5. Capturas, repo público y codificación

- **Capturas** (28 archivos, CAP-01 a CAP-16): las miré todas y coinciden con la spec §7 y con el legacy, salvo m-01 y m-02.
  - Textos: `Buscando sensores...`, `Conectando a {name}...`, `· reconectando...`, `· no encontrado` y `hrmIncompatible` son iguales a `app_es.arb`. Las 5 claves nuevas coinciden con la spec §4 (con `…` U+2026).
  - Arte: el corazón está atenuado sin señal y respira mientras reconecta. Hay arco de espera, spinner y `link_off`. El error va en dos renglones, sin caja.
  - Avisos: `Pulsómetro conectado` y `Se perdió el pulsómetro, reconectando…` salen en la cápsula, sobre el HUD.
  - Disposición: en TH hay dos columnas. CAP-15 lleva la tarjeta bajo la grilla.
  - Solo hay equipos ficticios (`AA:BB:CC:…`).

  El arnés escribe en la carpeta de la 11 (`capture-snapshots.ps1:171`). La carpeta de la 10a no cambió.
- **Repo público: limpio.** Corrí `git grep --untracked` sobre lo versionado y lo nuevo:
  - **Rutas:** no hay rutas de la PC, nombre de usuario, carpeta del scratchpad ni nombre del equipo. Solo aparecen las rutas genéricas de instalación de `dotnet` y Unity, ya aceptadas por `RepoHygieneTests`.
  - **Direcciones MAC:** no hay ninguna fuera de `AA:BB:CC:…`.
  - **Dispositivos:** no hay nombres de equipos reales. `Polar H10 7E1A2B3C` es un dato ficticio de la It. 2 que ya estaba versionado.
  - **Binarios:** no hay binarios nuevos y los nativos no cambiaron.

  Los registros de los players, con el nombre del equipo en la línea de PlayerConnection de Unity, quedaron solo en `<scratch>`.
- **UTF-8:** los 90 archivos de texto nuevos o cambiados son UTF-8 válido, sin BOM y sin doble codificación.
- **Legacy intacto:** `git -C ../rowmate-legacy status --porcelain` vacío. No se leyó `strava_secrets.dart`.
- **Procesos:** no queda ningún Unity ni RowMate.exe de esta verificación abierto. El único `unity.exe` es el servicio del Hub, que no lancé yo.
