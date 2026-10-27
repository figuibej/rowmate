# Inventario de producto: Pulsómetro BLE (0x180D), auto-reconexión, watchdog y tarjeta de pulsómetro

| Campo | Valor |
|---|---|
| Área | **HRM** (IDs `F-HRM-NN`; cada comportamiento: `HRM-NN.k`) |
| Autor | rowmate-po |
| Fecha | 2026-10-27 |
| Fuente de verdad | `https://github.com/figuibej/rowmate-legacy` (solo lectura). **El código manda sobre los docs**: las diferencias están en la sección 5. |
| Rutas | Todas las rutas de archivos son relativas a `rowmate-legacy/`. |

Este documento es el **contrato de paridad** del subsistema. Lo que no figura acá se pierde en la migración. Describe el QUÉ, sin decisiones técnicas.

**Fuentes leídas completas:** `lib/core/bluetooth/heart_rate_service.dart`, `heart_rate_parser.dart`, `reconnect_loop.dart`, `simulated_heart_rate_service.dart`, `scan_filter.dart`; `lib/features/device/heart_rate_card.dart`, `heart_rate_provider.dart`; `docs/superpowers/specs/2026-10-02-heart-rate-monitor-design.md`; `test/heart_rate_card_test.dart`, `heart_rate_parser_test.dart`, `heart_rate_provider_test.dart`, `reconnect_loop_test.dart`, `simulated_heart_rate_service_test.dart`, `workout_provider_hr_test.dart`, `scan_filter_test.dart`.
**Leídas en parte (contexto):** `lib/main.dart`, `lib/features/workout/workout_provider.dart`, `lib/features/device/device_screen.dart`, `lib/core/bluetooth/ble_service.dart`, `simulated_ble_service.dart`, `lib/core/dev/rowing_simulator.dart`, `simulator_overlay.dart`, `lib/shared/theme.dart`, `lib/l10n/app_es.arb`, `app_en.arb`, `test/simulator_overlay_test.dart`, `test/session_stats_test.dart`, `CLAUDE.md`, `README.md`, `docs/image-01.png` (captura de la tarjeta conectada con 143 bpm).

**Vocabulario:** *sensor* o *pulsómetro* = periférico con el servicio Heart Rate (0x180D). *Remo* = monitor FTMS (área BLE). *Recordado* = último sensor conectado con éxito. *Cadencia* = serie de reintentos automáticos. *Acción del usuario* = Buscar, Buscar otro, elegir un sensor de la lista, Conectar, Desconectar u Olvidar.

---

## 0. Vista general

El teléfono (o la PC) mantiene **dos conexiones Bluetooth a la vez**: el remo y el pulsómetro. El pulsómetro es independiente del remo: se busca, se conecta, se recuerda y se reconecta solo. Su pulso se ve en vivo en la tarjeta "Pulsómetro" de la pestaña Dispositivo y **reemplaza** al pulso que pudiera mandar el remo en todo lo que viene después (pantallas de entrenamiento, puntos guardados, estadísticas, Strava).

Flujo de datos de producto:

```
sensor 0x2A37 ─► lectura (F-HRM-01) ─► pulso en vivo (F-HRM-04, watchdog F-HRM-05) ─┬─► tarjeta (F-HRM-12/13)
                                                                                     └─► fusión con el remo (F-HRM-14) ─► HUD, data_points, stats, TCX
```

### 0.1 Constantes de producto

| Constante | Valor exacto | Fuente |
|---|---|---|
| Servicio Heart Rate | `0000180d-0000-1000-8000-00805f9b34fb` (0x180D) | `lib/core/bluetooth/heart_rate_parser.dart:21` |
| Característica de medición | `00002a37-0000-1000-8000-00805f9b34fb` (0x2A37, por notificación) | `heart_rate_parser.dart:22` |
| Duración de una búsqueda | 10 s (valor por defecto; la UI siempre usa el defecto) | `heart_rate_service.dart:108` |
| Tiempo máximo de un intento de conexión | 15 s | `heart_rate_service.dart:245` |
| Tamaño de paquete (MTU) | no se negocia (0x2A37 mide 2–20 bytes; algunos relojes rechazan el pedido) | `heart_rate_service.dart:244` |
| Watchdog de lectura vencida | 10 s sin lectura válida → pulso 0 | `heart_rate_service.dart:38` |
| Reconexión al arrancar | 6 intentos **en total**, 10 s de espera entre el fin de un intento fallido y el siguiente | `heart_rate_service.dart:41-42` |
| Reconexión tras caída | cada 3 s, sin límite | `heart_rate_service.dart:45` |
| Peor duración de la reconexión al arrancar | 6 × 15 s + 5 × 10 s = **140 s** (los docs dicen "~2 minutos") | derivado |
| Preferencia: id del sensor | `hrm.deviceId` | `heart_rate_service.dart:34` |
| Preferencia: nombre del sensor | `hrm.deviceName` | `heart_rate_service.dart:35` |
| Simulador: nombre del sensor | `Pulsómetro simulado` (siempre en español) | `simulated_heart_rate_service.dart:13` |
| Simulador: id del sensor | `SIM:HRM` | `simulated_heart_rate_service.dart:14` |
| Simulador: intensidad de señal | −50 dBm | `simulated_heart_rate_service.dart:61` |
| Simulador: frecuencia de emisión | 1 lectura por segundo | `simulated_heart_rate_service.dart:126` |
| Simulador: reconexión tras caída simulada | 3 s | `simulated_heart_rate_service.dart:108` |
| Color identitario del pulso | `#EF476F` (rosa/rojo) | `lib/shared/theme.dart:37` |
| Fondo de la tarjeta | `#1A2E45` | `heart_rate_card.dart:30` |

### 0.2 Estados del sensor y banderas derivadas

| Estado | Significado |
|---|---|
| `scanning` | Buscando sensores. |
| `connecting` | Intentando conectar (manual o automático). |
| `connected` | Conectado y recibiendo notificaciones. |
| `disconnected` | Sin conexión (estado inicial). |

| Bandera | Verdadera cuando… | Fuente |
|---|---|---|
| *Reintentando* | corre la cadencia de arranque o la de caída (incluye las esperas entre intentos) | `heart_rate_service.dart:100` |
| *Tiene recordado* | existe un nombre recordado para mostrar (= hay id recordado) | `heart_rate_provider.dart:50`, `heart_rate_service.dart:97-98` |

### 0.3 Transiciones del sensor real

| Desde | Evento | Hacia | Efectos observables |
|---|---|---|---|
| cualquiera | Buscar / Buscar otro (con BLE soportado) | `scanning` | cancela cadencias e intento en vuelo; lista de resultados vacía |
| `scanning` | pasan 10 s, error nativo de escaneo o detener búsqueda | `disconnected` | la lista desaparece de la tarjeta |
| `scanning` / `disconnected` | el usuario elige un sensor | `connecting` | se detiene la búsqueda; "Conectando a {nombre}..." |
| `connecting` | conexión + servicio 0x180D/0x2A37 + notificaciones OK | `connected` | se recuerda el sensor; watchdog armado; vigilancia de caída armada |
| `connecting` | falla, timeout de 15 s o sensor incompatible | `disconnected` | manual: error visible. Automático: cuenta como intento fallido, sin error visible |
| `connecting` | otra acción del usuario | `disconnected` (y luego lo que pida la acción) | el intento se aborta en silencio, sin error |
| `connected` | caída no pedida | `disconnected` → `connecting` (inmediato) | pulso 0 **antes** del cambio de estado; arranca la cadencia de caída |
| `connected` | Desconectar | `disconnected` | pulso 0; se olvida el sensor |
| `disconnected` con recordado | arranque de la app o botón Conectar | `connecting` | arranca la cadencia de arranque |
| `connecting` reintentando | Olvidar | `disconnected` | se aborta el intento; se olvida el sensor |
| cualquiera | cierre de la app | — | se detiene todo (búsqueda, intento, cadencias, watchdog) |

### 0.4 Textos visibles (`lib/l10n/app_es.arb:23-48`, `app_en.arb:23-48`)

| Clave | es | en | Uso |
|---|---|---|---|
| `hrmTitle` | Pulsómetro | Heart rate sensor | Título de la tarjeta, **se muestra en mayúsculas** ("PULSÓMETRO" / "HEART RATE SENSOR") |
| `hrmNoSensor` | Sin sensor | No sensor | Estado sin sensor |
| `hrmWatchHint` | Un smartwatch (Wear OS, Apple Watch) necesita una app que transmita el pulso por Bluetooth, p. ej. Heart for Bluetooth en Wear OS. | A smartwatch (Wear OS, Apple Watch) needs an app that broadcasts heart rate over Bluetooth, e.g. Heart for Bluetooth on Wear OS. | Pista bajo "Sin sensor" |
| `hrmSearch` | Buscar | Search | Botón del estado sin sensor |
| `hrmSearchOther` | Buscar otro | Search another | Botón del estado recordado |
| `hrmSearching` | Buscando sensores... | Searching for sensors... | Estado buscando |
| `hrmConnectingTo` | Conectando a {name}... | Connecting to {name}... | Estado conectando (manual) |
| `hrmConnect` | Conectar | Connect | Botón del estado recordado (reintentar) |
| `hrmDisconnect` | Desconectar | Disconnect | Botón del estado conectado |
| `hrmForget` | Olvidar | Forget | Botón del estado recordado |
| `hrmReconnecting` | reconectando... | reconnecting... | Sufijo "{nombre} · reconectando..." |
| `hrmNotFound` | no encontrado | not found | Sufijo "{nombre} · no encontrado" |
| `hrmUnknownSensor` | Sensor desconocido | Unknown sensor | Sensor sin nombre en la lista |
| `hrmIncompatible` | No es un pulsómetro compatible (sin servicio Heart Rate) | Not a compatible heart rate sensor (no Heart Rate service) | Error de sensor incompatible |
| `hrmConnectError` | Error al conectar: {error} | Connection error: {error} | Cualquier otro error (también los de búsqueda); `{error}` = texto técnico de la excepción |
| `hrmBpm` | bpm | bpm | Unidad junto al número |

