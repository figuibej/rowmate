# It. 1 — Fundaciones: spec de producto

| Campo | Valor |
|---|---|
| Autor | rowmate-po |
| Fecha | 2026-10-27 |
| Iteración | 1 · `01-fundaciones` ([ROADMAP §2, It. 1](../../ROADMAP.md)) |
| Estado | Corregida según el gate de armonía ([03-qa-harmony.md](03-qa-harmony.md), APROBADO CON OBSERVACIONES): QA-02, QA-11 y QA-12 resueltos (ver "Cambios por gate QA", al final). Alineada con [02-tech-design.md](02-tech-design.md) D-02, D-12 y §10.3 |
| IDs dueños | **20 = 17 canónicos + 3 alias**: F-BLE-01 (+ F-RUT-15), F-BLE-02, F-BLE-03, F-BLE-14, F-HRM-01, F-RUT-01 (+ F-APP-07), F-RUT-02, F-RUT-03, F-RUT-04, F-RUT-05, F-RUT-06, F-RUT-07, F-RUT-16, F-RUT-17, F-RUT-18 (+ F-HIS-04), F-APP-09, F-APP-10 |
| También toca | F-APP-16 (nombre, identificadores provisionales y Android mínimo; se cierra en la It. 14), F-APP-19 (`ci-core.yml`; se cierra en la It. 14) |
| Criterios | **139**: BLE 37 (F-BLE-01 6, F-BLE-02 16, F-BLE-03 5, F-BLE-14 10), HRM 9, RUT 58, APP 25 (F-APP-09 7, F-APP-10 9, F-APP-16 5, F-APP-19 4) y BASE 10. Más los 19 casos legacy (§5) |
| Contrato | `rowmate-legacy` (solo lectura). Cada valor de esta spec se leyó en `lib/` y `test/`. Los numéricos y los textos se reprodujeron además con el oráculo Dart (§0.3) |
| Insumos | [FEATURE_INVENTORY](../../product/FEATURE_INVENTORY.md), inventarios [rower-ble-device-sim](../../product/inventory/rower-ble-device-sim.md), [heart-rate](../../product/inventory/heart-rate.md), [routines-models](../../product/inventory/routines-models.md) y [app-shell-platform](../../product/inventory/app-shell-platform.md), [QA_PLAN_REVIEW](../../QA_PLAN_REVIEW.md), [ARCHITECTURE](../../architecture/ARCHITECTURE.md) y ADR 001, 011, 012, 013, 014 y 016, [ART_BIBLE](../../design/ART_BIBLE.md) §2-§3 |

---

## 0. Cómo leer esta spec

### 0.1 Identificadores

- **`AC-<ÁREA>-<NN>-<nn>`**: criterio `nn` de la ficha `F-<ÁREA>-<NN>`. Por ejemplo, `AC-BLE-02-05` es el criterio 5 de F-BLE-02. Los alias se verifican con su canónico: F-RUT-15 con F-BLE-01, F-APP-07 con F-RUT-01 y F-HIS-04 con F-RUT-18.
- **`AC-BASE-nn`**: piezas base sin ID propio (P-01 ampliada, [QA_PLAN_REVIEW §4 y §9](../../QA_PLAN_REVIEW.md)).
- **`LT-nn`**: los 19 casos de test legacy que se portan 1 a 1 (§5).
- **"caso N"**: caso N de [routines-models §5.3](../../product/inventory/routines-models.md) (mapa en §6).
- Cada AC indica cómo se verifica: **[dotnet]**, **[Editor]**, **[Device]**, **[Apple]** (leyenda del ROADMAP §0). "**NO VERIFICABLE → It. N**" quiere decir que en la It. 1 QA lo revisa de forma estática y que se verifica de verdad en la It. N (ROADMAP §2, It. 1, "Verificación").

### 0.2 Notación

- Paquetes en hexadecimal, un byte por grupo (`2C 00 30`). En F-HRM-01 se usa la notación del test legacy (`[0x00, 72]`: banderas en hex, pulso en decimal).
- Los textos entre comillas invertidas son **exactos al carácter**: `·` es U+00B7 (punto medio), `📈` es U+1F4C8, `...` son tres puntos (no el carácter de elipsis) y `¿`/`¡` son los signos de apertura.
- `⏎` = salto de línea dentro del texto, **sin** espacios alrededor. `␣` = un espacio que importa.
- Los números que ve el usuario se escriben con punto (`24.0`, como en la app); en la prosa, con coma (24,5 spm).

### 0.3 Oráculo

Los valores esperados se reprodujeron el 2026-10-27 con **Dart 3.13.5** (SDK de `<flutter-sdk>`, Flutter 3.47.6), con un script fuera de los dos repositorios que importa por ruta, en solo lectura, `core/models/rowing_data.dart`, `core/bluetooth/ftms_parser.dart`, `core/bluetooth/heart_rate_parser.dart`, `core/dev/rowing_simulator.dart`, `core/models/interval_step.dart`, `core/models/routine.dart` y `core/models/workout_session.dart`. `git status --porcelain` del legacy quedó vacío antes y después.

Lo que depende de Flutter o de sus paquetes no corre en el oráculo y se tomó leyendo el código:

- Filtro de búsqueda: `lib/core/bluetooth/scan_filter.dart` y la igualdad de UUID de `flutter_blue_plus_platform_interface` 9.0.3 (`lib/src/guid.dart`; versión fijada en `pubspec.lock`).
- Elección de idioma: `lib/main.dart:103-106` y el algoritmo del SDK `basicLocaleListResolution` (`flutter/packages/flutter/lib/src/widgets/app.dart:146-238`, Flutter 3.47.6).

---

## 1. Objetivo y demo

**Objetivo.** Que cada número y cada texto que el remero va a ver en la app nueva salga de reglas **idénticas** a las del legacy y verificadas fuera de Unity: la lectura del remo y del pulsómetro, el filtro de búsqueda, la física del simulador, los pasos, las rutinas con series y progresiones, la sesión, la telemetría, las estadísticas y el catálogo es/en. Además queda armado el proyecto Unity (todavía sin pantallas) y la red de seguridad que usan todas las iteraciones: paridad numérica con Dart, vectores del oráculo, una sola fuente de colores y el CI de los tests.

**Demo (se le muestra al usuario antes de empezar la It. 2).**

1. En la PC, **un solo comando documentado** corre todos los tests del Core sin Unity y termina en verde. Incluye los **19 casos legacy portados** (§5), cada uno con su nombre original, y los criterios nuevos de esta spec, que se pueden filtrar por ID de AC.
2. En la salida se ven, entre otros, los 8 paquetes FTMS decodificados (`24.0` spm, 298 paladas, 482 m, `02:13`, 150 W), el simulador a 150 W y 24 spm (60 s → 226 m, 24 paladas, 13 kcal, 142 bpm), los resúmenes de rutina en es y en (`1 serie · 1 descanso` / `1 interval · 1 rest`) y el P99.
3. Si el proyecto ya tiene repositorio remoto en GitHub: la corrida de `ci-core.yml` en verde, en Windows y en Linux. Si no lo tiene, QA muestra que el workflow corre los mismos comandos y la corrida queda NO VERIFICABLE (P-03 de §13).
4. El árbol del proyecto Unity (Core puro, servicios, presentación) con la revisión estática de QA, listo para abrirse en cuanto el usuario instale Unity 6000.3.27f1.

---

## 2. Alcance

| ID | Funcionalidad | En la It. 1 se cierra | Se ve en pantalla en |
|---|---|---|---|
| F-BLE-01 (+ F-RUT-15) | Lectura en vivo del remo y formatos | Modelo de la lectura, formatos de split y tiempo, textos de la grilla de Dispositivo | It. 2 (Dispositivo), 4a (HUD) |
| F-BLE-02 | Paquete FTMS Rower Data `0x2AD1` | Decodificación completa | It. 10a (remo real) |
| F-BLE-03 | Filtro por servicio anunciado | Regla por plataforma y equivalencia de UUID | It. 10a / 11 (S-1 opcional) |
| F-BLE-14 | Física del remo simulado | Fórmulas, topes, pulso, ruido y redondeos | It. 2 (simulador) |
| F-HRM-01 | Paquete Heart Rate Measurement `0x2A37` | Decodificación completa | It. 2 (simulado), 11 (real) |
| F-RUT-01 (+ F-APP-07) | Tipos de paso | Identidad, orden, nombres es/en, colores, íconos (dato) | It. 3 (editor), 4b, 9 |
| F-RUT-02 | Modelo de paso | Atributos, meta, copia, sin validación | It. 3 |
| F-RUT-03 | Etiqueta de duración | Texto exacto es/en | It. 3 |
| F-RUT-04 | Etiqueta de objetivos | Texto exacto es/en, progresiones | It. 3, 4a |
| F-RUT-05 | Modelo de rutina y resumen | Conteo y texto es/en con plural | It. 3, 4a |
| F-RUT-06 | Expansión de series con progresiones | Regla completa | It. 4a, 9, 12 |
| F-RUT-07 | Posición de cada paso en su serie | Regla completa | It. 5 |
| F-RUT-16 | Sesión (registro) | Atributos, "promedio" = P99, duración, fechas | It. 4a, 9 |
| F-RUT-17 | Punto de telemetría | Atributos y valores por defecto | It. 4a, 9 |
| F-RUT-18 (+ F-HIS-04) | Estadísticas: P99, totales, pulso | Regla completa | It. 9, 12 |
| F-APP-09 | Idiomas y elección por el sistema | Regla de elección; lo visible → It. 2 | It. 2 |
| F-APP-10 | Catálogo es/en | 174 claves, parámetros, plurales | todas |
| F-APP-16 (también toca) | Identidad | Nombre, ids provisionales, versión, Android mínimo (configuración) | It. 2, 10b, 14 |
| F-APP-19 (también toca) | Compilación y CI | `ci-core.yml` con higiene del repositorio | cada cambio |

---

## 3. Historias de usuario

| ID | Historia |
|---|---|
| H-01 | Como **remero**, quiero que la app nueva lea mi monitor FTMS exactamente como la vieja, para que split, vatios, distancia y paladas sean los mismos números. |
| H-02 | Como **remero con pulsómetro**, quiero que cualquier banda o reloj estándar funcione sin configurar nada, para entrenar por pulso. |
| H-03 | Como **remero en Windows**, quiero que la búsqueda muestre solo remos (o solo pulsómetros) y no los auriculares del vecino. |
| H-04 | Como **desarrollador o tester**, quiero un remo simulado con la física de Concept2 y un modo sin ruido, para probar toda la app sin hardware y con resultados repetibles. |
| H-05 | Como **remero que arma rutinas**, quiero que mis series ×N con progresiones se expandan igual que hoy, para que cada repetición tenga el objetivo que espero. |
| H-06 | Como **remero**, quiero leer la duración, los objetivos y el resumen de cada rutina en mi idioma (es o en), con el mismo español de hoy y plurales correctos. |
| H-07 | Como **remero que mira su historial**, quiero que el "promedio", los totales y el pulso de cada sesión den lo mismo que en la app vieja (P99 incluido). |
| H-08 | Como **usuario de la app vieja**, quiero instalar la nueva sin que reemplace ni borre la vieja ni sus datos. |
| H-09 | Como **usuario de Android**, quiero saber de antemano que la app nueva pide Android 8.0 o más nuevo y un equipo de 64 bits. |
| H-10 | Como **equipo** (PO, Architect, QA), queremos que cada cambio corra todos los tests del Core sin Unity ni secretos, para que ninguna iteración rompa la paridad sin que se note. |

---

## 4. Criterios de aceptación

### 4.1 F-BLE-01 (+ F-RUT-15) — Lectura en vivo del remo y formatos

- **AC-BLE-01-01 · Lectura y lectura vacía.** *Dado* una lectura del remo, *entonces* tiene 8 campos: ritmo (spm, con decimales), paladas totales, distancia (m), split (s/500 m), potencia (W), calorías totales (kcal), pulso (bpm) y tiempo transcurrido (s), todos enteros salvo el ritmo. *Dado* una lectura vacía, *entonces* los 8 valen 0, su split se ve `--:--` y su tiempo `00:00`. Pulso 0 significa "sin dato", no "0 bpm". [dotnet] · `lib/core/models/rowing_data.dart:2-21`
- **AC-BLE-01-02 · Split (caso 15).** *Dado* un split en segundos, *cuando* se formatea, *entonces* es `--:--` si es ≤ 0 y si no `MM:SS` con minutos y segundos a 2 dígitos y cero a la izquierda (desde 6000 s, los minutos tienen 3 dígitos). No es `m:ss`. [dotnet] · `rowing_data.dart:24-29`

  | s | −5 | 0 | 1 | 59 | 60 | 120 | 125 | 133 | 599 | 600 | 5999 | 6000 | 6001 |
  |---|---|---|---|---|---|---|---|---|---|---|---|---|---|
  | texto | `--:--` | `--:--` | `00:01` | `00:59` | `01:00` | `02:00` | `02:05` | `02:13` | `09:59` | `10:00` | `99:59` | `100:00` | `100:01` |

- **AC-BLE-01-03 · Tiempo (caso 15).** *Dado* un tiempo en segundos, *cuando* se formatea, *entonces* es `H:MM:SS` con la hora sin cero a la izquierda si hay al menos 1 hora, y si no `MM:SS`. [dotnet] · `rowing_data.dart:32-40`

  | s | 0 | 1 | 59 | 60 | 61 | 3599 | 3600 | 3661 | 36000 | 86400 |
  |---|---|---|---|---|---|---|---|---|---|---|
  | texto | `00:00` | `00:01` | `00:59` | `01:00` | `01:01` | `59:59` | `1:00:00` | `1:01:01` | `10:00:00` | `24:00:00` |

- **AC-BLE-01-04 · Copia con cambios.** *Dado* una lectura con 24,0 spm, 150 W y el resto en 0, *cuando* se copia cambiando solo el pulso a 140, *entonces* la copia tiene 140 bpm y los otros 7 campos sin cambios. Un campo que no se nombra nunca se borra (así el pulsómetro reemplaza el pulso del remo sin tocar lo demás). [dotnet] · `rowing_data.dart:42-62`
- **AC-BLE-01-05 · Textos de la grilla de Dispositivo.** *Dado* una lectura, *cuando* se arman los textos de la grilla, *entonces*: SPM con 1 decimal y **punto** en los dos idiomas (24,0 → `24.0`; 23,5 → `23.5`; 24,25 → `24.3`); vatios como entero sin unidad (149 → `149`; −10 → `-10`); distancia como entero con `m` pegada y sin separador de miles (652 → `652m`; 10000 → `10000m`); pulso como entero sin unidad (140 → `140`). [dotnet] para la regla; en pantalla [Editor] NO VERIFICABLE → It. 2 (F-BLE-11) · `lib/features/device/device_screen.dart:253-258`
- **AC-BLE-01-06 · Quién usa estas dos reglas.** *Entonces* el formato `MM:SS` de AC-BLE-01-02 es el del split en la grilla de Dispositivo, en la tarjeta de split del HUD y en los chips de split del historial y del detalle (el split de las estadísticas, AC-RUT-18-09), y el formato de AC-BLE-01-03 es el de la duración de la sesión (AC-RUT-16-03) y de las estadísticas. Los otros textos de tiempo del legacy tienen formatos propios y **no** son estos (cuenta regresiva del paso `M:SS`, parciales `m:ss.d`, rendimiento por paso `m:ss`): los fijan las It. 4a, 5 y 9 (F-WKT-11, F-HIS-05). [dotnet] · `workout_session.dart:29-37`, `:94-109`; `immersive_workout_screen.dart:647`, `:457`; `history_screen.dart:111`, `:115`; `session_detail_screen.dart:162`, `:166`, `:221-224`; `series_panels.dart:13`, `:19`

