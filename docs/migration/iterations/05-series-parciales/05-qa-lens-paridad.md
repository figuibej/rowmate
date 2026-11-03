# It. 5 — Verificación, lente "paridad" (QA)

| Campo | Valor |
|---|---|
| Autor | rowmate-qa |
| Fecha | 2026-10-30 |
| Iteración | 5 · `05-series-parciales` ([ROADMAP §It. 5](../../ROADMAP.md)) |
| Lente | **Paridad**: parciales, repeticiones y paneles contra el código del legacy (no contra los documentos). Corre en paralelo con la verificación principal (`05-qa-verification.md`) |
| Legacy (solo lectura, `3ae0f3a`) | [rowmate-legacy](https://github.com/figuibej/rowmate-legacy). Enteros: `lib/features/workout/series_tracker.dart`, `test/series_tracker_test.dart`, `lib/features/workout/series_panels.dart`, `test/series_panels_test.dart`, `lib/core/models/routine.dart:1-97`, `lib/features/workout/workout_provider.dart:40-330`; `lib/features/workout/immersive_workout_screen.dart:60-140` y `:640-760`; `shared_preferences` 2.5.4 (`lib/src/shared_preferences_legacy.dart:117-137`, del caché de paquetes). No se leyó `lib/core/strava/strava_secrets.dart` |
| Herramientas | **Sin Unity**. `tools/dotnet/run-tests.ps1` (y `-Matrix`) con su propia carpeta de artefactos en `<scratch>/it5/lens-paridad/`; el oráculo de series corrido otra vez desde el legacy con el Dart de `<flutter-sdk>` en una copia en `<scratch>/it5/lens-paridad/oracle/`; un generador al azar propio sobre el `SeriesTracker` del legacy y un proyecto de pruebas propio en `<scratch>/it5/lens-paridad/proof/` (ninguno va al repo). No se modificó ningún archivo del repo salvo este reporte |
| Veredicto | **APROBADO CON OBSERVACIONES**: 0 bloqueantes, **1 mayor**, 1 menor (§2) |

---

## 0. Veredicto

**APROBADO CON OBSERVACIONES.** 0 bloqueantes, **1 mayor** (QA-V5-P01, de una línea) y 1 menor de documentación (QA-V5-P02).

Lo que es del legacy es del legacy, y con más evidencia que la que pide la spec:

- **El tracker es el del legacy, bit a bit.** `Core/Workout/SeriesTracker.cs` y `SanitizedDistance.cs` copian `series_tracker.dart` línea por línea, con las expresiones de coma flotante en el mismo orden (ADR 012). Los 12 casos de `test/series_tracker_test.dart` (LT-47 a LT-58) están portados uno por uno, con el mismo nombre y las mismas afirmaciones (§3).
- **El oráculo de series es reproducible.** Lo corrí otra vez desde el legacy en `3ae0f3a`: el `series_tracker.json` que sale es **idéntico** al del repo salvo `generatedAt` (§1.2). `SeriesGoldenTests` pasa entero (7 de 7): los 12 casos, los 5 × 8 guiones de las rutinas por remo, `Trabajo 1000`, `Fondo 3000`, las pausas y el paso de 0 s; y el motor de la app le da al tracker exactamente las entradas de cada guion.
- **Fuera de los guiones, también.** Con 400 secuencias al azar generadas con el `SeriesTracker` del legacy (Δt de 0 y saltos de hasta 300 s, lecturas en 0, reinicios, retrocesos, saltos de 3000 m, `rebase` y `begin` a mitad, posiciones al azar), el tracker de la app da los **mismos 160 362 estados con igualdad exacta** (sin tolerancia), con un control positivo que sí detecta una lectura alterada en 1 m (§1.3).
- **Un solo saneamiento (Q-55, DV-26).** El motor ya no tiene el suyo: el paso por distancia lee `SeriesTracker.Meters` (`WorkoutEngine.cs:476`, `:502`), y el tracker es el dueño del único `SanitizedDistance` (`SeriesTracker.cs:198`). Los metros del paso son los mismos segundo a segundo con `R-4`, `R-4R`, `R-4Z`, `R-4D` y `R-4X` (§5).
- **El motor sigue idéntico al legacy.** `WorkoutGoldenTests` pasa entero (10 de 10), y `workout_engine.json` no cambió desde el cierre de la 4b (`d0bd574`).
- **Los paneles, en sus reglas**: solo con rutina, repeticiones solo en serie, las 4 últimas vueltas con la más reciente arriba, hasta 3 repeticiones, el segundo de atraso en el cambio de paso (P-01), los formatos `M:SS.d`, `M:SS` o `—` y `1.245 m`, cerrados por defecto, las claves `immersive.lapsExpanded` / `immersive.repsExpanded`, el toque antes de leer que gana. Los 9 textos nuevos son **el español exacto del legacy** (Q-01), con `·` = U+00B7 como en `series_panels.dart:315` (§4).

Encontré **un bug real** con escenario concreto y salida de prueba:

- **QA-V5-P01 (mayor) · después del segundo plano, `Series anteriores` puede mostrar el split de otra serie.** `WorkoutHudStore.RepRows` reusa las filas de la foto anterior si coinciden la repetición y los **metros totales**, sin mirar el split. Si la app pasa al segundo plano en una serie y vuelve en otra, y la `Rep n` de las dos tiene los mismos metros totales, la fila muestra el split de la serie vieja, y se queda así mientras la lista no cambie de largo. En mi sonda: `Rep 1 2:05 620 m` cuando la repetición es de `1:40` (el tracker lo tiene bien). El legacy arma las filas desde el tracker en cada dibujo y no puede quedar viejo. Falla AC-WKT-02-08 (cambia) y AC-WKT-18-07 en ese caso.

Y una diferencia con el legacy sin documentar, sin cambio de código:

- **QA-V5-P02 (menor, PO) · una clave de otro tipo.** En el legacy, `getBool` con un valor que no es booleano lanza (`as bool?`) dentro del `setState` de `_load`, así que con `immersive.lapsExpanded` dañada **ninguno** de los dos paneles lee su clave. La app cierra solo el de la clave dañada y lee la otra. Es mejor, pero no figura en §11.1.1 ni en §15, y el texto de AC-WKT-18-11 ("los paneles arrancan cerrados") se puede leer de las dos maneras.

---

## 1. Método y evidencia

### 1.1 dotnet (salida real)

`powershell -NoProfile -File tools/dotnet/run-tests.ps1 -ArtifactsPath <scratch>/it5/lens-paridad/artifacts`:

```
Build succeeded.
    0 Warning(s)
    0 Error(s)
...
Test Run Successful.
Total tests: 192
     Passed: 192                       (RowMate.Tests.Simulator)
...
Test Run Successful.
Total tests: 2536
     Passed: 2502
    Skipped: 34                        (RowMate.Tests.EditMode)
exit=0
```

En total, **2728 pruebas: 2694 pasan, 0 fallan y 34 ignoradas**, que son las de equipo (Android, Apple, el suspendido y el `[PERF]` de Windows, GitHub).

Después, con `-Matrix` y la misma carpeta:

```
[run-tests] matriz: <scratch>/it5/lens-paridad/artifacts/TestResults/ac-matrix.md (PASA 531, FALLA 0, IGNORADO 2, PENDIENTE UNITY 125, PENDIENTE WP-B 0, SIN TEST 0)
exit=0
```

En esa segunda corrida `Project.SimulatorInUnityTests.CommittedSettingsDoNotEnableTheSimulator` quedó **inconclusa** (2501 pasan en lugar de 2502): encontró la marca de una corrida de pruebas de Unity con simulador (`Library/RowMate/simulator-tests.txt`) que estaba haciendo en ese momento la verificación principal. En la primera corrida pasó. Es del entorno, no de la iteración.

Las de la lente, todas en verde: `SeriesTrackerTests` (28), `SeriesGoldenTests` (7), `SeriesEngineTests` (16), `SeriesPanelsTests` (8), `SeriesPanelPrefsTests` (9), `HudSeriesChangeTests` (12), `SanitizedDistanceTests` (9), `WorkoutGoldenTests` (10) y `SeriesSimulatorTests` (1, en `RowMate.Tests.Simulator`).

### 1.2 El oráculo de series, regenerado desde el legacy

Copié `tools/oracle/bin/series_vectors.dart` y `tools/oracle/lib/golden.dart` a `<scratch>/it5/lens-paridad/oracle/` y los corrí con el Dart de `<flutter-sdk>` y un `package_config` propio que apunta al legacy (solo lectura), con `--out` en el scratch:

```
dart --packages=<scratch>/…/pc.json <scratch>/…/bin/series_vectors.dart --out <scratch>/…/out --legacy-commit 3ae0f3a8bed5966700e830db3d8a5f050f34a0bc
[oracle] series_tracker.json: 865673 bytes
exit=0

diff <(grep -v generatedAt out/series_tracker.json) <(grep -v generatedAt Assets/RowMate/Tests/Golden/oracle/series_tracker.json)
IDENTICAL_EXCEPT_generatedAt
```

Revisé además el generador: corre `package:rowmate/features/workout/series_tracker.dart` (no una copia), reescribe los alimentadores privados del test (`_Feeder`, `:20-42`; `_Raw`, `:133-152`) con los mismos pasos y lecturas, y el modelo de tiempo de los guiones (lecturas en `k − 0,5`, pausas que pierden la fracción, Q-58) lo valida del lado de C# `TheEngineFeedsTheOracleInputs`, que exige que **cada llamada del motor al tracker** sea la del guion. La comparación (`SeriesGoldenTests.Compare`) mira todos los campos del estado del legacy: vueltas (número y segundos), si es trabajo, vuelta en curso (metros y segundos), serie, repetición, cantidad, metros de la repetición y las anteriores con repetición, total, trabajo, segundos y split.

### 1.3 Diferencial al azar contra el legacy

El oráculo cubre guiones con forma de entrenamiento. Para el resto del espacio de entradas escribí `<scratch>/…/oracle/bin/fuzz_series.dart`, que corre el `SeriesTracker` **del legacy** con 400 semillas: de 1 a 14 pasos con tipo y posición al azar (serie `null`, `a`, `b` o `c`, repetición y cantidad al azar: el tracker no valida coherencia), entre 50 y 750 llamadas cada una, con el reloj en +1 (85 %), +0 (5 %) o +2 a +301 (10 %); la distancia en +0 a +7, 0, +11 a +3010, un retroceso o algo cerca del tope de 10 m/s; `rebase` (3 %) y `begin` a mitad (0,5 %); y el paso que sigue igual, avanza uno o salta uno. Guarda el estado **completo** después de cada llamada. `FuzzTests` (en `<scratch>/…/proof/`, ensamblado con el nombre de las pruebas para ver lo interno) los reproduce con el `SeriesTracker` de la app y compara con **igualdad exacta** (sin el 1e-9 del oráculo):

```
estados comparados: 160362; casos: 400
  Passed TheTrackerMatchesTheLegacyOnRandomInputs [945 ms]
```

Cobertura de esas secuencias: 39 084 estados en trabajo, 8042 con vueltas (varias vueltas en una lectura con los Δt grandes), 18 853 con repeticiones anteriores (195 con las 3) y miles con split nulo. **Control positivo** (`ControlTests`): con la lectura número 10 de cada secuencia alterada en 1 m, la comparación detecta la diferencia en 374 de las 400 secuencias (las otras 26 la absorben porque esa lectura es un salto que no suma).

### 1.4 La sonda de QA-V5-P01

`<scratch>/…/proof/ProofTests.cs`: el motor de verdad (`WorkoutHarness`, `FakeScheduler`, `FakeAppLifecycle`) y el `WorkoutHudStore` de la app, con una rutina de dos series de la misma forma, A ×2 [trabajo 500 m; descanso 60 s] y B ×2 [trabajo 500 m; descanso 60 s], y un remo que hace la `Rep 1` de A a 4 m/s (125 s) y la `Rep 1` de B a 5 m/s (100 s), con los descansos a 2 m/s (las dos suman **620 m**). La prueba de control corre todo en primer plano; la otra pasa al segundo plano a los 200,25 s (en la rep 2 de A, la lista es `[Rep 1 de A]`) y vuelve a los 540,25 s (en la rep 2 de B, la lista es `[Rep 1 de B]`):

```
Passed Control_Foreground_B1ShowsItsOwnSplit
  200,25 s: Rep 2 (Rep 2/2 · 60 m), filas: [Rep 1 2:05 620 m]
  540,25 s: Rep 2 (Rep 2/2 · 40 m), filas: [Rep 1 1:40 620 m] | tracker: rep 1, 620 m, trabajo 500 m en 100 s -> 1:40

Failed Background_FromA2ToB2_B1ShowsItsOwnSplit
  200,25 s (A rep 2): Rep 2 (Rep 2/2 · 60 m), filas: [Rep 1 2:05 620 m]
  540,5 s (B rep 2, al volver): Rep 2 (Rep 2/2 · 40 m), filas: [Rep 1 2:05 620 m] | tracker: rep 1, 620 m, trabajo 500 m en 100 s -> 1:40
  560,25 s (20 s después, en primer plano): Rep 2 (Rep 2/2 · 120 m), filas: [Rep 1 2:05 620 m]
  el split de la rep 1 de la serie B
  Expected: "1:40"
  But was:  "2:05"
```

---

## 2. Issues

| ID | Severidad | Responsable | Issue | Evidencia | Corrección sugerida |
|---|---|---|---|---|---|
| **QA-V5-P01** | **mayor** | architect | **Al volver del segundo plano, una fila de `Series anteriores` puede quedar con el split de otra serie, y no se corrige sola.** `RepRows` reusa la lista anterior si tiene el mismo largo y, fila por fila, la misma repetición y los mismos metros totales; no mira el split (ni los metros ni los segundos de trabajo). Dentro de una serie eso alcanza (los números de repetición no se repiten), pero entre dos series, no: en primer plano siempre hay un segundo con la lista vacía entre las dos (`series_tracker.dart:167-170`), pero en segundo plano el motor no publica fotos (`WorkoutEngine.cs:784-793`, `Notify`; `WorkoutHudStore.cs:15-17`) y al volver el store compone **una** foto contra la última que vio. Si la `Rep n` de las dos series suma los mismos metros totales (trabajo + descanso) y el trabajo tardó distinto, la fila sigue mostrando el split viejo; la próxima foto también la reusa, hasta que la lista cambie de largo (en una serie ×2, hasta salir de la serie). El legacy no puede quedar viejo: `_RepsContent` arma las filas desde `tracker.previousReps` en cada dibujo (`series_panels.dart:330-345`). Falla AC-WKT-02-08 (cambia: "las repeticiones que se cerraron en segundo plano están en el primer cuadro") y AC-WKT-18-07 en ese caso. Es un dato equivocado en la pieza que sirve para "regatear contra uno mismo". Necesita segundo plano de verdad (Android e iOS; en escritorio, sin foco no es segundo plano: `AppLifecycle.cs:7-9`) o un alcance que cruce una serie entera, y la coincidencia de los metros totales: es raro, pero el arreglo es de una línea | Sonda §1.4: `Rep 1 2:05 620 m` al volver y 20 s después, con el tracker en `1:40`; el control en primer plano da `1:40`. Código: `App/Workout/WorkoutHudStore.cs:350` (le pasa las filas anteriores de la misma sesión, de cualquier serie), `:404-422` (compara solo `Rep` y `MetersValue`), `:454` (la foto anterior). `SeriesEngineTests.TheBackgroundGivesTheSameAsTheForeground` compara solo las llamadas del motor, no el store: por eso no lo vio | Reusar la lista solo si las filas se armaron desde **la misma instancia** de `PreviousReps` (el tracker la reemplaza en cada cambio: copia al escribir, `SeriesTracker.cs:311-321`), o comparar el `RepSummary` entero (`Equals` ya existe, `SeriesTracker.cs:61-64`). Sumar a `WorkoutHudStoreTests` la regresión de la sonda: dos series de la misma forma, segundo plano de 200,25 a 540,25 s, y `Reps[0].Split` igual a `RepSplit` del tracker |
| **QA-V5-P02** | menor | po | **Una clave de otro tipo: el legacy no lee ninguna; la app lee la otra.** En `shared_preferences` 2.5.4, `getBool` es `_preferenceCache[key] as bool?` (`shared_preferences_legacy.dart:117`): con un valor que no es booleano lanza un `TypeError`. En `_load` (`series_panels.dart:140-151`) eso pasa dentro del `setState`: con `immersive.lapsExpanded` dañada, **los dos** paneles quedan cerrados aunque `immersive.repsExpanded` esté en verdadero (y queda un error sin atrapar en el registro); con `immersive.repsExpanded` dañada, parciales ya tomó su valor antes de la excepción y se ve en la reconstrucción siguiente. La app cierra solo el panel de la clave dañada y lee la otra (`SeriesPanelPrefs.cs:100-105`; `SeriesPanelPrefsTests.AKeyOfAnotherTypeIsClosed`). La app hace lo más sensato y no hay que tocar código, pero la diferencia no está en §11.1.1 ni en §15, y AC-WKT-18-11 dice "la clave tiene otro tipo, los paneles arrancan cerrados", que se puede leer como el legacy | `shared_preferences_legacy.dart:117`; `series_panels.dart:143-150`; `App/Workout/SeriesPanelPrefs.cs:100-105` | Precisar AC-WKT-18-11 ("ese panel arranca cerrado; el otro, con su clave") y sumar una fila informativa a §11.1.1 (o a §15) con la diferencia. Sin cambio de código |

---

## 3. Los casos legacy de la iteración

Revisé cada prueba contra su caso del legacy: los alimentadores, los pasos, las lecturas y cada afirmación.

| LT | Caso legacy | Port | Mismos casos y afirmaciones | Resultado |
|---|---|---|---|---|
| LT-45 | `series_panels_test.dart:32` `formatos` | `SeriesPanelsTests.formatos` (de la 4a) | Sí: `2:46.7`, `1:05.0`, `1:54`, `1.245 m` | PASA |
| LT-47 | `series_tracker_test.dart:46` | `SeriesTrackerTests` (mismo nombre) | Sí: `_Feeder` con `begin(0)`, `⌊d⌋`, `t` desde 1; 1 vuelta, n.º 1, 166,67 ± 0,01, 10 m, 3,33 ± 0,01, trabajo | PASA |
| LT-48 | `:59` | ídem | Sí: `[100, 100]` exacto | PASA |
| LT-49 | `:65` | ídem | Sí | PASA |
| LT-50 | `:71` | ídem | Sí: las siete afirmaciones | PASA |
| LT-51 | `:85` | ídem | Sí: rep 2, 50 m, 1 resumen (1, 620, 500, 100, split 100 ± 0,001) | PASA |
| LT-52 | `:102` | ídem | Sí: `[4, 3, 2]` | PASA |
| LT-53 | `:112` | ídem | Sí: g1 → g2 → suelto | PASA |
| LT-54 | `:126` | ídem | Sí | PASA |
| LT-55 | `:155` | ídem | Sí: `_Raw`, 250 lecturas, el 0, de 1004 a 1100, 2 vueltas de 125 ± 2, la lectura 1104 en el paso 1 con `_pos(2)`, trabajo en [1060; 1110] | PASA |
| LT-56 | `:180` | ídem | Sí | PASA |
| LT-57 | `:191` | ídem | Sí: el `check()` en cada lectura | PASA |
| LT-58 | `:211` | ídem | Sí: 20 m, `rebase(9000)`, 24 m | PASA |
| LT-59 | `series_panels_test.dart:39` | dotnet: `SeriesPanelPrefsTests` (la regla, con el tracker de `:12-22` pasado por `ComposeSeries`); Unity: `SeriesPanelViewTests.StartCollapsedTapExpandsAndRemembers` | Sí, en las dos: cerrados, `500 m` y `Reps`; el toque, `Parciales 500 m`, `Rep 1/3 · 300 m` y la clave en verdadero (en Unity, leída del archivo temporal); `Sin repeticiones completas` | dotnet PASA · Unity PENDIENTE (verificación principal) |
| LT-60 | `:62` | dotnet: `SeriesPanelPrefsTests` (mismo nombre); Unity: `RespectsTheSavedPreference` | Sí; además, sin pasar por cerrado (§11.1.1) | dotnet PASA · Unity PENDIENTE |
| LT-61 | `:68` | dotnet: `SeriesPanelsTests` (mismo nombre); Unity: `WithoutASeriesThereIsNoRepsPanel` | Sí: `500 m` y no `Reps` | dotnet PASA · Unity PENDIENTE |

Los 12 casos del tracker van también, estado por estado, en `legacyCases` del oráculo (`TheLegacyCasesMatchTheLegacy`, PASA).

---

## 4. Checklist de paridad (spec §9), contra el código

| # | Comportamiento | Dónde en la app | Resultado |
|---|---|---|---|
| 1 | Solo con rutina y en trabajo | `SeriesTracker.cs:340-341`; `WorkoutHudStore.SeriesFor` (`IsRoutine`); `HudView.cs:104` (`isRoutine`) | OK |
| 2-5 | La vuelta, su numeración por paso, menos de 500 m, por tiempo, el cruce interpolado y varias por lectura, la vuelta en curso | `SeriesTracker.cs:271-290`, `:332-334`, `:340-341`; oráculo y diferencial (§1.2, §1.3) | OK, bit a bit |
| 6 | Saneamiento | `SanitizedDistance.cs:75-88` (el orden de `series_tracker.dart:120-127`) | OK |
| 7 | Al reanudar, referencia nueva | `WorkoutEngine.cs:229` (`workout_provider.dart:248`) | OK |
| 8 | Desde la distancia del monitor y el reloj 0 | `WorkoutEngine.cs:192` (`:128`) | OK |
| 9-10 | Una lectura por segundo antes del fin del paso; nada en pausa ni en el libre | `WorkoutEngine.cs:432-457` (`:151-186`); `TheEngineFeedsTheOracleInputs` | OK |
| 11-12 | Se reinicia al iniciar y en reposo; nada se guarda | `WorkoutEngine.cs:192`, `:662`; la foto solo en `WorkoutSnapshot.Series` | OK |
| 13-20 | Posición, rep, cantidad y metros con `⌊ ⌋`, repetición nueva, resumen al frente hasta 3, otra serie o suelto vacían, metros y segundos de trabajo, split, la última nunca entra | `RoutineExpansion.cs:75-112`; `SeriesTracker.cs:300-335`, `:59` | OK, bit a bit |
| 21-28 | Lugar, chip, cabecera, vidrio y anchos | DV-50, DV-51 y SD-01 a SD-19 (decididos en el gate); se ven en Unity y en capturas | Fuera de esta lente (PENDIENTE UNITY) |
| 22 | Repeticiones solo en serie | `SeriesPanels.For` (`InSeries`); `SeriesColumn.cs:187-214` | OK |
| 29-30 | El toque alterna y guarda en el acto; las claves; cerrados por defecto; el toque antes de leer gana | `SeriesPanelPrefs.cs:55-75`, `:89-105` | OK (con QA-V5-P02 para la clave de otro tipo) |
| 31, 35, 36 | `Sin parciales en este paso`, `Rep r/R · N m` también fuera de un trabajo, `Sin repeticiones completas` | Catálogo; `SeriesPanelView.cs:257-259`, `:999-1024` | OK (lo dibujado, PENDIENTE UNITY) |
| 32-34 | Vuelta en curso y filas: tamaños y disposición | DV-51 | Fuera de esta lente |
| 37 | Filas `Rep n`, split o `—`, metros totales | `WorkoutHudStore.cs:404-433` | **QA-V5-P01** después del segundo plano |
| 38 | Formatos | `WorkoutFormats.LapTime`, `RepSplit`, `PanelMeters` (`series_panels.dart:9-24`; LT-45) | OK |
| 39-40 | La columna que desplaza; cifras tabulares | Unity | PENDIENTE UNITY |
| 41 | Textos fijos en español → catálogo (Q-01) | §6 | OK |
| 42 | El primer segundo (DV-48) | `SeriesPanels.cs:121-132` | OK (la diferencia aceptada) |
| 43 | Un segundo de atraso en el cambio de paso (P-01) | Natural del tracker; `SeriesPanelsTests.WhatTheyShowSecondBySecondInSeriesSeiscientos` | OK |
| 44 | Repeticiones conserva su estado oculto | `SeriesPanelPrefs` (uno por app) | OK |
| 45 | Los valores cambian solo en el segundo | Caché por `Revision` (`WorkoutHudStore.cs:440-460`); `BetweenTwoSecondsTheSeriesCostNothing` | OK |
| 46 | `Simular desconexión` | `SeriesSimulatorTests.ADisconnectDuringAWorkKeepsTheLapMeters` | OK (dotnet) |

---

## 5. Matriz AC → test → resultado (lo de esta lente)

Resultados de la corrida de §1.1 (`ac-matrix.md`). "Lente" es mi veredicto con lo que revisé en el código.

| AC | Tests (dotnet) | Matriz | Lente |
|---|---|---|---|
| AC-WKT-07-01 | `SeriesPanelsTests.OnlyWithARoutine`, LT-50, `SeriesSeiscientosCurrentLapAndRepMeters` | PASA | PASS |
| AC-WKT-07-02 | LT-47 a LT-49, `PiramideQuinientos`, `FondoTresMil…`, `TrabajoMilWithFailingMonitors` | PASA | PASS |
| AC-WKT-07-03 | `OneReadingPerSecondBeforeTheEndOfTheStep`, `NothingInPauseInFreeOrAfterTheEnd`, `TheEngineFeedsTheOracleInputs` | PASA | PASS |
| AC-WKT-07-04, -05 | LT-47, LT-48, LT-50, `TrabajoMilWithRThreeInterpolates`, `SeriesSeiscientosCurrentLapAndRepMeters` | PASA | PASS |
| AC-WKT-07-06 | LT-55 a LT-57, `TrabajoMilWithFailingMonitors` (R-4, R-4R, R-4Z, R-4D), `SanitizedDistanceTests`, oráculo | PASA | PASS |
| AC-WKT-07-07 | LT-58, `WhatIsRowedInPauseDoesNotCount` (124 m) | PASA | PASS |
| AC-WKT-07-08 | LT-56, `CountsFromTheMonitorDistanceAtTheStart` | PASA | PASS |
| AC-WKT-07-09 | `TheSameMetersAsTheDistanceStepSecondBySecond` (5 remos), `…InEveryStepOfSeriesSeiscientos`, `PureCodeRulesTests.OneDistanceSanitizerOnly` | PASA | PASS |
| AC-WKT-07-10 | `TheBackgroundGivesTheSameAsTheForeground`, `SuspendedWithRFourS_RawCounts` / `_RawStops` | PASA | PASS (el motor; el store, ver QA-V5-P01) |
| AC-WKT-07-11 | `BeginOnStartAndOnIdle…`, `TheSnapshotCarriesTheTrackerStateWithoutCopying`, `BeginResetsEverything` | PASA | PASS |
| AC-WKT-07-12 | `SeriesGoldenTests` (7), `OracleToolingTests` | PASA | PASS (regenerado idéntico, §1.2; diferencial, §1.3) |
| AC-WKT-08-01 a -08 | LT-50 a LT-55, `SeriesSeiscientosReps`, `CincoRepsKeepsTheLastThree`, `DosSeriesClearsTheComparison`, `PiramideQuinientos`, `PruebaCuatroAAndCorta`, `TheWorkSplitCountsTheSecondsOfAJumpButNotItsMeters`, oráculo | PASA | PASS |
| AC-WKT-18-01 | `SeriesPanelsTests` (4), `HudSeriesChangeTests`, `SeriesTimelinesTests` | PASA | PASS (la regla; lo montado, PENDIENTE UNITY) |
| AC-WKT-18-04 a -06 | `SeriesPanelsTests`, `SeriesPanelPrefsTests` (LT-59), `WorkoutHudStoreTests.SeriesTextsInSpanishAndEnglish`, geometría | PASA | PASS (la regla) |
| AC-WKT-18-07 | ídem y `RepsRowsOfTheAppCases` | PASA | **FAIL en el caso de QA-V5-P01** (segundo plano entre dos series); PASS en primer plano |
| AC-WKT-18-08 | `WhatTheyShowSecondBySecondInSeriesSeiscientos`, `HudSeriesChangeTests`, `BetweenTwoSecondsTheSeriesCostNothing` | PASA | PASS |
| AC-WKT-18-10, -11 | `SeriesPanelPrefsTests`, `JsonFilePreferencesTests`, `LegacyIsolationTests.ThePreferencesAreTheAppsOwn` | PASA (1 ignorado: el reinicio en el build) | PASS (QA-V5-P02 es de texto) |
| AC-WKT-18-22 | `CatalogParityTests.NineItFiveKeys`, `SeriesTextsInSpanishAndEnglish` | PASA | PASS |
| AC-APP-10-12 | `CatalogParityTests.NineItFiveKeys`, `SameKeysInBothLanguages` | PASA | PASS (223 claves en es y en, contadas en los archivos) |
| AC-WKT-11-09 a -11 | LT-45, `WorkoutFormatsTests` | PASA | PASS |
| AC-WKT-01-05, AC-WKT-03-03 | `SeriesEngineTests`, `SanitizedDistanceTests`, `StepAdvanceTests`, `OneDistanceSanitizerOnly` | PASA | PASS |
| AC-WKT-02-08 (cambia) | — (Editor-PM, Device-And) | — | **FAIL en el caso de QA-V5-P01** (lo vi con el store en dotnet); lo montado, PENDIENTE UNITY |
| LT-45, LT-47 a LT-61 | §3 | PASA (15 de 15 en dotnet) | PASS; lo montado de LT-59 a LT-61, PENDIENTE UNITY |

---

## 6. Textos (Q-01)

Los 9 de spec §11.2, contra los literales de `series_panels.dart`:

| Clave | es (app) | Legacy | en |
|---|---|---|---|
| `hudLapsChip` | `500 m` | `:168` `'500 m'` | `500 m` |
| `hudLapsTitle` | `Parciales 500 m` | `:169` | `500 m splits` |
| `hudLapsEmpty` | `Sin parciales en este paso` | `:287` | `No splits in this step` |
| `hudLapNumber` | `#{number}` | `:308` `'#${lap.number}'` | `#{number}` |
| `hudRepProgress` | `Rep {rep}/{count} · {meters}` | `:315` | igual |
| `hudRepsChip` | `Reps` | `:183` | `Reps` |
| `hudRepsTitle` | `Series anteriores` | `:184` | `Previous reps` |
| `hudRepsEmpty` | `Sin repeticiones completas` | `:332` | `No completed reps` |
| `hudRepLabel` | `Rep {rep}` | `:338` `'Rep ${r.rep}'` | `Rep {rep}` |

El `·` es U+00B7 (bytes `C2 B7`) en el catálogo y en `series_panels.dart:315`. Los dos catálogos tienen 223 claves. `{meters}` lleva `PanelMeters` (`1.112 m`, con punto de miles también en inglés: Q-06, P-08), como `formatMeters` (`:22-24`); los metros de la vuelta en curso van sin separador, como `'${t.currentLapMeters} m'` (`:301`; nunca pasan de 499).

---

## 7. Un solo saneamiento y el motor idéntico

- **Q-55 / DV-26.** `WorkoutEngine.cs` no crea ni nombra `SanitizedDistance` (lo fija `PureCodeRulesTests.OneDistanceSanitizerOnly`); alimenta el tracker una vez por segundo (`:455`) y el paso por distancia compara `_series.Meters − _stepStartMeters` (`:476`) y fija `_stepStartMeters = _series.Meters` al avanzar (`:502`). El tracker fija el inicio del paso siguiente al entrar, en el segundo siguiente, con su `_distance` de antes de esa lectura (`SeriesTracker.cs:332`): es el mismo `double`, así que `d − inicio ≥ 500·n` (la vuelta) y `metros − inicio ≥ meta` (el paso) son la misma cuenta con los mismos números. `TheSameMetersAsTheDistanceStepSecondBySecond` lo afirma con `R-4`, `R-4R`, `R-4Z`, `R-4D` (fin en 250, 251, 252 y 254 s, con `#2` en ese mismo segundo) y `R-4X` (nunca termina, nada negativo). No hay otra cuenta de "metros del paso" en el HUD: los pasos por distancia muestran su largo fijo (`StepDistance`) y las píldoras solo se llenan por tiempo.
- **El motor.** El único cambio de comportamiento posible era mover `_meter` adentro del tracker: misma regla, mismas lecturas, mismos segundos. `WorkoutGoldenTests` pasa 10 de 10 (`SessionsAreIdenticalToTheLegacy`, `ScriptsAreIdenticalToTheLegacy`, `SanitizerScenariosMatchUntilTheFirstStepChange`, `NegativeStepIsTheExpectedDifference`, `CompletedHoldReset` con su espera, entre otras). El diff de `WorkoutGoldenTests.cs` contra `d0bd574` solo **suma** afirmaciones (O-P01 y O-P02 de la lente de la 4b) y `workout_engine.json` no cambió. No volví a correr el oráculo del motor: el legacy sigue en `3ae0f3a` y la lente de la 4b ya lo regeneró idéntico byte a byte.

---

## 8. Lo que no verifiqué

- **Todo lo montado en Unity** (la tarea es sin Unity): las partes [Editor-PM] de LT-59 a LT-61 (revisé el código de `SeriesPanelViewTests` y es el port fiel de los tres casos), el dibujo de los paneles, la columna, el desplazamiento, el teclado, las capturas y los videos. Los cubre la verificación principal.
- **AC-WKT-18-10 en el build** (la preferencia después de cerrar y abrir) y el `[PERF]`: [Device-Win].
- **Android** (QA-V5-P01 es justamente de segundo plano en el teléfono): NO VERIFICABLE sin el módulo; lo reproduje con el ciclo de vida de prueba en dotnet.

## 9. Higiene

- `git -C ../rowmate-legacy status --porcelain`: vacío al empezar, después de cada corrida de Dart y al terminar; ningún archivo del legacy con fecha posterior al comienzo de esta lente.
- En el repo solo escribí este reporte. Los generadores, el JSON regenerado, el diferencial y las sondas están en `<scratch>/it5/lens-paridad/`.
- No abrí Unity ni el build. Ninguna corrida tocó la base ni el archivo de preferencias del usuario: en su carpeta de datos no hay `prefs.json`, y la base y sus archivos `-wal` / `-shm` tienen fechas anteriores a esta lente.
- Sin rutas de la PC, nombres de equipo ni datos del usuario en este reporte; las salidas citadas están recortadas a `<scratch>`.
