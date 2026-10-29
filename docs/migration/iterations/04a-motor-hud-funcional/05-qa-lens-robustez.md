# It. 4a — Verificación QA, lente "robustez"

> Lente paralelo al QA principal. Foco: guardado incremental y cierres forzados, dobles toques, caídas del remo y del pulsómetro, segundo plano (Q-91), pantalla encendida, timers del `IScheduler`, excepciones sin observar, fugas al salir del inmersivo y asignaciones por cuadro del HUD.
> Sin Unity (otro agente lo usa): todo lo de pantalla es revisión estática del código, con las pruebas PlayMode que hacen falta escritas abajo para que las corra quien tenga el Editor. Lo de dotnet se corrió de verdad.
> Legacy de referencia: https://github.com/figuibej/rowmate-legacy (solo lectura; `git status --porcelain` vacío al empezar y al terminar).

---

## 0. Veredicto

**APROBADO CON OBSERVACIONES.** 0 bloqueantes, **2 mayores** y **3 menores** (§3).

- **El motor y el guardado son robustos donde se mira con dotnet.** El reloj no deriva ni duplica: en 200 ciclos de pausa y reanudación sobre el `FrameScheduler` de la app a 60 fps da 400 s exactos, 80 puntos y nunca más de un timer del motor vivo (L06). Un cuadro 1 h tarde tampoco pierde segundos (3602 s, L05). Los dobles toques en Pausa y Reanudar no tienen efecto en el motor. Cada lote y el cierre son una transacción (WAL y `synchronous = FULL`). Cerrar la ventana en el mismo cuadro de Terminar deja la sesión cerrada con sus 3 puntos (L08). Las excepciones de las tareas sueltas están todas observadas.
- **Mayor 1, QA-V4a-ROB-01: una sesión puede quedar sin HUD.** Pasa si se toca `Entrenamiento libre` durante los 200 ms en que sale el inmersivo anterior. La sesión queda en marcha sin Pausa ni Terminar. El legacy no tiene este problema.
- **Mayor 2, QA-V4a-ROB-02: dos animaciones del HUD quedan corriendo después de salir del inmersivo.** Son el latido del corazón de BPM y el pulso de la fila del paso. Retienen el HUD entero de cada sesión y dejan la app a 60 fps hasta cerrarla.
- **Menores: tres huecos en casos borde.** Dos de AC-WKT-06-15 / §12 punto 3, sobre lo que queda si el sistema mata la app en segundo plano o se cierra la ventana enseguida: ROB-03 (los puntos de un lote que falló) y ROB-04 (el cierre con un lote en vuelo). El tercero (ROB-05) es que, en primer plano, un cuadro muy tarde publica una foto por segundo.
- **NO VERIFICABLE en este lente:** todo lo PlayMode y Unity EditMode, Windows (`powercfg /requests`, minimizar) y Android. Va en §6.

---

## 1. Corridas

| Corrida | Resultado |
|---|---|
| dotnet, solución del repo (`powershell -NoProfile -File tools/dotnet/run-tests.ps1 -ArtifactsPath <scratch>/it4a/lens-robustez/artifacts`) | `Build succeeded. 0 Warning(s) 0 Error(s)`. `RowMate.Tests.Simulator`: **173 / 173**. `RowMate.Tests.EditMode`: **2050, 2028 pasan, 0 fallan, 22 ignoradas** (dispositivo, GitHub y Unity). `exit 0` |
| Copia del lente (`<scratch>/it4a/lens-robustez/proj/LensRobustez.csproj`: las fuentes de `Tests/EditMode` enlazadas, sin copiar ni tocar el repo, más 8 pruebas propias en `Lens/`) | 2058 pruebas: 2027 pasan, 9 fallan y 22 ignoradas. **Las 4 que fallan del lente son la evidencia** (L03, L04, L05, L08b). Las otras **5 que fallan son de entorno**, `SessionListToolTests`: "No está compilada la herramienta (RowMate.SessionList.dll)", porque la copia no compila `tools/sessions` |
| Repo | Sin cambios salvo este archivo (`git status --porcelain`: 340 líneas antes y después, las del trabajo de la 4a). Ningún `obj/` ni `bin/` en `tools/dotnet`. Legacy limpio |

