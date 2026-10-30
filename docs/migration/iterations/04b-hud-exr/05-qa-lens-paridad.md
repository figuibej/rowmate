# It. 4b — Verificación, lente "paridad" (QA)

| Campo | Valor |
|---|---|
| Autor | rowmate-qa |
| Fecha | 2026-10-29 |
| Iteración | 4b · `04b-hud-exr` ([ROADMAP §It. 4b](../../ROADMAP.md)) |
| Lente | **Paridad**: las reglas nuevas de la 4b contra el código del legacy (no contra los documentos). Corre en paralelo con la verificación principal ([05-qa-verification](05-qa-verification.md)) |
| Legacy (solo lectura, `3ae0f3a`) | [rowmate-legacy](https://github.com/figuibej/rowmate-legacy). Enteros: `lib/features/workout/step_pill.dart`, `test/step_pill_test.dart`, `lib/features/workout/step_target_tracker.dart`, `lib/features/workout/workout_provider.dart:1-320`, `lib/features/workout/immersive_workout_screen.dart:1-560` y `:940-1019`, `lib/features/workout/series_panels.dart:1-130` (`WallClock`), `test/series_panels_test.dart`, y las claves de `lib/l10n/app_es.arb` y `app_en.arb` que usa la 4b |
| Herramientas | **Sin Unity**. `tools/dotnet/run-tests.ps1 -Matrix` con su propia carpeta de artefactos; el oráculo del motor corrido otra vez en una **copia** de `tools/oracle/engine` en `<scratch>/it4b/lens-paridad/oracle/` con `<flutter-sdk>`; un proyecto de pruebas propio en `<scratch>/it4b/lens-paridad/probe/` (no va al repo); las medidas de las capturas de `snapshots/manifest.json` y de `CAP-13`. No se modificó ningún archivo del repo salvo este reporte |
| Veredicto | **APROBADO CON OBSERVACIONES**: 0 bloqueantes, 0 mayores, 2 menores (§2) |

---

## 0. Veredicto

**APROBADO CON OBSERVACIONES.**

Las reglas de la 4b son las del legacy, y el motor sigue idéntico a él con la espera del diálogo:

- **La píldora.** Los cuatro estados, las opacidades 0,18 / 0,30 / 0,85, el borde de 1,5 de la no cumplida, "hecha sin resultado = cumplida", el relleno acotado y sin relleno en 0, la regla por índice y "solo se llena un paso por tiempo con duración > 0" coinciden línea por línea con `step_pill.dart:30-76`, `immersive_workout_screen.dart:363-375` y `workout_provider.dart:31-36`. LT-46 está portado con sus 12 afirmaciones, armado con `LookOf` (QA-G4b-P08 resuelto).
- **Q-52** (el último paso queda en curso) y **Q-53** (nunca el diálogo al terminar a mano) se cumplen de punta a punta: motor más store del HUD, con mis sondas (§1.3).
- **Q-59**: todas del mismo ancho, `(útil − 3·(n − 1)) / n`, igual que los `Expanded` del legacy.
- **El diálogo**: una vez por sesión, solo al terminar sola, modal, con los textos del legacy (`workoutCompleted`, `workoutCompletedMessage`, `workoutFinish`) y las etiquetas fijas del legacy (`Tiempo`, `Distancia`, `Calorías`) en el catálogo; el resumen da `00:30`, `1120 m` y `43` con `Corta` y `R-4`.
- **El chip**, la barra libre y el reloj de pared tienen los textos, los colores y el orden del legacy (con las DV aceptadas: DV-41, DV-44, DV-46, DV-47).
- **El oráculo es reproducible.** Lo corrí otra vez desde el legacy en `3ae0f3a`: el `workout_engine.json` que sale es **idéntico byte a byte** al del repo, salvo `generatedAt`. `WorkoutGoldenTests` pasa entero: los 11 escenarios, los 16 guiones y `CompletedHoldReset`.

Encontré **dos bugs menores**, los dos de presentación y con escenario concreto:

- **QA-V4b-P01 · el *tracking* del chip de fase (y de `PAUSA`) es 1/9 del pedido.** `PhaseChipView` escribe `letter-spacing` en dp, pero en TextCore la unidad es 1/100 de em: queda 0,0088 em en vez de 0,08 em. El legacy usa 0,8 px a 11, y la spec y el Designer cuentan con ese *tracking* para que el 700 se vea como el 800 del legacy. Las capturas lo confirman: el chip mide 95,2 base en lugar de 101,8. Ojo: el arreglo cambia el ajuste del título de la barra libre en TV (AC-WKT-16-06).
- **QA-V4b-P02 · la última píldora puede quedar al 98,4-99,9 %** en vez de llena (AC-WKT-14-13). La foto final llega con el reloj quieto a menos de 1 s del segundo anterior, por el *jitter* de cuadro, y `PillProgress` se congela antes de llegar a 1. El arnés de capturas no lo ve porque sus cuadros son exactos.

---

## 1. Método y evidencia

### 1.1 dotnet (salida real)

`powershell -NoProfile -File tools/dotnet/run-tests.ps1 -Matrix -ArtifactsPath <scratch>/it4b/lens-paridad/art`:

```
Build succeeded.
    0 Warning(s)
    0 Error(s)
...
Total tests: 182
     Passed: 182                       (RowMate.Tests.Simulator)
...
Test Run Successful.
Total tests: 2306
     Passed: 2279
    Skipped: 27                        (RowMate.Tests.EditMode)
[run-tests] matriz: .../ac-matrix.md (PASA 478, FALLA 0, IGNORADO 2, PENDIENTE UNITY 117, PENDIENTE WP-B 0, SIN TEST 0)
exit=0
```

En total: **2488 pruebas, 2461 pasan, 0 fallan y 27 ignoradas**, que son las de equipo: Android, Apple, el suspendido de Windows y GitHub.

Las de la lente, todas en verde:

- `StepPillsTests`: los cinco casos y LT-46;
- `PillRowGeometryTests`, `PillProgressTests`, `PhaseChipTests`, `WallClockTickerTests`;
- `CompletionDialogRuleTests` (`OncePerSession`, `NeverOnManual`, `InstantStateOpensWithoutItsEntry`);
- `CompletionHoldTests`: los 13 casos;
- `WorkoutHudStoreTests.PillsWithTheCache` y `CompletionTextsEsEn`;
- `WorkoutStoreTests.NoCompletedToast`;
- `WorkoutGoldenTests`: `VectorsComeFromTheLegacyEngine`, `ScriptsComeFromTheLegacyEngine`, `ScriptsAreIdenticalToTheLegacy` (los 14 que se comparan), `CompletedHoldReset`, `NegativeStepIsTheExpectedDifference`, `MonitorEveryTwoSecondsMatchesUntilTheFirstStepChange` y `LegacyDistanceColumn`.

La consola informó `ScriptsAreIdenticalToTheLegacy [1 ms]`, un tiempo sospechoso para 14 guiones. Lo medí aparte, llamando al test desde mi proyecto: tarda **101,3 ms** la primera vez (con la compilación JIT) y **3,2 ms** después, y afirma `compared == 14`. Corre de verdad.

### 1.2 El oráculo del motor, otra vez desde el legacy

Copié `tools/oracle/engine/{pubspec.yaml,test/}` y `tools/oracle/lib/golden.dart` a `<scratch>/it4b/lens-paridad/oracle/`, con un `pubspec_overrides.yaml` propio hacia el legacy. Corrí `<flutter-sdk>/bin/flutter pub get --offline` y `flutter test test/engine_vectors_test.dart`, con `ROWMATE_ORACLE_OUT` apuntando al scratch y `ROWMATE_LEGACY_COMMIT=3ae0f3a…`.

```
00:00 +0: vectores del motor legacy (It. 4a, workout_engine.json)
Shell: [oracle] workout_engine.json: 138115 bytes
00:00 +1: All tests passed!
```

Después, `diff` contra `Assets/RowMate/Tests/Golden/oracle/workout_engine.json` sin la línea `generatedAt`: **idénticos**. Los dos archivos tienen 138 115 bytes.

Guarda del legacy:

- antes y después, `git status --porcelain --ignored --untracked-files=all` y una instantánea de metadatos (ruta, tamaño y fecha de cada archivo fuera de `.git/`, sin abrir ninguno): **iguales**;
- `git -C ../rowmate-legacy status --porcelain` quedó **vacío**.

Borré del scratch lo que deja `pub get` (`.dart_tool/`, `build/`, `pubspec.lock` y el override), que trae rutas de la PC. El repo no cambió: `tools/oracle/` muestra solo los dos `M` que ya estaban.

### 1.3 Sondas propias (`<scratch>/it4b/lens-paridad/probe/`)

Es un proyecto NUnit que referencia los binarios compilados en §1.1 y enlaza `Tests/EditMode/Workout/WorkoutHarness.cs` del repo, sin copiarlo ni modificarlo.

| Sonda | Qué prueba | Resultado |
|---|---|---|
| `HudQ52Probe.CortaEndsWithTheLastPillCurrentAndFull` | `Corta` con `R-4`, motor más `WorkoutHudStore`, a los 30 s: `Looks = [Solid, Solid, Solid, Current]`, `Elapsed 5 = Duration 5`, `Running = false`, sin halo ni cuenta grande; resumen `00:30\|1120 m\|43`; se cambia el idioma con el diálogo abierto y se espera a los 45 s, y la última sigue en curso; `AcknowledgeCompletion` lleva a reposo | **Pasa** |
| `HudQ52Probe.FinalByDistanceEndsWithTheTrackOnly` | `Final por distancia`: `[Solid, Current]` y `Duration = null` (la pista sola). El motor calculó `Met` para el último paso y no se ve | **Pasa** (termina a los 35 s; `ResultOf(1) = Met`) |
| `HudQ52Probe.ManualFinishOnTheLastStepNeverCompletes` | Q-53: `Terminar` a los 27 s, en el último paso, con la base lenta (2 s) y el remo mandando lecturas cada 0,1 s mientras se guarda. En ningún estado `CompletionDialogRule.ShouldOpen` da `true`, `Completion = null` y `EndedBy = Manual` | **Pasa** |
| `LensParidadProbes.LastTimedStepEndsFull` | QA-V4b-P02 (§3.2) | **Falla**: `0.984`, `0.9968` y `0.999` |
| `GoldenTiming.ScriptsReallyRun` | §1.1 | 101,3 ms y 3,2 ms |

---

## 2. Issues

| ID | Sev. | Resp. | Issue | Evidencia | Corrección sugerida |
|---|---|---|---|---|---|
| **QA-V4b-P01** | menor | architect | **El *tracking* del chip de fase y del `PAUSA` queda en 0,0088 em en vez de 0,08 em.** `PhaseChipView` hace `letterSpacing = 0,08 × 11 = 0,88`, y `CenterCallouts` hace `0,08 × 12 = 0,96`. En UI Toolkit 6000.3 la unidad de `letter-spacing` es la de TextCore: N px valen N/100 em por letra. Por eso `.rm-type-label` lleva `8px` y el tipo del paso de la 4a lleva `4`. El chip queda casi sin *tracking*. El legacy lleva 0,8 px a 11 (0,073 em). La spec (AC-WKT-16-03, §9 fila 25) pide "tracking 0,8", y el Designer cuenta con él para que el 700 "se vea como el 800 del legacy" | **Código:** `Presentation/Workout/PhaseChipView.cs:87`, `Presentation/Workout/CenterCallouts.cs:67`, `Core/Design/HudGeometry.cs:355` y `:362` (0,08 em). Uso correcto: `Presentation/Workout/StepInfoBar.cs:118` (`4` = 0,04 em), `Core/Design/TypographyUssWriter.cs:14-15` y `:136-139`, y [ART_BIBLE](../../design/ART_BIBLE.md) línea 436. **Legacy:** `immersive_workout_screen.dart:546` (`letterSpacing: 0.8`). **Capturas** (`snapshots/manifest.json`, s = 42,2 / 40 = 1,055; el resto en base): ver §3.1. Con cuatro etiquetas el desvío coincide con "0,0088 em en vez de 0,08" con un error de ± 0,6 base; el título, que no lleva *tracking*, coincide con fontTools en 0,1 | **Architect:** `letterSpacing = em × 100` (8 para los dos; o 7,27 si se quiere el 0,8 px exacto del legacy), con una función como `TypographyUssWriter.LetterSpacingPx`. Sumar una prueba de Unity que falle hoy: `Chip.FixedWidth` ≈ 101,8 ± 1 en es y 74,1 ± 1 en en (hoy `FreeBarTests.cs:33` pide solo `> 70`), y el ancho de `PAUSA`. **Cuidado, AC-WKT-16-06:** el título de la barra libre entra hoy en TV en español (0,818) **gracias** al chip angosto, más el aire `TitleClockGapDp` de 6 que se sumó después de las notas. Con el chip en 101,8 le quedan ≈ 121 base: escala ≈ 0,78 < 0,8, y se recorta (`Entrenamient…`). **Designer y Architect** tienen que rebalancear la barra antes de cerrar (§3.1) y volver a sacar CAP-13, -14, -15, -16, -17, -18 y CAP-27 |
| **QA-V4b-P02** | menor | architect | **La última píldora puede no quedar llena (Q-52).** Con la rutina terminada, la píldora de un paso por tiempo tiene que quedar "llena (progreso 1)". El motor entrega cada segundo en el primer cuadro con hora ≥ su vencimiento, y los vencimientos no derivan. Dos fotos seguidas pueden llegar, entonces, a **menos** de 1 s entre sí: hasta un cuadro menos. La foto final trae `Running = false`. `PillProgress.OnState` avanza primero con el estado anterior hasta `(D − 1 + since) / D`, con `since < 1`, y después ya no avanza: con el reloj quieto no corrige al presente, salvo con `Instant`. La píldora se queda en 98,4 %-99,9 % para siempre (hasta que sale el inmersivo). Con `fillWidth < ancho` `DrawFill` dibuja el corte recto y no la cápsula entera. Se nota en un último paso corto, de 1 a 5 s: con 1 s y un cuadro de 60 fps faltan ≈ 1,6 % del ancho, ≈ 1 px. El arnés de capturas no lo ve: sus cuadros son exactos y CAP-06 tolera 0,005 | **Código:** `Core/Design/PillProgress.cs:58-74` (con un segundo nuevo, solo mueve `_tick`), `:108-111` (`Advance` sale si no corre); `Core/Workout/WorkoutEngine.cs:361-365` (`Arm` con `NextDueTicks`) y `:39` (tolerancia de 1 ms); `Presentation/Motion/MotionDriver.cs:32` (`Now` es el reloj del cuadro); `Snapshots/HudExrCatalog.cs:127` (CAP-06, ± 0,005). **Legacy:** `workout_provider.dart:31-36` y `:203-216` (progreso = 15/15 = 1). **Sonda** `LensParidadProbes.LastTimedStepEndsFull`: `D=1 gap=0.984 -> relleno final 0.984`; `D=5 gap=0.984 -> 0.9968`; `D=1 gap=0.999 -> 0.999`; el control con 1,016 s da 1. Las tres fallan con `Expected: 1.0d` | En `PillProgress.OnState`, con un segundo nuevo (`newSecond`) y `fills`, `_value = max(_value; clamp(elapsed / D))`: es el presente del motor, así que nunca adelanta (AC-WKT-14-10) y deja llena la última sin tocar la pausa (Q-58 no trae segundo nuevo). Sumar a `PillProgressTests` el caso "último segundo a 0,984 s del anterior, `running = false` → 1" |

### 2.1 Observaciones (no son bugs)

- **O-P01 · Comentarios viejos en `WorkoutGoldenTests`.** Dos comentarios siguen diciendo que el motor nuevo "vuelve solo a reposo", cosa que con la 4b ya no pasa cuando la rutina termina sola: `:680-681` ("el motor nuevo, al volver solo a reposo, los borra como ese reset") y `:749` ("el motor nuevo vuelve solo a reposo: se corta antes"). La conducta está bien: `Ended` corta en `Finished`. Conviene alinear el texto con el de los guiones (`:211-216`).
- **O-P02 · `CompletedHoldReset` no mira los resultados durante la espera.** `run.Targets` se anota en el instante del fin (32 s es el primer evento después). El comentario dice que "los resultados de los objetivos siguen", pero no se comprueba entre los 30 y los 34 s. El motor los borra solo en `ReturnToIdle` (`WorkoutEngine.cs:654`), y mi sonda `CortaEndsWithTheLastPillCurrentAndFull` ve las píldoras intactas a los 45 s. Hoy no hay riesgo. Si se quiere fijar, alcanza con una aserción a los 33 s.
- **O-P03 · El ancho del chip se mide pero no se compara.** `FreeBarTests.cs:30-35` se conforma con `FixedWidth > 70`, y por eso no vio QA-V4b-P01. Lo mismo `HudExrCatalog.cs:1016`, que compara el chip con su propio ancho medido.

---

## 3. Detalle de los dos hallazgos

### 3.1 QA-V4b-P01: las medidas

Todos los anchos están en base (dp de la captura / 1,055) y salen de `snapshots/manifest.json`. La columna "esperado sin *tracking*" es el valor de las notas menos 0,88 por letra, más la que sí queda (0,097 por letra).

| Elemento | Captura | Unity | Notas (0,08 em) | Esperado sin *tracking* |
|---|---|---|---|---|
| `EN MARCHA` | CAP-13 | 69,3 | 75,8 | 68,8 |
| `PAUSADO` | CAP-14 | 54,9 | 60,1 | 54,6 |
| `FINALIZADO` | CAP-15 | 68,6 | 76,2 | 68,4 |
| `ACTIVE` | CAP-18 | 41,9 | 46,5 | 41,8 |
| chip (es) | CAP-13 | 95,2 | 101,8 | 95,3 |
| chip (en) | CAP-18 | 68,2 | 74,1 | 67,9 |
| `Entrenamiento Libre` (sin *tracking*) | CAP-13 | 155,5 × 0,818 | 155,4 (fontTools) | — |

La última fila es el control: el título no lleva *tracking* y coincide con fontTools. Entonces la diferencia de las etiquetas del chip no sale del método de medida.

El ajuste del título en TV y en un teléfono de 360, en español:

- ancho útil: 336,1;
- hoy: 20 + 8 + 6 + 69,3 (reloj) + 10 + 95,2 (chip) = 208,5. Le quedan 127,6 al título, que mide 155,4: escala 0,821, entero;
- con el chip arreglado (101,8): le quedan ≈ 121,0, escala ≈ 0,779 < 0,8, recortado.

Las notas (§3.5) llegaban a 0,817 sin el aire de 6 de `FreeTopBar.TitleClockGapDp`, que se sumó después porque "con 10 el título no entraba a 0,8 en TV con los anchos de Unity" (`FreeTopBar.cs:29-33`). Ese ajuste se calibró con el chip angosto.

### 3.2 QA-V4b-P02: la sonda

```
D=1 gap=0.984 -> relleno final 0.984
D=5 gap=0.984 -> relleno final 0.9968
D=1 gap=0.999 -> relleno final 0.9990000000000006
  Failed LastTimedStepEndsFull(1,0.984d)   Expected: 1.0d  But was: 0.984d
  Failed LastTimedStepEndsFull(5,0.984d)
  Failed LastTimedStepEndsFull(1,0.999d)
  Passed LastTimedStepEndsFullWhenTheGapIsOneSecondOrMore
```

La secuencia es la que arma la vista: `OnState(3, k, D, true, t_k)` cada segundo y, al final, `OnState(3, D, D, false, t_final)` con `t_final − t_(D−1) < 1`.

---

## 4. Matriz AC → test → resultado (la lente)

PASA = pasa en dotnet y la revisión estática o la captura no muestra nada en contra. "Unity" = la parte que falta es de PlayMode o de captura, que corre la verificación principal.

| AC | Tests (dotnet) | Resultado de la lente |
|---|---|---|
| AC-WKT-15-01 | `StepPillsTests.ColorIsTheBaseOfTheStepType` | PASA. Los colores base son los de `theme.dart:56-69` y la vista usa `StepTypeInfo.Color`, no los `.hud` (`PillRowView.cs:657-659`) |
| AC-WKT-15-02 a -05 | `StepPillsTests` (LT-46 y los de cada estado), `PillRowGeometryTests.NarrowHollowHasRails` | PASA |
| AC-WKT-15-06 | `PillColorVisionTests.FourLooksDifferUnderDeuteranopiaAndProtanopia` | PASA (la regla); la parte de CAP-01 es de Unity |
| AC-WKT-14-07 | `PillRowGeometryTests.PaddingAndHeight` | PASA (10 / 8 / 10 / 4, alto 8, `:359-360`) |
| AC-WKT-14-08 (Q-59) | `PillRowGeometryTests.EqualWidthsWithinOnePixel`, `StepPillsTests.LooksByIndex`, `WorkoutHudStoreTests.PillsWithTheCache` | PASA. Mismo ancho para todas, separación 3 y orden de la rutina expandida |
| AC-WKT-14-09 (Q-51) | `PillProgressTests.NoFillByDistanceOrWithoutDuration`, `StepPillsTests.OnlyTimedStepsFill` | PASA |
| AC-WKT-14-10 | `PillProgressTests` (4) | PASA. Nunca adelanta el reloj. La foto final se queda corta: QA-V4b-P02 |
| AC-WKT-14-13 (Q-52) | `StepPillsTests.LastStepStaysCurrent`; sondas `HudQ52Probe` | **FALLA (menor)**: la regla y el estado del HUD pasan, pero la última puede no quedar llena en el equipo (QA-V4b-P02) |
| AC-WKT-14-16 | `PillRowGeometryTests` (6), `HudTimelinesTests.ThumbSwap` | PASA |
| AC-WKT-16-02 | — | Unity. En la revisión estática el orden y las medidas coinciden con `:505-519` (`FreeTopBar.cs:52-75`) |
| AC-WKT-16-03 | `PhaseChipTests` (5), `HudTimelinesTests.PhaseChipColorsAndLabel` | **FALLA (menor)**: los textos, los colores, el tinte del 15 % y el borde del 50 % están bien (CAP-13, -14 y -15, todas las sondas en verde), pero el *tracking* no (QA-V4b-P01) |
| AC-WKT-16-04 | `PhaseChipTests.IdleHasNoChip` | PASA (el `—` nunca se ve: `PhaseChip.For(Idle) = null` y la vista se congela en reposo) |
| AC-WKT-16-05 | `PhaseChipTests.LiveDotOnlyWhenActive`, `HudTimelinesTests.LiveDot*` | PASA |
| AC-WKT-16-06 | `InfoRowFitTests.FreeTitleScale` | PASA hoy (0,818 en CAP-13). **En riesgo** al corregir QA-V4b-P01 |
| AC-WKT-19-01 a -04 | `WallClockTickerTests` (9) | PASA. `HH:mm` de 24 h; se redibuja solo cuando cambian la hora o el minuto (`series_panels.dart:75-80`, `WallClockTicker.cs:108-131`); sigue en pausa, terminada y con el diálogo abierto |
| AC-WKT-19-05 | — | Unity. En la revisión estática va sin marco en las dos barras |
| AC-WKT-21-01 | `CompletionDialogRuleTests.OncePerSession`, `.NeverOnManual`, `CompletionHoldTests.RoutineEndHolds` | PASA |
| AC-WKT-21-02 | `WorkoutDeviceChecksTests.AndroidBackWithTheCompletionDialog` (ignorada: equipo) | Unity / equipo. En la revisión estática: `Dismissible = false`, y "Atrás" no abre la confirmación con el diálogo abierto (`ImmersiveWorkoutPage.cs:184`) |
| AC-WKT-21-03 | `WorkoutHudStoreTests.CompletionTextsEsEn` | PASA: textos, íconos y orden como en `:213-229` y `:959-1019`; lo vi en CAP-07 |
| AC-WKT-21-04 | `CompletionHoldTests.TotalsAreThoseOfTheFinalPoint`; sonda `CortaEnds…` | PASA (`00:30`, `1120 m`, `43`, fijos con lecturas nuevas) |
| AC-WKT-21-05 | `HudTimelinesTests.CountUpEndsAtTheExactValue` y otro | PASA (la regla) |
| AC-WKT-21-06 | `CompletionHoldTests` (6) | PASA. Revisé los cuatro caminos en `WorkoutEngine.cs:243-259` y `:563-637`: cierre ya vuelto, lento, fallido o vencido, y el bueno que llega tarde pero antes del toque |
| AC-WKT-21-07 (Q-53) | `CompletionDialogRuleTests.NeverOnManual`, `CompletionHoldTests.ManualNeverHasCompletion`; sonda `ManualFinishOnTheLastStep…` | PASA. Además el aviso `workoutCompleted` salió de `WorkoutToast.For` |
| AC-WKT-21-08 | `CompletionHoldTests` (4) | PASA |
| AC-WKT-21-09 | `CompletionHoldTests.EndsInBackground` y dos más | PASA (la regla) |
| AC-WKT-21-10 | — | Unity. En la revisión estática hay un solo botón, `workoutFinish` (`CompletionDialog.cs:113`) |
| AC-WKT-21-13 | `WorkoutGoldenTests.CompletedHoldReset`, `.ScriptsAreIdenticalToTheLegacy` | PASA. El oráculo es reproducible (§1.2) y los 7 guiones de QA-V4a-L02 que pide la spec están entre los 16 |
| LT-46 | `StepPillsTests."pendiente, en curso, hecho y no cumplido se pintan distinto"` | PASA. Las 12 afirmaciones de `test/step_pill_test.dart:34-52`, una por una y con el mismo mapeo |

---

## 5. Paridad regla por regla (spec §9)

Revisadas contra el código del legacy: las filas 1 a 13, 17 a 19, 21 a 29, 31 a 39, 41 a 45 y 53.

**Coinciden**, con las DV ya aceptadas donde aplica: todas menos la 17 y la 25.

**Con observación:**

- **Fila 17** (el progreso del último paso es 1): QA-V4b-P02.
- **Fila 25** (chip 11/800, *tracking* 0,8): QA-V4b-P01.

**Detalles revisados:**

- **Fila 4** (hecha sin objetivo = cumplida): `StepPills.LookOf` devuelve `Hollow` solo con `Missed`, igual que el `switch` de `step_pill.dart:48-54`.
- **Fila 18** (el último paso se evalúa, pero no se ve): el motor llama a `Targets.Complete` del último (`WorkoutEngine.cs:479`) y la píldora queda `Current`. Con `Final por distancia`, `ResultOf(1) = Met` no se ve.
- **Fila 31** (una vez por sesión): el legacy usa un `bool` por página que se borra al volver a reposo (`:29`, `:61`); la app guarda la generación de la sesión (`CompletionDialogRule.cs:21-22`). Es más fuerte e igual en la práctica.
- **Fila 37** (los totales): el legacy lee `w.totalElapsedSeconds` y `w.data` un cuadro después del fin; la app usa el reloj activo y `_fusion.Data` en `EndSession` (`WorkoutEngine.cs:518-520`), que es el mismo instante del punto final (DV-43).
- **Fila 45** (a mano puede asomar el diálogo): la app nunca lo muestra. `EndedBy = Manual` en toda foto `Finished` a mano, también con lecturas durante el guardado (mi sonda).
- **Textos (Q-01).** El catálogo tiene 214 claves por idioma, como dice §11.2. Las doce claves nuevas tienen el texto de la tabla. Las reusadas son idénticas al `.arb` del legacy: `workoutCompleted`, `workoutCompletedMessage`, `workoutFinish` (`Terminar` / `Finish`) y `stepType*`. Las que el legacy tiene fijas en español salen con el mismo español: `Entrenamiento Libre`, `EN MARCHA`, `PAUSADO`, `FINALIZADO`, `Tiempo`, `Distancia`, `Calorías`.

---

## 6. Guardas

- `git -C ../rowmate-legacy status --porcelain` quedó **vacío** al empezar y al terminar. La instantánea de estado (con los ignorados) y la de metadatos no cambiaron con la corrida del oráculo (§1.2). No leí ni copié el módulo de credenciales del legacy; la instantánea de metadatos no abre archivos.
- No se abrió Unity ni se hizo ningún build.
- El único archivo del repo que cambié es este reporte. Las sondas, la copia del oráculo y los artefactos de dotnet quedan en `<scratch>/it4b/lens-paridad/`.
- Este documento no tiene rutas de la PC ni datos del equipo.