Textos relacionados de otras áreas: `deviceSearch` "Buscar dispositivos"/"Search devices" (botón del remo que se deshabilita durante la búsqueda del pulsómetro), `metricHeartRate` "PULSO"/"HR", `historyStatHeartRate` "Pulso"/"HR". Textos **no localizados** del simulador: "Pulsómetro simulado", "Simular caída del pulsómetro".

---

## 1. Funcionalidades

### F-HRM-01 — Lectura del paquete de pulso (Heart Rate Measurement 0x2A37)

**Valor para el usuario:** cualquier pulsómetro estándar (bandas Polar, Garmin, Wahoo, Coospo; brazaletes; relojes con app emisora) funciona sin configuración por marca.

**Comportamientos observables**
- **HRM-01.1** El byte 0 es de banderas. Bit 0 = formato del pulso: `0` → pulso = byte 1 (entero sin signo de 8 bits, 0–255); `1` → pulso = bytes 1-2, entero sin signo de 16 bits *little-endian* (0–65535).
- **HRM-01.2** Contacto = bits 1-2, es decir `(banderas >> 1) & 0b11`: `0` o `1` → el sensor **no informa** contacto; `2` → **sin contacto**; `3` → **con contacto**.
- **HRM-01.3** Bit 3 (energía gastada) y bit 4 (intervalos RR) se ignoran por completo: no se leen ni se validan; los bytes que vienen después del pulso no afectan el resultado.
- **HRM-01.4** Paquete inválido → "sin lectura" (se descarta): vacío; formato de 8 bits con menos de 2 bytes; formato de 16 bits con menos de 3 bytes.
- **HRM-01.5** No se valida el rango: 0, 255 o 300 se aceptan tal cual.
- **HRM-01.6** Bytes de sobra y bits reservados (5-7) se ignoran.

**Casos borde:** formato de 16 bits con solo 2 bytes se descarta (no "cae" a 8 bits). Banderas `0x02` (bits 1-2 = `01`) se interpretan como "no informa contacto".
**UI:** ninguna. **Datos persistidos:** ninguno.
**Archivos legacy:** `lib/core/bluetooth/heart_rate_parser.dart:4-11` (lectura: pulso + contacto opcional), `:21-22` (UUIDs), `:24-40` (decodificación).
**Tests legacy** (`test/heart_rate_parser_test.dart`):
- "bpm en uint8 sin información de contacto": `[0x00, 72]` → pulso 72, contacto "no informa".
- "bpm en uint16 little-endian": `[0x01, 0x2C, 0x01]` → 300.
- "bits de contacto…": `[0x02, 70]` → no informa; `[0x04, 0]` → sin contacto; `[0x06, 65]` → con contacto y pulso 65.
- "ignora Energy Expended y RR intervals": `[0x18, 80, 0x10, 0x00, 0x00, 0x04]` → pulso 80.
- "bytes vacíos o truncados devuelven null": `[]`, `[0x00]`, `[0x01, 72]` → sin lectura.
- "UUIDs del perfil": servicio y característica exactos de la tabla 0.1.

**Dependencias:** ninguna (lógica pura). La consume F-HRM-04.
**Mejora EXR:** el número que ve el remero es siempre el que mide el sensor, sin "suavizados" que lo falseen. Como **agregado** (ver preguntas abiertas): cuando el sensor informa "sin contacto", decirlo en lenguaje humano ("Ajustá la banda: no detecta contacto") en vez de solo mostrar `--`.

---

### F-HRM-02 — Búsqueda de pulsómetros

**Valor para el usuario:** encontrar el sensor cercano con un toque, viendo solo pulsómetros (no auriculares, TVs ni el remo).

**Comportamientos observables**
- **HRM-02.1** Al iniciar la búsqueda se cancelan las dos cadencias de reconexión y cualquier intento de conexión en vuelo (ese intento deja el estado en `disconnected` y cierra su enlace en silencio; ver F-HRM-11).
- **HRM-02.2** Si la plataforma no soporta Bluetooth LE, la búsqueda no hace nada: sin cambio de estado y sin error visible.
- **HRM-02.3** Si otra búsqueda de pulsómetro seguía esperando su plazo, termina ya; la nueva es la dueña del estado.
- **HRM-02.4** Estado → `scanning` y la lista de resultados arranca **vacía** antes de que llegue cualquier resultado.
- **HRM-02.5** Solo se listan periféricos que anuncian 0x180D. En Android e iOS/macOS filtra el sistema. En Windows (y Linux) la app filtra por los UUID anunciados (un `180d` de 16 bits equivale al UUID de 128 bits). En iOS/macOS **nunca** se filtra en la app (el servicio puede venir en los "overflow UUIDs" y no figurar en la lista anunciada).
- **HRM-02.6** Cada sensor aparece **una sola vez** (se identifica por su id). Un anuncio nuevo del mismo sensor actualiza su fila sin moverla. Orden = orden de primer descubrimiento. Cada lote de resultados reemite la lista completa.
- **HRM-02.7** Solo cuentan resultados de **esta** búsqueda: nunca se heredan los de una búsqueda anterior ni los de la búsqueda del remo (y viceversa: el reloj no aparece en la lista del remo).
- **HRM-02.8** Dura 10 s. Termina antes si el usuario elige un sensor, si se detiene la búsqueda, si otra acción la reemplaza o si el sistema informa un error nativo de escaneo (en ese caso termina **en silencio**, sin error visible).
- **HRM-02.9** Al terminar, si el estado sigue en `scanning`, pasa a `disconnected`: la tarjeta vuelve a "Sin sensor" o a "{recordado} · no encontrado". La reconexión automática **no** se reanuda sola.
- **HRM-02.10** Detener búsqueda: solo detiene el escaneo del sistema si el activo es el del pulsómetro (podría ser el del remo); `scanning` → `disconnected`. No hay botón en la UI (solo lo usan los tests).
- **HRM-02.11** Si arrancar el escaneo falla con un error, se muestra en la tarjeta con `hrmConnectError` ("Error al conectar: {error}") y el estado vuelve a `disconnected`.
- **HRM-02.12** **Exclusión mutua con el remo** (nunca dos búsquedas a la vez): mientras el remo está `scanning` o `connecting`, "Buscar", "Buscar otro" y "Conectar" del pulsómetro están deshabilitados; mientras el pulsómetro está `scanning`, el botón del remo "Buscar dispositivos" está deshabilitado (además de sus propias reglas).
- **HRM-02.13** Una búsqueda del usuario tiene prioridad sobre la reconexión automática: mientras está activa, el arranque automático no hace nada.
- **HRM-02.14** Diagnóstico de campo en el log: `[HRM] Escaneando sensores 0x180D durante 10s` al empezar y una línea `[HRM] Encontrado <id> "<nombre>" rssi=<dBm> uuids=[…]` por cada sensor nuevo que pasa el filtro. Sin línea "Encontrado" = nada cercano anuncia `180d`.

**Casos borde y errores:** búsqueda sin resultados → vuelve a "Sin sensor" con la pista del smartwatch. Búsqueda **con** resultados en la que el usuario no elige nada → al vencer los 10 s la lista desaparece y vuelve a "Sin sensor"/"recordado". En Windows el reloj suele aparecer como "Sensor desconocido" + dirección (su anuncio no trae nombre).
**UI:** ver F-HRM-13 (spinner + "Buscando sensores..." + lista). Durante la búsqueda no hay mensaje de lista vacía, ni botón para cancelar, ni intensidad de señal visible.
**Datos persistidos:** ninguno.
**Archivos legacy:** `lib/core/bluetooth/heart_rate_service.dart:104-183` (búsqueda, detener, fin de espera), `:110-112` (cancelaciones), `:113` (sin soporte), `:117-118` (estado + lista vacía), `:122-125` (filtro por servicio), `:136` (filtro en la app), `:137-145` (deduplicación y emisión), `:148-151` (error nativo), `:155-157` (error propagado), `:162-166` (fin), `:211` (prioridad sobre autoConnect); `lib/core/bluetooth/scan_filter.dart:12-20`; `lib/features/device/heart_rate_card.dart:22-24, 108, 218`; `lib/features/device/device_screen.dart:348, 397`.
**Tests legacy:**
- `heart_rate_card_test` "sin sensor → buscar → …": tocar Buscar muestra "Buscando sensores..." y la fila "Pulsómetro simulado".
- `heart_rate_card_test` "los escaneos del remo y del pulsómetro se excluyen": con el remo desconectado, "Buscar dispositivos" está habilitado; al buscar pulsómetro se deshabilita; al detener la búsqueda se rehabilita.
- `heart_rate_card_test` "mientras el remo escanea no se puede buscar pulsómetro": "Buscar" del pulsómetro deshabilitado con el remo en `scanning` y en `connecting`; habilitado con el remo en `disconnected`.
- `heart_rate_card_test` "en landscape la vista de escaneo no desborda": ver F-HRM-13.
- `simulated_heart_rate_service_test` "startScan emite el sensor sintético…" y "el escaneo expira a disconnected si no se conecta" (a los 9 s sigue buscando, a los 10 s `disconnected`).
- `heart_rate_provider_test` "refleja escaneo…": tras buscar, *buscando* = sí y 1 resultado.
- `scan_filter_test` (compartido con el remo): con filtro en la app solo pasan los que anuncian 0x180D (vacío o `febe` → no; `180d` de 16 bits → sí; `febe` + 0x180D → sí); sin filtro en la app pasa todo.

