# It. 9 — Historial y detalle con gráficos: diseño técnico

| Campo | Valor |
|---|---|
| Autor | unity-architect |
| Fecha | 2026-11-07 |
| Iteración | 9 · `09-historial` ([ROADMAP §It. 9](../../ROADMAP.md)) |
| Entrada | [01-po-spec](01-po-spec.md) (43 AC, CAP-01 a -17, VID-01 y -02, DEMO-01 a -05, DV-90 a -95); [00-arte-historial](00-arte-historial.md) (D9-01 a -14, A9-01 a -11) |
| Legacy leído (solo lectura, `3ae0f3a`) | [rowmate-legacy](https://github.com/figuibej/rowmate-legacy) (`../rowmate-legacy`): `lib/features/history/{history_provider,history_screen,session_detail_screen}.dart`, `lib/core/database/database_service.dart:205-322`; `fl_chart` 1.1.1 (`utils/utils.dart:117-187`, `:295-310`; `chart/base/axis_chart/axis_chart_helper.dart:23-58`; `chart/line_chart/line_chart_painter.dart:565-640`). No se leyó el módulo de credenciales. `git -C ../rowmate-legacy status --porcelain` vacío |
| Código leído (este repo) | `Core/Ports/{ISessionRepository,IRoutineRepository}.cs`, `Core/Models/{SessionStats,DataPoint,WorkoutSession,SessionListing,MetricPalette,RoutineExpansion}.cs`, `Core/Text/{TimeFormats,Fmt,DartMath}.cs`, `Core/Design/{Tokens,Icons,CheckGlyph}.cs`, `Infrastructure/Sessions/SqliteSessionRepository.cs`, `Infrastructure/Database/LocalStore.cs`, `App/Routines/RoutinesStore.cs`, `App/Shell/ShellStore.cs`, `App/Composition/{AppComposer,AppContext}.cs`, `Presentation/Shell/{AppShellView,TabBarView,PageStack,IPage,ToastLayer}.cs`, `Presentation/Routines/{RoutineListView,RoutinesScreenView,StructureBarView}.cs`, `Presentation/Controls/{SegmentedControl,FitBox,DrawnText,Tooltip}.cs`, `Presentation/Styles/RowMateTheme.tss`, `Snapshots/SnapshotCatalog.cs`, `Tests/UiSupport/{HostSessions,HostRoutines,DeviceProfile,UiHostOptions,FrameAllocations}.cs` |
| Estado | Diseño compacto, **sin código**. Orden: **WP-A** → **WP-D1** → **WP-B1** → **WP-B2** → **WP-D2** → **WP-B3** (§9) |

Convenciones: rutas relativas a `Assets/RowMate/` salvo `tools/` y `docs/`. Marcas: **N** nuevo, **C** cambia. `sd` = `session_detail_screen.dart`, `hs` = `history_screen.dart`, `db` = `database_service.dart`, `fl` = `fl_chart` 1.1.1.

---

## 0. Resumen

1. **Todo lo que calcula es Core puro** (`RowMate.Core.History`, dotnet): formatos, columnas de la tarjeta, rendimiento por paso, franjas, líneas objetivo, serie con split invertido, escalas y marcas de `fl_chart` portadas al pie de la letra, reducción por columna, suavizado, geometría de la malla en *buffers* reusados y el punto más cercano. La vista solo copia vértices.
2. **La consulta del historial calcula las estadísticas en el hilo de la base** (`ISessionRepository.GetHistoryAsync`): el hilo principal recibe la lista lista. Con 200 sesiones no hay cuadros largos.
3. **`HistoryStore` (App, puro)**: carga al arrancar, recarga al tocar la pestaña (también si ya estaba elegida), al volver del detalle y al cerrar una sesión. Una recarga en vuelo más una pendiente, como máximo. Si los datos no cambiaron **no emite** (igualdad por valor): la lista no se toca (Q-22).
4. **Lista virtual** (`ListView` de alto fijo por escala de letra, como el `ListView.builder` del legacy), con una entrada breve para la sesión nueva y la posición conservada.
5. **El detalle es una página apilada** (`IPage`) con su `SessionDetailStore`. El gráfico es una **malla propia** de UI Toolkit (`generateVisualContent`), con una capa hermana para el cursor y el tooltip que nunca repinta la malla grande.
6. **Manda la spec** sobre el arte en 13 puntos (§10.2): sin dos columnas, gráfico de 220 en todos los tamaños, paradas del split al piso, tilde en el selector, fila de paso y resumen del legacy y colores base.
7. **Arnés `it9`** con las bases sembradas por código (`HistorySeeds`: `F-9`, `F-9V`, `F-9P`; `F-ROTA` de la It. 3) en `ROWMATE_DATA_DIR` temporal, reloj de prueba y zona UTC−03:00. Dos corridas iguales byte a byte.

### 0.1 Decisiones del Architect (una línea cada una)

| ID | Decisión |
|---|---|
| DT9-01 | `GetHistoryAsync` hace 2 consultas en el ejecutor de la base (sesiones `ORDER BY started_at DESC, id DESC`; puntos de todas las sesiones `ORDER BY session_id`) y agrupa y calcula `SessionStats.Compute` ahí mismo (`db:248-252`, `:273-321`; `heart_rate` nulo = 0) |
| DT9-02 | `HistoryEntry(WorkoutSession Session, SessionStats Stats)` es un `record`: la igualdad por valor decide si una recarga cambió algo. Si es igual, no emite |
| DT9-03 | Recargas: si hay una en vuelo, se marca **una** pendiente que corre al terminar. Así la última lectura siempre es posterior al último pedido |
| DT9-04 | `ShellStore.Tap(int)` y su evento `Tapped(int)`. La barra llama a `Tap`, que elige y siempre avisa. `Select` no cambia (AC-APP-02-03) |
| DT9-05 | Cerrar una sesión: `HistoryStore` mira `WorkoutEngine.State` y recarga cuando `Close` pasa a `Saved` o `Failed`, porque una sesión con el cierre fallado queda cortada con sus puntos y también se lista. La cortada por cerrar la app aparece con la carga del arranque |
| DT9-06 | Recarga fallida con lista: la lista queda igual y el evento `ReloadFailed` muestra el aviso `historyLoadError` de 4 s con `ToastLayer`. Sin lista: `LoadFailed` con `Reintentar` (Q-13, DV-95) |
| DT9-07 | Duración del paso = puntos × `HistoryRules.AppSampleIntervalSeconds` (5), un parámetro para la It. 13 (Q-23, `sd:218`) |
| DT9-08 | Split: `y = ritmo > 0 ? lento − ritmo : 0`, con `lento` = el máximo de los ritmos > 0. Objetivo: `max(0, lento − objetivo)`. Etiquetas y tooltip: `m:ss` del ritmo truncado. Sin ritmo, no hay etiquetas Y (Q-24, DV-93) |
| DT9-09 | Eje Y con **4 divisiones fijas** (`fl`: `(220 − 24) ~/ 40`). Como el gráfico mide 220 en todos los tamaños, las marcas no cambian con la ventana |
| DT9-10 | La malla se reduce con mínimo y máximo por columna de 1 dp, solo si hay más puntos que columnas. Se suaviza con la cúbica de `fl` (0,2) solo si los puntos quedan a 3 dp o más. El cursor busca sobre los puntos **originales** |
| DT9-11 | El cambio de métrica es un **fundido cruzado de 220** de la curva vieja y la nueva, con el eje Y reescalado en 300 `ease.move` (spec AC-HIS-12-02, ANIMATION_BIBLE §2.12). Sin transformar vértices (§10.1, D9 §5.6) |
| DT9-12 | Tooltip y etiquetas sin basura por cuadro: el tooltip es un `DrawnText` sobre un `char[]`. Las etiquetas de los ejes (≤ 16 `Label` reusados) cambian de texto solo al cambiar de métrica o de ancho |
| DT9-13 | El detalle se prepara en el hilo principal al volver la lectura (`SessionChartData.Build`, ≤ 2 ms con 746 puntos), con el marcador `RowMate.Chart.Prepare`. El dibujo usa `RowMate.Chart.Generate` |
| DT9-14 | El tilde de la opción elegida es `CheckGlyph` dibujado (malla chica), sin glifo nuevo en la fuente de íconos |
| DT9-15 | Las bases de prueba se arman **por código** con los repositorios reales (`HistorySeeds`). Los números de la spec son el oráculo de `HistorySeedsTests`. Para el `[PERF]` hay `tools/dotnet/RowMate.SessionList seed <set> --data-dir <dir>` |
| DT9-16 | La zona de prueba entra por `LocalStore(…, localZone)` y `UiHostOptions.LocalZone` (UTC−03:00), y llega a `SqliteSessionRepository`, que ya la acepta |
| DT9-17 | Strava no aparece: no se escribe nada de la It. 12 (ni ícono ni botón) |

---

## 1. Árbol de archivos

### 1.1 Puro (dotnet) — WP-A

```
Core/History/                         N  namespace RowMate.Core.History
  ChartMetric.cs                      N  enum Watts, Spm, Split, Distance, HeartRate (orden de sd:25, :67-73) + Unit, Palette
  HistoryRules.cs                     N  AppSampleIntervalSeconds = 5, XIntervalFor(lastSeconds)
  HistoryFormats.cs                   N  fecha, paso, ejes, tooltip (string y Write* sobre char[])
  StatColumns.cs                      N  StatColumn + Build(SessionStats) → 5 o 6
  StepBreakdown.cs                    N  StepBreakdownRow + Compute(points, interval)
  StepBands.cs                        N  StepBand + Build(points) + StepBandPalette
  TargetLines.cs                      N  TargetLine + Build(bands, flatSteps, metric, slowest)
  ChartSeries.cs                      N  SessionChartData.Build(points, flatSteps) (5 series, lento, franjas, objetivos)
  AxisScale.cs                        N  FlChartInterval, AxisTicks.Iterate, YAxis.For, XAxis.For
  ChartDecimation.cs                  N  MinMaxPerColumn
  ChartSmoothing.cs                   N  la cúbica de fl (0,2) y su teselado
  ChartGeometry.cs                    N  ChartFrame, ChartMeshBuffer, ChartGeometry.Write*
  ChartHitTest.cs                     N  Nearest(xs, x)
  ChartTimeline.cs                    N  entrada, cambio de métrica, cursor y tooltip: funciones de t (puras)
Core/Design/ChartSpecs.cs             N  medidas y tiempos del gráfico (A9-08)
Core/Models/HistoryEntry.cs           N  record HistoryEntry(WorkoutSession Session, SessionStats Stats)
Core/Ports/ISessionRepository.cs      C  + Task<IReadOnlyList<HistoryEntry>> GetHistoryAsync()
Core/Localization/L10nKeys.cs         C  + HistoryStatPower, HistoryStatSpm, HistoryStatSplit, HistoryLoadError, SessionLoadError
Localization/strings.{es,en}.json     C  + 5 claves (223 → 228), textos de spec §5
App/History/                          N  namespace RowMate.App.History
  HistoryPhase.cs, HistoryState.cs    N  Loading | Ready | LoadFailed; (Phase, Entries)
  HistoryStore.cs                     N  §3.1
  HistoryCardModel.cs                 N  textos de la tarjeta (nombre, fecha, columnas) con el Localizer
  SessionDetailPhase.cs, SessionDetailState.cs  N  Loading | Ready | NoTelemetry | LoadFailed
  SessionDetailStore.cs               N  §3.2
App/Shell/ShellStore.cs               C  Tap(int) + event Tapped
App/Composition/{AppComposer,AppContext}.cs  C  History + CreateSessionDetail(entry); Start() carga el historial
Infrastructure/Sessions/HistoryQuery.cs        N  SQL y agrupado (DT9-01)
Infrastructure/Sessions/SqliteSessionRepository.cs  C  GetHistoryAsync
Infrastructure/Database/LocalStore.cs C  localZone opcional (DT9-16)
Tests/Support/{InMemorySessionRepository,ScriptedSessionRepository}.cs  C  GetHistoryAsync; fallas a pedido en historia, sesión y puntos
Tests/Support/HistorySeeds.cs         N  F-9, F-9V, F-9P, rutina Escalera, sesión de 60 min a 1 Hz (estrés)
Tests/Support/TempDatabase.cs         C  Store(…, localZone)
Tests/EditMode/History/*.cs           N  §8.2
Tests/EditMode/{Localization/CatalogParityTests,Localization/L10nKeysTests,Shell/ShellStoreTests}.cs  C  228 claves; Tap
Tests/EditMode/Traceability/It09Ids.cs  N  matriz §8.3
tools/dotnet/RowMate.SessionList/     C  comando `seed` (DT9-15)
```

### 1.2 Unity — WP-B

```
Presentation/History/                 N  namespace RowMate.Presentation.History
  HistoryScreenView.cs                N  pantalla de la pestaña: estados y lista (§5.1)
  HistoryListView.cs                  N  ListView virtual + diferencia por id + entrada de la nueva (§5.2)
  HistoryCardView.cs                  N  la tarjeta (Bind(HistoryCardModel, n))
  StatRowView.cs                      N  5 o 6 columnas con FitBox (tarjeta y resumen)
  SessionDetailPage.cs                N  IPage (§5.3)
  StepBreakdownView.cs                N  filas de paso
  SessionChartView.cs                 N  la malla (§5.4)
  ChartAxisLabels.cs                  N  pool de Label de los ejes
  SessionChartCursor.cs               N  guía, punto y tooltip (§5.5)
Presentation/Controls/SegmentedControl.cs  C  variante .rm-segmented--chart: ancho por texto, achicar hasta 0,70 (gate M-02), tilde CheckGlyph
Presentation/Shell/{AppShellView,TabBarView}.cs  C  Historial = HistoryScreenView; la barra llama Tap
Presentation/Styles/Base.uss          C  estructura .rm-history*, .rm-session*, .rm-chart* (layout, sin color)
Snapshots/HistoryCatalog.cs           N  conjunto it9 (§7)
Snapshots/SnapshotCatalog.cs          C  registra it9
Tests/UiSupport/{UiHostOptions,HostSessions,HostRoutines}.cs  C  LocalZone; HostSessions.Seeded(set)
Tests/PlayMode/History/*.cs           N  §8.2
Tests/PlayModeSimulator/HistorySimulatorTests.cs  N  demo (finish → arriba; Reopen → cortada)
tools/unity/capture-snapshots.ps1, tools/unity/README.md  C  -Set it9 por defecto
```

### 1.3 Arte — WP-D

```
Presentation/Styles/Skin.uss          C  color, tipografía, radios y capas de estado de .rm-history*, .rm-session*, .rm-chart*
Presentation/Styles/Motion.uss        C  hover y presionado de la tarjeta y del selector
Presentation/Motion/History/HistoryMotion.cs  N  escalonado de la primera carga, entrada de la tarjeta nueva, cascada del detalle (§5.6)
docs/migration/iterations/09-historial/04-design-notes.md  N
```

---

## 2. Core puro (WP-A)

### 2.1 Formatos (AC-HIS-05-01 a -04; spec §4)

```csharp
public static class HistoryFormats
{
    string CardDate(DateTimeOffset local);           // "dd/MM/yyyy  HH:mm", dos espacios, por componentes (sin cultura) · hs:74
    string StepDuration(int seconds);                // m:ss, minutos sin relleno: 45 → "0:45", 3600 → "60:00" · sd:221
    string StepLine1(int seconds, int meters);       // "{dur}  ·  {n}m" · sd:254
    string StepWatts(int w);                         // "{n}W" · sd:259
    string StepSpm(double spm);                      // Fmt.Fixed(spm, 1) + " spm" · sd:260
    string StepSplit(int seconds);                   // "m:ss/500m", 0 → "0:00/500m" · sd:222-224, :261
    string AxisTime(double seconds);                 // m:ss sin horas: (int)s / 60, (int)(s % 60) · sd:434-436
    string AxisValue(double v);                      // entero truncado · sd:459-461
    string AxisPace(double slowest, double v);       // m:ss de (int)(slowest − v) · sd:447-456
    int WriteTooltip(ChartMetric m, double raw, char[] buf, int offset);  // "{x.x} {W|spm|m|bpm}"; split "m:ss s", ritmo ≤ 0 → "--:-- s" · sd:501-510
}
```

Los de la sesión se reusan: `SessionStats.DurationFormatted` (`TimeFormats.Elapsed`) y `SplitFormatted` (`SplitOrDashes`). El SPM usa `Fmt.Fixed(x, 1)`, el `toStringAsFixed` de Dart con punto, y los enteros `Fmt.Integer`, sin miles.

### 2.2 Columnas, tarjeta y resumen (AC-HIS-07-03 a -05, AC-HIS-10-01)

```csharp
public sealed record StatColumn(MetricKind Metric, string LabelKey, string Value);
public static class StatColumns { IReadOnlyList<StatColumn> Build(SessionStats s); }  // hs:109-123
```

El orden es Tiempo (`MetricKind.Time`, blanco), Distancia `{n}m`, Watts (`historyStatPower`), SPM, Split y, solo si `HasHeartRate`, Pulso `{prom}/{máx}`. El color es `MetricPalette.For` (el **base**; §10.2). `HistoryCardModel.From(HistoryEntry, Localizer)` (App) agrega el nombre (`RoutineName ?? historyFree`) y la fecha. El título del detalle es `RoutineName ?? workoutFree` (Q-09).

### 2.3 Rendimiento por paso (AC-HIS-11-01, -03, -04; `sd:183-224`)

```csharp
public sealed record StepBreakdownRow(int StepIndex, int PointCount, int DurationSeconds, int DistanceMeters,
                                      int AvgWatts, double AvgSpm, int AvgSplitSeconds);
public static class StepBreakdown { IReadOnlyList<StepBreakdownRow> Compute(IReadOnlyList<DataPoint> points, int sampleIntervalSeconds); }
```

- Agrupa por `StepIndex` (los nulos fuera), en orden ascendente, sumando los tramos no contiguos.
- `AvgWatts = DartMath.Round(Σw / n)`, con los ceros. `AvgSpm = Σspm / n` (se formatea a 1 decimal). `AvgSplit = DartMath.Round(Σsplit / n)`.
- `Distance = last − first`, puede dar negativa. `Duration = n × interval`.

### 2.4 Franjas (AC-HIS-14-01, -02; `sd:313-332`, `:410-418`)

```csharp
public sealed record StepBand(double Start, double End, int StepIndex, string TypeName);
public static class StepBands { IReadOnlyList<StepBand> Build(IReadOnlyList<DataPoint> points); }
public static class StepBandPalette { ColorToken For(string typeName); }  // work/rest/warmup/cooldown = Tokens.Step*; otro = Tokens.StepUnknown (#607D8B)
```

Es el bucle del legacy literal: abre una franja en cada índice distinto del abierto, con el tipo del punto que la abre (`?? "work"`). Los puntos sin índice no cortan, y la última franja cierra en el último punto. El color "otro" es `Tokens.StepUnknown`, que ya existe (`theme.dart:67`).

### 2.5 Líneas objetivo (AC-HIS-15-01, -03, AC-RUT-20-01; `sd:352-397`)

```csharp
public sealed record TargetLine(double Start, double End, double Y, double RawTarget, string TypeName);
public static class TargetLines { IReadOnlyList<TargetLine> Build(IReadOnlyList<StepBand> bands, IReadOnlyList<IntervalStep> flatSteps, ChartMetric m, double slowestPace); }
```

- Hay una línea por franja con `StepIndex < flatSteps.Count`.
- En Vatios van el mínimo y el máximo definidos, en SPM el objetivo de SPM y en Split `max(0, lento − objetivo)` (DT9-08). Distancia y Pulso no tienen.
- `flatSteps` sale de `RoutineExpansion.Flatten(routine.Steps)`, con progresiones. Si la rutina no existe, la lista va vacía.

### 2.6 Serie y datos del gráfico (AC-HIS-13-01 a -03; `sd:291-311`)

```csharp
public sealed class SessionChartData
{
    public static SessionChartData Build(IReadOnlyList<DataPoint> points, IReadOnlyList<IntervalStep> flatSteps);
    public int Count { get; }
    public double[] X { get; }                         // ElapsedSeconds del monitor
    public float[] Raw(ChartMetric m);                 // tal cual, con los ceros
    public float[] Plotted(ChartMetric m);             // = Raw salvo Split (DT9-08)
    public double SlowestPace { get; }                 // máx de ritmos > 0; 0 si no hay
    public IReadOnlyList<StepBand> Bands { get; }
    public ChartMetricView View(ChartMetric m);        // perezoso y en caché: YAxis, XAxis, TargetLines, MaxPlotted
}
```

Usa 5 × n `float` y un `double[]`: con 3 600 puntos son 86 KB, armados una vez por apertura.

### 2.7 Escalas y marcas (AC-HIS-13-04, -05; `fl`)

```csharp
public static class FlChartInterval { double Round(double input); }   // utils.dart:134-183: < 1 con la cadena de Fmt.DartToString; ≥ 1 mantisa 7,6/2,6/1,6 → 10/5/2/1
public static class AxisTicks { int Iterate(double min, double max, double baseline, double interval, double[] outValues); } // axis_chart_helper.dart:23-58 + getBestInitialIntervalValue (utils.dart:295-310) con DartMath.Mod (módulo de Dart)
public readonly struct YAxis { double Max; double Interval; int TickCount; double[] Ticks;
    static YAxis For(double maxPlotted); }    // Max = 1,1·m > 0 ? : 100; Interval = Round(Max / 4) (DT9-09); Ticks = Iterate(0, Max, 0, Interval)
public readonly struct XAxis { double Min, Max, Interval; double[] Ticks;
    static XAxis For(double first, double last); }   // Interval = HistoryRules.XIntervalFor(last): 30/60/120/300 (sd:525-532)
```

- Las etiquetas Y son `AxisValue` o, en el split, `AxisPace`. Sin ritmo no hay ninguna.
- La grilla va en las marcas **interiores** (`minIncluded: false, maxIncluded: false`, como `fl`). La base es una línea aparte (§2.9).
- Vectores del oráculo, en `AxisScaleTests`: Vatios `0 50 100 150 198`; SPM `0 … 25 26`; Split `3:00 … 2:00`; Pulso `0 50 100 150 165`; X `0:05 2:00 … 10:00 11:00`, `3:44 5:00 … 30:00 33:44` y `… 62:05`; y los de `roundInterval` (0,0042, 0,37, 1, 1,59, 1,6, 2,6, 7,6, 9,99, 49,5, 619,6).

### 2.8 Reducción, suavizado y punto más cercano (DT9-10)

```csharp
public static class ChartDecimation { int MinMaxPerColumn(double[] xs, float[] ys, double x0, double x1, int columns, int[] outIndices); }
public static class ChartSmoothing { int Tessellate(float[] px, float[] py, int n, float minSpacingDp, float[] outX, float[] outY); } // cúbica line_chart_painter.dart:575-636, temp = (next − prev)/2 · 0,2; sin preventCurveOverShooting (default de fl); hasta 8 partes por tramo; y limitada al área
public static class ChartHitTest { int Nearest(double[] xs, int count, double x); }   // búsqueda binaria, empate → el de la izquierda
```

- La reducción solo corre si `n > columns`.
- Por columna salen el mínimo y el máximo, en el orden en que ocurren. Si hay uno solo, se duplica, para que no se pierdan las caídas a 0.

### 2.9 Geometría (A9-01, A9-02; AC-HIS-13-06, -14-02, -14-03, -15-02)

```csharp
public readonly struct ChartFrame { DpRect Area; float PixelDp; static ChartFrame For(float widthDp, float heightDp, float pixelsPerDp); } // ChartSpecs
public sealed class ChartMeshBuffer { float[] X, Y; uint[] Rgba; ushort[] Indices; int VertexCount, IndexCount; void Clear(); } // capacidad fija, sin crecer por cuadro
public sealed class ChartGeometry
{
    public void Prepare(SessionChartData data, ChartMetric metric, ChartFrame frame);  // reducción + escala: una vez por métrica y ancho
    public void WriteStatic(ChartMeshBuffer b, float alpha);                 // franjas @10 %, tira de 3 dp, separadores [4,4] (salvo el 1.º), grilla @8 %, base
    public void WriteSeries(ChartMeshBuffer b, double yMax, float reveal, float lineAlpha, float areaAlpha, Rgb color); // línea 2 dp + 1 px suavizado, área en degradé 24 % → 0
    public void WriteTargets(ChartMeshBuffer b, double yMax, float reveal, float alpha);   // líneas [8,5] 2 dp blanco @78 %, fase anclada al inicio de su franja; área del tipo @7 % hasta 0
    public void WriteSinglePoint(ChartMeshBuffer b, double yMax, Rgb color);    // un punto: disco de 6 dp, sin área
}
```

- `reveal` ∈ [0;1] dibuja un **prefijo** de vértices y el último tramo interpolado hasta el corte.
- `yMax` animado reescala las y sin rehacer las x. El fundido cruzado son dos `WriteSeries` (la vieja y la nueva) con sus alfas.
- Las franjas, la tira y los separadores no dependen de la métrica.
- El color sale de los tokens y de `Rgb`. La compensación del panel gamma (`MeshColor`) la aplica la vista al copiar.

### 2.10 Tiempos (A9-11; ANIMATION_BIBLE §2.12; arte §5)

`ChartTimeline` es puro y toma `t` en ms con `reduceMotion`:

| Momento | Qué devuelve |
|---|---|
| `Entry(t)` | Marco, franjas, grilla y etiquetas en 0–200 `ease.enter`; revelado en 100–700 `ease.enter`; área en 150–450 |
| `Switch(t)` | Vieja 1→0 y nueva 0→1 en 220; `yMax` de la vieja a la nueva en 300 `ease.move`; etiquetas viejas 0–120 y nuevas 100–220; objetivos viejos 0–120 y nuevos 100–300 |
| `Tooltip(t)` | Entra en 90 `ease.enter` (escala 0,96 → 1) y sale en 160 `ease.exit` |
| Cursor | `CriticalSpring` ω 22 (`spring.snappy`) |

- Con reducir movimiento (gate M-01; manda la spec, AC-HIS-12-02 y §9): `Entry(t)` y `Switch(t)` devuelven su **estado final ya en `t = 0`**, sin fundidos ni reescalado: todo a opacidad 1, revelado completo, `yMax`, etiquetas y objetivos de la métrica nueva, y la curva vieja a 0. El cursor va en el acto y el tooltip solo cambia de opacidad (ningún AC lo prohíbe). `ChartTimelineTests.{EntryReduceMotion,SwitchReduceMotion}` lo verifica en `t = 0`.
- Los números van en `ChartSpecs`.

---

## 3. App (WP-A, puro)

### 3.1 `HistoryStore` (F-HIS-06; `history_provider.dart`, `main.dart:84`, `:146-148`)

```csharp
public sealed class HistoryStore : IDisposable
{
    public HistoryStore(ISessionRepository sessions, ShellStore shell, IReadOnlyObservable<WorkoutSnapshot> workout, IAppLogger logger);
    public IReadOnlyObservable<HistoryState> State { get; }   // arranca Loading con lista vacía
    public event Action? ReloadFailed;                         // falla con lista → aviso 4 s (DT9-06)
    public Task LoadAsync();                                   // coalescente (DT9-03)
}
```

| Evento | Qué hace |
|---|---|
| `AppContext.Start()` | `LoadAsync()` |
| `shell.Tapped(HistoryIndex)` | `LoadAsync()` |
| `SessionDetailPage.OnPopped` | `LoadAsync()` |
| `Close` → `Saved` o `Failed` | `LoadAsync()` |
| Falla sin datos | `LoadFailed` y `[DB] carga del historial: …` (Warning) |
| Falla con datos | `ReloadFailed` |
| Lista igual (DT9-02) | No emite |
| Después de `Dispose` | Lo que vuelve tarde se ignora (patrón de `RoutinesStore`) |

### 3.2 `SessionDetailStore` (F-HIS-09; `sd:35-54`)

```csharp
public sealed class SessionDetailStore : IDisposable
{
    public SessionDetailStore(HistoryEntry entry, ISessionRepository sessions, IRoutineRepository routines, IAppLogger logger);
    public IReadOnlyObservable<SessionDetailState> State { get; }   // Phase, Session, Stats, Steps, Chart (SessionChartData), Title key/text
    public IReadOnlyObservable<ChartMetric> Metric { get; }         // siempre arranca en Watts (Q-21)
    public void SelectMetric(ChartMetric m);
    public Task LoadAsync();                                         // Reintentar = LoadAsync
}
```

1. `GetByIdAsync(id) ?? entry.Session`.
2. `GetPointsAsync(id)`.
3. Si `RoutineId` está y la rutina existe, `Flatten(routine.Steps)`.
4. Con 0 puntos, `NoTelemetry`. Si no, `Ready` con `SessionStats.Compute(points)`, `StepBreakdown.Compute(points, 5)` y `SessionChartData.Build`.
5. Cualquier excepción da `LoadFailed` (AC-HIS-09-04) y `[DB] carga de la sesión: …`.

`AppContext.CreateSessionDetail(entry)` crea el store y lo dispone la página.

---

## 4. Persistencia (WP-A)

- `HistoryQuery` hace dos lecturas en una sola tarea del ejecutor:
  - `SELECT id, routine_id, routine_name, started_at, finished_at, total_* , avg_*, total_calories, strava_activity_id FROM workout_sessions ORDER BY started_at DESC, id DESC` (las fechas pasan por `IsoDate.Parse(localZone)`).
  - `SELECT session_id, power_watts, stroke_rate, pace_500m_seconds, distance_meters, elapsed_seconds, calories, heart_rate FROM data_points ORDER BY session_id`.
- El agrupado reusa una sola `List<DataPoint>` por sesión. Una sesión sin puntos queda con `SessionStats.Empty`.
- No hay migración: es el esquema v8 y se leen las sesiones de las It. 4a a 8 tal cual (AC-HIS-19-03).
- La consulta de puntos usa el índice `idx_data_points_session (session_id, elapsed_seconds)`, que ya existe en v8.
- `InMemorySessionRepository` calcula lo mismo. `ScriptedSessionRepository` agrega `HistoryFailures`, `SessionFailures`, `PointsFailures` y `HistoryDelay` (fallar N veces y después responder).

---

## 5. Pantallas (WP-B)

### 5.1 Historial

```
#history-screen .rm-history                (TabHost, índice 3; AppBar historyTitle)
├── #history-spinner      Spinner 32/3, solo en Loading sin datos, con 120 ms de gracia y fundido de 160 (ART_BIBLE §3.10)
├── #history-empty        .rm-empty: history 56 @12 % → 12 → #history-empty-text (historyEmpty, text.tertiary)
├── #history-error        .rm-empty: #history-error-text (historyLoadError) + #history-retry (tonal, retry)
└── #history-list         HistoryListView (ListView)
    └── #history-card-<n> .rm-history-card   n = 0 la más nueva; focusable; capa de estado 4 / 8 %
        ├── .rm-history-card__top   rowing 18 #00B4D8 · #history-card-<n>-name 15/600 con "…" · -date 12 text.tertiary · chevron 16 @24 %
        └── StatRowView .rm-stat-row  (12 debajo) .rm-stat × 5|6 > FitBox > .rm-stat__value[--metric] 16/700 + .rm-stat__label 11/500
```

- La lista es una columna a todo el ancho, con margen de 12 y 10 entre tarjetas (AC-HIS-07-06).
- El relleno inferior es el de la barra de pestañas (`ShellLayout`).
- Abrir una tarjeta (toque, Enter o Espacio) hace `Pages.Push(new SessionDetailPage(entry))`.
- En el escritorio, Tab pasa de tarjeta en tarjeta: `NavigationMoveEvent` lleva al índice siguiente y `ScrollToItem` lo trae, porque los elementos virtuales no están en el árbol. La rueda desplaza la lista.

### 5.2 Lista virtual y recarga sin parpadeo (Q-22, AC-HIS-06-03)

- `ListView`:
  - `virtualizationMethod = FixedHeight` y `fixedItemHeight = HistoryCardView.Height(textScale) + 10`, medida una vez por escala.
  - `selectionType = None` y `reorderable = false`.
  - Los elementos son los índices de `HistoryState.Entries`.
- Al llegar un `HistoryState` distinto:
  - Las tarjetas cuyo id sigue se **vuelven a enlazar en su lugar**, sin animar (`RefreshItems`). Si su texto es igual, no cambia nada.
  - Si entran ids nuevos arriba y el desplazamiento es > 0, se suma `k × alto` al `scrollOffset`, así lo que se ve no se mueve.
  - Si el desplazamiento es 0, `HistoryMotion.PlayTopInsert(k)`: el contenido entra desde −k·alto con `spring.smooth` y la tarjeta nueva funde 60–260 `ease.enter`. Con reducir movimiento, en el acto.
- El indicador nunca sale si ya hay datos. La primera carga con la pestaña a la vista escalona 35 ms hasta 8 tarjetas. Si llegó antes, entra con la transición de la pestaña (patrón de `RoutineListView`).

### 5.3 Detalle (`SessionDetailPage : IPage`)

```
#session-detail .rm-page                    (PageStack.Push: por encima de la barra, AC-APP-03-01)
├── AppBarView  flecha (navBack) + título (RoutineName ?? workoutFree)
├── #session-spinner | #session-no-telemetry (sessionNoTelemetry, text.tertiary) | #session-error (sessionLoadError + #session-retry)
└── #session-scroll  ScrollView, relleno 16
    ├── #session-summary  .rm-card: sessionSummary 16/700 → 12 → StatRowView
    ├── (20) #session-steps .rm-card: sessionStepBreakdown 16/700 → 12 → #session-step-<n> (solo si algún punto tiene índice)
    │       .rm-step-row: blanco 4 %, r 8, 10/8 · #…-badge 28×28 r 6 #00B4D8 @16 % "n" 12/700 #00B4D8 · línea 1 11 text.secondary · línea 2 12/600 (3 Label, 12 entre ellos)
    ├── (20) #session-metric  SegmentedControl .rm-segmented--chart (5 opciones, 40, 13/600, elegida #0077B6 + tilde)
    └── (8)  #session-chart   .rm-chart 220, bg.sunken r 12
             ├── SessionChartView (malla, pickingMode Ignore)
             ├── ChartAxisLabels  #chart-y-<i>, #chart-x-<i>  (.rm-chart__axis: fuente numérica tabular 11, text.tertiary)
             └── SessionChartCursor (recibe el puntero; malla chica + #chart-tooltip DrawnText)
```

- La flecha, "Atrás" de Android y Escape hacen `Pages.Pop`. `OnPopped` dispone el store y pide `History.LoadAsync()`.
- **En todos los tamaños hay una sola columna** y el gráfico mide 220 (§10.2).
- Las secciones entran en cascada de 35 ms si los datos llegan después del *push*. Si llegan durante el *push*, entran con la página (`HistoryMotion.PlayDetailCascade`).

### 5.4 `SessionChartView` (A9-01)

- Tiene un `ChartGeometry` y un `ChartMeshBuffer` con capacidad fija (§6).
- `generateVisualContent` hace `MeshGenerationContext.Allocate(vc, ic)` y copia el *buffer*, con `MeshColor` y sin `Painter2D`.
- Se marca sucio (`MarkDirtyRepaint`) solo mientras corre una línea de tiempo (entrada o cambio) o al cambiar el ancho. Desplazar la página **no** repinta.
- Al cambiar el ancho hace `Prepare` otra vez. `ChartAxisLabels` ubica las etiquetas con `translate`, no con *layout*, y la de los bordes la empuja adentro (como `fl`).
- Hay un control interno para el arnés y las pruebas: `ShowCursorAt(double seconds)`, `FinishEntry()` y `LastGenerateMs`.

### 5.5 Cursor y tooltip (A9-03; AC-HIS-13-07)

- **Puntero de escritorio**: `PointerMove` sobre el área elige `ChartHitTest.Nearest` sobre los originales y la guía y el punto van con el resorte. `PointerLeave` lo oculta.
- **Toque**:
  - `PointerDown` muestra el cursor en ese cuadro.
  - Si el dedo se mueve más de 6 dp con |dx| > |dy|, el gráfico captura el puntero (`CapturePointer` + `StopPropagation`) y la página no se desplaza.
  - Si el primer movimiento es vertical, suelta el puntero, oculta el cursor y deja desplazar a la `ScrollView`.
  - `PointerUp` oculta el cursor **en el acto** (spec, con la salida de 160).
- **Tooltip**:
  - Caja `glass.fallback`, borde 1 `border.default`, r 8 y relleno 6/10, con el texto de `HistoryFormats.WriteTooltip`.
  - Va centrado sobre el punto, 10 dp arriba. Si no entra, va abajo, ajustado a 4 dp de cada borde.
  - Solo muestra el valor de la curva (DV-94).
- Mover el cursor repinta solo la malla chica (≤ 40 vértices) y el `DrawnText`.

### 5.6 Movimiento (WP-D, `HistoryMotion`)

Contrato que llama WP-B (todo con `MotionDriver`; con `MotionSettings.ReduceMotion`, las variantes de §2.10 y del arte §1.4 y §2.1):

```csharp
public static class HistoryMotion
{
    void PlayFirstLoad(MotionDriver d, IReadOnlyList<VisualElement> visibleCards);       // 35 ms, hasta 8, 240 ease.enter, y +12 → 0
    void PlayTopInsert(MotionDriver d, VisualElement listContent, VisualElement newCard, float rowHeight, int count);
    void PlayDetailCascade(MotionDriver d, IReadOnlyList<VisualElement> sections, bool duringPush);
}
```

La línea de tiempo del gráfico **no** está acá: es `ChartTimeline` (Core) y la corre `SessionChartView`.

---

## 6. Presupuesto (60 fps; el arte §7.3, en los números de esta spec)

| Ítem | Tope | Cómo se mide |
|---|---|---|
| Vértices de la malla grande (TV / W1280) | ≤ 4 500 / ≤ 9 500 (capacidad fija del *buffer*) | `ChartGeometryTests` con la sesión de 60 min a 1 Hz |
| Preparar el detalle (`Build` + `Prepare`) | ≤ 2 ms con 746 puntos y ≤ 6 ms con 3 600, en el escritorio | `RowMate.Chart.Prepare` en `HistoryPerformanceTests` |
| `generateVisualContent` en la entrada o el cambio | ≤ 0,2 ms de CPU en el escritorio (≤ 0,6 en el Android de referencia: **NO VERIFICABLE** sin el equipo) | `RowMate.Chart.Generate` y `LastGenerateMs` |
| Basura por cuadro | **0 B** en la entrada, el cambio, el cursor y el desplazamiento | `FrameAllocations` (nada de `GC.GetAllocatedBytesForCurrentThread`) |
| Cursor | 0 repintados de la malla grande | contador de repintados de `SessionChartView` |
| Lista `F-9P` | Primera vista ≤ 1,0 s; ningún cuadro > 50 ms al recargar; desplazamiento a 60 fps | `[PERF]` Device-Win con `seed F-9P`; en el Editor, `HistoryPerformanceTests` como indicio |
| Detalle de 1 h (746 puntos) | ≤ 0,5 s desde el toque | ídem |
| Elementos | Tarjeta ≤ 20 y unas 10 vivas; gráfico ≤ 22 (2 mallas + ≤ 16 etiquetas + tooltip); paso 6 | `UiQuery` en `HistoryScreenTests` y `SessionDetailTests` |

El HUD no se toca: el tope de 60 elementos del inmersivo (`HudBudgetTests`) no cambia.

---

## 7. Capturas y videos (WP-B2, conjunto `it9`)

**Método**:

- Se corre `tools/unity/capture-snapshots.ps1 -Set it9` (`Snapshots/HistoryCatalog.cs`), que escribe en `docs/migration/iterations/09-historial/snapshots/` con su `manifest.json` e `INDEX.md`, sin rutas de la PC.
- Cada toma abre una `TempDatabase` en el `ROWMATE_DATA_DIR` temporal y siembra con `HistorySeeds`.
- Las tomas usan el reloj de prueba, `UiHostOptions.LocalZone = UTC−03:00`, el simulador sin ruido y español. Van sin escena (`SceneMode.None`) y con Direct3D 11.
- Las líneas de tiempo terminan con `FinishEntry()`, salvo en los videos.
- Se hacen **dos corridas** y el `manifest` compara los SHA-256: los PNG y los MP4 tienen que dar iguales byte a byte.
- Nunca se usan el `prefs.json` ni las bases del usuario, y la guardia de `ProjectSettings` sigue activa.

| ID | Perfil | Base y estado | Sondas en el `manifest` |
|---|---|---|---|
| CAP-01 | TV | F-9, Historial | orden de los 6 nombres y fechas; columnas 6/6/6/5(sin puntos)/5/6; textos de AC-HIS-07-05 |
| CAP-02 | TH | F-9, Historial | ancho de la tarjeta = ventana − 24; ningún `FitBox` < escala 1 recortado |
| CAP-03 | P360, en, `TextScale` 1,3 | F-9, Historial | `History`, `Free`, `Time`, `Distance`, `HR`; escala de ajuste de `1:02:05`, `13796m` y `139/150` < 1 y sin desborde |
| CAP-04 | W1280 | F-9, Historial | una columna; chevron y fecha a la derecha |
| CAP-05 | TV | F-9V | el vacío centrado |
| CAP-06 | TV | F-9 n.º 2, Vatios | título `Entrenamiento libre`; sin `#session-steps`; 0 franjas; X `3:44` … `33:44` |
| CAP-07 | TV | F-9 n.º 1, Vatios | 8 filas (1, 2, 6 y 8 con sus textos); 8 franjas; 7 separadores; 6 objetivos; Y `0` … `198`; mínimos en 425 y 430 s |
| CAP-08 | TV | n.º 1, SPM | objetivos 18/20/22; Y `0` … `26` |
| CAP-09 | TV | n.º 1, Split | Y `3:00` … `2:00`; los ceros en y = base; los objetivos en las marcas 2:10, 2:08 y 2:06 |
| CAP-10 | TV | n.º 1, Pulso | color `#EF476F`; 0 objetivos; Y `0` … `165` |
| CAP-11 | TV | n.º 1, Distancia | 0 objetivos; máximo 2253 |
| CAP-12 | TV | n.º 3, Vatios | 2 filas; 2 franjas; X `0:05` … `3:00` |
| CAP-13 | TV | n.º 4 | solo `#session-no-telemetry` |
| CAP-14 | TH | n.º 1, Vatios, desplazado hasta el gráfico | una columna; selector y gráfico a todo el ancho |
| CAP-15 | W1280 | n.º 1, Split, `ShowCursorAt(300)` | tooltip `2:13 s`; guía en x(300) |
| CAP-16 | TV | n.º 6, Vatios | 3 filas, 3 franjas, 0 objetivos, título `Rutina borrada` |
| CAP-17 | TV | `HostRoutines.Rota()` | `No se pudo cargar el historial` y `Reintentar` |
| VID-01 | TV, ≈ 10 s | F-9 → `HistorySeeds.AddFinished` y `Close` → `Saved` en el motor de prueba → dos toques en Historial | 0 cuadros con el indicador; la nueva arriba |
| VID-02 | TV, ≈ 8 s | n.º 1: abrir y pasar por las 5 métricas cada 1,2 s | entrada y fundidos |
| DEMO-01 a -05 | W1280, simulador | una secuencia (`ScenarioScript`) en una base temporal vacía, con los pasos 1 a 5 de spec §1. El paso 5 usa `Reopen()` sin cerrar la sesión | una toma por paso |

---

## 8. Tests

### 8.1 Suites y comandos

- **dotnet**: `powershell -NoProfile -File tools/dotnet/run-tests.ps1 -ArtifactsPath <scratch>/it9`. Corre todo el Core, App, Infrastructure (SQLite real) y `Tests/Support`.
- **Unity**:
  - `tools/unity/run-unity-tests.ps1` (PlayMode y EditMode).
  - `tools/unity/run-unity-tests.ps1 -Simulator` (`HistorySimulatorTests`).
- **Build**: `tools/build/build.ps1` a `Builds/Windows/simulator`. Si está en uso, se reintenta cada 2 min hasta 20 veces.

### 8.2 Archivos de test

| Suite | Archivo | Qué |
|---|---|---|
| dotnet | `History/HistoryFormatsTests.cs` | spec §4 completa (fechas en UTC−03, bordes 0/3600/36000, punto decimal en es) |
| dotnet | `History/StatColumnsTests.cs` | 5/6 columnas, claves, orden y colores |
| dotnet | `History/StepBreakdownTests.cs` | agrupado, no contiguos, ceros, negativa, × intervalo |
| dotnet | `History/StepBandsTests.cs` | bucle legacy, `null` → work, sin índice no corta, paleta |
| dotnet | `History/TargetLinesTests.cs` | por métrica, fuera de rango, progresiones, split invertido y al piso, rutina borrada |
| dotnet | `History/ChartSeriesTests.cs` | X del monitor, ceros, lento, ritmo 0 al piso, sin ritmo, un punto |
| dotnet | `History/AxisScaleTests.cs` | `roundInterval` y `iterateThroughAxis` con módulo de Dart; todos los vectores de §2.7 |
| dotnet | `History/ChartGeometryTests.cs` | conteos, prefijo del revelado, fase del punteado, separadores salvo el 1.º, degradé, capacidad |
| dotnet | `History/ChartDecimationTests.cs`, `ChartSmoothingTests.cs`, `ChartHitTestTests.cs`, `ChartTimelineTests.cs` | picos y ceros conservados; umbral de 3 dp; vecino; tiempos y reducir movimiento (estado final en `t = 0`) |
| dotnet | `History/HistoryStoreTests.cs` | arranque, 4 disparadores, coalescencia, sin emitir si es igual, fallas con y sin lista, Reintentar |
| dotnet | `History/SessionDetailStoreTests.cs` | relee por id o usa la de la tarjeta, rutina actual, editada o borrada, NoTelemetry, LoadFailed, Watts por defecto |
| dotnet | `Infrastructure/HistoryQueryTests.cs` | SQLite: orden con empate por id, stats de los puntos (no de los totales), cortada, sin puntos, pulso nulo, base de la 4a |
| dotnet | `History/HistorySeedsTests.cs` | **oráculo F-9**: todos los números de la spec (AC-HIS-07-05, -11-03, -11-04, -13-04, -13-05, -14-01, -15-03, -19-04) |
| Unity PM | `History/HistoryScreenTests.cs` | estados, tarjeta, columnas, ajuste en TV/P360 1,3×/TH/W1280, abrir con toque y teclado, hover, Tab, error y Reintentar |
| Unity PM | `History/HistoryReloadTests.cs` | toque doble, sin indicador, desplazamiento conservado, sin re-animar, entrada de la nueva, reducir movimiento, aviso de 4 s |
| Unity PM | `History/SessionDetailTests.cs` | push y back/Escape, orden y aires, filas, selector (orden, tilde, ajuste P360 1,3×, flechas y Enter), error del detalle |
| Unity PM | `History/SessionChartTests.cs` | malla presente, etiquetas, tooltip (`150.0 W`, `2:13 s`, `--:-- s`), el gesto con la `ScrollView`, tiempos del cambio |
| Unity PM | `History/HistoryPerformanceTests.cs` | 0 B (`FrameAllocations`) en la entrada, el cambio, el cursor y el desplazamiento; 0 repintados con el cursor; marcadores |
| Unity Sim | `HistorySimulatorTests.cs` | libre → Terminar → arriba; rutina → Reopen → cortada con stats y gráfico |

### 8.3 Matriz AC → test (`It09Ids.cs`)

| AC | Tests |
|---|---|
| AC-HIS-05-01, -02, -03, -04 | `HistoryFormatsTests` (+ `StatColumnsTests` para -02 y -04) |
| AC-HIS-06-01 | `HistoryStoreTests.LoadsOnStart`, `HistoryScreenTests.NoSpinnerAfterStartupLoad` |
| AC-HIS-06-02 | `HistoryStoreTests.ReloadTriggers`, `HistoryReloadTests.TapTwiceReloads`, `HistorySimulatorTests.FinishedSessionOnTop` |
| AC-HIS-06-03 | `HistoryStoreTests.EqualListDoesNotEmit`, `HistoryReloadTests.{NoFlicker,KeepsScroll,NewOnTopEnters,ReduceMotion}`, VID-01 |
| AC-HIS-06-04 | `HistoryStoreTests.Failures`, `HistoryReloadTests.ReloadFailureToast`, `HistoryScreenTests.ErrorAndRetry`, CAP-17 |
| AC-HIS-07-01 | `HistoryScreenTests.Empty`, CAP-05 |
| AC-HIS-07-02 | `HistoryQueryTests.OrderAndAll`, `HistoryScreenTests.Order`, CAP-01 |
| AC-HIS-07-03, -04 | `StatColumnsTests`, `HistoryScreenTests.CardRows`, CAP-01 |
| AC-HIS-07-05 | `HistorySeedsTests.CardValues`, `HistoryQueryTests.StatsFromPoints`, CAP-01 |
| AC-HIS-07-06 | `HistoryScreenTests.FitsAllProfiles`, CAP-02, -03, -04 |
| AC-HIS-07-07 | `HistoryScreenTests.{OpenByTap,Keyboard,Hover}` [Device-Win] |
| AC-HIS-09-01 | `SessionDetailTests.{PushOverTabs,BackAndEscape,TitleFree}` [Device-And si hay equipo] |
| AC-HIS-09-02 | `SessionDetailStoreTests.{RereadsById,FallbackSession,CurrentRoutineSteps}` |
| AC-HIS-09-03 | `SessionDetailTests.SectionOrder`, `SessionDetailStoreTests.NoTelemetry`, CAP-06, -07, -13 |
| AC-HIS-09-04 | `SessionDetailStoreTests.LoadFailed`, `SessionDetailTests.ErrorRetryBack` |
| AC-HIS-10-01 | `SessionDetailTests.SummaryEqualsCard`, CAP-06, -07 |
| AC-HIS-11-01, -03, -04 | `StepBreakdownTests`, `HistorySeedsTests.StepRows`, CAP-07 |
| AC-HIS-11-02 | `SessionDetailTests.StepRowLayout`, CAP-07 |
| AC-HIS-12-01 | `SessionDetailTests.{SelectorOptions,SelectorFitsP360}` (escala ≥ 0,70, sin `…`), `SessionDetailStoreTests.DefaultsToWatts`, CAP-07 a -11 |
| AC-HIS-12-02 | `ChartTimelineTests.{Switch,SwitchReduceMotion}`, `SessionChartTests.SwitchTimingAndKeys`, VID-02 |
| AC-HIS-13-01, -02, -03 | `ChartSeriesTests`, `HistorySeedsTests.ChartSeries`, CAP-06, -07, -09 |
| AC-HIS-13-04, -05 | `AxisScaleTests`, `HistorySeedsTests.Axes`, CAP-06 a -10 |
| AC-HIS-13-06 | `ChartGeometryTests.Style`, `SessionChartTests.Colors`, CAP-07 |
| AC-HIS-13-07 | `ChartHitTestTests`, `HistoryFormatsTests.Tooltip`, `SessionChartTests.Tooltip` [Device-Win], CAP-15 |
| AC-HIS-14-01, -02 | `StepBandsTests`, `HistorySeedsTests.Bands`, CAP-07 |
| AC-HIS-14-03 | `ChartGeometryTests.Separators`, CAP-06, -07 |
| AC-HIS-15-01, -03 | `TargetLinesTests`, `HistorySeedsTests.Targets`, CAP-08, -09 |
| AC-HIS-15-02 | `ChartGeometryTests.TargetDashes`, CAP-07 |
| AC-RUT-20-01 | `TargetLinesTests.DeletedRoutine`, `SessionDetailStoreTests.{EditedRoutine,DeletedRoutine}`, CAP-16 |
| AC-HIS-19-03 | `HistoryQueryTests.{DefaultsWithoutPoints,MissingHr,OldSessions}` |
| AC-HIS-19-04 | `HistorySeedsTests.CutSession`, `HistorySimulatorTests.CutSessionAfterReopen`, CAP-12, DEMO-05 |
| AC-HIS-19-05 | `HistoryPerformanceTests` (indicio en el Editor) + `[PERF]` Device-Win con `seed F-9P` |
| Se vuelven a verificar | AC-RUT-18-01 a -09 (`SessionStats` ya portado, en `HistoryQueryTests`); AC-HIS-19-01, -02; AC-APP-02-03 (`ShellStoreTests.SelectSameDoesNotNotify` y `Tap` siempre avisa); AC-APP-02-08 (Historial sin provisorio); AC-HIS-20-04 (guardia de `TempDatabase`) |

---

## 9. Paquetes de trabajo

**Orden**: WP-A → WP-D1 → WP-B1 → WP-B2 → WP-D2 → WP-B3.

**Reglas**:

- Una sola instancia de Unity. Nadie edita fuentes mientras corre una.
- Los archivos de cada paquete son **disjuntos**: los de §1.1, §1.2 y §1.3.
- Si un paquete necesita algo de otro, lo anota y no lo toca.

| Paquete | Alcance | Hecho = |
|---|---|---|
| **WP-A** (dotnet) | §1.1 completo: Core/History, ChartSpecs, HistoryEntry, puerto, claves (228), App/History, ShellStore.Tap, composición, HistoryQuery, LocalStore con zona, dobles, HistorySeeds, tests de §8.2 (dotnet), It09Ids y `seed` | `run-tests.ps1` verde; `HistorySeedsTests` reproduce todos los números de la spec, con las erratas de §10.3 aplicadas |
| **WP-D1** | `Skin.uss`, `Motion.uss` y `HistoryMotion.cs` sobre el árbol y las clases de §5 | compila en Unity (lo compila WP-B1 en su primera corrida) |
| **WP-B1** (Unity) | pantallas §5.1 a §5.5, SegmentedControl `--chart`, AppShellView/TabBarView, `Base.uss`, UiSupport y tests PlayMode y Simulator | `run-unity-tests.ps1` y `-Simulator` verdes |
| **WP-B2** | `HistoryCatalog` (§7), dos corridas iguales y build de simulador | `snapshots/` con `manifest.json` e `INDEX.md`; build en `Builds/Windows/simulator`; `[PERF]` medido con `seed F-9P` |
| **WP-D2** | pulido sobre las capturas, solo en sus archivos; `04-design-notes.md` | notas con cada ajuste |
| **WP-B3** | vuelve a capturar si D2 cambió algo; deja `STATUS` de WP-B en este documento | dos corridas iguales |

---

## 10. Integración del arte y la spec

### 10.1 Pedidos al Architect (00-arte-historial §9)

| ID | Decisión |
|---|---|
| A9-01 | **Adoptado**: malla propia con *buffers* reusados, revelado por prefijo y sin `Painter2D`. La transformación de vértices **no**: el cambio es un fundido cruzado con reescalado (DT9-11) |
| A9-02 | **Adoptado**, salvo la etiqueta del máximo que se omite, la regla de etiquetas X que chocan y los huecos del split: manda la spec (§10.2) |
| A9-03 | **Adoptado** (capa hermana, 6 dp y \|dx\| > \|dy\|, ω 22). Sin la permanencia de 1 500 ms: al soltar se va (AC-HIS-13-07) |
| A9-04 | **Adoptado** (`ListView` virtual, alto fijo por escala, clave por id). Hay entrada de la nueva; colapsar e iluminar no se usan, porque esta iteración no borra ni edita sesiones |
| A9-05 | **Adoptado** el achicar para entrar (`FitBox`, mínimo 0,70). **Rechazada** la unidad en otra clase (D9-03): el valor es un solo texto, como el legacy |
| A9-06 | **Rechazado**: una columna y 220 en todos los tamaños (AC-HIS-07-06, AC-HIS-09-03, CAP-14; además mantiene las 4 divisiones de DT9-09) |
| A9-07 | **Rechazado** el conteo del resumen: la spec no lo pide y el valor final aparece en el acto. Va a "Para issues" |
| A9-08 | **Adoptado** `ChartSpecs` (36, 6, 18, 3 + 2, 2 dp, [4,4], [8,5], 24 %, 6 dp), sin la permanencia de 1 500. **Ningún token nuevo** (el "otro" es `step.unknown`, que ya existe) |
| A9-09 | **Adoptado solo para las franjas** (tipo del punto que abre, `null` → work). La fila de paso no lleva color de tipo (AC-HIS-11-01, -02) |
| A9-10 | **Adoptado** (FrameAllocations, `RowMate.Chart.Generate` y la sesión de 60 min a 1 Hz en `HistorySeeds`); las tomas en la base temporal |
| A9-11 | **Adoptado** para lo que queda adoptado |

### 10.2 Arte contra spec (manda la spec)

| Arte | Spec | Queda |
|---|---|---|
| D9-02: cifras `*.hud` | AC-HIS-07-04: `#4CC9F0`, `#FFD60A`, `#00B4D8`, `#FF9F1C`, `#EF476F` | colores base (el contraste del pulso va a "Para issues") |
| D9-03: unidad más chica | `{n}m` del mismo texto | un solo texto |
| D9-04: resumen en dos renglones | AC-HIS-10-01: la misma fila de la tarjeta | fila única |
| D9-05 y D9-06: fila de paso teñida, número en blanco | AC-HIS-11-02: insignia `#00B4D8` @16 %, número `#00B4D8` 12/700, fila blanca @4 % | la de la spec |
| D9-09: se omite la etiqueta del máximo | AC-HIS-13-04: incluyen el 0 y el máximo | se incluye (ya está en "Para issues" de la spec) |
| D9-10: huecos en el split | AC-HIS-13-03: ritmo 0 al piso | al piso |
| D9-11: sin puntos en `text.tertiary` | AC-HIS-07-04: color de la métrica | color de la métrica |
| D9-13: sin tilde | AC-HIS-12-01: con tilde | `CheckGlyph` (DT9-14) |
| D9-14 y §6: dos columnas, 280, columna de 720 | AC-HIS-07-06, CAP-14 | una columna a todo el ancho y 220 |
| §1.1: margen 16 | AC-HIS-07-06: margen 12 y 10 entre tarjetas | 12 y 10 |
| §1.2: nombre `titleS` 16/600, chevron 18 `text.tertiary` | AC-HIS-07-03: 15/600 y `>` 16 @24 % | lo de la spec |
| §2.4: selector 12/600 | DV-91: 13/600 | 13/600, que se achica hasta 0,70 si no entra (D9-12; gate M-02). En P360 a 1,3× queda en ≈ 0,73 (≈ 12,3 px), más que los 11 px del legacy (`sd:278`) |
| §4: no dibujar una marca X intermedia que choca | AC-HIS-13-05: los múltiplos, el inicio y el fin | todas; las de los bordes se empujan adentro (`fl`) |
| §5.5: renglón 2 del tooltip con el tiempo | AC-HIS-13-07: `{valor} {unidad}` | un renglón |
| D9-08: Inter proporcional en los ejes | AC-HIS-13-06: cifras tabulares | fuente numérica tabular a 11 (`.rm-chart__axis`) |

**Adoptados del arte**:

- D9-01, D9-07 (tooltip `glass.fallback` con `metric.*.hud`) y D9-12 (ancho por texto).
- La tira de tipo de 3 dp, la grilla @8 %, la línea base `border.default` @12 % y el degradé 24 → 0.
- La entrada del gráfico (§5.1 del arte = ANIMATION_BIBLE) y la cascada del detalle.

### 10.3 Erratas de la spec (corregidas en la spec por el gate M-03; quedan acá como registro; ningún resultado visible cambia)

- **E-01, AC-HIS-14-01**: las franjas de `Escalera` son `warmup` 5–65, `work` 65–185, `rest` 185–245, `work` 245–365, `rest` 365–425, `work` 425–545, `rest` 545–605 y `cooldown` 605–660. Dice `work 65–125, rest 125–185`, y eso choca con el trabajo de 120 s y con la fila 2 (24 puntos, `2:00`).
- **E-02, AC-HIS-13-02 y §6**: los dos ceros de la fila 6 van en **425 y 430 s**. En 420 caen en la fila 5 (descanso), y entonces la fila 6 daría `144W`, no el `138W` `22.1 spm` `2:02/500m` de AC-HIS-11-03. Para cuadrar los promedios tienen que estar los dos dentro de los 24 puntos de la fila 6 (425 a 540).

Las dos se verificaron con la regla de siembra de §6: 12/24/12/24/12/24/12/13 puntos; distancias 183, 432, 433 y 2253; pulso 139.

---

## 11. Riesgos

| Riesgo | Mitigación |
|---|---|
| `ListView` virtual con Tab y anillo de foco, y el `scrollOffset` al insertar arriba | Navegación propia por índice (§5.1) con un test. Si `ListView` no lo resuelve en WP-B1, se pasa a `ScrollView` con armado por tandas de 16 tarjetas por cuadro y se anota |
| El gesto del gráfico contra la `ScrollView` táctil de UI Toolkit | `CapturePointer` después del umbral; `SessionChartTests` simula los dos gestos; [Device-And] si hay equipo |
| Las etiquetas X de los bordes se encimen (`10:00` y `11:00` en TV) | Es la paridad pedida por la spec; va a "Para issues" |
| `roundInterval` < 1 depende del `toString` de Dart | `Fmt.DartToString` ya portado, con vectores en `AxisScaleTests` |
| El selector de 5 opciones con tilde en P360 a 1,3× | Necesita ≈ 0,73: ≈ 236 dp útiles (328 − 26 del tilde − 60 de relleno − 6 de bordes) para ≈ 325 de texto. Achicar hasta **0,70**, el tope del `FitBox` (gate M-02), con relleno de 6 a 12; `SelectorFitsP360` verifica escala ≥ 0,70 y que no haya `…`. Si igual no entra, se anota como desvío y no se reinterpreta |
| `[PERF]` en Android sin el equipo | **NO VERIFICABLE** en `REPORT.md`; se arrastra a la It. 14 |
| La consulta de puntos de todas las sesiones con miles de sesiones | Corre en el hilo de la base, con el índice `idx_data_points_session` |
| Que el arnés o las pruebas lean bases reales | `ROWMATE_DATA_DIR` temporal, `TempDatabase` con su guardia y `HistorySeeds` solo sobre bases temporales |

---

## 12. Para issues

- Contraste del pulso base `#EF476F` sobre `surface.card` (3,81:1) en la tarjeta y el resumen (D9-02).
- Unidad más chica que la cifra (D9-03), resumen con jerarquía (D9-04), fila de paso con el color de su tipo (D9-05, D9-06) y conteo del resumen (A9-07).
- Dos columnas en el detalle en horizontal y en el escritorio, y columna de 720 en la lista (D9-14, A9-06).
- Huecos en el split en lugar del piso (D9-10) y transformación de la curva entre métricas (arte §5.6).
- Choque de etiquetas X en los bordes y del máximo del eje Y.
- Lo que ya está en spec §12 y en arte §10.

---

## 13. Estado de WP-B

Hecho (B1 + B2 de §9): §1.2 completo salvo `HistoryMotion` (WP-D1 no estaba: ver desvíos). Pantallas §5.1 a §5.5, `SegmentedControl --chart`, `AppShellView`/`TabBarView` (`Tap`), `Base.uss`, `UiSupport` (`LocalZone`, `UseHistory`, `HostRoutines.History`, `HostSessions.Seeded`), 37 pruebas PlayMode en `Tests/PlayMode/History/` y 2 del simulador, `HistoryCatalog` (`it9`, por defecto), el build de simulador y la prueba a mano.

Resultados (corridas reales; números en el `REPORT`):

- dotnet (`run-tests.ps1`): 3 500 pruebas, 3 461 pasan, 39 omitidas, 0 fallan (más las 192 de herramientas).
- Unity: compilación limpia; EditMode 3 603 (3 556 pasan, 47 ignoradas, 0 fallan) y con `-Simulator` 3 795 (3 748, 47, 0); PlayMode con `-Simulator` 663 de 663 y sin él 588 de 588.
- Tomas `it9`: 24 tomas (17 CAP, 2 VID, 5 DEMO), 0 con problemas, con `manifest.json` e `INDEX.md` sin rutas; dos corridas (la segunda con `-Out <scratch>`) iguales byte a byte en los 24 PNG y MP4 (comparación de archivos: el `manifest` no compara SHA-256 solo). La sonda de aspecto de CAP-01 (dominante del valor de Watts y Pulso) no encuentra el color: se anota para WP-D.
- Build `Builds/Windows/simulator`: `build.ps1 -Target Windows -Simulator` → 0 (Mono, Development), guardia de `ProjectSettings` sin cambios.
- Prueba a mano y `[PERF]` Device-Win: con `ROWMATE_DATA_DIR` temporal y clics sintéticos en la ventana del build (960 × 600, ayuda en `<scratch>`). Con `seed F-9P` la lista completa ya se ve a los 300 ms del toque (≤ 1,0 s), el segundo toque no muestra indicador y el detalle de 30 min está a la vista a los 500 ms (con la entrada corriendo). Con `seed F-9`: una sesión libre de 1:39 terminada aparece arriba con 5 columnas (sin pulsómetro), las dos cortadas por cerrar la app también; `Escalera` remada 2:40 y terminada a mano muestra 2 filas, 2 franjas y los objetivos 140/160 en Vatios; la rueda desplaza el detalle. El cuadro más largo de una recarga y los 60 fps del desplazamiento no se midieron con el Profiler del build (NO MEDIDO: para issues); en el Editor, con la base en el hilo principal, el peor cuadro de recarga de F-9P es de 55-80 ms (indicio, no el build).

Desvíos (una línea cada uno):

- **Lista con `ScrollView`, no `ListView` virtual** (el plan B de §11): todas las tarjetas, armadas en tandas de 16 por cuadro la primera vez. Tab, el anillo de foco, el `GrowMotion` de Rutinas y el relleno de `ShellLayout` andan solos. Con `F-9P` son unos 6 600 elementos vivos (el tope de §6 era unas 10 tarjetas vivas).
- **Desplazamiento conservado con un ancla**: la primera tarjeta a la vista; cada layout que la corre suma lo mismo al desplazamiento (el alto de la nueva cambia después del primer layout por las `FitBox`).
- **`HistoryMotion` (WP-D1) no existía**: la entrada escalonada, la de la nueva y la cascada del detalle usan `EnterMotion` y `GrowMotion` con los números de §5.6 dentro de `HistoryListView` y `SessionDetailPage`. WP-D puede extraerlas sin cambiar la conducta.
- **Colores base en `Base.uss`** (como el resto de `Base.uss`): fondo de la tarjeta, textos y tooltip; las transparencias de un token (fila de paso @4 %, insignia @16 %) por código (ADR 016). El pulido queda para `Skin.uss` (WP-D2).
- **Tipos de 15/600, 16/700, 12/700 y 13/600** como reglas propias con sus 3 escalas de letra: la tipografía generada no los tiene.
- **Escape cierra el detalle** (AC-HIS-09-01) con `IClosesOnEscape` en `BackNavigation` (P-08 sigue para el resto).
- **Fila de números**: el valor y la etiqueta van en dos `FitBox` del alto natural de su texto, centradas: un valor achicado queda en la misma línea media (en CAP-03 se veían como superíndices).
- **El selector** se achica hasta 0,70 (gate M-02); las flechas mueven el foco por `KeyDown` y por `NavigationMoveEvent` (una vez por cuadro).
- **Malla en trozos de ≤ 1 024 vértices** y con los triángulos dados vuelta al sentido del reloj al copiar (UI Toolkit no dibujaba el área de la curva, que Core escribe al revés).
- **Basura por cuadro** (`FrameAllocations`, `HistoryPerformanceTests`): el cursor da 0; la entrada asigna en ≈ 16 de 48 cuadros (el panel mientras las etiquetas funden y en el dibujo de la malla mientras crece) y el cambio de métrica en ≈ 10 de 24 (el panel, mientras funden las etiquetas Y). `WriteFrame` (Core) da 0. Son eventos de 700 y 300 ms, no gasto continuo: queda para issues.
- **Tooltip** dibujado carácter por carácter (`DrawnFont`, sin cadenas por cuadro); `TooltipText` arma la cadena solo para pruebas y sondas.
- **Capturas**: las CAP y los VID van con los dispositivos de prueba y sin la capa del simulador (la llave no tapa tarjetas); la DEMO, con el simulador. CAP-08 a -11 y -14 van desplazadas hasta el gráfico (en TV no entra con las 8 filas). VID-01 recarga con `History.LoadAsync()` después de `AddFinished` (lo que hace el cierre). DEMO-02 crea `Demo 9` por el repositorio (el editor es de la It. 3).
- **`HistoryStore` (de WP-A) recarga también al volver a reposo**: Terminar a mano vuelve a reposo apenas el cierre vuelve, sin publicar `Saved`, y la lista no recargaba (lo encontró `HistorySimulatorTests.FinishedSessionOnTop`). `HistoryStoreTests.ReloadTriggers` lo cubre.
- **`SnapshotCli`** suma `It9Output` y `DefaultSet = "it9"`; `capture-snapshots.ps1` y su README, `-Set it9` por defecto.
- **Pruebas que cambian por la It. 9**: `ShellTests.PlaceholdersShowTitleAndDimIconOnly` (ahora Perfil), `RoutinesScreenTests.RoutinesIsNotAPlaceholder`, `WorkoutScreenTests.WorkoutIsNotAPlaceholder` y las de herramientas (`-Set it9` por defecto).

Para issues:

- La entrada del gráfico y el cambio de métrica asignan en UIR (≈ 16 y ≈ 10 cuadros): probar etiquetas dibujadas (`DrawnFont`) y una malla de tamaño fijo con el Profiler.
- `RoutineListView`: una rutina agregada con la pestaña oculta deja un `GrowMotion` esperando un layout que no llega (frenaba la regla de toma de DEMO-02; la toma pasa por Rutinas antes).
- Con un split constante (el simulador), la curva del split queda en el piso y el eje va de `2:13` a `0:33` (regla Q-24 al pie de la letra).
- Las etiquetas X de los bordes se enciman (`3:44` y `5:00` en CAP-06; `10:00` y `11:00`): paridad con `fl_chart`.
- La cúbica de `fl_chart` hace un pico chico en cada escalón (se ve en W1280): paridad, sin `preventCurveOverShooting`.
- La cinta `SIMULADOR` tapa la primera letra del título en el escritorio (DEMO-01; de la It. 2).


---

## 14. Estado de WP-A

Hecho (dotnet, sin Unity): todo §1.1. `run-tests.ps1` verde; `HistorySeedsTests` reproduce los números de la spec con E-01 y E-02. Los `.meta` de los archivos nuevos los genera Unity en la primera corrida de WP-B1.

Desvíos (una línea cada uno):

- `SessionChartData.Raw` es `double[]` (no `float[]`): el tooltip formatea con `toStringAsFixed(1)` y en `float` 24,05 daría `24.0`. `Plotted` sigue en `float`.
- Columnas de 1 dp hasta `ChartSpecs.MaxColumns` = 630; más anchas en W1280 (1 184 dp de área con una columna): así un cuadro quieto entra en ≤ 4 500 (TV) y ≤ 9 500 (W1280). En el fundido cruzado hay dos curvas: la capacidad del *buffer* es la suma de los dos `VertexBudget`.
- Una columna con mínimo = máximo da un solo punto (un tramo de largo 0 no tiene normal); el primero y el último punto siempre están.
- `ChartSmoothing.Tessellate` recibe el piso (`yFloor`) y trabaja en (dp, valor): la cúbica es afín, así el `yMax` animado no la rehace.
- `ChartGeometry` expone las capas sueltas y `WriteFrame(buffer, current, previous, in ChartVisual)` con el orden del arte §3; `WriteStatic`/`WriteSeries`/`WriteTargets` quedan como atajos. Sin extremos redondos ni radio de la tira (para issues).
- `StatColumn` lleva su `ColorToken`: el Tiempo va en `text.primary` (`color ?? Colors.white`, `hs:146`), no en `metric.time`.
- `ScriptedSessionRepository`: lecturas con `ReadDelay`/`HistoryDelay` fuera de la cola de escrituras (la respuesta se lee al vencer la demora).
- `RowMate.SessionList` referencia `RowMate.Tests.Support` (el comando `seed` usa `HistorySeeds`); arma la base con un nombre neutro y la renombra, porque `TempDatabase.Guard` no deja el nombre de la app fuera de la carpeta de pruebas.
- Entrada: el área funde con `ease.enter` (el arte no nombra la curva). Cambio: fundido cruzado lineal; etiquetas y objetivos que salen con `ease.exit` y entran con `ease.enter`.
- `F-9P` tiene 200 × 362 = 72 400 puntos ("como la n.º 2", con el punto final repetido); la spec dice 72 200.

Contrato para WP-B: `AppContext.History`, `AppContext.CreateSessionDetail(entry)`, `ShellStore.Tap`, `HistoryStore.ReloadFailed`, `HistoryCardModel.From`, `SessionDetailState.{Columns,Steps,Chart,Title}`, `SessionDetailStore.{Metric,SelectMetric,LoadAsync}`, `ChartGeometry`/`ChartMeshBuffer`/`ChartTimeline`/`ChartHitTest`, `HistoryFormats.WriteTooltip`, `HistorySeeds.{Apply,WriteFile,AddFinished,Stress60MinAt1Hz,Zone}`, `TempDatabase.Store(…, localZone)`. La página llama `History.LoadAsync()` en `OnPopped`.

---

## Cambios por gate

Gate: [03-qa-gate](03-qa-gate.md), APROBADO CON OBSERVACIONES.

| Issue | Qué cambió |
|---|---|
| M-01 | §2.10: con reducir movimiento, `Entry` y `Switch` quedan en su estado final en `t = 0` (se van el fundido de 200 y el de 160). §8.2 y §8.3: `ChartTimelineTests.{EntryReduceMotion,SwitchReduceMotion}`. Arte §5.1 y §5.6 alineados, con una línea de motivo |
| M-02 | §1.2, §10.2 y §11: el tope del `.rm-segmented--chart` baja a **0,70**, el del `FitBox`. `SelectorFitsP360` verifica escala ≥ 0,70 y que no haya `…`. Arte §2.4 corregido, con una línea de motivo |
| M-03 | La spec ya tiene E-01 (AC-HIS-14-01) y E-02 (AC-HIS-13-02 y §6), con una línea de motivo. §10.3 queda como registro y el oráculo usa los mismos números que la spec |
| m-01 a m-06 | Sin cambios en el documento: se resuelven al implementar, como dice el gate. WP-A: igualdad con `SequenceEqual` sobre las `HistoryEntry` (`EqualListDoesNotEmit`), x centrada con un solo punto (`ChartSeriesTests`). WP-B1: reenlazar las tarjetas al cambiar el idioma, la sonda de CAP-09 compara la y del objetivo con la de su ritmo y `PointerUp` oculta el cursor con la salida de 160. WP-B2: medir `F-9P` en el `[PERF]` |