Salidas de las pruebas del lente (copiadas de la consola):

```
[L03]  llamadas a la base: create, points:1, points:1 | puntos escritos al congelarse: 1        → FALLA (se esperaban 2)
[L03b] (Abandon junta el reintento con lo pendiente)                                           → PASA
[L04]  al congelarse: finished_at = NULL, puntos = 1, cierre terminado = False                 → FALLA
[L05]  1 h en un cuadro de primer plano: 3601 fotos, 3601 estados del HUD, 2 ms (sin la vista); reloj 3602 s; puntos 720 → FALLA (fotos)
[L06]  reloj 400 s, puntos 80, timers del motor vivos (max) 1                                  → PASA
[L07]  reloj 4 s; llamadas create; puntos escritos 0 (y 5 s / 1 punto en el instante 6,5)      → PASA
[L08]  finished_at = 2026-10-28T13:00:12.300Z, puntos = 3                                      → PASA
[L08b] finished_at = NULL, puntos = 2                                                          → FALLA
```

---

## 2. El foco, punto por punto

| Foco | Resultado | Evidencia |
|---|---|---|
| Guardado incremental: transacciones y lotes | **OK.** Un lote en vuelo a la vez; lo que llega mientras tanto sale en el siguiente; cada lote y el cierre en una transacción; WAL con `synchronous = FULL` | `Core/Workout/SessionRecorder.cs:100-117`, `:303-315`, `:359-380`; `Infrastructure/Sessions/SqliteSessionRepository.cs:85-98`, `:134-170`; `Infrastructure/Database/DatabaseSchema.cs:94-95`; `SSR.BatchIsOneTransactionInOrder`, `SSR.CloseIsAtomic` (pasan) |
| Cierre forzado en cada momento | **OK salvo dos casos borde** (tabla de abajo) | ROB-03, ROB-04 |
| Doble toque en Iniciar, Terminar y Pausa | **OK en el motor y los stores.** `StartFree` / `StartRoutine` devuelven `Busy` fuera de reposo; `FinishAsync` es idempotente; `Pause` y `Resume` repetidos no hacen nada (L06). **Hueco en la pantalla**: iniciar mientras sale el inmersivo anterior (ROB-01) | `Core/Workout/WorkoutEngine.cs:121-207`; `WST.SecondStartIsIgnored`; L06 |
| Desconexión del remo y del pulsómetro a mitad de sesión | **OK.** El motor no mira la conexión. El reloj y los pasos por tiempo siguen y los puntos repiten la última lectura. El pulsómetro que se va pone el pulso en 0 y la siguiente lectura del remo lo repone (LT-35 a LT-38) | `WorkoutEngine.cs:526-559`; `Core/Workout/HeartRateFusion.cs:19-47`; `WEL.EngineIgnoresTheConnection`, `WorkoutProviderHrTests` (pasan) |
| Caída simulada y reconexión | **OK en la lógica.** Un reinicio del monitor o un paquete sin distancia no suman metros (Q-55). La pantalla con el simulador es PlayMode (`WorkoutSimulatorTests.RowerDropsDuringTheSession`) y queda **NO VERIFICABLE** en este lente | `Core/Workout/SanitizedDistance.cs:34-44`; `SanitizedDistanceTests` |
| Segundo plano y vuelta (Q-91) | **OK**: alcance en tiempo virtual, una foto con `Instant` y topes que no cuentan el segundo plano. Si el sistema mata la app en segundo plano: ROB-03 y ROB-04 | `Core/Common/FrameScheduler.cs:88-109`; `Core/Common/ForegroundTimeout.cs`; `WorkoutBackgroundTests`, `FrameSchedulerTests`, `ForegroundTimeoutTests` (pasan) |
| Pantalla siempre encendida | **OK en lo estático.** Se pide en `Awake`, otra vez al volver o al recuperar el foco, y se libera en `OnDestroy`, en el mismo hilo. En Windows se pide `ES_CONTINUOUS \| ES_DISPLAY_REQUIRED \| ES_SYSTEM_REQUIRED` | `Bootstrap/AppRoot.cs:118-120`, `:295-296`; `Core/Common/ScreenKeepAwake.cs`; `Platform/Power/WindowsWakeLock.cs:46-86`; `ScreenKeepAwakeTests` (pasan). `powercfg /requests`: NO VERIFICABLE |
| Timers del `IScheduler`: sin deriva ni duplicados al pausar y reanudar | **OK.** El reloj está anclado al inicio del tramo (`ActiveClock`) y hay un solo timer de una vez, rearmado al próximo vencimiento. Q-58: la fracción del segundo se pierde al pausar | `Core/Workout/ActiveClock.cs:23-61`; `WorkoutEngine.cs:297-331`; **L06** (400 s exactos, máx. 1 timer), **L07** (pausa antes de que abra el primer tramo, con alta lenta: un solo tramo), **L05** (1 h tarde: no se pierde ningún segundo) |
| Excepciones sin observar | **OK.** Cada tarea suelta se observa: `SessionRecorder.cs:356` → `ObserveBatch` captura; `WorkoutEngine.cs:261` → `OpenWhenCreated` captura; `ImmersiveWorkoutPage.cs:129` → `Forget`. Los suscriptores que lanzan los registran `ObservableValue` y `EventStream`; un callback que lanza lo registra `RealtimeTimerQueue` (`:135-142`). `FinishTask` nunca queda fallada | revisión estática |
| Fugas de suscripciones al salir del inmersivo | **Suscripciones OK**: `Bind` se da de baja al quitarse del panel, `Hud.Dispose()` y `layout`, `zones`, la llave y la raíz del panel se desenganchan en `OnPopped`. **Animaciones: fuga (ROB-02)** | `Presentation/Workout/ImmersiveWorkoutPage.cs:89-105`; `Presentation/Binding/ViewBinding.cs:39-77` |
| Asignaciones por cuadro del HUD | **Sin hallazgos en lo estático.** `HeartbeatAnimator.Tick`, `StepPulse.Tick`, `MotionDriver.Tick` y `RealtimeTimerQueue.RunDue` no asignan (el `RemoveAll` usa una lambda sin captura, que el compilador cachea). La medición es `HudBudgetTests.NoAllocationsPerFrameWhileItOnlyBeats` (PlayMode) y queda **NO VERIFICABLE** acá. Lo que sí hay es trabajo por cuadro de más después de salir (ROB-02) | revisión estática |