**Dependencias:** área BLE (estado del remo para la exclusión mutua; filtro compartido), F-HRM-11, F-HRM-13, F-HRM-16.
**Mejora EXR:** barrido animado tipo "radar/latido" durante los 10 s con indicación del tiempo restante; los sensores entran a la lista con una aparición suave, sin saltos de layout; intensidad de señal visible (agregado); al terminar sin resultados, un mensaje amable ("No encontramos pulsómetros cerca") junto a la pista del smartwatch.

---

### F-HRM-03 — Conexión manual a un sensor de la lista

**Valor para el usuario:** elegir su sensor y tenerlo andando en segundos, con un error claro si no es un pulsómetro.

**Comportamientos observables**
- **HRM-03.1** Tocar una fila conecta a ese sensor y muestra "Conectando a {nombre de la fila}..." (`hrmConnectingTo`).
- **HRM-03.2** Si ya hay un intento en curso (`connecting`), el pedido se **ignora en silencio**.
- **HRM-03.3** Cancela las cadencias automáticas, detiene cualquier escaneo activo del sistema y la búsqueda deja de esperar su plazo.
- **HRM-03.4** Estado → `connecting`. Conexión directa (no la conexión diferida del sistema), con tope de **15 s**, sin negociar MTU.
- **HRM-03.5** Ya conectado, se buscan el servicio 0x180D y su característica 0x2A37. Si falta alguno: se desconecta, estado `disconnected`, error **incompatible** (`hrmIncompatible`) y el sensor **no** se recuerda.
- **HRM-03.6** Se activan las notificaciones de 0x2A37 y empiezan a llegar lecturas (F-HRM-04).
- **HRM-03.7** Se recuerda el sensor (F-HRM-06).
- **HRM-03.8** Recién después se arma la vigilancia de caída (desde ahí, una desconexión no pedida dispara F-HRM-08); armarla al final evita captar estados residuales de la conexión.
- **HRM-03.9** Se arma el watchdog (F-HRM-05) y estado → `connected`. Log `[HRM] Conectado a {nombre}`.
- **HRM-03.10** Cualquier falla (timeout de 15 s, rechazo, error al descubrir servicios o al activar notificaciones, diálogo de emparejamiento lento) → se cierra el enlace (para no dejar una conexión colgada invisible), pulso → 0 si no lo era, estado `disconnected` y el error se muestra en la tarjeta (`hrmConnectError` con el texto técnico, o `hrmIncompatible`).
- **HRM-03.11** La conexión manual **no** programa reintentos si falla. Un sensor que ya estaba recordado sigue recordado.
- **HRM-03.12** Si otra acción del usuario reemplaza el intento (buscar, otro sensor, desconectar, olvidar, cerrar la app), se aborta **sin error visible** (F-HRM-11).
- **HRM-03.13** Al llegar a `connected` se borra cualquier error anterior de la tarjeta y se descarta el nombre elegido (desde ahí se muestra el nombre conectado).

**Casos borde:** conectar otro sensor con éxito reemplaza al recordado. Si ese sensor nuevo no tiene nombre, queda el **nombre del sensor anterior** (ver HRM-06.1 y preguntas abiertas). Durante los hasta 15 s de un intento manual la tarjeta no ofrece ningún botón (no se puede cancelar).
**UI:** spinner de 16 px + "Conectando a {nombre}..." (F-HRM-13). Error en rojo bajo el contenido.
**Datos persistidos:** `hrm.deviceId`, `hrm.deviceName` al conectar con éxito.
**Archivos legacy:** `lib/core/bluetooth/heart_rate_service.dart:187-203` (conexión manual), `:229-306` (secuencia común de conexión), `:241-246` (parámetros), `:248-253` (incompatible), `:254-258` (notificaciones), `:259` (recordar), `:263-272` (vigilancia de caída), `:274-276` (watchdog + conectado), `:294-304` (falla), `:308-319` (búsqueda de 0x180D/0x2A37); `lib/features/device/heart_rate_provider.dart:25, 28, 65-70`; `lib/features/device/heart_rate_card.dart:79-86, 143`.
**Tests legacy:**
- `heart_rate_card_test` "sin sensor → buscar → conectar…": tras tocar la fila, estado `connected`, se ve "Pulsómetro simulado" y `--` (todavía sin lectura).
- `heart_rate_provider_test` "refleja escaneo, conexión…": mientras conecta, el nombre a mostrar es el elegido; al conectar, el nombre conectado es "Pulsómetro simulado" y el nombre "conectando" queda vacío.
- `heart_rate_provider_test` "una excepción del servicio queda en error…": conectar a un sensor incompatible deja el error incompatible y se reconoce como tal.
- `heart_rate_provider_test` "isIncompatible distingue el sensor incompatible": incompatible → sí; otro error → no.
- `simulated_heart_rate_service_test` "startScan emite… y connect emite bpm": conectar el sensor sintético → `connected` con su nombre.

**Dependencias:** F-HRM-02, F-HRM-04, F-HRM-05, F-HRM-06, F-HRM-08, F-HRM-11, F-HRM-12.
**Mejora EXR:** conexión con feedback progresivo (conectando → verificando que sea un pulsómetro → listo), el corazón "arranca" a latir con la primera lectura y una confirmación suave (sonido/vibración). Errores en lenguaje humano con la acción sugerida ("Este dispositivo no mide pulso. Probá con otro sensor") en lugar del texto técnico.

---

### F-HRM-04 — Pulso en vivo

**Valor para el usuario:** ver su frecuencia cardíaca real, segundo a segundo, mientras rema (o aunque no esté remando).

**Comportamientos observables**
- **HRM-04.1** Cada notificación válida (F-HRM-01) rearma el watchdog y emite un pulso. Las notificaciones inválidas se descartan y **no** rearman el watchdog.
- **HRM-04.2** Sensor "sin contacto" → se emite **0**. "Con contacto" o "no informa" → se emite el pulso leído (aunque sea 0).
- **HRM-04.3** Se emite en **cada** notificación (típicamente 1 por segundo), aunque el valor se repita. Sin promedios ni suavizado.
- **HRM-04.4** `0` significa "sin lectura válida" en toda la app.
- **HRM-04.5** El último pulso emitido queda disponible como valor actual; vale 0 al inicio.
- **HRM-04.6** Al cortarse la conexión (caída o Desconectar) se emite 0 si el último valor no era 0.
- **HRM-04.7** El pulso fluye independientemente del remo (remo quieto, sin conectar o sin sesión).

**Casos borde:** un sensor que manda 0 con contacto "no informado" muestra `--`. Valores fuera de rango fisiológico se muestran tal cual.
**UI:** tarjeta conectada: número grande o `--` si es 0, con la unidad "bpm" (F-HRM-13). En entrenamiento lo muestra el HUD (área HUD, solo si > 0).
**Datos persistidos:** ninguno propio; se guarda a través de F-HRM-14 (`data_points.heart_rate`).
**Archivos legacy:** `lib/core/bluetooth/heart_rate_service.dart:58-59` (valor actual), `:321-326` (notificación), `:328-331` (emisión), `:393-401` (0 al limpiar la conexión).
**Tests legacy:**
- `heart_rate_card_test` "sin sensor → … → bpm": antes de la primera lectura se ve `--`; después de un segundo se ve el mismo número que emitió el sensor (> 0) y desaparece `--`.
- `heart_rate_provider_test` "refleja escaneo…": el pulso de la tarjeta = pulso del simulador (> 0); tras Desconectar, 0.
- `simulated_heart_rate_service_test` "…connect emite bpm cada segundo": 3 lecturas en 3 s; la última = pulso del simulador (> 0) = valor actual.
- `workout_provider_hr_test` (F-HRM-14): un 0 del sensor también manda.

**Dependencias:** F-HRM-01, F-HRM-05, F-HRM-14.
**Mejora EXR:** el número cambia con una transición corta y suave al valor exacto (nunca un valor inventado); el corazón late al ritmo real del pulso; cuando no hay contacto, el corazón se apaga con suavidad en vez de un salto brusco a `--`. Legible a 2–3 m (el usuario está sentado en el remo).

---

### F-HRM-05 — Watchdog de lectura vencida

**Valor para el usuario:** si el reloj se apaga, se sale de la muñeca o la app emisora se pausa, la app no sigue mostrando un pulso viejo como si fuera actual.

**Comportamientos observables**
- **HRM-05.1** Al conectar se arma un plazo de **10 s**.
- **HRM-05.2** Cada notificación válida reinicia el plazo a 10 s.
- **HRM-05.3** Si vencen 10 s sin notificación válida → se emite **0 una sola vez** (no se repite cada 10 s mientras siga sin datos).
- **HRM-05.4** La conexión **no** se corta y no arranca ningún reintento.
- **HRM-05.5** La próxima notificación válida restaura el valor real y rearma el plazo.
- **HRM-05.6** El watchdog se detiene al desconectar, al caerse la conexión, al abortar un intento y al cerrar la app.

