# It. 9 — Historial y detalle con gráficos: verificación de QA

| Campo | Valor |
|---|---|
| Autor | rowmate-qa |
| Fecha | 2026-11-08 |
| Entrada | [01-po-spec](01-po-spec.md) (43 AC), [02-tech-design](02-tech-design.md) (§13 y §14), [03-qa-gate](03-qa-gate.md), [04-design-notes](04-design-notes.md), [snapshots/](snapshots/) (24 tomas) |
| Árbol | el repo de trabajo (sin worktrees ni copias). Legacy [rowmate-legacy](https://github.com/figuibej/rowmate-legacy) (`../rowmate-legacy`): `git status --porcelain` vacío antes y después |
| Artefactos | `<scratch>/it9/qa-r1` (dotnet), `<scratch>/it9/qa-unity` (Unity), `<scratch>/it9/qa-build` (build, bases temporales y capturas de la prueba a mano) |

## Veredicto: **APROBADO CON OBSERVACIONES**

No hay bloqueantes ni mayores. Hay 42 AC en PASS, 0 en FAIL y 1 NO VERIFICABLE (AC-HIS-19-05, en parte: el cuadro de 50 ms y los 60 fps no se pueden medir en el build). Los menores van a issues.

## 1. Corridas (salida real)

| Suite | Resultado |
|---|---|
| `run-tests.ps1 -Matrix` (dotnet 8, Release) | build OK. **3 500 tests: 3 461 pasan, 39 omitidos, 0 fallan.** Herramientas: 192 de 192. Matriz: PASA 737, FALLA 0, IGNORADO 4, PENDIENTE UNITY 163, SIN TEST 0 |
| Unity EditMode | **3 603: 3 556 pasan, 47 ignorados, 0 fallan** |
| Unity PlayMode | **588 de 588** |
| Unity EditMode `-Simulator` | **3 795: 3 748 pasan, 47 ignorados, 0 fallan** |
| Unity PlayMode `-Simulator` | **663 de 663** (los 37 de `PlayMode/History` y los 2 de `HistorySimulatorTests`, en verde) |
| Guardia de `ProjectSettings` | "sin cambios" en las 4 corridas y en el build. `git status` igual al del inicio |
| `build.ps1 -Target Windows -Simulator` | 0 (Mono, Development) en `Builds/Windows/simulator` |

Indicios de rendimiento en el Editor (`[HPF]`, PlayMode con simulador): 746 puntos se preparan en 0,19 ms y se generan en 0,52 ms (4 502 vértices). F-9P: la lista completa tarda 347 ms y el **peor cuadro de recarga es de 96 ms** (Editor, con la base en el hilo principal). Basura: la entrada asigna en 15 de 48 cuadros, el cambio en 10 de 24 y el cursor en 3 de 60.

## 2. Prueba a mano en el build de simulador (W 960 × 600, `ROWMATE_DATA_DIR` temporal)

Base: `seed F-9 --variant simulator` en `<scratch>/it9/qa-build/data`. Se hicieron clics sintéticos con la ventana adelante. Ninguna base del usuario se abrió.

1. **Sesión libre a mano**: 75 s y `Finish` → `Finish`. Al tocar Historial **a los 0,3 s** ya está arriba (`01:27`, `328m`, `157`, `24.9`, `02:14`, 5 columnas sin pulsómetro), sin indicador. El detalle se titula `Free workout`, el eje X va de `0:16` a `1:27` (tiempo del monitor) y no tiene franjas.
2. **Libre cortada**: se cerró la ventana con el diálogo de terminar abierto, a los 71 s. Al reabrir aparece con sus números (`01:21`, `305m`, `155`). Otra cerrada a los 3 s queda como `00:00  0m  0  0.0  --:--`, que es paridad (spec §12).
3. **Rutina completa** (`Escalera`, 11 min): diálogo de completado y después Historial arriba con `11:10` y `2527m`. El detalle tiene 8 filas, 8 franjas y los objetivos 140/160, 150/170 y 160/180 en Vatios. Al recorrer **SPM, Split 500m (lento abajo, objetivos 2:10 / 2:08 / 2:06), Distance y HR** (plano en 0 sin pulsómetro y eje `0` a `100`), cada métrica responde. La flecha vuelve a la lista con la posición conservada.
4. **Rutina cortada** a los 100 s, cerrando la ventana: al reabrir está arriba con `02:02` y `460m`. Su detalle tiene 2 filas (`1:00  ·  208m` y `0:40  ·  132m`), 2 franjas, objetivos solo en el trabajo y el gráfico hasta el corte.
5. **Reinicio**: con 3 aperturas más de la app, todas las sesiones siguen con los mismos números.
6. **Cursor**: al mover el mouse sobre la curva, aparecen la guía, el punto y `145.0 W`. Arrastrar con el botón apretado también funciona. Con un `SetCursorPos` solo, sin movimiento real, Unity no se entera; eso es del arnés, no de la app. La tarjeta bajo el cursor se resalta (DN9-01). La rueda desplaza la lista y el detalle.
7. **F-9P (200 sesiones)**, con `seed F-9P`: a los **0,25 s del toque** la lista ya se ve. Un segundo toque no muestra indicador ni salto (la posición se conserva). El detalle de 30 min está a la vista a los 0,4-0,5 s, con la entrada en curso. El cuadro más largo y los 60 fps **no se pueden medir**, porque el build no tiene una sonda de cuadros para el historial (m-02).

## 3. Capturas (miradas una por una contra la spec, el arte y el legacy)

- **CAP-01**: el orden, las fechas `dd/MM/yyyy  HH:mm` y los números de AC-HIS-07-05 coinciden al carácter, con 6, 6, 6, 5, 5 y 6 columnas, colores de métrica y la flecha. La sonda de aspecto falla (0/2) por un nombre mal puesto (m-01): los colores de la toma son los correctos.
- **CAP-02 a -05**: entra en una columna. En P360 a 1,3× `1:02:05`, `13796m` y `139/150` se achican sin cortarse y `Rutina bor…` se recorta con `…`, como pide AC-HIS-07-03. El vacío lleva el ícono de 56 al 12 %.
- **CAP-06**: `Entrenamiento libre`, sin pasos ni franjas, eje Y `0` a `176`. Las etiquetas `3:44` y `5:00` se pisan y se leen "3:45:00" (m-04).
- **CAP-07, -08, -09, -10, -14**: las filas 1, 2, 6 y 8 coinciden (`138W  22.1 spm  2:02/500m`, `1:05  ·  183m`). Hay 8 franjas, 7 separadores, objetivos con progresión y caídas a 0 en 425 y 430 s. Los ejes van `0` a `198`, `0` a `26`, `3:00` a `2:00` con objetivos sobre 2:10, 2:08 y 2:06, y `0` a `165`. En el split, lo rápido queda arriba y los ceros en el piso (DV-93). CAP-07 en TV corta el gráfico abajo (m-07); CAP-14 lo muestra entero.
- **CAP-11, -12, -13, -16, -17**: distancia sin objetivos; la cortada con 2 filas, 2 franjas y `0:05` a `3:00`; `No hay datos de telemetría` sola; `Rutina borrada` con 3 franjas y sin objetivos; `No se pudo cargar el historial` con `Reintentar`.
- **CAP-15**: guía, punto con anillo y `2:13 s`.
- **DEMO-01 a -05**: siguen la demo de §1. En DEMO-03 la llave tapa la primera etiqueta X (m-06).
- **VID-01 y VID-02**: no se pudieron mirar porque no hay `ffmpeg` en la máquina. Sus AC (06-03 y 12-02) quedan cubiertos por `HistoryReloadTests`, `SessionChartTests` y `ChartTimelineTests`, y por el paso 1 del build (sin indicador a los 0,3 s).

## 4. Matriz AC → evidencia → resultado

| AC | Evidencia | Resultado |
|---|---|---|
| AC-HIS-05-01 a -04 | `HistoryFormatsTests`, `StatColumnsTests` (dotnet); CAP-01 | PASS |
| AC-HIS-06-01 | `HistoryStoreTests.LoadsOnStart`, `HistoryScreenTests.NoSpinnerAfterStartupLoad`; build | PASS |
| AC-HIS-06-02 | `HistoryStoreTests.ReloadTriggers`, `HistoryReloadTests.TapTwiceReloads`/`BackFromTheDetailReloads`, `HistorySimulatorTests.FinishedSessionOnTop`; build, arriba a los 0,3 s | PASS |
| AC-HIS-06-03 | `HistoryStoreTests.EqualListDoesNotEmit…`, `HistoryReloadTests.{NoFlicker,KeepsScroll,NewOnTopEnters,ReduceMotion}`; build con F-9P | PASS |
| AC-HIS-06-04 | `HistoryStoreTests.Failures`, `HistoryReloadTests.ReloadFailureToast`, `HistoryScreenTests.{ErrorAndRetry,RotaShowsTheError}`; CAP-17 | PASS |
| AC-HIS-07-01 | `HistoryScreenTests.Empty`; CAP-05 | PASS |
| AC-HIS-07-02 | `HistoryQueryTests.OrderAndAll`, `HistoryScreenTests.Order`; CAP-01 | PASS |
| AC-HIS-07-03, -04 | `StatColumnsTests`, `HistoryScreenTests.CardRows`; CAP-01, -03 | PASS |
| AC-HIS-07-05 | `HistorySeedsTests.CardValues`, `HistoryQueryTests.StatsFromPoints`; CAP-01 al carácter | PASS |
| AC-HIS-07-06 | `HistoryScreenTests.FitsAllProfiles`; CAP-02, -03, -04 | PASS (m-05) |
| AC-HIS-07-07 | `HistoryScreenTests.{OpenByTapAndKeyboard,TabWalksTheCards}`; build: clic, resaltado con el cursor y rueda | PASS |
| AC-HIS-09-01 | `SessionDetailTests.{PushOverTabsAndTitleFree,BackAndEscape}`; build: título y flecha | PASS (el "Atrás" de Android: sin equipo) |
| AC-HIS-09-02 | `SessionDetailStoreTests.{RereadsById,FallbackSession,CurrentRoutineSteps}` | PASS |
| AC-HIS-09-03 | `SessionDetailTests.{SectionOrder,NoTelemetry,FreeHasNoSteps}`; CAP-06, -07, -13 | PASS |
| AC-HIS-09-04 | `SessionDetailStoreTests.LoadFailedAndRetry`, `SessionDetailTests.ErrorRetryBack` | PASS |
| AC-HIS-10-01 | `HistorySeedsTests.SummaryEqualsCard`, `SessionDetailTests.SummaryEqualsCard`; CAP-06, -07 | PASS |
| AC-HIS-11-01, -03, -04 | `StepBreakdownTests`, `HistorySeedsTests.StepRows`; CAP-07; build (corte: 2 filas) | PASS |
| AC-HIS-11-02 | `SessionDetailTests.StepRowLayout`; CAP-07 | PASS |
| AC-HIS-12-01 | `SessionDetailTests.{SelectorOptions,SelectorFitsP360}`, `SessionDetailStoreTests.DefaultsToWatts`; CAP-07 a -11 | PASS |
| AC-HIS-12-02 | `ChartTimelineTests.{Switch,SwitchReduceMotion}`, `SessionChartTests.{SwitchTiming,SwitchReduceMotion}`, `SessionDetailTests.ArrowsAndEnter` | PASS (VID-02 sin mirar) |
| AC-HIS-13-01 a -03 | `ChartSeriesTests`, `HistorySeedsTests.ChartSeries`; CAP-06, -07, -09 | PASS |
| AC-HIS-13-04, -05 | `AxisScaleTests` (31 casos), `HistorySeedsTests.Axes`; CAP-06 a -10 | PASS (m-04, paridad) |
| AC-HIS-13-06 | `ChartGeometryTests` (13), `SessionChartTests.MeshAndLabels`; CAP-07 | PASS |
| AC-HIS-13-07 | `ChartHitTestTests`, `HistoryFormatsTests.Tooltip`, `SessionChartTests.{Tooltip,MouseHoverShowsAndLeaves}`; CAP-15; build (`145.0 W`) | PASS |
| AC-HIS-14-01 a -03 | `StepBandsTests`, `HistorySeedsTests.Bands`, `ChartGeometryTests.{BandsAndStrip,Separators}`; CAP-07 | PASS |
| AC-HIS-15-01 a -03 | `TargetLinesTests`, `HistorySeedsTests.Targets`, `ChartGeometryTests.TargetDashes`; CAP-07 a -09; build | PASS |
| AC-RUT-20-01 | `SessionDetailStoreTests.{EditedRoutine,DeletedRoutine}`, `TargetLinesTests`; CAP-16 | PASS |
| AC-HIS-19-03 | `HistoryQueryTests.{DefaultsWithoutPoints,MissingHr,OldSessions,ManySessionsInOneRead}` | PASS |
| AC-HIS-19-04 | `HistorySeedsTests.CutSession`, `HistorySimulatorTests.CutSessionAfterReopen`; CAP-12; build (libre y rutina cortadas) | PASS |
| AC-HIS-19-05 | `HistoryPerformanceTests` (indicio); build: lista ≤ 0,25 s y detalle ≤ 0,5 s | **NO VERIFICABLE** el cuadro de 50 ms y los 60 fps (m-02) |

Re-verificados: AC-RUT-18-01 a -09 (estadísticas en `HistoryQuery`), AC-HIS-19-01 y -02, AC-APP-02-03 (`ShellStoreTests.TapAlwaysNotifies…`), AC-APP-02-08 (Historial sin contenido provisorio) y AC-HIS-20-04 (bases temporales en todas las corridas): todos en PASS.

## 5. Revisión estática

- Las franjas (`StepBands`), los objetivos (`TargetLines`), la agrupación por paso (`StepBreakdown`) y la consulta (`HistoryQuery`: `ORDER BY started_at DESC, id DESC`, `heart_rate` nulo = 0) se compararon línea por línea con `sd:183-224` y `:313-397` y con `db:248-321`. Coinciden. Las únicas diferencias son las DV declaradas (split invertido, índice negativo descartado).
- `HistoryStore` y `SessionDetailStore`: la coalescencia y el `_inFlight`/`_loading` se limpian bien también cuando la tarea termina sincrónica. Lo que vuelve después de `Dispose` no publica nada. Al cambiar el idioma se reenlaza (`HistoryScreenView.OnTexts` → `RenderTexts`, que resuelve m-03 del gate).
- Nada nuevo de terceros: los archivos nuevos son fuentes propias, `.meta`, documentos y tomas. `Packages/` no cambia y `THIRD_PARTY_NOTICES.md` tampoco.

## 6. Repo público y codificación

- `git grep` (en lo versionado y en lo nuevo sin versionar) de unidades `X:\`/`X:/Users`, del usuario de la PC y del nombre del equipo: **0 hallazgos**. Solo aparecen la ruta estándar de instalación de Unity en `tools/` (ya estaba) y el autor en `LICENSE` y `README` (ya estaba e intencional). En los PNG y MP4 de `snapshots/` (búsqueda binaria) tampoco hay rutas ni datos de la PC. Las tomas usan datos sembrados o del simulador con el reloj de prueba.
- Los 107 archivos de texto cambiados o nuevos son **UTF-8 válido, sin BOM y sin doble codificación** (se buscaron `Ã¡`, `Ã±`, `â€` y `Â·`).
- Legacy intacto (`git status --porcelain` vacío). No se leyó `strava_secrets.dart`.

## 7. Issues

| ID | Sev. | Resp. | Issue | Evidencia | Corrección |
|---|---|---|---|---|---|
| m-01 | menor | architect | Las columnas de la tarjeta se llaman `history-card-stat-<i>` en **todas** las tarjetas, y no `history-card-<n>-stat-<i>`. `SetNamePrefix` renombra solo las columnas que ya existen y no guarda el prefijo, y `Bind` crea las columnas después con el prefijo del constructor. Por eso la sonda de CAP-01 lee `#000000` (0/2) y la toma igual queda "ok" | `Presentation/History/StatRowView.cs:47-56`, `:121`; `HistoryCardView.cs:61`, `:100`; `snapshots/manifest.json` (CAP-01: nombres repetidos) | Guardar el prefijo en un campo mutable y usarlo en `CreateColumn`. Que una sonda de aspecto que falla marque la toma |
| m-02 | menor | architect | AC-HIS-19-05: en el build no se puede medir el peor cuadro de recarga (≤ 50 ms) ni los 60 fps del desplazamiento, porque no hay una sonda `-rowmate-perf-*` para el historial. El indicio del Editor dio 96 ms (el diseño dice 55-80) y `TwoHundredSessions` solo lo registra, sin afirmar nada | log `[HPF]` de PlayMode `-Simulator`; `HistoryPerformanceTests.cs:126-150` | Agregar `-rowmate-perf-history` (con `FrameTimeProbe`, `seed F-9P`, toques y rueda) y medir en el build |
| m-03 | menor | architect | Hay basura por cuadro fuera del presupuesto de §6 (0 B): la entrada asigna en 15 de 48 cuadros, el cambio en 10 de 24 y **el cursor en 3 de 60** (§13 dice que el cursor da 0). Los tests lo toleran con topes | `[HPF] cursor: 11 asignaciones … 3 con alguna`; `HistoryPerformanceTests.cs:25-26` | Para issues (ya anotado en §13); corregir el dato del cursor en el `REPORT` |
| m-04 | menor | designer | Etiquetas de los ejes encimadas: `3:44`/`5:00` se lee "3:45:00" (CAP-06, F-9P), `0:27`/`0:30` y `2:00`/`2:02` (build), `10:00`/`11:00` (CAP-08 a -10) y `25`/`26` en el eje Y (CAP-08). Es paridad con `fl_chart`, pero es lo que más se nota | CAP-06, CAP-08; build | Issue con prioridad: no dibujar la marca intermedia que choca con un borde |
| m-05 | menor | designer | En P360 a 1,3×, `Distance` y el pulso quedan mucho más chicos que el resto, y hay cifras vecinas a ≈ 2 dp | CAP-03 | Para issues (04-design-notes §5) |
| m-06 | menor | architect | La llave del simulador tapa la primera etiqueta X del gráfico en W1280 (`2:11`, DEMO-03), y la cinta `SIMULADOR` tapa la flecha y la primera letra del título | DEMO-03, DEMO-05; build | Para issues (de la It. 2): mover la llave fuera del área del gráfico en el detalle |
| m-07 | menor | architect | En CAP-07 (TV) el gráfico queda cortado abajo: no se ven el `0` del eje Y ni el eje X que pide la tabla de §7 de la spec. CAP-14 lo muestra entero | CAP-07 | Desplazar CAP-07 hasta el gráfico como CAP-08, o anotar que CAP-14 lo cubre |

### Para issues (una línea cada uno)

- `HistoryListView` arma todas las tarjetas (≈ 6 600 elementos con F-9P), cuando el tope de §6 era ≈ 10 tarjetas vivas: lista virtual si m-02 da cuadros largos.
- La sesión libre cerrada a los pocos segundos queda en la lista como `00:00 0m` (paridad; spec §12: distinguir la cortada o vacía).
- El arnés de la prueba a mano necesita un movimiento real del mouse (`mouse_event` relativo) para el *hover*; `SetCursorPos` solo no alcanza. Conviene dejarlo en `tools/` si se repite.
- Fuera de alcance (It. 4b): el diálogo de completado de `Escalera` mostró `2527 m` y el HUD `2595 m` en el mismo instante.
