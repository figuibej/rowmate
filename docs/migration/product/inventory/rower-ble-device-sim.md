# Inventario de producto: remo BLE (FTMS), pestaña Dispositivo y simulador de desarrollo

| | |
|---|---|
| Área | **BLE** (IDs `F-BLE-NN`, comportamientos `BLE-NN.k`) |
| Fuente | `https://github.com/figuibej/rowmate-legacy` (solo lectura). Rutas `archivo:línea` relativas a esa carpeta |
| Autor / fecha | rowmate-po · 2026-10-27 |
| Estado | Contrato de paridad. Lo que no figure acá se pierde en la migración |

**Regla de lectura.** Cuando el código legacy y sus docs dicen cosas distintas, manda el código. Las diferencias están en la §7. Los textos visibles se citan por clave `.arb` (`lib/l10n/app_es.arb` / `app_en.arb`). Los textos fijos en el código (sin clave) se marcan como **[fijo]**.

**Alcance.** Incluye el flujo de datos del remo FTMS (decodificación, búsqueda, conexión, toma de control, keepalive, reconexión, watchdog), la pestaña Dispositivo (todas sus vistas y estados) y el modo simulador (activación, física, remo simulado, panel de control).

**Fuera de alcance (tienen su propio inventario).** El pulsómetro (HRM): acá solo se cubre cómo encaja su tarjeta en la pestaña y el bloqueo cruzado de búsquedas. También quedan fuera el motor de entrenamiento (WKT), la escena (SCN), la base de datos (DB) y Strava (STR); de esas áreas solo se nombran los puntos de contacto.

---

## 1. Mapa rápido

| ID | Funcionalidad | Lógica pura | Tests legacy |
|---|---|---|---|
| F-BLE-01 | Lectura en vivo del remo y formatos de display | Sí | — (hueco) |
| F-BLE-02 | Decodificación del paquete FTMS "Rower Data" | Sí | — (hueco) |
| F-BLE-03 | Filtro de candidatos por servicio anunciado según plataforma | Sí | `scan_filter_test.dart` |
| F-BLE-04 | Estado del Bluetooth, permisos y requisitos de plataforma | No | — |
| F-BLE-05 | Búsqueda de remos FTMS | No | `heart_rate_card_test.dart` (exclusión, landscape) |
| F-BLE-06 | Conexión al remo y toma de control FTMS (handshake + keepalive) | No | — |
| F-BLE-07 | Desconexión manual | No | `simulated_ble_service_test.dart` (versión simulada) |
| F-BLE-08 | Reconexión automática tras una caída | No | `simulated_ble_service_test.dart` (versión simulada) |
| F-BLE-09 | Detección de monitor apagado (watchdog de 5 s) | Sí (regla) | — |
| F-BLE-10 | Señal "remo conectado" para el resto de la app | No | `heart_rate_card_test.dart` (bloqueo cruzado) |
| F-BLE-11 | Pestaña Dispositivo: vista conectada (tablero en vivo) | No | `heart_rate_card_test.dart` |
| F-BLE-12 | Diagnóstico BLE oculto | No | — |
| F-BLE-13 | Modo simulador: activación y aislamiento (DB aparte, Strava bloqueado) | No | — |
| F-BLE-14 | Física del remo simulado | Sí | `rowing_simulator_test.dart` |
| F-BLE-15 | Remo simulado "conectado" | Sí (con reloj falso) | `simulated_ble_service_test.dart` |
| F-BLE-16 | Panel de control del simulador | No | `simulator_overlay_test.dart` |

---

## 2. Estados de referencia

**Estado de conexión del remo** (`lib/core/bluetooth/ble_service.dart:8`): `off` · `scanning` · `connecting` · `connected` · `disconnected`. El estado inicial es `disconnected` (`:43`).

**Estado del adaptador Bluetooth** (lo informa el sistema): desconocido · no disponible · sin permiso (`unauthorized`) · encendiendo · encendido · apagando · apagado. El estado inicial en la pestaña es "desconocido" (`lib/features/device/device_provider.dart:14`).

Transiciones del remo real:

| Desde | Evento | Hacia | Fuente |
|---|---|---|---|
| cualquiera | el adaptador pasa a apagado | `off` | `ble_service.dart:51-53` |
| `disconnected`/`off` | "Buscar dispositivos" con adaptador encendido (o que se enciende en ≤ 5 s) | `scanning` | `:65-86` |
| cualquiera | "Buscar" y el adaptador no se enciende en 5 s | `off` | `:79-82` |
| `scanning` | pasan 10 s o falla el inicio de la búsqueda | `disconnected` (solo si sigue en `scanning`) | `:114-123` |
| `scanning`/`disconnected` | el usuario toca un remo | `connecting` | `:138` |
| `connecting` | conexión + descubrimiento + toma de control OK | `connected` | `:155` |
| `connecting` | timeout de 15 s o error | `disconnected` + mensaje de error | `:156-159` |
| `connected` | caída no pedida | `disconnected`, y a los 3 s `connecting` (reintento) | `:148-153`, `:163-199` |
| `connected` | "Desconectar" o watchdog | `disconnected` (sin reintento) | `:304-313`; `device_provider.dart:75-79` |

---

## 3. Funcionalidades

### F-BLE-01: Lectura en vivo del remo y formatos de display

**Valor.** Todo lo que manda el monitor llega como una sola "foto" del momento, con formatos iguales en toda la app.

**Comportamientos observables**
- BLE-01.1. Una lectura tiene 8 campos: ritmo de paladas (spm, decimal), paladas totales (entero), distancia (m, entero), split cada 500 m (s, entero), potencia (W, entero), calorías totales (kcal, entero), pulso (bpm, entero, `0` = sin dato) y tiempo transcurrido (s, entero). `lib/core/models/rowing_data.dart:2-10`
- BLE-01.2. Una lectura vacía tiene todos los campos en 0. `rowing_data.dart:12-21`
- BLE-01.3. El split se muestra como `MM:SS`, con minutos y segundos a 2 dígitos y cero a la izquierda: 133 s → `02:13`, 120 → `02:00`, 59 → `00:59`. A partir de 6000 s los minutos tienen 3 dígitos (`100:00`). Si el split es ≤ 0 se muestra `--:--`. `rowing_data.dart:24-29`
  - Nota: el formato del legacy es `MM:SS` con cero a la izquierda (`02:13`), no `m:ss`.
- BLE-01.4. El tiempo se muestra como `H:MM:SS` cuando hay al menos 1 hora, con la hora sin cero (3661 s → `1:01:01`). Si no, se muestra como `MM:SS` (59 → `00:59`, 0 → `00:00`). `rowing_data.dart:32-40`
- BLE-01.5. Una lectura se puede copiar cambiando solo algunos campos. Así el pulsómetro reemplaza el pulso sin tocar el resto (ver HRM/WKT). `rowing_data.dart:42-62`
- BLE-01.6. Formatos de la pestaña Dispositivo (`lib/features/device/device_screen.dart:253-258`):
  - SPM con 1 decimal y **punto** decimal en los dos idiomas (`24.0`, `23.5`).
  - Vatios como entero sin unidad (`149`).
  - Distancia como entero con sufijo `m`, sin espacio ni separador de miles (`652m`, `10000m`).
  - Pulso como entero sin unidad.

**Casos borde.** Una potencia negativa (el protocolo la permite) se muestra con signo. Un split 0 del monitor se muestra como `--:--`.

**Datos persistidos.** Ninguno.

**Tests legacy.** No hay tests directos de los formatos (hueco). Los tests del simulador solo validan los valores de los campos.

**Dependencias.** La usan F-BLE-02, F-BLE-11, F-BLE-12 y F-BLE-14, y también WKT, HUD, SCN, DB y STR (todo el resto de la app lee esta lectura).

**Mejora EXR.** Números grandes con tipografía deportiva de ancho fijo, para que no "bailen" cuando cambian los dígitos. Cada valor nuevo entra con una transición visual breve y suave, pero el valor mostrado siempre es el real. Cuando no hay dato, `--:--` se ve claro y tranquilo, sin parecer un error.

---

### F-BLE-02: Decodificación del paquete FTMS "Rower Data"

**Valor.** Funciona con cualquier remo que siga el estándar FTMS de Bluetooth. Fue probado con el AMS-670B (módulo Kinomap-XG/H201) (`ble_service.dart:10`; `README.md:17-27`).

**Comportamientos observables**
- BLE-02.1. Solo se decodifica la característica **Rower Data `0x2AD1`** (`00002ad1-0000-1000-8000-00805f9b34fb`) del servicio **FTMS `0x1826`**. `lib/core/bluetooth/ftms_parser.dart:22-23` (los docs dicen 0x2AD2, ver §7)
- BLE-02.2. Un paquete vacío se ignora (`ble_service.dart:292`). Un paquete de menos de 2 bytes se descarta y no genera lectura (`ftms_parser.dart:27`).
- BLE-02.3. Los bytes 0-1 son los *flags* (uint16 little-endian). Después vienen los campos en este orden, todos little-endian, cada uno presente según su bit (`ftms_parser.dart:30-129`):