**Qué queda tras un cierre forzado en cada momento** (SQLite real, salvo donde se aclara):

| Momento | Qué queda en la base | Evidencia |
|---|---|---|
| Justo al iniciar, con el alta en vuelo | Nada, o la fila sin fin y sin puntos (AC-WKT-06-10). Los puntos tomados con el alta en vuelo salen detrás del alta al pasar a segundo plano o al cerrar | `SRC.PointsTakenWhileCreatingAreFlushedOnBackground`, `SSR.PointsAfterCreateWaitForTheCreate` (pasan) |
| A mitad (31,5 s, libre con R-4) | Los 6 puntos de los segundos 5 a 30, sin fin y con los totales en 0 | `SSR.KilledAt31_5sHasSixPoints` y `…WithTheSerialExecutor` (pasan) |
| Durante un lote | Todo o nada: la transacción hace rollback | `SSR.BatchIsOneTransactionInOrder` (pasa) |
| Al terminar, con el cierre ya encolado | Cerrada con todos sus puntos, o cortada sin el final. Cerrar la ventana en el mismo cuadro: cerrada, 3 puntos | `SSR.CloseIsAtomic`; **L08** (pasa) |
| Al terminar con un lote en vuelo, y enseguida segundo plano o cerrar la ventana | **Cortada, sin el punto final** | **L04, L08b → ROB-04** |
| En segundo plano después de un lote que falló | **Falta el punto de ese lote** | **L03 → ROB-03** |
| En segundo plano con la base al día | Todos los puntos hasta que se fue | `WBG.BackgroundFlushesPendingWrites` (pasa) |