### 4.2 F-BLE-02 — Decodificación del paquete FTMS Rower Data

- **AC-BLE-02-01 · Identificadores.** *Entonces* el servicio FTMS es `00001826-0000-1000-8000-00805f9b34fb`, la característica que se decodifica es Rower Data **`00002ad1-0000-1000-8000-00805f9b34fb`** (0x2AD1; no 0x2AD2, DC-01) y el Control Point es `00002ad9-0000-1000-8000-00805f9b34fb` (lo usa la It. 10a). [dotnet] · `lib/core/bluetooth/ftms_parser.dart:22-24`
- **AC-BLE-02-02 a AC-BLE-02-09 · Los 8 paquetes de referencia.** *Dado* cada paquete de la tabla, *cuando* se decodifica, *entonces* la lectura tiene exactamente esos 8 valores (el split entre paréntesis es su texto). [dotnet] · `ftms_parser.dart:26-140`

  | AC | Bytes | spm | paladas | m | split | W | kcal | bpm | s | Qué prueba |
  |---|---|---|---|---|---|---|---|---|---|---|
  | AC-BLE-02-02 | `2C 00 30 2A 01 E2 01 00 85 00 96 00` | 24.0 | 298 | 482 | 133 (`02:13`) | 150 | 0 | 0 | 0 | banderas `0x002C`: ritmo y paladas (bit 0 apagado), distancia de 3 bytes, split y potencia |
  | AC-BLE-02-03 | `05 00 10 27 00` | 0.0 | 0 | 10000 | 0 (`--:--`) | 0 | 0 | 0 | 0 | bit 0 ("More Data") prendido: no vienen ritmo ni paladas |
  | AC-BLE-02-04 | `11 00 78 00` | 0.0 | 0 | 0 | 120 (`02:00`) | 0 | 0 | 0 | 0 | sin split instantáneo, el promedio pasa a ser el split |
  | AC-BLE-02-05 | `19 00 00 00 7D 00` | 0.0 | 0 | 0 | 125 (`02:05`) | 0 | 0 | 0 | 0 | split instantáneo 0: se usa el promedio |
  | AC-BLE-02-06 | `19 00 85 00 7D 00` | 0.0 | 0 | 0 | 133 (`02:13`) | 0 | 0 | 0 | 0 | split instantáneo distinto de 0: el promedio se ignora |
  | AC-BLE-02-07 | `01 0B 2D 00 00 00 00 8C 2C 01` | 0.0 | 0 | 0 | 0 (`--:--`) | 0 | 45 | 140 | 300 (`05:00`) | energía total (se saltean kcal/h y kcal/min), pulso y tiempo |
  | AC-BLE-02-08 | `00 00 30` | 24.0 | 0 | 0 | 0 (`--:--`) | 0 | 0 | 0 | 0 | faltan los bytes de las paladas: quedan en 0 y el paquete se procesa igual |
  | AC-BLE-02-09 | `2C` (1 byte) | — | — | — | — | — | — | — | — | menos de 2 bytes: se descarta, **no hay lectura** |

- **AC-BLE-02-10 · Resolución del ritmo.** *Entonces* el ritmo es el byte crudo × 0,5: `30` (48) → 24.0 spm; `F6` (246) → 123.0 spm. [dotnet] · `ftms_parser.dart:46`
- **AC-BLE-02-11 · Potencia con signo y potencia promedio.** *Dado* `21 00 F6 FF`, *entonces* la potencia es **−10 W** (y se muestra `-10`). *Dado* `61 00 00 00 64 00` (instantánea 0 y promedio 100), *entonces* la potencia es 100 W: el promedio se usa **solo** si la instantánea es 0. [dotnet] · `ftms_parser.dart:83-94`
- **AC-BLE-02-12 · Campos que se saltean.** *Entonces*: `07 00 14 10 27 00` → 10000 m (se saltea 1 byte de ritmo promedio, bit 1); `81 01 05 00 2D 00 00 00 00` → 45 kcal (se saltean 2 bytes de resistencia, bit 7); `01 0C 03 2C 01` → 300 s (se saltea 1 byte de equivalente metabólico, bit 10); `01 10 3C 00` → los 8 campos en 0 (el tiempo restante, bit 12, se ignora: lo calcula la rutina). [dotnet] · `ftms_parser.dart:56-58`, `:97-99`, `:118-120`, `:128-129`
- **AC-BLE-02-13 · Truncados y sobrantes.** *Entonces*: `01 01 2D 00` → 45 kcal (faltan kcal/h y kcal/min y se procesa igual); `2C 00 30 2A 01 E2 01 00 85 00 96 00 FF FF` → lo mismo que AC-BLE-02-02 (los bytes de sobra se ignoran); `00 00` → una lectura con los 8 campos en 0 (2 bytes alcanzan: no se descarta). Ningún paquete raro produce un error. [dotnet] · `ftms_parser.dart:45-126`
- **AC-BLE-02-14 · Valores crudos (Q-38).** *Dado* `01 01 FF FF FF FF FF`, *entonces* las calorías son 65535 (el "no disponible" de FTMS se toma crudo). La potencia negativa también pasa tal cual (AC-BLE-02-11). [dotnet] · `ftms_parser.dart:102-109`
- **AC-BLE-02-15 · Un campo ausente vale 0.** *Dado* que se decodificó el paquete de AC-BLE-02-02 (482 m, 150 W, 24,0 spm), *cuando* llega `11 00 78 00`, *entonces* la nueva lectura tiene distancia 0, potencia 0 y ritmo 0.0: **no conserva** el valor anterior. (El saneamiento de esos saltos es de las It. 4a y 5.) [dotnet] · `ftms_parser.dart:33-40`
- **AC-BLE-02-16 · Paquete vacío.** *Dado* una notificación de 0 bytes, *entonces* no genera lectura ni error. [dotnet] · `lib/core/bluetooth/ble_service.dart:292`; `ftms_parser.dart:27`

### 4.3 F-BLE-03 — Filtro de candidatos por servicio anunciado

- **AC-BLE-03-01 · Plataformas.** *Dado* un resultado de búsqueda, *cuando* la app corre en **Windows**, *entonces* descarta los equipos que no anuncian el servicio buscado. *Cuando* corre en **Android, iOS o macOS**, *entonces* no filtra por su cuenta y confía en el filtro del sistema (en iOS y macOS el servicio puede viajar en los "overflow UUIDs" y no figurar en la lista anunciada). Linux queda fuera (Q-50). [dotnet] para la regla; plataforma real [Device] → It. 10a/11 (S-1 opcional), [Apple] → It. 10b · `lib/core/bluetooth/scan_filter.dart:12`, `:19-20`; usos en `ble_service.dart:106` y `heart_rate_service.dart:136`
- **AC-BLE-03-02 · Con filtro en la app (= LT-08).** *Dado* que se buscan pulsómetros (`0000180d-0000-1000-8000-00805f9b34fb`) con filtro en la app, *entonces*: sin servicios anunciados `[]` → rechazado; `[febe]` → rechazado; `[180d]` → **aceptado** (el UUID de 16 bits equivale al de 128); `[febe, 0000180d-0000-1000-8000-00805f9b34fb]` → aceptado (alcanza con que uno sea el buscado). [dotnet] · `test/scan_filter_test.dart:24`
- **AC-BLE-03-03 · Sin filtro en la app (= LT-09).** *Dado* que el sistema ya filtró, *entonces* `[]` → aceptado y `[febe]` → aceptado. [dotnet] · `test/scan_filter_test.dart:32`
- **AC-BLE-03-04 · Equivalencia de UUID.** *Entonces* `180d`, `180D`, `0000180d`, `0000180d-0000-1000-8000-00805f9b34fb` y `0000180D-0000-1000-8000-00805F9B34FB` son el mismo servicio, y lo mismo pasa con `1826` y `00001826-0000-1000-8000-00805f9b34fb`: se comparan en su forma de 128 bits, sin distinguir mayúsculas; las formas de 16 y 32 bits se completan con la base Bluetooth `-0000-1000-8000-00805f9b34fb`. [dotnet] · `flutter_blue_plus_platform_interface` 9.0.3, `lib/src/guid.dart` (`str128` y `==`)
- **AC-BLE-03-05 · Misma regla para el remo.** *Dado* que se buscan remos (FTMS) con filtro en la app, *entonces* `[1826]` → aceptado, `[180d]` → rechazado (un pulsómetro no aparece en la lista del remo en Windows) y `[180d, 1826]` → aceptado. [dotnet] · `scan_filter.dart:14-20`; `ble_service.dart:106`

### 4.4 F-BLE-14 — Física del remo simulado

- **AC-BLE-14-01 · Valores iniciales.** *Dado* un simulador nuevo, *entonces* el objetivo es 150 W y 24 spm, "Remando" está encendido y el pulso interno arranca en 70. [dotnet] · `lib/core/dev/rowing_simulator.dart:20`, `:24`, `:29`, `:37`
- **AC-BLE-14-02 · Topes (incluye LT-05).** *Entonces* la potencia objetivo queda en 30–500 W y el ritmo en 14–40 spm, recortando al límite: 9999 W → 500 y 1 spm → 14 (LT-05); 0 W → 30 y 99 spm → 40. [dotnet] · `rowing_simulator.dart:7-10`, `:22`, `:26`
- **AC-BLE-14-03 · Split de Concept2 (incluye LT-01 y LT-02).** *Entonces* el split exacto es `500 · (2,80 / W)^(1/3)` s y el que se emite es su redondeo: [dotnet] · `rowing_simulator.dart:77-78`, `:49`

  | W | 30 | 100 | 150 | 180 | 203 | 280 | 300 | 500 |
  |---|---|---|---|---|---|---|---|---|
  | split exacto (s) | 226,8031 | 151,8294 | 132,6352 | 124,8145 | 119,9105 | 107,7217 | 105,2727 | 88,7904 |
  | split emitido | 227 (`03:47`) | 152 (`02:32`) | 133 (`02:13`) | 125 (`02:05`) | 120 (`02:00`) | 108 (`01:48`) | 105 (`01:45`) | 89 (`01:29`) |
  | m/s (500 / exacto) | 2,2046 | 3,2932 | 3,7697 | 4,0059 | 4,1698 | 4,6416 | 4,7496 | 5,6312 |

  LT-01: el split de 300 W es menor que el de 150 W. LT-02: el de 203 W es 120 ± 0,5. El split exacto se compara con tolerancia de 1e-9 (puede diferir en el último bit entre plataformas, ADR 012 §3).
- **AC-BLE-14-04 · Calorías de Concept2.** *Entonces* las kcal por segundo son `(W · 4 · 0,8604 + 300) / 3600`, es decir, por hora: 30 W → 403,248; 100 → 644,16; 150 → 816,24; 180 → 919,488; 203 → 998,6448; 280 → 1263,648; 300 → 1332,48; 500 → 2020,8. [dotnet] · `rowing_simulator.dart:81-82`
- **AC-BLE-14-05 · Remando sin ruido (incluye LT-03).** *Dado* un simulador sin ruido a 150 W y 24 spm, *cuando* avanza tick a tick (1 tick = 1 s), *entonces* las lecturas son: [dotnet] · `rowing_simulator.dart:45-69`

  | Tick | spm | paladas | m | split | W | kcal | bpm | s |
  |---|---|---|---|---|---|---|---|---|
  | 1 | 24.0 | 0 | 3 | 133 | 150 | 0 | 77 | 1 |
  | 60 | 24.0 | 24 | 226 | 133 | 150 | 13 | 142 | 60 |
  | 120 | 24.0 | 48 | 452 | 133 | 150 | 27 | 142 | 120 |
  | 600 | 24.0 | 240 | 2261 | 133 | 150 | 136 | 142 | 600 |

  El pulso de los 12 primeros ticks es `77, 84, 90, 95, 100, 104, 108, 111, 114, 117, 120, 122`. LT-03 exige al tick 60: 150 W, 24 spm, split 133, 60 s, 24 paladas, 226 m, 13 kcal y pulso > 70; esta spec además fija 142 bpm.
- **AC-BLE-14-06 · Dejar de remar (incluye LT-04).** *Dado* 11 ticks remando sin ruido con los valores iniciales (lectura: 24.0 spm, 4 paladas, 41 m, split 133, 150 W, 2 kcal, 120 bpm, 11 s), *cuando* se apaga "Remando" y pasa un tick, *entonces* la lectura es 0.0 spm, 0 W, split 0 (`--:--`), 4 paladas, 41 m, 2 kcal, 11 s y **115 bpm** (el pulso baja hacia 70). *Cuando* se vuelve a remar, el tick siguiente da 45 m, 12 s y 118 bpm. LT-04 exige: ritmo, vatios y split en 0; distancia, paladas, tiempo y kcal iguales al tick anterior; pulso menor. [dotnet] · `rowing_simulator.dart:41-57`
- **AC-BLE-14-07 · Pulso.** *Entonces* el objetivo de pulso es `mín(190; 90 + 0,35 · W)` remando (W = la potencia de ese tick, con ruido) y 70 sin remar, y en cada tick el pulso avanza el 10 % de la distancia al objetivo. Valores estables: 30 W → 100,5; 100 → 125; 150 → 142,5; 180 → 153; 203 → 161,05; 280 → 188; 300 y 500 → 190 (tope). [dotnet] · `rowing_simulator.dart:56-57`
- **AC-BLE-14-08 · Pulso expuesto (= LT-07).** *Dado* un simulador sin ruido, *cuando* da 1 tick, *entonces* el pulso que expone es 77, igual al de la lectura y mayor que 70 (es el que va a emitir el pulsómetro simulado de la It. 2). [dotnet] · `rowing_simulator.dart:32`; `test/rowing_simulator_test.dart:70`
- **AC-BLE-14-09 · Ruido (incluye LT-06).** *Entonces*, remando, W = `máx(1; redondeo(objetivo · (1 + u · 0,05)))` y spm = `máx(1; objetivo + u · 1)`, con `u` uniforme en [−1; 1] e independiente para cada uno. Con objetivo 200 W, 100 ticks seguidos quedan todos en [190; 210] (LT-06); con 24 spm, los valores quedan en [23.0; 25.0]. Sin ruido, lo emitido es exactamente el objetivo. La fuente de azar se puede reemplazar en los tests (AC-BASE-07) y sus valores no tienen que coincidir con los de Dart: sí las fórmulas y los rangos. [dotnet] · `rowing_simulator.dart:46-47`, `:73-74`
- **AC-BLE-14-10 · Redondeos de la salida.** *Entonces*: el ritmo sale con 1 decimal redondeado **como Dart** (un ritmo interno de 24,25 sale 24.3 y uno de 23,95 sale 23.9, AC-BASE-01); paladas, distancia y kcal son la parte entera con una tolerancia de 1e-6 (a 24 spm, 60 ticks acumulan 23,99999999999998 paladas, que tienen que dar **24**, no 23); el pulso se redondea al entero más cercano (la mitad se aleja del cero); el tiempo son los segundos remados. [dotnet] · `rowing_simulator.dart:59-69`