| Bit | Campo | Bytes | Uso |
|---|---|---|---|
| 0 = **0** ("More Data" apagado) | Stroke Rate uint8 (× 0,5 spm) + Stroke Count uint16 | 1 + 2 | ritmo y paladas. Con el bit 0 en 1 los dos quedan en 0 |
| 1 | Average Stroke Rate | 1 | se saltea |
| 2 | Total Distance uint24 (m) | 3 | distancia |
| 3 | Instantaneous Pace uint16 (s/500 m) | 2 | split |
| 4 | Average Pace uint16 | 2 | se usa como split **solo si** el split instantáneo es 0 (porque falta o porque vale 0) |
| 5 | Instantaneous Power sint16 (W) | 2 | potencia (puede ser negativa) |
| 6 | Average Power sint16 | 2 | se usa **solo si** la potencia instantánea es 0 |
| 7 | Resistance Level | 2 | se saltea |
| 8 | Expended Energy: total uint16 (kcal) + por hora uint16 + por minuto uint8 | 2 + 2 + 1 | solo el total |
| 9 | Heart Rate uint8 (bpm) | 1 | pulso |
| 10 | Metabolic Equivalent | 1 | se saltea |
| 11 | Elapsed Time uint16 (s) | 2 | tiempo |
| 12 | Remaining Time | 2 | se ignora (el tiempo restante lo calcula la rutina) |

- BLE-02.4. El ritmo tiene resolución de 0,5 spm (valor crudo ÷ 2). `ftms_parser.dart:46`
- BLE-02.5. Paquetes truncados: cada campo se lee solo si quedan bytes suficientes; si no, queda en 0 y el paquete se procesa igual, sin fallar. Los campos que se saltean (bits 1, 7 y 10) avanzan sin esa verificación. `ftms_parser.dart:45-126`
- BLE-02.6. Un campo ausente vale **0** en esa lectura; no conserva el valor anterior. Por ejemplo, un paquete sin distancia deja la distancia en 0 (WKT sanea esos saltos en los parciales).
- BLE-02.7. Los valores FTMS de "no disponible" (por ejemplo 0xFFFF en energía) no tienen tratamiento especial: se toman crudos.

**Ejemplos verificables** (derivados del código; el legacy no los tiene como tests):

| Bytes (hex) | Lectura resultante |
|---|---|
| `2C 00 30 2A 01 E2 01 00 85 00 96 00` | 24.0 spm, 298 paladas, 482 m, split 133 s (`02:13`), 150 W; el resto en 0 |
| `05 00 10 27 00` | "More Data" = 1, así que 0 spm y 0 paladas; 10000 m |
| `11 00 78 00` | sin split instantáneo, el promedio pasa a ser el split: 120 s (`02:00`) |
| `19 00 00 00 7D 00` | split instantáneo 0, así que se usa el promedio: 125 s (`02:05`) |
| `19 00 85 00 7D 00` | split instantáneo 133 s; el promedio se ignora |
| `01 0B 2D 00 00 00 00 8C 2C 01` | 45 kcal, 140 bpm, 300 s (`05:00`) |
| `00 00 30` | 24.0 spm, 0 paladas (faltan bytes) |
| `2C` (1 byte) | descartado, sin lectura |

**Casos borde.** Un equipo FTMS que no es remo (bici, cinta) no manda Rower Data, así que no hay lecturas (ver F-BLE-06). Una potencia negativa pasa tal cual.

**Datos persistidos.** Ninguno.

**Tests legacy.** No hay. Es un hueco explícito: el spec del simulador lo deja fuera de alcance (`docs/superpowers/specs/2026-10-01-dev-simulator-design.md:79`). El QA tiene que convertir la tabla de ejemplos en casos de aceptación.

**Dependencias.** F-BLE-01. La usan F-BLE-06 y F-BLE-12.

**Mejora EXR.** No se ve, pero se siente: los números son estables y confiables, y un paquete raro nunca congela ni rompe la pantalla.

---

### F-BLE-03: Filtro de candidatos por servicio anunciado

**Valor.** La lista de búsqueda muestra solo equipos relevantes en todas las plataformas, sin esconder equipos válidos.

**Comportamientos observables**
- BLE-03.1. En **Windows y Linux** el sistema no aplica el filtro por servicio y entrega todos los equipos cercanos. Por eso la app descarta los que no anuncian el servicio buscado: FTMS `0x1826` para el remo, `0x180D` para el pulsómetro. `lib/core/bluetooth/scan_filter.dart:12,19-20`; `ble_service.dart:106`
- BLE-03.2. En **Android, iOS y macOS** la app confía en el filtro del sistema y **no** filtra por su cuenta. En iOS/macOS un servicio puede viajar en los "overflow UUIDs" y no aparecer en la lista anunciada; filtrar ahí escondería equipos válidos. `scan_filter.dart:4-11`
- BLE-03.3. El UUID corto de 16 bits equivale al de 128 bits (`180d` ≡ `0000180d-0000-1000-8000-00805f9b34fb`, y lo mismo con `1826`).
- BLE-03.4. Un equipo que anuncia varios servicios pasa el filtro si uno de ellos es el buscado. `scan_filter.dart:14-15`
- BLE-03.5. La misma regla vale para el remo y para el pulsómetro.

**Casos borde.** En Windows, un equipo que anuncia FTMS pero no su nombre aparece como "Unknown device" (ver F-BLE-05).

**Datos persistidos.** Ninguno.

**Tests legacy** (`test/scan_filter_test.dart`)
- `:24` "con filtro en el cliente solo pasan los que anuncian el servicio". Con filtro en la app se rechaza la lista vacía y la que trae solo `febe`; se acepta `180d` en forma corta ("el UUID de 16 bits equivale al de 128"); se acepta `febe` + Heart Rate.
- `:32` "sin filtro en el cliente (el sistema ya filtró) pasa todo". Sin filtro en la app se aceptan la lista vacía y la que trae solo `febe`.

**Dependencias.** La usan F-BLE-05 y HRM (búsqueda de pulsómetros).

**Mejora EXR.** La lista queda limpia y sin ruido: solo aparecen remos (o pulsómetros), nunca auriculares o televisores del vecino.

---

### F-BLE-04: Estado del Bluetooth, permisos y requisitos de plataforma

**Valor.** El usuario entiende enseguida por qué no puede conectar (Bluetooth apagado o sin permiso) y cómo resolverlo.

**Comportamientos observables**
- BLE-04.1. La app observa el estado del adaptador desde que arranca. `ble_service.dart:46-55`
- BLE-04.2. Si el adaptador se apaga, el remo pasa a `off`. `ble_service.dart:51-53`
- BLE-04.3. Si en la pestaña Dispositivo, sin remo conectado, el adaptador está **apagado**, se muestra una vista de pantalla completa centrada: ícono de Bluetooth tachado de 64 px en rojo, título `deviceBtOff` (18 pt, negrita), ayuda `deviceBtOffHint` (14 pt, gris 54 %, centrada) y botón `deviceBtEnable` con ícono Bluetooth. `device_screen.dart:346,367-394`
- BLE-04.4. Si el adaptador está **sin permiso**, la vista es la misma pero con `deviceBtDenied` y `deviceBtDeniedHint`. `device_screen.dart:347,376,381`
- BLE-04.5. Esa vista reemplaza al botón de búsqueda, a la lista y a la **tarjeta del pulsómetro**, que quedan ocultos. El banner de error, si lo hay, se sigue viendo arriba. `device_screen.dart:355-395`
- BLE-04.6. En cualquier otro estado del adaptador (desconocido, no disponible, encendiendo, apagando, encendido) se ve la vista normal de búsqueda. Al arrancar, antes del primer aviso del sistema, el estado es "desconocido", así que se ve la vista normal. `device_provider.dart:14`
- BLE-04.7. "Activar Bluetooth" le pide al sistema que encienda el Bluetooth. Solo tiene efecto en Android; en iOS, macOS y Windows el sistema no lo permite y el botón no hace nada visible ni muestra error. `device_screen.dart:387`
- BLE-04.8. Si el adaptador no está listo al tocar "Buscar dispositivos", la app espera **hasta 5 s** a que se encienda. En iOS el primer uso dispara el diálogo de permiso del sistema. Si no se enciende, el remo pasa a `off`, la búsqueda no arranca y no aparece ningún mensaje. `ble_service.dart:69-84`
- BLE-04.9. Si la plataforma no soporta BLE, "Buscar" no hace nada (sin estado ni mensaje). `ble_service.dart:66-67`
- BLE-04.10. No hay una pantalla propia antes del permiso: el diálogo lo muestra el sistema.
- BLE-04.11. Requisitos de plataforma que tiene el legacy:
  - **Android:** la app solo se instala en equipos con BLE (`uses-feature bluetooth_le required=true`). Pide BLUETOOTH_SCAN declarado "nunca para ubicación" y BLUETOOTH_CONNECT (API 31+), y BLUETOOTH, BLUETOOTH_ADMIN y ACCESS_FINE_LOCATION hasta API 30. Mínimo: el del kit de Flutter (`flutter.minSdkVersion` = API 24, Android 7.0, con el Flutter 3.47.6 instalado; los docs dicen 21). En la app nueva: Android 8.0 (API 26) y ARM64 (Q-90). `android/app/src/main/AndroidManifest.xml:4-19`
  - **iOS:** el texto del permiso es "Se necesita Bluetooth para conectar con el monitor del rower." (solo en español, sin versión en inglés). Tiene modo de segundo plano *bluetooth-central*, así que la conexión sigue con la app en segundo plano. Requiere iOS 15. `ios/Runner/Info.plist:44-51`
  - **macOS:** el mismo texto de permiso y el permiso de Bluetooth en el sandbox. `macos/Runner/Info.plist:31-32`; `macos/Runner/*.entitlements`
  - **Windows:** sin permisos del sistema.