---

## 3. Issues

| ID | Sev. | Resp. | Issue | Evidencia | Corrección sugerida |
|---|---|---|---|---|---|
| QA-V4a-ROB-01 | mayor | architect | Una sesión iniciada mientras sale el inmersivo anterior se queda **sin HUD**, sin Pausa ni Terminar | `Presentation/Workout/WorkoutScreenView.cs:207`, `:215-230`, `:405-420`; `Presentation/Shell/PageStack.cs:107-110`, `:149-159`, `:221-223`; `Presentation/Shell/ExitLock.cs:60-69`; `App/Workout/WorkoutRestState.cs:18`; `Core/Design/MotionSpecs.cs:220-221`. Legacy: `lib/features/workout/workout_screen.dart:88-89`, `:164-165`, `:169` | Que `Immersive` no cuente la página que está saliendo (`!_ui.Pages.IsExiting(_immersive)`; `PageStack.Push` ya acepta una página nueva mientras otra sale), o volver a mirar la fase en el `OnPopped` del inmersivo viejo. Sumar la PlayMode de §4.1 |
| QA-V4a-ROB-02 | mayor | architect | Al salir del inmersivo quedan en el `MotionDriver`, para siempre, el **latido** de la tarjeta BPM (con pulso > 0) y el **pulso de la fila del paso** (si se terminó a mano con "Próximo" o "por terminar"). Retienen el HUD de cada sesión y dejan una demanda de 60 fps | `Presentation/Workout/HudView.cs:214`, `:257-275`; `Presentation/Workout/HudCard.cs:137` (sin `Dispose` ni `DetachFromPanel`); `Presentation/Motion/HeartbeatAnimator.cs:65-70`, `:77-98`, `:100-106`; `Presentation/Workout/StepInfoBar.cs:157-162`, `:185`; `Presentation/Motion/StepPulse.cs:147-151`, `:154-174`; `Presentation/Motion/MotionDriver.cs:120-139`, `:193-214`; `Core/Design/FramePacingPolicy.cs:52-62`; `Core/Workout/WorkoutEngine.cs:504-506`; `Core/Workout/StepProgress.cs:82-89`. Legacy: `immersive_workout_screen.dart:326-328` (`_pulseCtrl.dispose()`) | En `HudView.Dispose`, o al quitar `HudCard` y `StepInfoBar` del panel: `Bpm.Heartbeat?.Stop()` y un `StepPulse.Stop()` nuevo (`MotionDriver.Remove` + `_running = false`). Que `StepInfoBar.Apply(null)` apague el pulso. Sumar la PlayMode de §4.2 |
| QA-V4a-ROB-03 | menor | architect | Al pasar a segundo plano, `FlushAndWait` no manda los puntos de un lote que falló (esperan al cierre). Si el sistema mata la app, se pierden aunque la base ya ande. `Abandon` sí los manda | **L03** (falla) y **L03b** (pasa); `Core/Workout/SessionRecorder.cs:223-240` contra `:244-259`; `:318-340`. Spec §12 punto 3 / AC-WKT-06-15: "todo lo pendiente" | Igual que en `Abandon`: si el cierre no está encolado, pasar `_retry` al frente de `_pending` antes de `FlushPending()` |
| QA-V4a-ROB-04 | menor | architect | Si se confirma Terminar con un lote en vuelo y enseguida la app pasa a segundo plano, o se cierra la ventana, el cierre no llega a encolarse: espera la vuelta del lote en el hilo principal. La sesión queda **cortada y sin el punto final** | **L04** (`finished_at = NULL`, 1 punto) y **L08b** (`finished_at = NULL`, 2 puntos); **L08** pasa sin el lote en vuelo; `SessionRecorder.cs:147`, `:166-195`, `:206`; `WorkoutEngine.cs:211-233`, `:573-589` | Encolar el cierre en el acto, detrás de lo que está en vuelo, y resolver en el hilo de la base qué lotes fallaron (como `AddPointsAfterCreateAsync` resuelve el alta). Como mínimo, que `FlushAndWait` y `Abandon` encolen ese cierre si `_finishing` y todavía no salió. Si se acepta como riesgo, anotarlo junto a RA-08 |
| QA-V4a-ROB-05 | menor | architect | En primer plano, un cuadro muy tarde publica **una foto por segundo procesado** (no una sola con `Instant`, como al volver del segundo plano). Con 1 h: 3601 fotos y 3601 estados del HUD en el mismo cuadro. Si Windows suspende el equipo con una sesión en marcha, al despertar la sesión suma la hora suspendida con 720 puntos de la última lectura | **L05** (falla en la cuenta de fotos; el reloj da 3602 s); `WorkoutEngine.cs:311-331`, `:599-609`; `Core/Common/FrameScheduler.cs:76-80`. Que el `Stopwatch` de Windows siga contando durante la suspensión **no lo verifiqué en el equipo** | Si `OnClock` procesa más de un segundo, publicar una sola foto al final, con `Instant`. **Pregunta al PO**: ¿la suspensión del equipo cuenta como tiempo de sesión? AC-WKT-02-09 solo cubre la ventana minimizada o sin foco |