### 4.5 F-HRM-01 — Paquete Heart Rate Measurement `0x2A37`

- **AC-HRM-01-01 · Identificadores (= LT-15).** *Entonces* el servicio es `0000180d-0000-1000-8000-00805f9b34fb` y la característica de medición es `00002a37-0000-1000-8000-00805f9b34fb`. [dotnet] · `lib/core/bluetooth/heart_rate_parser.dart:21-22`
- **AC-HRM-01-02 · 8 bits (= LT-10).** *Dado* `[0x00, 72]`, *entonces* el pulso es 72 y el contacto es "el sensor no informa". [dotnet] · `heart_rate_parser.dart:27-31`
- **AC-HRM-01-03 · 16 bits little-endian (= LT-11).** *Dado* `[0x01, 0x2C, 0x01]`, *entonces* el pulso es 300. [dotnet] · `heart_rate_parser.dart:31`
- **AC-HRM-01-04 · Contacto (= LT-12).** *Entonces* el contacto son los bits 1-2: `[0x02, 70]` → no informa (pulso 70); `[0x04, 0]` → **sin contacto** (pulso 0); `[0x06, 65]` → **con contacto** y pulso 65. [dotnet] · `heart_rate_parser.dart:33-38`
- **AC-HRM-01-05 · Energía y RR (= LT-13).** *Dado* `[0x18, 80, 0x10, 0x00, 0x00, 0x04]`, *entonces* el pulso es 80: la energía gastada (bit 3) y los intervalos RR (bit 4) no se leen ni se validan. [dotnet]
- **AC-HRM-01-06 · Inválidos (= LT-14).** *Entonces* `[]`, `[0x00]` y `[0x01, 72]` no dan lectura: el formato de 16 bits con 2 bytes se descarta, no "cae" a 8 bits. [dotnet] · `heart_rate_parser.dart:25`, `:28`
- **AC-HRM-01-07 · Sin validación de rango (Q-48).** *Entonces* `[0x00, 0]` → 0, `[0x00, 255]` → 255 y `[0x01, 0xFF, 0xFF]` → 65535. [dotnet]
- **AC-HRM-01-08 · Bits reservados y bytes de sobra.** *Entonces* `[0xE0, 90, 1, 2, 3]` → 90, no informa contacto; `[0x01, 72, 0]` → 72. [dotnet]
- **AC-HRM-01-09 · 16 bits con contacto.** *Dado* `[0x07, 0x2C, 0x01]`, *entonces* el pulso es 300 con contacto. [dotnet]

### 4.6 F-RUT-01 (+ F-APP-07) — Tipos de paso

- **AC-RUT-01-01 · Identidad y orden.** *Entonces* hay exactamente 4 tipos, en este orden (el del selector del diálogo de paso): `warmup`, `work`, `rest`, `cooldown`. Ese nombre en minúsculas es el que se guarda. [dotnet] · `lib/core/models/interval_step.dart:1`, `:115`
- **AC-RUT-01-02 · Nombres.** *Entonces* el nombre visible sale del catálogo: [dotnet] · `lib/shared/theme.dart:42-53`; `lib/l10n/app_es.arb:117-120`, `app_en.arb:117-120`

  | Tipo | Clave | es | en |
  |---|---|---|---|
  | `warmup` | `stepTypeWarmup` | `Calentamiento` | `Warmup` |
  | `work` | `stepTypeWork` | `Trabajo` | `Work` |
  | `rest` | `stepTypeRest` | `Descanso` | `Rest` |
  | `cooldown` | `stepTypeCooldown` | `Enfriamiento` | `Cooldown` |

- **AC-RUT-01-03 · Colores.** *Entonces* `work` = `#EF476F`, `rest` = `#06D6A0`, `warmup` = `#FFD166`, `cooldown` = `#118AB2`, y cualquier otro valor (texto vacío, `sprint`, un punto de telemetría sin tipo) = `#607D8B`. Salen de la fuente única de colores (AC-BASE-04). [dotnet] · `theme.dart:56-69`
- **AC-RUT-01-04 · Íconos (dato).** *Entonces* el ícono de cada tipo es: `warmup` llama (`whatshot`), `work` pesa (`fitness_center`), `rest` círculo de pausa sin relleno (`pause_circle_outline`), `cooldown` copo de nieve (`ac_unit`). [dotnet] el dato; en el editor [Editor] NO VERIFICABLE → It. 3 · `lib/features/routines/routine_editor_screen.dart:447-458`
- **AC-RUT-01-05 · Tipo desconocido.** *Dado* un nombre de tipo que no es ninguno de los 4, *cuando* se convierte a tipo de paso, *entonces* es un error: no cae en un tipo por defecto. Su efecto visible (esa rutina no carga) se verifica en la It. 3 (F-RUT-08). [dotnet] · `interval_step.dart:133`

### 4.7 F-RUT-02 — Modelo de paso

- **AC-RUT-02-01 · Atributos.** *Entonces* un paso tiene rutina, orden (0-based), tipo (obligatorios) y, opcionales: duración (s), distancia (m), vatios mínimo y máximo, SPM objetivo, split objetivo (s/500 m), identificador de serie, repeticiones de la serie y progresiones de SPM, vatios y split por repetición. Todos los valores numéricos son enteros. [dotnet] · `interval_step.dart:21-62`
- **AC-RUT-02-02 · Meta.** *Entonces*, si hay duración, el paso es "por tiempo" aunque también tenga distancia (duración 90 s y 500 m → etiqueta `1m 30s`); sin duración y con distancia es "por distancia"; sin ninguna de las dos no tiene meta (etiqueta `?`). Que un paso sin meta nunca termine solo y que uno de 0 s termine en el primer segundo se verifica en la It. 4a (F-WKT-03). [dotnet] · `interval_step.dart:67-77`
- **AC-RUT-02-03 · Copia con cambios.** *Dado* un paso con SPM 20, *cuando* se copia pidiendo SPM "vacío", *entonces* sigue en 20: la copia con cambios nunca borra un valor. [dotnet] · `interval_step.dart:147-180`
- **AC-RUT-02-04 · Sin validación (Q-25).** *Entonces* se aceptan y se muestran tal cual el 0, los negativos y mínimo > máximo: vatios mínimo 0 → `>0W`; vatios de −10 a 5 → `-10-5W`; SPM 0 → `0 spm`. [dotnet] · `interval_step.dart:79-91`

### 4.8 F-RUT-03 — Etiqueta de duración del paso

Mismo resultado en es y en (Q-01, Q-07): el texto sale de las claves `stepDuration*` de §4.17.2 con el valor idéntico al legacy.

- **AC-RUT-03-01 · Por tiempo (caso 9).** *Entonces*, con `m` = segundos ÷ 60 (división entera) y `s` = resto: `m > 0` y `s > 0` → `{m}m {s}s`; `m > 0` y `s = 0` → `{m}m`; `m = 0` → `{s}s`. No hay horas. [dotnet] · `interval_step.dart:67-74`

  | s | 0 | 45 | 59 | 60 | 61 | 90 | 120 | 3600 | 3725 | 7200 |
  |---|---|---|---|---|---|---|---|---|---|---|
  | etiqueta | `0s` | `45s` | `59s` | `1m` | `1m 1s` | `1m 30s` | `2m` | `60m` | `62m 5s` | `120m` |

- **AC-RUT-03-02 · Por distancia (caso 9).** *Entonces* `{metros}m` sin separador de miles: 500 → `500m`; 2000 → `2000m`; 10000 → `10000m`. [dotnet] · `interval_step.dart:75`
- **AC-RUT-03-03 · Sin meta y con las dos (caso 9).** *Entonces* sin duración ni distancia → `?`; con duración 90 s y distancia 500 m → `1m 30s`. [dotnet] · `interval_step.dart:76`
- **AC-RUT-03-04 · Negativos.** *Entonces*, como no se bloquean y la división entera trunca y el resto nunca es negativo (reglas de Dart): −30 s → `30s`; −60 s → `0s`; −90 s → `30s`. [dotnet] · `interval_step.dart:69-73`
- **AC-RUT-03-05 · Mismo texto en los dos idiomas.** *Dado* el catálogo en es y *dado* el catálogo en en, *cuando* se arman las etiquetas de AC-RUT-03-01 a -04, *entonces* dan el mismo texto. [dotnet]

### 4.9 F-RUT-04 — Etiqueta de objetivos del paso

Las partes se arman en este orden y se unen con `␣·␣` (formato fijo, no se traduce): vatios, SPM, split, progresiones. Mismo resultado en es y en (Q-01, Q-10: el `📈` se mantiene en el texto); las claves `stepTarget*` y `stepProgression*` están en §4.17.2.

- **AC-RUT-04-01 · Objetivos (caso 10).** *Entonces*: [dotnet] · `interval_step.dart:79-91`, `:108`

  | Objetivos | Etiqueta |
  |---|---|
  | ninguno | (vacío) |
  | vatios mín. 150 | `>150W` |
  | vatios 120–180 | `120-180W` |
  | solo vatios máx. 180 | (vacío) |
  | SPM 22 | `22 spm` |
  | split 59 / 125 / 600 / 6000 | `00:59/500m` / `02:05/500m` / `10:00/500m` / `100:00/500m` |
  | 120–180 W, SPM 22, split 125 | `120-180W · 22 spm · 02:05/500m` |

- **AC-RUT-04-02 · Progresiones (caso 10).** *Entonces* se listan las que existen y son distintas de 0, en orden SPM, vatios, split, unidas por `,␣` y precedidas por `📈␣`; el `+` va solo si es positiva y la negativa trae su `-`: prog. SPM 2 → `📈 SPM +2/rep`; SPM 20 con prog. −1 → `20 spm · 📈 SPM -1/rep`; prog. W 10 → `📈 W +10/rep`; prog. split 3 → `📈 Split +3s/rep`; progresiones 0, 0 y 0 → (vacío). La progresión se muestra aunque falte el objetivo que modifica: solo vatios máx. 180 con prog. W 10 → `📈 W +10/rep`. [dotnet] · `interval_step.dart:94-106`
- **AC-RUT-04-03 · Línea completa (caso 10).** *Dado* 120–180 W, SPM 22, split 125 y progresiones SPM 2, W 5 y split −2, *entonces* la etiqueta es `120-180W · 22 spm · 02:05/500m · 📈 SPM +2/rep, W +5/rep, Split -2s/rep`. [dotnet]
- **AC-RUT-04-04 · Split sin piso.** *Entonces*, para objetivos que dejó una progresión negativa: split 0 → `00:00/500m`; −5 → `00:55/500m`; −65 → `-1:55/500m`. [dotnet] · `interval_step.dart:87-91`
- **AC-RUT-04-05 · El paso expandido conserva la progresión.** *Dado* vatios 140–160 con prog. +10 en una serie ×2, *entonces* el paso de la repetición 2 dice `150-170W · 📈 W +10/rep`. [dotnet] · `lib/core/models/routine.dart:50-63`
- **AC-RUT-04-06 · Subtítulo del editor (regla).** *Entonces*, si la etiqueta de objetivos está vacía, el subtítulo es solo la duración; si no, `{duración}␣␣·␣␣{objetivos}` (dos espacios a cada lado del punto medio), por ejemplo `5m␣␣·␣␣18 spm · 📈 SPM +2/rep`. [dotnet] la regla; en el editor [Editor] NO VERIFICABLE → It. 3 · `routine_editor_screen.dart:345-349`

### 4.10 F-RUT-05 — Modelo de rutina y resumen

- **AC-RUT-05-01 · Atributos.** *Entonces* una rutina tiene nombre (obligatorio), descripción (por defecto vacía), fecha de creación y una lista de pasos que es la **plantilla** sin expandir (por defecto, ninguno). [dotnet] · `routine.dart:8-21`
- **AC-RUT-05-02 · Resumen (caso 11).** *Entonces* se cuentan los pasos de la plantilla (no de la rutina expandida): si no hay pasos de trabajo, el total de pasos (`routineSummarySteps`); si hay, pasos de trabajo y pasos de descanso (`routineSummaryWorkRest`). "Series" cuenta **pasos de trabajo**: una serie ×3 de trabajo + descanso cuenta 1 y 1. La columna "Legacy" es la referencia del oráculo y prueba que las reglas de conteo no cambiaron; lo que se muestra son las columnas es y en (Q-01 + Q-02). [dotnet] · `routine.dart:100-105`

  | Plantilla | Legacy (oráculo, igual en es y en) | es | en |
  |---|---|---|---|
  | calentamiento | `1 pasos` | `1 paso` | `1 step` |
  | calentamiento + enfriamiento | `2 pasos` | `2 pasos` | `2 steps` |
  | descanso + descanso | `2 pasos` | `2 pasos` | `2 steps` |
  | trabajo | `1 series · 0 descansos` | `1 serie · 0 descansos` | `1 interval · 0 rests` |
  | calentamiento + serie ×3 (trabajo, descanso) + enfriamiento | `1 series · 1 descansos` | `1 serie · 1 descanso` | `1 interval · 1 rest` |
  | 2 trabajos sueltos + 1 descanso | `2 series · 1 descansos` | `2 series · 1 descanso` | `2 intervals · 1 rest` |
  | 1 trabajo + 2 descansos | `1 series · 2 descansos` | `1 serie · 2 descansos` | `1 interval · 2 rests` |
  | 4 trabajos y 3 descansos sueltos | `4 series · 3 descansos` | `4 series · 3 descansos` | `4 intervals · 3 rests` |
  | sin pasos | `0 pasos` | `0 pasos` | `0 steps` |

- **AC-RUT-05-03 · Resumen en el diálogo de inicio.** *Dado* la rutina "calentamiento + serie ×3 (trabajo, descanso) + enfriamiento", *cuando* se arma `workoutStartContent`, *entonces* el texto es `1 serie · 1 descanso⏎⏎¿Iniciar el entrenamiento?` en es y `1 interval · 1 rest⏎⏎Start the workout?` en en. [dotnet]; el diálogo [Editor] NO VERIFICABLE → It. 4a · `lib/features/workout/workout_screen.dart:152`
- **AC-RUT-05-04 · Mismo conteo que el legacy.** *Dado* cada plantilla de AC-RUT-05-02, *entonces* los números que entran en el texto nuevo son los mismos del texto legacy. [dotnet]