**Casos borde**
- Bluetooth apagado con el remo conectado: el remo pasa a `off` y la conexión se cae, así que la pestaña muestra la vista "Bluetooth apagado". El reintento automático (F-BLE-08) falla porque el Bluetooth está apagado, y al volver a encenderlo no hay reconexión: hay que buscar de nuevo.
- El adaptador puede quedar "encendido" mientras el estado del remo sigue en `off` (después de un timeout de 5 s). El botón "Buscar" sigue habilitado porque solo se deshabilita con `scanning` o `connecting`.

**UI.** Se describe arriba. El fondo es el oscuro general `#0D1B2A` (`lib/shared/theme.dart:13`).

**Datos persistidos.** Ninguno.

**Tests legacy.** No hay.

**Dependencias.** La usan F-BLE-05 y F-BLE-11. Oculta la tarjeta HRM.

**Mejora EXR.** Una pantalla de estado ilustrada en lugar de un error seco: el ícono de Bluetooth "respira" suavemente y hay una guía visual corta para cada plataforma. El botón lleva directo a los ajustes del sistema. Cuando el Bluetooth vuelve, la vista se transforma con una animación en la búsqueda normal, sin saltos.

---

### F-BLE-05: Búsqueda de remos FTMS

**Valor.** El usuario encuentra su remo con un solo toque.

**Comportamientos observables**
- BLE-05.1. "Buscar dispositivos" (`deviceSearch`) borra el error y la lista anterior, y arranca una búsqueda de **10 s** filtrada por el servicio FTMS `0x1826`. `device_provider.dart:104-114`; `ble_service.dart:65,93-96`
- BLE-05.2. Mientras busca, el remo está en `scanning`. El botón queda deshabilitado, con un spinner blanco de 16 px y el texto `deviceSearching`. Debajo aparece `deviceSearchHint` (12 pt, gris 54 %, centrado). `device_screen.dart:396-417`
- BLE-05.3. La lista se actualiza en vivo a medida que aparecen equipos. No hay duplicados: hay uno por dirección, y si un equipo reaparece se actualiza en su lugar. El orden es el del primer descubrimiento. `ble_service.dart:87,104-109`
- BLE-05.4. Solo cuentan los resultados de esta búsqueda; no se mezclan los guardados de una búsqueda anterior, por ejemplo la del pulsómetro. `ble_service.dart:97-102`
- BLE-05.5. A los 10 s el remo vuelve a `disconnected`, salvo que el usuario ya esté conectando o conectado; en ese caso el estado no se pisa. La lista queda visible hasta la próxima búsqueda, incluso después de conectar y desconectar. `ble_service.dart:113-123`
- BLE-05.6. **No hay botón para detener la búsqueda**: dura siempre 10 s. La acción existe, pero ninguna pantalla la usa. `device_provider.dart:116`
- BLE-05.7. Conectar un remo no detiene la búsqueda en curso.
- BLE-05.8. **Exclusión con el pulsómetro.** "Buscar dispositivos" se deshabilita mientras el pulsómetro busca. A la vez, "Buscar" y "Reintentar" del pulsómetro se deshabilitan mientras el remo busca **o conecta**. La regla es asimétrica: que el pulsómetro esté *conectando* no bloquea la búsqueda del remo. `device_screen.dart:348,397`; `heart_rate_card.dart:22-24,108,212,218`
- BLE-05.9. Si la búsqueda no puede arrancar, aparece un banner rojo arriba (fondo rojo 15 %, radio 12, texto 13 pt rojo claro) con "Error al escanear: <detalle técnico>" **[fijo, en español en los dos idiomas]**. El remo vuelve a `disconnected` para que "Buscar" no quede deshabilitado. `device_provider.dart:110-112`; `device_screen.dart:355-365`; `ble_service.dart:118-121`
- BLE-05.10. Puede empezar una búsqueda nueva mientras la anterior sigue "viva" (caso típico: la conexión falla durante una búsqueda y el usuario vuelve a buscar). La vieja no pisa el estado ni la lista de la nueva. `ble_service.dart:88-90,114-123`
- BLE-05.11. Ítem de la lista (`device_screen.dart:444-471`):
  - Tarjeta con ícono de remo cian `#00B4D8`.
  - Título: el nombre del equipo, o "Unknown device" **[fijo, en inglés en los dos idiomas]** si no tiene nombre.
  - Subtítulo: la dirección o ID (11 pt, gris 38 %).
  - Flecha a la derecha. Tocar el ítem conecta (F-BLE-06).

**Estados de la vista de búsqueda**

| Estado | Qué se ve |
|---|---|
| Inicial (sin búsqueda) o terminó sin resultados | ícono Bluetooth tachado de 48 px (gris 24 %), `deviceNoResults` (gris 54 %) y `deviceNoResultsHint` (12 pt, gris 38 %, centrado), con scroll si no entra. **Aparece apenas se abre la app, antes de cualquier búsqueda** `device_screen.dart:419-442` |
| Buscando sin resultados | solo el botón deshabilitado y el hint (sin estado vacío) |
| Buscando o terminada con resultados | la lista |
| Conectando | spinners en todos los ítems (F-BLE-06) |
| Error | banner rojo arriba, sumado a cualquiera de los anteriores |

Al pie siempre está la tarjeta del pulsómetro, con scroll propio para que en horizontal no desborde (`device_screen.dart:472-474`).

**Casos borde**
- La lista muestra **cualquier** equipo FTMS: bicis, cintas y elípticos también anuncian `0x1826`. No se filtra por tipo de máquina.
- En Windows los equipos sin nombre en el anuncio aparecen como "Unknown device".

**Datos persistidos.** Ninguno. El legacy **no recuerda el remo**: hay que buscarlo en cada uso (a diferencia del pulsómetro).

**Tests legacy**
- `test/heart_rate_card_test.dart:121` "los escaneos del remo y del pulsómetro se excluyen". Con el remo desconectado, "Buscar dispositivos" está habilitado; al buscar un pulsómetro queda deshabilitado; al detener esa búsqueda vuelve a habilitarse.
- `test/heart_rate_card_test.dart:161` "mientras el remo escanea no se puede buscar pulsómetro". Con el remo en `scanning` o `connecting`, el botón de búsqueda del pulsómetro está deshabilitado; al volver a `disconnected`, habilitado.
- `test/heart_rate_card_test.dart:142` "en landscape la vista de escaneo no desborda". En 640×360, la vista de búsqueda con la tarjeta no da error de desborde, y la lista de sensores queda dentro del scroll de la tarjeta.
- `test/scan_filter_test.dart` cubre el filtro (F-BLE-03).

**Dependencias.** F-BLE-03, F-BLE-04 y F-BLE-06. HRM (bloqueo cruzado).

**Mejora EXR.** La búsqueda se ve como un "sonar" animado y sereno. Cada remo encontrado entra como tarjeta con una animación suave, con su ícono de máquina y la intensidad de señal. Al terminar, un cierre claro ("Búsqueda terminada") invita a reintentar. Nada parpadea ni salta.

---

### F-BLE-06: Conexión al remo y toma de control FTMS

**Valor.** Un toque y el monitor queda conectado, controlado y mandando datos, sin pasos manuales en el monitor.

