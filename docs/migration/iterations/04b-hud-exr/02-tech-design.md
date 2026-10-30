# It. 4b — HUD EXR: píldoras, transiciones, diálogos e inmersivo del sistema: diseño técnico

| Campo | Valor |
|---|---|
| Autor | unity-architect |
| Fecha | 2026-10-29 |
| Iteración | 4b · `04b-hud-exr` ([ROADMAP §It. 4b](../../ROADMAP.md)) |
| Entrada | [01-po-spec](01-po-spec.md), corregida por el gate (101 AC = 61 nuevos + 40 de iteraciones anteriores; LT-46; CAP-01 a -31, DEMO-01 a -12, VID-01 a -08; DV-40 a DV-47; P-01 a P-16; §16 "Cambios por gate QA"); [04-design-notes](04-design-notes.md) (DN4b-01 a DN4b-29, integradas en el Anexo B); el gate de armonía de QA, lentes de [paridad](03-qa-harmony-paridad.md) y [técnica](03-qa-harmony-tecnica.md); [00-deuda-4a](00-deuda-4a.md) (las dos condiciones y la deuda de la 4a, saldadas, con la regeneración única); [00-deuda-diseno](00-deuda-diseno.md) (la base de motion de la 4b y los pedidos DD4b-01 a DD4b-18); [02-tech-design de la 4a](../04a-motor-hud-funcional/02-tech-design.md) con sus "Cambios por gate QA" y "por verificación"; [REPORT de la 4a](../04a-motor-hud-funcional/REPORT.md) §7 a §9; [ARCHITECTURE](../../architecture/ARCHITECTURE.md) §2, §3, §6, §6.1, §7.2, §7.8, §13, §20; ADR [005](../../architecture/adr/005-hilos-y-asincronia.md), [016](../../architecture/adr/016-tokens-de-diseno.md) y [017](../../architecture/adr/017-segundo-plano.md); [ART_BIBLE](../../design/ART_BIBLE.md) §3.2, §3.3, §3.5, §5.2, §5.3, §10; [ANIMATION_BIBLE](../../design/ANIMATION_BIBLE.md) §0.1, §1, §2.2, §2.4 a §2.11, §7.1 y las dos secciones "Cambios al iniciar la It. 4b" |
| Legacy leído (solo lectura, `3ae0f3a`) | [rowmate-legacy](https://github.com/figuibej/rowmate-legacy): `lib/features/workout/step_pill.dart` (entero), `test/step_pill_test.dart` (entero), `lib/features/workout/immersive_workout_screen.dart:1-560` y `:955-1019`, `lib/features/workout/series_panels.dart:1-115`, `lib/features/workout/workout_provider.dart:14-45`, `:140-330`, `lib/features/workout/workout_screen.dart:160-180`. No se leyó `lib/core/strava/strava_secrets.dart`. `git -C ../rowmate-legacy status --porcelain` vacío al empezar y al terminar |
| Código leído (este repo) | `Core/Workout/{WorkoutEngine,WorkoutSnapshot,StepProgress,StepTargetTracker,StepResult,ActiveClock,WorkoutFormats}.cs`, `Core/Common/{FrameScheduler,IClock,IScheduler}.cs`, `Core/Ports/{IAppLifecycle,IDisplayMetrics}.cs`, `Core/Design/{HudGeometry,MotionSpecs,Tokens,Icons}.cs`, `App/Workout/{WorkoutHudState,WorkoutHudStore,WorkoutStore,WorkoutToast}.cs`, `App/Composition/AppContext.cs`, `Platform/Time/UnityScheduler.cs`, `Platform/Lifecycle/AppLifecycle.cs`, `Platform/Accessibility/AndroidAccessibility.cs`, `Presentation/Workout/{ImmersiveWorkoutPage,HudView,HudCard,HudBand,StepInfoBar,FreeTopBar,HudControls,HudBackdrop}.cs`, `Presentation/Motion/{MotionDriver,IMotion,TimedMotion,StepPulse,PageTransition,DialogTransition}.cs`, `Presentation/Shell/{PageStack,ModalDialog,DialogLayer}.cs`, `Presentation/Controls/{TabularNumber,RmButton}.cs`, `Presentation/UiContext.cs`, `Snapshots/{SnapshotCatalog,WorkoutCatalog}.cs`, `Tests/Support/{LifecycleHarness,FakeAppLifecycle,FakeScheduler,RoutineSeeds,RowerScripts,ScriptedSessionRepository}.cs`, `Tests/UiSupport/{UiHostOptions,UiTestHost}.cs`, `Tests/PlayMode/HudBudgetTests.cs`, `Tests/EditMode/Traceability/It04aIds.cs`, `tools/oracle/engine/test/engine_vectors_test.dart` (el conductor de los guiones) |
| Verificación previa | Medición aproximada de los anchos de la fila de información, del sello, de la barra libre y del chip con las fuentes del repo (Anexo A; fontTools 4.68.0 sin red, `<scratch>/it4b/arch/measure.py`): **la fila más larga de AC-WKT-14-15 no entra en TV en español ni sin la pastilla** (faltan ≈ 7 base) y **el título de la barra libre se recorta en TV en español** (faltan ≈ 30 base). Los dos fueron al Designer (§8.4), que los resolvió con DN4b-16 y DN4b-27 (Anexo B); las medidas que usa el diseño son las de las 04-design-notes, con las tablas K2 |
| Estado | Diseño, sin código, **corregido por el gate de armonía de QA** y alineado con la spec corregida y con las 04-design-notes (Anexo B; qué cambió y por qué, en "Cambios por gate QA", al final). Orden: **WP-A** → **WP-B0** → **WP-D1** → **WP-B1** → **WP-B2** → **WP-B3** → **WP-D2** (§7). Lo que tiene que estar antes de cada paquete está en el Anexo B y en §7.0 |

Convenciones: las de la 4a. Rutas relativas a `Assets/RowMate/` salvo que empiecen con `tools/`, `docs/` o `ProjectSettings/`. Marcas: **N** nuevo, **C** cambia, **G** generado. Dueño entre corchetes: **[A]** WP-A, **[B0]** a **[B3]** las entregas de WP-B, **[D1]** y **[D2]** las de WP-D (§7). `dp` = px de USS; `s` = `hudScale`; "base" = antes de multiplicar por `s`. Perfiles: **TV** 411 × 914, **TH** 914 × 411, **L640** 640 × 360, **W1280** 1280 × 720 y, solo para la geometría, **P360** 360 × 800. Las citas `lib/...:línea` y `test/...:línea` son del legacy. "Tick" = un segundo del motor (`ActiveClock`). "Instante" = el de la spec (§0.4): la sesión se crea en el instante 0 y el segundo k vence en el instante k.

---

## 0. Resumen

1. **Lo de la 4a ya está saldado** ([00-deuda-4a](00-deuda-4a.md)): las dos condiciones (el divisor de la banda, AC-WKT-17-04, y el primer Tab con anillo, AC-APP-05-07), las cifras de tablero y el kerning K2 (WP-B0 de la 4a, AC-WKT-17-12), `TabularNumber` fuera del panel (QA-V4a-05), los guiones del oráculo (QA-V4a-L02) y **la regeneración única de `it2`, `it3` e `it4a` con dos corridas iguales**. QA las verifica antes de WP-B1 (spec §12 punto 1). Este diseño no las vuelve a tocar.
2. **El motor espera al diálogo (DT4a-15).** Con la rutina terminada sola, `WorkoutEngine` queda en `Finished` aunque el cierre haya vuelto, hasta `AcknowledgeCompletion()` (el `Terminar` del diálogo, el `reset()` del legacy). El terminar a mano no cambia: vuelve solo a reposo cuando vuelve el cierre. La foto suma, como propiedades `init` (sin romper a nadie), la sesión, cómo terminó, los **totales fijos** del diálogo (los del punto final), el estado del cierre y el **último paso cerrado con objetivos con sus promedios** (para el sello). `StepTargetTracker` expone los promedios (`OutcomeOf`). El oráculo compara ahora también lo que pasa después del fin (AC-WKT-21-13).
3. **P-17 de la 4a: el reloj de la app es "despierto".** Entre el monótono crudo (el `Stopwatch`, que puede contar la suspensión o no) y `FrameScheduler` va `SuspendAccounting` (Core, puro): resta lo dormido que el crudo contó y avisa el despertar, en el cuadro y también en el alcance de la vuelta del segundo plano. Las señales en Windows son los avisos del sistema de "se suspende" y "reanuda" (`PowerRegisterSuspendResumeNotification`, solo en el build) y la contabilidad de sueño del núcleo (`GetTickCount64 − QueryUnbiasedInterruptTime`). Como todo (el motor, el remo simulado, los topes de espera y el motion) mide con ese reloj, **el lapso dormido no existe para la app**: sin segundos, sin puntos, sin lecturas de objetivos y sin falsa demora del cierre. Al despertar, `IAppLifecycle.Woke` hace que el motor publique una foto `Instant` y que el reloj de pared salte sin rodillo. Si no llega ninguna señal, el lapso es un cuadro tarde, como en la 4a (§2.9).
4. **Reglas puras nuevas** (WP-A, dotnet): el aspecto de cada píldora y su pintura (LT-46, con el puerto pasando por `LookOf`), la fila de píldoras del mismo ancho con la tira continua, el **pulgar** y los **rieles** de las píldoras angostas (Q-59, DV-40), el relleno continuo que nunca retrocede (`PillProgress`), la cuenta grande, el sello con el redondeo hacia el lado desfavorable (P-03), el chip de fase con `phase.finished.hud` (DV-44) y su ancho fijo, **la fila de información por modos y por fragmentos** y el título libre que se achica (DN4b-16, -27; DV-45, -47), el reloj de pared (con la zona horaria y los cambios de hora), la regla del diálogo (una vez por sesión, nunca a mano), la regla de las barras del sistema, la llave y el techo del panel (DN4b-22, -23) y **las líneas de tiempo de todo el motion como funciones puras del tiempo**, con sus constantes en `MotionSpecs` y `HudGeometry`.
5. **UI** (WP-B): la barra de pasos de dos filas, más la del sello mientras se ve (52 base; 74 con el sello), con la fila de píldoras en **un solo elemento** (el halo desde el borde con su textura, el brillo, el pulgar y los rieles), el reloj de pared como **un solo `TabularNumber` con el ícono de prefijo** (conserva el rodillo), la barra libre con el reloj, el chip de ancho fijo y el título que se achica, la cuenta grande y `PAUSA` **en el mismo disco**, uno por vez (DN4b-14), el sello como tercera fila de la barra, el diálogo de completado (un `ModalDialog` no descartable, opaco, con los valores en `num.s`), el velo de pausa (el tinte del fondo, sin elementos), kcal que se desliza (FLIP), la última foto en la salida (O-01), la entrada "sumergirse" y la salida, y la pastilla por fragmentos con el "…" blanco (DD4b-03, DN4b-16).
6. **Motion** (WP-D1): los `IMotion` de la 4b leen las líneas de tiempo puras y devuelven estructuras de valores a la vista por un *callback*; no conocen las vistas, así los archivos quedan disjuntos. Cada uno dice **en qué fases es finito** (y bloquea una toma) y cuál es su variante de reducir movimiento (§7.4). Nada del motion decide cuándo pasa algo (AC-WKT-13-14).
7. **Barras del sistema** (F-APP-15): puerto `ISystemUi.SetImmersive(bool)` (el nombre de ARCHITECTURE §13); Android por JNI (inmersivo persistente), escritorio y Editor sin efecto, iOS en la 10b. La regla (App, pura) pide ocultar al empezar cada entrada y mostrar al terminar cada salida, con cuenta de inmersivos para el caso de QA-V4a-01 (§4).
8. **Presupuesto del HUD (R4a-03).** El peor caso real de la 4b (los tres objetivos fuera, BPM, `Próximo`, la cuenta grande y el sello) llegaría a 69 elementos. Se juntan 11 sin cambiar lo que se ve: la flecha y la unidad las dibuja su `TabularNumber` (afijos), el halo de objetivo lo dibuja su tarjeta y el corazón de BPM va sin fila propia. Queda en **58** (§3.11).
9. **Arnés `it4b`** (WP-B3): `HudExrCatalog` con CAP-01 a -31, DEMO-01 a -12 y VID-01 a -08, con la zona horaria UTC−03:00, la fase fija del pulso por instante y **una regla de toma con definición operativa**: la toma es en el instante de la spec (nunca en un segundo entero: +0,2 s), y la bloquea una lista con nombre (las fases finitas de la 4b, lo de duración fija de la 4a, los rodillos antes de su mitad y los resortes de la 4a a más de 0,15 dp de su destino; §5.2). Las tomas del HUD de `it4a` quedan **reemplazadas** por las de `it4b` (no se regeneran otra vez; §5.6).
10. **Spec y Designer ya coinciden**: el Designer confirmó los números de la spec para la entrada, la salida y el sello (DN4b-01 a -03), y el PO decidió el resto en el gate (spec §16). Las alternativas del Designer salen de `MotionSpecs` (§8.2), y las 04-design-notes quedan integradas con su decisión una por una (Anexo B).
11. **Paquetes**: WP-A (≈ 66 archivos, dotnet), WP-B (≈ 78, Unity, en cuatro entregas), WP-D1 (≈ 17, Unity, motion) y WP-D2 (Designer). Archivos disjuntos con sus excepciones (§7.6), contrato de motion (§7.4) y contrato USS (§7.5).

| Pregunta de la tarea | Dónde |
|---|---|
| 1) Árbol de archivos | §1 |
| 2) Reglas puras con firmas y fuente legacy: píldoras, último en curso (Q-52), diálogo solo al terminar sola (Q-53) y el motor que espera (DT4a-15), mismo ancho (Q-59), chip, reloj de pared, sello (D-ANI-05), suspensión (P-17) | §2 |
| 3) Motion: interpolación sin tocar el dato, pulso de los últimos 5 s, cuenta grande, entrada y salida (O-01), reducir movimiento, presupuesto (R4a-03), frame pacing y nada animando al salir | §3 |
| 4) Barras del sistema por plataforma | §4 |
| 5) Capturas y videos | §5 |
| 6) Tests, matriz AC → test / captura, LT-46 | §6 |
| 7) Paquetes, archivos disjuntos, contrato de motion y contrato USS | §7 |
| 8) Riesgos y desvíos | §8 |
| Medición de anchos | Anexo A |
| Las 04-design-notes, aceptadas o rechazadas una por una | Anexo B |
| Qué cambió por el gate de QA | "Cambios por gate QA" |

---

## 1. Estructura

### 1.1 Árbol de archivos

#### Core (`RowMate.Core`, puro)

| Archivo | Marca | Contenido |
|---|---|---|
| `Core/Workout/StepPills.cs` | N [A] | `StepPillLook`, `PillPaint` y la regla de cada píldora: aspecto por índice y resultado, si se llena, su pintura (§2.1; `step_pill.dart:6`, `:30-55`, `:71-76`; `immersive_workout_screen.dart:367-371`; `workout_provider.dart:31-36`) |
| `Core/Workout/StepOutcome.cs` | N [A] | el resultado de un paso cerrado con sus promedios y cuentas (§2.5; `step_target_tracker.dart:30-62`) |
| `Core/Workout/StepTargetTracker.cs` | C [A] | + `OutcomeOf(int)` (los promedios que ya calcula `Complete`); la regla no cambia (LT-39 a LT-44) |
| `Core/Workout/StepSeal.cs` | N [A] | cuándo hay sello y qué dice: veredicto y fragmentos con el redondeo de P-03 (§2.5) |
| `Core/Workout/BigCountdown.cs` | N [A] | cuándo hay cuenta grande y qué número (§2.4) |
| `Core/Workout/PhaseChip.cs` | N [A] | texto y colores del chip por fase, con `FINALIZADO` en `phase.finished.hud`, y su ancho fijo (§2.7; `immersive_workout_screen.dart:530-547`; DV-44; DN4b-15, -26) |
| `Core/Workout/CompletionSummary.cs`, `CloseStatus.cs` | N [A] | los totales fijos del diálogo y el estado del cierre (§2.6) |
| `Core/Workout/WorkoutSnapshot.cs` | C [A] | + `Session`, `EndedBy`, `Completion`, `Close`, `LastOutcome`, `LastOutcomeAt` como `init` (§2.6) |
| `Core/Workout/WorkoutEngine.cs` | C [A] | la espera del diálogo (`AcknowledgeCompletion`), los totales del punto final, el último paso cerrado con objetivos, la foto `Instant` al despertar (§2.6, §2.9) |
| `Core/Common/SuspendAccounting.cs`, `SystemWake.cs` | N [A] | P-17: el reloj despierto y el despertar, con los avisos de "se suspende" y "reanuda" (§2.9) |
| `Core/Common/FrameScheduler.cs` | C [A] | + `Tick(in FrameClock)` y `ResumeFromBackground(in FrameClock)`, los dos con el reloj despierto; `Woke`; `SleepBias` (§2.9). `Tick(double, DateTimeOffset, TimeSpan)` y `ResumeFromBackground(double, DateTimeOffset, TimeSpan)` siguen (sin medida ni avisos: las pruebas de la 4a) |
| `Core/Common/FrameClock.cs` | N [A] | lo que el que avanza le pasa al programador en cada cuadro y al volver del segundo plano (monótono crudo, UTC, offset, dormido según el sistema, avisos de "se suspende" y de "reanuda") |
| `Core/Common/ForegroundLifecycle.cs` | C [A] | `Woke` que nunca avisa |
| `Core/Ports/IAppLifecycle.cs` | C [A] | + `IEventSource<SystemWake> Woke` (§2.9) |
| `Core/Ports/ISystemUi.cs` | N [A] | `SetImmersive(bool)` (§4; ARCHITECTURE §13) |
| `Core/Ports/IDisplayMetrics.cs` | C [A] | + `ImmersiveSafeArea` (solo el recorte de la pantalla; §4.3) |
| `Core/Design/PillRowGeometry.cs` | N [A] | la fila de píldoras: mismo ancho, separación 3 → 1 → tira, bordes en píxeles, el pulgar y los rieles de las angostas (§2.2; DN4b-18) |
| `Core/Design/PillShapes.cs` | N [A] | la geometría que dibuja la fila: la cápsula recortada del relleno y su brillo, el trazo del borde por perímetro, los rieles, el pulgar, el 9-slice del halo desde el borde con su largo por lado, los tramos de la tira juntados por columna de píxel, el arco del aro (§3.4, §3.5; DN4b-17 a -20) |
| `Core/Design/PillProgress.cs` | N [A] | el relleno continuo, que nunca retrocede (§2.3; ANIMATION_BIBLE §2.6, §7.1; DD4b-09; QA-G4bT-04) |
| `Core/Design/InfoRowFit.cs` | N [A] | qué cede en la fila de información: los modos y los fragmentos de la pastilla; el título libre que se achica hasta 0,8 (§2.15; DN4b-16, -27; AC-WKT-14-03, -04, -14, -15, -16-06) |
| `Core/Design/CountUp.cs` | N [A] | los totales que cuentan de 0 a su valor exacto (ANIMATION_BIBLE §2.5) |
| `Core/Design/HudTimelines.cs` | N [A] | las líneas de tiempo puras: `ImmersiveTimeline`, `StepBarTimeline` (con el pulgar que pasa), `InfoRowTimeline`, `SealTimeline`, `CenterDiscTimeline` (la cuenta grande, `PAUSA` y un disco por vez), `PauseTimeline` (el velo), `PhaseChipTimeline`, `KcalSlideTimeline`, `CompletionTimeline`, los tipos de valores (`PieceVisual`, `SealVisual`…) y `Reached` (la comparación de los finales con tolerancia) (§3.1) |
| `Core/Design/MotionSpecs.cs` | C [A] | el bloque de la 4b (§3.1, tabla de constantes) |
| `Core/Design/HudGeometry.cs` | C [A] | la barra de 52 / 74 / 40, la fila de información sin relleno de arriba, el disco del lugar "centro alto" (la cuenta grande y `PAUSA`), el sello dentro de la barra, el techo del panel en las dos orientaciones y las piezas del carril izquierdo para la llave con el diálogo; `HudLayout.BigCount`, `.TopBarMax`, `.PanelCeiling`, `.LeftLaneObstacles` (§3.5, §5.7; DN4b-14, -22, -23) |
| `Core/Design/OverlaySurface.cs` | C [A] | `Workout(…)` + las piezas del carril izquierdo y el rectángulo del diálogo abierto (R10, §5.7) |
| `Core/Design/Tokens.cs`, `Presentation/Styles/Tokens.uss` (G) | C [A] | `phase.finished.hud` `#FF7A92` (rol `HudText`): 135 → 136; `tools/tokens --check` en 0 (DN4b-15; excepción de dueño, §7.6) |
| `Core/Design/CheckGlyph.cs` | N [A] | el trazo del check del diálogo como polilínea parcial (§3.9) |
| `Core/Localization/L10nKeys.cs`, `Localization/strings.es.json`, `strings.en.json` | C [A] | las 12 claves de spec §11.2: 202 → **214** (§2.12) |

#### App (`RowMate.App`, puro)

| Archivo | Marca | Contenido |
|---|---|---|
| `App/Workout/WorkoutHudState.cs` | C [A] | `HudPillRow`, `HudSeal`, `HudPhaseChip`, `HudCompletion`; `HudTargetLabel` + `Fragments` (los fragmentos de la pastilla, DN4b-16); `WorkoutHudState` + `Session`, `EndedBy`, `Pills`, `BigCount`, `Seal`, `Chip`, `Completion`, `CloseReturned` como `init` (§2.11) |
| `App/Workout/WorkoutHudStore.cs` | C [A] | arma lo nuevo (las píldoras con una caché que se rehace solo en el cambio de paso; los fragmentos de la pastilla, una vez por paso), `AcknowledgeCompletionAsync()`, `WallClock` (§2.11) |
| `App/Workout/WallClockTicker.cs` | N [A] | el reloj de pared, su propio ritmo, sin periódico mientras la app está en segundo plano (§2.8; QA-G4bT-08) |
| `App/Workout/CompletionDialogRule.cs` | N [A] | una vez por sesión, nunca a mano, sin entrada si llega con `Instant` (§2.6) |
| `App/Workout/ImmersiveSystemBars.cs` | N [A] | la regla de las barras del sistema, con cuenta de inmersivos (§4.1) |
| `App/Workout/WorkoutStore.cs`, `WorkoutToast.cs` | C [A] | sale el aviso `workoutCompleted` (es el título del diálogo); los avisos se retienen con el diálogo de completado abierto (AC-APP-03-11; §2.11) |
| `App/Composition/AppServices.cs`, `AppComposer.cs`, `AppContext.cs` | C [A] | + `ISystemUi? SystemUi` (`init`; nulo = sin efecto), `AppContext.SystemBars`, `CreateHud()` con el programador y el ciclo de vida (§2.14) |

#### Infrastructure y Simulator (puros)

| Archivo | Marca | Contenido |
|---|---|---|
| `Simulator/Panel/WrenchPlacement.cs` | C [A] | R10: en el entrenamiento, con un diálogo que se cruza con el lugar del HUD, la llave va al hueco libre más bajo del carril izquierdo, a 8 dp de las piezas del HUD y del diálogo (§5.7; AC-BLE-16-02; DN4b-22) |
| `Simulator/Panel/SimulatorPanelLayout.cs` | C [A] | el alto máximo con el borde de abajo **del cuadro** (DD4b-04, 04b/00-deuda-diseno §3.2). El techo nuevo del entrenamiento (AC-BLE-16-03, DN4b-23) llega por `OverlaySurface.PanelCeiling`: la fórmula no cambia |

Infrastructure no cambia: el esquema sigue en v8 y nada nuevo se guarda (AC-WKT-06-14).

#### Platform (`RowMate.Platform`, Unity)

| Archivo | Marca | Contenido |
|---|---|---|
| `Platform/Power/SystemSleepSource.cs` | N [B2] | `ISystemSleepSource` (interno de Platform), `WindowsSleepSource` (P/Invoke a `powrprof.dll` y `kernel32.dll`; los avisos de "se suspende" y "reanuda" solo en el build, con su estructura en memoria propia; las llamadas nativas detrás de `IPowerNotifyApi`, interno, para probar el orden de registro y baja) y `NullSleepSource` (§2.9.3; QA-G4bT-05) |
| `Platform/Time/UnityScheduler.cs` | C [B2] | arma el `FrameClock` con la fuente de sueño en cada cuadro **y en la vuelta del segundo plano**; reenvía `FrameScheduler.Woke`; el offset local cada 0,5 s con `LocalOffsetSource` (§2.8.3, §2.9.3) |
| `Platform/Time/LocalOffsetSource.cs` | N [B2] | el offset de la zona sin la caché de `TimeZoneInfo` (Windows: `GetTimeZoneInformation` en un búfer reservado una vez) |
| `Platform/Lifecycle/AppLifecycle.cs` | C [B2] | + `Woke` y `SignalWake(SystemWake)` |
| `Platform/SystemUi/AndroidSystemUi.cs`, `DesktopSystemUi.cs`, `SystemUiFactory.cs` | N [B2] | §4.2 |
| `Platform/Display/DisplayMetricsProvider.cs` | C [B2] | + `ImmersiveSafeArea`; en Android, los márgenes de la app estables con las barras ocultas (§4.3) |
| `Platform/PlatformServices.cs`, `PlatformFactory.cs` | C [B2] | + `SystemUi`, `SleepSource` |

#### Presentation (`RowMate.Presentation`, Unity)

| Archivo | Marca | Contenido |
|---|---|---|
| `Presentation/Controls/TabularNumber.cs` | C [B0] | **afijos** (prefijo y sufijo con su fuente, tamaño en em, color, alfa, línea de base, aire y desplazamiento) y **placa de fondo** opcional que dibuja el mismo elemento (§3.11, M1 a M3, M7); `RollProgress` (0 → 1 del rodillo en curso; 1 sin rodillo) para la regla de toma (§5.2) |
| `Presentation/Controls/FitBox.cs` | C [D1] | implementa `ISettlingMotion` (lo que le falta a su resorte, en dp; §5.2) |
| `Presentation/Workout/HudCard.cs` | C [B0] | la flecha como prefijo y la unidad como sufijo del número (M1, M2), el halo de objetivo dibujado por la tarjeta (M4), el corazón de BPM sin fila propia (M6) |
| `Presentation/Workout/HudBand.cs` | C [B0] | la `m` como sufijo de la distancia (M2) |
| `Presentation/Workout/StepInfoBar.cs` | C [B0, B1] | B0: `Próximo` en una sola etiqueta (M3) y sin el espaciador (M5), la pastilla armada en código por fragmentos con el "…" blanco (DD4b-03, DN4b-16). B1: la barra de dos filas más la del sello (§3.4 a §3.6), el plan de `InfoRowFit` por paso con `InfoRowMotion` (§2.15); mide solo con panel |
| `Presentation/Workout/PillRowView.cs` | N [B1] | la fila de píldoras: un elemento con `generateVisualContent` (§3.4) |
| `Presentation/Workout/PillGlowTexture.cs` | N [B1] | la textura del halo de la píldora: radio 4, 10 de halo, el perfil de `GlowTexture`, a 3× (86 × 86 px, ≈ 29,6 KB), generada al arrancar (DN4b-17; 04-design-notes §6) |
| `Presentation/Workout/WallClockView.cs` | N [B1] | el reloj de pared: un `TabularNumber` con `schedule` de prefijo (§2.8, §3.7) |
| `Presentation/Workout/FreeTopBar.cs` | C [B1] | + el reloj, el chip y el título que se achica hasta 0,8 antes del "…" (§3.7; DN4b-27) |
| `Presentation/Workout/PhaseChipView.cs` | N [B1] | el chip de fase con el punto en vivo y ancho fijo (§3.7; DN4b-26) |
| `Presentation/Workout/SealRow.cs` | N [B1] | la tercera fila de la barra (§3.6) |
| `Presentation/Workout/CenterCallouts.cs` | N [B1] | el disco del lugar "centro alto": la cuenta grande (un `TabularNumber` con su placa) y `PAUSA` (un `Label` con su disco, su aro y el glifo dibujados por el mismo elemento), uno por vez (§3.5; DN4b-14) |
| `Presentation/Workout/HudView.cs` | C [B1, B2] | las piezas nuevas; `Freeze()` (la última foto); el velo; kcal; `Dispose` que para todo |
| `Presentation/Workout/HudZones.cs` | C [B1] | el ancla del disco (la cuenta grande y `PAUSA`); el alto vivo de la barra para `HudOverlap`; las piezas del carril izquierdo para la llave (§5.7) |
| `Presentation/Workout/HudBackdrop.cs` | C [B1] | el canal del velo (tinte de la textura) |
| `Presentation/Workout/HudControls.cs` | C [B2] | `SetInteractive(bool)`: los controles responden al toque **y al teclado** desde que empieza su tramo de la cascada; antes no se enfocan (§3.2; QA-G4bT-09) |
| `Presentation/Workout/CompletionDialog.cs`, `CheckMarkView.cs` | N [B2] | el diálogo de completado y su check dibujado; `OnClosing` y `DetachFromPanelEvent` detienen `CompletionMotion` (§3.9; QA-G4bT-08) |
| `Presentation/Workout/ImmersiveWorkoutPage.cs` | C [B2] | la sesión que muestra, el diálogo (300 ms, una vez, sin entrada con `Instant` ni si vence en el alcance), la entrada y la salida, la última foto, las barras del sistema, la llave al terminar la cascada, `RevealStarted` a los 400 de la salida (los avisos del final, DN4b-24); el primer Tab no entra al anillo hasta que los controles responden (840) y el diálogo se cierra con `Cancel()` al volver a reposo (§3.2, §3.9; QA-G4bT-09); el rectángulo del diálogo y las piezas del carril izquierdo para la llave (R10, §5.7) |
| `Presentation/Workout/WorkoutScreenView.cs` | C [B2] | `Push(page, PageTransitionKind.Immerse)`; el aviso del final espera el `RevealStarted` del inmersivo que sale |
| `Presentation/Motion/PageTransition.cs` | C [B2] | `PageTransitionKind.Immerse`: entra la página negra en 220; sale después de la salida del HUD (espera 400 y funde en 240) |
| `Presentation/Shell/ModalDialog.cs`, `DialogLayer.cs` | C [B0] | `Dismissible`: los caminos del usuario (el velo, Escape y "Atrás") pasan por `Dismiss()`, que con `false` no cierra y consume; `Cancel()` sigue siendo el cierre del dueño. `Open(dialog, instantly)`. `DialogLayer` lleva el primer Tab sin foco al diálogo de arriba (§3.9; QA-G4bT-09) |
| `Presentation/Shell/ShellLayout.cs` | C [B2] | `ImmersiveInsets`; la superficie "oculta" de la llave durante la cascada (§5.7) |
| `Presentation/Motion/Hud/*.cs` | N [D1] | los `IMotion` de la 4b (§7.4): `ImmersiveCascadeMotion`, `ImmersiveExitMotion`, `PillRowMotion` (con el pulgar que pasa), `InfoRowMotion` (los aires por modo y la pastilla que funde), `SealMotion`, `CenterDiscMotion` (la cuenta grande, `PAUSA` y un disco por vez), `PauseVeilMotion`, `LiveDotMotion`, `PhaseChipMotion`, `KcalSlideMotion`, `CompletionMotion` |
| `Presentation/Motion/ISettlingMotion.cs` | N [D1] | lo que le falta a un resorte de la 4a, en dp (§5.2) |
| `Presentation/Motion/SpringMotion.cs`, `StepPulse.cs`, `TimedMotion.cs` | C [D1] | los dos resortes implementan `ISettlingMotion` (el ancho, en dp; la escala de la cuenta, por su ancho); `TimedMotion` compara su final con `HudTimelines.Reached` (QA-G4bT-07) |
| `Presentation/Styles/Base.uss` | C [B0, B1, B2] | estructura y contrato de §7.5 |
| `Presentation/Styles/Skin.uss`, `Motion.uss` | C [D2] | aspecto |

#### Simulator.Presentation, Bootstrap, arnés, soporte de pruebas y herramientas

| Archivo | Marca | Contenido |
|---|---|---|
| `SimulatorPresentation/SimulatorLayerView.cs` | C [B0, B2] | B0: DD4b-04 (el panel sigue a la llave sin saltos). B2: la llave entra al terminar la cascada y se va en el cuadro 0 de la salida; R10 con el diálogo (la página declara su rectángulo) |
| `SimulatorPresentation/FrameTimeProbe.cs` | N [B2] | solo con el simulador: el resumen del tiempo de CPU del hilo principal por cuadro en el registro del build mientras el inmersivo está arriba (AC-WKT-14-19 [Device-Win]; §3.13) |
| `Bootstrap/AppRoot.cs` | C [B2] | `SystemUi`, la fuente de sueño, `Woke` del programador al ciclo de vida; la vuelta del segundo plano con el `FrameClock` (§2.9) |
| `Editor/Build/BuildCli.cs` | C [B3] | con `-rowmateSimulator`, `PlayerSettings.enableFrameTimingStats` encendido durante ese build y restaurado al terminar (`ProjectSettings` no cambia; §3.13) |
| `Snapshots/HudExrCatalog.cs` | N [B3] | el conjunto `it4b` (§5) |
| `Snapshots/SnapshotCatalog.cs`, `SnapshotCli.cs`, `SnapshotRunner.cs`, `ScenarioScript.cs`, `WrenchAudit.cs`, `SnapshotManifest.cs` | C [B3] | `-snapshotSet it4b` por defecto; la regla de toma de la 4b con su lista de bloqueos y `inFlight` en el *manifest*; los instantes en ms enteros; el remo simulado con sus lecturas en k − 0,5 (§5.2); `HudOverlap` y `WrenchAudit` con las piezas nuevas; las tomas reemplazadas (§5.6) |
| `Snapshots/WorkoutCatalog.cs` | C [B3] | las tomas del HUD de `it4a`, marcadas `SupersededBy("it4b", …)` (§5.6) |
| `Tests/Support/FakeAppLifecycle.cs`, `LifecycleHarness.cs`, `FakeScheduler.cs`, `FakeDisplayMetrics.cs` | C [A] | `Woke`; `Suspend` / `Wake` en los dos casos del monótono, con el aviso de reanudar antes o después del primer cuadro y con la vuelta por el segundo plano; `Stall` (un tirón); `SetLocalOffset`, `ShiftWallClock`; `ImmersiveSafeArea` |
| `Tests/Support/FakeSystemUi.cs` | N [A] | anota cada pedido con su instante |
| `Tests/Support/RoutineSeeds.cs`, `RowerScripts.cs` | C [A] | `F4B` (`Estados`, `Final por distancia`, `Texto largo`, `Serie larga`, `Maratón`); `R4With(spm, watts)` y `R4X` (= `Extremes`) |
| `Tests/UiSupport/UiTestHost.cs`, `UiHostOptions.cs` | C [B1, B2] | `SystemUi` de prueba por host; `SuspendAndWake(s)`, `SetLocalOffset`, `ShiftWallClock`; accesos al diálogo y `OpenCompletion(HudCompletion)` (abre el diálogo con los totales de un caso, sin correr la rutina: QA-G4bT-11 (d)); `AdvanceToMs` |
| `Tests/EditMode/**`, `Tests/Simulator/**` | N/C [A] | §6.2 (salvo `Tests/EditMode/Project/{SnapshotEvidenceTests,ItFourBToolingTests}.cs`, de B3: excepción de §7.6) |
| `Tests/PlayMode/**`, `Tests/PlayModeSimulator/**`, `Tests/UnityEditMode/**` | N/C [B0-B3, D1] | §6.2 |
| `Tests/Golden/oracle/workout_engine.json`, `tools/oracle/engine/test/engine_vectors_test.dart` | G / C [A] | un guion nuevo del fin que espera (§2.13) |
| `tools/unity/capture-snapshots.ps1`, `tools/unity/README.md` | C [B3] | `-Set it4b` |
| `docs/migration/iterations/04b-hud-exr/snapshots/*` | G [B3, D1] | el arnés |
| `docs/migration/architecture/adr/020-reloj-despierto.md` | N [A] | ADR corto de P-17: el reloj de la app no cuenta el sueño del equipo; las señales y qué pasa sin ellas (§2.9) |

**Manifest:** `Packages/manifest.json` no cambia. Ningún paquete nuevo de NuGet.

### 1.2 Asmdefs

**Ninguno nuevo y ninguna referencia nueva.** `Presentation/Motion/Hud/` es una carpeta de `RowMate.Presentation`; `Platform/SystemUi/` y `Platform/Power/SystemSleepSource.cs`, de `RowMate.Platform` (que ya usa P/Invoke y JNI). El callback del aviso de suspensión lleva `[AOT.MonoPInvokeCallback]` (necesario con IL2CPP), que está en `UnityEngine`, y **solo se registra en el build** (`!Application.isEditor`): en el Editor una recarga de dominio dejaría al sistema llamando a código descargado (QA-G4bT-05). No hace falta `allowUnsafeCode`: el búfer de `GetTimeZoneInformation` y la estructura del registro del aviso se reservan una vez con `Marshal.AllocHGlobal`. `PureCodeRulesTests` suma: ningún reloj del sistema en los archivos nuevos de `Core/Workout`, `Core/Design` y `App/Workout`, y `Core/Common/SuspendAccounting.cs` sin `Stopwatch` ni `DateTime.Now` (recibe todo por argumento).

---
## 2. Reglas puras (Core y App, WP-A)

Todo compila en `netstandard2.1`, C# 9, sin `UnityEngine`, sin asignar memoria en lo que se llama por cuadro (structs, arreglos propios, sin LINQ ni *closures*). El tiempo sale solo del `IScheduler` o llega por argumento (`PureCodeRulesTests`). Cada regla cita su fuente legacy; las mejoras EXR dicen de qué dato salen (§3.0).

### 2.1 Píldoras: aspecto, pintura y el último paso (F-WKT-15, AC-WKT-14-08, -09, -13; Q-51, Q-52; LT-46)

```csharp
namespace RowMate.Core.Workout
{
    // Los cuatro estados de la píldora (step_pill.dart:6, :10-12; docs/superpowers/specs/2026-10-02-step-timeline-states-design.md:14-21
    // del legacy): tenue = falta, llenándose = en curso, sólida = cumplida o sin objetivo, hueca = intentada y no cumplida.
    public enum StepPillLook { Pending, Current, Solid, Hollow }

    // Lo que se pinta de una píldora, sobre el color BASE de su tipo (StepTypeInfo.Color; es un gráfico: ART_BIBLE §2.3).
    public readonly struct PillPaint
    {
        public double TrackAlpha { get; }       // el fondo: 0,18 pendiente y hueca; 0,30 en curso; 0,85 sólida (:30-32, :44-54)
        public double BorderAlpha { get; }      // 0 sin borde; 0,85 la hueca (:49-51)
        public double BorderWidthBase { get; }  // 0 o 1,5 (× s)
        public double FillFraction { get; }     // solo en curso con progreso > 0, acotado a [0; 1] (:71-76); si no, 0 = sin relleno
        public bool HasFill => FillFraction > 0;
        public double HighlightAlpha { get; }   // 0,25 en curso con relleno (el brillo de 1·s, DN4b-19); si no, 0
    }

    public static class StepPills
    {
        public const double PendingAlpha = 0.18, CurrentTrackAlpha = 0.30, DoneAlpha = 0.85, MissedBorderWidth = 1.5; // :30-32, :51

        // immersive_workout_screen.dart:367-371: antes del paso en curso, hecha con su resultado (Met → sólida, Missed → hueca,
        // null = sin objetivo → sólida, :52-54); el paso en curso; después, pendiente. Q-52 sale solo: con la rutina terminada el
        // índice queda en el último (workout_provider.dart:203-216), así que el último sigue "en curso" y su resultado, que el
        // motor sí calculó (:204), no se ve.
        public static StepPillLook LookOf(int index, int currentIndex, StepResult? result);

        // workout_provider.dart:31-36 (Q-51): solo se llena un paso por tiempo con duración > 0. Por distancia, sin meta, 0 s o
        // duración negativa: en curso con la pista sola.
        public static bool Fills(IntervalStep step);

        // step_pill.dart:30-55, :71-76. progress solo cuenta para Current (LT-46).
        public static PillPaint Paint(StepPillLook look, double progress);
    }
}
```

| Aspecto | `TrackAlpha` | Borde | Relleno | Legacy |
|---|---|---|---|---|
| `Pending` | 0,18 | — | — | `step_pill.dart:44-45` |
| `Current` | 0,30 | — (sin marco: commit `3ae0f3a`) | `clamp(progress; 0; 1)` al 100 %, desde la izquierda; con 0, nada | `:46-47`, `:71-76` |
| `Solid` (cumplida o sin objetivo) | 0,85 | — | — | `:52-54` |
| `Hollow` (no cumplida) | 0,18 | 1,5 al 0,85 | — | `:49-51` |

- **El brillo del relleno** (AC-WKT-15-03; DN4b-19): `PillPaint.HighlightAlpha` vale 0,25 solo en `Current` con relleno (`HasFill`); es una línea de 1·s blanca en el borde de arriba del relleno, recortada por la cápsula. Distingue la en curso llena (CAP-06) de la hecha. Es del sistema visual (spec §11.1.1).
- **Las angostas** (AC-WKT-15-05, -14-16; DN4b-18): la pintura es la misma; con menos de 6 base el borde de `Hollow` se dibuja como **dos rieles** (arriba y abajo, sin los costados) y la `Current` como **pulgar** (§2.2). Lo decide la geometría, no el aspecto.

El color de cada píldora es `StepTypeInfo.Color(type)` (trabajo `#EF476F`, descanso `#06D6A0`, calentamiento `#FFD166`, enfriamiento `#118AB2`; desconocido `#607D8B`, que no llega a verse porque esa rutina no carga: AC-WKT-12-06). La regla de LT-46 se prueba armando cada aspecto con `LookOf` y comparando lo que se pinta: el color final (el base por el alfa), el borde y el relleno (§6.3; QA-G4b-P08). El estado por índice lo arma el *store* del HUD (§2.11) con `engine.Targets.ResultOf(i)`; el progreso de la en curso, con `StepProgress.Progress` (el del tick) y el relleno continuo de §2.3.

### 2.2 La fila: todas del mismo ancho, con cualquier cantidad de pasos (AC-WKT-14-07, -08, -16; Q-59; DV-40)

```csharp
namespace RowMate.Core.Design
{
    // La fila de píldoras (immersive_workout_screen.dart:358-377; step_pill.dart:57-60, :103; 04b/00-deuda-diseno §4.4). Todo
    // en dp base; la vista lo multiplica por s y lo lleva a píxeles con la densidad.
    public readonly struct PillRowLayout
    {
        public int Count { get; }
        public double Gap { get; }             // 3; 1 si con 3 no llegan a 6 de ancho; 0 en la tira
        public double PillWidth { get; }       // el lugar de cada una, igual para todas (Q-59): (ancho útil − Gap·(n − 1)) / n
        public bool Strip { get; }             // la tira continua (DV-40): ni con 1 llegan a 2 de ancho
        public bool Narrow { get; }            // PillWidth < 6 (en cualquier régimen): la en curso es un pulgar y la hueca, rieles
        public double LeftOf(int index);       // borde izquierdo, desde el borde izquierdo del área de la fila
        // El pulgar de la en curso (DN4b-18; AC-WKT-14-16): max(4; PillWidth) de ancho × 10 de alto (1 arriba y 1 abajo de la
        // franja de 8), centrado en su lugar y acotado a la fila; radio min(4; ancho / 2). Sin Narrow, la píldora misma.
        public DpRectBase ThumbOf(int index);
    }

    public static class PillRowGeometry
    {
        public const double PadLeft = 10, PadTop = 8, PadRight = 10, PadBottom = 4;      // :359-360
        public const double Height = 8, Radius = 4, Gap = 3, NarrowGap = 1;             // step_pill.dart:57-60; :364
        public const double MinWidthWithGap = 6, MinWidthNarrow = 2;                     // 04b/00-deuda-diseno §4.4
        public const double NarrowWidth = 6, ThumbMinWidth = 4, ThumbHeight = 10;        // DN4b-18 (sale StripCurrentMin 2)
        public static double RowHeight => PadTop + Height + PadBottom;                  // 20

        public static PillRowLayout Layout(double usefulWidthBase, int count);

        // Bordes en píxeles enteros de la fila (izquierdo y derecho de cada lugar), así ningún lugar se ve más ancho que otro en
        // más de 1 px (AC-WKT-14-08). Escribe en los arreglos que le pasan (sin asignar).
        public static void PixelEdges(in PillRowLayout layout, double originPx, double pxPerBase, int[] left, int[] right);
    }
}
```

`DpRectBase` es un rectángulo en dp base desde el borde izquierdo y de arriba del área de la fila (el *struct* de `HudGeometry`, sin asignar).

| Perfil | Ancho útil (base) | Separación 3 (píldoras ≥ 6) | Separación 1 (≥ 2) | Angostas (< 6): pulgar y rieles | Tira | `Prueba 4a` (6) | `Pirámide 500` (14) | `Serie larga` (200) | Tope del editor (1998) |
|---|---|---|---|---|---|---|---|---|---|
| TV · P360 | 344,0 | hasta 38 pasos | hasta 115 | desde 50 | desde 116 | 54,8 | 21,8 | tira de 1,72 | tira de 0,172 (0,48 px en TV) |
| TH | 821,3 | hasta 91 | hasta 274 | desde 118 | desde 275 | 134,4 | 55,9 | separación 1: 3,11 | tira de 0,411 |
| L640 | 647,3 | hasta 72 | hasta 216 | desde 93 | desde 217 | 105,4 | 43,5 | separación 1: 2,24 | tira de 0,324 |
| W1280 | 754,0 | hasta 84 | hasta 251 | desde 108 | desde 252 | 123,2 | 51,1 | separación 1: 2,77 | tira de 0,377 |

(El ancho útil es el de 04b/00-deuda-diseno §4.4: la barra a 12·s de cada lado, menos el borde y el relleno de 10 y 10. Los umbrales de las angostas son los de la spec §16.1, recalculados con la regla de regímenes: entre 39 y 49 pasos en TV rige la separación 1 con lugares de 6 base o más.)

- **Las angostas** (AC-WKT-14-08, -16, AC-WKT-15-05; DN4b-18). Cada lugar mide lo mismo (Q-59 sigue igual). La en curso se dibuja como **pulgar** (`ThumbOf`) por encima de sus vecinas, con su pista, su relleno y su brillo adentro: es la marca de "acá estás" y no rodea nada. La hueca lleva **dos rieles** de 1,5·s al 0,85, sin los costados, sobre su interior al 18 %. En CAP-22 (`Serie larga` en TV) el pulgar mide 4·s × 10·s = 11,1 px de ancho contra los 4,8 px de un tramo.
- **Con el tope del editor** (×999, Q-26, Q-92) la tira tiene tramos de menos de un píxel: la franja muestra cuánto se hizo, el pulgar y lo que falta (DV-40). Para que el costo no dependa de `n`, `PillShapes.StripRuns` junta los pasos contiguos con la misma pintura y dibuja **una columna de píxel** con el color promedio de los tramos que la cubren (pesado por lo que cubre cada uno: lo que haría el suavizado). Así la tira cuesta a lo sumo un rectángulo por columna (≈ 950 en TV) y no 1998 (QA-G4bT-11; desvío DT4b-21).
- Nunca se desplaza, nunca sale de la barra y nunca se encima con la fila de información: la fila de píldoras tiene su alto de 20, y el pulgar (10) entra en sus 8 de arriba y 4 de abajo.

### 2.3 El relleno continuo (`PillProgress`; AC-WKT-14-10, AC-WKT-13-14; DD4b-09)

```csharp
namespace RowMate.Core.Design
{
    // El relleno de la píldora en curso y el aro de la cuenta grande (ANIMATION_BIBLE §2.6, §7.1; 04b/00-deuda-diseno §4.5).
    // Lo alimenta la vista con cada estado del HUD y lo lee una vez por cuadro, con el reloj del MotionDriver (el del motor).
    public sealed class PillProgress
    {
        // Un estado del HUD: el paso, los segundos que contó el motor en él, su duración (null o ≤ 0: no se llena), si el reloj
        // corre (fase en marcha), el instante de ahora y si el estado es Instant (segundo plano, despertar, cuadro tarde).
        // "tick" = el instante en que llegó el último segundo, o el comienzo del tramo al reanudar (Q-58).
        // Primero avanza el relleno hasta `now` con el estado anterior (como StepPulse.Update): un estado nuevo nunca le roba el
        // tramo del cuadro, sea cual sea el orden en que la vista llama a OnState y a ValueAt.
        public void OnState(int stepIndex, int elapsedSeconds, int? durationSeconds, bool running, double now, bool instant);

        // objetivo = (transcurrido + clamp(ahora − tick; 0; 1)) / D;
        // relleno = max(anterior; min(objetivo; (transcurrido + 1) / D)), en [0; 1]:
        //   nunca pasa del valor del tick siguiente y, dentro de un paso, nunca baja (al reanudar espera al motor).
        // En pausa no avanza. En el cambio de paso vale transcurrido / D en el acto. Con Instant en el MISMO paso vale
        // max(anterior; transcurrido / D): el presente en el acto, sin retroceder (AC-WKT-14-10; QA-G4bT-04); el tick queda donde
        // estaba si no llegó un segundo nuevo (con el reloj despierto, ahora − tick sigue continuo después de despertar). Avanza
        // el estado: se llama una vez por cuadro con un `now` que no baja.
        public double ValueAt(double now);

        // Lo que falta del paso, en segundos visuales: D·(1 − relleno). El aro de la cuenta grande es RemainingAt / 3 (§3.5).
        public double RemainingAt(double now);

        public bool Fills { get; }
        public int StepIndex { get; }
    }
}
```

La tabla de 04b/00-deuda-diseno §4.5 (D = 30 s; ticks a los 1,000, 2,000 y 3,040 s; pausa a los 3,600, reanuda a los 5,000 y el tick siguiente a los 6,000; cuadros cada 1/60 s) sale de la regla: 0,500 · 1,000 · 1,500 · 3,000 (tope) · 3,000 · 3,260 · **3,560** (el cuadro de la pausa, que avanza antes de aplicarla) · 3,560 · 3,560 · 3,600 · 3,900 · 4,000 · 5,000 s (`<scratch>/it4b/arch-gate/check.py`). 04b/00-deuda-diseno dice 3,555: su último cuadro cae a los 3,595, que no está en la grilla de 1/60 s; la diferencia (0,005 s de 30) no se ve, y PPG fija la secuencia de cuadros de esta tabla. El motor no cambia: "el instante del tick" es el del cuadro en que llega el estado con el segundo nuevo (el motor publica en el mismo cuadro en que procesa el segundo; con `FakeScheduler`, en el instante exacto).

Los dos casos de `Instant` en el mismo paso (QA-G4bT-04), en PPG:

| Caso | Antes de `Instant` | Con la regla anterior ("`transcurrido / D` en el acto") | Con esta regla |
|---|---|---|---|
| Despertar en marcha (`Woke`): D = 100 s, el equipo duerme a los 62,4 s y el primer cuadro despierto trae `Instant` con 62 | 62,400 / 100 | 62,000 / 100 durante un cuadro, y vuelve a subir | 62,417 / 100 (el cuadro de la vuelta) y sigue: 62,433… |
| Pausa con segundo plano o despertar (la tabla de arriba): `Instant` con 3 a los 4,5 s | 3,560 / 30 | 3,000 / 30 hasta reanudar | 3,560 / 30; al reanudar a los 5,0 espera al motor y vuelve a subir desde los 5,56 |

### 2.4 La cuenta grande (D-ANI-05; AC-WKT-14-18)

```csharp
namespace RowMate.Core.Workout
{
    public static class BigCountdown
    {
        public const int FromSeconds = 3;

        // En marcha, en un paso por tiempo (también el último) con restante 3, 2 o 1: ese número, sin ceros ni dos puntos.
        // null en pausa (ahí va PAUSA, AC-WKT-13-12), por distancia o sin meta (RemainingInStep null), con 0 s o duración
        // negativa (restante 0), en libre (sin paso) y terminada. Sale del restante del motor (StepProgress, :101-104), así cambia
        // en la misma foto que la cuenta de la barra y el reloj de la banda.
        public static int? ValueOf(StepProgress? step, WorkoutPhase phase);
    }
}
```

`Estados` con `R-4`: de los 7 a los 10 s, de los 17 a los 20, de los 27 a los 30, de los 47 a los 50 y de los 57 a los 60. `Final por distancia`: de los 7 a los 10 s, y nunca en el paso de 100 m. Al reanudar vuelve si corresponde (la regla no tiene memoria). Si el usuario rechaza D-ANI-05 en el gate, la vista no la monta (`HudFeatures.BigCountdown`, una constante de Presentation) y la regla queda probada sin uso.

### 2.5 El sello (D-ANI-05; AC-WKT-09-08, -09; P-02, P-03)

```csharp
namespace RowMate.Core.Workout
{
    // El resultado de un paso cerrado con sus promedios (step_target_tracker.dart:30-62): los mismos que deciden el veredicto.
    public readonly struct StepOutcome
    {
        public int StepIndex { get; }
        public StepResult Result { get; }
        public int Samples { get; }            // lecturas remando (SPM > 0 o W > 0, :30-40)
        public double AverageWatts { get; }    // con Samples = 0, NaN
        public double AverageSpm { get; }
        public int SplitSamples { get; }       // lecturas con split > 0
        public double AverageSplit { get; }    // con SplitSamples = 0, NaN
    }

    // StepTargetTracker (C): Complete guarda además el StepOutcome; OutcomeOf(i) lo devuelve (null sin objetivo o sin
    // completar). Reset lo borra. La regla del veredicto no cambia (LT-39 a LT-44).

    public enum SealMetric { Watts, Spm, Split }
    public readonly struct SealFragment { public SealMetric Metric { get; } public string Average { get; } public string Target { get; } }

    public sealed class SealContent
    {
        public StepResult Verdict { get; }
        public IReadOnlyList<SealFragment> Fragments { get; }   // en el orden de la pastilla: vatios, SPM, split
        public int StepIndex { get; }
    }

    public static class StepSeal
    {
        public const double DurationSeconds = 2;

        // AC-WKT-09-08: hay sello si el paso tenía objetivos (StepTargetTracker.HasTargets) y se cerró SOLO; nunca el último
        // (queda en curso, Q-52, P-02), nunca un paso cortado con Terminar (no tiene resultado), nunca en libre.
        public static bool Applies(StepOutcome? outcome, int totalSteps);

        // AC-WKT-09-09: un fragmento por objetivo que define el paso (vatios MÍN., SPM, split; los vatios máx. no son objetivo,
        // :23-26). El promedio se redondea hacia el lado desfavorable, así el número nunca contradice el veredicto (P-03).
        public static SealContent For(StepOutcome outcome, IntervalStep step);

        // ⌊promedio⌋ entero; sin lecturas remando, "--".
        public static string WattsAverage(in StepOutcome outcome);
        // ⌊10·promedio⌋ / 10 con un decimal y punto (Fmt), corregido para que nunca pase del promedio: si la cuenta en coma
        // flotante dio un décimo de más (k / 10 > promedio), k − 1. Sin lecturas, "--".
        public static string SpmAverage(in StepOutcome outcome);
        // ⌈promedio⌉ s en MM:SS (TimeFormats.MinSec); sin split informado, "--:--".
        public static string SplitAverage(in StepOutcome outcome);
    }
}
```

- **El objetivo** va con el formato de la pastilla: `WorkoutFormats.TargetValue` (vatios mín. y SPM enteros) y `WorkoutFormats.SplitTargetValue` (`MM:SS`).
- **Por qué la corrección del SPM.** `⌊10x⌋` en coma flotante puede subir un décimo cuando `x` está a un ulp por debajo del entero (21,999999999999996 × 10 da 220,0): mostraría `22.0 / 22` con `No alcanzado`. Con la corrección el número nunca supera el promedio y, como el objetivo es entero, con `Met` nunca baja de él. Vatios (sin escala) y split (`⌈x⌉`) no tienen ese caso.
- **El texto** lo arma el *store* (§2.11): `hudSealMet` / `hudSealMissed` y cada fragmento con `hudSealWatts`, `hudSealSpm` o `hudSealSplit` (`{avg}`, `{target}`), unidos por ` · ` (U+00B7 con un espacio a cada lado). Los mismos textos en es y en.
- **El instante.** El motor publica el último paso cerrado con objetivos y el instante monótono del segundo que lo cerró (`WorkoutSnapshot.LastOutcome`, `.LastOutcomeAt`, §2.6). La vista lo muestra mientras `ahora − LastOutcomeAt < 2 s` (reloj del `MotionDriver`, que es el mismo): en pausa sigue sus 2 s, un sello nuevo reemplaza al visible y, al volver del segundo plano, uno cuyos 2 s ya pasaron no se muestra.

| Caso (de la spec) | Texto |
|---|---|
| `Estados`, paso 0 | `Objetivo cumplido` · `24.0 / 18 spm` |
| `Estados`, paso 1 | `No alcanzado` · `24.0 / 30 spm` |
| `Prueba 4a` con `R-4`, trabajo 1 | `No alcanzado` · `150 / 160 W · 24.0 / 22 spm` |
| `Pirámide 500` con `R-4`, primer trabajo (500 m) | `Objetivo cumplido` · `150 / 150 W · 02:05 / 02:05/500m` |
| `Pirámide 500`, trabajo de 250 m | `No alcanzado` · `24.0 / 30 spm` |
| Redondeo | 159,6 W / 160 → `159 / 160 W` (no alcanzado); 21,96 / 22 → `21.9 / 22 spm`; split 125,2 / 125 → `02:06 / 02:05/500m`; 124,6 / 125 → `02:05 / 02:05/500m` (cumplido) |
| `R-4P` todo el paso, vatios mín. 160 y SPM 22 | `No alcanzado` · `-- / 160 W · -- / 22 spm` |
| `Estados` a los 30 y a los 60 s | sin sello (descanso sin objetivo; el último) |

### 2.6 El fin de la rutina espera al diálogo (DT4a-15; AC-WKT-21-01, -04, -06 a -09, -13; AC-WKT-01-06, -03-05, -06-12, -20-07)

**Fases**, con lo que cambia respecto de 02-tech-design §2.1 de la 4a en negrita:

| Desde | Evento | Hacia | Qué pasa | Fuente |
|---|---|---|---|---|
| Active | último paso completo | Finished | en el mismo segundo; el reloj se detiene; el punto final, las estadísticas y el cierre (como en la 4a); **`EndedBy = Completed`, `Completion` = los totales del punto final, `Close = Pending`** | `workout_provider.dart:210-216` |
| Active o Paused | `FinishAsync()` | Finished | igual que en la 4a, con `EndedBy = Manual` y sin `Completion` | `:252-303` |
| Finished, a mano | el cierre volvió (o el tope de 15 s de primer plano) | Idle | **sin cambios**: vuelve solo a reposo y avisa `Ended(Manual, saved)` (AC-WKT-01-06) | el legacy hace `await finish(); reset()` (`immersive_workout_screen.dart:141-147`) |
| Finished, sola | el cierre volvió | **Finished** | **queda terminada**: `Close = Saved` o `Failed`, el reloj quieto, los resultados de los objetivos en su lugar, ningún punto más; las lecturas siguen llegando y publicándose (AC-WKT-21-08) | `immersive_workout_screen.dart:235`, `:255`, `:287` (el `reset()` lo hace el diálogo) |
| Finished, sola | **`AcknowledgeCompletion()`** (el `Terminar` del diálogo) | Idle | si el cierre ya volvió, en el acto: la vuelta a reposo de la 4a y `Ended(Completed, saved)`; si no, cuando vuelva o venza el tope | `:261-289` (DV-42: el legacy llama a `reset()` sin esperar) |
| Finished, sola | el tope de 15 s **de primer plano** desde el fin | Finished | `Close = Failed`; si ya se tocó `Terminar`, vuelve a reposo con `Ended(Completed, false)`. Si después el cierre vuelve bien y todavía no se tocó `Terminar`, `Close = Saved` y el final es el de un guardado bueno | DT4a-13; AC-WKT-21-06 |
| Finished | `StartFree`, `StartWithRoutine` | (igual) | `Busy` (una sesión a la vez) | AC-WKT-01-08 |
| Finished | `Pause`, `Resume`, `FinishAsync` | (igual) | nada (un `Terminar` del HUD que llega después del fin no cambia nada, AC-WKT-20-07) | — |

```csharp
namespace RowMate.Core.Workout
{
    public enum CloseStatus { None, Pending, Saved, Failed }

    // Los totales del diálogo de rutina completada (AC-WKT-21-04; DV-43): el reloj activo al terminar y la distancia y las
    // kcal del punto final (workout_provider.dart:259-273), fijos aunque lleguen lecturas o se rote la pantalla.
    public readonly struct CompletionSummary
    {
        public int ActiveSeconds { get; }
        public int DistanceMeters { get; }
        public int Calories { get; }
    }

    // WorkoutSnapshot (C): las propiedades nuevas son init, con valor por defecto (los que arman fotos en las pruebas no
    // cambian).
    public sealed record WorkoutSnapshot(/* los 8 posicionales de la 4a */)
    {
        public long Session { get; init; }                  // la generación de la sesión (sube en cada inicio; la conserva el reposo)
        public WorkoutEndKind? EndedBy { get; init; }       // con Phase == Finished
        public CompletionSummary? Completion { get; init; } // con EndedBy == Completed
        public CloseStatus Close { get; init; }
        public StepOutcome? LastOutcome { get; init; }      // el último paso que cerró SOLO con objetivos y no era el último
        public double LastOutcomeAt { get; init; }          // instante monótono (s) del segundo que lo cerró; NaN sin él
    }

    public sealed class WorkoutEngine
    {
        // "Terminar" del diálogo de rutina completada (AC-WKT-21-06, -08). Con la rutina terminada sola: vuelve a reposo en el
        // acto si el cierre ya volvió, o cuando vuelva (o venza su tope). La tarea se completa al volver a reposo. Idempotente;
        // fuera de ese estado, una tarea completa sin efecto.
        public Task AcknowledgeCompletion();
        // … lo de la 4a, sin cambios de firma.
    }
}
```

- **"Tocar rápido nunca pierde el cierre"** (DV-42): `Terminar` solo marca la intención; el cierre sigue siendo el de la 4a (una transacción, con los puntos de un lote fallado) y la vuelta a reposo espera su resultado. El aviso del final (`Ended(Completed, saved)`) sale recién al volver a reposo, con lo que se sabe en ese momento.
- **El tope**: el mismo `ForegroundTimeout` de 15 s de la 4a, armado al empezar el cierre. No cuenta el segundo plano (QA-G4aT-03) ni el equipo suspendido (mide con el reloj despierto, §2.9). Así `Corta` en segundo plano de los 8 a los 330 s cierra en el segundo 30 y, al volver, el diálogo está abierto y no hay un `workoutSaveError` falso (AC-WKT-02-06).
- **La app se cierra con el diálogo abierto**: `Dispose` hace lo de la 4a (`Abandon`, QA-V4a-ROB-04). Si el cierre ya volvió, la sesión quedó cerrada; si no, queda cortada (AC-WKT-06-10). Al abrir de nuevo no aparece nada (paridad).
- **El último paso cerrado con objetivos** (`LastOutcome`) se fija en `CheckStepCompletion` cuando el paso no es el último (antes de `i++`), con el instante del segundo; se borra al iniciar y al volver a reposo. El último paso y uno cortado con `Terminar` nunca lo fijan.
- **El oráculo** (AC-WKT-21-13): §2.13.

**La regla del diálogo** (App, pura):

```csharp
namespace RowMate.App.Workout
{
    public static class CompletionDialogRule
    {
        // AC-WKT-21-01, -07, AC-WKT-13-11: a lo sumo una vez por sesión, solo si la rutina terminó sola (EndedBy == Completed).
        // Nunca con EndedBy == Manual, en libre ni en reposo.
        public static bool ShouldOpen(WorkoutHudState state, long shownForSession);

        // AC-WKT-21-09: sin su entrada si el primer estado que lo pide es Instant (vuelta del segundo plano, despertar, cuadro
        // tarde): el diálogo ya está, con los totales finales.
        public static bool OpensInstantly(WorkoutHudState state);
    }
}
```

La página lo abre a los `CompletedDialogDelayMs` = 300 ms del primer estado `Finished` + `Completed` (un `Schedule` del programador: determinista en el arnés), sin esperar al guardado. "Una vez" es por `Session`: rotar, cambiar la ventana, minimizarla o pasar a segundo plano no lo duplican, y una sesión nueva muestra el suyo (§3.9). Si el `Schedule` vence **dentro del alcance** (la app pasó a segundo plano en esos 300 ms; `IScheduler.IsCatchingUp`), el diálogo se abre con `instantly`: al volver ya está, con el check dibujado y los totales finales (AC-WKT-21-09; QA-G4bT-08). El `IDisposable` del `Schedule` se dispone al empezar la salida y en `OnPopped`: nunca abre un diálogo sobre una página que se va.

### 2.7 El chip de fase (AC-WKT-16-03, -04; DV-41, DV-44)

```csharp
namespace RowMate.Core.Workout
{
    public readonly struct PhaseChipSpec
    {
        public string Key { get; }           // hudPhaseActive / hudPhasePaused / hudPhaseFinished
        public ColorToken Base { get; }      // phase.running / phase.paused / phase.finished: el tinte al 15 % y el borde al 50 %
        public ColorToken Text { get; }      // phase.running, phase.paused y, en FINALIZADO, phase.finished.hud (#FF7A92; DV-44, DN4b-15)
        public bool LiveDot { get; }         // solo en marcha (AC-WKT-16-05)
    }

    public static class PhaseChip
    {
        public const double PadH = 8, DotSlot = 10;   // 8 × 3 de relleno; el punto de 6 y su aire de 4, solo en EN MARCHA

        // immersive_workout_screen.dart:530-547. Reposo → null: el "—" del legacy (:534) nunca se ve, porque la salida conserva la
        // última foto (DV-41).
        public static PhaseChipSpec? For(WorkoutPhase phase);

        // El ancho fijo del chip en base (DT4b-11; DN4b-26): max(EN MARCHA + DotSlot; PAUSADO; FINALIZADO) + 2·PadH, con los
        // anchos de las tres etiquetas en el idioma de la app (11/700, tracking 0,08 em). es: 101,8; en: 74,1 (04-design-notes §3.5).
        public static double FixedWidthBase(double active, double paused, double finished);
    }
}
```

- `FINALIZADO` aparece mientras se guarda un libre terminado a mano (la fase es `Finished`): con la base lenta de CAP-15, durante los 2 s del cierre (AC-WKT-16-04). Los textos salen del catálogo con el español del legacy, que los tiene fijos (Q-01).
- **El color de `FINALIZADO`** es el token nuevo `phase.finished.hud` `#FF7A92` (rol `HudText`, 135 → 136 tokens): la variante legible de `#EF476F`, 4,677:1 sobre su tinte en el peor vidrio. `#FF5D7F` (`step.work.hud`) daba 3,942 (DV-44, spec §16.1). WP-A lo agrega a `Tokens.cs` y regenera `Tokens.uss` con `tools/tokens` (excepción de dueño, §7.6); PCH comprueba el par con `Contrast` (la pila B1 de las notas, ± 0,001), además del grupo de WP-D2.
- **El ancho fijo** lo mide `PhaseChipView` con `MeasureTextSize` (solo con panel) al cambiar el idioma, y lo aplica con `FixedWidthBase` × s. El contenido va centrado. El reloj nunca se corre cuando cambia la fase.

### 2.8 El reloj de pared (F-WKT-19; AC-WKT-19-01 a -05)

#### 2.8.1 La regla

```csharp
namespace RowMate.App.Workout
{
    // MinuteOfDay: hora·60 + minuto (para el sentido del rodillo). Instant: el cambio no fue "el minuto siguiente" (cambio de
    // zona o de hora a mano, despertar, vuelta del segundo plano, primer estado): se aplica sin rodillo.
    public sealed record WallClockState(string Text, int MinuteOfDay, bool Instant);

    // El reloj de pared, con su propio ritmo (series_panels.dart:57-82): no depende del reloj de la sesión ni lo mueve; sigue en
    // pausa, terminada y con el diálogo abierto; la hora es la del equipo (LocalNow), no la de la escena.
    public sealed class WallClockTicker : IDisposable
    {
        public const double CheckPeriodSeconds = 0.25;   // el legacy mira cada 1 s; acá cada 250 ms: el minuto cambia antes de 1 s
        public WallClockTicker(IScheduler scheduler, IAppLifecycle lifecycle, IAppLogger logger);
        public IReadOnlyObservable<WallClockState> State { get; }   // publica SOLO cuando cambia el HH:mm (no "late" cada segundo)
        public void Dispose();
    }
}
```

- **El formato** es `WorkoutFormats.WallClock(LocalNow)`: `HH:mm` de 24 h, siempre, aunque el sistema use 12 h (AC-WKT-11-12 de la 4a; Q-06): 09:05 → `09:05`; 00:00 → `00:00`.
- **Cuándo cambia.** Un periódico de 250 ms del programador compara la hora y el minuto de `LocalNow` con los últimos publicados (sin asignar). Con la zona de prueba (UTC−03:00), a los `13:00:59.5Z` dice `10:00` y a más tardar desde los `13:01:00.25Z`, `10:01` (la spec pide desde los `13:01:01.0Z`).
- **Cambio de hora del equipo** (AC-WKT-19-04): el periódico ve el salto (`10:00` → `08:00` con la zona pasada a UTC−05:00) y publica con `Instant`. El reloj de la sesión, la cuenta, los pasos y los puntos no cambian: cuentan con el monótono.
- **Segundo plano y alcance**: con `InBackground → true` dispone su periódico y con `InBackground → false` lo vuelve a armar, así **el alcance no lo dispara** (QA-G4bT-08: si no, 4 disparos por segundo virtual que no publican nada, 115 200 en 8 h). El orden de `AppRoot` lo garantiza: al irse, el ciclo de vida antes que el programador; al volver, el alcance antes que el ciclo de vida. Al volver y al despertar (`Woke`, §2.9) publica una vez, con `Instant` (AC-WKT-19-03).
- **Lo crea y lo dispone el *store* del HUD**, uno por inmersivo (§2.11).

#### 2.8.2 Dónde va (AC-WKT-14-14, -16-02, -19-05)

En la barra de pasos, a 10·s a la derecha de la cuenta, último de la fila de información; en la barra libre, después del espacio flexible y antes del chip. Sin marco (la variante con marco, `series_panels.dart:104-110`, no se migra), sin toques y sin foco. Su vista es un `TabularNumber` con el ícono de prefijo (§3.7).

#### 2.8.3 La zona horaria en la app (Platform)

`UnityScheduler` lee el offset cada **0,5 s** (hoy, cada 5 s) con `LocalOffsetSource`. En Windows y en el Editor usa `GetTimeZoneInformation` en un búfer reservado una vez (`Marshal.AllocHGlobal`): da el sesgo actual sin la caché de `TimeZoneInfo.Local`, que en .NET no ve un cambio de zona hasta `ClearCachedData`. En Android, `TimeZoneInfo.Local` (la 10a lo revisa). El cambio de hora a mano llega solo, porque `DateTimeOffset.UtcNow` se lee en cada cuadro. Con 0,5 s más el periódico de 250 ms, la hora nueva se ve antes de 1 s.

### 2.9 P-17 de la 4a: el tiempo con el equipo suspendido no cuenta (AC-WKT-02-11, -04)

#### 2.9.1 La idea: un reloj despierto

El legacy cuenta disparos de un `Timer.periodic(1 s)` (`workout_provider.dart:147`, `:247`) que no corre con el equipo dormido: el lapso no existe para él. La app cuenta tiempo monótono (`ActiveClock`, ADR 017), y el `Stopwatch` de Windows puede seguir contando la suspensión o no (no está medido, QA-V4a-ROB-05). En vez de enseñarle a cada parte qué es una suspensión, **el reloj de la app pasa a ser el despierto**: `FrameScheduler` le resta al monótono crudo lo dormido que el crudo contó. Como el motor, el remo simulado, los topes de espera (`ForegroundTimeout`), el reloj de pared (su periódico) y el `MotionDriver` miden con ese reloj, el lapso dormido no produce segundos, pasos, puntos, lecturas de objetivos ni demora del cierre, en los dos casos del monótono. La hora de pared (`UtcNow`, `LocalNow`) sigue siendo la real. Va como ADR 020 (corto), porque cambia el reloj de toda la app.

#### 2.9.2 Las firmas

```csharp
namespace RowMate.Core.Common
{
    [Flags] public enum SleepEvidence { None = 0, Signal = 1, Measure = 2, WallJump = 4 }

    // Un despertar: lo que durmió según el sistema (0 si solo llegó el aviso y no hay medida), lo que le restó el reloj de la
    // app y con qué evidencia.
    public readonly struct SystemWake
    {
        public double AsleepSeconds { get; }
        public double DiscountedSeconds { get; }
        public SleepEvidence Evidence { get; }
    }

    // Lo que el que avanza el programador le pasa en cada cuadro.
    public readonly struct FrameClock
    {
        public double RawMonotonic { get; }          // el Stopwatch
        public DateTimeOffset Utc { get; }
        public TimeSpan LocalOffset { get; }
        public double SystemAsleepSeconds { get; }   // lo dormido según el sistema desde el arranque; NaN sin medida
        public int SuspendSignals { get; }           // cuántos avisos de "se suspende" llegaron desde el arranque (PBT_APMSUSPEND)
        public int ResumeSignals { get; }            // cuántos de "reanuda" (PBT_APMRESUMEAUTOMATIC o PBT_APMRESUMESUSPEND)
    }

    // P-17 de la 4a (AC-WKT-02-11). Pura: la usa FrameScheduler (el mismo código en Unity y en dotnet).
    public sealed class SuspendAccounting
    {
        public const double MinSleepSeconds = 0.5;      // menos es ruido (los contadores del sistema van de a 15,6 ms)
        public const double CountedRatio = 0.9;         // el crudo "contó" el sueño si avanzó al menos el 90 % de lo dormido
        public const double SignalGapSeconds = 1;       // con un aviso pendiente, un cuadro de ≥ 1 s es sueño
        public const double SignalExpirySeconds = 30;   // un aviso sin sueño ni "reanuda" después (cancelado) se descarta
        public SuspendAccounting(in FrameClock first);
        public double Bias { get; }                     // segundos restados en total
        public bool SuspendPending { get; }             // llegó un "se suspende" y todavía no se cerró con un "reanuda"
        public double Awake(in FrameClock clock, out SystemWake? woke);   // el monótono despierto de este cuadro (o de la vuelta)
    }

    // FrameScheduler (C)
    //   public int Tick(in FrameClock clock);                   // la conducta de la 4a, con el monótono despierto
    //   public int ResumeFromBackground(in FrameClock clock);   // el alcance HASTA EL MONÓTONO DESPIERTO (QA-G4bT-05)
    //   public event Action<SystemWake>? Woke;                  // en el cuadro (o la vuelta) del despertar, DESPUÉS de correr lo vencido
    //   public double SleepBias { get; }
    //   Tick(double, DateTimeOffset, TimeSpan) y ResumeFromBackground(double, DateTimeOffset, TimeSpan) siguen: son las de
    //   FrameClock sin medida ni avisos (las pruebas de la 4a).
}
```

**El algoritmo**, por cuadro y en la vuelta del segundo plano (Δ = lo de este cuadro menos lo del anterior; el mismo `Awake` en los dos caminos):

1. **Con medida del sistema** (`ΔAsleep ≥ 0,5 s`): durmió `ΔAsleep`. Si `ΔRaw ≥ 0,9·ΔAsleep`, el crudo lo contó y se restan `min(ΔAsleep; ΔRaw)`; si no, no lo contó y no se resta nada. Es un despertar con `Measure` (y `Signal` si había un aviso pendiente). El aviso pendiente, si lo había, se cierra.
2. **Sin medida, con un "se suspende" pendiente** (todavía no llegó el "reanuda" que lo cierra):
   - **todo** cuadro con `ΔRaw ≥ 1 s` se descuenta como sueño (`ΔRaw` menos un cuadro nominal), y el primero avisa el despertar con `Signal`. El aviso **no se consume** con ese cuadro: un tirón entre el aviso y el sueño de verdad se descuenta (a lo sumo lo que duró, y queda en el registro), pero el sueño que viene después también (QA-G4bT-05 (b));
   - si en cambio salta la hora de pared (`ΔUtc − ΔRaw ≥ 0,5 s`) con un `ΔRaw` normal, el crudo no contó: no se resta nada, y es un despertar con `Signal | WallJump`;
   - **el aviso de "reanuda" ancla el despertar**: el primer cuadro en que `ResumeSignals` subió cierra el pendiente, después de aplicar lo de ese cuadro. Así da igual si el "reanuda" llega antes o después del primer cuadro despierto (los avisos corren en un hilo del sistema). Si ningún cuadro mostró el sueño, ese es el despertar (`Signal`, 0 descontado);
   - un "se suspende" sin sueño ni "reanuda" en 30 s despiertos se descarta y se anota.
3. **Sin medida ni aviso**: nada. Un cuadro tarde es un cuadro tarde (AC-WKT-02-04, la conducta de la 4a: se procesan los segundos, una sola foto `Instant`). Un salto de la hora de pared sin aviso es un cambio de hora (AC-WKT-19-04), no una suspensión.

El monótono despierto nunca baja. `FrameScheduler` corre lo vencido con él y, si hubo despertar, avisa `Woke`; `UnityScheduler` lo pasa a `AppLifecycle.SignalWake` y de ahí a `IAppLifecycle.Woke` (nuevo, `IEventSource<SystemWake>`; `ForegroundLifecycle` nunca avisa).

**La vuelta del segundo plano** (QA-G4bT-05 (a)): `AppRoot.OnApplicationPause(false)` llama a `UnityScheduler.ResumeFromBackground()`, que arma el `FrameClock` igual que en un cuadro y llama a `FrameScheduler.ResumeFromBackground(in FrameClock)`. El alcance va **hasta el monótono despierto**, no hasta el crudo: si una suspensión volviera por ese camino (no está medido qué hace el *player* de Windows; en macOS, 10b), el lapso dormido tampoco se procesa (25 puntos, no 145). `Woke` sale después del alcance.

#### 2.9.3 La señal en cada plataforma

| Plataforma | Señal | Qué pasa si no llega |
|---|---|---|
| Windows (build; en el Editor, solo la medida) | `WindowsSleepSource`. (a) **Los avisos**, solo en el build: `PowerRegisterSuspendResumeNotification(DEVICE_NOTIFY_CALLBACK)` de `powrprof.dll` (Windows 8+). Su *callback* corre en un hilo del sistema y solo hace `Interlocked.Increment` de dos contadores: "se suspende" (`PBT_APMSUSPEND` = 4) y "reanuda" (`PBT_APMRESUMEAUTOMATIC` = 18 o `PBT_APMRESUMESUSPEND` = 7). Nunca toca la app. Lleva `[AOT.MonoPInvokeCallback]` y el delegado queda en un campo estático. La estructura `DEVICE_NOTIFY_SUBSCRIBE_PARAMETERS` vive en `Marshal.AllocHGlobal` mientras dura el registro: `Dispose` da de baja (`PowerUnregisterSuspendResumeNotification`) y recién después la libera. **En el Editor no se registra** (una recarga de dominio dejaría al sistema llamando a código descargado; QA-G4bT-05 (c)): ahí vale solo la medida. (b) **La medida**: `GetTickCount64()` cuenta el sueño y `QueryUnbiasedInterruptTime()` no (Windows 7+), así que `(GetTickCount64·10⁴ − QueryUnbiasedInterruptTime) / 10⁷` sube solo mientras el equipo duerme (S3 y S4). Es una lectura: también en el Editor | Con S3 o S4 alcanza la medida. En el modo de espera moderno (S0 de bajo consumo) el núcleo puede no contarlo como sueño: quedan los avisos, que la documentación de Windows da también para esos equipos. **Si no llega ninguno, el lapso cuenta** como un cuadro tarde (la conducta de la 4a) y se anota `[TIME] cuadro de N s sin aviso de suspensión: cuenta (AC-WKT-02-04)` con N ≥ 30. En el Editor, el modo de espera moderno sin medida cuenta (DT4b-20). NO VERIFICABLE por QA: lo verifica el usuario en la demo (c), con el build |
| macOS | 10b (la misma forma: `NSWorkspaceWillSleepNotification` y un reloj que no cuente el sueño) | — |
| Android, iOS | `NullSleepSource`: la pantalla bloqueada es segundo plano y la sesión sigue (Q-91, P-12; AC-WKT-02-10). El reloj del teléfono con el sueño profundo es RA-19, de la 10a | — |

Al despertar con evidencia, el motor anota `[WORKOUT] el equipo durmió N s: no cuenta (P-17; aviso, medida)`: si la demo (c) falla en la PC del usuario, el registro dice qué faltó.

#### 2.9.4 Qué hace cada parte al despertar

| Parte | Qué hace | AC |
|---|---|---|
| El motor | sigue en la fase en que estaba; publica una foto con `Instant` (el primer cuadro ya muestra el presente); no avisa nada | AC-WKT-02-11, -02-08 |
| El remo simulado y los objetivos | su periódico sigue desde donde estaba: no hubo lecturas en el lapso; la primera que llega después la cuenta el saneamiento (un salto de más de 10 m en 1 s no suma, AC-WKT-03-03) | AC-WKT-02-11 |
| El cierre (`ForegroundTimeout`) | no contó el lapso: no hay `workoutSaveError` falso | AC-WKT-02-11 |
| El reloj de pared | publica la hora actual, con `Instant` | AC-WKT-19-03 |
| Los diálogos (terminar, completado) | siguen abiertos | AC-WKT-02-11 |
| El HUD | aplica la foto `Instant`: ni rodillos, ni destellos, ni cuenta grande ni sello de lo que no pasó | AC-WKT-02-08 |

Con el ciclo de vida de prueba (`LifecycleHarness.Suspend(62,4)` y `Wake(600, monotonicCounts)`), en un libre con `R-4`, en los dos casos del monótono y con aviso, con medida o con los dos: al despertar el reloj dice `01:02`; los puntos son los de los segundos en marcha 5 a 60 y después 65, 70…; terminada a los 120 s en marcha, la sesión tiene **25 puntos** (24 + el final). Sin ninguna señal y con un crudo que contó, 145 (la conducta de la 4a, que la prueba deja escrita). Además (QA-G4bT-05): con el "reanuda" antes y después del primer cuadro despierto, 25; con un tirón de 1,2 s entre el aviso y el sueño, 25 y el tirón descontado en el registro; con la suspensión durante el segundo plano y la vuelta por `ResumeFromBackground`, 25.

### 2.10 Barras del sistema: la regla (AC-APP-15-01, -04)

Ver §4.1: la regla es de App y se prueba en dotnet.

### 2.11 El estado del HUD y los *stores*

```csharp
namespace RowMate.App.Workout
{
    // La fila de píldoras: los tipos de la rutina expandida (el mismo objeto toda la sesión) y los aspectos (el mismo objeto
    // mientras no cambian el paso ni un resultado: rehacerlo cuesta O(n) solo ahí).
    public sealed record HudPillRow(IReadOnlyList<StepType> Types, IReadOnlyList<StepPillLook> Looks, int Current,
                                    int Elapsed, int? Duration, bool Running, bool EndingSoon);

    // El sello del último paso que cerró solo con objetivos (§2.5): Title = hudSealMet / hudSealMissed; Detail = los
    // fragmentos unidos por " · "; At = el instante monótono del cierre (la vista decide si está en sus 2 s).
    public sealed record HudSeal(int StepIndex, StepResult Verdict, string Title, string Detail, double At);

    public sealed record HudPhaseChip(string Text, ColorToken Base, ColorToken TextColor, bool LiveDot);

    // Los totales del diálogo con el formato del legacy (immersive_workout_screen.dart:965-991): SessionClock (MM:SS o
    // H:MM:SS), FinalDistance ("{n} m", sin miles) y FinalCalories.
    public sealed record HudCompletion(string Time, int TimeValue, string Distance, int DistanceValue, string Calories, int CaloriesValue);

    // HudTargetLabel (C, de la 4a): + init Fragments, los fragmentos de la pastilla en su orden (AC-WKT-14-03; DN4b-16): cada parte
    // separada por " · " (U+00B7 con un espacio a cada lado); la tendencia con su texto ("📈 SPM +2/rep") es UNO, con IsTrend.
    public sealed record HudTargetFragment(string Text, bool IsTrend);

    // WorkoutHudState (C): + init
    //   long Session; WorkoutEndKind? EndedBy; HudPillRow? Pills; int? BigCount; HudSeal? Seal; HudPhaseChip? Chip;
    //   HudCompletion? Completion; bool CloseReturned (el cierre volvió: Terminar del diálogo vuelve a reposo en el acto)

    // WorkoutHudStore (C)
    //   WorkoutHudStore(WorkoutEngine engine, IReadOnlyObservable<Localizer> texts, IScheduler scheduler, IAppLifecycle lifecycle,
    //                   IAppLogger logger)
    //   IReadOnlyObservable<WallClockState> WallClock { get; }   // el WallClockTicker de este inmersivo
    //   Task AcknowledgeCompletionAsync();                        // → engine.AcknowledgeCompletion()
}
```

| Campo nuevo | Regla | Fuente |
|---|---|---|
| `Pills` | con rutina: `Types` = los tipos de `engine.Steps` (una vez por sesión); `Looks[i]` = `StepPills.LookOf(i, Step.StepIndex, Targets.ResultOf(i))`; `Elapsed`; `Duration` (null si `!StepPills.Fills`); `Running` (= `Active`); `EndingSoon` | §2.1; `immersive_workout_screen.dart:363-375` |
| `BigCount` | `BigCountdown.ValueOf(Step, Phase)` | §2.4 |
| `Seal` | `StepSeal.Applies(LastOutcome, TotalSteps)` → el texto de §2.5, con `LastOutcomeAt` | §2.5 |
| `Chip` | en libre, `PhaseChip.For(Phase)` con el texto del catálogo; en reposo, null | §2.7 |
| `Completion` | con `EndedBy == Completed`, los textos de `Completion` | §2.6 |
| `CloseReturned` | `Close` es `Saved` o `Failed` | §2.6 |
| `HudTargetLabel.Fragments` | `WorkoutFormats.TargetLabel` partido por ` · `, con la tendencia como un fragmento; se arma una vez por paso (en el cambio de paso, no por cuadro) | §2.15; `immersive_workout_screen.dart:394-416` (el legacy corta por carácter: DV-45) |

- **Sale el aviso `workoutCompleted`** (AC-WKT-03-05): `WorkoutToast.For(Ended(Completed, true))` devuelve null; `Ended(_, false)` sigue dando `workoutSaveError` (AC-WKT-06-12, después del diálogo).
- **Con el diálogo de completado abierto no hay avisos** (AC-APP-03-11, AC-WKT-02-08): `WorkoutStore` retiene el último (como sin el usuario delante) mientras la foto del motor está en `Finished` con `EndedBy == Completed`, y lo emite al volver a reposo, una vez. El `workoutSaveError` del final sale en ese mismo momento. **Cuándo se ve** es de la presentación: si un inmersivo está saliendo, `WorkoutScreenView` lo muestra con el `RevealStarted` de la página, a los 400 de la salida (DN4b-24, §3.3); el *store* no cambia.
- **0 B por cuadro** sigue valiendo: el *store* arma estados a 1-3 Hz (los eventos de 1 Hz pueden reservar poco, ARCHITECTURE §20.2), y la fila de 200 píldoras rehace su lista de aspectos solo en el cambio de paso.

### 2.12 Catálogo: 214 claves (AC-APP-10-11)

Las 12 de spec §11.2 con sus parámetros: `hudPhaseActive`, `hudPhasePaused`, `hudPhaseFinished`, `hudPausedLabel`, `hudSealMet`, `hudSealMissed`, `hudSealWatts`, `hudSealSpm`, `hudSealSplit` (`avg` y `target` como texto), `workoutCompletedTime`, `workoutCompletedDistance`, `workoutCompletedCalories`. `L10nKeys` las suma; `CatalogParityTests` y `L10nKeysTests` pasan de 202 a 214, y ninguna queda en un solo idioma. Se reusan `workoutCompleted` (ahora el título del diálogo), `workoutCompletedMessage`, `workoutFinish`, `stepType*`, `hudFreeTitle`, `hudUpNext`, `workoutSaveError`, `stepDurationMeters` y `stepDurationNone`. Ningún texto visible nuevo queda fuera del catálogo, salvo los formatos (números, `:`, `.`, `--`, `--:--`, `?`, `·` y los dígitos de la cuenta grande).

### 2.13 El oráculo: el fin automático como en el legacy (AC-WKT-21-13)

- **`WorkoutGoldenTests.ScriptsAreIdenticalToTheLegacy`** (C): la acción `reset` de los guiones pasa a ser `AcknowledgeCompletion()` cuando el motor está en `Finished` por la rutina, así se comparan también el reloj y la fase después del `reset` y lo que pasa entre el fin y el `reset` (los cambios de paso de la grilla de 100 ms y los puntos: ninguno). Queda afuera solo la fase después de un `finish` **a mano** (el motor vuelve solo a reposo cuando vuelve el cierre, AC-WKT-01-06; en los guiones el `reset` de un terminar a mano va en el mismo instante, así que el estado después del `reset` es el mismo), más DV-37 (`neg_step`). Corren los 15 guiones de QA-V4a-L02 y los 11 escenarios de la 4a.
- **Un guion nuevo**, `completed_hold_reset`: `Corta` con `R-4`, la rutina termina sola a los 30 s, un bpm a los 32 s y el `reset` a los 34 s. Entre los 30 y los 34 s la fase es `finished`, el reloj queda en 30, no hay puntos y los resultados de los objetivos siguen. Lo agrega `tools/oracle/engine/test/engine_vectors_test.dart`, y `workout_engine.json` se regenera con `powershell -NoProfile -File tools/oracle/run-oracle.ps1 -Engine` (con `<flutter-sdk>`, `pub get --offline` y la guardia del legacy). Si Flutter no está, ese guion queda como `[Ignore("NO VERIFICABLE: sin Flutter")]` y los 15 de antes cubren el AC.

### 2.14 Composición

```csharp
public sealed record AppServices(/* los 9 posicionales de la 4a */)
{
    public IClock? CreationClock { get; init; }
    public IAppLifecycle? Lifecycle { get; init; }
    public ISystemUi? SystemUi { get; init; }      // It. 4b: null = sin efecto (dotnet, pruebas que no lo miran)
}

public sealed class AppContext
{
    public ImmersiveSystemBars SystemBars { get; }   // una por app: la usa cada inmersivo (§4.1)
    public WorkoutHudStore CreateHud();              // + el programador y el ciclo de vida (el reloj de pared)
}
```

`AppRoot` pasa `PlatformServices.SystemUi` y arma `UnityScheduler` con `PlatformServices.SleepSource`; `UiTestHost`, un `FakeSystemUi` por host. `AppContext.Dispose` dispone `SystemBars`, que pide mostrar las barras si quedaron ocultas.

### 2.15 Qué cede en la fila de información y en la barra libre (AC-WKT-14-03, -04, -14, -15, -16-06; DV-45, DV-47; DN4b-16, -27)

La regla del Designer (04-design-notes §3.3, §3.5), que el PO tomó en el gate (spec §16.1, camino (a) de QA-G4b-P01). Reemplaza a DT4b-12. Es pura: la vista mide los anchos (WP-B1, `MeasureTextSize`, solo con panel) y la regla decide; dotnet la prueba con los anchos de las notas.

```csharp
namespace RowMate.Core.Design
{
    // Los tres modos de la fila (04-design-notes §3.3): normal 6 · 8 · 10 con Próximo 7 × 3; compacto 1 con el aire antes de
    // la cuenta y el de la cuenta al reloj en 6; compacto 2, además, Próximo con 4 × 3.
    public enum InfoRowMode { Normal, Compact1, Compact2 }

    public readonly struct InfoRowPlan
    {
        public InfoRowMode Mode { get; }
        public double CountdownGap { get; }      // el aire antes de la cuenta (a Próximo o a la pastilla): 8 → 6 (:452)
        public double ClockGap { get; }          // de la cuenta al reloj: 10 → 6 (:473)
        public double UpNextPadH { get; }        // el relleno de Próximo por lado: 7 → 4
        public int FragmentsAlone { get; }       // los fragmentos de la pastilla mientras no hay Próximo (0 = sin pastilla)
        public int FragmentsWithUpNext { get; }  // con Próximo; 0 con FragmentsAlone > 0: la pastilla funde y se va al entrar Próximo
    }

    public static class InfoRowFit
    {
        public const double TypeGap = 6, CountdownGap = 8, ClockGap = 10, CompactGap = 6;   // :395, :452, :473; DN4b-16
        public const double UpNextPadH = 7, UpNextCompactPadH = 4, FitMarginDp = 1;         // 1 de margen por el redondeo a píxeles
        public const double FreeTitleMinScale = 0.8;                                        // DN4b-27 (spec AC-WKT-16-06)

        // Se decide al empezar el paso y cuando cambian la ventana, el idioma o la letra (nunca dentro del paso). Todo en dp base.
        // available: el ancho útil de la fila (la barra − el borde − 12 de cada lado: TV 339,9). upNextWidth: Próximo con su tipo
        // y el relleno normal (0 si el paso no lo va a tener: sin paso siguiente o sin restante por tiempo, AC-WKT-04-05).
        // countdownWidth: la cuenta como se ve en los últimos 15 s (M:SS). targetPrefix[k]: la pastilla con sus k + 1 primeros
        // fragmentos, con el "…" si queda alguno afuera, y su relleno de 6 × 2 (los arma la vista una vez por paso; sin asignar).
        //   1. el modo es el primero en el que entra todo con la pastilla en su mínimo (el primer fragmento, o entera si tiene
        //      uno), junto a Próximo si el paso lo va a tener; si en ninguno entra, el primero en el que entra el resto;
        //   2. con ese modo, cuántos fragmentos ENTEROS entran sin Próximo y con Próximo.
        public static InfoRowPlan Plan(double available, double typeWidth, double upNextWidth, double countdownWidth,
                                       double clockWidth, double[] targetPrefix, int fragmentCount);

        // La barra libre (AC-WKT-16-06): la escala pareja del título para que entre, en [0,8; 1]; con menos de 0,8 queda en 0,8
        // y el título se recorta con "…". TV y 360 en español: 127,0 / 155,4 = 0,818 (04-design-notes §3.5).
        public static double FreeTitleScale(double available, double titleWidth, double othersWidth);
    }
}
```

| Caso (TV, anchos de 04-design-notes §3.3) | Modo | Fragmentos sin `Próximo` · con `Próximo` | Lo que se ve |
|---|---|---|---|
| CAP-01: `Estados` a los 40,2 s, `18 spm` (un fragmento) | compacto 1 (en normal sobran 0,2 y la regla pide 1 de margen) | 1 · 1 | `18 spm` entera; sobran 6,1 |
| CAP-31: `Texto largo` a los 2,2 s, es (tres fragmentos) | compacto 2 (la pastilla no entra junto a `Próximo` en ningún modo, y el resto entra recién en compacto 2) | 2 · 0 | `>150W · 24 spm…` (114,7 de lugar; dos fragmentos miden 108,4 y los tres, 173,9) |
| CAP-19: el mismo paso a los 8,2 s | compacto 2 | — · 0 | sin pastilla: se fue con un fundido de 120 al entrar `Próximo` |
| CAP-20: el mismo, en | normal (`>150W…` pide 59,8 y quedan 44,7 con `Up next`) | los que entren · 0 | sin pastilla con `Up next` |
| `Prueba 4a`, un trabajo en sus últimos 15 s (AUX-01 de las notas) | compacto 2 | los que entren · 1 | `>160W…` con el "…" blanco (60,1 de 60,4: el margen más justo, que WP-B1 vuelve a medir) |
| TH, L640 y W1280 | normal | todos · todos | todo entero |

- **Los fragmentos** son los de `HudTargetLabel.Fragments` (§2.11): cada parte separada por ` · `; la tendencia con su texto es uno solo. Nunca se corta un número ni una palabra (`>15…` diría "más de 15"). El "…" va pegado y en `text.primary` (DD4b-03), aunque el último fragmento sea la tendencia en cian.
- **Nada salta.** El modo vale para todo el paso: cuando entra `Próximo`, lo único que cambia es la pastilla, que pasa en el acto a `FragmentsWithUpNext` o, con 0, funde a 0 en 120 (`ease.exit`). Va en un *slot* que recorta con `flex-shrink: 1`, así, si `Próximo` crece (su resorte de la 4a) antes de que termine de irse, el *layout* la angosta y nunca se encima; al terminar el fundido sale del árbol con su aire de 6. La cuenta y el reloj quedan pegados al borde derecho (AC-WKT-14-14, DV-46). Si el modo cambia de un paso al siguiente, los aires y el relleno pasan al nuevo en 0-240 (`ease.move`) con el cambio de paso (`InfoRowMotion`, §7.4); con reducir movimiento, en el acto.
- **Lo que nunca se corta**: el tipo, `Próximo:` y su tipo, la cuenta y el reloj. Si ni en compacto 2 entra el resto (no pasa con los textos del catálogo), el plan queda en compacto 2, la vista lo anota una vez en el registro y `HudOverlap` lo marca en las tomas.
- **El título libre** se achica en el acto (escala pareja desde su borde izquierdo, nunca *layout* por cuadro) al cambiar la ventana o el idioma; el ícono, el reloj y el chip no cambian (`FreeTopBar`, §3.7).
- **El legacy** reparte de otra forma (DV-45): la pastilla es un `Flexible` que recibe a lo sumo la mitad del espacio libre (`immersive_workout_screen.dart:394-416`), se corta por carácter y nunca se oculta.

---

## 3. Motion

Todo por código con el reloj del `MotionDriver` (el monótono despierto del programador; determinista en el arnés), nada por transiciones de USS salvo *hover* y presionado (ANIMATION_BIBLE §0.1, principio 7). Las **líneas de tiempo son puras** (WP-A, `Core/Design/HudTimelines.cs`, probadas en dotnet contra las tablas); los `IMotion` de WP-D1 solo las evalúan cada cuadro y le pasan los valores a la vista (§7.4).

### 3.0 La regla de oro, en cada pieza (ANIMATION_BIBLE §0.1; AC-WKT-13-14)

"La animación nunca adelanta, demora ni frena el reloj ni las métricas": el estado cambia en el instante del dato y se anima solo cómo se ve.

| Animación | De qué dato sale | Por qué no adelanta ni frena |
|---|---|---|
| Entrada "sumergirse" (§3.2) | la sesión, que corre desde el toque (AC-WKT-13-01) | cambian la opacidad y el `translate` de piezas que ya muestran el valor del momento; la banda entra con el reloj real |
| Salida (§3.3) | el último estado antes del reposo | la vista deja de aplicar estados (la última foto); volver a reposo es interno y no espera a la salida |
| Relleno continuo (§2.3, §3.4) | el tick del motor y el tiempo desde ese tick | `PillProgress` nunca pasa de `(transcurrido + 1) / D`; en pausa queda quieto |
| Halo de los últimos 5 s (§3.4) | `IsEndingSoon` del motor | empieza y termina en el tick que lo cambia (la rampa es de aspecto) |
| Cambio de paso de las píldoras y el pulgar que pasa (§3.4) | el paso nuevo, que ya está en el estado | todo empieza en el cuadro del cambio, después del dato |
| La fila de información: el cambio de modo y la pastilla que se va (§2.15) | el paso nuevo (el modo) y `Próximo` (la pastilla) | el plan se decide en el cuadro del cambio de paso; la pastilla funde cuando `Próximo` ya está en el estado |
| Cuenta grande (§3.5) | `BigCountdown.ValueOf` | aparece en el tick del 3 y se va en el del cambio; la cifra cambia en la misma foto que la cuenta y la banda; el aro usa `PillProgress` |
| Un disco por vez: la cuenta grande y `PAUSA` (§3.5) | la fase | `Pausa` y `Reanudar` actúan en el acto; solo el disco que entra espera 120 a que se vaya el otro, y la cifra que entra es la del momento |
| Sello (§3.6) | `LastOutcome` y su instante, que calcula el motor al cerrar el paso | entra después del cambio que anuncia; dura 2 s del reloj de la interfaz |
| Diálogo de completado (§3.9) | `EndedBy == Completed` | entra a los 300 ms con la sesión ya terminada; los totales cuentan hasta el valor exacto (los únicos números que cuentan, ANIMATION_BIBLE §2.5) |
| Velo, `PAUSA` y chip (§3.7) | la fase | `Pausa` y `Reanudar` actúan en el acto (AC-WKT-20-02); el chip cambia de texto en el cuadro de la fase |
| Reloj de pared (§3.7) | `WallClockTicker` | el texto nuevo está desde el cuadro del cambio de minuto |
| kcal (§3.8) | el pulso fusionado | FLIP: el *layout* cambia en el acto y se anima solo el `translate` |
| Volver del segundo plano o despertar | la foto con `Instant` | todo a su estado final en el acto (04a/04-design-notes §5.10) |

La prueba: `PresentationIndependenceTests` (dotnet: la secuencia del motor con y sin *store* del HUD es idéntica) e `ImmersiveHudTests.PresentationNeverTouchesTheData` (PlayMode: con animaciones, con reducir movimiento y sin HUD; el reloj de la banda, la cuenta y la cuenta grande cambian en el mismo cuadro que el segundo del motor; el relleno nunca pasa del tick siguiente) (§6).

### 3.1 Líneas de tiempo puras y constantes

`Core/Design/HudTimelines.cs` (WP-A). Cada función recibe los **ms enteros** desde el comienzo (`long`), si se reduce el movimiento y, cuando corresponde, desde dónde arranca, y devuelve un *struct*: ninguna asigna memoria. Los `IMotion` pasan de segundos a ms una sola vez, con `HudTimelines.Ms(now − start)` = `(long)Math.Round((now − start)·1000)`: así `30.2 − 30.0 = 0.1999999999999993` s da 200 ms, y `HudTimelines.Reached(tMs, endMs)` (= `tMs ≥ endMs`) no depende de la coma flotante (QA-G4bT-07 (b)).

| Línea de tiempo | Devuelve | Para |
|---|---|---|
| `ImmersiveTimeline.Entry(piece, t, reduced)`, `.EntryPage(t, reduced)`, `.EntryBackdrop(t, reduced)`, `.EntryInteractive(piece, t, reduced)`, `.EntryEndMs(reduced)` | `PieceVisual` (opacidad, subida en dp base), opacidades | §3.2 |
| `ImmersiveTimeline.Exit(t, from, reduced)`, `.ExitEndMs(reduced)`, `.RevealStartMs(reduced)` | `ExitVisual` (HUD, fondo, página) y cuándo empieza a verse Workout (los avisos del final, DN4b-24) | §3.3 |
| `StepBarTimeline.PillChange(t, met, reduced)`, `.HaloRamp(t, rising)`, `.Halo(ramp, p, reduced)`, **`.ThumbSwap(t, reduced)`** | `PillChangeVisual` (relleno que faltaba, destello, opacidad de la sólida, el brillo que se va, interior y trazo de la hueca, pista de la nueva, rampa del halo de la que termina); `ThumbVisual` (alto y opacidad del pulgar que se va y del que llega) | §3.4 |
| **`InfoRowTimeline.ModeChange(t, from, to, reduced)`**, **`.TargetOut(t, reduced)`** | `InfoRowVisual` (los dos aires y el relleno de `Próximo`); la opacidad de la pastilla que se va | §2.15 |
| **`CenterDiscTimeline.CountIn(t, reduced)`**, `.CountOut(t, reduced)`, **`.PausedIn(t, reduced)`**, **`.PausedOut(t, reduced)`**, **`.Swap(t, toPaused, reduced)`** | `DiscVisual` (opacidad y escala del disco que se ve; en el cambio, de los dos) | §3.5 |
| `SealTimeline.At(t, reduced)`, `.Swap(t, reduced)` | `SealVisual` (alto de la barra, opacidad del contenido, escala del ícono) | §3.6 |
| `PauseTimeline.Veil(t, pausing, from, reduced)` | el alfa del velo | §3.7 |
| `PhaseChipTimeline.At(t)`, `LiveDot.At(t, reduced)` | `ChipVisual` (mezcla de color, opacidades de las etiquetas), la opacidad del punto | §3.7 |
| `KcalSlideTimeline.At(t, appearing, reduced)` | `KcalVisual` (desplazamiento de kcal, opacidad de BPM) | §3.8 |
| `CompletionTimeline.At(t, reduced)`, `.EndMs(reduced)` y `CountUp.ValueAt(final, t, start)` | `CompletionVisual` (escala y trazo del check, fracción de cada total) | §3.9 |

En negrita, las cuatro que suman las 04-design-notes (QA-G4bT-01): el pulgar que pasa (§4.5 de las notas), el cambio de modo de la fila y la pastilla que funde (§3.3), y un disco por vez (§4.8), que junta la cuenta grande y `PAUSA` en una sola línea de tiempo y un solo `IMotion` (`CenterDiscMotion`). Cada una tiene su variante de reducir movimiento (§3.10) y su `IMotion` con sus fases finitas (§7.4).

**Constantes nuevas de `MotionSpecs`** (ms salvo que se diga; curvas con nombre de token). Son **los números de la spec**, que el Designer confirmó (DN4b-01 a -03); las alternativas de 04b/00-deuda-diseno quedan retiradas (§8.2) y salen de la tabla. Con **[notas]**, lo que suman o cambian las 04-design-notes (§4.15; Anexo B).

| Constante | Valor | Curva |
|---|---|---|
| `ImmersiveBlackInMs` | 220 (Workout funde a negro) | `ease.enter` |
| `ImmersiveBackdropInStartMs` / `EndMs` | 220 / 400 ("aparece el fondo") | `ease.enter` |
| `ImmersiveCascadeStartsMs` (barra, SPM, Split y Watts, banda, kcal y BPM, controles) | 400, 520, 600, 680, 760, 840 | — |
| `ImmersivePieceInMs` / `ImmersivePieceShiftDp` | 280 / 8 (`shift.s`; la spec pide ≤ 12) | `ease.enter` |
| `ImmersiveEntryEndMs` (la llave entra) | 1120 | — |
| `ImmersiveHudOutMs` | 200 (el HUD se va junto) | `ease.exit` |
| `ImmersiveBlackOutStartMs` / `EndMs` | 200 / 400 (el fondo funde a negro) | `ease.exit` |
| `ImmersiveRevealStartMs` / `ImmersiveRevealMs` | 400 / 240 ("vuelve Workout"; total 640 < 1 s) | `ease.enter` |
| `EndToastStartMs` **[notas]** | 400 (= `ImmersiveRevealStartMs`; con reducir movimiento, 0): los avisos del final (DN4b-24) | — |
| `ReducedImmersiveFadeMs` | 120 (la de la 4a, lineal) | `ease.linear` |
| `PillFillCompleteMs` | 120 | `ease.move` |
| `PillSolidifyStartMs` / `EndMs` / `PillFlashPeak` | 120 / 340 / 0,40 (sin destello con reducir movimiento: QA-G4bT-03) | `ease.move`; destello `0,40·(1 − ease.enter)` |
| `PillMissedStartMs` / `EndMs` | 120 / 440 | `ease.move` |
| `PillTrackInMs` | 150 | `ease.move` |
| `PillGlowMin` / `PillGlowMax` / `PillGlowRampMs` | 0,30 / 0,70 / 150 | `ease.move` (rampa) |
| `PillGlowEdgeFactor` **[notas]** | 0,65 (lo que se ve en el borde de la píldora: Φ(2 / 5,12) del `BoxShadow` del legacy) | — |
| `PillGlowExtentSideDp` / `TopDp` / `BottomDp` **[notas]** | 10 / 8 / 4, × s, con el perfil `(1 − d/E)²` | — |
| `PillGlowBlurDp` / `SpreadDp` / `RadiusDp` | 8 / 2 / 5 (`step_pill.dart:88-94`): referencia del legacy, para la prueba del 0,65 | — |
| `PillHighlightAlpha` / `PillHighlightDp` **[notas]** | 0,25 / 1 (el brillo: **sí**, ya no es opcional); se va en 120-340 con la sólida | `ease.move` |
| `PillThumbSwapMs` **[notas]** | 150: el pulgar que se va baja a la franja y se apaga mientras el nuevo sube | `ease.move` |
| `ReducedPillBorderFadeMs` / `PillRepaintPx` | 160 / 0,25 | — |
| `InfoRowModeChangeMs` / `TargetPillOutMs` **[notas]** | 240 (`ease.move`, con el cambio de paso) / 120 (`ease.exit`) | — |
| `BigCountInMs` / `BigCountInScaleFrom` | 200 / 0,85 | `ease.enter` |
| `BigCountOutMs` / `BigCountOutScaleTo` | 200 / 1,10 | opacidad `ease.exit`, escala `ease.enter` |
| `ReducedBigCountInMs` / `OutMs` | 160 / 120 | — |
| `CalloutSwapOutMs` / `CalloutSwapInStartMs` **[notas]** | 120 / 120: un disco por vez; el que sale funde sin escala y el que entra empieza a los 120 con su entrada | `ease.exit` |
| `BigCountRingClockwise` **[notas]** | `true`: el borde que se mueve avanza en el sentido del reloj y el otro queda en las 12 (DN4b-20) | — |
| `SealDurationMs` | 2000 | — |
| `SealGrowMs` (52 → 74) | 240 | `ease.move` |
| `SealContentInStartMs` / `EndMs` | 40 / 240 | `ease.enter` |
| `SealIconScaleFrom` / `SealIconStartMs` / `EndMs` | 0,6 / 60 / 300 | `ease.emphasis` |
| `SealContentOutStartMs` / `EndMs` | 1800 / 1920 | `ease.exit` |
| `SealCollapseStartMs` / `EndMs` | 1800 / 2000 | `ease.move` |
| `SealSwapOutMs` / `SealSwapInEndMs` | 80 / 200 (el ícono se estampa otra vez en 80-320) | — |
| `CompletedDialogDelayMs` | 300 | — |
| `CompletedCheckScaleFrom` / `StartMs` / `EndMs` (desde que entra el diálogo) | 0,6 / 60 / 300 | `ease.emphasis` |
| `CompletedCheckStrokeStartMs` / `EndMs` | 180 / 500 | `ease.move` |
| `TotalsCountMs` / `TotalsStaggerMs` / `TotalsStartMs` | 700 / 120 / 220 (el conteo termina a los 1160 del diálogo, 1460 del fin) | `ease.enter` |
| `PauseVeilAlpha` / `PauseVeilInMs` / `OutMs` | 0,15 / 300 / 240 | `ease.move` |
| `PausedLabelInMs` / `PausedLabelScaleFrom` / `PausedLabelOutMs` / `PausedLabelOutScaleTo` **[notas]** | 240 / 0,96 / 240 / 0,96 | `ease.enter` / `ease.exit` |
| `PhaseChipColorMs` | 150 (= `HudColorMs`) | `ease.move` |
| `LiveDotHalfMs` / `LiveDotMinOpacity` | 800 / 0,35; **[notas]** empieza en 1 (bajando) cada vez que el chip pasa a `EN MARCHA` | `ease.pulse` |

Se reusan sin cambios `DigitRoll.DurationMs` (180), `ReducedFadeMs` (80), `HudColorMs`, `StepPulseHalfMs`, `UpNextMinOpacity`, los de BPM y kcal (`BpmMoveMs`…), `ToastTextOutMs` / `ToastTextInEndMs` (el cambio de etiqueta del chip, 0-80 y 80-200), `FabInScaleFrom` (la llave) y los del diálogo de la It. 3 (`DialogInMs`, `DialogOutMs`…).

**Constantes nuevas de `HudGeometry`** (dp base): `TopBarHeightSteps` 52, `SealRowGap` 5, `SealRowHeight` 17, `SealRowPadH` 12, `TopBarHeightSeal` 74, `TopBarHeightFree` 40 (la de la 4a), `InfoRowPadBottom` 8 (`immersive_workout_screen.dart:381`), `WallClockGap` 10, `WallClockIcon` 16, `WallClockIconGap` 5, `WallClockIconAlpha` 0,70, `PhaseChipGap` 10, `PhaseChipPadH` 8, `PhaseChipPadV` 3, `PhaseChipRadius` 8, `PhaseChipText` 11, `PhaseChipTrackingEm` 0,08, `PhaseChipTintAlpha` 0,15, `PhaseChipBorderAlpha` 0,50, `PhaseChipDotSlot` 10 (6 + 4, solo en `EN MARCHA`), `LiveDotSize` 6, `LiveDotGap` 4, `BigCountDiameter` 72 (DT4b-04, confirmado en DN4b-03), `BigCountDigit` 64 (`num.hero`), `BigCountRingStroke` 3 (la línea central a 34,5), `BigCountRingTrackAlpha` 0,20, `BigCountGap` 8, **[notas]** `PausedGlyphDp` 28, `PausedGlyphGap` 4, `PausedLabelSize` 12, `PausedLabelTrackingEm` 0,08 (`PAUSA` en el disco, DN4b-14), `PanelCeilingGap` 8 (DN4b-23), `CompletionCheckDp` 32 y `CompletionUnitGapDp` 3 (DN4b-21); las de la fila de píldoras en `PillRowGeometry` (§2.2) y las de la fila de información en `InfoRowFit` (§2.15).

### 3.2 Entrada "sumergirse" (AC-WKT-13-06, -07; AC-APP-03-09, AC-APP-15-01)

| t (ms) desde el toque | Qué pasa (por defecto, la spec) |
|---|---|
| 0 | `Push(page, PageTransitionKind.Immerse)`: la página (fondo negro, `.rm-immersive`) entra con opacidad 0; el motor ya corre (AC-WKT-13-01). `ImmersiveSystemBars.EntryStarted()`: se piden las barras ocultas (§4). La llave sale (90) y la página declara la superficie "oculta" de la llave (§5.7). "Atrás" ya abre la confirmación de terminar (AC-WKT-13-07) |
| 0-220 | la página funde de 0 a 1: Workout "funde a negro" |
| 220 | termina la transición de la página: pestañas y barra de Workout en `display: none` (lo de la 4a, a los 240) |
| 220-400 | el fondo (`#hud-backdrop`) funde de 0 a 1 sobre el negro |
| 400-1120 | la cascada: barra superior 400, SPM 520, Split y Watts 600, banda 680, kcal y BPM 760, controles 840; cada pieza en 280 (`ease.enter`), de opacidad 0 a 1 y de +8 a 0 de `translate` (nunca *layout*). La banda entra con el reloj real (ya dice `00:01` si pasó el segundo) |
| 840 | los controles responden desde que empieza su tramo (`HudControls.SetInteractive(true)`; P-15), al toque **y al teclado**; antes, un toque en su lugar no hace nada y ningún control se puede enfocar |
| 1120 | todo quieto: la página declara la superficie real de la llave, que entra en su lugar (opacidad 0 → 1, escala 0,85 → 1 en 160) y, si el panel estaba abierto, se abre ahí |

- **El teclado durante la cascada** (AC-APP-05-07, P-15; QA-G4bT-09 (a)). Opacidad 0 no saca a un control del anillo de Tab. Por eso `SetInteractive(false)` pone `focusable = false` en `Terminar`, `Pausa` y `Reanudar` (y les saca el foco si lo tenían), y `SetInteractive(true)` lo devuelve. Además, el *hook* del primer Tab de la página (el de la raíz del panel, `ImmersiveWorkoutPage.OnPanelNavigation`) y su anillo se abstienen mientras ningún control responde, así un Tab a los 500 ms no enfoca nada y un Enter no activa nada que no se ve. Desde los 840 (con reducir movimiento, desde el cuadro 0) el primer Tab enfoca `Terminar` con su anillo, como en la 4a. "Atrás" y Escape (la confirmación de terminar) responden desde el cuadro 0 (AC-WKT-13-07): no pasan por el foco. `ITT.KeyboardDuringTheCascade` lo prueba con Tab y Enter a los 500 ms y a los 900 ms.
- **Interrupciones.** Si la sesión vuelve a reposo durante la entrada, la cascada se detiene donde está y la salida (§3.3) arranca desde las opacidades que tiene, sin saltos (AC-WKT-13-07). Si se rota o cambia la ventana, el HUD se reacomoda en el acto y la cascada sigue con sus tiempos.
- **Reducir movimiento.** La página funde lineal en 120 con el fondo y el HUD ya a 1: sin negro, sin cascada; los controles responden desde el cuadro 0; la llave entra a los 120, solo con opacidad.
- **Cómo.** `PageTransition` (WP-B2) hace solo la opacidad de la página (`Immerse`: entra en 220; sale esperando la salida del HUD, §3.3). `ImmersiveCascadeMotion` (WP-D1) hace el fondo y las seis zonas con `ImmersiveTimeline.Entry` y avisa cuándo responde cada pieza y cuándo termina. En la 6a el vuelo de cámara reemplaza al negro y al fondo, y la cascada conserva el orden (ANIMATION_BIBLE §2.11): por eso los tiempos son constantes.
- **Los números** son los de la spec, que el Designer confirmó (DN4b-01): su alternativa de 00-deuda-diseno §4.1 queda retirada.

### 3.3 Salida con la última foto (O-01; AC-WKT-13-08, -09; DV-41)

**La regla** (DD4b-05): desde el primer estado de reposo, **el HUD no aplica datos** (`HudView.Freeze()`): los valores, la barra con sus píldoras (o la barra libre con su chip), el sello, la cuenta y el reloj de la banda quedan como en el último estado anterior al reposo, que es `Finished`. Nunca se ve `00:00`, la barra libre en una rutina ni el chip `—`. El reloj de pared también queda (la salida dura 640 ms).

| t (ms) | Qué pasa (por defecto, la spec) |
|---|---|
| 0 | la página queda inerte (`ExitLock`); pestañas y barra de Workout vuelven a `flex` (tapadas por la página, que sigue opaca); la llave sale (90); el diálogo de completado, si estaba, sale con su transición (150, a 0,98; It. 3) y su `CompletionMotion` se detiene en ese mismo cuadro (§3.9). Lo finito del HUD que corría termina con su reloj; lo que se repite (el corazón, el pulso de la fila, el halo, el punto en vivo) sigue hasta que la página sale del árbol |
| 0-200 | el HUD (las seis zonas y las piezas del centro) se va junto: opacidad 1 → 0 (`ease.exit`), desde la que tenga |
| 200-400 | el fondo funde a negro (`ease.exit`) |
| 400-640 | la página negra funde de 1 a 0 (`ease.enter`): aparece Workout en reposo con su barra de pestañas. A los 400 la página avisa `RevealStarted` y entran los avisos del final (DN4b-24) |
| 640 | la página sale del árbol (`OnPopped`): `HudView.Dispose` para todo lo que se mueve (§3.12); `ImmersiveSystemBars.ExitFinished()` pide mostrar las barras (AC-APP-15-04); la llave entra en el carril izquierdo (160: quieta a los 800) |

- **Con el diálogo de completado.** `Terminar` → el motor vuelve a reposo (§2.6) → en ese cuadro la página cierra el diálogo (su cierre de dueño, `Cancel()`, que `Dismissible = false` no bloquea: §3.9) y empieza la salida (640). Nunca queda un diálogo del inmersivo sobre Workout en reposo (AC-WKT-13-04).
- **Sin transición.** Si el primer estado después del segundo plano ya está en reposo (`Instant`), la página sale en el acto (`PopImmediately`, como en la 4a), y las barras y los avisos se piden en ese cuadro.
- **Reducir movimiento.** La página funde lineal en 120, con la misma última foto; los avisos, en el cuadro 0.
- **Los avisos del final** (`workoutSaveError`, AC-APP-03-11): el *store* los emite al volver a reposo (§2.11), pero `WorkoutScreenView` los muestra recién con el `RevealStarted` del inmersivo que sale, a los 400, cuando empieza a verse Workout; sus 4 s cuentan desde ahí. Sobre el negro no se ve nada más que la cinta. Si no hay un inmersivo saliendo, en el acto (CAP-28).
- **Los números** son los de la spec, que el Designer confirmó (DN4b-01, DN4b-09): la alternativa de 00-deuda-diseno §4.3 queda retirada.

### 3.4 Las píldoras: la fila, el relleno, el halo y el cambio de paso (AC-WKT-14-07 a -13, -16; F-WKT-15)

**La barra de pasos de la 4b** (WP-B1; DN4a-04 → 52 base):

```
#hud-topbar .rm-hud-topbar .rm-hud-topbar--steps .rm-hud-glass   alto 52 (74 con el sello); borde 1 @8 %; radio 14; overflow: hidden; sin eventos
├── #hud-pill-row .rm-hud-pill-row     PillRowView: UN elemento (generateVisualContent), alto 20; las píldoras en 10 / 8 / 10 / 4
├── .rm-hud-step-row                   la fila de información de la 4a: relleno 12 / 0 / 12 / 8, min-height 22
│   ├── #hud-step-type                 stepType* 13/700, tracking 0,04 em, step.<tipo>.hud
│   ├── #hud-step-target (.rm-hud-pill)  pastilla en un slot que recorta (flex-shrink 1): el texto lo arma el código con los
│   │                                    fragmentos enteros del plan y el "…" en text.primary (§2.15; DD4b-03, DN4b-16)
│   ├── #hud-up-next (.rm-hud-up-next)   margin-left: auto (M5); .rm-hud-up-next__bg (lo que late) + #hud-up-next-label
│   │                                    (UN Label: "Próximo:" en text.secondary y el tipo en 700 blanco, M3); relleno 7 o 4
│   ├── #hud-countdown                 TabularNumber num.s 20, blanco, flex-shrink 0; su aire a la izquierda, 8 o 6 (§2.15)
│   └── #hud-wall-clock .rm-hud-wall-clock   TabularNumber num.s 20 blanco con el prefijo schedule 16 @70 % y 5 de aire;
│                                            margin-left 10 o 6 (§2.15); flex-shrink 0 (§3.7)
└── #hud-seal .rm-hud-seal             la tercera fila (SealRow, §3.6): 17 de contenido + 5, centrada; alto 0 sin sello
```

Las tres filas son hijas de la barra (en columna). La barra no tiene relleno a los lados: cada fila lleva el suyo, como en el legacy (`:359-362`, `:380-381`). Las tarjetas siguen a `margen seguro superior + 90·s` (AC-WKT-14-01): la barra crece hacia abajo dentro de ese lugar. La geometría por perfil es la de 04-design-notes §3.1 (TV: la barra en x 12,6-398,4, y 8,4-63,2 y 86,4 con el sello; la franja de píldoras en y 17,9-26,3).

**`PillRowView`** (un `VisualElement` con `generateVisualContent`, `pickingMode = Ignore`, nunca enfocable: AC-WKT-14-17):

- **La geometría** es `PillRowGeometry.Layout(ancho útil, n)`, `PixelEdges` y `ThumbOf` (§2.2): bordes en píxeles enteros, todos los lugares del mismo ancho ± 1 px.
- **El orden de dibujo**: (1) el halo de la en curso, por debajo de todas (las pendientes vecinas dejan pasar el 82 %); (2) cada píldora con su pintura (`StepPills.Paint`), con cápsulas de `Painter2D` (radio 4·s); (3) el relleno de la en curso al 100 %, recortado por la cápsula (`PillShapes.FillPolygon`: el borde derecho del relleno es recto, como el `ClipRRect` del legacy, `step_pill.dart:57-76`), con el **brillo** de 1·s `#FFFFFF @25 %` en su borde de arriba, recortado por la cápsula y solo con relleno (DN4b-19: distingue la llena en curso de la hecha, CAP-06); (4) el borde de la hueca (1,5·s al 0,85, por dentro), entero o como trazo parcial durante el cambio de paso (`PillShapes.PerimeterPolyline`), y en las angostas como **dos rieles** (`PillShapes.Rails`); (5) el destello blanco de la que se solidifica; (6) en las angostas, el **pulgar** de la en curso (`ThumbOf`: ≥ 4·s × 10·s, centrado en su lugar y encima de las vecinas, con su pista, su relleno y su brillo adentro; DN4b-18).
- **El halo** (DN4b-17) empieza en el borde de la píldora (o del pulgar), sin hueco, **solo por fuera**: la pista no se tiñe. En el borde se ve `PillGlowEdgeFactor`·α = 0,65·α, lo que deja ahí el `BoxShadow` del legacy (expansión 2, desenfoque 8, sigma 5,12), y se apaga como el halo de las tarjetas, `(1 − d/E)²`, hasta E = 10·s a los costados, 8·s arriba y 4·s abajo: son los aires de la fila, así que el halo llega a 0 en su borde, sin un corte y sin pasar debajo de la fila de información (ART_BIBLE §2.3). α = `a·(0,30 + 0,40·p)`: `p` es el reloj común de la barra (`StepPulse.PulseAt(now)`, el mismo de `Próximo` y de la cuenta) y `a`, la rampa de 150 (`StepPulse.Ramp`). Se dibuja con la textura propia `PillGlowTexture` (radio 4, 10 de halo y el perfil de `GlowTexture`, a 3×: 86 × 86 px, ≈ 29,6 KB, generada al arrancar; la de las tarjetas, de radio 14, no sirve) en 9 partes (`PillShapes.GlowNineSlice`, `MeshGenerationContext.Allocate`), con cada borde estirado a su largo: el perfil en d/E da `(1 − d/E)²` con E distinto por lado. Sale el "agrandado 2 con 8 de desenfoque y recortado a la fila" de la primera versión: dejaba un aro a 2 dp que se leía como marco y un corte duro (04-design-notes DN4b-17). Solo en la en curso y solo "por terminar"; en pausa sigue latiendo; con reducir movimiento, fijo en 0,70 con la rampa.
- **La diferencia con el legacy** (QA-G4b-P03, QA-G4bT-12): el `BoxShadow` del legacy se pinta también debajo de la píldora y la pista se aclara con el pulso; acá la pista queda al 30 %, así el avance del relleno se sigue leyendo. El PO lo tomó como cambio del sistema visual (spec §11.1.1, AC-WKT-14-11).
- **El relleno continuo** es `PillProgress.ValueAt(now)` (§2.3).
- **La tira** (desde 116 pasos en TV): tramos de `ancho útil / n` sin separación, cada uno en su color y con la opacidad de su estado, solo los extremos de la tira redondeados (4·s); con tramos de menos de un píxel, por columnas de píxel (`PillShapes.StripRuns`, §2.2). La en curso es el pulgar.
- **La cinta `SIMULADOR`** tapa el comienzo de la primera píldora (AC-BLE-16-01; en TV, la franja x + y < 56,5 dp): es la capa del simulador, que está arriba de todo.
- **Cuándo se repinta.** Solo si cambió algo: el borde del relleno avanzó un cuarto de píxel (`PillRepaintPx`), corre un cambio de paso, el pulgar que pasa o la rampa, o se ve el halo (que late: cada cuadro durante los últimos 5 s). Sin asignar memoria: los arreglos de bordes y los puntos de las polilíneas son del elemento (§3.13).
- **Sin panel no dibuja ni mide** (QA-V4a-05; QA-G4bT-08 (b)): con `DetachFromPanelEvent` detiene su `PillRowMotion` y, al volver al panel, aplica el estado en el acto.

**El cambio de paso** (`PillRowMotion`, WP-D1; `t = 0` en el cuadro en que el estado trae el paso nuevo; 04-design-notes §4.5):

| t (ms) | La que termina | La nueva |
|---|---|---|
| 0-120 | completa el relleno que le faltaba (`ease.move`) y su halo se apaga con la rampa (0-150) | de pendiente (0,18) a pista (0,30) en 0-150 (`ease.move`); su relleno arranca de 0 con el primer segundo |
| 0-150 | **angostas**: su pulgar baja a la franja y se apaga (`ease.move`) | **angostas**: su pulgar sube desde la franja y se enciende (`ease.move`); nunca hay dos pulgares enteros |
| 120-340 | **cumplida o sin objetivo**: "se solidifica": opacidad 1 → 0,85 (`ease.move`) con el destello blanco `0,40·(1 − ease.enter)`; el brillo del relleno se va (`ease.move`) | — |
| 120-440 | **no cumplida**: el interior baja de 1 a 0,18 mientras el borde se dibuja alrededor, en sentido horario desde el centro de su extremo izquierdo (`ease.move`; en una píldora de 54,8 base, 118,7 de perímetro); en las angostas, los dos rieles a la vez, de izquierda a derecha; el brillo se va en 120-340 | — |

- **El último paso** completa su relleno (0-120) y queda "en curso", lleno y con su brillo si es por tiempo y con la pista sola si es por distancia, sin solidificarse ni marcar su resultado (Q-52, AC-WKT-14-13), hasta que sale el inmersivo.
- **Un paso cortado con `Terminar`** no tiene resultado: queda en curso, con su relleno, en la última foto.
- **Dos cambios seguidos** (pasos de 1 s o un cuadro tarde): lo que corría salta a su final y empieza lo nuevo; con `Instant`, todo a su estado final en el acto.
- **Reducir movimiento** (AC-APP-05-08: "con un fundido, sin destello ni trazo"; QA-G4bT-03): **sin destello**: la sólida solo funde de 1 a 0,85 (120-340); la hueca baja su interior (120-440) y su borde funde de 0 a 1 en 160, **sin trazo**; el brillo se va con su fundido; la nueva sube su pista; el pulgar pasa con un fundido cruzado de 150, sin subir ni bajar. Las 04-design-notes §4.5 y §4.14 conservaban el destello: lo alinea el Designer (la spec y el PO lo confirmaron, spec §16.1).
- **Nunca hay marco** en la en curso (AC-WKT-15-03; commit `3ae0f3a`): no hay ningún "marcador que viaje" (ANIMATION_BIBLE §2.7, precisado en la 4b). El pulgar no rodea nada: es la píldora en curso, más ancha y más alta.

### 3.5 La cuenta grande y `PAUSA`, en el lugar "centro alto" (AC-WKT-14-18, AC-WKT-13-12; D-ANI-05)

**El lugar** lo calcula `HudGeometry` por ventana (04-design-notes §3.6; ART_BIBLE §5.2): un **disco de 72·s** centrado en x en la ventana; en y, centrado entre lo que tiene arriba más 8·s (la barra en su **alto máximo**, 74, así no se mueve con el sello; o SPM, Split o Watts **con objetivo** si se cruzan con él en x) y el borde de arriba de la zona del bote menos 8·s. Nunca toca una pieza ni la zona del bote; al rotar cambia en el acto. **`HudLayout.BigCount`** es el disco, y lo usan **la cuenta grande y `PAUSA`** (DN4b-14): sale `HudLayout.Callout`.

| Perfil | Disco (dp) | x | y | Alto libre para el disco | Aire con la vecina (pide 8·s) | Disco máximo (base) |
|---|---|---|---|---|---|---|
| TV | 75,9 | 167,6-243,4 | 234,0-309,9 | 86,6 | Watts 10,7 | 76,2 |
| P360 | 66,5 | 146,8-213,2 | 204,9-271,4 | 75,7 | Watts 9,4 | 76,3 |
| TH | 75,9 | 419,1-494,9 | 108,3-184,2 | 102,8 | SPM 208,4 | 97,6 |
| L640 | 66,5 | 286,8-353,2 | 94,9-161,4 | 90,1 | SPM 107,6 | 97,6 |
| W1280 | 115,2 | 582,4-697,6 | 188,6-303,8 | 204,3 | SPM 262,5 | 127,7 |

(04-design-notes §3.6, sobre los rectángulos de la 4a sin la derivada, que solo agranda los aires. `HudGeometryTests` los repite ± 0,1.) 72·s es el disco más grande que entra en TV y P360 con el aire que pide la regla (76 base como máximo).

**La cuenta grande** (`#hud-big-countdown`, **un** `TabularNumber` con su placa, M7):

- **La cifra**: `num.hero` 64/800, la de SPM (la spec pide al menos el tamaño del valor de SPM; DT4b-04, confirmado en DN4b-03), blanca, centrada por la banda de las mayúsculas (44,8 base de alto y 34,2 de ancho, en un interior de 66).
- **La placa**, que dibuja el mismo elemento antes de las cifras: el disco de vidrio (`glass.fallback`, sin borde) con el aro de 3·s (la línea central a 34,5·s del centro), la pista al 20 % y el arco al 100 % del color base del paso. El arco es la fracción que falta, f = `PillProgress.RemainingAt(now) / 3`: va del ángulo 360°·(1 − f) hasta las 12, medido en el sentido del reloj desde las 12, con los extremos rectos (`PillShapes.RingArc`). **El borde que se mueve avanza en el sentido del reloj**, como una aguja que se come el arco, y el otro queda fijo en las 12 (DN4b-20). Lleno en el tick del 3, en 2/3 en el del 2, en 1/3 en el del 1 y vacío en el cambio; nunca se vacía antes del tick. Es continuo, también con reducir movimiento: es avance.
- **El rodillo** de la cifra (en los ticks del 2 y del 1; 180, hacia abajo) queda dentro del aro: solo se asoma en un cuadro, por menos de 2 base y con α < 0,12, así que no se recorta.

**`PAUSA`** (`#hud-paused-label`, DN4b-14; spec AC-WKT-13-12): **un** `Label` con `hudPausedLabel` en el mismo disco, centrado en `HudLayout.BigCount`.

- **El disco** es el fondo y el borde del mismo elemento: `glass.fallback`, `border-radius` 36·s y el aro **entero** de 3·s en `phase.paused` (`border-width`), los dos en px por código (USS no tiene `em`: QA-G4aT-14).
- **El glifo de pausa** (el `pause` de Material, de 28 base: dos barras de 4,67 × 16,33 separadas 4,67, radio 1,17, en blanco) lo dibuja el `generateVisualContent` del mismo `Label`, con `Painter2D`, junto a su texto (lo prueba S-B4b; si no da, un `IconView` hijo: +1 elemento, solo en pausa; §7.0).
- **La etiqueta**: 12/700 (`.rm-type-header-s`, Inter 700), *tracking* 0,08 em, blanca, 4 debajo del glifo. Glifo y etiqueta van como un bloque centrado: las barras de −14,5 a +1,8 y las mayúsculas de +5,8 a +14,5 desde el centro. `PAUSED` (53,9) entra en la cuerda del aro a la altura de su base (59,3).
- **Quieto**: no late (lo que corre late, lo que está en pausa queda quieto). No responde ni se enfoca.
- **Por qué no la cápsula** de la primera versión: `title` 20/700 con su relleno mide 108 base (`PAUSED`, 122), y en vertical, al lado de Watts con objetivo, caben 76.

**Un disco por vez** (`CenterDiscMotion`, WP-D1; `CenterDiscTimeline`; 04-design-notes §4.6 y §4.8):

| Qué | Cómo | Reducir movimiento |
|---|---|---|
| La cuenta entra (tick del 3) | 200: opacidad `ease.enter` y escala 0,85 → 1 desde el centro del disco | 160, solo opacidad |
| La cifra cambia (ticks del 2 y del 1) | el `DigitRoll` de `TabularNumber`, hacia abajo (180) | el fundido de 80 |
| La cuenta sale (tick del cambio de paso o fin) | 200: opacidad `ease.exit` y escala 1 → 1,10, "se suelta", con el aro ya vacío | 120, solo opacidad |
| `PAUSA` entra (`Pausa`) | 240: opacidad `ease.enter` y escala 0,96 → 1 | 240, solo opacidad |
| `PAUSA` sale (`Reanudar`) | 240: opacidad `ease.exit` y escala 1 → 0,96 | 240, solo opacidad |
| **Cambio de disco** (pausar con la cuenta a la vista; reanudar cuando la cuenta vuelve) | el que sale funde a 0 en 0-120 (`ease.exit`, sin escala) y el que entra empieza a los 120 con su entrada de siempre; nunca hay dos discos a la vista | los mismos tiempos, sin escala |

- **En pausa no hay cuenta grande** (AC-WKT-14-18, P-04): la regla da null; va `PAUSA`. Al reanudar, la cuenta vuelve si corresponde, con la cifra del momento (el dato no espera al disco).
- **Al volver del segundo plano o despertar**, el primer cuadro muestra el disco que corresponde, sin entrada (y la cuenta, con su aro en su lugar).
- **CAP-27** (`Pausa` a los 47,0 s, en el tick del 3): la cuenta entra y sale en el mismo instante (DT4b-18); con un disco por vez, `PAUSA` entra a los 47,12 y a los 47,6 está quieto.

### 3.6 El sello (AC-WKT-09-08, -09; D-ANI-05)

**Dónde**: una tercera fila **dentro** de la barra de pasos (04b/00-deuda-diseno §4.9; ANIMATION_BIBLE §2.7): bajo las píldoras y la fila de información, centrada, en una línea, sobre el vidrio de la barra; la barra crece de 52 a 74 hacia abajo y queda siempre a 8·s de las tarjetas (TV: termina en 86,4 y las tarjetas empiezan en 94,8). No tapa la barra, ninguna tarjeta, la zona del bote ni los controles; el panel del simulador abierto no la cambia.

```
#hud-seal .rm-hud-seal                      slot que recorta (overflow: hidden): alto 0 → 22 con la barra; relleno 12 a los lados;
│                                           el contenido (17) quieto arriba del slot y 5 de aire abajo: se descubre al abrirse
├── #hud-seal-icon .rm-hud-seal__icon       IconView 14: check_circle relleno (cumplido) o error de contorno (no alcanzado)
└── #hud-seal-text .rm-hud-seal__text       UN Label, nowrap: el título (headerS 13/700, del color del veredicto) + " · " + el
                                            detalle (12/600, blanco) por texto enriquecido; si no entra, el contenido se ACHICA
                                            (escala pareja desde su centro, en el acto), nunca "…"
```

- **Colores e íconos** (la spec; el Designer los confirmó en DN4b-02 y retiró `#FF9800` y `text.secondary`): `Objetivo cumplido` en `state.success` (`#06D6A0`) con `check_circle` relleno, como la sólida; `No alcanzado` en `#FFD166` (`phase.paused`) con `error` de contorno, como la hueca; el detalle en blanco. TV: la fila del sello en y 62,2-80,1 (04-design-notes §3.1).
- **El texto enriquecido** (13/700 en el color del veredicto y 12/600 blanco en una sola etiqueta) lo prueba la sonda S-B4b; si no da, son dos etiquetas (+1 elemento; §7.0).
- **Lo que no entra se achica**: en TV, `Pirámide 500` entra justo a 1 (338,8 de 339,9) y los extremos de FTMS bajan a 0,753 (04-design-notes §3.7). La escala se mide solo con panel.
- **Movimiento** (`SealMotion`, WP-D1; `t = 0` en `LastOutcomeAt`), todo **dentro de los 2 s** (la spec):

  | t (ms) | 0-240 | 40-240 | 60-300 | 1800-1920 | 1800-2000 |
  |---|---|---|---|---|---|
  | Qué | la barra crece de 52 a 74 (`ease.move`) | el contenido funde de 0 a 1 (`ease.enter`) | el ícono se estampa: 0,6 → 1 (`ease.emphasis`) | el contenido funde a 0 (`ease.exit`) | la barra vuelve a 52 (`ease.move`) |

- **Un sello nuevo** (pasos de 2 s o menos) reemplaza al visible: el viejo funde en 0-80, el nuevo entra en 80-200 con el ícono que se estampa otra vez, los 2 s vuelven a empezar y la barra sigue abierta. Un paso sin objetivos no lo toca.
- **En pausa** sigue sus 2 s (es el reloj de la interfaz). **Al volver del segundo plano**, uno cuyos 2 s ya pasaron no se muestra; uno que todavía tiene tiempo se ve en el acto, sin entrada, por lo que le queda.
- **Desde `Finished`** no entra ningún sello (el último paso no tiene); uno que se estaba viendo (un `Terminar` a mano dentro de los 2 s) sigue su reloj y la última foto puede llevarlo.
- **Reducir movimiento**: el alto cambia en el acto, el contenido funde en 160 y en 120 y el ícono no escala.
- **Los tiempos** son los de la spec, que el Designer confirmó (DN4b-02): su alternativa (un resorte desde los 120 y la salida hasta los 2280) queda retirada.
- **La entrega de la posta** (04-design-notes §4.7): el sello se estampa (60-300) mientras la cuenta grande "se suelta" (0-200) y la píldora se solidifica (120-340).

### 3.7 La barra libre (reloj, chip y punto en vivo) y el velo de pausa (AC-WKT-16-01 a -06, -19-01 a -05, -13-12)

```
#hud-topbar .rm-hud-topbar .rm-hud-topbar--free .rm-hud-glass   alto 40; relleno 14 × 8; borde 1 @8 %; radio 14; en fila; sin eventos
├── #hud-free-title                 rowing 20 accent.primary + 8 + hudFreeTitle 16/600 blanco; flex-shrink 1: el título se
│                                   ACHICA hasta 0,8 (escala pareja) y solo por debajo se recorta con "…" (§2.15; DN4b-27)
├── #hud-wall-clock                 margin-left: auto; el mismo WallClockView que en la barra de pasos
└── #hud-phase-chip .rm-hud-phase-chip   margin-left 10; ANCHO FIJO (101,8 base en es, 74,1 en en); alto 19,6 (3 + 13,6 + 3);
    │                                    relleno 8 × 3; radio r.m 8; tinte @15 % y borde 1 @50 % del color base; contenido centrado
    ├── #hud-phase-dot .rm-hud-phase-chip__dot     6, solo en EN MARCHA, en phase.running; 4 de aire; centrado en las mayúsculas
    └── #hud-phase-label .rm-hud-phase-chip__label 11/700 (el repo no tiene Inter 800), tracking 0,08 em; color de texto por código
```

- **El reloj de pared** (`WallClockView`, en las dos barras) es **un** `TabularNumber` `num.s` 20/700 blanco, del tamaño de la cuenta (P-07; `series_panels.dart:99`), con el ícono `schedule` de 16 al 70 % como **prefijo** (fuente de íconos) y 5 de aire, con la tinta del ícono centrada en la banda de las mayúsculas de las cifras: 69,1 base en total, siempre igual (04-design-notes §3.4). Cifras tabulares: no cambia de ancho con la hora ni con la cuenta que crece. Con un estado de `WallClockTicker`: `SetValue(text, minuteOfDay, animate: !Instant)` → el rodillo del HUD (180, hacia arriba; a medianoche, hacia abajo) solo al cambiar el minuto (AC-WKT-19-02). Así se cumple el rodillo de la spec con un solo elemento (DT4b-08, de acuerdo el Designer).
- **El chip**: el texto, el tinte, el borde y el punto de `HudPhaseChip`. **Ancho fijo** (DT4b-11; DN4b-26) = `PhaseChip.FixedWidthBase`: máx(`EN MARCHA` + 10 del punto, `PAUSADO`, `FINALIZADO`) + 16, con el lugar del punto solo en la etiqueta que lo lleva, medido con `MeasureTextSize` (solo con panel) al cambiar el idioma: 101,8 base en es y 74,1 en en (04-design-notes §3.5; la primera versión daba 102,2 y 84,1 sin las tablas K2 y con el punto en todas). El contenido va centrado: el reloj nunca se corre cuando cambia la fase. Al cambiar la fase (`PhaseChipMotion`): los colores cruzan en 150 (`ease.move`; 0,053 · 0,350 · 0,851 · 0,989 en los cuadros 1 a 4) y la etiqueta se reemplaza (la vieja funde en 0-80, la nueva entra en 80-200 ya en su color). En el cuadro de la fase el texto nuevo ya es el bueno (AC-WKT-16-03). El texto de `FINALIZADO` va en `phase.finished.hud` `#FF7A92` (§2.7).
- **El punto en vivo** (`LiveDotMotion`) va con la etiqueta: se va con `EN MARCHA` y vuelve con ella. Late entre 0,35 y 1 (q de 1 a 0 en 800 y vuelta en 800, `ease.pulse`; opacidad 0,35 + 0,65·q) y **empieza en 1, bajando, cada vez que el chip pasa a `EN MARCHA`**: al reanudar se ve encendido de entrada (DN4b-26). En `PAUSADO` y `FINALIZADO` no hay punto (la spec); con reducir movimiento, quieto en 1. En las tomas, con el origen en el instante 0: 0,444 a los 42,2 s (CAP-13) y 1 a los 3,2 s (DEMO-02).
- **Nada se encima** (AC-WKT-16-06; DV-47): el título es lo único que cede; el reloj y el chip tienen `flex-shrink: 0`. **Primero se achica** con `InfoRowFit.FreeTitleScale` (§2.15), en el acto con la ventana o el idioma, hasta 0,8; solo por debajo de eso se recorta con "…". En TV y en 360 en español entra entero a 0,818 (127,0 de 155,4 base), como espera CAP-13; recortado se leería `Entrenamient…`. En inglés y en TH, L640 y W1280 entra a su tamaño. La escala es pareja, desde el borde izquierdo del título, y se mide solo con panel.
- **El velo de pausa** (`PauseVeilMotion`, WP-D1): al pausar, el fondo se oscurece un 15 % en 300 (`ease.move`) como negro al 15 % encima: es el **tinte de la textura del fondo** (`unityBackgroundImageTintColor` de `#hud-backdrop` de 1 a 0,85), sin elementos nuevos; el cielo pasa de `#0B4FA8` a `#09438F` y el agua de `#0E4F8F` a `#0C437A` (DD4b-14). `PAUSA` entra en su disco (§3.5, con un disco por vez). Las tarjetas, la barra, sus textos y el chip **no bajan de opacidad** y los valores siguen en vivo (ART_BIBLE §3.2): su vidrio sobre el fondo oscurecido queda más oscuro y el contraste solo sube. Al reanudar, todo vuelve en 240. Con reducir movimiento, fundidos sin escala.

### 3.8 kcal que se desliza (AC-WKT-17-11; 04a/04-design-notes §5.6)

`KcalSlideMotion` (WP-D1) reemplaza a `HudView.BpmMotion`. Es un FLIP: el *layout* de la columna cambia en el acto y kcal se dibuja con un `translate` desde su lugar viejo hasta 0, medido del `layout` antes y después (en vertical sube o baja el alto de BPM más su separación: 71,7·s).

| Caso | kcal | BPM |
|---|---|---|
| Aparece BPM (pulso > 0) | se mueve en 0-240 (`ease.move`) | funde de 0 a 1 en 120-360 (`ease.enter`) |
| Se va BPM (pulso 0) | se mueve en 120-360, cuando BPM salió del árbol visible | funde a 0 en 0-120 (`ease.exit`) en su lugar |
| Horizontal | está en el rincón y no se mueve | funde igual |
| Reducir movimiento | cambia de lugar en el acto | funde (240 / 120) |

### 3.9 El diálogo de completado (F-WKT-21; AC-WKT-21-01 a -12)

```
#completion-dialog .rm-dialog .rm-dialog--completion   ModalDialog(ui, "completion-dialog", 400), Dismissible = false; opaco (surface.overlay, P-08)
├── (dialog-title de la base, en display: none)
├── .rm-dialog__pinned > .rm-completion__header          fijo arriba: no se desplaza; en fila: [check 32] 12 [título]
│   ├── #completion-check .rm-completion__check          CheckMarkView 32 (el legacy, 28): disco state.success + trazo del check
│   │                                                    en surface.overlay; centrado en el alto del título (DN4b-21)
│   └── .rm-completion__title                            workoutCompleted, .rm-type-title (20/600, como los otros diálogos), ajusta:
│                                                        en TV y P360, dos líneas (`¡Entrenamiento` / `completado!`; DV-47)
├── 12
├── ScrollView .rm-dialog__content                       lo que no entra se desplaza (AC-WKT-21-12)
│   ├── .rm-completion__message                          workoutCompletedMessage, .rm-type-body 15, text.secondary
│   ├── 16
│   └── .rm-completion__summary                          caja blanca al 4 %, radio 8, relleno 12 (:973-978); filas separadas 8
│       ├── #completion-time .rm-completion__row        timer 20 accent.primary · 12 · workoutCompletedTime · valor
│       ├── #completion-distance .rm-completion__row    straighten · workoutCompletedDistance · "{n}" con la m de sufijo
│       └── #completion-calories .rm-completion__row    local_fire_department · workoutCompletedCalories · kcal sin unidad
│           (rótulos .rm-type-body-m 14 text.secondary; cada valor, un TabularNumber .rm-type-num-s 20/700 blanco, que sigue
│            la letra del sistema; la m, sufijo Inter 500 de 14 en text.secondary a 3, sobre la línea de base; DN4b-21)
├── 20
└── .rm-dialog__actions > #completion-finish             RmButton primario relleno, workoutFinish (un solo botón: AC-WKT-21-10)
```

- **`ModalDialog` (C, WP-B0)**: `protected virtual bool Dismissible => true`. Los caminos **del usuario** (el velo, y Escape y "Atrás" por `DialogLayer.CancelTop()` y `BackNavigation`) pasan por `Dismiss()`, que con `false` no lo cierra: tocar el velo no hace nada y `CancelTop()` **consume** el pedido sin cerrarlo, así Escape no llega al HUD (AC-WKT-21-02, AC-WKT-13-03). **El cierre del dueño no se bloquea** (QA-G4bT-09 (c); la lección de QA-G4aT-04): `Cancel()` (público) y `Accept()` (protegido) siguen cerrándolo, y la página lo cierra con `Cancel()` en el primer estado de reposo, antes del `Pop` (§3.3). `DialogLayer.Open(dialog, instantly)`: sin transición, con todo en su estado final (AC-WKT-21-09).
- **Cuándo**: la página lo abre a los 300 ms del fin (§2.6), una vez por sesión; con `OpensInstantly`, o si el `Schedule` vence dentro del alcance, en el acto, con el check dibujado y los totales finales. La cinta y la llave quedan por encima del velo, y la llave se puede usar (si su lugar se cruza con el diálogo, pasa al hueco libre más bajo del carril izquierdo: §5.7).
- **Entra con peso** (`CompletionMotion`, WP-D1; ANIMATION_BIBLE §2.11; 04-design-notes §4.12): el velo y el diálogo con la transición de la It. 3 (velo 0 → 0,6 en 200; diálogo 0,96 → 1 y fundido en 220); el disco del check de 0,6 a 1 en 60-300 desde que entra el diálogo (`ease.emphasis`) y su trazo en 180-500 (`ease.move`), de P0 a P2 en proporción al largo (`CheckGlyph`: P0 (−0,629; −0,071), P1 (−0,200; +0,359), P2 (+0,630; −0,471) en radios del disco, R = 16, y hacia abajo; 0,20·R de grosor, unión en inglete, extremos rectos y 1,780·R de largo, en tramos de 0,607·R y 1,173·R); los totales cuentan de 0 a su valor en 700 (`ease.enter`) desde los 220, escalonados 120, con el formato del legacy en cada cuadro (`CountUp`: `⌊final·e(u)⌋`, el tiempo en su formato `00:00` … `00:30`), **sin rodillo** mientras cuentan, y **siempre** terminan en el valor exacto (a los 1160 del diálogo). Reducir movimiento: solo opacidad, el check entero y los totales finales desde el primer cuadro.
- **El conteo se detiene con el diálogo** (QA-G4bT-08 (a)): `CompletionDialog.OnClosing` (el gancho de `ModalDialog`, en el cuadro 0 de su salida) llama a `CompletionMotion.Stop()`, y también lo hace su `DetachFromPanelEvent`. Con `Terminar` durante el conteo (300-1460 ms desde el fin) y el cierre ya vuelto, nada sigue contando sobre números fuera del panel ni pide 60 fps después de los 640 de la salida (`ITT.NothingAnimatesAfterTheExit`, el caso "Terminar durante el conteo").
- **Los valores** son los de `HudCompletion`: fijos (DV-43), aunque lleguen lecturas o se rote (AC-WKT-21-04). Detrás, el HUD sigue en vivo bajo el velo (AC-WKT-21-08).
- **`Terminar`**: `Hud.AcknowledgeCompletionAsync()`. Desde el primer toque el botón queda tomado (no responde a otro); si la vuelta a reposo tarda más de 300 ms, muestra el *spinner* (`RmButton.SetBusy`, `FinishSpinnerDelayMs`). Cuando el motor vuelve a reposo, la página cierra el diálogo y sale (§3.3). Si el cierre falló o no volvió en 15 s de primer plano, Workout muestra `workoutSaveError` (CAP-28).
- **Teclado** (AC-WKT-21-11, AC-APP-05-07): al aparecer, nada enfocado; el primer Tab va a `Terminar` con su anillo (con el arreglo de `FocusHighlight` de 00-deuda-4a §2), también después del mouse; Enter y Espacio lo activan; Tab no sale del diálogo (`FocusTrap`); Escape no hace nada. **El primer Tab sin foco** (QA-G4bT-09 (b)): sin nada enfocado, el `NavigationMoveEvent` llega a la raíz del panel y no al árbol del diálogo, donde escucha `FocusTrap`; la página se abstiene con un diálogo abierto. Por eso `DialogLayer` (B0) escucha la raíz del panel (`TrickleDown`) y, con un diálogo arriba y el foco fuera de su caja, enfoca `FocusTrap.Next(Top.Box, null, forward)` e ignora el evento. Vale para todos los diálogos de la app (en los otros ya había foco adentro).
- **Tamaños y letra** (AC-WKT-21-12; DN4b-21): `ModalDialog` ya da el ancho `min(400; ventana − 80)` con un mínimo de 280, el relleno de 24 (16 con la ventana de menos de 480 dp de alto), el alto máximo `ventana − 48 − área segura` y el contenido desplazable con las acciones a la vista; el encabezado va en `Pinned` (fijo). Crece con la letra del sistema hasta 1,3× (AC-APP-05-05). Los rectángulos de 04-design-notes §3.8 (TV 40,0-371,0 × 298,0-616,0; TH 257,0-657,0 × 63,2-347,8; L640 a 1,3× se desplaza 30,3 con `Terminar` a la vista; W1280 440,0-840,0 × 209,7-510,3) son los de `CompletionDialogTests.SizesAndLetter`, ± 1, en los cuatro perfiles × es / en × 1 y 1,3 con los totales de `Corta` y con los de `Maratón` con `R-4X` (`9:59:59`, `16777215 m`, `65535`, que a 1,3 en TV usan 251,6 de 259). **El caso de `Maratón` abre el diálogo con su `HudCompletion`** (`UiTestHost.OpenCompletion`), sin correr 10 h simuladas en cada combinación (QA-G4bT-11 (d)); `Corta` lo recorre entero, del motor al diálogo.
- **Sin Strava** hasta la It. 12 (AC-WKT-21-10): un solo botón; las acciones quedan con lugar para la variante de dos botones (`profileUploadToStrava`).
- **`.rm-dialog--hud`** sigue reservada para el vidrio con desenfoque (6b); hasta entonces, el opaco de la app (P-08, DN4a-11).

### 3.10 Reducir movimiento (AC-APP-05-08)

| Pieza | Con reducir movimiento |
|---|---|
| Entrada y salida | fundido lineal de 120, sin negro ni cascada; la salida con la última foto; los controles responden desde el cuadro 0, la llave entra a los 120 (solo opacidad) y los avisos del final salen en el cuadro 0 |
| Relleno de la píldora, aro de la cuenta grande | igual: son avance |
| Halo de los últimos 5 s | fijo en 0,70 (0,455 en el borde), con la rampa de 150 (un fundido) |
| Cambio de paso de la píldora | **sin destello ni trazo** (AC-APP-05-08; QA-G4bT-03): la sólida funde de 1 a 0,85; la hueca baja su interior y su borde funde de 0 a 1 en 160; el brillo se va con su fundido |
| Pulgar (píldoras angostas) | pasa a la siguiente con un fundido cruzado de 150, sin subir ni bajar |
| Fila de información | los aires y el relleno de `Próximo` cambian en el acto entre pasos; la pastilla que no entra se va con su fundido de 120 |
| Cuenta grande | entra en 160 y sale en 120, solo opacidad; la cifra con el fundido de 80 |
| Un disco por vez | los mismos tiempos (120 y 120), sin escala |
| Sello | el alto en el acto; el contenido en 160 y en 120; el ícono no escala |
| Velo y `PAUSA` | fundidos, sin escala |
| Chip | el punto quieto en 1; los colores y la etiqueta, iguales |
| Reloj de pared | fundido de 80 en lugar del rodillo |
| kcal | cambia de lugar en el acto; BPM funde (240 y 120) |
| Diálogo de completado | solo opacidad; el check entero y los totales finales desde el primer cuadro |
| Lo de la 4a | lo de la 4a |

Nada de esto cambia los datos ni cuándo pasan. Una toma fija con y sin reducir movimiento sale igual, salvo los realces fijos (el halo en 0,70, la cuenta por terminar a 1,1 en el color del paso, `Próximo` a 1 y los de la 4a). `HTL.ReducedVariants` prueba cada fila de esta tabla contra su línea de tiempo, con el destello en 0 en todo cuadro del cambio de paso; las 04-design-notes §4.5 y §4.14 todavía dicen que el destello queda, y las alinea el Designer (spec §16.1).

### 3.11 El presupuesto del HUD (R4a-03; AC-WKT-14-19, AC-WKT-17-01)

"Visible" = en el árbol de `#immersive` y con `display` distinto de `none` (la regla de `HudBudgetTests`). El tope sigue en **60** (ARCHITECTURE §7.8, §20.2). La 4a contó su peor caso con objetivos en SPM y Watts; el de la 4b es más exigente y se define así: **rutina con objetivos en SPM, Split y Watts, los tres fuera (tres flechas), BPM, `Próximo`, la cuenta grande y el sello a la vista**; y **guardando** (terminada a mano) con el *spinner*, `Próximo` y un sello.

| Cuenta | En marcha | Guardando |
|---|---|---|
| La 4a en su peor caso (02-tech-design de la 4a §6.13) | 59 | 60 |
| + Split con objetivo (halo y `obj.`) y dos flechas más visibles | 63 | 64 |
| + la fila de píldoras (1), el reloj de pared (1), el sello (3: la fila, el ícono y el texto) | 68 | 69 |
| + la cuenta grande (1; en pausa, `PAUSA` en su lugar) | 69 | — (la cuenta grande no se ve terminada) |
| **M1** la flecha la dibuja su `TabularNumber` como prefijo (Inter 700 a 0,4 em, en el hueco de 0,48 em, con su línea de base, color, alfa y desplazamiento) | −3 | −3 |
| **M2** las unidades las dibuja su `TabularNumber` como sufijo (`W` de Watts y `m` de la banda), y se van sus dos filas contenedoras | −4 | −4 |
| **M4** el halo de objetivo lo dibuja su tarjeta (el mismo `GlowTexture` en 9 partes, por fuera, en su `generateVisualContent`) | −3 | −3 |
| **M6** el corazón de BPM va en posición absoluta en su tarjeta, sin la fila del rótulo | −1 | −1 |
| **Total** | **58** | **58** |
| Opcionales, si la sonda S-B4b los confirma: **M3** `Próximo:` y el tipo en una sola etiqueta con texto enriquecido (−1); **M5** `margin-left: auto` en lugar del espaciador de la fila (−1) | 56 | 56 |

- **M7**: la cuenta grande es un solo `TabularNumber` con su disco y su aro como **placa** del mismo elemento, y el reloj de pared, un `TabularNumber` con su ícono de **prefijo**: por eso suman 1 cada uno (el Designer contaba 2).
- **Las 04-design-notes no suman elementos** (§6 de las notas): `PAUSA` es uno (el disco y el aro son su fondo y su borde, y el glifo lo dibuja su `generateVisualContent`) y ocupa el lugar de la cuenta grande, que en pausa no está; el pulgar, los rieles, el brillo y el halo los dibuja la fila; la pastilla por fragmentos sigue siendo una etiqueta. Si la sonda S-B4b no da para el glifo, `PAUSA` lleva un `IconView` hijo (+1 en pausa: 59); si no da para el texto enriquecido del sello, el sello va en dos etiquetas (+1 con el sello: 59).
- **Lo que no cambia a la vista** con M1, M2, M4 y M6: las sondas de 04a/04-design-notes §8.2 y las medidas de la flecha, la unidad y `obj.` contra sus líneas de base (± 1 dp, las de la sonda S-B4a de la 4a) tienen que dar lo mismo; `HudLayoutTests.MergedPiecesMatchTheFourAGeometry` lo prueba (§6).
- **`HudBudgetTests`** prueba los dos peores casos de la 4b en los cuatro perfiles y, cuando falla, lista cada elemento visible con su nombre (así el número de esta tabla queda verificado con el árbol real).
- **0 B por cuadro**: los afijos, la placa y el halo de la tarjeta se arman sin asignar (cadenas de un carácter precalculadas, como las cifras; colores en *structs*).

### 3.12 Frame pacing y nada animando al salir (AC-WKT-13-09; QA-V4a-02 = ROB-02, QA-V4a-05)

- **Mientras el inmersivo está arriba** pide 60 fps (`FramePacingPolicy.Acquire` desde la construcción, como en la 4a); cada `IMotion` nuevo pide y libera lo suyo con el `MotionDriver`.
- **Al salir** (`OnPopped`, a los 640), `HudView.Dispose` detiene, además de lo de la 4a (el corazón, el pulso de la fila), todo lo nuevo: `PillRowMotion`, `InfoRowMotion`, `CenterDiscMotion`, `SealMotion`, `PauseVeilMotion`, `LiveDotMotion`, `PhaseChipMotion`, `KcalSlideMotion` y el rodillo del reloj de pared (que `TabularNumber` ya termina al salir del panel, QA-V4a-05); el *store* dispone `WallClockTicker` (su periódico). El diálogo de completado salió antes: su `OnClosing` detiene `CompletionMotion` en el cuadro 0 de la salida (§3.9).
- **Cada vista nueva** (`PillRowView`, `StepInfoBar`, `SealRow`, `CenterCallouts`, `PhaseChipView`, `WallClockView`, `FreeTopBar`, `CompletionDialog`) **detiene sus `IMotion` en `DetachFromPanelEvent` y no mide texto sin panel** (QA-V4a-05; QA-G4bT-08 (b)): `MeasureTextSize` y los anchos de §2.15 solo con `panel != null`; si llega un estado sin panel, se guarda y se aplica en el acto al volver (`AttachToPanelEvent`). Así ninguna pieza pide la escala del panel después de salir y el registro no lleva avisos de DPI.
- **Nada corre en el alcance** (QA-G4bT-08 (c), (d)): `WallClockTicker` no tiene periódico mientras la app está en segundo plano (§2.8), y el `Schedule` de 300 ms del diálogo, si vence dentro del alcance, lo abre con `instantly` (§2.6).
- **La prueba** (`ImmersiveTransitionTests.NothingAnimatesAfterTheExit`; al día por verificación, ronda 1, QA-V4b-MOT-05): después de la salida, el `MotionDriver` y las demandas de 60 fps vuelven a los de reposo, con **cinco salidas**: a mano en un libre (el chip y el punto en vivo a la vista), a mano en `Estados` con el sello y `PAUSA` a la vista, a mano en `Estados` con la cuenta grande, el halo y `Próximo`, con `Terminar` del diálogo ya contado y **con `Terminar` durante el conteo**; y **0 avisos de DPI** en el registro (`Application.logMessageReceived`, como `IHT.DigitsLeavingThePanelStopRolling`). **La vuelta del segundo plano ya en reposo queda en `IHT.LeavingTheImmersiveStopsItsContinuousMotions`** (no en ITT). En el build de simulador, QA mira el registro después de tres entrenamientos ([Device-Win]).

### 3.13 Rendimiento de la fila (AC-WKT-14-19)

- **La fila** se repinta solo cuando cambia algo (§3.4). Con 200 pasos, un repintado son unas 200 cápsulas de `Painter2D` y el 9-slice del halo; durante los últimos 5 s de cada paso, uno por cuadro. Con el tope del editor (1998 pasos), la tira se dibuja por columnas de píxel (`PillShapes.StripRuns`, §2.2): a lo sumo una por columna (≈ 950 en TV), así el costo no crece con `n` (DT4b-21).
- **PlayMode** (`HudBudgetTests.LongSeriesStaysSmooth`; QA-G4bT-11 (a), (c)): `Serie larga` en los cambios de paso, con el halo, el sello y la cuenta grande, y **la fila sola con 1998 pasos** (montada con estados de `HudPillRow` armados a mano, con un cambio de paso, el halo y el pulgar, sin correr el motor 1998 pasos). Comprueba 0 B por cuadro (`GC.GetAllocatedBytesForCurrentThread`) y **el trabajo del HUD**, no el cuadro del Editor: lo que tardan aplicar el estado (`HudView.Apply`) y el `generateVisualContent` de la fila, medidos con su `Stopwatch` alrededor de esas dos llamadas, a lo sumo 16 ms por cuadro (la mitad de los 33 de AC-WKT-14-19), con el máximo y el p95 en el informe de la prueba. El cuadro entero del Editor en *batchmode* no se acota: depende de la PC.
- **En el build de simulador** ([Device-Win]; QA-G4bT-11 (b)): `FrameTimeProbe` (solo con `ROWMATE_SIMULATOR`) mide **el tiempo de CPU del hilo principal de cada cuadro** (`FrameTimingManager.CaptureFrameTimings` y `GetLatestTimings`: `cpuMainThreadFrameTime`, en ms) y escribe en el registro, cada 10 s mientras el inmersivo está arriba, `[PERF] cpu del hilo principal: máx X ms, p99 Y ms, N cuadros`. "Ningún cuadro tarda más de 33 ms" (AC-WKT-14-19) = el máximo de esa medida ≤ 33 en `Serie larga`, con los cambios de paso, el halo, el sello y la cuenta grande. Las estadísticas necesitan `PlayerSettings.enableFrameTimingStats`: `BuildCli` las prende **solo** en el build de desarrollo con simulador y las restaura al terminar (`ProjectSettings` no cambia). Si `FrameTimingManager.IsFeatureEnabled()` da `false` en el equipo, la sonda anota `[PERF] sin FrameTimingManager: NO VERIFICABLE` y el intervalo entre cuadros (con su `Stopwatch`) solo como información, porque con vsync no es la métrica. QA corre `Serie larga` y lee el máximo.
- **Si en el teléfono (6a) la fila pesa**, el corte previsto es dibujar en dos elementos (las píldoras quietas, que se repintan solo en el cambio de paso, y la en curso con el halo): +1 elemento, que el presupuesto de §3.11 tiene (R4b-02).

---

## 4. Las barras del sistema, por plataforma (F-APP-15; AC-APP-15-01 a -04; DD4b-07)

El legacy pone el modo inmersivo persistente al crear la página y "de borde a borde" al destruirla (`immersive_workout_screen.dart:32-41`); en escritorio no tiene efecto. La spec fija los momentos: ocultar con el fundido de Workout, al empezar cada entrada, y mostrar al terminar cada salida (AC-APP-15-04).

### 4.1 La regla (App, pura)

```csharp
namespace RowMate.Core.Ports
{
    // F-APP-15 (ARCHITECTURE §13: ISystemUi.SetImmersive). Android: barras ocultas en modo inmersivo persistente; iOS: la barra
    // de estado (10b); escritorio y Editor: sin efecto (paridad: immersive_workout_screen.dart:34 no hace nada en escritorio).
    public interface ISystemUi
    {
        void SetImmersive(bool immersive);
    }
}

namespace RowMate.App.Workout
{
    // Cuándo se piden las barras ocultas y visibles. Cuenta los inmersivos entre su entrada y el final de su salida: con la
    // sesión nueva que abre su inmersivo durante la salida del anterior (QA-V4a-01), la salida del viejo no las muestra.
    public sealed class ImmersiveSystemBars : IDisposable
    {
        public ImmersiveSystemBars(ISystemUi ui, IAppLifecycle lifecycle, IAppLogger logger);
        public bool Hidden { get; }
        public void EntryStarted(object owner);   // cada entrada: SetImmersive(true), aunque ya estén ocultas
        public void ExitFinished(object owner);   // cada salida terminada: SetImmersive(false) si no queda otro inmersivo
        // Con Hidden: al volver del segundo plano (InBackground → false) y al recuperar el foco, SetImmersive(true) otra vez
        // (Android las puede devolver al volver de otra app). Dispose: si quedaron ocultas, SetImmersive(false).
        public void Dispose();
    }
}
```

| Caso (AC-APP-15-04) | Pedidos |
|---|---|
| Entrada y salida normales | `true` en el cuadro del toque; `false` al terminar la salida (a los 640 ms) |
| La sesión termina durante la entrada | `true`; la salida arranca desde donde estaba la entrada; `false` al terminar la salida |
| Con el diálogo de completado abierto | ninguno (los diálogos son de UI Toolkit, no ventanas del sistema) |
| Vuelta del segundo plano con el inmersivo arriba | `true` otra vez |
| Una sesión nueva durante la salida de la anterior (QA-V4a-01) | `true` (la nueva); la salida de la vieja termina sin pedir `false`; `false` al terminar la salida de la nueva (DT4b-05) |
| La app se cierra con el inmersivo arriba | `false` en `Dispose` |

Nunca quedan ocultas en Workout ni en otra pantalla. `FakeSystemUi` (Tests/Support) anota cada pedido con el instante del programador: lo leen `ImmersiveSystemBarsTests` (dotnet) y `SystemBarsTests` (PlayMode).

### 4.2 Por plataforma (Platform, WP-B2)

| Plataforma | Implementación | Verificación |
|---|---|---|
| Android | `AndroidSystemUi`, por JNI desde C# (como `AndroidAccessibility`, bajo `#if UNITY_ANDROID && !UNITY_EDITOR`), en el hilo de UI de la actividad (`runOnUiThread` con un `AndroidJavaRunnable`). API 30 o más: `window.getInsetsController()`, `hide(WindowInsets.Type.systemBars())` y `setSystemBarsBehavior(BEHAVIOR_SHOW_TRANSIENT_BARS_BY_SWIPE)`: si el usuario las revela deslizando, se vuelven a ocultar solas; mostrar: `show(systemBars())`. API 26 a 29: `decorView.setSystemUiVisibility` con `IMMERSIVE_STICKY \| FULLSCREEN \| HIDE_NAVIGATION \| LAYOUT_STABLE \| LAYOUT_HIDE_NAVIGATION \| LAYOUT_FULLSCREEN`, y al mostrar, solo los `LAYOUT_*` ("de borde a borde", `:39`). El *bootstrapper* deja apagado "Hide navigation bar" en los *Player Settings*, así Unity no pelea con estos pedidos | [Device-And]: NO VERIFICABLE sin el módulo de Android y el teléfono; su `[Ignore]` en `WorkoutDeviceChecksTests` |
| Windows, Editor (y macOS de escritorio) | `DesktopSystemUi`: no hace nada. La ventana queda como estaba: no pasa a pantalla completa y no cambian la barra de tareas, la barra de título, el tamaño ni la posición (la pantalla completa real es Q-88, del backlog) | [Device-Win] AC-APP-15-02; PlayMode: `Screen.fullScreenMode`, `Screen.width` y `Screen.height` iguales antes y después de entrar y salir |
| iOS | 10b: `prefersStatusBarHidden` y `prefersHomeIndicatorAutoHidden` con el *helper* Swift (ARCHITECTURE §13). En la 4b, `DesktopSystemUi` | [Apple] AC-APP-15-03, con su `[Ignore]` |

`SystemUiFactory` elige por `Application.platform`; `PlatformServices.SystemUi` la expone y `AppRoot` la pasa a `AppServices.SystemUi`.

### 4.3 Los márgenes: nada se mueve con las barras (AC-APP-15-01; DD4b-07)

Dos juegos de márgenes, y ninguno cambia cuando las barras se ocultan, vuelven o aparecen un momento por el deslizamiento:

- **los de la app** (`IDisplayMetrics.SafeArea`): con las barras, aunque no se vean. Los usan las pestañas, las barras superiores y Workout debajo del inmersivo. En Android, con API 30 o más, salen de `getInsetsIgnoringVisibility(systemBars() | displayCutout())`, que no depende de la visibilidad; con API 26 a 29, se conserva el último valor con las barras visibles mientras el inmersivo las oculta;
- **los del inmersivo** (`IDisplayMetrics.ImmersiveSafeArea`, nuevo): solo el recorte de la pantalla (`Screen.cutouts`). El HUD los usa desde el cuadro del *push* (`HudZones`, por `ShellLayout.ImmersiveInsets`).

En escritorio los dos son iguales (los de hoy), así que nada cambia en la PC. `SystemBarsTests.InsetsDoNotMoveWithTheBars` (PlayMode) cambia los márgenes "de las barras" de `FakeDisplayMetrics` con el inmersivo arriba y comprueba que no se mueven ni el HUD ni, al salir, Workout.

---

## 5. Capturas y videos (WP-B3; WP-D1 para los videos)

### 5.1 Antes de las nuevas: la regeneración única (hecha)

| Qué | Estado | Evidencia |
|---|---|---|
| Regeneración única de `it2`, `it3` e `it4a` con WP-B0 de la 4a (RowMate Numeric SC, K2) y el divisor de la banda, con dos corridas iguales (QA-V4a-03, -06; AC-WKT-17-04, -12; spec §7.1) | **Hecha** al empezar la 4b, antes de este diseño | [00-deuda-4a](00-deuda-4a.md) §7: `it4a` 49 tomas, `it3` 47, `it2` 41, 0 con problemas, 0 sondas que fallan; la corrida A (`<scratch>`) y la B (repo) iguales byte a byte salvo `durationMs` del `manifest.json`; la sonda del divisor (DD4b-01) en CAP-12 y CAP-15 de la 4a. **QA la verifica antes de WP-B1** (spec §12 punto 1) |

Este diseño no vuelve a regenerar esos conjuntos. Desde WP-B1, las tomas del HUD de `it4a` dejan de coincidir con la app (la barra de 52, el reloj, el chip): pasan a estar **reemplazadas** por las de `it4b` (§5.6).

### 5.2 Reglas de toma de la 4b

Las de la 4a (04a/02-tech-design §7.1, 04a/04-design-notes §8.1), las de 04b/00-deuda-diseno §4.16 y las de 04-design-notes §7.1, con estas precisiones:

1. **El instante es el de la spec** (spec §7.1, DN4b-25): la toma se hace exactamente en el instante del reloj de prueba que dice cada una (`AdvanceTo`), que **nunca es un segundo entero del motor**: las que caían ahí van a los +0,2 s (el rodillo de 180 ya terminó) y CAP-06 a los 30,25 s. Los instantes se escriben en **ms enteros** (`AdvanceToMs(40200)`), y las líneas de tiempo comparan en ms enteros (§3.1), así ningún borde depende de la coma flotante (QA-G4bT-07 (b)).
2. **La toma no espera: falla si algo la bloquea** (esperar movería el relleno y los pulsos). **Qué bloquea** tiene definición operativa (QA-G4bT-07; DT4b-19): `SnapshotTakeRule.Blockers(MotionDriver)` (B3) recorre los `IMotion` del `MotionDriver` y nombra cada uno que bloquea, en este orden:
   - **un `IMotion` de la 4b en una fase finita**: cada uno dice `IsFinite` por fase (§7.4). Las transiciones son finitas (la cascada, la salida, el cambio de paso y el pulgar que pasa, el cambio de modo de la fila y la pastilla que se va, la entrada y la salida de un disco y el cambio de disco, el sello creciendo o cerrándose, el velo, el cambio del chip, kcal, la entrada del diálogo con su check y su conteo). **No lo son** el sostén del sello (300-1800), la cuenta grande entre ticks (el aro es continuo), el halo, el punto en vivo, `PAUSA` quieto y el diálogo ya contado;
   - **una animación de duración fija de la 4a** (`TimedMotion`: fundidos, cruces de color, la entrada de `Próximo`) que no llegó a su final (`HudTimelines.Reached`);
   - **un rodillo de `TabularNumber` antes de su mitad** (`RollProgress < 0,5`): con el remo `R-4` la lectura llega en k − 0,5 y en las tomas de "k,6 s" los rodillos de la banda y de las tarjetas van por 100 de 180 (0,556), con la cifra nueva casi en su lugar y la misma en las dos corridas;
   - **un resorte de la 4a a más de 0,15 dp de su destino** (`ISettlingMotion.RemainingDp`: el ancho de `Próximo` y los de `FitBox`; la escala de la cuenta, por su ancho). En CAP-03, -04, -26 y -30 (a +0,6 s del cambio de paso) el ancho de `Próximo` va a 0,034 dp del suyo (ω 12, 5,5 dp; se asienta en 0,79 s) y la escala de la cuenta a 0,106 dp a los +0,25 s de CAP-06: no bloquean (`<scratch>/it4b/arch-gate/check.py`).

   Lo continuo (el halo, el pulso de la fila, el punto en vivo, el corazón) no bloquea y va en la fase fija de su instante. Cada toma deja en el *manifest* `inFlight` con lo que corría y su progreso, y la falla dice qué la bloqueó.
3. **Los instantes que esto mueve** (QA-G4bT-07 (c)): DEMO-04 y DEMO-12 se toman a los **800 ms** de empezar la salida (la llave termina de entrar en el carril izquierdo a los 640 + 160), y DEMO-08 a los **101,6 s**, con el panel ya abierto y quieto. CAP-22 a los 3016,6 s (QA-G4bT-06: a los 3015,0 entraba `Próximo`).
4. **El relleno y el aro dependen del instante** (`PillProgress`): cada toma comprueba el valor esperado ± 0,005 (0,510 en CAP-01, 1 en CAP-06, 0,503 en CAP-21, 0,553 en CAP-22, 0,337 en CAP-23 y CAP-24, 0,607 en DEMO-05; el aro en 0,80 a los k,6 del 3 y en 0,267 en DEMO-10).
5. **La zona horaria de prueba** es UTC−03:00 (`UiHostOptions.LocalOffset`) y la sesión se crea a los `2026-10-28T13:00:00.000Z`: el reloj de pared dice `10:00` en el primer minuto.
6. **La interfaz del sistema de prueba** (`FakeSystemUi`) en cada anfitrión: ninguna toma llama al sistema.
7. **`HudOverlap`** suma la fila de píldoras (con el pulgar), la fila de información con su plan (§2.15), el reloj de pared, el chip, el sello (con el alto vivo de la barra) y el disco del lugar centro alto (la cuenta grande o `PAUSA`); **`WrenchAudit`**, lo mismo y el diálogo de completado (§5.7). Si fallan, la toma falla.
8. Español, modo simulador sin ruido, el pulsómetro simulado sin conectar, una base temporal propia (nunca `rower_app.db` ni `rower_app_dev.db`, AC-HIS-20-04), videos a 30 cuadros por segundo, y `manifest.json` e `INDEX.md` sin rutas de la PC.
9. **Dos corridas seguidas** de `it4b` (A a `<scratch>`, B al repo) con los mismos bytes en las fotos y en los videos (QA-V3-06); en `manifest.json` cambia solo `durationMs`.

### 5.3 El catálogo `it4b` (`Snapshots/HudExrCatalog.cs`)

Archivos `CAP-nn-<tema>.png`, `DEMO-nn-<tema>.png` y `VID-nn-<tema>.mp4` en `docs/migration/iterations/04b-hud-exr/snapshots/`. "Comprobaciones" = `Checks` (si fallan, la toma falla); las sondas de aspecto (`aspect: true`) son las de 04-design-notes §7.2 y §7.3 con las tolerancias de su §7.1 (fondos ± 1 por canal; trazos, bordes finos y el brillo ± 3; el halo ± 4; DN4b-28) y se anotan para WP-D2. La toma auxiliar AUX-01 de las notas pasa a ser CAP-31, en otro estado (spec §16.2). "Rutina R a los N s" = la tarjeta, `Iniciar` y `AdvanceTo(N)` desde la creación de la sesión.

| ID | Perfil · base · remo | Cómo se llega | Comprobaciones |
|---|---|---|---|
| CAP-01 | TV · `F-4B` · `R-4` | `Estados` a los 40,2 s | 5 píldoras con aspectos `Solid`, `Hollow`, `Solid`, `Current`, `Pending` y colores rosa, rosa, verde, rosa, verde; anchos iguales ± 1 px; la 3 con relleno 0,510 ± 0,005, su brillo y sin borde; `#hud-step-type` `Trabajo`, la pastilla `18 spm` **entera** (plan en compacto 1: los dos aires en 6, §2.15), `Próximo: Descanso`, `#hud-countdown` `0:10`, `#hud-wall-clock` `10:00` con `schedule`, la cuenta y el reloj pegados al borde derecho; SPM con `obj. 18`; la cinta sobre el comienzo de la barra |
| CAP-02 | TV · `F-4B` · `R-4` | `Estados` a los 47,6 s (p = 1) | `#hud-big-countdown` `3` en `HudLayout.BigCount` (167,6-243,4 × 234,0-309,9 ± 1), a 10,7 de Watts y fuera de la zona del bote; el aro en 0,80 ± 0,005, con el borde que se mueve en los 72° (en el sentido del reloj); el halo de la 3 en α 0,70 ± 0,01, solo por fuera y ningún píxel fuera de la fila; `#hud-countdown` `0:03` a escala 1,1 en `#FF5D7F`; `Próximo: Descanso` |
| CAP-03 | TV · `F-4B` · `R-4` | `Estados` a los 10,6 s | `#hud-seal` con `check_circle` relleno, `Objetivo cumplido` en `#06D6A0` y `24.0 / 18 spm` en blanco, en una línea, sin "…"; la barra en 74·s (± 0,5), hasta 86,4, y el cielo entre la barra y las tarjetas; la 0 sólida; la 1 en curso con relleno 0,060 ± 0,005 |
| CAP-04 | TV · `F-4B` · `R-4` | `Estados` a los 20,6 s | `error` de contorno y `No alcanzado` en `#FFD166` con `24.0 / 30 spm` en blanco; la 1 hueca, con el borde entero (el trazo terminó a los 440) |
| CAP-05 | TV · `F-LISTA` · `R-4` | `Pirámide 500` a los 725,6 s | `Objetivo cumplido · 150 / 150 W · 02:05 / 02:05/500m` entero (si hace falta, achicado, nunca con "…"); 14 píldoras del mismo ancho; la 0 y la 1 sólidas; la 2 en curso sin relleno (Q-51); `#hud-countdown` `250m` |
| CAP-06 | TV · `F-4A` · `R-4` | `Corta` a los 30,25 s | la 0 y la 2 sólidas rosas, la 1 sólida verde, la 3 en curso con relleno 1, su brillo y sin borde (no es la sólida al 85 %); `Descanso`, `0:00`; sin `Próximo`, halo, cuenta grande, sello ni diálogo (a los 30,2 justo terminaba la salida de la cuenta grande) |
| CAP-07 | TV · `F-4A` · `R-4` | `Corta` a los 32,0 s | `#completion-dialog` sobre el velo (40,0-371,0 × 298,0-616,0 ± 1); `#completion-check` a la izquierda del título; `¡Entrenamiento completado!` en dos líneas; `Has completado tu rutina exitosamente.`; `#completion-time` `00:30`, `#completion-distance` `1120` con la `m` de sufijo, `#completion-calories` `43`; `#completion-finish` `Terminar`; detrás, la barra de CAP-06; la cinta y la llave por encima del velo, la llave en x 16-56 × y 790,1-830,1 (R10, §5.7), fuera del diálogo |
| CAP-08 | TV · `en` | CAP-07 | `Workout Completed!`, `You have successfully completed your routine.`, `Time` `00:30`, `Distance` `1120 m`, `Calories` `43`, `Finish` |
| CAP-09 | TH | CAP-07 | el diálogo centrado y entero (257,0-657,0 × 63,2-347,8), con el título en una línea; detrás, el HUD horizontal; la llave en x 16-56 × y 270,3-310,3, sobre la banda |
| CAP-10 | L640 · letra 1,3 | CAP-07 | el diálogo entra o desplaza su contenido, con `Terminar` a la vista; `Audit()` sin cortes |
| CAP-11 | W1280 · escritorio | CAP-07 | a escala de escritorio (440,0-840,0 × 209,7-510,3); detrás, las píldoras finales; la llave en su lugar del HUD (no se cruza con el diálogo) |
| CAP-12 | TV · `F-4B` · `R-4` | `Final por distancia` a los 35,2 s | la 0 sólida; la 1 en curso con la pista sola (relleno 0) y sin borde; `100m`; sin sello |
| CAP-13 | TV · `F-LISTA` | libre a los 42,2 s | `#hud-free-title` `Entrenamiento Libre` **entero**, a escala 0,818 ± 0,005 (`FreeTitleScale`, sin "…"), `#hud-wall-clock` `10:00`, `#hud-phase-chip` `EN MARCHA` de 101,8·s ± 0,5 con `#hud-phase-dot` (0,444 de opacidad); la sonda del divisor de la banda (`#263241` ± 2, DD4b-01); el `1` de la derivada; lo de CAP-12 de la 4a |
| CAP-14 | TV · `F-LISTA` | libre: `Pausa` a los 42 s, toma a los 44 s | el fondo: cielo `#09438F` y agua `#0C437A` ± 1 (DD4b-14); `#hud-paused-label` en el disco de `HudLayout.BigCount` (± 0,5), con el aro en `phase.paused`, el glifo y `PAUSA`, fuera de la zona del bote y sin cruzarse con Watts (`HudOverlap`); el chip `PAUSADO` en `#FFD166`, sin punto; `#hud-resume`; las tarjetas y la barra con opacidad 1; `#hud-time` `00:42` |
| CAP-15 | TV · `F-LISTA` · base lenta (`WriteDelay` 2 s) | libre a los 10 s, `Terminar` → `Terminar`, toma 1 s después | el chip `FINALIZADO` con el texto en `#FF7A92` (`phase.finished.hud`, DV-44) y sin punto; el chip del mismo ancho que en `EN MARCHA`; los controles al 0,38; el *spinner* de `Terminar`; ningún `#completion-dialog` |
| CAP-16 | TH · pulsómetro simulado | libre a los 42,2 s | la barra libre con el título a su tamaño, el reloj y el chip enteros; la fila de abajo como CAP-15 de la 4a; `HudOverlap` |
| CAP-17 | W1280 · escritorio | CAP-16 | escala 1,6; el reloj y el chip a la derecha de la barra; las zonas de `docs/image-03.png` sin la escena |
| CAP-18 | TV · `en` | CAP-13 | `Free Workout` entero y a su tamaño (escala 1), el reloj, `ACTIVE` con el punto, en un chip de 74,1·s ± 0,5 |
| CAP-19 | TV · `F-4B` | `Texto largo` a los 8,2 s | `Calentamiento`, `Próximo: Enfriamiento` (relleno 4 × 3), `0:12` y `10:00` enteros, con los aires en 6 (plan en compacto 2, §2.15); **sin pastilla** (`#hud-step-target` fuera del árbol visible: se fue al entrar `Próximo`); nada sale de la barra (`HudOverlap`) |
| CAP-20 | TV · `en` | CAP-19 | `Warmup`, `Up next: Cooldown`, la cuenta y el reloj enteros, con los aires normales; **sin pastilla** (ni `>150W…` entra junto a `Up next`) |
| CAP-21 | L640 · `F-4A` · `R-4(23)` | `Prueba 4a` a los 150,2 s | 6 píldoras: la 0 sólida amarilla, la 1 hueca rosa, la 2 sólida verde, la 3 en curso (relleno 0,503 ± 0,005), la 4 y la 5 pendientes; la pastilla entera; SPM y Watts fuera, con `▲` (prefijo del número, M1); `HudOverlap`; `WrenchAudit` |
| CAP-22 | TV · `F-4B` · `R-4` | `Serie larga` a los 3016,6 s | la fila en tira (`Strip`), 200 tramos en orden; el 100 en curso como **pulgar** (`ThumbOf`: 4·s × 10·s ± 0,5, más alto que la franja) con relleno 0,553 ± 0,005 y su brillo; tres sondas de alfa (un hecho, el en curso, un pendiente) distintas; `Trabajo`, `Próximo: Descanso` (entró a los 3015,0 y está quieto), `0:14`, `#hud-wall-clock` `10:50` |
| CAP-23 | TH · `F-4A` · simulador | `Prueba 4a` a los 50,2 s | 6 píldoras del mismo ancho, la 1 en curso (relleno 0,337 ± 0,005); la fila con la pastilla entera y el reloj; `HudOverlap` |
| CAP-24 | W1280 · escritorio | CAP-23 | comparable con `docs/image-03.png` (un paso sin pastilla): las píldoras arriba, `Trabajo` y la pastilla entera a la izquierda, y la cuenta y el reloj pegados a la derecha (DV-46) |
| CAP-25 | TH · `F-4B` · `R-4` | `Estados` a los 47,6 s | el halo y la cuenta grande `3` en horizontal, fuera de la zona del bote |
| CAP-26 | TH · `F-4B` · `R-4` | `Estados` a los 20,6 s | el sello `No alcanzado` sin tapar Split ni SPM (`HudOverlap` con el alto vivo) |
| CAP-27 | TV · `F-4B` · `R-4` | `Estados`: `Pausa` a los 47,0 s y la toma a los 47,6 s | el velo y el disco de `PAUSA` quieto (entró a los 47,12, un disco por vez); sin `#hud-big-countdown`; `0:03` a 1,1 y teñida (p = 1); el halo |
| CAP-28 | TV · `F-4A` · base que falla al cerrar (`FailOnFinish`) | `Corta` con `R-4`; a los 32 s, `Tap(completion-finish)` y `Settle` | Workout en reposo; `#toast` `No se pudo guardar la sesión`, entero; ningún diálogo |
| CAP-29 | L640 · `F-4B` · `R-4` | `Estados` a los 47,6 s | la cuenta grande y el halo en la ventana baja: sin tapar tarjetas ni la zona del bote |
| CAP-30 | L640 · `F-4B` · `R-4` | `Estados` a los 20,6 s | el sello en una línea, en la tercera fila (y 54,5-70,2), entre la barra y las tarjetas (desde 83,1), sin cortarse |
| CAP-31 | TV · `F-4B` | `Texto largo` a los 2,2 s (18 s del final, todavía sin `Próximo`) | el plan en compacto 2: `Calentamiento`, la pastilla `>150W · 24 spm…` (dos fragmentos enteros, ningún número cortado) con el "…" pegado en blanco (sonda del trazo `#FFFFFF` ± 3, DD4b-03), `0:18` y `10:00`, con los aires en 6; nada sale de la barra (`HudOverlap`). Es el paso de CAP-19 antes de que entre `Próximo` (AC-WKT-14-03, -15) |

### 5.4 La demo (DEMO-01 a -12, una secuencia)

W1280, español, modo simulador sin ruido, `F-4A`, escritorio, reloj de prueba y zona UTC−03:00. Cada toma sigue a la anterior sin rearmar nada (`SnapshotKind.Sequence`).

| ID | Paso | Comprobaciones |
|---|---|---|
| DEMO-01 | `Tap(tab-workout)` | Workout en reposo con `Entrenamiento libre` y `Prueba 4a` |
| DEMO-02 | libre, `AdvanceTo(3.2)` | el HUD entero (la cascada terminó a los 1,12 s); `10:00` y `EN MARCHA` con el punto en 1 |
| DEMO-03 | `Tap(hud-pause)` y 0,5 s | el velo, `PAUSA`, `PAUSADO` y `Reanudar` |
| DEMO-04 | `Tap(hud-resume)`, `Tap(hud-finish)`, `Tap(dialog-confirm)` y la toma a los 800 ms de empezar la salida | Workout en reposo, sin diálogo ni aviso; la llave quieta en el carril izquierdo |
| DEMO-05 | `Prueba 4a` → `Iniciar`, `AdvanceTo(18.2)` | 6 píldoras; la amarilla con relleno 0,607 ± 0,005 y su brillo; `Calentamiento`, `Próximo: Trabajo`, `0:12` y el reloj |
| DEMO-06 | `AdvanceTo(27.6)` | el halo de la 0 y la cuenta grande `3` (p = 1: el reloj común arrancó a los 15,0) |
| DEMO-07 | `AdvanceTo(90.6)` | `No alcanzado · 150 / 160 W · 24.0 / 22 spm`; la 1 hueca y la 2 empezando |
| DEMO-08 | `AdvanceTo(100)`, `sim-open`, `sim-watts-plus` ×2, `sim-spm-plus` ×2, `AdvanceTo(101.6)` | el panel abierto y quieto, con Split, Watts y la barra a la vista. El sello ya se fue (se vio de los 90 a los 92): el panel termina 8 dp debajo de la barra en su alto mayor (74·s; y 139,2), así no tapa su lugar, y su alto máximo es 530,4 (AC-BLE-16-03, §5.7) |
| DEMO-09 | `sim-close`, `AdvanceTo(180.6)` | `Objetivo cumplido · 170 / 160 W · 26.0 / 24 spm`; la 3 sólida |
| DEMO-10 | `AdvanceTo(239.2)` | la cuenta grande `1` y el halo de la 5 (p = 1: el reloj común arrancó a los 235,0); sin `Próximo` |
| DEMO-11 | `AdvanceTo(242)` | el diálogo con `04:00`, la distancia y las kcal del monitor; detrás, la 1 hueca, la 3 sólida y la 5 llena, en curso |
| DEMO-12 | `Tap(completion-finish)` y la toma a los 800 ms de empezar la salida | Workout en reposo, sin aviso |

### 5.5 Los videos (VID-01 a -08)

Los graba el arnés (`VideoRecorder`, 30 cuadros por segundo) con el reloj de prueba; además del `.mp4`, cada uno deja en el *manifest* los valores por cuadro de sus piezas (los que devuelven los `IMotion` de WP-D1), y sus comprobaciones los comparan con las líneas de tiempo puras (± 0,01; las subidas ± 0,1 dp) y miran que ninguna caja salte entre dos cuadros (`HudOverlap` por cuadro en las piezas que no se mueven).

| ID | Tamaño, base y duración | Qué se hace | Comprobaciones |
|---|---|---|---|
| VID-01 | TV · `F-LISTA` · 2,5 s | Workout; `Tap(workout-free)` a los 0,25 s del video | la página negra 0 → 1 en 220; el fondo en 220-400; la cascada (400, 520, 600, 680, 760, 840; 280 cada una; +8 → 0); la banda con el reloj real y `00:01` antes de que termine la cascada; la llave entra a los 1120 |
| VID-02 | TV · `F-LISTA` · 2,5 s | libre a los 12 s; `Terminar` → `Terminar` | la salida con la última foto: `00:12` y `EN MARCHA` o `FINALIZADO` en todos los cuadros, nunca `00:00` ni `—`; el negro; Workout con su barra de pestañas; sin aviso |
| VID-03 | W1280 · `F-4A` · 2,5 s | Workout → `Prueba 4a` → `Iniciar` | la barra de pasos con sus 6 píldoras llega primero; la cuenta ya corre |
| VID-04 | TV · `F-4B` · `R-4` · 18,5 s | `Estados` de los 4,5 a los 23 s | el halo desde `0:05`; la cuenta grande `3`, `2`, `1`; a los 10 s la 0 se solidifica con el destello y la 1 empieza a llenarse; `Objetivo cumplido` entra y se va a los 12; a los 20 s el borde de la 1 se dibuja y entra `No alcanzado`; el reloj de la banda, la cuenta y la cuenta grande cambian en el mismo cuadro |
| VID-05 | TV · `F-4A` · `R-4` · 8 s | `Corta` de los 26 a los 34 s; `Terminar` del diálogo a los 33 s | la cuenta grande del último paso; a los 30 s la última píldora completa su relleno y no se solidifica; a los 30,3 entra el diálogo: el check se dibuja y los totales cuentan hasta `00:30`, `1120 m` y `43`; `Terminar` → la salida |
| VID-06 | TV · `F-LISTA` · 5 s | libre: `Pausa` a 1 s; `Reanudar` a 3 s | el velo en 300, `PAUSA` entra, el chip cambia en el cuadro del toque; al reanudar todo vuelve en 240; el reloj de la banda quieto en pausa |
| VID-07 | TV · `F-LISTA` · pulsómetro simulado · 6 s | libre; llave → `Simular caída del pulsómetro` a 1 s | BPM se va y kcal baja deslizándose; a los 3 s de la caída BPM vuelve y kcal sube; nada salta |
| VID-08 | TV · `F-LISTA` · reducir movimiento · 2 s | VID-01 | un fundido lineal de 120, sin negro ni cascada; el reloj corre igual |

### 5.6 Las tomas de `it4a` que reemplaza `it4b`

Las del HUD de `it4a` (CAP-12 a CAP-30, la secuencia DEMO-01 a -14 y VID-01 a -03 de la 4a) muestran la barra de 40 sin píldoras ni reloj, el aviso `workoutCompleted` y la salida sin la última foto: con la 4b ya no son la app. `WorkoutCatalog` las marca `SupersededBy("it4b", "<IDs que las reemplazan>")`: `capture-snapshots.ps1 -Set it4a` no las vuelve a tomar, conserva los archivos de la 4a como registro y las lista en el `manifest.json` como `"superseded": "it4b"`. Las demás (CAP-01 a CAP-11 de la 4a: Workout en reposo y el diálogo de inicio) se siguen tomando y tienen que dar los mismos bytes. `SnapshotEvidenceTests` (C) acepta una toma reemplazada sin su archivo nuevo y exige el reemplazo en `it4b` (DT4b-07).

### 5.7 La llave del simulador con las piezas nuevas (AC-BLE-16-02; D-ART-10)

- **Durante la cascada** la página declara una superficie "oculta" (`OverlaySurface.Hidden`): la llave sale en el cuadro 0 (90) y no se ve hasta `ImmersiveEntryEndMs`, cuando la página declara su lugar del HUD y la llave entra (160; con el panel abierto, el panel se abre ahí). En la salida se va en el cuadro 0 y entra en el carril izquierdo al terminar (ANIMATION_BIBLE §2.11).
- **Con el diálogo de completado** (R10, en `WrenchPlacement`, dotnet; DN4b-22, que reemplaza a DT4b-15): si el lugar del HUD, agrandado 8, se cruza con el rectángulo del diálogo, la llave va al **hueco libre más bajo del carril izquierdo** (x 16-56), a 8 dp de toda pieza del HUD (siguen a la vista debajo del velo) y del diálogo, mientras el diálogo está abierto. "16 + margen inferior" de la primera versión se encimaba con `Terminar` en TV y P360 y con la banda en TH y L640. La llave queda por encima del velo y se puede usar; cambia de lugar con el fundido del cambio de carril (sale en 90 y entra en 160 con 0,85 → 1, cuando termina la entrada del diálogo).

  | Perfil | Su lugar del HUD | Con el diálogo (04-design-notes §3.9) |
  |---|---|---|
  | TV | 355,0-395,0 × 575,9-615,9 | x 16-56 · y 790,1-830,1, entre la banda (766,5) y los controles (838,1) |
  | P360 | 304,0-344,0 × 497,9-537,9 | x 16-56 · y 685,5-725,5 |
  | TH | 618,9-658,9 × 344,6-384,6 | x 16-56 · y 270,3-310,3, sobre la banda |
  | L640 | 414,8-454,8 × 245,5-285,5 | x 16-56 · y 230,8-270,8, sobre la banda |
  | W1280 | 881,6-921,6 × 629,6-669,6 | no se cruza: queda en su lugar |

  A 1,3× es igual (los rectángulos del diálogo a 1,3 no llegan al carril izquierdo). Las piezas del carril las da `HudLayout.LeftLaneObstacles` (`HudGeometry`, WP-A: los rectángulos de las tarjetas, la banda, kcal y BPM y los controles que se cruzan con x 16-56) y la página las declara con el diálogo abierto:

  ```csharp
  // OverlaySurface (C): el inmersivo con el diálogo abierto. leftLane: las piezas del HUD que se cruzan con el carril izquierdo
  // (dp de ventana, y desde ARRIBA); dialog: el rectángulo del diálogo. Sin diálogo, los dos null (lo de la 4a).
  public static OverlaySurface Workout(DpRect wrenchSlot, double? panelCeiling, IReadOnlyList<DpRect>? leftLane = null, DpRect? dialog = null);

  // WrenchPlacement.For (C), R10: con surface.Dialog que se cruza con WrenchSlot agrandado 8, el y más bajo del carril
  // izquierdo donde la caja de 40, agrandada 8, no toca ninguna pieza de LeftLane ni el diálogo, dentro del área segura.
  // Si no hay hueco (no pasa en los cinco perfiles), "16 + margen inferior" y una línea en el registro.
  ```
- **El techo del panel en el entrenamiento** (AC-BLE-16-03, que la spec trae a §5.2; DN4b-23): `HudLayout.PanelCeiling` (`HudGeometry`, WP-A) pasa a valer en las dos orientaciones. **En vertical**, 8 dp debajo del disco del lugar centro alto (`HudLayout.BigCount`, que está debajo de Watts con su objetivo; en la 4a, 8 debajo de Watts); **en horizontal**, 8 dp debajo de la barra en su alto mayor (74·s, con el sello; en la 4a no había techo y el panel tapaba el medio de la barra). Así el panel abierto no tapa el sello ni el lugar de la cuenta grande y de `PAUSA`, y su alto no cambia con el paso, con el sello ni con la cuenta. La fórmula de `SimulatorPanelLayout.MaxHeight` no cambia: recibe el techo. Los altos máximos en el entrenamiento: **TV 298,0** (era 316,4), **P360 258,5** (274,6), **TH 290,2** (368,6), **L640 201,8** (269,5) y **W1280 530,4** (653,6); `SimulatorPanelLayoutTests.WorkoutCeilingFiveHeights` y `HudGeometryTests.PanelCeiling` los repiten ± 0,1. Fuera del entrenamiento, sin cambios.
- **`WrenchAudit`** (C) suma la fila de píldoras, el reloj, el chip, el sello, el disco del lugar centro alto (la cuenta grande o `PAUSA`) y el diálogo de completado; corre en toda toma con simulador.
- **DD4b-04**: el panel abierto sigue a la llave sin saltos entre una pestaña y una pantalla apilada (el alto máximo con el borde de abajo de cada cuadro), en WP-B0.

---

## 6. Tests

### 6.1 Suites y comandos

| Suite | Qué | Comando (desde la raíz del repo) |
|---|---|---|
| dotnet | Core (píldoras, fila, relleno, cuenta grande, sello, chip, líneas de tiempo, geometría, el reloj despierto, el programador), App (los *stores*, el reloj de pared, las reglas del diálogo y de las barras), el motor con el ciclo de vida de prueba, el oráculo, el simulador (la llave) | `powershell -NoProfile -File tools/dotnet/run-tests.ps1 -ArtifactsPath <scratch>/it4b/<carpeta>` (y `-Matrix -UnityResults …`) |
| Unity EditMode, con y sin simulador | las mismas fuentes; `Tests/UnityEditMode` (el programador de Unity con la fuente de sueño, la fuente de Windows, el offset local) | `tools/unity/run-unity-tests.ps1 [-Simulator]` |
| Unity PlayMode, con y sin simulador | la UI montada con la base, el reloj, la zona, el ciclo de vida (con el despertar), la interfaz del sistema y el remo de prueba | ídem |
| Arnés | §5 | `tools/unity/capture-snapshots.ps1 -Set it4b` |
| Oráculo del motor | §2.13 (solo para regenerar los vectores) | `powershell -NoProfile -File tools/oracle/run-oracle.ps1 -Engine` |
| Builds | simulador Mono a `Builds/Windows/simulator` (la carpeta por defecto) y normal Mono; IL2CPP NO VERIFICABLE sin el módulo | `tools/build/build.ps1 …` |

Dobles nuevos o cambiados (Tests/Support, nunca en un build): `FakeAppLifecycle.SignalWake`, `LifecycleHarness.Suspend(instante, signal)` y `.Wake(segundos, monotonicCounts, measured)` (con `SuspendAccounting` en el camino del `FrameScheduler`, en los dos casos del monótono), `.SetLocalOffset` y `.ShiftWallClock`; `FakeScheduler.SetLocalOffset` y `.ShiftUtc`; `FakeSystemUi`; `FakeDisplayMetrics.ImmersiveSafeArea`; `RoutineSeeds.F4B`; `RowerScripts.R4With(spm, watts)` y `R4X` (= `Extremes`). `ScriptedSessionRepository` ya tiene `WriteDelay`, `FinishDelay` y `FailOnFinish`.

### 6.2 Archivos de test

**dotnet + Unity EditMode** (WP-A, `Tests/EditMode/`, salvo lo marcado):

| Archivo | Abrev. | Cubre |
|---|---|---|
| `Workout/StepPillsTests.cs` (N) | PIL | LT-46 (las cinco píldoras armadas con `LookOf`, §6.3; QA-G4b-P08); AC-WKT-15-01 a -05 (la regla), AC-WKT-14-08 (el aspecto por índice), -09 (qué se llena), -13 (Q-52); el brillo solo en la en curso con relleno |
| `Design/PillRowGeometryTests.cs` (N) | PRG | AC-WKT-14-07 (relleno y alto), -08 (mismo ancho ± 1 px en los cuatro perfiles con 6 y 14 pasos), -16 (la separación que baja, los umbrales de §2.2, la tira con 200 y con 1998 pasos; **el pulgar** `ThumbOf` (≥ 4 × 10, centrado y acotado a la fila) desde 50 pasos en TV, 118 en TH, 93 en L640 y 108 en W1280, en la separación 1 y en la tira; `Narrow` en 49 → falso y en 50 → verdadero en TV); AC-WKT-15-05 (los rieles de la hueca angosta); `StripRuns`: a lo sumo una columna por píxel con 1998, con los colores pesados por lo que cubre cada tramo |
| `Design/PillProgressTests.cs` (N) | PPG | AC-WKT-14-10 (la regla), AC-WKT-13-14 (nunca pasa del tick siguiente): la tabla de §2.3 con su grilla de 1/60 s (3,560), nunca baja dentro de un paso, quieto en pausa, espera al motor al reanudar, el cambio de paso en el acto, **`Instant` en el mismo paso nunca retrocede** (los dos casos de §2.3: despertar a los 62,4 s y pausa con segundo plano; QA-G4bT-04), sin relleno por distancia o con D ≤ 0; los rellenos y el aro de los instantes de las tomas (0,510, 0,503, 0,553, 0,337, 0,607; 0,80 y 0,267) |
| `Design/PillColorVisionTests.cs` (N) | CVD | AC-WKT-15-06: los cuatro aspectos sobre el vidrio, pasados por las matrices de deuteranopía y protanopía, se distinguen por luminancia y forma (borde, relleno) |
| `Workout/BigCountdownTests.cs` (N) | BCD | AC-WKT-14-18 (cuándo y qué número): las ventanas de `Estados` y `Final por distancia`, nada en pausa, libre, terminada, por distancia, sin meta, 0 s ni duración negativa; el último paso sí |
| `Workout/StepSealTests.cs` (N) | SEL | AC-WKT-09-08 (cuándo y cuál: 10, 20 y 50 s de `Estados`; nunca el último, a mano, en libre ni sin objetivos), -09 (los textos y todos los casos de §2.5, `21.999999999999996` → `21.9`, `--` y `--:--`, el orden de la pastilla, los vatios máx. no cuentan) |
| `Workout/StepTargetTrackerTests.cs` (C) | STT | `OutcomeOf` con los promedios y las cuentas; LT-39 a LT-44 siguen |
| `Workout/CompletionHoldTests.cs` (N) | CHT | AC-WKT-21-01 (el motor, `EndedBy`, `Session`), -04 (los totales del punto final), -06 (`AcknowledgeCompletion` con el cierre lento, con el que falla, el tope de 15 s, el cierre tardío que vuelve bien antes de `Terminar`, cerrar la app con el diálogo abierto), -07 (a mano nunca hay `Completion`), -08 (terminada hasta `Terminar`: sin puntos, con lecturas, con los resultados), -09 (`Corta` en segundo plano de los 8 a los 40 s y de los 8 a los 330 s); AC-WKT-01-06, -03-05, -06-12, -02-06 |
| `Workout/CompletionDialogRuleTests.cs` (N, App) | CDR | AC-WKT-21-01, -07, AC-WKT-13-11: una vez por sesión, nunca a mano ni en libre, una sesión nueva tiene el suyo, sin entrada con `Instant` |
| `Workout/PhaseChipTests.cs` (N) | PCH | AC-WKT-16-03 (texto y colores por fase, es / en; `FINALIZADO` en `phase.finished.hud` `#FF7A92`, y el par B1 con `Contrast`: 5,324 · 6,432 · 4,677 ± 0,001), -04 (reposo sin chip), -05 (el punto, solo en marcha); `FixedWidthBase` con los anchos de 04-design-notes §3.5 (101,8 y 74,1 ± 0,1; el lugar del punto solo en `EN MARCHA`) |
| `Workout/WallClockTickerTests.cs` (N, App) | WCL | AC-WKT-19-01 (`HH:mm` de 24 h), -02 (el minuto antes de 1 s; nada entre minutos), -03 (en pausa, terminada y con el diálogo; `Instant` al volver y al despertar; **sin periódico en segundo plano**: 0 disparos durante un alcance de 1 h; QA-G4bT-08 (c)), -04 (la zona de UTC−03:00 a UTC−05:00 y el cambio a mano; el reloj de la sesión no cambia) |
| `Common/SuspendAccountingTests.cs` (N) | SUS | AC-WKT-02-11, -04: con medida y crudo que cuenta, con medida y crudo que no cuenta, aviso y hueco, aviso y salto de la hora de pared, aviso que vence, ruido bajo el umbral, cuadro tarde sin evidencia (cuenta), salto de la hora sin aviso (cambio de hora), el despierto nunca baja; **QA-G4bT-05**: el "reanuda" antes y después del primer cuadro despierto (ancla el despertar), un tirón de 1,2 s entre el aviso y el sueño (se descuenta y el sueño de después también), la vuelta por `ResumeFromBackground(in FrameClock)` con el mismo `Awake` |
| `Workout/WorkoutSuspendTests.cs` (N) | WSU | AC-WKT-02-11 con `LifecycleHarness`: el libre suspendido a los 62,4 s por 600 s en los dos casos del monótono × aviso / medida / los dos → `01:02`, los puntos de 5 a 60 y después 65…, 25 puntos (no 145); en pausa sigue en pausa; el diálogo sigue abierto; ningún `workoutSaveError`; los objetivos sin lecturas del lapso; el paso por distancia sin el salto; **la suspensión durante el segundo plano con la vuelta por `ResumeFromBackground`** (25) y **el tirón antes de dormir** (25; QA-G4bT-05); sin evidencia, 145 (documenta AC-WKT-02-04) |
| `Workout/ImmersiveSystemBarsTests.cs` (N, App) | SBR | AC-APP-15-01 (la regla), -04: los casos de §4.1 en orden, con sus instantes |
| `Workout/PresentationIndependenceTests.cs` (N) | PIN | AC-WKT-13-14: `Estados`, `Corta` y `Prueba 4a` con `R-4`, con y sin el *store* del HUD y el reloj de pared: la misma secuencia (fase, reloj, paso, restante, puntos con sus valores, resultados, instante del fin) |
| `Design/HudTimelinesTests.cs` (N) | HTL | AC-WKT-13-06 (la entrada de la spec, cuadro a cuadro: la tabla de 04-design-notes §4.1), -08 (la salida, §4.2; `RevealStartMs` 400 o 0), -12 (el velo, §4.8), AC-WKT-14-11 (el halo, su rampa y el 0,65 del borde contra el `BoxShadow` del legacy, §3.2 y §4.4 de las notas), -12 (el cambio de paso, §4.5: el brillo que se va), -16 (**el pulgar que pasa**), -15 (**el cambio de modo de la fila y la pastilla que se va**), -18 (la cuenta grande, §4.6, y **un disco por vez**, §4.8), AC-WKT-09-08 (el sello dentro de los 2 s y el reemplazo, §4.7), AC-WKT-16-05 (el punto, que empieza en 1, §4.9), AC-WKT-17-11 (kcal, §4.11), AC-WKT-21-05 (el check y `CountUp`, §4.12, que termina en el valor exacto), AC-APP-05-08 (`ReducedVariants`: cada fila de §3.10, **con el destello en 0 en todo el cambio de paso**; QA-G4bT-03); `Reached` y `Ms` en los bordes (30,2 − 30,0 → 200) |
| `Workout/WorkoutHudStoreTests.cs` (C) | WHS | los campos nuevos de §2.11 en es / en (píldoras con la caché, sello, cuenta grande, chip, diálogo); `Instant` al despertar |
| `Workout/WorkoutStoreTests.cs` (C) | WST | AC-APP-03-11, AC-WKT-02-08 (avisos retenidos con el diálogo de completado), AC-WKT-03-05 (sin `workoutCompleted`), AC-WKT-06-12 (`workoutSaveError` después de `Terminar`) |
| `Workout/WorkoutGoldenTests.cs` (C) | WGD | AC-WKT-21-13 (§2.13) |
| `Design/HudGeometryTests.cs` (C) | HGT | AC-WKT-14-01 (52 / 74 / 40; las tarjetas no se mueven), -18 (el disco del lugar "centro alto" en los cinco perfiles: los rectángulos de §3.5 ± 0,1, aire ≥ 8·s, fuera de la zona del bote), AC-WKT-13-12 (`PAUSA` en el mismo disco, `HudLayout.BigCount`), -13 y AC-WKT-17-08 (ninguna pieza nueva se cruza: 5 perfiles × los casos de valores × con y sin simulador), AC-WKT-09-08 (el sello a 8·s de las tarjetas), AC-BLE-16-03 (`PanelCeiling` en las dos orientaciones, §5.7), AC-BLE-16-02 (`LeftLaneObstacles`) |
| `Design/InfoRowFitTests.cs` (N) | IRF | AC-WKT-14-03 (los fragmentos enteros, la tendencia como uno, el "…" pegado, nunca un número cortado), -04 y -14 (los aires y el relleno de cada modo), -15 (los casos de §2.15 con los anchos de 04-design-notes §3.3: CAP-01 en compacto 1, CAP-31 con dos fragmentos, CAP-19 sin pastilla, CAP-20 en normal; el plan no cambia dentro del paso), AC-WKT-16-06 (`FreeTitleScale`: 0,818 en TV y 360 en español, 1 en inglés y en horizontal, el tope de 0,8) |
| `Design/MotionSpecsTests.cs` (C) | MSP | las constantes de §3.1, con las de las notas (§4.15) |
| `Localization/CatalogParityTests.cs`, `L10nKeysTests.cs` (C) | CAT | AC-APP-10-11: 214 claves |
| `Common/FrameSchedulerTests.cs` (C) | FSC | `Tick(in FrameClock)` con el monótono despierto; `Woke` después de lo vencido; `Tick` de la 4a igual |
| `Project/PureCodeRulesTests.cs` (C) | PCR | los archivos nuevos de Core y App sin reloj del sistema |
| `Traceability/It04bIds.cs` (N), `TraceabilityTests.cs` (C) | ITB | los 101 IDs y LT-46, con sus `UnityOnlyIds` (§6.4) |
| `Workout/WorkoutDeviceChecksTests.cs` (C) | WDC | `[Ignore("NO VERIFICABLE: …")]` con la categoría del AC: AC-APP-15-01 (Android), AC-APP-15-03 ([Apple], 10b), AC-WKT-13-07 y AC-WKT-21-02 ("Atrás" de Android), AC-WKT-02-11 (en equipo: lo verifica el usuario), AC-WKT-02-08 (Android) |
| `Project/SessionListToolTests.cs` (C) | SLT | AC-HIS-20-04 |
| `Tests/Simulator/WrenchPlacementTests.cs` (C) | WPL (s) | AC-BLE-16-02 (la regla): R10 con el diálogo de completado en los cinco lugares de §5.7 (± 0,1), también a 1,3×; el lugar de la llave libre de las piezas nuevas en los cinco perfiles |
| `Tests/Simulator/SimulatorPanelLayoutTests.cs` (C) | SPL (s) | DD4b-04: el alto máximo con el borde de abajo de cada cuadro (L640: 32 ± 0,5 en cada cuadro del resorte; 312 y 248 al final); AC-BLE-16-03 (`WorkoutCeilingFiveHeights`: 298,0 · 258,5 · 290,2 · 201,8 · 530,4 ± 0,1; fuera del entrenamiento, sin cambios) |

**De WP-B3, en `Tests/EditMode/Project/`** (corren con dotnet; excepción de dueño, §7.6): `SnapshotEvidenceTests` (C, SEV: cada toma `superseded` de `it4a` tiene su reemplazo en `it4b`; CAP-01 a CAP-11 de la 4a con los mismos bytes; DT4b-07) e `ItFourBToolingTests` (N, ITO: `capture-snapshots.ps1 -Set it4b` por defecto, `BuildCli` restaura `enableFrameTimingStats`, el *manifest* sin rutas de la PC).

**Unity EditMode** (WP-B2, `Tests/UnityEditMode/`): `UnitySchedulerTests` (C, USC: la fuente de sueño inyectada en el cuadro **y en la vuelta del segundo plano**, el `Woke` al ciclo de vida, el offset cada 0,5 s), `SystemSleepSourceTests` (N, SSL: en el Editor de Windows, la medida nunca baja y **el aviso no se registra** (QA-G4bT-05 (c)); el orden del registro (reservar la estructura con `AllocHGlobal`, registrar, y en `Dispose` dar de baja y recién después liberar) se prueba con un doble de las llamadas nativas (`IPowerNotifyApi`, interno de Platform), sin registrar nada en el sistema; el registro real lo mira QA en el build ([Device-Win])), `LocalOffsetSourceTests` (N, LOS: el offset de Windows es el de `TimeZoneInfo` sin cambios de zona).

**PlayMode** (WP-B y WP-D1, `Tests/PlayMode/`; con simulador, `Tests/PlayModeSimulator/`):

| Archivo | Abrev. | Cubre |
|---|---|---|
| `PillRowTests.cs` (N, B1) | PRT | LT-46 montado (`PillRowView` sola en 400 dp, con las cinco píldoras de §6.3 armadas con `LookOf`: lo que dibuja la vista y sondas de color); AC-WKT-15-01 a -05 en pantalla (CAP-01 a los 40,2 s), -03 (sin marco; el brillo); AC-WKT-14-07 a -13 (el halo solo por fuera y sin un píxel fuera de la fila), -16 (el pulgar y los rieles; el pulgar que pasa), -17 |
| `StepBarTests.cs` (N, B1) | SBT | AC-WKT-14-01 (tres filas; las tarjetas no se mueven), -02 a -05 (la fila de la 4a, otra vez, con los aires del plan), -06, -14 (la cuenta y el reloj pegados al borde y quietos cuando entra `Próximo`), -15 (los casos de §2.15 en cuatro perfiles, es / en, con los anchos medidos en Unity; el "…" blanco en `>160W…` de `Prueba 4a` y en CAP-31; la pastilla que funde al entrar `Próximo`; ninguna caja salta), AC-WKT-19-01, -05 |
| `CenterCalloutTests.cs` (N, B1) | CCT | AC-WKT-14-18 (lugar, tamaño ≥ SPM, el mismo cuadro que la banda y la cuenta, se va con fundido, pausa con `PAUSA`, sin entrada al volver; el aro en el sentido del reloj), AC-WKT-13-12 (`PAUSA` en el disco: su rectángulo es el de `HudLayout.BigCount` ± 0,5; un disco por vez al pausar y al reanudar con la cuenta), AC-WKT-17-09 (fuera de la zona del bote) |
| `SealTests.cs` (N, B1) | SVT | AC-WKT-09-08 (cuándo, dónde, una línea que se achica, 2 s también en pausa, el reemplazo, vencido al volver), -09 (el texto en pantalla), AC-WKT-13-05 (sin el nombre de la rutina) |
| `FreeBarTests.cs` (N, B1) | FBT | AC-WKT-16-01 a -06 (el chip de ancho fijo ± 0,5 y el reloj quieto al cambiar la fase; el título que se achica a 0,818 en TV y en 360 en español y que se recorta con "…" solo por debajo de 0,8); AC-WKT-19-01 a -05 en la barra libre |
| `CompletionDialogTests.cs` (N, B2) | CDT | AC-WKT-21-01 a -12 (`SizesAndLetter` con los rectángulos de 04-design-notes §3.8 ± 1 y `Maratón` por `HudCompletion`; los valores en `num.s` con la `m` de sufijo); AC-WKT-13-11; AC-WKT-20-07; AC-APP-03-10, -11; AC-APP-05-05, -05-07 (**el primer Tab sin foco va a `Terminar`**, por `DialogLayer`, también después del mouse; QA-G4bT-09 (b)); AC-WKT-03-05, -06-12 (pantalla); **la página lo cierra al volver a reposo** aunque no sea descartable (QA-G4bT-09 (c)) |
| `ImmersiveTransitionTests.cs` (N, B2) | ITT | AC-WKT-13-01, -04, -06 (los estados de la cascada con el reloj inyectado), -07 (los controles desde su tramo; "Atrás" desde el cuadro 0; reposo durante la entrada; **`KeyboardDuringTheCascade`: Tab y Enter a los 500 ms no enfocan ni activan nada; a los 900 ms, el primer Tab enfoca `Terminar` con su anillo**; QA-G4bT-09 (a)), -08 (la última foto, nunca `00:00`; los avisos del final a los 400), -09 (nada animando al salir, con `Terminar` durante el conteo), -10; AC-APP-03-09 |
| `PauseVeilTests.cs` (N, B2) | PVT | AC-WKT-13-12 (el velo, `PAUSA`, las tarjetas al 100 %), AC-WKT-16-03 (el chip en el mismo cuadro), AC-WKT-14-06 (los pulsos siguen en pausa) |
| `SystemBarsTests.cs` (N, B2) | SBU | AC-APP-15-01 (en pantalla, con el doble; los márgenes no se mueven), -02 (el escritorio no toca la ventana), -04 |
| `HudMotionTests.cs` (N, D1) | HMT | cada `IMotion` de §7.4 con elementos de prueba: los valores de las líneas de tiempo cuadro a cuadro, **`IsFinite` en cada fase**, la interrupción, el reemplazo, `Instant`, reducir movimiento, `Stop` y 0 B por cuadro |
| `HudLayoutTests.cs` (C, B1) | HLT | AC-WKT-13-13 (cuatro perfiles × libre y rutina × pulso × valores largos), AC-WKT-17-07 (rotar con el diálogo, el sello o la cuenta grande), -08, -09, -10 (extremos con píldoras y el diálogo de `Maratón`), -04 (el divisor, de la deuda), -12 (`ScalesOfTheDesignNotes`, `DerivedDigitWidthsOfTheDesignNotes`, de la deuda); `MergedPiecesMatchTheFourAGeometry` (M1, M2, M4, M6) |
| `HudBudgetTests.cs` (C, B1) | HBT | AC-WKT-17-01 y AC-WKT-14-19: ≤ 60 en los dos peores casos de §3.11 en los cuatro perfiles; 0 B por cuadro con 200 píldoras, el halo, el sello y la cuenta grande; `LongSeriesStaysSmooth` (el trabajo del HUD con su `Stopwatch`, con 200 pasos y con la fila sola de 1998; §3.13) |
| `ImmersiveHudTests.cs` (C, B1-B2) | IHT | AC-WKT-13-02 (las capas con el velo, el sello, la cuenta grande y el diálogo), -05, -14 (`PresentationNeverTouchesTheData`), AC-WKT-17-02, -03, -05, -06 (los valores en vivo detrás del velo del diálogo), -11 (kcal), AC-APP-05-05 (el HUD no sigue la letra), -05-08 |
| `WorkoutBackgroundUiTests.cs` (C, B2) | WBU | AC-WKT-02-06, -08 (el primer cuadro con las píldoras, el halo, la cuenta grande, el reloj, el chip y el diálogo), -09 (minimizada: el diálogo al restaurar), -11 (al despertar, el presente), AC-WKT-21-09 |
| `WorkoutScreenTests.cs` (C, B2) | WSC | AC-WKT-13-10 (con la salida de 640), AC-APP-05-07 (de la deuda) |
| `HudControlsTests.cs` (C, B2) | HCT | AC-WKT-01-06 (pantalla), -20-07 (el de terminar se cierra y a los 300 ms entra el de completado; nunca dos), -13-03 (Escape con el diálogo de completado: nada), AC-APP-05-07 (de la deuda) |
| `PlayModeSimulator/SimulatorOverlayTests.cs` (C, B0-B2) | SOT (ps) | AC-BLE-16-01 (la cinta sobre la primera píldora y sobre el velo del diálogo), -02 (la llave al terminar la cascada, fuera de las piezas nuevas, R10 con el diálogo en el hueco del carril izquierdo, usable sobre el velo), -03 (`PanelCeilingInTheWorkout`: el panel abierto termina 8 debajo del disco o de la barra de 74·s y no tapa el sello), DD4b-04 |
| `PlayModeSimulator/WorkoutSimulatorTests.cs` (C, B2) | WSM (ps) | AC-WKT-17-11 con la caída del pulsómetro simulado (VID-07); AC-BLE-16-02 |

### 6.3 El caso legacy (LT-46)

| LT | Legacy (`// legacy:` con el nombre exacto) | Tests | AC |
|---|---|---|---|
| LT-46 | `test/step_pill_test.dart:19` 'pendiente, en curso, hecho y no cumplido se pintan distinto' | `StepPillsTests.PendingActiveDoneAndMissedPaintDifferently` (dotnet: `LookOf` → `Paint` → el color final) y `PillRowTests.PendingActiveDoneAndMissedPaintDifferently` (PlayMode, la fila montada) | AC-WKT-15-02 a -05 |

**Para que el puerto no sea tautológico** (QA-G4b-P08). En el legacy cada píldora entra como (estado, resultado) y la regla "hecha sin resultado = cumplida" vive en el `switch` de `step_pill.dart:43-55`; en la app vive en `LookOf`. Si el test le pasara `Solid` a la cumplida y a la sin resultado, `expect(noTarget, done)` no probaría nada. Por eso el test **no elige aspectos**: arma las cinco con `StepPills.LookOf(índice, enCurso, resultado)` sobre un trabajo de 60 s (`:7`), con el paso en curso en el 3, y escribe el mapeo en el test:

| Legacy (`:20-26`) | En la app |
|---|---|
| `p`: `pending` | `LookOf(4, 3, null)` (después del en curso) |
| `a`: `active`, `progress: 0.5` | `LookOf(3, 3, null)` y `Paint(…, 0,5)` |
| `d`: `done`, `StepResult.met` | `LookOf(0, 3, Met)` |
| `m`: `done`, `StepResult.missed` | `LookOf(1, 3, Missed)` |
| `n`: `done` sin resultado | `LookOf(2, 3, null)` |

Además, `LookOf(4, 3, Met)` sigue pendiente (el resultado de un paso que no se hizo no se ve). Después compara **lo que se pinta**, no el aspecto: el color final de cada una (el base de `StepTypeInfo.Color(Work)` con el alfa de `Paint`, como el `color.withValues(alpha:)` del legacy), el borde (alfa y ancho) y el relleno. Las 12 afirmaciones de `:34-52`, una por una y en su orden, con su línea en un comentario:

1. `:35` la hecha cumplida con alfa > 0,8; 2. `:36` la hecha sin resultado **igual** a la cumplida (el color entero, RGBA, y el borde); 3. `:37` la pendiente con alfa < 0,3; 4. `:40` el interior de la no cumplida con el alfa de la pendiente; 5. `:41-42` el borde de la no cumplida con alfa > 0,8; 6. `:43` la pendiente sin borde; 7. `:44` la hecha sin borde; 8. `:47` la en curso con alfa mayor que la pendiente; 9. `:48` y menor que la hecha; 10. `:49-50` el relleno de la en curso en 0,5; 11. `:51` la en curso sin borde; 12. `:52` la pendiente sin relleno.

`PillRowTests` hace lo mismo con `PillRowView` montada sola en una fila de 400 dp y la `HudPillRow` armada con el mismo `LookOf`: lee lo que dibuja la vista (`LookAt(n)`, el color de cada píldora con una sonda y el relleno de la en curso, la mitad de su ancho ± 1 px). Convención de ARCHITECTURE §15.3: el método en inglés, el nombre del legacy al carácter en `// legacy: test/step_pill_test.dart:19 '…'`, `[Category("LT-46")]` y las categorías de los AC.

### 6.4 Matriz AC → test / captura

Marcas: **d** dotnet (y Unity EditMode) · **s** dotnet con simulador · **u** Unity EditMode solo · **p** PlayMode · **ps** PlayMode con simulador · **cap** captura del arnés · **eq** equipo (QA o el usuario) · **qa** revisión de QA. Ningún ID queda solo con **eq** o **qa** (`TraceabilityTests`): las partes sin módulo, sin teléfono o del usuario tienen su `[Ignore]` en WDC.

| AC | Verif. | Tests / capturas |
|---|---|---|
| AC-WKT-15-01 | d p cap | PIL.ColorIsTheBaseOfTheStepType; PRT.SizeAndRadiusTimesScale; CAP-01 |
| AC-WKT-15-02 | d p cap | PIL.PendingIsEighteenPercent; PRT; CAP-01 |
| AC-WKT-15-03 | d p cap | PIL.CurrentTrackAndFill, .HighlightOnlyWithFill; PRT.NoFrameOnTheCurrentPill; CAP-01, CAP-06 |
| AC-WKT-15-04 | d p cap | PIL.SolidAndNoTargetAreIdentical; PRT; CAP-01 |
| AC-WKT-15-05 | d p cap | PIL.HollowInteriorAndBorder; PRG.NarrowHollowHasRails; PRT; CAP-01, CAP-04 |
| AC-WKT-15-06 | d cap qa | CVD.FourLooksDifferUnderDeuteranopiaAndProtanopia; CAP-01 (QA, con la simulación) |
| AC-WKT-14-07 | d p cap | PRG.PaddingAndHeight; PRT; CAP-01, CAP-24 |
| AC-WKT-14-08 | d p cap | PIL.LooksByIndex; PRG.EqualWidthsWithinOnePixel; PRT.OnePillPerStepSameWidth; CAP-01, CAP-05, CAP-24 |
| AC-WKT-14-09 | d p cap | PIL.OnlyTimedStepsFill; PRT; CAP-05, CAP-12 |
| AC-WKT-14-10 | d p cap | PPG.* (con `.InstantInTheSameStepNeverGoesBack`); PRT.FillIsContinuousAndNeverAhead; VID-04 |
| AC-WKT-14-11 | d p cap | HTL.HaloIntensityAndRamp, .HaloEdgeMatchesTheLegacyShadow; PRT.HaloOnlyOutsideWithinTheRow, .HaloKeepsPulsingInPause; CAP-02, CAP-25 |
| AC-WKT-14-12 | d p cap | HTL.PillChangeTable; PRT.StepChangeAnimatesOnlyTheLook; HMT.PillRowMotion; VID-04 |
| AC-WKT-14-13 | d p cap | PIL.LastStepStaysCurrent; PRT.LastStepStaysCurrent; CAP-06, CAP-12 |
| AC-WKT-14-14 | d p cap | IRF.ModesGapsAndUpNextPadding; SBT.WallClockRightOfTheCountdown, .CountdownAndClockStayAtTheEdge; CAP-01, CAP-24 |
| AC-WKT-14-15 | d p cap | IRF.CasesOfTheDesignNotes, .PlanHoldsWithinTheStep; SBT.LongestRowInFourProfiles, .EllipsisInWhite, .TargetFadesWhenUpNextEnters; CAP-01, CAP-19, CAP-20, CAP-31 |
| AC-WKT-14-16 | d p cap | PRG.TwoHundredStepsInTvAreAStrip, .MaxEditorSteps, .ThumbFromFiftyStepsInTv; HTL.ThumbSwap; PRT.TwoHundredStepsFitTheBar, .ThumbPassesOnStepChange; CAP-22 |
| AC-WKT-14-17 | p | PRT.NoInteractionsNorTab |
| AC-WKT-14-18 | d p cap | BCD.*; HGT.BigCountPlace; HTL.BigCountInOut, .OneDiscAtATime; CCT.*; CAP-02, CAP-25, CAP-29, VID-04, VID-05 |
| AC-WKT-14-19 | p eq | HBT.LongSeriesStaysSmooth (el trabajo del HUD, con 200 y con 1998 pasos), .NoAllocationsPerFrameWithTwoHundredPills; [Device-Win] `FrameTimeProbe` (la CPU del hilo principal, §3.13) |
| AC-WKT-19-01 | d p cap | WCL.Format24Hours; FBT, SBT; CAP-01, CAP-13 |
| AC-WKT-19-02 | d p | WCL.MinuteChangesWithinASecond, .DoesNotRedrawBetweenMinutes; FBT.WallClockRollsOnlyOnTheMinute |
| AC-WKT-19-03 | d p | WCL.RunsInPauseFinishedAndDialog, .WakeAndForegroundPublishInstant; FBT; WBU |
| AC-WKT-19-04 | d p | WCL.ZoneChangeShowsTheNewHourWithinASecond, .SessionClockUnaffected; FBT |
| AC-WKT-19-05 | p | SBT, FBT.WallClockNoFrameNoFocus |
| AC-WKT-16-02 | p cap | FBT.ContentInOrder; CAP-13, CAP-18 |
| AC-WKT-16-03 | d p cap | PCH.TextAndColorsPerPhase, .FinishedTextContrast, .FixedWidth; FBT; PVT.ChipChangesInTheSameFrame; CAP-13, CAP-14, CAP-15, CAP-18 |
| AC-WKT-16-04 | d p cap | PCH.IdleHasNoChip; FBT.FinishedWhileSavingNoDialog; CAP-15 |
| AC-WKT-16-05 | d p cap | HTL.LiveDot, .LiveDotStartsAtOne; FBT.LiveDotOnlyWhenActive; CAP-13 |
| AC-WKT-16-06 | d p cap | IRF.FreeTitleScale; FBT.NothingOverlapsInFourProfilesEsEn, .TitleShrinksBeforeTheEllipsis; CAP-13, CAP-16, CAP-17, CAP-18 |
| AC-WKT-21-01 | d p cap | CHT.RoutineEndHolds; CDR.OncePerSession, .NeverOnManual; CDT.AppearsThreeHundredMsAfterTheEnd; CAP-07; VID-05 |
| AC-WKT-21-02 | p eq | CDT.ModalScrimEscapeDoNothing, .RibbonAndWrenchAboveTheVeil; WDC ("Atrás") |
| AC-WKT-21-03 | d p cap | WHS.CompletionTextsEsEn; CDT.ContentAndTexts; CAP-07, CAP-08 |
| AC-WKT-21-04 | d p | CHT.TotalsAreThoseOfTheFinalPoint; CDT.TotalsAreFixed |
| AC-WKT-21-05 | d p cap | HTL.CompletionCheckAndCountUp, .CountUpEndsAtTheExactValue; HMT.CompletionMotion; CDT; ITT.NothingAnimatesAfterTheExit (`Terminar` durante el conteo); VID-05 |
| AC-WKT-21-06 | d p cap | CHT.AcknowledgeWaitsForASlowClose, .FailedClose, .CloseTimeout, .QuitWithTheDialogOpen; CDT.TerminarWaitsForTheClose, .FailedCloseShowsSaveError; CAP-28 |
| AC-WKT-21-07 | d p | CHT.ManualNeverHasCompletion; CDR.NeverOnManual; CDT.NeverOnManualFinish |
| AC-WKT-21-08 | d p | CHT.NoPointsAfterTheEnd, .LiveReadingsKeepPublishing; CDT; IHT.LiveValuesBehindTheCompletionVeil |
| AC-WKT-21-09 | d p eq | CHT.EndsInBackground; WBU.CortaEndsInBackgroundDialogAlreadyOpen; [Device-Win] minimizada |
| AC-WKT-21-10 | p qa | CDT.OneButtonNoStrava; QA (ninguna llamada de Strava) |
| AC-WKT-21-11 | p eq | CDT.KeyboardFirstTabGoesToFinish, .FirstTabWithoutFocusReachesTheDialog; [Device-Win] |
| AC-WKT-21-12 | p cap | CDT.SizesAndLetter (los rectángulos de 04-design-notes §3.8 ± 1; `Maratón` por `HudCompletion`); CAP-09, CAP-10, CAP-11 |
| AC-WKT-21-13 | d | WGD.ScriptsAreIdenticalToTheLegacy, .CompletedHoldReset |
| AC-WKT-13-06 | d p cap | HTL.EntryTableOfTheSpec; ITT.EntryCascadeStates; HMT.ImmersiveCascadeMotion; VID-01, VID-03, VID-08 |
| AC-WKT-13-07 | p eq | ITT.ControlsRespondFromTheirPiece, .KeyboardDuringTheCascade, .BackFromTheFirstFrame, .IdleDuringEntryExitsFromThere; WDC ("Atrás") |
| AC-WKT-13-08 | d p cap | HTL.ExitTable; ITT.ExitKeepsTheLastPhoto, .EndToastsAtTheReveal; VID-02 |
| AC-WKT-13-09 | p eq | ITT.NothingAnimatesAfterTheExit (cuatro salidas, con `Terminar` durante el conteo); [Device-Win] el registro después de tres entrenamientos |
| AC-WKT-13-10 | p | ITT.OneSessionOneImmersive; WSC.StartDuringThePreviousExitHasItsOwnImmersive |
| AC-WKT-13-11 | d p | CDR.OncePerSession; CDT.OncePerSessionRotationMinimize |
| AC-WKT-13-12 | d p cap | HTL.PauseVeil, .OneDiscAtATime; HGT.PausedInTheDisc; PVT.*; CCT; CAP-14, CAP-27, VID-06 |
| AC-WKT-13-13 | d p cap | HGT.NoOverlapWithTheNewPieces; HLT.NothingOverlapsWithTheNewPieces; `HudOverlap` en CAP-01 a -06, -13 a -27, -29, -30 |
| AC-WKT-13-14 | d p cap | PIN.EngineSequenceIsTheSameWithAndWithoutTheHud; PPG.NeverPassesTheNextTick; IHT.PresentationNeverTouchesTheData; VID-04, VID-05 (cuadro por cuadro) |
| AC-WKT-17-11 | d p cap | HTL.KcalSlide; IHT.KcalSlides; WSM.HeartRateDropSlidesKcal; VID-07 |
| AC-WKT-17-12 | d p cap qa | `ItFourAFontTests` (de la deuda); HLT.ScalesOfTheDesignNotes, .DerivedDigitWidthsOfTheDesignNotes; CAP-13; la regeneración de §5.1; QA |
| AC-WKT-09-08 | d p cap | SEL.When*; SVT.*; CAP-03, CAP-04, CAP-05, CAP-26, CAP-30, VID-04 |
| AC-WKT-09-09 | d p cap | SEL.TextsOfTheSpecCases, .Rounding*; SVT.TextOnScreen; CAP-03, CAP-04, CAP-05 |
| AC-WKT-02-11 | d p eq | SUS.* (con el "reanuda" que ancla, el tirón y la vuelta del segundo plano); WSU.*; USC; WBU.WakeShowsThePresent; WDC (el equipo: lo verifica el usuario, demo (c)) |
| AC-APP-15-01 | d p eq | SBR.*; SBU.InsetsDoNotMoveWithTheBars; WDC (Android) |
| AC-APP-15-02 | p eq | SBU.DesktopDoesNotTouchTheWindow; [Device-Win] |
| AC-APP-15-03 | d eq | WDC ([Apple], 10b) |
| AC-APP-15-04 | d p | SBR.RequestsInOrder; SBU.RequestsInOrder |
| AC-APP-10-11 | d | CAT (214) |
| AC-WKT-17-04 | p cap | HLT.BandDividerKeepsItsWidth (de la deuda); CAP-13; las tomas regeneradas (§5.1) |
| AC-APP-05-07 | p eq | `FirstTabWithoutFocusShowsTheRing*` (de la deuda); ITT.KeyboardDuringTheCascade; CDT.KeyboardFirstTabGoesToFinish, .FirstTabWithoutFocusReachesTheDialog; [Device-Win] |
| AC-WKT-01-06 | d p | CHT.ManualFinishStillReturnsAlone, .RoutineEndHolds; HCT.FinishFromTheHudReturnsAloneWhenSaved |
| AC-WKT-02-04 | d | ACK.SixHundredSecondsWithALateFrame (4a); WSU.WithoutEvidenceItCountsLikeALateFrame; SUS.StallWithoutEvidenceCounts |
| AC-WKT-02-06 | d p | CHT.EndsInBackground (40 y 330 s); WBU.CortaEndsInBackgroundDialogAlreadyOpen |
| AC-WKT-02-08 | d p eq | WHS.InstantOnWake; WBU.FirstFrameAfterBackgroundShowsThePresent; WDC (Android) |
| AC-WKT-02-09 | d p eq | `BootstrapperStaticTests` (`runInBackground`, 4a); WBU.MinimizedEndShowsTheDialogOnRestore; [Device-Win] |
| AC-WKT-03-05 | d p cap | CHT.RoutineEndHolds; WST.NoCompletedToast; CDT; CAP-07, DEMO-11 |
| AC-WKT-06-12 | d p cap | CHT.FailedClose; WST.SaveErrorAfterAcknowledge; CDT.FailedCloseShowsSaveError; CAP-28 |
| AC-WKT-13-01 | p | ITT.OpensAtOnce |
| AC-WKT-13-04 | p | ITT.ClosesAlone; HCT |
| AC-WKT-14-01 | d p cap | HGT.StepBarHeights; SBT.ThreeRowsAndCardsDoNotMove; CAP-01, CAP-03 |
| AC-WKT-14-06 | p cap | SBT.PausedAndFinishedBar; PVT; CAP-06, CAP-27 |
| AC-WKT-16-01 | p cap | FBT.ContentInOrder; CAP-13 |
| AC-WKT-20-07 | p | HCT.RoutineEndsWithTheFinishDialogOpenThenCompletion; CDT |
| AC-APP-03-09 | p cap | ITT.StacksOverTheApp; VID-01, VID-02 |
| AC-APP-03-10 | p cap | CDT.ModalScrimEscapeDoNothing; CAP-07 |
| AC-APP-03-11 | d p cap | WST.ToastsHeldWhileTheCompletionDialogIsOpen; CDT; ITT.EndToastsAtTheReveal; CAP-28 |
| AC-APP-05-05 | p cap | CDT.SizesAndLetter; IHT.HudDoesNotFollowTheSystemLetter; CAP-10 |
| AC-APP-05-08 | d p | HTL.ReducedVariants (sin destello ni trazo en el cambio de paso; el pulgar, la fila y un disco por vez); IHT.ReducedMotionPieces; HMT |
| AC-BLE-16-01 | ps cap | SOT.RibbonAboveTheCompletionDialog; CAP-01, CAP-07, CAP-13 |
| AC-BLE-16-02 | s ps cap | WPL.WrenchAvoidsTheCompletionDialog (los cinco lugares de §5.7), .WrenchSlotClearOfTheNewPieces; SOT.WrenchEntersAtTheEndOfTheCascade, .WrenchMovesAwayFromTheCompletionDialog; `WrenchAudit` en toda toma con simulador |
| AC-BLE-16-03 | d s ps cap | HGT.PanelCeiling; SPL.WorkoutCeilingFiveHeights; SOT.PanelCeilingInTheWorkout; DEMO-08 |
| AC-WKT-13-02 | p cap | IHT.Layers; CAP-07 |
| AC-WKT-13-03 | p eq | HCT.EscapeWithTheCompletionDialogDoesNothing; WDC ("Atrás", 4a) |
| AC-WKT-13-05 | p | IHT.NoRoutineName; SVT.NoRoutineName |
| AC-WKT-14-02 | p cap | SBT.InfoRowAsInTheFourA; CAP-01 |
| AC-WKT-14-03 | d p cap | IRF.WholeFragmentsAndWhiteEllipsis; SBT.InfoRowAsInTheFourA, .EllipsisInWhite; CAP-01, CAP-21, CAP-24, CAP-31 |
| AC-WKT-14-04 | d p cap | IRF.ModesGapsAndUpNextPadding; SBT.InfoRowAsInTheFourA; CAP-01, CAP-19, CAP-20 |
| AC-WKT-14-05 | p cap | SBT.InfoRowAsInTheFourA; CAP-02 |
| AC-WKT-17-01 | p cap | HBT; HLT.MergedPiecesMatchTheFourAGeometry; CAP-13 |
| AC-WKT-17-02 | p | IHT (4a) |
| AC-WKT-17-03 | p | IHT (4a), con los afijos |
| AC-WKT-17-05 | p cap | IHT.KcalSlides; CAP-16 |
| AC-WKT-17-06 | p | IHT.LiveValuesBehindTheCompletionVeil |
| AC-WKT-17-07 | p | HLT.RotationWithDialogSealOrBigCount |
| AC-WKT-17-08 | d p cap | HGT.NoOverlapWithTheNewPieces; HLT; `HudOverlap` |
| AC-WKT-17-09 | p cap | HLT.BoatZoneFreeOfTheNewPieces; CCT; CAP-29 |
| AC-WKT-17-10 | p cap | HLT.ExtremesWithPillsAndMaratonDialog; CAP-10 |
| AC-HIS-20-04 | d qa | SLT; QA (las bases del usuario sin tocar) |
| LT-46 | d p | §6.3 |

**`It04bIds.UnityOnlyIds`** (sin **d** ni **s**; 36): AC-WKT-14-17, -14-19, -19-05, -16-02, -21-02, -21-10, -21-11, -21-12, -13-07, -13-09, -13-10, AC-APP-15-02, AC-WKT-17-04, AC-APP-05-07, AC-WKT-13-01, -13-04, -14-06, -16-01, -20-07, AC-APP-03-09, -03-10, -05-05, AC-BLE-16-01, AC-WKT-13-02, -13-03, -13-05, -14-02, -14-05, -17-01, -17-02, -17-03, -17-05, -17-06, -17-07, -17-09, -17-10. Salen de la lista AC-WKT-14-03, -14-04, -14-14, -14-15 y -16-06, que ahora tienen su regla en dotnet (`InfoRowFitTests`, §2.15), y AC-BLE-16-03 entra con **d** y **s**: son 101 IDs más LT-46 (102 filas). `WorkPackageBIds` vacío. `TraceabilityTests` suma `It04bIdsAreComplete`, `It04bDotnetIdsHaveTests` e `It04bUnityOnlyIdsHaveAUnityLocation`.

### 6.5 Conteos esperados (aproximados)

dotnet: +≈ 280 (píldoras, fila, pulgar y relleno ≈ 55; la fila de información y el título libre (IRF) ≈ 15; cuenta grande y sello ≈ 35; el fin que espera y el diálogo ≈ 35; chip y reloj de pared ≈ 20; suspensión ≈ 35; barras ≈ 8; líneas de tiempo ≈ 40; geometría, la llave y el techo del panel ≈ 25 más los casos parametrizados; *stores*, catálogo, oráculo, trazabilidad y equipo ≈ 15). PlayMode: +≈ 105 sin simulador y +≈ 12 con simulador. dotnet de WP-B3: 2 archivos (SEV, ITO). Unity EditMode: +2 archivos y 1 cambiado. Arnés: 51 archivos (31 CAP, 12 DEMO en una secuencia, 8 VID).

### 6.6 Lo que queda sin test automatizado

- **[Device-Win]**: la ventana que no cambia al entrar y al salir (AC-APP-15-02); el teclado real con el primer Tab en el HUD, en Workout, en el editor y en el diálogo (AC-APP-05-07, AC-WKT-21-11); la ventana minimizada con la rutina por terminar y el diálogo al restaurar (AC-WKT-02-09, -21-09); el registro del build de simulador sin avisos de elementos fuera del panel después de tres entrenamientos (AC-WKT-13-09); los tiempos de cuadro con `Serie larga` (AC-WKT-14-19); **la suspensión del equipo, que verifica el usuario** en la demo (c) (AC-WKT-02-11).
- **[Device-And]**, NO VERIFICABLE sin el módulo y el teléfono, cada uno con su `[Ignore]` en WDC: las barras ocultas y su regreso (AC-APP-15-01), "Atrás" en la entrada y con el diálogo de completado (AC-WKT-13-07, -21-02) y el primer cuadro al volver (AC-WKT-02-08).
- **[Apple]**: AC-APP-15-03, en la 10b.

---

## 7. Paquetes de trabajo

### 7.0 Orden y reglas

1. **WP-A** (dotnet, sin Unity): todo lo puro de §2 y §3.1, los dobles de Tests/Support, los tests de §6.2 que son de dotnet, el oráculo (§2.13) y el ADR 020. Arranca con las 04-design-notes ya integradas (Anexo B): DN4b-14 a -18, -22, -23 y -25 cambian sus reglas, su geometría, sus constantes y el *token* (QA-G4bT-01). Termina con `run-tests.ps1` en verde y la matriz con los IDs de Unity en `PENDIENTE UNITY`.
2. **Las 04-design-notes de la 4b** (Designer) **están hechas** e integradas una por una en el Anexo B, con las decisiones del PO del gate (spec §16). Le queda al Designer alinear §4.5 y §4.14 de las notas con "sin destello" (QA-G4bT-03) y CAP-22 a los 3016,6 (spec §16.2); WP-D2 sigue después de WP-B3.
3. **WP-B0** (Unity, después de WP-A): la sonda **S-B4b** primero (abajo); los afijos y la placa de `TabularNumber`; las juntas M1 a M6 con las pruebas de la 4a que miran esas piezas al día; `ModalDialog` no descartable y la apertura sin transición; DD4b-03 (el "…" blanco) y DD4b-04 (el panel que sigue a la llave). Termina con las cuatro suites de Unity en verde y `capture-snapshots.ps1 -Set it4a -Out <scratch>/…` con **0 comprobaciones y 0 sondas que fallan**: es la red de regresión de las juntas, porque el HUD todavía es el de la 4a. Las comprobaciones de `WorkoutCatalog` que nombran la flecha o la unidad como elementos pasan a mirarlas en el `TabularNumber` (sin cambiar lo que esperan). Los bytes de las tomas del HUD pueden cambiar en un subpíxel: no se versionan, porque `it4a` queda como el registro de la 4a.
4. **WP-D1** (Unity, después de WP-A; independiente de B0): los once `IMotion` de §7.4, con sus fases finitas y su variante de reducir movimiento, incluidos los cuatro de las notas (el pulgar que pasa en `PillRowMotion`, `InfoRowMotion` y `CenterDiscMotion` con un disco por vez), `ISettlingMotion` en los resortes de la 4a y `Reached` en `TimedMotion`, con `HudMotionTests` sobre elementos de prueba.
5. **WP-B1** (después de B0 y D1): la barra de tres filas, `PillRowView` con `PillGlowTexture`, `WallClockView`, la barra libre con el chip y el título que se achica, `CenterCallouts` (el disco de la cuenta grande y de `PAUSA`), `SealRow`, el plan de `InfoRowFit` en `StepInfoBar`, el velo, kcal, `HudZones` y `HudView` con las piezas nuevas y sus `IMotion`; el presupuesto y la disposición.
6. **WP-B2**: el diálogo de completado (`CompletionDialog`, `CheckMarkView`), la página (la sesión, el diálogo a los 300 ms, la entrada y la salida con la última foto, `PageTransitionKind.Immerse`), las barras del sistema y los márgenes, P-17 en Platform (la fuente de sueño, `UnityScheduler`, `AppLifecycle`, `AppRoot`), el offset local, la llave (la superficie oculta y R10), `FrameTimeProbe`, y las pruebas de segundo plano.
7. **WP-B3**: el arnés `it4b` (§5) con sus dos corridas, las tomas reemplazadas de `it4a`, los builds (simulador a `Builds/Windows/simulator`, normal Mono; IL2CPP NO VERIFICABLE sin el módulo) y, al cerrar, ARCHITECTURE y ADR 017 ("Cambios de la It. 4b").
8. **WP-D2** (Designer): el aspecto (`Skin.uss`, `Motion.uss`) sobre las clases de §7.5, los valores de las sondas de aspecto, la revisión cuadro a cuadro de VID-01 a -08, los pares de contraste nuevos y las biblias ("Cambios de la It. 4b").

**Reglas** (las de la 4a): cada archivo tiene un solo dueño; una sola instancia de Unity por vez (si el proyecto está tomado, se espera y se reintenta; nunca se mata un Unity que no lanzó el paquete); ningún paquete hace *commits*; los builds de desarrollo van solo a su carpeta por defecto y, si el `RowMate.exe` del usuario la tiene tomada, no se lo cierra: se reintenta cada 2 minutos hasta 20 y, si sigue, esa parte queda NO VERIFICABLE; nada escribe rutas de la PC en lo versionado.

**La sonda S-B4b** (WP-B0, en el proyecto real, antes de tocar el HUD; deja la evidencia en su informe):

| Qué | Si no da | 
|---|---|
| `MeshGenerationContext.DrawText` dentro de `TabularNumber` con otro `FontAsset` (Inter 700 para `▲` y `▼`, Inter 500 para `W` y `m`, la fuente de íconos para `schedule`), con su tamaño, color y alfa, en el mismo elemento que las cifras | la flecha como un triángulo de `Painter2D` (no necesita fuente) y las unidades con las letras de la derivada (están en Barlow): se ve casi igual y lo aprueba el Designer; el reloj, con el ícono como elemento aparte (+1) |
| `margin-left: auto` en un hijo de una fila de UI Toolkit 6000.3 (M5) | queda el espaciador de la 4a (+1) |
| `<b>` (o `<font-weight=700>`) en un `Label` de Inter 400 que pase al `FontAsset` de Inter 700 (M3) | quedan las dos etiquetas de la 4a (+1) |
| `Painter2D` y `Allocate` con una textura en el mismo `generateVisualContent` (la fila de píldoras y el halo de la tarjeta, M4) | el halo de la tarjeta vuelve a ser su elemento (+3) y la fila dibuja el halo con `Painter2D` (sin textura) |
| `unityBackgroundImageTintColor` del fondo, por código, sin *relayout* | un elemento negro con opacidad sobre el fondo (+1) |
| **El texto enriquecido del sello** (QA-G4bT-10 (c)): en un solo `Label`, `<size>`, `<font-weight=700>` / `<font-weight=600>` y `<color>` que pasen a los `FontAsset` de Inter 700 y 600, medido con `MeasureTextSize` ± 0,5 | el sello en dos etiquetas, el título y el detalle (+1 con el sello) |
| **El glifo de `PAUSA`**: `Painter2D` en el `generateVisualContent` de un `Label`, junto con su texto, sobre su fondo y su borde redondeados (DN4b-14) | el glifo como `IconView` hijo (+1, solo en pausa) |
| **La textura del halo con largos por lado**: `Allocate` con `PillGlowTexture` en 9 partes, con cada borde estirado a su largo (10 / 8 / 4·s) y la textura de 86 × 86 generada al arrancar (DN4b-17) | el halo con `Painter2D`: seis cápsulas concéntricas con el perfil escalonado (0 elementos; más vértices, que mide HBT) |
| **El corte por fragmentos**: `MeasureTextSize` de la pastilla con su marcado (la tendencia en `<color>` y el "…" en `text.primary`) da el ancho que se ve ± 0,5 (DN4b-16) | cada fragmento medido sin marcado y sumado con el ancho de ` · ` y del "…" (0 elementos; el error de kerning entre fragmentos lo cubre el margen de 1 de `InfoRowFit`) |

Con todos los "si no da" juntos el peor caso pasa de 64 a **66** (la pausa con el sello: +1 por las dos etiquetas del sello y +1 por el glifo): el informe de B0 lo dice y se decide antes de B1 (R4b-04, R4b-12). Cada fila de la sonda deja su evidencia (una toma de prueba y las medidas) en el informe.

### 7.1 WP-A — reglas puras y pruebas de dotnet (≈ 66 archivos)

Core: `Workout/{StepPills,StepOutcome,StepSeal,BigCountdown,PhaseChip,CompletionSummary,CloseStatus}.cs` (N), `StepTargetTracker.cs`, `WorkoutSnapshot.cs`, `WorkoutEngine.cs` (C); `Common/{SuspendAccounting,SystemWake,FrameClock}.cs` (N), `FrameScheduler.cs`, `ForegroundLifecycle.cs` (C); `Ports/ISystemUi.cs` (N), `IAppLifecycle.cs`, `IDisplayMetrics.cs`, `SafeInsets.cs` (C); `Design/{PillRowGeometry,PillShapes,PillProgress,InfoRowFit,CountUp,HudTimelines,CheckGlyph}.cs` (N), `MotionSpecs.cs`, `HudGeometry.cs`, `OverlaySurface.cs` (C); `Design/Tokens.cs` y `Presentation/Styles/Tokens.uss` (C / G: solo `phase.finished.hud`, con `tools/tokens`; excepción de §7.6); `Localization/L10nKeys.cs`, `strings.es.json`, `strings.en.json` (C). App: `Workout/{WallClockTicker,CompletionDialogRule,ImmersiveSystemBars}.cs` (N), `WorkoutHudState.cs`, `WorkoutHudStore.cs`, `WorkoutStore.cs`, `WorkoutToast.cs` (C); `Composition/{AppServices,AppComposer,AppContext}.cs` (C). Simulator: `Panel/WrenchPlacement.cs`, `SimulatorPanelLayout.cs` (C). Tests/Support: §6.1. Tests: los de dotnet de §6.2 y `Tests/Simulator/*` (C). `Tests/Golden/oracle/workout_engine.json` (G), `tools/oracle/engine/test/engine_vectors_test.dart` (C). `docs/migration/architecture/adr/020-reloj-despierto.md` (N).

### 7.2 WP-B — UI, plataforma, composición, arnés y pruebas de Unity (≈ 78 archivos)

| Entrega | Archivos |
|---|---|
| B0 | `Presentation/Controls/TabularNumber.cs` (los afijos, la placa y `RollProgress`); `Presentation/Workout/{HudCard,HudBand,StepInfoBar}.cs` (la pastilla por fragmentos con el "…" blanco); `Presentation/Shell/{ModalDialog,DialogLayer}.cs` (`Dismissible` con `Dismiss()`, el cierre del dueño, `Open(…, instantly)` y el primer Tab sin foco al diálogo de arriba); `Presentation/Styles/Base.uss`; `SimulatorPresentation/SimulatorLayerView.cs` (DD4b-04); las PlayMode de la 4a que miran la flecha, la unidad, el halo o el corazón (`ImmersiveHudTests`, `HudLayoutTests`, `HudBudgetTests`) y `SimulatorOverlayTests` (DD4b-04) |
| B1 | `Presentation/Workout/{PillRowView,PillGlowTexture,WallClockView,PhaseChipView,SealRow,CenterCallouts}.cs` (N); `FreeTopBar.cs`, `StepInfoBar.cs`, `HudView.cs`, `HudZones.cs`, `HudBackdrop.cs` (C); `Base.uss`; `Tests/UiSupport/{UiTestHost,UiHostOptions}.cs` (C); `Tests/PlayMode/{PillRowTests,StepBarTests,CenterCalloutTests,SealTests,FreeBarTests}.cs` (N), `HudLayoutTests.cs`, `HudBudgetTests.cs`, `ImmersiveHudTests.cs` (C) |
| B2 | `Presentation/Workout/{CompletionDialog,CheckMarkView}.cs` (N); `ImmersiveWorkoutPage.cs`, `WorkoutScreenView.cs`, `HudControls.cs` (C); `Presentation/Motion/PageTransition.cs`, `Presentation/Shell/ShellLayout.cs` (C); `Platform/Power/SystemSleepSource.cs`, `Platform/Time/LocalOffsetSource.cs`, `Platform/SystemUi/{AndroidSystemUi,DesktopSystemUi,SystemUiFactory}.cs` (N); `Platform/Time/UnityScheduler.cs`, `Platform/Lifecycle/AppLifecycle.cs`, `Platform/Display/DisplayMetricsProvider.cs`, `Platform/{PlatformServices,PlatformFactory}.cs` (C); `SimulatorPresentation/FrameTimeProbe.cs` (N), `SimulatorLayerView.cs` (C); `Bootstrap/AppRoot.cs` (C); `Tests/PlayMode/{CompletionDialogTests,ImmersiveTransitionTests,PauseVeilTests,SystemBarsTests}.cs` (N), `WorkoutBackgroundUiTests.cs`, `WorkoutScreenTests.cs`, `HudControlsTests.cs` (C); `Tests/PlayModeSimulator/{SimulatorOverlayTests,WorkoutSimulatorTests}.cs` (C); `Tests/UnityEditMode/{SystemSleepSourceTests,LocalOffsetSourceTests}.cs` (N), `UnitySchedulerTests.cs` (C) |
| B3 | `Snapshots/HudExrCatalog.cs` (N); `Snapshots/{SnapshotCatalog,SnapshotCli,SnapshotRunner,ScenarioScript,WrenchAudit,SnapshotManifest,WorkoutCatalog}.cs` (C; `SnapshotRunner` con `SnapshotTakeRule`, §5.2); `Editor/Build/BuildCli.cs` (C: `enableFrameTimingStats` solo en el build de simulador); `Tests/EditMode/Project/{SnapshotEvidenceTests,ItFourBToolingTests}.cs` (C / N, dotnet en archivos de WP-B: excepción de §7.6); `tools/unity/{capture-snapshots.ps1,README.md}` (C); `docs/…/04b-hud-exr/snapshots/*` (G); al cerrar, `docs/migration/architecture/ARCHITECTURE.md` y `adr/017-segundo-plano.md` ("Cambios de la It. 4b") |

### 7.3 WP-D — motion (D1) y pulido del Designer (D2)

| Entrega | Archivos |
|---|---|
| D1 (≈ 17 archivos) | `Presentation/Motion/Hud/{ImmersiveCascadeMotion,ImmersiveExitMotion,PillRowMotion,InfoRowMotion,SealMotion,CenterDiscMotion,PauseVeilMotion,PhaseChipMotion,LiveDotMotion,KcalSlideMotion,CompletionMotion}.cs` (N); `Presentation/Motion/ISettlingMotion.cs` (N); `Presentation/Motion/{SpringMotion,StepPulse,TimedMotion}.cs` y `Presentation/Controls/FitBox.cs` (C: solo `ISettlingMotion` y `Reached`; excepción de §7.6); `Tests/PlayMode/HudMotionTests.cs` (N); los VID-01 a -08 se graban en B3 con estos `IMotion` |
| D2 | `Presentation/Styles/{Skin,Motion}.uss`, `SimulatorPresentation/Styles/SimulatorSkin.uss` (si la llave cambia); `docs/…/04b-hud-exr/04-design-notes.md`; `Tests/EditMode/Design/ContrastTests.cs` (el grupo de la 4b, excepción de §7.6); `docs/migration/design/{ART_BIBLE,ANIMATION_BIBLE}.md` y `LEGACY_VISUAL_AUDIT.md` ("Cambios de la It. 4b") |

### 7.4 El contrato de motion (WP-D1 ↔ WP-B)

Los `IMotion` de D1 **no conocen las vistas**: reciben el `MotionDriver` y *callbacks* con los *structs* de valores de WP-A (`Core/Design/HudTimelines.cs`), y las vistas de B los construyen y los llaman. Así D1 compila y se prueba antes que B1, y los archivos no se cruzan. Todos respetan `Driver.Armed` (nada se anima antes del primer cuadro presentado), `Driver.Settings.ReduceMotion` e `Instant` (estado final en el acto), pasan el tiempo a ms enteros con `HudTimelines.Ms` (§3.1), no asignan memoria por cuadro y salen del `MotionDriver` con `Stop()`, que la vista llama en su `Dispose` y en su `DetachFromPanelEvent` (QA-G4bT-08 (b)).

**`IsFinite` por fase** (QA-G4bT-07 (a)): es lo que mira la regla de toma (§5.2) y lo que espera `Settle` en las pruebas. **F** = finito (una transición: bloquea una toma); **C** = continuo (en el *driver*, sin bloquear); "sale" = deja el *driver* al terminar. `HudMotionTests` comprueba cada fase con su valor.

| `IMotion` (D1) | Construcción | Lo que expone | Fases (`IsFinite`) | Reducir movimiento | Línea de tiempo (WP-A) | Lo usa (WP-B) |
|---|---|---|---|---|---|---|
| `ImmersiveCascadeMotion` | `(MotionDriver, Action<HudPiece, PieceVisual> piece, Action<double> backdrop, Action<HudPiece> interactive, Action done)` | `Play()`, `IsRunning`, `PieceOpacity(HudPiece)` (para que la salida arranque desde ahí), `Stop()` | F de 0 a 1120 (la llave entra después, en la capa del simulador, hasta 1280); sale | fundido lineal de 120 | `ImmersiveTimeline.Entry*` | `ImmersiveWorkoutPage` |
| `ImmersiveExitMotion` | `(MotionDriver, Action<ExitVisual> apply, Action revealStarted, Action done)` | `Play(in ExitStart from)`, `Finish()`, `Stop()` | F de 0 a 640; avisa `revealStarted` a los 400 (0 con reducir); sale | fundido de 120 | `ImmersiveTimeline.Exit`, `.RevealStartMs` | `ImmersiveWorkoutPage` |
| `PillRowMotion` | `(MotionDriver, StepPulse pulse, Action repaint)` | `OnState(HudPillRow row, bool instant)`, `Frame` (`PillRowFrame`: el relleno de la en curso, el alfa del halo, el cambio de paso y el pulgar en curso), `Stop()` | F en el cambio de paso (0-440) y en el pulgar que pasa (0-150); C mientras se ve el halo o se llena la en curso; fuera de eso, sale | sin destello ni trazo; el pulgar, fundido cruzado de 150 | `PillProgress`, `StepBarTimeline` (`.PillChange`, `.Halo`, `.ThumbSwap`) | `PillRowView` |
| `InfoRowMotion` **[notas]** | `(MotionDriver, Action<InfoRowVisual> gaps, Action<double> targetOpacity, Action targetGone)` | `ChangeMode(in InfoRowPlan from, in InfoRowPlan to, bool instant)`, `HideTarget(bool instant)`, `Stop()` | F en el cambio de modo (0-240) y en la pastilla que se va (0-120); sale | los aires en el acto; la pastilla, su fundido de 120 | `InfoRowTimeline` | `StepInfoBar` |
| `SealMotion` | `(MotionDriver, Action<SealVisual> apply)` | `Show(HudSeal seal, bool instant)`, `Clear(bool instant)`, `IsShowing`, `Stop()` | F en la entrada (0-300) y en el reemplazo (0-320); **C en el sostén (300-1800)**; F en la salida (1800-2000); sale | el alto en el acto; el contenido en 160 / 120; el ícono sin escala | `SealTimeline` | `SealRow` |
| `CenterDiscMotion` **[notas]** | `(MotionDriver, PillProgress progress, Action<DiscVisual> apply)` | `SetCount(int? value, bool instant)`, `SetPaused(bool paused, bool instant)`, `RingFraction`, `Stop()` | F en la entrada (200; `PAUSA`, 240), la salida (200; `PAUSA`, 240) y el cambio de disco (0-120 + la entrada); **C entre ticks** (el aro, del `PillProgress`); `PAUSA` quieto, sale; el rodillo de la cifra es el de su `TabularNumber` (§5.2) | solo opacidad (160 / 120 la cuenta; 240 `PAUSA`); el cambio, los mismos tiempos sin escala | `CenterDiscTimeline` | `CenterCallouts` |
| `PauseVeilMotion` | `(MotionDriver, Action<double> veilAlpha)` | `SetPaused(bool paused, bool instant)`, `Stop()` | F en 300 al pausar y 240 al reanudar; sale | el mismo fundido | `PauseTimeline.Veil` | `HudView` |
| `PhaseChipMotion` | `(MotionDriver, Action<ChipVisual> apply)` | `Change(bool instant)`, `Stop()` | F en 0-200 (colores 150, etiqueta 0-80 y 80-200); sale | igual (son fundidos) | `PhaseChipTimeline` | `PhaseChipView` |
| `LiveDotMotion` | `(MotionDriver, Action<double> opacity)` | `SetActive(bool active)` (al activarse, la fase vuelve a 1, bajando: DN4b-26), `Stop()` | C mientras está activo; sale al desactivarse | quieto en 1, fuera del *driver* | `LiveDot` | `PhaseChipView` |
| `KcalSlideMotion` | `(MotionDriver, Action<KcalVisual> apply, Action bpmGone)` | `Appear(double fromShiftDp, bool instant)`, `Disappear(bool instant)`, `Finish()`, `Stop()` | F en 0-360; sale | kcal en el acto; BPM funde (240 / 120) | `KcalSlideTimeline` | `HudView` |
| `CompletionMotion` | `(MotionDriver, Action<CompletionVisual> apply)` | `Play(bool instant)`, `Finish()`, `Stop()` | F de 0 a 1160 desde que entra el diálogo (el conteo termina); sale. `CompletionDialog.OnClosing` lo detiene (QA-G4bT-08 (a)) | solo opacidad; el check entero y los totales finales | `CompletionTimeline`, `CountUp` | `CompletionDialog` |

**Lo de la 4a** (`SpringMotion`, `StepPulse`, `TimedMotion`, `FitBox`, el rodillo de `TabularNumber`) no cambia de conducta: D1 les suma lo que la regla de toma necesita para no tratar todo lo finito igual (QA-G4bT-07 (d)): `ISettlingMotion.RemainingDp` en los resortes (el ancho de `Próximo` y de `FitBox` en dp; la escala de la cuenta, por su ancho), `HudTimelines.Reached` en el final de `TimedMotion`, y `TabularNumber.RollProgress` (B0).

```csharp
namespace RowMate.Presentation.Motion
{
    // Lo que le falta a un resorte de la 4a para llegar a su destino, en dp de pantalla (§5.2: bloquea una toma con > 0,15).
    public interface ISettlingMotion
    {
        double RemainingDp { get; }
    }
}
```

### 7.5 El contrato USS (WP-B en `Base.uss` o por código; WP-D2 no lo cambia)

Siguen las reglas de las It. 2, 3 y 4a: sin colores literales; nadie define tamaño ni fuente en un elemento con `.rm-type-*` (en el HUD el tamaño va en línea por código); `Skin.uss` y `Motion.uss` no tocan el contrato; las clases de estado las pone el código; lo que va en línea por código manda; USS no tiene `em` ni `baseline` (los afijos de `TabularNumber` los mide el código con las métricas del `FontAsset`); lo que crece desde 0 (el sello, `Próximo`) va en un *slot* que recorta.

| Elemento | Contrato | AC |
|---|---|---|
| `.rm-immersive` | `background-color` negro: el de la entrada y la salida | AC-WKT-13-06, -08 |
| `#hud-topbar.rm-hud-topbar--steps` | columna; sin relleno a los lados; alto 52 por código (74 con el sello, `SealMotion`); `overflow: hidden`; borde 1 `border.subtle`; radio 14; vidrio | AC-WKT-14-01 |
| `#hud-pill-row` (`.rm-hud-pill-row`) | alto 20; el ancho de la barra; `pickingMode` `Ignore`; no enfocable; todo lo dibuja el código (las píldoras, el brillo, el halo con `PillGlowTexture`, el pulgar y los rieles) | AC-WKT-14-07, -16, -17 |
| `.rm-hud-step-row` | relleno 12 / 0 / 12 / 8; `min-height` 22; **los aires del plan por código** (`InfoRowPlan`: 8 o 6 antes de la cuenta, 10 o 6 antes del reloj; §2.15) | AC-WKT-14-01, -14, -15 |
| `#hud-step-target` (`.rm-hud-pill`) | *slot* que recorta, `flex-shrink: 1`, `white-space: nowrap`, **sin** `text-overflow` (el texto con sus fragmentos y el "…" en `text.primary` lo arma el código); su opacidad por código al irse | AC-WKT-14-03, -15 |
| `#hud-up-next` | `margin-left: auto` (M5; si S-B4b no da, el espaciador de la 4a); **el relleno lateral por código** (7 o 4) | AC-WKT-14-04 |
| `#hud-wall-clock` (`.rm-hud-wall-clock`) | `TabularNumber` `.rm-type-num-s` con el tamaño en línea; el prefijo `schedule` por código, centrado en la banda de las mayúsculas; `margin-left` del plan (10 o 6) en la barra de pasos y `auto` en la libre; `flex-shrink: 0`; sin eventos ni foco | AC-WKT-14-14, -19-01, -19-05 |
| `#hud-seal` (`.rm-hud-seal`, `__icon`, `__text`) | *slot* que recorta, alto por código (0 ↔ 22: 17 de contenido arriba + 5); relleno 12 a los lados; contenido en fila, centrado, `white-space: nowrap`; la escala de ajuste por código | AC-WKT-09-08 |
| `#hud-free-title` | `flex-shrink: 1`; **la escala del título por código** (`FreeTitleScale`, 0,8 a 1, desde su borde izquierdo) y `text-overflow: ellipsis` solo por debajo de 0,8 | AC-WKT-16-06 |
| `#hud-phase-chip` (`.rm-hud-phase-chip`, `__dot`, `__label`) | **ancho fijo por código** (`PhaseChip.FixedWidthBase` × s: 101,8 / 74,1 base); contenido centrado; relleno 8 × 3; radio `r.m`; tinte, borde y color del texto por código; `flex-shrink: 0`; el punto de 6 con radio 3 y 4 de aire, su opacidad por código | AC-WKT-16-03, -05 |
| `#hud-big-countdown` (`.rm-hud-big-countdown`) | absoluto en `HudLayout.BigCount`; `TabularNumber` `.rm-type-num-hero` con el tamaño en línea; el disco y el aro los dibuja el código; sin eventos | AC-WKT-14-18 |
| `#hud-paused-label` (`.rm-hud-paused-label`) **[DN4b-14]** | absoluto en el disco de `HudLayout.BigCount` (no en `Callout`, que sale); fondo `glass.fallback`, `border-radius` 36·s y `border-width` 3·s en `phase.paused`, en px por código; el glifo de pausa lo dibuja su `generateVisualContent`; sin eventos ni foco | AC-WKT-13-12 |
| `#hud-backdrop` | el tinte del velo por código (`unityBackgroundImageTintColor`) | AC-WKT-13-12 |
| `#completion-dialog` (`.rm-dialog--completion`) | el `ModalDialog` opaco de la It. 3; `.rm-completion__header` en `Pinned`, en fila: el check de 32, 12 de aire y el título; 12 hasta el contenido; `.rm-completion__message`, 16 y `.rm-completion__summary` blanco al 4 %, radio 8, relleno 12; filas `.rm-completion__row` de 22 con 8 de separación (`:982`, `:987`); el ícono de 20 y 12 de aire; 20 hasta las acciones | AC-WKT-21-03 |

**Clase de texto y color por elemento nuevo** (04-design-notes §2.2; **[notas]** = cambia respecto de la primera versión):

| Elemento | Clase (fuente) | Tamaño | Color |
|---|---|---|---|
| `#hud-wall-clock` | `.rm-type-num-s` (derivada 700) | 20, en línea | `text.primary`; el ícono, `text.primary` al 70 % |
| `#hud-phase-label` | `.rm-type-header-s` (Inter 700) | 11, en línea, tracking 0,08 em | `phase.running` · `phase.paused` · **`phase.finished.hud`** **[notas]** |
| `#hud-seal-text`, el título | `.rm-type-header-s` (Inter 700) | 13, en línea | `state.success` o `phase.paused` |
| `#hud-seal-text`, el detalle | Inter 600 por texto enriquecido | 12 | `text.primary` |
| `#hud-seal-icon` | fuente de íconos | 14 | el del título |
| `#hud-big-countdown` | `.rm-type-num-hero` (derivada 800) | 64, en línea | `text.primary`; el disco en `glass.fallback`, el aro en el color base del paso (pista al 20 %) |
| `#hud-paused-label` **[notas]** | `.rm-type-header-s` (Inter 700) | 12, en línea, tracking 0,08 em | `text.primary`; el glifo en `text.primary`; el disco en `glass.fallback`; el aro en `phase.paused` |
| `.rm-completion__title` | `.rm-type-title` | 20/600, sigue la letra | `text.primary` |
| `.rm-completion__message` | `.rm-type-body` | 15, sigue la letra | `text.secondary` |
| `.rm-completion__row-label` **[notas]** | `.rm-type-body-m` | 14/400, sigue la letra | `text.secondary` |
| `.rm-completion__row-value` **[notas]** | `TabularNumber` `.rm-type-num-s` | 20/700, sigue la letra | `text.primary`; la `m`, sufijo Inter 500 de 14 en `text.secondary` a 3 |
| `.rm-completion__row-icon` | fuente de íconos | 20 | `accent.primary` |
| `#completion-check` | dibujado | 32 | el disco en `state.success`; el trazo en `surface.overlay` |
| `#completion-finish` | `.rm-type-button` | 14/600 | `text.onAccent` |

**Lo que crea WP-B para WP-D2:** `.rm-hud-pill-row`, `.rm-hud-wall-clock`, `.rm-hud-seal` (`__icon`, `__text`), `.rm-hud-phase-chip` (`__dot`, `__label`), `.rm-hud-big-countdown`, `.rm-hud-paused-label`, `.rm-dialog--completion`, `.rm-completion__header`, `__check`, `__title`, `__message`, `__summary`, `__row` (`__row-icon`, `__row-label`, `__row-value`). **Lo que es de WP-D2:** *hover*, presionado y foco de `#completion-finish` (los de la app) y las opacidades finas que no son contrato. Las posiciones, la escala, los tamaños base, el *tracking*, el disco de `PAUSA` y los colores semánticos son contrato.

### 7.6 Excepciones de dueño

| Archivo | Dueño | Excepción |
|---|---|---|
| `Core/Design/MotionSpecs.cs`, `HudGeometry.cs` y sus tests | WP-A | las 04-design-notes ya están integradas (Anexo B). Si una revisión del Designer cambia un número del bloque de la 4b (con el visto bueno del PO cuando contradice la spec), el paquete que siga cambia **solo esa constante y su valor en el test**, y lo anota |
| `Core/Design/Tokens.cs` y `Presentation/Styles/Tokens.uss` (G) | el Designer (WP-D2) | **WP-A suma solo `phase.finished.hud` `#FF7A92`** (rol `HudText`; 135 → 136) y regenera `Tokens.uss` con `tools/tokens`, con `--check` en 0, porque `PhaseChip` lo necesita antes de WP-D2 (DN4b-15; QA-G4bT-10 (b)). Los conteos de las pruebas leen el catálogo y no cambian (como QA-G4aT-15 en la 4a); la biblia la alinea WP-D2 |
| `Tests/EditMode/**` | WP-A | **`Tests/EditMode/Project/{SnapshotEvidenceTests,ItFourBToolingTests}.cs` son de WP-B3** (miran el arnés, las tomas y las herramientas de B3), aunque corren con dotnet en el conjunto de EditMode (QA-G4bT-10 (a)). WP-A no los toca |
| `Tests/EditMode/Design/ContrastTests.cs` | WP-A | WP-D2 suma el grupo `ItFourBDesignerPairs` (B1 a B10, DN4b-29) e `ItFourBControlValuesMatchTheDesignNotes`, y no toca los otros. El par B1 de `FINALIZADO` va además en PCH (WP-A) |
| `Presentation/Controls/FitBox.cs`, `Presentation/Motion/{SpringMotion,StepPulse,TimedMotion}.cs` | de la 4a; en la 4b, WP-D1 | WP-D1 solo les suma `ISettlingMotion` y `Reached` (§7.4), sin cambiar su conducta; las vistas de B los siguen usando como en la 4a |
| `Presentation/Controls/TabularNumber.cs` | WP-B0 | los afijos, la placa y `RollProgress`; nadie más lo toca en la 4b |
| las PlayMode de la 4a que miran la flecha, la unidad, el halo o el corazón | WP-B | WP-B0 las pone al día con los afijos (M1, M2, M4, M6), sin bajar sus tolerancias |
| `Snapshots/WorkoutCatalog.cs` | WP-B | WP-B3 solo marca las tomas reemplazadas (§5.6) |
| `docs/migration/iterations/04b-hud-exr/04-design-notes.md` | el Designer | alinea §4.5 y §4.14 ("sin destello", QA-G4bT-03) y §7.1 (CAP-22 a los 3016,6) |
| `docs/migration/design/*` | WP-D2 | — |

---

## 8. Riesgos y desvíos

### 8.1 Desvíos propuestos

| ID | Qué | Por qué | Decide |
|---|---|---|---|
| DT4b-01 | Las píldoras se dibujan en **un** elemento: no hay `hud-pill-<n>`; las pruebas leen cada píldora por índice en la vista (`PillRowView.LookAt(n)`, `RectOf(n)`) | El presupuesto (§3.11) y 04b/00-deuda-diseno §4.4. La spec deja los nombres al Architect (§12 punto 11) | — |
| DT4b-02 | El sello: tercera fila del vidrio de la barra, con sus tiempos dentro de los 2 s, `No alcanzado` en `#FFD166` y el detalle en blanco (la spec) | "Bajo la barra de pasos, sobre el vidrio" se cumple debajo de las píldoras y de la fila de información, en el mismo vidrio | **Cerrado**: el Designer lo confirmó (DN4b-02) y el PO también (spec §16.1; AC-WKT-14-01 y -09-08 dicen "tercera fila") |
| DT4b-03 | La entrada y la salida con los números de la spec | AC-WKT-13-06 y VID-01 piden números | **Cerrado**: el Designer confirmó los de la spec y retiró los suyos (DN4b-01) |
| DT4b-04 | La cuenta grande con la cifra de 64 (`num.hero`, la de SPM) en un disco de 72·s | La spec pide "al menos el tamaño del de SPM". Con 72·s entra en los cinco perfiles con 8·s de aire (§3.5) | **Cerrado** (DN4b-03; spec §16.1) |
| DT4b-05 | Las barras del sistema: "mostrar al terminar cada salida", salvo que otro inmersivo ya esté entrando o arriba | Con QA-V4a-01 dos inmersivos conviven unos cientos de ms | **Cerrado**: el PO lo escribió en AC-APP-15-04 (spec §16.1) |
| DT4b-06 | P-17: las señales de Windows (el aviso de suspensión y la contabilidad de sueño del núcleo) y, si no llega ninguna, el lapso cuenta como un cuadro tarde (la 4a) | La spec pide elegir la señal y documentar qué pasa si no llega (§12 punto 5). Va en el ADR 020 | QA (informativo); el usuario lo verifica (demo (c)) |
| DT4b-07 | Las tomas del HUD de `it4a` quedan **reemplazadas** por las de `it4b` y no se regeneran otra vez | La regeneración única ya se hizo (§5.1); desde WP-B1 esas tomas ya no son la app, y regenerarlas con los IDs y el "qué tiene que verse" de la 4a confundiría el registro | QA |
| DT4b-08 | El reloj de pared es un `TabularNumber` con el ícono de prefijo: conserva el rodillo de la spec en un elemento | AC-WKT-19-02 pide el rodillo; el Designer ofrecía una etiqueta con fundido para el presupuesto | — |
| DT4b-09 | El tope de 15 s del cierre cuenta desde el fin de la rutina (DT4a-13), no desde `Terminar`; un cierre tardío que vuelve bien antes de `Terminar` cuenta como guardado | Es el mismo tope de la 4a; el aviso sale recién al volver a reposo | **Cerrado**: el PO lo escribió en AC-WKT-21-06 (spec §16.1) |
| DT4b-10 | Los controles responden desde que empieza su tramo de la cascada (840 ms; con reducir movimiento, desde el cuadro 0), **al toque y al teclado** (antes no se enfocan); "Atrás", desde el primer cuadro | P-15 de la spec ("desde que empiezan a aparecer") y AC-APP-05-07 (un Enter nunca activa un control que no se ve; QA-G4bT-09 (a)) | — (el Designer, de acuerdo) |
| DT4b-11 | El chip de fase con ancho fijo: máx(`EN MARCHA` + el punto, `PAUSADO`, `FINALIZADO`) en el idioma de la app, con el contenido centrado (101,8 / 74,1 base) | El reloj no se corre cuando cambia la fase; compatible con AC-WKT-16-06 | — (el Designer, de acuerdo, con el lugar del punto solo en `EN MARCHA`: DN4b-26) |
| DT4b-12 | ~~Si la fila de información no entra, achicar los aires y el relleno de `Próximo` (8 → 5)~~ | **Retirado**: lo reemplaza la regla de DN4b-16 (modos y fragmentos, §2.15), que el PO tomó en el gate (spec §16.1, QA-G4b-P01) | — |
| DT4b-13 | El offset de la zona se lee cada 0,5 s sin la caché de `TimeZoneInfo` (Windows: `GetTimeZoneInformation`) | Con la caché, un cambio de zona no se ve hasta reiniciar la app (AC-WKT-19-04 pide 1 s) | — |
| DT4b-14 | `FrameTimeProbe` en el build de simulador: el resumen de tiempos de cuadro en el registro | Es la evidencia de AC-WKT-14-19 [Device-Win] sin el *Profiler* | QA |
| DT4b-15 | ~~Con el diálogo de completado, la llave a `16 + margen inferior`~~ | **Retirado**: se encimaba con `Terminar` (TV, P360) o con la banda (TH, L640). Lo reemplaza R10 de DN4b-22, el hueco libre más bajo del carril izquierdo (§5.7) | — |
| DT4b-16 | Los valores del diálogo son `TabularNumber` que siguen la letra del sistema, en **`num.s` 20/700** con la `m` de sufijo (DN4b-21; antes, `num.stat` 16/700) | AC-WKT-17-12 pide las cifras de tablero también en el diálogo; AC-WKT-21-12, que crezca con la letra; a 1,3× en TV, `Maratón` usa 251,6 de 259 | — (DN4b-08, DN4b-21) |
| DT4b-17 | El peor caso del presupuesto se redefine con los tres objetivos fuera, BPM, `Próximo`, la cuenta grande y el sello | El de la 4a (SPM y Watts) no era el peor posible | **Aceptado** por QA (gate técnico §15) |
| DT4b-18 | La pausa a los 47,0 s de CAP-27 llega en el mismo instante que el tick del 3: la cuenta grande entra y sale en ese instante, y con un disco por vez `PAUSA` entra a los 47,12 | Es lo que dan las reglas (en pausa no hay cuenta grande; un disco por vez, §3.5); a los 47,6 `PAUSA` está quieto | — (QA, de acuerdo) |
| DT4b-19 | La regla de toma con una lista de bloqueos con nombre (§5.2): las fases finitas de los `IMotion` de la 4b, lo de duración fija de la 4a que no llegó a su final, los rodillos antes de su mitad y los resortes de la 4a a más de 0,15 dp | "Falla si corre una animación finita" no tenía definición operativa y, al pie de la letra, hacía fallar 9 capturas y 7 tomas de la demo (QA-G4bT-07). Con la lista, las tomas de k,6 s (rodillos de las lecturas de k − 0,5 por la mitad) y las de +0,6 s de un cambio de paso (el ancho de `Próximo` a 0,034 dp) son estables y dan los mismos bytes en dos corridas | QA |
| DT4b-20 | En el Editor el aviso de suspensión no se registra (solo vale la medida del núcleo): con el modo de espera moderno y sin medida, en el Editor el lapso cuenta | Una recarga de dominio dejaría al sistema llamando a código descargado (QA-G4bT-05 (c)); la demo (c) del usuario es con el build, que sí lo registra | QA (informativo) |
| DT4b-21 | Con tramos de menos de un píxel (el tope del editor), la tira se dibuja por columnas de píxel con el color promedio de los tramos que cubre cada una | Así el costo de la fila no crece con `n` (QA-G4bT-11 (c)); es lo que haría el suavizado: la franja muestra igual cuánto se hizo, el pulgar y lo que falta (DV-40) | Designer (WP-D2, en las tomas) |

### 8.2 La spec y el Designer: dónde no coincidían

Ya coinciden: el Designer confirmó los números de la spec y retiró sus alternativas (04-design-notes §1, DN4b-01 a -04), y el PO decidió lo demás en el gate (spec §16.1). La tabla queda como registro; cada número sigue siendo una constante (§3.1).

| # | Tema | Spec | 04b/00-deuda-diseno y las biblias | Quedó |
|---|---|---|---|---|
| C-01 | Entrada | negro en 220; el fondo; cascada a los 400 / 520 / 600 / 680 / 760 / 840, de 280 cada una, subida ≤ 12; todo a ≈ 1,1 s (AC-WKT-13-06, VID-01) | la página funde en 240, sin negro; cascada desde los 120 cada 40, de 240 cada una, +8; todo quieto a los 560 | la spec (DN4b-01) |
| C-02 | Salida | el HUD se va junto, el fondo funde a negro y vuelve Workout, en menos de 1 s | la página funde en 200 | la spec: HUD 0-200, negro 200-400, Workout 400-640 (DN4b-01, -09) |
| C-03 | Barras del sistema, cuándo vuelven | al terminar cada salida (AC-APP-15-04) | en el cuadro 0 de la salida | la spec: a los 640 (04-design-notes §4.2) |
| C-04 | Sello, color de `No alcanzado` | `#FFD166` | `state.warning` `#FF9800` | la spec (DN4b-02: `#FF9800` es casi el naranja de Split, `#FF9F1C`) |
| C-05 | Sello, detalle | todos los objetivos del paso, en blanco; lo que no entra se achica | solo los que fallaron; `text.secondary`; si no entra, "…" | la spec (DN4b-02) |
| C-06 | Sello, tiempos | entra en 240 y sale en 200 dentro de los 2 s | entra a los 120 (resorte ω 12), sale de los 2000 a los 2280 | la spec (DN4b-02) |
| C-07 | Cuenta grande, cifra | al menos el tamaño del valor de SPM (64) | `num.xl` 52 en un disco de 64·s | la spec, en un disco de 72·s (DT4b-04, DN4b-03) |
| C-08 | Cuenta grande en pausa | no está: `PAUSA` en su lugar | queda a la vista, quieta | la spec, con un disco por vez (DN4b-14) |
| C-09 | `PAUSA` | sí (`hudPausedLabel`, AC-WKT-13-12) | "queda para cuando la pida el PO" | la spec, en el disco (DN4b-14) |
| C-10 | Chip, texto | la variante legible del matiz | en blanco | la variante: `FINALIZADO` en `#FF7A92` (DV-44 al día, DN4b-15) |
| C-11 | Chip, punto | solo en `EN MARCHA` | en el color de la fase; quieto en `PAUSADO` y `FINALIZADO` | la spec (DN4b-04, -26) |
| C-12 | Reloj de pared, cambio de minuto | el rodillo del HUD | el rodillo, o un fundido de 80 si es una sola etiqueta | el rodillo, en un elemento (DT4b-08) |
| C-13 | Controles durante la entrada | desde que empiezan a aparecer (P-15) | desde el cuadro 0 | la spec (DT4b-10), también para el teclado |

### 8.3 Riesgos

| ID | Riesgo | Mitigación |
|---|---|---|
| R4b-01 | P-17 en el modo de espera moderno: no está medido si llega el aviso ni si el núcleo cuenta ese lapso como sueño | Dos señales; si no llega ninguna, el lapso cuenta (la 4a) y el registro lo dice (§2.9.3). Lo verifica el usuario en la demo (c); si falla en su PC, se suma otra señal (por ejemplo, el aviso de pantalla apagada con la sesión en marcha, que la app no deja apagar sola) |
| R4b-02 | La fila con 200 píldoras se repinta cada cuadro durante los últimos 5 s de cada paso | Repintar solo si cambió algo (§3.4); la medición de §3.13; si en el teléfono (6a) pesa, dos elementos (+1, que el presupuesto tiene) |
| R4b-03 | Las juntas M1, M2, M4 y M6 tocan piezas de la 4a ya verificadas | `MergedPiecesMatchTheFourAGeometry` (± 1 dp contra las líneas de base de la 4a) y las sondas de `it4a` corridas en B0 sin fallas (§7.0) |
| R4b-04 | APIs de UI Toolkit 6000.3 sin verificar en este uso (`DrawText` con otros `FontAsset`, `margin-left: auto`, el texto enriquecido en negrita y en dos pesos, `Painter2D` con `Allocate` y con texto en un `Label`, la textura del halo estirada por lado, la medida del marcado de la pastilla, el tinte del fondo) | La sonda S-B4b al empezar B0, con un plan B para cada una (§7.0) |
| R4b-05 | Android: las barras, los márgenes estables y que Unity no vuelva a poner sus propias banderas | NO VERIFICABLE sin el módulo y el teléfono; se vuelve a pedir el modo al volver del segundo plano y al recuperar el foco (§4.1); el *bootstrapper* deja apagado "Hide navigation bar" |
| R4b-06 | El *callback* del aviso de suspensión con IL2CPP (P/Invoke inverso) y su delegado | Método estático con `[AOT.MonoPInvokeCallback]`, el delegado en un campo estático, baja en `Dispose`; `SystemSleepSourceTests` en el Editor; IL2CPP NO VERIFICABLE sin el módulo |
| R4b-07 | Regenerar el oráculo necesita Flutter y la caché de `pub` | El guion nuevo se ignora con el motivo si no está; los 15 de antes cubren AC-WKT-21-13 (§2.13) |
| R4b-08 | Una revisión tardía de las 04-design-notes cambia un número | Constantes en un solo lugar (§3.1) y la excepción de §7.6; las notas ya están integradas (Anexo B) |
| R4b-09 | Los márgenes más justos de las notas (CAP-01 con 6,1 base en compacto 1, `>160W…` de `Prueba 4a` con 0,3, el sello de `Pirámide 500` con 1,1 y `PAUSED` en el disco con 2,7 por lado) salen de fontTools con las tablas K2, no de Unity | WP-B1 los mide en Unity (SBT, SVT, CCT); el plan de `InfoRowFit` tiene 1 base de margen y, si un caso cambia de modo, cambia lo esperado de su toma y se anota |
| R4b-10 | La salida pasa de 200 a 640 ms: más tiempo con dos inmersivos (QA-V4a-01) y pruebas de la 4a que esperaban 200 | `WorkoutScreenTests.StartDuringThePreviousExitHasItsOwnImmersive` con la salida nueva; las pruebas esperan con `Settle` y no con 200 fijos |
| R4b-11 | Los controles no responden en los primeros 840 ms de la entrada | Es lo que pide P-15; "Atrás" responde desde el cuadro 0; las pruebas que tocan `Pausa` en el acto pasan a esperar la cascada |
| R4b-12 | `HudBudgetTests` en el tope si la sonda S-B4b falla entera (66 en la pausa con el sello) | Se decide antes de B1 con el informe de B0: subir el tope con una medición o juntar la etiqueta y el valor de las tarjetas chicas (la salida de R4a-03) |

### 8.4 Pedidos al Designer (para las 04-design-notes de la 4b): respondidos

Los trece están respondidos en 04-design-notes §1; qué se tomó de cada uno, en el Anexo B.

| ID | Qué | Respuesta |
|---|---|---|
| DN4b-01 | La entrada y la salida (C-01, C-02) | la spec |
| DN4b-02 | El sello: el color de `No alcanzado`, el del detalle y los tiempos (C-04 a C-06) | la spec, con `check_circle` relleno y `error` de contorno |
| DN4b-03 | La cuenta grande y `PAUSA` | 64 en 72·s; `PAUSA` en el mismo disco (DN4b-14) |
| DN4b-04 | El chip: el texto de DV-44, sin punto en `PAUSADO` y `FINALIZADO` | `FINALIZADO` en `#FF7A92` (DN4b-15); ancho fijo con el punto solo en `EN MARCHA` (DN4b-26) |
| DN4b-05 | Qué se achica en la fila más larga | los modos y los fragmentos (DN4b-16; §2.15) |
| DN4b-06 | La barra libre en TV en español | el título se achica hasta 0,8 (DN4b-27; el PO lo aceptó) |
| DN4b-07 | CAP-01 en TV | compacto 1, con `18 spm` entera |
| DN4b-08 | El diálogo: el tamaño de los valores y el check | `num.s` 20/700, el check de 32 junto al título (DN4b-21) |
| DN4b-09 | "Vuelve Workout" | la página negra que se va en 240 |
| DN4b-10 | La en curso en la tira | el pulgar en toda píldora de menos de 6 base (DN4b-18; el PO lo confirmó) |
| DN4b-11 | Las sondas de aspecto de `it4b` | 04-design-notes §7 (DN4b-28) |
| DN4b-12 | Los pares de contraste | B1 a B10 (DN4b-29) |
| DN4b-13 | La llave con el diálogo | R10 en el hueco libre más bajo del carril izquierdo (DN4b-22) |

### 8.5 Para el PO, QA y el coordinador

| Para | Qué |
|---|---|
| PO | Nada pendiente de este diseño: las decisiones del gate (spec §16.1) están integradas (DT4b-02 a -05, -09; DN4b-16, -18, -23, -25, -27; DEMO-08 y CAP-22). D-ANI-05 sigue yendo al gate del usuario (si se rechaza, `HudFeatures` apaga la cuenta grande y el sello) |
| Designer | Alinear 04-design-notes §4.5 y §4.14 con "sin destello ni trazo" con reducir movimiento (QA-G4bT-03, spec §16.1) y §7.1 con CAP-22 a los 3016,6 (relleno 0,553). WP-D2 después de WP-B3 |
| QA | Antes de WP-B1, las dos condiciones y la regeneración única (§5.1). La demo (c) la hace el usuario: QA queda NO VERIFICABLE en equipo para AC-WKT-02-11, con su `[Ignore]`. Lo NO VERIFICABLE sin el módulo de Android, sin IL2CPP o sin Mac (§6.6). DT4b-19 (la regla de toma) y DT4b-20 (el aviso en el Editor) son suyos. El legacy sin cambios, las bases del usuario sin tocar y ninguna ruta de la PC en lo versionado |
| Coordinador | Si el `RowMate.exe` del usuario sigue abierto, el build de B3 espera (§7.0). Para la demo (c): el registro del build dice `[WORKOUT] el equipo durmió N s: no cuenta (P-17; …)` o, si no hubo señal, `[TIME] cuadro de N s sin aviso de suspensión: cuenta`. Para AC-WKT-14-19, el registro del build dice `[PERF] cpu del hilo principal: máx X ms…` (§3.13). Flutter con la caché de `pub` para regenerar el oráculo (§2.13) |

---

## Anexo A. Medición de anchos (aproximada)

**Queda como registro de la primera versión.** Las medidas que usa este diseño son las de 04-design-notes §3.3, §3.5 y §3.7, hechas con las tablas K2 del repo (`measure4b.py` del Designer): por ejemplo, `Texto largo` mide 346,5 y no 347,0, CAP-01 339,8 y no 340,9, y el chip 101,8 / 74,1 y no 102,2 / 84,1. Las conclusiones de abajo no cambian.

Con las fuentes del repo (`UI/Fonts/`), fontTools 4.68.0 sin red y `<scratch>/it4b/arch/measure.py`: avances de `hmtx`, **sin kerning** (las tablas K2 cambian del orden del 1 al 2 %), las cifras de `TabularNumber` en celdas de 0,521 em (derivada 700) y `:` y `.` en celdas de 0,32 em. Todo en dp base. Ancho disponible de la fila de información: la barra a 12·s de cada lado, menos el borde y el relleno de 12 y 12 (TV 339,9; TH 817,2; L640 643,4; W1280 750,0).

| Qué | Medida | Resultado |
|---|---|---|
| `Texto largo` a los 8 s, es, sin la pastilla: `Calentamiento` (13/700, tracking 0,04) + `Próximo: Enfriamiento` con su relleno + `0:12` + reloj + aires | 99,6 + 122,7 + 37,7 + 69,1 + 18 = **347,0** | **TV: no entra por 7,1** aun sin la pastilla (DN4b-05). TH, L640 y W1280 sobran 296 a 470 |
| Lo mismo en inglés (`Warmup`, `Up next: Cooldown`) | 290,0 | TV: sobran 50 para la pastilla |
| `Estados` a los 40 s (CAP-01): `Trabajo`, `18 spm`, `Próximo: Descanso`, `0:10`, reloj | 340,9 | TV: 1,0 de más, dentro del error de la medición (DN4b-07) |
| Barra libre, es: ícono + 8 + `Entrenamiento Libre` (16/600) + reloj + 10 + chip de ancho fijo | 365,5 contra 335,9 | **TV: el título se recorta ≈ 30** (DN4b-06); TH, L640 y W1280 entran |
| Barra libre, en (`Free Workout`) | 296,6 | entra en los cuatro |
| Chip de ancho fijo (la etiqueta más larga + 16 de relleno + 10 del punto) | es 102,2 (`FINALIZADO` 76,2; `EN MARCHA` 75,8); en 84,1 (`FINISHED` 58,1) | — |
| Sello `Objetivo cumplido · 150 / 150 W · 02:05 / 02:05/500m` (ícono 14 + 5 + 13/700 + 12/600) | 339,0 | TV: entra justo (339,9); con los extremos de FTMS (434,4) se achica |
| Sello `No alcanzado · 150 / 160 W · 24.0 / 22 spm` | 272,8 | entra en los cuatro |
| Cuenta grande `3` a 64 | 33,3 de ancho; mayúscula 0,7 em = 44,8 | entra en el interior del aro de un disco de 72 (66) |

---

## Anexo B. Cambios por 04-design-notes de la It. 4b (DN4b-01 a DN4b-29)

Las 04-design-notes llegaron después de la primera versión de este diseño. QA-G4bT-01 pide aceptar o rechazar cada respuesta y cada pedido y dejar el contrato al día, como QA-G4aT-01 en la 4a. **Se aceptan las 29.** Las que tocan un número o un texto de la spec las decidió el PO en el gate (spec §16.1: DN4b-05/-16 por el camino (a) de QA-G4b-P01, DN4b-10/-18, -15, -23, -25 y -27). Ninguna cambia datos ni reglas del motor. Una sola precisión, que no es un rechazo: con reducir movimiento el cambio de paso va **sin destello** (AC-APP-05-08; QA-G4bT-03), y las notas §4.5 y §4.14 lo conservaban; la spec y el PO lo confirmaron y lo alinea el Designer.

"Antes de" es el paquete que no puede empezar sin el cambio (QA-G4bT-01: DN4b-14 a -18, -22, -23 y -25 antes de WP-A; los cuatro movimientos nuevos, con su línea de tiempo pura en WP-A y su `IMotion` en WP-D1, antes de WP-D1; el resto antes de WP-B1).

| ID | Decisión | Qué cambia en este diseño | Paquete | Antes de |
|---|---|---|---|---|
| DN4b-01 (respuesta) | Aceptada | La entrada y la salida de la spec; sale la columna "Designer" de §3.1 y C-01, C-02 quedan cerradas (§3.2, §3.3, §8.2) | — | — |
| DN4b-02 (respuesta) | Aceptada | El sello de la spec (`#FFD166`, el detalle blanco, los tiempos dentro de los 2 s), con `check_circle` relleno y `error` de contorno (§3.6); C-04 a C-06 cerradas | B1 | WP-B1 |
| DN4b-03 (respuesta) | Aceptada | 64 en un disco de 72·s (DT4b-04 cerrado); `PAUSA` pasa al disco (DN4b-14) | A, B1 | WP-A |
| DN4b-04 (respuesta) | Aceptada | El chip con el texto en su color, `FINALIZADO` en `#FF7A92`, el punto solo en `EN MARCHA` y el ancho fijo (DN4b-15, -26) | A, B1 | WP-A |
| DN4b-05 (respuesta) | Aceptada (el PO tomó el camino (a)) | Reemplaza a DT4b-12, que se retira: la regla de DN4b-16 | A, B0, B1 | WP-A |
| DN4b-06 (respuesta) | Aceptada (el PO aceptó DN4b-27) | El título libre que se achica hasta 0,8; CAP-13 lo espera entero | A, B1 | WP-B1 |
| DN4b-07 (respuesta) | Aceptada | CAP-01 en compacto 1 con `18 spm` entera (§2.15, §5.3) | A, B3 | WP-A |
| DN4b-08 (respuesta) | Aceptada | Los valores del diálogo en `num.s` 20/700 (DN4b-21; DT4b-16 al día) | B2 | WP-B2 |
| DN4b-09 (respuesta) | Aceptada | Workout vuelve con la página negra que se va en 240, sin entrada propia (ya era así; §3.3) | — | — |
| DN4b-10 (respuesta) | Aceptada (el PO confirmó la excepción a Q-59) | El pulgar en toda píldora de menos de 6 base (DN4b-18); sale `StripCurrentMin` 2 | A, B1, D1 | WP-A |
| DN4b-11 (respuesta) | Aceptada | Las sondas de aspecto de `it4b` son las de 04-design-notes §7 (DN4b-28) | B3 | WP-B3 |
| DN4b-12 (respuesta) | Aceptada | B1 a B10 para `ItFourBDesignerPairs` (DN4b-29); B1 también en PCH | A (B1), D2 | WP-A (B1) |
| DN4b-13 (respuesta) | Aceptada | Reemplaza a DT4b-15, que se retira: R10 de DN4b-22 | A | WP-A |
| Respuestas a DT4b-02, -03, -04, -08, -10, -11, -15, -16, -18 y a P-07, P-08 | Aceptadas | DT4b-08, -10, -11 y -18 quedan; DT4b-02, -03, -04 cerrados; -15 y -12 retirados; -16 al día (§8.1). El reloj de pared en 20 (P-07) y el diálogo opaco (P-08), sin cambios | — | — |
| **DN4b-14** [contrato] | Aceptada | `PAUSA` en el disco de `HudLayout.BigCount` (sale `HudLayout.Callout`): fondo, `border-radius` 36·s y aro de 3·s en `phase.paused` por código, el glifo en su `generateVisualContent` (sonda S-B4b; si no, un `IconView`, +1), la etiqueta 12/700 con 0,08 em; **un disco por vez** con `CenterDiscTimeline` (WP-A) y `CenterDiscMotion` (WP-D1), que junta `BigCountMotion` y la parte de `PAUSA` de `PauseMotion` (el velo queda en `PauseVeilMotion`) (§3.5, §3.7, §7.4, §7.5; CAP-14, -27) | A, D1, B1 | WP-A (geometría y línea de tiempo), WP-D1 (el `IMotion`) |
| **DN4b-15** | Aceptada | `phase.finished.hud` `#FF7A92` (135 → 136) en `Tokens.cs` y `Tokens.uss` por WP-A, con `tools/tokens` (excepción de §7.6); `PhaseChipSpec.Text` de `FINALIZADO`; PCH con el par B1 (§2.7; CAP-15) | A | WP-A |
| **DN4b-16** [contrato] | Aceptada (spec §16.1) | `InfoRowFit` con los tres modos y los fragmentos (§2.15, nuevo), `HudTargetLabel.Fragments` (§2.11), la pastilla armada por fragmentos con el "…" blanco en `StepInfoBar` (B0) y el plan por paso (B1); el cambio de modo y la pastilla que se va con `InfoRowTimeline` (A) e `InfoRowMotion` (D1). Reemplaza a DT4b-12 (§3.4, §7.5; CAP-01, -19, -20, -31) | A, D1, B0, B1 | WP-A, WP-D1 (el `IMotion`) |
| **DN4b-17** [contrato] | Aceptada | El halo desde el borde a 0,65·α con `(1 − d/E)²` hasta 10 / 8 / 4·s, con `PillGlowTexture` (B1) en 9 partes estiradas por lado (`PillShapes.GlowNineSlice`, A); sale el "agrandado 2, desenfoque 8, recortado a la fila" y `ClippedNineSlice`; las constantes `PillGlowEdgeFactor` y `PillGlowExtent*` (§3.1, §3.4; S-B4b). La pista no se tiñe: el PO lo tomó como sistema visual (QA-G4bT-12) | A, B1 | WP-A |
| **DN4b-18** [contrato] | Aceptada (spec §16.1) | `PillRowLayout.Narrow` y `ThumbOf`, `PillShapes.Rails`, `NarrowWidth` 6, `ThumbMinWidth` 4 y `ThumbHeight` 10 (§2.2); el pulgar que pasa en 150 con `StepBarTimeline.ThumbSwap` (A) y `PillRowMotion` (D1), con su variante de reducir movimiento (§3.4, §3.10; CAP-22) | A, D1, B1 | WP-A, WP-D1 (el `IMotion`) |
| DN4b-19 | Aceptada | `PillPaint.HighlightAlpha` 0,25 solo en la en curso con relleno; el brillo se va en 120-340 en el cambio de paso (§2.1, §3.4; CAP-01, -06) | A, B1 | WP-B1 |
| DN4b-20 | Aceptada | El aro de 360°·(1 − f) a las 12, con el borde que se mueve en el sentido del reloj, extremos rectos y la línea central a 34,5·s (`PillShapes.RingArc`, `BigCountRingClockwise`; §3.5; CAP-02) | A, B1 | WP-B1 |
| DN4b-21 [contrato] | Aceptada | El diálogo: `num.s` 20/700 con la `m` de sufijo 14/500 `text.secondary` a 3, rótulos `bodyM` 14, el check de 32 junto al título, los puntos de `CheckGlyph`, los totales sin rodillo; `SizesAndLetter` con los rectángulos de las notas (§3.9, §7.5) | A (`CheckGlyph`), B2 | WP-B2 |
| **DN4b-22** | Aceptada | R10 en `WrenchPlacement`: el hueco libre más bajo del carril izquierdo, a 8 dp de las piezas y del diálogo, con `HudLayout.LeftLaneObstacles` y `OverlaySurface.Workout(…, leftLane, dialog)`; los cinco lugares en WPL (§5.7; CAP-07, -09, -11) | A, B2 | WP-A |
| **DN4b-23** [contrato, PO] | Aceptada (el PO trajo AC-BLE-16-03 a §5.2) | `HudLayout.PanelCeiling` en las dos orientaciones (8 debajo del disco en vertical y de la barra de 74·s en horizontal); los cinco altos en SPL y HGT; `SimulatorPanelLayout` sin cambios de fórmula (§5.7; DEMO-08; la fila nueva de la matriz) | A, B2 | WP-A |
| DN4b-24 | Aceptada | Los avisos del final a los 400 de la salida (`EndToastStartMs`, `RevealStarted`), en el cuadro 0 con reducir movimiento (§2.11, §3.3; ITT) | B2 | WP-B2 |
| **DN4b-25** | Aceptada (spec §7.1) | Los instantes de las tomas a +0,2 s y CAP-06 a los 30,25, con los rellenos esperados (0,510, 0,503, 0,337, 0,607) en PPG y en las comprobaciones; CAP-22 a los 3016,6 por QA-G4bT-06 (§5.2, §5.3, §5.4) | A (PPG), B3 | WP-A |
| DN4b-26 | Aceptada | `PhaseChip.FixedWidthBase` con el lugar del punto solo en `EN MARCHA` (101,8 / 74,1), el contenido centrado y el punto que va con la etiqueta y empieza en 1 (§2.7, §3.7, §7.4) | A, B1, D1 | WP-B1 |
| DN4b-27 [PO] | Aceptada (el PO la aceptó: AC-WKT-16-06) | `InfoRowFit.FreeTitleScale` (A) en `FreeTopBar` (B1); CAP-13 entero a 0,818 (§2.15, §3.7) | A, B1 | WP-B1 |
| DN4b-28 | Aceptada | Las sondas de §7.2 y §7.3 de las notas con sus tolerancias de §7.1; AUX-01 pasa a ser CAP-31 (spec §16.2) (§5.3) | B3 | WP-B3 |
| DN4b-29 | Aceptada | `ItFourBDesignerPairs` (B1 a B10) e `ItFourBControlValuesMatchTheDesignNotes` en `ContrastTests`, de WP-D2 (excepción de §7.6) | D2 | WP-D2 |

**Los cuatro movimientos nuevos** (QA-G4bT-01), cada uno con su línea de tiempo pura (WP-A, §3.1), su `IMotion` con sus fases finitas (WP-D1, §7.4) y su variante de reducir movimiento (§3.10):

| Movimiento | Línea de tiempo | `IMotion` | Reducir movimiento |
|---|---|---|---|
| El cambio de modo de la fila (0-240, `ease.move`) | `InfoRowTimeline.ModeChange` | `InfoRowMotion` | en el acto |
| La pastilla que funde al entrar `Próximo` (120, `ease.exit`) | `InfoRowTimeline.TargetOut` | `InfoRowMotion` | el mismo fundido |
| El pulgar que pasa a la siguiente (150, `ease.move`) | `StepBarTimeline.ThumbSwap` | `PillRowMotion` | fundido cruzado de 150, sin subir ni bajar |
| Un disco por vez (120 de salida, la entrada desde los 120) | `CenterDiscTimeline.Swap` | `CenterDiscMotion` | los mismos tiempos, sin escala |

**Los pedidos de 00-deuda-diseno** (DD4b-01 a DD4b-18) ya estaban integrados en la primera versión. Las notas reemplazan cuatro partes, y se toma lo de las notas: DD4b-06 (la cascada de 120 a 560, por la de la spec: DN4b-01), DD4b-07 en cuándo vuelven las barras (al terminar la salida, C-03, que las notas aceptan en su §4.2), DD4b-08 en el halo recortado a la fila (por DN4b-17) y DD4b-15 en el texto blanco del chip (por DN4b-04).

---

## Verificación hecha

1. **Legacy, solo lectura** (`3ae0f3a`): se leyeron los archivos del encabezado; `git -C ../rowmate-legacy status --porcelain` quedó vacío al empezar y al terminar. No se leyó `lib/core/strava/strava_secrets.dart`.
2. **Es un documento**: no se escribió código, no se corrió dotnet y no se abrió Unity.
3. **Las medidas del Anexo A** salieron de `<scratch>/it4b/arch/measure.py` con fontTools 4.68.0 (`uv run --offline`), sin red; son aproximadas (sin kerning).
4. **La matriz** (§6.4) se contrastó con la spec corregida con un *script* en `<scratch>/it4b/arch-gate2/`: los **101** IDs de §4 y §5 de la spec están (con AC-BLE-16-03), ninguno de más, más LT-46 (102 filas); los **36** de `UnityOnlyIds` salen de la columna de verificación (las filas sin **d** ni **s**).
5. **Las cuentas de la tabla del Designer** (el lugar de la cuenta grande con el disco de 72·s, el umbral de la tira, el relleno continuo de §2.3) se rehicieron a mano sobre sus números; las de la corrección del gate (los resortes de la 4a en los instantes de las tomas, los bordes de coma flotante, los umbrales del pulgar, los rellenos de las tomas y los dos casos de `Instant` de `PillProgress`) con `<scratch>/it4b/arch-gate/check.py`, en Python de `uv` sin red.
6. **Repo público**: este documento cita el repo con rutas relativas, el legacy como [rowmate-legacy](https://github.com/figuibej/rowmate-legacy) o `../rowmate-legacy`, el scratch como `<scratch>` y el SDK de Flutter como `<flutter-sdk>`; ninguna ruta de la PC ni nombre de usuario (búsqueda mecánica en 0).
7. **Sin commits.** El trabajo en paralelo del PO y del Designer (`01-po-spec.md`, `04-design-notes.md`, `00-deuda-diseno.md`, `00-deuda-4a.md`, las biblias) no se tocó.

---

## Cambios por gate QA

Correcciones por los dos gates de armonía de QA de la 4b, del 2026-10-29: [lente de paridad](03-qa-harmony-paridad.md) (APROBADO CON OBSERVACIONES: 0 bloqueantes, 1 mayor, 7 menores) y [lente técnica](03-qa-harmony-tecnica.md) (APROBADO CON OBSERVACIONES: 0 bloqueantes, 4 mayores, 8 menores). Se resolvieron los **diez issues del Architect** (3 mayores y 7 menores). El diseño se alineó además con la spec corregida (01-po-spec §16: los issues del PO, 101 AC, 31 capturas, DV-45 a DV-47) y con las 04-design-notes, una por una (Anexo B).

**Verificación hecha.** Legacy (solo lectura, `3ae0f3a`): `test/step_pill_test.dart` entero (las cinco píldoras de `:20-26` y las 12 afirmaciones de `:34-52`), `lib/features/workout/step_pill.dart:25-60` (el `switch` de `:43-55` con la rama `else` de la hecha sin resultado) y `lib/features/workout/immersive_workout_screen.dart:393-476` (el `SizedBox(width: 6)` de `:395`, el `Flexible` de `:397`, el `Spacer` de `:416`, el `SizedBox(width: 8)` de `:452` antes de la cuenta, que está siempre, con `Próximo` o sin él, y el de 10 de `:473`). Las cuentas de los resortes y de los bordes, con `<scratch>/it4b/arch-gate/check.py`: el ancho de `Próximo` (ω 12, 5,5 dp) a 0,034 dp a los +0,6 s, la escala de la cuenta a 0,106 dp a los +0,25 s, `30.2 − 30.0 = 0.1999999999999993` y los rellenos 0,510 · 0,503 · 0,553 · 0,337 · 0,607. `git -C ../rowmate-legacy status --porcelain` vacío antes y después. No se leyó `lib/core/strava/strava_secrets.dart`. Es un documento: no se escribió código, no se corrió dotnet y no se abrió Unity.

| Issue | Sev. | Qué cambió | Dónde |
|---|---|---|---|
| QA-G4bT-01 | mayor | **Anexo B**: las 29 DN4b aceptadas, con el paquete y el "antes de" de cada una (DN4b-14 a -18, -22, -23 y -25 antes de WP-A; los cuatro movimientos antes de WP-D1). Al día: el *token* `phase.finished.hud` (135 → 136, con la excepción de dueño); la fila de información por modos y fragmentos en `InfoRowFit` (§2.15, nuevo; sale DT4b-12) y el título libre que se achica; el halo desde el borde a 0,65·α con `(1 − d/E)²` y `PillGlowTexture`; el pulgar y los rieles (sale `StripCurrentMin`); `PAUSA` en el disco de `HudLayout.BigCount` (sale `Callout`); R10 en el hueco libre más bajo del carril izquierdo (sale DT4b-15); el techo del panel en las dos orientaciones con los cinco altos; los avisos a los 400; los valores del diálogo en `num.s` con la `m` de sufijo; el ancho del chip 101,8 / 74,1; los instantes a +0,2 s y CAP-06 a los 30,25. **Los cuatro movimientos sin dueño** (el cambio de modo de la fila, la pastilla que funde, el pulgar que pasa y un disco por vez) tienen su línea de tiempo pura (`InfoRowTimeline`, `StepBarTimeline.ThumbSwap`, `CenterDiscTimeline`), su `IMotion` (`InfoRowMotion`, `PillRowMotion`, `CenterDiscMotion`) con sus fases finitas y su variante de reducir movimiento. Las comprobaciones de CAP-01, -14, -15, -19, -20 y -22 esperan lo de las notas y la spec; CAP-31 nueva | §1.1, §2.2, §2.7, §2.11, §2.15, §3.0, §3.1, §3.3 a §3.11, §5.2, §5.3, §5.7, §6.2, §6.4, §7.0 a §7.6, §8, Anexo B |
| QA-G4bT-03 | mayor | Con reducir movimiento, el cambio de paso va **sin destello ni trazo**, como dice AC-APP-05-08 (el PO lo confirmó, spec §16.1): la sólida solo funde de 1 a 0,85, la hueca baja su interior y su borde funde en 160, el brillo se va con su fundido. `HTL.ReducedVariants` exige el destello en 0 en todo el cambio. El Designer alinea §4.5 y §4.14 de las notas | §3.1 (`PillFlashPeak`), §3.4, §3.10, §6.2 (HTL), §6.4 (AC-APP-05-08), §7.4, §7.6 |
| QA-G4bT-07 | mayor | La regla de toma tiene definición operativa (DT4b-19): `SnapshotTakeRule.Blockers` nombra lo que bloquea (las fases finitas de los `IMotion` de la 4b, lo de duración fija de la 4a sin llegar a su final, los rodillos antes de su mitad y los resortes de la 4a a más de 0,15 dp) y deja `inFlight` en el *manifest*. (a) `IsFinite` por fase en §7.4: el sostén del sello, la cuenta grande entre ticks, el halo, el punto y `PAUSA` quieto son continuos. (b) Las líneas de tiempo y los instantes, en ms enteros (`HudTimelines.Ms`, `Reached`; `AdvanceToMs`); además, CAP-06 pasa a los 30,25. (c) DEMO-04 y DEMO-12 a los 800 ms de la salida y DEMO-08 a los 101,6 s. (d) Los resortes de la 4a cuentan solo a más de 0,15 dp (`ISettlingMotion`): el ancho de `Próximo` a +0,6 s va a 0,034 dp y no bloquea CAP-03, -04, -26 ni -30 | §1.1, §3.1, §5.2, §5.4, §7.3, §7.4, §8.1 |
| QA-G4bT-04 | menor | `PillProgress` con `Instant` en el mismo paso vale `max(anterior; transcurrido / D)` y deja el tick donde estaba si no llegó un segundo nuevo; los dos casos (el despertar a los 62,4 s y la pausa con segundo plano) en PPG, con sus números | §2.3, §6.2 (PPG), §6.4 |
| QA-G4bT-05 | menor | (a) `ResumeFromBackground(in FrameClock)` hace el alcance hasta el monótono despierto, con el mismo `Awake`. (b) Se cuentan también los avisos de "reanuda" (`PBT_APMRESUMEAUTOMATIC`, `PBT_APMRESUMESUSPEND`), que anclan el despertar; el "se suspende" no se consume con un tirón. (c) En el Editor el aviso no se registra (DT4b-20). (d) `DEVICE_NOTIFY_SUBSCRIBE_PARAMETERS` en `AllocHGlobal`, liberada después de dar de baja. Casos nuevos en SUS, WSU, USC y SSL | §1.2, §2.9, §6.2, §8.1 |
| QA-G4bT-08 | menor | (a) `CompletionDialog.OnClosing` (y su `DetachFromPanelEvent`) detienen `CompletionMotion`. (b) Cada vista nueva detiene sus `IMotion` en `DetachFromPanelEvent` y no mide texto sin panel. (c) `WallClockTicker` sin periódico en segundo plano. (d) El `Schedule` de 300 ms que vence en el alcance abre el diálogo con `instantly`. `NothingAnimatesAfterTheExit` suma "`Terminar` durante el conteo" y 0 avisos de DPI con el sello y el chip a la vista | §1.1, §2.6, §2.8, §3.3, §3.4, §3.9, §3.12, §6.2 (ITT, WCL), §7.4 |
| QA-G4bT-09 | menor | (a) `SetInteractive(false)` saca a los controles del foco (`focusable = false`) y la página no lleva el primer Tab al anillo hasta los 840 (con reducir movimiento, desde el 0): `ITT.KeyboardDuringTheCascade`, con Tab y Enter a los 500 y a los 900 ms. (b) `DialogLayer` escucha la raíz del panel y, con un diálogo arriba y el foco afuera, enfoca `FocusTrap.Next(Top.Box, null, …)`: `CDT.FirstTabWithoutFocusReachesTheDialog`. (c) `Dismissible = false` bloquea solo los caminos del usuario (`Dismiss()`): la página cierra el diálogo con `Cancel()` al volver a reposo | §1.1, §3.2, §3.3, §3.9, §6.2 (ITT, CDT), §6.4, §8.1 (DT4b-10) |
| QA-G4bT-10 | menor | (a) y (b) en §7.6: `SnapshotEvidenceTests` e `ItFourBToolingTests` son de WP-B3 aunque vivan en `Tests/EditMode/**`; `Tokens.cs` y `Tokens.uss` los toca WP-A solo para `phase.finished.hud`. (c) S-B4b suma el texto enriquecido del sello, el glifo de `PAUSA` en un `Label`, la textura del halo estirada por lado y la medida del marcado de la pastilla, cada uno con su plan B y su costo (el peor caso con todo en contra pasa de 64 a 66) | §6.2, §7.0, §7.1, §7.2, §7.6, §3.11, §8.3 (R4b-04, -12) |
| QA-G4bT-11 | menor | (a) HBT acota el trabajo del HUD (`HudView.Apply` y el `generateVisualContent` de la fila, con su `Stopwatch`, ≤ 16 ms), no el cuadro del Editor. (b) `FrameTimeProbe` mide la CPU del hilo principal con `FrameTimingManager` (`cpuMainThreadFrameTime`); `enableFrameTimingStats` solo en el build de desarrollo con simulador, por `BuildCli`. (c) HBT también con la fila sola de 1998 pasos, y la tira por columnas de píxel (DT4b-21). (d) `SizesAndLetter` abre el diálogo de `Maratón` con su `HudCompletion` | §1.1, §2.2, §3.9, §3.13, §6.2 (HBT, CDT), §6.4, §7.2, §8.1 |
| QA-G4b-P08 | menor | LT-46 arma las cinco píldoras con `LookOf` (índices antes, en y después del paso en curso, con `Met`, `Missed` y `null`), con el mapeo escrito en el test, y compara el color final (el base por el alfa), el borde y el relleno, con las 12 afirmaciones de `:34-52` una por una; `PillRowTests` hace lo mismo con la fila montada | §2.1, §6.2 (PIL, PRT), §6.3 |

**Alineación con la spec corregida (issues del PO).** QA-G4b-P01 y QA-G4bT-02 (b): la regla de DN4b-16 en §2.15, con CAP-19 y CAP-20 sin pastilla en TV y CAP-31 para el "…" blanco. QA-G4b-P02 (DV-46): la cuenta y el reloj pegados al borde, también en SBT. QA-G4b-P03 y QA-G4bT-12: el halo "solo por fuera" como sistema visual (§3.4). QA-G4b-P04 y QA-G4bT-02 (f) (DV-47): el título que se achica, CAP-13 entero a 0,818. QA-G4b-P05: el título del diálogo en dos líneas (DV-47) y DV-43 en §3.9. QA-G4b-P06 y QA-G4bT-02 (a): `#FF7A92` en §2.7, PCH y CAP-15. QA-G4b-P07 y QA-G4bT-02 (c): el pulgar desde 50 pasos en TV. QA-G4bT-02 (d): AC-BLE-16-03 en la matriz, con **d**, **s**, **ps** y DEMO-08. QA-G4bT-02 (e): los +0,2 s. QA-G4bT-02 (g): la tercera fila. DT4b-05 y -09, confirmados. QA-G4bT-06: CAP-22 a los 3016,6, con `0:14`, el relleno 0,553 y el reloj `10:50`.

**No se disputa ningún issue.** Tres precisiones sobre cómo se cumplen:

1. **QA-G4bT-07 (d), qué resortes de la 4a cuentan.** QA proponía decir si cuentan. Cuentan, pero con un umbral (0,15 dp de lo que les falta, en dp de pantalla): sin umbral, un resorte nunca llega exactamente a su destino y cualquier toma posterior a un cambio de ancho fallaría; con el umbral, las tomas de +0,6 s quedan quietas a menos de un quinto de píxel en W1280 y a menos de medio píxel en TV, y dan los mismos bytes en las dos corridas.
2. **QA-G4bT-07, los rodillos.** Las tomas de "k,6 s" caen con el rodillo de la lectura de k − 0,5 (`R-4`) en el 0,556 de su recorrido. En vez de mover las tomas (lo que cambiaría la fase de los pulsos que la spec fija en p = 1), un rodillo bloquea solo antes de su mitad: después, la cifra nueva ya está casi en su lugar, y la imagen es determinista.
3. **QA-G4bT-11 (c), las dos cosas.** QA proponía medir con 1998 **o** juntar los tramos por píxel; se hacen las dos: la tira por columnas acota el costo (DT4b-21) y HBT lo mide con 1998.

**Pendientes que no son del Architect:** del Designer, alinear 04-design-notes §4.5 y §4.14 con "sin destello" y §7.1 con CAP-22 a los 3016,6 (spec §16.2). Del PO, nada de este diseño.

---

## Cambios por implementación de WP-A

WP-A quedó implementado con los archivos de §7.1 (código de Core, App y Simulator, dobles de `Tests/Support`, las pruebas de dotnet de §6.2, el guion `completed_hold_reset` del oráculo con `workout_engine.json` regenerado y el ADR [020](../../architecture/adr/020-reloj-despierto.md)). Precisiones, sin cambiar el QUÉ:

| # | Qué | Por qué |
|---|---|---|
| A-01 | `IAppLifecycle.Woke` (nunca avisa) e `IDisplayMetrics.ImmersiveSafeArea` (= `SafeArea`) van con implementación por defecto en la interfaz (C# 8, `netstandard2.1`) | Así `Platform/Lifecycle/AppLifecycle.cs` y `Platform/Display/DisplayMetricsProvider.cs`, que son de WP-B2, compilan sin cambios hasta que WP-B2 los implemente |
| A-02 | `SafeInsets.cs` no cambió | `ImmersiveSafeArea` usa el mismo *struct*; no hizo falta nada nuevo |
| A-03 | MSP (las constantes de §3.1, con las de `HudGeometry`) está en `MotionSpecsTests.HudOfIterationFourB`, como `HudOfIterationFourA` | Un solo lugar para los números; `HudTimelinesTests` prueba las líneas de tiempo. `HTL.LiveDot` (el pulso entre 0,35 y 1) se suma a `.LiveDotStartsAtOne` |
| **DT4b-22** | **WP-A deja en rojo ocho pruebas de PlayMode de la 4a, que son de WP-B (§7.6), porque cambia a propósito lo que esperan.** Sin simulador: `HudControlsTests.DialogClosesWhenTheRoutineEndsInBackground`, `.FinishTapAfterTheEndDoesNothing`, `.RoutineEndsWithTheFinishDialogOpen`, `ImmersiveHudTests.LeavingTheImmersiveStopsItsContinuousMotions`, `WorkoutBackgroundUiTests.CompletedToastOnceOnReturn` y `WorkoutScreenTests.StartDuringThePreviousExitHasItsOwnImmersive`. Con simulador, además, `SimulatorOverlayTests.PanelFollowsTheWrench` y `.PanelKeepsSplitAndWattsInView` | (1) **DT4a-15**: con la rutina terminada sola el motor queda en `Finished` hasta `AcknowledgeCompletion()`, y el diálogo que lo llama es de WP-B2; mientras tanto el inmersivo de la 4a no sale solo. (2) **AC-WKT-03-05**: sale el aviso `workoutCompleted`, que dos de esas pruebas esperan. (3) **DN4b-23 / AC-BLE-16-03**: el techo del panel pasa a 8 dp debajo del disco de `HudLayout.BigCount` (TV 298,0 de alto máximo), y la prueba espera el de la 4a (8 debajo de Watts, 316). **Propuesta para WP-B0**, que tiene que terminar con las cuatro suites en verde y `capture-snapshots.ps1 -Set it4a` sin fallas (§7.0): un puente de una línea en `ImmersiveWorkoutPage` (al primer estado `Finished` con `EndedBy == Completed`, `Hud.AcknowledgeCompletionAsync()`), que WP-B2 reemplaza por el diálogo, y poner al día `FinishTapAfterTheEndDoesNothing`, `CompletedToastOnceOnReturn` (sin aviso al volver, AC-WKT-03-05) y `PanelKeepsSplitAndWattsInView` (el techo y el alto de DN4b-23). Con el puente, las otras cinco vuelven a pasar como en la 4a. Lo decide el coordinador |

**Corridas** (desde la raíz del repo; `<scratch>/it4b/wp-a` y `<scratch>/it4b/wp-a-unity`):

| Suite | Resultado |
|---|---|
| `tools/dotnet/run-tests.ps1` | compilación con 0 avisos y 0 errores; herramientas 182 / 182; Core 2274 / 2246 pasan / 0 fallan / 28 ignoradas; matriz: PASA 478, FALLA 0, IGNORADO 2, PENDIENTE UNITY 117, SIN TEST 0 |
| `tools/unity/run-unity-tests.ps1 -Platform EditMode` | 2313 / 2281 / 0 / 32, 0 inconclusas |
| `… -Platform EditMode -Simulator` | 2495 / 2463 / 0 / 32, 0 inconclusas; el *define* restaurado |
| `… -Platform PlayMode` | 270 / 264 / **6** / 0 (DT4b-22) |
| `… -Platform PlayMode -Simulator` | 334 / 326 / **8** / 0 (DT4b-22); el *define* restaurado |

Unity creó los `.meta` de los archivos nuevos (se versionan con su archivo). `git -C ../rowmate-legacy status --porcelain` vacío; `lib/core/strava/strava_secrets.dart` no se leyó. Sin *commits*.

---

## Cambios por implementación de WP-B

WP-B quedó implementado con sus cuatro entregas (B0 a B3) sobre WP-A y los `IMotion` de WP-D1, sin cambiar el QUÉ. Las precisiones y los desvíos, con su motivo:

| # | Qué | Por qué |
|---|---|---|
| B-01 | **Sonda S-B4b**: no quedó como un informe aparte; cada fila se resolvió en el código y la prueban sus PlayMode. `DrawText` con otro `FontAsset` **da** (los afijos de `TabularNumber`: la flecha Inter 700, la `W` y la `m` Inter 500, el ícono `schedule`; `HLT.CardsArrowAndTargetMatchTheDesignNotes` mide la línea de base de ▲ ± 1 dp). `Painter2D` y `Allocate` en el mismo `generateVisualContent` **dan** (la fila de píldoras con su halo, el halo de las tarjetas, el aro de la cuenta grande). El tinte del fondo **da** (el velo, `PVT`). El glifo de `PAUSA` en el `generateVisualContent` de su `Label` **da**. **Plan B** en dos filas: M3 y M5 no se hicieron (opcionales: `Próximo` en dos etiquetas y el espaciador de la 4a) y el sello va en **dos etiquetas** (el título Inter 700 y el detalle Inter 600): el texto enriquecido con dos pesos en un `Label` necesita una tabla de pesos que los `FontAsset` del repo no tienen. Con todo, el peor caso de la 4b queda en el tope (`HBT.AtMostSixtyVisibleElementsInTheFourBWorstCase`, los cuatro perfiles) | R4b-04, R4b-12; §3.11, §7.0 |
| B-02 | **El halo de la píldora** con `PillGlowTexture` en una grilla de 6 × 6 (no 4 × 4): cada lado se parte en el borde de la píldora y en su radio, así el perfil `(1 − d/E)²` empieza justo en el borde con E distinto por lado (10 / 8 / 4·s). Con el 9-slice simple el borde de la textura caía 0,6 dp adentro arriba y 1,7 dp adentro abajo y teñía la pista | DN4b-17 |
| B-03 | **El chip de fase**: su ancho fijo lo mide Unity (`MeasureTextSize`, solo con panel): en español da menos que el 101,8 de las notas (fontTools con K2), y por eso el título libre en TV queda entre 0,8 y 1 (no 0,818). Las pruebas (`FBT`) y las comprobaciones (CAP-13, -18) miden contra el ancho medido, como pide R4b-09. Una sola etiqueta: el texto nuevo está desde el cuadro de la fase (AC-WKT-16-03) y su opacidad sigue el cruce de `PhaseChipMotion` (baja en 0-80, sube en 80-200) | R4b-09; AC-WKT-16-03 |
| B-04 | **La superficie "oculta" de la llave** es `ShellLayout.HoldWrench(owner, bool)` (Presentation), no un `OverlaySurface.Hidden` de Core: la llave que cambia de carril espera, además de la transición de página, a que el inmersivo la suelte (1120) y a que termine la entrada o la salida de un diálogo (R10) | §5.7; sin tocar Core ni el simulador puro |
| B-05 | **El diálogo de completado** se abre con la regla pura (`CompletionDialogRule`) a los 300 ms del primer `Finished` + `Completed`, o en el acto (`DialogLayer.Open(…, instantly)`) si el estado llega con `Instant` o si el `Schedule` vence en el alcance. No hizo falta el puente de DT4b-22 (B0 y B2 se hicieron juntos): las ocho pruebas de la 4a que WP-A dejó en rojo pasan con el diálogo (`HCT`, `IHT.LeavingTheImmersiveStopsItsContinuousMotions`, `WBU.CortaEndsInBackgroundDialogAlreadyOpen` en lugar de `CompletedToastOnceOnReturn`, `WSC.StartDuringThePreviousExitHasItsOwnImmersive`, `SOT.PanelFollowsTheWrench`, `SOT.PanelKeepsSplitAndWattsInView` con el techo de DN4b-23: 298) | DT4b-22 |
| B-06 | **Los avisos del final** los retiene `WorkoutScreenView` solo si la sesión ya volvió a reposo (o su inmersivo sale) y el inmersivo todavía no empezó a mostrar Workout; los de una sesión en marcha (un guardado que falló con el HUD arriba) salen en el acto, como en la 4a | DN4b-24; `WPS.CreateFailsButTheWorkoutRuns` |
| B-07 | **La cascada** no mueve las piezas del centro (la cuenta grande y `PAUSA`): su opacidad es la del disco (`CenterDiscMotion`); al entrar casi nunca están (un primer paso de 3 s o menos) | §3.2; 04-design-notes §4.1 |
| B-08 | **El arnés**: el toque que empieza la sesión cae en un `k + 0,5` del reloj de prueba y la hora de pared se corre a los 13:00:00.000Z (`ScenarioScript.AlignSession`, solo en las tomas de it4b): las lecturas del remo (L(k) en el instante k del anfitrión) llegan en los `k − 0,5` de la sesión. La regla de toma de la 4b (`SnapshotTakeRule`) no espera: nombra lo que bloquea y deja `inFlight` en el *manifest*. Los videos dejan los valores por cuadro (`values`) y comparan con las líneas de tiempo puras la cascada (VID-01), la cuenta grande y la de la barra en el mismo cuadro (VID-04) y el conteo con `CountUp` (VID-05); los demás solo dejan sus valores. El tope del arnés pasa de 15 a 30 min (it4b simula ≈ 63 min de sesiones) | §5.2, §5.5 |
| B-09 | **Las tomas de it4a reemplazadas** (`WorkoutCatalog.MarkSuperseded`): CAP-12 a CAP-30, la DEMO y VID-01 a -03 no se vuelven a tomar y van al *manifest* con `"superseded": "it4b"`; CAP-01 a CAP-11 se siguen tomando | §5.6; DT4b-07 |
| B-10 | **Excepciones de dueño** que hicieron falta: `Presentation/Motion/OffTargetMotion.cs` (la flecha es el prefijo del número, M1), `Tests/Support/RoutineSeeds.cs` (`Budget`, el peor caso del presupuesto), `Tests/Support/ScriptedRower.cs` (`Next` con `set`: el arnés empieza el guion en L(1) al alinear la sesión, B-08), `Tests/EditMode/Project/ItFourAToolingTests.cs` (it4b es el conjunto por defecto) y `Presentation/Motion/MotionDriver.cs` (`CopyMotions`, para la regla de toma) | §7.6 |
| B-11 | **Platform**: `DisplayMetricsProvider.ImmersiveSafeArea` queda con la implementación por defecto de A-01 (en escritorio son los mismos márgenes); los márgenes estables de Android con las barras ocultas quedan NO VERIFICABLE sin el módulo (R4b-05). `AndroidSystemUi` compila solo bajo `UNITY_ANDROID && !UNITY_EDITOR` | §4.2, §4.3 |
| B-12 | **Pruebas**: las PlayMode nuevas cubren los AC de §6.2 con menos casos que el estimado de §6.5 (≈ 45 en lugar de ≈ 105): PRT, SBT, CCT, SVT, FBT, CDT, ITT, PVT, SBU y las de HBT (el peor caso de la 4b y `LongSeriesStaysSmooth`); `USC`, `SSL` y `LOS` en Unity EditMode | §6.5 |
| B-13 | **El ícono del sello** son dos `IconView` (`#hud-seal-icon`, el `check_circle` relleno, y `#hud-seal-icon-missed`, el `error` de contorno), uno a la vista por vez, en lugar de uno que cambia de glifo y de fuente: al pasar de cumplido a no alcanzado, `IconView.CheckGlyph` miraba la fuente todavía sin resolver y dibujaba el cuadrado genérico de ASSET_SLOTS §4.4 (se vio en CAP-04). El que no se ve no cuenta para el presupuesto (`display: none`) | §2.5, §3.6; CAP-03, CAP-04, CAP-26, CAP-30 |
| B-14 | **La barra libre** deja al menos 6 dp (`InfoRowFit.CompactGap`, el aire compacto de la barra de pasos) entre el título y el reloj: con el "···" de las notas en 0, el título achicado quedaba a ≈ 3 dp del ícono del reloj (CAP-13). Con 10 (el aire normal) el título, que Unity mide más ancho que fontTools, no entraba a 0,8 en TV y se recortaba; con 6, en TV en español la escala queda cerca del 0,818 de las notas, sin "…"; `FBT.ContentInOrderWithClockAndChip` mide el aire | DN4b-27; 04-design-notes §3.5 |
| B-15 | **`WSC.StartDuringThePreviousExitHasItsOwnImmersive` con reducir movimiento**: la salida del inmersivo dura 120 (lineal) y el velo de un diálogo que se cierra se traga los toques 150 (DT-21), así que después del diálogo de completado no hay salida que tocar. Esa vuelta de la prueba termina a mano con un cierre lento (la base vuelve 1 s después): el toque cae en la salida, con el diálogo ya ido. Sin reducir movimiento sigue con el diálogo de completado (la salida dura 640) | DT-21; QA-V4a-01 |
| B-16 | **`FrameTimeProbe`**: el máximo y la cuenta miran todos los cuadros de la ventana de 10 s (antes, solo los primeros 1024: a más de 102 fps el resto no contaba) y el p99 sale de hasta 4096 muestras. Las líneas del registro llevan la etiqueta una sola vez (`[PERF] cpu del hilo principal: …`, `[TIME] avisos de suspensión registrados (P-17)`; antes salían `[PERF] [PERF]` y `[TIME] [TIME]`) | §3.13; QA-G4bT-11 (b) |
| B-17 | **`SnapshotCli.SeedDatabase -seed F4A`** (además de `Cap27`): la base de `F-4A` (`Prueba 4a`, `Corta`, `Distancia 100`, `Sin meta`) para probar a mano el build del simulador sobre una carpeta de `ROWMATE_DATA_DIR` temporal, nunca la del usuario | §7.0 |

**Corridas** (desde la raíz del repo; artefactos en `<scratch>/it4b/wp-b`):

| Suite | Resultado |
|---|---|
| `tools/dotnet/run-tests.ps1` | compilación con 0 avisos y 0 errores; herramientas 182 / 182; Core 2280 / 2253 pasan / 0 fallan / 27 ignoradas |
| `… -UnityResults <los XML de las cuatro corridas de Unity>` | matriz: PASA 595, FALLA 0, IGNORADO 2, PENDIENTE UNITY 0, PENDIENTE WP-B 0, SIN TEST 0 |
| `tools/unity/run-unity-tests.ps1 -Platform EditMode` | 2329 / 2298 / 0 / 31, 0 inconclusas |
| `… -Platform EditMode -Simulator` | 2511 / 2480 / 0 / 31, 0 inconclusas; el *define* restaurado |
| `… -Platform PlayMode` | 346 / 346 / 0 / 0 |
| `… -Platform PlayMode -Simulator` | 410 / 410 / 0 / 0; el *define* restaurado |
| `tools/unity/capture-snapshots.ps1 -Set it4b`, A a `<scratch>` y B al repo, una detrás de otra | 51 tomas, 0 con problemas, 0 sondas de aspecto que fallan en las dos; los 53 archivos (51 tomas, `INDEX.md` y `manifest.json`) iguales byte a byte salvo `durationMs` |
| `… -Set it4a` a `<scratch>` y al repo | 49 tomas, 0 con problemas: CAP-01 a CAP-11 iguales byte a byte a las del repo; las del HUD, `"superseded": "it4b"` en el `manifest.json` y "reemplazada por" en el `INDEX.md` (sus archivos de la 4a quedan como registro) |
| `… -Set it3` y `-Set it2` a `<scratch>` | 47 y 41 tomas, 0 con problemas; iguales byte a byte a las del repo (49 y 44 archivos) |
| `tools/build/build.ps1 -Target Windows -Simulator -Backend Mono` | bien, en `Builds/Windows/simulator` |
| `tools/build/build.ps1 -Target Windows -Backend Mono` | bien, en `Builds/Windows/normal`; `BuildOutputInspector` sin rastros del simulador, de diagnósticos ni de tests |
| `… -Backend IL2CPP` y `-Target Android` | NO VERIFICABLE (código 3: faltan los módulos en este Editor) |

**Prueba a mano en el build del simulador** (W1280 maximizada, en inglés, con `ROWMATE_DATA_DIR` en una carpeta temporal sembrada con `-seed F4A` y borrada al final): el libre con la barra de arriba (`Free Workout`, el reloj de pared con su ícono y el chip `ACTIVE` → `PAUSED`), `PAUSA` en su disco con el velo, `Finish` → `Finish` y Workout en reposo sin aviso; `Corta` hasta el final con las píldoras, la última llena, el diálogo `Workout Completed!` (00:30, 455 m, 27) a los 300 ms, y su `Finish` vuelve a Workout en reposo sin aviso. El registro dice `[TIME] avisos de suspensión registrados (P-17)`, `[UI] barras del sistema: ocultas` / `visibles` y `[PERF] cpu del hilo principal: …`: la primera ventana del inmersivo tuvo un máximo de ≈ 50 ms (el cuadro que arma el HUD al entrar) y las siguientes, de 1 a 8 ms, con p99 ≤ 1,4 ms. **Para QA**: la distancia de la banda (la del monitor, `RowingData.DistanceMeters`, como en la 4a) no es la del diálogo (la de la sesión); en el simulador, que cuenta desde que se conecta, se ve la diferencia (501 m contra 455 m). La regla del HUD es de la 4a y no la cambió WP-B.

Unity creó los `.meta` de los archivos nuevos (se versionan con su archivo). `git -C ../rowmate-legacy status --porcelain` vacío; `lib/core/strava/strava_secrets.dart` no se leyó. Sin *commits*.

---

## Cambios por verificación (ronda 1)

Corrige los dos FAIL y los menores de [05-qa-verification](05-qa-verification.md) §7 y de las lentes ([05-qa-lens-paridad](05-qa-lens-paridad.md) §2, [05-qa-lens-motion](05-qa-lens-motion.md) §3), sin cambiar el QUÉ. Cada arreglo tiene una prueba que fallaba antes y pasa después (la columna "Prueba"; las de V1-03, V1-04, V1-07 y V1-10 corrieron en rojo contra el código anterior, en `<scratch>/it4b/fix-architect-r1b/red-pm`, y las de V1-01, V1-02, V1-08, V1-09, V1-11 y V1-12 las había corrido en rojo la primera parte de esta ronda). Las de V1-05 son la cobertura que faltaba y pasan con el código de la ronda.

| # | Issue | Qué cambió | Prueba |
|---|---|---|---|
| V1-01 | **QA-V4b-01** (AC-BLE-16-02; H-D2-02) | `SimulatorLayerView.Place`: entrar o salir de la superficie del entrenamiento es **siempre** un cambio de carril, también cuando el carril es el mismo (con el diálogo de completado la llave ya está en el izquierdo, R10). La llave sale en el cuadro 0 (90, `ease.exit`), espera la transición de la página, lo que retiene `ShellLayout.HoldWrench` y la salida de los diálogos, y entra en su lugar de Workout al terminar la salida, como en VID-02 | `SOT.WrenchLeavesInTheFirstFrameAlsoFromTheCompletionDialog`; VID-05: la llave se va en los cuadros 210-212 y vuelve en el 232, nunca sobre el negro |
| V1-02 | **QA-V4b-02 = QA-V4b-P01** (AC-WKT-16-03, -06) | El *tracking* por código va en la unidad de TextCore, como el USS: `TypographyUssWriter.LetterSpacingUnits(em)` = em × 100 (N = N/100 em por letra). `PhaseChipView` y `CenterCallouts` (`PAUSA`) ponen 8 = 0,08 em, el valor de 04-design-notes §3.5 (el legacy usa 0,8 px a 11 = 0,073 em; vale el de las notas). El chip mide lo de las notas: 101,3 base en español (CAP-13: 106,9 dp con s = 1,055; notas 101,8) y 73,5 en inglés (CAP-18; notas 74,1). **La barra libre, rebalanceada** (04-design-notes §3.5 y §16): el aire mínimo entre el título y el reloj (`InfoRowFit.FreeTitleClockGap` = `FreeTopBar.TitleClockGapDp`) baja de 6 (B-14, calibrado con el chip angosto) a **2**; con 6 y el chip a su ancho el título quedaba a 0,779 y se recortaba. En TV y P360 en español entra **entero a 0,805** (125,1 de 155,4 base), nunca por debajo de 0,8 ni con "…" (AC-WKT-16-06); en inglés y en horizontal, a su tamaño. `PAUSED` con su *tracking* mide 53,9 base y entra en la cuerda del aro (59,3) | `FBT.ContentInOrderWithClockAndChip` (`letterSpacing` 8, `FixedWidth` 101,8 ± 1, escala 0,805 ± 0,004, aire ≥ 2·s), `FBT.EnglishTitleFitsAtItsSize` (74,1 ± 1; antes solo `> 70`), `CCT.PausedTakesTheDiscOneAtATime` (`PAUSED` 53,9 ± 1,2 y ≤ 59,3, `PAUSA` 45,7 ± 1,2), `IRF.FreeTitleFitsInTvWithTheChipAtItsTracking`, `TypographyUssSyncTests.CodeTrackingUsesTheTextCoreUnit`; en el arnés, CAP-13 y CAP-18 comparan el ancho fijo con el de las notas (O-P03), no con el medido |
| V1-03 | **QA-V4b-03** (AC-WKT-14-16; H-D2-01) | `PillRowView.DrawStrip` no pinta la tira debajo del pulgar de la en curso (en columnas de píxel enteras, hacia adentro del pulgar) y, en la separación 1, la en curso angosta no pinta su lugar debajo del pulgar: su pista al 30 % queda sobre el vidrio, "la de siempre" (04-design-notes §3.2). Mientras el pulgar llega (`ThumbSwapping`, 0-150), lo de abajo se apaga con él (1 − su opacidad), sin saltos | `PRT.ThumbTrackSitsOnTheGlass` (`#61324B` antes, `#4D253F` después); la sonda de CAP-22 pasa (6/6: `#4E2540` contra `#4D253F`) |
| V1-04 | **QA-V4b-04** (H-D2-03) | Dos cambios. (a) `PillShapes.CapsuleOutline` y el relleno recortado **no repiten vértices**: las puntas de la cápsula de 8 con radio 4 son un solo punto y el contorno no cierra en su primer punto (una arista de largo 0); cada arco empieza en su punto, así los bordes de arriba y de abajo quedan rectos (antes iban al segundo punto del arco siguiente, 0,08 inclinados). Con eso la grieta se corrió de (724, 58) px a (678, 51) px, justo debajo del brillo: la teselación de `Painter2D` del polígono recortado igual deja a veces un píxel sin cubrir. (b) **Debajo del relleno opaco**, un rectángulo sin suavizado (`MeshGenerationContext.Allocate`, dos triángulos) que tapa la parte recta del relleno sin tocar sus bordes (un píxel adentro arriba, abajo y en el corte; los arcos quedan afuera). Opaco sobre opaco del mismo color: no cambia nada donde no hay grieta (las 150 sondas, el brillo incluido, dentro de su tolerancia). Con alfa < 1 (las transiciones del cambio de paso) no se pone | `PRG.OutlinesHaveNoRepeatedVertices` (4 casos, dotnet); `PRT.CurrentFillHasNoHoles` (recorre el relleno de la 3 en el estado de CAP-01 y en el de CAP-27 desde la fila de abajo del brillo: falla con `(724, 58) #4D243D` antes de (a) y con `(678, 51) #4D243D` sin (b)) |
| V1-05 | **QA-V4b-05** (§6.2, §6.4) | Las PlayMode que faltaban, permanentes: `HLT.RotationWithDialogSealOrBigCount` (las dos pruebas temporales de QA: el diálogo TV → TH → TV, la cuenta grande TV → TH y el sello TH → TV, sin repetir ni reiniciar nada); `HBT.LongSeriesStaysSmooth` ahora mide de los 26,2 a los 31,5 s (el halo, la cuenta grande 3-2-1, el cambio de paso y el pulgar que pasa; `Serie larga` no tiene objetivos, así que no hay sello: el sello con todo lo demás es el peor caso de `AtMostSixtyVisibleElementsInTheFourBWorstCase`; su comentario ya no promete 0 B); `HBT.NoAllocationsPerFrameWithTwoHundredPills` (200 píldoras con el halo y **la fila sola de 1998 pasos**, una `HudPillRow` armada a mano con el pulgar y el halo: dibujo ≤ 16 ms y 0 asignaciones); `IHT.KcalSlides` (montado: kcal desde su lugar viejo hasta 0 en 240 con su `translate` en cada cuadro, BPM que funde, al revés al irse, y BPM desde el primer estado sin NaN); `SOT.RibbonAboveTheCompletionDialog` (la capa del simulador después de la de diálogos y la cinta sin el velo encima: `#FF5722` en un punto sin letras); `CDT.CountUpDoesNotAllocatePerFrame` (V1-09). **Lo que queda cubierto de otra forma:** `WBU.MinimizedEndShowsTheDialogOnRestore` → en Windows minimizar no es segundo plano (`runInBackground`): lo cubre el equipo (05-qa-verification §5, la ventana minimizada) y `WBU.CortaEndsInBackgroundDialogAlreadyOpen` cubre el segundo plano de verdad; `WSM.HeartRateDropSlidesKcal` → VID-07 (valores por cuadro, sin NaN) e `IHT.KcalSlides`. **Medir asignaciones en Unity:** en Unity (Mono) `GC.GetAllocatedBytesForCurrentThread` devuelve siempre 0 (medido con un `byte[4096]` en el medio), así que las pruebas de "0 B por cuadro" de PlayMode no medían nada (tampoco la prueba temporal de QA que dio 0 B). Ahora cuentan las muestras `GC.Alloc` del Profiler con un `Recorder` (`Tests/UiSupport/GcAllocations`, como `AllocatingGCMemoryConstraint` del Unity Test Framework): `HBT.NoAllocationsPerFrameWhileItOnlyBeats`, `HBT.NoAllocationsPerFrameWithTwoHundredPills`, `HMT.NoAllocationsPerFrameAndStopLeavesNothingAnimating`, `CDT.CountUpDoesNotAllocatePerFrame` y `LocalOffsetSourceTests.ReadingDoesNotAllocate` (Unity EditMode). Miden el código del HUD de cada cuadro (`H.Step`: los `IMotion`, el estado aplicado), no el `generateVisualContent` del repintado del panel (su tiempo lo acota `LongSeriesStaysSmooth`). dotnet sigue midiendo bytes con `GC.GetAllocatedBytesForCurrentThread` | las seis de la izquierda |
| V1-06 | **QA-V4b-06** | **Queda abierto** (desvío): el primer cuadro del inmersivo arma el HUD entero (su árbol, su *layout* y su texto) en el cuadro del toque. Moverlo al negro o a la apertura de Workout cambia el orden de construcción de `ImmersiveWorkoutPage` y lo que ven las pruebas en el cuadro del toque (el HUD existe desde el toque, AC-WKT-13-01); no es barato ni sin riesgo para esta ronda. Propuesta para la It. 5: armar el HUD en un cuadro del negro (los 0-220 de la página negra no lo muestran) o tener uno listo al abrir Workout, y medir en el build con `FrameTimeProbe` que el fundido no pierda cuadros | — |
| V1-07 | **QA-V4b-07** (§5.5) | VID-02 deja sus valores por cuadro (el HUD, el fondo y la opacidad de la página, `t`, el reloj y el chip) y los compara con `ImmersiveTimeline.Exit` (± 0,01). La página que sale ya no es `h.Immersive` (`PageStack.Top` salta las que salen), así que se guarda al tocar `Terminar`: antes ni la comprobación de "nunca `00:00`" corría durante la salida. VID-07: `KcalSlideMotion` toma una distancia que no es finita como 0 y `HudView` no usa el alto de kcal sin *layout* (el primer estado con BPM): `kcalShift` arranca en 0 y el `translate` de kcal nunca es NaN | `HMT.KcalShiftIsNeverNaN`, `IHT.KcalSlides` (el último caso), `ITO.VideosLeaveTheirFrameValuesWithoutNaN` (el *manifest*: los 8 videos con valores y sin NaN; VID-02 con 31) |
| V1-08 | **QA-V4b-P02** (AC-WKT-14-13, Q-52) | `PillProgress.OnState`: con un segundo nuevo (o `Instant`) y por llenar, el relleno vale al menos transcurrido / D, el presente del motor (nunca adelanta, AC-WKT-14-10; la pausa no cambia: Q-58 no trae segundo nuevo). La última píldora queda llena aunque el último segundo llegue un cuadro antes | `PPG.LastTimedStepEndsFull` (D = 1 y 5 con 0,984 y 0,999 s, y el control con 1,016); `PPG.NeverGoesBackWithinAStepAndStillInPause` ya no manda segundos que el motor no daría |
| V1-09 | **QA-V4b-MOT-01** (§3.11, §7.4) | El conteo del diálogo escribe los tres totales en un `char[]` propio (`WorkoutFormats.WriteSessionClock`, `TimeFormats.WriteElapsed` y `WritePad2`, `Fmt.WriteInteger`: los mismos caracteres que las versiones con cadena) y `TabularNumber.SetValue(char[], int, double)` rehace las celdas solo si cambiaron, sin cadena nueva y sin rodillo | `CDT.CountUpDoesNotAllocatePerFrame` (0 asignaciones en 30 cuadros del conteo), `WorkoutFormatsTests` (los escritores en búfer dan lo mismo que las cadenas, sin asignar) |
| V1-10 | **QA-V4b-MOT-02** (AC-WKT-13-08; AC-WKT-01-09) | `ImmersiveWorkoutPage.BeginExit`: después del `Pop`, la raíz negra **absorbe** los toques sin actuar (`pickingMode = Position`; su árbol sigue inerte por `ExitLock` y la raíz no tiene manejadores de toque) hasta `RevealStarted` (400), donde los suelta: desde que empieza a verse Workout vale el toque de ROB-01 (el OK del PO). Con reducir movimiento Workout se ve desde el cuadro 0 y no cambia nada | `ITT.TouchesDuringTheBlackExitDoNotReachWorkout` (a los 200 ms el toque en `Entrenamiento libre` y en una rutina lo recibe el inmersivo que sale; a los 420, Workout responde); `WSC.StartDuringThePreviousExitHasItsOwnImmersive` toca a los 450 ms (y comprueba que a los 200 no llega) |
| V1-11 | **QA-V4b-MOT-03** (AC-APP-05-08) | `LiveDotMotion` lee reducir movimiento en cada cuadro, como el corazón y el halo: si se prende, queda en 1 y sale del `MotionDriver`; si se apaga, el `SetActive(true)` del estado siguiente lo vuelve a hacer latir desde 1 | `HMT.LiveDotStartsAtOneAndPulsesWhileActive` (el caso nuevo) |
| V1-12 | **QA-V4b-MOT-04** (Q-91; §2.9.3) | `SuspendAccounting.Awake(…, fromBackground)`: sin medida ni aviso, la vuelta del segundo plano cuenta como siempre pero se anota como `[TIME] vuelta del segundo plano de N s sin aviso de suspensión: cuenta (Q-91)`; el "cuadro de N s … (AC-WKT-02-04)" queda para los cuadros tarde de verdad | `SUS.BackgroundReturnIsNotLoggedAsALateFrame` |
| V1-13 | **QA-V4b-MOT-05** (§3.12) | `ITT.NothingAnimatesAfterTheExit` con cinco salidas (§3.12, al día) y 0 avisos de DPI en el registro; la vuelta del segundo plano queda en `IHT.LeavingTheImmersiveStopsItsContinuousMotions` | la misma |

**Capturas** (dos corridas seguidas: A a `<scratch>/it4b/fix-architect-r1b/capA`, B al repo): `listo: 51 tomas, 0 con problemas, 0 sondas de aspecto que fallan` en las dos; los 53 archivos iguales byte a byte salvo `durationMs` del *manifest*. 150 / 150 sondas dentro de su tolerancia (CAP-22 6/6). Cambian, contra la ronda 1, todas las tomas con píldoras (el contorno con los bordes rectos y, en el relleno opaco, el rectángulo de abajo) y las de la barra libre y `PAUSA` (el *tracking*, el chip y el título a 0,805); quedan iguales CAP-28, DEMO-01, DEMO-04 y DEMO-12 (sin HUD). Se miraron CAP-13 a CAP-18, CAP-22 (el pulgar sobre el vidrio), CAP-27 (sin el píxel suelto; `PAUSA` en el disco con su *tracking*), CAP-14, DEMO-02 y DEMO-03, y cuadros de VID-02 (la salida y la llave), VID-05 (la llave se va en los cuadros 210-212 y vuelve en el 232) y VID-07 (BPM funde en 30-34, kcal baja 71,4 dp en 33-40 y vuelve cerca de los 5 s; sin NaN).

**Corridas** (desde la raíz del repo; artefactos en `<scratch>/it4b/fix-architect-r1b`):

| Suite | Resultado |
|---|---|
| `tools/dotnet/run-tests.ps1 -Matrix -UnityResults …` | compilación con 0 avisos y 0 errores; herramientas 182 / 182; Core 2319: 2292 pasan, 0 fallan, 27 ignoradas; matriz con los cuatro XML de Unity: PASA 595, FALLA 0, IGNORADO 2, PENDIENTE UNITY 0, SIN TEST 0 |
| `tools/unity/run-unity-tests.ps1 -Platform EditMode` | 2368: 2337 pasan, 0 fallan, 31 ignoradas, 0 inconclusas |
| `… -Platform EditMode -Simulator` | 2550: 2519 pasan, 0 fallan, 31 ignoradas, 0 inconclusas; el *define* restaurado |
| `… -Platform PlayMode` | 354: 354 pasan, 0 fallan |
| `… -Platform PlayMode -Simulator` | 420: 420 pasan, 0 fallan; el *define* restaurado |
| `tools/unity/capture-snapshots.ps1 -Set it4b` (A y B) | 51 tomas, 0 con problemas, 0 sondas de aspecto que fallan; A = B salvo `durationMs` |
| `tools/build/build.ps1 -Target Windows -Simulator -Backend Mono` | `Windows simulator Mono2x Development`, `Build Finished, Result: Success`, en `Builds/Windows/simulator` (no había ningún `RowMate.exe` abierto) |

La corrida anterior de esta ronda se había cortado con el *define* del simulador puesto en `ProjectSettings/ProjectSettings.asset` y la marca en `Library/RowMate/`: se restauró con `run-unity-tests.ps1 -RestoreOnly` antes de empezar. `git -C ../rowmate-legacy status --porcelain` vacío; no se leyó el módulo de credenciales de Strava. Sin *commits*.