### 4.11 F-RUT-06 — Expansión de series con progresiones

Plantilla **T1** (la del test legacy): calentamiento 300 s suelto · serie "a" ×3 [trabajo 500 m, enfriamiento 60 s] · enfriamiento 300 s suelto.

- **AC-RUT-06-01 · T1.** *Entonces* T1 se expande en **8 pasos**: calentamiento 300 s, trabajo 500 m, enfriamiento 60 s, trabajo 500 m, enfriamiento 60 s, trabajo 500 m, enfriamiento 60 s, enfriamiento 300 s. El último paso de la serie se incluye en todas las repeticiones, también en la última. LT-16 verifica el largo 8. [dotnet] · `routine.dart:29-70`
- **AC-RUT-06-02 · Progresión de SPM (caso 1).** *Dado* una serie ×3 [trabajo 300 s, 18 SPM, prog. +2; descanso 120 s], *entonces* hay 6 pasos: trabajo 18, descanso, trabajo 20, descanso, trabajo 22, descanso. Los descansos no cambian y el último paso es un descanso. [dotnet] · `routine.dart:51-53`
- **AC-RUT-06-03 · Vatios en rango (caso 2).** *Dado* vatios 140–160 con prog. +10, ×3, *entonces* 140–160, 150–170, 160–180 (la progresión se aplica a los dos extremos). [dotnet] · `routine.dart:54-59`
- **AC-RUT-06-04 · Solo mínimo (caso 3).** *Dado* vatios mín. 150 (sin máx.), 22 SPM, prog. W +10 y SPM +2, ×3, *entonces* 150 W/22, 160 W/24, 170 W/26; el máximo sigue vacío. [dotnet]
- **AC-RUT-06-05 · Split (caso 4).** *Dado* split 120 con prog. −2, ×3, *entonces* 120, 118, 116 (`02:00`, `01:58`, `01:56`). [dotnet] · `routine.dart:60-62`
- **AC-RUT-06-06 · Progresión sin objetivo (caso 5).** *Dado* SPM vacío con prog. +2, ×3, *entonces* el SPM sigue vacío en las 3 repeticiones. [dotnet]
- **AC-RUT-06-07 · Paso suelto con progresión (caso 6).** *Dado* un paso suelto con SPM 20 y prog. +2 (dato cargado a mano), *entonces* la rutina expandida tiene 1 paso con SPM 20: los pasos sueltos no se progresan. [dotnet] · `routine.dart:34-36`
- **AC-RUT-06-08 · Repeticiones vacías o ≤ 0 (caso 7).** *Entonces*: una serie sin repeticiones cuenta ×1 (trabajo 60 s → 1 paso); [calentamiento 60 s, serie ×0 [trabajo 60 s], enfriamiento 60 s] → 2 pasos (calentamiento y enfriamiento); una serie ×−1 → 0 pasos; una rutina que es solo una serie ×0 → 0 pasos (el motor de la It. 4a no puede arrancarla). [dotnet] · `routine.dart:40`, `:47`
- **AC-RUT-06-09 · Series contiguas (caso 8).** *Dado* "a" ×2 [trabajo 60 s] seguida de "b" ×3 [trabajo 30 s], *entonces* 5 pasos: 60, 60, 30, 30, 30. [dotnet] · `routine.dart:42-45`
- **AC-RUT-06-10 · Repeticiones del primer paso.** *Dado* una serie "a" con trabajo 60 s (×2) y descanso 30 s (×5), *entonces* manda el ×2 del primer paso: 4 pasos. [dotnet] · `routine.dart:40`
- **AC-RUT-06-11 · Mismo identificador no consecutivo.** *Dado* "a" ×2 [trabajo 60 s], un descanso 30 s suelto y otra vez "a" ×2 [trabajo 60 s], *entonces* son dos bloques independientes: 5 pasos (60, 60, 30, 60, 60). [dotnet]
- **AC-RUT-06-12 · Sin piso.** *Dado* SPM 4 con prog. −3, ×3, *entonces* 4, 1, −2. [dotnet]
- **AC-RUT-06-13 · Lo demás se conserva.** *Entonces* en cada repetición se conservan tipo, duración, distancia, identificador de serie, repeticiones y las progresiones mismas (por eso vale AC-RUT-04-05). [dotnet] · `routine.dart:50-63`

### 4.12 F-RUT-07 — Posición de cada paso en su serie

Cada posición es (serie, repetición desde 1, total de repeticiones). Un paso suelto es (sin serie, 1, 1).

- **AC-RUT-07-01 · T1 (= LT-16).** *Entonces* la lista de posiciones de T1 tiene el mismo largo que la rutina expandida, 8, y: [0] = (sin serie, 1, 1); [1] = (a, 1, 3); [2] = (a, 1, 3); [3] = (a, 2, 3); [6] = (a, 3, 3); [7] = (sin serie, 1, 1). [dotnet] · `routine.dart:74-97`; `test/routine_positions_test.dart:6`
- **AC-RUT-07-02 · T1 completo.** *Entonces* además [4] = (a, 2, 3) y [5] = (a, 3, 3) (el test legacy no los verifica). [dotnet]
- **AC-RUT-07-03 · Repeticiones vacías o 0 (caso 7).** *Entonces* la serie sin repeticiones da (a, 1, 1); [calentamiento, serie ×0, enfriamiento] da 2 posiciones (sin serie, 1, 1). [dotnet]
- **AC-RUT-07-04 · Series contiguas (caso 8).** *Entonces* (a, 1, 2), (a, 2, 2), (b, 1, 3), (b, 2, 3), (b, 3, 3). [dotnet]
- **AC-RUT-07-05 · Siempre alineadas.** *Dado* cada plantilla de §4.11, *entonces* la lista de posiciones y la rutina expandida tienen el mismo largo y el mismo orden. [dotnet]

### 4.13 F-RUT-16 — Sesión de entrenamiento (registro)

- **AC-RUT-16-01 · Atributos.** *Entonces* una sesión tiene: rutina (id y nombre, los dos vacíos en entrenamiento libre), inicio (obligatorio), fin (vacío mientras está en curso), distancia total (m), tiempo total (s), "potencia promedio" (W), "SPM promedio" (decimal), calorías totales (los cinco en 0 por defecto) e id de actividad de Strava (vacío si no se subió). [dotnet] · `lib/core/models/workout_session.dart:2-27`
- **AC-RUT-16-02 · "Promedio" = P99 (Q-14).** *Entonces* los campos de potencia y SPM "promedio" guardan el **P99** de F-RUT-18 (potencia redondeada a entero, SPM decimal) y distancia, tiempo y calorías son los máximos de la telemetría. El cierre de la sesión que los carga es de la It. 4a (F-WKT-06). [dotnet] · DC-07; `lib/features/workout/workout_provider.dart:276-292`
- **AC-RUT-16-03 · Duración.** *Entonces* la duración se formatea como en AC-BLE-01-03: 59 s → `00:59`; 3661 s → `1:01:01`. [dotnet] · `workout_session.dart:29-37`
- **AC-RUT-16-04 · Fechas.** *Dado* un inicio o fin, *cuando* se guarda, *entonces* queda en UTC, ISO-8601 con `Z`, con milisegundos siempre y microsegundos **solo si no son 0**: 09:37:38,628537 → `2026-10-27T09:37:38.628537Z`; 09:37:38,628 → `2026-10-27T09:37:38.628Z`; 09:37:38 → `2026-10-27T09:37:38.000Z`. *Cuando* se lee, *entonces* pasa a la hora local: con UTC−3, `2026-10-27T09:37:38.628Z` es el 27/10/2026 a las 06:37:38,628. [dotnet] · `workout_session.dart:43-44`, `:57-60`
- **AC-RUT-16-05 · Lectura tolerante.** *Dado* un registro que solo trae la fecha de inicio, *cuando* se lee, *entonces* distancia 0, tiempo 0, potencia 0, SPM 0.0, calorías 0, sin rutina y sin id de Strava. [dotnet] · `workout_session.dart:53-67`

### 4.14 F-RUT-17 — Punto de telemetría

- **AC-RUT-17-01 · Atributos.** *Entonces* un punto tiene: sesión y tiempo transcurrido (s), los dos **obligatorios y sin valor por defecto** (el tiempo es el **del monitor**, `workout_provider.dart:162`, `:261`); SPM (decimal), paladas, distancia, split, potencia, calorías y pulso, los siete en **0 por defecto**; índice y tipo del paso, **vacíos por defecto** (y siempre vacíos en entrenamiento libre); id vacío hasta que el punto se guarda. El tipo se guarda como `warmup`, `work`, `rest` o `cooldown`. [dotnet] · `workout_session.dart:140-182` (obligatorios `:156-157`, en 0 `:158-164`, vacíos `:154`, `:165-166`)
- **AC-RUT-17-02 · Índice de la rutina expandida.** *Entonces* el índice del paso se refiere a la rutina **expandida**: en T1, un punto del segundo trabajo tiene índice 3 y tipo `work` (no el índice 1 de la plantilla). [dotnet] · DC-48 y F-RUT-20 (It. 9)

### 4.15 F-RUT-18 (+ F-HIS-04) — Estadísticas de sesión

- **AC-RUT-18-01 · Sin puntos (incluye LT-19).** *Dado* una lista vacía, *entonces* todo vale 0, no tiene pulso, el split se ve `--:--` y la duración `00:00`. [dotnet] · `workout_session.dart:118-119`
- **AC-RUT-18-02 · Índice del P99 (caso 12).** *Entonces* el P99 toma solo los valores **> 0**, los ordena de menor a mayor y elige el de índice `piso((n − 1) · 0,99)` (0-based, en doble precisión); sin valores, 0. Índices: n = 1 → 0; 2 → 0; 3 → 1; 10 → 8; 100 → 98; 101 → 99; 102 → 99; 361 → 356; 1000 → 989. Con vatios 1, 2, …, 361 → **357**. [dotnet] · `workout_session.dart:111-116`
- **AC-RUT-18-03 · Ejemplos (caso 12).** *Entonces*: vatios [0, 100, 200, 300] → **200**; split [130, 0, 125, 140] → **130** (`02:10`, casi el más lento); vatios [200, 150] (n = 2) → **150**, el menor. [dotnet] · `workout_session.dart:121-129`
- **AC-RUT-18-04 · Decimales.** *Entonces* el P99 de vatios y el de split son enteros y el de SPM es decimal **sin redondear**: SPM [24.5, 23.0, 25.5] → 24.5. Con 100 puntos de vatios 2, 4, …, 200, split 100, 101, …, 199 y SPM `20 + 0,1 · i` (i = 0…99) → 198 W, split 198 (`03:18`) y 29.8 spm (el SPM se compara con tolerancia de 1e-9). [dotnet] · `workout_session.dart:127-129`
- **AC-RUT-18-05 · Totales (caso 13).** *Entonces* distancia, tiempo y calorías totales son el **máximo** de cada columna entre todos los puntos (ceros incluidos y en cualquier orden): puntos (tiempo, metros, kcal) = (5, 20, 1), (10, 45, 2), (3, 0, 0), (615, 2000, 120) → 2000 m, 615 s (`10:15`), 120 kcal. [dotnet] · `workout_session.dart:130-132`
- **AC-RUT-18-06 · Pulso (= LT-17).** *Dado* pulsos [0, 120, 150, 0, 141], *entonces* promedio **137** ((120 + 150 + 141) / 3), máximo 150 y "tiene pulso". [dotnet] · `workout_session.dart:133-134`, `:92`; `test/session_stats_test.dart:7`
- **AC-RUT-18-07 · Sin pulso (= LT-18).** *Dado* pulsos [0, 0], *entonces* promedio 0, máximo 0 y no tiene pulso. [dotnet] · `test/session_stats_test.dart:14`
- **AC-RUT-18-08 · Redondeo del pulso (caso 14).** *Entonces* el promedio se redondea al entero más cercano y la mitad sube: [120, 121] → **121** (120,5); [120, 121, 121] → 121; [100, 101, 101, 101] → 101. [dotnet] · `workout_session.dart:133`
- **AC-RUT-18-09 · Formatos (caso 15).** *Entonces* el split del resumen es `--:--` si es ≤ 0 y si no `MM:SS`, y la duración sigue AC-BLE-01-03: un punto con 3661 s → `1:01:01`. [dotnet] · `workout_session.dart:94-109`

### 4.16 F-APP-09 — Idiomas soportados y elección del idioma

- **AC-APP-09-01 · Idiomas.** *Entonces* la app soporta español e inglés. Cualquier variante de español (`es`, `es-AR`, `es-ES`, `es-MX`, `es-419`) usa el español y cualquier variante de inglés usa el inglés. [dotnet] · `lib/main.dart:103-106`; `lib/l10n/app_localizations.dart:1066-1067`
- **AC-APP-09-02 · Elección por la lista del sistema (paridad con Flutter).** *Dado* la lista de idiomas preferidos del sistema, en orden, *cuando* se resuelve el idioma, *entonces* da lo de la tabla. La regla del legacy: (1) una entrada que es exactamente `es` o `en`, sin región, gana en el acto; (2) si la **primera** entrada es una variante de es/en, gana en el acto; (3) si una variante de es/en aparece más abajo, gana, **salvo** que la entrada inmediatamente siguiente sea exactamente `es` o `en`, que entonces tiene prioridad (filas †); (4) si nada coincide, inglés. [dotnet] · `flutter/.../widgets/app.dart:146-238` (`basicLocaleListResolution`, Flutter 3.47.6)

  | Lista del sistema | Idioma |
  |---|---|
  | (vacía) | en |
  | `es-AR` | es |
  | `es-419` | es |
  | `es` | es |
  | `en-US` | en |
  | `pt-BR` | en |
  | `fr-FR, es-AR` | es |
  | `ca-ES, es-ES` | es |
  | `pt-BR, en-GB, es-ES` | en |
  | `en-US, es-AR` | en |
  | `es-AR, en-US` | es |
  | `es-AR, en` | es |
  | `es-AR, es-ES` | es |
  | `fr-FR, es-AR, pt-PT, en` | es |
  | `zh-Hans-CN, ja-JP` | en |
  | † `fr-FR, es-AR, en` | **en** |
  | † `fr-FR, en-GB, es` | **es** |

