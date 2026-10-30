# It. 4b — Verificación QA, lente "motion"

> Lente paralelo al QA principal. Foco: motion y robustez. Se revisó que ninguna animación adelante ni frene el reloj ni las métricas, que nada quede animando ni retenga el HUD al salir del inmersivo (la lección de ROB-02), el frame pacing en reposo, reducir movimiento, el doble toque y las páginas que salen (la lección de ROB-01) con el diálogo de completado, el motor que espera al diálogo ante un cierre forzado o el segundo plano, P-17 (el lapso suspendido no deja puntos) y las asignaciones por cuadro.
> Sin Unity (otro agente lo usa). Todo lo de pantalla es revisión estática del código, y las pruebas PlayMode que hacen falta están escritas abajo para que las corra quien tenga el Editor. Lo de dotnet se corrió de verdad.
> Legacy de referencia: [rowmate-legacy](https://github.com/figuibej/rowmate-legacy) (solo lectura, `3ae0f3a`). `git -C ../rowmate-legacy status --porcelain` dio vacío al empezar y al terminar. No se leyó `lib/core/strava/strava_secrets.dart`.

---

## 0. Veredicto

**APROBADO CON OBSERVACIONES.** Hay 0 bloqueantes, 0 mayores y **5 menores** (§3). Ninguno frena la iteración.

- **La regla de oro se cumple.** El motor no depende de la vista. `PresentationIndependenceTests` da la misma secuencia del motor con *store* del HUD y sin él, en `Estados`, `Corta` y `Prueba 4a`. Las animaciones leen el reloj del `MotionDriver` y no lo mueven. El relleno de la píldora tiene tope en el tick siguiente (`PillProgress`) y el sello se ancla al instante del motor (`LastOutcomeAt`). La cuenta grande sale del restante del motor. El diálogo no frena nada: el cierre empieza en el segundo del fin y `Terminar` solo vuelve a reposo.
- **No se repite ROB-02.** `HudView.Dispose` detiene todos los `IMotion` continuos de la 4b y la página suelta los 60 fps en el cuadro 0 de la salida. El diálogo detiene el conteo en `OnClosing` (§2, punto 2).
- **El motor que espera al diálogo es sólido en dotnet.** Lo prueban las 14 pruebas de `CompletionHoldTests` y LM-05 de esta lente. En LM-05, `Corta` espera al diálogo 10 minutos, con lecturas, dos idas y vueltas del segundo plano y una suspensión de 600 s: no suma ningún punto ni ningún aviso y el cierre queda bueno. `Terminar` vuelve a reposo y avisa `Ended(Completed, true)`.
- **P-17 se cumple en dotnet.** Lo prueban las 13 pruebas de `WorkoutSuspendTests` y LM-06 de esta lente. En LM-06, `Corta` se suspende 600 s a los 12,4 s, y da lo mismo que el crudo cuente el sueño o no: termina a los 30 s en marcha con los mismos 7 puntos que sin dormir.
- **Menores:**
  - MOT-01: el conteo del diálogo asigna memoria en cada cuadro.
  - MOT-02: con la salida nueva, un toque durante sus 400 ms en negro le llega a Workout, que todavía no se ve.
  - MOT-03: el punto en vivo no se entera de que se prendió "reducir movimiento" a mitad de la sesión.
  - MOT-04: cada vuelta del segundo plano queda en el registro como un "cuadro tarde".
  - MOT-05: a la prueba de "nada animando al salir" le faltan dos de los casos que prometía el diseño.
- **NO VERIFICABLE en este lente:**
  - Todo lo PlayMode y Unity EditMode (el Architect reporta las cuatro suites en verde, §1).
  - Los avisos de suspensión de Windows en el build (demo (c) del usuario).
  - El registro del build después de tres entrenamientos (AC-WKT-13-09 [Device-Win]).
  - Android.

---

## 1. Corridas

| Corrida | Resultado |
|---|---|
| dotnet, solución del repo (`powershell -NoProfile -File tools/dotnet/run-tests.ps1 -ArtifactsPath <scratch>/it4b/lens-motion/repo-run`) | `Build succeeded. 0 Warning(s) 0 Error(s)`. Herramientas **182 / 182**. Core **2306: 2279 pasan, 0 fallan, 27 ignoradas** (dispositivo, GitHub y Unity, entre ellas `WindowsSuspendDoesNotCount`, la de la demo (c)). `exit=0`. En el foco de esta lente: `CompletionHoldTests` 14 / 14, `WorkoutSuspendTests` 13 / 13, `SuspendAccountingTests` en verde y `PresentationIndependenceTests` 3 / 3 |
| Copia del lente (`<scratch>/it4b/lens-motion/sln`: los `.csproj` de Core, App, Infrastructure y Tests.Support copiados, con las fuentes de `Assets/` enlazadas sin tocarlas, más el proyecto `Probe` con 7 pruebas propias) | **7 / 7 pasan.** Las de MOT-01 y MOT-04 pasan porque **afirman el defecto** (§5) |
| Repo | No se cambió nada salvo este archivo. `git status --porcelain` dio 405 líneas antes y después, todas del trabajo de la 4b. Aparecieron `tools/dotnet/{RowMate.Core,RowMate.Infrastructure,RowMate.SessionList}/{bin,obj}` en Debug, pero no son de esta lente: las compilaciones del lente van a `<scratch>` y ninguna toca `RowMate.SessionList`. Están en `.gitignore` |
| Legacy | `git status --porcelain` vacío al empezar y al terminar |

Salidas de las pruebas del lente, copiadas de la consola:

```
[LM-01] totales 30 s / 455 m / 27 kcal: 6592 B en 70 cuadros (94 B por cuadro)
[LM-01] totales 35999 s / 16777215 m / 65535 kcal: 11232 B en 70 cuadros (160 B por cuadro)
[LM-04] [TIME] Info: cuadro de 600.0 s sin aviso de suspensión: cuenta (AC-WKT-02-04)
[LM-04b] [TIME] Info: cuadro de 60.0 s sin aviso de suspensión: cuenta (AC-WKT-02-04)
[LM-05] fase Finished, cierre Saved, reloj 30 s, puntos 7 (al terminar 7), avisos 0, diálogo 30 s / 1120 m
[LM-06] crudo cuenta=True: fase Finished, reloj 30 s, puntos 7, fin 2026-10-28T13:10:30.0000000+00:00
[LM-06] crudo cuenta=False: fase Finished, reloj 30 s, puntos 7, fin 2026-10-28T13:10:30.0000000+00:00
Total tests: 7  Passed: 7
```

---

## 2. El foco, punto por punto

| # | Tema | Resultado | Evidencia |
|---|---|---|---|
| 1 | La animación no adelanta ni frena el reloj ni las métricas | **PASA** | `PillProgress.Advance`: el relleno es `min(objetivo; (transcurrido + 1) / D)` y nunca baja dentro del paso; en pausa queda quieto. `CenterDiscMotion.RingFraction` sale del mismo `PillProgress`. `SealMotion.Show` ancla `t = 0` en `HudSeal.At = LastOutcomeAt`, el instante monótono del segundo que cerró el paso (`WorkoutEngine.cs:484-489`), sobre el mismo reloj del `MotionDriver`. `BigCountdown.ValueOf` sale del restante del motor. `HudControls`: `Pausa` y `Reanudar` actúan en el acto; solo se abstienen durante la cascada (840 ms, P-15). `PresentationIndependenceTests` 3 / 3 en la corrida. Lo único que "cuenta" son los totales del diálogo, a propósito (ANIMATION_BIBLE §2.5), y siempre terminan en el valor exacto (`CountUp.ValueAt`) |
| 2 | Nada queda animando ni retiene el HUD al salir (ROB-02) | **PASA** (estático; las PlayMode no se corrieron acá) | `ImmersiveWorkoutPage.OnPopped` (`:141-171`) detiene la cascada y la salida, cancela el *timer* de 300 ms y llama a `HudView.Dispose` (`HudView.cs:328-355`). Ahí paran el latido, `StepInfoBar.Dispose` (el pulso de la fila, `PillRowMotion`, `InfoRowMotion` y `SealMotion`), `FreeTopBar.Dispose` (`PhaseChipMotion` y `LiveDotMotion`), `CenterCallouts.Dispose`, el velo, kcal, el *timer* del *spinner* y las texturas. El *store* dispone `WallClockTicker`. Cada vista nueva para lo suyo también en `DetachFromPanelEvent`. `CompletionDialog.OnClosing` detiene `CompletionMotion` en el cuadro 0 de su salida (`:152-157`). `HudMotion.Stop` sale del `MotionDriver` sin avisos. `HudMotionTests.NoAllocationsPerFrameAndStopLeavesNothingAnimating` lo prueba con los 11 `IMotion` y deja el *driver* vacío; `ImmersiveHudTests.LeavingTheImmersiveStopsItsContinuousMotions` e `ImmersiveTransitionTests.NothingAnimatesAfterTheExit` comparan `ActiveCount` y `Demands` con los de reposo. Ver O-1 y MOT-05 |
| 3 | *Frame pacing* en reposo | **PASA** (estático) | La página pide 60 fps en `DeclareInitialSurface` (`:475-479`) y los suelta en `ReleaseShell` en el cuadro 0 de la salida (`:455-459`). Cada `HudMotion` pide y suelta lo suyo con `Run`/`Leave` (`MotionDriver.Add`/`Compact`). `WallClockTicker` no tiene periódico en segundo plano y se dispone con el *store*. En reposo, `FramePacer` vuelve al intervalo 2 (30 fps); IHT lo afirma con `Demands = 0` antes de la sesión y lo mismo después. Ver O-2 |
| 4 | Reducir movimiento | **PASA, salvo MOT-03** | Los `IMotion` de la 4b siguen la tabla de §3.10. `HTL.ReducedVariants` (dotnet) y las `Reduced*` de `HudMotionTests` (PlayMode) prueban cada fila. Leen `ReduceMotion` al empezar cada fase, como manda `MotionSettings`. El halo lo lee en cada cuadro (`PillRowMotion.Compose`, `:248`), igual que el latido y el pulso de la 4a. El punto en vivo no: ver MOT-03 |
| 5 | Doble toque y páginas que salen, con el diálogo | **PASA, con MOT-02** | `Terminar` del diálogo queda tomado desde el primer toque (`CompletionDialog._finishing`, `:218-235`), `AddAction` solo actúa con `IsOpen` y `Cancel` es idempotente (`ModalDialog.Close`, el primer cierre gana). La página tiene `_exiting` en `OnFinishTapped`, `MaybeScheduleCompletion` y `OpenCompletion`. Una sesión nueva durante la salida tiene su inmersivo (ROB-01 resuelto en la 4a; `WSC.StartDuringThePreviousExitHasItsOwnImmersive`, con el diálogo, según B-15). La vieja solo retira lo suyo: superficie, obstáculo, llave y barras por dueño (`ImmersiveSystemBars._owners`). Lo nuevo de la 4b es la salida en negro, ver MOT-02 |
| 6 | El motor espera al diálogo sin perder puntos | **PASA** | `WorkoutEngine.cs:243-259`, `:601-620`. `CompletionHoldTests` 14 / 14: `NoPointsAfterTheEnd`, `AcknowledgeWaitsForASlowClose`, `CloseTimeout`, `LateGoodCloseBeforeAcknowledgeCountsAsSaved`, `FailedClose` y `LiveReadingsKeepPublishing`. **LM-05** (propia): 10 minutos con el diálogo, segundo plano dos veces y una suspensión, y sigue con 7 puntos, 0 avisos y `Close = Saved` |
| 7 | Cierre forzado o segundo plano con el diálogo | **PASA** | `Dispose` (`:263-291`) hace lo de la 4a: `Abandon`, que con el cierre encolado no hace nada más, o espera hasta 1 s y lo encola (ROB-04). Nadie queda esperando el reposo (`_idleWaiter.TrySetResult`). En segundo plano, `FlushAndWait` con el cierre encolado solo espera la base (`SessionRecorder.cs:247-249`). `QuitWithTheDialogOpen`, `EndsInBackground` y `EndsInALongBackgroundWithSqlite` (SQLite real, 8 a 330 s en segundo plano: 7 puntos y ningún `workoutSaveError` falso) en verde |
| 8 | P-17: sin puntos del lapso suspendido | **PASA en dotnet**; Windows real NO VERIFICABLE | `SuspendAccounting.Awake` (`:80-195`) y `FrameScheduler.Tick` / `ResumeFromBackground` (`:98-154`): el motor, el remo simulado (`SchedulePeriodic`), `ForegroundTimeout`, el reloj de pared y el `MotionDriver` miden con el reloj despierto. No queda ninguna lectura de `Time.*` ni `Stopwatch` para lógica fuera del arnés y de `FrameTimeProbe` (búsqueda en `Assets/RowMate` sin `Tests/`). `WorkoutSuspendTests` 13 / 13. **LM-06** (propia), en una rutina: 7 puntos y 00:30 en el diálogo en los dos casos del monótono; la hora de fin es la de pared, con el sueño adentro. Ver MOT-04 |
| 9 | Asignaciones por cuadro | **PASA en los `IMotion`; MOT-01 en el diálogo** | Los `IMotion` solo usan *structs* y *callbacks* creados una vez (`HudMotionTests`: 0 B en 20 cuadros, PlayMode). En las vistas, los *callbacks* por cuadro (`ApplyPiece`, `ApplyExit`, `CenterCallouts.ApplyVisual`, `SealRow.ApplyVisual`, `PhaseChipView.ApplyVisual`, `HudBackdrop.SetVeil`, `ApplyKcalSlide` y `StepInfoBar.ApplyGaps`) usan *structs* de estilo. `PillRowView.Draw` usa búferes propios. `LocalOffsetSource` lee la zona sin asignar. La excepción es `CompletionDialog.ApplyVisual`, ver MOT-01 |

---

## 3. Issues

| ID | Sev. | Resp. | Issue | Evidencia | Corrección sugerida |
|---|---|---|---|---|---|
| QA-V4b-MOT-01 | menor | architect | **El conteo del diálogo de completado asigna memoria en cada cuadro.** `CompletionDialog.ApplyVisual` llama a `ShowValues` en cada cuadro del conteo (0 a 1160 ms) y formatea los tres totales cada vez, aunque no cambien: `WorkoutFormats.SessionClock(time)`, `distance.ToString(…)` y `calories.ToString(…)`. Rompe la regla de 0 B por cuadro de §3.11 y §7.4, y ninguna prueba lo mira: la de 0 B usa *callbacks* de prueba, no los de la vista | `Presentation/Workout/CompletionDialog.cs:204-216`. **LM-01**: con los totales de `Corta` son 6592 B en 70 cuadros (94 B por cuadro); con los de `Maratón` con `R-4X`, 11 232 B (160 B por cuadro), una vez por sesión | Formatear solo cuando cambia el valor que se ve (guardar los últimos tres `long` y saltear `SetValue` si son iguales), y sin cadena nueva por valor: un `char[]` propio para `00:00`/`0:00:00` y las cifras, o un `TabularNumber.SetDigits(long)` sin asignar. Sumar el diálogo a la prueba de 0 B (PlayMode, §5.2) |
| QA-V4b-MOT-02 | menor | architect (con el OK del PO) | **Durante los 400 ms en negro de la salida, un toque le llega a Workout, que todavía no se ve.** Escenario: `Corta` termina, el cierre ya volvió y en TH, con 3 rutinas o más, se toca `Terminar` del diálogo dos veces, a 200 ms una de otra. El primer toque devuelve el motor a reposo y empieza la salida, y el velo del diálogo que se cierra solo se traga toques durante 150 ms (DT-21). El segundo toque cae en la tarjeta de la rutina que está debajo: al verse Workout aparece `¿Empezar …?` sin que el usuario lo haya pedido. Si debajo está `Entrenamiento libre`, arranca una sesión nueva. Lo mismo pasa con un toque al aire mientras la pantalla está negra, y en la barra de pestañas cambia de pestaña. En la 4a la salida era un fundido de 200 ms con Workout a la vista desde el cuadro 0 (QA-V4a-01 aceptó los toques durante la salida). En el legacy la ruta que sale ignora los toques y la de abajo se ve mientras recibe toques. Ahora son **400 ms de negro**. Es PLAUSIBLE: se dedujo del código y falta la PlayMode de §5.2 | `Presentation/Shell/PageStack.cs:158-159` (en el cuadro 0, `ExitLock` deja la página inerte y `SetCovers(false)` vuelve a mostrar pestañas y barra debajo); `Presentation/Motion/PageTransition.cs:222-233` (`Immerse`: la página queda a 1 hasta los 400); `MotionSpecs.DialogOutMs` = 150. AC-WKT-13-08 dice "lo que sale no responde", pero no dice nada de lo que está debajo | Que lo que está debajo no reciba toques hasta `RevealStarted`: que el inmersivo que sale **absorba** los toques sin actuar en 0-400 (un bloqueador en la raíz que solo corte la propagación, o `PageStack` sin `SetCovers(false)` hasta el aviso) y suelte desde los 400. Con reducir movimiento la salida es un fundido de 120 y Workout se ve desde el cuadro 0, así que no cambia nada. El PO confirma que el toque de ROB-01 (AC-WKT-01-09) vale desde que empieza a verse Workout |
| QA-V4b-MOT-03 | menor | architect | **El punto en vivo sigue latiendo si se prende "reducir movimiento" en la mitad de un libre.** `LiveDotMotion` mira `ReduceMotion` solo en `SetActive`, y en cada cuadro evalúa `LiveDot.At(t, false)`. `PhaseChipView.Apply` llama a `SetActive(true)` en cada estado, pero no hace nada si ya estaba activo. Escenario: un libre en marcha y el usuario apaga las animaciones de Windows. Al volver el foco, `AppShellView` pone `ReduceMotion = true`; el corazón, el pulso de `Próximo` y el halo se quedan quietos en ese cuadro, pero el punto late hasta la próxima pausa (a 60 fps, como siempre con el inmersivo arriba). Al revés, si se apaga "reducir movimiento" a mitad de sesión, el punto queda quieto. AC-APP-05-08 y §3.10 dicen "el punto quieto en 1" | `Presentation/Motion/Hud/LiveDotMotion.cs:64` (`LiveDot.At(…, false)`) y `:48-52`; frente a `HeartbeatAnimator.cs:90`, `StepPulse.cs:101`, `:122`, `:232` y `PillRowMotion.cs:248`, que lo leen en cada cuadro | Que `Advance` use `LiveDot.At(t, ReduceMotion)` y, si `ReduceMotion` pasó a `true`, aplique 1 y salga del *driver*. Que `SetActive` vuelva a entrar si se apaga (o que lo haga `PhaseChipView.Apply` en cada estado). Sumarlo a `HudMotionTests.LiveDotStartsAtOneAndPulsesWhileActive` |
| QA-V4b-MOT-04 | menor | architect | **Cada vuelta del segundo plano de 30 s o más queda en el registro como un "cuadro tarde".** `FrameScheduler.ResumeFromBackground` pasa por `SuspendAccounting.Awake` con el delta del crudo de todo el segundo plano. Sin fuente de sueño (Android e iOS, `NullSleepSource`), el caso 3 anota `[TIME] cuadro de N s sin aviso de suspensión: cuenta (AC-WKT-02-04)`, aunque no hubo ningún cuadro tarde sino el segundo plano normal (Q-91). Ese registro es el que tiene que decir qué faltó cuando la demo (c) falla (§2.9.3). En Windows no pasa, porque minimizar no es segundo plano | `Core/Common/SuspendAccounting.cs:136-140`; `Core/Common/FrameScheduler.cs:124-135`. **LM-04** y **LM-04b** (el camino de `LifecycleHarness.Background` / `Foreground`, el de `AppRoot`): `[TIME] Info: cuadro de 600.0 s sin aviso de suspensión: cuenta (AC-WKT-02-04)` | Anotar el caso 3 solo desde `Tick` (un parámetro de `Awake`, o que `ResumeFromBackground` anote otra cosa: `[TIME] vuelta del segundo plano: N s, cuentan (Q-91)`). Una prueba en `SuspendAccountingTests` |
| QA-V4b-MOT-05 | menor | architect | **La prueba de "nada animando al salir" se queda corta respecto de lo que prometía §3.12.** El diseño pedía cuatro salidas y 0 avisos de DPI en el registro con el sello y el chip a la vista. `ITT.NothingAnimatesAfterTheExit` tiene tres. La "a mano" es un **libre** (`RoutineSeeds.Lista` + `StartFree`), sin `Próximo`, sin halo y sin píldoras. No tiene la vuelta del segundo plano y no mira el registro. La rutina con `Próximo` y el halo, y la vuelta del segundo plano, quedan cubiertas por `IHT.LeavingTheImmersiveStopsItsContinuousMotions`. **Los avisos de DPI de las piezas de la 4b no los prueba nadie**: la única que los mira es `IHT.DigitsLeavingThePanelStopRolling`, de la 4a, que solo mira cifras. El código cuida `panel == null` (`PillRowView.UpdateFillWidth`, `SealRow.Fit`, `PhaseChipView.Measure`, `FreeTopBar.Fit` y `StepInfoBar.MeasureUpNext`), pero no hay prueba que lo sostenga | `Tests/PlayMode/ImmersiveTransitionTests.cs:110-145`; `02-tech-design.md` §3.12 ("La prueba"); búsqueda de `DPI` en `Tests/`: un solo uso (`ImmersiveHudTests.cs:333`) | Sumar a ITT la salida "a mano" con una rutina que tenga el sello, la cuenta grande y `PAUSA` (o el chip) a la vista en el cuadro de `Terminar`, con el `Application.logMessageReceived` de `DigitsLeavingThePanelStopRolling`, y afirmar 0 avisos de DPI. El caso del segundo plano puede quedar en IHT, pero conviene decirlo en §3.12 |

---

## 4. Detalle

### 4.1 MOT-01: el conteo asigna en cada cuadro

```csharp
// Presentation/Workout/CompletionDialog.cs:204-216
private void ApplyVisual(CompletionVisual visual)
{
    Check.Set(visual.CheckScale, visual.CheckStroke);
    ShowValues(_motion.ValueOf(_completion.TimeValue, 0), _motion.ValueOf(_completion.DistanceValue, 1), _motion.ValueOf(_completion.CaloriesValue, 2));
}

private void ShowValues(long time, long distance, long calories)
{
    TimeValue.SetValue(WorkoutFormats.SessionClock(time), time, false);
    DistanceValue.SetValue(distance.ToString(CultureInfo.InvariantCulture), distance, false);
    CaloriesValue.SetValue(calories.ToString(CultureInfo.InvariantCulture), calories, false);
}
```

`CompletionMotion.Advance` llama a `apply` en cada cuadro de los 1160 ms. Los totales empiezan a contar recién a los 220, 340 y 460 ms (`CountUp.StartMs`) y, de ahí en más, muchos cuadros repiten el valor, pero se formatean las tres cadenas igual. LM-01 reproduce exactamente esas llamadas sin la vista: 94 a 160 B por cuadro. Además, cada cadena nueva hace que `TabularNumber.SetValue` rehaga las celdas y el ancho (`ComputeCells`, `ApplySize`). Es poco, una vez por sesión, pero el contrato de §7.4 es 0 B, y en el teléfono (6a) el GC incremental se nota justo en el momento "de peso" del diálogo.

### 4.2 MOT-02: la salida en negro deja tocar Workout

Línea de tiempo con `Terminar` del diálogo (el cierre ya volvió, sin reducir movimiento):

| t (ms) | Qué pasa | Lo que recibe un toque |
|---|---|---|
| 0 | `AcknowledgeCompletion` → reposo → `BeginExit`: `Cancel()` del diálogo; `Pop` → `ExitLock(page)` y `SetCovers(false)` | el velo del diálogo que sale (se lo traga, DT-21) |
| 0-150 | el diálogo sale (`DialogOutMs`) | el velo |
| 150-400 | la página sigue **opaca**: el HUD se fue (0-200) y el fondo fundió a negro (200-400) | **Workout en reposo, que no se ve** (la página es inerte y las pestañas están en `flex` debajo) |
| 400-640 | la página funde de 1 a 0 y empieza a verse Workout | Workout, que ya se empieza a ver |

Prueba PlayMode para quien tenga el Editor (no está en el repo):

```csharp
[UnityTest, Category("AC-WKT-13-08")]
public IEnumerator SecondTapDuringTheBlackExitDoesNothing()
{
    CreateWorkout(RoutineSeeds.F4A, o => o.Profile = DeviceProfile.TH); // 4 rutinas
    yield return StartRoutine("Corta");
    yield return AdvanceTo(31);
    var point = H.Find("completion-finish")!.worldBound.center;
    H.Tap("completion-finish");
    yield return Frames();
    H.Advance(0.2);                       // 200 ms: el velo del diálogo ya no está; la página sigue negra
    yield return Frames();
    var picked = H.TapAt(point);
    yield return Settle();
    Assert.That(H.Shell.Dialogs.HasOpen, Is.False, "ningún ¿Empezar …? que no se pidió (picked: " + picked?.name + ")");
    Assert.That(H.App.Workout.State.Value.Phase, Is.EqualTo(WorkoutPhase.Idle), "ninguna sesión nueva");
}
```

La misma prueba sirve para medir el arreglo: con el bloqueador, a los 200 ms se afirma que `picked` está en el árbol del inmersivo y no hace nada, y a los 450 ms que el toque llega a Workout (ROB-01 sigue valiendo).

### 4.3 MOT-03: el punto en vivo y reducir movimiento

```csharp
// Presentation/Motion/Hud/LiveDotMotion.cs:56-70
protected override bool Advance(double nowSeconds)
{
    if (!_active) { return false; }
    var opacity = LiveDot.At(HudTimelines.Ms(nowSeconds - _origin), false);   // <- nunca la variante reducida
    ...
    return true;
}
```

La regla de `HudMotion` ("reducir movimiento se lee al empezar cada fase") tiene sentido para lo finito, que dura milisegundos. La "fase" del punto, en cambio, dura toda la sesión en marcha. Las otras animaciones continuas del HUD (el corazón, el pulso de `Próximo`, el halo) obedecen en el cuadro siguiente. PlayMode sugerida, dentro de `HudMotionTests`: `dot.SetActive(true)`, 30 cuadros, `rig.Driver.Settings.ReduceMotion = true`, 1 cuadro, y afirmar `dot.Opacity == 1` y `!rig.Driver.Contains(dot)`.

### 4.4 MOT-04: el registro de la vuelta del segundo plano

`SuspendAccounting.Awake` no sabe si lo llamó un cuadro o el alcance. Con `NullSleepSource` el delta del crudo es todo el segundo plano y cae en el caso 3:

```csharp
// Core/Common/SuspendAccounting.cs:136-140
else if (dRaw >= LateFrameLogSeconds)
{
    // 3 · sin evidencia: cuenta.
    _logger.Log(LogLevel.Info, Tag, "cuadro de " + Fmt.Fixed(dRaw, 1) + " s sin aviso de suspensión: cuenta (AC-WKT-02-04)");
}
```

La conducta está bien (el segundo plano cuenta, Q-91). Lo que confunde es el texto del registro. En la 10a, cada pantalla bloqueada de una sesión en el teléfono va a dejar esa línea.

### 4.5 MOT-05: lo que falta en la prueba de la salida

§3.12 decía: "con cuatro salidas (a mano con `Próximo` y el halo latiendo; con `Terminar` del diálogo ya contado; con `Terminar` durante el conteo…; y con la vuelta del segundo plano ya en reposo), y 0 avisos de DPI en el registro con el sello y el chip a la vista al salir". La prueba hecha tiene tres salidas, la "a mano" con un libre. Lo que más importa de la lección de QA-V4a-05 son las piezas nuevas que miden texto, y de eso no queda ninguna prueba.

---

## 5. Las pruebas del lente

En `<scratch>/it4b/lens-motion/sln/Probe/`, nunca en el repo. Se corren con `dotnet test Probe/Probe.csproj --artifacts-path <scratch>/it4b/lens-motion/probe-artifacts` desde `<scratch>/it4b/lens-motion/sln`.

| Prueba | Qué hace | Resultado |
|---|---|---|
| `LM01_CountUpFormattingAllocates_Corta` / `_Maraton` | Las llamadas de `CompletionDialog.ShowValues` en cada cuadro de 60 fps durante los 1160 ms, con `GC.GetAllocatedBytesForCurrentThread` | 6592 B y 11 232 B en 70 cuadros. **Afirma el defecto** (MOT-01) |
| `LM04_BackgroundReturnIsLoggedAsALateFrame` | `FrameScheduler` con `EnterBackground` y `ResumeFromBackground(Plain(600 …))` sin medida ni avisos | Anota "cuadro de 600.0 s sin aviso de suspensión: cuenta". **Afirma el defecto** (MOT-04) |
| `LM04b_LifecycleHarnessBackgroundReturn` | Lo mismo por `LifecycleHarness.Background(5)` / `Foreground(65)` | "cuadro de 60.0 s …" (MOT-04) |
| `LM05_TheHoldSurvivesBackgroundAndSleep` | `Corta` termina sola a los 30 s y queda con el diálogo hasta los 660 s, con segundo plano de 60 a 200 s, una suspensión a los 250 s (600 s, el crudo cuenta, con medida) y otro segundo plano de 300 a 640 s; después `AcknowledgeCompletion` | En `Finished`, `Saved`, reloj 30 s, 7 puntos (los mismos que al terminar), 0 avisos; después `Idle`, `Ended(Completed, true)` y `FinishedAt` = inicio + 30 s. **PASA** |
| `LM06_SuspendedRoutine_RawCounts` / `_RawStops` | `Corta` suspendida 600 s a los 12,4 s, con medida y aviso, y que el crudo cuente o no | Al despertar, reloj 12 s; termina a los 30 s en marcha con 7 puntos y 00:30 en el diálogo. **PASA** |

---

## 6. Observaciones (sin issue)

- **O-1, los resortes finitos de la 4a siguen hasta llegar.** `StepInfoBar.Dispose` no para `_upNextWidth` (`SpringMotion`) ni `_swap` (`StepSwap`). Si se termina a mano en los primeros 300 ms de `Próximo` y el cierre vuelve rápido, el resorte del ancho puede seguir unos cientos de ms después de `OnPopped`. Solo escribe `style.width` sobre un elemento fuera del panel: no mide, no da avisos de DPI y sale solo. Las pruebas usan `Settle`, así que no lo ven. Se podría sumar `_upNextWidth.Stop()` y `_swap.Finish()` a `StopMotions` por prolijidad.
- **O-2, 60 fps con el diálogo abierto.** La página pide 60 fps toda su vida (§3.12, como en la 4a), y en la 4b el inmersivo puede quedar arriba sin límite: el diálogo espera a `Terminar`. Es lo mismo que una pausa larga en la 4a (60 contra 30 fps, con la pantalla siempre encendida por AC-APP-14). Para la 6a, en el teléfono, conviene que el PO decida si con el diálogo arriba y nada en movimiento se puede bajar a 30.
- **O-3, `WorkoutScreenView._immersive` retiene la última página hasta la sesión siguiente.** La página ya está dispuesta (sin animaciones, sin *store* y con las texturas destruidas). Viene de la 4a y es solo memoria acotada a una página, no ROB-02.
- **O-4, el `MotionDriver` no aísla las excepciones por animación** (`MotionDriver.cs:178-207`: `try`/`finally` sin `catch`). Si un *callback* de la 4b lanzara, se cortaría el resto del cuadro, `PageTransition` incluida, y la salida podría quedar trabada en negro. No se encontró ningún *callback* que lance; queda como endurecimiento posible.
- **O-5, el motor depende del diálogo para salir de `Finished`.** Sin el `Terminar` del diálogo, la rutina terminada sola no vuelve nunca a reposo. Es lo mismo que en el legacy (`immersive_workout_screen.dart:261-289`: solo el diálogo llama a `reset()`), y todos los caminos del código abren el diálogo una vez por sesión. Pero si `OpenCompletion` fallara (una excepción al armar el diálogo), `_completionSession` ya está marcada (`ImmersiveWorkoutPage.cs:263`) y no se reintenta: la app quedaría `Busy` hasta reiniciarse.

---

## 7. NO VERIFICABLE en este lente (y por qué)

| Qué | Por qué | Quién |
|---|---|---|
| Las PlayMode y Unity EditMode citadas (`HudMotionTests`, ITT, IHT, `CompletionDialogTests` y las de 0 B) | Esta lente no abre Unity; el Architect las reporta en verde en "Cambios por implementación de WP-B" | QA principal |
| MOT-02 en pantalla | Necesita PlayMode (la prueba de §4.2) | QA principal |
| AC-WKT-13-09 [Device-Win]: el registro del build después de tres entrenamientos | Build de simulador | QA principal |
| P-17 con los avisos reales de Windows (`PowerRegisterSuspendResumeNotification`, la medida `GetTickCount64 − QueryUnbiasedInterruptTime`, el modo de espera moderno) | Hay que suspender el equipo con el build: demo (c), `WindowsSuspendDoesNotCount` ignorada en dotnet | Usuario |
| Android (barras, segundo plano, sueño profundo RA-19) | Sin módulo ni teléfono | 10a |