---

## 4. Detalle de los mayores

### 4.1 QA-V4a-ROB-01 — Sesión sin HUD si se inicia mientras sale el inmersivo anterior

**Escenario.** Una rutina `Corta` con R-4 termina sola a los 30 s y el cierre vuelve en el acto (base al día).

1. En ese cuadro el inmersivo empieza a salir con un fundido de 200 ms (120 con reducir movimiento).
2. En el cuadro 0 de la salida, `ExitLock` deja inerte todo el árbol de la página (`pickingMode = Ignore`) y `SetCovers(false)` vuelve a mostrar las pestañas.
3. Como la fase ya es `Idle`, `Render` mostró la vista de reposo con `Entrenamiento libre` habilitado (`CanStart = RowerConnected && IsIdle`). Queda debajo de un HUD que se desvanece y **responde**.
4. Un toque ahí antes de los 200 ms llama `StartFree()`: la fase pasa a `Active`, `Render` corre y llama `OpenImmersive()`.
5. `Immersive` devuelve la página vieja, porque `PageStack.HasPage` la encuentra hasta `OnExited`. `OpenImmersive` no apila nada.
6. Cuando el fundido termina, la página vieja sale. La sesión nueva corre (reloj y puntos) **sin HUD**:
   - Workout muestra "Entrenamiento en curso..." con la barra de pestañas a la vista;
   - "Atrás" de Android no abre la confirmación, porque no hay página;
   - no hay Pausa ni Terminar.

`Render` solo vuelve a correr si cambia `WorkoutRestState`: desconectar y volver a conectar el remo en Dispositivo, o editar una rutina, hace aparecer el HUD. Si nadie hace eso, la sesión termina cortada cuando se cierra la app.

Con Terminar a mano la ventana es más chica, porque el velo del diálogo se traga los toques durante 150 ms. En el fin automático, o después del tope de 15 s del cierre, son los 200 ms enteros.

**Legacy.** Cada inicio apila su propia ruta en el mismo toque (`workout_screen.dart:88-89`, `:164-165`; `_openFullscreen`, `:169`), así que no le pasa. Es una regresión del cambio a "el inmersivo se abre por el estado". Contradice AC-WKT-01-09 ("se alcanza a ver solo mientras el inmersivo entra o sale") y §12 punto 13.

**PlayMode propuesta** (`Tests/PlayMode/WorkoutScreenTests.cs`). Hoy debería fallar en la última línea:

```csharp
[UnityTest, Category("AC-WKT-13-04"), Category("AC-WKT-01-09")]
public IEnumerator StartDuringThePreviousExitHasItsOwnImmersive()
{
    CreateWorkout(RoutineSeeds.F4A);
    yield return StartRoutine("Corta");
    yield return AdvanceTo(30.02);                    // fin automático; con la base en línea el cierre vuelve en el acto
    Assert.That(Phase, Is.EqualTo(WorkoutPhase.Idle));
    Assert.That(Screen.Immersive!.IsExiting, Is.True, "el inmersivo de la Corta está saliendo (200 ms)");
    H.Tap("workout-free");                            // la vista de reposo ya responde debajo (ExitLock)
    yield return Settle();
    Assert.That(Phase, Is.EqualTo(WorkoutPhase.Active));
    Assert.That(H.Immersive, Is.Not.Null, "la sesión nueva tiene su HUD");
}
```