- **AC-APP-09-03 · Respaldo en inglés.** *Dado* un sistema sin español ni inglés en su lista (portugués, francés, chino…) o sin lista, *entonces* la app se muestra en inglés (el primer idioma soportado y el idioma plantilla del catálogo). [dotnet] · `main.dart:104`; `l10n.yaml:2`
- **AC-APP-09-04 · Sin selector ni memoria (Q-05).** *Entonces* no hay ajuste de idioma dentro de la app y el idioma no se guarda: cada arranque lo vuelve a resolver desde el sistema. [dotnet] la regla; [Editor] NO VERIFICABLE → It. 2
- **AC-APP-09-05 · Cambio con la app abierta.** *Dado* la app en español, *cuando* el sistema pasa a `[en-US]`, *entonces* al resolver de nuevo da inglés y la interfaz cambia sin reiniciar la app. [dotnet] la resolución; lo visible [Editor]/[Device] NO VERIFICABLE → It. 2 · `android/app/src/main/AndroidManifest.xml:30` (`configChanges` con `locale`)
- **AC-APP-09-06 · Lista completa del sistema (QA-P-20).** *Entonces* la app toma la **lista** de idiomas preferidos del sistema (no solo el principal), para que `fr-FR, es-AR` dé español. [Device] Android y Windows NO VERIFICABLE → It. 2; [Apple] → It. 10b
- **AC-APP-09-07 · Textos del sistema (diferencia de producto aceptada, P-06).** *Entonces* todo texto **de la app**, incluidos los de accesibilidad, sale del catálogo y sigue el idioma elegido. Lo que dibuja **el sistema** dentro de la app (el teclado y su menú de copiar, cortar y pegar en un campo de texto) sigue el idioma **del sistema**. Diferencia con el legacy: Flutter dibuja su propia barra de copiar y pegar en el idioma de la app (`main.dart:97-102`, `GlobalMaterialLocalizations`). Con el sistema en es o en, las dos apps coinciden; con el sistema en portugués, el legacy muestra `Copy`/`Paste` y la app nueva `Copiar`/`Colar`. Se acepta como diferencia conocida: igualarla exige una barra de edición propia, que el usuario puede pedir en el gate de la It. 2 (entraría en la It. 3, junto con el único campo de texto del legacy, el del editor de rutinas). [Device] NO VERIFICABLE → It. 3 (primer campo de texto: `routine_editor_screen.dart`, la única pantalla del legacy con `TextField`)

### 4.17 F-APP-10 — Catálogo de textos es/en (174 claves)

- **AC-APP-10-01 · Mismo conjunto en es y en.** *Entonces* el catálogo tiene **174 claves en es y las mismas 174 en en**: las 159 del legacy más las 15 nuevas de §4.17.2. Un chequeo automático falla si una clave existe en un solo idioma. [dotnet] · `lib/l10n/app_es.arb:1-208`, `app_en.arb:1-208`
- **AC-APP-10-02 · 155 textos idénticos.** *Entonces* las 155 claves del legacy que no cambian tienen, en es y en, el texto del `.arb` **carácter por carácter**. En particular: los saltos de línea de `deviceNoResultsHint` (`Asegurate que el rower esté encendido⏎y el Bluetooth activado` / `Make sure the rower is on⏎and Bluetooth is enabled`), `deviceBtOffHint`, `deviceBtDeniedHint` y `editorEmptySteps` (uno cada una); los dos espacios iniciales de `workoutOrPickRoutine` (`␣␣O elegí una rutina:` / `␣␣Or pick a routine:`); `navWorkout` = `Workout` también en es (Q-08); las comillas de `routineDeleteContent` (`¿Eliminar "{name}"?` / `Delete "{name}"?`); los términos sin traducir (`SPM`, `SPLIT 500m`, `Split 500m`, `kcal`, `bpm`, `OK`). El tono del español (voseo y las excepciones con tuteo) no se reescribe (Q-02). [dotnet] · `app_es.arb:5`, `:17`, `:53`, `:92`, `:140`
- **AC-APP-10-03 · Las 4 claves que cambian (Q-02).** *Entonces* `workoutStartTitle`, `workoutStartContent`, `profileSyncResult` y `profileUploadResult` tienen los textos de §4.17.1. [dotnet] · `app_es.arb:66-67`, `:189-190`
- **AC-APP-10-04 · Parámetros.** *Entonces* cada clave tiene los mismos parámetros, con el mismo nombre y tipo, en es y en. Las 7 del legacy: `hrmConnectingTo{name}`, `hrmConnectError{error}`, `workoutStartContent{summary}` y `routineDeleteContent{name}` (texto); `editorStepsCount{count}`, `profileSyncResult{count}` y `profileUploadResult{count}` (entero). Las 15 nuevas, según §4.17.2. Ninguna otra clave tiene parámetros. [dotnet] · metadatos `@` en `app_es.arb:30`, `:43`, `:68`, `:93`, `:133`, `:191`, `:197`
- **AC-APP-10-05 · Textos con parámetros.** *Entonces*: `hrmConnectingTo` con `Polar H10` → `Conectando a Polar H10...` / `Connecting to Polar H10...`; `hrmConnectError` con `timeout` → `Error al conectar: timeout` / `Connection error: timeout`; `routineDeleteContent` con `Escalera SPM` → `¿Eliminar "Escalera SPM"?` / `Delete "Escalera SPM"?`; `editorStepsCount` con 1 → `Pasos (1)` / `Steps (1)` (sin plural, como el legacy) y con 3 → `Pasos (3)` / `Steps (3)`. [dotnet]
- **AC-APP-10-06 · Plurales (Q-02).** *Entonces*, con `one` = exactamente 1 y `other` = todo lo demás, incluido 0: `profileSyncResult` 0 → `0 sesiones sincronizadas` / `0 sessions synced`; 1 → `1 sesión sincronizada` / `1 session synced`; 2 → `2 sesiones sincronizadas` / `2 sessions synced`; `profileUploadResult` 1 → `1 sesión subida` / `1 session uploaded`; 3 → `3 sesiones subidas` / `3 sessions uploaded`. [dotnet]
- **AC-APP-10-07 · Claves nuevas.** *Entonces* las 15 claves de §4.17.2 existen en es y en con esos textos y parámetros, y producen los resultados de AC-RUT-03, AC-RUT-04 y AC-RUT-05. [dotnet]
- **AC-APP-10-08 · No se pierde ninguna clave.** *Entonces* se conservan las 159 del legacy, incluidas las 3 sin uso (`workoutStartTitle`, `historyStatWatts`, `historyStatCalories`) y las 7 que hoy solo usa código muerto (`workoutFinishTitle`, `workoutFinishContent`, `workoutPause`, `workoutResume`, `metricTime`, `metricCalories`, `metricFreeLabel`). La It. 14 decide si se usan o se quitan. [dotnet]
- **AC-APP-10-09 · Clave inexistente.** *Dado* un pedido de una clave que no está en el catálogo, *entonces* los tests lo detectan (quedan en rojo): el usuario nunca ve un nombre de clave ni un texto vacío en su lugar. [dotnet]

#### 4.17.1 Las 4 claves del legacy que cambian (Q-02, [FEATURE_INVENTORY §4.1.1](../../product/FEATURE_INVENTORY.md))

| Clave | es legacy | es nuevo | en legacy | en nuevo |
|---|---|---|---|---|
| `workoutStartTitle` | `Iniciar entrenamiento?` | `¿Iniciar entrenamiento?` | `Start workout?` | sin cambio |
| `workoutStartContent` | `{summary}⏎⏎Iniciar el entrenamiento?` | `{summary}⏎⏎¿Iniciar el entrenamiento?` | `{summary}⏎⏎Start the workout?` | sin cambio |
| `profileSyncResult` | `{count} sesiones sincronizadas` | `{count, plural, one{# sesión sincronizada} other{# sesiones sincronizadas}}` | `{count} sessions synced` | `{count, plural, one{# session synced} other{# sessions synced}}` |
| `profileUploadResult` | `{count} sesiones subidas` | `{count, plural, one{# sesión subida} other{# sesiones subidas}}` | `{count} sessions uploaded` | `{count, plural, one{# session uploaded} other{# sessions uploaded}}` |

El resto de los textos con `¿`/`¡` ya está bien en el legacy (`workoutFinishContent`, `workoutCompleted`, `routineDeleteContent`, `profileDisconnectContent`, `profileAskUploadDialog`).

#### 4.17.2 Las 15 claves nuevas (Q-01 + Q-02, QA-P-04; [routines-models §3.1](../../product/inventory/routines-models.md))

Sintaxis de mensaje de los `.arb`: `{param}` y plural `one`/`other`. El Architect puede partir una clave en dos si el formateador no admite dos plurales en un mensaje, **siempre que el texto final sea el mismo**.

| Clave | es | en | Parámetros | Cuándo |
|---|---|---|---|---|
| `routineSummarySteps` | `{count, plural, one{# paso} other{# pasos}}` | `{count, plural, one{# step} other{# steps}}` | `count`: total de pasos de la plantilla | la plantilla no tiene pasos de trabajo |
| `routineSummaryWorkRest` | `{work, plural, one{# serie} other{# series}} · {rest, plural, one{# descanso} other{# descansos}}` | `{work, plural, one{# interval} other{# intervals}} · {rest, plural, one{# rest} other{# rests}}` | `work`, `rest`: pasos de trabajo y de descanso | hay ≥ 1 paso de trabajo |
| `stepDurationMinSec` | `{m}m {s}s` | `{m}m {s}s` | `m`, `s` | por tiempo, m > 0 y s > 0 |
| `stepDurationMin` | `{m}m` | `{m}m` | `m` | por tiempo, m > 0 y s = 0 |
| `stepDurationSec` | `{s}s` | `{s}s` | `s` | por tiempo, m = 0 |
| `stepDurationMeters` | `{meters}m` | `{meters}m` | `meters`: entero sin separador de miles | por distancia, sin duración |
| `stepDurationNone` | `?` | `?` | — | sin duración ni distancia |
| `stepTargetWattsRange` | `{min}-{max}W` | `{min}-{max}W` | `min`, `max` | vatios mín. y máx. |
| `stepTargetWattsMin` | `>{min}W` | `>{min}W` | `min` | solo vatios mín. |
| `stepTargetSpm` | `{spm} spm` | `{spm} spm` | `spm` | hay SPM objetivo |
| `stepTargetSplit` | `{split}/500m` | `{split}/500m` | `split`: `MM:SS` (125 → `02:05`) | hay split objetivo |
| `stepTargetProgressions` | `📈 {list}` | `📈 {list}` | `list`: progresiones unidas por `,␣` en orden SPM, vatios, split | hay ≥ 1 progresión ≠ 0 |
| `stepProgressionSpm` | `SPM {delta}/rep` | `SPM {delta}/rep` | `delta`: con signo (`+2`, `-1`) | progresión de SPM ≠ 0 |
| `stepProgressionWatts` | `W {delta}/rep` | `W {delta}/rep` | `delta` | progresión de vatios ≠ 0 |
| `stepProgressionSplit` | `Split {delta}s/rep` | `Split {delta}s/rep` | `delta` | progresión de split ≠ 0 |

### 4.18 Piezas base (AC-BASE)

Son de producto en cuanto a **qué** garantizan; el cómo es del Architect (ARCHITECTURE §15-§19, ADR 012, 013, 014 y 016).

- **AC-BASE-01 · Paridad numérica con Dart.** *Dado* cada valor de la tabla, *cuando* se formatea o se redondea, *entonces* el resultado es el de Dart, en .NET 8 y (con Editor) en Unity. Ni el formateo estándar ni el redondeo estándar de .NET los reproducen (QA_PLAN_REVIEW §3.2). [dotnet]; en Unity [Editor] NO VERIFICABLE → It. 2 · ADR 012 §5; oráculo

  | Operación | Entrada → resultado |
  |---|---|
  | 1 decimal | 0.15 → `0.1` · 0.25 → `0.3` · 0.35 → `0.3` · 24.45 → `24.4` · 24.25 → `24.3` · 23.95 → `23.9` · 1.05 → `1.1` · −0.05 → `-0.1` |
  | 0 decimales | 2.5 → `3` · 0.5 → `1` · 1.5 → `2` · −0.5 → `-1` |
  | redondeo a entero | 0.5 → 1 · 2.5 → 3 · −0.5 → −1 · 120.5 → 121 · −0.05 → 0 (nunca "−0") · 0.49999999999999994 → 0 |
  | 1 decimal y de vuelta a número (valor que emite el simulador) | 24.25 → 24.3 |

- **AC-BASE-02 · Independiente de la región del equipo (Q-06).** *Dado* un equipo con la región en `es-AR`, `de-DE` o `fr-FR` (coma decimal y punto de miles), *cuando* se formatean 24,25 spm con 1 decimal y 10000 m de distancia, *entonces* se ve `24.3` y `10000m`, igual que con `en-US`. [dotnet]
- **AC-BASE-03 · Vectores del oráculo Dart (QA-P-23).** *Entonces* los valores esperados de esta spec quedan guardados como datos generados por el oráculo Dart sobre el legacy, con la versión de Dart y el commit del legacy con que se generaron, y los tests de `dotnet` los leen **sin necesitar Dart**. Regenerarlos nunca escribe en el legacy (`git status --porcelain` vacío). Cobertura mínima: AC-BASE-01, AC-BLE-01-02/03, AC-BLE-02 (todos los paquetes), AC-BLE-14-03 a 14-06, AC-RUT-03, AC-RUT-04, AC-RUT-05 (columna "Legacy"), AC-RUT-06, AC-RUT-07, AC-RUT-16-04 y AC-RUT-18. [dotnet] · ADR 012 §4
- **AC-BASE-04 · Una sola fuente de colores (QA-P-05).** *Entonces* cada color de contrato del legacy está definido **una sola vez**, con su hex exacto, y todo lo que lo usa (pantallas, escena, gráficos, tests) lo toma de ahí. Los estilos de pantalla se generan desde esa fuente y un chequeo falla si quedan distintos o si un hex de contrato cambia. Colores del legacy que se fijan en la It. 1: [dotnet] · ADR 016

  | Grupo | Valores | Legacy |
  |---|---|---|
  | Base | fondo `#0D1B2A`, tarjeta `#1A2E45`, semilla `#0077B6`, texto de la barra superior blanco | `theme.dart:5`, `:13`, `:15`, `:20-21` |
  | Métricas | split `#FF9F1C`, SPM `#00B4D8`, vatios `#FFD60A`, distancia `#4CC9F0`, pulso `#EF476F`, tiempo `#E0E0E0`, calorías `#06D6A0` | `theme.dart:33-39` |
  | Pasos | `work` `#EF476F`, `rest` `#06D6A0`, `warmup` `#FFD166`, `cooldown` `#118AB2`, otro `#607D8B` | `theme.dart:56-68` |
  | Fases | EN MARCHA `#06D6A0`, PAUSADO `#FFD166`, FINALIZADO `#EF476F` | `immersive_workout_screen.dart:531-533` |
  | Estados | fuera de objetivo `#FF4444`; Strava `#FC4C02` | `immersive_workout_screen.dart:581-582`; `history_screen.dart:99`, `profile_screen.dart:99` |

  En la misma fuente viven las variantes del HUD del mismo matiz y el respaldo del vidrio `glass.fallback` (`#070D16 @86 %`) del Designer ([ART_BIBLE §2-§3.2](../../design/ART_BIBLE.md)). Los colores de barra inferior del tema legacy (`#12243A`, `#00B4D8`, `#607D8B`, `theme.dart:24-28`) no se aplican en el legacy (DC-58) y no se migran como colores visibles.