**Casos borde:** sensor que conecta pero nunca notifica → a los 10 s se emite 0 (ya era 0; no cambia lo visible). Notificaciones inválidas no cuentan como señal de vida.
**UI:** la tarjeta pasa a `--`; en entrenamiento el pulso pasa a 0 y el HUD oculta la métrica (área HUD).
**Datos persistidos:** los puntos guardados durante ese lapso tienen `heart_rate = 0` (y no cuentan en el promedio/máximo, área HIS).
**Archivos legacy:** `lib/core/bluetooth/reconnect_loop.dart:73-91` (plazo genérico), `lib/core/bluetooth/heart_rate_service.dart:37-38, 86-89, 274, 324, 380, 396, 440`.
**Tests legacy:** `reconnect_loop_test` grupo "StaleWatchdog" — "dispara tras el timeout sin touch, y touch lo rearma": con plazo de 10 s, tocar a los 0 s y a los 9 s no dispara hasta los 19 s; dispara exactamente una vez; 30 s más no vuelve a disparar; tocar y detener → no dispara.
**Dependencias:** F-HRM-04.
**Mejora EXR:** estado "sin señal del sensor" explícito y elegante (corazón en gris, leyenda breve) en la tarjeta y en el HUD; al volver la señal, el valor reaparece con suavidad.

---

### F-HRM-06 — Memoria del último sensor

**Valor para el usuario:** se empareja una vez y la app lo reconoce siempre, sin buscarlo de nuevo.

**Comportamientos observables**
- **HRM-06.1** Se guarda al conectar con éxito (después de activar notificaciones): `hrm.deviceId` = id del dispositivo (texto); `hrm.deviceName` = nombre del sistema, o si está vacío el nombre anunciado. Si ambos están vacíos **no** se escribe nombre y queda el anterior (en memoria y en preferencias).
- **HRM-06.2** Se carga al arrancar y en cada "Conectar": el id; el nombre **solo si hay id** (un nombre huérfano no se hereda al próximo sensor).
- **HRM-06.3** Nombre recordado para mostrar = nombre guardado; si no hay, el id; si no hay id, "sin recordado".
- **HRM-06.4** Nombre conectado para mostrar (solo en `connected`) = nombre del sistema del dispositivo; si está vacío, el recordado; si no, el id.
- **HRM-06.5** Olvidar (o Desconectar) borra ambas claves y la memoria en curso.
- **HRM-06.6** Nunca se recuerda un sensor incompatible ni uno cuya conexión falló antes de activar notificaciones.
- **HRM-06.7** Un intento abortado nunca vuelve a recordar un sensor olvidado.
- **HRM-06.8** Un solo sensor recordado; conectar otro lo reemplaza ("Buscar otro" no olvida hasta que otro conecta con éxito).
- **HRM-06.9** En modo simulador no se persiste nada.

**Casos borde:** id sin nombre → la tarjeta muestra el id. Direcciones privadas rotativas: el id guardado puede dejar de servir (F-HRM-16). Al abrir la app, hasta que se lee la memoria (instantes) la tarjeta puede mostrar "Sin sensor".
**UI:** "{nombre} · reconectando..." / "{nombre} · no encontrado"; nombre en el estado conectado.
**Datos persistidos (preferencias):** `hrm.deviceId` (texto: dirección tipo `AA:BB:CC:DD:EE:FF` en Android/Windows; identificador del sistema en iOS/macOS) y `hrm.deviceName` (texto). Sin base de datos.
**Archivos legacy:** `lib/core/bluetooth/heart_rate_service.dart:34-35, 91-98, 403-421, 353-363`; `lib/core/bluetooth/simulated_heart_rate_service.dart:11, 41-48`.
**Tests legacy:** sin test directo del servicio real (depende de la plataforma Bluetooth). Indirectos: `simulated_heart_rate_service_test` (sin recordado al inicio; recordado solo durante la caída simulada), `heart_rate_provider_test` "refleja escaneo…" (*tiene recordado* = no al inicio, sí durante la caída simulada).
**Dependencias:** F-HRM-03, F-HRM-07, F-HRM-10.
**Mejora EXR:** el sensor es "tu sensor": nombre e ícono persistentes, reconocido apenas se abre la app.

---

### F-HRM-07 — Reconexión automática al arrancar (y botón "Conectar")

**Valor para el usuario:** abrir la app, ponerse la banda y empezar a remar sin tocar nada.

**Comportamientos observables**
- **HRM-07.1** Se dispara sola al abrir la app (modo real); un error en ese disparo solo va al log (`[HRM] autoConnect: …`).
- **HRM-07.2** Precondiciones: hay sensor recordado, el estado es `disconnected` y no hay una búsqueda del usuario en curso. Si no se cumplen, no hace nada.
- **HRM-07.3** Cancela la cadencia de caída y arranca la de arranque: **primer intento inmediato**, conexión directa al id recordado **sin buscar** (Android por dirección; iOS por el identificador que el sistema ya conoce).
- **HRM-07.4** Si falla, espera **10 s** desde el fin del intento fallido y reintenta. Máximo **6 intentos en total**.
- **HRM-07.5** Tras el 6.º fallo se rinde: *reintentando* = no → la tarjeta pasa a "{nombre} · no encontrado" y se habilita "Conectar".
- **HRM-07.6** Cada intento dura hasta 15 s → peor caso **140 s**.
- **HRM-07.7** Los fallos de los intentos automáticos **nunca** se muestran como error en la tarjeta (solo log `[HRM] Reintento fallido: …`).
- **HRM-07.8** Un sensor incompatible cuenta como intento fallido.
- **HRM-07.9** Durante toda la cadencia (intentos y esperas) la tarjeta muestra "{nombre} · reconectando..." con "Conectar" deshabilitado y "Buscar otro" y "Olvidar" disponibles (no solo un spinner).
- **HRM-07.10** "Conectar" (estado recordado) relanza **la misma cadencia desde cero** (6 intentos, 10 s), relee la memoria y limpia el error de la tarjeta.
- **HRM-07.11** Éxito → `connected`, se detiene la cadencia y queda armada la vigilancia de caída (F-HRM-08).
- **HRM-07.12** Cualquier acción del usuario (Buscar otro, elegir sensor, Desconectar, Olvidar) la cancela.

**Casos borde:** Bluetooth apagado al arrancar → los intentos fallan y siguen su cadencia. Si el usuario usa "Buscar otro" y no elige nada, la cadencia no se reanuda: queda "no encontrado". Dirección rotativa → nunca reconecta → "no encontrado".
**UI:** estado "recordado" (F-HRM-13).
**Datos persistidos:** lee `hrm.deviceId` / `hrm.deviceName`.
**Archivos legacy:** `lib/main.dart:52-54, 68-77`; `lib/core/bluetooth/heart_rate_service.dart:40-42, 76-81, 205-227`; `lib/features/device/heart_rate_provider.dart:72-78`; `lib/features/device/heart_rate_card.dart:73-78, 198-215`.
**Tests legacy:** `reconnect_loop_test` "se rinde tras maxAttempts y avisa" y "start() desde onGiveUp arranca una corrida nueva" (motor, F-HRM-09); `heart_rate_provider_test` "una excepción del servicio queda en error y se limpia al reintentar" (Conectar limpia el error). Sin test del arranque real (depende de la plataforma).
**Dependencias:** F-HRM-06, F-HRM-09, F-HRM-11, F-HRM-13; área APP (arranque).
**Mejora EXR:** al abrir la app el sensor "aparece" solo con un aviso discreto ("Polar H10 conectado") y el corazón empieza a latir; mientras lo busca, un indicador sutil de progreso; si no lo encuentra, una sugerencia útil ("¿Está encendido y puesto?") junto a "Conectar".

---

### F-HRM-08 — Reconexión automática tras una caída

**Valor para el usuario:** si la banda pierde señal en medio de una serie, vuelve sola sin que tenga que bajarse del remo.

**Comportamientos observables**
- **HRM-08.1** Se dispara cuando el sistema informa una desconexión estando conectado **y no la pidió el usuario**.
- **HRM-08.2** De inmediato: pulso → 0 (si no lo era), **después** estado `disconnected` (en ese orden), y arranca la cadencia de caída. Log `[HRM] Conexión perdida, reintentando cada 3s`.
- **HRM-08.3** Primer intento inmediato; cada fallo → 3 s → nuevo intento; **sin límite**.
- **HRM-08.4** Termina con éxito (`connected`), con una acción del usuario o al cerrar la app.
- **HRM-08.5** El sensor sigue recordado; la tarjeta muestra "{nombre} · reconectando..." con "Conectar" deshabilitado.
- **HRM-08.6** Mientras dura, el pulso del entrenamiento queda en 0 (o en el del remo si este lo manda por FTMS; F-HRM-14).
- **HRM-08.7** Al reconectar vuelven las lecturas sin intervención.
- **HRM-08.8** Desconectar (manual) nunca dispara esta reconexión.

**Casos borde:** caída durante un entrenamiento activo (la sesión sigue; los puntos de ese lapso guardan 0). App cerrada durante la caída → al reabrir aplica F-HRM-07. Caídas repetidas: cada una relanza la cadencia.
**UI:** estado "recordado · reconectando..." (F-HRM-13).
**Datos persistidos:** ninguno nuevo.
**Archivos legacy:** `lib/core/bluetooth/heart_rate_service.dart:44-45, 82-85, 263-272`.
**Tests legacy:**
- `heart_rate_provider_test` "refleja escaneo…": tras la caída simulada, no conectado, *reintentando* = sí, *tiene recordado* = sí; a los 3 s vuelve a conectado y *reintentando* = no.
- `simulated_heart_rate_service_test` "simulateDisconnect cae y reconecta a los 3 s…".
- `simulator_overlay_test` (botón "Simular caída del pulsómetro"): estado `disconnected` y *reintentando* = sí.
- `workout_provider_hr_test` "orden real de caída: bpm 0 y después disconnected".
- `reconnect_loop_test` "intenta de inmediato, reintenta cada intervalo y para al tener éxito" (cadencia de 3 s).

