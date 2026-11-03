# It. 5 — Gate de armonía, lente "paridad" (QA)

| Campo | Valor |
|---|---|
| Autor | rowmate-qa |
| Fecha | 2026-10-30 |
| Lente | **Paridad** con el legacy: `series_tracker.dart` y su test (12 casos), `series_panels.dart` y su test (3 casos más `formatos`), la integración en `workout_provider.dart` e `immersive_workout_screen.dart`, los textos (Q-01), Q-55 / DV-26 y los huecos del spec |
| Entrada | [01-po-spec](01-po-spec.md), [02-tech-design](02-tech-design.md), [04-design-notes](04-design-notes.md), [00-deuda-diseno](00-deuda-diseno.md), [00-deuda-4b](00-deuda-4b.md) |
| Legacy (solo lectura, `3ae0f3a`) | [rowmate-legacy](https://github.com/figuibej/rowmate-legacy): `lib/features/workout/series_tracker.dart` y `series_panels.dart` (enteros), `lib/features/workout/workout_provider.dart:40-330`, `lib/features/workout/immersive_workout_screen.dart:60-140` y `:550-700`, `lib/core/bluetooth/simulated_ble_service.dart:60-110`, `lib/l10n/app_es.arb` y `app_en.arb`, `test/series_tracker_test.dart` y `test/series_panels_test.dart` (enteros), `docs/superpowers/specs/2026-10-02-series-splits-design.md`; del caché de paquetes, `shared_preferences` 2.5.4 (la versión de `pubspec.lock`), `lib/src/shared_preferences_legacy.dart:117`, `:177-187`. No se leyó `lib/core/strava/strava_secrets.dart`. `git -C ../rowmate-legacy status --porcelain` vacío al empezar y al terminar |
| Código del repo (sin tocarlo) | `Core/Workout/{WorkoutEngine,SanitizedDistance,WorkoutFormats}.cs`, `Core/Models/StepPosition.cs`, `Tests/Support/RoutineSeeds.cs`, `Tests/EditMode/Workout/SeriesPanelsTests.cs`, `Localization/strings.{es,en}.json` |
| Verificación | Ver §1. Sin Unity. Sin *commits* |

---

## 0. Veredicto

**APROBADO CON OBSERVACIONES.**

- La lógica es un puerto fiel. El tracker, el motor que lo alimenta, la regla de qué muestra cada panel, las claves de preferencias y los 9 textos del catálogo coinciden con el legacy en cada regla, número y caso borde que revisé.
- Los números de la spec y del diseño se reproducen con un puerto propio del `SeriesTracker`, escrito desde el Dart e independiente del `sim.py` del Architect: **88 de 88 comprobaciones**.
- No hay bloqueantes. Quedan:
  - **2 mayores**: decisiones de presentación que se alejan del legacy y que el PO todavía no tomó; y tres de ellas, que contradicen un AC, no están en la lista del gate;
  - **8 menores**: textos de la spec que, tal como están, harían fallar una prueba; una expectativa del diseño que se contradice con las notas del Designer; un archivo de prueba marcado como nuevo que ya existe; y precisiones para el puerto de LT-59.

---

## 1. Cómo se verificó

1. **Puerto propio del tracker** (`<scratch>/it5/gate-paridad/tracker.js`): `series_tracker.dart:1-183` línea a línea (mismo orden de las operaciones de coma flotante, `floor`, la interpolación, `_enterStep` con la distancia y el reloj de la lectura anterior), más `formatLapTime`, `formatSplit` y `formatMeters` de `series_panels.dart:8-24`.
2. **Los 12 casos de `series_tracker_test.dart`** y la parte de reglas de `series_panels_test.dart` (`legacy_cases.js`), con los alimentadores `_Feeder` (`:20-42`) y `_Raw` (`:133-152`) reescritos tal cual: **31 de 31 afirmaciones**.
3. **Los números de la spec** (`spec_numbers.js`), con el modelo de tiempo de spec §0.4: el segundo `n` cae en el instante `n`; la lectura vigente es la última `L(k)` con `k − 0,5 ≤ w`; la pausa pierde la fracción (Q-58). Fin de paso con la regla de la app (metros saneados, DV-26) y, para comparar, con la del legacy (crudos, `workout_provider.dart:198-201`). Resultado: **57 de 57**. Cubre `Series 600`, `Trabajo 1000` con `R-4`, `-4R`, `-4Z` y `-4D`, `R-3`, `Fondo 3000` con `R-4A`, la pausa, `Cinco reps`, `Dos series`, `Pirámide 500`, `Prueba 4a`, `Corta` con `R-4` y `R-4P`, `R-4D` en `Series 600` y la suspensión de 600 s.
4. **El `sim.py` del Architect** (`<scratch>/it5/arch/`): corre con `uv run --offline --no-project python` y da lo mismo que mi puerto en cada número que imprime.
5. **dotnet, línea de base**: `run-tests.ps1 -Filter "FullyQualifiedName~SanitizedDistance|FullyQualifiedName~SeriesPanelsTests|FullyQualifiedName~WorkoutFormats"` dio `Total tests: 52 · Passed: 52`. Ahí están el saneamiento de la 4a y LT-45 (`formatos`).
6. **El catálogo**: 214 claves por idioma hoy. Ninguna de las 9 nuevas existe y `hudUnitMeters` = `m` en los dos idiomas. El `·` del legacy es U+00B7 (bytes `c2 b7` en `series_panels.dart:315`).

---

## 2. Muestreo: casos del legacy contra la spec y el diseño

Cada fila es un comportamiento del legacy corrido con el puerto, con lo que dicen la spec y el diseño. **OK** = las tres cosas coinciden.

| # | Caso (legacy) | Lo que da el legacy (puerto) | Spec | Diseño | |
|---|---|---|---|---|---|
| 1 | LT-47 `:46` interpolación a 3 m/s | 1 vuelta de 166,667 s (`2:46.7`); vuelta en curso de 10 m y 3,333 s (`0:03.3`) | AC-WKT-07-04, -05; CAP-09 | STR, §2.2 | OK |
| 2 | LT-48 `:59` dos vueltas | [100, 100] | AC-WKT-07-02 | STR | OK |
| 3 | LT-49 `:65` menos de 500 m | sin vueltas | AC-WKT-07-02 | STR | OK |
| 4 | LT-50 `:71` el enfriamiento | no es trabajo, 0 m, en serie, rep 1/5, 620 m | AC-WKT-07-01, -05, AC-WKT-08-01, -02 | STR | OK |
| 5 | LT-51 `:85` resumen | rep 2 con 50 m; resumen (1, 620, 500, 100 s), split 100 | AC-WKT-08-03, -04, -06 | STR | OK |
| 6 | LT-52 `:102` tope de 3 | [4, 3, 2] | AC-WKT-08-04 | STR | OK |
| 7 | LT-53 `:112` serie nueva o suelto | 1 → 0 → 0, y fuera de la serie | AC-WKT-08-05 | STR | OK |
| 8 | LT-54 `:126` sin metros de trabajo | split nulo → `—` | AC-WKT-08-06 | STR | OK |
| 9 | LT-55 `:155` lectura de 0 | nunca negativo; vueltas [125, 125]; trabajo de la rep 1 = 1096 m (entre 1060 y 1110) | AC-WKT-07-06 | STR, SAN | OK |
| 10 | LT-56 `:180` primer salto a 5000 | sin vueltas; después, 1 de 125 s | AC-WKT-07-06, -08 | STR | OK |
| 11 | LT-57 `:191` reinicio del monitor | los metros de la repetición nunca bajan; primera vuelta de 125 s | AC-WKT-07-06, AC-WKT-08-02 | STR | OK |
| 12 | LT-58 `:211` `rebase` | 20 → 24 | AC-WKT-07-07 | STR | OK |
| 13 | LT-59 `series_panels_test.dart:39` tracker a 300 m | `Rep 1/3 · 300 m`. Ojo: la única lectura (300 m con Δt = 60) **suma**, porque 300 ≤ 10 × 60 | AC-WKT-18-02 a -10 | SPP, SPV | OK, con QA-G5P-05 |
| 14 | LT-61 `:68` sin grupo | `inSeries` falso: sin `Reps` | AC-WKT-18-01 | SPN, SPV | OK |
| 15 | `formatos` `:32` (LT-45, 4a) | `2:46.7`, `1:05.0`, `1:54`, `1.245 m` (redondeo de Dart, la mitad hacia arriba) | AC-WKT-11-09 a -11 | `WorkoutFormats` (`DartMath.Round`), ya en verde | OK |
| 16 | La lectura del segundo en que termina el paso es de ese paso (`workout_provider.dart:176-187`) | `Series 600` a los 180 s: `0:25.0` `100 m`, `#1 2:05.0`, `Rep 1/3 · 600 m`; a los 181, fuera de un trabajo y `604 m` | AC-WKT-07-03, AC-WKT-18-08 (P-01); CAP-06 | §2.3, §2.4 | OK |
| 17 | Trabajo → trabajo vacía la lista (`:179-181`) | `Pirámide 500`: `#1 2:05.0` a los 725 s; el de 250 m (726-788) sin vueltas | AC-WKT-07-02 | STR | OK |
| 18 | Dos pasos sueltos seguidos no son repetición nueva (`:149-151`) | `repStart` no se reinicia: en el enfriamiento de `Series 600` los metros siguen, pero no se ven | AC-WKT-08-03 | STR | OK |
| 19 | La última repetición no se compara (`:153-174`) | `Corta` termina a los 30 s con 1 resumen y la rep 2 en curso | AC-WKT-08-07 | STR | OK |
| 20 | `Simular desconexión` (`simulated_ble_service.dart:81-103`) | 3 s sin lecturas y la física quieta; la primera lectura nueva sigue desde los mismos metros (Δ normal, suma) | AC-BLE-15-09; demo (a) 7 | SSM, WSM | OK |
| 21 | Preferencias: cerrados por defecto, guardar en el acto, el toque antes de leer gana (`series_panels.dart:120-156`) | `getBool(...) ?? false`; `_lapsTouched` / `_repsTouched` por panel | AC-WKT-18-09, -10 | SPP, §2.7 | OK, con QA-G5P-07 |
| 22 | Los paneles solo con rutina (`immersive_workout_screen.dart:105-106`) y debajo de SPM a 10, el segundo a 8 (`:630-633`, `series_panels.dart:179`) | — | AC-WKT-18-01, -13 | §3.3; la distancia a SPM es **8·s** (SD-05) | Lugar OK; la distancia, QA-G5P-01 |

**Otros números de la spec que confirmé** (todos OK):

- `Series 600`: los pasos terminan en 30, 180, 200, 350, 370, 520, 540 y 570; `#1` a los 155; `560 m` a los 170; `640 m` a los 190; `668 m` a los 197; `680 m` a los 200; `Rep 1` (680, 600, 150 s, `2:05`) y `Rep 2/3 · 4 m` a los 201; `0:50.0` `200 m` a los 250; `0:52.0` `208 m` a los 252; `0:00.0` `0 m` a los 325 y `0:01.0` `4 m` a los 326; `0:15.0` `60 m` y `Rep 2/3 · 560 m` a los 340; [`Rep 2`, `Rep 1`] a los 371; `0:10.0` `40 m` y `Rep 3/3 · 40 m` a los 380; fuera de la serie a los 541 y 545; con la rutina terminada, el enfriamiento (AC-WKT-18-20).
- `Trabajo 1000`: termina en 250, 251, 252 y 254 con `R-4`, `-4R`, `-4Z` y `-4D`. `#1` mide 125, 126, 127 y 129 s, y `#2`, 125 s. La vuelta 2 cierra en el mismo segundo en que termina el paso (AC-WKT-07-09).
- `Fondo 3000` con `R-4A` a los 600 s: `0:50.0` `250 m` y `#5 1:40.0`, `#4 1:40.0`, `#3 1:40.0`, `#2 2:05.0`.
- La pausa de 20,7 a 50,7: `0:20.0` `80 m`, `0:21.0` `84 m` y `#1` a los 125 s.
- `Cinco reps`: la rep `r` empieza a los `1 + 135 (r − 1)` s; a los 541 s, [`Rep 4`, `Rep 3`, `Rep 2`] con `2:05` y `540 m`; a los 545, `0:05.0` `20 m` y `Rep 5/5 · 20 m`.
- `Dos series`: a los 120, 121, 181 y 241 s, lo que dice AC-WKT-08-05.
- `Pirámide 500`: a los 879 s, `Rep 1 2:05 1.112 m` (752 m y 188 s); a los 880, `0:02.0` `8 m`; a los 881 (DT5-04), `0:03.0` `12 m` y `Rep 2/4 · 12 m`.
- `Prueba 4a`: a los 90 s, `1:00.0` `240 m` y `Rep 1/2 · 240 m`; a los 121, `Rep 1 2:05 360 m`.
- `Corta`: a los 16 s, `Rep 1 2:05 60 m`, y con `R-4P`, `Rep 1 — 0 m`.
- `R-4D` en `Series 600`: 154 s y 600 m dan `2:08`.
- La suspensión de 600 s a los 250,4: `0:50.0` `200 m`, después `0:51.0` `200 m` (el salto no suma) y `0:52.0` `204 m`.

---

## 3. El tracker (`series_tracker.dart`), regla por regla

| Regla del legacy | Línea | Spec | Diseño | |
|---|---|---|---|---|
| Vuelta = 500 m desde el inicio del paso, solo en trabajo | `:40`, `:73-78`, `:129-141` | AC-WKT-07-01, -02 | §2.2 | OK |
| Interpolación `t0 + (marca − d0) / dd · dt`, en ese orden; con `dd = 0`, `t` | `:132-140` | AC-WKT-07-04 | §2 (ADR 012: mismo orden), §2.2 | OK |
| Saneamiento: `Δ < 0` o `Δ > 10 · máx(dt; 1)` no suma, el tiempo sí, referencia nueva | `:43-46`, `:120-127` | AC-WKT-07-06 | §2.1 (sin cambios; `DistanceSample` da lo de antes) | OK |
| `begin` con la distancia cruda y el reloj activo; `rebase` toca solo el crudo | `:86-108` | AC-WKT-07-07, -08 | §2.1 a §2.3 | OK |
| Al entrar a un paso: inicio y fin de vuelta = la distancia y el reloj **de la lectura anterior** | `:176-181` | AC-WKT-07-04 | §2.2 | OK |
| Repetición nueva si cambia la serie o el número; resumen al frente con la misma serie; tope de 3; si no, se vacía | `:147-174` | AC-WKT-08-03 a -05 | §2.2 | OK |
| Metros de trabajo = lo que suma; segundos de trabajo = **todos** los Δt (también los de un salto) | `:129-131` | AC-WKT-08-06 | §2.2 | OK |
| `⌊ ⌋` en la vuelta en curso, la repetición y el resumen | `:74`, `:83`, `:161-162` | AC-WKT-07-05, AC-WKT-08-02, -04 | §2.2 (`(int)Math.Floor`, siempre ≥ 0) | OK |
| `inSeries` = `groupId != null`; `rep` y `repCount` en 0 antes de la primera lectura | `:80-82` | AC-WKT-08-01; DV-48 | `SeriesState`, `SeriesPanels.For` | OK (DV-48 es de presentación: el oráculo compara el tracker crudo) |
| Comparación de `groupId` con `!=` de Dart | `:150`, `:155` | — | `StepPosition` con `StringComparison.Ordinal` | OK |

---

## 4. Los paneles (`series_panels.dart`), pieza por pieza

**P** = paridad. **DV** = diferencia aceptada o para aceptar. **SD** = contradicción spec ↔ diseño que el gate tiene que decidir. **—** = no está en ninguna lista.

| Pieza | Legacy | Spec | Diseño (00-deuda-diseno, 04-design-notes) | |
|---|---|---|---|---|
| Qué filas, en qué orden: las 4 últimas, la más reciente arriba | `:279-282` | AC-WKT-18-04 | `SeriesPanels.For` (`Lap0`..`Lap3`) | P |
| Fuera de un trabajo, `Sin parciales en este paso` (15, blanco al 54 %) | `:55`, `:286-287` | AC-WKT-18-05 | `body` 15, `text.tertiary`, en dos renglones equilibrados (SD-13) | P (el corte, SD-13) |
| Línea `Rep r/R · m`, también fuera de un trabajo, solo en serie | `:313-317` | AC-WKT-18-06 | igual | P |
| Repeticiones: `Rep n`, split o `—`, metros totales a la derecha, en columnas y sin `·` | `:323-347` | AC-WKT-18-07 | igual; metros 13/700 (SD-04) | P (tamaños: SD-04) |
| Sin repeticiones: `Sin repeticiones completas` | `:330-333` | AC-WKT-18-07 | igual, en dos renglones | P |
| Panel de repeticiones solo en serie; conserva su estado sin verse | `:178`, `:128-129` | AC-WKT-18-01, -10 | igual | P |
| Toque en todo el panel; alterna y guarda en el acto | `:171-175`, `:186-190`, `:220-222` | AC-WKT-18-09 | igual (+ arrastre de 8 dp) | P |
| Claves `immersive.lapsExpanded` / `repsExpanded`, cerrados por defecto | `:120-121`, `:145`, `:148` | AC-WKT-18-10 | `SeriesPanelPrefs.LapsKey` / `RepsKey` | P |
| Ancho cerrado `minWidth 130`, abierto **200 fijo** | `:225-227` | 140 / 200 | **137 fijo**, cerrado y abierto | SD-01, SD-02 |
| A 10 de SPM; el segundo a 8 | `immersive_workout_screen.dart:631`; `series_panels.dart:179` | 10·s (paridad) y 8·s | **8·s** y 8·s | SD-05 |
| Chip: ícono 14 al 80 %, rótulo 13/600, *letterSpacing* 0,8, **color al 80 %**, flecha 18 al 54 % | `:34-39`, `:249-259` | AC-WKT-18-02 | ícono al 80 %; rótulo 13/600 con 0,08 em **al 100 %**; flecha dibujada al 54 % | **—** (QA-G5P-02 c) |
| Título abierto = **el estilo del chip** (13/600, 0,8, al 80 %), en una línea con `…` | `:240-245` | AC-WKT-18-03: "con el estilo del chip" | **11/600, sin *tracking***, a 0,949 en español; se achica hasta 0,8 y recién después `…` | **—** (QA-G5P-02 a) |
| Vuelta en curso: el tiempo 30/800 ámbar con `…`; `{n} m` 14/500 al 55 %, **a 6 del tiempo**, por la línea de base | `:290-304` | AC-WKT-18-04 ("al lado"); DV-49 | `num.lap` 28; metros `num.xs` 13 al 80 % (SD-03), **pegados a la derecha (x 127)** | Tamaños: SD-03. Posición: **—** (QA-G5P-02 b) |
| Filas: 2 arriba y 2 abajo de cada una (4 entre filas); rótulo en 52, blanco al 60 %, 15/600; valor 22/800 | `:351-390` | AC-WKT-18-04: "2 entre filas", 52, 15/600 | 2 entre filas; rótulo 12/600 `text.secondary` en el ancho de `#99`; `num.row` 20 | SD-03 (el "2 entre filas" de la spec no es el del legacy: QA-G5P-09) |
| La preferencia se lee después del primer dibujo | `:134-151` | §11.1.1 | lectura sincrónica en el arranque | DV informativa (OK) |
| Nada se corta: el legacy corta con `…` y parte `Rep 100` y la línea | `:294-299`, `:315-316`, `:372-385` | DV-49 | `FitScale` (§4.7) | DV (gate, P-13) |

---

## 5. La integración (`workout_provider.dart`, `immersive_workout_screen.dart`)

| Legacy | Línea | Spec | Diseño | |
|---|---|---|---|---|
| `begin(distancia actual, reloj activo)` al iniciar la rutina, con el reloj en 0 (el inicio siempre viene de `reset()`, `workout_screen.dart:310`, `:554`) | `:128` | AC-WKT-07-08 | `_series.Begin(_fusion.Data.DistanceMeters, 0)` | OK |
| `update` en el segundo, después de sumar los relojes y **antes** de los objetivos y del fin del paso | `:154-186` | AC-WKT-07-03 | §2.3 (el orden de `ProcessSecond` de la 4a se conserva) | OK |
| Solo con rutina; nada en pausa ni después del fin | `:151-152`, `:176`, `:210-214` | AC-WKT-07-03 | §2.3 | OK |
| `rebase` al reanudar | `:248` | AC-WKT-07-07 | §2.3 (`Resume`) | OK |
| `begin(0)` en `reset()` | `:315` | AC-WKT-07-11 | `ReturnToIdle` | OK |
| Paneles con `w.routine != null`, también en pausa y con la rutina terminada (hasta el `reset()` del diálogo) | `immersive_workout_screen.dart:105-106` | AC-WKT-18-01, -20 | foto `Series` con `_steps != null && _phase != Idle`; `Freeze` en la salida | OK |
| La columna termina arriba de la banda (vertical: margen + 200; horizontal: + 16 + 66 + 8) y se desplaza (`Flexible` + `SingleChildScrollView`) | `:611-633` | AC-WKT-18-13 | `SeriesPanelGeometry` (techo, piso y las tres reglas) | OK en la regla; números en SD-05 |
| Los valores cambian solo en el segundo, aunque el HUD se redibuje con cada lectura | `:220-224` | AC-WKT-18-08 | caché por `Revision` (§2.5) | OK |
| Las fallas de `shared_preferences`: `setBool` actualiza el caché en memoria antes de escribir (`shared_preferences_legacy.dart:177-187`); `getBool` de otro tipo lanza (`:117`) y los paneles quedan cerrados | — | AC-WKT-18-11 | el valor en memoria queda; otro tipo → cerrados | OK en el diseño; el texto de la spec, QA-G5P-07 |

---

## 6. Textos (Q-01)

El legacy no tiene claves para los paneles: ninguna en `app_es.arb` ni en `app_en.arb`. Los textos están fijos en `series_panels.dart`:

| Clave (spec §11.2) | Literal del legacy | |
|---|---|---|
| `hudLapsChip` `500 m` | `:168` `'500 m'` | OK |
| `hudLapsTitle` `Parciales 500 m` | `:169` | OK |
| `hudLapsEmpty` `Sin parciales en este paso` | `:287` | OK |
| `hudLapNumber` `#{number}` | `:308` `'#${lap.number}'`, sin separador de miles (`#1999`) | OK |
| `hudRepProgress` `Rep {rep}/{count} · {meters}` | `:315`: `·` = U+00B7; `rep` y `count` sin separador; `meters` con `formatMeters` | OK |
| `hudRepsChip` `Reps` | `:183` | OK |
| `hudRepsTitle` `Series anteriores` | `:184` | OK |
| `hudRepsEmpty` `Sin repeticiones completas` | `:332` | OK |
| `hudRepLabel` `Rep {rep}` | `:338` | OK |
| (reuso) `hudUnitMeters` `m` | `:301` `'${t.currentLapMeters} m'` (0 a 499: nunca lleva separador) | OK |
| (formato) `—` U+2014 | `:340` | OK (`WorkoutFormats.NoRepSplit`) |

No queda ningún literal visible del legacy afuera (el `HH:mm` de `WallClock` es de la 4b). Hoy hay 214 claves por idioma, así que 214 + 9 = 223 (AC-APP-10-12). El documento de diseño del legacy (`docs/superpowers/specs/2026-10-02-series-splits-design.md`) describe `▸ 340 m · 1:15.0` y `Rep 4 · 1:54 · 741 m`, pero el código no lo hace así. La spec sigue al código, que es el contrato: está bien.

---

## 7. Q-55 / DV-26

- **Un solo saneamiento** (spec §12 punto 2). El diseño lo hace dueño al `SeriesTracker` (DT5-06) y lo fija con `PureCodeRulesTests`: el motor no contiene `new SanitizedDistance` ni la fórmula del salto, y llama a `_series.Update` una sola vez. Es correcto: el paso por distancia y la vuelta `n` de un trabajo de `500 × n` m cierran en el mismo segundo con cualquier remo (confirmado con `R-4`, `-4R`, `-4Z` y `-4D`).
- **DT5-07 es necesario**, y lo confirmé. Con la regla del legacy (la distancia cruda), el mismo `Trabajo 1000` da otra secuencia de pasos:
  - `R-4R` no termina hasta los **510 s**, con 4 vueltas en un trabajo de 1000 m;
  - `R-4Z` y `R-4D` terminan a los **250 s** con solo `#1` y 492 / 484 m en la vuelta en curso.

  Correr el `WorkoutProvider` daría otro oráculo. El enfoque del diseño (las entradas del tracker con la regla de la app, y `TheEngineFeedsTheOracleInputs` para atar el motor a esas entradas) compara lo que hay que comparar.
- **QA-V4a-L01** (un monitor que no manda la distancia una vez por segundo) no cambia en la 5. Con la misma secuencia de pasos, los parciales del legacy ya se saneaban igual. El riesgo sigue siendo el del paso, en la 10a.
- **Lo que falta escribir:** el efecto visible de DV-26 en los paneles con un monitor que falla (QA-G5P-08).

---

## 8. Huecos del spec (lo del legacy que la spec no dice o dice distinto)

La spec es muy completa: cubre todas las reglas del tracker, la alimentación, los paneles, las preferencias, los textos y los casos borde que encontré (trabajo por tiempo, trabajo → trabajo, sueltos seguidos, serie ×1, la última repetición, el paso de 0 s, la lectura que no llega). Lo que encontré:

1. **La pausa**: "los 120 m remados en pausa" son 124 m de lecturas (QA-G5P-06).
2. **El guardado que falla**: el legacy y el diseño conservan el valor tocado en la misma ejecución (QA-G5P-07).
3. **DV-26 en los paneles** (QA-G5P-08).
4. **El aire entre filas**: el legacy deja 4 (2 + 2) y la spec dice 2 (QA-G5P-09). Además, el legacy deja siempre 4 debajo de la vuelta en curso, aun sin vueltas (`:305`), y el diseño solo con vueltas. Es menor y del sistema visual.
5. **La fila de 52**: la columna del rótulo del legacy (`:372-379`) es fija. El diseño la ajusta al ancho de `#99` o del más ancho (SD-03 y SD-04, en la lista).

---

## 9. Issues

| ID | Sev. | Resp. | Issue | Evidencia | Corrección sugerida |
|---|---|---|---|---|---|
| QA-G5P-01 | mayor | po | **SD-01 a SD-14 sin decidir, y varias se alejan del legacy en lo que se ve:**<br>- el panel abierto pasa de 200 a 137 base fijo;<br>- la distancia a SPM, de 10 a 8·s;<br>- varios tamaños quedan **por debajo del legacy**: `#n` de 15 a 12, el valor de fila de 22 a 20, los metros de la repetición de 16 a 13, la vuelta en curso de 30 a 28 y sus metros de 14/500 al 55 % a 13/700 al 80 %.<br>La fila informativa de §11.1.1 promete los tamaños de ART_BIBLE (`num.lap` 34, `num.row` 24), más grandes que el legacy, y ya no describe lo que se va a construir. El Architect construye con la columna del Designer mientras el PO no decide (02-tech-design §8.2) | `series_panels.dart:225-227`, `:298`, `:301-302`, `:372-387`; `immersive_workout_screen.dart:631`; 02-tech-design §8.2; 04-design-notes §3 y §11.1; 00-deuda-diseno §8.1 | Decidir SD-01 a SD-14 **antes de WP-B1**. Si se aceptan:<br>- reescribir AC-WKT-18-02, -03, -04, -07 y -13 con los números nuevos;<br>- actualizar §11.1.1;<br>- presentar al usuario, con las DV, el panel más angosto y los textos más chicos que el legacy, con la tabla de legibilidad de 04-design-notes §8 |
| QA-G5P-02 | mayor | designer | **Tres contradicciones con un AC o con el legacy que no están en la lista del gate:**<br>(a) el título abierto va en 11/600, sin *tracking* y a 0,949 en español; AC-WKT-18-03 pide "con el estilo del chip", y en el legacy es el mismo `_labelStyle` del chip (13/600, 0,8, al 80 %);<br>(b) los metros de la vuelta en curso van pegados a la derecha (x 127); en el legacy van a 6 del tiempo (`SizedBox(width: 6)`), y AC-WKT-18-04 dice "al lado";<br>(c) el rótulo del chip va al 100 %; en el legacy es el color al 80 % (`_labelStyle`, checklist §9 #23) | `series_panels.dart:34-39`, `:240-245`, `:253-255`, `:294-302`; 04-design-notes §3 (tabla), §4.3, §4.4; 00-deuda-diseno §3.4 (`Título · Inter 600, 11`) | Sumarlas como SD-15 a SD-17 en 04-design-notes §11.1, con el motivo (el ancho de 137, que los metros no "bailen", el contraste), para que el PO las decida con las demás. Si no, seguir la spec |
| QA-G5P-03 | menor | architect | `HudSeriesChange`: 02-tech-design §2.5 espera `ContentSwap` a los 541 s de `Series 600`, pero ahí se pasa de un descanso a un enfriamiento, los dos fuera de un trabajo. Su propia definición ("trabajo ↔ fuera de trabajo, o trabajo nuevo") y DD5-18 ("en `Series 600` a los 541, el vacío no se mueve") dicen que el contenido de parciales no cambia: solo hay `SeriesEnded`, y se van la línea `Rep` y el panel de repeticiones. HSC fallaría o forzaría un fundido que el Designer no quiere | 02-tech-design §2.5 (último punto); 04-design-notes §6.3 y DD5-18; puerto: a los 540 y 541, `work = false` | Corregir la expectativa de HSC a los 541 (`SeriesEnded`, sin cambio de clase en parciales) y llevar `ContentSwap` a la definición por pieza de DD5-18 |
| QA-G5P-04 | menor | architect | `Tests/EditMode/Workout/SeriesPanelsTests.cs` figura como **N** en §6.3, pero existe desde la 4a con LT-45 (`TestName = "formatos"`, `Category = "LT-45,AC-WKT-11-09,AC-WKT-11-10,AC-WKT-11-11"`). Crearlo de nuevo borraría LT-45 y la trazabilidad de la 4a | `Tests/EditMode/Workout/SeriesPanelsTests.cs:1-20`; 02-tech-design §6.3 (fila SPN) | Marcarlo **C**, sumar los casos de SPN al archivo y dejar `formatos` intacto |
| QA-G5P-05 | menor | architect | **El puerto de LT-59 y LT-60.** El legacy arma el tracker con **una** lectura de 300 m a los 60 s (`series_panels_test.dart:12-22`), que suma porque 300 ≤ 10 × 60. Con esa sola lectura, la vuelta en curso queda en `1:00.0` `300 m`. El motor no puede dar un Δt de 60, y con `R-4` los 300 m llegan a los 75 s (`1:15.0`). El diseño dice "el tracker a 300 m" sin decir cómo. Además, `find.text` es una coincidencia exacta y no ve lo que no se dibuja | 02-tech-design §3.1 (la API de ranuras), §6.4 (LT-59); `series_panels_test.dart:12-29`, `:44-59` | SPP y SPV:<br>- `SeriesTracker.Begin(0)` y `Update(0, trabajo de 1000 m en g ×3, (g, 1, 3), 300, 60)`, como el legacy, y de ahí `SeriesPanels.For` y el *store* (no un `HudSeries` armado a mano);<br>- `IsTextVisible` con coincidencia exacta, que trate como no visible una ranura con alfa 0 (el título con el panel cerrado, el rótulo del chip con el panel abierto) |
| QA-G5P-06 | menor | po | AC-WKT-07-07 dice "los 120 m remados en pausa no suman". Con el modelo de §0.4, la referencia pasa de `L(20)` = 1080 a `L(51)` = 1204: no se cuentan **124 m**. Los 4 de más vienen de la fracción perdida (Q-58) y del medio segundo de las lecturas. Una prueba que afirme 120 falla | `spec_numbers.js` (la pausa); `sim.py` del Architect | Escribir "lo remado en pausa (las lecturas de `L(20)` a `L(51)`, 124 m) no suma", o sacar el número. Los otros números del AC están bien |
| QA-G5P-07 | menor | po | AC-WKT-18-11: "si guardar falla, … la sesión siguiente abre como quedó guardado la última vez". En el legacy, `setBool` actualiza el caché antes de escribir, así que la sesión siguiente de **la misma ejecución** abre con lo que se tocó. El diseño hace lo mismo ("el valor en memoria queda"). Lo guardado vale recién después de cerrar y abrir la app. Tal como está, la spec contradice al legacy y al diseño | `shared_preferences_legacy.dart:177-187` (2.5.4, `pubspec.lock`); 02-tech-design §2.7 | "…la sesión siguiente, en la misma ejecución, abre como quedó; después de cerrar y abrir la app, como quedó guardado la última vez" |
| QA-G5P-08 | menor | po | **DV-26 se ve en los paneles y no está escrito.** Con un monitor que falla, el legacy cierra el trabajo de 1000 m por la distancia cruda:<br>- `R-4Z` y `R-4D`: a los 250 s, con solo `#1` y 492 / 484 m en la vuelta en curso;<br>- `R-4R`: recién a los 510 s, con 4 vueltas.<br>La app (AC-WKT-07-09) cierra con `#1` y `#2` a los 252, 254 y 251. Es una DV aceptada en la 4a, pero en la 5 el usuario la ve en los paneles | `spec_numbers.js` (la regla del legacy); `workout_provider.dart:198-201`; spec §11 (la fila Q-55 / DV-26) | Una línea en §11.1, o en la fila Q-55 / DV-26, con este efecto, para que el usuario lo vea con las DV |
| QA-G5P-09 | menor | po | AC-WKT-18-04 dice "con 2 entre filas". En el legacy, cada `_MetricRow` tiene 2 arriba y 2 abajo (4 entre filas), como dice bien el checklist §9 #34. El Designer construye con 2 | `series_panels.dart:366-367`; 04-design-notes §4.4 | Corregir el texto del AC ("2 arriba y abajo de cada fila"), o anotar que el 2 entre filas es del sistema visual (con SD-03) |
| QA-G5P-10 | menor | po | Las tomas que el diseño corrió no están en la spec. Por DT5-04, CAP-11 va a los 881,2 s y DEMO-11 a los 31,6. Por SD-14, VID-08 arranca con parciales cerrado. Si se acepta SD-07, la demo (b) ya no puede decir "la cuenta grande no aparece esa vez". Si la spec no cambia, QA va a verificar contra textos viejos | 02-tech-design DT5-04; 04-design-notes R-15, SD-14; spec §1 (b), §7.3, §7.4, §7.5. CAP-11 a los 881: `0:03.0` `12 m` y `Rep 2/4 · 12 m` (confirmado) | Actualizar §1 (b), CAP-11, DEMO-11 y VID-08 al cerrar el gate |

---

## 10. Lo que queda sin test (según el Architect), visto desde la paridad

- **De acuerdo.** Ningún AC de paridad (F-WKT-07, F-WKT-08 y la parte de reglas de F-WKT-18) queda solo en el equipo o en QA. El oráculo (SGD) y STR, SEN y SPN cubren todo lo del tracker y de la alimentación.
- **Las partes que solo se ven en el equipo** no tocan la paridad numérica: el `[PERF]`, el teclado real, el arrastre con el dedo y el primer cuadro en Android, el reemplazo atómico con IL2CPP y AC-WKT-02-11 (el usuario).
- **Para el oráculo**: el guion de `Pirámide 500` tiene que pasar por los dos trabajos seguidos (726 s, trabajo → trabajo), y uno de `R-4X` tiene que cruzar la vuelta del contador de 24 bits, si `R-4X` la incluye.

---

## 11. Matriz AC (paridad) → test planificado → evidencia del gate

| AC | Test (02-tech-design §6.5) | Evidencia del gate |
|---|---|---|
| AC-WKT-07-01, -02 | STR, SPN | LT-49, LT-50 y `Pirámide 500` (725 / 726-788), con el puerto |
| AC-WKT-07-03 | SEN | los pasos de `Series 600` (30, 180, 200…) y 31-180 / 181-200 |
| AC-WKT-07-04, -05 | STR, SEN | LT-47; `Series 600` a los 170 y 180; `R-3` a los 170 |
| AC-WKT-07-06 | STR, SEN | LT-55 a LT-57; `R-4R`, `-4Z` y `-4D` (126 / 127 / 129 s) |
| AC-WKT-07-07 | STR, SEN | LT-58; la pausa (`0:20.0` `80 m`, `0:21.0` `84 m`, `#1` en 125); QA-G5P-06 |
| AC-WKT-07-08 | STR, SEN | LT-56 |
| AC-WKT-07-09 | SEN, PCR | fin y vuelta `n` en el mismo segundo (250 / 251 / 252 / 254) |
| AC-WKT-07-10 | SEN | la suspensión (200 → 200 → 204 m) |
| AC-WKT-07-11 | SEN, PIN | `begin` en el inicio y el reposo (§5) |
| AC-WKT-07-12 | SGD | puerto y `sim.py` coinciden; DT5-07 confirmado (§7) |
| AC-WKT-08-01 a -07 | STR, SPN | LT-50 a LT-54; `Cinco reps`, `Dos series` y `Corta` |
| AC-WKT-08-08 | SEN | `Series 600` (201, 371, 541), `Pirámide 500` (879), `Prueba 4a` (121), `Corta` (16, 30) |
| AC-WKT-18-01, -04 a -08 | SPN, WHS, HSC; SPV, IHT | los textos de §2 del muestreo; QA-G5P-03 |
| AC-WKT-18-02, -03 | SPV | QA-G5P-01, -02 |
| AC-WKT-18-09, -10 | SPP, SPV, BST | LT-59 y LT-60 (reglas); QA-G5P-05 |
| AC-WKT-18-11 | JFP, PFU, PIS | QA-G5P-07 |
| AC-WKT-18-22, AC-APP-10-12 | CAT, WHS, SPV | §6: las 9 claves contra los literales; 214 → 223 |
| AC-BLE-15-09 | SSM, WSM | `simulated_ble_service.dart:81-103` (§2, fila 20) |

Archivos de esta verificación (fuera del repo): `<scratch>/it5/gate-paridad/tracker.js`, `legacy_cases.js`, `spec_numbers.js` y los resultados de dotnet.