- **AC-BASE-05 · Contraste del HUD (R-10; D-ART-01 se confirma en el gate de la 4a).** *Entonces* un chequeo automático mide el contraste de los valores del HUD contra el peor fondo del vidrio sin desenfoque (`glass.fallback` sobre blanco = `#2A2F37`): blanco y métricas no rojas ≥ 7:1, familia roja (`#FF5A7E` pulso, `#FF5F59` fuera de objetivo) ≥ 4,5:1, y texto de interfaz ≥ 4,5:1 sobre `#1A2E45`. Ver P-05 de §13 (valores al límite). [dotnet] · ART_BIBLE §2.3, §10.1
- **AC-BASE-06 · Tiempo controlable.** *Entonces* toda regla que dependa del paso del tiempo se puede probar sin esperar tiempo real (por ejemplo, 3 s simulados en menos de 1 s real) y da el mismo resultado en cada corrida. El primer uso es la It. 2 (remo simulado, caída de 3 s); en la It. 1 existe y tiene su propio test. [dotnet]
- **AC-BASE-07 · Azar controlable.** *Entonces* el ruido del simulador sale de una fuente reemplazable: con "sin ruido", AC-BLE-14-05 y AC-BLE-14-06 dan exactamente lo mismo en cada corrida. [dotnet]
- **AC-BASE-08 · Core fuera de Unity (R-09).** *Entonces* todas las reglas de esta spec se verifican con un comando en Windows (y en Linux, en el CI), sin Unity, sin red y sin Dart, y la lógica pura no depende del motor. Las mismas pruebas compilan y corren dentro de Unity. [dotnet]; dentro de Unity [Editor] NO VERIFICABLE → It. 2
- **AC-BASE-09 · Trazabilidad.** *Entonces* cada AC de esta spec tiene al menos un test marcado con su ID, cada caso legacy portado cita su origen (`archivo:línea` y nombre del caso, §5) y QA puede armar la matriz AC → test → resultado desde la salida de los tests. [dotnet]
- **AC-BASE-10 · Proyecto Unity preparado (QA-P-25).** *Entonces* el proyecto declara Unity **6000.3.27f1** con URP 17.3.0 y UI Toolkit (ADR 001 y 006), con versiones de paquetes exactas, y separa Core puro, servicios y presentación. Una preparación inicial mínima aplica el nombre y los identificadores de §4.19, **no** fija niveles de calidad definitivos (QA-P-10) y **no** activa el modo simulador (R-01). Revisión estática [dotnet + QA]; abrir el proyecto sin errores de compilación [Editor] NO VERIFICABLE → It. 2. · ARCHITECTURE §16

### 4.19 F-APP-16 (también toca) — Nombre, identificadores provisionales y Android mínimo (R-14, Q-85, Q-90)

En la It. 1 la preparación del proyecto deja esto configurado y QA lo revisa de forma estática; se verifica en equipos a partir de la It. 2. F-APP-16 se cierra en la It. 14 (ícono, arranque, identificadores definitivos).

- **AC-APP-16-01 · Nombre y versión.** *Entonces* el nombre visible es `RowMate` en Android, iOS, macOS **y Windows** (hoy Windows dice `rower_app`, DC-63), la empresa es `iguisoft` y la versión es `1.0.0`, compilación 1 (como el legacy, `pubspec.yaml:4`). [Editor]/[Device] NO VERIFICABLE → It. 2; [Apple] → It. 10b
- **AC-APP-16-02 · Identificadores provisionales (D-02 aceptada).** *Entonces*:
  - **Android**: `com.iguisoft.rowmate` (el legacy es `com.example.rower_app`, `android/app/build.gradle.kts:24`).
  - **iOS y macOS**: **`com.iguisoft.rowmate.unity`**, distinto del legacy `com.iguisoft.rowmate` (`ios/Runner.xcodeproj/project.pbxproj:485`, `macos/Runner/Configs/AppInfo.xcconfig:11`). Es también el identificador único de escritorio del motor, que comparten macOS y Windows.
  - **Windows**: una app de escritorio de Unity **no tiene identificador propio** en Windows (restricción del motor, D-02 del diseño). La identidad es **producto `RowMate` de la empresa `iguisoft`**, y con eso la carpeta de datos de la app nueva es `AppData/LocalLow/iguisoft/RowMate`, distinta de las del legacy (preferencias en `AppData/Roaming/com.example/rower_app`, por `windows/runner/Runner.rc:92-98` y `path_provider_windows` 2.3.0; base `rower_app.db` en `.dart_tool/sqflite_common_ffi/databases/` del directorio de trabajo, por `main.dart:38-41`, `core/database/database_service.dart:21` y `sqflite_common_ffi` 2.4.3). **`com.iguisoft.rowmate` queda reservado** como identificador de Windows para el paquete instalable (MSIX) de la It. 14 (Q-85): en la It. 1 es solo un dato documentado, que ningún paso de la preparación aplica.
  - **Ningún `com.example`** en ninguna plataforma: ni en identificadores, ni en empresa, ni en producto.

  Estático [QA] (identificadores, empresa y producto); la carpeta de datos de Windows y la convivencia, [Device] → It. 2; [Apple] → It. 10b
- **AC-APP-16-03 · Convivencia con el legacy (R-14).** *Dado* un equipo con la app legacy instalada y con datos, *cuando* se instala un build de la app nueva, *entonces* la legacy sigue instalada, abre y conserva sus rutinas, historial, pulsómetro recordado y sesión de Strava, y la app nueva no abre ni modifica la base del legacy. [Device] Android y Windows NO VERIFICABLE → It. 2; [Apple] → It. 10b
- **AC-APP-16-04 · Android mínimo (Q-90).** *Entonces* la app instala en Android **8.0 (API 26)** o más nuevo y **solo en ARM64**; en Android 7.x o en un equipo de 32 bits no se instala. El legacy instala desde API 24 (Android 7.0, valor por defecto de Flutter 3.47.6, `android/app/build.gradle.kts:27`) y en 32 bits: esto es un cambio de producto que el usuario confirma en el gate. [Device] NO VERIFICABLE → It. 2 (verificación final en la It. 14)
- **AC-APP-16-05 · iOS mínimo.** *Entonces* iOS 15.0, como el legacy (`ios/Runner.xcodeproj/project.pbxproj:461`, `:595`, `:646`). [Apple] → It. 10b. macOS y Windows no tienen criterio en la It. 1: sus mínimos los fija el motor (Windows 10 21H1, build 19043, y macOS 12) y son una pérdida de equipos frente al legacy que se decide en los gates de la It. 2 (Windows) y de la 10b (macOS) (P-04 de §13, §9).

### 4.20 F-APP-19 (también toca) — CI de los tests del Core (`ci-core.yml`)

F-APP-19 se cierra en la It. 14 (builds de las 4 plataformas). En la It. 1 nace el CI de los tests.

- **AC-APP-19-01 · Corre en cada cambio.** *Dado* un push o un pull request, *entonces* el CI compila y corre todos los tests de §4 y §5 en Windows y en Linux, sin Unity, sin licencia de Unity, sin Dart y sin credenciales. [CI] NO VERIFICABLE si no hay repositorio remoto (P-03 de §13)
- **AC-APP-19-02 · Rojo cuando algo falla.** *Dado* un test que falla, *entonces* el CI queda en rojo y nombra el test (y con él, su AC). [CI]
- **AC-APP-19-03 · Higiene del repositorio (R-02, R-08).** *Entonces* el CI falla si el repositorio versiona un archivo de credenciales de Strava (`StravaSecrets*`, `strava_secrets.*`, `strava.local.json`), una llave de firma (`*.keystore`, `*.jks`, `*.p12`, `*.mobileprovision`), la licencia de Unity (`*.ulf`) o archivos de terceros (Mixamo, Asset Store) que no sean sus `.meta`, o si los estilos de color generados no coinciden con la fuente única (AC-BASE-04). Nadie lee ni copia `lib/core/strava/strava_secrets.dart` del legacy. [CI] y [dotnet] · ARCHITECTURE §19
- **AC-APP-19-04 · Local = CI.** *Entonces* el mismo comando documentado de la demo corre, en la PC, exactamente lo mismo que el CI y da el mismo resultado. [dotnet]

---

## 5. Los 19 casos de test legacy que se portan

Cada caso se porta 1 a 1, con su nombre original y el comentario de origen (`archivo:línea`). Las líneas son las de la declaración `test(` en el legacy.

| LT | Archivo:línea | Caso (nombre exacto) | Qué afirma | AC |
|---|---|---|---|---|
| LT-01 | `test/rowing_simulator_test.dart:7` | `más watts → pace más rápido` | El split de 300 W es menor que el de 150 W | AC-BLE-14-03 |
| LT-02 | `test/rowing_simulator_test.dart:14` | `203 W ≈ 2:00 /500m (fórmula Concept2)` | El split de 203 W es 120 ± 0,5 s | AC-BLE-14-03 |
| LT-03 | `test/rowing_simulator_test.dart:18` | `acumula distancia, remadas y tiempo mientras rema` | Sin ruido, 150 W, 24 spm, 60 ticks: 150 W, 24 spm, split 133, 60 s, 24 paladas, 226 m, 13 kcal y pulso > 70 | AC-BLE-14-05 |
| LT-04 | `test/rowing_simulator_test.dart:36` | `sin remar: 0 spm/watts/pace y totales congelados` | Sin ruido: 10 ticks, un tick "antes", "Remando" apagado y un tick "después": spm, W y split 0; distancia, paladas, tiempo y kcal iguales a "antes"; pulso menor que "antes" | AC-BLE-14-06 |
| LT-05 | `test/rowing_simulator_test.dart:54` | `los objetivos se limitan a su rango` | 9999 W → 500 (máximo) y 1 spm → 14 (mínimo) | AC-BLE-14-02 |
| LT-06 | `test/rowing_simulator_test.dart:62` | `con ruido, los watts quedan dentro de ±5 %` | Con ruido real y objetivo 200 W, los 100 ticks dan potencia en [190; 210] | AC-BLE-14-09 |
| LT-07 | `test/rowing_simulator_test.dart:70` | `heartRate expone el último pulso calculado por tick()` | Sin ruido, 1 tick: el pulso expuesto es igual al de la lectura y mayor que 70 (este caso está fuera del grupo `RowingSimulator`) | AC-BLE-14-08 |
| LT-08 | `test/scan_filter_test.dart:24` | `con filtro en el cliente solo pasan los que anuncian el servicio` | Con filtro y servicio 0x180D: `[]` y `[febe]` rechazados; `[180d]` aceptado ("el UUID de 16 bits equivale al de 128"); `[febe, 0x180D completo]` aceptado | AC-BLE-03-02 |
| LT-09 | `test/scan_filter_test.dart:32` | `sin filtro en el cliente (el sistema ya filtró) pasa todo` | Sin filtro: `[]` y `[febe]` aceptados | AC-BLE-03-03 |
| LT-10 | `test/heart_rate_parser_test.dart:5` | `bpm en uint8 sin información de contacto` | `[0x00, 72]` → 72 bpm, contacto "no informa" | AC-HRM-01-02 |
| LT-11 | `test/heart_rate_parser_test.dart:11` | `bpm en uint16 little-endian` | `[0x01, 0x2C, 0x01]` → 300 | AC-HRM-01-03 |
| LT-12 | `test/heart_rate_parser_test.dart:16` | `bits de contacto: 01 = no soportado, 10 = sin contacto, 11 = con contacto` | `[0x02, 70]` → no informa; `[0x04, 0]` → sin contacto; `[0x06, 65]` → con contacto y 65 bpm | AC-HRM-01-04 |
| LT-13 | `test/heart_rate_parser_test.dart:23` | `ignora Energy Expended y RR intervals` | `[0x18, 80, 0x10, 0x00, 0x00, 0x04]` → 80 | AC-HRM-01-05 |
| LT-14 | `test/heart_rate_parser_test.dart:29` | `bytes vacíos o truncados devuelven null` | `[]`, `[0x00]` y `[0x01, 72]` → sin lectura ("uint16 necesita 3 bytes") | AC-HRM-01-06 |
| LT-15 | `test/heart_rate_parser_test.dart:35` | `UUIDs del perfil` | Servicio `0000180d-0000-1000-8000-00805f9b34fb` y medición `00002a37-0000-1000-8000-00805f9b34fb` | AC-HRM-01-01 |
| LT-16 | `test/routine_positions_test.dart:6` | `las posiciones quedan alineadas con flattenedSteps` | T1: largo de posiciones = largo expandido = 8; [0] = (sin serie, 1, 1); [1] = [2] = (a, 1, 3); [3] = (a, 2, 3); [6] = (a, 3, 3); [7] = (sin serie, 1, 1) | AC-RUT-06-01, AC-RUT-07-01 |
| LT-17 | `test/session_stats_test.dart:7` | `avgHeartRate y maxHeartRate ignoran los ceros` | Pulsos [0, 120, 150, 0, 141] → promedio 137, máximo 150, tiene pulso | AC-RUT-18-06 |
| LT-18 | `test/session_stats_test.dart:14` | `sin pulso quedan en 0` | Pulsos [0, 0] → promedio 0, máximo 0, sin pulso | AC-RUT-18-07 |
| LT-19 | `test/session_stats_test.dart:21` | `sin puntos quedan en 0` | Lista vacía → sin pulso | AC-RUT-18-01 |

Total: `rowing_simulator_test` 7 + `scan_filter_test` 2 + `heart_rate_parser_test` 6 + `routine_positions_test` 1 + `session_stats_test` 3 = **19**.

---

## 6. Casos 1-15 de RUT §5.3 → criterios

| Caso | Qué fija | AC |
|---|---|---|
| 1 | Progresión de SPM: 18, 20, 22; descansos sin cambio; último paso descanso | AC-RUT-06-02 |
| 2 | Vatios en rango: 140–160, 150–170, 160–180 | AC-RUT-06-03 |
| 3 | Vatios solo mínimo: 150, 160, 170 (máximo vacío) | AC-RUT-06-04 |
| 4 | Split: 120, 118, 116 | AC-RUT-06-05 |
| 5 | Progresión sin objetivo: sigue vacío | AC-RUT-06-06 |
| 6 | Paso suelto con progresión: no se progresa | AC-RUT-06-07 |
| 7 | Repeticiones vacías → ×1; 0 → 0 pasos y 0 posiciones | AC-RUT-06-08, AC-RUT-07-03 |
| 8 | Dos series contiguas con su propio total | AC-RUT-06-09, AC-RUT-07-04 |
| 9 | Etiquetas de duración en es y en | AC-RUT-03-01 a AC-RUT-03-05 |
| 10 | Etiquetas de objetivos en es y en | AC-RUT-04-01 a AC-RUT-04-03 |
| 11 | Resúmenes es y en (y columna legacy) | AC-RUT-05-02, AC-RUT-05-04 |
| 12 | P99: índices y ejemplos | AC-RUT-18-02, AC-RUT-18-03 |
| 13 | Totales = máximos | AC-RUT-18-05 |
| 14 | Redondeo del pulso: [120, 121] → 121 | AC-RUT-18-08 |
| 15 | Formatos de split y duración | AC-BLE-01-02, AC-BLE-01-03, AC-RUT-18-09 |