**Dependencias:** F-HRM-03, F-HRM-09, F-HRM-11, F-HRM-14.
**Mejora EXR:** durante el entrenamiento la caída se comunica con un indicador no intrusivo en el HUD ("Pulsómetro reconectando…") que no interrumpe la sesión ni tapa la escena; al volver, confirmación breve y el valor reaparece con suavidad.

---

### F-HRM-09 — Motor de reintentos (cadencia)

**Valor para el usuario:** reintentos predecibles que no se pisan entre sí ni siguen andando cuando el usuario ya decidió otra cosa.

**Comportamientos observables**
- **HRM-09.1** Iniciar: arranca (o reinicia desde cero, con el contador de intentos en 0) y el **primer intento es inmediato**.
- **HRM-09.2** Éxito → se detiene. Fallo **o error** → espera el intervalo y reintenta.
- **HRM-09.3** El intervalo se cuenta desde que terminó el intento fallido (no desde que empezó).
- **HRM-09.4** Con máximo N: al llegar a N intentos fallidos se detiene y avisa "me rendí" una vez.
- **HRM-09.5** Sin máximo: reintenta indefinidamente.
- **HRM-09.6** Cancelar: se detiene; un intento en vuelo no se interrumpe, pero su resultado se descarta y no se programa otro.
- **HRM-09.7** Reiniciar con un intento en vuelo: el resultado del intento viejo se descarta (un éxito tardío no termina la corrida nueva) y la corrida nueva intenta de inmediato.
- **HRM-09.8** Se puede reiniciar desde el propio aviso de "me rendí" (arranca una corrida nueva completa).
- **HRM-09.9** *Reintentando* = hay una corrida activa (incluye las esperas).

**UI / datos:** ninguno directo.
**Archivos legacy:** `lib/core/bluetooth/reconnect_loop.dart:4-71`.
**Tests legacy** (`test/reconnect_loop_test.dart`, grupo "ReconnectLoop"):
- "intenta de inmediato, reintenta cada intervalo y para al tener éxito": intervalo 3 s, éxito al 3.er intento → 1 intento al inicio, 2 a los 3 s, 3 a los 6 s y se detiene; 30 s después siguen siendo 3.
- "se rinde tras maxAttempts y avisa": intervalo 10 s, máximo 3 → en 25 s hubo 3 intentos, avisó y se detuvo; 60 s más: siguen 3.
- "cancel frena los reintentos y una excepción cuenta como fallo": intervalo 1 s con error → 3 intentos en 2 s; tras cancelar, ninguno más en 10 s.
- "start() durante un intento en vuelo descarta ese resultado y arranca de cero": el reinicio intenta de inmediato (contador 1); el éxito tardío del viejo no detiene la nueva; tras un fallo, a los 3 s hay un 3.er intento (contador 2).
- "cancel() durante un intento en vuelo no programa otro intento".
- "start() desde onGiveUp arranca una corrida nueva": máximo 2, intervalo 1 s → al rendirse reinicia con intento inmediato (3 intentos, sigue activo); al rendirse otra vez (4 intentos) se detiene.

**Dependencias:** ninguna (lógica pura). La usan F-HRM-07 y F-HRM-08.
**Mejora EXR:** invisible; se percibe en F-HRM-07/08 como una app que "sabe lo que hace".

---

### F-HRM-10 — Desconectar y Olvidar

**Valor para el usuario:** soltar el sensor (por ejemplo, para prestarlo o cambiarlo) y que la app no lo vuelva a enganchar sola.

**Comportamientos observables**
- **HRM-10.1** **Desconectar** (estado conectado): cancela cadencias e intento en vuelo; marca la desconexión como pedida (no dispara F-HRM-08); pulso → 0 si no lo era; cierra el enlace de inmediato (también cancela una conexión pendiente sin esperar los 15 s); **olvida el sensor** (borra `hrm.deviceId` y `hrm.deviceName`); estado `disconnected` → tarjeta "Sin sensor".
- **HRM-10.2** Consecuencia: al reabrir la app **no** se reconecta solo.
- **HRM-10.3** **Olvidar** (estado recordado): cancela cadencias, aborta un intento en vuelo (estado `disconnected`, enlace cerrado en silencio), borra la memoria → tarjeta "Sin sensor". No toca una conexión activa (en la UI no está disponible estando conectado).
- **HRM-10.4** Ambas acciones borran el error previo de la tarjeta.
- **HRM-10.5** Si la acción falla, el error queda en la tarjeta; la app no se rompe.
- **HRM-10.6** No hay confirmación ni "deshacer".

**Casos borde:** Olvidar durante "reconectando..." corta la cadencia en el acto. Desconectar durante un entrenamiento: el pulso pasa a 0 (F-HRM-14).
**UI:** botón "Desconectar" (rojo claro, ícono de Bluetooth tachado) en conectado; "Olvidar" (botón de texto, siempre habilitado) en recordado.
**Datos persistidos:** borra `hrm.deviceId` y `hrm.deviceName`.
**Archivos legacy:** `lib/core/bluetooth/heart_rate_service.dart:333-363, 370-391`; `lib/features/device/heart_rate_provider.dart:80-89`; `lib/features/device/heart_rate_card.dart:187-193, 221-225`.
**Tests legacy:**
- `heart_rate_card_test` "… → desconectar": tocar Desconectar vuelve a "Sin sensor".
- `heart_rate_provider_test` "refleja escaneo…": Desconectar → no conectado, pulso 0, más de 3 notificaciones en todo el flujo y ningún temporizador vivo al cerrar.
- `heart_rate_provider_test` "una excepción del servicio…": Desconectar no lanza aunque el servicio falle.
- `simulated_heart_rate_service_test` "… disconnect no reconecta": 10 s después sigue `disconnected` y sin nombre conectado.

**Dependencias:** F-HRM-06, F-HRM-11, F-HRM-14.
**Mejora EXR:** el corazón se apaga con una animación breve y la tarjeta vuelve a su estado inicial con una transición fluida. Una opción de "deshacer" sería agregado (pregunta abierta).

---

### F-HRM-11 — Prioridad de las acciones del usuario sobre lo automático

**Valor para el usuario:** lo que toca pasa al instante; no hay reconexiones fantasma que reaparecen ni estados que se pisan.

**Comportamientos observables**
- **HRM-11.1** Toda acción del usuario (Buscar, Buscar otro, elegir sensor, Desconectar, Olvidar) y el cierre de la app invalidan cualquier intento de conexión en vuelo y cancelan ambas cadencias.
- **HRM-11.2** Un intento invalidado: no cambia el estado visible, no recuerda (ni re-recuerda un sensor olvidado), deja de emitir lecturas y cierra su enlace en silencio, **salvo** que la acción nueva apunte al mismo sensor (en ese caso no le corta el intento nuevo).
- **HRM-11.3** Una conexión manual reemplazada por otra acción no muestra error.
- **HRM-11.4** Si mientras se cerraba un intento fallido arrancó otra acción, ese fallo ya no toca el estado.
- **HRM-11.5** Búsqueda del usuario > reconexión automática (HRM-02.13).
- **HRM-11.6** Pedir conectar mientras ya está conectando se ignora (HRM-03.2).
- **HRM-11.7** Al cerrar la app se detiene todo: búsqueda, intento, cadencias y watchdog (no quedan temporizadores vivos).

**UI:** sin elementos propios.
**Archivos legacy:** `lib/core/bluetooth/heart_rate_service.dart:12-17, 66-69, 110-112, 189-202, 229-234, 277-293, 299-301, 365-383, 435-447`.
**Tests legacy:** `reconnect_loop_test` "start() durante un intento en vuelo…" y "cancel() durante un intento en vuelo…" (descartar resultados viejos); `heart_rate_provider_test` "refleja escaneo…" (sin temporizadores vivos al final); `simulated_heart_rate_service_test` (los 3 casos verifican que no quedan temporizadores tras cerrar). Sin test directo del servicio real.
**Dependencias:** F-HRM-02, F-HRM-03, F-HRM-07, F-HRM-08, F-HRM-09, F-HRM-10.
**Mejora EXR:** respuesta inmediata a cada toque (feedback visual en el mismo instante), sin parpadeos de estados intermedios.

---

### F-HRM-12 — Estado de presentación del pulsómetro

**Valor para el usuario:** la tarjeta siempre muestra un estado coherente, con el nombre correcto y el error de la última acción.

**Comportamientos observables**
- **HRM-12.1** Refleja estado, pulso y lista de resultados del sensor, y avisa cada cambio a la tarjeta.
- **HRM-12.2** Derivados expuestos: conectado, buscando, conectando, *reintentando*, *tiene recordado*, nombre conectado, nombre recordado.
- **HRM-12.3** Nombre mientras conecta = nombre elegido en la lista; si no hay, el recordado; si no, vacío. El nombre elegido vale **solo para ese intento**: cualquier estado distinto de `connecting` lo descarta (los reintentos automáticos muestran el recordado).
- **HRM-12.4** Error: toda falla de una acción (buscar, conectar, Conectar/reintentar, desconectar, olvidar) se captura y queda visible. Se borra al **iniciar** cualquier acción y al llegar a `connected`. **No** se borra al pasar a `disconnected`.
- **HRM-12.5** Distingue el error "incompatible" (texto `hrmIncompatible`) de cualquier otro (`hrmConnectError`).
- **HRM-12.6** Buscar borra el error y vacía la lista antes de empezar.
- **HRM-12.7** Si la tarjeta ya no existe cuando termina una acción lenta (hasta 15 s), no se intenta actualizarla.