**Comportamientos observables**
- BLE-06.1. Al tocar un remo de la lista, el remo pasa a `connecting`. Todos los ítems muestran un spinner de 20 px en lugar de la flecha y no se pueden tocar; "Buscar" queda deshabilitado. `device_screen.dart:397,460-466`
- BLE-06.2. Hay un intento de conexión directa con **tiempo máximo de 15 s**. `ble_service.dart:144`
- BLE-06.3. Ya conectado, la app descubre los servicios y activa las notificaciones de **todas** las características que las soportan (solo para el registro de diagnóstico). Identifica Rower Data `0x2AD1`, que decodifica en cada notificación, y Control Point `0x2AD9`. `ble_service.dart:204-247`; `ftms_parser.dart:22-24`
- BLE-06.4. Si el equipo no tiene Rower Data, queda "conectado" igual, sin datos, con un aviso en el registro. `ble_service.dart:251-253`
- BLE-06.5. **Toma de control.** Si hay Control Point, la app escribe `0x00` (Request Control), espera **300 ms** y escribe `0x07` (Start or Resume). Si algo falla, se ignora. Sin Control Point, se omite. `ble_service.dart:259-276`
- BLE-06.6. **Keepalive.** Mientras dure la conexión, la app escribe `0x07` en el Control Point **cada 2 s** para que el monitor no salga del modo Bluetooth. Los errores solo se registran. `ble_service.dart:278-288`
- BLE-06.7. Recién después de todo esto el remo pasa a `connected` y la pestaña cambia a la vista conectada (F-BLE-11). `ble_service.dart:155`
- BLE-06.8. Cada notificación de Rower Data con contenido se guarda como "último paquete crudo" (para F-BLE-12). Si se decodifica, se publica como lectura nueva para toda la app (Dispositivo, entrenamiento, escena). `ble_service.dart:291-302`
- BLE-06.9. Al pasar a `connected`, la pestaña pone su lectura en ceros (split `--:--`) hasta el primer paquete. `device_provider.dart:40-42,64-67`
- BLE-06.10. Si la conexión falla (timeout o error), el remo vuelve a `disconnected` y aparece el banner rojo "Error al conectar: <detalle técnico>" **[fijo, en español]**. El error se borra al volver a buscar o a conectar. `ble_service.dart:156-159`; `device_provider.dart:118-127`
- BLE-06.11. Registro de diagnóstico de campo: se anotan la lista de servicios y características con sus propiedades, cada paquete crudo en hex y cada lectura decodificada (`[BLE] Parsed → SPM:… W:… dist:…m elapsed:…s`). `ble_service.dart:207-249,293-300`

**Casos borde**
- Un equipo FTMS que no es remo queda "conectado" con métricas en 0 para siempre: el watchdog (F-BLE-09) no dispara porque nunca llega un primer paquete.
- Un equipo sin nombre: la cabecera conectada muestra el nombre vacío. La alternativa "Rower" solo aparece si no hay ningún equipo asociado.

**Datos persistidos.** Ninguno.

**Tests legacy.** No hay; la conexión real no tiene tests.

**Dependencias.** F-BLE-02, F-BLE-05, F-BLE-08, F-BLE-09 y F-BLE-11.

**Mejora EXR.** La conexión es una secuencia con pasos visibles y cortos: "Conectando…", "Tomando control del monitor…", "Listo". Al quedar listo hay un sonido y una vibración sutiles, y la lista se transforma con una animación en el tablero, como si el monitor "despertara".

---

### F-BLE-07: Desconexión manual

**Valor.** El usuario corta la conexión cuando quiere, y la app respeta esa decisión y no se reconecta sola.

**Comportamientos observables**
- BLE-07.1. El botón "Desconectar" (`deviceDisconnect`) está en la barra superior y aparece **solo** con el remo conectado. Tiene ícono de Bluetooth tachado de 18 px, en rojo claro. `device_screen.dart:21-30`
- BLE-07.2. Al tocarlo, la app corta la conexión, detiene el keepalive y las notificaciones y **olvida el equipo para la reconexión**, así que no se reconecta. El remo pasa a `disconnected` y se ve la vista de búsqueda con la lista de la última búsqueda. `ble_service.dart:304-313`
- BLE-07.3. No pide confirmación.

**Datos persistidos.** Ninguno.

**Tests legacy.** `test/simulated_ble_service_test.dart:64` "disconnect no reconecta solo; startScan reconecta" (solo la versión simulada): después de desconectar y esperar 10 s sigue `disconnected` y sin nombre; "Buscar" vuelve a conectar.

**Dependencias.** F-BLE-06 y F-BLE-11.

**Mejora EXR.** El tablero se "apaga" con una animación breve y elegante, con vibración sutil. Si hay un entrenamiento en curso, ver la pregunta abierta Q6.

---

### F-BLE-08: Reconexión automática tras una caída

**Valor.** Un corte breve de Bluetooth no debería obligar al usuario a bajarse del remo para reconectar.

**Comportamientos observables**
- BLE-08.1. Si la conexión se cae sola (no por el usuario ni por el watchdog), el remo pasa a `disconnected` (vista de búsqueda), se limpia todo y se programa la reconexión al último equipo. `ble_service.dart:148-153`
- BLE-08.2. A los **3 s** el remo pasa a `connecting` y hay un intento con **tiempo máximo de 10 s**, que incluye descubrimiento, toma de control y keepalive. Si sale bien, pasa a `connected`. `ble_service.dart:163-192`
- BLE-08.3. **Lo que hace hoy el código.** Si ese primer intento falla, no hay más intentos: el ciclo se cancela, el remo queda `disconnected` y el usuario tiene que buscar de nuevo. El comentario del código y los docs dicen "cada 3 s hasta lograrlo", pero la limpieza que sigue al fallo cancela el ciclo, y si el intento dura más de 3 s, el ciclo mismo se cancela al ver `connecting`. `ble_service.dart:168-172,193-197,315-319` (ver §7 y la pregunta Q1)
- BLE-08.4. Si el usuario toca "Conectar" en un ítem durante los 3 s de espera, se cancela la reconexión programada. "Buscar" **no** la cancela: si el intento arranca durante la búsqueda, el estado pasa a `connecting` y la búsqueda no lo pisa al terminar. `ble_service.dart:136-137,169`
- BLE-08.5. Mientras reconecta, la pestaña muestra la vista de búsqueda con "Buscar" deshabilitado y spinners en la lista, si la hay. Si la lista está vacía, se ve "No se encontraron dispositivos". **No hay un texto de "reconectando" para el remo**, a diferencia del pulsómetro.
- BLE-08.6. Durante un entrenamiento la caída **no se avisa** en la pantalla de entrenamiento: las métricas quedan congeladas en la última lectura y el reloj de la sesión sigue corriendo (ver WKT). `lib/features/workout/workout_provider.dart:82-86,151-155,220-224`

**Datos persistidos.** Ninguno. El "último equipo" se recuerda solo en memoria.

**Tests legacy.** `test/simulated_ble_service_test.dart:39` "simulateDisconnect emite disconnected y reconecta a los 3 s" (solo la versión simulada). A los 2 s los estados son [connected, disconnected] y no hubo datos; a los 3 s son [connected, disconnected, connected]. La versión real no tiene tests.

**Dependencias.** F-BLE-06, F-BLE-11 y WKT.

**Mejora EXR.** Aparece un aviso discreto, "Reconectando con <remo>…", con progreso, tanto en Dispositivo como encima de la escena de entrenamiento. Al volver, una confirmación sutil. En la escena, el bote se desliza y frena con naturalidad en lugar de congelarse.

---

### F-BLE-09: Detección de monitor apagado (watchdog)

**Valor.** Si el usuario apaga el monitor, la app no queda "conectada" a un equipo mudo.

**Comportamientos observables**
- BLE-09.1. Al pasar a `connected` arranca un chequeo **cada 1 s**. `device_provider.dart:38-42,69-81`
- BLE-09.2. El chequeo solo actúa después del **primer** paquete de Rower Data de esa conexión; si nunca llega uno, no dispara. `device_provider.dart:64-67,72-73`
- BLE-09.3. Si pasan **≥ 5 s** (contados en segundos enteros, en la práctica entre 5 y 6 s) desde el último paquete, la app hace una **desconexión manual** (como F-BLE-07, sin reconexión) y vuelve a la vista de búsqueda. `device_provider.dart:8,74-79`
- BLE-09.4. El chequeo se detiene ante cualquier estado distinto de `connected`. `device_provider.dart:43-45`
- BLE-09.5. Motivo según el código: cuando el monitor se apaga, deja de mandar datos aunque el enlace no se caiga enseguida. `device_provider.dart:7,24`
- BLE-09.6. No aparece ningún mensaje que explique la desconexión.
- BLE-09.7. Con el simulador nunca dispara, porque el remo simulado no manda paquetes crudos.

**Casos borde.** Si un monitor deja de notificar cuando el remero para (pausa), la app se desconecta a los 5 s. El legacy no distingue ese caso (pregunta Q3).

**Datos persistidos.** Ninguno.

**Tests legacy.** No hay.

**Dependencias.** F-BLE-06 y F-BLE-07.

**Mejora EXR.** Mensaje explícito y amable: "El monitor dejó de enviar datos", con un botón para reintentar. La escena se detiene suavemente en lugar de cortarse.

---

### F-BLE-10: Señal "remo conectado" para el resto de la app

**Valor.** Con el remo desconectado no se puede arrancar un entrenamiento vacío, y la app guía al usuario.