### 4.2 QA-V4a-ROB-02 — El latido y el pulso del HUD siguen después de salir

**Escenario 1, el latido.** Una sesión con pulso > 0 (pulsómetro conectado, o el FTMS con pulso):

1. Al volver a reposo, la foto `Idle` conserva el pulso: AC-WKT-01-07, `WorkoutEngine.cs:504-506`.
2. `HudView.Apply` todavía la dibuja y llama `Bpm.Heartbeat.SetState(true, true, bpm)`.
3. La página sale y `HudView.Dispose` no para el `HeartbeatAnimator`. `HudCard` no tiene `Dispose` ni escucha `DetachFromPanelEvent`.
4. El animador queda en el `MotionDriver` y su `Tick` devuelve `true` mientras `IsBeating`, que ya nadie cambia.

**Escenario 2, el pulso.** Una rutina que se termina a mano con "Próximo" (los últimos 15 s de un paso que no es el último) o "por terminar" (los últimos 5 s):

1. La foto `Finished` conserva la fila del paso con `UpNext` o `EndingSoon`, así que `StepPulse` queda activo.
2. La foto `Idle` trae `Step = null`, y `StepInfoBar.Apply(null)` vuelve sin tocar el pulso.
3. `StepPulse.Tick` devuelve `true` mientras `_active`, sin fin. No tiene `Stop`.

Pasa lo mismo si al volver del segundo plano la sesión ya está en reposo (`PopImmediately`). En rutinas de intervalos cortos es la mitad de las veces o más: con pasos de 10 s, siempre.

**Efecto.**

- **Memoria.** Cada animación que queda retiene el árbol entero del HUD de su sesión (≈ 60 elementos, los closures de los controles y el `WorkoutHudStore` dispuesto), y se van sumando sesión tras sesión.
- **Trabajo por cuadro.** Escriben estilos en cada cuadro sobre elementos que ya no están en el panel.
- **Frecuencia de cuadros.** `MotionDriver.Add` hace `Acquire` sobre `FramePacingPolicy` y el `Release` llega solo cuando la animación sale, que acá no pasa. La app queda a **60 fps en vez de 30 en reposo hasta cerrarla** (ARCHITECTURE §13.1).
- **Por qué las pruebas no lo ven.** Las dos son animaciones continuas (`IsFinite = false`) y `Settle` solo espera las finitas.

El legacy libera el `AnimationController` del pulso con el widget (`immersive_workout_screen.dart:326-328`).

**PlayMode propuesta** (`Tests/PlayMode/ImmersiveHudTests.cs`). Hoy debería fallar en los `Contains`:

```csharp
[UnityTest, Category("AC-WKT-13-04")]
public IEnumerator LeavingTheImmersiveStopsItsContinuousMotions()
{
    CreateWorkout(RoutineSeeds.F4A, null, k => RowerScripts.R4(k).CopyWith(heartRate: 140));
    yield return StartRoutine("Prueba 4a");
    yield return AdvanceTo(20.05);                    // calentamiento de 30 s: "Próximo" y el corazón latiendo
    var heart = Hud.Bpm.Heartbeat!;
    var pulse = Hud.Steps!.Pulse;
    Assert.That(pulse.IsPulsing, Is.True);
    yield return Tap("hud-finish");
    yield return Frames();
    H.Tap("dialog-confirm");
    yield return Settle();
    Assert.That(H.Immersive, Is.Null);
    Assert.That(H.Handle.Motion.Contains(heart), Is.False, "el corazón del HUD sigue en el MotionDriver");
    Assert.That(H.Handle.Motion.Contains(pulse), Is.False, "el pulso de la fila sigue en el MotionDriver");
}
```

---

## 5. Las pruebas del lente

