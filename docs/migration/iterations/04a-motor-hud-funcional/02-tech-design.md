# It. 4a — Entrenar: motor de sesión, guardado incremental, reposo y HUD funcional: diseño técnico

| Campo | Valor |
|---|---|
| Autor | unity-architect |
| Fecha | 2026-10-28 |
| Iteración | 4a · `04a-motor-hud-funcional` ([ROADMAP §It. 4a](../../ROADMAP.md)) |
| Entrada | [01-po-spec](01-po-spec.md) corregida por el gate (162 AC: 147 nuevos + 15 de iteraciones anteriores; 11 LT; 30 CAP, 14 DEMO, 3 VID; DV-21 a DV-39), [04-design-notes](04-design-notes.md) (respuestas a DN4a-01 a DN4a-12 y pedidos DN4a-13 a DN4a-22: Anexo C), [03-qa-harmony-paridad](03-qa-harmony-paridad.md) y [03-qa-harmony-tecnica](03-qa-harmony-tecnica.md) ("Cambios por gate QA", al final), [00-deuda-it3](00-deuda-it3.md) (la deuda de la It. 3, cerrada), [00-deuda-diseno](00-deuda-diseno.md) (D-ART-10 y los pedidos DD4a-01 a DD4a-07), [02-tech-design de la It. 3](../03-rutinas/02-tech-design.md) (base v8, `SerialExecutor`, `PageStack`, `DialogLayer`, `ToastLayer`, `ExitLock`, `BottomObstacles`, arnés), [ARCHITECTURE](../../architecture/ARCHITECTURE.md) §2, §5, §6, §6.1, §7.2-§7.8, §9, §13, §15, §20, [ADR 005](../../architecture/adr/005-hilos-y-asincronia.md), [ADR 008](../../architecture/adr/008-persistencia.md), [ADR 017](../../architecture/adr/017-segundo-plano.md), [ART_BIBLE](../../design/ART_BIBLE.md) §2.2-§2.4, §3.2-§3.7, §3.11, §4, §5.2-§5.3, §10, [ANIMATION_BIBLE](../../design/ANIMATION_BIBLE.md) §2.2-§2.14 |
| Legacy leído (solo lectura, `3ae0f3a`) | [rowmate-legacy](https://github.com/figuibej/rowmate-legacy): `lib/features/workout/workout_provider.dart` (entero, línea por línea), `step_target_tracker.dart` (entero), `series_tracker.dart` (entero), `workout_screen.dart:1-204`, `immersive_workout_screen.dart` (entero), `series_panels.dart:1-30`, `:90-105`, `:330-345`, `lib/core/database/database_service.dart:1-40`, `:100-140`, `:212-290`, `lib/core/models/workout_session.dart:36-70`, `:136-183`, `rowing_data.dart:20-30`, `routine.dart:98-106`, `lib/core/bluetooth/heart_rate_service.dart:8-12`, `lib/core/dev/dev_config.dart`, `lib/main.dart:30-165`, `lib/features/device/device_provider.dart:85-100`, `test/workout_provider_hr_test.dart`, `test/step_target_tracker_test.dart`, `test/series_panels_test.dart:25-45`, `docs/image-02.png`, `docs/image-03.png`. Pantalla encendida: `<pub-cache>/wakelock_plus-1.4.0/lib/src/wakelock_plus_windows_plugin.dart:17-32` y `android/src/main/kotlin/dev/fluttercommunity/plus/wakelock/Wakelock.kt:15-28`. No se leyó `lib/core/strava/strava_secrets.dart`. `git -C ../rowmate-legacy status --porcelain` vacío al empezar y al terminar; además, el listado con `--ignored` y una instantánea de metadatos dieron lo mismo antes y después de la sonda S4-a |
| Verificación previa | **Sonda S4-a** (Anexo A): el `WorkoutProvider` **del legacy** corre bajo `flutter test` en un proyecto descartable de `<scratch>/it4a/arch-s4/legacy-engine/`, con una dependencia por ruta al legacy, `pub get --offline` y una base que solo anota. Con el remo de prueba R-4 de la spec dio los puntos y los totales de AC-WKT-06-03, -04, -06, -09 y de AC-WKT-03-03 (columna "Legacy") exactamente como los escribió el PO. Medición del HUD (Anexo B): las zonas del legacy × `hudScale` con los tamaños de D-ART-02, en los cinco perfiles |
| Estado | Diseño, sin código. **Corregido por el gate de armonía de QA** (las dos lentes; "Cambios por gate QA", al final) y **alineado con las 04-design-notes** (Anexo C: se aceptan DN4a-13 a DN4a-22, DN4a-18 con la variante de presupuesto que dejó el Designer) y con la spec corregida (§16 de 01-po-spec). Lo que había que cerrar antes de cada paquete está cerrado acá (§9.0). Orden: **WP-D0** y **WP-A1** → **WP-B0** ‖ **WP-A2** → **WP-B1** a **B3** → **WP-D** (§9) |

Convenciones: rutas relativas a `Assets/RowMate/` salvo que empiecen con `tools/`, `docs/` o `ProjectSettings/`. Marcas: **N** nuevo, **C** cambia, **G** generado. Dueño entre corchetes: **[A]** WP-A, **[B]** WP-B, **[D0]** WP-D0, **[D]** WP-D (§9). `dp` = px de USS (ARCHITECTURE §7.3). `s` = `hudScale` (ART_BIBLE §5.3). Perfiles: **TV** 411 × 914, **TH** 914 × 411, **L640** 640 × 360, **W1280** 1280 × 720 y, solo para la geometría del HUD, **P360** 360 × 800. Identificadores en inglés; comentarios y textos de diseño en español. Las citas `lib/...:línea` y `test/...:línea` son del legacy.

---

## 0. Resumen

1. **Un motor puro, en Core.** `Core/Workout/WorkoutEngine` porta `WorkoutProvider` (`workout_provider.dart:44-328`) sin Flutter ni Unity: las cuatro fases, el reloj activo, el avance de pasos de la rutina expandida, el estado del paso (restante, progreso, "por terminar" de 1 a 5 s y "a continuación" de 0 a 15 s), el cumplimiento de objetivos (`StepTargetTracker`, puerto de `step_target_tracker.dart`), la fusión del pulso, el muestreo cada 5 s activos y el cierre con el punto final y el P99 (`SessionStats`, de la It. 1). Recibe las lecturas en **todas** las fases y no mira la conexión del remo. Lo usan dos *stores* de App: `WorkoutStore` (la vista de reposo) y `WorkoutHudStore` (el HUD, con los textos ya formateados y el semáforo). Todo se prueba en dotnet con el reloj de prueba.
2. **Reloj por tiempo monótono (ADR 017, decisión 4).** `ActiveClock` cuenta segundos enteros desde el inicio del tramo activo, en *ticks* de 100 ns: un cuadro tarde procesa todos los segundos vencidos, en orden, y no se pierde ni se duplica ninguno (AC-WKT-02-04). Pausar descarta la fracción y reanudar abre un tramo nuevo (Q-58). El reloj arranca cuando vuelve el alta de la sesión y **nunca después de 100 ms del toque** (AC-WKT-01-02): con una base lenta, el HUD no espera.
3. **Segundo plano como si nada (Q-91).** El programador de la app pasa a Core: `FrameScheduler` es el mismo código en Unity (lo avanza `UnityScheduler` con el `Stopwatch`) y en dotnet (lo avanzan los tests). En primer plano es la conducta de la It. 2 (un disparo por cuadro). Al volver del segundo plano (`OnApplicationPause(false)` en Android e iOS) hace el **alcance**: avanza en tiempo virtual y dispara en orden todos los timers vencidos (el remo simulado y el motor), con `UtcNow` y `MonotonicSeconds` virtuales e `IsCatchingUp`. Así los puntos, los pasos y la hora de fin dan lo mismo que en primer plano (AC-WKT-02-05, -06). En Windows el *loop* sigue (`runInBackground`, ya activado). Los **topes de espera** (el del cierre) no cuentan el tiempo en segundo plano (`ForegroundTimeout`, §4.1; QA-G4aT-03) y, mientras la app está en segundo plano, el motor no publica fotos: al volver sale **una**, marcada `Instant`, que la vista aplica sin animar (§2.10; QA-G4aT-07). Lo que se avisa sin que el usuario lo vea (segundo plano, ventana minimizada o sin foco) se muestra una vez al volver, solo el último (§2.11).
4. **Guardado incremental, durable y en orden (Q-12).** `SessionRecorder` (Core) da de alta la sesión al iniciar y escribe cada punto en el acto, en **lotes** cuando una escritura todavía está en vuelo; el punto final y el cierre van **en una sola transacción**, después del último lote. Todo pasa por el `SerialExecutor` FIFO de la It. 3 con `synchronous = FULL`. Una sesión cortada queda con `finished_at` vacío, los totales en 0 y sus puntos: es el estado que la It. 9 muestra y la It. 13 repara. Un error se avisa una vez y el entrenamiento sigue (Q-13); los puntos de un lote que falló se reintentan **dentro de la transacción del cierre** y, si tampoco entran, la sesión queda cortada: una sesión cerrada siempre tiene todos sus puntos (AC-WKT-06-12; QA-G4aT-13). Al pasar a segundo plano se manda todo lo pendiente, también lo tomado con el alta en vuelo (encolado detrás del alta, en el hilo de la base), y se espera a lo sumo 1 s (AC-WKT-06-15; QA-G4aT-06). Esquema v8 sin cambios.
5. **Identidad con el legacy verificada.** La sonda S4-a muestra que el motor del legacy se puede correr fuera del legacy. WP-A la convierte en `tools/oracle/engine/` y genera `Tests/Golden/oracle/workout_engine.json`: las sesiones terminadas normalmente se comparan con sus vectores, punto por punto y total por total (AC-WKT-06-07, QA-P-23).
6. **R3-08 sin expandir.** `WorkoutStartRule.CanStart` = `StepLimitCheck.TemplateIsWithinLimits` (la validación 3 del editor) y `RoutineStructure.Segments(t, 0).Total > 0`, las dos en forma cerrada: con ×2 000 000 000 decide en microsegundos y no reserva memoria. Lo miran el *store* al tocar la tarjeta (aviso, sin diálogo) y el motor al iniciar. En el motor la progresión **satura** en los límites de `int` en lugar de lanzar: un split guardado ≤ 0 (que la regla no controla) ya no puede desbordar el inicio (QA-G4aT-11, DT4a-16).
7. **UI.** Workout en reposo reemplaza a la pantalla provisoria: aviso sin remo, `Entrenamiento libre`, la lista con la tarjeta de Rutinas y su *chevron*, cargando, error y vacío, todo en un solo desplazamiento (DV-22). El diálogo de inicio y el de terminar son `ModalDialog<bool>` de la It. 3, los dos con el aspecto opaco de la app (AC-WKT-20-03). El inmersivo es una página de `PageStack` que se abre en el acto y se cierra sola al volver a reposo; antes, cierra sin efecto su diálogo de terminar si la rutina terminó con él abierto (AC-WKT-20-07). La escala del HUD se aplica **por zona** con `scale` y el origen en su ancla: cada zona se arma a tamaño base (los dp del legacy y de D-ART-02) y el código solo pone el ancla, la escala y los topes de ancho. Tarjetas `glass.fallback`, semáforo con flecha y `obj.`, fila de información de pasos, banda, controles y fondo provisorio. La capa del simulador sigue arriba de todo.
8. **La llave del simulador (D-ART-10).** Regla pura `WrenchPlacement` (DD4a-01) con los carriles, alturas, obstáculos y techo del Designer; las pantallas declaran su contexto en `ShellLayout` (`OverlaySurface`); el panel se abre desde la llave y la sigue si cambia de lugar (DD4a-03), los menús la esquivan (DD4a-02) y el arnés audita cada toma (DD4a-04). D-ART-10 ya está en la spec (AC-BLE-16-02, -03, AC-RUT-10-04, -09; DV-39), con el ajuste del inmersivo de 04-design-notes §3.9: ahí el lugar de la llave lo calcula `HudGeometry` y la página lo declara (DN4a-14), y el panel deja a la vista Split y Watts.
9. **HUD horizontal, resuelto (DN4a-01, DN4a-02, DN4a-13).** La medición del Anexo B confirmó lo de 00-deuda-diseno §4.1 (la columna derecha no entra) y que la banda chocaba con los controles. Las 04-design-notes lo resuelven con la **fila de abajo**: en horizontal, kcal y BPM van en fila a la altura de los controles (kcal en el rincón) y la banda es compacta; arriba quedan SPM a la izquierda y Split y Watts a la derecha. El centro de la zona del bote es el de la cámara (0,504 del alto en vertical, 0,652 en horizontal), y en L640 hay lugar para la llave (sobre `Pausa`). La geometría sigue siendo una función pura (`HudGeometry`, ahora con los anchos naturales que miden los `FitBox`), probada en dotnet contra los rectángulos y las escalas de 04-design-notes §3.7 y §3.8.
10. **Paquetes.** WP-D0 (Designer: RowMate Numeric SC, la receta de kerning, pares de contraste) y WP-A1 (la regla de la llave y el programador) → WP-B0 (fuentes, llave y regeneración de las It. 2 y 3) en paralelo con WP-A2 (el resto del motor y del guardado; WP-A suma ≈ 95 archivos, dotnet, sin Unity) → WP-B1 a B3 (≈ 70 archivos, Unity) → WP-D (pulido). Archivos disjuntos y contrato USS en §9.5.

| Pregunta de la tarea | Dónde |
|---|---|
| 1) Árbol de archivos y asmdefs | §1 |
| 2) Motor de sesión: fases, reloj, pasos, estado del paso, objetivos, pulso, muestreo, P99 (firmas y fuente legacy) | §2 |
| 3) Guardado incremental durable, SQL exacto sobre v8, qué queda si la app se cierra | §3 |
| 4) Segundo plano (Q-91) y pantalla encendida por plataforma | §4 |
| 5) R3-08 | §5 |
| 6) UI: reposo, diálogos, inmersivo, HUD vertical y horizontal, cinta y llave | §6 |
| 7) Capturas del arnés | §7 |
| 8) Tests, casos legacy y matriz AC → test / captura | §8 |
| 9) Paquetes de trabajo y contrato USS | §9 |
| 10) Riesgos, desvíos y pedidos | §10 |
| Evidencia (sonda del motor legacy y medición del HUD) | Anexos A y B |
| Qué se acepta de las 04-design-notes (DN4a-01 a DN4a-22) | Anexo C |
| Cambios por el gate de armonía de QA (las dos lentes) | "Cambios por gate QA", al final |

---

## 1. Estructura

### 1.1 Árbol de archivos

#### Core (`RowMate.Core`, puro)

| Archivo | Marca | Contenido |
|---|---|---|
| `Core/Workout/WorkoutPhase.cs` | N [A] | `enum WorkoutPhase { Idle, Active, Paused, Finished }` (`workout_provider.dart:13`) |
| `Core/Workout/StepProgress.cs` | N [A] | estado del paso en curso (§2.5; `workout_provider.dart:16-42`, `:98-112`; `immersive_workout_screen.dart:342-346`) |
| `Core/Workout/StepResult.cs`, `StepTargetTracker.cs` | N [A] | puerto de `step_target_tracker.dart:1-71` (§2.6) |
| `Core/Workout/SanitizedDistance.cs` | N [A] | el saneamiento de los parciales (Q-55): `series_tracker.dart:43-46`, `:86-108`, `:118-127` (§2.4). Lo reusa el `SeriesTracker` de la It. 5 |
| `Core/Workout/HeartRateFusion.cs` | N [A] | `workout_provider.dart:68-70`, `:220-237` (§2.7) |
| `Core/Workout/ActiveClock.cs` | N [A] | reloj activo por tiempo monótono en *ticks* de 100 ns (§2.2) |
| `Core/Workout/TargetLight.cs` | N [A] | regla del semáforo: `TargetState`, `TargetLights` (`immersive_workout_screen.dart:579-602`, §2.6) |
| `Core/Workout/WorkoutFormats.cs` | N [A] | los 15 formatos de F-WKT-11 (§2.12) |
| `Core/Workout/WorkoutStartRule.cs` | N [A] | R3-08 (§5) |
| `Core/Workout/TelemetryPoint.cs` | N [A] | arma un `DataPoint` desde la lectura y el paso (`workout_provider.dart:160-172`, `:259-273`) |
| `Core/Workout/SessionRecorder.cs` | N [A] | alta, lotes de puntos, cierre, errores y espera al pasar a segundo plano (§3.2) |
| `Core/Workout/WorkoutEngine.cs`, `WorkoutSnapshot.cs`, `WorkoutNotice.cs`, `WorkoutEndKind.cs`, `StartOutcome.cs` | N [A] | el motor (§2.10) |
| `Core/Workout/WorkoutTimings.cs` | N [A] | `SampleEverySeconds` 5, `ClockStartGrace` 100 ms, `FinishSaveTimeout` 15 s, `BackgroundFlushTimeout` 1 s |
| `Core/Ports/ISessionRepository.cs`, `SessionNotFoundException.cs` | N [A] | puerto de sesiones y puntos (§3.1) |
| `Core/Ports/IAppLifecycle.cs`, `Core/Common/ForegroundLifecycle.cs` | N [A] | ciclo de vida (`InBackground` y `HasFocus`, observables; `IsUserFacing`) y su objeto nulo, siempre adelante y con foco (§4.2) |
| `Core/Ports/IWakeLock.cs`, `Core/Common/ScreenKeepAwake.cs` | N [A] | pantalla encendida: el puerto y la regla (pedir al arrancar, volver a pedir al volver del segundo plano y al recuperar el foco, liberar al cerrar, registrar un rechazo sin avisar; §4.4). Escucha el ciclo de vida: la regla entera se prueba en dotnet (AC-APP-14-02; QA-G4aT-09) |
| `Core/Common/FrameScheduler.cs` | N [A] | el `IScheduler` de la app, puro: `Tick` por cuadro, `EnterBackground` y `ResumeFromBackground` con alcance (§4.1) |
| `Core/Common/ForegroundTimeout.cs` | N [A] | tope de espera que **no cuenta el tiempo en segundo plano** (§4.1; spec §12 punto 2; QA-G4aT-03): lo usa el cierre de la sesión |
| `Core/Common/RealtimeTimerQueue.cs` | C [A] | + `CatchUp(from, to, setNow)`: dispara en orden de vencimiento todo lo vencido, los periódicos una vez por período y lo que se agenda durante el alcance si vence antes del destino, con tope de llamadas (§4.1) |
| `Core/Models/SessionListing.cs` | N [A] | fila de la herramienta de la demo: id, inicio, fin, rutina, origen y cantidad de puntos (§3.6) |
| `Core/Models/RoutineExpansion.cs` | C [A] | + `Flatten(template, ProgressionOverflow.Saturate)`: la expansión del motor satura en los límites de `int` en lugar de lanzar (§2.4, §5; QA-G4aT-11). `Flatten(template)` sigue con `checked` (It. 1) |
| `Core/Models/MetricPalette.cs` | C [A] | + `HudFor(MetricKind)`: las variantes `.hud` (AC-APP-06-01) |
| `Core/Routines/TrendText.cs`, `StepSubtitle.cs` | N / C [A] | `TrendText.Split(text)` separa el `📈` (DV-10, DV-35); `StepSubtitle` lo usa |
| `Core/Design/HudGeometry.cs` | N [A] | `hudScale`, anclas de las zonas × `s`, la fila de abajo en horizontal, los topes T1 a T5 con los anchos naturales de cada `FitBox`, las escalas esperadas, la zona del bote con el centro de la cámara, el horizonte del fondo y el lugar de la llave en el inmersivo (§6.4; DN4a-13, DN4a-14) |
| `Core/Design/OverlaySurface.cs` | N [A] | lo que declara la pantalla de arriba para la llave: contexto, alto de la barra, obstáculos y, en el entrenamiento, el lugar de la llave y el techo del panel (DD4a-01, DN4a-14; §6.10) |
| `Core/Design/MotionSpecs.cs` | C [A] | las constantes del HUD de 04-design-notes §5.13 (§6.11; DN4a-17) |
| `Core/Design/AtlasCharacters.cs` | C [A] | + `▲` `▼` (las dibuja el código, no salen del catálogo; QA-V3-06) |
| `Core/Design/FontAssetPaths.cs` | C [B] | el rol numérico pasa a las tres caras de RowMate Numeric SC (DD4a-07) y los `FontAsset` de Barlow salen de `All` (DN4a-22; los `.ttf` quedan para la receta), en B0 (§6.14) |
| `Core/Design/Tokens.cs`, `Presentation/Styles/Tokens.uss` (G) | C [D0] | `backdrop.sky` y `backdrop.water`: 133 → 135 (DN4a-18; excepción de dueño en §9.0) |
| `Core/Localization/L10nKeys.cs`, `Localization/strings.es.json`, `strings.en.json` | C [A] | las 12 claves de spec §11.2: 190 → **202** |

#### App (`RowMate.App`, puro)

| Archivo | Marca | Contenido |
|---|---|---|
| `App/Workout/WorkoutRestState.cs`, `RoutineTapOutcome.cs`, `WorkoutToast.cs`, `WorkoutStore.cs` | N [A] | la vista de reposo: remo conectado, colección compartida, iniciar y avisos (§2.11) |
| `App/Workout/WorkoutHudState.cs` (`HudMetric`, `TargetArrow`, `HudStepRow`, `HudTargetLabel`), `WorkoutHudStore.cs` | N [A] | el HUD con los textos ya formateados y el semáforo (§2.11) |
| `App/Composition/AppServices.cs`, `AppComposer.cs`, `AppContext.cs` | C [A] | + `ISessionRepository Sessions` (posicional), `IAppLifecycle? Lifecycle` (`init`); `AppContext.Workout`, `.WorkoutRest`, `CreateHud()` (§2.13) |

#### Infrastructure (`RowMate.Infrastructure`, sin API de Unity)

| Archivo | Marca | Contenido |
|---|---|---|
| `Infrastructure/Sessions/WorkoutSessionRow.cs`, `DataPointRow.cs`, `SessionRowMapper.cs`, `SqliteSessionRepository.cs` | N [A] | §3.3 |
| `Infrastructure/Database/LocalStore.cs` | N [A] | dueño de la base: arma los dos repositorios sobre la misma `RowMateDatabase` y la dispone (§3.5) |
| `Infrastructure/Database/RowMateDatabase.cs` | C [A] | + `WaitForPendingWork(TimeSpan)` (espera acotada a que el ejecutor quede vacío) |
| `Infrastructure/Threading/IDbExecutor.cs`, `SerialExecutor.cs` | C [A] | + `bool WaitIdle(TimeSpan timeout)` |
| `Infrastructure/Routines/SqliteRoutineRepository.cs` | C [A] | + `ownsDatabase` (por defecto `true`, la conducta de la It. 3); `LocalStore` lo arma con `false` |

#### Simulator (`RowMate.Simulator`, puro, con el define)

| Archivo | Marca | Contenido |
|---|---|---|
| `Simulator/Panel/WrenchPlacement.cs` | N [A] | la regla de D-ART-10 (R1 a R5, R9) sobre `OverlaySurface`; en el entrenamiento usa tal cual el lugar que declara el HUD (§6.10; DN4a-14) |
| `Simulator/Panel/SimulatorPanelLayout.cs` | C [A] | sale `FabBottom` con obstáculos (DV-13, reemplazada por DV-39); quedan el ancho y `MaxHeight`, que pasa a medirse desde el borde de abajo del panel, con el techo de Watts en el entrenamiento vertical (AC-BLE-16-03) |

#### Platform (`RowMate.Platform`, Unity)

| Archivo | Marca | Contenido |
|---|---|---|
| `Platform/Time/UnityScheduler.cs` | C [B] | envuelve `FrameScheduler`; `EnterBackground()` y `ResumeFromBackground()` (§4.1) |
| `Platform/Lifecycle/AppLifecycle.cs` | N [B] | `IAppLifecycle` que mueve `AppRoot.OnApplicationPause` |
| `Platform/Power/WindowsWakeLock.cs`, `ScreenSleepWakeLock.cs`, `NoWakeLock.cs`, `WakeLockFactory.cs` | N [B] | §4.4 |
| `Platform/PlatformServices.cs`, `PlatformFactory.cs` | C [B] | + `WakeLock` |

#### Presentation (`RowMate.Presentation`, Unity)

| Archivo | Marca | Contenido |
|---|---|---|
| `Presentation/Workout/WorkoutScreenView.cs`, `WorkoutRoutineListView.cs` | N [B] | §6.1 |
| `Presentation/Workout/StartWorkoutDialog.cs`, `FinishWorkoutDialog.cs` | N [B] | §6.2, §6.8 |
| `Presentation/Workout/ImmersiveWorkoutPage.cs` | N [B] | la página: capas, "Atrás", cierre en reposo, superficie para la llave (§6.3) |
| `Presentation/Workout/HudView.cs`, `HudZones.cs` | N [B] | dibuja un `WorkoutHudState` y aplica `HudGeometry` (posiciones y escala por zona, topes; §6.4) |
| `Presentation/Workout/HudMetricCard.cs`, `HudBand.cs`, `StepInfoBar.cs`, `FreeTopBar.cs`, `HudControls.cs`, `HudBackdrop.cs` | N [B] | §6.3, §6.5-§6.8 |
| `Presentation/Controls/GlowTexture.cs`, `BackdropTexture.cs` | N [B] | anillo 9-slice generado al arrancar, transparente por dentro (ART_BIBLE §3.2, respaldo); la textura de 1 × 100 del fondo provisorio, cielo y agua, con el corte en el horizonte (DN4a-18 con la variante de presupuesto de 04-design-notes §7) |
| `Presentation/Controls/TabularNumber.cs` | C [B] | + `SetValue(text, value, animate)`: sin rodillo después de un alcance (AC-WKT-02-08) |
| `Presentation/Controls/FitBox.cs` | C [B] | + `NaturalWidth` (ancho del contenido sin escala, para `HudGeometry`) y `FitScaleGroup` (dos `FitBox` con una sola escala: la banda, DN4a-01) |
| `Presentation/Motion/ImmersiveTransition.cs`, `StepPulse.cs`, `OffTargetMotion.cs` | N [B] | §6.11 |
| `Presentation/Routines/RoutineCardView.cs` | C [B] | + `RoutineCardTrailing.Chevron` (Workout): la caja `.rm-routine-card__chevron` de 48 en la columna de 28 dp del `⋮` (DN4a-19), además de `Menu` (Rutinas) |
| `Presentation/Routines/RoutineEditorPage.cs`, `RoutinesScreenView.cs` | C [B] | declaran su superficie para la llave (`OverlaySurface`, D-ART-10); el `+` deja de ser obstáculo de la llave (sigue siéndolo del aviso y del relleno) |
| `Presentation/Shell/AppShellView.cs` | C [B] | `WorkoutScreenView` en lugar de la provisoria |
| `Presentation/Shell/PageStack.cs`, `Motion/PageTransition.cs` | C [B] | `Push(page, PageTransitionKind)`: `Slide` (editor) o `Fade` (inmersivo) |
| `Presentation/Shell/ShellLayout.cs` | C [B] | `SetSurface(owner, OverlaySurface?)` y el aviso de cambio (§6.10) |
| `Presentation/Shell/ExitLock.cs` | C [B] | DT4a-01: devuelve el foco solo si se navegaba con teclado |
| `Presentation/Controls/PopupMenu.cs` | C [B] | esquiva la llave (DD4a-02) |
| `Presentation/Styles/Base.uss` | C [B] | estructura y contrato de §9.5 |
| `Presentation/Styles/Skin.uss`, `Motion.uss` | C [D] | aspecto |

#### Simulator.Presentation, Bootstrap, Editor, arnés, soporte y herramientas

| Archivo | Marca | Contenido |
|---|---|---|
| `SimulatorPresentation/SimulatorLayerView.cs` | C [B] | D-ART-10: carril según la superficie, fundido al cambiar de carril, panel anclado en la llave (§6.10) |
| `SimulatorPresentation/Styles/SimulatorSkin.uss` | C [D] | si cambia algo de la llave |
| `Bootstrap/AppRoot.cs` | C [B] | `LocalStore`, ciclo de vida, pantalla encendida, segundo plano del *scheduler* (§4.2, §4.4) |
| `Editor/Bootstrap/FontAssetBuilder.cs`, `ProjectBootstrapper.cs`, `UiAssetsBuilder.cs` | C [B] | DD4a-06 (tablas de kerning K2 desde la receta) y DD4a-07 (las tres caras derivadas), en B0 (§6.14) |
| `UI/Fonts/RowMateNumericSC/*.ttf`, `OFL.txt` | N [D0] | DD4a-07 |
| `UI/Fonts/Kerning/<cara>.pairs.json` | G [D0] | la lista de pares de K2 que escribe la receta (`tools/fonts/`) |
| `UI/Fonts/Generated/RowMateNumericSC-*-SDF.asset`, los 7 `*.asset` de texto | G [B] | los arma el *bootstrapper* (B0) |
| `Tests/Support/ScriptedRower.cs`, `RowerScripts.cs` | N [A] | remo de prueba R-4 y sus variantes, por timers del programador (§4.5) |
| `Tests/Support/InMemorySessionRepository.cs`, `ScriptedSessionRepository.cs` | N [A] | base de sesiones en memoria y de guion (lenta o con fallas a pedido) |
| `Tests/Support/FakeAppLifecycle.cs`, `FakeWakeLock.cs`, `LifecycleHarness.cs` | N [A] | el ciclo de vida de prueba (§4.5): segundo plano **y foco** (spec §0.4: sin pedido, adelante y con foco) |
| `Tests/Support/RoutineSeeds.cs`, `TempDatabase.cs` | C [A] | + `F4A`, las F-RARA con los nombres de la 4a; `TempDatabase.Store()` |
| `Tests/EditMode/**` | N/C [A] | §8.2, salvo `Tests/EditMode/Project/ItFourAToolingTests.cs` y `ItFourAFontTests.cs` (de [B]) y `Tests/EditMode/Design/ContrastTests.cs` (de [D0]); las excepciones de §9.0 (los conteos de fuentes que cambia B0) |
| `Tests/Simulator/WrenchPlacementTests.cs`, `SimulatorPanelLayoutTests.cs` | N / C [A] | §8.2 |
| `Tests/Golden/oracle/workout_engine.json` | G [A] | vectores del motor legacy (§3.7) |
| `Tests/UiSupport/UiTestHost.cs`, `UiHostOptions.cs`, `HostSessions.cs`, `HostBackends.cs`, `DeviceProfile.cs` | C / N [B] | sesiones, ciclo de vida, remo de prueba, `P360`, contorno de la zona del bote (§7.1) |
| `Tests/PlayMode/*`, `Tests/PlayModeSimulator/*`, `Tests/UnityEditMode/*` | N/C [B] | §8.2 |
| `Snapshots/WorkoutCatalog.cs`, `WrenchAudit.cs` | N [B] | §7.3, DD4a-04 |
| `Snapshots/SnapshotCatalog.cs`, `SnapshotCli.cs`, `SnapshotRunner.cs`, `ScenarioScript.cs`, `RoutinesCatalog.cs` | C [B] | conjunto `it4a`, auditoría de la llave, ayudas de guion; las sondas de la It. 3 que nombran la llave (D-ART-10) |
| `tools/oracle/engine/pubspec.yaml`, `tools/oracle/engine/test/engine_vectors_test.dart`, `tools/oracle/run-oracle.ps1`, `tools/oracle/README.md` | N / C [A] | §3.7 |
| `.gitignore` | C [A] | lo que deja `pub get` en `tools/oracle/engine/`, antes de la primera corrida de `run-oracle.ps1 -Engine` (§3.7; QA-G4aT-05). Dueño: WP-A (QA-G4aT-15) |
| `tools/sessions/Program.cs`, `tools/dotnet/RowMate.SessionList/RowMate.SessionList.csproj`, `tools/dotnet/RowMate.sln` | N / C [A] | la herramienta de solo lectura de la demo (§3.6) |
| `tools/fonts/*.py`, `tools/fonts/README.md` | N [D0] | la receta de DD4a-06 y DD4a-07 con fontTools fijado (`uvx --offline --from fonttools==4.68.0`) |
| `tools/unity/capture-snapshots.ps1`, `tools/unity/README.md` | C [B] | `-Set it4a`; las tomas nuevas |
| `docs/…/04a-motor-hud-funcional/snapshots/*` | G [B] | arnés |

**Manifest:** `Packages/manifest.json` **no cambia**. Ningún paquete nuevo en dotnet salvo el proyecto de la herramienta (que usa los del `.csproj` de Infrastructure).

### 1.2 Asmdefs

**Ninguno nuevo y ninguna referencia nueva.** `Core/Workout` es una carpeta de `RowMate.Core`; `App/Workout`, de `RowMate.App`; `Infrastructure/Sessions`, de `RowMate.Infrastructure`. El motor depende solo de Core (puertos de `Core/Ble`, `Core/Ports`, modelos y reglas de rutina). La regla de dependencias de ARCHITECTURE §3.2 no cambia: Presentation sigue sin conocer Infrastructure, Platform ni Simulator; el contexto de la llave que declara el HUD (`OverlaySurface`) es de Core y la regla que lo usa (`WrenchPlacement`) es del simulador. `AsmdefLintTests` no cambia. `PureCodeRulesTests` suma: ningún `DateTime.Now`, `DateTimeOffset.Now`, `Stopwatch` ni `Environment.TickCount` en `Core/Workout` y `App/Workout` (el tiempo es el `IScheduler`), y ninguna mención de `StepTargetTracker`, `StepResult`, `SanitizedDistance`, `StepPosition` ni `TargetLights` en `Infrastructure/` (AC-WKT-06-14: lo que no se guarda no llega a la base; QA-G4aT-09).

`tools/dotnet/RowMate.SessionList/RowMate.SessionList.csproj` (net8.0, consola) referencia `RowMate.Core` y `RowMate.Infrastructure`, como `RowMate.Tokens` referencia Core. Entra en `RowMate.sln`, así `run-tests.ps1` lo compila.

---

## 2. Motor de sesión (Core/Workout y App/Workout, WP-A)

Todo compila en `netstandard2.1`, C# 9, sin `UnityEngine`. Clases `sealed`, nulabilidad habilitada, `IAppLogger` en todo observable (ADR 004). El tiempo sale **solo** del `IScheduler` (que es un `IClock`): ni `DateTime.Now` ni `Stopwatch` (lo vigila `PureCodeRulesTests`, §1.2). Todo corre en el hilo principal; la base, en su hilo (§3). Cada regla cita su fuente: sin otra indicación, `workout_provider.dart`.

### 2.1 Fases

| Desde | Evento | Hacia | Qué pasa | Fuente |
|---|---|---|---|---|
| — | se arma la app | Idle | el motor se suscribe al remo y al pulsómetro; recibe lecturas en todas las fases | `:49`, `:82-86` |
| Idle | `StartFree()` | Active | hora de inicio, cuenta de muestreo en 0, puntos en memoria vacíos, alta de la sesión sin rutina; el reloj arranca cuando vuelve el alta (o a los 100 ms) | `:115-117`, `:133-149` |
| Idle | `StartWithRoutine(t)` | Active | R3-08 (§5); expande, paso 0 con 0 s, distancia de inicio, posiciones, objetivos en 0; después, como el libre, con id y nombre **de la plantilla** | `:120-131` |
| Active | `Pause()` | Paused | se cancela el timer y el tramo se cierra: la fracción del segundo se pierde (Q-58) | `:239-243` |
| Paused | `Resume()` | Active | tramo nuevo desde ahora; la distancia de ese momento es la referencia nueva de los metros del paso | `:245-250` |
| Active o Paused | `FinishAsync()` | Finished | se cancela el timer; punto final, estadísticas, cierre (§2.9) | `:252-303` |
| Active | último paso completo | Finished | en el mismo segundo; nada más se evalúa | `:210-216` |
| Finished | el cierre volvió (o venció el tope de 15 s **de primer plano**, §2.9) | Idle | vuelta a reposo (§2.9) y aviso (`WorkoutNotice.Ended`) | `:305-318`; AC-WKT-01-06, -07 |
| cualquiera menos Idle | `StartFree`, `StartWithRoutine` | (igual) | `StartOutcome.Busy`: ni sesión nueva ni cambio (AC-WKT-01-08, -10) | — |
| Idle, Finished | `Pause`, `Resume`, `FinishAsync` | (igual) | nada (AC-WKT-01-08; dos `Terminar` seguidos cierran una vez, AC-WKT-20-06) | — |

En el legacy `reset()` lo llama la pantalla después de `finish()` (`immersive_workout_screen.dart:146`, `:235`, `:255`, `:287`). Acá el motor vuelve a reposo **solo**, cuando termina el guardado: así el inmersivo se cierra por estado (AC-WKT-13-04) y no hay forma de quedar en `Finished` sin pantalla. La 4b, que pone el diálogo de rutina completada, suma `HoldCompleted` (el motor espera a que el diálogo se cierre); en la 4a no existe.

### 2.2 Reloj activo

```csharp
namespace RowMate.Core.Workout
{
    // Reloj activo por tiempo monótono (ADR 017, decisión 4; ARCHITECTURE §6.1, regla 2). Reemplaza al Timer.periodic(1 s)
    // del legacy (workout_provider.dart:147, :247): no cuenta disparos, cuenta los segundos enteros vencidos desde el inicio
    // del tramo activo. En ticks de 100 ns (long): el borde del segundo no depende de la coma flotante.
    public sealed class ActiveClock
    {
        public const long TicksPerSecond = TimeSpan.TicksPerSecond;
        public bool IsRunning { get; }
        public int SegmentSeconds { get; }                 // segundos ya procesados del tramo
        public void Start(long nowTicks);                  // abre un tramo: su segundo 1 vence en now + 1 s
        public void Stop();                                // cierra el tramo; lo que corría del segundo se pierde (Q-58)
        public int Due(long nowTicks);                     // segundos vencidos sin procesar; 0 si no corre
        public long DueAtTicks(int segmentSecond);         // instante monótono del segundo n del tramo (n ≥ 1)
        public long NextDueTicks { get; }                  // DueAtTicks(SegmentSeconds + 1)
        public void MarkProcessed();
        public static long ToTicks(double monotonicSeconds);   // (long)Math.Round(s · 10⁷)
    }
}
```

- **Agenda.** El motor tiene **un** timer de una vez, armado al próximo vencimiento: `Schedule(TimeSpan.FromTicks(max(0, NextDueTicks − ahora)), OnClock)`. `OnClock` procesa los `Due(ahora)` segundos pendientes, en orden, cada uno con el instante de su vencimiento, y vuelve a armar. En primer plano es uno por segundo, como el legacy; un cuadro demorado 2,5 s procesa 2 o 3 de una vez (AC-WKT-02-04). Con `FakeScheduler` el timer dispara exacto en el tick vencido; con `FrameScheduler`, en el primer cuadro que lo pasa.
- **Arranque (AC-WKT-01-02).** El primer tramo se abre cuando vuelve el alta de la sesión (bien o mal: AC-WKT-01-11) **o** a los `WorkoutTimings.ClockStartGrace` = 100 ms del toque, lo que llegue primero. Con la base en línea de las pruebas el alta vuelve en el acto: el tramo abre en el instante 0 y el segundo k cae en el instante k (spec §0.4). En la PC el alta tarda unos milisegundos (vuelve en el cuadro siguiente): es la regla del legacy (`:145-147`). Con una base lenta el reloj no espera más de 100 ms (AC-WKT-06-13) y los puntos que se tomen antes de que vuelva el alta quedan en el lote pendiente del grabador (§3.2).
- **Pausa y reanudación (Q-58, AC-WKT-01-05).** `Pause` cancela el timer y llama `Stop()`. `Resume` llama `Start(ahora)`: el segundo siguiente vence 1 s después. Con la pausa a los 10,7 s (10 segundos contados) y 30 s de pausa, el segundo 11 llega a los 41,7 s. Si se pausa antes de que abra el primer tramo, `Resume` lo abre.
- **El reloj del HUD es este** (AC-WKT-02-03): `TotalElapsedSeconds` suma uno por segundo procesado (`:155`); el tiempo del monitor solo va a los puntos.
- **Hora de pared de un segundo.** `WallAt(t) = scheduler.UtcNow − (ahora − t)`. Fuera de un alcance da la hora actual; durante el alcance de §4.1, la del instante virtual. Así la hora de fin de una rutina que terminó en segundo plano es la del segundo 30 y no la de la vuelta (AC-WKT-02-06).

### 2.3 El segundo

`ProcessSecond(long at)`, en este orden (AC-WKT-02-02; `:151-190`):

| # | Qué | Fuente |
|---|---|---|
| 0 | si la fase no es `Active`, nada | `:152` |
| 1 | `_stepElapsed++`, `_totalElapsed++`, `_sampleTick++` | `:154-156` |
| 2 | si `_sampleTick % 5 == 0`: punto con la lectura fusionada de ese momento y el paso en curso → memoria y grabador (§2.8). El legacy exige además la sesión creada (`_sessionId != null`); acá el punto se toma también con el alta en vuelo y el grabador lo escribe cuando vuelve, o lo descarta si el alta falló (AC-WKT-02-02 lo dice así desde el gate, QA-G4aT-16). En el legacy el reloj no corre antes del alta, así que con la base al día da lo mismo | `:158-173` |
| 3 | con rutina: los metros del paso (`SanitizedDistance.Update`, §2.4), los objetivos (`Targets.Update(i, data)`) y el fin del paso (§2.4) | `:176-187` (`:178-184` es `_series.update`, de la It. 5, salvo el saneamiento) |
| 4 | aviso al HUD (`Notify`) | `:189` |

El punto del paso 2 va **antes** del fin del paso del 3: pertenece al paso que termina en ese segundo (AC-WKT-06-03). Si el paso 3 termina la rutina, el aviso del paso 4 sale igual (en el legacy, `notifyListeners` corre después de `finish()`, que no se espera).

### 2.4 Avance de pasos

- **Rutina expandida** (AC-WKT-03-01): `RoutineExpansion.Flatten(t.Steps, ProgressionOverflow.Saturate)` y `Positions(t.Steps)` de la It. 1 (`routine.dart:29-97`): N repeticiones; en la repetición r, objetivo + progresión × r si existen los dos. Siempre después de R3-08 (§5). Con los topes de Q-92 los valores controlados no desbordan, pero un split guardado ≤ 0 no se controla (es "sin split", AC-RUT-13-08) y con una progresión negativa puede pasar de `int.MinValue` (QA-G4aT-11). Por eso el motor expande **saturando** en los límites de `int` en lugar de lanzar `OverflowException` (DT4a-16; el legacy, con enteros de 64 bits, sigue la cuenta: lo que se ve es lo mismo, Split siempre fuera).
- **Por tiempo** (AC-WKT-03-02): termina cuando `_stepElapsed >= DurationSeconds`; si tiene tiempo y distancia, manda el tiempo (`IntervalStep.Goal`, `:196-197`). Un paso de 0 s termina en el primer segundo (AC-WKT-03-07); uno de duración negativa también (DT4a-12).
- **Por distancia, con Q-55** (AC-WKT-03-03, DV-26): termina cuando `meter.Meters − _stepStartMeters >= DistanceMeters`, evaluado solo en el segundo. `meter` es `SanitizedDistance`, el saneamiento de los parciales:

```csharp
// series_tracker.dart:43-46 (tope), :86-102 (begin), :104-108 (rebase), :118-127 y :143 (la lectura). Lo reusa la It. 5.
public sealed class SanitizedDistance
{
    public const double MaxSpeedMetersPerSecond = 10.0;
    public double Meters { get; }                           // metros saneados desde Begin (_distance)
    public void Begin(int rawMeters, int activeSeconds);    // referencia cruda y tiempo; Meters = 0
    public void Rebase(int rawMeters);                      // al reanudar: lo remado en pausa no cuenta
    public void Update(int rawMeters, int activeSeconds);   // dt = t − último; Δ = crudo − último crudo;
                                                            // Δ < 0 o Δ > 10·max(dt; 1) → no suma; la lectura es la referencia nueva
}
```

  El motor llama `Begin(lectura.Distancia, 0)` al iniciar la rutina (`:126`, `:128`), `Update(lectura.Distancia, _totalElapsed)` en cada segundo (paso 3 de §2.3), `Rebase(lectura.Distancia)` en `Resume` (`:248`) y `Begin(0, 0)` al volver a reposo (`:315`). Con `Distancia 100`: `R-4` 25, `R-4R` 26, `R-4Z` 27 y la pausa de 20 s a los 40 m, 25 (los valores de la spec, verificados a mano en §8.3). **Sin lectura previa** (la rutina arranca con `RowingData.Empty`, distancia 0, y el monitor ya está en 5000 m): el primer salto (5004 m en 1 s) no suma, así que el paso de 100 m termina en el segundo 26; el legacy, que compara contra el 0 del inicio, en el 1 (QA-G4a-P06; es DV-26, y lo prueba `SAD.StartWithoutAPreviousReadingDropsTheFirstJump`).
- **Cambio de paso** (AC-WKT-03-04, `:203-209`): `Targets.Complete(i, paso)`, `i++`, `_stepElapsed = 0`, `_stepStartMeters = meter.Meters` (los metros de más quedan en el paso que terminó).
- **Último paso** (AC-WKT-03-05, -06, `:210-216`): `Targets.Complete`, fase `Finished`, timer cancelado, `Stop()`; el índice queda en el último paso. Si es por tiempo, su reloj queda en la duración y la cuenta muestra `0:00`; si es por distancia, la cuenta sigue mostrando su largo, sin restante ni progreso (AC-WKT-03-06, QA-G4a-P10). Empieza el cierre con `WorkoutEndKind.Completed` y la hora `WallAt(at)`.
- **Sin meta** (AC-WKT-03-08, DV-25): sin duración ni distancia nunca termina solo; se termina con `Terminar`.
- **Libre** (AC-WKT-03-09): sin pasos, sin fin automático, sin `SanitizedDistance` ni objetivos.
- **El remo se desconecta** (AC-WKT-03-10): el motor no mira la conexión (AC-BLE-10-07). Sin lecturas nuevas el reloj y los pasos por tiempo siguen, la distancia no cambia (el paso por distancia queda quieto) y los puntos repiten la última lectura.

### 2.5 Estado del paso

```csharp
// workout_provider.dart:16-42 (StepProgress), :98-112 (stepProgress); immersive_workout_screen.dart:342-346 (showUpNext).
// Existe solo con rutina y fuera de reposo (AC-WKT-04-01). Struct: el HUD lo pide en cada aviso sin reservar memoria.
public readonly struct StepProgress
{
    public StepProgress(IntervalStep step, int stepIndex, int totalSteps, int elapsedInStep, IntervalStep? nextStep);
    public IntervalStep Step { get; }            // con los objetivos ya progresados
    public int StepIndex { get; }                // desde 0, en la rutina expandida
    public int TotalSteps { get; }
    public int ElapsedInStep { get; }
    public IntervalStep? NextStep { get; }       // null en el último
    public int? RemainingInStep { get; }         // por tiempo: duración − transcurrido en [0; duración]; si no, null (el −1)
    public double Progress { get; }              // transcurrido / duración en [0; 1] si duración > 0; si no, 0 (Q-51)
    public bool IsEndingSoon { get; }            // restante de 1 a 5 (:39-41)
    public bool IsUpNext { get; }                // restante de 0 a 15 y hay siguiente (:343-344)
}
```

`WorkoutEngine.State.Value.Step` lo trae ya armado. Ejemplos de AC-WKT-04-06: en un paso de 60 s que no es el último, `IsUpNext` desde los 45 s transcurridos hasta el cambio e `IsEndingSoon` con 5 a 1; en uno de 10 s, `IsUpNext` todo el paso; en el último, `IsEndingSoon` sí y `IsUpNext` no; por distancia o sin meta, ninguno. Con la rutina terminada (AC-WKT-04-07, QA-G4a-P10): si el último paso es por tiempo, restante 0 y progreso 1; si es por distancia, sin restante (`null`) y progreso 0, como en marcha; en los dos casos, ni "por terminar" ni "a continuación" (no hay paso siguiente). **Duración negativa** (un `Min` negativo en el editor, DV-15, o una base tocada a mano): el legacy lanza en `clamp(0, d)` con `d < 0`; acá el restante es 0 y la cuenta muestra `0:00` (DT4a-12, aceptado como DV-37; `SPG.NegativeDurationShowsZero`).

### 2.6 Objetivos y semáforo

```csharp
// step_target_tracker.dart:1-71, sin cambios de regla (LT-39 a LT-44).
public enum StepResult { Met, Missed }

public sealed class StepTargetTracker
{
    public static bool HasTargets(IntervalStep step);           // :23-26: vatios mín., SPM o split (los vatios máx. no cuentan)
    public void Update(int stepIndex, RowingData data);         // :30-40: parado (SPM ≤ 0 y W ≤ 0) no cuenta; split solo si > 0
    public void Complete(int stepIndex, IntervalStep step);     // :43-62: promedios contra objetivos; sin lecturas → Missed
    public StepResult? ResultOf(int stepIndex);                 // :65
    public void Reset();                                        // :67-70
}

// Semáforo de las tarjetas (immersive_workout_screen.dart:579-602): instantáneo, con cada lectura y también en pausa.
public enum TargetState { None, Met, Off }
public readonly struct TargetLights { public TargetState Spm { get; } public TargetState Split { get; } public TargetState Watts { get; } }
public static class TargetLight
{
    // SPM: Off si actual < objetivo. Watts: Off si actual < mín. (el máx. no se mira). Split: Off si actual > objetivo
    // (más lento), sin casos especiales (paridad de val > target, immersive_workout_screen.dart:581; AC-WKT-10-04): un
    // split 0 (--:--) cumple con un objetivo ≥ 0 y queda fuera con uno < 0; con un objetivo 0, todo split > 0 queda fuera
    // (QA-G4a-P01, QA-G4aT-08).
    // null o sin ese objetivo → None (libre, calentamiento, descanso…).
    public static TargetLights Evaluate(IntervalStep? step, RowingData data);
}
```

El motor alimenta los objetivos en el paso 3 de cada segundo, cierra cada paso al completarlo (por tiempo o por distancia) y también el último; un paso cortado con `Terminar` queda sin resultado; todo se reinicia al iniciar una rutina y al volver a reposo (AC-WKT-09-07; `:129`, `:185`, `:204`, `:316`). En la 4a no se muestra (las píldoras son de la 4b): `WorkoutEngine.Targets` queda expuesto para ellas.

### 2.7 Fusión del pulso

```csharp
// workout_provider.dart:68-70, :220-237. LT-35 a LT-38.
public sealed class HeartRateFusion
{
    public int? SensorBpm { get; }                  // null = sin sensor (manda el FTMS); 0 = conectado sin lectura válida
    public RowingData Data { get; }                 // la lectura fusionada, en todas las fases
    public void OnRowerReading(RowingData d);       // Data = d con pulso = SensorBpm ?? d.HeartRate (:222)
    public void OnSensorBpm(int bpm);               // SensorBpm = bpm; Data = Data con pulso bpm (:227-228)
    public bool OnSensorStatus(HrmStatus status);   // conectado o sin bpm previo → false (nada cambia, no se avisa);
                                                    // si no: SensorBpm = null, pulso 0, true (:233-235)
}
```

El motor se suscribe a `IRowerService.Data`, `IHeartRateService.Bpm` y `IHeartRateService.Status` con `emitCurrent: false` (el `statusStream` del legacy es *broadcast*: el estado actual no llega). Avisa **una vez** por lectura del remo y una vez por bpm, aunque los valores se repitan (`Revision`, §2.10), y ninguna por un estado que no cambia nada: LT-35 cuenta exactamente 2 avisos y LT-38, 0. La prueba que cuenta se suscribe a `WorkoutEngine.State` **también** con `emitCurrent: false`: si recibiera el valor actual, LT-35 contaría 3 y LT-38, 1 (03-qa-harmony-paridad §5). Todo lo que sigue usa `Data`: la tarjeta BPM y la columna `heart_rate` de los puntos (AC-WKT-05-05).

### 2.8 Telemetría

- **Punto** (`TelemetryPoint.From(sessionId, data, stepIndex, step)`, `:160-172`): `elapsed_seconds` = tiempo **del monitor** (`data.ElapsedSeconds`, Q-15, D-12), `stroke_rate`, `stroke_count`, `distance_meters`, `pace_500m_seconds`, `power_watts`, `calories` = `TotalCalories`, `heart_rate` = el fusionado; con rutina, `step_index` (expandido) y `step_type` (`StepTypeNames.ToStorage`); en libre, vacíos.
- **Cada 5 s activos** (AC-WKT-06-02, -04): `_sampleTick % 5 == 0` en el paso 2 de §2.3; la cuenta vuelve a 0 al iniciar (`:137`) y sigue igual después de una pausa. Con `R-4` libre: segundo 5 → (305, 24.0, 105, 1020, 125, 150, 40, 0); segundo 10 → (310, 24.0, 110, 1040, 125, 150, 41, 0).
- **Punto final** (AC-WKT-06-06, `:259-273`): al terminar, con la lectura de ese momento y el paso en curso (tipo vacío si el índice quedara fuera de rango, `:270-272`). Si el cierre cae en un segundo múltiplo de 5, repite al de ese segundo (AC-WKT-06-07): `Corta` da 7 puntos con los dos últimos iguales.
- **Estadísticas** (AC-RUT-18): `SessionStats.Compute(todos los puntos en memoria)` de la It. 1 (`:276`), y `WorkoutSession.Closed(stats, fin)` (`:283-294`): distancia, tiempo y kcal máximos; vatios y SPM = P99; `strava_activity_id` vacío.
- **En memoria** quedan todos los puntos de la sesión (uno cada 5 s: 720 por hora), como `_dataBuffer` (`:75`): las estadísticas se calculan sobre todos. Uno que no se pudo escribir se reintenta en la transacción del cierre (§3.2), así que una sesión **cerrada** tiene en la base exactamente los puntos de sus estadísticas; si el reintento falla, la sesión no se cierra (AC-WKT-06-12; QA-G4aT-13). Es el "todo o nada" del legacy, donde puntos y cierre van en el mismo `try` (`:257-299`), y el Historial de la It. 9, que calcula desde `data_points` (`database_service.dart:289`), da los mismos números que la fila.
- **No se guardan** los parciales, las repeticiones ni los resultados de objetivos (AC-WKT-06-14, `:57-61`).

### 2.9 Inicio, fin y vuelta a reposo

- **`StartFree()`** (`:115-117`, `:133-149`): `Active`; `_startedAt = UtcNow`; puntos en memoria vacíos; `_sampleTick = 0`; `Notify`; `recorder.BeginAsync(new WorkoutSession(_startedAt))`; espera del primer tramo (§2.2).
- **`StartWithRoutine(t)`** (`:120-131`): si `!WorkoutStartRule.CanStart(t)` → `CannotStart` sin tocar nada (§5). Si no: `Steps = Flatten`, `Positions`, `i = 0`, `_stepElapsed = 0`, `meter.Begin(Data.DistanceMeters, 0)`, `_stepStartMeters = 0`, `Targets.Reset()`, y lo mismo que el libre con `RoutineId = t.Id` y `RoutineName = t.Name` **copiados** (renombrar o borrar la plantilla después no los cambia; AC-WKT-01-03).
- **`FinishAsync()`** (a mano, `:252-303`): `Finished`, timer cancelado, `Stop()`, `Notify`; cierre con `WorkoutEndKind.Manual` y `WallAt(ahora)`.
- **Cierre** (los dos casos): si hubo alta (o está en vuelo), punto final → memoria; estadísticas; `recorder.FinishAsync(final, cerrada)`, que espera el alta y los lotes pendientes y escribe **en una transacción** los puntos de los lotes que fallaron (una vez más), el punto final y el cierre (§3.2). Se espera con un tope de `FinishSaveTimeout` = 15 s **de primer plano**: un `ForegroundTimeout` (§4.1), que no cuenta el tiempo en segundo plano (spec §12 punto 2). Así una rutina que termina durante el alcance espera al cierre real, aunque la vuelta llegue mucho después del fin (AC-WKT-02-06, a los 330 s; QA-G4aT-03). Si el alta falló, no hay punto final ni cierre (`:256`).
- **El primer cuadro al volver, con el cierre en vuelo** (QA-G4aT-03). Si la rutina terminó durante el alcance, el cierre se encoló en el tiempo virtual y su continuación llega al hilo principal recién después. El primer cuadro muestra entonces el inmersivo con la rutina terminada (AC-WKT-03-06: el último paso, `0:00` o su largo, los controles apagados) y, cuando vuelve el cierre (con la base al día, en el cuadro siguiente), sale el inmersivo y aparece el aviso del fin, una vez y con sus 4 s desde ese momento (AC-WKT-02-06, §2.11).
- **Vuelta a reposo** (`:305-318`, AC-WKT-01-07): `Idle`; sin rutina, índice 0, relojes en 0, sesión, inicio, puntos, posiciones; `meter.Begin(0, 0)`; `Targets.Reset()`; `Notify`. **No** se borran `Data`, `SensorBpm` ni `LastFinishedSessionId`: al iniciar otra sesión el HUD muestra los últimos valores del monitor hasta la lectura siguiente.
- **Última terminada** (AC-WKT-06-11, `:296`): `LastFinishedSessionId = id` solo si el cierre volvió bien, también si volvió después del tope (abajo).
- **Avisos** (`WorkoutNotice`): `SaveFailed` la primera vez que falla el alta o un lote, una sola vez mientras se rema (AC-WKT-01-11, AC-WKT-06-12); `Ended(kind, saved)` al volver a reposo, con `saved` = la sesión quedó **cerrada en la base con todos sus puntos** (el alta y la transacción del cierre bien; un lote que falló cuenta como guardado si entró en el reintento del cierre). Una falla del cierre solo se ve ahí. Con una falla pasajera que el reintento recupera, el aviso del final es el de un guardado bueno (DT4a-17, para el PO). El legacy no avisa nada (DV-29).
- **Tope del cierre.** Si el cierre no vuelve en 15 s de primer plano (una base colgada; con SQLite el *busy timeout* es de 5 s), el motor vuelve a reposo con `saved = false` y lo anota (`[WORKOUT] cierre sin respuesta`, `Warning`). Si después vuelve bien, la sesión queda cerrada en la base, con todos sus puntos, y pasa a ser la última terminada (`[WORKOUT] cierre tardío`, `Info`): el orden FIFO de la base garantiza que una sesión más nueva no se cerró antes, así que no la pisa. El aviso que ya salió no cambia. Nunca queda el inmersivo abierto (AC-WKT-06-12: "nunca se cuelga").

### 2.10 Firmas

```csharp
namespace RowMate.Core.Workout
{
    public enum WorkoutEndKind { Manual, Completed }
    public enum WorkoutNoticeKind { SaveFailed, Ended }
    public enum StartOutcome { Started, Busy, CannotStart }

    public readonly struct WorkoutNotice
    {
        public WorkoutNoticeKind Kind { get; }
        public WorkoutEndKind End { get; }          // con Ended
        public bool Saved { get; }                  // con Ended: la sesión quedó cerrada en la base con todos sus puntos
    }

    // Una foto por aviso (= cada notifyListeners del legacy). Revision sube siempre: dos lecturas iguales también avisan.
    // En segundo plano no se publica ninguna (QA-G4aT-07): la primera después de un tramo sin publicar lleva Instant = true
    // (la vista la aplica sin animar: AC-WKT-02-08).
    public sealed record WorkoutSnapshot(long Revision, WorkoutPhase Phase, RowingData Data, int TotalElapsedSeconds,
                                         StepProgress? Step, bool IsRoutine, bool IsSaving, bool Instant);

    // Puerto de WorkoutProvider (workout_provider.dart:44-328). Hilo principal. Lo arma AppComposer y vive con la app.
    public sealed class WorkoutEngine : IDisposable
    {
        public const string Tag = "WORKOUT";

        public WorkoutEngine(IRowerService rower, IHeartRateService heartRate, ISessionRepository sessions, IScheduler scheduler,
                             IAppLifecycle lifecycle, IAppLogger logger);

        public IReadOnlyObservable<WorkoutSnapshot> State { get; }      // arranca en Idle con RowingData.Empty
        public IEventSource<WorkoutNotice> Notices { get; }
        public long? LastFinishedSessionId { get; }                     // :78-80
        public IReadOnlyList<IntervalStep>? Steps { get; }              // la rutina expandida en curso
        public IReadOnlyList<StepPosition>? Positions { get; }          // :61, :127 (para la It. 5)
        public StepTargetTracker Targets { get; }                       // :60, :92 (para las píldoras de la 4b)

        public StartOutcome StartFree();                                // :115-117
        public StartOutcome StartWithRoutine(Routine template);         // :120-131 + R3-08
        public void Pause();                                            // :239-243
        public void Resume();                                           // :245-250
        public Task FinishAsync();                                      // :252-303; idempotente
        public void Dispose();                                          // :320-327; una sesión en curso queda cortada (§3.4)
    }

    public static class WorkoutTimings
    {
        public const int SampleEverySeconds = 5;                                        // :159
        public static readonly TimeSpan ClockStartGrace = TimeSpan.FromMilliseconds(100); // AC-WKT-01-02
        public static readonly TimeSpan FinishSaveTimeout = TimeSpan.FromSeconds(15);     // AC-WKT-06-12; de primer plano
                                                                                         // (ForegroundTimeout, §4.1)
        public static readonly TimeSpan BackgroundFlushTimeout = TimeSpan.FromSeconds(1); // AC-WKT-06-15
    }
}
```

- **Ciclo de vida.** El motor se suscribe a `IAppLifecycle.InBackground`: al pasar a `true` con una sesión en curso llama `recorder.FlushAndWait(BackgroundFlushTimeout)` (AC-WKT-06-15; ADR 017, decisión 5). No pausa nada: en Windows el *loop* sigue y en Android el alcance de §4.1 pone todo al día al volver.
- **Sin fotos en segundo plano** (QA-G4aT-07). Mientras `InBackground` es `true` (en la app dura todo el alcance, por el orden de `AppRoot`, §4.2) o `IsCatchingUp`, `Notify()` solo sube `Revision` y marca la foto como pendiente: no arma ni publica nada. Al pasar `InBackground` a `false` publica **una** foto, con `Instant = true`. Un alcance de 8 h (≈ 57 600 segundos y lecturas) arma así una sola foto y un solo estado del HUD en lugar de ≈ 86 000 (`WBG.EightHourCatchUpIsBounded`, §8.2). Los `WorkoutNotice` sí salen en el momento (son pocos) y los retiene `WorkoutStore` (§2.11). En primer plano nada cambia: LT-35 y LT-38 cuentan igual. **Un cuadro de primer plano que llega con varios segundos vencidos** (el *loop* frenado, o el equipo suspendido con un monótono que siguió contando) los procesa en orden, cada uno en su segundo, pero publica **una** foto al final, con `Instant = true`, como la vuelta del segundo plano: `OnClock` marca `_coalescing` cuando vencieron dos o más y `Notify()` lo trata como `IsCatchingUp` (QA-V4a-ROB-05; `WHS.OneInstantStateAfterALateFrameInTheForeground`; "Cambios por verificación (ronda 1)").
- **`Dispose`** (la app se cierra): da de baja las suscripciones y el timer y llama `recorder.Abandon()` (manda lo pendiente; no cierra la sesión: queda cortada, como un corte de luz; P-03). `AppRoot` dispone la base después (§3.5): el ejecutor termina lo encolado.
- **Continuaciones tarde.** Como en la It. 3 (QA-G3-09), todo lo que vuelve de la base después de `Dispose` no cambia estado ni avisa.

### 2.11 Stores de App

```csharp
namespace RowMate.App.Workout
{
    public enum RoutineTapOutcome { Ignored, CannotStart, Confirm }   // sin remo o con sesión / R3-08 / abrir el diálogo

    // La vista de reposo (F-WKT-12, F-BLE-10). Routines y Phase son los de la colección compartida (AC-WKT-12-05).
    public sealed record WorkoutRestState(bool RowerConnected, RoutinesPhase RoutinesPhase, IReadOnlyList<Routine> Routines,
                                          WorkoutPhase SessionPhase);

    public sealed class WorkoutStore : IDisposable
    {
        public const string Tag = WorkoutEngine.Tag;
        public WorkoutStore(WorkoutEngine engine, DeviceStore device, RoutinesStore routines, IAppLifecycle lifecycle, IAppLogger logger);
        public IReadOnlyObservable<WorkoutRestState> State { get; }
        public IEventSource<string> Toasts { get; }      // clave del catálogo: workoutRoutineCannotStart, workoutSaveError, workoutCompleted;
                                                          // sin el usuario delante (IsUserFacing false) retiene solo el último
        public bool StartFree();                          // conectado y en reposo → engine.StartFree()
        public RoutineTapOutcome TapRoutine(Routine routine);
        public bool StartRoutine(Routine routine);        // al confirmar: solo mira que el motor esté en reposo (AC-BLE-10-06)
    }

    public enum TargetArrow { None, Up, Down }            // ▲ SPM y Watts (subir), ▼ Split (bajar)

    public readonly struct HudMetric
    {
        public string Value { get; }                      // texto exacto (WorkoutFormats)
        public double RollValue { get; }                  // para el sentido del rodillo (TabularNumber)
        public bool HasTarget { get; }
        public bool OffTarget { get; }
        public TargetArrow Arrow { get; }                 // None si cumple o no tiene objetivo
        public string? TargetText { get; }                // hudTarget: "obj. 22", "target 160", "obj. 02:05"; null sin objetivo
    }

    public sealed record HudTargetLabel(string Text, string Before, bool HasTrend, string After);   // DV-35
    public sealed record HudStepRow(StepType Type, string TypeText, HudTargetLabel? Target, string Countdown, double CountdownValue,
                                    bool EndingSoon, StepType? UpNextType, string? UpNextTypeText);

    // Instant: copia de WorkoutSnapshot.Instant; true en el primer estado después del segundo plano (AC-WKT-02-08).
    public sealed record WorkoutHudState(long Revision, WorkoutPhase Phase, bool IsRoutine, HudMetric Spm, HudMetric Split, HudMetric Watts,
                                         string Kcal, int KcalValue, string? Bpm, int BpmValue, string Clock, int ClockValue,
                                         string Distance, int DistanceValue, HudStepRow? Step, bool ControlsEnabled, bool ShowResume,
                                         bool Instant);

    // El HUD (F-WKT-10, -13, -14, -16, -17, -20). Uno por inmersivo: lo crea AppContext.CreateHud() y lo dispone la página.
    public sealed class WorkoutHudStore : IDisposable
    {
        public WorkoutHudStore(WorkoutEngine engine, IReadOnlyObservable<Localizer> texts, IAppLogger logger);
        public IReadOnlyObservable<WorkoutHudState> State { get; }
        public void Pause();
        public void Resume();
        public Task FinishAsync();
    }
}
```

| Regla del *store* de reposo | Fuente |
|---|---|
| "Conectado" = `DeviceState.IsConnected` (estado del remo `Connected`); **cualquier otro** de los cinco de `BleStatus` es "sin remo": `Off` (Bluetooth apagado), `Scanning`, `Connecting` y `Disconnected`. Se prueba con los cuatro (`WST.AllFourNoRowerStates`, `WSC.BannerWithoutRower`; QA-G4a-P07) | `device_provider.dart:94`; `ble_service.dart:8`; AC-BLE-10-03 |
| `StartFree`: sin remo o fuera de reposo → `false`, sin sesión | `workout_screen.dart:86-91`; AC-BLE-10-04, AC-WKT-01-10 |
| `TapRoutine`: sin remo o fuera de reposo → `Ignored`; `!CanStart` → `CannotStart` y aviso `workoutRoutineCannotStart`, sin sesión ni diálogo; si no, `Confirm` | `:118-120`; AC-WKT-12-07, -08 (P-09) |
| `StartRoutine`: no vuelve a mirar la conexión (el diálogo pudo quedar abierto mientras se desconectaba) | `:163-165`; AC-BLE-10-06 |
| Avisos del motor: `SaveFailed` → `workoutSaveError`; `Ended(Completed, true)` → `workoutCompleted`; `Ended(_, false)` → `workoutSaveError`; `Ended(Manual, true)` → nada | AC-WKT-01-11, AC-WKT-03-05, AC-WKT-06-12, Q-53 |
| **Sin el usuario delante** (`IAppLifecycle.IsUserFacing` = adelante **y** con foco; en segundo plano, con la ventana minimizada o sin foco, `false`): el *store* no emite; guarda solo el último aviso (reemplaza al anterior). Al volver `IsUserFacing` a `true` emite ese aviso **una vez**: el `ToastLayer` lo muestra con sus 4 s desde ese momento. Así el `workoutCompleted` de una rutina que terminó en segundo plano o minimizada siempre se ve, una sola vez (QA-G4a-P08; `WST.HeldToastShowsOnceWhenFacingTheUserAgain`, `WBU.CompletedToastOnceOnReturn`) | AC-WKT-02-06, -08, -09; spec §12 punto 13; legacy: el diálogo de completado es modal (`immersive_workout_screen.dart:207-212`) |
| La colección es la de `RoutinesStore`: Workout no vuelve a leer la base | AC-WKT-12-05 |

| Campo del HUD | Regla | Fuente |
|---|---|---|
| `Spm.Value` | `WorkoutFormats.Spm` (`24.0`) | `immersive_workout_screen.dart:625` |
| `Split.Value` | `WorkoutFormats.LiveSplit` (`02:13`, `--:--`) | `rowing_data.dart:24-29` |
| `Watts.Value` | `WorkoutFormats.Watts` (entero crudo, Q-38); la unidad `hudUnitWatts` la pone la vista | `:655-656` |
| `Kcal`, `Bpm` | enteros; `Bpm` null con pulso fusionado 0 (sin tarjeta) | `:717`, `:721-725` |
| `Clock`, `Distance` | `WorkoutFormats.SessionClock(total)`, `HudDistance(distancia del monitor)` | `:569-577`, `:698-699` |
| semáforo de cada tarjeta | `TargetLight.Evaluate(Step?.Step, Data)`: `HasTarget`, `OffTarget`, `Arrow` y `TargetText` = `hudTarget` con el objetivo (SPM y vatios mín. con `Fmt.Integer`, split con `TimeFormats.MinSec`) | `:585-602`; AC-WKT-10-01 a -05; D-ART-09 |
| `Step.TypeText`, `Target` | `StepTypeInfo.NameKey`; `StepLabels.Targets` partido por `TrendText` (sin objetivos, null) | `:385-415`; AC-WKT-11-13, AC-WKT-14-03 |
| `Step.Countdown` | por tiempo `WorkoutFormats.Countdown(restante)`; por distancia `stepDurationMeters`; sin meta `stepDurationNone` | `:456-458`; AC-WKT-03-08 |
| `Step.EndingSoon`, `UpNextType` | `StepProgress.IsEndingSoon`; `IsUpNext ? NextStep.Type : null` | `:340-346` |
| `ControlsEnabled`, `ShowResume` | `Active` o `Paused`; `Paused` | `:923-948`; AC-WKT-20-06 |

El *store* del HUD rehace el estado con cada foto del motor y con cada cambio de idioma: unas pocas cadenas por lectura (los eventos de 1 Hz pueden reservar poco, ARCHITECTURE §20.2). En segundo plano el motor no publica fotos (§2.10), así que el *store* no rehace nada durante el alcance: arma **un** estado al volver, con `Instant = true` (QA-G4aT-07). Los rótulos fijos (`hudSpm`, `hudFreeTitle`…) los pone la vista desde `Localizer`.

### 2.12 Formatos (F-WKT-11)

`Core/Workout/WorkoutFormats` reúne los 15 formatos; donde ya hay una regla de la It. 1, la llama. Todo con `Fmt`, `DartMath` y `TimeFormats`: punto decimal, sin cultura del equipo (AC-WKT-11-16, Q-06).

| # | Formato | Regla | Ejemplos (AC) | Fuente |
|---|---|---|---|---|
| 1 | `LiveSplit(int)` | `TimeFormats.SplitOrDashes` | 133 → `02:13`; 6000 → `100:00`; 0, −5 → `--:--` | `rowing_data.dart:24-29` |
| 2 | `Spm(double)` | `Fmt.Fixed(x, 1)` | 24.25 → `24.3` | `immersive_workout_screen.dart:625` |
| 3 | `Watts(int)` | `Fmt.Integer` | −10 → `-10` | `:655` |
| 4 | `HudDistance(int)` | `Fmt.Integer`, sin miles | 123456 → `123456` | `:698` |
| 5 | `Count(int)` | `Fmt.Integer` (kcal, pulso) | 142 → `142` | `:717`, `:725` |
| 6 | `SessionClock(long)` | `TimeFormats.Elapsed` | 175 → `02:55`; 3725 → `1:02:05` | `:569-577` |
| 7 | `Countdown(long)` | `TruncDiv(r, 60) + ":" + Mod(r, 60)` a dos dígitos | 65 → `1:05`; 359 999 → `5999:59` | `:456-457` |
| 8 | `StepDistance(int, l10n)`, `StepNoGoal(l10n)` | `stepDurationMeters` / `stepDurationNone` | 500 → `500m`; sin meta → `?` | `:458`; DV-25 |
| 9 | `LapTime(double)` | décimas = `DartMath.Round(s · 10)`; `m:` + `Fmt.Fixed(resto/10, 1)` relleno a 4 | 166.667 → `2:46.7`; 59.96 → `1:00.0` | `series_panels.dart:9-14` |
| 10 | `RepSplit(double?)` | `DartMath.Round`, `m:ss`; null → `—` | 133.5 → `2:14` | `series_panels.dart:16-20`, `:340` |
| 11 | `PanelMeters(long)` | miles con punto, siempre (`NumberFormat('#,##0', 'es')`), + `␣m` | 1245 → `1.245 m` | `series_panels.dart:22-24` |
| 12 | `WallClock(DateTimeOffset)` | `HH:mm` de 24 h, de la hora local | 09:05 → `09:05` | `series_panels.dart:98` |
| 13 | etiqueta de objetivo | `StepLabels.Targets` (It. 1) | `>160W · 24 spm · 📈 SPM +2/rep` | `interval_step.dart:79-109` |
| 14 | resumen de la rutina | `RoutineSummary.Text` (It. 1) | `1 serie · 1 descanso` | `routine.dart:100-105` |
| 15 | `FinalDistance(int)`, `FinalCalories(int)` | `{n} m` sin miles; kcal sin unidad | 1484 → `1484 m` | `immersive_workout_screen.dart:986`, `:991` |

### 2.13 Composición

```csharp
public sealed record AppServices(DeviceBackend Devices, IRoutineRepository Routines, ISessionRepository Sessions, IScheduler Scheduler,
                                 IAppLogger Logger, LocalizationSet Texts, ISystemLocale Locale, IAccessibilitySettings Accessibility,
                                 ShellOptions Shell)
{
    public IClock? CreationClock { get; init; }          // It. 3
    public IAppLifecycle? Lifecycle { get; init; }       // null = ForegroundLifecycle (siempre en primer plano)
}

public sealed class AppContext : IDisposable
{
    public WorkoutEngine Workout { get; }                 // vive con la app (legacy: WorkoutProvider en el MultiProvider, main.dart:82)
    public WorkoutStore WorkoutRest { get; }
    public WorkoutHudStore CreateHud();                   // uno por inmersivo; lo dispone la página
    // Dispose: WorkoutRest, después Workout (que deja cortada una sesión en curso), antes que Routines y el resto.
}
```

`AppComposer` arma el motor con `services.Devices.Rower`, `.HeartRate`, `services.Sessions`, el `Scheduler`, `services.Lifecycle ?? ForegroundLifecycle.Instance` y el registro, y después `WorkoutStore` con el motor, `DeviceStore`, `RoutinesStore` y el mismo ciclo de vida (para retener los avisos, §2.11). `AppServices` incompleto (sin `Sessions`) sigue lanzando `ArgumentException`. Lo cambian en WP-B los que arman `AppServices` fuera de dotnet: `AppRoot` y `UiTestHost` (§1.1).

---

## 3. Guardado incremental y durable (WP-A)

### 3.1 Puerto

```csharp
namespace RowMate.Core.Ports
{
    // Sesiones y puntos (F-WKT-06 + F-HIS-01 a -03, F-HIS-17, F-HIS-19). Contrato: las operaciones se EJECUTAN EN EL ORDEN EN
    // QUE SE LLAMAN (el SerialExecutor de la It. 3 es FIFO; los dobles de prueba también lo cumplen). Cada escritura es
    // atómica. Las fechas van en UTC con IsoDate.FormatUtc (AC-RUT-16-04) y vuelven en hora local (IsoDate.Parse).
    public interface ISessionRepository
    {
        Task<long> CreateAsync(WorkoutSession session);                      // alta (AC-WKT-06-01): el id nuevo. La tarea es la
                                                                             // del ejecutor (sin async/await en el medio): se
                                                                             // completa EN EL HILO DE LA BASE, antes del trabajo
                                                                             // siguiente (lo usa AddPointsAfterCreateAsync)
        Task AddPointsAsync(IReadOnlyList<DataPoint> points);                // un lote, en una transacción
        Task AddPointsAfterCreateAsync(Task<long> creation,                  // un lote de una sesión cuyo id todavía no volvió al
                                       IReadOnlyList<DataPoint> points);     // hilo principal: se encola en el acto, DETRÁS del
                                                                             // alta (FIFO), y toma el id que dejó; si el alta falló,
                                                                             // no escribe nada (AC-WKT-06-15; QA-G4aT-06)
        Task FinishAsync(IReadOnlyList<DataPoint> points, WorkoutSession closed);   // en UNA transacción: los puntos (los de lotes
                                                                             // que fallaron, una vez más, y el final al último) y
                                                                             // el cierre; SessionNotFoundException si no está.
                                                                             // Cualquier falla → rollback: la sesión queda cortada
                                                                             // con lo que ya tenía (AC-WKT-06-12; QA-G4aT-13)
        Task<WorkoutSession?> GetByIdAsync(long id);                         // AC-HIS-19-01 (database_service.dart:241-246)
        Task<IReadOnlyList<DataPoint>> GetPointsAsync(long sessionId);       // AC-HIS-19-02 (:263-287)
        Task<IReadOnlyList<SessionListing>> ListAsync();                     // herramienta de la demo y QA (§3.6)
        bool WaitForPendingWork(TimeSpan timeout);                           // espera acotada a que se escriba lo encolado
    }
}
```

`GetByIdAsync`, `GetPointsAsync` y `ListAsync` no los usa la app en la 4a: los usan las pruebas, la herramienta de la demo y, desde la It. 9, el Historial. `WaitForPendingWork` bloquea el hilo que llama: solo se usa al pasar a segundo plano (§3.2), con tope. No espera ninguna continuación en el hilo principal (la tarea del ejecutor se completa en el hilo de la base), así que no se traba. Por la misma razón `AddPointsAfterCreateAsync` funciona con el hilo principal bloqueado: el trabajo del lote corre en el hilo de la base, después del alta, y lee el resultado de la tarea del alta, que ya se completó ahí (`SerialExecutor` pone el resultado antes de pasar al trabajo siguiente; `SerialExecutor.cs`, `Execute`). Si encontrara esa tarea sin terminar, el contrato FIFO se rompió: lanza y el lote cuenta como fallado. Los dobles (`InMemorySessionRepository`, `ScriptedSessionRepository`) cumplen lo mismo con su propio orden.

### 3.2 `SessionRecorder`

```csharp
namespace RowMate.Core.Workout
{
    // Lo que el motor le pide a la base durante una sesión, en orden y sin frenar el HUD (Q-12, Q-13). Hilo principal.
    // - Alta en el acto (BeginAsync). Los puntos que llegan con el alta en vuelo esperan en el lote pendiente.
    // - Un solo lote de puntos en vuelo: los que llegan mientras tanto se juntan y salen en el lote siguiente, en una
    //   transacción. Con la base al día, cada lote es un punto y sale en el mismo cuadro (AC-WKT-06-05: "a más tardar 1 s").
    // - Cierre: espera el alta y manda lo pendiente; los puntos de los lotes que fallaron (una vez más), el punto final y el
    //   cierre van juntos, en UNA transacción, DESPUÉS del último lote (lo garantiza el orden del puerto). Si esa transacción
    //   falla, la sesión queda cortada: una sesión cerrada siempre tiene todos sus puntos (AC-WKT-06-12; QA-G4aT-13).
    // - Una falla no corta nada: se registra (Warning, tag WORKOUT), HadFailure queda en true y Failed avisa una sola vez por
    //   sesión. Lo que ya se escribió queda escrito y los puntos del lote que falló se guardan para el reintento del cierre.
    //   Si el alta falla, los puntos de esa sesión se descartan (no hay sesión).
    // - Con el alta en vuelo y el hilo principal que no puede esperar su vuelta (FlushAndWait, Abandon), el lote pendiente
    //   va con AddPointsAfterCreateAsync: detrás del alta, en el hilo de la base (AC-WKT-06-15; QA-G4aT-06).
    // - Ronda 1 de verificación: FlushAndWait y Abandon mandan también los puntos de los lotes que fallaron (QA-V4a-ROB-03) y,
    //   con Terminar confirmado y el cierre todavía esperando en el hilo principal la vuelta de un lote o del alta, esperan la
    //   base con tope, resuelven ahí lo que volvió y encolan el cierre (QA-V4a-ROB-04).
    public sealed class SessionRecorder : IDisposable
    {
        public SessionRecorder(ISessionRepository repository, IAppLogger logger);
        public long? SessionId { get; }
        public bool IsCreating { get; }
        public bool HadFailure { get; }
        public int PointsToRetry { get; }                            // los de lotes que fallaron, para el cierre
        public event Action? Failed;
        public Task<bool> BeginAsync(WorkoutSession session);        // true = creada
        public void Add(DataPoint point);                            // el SessionId lo pone el grabador (o el repositorio, detrás
                                                                     // del alta)
        public Task<bool> FinishAsync(DataPoint finalPoint, WorkoutSession closed);   // true = cerrada con todos sus puntos
        public bool FlushAndWait(TimeSpan timeout);                  // al pasar a segundo plano: manda todo lo pendiente (también
                                                                     // los puntos de lotes fallados) aunque haya otro en vuelo (o
                                                                     // el alta), y espera la base (con tope); con Terminar
                                                                     // confirmado, mitad del tope y el cierre en el acto
        public void Abandon();                                       // la app se cierra: manda lo pendiente (también detrás del
                                                                     // alta) y no cierra la sesión, salvo un Terminar confirmado
                                                                     // cuyo cierre esperaba un lote (lo encola)
        public void Dispose();                                       // resultados que llegan después no cambian nada
    }
}
```

| Situación | Qué hace | Por qué |
|---|---|---|
| Base al día | alta, cada punto en su lote de uno, cierre | es lo de siempre; un punto queda escrito milisegundos después de tomarse |
| Base lenta (2 s por escritura, AC-WKT-06-13) | con un lote en vuelo, los puntos nuevos se juntan; el cierre sale cuando vuelve el último lote | el orden se conserva y la cola no crece; el HUD no espera nada |
| Alta lenta | el reloj arranca a los 100 ms (§2.2); los puntos esperan el id y salen en orden cuando vuelve | el reloj nunca espera a la base |
| Alta que falla (AC-WKT-01-11) | `Failed`; sin puntos ni cierre; el entrenamiento sigue igual | el legacy deja el reloj en `00:00` (DV-29) |
| Lote que falla (AC-WKT-06-12) | `Failed` (una vez); sus puntos quedan para el reintento; los lotes siguientes se siguen intentando | lo que se pudo guardar queda guardado |
| Cierre (con o sin lotes fallados) | una transacción: los puntos a reintentar (en su orden), el punto final y el `UPDATE`. Si falla (otra vez el mismo punto, o el cierre), *rollback*: la sesión queda **cortada**, con lo que ya tenía, y `Ended(_, false)` | una sesión cerrada siempre tiene sus puntos (P-16; QA-G4aT-13); el legacy, todo o nada en el mismo `try` (`:257-299`) |
| Pasa a segundo plano (AC-WKT-06-15) | `FlushAndWait(1 s)`: manda el lote pendiente (con el alta en vuelo, con `AddPointsAfterCreateAsync`), **también los puntos de los lotes que fallaron** (adelante, como `Abandon`; QA-V4a-ROB-03), y espera el ejecutor, a lo sumo 1 s. **Con Terminar ya confirmado y el cierre sin encolar** (espera en el hilo principal la vuelta de un lote o del alta, una continuación que en segundo plano puede no correr nunca): espera la mitad del tope; si la base terminó lo que tenía, las tareas del alta y de los lotes ya están completas, así que se resuelven ahí (`IsCompleted` antes de `Result`, sin bloquear), el cierre se encola y se espera la otra mitad (QA-V4a-ROB-04) | en Android el sistema puede cerrar la app en segundo plano (RA-08); con una base que tarda más de 1 s pueden faltar los últimos puntos (lo acepta AC-WKT-06-15). Un Terminar confirmado ya no queda cortado por irse enseguida, y el tope total no cambia |
| La app se cierra (P-03) | `Abandon()`: manda el lote pendiente (también detrás del alta, y los puntos a reintentar); `LocalStore.Dispose` espera el ejecutor (hasta 5 s). Con Terminar confirmado y el cierre esperando un lote o el alta: espera la base (tope de 1 s), resuelve lo que volvió y encola el cierre, que `LocalStore.Dispose` termina (QA-V4a-ROB-04) | sin Terminar, la sesión queda cortada, con sus puntos; un Terminar confirmado antes de cerrar la ventana queda cerrado, con todos sus puntos (L08 y L08b de la lente de robustez) |

### 3.3 SQL exacto (esquema v8, sin cambios)

El DDL es el de la It. 3 (`03-rutinas/02-tech-design §2.1`), que repite `database_service.dart:106-138` más `origin` y el índice. `SqliteSessionRepository` usa solo estas sentencias, con parámetros y literales entre comillas simples (QA-G3-08):

```sql
-- CreateAsync (insertSession, :216-219, con toMap de workout_session.dart:39-51: los totales van en 0 explícitos)
INSERT INTO workout_sessions (routine_id, routine_name, started_at, finished_at, total_distance_meters, total_time_seconds,
                              avg_power_watts, avg_stroke_rate, total_calories, strava_activity_id, origin)
VALUES (?, ?, ?, NULL, 0, 0, 0, 0, 0, NULL, 'app');
SELECT last_insert_rowid();

-- AddPointsAsync: una transacción (RunInTransaction) con una inserción por punto, en orden (toMap de DataPoint, :169-182).
-- AddPointsAfterCreateAsync: la misma, con session_id = el resultado de la tarea del alta, leído en el hilo de la base.
INSERT INTO data_points (session_id, elapsed_seconds, stroke_rate, stroke_count, distance_meters, pace_500m_seconds,
                         power_watts, calories, heart_rate, step_index, step_type)
VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?);

-- FinishAsync: una transacción. Los puntos que se reintentan y el punto final, en orden, con la misma inserción y, después,
-- la fila entera como updateSession (:221-229), salvo el id y origin (el legacy escribe también id con el mismo valor; origin
-- no existía). 0 filas → SessionNotFoundException; cualquier falla → rollback: no queda ni un punto ni el cierre, y la
-- sesión sigue cortada con lo que tenía (QA-G4aT-13).
INSERT INTO data_points (...) VALUES (...);   -- una por punto: los reintentados y el final
UPDATE workout_sessions
   SET routine_id = ?, routine_name = ?, started_at = ?, finished_at = ?, total_distance_meters = ?, total_time_seconds = ?,
       avg_power_watts = ?, avg_stroke_rate = ?, total_calories = ?, strava_activity_id = NULL
 WHERE id = ?;

-- GetByIdAsync (getSessionById, :241-246)
SELECT id, routine_id, routine_name, started_at, finished_at, total_distance_meters, total_time_seconds, avg_power_watts,
       avg_stroke_rate, total_calories, strava_activity_id
  FROM workout_sessions WHERE id = ?;

-- GetPointsAsync (getDataPointsForSession, :263-287: ORDER BY elapsed_seconds ASC; acá con id para desempatar, DT4a-05)
SELECT id, session_id, elapsed_seconds, stroke_rate, stroke_count, distance_meters, pace_500m_seconds, power_watts, calories,
       heart_rate, step_index, step_type
  FROM data_points WHERE session_id = ? ORDER BY elapsed_seconds, id;

-- ListAsync (la herramienta de la demo; el orden de getSessions, :248-252, con id para desempatar)
SELECT s.id, s.started_at, s.finished_at, s.routine_name, s.origin,
       (SELECT count(*) FROM data_points p WHERE p.session_id = s.id)
  FROM workout_sessions s ORDER BY s.started_at DESC, s.id DESC;
```

- **Filas.** `WorkoutSessionRow` y `DataPointRow` (`[Table]`, `[Column]` de sqlite-net, como las de rutinas), con los enteros en `long?` y la conversión `checked` de la It. 3. La fila de sesión pasa por `WorkoutSessionCodec` (It. 1): fechas con `IsoDate.FormatUtc` al escribir (`2026-10-28T13:00:12.300Z`) y `IsoDate.Parse` a la hora local al leer (AC-HIS-19-01). `step_type` va como texto, igual que el legacy (`StepTypeNames`).
- **El índice** `idx_data_points_session (session_id, elapsed_seconds)` de la It. 3 sirve a las dos lecturas de puntos.
- **Pragmas** (It. 3): WAL, `synchronous = FULL` (cada *commit* sobrevive a un corte de luz, DT-07), `foreign_keys = OFF`, *busy timeout* de 5 s.
- **Ningún tipo de sqlite-net** sale de Infrastructure (`PureCodeRulesTests.SqliteOnlyInInfrastructure`). Las pruebas leen con `QueryRawForTestsAsync` y con el mismo repositorio.

### 3.4 Qué queda si la app se corta

| Momento del corte | Base |
|---|---|
| Antes de que vuelva el alta | nada, o la fila de la sesión sin puntos si el *commit* llegó (`finished_at` vacío, totales en 0) |
| En marcha (o en pausa) | la sesión con `finished_at` vacío, totales en 0 (`DEFAULT 0` y los ceros del alta) y **todos** los puntos escritos hasta ese momento: los de 5 s, a más tardar 1 s después de tomarse (AC-WKT-06-05, -10) |
| En segundo plano, si el sistema cierra la app (Android) | lo mismo, con los puntos hasta que pasó a segundo plano: se esperaron al pasar (AC-WKT-06-15, RA-08), también los de un lote que había fallado (QA-V4a-ROB-03) |
| Terminar confirmado con un lote en vuelo y, enseguida, segundo plano o cerrar la ventana | la sesión **cerrada** con todos sus puntos y el final, si la base terminó lo que tenía dentro del tope (QA-V4a-ROB-04); si no, cortada, como antes |
| Durante el cierre | o la sesión cortada (la transacción del cierre no llegó) o la cerrada completa: los puntos reintentados, el punto final y el cierre van juntos (DT4a-07) |
| Después de un cierre que falló | la sesión cortada, con los puntos que se guardaron mientras se remaba (AC-WKT-06-12) |
| Después del cierre | la sesión cerrada, idéntica a la del legacy (AC-WKT-06-07), con todos sus puntos |

Una sesión cortada es exactamente la que la It. 13 sabe reparar (F-HIS-21: "con puntos pero sin fin") y la que la It. 9 muestra con sus puntos. Al volver a abrir, la app no la toca ni ofrece retomarla (AC-WKT-06-10, paridad).

### 3.5 Dueño de la base

```csharp
namespace RowMate.Infrastructure.Database
{
    // La base local de la app: una RowMateDatabase (una conexión, un SerialExecutor) para rutinas y sesiones. AppRoot y las
    // pruebas la crean y la disponen al final: el ejecutor termina lo encolado y cierra la conexión en su hilo (It. 3 §2.6).
    public sealed class LocalStore : IDisposable
    {
        public LocalStore(RowMateDatabase database, IAppLogger logger);
        public RowMateDatabase Database { get; }
        public SqliteRoutineRepository Routines { get; }     // ownsDatabase: false
        public SqliteSessionRepository Sessions { get; }
        public void Dispose();
    }
}
```

`AppRoot.Awake`, paso 4b: `_store = new LocalStore(new RowMateDatabase(dbPath, new SerialExecutor("RowMate.Db", logger), logger), logger)` y `AppServices(_backend, _store.Routines, _store.Sessions, …)`. `OnDestroy` dispone el *store* después de la app y del *backend*, como hoy el repositorio. `TempDatabase.Store(logger, executor?, fileName?)` arma lo mismo sobre una carpeta temporal; `HostRoutines` lo usa para que rutinas y sesiones compartan el archivo en PlayMode y en el arnés.

### 3.6 Herramienta de la demo (solo lectura)

`tools/sessions/Program.cs` (proyecto `tools/dotnet/RowMate.SessionList`, net8.0), para la demo (c) y para QA (spec §12.9):

```
dotnet run --project tools/dotnet/RowMate.SessionList -- [--variant simulator|normal | --db <archivo>]
```

1. **Nunca abre el archivo original con SQLite.** Copia `rower_app_dev.db` (o el que se pida) y su `-wal`, si existe, a una carpeta temporal propia (`FileShare.ReadWrite`, sin bloquear a la app), abre la copia y la borra al terminar. Así ni el contenido ni la fecha de la base del usuario cambian, y el WAL de una sesión cortada se lee igual. Si la copia no se puede abrir o leer (la app escribía justo en ese instante), copia de nuevo, hasta dos veces.
2. Lista con `SqliteSessionRepository.ListAsync()`: `id · inicio (hora local) · fin (o "—") · rutina (o "libre") · puntos`, la más nueva arriba.
3. **Ninguna ruta** en la salida ni en los errores: solo el nombre del archivo (el repo es público y los informes copian la salida). La carpeta por defecto es la de la It. 3 (`LocalLow/iguisoft/RowMate`), calculada en el momento; nunca se escribe en el código.
4. **Los errores son textos fijos** (QA-G4aT-18). Los mensajes de las excepciones de E/S y de SQLite de .NET traen la ruta completa, así que nunca se imprimen: `Program` atrapa `FileNotFoundException`, `DirectoryNotFoundException`, `IOException`, `UnauthorizedAccessException` y la excepción de SQLite, y escribe uno de tres textos con **solo el nombre del archivo** (`[sessions] no se encontró rower_app_dev.db`; `[sessions] rower_app_dev.db está en uso o no se puede leer`; `[sessions] rower_app_dev.db no es una base de RowMate`), con código de salida 2. El detalle va al registro de depuración solo con `--verbose`, que tampoco imprime rutas (la excepción, por tipo).

Lo prueba `SessionListToolTests` (dotnet): la base de origen queda con el mismo SHA-256 y la misma fecha, también con un `-wal` presente; la salida no contiene la carpeta temporal. Además, con un archivo que falta y con uno bloqueado (la prueba lo abre con `FileShare.None`), la salida y el error estándar dicen el texto fijo y **no contienen** ni la carpeta de datos ni la temporal ni ningún separador de ruta delante del nombre (`SLT.MissingFileErrorHasNoPath`, `SLT.LockedFileErrorHasNoPath`).

### 3.7 Oráculo del motor legacy (S4-a, QA-P-23)

La sonda del Anexo A mostró que el `WorkoutProvider` del legacy corre en un proyecto de Flutter aparte, sin tocar el legacy. WP-A la vuelve herramienta:

- `tools/oracle/engine/pubspec.yaml` (depende del legacy por ruta relativa, `../../../../rowmate-legacy`; con `-LegacyPath`, `run-oracle.ps1` escribe un `pubspec_overrides.yaml` ignorado por git) y `test/engine_vectors_test.dart` (los dobles del remo y del pulsómetro de `test/workout_provider_hr_test.dart`, una `DatabaseService` que solo anota y `fake_async`).
- `run-oracle.ps1 -Engine` corre `flutter pub get --offline` y `flutter test` dentro de la misma guardia del legacy (estado, ignorados y metadatos antes y después) y deja `Assets/RowMate/Tests/Golden/oracle/workout_engine.json` (formato de `lib/golden.dart`, tope de 1 MiB).
- **Escenarios** (los de la spec §0.4 con `R-4`): libre terminado a los 12,3 s; `Corta`; `Corta` con `R-4(19)`; `Prueba 4a`; `Distancia 100` con `R-4`, `R-4R`, `R-4Z` y la pausa; pausa de Q-58; pausa del muestreo; sesión de 3 s. Cada uno con los puntos (sin `id` ni fechas), la fila de cierre y los resultados de objetivos.
- `WorkoutGoldenTests` corre el motor nuevo con las mismas lecturas y compara puntos, cierre y objetivos, uno por uno. La única excepción es Q-55 (DV-26): en `Distancia 100` con `R-4R`, `R-4Z` y la pausa, el oráculo tiene que dar la columna "Legacy" de la spec (segundos 285, 25 y 11, medidos en la sonda) y el motor nuevo la columna nueva (26, 27 y 25). Así queda probado que el cambio es solo ese.
- Regenerarlos necesita Flutter y la caché de `pub`; correr los tests, no (como el resto del oráculo).
- **Repo público** (QA-G4aT-05). `flutter pub get` y `flutter test` dejan en `tools/oracle/engine/` archivos con rutas absolutas de la caché de `pub` en el perfil del usuario (`.dart_tool/package_config.json` y `.flutter-plugins-dependencies`, con `file:///…` y la unidad), además de `pubspec.lock`, `build/` y el `pubspec_overrides.yaml` de `-LegacyPath` (también absoluto). WP-A suma al `.gitignore`, **antes de la primera corrida** de `run-oracle.ps1 -Engine`:

  ```
  # Oráculo del motor legacy (tools/oracle/engine, It. 4a): lo que dejan pub get y flutter test
  /tools/oracle/engine/.dart_tool/
  /tools/oracle/engine/build/
  /tools/oracle/engine/.flutter-plugins*
  /tools/oracle/engine/pubspec_overrides.yaml
  /tools/oracle/engine/pubspec.lock
  ```

  `run-oracle.ps1 -Engine` comprueba además, antes de `pub get`, que esas cinco líneas estén en el `.gitignore` (si no, aborta) y, al terminar, que `git status --porcelain --untracked-files=all tools/oracle/engine` no muestre ningún archivo fuera de `pubspec.yaml` y `test/` (lo que `pub get` deje y no esté ignorado hace fallar la corrida). El README del oráculo deja de decir "no hay `pubspec.yaml` ni `pub get`": el proyecto `engine/` es la excepción, con `pub get --offline` y la misma guardia del legacy. La prueba estática `RepoHygieneTests.ToolsHaveNoMachinePaths` (§8.2) recorre **todo** archivo de texto de `tools/` (no solo `.ps1` y `.sh`, como `ItThreeToolingTests.NoMachinePathsInScriptsOrManifests` de QA-G3-20), salvo lo que el `.gitignore` excluye, y falla con una ruta de la PC: la carpeta del usuario o la temporal (la expresión de QA-G3-20), `file:///` seguido de una unidad o de `/Users` o `/home`, y la caché de `pub` (`Pub/Cache`, `.pub-cache`). Comprueba también que cada exclusión que usa esté en el `.gitignore`, así las dos listas no se separan.

---

## 4. Segundo plano (Q-91) y pantalla encendida

### 4.1 El programador pasa a Core: `FrameScheduler` y el alcance

```csharp
namespace RowMate.Core.Common
{
    // El IScheduler de la app, puro (ADR 005; ADR 017, decisión 3). Quien lo avanza le pasa la hora: UnityScheduler (Platform)
    // en cada cuadro, con el Stopwatch y DateTimeOffset.UtcNow; las pruebas, con números. Es el mismo código en los dos mundos.
    public sealed class FrameScheduler : IScheduler
    {
        public FrameScheduler(double monotonicNow, DateTimeOffset utcNow, TimeSpan localOffset, IAppLogger logger);
        public DateTimeOffset UtcNow { get; }              // durante el alcance, la hora del instante virtual
        public DateTimeOffset LocalNow { get; }
        public double MonotonicSeconds { get; }            // ídem
        public bool IsCatchingUp { get; }
        public bool InBackground { get; }
        public int Pending { get; }

        // Un cuadro en primer plano: RealtimeTimerQueue.RunDue, la conducta de la It. 2 (QA-G2-15): a lo sumo un disparo de
        // cada periódico y, tras un atraso de un período o más, se reancla en ahora + período.
        public int Tick(double monotonicNow, DateTimeOffset utcNow, TimeSpan localOffset);

        // La app pasa a segundo plano (Android e iOS: el loop se detiene). Solo anota: los timers quedan como están.
        public void EnterBackground();

        // Vuelve: alcance (RealtimeTimerQueue.CatchUp) desde el último cuadro hasta ahora, en tiempo virtual, con IsCatchingUp.
        public int ResumeFromBackground(double monotonicNow, DateTimeOffset utcNow, TimeSpan localOffset);

        public IDisposable Schedule(TimeSpan delay, Action callback);
        public IDisposable SchedulePeriodic(TimeSpan period, Action callback);
        public Task Delay(TimeSpan delay, CancellationToken ct = default);
    }
}
```

- **`RealtimeTimerQueue.CatchUp(from, to, setNow, maxCallbacks)`** (nuevo): repite "el timer vivo de vencimiento más chico ≤ `to`" (empate: orden de alta), pone el reloj virtual en su vencimiento y lo dispara. Un periódico vuelve a vencer en vencimiento + período, **sin reanclar**; uno de una vez se va. A diferencia de `RunDue`, lo que se agenda **durante** el alcance también corre si vence antes de `to`: el timer del motor que se rearma cada segundo y las lecturas del remo simulado se intercalan en orden. Con más de `maxCallbacks` (un millón, como `FakeScheduler`), corta, lo registra como `Error` (`TIMER`) y reancla lo que queda en `to` + período. Al terminar, el reloj queda en `to`.
- **Hora virtual.** `MonotonicSeconds` = el vencimiento en curso; `UtcNow` = hora de la vuelta − (ahora − virtual); `LocalNow` = `UtcNow` con el *offset* de la vuelta.
- **La hora es la del cuadro** (03-qa-harmony-tecnica §5). En primer plano, `MonotonicSeconds`, `UtcNow` y `LocalNow` son los que fijó el último `Tick`, no los del instante en que se leen (hoy `UnityScheduler` lee el `Stopwatch` en cada llamada). Para el motor, `MotionDriver`, `FramePacer` y la barra de título es mejor: todo el cuadro ve la misma hora. Ninguna medición de rendimiento usa el programador: usan el `Stopwatch` (QA-G3-21).
- **Volver sin haberse ido.** Unity llama una vez a `OnApplicationPause(false)` al arrancar, después de `Awake`: `ResumeFromBackground` sin un `EnterBackground` antes no hace nada (ni alcance ni cambio de hora). Lo prueba `FSC.ResumeWithoutEnterIsANoOp`.
- **Topes de espera: `ForegroundTimeout`** (QA-G4aT-03; spec §12 punto 2). `CatchUp` dispara todo lo que vence antes de `to`, también lo que se agenda durante el alcance; es lo correcto para el motor y el remo, pero no para un tope que espera algo de **afuera** (la base, en su hilo): la continuación de esa espera vuelve al hilo principal recién después del alcance, así que un tope en tiempo virtual vencería antes de que pueda volver. Un tope de espera mide solo tiempo de primer plano:

  ```csharp
  namespace RowMate.Core.Common
  {
      // Tope de espera que no cuenta el tiempo en segundo plano. Con InBackground = false corre como un Schedule; al pasar a
      // true se detiene y guarda lo que le queda; al volver a false (en la app, DESPUÉS del alcance: §4.2) se rearma con eso
      // desde la hora de la vuelta. Creado durante el segundo plano (por ejemplo, el cierre de una rutina que termina en el
      // alcance), arranca entero al volver. Hilo principal; Dispose lo cancela (la espera terminó antes).
      public sealed class ForegroundTimeout : IDisposable
      {
          public ForegroundTimeout(IScheduler scheduler, IAppLifecycle lifecycle, TimeSpan timeout, Action onTimeout);
          public bool HasFired { get; }
          public TimeSpan Remaining { get; }
          public void Dispose();
      }
  }
  ```

  El cierre de la sesión lo usa en lugar de `Task.WhenAny(cierre, scheduler.Delay(15 s))` (§2.9). Con `Corta`, pantalla bloqueada de los 8 a los 330 s: la rutina termina en el segundo virtual 30 dentro del alcance, el tope se crea en segundo plano y arranca a los 330 s; el cierre real (que la base hizo en su hilo) vuelve en el cuadro siguiente y cancela el tope: `Ended(Completed, true)` y la sesión pasa a ser la última terminada (AC-WKT-02-06, -06-11). Con `FakeScheduler` y `FakeAppLifecycle` vale lo mismo: lo prueban `FTO.*` y `WBG.LongBackgroundAfterTheEndStillSaves` (§8.2).
- **El reloj monótono en Android (RA-19; R4a-12).** `UnityScheduler` le pasa a `FrameScheduler` la hora del `Stopwatch`, que en Android es `CLOCK_MONOTONIC`: **no cuenta el sueño profundo** con la pantalla bloqueada. Al volver, el alcance avanzaría menos que la hora de pared y el reloj del HUD, los pasos y los puntos quedarían corridos (AC-WKT-02-10: `2:00 ± 1 s`). ADR 017 deja la elección de la fuente para la 10a, midiendo el legacy en el mismo teléfono (`elapsedRealtime`, que cuenta el sueño, contra `uptimeMillis`). Si cambia, cambia solo la fuente que `UnityScheduler` le pasa a `FrameScheduler` (una función por plataforma); el programador y el motor no. En la 4a, `FrameScheduler` tolera la diferencia: con un salto monótono menor que el de UTC el alcance cubre el salto monótono, todo dispara en orden y la hora virtual nunca retrocede (`FSC.ResumeWithASmallerMonotonicJumpKeepsOrder`).
- **`UnityScheduler`** (Platform) queda como un adaptador: arma el `FrameScheduler` con el `Stopwatch`, le pasa `Tick` en cada `Update` y expone `EnterBackground()` y `ResumeFromBackground()`, que llama `AppRoot` (§4.2). Su conducta en primer plano no cambia.
- **Por qué alcanza.** El motor procesa los segundos por tiempo monótono (§2.2) y el remo simulado emite por un periódico del mismo programador: en el alcance, cada segundo del motor encuentra la lectura de su instante. El remo real de la It. 10a guarda sus eventos con la marca monótona nativa (ADR 017, decisión 2); al volver, `NativeEventPump` los agenda como timers de una vez en esa marca, **antes** del alcance, y quedan intercalados por tiempo sin cambiar esta pieza.
- **Un cuadro tarde en primer plano** (arrastrar la ventana en Windows, AC-WKT-02-04): se queda con la conducta de la It. 2. El motor procesa en ese cuadro todos sus segundos vencidos, con la lectura que había (el simulador tampoco emitió durante el bloqueo). No se pierden ni se duplican segundos (DT4a-10). Condición que QA puso para aceptar DT4a-10 (03-qa-harmony-tecnica §6, §13): con el remo **real** (10a), la lectura que llega después de un cuadro tarde trae un salto de más de 10 m en un segundo, que el saneamiento descarta: se pierden metros del paso por distancia y de los parciales. La 10a lo mide y decide si el bombeo de eventos nativos usa el alcance también con cuadros tarde. Con el remo simulado no pasa (emite un incremento fijo por disparo).

### 4.2 Ciclo de vida

```csharp
namespace RowMate.Core.Ports
{
    // Ciclo de vida (ARCHITECTURE §13; ADR 017). Segundo plano = Android o iOS con la actividad pausada. En escritorio, la
    // ventana minimizada o sin foco NO es segundo plano: el loop sigue (runInBackground). El foco es el de la ventana (en
    // Windows, minimizar también lo saca); "el usuario la ve" = adelante y con foco (spec §0.4, §12 punto 13).
    public interface IAppLifecycle
    {
        IReadOnlyObservable<bool> InBackground { get; }
        IReadOnlyObservable<bool> HasFocus { get; }
        bool IsUserFacing { get; }                       // !InBackground.Value && HasFocus.Value
    }
}
// Core/Common/ForegroundLifecycle.Instance: siempre adelante y con foco (dotnet, pruebas de pantalla que no lo usan).
// Platform/Lifecycle/AppLifecycle: dos ObservableValue<bool> que mueve AppRoot.
```

`AppRoot`:

| Callback | Orden | Por qué |
|---|---|---|
| `Awake`, paso 3b | `_keepAwake = new ScreenKeepAwake(wakeLock, _lifecycle, logger)` y `_keepAwake.Start()` (el primer pedido), antes de armar la UI | AC-APP-14-01: desde el arranque |
| `OnApplicationPause(true)` | `_lifecycle.SetBackground(true)` (el motor manda lo pendiente y espera hasta 1 s) → `_scheduler.EnterBackground()` | AC-WKT-06-15: lo pendiente queda escrito **antes** de que el sistema congele la app |
| `OnApplicationPause(false)` | `_scheduler.ResumeFromBackground()` (alcance; un no-op si no hubo `EnterBackground`, como la llamada del arranque) → `_lifecycle.SetBackground(false)`: el motor publica su foto con `Instant` (§2.10), los topes de espera vuelven a correr (§4.1), `ScreenKeepAwake` vuelve a pedir la pantalla y, si además hay foco, `WorkoutStore` muestra el aviso retenido | AC-WKT-02-05 a -08; AC-APP-14-02 |
| `OnApplicationFocus(f)` | lo de hoy (idioma, accesibilidad, barra de título) + `_lifecycle.SetFocus(f)`; con `f = true`, `ScreenKeepAwake` vuelve a pedir y `WorkoutStore` muestra el aviso retenido | ARCHITECTURE §13; AC-APP-14-02; AC-WKT-02-09 |
| `OnDestroy` | shell → app (el motor deja cortada una sesión en curso) → *backend* → `LocalStore` (el ejecutor termina lo encolado) → superficie → `_keepAwake.Release()` | AC-APP-14-03; P-03 |

La regla de cuándo se vuelve a pedir la pantalla vive en `ScreenKeepAwake` (Core), que escucha el ciclo de vida: `AppRoot` solo la arma y la libera. Así AC-APP-14-02 se prueba en dotnet con `FakeAppLifecycle` y `FakeWakeLock` (`SKA.RequestsAgainOnResumeAndFocus`; QA-G4aT-09).

### 4.3 Por plataforma

| Plataforma | Qué hace Unity | Qué hace la app | Verificación |
|---|---|---|---|
| Windows (build y Editor) | `runInBackground = true` (ya lo aplica `PlayerSettingsStep`): minimizada o sin foco, el loop sigue | nada especial: reloj, simulador, puntos y pasos siguen en tiempo real; la pantalla sigue pedida (es del hilo principal, que vive todo el proceso) | [Device-Win] AC-WKT-02-09, AC-APP-14-02 |
| Android | la actividad pausa el loop; el proceso y los hilos (el de la base también) siguen vivos | al pausar, lo pendiente se escribe y se espera; al volver, alcance. Si el sistema mata el proceso, la sesión queda cortada con los puntos hasta la pausa (RA-08). El `Stopwatch` no cuenta el sueño profundo (RA-19, R4a-12: la fuente se elige en la 10a) | [dotnet] AC-WKT-02-05 a -07, AC-WKT-06-15; [Device-And] AC-WKT-02-08 (el primer cuadro) y AC-WKT-02-10: NO VERIFICABLE sin módulo y teléfono |
| iOS, macOS | iOS como Android; macOS como Windows | el mismo código; `IWakeLock` de Apple en la 10b | [Apple] 10b |

El tope de 10 fps con la ventana minimizada (ADR 017, decisión 6) se deja para la It. 6a, cuando haya escena que dejar de dibujar (DT4a-10): en la 4a la ventana minimizada sigue con la política de 60/30 fps de la It. 2.

### 4.4 Pantalla encendida (F-APP-14, Q-56)

```csharp
namespace RowMate.Core.Ports
{
    public interface IWakeLock
    {
        bool Enable();     // true = el sistema aceptó el pedido
        void Disable();
    }
}

namespace RowMate.Core.Common
{
    // Pedida al arrancar y otra vez al volver (foco o fin de la pausa); liberada al cerrar (lib/main.dart:42 la pide una vez y
    // no la suelta: el proceso que termina la libera). Un rechazo no se le muestra al usuario (paridad) y se registra UNA vez
    // como Warning: "[APP] pantalla encendida: el sistema la rechazó" (AC-APP-14-04).
    // Escucha el ciclo de vida: vuelve a pedir cuando InBackground pasa a false y cuando HasFocus pasa a true
    // (AC-APP-14-02). Después de Release no vuelve a pedir.
    public sealed class ScreenKeepAwake : IDisposable
    {
        public ScreenKeepAwake(IWakeLock wakeLock, IAppLifecycle lifecycle, IAppLogger logger);
        public bool IsHeld { get; }
        public void Start();          // el primer pedido (AppRoot.Awake)
        public void Request();
        public void Release();
        public void Dispose();        // = Release y se desuscribe
    }
}
```

| Plataforma | Implementación (Platform/Power) | Legacy |
|---|---|---|
| Windows (build y Editor de Windows) | `WindowsWakeLock`: `SetThreadExecutionState(ES_CONTINUOUS \| ES_DISPLAY_REQUIRED \| ES_SYSTEM_REQUIRED)` por P/Invoke a `kernel32.dll`, **desde el hilo principal** (el pedido es del hilo que lo hace; el de Unity vive todo el proceso, así vale también minimizada). `Disable`: `SetThreadExecutionState(ES_CONTINUOUS)`. Devuelve 0 = rechazo | `wakelock_plus_windows_plugin.dart:23` pide `ES_CONTINUOUS \| ES_DISPLAY_REQUIRED`; ARCHITECTURE §13 suma `ES_SYSTEM_REQUIRED` para que tampoco se suspenda el equipo (AC-APP-14-01; DT4a-09). `powercfg /requests` muestra `RowMate.exe` en `DISPLAY` y en `SYSTEM` |
| Android | `ScreenSleepWakeLock`: `Screen.sleepTimeout = SleepTimeout.NeverSleep` (Unity pone `FLAG_KEEP_SCREEN_ON` en la ventana de la actividad); `Disable`: `SleepTimeout.SystemSetting` | `Wakelock.kt:24` (`FLAG_KEEP_SCREEN_ON`): vale mientras la app está adelante |
| iOS | la misma `ScreenSleepWakeLock` (`idleTimerDisabled`) | 10b |
| macOS | `NoWakeLock` (rechaza: queda el aviso del registro) hasta la 10b (`IOPMAssertion` en el bundle Swift) | 10b |

En las pruebas no hay pedido real: `UiTestHost` no pasa por `AppRoot` y `BootSceneTests` lo pide y lo libera en su `TearDown`. `FakeWakeLock` (Tests/Support) cuenta pedidos y simula el rechazo. Con esto termina DV-08 de la It. 2.

`SetThreadExecutionState` es del hilo que lo pide: la liberación desde `AppDomain.DomainUnload` (R4a-07) sirve solo si corre en ese mismo hilo (en el Editor, el principal). `WindowsWakeLock` guarda el id del hilo del pedido; un `Disable` desde otro hilo no llama al sistema (no liberaría nada) y lo anota como `Warning`. `WLK.ReleaseRunsOnTheRequestingThread` lo comprueba en el Editor en lugar de suponerlo (03-qa-harmony-tecnica §6).

### 4.5 Ciclo de vida de prueba (dotnet)

```csharp
namespace RowMate.Tests.Support
{
    // "Ciclo de vida de prueba" de la spec §0.4: el FrameScheduler de la app con cuadros a 60 fps y un FakeAppLifecycle.
    // El reloj monótono 0 es el instante −0,5 s (la primera lectura de R-4); Instant(k) = k + 0,5.
    public sealed class LifecycleHarness
    {
        public LifecycleHarness(DateTimeOffset sessionStartUtc, double fps = 60);   // 2026-10-28T13:00:00.000Z en el instante 0
        public FrameScheduler Scheduler { get; }
        public FakeAppLifecycle Lifecycle { get; }   // sin pedido: adelante y con foco (spec §0.4)
        public static double Instant(double k);
        public void RunUntil(double instant);        // cuadros cada 1/fps hasta ese instante
        public void Stall(double seconds);           // un cuadro que llega tarde (AC-WKT-02-04)
        public void Background(double instant);      // corre hasta ahí, InBackground = true, EnterBackground; sin cuadros
        public void Foreground(double instant);      // ResumeFromBackground en ese instante; InBackground = false
        public void Unfocus(double instant);         // corre hasta ahí y saca el foco (Windows minimizada: el loop sigue)
        public void Focus(double instant);           // corre hasta ahí y devuelve el foco
        public void ResumeWithMonotonicJump(double monotonicSeconds, double utcSeconds);   // RA-19: salto monótono ≠ UTC
    }

    // Remo de prueba (spec §0.4): L(k) en el instante k − 0,5 por un timer del programador, también en pausa y en segundo
    // plano (en segundo plano queda pendiente y llega en el alcance, como el buffer nativo de la 10a). Estado Connected.
    public sealed class ScriptedRower : IRowerService
    {
        public ScriptedRower(IScheduler scheduler, Func<int, RowingData> reading, IAppLogger logger);
        public void Disconnect(); public void Connect();            // AC-WKT-03-10, F-BLE-10
    }

    // R4(k): tiempo 300 + k, paladas 100 + k, distancia 1000 + 4k, split 125, 150 W, 24.0 spm, kcal 40 + ⌊k/10⌋, pulso 0.
    // R4Spm(s), R4R (desde L(10) distancia 4(k − 10)), R4Z (L(10) con distancia 0), R4P (parado), Long (CAP-16), Extremes
    // (AC-WKT-17-10).
    public static class RowerScripts { /* … */ }
}
```

Con `FakeScheduler` (sin cuadros) el alcance es implícito: `Advance` ya dispara todo en orden. `LifecycleHarness` existe para probar el camino de la app (un disparo por cuadro y el alcance al volver) con el mismo `FrameScheduler` que corre en Unity.

---

## 5. R3-08: una rutina que no se puede iniciar

```csharp
namespace RowMate.Core.Workout
{
    // R3-08 (03-rutinas/02-tech-design §8.2), AC-WKT-12-08, AC-RUT-06-08. Sin expandir y sin bucles por repetición.
    public static class WorkoutStartRule
    {
        // La validación 3 del editor (AC-RUT-11-05: los topes de Q-92 por campo y por repetición, ×1000 o más fuera de rango,
        // ×0 y negativos sin controlar) y que la rutina expandida tenga al menos un paso (Total con saturación).
        public static bool CanStart(Routine template)
            => StepLimitCheck.TemplateIsWithinLimits(template.Steps)
               && RoutineStructure.Segments(template.Steps, 0).Total > 0;
    }
}
```

| Rutina (F-RARA, cada una en su base) | `CanStart` | Por qué | En el legacy |
|---|---|---|---|
| `Serie enorme` (×2 000 000 000) | no | más de 999 repeticiones | expande 2 000 millones de pasos y se traba |
| `Vatios imposibles` (mín. 50 000) | no | fuera del tope de vatios | arranca |
| `Serie ×0` | no | expandida no tiene pasos | el inmersivo falla (no hay paso 0, `:100`) |
| `Sprint` | — | no llega: la carga de la colección falla (AC-WKT-12-06) | la carga lanza: Workout muestra `Creá una rutina en la pestaña "Rutinas"` (DV-23) y Rutinas, el indicador para siempre (`workout_screen.dart:129-138`; `routines_screen.dart:24-26`) |
| `Split negativo enorme` (split guardado −2 147 483 000, progresión −5 999, ×2; WSR la arma en su base) | sí | la regla no controla un split ≤ 0 ("sin split", AC-RUT-13-08) y AC-WKT-12-08 pide sus mismas excepciones; el motor satura la progresión (DT4a-16) y arranca | arranca (enteros de 64 bits); Split siempre fuera |
| F-LISTA, F-4A, F-BORDE | sí | — | — |

- **Cuándo se mira.** Al tocar la tarjeta (`WorkoutStore.TapRoutine`): si no se puede, aviso `workoutRoutineCannotStart` y **ni diálogo ni sesión** (P-09). El motor lo vuelve a mirar en `StartWithRoutine` (otra entrada en la 4b o la It. 12 no puede saltearlo). `Flatten` nunca recibe una rutina que no pasó la regla (spec §12.5).
- **Costo.** Las dos funciones recorren la plantilla una vez, con la forma cerrada de `StepLimits.FirstRepetitionOutOfRange`: con ×2 000 000 000 el tiempo es el de una rutina de dos pasos. `WSR.HugeSeriesIsDecidedFastAndWithoutMemory` mide menos de 50 ms y menos de 64 KB reservados (`GC.GetAllocatedBytesForCurrentThread`).
- La rutina sigue en la lista, igual que en Rutinas (AC-RUT-10-11): solo no se inicia.
- **Desborde (QA-G4aT-11).** "Ninguna cuenta desborda" valía para los valores que la regla controla, no para un split guardado ≤ 0: `StepLimitCheck` lo trata como "sin split" (`StepLimitCheck.cs:111-114`) y `RoutineExpansion.Apply` lo progresaba con `checked` (`RoutineExpansion.cs:115`), así que `Split negativo enorme` pasaba la regla y lanzaba `OverflowException` en `StartWithRoutine`. De las dos salidas que propuso QA se elige **saturar en el camino del motor** (`Flatten(…, ProgressionOverflow.Saturate)`) y no agregar una tercera condición a `CanStart`: AC-WKT-12-08 fija la regla ("la validación 3 de AC-RUT-11-05, con sus mismas excepciones") y esa rutina el editor la deja guardar. Solo los valores sin tope pueden saturar (el split ≤ 0 con una progresión negativa: por debajo de −2 141 496 646); el objetivo queda en `int.MinValue` y Split está siempre fuera, como en el legacy. Lo único que puede diferir es el texto de un objetivo así, que en los dos es una cuenta sin sentido. `WSR.HugeNegativeSplitStartsWithoutOverflow` lo prueba; `Flatten(template)` sigue con `checked` para la It. 1.

---

## 6. UI (WP-B, UI Toolkit)

Valen las reglas de las It. 2 y 3: vistas en C# sin UXML, nombres y clases de contrato, binding manual (`ViewBinding.Bind`), nada de LINQ ni *closures* nuevas en caminos por cuadro, colores solo de `Tokens` (con transparencia, por código), lo que sale no responde (`ExitLock`). Las vistas leen estados ya armados (`WorkoutRestState`, `WorkoutHudState`) y reenvían gestos.

### 6.1 Workout en reposo

```
#screen-workout .rm-screen .rm-workout                                       (WorkoutScreenView)
├── #workout-appbar .rm-appbar > .rm-appbar__title                          workoutTitle (title 20/600), sin acciones (AC-WKT-12-01); SOLO en
│                                                                           reposo: fuera de reposo, display: none (AC-WKT-01-09, paridad)
├── #workout-rest .rm-workout__rest                                         solo con la sesión en reposo
│   └── #workout-scroll (ScrollView, ScrollPolicy, FocusScroller) > .rm-workout__content    16 de margen; TODO se desplaza junto (DV-22)
│       ├── #workout-banner .rm-workout__banner                             sin remo: warning 18 #FF9800 (alineado con la 1.ª línea) + 8 +
│       │                                                                   workoutRowerNotConnected 13; crece y colapsa (GrowMotion, CollapseMotion)
│       ├── #workout-free .rm-button --primary --tall                       play_arrow 18 + 8 + workoutFree; ancho completo, min-height 56, r.l 12
│       ├── .rm-workout__gap                                                24
│       ├── #workout-routines-label .rm-workout__label                      workoutOrPickRoutine (con sus dos espacios), 8 abajo, solo con rutinas
│       ├── #workout-loading .rm-workout__state > Spinner(32, 3)           cargando, después de 120 ms de gracia, con fundido de 160; el
│       │                                                                   Spinner gira solo a la vista (SetShown; ronda 1 de verificación)
│       ├── #workout-error .rm-workout__state                               error 48 + 12 + routinesLoadError + 16 + #workout-retry --tonal (refresh)
│       ├── #workout-empty .rm-workout__empty                               workoutNoRoutines, text.tertiary (54 %), ancho máximo 320
│       │                                                                   (los tres estados: flex-grow 1, centrados en lo que queda debajo del botón)
│       └── #workout-routines .rm-workout__list [--disabled]                #workout-routine-<n>: RoutineCardView(trailing: Chevron), 10 entre tarjetas
└── #workout-in-progress .rm-workout__in-progress                           fuera de reposo: workoutInProgress, centrado entre el borde de arriba
                                                                            y la barra de pestañas, text.tertiary (DV-36); sin barra superior
```

Las medidas, colores y estados son los de 04-design-notes §4.1 (DN4a-08, DN4a-19). El *chevron* de la tarjeta va en `.rm-routine-card__chevron` **[nuevo]**: una caja de 48 × 48 en la misma columna de 28 dp del borde derecho que el `⋮` de Rutinas (así la columna de texto mide igual), con `chevron_right` 18 en `text.tertiary`, `align-self: center` y `pickingMode = Ignore` (la tarjeta entera es la acción).

- **Estados** (`WorkoutRestState`, AC-WKT-12-02 a -06, DV-23): con `RoutinesPhase.Loading`, solo el indicador (nunca `workoutNoRoutines`); con `LoadFailed`, el error y `Reintentar` (`RoutinesStore.LoadAsync()`, que recarga para Workout y Rutinas a la vez); con `Ready` y sin rutinas, el vacío; con rutinas, la etiqueta y la lista. `Entrenamiento libre` no depende de ninguno.
- **Sin remo** (AC-BLE-10-03 a -05; con **cualquier** estado distinto de conectado, también `Off`: §2.11): el aviso arriba; `Entrenamiento libre` deshabilitado (`RmButton.SetEnabled(false)`, 0,38) y `.rm-workout__list--disabled`: la lista al 50 % (opacidad por código, [CONTRATO]) y sin eventos (`pickingMode` de cada tarjeta en `Ignore`, `focusable` en `false`). Sigue al remo en vivo: el *store* emite con cada cambio de `DeviceStore`.
- **"En curso"** (AC-WKT-01-09; QA-G4a-P09, QA-G4aT-17): fuera de reposo, `#workout-appbar` y `#workout-rest` van en `display: none` y queda solo `workoutInProgress`, como el `_ActiveView` del legacy, un `Scaffold` sin `AppBar` (`workout_screen.dart:180-191`). Se ve solo durante los fundidos del inmersivo (240 y 200 ms). Al volver a reposo, la barra vuelve en el mismo cuadro que la vista.
- **Lista** (AC-WKT-12-04, DV-21): `WorkoutRoutineListView` arma una `RoutineCardView` por rutina, en el orden de la colección, reusando las tarjetas por `Routine.Id`; cada una con `RoutineCardTrailing.Chevron` (`chevron_right` 18 `text.tertiary`, sin `⋮`) y se toca entera. Los nombres `workout-routine-<n>` se renumeran después de cada cambio. Sin animaciones de alta y baja (los cambios llegan desde otra pestaña); la primera carga entra con la pestaña (ANIMATION_BIBLE §2.3).
- **Tocar una tarjeta** → `WorkoutStore.TapRoutine`: `CannotStart` → aviso; `Confirm` → `StartWorkoutDialog` (§6.2); `Ignored` → nada.
- **Un toque, una sesión** (AC-WKT-01-10, DV-30): `StartFree()` y `StartRoutine()` devuelven `false` si el motor ya no está en reposo; la vista deja de responder desde el primer toque y el diálogo, desde que se confirma (`ExitLock`). El inmersivo **no se abre por el toque sino por el estado**: la vista observa `WorkoutEngine.State` y, cuando la fase deja `Idle` y no hay un `ImmersiveWorkoutPage` en la pila **que no esté saliendo**, lo apila (`PageStack.Push` ignora además un segundo `Push` durante la entrada, DT-18). La página que sale no cuenta (`WorkoutScreenView.Immersive` pide `!Pages.IsExiting`): una sesión que empieza durante su fundido de salida (200 ms, 120 con reducir movimiento; la vista de reposo ya responde debajo por `ExitLock`) apila su propio inmersivo en el mismo toque, como el legacy, que apila una ruta por inicio (QA-V4a-01 = QA-V4a-ROB-01; `WSC.StartDuringThePreviousExitHasItsOwnImmersive`).
- **Desplazamiento** (AC-WKT-12-09): el relleno de abajo del contenido es el de la It. 3 (`ShellLayout.RegisterClearance`: barra + el obstáculo más alto que se cruza + 16; con D-ART-10 la llave está en el carril izquierdo y cuenta). El vacío se centra en lo que queda: el contenido toma de `min-height` el alto del visor (por código, en `GeometryChangedEvent`) y el vacío crece. `display: none` al cambiar de pestaña o durante el inmersivo conserva la posición. En el teléfono, sin barra fija (`ScrollPolicy`).
- **Letra 1,3×** (AC-APP-05-05): todo con las clases `.rm-type-*`; lo que no entra se desplaza.

### 6.2 Diálogo de inicio

`StartWorkoutDialog : ModalDialog<bool>` (`workout-start-dialog`, ancho máximo 400 como el de eliminar), AC-WKT-12-07:

- Título = el **nombre de la rutina tal cual** (`enableRichText = false`, `parseEscapeSequences = false`, `white-space: pre-wrap`, como en la tarjeta); `workoutStartTitle` sigue sin usarse (paridad).
- Contenido = `RoutineSummary.StartDialogContent(rutina, l10n)` (`workoutStartContent`: `{summary}⏎⏎¿Iniciar el entrenamiento?`).
- Acciones: `#dialog-cancel` (`cancel`, de texto) y `#dialog-confirm` (`workoutStart`, primario relleno).
- `Cancelar`, tocar afuera, Escape y "Atrás" cierran sin iniciar (DialogLayer, It. 3). Al confirmar, `WorkoutStore.StartRoutine(rutina)` sin mirar la conexión (AC-BLE-10-06). Dos toques en `Iniciar` dan una sesión: el diálogo se cierra con el primero y su velo se traga el segundo (DT-21).

### 6.3 El inmersivo

`ImmersiveWorkoutPage : IPage` (`#immersive .rm-page .rm-immersive`):

```
#immersive .rm-page .rm-immersive                       pantalla completa, recibe eventos; tapa la barra de pestañas hasta que sale
├── #hud-backdrop .rm-hud-backdrop                      fondo provisorio (AC-SCN-01-01): cielo y agua con el corte en el horizonte,
│                                                       un solo elemento con la textura de BackdropTexture (DN4a-18); Ignore
├── #hud-boat-zone .rm-hud-boat-zone                    contorno de depuración de la zona del bote, 2 dp dev.simulator (solo si lo
│                                                       pide el anfitrión); pickingMode Ignore
├── #hud-topbar  (zona, ancla arriba a la izquierda)    StepInfoBar (con rutina) o FreeTopBar (libre)
├── #hud-zone-spm (ancla arriba a la izquierda)         #hud-spm
├── #hud-zone-right (ancla arriba a la derecha)         #hud-split, #hud-watts
├── #hud-zone-vitals (ancla según la orientación)       #hud-kcal, #hud-bpm
├── #hud-band (ancla abajo a la izquierda)              #hud-time │ #hud-distance
└── #hud-controls (ancla abajo al centro)               #hud-finish, #hud-pause | #hud-resume
```

- **Se abre en el acto** (AC-WKT-13-01, AC-APP-03-09): `PageStack.Push(page, PageTransitionKind.Fade)`; el motor ya está en `Active` desde el toque y ninguna transición lo demora. Entrada: opacidad 0 → 1 en 240 (`ease.enter`); salida: 1 → 0 en 200 (`ease.exit`); con reducir movimiento, fundido de 120 lineal (DN4a-05, aceptado; la "sumergirse" es de la 4b). Al terminar la entrada, pestañas y barra pasan a `display: none` (It. 3). Al salir, `CoversShellChanged(false)` va en el **cuadro 0** de la salida: pestañas y barra vuelven debajo de la página que funde, y ahí entra el aviso del fin (04-design-notes §5.1). Si la sesión vuelve a reposo antes de que termine la entrada, la salida arranca desde la opacidad que tiene.
- **Se cierra solo** (AC-WKT-13-04): cuando `WorkoutHudState.Phase` vuelve a `Idle`, la página hace `Pop` una vez. Nunca queda el inmersivo en reposo ni Workout en reposo con una sesión: si la fase no es `Idle`, la pestaña muestra `workoutInProgress` y la vista vuelve a apilar el inmersivo si faltara (§6.1).
- **Nada queda huérfano** (AC-WKT-20-07, AC-WKT-13-04; QA-G4a-P02, QA-G4aT-04). `DialogLayer` está arriba de las páginas y no depende de `PageStack` (`DialogLayer.cs:8`), así que la página lleva la cuenta de sus diálogos: en el primer estado con la fase fuera de `Active` y `Paused` (`Finished` o, después de un segundo plano, ya `Idle`), **antes** del `Pop`, llama `Cancel()` del `FinishWorkoutDialog` abierto. Se cierra como con `Cancelar`, con su salida y sin responder mientras sale (`ExitLock`). Un `Terminar` del diálogo que llegue después no cambia nada: `FinishAsync` fuera de `Active` y `Paused` no hace nada, y la sesión cuenta como terminada sola. Un toque que llegó antes (con la fase en marcha) es un terminar a mano (AC-WKT-20-04). Vale igual si la rutina terminó durante el alcance: la primera foto de la vuelta ya trae `Finished` y la página cancela el diálogo en ese cuadro. El panel del simulador no se cierra: sigue a la llave a su lugar nuevo (§6.10).
- **"Atrás" y Escape** (AC-WKT-13-03): `OnBackRequested()` (Android) abre la confirmación de terminar si los controles responden. Escape sin diálogo no hace nada: es la regla de la It. 3 (`BackNavigation`, P-08); con el diálogo abierto, lo cancela.
- **Sin el nombre de la rutina** (AC-WKT-13-05). Las barras del sistema de Android siguen visibles (4b).
- **Mientras está arriba:** pide 60 fps (`FramePacingPolicy.Acquire` en `OnPushed`, `Release` en `OnPopped`); registra sus controles como obstáculo inferior (para los avisos, §6.9) y declara su superficie para la llave, con el lugar que calcula `HudGeometry` (§6.10); dispone su `WorkoutHudStore` en `OnPopped`, después de cancelar sus diálogos. `HudView.Dispose` saca del `MotionDriver` las animaciones continuas del HUD (el latido de BPM y el pulso de la fila del paso), que `Settle` no espera y nadie más paraba; la foto de reposo, sin paso, ya apaga el pulso (`StepPulse.Release`) mientras la página sale (QA-V4a-02 = QA-V4a-ROB-02; `IHT.LeavingTheImmersiveStopsItsContinuousMotions`).

### 6.4 Geometría del HUD

**Escala por zona.** Cada zona es un elemento absoluto con `transform-origin` en su ancla y `scale = s`; su contenido se arma a **tamaño base** (los dp del legacy y de ART_BIBLE §3.2). El código pone solo el ancla × `s`, la escala y los topes de ancho; los tamaños base son contrato (§9.5). Es lo que pide ART_BIBLE §5.3 ("tipografía, padding, radios y posiciones × `hudScale`"): también los bordes de 1 dp pasan a `s` dp, como toda medida del legacy (spec §0.3). Los textos del HUD llevan su tamaño **en línea**, así no siguen la letra del sistema (AC-WKT-17-01; ART_BIBLE §10.4).

```csharp
namespace RowMate.Core.Design
{
    // ART_BIBLE §5.2-§5.3; immersive_workout_screen.dart:91, :612-613, :640-641, :652, :672, :676, :693, :710-711, :112,
    // :902, :920; 04-design-notes §3 (DN4a-01, DN4a-02, DN4a-04, DN4a-13, DN4a-14). Las constantes de geometría de
    // 04-design-notes §5.13 viven acá (las de tiempo, en MotionSpecs).
    public static class HudGeometry
    {
        public const double ScaleShortSide = 390, MinScale = 0.9, MaxScale = 1.6;
        public const double TopBarTop = 8, TopBarSide = 12, TopBarHeight = 40, TopBarRowHeight = 22, TopBarPadV = 8;
        public const double StepRowPadH = 12, FreeRowPadH = 14;
        public const double CardsTop = 90, Side = 14, CardGap = 8, SmallCardMinWidth = 96;
        public const double PortraitBottom = 140, ControlsBottom = 16, ControlsSidePadding = 24, ControlsGap = 16;
        public const double BandPadHPortrait = 20, BandPadHLandscape = 14, BandDividerGapPortrait = 16, BandDividerGapLandscape = 10;
        public const double ArrowEm = 0.4, ArrowGutterEm = 0.48, ArrowUpBaselineEm = 0.196, ArrowDownBaselineEm = 0.214;
        public const double UnitAlpha = 0.8;
        public const double BoatZoneWidth = 0.44, BoatZoneHeight = 0.30;
        public const double HorizonPortrait = 0.40, HorizonLandscape = 0.42, CameraHeightM = 2.2, RowerDistanceM = 9.5;
        public const double WrenchSize = 40, WrenchEdge = 16, WrenchGap = 8;   // dp sin s: la llave no escala (R1)

        public static double Scale(double width, double height);              // clamp(lado corto / 390; 0,9; 1,6)
        public static double Horizon(double width, double height);            // 0,40 / 0,42 del alto: el fondo y la cámara
        public static double BoatCenterY(double width, double height);        // h₀ + (lado corto / alto) × 2,2 / 9,5
        public static HudLayout Layout(in HudLayoutInput input);
    }

    // Lo que mide la vista (HudZones): la ventana, los márgenes seguros, si reserva la llave (build con simulador), si hay
    // BPM y objetivos, y los ANCHOS NATURALES a tamaño base de lo que va en cada FitBox (FitBox.NaturalWidth: sin escala).
    public readonly struct HudLayoutInput { /* Width, Height, Insets, ReserveWrench, HasBpm, Targets, Natural: HudNaturalWidths */ }
    public readonly struct HudNaturalWidths { /* Spm, Split, Watts, Kcal, Bpm, BandTime, BandDistance (las cifras) */ }

    // Anclas (dp de ventana), escala, el tope de cada FitBox (dp base) y la escala esperada de sus cifras (T1 a T5), la fila
    // de abajo en horizontal, la zona del bote, el horizonte y, con la llave, su rectángulo y el techo del panel (§6.10).
    public sealed class HudLayout { /* TopBar, Spm, Right, Vitals, Band, Controls: HudZone; VitalsInRow, VitalsMinWidth,
                                       BandCompact; ValueScale(HudValue); BoatZone: Rect; Horizon; Wrench: Rect?;
                                       PanelCeiling: double?; Scale; IsLandscape */ }
    public readonly struct HudZone { /* AnchorX, AnchorY, Corner, MaxBaseWidth */ }
}
// Sale HudArrangement: la disposición horizontal ya la fijó el Designer (DN4a-01) y es parte de la función.
```

| Zona | Vertical (TV, P360) | Horizontal (TH, L640, W1280) | Fuente |
|---|---|---|---|
| Barra superior | `margen seguro sup. + 8·s`, 12·s a cada lado, alto 40·s (DN4a-04) | igual | `:89-97`, `:348-354`, `:497-498` |
| SPM | `14·s`, `margen sup. + 90·s` | igual (T1, T2) | `:611-613` |
| Split y Watts | `14·s` desde la derecha, `margen sup. + 90·s`, columna alineada a la derecha, 8·s entre las dos | igual (T1, T2) | `:639-652` |
| kcal y BPM | `14·s` desde la derecha, `140·s` desde abajo (sin el margen seguro), en columna: kcal arriba y BPM abajo, alineadas a la derecha, 8·s entre las dos; ancho = el mayor, con mínimo de 96·s (T5); sin BPM, kcal baja al lugar de BPM | **en fila a la altura de los controles** (borde de abajo en `margen inf. + 16·s`): kcal en el rincón, a `14·s` del borde, y BPM a su izquierda, 8·s entre las dos; con los mínimos de 96·s si la fila entra con los dos, decidido **por ventana** (T4) | `:709-733`; DV-33 (spec, con el texto de 04-design-notes §11) |
| Banda | `14·s`, `140·s` desde abajo; relleno 20 a los lados y 16 a cada lado del divisor | `14·s`, borde de abajo en `margen inf. + 16·s` (al nivel de los controles); **compacta**: relleno 14 y 10 a cada lado del divisor (T3) | `:669-706`, `:676`, `:693`; DV-33 |
| Controles | `margen inf. + 16·s`, centrados, 24·s a los lados | igual | `:110-116`, `:901-920` |
| Zona del bote | 44 % × 30 % centrada en x, con el centro en `BoatCenterY` = **0,504** del alto | igual, con el centro en **0,652** del alto | ART_BIBLE §5.2, §8.1; DN4a-02 (04-design-notes §3.6) |
| Llave (con simulador) | carril derecho (`16 + margen der.`); su borde de abajo, 8 dp sobre el borde de arriba que tiene la columna kcal y BPM **con BPM** (no se mueve con el pulso) | (a) 8 dp a la derecha de `Pausa`, centrada en el alto de los controles, si a su derecha entra la fila con sus mínimos: `(2 × 96 + 8)·s ≤ ancho − 14·s − (borde derecho de Pausa + 56)` (TH, W1280); (b) si no (L640), 8 dp sobre `Pausa`, alineada con su borde derecho. Por ventana | 04-design-notes §3.9; AC-BLE-16-02 |

- **Topes** (AC-WKT-17-08, -10; 04-design-notes §3.7). Cada zona tiene un ancho máximo para no entrar en la zona del bote ni en su vecina, con las reglas **T1** (la fila de arriba se reparte en proporción si los anchos naturales no entran: solo con los extremos de FTMS), **T2** (en horizontal, SPM no pasa del borde izquierdo de la zona del bote y la columna derecha no empieza antes de su borde derecho), **T3** (la banda: en vertical, hasta 8·s antes de la columna de kcal y BPM; en horizontal, hasta 8·s antes de los controles y, si se cruza en alto con la zona del bote, hasta su borde izquierdo), **T4** (la fila de kcal y BPM: empieza en el mayor de 8·s después de los controles u 8 dp después de la llave si está a su izquierda, y termina a 14·s del borde; mínimos si los dos entran, si no ancho de contenido y, si tampoco, un `FitBox` común) y **T5** (la columna de kcal y BPM, el ancho de la mayor). Los valores van en un `FitBox` (D-10): se achican **solo las cifras**, nunca el rótulo, el relleno ni el borde; el primer cálculo, una rotación o un cambio de ventana se aplican en el acto, y un dato que cambia la escala la sigue con `spring.smooth`, como en la It. 2. Así nada se encima con ningún valor.
- **Los anchos naturales, sin bucles** (03-qa-harmony-tecnica §11). `HudZones` lee `FitBox.NaturalWidth` (el ancho del contenido **sin** escala) en el `GeometryChangedEvent` de cada contenido, llama a `HudGeometry.Layout` y aplica el resultado en el cuadro siguiente. Recalcula solo con un cambio de ventana o de un ancho natural. Los topes no cambian los anchos naturales (el `FitBox` escala, no relayouta su contenido), así que no hay vuelta, y la decisión de T4 y de la llave es por ventana: ninguna tarjeta cambia de modo con un dato.
- **Los números de la disposición** son los de 04-design-notes §3.8 (rectángulos por perfil, con el simulador) y §3.7 (escalas de las cifras que se achican; por ejemplo, la banda a 0,776 en L640 con valores normales y a 0,561 con los largos de CAP-16). El Designer los midió en 5 perfiles × 6 casos de valores × con y sin simulador, 60 combinaciones sin ninguna superposición (`hud_layout.py`), y `HudGeometryTests` los repite: rectángulos ± 0,1, escalas ± 0,005. El Anexo B (las zonas del legacy × `s`) queda como el origen del problema: la columna derecha no entraba en horizontal y la banda chocaba con los controles.
- **El centro de la zona del bote** es el punto donde se proyecta el remero con la cámara de persecución del legacy (cámara a 2,2 m, remero a 9,5 m, focal = lado corto), así la 6a no tiene que mover nada: con 0,5 la zona horizontal quedaba sobre el horizonte y no sobre el bote. Con `docs/image-03.png` el centro da y ≈ 704 de 1080, y el carro está en y ≈ 700.
- **Rotar o cambiar la ventana** (AC-WKT-17-07): `HudZones` recalcula en cada `GeometryChangedEvent` de la página sin tocar la sesión ni los valores; horizontal = ancho > alto (`:605-606`).

### 6.5 Tarjetas `glass.fallback` y semáforo

```
#hud-<métrica> .rm-hud-card --large|--medium|--small [--target] [--off]     (HudMetricCard)
├── .rm-hud-card__glow          absoluto, 12 por fuera, solo con objetivo; GlowTexture (anillo 9-slice, transparente por dentro), base @20 %
├── .rm-hud-card__label         [♥ 11 + 4 en BPM] hudSpm / hudSplit / hudWatts / hudKcal / hudBpm, 13 / 12 / 11 /600, tracking 0,08 em;
│                               color base de la métrica (pulso: metric.hr.hud); 2 abajo
├── .rm-hud-card__value         fila; la alineación por línea de base la hace el código (USS no tiene baseline, QA-G4aT-14)
│   ├── .rm-hud-card__arrow     HUECO de 0,48 em del valor, presente con objetivo desde que empieza el paso (DN4a-15): "▲" / "▼"
│   │                           adentro, Inter 700 a 0,4 em, pegado a la izquierda, state.offTarget.hud, visible solo fuera de objetivo
│   ├── FitBox > TabularNumber  num.hero / num.l / num.m (RowMate Numeric SC); color: metric.*.hud, text.hudPrimary o state.offTarget.hud
│   └── .rm-hud-card__unit      Watts: 4 + hudUnitWatts 13/500 en metric.watts.hud @80 %, en la línea de base de las cifras (DV-31)
└── #hud-target-<métrica> .rm-hud-card__target    2 + hudTarget 12/600 en text.secondary (blanco @70 %); margin-left = el hueco
                                                  (empieza donde empiezan las cifras); solo con objetivo (D-ART-09)
```

- **Vidrio** (AC-WKT-17-01, QA-P-17): fondo `glass.fallback` (`#070D16 @86 %`), borde 1 `#FFFFFF @10 %`, radio `r.glass` 14, sin desenfoque y sin halo. Tamaños base de ART_BIBLE §3.2 (D-ART-02): grande 64 / etiqueta 13/600 / unidad 14/500 / ancho mínimo 140 / relleno 16 × 10; mediana 44 / 12 / 13 / 118 / 14 × 10; chica 32 / 11 / 12 / 96 / 12 × 8.
- **Con objetivo** (AC-WKT-10-05, Q-60, DV-32; 04-design-notes §4.4): etiqueta en el color base; borde del color base al 70 % de 1,5 (`stroke.target`); el halo solo por fuera (`GlowTexture`: con un `BoxShadow` debajo de un vidrio translúcido el interior se teñiría y el fuera de objetivo bajaría a 3,3:1, ART_BIBLE §3.2); el hueco de la flecha, vacío si cumple; `obj. N` debajo, alineado con las cifras; el valor en `text.hudPrimary` si cumple o en `state.offTarget.hud` con la flecha si está fuera. La unidad `W` no cambia con el semáforo: sigue en `metric.watts.hud @80 %` (DV-31). Sin objetivo: el valor en la variante `.hud` de su métrica (AC-APP-06-01), el borde de reposo y sin hueco. **El hueco existe toda la duración de un paso con objetivo**: así, al cruzar el umbral, cambian solo el color y la flecha, y el número no se corre ("no baila"); el hueco aparece o se va solo al cambiar de paso, con la coreografía de §6.11.
- **Medidas en `em` y líneas de base, por código** (QA-G4aT-14). USS no tiene `em` ni `align-items: baseline`, así que `HudMetricCard` las calcula en dp base a partir del tamaño del valor: el hueco mide 0,48 × tamaño (30,7 a 64; 21,1 a 44), la flecha 0,4 × tamaño (25,6; 17,6) y va en posición absoluta dentro del hueco, con su línea de base 0,196 (▲) o 0,214 (▼) × tamaño por encima de la de las cifras (`TabularNumber.BaselineFromTop`), así su tinta queda centrada en la banda de las mayúsculas; la unidad y `obj.` se ubican contra la misma línea de base con las métricas de su `FontAsset` (`faceInfo`), y el `margin-left` de `obj.` es el ancho del hueco. La sonda S-B4a (§9.0) mide la flecha y `obj.` contra las líneas de base de 04-design-notes §3.2, ± 1 dp.
- **Lo que crece desde 0, con el patrón de la It. 3** (QA-G4aT-14). El hueco (0 → 0,48 em), el alto de `obj.` (0 → 16,9 base), `Próximo` (ancho 0 → N, §6.6) y el deslizamiento de kcal cuando BPM aparece o se va: cada medida que se anima vive en un ***slot*** que recorta (`overflow: hidden`, `flex-shrink: 0`) y cuyo tamaño mueve el código; su contenido va en `position: absolute` (o `white-space: nowrap` con `flex-shrink: 0`) con su tamaño natural, medido en el cuadro siguiente (`ILayoutBoundMotion`, como `GrowMotion` y `ErrorLine` de la It. 3, 00-deuda-it3 §1.4). Así Yoga nunca aplasta ni reacomoda el texto. kcal se mueve por FLIP: el *layout* cambia en el acto y kcal se dibuja con un `translate` desde su lugar viejo hasta 0 (en vertical sube 71,7·s dp; en horizontal está en el rincón y no se mueve; 04-design-notes §5.6).
- **Colores por código** desde `Tokens` y `MetricPalette.HudFor`/`LabelFor` (el color de un valor no puede ir por USS con transparencia). La tarjeta BPM aparece y se va con los tiempos de §6.11 (BPM espera a que kcal le deje la mitad de su lugar) y su corazón late con `HeartbeatAnimator` de la It. 2 (AC-WKT-17-05, ANIMATION_BIBLE §2.9).
- **Valores en vivo** (AC-WKT-17-06): `TabularNumber.SetValue(texto, valor)` en cada estado nuevo, también en pausa. Con `WorkoutHudState.Instant` (el primer estado después del segundo plano), `SetValue(…, animate: false)` en todos los números, sin empujón y con todo lo demás en su estado final en el acto (colores, flecha, bordes, halo, hueco, `obj.`, la fila del paso y BPM): el primer cuadro ya muestra el presente y ningún número pasa por valores intermedios (AC-WKT-02-08; 04-design-notes §5.10). Si en un mismo cuadro llegara más de un estado, el rodillo en curso salta a su final (04-design-notes §5.2).

### 6.6 Barra superior

**Con rutina** (`StepInfoBar`, AC-WKT-14-01 a -06): `.rm-hud-topbar --steps`, vidrio con borde `#FFFFFF @8 %` (`border.subtle`) y radio 14, **alto 40** (borde 1, relleno 8, fila de 22, relleno 8, borde 1; DN4a-04), relleno de 12 a los lados, una sola fila (las píldoras y el reloj de pared son de la 4b, DV-34), sin eventos:

```
.rm-hud-topbar --steps
└── .rm-hud-step-row        min-height 22, align-items: center
    ├── #hud-step-type      stepType* 13/700, tracking 0,04 em, en step.<tipo>.hud
    ├── 6
    ├── #hud-step-target    pastilla: fondo step.<tipo> @20 %, radio 6, relleno 6 × 2, texto 11/600 en text.primary (blanco, DV-38);
    │   (.rm-hud-pill)      UN Label con el ícono trending_up (1,17 em, en metric.distance) por texto enriquecido generado por código
    │                       (DV-35, el camino de StepSubtitleView): se recorta con "…" (flex-shrink 1, nowrap) y nunca empuja la cuenta
    ├── .rm-flex-spacer
    ├── #hud-up-next        slot que recorta (ancho 0 → N, §6.5); relleno 7 × 3, radio 8, alto 20,4
    │   (.rm-hud-up-next)
    │   ├── .rm-hud-up-next__bg   [nuevo] absoluto: fondo step.<siguiente> @20 % y borde 1 @50 %; ES LO QUE LATE (0,6 ↔ 1,0)
    │   ├── hudUpNext       10, Inter 400 (.rm-type-body-s con el tamaño en línea), text.secondary (blanco @70 %)
    │   └── tipo siguiente  " " + stepType* 10/700 (.rm-type-header-s, tamaño en línea), text.primary (blanco); el texto, siempre al 100 %
    ├── 8
    └── #hud-countdown      FitBox > TabularNumber num.s 20, blanco, flex-shrink 0; por terminar: escala 1,1 con origen en su borde
                            derecho y color blanco ↔ step.<tipo>.hud con el reloj común
```

- **Pastillas (DV-38, aceptada por el PO; DN4a-16)**: los textos de las dos pastillas en blanco (`Próximo:` en `text.secondary`), los dos tintes al 20 % y, en `Próximo`, laten **solo el fondo y el borde** (`.rm-hud-up-next__bg`), con el texto siempre entero (AC-WKT-14-03, -04). El tipo en curso, a la izquierda, sigue en su color. Con el latido del legacy en toda la pastilla, el texto bajaba a ≈ 3:1 en el punto más tenue (04-design-notes §4.5).
- Por distancia: `500m`, sin "por terminar" ni `Próximo` (Q-51). Sin meta: `?` (DV-25). En pausa la fila queda quieta pero los pulsos siguen (paridad, `:319-322`). Con la rutina terminada: el último paso con `0:00` (o su largo, si es por distancia), sin `Próximo`, de vuelta a escala 1 y a blanco.
- **Un reloj común de pulso** (04-design-notes §5.4; DN4a-17): `Próximo` y la cuenta laten con **un solo** reloj de 600 + 600 ms (`ease.pulse`), como el único `AnimationController(600)..repeat(reverse: true)` del legacy (`:319-322`). Su origen es el primer cuadro con "a continuación" o "por terminar" después de uno sin ninguno de los dos; con pasos de 15 s o menos (`Corta`) "a continuación" sigue de paso a paso y el reloj no se reinicia. Al empezar "por terminar", el color entra con una **rampa** de 150 ms (`a·p`), así no salta aunque el reloj esté a mitad de ciclo. El reloj es el del `MotionDriver` (cuadros reales): después de un segundo plano, el origen es el primer cuadro de la vuelta (p = 0; 04-design-notes §5.10). Con el reloj de prueba la fase de una toma depende solo del instante: la "fase fija" que pide spec §7.1 (por eso CAP-22 va a los 87,6 s, §7.3).

**Libre** (`FreeTopBar`, AC-WKT-16-01): `#hud-free-title`: `rowing` 20 `#00B4D8`, 8, `hudFreeTitle` 16/600 blanco; relleno 14 a los lados y la misma caja de 40 de alto que la de pasos (así SPM queda a la misma distancia en libre y con rutina); vidrio con borde `@8 %`, radio 14. El reloj de pared y el chip de fase son de la 4b.

### 6.7 Banda

`#hud-band .rm-hud-band` (AC-WKT-17-04; 04-design-notes §3.4): cápsula (radio `r.full`), vidrio, borde 1 `@10 %`, relleno vertical 10; `#hud-time` (cronómetro `timer` 16 en `metric.time @70 %` centrado arriba, 2, `TabularNumber` `num.band` 34 en `metric.time.hud`), divisor 1 × 32 al 12 %, `#hud-distance` (`straighten` 16 en `metric.distance @70 %`, 2, `num.band` en `metric.distance.hud`, 3 y `hudUnitMeters` 13/500 en `metric.distance.hud @80 %` en la línea de base, DV-31). **En vertical** como el legacy: relleno 20 a los lados y 16 a cada lado del divisor. **En horizontal, compacta** (DV-33): relleno 14 y 10 a cada lado del divisor, porque comparte la fila con los controles (`00:42 │ 1048 m` mide 216,1 en lugar de 240,1). Las dos cifras comparten **una sola escala** (`FitScaleGroup` sobre sus dos `FitBox`, DN4a-01): la banda se lee pareja; el relleno, el divisor, los íconos y la unidad no se achican. El tope es el de T3 (§6.4).

### 6.8 Controles y diálogo de terminar

```
#hud-controls .rm-hud-controls                 fila centrada, 24 de relleno a los lados, 16 entre botones (base)
├── #hud-finish .rm-button --hud-danger        workoutFinish 16/600 state.dangerText; contorno 1,5 #FF7272; vidrio; mín. 128 × 56; r.l
├── #hud-pause  .rm-button --hud-outline       pause 22 + workoutPause 16/600 blanco; contorno 1,5 border.strong; vidrio; mín. 148 × 56
└── #hud-resume .rm-button --hud-success       play_arrow + workoutResume 16/700 text.onAccent; relleno state.success; mín. 148 × 56
```

- **Pausa y Reanudar** (AC-WKT-20-02): `WorkoutHudStore.Pause()/Resume()` cambian la fase en el acto; los dos botones existen y se alterna su `display` en el mismo cuadro; si el que se oculta tenía el foco, lo toma el otro.
- **Terminar** (AC-WKT-20-03, -04, -05): `FinishWorkoutDialog : ModalDialog<bool>` (`workout-finish-dialog`): `workoutFinishTitle`, `workoutFinishContent`, `#dialog-cancel` (`cancel`, de texto) y `#dialog-confirm` (`workoutFinish`, primario relleno). Tiene **el aspecto opaco de los diálogos de la app**, el mismo del diálogo de inicio (QA-G4a-P05; DN4a-11; 04-design-notes §4.2 y §4.7): `surface.overlay` opaco, `r.card` 16, borde 1 `border.default`, relleno 24 (16 con la ventana de menos de 480 de alto), ancho `min(400; ventana − 80)` (331 en TV), velo al 60 %, título `title` **20/600** `text.primary` (`Terminar entrenamiento` entra en una línea en TV) y cuerpo `body` 15 `text.secondary`. Es la opción de paridad: el legacy usa un `AlertDialog` estándar, opaco (`immersive_workout_screen.dart:126-140`). Sin desenfoque (la It. 6b), un vidrio al 92 % solo dejaba ver el HUD fantasma detrás del velo. **No lleva `.rm-dialog--hud`**: esa clase queda reservada, sin reglas, para el diálogo de completado de la 4b. Mientras está abierto, la sesión sigue en su fase. Confirmar llama `FinishAsync()`: sin resumen ni aviso de completado (Q-53) y sin nada de Strava (no configurado hasta la It. 12; con el simulador, nunca: R-01).
- **La rutina termina con el diálogo abierto** (AC-WKT-20-07): la página lo cancela, sin efecto, en el cuadro en que la fase deja `Active` o `Paused` (§6.3). `HCT.RoutineEndsWithTheFinishDialogOpen` y `HCT.FinishTapAfterTheEndDoesNothing` (§8.2).
- **Sin dobles** (AC-WKT-20-06): desde que se confirma, `ControlsEnabled` es `false` (fase `Finished`) hasta que la página sale: los dos controles pasan al 0,38 en el acto (`SetEnabled(false)`); dos toques en el `Terminar` del diálogo cierran una vez (`Close` idempotente, `ExitLock`, `FinishAsync` idempotente).
- **Guardado lento** (DN4a-20, aceptado): si el cierre tarda más de `FinishSpinnerDelayMs` = 300 ms (= `SaveSpinnerDelayMs` de la It. 3), `Terminar` muestra el `Spinner` de 16 en `state.dangerText` a la izquierda de su texto, sin cambiar de ancho (cabe en su mínimo de 128). Con una base lenta (AC-WKT-06-13) es la señal de que el toque llegó.

### 6.9 Avisos en el inmersivo

`ToastLayer` de la It. 3, sin cambios: el inmersivo registra sus controles (el `worldBound` de `#hud-controls`) como obstáculo inferior y el aviso sube 8 dp sobre ellos (`BottomObstacles.Lift`); la llave también es obstáculo. Así `workoutSaveError` no tapa los controles (AC-APP-03-11, CAP-29). Con la llave de D-ART-10 ajustada (§6.10), en vertical la llave ya no está en el hueco entre la banda y los controles: el aviso cae ahí sin subir y **no tapa el reloj** (en TV, de 782,1 a 830,1, con 15,6 dp sobre la banda; 04-design-notes §4.8). En horizontal va centrado sobre los controles y puede quedar 4 s sobre la zona del bote (es un mensaje, no una pieza del HUD); en L640 sube sobre la llave, que está sobre `Pausa`. `workoutCompleted` y el `workoutSaveError` del final entran en el cuadro 0 de la salida del inmersivo, sobre la barra de pestañas que vuelve (§6.3); si el usuario no está delante, `WorkoutStore` los retiene y salen una vez al volver (§2.11).

### 6.10 La cinta y la llave (D-ART-10, DD4a-01 a DD4a-04)

La capa del simulador sigue siendo el último hijo de `#app-root`: queda arriba del inmersivo, de sus diálogos y de sus avisos sin código nuevo (AC-BLE-16-01). La cinta no recibe toques y en el inmersivo tapa solo el comienzo de la barra superior, como en `docs/image-03.png`.

```csharp
namespace RowMate.Core.Design
{
    public enum OverlayContextKind { Tab, StackedPage, Workout }

    // Lo que declara la pantalla de arriba para la llave (DD4a-01, DN4a-14). Lo guarda ShellLayout (SetSurface); manda la de
    // arriba de la pila. En el entrenamiento, WrenchSlot es el lugar que calcula HudGeometry (§6.4) y PanelCeiling, el borde
    // de arriba que el panel no pasa (en vertical, 8 dp debajo del lugar de Watts con su objetivo; AC-BLE-16-03).
    public sealed record OverlaySurface(OverlayContextKind Kind, double TabBarHeight, IReadOnlyList<BottomObstacle> Obstacles,
                                        Rect? WrenchSlot, double? PanelCeiling);
}

namespace RowMate.Simulator.Panel
{
    public readonly struct WrenchSpot
    {
        public bool RightLane { get; }
        public double Left { get; }          // dp desde la izquierda
        public double Bottom { get; }        // dp desde abajo
        public bool Clear { get; }           // false: ninguna altura cumple los obstáculos (con el lugar del HUD, nunca en los 5 perfiles)
    }

    // R1-R5 y R9 de 04a/00-deuda-diseno §1.3, con el entrenamiento de 04-design-notes §3.9 (D-ART-10 ajustada, DV-39).
    public static class WrenchPlacement
    {
        public const double Size = 40, EdgeMargin = 16, ObstacleGap = 8;
        public static WrenchSpot For(double windowWidth, double windowHeight, SafeInsets insets, OverlaySurface surface);
        public static PanelAnchor PanelFor(WrenchSpot wrench, OverlaySurface surface);   // esquina, lado de crecimiento y
                                                     // alto máximo (SimulatorPanelLayout.MaxHeight desde el borde de abajo
                                                     // del panel, mínimo 120; con PanelCeiling en el entrenamiento vertical)
    }
}
```

D-ART-10 está en la spec (AC-BLE-16-02, -03, AC-RUT-10-04, -09; DV-39 reemplaza a DV-13; QA-G4aT-02), con el ajuste del inmersivo de 04-design-notes §3.9 (DN4a-14), que reemplaza para el entrenamiento a 00-deuda-diseno §1.5 y al "L640: ningún lugar" del Anexo B.

- **R1, carril**: izquierdo (`16 + margen izq.`) en pestañas, pantallas apiladas, diálogos y menús; en el inmersivo, el lugar del HUD.
- **R2, altura**: pestaña, barra visible + 16; pantalla apilada que tapa la barra, 16 + margen inf.
- **El inmersivo** (DN4a-14): `WrenchPlacement` usa **tal cual** `OverlaySurface.WrenchSlot`, que calcula `HudGeometry` por ventana (§6.4): en vertical, en el carril derecho, 8 dp sobre el lugar de la columna kcal y BPM **con BPM** (así no se mueve cuando el pulso aparece o se va, y deja libre el hueco de los avisos, §6.9); en horizontal, 8 dp a la derecha de `Pausa`, centrada en su alto (TH, W1280) o, si a su derecha no entra la fila de kcal y BPM con sus mínimos, 8 dp sobre `Pausa`, alineada con su borde derecho (L640). R3 y R4 ya no hacen falta ahí: el lugar sale de la misma geometría que el HUD, que lo deja libre. Rectángulos de 04-design-notes §3.8 (fila "Llave"): TV 355,0-395,0 · 575,9-615,9; P360 304,0-344,0 · 497,9-537,9; TH 618,9-658,9 · 344,6-384,6; L640 414,8-454,8 · 245,5-285,5; W1280 881,6-921,6 · 629,6-669,6. `Clear` queda en `true` en los cinco perfiles (también con `s` = 0,9).
- **R3, obstáculos** (fuera del entrenamiento): sube hasta 8 sobre cada obstáculo que cruce su caja agrandada 8 (`BottomObstacles.Lift`, gap 8). Hoy no hay ninguno en el carril izquierdo: el `+` de Rutinas deja de ser obstáculo de la llave (sigue siéndolo del aviso y del relleno), así el `+` queda entero en su rincón (AC-RUT-10-04).
- **R5**: el relleno inferior de lo que se desplaza suma el tope de la llave + 16 (`RegisterClearance`, It. 3): la última tarjeta queda por encima del `+` y de la llave (AC-RUT-10-09, AC-WKT-12-09).
- **R9, panel** (DD4a-03; AC-BLE-16-03): mide 280 dp, se abre donde está la llave y crece desde su esquina, con la transición de la It. 2 (opacidad 160, escala 0,92 → 1 en 240) y el origen ahí: fuera del entrenamiento, desde la esquina de abajo a la izquierda, hacia arriba y a la derecha; en el entrenamiento, desde la de abajo a la derecha, hacia arriba y a la izquierda. Alto máximo, medido desde su borde de abajo, con mínimo 120: en las pestañas, el de la It. 2 (sin margen seguro: 360 → 248, 914 → 802, 200 → 120); con una pantalla apilada que tapa la barra, 64 dp más (312, 866, 152); en el entrenamiento vertical, hasta `PanelCeiling` = 8 dp debajo del lugar de Watts **con su objetivo** (así no cambia de alto al cambiar de paso: en TV quedan 316 dp y el contenido se desplaza adentro); en horizontal no llega a Split ni a Watts, que quedan a su derecha. El tope de Watts **se acepta** (spec P-14): deja de estar detrás de una opción. Si no entra, el contenido se desplaza adentro y el botón de cerrar queda a la vista. Mientras el panel está abierto, la llave no se ve.
- **El panel sigue a la llave** (AC-BLE-16-02; spec §12 punto 13). Si la llave cambia de lugar con el panel abierto (se abre o se cierra una pantalla apilada, empieza o termina un entrenamiento, también cuando el inmersivo se cierra solo, o cambia la ventana), el panel **sigue abierto** y pasa al lugar nuevo con el mismo movimiento que tendría la llave: entre una pestaña y una pantalla apilada, el resorte ω 22 en y; al entrar o salir del inmersivo, un fundido (sale en 90 en el cuadro 0 y entra en 160 en el lugar nuevo, con su esquina nueva); con reducir movimiento, en el acto o solo con el fundido. Nunca queda en el lugar de una pantalla que ya se cerró (en el legacy queda abierto por encima de todas las pantallas).
- **Movimiento de la llave** (ANIMATION_BIBLE §2.11, §2.14; 04-design-notes §5.9): entre pestañas no se mueve; pestaña ↔ pantalla apilada, resorte ω 22 en y; al entrar y salir del inmersivo cambia de lugar con un fundido (sale en 90 en el cuadro 0 de la transición; entra al terminar con 0,85 → 1 en 160); dentro del inmersivo no se mueve con los valores ni con BPM, y con una rotación o un cambio de ventana se reacomoda en el acto, junto con el HUD. Se puede tocar mientras se mueve.
- **Menús** (DD4a-02): `PopupMenu` trata la caja de la llave agrandada 8 dp como un borde: se abre del otro lado del ancla y, si tampoco entra, se corre en x.
- **Auditoría** (DD4a-04): `Snapshots/WrenchAudit` revisa cada toma con simulador: ningún control (`.rm-button`, `.rm-icon-button`, `.rm-chip__pill`, `.rm-fab`, pestañas, `.rm-segmented__option`, `.rm-switch`, `.rm-menu__item`, `.rm-field__box`, *chevron*, controles del HUD) se cruza con `#sim-open` agrandado 8; campos y superficies grandes, sin superposición; en el inmersivo, ninguna pieza del HUD. Si falla, la toma falla.

### 6.11 Movimiento

Todo por código con el reloj del `MotionDriver` (deterministas en el arnés), con los valores de 04-design-notes §5 (DN4a-05, DN4a-06 y DN4a-17, aceptados). Las constantes nuevas van en `MotionSpecs` con los nombres de 04-design-notes §5.13, **en WP-A2** (las prueba `MotionSpecsTests`): `HudColorMs` 150, `OffTargetArrowMs` 120 y `OffTargetArrowShiftDp` 4, `OffTargetBumpScale` 1,04, `OffTargetBumpPeakMs` 90, `OffTargetBumpEndMs` 240, `OffTargetBumpCooldownMs` 4000, `StepPulseHalfMs` 600, `UpNextMinOpacity` 0,6, `UpNextFadeInMs` 200, `UpNextWidthOmega` (= `CriticalSpring.SmoothOmega`), `EndingSoonScale` 1,1 y `EndingSoonOmega` (= `SnappyOmega`), `StepSwapOutMs` 120, `StepSwapInStartMs` / `StepSwapInEndMs` 60 / 260, `TargetDecorMs` 240, `TargetTextInStartMs` / `TargetTextInEndMs` 60 / 240, `BpmMoveMs` 240, `BpmFadeInStartMs` / `BpmFadeInEndMs` 120 / 360, `BpmFadeOutMs` 120, `BpmMoveOutStartMs` / `BpmMoveOutEndMs` 120 / 360, `ImmersiveInMs` / `ImmersiveOutMs` / `ReducedImmersiveFadeMs` 240 / 200 / 120 y `FinishSpinnerDelayMs` (= `SaveSpinnerDelayMs`, 300). Se reusan sin cambios `DigitRoll.DurationMs` (180), `ReducedFadeMs` (80), `GrowOmega` y los de `CollapseMotion`, `ToastTextOutMs` / `ToastTextInEndMs`, `FitScaleOmega`, `FabInScaleFrom` y `PanelInScaleFrom`. Las de geometría (`TopBarHeight`, `ArrowGutterEm`, `UnitAlpha`, horizonte, cámara, `WrenchGap`…) van en `HudGeometry` (§6.4).

| Qué | Cómo | Reducir movimiento | Fuente |
|---|---|---|---|
| Entrada y salida del inmersivo | fundido 240 `ease.enter` / 200 `ease.exit`; pestañas y aviso del fin en el cuadro 0 de la salida | fundido de 120 lineal | AC-APP-03-09; 04-design-notes §5.1 |
| Diálogos, avisos | `DialogTransition`, `ToastTransition` de la It. 3 | solo opacidad | AC-APP-03-10, -11 |
| Rodillo de los valores, reloj y cuenta | `TabularNumber` + `DigitRoll`, **180** en todos (también el reloj y la cuenta, no 160); sin rodillo con `Instant` | fundido de 80 | ANIMATION_BIBLE §2.4, §2.6; 04-design-notes §5.2 |
| Entra o sale "fuera de objetivo" | color 150 `ease.move` (mezcla sRGB desde el color que se ve); la flecha entra en 120 (`ease.enter`) desde 4 dp en el sentido contrario al que indica (▲ sube, ▼ baja) y sale 1 → 0 en 120 siguiendo 4 dp en su sentido; empujón 1 → 1,04 en 0-90 (`ease.enter`) → 1 en 90-240 (`ease.move`), **solo** al pasar de cumple a fuera dentro del mismo paso y como mucho uno cada 4 s por tarjeta, nunca al empezar un paso | sin empujón ni desplazamiento: color 150 y la flecha funde en 120 | AC-WKT-10-06; §2.8; VID-02; 04-design-notes §5.3 |
| Cambio de paso | tipo y pastilla viejos 0-120 `ease.exit`, quietos y absolutos; nuevos 60-260 `ease.enter` con subida de 8 → 0; borde, halo, hueco de la flecha y alto de `obj.` 0-240 `ease.move`; texto de `obj.` 60-240 `ease.enter`; el valor cruza en 150; sin empujón | fundidos, sin subida | §2.7; VID-01; 04-design-notes §5.5 |
| `Próximo` | entra con ancho 0 → N (ω 12) y opacidad 0 → 1 en 200 por el latido 0,6 + 0,4·p; de un paso al siguiente no sale (el tipo viejo funde en 0-80 y el nuevo entra en 80-200); sale con opacidad → 0 en 0-120 y ancho → 0 en 80-280; late el fondo (§6.6) | entra con fundido; realce fijo (opacidad 1) | AC-WKT-14-04; 04-design-notes §5.4 |
| Por terminar | escala 1 → 1,1 con ω 22 (origen en el borde derecho); color blanco ↔ paso en `a·p`, con la rampa `a` de 150 al empezar y al terminar | realce fijo: escala 1,1 y el color del paso, que llega en 150 | AC-WKT-14-05 |
| Tarjeta BPM y kcal | BPM aparece: kcal a su lugar en 0-240 `ease.move` (FLIP) y BPM funde en 120-360 `ease.enter`; se va: BPM funde en 0-120, en su lugar, y kcal se mueve en 120-360; corazón con `HeartbeatAnimator` | BPM funde 240 / 120; kcal en el acto; el corazón quieto | §2.9; 04-design-notes §5.6 |
| Escala de un `FitBox` | el resorte de la It. 2 (`spring.smooth`) con un dato nuevo; en el acto al armar, rotar o cambiar la ventana | en el acto | 04-design-notes §3.7 |
| *Spinner* de `Terminar` | aparece si el cierre tarda más de 300 ms | igual | DN4a-20 |
| Llave y panel | §6.10 | en el acto; cambio de lugar solo opacidad | §2.11, §2.14 |

Nada se anima antes del primer cuadro presentado; ninguna propiedad animada por código tiene transición de USS. Todas son finitas salvo los pulsos y el corazón (no cuentan para `Settle`, como en la It. 2): por eso salen del `MotionDriver` con el HUD (`HeartbeatAnimator.Stop` y `StepPulse.Stop` en `HudView.Dispose`; QA-V4a-02). Con `WorkoutHudState.Instant` nada se anima: todo va a su estado final en el acto (§6.5).

### 6.12 Foco, teclado, letra

- **Tab** (AC-APP-05-07): `Entrenamiento libre`, las tarjetas, `Reintentar`; en los diálogos, sus botones (`FocusTrap`); en el inmersivo, `Terminar` → `Pausa` / `Reanudar` → la llave (con el simulador) → `Terminar`, y Shift+Tab al revés (DN4a-10). La llave vive en la capa del simulador: la página arma su anillo de Tab con `#sim-open` al final cuando existe (`ShellLayout` la expone; `SimulatorOverlayTests.WrenchInTheImmersiveTabRing`). El inmersivo abre sin foco. Enter y Espacio activan; Escape cancela el diálogo abierto.
- **Anillo de foco en el HUD** (DN4a-10; 04-design-notes §4.6): blanco (`text.primary`) de 2 dp, a −4 del borde de afuera del control (radio 16), con un halo `overlay.scrim` al 50 % de 4 dp pegado por fuera (radio 20), sin animación y solo navegando con teclado. Se ve sobre cualquier fondo (3,977:1 sobre una nube; el cian de la app daba 2,465). Fuera del HUD, el anillo de ART_BIBLE §3.6.
- **DT4a-01** (00-deuda-it3 §11.1; aceptado por el PO): `ExitLock` devuelve el foco a quien abrió **solo si se estaba navegando con teclado** (`.rm-keyboard-nav`); si no, lo quita. Con el mouse, Flutter de escritorio no enfoca el botón: así un Enter después de `Cancelar` no vuelve a abrir el diálogo de terminar.
- **Letra**: Workout en reposo y los diálogos siguen la letra del sistema hasta 1,3×; el HUD no (§6.4).

### 6.13 Rendimiento del HUD

- **≤ 60 elementos visibles** (ARCHITECTURE §7.8, §20.2; recontado por QA-G4aT-01). "Visible" = en la jerarquía del inmersivo y con `display` distinto de `none`; el contorno de depuración no cuenta (solo existe con `BoatZoneOutline`). Con rutina, objetivos en SPM y Watts, BPM y `Próximo`, el árbol de §6.3-§6.8 daba 58. Las 04-design-notes suman el cielo del fondo y el fondo de `Próximo` (60), y DN4a-20 el *spinner* de `Terminar` mientras se guarda (61). Para quedar en el tope se toma la salida que dejó el Designer (04-design-notes §7): el fondo es **un solo elemento** con la textura de 1 × 100 de `BackdropTexture` (cielo y agua, con el corte en el texel 40 o 42, filtro *point*: el borde queda recto), así el cielo no suma. Cuenta del peor caso: **59** en marcha y **60** durante el guardado, con el *spinner* (si la sesión se termina a mano con `Próximo` a la vista, `Próximo` sigue mientras se guarda). El hueco de la flecha es el mismo `.rm-hud-card__arrow` con un ancho fijo: no suma. La flecha, `obj.`, el hueco, el halo, el *spinner* y la tarjeta BPM están en `display: none` cuando no corresponden. `HudBudgetTests` cuenta el peor caso, con el *spinner*, en los cuatro perfiles, y falla con 61. **No queda margen para la 4b** (la fila de píldoras): su diseño tiene que juntar elementos (R4a-03).
- **0 B por cuadro** entre lecturas: rodillos y pulsos cambian estilos (estructuras) y redibujan la malla de su elemento; nada de LINQ, *closures* ni cadenas por cuadro. Lo que se arma por lectura (el estado del HUD) es de 1 Hz. `HudBudgetTests` mide `GC.Alloc` con el *Profiler* durante 10 s con valores cambiando a 1 Hz y los pulsos corriendo.
- **60 fps** mientras el inmersivo está arriba (§6.3); fuera, la política de 60/30 de la It. 2.

### 6.14 Fuentes: kerning K2 y RowMate Numeric SC (DD4a-06, DD4a-07)

- **WP-D0**: `tools/fonts/` con la receta del Designer y fontTools fijado (`uvx --offline --from fonttools==4.68.0`). **Primero** `freeze_tnum.py` arma `UI/Fonts/RowMateNumericSC/RowMateNumericSC-{SemiBold,Bold,ExtraBold}.ttf` con su `OFL.txt`; **después** `kern_pairs.py` escribe `UI/Fonts/Kerning/<cara>.pairs.json` con **todos** los pares GPOS `kern` distintos de 0 del juego fijo (K2) para Inter 400, 500, 600 y 700 y **RowMate Numeric SC 700** (la derivada, en lugar de Barlow 700: DN4a-22), ordenados por (primero, segundo), en unidades de la fuente. La de la derivada 700 es la de Barlow sin pares entre cifras (las `*.tf` no tienen). La derivada 600 y 800 no llevan tabla (K4: solo dibujan `TabularNumber`).
- **WP-B0**: `FontAssetBuilder` vuelca cada lista en `m_FontFeatureTable.m_GlyphPairAdjustmentRecords` con `SerializedObject`, dentro de `GeneratedAssetGuard.AllowWrites()` (`XAdvance` = unidades / `unitsPerEm` × tamaño de muestreo: 48 en Inter, 90 en la derivada; el resto en 0), deja `m_GetFontFeatures: 0` en las 7 caras de texto (K1) y crea los tres `FontAsset` dinámicos de la derivada (muestreo 90, vacíos, como los otros). `FontAssetPaths` pasa el rol numérico (`FontFamilyRole.Numeric` 600, 700 y 800) **a la derivada** y los `FontAsset` de Barlow **salen de `All`** (DN4a-22): nada los referencia, porque todo `.rm-type-num-*` usa la derivada, también `num.stat`. `All` sigue en 10 (7 de texto y número, 2 de íconos y el respaldo de emojis) y `TextFaces` en 7 (Inter ×4 y la derivada ×3), así que `FontAtlasWarmup` no crece. B0 borra los tres `BarlowSemiCondensed-*-SDF.asset` generados (y sus `.meta`); los `.ttf` de Barlow quedan como entrada de la receta (ASSET_SLOTS §3.7). `tools/tokens` regenera `Typography.uss`. `TabularNumber` no cambia: sus celdas pasan a medir todas lo mismo.
- **Pruebas**: `ItFourAFontTests` (dotnet, archivo de WP-B): la tabla de cada `.asset` es exactamente su lista (K3), `m_GetFontFeatures` es 0, y ni `Typography.uss` ni ningún `.asset` o `.uss` de `Assets/RowMate` nombra los `FontAsset` de Barlow; `FontAndIconAssetsTests` (Unity EditMode): en cada derivada las diez cifras tienen el mismo avance (507, 521, 535 unidades). Los conteos de `FontAssetPaths.All` de `TypographyUssSyncTests` y `ItTwoDebtTests` siguen en 10 (cambia solo su mensaje; excepción de dueño en §9.0).
- **DN4a-12, respondida**: sí, todo `.rm-type-num-*` usa la derivada, también `num.stat` (el panel del simulador): `150 W` → `160 W` no corre las letras. La cara que dibuja texto es la derivada 700, con su tabla K2 (DN4a-22).
- **Regeneración** de los conjuntos `it2` e `it3` con las tablas nuevas, la derivada y la llave de D-ART-10, una sola vez en B0 y con la regla de QA-V3-06 (dos corridas seguidas con los mismos bytes).

---

## 7. Arnés de capturas (WP-B)

### 7.1 Cambios al arnés

| Pieza | Cambio |
|---|---|
| `SnapshotCli`, `SnapshotCatalog` | `-snapshotSet it4a` (por defecto desde la 4a) → `WorkoutCatalog.All`, salida `docs/migration/iterations/04a-motor-hud-funcional/snapshots/`; `it2` e `it3` siguen igual |
| `UiHostOptions` | `Sessions` (`HostSessionFactory`: por defecto, el mismo archivo que las rutinas si son SQLite y en memoria si no), `Lifecycle` (un `FakeAppLifecycle` por host), `BoatZoneOutline` (muestra `#hud-boat-zone`; solo PlayMode y arnés), `StartUtc` = `2026-10-28T13:00:00Z` en el conjunto `it4a` |
| `HostSessions` (N) | `SameFileAsRoutines`, `InMemory`, `Scripted(Action<ScriptedSessionRepository>)` (lenta o con fallas al crear, al guardar un punto o al cerrar), `Serial` (rendimiento) |
| `HostBackends` | `Simulator(noise: false)` (ya está) y `TestRower(Func<int, RowingData>)` con `ScriptedRower` y el pulsómetro de prueba |
| `UiTestHost` | `SendToBackground()` / `BringToForeground(segundos)`: con `FakeScheduler` el alcance es `Advance`, con `IsCatchingUp = true` mientras dura y `InBackground = false` recién al final, como el orden de `AppRoot` (QA-G4aT-07); `Unfocus()` / `Focus()` (la ventana sin foco o minimizada: el reloj sigue); `HudState`, `Immersive`, `TapRoutineNamed(nombre)`; `DeviceProfile.P360`. Sin pedido, adelante y con foco, también en las corridas sin ventana (spec §0.4) |
| `ScenarioScript` | `TapRoutine(nombre)`, `StartRoutine(nombre)` (tarjeta + `Iniciar`), `AdvanceTo(segundosDesdeElInicio)` |
| `SnapshotRunner` | `WrenchAudit` en toda toma con simulador (DD4a-04); `HudOverlap` (ningún par entre barra, tarjetas, banda, controles y llave se cruza; todo dentro de la ventana) en toda toma del inmersivo |
| Regla de toma | la de las It. 2 y 3 y las de 04-design-notes §8.1: ningún fundido del inmersivo, cambio de lugar de la llave o del panel, cambio de paso, salida de `Próximo`, animación de BPM o kcal, empujón ni rampa de color en curso; los pulsos (`Próximo`, por terminar) se fotografían en la fase de 04-design-notes §5.4 y el corazón cuenta "en reposo" con `BeatRestTolerance`; el aviso entero a la vista cuando la toma lo pide; las escalas de los `FitBox` de 04-design-notes §3.7 son parte de lo esperado |
| `capture-snapshots.ps1` | `-Set it4a` por defecto; `it2` e `it3` igual que siempre |

Por defecto en `it4a`: el perfil indicado, español (`es-AR`), modo simulador con la capa completa y sin ruido (150 W y 24 spm dan `02:13`), el pulsómetro simulado sin conectar, la base sembrada en un archivo temporal propio con sesiones en el mismo archivo y el ejecutor en línea. Ninguna toma abre `rower_app.db` ni `rower_app_dev.db` (AC-HIS-20-04). Los tiempos de los guiones (`Advance`) son de reloj de prueba.

### 7.2 Semillas

`RoutineSeeds.F4A` (de la más nueva a la más vieja): `Prueba 4a` (calentamiento 30 s; serie ×2 [trabajo 60 s, vatios mín. 160, SPM 22, progresión SPM +2; descanso 30 s]; enfriamiento 30 s), `Corta` (serie ×2 [trabajo 10 s, SPM 18, +2; descanso 5 s]), `Distancia 100` (trabajo 100 m; descanso 5 s) y `Sin meta` (trabajo por distancia sin metros; descanso 5 s). `RoutineSeeds.Rara4a(nombre)`: las F-RARA con los nombres de la spec de la 4a (`Serie enorme`, `Vatios imposibles`, `Serie ×0`, `Sprint`), cada una en su base. `Lista`, `Empty`, `ListaPlus(26)` y la F-ROTA de la It. 3 no cambian. `RowerScripts.Long`: reloj del HUD a `1:02:05` (3725 s de entrenamiento), distancia `123456`, −999 W, split 0, kcal 9999, pulso 188 y SPM 40.5 (CAP-16).

### 7.3 Catálogo `it4a`

Archivos `CAP-nn[a|b|c]-<tema>.png`, `DEMO-nn-<tema>.png`, `VID-nn-<tema>.mp4`. "Comprobaciones" = `Checks` (si fallan, la toma falla). "Libre a los N s" = `Tap(workout-free)` y `Advance(N)`; "rutina R a los N s" = `StartRoutine(R)` y `Advance(N)`.

| ID | Perfil · base · backend | Cómo se llega | Comprobaciones |
|---|---|---|---|
| CAP-01 | TV · `Lista` | `Tap(tab-workout)` | `workoutTitle`, sin acciones; `#workout-free` habilitado, de 16 a 16 y alto ≥ 56; `#workout-routines-label` = `␣␣O elegí una rutina:`; `workout-routine-0…3` = `Escalera SPM`, `Pirámide 500`, `Solo calentamiento`, `Intervalos 4×`, con resumen y `SegmentCount` 8 / 14 / 1 / 7; *chevron* en `.rm-routine-card__chevron`, centrado en la columna de 28 dp, y ningún `⋮`; sin `#workout-banner` ni escenario; `#sim-open` abajo a la izquierda (`xMin` = 16, borde de abajo a barra + 16), sin cruzar ningún *chevron* (`WrenchAudit`); pestaña 1 elegida |
| CAP-02 | TV · `Lista` | `Do(desconectar el remo simulado)`, `Tap(tab-workout)` | `#workout-banner` con `warning` y `Conectá el rower para iniciar un entrenamiento`; `#workout-free` al 0,38 y sin responder; `#workout-routines` al 0,5 y `WhyNotTappable(workout-routine-0) != null` |
| CAP-03 | TV · `Empty` | `Tap(tab-workout)` | sin etiqueta ni tarjetas; `#workout-empty` centrado en x (± 1) con alfa del texto ≥ 0,54 |
| CAP-04 | TV · `Lista` · `en` | como CAP-01 | `Workout`, `Free workout`, `␣␣Or pick a routine:`, `1 interval · 1 rest`, `2 intervals · 1 rest`, `1 step`, `4 intervals · 3 rests` |
| CAP-05 | L640 · `ListaPlus(26)` · letra 1,3 · sin remo | `Tap(tab-workout)`, `ScrollToEnd(workout-scroll)` | la última tarjeta entera, por encima de la barra y de `#sim-open` (16 ± 1); `Audit()` sin desbordes |
| CAP-06 | TV · `Scripted(LoadDelay = Never)` | `Tap(tab-workout)`, `Advance(0.3)` | `#workout-free` y solo `#workout-loading` con opacidad 1; nunca `#workout-empty` |
| CAP-07 | TV · `Rota()` | `Tap(tab-workout)` | `#workout-error` con `No se pudieron cargar las rutinas` y `#workout-retry` debajo de `#workout-free`, que sigue habilitado |
| CAP-08 | TV · `Lista` | `Tap(workout-routine-0)` | `workout-start-dialog`: título `Escalera SPM`, contenido `1 serie · 1 descanso⏎⏎¿Iniciar el entrenamiento?`, `#dialog-cancel` de texto, `#dialog-confirm` relleno; ancho 331; `#sim-open` tocable sobre el velo |
| CAP-09 | TV · `Rara4a("Serie enorme")` | `Tap(workout-routine-0)` | sin diálogo; `#toast` entero = `Esta rutina no se puede iniciar: revisá sus pasos en Rutinas`, sin cruzarse con `#sim-open`; ninguna fila en `workout_sessions` |
| CAP-10 | W1280 · `Lista` · escritorio | como CAP-01 | lo mismo a lo ancho; comparable con `docs/image-02.png` sin el escenario |
| CAP-11 | TV · `Lista` · letra 1,3 | `Tap(tab-workout)` | `Audit()` sin textos cortados ni fuera de su tarjeta |
| CAP-12 | TV · `Lista` | libre a los 42 s | `#hud-free-title` con `rowing`; `#hud-spm` `24.0`, `#hud-split` `02:13`, `#hud-watts` `150` + `W`, `#hud-time` `00:42`, `#hud-distance`, `#hud-kcal`; sin `#hud-bpm`; `#hud-finish` y `#hud-pause`; el fondo con el cielo arriba del 40 % del alto y el agua abajo; sondas: el vidrio (`#070D16 @86 %` compuesto sobre el cielo y sobre el agua, ± 2), los valores en `.hud` y las unidades en `.hud @80 %` (AC-APP-06-01, DV-31); `#sim-ribbon` sobre el comienzo de la barra; `#sim-open` sobre kcal, en el lugar de §6.10 (355-395 · 575,9-615,9, ± 1); `HudOverlap`, `WrenchAudit` |
| CAP-13 | TV · `Lista` | conectar el pulsómetro simulado; libre a los 42 s | lo de CAP-12 y `#hud-bpm` con `favorite` y el pulso en `#FF5D7F` (sonda) |
| CAP-14 | TV · `Lista` | CAP-13, `Tap(hud-pause)`, `Advance(3)` | `#hud-resume` (fondo `#06D6A0`) en lugar de `#hud-pause`; `#hud-time` igual que al pausar |
| CAP-15 | TH · `Lista` | como CAP-13 | la fila de abajo: la banda compacta a la izquierda, `Terminar` y `Pausa` al centro, la llave a la derecha de `Pausa` y BPM y kcal en fila con kcal en el rincón (DV-33); nada sobre la zona del bote; los rectángulos de 04-design-notes §3.8 (± 1); `HudOverlap`, `WrenchAudit` |
| CAP-16 | L640 · `Empty` · `TestRower(Long)` · sesiones en memoria · `BoatZoneOutline` | libre a los 3725 s | los valores de §7.2, enteros; `HudOverlap`; ninguna pieza del HUD entra en `#hud-boat-zone`; la banda a escala 0,561 (± 0,005) sin tocar los controles ni la zona; kcal `9999` y BPM `188` a ancho de contenido; la llave sobre `Pausa` (el caso b de §6.10); `WrenchAudit` |
| CAP-17 | W1280 · `Lista` · escritorio | como CAP-13 | escala 1,6 (la caja del valor de SPM mide 102,4 ± 1); kcal y BPM en fila, de 153,6 cada una; la llave a la derecha de `Pausa`; la banda compacta, sin achicar; las zonas de `docs/image-03.png` |
| CAP-18 | TV · `F4A` | `Prueba 4a` a los 6 s | `#hud-step-type` `Calentamiento` en `#FFD166`, `#hud-countdown` `0:24`; sin `#hud-step-target` ni `#hud-up-next`; ninguna tarjeta con objetivo |
| CAP-19 | TV · `F4A` | `Prueba 4a` a los 50 s | `Trabajo` en `#FF5D7F`; pastilla `>160W · 22 spm · ` + `trending_up` + `SPM +2/rep`, con el texto en blanco sobre el tinte rosa al 20 % y la tendencia en `metric.distance` (DV-38); `#hud-watts` `150` en `#FF625B` con `▲` en su hueco y `#hud-target-watts` `obj. 160` debajo de las cifras, etiqueta y borde amarillos con halo; `#hud-spm` blanco con el hueco vacío y `obj. 22`; Split sin objetivo |
| CAP-20 | TV · `Lista` | `sim-preset-suave`; `Pirámide 500` a los 605 s | `#hud-countdown` `500m`; pastilla `150-180W · 02:05/500m · ` + ícono + `W +10/rep, Split -2s/rep` (o recortada con `…`); Watts fuera (`▲`, `obj. 150`); Split fuera (`▼`, `obj. 02:05`); SPM sin objetivo |
| CAP-21 | TV · `F4A` | `Prueba 4a` a los 78 s | `#hud-up-next` = `Próximo: Descanso`, entero, a 8 de `0:12`: `Próximo:` en `text.secondary` y `Descanso` en blanco (`#FFFFFF`), sobre el fondo `step.rest @20 %` con borde `@50 %` (DV-38); la pastilla de objetivo recortada con `…` |
| CAP-22 | TV · `F4A` | `Prueba 4a` a los **87,6 s** (DN4a-21: a los 87,0 el reloj común está en p = 0 y la cuenta se vería blanca) | `#hud-countdown` `0:03` (a 3 s del final, como pide la spec) con escala 1,1 y en `#FF5D7F` puro (p = 1); `Próximo: Descanso` entero |
| CAP-23 | TH · `F4A` | como CAP-19 | `HudOverlap`; la pastilla entera o con `…` |
| CAP-24 | L640 · `F4A` | `Prueba 4a` a los 118 s, `sim-spm-minus` (23), `Advance(7)` | SPM fuera (`▲`, `obj. 24`), Watts fuera (`▲`, `obj. 160`); SPM a escala 0,962 por la zona del bote (T2, ± 0,005); la llave sobre `Pausa`; `HudOverlap`, `WrenchAudit` |
| CAP-25 | W1280 · `F4A` · escritorio | como CAP-19 | lo mismo a escala 1,6 |
| CAP-26 | TV · `Lista` | libre a los 10 s, `Tap(hud-finish)` | `workout-finish-dialog` **opaco**, de 331 (AC-WKT-20-03): `Terminar entrenamiento` (20/600, en una línea), `Se guardará la sesión. ¿Continuar?`, `Cancelar` de texto y `Terminar` relleno; sin `.rm-dialog--hud`; el HUD detrás del velo; cinta y llave encima |
| CAP-27a/b/c | TV · `F4A` · `en` | a) libre a los 10 s; b) + `Tap(hud-finish)`; c) `Prueba 4a` a los 78 s | a) `Free Workout`, `SPM`, `Split /500m`, `Watts`, `kcal`, `Finish`, `Pause`; b) `Finish workout`, `The session will be saved. Continue?`, `Cancel`, `Finish`; c) `Work`, `Up next: Rest` con `Rest` en blanco, como CAP-21 (DV-38), `target 160` |
| CAP-28 | TV · `F4A` | `Corta` a los 31 s, `Settle` | Workout en reposo; `#toast` `¡Entrenamiento completado!`, 8 dp sobre la llave; en la base, la sesión cerrada con 7 puntos |
| CAP-29 | TV · `Lista` · `Scripted(FailOnCreate)` | libre a los **2 s** (QA-G4aT-12: el alta falla en t ≈ 0 y el aviso dura 4 s, así que a los 5 s ya no estaba) | `#toast` entero `No se pudo guardar la sesión` en el hueco entre la banda y los controles: `#toast.yMax + 8 ≤ #hud-controls.yMin` y `#toast.yMin ≥ #hud-band.yMax` (el reloj a la vista, DN4a-21); `#hud-time` `00:02` (el reloj andando) |
| CAP-30 | TV · `F4A` | `Sin meta` a los 3 s | `#hud-countdown` `?`; sin `#hud-up-next` |
| DEMO-01…14 | W1280 · `F4A` · escritorio · `Sequence` | spec §7.4, un `Shot` por paso: 01 `Tap(tab-workout)`; 02 libre a los 5 s; 03 `sim-open`, `sim-watts-plus`, `Advance(1)`; 04 `sim-close`, `hud-pause`; 05 `hud-resume`, `hud-finish`; 06 `dialog-confirm`, `Settle`; 07 `sim-open`, `sim-watts-minus`, `sim-close`, tarjeta `Prueba 4a`; 08 `Iniciar`, `Advance(18)`; 09 hasta los 45 s; 10 `sim-watts-plus`, `Advance(1)`; 11 hasta los 120 s, `sim-spm-minus`, `Advance(5)`; 12 hasta los 237 s; 13 `Advance(4)`, `Settle`; 14 `tab-device`, `device-disconnect`, `tab-workout` | lo de spec §7.4 en cada toma |
| VID-01 | TV · `F4A` · 17 s | `Prueba 4a` a los 14,5 s y se graba hasta los 31,5 | `Próximo` entra a los 0,5 s del video; por terminar desde los 10,5; el cambio a `Trabajo` a los 15,5; ningún salto de caja entre cuadros (DT4a-04) |
| VID-02 | TV · `F4A` · 10 s | `Prueba 4a` a los 35 s (objetivo 22, a 24); 1 `sim-spm-minus` (23) · 2 `-minus` (22) · 3 `-minus` (21: **primer cruce**) · 4,5 `sim-spm-plus` (22) · 5,5 `-minus` (21: **segundo cruce**, a menos de 4 s del primero) · 7 `-plus` (22) · 8,5 `-plus` (23). Los dos cruces en menos de 4 s, como piden la spec (§7.5) y las notas de diseño (QA-G4aT-12) | el color de SPM cruza en 150 (0,053 / 0,350 / 0,851 / 0,989 en los cuadros 1 a 4 de cada cruce); la flecha sube desde 4 dp en 120; **un solo empujón**, en el primer cruce (pico 1,0398 en su cuadro 3); el segundo cruce, sin empujón; la vuelta a blanco sin parpadeo; el número siempre el de la lectura |
| VID-03 | TV · `Lista` · 10 s | libre a los 3 s; 1 `hud-pause` · 2,5 `hud-resume` · 4 `hud-finish` · 5,5 `dialog-cancel` · 7 `hud-finish` · 8,5 `dialog-confirm` | el botón cambia en el cuadro del toque; el diálogo entra y sale con su transición; el inmersivo se funde y vuelve Workout |

Las sondas de aspecto del Designer se suman en B3 con `WithProbe(…, aspect: true)`, con los valores de 04-design-notes §8.2 y sus reglas de §8.1 (DN4a-21): tubería `GammaRt`, ± 1 por canal en fondos, ± 3 en trazos de texto y ± 2 en los valores del HUD. DEMO-03 comprueba además el panel abierto desde la esquina de abajo a la derecha de la llave (a la derecha de `Pausa`), crecido hacia arriba y a la izquierda, con Split y Watts a la vista (AC-BLE-16-03).

### 7.4 Regeneración de las It. 2 y 3

B0 regenera `it2` e `it3` una sola vez con las tablas de kerning (DD4a-06, con K2 sobre la derivada 700: DN4a-22), la derivada tabular (DD4a-07) y la llave de D-ART-10: cambian las tomas con títulos o números (todas) y las de la llave, con lo que dice la tabla de la spec §5 (en `it3`: CAP-01, -06, -09, -18, -19, -28 y las sondas de AC-RUT-10-04 y -09; en `it2`: CAP-01, el panel de CAP-16 y todas las de Dispositivo con simulador). Regla de QA-V3-06: dos corridas seguidas con los mismos bytes antes de versionar. La comparación contra `HEAD` va al informe de B0.

---

## 8. Tests

### 8.1 Suites, dobles y comandos

| Suite | Qué | Comando |
|---|---|---|
| dotnet | Core (motor, reloj, programador, formatos, geometría), App (*stores*), Infrastructure con SQLite real, Simulator (llave) y la herramienta de la demo | `powershell -NoProfile -File tools/dotnet/run-tests.ps1 -ArtifactsPath <scratch>/it4a/<carpeta>` (y `-Matrix -UnityResults …`) |
| Unity EditMode, con y sin simulador | las mismas fuentes con el SQLite de Unity; `Tests/UnityEditMode` (pantalla encendida, programador de Unity, fuentes) | `tools/unity/run-unity-tests.ps1 [-Simulator]` |
| Unity PlayMode, con y sin simulador | la UI montada con base de prueba, reloj, ciclo de vida, remo y pulsómetro de prueba | ídem |
| Arnés | §7 | `tools/unity/capture-snapshots.ps1 -Set it4a` |
| Oráculo del motor | §3.7 (solo para regenerar los vectores) | `powershell -NoProfile -File tools/oracle/run-oracle.ps1 -Engine` |
| Builds | normal Mono, simulador Mono (siempre a `Builds/Windows/simulator`, QA-V3-08), IL2CPP (3 = NO VERIFICABLE sin módulo) | `tools/build/build.ps1 …` |

Dobles (Tests/Support, nunca en un build): `ScriptedRower` y `RowerScripts` (R-4 y sus variantes), `ScriptedHeartRateService` (It. 2: bpm y estados a pedido), `InMemorySessionRepository`, `ScriptedSessionRepository` (demoras por el `FakeScheduler`, fallas a pedido al crear, al escribir el lote n —una vez o siempre— o al cerrar, `Never`, cuenta de llamadas y orden), `FakeAppLifecycle` (segundo plano y foco), `FakeWakeLock`, `LifecycleHarness`, `TempDatabase.Store()`, `InlineDbExecutor`, el `SerialExecutor` real para la durabilidad y `TestMainThreadContext` (It. 1) para que las continuaciones de la base vuelvan al hilo de la prueba, como en Unity.

### 8.2 Archivos de test

dotnet + Unity EditMode (WP-A, `Tests/EditMode/`, salvo lo marcado):

| Archivo | Abrev. | Cubre |
|---|---|---|
| `Workout/WorkoutEngineLifecycleTests.cs` | WEL | AC-WKT-01-01 a -08, -11; AC-WKT-02-02, -03; AC-WKT-03-05; AC-WKT-06-10, -11 (también el cierre tardío); AC-BLE-10-07; `WEL.OneSnapshotPerReturn` (QA-G4aT-07) |
| `Workout/ActiveClockTests.cs` | ACK | AC-WKT-01-05 (Q-58), AC-WKT-02-01, -04 (600 s con un cuadro tarde) |
| `Workout/WorkoutBackgroundTests.cs` | WBG | AC-WKT-02-05, -06 (también la vuelta a los 330 s con `SerialExecutor` y `TestMainThreadContext`: `Ended(Completed, true)`, la última terminada y ningún `workoutSaveError`; QA-G4aT-03), -07; AC-WKT-06-15; el alcance de 1 h y de 8 h con tope (QA-G4aT-07) |
| `Workout/StepAdvanceTests.cs` | SAD | AC-WKT-01-03; AC-WKT-03-01 a -04, -06 a -10; AC-WKT-09-07; el inicio sin lectura previa (QA-G4a-P06) |
| `Workout/SanitizedDistanceTests.cs` | SDI | Q-55 (`series_tracker.dart:118-127`): negativos, más de 10 m/s, `Rebase`, paquete sin distancia; las afirmaciones de metros de los 4 casos del grupo `_hardening` de `test/series_tracker_test.dart:155-220` (§8.3; QA-G4a-P06) |
| `Workout/StepProgressTests.cs` | SPG | AC-WKT-04-01 a -07 (también el último paso por distancia); DT4a-12 (DV-37) |
| `Common/ForegroundTimeoutTests.cs` | FTO | el tope que no cuenta el segundo plano: corre en primer plano, se detiene y se rearma con lo que le quedaba, arranca entero si se creó en segundo plano, `Dispose` lo cancela (QA-G4aT-03) |
| `Workout/WorkoutDeviceChecksTests.cs` | WDC | las partes de equipo como `[Ignore("NO VERIFICABLE: …")]` con la categoría del AC, así la matriz dice IGNORADO y no SIN TEST: AC-WKT-02-10, y las de [Device-And] de AC-WKT-02-08, AC-WKT-13-03 y AC-APP-14-02 (QA-G4aT-09) |
| `Workout/WorkoutProviderHrTests.cs` | (LT) | LT-35 a LT-38; AC-WKT-05-05 |
| `Workout/StepTargetTrackerTests.cs` | (LT) | LT-39 a LT-44 |
| `Workout/TargetLightTests.cs` | TLT | AC-WKT-10-01 a -04, -06 (regla) |
| `Workout/WorkoutFormatsTests.cs`, `Workout/SeriesPanelsTests.cs` | WFT, (LT) | AC-WKT-11-01 a -16; LT-45 (`formatos`) |
| `Workout/WorkoutStartRuleTests.cs` | WSR | AC-WKT-12-08 (regla y costo; el split negativo enorme no desborda, QA-G4aT-11), AC-RUT-06-08 |
| `Workout/TelemetryTests.cs` | TEL | AC-WKT-06-01 a -04, -06, -08, -09 |
| `Workout/SessionRecorderTests.cs` | SRC | AC-WKT-06-05 (en el acto), -12 (el lote fallado se reintenta en el cierre), -13 (orden y lotes), `Abandon`, `FlushAndWait`; el alta lenta (un trabajo que bloquea el `SerialExecutor` real) y el paso a segundo plano a los 6 s: el punto del segundo 5 queda escrito detrás del alta (QA-G4aT-06) |
| `Workout/WorkoutGoldenTests.cs` | WGD | AC-WKT-06-07 (vectores del motor legacy), AC-WKT-03-03 (columna "Legacy") |
| `Infrastructure/SqliteSessionRepositoryTests.cs` | SSR | AC-WKT-06-01, -05 (cortado a los 31,5 s: inline y con `SerialExecutor`, leído desde otra conexión), -06, -10, -12 (un lote que falla una vez entra en la transacción del cierre y la sesión queda cerrada con todos sus puntos; uno que falla siempre deja la sesión cortada, sin punto final ni cierre; QA-G4aT-13), -14 (después de `Corta`, solo hay filas en `workout_sessions` y `data_points`, con las columnas de v8); AC-HIS-17-07, AC-HIS-17-02 (esquema igual), AC-HIS-19-01, -02; cierre atómico (un *trigger* que falla en el `UPDATE` deja la sesión cortada y sin punto final); `AddPointsAfterCreateAsync` detrás del alta, también con el alta que falla |
| `Infrastructure/LocalStoreTests.cs` | LST | una base para los dos repositorios; `Dispose` termina lo encolado |
| `Common/FrameSchedulerTests.cs` | FSC | `Tick` = `RunDue` de la It. 2; alcance en orden, periódicos sin reanclar, hora virtual, `IsCatchingUp`, tope de llamadas; la hora del cuadro; `ResumeWithoutEnterIsANoOp`; `ResumeWithASmallerMonotonicJumpKeepsOrder` (RA-19, QA-G4aT-10) |
| `Common/ScreenKeepAwakeTests.cs` | SKA | AC-APP-14-04; AC-APP-14-02 (`RequestsAgainOnResumeAndFocus`: vuelve a pedir al volver del segundo plano y al recuperar el foco, y no después de `Release`; QA-G4aT-09) |
| `Workout/WorkoutStoreTests.cs` | WST | AC-WKT-12-03, -05, -07, -08 (*store*); AC-BLE-10-03 (la regla, con los cuatro estados sin remo: `Off`, `Scanning`, `Connecting`, `Disconnected`; QA-G4a-P07), -04, -06; AC-WKT-01-10 (*store*); avisos de AC-WKT-01-11, -03-05, -06-12; los avisos retenidos sin el usuario delante, uno solo y una vez (AC-WKT-02-06, -08, -09; QA-G4a-P08); DT4a-17 |
| `Workout/WorkoutHudStoreTests.cs` | WHS | textos y semáforo del HUD (AC-WKT-10-05 en datos, AC-WKT-11-13, AC-WKT-14-02 a -05 en datos, AC-WKT-03-08), es / en; un solo estado, con `Instant`, después del segundo plano |
| `Design/HudGeometryTests.cs` | HGT | `hudScale` (0,92 / 1,054 / 1,6), anclas × `s`, la fila de abajo en horizontal, `BoatCenterY` (0,504 / 0,652), el horizonte (0,40 / 0,42), T1 a T5, el lugar de la llave; los rectángulos de 04-design-notes §3.8 (± 0,1) y las escalas de §3.7 (± 0,005); ninguna pieza se cruza en las 60 combinaciones (5 perfiles × 6 casos × con y sin simulador; AC-WKT-17-08 en geometría) (DN4a-13, DN4a-14) |
| `Design/MotionSpecsTests.cs` | (C) | las constantes de 04-design-notes §5.13 (DN4a-17) |
| `Models/RoutineExpansionTests.cs` | (C) | `Flatten(…, Saturate)`: satura en `int.MinValue` y `int.MaxValue`; `Flatten(template)` sigue lanzando `OverflowException` (QA-G4aT-11) |
| `Localization/CatalogParityTests.cs`, `L10nKeysTests.cs` | (C) | AC-APP-10-10: 202 claves |
| `Project/PureCodeRulesTests.cs` | (C) | sin reloj del sistema en `Core/Workout` y `App/Workout`; Infrastructure no conoce objetivos ni parciales (AC-WKT-06-14) |
| `Project/BootstrapperStaticTests.cs` | (C) | `PlatformMinimumsComeFromAppIdentity` (`runInBackground = true`, `:141`) suma la categoría de AC-WKT-02-09 (la parte de configuración; QA-G4aT-09) |
| `Project/LegacyDiscardTests.cs` | (C) | AC-WKT-23-01, la parte de la app nueva (como AC-APP-08-01): ningún tipo, archivo ni clase de USS porta la pantalla clásica (`FullscreenWorkout*`, `ClassicWorkout*`). Que el legacy no la abre lo confirma QA (el único `_FullscreenWorkoutPage(` es su declaración, `workout_screen.dart:206`) |
| `Project/RepoHygieneTests.cs` | RHT | `ToolsHaveNoMachinePaths`: ningún archivo de texto de `tools/` (salvo lo ignorado, que también tiene que estar en el `.gitignore`) con una ruta de la PC (§3.7; QA-G4aT-05) |
| `Project/SessionListToolTests.cs` | SLT | herramienta de la demo: no toca la base, sin rutas en la salida ni en los errores (archivo que falta y archivo bloqueado; spec §12.9; QA-G4aT-18) |
| `Project/ItFourAToolingTests.cs` | ITT4 (dB, [B]) | `capture-snapshots.ps1 -Set it4a`, `WrenchAudit` en el arnés, ninguna ruta de la PC en el *manifest* |
| `Project/ItFourAFontTests.cs` | IFT (dB, [B]) | K3 y `m_GetFontFeatures: 0` (DD4a-06); nada nombra los `FontAsset` de Barlow (DN4a-22) |
| `Traceability/It04aIds.cs`, `TraceabilityTests.cs` | (N/C) | los 162 IDs y los 11 LT, con `UnityOnlyIds` (§8.4) |
| `Tests/Simulator/WrenchPlacementTests.cs` | WPL (s) | DD4a-01: los rectángulos de 00-deuda-diseno §1.4 (carril izquierdo, ± 0,1) con margen seguro; en el entrenamiento, el lugar de la llave de 04-design-notes §3.8, que `WrenchPlacement` devuelve tal cual desde `OverlaySurface.WrenchSlot` (reemplaza a §1.5; DN4a-14); `Clear` en los cinco perfiles; reemplaza `FabBottomAvoidsTheFab` |
| `Tests/Simulator/SimulatorPanelLayoutTests.cs` | SPL (s, C) | el alto máximo desde el borde de abajo del panel (DD4a-03): pestañas 248 / 802 / 120, editor 312 / 866 / 152, entrenamiento vertical hasta 8 dp debajo de Watts con su objetivo (TV: 316, ± 0,5); la esquina y el lado de crecimiento (AC-BLE-16-03) |

Unity (WP-B):

| Archivo | Abrev. | Cubre |
|---|---|---|
| `PlayMode/WorkoutScreenTests.cs` | WSC | AC-WKT-12-01 a -09; AC-BLE-10-03 (los cuatro estados sin remo), -04, -06; AC-WKT-01-09 (sin la barra superior fuera de reposo), -10; AC-APP-01-09, -02-08, -05-05, -05-07; AC-RUT-10-11 |
| `PlayMode/ImmersiveHudTests.cs` | IHT | AC-WKT-13-01, -02, -04, -05; AC-WKT-14-01 a -06 (con DV-38); AC-WKT-16-01; AC-WKT-17-01 a -07 (con la fila de abajo, la banda compacta y las unidades `.hud @80 %`); AC-WKT-10-05, -07, -08; AC-WKT-03-08 (pantalla); AC-SCN-01-01 (el cielo y el agua); AC-APP-06-01; AC-APP-05-08 |
| `PlayMode/HudLayoutTests.cs` | HLT | AC-WKT-17-08, -09, -10 (con estados del HUD sintéticos: los extremos de FTMS sin correr 10 h de motor); las escalas de 04-design-notes §3.7 en la pantalla real |
| `PlayMode/HudControlsTests.cs` | HCT | AC-WKT-20-01 a -07 (`RoutineEndsWithTheFinishDialogOpen`, `FinishTapAfterTheEndDoesNothing`, `DialogClosesWhenTheRoutineEndsInBackground`; QA-G4aT-04); el *spinner* de `Terminar` con un cierre de más de 300 ms (DN4a-20); AC-WKT-13-03, -04; AC-WKT-01-06 (pantalla); AC-WKT-03-05 (pantalla); AC-APP-03-09, -10 |
| `PlayMode/WorkoutPersistenceTests.cs` | WPS | AC-WKT-01-02 (pantalla), -11 (pantalla); AC-WKT-06-12, -13 (pantalla); AC-APP-03-11 |
| `PlayMode/WorkoutBackgroundUiTests.cs` | WBU | AC-WKT-02-08 (con `IsCatchingUp` durante el `Advance` y `Instant` en el primer estado: sin rodillos, sin empujón, los pulsos con origen en el primer cuadro real; QA-G4aT-07); AC-WKT-02-06 en pantalla (`CompletedToastOnceOnReturn`: el aviso una vez, con sus 4 s desde la vuelta; QA-G4a-P08) |
| `PlayMode/HudBudgetTests.cs` | HBT | ≤ 60 elementos en el peor caso (rutina, objetivos en SPM y Watts, BPM, `Próximo` y el *spinner* del guardado) y 0 B por cuadro (ARCHITECTURE §7.8) |
| `PlayMode/BootSceneTests.cs` | BOOT (C) | `LocalStore`, ciclo de vida y pantalla encendida con `ROWMATE_DATA_DIR` |
| `PlayModeSimulator/WorkoutSimulatorTests.cs` | WSM | AC-WKT-05-06; AC-WKT-10-09; AC-WKT-03-10 (pantalla); AC-BLE-10-05; AC-BLE-16-01 |
| `PlayModeSimulator/SimulatorOverlayTests.cs` | (C) | AC-BLE-16-02 y -03 con D-ART-10 (DV-39): el carril izquierdo fuera del entrenamiento, el lugar del HUD en el inmersivo, el cambio de lugar con fundido, el panel que crece desde la llave y la **sigue** con el panel abierto (también cuando el inmersivo se cierra solo), el tope de Watts, menús que esquivan la llave (DD4a-02), la llave en el anillo de Tab del inmersivo; AC-RUT-10-04 (el `+` entero, con el panel cerrado y abierto desde 368 dp) y AC-RUT-10-09 (la última tarjeta sobre el `+` y la llave) |
| `UnityEditMode/WakeLockTests.cs` | WLK | el pedido de Windows se acepta y se libera (`SetThreadExecutionState` ≠ 0) en el mismo hilo que lo hizo (`ReleaseRunsOnTheRequestingThread`); Android por el camino de código |
| `UnityEditMode/UnitySchedulerTests.cs` | USC | `EnterBackground` / `ResumeFromBackground` con un reloj inyectado |
| `UnityEditMode/FontAndIconAssetsTests.cs` | (C) | la derivada: diez cifras con el mismo avance (DD4a-07) |

### 8.3 Casos legacy

| LT | Legacy (`// legacy:` con el nombre exacto) | Test | AC |
|---|---|---|---|
| LT-35 | `test/workout_provider_hr_test.dart:80` 'el pulsómetro manda sobre el FTMS y se actualiza sin paquetes del remo' | `WorkoutProviderHrTests.SensorBeatsFtmsAndUpdatesWithoutRowerPackets` (2 avisos) | AC-WKT-05-01 |
| LT-36 | `:125` 'orden real de caída: bpm 0 y después disconnected' | `WorkoutProviderHrTests.RealDropOrderZeroThenDisconnected` | AC-WKT-05-02 |
| LT-37 | `:148` 'un bpm recibido durante connecting se conserva al pasar a connected' | `WorkoutProviderHrTests.BpmDuringConnectingIsKeptWhenConnected` | AC-WKT-05-03 |
| LT-38 | `:167` 'connected del sensor sin bpm previo no toca los datos' | `WorkoutProviderHrTests.ConnectedWithoutPreviousBpmTouchesNothing` (0 avisos) | AC-WKT-05-04 |
| LT-39 | `test/step_target_tracker_test.dart:20` 'cumplido cuando el promedio de vatios alcanza el mínimo' | `StepTargetTrackerTests.MetWhenTheWattsAverageReachesTheMinimum` | AC-WKT-09-01 |
| LT-40 | `:28` 'no cumplido si falla cualquiera de los objetivos definidos' | `StepTargetTrackerTests.MissedIfAnyDefinedTargetFails` | AC-WKT-09-02 |
| LT-41 | `:35` 'el split se cumple por debajo del objetivo (más rápido)' | `StepTargetTrackerTests.SplitIsMetBelowTheTarget` | AC-WKT-09-03 |
| LT-42 | `:46` 'un paso sin objetivos no tiene resultado' | `StepTargetTrackerTests.StepWithoutTargetsHasNoResult` | AC-WKT-09-04 |
| LT-43 | `:53` 'sin lecturas remando el objetivo no se cumple; los ceros no cuentan' | `StepTargetTrackerTests.WithoutRowingItIsMissedAndZerosDoNotCount` | AC-WKT-09-05 |
| LT-44 | `:65` 'un paso no completado no tiene resultado y reset borra todo' | `StepTargetTrackerTests.UncompletedStepHasNoResultAndResetClears` | AC-WKT-09-06 |
| LT-45 | `test/series_panels_test.dart:32` 'formatos' | `SeriesPanelsTests.Formats` (`2:46.7`, `1:05.0`, `1:54`, `1.245 m`) | AC-WKT-11-09, -10, -11 |

Convención de ARCHITECTURE §15.3 y de las It. 1 y 2: el método en inglés y el nombre del legacy, al carácter, en `// legacy: test/…:línea 'nombre'`, con `[Category("LT-nn")]` y la del AC. Los dobles son los del legacy: el remo y el pulsómetro como *broadcast* sin estado inicial (`emitCurrent: false`) y el conteo de avisos sobre `WorkoutEngine.State`. Los valores del legacy que usa la spec (`Corta`: 7 puntos; la columna "Legacy" de `Distancia 100`: 25, 285, 25 y 11; Q-58: el segundo 11 a los 41,7 s; 3 puntos y 1048 m a los 12,3 s) coinciden con la sonda S4-a (Anexo A). La columna nueva de `Distancia 100` (25, 26, 27 y 25) sale del saneamiento de §2.4, hecha a mano con las lecturas de `R-4`, `R-4R` y `R-4Z`.

**Casos legacy portados en parte (completan en la It. 5)** (QA-G4a-P06). La 4a porta el saneamiento de `series_tracker.dart:118-127` como `SanitizedDistance`, y el paso por distancia depende de él (Q-55). Los casos que lo prueban en el legacy son el grupo `_hardening` de `test/series_tracker_test.dart:155-220`, que el ROADMAP §5 asigna enteros a la It. 5 (`series_tracker_test`, 12 casos). SDI porta **ya** sus afirmaciones de **metros**, con su nombre en `// legacy:` y `[Category("Q-55")]`; las de vueltas y repeticiones quedan para la It. 5, que les da su número de LT. En la matriz y en el informe van como **parciales: completan en la It. 5**.

| Legacy (`// legacy:` con el nombre exacto) | Test de la 4a | Lo que afirma en la 4a (los metros saneados) | Lo que queda para la It. 5 |
|---|---|---|---|
| `test/series_tracker_test.dart:155` 'una lectura de 0 dentro del paso no genera metros negativos ni dobles' | `SDI.ZeroReadingInsideTheStep` | con lecturas de 4 m por segundo hasta 1000, una lectura de 0 y después de 1004 a 1100: los metros nunca bajan de 0 y terminan en 1096, dentro del rango del legacy (1060 a 1110) y no dobles | las 2 vueltas de ≈ 125 s y los metros de trabajo de la repetición |
| `:180` 'un primer salto desde 0 no crea vueltas falsas' | `SDI.FirstJumpFromZero` | `Begin(0)` y una lectura de 5000: 0 m; 140 lecturas más de 4 m: 560 m | la vuelta única de ≈ 125 s |
| `:191` 'un reinicio del monitor no genera valores negativos' | `SDI.MonitorResetNeverGoesNegative` | 200 lecturas de 4 m (800) y un reinicio a 0 con 60 lecturas más: los metros nunca bajan y terminan en 1040 | las vueltas después del reinicio |
| `:211` 'rebase no cuenta el salto al reanudar' | `SDI.RebaseIgnoresTheJumpOnResume` | 5 lecturas de 4 m (20), `Rebase(9000)` y 9004: 24 | — (el caso es solo de metros: queda completo, y la It. 5 lo vuelve a correr sobre `SeriesTracker`) |

Desde el motor, `SAD.StartWithoutAPreviousReadingDropsTheFirstJump` (§2.4): una rutina iniciada sin lectura previa, con el monitor en 5000 m, termina el paso de 100 m en el segundo 26; el legacy, en el 1 (DV-26).

### 8.4 Matriz AC → test / captura

Marcas: **d** dotnet (y Unity EditMode) · **dB** dotnet en un archivo de WP-B · **s** dotnet con simulador · **u** Unity EditMode solo · **p** PlayMode · **ps** PlayMode con simulador · **cap** captura · **eq** equipo (QA) · **qa** revisión de QA. Son los **162 IDs** de la spec corregida (147 nuevos, con AC-WKT-20-07, y 15 de §5, con AC-BLE-16-03, AC-RUT-10-04 y AC-RUT-10-09). Como en las It. 2 y 3, ningún ID queda solo con **eq** o **qa**: `TraceabilityTests` falla con uno así, y `run-tests.ps1 -Matrix` lo daría SIN TEST (QA-G4aT-09). Las partes de equipo sin módulo ni teléfono llevan un test `[Ignore("NO VERIFICABLE: …")]` con la categoría del AC en `WorkoutDeviceChecksTests` (WDC): si es lo único que tiene el AC, la matriz dice IGNORADO.

| AC | Verif. | Tests / capturas |
|---|---|---|
| WKT-01-01 | d | `WEL.FourPhasesStartingIdle`, `WEL.ReadingsArriveInEveryPhase` |
| WKT-01-02 | d p | `WEL.StartFreeCreatesTheSessionThenTheClock`, `WEL.ClockNeverWaitsMoreThan100Ms` (con un alta de 2 s: la condición de QA para DT4a-02); `WPS.FreeStartsAtOnce` |
| WKT-01-03 | d | `WEL.RoutineStartExpandsAndPositions`, `WEL.SessionKeepsTheTemplateIdAndName` (renombrar y borrar después) |
| WKT-01-04 | d | `WEL.PauseFreezesClocksSamplingTargetsAndMeters` |
| WKT-01-05 | d | `ACK.ResumeRestartsTheRhythm` (10,7 + 30 → 41,7), `SAD.ResumeRebasesTheMeters` |
| WKT-01-06 | d p | `WEL.FinishSavesThenGoesIdle`; `HCT.FinishClosesTheImmersiveAfterTheSave` |
| WKT-01-07 | d | `WEL.IdleKeepsTheLastReadingAndTheLastFinished` |
| WKT-01-08 | d | `WEL.NoUndoAndStartOnlyFromIdle` |
| WKT-01-09 | p | `WSC.InProgressTextOutsideIdle` (con `#workout-appbar` en `none`: sin la barra superior, QA-G4a-P09); `WSC.StartDuringThePreviousExitHasItsOwnImmersive` (ronda 1: una sesión iniciada durante la salida del inmersivo anterior tiene el suyo, QA-V4a-01) |
| WKT-01-10 | d p | `WST.SecondStartIsIgnored`; `WSC.TwoTapsOneSession` (libre e `Iniciar`) |
| WKT-01-11 | d p cap | `WEL.CreateFailureStillRuns`, `WST.SaveErrorOnStartAndAtTheEnd`; `WPS.CreateFailureShowsTheToast`; CAP-29 |
| WKT-02-01 | d | `ACK.OneSecondPerActiveSecondOnly` |
| WKT-02-02 | d | `WEL.SecondOrderIsTheLegacyOrder` |
| WKT-02-03 | d p | `WEL.HudClockIsTheActiveClock` (`00:12` y 312); `IHT.BandShowsTheActiveClock` |
| WKT-02-04 | d | `ACK.SixHundredSecondsWithALateFrame` (`LifecycleHarness.Stall(2,5)`); `WHS.OneInstantStateAfterALateFrameInTheForeground` (ronda 1: 1 h en un cuadro, una foto con `Instant`; QA-V4a-ROB-05) |
| WKT-02-05 | d | `WBG.BackgroundIsLikeForeground` (`Corta`, 8 → 23) |
| WKT-02-06 | d p | `WBG.RoutineEndsWhileInBackground` (7 puntos, fin `13:00:30.000Z`), `WBG.LongBackgroundAfterTheEndStillSaves` (vuelta a los 330 s: `Ended(Completed, true)`, la última terminada y ningún `workoutSaveError`; QA-G4aT-03), `WST.HeldToastShowsOnceWhenFacingTheUserAgain`; `WBU.CompletedToastOnceOnReturn` (QA-G4a-P08) |
| WKT-02-07 | d | `WBG.PausedSessionDoesNotMoveInBackground` |
| WKT-02-08 | d p eq | `WEL.OneSnapshotPerReturn`, `WHS.OneInstantStateAfterBackground`; `WBU.FirstFrameShowsThePresent` (QA-G4aT-07); [Device-And]: `WDC.AndroidFirstFrameAfterBackground` con `[Ignore]` (NO VERIFICABLE; en Windows minimizada no hay vuelta que alcanzar: QA-G4aT-09, -16) |
| WKT-02-09 | d eq | `BootstrapperStaticTests.PlatformMinimumsComeFromAppIdentity` (`runInBackground = true`: la parte de configuración), `WST.HeldToastShowsOnFocus` (el aviso del fin al volver el foco); [Device-Win] |
| WKT-02-10 | d eq | `WDC.AndroidBackgroundTwoMinutes` con `[Ignore("NO VERIFICABLE: …")]`, así la matriz dice IGNORADO y no SIN TEST (QA-G4aT-09); [Device-And]: NO VERIFICABLE sin módulo y teléfono; RA-19 (R4a-12) |
| WKT-03-01 | d | `SAD.Prueba4aExpandsToSixSteps` |
| WKT-03-02 | d | `SAD.TimeStepEndsAtItsDuration` (10, 15, 25, 30), `SAD.TimeWinsOverDistance` |
| WKT-03-03 | d | `SAD.DistanceStepWithTheSanitizer` (25, 26, 27, 25), `SAD.StartWithoutAPreviousReadingDropsTheFirstJump` (26; legacy 1); `SDI.*` (con los 4 casos legacy parciales de §8.3); `WGD.LegacyDistanceColumn` (25, 285, 25, 11) |
| WKT-03-04 | d | `SAD.StepChangeKeepsTheExtraMetersInTheOldStep` |
| WKT-03-05 | d p cap | `WEL.LastStepFinishesAndSaves`, `WST.CompletedToast`; `HCT.CompletedRoutineClosesWithTheToast`; CAP-28, DEMO-13 |
| WKT-03-06 | d | `SAD.FinishedRoutineStaysOnTheLastStep` (último paso por tiempo: `0:00`; por distancia: su largo, sin restante ni progreso; QA-G4a-P10) |
| WKT-03-07 | d p | `SAD.ZeroSecondStepEndsAtTheFirstSecond`, `SAD.NegativeDurationEndsAtTheFirstSecond`; `IHT.NegativeDurationShowsZero` (DV-37) |
| WKT-03-08 | d p cap | `SAD.NoGoalStepNeverEnds`, `WHS.NoGoalCountdownIsQuestionMark`; `IHT.NoGoalShowsQuestionMark`; CAP-30 |
| WKT-03-09 | d | `SAD.FreeHasNoStepsNorEnd` |
| WKT-03-10 | d ps | `SAD.RowerDisconnectKeepsClockAndTimeSteps`; `WSM.SimulatedDisconnectDuringARoutine` |
| WKT-04-01 a -05, -07 | d | `SPG.ExistsOnlyWithRoutineOutsideIdle`, `SPG.Remaining`, `SPG.NegativeDurationShowsZero` (DV-37), `SPG.Progress`, `SPG.EndingSoonOneToFive`, `SPG.UpNextZeroToFifteen`, `SPG.FinishedRoutine` (por tiempo), `SPG.FinishedRoutineByDistance` (QA-G4a-P10) |
| WKT-04-06 | d p cap | `SPG.ExamplesOfTheSpec`; `IHT.UpNextAndEndingSoonOnScreen`; CAP-21, CAP-22 |
| WKT-05-01 a -04 | d | LT-35 a LT-38 (§8.3) |
| WKT-05-05 | d | `WorkoutProviderHrTests.FusionInEveryPhaseFeedsHudAndPoints`, `TEL.PointsCarryTheFusedPulse` |
| WKT-05-06 | ps cap | `WSM.SimulatedHrmShowsAndDropsTheBpmCard`; CAP-13 |
| WKT-06-01 | d | `SSR.CreateWritesTheLegacyRow`, `TEL.StartCreatesTheRow` |
| WKT-06-02 | d | `TEL.PointEveryFiveActiveSeconds` (tabla de la spec) |
| WKT-06-03 | d | `TEL.PointBelongsToTheStepThatEnds` (7 puntos de `Corta`); `WGD.Corta` |
| WKT-06-04 | d | `TEL.NoSamplingInPause` (7 + 30 + 5) |
| WKT-06-05 | d eq | `SSR.KilledAt31_5sHasSixPoints` (inline y `SerialExecutor`), `SRC.PointIsWrittenAtOnce`; [Device-Win] |
| WKT-06-06 | d | `TEL.FinishWritesTheFinalPointAndTotals` (12,3 s) |
| WKT-06-07 | d | `WGD.SessionsAreIdenticalToTheLegacy`, `TEL.LastTwoPointsEqualWhenEndingOnAFive`; `WGD.SanitizerScenariosMatchUntilTheFirstStepChange` (ronda 1: los 3 escenarios de Q-55 hasta el primer cambio de paso; QA-V4a-L02, parcial) |
| WKT-06-08 | d | `TEL.MonitorCountersAreSaved` (Q-15) |
| WKT-06-09 | d | `TEL.ShortAndUnrowedSessions` |
| WKT-06-10 | d eq qa | `SSR.CutSessionIsOpenWithItsPoints`, `WEL.ReopenDoesNotTouchACutSession`; [Device-Win]; herramienta de la demo; `WBG.QuitRightAfterFinishKeepsTheClose` (ronda 1: cerrar la ventana justo después de un Terminar confirmado, con y sin un lote en vuelo, deja la sesión cerrada; QA-V4a-ROB-04) |
| WKT-06-11 | d | `WEL.LastFinishedOnlyWhenTheCloseSucceeds`, `WEL.LateCloseStillBecomesTheLastFinished`, `WBG.LongBackgroundAfterTheEndStillSaves` |
| WKT-06-12 | d p cap | `SRC.FailuresNoticeOnceAndKeepWriting`, `SRC.FailedBatchIsRetriedInTheClose`, `SSR.CloseRetriesTheFailedBatchInOneTransaction`, `SSR.PersistentPointFailureLeavesTheSessionCut` (QA-G4aT-13), `WST.SaveErrorToast`, `WST.TransientFailureRecoveredAtTheClose` (DT4a-17); `WPS.PointAndCloseFailures`; CAP-29 |
| WKT-06-13 | d p | `SRC.SlowDatabaseKeepsOrderAndBatches`, `WEL.SlowDatabaseDoesNotDelaySeconds`; `WPS.SlowDatabaseHudStaysCurrent` |
| WKT-06-14 | d qa | `SSR.NothingButSessionsAndPointsIsWritten`, `PureCodeRulesTests.InfrastructureDoesNotKnowTargetsOrSplits` (QA-G4aT-09); revisión: nada de parciales, repeticiones ni objetivos en la base |
| WKT-06-15 | d | `WBG.BackgroundFlushesPendingWrites` (con `SerialExecutor` y otra conexión), `SRC.PointsTakenWhileCreatingAreFlushedOnBackground` (alta lenta y segundo plano a los 6 s; QA-G4aT-06), `SSR.PointsAfterCreateWaitForTheCreate`; ronda 1: `WBG.BackgroundFlushAlsoSendsTheFailedBatchAgain` (QA-V4a-ROB-03) y `WBG.FinishThenBackgroundWithABatchInFlightStillCloses` (QA-V4a-ROB-04) |
| WKT-09-01 a -06 | d | LT-39 a LT-44 (§8.3) |
| WKT-09-07 | d | `SAD.TargetsFromTheEngine` (`Corta` con `R-4` y `R-4(19)`; `Terminar` sin resultado; el último paso se evalúa) |
| WKT-10-01 | d p | `TLT.OnlyCardsWithTheirTarget`; `IHT.TrafficLightCards` |
| WKT-10-02 a -04 | d | `TLT.Spm`, `TLT.WattsIgnoresTheMax`, `TLT.SplitAndNonPositiveTargets` (`--:--` contra 130, 0 y −5 → cumple, cumple y fuera; 125 contra 0 → fuera; QA-G4a-P01, QA-G4aT-08) |
| WKT-10-05 | p cap | `IHT.TargetCardAspect` (el hueco de la flecha desde que empieza el paso, `obj.` alineado con las cifras, `W` sin cambio con el semáforo; DN4a-15); CAP-19, CAP-20, CAP-24 |
| WKT-10-06 | d cap | `WHS.EvaluatedOnEveryReadingAlsoInPause`; VID-02 |
| WKT-10-07 | p | `IHT.StoppedRowerIsOffTarget` (SPM y Watts fuera; Split `--:--` cumple, salvo con un objetivo negativo) |
| WKT-10-08 | p | `IHT.FreeHasNoTrafficLight` |
| WKT-10-09 | ps cap | `WSM.DemoTrafficLight`; CAP-19, DEMO-09 a -11 |
| WKT-11-01 a -16 | d | `WFT.*` (los ejemplos de cada AC); -09 a -11 también LT-45; -16 con cultura `de-DE` y `ar-SA` fijada a mano |
| WKT-12-01 | p cap | `WSC.TitleWithoutActions`; CAP-01 |
| WKT-12-02 | p | `WSC.OrderOfTheRestView` |
| WKT-12-03 | d p cap | `WST.FreeNeedsTheRower`; `WSC.FreeButton`; CAP-01 |
| WKT-12-04 | p cap | `WSC.RoutineCardsWithChevron`; CAP-01, CAP-10 |
| WKT-12-05 | d p | `WST.UsesTheSharedCollection`; `WSC.SharedCollectionWithoutLoading` |
| WKT-12-06 | p cap | `WSC.LoadingErrorAndEmpty`; CAP-03, CAP-06, CAP-07; `WSC.LoadingSpinnersSpinOnlyWhileShown` (ronda 1: el indicador gira solo a la vista) |
| WKT-12-07 | d p cap | `WST.TapConfirmsWhenItCanStart`; `WSC.StartDialog`; CAP-08, DEMO-07 |
| WKT-12-08 | d p cap | `WSR.*` (también `WSR.HugeNegativeSplitStartsWithoutOverflow`, QA-G4aT-11), `WST.CannotStartToastWithoutSession`; `WSC.CannotStartRoutine`; CAP-09 |
| WKT-12-09 | p cap | `WSC.EverythingScrollsTogether`; CAP-05 |
| WKT-20-01 | p cap | `HCT.ControlsLayoutAndAspect`; CAP-12, CAP-14 |
| WKT-20-02 | p | `HCT.PauseAndResumeInTheSameFrame` |
| WKT-20-03 | p cap | `HCT.FinishConfirmation` (el diálogo opaco de la app, sin `.rm-dialog--hud`; QA-G4a-P05); CAP-26 |
| WKT-20-04 | p | `HCT.ConfirmGoesBackWithoutSummary` |
| WKT-20-05 | p | `HCT.FinishFromPause` |
| WKT-20-06 | p | `HCT.ControlsDisabledWhileSaving` (con el *spinner* después de 300 ms, DN4a-20), `HCT.DoubleConfirmClosesOnce` |
| WKT-20-07 | p | `HCT.RoutineEndsWithTheFinishDialogOpen`, `HCT.FinishTapAfterTheEndDoesNothing`, `HCT.DialogClosesWhenTheRoutineEndsInBackground` (QA-G4a-P02, QA-G4aT-04) |
| WKT-23-01 | d qa | `LegacyDiscardTests.NoClassicWorkoutScreenPort` (la parte de la app nueva); revisión: `_FullscreenWorkoutPage` sin navegación en el legacy (QA-G4aT-09) |
| BLE-10-03 | d p cap | `WST.AllFourNoRowerStates` (la regla); `WSC.BannerWithoutRower` (los cuatro estados: `Off`, `Scanning`, `Connecting`, `Disconnected`; QA-G4a-P07); CAP-02 |
| BLE-10-04 | d p cap | `WST.FreeNeedsTheRower`; `WSC.DisabledWithoutRower`; CAP-02 |
| BLE-10-05 | ps cap | `WSM.BannerFollowsTheRowerLive`; DEMO-14 |
| BLE-10-06 | d p | `WST.StartRoutineDoesNotCheckTheRower`; `WSC.ConnectionIsCheckedOnTap` |
| BLE-10-07 | d | `WEL.EngineIgnoresTheConnection` |
| APP-14-01 | u eq | `WLK.WindowsRequestIsAccepted`; [Device-Win] (`powercfg /requests`) |
| APP-14-02 | d eq | `SKA.RequestsAgainOnResumeAndFocus` (la regla; QA-G4aT-09); [Device-Win]; [Device-And]: `WDC.AndroidScreenStaysOnAfterResume` con `[Ignore]` (NO VERIFICABLE) |
| APP-14-03 | u eq | `WLK.ReleaseClearsTheRequest`; [Device-Win] |
| APP-14-04 | d qa | `SKA.RejectedRequestOnlyLogsOnce`; revisión |
| WKT-13-01 | p | `IHT.OpensAtOnceOverTheApp`; `WSC.StartDuringThePreviousExitHasItsOwnImmersive` (ronda 1, QA-V4a-01) |
| WKT-13-02 | p cap | `IHT.LayerOrder`; CAP-12, CAP-18 |
| WKT-13-03 | p eq | `HCT.BackOpensTheConfirmation`, `HCT.EscapeWithoutDialogDoesNothing`; [Device-And] (`WDC.AndroidBackOpensTheConfirmation` con `[Ignore]`, documental: sin resultados de Unity el AC sigue en PENDIENTE UNITY) |
| WKT-13-04 | p | `IHT.ClosesItselfAtIdle`, `HCT.RoutineEndsWithTheFinishDialogOpen` (ningún diálogo del inmersivo sobre Workout en reposo); ronda 1: `IHT.LeavingTheImmersiveStopsItsContinuousMotions` (nada del HUD queda en el `MotionDriver`, QA-V4a-02) y `WSC.StartDuringThePreviousExitHasItsOwnImmersive` (QA-V4a-01) |
| WKT-13-05 | p | `IHT.NoRoutineName` |
| WKT-14-01 a -03, -05, -06 | p cap | `IHT.StepRowContainer` (40 de alto, DN4a-04), `IHT.StepTypeColor`, `IHT.TargetPill` (texto blanco, DV-38), `IHT.Countdown`, `IHT.StepRowInPauseAndAtTheEnd`; CAP-18, -19, -20, -22, -23 |
| WKT-14-04 | p cap | `IHT.UpNextChip` (texto blanco al 100 %, fondo `@20 %`; late solo `.rm-hud-up-next__bg`, DV-38); CAP-21, VID-01 |
| WKT-16-01 | p cap | `IHT.FreeTopBar`; CAP-12 |
| WKT-17-01 | d p cap | `HGT.ScaleValues`; `IHT.GlassAndBaseSizes`; CAP-12, -15, -16, -17 |
| WKT-17-02 a -04, -06, -07 | p | `IHT.SpmCard`, `IHT.SplitAndWatts` (`W` en `.hud @80 %`, DV-31), `IHT.Band` (compacta en horizontal, con una sola escala; DV-33), `IHT.LiveValuesAlsoInPause`, `IHT.RotationKeepsTheSession` |
| WKT-17-05 | p cap | `IHT.KcalAndBpm` (columna en vertical; fila a la altura de los controles en horizontal, DV-33); CAP-12, CAP-13, CAP-15 |
| WKT-17-08 | d p cap | `HGT.ZonesDoNotOverlap`; `HLT.NothingOverlaps` (4 ventanas × libre/rutina × pulsómetro × valores largos); CAP-15, -16, -23, -24 |
| WKT-17-09 | d p cap | `HGT.BoatZone` (centro 0,504 / 0,652, DN4a-02); `HLT.BoatZoneIsFree`; CAP-16 |
| WKT-17-10 | p | `HLT.ExtremeValues` |
| HIS-17-07 | d qa | `SSR.SessionsAndPointsFillV8`; revisión con una herramienta de SQLite |
| HIS-19-01, -02 | d | `SSR.GetByIdInLocalTime`, `SSR.PointsOrderedByMonitorTime` |
| SCN-01-01 | p cap | `IHT.ProvisionalBackground` (cielo `backdrop.sky` hasta el horizonte, 0,40 / 0,42 del alto, y agua `backdrop.water`; DN4a-18); CAP-12 |
| APP-03-09 | p cap | `HCT.ImmersiveStacksAndFades`; VID-03 |
| APP-03-10 | p cap | `WSC.StartDialogBehaves`, `HCT.FinishDialogBehaves`; CAP-08, CAP-26 |
| APP-03-11 | p cap | `WPS.ToastsInTheImmersive`; CAP-09, CAP-28, CAP-29 |
| APP-10-10 | d | `CatalogParityTests` (202), `L10nKeysTests` |
| BLE-16-01 (§5) | ps cap | `WSM.RibbonOverTheImmersive`; CAP-12, CAP-26 |
| BLE-16-02 (§5) | d s ps cap | `HGT.WrenchSlot` (DN4a-14); `WPL.*`; `SimulatorOverlayTests.WrenchInTheWorkout*`, `.WrenchLeftLaneOutsideTheWorkout`, `.PanelFollowsTheWrench`, `.MenusAvoidTheWrench`; CAP-01, -12, -15, -16, -17, DEMO-03 y las tomas de las It. 2 y 3 de la spec §5; `WrenchAudit` en toda toma con simulador |
| BLE-16-03 (§5) | s ps cap | `SPL.*` (alto máximo, esquina y lado de crecimiento, tope de Watts); `SimulatorOverlayTests.PanelGrowsFromTheWrench`, `.PanelKeepsSplitAndWattsInView`; DEMO-03 |
| APP-02-08 (§5) | p cap | `WSC.WorkoutIsNotAPlaceholder`; CAP-01 |
| APP-06-01 (§5) | p cap | `IHT.HudColors`; CAP-12, -13, -19 (sondas, ± 2 por canal) |
| APP-05-05 (§5) | p cap | `WSC.TextScale130`; CAP-05, CAP-11 |
| APP-05-07 (§5) | p ps eq | `WSC.TabOrder`, `HCT.TabOrder`, `HCT.FocusReturnsOnlyWithTheKeyboard` (DT4a-01); `SimulatorOverlayTests.WrenchInTheImmersiveTabRing`; [Device-Win] |
| APP-05-08 (§5) | p | `IHT.ReducedMotion` |
| RUT-06-08 (§5) | d p | `WSR.EmptyExpansionCannotStart`; `WSC.CannotStartRoutine` |
| RUT-10-11 (§5) | p cap | `WSC.MiniBarInWorkout`; CAP-01, CAP-02, DEMO-01 |
| RUT-10-04 (§5) | ps cap | `SimulatorOverlayTests.PlusIsFreeWithTheWrenchOnTheLeft` (con el panel cerrado y abierto, desde 368 dp); CAP-01 de la It. 3, regenerada en B0 |
| RUT-10-09 (§5) | ps cap | `SimulatorOverlayTests.LastCardAbovePlusAndWrench`; CAP-06 de la It. 3, regenerada en B0 |
| HIS-17-02 (§5) | d qa | `DSC.*` (It. 3, sin cambios), `SSR.SchemaStillV8`; revisión |
| HIS-20-04 (§5) | d qa | `TDT.*` (It. 3); `SLT.SourceDatabaseIsUntouched`, `SLT.MissingFileErrorHasNoPath`, `SLT.LockedFileErrorHasNoPath`; QA compara las bases del usuario antes y después (QA-G4aT-09) |
| APP-01-09 (§5) | p | `WSC.RoutinesLoadedAtStartup` |

### 8.5 Conteos esperados (aproximados)

dotnet: +≈ 275 tests (motor y reloj ≈ 95, guardado y repositorio ≈ 50, formatos ≈ 45, *stores* ≈ 30, programador y topes ≈ 20, geometría ≈ 15 más las 60 combinaciones parametrizadas, llave y panel ≈ 12, herramientas e higiene ≈ 8; 5 de ellos `[Ignore]` de equipo). PlayMode: +≈ 80 sin simulador y +≈ 28 con simulador. Unity EditMode: +3 archivos. Arnés: 47 IDs (30 CAP, 14 DEMO, 3 VID) = 49 archivos (CAP-27 tiene tres tomas). Matriz: `It04aIds` con los **162 IDs** y los 11 LT; `UnityOnlyIds` = los **54** sin **d** ni **s** (salen de la columna de verificación, como en las It. 2 y 3: AC-WKT-01-09, -05-06, -10-05, -07 a -09, -12-01, -02, -04, -06, -09, -20-01 a -07, -13-01 a -05, -14-01 a -06, -16-01, -17-02 a -07, -10; AC-BLE-10-05; AC-APP-14-01, -03; AC-SCN-01-01; AC-APP-03-09 a -11; y de §5, AC-BLE-16-01, AC-APP-02-08, -06-01, -05-05, -05-07, -05-08, -01-09, AC-RUT-10-04, -09, -11); `WorkPackageBIds` vacío. `TraceabilityTests` suma `It04aIdsAreComplete`, `It04aDotnetIdsHaveTests` e `It04aUnityOnlyIdsHaveAUnityLocation`.

### 8.6 Lo que queda sin test automatizado

[Device-Win]: el proceso terminado desde el Administrador de tareas (AC-WKT-06-05, -10), la ventana minimizada 2 min con el aviso del fin al restaurarla (AC-WKT-02-09), `powercfg /requests` (AC-APP-14-01 a -03; en lo versionado se anota solo `RowMate.exe` y la sección, `DISPLAY` y `SYSTEM`, porque la salida trae la ruta completa del ejecutable: QA-G4aT-16), el teclado real (AC-APP-05-07). [Device-And], NO VERIFICABLE sin módulo y teléfono, cada uno con su `[Ignore]` en WDC: "Atrás" (AC-WKT-13-03), el segundo plano real y el primer cuadro al volver (AC-WKT-02-08, -10; la vuelta del segundo plano es de Android: en Windows minimizada el *loop* sigue, QA-G4aT-09), la pantalla encendida (AC-APP-14-02) y RA-19. IL2CPP: el humo de la base de la It. 3, cuando esté el módulo.

---

## 9. Paquetes de trabajo

### 9.0 Orden y reglas

1. **WP-D0** (Designer, chico, antes de WP-B0): fuentes derivadas, receta de kerning con K2 sobre la derivada 700 (DN4a-22), los dos *tokens* del fondo (DN4a-18) y los pares de contraste (§9.1).
2. **WP-A1** (sin Unity, chico): `OverlaySurface` con `WrenchSlot` y `PanelCeiling` (DN4a-14), `WrenchPlacement`, `SimulatorPanelLayout` (el tope de Watts encendido), el lugar de la llave en `HudGeometry` (la parte de §3.9 de 04-design-notes), `FrameScheduler`, `RealtimeTimerQueue.CatchUp` y `ForegroundTimeout` (QA-G4aT-03), con sus tests (WPL con los rectángulos de 00-deuda-diseno §1.4 y 04-design-notes §3.8). Lo necesita WP-B0.
3. **WP-B0** (Unity) en paralelo con **WP-A2** (el resto de WP-A): las tablas de kerning y la derivada en el *bootstrapper*, D-ART-10 en el shell (pestañas, editor, menús, panel, auditoría) y la regeneración única de `it2` e `it3` (§7.4). Termina con las cuatro suites de Unity en verde y las dos corridas iguales.
4. **WP-B1** (después de WP-A2): composición (`LocalStore`, ciclo de vida con foco, pantalla encendida, `UnityScheduler`, `AppRoot`, `UiTestHost`), Workout en reposo (sin barra superior fuera de reposo), diálogo de inicio, inmersivo libre (fondo de dos tonos, barra libre, tarjetas, banda, controles, diálogo de terminar opaco y su cierre cuando la fase sale de marcha o pausa). Antes de B2 corre la sonda **S-B4a** en el proyecto real y deja la evidencia: la escala por zona con `transform-origin` (texto nítido, *hit-test* de los botones, `FitBox` y `TabularNumber` adentro, `worldBound`), el `GlowTexture` 9-slice a escala 1,6, `▲` y `▼` en Inter 700 con sus métricas reales, **la flecha y `obj.` medidos contra las líneas de base de 04-design-notes §3.2 (± 1 dp)** con el cálculo en px de §6.5 (QA-G4aT-14), un *slot* que crece desde 0 sin aplastar el texto, la textura del fondo con su borde recto, y la altura medida de cada tarjeta contra la de `HudGeometry` (± 1 dp).
5. **WP-B2**: HUD con rutina (fila de información con DV-38, semáforo con el hueco de la flecha, el reloj común de los pulsos), la fila de abajo en horizontal (DN4a-01), segundo plano en pantalla (`Instant`, el aviso retenido), presupuesto del HUD.
6. **WP-B3**: arnés `it4a` (§7), builds (simulador a `Builds/Windows/simulator`, normal con `BuildOutputInspector`, IL2CPP NO VERIFICABLE sin módulo), herramientas y documentos de cierre.
7. **WP-D** (Designer): aspecto sobre las clases de §9.5, iterando con el arnés.

Cada archivo tiene un solo dueño; si alguien necesita cambiar el de otro, lo pide. Una sola instancia de Unity por vez. Ningún paquete hace *commits* (los hace el coordinador). Los builds de desarrollo se abren solo desde su carpeta de salida por defecto (QA-V3-08).

**Excepciones de dueño** (QA-G4aT-15, como QA-G3-15):

| Archivo | Dueño | Excepción |
|---|---|---|
| `Core/Design/Tokens.cs`, `Presentation/Styles/Tokens.uss` (G) | WP-D0 | `backdrop.sky` y `backdrop.water` (133 → 135, DN4a-18). `TokensUssSyncTests` y `TokenCatalogTests` (WP-A) leen el catálogo y no tienen el número escrito: no cambian. Si algún conteo de `Tests/EditMode/**` lo tuviera, WP-D0 puede tocar **solo esa línea** y lo anota en su informe |
| `Tests/EditMode/Design/TypographyUssSyncTests.cs`, `Tests/EditMode/Project/ItTwoDebtTests.cs` | WP-A | WP-B0 puede cambiar **solo** el mensaje de los conteos de `FontAssetPaths.All` (siguen en 10, §6.14) y de `TextFaces` (sigue en 7) |
| `.gitignore` | WP-A | las cinco líneas del oráculo del motor (§3.7), antes de la primera corrida de `run-oracle.ps1 -Engine`. Nadie más lo toca en la 4a |
| `Tests/EditMode/Design/MotionSpecsTests.cs` | WP-A | con las constantes de 04-design-notes §5.13 en WP-A2; WP-D no cambia ningún valor sin pedirlo |

**Lo que había que cerrar antes de cada paquete** (03-qa-harmony-tecnica §0) está cerrado en este documento: antes de WP-D0, DN4a-22 y DN4a-18 (§6.14, §9.1) y el dueño de los conteos (arriba); antes de WP-A1, DN4a-14 (§6.10, `OverlaySurface.WrenchSlot`, los rectángulos de `WrenchPlacementTests`), QA-G4aT-03 (`ForegroundTimeout`, §4.1) y QA-G4aT-07 (sin fotos en segundo plano, §2.10); antes de WP-A2, DN4a-13 (§6.4) y las constantes de §5.13 (§6.11), QA-G4aT-05, -06, -09, -10, -11, -13 y -18, y lo del PO (QA-G4aT-08 y -16, ya en la spec); antes de WP-B0, D-ART-10 en la spec (QA-G4aT-02, ya integrada); antes de WP-B1, QA-G4aT-04, -14 y -17; antes de WP-B2, la DV de las pastillas (DV-38) y la vista después de un alcance (QA-G4aT-07); antes de WP-B3, QA-G4aT-12 (CAP-29 y VID-02, §7.3).

### 9.1 WP-D0 — fuentes, receta y contraste (Designer)

| Archivo | Qué |
|---|---|
| `UI/Fonts/RowMateNumericSC/RowMateNumericSC-{SemiBold,Bold,ExtraBold}.ttf`, `OFL.txt` | DD4a-07, con la receta verificada (`tnum` congelado, nombres 1, 3, 4, 6, 16 y 21 cambiados) |
| `tools/fonts/kern_pairs.py`, `freeze_tnum.py`, `README.md` | la receta de DD4a-06 y DD4a-07 con fontTools 4.68.0 (`uvx --offline`), reproducible; `freeze_tnum.py` corre primero y `kern_pairs.py` después (DN4a-22) |
| `UI/Fonts/Kerning/<cara>.pairs.json` (G) | las listas de K2 para Inter 400, 500, 600, 700 y **RowMate Numeric SC 700** (no Barlow 700: DN4a-22) |
| `Tests/EditMode/Design/ContrastTests.cs` | `ItFourADesignerPairs` con los grupos H1 a H17 y W1 a W4 de 04-design-notes §9.1, con sus valores a tres decimales (`ItFourAControlValuesMatchTheDesignNotes`): entre otros, la pastilla y el tipo de `Próximo` en blanco sobre su `@20 %` (DV-38), `Próximo:` y `obj.` en `text.secondary`, las unidades en `.hud @80 %`, la capa oscura de `Terminar`, el anillo de foco del HUD, el aviso naranja, `workoutNoRoutines` y `workoutInProgress`. Las excepciones de §9.2 de esas notas no se prueban como umbral (y sale la del latido, porque el PO aceptó DV-38) |
| `docs/migration/design/ASSET_SLOTS.md` §3.7 | la derivada integrada; sale la rama "si no trae `tnum`"; los `.ttf` de Barlow quedan como entrada de la receta |
| `Core/Design/Tokens.cs`, `Presentation/Styles/Tokens.uss` (G) | `backdrop.sky` `#0B4FA8` y `backdrop.water` `#0E4F8F`, rol superficie (DN4a-03 y DN4a-18): **133 → 135**; `tools/tokens --check` en 0 (excepción de dueño en §9.0) |

### 9.2 WP-A — motor, guardado y tests de dotnet (≈ 95 archivos)

Core (`Core/Workout/*` entero, `Core/Ports/{ISessionRepository,SessionNotFoundException,IAppLifecycle,IWakeLock}`, `Core/Common/{FrameScheduler,ForegroundLifecycle,ForegroundTimeout,ScreenKeepAwake}`, `RealtimeTimerQueue` con `CatchUp`, `Core/Models/{SessionListing,RoutineExpansion}` (la expansión que satura), `MetricPalette.HudFor`, `Core/Routines/{TrendText,StepSubtitle}`, `Core/Design/{HudGeometry,OverlaySurface}` con DN4a-13 y DN4a-14, `MotionSpecs` con las constantes de 04-design-notes §5.13, `AtlasCharacters`, `L10nKeys`); catálogos (12 claves, 202); App (`App/Workout/*`, composición); Infrastructure (`Sessions/*` con `AddPointsAfterCreateAsync` y el cierre con los puntos a reintentar, `Database/LocalStore`, `RowMateDatabase.WaitForPendingWork`, `IDbExecutor.WaitIdle`, `SerialExecutor`, `SqliteRoutineRepository` con `ownsDatabase`); Simulator (`Panel/WrenchPlacement`, `SimulatorPanelLayout`); `Tests/Support/*` nuevos y cambiados; `Tests/EditMode/**` de §8.2 salvo `ItFourAToolingTests`, `ItFourAFontTests` y `ContrastTests` (con las excepciones de §9.0); `Tests/Simulator/{WrenchPlacementTests,SimulatorPanelLayoutTests}`; `Tests/Golden/oracle/workout_engine.json`; `.gitignore` (§3.7, antes de la primera corrida del oráculo); `tools/oracle/engine/*`, `run-oracle.ps1 -Engine`, su README; `tools/sessions/Program.cs` (errores con textos fijos), `tools/dotnet/RowMate.SessionList/*`, `RowMate.sln`, `run-tests.ps1` (comentario de conteos). Termina con `run-tests.ps1` en verde, la matriz con los IDs de Unity en `PENDIENTE UNITY` y la compilación de `SqliteSessionRepository.cs` y `LocalStore.cs` también contra el `SQLite.cs` de gilzoide en `<scratch>` (como S3-a: la API de bajo nivel tiene los mismos nombres, los *handles* no).

### 9.3 WP-B — UI, composición, arnés y tests de Unity (≈ 70 archivos)

| Entrega | Archivos |
|---|---|
| B0 | `Editor/Bootstrap/{FontAssetBuilder,ProjectBootstrapper,UiAssetsBuilder}.cs`, `Core/Design/FontAssetPaths.cs`, `UI/Fonts/Generated/*` (G), `Presentation/Styles/Typography.uss` (G, por `tools/tokens`), `Presentation/Shell/ShellLayout.cs` (superficie), `Presentation/Routines/RoutineEditorPage.cs` y `RoutinesScreenView.cs` (declaran su superficie; el `+` deja de ser obstáculo de la llave), `Presentation/Controls/PopupMenu.cs`, `SimulatorPresentation/SimulatorLayerView.cs`, `Snapshots/WrenchAudit.cs`, `SnapshotRunner.cs`, `Tests/EditMode/Project/ItFourAFontTests.cs`, `Tests/UnityEditMode/FontAndIconAssetsTests.cs`, `Tests/PlayModeSimulator/SimulatorOverlayTests.cs`, las sondas de la It. 3 que nombran la llave (`RoutinesCatalog.cs`), `docs/…/02-dispositivo-simulador/snapshots/*` y `03-rutinas/snapshots/*` (G) |
| B1 | `Platform/Time/UnityScheduler.cs`, `Platform/Lifecycle/AppLifecycle.cs`, `Platform/Power/*`, `Platform/{PlatformServices,PlatformFactory}.cs`, `Bootstrap/AppRoot.cs`, `Presentation/Workout/{WorkoutScreenView,WorkoutRoutineListView,StartWorkoutDialog,ImmersiveWorkoutPage,HudView,HudZones,HudMetricCard,HudBand,FreeTopBar,HudControls,FinishWorkoutDialog,HudBackdrop}.cs`, `Presentation/Controls/{GlowTexture,BackdropTexture,TabularNumber,FitBox}.cs`, `Presentation/Routines/RoutineCardView.cs`, `Presentation/Shell/{AppShellView,PageStack,ExitLock}.cs`, `Presentation/Motion/{PageTransition,ImmersiveTransition}.cs`, `Presentation/Styles/Base.uss`, `Tests/UiSupport/*`, `Tests/PlayMode/{WorkoutScreenTests,HudControlsTests,WorkoutPersistenceTests,BootSceneTests}.cs`, `Tests/UnityEditMode/{WakeLockTests,UnitySchedulerTests}.cs` |
| B2 | `Presentation/Workout/StepInfoBar.cs`, `Presentation/Motion/{StepPulse,OffTargetMotion}.cs`, `Tests/PlayMode/{ImmersiveHudTests,HudLayoutTests,WorkoutBackgroundUiTests,HudBudgetTests}.cs`, `Tests/PlayModeSimulator/WorkoutSimulatorTests.cs` |
| B3 | `Snapshots/{WorkoutCatalog,SnapshotCatalog,SnapshotCli,ScenarioScript}.cs`, `Tests/EditMode/Project/ItFourAToolingTests.cs`, `tools/unity/{capture-snapshots.ps1,README.md}`, `docs/…/04a-motor-hud-funcional/snapshots/*` (G). Al cerrar: ARCHITECTURE (§2 `Core/Workout`, §6 y §6.1 con `FrameScheduler` y el alcance, §9.2, §13, "Cambios de la It. 4a"), ADR 017 y ADR 008 ("Cambios de la It. 4a") |

### 9.4 WP-D — pulido visual (Designer)

`Skin.uss`, `Motion.uss`, `SimulatorSkin.uss` (lo de 04-design-notes §4.6 y §5.12: las capas de los controles, con la oscura de `Terminar`, el anillo del HUD, las tarjetas de Workout, el aviso y los estados); el visto bueno de las sondas de aspecto de `WorkoutCatalog` (las suma WP-B en B3 con los valores de 04-design-notes §8.2) y de los videos cuadro por cuadro; `04-design-notes.md` (con QA-G4aT-17 ya resuelto por la spec: la vista "en curso" sin barra); las biblias con la tabla de 04-design-notes §13 (ART_BIBLE §2.1 con 135 *tokens*, §3.2, §3.3, §3.5 con el diálogo de terminar opaco, §3.7, §3.11, §4.4, §5.2, §10.2; ANIMATION_BIBLE §2.4, §2.6-§2.9, §2.11, §2.12) y LEGACY_VISUAL_AUDIT §8 (DV-33), con su sección "Cambios de la It. 4a".

### 9.5 Contrato USS (WP-B en `Base.uss` o por código; WP-D no lo cambia)

Siguen las cinco reglas de las It. 2 y 3: sin colores literales; nadie define tamaño ni fuente en un elemento con `.rm-type-*` (en el HUD, el tamaño va en línea por código: §6.4); Skin y Motion no tocan el contrato; las clases de estado las pone el código; lo que va en línea por código manda. Y dos de UI Toolkit para la 4a (QA-G4aT-14): **USS no tiene `em` ni `align-items: baseline`**, así que toda medida en `em` (el hueco y la flecha, el *tracking* aparte, que ya va en 1/100 em) y toda alineación por línea de base (la flecha, la unidad, `obj.`) las calcula el código en px a partir del tamaño del valor y de las métricas del `FontAsset` (§6.5); y **lo que crece desde 0** (el hueco, el alto de `obj.`, `Próximo`, kcal) va en un *slot* que recorta, con el contenido en absoluto o en `nowrap` y la medida en el cuadro siguiente (§6.5), nunca con el texto expuesto al `flex-shrink` de Yoga.

| Elemento | Contrato | Fuente |
|---|---|---|
| `#screen-workout`, `.rm-workout__content` | una sola `ScrollView` con todo (DV-22); relleno 16; relleno inferior por `RegisterClearance`; `min-height` del contenido = alto del visor, por código | AC-WKT-12-09 |
| `#workout-appbar` | la barra de la It. 3 con `workoutTitle` (`title` 20/600); `display: none` fuera de reposo, por código | AC-WKT-12-01, AC-WKT-01-09 |
| `#workout-banner` | fondo `--rm-state-warning-bg`, radio 12, relleno 12, margen inferior 16; ícono `warning` 18 `--rm-state-warning` alineado con la primera línea (no con el centro), 8, texto 13 `--rm-state-warning` que ajusta; crece y colapsa por código (`GrowMotion`, `CollapseMotion`) | AC-BLE-10-03 (DN4a-19) |
| `.rm-workout__state`, `.rm-workout__empty` | `flex-grow: 1`, centrados en lo que queda debajo de `Entrenamiento libre` (el mismo centro para cargando, error y vacío); vacío con ancho máximo 320 | AC-WKT-12-06 (DN4a-19) |
| `#workout-free` | `.rm-button--primary`, ancho completo, `min-height` 56; deshabilitado con la regla de la It. 2 (0,38) | AC-WKT-12-03, AC-BLE-10-04 |
| `.rm-workout__list--disabled` | opacidad 0,5 por código; tarjetas sin eventos | AC-BLE-10-04 |
| `#workout-routines-label`, `#workout-empty`, `#workout-in-progress` | `text.tertiary` (≥ 54 %); el vacío y el "en curso", centrados | AC-WKT-12-04, -06, AC-WKT-01-09 (DV-36) |
| `.rm-routine-card` con `RoutineCardTrailing.Chevron` | la tarjeta de la It. 3 con `.rm-routine-card__chevron` **[nuevo]**: caja de 48 × 48 en la columna de 28 dp del borde derecho (la del `⋮`), `chevron_right` 18 `text.tertiary`, `align-self: center`, `pickingMode` `Ignore` | AC-WKT-12-04 (DV-21; DN4a-19) |
| `#immersive`, `.rm-immersive` | página a pantalla completa; zonas absolutas; `transform-origin` y `scale` por código | AC-WKT-13-01, -02 |
| `#hud-backdrop` | **un solo elemento** con la textura de `BackdropTexture`: cielo `backdrop.sky` del borde de arriba hasta el horizonte (0,40 del alto en vertical, 0,42 en horizontal) y agua `backdrop.water` abajo, con el borde recto; sin eventos ni animación; la textura se elige con la orientación, en el acto | AC-SCN-01-01 (DN4a-03, DN4a-18; la variante de presupuesto de 04-design-notes §7) |
| `#hud-boat-zone` | `display: none` salvo `BoatZoneOutline`; contorno de 2 dp en `dev.simulator`, sin relleno; `pickingMode` `Ignore` | AC-WKT-17-09 |
| `.rm-hud-glass` (tarjetas, banda, barra, controles de vidrio) | fondo `--rm-glass-fallback`; borde 1 `#FFFFFF @10 %` por código (barra superior `--rm-border-subtle`); radio 14 (banda `r.full`) | AC-WKT-17-01 |
| `.rm-hud-card --large/--medium/--small` | tamaños base de ART_BIBLE §3.2: valores 64 / 44 / 32 y rótulos 13 / 12 / 11 en línea; anchos mínimos 140 / 118 / 96 y rellenos 16 × 10 / 14 × 10 / 12 × 8 | AC-WKT-17-01 (D-ART-02) |
| `.rm-hud-card--target` | borde 1,5 del color base al 70 % y `.rm-hud-card__glow` (anillo 9-slice, color base al 20 %), por código | AC-WKT-10-05 |
| `.rm-hud-card__arrow`, `#hud-target-*` | `.rm-hud-card__arrow` es el **hueco** de 0,48 em del valor (ancho en px por código), presente con objetivo desde que empieza el paso; adentro, la flecha Inter 700 de 0,4 em, en absoluto, pegada a la izquierda, con su línea de base por código, `state.offTarget.hud`, visible solo fuera; `obj.` 12/600 `text.secondary`, solo con objetivo, con `margin-left` = el hueco | AC-WKT-10-05 (D-ART-01, D-ART-09; DN4a-07, DN4a-15) |
| `.rm-hud-card__unit` | 4 + `W` (y `m` en la banda) 13/500 en la variante `.hud` de su métrica **al 80 %**, por código, en la línea de base de las cifras; no cambia con el semáforo | AC-WKT-17-03, -04, AC-WKT-10-05 (DV-31) |
| valores del HUD | `FitBox` > `TabularNumber`; color por código (`.hud`, `text.hudPrimary`, `state.offTarget.hud`); se achican solo las cifras | AC-WKT-17-02 a -06, -10 |
| `.rm-hud-topbar --steps` | margen 12; **alto 40** (fila de `min-height` 22, relleno vertical 8); relleno 12 a los lados; tipo 13/700 tracking 0,04 em `step.*.hud`; 6; pastilla (`.rm-hud-pill`): fondo `step.<tipo> @20 %`, radio 6, relleno 6 × 2, 11/600 **`text.primary`**, `trending_up` en `metric.distance`, un `Label` con "…"; `#hud-up-next` (`.rm-hud-up-next`): *slot* que recorta, relleno 7 × 3, radio 8; `.rm-hud-up-next__bg` **[nuevo]** en absoluto, con el fondo `@20 %` y el borde 1 `@50 %` del paso siguiente (es lo único que late); `Próximo:` 10/400 `text.secondary` y el tipo siguiente 10/700 `text.primary`, siempre al 100 %; 8; `#hud-countdown` `num.s` 20 blanco, `flex-shrink: 0` | AC-WKT-14-01 a -05 (DN4a-04, DN4a-16; DV-38) |
| `.rm-hud-topbar --free` | **alto 40**; relleno 14 a los lados; `rowing` 20 `--rm-accent-primary`; 8; título 16/600 blanco | AC-WKT-16-01 (DN4a-04) |
| `#hud-band` | vertical: relleno 20 × 10 y 16 a cada lado del divisor; **horizontal, compacta**: relleno 14 × 10 y 10 a cada lado; íconos 16 al 70 % (`metric.time`, `metric.distance`); `num.band` 34; divisor 1 × 32 al 12 %; `m` 13/500 `.hud @80 %`; las dos cifras con **una sola escala** (`FitScaleGroup`) | AC-WKT-17-04 (DV-33; DN4a-01) |
| `#hud-controls` | fila centrada, relleno 24 a los lados, 16 entre botones; `--hud-danger` contorno 1,5 `state.dangerText`; `--hud-outline` contorno 1,5 `border.strong`; `--hud-success` relleno `state.success` con `text.onAccent`; mínimos 128 × 56 y 148 × 56; radio 12; el *spinner* de 16 en `state.dangerText` a la izquierda del texto de `Terminar`, sin cambiar su ancho, mientras se guarda (DN4a-20) | AC-WKT-20-01, -06 |
| diálogo de terminar | el diálogo opaco de la app de la It. 3, sin variante (`workout-finish-dialog`): título `title` 20/600, cuerpo `body` 15 `text.secondary`. `.rm-dialog--hud` queda **reservada, sin reglas**, para el diálogo de completado de la 4b: en la 4a ningún elemento la lleva | AC-WKT-20-03 (QA-G4a-P05; DN4a-11) |
| `#sim-open` | posición por código (`WrenchPlacement`, con `OverlaySurface.WrenchSlot` en el inmersivo); lugar y fundido de cambio por código; el panel anclado en la llave, por código | AC-BLE-16-02, -03 (D-ART-10, DV-39) |

**Clase de texto y color por elemento nuevo:**

| Elemento | Clase (fuente) | Tamaño | Color |
|---|---|---|---|
| `#workout-appbar` (título) | `.rm-type-title` | 20/600 | `text.primary` |
| `.rm-workout__label` | `.rm-type-body-s` | 13 | `text.tertiary` |
| `#workout-banner` (texto) | `.rm-type-body-s` | 13 | `state.warning` |
| `#workout-free` | `.rm-type-button` | 14/600 | `text.onAccent` (y su `play_arrow` 18) |
| `#workout-error` (texto) | `.rm-type-body` | 15 | `text.secondary` (como Rutinas) |
| `#workout-empty`, `#workout-in-progress` | `.rm-type-body` | 15 | `text.tertiary` |
| rótulo de tarjeta del HUD | `.rm-type-label` | 13 / 12 / 11, en línea | color base de la métrica (pulso: `metric.hr.hud`) |
| valor del HUD | `.rm-type-num-hero` / `-l` / `-m` (RowMate Numeric SC) | 64 / 44 / 32, en línea | `.hud`, blanco o `state.offTarget.hud` |
| flecha (`▲`, `▼`) | `.rm-type-header-s` (Inter 700) | 0,4 em del valor: 25,6 / 17,6, en línea | `state.offTarget.hud` |
| unidad (`W`, `m`) | `.rm-type-caption` (Inter 500) | 13, en línea | `.hud` de su métrica al 80 % (DV-31; era el color base al 55 %) |
| `obj.` | `.rm-type-button-s` (Inter 600) | 12, en línea | `text.secondary` (blanco al 70 %) |
| tipo de paso | `.rm-type-header-s` (Inter 700) | 13, en línea | `step.*.hud` |
| pastilla de objetivo | `.rm-type-button-s` (Inter 600) | 11, en línea | `text.primary` (DV-38); `trending_up` en `metric.distance` |
| `Próximo:` / tipo siguiente | `.rm-type-body-s` (Inter 400) / `.rm-type-header-s` (Inter 700) | 10 / 10, en línea | `text.secondary` / `text.primary` (DV-38) |
| cuenta, banda | `.rm-type-num-s`, `.rm-type-num-band` | 20 (22 por escala), 34 | blanco; `metric.time.hud`, `metric.distance.hud` |
| título libre | `.rm-type-title-s` | 16, en línea | `text.primary` |
| botones del HUD | `.rm-type-button` (600; Reanudar 700) | 16, en línea | `state.dangerText`, blanco, `text.onAccent` |

**Elementos que crea WP-B para WP-D:** `.rm-workout__banner`, `.rm-workout__state`, `.rm-workout__empty`, `.rm-workout__in-progress`, `.rm-routine-card__chevron`, `.rm-hud-glass`, `.rm-hud-card` (con `__label`, `__value`, `__unit`, `__arrow`, `__target`, `__glow`), `.rm-hud-topbar` (`--steps`, `--free`), `.rm-hud-step-row`, `.rm-hud-pill`, `.rm-hud-up-next` (con `__bg`), `.rm-hud-band` (`__divider`), `.rm-hud-controls`, `.rm-button--hud-danger`, `--hud-outline`, `--hud-success`, `.rm-hud-backdrop`, `.rm-hud-boat-zone`. `.rm-dialog--hud` no se crea en la 4a (reservada para la 4b).

**Lo que es de WP-D:** *hover*, presionado y foco de los controles del HUD y de las tarjetas de Workout (`Motion.uss`, siempre bajo `:hover`; con la capa **oscura** de `Terminar`, `overlay.scrim` al 12 y al 24 %, y el anillo de foco blanco con halo oscuro del HUD: 04-design-notes §4.6); tracking y opacidades finas que no son contrato; el aspecto del aviso naranja y de los estados de la lista; el contorno de depuración; la llave en `SimulatorSkin.uss` si hace falta. El diálogo de terminar es el de la It. 3 y no tiene aspecto propio. Las posiciones, la escala, los tamaños base y los colores semánticos son contrato.

---

## 10. Riesgos, desvíos y pedidos

### 10.1 Desvíos propuestos

| ID | Qué | Por qué | Decide |
|---|---|---|---|
| DT4a-01 | `ExitLock` devuelve el foco a quien abrió solo si se navegaba con teclado; con el mouse, lo quita | UI Toolkit enfoca el botón con el clic (medido en `PointerDownFocusesAButton`) y Flutter de escritorio no: hoy un Enter después de `Cancelar` volvería a abrir el diálogo de terminar (00-deuda-it3 §11.1, camino a) | **Aceptado** por el PO (AC-APP-05-07) y el Designer |
| DT4a-02 | El reloj arranca cuando vuelve el alta **o a los 100 ms del toque**, lo que llegue primero | Es la lectura de AC-WKT-01-02 ("a más tardar 100 ms") que también cumple AC-WKT-06-13 con la base lenta; con la base al día da lo mismo que el legacy (`:145-147`) | **Aceptado** por QA, con `WEL.ClockNeverWaitsMoreThan100Ms` con un alta de 2 s |
| DT4a-03 | La escala del HUD se aplica por zona con `scale` | Los USS quedan a tamaño base; también los bordes de 1 dp pasan a `s` dp, que es lo que pide spec §0.3 | **Aceptado** por el Designer |
| DT4a-04 | VID-01 dura 17 s (de los 14,5 a los 31,5 s de `Prueba 4a`) | Con 12 s no entran a la vez la entrada de `Próximo` (a 15 s del final), el "por terminar" y el cambio de paso | **Aceptado** por el PO (spec §7.5) |
| DT4a-05 | Los puntos salen `ORDER BY elapsed_seconds, id` | El legacy no desempata (`:269`); con el id el orden es estable, y entre dos con el mismo tiempo la spec dice que no importa | — |
| DT4a-06 | El cierre reescribe las columnas del legacy salvo el `id` y `origin` | El legacy escribe también el id con su mismo valor; `origin` no existía | — |
| DT4a-07 | Punto final y cierre en una transacción (con los puntos de los lotes que fallaron, desde el gate) | Un corte entre los dos ya no deja una sesión con el punto final y sin fin; terminada, es idéntica | **Aceptado** por QA |
| DT4a-08 | Lotes cuando hay una escritura en vuelo | Con la base lenta la cola no crece y se conserva el orden; con la base al día cada punto sale solo | — |
| DT4a-09 | Windows pide también `ES_SYSTEM_REQUIRED` | ARCHITECTURE §13; el criterio es que tampoco se suspenda el equipo (AC-APP-14-01). La spec lo anotó "sin acción" (§15) | — |
| DT4a-10 | Alcance solo al volver de una pausa de la app (Android, iOS); un cuadro tarde en primer plano mantiene la conducta de la It. 2 y el motor procesa sus segundos por tiempo; el tope de 10 fps minimizada, a la It. 6a | No cambia lo verificado en la It. 2 en Windows; la 10a decide si el pulso de eventos nativos usa el alcance también con cuadros tarde | **Aceptado** por QA con dos condiciones, cumplidas acá: los topes de espera no corren en tiempo virtual (`ForegroundTimeout`, §4.1) y la 10a mide un cuadro tarde con el remo real (§4.1) |
| DT4a-11 | La llave sigue D-ART-10 en toda la app | La tarea lo pedía y el Designer lo propuso | **Resuelto**: el PO lo integró en la spec (AC-BLE-16-02, -03, AC-RUT-10-04, -09; DV-39 reemplaza a DV-13), con el ajuste del inmersivo (DN4a-14) y el tope de Watts encendido |
| DT4a-12 | Un paso de duración negativa: restante 0 (`0:00`), termina en el primer segundo | El legacy lanza (`clamp(0, d)` con `d < 0`, `:103`) y el inmersivo falla; el editor deja escribir `Min` negativo (DV-15) y Q-92 controla por valor absoluto | **Aceptado** por el PO como DV-37 (AC-WKT-03-07, -04-02) |
| DT4a-13 | Si el cierre no vuelve en 15 s **de primer plano**, vuelta a reposo con `workoutSaveError`; si vuelve bien más tarde, la sesión pasa a ser la última terminada | "Nunca se cuelga" (AC-WKT-06-12) con una base colgada; con SQLite el *busy timeout* corta a los 5 s. El tiempo en segundo plano no cuenta (spec §12 punto 2; QA-G4aT-03) | — |
| DT4a-14 | La herramienta de la demo lee una copia de la base | Así "nunca la modifica" vale también para el `-wal` y el `-shm` y para la fecha | **Aceptado** por QA, con los errores sin ruta y su prueba (§3.6; QA-G4aT-18) |
| DT4a-15 | El motor vuelve a reposo solo, al terminar el guardado | En el legacy lo hace la pantalla (`reset()`); así el inmersivo se cierra por estado. La 4b suma la espera del diálogo de completado | — |
| DT4a-16 | La expansión del motor **satura** la progresión en los límites de `int` en lugar de lanzar | Un split guardado ≤ 0 es "sin split" para R3-08 (sus mismas excepciones, AC-WKT-12-08) y con una progresión negativa podía desbordar el inicio (QA-G4aT-11). Lo que se ve es lo del legacy (enteros de 64 bits): arranca y Split está siempre fuera; solo puede diferir el texto de un objetivo así, que en los dos es una cuenta sin sentido | QA (informativo) |
| DT4a-17 | Con una falla pasajera de un lote que el reintento del cierre recupera, el aviso del final es el de un guardado bueno (`workoutCompleted` si terminó sola; nada a mano); `workoutSaveError` al final solo si la sesión no quedó cerrada con todos sus puntos | AC-WKT-06-12 dice "al terminar se vuelve a reposo con el mismo aviso", escrito antes de que el gate sumara el reintento (P-16). Repetir `workoutSaveError` cuando todo quedó guardado sería falso. Alternativa: repetirlo siempre que hubo una falla (`WorkoutStore` recuerda el `SaveFailed` de la sesión y lo usa en el `Ended`; una regla, sin tocar el motor) | **PO** (precisión de AC-WKT-06-12; por defecto, la propuesta; lo prueba `WST.TransientFailureRecoveredAtTheClose`) |

### 10.2 Riesgos

| ID | Riesgo | Mitigación |
|---|---|---|
| R4a-01 | El alcance en Android no se puede ver sin teléfono | El mismo `FrameScheduler` corre en dotnet con el ciclo de vida de prueba (§4.5); [Device-And] en la 10a |
| R4a-02 | La escala por zona (`scale` + `transform-origin`): nitidez del texto, *hit-test*, `FitBox` adentro, `worldBound` para las auditorías | Sonda S-B4a antes de B2; si algo falla, el código multiplica cada medida por `s` (más trabajo, mismo contrato) |
| R4a-03 | El HUD pasa de 60 elementos o reserva memoria por cuadro. Con las notas de diseño queda en 60 en el peor caso (el guardado con el *spinner*), sin margen para la fila de píldoras de la 4b | `HudBudgetTests` en el peor caso; el fondo en un solo elemento (§6.13). Si se pasa, la etiqueta y el valor de cada tarjeta comparten fila. La 4b tiene que juntar elementos para sumar las píldoras (por ejemplo, la flecha dibujada por el mismo `TabularNumber`) |
| R4a-04 | ~~La disposición horizontal llega tarde (DN4a-01)~~ | **Cerrado**: las 04-design-notes la fijaron (§6.4) |
| R4a-05 | ~~L640 horizontal con el simulador: no queda lugar para la llave (Anexo B)~~ | **Cerrado**: la llave va sobre `Pausa` en L640 (DN4a-14, §6.10); `Clear` queda en `true` en los cinco perfiles |
| R4a-06 | La regeneración de B0 cambia todas las bases de captura de las It. 2 y 3 | Una sola vez, con la regla de QA-V3-06; comparación contra `HEAD` en el informe |
| R4a-07 | `SetThreadExecutionState` es del hilo: en el Editor, un pedido que no se libera dejaría la pantalla encendida | Se libera en `OnDestroy` y en `AppDomain.DomainUnload`, en el mismo hilo que lo pidió; `WindowsWakeLock` guarda ese hilo y `WLK.ReleaseRunsOnTheRequestingThread` lo comprueba (§4.4) |
| R4a-08 | Regenerar los vectores del motor legacy necesita Flutter y la caché de `pub` | Los tests leen el JSON versionado; la guardia del legacy de `run-oracle.ps1` |
| R4a-09 | Esperar hasta 1 s en `OnApplicationPause(true)` | Muy por debajo del límite de ANR de Android (5 s); con la base al día, milisegundos |
| R4a-10 | Android cierra la app en segundo plano (RA-08) | La sesión queda cortada con los puntos hasta la pausa (§3.4); se mide en la 10a |
| R4a-11 | Un `fsync` por lote en el teléfono | En el hilo de la base, a 0,2 Hz; nunca en el principal |
| R4a-12 | **RA-19** (ADR 017; QA-G4aT-10): en Android el `Stopwatch` (`CLOCK_MONOTONIC`) no cuenta el sueño profundo con la pantalla bloqueada, así que el alcance avanzaría menos que la hora de pared y el reloj del HUD, los pasos y los puntos se correrían (AC-WKT-02-10, `2:00 ± 1 s`) | Decisión para la 10a: medir el legacy en el mismo teléfono y elegir la fuente que da su resultado (`SystemClock.elapsedRealtime`, que cuenta el sueño, o `uptimeMillis`), como dice ADR 017. Cambia solo la fuente que `UnityScheduler` le pasa a `FrameScheduler`. En la 4a, `FSC.ResumeWithASmallerMonotonicJumpKeepsOrder` prueba que con un salto monótono menor que el de UTC todo dispara en orden y la hora virtual no retrocede (§4.1) |
| R4a-13 | Un alcance muy largo en el hilo principal (una sesión olvidada en segundo plano): con un aviso por lectura y por segundo, el primer cuadro de vuelta haría ≈ 86 000 estados del HUD con textos en 8 h (ANR en Android) | El motor no publica fotos en segundo plano y publica una al volver (§2.10; QA-G4aT-07). `WBG.EightHourCatchUpIsBounded`: 8 h en menos de 500 ms y 32 MB en la PC (dotnet; con margen para un teléfono 5 a 10 veces más lento frente a los 5 s del ANR), 1 h en menos de 100 ms, y un solo estado del HUD. El valor medido va al informe de WP-A |

### 10.3 Pedidos al Designer (para las 04-design-notes de la 4a)

Todos respondidos en [04-design-notes](04-design-notes.md) §1; lo que se acepta y dónde quedó, en el Anexo C.

| ID | Qué | Respuesta y dónde quedó |
|---|---|---|
| DN4a-01 | **Disposición horizontal**: dónde van kcal y BPM, la banda frente a los controles en L640 y W1280, y la llave en L640 | la fila de abajo y la banda compacta; la llave junto a `Pausa` o sobre ella (§6.4, §6.7, §6.10) |
| DN4a-02 | Centro vertical de la zona del bote para el fondo provisorio (lo mantiene la 6a) | el de la cámara: 0,504 / 0,652 (§6.4) |
| DN4a-03 | El fondo provisorio: `bg.app` parejo o dos tonos del mediodía del legacy (con tokens nuevos en WP-D0) | dos tonos, `backdrop.sky` y `backdrop.water`, cortados en el horizonte (§6.3, §9.1, §9.5) |
| DN4a-04 | La fila de información sin píldoras: rellenos y alto de la barra de pasos en la 4a | las dos barras de 40 (§6.6) |
| DN4a-05 | Entrada y salida del inmersivo en la 4a | fundido 240 / 200, 120 lineal con reducir movimiento; las pestañas y el aviso en el cuadro 0 de la salida (§6.3, §6.11) |
| DN4a-06 | Constantes del HUD | las de 04-design-notes §5 y §5.13; el rodillo, 180 en todos (§6.11) |
| DN4a-07 | `obj.`, flecha y unidades: color, tamaño y posición exactos | `obj.` en `text.secondary` alineado con las cifras; la flecha en un hueco de 0,48 em; las unidades en `.hud @80 %` (§6.5) |
| DN4a-08 | Workout en reposo: aviso, botón, etiqueta, vacío y "en curso" | 04-design-notes §4.1 (§6.1, §9.5) |
| DN4a-09 | Los pares de contraste del HUD para WP-D0 | H1 a H17 y W1 a W4 (§9.1) |
| DN4a-10 | El anillo de foco sobre el vidrio y el orden de Tab del HUD | blanco con halo oscuro pegado; `Terminar` → `Pausa` / `Reanudar` → la llave (§6.12) |
| DN4a-11 | El aspecto del diálogo de terminar | el opaco de la app; `.rm-dialog--hud` reservada para la 4b (§6.8, §9.5) |
| DN4a-12 | ¿`num.stat` también pasa a la derivada? | sí: Barlow 700 sale de K2 y entra la derivada 700 (§6.14) |

### 10.4 Para otros roles

| Para | Qué |
|---|---|
| PO | **DT4a-17** (el aviso del final después de una falla pasajera que el reintento recupera; precisión de AC-WKT-06-12). DT4a-01, -04, -11 y -12 ya están resueltos en la spec (§16). Los pendientes de spec §15 siguen siendo suyos |
| QA | DT4a-16 (informativo); la comparación de AC-HIS-20-04 antes y después de todo, también con la herramienta de la demo; la auditoría de la llave (DD4a-04) en todas las tomas con simulador; la base nueva de las It. 2 y 3 (§7.4); `RepoHygieneTests` y el `.gitignore` del oráculo antes de la primera corrida (§3.7) |
| Designer | WP-D0 con DN4a-18 y DN4a-22 (§9.1); en WP-D, el visto bueno de las sondas y de los videos; las biblias con 04-design-notes §13 |
| Coordinador | La sonda S4-a dejó un proyecto descartable en `<scratch>/it4a/arch-s4/legacy-engine/` (no se versiona). `flutter pub get --offline` resolvió con la caché local; el legacy quedó igual antes y después |

---

## Anexo A. Sonda S4-a: el motor del legacy, fuera del legacy

**Qué se hizo.** Un proyecto de Flutter descartable en `<scratch>/it4a/arch-s4/legacy-engine/` con `rowmate` como dependencia por ruta al legacy, `flutter_blue_plus` y `fake_async`. `flutter pub get --offline` resolvió con la caché local. El test (`flutter test`) arma `WorkoutProvider(_FakeBle, _FakeHrm, _CaptureDb)`: los dobles del remo y del pulsómetro son los de `test/workout_provider_hr_test.dart:11-77`, y `_CaptureDb extends DatabaseService` solo anota `insertSession`, `insertDataPoints` y `updateSession`, sin abrir SQLite. Con `fakeAsync`, el remo R-4 de la spec emite L(k) en el instante k − 0,5 y la sesión arranca en el instante 0. El JSON quedó en el scratch.

**Legacy intacto.** `git status --porcelain` vacío, y el MD5 del listado `--ignored --untracked-files=all` y de la instantánea de metadatos (ruta, tamaño y fecha de todo archivo fuera de `.git/`) iguales antes, después del `pub get` y después del test. No se leyó `strava_secrets.dart` (el test no lo importa).

**Resultados** (puntos como (tiempo del monitor, distancia, kcal, índice, tipo)):

| Escenario | Puntos | Cierre | Coincide con |
|---|---|---|---|
| Libre, `Terminar` a los 12,3 s | (305, 1020, 40), (310, 1040, 41), (312, 1048, 41) | 1048 m, 312 s, 150 W, 24.0, 41 kcal | AC-WKT-06-06 |
| `Corta` | 305/0/work, 310/0/work, 315/1/rest, 320/2/work, 325/2/work, 330/3/rest, 330/3/rest | 1120 m, 330 s; objetivos 0 y 2 cumplidos | AC-WKT-06-03, -07, AC-WKT-03-02, AC-WKT-09-07 |
| `Corta` con `R-4(19)` | los mismos | P99 de SPM 19.0; paso 2 no cumplido | AC-WKT-09-07 |
| `Prueba 4a` | 49 puntos; pasos en 30, 90, 120, 180, 210; fin en 240 | 1960 m, 540 s; pasos 1 y 3 no cumplidos (150 < 160) | AC-WKT-03-01 |
| `Distancia 100`: `R-4`, `R-4R`, `R-4Z`, pausa de 20 s a los 40 m | — | el paso termina en los segundos 25, 285, 25 y 11 | AC-WKT-03-03 (columna "Legacy") |
| Pausa a los 10,7 s, 30 s, `Terminar` a los 43 s | (305), (310), (343) | 12 s contados | AC-WKT-01-05 |
| Pausa a los 7 s, 30 s, `Terminar` a los 42,5 s | (305), (340), (343) | puntos en los segundos en marcha 5 y 10 | AC-WKT-06-04 |
| Libre de 3 s | (303) | un solo punto, el final | AC-WKT-06-09 |

En todos los casos el alta va primero, los puntos en lote al final y el cierre al último (el orden del legacy), y `lastFinishedSessionId` queda en la sesión.

## Anexo B. Medición del HUD (para DN4a-01 y DN4a-02)

> **Superado por las 04-design-notes** (Anexo C). Este anexo midió las zonas **del legacy** × `s` y encontró el problema; la disposición que se construye y sus números son los de 04-design-notes §3.7 a §3.9: la fila de abajo en horizontal, el centro de la zona del bote en 0,504 / 0,652 (no 0,5) y la llave sobre `Pausa` en L640 (no "ningún lugar"). Queda como registro de por qué se cambió.

Alturas base con ART_BIBLE §3.2 y las cajas de línea de Inter (§4.3, ≈ 1,24 em): tarjeta grande 102,1 (119,0 con `obj.`), mediana 80,9 (97,8), chica 63,7, banda 72, controles 56. Anchos con las cifras tabulares de la derivada (0,521 em en 700, 0,535 en 800) y `:` y `.` en 0,32 em. `s` = 1,054 (TV, TH), 0,923 (P360, L640) y 1,6 (W1280).

| Perfil | Columna derecha con objetivos (Split + Watts) | Controles (x · arriba) | Banda normal ("00:42", "1048") · libre a la izquierda de los controles | Banda larga ("1:02:05", "123456") |
|---|---|---|---|---|
| TV | 94,9-309,5 | 51,6-359,4 · 838,1 | 252,6 a 140·s del borde: no comparte fila | — |
| TH | 94,9-309,5 | 303,1-610,9 · 335,1 | 252,6 · 279,9: entra | 320 · 279,9: **choca** |
| L640 | 83,1-271,0 | 185,2-454,8 · 293,5 | 221,2 · 164,9: **choca** (con tope, escala 0,75) | 280 · 164,9: choca |
| W1280 | 144,0-469,8 | 406,4-873,6 · 604,8 | 383,5 · 371,2: **choca** por 12 (con tope, 0,97) | 486 · 371,2: choca |

**kcal y BPM en horizontal, en fila debajo de Watts** (la propuesta por defecto): TH 317,9-385,0 con x desde 688,4 (con los mínimos de 96) o 752,4 (ancho de contenido), sin cruzar los controles; L640 278,4-337,2 con x desde 442,5 con los mínimos (**cruza los controles**) o 498,5 con ancho de contenido (no cruza); W1280 482,6-584,5 desde 937,6, sin cruzar nada. Por eso el defecto saca los anchos mínimos cuando no entran.

**La llave en el inmersivo (D-ART-10).** Vertical: TV 790,1-830,1 sobre `Pausa`, techo a 15,7 dp (como 00-deuda-diseno §1.5). Horizontal con la fila de arriba: W1280 en el rincón (654,4-694,4), 70 dp debajo de la fila; TH: el rincón cruza la fila y subir cruza Watts, pero entra a la izquierda de la fila (x 696,4-736,4), algo que R3 no prevé; **L640: ningún lugar** (subir cruza Watts; a la izquierda de la fila, los controles).

**Zona del bote con el centro en 0,5 del alto:** TV 115,1-295,9 × 319,9-594,1 (10,4 dp debajo de Watts y 29,7 sobre BPM); P360 100,8-259,2 × 280-520 (9,0 y 25,8); TH 255,9-658,1 × 143,9-267,2; L640 179,2-460,8 × 126-234; W1280 358,4-921,6 × 252-468. En los cinco queda libre, con topes de ancho para SPM y para la columna derecha de 232,7 (TH), 158,9 (L640) y 323,2 (W1280) dp.

## Anexo C. Cambios por 04-design-notes de la It. 4a (DN4a-01 a DN4a-22)

Las 04-design-notes llegaron después de la primera versión de este diseño. QA-G4aT-01 pide aceptar o rechazar cada respuesta y cada pedido y dejar el contrato al día, como QA-G3-02 en la It. 3. **Se aceptan los 22.** DN4a-18 se acepta con la variante de presupuesto que el mismo Designer dejó (04-design-notes §7): el fondo va en un solo elemento con una textura de 1 × 100 en lugar de un hijo `.rm-hud-backdrop__sky`; se ve igual y ahorra un elemento (§6.13). Ninguno cambia datos, reglas ni textos del catálogo. Los que tocan un AC ya están en la spec corregida (spec §16: DV-38 para DN4a-16, DV-31 y DV-33 para DN4a-13 y -15, D-ART-10 ajustada para DN4a-14). "Antes de" dice qué paquete no puede empezar sin el punto resuelto; con este documento, **todos quedan resueltos**. También se acepta lo que las notas dicen de los desvíos: DT4a-01, DT4a-03 y DT4a-04 (04-design-notes §1).

| ID | Decisión | Qué cambia en este diseño | Paquete | Antes de |
|---|---|---|---|---|
| DN4a-01 (respuesta) | Aceptada | La fila de abajo en horizontal: kcal y BPM a la altura de los controles, kcal en el rincón; la banda compacta; T1 a T5 (§6.4, §6.7); CAP-15, -16, -17 (§7.3) | A2 (`HudGeometry`), B2 | WP-A2 |
| DN4a-02 (respuesta) | Aceptada | `BoatCenterY` con la fórmula de la cámara: 0,504 / 0,652 (§6.4); `HGT.BoatZone` | A2 | WP-A2 |
| DN4a-03 (respuesta) | Aceptada | El fondo de dos tonos del mediodía del legacy, cortado en el horizonte (§6.3, §9.5) | D0 (*tokens*), B1 | WP-D0 |
| DN4a-04 (respuesta) | Aceptada | Las dos barras de 40 (fila de 22); `TopBarHeight`, `TopBarRowHeight`, `TopBarPadV`, `StepRowPadH`, `FreeRowPadH` en `HudGeometry` (§6.4, §6.6) | A2, B1, B2 | WP-A2 |
| DN4a-05 (respuesta) | Aceptada | Fundido 240 / 200 (120 lineal con reducir movimiento); pestañas y aviso del fin en el cuadro 0 de la salida; la salida arranca desde la opacidad que tiene (§6.3, §6.11) | B1 | WP-B1 |
| DN4a-06 (respuesta) | Aceptada | Las constantes del HUD; el rodillo en 180 también en el reloj y la cuenta (§6.11) | A2 (`MotionSpecs`), B2 | WP-A2 |
| DN4a-07 (respuesta) | Aceptada | `obj.` en `text.secondary`, 2 dp debajo y alineado con las cifras; la flecha en el hueco; las unidades `.hud @80 %` (§6.5, §9.5) | B1, B2 | WP-B1 |
| DN4a-08 (respuesta) | Aceptada | Workout en reposo con las medidas, colores y estados de 04-design-notes §4.1 (§6.1, §9.5) | B1 | WP-B1 |
| DN4a-09 (respuesta) | Aceptada | `ItFourADesignerPairs` con H1 a H17 y W1 a W4, y los valores a tres decimales (§9.1) | D0 | WP-D0 |
| DN4a-10 (respuesta) | Aceptada | El anillo de foco del HUD y el orden de Tab con la llave (§6.12); el aspecto del anillo, en WP-D | B1, D | WP-B1 |
| DN4a-11 (respuesta) | Aceptada (QA-G4a-P05) | El diálogo de terminar es el opaco de la app, con el título en 600; `.rm-dialog--hud` queda reservada, sin reglas (§6.8, §9.5) | B1 | WP-B1 |
| DN4a-12 (respuesta) | Aceptada | `num.stat` pasa a la derivada; Barlow 700 sale de K2 (§6.14) | D0, B0 | WP-D0 |
| DN4a-13 [contrato] | Aceptada | `HudGeometry` con los anchos naturales de cada `FitBox` (medidos en `GeometryChangedEvent` y aplicados en el cuadro siguiente, sin bucle), la fila de abajo, el mínimo por ventana, la banda compacta, `BoatCenterY`, T1 a T5; las pruebas con los rectángulos de 04-design-notes §3.8 (± 0,1) y las escalas de §3.7 (± 0,005). Sale `HudArrangement` (§6.4, §8.2) | A2 | WP-A2 |
| DN4a-14 [contrato] | Aceptada | El lugar de la llave en el inmersivo lo calcula `HudGeometry` y la página lo declara en `OverlaySurface.WrenchSlot`; `WrenchPlacement` lo usa tal cual en `Workout`; el panel crece desde la esquina de abajo a la derecha; el tope de Watts solo en vertical (`PanelCeiling`); `Clear` en `true` en L640. Reemplaza los rectángulos de 00-deuda-diseno §1.5 que pedía `WrenchPlacementTests` por los de 04-design-notes §3.8 (§6.10, §8.2) | A1 (`OverlaySurface`, `WrenchPlacement`, la parte de la llave de `HudGeometry`), B0 (`SimulatorLayerView`) | WP-A1 |
| DN4a-15 [contrato] | Aceptada | `.rm-hud-card__arrow` como hueco de 0,48 em desde que empieza el paso, la flecha con su línea de base, `obj.` con el `margin-left` del hueco, las unidades `.hud @80 %`; todo en px por código (QA-G4aT-14) (§6.5, §9.5) | B1, B2 | WP-B1 |
| DN4a-16 [contrato] | Aceptada (el PO aceptó DV-38) | Las pastillas con el texto en blanco, los tintes al 20 % y el latido solo en `.rm-hud-up-next__bg`; `Próximo:` en Inter 400 con `.rm-type-body-s` y el tamaño en línea (la elección que dejó al Architect la nota de 04-design-notes §2.2) (§6.6, §9.5, CAP-19, -21, -27c) | B2 | WP-B2 |
| DN4a-17 | Aceptada | La motion de 04-design-notes §5: el reloj común con la rampa, la flecha que se desplaza en su sentido, el empujón solo dentro de un paso y cada 4 s, el cambio de paso con el hueco, el alto de `obj.` y el borde, BPM y kcal por FLIP y el alcance sin animación. Las constantes de §5.13 en `MotionSpecs` (§6.11) | A2 (constantes), B2 | WP-A2 |
| DN4a-18 [contrato] | Aceptada, con la variante de presupuesto de 04-design-notes §7 | `backdrop.sky` y `backdrop.water` (133 → 135) en WP-D0; `#hud-backdrop` como un solo elemento con la textura de `BackdropTexture` (cielo hasta 0,40 / 0,42 del alto, agua debajo, borde recto con filtro *point*), recalculada con la orientación; `#hud-boat-zone` con el contorno de 2 dp `dev.simulator` (§6.3, §6.13, §9.1, §9.5) | D0, B1 | WP-D0 |
| DN4a-19 [contrato] | Aceptada | `.rm-routine-card__chevron` (48, ícono 18, `align-self: center`, sin toques); los tres estados centrados debajo del botón; el ícono del aviso con la primera línea; el aviso crece y colapsa con `GrowMotion` y `CollapseMotion` (§6.1, §9.5) | B1 | WP-B1 |
| DN4a-20 | Aceptada (P2) | Los controles al 0,38 en el acto al confirmar y el *spinner* de 16 a los 300 ms del cierre; cuenta en el presupuesto (§6.8, §6.11, §6.13) | B1 | WP-B1 |
| DN4a-21 | Aceptada | CAP-22 a los 87,6 s; CAP-29 con `#toast.yMin ≥ #hud-band.yMax` (y a los 2 s, por QA-G4aT-12); las sondas de 04-design-notes §8.2 con `aspect: true` y las reglas de §8.1 (§7.1, §7.3) | B3 | WP-B3 |
| DN4a-22 | Aceptada | K2 sobre RowMate Numeric SC 700 en lugar de Barlow 700; `freeze_tnum.py` antes que `kern_pairs.py`; el rol numérico a la derivada y los `FontAsset` de Barlow fuera de `All` (sigue en 10) (§6.14, §9.1) | D0, B0 | WP-D0 |

## Cambios por gate QA

Correcciones por los dos gates de armonía de QA de la 4a, del 2026-10-28: [lente de paridad](03-qa-harmony-paridad.md) (APROBADO CON OBSERVACIONES: 0 bloqueantes, 3 mayores, 7 menores) y [lente técnica](03-qa-harmony-tecnica.md) (APROBADO CON OBSERVACIONES: 0 bloqueantes, 5 mayores, 13 menores). Se resolvieron los **16 issues del Architect** (4 mayores y 12 menores). El diseño se alineó además con la spec corregida (01-po-spec §16: los issues del PO, 162 AC, DV-37 a DV-39 y D-ART-10 integrada) y con las 04-design-notes (Anexo C).

**Verificación hecha.** Legacy (solo lectura, `3ae0f3a`): `lib/features/workout/immersive_workout_screen.dart:120-142` (el diálogo de terminar es un `AlertDialog` estándar, opaco, con `TextButton` `Cancelar` y `FilledButton` `Terminar`, y el `if (ok == true)`); `lib/features/workout/series_tracker.dart:36-150` (`begin`, `rebase` y `update`: el tope de 10 m/s con `dt < 1 ? 1 : dt`, el salto negativo o grande que no suma y la lectura que pasa a ser la referencia); `test/series_tracker_test.dart:1-60` y `:100-225` (el grupo `_hardening` y el alimentador `_Raw`, con `t` de a 1 s). Los cuatro casos de §8.3 se recalcularon a mano contra ese código: 1096 (dentro de 1060 a 1110), 0 y 560, 1040 sin bajar nunca, y 24; y el inicio sin lectura previa con el monitor en 5000 m da el segundo 26 (legacy, 1), como la sonda de QA. Repo: `Core/Common/RealtimeTimerQueue.cs` (el `Delay` se completa dentro de la corrida; `RunDue` reancla los periódicos), `IScheduler.cs`, `Tests/Support/FakeScheduler.cs`, `Platform/Time/UnityScheduler.cs` (`MonotonicSeconds` vivo), `Core/Routines/StepLimitCheck.cs` (`PositiveSplit` deja afuera un split ≤ 0), `Core/Models/RoutineExpansion.cs` (`checked` en `Apply`; lo usa solo `Routine.FlattenedSteps`), `Presentation/Shell/DialogLayer.cs` y `ModalDialog.cs` (`Cancel`, `IsOpen`; ninguna relación con `PageStack`), `Infrastructure/Threading/SerialExecutor.cs` (FIFO; `TrySetResult` dentro del trabajo, antes del siguiente), `Tests/Support/TestMainThreadContext.cs`, `.gitignore:30-45`, `tools/oracle/README.md` y `run-oracle.ps1:116` (arma un `file:///` en tiempo de ejecución), `tools/dotnet/run-tests.ps1:120-185` (los estados de la matriz), `Tests/EditMode/Traceability/{It03Ids,TraceabilityTests}.cs` (`Classify` hace fallar un ID solo con **eq** o **qa**), `BootstrapperStaticTests.cs:141`, `ItThreeToolingTests.NoMachinePathsInScriptsOrManifests`, `LegacyDiscardTests.cs`, `TokensUssSyncTests.cs` y `TokenCatalogTests.cs` (sin el conteo escrito), `TypographyUssSyncTests.cs:123` e `ItTwoDebtTests.cs:25` (`FontAssetPaths.All` = 10), `Core/Design/{FontAssetPaths,MotionSpecs}.cs`, `Presentation/Controls/{TabularNumber,FitBox}.cs` (`NaturalWidth`, `BaselineFromTop`, `FitScale`), `Simulator/Panel/SimulatorPanelLayout.cs`; ADR 017 y ARCHITECTURE (RA-19); 00-deuda-diseno §1.3 a §1.8 y 00-deuda-it3 §1.4. Una búsqueda en `tools/` de unidades de disco y de `file:///`: aparecen solo los valores por defecto de instalación de dotnet y de Unity (`<Program Files>\…`) en 8 archivos (`build.ps1`, `run-tests.ps1`, `run-unity-tests.ps1`, `capture-snapshots.ps1`, `capture-cap27.ps1`, `tokens/Program.cs` y los README de `dotnet` y `unity`), que no son datos del usuario, y la línea de `run-oracle.ps1`. No se leyó `lib/core/strava/strava_secrets.dart`; `git -C ../rowmate-legacy status --porcelain` vacío antes y después. Es un cambio de documento: no se tocó código, no se corrió dotnet y no se abrió Unity.

| Issue | Sev. | Qué cambió | Dónde |
|---|---|---|---|
| QA-G4aT-01 | mayor | Anexo C: los 22 DN4a aceptados (DN4a-18 con la variante de presupuesto). Al día: §6.4 (la fila de abajo, `BoatCenterY` 0,504 / 0,652, T1 a T5, los anchos naturales y el lugar de la llave), §6.5 (el hueco, `obj.`, las unidades), §6.6 (las barras de 40, DV-38, el reloj común), §6.7 (la banda compacta con una escala), §6.8 (el diálogo opaco, el *spinner*), §6.10 (DN4a-14), §6.11 (las constantes de §5.13 en `MotionSpecs`, en WP-A2), §6.12 (el anillo y Tab), §6.14 (K2 sobre la derivada 700), §7.3 (CAP-12, -15 a -17, -19, -21, -22 a los 87,6 s, -24, -26, -27c, -29), §8.2 (HGT y WPL con 04-design-notes §3.8) y §9.5. `HudBudgetTests` recontado: 59 en marcha y 60 en el guardado con el *spinner* (§6.13). Lo de cada paquete queda cerrado antes de empezarlo (§9.0) | Anexo C, §0, §1.1, §6.1-§6.14, §7.1, §7.3, §8.2, §9.0, §9.1, §9.5, §10.3 |
| QA-G4aT-03 | mayor | `ForegroundTimeout` (Core/Common): un tope de espera que no cuenta el segundo plano. Se detiene con `InBackground` y se rearma con lo que le quedaba al volver, que en la app pasa **después** del alcance; creado durante el segundo plano, arranca entero a la vuelta. Es la variante "se reanclan en `to`" que pidió QA, y la regla de la spec §12 punto 2. El cierre lo usa en lugar de `scheduler.Delay(15 s)`. Se define el primer cuadro con el cierre en vuelo: el inmersivo con la rutina terminada y, cuando vuelve el cierre, el aviso una vez. Un cierre que vuelve bien después del tope pasa a ser la última terminada. Pruebas: `FTO.*` y `WBG.LongBackgroundAfterTheEndStillSaves` (`LifecycleHarness`, `SerialExecutor` y `TestMainThreadContext`, con la vuelta a los 330 s: `Ended(Completed, true)`, `LastFinishedSessionId` puesto y ningún `workoutSaveError`) | §0, §1.1, §2.1, §2.9, §4.1, §4.2, §8.2, §8.4 (WKT-02-06, -06-11), §10.1 (DT4a-10, -13) |
| QA-G4aT-04 | mayor | `ImmersiveWorkoutPage` cancela su `FinishWorkoutDialog` (`ModalDialog.Cancel`, con su salida) en el primer estado con la fase fuera de `Active` y `Paused`, **antes** del `Pop`, también después de un alcance. Un `Terminar` que llega después no hace nada; uno que llegó antes es un terminar a mano (AC-WKT-20-07). El panel del simulador no se cierra: sigue a la llave a su lugar nuevo (AC-BLE-16-02). Pruebas: `HCT.RoutineEndsWithTheFinishDialogOpen`, `.FinishTapAfterTheEndDoesNothing`, `.DialogClosesWhenTheRoutineEndsInBackground`, `SimulatorOverlayTests.PanelFollowsTheWrench` | §0, §6.3, §6.8, §6.10, §8.2, §8.4 (WKT-20-07, -13-04) |
| QA-G4aT-05 | mayor | `.gitignore` con las cinco líneas de `tools/oracle/engine/` (`.dart_tool/`, `build/`, `.flutter-plugins*`, `pubspec_overrides.yaml`, `pubspec.lock`), de WP-A y antes de la primera corrida; `run-oracle.ps1 -Engine` comprueba esas líneas antes de `pub get` y que no quede ningún archivo sin ignorar al terminar; el README del oráculo deja de decir "no hay `pub get`"; `RepoHygieneTests.ToolsHaveNoMachinePaths` sobre **todo** archivo de texto de `tools/` (precisión abajo) | §1.1, §3.7, §8.2, §9.0, §9.2 |
| QA-G4a-P05 | menor | El diálogo de terminar es el opaco de la app (el de inicio), con el título `title` 20/600 y el cuerpo `body` 15; no lleva `.rm-dialog--hud`, que queda reservada y sin reglas para la 4b. Es la opción de paridad: el legacy usa un `AlertDialog` estándar, opaco | §0, §6.8, §9.5, §7.3 (CAP-26), §8.4 (WKT-20-03) |
| QA-G4a-P06 | menor | SDI porta las afirmaciones de metros de los 4 casos `_hardening` de `test/series_tracker_test.dart:155-220`, con `// legacy:` y su nombre exacto; SAD suma el inicio sin lectura previa (26; legacy 1); en la matriz y en el informe van como "parciales: completan en la It. 5" | §2.4, §8.2, §8.3, §8.4 (WKT-03-03) |
| QA-G4aT-06 | menor | Se toma la otra opción de QA, la que eligió el PO: con el alta en vuelo, `FlushAndWait` (y `Abandon`) mandan el lote con `ISessionRepository.AddPointsAfterCreateAsync`, encolado **detrás del alta** en el hilo de la base, que toma el id que dejó el alta; el tope sigue en 1 s (AC-WKT-06-15). `CreateAsync` devuelve la tarea del ejecutor, que se completa en el hilo de la base antes del trabajo siguiente. Pruebas: `SRC.PointsTakenWhileCreatingAreFlushedOnBackground` (alta lenta con el `SerialExecutor` real y segundo plano a los 6 s) y `SSR.PointsAfterCreateWaitForTheCreate` | §0, §3.1, §3.2, §3.3, §8.2, §8.4 (WKT-06-15) |
| QA-G4aT-07 | menor | En segundo plano (o durante el alcance) el motor no publica fotos: una sola al volver, con `Instant = true`; el *store* del HUD arma un solo estado con `Instant` y la vista lo aplica sin rodillo, sin empujón y con todo en su estado final; los pulsos toman como origen el primer cuadro real. En WBU, `IsCatchingUp = true` durante el `Advance` e `InBackground = false` al final. `WBG.EightHourCatchUpIsBounded` mide 1 h y 8 h con tope (R4a-13) | §0, §2.10, §2.11, §6.5, §6.6, §7.1, §8.2, §10.2 |
| QA-G4aT-09 | menor | Ningún AC queda solo con **eq** o **qa**: `BootstrapperStaticTests` (`runInBackground`) suma la categoría de AC-WKT-02-09; `[Ignore("NO VERIFICABLE: …")]` en `WorkoutDeviceChecksTests` para AC-WKT-02-10 y las partes de [Device-And]; `SKA.RequestsAgainOnResumeAndFocus` para AC-APP-14-02 (la regla pasa a `ScreenKeepAwake`, que escucha el ciclo de vida); `SSR.NothingButSessionsAndPointsIsWritten` y una regla de `PureCodeRulesTests` para AC-WKT-06-14; "d qa" en AC-WKT-23-01 (`LegacyDiscardTests`) y AC-HIS-20-04 (`SLT`). La parte de equipo de AC-WKT-02-08 es [Device-And]. `It04aIds` con 162 IDs y sus 54 `UnityOnlyIds` | §1.1, §1.2, §4.2, §4.3, §4.4, §8.2, §8.4, §8.5, §8.6 |
| QA-G4aT-10 | menor | RA-19 como R4a-12, con la decisión para la 10a (`elapsedRealtime` contra `uptimeMillis`, midiendo el legacy en el mismo teléfono); `FSC.ResumeWithASmallerMonotonicJumpKeepsOrder` (`LifecycleHarness.ResumeWithMonotonicJump`) | §4.1, §4.3, §4.5, §8.2, §10.2 |
| QA-G4aT-11 | menor | La expansión del motor satura la progresión en los límites de `int` (`Flatten(…, ProgressionOverflow.Saturate)`); `Flatten(template)` sigue con `checked`. `WSR.HugeNegativeSplitStartsWithoutOverflow` con el split −2 147 483 000, la progresión −5 999 y ×2 (DT4a-16; precisión abajo) | §0, §1.1, §2.4, §5, §8.2, §8.4 (WKT-12-08), §10.1 |
| QA-G4aT-12 | menor | (a) CAP-29 a los 2 s (`00:02`), con el aviso en el hueco entre la banda y los controles. (b) VID-02 cruza dos veces en menos de 4 s (a los 3 y a los 5,5 s) y el empujón sale solo en el primero, como pidieron la spec (§7.5) y las notas de diseño | §7.3 |
| QA-G4aT-13 | menor | Se toma la primera opción, la que eligió el PO (P-16): los puntos de un lote fallado se guardan y el cierre los reinserta una vez, en **su** transacción, antes del punto final y del `UPDATE`; si esa transacción falla, la sesión queda cortada. Una sesión cerrada tiene siempre todos sus puntos y sus totales salen de ellos (lo que va a calcular el Historial de la It. 9). `saved` = cerrada con todos sus puntos. Pruebas: `SRC.FailedBatchIsRetriedInTheClose`, `SSR.CloseRetriesTheFailedBatchInOneTransaction`, `SSR.PersistentPointFailureLeavesTheSessionCut`. DT4a-17, para el PO: el aviso del final después de una falla pasajera recuperada | §0, §2.8, §2.9, §3.1-§3.4, §8.2, §8.4 (WKT-06-12), §10.1 |
| QA-G4aT-14 | menor | §9.5 y §6.5: USS no tiene `em` ni `align-items: baseline` (el hueco, la flecha, las líneas de base y `obj.` en px por código, con `TabularNumber.BaselineFromTop` y las métricas del `FontAsset`); lo que crece desde 0 (el hueco, `obj.`, `Próximo`, kcal por FLIP) con el patrón de la It. 3 (*slot* que recorta, contenido en absoluto o `nowrap`, medida en el cuadro siguiente). La sonda S-B4a mide la flecha y `obj.` contra las líneas de base de 04-design-notes §3.2 (± 1 dp) | §6.5, §6.6, §9.0 (S-B4a), §9.5 |
| QA-G4aT-15 | menor | Excepciones de dueño en §9.0: `Tokens.cs` y `Tokens.uss` de WP-D0 (los conteos de tests leen el catálogo y no cambian), los mensajes de los conteos de fuentes que cambia B0, el `.gitignore` de WP-A y `MotionSpecsTests` de WP-A | §1.1, §9.0, §9.1 |
| QA-G4aT-18 | menor | Los errores de la herramienta de la demo son tres textos fijos con solo el nombre del archivo (código 2); el mensaje de la excepción no se imprime. `SLT.MissingFileErrorHasNoPath` y `SLT.LockedFileErrorHasNoPath` comprueban que no aparecen ni la carpeta de datos ni la temporal | §3.6, §8.2, §8.4 (HIS-20-04) |

**Alineación con la spec corregida (issues del PO y partes del PO de otros).** QA-G4a-P01 y QA-G4aT-08: el comentario del Split en §2.6 ("< 0") y los cuatro casos de `TLT.SplitAndNonPositiveTargets`. QA-G4a-P02: AC-WKT-20-07 en §6.3, §6.8 y la matriz. QA-G4a-P03 (DV-38): §6.6, §9.5, CAP-19, -21 y -27c. QA-G4a-P04: las unidades `.hud @80 %` (DV-31) y la fila de abajo (DV-33). QA-G4a-P07: los cuatro estados sin remo en §2.11, WST y WSC. QA-G4a-P08: el aviso retenido sin el usuario delante (§2.11; `IAppLifecycle.HasFocus`, §4.2), en WBG, WST y WBU. QA-G4a-P09 y QA-G4aT-17: la vista "en curso" sin la barra superior (§6.1). QA-G4a-P10: el último paso por distancia (§2.4, §2.5, SPG). QA-G4aT-02: D-ART-10 integrada (§6.10; DT4a-11 resuelto; el tope de Watts encendido; AC-BLE-16-03, AC-RUT-10-04 y -09 en la matriz; las tomas de las It. 2 y 3 de §7.4). QA-G4aT-16: el punto con el alta en vuelo (§2.3), `FlushAndWait` (§3.2), la evidencia de `powercfg` (§8.6) y [Device-And] en AC-WKT-02-08. DT4a-01, -04 y -12 aceptados (DV-37). Observaciones de 03-qa-harmony-tecnica sin issue, también incorporadas: la hora del cuadro de `FrameScheduler` (§4.1), `ResumeFromBackground` sin `EnterBackground` es un no-op (§4.1, §4.2), la liberación de la pantalla en el mismo hilo (§4.4), `#hud-boat-zone` en `Ignore` (§6.3), la condición de la 10a para DT4a-10 (§4.1) y `emitCurrent: false` en el conteo de avisos de LT-35 y LT-38 (§2.7). De 03-qa-harmony-paridad, O-02 (`Sprint` en §5).

**No se disputa ningún issue.** Dos precisiones sobre cómo se cumplen:

1. **QA-G4aT-05, el patrón de la prueba estática.** "Falla si un archivo versionado de `tools/` contiene `file:///` o una unidad de disco", tomado al pie de la letra, haría fallar el repo de hoy: 8 archivos tienen los valores por defecto de dotnet y de Unity en la carpeta de programas de Windows (`<Program Files>\dotnet\…`, `<Program Files>\Unity\Hub\…`: lugares de instalación, no datos del usuario, que QA-G3-20 ya aceptó) y `run-oracle.ps1:116` arma un `file:///` en tiempo de ejecución. La prueba busca rutas **de la PC**: la carpeta del usuario o la temporal (la expresión de QA-G3-20), `file:///` seguido de una unidad o de `/Users` o `/home`, y la caché de `pub`. Esas son exactamente las que dejan `pub get` y `-LegacyPath`. Para saber qué está versionado no llama a git: excluye la misma lista que el `.gitignore` y comprueba que estén ahí.
2. **QA-G4aT-11, la opción elegida.** De las dos que propuso QA se toma saturar en el camino del motor y no agregar una condición a `CanStart`: AC-WKT-12-08 fija la regla ("la validación 3 de AC-RUT-11-05, con sus mismas excepciones"), el split ≤ 0 es una de esas excepciones y el editor deja guardar esa rutina. Con la saturación arranca como en el legacy.

**Pendientes que no son del Architect:** DT4a-17, para el PO (precisión de AC-WKT-06-12). Del Designer: alinear 04-design-notes §4.1 con la vista "en curso" sin barra (QA-G4a-P09, ya decidido en la spec), sacar la excepción del latido de §9.2 (DV-38 aceptada) y precisar 00-deuda-diseno §1.7 (spec §15). Los pendientes del PO de spec §15 (ROADMAP, inventarios, STATUS).

## Cambios por verificación (ronda 1)

Correcciones por la verificación de QA de la 4a, del 2026-10-28: la [principal](05-qa-verification.md) (APROBADO CON OBSERVACIONES: 0 bloqueantes, 2 mayores, 10 menores) y sus dos lentes, [paridad](05-qa-lens-paridad.md) y [robustez](05-qa-lens-robustez.md). Esta ronda resuelve:

- **Los dos mayores**, que un escéptico independiente confirmó y QA reprodujo en Unity: QA-V4a-01 (= QA-V4a-ROB-01) y QA-V4a-02 (= QA-V4a-ROB-02). Las PlayMode que propuso la lente quedan versionadas, con más casos.
- **La regla del repo público**: la búsqueda mecánica de rutas de la PC da 0 (QA-V4a-07).
- **Tres menores de la lente de robustez**, baratos: QA-V4a-ROB-03, -04 y -05.
- **La parte de QA-V4a-L02 que no necesita Flutter.** Los guiones nuevos del oráculo quedan pendientes (abajo).
- **Un hallazgo propio**, al medir QA-V4a-02: los indicadores de carga ocultos pedían 60 fps para siempre.

Cada arreglo tiene antes una prueba que lo reproduce: se la vio fallar y después pasar.

| Issue | Sev. | Qué cambió | Dónde | Prueba (roja antes, verde después) |
|---|---|---|---|---|
| QA-V4a-01 = QA-V4a-ROB-01 | mayor | `WorkoutScreenView.Immersive` no cuenta la página que está saliendo (`!Pages.IsExiting`). Una sesión que empieza durante el fundido de salida del inmersivo anterior (200 ms; 120 con reducir movimiento), con la vista de reposo ya respondiendo debajo (`ExitLock`), apila **su** inmersivo en el mismo toque. `PageStack.Push` ya aceptaba una página nueva con otra saliendo; la vieja termina su fundido debajo y se dispone sola (`OnPopped`). Es lo del legacy, que apila una ruta por inicio (`workout_screen.dart:88-89`, `:164-169`). Se descartó la otra opción de la lente (volver a mirar la fase en el `OnPopped` del inmersivo viejo): el HUD llegaba 200 ms tarde y con un fundido de entrada encima, contra AC-WKT-13-01 ("en el acto") | `Presentation/Workout/WorkoutScreenView.cs`; §6.1 | `WorkoutScreenTests.StartDuringThePreviousExitHasItsOwnImmersive`: la PlayMode de la lente (§4.1), con y sin reducir movimiento. Suma que queda un solo inmersivo, el del libre, que tapa las pestañas, con `workoutInProgress` debajo (AC-WKT-01-09), y que Pausa, Terminar y "Atrás" responden. Roja antes: `la página que sale ya no es el inmersivo de la pestaña` |
| QA-V4a-02 = QA-V4a-ROB-02 | mayor | `HudView.Dispose` para las dos animaciones continuas del HUD: el latido de BPM (`HeartbeatAnimator.Stop`, que existía y nadie llamaba) y el pulso de la fila (`StepPulse.Stop`, nuevo: sale del `MotionDriver` en el acto y deja la cuenta en escala 1 y en su color de reposo). Además, `StepInfoBar.Apply(null)` (la foto de reposo, que no trae paso) apaga el pulso con `StepPulse.Release`, como un estado sin "a continuación" ni "por terminar": la fila deja de latir con la foto de reposo. El corazón sigue durante el fundido de salida, porque esa foto conserva el pulso (AC-WKT-01-07), y se para cuando la página sale | `Presentation/Workout/HudView.cs`, `StepInfoBar.cs`; `Presentation/Motion/StepPulse.cs`; §6.3, §6.11 | `ImmersiveHudTests.LeavingTheImmersiveStopsItsContinuousMotions`: la PlayMode de la lente (§4.2), con tres salidas (Terminar con Próximo latiendo, Terminar con la cuenta por terminar y la vuelta del segundo plano con la rutina ya terminada, que sale sin transición). Afirma además que el `MotionDriver` y las demandas de 60 fps vuelven a los de reposo (0). Roja antes: `el corazón del HUD sigue en el MotionDriver` |
| Hallazgo de la corrección (sin ID de QA) | — | Los indicadores de carga de Workout y de Rutinas (`Spinner`, continuo) giraban en el `MotionDriver` desde que se armaba la pantalla: `Spinner` nace "a la vista", y lo oculto es su contenedor (`display: none`). Con eso la app tenía 2 demandas de 60 fps permanentes y no bajaba nunca a 30 en reposo (ARCHITECTURE §13.1), con o sin QA-V4a-02: lo mostró la PlayMode de QA-V4a-02, que medía lo mismo antes y después de la sesión. Las dos pantallas lo muestran ahora con `SetShown` solo mientras se ve el estado de carga. A la vista no cambia nada | `Presentation/Workout/WorkoutScreenView.cs`, `Presentation/Routines/RoutinesScreenView.cs`; §6.1 | `WorkoutScreenTests.LoadingSpinnersSpinOnlyWhileShown` (en reposo, 0 animaciones y 0 demandas; con una carga de 5 s, gira mientras se ve y deja de girar al llegar la lista). Roja antes: `con la lista cargada, el indicador de Workout no gira` |
| QA-V4a-07 | menor | La búsqueda obligatoria del repo público daba 1 coincidencia. Era un caso sintético de `RepoHygieneTests.MachinePathsAreRecognized`, de una persona inventada. Los casos con forma de ruta de la PC van ahora partidos en dos o tres literales concatenados: la misma constante, y la búsqueda no los confunde. Son cuatro: la carpeta de usuario de Windows en sus dos formas, el prefijo de las carpetas de trabajo y `file:///` con la carpeta de usuario de macOS | `Tests/EditMode/Project/RepoHygieneTests.cs` | `git grep --untracked` con las rutas de usuario de Windows y de Git Bash y el prefijo de las carpetas de trabajo, sobre lo versionado y lo nuevo: **0 coincidencias** (también sin distinguir mayúsculas). `MachinePathsAreRecognized` sigue pasando |
| QA-V4a-ROB-03 | menor | `FlushAndWait` pone los puntos de los lotes fallados delante de los pendientes antes de mandar, como ya hacía `Abandon` (un método común, `RetryWithThePending`). Si ese lote vuelve a fallar, `Settle` los deja otra vez para el cierre, que los reintenta en su transacción (QA-G4aT-13 no cambia) | `Core/Workout/SessionRecorder.cs`; §3.2, §3.4 | `WorkoutBackgroundTests.BackgroundFlushAlsoSendsTheFailedBatchAgain`, la L03 de la lente: al congelarse están los dos puntos, y el cierre posterior no repite el del 5. Roja antes: 1 punto, se esperaban 2 |
| QA-V4a-ROB-04 | menor | Se toma la opción "como mínimo" de la lente, sin cambiar el puerto. `FinishAsync` guarda el punto final y la fila de cierre. **Con Terminar confirmado y el cierre todavía sin encolar**, el cierre espera en el hilo principal la vuelta de un lote o del alta: es una continuación que en segundo plano, o al cerrar la app, puede no correr nunca. Para ese caso:<br>• `FlushAndWait` espera la mitad del tope. Si la base terminó lo que tenía, las tareas del alta y de los lotes ya están completas (`SerialExecutor.WaitIdle` corre detrás de ellas), así que se resuelven ahí, sin bloquear (`IsCompleted` antes de `Result`). El cierre se encola (`TryQueueCloseNow`) y se espera la otra mitad: el total sigue en 1 s como mucho (AC-WKT-06-15).<br>• `Abandon` hace lo mismo con un tope de 1 s, antes de que `LocalStore.Dispose` termine lo encolado.<br>• La continuación de `FinishAsync`, cuando corre, espera ese mismo cierre y no encola otro.<br>• Si la base no terminó dentro del tope, la sesión queda cortada, como antes (RA-08).<br>La variante completa de la lente (encolar el cierre en el acto y resolver en el hilo de la base qué lotes fallaron) pide un método nuevo del puerto: queda para cuando la 10a mida el teléfono | `Core/Workout/SessionRecorder.cs`; §3.2, §3.4 | `WorkoutBackgroundTests.FinishThenBackgroundWithABatchInFlightStillCloses` (L04, con SQLite real y el `SerialExecutor`; lee desde otra conexión antes de soltar el hilo principal; total < 1,5 s) y `.QuitRightAfterFinishKeepsTheClose` (L08 y L08b). Rojas antes: `finished_at` vacío |
| QA-V4a-ROB-05 | menor | `WorkoutEngine.OnClock` con dos o más segundos vencidos (un cuadro de primer plano muy tarde) los procesa igual, en orden y cada uno con su instante. `Notify()` los junta (`_coalescing`, que trata como `IsCatchingUp`) y al final publica **una** foto con `Instant`, como la vuelta del segundo plano. Los puntos, los pasos y los avisos no cambian; un cuadro con un solo segundo vencido sigue como siempre | `Core/Workout/WorkoutEngine.cs`; §2.10 | `WorkoutHudStoreTests.OneInstantStateAfterALateFrameInTheForeground` (L05: 1 h en un cuadro da 3602 s, 720 puntos, a lo sumo 2 fotos con una sola `Instant`, y el segundo siguiente sin `Instant`). Roja antes: 3601 fotos. `ActiveClockTests.SixHundredSecondsWithALateFrame` sigue en 600 s y 120 puntos |
| QA-V4a-L02 | menor | **Parcial.** La parte que no necesita Flutter: los 3 escenarios de Q-55 que `SessionsAreIdenticalToTheLegacy` saltea entero se comparan ahora hasta el primer cambio de paso | `Tests/EditMode/Workout/WorkoutGoldenTests.cs` | `WorkoutGoldenTests.SanitizerScenariosMatchUntilTheFirstStepChange`: los puntos del paso 0 que tienen los dos motores, uno por uno y con todos sus campos (12: los segundos 5 a 25 con R-4R y con R-4Z; 5 y 10 con la pausa) |

**Las firmas que cambian.** Ninguna pública:

- `StepPulse.Stop()` y `StepPulse.Release(bool instant)` son nuevos.
- `SessionRecorder.FlushAndWait` y `Abandon` conservan su firma y amplían lo que hacen (§3.2).
- `ISessionRepository` no cambia.

**Pregunta de QA-V4a-ROB-05 para el PO (sigue abierta).** Si Windows suspende el equipo con una sesión en marcha y el `Stopwatch` sigue contando, al despertar la sesión suma el tiempo suspendido, con un punto cada 5 s de la última lectura. ¿La suspensión cuenta como tiempo de sesión? AC-WKT-02-09 solo cubre la ventana minimizada o sin foco. Esta ronda solo evita las 3600 fotos en un cuadro; la regla es del PO, y medir el `Stopwatch` con el equipo suspendido queda para quien pueda suspender el equipo.

**Pendientes de esta ronda:**

- **QA-V4a-L02, los guiones nuevos del oráculo.** Son los que la lente de paridad corrió a mano, idénticos al legacy:
  - pasos de 0 s y negativos, con DV-37 como diferencia esperada;
  - paso sin meta con Terminar, terminar en pausa, terminar en el segundo del cambio;
  - último paso por distancia o de 0 s, tiempo y distancia a la vez;
  - el pulso del sensor en los puntos;
  - pausa antes del primer segundo, pausa que cruza un cambio de paso, varias pausas en un segundo;
  - dos sesiones seguidas.

  Hacen falta tres cosas: correr el legacy con Flutter (`run-oracle.ps1 -Engine`), ampliar el esquema de `workout_engine.json` con los eventos del pulsómetro y una segunda sesión, y ampliar el conductor de `WorkoutGoldenTests`. Propuesta: la 4b, junto con el diálogo de completado, que cambia el fin automático (DT4a-15) y obliga a regenerar el oráculo de todos modos.
- **No se tocaron** los menores de la verificación principal que no entraron en esta ronda: QA-V4a-03 (el divisor de la banda), -04 (el primer Tab sin anillo), -05 (`TabularNumber` sobre elementos fuera del panel) y -06 (WP-B0). Tampoco QA-V4a-L01, que es del PO.
- **QA-V4a-05 sigue igual.** El rodillo de `TabularNumber` es finito: `Settle` lo espera y por eso la PlayMode de QA-V4a-02 ve el `MotionDriver` como en reposo, pero los avisos de DPI del registro siguen.

**Verificación hecha.** Todo en serie, una sola instancia de Unity por vez. No se compiló ni se abrió ningún build (los arreglos no lo necesitan) ni se tocó el que el usuario tenía abierto. El legacy, solo lectura: `git -C ../rowmate-legacy status --porcelain` vacío al empezar y al terminar; las líneas del legacy que se citan arriba son las de las lentes. Los resultados y registros quedaron en `<scratch>/it4a/fix-architect-c2/`.

| Qué | Resultado |
|---|---|
| dotnet (`tools/dotnet/run-tests.ps1 -UnityResults <scratch>/it4a/fix-architect-c2/unity -ArtifactsPath <scratch>/it4a/fix-architect-c2`) | `Build succeeded. 0 Warning(s) 0 Error(s)` · Simulator **173 / 173** · EditMode **2055: 2033 pasan, 0 fallan, 22 ignoradas** (QA: 2050 / 2028; +5 de esta ronda) · matriz con los resultados de Unity: **PASA 534, FALLA 0, IGNORADO 1** (AC-WKT-02-10, Android), PENDIENTE UNITY 0, SIN TEST 0. AC-WKT-01-09 y AC-WKT-13-01 pasan con `StartDuringThePreviousExitHasItsOwnImmersive` |
| Unity EditMode | **2093: 2067 pasan, 0 fallan, 26 ignoradas** (QA: 2088 / 2062; +5 de esta ronda) |
| Unity EditMode + simulador | **2266: 2240 pasan, 0 fallan, 26 ignoradas**, 0 inconclusas; el define restaurado |
| Unity PlayMode | **264: 263 pasan, 0 fallan, 1 ignorada** (`ScalesOfTheDesignNotes`, QA-V4a-06; +3 de esta ronda) |
| Unity PlayMode + simulador | **328: 327 pasan, 0 fallan, 1 ignorada** (la misma; +3 de esta ronda); el define restaurado, sin la marca |
| Arnés `it4a` (A y B), `it3` e `it2` | `it4a`: 49 tomas, 0 con problemas, 0 sondas que fallan; A = B = repo byte a byte (50 de 51 archivos; en `manifest.json` cambia solo `durationMs`). `it3` (47 tomas) e `it2` (41) iguales al repo byte a byte, salvo `durationMs`. Ninguna toma cambia: ningún arreglo toca lo que se ve en una foto ni en un video (el inicio durante una salida, el corazón y el pulso después de salir y el indicador oculto no salen en ninguna toma) |
| Después de las corridas | `ProjectSettings.asset` sin `ROWMATE_SIMULATOR`, sin `Library/RowMate/simulator-tests.txt` ni `Assets/Resources`; `UI/Fonts/Generated`, `UI/Icons/Generated` y `Settings/` sin cambios; las capturas del repo con los mismos md5; `git status --porcelain`, las mismas 340 entradas del principio (los archivos de esta ronda ya figuraban como cambiados o nuevos) |
| Repo público | 0 coincidencias de la búsqueda de rutas de la PC (versionado y nuevo); ningún nombre de equipo ni de usuario |
