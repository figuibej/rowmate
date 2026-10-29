# It. 4a — Motor de sesión, guardado incremental, reposo y HUD funcional: gate de armonía de QA (lente técnica)

| Campo | Valor |
|---|---|
| Autor | rowmate-qa |
| Fecha | 2026-10-28 |
| Iteración | 4a · `04a-motor-hud-funcional` ([ROADMAP §It. 4a](../../ROADMAP.md)) |
| Lente | **Técnica**: cobertura AC → componente → test o captura; guardado incremental sobre el esquema v8; hilos y reloj; segundo plano (Q-91) y pantalla encendida por plataforma; R3-08; ciclo de vida de UI Toolkit; rendimiento del HUD; paquetes disjuntos y contrato USS; contradicciones entre spec, diseño técnico y notas de diseño (incluidos los "Pedidos al Architect"); regla de repo público. El aspecto visual y el texto de producto los miran las otras lentes |
| Entrada | [01-po-spec](01-po-spec.md) (158 AC, 11 LT, 30 CAP, 14 DEMO, 3 VID), [02-tech-design](02-tech-design.md), [04-design-notes](04-design-notes.md) (DN4a-01 a DN4a-22), [00-deuda-it3](00-deuda-it3.md), [00-deuda-diseno](00-deuda-diseno.md); [ADR 017](../../architecture/adr/017-segundo-plano.md); [03-qa-harmony de la It. 3](../03-rutinas/03-qa-harmony.md) (lecciones) |
| Legacy (solo lectura, `3ae0f3a`) | [rowmate-legacy](https://github.com/figuibej/rowmate-legacy): `lib/features/workout/workout_provider.dart` (entero), `step_target_tracker.dart` (entero), `series_tracker.dart` (entero), `immersive_workout_screen.dart` (entero), `workout_screen.dart:1-210`, `series_panels.dart:1-110`, `:320-392`, `lib/core/models/{rowing_data,routine,interval_step,workout_session}.dart` (enteros), `lib/core/database/database_service.dart:1-60`, `:95-145`, `:205-300`, `lib/main.dart` (entero), `lib/core/bluetooth/heart_rate_service.dart:1-20`, `lib/features/device/device_provider.dart:85-100`, `lib/features/history/history_provider.dart` (las líneas de `getStatsForSessions`), `test/workout_provider_hr_test.dart`, `test/step_target_tracker_test.dart`, `test/series_panels_test.dart` (enteros); `<pub-cache>/wakelock_plus-1.4.0/lib/src/wakelock_plus_windows_plugin.dart` y `android/.../Wakelock.kt`. Se revisaron los `import` de la clausura de `workout_provider.dart` (ninguno llega al módulo de credenciales); no se leyó `lib/core/strava/strava_secrets.dart`. `git -C ../rowmate-legacy status --porcelain` vacío antes y después |
| Verificación hecha | `dotnet test` de línea base (§1); script de cobertura de la matriz; los 26 casos de §3 recalculados contra el legacy; lectura del código de la app que el diseño reutiliza (programador, cola de timers, ejecutor de la base, apertura y pragmas de SQLite, `AppRoot`, `DialogLayer`, `TabularNumber`, `FitBox`, reglas de topes y expansión, matriz de `run-tests.ps1`, `.gitignore`). **Unity no se abrió** (la tarea es sin Unity) |

---

## 0. Veredicto

**APROBADO CON OBSERVACIONES.** **0 bloqueantes, 5 mayores, 13 menores** (§14).

- **Cobertura**: los 158 AC del spec están en la matriz de 02-tech-design §8.4 (script propio: 158 de 158, 0 faltantes, 0 sobrantes) y todos tienen un componente responsable. 152 tienen al menos un test automático planificado (dotnet, Unity o captura). Quedan 6 solo con equipo o revisión: los 4 que declara el Architect (AC-WKT-02-09, -02-10, -06-14, AC-APP-14-02) y 2 que nombran tests de dotnet pero están marcados solo "qa" (AC-WKT-23-01, AC-HIS-20-04). Tal como está, `run-tests.ps1 -Matrix` daría SIN TEST en esos casos (QA-G4aT-09). Los 11 LT conservan nombre y línea exactos.
- **Fidelidad**: comparé 26 casos con el legacy (§3). 23 coinciden al número o al carácter, entre ellos los 8 valores de Q-55 de `Distancia 100`, los 7 puntos de `Corta`, el cierre a los 12,3 s, Q-58 y los formatos. 2 no coinciden: el split 0 contra un objetivo 0 (QA-G4aT-08) y la barra superior de la vista "en curso" (QA-G4aT-17). Un caso borde rompe el "ninguna cuenta desborda" de R3-08 (QA-G4aT-11).
- **Lo que más pesa**:
  1. Las 04-design-notes llegaron **después** del diseño técnico. Cambian el contrato USS, la geometría del HUD, la llave en el inmersivo, el fondo, el diálogo de terminar y la receta de fuentes, y algunos de esos cambios caen en los **primeros** paquetes (WP-D0 y WP-A1). Es la situación de QA-G3-02 de la It. 3 (QA-G4aT-01).
  2. D-ART-10 todavía no está en la spec y WP-B0 depende de eso (QA-G4aT-02).
  3. El tope de 15 s del cierre corre en el tiempo virtual del alcance: al volver de un segundo plano largo se puede avisar un error de guardado que no ocurrió (QA-G4aT-03).
  4. Si la rutina termina sola con el diálogo de terminar abierto, el diálogo queda huérfano sobre Workout en reposo (QA-G4aT-04).
  5. El oráculo nuevo, que es un proyecto de Flutter, deja en el árbol del repo archivos con rutas absolutas de la PC, y el `.gitignore` no los cubre (QA-G4aT-05).

Qué hay que cerrar antes de cada paquete:

| Antes de | Issues |
|---|---|
| **WP-D0** (fuentes, tokens, contraste) | QA-G4aT-01 (DN4a-22: K2 sobre la derivada 700 y no sobre Barlow 700; DN4a-18: los dos tokens nuevos), QA-G4aT-15 (dueño de los conteos de tokens) |
| **WP-A1** (llave y programador) | QA-G4aT-01 (DN4a-14: `OverlaySurface` con el lugar de la llave y los rectángulos nuevos de `WrenchPlacementTests`), QA-G4aT-03 (timers de tiempo real en `FrameScheduler.CatchUp`), QA-G4aT-07 (avisos durante el alcance) |
| **WP-A2** (motor, guardado, dotnet) | QA-G4aT-01 (DN4a-13 en `HudGeometry` y las constantes de §5.13 en `MotionSpecs`), -03, -05 (antes de correr `run-oracle.ps1 -Engine`), -06, -09, -10, -11, -13, -18; el PO, QA-G4aT-08 y -16 |
| **WP-B0** (fuentes en Unity, llave, regeneración) | QA-G4aT-02 (D-ART-10 en la spec, con el ajuste del inmersivo) |
| **WP-B1** (reposo, inmersivo libre) | QA-G4aT-04, -14, -17 |
| **WP-B2** (HUD con rutina, horizontal, segundo plano) | QA-G4aT-02 (DV de las pastillas), QA-G4aT-07 (la vista después de un alcance) |
| **WP-B3** (arnés) | QA-G4aT-12 (CAP-29 y VID-02) |

---

## 1. Evidencia corrida

| Qué | Comando o fuente | Resultado |
|---|---|---|
| dotnet, línea base antes de WP-A | `powershell -NoProfile -File tools/dotnet/run-tests.ps1 -ArtifactsPath <scratch>/it4a/gate-tecnica` | `Build succeeded. 0 Warning(s) 0 Error(s)`; simulador **151/151**; EditMode **1457: 1440 pasan, 0 fallan, 17 omitidos**; exit 0. Es lo mismo que informa 00-deuda-it3 §12 (el árbol tiene la deuda sin commitear) |
| Cobertura de la matriz | script propio en `<scratch>/it4a/gate-tecnica/` que lee los `- **AC-…**` del spec y expande los rangos de 02-tech-design §8.4 | spec: 158 AC (146 nuevos + 12 de §5). Matriz: 158. Faltan 0, sobran 0. Sin test automático: AC-WKT-02-09, -02-10, -06-14, -23-01, AC-APP-14-02, AC-HIS-20-04 |
| Catálogo actual | `Assets/RowMate/Localization/strings.{es,en}.json` | 190 claves por idioma (217 entradas, 27 de ellas metadatos `@…`). Las 15 que el spec reusa están, con los textos del spec. Ninguna `hud*` todavía |
| `runInBackground` | `ProjectSettings/ProjectSettings.asset:89`, `Assets/RowMate/Editor/Bootstrap/PlayerSettingsStep.cs:88` | `1` y lo prueba `BootstrapperStaticTests.cs:141` |
| Pantalla encendida en el legacy | `<pub-cache>/wakelock_plus-1.4.0/lib/src/wakelock_plus_windows_plugin.dart:23`; `.../Wakelock.kt:24` | `SetThreadExecutionState(ES_CONTINUOUS \| ES_DISPLAY_REQUIRED)`; `FLAG_KEEP_SCREEN_ON` en la ventana de la actividad |
| Legacy intacto | `git -C ../rowmate-legacy status --porcelain` | vacío antes y después |

---

## 2. Cobertura: AC → componente → test

### 2.1 Matriz (agrupada por ficha)

**OK** = componente y test o captura planificados y coherentes. **OK · QA-G4aT-nn** = cubierto, con una observación. **OBS QA-G4aT-nn** = lo afecta un issue mayor. **Parcial (justificado)** = una parte queda para el equipo. Abreviaturas de tests: 02-tech-design §8.2.

| AC | Componente | Tests y capturas planificados | Gate |
|---|---|---|---|
| AC-WKT-01-01 a -08 | `WorkoutEngine` (fases), `ActiveClock` | WEL.*, `ACK.ResumeRestartsTheRhythm`, `SAD.ResumeRebasesTheMeters`; WPS, HCT (pantalla) | OK |
| AC-WKT-01-09, -10 | `WorkoutScreenView`, `WorkoutStore`, `PageStack` | `WSC.InProgressTextOutsideIdle`, `WST.SecondStartIsIgnored`, `WSC.TwoTapsOneSession` | OK · QA-G4aT-17 |
| AC-WKT-01-11 | `SessionRecorder`, `WorkoutStore`, `ToastLayer` | `WEL.CreateFailureStillRuns`, `WST.SaveErrorOnStartAndAtTheEnd`, `WPS.CreateFailureShowsTheToast`; CAP-29 | OK · QA-G4aT-12 |
| AC-WKT-02-01 a -04 | `ActiveClock`, `FrameScheduler` | ACK.*, `WEL.SecondOrderIsTheLegacyOrder`, `WEL.HudClockIsTheActiveClock`, `IHT.BandShowsTheActiveClock` | OK |
| AC-WKT-02-05 a -07 | `FrameScheduler.ResumeFromBackground`, `RealtimeTimerQueue.CatchUp`, `LifecycleHarness` | WBG.* | **OBS QA-G4aT-03** (con base en línea el tope de 15 s nunca se ve) |
| AC-WKT-02-08 | `HudView`, `TabularNumber.SetValue(…, animate)` | `WBU.FirstFrameShowsThePresent`; [Device-Win] | OK · QA-G4aT-07, -09 |
| AC-WKT-02-09 | `runInBackground` | [Device-Win] | Parcial (justificado) · QA-G4aT-09 |
| AC-WKT-02-10 | — | [Device-And]: NO VERIFICABLE | Parcial (justificado) · QA-G4aT-10 |
| AC-WKT-03-01 a -10 | `WorkoutEngine`, `SanitizedDistance`, `RoutineExpansion` | SAD.*, SDI.*, `WGD.LegacyDistanceColumn`, `WEL.LastStepFinishesAndSaves`, `WST.CompletedToast`, `HCT.CompletedRoutineClosesWithTheToast`, `IHT.NoGoalShowsQuestionMark`, `WSM.SimulatedDisconnectDuringARoutine`; CAP-28, CAP-30, DEMO-13 | OK (los 8 valores de AC-WKT-03-03, recalculados en §3) |
| AC-WKT-04-01 a -07 | `StepProgress` | SPG.*, `IHT.UpNextAndEndingSoonOnScreen`; CAP-21, CAP-22 | OK |
| AC-WKT-05-01 a -05 | `HeartRateFusion` | LT-35 a LT-38, `FusionInEveryPhaseFeedsHudAndPoints`, `TEL.PointsCarryTheFusedPulse` | OK |
| AC-WKT-05-06 | pulsómetro simulado + `HudView` | `WSM.SimulatedHrmShowsAndDropsTheBpmCard`; CAP-13 | OK |
| AC-WKT-06-01 a -04, -06, -08, -09 | `TelemetryPoint`, `SessionRecorder`, `SqliteSessionRepository` | TEL.*, `SSR.CreateWritesTheLegacyRow` | OK |
| AC-WKT-06-05, -10 | `SessionRecorder`, `SerialExecutor`, `synchronous = FULL` | `SSR.KilledAt31_5sHasSixPoints`, `SRC.PointIsWrittenAtOnce`, `SSR.CutSessionIsOpenWithItsPoints`, `WEL.ReopenDoesNotTouchACutSession`; [Device-Win]; herramienta de la demo | OK · QA-G4aT-13 |
| AC-WKT-06-07 | oráculo del motor legacy, `WorkoutGoldenTests` | WGD.*, `TEL.LastTwoPointsEqualWhenEndingOnAFive` | OK · QA-G4aT-05 |
| AC-WKT-06-11 | `WorkoutEngine.LastFinishedSessionId` | `WEL.LastFinishedOnlyWhenTheCloseSucceeds` | **OBS QA-G4aT-03** |
| AC-WKT-06-12, -13 | `SessionRecorder`, `WorkoutStore` | SRC.*, `WST.SaveErrorToast`, WPS.*; CAP-29 | OK · QA-G4aT-13 |
| AC-WKT-06-14 | — | revisión de QA | Parcial · QA-G4aT-09 (cabe una prueba estática) |
| AC-WKT-06-15 | `SessionRecorder.FlushAndWait`, `AppRoot.OnApplicationPause` | `WBG.BackgroundFlushesPendingWrites` | OK · QA-G4aT-06 |
| AC-WKT-09-01 a -07 | `StepTargetTracker` | LT-39 a LT-44, `SAD.TargetsFromTheEngine` | OK |
| AC-WKT-10-01 a -04, -06 | `TargetLight` | TLT.*, `WHS.EvaluatedOnEveryReadingAlsoInPause`; VID-02 | OK · QA-G4aT-08 |
| AC-WKT-10-05, -07 a -09 | `HudMetricCard`, `OffTargetMotion` | IHT.*, `WSM.DemoTrafficLight`; CAP-19, -20, -24, DEMO-09 a -11 | **OBS QA-G4aT-01** (DN4a-15) · QA-G4aT-08 (-07) |
| AC-WKT-11-01 a -16 | `WorkoutFormats` | WFT.*, `SeriesPanelsTests.Formats` (LT-45) | OK (12 ejemplos recalculados en §3) |
| AC-WKT-12-01 a -09 | `WorkoutScreenView`, `WorkoutRoutineListView`, `StartWorkoutDialog`, `WorkoutStore`, `WorkoutStartRule` | WSC.*, WST.*, WSR.*; CAP-01 a -11, DEMO-07 | OK · QA-G4aT-11 |
| AC-WKT-20-01 a -06 | `HudControls`, `FinishWorkoutDialog` | HCT.*; CAP-12, -14, -26; VID-03 | **OBS QA-G4aT-04** |
| AC-WKT-23-01 | — (descarte) | revisión de QA + `LegacyDiscardTests` | OK · QA-G4aT-09 (la marca) |
| AC-BLE-10-03 a -07 | `WorkoutStore`, `WorkoutScreenView`, motor | WSC.*, WST.*, `WSM.BannerFollowsTheRowerLive`, `WEL.EngineIgnoresTheConnection`; CAP-02, DEMO-14 | OK |
| AC-APP-14-01 a -04 | `ScreenKeepAwake`, `WindowsWakeLock`, `ScreenSleepWakeLock`, `AppRoot` | WLK.*, `SKA.RejectedRequestOnlyLogsOnce`; [Device-Win] | OK · QA-G4aT-09 (-02), QA-G4aT-16 (evidencia) |
| AC-WKT-13-01 a -05 | `ImmersiveWorkoutPage`, `PageStack.Push(…, Fade)` | IHT.*, `HCT.BackOpensTheConfirmation`, `HCT.EscapeWithoutDialogDoesNothing` | **OBS QA-G4aT-04** (-04) |
| AC-WKT-14-01 a -06 | `StepInfoBar`, `StepPulse` | IHT.*; CAP-18 a -23, VID-01 | **OBS QA-G4aT-02** (DV de las pastillas) |
| AC-WKT-16-01 | `FreeTopBar` | `IHT.FreeTopBar`; CAP-12 | OK |
| AC-WKT-17-01 a -10 | `HudGeometry`, `HudZones`, `HudMetricCard`, `HudBand` | HGT.*, IHT.*, HLT.*; CAP-12 a -17, -23, -24 | **OBS QA-G4aT-01** (DN4a-13) |
| AC-HIS-17-07, AC-HIS-19-01, -02 | `SqliteSessionRepository` | `SSR.SessionsAndPointsFillV8`, `SSR.GetByIdInLocalTime`, `SSR.PointsOrderedByMonitorTime` | OK |
| AC-SCN-01-01 | `HudBackdrop` | `IHT.ProvisionalBackground`; CAP-12 | **OBS QA-G4aT-01** (DN4a-18) |
| AC-APP-03-09 a -11 | `PageStack`, `DialogLayer`, `ToastLayer` | HCT.*, `WSC.StartDialogBehaves`, `WPS.ToastsInTheImmersive`; CAP-08, -09, -26, -28, -29; VID-03 | OK · QA-G4aT-12 |
| AC-APP-10-10 | catálogos | `CatalogParityTests` (202), `L10nKeysTests` | OK |
| §5: AC-BLE-16-01, -02 | `SimulatorLayerView`, `WrenchPlacement` | `WSM.RibbonOverTheImmersive`, WPL.*, `SimulatorOverlayTests` | **OBS QA-G4aT-01, -02** |
| §5: AC-APP-02-08, -06-01, -05-05, -05-07, -05-08 | `WorkoutScreenView`, `HudView` | WSC.*, IHT.*, `HCT.TabOrder` | OK |
| §5: AC-RUT-06-08, -10-11 | `WorkoutStartRule`, `RoutineCardView` | `WSR.EmptyExpansionCannotStart`, `WSC.MiniBarInWorkout` | OK |
| §5: AC-HIS-17-02, -20-04, AC-APP-01-09 | esquema v8, herramienta de la demo | DSC.*, `SSR.SchemaStillV8`, TDT.*, SLT.*, `WSC.RoutinesLoadedAtStartup` | OK · QA-G4aT-09, -18 |

### 2.2 Lo que el Architect declara sin test

| AC | Lo que dice el diseño | Gate |
|---|---|---|
| AC-WKT-02-09 | Solo [Device-Win] | **Justificado** (hace falta una ventana real). La parte de configuración (`runInBackground`) ya tiene una prueba estática que puede llevar la categoría |
| AC-WKT-02-10 | [Device-And], NO VERIFICABLE | **Justificado**. Necesita un `[Ignore]` con la categoría para que la matriz diga IGNORADO y no SIN TEST. Ver además RA-19 (QA-G4aT-10) |
| AC-APP-14-02 | [Device-Win]; [Device-And] NO VERIFICABLE | **Parcial**: la regla ("se vuelve a pedir al volver de la pausa y al recuperar el foco") es pura y se prueba en dotnet con `ScreenKeepAwake` y un ciclo de vida falso |
| AC-WKT-06-14 | Solo revisión de QA | **Mejorable**: una prueba estática (el repositorio escribe solo las columnas de `data_points` y `workout_sessions`; ningún parcial, repetición ni resultado de objetivos en Infrastructure) |
| Con parte de equipo (§8.6) | AC-APP-14-01 y -03, AC-WKT-02-08, AC-WKT-06-05 y -10, AC-WKT-13-03, AC-APP-05-07 | **Justificados**, con una salvedad: en Windows no hay alcance (el *loop* sigue minimizado), así que la parte de equipo de AC-WKT-02-08 es en realidad [Device-And] |

---

## 3. Fidelidad: casos del legacy recalculados

| # | Caso | Legacy | Spec y diseño | Resultado |
|---|---|---|---|---|
| F1 | Fusión del pulso: el sensor manda; un estado distinto de conectado con bpm previo pone 0; sin bpm previo no avisa (LT-35 a LT-38) | `workout_provider.dart:222`, `:227-229`, `:233-236`; `test/workout_provider_hr_test.dart:80-180` | AC-WKT-05-01 a -05; `HeartRateFusion` (§2.7). `Bpm` y `Data` son `IEventSource` (cada lectura, aunque repita): el conteo de LT-35 (2) y de LT-38 (0) es posible | Coincide |
| F2 | Objetivos: parado = SPM ≤ 0 **y** W ≤ 0; split solo si > 0; sin lecturas → no cumplido; split sin dato no se evalúa | `step_target_tracker.dart:31`, `:36`, `:46-48`, `:57-59` | AC-WKT-09-01 a -07; `StepTargetTracker` | Coincide |
| F3 | Orden del segundo: +1 a los tres contadores, punto si `% 5`, objetivos, fin de paso y aviso | `workout_provider.dart:154-189` | AC-WKT-02-02; §2.3 | Coincide, salvo el punto con el alta en vuelo (declarado, QA-G4aT-16) |
| F4 | "Por terminar" de 1 a 5 y "a continuación" de 0 a 15 con paso siguiente | `workout_provider.dart:39-41`; `immersive_workout_screen.dart:343-344` | AC-WKT-04-04, -05; `StepProgress` | Coincide |
| F5 | Restante `clamp(0, d)`, −1 sin tiempo; progreso acotado | `workout_provider.dart:31-36`, `:101-104` | AC-WKT-04-02, -03; DT4a-12 | Coincide. Con d < 0 el `clamp` de Dart lanza: DT4a-12 tiene sentido |
| F6 | Fin de paso: tiempo ≥ d (manda sobre la distancia); distancia cruda; un paso de 0 s termina en el primer segundo | `workout_provider.dart:196-201` | AC-WKT-03-02, -03, -07 | Coincide (Q-55 cambia la distancia a propósito) |
| F7 | `Distancia 100` con `R-4`, `R-4R`, `R-4Z` y la pausa de 20 s | `series_tracker.dart:106-108`, `:118-127`; `workout_provider.dart:198-201` | AC-WKT-03-03; `SanitizedDistance` | Recalculado a mano: nuevo 25, 26, 27, 25; legacy 25, 285, 25, 11. Los 8 coinciden con la spec y con S4-a |
| F8 | `Corta` con `R-4`: 7 puntos con índices 0, 0, 1, 2, 2, 3, 3 y el último repetido | `workout_provider.dart:158-187`, `:259-273` | AC-WKT-06-03, -07 | Recalculado: coincide |
| F9 | Libre terminado a los 12,3 s: 3 puntos (305, 310, 312); 1048 m, 312 s, 41 kcal | `workout_provider.dart:259-294`; `workout_session.dart:118-131` | AC-WKT-06-06 | Recalculado (`L(12)` llega a los 11,5 s): coincide |
| F10 | Q-58: pausa a los 10,7 s y 30 s de pausa → el segundo 11 a los 41,7 | `workout_provider.dart:239-250` (un `Timer.periodic` nuevo al reanudar) | AC-WKT-01-05; `ActiveClock` | Coincide |
| F11 | El alta va antes del reloj; si el alta falla, el reloj no arranca | `workout_provider.dart:145-148` | AC-WKT-01-02, -11; DT4a-02; DV-29 | Coincide con el cambio declarado |
| F12 | Cierre: punto final (tipo vacío si el índice queda fuera de rango), estadísticas sobre todo, puntos, actualización y "última terminada" | `workout_provider.dart:256-296` | AC-WKT-06-06, -11; §2.9, §3.3 | Coincide. DT4a-07 (una transacción) no cambia una sesión terminada |
| F13 | La actualización reescribe la fila con `toMap` (también `id`, y `strava_activity_id` en null) | `database_service.dart:221-229`; `workout_session.dart:39-51` | §3.3; DT4a-06 | Coincide |
| F14 | Puntos ordenados por tiempo del monitor, ascendente | `database_service.dart:263-270` | AC-HIS-19-02; DT4a-05 (+ `id`) | Coincide |
| F15 | Volver a reposo no borra las lecturas ni "la última terminada" | `workout_provider.dart:305-318` | AC-WKT-01-07 | Coincide |
| F16 | Semáforo: split con `val > target`; SPM y vatios con `val < target`; los vatios máx. no cuentan | `immersive_workout_screen.dart:579-602` | AC-WKT-10-01 a -04, -07 | **No coincide en un caso**: con objetivo 0 y split 0 (`--:--`) el legacy da blanco (`0 > 0` es falso); la spec dice "≤ 0: siempre fuera, también con `--:--`" (QA-G4aT-08) |
| F17 | Split en vivo `MM:SS` y `--:--`; reloj `H:MM:SS`; cuenta `M:SS`; `{m}m` y `nullm` | `rowing_data.dart:24-29`; `immersive_workout_screen.dart:456-458`, `:569-577` | AC-WKT-11-01, -06 a -08; DV-25 | Recalculado: 6000 → `100:00`, 36000 → `10:00:00`, 359 999 → `5999:59`. Coincide |
| F18 | `formatLapTime(59,96)` = `1:00.0`; `(55)` = `0:55.0`; `formatSplit(133,5)` = `2:14`; `formatMeters(1 000 000)` = `1.000.000 m` | `series_panels.dart:9-24` | AC-WKT-11-09 a -11 | Coincide |
| F19 | Resumen sobre la plantilla (no la expandida); sin trabajo, `N pasos` | `routine.dart:100-105` | AC-WKT-11-14 | Coincide |
| F20 | Expansión: bloques consecutivos, `?? 1`, ×0 vacío; la progresión de vatios mueve W mín. y W máx. | `routine.dart:29-70` | AC-WKT-03-01; `RoutineExpansion`; R3-08 | Coincide; con un split guardado ≤ 0 muy negativo, el C# desborda (QA-G4aT-11) |
| F21 | kcal y BPM a `bottom: 140` sin margen seguro; banda a 140 o a `padding.bottom + 16`; SPM y columna a `padding.top + 90` | `immersive_workout_screen.dart:612`, `:640`, `:672`, `:710` | AC-WKT-17-02 a -05; §6.4 | Coincide (× `s` por spec §0.3) |
| F22 | `Terminar` siempre habilitado; `Pausa` también en `finished` (llama `pause()` durante el guardado) | `immersive_workout_screen.dart:908`, `:923-948` | AC-WKT-20-06 los apaga | Diferencia a propósito y correcta |
| F23 | El diálogo de completado sale con `phase == finished`, también al terminar a mano si llega una lectura mientras se guarda | `immersive_workout_screen.dart:49-55`, `:141-147`; `workout_provider.dart:253` | Q-53; AC-WKT-20-04 | La 4a lo corrige. Vale para la 4b: el diálogo de completado tiene que mirar cómo terminó, no la fase |
| F24 | La vista "en curso" es un `Scaffold` sin barra superior | `workout_screen.dart:180-191` | AC-WKT-01-09; 04-design-notes §4.1 | **No coincide**: el diseño deja `#workout-appbar` (QA-G4aT-17) |
| F25 | Pantalla encendida antes de `runApp`; Windows solo `DISPLAY`; Android, la bandera de la ventana | `main.dart:42`; `wakelock_plus_windows_plugin.dart:23`; `Wakelock.kt:24` | AC-APP-14-01 a -03; §4.4; DT4a-09 | Coincide (+ `ES_SYSTEM_REQUIRED`, aceptado) |
| F26 | Dos toques rápidos crean dos sesiones (`startFree()` sin esperar y `push`) | `workout_screen.dart:86-89`, `:163-165` | DV-30; AC-WKT-01-10 | La corrección está bien diseñada |

---

## 4. Guardado incremental y durable (esquema v8)

- **Transacciones.** El alta, cada lote y el cierre (punto final + `UPDATE`) son transacciones separadas, en un `SerialExecutor` FIFO con WAL y `synchronous = FULL`. Lo comprobé en el código: los pragmas se aplican siempre en cada conexión de escritura (`Assets/RowMate/Infrastructure/Database/DatabaseSchema.cs`), y el ejecutor completa con `RunContinuationsAsynchronously` (`SerialExecutor.cs:62`). Un `UPDATE` de 0 filas que lanza y hace *rollback* evita una sesión con punto final y sin fin (DT4a-07). Bien.
- **Qué queda tras un cierre forzado** (§3.4). La tabla es correcta para la base al día: cada punto sale en el cuadro en que se toma. Con la base lenta, los puntos que esperan un lote en vuelo están solo en memoria. Es lo esperado: AC-WKT-06-13 no pide el segundo de AC-WKT-06-05.
- **Consistencia para el Historial.** El Historial del legacy no usa los totales de la fila: los calcula desde `data_points` (`getStatsForSessions`, `database_service.dart:289`; `history_provider.dart:31`). Una sesión cortada, con totales en 0 y sus puntos, se va a ver bien en la It. 9. Pero el diseño agrega un estado que el legacy nunca produce: un **lote fallado se descarta** y el cierre sale igual con estadísticas sobre **todos** los puntos en memoria. Queda una sesión cerrada con huecos y con totales que no salen de sus puntos (QA-G4aT-13). En el legacy los puntos y la actualización van en el mismo `try` (`workout_provider.dart:257-299`): o todo o nada.
- **Segundo plano** (AC-WKT-06-15). `FlushAndWait` bloquea el hilo principal, pero no depende de él: la tarea del ejecutor se completa en su hilo, así que no hay *deadlock*. Tiene dos límites que el spec no admite: el tope de 1 s ("todo lo pendiente", dice el AC) y los puntos tomados con el alta en vuelo, que no tienen id y no se pueden mandar (QA-G4aT-06).
- **Al cerrar la app** (P-03). El orden de `OnDestroy` (app → *backend* → `LocalStore`) manda lo pendiente antes de que `RowMateDatabase.Dispose` deje de aceptar trabajo: ese `Dispose` pone `_disposed` antes de encolar el cierre (`RowMateDatabase.cs`), así que `Abandon()` tiene que ir antes, como está.
- **Esquema.** Las sentencias de §3.3 coinciden con el DDL v8 (`origin` con `DEFAULT 'app'`, y el índice `(session_id, elapsed_seconds)` sirve a las dos lecturas). `started_at` en ISO UTC de ancho fijo ordena bien como texto.

## 5. Hilos y reloj

- **Hilo principal.** Motor, grabador, *stores* y vistas corren en el hilo principal. La base corre en su hilo. Los eventos del remo y del pulsómetro llegan al hilo principal (`IRowerService`, `IHeartRateService`). Bien.
- **Sin deriva.** `ActiveClock` cuenta segundos enteros desde el inicio del tramo, en *ticks*. Rearmar un timer de una vez al próximo vencimiento no acumula atraso. Un cuadro tarde procesa todos los segundos vencidos (AC-WKT-02-04). Bien.
- **El alcance y los timers de tiempo real.** `CatchUp` dispara **todo** lo que vence antes de `to`, también lo que se agenda durante el alcance (§4.1). Eso es lo correcto para el motor y el remo, pero no para un **tope de espera**. El tope de 15 s del cierre es un `scheduler.Delay` (§2.9) y `RealtimeTimerQueue.Delay` completa su tarea dentro de la corrida (`RealtimeTimerQueue.cs:89-94`). Si la rutina terminó en segundo plano más de 15 s antes de volver, el tope vence en tiempo virtual antes de que pueda volver el cierre real, cuya continuación llega al hilo principal recién después del alcance (QA-G4aT-03). Con `FakeScheduler` y base en línea el cierre vuelve en el acto, así que ninguna prueba planificada lo ve.
- **Semántica de `IScheduler`.** Hoy `UnityScheduler.MonotonicSeconds` es "vivo" (lee el `Stopwatch` al llamarlo; `UnityScheduler.cs:35`). `FrameScheduler` lo pasa a "del cuadro" (lo fija `Tick`). Con `MotionDriver`, `FramePacer` y la barra de título es incluso mejor, pero conviene decirlo en §4.1. Ninguna medición de rendimiento debería usar el programador: tienen que usar el `Stopwatch` (QA-G3-21).
- **`OnApplicationPause(false)` al arrancar.** Unity llama una vez a `OnApplicationPause(false)` cuando arranca, después de `Awake`. `ResumeFromBackground` sin un `EnterBackground` previo tiene que ser un no-op. Es un detalle para WP-B1.

## 6. Segundo plano (Q-91) y pantalla encendida, por plataforma

| Plataforma | Diseño | Gate |
|---|---|---|
| Windows | `runInBackground` (ya en `ProjectSettings`); minimizada no es segundo plano; `SetThreadExecutionState` desde el hilo principal, que vive todo el proceso; liberada en `OnDestroy` y en `DomainUnload` (R4a-07) | OK. La liberación desde `DomainUnload` sirve solo si corre en el mismo hilo que hizo el pedido (el principal del Editor): WLK lo tiene que comprobar, no suponer |
| Android | pausa con `FlushAndWait(1 s)` y alcance al volver; `Screen.sleepTimeout = NeverSleep` (`FLAG_KEEP_SCREEN_ON`, como el legacy) | **OBS**: el reloj monótono del `Stopwatch` no cuenta el sueño profundo con la pantalla bloqueada. ADR 017 lo deja como RA-19, pero §4 no lo nombra (QA-G4aT-10). Todo NO VERIFICABLE sin teléfono |
| iOS y macOS | 10b; `NoWakeLock` en macOS solo anota | OK |
| Evidencia | `powercfg /requests` (AC-APP-14-01) | Imprime la ruta completa del `.exe`, con la carpeta del usuario: en lo versionado va solo `RowMate.exe` (QA-G4aT-16) |

DT4a-10 (el alcance solo al volver de una pausa de la app) deja abierto algo para la 10a. Con el remo real, un cuadro que llega tarde en primer plano procesa varios segundos con la última lectura, y la lectura siguiente trae un salto de más de 10 m en un segundo, que el saneamiento descarta: se pierden metros del paso por distancia y de los parciales. Con el remo simulado no pasa, porque emite un incremento fijo por disparo (`Simulator/RowingSimulator.cs`). Lo registro como condición de la decisión (§13).

## 7. R3-08

- `WorkoutStartRule.CanStart` = `StepLimitCheck.TemplateIsWithinLimits` y `RoutineStructure.Segments(…, 0).Total > 0`. Las dos existen y son de forma cerrada: `FirstRepetitionOutOfRange` no recorre repeticiones, y `Segments` con `max = 0` no reserva listas. `Serie enorme` sale por `repeatCount > 999` y `Vatios imposibles` por el tope. `Serie ×0` y las repeticiones negativas salen por `Total = 0`. El costo y la memoria de AC-WKT-12-08 son alcanzables (`GC.GetAllocatedBytesForCurrentThread` ya se usa en pruebas que corren en dotnet y en Unity).
- **El hueco.** `StepLimitCheck` trata un split guardado ≤ 0 como "sin split" y no lo acota (`StepLimitCheck.cs:111-114`). En cambio, `RoutineExpansion.Apply` lo progresa con `checked` (`RoutineExpansion.cs:115`). Un split guardado de −2 147 483 000 con progresión −5 999 y ×2 pasa la regla y desborda en `StartWithRoutine` (QA-G4aT-11). El legacy, con enteros de 64 bits, arranca. Solo se da con una base tocada a mano, pero es justo el caso de H-10.

## 8. UI Toolkit: lecciones de las It. 2 y 3

| Lección | En el diseño de la 4a | Gate |
|---|---|---|
| `rootVisualElement` nulo en `Awake` (QA-G2-01) | `LocalStore`, ciclo de vida y pantalla encendida en `Awake`, sin tocar la UI | OK |
| *Hit-test* y lo que sale no responde (QA-G2-07, QA-G3-01) | fondo, cinta, barra de pasos y tarjetas sin eventos; `ExitLock`; sonda S-B4a para la escala por zona | OK. Faltan dos cosas: el contorno `#hud-boat-zone` tiene que ser `Ignore`, y quién cierra el diálogo de terminar cuando la página sale sola (QA-G4aT-04) |
| Unidades de USS | el tamaño en línea por código; `letter-spacing` en 1/100 em | **Observación**: USS no tiene `em` ni `align-items: baseline`. La flecha (0,4 em), el hueco (0,48 em), las líneas de base y `obj.` alineado con las cifras se calculan en px por código (QA-G4aT-14) |
| Transiciones de USS congeladas en *batchmode* | todo el movimiento por código; `Motion.uss` solo bajo `:hover` | OK |
| Lo que crece desde alto 0 se mide en el cuadro siguiente (`ILayoutBoundMotion`); Yoga aplasta el texto con `flex-shrink` 1 | `obj.` 0 → 16,9, `Próximo` 0 → N, el hueco de la flecha, kcal por FLIP | **Observación**: hace falta el patrón de la It. 3 (un *slot* que recorta, el contenido en absoluto o en `nowrap`, y la medida en el cuadro siguiente). §9.5 no lo dice (QA-G4aT-14) |
| Doble toque (Q-19, DV-30) | el inmersivo se abre por estado; `StartFree` y `StartRoutine` ignoran fuera de reposo; el velo y el `Close` idempotente | OK |
| `ScrollView` conserva el desplazamiento con `display: none` | la vista de reposo en `none` durante el inmersivo | OK (AC-WKT-12-09) |
| Escala por zona (`scale` + `transform-origin`) | R4a-02 y la sonda S-B4a antes de B2 | OK. `FitBox` mide en coordenadas locales y ajusta a la grilla del panel, que también ignora las transformaciones: es coherente |

## 9. Rendimiento del HUD

- **Entre lecturas**: el diseño apunta a 0 B por cuadro, y está bien planteado. `TabularNumber` es un solo elemento que dibuja con `DrawText`, y los pulsos cambian estilos de tipo valor. `HudBudgetTests` mide con los pulsos corriendo, que es el caso que repinta `TabularNumber` en cada cuadro (la cuenta que se tiñe).
- **Por lectura**: unas pocas cadenas y dos *records* a unos 3 Hz (remo, pulsómetro y segundo del motor: el remo simulado y el pulsómetro emiten a 1 Hz). Es aceptable.
- **Elementos**: 58 en el diseño técnico, más 2 de las notas de diseño (el cielo y el fondo de `Próximo`), da **60 de 60**, sin margen. Más el *spinner* de DN4a-20 cuando se guarda. El Designer dejó la salida (una textura de 1 × 100). La fila de píldoras de la 4b no va a entrar en el presupuesto así como está.
- **Durante el alcance**: el motor avisa por cada lectura y cada segundo, y el *store* del HUD rehace el estado con textos en cada aviso. Con una sesión libre olvidada 8 h en segundo plano son ~86 000 llamadas y otros tantos estados en el primer cuadro de vuelta: segundos de hilo principal y decenas de MB de basura, con riesgo de ANR en Android (QA-G4aT-07).

## 10. Paquetes disjuntos y contrato USS

- **Dueños.** Los archivos de §9 son disjuntos, con dos huecos. (a) WP-D0 suma dos *tokens* (133 → 135, DN4a-18), pero los conteos y `TokensUssSyncTests` están en `Tests/EditMode/**`, que es de WP-A. (b) El `.gitignore` que necesita el oráculo nuevo no tiene dueño (QA-G4aT-05 y -15).
- **Orden.** WP-D0 y WP-A1 son los primeros. DN4a-22 (la tabla K2 sobre la derivada 700) cambia la receta de WP-D0. DN4a-14 (el lugar de la llave en el inmersivo) cambia `OverlaySurface` y `WrenchPlacement` de WP-A1 y reemplaza los rectángulos de 00-deuda-diseno §1.5 que pide `WrenchPlacementTests`. Si no se decide antes, se reescribe (QA-G4aT-01).
- **Contrato USS.** Las notas de diseño marcan **[contrato]** en DN4a-13 a -16, -18 y -19. Cambian filas de §9.5: el fondo, las unidades, las pastillas, `Próximo`, el hueco de la flecha, el *chevron*, la banda compacta con un `FitBox` común y el diálogo opaco. §9.5 sigue con lo anterior (QA-G4aT-01).

## 11. Contradicciones spec ↔ diseño técnico ↔ notas de diseño

| Tema | 01-po-spec | 02-tech-design | 04-design-notes | Issue |
|---|---|---|---|---|
| kcal y BPM en horizontal | "donde lo ubique el Designer" (AC-WKT-17-05) | en fila, 8·s debajo de Watts (`:984`) | en fila a la altura de los controles, kcal en el rincón (`:166`) | QA-G4aT-01 |
| Centro de la zona del bote | lo fija el Designer (AC-WKT-17-09) | 0,5 del alto (`:987`) | 0,504 en vertical y 0,652 en horizontal (`:21`, §3.6) | QA-G4aT-01 |
| La llave en el inmersivo | "lo fija el Designer"; AC-BLE-16-03 "no cambia" (`:411`) | R2 a R4 y `Clear = false` en L640 (`:1090-1098`) | sobre kcal y BPM en vertical, junto a `Pausa` en horizontal, con lugar en L640 (§3.9) | QA-G4aT-01, -02 |
| Unidades `W` y `m` | — | color base al 55 % (`:1512`) | `.hud` al 80 % (`:109-110`) | QA-G4aT-01 (y DV-31, PO) |
| Pastilla y `Próximo` | color del tipo, `Próximo:` al 60 %, fondo al 25 %, laten enteras (AC-WKT-14-03, -04) | lo mismo (`:1496`, `:1515`) | texto blanco, 70 % sobre 20 %, late solo el fondo (`:113-115`, §11) | QA-G4aT-02 |
| Fondo provisorio | lo fija el Designer (AC-SCN-01-01) | `--rm-bg-app` parejo (`:1489`) | cielo y agua, dos *tokens* nuevos (`:415`) | QA-G4aT-01 |
| Diálogo de terminar | "aspecto de ART_BIBLE §3.5" (AC-WKT-20-03) | `.rm-dialog--hud` al 92 % (`:1048`) | el diálogo opaco de la app (`:473`) | QA-G4aT-01 |
| Banda | — | un `FitBox` por valor (§6.7) | un `FitBox` común (`:265`) | QA-G4aT-01 |
| K2 | — | Inter y **Barlow 700** (`:1132`, `:1454`) | la derivada 700 en lugar de Barlow 700 (`:894`) | QA-G4aT-01 |
| CAP-22 | "a 3 s del final" | 87 s (`:1189`) | 87,6 s (`:582`) | QA-G4aT-01 |
| CAP-29 | "base que falla al crear la sesión, libre en marcha" | a los 5 s, con aviso entero (`:1196`) | el aviso entre la banda y los controles | QA-G4aT-12 (a los 5 s ya no hay aviso: dura 4 s) |
| VID-02 | "el empujón una sola vez" | un solo cruce de cumple a fuera (`:1200`) | "el segundo cruce, antes de 4 s, sin empujón" (`:814`) | QA-G4aT-12 |
| Punto con el alta en vuelo | "si la cuenta es múltiplo de 5 y la sesión existe" (AC-WKT-02-02) | también con el alta en vuelo (`:235`) | — | QA-G4aT-16 |
| Split objetivo 0 | "≤ 0: siempre fuera, también con `--:--`" (`:275`, `:278`) | "no con uno ≤ 0" (`:310`) | — | QA-G4aT-08 (el legacy da cumple con 0 y 0) |
| Vista "en curso" | centrado, `text.tertiary` (AC-WKT-01-09) | `#workout-appbar` siempre (`:898`) | "centrado entre las dos barras" (`:369`, `:387`) | QA-G4aT-17 (el legacy no tiene barra) |

**Pedidos de las notas de diseño al Architect (DN4a-13 a DN4a-22).** Todos son viables en UI Toolkit y en Core, con estas condiciones:
- **DN4a-13**: `HudGeometry` pasa a recibir los anchos naturales que mide cada `FitBox`. La geometría queda acoplada al *layout*: hay que medir en `GeometryChangedEvent`, aplicar en el cuadro siguiente y recalcular solo con un cambio de ventana o un ancho que cruce un umbral, para no entrar en un bucle. Las decisiones "por ventana" (T4) evitan que oscile.
- **DN4a-14**: tiene que estar decidido antes de WP-A1.
- **DN4a-16**: espera al PO.
- **DN4a-17 y DN4a-19**: necesitan el patrón de *slot* de §8.
- **DN4a-20** suma un elemento.
- **DN4a-22**: tiene que estar decidido antes de WP-D0.

## 12. Regla de repo público

- Los cinco documentos de la iteración la cumplen: busqué rutas absolutas, carpetas de usuario, nombres y datos del equipo, y no hay. Las rutas son relativas, el legacy va por URL y aparecen `<scratch>`, `<pub-cache>` y `<flutter-sdk>`. `LocalLow/iguisoft/RowMate` es un fragmento relativo, sin usuario.
- **Riesgo nuevo** (QA-G4aT-05): `tools/oracle/engine/` es un proyecto de Flutter con `pub get`. Hasta ahora el oráculo evitaba a propósito `pubspec.yaml` y `pub get` (`tools/oracle/README.md`). `flutter pub get` y `flutter test` dejan `.dart_tool/package_config.json` y `.flutter-plugins-dependencies` con rutas `file:///` absolutas de la caché de `pub` en el perfil del usuario, además del `pubspec_overrides.yaml` de `-LegacyPath`. El `.gitignore` solo cubre `/tools/oracle/.dart_tool/` y `/tools/oracle/pubspec.lock`, con la ruta anclada (`.gitignore:41-43`).
- La herramienta de la demo promete "ninguna ruta en los errores". Los mensajes de `IOException` y `UnauthorizedAccessException` de .NET la traen (QA-G4aT-18).
- La evidencia de `powercfg /requests` trae la ruta del `.exe` (QA-G4aT-16).

## 13. Decisiones que le tocan a QA

| ID | Decisión |
|---|---|
| DT4a-02 | **Aceptado**. "A más tardar 100 ms" (AC-WKT-01-02) y "la base lenta no frena el HUD" (AC-WKT-06-13) piden lo mismo. Condición: `WEL.ClockNeverWaitsMoreThan100Ms` con un alta de 2 s, y el texto de AC-WKT-02-02 alineado (QA-G4aT-16) |
| DT4a-07 | **Aceptado**. Una sesión terminada queda idéntica a la del legacy, y un corte entre el punto final y el cierre ya no deja estados intermedios. La prueba con un *trigger* que falla en el `UPDATE` está planificada |
| DT4a-10 | **Aceptado con dos condiciones**: (1) los topes de espera no corren en tiempo virtual (QA-G4aT-03); (2) la 10a prueba un cuadro tarde con el remo real y la pérdida de metros que hace el saneamiento (§6) |
| DT4a-14 | **Aceptado**. Leer una copia es la única forma de no tocar ni el `-wal` ni la fecha. Condiciones: errores sin ruta y la prueba del camino de error (QA-G4aT-18) |

Para el PO: QA-G4aT-02, -08 y -16, y las decisiones de DT4a-01, -04, -11 y -12 que pide el Architect. Para el Designer: QA-G4aT-17 y VID-02 (QA-G4aT-12).

---

## 14. Issues

| ID | Severidad | Responsable | Issue | Evidencia | Corrección sugerida |
|---|---|---|---|---|---|
| QA-G4aT-01 | mayor | architect | 02-tech-design no incorpora lo que resolvieron y pidieron las 04-design-notes, que llegaron después: las respuestas a DN4a-01, -02, -03, -07, -11 y -12 y los pedidos DN4a-13 a DN4a-22. Cambian el contrato USS (§9.5), la geometría y la zona del bote (§6.4), la llave en el inmersivo (§6.10 y los rectángulos de `WrenchPlacementTests`), el fondo y sus *tokens*, el diálogo de terminar, la banda, las constantes de `MotionSpecs`, la receta de K2 de WP-D0 y una toma. Si WP-D0, WP-A1 o WP-B implementan el diseño de hoy, se rehacen, y las comprobaciones del arnés chocan con el Designer. El presupuesto del HUD queda en 60 de 60 | `02-tech-design.md:984`, `:987`, `:1090-1098`, `:1259`, `:1489`, `:1512`, `:1515`, `:1048`, `:1132`, `:1454`, `:1189`, `:1126`; `04-design-notes.md:21`, `:109-115`, `:166`, `:265`, `:336-351`, `:415`, `:473`, `:582`, `:745`, `:885-894` | Un anexo "Cambios por 04-design-notes de la It. 4a" que acepte o rechace cada DN4a y actualice §6.4, §6.10, §6.11, §6.14, §7.3, §8.2 y §9.5. DN4a-22 antes de WP-D0, DN4a-14 antes de WP-A1, DN4a-13 y §5.13 antes de WP-A2, y el resto antes de B1 y B2. Recontar `HudBudgetTests` con los dos elementos nuevos y el *spinner* |
| QA-G4aT-02 | mayor | po | D-ART-10 no está en la spec. AC-BLE-16-02 todavía dice "como en Rutinas con DV-13" y "AC-BLE-16-03 no cambia", pero el diseño cambia los dos: el carril izquierdo fuera del entrenamiento y el panel anclado en la llave. También cambian AC-RUT-10-04 y -09 (It. 3, cerrados) y las tomas de la llave de las It. 2 y 3. Encima, las notas de diseño ajustan D-ART-10 para el inmersivo (§3.9). WP-B0 no puede empezar. Además, la DV nueva de las pastillas cambia AC-WKT-14-03 y -04, y hay que precisar DV-31 (unidades) y DV-33 | `01-po-spec.md:411`; `02-tech-design.md:1542` (DT4a-11); `04-design-notes.md:902-905`; `00-deuda-diseno.md:416` | Integrar D-ART-10 con el texto de 04-design-notes §11 en AC-BLE-16-02 y -03 (§5) y reabrir AC-RUT-10-04 y -09 en §5, todo **antes de WP-B0**. Registrar la DV que reemplaza a DV-13. Decidir la DV de las pastillas (o rechazarla) y precisar DV-31 y DV-33 **antes de B2** |
| QA-G4aT-03 | mayor | architect | El tope de 15 s del cierre es un `scheduler.Delay`, y `CatchUp` dispara en tiempo virtual todo lo que vence antes de `to`, también lo que se agenda durante el alcance. Escenario en Android: `Prueba 4a`, pantalla bloqueada a los 100 s y de vuelta a los 400 s. En el alcance la rutina termina en el segundo virtual 240 y el cierre se encola, pero su continuación vuelve al hilo principal recién después del alcance. El tope vence en el 255 virtual, dentro del alcance. El motor vuelve a reposo con `saved = false`: aparece `workoutSaveError` (o ni eso, si su tiempo de 4 s también vence en el alcance) y `LastFinishedSessionId` no cambia, aunque la sesión queda bien cerrada en la base. En la It. 12 eso deja sin subir la sesión. Las pruebas no lo ven: `FakeScheduler` con la base en línea cierra en el acto, y AC-WKT-02-06 vuelve 10 s después del fin | `02-tech-design.md:349`, `:353`, `:402`, `:743`; `Assets/RowMate/Core/Common/RealtimeTimerQueue.cs:89-94` (el `Delay` se completa dentro de la corrida) | Que los topes de espera corran en tiempo real: que `CatchUp` no los dispare (se reanclan en `to`) o que el tope se arme o rearme al terminar el alcance. Prueba en WBG con `LifecycleHarness`, `SerialExecutor` y `TestMainThreadContext`, con la vuelta 300 s después del fin: `Ended(Completed, true)`, `LastFinishedSessionId` puesto y ningún `workoutSaveError`. Decir qué muestra el primer cuadro de vuelta mientras el cierre todavía no volvió |
| QA-G4aT-04 | mayor | architect | El diálogo de terminar queda huérfano. `DialogLayer` está "arriba de las páginas" y no depende de `PageStack`. Si la rutina termina sola, o vuelve el cierre, con `FinishWorkoutDialog` abierto, el inmersivo sale y el diálogo queda sobre Workout en reposo, junto con el aviso `¡Entrenamiento completado!`. Su `Terminar` llama `FinishAsync` en reposo y no hace nada. Pasa, por ejemplo, si se abre el diálogo en los últimos segundos del enfriamiento. Lo mismo con el panel del simulador anclado en la llave del inmersivo. El spec no lo define: AC-WKT-20-03 solo dice que la sesión sigue en su fase mientras el diálogo está abierto | `01-po-spec.md:319`, `:352`; `02-tech-design.md:946`, `:1048`; `Assets/RowMate/Presentation/Shell/DialogLayer.cs:8` | `ImmersiveWorkoutPage` cancela sus diálogos cuando la fase deja Active o Paused, o al empezar su salida, y cierra o reancla el panel. Prueba `HCT.RoutineEndsWithTheFinishDialogOpen`. El PO suma el caso a AC-WKT-20-03 y -13-04 |
| QA-G4aT-05 | mayor | architect | Regla de repo público. `tools/oracle/engine/` es un proyecto de Flutter: `flutter pub get` y `flutter test` dejan `.dart_tool/package_config.json` y `.flutter-plugins-dependencies`, con rutas absolutas de la caché de `pub` en el perfil del usuario, más `pubspec.lock`, `build/` y el `pubspec_overrides.yaml` de `-LegacyPath`, que también es absoluto. El `.gitignore` solo cubre `/tools/oracle/.dart_tool/` y `/tools/oracle/pubspec.lock`, con la ruta anclada, y §1.1 no lista ningún cambio al `.gitignore`. El README del oráculo todavía dice "no hay `pubspec.yaml` ni `pub get`" | `.gitignore:41-43`; `02-tech-design.md:160`, `:699`; `tools/oracle/README.md` §"El legacy no se toca", punto 3 | En WP-A: sumar al `.gitignore` `/tools/oracle/engine/.dart_tool/`, `build/`, `.flutter-plugins*`, `pubspec_overrides.yaml` y `pubspec.lock`, antes de la primera corrida de `run-oracle.ps1 -Engine`. Una prueba estática que falle si un archivo versionado de `tools/` contiene `file:///` o una unidad de disco. Actualizar el README |
| QA-G4aT-06 | menor | architect | `FlushAndWait` (AC-WKT-06-15) tiene dos límites que el spec no admite. (a) Los puntos tomados con el alta en vuelo no tienen id. Como el hilo principal está bloqueado, la continuación que los mandaría no corre: quedan en memoria. (b) El tope de 1 s contra "todo lo pendiente" | `02-tech-design.md:585`, `:600`; `01-po-spec.md:256` | Documentar los dos límites, y que el PO los acepte en el texto (QA-G4aT-16), o encolar el lote detrás del alta en el hilo de la base (un trabajo que lee el id que dejó el anterior). Prueba en SRC con un alta lenta y una pausa a los 6 s |
| QA-G4aT-07 | menor | architect | El alcance y la UI. (a) Durante el alcance, el motor avisa por cada lectura y cada segundo, y el *store* del HUD rehace el estado con textos cada vez. Con un fondo largo, como una sesión libre olvidada 8 h (~86 000 llamadas), se van segundos de hilo principal y decenas de MB de basura en el primer cuadro de vuelta, con riesgo de ANR. (b) No está definido cómo sabe la vista que un estado viene de un alcance (AC-WKT-02-08). `WorkoutHudState` no trae la marca. En PlayMode el anfitrión usa `FakeScheduler.Advance`, con `IsCatchingUp` en false. La vista recibe N estados en el mismo cuadro y el último rodillo arranca del valor anterior (`00:22` → `00:23`). El origen de los pulsos queda en el tiempo virtual | `02-tech-design.md:450-452`, `:488`, `:743`, `:1009`, `:1150`; `04-design-notes.md` §5.10 | Mientras `IAppLifecycle.InBackground` sea true (dura todo el alcance, por el orden de `AppRoot`) o `IsCatchingUp`, el *store* del HUD no rehace nada: un solo estado al final, con `Instant = true`. La vista aplica el último estado del cuadro sin animar y el origen de los pulsos es el primer cuadro real. En WBU, `IsCatchingUp = true` durante el `Advance`. Medir en dotnet el alcance de 1 h y de 8 h con un tope |
| QA-G4aT-08 | menor | po | Split con objetivo 0. AC-WKT-10-04 y -07 dicen "con un objetivo ≤ 0, Split siempre fuera, también con `--:--` (paridad: `val > target`)". Pero con objetivo 0 y split 0 el legacy da cumple (`0 > 0` es falso). Se llega con una progresión que los topes permiten, porque van por valor absoluto: split 4 s, −2 s por repetición, ×3 → 4, 2, 0. El diseño copió el "≤ 0". Una prueba escrita según el AC codifica lo contrario del legacy | `immersive_workout_screen.dart:581`; `01-po-spec.md:275`, `:278`; `02-tech-design.md:310`; `Assets/RowMate/Core/Routines/StepLimits.cs` (`FirstRepetitionOutOfRange` por valor absoluto) | "< 0" en AC-WKT-10-04 y -07 y en el comentario de §2.6. Casos en TLT: 0 contra 0 → cumple; 125 contra 0 → fuera; 0 contra −2 → fuera |
| QA-G4aT-09 | menor | architect | Los AC sin test van a salir SIN TEST en la matriz (`run-tests.ps1:165-170`, como QA-G3-14). AC-WKT-02-09, -02-10, -06-14 y AC-APP-14-02 no tienen ni test ni `[Ignore]` con su categoría. AC-WKT-23-01 y AC-HIS-20-04 nombran tests de dotnet pero están marcados solo "qa". Hay partes automatizables que no se planificaron: la configuración de -02-09 (ya la prueba `BootstrapperStaticTests.cs:141`), la regla de -14-02 y una prueba estática de -06-14. La parte de equipo de AC-WKT-02-08 es Android, no Windows | `02-tech-design.md:1322-1324`, `:1353`, `:1380`, `:1387`, `:1421`; `tools/dotnet/run-tests.ps1:165-170` | Categorías en los tests que ya existen. `[Ignore("NO VERIFICABLE: …")]` para -02-10 y para las partes de equipo. `SKA.RequestsAgainOnResumeAndFocus` para AC-APP-14-02. Una prueba estática para -06-14. Las marcas "d qa" para -23-01 y -20-04, y `It04aIds` con su bloque `UnityOnlyIds` |
| QA-G4aT-10 | menor | architect | RA-19 no figura en el diseño. En Android, el `Stopwatch` (reloj monótono sin sueño profundo) no cuenta el tiempo en que el equipo durmió con la pantalla bloqueada. Al volver, el alcance avanza menos que la hora de pared: el reloj del HUD, los pasos y los puntos se corren, y AC-WKT-02-10 ("2:00 ± 1 s") puede fallar. ADR 017 dice que la fuente se elige midiendo el legacy, y §4 no lo menciona | `../../architecture/adr/017-segundo-plano.md` (Consecuencias, RA-19); `02-tech-design.md:745`, `:780` | Riesgo R4a-12 en §10.2, con la decisión para la 10a (`elapsedRealtime` contra el monótono, contrastado con el legacy en el mismo teléfono). Una prueba de dotnet en la que `ResumeFromBackground` recibe un salto monótono menor que el de UTC y conserva el orden |
| QA-G4aT-11 | menor | architect | R3-08: "ninguna cuenta desborda" no vale para un split guardado ≤ 0. `StepLimitCheck` lo trata como "sin split" y no lo acota, pero `RoutineExpansion.Apply` lo progresa con `checked`. Un split guardado de −2 147 483 000, con progresión −5 999 y ×2, pasa la regla y desborda en `StartWithRoutine`: el usuario no ve nada y queda un `OverflowException` en el registro. El legacy arranca (enteros de 64 bits) | `Assets/RowMate/Core/Routines/StepLimitCheck.cs:111-114`; `Assets/RowMate/Core/Models/RoutineExpansion.cs:115`; `02-tech-design.md:243` | Que `WorkoutStartRule` compruebe, en forma cerrada, que todo valor progresado entra en un `int`, o que `Apply` sature en el camino del motor. Sumar ese caso a WSR (F-RARA) |
| QA-G4aT-12 | menor | architect | Arnés. (a) CAP-29: con `Scripted(FailOnCreate)` el alta falla en t ≈ 0 y el aviso dura 4 s. A los 5 s ya no está y la comprobación (`#toast` entero y `#hud-time 00:05`) no puede pasar. (b) VID-02: el guion cruza una sola vez de cumple a fuera, pero las notas de diseño esperan "el segundo cruce, antes de 4 s, sin empujón" | `02-tech-design.md:1196`, `:1200`; `01-po-spec.md:50`; `04-design-notes.md:814` | (a) CAP-29 a los 2 s (`00:02`), o la falla en el punto de los 5 s y la toma a los 5,5 s. (b) Que el guion de VID-02 cruce dos veces en menos de 4 s, o que el Designer saque esa expectativa |
| QA-G4aT-13 | menor | architect | Un lote que falla se descarta, y el cierre sale igual con estadísticas sobre todos los puntos en memoria. Queda una sesión **cerrada** con huecos y con totales que no salen de sus puntos, un estado que el legacy nunca produce: allí los puntos y la actualización van en el mismo `try`. El Historial del legacy calcula desde `data_points`, así que la It. 9 y la It. 12 verían números distintos | `02-tech-design.md:341`, `:599`; `workout_provider.dart:257-299`; `database_service.dart:289` | Que el cierre reinserte, en su misma transacción, los puntos de los lotes que fallaron (una vez), o que el spec lo acepte en AC-WKT-06-12 y se avise a la It. 9 |
| QA-G4aT-14 | menor | architect | Contrato USS en UI Toolkit. (a) USS no tiene `em` ni `align-items: baseline`: la flecha (0,4 em), el hueco (0,48 em), las líneas de base (+0,196 y +0,214 em) y `obj.` alineado con las cifras van en px, calculados por código, como `TabularNumber.BaselineFromTop`. (b) Las animaciones desde 0 (`obj.`, `Próximo`, el hueco, kcal por FLIP) necesitan el patrón de la It. 3: un *slot* que recorta, el contenido en absoluto o en `nowrap`, y la medida en el cuadro siguiente. Si no, Yoga aplasta o reacomoda el texto. §9.5 no lo dice | `02-tech-design.md:1494`, `:1496`; `04-design-notes.md:188-193`, `:570-575`, `:606`; `00-deuda-it3.md` §1.4 | Sumarlo a §9.5 y a la sonda S-B4a (la flecha y `obj.` medidos contra las líneas de base de §3.2, ± 1 dp) |
| QA-G4aT-15 | menor | architect | Dueños de archivos. WP-D0 cambia `Tokens.cs` (133 → 135, DN4a-18), pero los conteos y `TokensUssSyncTests` son de WP-A (`Tests/EditMode/**`). El `.gitignore` no tiene dueño | `02-tech-design.md:1457`, `:1461`; `04-design-notes.md:920` | Excepciones explícitas en §9, como QA-G3-15 |
| QA-G4aT-16 | menor | po | Precisiones de texto. (a) AC-WKT-02-02: "y la sesión existe" contra el punto con el alta en vuelo del diseño. (b) AC-WKT-06-15: "todo lo pendiente" contra el tope de 1 s y los puntos sin id (QA-G4aT-06). (c) AC-APP-14-01: `powercfg /requests` imprime la ruta completa del `.exe`, con la carpeta del usuario; en lo versionado va solo `RowMate.exe` en `DISPLAY` y `SYSTEM`. (d) La marca de equipo de AC-WKT-02-08 es [Device-And] | `01-po-spec.md:182`, `:188`, `:256`, `:340`; `02-tech-design.md:235`, `:585` | Ajustar los cuatro textos y sumar la nota de la evidencia a §7.1 y §8 |
| QA-G4aT-17 | menor | designer | Vista "en curso": en el legacy, `_ActiveView` es un `Scaffold` sin barra superior, solo con el texto centrado. Las notas de diseño y el árbol de §6.1 dejan `#workout-appbar` y centran el texto "entre las dos barras". Se ve solo durante los fundidos de 240 y 200 ms | `workout_screen.dart:180-191`; `04-design-notes.md:369`, `:387`; `02-tech-design.md:898` | Ocultar la barra superior en ese estado (paridad), o que el PO lo registre como DV |
| QA-G4aT-18 | menor | architect | La herramienta de la demo promete "ninguna ruta en la salida ni en los errores". Los mensajes de las excepciones de E/S y de SQLite de .NET traen la ruta completa. `SessionListToolTests` solo mira la salida buena | `02-tech-design.md:689-693` | Traducir los errores a textos fijos con el nombre del archivo. SLT con un archivo que falta y otro bloqueado, que compruebe que no aparecen ni la carpeta de datos ni la temporal |

---

## 15. Lo que este gate no verificó

- **Unity no se abrió** (la tarea es sin Unity). Lo de UI Toolkit en tiempo de ejecución se apoya en la sonda S-B4a, que el diseño pone antes de B2: la escala por zona, el *hit-test*, `FitBox` y `TabularNumber` escalados, `GlowTexture` y las métricas de ▲ y ▼.
- **Android, iOS e IL2CPP**: NO VERIFICABLE sin módulos ni teléfono. El alcance real, la ANR, RA-19 y `FLAG_KEEP_SCREEN_ON` se ven recién en la 10a y la 10b.
- **El orden de los eventos de Dart** (los *streams broadcast* de los tests legacy) lo leí en el código, sin ejecutarlo. La sonda S4-a del Architect los ejecutó y coincide con la spec.
- **La geometría y los contrastes de las notas de diseño** (los rectángulos de §3.8, las escalas de §3.7 y los pares de §9) no los recalculé: son de la lente visual.
- La deuda de la It. 3 (00-deuda-it3) solo la contrasté con la corrida de dotnet (1457 / 1440 / 0 / 17, igual que §12). Las suites de Unity y el arnés de esa deuda se re-verifican en 05-qa-verification.
