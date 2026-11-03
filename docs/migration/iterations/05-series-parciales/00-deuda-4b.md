# It. 5 — Deuda de la It. 4b, saldada al empezar

| Campo | Valor |
|---|---|
| Autor | unity-architect |
| Fecha | 2026-10-30 |
| Iteración | 5 · `05-series-parciales` ([ROADMAP §It. 5](../../ROADMAP.md)). Primer trabajo de la iteración, antes de construir encima |
| Entrada | [REPORT de la 4b](../04b-hud-exr/REPORT.md) §7 y §9.2; [05-qa-verification de la 4b](../04b-hud-exr/05-qa-verification.md), ronda 2 (R2.8); [lente de motion](../04b-hud-exr/05-qa-lens-motion.md) §6 (O-1, O-4, O-5); [lente de paridad](../04b-hud-exr/05-qa-lens-paridad.md) (O-P01, O-P02); [02-tech-design de la 4b](../04b-hud-exr/02-tech-design.md) §2.9 y V1-06; la [spec de la 5](01-po-spec.md) §5.1 (AC-WKT-13-15, -13-16 y -16-07) |
| Qué se salda | P-17 / AC-WKT-02-11 (la demo (c) que no fue concluyente: revisión de punta a punta y el registro de cada despertar); QA-V4b-06 (el primer cuadro del inmersivo); QA-V4b-R2-03 (Unity que revienta al cerrar en las herramientas); O-4, O-5 y O-1 de la lente de motion; O-P01 y O-P02 de la lente de paridad; O-R2-01 de la 4b; y QA-V4b-R2-01 con el valor del Designer ([00-deuda-diseno](00-deuda-diseno.md) §1, DD5-01 y DD5-02) |
| Legacy | [rowmate-legacy](https://github.com/figuibej/rowmate-legacy), solo lectura: no hizo falta leerlo. `git -C ../rowmate-legacy status --porcelain` vacío al empezar y al terminar. No se leyó el módulo de credenciales de Strava |
| Repo | Sin commits. Salidas, registros y sondas en `<scratch>/it5/` |

---

## 0. Resumen

| ID | Qué pasaba | Qué se hizo | Estado |
|---|---|---|---|
| **P-17 / AC-WKT-02-11** | La demo (c) dejó un registro sin `durmió` ni cuadro largo | El registro de eventos del sistema **no muestra suspensiones en la ventana de la demo**. La revisión de punta a punta no encontró un camino por el que un sueño se pierda. Se suma una línea `[TIME]` en **cada** despertar, también en reposo, con lo que vio cada fuente | **Sin bug**; demo para repetir al cierre de la 5 (§1.5) |
| **QA-V4b-06** (AC-WKT-13-15) | El primer cuadro de cada inmersivo, 31-69 ms (armaba el HUD en el cuadro del toque, con la primera vez de todo) | (a) `HudWarmup`: la primera vez del HUD se paga en el arranque; (b) el HUD se arma en el primer cuadro **negro y quieto** (cuando el fondo deja el 0), no en el del toque | Editor: el primer inmersivo, **13,7 ms** en el toque y **10,1 ms** en el negro (eran 54 ms en el toque). En el build, **NO VERIFICABLE** (la app de la demo del usuario siguió abierta en su carpeta: §2.4) |
| **QA-V4b-R2-03** | Unity revienta (0xC0000005) al cerrar un `-batchmode -quit` que cambió el *define* y la corrida quedaba fallida | Los dos *scripts* aceptan ese código si el *define* quedó como se pidió y, si no, repiten el paso una vez | **Cerrado**; pasó de verdad cinco veces en estas corridas y las cinco siguió solo (§3) |
| **O-4** (AC-WKT-13-16) | Una animación que lanzaba cortaba el cuadro de todas | `MotionDriver` aísla cada una: la que lanza sale y queda en el registro | **Cerrado** |
| **O-5** (AC-WKT-13-16) | Si el diálogo de completado no se armaba, la sesión quedaba en `Finished` para siempre | Se anota y se hace lo que haría su `Terminar`: reposo, guardada, la página sale | **Cerrado** |
| **O-1** | El ancho de `Próximo` y el cambio de paso seguían después de salir | La barra de pasos los termina al irse | **Cerrado** |
| O-P01, O-P02 | Dos comentarios viejos y la espera del diálogo sin mirar | Comentarios al día; la espera se mira segundo a segundo | **Cerrado** |
| O-R2-01 (4b) | `TestResults.xml` en la carpeta de datos del Editor | `run-unity-tests.ps1 -Platform PlayMode` lo borra si lo escribió la corrida | **Cerrado** |
| QA-V4b-R2-01 (AC-WKT-16-07) | El título libre pegado al reloj (TV y P360, español) | El valor del Designer (el modo compacto de la barra libre, DD5-01) aplicado, con CAP-13 a CAP-15 regeneradas (DD5-02) | **Cerrado**; título entero a ≈ 0,805, todo dentro de ± 1 px de lo que pidió el Designer (§5) |

| Corrida | Resultado |
|---|---|
| dotnet (`run-tests.ps1`) | 0 avisos, 0 errores · herramientas **182 / 182** · Core **2332: 2305 pasan, 0 fallan, 27 ignoradas** · matriz con Unity: **PASA 597, FALLA 0, IGNORADO 2** |
| Unity EditMode | **2388: 2357 pasan, 0 fallan, 31 ignoradas** |
| Unity EditMode con simulador | **2570: 2539 pasan, 0 fallan, 31 ignoradas, 0 inconclusas**; el *define* restaurado |
| Unity PlayMode | **363 / 363** |
| Unity PlayMode con simulador | **429 / 429**; el *define* restaurado |
| Arnés `it4b` (A y B) | `51 tomas, 0 con problemas, 0 sondas de aspecto que fallan` en las dos; **A = B** byte a byte (salvo `durationMs`). Cambian CAP-13 a -15 y los videos de la barra libre en TV (el modo compacto), más CAP-18 y VID-03 por debajo de lo visible (§6) |
| `it2` / `it3` / `it4a` | **Iguales byte a byte a lo versionado** (en `manifest.json`, solo `durationMs`) |
| Build de Windows con simulador | **NO VERIFICABLE**: el `RowMate.exe` de la demo del usuario siguió abierto en `Builds/Windows/simulator` toda la tarea; se esperó de a 2 minutos durante 20 y no se tocó (§2.4). La carpeta tiene el build de la 4b |

---

## 1. P-17 / AC-WKT-02-11: la demo (c) no fue concluyente

### 1.1 Lo que dice la evidencia

El usuario hizo la demo con el build de simulador de `Builds/Windows/simulator`. Lo que quedó:

| Fuente | Qué hay |
|---|---|
| `Player.log` del build | `[TIME] avisos de suspensión registrados (P-17)`: el registro de los avisos funcionó. Ninguna línea `durmió`, ninguna `cuadro de … s sin aviso de suspensión`. Una sola ventana `[PERF]` (10 s de un inmersivo) y, más tarde, cuatro líneas de Direct3D `GetFrameStatistics is broken`, las que deja un cambio de energía de la pantalla (apagarse y prenderse) |
| La base de desarrollo | Una sola sesión corta en la ventana de la demo |
| Los registros del sistema | **Ninguna suspensión en la ventana de la demo**. Lo que hay es compatible con la pantalla bloqueada o apagada un rato, no con una suspensión |
| El proceso | El `RowMate.exe` de la demo seguía abierto mientras se hizo este trabajo |

**Conclusión: no hubo nada que detectar.** Con la app abierta el equipo no se duerme solo: la app pide `ES_SYSTEM_REQUIRED | ES_DISPLAY_REQUIRED` desde el arranque (AC-APP-14-01, legacy `main.dart:42`), así que la única forma de suspenderlo es pedirlo (Inicio → Inicio/apagado → Suspender). Bloquear la sesión o apagar el monitor no suspende.

Además se comprobó en el equipo de desarrollo que **la medida funciona**: `GetTickCount64 − QueryUnbiasedInterruptTime` da un sueño acumulado desde el arranque coherente con las suspensiones anteriores, y el `Stopwatch` (QPC) avanza igual que `GetTickCount64`: en ese equipo el crudo **cuenta** el sueño, así que un S3 con la app abierta cae en el caso 1 de `SuspendAccounting` (con medida y crudo que contó: se descuenta). La lectura se hizo con un P/Invoke de PowerShell, sin tocar la app.

### 1.2 La revisión de punta a punta

| Punto | Qué se miró | Resultado |
|---|---|---|
| El registro del aviso | `WindowsSleepSource.Register`: `PowerRegisterSuspendResumeNotification(DEVICE_NOTIFY_CALLBACK)` con la estructura en `AllocHGlobal`, el delegado en un campo estático y `[AOT.MonoPInvokeCallback]` | Bien: el build escribió `avisos de suspensión registrados`, que solo sale con código 0 y un *handle* |
| El hilo del *callback* | Corre en un hilo del sistema; solo `Interlocked` | Bien. Ahora también toma la hora con `GetTickCount64` (un P/Invoke desde ese hilo), con `try`/`catch` alrededor: una excepción ahí volvería a código nativo |
| La ventana / *handle* | El aviso por *callback* no usa la ventana de Unity ni `WM_POWERBROADCAST` | No depende del foco ni de la ventana minimizada; con `runInBackground` el *loop* sigue sin foco |
| El reposo sin sesión | `WorkoutEngine` vive toda la app y `OnWoke` escribía `[WORKOUT] el equipo durmió …` **antes** de mirar si está en reposo; `SuspendAccounting` corre en cada cuadro, haya sesión o no | Bien: un sueño en reposo también dejaba la línea. Ahora además la escribe el reloj despierto (§1.3) |
| La medida | Acumulada desde el arranque: aunque se pierdan cuadros, el primero que corre ve todo lo dormido | Bien (y comprobada en el equipo, §1.1) |
| El crudo | El `Stopwatch` puede contar el sueño o no (QA-V4a-ROB-05) | En este equipo lo cuenta. Las dos ramas siguen probadas |
| El Editor | El aviso no se registra (DT4b-20); la medida sí | Sin cambios |

**No se encontró un bug.** Lo que faltaba era poder confirmar la demo con el registro: qué fuente vio el sueño, cuánto dijo cada una y si el aviso llegó. Hay dos casos que antes no dejaban rastro y ahora sí: un sueño que el núcleo no cuenta y que el crudo tampoco (el reloj de la app queda bien, pero el registro no decía nada), y la duración según los avisos.

### 1.3 Lo que se suma: el registro de cada fuente

En `Core/Common/SuspendAccounting.cs` (puro: lo prueban dotnet y Unity), solo con un evento (nada por cuadro):

```
[TIME] medida del sueño del núcleo disponible (GetTickCount64 − QueryUnbiasedInterruptTime)
[TIME] aviso del sistema: se suspende (1)
[TIME] despertar: durmió 60.0 s según la medida del núcleo y 60.4 s según el aviso del sistema; el reloj crudo avanzó 60.0 s y se descuentan 60.0 s (aviso, medida)
[TIME] aviso del sistema: reanuda (1): durmió 60.4 s según el aviso
[WORKOUT] el equipo durmió 60.0 s: no cuenta (P-17; aviso, medida)
```

- Si falta una fuente, la línea lo dice: `sin medida del núcleo`, `sin aviso del sistema` o `el aviso de reanudación todavía no llegó` (el "reanuda" puede llegar después del primer cuadro despierto; entonces su propia línea trae la duración).
- La duración según el aviso es la del último ciclo completo, de "se suspende" a "reanuda", con la hora que el *callback* toma con `GetTickCount64` (`ISystemSleepSource.SignalAsleepSeconds`, `FrameClock.SignalAsleepSeconds`). Solo informa: el reloj despierto no la usa.
- Un salto de la hora de pared de 30 s o más sin aviso ni medida queda como `[TIME] la hora de pared se movió N s respecto del reloj crudo, sin aviso ni medida de sueño: no se descuenta (AC-WKT-19-04)`: es un cambio de hora, o un sueño que ninguna señal vio.
- La conducta no cambia: las 13 pruebas de `WorkoutSuspendTests` y las de SUS de la 4b pasan igual. El ADR 020 tiene un addendum.

### 1.4 Pruebas nuevas

| Dónde | Prueba | Qué fija |
|---|---|---|
| dotnet y Unity (`SuspendAccountingTests`) | `EveryWakeIsLoggedWithBothSources`, `AWakeWithOnlyTheMeasureSaysTheSignalDidNotCome`, `AWakeWithOnlyTheSignalSaysThereIsNoMeasure`, `AResumeAfterTheWakeBringsItsDurationOnItsOwnLine`, `ManyWakesEachGetTheirLine`, `AWallJumpWithoutEvidenceIsLoggedButNotDiscounted`, `NothingIsLoggedWithoutEvents`, `AWakeAtRestIsLoggedThroughTheHarness` | Las líneas exactas, en reposo (sin motor), con una fuente o las dos, y nada sin eventos |
| Unity EditMode (`UnitySchedulerTests`) | `EveryWakeIsLoggedAtRestWithBothSources`; `WindowsSourceThroughTheSchedulerLogsTheWake` (la medida del núcleo de verdad, con P/Invoke, y la hora del aviso por `UnityScheduler`) | La cadena de la app sin motor ni sesión |
| Unity EditMode (`SystemSleepSourceTests`) | `SignalAsleepIsTheLastCompleteCycle`, `TheCallbackTakesTheSystemTick` (el *callback* real con `GetTickCount64`), `TheLogSaysWhetherTheMeasureExists` | La duración del ciclo de avisos y el registro de la medida |

### 1.5 Para la demo (c) al cierre de la 5 (para el PO, que la reescribe en el REPORT)

1. Abrir el build de simulador y `Entrenamiento libre`; dejar correr hasta `01:00`.
2. **Suspender de verdad**: Inicio → Inicio/apagado → **Suspender**. No sirve bloquear la sesión (Win+L), apagar el monitor ni esperar a que se duerma solo: la app no lo deja dormirse solo (AC-APP-14).
3. Esperar un minuto y despertar. El reloj sigue desde `01:00` (a lo sumo `01:01`).
4. Antes de volver a abrir la app, buscar `durmió` en `Player.log`: tiene que estar `[TIME] despertar: durmió …` con las dos fuentes y `[WORKOUT] el equipo durmió …`. Si en cambio dice `sin aviso del sistema` o `sin medida del núcleo`, esa es la fuente que faltó (R4b-01); si no hay ninguna línea `despertar`, el equipo no se suspendió.
5. Los puntos de la sesión (unos 25 para 2 minutos remados), como en la 4b.

---

## 2. QA-V4b-06 (AC-WKT-13-15): el primer cuadro del inmersivo

### 2.1 Dónde se iba el tiempo

Medido en el Editor con una prueba PlayMode temporal (borrada con su `.meta`), en un proceso nuevo, con `Stopwatch` alrededor del toque (lo de C#) y del cuadro siguiente (el panel: estilos, *layout*, texto y dibujo):

| Entrada | Toque (C#) | Cuadro siguiente | Total |
|---|---|---|---|
| Primer libre del proceso | 35,1 ms | 18,9 ms | **54,0 ms** |
| Primera rutina (`Prueba 4a`) | 11,9 ms | 8,3 ms | 20,2 ms |
| Las siguientes | 5,0-5,1 ms | 3,3-3,6 ms | 8,3-8,7 ms |

Lo que cuesta de más el primero es lo que se paga **una vez por proceso**: el JIT de Mono del código del HUD (y de la página y el motor), y la primera vez de sus estilos y de su texto. Coincide con el build: la primera ventana `[PERF]` de cada arranque daba 31-69 ms (QA) y 46,6 / 50,4 ms (los registros de la demo del usuario).

### 2.2 Qué se hizo

**(a) `HudWarmup`** (`Presentation/Workout/HudWarmup.cs`, nuevo). En el cuadro del arranque (`AppRoot.Start`, después de `_app.Start()`) arma un HUD de rutina y uno libre como los del inmersivo, con el estado de reposo del store y uno "en marcha" de ejemplo (`HudWarmup.Running`: valores, objetivos, pastilla, píldoras, `Próximo`, chip), sin animar; detrás de toda la app (primer hijo de la raíz), transparentes y sin eventos. Los deja un cuadro en el panel, les aplica `HudZones` y en el cuadro siguiente los desarma (`AppRoot.Update`). No crea sesión ni toca el motor, las barras del sistema, la llave, el obstáculo ni los 60 fps. Si algo falla, lo anota (`[UI] no se pudo precalentar el HUD`) y se desarma: nunca frena la app. Lo usa solo `AppRoot` (las pruebas de UI y el arnés no lo corren, así sus atlas y sus bytes no cambian).

**(b) El HUD se arma en el negro** (`ImmersiveWorkoutPage`), lo que proponía V1-06 de la 4b. En el cuadro del toque la página nace igual (negra, con el store, la cascada, las barras del sistema, la llave, la superficie, "Atrás" y el foco), pero **sin** `HudView` ni `HudZones`. El HUD se arma en el primer cuadro en que el fondo de la cascada deja el 0: el primero después de los 220 ms, con la página entera, negra y quieta, que es lo único que AC-WKT-13-15 admite para un cuadro largo. Nace con las opacidades y subidas que ya tiene la cascada (todo en 0: nada aparece de golpe), con el estado del store (sin animar, es su primer estado) y con los controles como estén. Con reducir movimiento (el fondo está en 1 desde el cuadro 0) o sin animar, la regla es la misma y se arma en el acto. Si la sesión vuelve a reposo antes, no se arma: la salida funde la página negra.

Lo que **no** cambia: la sesión y el reloj corren desde el toque (AC-WKT-13-01); los tiempos de la entrada (AC-WKT-13-06) salen de la misma línea de tiempo pura; "Atrás" responde desde el cuadro 0 (AC-WKT-13-07); las barras del sistema se piden en el cuadro del toque (AC-APP-15-04); los toques en el negro de la salida (QA-V4b-MOT-02).

**Las fases de lo continuo, como en la 4b.** La primera regeneración del arnés mostró que armar el HUD 230 ms después corría la fase de lo que late desde el primer estado: el punto en vivo del chip (las sondas de CAP-13, CAP-17 y CAP-18 esperan 0,444 a los 42,2 s con el origen en el toque) y el pulso de `Próximo` en las rutinas de pasos cortos (CAP-03, -04, -26 y -30, unos pocos niveles de gris). Se resolvió sin tocar cada animación: `MotionDriver.StartingAt(hora)` es un alcance (un `struct`, sin asignar) dentro del cual `Now` devuelve esa hora, y `BuildView` arma el HUD dentro de `StartingAt(la hora del toque)`. Lo que arranca con el primer estado toma el toque como comienzo, igual que cuando el HUD se armaba ahí; `Tick` sigue usando el reloj. Con eso, todo lo que no es la barra libre en TV y P360 en español queda igual byte a byte (§6).

### 2.3 Medidas en el Editor (la misma prueba temporal, con el precalentado y el armado en el negro; `StartingAt` y el modo compacto de la barra libre vinieron después y no suman trabajo apreciable)

| Entrada | Sin nada (la 4b) | Solo el HUD en el negro | **Precalentado + HUD en el negro** |
|---|---|---|---|
| Primer libre: toque | 54,0 ms | 21,2 ms | **13,7 ms** |
| Primer libre: cuadro negro (el armado) | — | 35,5 ms | **10,1 ms** |
| Primera rutina: toque | 20,2 ms | 6,9 ms | **5,7 ms** |
| Primera rutina: cuadro negro | — | 24,0 ms | **7,0 ms** |
| Las siguientes: toque / negro | 8,3-8,7 ms / — | 2,5-2,9 ms / 6,5-8,0 ms | **2,4-2,6 ms / 6,6 ms** |

El precalentado cuesta en el arranque, en el Editor, unos 37 ms en el primer cuadro (que es el del arranque y no se ve) más 28 ms del panel y 11 ms en el segundo cuadro (la geometría y el desarme).

### 2.4 Medida en el build (`FrameTimeProbe`)

**NO VERIFICABLE en esta tarea.** El `RowMate.exe` de la demo del usuario estuvo abierto en `Builds/Windows/simulator` desde antes de empezar hasta el final (se esperó de a 2 minutos durante 20, sin cerrarlo), y los builds de desarrollo van solo a esa carpeta. La carpeta sigue con el build de la 4b. Lo que queda para QA (o para el Architect en WP-B), cuando el usuario cierre la app:

1. `powershell -NoProfile -File tools/build/build.ps1 -Target Windows -Simulator`.
2. Con `ROWMATE_DATA_DIR` en una carpeta temporal (nunca la base del usuario) y `-logFile` en esa misma carpeta (así no se toca el `Player.log` del usuario): `Builds\Windows\simulator\RowMate.exe -logFile <carpeta>\perf.log -screen-fullscreen 0 -screen-width 1280 -screen-height 720 -rowmate-perf-entries 4`, y otra vez con `-rowmate-no-hud-warmup` para comparar en el mismo build. Cada corrida dura poco más de un minuto y sale sola con 0. Antes, exportar `HKCU\Software\iguisoft\RowMate` (las preferencias de la ventana del *player*) y restaurarlas después.
3. En el registro: `[PERF] entrada del inmersivo: máx X ms en los primeros 30 cuadros (n.º k)` y la primera ventana `[PERF]` de cada entrada. Objetivo (AC-WKT-13-15): ≤ 16,7 ms en todo cuadro que se mueve. El cuadro del armado, si pasara, es negro y quieto, que es lo que el AC admite.

Lo que se espera, por el Editor (§2.3): el toque del primer libre baja de ≈ 54 a ≈ 14 ms y el armado queda en ≈ 10 ms en un cuadro negro; los siguientes, ≈ 2,5 y ≈ 7 ms. En la 4b el build y el Editor dieron números parecidos para el primer inmersivo (46,6 / 50,4 ms en los registros de la demo y 54 ms en el Editor).

### 2.5 Pruebas

| Prueba | Qué fija |
|---|---|
| `ITT.TheHudIsBuiltInTheBlack` (nueva) | Sin HUD en el cuadro del toque ni a los 200 ms; con HUD en el primer cuadro con el fondo fuera del 0 (a los 230), la página entera, las seis piezas en 0 y los controles sin responder; al final, los valores del motor y el punto en vivo con la fase contada desde el toque |
| `WorkoutPersistenceTests.FreeStartCreatesTheSessionAndTheClockRuns` (cambia) | En el cuadro del toque mira el reloj del store (el HUD todavía no está); a los 250 ms, el de la banda |
| `ITT.WithReducedMotionTheHudIsThereAtOnce` (nueva) | Con reducir movimiento, el HUD y los controles desde el cuadro 0 |
| `ITT.EntryCascadeStates` (cambia) | A los 100 ms ya no mira el fondo del HUD (todavía no existe) sino el de la cascada; a los 500, el HUD |
| `4BD.HudWarmupIsNotSeenAndLeavesNothing` (nueva) | El precalentado va detrás de todo; la RT es igual píxel a píxel con él y sin él; al terminar, nada en el árbol, el `MotionDriver` y las demandas de 60 fps como antes, el motor en reposo, ninguna sesión, ningún aviso |
| `4BD.TheFirstImmersiveAfterTheWarmupOpensTheSame` (nueva) | Después del precalentado, el libre se abre en el acto con el chip y los valores de verdad |
| `BootSceneTests.TheBootWarmsUpTheHudAndLeavesNothing` (nueva) | El arranque real lo precalienta y lo desarma, sin avisos |
| `I5D.PerfArgumentsOnlyInDevelopmentBuilds`, `I5D.AppRootWarmsUpTheHudAfterStartingTheApp` (nuevas, dotnet) | Los argumentos de la medida solo en desarrollo y en el inspector del build normal; la medida solo con la carpeta de datos de desarrollo; el orden en `AppRoot` |

`ImmersiveWorkoutPage.View` lanza una excepción con un mensaje claro si se pide antes de que el HUD exista; `HasView` dice si ya está. El arnés lo respeta (`WrenchAudit.HudOverlap` y la regla de toma no miran el HUD antes de que exista): ninguna toma del catálogo cae en esos 220 ms.

**Para medir en el build** (desarrollo, con simulador): `RowMate.exe -rowmate-perf-entries 4` con `ROWMATE_DATA_DIR` en una carpeta temporal hace cuatro entradas (libre y rutina alternados, 12 s cada una, terminadas como con `Terminar`) y sale; `FrameTimeProbe` escribe además `[PERF] entrada del inmersivo: máx X ms en los primeros 30 cuadros (n.º k)`. `-rowmate-no-hud-warmup` apaga el precalentado para comparar en el mismo build. Los dos argumentos existen solo en el Editor y en desarrollo y el inspector del build normal los busca.

### 2.6 Desvíos propuestos (técnicos; no cambian el QUÉ)

| ID | Qué | Por qué |
|---|---|---|
| DT5-D1 | En el cuadro del toque el inmersivo existe, pero su HUD no: se arma en el primer cuadro negro (≈ 230 ms) | Es la propuesta V1-06 de la 4b y lo que admite AC-WKT-13-15. El HUD no se ve antes (el fondo y las piezas están en 0 hasta los 220 y los 400); las pruebas que lo pedían en esos cuadros se ajustaron |
| DT5-D2 | El arranque hace un poco más (el precalentado: ≈ 65 ms más en los dos primeros cuadros, medido en el Editor) | Es el único momento en que nada se mueve todavía; el precalentado le saca al primer inmersivo la primera vez del JIT, los estilos y el texto. Con IL2CPP (Android, iOS) no hay JIT, pero la primera vez de los estilos y el texto sigue |

---

## 3. QA-V4b-R2-03: Unity que revienta al cerrar en las herramientas

`tools/unity/run-unity-tests.ps1` y `tools/unity/capture-snapshots.ps1` (`Invoke-Method`, que solo usan los dos pasos del *define*):

1. Si Unity sale con `-1073741819` (0xC0000005) y el *define* quedó **como se pidió**, el paso cuenta como bien y lo dicen. Al activar: `ROWMATE_SIMULATOR` en `ProjectSettings/ProjectSettings.asset` y la marca `Library/RowMate/simulator-tests.txt`. Al restaurar: sin la marca y con el *define* como antes de la corrida.
2. Si no quedó como se pidió, el paso se repite **una sola vez** (registro con `-retry`).
3. Otro código distinto de 0 sigue siendo un error. La restauración sigue en `finally`, verificada (código 11), y `-RestoreOnly` sigue igual.

`tools/unity/README.md` lo explica (sección de la It. 5). `I5D.DefineStepsSurviveTheCrashOnExit` fija el texto de los dos *scripts*, que siguen siendo solo ASCII.

**Pasó de verdad en estas corridas, cinco veces** (en unos 40 cambios de *define* entre pruebas y capturas), y las cinco siguió solo:

- tres veces el Unity de `EnableForTests` salió con `-1073741819`: el *script* vio el *define* y la marca y corrió las pruebas;
- dos veces el de `RestoreAfterTests` (EditMode y PlayMode con simulador) salió con `-1073741819` después de `[SIMULATOR] defines restaurados`: el *script* vio la marca borrada y el *define* como antes, y la corrida quedó en 0.

En los cinco casos la consola dice `Unity revento al cerrar despues de cambiar el define, pero quedo como se pidio: se sigue (QA-V4b-R2-03)`. Antes, cada uno era una corrida fallida (o un arnés que no corría) que había que repetir a mano.

**Un incidente de esta tarea, ya resuelto.** En una corrida anterior con simulador, el Architect editó fuentes mientras Unity corría las pruebas: el Unity de `RestoreAfterTests` arrancó con errores de compilación, salió con 1 (no con el código del cierre) y el *define* quedó puesto. El *script* lo detectó y avisó (código 11, "Corre de nuevo con -RestoreOnly"), como debe. Se restauró con `-RestoreOnly` en cuanto el proyecto volvió a compilar: `ProjectSettings.asset` sin cambios y sin la marca. La lección queda para el equipo: **no tocar fuentes mientras corre Unity**.

---

## 4. Endurecimiento (lente de motion O-4, O-5 y O-1; lente de paridad O-P01 y O-P02; O-R2-01)

| ID | Qué | Prueba |
|---|---|---|
| **O-4** (AC-WKT-13-16) | `MotionDriver.Tick` avanza cada animación con su propio `try`/`catch`: la que lanza sale del *driver* (y suelta sus 60 fps), queda como error en el registro (`[UI] una animación lanzó … y sale del cuadro (…)`) y las demás del mismo cuadro siguen, la transición de la página incluida. `ShellHost` le pasa el registro de la app; `Faulted` cuenta las que salieron | Unity EditMode `MDI.AThrowingMotionLeavesAndTheOthersKeepGoing`, `MDI.WithoutLoggerItStillIsolates` |
| **O-5** (AC-WKT-13-16) | `ImmersiveWorkoutPage.OpenCompletion`: si el diálogo no se puede armar o abrir, se anota (`no se pudo abrir el diálogo de completado: vuelve a reposo sin él (O-5)`) y se hace lo que haría su `Terminar` (`AcknowledgeCompletionAsync`): el motor vuelve a reposo con la sesión guardada y la página sale como siempre. `workoutSaveError` sale solo si el guardado falló (es la regla de siempre del motor) | PlayMode `4BD.ACompletionDialogThatCannotOpenStillReturnsToRest` (con un armado que lanza) |
| **O-1** | `StepInfoBar` termina, al irse, el cambio de paso y la entrada o salida de `Próximo` (en su estado final: los textos de la última foto) y para el resorte del ancho | PlayMode `4BD.TheStepBarLeavesWithoutItsFiniteMotions`: con el resorte corriendo, al disponer no queda ningún finito |
| O-P01 | Los dos comentarios de `WorkoutGoldenTests` que decían que el motor "vuelve solo a reposo" | Al día con la 4b |
| O-P02 | `CompletedHoldReset` mira la espera del diálogo segundo a segundo (de los 30 a los 34 s): el reloj en 30 y los objetivos `0:met,2:met` | dotnet y Unity |
| O-R2-01 (4b) | `run-unity-tests.ps1 -Platform PlayMode` borra el `TestResults.xml` que el Test Framework deja en la carpeta de datos del Editor, solo si lo escribió esa corrida (mira la fecha) | `I5D.PlayModeRunsRemoveOnlyTheirOwnEditorTestResults`; en las corridas: "se borro el TestResults.xml …" |

O-R2-01 de la 4a (`FlushAndWait` devuelve `true` aunque no encole el cierre) queda para cuando se use, como decía el STATUS.

---

## 5. QA-V4b-R2-01 (Designer): el título libre y el reloj

El valor del Designer estaba antes de la regeneración final de las capturas: [00-deuda-diseno](00-deuda-diseno.md) §1.3, el **modo compacto** de la barra libre, pedido como DD5-01 y DD5-02 (§7). Se aplicó:

| Pieza | Qué |
|---|---|
| `Core/Design/InfoRowFit.cs` | `FreeCompactTitleClockGap` 10, `FreeCompactClockIconGap` 3, `FreeCompactChipGap` 8, `FreeCompactChipPadH` 6 y `FreeCompact(available, title, othersNormal)`: compacto si y solo si, con las medidas normales, el título no entra a 1 (se decide con las medidas normales, así no oscila) |
| `Core/Workout/PhaseChip.cs` | `FixedWidthBase(…, padH)`: con relleno 6, 4 base menos (97,8 y 70,1 con los anchos de las notas; 97,3 medido en Unity) |
| `Presentation/Workout/FreeTopBar.cs` | Decide el modo en `Fit` y se lo pasa al reloj y al chip; `IsCompact` y `TitleClockGapNowDp`. Lo demás ocupa lo mismo (210,8 base), así que el título queda en la misma escala, entero |
| `Presentation/Workout/WallClockView.cs` | `SetCompact`: el ícono a 3 de sus cifras (solo lo pide la barra libre; el reloj de la barra de pasos sigue a 5, AC-WKT-19-01) |
| `Presentation/Workout/PhaseChipView.cs` | `SetCompact`: relleno 6 y 8 del reloj; `NormalWidth` y `CompactWidth` medidos con el panel |
| `Snapshots/HudExrCatalog.cs` | Las comprobaciones de la barra libre con los dos modos (el chip de las notas, 4 base menos en el compacto; el aire con el reloj, 10 en el compacto) |

**El título no baja de 0,8**: queda en ≈ 0,805, entero. En inglés y en horizontal, nada cambia (`FBT.EnglishAndLandscapeKeepTheNormalBar`).

Pruebas: `InfoRowFitTests.FreeCompactModeGivesTheTitleTheAir` y `PhaseChipTests.FixedWidth` (dotnet y Unity); `FBT.CompactBarSeparatesTheTitleFromTheClock` (AC-WKT-16-07: el aire, más que el espacio entre las palabras; el ícono a 3; el chip a 8; el título entero a ≈ 0,805), `FBT.ContentInOrderWithClockAndChip` (el chip compacto, 97,3 ± 1) y `FBT.EnglishAndLandscapeKeepTheNormalBar`.

Una precisión para QA: la caja del título que arma UI Toolkit es ≈ 0,5 base más ancha que lo que mide `MeasureTextSize` (el redondeo del texto), así que de caja a caja el reloj queda a 10·s − 0,5 dp (10,05 en TV); de tinta a tinta, que es lo que se ve, el Designer da 12,62 dp. Las comprobaciones de caja usan 1 dp de margen en el compacto.

**CAP-13 a CAP-15 regeneradas** en dos corridas iguales (A y B, §6), con las sondas de la barra libre en verde. Medidas de tinta a tinta contra la tabla del Designer (00-deuda-diseno §1.4, ± 1 px): todas dentro (§6). CAP-16 (TH), CAP-17 (W1280), DEMO-02 y DEMO-03 no cambian; CAP-18 (inglés) cambia 20 píxeles en un nivel (§6).

Lo que el Designer pide al PO (las precisiones de AC-WKT-16-02, -16-03, -16-07 y -19-01 con el modo compacto, 00-deuda-diseno §1.5 y §8) queda para el PO.

---

## 6. Corridas

Desde la raíz del repo, una instancia de Unity por vez (solo estaba el `unity.exe serve` de la Hub, que no se tocó). Todo con el código final salvo donde se dice.

| # | Comando | Resultado |
|---|---|---|
| 1 | `powershell -NoProfile -File tools/dotnet/run-tests.ps1 -ArtifactsPath <scratch>/it5/debt` | `Build succeeded. 0 Warning(s) 0 Error(s)` · herramientas **182 / 182** · Core **2332: 2305 pasan, 0 fallan, 27 ignoradas** (eran 2319 / 2292: +13, las de §1.4, §4 y §5) |
| 2 | `tools/unity/run-unity-tests.ps1 -Platform EditMode` | **2388: 2357 pasan, 0 fallan, 31 ignoradas** (eran 2368 / 2337) |
| 3 | `… -Platform PlayMode` | **363: 363 pasan** (eran 354) |
| 4 | `… -Platform EditMode -Simulator` | **2570: 2539 pasan, 0 fallan, 31 ignoradas, 0 inconclusas** (eran 2550 / 2519); `RestoreAfterTests` reventó al cerrar y el *script* siguió (§3); el *define* restaurado |
| 5 | `… -Platform PlayMode -Simulator` | **429: 429 pasan** (eran 420); el *define* restaurado |
| 6 | `run-tests.ps1 -Matrix -UnityResults <scratch>/…/unity` | 597 IDs: **PASA 597, FALLA 0, IGNORADO 2** (AC-APP-15-03 [Apple], AC-WKT-02-10 [Android]), PENDIENTE UNITY 0, SIN TEST 0. Los IDs nuevos de la spec de la 5 (AC-WKT-13-15, -13-16, -16-07) todavía no están en una lista de trazabilidad (la arma la It. 5): sus pruebas PlayMode ya los llevan como categoría |
| 7 | `capture-snapshots.ps1 -Set it4b -Out <scratch>` (A) y sin `-Out` (B, al repo) | Las dos: `listo: 51 tomas, 0 con problemas, 0 sondas de aspecto que fallan`; **A = B byte a byte** en los 53 archivos (en `manifest.json`, solo `durationMs`) |
| 8 | `capture-snapshots.ps1 -Set it4a`, `-Set it3`, `-Set it2` con `-Out <scratch>` | 49 / 47 / 41 tomas, 0 con problemas, 0 sondas que fallan; **iguales byte a byte a lo versionado** (13 / 49 / 44 archivos; en `manifest.json`, solo `durationMs`). `it4a` se tomó dos veces (antes y después de `StartingAt`), las dos iguales. Los arreglos no tocan Dispositivo, Rutinas ni las tomas de reposo de Workout |
| 9 | `tools/build/build.ps1 -Target Windows -Simulator` | **NO VERIFICABLE**: antes de compilar se miró si el `RowMate.exe` de esa carpeta estaba abierto (pisar sus archivos con el *player* corriendo deja la carpeta a medias); lo estaba desde antes de empezar, se esperó de a 2 minutos durante 20 y siguió abierto. No se cerró ni se compiló |

**Qué cambió en `it4b` respecto de la 4b** (lo versionado), y por qué:

| Tomas | Por qué |
|---|---|
| CAP-13, CAP-14, CAP-15 | El modo compacto (QA-V4b-R2-01, §5). Medido en las nuevas, de tinta a tinta en la fila de la barra (y 55-105 px), contra 00-deuda-diseno §1.4 (± 1 px): título hasta **497** (igual), ícono del reloj **530-567** (esperado 531-567), cifras **583-709** (584-710), borde izquierdo del chip **734** (735) y el derecho **1004** (igual), punto en vivo **751-766** (750-767); los huecos, **32 · 15 · 24** px (33 · 15,5 · 24,5). En el *manifest*: el reloj de 73,14 a 71,13 dp y el chip de 106,90 a 102,88 dp, con el título en la misma escala |
| VID-01, VID-02, VID-06, VID-07, VID-08 | La misma barra libre en TV en español (el Designer los anunció, 00-deuda-diseno §1.4). Los valores por cuadro del *manifest* no cambian |
| CAP-18 (`Free Workout`) | 20 píxeles en **1 nivel** de un canal, en el borde de la `I` de `ACTIVE` (el antialias del texto). Las posiciones del *manifest* no cambian. El HUD ahora recibe su primer *layout* en el cuadro negro y no en el del toque; no se ve |
| VID-03 (la entrada con rutina, W1280) | Bytes del video distintos con los **mismos valores por cuadro** en el *manifest* (la cascada, el fondo y la página): diferencias de píxel del mismo tipo que CAP-18 en el H.264. QA puede mirar los cuadros con PyAV, como en la 4b |
| El resto (CAP-01 a CAP-12, CAP-16, CAP-17, CAP-19 a CAP-31, DEMO-01 a -12, VID-04, VID-05) | **Iguales byte a byte**. Una primera regeneración, sin `StartingAt`, cambiaba además CAP-03, -04, -16, -17, -26, -30 y DEMO-02, y tres sondas del punto en vivo fallaban: la fase de lo continuo dependía de cuándo se armaba el HUD (§2.2) |

---

## 7. Archivos

| Archivo | Qué |
|---|---|
| `Assets/RowMate/Core/Common/FrameClock.cs` | `SignalAsleepSeconds` |
| `Assets/RowMate/Core/Common/SuspendAccounting.cs` | El registro de cada aviso, cada despertar y el salto de pared sin evidencia |
| `Assets/RowMate/Platform/Power/SystemSleepSource.cs` | La hora de cada aviso (`GetTickCount64` en el *callback*, protegido), `SignalAsleepSeconds`, el registro de la medida |
| `Assets/RowMate/Platform/Time/UnityScheduler.cs` | La duración de los avisos en el `FrameClock` |
| `Assets/RowMate/Presentation/Workout/HudWarmup.cs` | Nuevo: el precalentado del HUD |
| `Assets/RowMate/Presentation/Workout/ImmersiveWorkoutPage.cs` | El HUD en el negro (`HasView`), O-5 |
| `Assets/RowMate/Presentation/Workout/StepInfoBar.cs` | O-1 |
| `Assets/RowMate/Presentation/Motion/MotionDriver.cs` | O-4; `StartingAt` (el HUD armado en el negro, con las fases del toque) |
| `Assets/RowMate/Core/Design/InfoRowFit.cs`, `Core/Workout/PhaseChip.cs`, `Presentation/Workout/{FreeTopBar,WallClockView,PhaseChipView}.cs`, `Snapshots/HudExrCatalog.cs` | QA-V4b-R2-01: el modo compacto de la barra libre (DD5-01) |
| `Assets/RowMate/Bootstrap/AppRoot.cs`, `Bootstrap/ShellHost.cs` | El precalentado en el arranque, la medida de la entrada (solo desarrollo), el registro del *driver* |
| `Assets/RowMate/SimulatorPresentation/FrameTimeProbe.cs` | La línea de la entrada |
| `Assets/RowMate/Editor/Build/BuildOutputInspector.cs` | Los argumentos nuevos, prohibidos en el build normal |
| `Assets/RowMate/Snapshots/SnapshotRunner.cs`, `Snapshots/WrenchAudit.cs` | No miran el HUD antes de que exista |
| `Assets/RowMate/Tests/…` | Las pruebas de §1.4, §2.5 y §4; `LifecycleHarness` con la duración de los avisos |
| `tools/unity/run-unity-tests.ps1`, `capture-snapshots.ps1`, `README.md` | QA-V4b-R2-03 y O-R2-01 |
| `docs/migration/architecture/adr/020-reloj-despierto.md` | Addendum de la It. 5 |

---

## 8. Para QA y para el PO

- **QA**: verificar esta deuda antes de que se sumen los paneles (spec §5.1): AC-WKT-13-15 en el build (tres entradas; los paneles llegan después) con los pasos de §2.4, que acá no se pudo compilar; AC-WKT-13-16 con las pruebas de §4; las herramientas con un cierre que revienta (§3); CAP-13 a CAP-15 contra la tabla del Designer (§5, §6) y las dos diferencias por debajo de lo visible de CAP-18 y VID-03 (§6).
- **Coordinador**: el `RowMate.exe` de la demo del usuario quedó abierto en `Builds/Windows/simulator` (desde antes de empezar esta tarea): mientras siga abierto no se puede compilar ahí. Cuando lo cierre, el build de §2.4 es el primer paso de QA.
- **PO**: la demo (c) del cierre de la 5 con los pasos de §1.5 (suspender de verdad; qué buscar en el registro); DT5-D1 y DT5-D2 (§2.6); las precisiones del modo compacto en AC-WKT-16-02, -16-03, -16-07 y -19-01 que pide el Designer (00-deuda-diseno §1.5 y §8).
- **Para el diseño de la 5**: los paneles de parciales y repeticiones nacen con el HUD, en el cuadro negro (DT5-D1), no en el del toque.
