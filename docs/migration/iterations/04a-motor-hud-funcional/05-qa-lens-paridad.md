# It. 4a — Verificación de QA, lente "paridad"

Fecha: 2026-10-28. Rol: [rowmate-qa](../../../../.claude/agents/rowmate-qa.md), momento 2 (verificación), en paralelo con el QA principal. Alcance: la paridad regla por regla del motor nuevo y de lo que el HUD calcula contra `lib/features/workout/workout_provider.dart` y `lib/features/workout/step_target_tracker.dart` de [rowmate-legacy](https://github.com/figuibej/rowmate-legacy). Se revisaron las transiciones de fase, el reloj con pausas, el avance por tiempo y por distancia con sus bordes, las progresiones, el último paso, "a continuación", el muestreo, el punto final, el P99 y las kcal, la fusión del pulso, los formatos de F-WKT-11 y los textos del catálogo del HUD y de los diálogos. Sin Unity: lo que se ve en pantalla queda para el QA principal (PlayMode y capturas).

---

## 0. Veredicto

**APROBADO CON OBSERVACIONES** — 0 bloqueantes, 0 mayores, 2 menores.

El motor nuevo reproduce las reglas del legacy. La evidencia sale de tres fuentes:

1. **Pruebas del repo.** El oráculo del repo y las pruebas portadas pasan sin fallas.
2. **Sonda contra el legacy.** Se corrieron 15 escenarios que el oráculo no cubre, en el `WorkoutProvider` real del legacy y en el motor nuevo. 13 dan idénticos: puntos, cierre, objetivos, cambios de paso e instante de fin. Los otros 2 difieren:
   - uno de forma esperada (DV-37);
   - otro en un caso que con el simulador no se da y queda como riesgo para la 10a (QA-V4a-L01).
3. **Sonda de formatos.** Los 84 casos de borde dan el mismo texto que el legacy.

| Qué | Resultado |
|---|---|
| `dotnet build` (solución entera) | 0 errores, 0 warnings |
| `RowMate.Tests.Simulator` | 173 / 173 pasan |
| `RowMate.Tests.EditMode` | 2050 en total: 2028 pasan, 22 omitidos (`[Ignore]` de equipo, pantalla o GitHub: NO VERIFICABLE en dotnet), 0 fallan; los 184 del espacio `Workout` pasan |
| Matriz (`-Matrix`) | PASA 425, FALLA 0, IGNORADO 1, PENDIENTE UNITY 109, SIN TEST 0 |
| Sonda del motor (§4) | 15 escenarios: 13 idénticos al legacy; 2 diferencias (DV-37, esperada; QA-V4a-L01) |
| Sonda de formatos (§5) | 84 casos: 0 diferencias |
| Casos legacy LT-35 a LT-45 (§6) | 11 / 11 portados 1 a 1, con el mismo nombre y las mismas afirmaciones; pasan |

---

## 1. Método

### 1.1 Lectura

**Legacy, solo lectura.** Se leyeron enteros:

- `lib/features/workout/workout_provider.dart`;
- `lib/features/workout/step_target_tracker.dart`;
- `lib/features/workout/series_tracker.dart`;
- de `lib/features/workout/immersive_workout_screen.dart`: `:1-300` (ciclo de vida, diálogos), `:300-560` (barra de pasos) y `:560-760` (HUD y semáforo);
- de `lib/features/workout/workout_screen.dart`: `:1-205`;
- de `lib/features/workout/series_panels.dart`: `:1-30`, `:90-100` y `:330-345`;
- de `lib/core/models/`: `routine.dart`, `workout_session.dart:60-185` e `interval_step.dart:60-140`;
- los catálogos `lib/l10n/app_es.arb` y `app_en.arb` (claves de Workout);
- los tests `test/workout_provider_hr_test.dart`, `test/step_target_tracker_test.dart`, `test/series_panels_test.dart:25-50` y `test/series_tracker_test.dart:150-225`.

**App.** Se leyeron:

- todo `Assets/RowMate/Core/Workout/`;
- de `Assets/RowMate/App/Workout/`: `WorkoutHudStore.cs`, `WorkoutHudState.cs`, `WorkoutStore.cs` y `WorkoutToast.cs`;
- de `Assets/RowMate/Core/Models/`: `SessionStats.cs`, `WorkoutSession.cs`, `DataPoint.cs`, `RowingData.cs` y `RoutineExpansion.cs`;
- `Assets/RowMate/Core/Text/TimeFormats.cs`;
- `Assets/RowMate/Core/Common/FrameScheduler.cs` y, en parte, `RealtimeTimerQueue.cs`;
- de `Assets/RowMate/Presentation/Workout/`: `StepInfoBar.cs`, `HudControls.cs`, `FinishWorkoutDialog.cs`, `StartWorkoutDialog.cs` e `ImmersiveWorkoutPage.cs:30-230`;
- `Assets/RowMate/Bootstrap/AppRoot.cs:225-275`;
- `Assets/RowMate/Localization/strings.es.json` y `strings.en.json`;
- las pruebas de `Assets/RowMate/Tests/EditMode/Workout/` (`WorkoutGoldenTests`, `WorkoutProviderHrTests`, `StepTargetTrackerTests`, `SeriesPanelsTests`, `StepAdvanceTests`, `StepProgressTests`, `SanitizedDistanceTests`);
- el oráculo `tools/oracle/engine/test/engine_vectors_test.dart`.

### 1.2 Pruebas del repo

```
powershell -NoProfile -File tools/dotnet/run-tests.ps1 -ArtifactsPath <scratch>/it4a/lens-paridad/artifacts
powershell -NoProfile -File tools/dotnet/run-tests.ps1 -Matrix -ArtifactsPath <scratch>/it4a/lens-paridad/artifacts-matrix
```

Salida real (las dos corridas dan lo mismo):

```
Build succeeded.
    0 Warning(s)
Test Run Successful.
Total tests: 173
     Passed: 173
Test Run Successful.
Total tests: 2050
     Passed: 2028
    Skipped: 22
[run-tests] matriz: <scratch>/it4a/lens-paridad/artifacts-matrix/TestResults/ac-matrix.md (PASA 425, FALLA 0, IGNORADO 1, PENDIENTE UNITY 109, PENDIENTE WP-B 0, SIN TEST 0)
```

Los 22 omitidos son `[Ignore]` que no se pueden verificar en dotnet: de equipo (Android, iOS, Windows), de pantalla (Unity) o de GitHub. Los de la 4a son `AndroidBackgroundTwoMinutes`, `AndroidBackOpensTheConfirmation`, `AndroidFirstFrameAfterBackground`, `AndroidMonotonicClockAcrossDeepSleep` y `AndroidScreenStaysOnAfterResume`: NO VERIFICABLE sin módulo ni teléfono.

### 1.3 Sonda contra el motor legacy

El oráculo del repo (`Assets/RowMate/Tests/Golden/oracle/workout_engine.json`, 11 escenarios) es la mejor garantía de AC-WKT-06-07, y `WorkoutGoldenTests` lo compara campo por campo. Sus escenarios no tocan varios caminos del foco: pasos de 0 s y de duración negativa, paso sin meta, terminar en pausa, último paso por distancia, tiempo y distancia a la vez, pulso del sensor en los puntos, pausa antes del primer segundo, pausa que cruza un cambio de paso, dos sesiones seguidas, varias pausas en un mismo segundo, terminar en el segundo del cambio y último paso de 0 s.

Para cubrirlos se armó una **copia del arnés del oráculo** en `<scratch>/it4a/lens-paridad/oracle-probe/`, nunca en el repo. Esa copia corre el `WorkoutProvider` del legacy, sin tocarlo, con `fakeAsync`, el remo de prueba R-4 (L(k) en el instante k − 0,5 s) y una base que solo anota. Para correrla se usó `<flutter-sdk>/bin/flutter test`.

El **mismo guion** corre en el motor nuevo en una copia de la solución dotnet: `<scratch>/it4a/lens-paridad/dotnet/RowMate.Tests.Probe/`, con el `WorkoutHarness` del repo enlazado. `compare.js` compara los dos:

- los 11 campos de cada punto;
- el cierre;
- los objetivos;
- los cambios de paso, en la grilla de 100 ms del oráculo;
- el instante de fin.

La salida está en `<scratch>/it4a/lens-paridad/compare-output.txt`.

**Guardia del legacy.** Antes y después de cada corrida se tomaron tres controles: `git -C ../rowmate-legacy status --porcelain`, el listado `status --porcelain --ignored --untracked-files=all` y una instantánea de metadatos (ruta, tamaño y fecha) de todo archivo fuera de `.git/`. Los tres dieron idénticos: el status vacío y 0 diferencias en el listado y en la instantánea. La sonda no lee ni nombra el módulo de credenciales del legacy.

### 1.4 Sonda de formatos

`oracle-probe/test/formats_probe_test.dart` calcula en el legacy:

- `formatLapTime`, `formatSplit` y `formatMeters` de `series_panels.dart`, importadas;
- `toStringAsFixed(1)` (el SPM del HUD);
- `pace500mFormatted` de `RowingData`;
- copiadas tal cual porque son privadas, `_timeFormatted` (`immersive_workout_screen.dart:569-577`) y la cuenta regresiva (`:456-457`).

Las entradas son de borde: mitades, negativos, `59.95`, `0.49999999999999994` y valores enormes. `RowMate.Tests.Probe/probe/FormatsProbe.cs` compara con `WorkoutFormats`:

```
[PROBE] formatos comparados: 84, diferencias: 0
  Passed SameAsTheLegacy [8 ms]
```

---

## 2. Issues

| ID | Sev. | Resp. | Issue | Evidencia | Corrección sugerida |
|---|---|---|---|---|---|
| QA-V4a-L01 | menor | po | **El saneamiento de Q-55 frena un paso por distancia si el monitor no informa la distancia una vez por segundo.** El tope es de 10 m por segundo de reloj activo, y el motor lo alimenta una vez por segundo, siempre con `dt = 1`. Si la distancia sube en saltos de más de 10 m, cada salto se descarta. Eso pasa con un monitor que la actualiza cada 2 s a más de 5 m/s (split mejor que 1:40), con dos notificaciones BLE que caen en el mismo segundo o con la ráfaga que llega después de un *loop* frenado (arrastrar o redimensionar la ventana en Windows; DT4a-10). En esos casos un paso por distancia **no termina nunca**. El legacy, que cuenta la distancia cruda, sí termina. Los parciales del legacy tienen el mismo problema, pero ahí solo se pierden vueltas y no queda trabada la rutina. Con el simulador no pasa: emite una lectura por segundo y llega a lo sumo a 5,6 m/s con 500 W (`RowingSimulator.Tick`). La demo de la 4a no se ve afectada | Sonda `dist100_monitor_cada_2s`: `Distancia 100` (trabajo de 100 m y descanso de 5 s) con R-4, salvo la distancia, que suma 11 m en cada k par. El legacy cambia de paso a los 20 s y termina a los 25 s, con 6 puntos y la sesión cerrada (1132 m, 325 s). El nuevo, a los 120 s, sigue en el paso 0 con 24 puntos y la sesión abierta (`compare-output.txt`). Código: `Assets/RowMate/Core/Workout/SanitizedDistance.cs:34-44` y `Assets/RowMate/Core/Workout/WorkoutEngine.cs:359`; legacy: `series_tracker.dart:120-127` y `workout_provider.dart:198-201` | (PO) Sumar el caso a P-04 y a DV-26 ("un monitor que no manda la distancia cada segundo puede trabar un paso por distancia") y decidirlo antes de la 10a. (Architect, 10a) Medir la cadencia y el *jitter* del AMS-670B. Si no es una lectura por segundo estable, comparar el salto contra el tiempo transcurrido desde la última lectura cruda que cambió, o contra la hora de llegada de la lectura, en el paso y en los parciales a la vez, para que Q-55 siga contando los mismos metros. Sumar a SAD y a SDI un caso con un monitor de 0,5 Hz |
| QA-V4a-L02 | menor | architect | **El oráculo del motor no protege varios caminos que esta lente comprobó a mano.** Los 11 vectores del repo no tienen: pasos de 0 s y de duración negativa, paso sin meta con `Terminar`, terminar en pausa, último paso por distancia, tiempo y distancia a la vez, pulso del sensor en los puntos, pausa antes del primer segundo, pausa que cruza un cambio de paso, dos sesiones seguidas, varias pausas en un mismo segundo, terminar en el segundo del cambio y último paso de 0 s. Hoy dan idénticos al legacy, pero una regresión ahí no la vería `WorkoutGoldenTests`. Además, `SessionsAreIdenticalToTheLegacy` saltea entero los 3 escenarios de Q-55, aunque sus puntos hasta el cambio de paso coinciden (segundos 5 a 25) | Sonda §4: 13 escenarios nuevos idénticos. En DV-37 el legacy lanza `Invalid argument(s): 0` en `int.clamp`, en `WorkoutProvider.stepProgress` (`workout_provider.dart 103:65`). `Assets/RowMate/Tests/EditMode/Workout/WorkoutGoldenTests.cs:26-29` y `:63-66` | Sumar esos guiones a `tools/oracle/engine/test/engine_vectors_test.dart` (con `runScript`, como en `<scratch>/it4a/lens-paridad/oracle-probe/test/probe_test.dart`) y regenerar `workout_engine.json` con `run-oracle.ps1 -Engine`. Compararlos en `WorkoutGoldenTests`, con DV-37 como diferencia esperada (la excepción del legacy, anotada como evidencia), y comparar en los escenarios de Q-55 los puntos anteriores al primer cambio de paso |

---

## 3. Paridad regla por regla

**Reglas del motor:**

| # | Regla | Legacy | App | Resultado |
|---|---|---|---|---|
| 1 | Cuatro fases; arranca en reposo | `workout_provider.dart:13`, `:49` | `Core/Workout/WorkoutPhase.cs`; `WorkoutEngine.cs:49` | PASA (`FourPhasesStartingIdle`) |
| 2 | Lecturas del remo y del pulsómetro en todas las fases; el estado del sensor sin el valor actual | `:82-86` (*streams* de difusión) | `WorkoutEngine.cs:91-94` (`Data` y `Bpm` son `IEventSource`; `Status` con `emitCurrent: false`) | PASA (LT-35 a LT-38) |
| 3 | Inicio libre: fase, hora, muestreo en 0, alta; el reloj después del alta | `:115-117`, `:133-149` | `WorkoutEngine.cs:121-130`, `:239-295` (o a los 100 ms, DT4a-02) | PASA; con la base al día, idéntico (oráculo `free_*`) |
| 4 | Inicio con rutina: expande, paso 0 con 0 s, distancia de inicio, posiciones, objetivos en 0; id y nombre de la plantilla | `:120-131` | `WorkoutEngine.cs:135-161` | PASA (`two_sessions`: la segunda sesión arranca desde cero, con la distancia de ese momento) |
| 5 | El segundo, en orden: relojes y muestreo; punto cada 5 s; metros, objetivos y fin del paso; aviso | `:151-190` | `WorkoutEngine.cs:335-366` | PASA (oráculo y las 13 sondas) |
| 6 | Paso por tiempo: `≥` duración; si tiene tiempo y distancia, manda el tiempo | `:196-197` | `WorkoutEngine.cs:374-377` | PASA (`time_and_distance`: cambio a los 7 s y fin a los 9 s) |
| 7 | Paso por distancia: metros desde el inicio del paso `≥` largo | `:198-201` (crudo) | `WorkoutEngine.cs:378-381` con `SanitizedDistance.cs:20-44` = `series_tracker.dart:88-145` (Q-55) | PASA con DV-26 (R-4 25, R-4R 26, R-4Z 27, pausa 25; legacy 25, 285, 25, 11). Ver QA-V4a-L01 |
| 8 | Cambio de paso: cierra objetivos, índice + 1, reloj del paso en 0, distancia de inicio de ese segundo | `:203-209` | `WorkoutEngine.cs:392-398` | PASA (`StepChangeKeepsTheExtraMetersInTheOldStep`, `corta_pause_across_change`) |
| 9 | Último paso: terminada en el mismo segundo, timer cancelado, cierre; el índice queda en el último | `:210-216` | `WorkoutEngine.cs:401-419` | PASA (`zero_steps` fin a los 9 s, `last_step_zero` a los 4 s, `dist_last` a los 16 s) |
| 10 | Paso de 0 s: termina en el primer segundo | `:196-197` | igual | PASA (`zero_steps`: un paso por segundo, como el legacy) |
| 11 | Paso de duración negativa | El motor lo termina en el segundo 1, pero `stepProgress` lanza (`:103`, confirmado: `Invalid argument(s): 0`) | `StepProgress.cs:46-49`: restante 0, mismo fin y mismos puntos | DV-37 (esperada) |
| 12 | Estado del paso solo con rutina y fuera de reposo; restante acotado o −1; progreso; por terminar 1-5 | `:16-42`, `:98-112` | `StepProgress.cs:36-79`; `WorkoutEngine.cs:624-634` | PASA (`StepProgressTests`, 9) |
| 13 | "A continuación": restante de 0 a 15 y un paso siguiente; nunca por distancia, sin meta ni en el último | `immersive_workout_screen.dart:342-346` | `StepProgress.cs:82-89`; `WorkoutHudStore.cs:154-160` | PASA (`UpNextZeroToFifteen`: 16 no, 15 sí, 0 sí, último no) |
| 14 | Fusión: el sensor manda; un estado ≠ conectado con bpm previo pone 0; sin bpm previo no avisa | `:220-237` | `HeartRateFusion.cs:19-47`; `WorkoutEngine.cs:548-559` | PASA (LT-35 a LT-38; `hr_points`: 120, 130, 0, 0) |
| 15 | Pausar: fase y timer; la lectura sigue llegando | `:239-243` | `WorkoutEngine.cs:164-175` | PASA |
| 16 | Reanudar: ritmo nuevo desde ese instante (Q-58); la distancia de ese momento es la referencia | `:245-250` | `WorkoutEngine.cs:179-194`; `ActiveClock.cs:26-38` | PASA (oráculo `free_r4_pause_q58`; `pause_resume_jitter`, `pause_early`, `corta_pause_across_change`) |
| 17 | Terminar: punto final con la lectura y el paso de ese momento, estadísticas sobre todos los puntos, cierre, "última terminada" | `:252-296` | `WorkoutEngine.cs:421-491`; `WorkoutSession.cs` (`Closed`); `SessionStats.cs` | PASA (`finish_in_pause`: punto final 320 s, 1080 m, paso 1; `finish_at_step_change`: final del paso 1; `free_finish_at_5`: los dos últimos iguales) |
| 18 | Volver a reposo: borra rutina, índice, relojes, sesión, puntos, posiciones y objetivos; no la lectura, el bpm ni la última terminada | `:305-318` | `WorkoutEngine.cs:506-522` | PASA (`IdleKeepsTheLastReadingAndTheLastFinished`, `two_sessions`) |
| 19 | Un punto cada 5 s activos, con la lectura fusionada y el paso; en libre, índice y tipo vacíos | `:156-173` | `WorkoutEngine.cs:345-354`; `TelemetryPoint.cs:13-40` | PASA |
| 20 | P99 de vatios (redondeado) y de SPM; máximos de distancia, tiempo del monitor y kcal | `workout_session.dart:111-136` | `SessionStats.cs` (It. 1) | PASA (cierres idénticos en el oráculo y en las 13 sondas) |
| 21 | Objetivos por paso: qué cuenta como objetivo, lectura parada, split solo > 0, promedios | `step_target_tracker.dart:23-70` | `StepTargetTracker.cs` | PASA (LT-39 a LT-44; objetivos idénticos en `corta_pause_across_change` y `two_sessions`) |
| 22 | Progresiones por repetición (base + progresión × r, solo si existen los dos; también vatios máx.) | `routine.dart:29-70` | `RoutineExpansion.cs` (`Saturate` solo alcanza valores que el editor no guarda, DT4a-16) | PASA (oráculo `prueba4a_r4`, `corta_r4_19`) |

**Lo que el HUD calcula:**

| # | Regla | Legacy | App | Resultado |
|---|---|---|---|---|
| 23 | Semáforo: SPM `<` objetivo, Watts `<` mín. (sin mirar el máx.), Split `>` objetivo; también con la rutina terminada y en pausa | `immersive_workout_screen.dart:579-602` (`sp` existe también en terminada) | `TargetLight.cs:41-63`; `WorkoutHudStore.cs:91-100` | PASA |
| 24 | Reloj del HUD = el activo; distancia y kcal = los del monitor; BPM solo con pulso > 0; kcal siempre | `:103`, `:569-577`, `:698`, `:709-733` | `WorkoutHudStore.cs:101-107` | PASA |
| 25 | Cuenta: por tiempo `M:SS`; por distancia `{m}m`; sin meta `?` (DV-25) | `:456-458` | `WorkoutHudStore.cs:135-152` | PASA |

---

## 4. Escenarios de la sonda (legacy contra el motor nuevo)

Con R-4 salvo donde se indica. "Idéntico" quiere decir que coinciden los puntos (los 11 campos, en orden), los cierres, los objetivos, los cambios de paso en la grilla de 100 ms y el instante de fin automático.

Diferencia esperada de forma: con el fin automático, el motor nuevo vuelve solo a reposo al volver el cierre (DT4a-15, provisorio hasta la 4b). El legacy, en cambio, queda en "terminada" hasta el `reset()` del diálogo. Por eso, después del fin automático la sonda no compara los cambios de paso: lo que se compara es el instante del fin.

| Escenario | Qué prueba | Legacy | Nuevo |
|---|---|---|---|
| `zero_steps` | Pasos de trabajo 0 s, descanso 0 s y trabajo 7 s | Pasa a los pasos 1 y 2 en los segundos 1 y 2 y termina en el 9; 2 puntos (305 y 309, paso 2) | Idéntico |
| `neg_step` | Pasos de trabajo −5 s y descanso 3 s | `stepProgress` lanza mientras dura el paso 0; termina el paso en el segundo 1 y la rutina en el 4; 1 punto | Restante 0 sin excepción; mismo fin y mismo punto (DV-37) |
| `nogoal_finish` | Paso sin meta; `Terminar` a los 12,3 s | 3 puntos (305, 310, 312, paso 0); 1048 m y 312 s | Idéntico |
| `finish_in_pause` | `Corta`; pausa a los 12,4 s y `Terminar` a los 20,0 s, en pausa | Punto final con la lectura de 20 s: 320 s, 1080 m, paso 1 `rest` | Idéntico |
| `dist_last` | Pasos de calentamiento 3 s y trabajo 50 m | Cambia en el segundo 3 y termina en el 16; 4 puntos | Idéntico |
| `time_and_distance` | Trabajo de 7 s y 1000 m, descanso de 2 s | Manda el tiempo: cambia en el 7 y termina en el 9 | Idéntico |
| `hr_points` | Libre; bpm 120 a los 2,2 s, 130 a los 7,2 s y `disconnected` a los 11,2 s; `Terminar` a los 16 s | `heart_rate` 120, 130, 0 y 0 | Idéntico |
| `pause_early` | Libre; pausa a los 0,05 s, reanuda a los 2,05 s; `Terminar` a los 8 s | 5 s en marcha; el punto del segundo 5 a los 7,05 s (307) y el final (308) | Idéntico |
| `corta_pause_across_change` | `Corta`; pausa a los 9,5 s y reanuda a los 15,3 s | Cambios a los 16,3, 21,3 y 31,3 s; fin a los 36,3 s; 7 puntos; pasos 0 y 2 cumplidos | Idéntico |
| `two_sessions` | Libre de 0 a 6 s; `Corta` desde los 9 s | Dos sesiones; la segunda empieza con el reloj, el muestreo y los metros desde cero (puntos 314 a 339) | Idéntico |
| `pause_resume_jitter` | Libre; pausa y reanuda a los 3,2, 3,4, 3,6 y 3,8 s | Q-58 en cada reanudación: 11 s a los 12 s; puntos 306, 311 y 312 | Idéntico |
| `finish_at_step_change` | `Corta`; `Terminar` a los 10,0 s, el segundo del cambio | Punto del segundo 10 del paso 0 y punto final del paso 1, con la misma lectura | Idéntico |
| `last_step_zero` | Pasos de trabajo 3 s y descanso 0 s | Termina en el segundo 4; 1 punto (paso 1) | Idéntico |
| `free_finish_at_5` | Libre; `Terminar` a los 5,0 s | El punto del segundo 5 y el final son iguales (305) | Idéntico |
| `dist100_monitor_cada_2s` | `Distancia 100`; la distancia sube 11 m cada 2 s | Cambia a los 20 s y termina a los 25 s | A los 120 s sigue en el paso 0: **QA-V4a-L01** |

---

## 5. Formatos de F-WKT-11 (84 casos, 0 diferencias)

| Formato | Legacy | App | Entradas de borde (todas iguales) |
|---|---|---|---|
| Vuelta `M:SS.d` | `series_panels.dart:9-14` | `WorkoutFormats.LapTime` | 166.667, 59.95 → `1:00.0`, 59.949999 → `0:59.9`, 0.05 → `0:00.1`, −0.05 → `0:59.9`, −65 → `-1:55.0`, 1e6 → `16666:40.0`, 12.25 → `0:12.3` |
| Split de repetición `M:SS` | `:16-20` | `RepSplit` | 0.5 → `0:01`, 2.5 → `0:03`, −0.5 → `0:59`, −59.5 → `-1:00`, 0.49999999999999994 → `0:00` |
| Metros de los paneles | `:22-24` (`NumberFormat('#,##0','es')`) | `PanelMeters` | 1000 → `1.000 m`, −1245 → `-1.245 m`, 16777215 → `16.777.215 m` |
| SPM (`toStringAsFixed(1)`) | `immersive_workout_screen.dart:625` | `Spm` | 24.35 → `24.4`, 0.15 → `0.1`, 0.25 → `0.3`, 23.95 → `23.9`, 1.45 → `1.4`, 2.675 → `2.7`, −0.05 → `-0.1` |
| Reloj `H:MM:SS` / `MM:SS` | `:569-577` | `SessionClock` | 3599 → `59:59`, 3600 → `1:00:00`, 359999 → `99:59:59`, −61 → `58:59` |
| Cuenta `M:SS` | `:456-457` | `Countdown` | 359999 → `5999:59`, −61 → `-1:59` |
| Split en vivo | `rowing_data.dart:24-29` | `LiveSplit` | 0 y −5 → `--:--`, 6000 → `100:00`, 65535 → `1092:15` |

Los demás formatos de la ficha tienen su prueba en `WorkoutFormatsTests`, que pasa:

- AC-WKT-11-03 a -05;
- -08 (`{m}m` y `?`);
- -12 (`HH:mm`);
- -13 (etiqueta del paso expandido);
- -14 (resumen);
- -15 (resumen final);
- -16 (cualquier cultura: `de-DE`, `ar-SA`, `es-AR`).

---

## 6. Casos legacy LT-35 a LT-45

| LT | Legacy | Portado en | ¿Mismo nombre y mismas afirmaciones? | Resultado |
|---|---|---|---|---|
| LT-35 a LT-38 | `test/workout_provider_hr_test.dart:80`, `:125`, `:148`, `:167` | `WorkoutProviderHrTests` (`TestName` = nombre del legacy) | Sí: 90 → 142 con 100 W y 2 avisos → 142 y 120 W → 0 → 0 → 90; el orden real de la caída; `connecting`; 0 avisos | PASA |
| LT-39 a LT-44 | `test/step_target_tracker_test.dart:20` a `:65` | `StepTargetTrackerTests` | Sí, 1 a 1 (lecturas 150 / 24 / 130 y trabajo de 60 s) | PASA |
| LT-45 | `test/series_panels_test.dart:32` `formatos` | `SeriesPanelsTests.formatos` | Sí: las 4 afirmaciones | PASA |

Los 4 casos `_hardening` de `test/series_tracker_test.dart:155-220` (QA-G4a-P06) están portados en `SanitizedDistanceTests`, con su `// legacy:`, y pasan.

---

## 7. Textos del catálogo del HUD y de los diálogos

| Dónde | Clave | es (app = legacy) | en (app = legacy) |
|---|---|---|---|
| Diálogo de terminar | `workoutFinishTitle`, `workoutFinishContent`, `cancel`, `workoutFinish` | `Terminar entrenamiento`, `Se guardará la sesión. ¿Continuar?`, `Cancelar`, `Terminar` (= los fijos de `immersive_workout_screen.dart:129-137`) | `Finish workout`, `The session will be saved. Continue?`, `Cancel`, `Finish` |
| Diálogo de inicio | título = nombre de la rutina; `workoutStartContent`; `workoutStart` | `{summary}⏎⏎¿Iniciar el entrenamiento?` (el `¿` es Q-02), `Iniciar` | `{summary}⏎⏎Start the workout?`, `Start` |
| Controles | `workoutPause`, `workoutResume`, `workoutFinish` | `Pausa`, `Reanudar`, `Terminar` | `Pause`, `Resume`, `Finish` |
| Tarjetas, unidades y barra libre | `hudSpm`, `hudSplit`, `hudWatts`, `hudUnitWatts`, `hudKcal`, `hudBpm`, `hudUnitMeters`, `hudFreeTitle` | `SPM`, `Split /500m`, `Watts`, `W`, `kcal`, `BPM`, `m`, `Entrenamiento Libre` (= los fijos de `:510`, `:624-726`) | iguales; `Free Workout` |
| Próximo | `hudUpNext` + `" "` + tipo | `Próximo: Descanso` (DV-24; el legacy `'Up next: '`) | `Up next: Rest` |
| Aviso provisorio | `workoutCompleted` | `¡Entrenamiento completado!` | `Workout Completed!` |

Todo sale del catálogo:

- en `Presentation/Workout/` no hay textos de interfaz fijos (solo el `""` que vacía la pastilla y el espacio que separa `Próximo:` del tipo);
- `CatalogParityTests.TwelveItFourAKeys` y `SameKeysInBothLanguages` pasan;
- `workoutStartTitle` sigue sin uso (paridad).

---

## 8. Matriz AC → test → resultado (lo de esta lente)

De `<scratch>/it4a/lens-paridad/artifacts-matrix/TestResults/ac-matrix.md` y de las sondas.

| AC | Test (dotnet) | Resultado |
|---|---|---|
| AC-WKT-01-01 a -08, -10, -11 | `WorkoutEngineLifecycleTests` (`FourPhasesStartingIdle`, `StartFreeCreatesTheSessionThenTheClock`, `ClockNeverWaitsMoreThan100Ms`, `RoutineStartExpandsAndPositions`, `SessionKeepsTheTemplateIdAndName`, `PauseFreezesClocksSamplingTargetsAndMeters`, `FinishSavesThenGoesIdle`, `IdleKeepsTheLastReadingAndTheLastFinished`, `NoUndoAndStartOnlyFromIdle`, `CreateFailureStillRuns`), `ActiveClockTests.ResumeRestartsTheRhythm`, `StepAdvanceTests.ResumeRebasesTheMeters`, `WorkoutStoreTests.SecondStartIsIgnored` + sondas §4 | PASA |
| AC-WKT-01-09 | PlayMode | PENDIENTE UNITY (QA principal) |
| AC-WKT-02-01 a -07 | `ActiveClockTests`, `SecondOrderIsTheLegacyOrder`, `HudClockIsTheActiveClock`, `SixHundredSecondsWithALateFrame`, `WorkoutBackgroundTests` | PASA |
| AC-WKT-02-10 | `AndroidBackgroundTwoMinutes`, `AndroidMonotonicClockAcrossDeepSleep` | NO VERIFICABLE (sin módulo ni teléfono) |
| AC-WKT-03-01 a -10 | `StepAdvanceTests` (17), `StepProgressTests.FinishedRoutine*`, `WorkoutGoldenTests.LegacyDistanceColumn` + sondas §4 | PASA (03-03 con DV-26, ver QA-V4a-L01; 03-07 con DV-37) |
| AC-WKT-04-01 a -07 | `StepProgressTests` (9) | PASA |
| AC-WKT-05-01 a -05 | `WorkoutProviderHrTests` (LT-35 a LT-38 y `FusionInEveryPhaseFeedsHudAndPoints`), `TelemetryTests.PointsCarryTheFusedPulse` + `hr_points` | PASA |
| AC-WKT-05-06 | PlayMode con el pulsómetro simulado | PENDIENTE UNITY |
| AC-WKT-06-01 a -04, -06 a -09, -11 | `TelemetryTests` (10), `WorkoutGoldenTests` (`Corta`, `SessionsAreIdenticalToTheLegacy`), `SqliteSessionRepositoryTests.CreateWritesTheLegacyRow`, `LastFinishedOnlyWhenTheCloseSucceeds` + sondas §4 | PASA |
| AC-WKT-09-01 a -07 | `StepTargetTrackerTests` (LT-39 a LT-44), `StepAdvanceTests.TargetsFromTheEngine` | PASA |
| AC-WKT-10-01 a -04, -06 | `TargetLightTests` (5), `WorkoutHudStoreTests.EvaluatedOnEveryReadingAlsoInPause` | PASA |
| AC-WKT-10-05, -07 a -09 | PlayMode y capturas | PENDIENTE UNITY |
| AC-WKT-11-01 a -16 | `WorkoutFormatsTests` (43), `SeriesPanelsTests.formatos` + sonda de formatos §5 | PASA |
| AC-WKT-14-02 a -06, AC-WKT-17-04 a -06, AC-WKT-20-03 a -07 | PlayMode (`HudControlsTests`, `WorkoutScreenTests`, `HudLayoutTests`) | PENDIENTE UNITY. La revisión estática no encontró diferencias de regla: `StepInfoBar.Apply` usa `UpNextType` y `EndingSoon` del store; `ImmersiveWorkoutPage.OnState` cancela el diálogo fuera de marcha y de pausa; `OnBackRequested` abre la confirmación; Escape no cierra páginas (`BackNavigation.cs:18`) |
| AC-BLE-10-07 | `WorkoutEngineLifecycleTests.EngineIgnoresTheConnection` | PASA |
| AC-APP-10-10 | `CatalogParityTests.TwelveItFourAKeys`, `SameKeysInBothLanguages` | PASA |
| LT-35 a LT-45 | §6 | PASA |

---

## 9. Observaciones sin issue

- **O-01 · DV-37 confirmada en el motor real del legacy.** Con un paso de −5 s, `WorkoutProvider.stepProgress` lanza `Invalid argument(s): 0` (`int.clamp`, `workout_provider.dart 103:65`). Mientras tanto, el motor lo termina en el segundo 1, como dice AC-WKT-03-07. El inmersivo del legacy falla en ese paso; la app muestra `0:00`.
- **O-02 · Fin automático y la 4b.** En el legacy, después de completar la rutina, la fase queda en "terminada" y los resultados de objetivos siguen vivos hasta que se toca el diálogo de completado (`immersive_workout_screen.dart:207-290`). La 4a vuelve sola a reposo al volver el cierre (DT4a-15). `ReturnToIdle` borra `Targets` (`WorkoutEngine.cs:519`), así que las píldoras y el diálogo de la 4b (F-WKT-14, F-WKT-21) van a necesitar que el motor espere al diálogo antes de borrar. DT4a-15 ya lo anuncia ("La 4b suma la espera del diálogo de completado").
- **O-03 · El cuadro del fin es el del legacy.** En el segundo en que termina la rutina, el motor publica "terminada" con los valores de ese segundo y no publica antes un "en marcha" con esos valores. El legacy hace lo mismo: avisa después de la parte sincrónica de `finish()` (`:189`, `:210-216`).
- **O-04 · El texto de un objetivo de split negativo.** Con una progresión negativa que deja el split en −2 s, `obj.` muestra `00:58` (`TimeFormats.MinSec`, con `~/` y `%` de Dart). La pastilla, por la regla de `interval_step.dart:87-91`, muestra `00:58/500m`, lo mismo que el legacy. Es paridad y la tarjeta queda siempre fuera (AC-WKT-10-04). El caso solo se da si una progresión baja el split a menos de 0, y los topes de Q-92 lo permiten porque van por valor absoluto. Se informa al PO por si quiere declararlo.
- **O-05 · El orden de la vuelta del segundo plano.** `AppRoot.OnApplicationPause(false)` hace primero el alcance y recién después `SetBackground(false)` (`AppRoot.cs:251-258`). Así, la foto única con `Instant` sale con el estado ya alcanzado, que es lo que pide AC-WKT-02-08.

---

## 10. Verificación hecha

1. **Legacy, solo lectura.** `git -C ../rowmate-legacy status --porcelain` quedó vacío al empezar y al terminar. Al terminar dieron idénticos al inicio el listado con ignorados y la instantánea de metadatos de todo archivo fuera de `.git/` (§1.3). No se leyó ni se nombró el módulo de credenciales del legacy.
2. **Sin Unity.** Unity no se abrió.
3. **dotnet y Flutter solo en el scratch.** dotnet corrió con `-ArtifactsPath <scratch>/it4a/lens-paridad/...`; la sonda, en una copia de los `.csproj` en `<scratch>/it4a/lens-paridad/dotnet/` con `--artifacts-path` propio. La copia del oráculo y sus archivos de `pub` están en `<scratch>/it4a/lens-paridad/oracle-probe/`. Antes y después, `git status --porcelain` del repo da la misma cantidad de entradas, sin `bin/` ni `obj/` nuevos.
4. **Un solo archivo escrito en el repo:** este. No se hicieron *commits*.
5. **Sin rutas de la PC.** Este archivo cita el repo con rutas relativas, el legacy como [rowmate-legacy](https://github.com/figuibej/rowmate-legacy), el scratch como `<scratch>` y el SDK de Flutter como `<flutter-sdk>`. No tiene rutas absolutas, nombres de usuario ni datos del equipo.
