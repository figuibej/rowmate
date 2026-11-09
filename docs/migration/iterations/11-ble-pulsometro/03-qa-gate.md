# It. 11 — Gate de QA (armonía QUÉ ↔ CÓMO)

| Campo | Valor |
|---|---|
| Autor | rowmate-qa |
| Fecha | 2026-11-08 |
| Entrada | [01-po-spec](01-po-spec.md) (41 AC), [02-tech-design](02-tech-design.md) (DT11-01 a -16), [00-arte-pulsometro](00-arte-pulsometro.md) (D11-01 a -13) |
| Legacy verificado (solo lectura) | [rowmate-legacy](https://github.com/figuibej/rowmate-legacy): `lib/core/bluetooth/heart_rate_service.dart`, `reconnect_loop.dart`, `scan_filter.dart`, `test/reconnect_loop_test.dart`, `test/workout_provider_hr_test.dart`, `lib/features/device/heart_rate_card.dart`, `heart_rate_provider.dart`, `device_screen.dart`, `workout_provider.dart:220-237`, `main.dart:68-77`, `l10n/app_es.arb`. `git status --porcelain` del legacy: vacío |
| Código verificado | `Core/Ble/{IBleAdapter,IBlePeripheral,IHeartRateService,RowerService,ScanFilter}.cs`, `Core/Ble/Native/NativeBleAdapter.cs`, `App/Device/{HeartRateStore,HrmCardRules}.cs`, `App/Composition/AppContext.cs`, `Bootstrap/{AppRoot,HardwareBackend}.cs`, `Infrastructure/Preferences/JsonFilePreferences.cs`, `Presentation/Shell/ToastLayer.cs`, `Tests/Support/FakeScheduler.cs`, plugins (`RmBle.kt`, `adapter_worker.cpp`, `device_queue.cpp`), `tools/build/build.ps1` |

## Veredicto: **APROBADO CON OBSERVACIONES**

Sin bloqueantes. Hay 3 mayores que se corrigen en los documentos antes de escribir el código de WP-A (las tres son de una o dos líneas). Los menores van a la lista de §4.

Lo que se confirmó contra el legacy:

- **Tiempos.** Búsqueda de 10 s, conexión de 15 s sin MTU, watchdog de 10 s, arranque 6 × 10 s, caída cada 3 s sin límite y espera contada desde el fin del intento (`hrs:38-45`, `:108`, `:241-246`; `rl:49-70`). Los números de AC-HRM-07-02 están bien: 0 a 50 s y 6 × 15 + 5 × 10 = 140 s.
- **Casos del legacy.** Los 7 de `rlt` están mapeados uno a uno (LT-139 a LT-145). La semántica de la espera por plazo (DT11-02) reproduce LT-145.
- **Textos.** `Conectando a {name}...`, `{nombre} · reconectando...`, `· no encontrado` y `Buscando sensores...` son iguales a `app_es.arb` y a `hc:84`, `:199-203`. Los registros `[HRM]` coinciden con `hrs:130`, `:224`, `:267`, `:276`. Hay 240 claves hoy y quedan 245.
- **Plugins sin cambios (DT11-01).** El de Android guarda varias sesiones en `ConcurrentHashMap<String, GattSession>` (`RmBle.kt:39`) y el de Windows en `unordered_map` (`device_queue.cpp:658`). Windows une los UUID anunciados por dirección (`adapter_worker.cpp:240-260`) y conecta por dirección con `FromBluetoothAddressAsync` (`device_queue.cpp:130`). Remo y pulsómetro pueden convivir.
- **Preferencias.** `JsonFilePreferences.IsLoaded` es `true` al construirse (`:72`): `AutoConnectAsync` del arranque lee la memoria a tiempo. `-BleSmoke` corre con `ROWMATE_DATA_DIR` temporal (`build.ps1:210`).
- **Reglas de Bluetooth y del repo público.** DT11-06 solo acepta ids de una búsqueda o el recordado. El humo solo cuenta, nunca conecta. Los sensores ficticios usan `AA:BB:CC:…`. En los tres documentos no hay rutas absolutas, nombres de usuario ni direcciones reales.
- **Diseño visual contra spec.** El Architect resolvió las 6 diferencias a favor de la spec (§9.1): fila única nombre · sufijo, corazón al 40 % y `text.tertiary` sin punto, aviso de conexión en cualquier pestaña, sin aviso de lectura vencida y sin regla de 10 s.

---

## 1. Issues

| ID | Sev. | Resp. | Issue | Evidencia | Corrección |
|---|---|---|---|---|---|
| QA-G11-01 | mayor | po | AC-WKT-05-07 dice "durante una caída… el pulso es 0… y esos puntos guardan 0". El legacy y LT-36, que la misma spec manda volver a correr (§5), reponen el pulso FTMS con el próximo paquete del remo. Hoy la spec se contradice: el diseño (DT11-11) eligió la paridad, y uno de los dos tests falla siempre | `workout_provider.dart:231-237` (`_onHrmStatus` → `_hrmBpm = null`; `_onData` usa `d.heartRate`); `test/workout_provider_hr_test.dart:125-141` ("el FTMS repone su valor") | Reescribir AC-WKT-05-07: "en una caída el pulso pasa a 0 y el próximo paquete FTMS repone el del monitor (LT-36); con el watchdog disparado queda en 0 y el FTMS no lo pisa". AC-HRM-08-03 y el paso 7 de la demo: "la tarjeta BPM desaparece **si el monitor no manda pulso** (AMS-670B)" |
| QA-G11-02 | mayor | architect | Fin de la búsqueda en línea. `FakeScheduler.Delay` (y su cancelación) corre las continuaciones en línea. En §2.4, `ConnectAsync` "termina la espera de la búsqueda" **sin cambiar `_scanGen`**. El `finally` de la búsqueda corre dentro de `_scanDone.Cancel()`, se cree dueño y pone `Status = Disconnected` antes de `ConnectToAsync`. `HeartRateStore.OnStatus` borra `_chosenName` con cualquier estado que no sea `Connecting`. Resultado: la tarjeta muestra `Conectando a Sensor desconocido...` en vez de `Conectando a Banda de prueba...` (AC-HRM-03-01, CAP-04), y el estado parpadea `Scanning → Disconnected → Connecting`. Pasa lo mismo en §2.3 ("termina la espera de una búsqueda anterior" **antes** de "nueva `_scanGen`"): el `finally` viejo puede llamar `StopScanAsync` del adaptador después del `StartScanAsync` nuevo y cortar la búsqueda nueva. En el legacy no pasa, porque `done.complete()` reanuda en una microtarea, después de que `_connectTo` ya puso `connecting` | `FakeScheduler.cs:114` ("Sin RunContinuationsAsynchronously: la continuación corre en línea"); `HeartRateStore.cs:140-144`; `hrs:162-166`, `:195-198`; `RowerService.cs:126` (el remo hace `++_scanGeneration` primero) | En `StartScanAsync`, `ConnectAsync`, `StopScanAsync`, `ForgetAsync`/`DisconnectAsync` (si cortan una búsqueda) y `Dispose`: **subir `_scanGen` (y reemplazar o anular `_scanDone`) antes** de cancelar la espera vieja, como `RowerService`. Tests: `HSC.ManualConnectNoDisconnectedFlashKeepsChosenName`, que registra la secuencia de `Status` (sin `Disconnected` entre `Scanning` y `Connecting`) y comprueba `ConnectingText` = `Conectando a Banda de prueba...`; `HSS.PreviousScanEndsNow`, que comprueba que el adaptador sigue buscando y que el estado sigue en `Scanning` |
| QA-G11-03 | mayor | architect | La segunda mitad de AC-HRM-08-03 ("cuando vuelve, `Pulsómetro conectado` 2 s") depende de que `ShowHrm` **reemplace su propio aviso** `Lost`, que dura 4 s. La regla de §4.3 ("con `CurrentLink != null` o `StickyLink != null` vuelve `false`") no aclara si el aviso del pulsómetro cuenta como `CurrentLink`. Si cuenta, la reconexión rápida descarta `Connected`. Es justo el guion HUD de CAP-12 (`DropLink` + `RespondAfter(1 s)`). No hay test para esto | `ToastLayer.cs:90-93`; spec AC-HRM-08-03; 02-tech §4.3 y §7 CAP-12 | Escribir en §4.3 que `ShowHrm` solo cede ante un aviso **del remo** (fijo o a la vista) y que reemplaza un aviso anterior del pulsómetro. Test `HTN.ConnectedReplacesLost` y uno `d` en `HNR` |

## 2. Cobertura AC → componente → test

Los 41 AC tienen componente y test (o marca de NO VERIFICABLE) en 02-tech §6.3. Quedan observaciones en:

| AC | Estado |
|---|---|
| AC-WKT-05-07, AC-HRM-08-03 | Test planificado, pero contra un texto que contradice LT-36 → QA-G11-01 |
| AC-HRM-03-01 | Test planificado, pero sin chequeo de la secuencia de estados → QA-G11-02 |
| AC-HRM-08-03 (vuelta) | Sin test del reemplazo `Lost` → `Connected` → QA-G11-03 |
| AC-HRM-13-13 | LT-28 y LT-30 con el servicio real no figuran por nombre (la spec §5 pide LT-28 a LT-31) → menor m-08 |
| AC-HRM-HW-01/-02, AC-HRM-16-02 (Android) | NO VERIFICABLE en el workflow (sin sensor, sin teléfono, sin Android Build Support) |

## 3. Riesgos aceptados

- **R-01.** Windows puede no resolver una dirección recordada sin buscar antes. Cuenta como intento fallido (DV-113); se ve en la demo.
- **Android en segundo plano.** El ciclo de Unity se pausa: las lecturas se acumulan en el anillo nativo y se drenan antes del `Tick` al volver (DV-114).
- **Cadencia de caída.** Al no tener límite, deja una línea `[HRM] Reintento fallido` cada 3 s en el `Player.log` (paridad).

## 4. Menores (para issues o para corregir al pasar)

- m-01 (architect): CAP-14 usa `RowerDataHr90` cada 1 s. Por DT11-11, la tarjeta BPM vuelve con 90 en el próximo paquete y la captura no muestra lo que pide AC-HRM-08-03. Usar el remo sin pulso, como el AMS-670B.
- m-02 (architect): `ScanFailed` termina "en silencio" en la UI, pero el legacy registra `[HRM] Error en el escaneo: …` (`hrs:149`). Mantener la línea en el registro.
- m-03 (po): AC-HRM-02-05 muestra `<id>` completo; DT11-10 lo enmascara con `BleIds.Mask`. Alinear el texto de la spec.
- m-04 (architect): la premisa de DT11-07 no se da con el adaptador real. `NativeBleAdapter.CancelConnect` solo actúa si `session.ConnectReqId == reqId` (`:284-299`), y un connect nuevo al mismo id ya cancela el pendiente (`:255-260`). `FakeBleAdapter` tiene que cancelar por `reqId`, no por id, para que `SameSensorNewAttemptNotCut` pruebe lo real.
- m-05 (architect): disponer el plazo de 15 s al conectar o al fallar, no solo en `Dispose`. En `HSC.SequenceOrder…`, después de conectar solo queda pendiente el temporizador del watchdog.
- m-06 (po): el paso 9 de la demo dice "unos 50 s". Con intentos reales de hasta 15 s pueden ser hasta 140 s (AC-HRM-07-02).
- m-07 (po/architect): un `Desconectar` u `Olvidar` que falla muestra `No se pudo conectar con {name}`. Es aceptable (el legacy muestra "Error al conectar"), pero conviene anotarlo en DT11-15.
- m-08 (architect): nombrar en §6.3 LT-28 y LT-30 con el servicio real (AC-HRM-13-13).
- m-09 (designer): 04-design-notes tiene que dejar asentado lo que ganó la spec sobre 00-arte (D11-02, D11-04, D11-11, el aviso de lectura vencida, la regla de 10 s y los textos de §6), para que WP-D no implemente el documento de arte.
- m-10 (architect): `DisconnectAsync` no vuelve `_isDisconnecting` a `false` después de cerrar (`hrs:346`). Hoy lo resetea `ConnectToAsync`; escribirlo explícito.
- m-11 (po): AC-HRM-05-03 (corazón al 40 % con la lectura vencida) es una excepción a AC-HRM-13-02 (corazón pleno conectado aun con `--`). Decirlo en la spec para que no choquen los tests de `HCR`.