**Comportamientos observables**
- BLE-10.1. En la pestaña Entrenamiento, sin remo conectado, se ve el aviso naranja `workoutRowerNotConnected` (fondo naranja 15 %, ícono de advertencia de 18 px). "Entrenamiento libre" está deshabilitado y las rutinas se ven al **50 %** de opacidad y no responden al toque. `lib/features/workout/workout_screen.dart:46-50,58-77,84-93,108,118-120`
- BLE-10.2. Las lecturas del remo alimentan el motor de entrenamiento. El motor **no** mira el estado de conexión (F-BLE-08.6). `workout_provider.dart:82-86`
- BLE-10.3. Que el remo esté ocupado (`scanning`/`connecting`) bloquea la búsqueda del pulsómetro (BLE-05.8).
- BLE-10.4. Dispositivo es la **primera pestaña** de la barra inferior (índice 0, la que se ve al abrir la app). Su ícono es Bluetooth, Bluetooth conectado cuando está seleccionada, y su etiqueta es `navDevice`. La pestaña conserva su estado al cambiar de pestaña. `lib/main.dart:124,129,143,151-155`

**Datos persistidos.** Ninguno.

**Tests legacy.** `heart_rate_card_test.dart:161` (bloqueo cruzado). La habilitación del entrenamiento no tiene tests.

**Dependencias.** WKT, HRM y APP (navegación).

**Mejora EXR.** Un indicador de conexión del remo siempre visible, como un "LED" de estado. En Entrenamiento, además del aviso, un acceso directo para conectar el remo.

---

### F-BLE-11: Pestaña Dispositivo, vista conectada (tablero en vivo)

**Valor.** El usuario confirma que el remo está conectado y ve las métricas en vivo antes de entrenar.

**Comportamientos observables**
- BLE-11.1. Barra superior con el título `deviceTitle` y la acción "Desconectar" (F-BLE-07). `device_screen.dart:18-31`
- BLE-11.2. El cuerpo muestra la vista conectada si el remo está `connected` y la vista de búsqueda en cualquier otro estado. `device_screen.dart:33-38`
- BLE-11.3. Cabecera centrada: ícono de remo de 32 px en el color primario y el nombre del equipo (alternativa "Rower" **[fijo]**). Al lado, una insignia verde (fondo verde 20 %, radio 12) con ícono de Bluetooth conectado de 12 px y `deviceConnected` en 12 pt verde claro. `device_screen.dart:93-118`
- BLE-11.4. Grilla de métricas en vivo, en este orden (`device_screen.dart:252-259`; colores en `theme.dart:32-40`):

| # | Etiqueta | Valor | Color | Ícono |
|---|---|---|---|---|
| 1 | `metricSplit` (SPLIT 500m) | `MM:SS` / `--:--` | `#FF9F1C` | reloj |
| 2 | `metricSpm` (SPM) | 1 decimal | `#00B4D8` | velocímetro |
| 3 | `metricWatts` (VATIOS / WATTS) | entero | `#FFD60A` | rayo |
| 4 | `metricDistance` (DISTANCIA) | `<n>m` | `#4CC9F0` | regla |
| 5 | `metricHeartRate` (PULSO / HR) | entero | `#EF476F` | corazón |

  La métrica 5 aparece **solo si el pulso que manda el propio monitor por FTMS es > 0**. El pulso del pulsómetro **no** aparece en esta grilla: se ve en la tarjeta del pulsómetro. En la captura `docs/image-01.png` (simulador) no hay tarjeta de pulso en la grilla y la tarjeta del pulsómetro muestra 143 bpm.
- BLE-11.5. Con la **grilla** de más de 600 dp de ancho tiene **3 columnas** y proporción 1,6; si no, **2 columnas** y proporción 1,3. La separación es de 12 dp. La grilla mide el ancho de la ventana menos 40 dp (20 de margen a cada lado), así que hay 3 columnas con la ventana de más de 640 dp (en 640 × 360, 2). `device_screen.dart:87-89`, `:263-272` (corregido en el gate de la It. 2, [spec de la It. 2](../../iterations/02-dispositivo-simulador/01-po-spec.md) AC-BLE-11-05)
- BLE-11.6. Tarjeta de métrica: fondo `#1A2E45`, radio 16, borde superior de 3 px en el color de la métrica al 60 %. La etiqueta lleva un ícono de 14 px, en el color al 70 %, 12 pt semi-negrita con espaciado 0,8. El valor va en 52 pt extra-negrita en el color de la métrica y se achica si no entra. `device_screen.dart:288-335`
- BLE-11.7. Debajo de la grilla: 12 px y la tarjeta del pulsómetro (ver HRM); después, 32 px y el acceso oculto al diagnóstico (F-BLE-12). Todo el contenido tiene scroll. `device_screen.dart:119-136`
- BLE-11.8. Calorías, tiempo y paladas totales **no** se muestran en esta pestaña, aunque se decodifican.
- BLE-11.9. Los valores se actualizan con cada paquete: el ritmo lo marca el monitor (en el simulador es exactamente 1 por segundo).
- BLE-11.10. El tema es oscuro, con fondo `#0D1B2A` y color semilla `#0077B6`. `theme.dart:5-29`

**Casos borde.** Después de una reconexión los valores arrancan en cero hasta el primer paquete (BLE-06.9).

**Datos persistidos.** Ninguno.

**Tests legacy.** `heart_rate_card_test.dart:83` "sin sensor → buscar → conectar → bpm → desconectar". Con el remo simulado conectado se ve la vista conectada, con la tarjeta "PULSÓMETRO" en "Sin sensor" y la ayuda de "Heart for Bluetooth". La tarjeta queda debajo de la grilla y hay que desplazarse para verla. El resto del test es de HRM.

**Dependencias.** F-BLE-01, F-BLE-06, F-BLE-07 y F-BLE-12. HRM (tarjeta).

**Mejora EXR.** Un tablero de instrumentos realista, al nivel de un monitor de remo premium. Un remero en miniatura palea al ritmo real del SPM. Cada métrica tiene un brillo sutil de su color cuando cambia, y los números se animan. Nada de grillas planas: profundidad, materiales y luz.

---

### F-BLE-12: Diagnóstico BLE oculto

**Valor.** Soporte de campo: permite ver si el monitor manda datos y qué manda, sin herramientas externas.

**Comportamientos observables**
- BLE-12.1. **Acceso escondido.** En la vista conectada, después de desplazar hacia abajo 3 veces más de 20 px cada vez (unos 60 px acumulados), aparece el botón discreto "Show BLE debug" **[fijo, en inglés]**, con ícono de bicho y texto de 12 pt en gris 24 %. Si el contenido entra en pantalla y no hay scroll, el acceso **no aparece**. `device_screen.dart:52-74,124-130`
- BLE-12.2. Tocarlo abre el panel "DEBUG BLE" **[fijo]** (fondo negro 38 %, radio 12, borde blanco 12 %). El panel sigue abierto mientras dure la vista conectada; si el remo se desconecta, al volver a conectar hay que repetir el gesto. `device_screen.dart:131-134,142-218`
- BLE-12.3. Contenido del panel, todo **[fijo, en inglés]** salvo `(sin datos)`:
  - Insignia `RECEIVING` (verde) si llegó al menos 1 notificación; si no, `NO DATA` (roja).
  - "Notifications": contador de paquetes Rower Data. Es **acumulado durante toda la vida de la app** y no se reinicia al reconectar.
  - "Last received": hora local `HH:MM:SS` (24 h) del último paquete **de esta conexión**, o `--:--:--`.
  - "Raw bytes (hex):": el último paquete en hex minúscula de 2 dígitos separados por espacio (por ejemplo `2c 00 30 2a 01 …`), o `(sin datos)`. Va en cian y monoespaciado. El último paquete **no** se borra al reconectar.
  - "Parsed SPM" (1 decimal), "Parsed Watts", "Parsed Distance" (`<n>m`) y "Parsed Split" (`MM:SS`/`--:--`).
  - Fuentes: `device_provider.dart:20-22,56-61,98-102`; `device_screen.dart:148-214`

**Datos persistidos.** Ninguno.

**Tests legacy.** No hay.

**Dependencias.** F-BLE-01, F-BLE-02, F-BLE-06 y F-BLE-11.

**Mejora EXR.** Sigue oculta para el usuario común. Para soporte, una consola técnica prolija y legible, con un indicador de paquetes por segundo.

---

### F-BLE-13: Modo simulador, activación y aislamiento

**Valor.** Desarrollar y probar toda la app (entrenamiento, rutinas, escena, historial) en cualquier plataforma, sin remo ni pulsómetro, sin ensuciar datos reales ni Strava.