Están en `<scratch>/it4a/lens-robustez/proj/Lens/`, nunca en el repo, sobre la solución de `tools/dotnet` y con los ayudantes de `Tests/Support` y `Tests/EditMode` (`WorkoutHarness`, `LifecycleHarness`, `TempDatabase`, `TestMainThreadContext`, `ScriptedSessionRepository`).

| Prueba | Qué hace | Resultado |
|---|---|---|
| `L03_BackgroundFlushAlsoSendsTheRetryPoints` | El lote del segundo 5 falla una vez (`FailBatch = 1`) y el del 10 entra. Segundo plano a los 10,5 s; se cuenta lo escrito al congelarse | **FALLA**: 1 punto, se esperaban 2 (ROB-03) |
| `L03b_AbandonDoesSendTheRetryPoints` | Lo mismo, pero se cierra la app (`Dispose`) | pasa: 2 puntos |
| `L04_FinishThenBackgroundWithABatchInFlightStillCloses` | SQLite real y `SerialExecutor`. Base ocupada desde 4,2 s, Terminar a los 5,5 y segundo plano a los 5,6. Sin bombear más el hilo principal, se lee desde otra conexión | **FALLA**: `finished_at = NULL`, 1 punto (ROB-04) |
| `L05_OneHourStallInTheForegroundRebuildsTheHudEverySecond` | `FrameScheduler`: un cuadro 1 h tarde en primer plano, con `WorkoutHudStore` | **FALLA**: 3601 fotos y 3601 estados del HUD (ROB-05). El reloj da 3602 s: no se pierde ningún segundo |
| `L06_TwoHundredPauseResumeCyclesNoDriftNoDuplicates` | 200 ciclos de 2,7 s en marcha y 1,3 s en pausa a 60 fps, con doble Pausa y doble Reanudar en cada ciclo | pasa: 400 s, 80 puntos, a lo sumo 1 timer del motor y ninguno en pausa |
| `L07_PauseBeforeTheFirstSegmentThenSlowCreate` | Alta de 2 s; pausa a los 50 ms (antes de que abra el primer tramo) y reanudar en el 1 | pasa: 4 s a los 5,5; 5 s y 1 punto a los 6,5. Sin un segundo tramo cuando vuelve el alta |
| `L08_QuitRightAfterFinishKeepsTheClose` | Terminar a los 12,3 s y `OnDestroy` en el mismo cuadro (motor → `LocalStore.Dispose`) | pasa: cerrada a las 13:00:12.300, 3 puntos |
| `L08b_QuitRightAfterFinishWithABatchInFlightKeepsTheClose` | Lo mismo, con el punto del 10 en vuelo | **FALLA**: `finished_at = NULL`, 2 puntos (ROB-04) |

---

## 6. NO VERIFICABLE en este lente (y por qué)

| Qué | Motivo | Quién lo cubre |
|---|---|---|
| ROB-01 y ROB-02 en pantalla | Sin Unity (la tarea lo prohíbe) | Las PlayMode de §4.1 y §4.2, en la corrida de Unity del QA principal |
| AC-WKT-01-10, -20-06, -20-07, -13-04 en pantalla; `HudBudgetTests` (asignaciones y presupuesto) | PlayMode | QA principal (`WorkoutScreenTests`, `HudControlsTests`, `HudBudgetTests`) |
| `WakeLockTests`, `UnitySchedulerTests` | Unity EditMode | QA principal |
| AC-APP-14-01 a -03 (`powercfg /requests`), AC-WKT-02-09 (minimizada 2 min), AC-WKT-06-05 con el Administrador de tareas | [Device-Win]: `powercfg` pide una consola de administrador. Lanzar builds desde carpetas nuevas dispara avisos del Firewall (QA-V3-08) | QA principal o el usuario en la demo |
| AC-WKT-02-08 y -10, AC-APP-14-02 en Android | Sin módulo de Android ni teléfono | It. 10a |
| ROB-05: si el `Stopwatch` de Windows cuenta la suspensión del equipo | Habría que suspender el equipo del usuario | El usuario o el Architect, si el PO decide que importa |