**UI:** alimenta F-HRM-13.
**Archivos legacy:** `lib/features/device/heart_rate_provider.dart:1-117` (constructor `:20-39`, derivados `:41-54`, incompatible `:56`, acciones `:58-89`, captura de errores `:91-105`).
**Tests legacy** (`test/heart_rate_provider_test.dart`):
- "refleja escaneo, conexión, bpm y desconexión del servicio": estado inicial `disconnected`, sin recordado, sin resultados → buscar (buscando, 1 resultado) → conectar (nombre elegido mientras conecta; luego conectado con "Pulsómetro simulado" y nombre "conectando" vacío) → pulso = simulador (> 0) → caída simulada (no conectado, reintentando, recordado) → a los 3 s conectado y sin reintentar → Desconectar (no conectado, pulso 0) → más de 3 avisos → sin temporizadores vivos.
- "una excepción del servicio queda en error y se limpia al reintentar": conectar con servicio que falla → error incompatible; "Conectar" (reintentar) → sin error; Desconectar con servicio que falla → no lanza.
- "isIncompatible distingue el sensor incompatible".

**Dependencias:** F-HRM-02 a F-HRM-10.
**Mejora EXR:** sin cambios de comportamiento; se expresa en la tarjeta.

---

### F-HRM-13 — Tarjeta "Pulsómetro" (pestaña Dispositivo)

**Valor para el usuario:** un único lugar para buscar, conectar, ver el pulso y soltar el sensor.

**Comportamientos observables**
- **HRM-13.1** Ubicación. Remo conectado: debajo de la grilla de métricas del remo (12 px de separación) y antes del acceso oculto al debug BLE; la pantalla entera se desplaza. Remo sin conectar: al pie de la vista de búsqueda del remo, dentro de un área de altura acotada con desplazamiento propio (en horizontal no desborda). Bluetooth apagado o sin permiso: la tarjeta **no se muestra** (la vista "Bluetooth apagado"/"permiso denegado" ocupa toda la pantalla).
- **HRM-13.2** Estructura: encabezado (corazón + `hrmTitle` en mayúsculas), cuerpo según estado y, si hay error, una línea de error debajo.
- **HRM-13.3** Selección de cuerpo:

| Condición | Cuerpo | Acciones |
|---|---|---|
| `connected` | nombre conectado (1 línea, elipsis) + número grande (`{bpm}` si > 0, si no `--`) + "bpm" | **Desconectar** |
| `connecting` **y** reintentando **y** tiene recordado | igual que "recordado" | igual que "recordado" |
| `connecting` (resto, p. ej. manual) | spinner + "Conectando a {nombre}..." | ninguna |
| `scanning` | spinner + "Buscando sensores..." + lista de sensores | tocar una fila → conectar |
| `disconnected` **y** tiene recordado | "{nombre recordado} · reconectando..." si reintentando, si no "{nombre recordado} · no encontrado" | **Conectar**, **Buscar otro**, **Olvidar** |
| `disconnected` sin recordado | "Sin sensor" + pista del smartwatch | **Buscar** |

- **HRM-13.4** Habilitación de botones:

| Botón | Deshabilitado cuando |
|---|---|
| Buscar | el remo está `scanning` o `connecting` |
| Buscar otro | el remo está `scanning` o `connecting` |
| Conectar | *reintentando*, o el remo está `scanning` o `connecting` |
| Olvidar | nunca |
| Desconectar | nunca |
| Fila de la lista | nunca |

- **HRM-13.5** Fila de la lista: ícono de corazón vacío en color pulso; título = nombre del sistema, si no el anunciado, si no "Sensor desconocido"; subtítulo = id del dispositivo; chevron a la derecha. Tocar = conectar con ese título como nombre.
- **HRM-13.6** Formatos: pulso como entero sin separadores; `--` cuando es 0; unidad "bpm" alineada por línea de base. Con textos grandes el número **se achica** para entrar (no desborda). Error: máximo 3 líneas con elipsis; incompatible → `hrmIncompatible`; otro → `hrmConnectError` con el texto técnico del error.
- **HRM-13.7** Corazón del encabezado: color pleno si está conectado; al 40 % en cualquier otro estado.
- **HRM-13.8** La grilla de métricas del remo **no** muestra el pulso del sensor (solo el que mande el remo por FTMS y si es > 0).
- **HRM-13.9** Referencia visual legacy (para el Designer; la identidad a conservar es el color `#EF476F`): fondo `#1A2E45`, radio 16, relleno 16, ancho completo, borde superior de 3 px en color pulso al 60 %; título 12 px semibold, espaciado 0,8, color pulso al 70 %; corazón 14 px; número 52 px extra-bold color pulso; "bpm" 14 px color pulso al 70 %; nombre 14 px blanco 70 %; "Sin sensor" 16 px semibold; pista 12 px blanco 38 %; "Buscando sensores..." 13 px blanco 54 %; error 12 px rojo; Desconectar en rojo claro con ícono de Bluetooth tachado; Conectar con ícono de refrescar; Buscar con ícono de búsqueda Bluetooth. Captura de referencia: `docs/image-01.png`.
- **HRM-13.10** Identificadores de prueba usados por los tests legacy (para el mapeo de QA): `hrm-search` (Buscar y Buscar otro), `hrm-retry`, `hrm-forget`, `hrm-disconnect`, `hrm-device-<id>`.

**Casos borde:** "Conectando a ..." con nombre vacío si no hay nombre elegido ni recordado. En el estado conectado sin nombre disponible, la línea del nombre queda vacía. Durante un intento manual no hay forma de cancelar desde la tarjeta.
**Estados vacío / cargando / error:** vacío = "Sin sensor"; cargando = buscando / conectando / reconectando; error = línea roja bajo cualquier estado.
**Datos persistidos:** ninguno propio.
**Archivos legacy:** `lib/features/device/heart_rate_card.dart:1-238`; `lib/features/device/device_screen.dart:117-122` (vista conectada), `:257-258` (grilla del remo), `:348, 397` (exclusión con el remo), `:367` (Bluetooth apagado), `:472-474` (vista de búsqueda); `lib/shared/theme.dart:37`.
**Tests legacy** (`test/heart_rate_card_test.dart`, con remo y pulsómetro simulados, idioma es):
- "sin sensor → buscar → conectar → bpm → desconectar": con el remo conectado se ven "PULSÓMETRO", "Sin sensor" y la pista que contiene "Heart for Bluetooth"; la tarjeta queda debajo de la grilla (hay que desplazarse); flujo completo hasta volver a "Sin sensor".
- "los escaneos del remo y del pulsómetro se excluyen" (ver F-HRM-02).
- "en landscape la vista de escaneo no desborda": pantalla 640 × 360 a densidad 1 → sin desbordes; "Buscar" alcanzable desplazando; al buscar, la fila "Pulsómetro simulado" existe dentro del área desplazable y sigue sin desbordar.
- "mientras el remo escanea no se puede buscar pulsómetro" (ver F-HRM-02).

**Dependencias:** F-HRM-12, F-HRM-02, F-HRM-03, F-HRM-07, F-HRM-10; área BLE (estado del remo, pantalla Dispositivo); área APP (tema, textos).
**Mejora EXR:** tarjeta tipo "dispositivo emparejado" premium: corazón animado que late a la frecuencia real con un brillo sutil del color pulso; transiciones fluidas entre estados sin saltos de altura; lista con señal y entrada suave; microinteracciones al tocar; estado "reconectando" con una animación de búsqueda serena; número enorme y legible a distancia.

---

### F-HRM-14 — Fusión del pulso con los datos del entrenamiento

**Valor para el usuario:** el pulso del sensor (más preciso) reemplaza al que pudiera mandar el remo en todas las pantallas, la sesión guardada y Strava.

**Comportamientos observables**
- **HRM-14.1** Mientras el sensor no emitió ningún pulso, se respeta el pulso que mande el remo por FTMS.
- **HRM-14.2** Cada pulso del sensor (cualquier valor, **incluido 0**, en cualquier estado) pasa a mandar: el pulso del entrenamiento se actualiza al instante, sin tocar las demás métricas, y se avisa a las pantallas (aunque el remo esté quieto o desconectado).
- **HRM-14.3** Los paquetes posteriores del remo conservan el pulso del sensor (se reemplaza su campo de pulso).
- **HRM-14.4** Un 0 del sensor (sin contacto, vencido) también manda: no se rellena con el del remo.
- **HRM-14.5** Si el sensor pasa a cualquier estado distinto de `connected` (`disconnected`, `connecting` o `scanning`) mientras mandaba: deja de mandar, el pulso del entrenamiento pasa a **0** y se avisa. El próximo paquete del remo repone su propio valor.
- **HRM-14.6** Si el sensor no había emitido pulso, sus cambios de estado no generan ningún aviso ni cambio.
- **HRM-14.7** Pasar a `connected` no cambia nada; un pulso recibido durante `connecting` se conserva al pasar a `connected`.
- **HRM-14.8** Todo lo que viene después lee este pulso fusionado (sección 3).

**Casos borde:** con el remo simulado, el remo manda 0 por FTMS, así que sin sensor no hay pulso. Caída durante un entrenamiento → los puntos guardados en ese lapso tienen 0.
**UI:** HUD de entrenamiento y escena inmersiva (área HUD).
**Datos persistidos:** `data_points.heart_rate` (área WKT/DB).
**Archivos legacy:** `lib/features/workout/workout_provider.dart:46, 65-70, 82-86, 220-237, 324-325`.
**Tests legacy** (`test/workout_provider_hr_test.dart`):
- "el pulsómetro manda sobre el FTMS y se actualiza sin paquetes del remo": remo 90 → 90; sensor 142 → 142 (potencia 100 intacta, 2 avisos); remo 90/120 W → sigue 142 con 120 W; sensor 0 → 0; sensor 130 y luego `disconnected` → 0; siguiente paquete del remo → 90.
- "orden real de caída: bpm 0 y después disconnected": remo 90, sensor 140 → 140; 0 + `disconnected` → 0; remo 90 → 90.
- "un bpm recibido durante connecting se conserva al pasar a connected": `connecting`, 120, `connected` → 120; remo 90 → sigue 120.
- "connected del sensor sin bpm previo no toca los datos": `connected` y `disconnected` sin pulso → 0 avisos.