**Comportamientos observables**
- BLE-13.1. El modo se activa **solo** en builds de desarrollo, con un flag explícito de compilación (`SIMULATOR=true`). Sin el flag, el simulador no existe en el build y no hay forma de activarlo en una app instalada. Ningún build de distribución pasa ese flag. `lib/core/dev/dev_config.dart:1-5`; `CLAUDE.md` legacy ("Dev Mode")
- BLE-13.2. El IDE tiene una configuración de arranque "RowMate (simulador)" con el flag. `.vscode/launch.json`
- BLE-13.3. Funciona en cualquier plataforma, sin hardware.
- BLE-13.4. Reemplaza **a la vez** el remo y el pulsómetro: no existe la combinación "remo simulado + pulsómetro real". Comparten la misma física, así que el pulso del pulsómetro simulado sale del simulador del remo. `main.dart:51-54`; `simulated_heart_rate_service.dart:12-18,126-129`
- BLE-13.5. La base de datos va aparte: `rower_app_dev.db` en lugar de `rower_app.db`, para no mezclar sesiones de prueba con las reales. `lib/core/database/database_service.dart:10-11`
- BLE-13.6. **Nada se sube a Strava.** El envío central devuelve "sin resultado" sin hacer pedidos y lo registra (`[Strava] Modo simulador: subida bloqueada (sesión <id>)`). Las pantallas de entrenamiento no ofrecen subir: no hay aviso emergente ni diálogo, tampoco en los diálogos de rutina completada. El login y la importación desde Strava siguen funcionando. **Toda escritura o subida nueva a Strava tiene que respetar este bloqueo.** `lib/core/strava/strava_api_service.dart:81-84`; `workout_screen.dart:317,448,559`; `immersive_workout_screen.dart:151,199`
- BLE-13.7. El resto de la app (pantallas y lógica) funciona igual y no sabe que está simulada.

**Datos persistidos.** Solo el archivo de base separado (el contenido es de DB).

**Tests legacy.** No hay tests del flag. Hay cobertura indirecta en F-BLE-14 a F-BLE-16.

**Dependencias.** F-BLE-14, F-BLE-15 y F-BLE-16. DB, STR, HRM, WKT y SCN.

**Mejora EXR.** Se reconoce al instante (F-BLE-16.1) y es imposible que llegue a un usuario final.

---

### F-BLE-14: Física del remo simulado

**Valor.** Datos realistas y deterministas (sin ruido) para iterar la UI y probar la lógica.

**Comportamientos observables**
- BLE-14.1. Los objetivos son potencia **30–500 W** (por defecto **150**) y ritmo **14–40 spm** (por defecto **24**). Un valor fuera de rango se recorta al límite. "Remando" arranca encendido. `lib/core/dev/rowing_simulator.dart:7-10,20-29`
- BLE-14.2. Cada *tick* equivale a **1 s** simulado. Remando (`rowing_simulator.dart:45-54,73-82`):
  - W = máx(1, redondeo(objetivo × (1 + ruido uniforme en ±5 %)))
  - spm = máx(1, objetivo + ruido uniforme en ±1)
  - split exacto = 500 × (2,80 / W)^(1/3) s (fórmula de Concept2); el split que se emite es el redondeo
  - distancia += 500 / split exacto
  - paladas += spm / 60
  - kcal += (W × 4 × 0,8604 + 300) / 3600 (fórmula de Concept2)
  - tiempo += 1
- BLE-14.3. Sin remar: W, spm y split valen 0; distancia, paladas, kcal y tiempo quedan congelados. `rowing_simulator.dart:41-44`
- BLE-14.4. Pulso: el objetivo es mín(190, 90 + W × 0,35) remando y 70 sin remar (W es la potencia de ese tick, con ruido). Cada tick el pulso avanza un 10 % de la distancia al objetivo: pulso += (objetivo − pulso) × 0,1. Arranca en 70. `rowing_simulator.dart:37,56-57`
- BLE-14.5. Salida de cada tick: spm redondeado a 1 decimal; paladas, distancia y kcal en parte entera (con una tolerancia de 1e-6 para no perder una unidad por redondeo, por ejemplo 23,9999… → 24); pulso redondeado; tiempo en segundos remados. `rowing_simulator.dart:59-69`
- BLE-14.6. Sin ruido (modo de prueba) los valores emitidos son exactamente los objetivos. `rowing_simulator.dart:17-18,73-74`
- BLE-14.7. Se expone el último pulso calculado, que es el que emite el pulsómetro simulado. `rowing_simulator.dart:32`

**Tabla de referencia** (sin ruido; cálculo propio sobre las fórmulas del código):

| W | Split exacto | Split mostrado | m/s | kcal/h | Pulso estable |
|---|---|---|---|---|---|
| 30 (mín.) | 226,8 s | `03:47` | 2,205 | 403 | 100,5 |
| 100 (Suave) | 151,8 s | `02:32` | 3,293 | 644 | 125 |
| 150 (por defecto) | 132,6 s | `02:13` | 3,770 | 816 | 142,5 |
| 180 (Medio) | 124,8 s | `02:05` | 4,006 | 919 | 153 |
| 203 | 119,9 s | `02:00` | 4,170 | 999 | 161 |
| 280 (Fuerte) | 107,7 s | `01:48` | 4,642 | 1264 | 188 |
| 500 (máx.) | 88,8 s | `01:29` | 5,631 | 2021 | 190 (tope) |

El pulso, a 150 W desde 70, pasa por 77, 84, 90, 95, 100… y llega a 142 a los 60 s. A 150 W y 24 spm, 60 s dan 226 m, 24 paladas y 13 kcal.

**Tests legacy** (`test/rowing_simulator_test.dart`)
- `:7` "más watts → pace más rápido": el split de 300 W es menor que el de 150 W.
- `:14` "203 W ≈ 2:00 /500m (fórmula Concept2)": el split de 203 W es 120 ± 0,5 s.
- `:18` "acumula distancia, remadas y tiempo mientras rema": sin ruido, a 150 W / 24 spm, después de 60 ticks: W 150, spm 24, split 133, tiempo 60, paladas 24, distancia 226, kcal 13 y pulso > 70.
- `:36` "sin remar: 0 spm/watts/pace y totales congelados": después de 11 ticks se apaga "remando"; el tick siguiente da spm 0, W 0 y split 0, la misma distancia, paladas, tiempo y kcal que el tick anterior, y pulso menor.
- `:54` "los objetivos se limitan a su rango": 9999 W pasa a 500 y 1 spm pasa a 14.
- `:62` "con ruido, los watts quedan dentro de ±5 %": con objetivo de 200 W, 100 ticks quedan todos en [190, 210].
- `:70` "heartRate expone el último pulso calculado por tick()": después de 1 tick, el pulso expuesto es igual al de la lectura y > 70.

**Dependencias.** F-BLE-01. La usan F-BLE-15, F-BLE-16 y HRM (pulsómetro simulado).

**Mejora EXR.** Las fórmulas no cambian (paridad). La experiencia es que la escena reacciona de forma creíble e inmediata a cada ajuste del panel.

---

### F-BLE-15: Remo simulado "conectado"

**Valor.** La app arranca lista para entrenar, con un remo que se comporta como el real en sus estados.

**Comportamientos observables**
- BLE-15.1. El equipo se llama **"Remo simulado"**. `lib/core/bluetooth/simulated_ble_service.dart:11`
- BLE-15.2. **Arranca conectado solo**: apenas la app empieza a escuchar el estado, pasa a `connected` (una sola vez en la vida de la app). El adaptador figura siempre encendido. `simulated_ble_service.dart:17-26,55`
- BLE-15.3. Mientras está conectado manda **una lectura por segundo** (un tick de F-BLE-14) con **pulso 0**: el pulso viaja solo por el pulsómetro simulado, como con el monitor real. `simulated_ble_service.dart:88-97`
- BLE-15.4. El nombre "Remo simulado" se expone solo en `connected`; en otro estado no hay nombre. `simulated_ble_service.dart:59-60`
- BLE-15.5. "Buscar dispositivos" y "Conectar" reconectan al instante: no hay estado `scanning` ni lista. Detener la búsqueda no hace nada. `simulated_ble_service.dart:62-71`
- BLE-15.6. "Desconectar" detiene los datos y pasa a `disconnected`, sin reconectar. `simulated_ble_service.dart:74-78`
- BLE-15.7. "Simular desconexión" solo actúa si está conectado: pasa a `disconnected`, deja de mandar datos y a los **3 s** vuelve a `connected`. `simulated_ble_service.dart:81-86`
- BLE-15.8. Conectar estando ya conectado no repite el estado (no hay eventos duplicados). `simulated_ble_service.dart:91`
- BLE-15.9. No manda paquetes crudos ni lista de equipos. Por eso el diagnóstico muestra `NO DATA` y el watchdog nunca dispara.
- BLE-15.10. La física **no se reinicia** al reconectar: distancia, paladas y tiempo siguen sumando desde el arranque de la app, y solo avanzan mientras está conectado.

**Vista resultante en la pestaña.** Al abrir se ve la vista conectada ("Remo simulado" + "Conectado"). Con "Simular desconexión", durante 3 s se ve la vista de búsqueda con "No se encontraron dispositivos" y después vuelve la vista conectada, primero en ceros y con valores al segundo.