Los casos 16 a 21 (editor y lista) son de la It. 3.

---

## 7. Checklist de paridad

| # | Comportamiento del legacy | Legacy | Test legacy | AC |
|---|---|---|---|---|
| 1 | Lectura de 8 campos, vacía en 0, pulso 0 = sin dato | `core/models/rowing_data.dart:2-21` | — | AC-BLE-01-01 |
| 2 | Split `MM:SS`, `--:--` si ≤ 0, `100:00` | `rowing_data.dart:24-29` | — | AC-BLE-01-02 |
| 3 | Tiempo `H:MM:SS` / `MM:SS` | `rowing_data.dart:32-40` | — | AC-BLE-01-03 |
| 4 | Copia con cambios (el pulso del sensor reemplaza al del remo) | `rowing_data.dart:42-62` | — | AC-BLE-01-04 |
| 5 | Textos de la grilla de Dispositivo (`24.0`, `149`, `652m`) | `features/device/device_screen.dart:253-258` | — | AC-BLE-01-05 |
| 6 | UUID FTMS `0x1826`, Rower Data `0x2AD1`, Control Point `0x2AD9` | `core/bluetooth/ftms_parser.dart:22-24` | — | AC-BLE-02-01 |
| 7 | Paquete < 2 bytes descartado; vacío ignorado | `ftms_parser.dart:27`; `ble_service.dart:292` | — | AC-BLE-02-09, AC-BLE-02-16 |
| 8 | Bit 0 = More Data; ritmo × 0,5; paladas uint16 | `ftms_parser.dart:43-53` | — | AC-BLE-02-02, -03, -10 |
| 9 | Distancia uint24, split, split promedio solo si el instantáneo es 0 | `ftms_parser.dart:61-80` | — | AC-BLE-02-02, -04, -05, -06 |
| 10 | Potencia sint16, promedio solo si la instantánea es 0 | `ftms_parser.dart:83-94` | — | AC-BLE-02-11 |
| 11 | Bits 1, 7 y 10 se saltean sin verificar bytes; bit 12 ignorado | `ftms_parser.dart:56-58`, `:97-99`, `:118-120`, `:128-129` | — | AC-BLE-02-12 |
| 12 | Energía total (+ kcal/h y kcal/min salteados), pulso, tiempo | `ftms_parser.dart:102-126` | — | AC-BLE-02-07 |
| 13 | Truncados sin error; campo ausente = 0; valores crudos | `ftms_parser.dart:45-140` | — | AC-BLE-02-08, -13, -14, -15 |
| 14 | Filtro en la app solo en Windows (y Linux, fuera de alcance) | `core/bluetooth/scan_filter.dart:12`, `:19-20` | `scan_filter_test.dart:24`, `:32` | AC-BLE-03-01 a -03 |
| 15 | UUID de 16 bits ≡ 128 bits; vale para remo y pulsómetro | `scan_filter.dart:14-15`; `guid.dart` (FBP 9.0.3); `ble_service.dart:106`; `heart_rate_service.dart:136` | `scan_filter_test.dart:24` | AC-BLE-03-04, -05 |
| 16 | Simulador: topes 30-500 W y 14-40 spm, iniciales 150/24, remando, pulso 70 | `core/dev/rowing_simulator.dart:7-10`, `:20-37` | `rowing_simulator_test.dart:54` | AC-BLE-14-01, -02 |
| 17 | Split de Concept2 y redondeo | `rowing_simulator.dart:48-49`, `:77-78` | `rowing_simulator_test.dart:7`, `:14` | AC-BLE-14-03 |
| 18 | Calorías de Concept2; distancia, paladas y tiempo por tick | `rowing_simulator.dart:50-53`, `:81-82` | `rowing_simulator_test.dart:18` | AC-BLE-14-04, -05 |
| 19 | Sin remar: 0 y totales congelados | `rowing_simulator.dart:41-44` | `rowing_simulator_test.dart:36` | AC-BLE-14-06 |
| 20 | Pulso: objetivo, 10 % por tick, pulso expuesto | `rowing_simulator.dart:32`, `:56-57` | `rowing_simulator_test.dart:70` | AC-BLE-14-07, -08 |
| 21 | Ruido ±5 % y ±1 spm; sin ruido = objetivos | `rowing_simulator.dart:46-47`, `:73-74` | `rowing_simulator_test.dart:62` | AC-BLE-14-09 |
| 22 | Salida: spm a 1 decimal como Dart, partes enteras con 1e-6, pulso redondeado | `rowing_simulator.dart:59-69` | `rowing_simulator_test.dart:18` | AC-BLE-14-10, AC-BASE-01 |
| 23 | Pulso 8/16 bits, contacto, energía y RR ignorados | `core/bluetooth/heart_rate_parser.dart:24-40` | `heart_rate_parser_test.dart:5`, `:11`, `:16`, `:23` | AC-HRM-01-02 a -05, -08, -09 |
| 24 | Paquetes de pulso inválidos; sin validación de rango | `heart_rate_parser.dart:25`, `:28` | `heart_rate_parser_test.dart:29` | AC-HRM-01-06, -07 |
| 25 | UUID 0x180D y 0x2A37 | `heart_rate_parser.dart:21-22` | `heart_rate_parser_test.dart:35` | AC-HRM-01-01 |
| 26 | 4 tipos de paso, orden y nombre guardado | `core/models/interval_step.dart:1`, `:115`, `:133` | indirecto (`step_pill_test`) | AC-RUT-01-01, -05 |
| 27 | Nombre localizado y color por tipo | `shared/theme.dart:42-69` | — | AC-RUT-01-02, -03 |
| 28 | Ícono por tipo | `features/routines/routine_editor_screen.dart:447-458` | — | AC-RUT-01-04 |
| 29 | Atributos del paso, meta, copia, sin validación | `interval_step.dart:21-62`, `:147-180` | fixture en varios | AC-RUT-02-01 a -04 |
| 30 | Etiqueta de duración | `interval_step.dart:67-77` | — | AC-RUT-03-01 a -05 |
| 31 | Etiqueta de objetivos y progresiones | `interval_step.dart:79-109` | — | AC-RUT-04-01 a -05 |
| 32 | Subtítulo del editor `{duración}␣␣·␣␣{objetivos}` | `routine_editor_screen.dart:345-349` | — | AC-RUT-04-06 |
| 33 | Atributos de la rutina | `core/models/routine.dart:8-21` | — | AC-RUT-05-01 |
| 34 | Resumen sobre la plantilla | `routine.dart:100-105`; `workout_screen.dart:115`, `:152`; `routines_screen.dart:59` | — | AC-RUT-05-02 a -04 |
| 35 | Expansión de series con progresiones | `routine.dart:29-70` | `routine_positions_test.dart:6` (largo) | AC-RUT-06-01 a -13 |
| 36 | Posiciones alineadas | `routine.dart:74-97` | `routine_positions_test.dart:6` | AC-RUT-07-01 a -05 |
| 37 | Sesión: atributos, duración, fechas UTC, lectura tolerante | `core/models/workout_session.dart:2-67` | — | AC-RUT-16-01 a -05 |
| 38 | "Promedio" = P99 al cerrar | `features/workout/workout_provider.dart:276-292` | — | AC-RUT-16-02 |
| 39 | Punto de telemetría | `workout_session.dart:140-182` | fixture (`session_stats_test`) | AC-RUT-17-01, -02 |
| 40 | P99, totales, pulso promedio y máximo | `workout_session.dart:111-136` | `session_stats_test.dart:7`, `:14`, `:21` | AC-RUT-18-01 a -09 |
| 41 | Idiomas en y es, respaldo en; elección por la lista | `main.dart:97-106`; `l10n/app_localizations.dart:96-99`, `:1066-1080`; `l10n.yaml:2`; SDK `widgets/app.dart:146-238` | indirectos (`heart_rate_card_test`, `environment_picker_test`) | AC-APP-09-01 a -07 |
| 42 | 159 claves, 7 con parámetros, sin plurales | `l10n/app_es.arb:1-208`, `app_en.arb:1-208` | indirectos | AC-APP-10-01 a -09 |
| 43 | Nombre RowMate, versión 1.0.0+1, ids, carpeta de datos en Windows, mínimos de plataforma | `pubspec.yaml:4`; `android/app/build.gradle.kts:24`, `:27`; `ios/Runner.xcodeproj/project.pbxproj:461`, `:485`; `macos/Runner/Configs/AppInfo.xcconfig:8`, `:11`; `macos/Runner.xcodeproj/project.pbxproj:560`; `windows/runner/Runner.rc:92-98`; `windows/runner/runner.exe.manifest` (Windows 10 y 11); `main.dart:38-41`; `core/database/database_service.dart:21` | — | AC-APP-16-01 a -05 |
| 44 | Colores de contrato | `theme.dart:5-68`; `immersive_workout_screen.dart:531-533`, `:581-582`; `history_screen.dart:99` | — | AC-BASE-04 |

---

## 8. Mejoras EXR (cómo se tiene que sentir)

La It. 1 no tiene pantallas. La mejora es de **confianza**: lo que la app nueva le muestre al remero va a coincidir con la app vieja hasta el último dígito y la última letra. Nunca un "24.2" donde antes decía "24.3", nunca un "1 sesiones", nunca una lista de búsqueda con equipos que no son remos. Un paquete raro del monitor nunca congela nada, y cada número que más adelante "entre con una transición" va a ser el número real. En inglés la app deja de mezclar textos en español (resumen y etiquetas de rutina) sin que el español cambie.

---

## 9. Decisiones aplicadas y gate de la It. 1

| Decisión | Qué se aplica | Confirmar en el gate |
|---|---|---|
| Q-01 | Resumen y etiquetas de rutina al catálogo (15 claves, §4.17.2); español idéntico al legacy | **Sí** |
| Q-02 | 4 claves corregidas y plurales en las claves nuevas (§4.17.1) | No |
| Q-05 | Sin selector de idioma; sigue al sistema con respaldo en inglés | No |
| Q-07 | Sin unidades explícitas en la etiqueta de duración (`2m` = 2 minutos, `500m` = 500 metros) | No |
| Q-10 | El `📈` se mantiene en el texto de las progresiones | No |
| Q-14 | "Promedio" = P99, idéntico al legacy | **Sí** |
| Q-38 | Valores FTMS crudos (65535, potencia negativa) | No |
| Q-48 | Pulso sin validación de rango | No |
| Q-69 | La lógica pura de la escena no entra en la It. 1 (va en las It. 6a-8) | No |
| Q-90 | Android mínimo 8.0 (API 26), solo ARM64 | **Sí** |
| R-14 / Q-85 + D-02 | Ids provisionales: Android `com.iguisoft.rowmate`; iOS y macOS `com.iguisoft.rowmate.unity` (distinto del legacy). Windows no tiene id en el motor: producto `RowMate` de `iguisoft`, datos en `LocalLow/iguisoft/RowMate`, ningún `com.example`, y `com.iguisoft.rowmate` reservado para el MSIX de la It. 14 (AC-APP-16-02) | **Sí** |
| Paridad de idioma | Elección con la regla exacta de Flutter, incluidas las 2 filas † de AC-APP-09-02 | No (ver P-01) |
| P-06 (diferencia aceptada) | Teclado y menú de copiar y pegar en el idioma **del sistema**, no en el de la app (AC-APP-09-07). Solo se nota con el sistema en un idioma que no es es ni en | **Sí, en el gate de la It. 2**: el usuario la acepta (por defecto) o pide una barra de edición propia para la It. 3 |
| P-04 (agendada) | Mínimos de escritorio del motor: Windows 10 21H1 (build 19043) o más nuevo y macOS 12 o más nuevo. Se pierden los Windows 10 anteriores a 21H1 y macOS 10.15 y 11, que el legacy soporta | **Sí, en el gate de la It. 2 (Windows) y en el de la 10b (macOS)**. Por defecto se adopta: lo impone Unity 6.3 (ADR 001) |

---

## 10. Restricciones de producto para el Architect

1. **R-13 y R-09.** Toda regla de esta spec se reproduce tal cual y se verifica con `dotnet test` fuera de Unity; los mismos tests corren en Unity.
2. **Mismo texto en todas las plataformas.** Un mismo valor se ve igual en Android, iOS, Windows y macOS, sea cual sea el runtime y la región del equipo (AC-BASE-01, AC-BASE-02).
3. **Idioma por la lista completa del sistema** (AC-APP-09-06). La resolución tiene que dar la tabla de AC-APP-09-02, filas † incluidas: la descripción actual de ARCHITECTURE §12 y ADR 011 ("devuelve el primero soportado") no cubre esas dos filas.
4. **Tiempo y azar controlables en tests** (AC-BASE-06, AC-BASE-07), para que todo lo que viene (remo simulado, motor, reconexiones) se pruebe en milisegundos y sin resultados al azar.
5. **Una sola fuente de colores** con chequeo de sincronía y de contraste (AC-BASE-04, AC-BASE-05).
6. **R-02.** Nadie lee ni copia `lib/core/strava/strava_secrets.dart`; ninguna credencial se versiona (AC-APP-19-03).
7. **R-14.** Un build de la app nueva nunca reemplaza ni borra la app legacy ni sus datos (AC-APP-16-03).
8. **R-01.** El modo simulador no se activa en la preparación del proyecto ni en ningún build de distribución (AC-BASE-10).
9. **QA-P-10.** La It. 1 no fija niveles de calidad definitivos; eso es de la It. 6a.

---

## 11. Fuera de alcance

- **Toda pantalla**: Dispositivo, tarjeta del pulsómetro, panel del simulador y barra de pestañas (It. 2); editor y lista de rutinas (It. 3); HUD (It. 4a-5); historial (It. 9); Perfil (It. 12-13).
- El remo y el pulsómetro **simulados como servicios** (1 lectura por segundo, caída de 3 s, el remo simulado manda pulso 0): It. 2 (F-BLE-15, F-HRM-15). En la It. 1 está solo la física (F-BLE-14).
- Búsqueda, conexión, toma de control, keepalive, reconexión y watchdog reales: It. 10a (remo) y 11 (pulsómetro).
- Base local, persistencia de rutinas y sesiones y base separada del simulador: It. 3 (las fechas y la lectura tolerante de F-RUT-16 se fijan acá como regla del modelo).
- Motor de entrenamiento: fases, reloj, avance, telemetría cada 5 s, semáforo, formatos del HUD (It. 4a).
- Lógica pura de la escena y los vectores del generador aleatorio del paisaje (It. 6a-8, Q-69).
- Strava y el TCX (It. 12-13).
- Textos fijos fuera del catálogo (F-APP-11, It. 14); los textos del panel del simulador y del diagnóstico quedan fijos (Q-03, Q-04).
- Bootstrapper completo, fuentes, íconos y `ci-unity.yml` (It. 2); niveles de calidad (It. 6a); ícono, arranque e ids definitivos (It. 14).
- Lo que el legacy tiene y no se migra: nombres fijos en español del tipo de paso, "es descanso", "por tiempo/por distancia" y la duración total estimada de la rutina (código sin uso, routines-models §7), y la tarjeta de paso compartida (F-RUT-19, descarte en la It. 2).