**Dependencias:** F-HRM-04, F-HRM-08, F-HRM-10; áreas WKT, HUD, HIS, STR, DB.
**Mejora EXR:** el pulso se integra al HUD con su color identitario; los cambios de fuente (sensor ↔ remo) no producen parpadeos.

---

### F-HRM-15 — Pulsómetro simulado (modo desarrollo)

**Valor para el usuario (equipo/QA):** probar todo el flujo del pulsómetro, incluida la caída y la reconexión, sin hardware.

**Comportamientos observables**
- **HRM-15.1** Activo solo en modo simulador (flag de compilación). Reemplaza al pulsómetro real: no existe "remo simulado + sensor real".
- **HRM-15.2** No persiste nada; el arranque automático no hace nada → la tarjeta arranca en "Sin sensor".
- **HRM-15.3** Buscar: cancela una reconexión simulada pendiente; estado `scanning`; lista **inmediata** con un único sensor: nombre "Pulsómetro simulado", id `SIM:HRM`, señal −50, anuncia 0x180D. A los 10 s, si no se conectó → `disconnected`.
- **HRM-15.4** Detener búsqueda: `disconnected` si estaba buscando.
- **HRM-15.5** Conectar (a cualquier dispositivo): pasa **directo a `connected`** (sin `connecting`); emite cada 1 s el pulso del simulador físico; la primera lectura llega al segundo (la tarjeta muestra `--` hasta entonces). Si ya está conectado, nada.
- **HRM-15.6** Nombre conectado = "Pulsómetro simulado" (solo en `connected`).
- **HRM-15.7** Desconectar: cancela una reconexión pendiente, corta la emisión, pulso → 0 si no lo era, `disconnected`.
- **HRM-15.8** Olvidar: cancela una reconexión pendiente y reemite el estado; **no** corta una conexión activa.
- **HRM-15.9** "Simular caída del pulsómetro" (panel del simulador, ícono de corazón roto; solo si está conectado): pulso 0, `disconnected` con *reintentando* = sí y nombre recordado = "Pulsómetro simulado" (tarjeta "Pulsómetro simulado · reconectando..."); a los **3 s** reconecta solo; luego *reintentando* = no y sin recordado.
- **HRM-15.10** Modelo de pulso (área SIM): arranca en 70; en cada segundo simulado, objetivo = remando ? mín(190, 90 + 0,35 × watts) : 70; pulso += (objetivo − pulso) × 0,1; se informa redondeado. El pulsómetro simulado emite el último valor calculado (su segundo no está sincronizado con el del remo: puede ir un paso atrás o adelante).
- **HRM-15.11** El remo simulado manda pulso 0 por FTMS (como un monitor real): el pulso llega solo por el pulsómetro simulado.
- **HRM-15.12** Si el remo simulado no está conectado (no avanza), el pulso simulado queda congelado en el último valor pero se sigue emitiendo cada segundo.
- **HRM-15.13** Textos del simulador sin localizar (siempre en español).

**UI:** misma tarjeta (F-HRM-13); botón en el panel del simulador.
**Datos persistidos:** ninguno.
**Archivos legacy:** `lib/core/bluetooth/simulated_heart_rate_service.dart:1-156`; `lib/main.dart:52-54`; `lib/core/dev/simulator_overlay.dart:71-72, 162-166`; `lib/core/dev/rowing_simulator.dart:31-37, 56-57`; `lib/core/bluetooth/simulated_ble_service.dart:95`.
**Tests legacy:**
- `simulated_heart_rate_service_test` "startScan emite el sensor sintético y connect emite bpm cada segundo": estado inicial `disconnected`, sin recordado; buscar → estados `[scanning]`, un resultado con el nombre y el id exactos; conectar → `connected`; tras 3 s, 3 lecturas, la última = pulso del simulador (> 0); al cerrar no quedan temporizadores.
- "el escaneo expira a disconnected si no se conecta": 9 s buscando, 10 s `disconnected`.
- "simulateDisconnect cae y reconecta a los 3 s; disconnect no reconecta": 1 lectura en 1 s; caída → reintentando, recordado = nombre, última lectura 0; a los 2 s estados `[connected, disconnected]`; a los 3 s `connected`, sin reintentar, sin recordado; Desconectar → 10 s después `disconnected` sin nombre conectado.
- `simulator_overlay_test`: el botón tira el sensor conectado → `disconnected` y reintentando.
- `heart_rate_card_test` y `heart_rate_provider_test` usan este sensor.

**Dependencias:** área SIM (simulador físico y panel), F-HRM-13, F-HRM-14.
**Mejora EXR:** el modo simulador debe permitir demostrar la experiencia completa (latido, caída, reconexión) con la misma fluidez que un sensor real.

---

### F-HRM-16 — Compatibilidad de sensores y plataformas (restricciones de producto)

**Valor para el usuario:** que funcione con lo que ya tiene (banda o reloj) y que la app le explique qué hacer si no aparece.

**Comportamientos y restricciones observables**
- **HRM-16.1** Cualquier sensor con el perfil estándar Heart Rate: bandas pectorales (Polar, Garmin, Wahoo, Coospo…), brazaletes y relojes con app emisora.
- **HRM-16.2** Los smartwatches no emiten por defecto (Samsung Health en Galaxy Watch, Apple Watch): necesitan una app emisora (Heart for Bluetooth en Wear OS; HeartCast o ECHO HR en Apple Watch). La tarjeta lo explica en "Sin sensor" (`hrmWatchHint`).
- **HRM-16.3** Verificado el 2026-10-02 con Galaxy Watch 7 + Heart for Bluetooth: la app del reloj necesita los permisos "Sensores corporales" **y** "Dispositivos cercanos" (sin el segundo mide pero nunca anuncia); debe haber una Activity en curso con la pantalla del reloj encendida; "Connection → Restart Beacon" reinicia el anuncio; hay que permitirle correr en segundo plano; la identidad emparejada del reloj ("Galaxy Watch7 (XXXX)") **no** es el sensor: el emisor anuncia desde otra dirección y sin nombre.
- **HRM-16.4** Windows: el anuncio del reloj no trae nombre → aparece "Sensor desconocido" + dirección; la app filtra por UUID anunciado (HRM-02.5).
- **HRM-16.5** iOS/macOS: nunca filtrar en la app (overflow UUIDs).
- **HRM-16.6** Algunas apps emisoras usan direcciones privadas que rotan: el id guardado puede dejar de servir para reconectar si no hay emparejamiento → la tarjeta queda "no encontrado" y el usuario vuelve a buscar (pendiente de verificar con reloj real).
- **HRM-16.7** Dos conexiones simultáneas (remo + pulsómetro); nunca dos búsquedas simultáneas (HRM-02.12).
- **HRM-16.8** No se negocia MTU (algunos relojes rechazan el pedido).
- **HRM-16.9** Bluetooth apagado o sin permiso: la tarjeta queda oculta detrás de la vista completa de Bluetooth; los reintentos fallan solos y siguen su cadencia.
- **HRM-16.10** Plataforma sin Bluetooth LE: "Buscar" no hace nada.
- **HRM-16.11** Verificación de campo: nRF Connect en el teléfono, pestaña Scanner, filtro `180D`; si ahí no aparece, RowMate tampoco lo verá. Log `[HRM] Encontrado…` por sensor.
- **HRM-16.12** Fuera de alcance en el legacy: zonas de frecuencia cardíaca, batería del sensor (0x180F), más de un sensor a la vez, pulso del sensor en la grilla del remo.

**Archivos legacy:** `lib/core/bluetooth/scan_filter.dart:1-20`; `docs/superpowers/specs/2026-10-02-heart-rate-monitor-design.md:12-20, 288-312`; `README.md:44-66`; `CLAUDE.md:53`; `lib/features/device/device_screen.dart:367`.
**Tests legacy:** `scan_filter_test` (2 casos, ver F-HRM-02).
**Dependencias:** área BLE (adaptador, permisos, filtro compartido), área APP (plataformas).
**Mejora EXR:** guía visual de emparejamiento para smartwatches (pasos ilustrados) accesible desde la tarjeta, como agregado a la pista actual.

---

## 2. Matriz de tests legacy → funcionalidades

| Test (archivo › caso) | Funcionalidades |
|---|---|
| `heart_rate_parser_test` › 6 casos | F-HRM-01 |
| `reconnect_loop_test` › ReconnectLoop (6 casos) | F-HRM-09 (y F-HRM-07, F-HRM-08, F-HRM-11) |
| `reconnect_loop_test` › StaleWatchdog (1 caso) | F-HRM-05 |
| `simulated_heart_rate_service_test` › 3 casos | F-HRM-15, F-HRM-02, F-HRM-04, F-HRM-08, F-HRM-10, F-HRM-11 |
| `heart_rate_provider_test` › 3 casos | F-HRM-12, F-HRM-03, F-HRM-07, F-HRM-08, F-HRM-10 |
| `heart_rate_card_test` › 4 casos | F-HRM-13, F-HRM-02, F-HRM-03, F-HRM-04, F-HRM-10 |
| `workout_provider_hr_test` › 4 casos | F-HRM-14 |
| `scan_filter_test` › 2 casos (compartido con BLE) | F-HRM-02, F-HRM-16 |
| `simulator_overlay_test` › paso del botón de caída | F-HRM-15, F-HRM-08 |