**Tests legacy** (`test/simulated_ble_service_test.dart`, con reloj falso)
- `:13` "emite connected y adapter on al primer listener, y datos cada segundo": los estados son [connected] y el adaptador [on]; el estado es `connected` y el nombre "Remo simulado". En 3 s hay 3 lecturas, todas con pulso 0 ("el pulso llega solo por el pulsómetro simulado"), y la última distancia es mayor que la primera.
- `:39` "simulateDisconnect emite disconnected y reconecta a los 3 s": se describe en F-BLE-08.
- `:64` "disconnect no reconecta solo; startScan reconecta": se describe en F-BLE-07.

**Dependencias.** F-BLE-13 y F-BLE-14. La usa F-BLE-16.

**Mejora EXR.** Los estados de la app con el simulador se ven idénticos a los del remo real, para que lo que se diseña con el simulador sea lo que ve el usuario.

---

### F-BLE-16: Panel de control del simulador

**Valor.** Cambiar la intensidad, el ritmo, la hora de la escena y provocar fallas al instante, desde cualquier pantalla.

**Comportamientos observables**
- BLE-16.1. Cinta diagonal **"SIMULADOR"** en naranja profundo en la esquina superior izquierda, por encima de **toda** la app, incluida la pantalla de entrenamiento a pantalla completa. `lib/core/dev/simulator_overlay.dart:33-36`; `main.dart:108-110`
- BLE-16.2. Botón flotante chico con ícono de llave, a 16 px del borde derecho y 96 px del inferior (por encima de la barra de navegación). Tocarlo abre la tarjeta del panel en ese mismo lugar. `simulator_overlay.dart:40-49`
- BLE-16.3. La tarjeta tiene sombra y un ancho fijo de **280 px**. Su alto máximo es el alto de la ventana − 96 − el margen superior seguro − 16, con un mínimo de 120, y tiene scroll interno para que en ventanas bajas el botón de cerrar siga accesible. `simulator_overlay.dart:83-95`
- BLE-16.4. Contenido en orden **[fijo, en español]**:
  1. "Simulador" y el botón cerrar (X). Cerrar vuelve al botón flotante. `:101-111`
  2. Presets: **"Suave"** (100 W / 20 spm), **"Medio"** (180 W / 24 spm) y **"Fuerte"** (280 W / 30 spm). `:10-14,112-124`
  3. "Watts": − / `<n> W` / +, de a **10**, dentro de 30–500. `:125-131`
  4. "Ritmo": − / `<n> spm` / +, de a **1**, dentro de 14–40. `:132-138`
  5. "Hora escena": − / valor / +. El valor es `real` si no hay hora forzada; si la hay, `<h>:00` con la hora sin cero (0–23, por ejemplo `7:00`). Cada toque mueve ±1 h desde la hora forzada o, si no hay, desde la hora actual del reloj (solo la hora). Da la vuelta entre 23 y 0. La hora forzada cambia la luz y la paleta de la escena (ver SCN). `:139-145`
  6. "Hora real": aparece solo con hora forzada y vuelve a la hora real. `:146-150`
  7. Interruptor "Remando": deja de remar o vuelve a remar (BLE-14.3). `:151-156`
  8. "Simular desconexión" (ícono de enlace roto): BLE-15.7. `:157-161`
  9. "Simular caída del pulsómetro" (ícono de corazón roto): si el pulsómetro simulado está conectado, se cae y a los 3 s se reconecta; mientras tanto, la tarjeta del pulsómetro muestra "reconectando...". Si no está conectado, no hace nada. `:162-166`; `simulated_heart_rate_service.dart:107-116`
- BLE-16.5. Los cambios se aplican al instante en el simulador y se notan en el próximo tick (≤ 1 s).
- BLE-16.6. Nada del panel se guarda: objetivos, hora forzada y panel abierto o cerrado viven mientras dure la app. `lib/features/workout/scene/scene_settings.dart:14,19-24`

**Datos persistidos.** Ninguno.

**Tests legacy** (`test/simulator_overlay_test.dart`)
- `:17` "el panel se abre fuera del Navigator y + sube los watts". Con el panel cerrado no se ve "150 W". Al tocar la llave se ven "150 W" y "24 spm". Con + de watts el objetivo pasa a 160 y se ve "160 W"; con + de ritmo pasa a 25. Con + de hora aparece "Hora real". "Simular desconexión" no da error. Con el pulsómetro simulado conectado, "Simular caída del pulsómetro" no da error y deja el pulsómetro `disconnected` y reintentando. "Fuerte" pone 280 W / 30 spm. Tocar "Remando" lo apaga. Al cerrar, ya no se ve "280 W".
- `:90` "en una ventana baja el botón de cerrar sigue accesible": en 800×360 el panel abre (se ve "150 W") y el botón de cerrar lo cierra.

**Dependencias.** F-BLE-14 y F-BLE-15. HRM (pulsómetro simulado) y SCN (hora de la escena).

**Mejora EXR.** Un panel de desarrollador con la misma estética de la app (vidrio esmerilado, tipografía de la app) que no tapa la escena y en el que cada ajuste se ve reflejado al instante en el agua, el bote y las métricas.

---

## 4. Textos visibles

### 4.1. Por clave `.arb` (es / en)

| Clave | es | en | Dónde |
|---|---|---|---|
| `navDevice` | Dispositivo | Device | barra inferior |
| `deviceTitle` | Dispositivo | Device | título |
| `deviceDisconnect` | Desconectar | Disconnect | barra superior (conectado) |
| `deviceConnected` | Conectado | Connected | insignia |
| `deviceSearch` | Buscar dispositivos | Search devices | botón |
| `deviceSearching` | Buscando... | Searching... | botón buscando |
| `deviceSearchHint` | Buscando dispositivos FTMS con servicio 0x1826... | Searching for FTMS devices with service 0x1826... | hint buscando |
| `deviceNoResults` | No se encontraron dispositivos | No devices found | vacío |
| `deviceNoResultsHint` | Asegurate que el rower esté encendido⏎y el Bluetooth activado | Make sure the rower is on⏎and Bluetooth is enabled | vacío |
| `deviceBtOff` | Bluetooth apagado | Bluetooth is off | BT apagado |
| `deviceBtOffHint` | Activá el Bluetooth desde el Centro de Control⏎o Ajustes para buscar el rower | Turn on Bluetooth from Control Center⏎or Settings to search for the rower | BT apagado |
| `deviceBtDenied` | Permiso de Bluetooth denegado | Bluetooth permission denied | sin permiso |
| `deviceBtDeniedHint` | Habilitá el permiso de Bluetooth en⏎Ajustes > Privacidad > Bluetooth | Enable Bluetooth permission in⏎Settings > Privacy > Bluetooth | sin permiso |
| `deviceBtEnable` | Activar Bluetooth | Enable Bluetooth | botón |
| `metricSplit` | SPLIT 500m | SPLIT 500m | grilla |
| `metricSpm` | SPM | SPM | grilla |
| `metricWatts` | VATIOS | WATTS | grilla |
| `metricDistance` | DISTANCIA | DISTANCE | grilla |
| `metricHeartRate` | PULSO | HR | grilla (solo con pulso FTMS > 0) |
| `workoutRowerNotConnected` | Conectá el rower para iniciar un entrenamiento | Connect the rower to start a workout | Entrenamiento |

`⏎` = salto de línea dentro del texto. Fuente: `lib/l10n/app_es.arb:4,10-22,52,77-83` y `app_en.arb`, mismas líneas. Las claves `hrm*` de la tarjeta del pulsómetro van en el inventario HRM.

### 4.2. Fijos en el código (sin clave, iguales en los dos idiomas)

| Texto | Dónde | Fuente |
|---|---|---|
| `Unknown device` | ítem sin nombre | `device_screen.dart:452` |
| `Rower` | cabecera sin equipo | `device_screen.dart:98` |
| `Error al escanear: <detalle>` / `Error al conectar: <detalle>` | banner de error | `device_provider.dart:111,124` |
| `Show BLE debug`, `DEBUG BLE`, `RECEIVING`, `NO DATA`, `Notifications`, `Last received`, `Raw bytes (hex):`, `Parsed SPM`, `Parsed Watts`, `Parsed Distance`, `Parsed Split`, `(sin datos)` | diagnóstico | `device_screen.dart:128,168,184,196-213`; `device_provider.dart:99` |
| `SIMULADOR`, `Simulador`, `Suave`, `Medio`, `Fuerte`, `Watts`, `Ritmo`, `Hora escena`, `real`, `Hora real`, `Remando`, `Simular desconexión`, `Simular caída del pulsómetro`, `<n> W`, `<n> spm`, `<h>:00` | simulador | `simulator_overlay.dart` |
| `Remo simulado` | nombre del remo simulado | `simulated_ble_service.dart:11` |
| `Se necesita Bluetooth para conectar con el monitor del rower.` | diálogo de permiso iOS/macOS | `ios/Runner/Info.plist:45`; `macos/Runner/Info.plist:32` |

---

## 5. Datos persistidos (resumen)