---

## 12. Lo que la It. 1 no puede ver y dónde se verifica

| AC | Qué falta ver | Dónde |
|---|---|---|
| AC-BLE-01-05 | Los textos en la grilla de Dispositivo | It. 2 [Editor] |
| AC-RUT-01-04, AC-RUT-04-06 | Íconos y subtítulos en el editor | It. 3 [Editor] |
| AC-RUT-05-03 | El diálogo de inicio de rutina | It. 4a [Editor] |
| AC-APP-09-04 a -06 | Idioma en pantalla, cambio en vivo, lista nativa | It. 2 [Editor]/[Device]; [Apple] It. 10b |
| AC-APP-09-07 | Teclado y menú de copiar y pegar en el idioma del sistema (diferencia aceptada, P-06) | It. 3 [Device] (primer campo de texto, en el editor de rutinas); [Apple] It. 10b |
| AC-BASE-01, AC-BASE-08 | Las mismas pruebas dentro de Unity | It. 2 [Editor] (y el *smoke test* de build de la It. 2) |
| AC-BASE-10 | Abrir el proyecto sin errores | It. 2 [Editor] |
| AC-APP-16-01 a -04 | Nombre, ids, carpeta de datos de Windows (`LocalLow/iguisoft/RowMate`), convivencia y Android mínimo en equipos | It. 2 [Device]; final en la It. 14 |
| AC-APP-16-05 y lo [Apple] de AC-APP-16-01 a -03 | iOS y macOS | It. 10b [Apple] |
| AC-APP-19-01, -02 | Corrida del CI en GitHub | En cuanto haya repositorio remoto (P-03) |

Estos AC se revisan de forma estática en la It. 1 y su ID igual se cierra en la It. 1 (ROADMAP §2, It. 1, "Verificación"). Si en la It. indicada fallan, el ID se reabre allí.

---

## 13. Preguntas abiertas

| ID | Pregunta | Por defecto (lo que hace el legacy o lo que se aplica) | Para |
|---|---|---|---|
| P-01 | La regla de idioma del legacy (la de Flutter) da dos resultados poco intuitivos (filas † de AC-APP-09-02: `fr-FR, es-AR, en` → inglés). ¿Se mantiene o se simplifica a "el primer idioma soportado de la lista"? | **Paridad**: se mantiene. Simplificarla sería un cambio de producto menor para casos muy raros | Usuario (sin urgencia) |
| P-02 | ¿Se quiere que la app avise "sin contacto" cuando la banda lo informa (AC-HRM-01-04 ya lo decodifica)? | No en la paridad (Q-46, backlog BKL-11) | Usuario |
| P-03 | `ci-core.yml` necesita un repositorio remoto en GitHub. Hoy el repo local no tiene remoto ni commits. ¿El usuario lo crea? | Si no hay remoto, AC-APP-19-01 y -02 quedan NO VERIFICABLE; QA revisa el workflow y corre los mismos comandos local (AC-APP-19-04). Se agregó como prerrequisito opcional en el ROADMAP | Usuario |
| P-04 | El legacy corre en macOS 10.15 (`macos/Runner.xcodeproj/project.pbxproj:560`) y en Windows 10 (`windows/runner/runner.exe.manifest`). ¿Cuál es el mínimo de macOS y de Windows de Unity 6000.3? Si es más alto, es una pérdida de equipos como la de Q-90 | **Respondida y agendada (QA-12).** El motor pide Windows 10 21H1 (build 19043) o más nuevo y macOS 12 Monterey o más nuevo (02-tech-design §10.3; manual de Unity 6.3, "System requirements", releído por el PO el 2026-10-27). Se pierden los Windows 10 anteriores a 21H1 y macOS 10.15 y 11. Decisión del usuario en el gate de la It. 2 (Windows) y en el de la 10b (macOS); por defecto se adopta, porque no hay alternativa sin cambiar de motor. Agendada en ROADMAP §6 | Usuario (gates de la It. 2 y la 10b) |
| P-05 | Con el fondo del peor caso redondeado a `#2A2F37`, `metric.split.hud` `#FEA845` da 6,99:1, `metric.distance` `#4CC9F0` 6,999:1 y `metric.hr.hud` `#FF5A7E` 4,497:1, apenas debajo de los umbrales de AC-BASE-05; con la mezcla sin redondear (`#070D16 @86 %` sobre blanco) dan 7,004, 7,016 y 4,508 y pasan. El chequeo de contraste puede fallar según cómo se calcule | El Designer y el Architect definen el método (mezcla sin redondear) o suben esos tokens con margen. El umbral de producto (R-10) no cambia | Designer + Architect |
| P-06 | ¿Qué textos del sistema aparecen dentro de una app de Unity (menús de copiar y pegar, teclado, accesibilidad)? AC-APP-09-07 pedía que sigan el idioma elegido | **Respondida y aceptada (QA-12).** Solo el teclado y su menú de copiar y pegar, que siguen el idioma del sistema; no hay textos de accesibilidad del sistema (02-tech-design §10.3). El PO la acepta como diferencia conocida y reescribe AC-APP-09-07. En el gate de la It. 2 el usuario puede pedir una barra de edición propia, que entraría en la It. 3. Agendada en ROADMAP §6 | Usuario (gate de la It. 2) |
| P-07 | El ícono legacy de descanso es `pause_circle_outline` (sin relleno) y ART_BIBLE §2.2 nombra `pause_circle`. ¿El glifo elegido es la variante sin relleno? | Paridad visual: sin relleno | Designer (It. 2/3) |
| P-08 | ARCHITECTURE RA-07 (§21) todavía dice "respaldo plano `#070D16 @80 %`" y ART_BIBLE §9 conserva una "diferencia abierta" con ARCHITECTURE §20.1 que ya está resuelta. Tienen que decir `glass.fallback` (`#070D16 @86 %`) | El ROADMAP ya se corrigió (§14) | Architect + Designer |
| P-09 | Además del menú de copiar y pegar, Flutter pone en el idioma de la app los textos de sus componentes estándar. El legacy muestra al menos la ayuda del botón de volver (`Atrás` / `Back`, `flutter_localizations` `material_es.arb:5`, `material_en.arb:19`) en las pantallas que se abren encima de otra: editor de rutina (`routine_editor_screen.dart:206`) y detalle de sesión (`session_detail_screen.dart:86`). En la app nueva esos textos no los pone nadie si no están en el catálogo | Paridad: si la pantalla nueva tiene un botón de volver con ayuda o etiqueta de accesibilidad, el texto sale del catálogo, en el idioma de la app (`Atrás` / `Back`). Lo inventarían los specs de la It. 3 (editor) y la 9 (detalle). No cambia las 174 claves de la It. 1 | PO (It. 3 y 9) |

---

## 14. Discrepancias encontradas y correcciones hechas al especificar

| Dónde | Qué decía | Qué es (verificado) | Acción |
|---|---|---|---|
| Pedido de la It. 1 | "scan filter (F-BLE-14), física del simulador (F-BLE-03)" | En el inventario, F-BLE-03 es el filtro y F-BLE-14 la física | Esta spec usa los IDs del inventario |
| `rower-ble-device-sim.md`, tabla de F-BLE-14 | 180 W → 920 kcal/h | 919,488 kcal/h → 919 | Corregido en el inventario |
| `app-shell-platform.md` Anexo A | Saltos de línea con espacios alrededor (`encendido ⏎ y`) | El `.arb` no tiene espacios: `encendido\ny` | Esta spec fija la forma exacta (§0.2, AC-APP-10-02) |
| ROADMAP (demo de la 4a, §6 QA-P-17, §7 P-04, §8) | Respaldo del vidrio `#070D16 @80 %` | Token `glass.fallback`, `#070D16 @86 %` (ART_BIBLE §3.2) | Corregido en el ROADMAP |
| ROADMAP regla 9 y criterio de la 6b | "con la tabla de ART_BIBLE §9" | La tabla única es ARCHITECTURE §20 (QA-P-10); ART_BIBLE §9 solo reparte el arte | Corregido en el ROADMAP |
| ARCHITECTURE §12 y ADR 011 | "devuelve el primero soportado" | Flutter tiene 2 excepciones (AC-APP-09-02, filas †) | Restricción 3 de §10 (para el Architect) |
| ROADMAP §6 (fila R-14 / Q-85) y STATUS (decisiones del gate de la It. 1) | `com.iguisoft.rowmate` "en Android y Windows" | En Windows el motor no tiene identificador (D-02): `RowMate` de `iguisoft`, `com.iguisoft.rowmate` reservado al MSIX de la It. 14 | Corregidos por el PO (gate QA, QA-02) |
| ROADMAP §6 | Sin fila para los mínimos de escritorio ni para los textos del sistema | P-04 y P-06 de §13 son decisiones del usuario | Agregadas: P-04 (Windows) y P-06 al gate de la It. 2; P-04 (macOS) al de la 10b (QA-12) |
| FEATURE_INVENTORY Q-85 y R-14; ARCHITECTURE §16 (punto 1) y §21 (Q-85 / R-14) | `com.iguisoft.rowmate` "en Android y Windows" | Ídem (D-02) | **Pendiente al cierre de la It. 1**: FEATURE_INVENTORY, el PO; ARCHITECTURE, el Architect (junto con su §10.4) |
| 02-tech-design §8 (fila AC-APP-09-07) | Ign. "[Device] → It. 2" | El primer campo de texto llega en la It. 3 (AC-APP-09-07) | **Pendiente**: el Architect cambia el motivo del `Ignore` a "→ It. 3" |

---

## Cambios por gate QA

Gate de armonía: [03-qa-harmony.md](03-qa-harmony.md) (APROBADO CON OBSERVACIONES). Issues asignados al PO: QA-02, QA-11 y QA-12. Los tres se resuelven, ninguno se disputa. El número de criterios no cambia (**139**) y ningún ID cambia de ficha, así que la matriz de 02-tech-design §8 sigue cubriéndolos uno a uno.

| ID | Sev. | Decisión del PO | Qué cambió | Verificado contra |
|---|---|---|---|---|
| QA-02 | mayor | **Se acepta D-02.** El motor no tiene identificador de aplicación para Windows (el de escritorio es uno solo y lo necesita macOS con `.unity` por R-14). El objetivo de producto de R-14 se cumple igual: el nombre y la carpeta de datos no se cruzan con los del legacy | AC-APP-16-02 reescrito por plataforma: Android `com.iguisoft.rowmate`; iOS y macOS `com.iguisoft.rowmate.unity` (también el identificador único de escritorio); Windows sin identificador, producto `RowMate` de `iguisoft`, datos en `AppData/LocalLow/iguisoft/RowMate` (el legacy usa `AppData/Roaming/com.example/rower_app` para las preferencias y `.dart_tool/sqflite_common_ffi/databases/` para la base), `com.iguisoft.rowmate` reservado para el MSIX de la It. 14 y ningún `com.example`. Lo estático (`AppIdentityTests.ProvisionalIdsAndNoComExample`) cubre identificadores, empresa y producto; la carpeta y la convivencia son [Device] en la It. 2. También: §9 (fila R-14 / Q-85 + D-02), §7 fila 43, §12, §14. Fuera de la spec: ROADMAP §6 (fila R-14 / Q-85) y STATUS (decisión del gate de la It. 1) | `windows/runner/Runner.rc:92-98`; `main.dart:38-41`; `core/database/database_service.dart:21`; `path_provider_windows` 2.3.0 y `sqflite_common_ffi` 2.4.3 (versiones de `pubspec.lock`, leídas en el *pub cache*); 02-tech-design §1.5, §3.1 y D-02 |
| QA-11 | menor | **Tiene razón QA y el diseño (D-12).** | AC-RUT-17-01: sesión y tiempo transcurrido obligatorios y sin valor por defecto (el tiempo es el del monitor); SPM, paladas, distancia, split, potencia, calorías y pulso en 0 por defecto; índice y tipo del paso vacíos por defecto; id vacío hasta guardarse. Coincide con `DataPoint(long sessionId, int elapsedSeconds)` de 02-tech-design §3.3 | `workout_session.dart:154-166` (`required` en `:156-157`); `workout_provider.dart:162`, `:261`; fixture `_p(hr)` de `test/session_stats_test.dart:4` |
| QA-12 (1) | menor | **Se acepta P-06 como diferencia conocida** (por defecto). Afecta solo a quien tiene el sistema en un idioma que no es es ni en. El usuario puede pedir una barra de edición propia en el gate de la It. 2 | AC-APP-09-07 reescrito: los textos de la app (accesibilidad incluida) siguen el idioma elegido; el teclado y su menú de copiar y pegar siguen el del sistema. La verificación pasa a la **It. 3** [Device], porque el único campo de texto del legacy es el del editor de rutinas. Nueva P-09 (botón de volver `Atrás` / `Back`, que en el legacy pone Flutter) para los specs de la It. 3 y la 9. También: §9, §12, §13 | `main.dart:97-102`; `routine_editor_screen.dart` (único `TextField` de `lib/`); `flutter_localizations` `material_{es,en,pt}.arb` (`copyButtonLabel`, `pasteButtonLabel`, `backButtonTooltip`); 02-tech-design §10.3 |
| QA-12 (2) | menor | **P-04 agendada**: por defecto se adopta (lo impone Unity 6.3) y se le muestra al usuario | §13 P-04 respondida; §9 con la decisión; AC-APP-16-05 aclara que macOS y Windows no tienen criterio en la It. 1. ROADMAP §6: fila de Windows en el gate de la It. 2 y fila de macOS en el de la 10b | Manual de Unity 6.3, "System requirements" (player: Windows 10 21H1 build 19043, macOS 12, Android 7.1, iOS 15); legacy `macos/Runner.xcodeproj/project.pbxproj:560` (10.15) y `windows/runner/runner.exe.manifest` (Windows 10 y 11) |

**Pendientes para otros responsables** (no bloquean a WP-A, B ni C):

- **Architect**: en 02-tech-design §8, el motivo del `Ignore` de AC-APP-09-07 pasa de "→ It. 2" a "→ It. 3". Al cerrar la It. 1, ARCHITECTURE §16 (punto 1) y §21 (Q-85 / R-14) tienen que decir lo mismo que AC-APP-16-02 sobre Windows.
- **PO, al cerrar la It. 1**: FEATURE_INVENTORY Q-85 y R-14 se alinean con AC-APP-16-02.

`git -C ../rowmate-legacy status --porcelain` quedó vacío antes y después de la corrección. No se leyó `lib/core/strava/strava_secrets.dart`.