Sin cobertura de test en el legacy (depende de la plataforma Bluetooth; QA deberá cubrirlo en la nueva app): secuencia real de conexión (HRM-03.4 a 03.13), memoria persistida (F-HRM-06), cadencias reales con sus constantes 10 s × 6 y 3 s (F-HRM-07/08), invalidación de intentos en vuelo del servicio real (F-HRM-11), watchdog dentro del servicio (HRM-05.1/05.6).

---

## 3. Consumidores aguas abajo (otras áreas; solo referencia)

| Área | Qué hace con el pulso fusionado | Fuente |
|---|---|---|
| WKT | Guarda un punto cada 5 s de entrenamiento activo con `heart_rate`, más un último punto al terminar | `lib/features/workout/workout_provider.dart:158-171, 258-270` |
| DB | Columna `data_points.heart_rate INTEGER NOT NULL DEFAULT 0`; las estadísticas por sesión la cargan | `lib/core/database/database_service.dart:133, 298, 315` |
| HIS | Promedio = media redondeada de los puntos con pulso > 0; máximo; "tiene pulso" = máximo > 0; chip `historyStatHeartRate` con formato `{prom}/{máx}` en historial y detalle; serie de pulso en el gráfico del detalle | `lib/core/models/workout_session.dart:78-92, 124-134`; `history_screen.dart:116-120`; `session_detail_screen.dart:167-171, 307, 339`; test `session_stats_test` (ignora ceros: 0,120,150,0,141 → 137/150; sin pulso → 0/0) |
| HUD | Métrica de pulso (`metricHeartRate`, unidad "bpm") solo si > 0; en la escena inmersiva, tarjeta "BPM" con corazón solo si > 0 | `workout_screen.dart:779-780`; `immersive_workout_screen.dart:721-729` |
| STR | El TCX incluye `HeartRateBpm` por punto solo si > 0 | `lib/core/strava/tcx_builder.dart:52-53` |
| BLE | La grilla del remo muestra solo el pulso FTMS (> 0), nunca el del sensor | `device_screen.dart:257-258` |

---

## 4. Experiencia EXR del subsistema (síntesis)

- El pulso es un **signo vital**, no un número más: corazón que late a la frecuencia real, color identitario `#EF476F`, número enorme legible desde el remo.
- **Nada brusco:** entradas, salidas y cambios de estado con transiciones suaves; sin saltos de layout; el valor final siempre es el exacto.
- **Autonomía:** al abrir la app el sensor aparece solo; si se cae, vuelve solo, con avisos discretos que no interrumpen la remada ni tapan la escena.
- **Claridad en los problemas:** "sin contacto", "sin señal", "no encontrado" e "incompatible" dichos en lenguaje humano, con la acción siguiente sugerida.
- **Respeto por el usuario:** sus acciones mandan al instante sobre lo automático.

---

## 5. Diferencias doc ↔ código (manda el código)

1. **Filtro de búsqueda en la app.** La spec (sección Escaneo) dice "no se filtra por UUID anunciado en el cliente"; el código filtra en Windows/Linux (`scan_filter.dart:12, 19-20`; `heart_rate_service.dart:136`). La propia spec lo admite en "Limitaciones conocidas".
2. **Errores al suscribirse.** La spec dice "excepciones al suscribirse → desconexión interna sin olvidar → reintentos de caída"; en el código, una falla antes de armar la vigilancia de caída (descubrimiento o notificaciones) deja `disconnected` y **no** arranca la cadencia de caída: en conexión manual se muestra el error; en un intento automático cuenta como fallo de su cadencia (`heart_rate_service.dart:263-272, 294-304`).
3. **Nombre recordado.** La spec dice "guardar `platformName`"; el código guarda el nombre del sistema o, si está vacío, el anunciado, y si ambos están vacíos conserva el nombre anterior (`heart_rate_service.dart:405-412`).
4. **Cantidad de reintentos al arrancar.** La spec dice "reintenta 6 veces"; el código hace **6 intentos en total** (1 inmediato + 5 reintentos a 10 s) (`reconnect_loop.dart:33-38, 63-67`; `heart_rate_service.dart:41-42`). El peor caso es 140 s, no "~2 minutos".
5. **Simulador: arranque automático.** La spec dice "`connect()` / `autoConnect()`: status = connected" y luego que `autoConnect()` no conecta; el código: no hace nada (`simulated_heart_rate_service.dart:91`).
6. **Simulador: olvidar.** La spec dice "`disconnect()` / `forget()`: cortan y vuelven a disconnected"; en el código, olvidar solo cancela la reconexión pendiente y no corta una conexión activa (`simulated_heart_rate_service.dart:101-105`).
7. **Fusión en el entrenamiento.** La spec dice "pulso del sensor nulo si no está connected" y "al pasar a disconnected → 0"; el código acepta pulsos en cualquier estado y vuelve a 0 ante **cualquier** estado distinto de `connected` (también `connecting` y `scanning`), y solo si el sensor había emitido; si no, ni siquiera avisa (`workout_provider.dart:226-237`).
8. **Formato del chip de historial.** La spec dice "`avg · max`"; el código muestra `{prom}/{máx}` (`history_screen.dart:119`, `session_detail_screen.dart:170`). (Área HIS.)
9. **Tests.** La spec menciona `heart_rate_service_test`; no existe: la lógica aislable vive en `reconnect_loop_test.dart` (motor de reintentos y watchdog).
10. **Ubicación de la tarjeta.** La spec la describe como `_HeartRateCard` dentro de `device_screen.dart`; es un componente propio en `heart_rate_card.dart`.
11. **Texto del estado sin sensor.** La spec dice "Pulsómetro · Sin sensor" y una pista sin "(Wear OS, Apple Watch)"; el código muestra el título en mayúsculas en el encabezado, "Sin sensor" en el cuerpo, y la pista del `.arb` incluye "(Wear OS, Apple Watch)".
12. **Botones deshabilitados por el remo.** La spec solo menciona "Buscar"; el código también deshabilita "Buscar otro" y "Conectar" mientras el remo está `scanning`/`connecting` (`heart_rate_card.dart:212, 218`).
13. **Estado conectando.** La tabla de la spec dice "Conectando a {nombre}… + spinner, sin acciones"; durante los reintentos automáticos el código muestra el estado "recordado · reconectando..." con botones (`heart_rate_card.dart:73-78`). La spec lo dice en otra sección.
14. **Bluetooth apagado.** La spec dice "la tarjeta queda detrás"; en el código la tarjeta directamente no se renderiza en esa vista (`device_screen.dart:367` vs `:474`). Efecto visible equivalente.
15. **Comportamientos no documentados en la spec:** pedir conectar mientras ya conecta se ignora (HRM-03.2); sin soporte de Bluetooth LE la búsqueda no hace nada (HRM-02.2); un error nativo de escaneo termina la búsqueda en silencio (HRM-02.8); un error al arrancar la búsqueda se muestra con el texto "Error al conectar" (HRM-02.11); el error de la tarjeta también se borra al llegar a `connected` y los errores de desconectar/olvidar también se capturan (HRM-12.4); en el simulador, conectar no pasa por `connecting` (HRM-15.5).

---

## 6. Preguntas abiertas (por defecto: se hace lo que hace el legacy)

1. **Nombre heredado (probable bug).** Si el sensor nuevo no tiene nombre, queda el nombre del sensor anterior como recordado y como nombre conectado (HRM-06.1). ¿Se mantiene por paridad o se corrige (mostrar el id / "Sensor desconocido")? Por defecto: paridad, marcado como bug conocido.
2. **Texto técnico en los errores.** `hrmConnectError` muestra el texto crudo de la excepción, y los errores de búsqueda dicen "Error al conectar". ¿Humanizamos los mensajes (mejora EXR) manteniendo las mismas situaciones de error?
3. **Desconectar también olvida.** Es intencional en el legacy (evita que la reconexión lo vuelva a enganchar). ¿Agregamos confirmación o "deshacer"? Por defecto: sin confirmación.
4. **Reanudar la reconexión tras "Buscar otro" sin elegir.** Hoy queda "no encontrado" y hay que tocar "Conectar". ¿Se mantiene?
5. **Lista que desaparece.** Si la búsqueda vence con sensores en la lista y el usuario no eligió, la lista desaparece. ¿Mantenerla visible hasta la próxima acción sería una mejora aceptable o un cambio de producto?
6. **Cancelar una conexión manual.** Hoy no se puede cancelar durante los hasta 15 s. ¿Agregamos "Cancelar"?
7. **Indicador de contacto.** El sensor informa "sin contacto"; hoy solo se ve `--`. ¿Lo comunicamos explícitamente (agregado)?
8. **Plataforma sin Bluetooth LE.** "Buscar" no hace nada y no avisa. ¿Mostramos un mensaje?
9. **Simulador.** ¿Mantenemos los textos sin localizar ("Pulsómetro simulado", "Simular caída del pulsómetro") y la ausencia de un modo "remo simulado + sensor real"?
10. **Preferencias existentes.** La app nueva no hereda las preferencias de la app Flutter instalada: el usuario deberá emparejar de nuevo una vez. ¿Es aceptable? (Restricción a confirmar con el Unity Architect.)
11. **Linux.** El legacy filtra en la app también en Linux, pero Linux no es plataforma objetivo de la app Unity. Confirmar que queda fuera.
12. **Rango de pulso.** Hoy no se valida (se acepta 0–65535). ¿Se mantiene sin validar?
13. **Fuera de alcance del legacy** (zonas de FC, batería 0x180F, varios sensores, pulso del sensor en la grilla del remo): ¿algún ítem entra como mejora EXR en una iteración futura?