Este subsistema **no persiste nada propio**: no usa claves de SharedPreferences, no tiene tablas y no recuerda el remo. Lo único relacionado es el nombre del archivo de base en modo simulador (`rower_app_dev.db` en lugar de `rower_app.db`, `database_service.dart:11`). El contenido de esa base es del área DB.

---

## 6. Cobertura de tests legacy y huecos

| Test | Casos | Qué cubre |
|---|---|---|
| `test/rowing_simulator_test.dart` | 7 | F-BLE-14 (todos los casos están descriptos arriba) |
| `test/simulated_ble_service_test.dart` | 3 | F-BLE-15, F-BLE-07 y F-BLE-08 (solo la versión simulada) |
| `test/simulator_overlay_test.dart` | 2 | F-BLE-16 |
| `test/scan_filter_test.dart` | 2 | F-BLE-03 |
| `test/heart_rate_card_test.dart` (inventario HRM) | 4 (todos montan la pestaña Dispositivo) | `:121` y `:161` cubren F-BLE-05.8, `:142` F-BLE-05 en horizontal, `:83` F-BLE-11 |
| `test/widget_test.dart` | 1 | **ninguno**: es un placeholder vacío, sin aserciones |

**Huecos (sin test en el legacy; el QA tiene que cubrirlos con criterios nuevos):**
- la decodificación FTMS (tabla de ejemplos de F-BLE-02);
- los formatos de `RowingData` (F-BLE-01);
- la búsqueda, conexión, toma de control, keepalive y reconexión reales (F-BLE-05/06/08);
- el watchdog de 5 s (F-BLE-09);
- los mensajes de error;
- los estados de la vista de búsqueda y "Bluetooth apagado / sin permiso" (F-BLE-04);
- la grilla conectada y su cambio de 2 a 3 columnas (F-BLE-11);
- el diagnóstico (F-BLE-12);
- la habilitación del entrenamiento según la conexión (F-BLE-10).

---

## 7. Diferencias entre docs y código (manda el código)

1. **UUID de Rower Data.** `CLAUDE.md`, la tabla de `README.md:37`, el comentario de `ftms_parser.dart:5` y el de `rowing_data.dart:1` dicen `0x2AD2`. El código usa **`0x2AD1`** (`ftms_parser.dart:22`), que es el correcto en el estándar FTMS (`0x2AD2` es Indoor Bike Data). El log de `ble_service.dart:252` también dice 0x2AD1.
2. **Reconexión automática.** El comentario de `ble_service.dart:162` ("cada 3 segundos hasta lograrlo"), `CLAUDE.md` y `README.md:73` ("recovers from drops automatically") prometen reintentos hasta reconectar. El código hace en la práctica **un solo intento**, a los 3 s y con timeout de 10 s, y si falla no reintenta (BLE-08.3).
3. **Pantalla siempre encendida.** `CLAUDE.md` dice "durante los entrenamientos". El código la activa al arrancar la app, para toda la sesión (`main.dart:42`). Le corresponde al área APP; se anota acá por si nadie más lo toma.
4. **Spec del simulador, panel.** `2026-10-01-dev-simulator-design.md:61-66` no menciona "Hora escena", "Hora real" ni "Simular caída del pulsómetro"; el código los tiene (`simulator_overlay.dart:139-166`).
5. **Spec del simulador, datos.** Dice que se emite `simulator.tick()` (`:48`) y la tabla incluye el pulso. El código emite el tick **con pulso 0** (`simulated_ble_service.dart:95`); el pulso llega solo por el pulsómetro simulado. `CLAUDE.md` lo dice bien.
6. **Spec del simulador, Strava en la UI.** Solo menciona `workout_screen.dart` (`:73`). El código también bloquea en `immersive_workout_screen.dart:151` y en los diálogos de rutina completada (`:199`; `workout_screen.dart:317`).
7. **Spec del simulador, tests.** La lista de `:84-94` está incompleta. Faltan: recorte de objetivos, ruido de ±5 %, pulso expuesto, ventana baja, hora de la escena y caída del pulsómetro.
8. **Textos sin localizar.** `README.md:256` habla de *Sensor desconocido / Unknown device* como si estuviera localizado. Para el remo, "Unknown device" está fijo en inglés en los dos idiomas (`device_screen.dart:452`). Lo mismo pasa con "Rower", los errores (fijos en español) y el diagnóstico (fijo en inglés).
9. **Métricas.** `README.md:40` lista "Calories" entre las métricas decodificadas, y en efecto se decodifican, pero la pestaña Dispositivo no las muestra; tampoco muestra el tiempo ni las paladas.
10. **Arquitectura declarada.** `CLAUDE.md` dice "navigation via go_router" y "BleService: Singleton". `go_router` está en `pubspec.yaml:24` pero no se usa en `lib/`: la navegación es una barra inferior con pestañas apiladas (`main.dart:143`). `BleService` no es un singleton: es una instancia creada por la app (`main.dart:51`). No afecta al producto; se anota para que nadie lo tome como requisito.
11. **`test/widget_test.dart`** figura como "smoke test" pero está vacío.

---

## 8. Preguntas abiertas

- **Q1. Reconexión.** ¿Paridad con el código (un solo intento a los 3 s) o con la intención documentada (reintentar cada 3 s hasta reconectar o hasta que el usuario actúe, como el pulsómetro)? **Propuesta PO:** adoptar la intención como *cambio de producto* explícito, porque es un defecto del legacy. Hasta que el usuario decida, el contrato de paridad es el comportamiento del código.
- **Q2. Recordar el remo.** El legacy no recuerda el último remo ni se conecta solo al arrancar; el pulsómetro sí. ¿Lo sumamos como mejora (sin quitar la búsqueda manual)? Hoy queda fuera de paridad.
- **Q3. Watchdog y pausas.** El watchdog desconecta sin reintentar. Si un monitor deja de notificar cuando el remero para, una pausa de más de 5 s corta la conexión. ¿Se mantiene la regla tal cual, o se avisa antes y se ofrece reconectar?
- **Q4. Localización de los textos fijos** ("Unknown device", "Rower", errores, diagnóstico, texto del permiso de iOS/macOS solo en español). **Propuesta PO:** crear claves nuevas es/en y usar los textos actuales como valor en el idioma en que ya están. El panel del simulador puede quedar solo en español (es una herramienta de desarrollo).
- **Q5. "Activar Bluetooth" fuera de Android.** No hace nada en iOS, macOS ni Windows. ¿Llevamos al usuario a los ajustes del sistema? Sería un cambio de producto menor.
- **Q6. Desconexión manual durante un entrenamiento.** El legacy no pide confirmación. ¿Agregamos una confirmación cuando hay una sesión activa?
- **Q7. Caída del remo en pleno entrenamiento.** Hoy no se avisa (métricas congeladas, reloj corriendo). ¿Agregamos un aviso en la pantalla de entrenamiento? Afecta también a WKT y HUD.
- **Q8. Equipos FTMS que no son remos.** Aparecen en la lista y "conectan" sin datos para siempre. ¿Los filtramos o avisamos?
- **Q9. Exclusión de búsquedas remo/pulsómetro.** En el legacy viene de una limitación de la plataforma. Si la versión nueva puede buscar los dos a la vez, ¿se mantiene la exclusión en la UI? **Propuesta PO:** mantenerla en la iteración de paridad y revisarla después.
- **Q10. Acceso al diagnóstico.** En pantallas grandes, donde el contenido no tiene scroll, el gesto de 3 desplazamientos no funciona. ¿Mantenemos el gesto y agregamos una alternativa equivalente (por ejemplo, mantener presionada la cabecera)?
- **Q11. Valores FTMS especiales.** "No disponible" (0xFFFF) y la potencia negativa se muestran crudos. ¿Mantener o mostrar `--`? Hoy es paridad cruda.
- **Q12. Detener la búsqueda.** No existe un botón para cortar los 10 s. ¿Mantener?
- **Q13. Restricción para el Architect.** El simulador **nunca** puede quedar activable en un build de distribución. Además tiene que reemplazar al remo y al pulsómetro a la vez (o definir si en Unity se permite "remo simulado + pulsómetro real", que sería un cambio de producto).

---

## Cambios por QA (plan)

Correcciones por el gate de armonía del plan ([QA_PLAN_REVIEW.md](../../QA_PLAN_REVIEW.md)), 2026-10-27.

| ID | Qué cambió |
|---|---|
| QA-P-18 | BLE-04 (Android): el mínimo real del legacy es API 24 (Android 7.0, el valor por defecto de Flutter 3.47.6), no API 21. La app nueva pide Android 8.0 (API 26) y ARM64 (Q-90 de FEATURE_INVENTORY) |
| Spec de la It. 1 (PO) | F-BLE-14, tabla de referencia: a 180 W son 919 kcal/h (919,488), no 920 (verificado con el oráculo Dart). Valores exactos en [01-po-spec AC-BLE-14-04](../../iterations/01-fundaciones/01-po-spec.md) |
