# Mapeo legacy → Unity, archivo por archivo

| Campo | Valor |
|---|---|
| Autor | unity-architect |
| Fecha | 2026-10-27 (sincronizado con el ROADMAP final tras el gate de QA del plan) |
| Base | [ARCHITECTURE.md](ARCHITECTURE.md) (árbol §2, assemblies §3), [ROADMAP](../ROADMAP.md) (17 iteraciones + S-1) |
| Alcance | Los 69 archivos de `rowmate-legacy/lib`, `shaders/water.frag` y los 26 archivos de `test/` |

Cómo leer la tabla:
- **Assembly**: `Core`, `App`, `Infra` (Infrastructure), `Sim` (Simulator), `SimUI` (Simulator.Presentation), `Platform`, `UI` (Presentation), `Scene`, `Boot` (Bootstrap), `Editor`, `Tests.EM` (EditMode, compartidos con `dotnet`), `Tests.Sim`, `Tests.PM` (PlayMode, solo Unity). Las rutas son relativas a `Assets/RowMate/` (o `Assets/Plugins/`).
- **It.**: iteración del roadmap que porta esa parte (1, 2, S-1, 3, 4a, 4b, 5, 6a, 6b, 7, 8, 9, 10a, 10b, 11, 12, 13, 14). Si un archivo se reparte, se indica qué parte va a cada una (con las líneas aproximadas que estima el roadmap). La 10b escribe todo el código nativo de Apple (ROADMAP regla 8); se nombra solo donde hay código nativo de Apple.
- Los nombres de clase son el objetivo; el diseño de cada iteración puede ajustarlos (y lo dice en sus "Desvíos propuestos").
- Regla de puerto: mismas fórmulas, constantes y casos borde, citando `lib/...:línea` en un comentario.

---

## 1. `lib/` — núcleo (`core/`)

### 1.1 Bluetooth

| Archivo legacy | Líneas | Assembly / ruta destino | Clase(s) Unity | It. | Notas |
|---|---|---|---|---|---|
| `core/bluetooth/ftms_parser.dart` | 142 | Core · `Core/Ble/` | `FtmsParser`, `FtmsUuids` (`0x1826`, `0x2AD1`, `0x2AD9`) | 1 | Tabla de 8 paquetes de F-BLE-02 como `[TestCase]` y vectores del oráculo. Valores "no disponible" se muestran crudos (Q-38) |
| `core/bluetooth/heart_rate_parser.dart` | 41 | Core · `Core/Ble/` | `HeartRateParser`, `HeartRateReading` (record) | 1 | Sin validar rango (Q-48) |
| `core/bluetooth/scan_filter.dart` | 20 | Core · `Core/Ble/` | `ScanFilter.Accept(BleScanResult, BleUuid, bool clientSide)`, `ScanFilter.IsClientSide(PlatformKind)` | 1 | `clientSide` solo en Windows (Linux fuera de alcance, Q-50). Nunca filtrar en iOS/macOS. Lo usa el *spike* S-1 |
| `core/bluetooth/ble_service.dart` | 345 | Core · `Core/Ble/` + Platform · `Platform/Ble/` + `Plugins/` | Core: `IRowerService`, `RowerService` (sobre `IBleAdapter`), `BleStatus`, `RowerWatchdog`, `StrokeCountIncrement`. Platform: `AndroidBleAdapter`, `WindowsBleAdapter`, `AppleBleAdapter`, `NativeEventPump` | 2 (interfaz `IRowerService` y `BleStatus`), 10a (resto + adaptadores Android y Windows), 10b (adaptador Apple) | Handshake `0x00` → 300 ms → `0x07`, keepalive 2 s (`:268-284`) y **keepalive nativo en segundo plano** (Q-91, ARCHITECTURE §6.1), reconexión (Q-29), suscripción a todas las características con notify (diagnóstico), MTU alto en Android. Licencia de FBP: no aplica |
| `core/bluetooth/heart_rate_service.dart` | 448 | Core · `Core/Ble/` | `IHeartRateService`, `HeartRateService`, `HrmStatus`, `HrmIncompatibleException`; `SupersededException` interna | 2 (interfaz y `HrmStatus`), 11 (resto) | Generaciones de conexión (`_connectGen`), `mtu: null`, memoria en `IPreferences` con `hrm.deviceId`/`hrm.deviceName`, `disconnect(queue:false)` → `DisconnectAsync(skipQueue: true)`. Con el pulsómetro real de Apple: criterios [Apple] en la 10b |
| `core/bluetooth/reconnect_loop.dart` | 91 | Core · `Core/Ble/` | `ReconnectLoop`, `StaleWatchdog` | 11 | Sobre `IScheduler` (tests con `FakeScheduler`) |
| `core/bluetooth/simulated_ble_service.dart` | 120 | Sim · `Simulator/` | `SimulatedRowerService : IRowerService` | 2 | "Conecta" al primer suscriptor; 1 lectura/s con pulso 0; caída simulada de 3 s; nombre "Remo simulado" fijo (Q-03) |
| `core/bluetooth/simulated_heart_rate_service.dart` | 156 | Sim · `Simulator/` | `SimulatedHeartRateService : IHeartRateService` | 2 | Comparte `RowingSimulator`; id `SIM:HRM`; `AutoConnectAsync` no hace nada (DC-21) |

### 1.2 Base de datos

| Archivo legacy | Líneas | Assembly / ruta destino | Clase(s) Unity | It. | Notas |
|---|---|---|---|---|---|
| `core/database/database_service.dart` | 340 | Core · `Core/Ports/` (interfaces) + Infra · `Infrastructure/Persistence/` | Core: `IRoutineRepository`, `ISessionRepository`. Infra: `RowMateDatabase` (archivo, pragmas, esquema v8, `IDbExecutor`/`SerialExecutor`), `SqliteRoutineRepository`, `SqliteSessionRepository`, DTOs `RoutineRow`, `IntervalStepRow`, `WorkoutSessionRow`, `DataPointRow`; opcional `LegacyDatabaseImporter` (migraciones v1→v7) | 3 (archivo, esquema, rutinas, base del simulador), 4a (sesiones y puntos, guardado incremental Q-12), 9 (consultas y estadísticas), 13 (limpieza de importadas Q-71, pendientes) | DDL idéntico a v7 + `origin` + índice (ARCHITECTURE §9.2). Fechas con `IsoDate` (microsegundos como Dart). `getStatsForSessions` usa `SessionStats.Compute` de Core. Importador solo si el PO lo pide (Q-11), nunca sobre la base del legacy en uso (R-14) |

### 1.3 Simulador de desarrollo

| Archivo legacy | Líneas | Assembly / ruta destino | Clase(s) Unity | It. | Notas |
|---|---|---|---|---|---|
| `core/dev/dev_config.dart` | 5 | Boot · `Bootstrap/` + asmdefs | `BuildFlags.IsSimulator` (`const` dentro de `#if ROWMATE_SIMULATOR`); `defineConstraints` de `RowMate.Simulator*`; `SimulatorBuildGuard` (Editor) | 1 (define y asmdefs), 2 (uso y guardia de build, con Editor) | Ningún build de distribución define el símbolo (R-01) |
| `core/dev/rowing_simulator.dart` | 83 | Sim · `Simulator/` | `RowingSimulator` (con `IRandom` inyectable; `noise: false` en tests) | 1 | Concept2: pace `500·(2.80/W)^(1/3)`, kcal/s `(W·4·0.8604+300)/3600`; topes 30-500 W y 14-40 SPM. `double.parse(spm.toStringAsFixed(1))` (`:60`) → `DartMath.RoundToFixed(spm, 1)`; `.round()` → `DartMath.Round` (ADR 012) |
| `core/dev/simulator_overlay.dart` | 212 | Sim · `Simulator/` (reglas) + SimUI · `SimulatorPresentation/` (vista) | Sim: `SimulatorPanelModel` (presets Suave/Medio/Fuerte, ± vatios/ritmo, "Remando", hora de escena, fallas). SimUI: `SimulatorOverlay` (cinta "SIMULADOR" + botón 🛠 + panel, UXML/USS) | 2 | Capa superior del shell (por encima de pilas y diálogos). Textos fijos en español (Q-03). "Hora escena" afecta la escena desde la It. 6a |

### 1.4 Modelos de dominio

| Archivo legacy | Líneas | Assembly / ruta destino | Clase(s) Unity | It. | Notas |
|---|---|---|---|---|---|
| `core/models/rowing_data.dart` | 63 | Core · `Core/Models/` | `RowingData` (record + `WithHeartRate`), `RowingFormats` | 1 | Formatos `02:13`, `--:--`, `100:00`, `1:01:01` (F-BLE-01) vía `Fmt` |
| `core/models/interval_step.dart` | 181 | Core · `Core/Models/` | `StepType` (enum con nombres persistidos `warmup`/`work`/`rest`/`cooldown`), `IntervalStep` (record + `CopyWith` con "null = conservar"), `StepLabels.Duration`/`StepLabels.Targets` | 1 | `StepType.label` en español no se migra (DC-32). Emoji 📈 según Q-10. Etiquetas verificadas con el oráculo Dart |
| `core/models/routine.dart` | 138 | Core · `Core/Models/` | `Routine`, `StepPosition` (readonly struct `GroupId, Rep, RepCount`), `RoutineExpansion.Flatten` / `.Positions`, `RoutineSummary` | 1 | Resumen "{n} series · {m} descansos" / "{n} pasos" sale del catálogo (Q-01, plurales de Q-02); `created_at` en UTC (Q-18) |
| `core/models/workout_session.dart` | 183 | Core · `Core/Models/` | `WorkoutSession`, `SessionStats` (`Compute`, P99 idéntico, Q-14), `DataPoint` | 1 | `durationFormatted`/`splitFormatted` en `Fmt`. Índice P99 = `floor((n−1)·0,99)`; `.round()` → `DartMath.Round : long`. `toUtc().toIso8601String()` → `IsoDate.Format` |

### 1.5 Strava

| Archivo legacy | Líneas | Assembly / ruta destino | Clase(s) Unity | It. | Notas |
|---|---|---|---|---|---|
| `core/strava/strava_config.dart` | 22 | Core · `Core/Strava/` + Boot · `Bootstrap/` | Core: `StravaConfig` (URLs, scopes `activity:read_all,activity:write`, `redirect_uri` por plataforma, `IsConfigured(id, secret)` con marcadores de Q-84). Boot: `StravaCredentials` (`static partial`) | 12 | La regla de configurado es pura y testeable |
| `core/strava/strava_secrets.dart` | 4 | — (**no se lee**) | Reemplazo: `Bootstrap/Generated/StravaSecrets.g.cs` generado al compilar desde `STRAVA_CLIENT_ID`/`STRAVA_CLIENT_SECRET` (carpeta `Generated/` y `**/StravaSecrets*.cs` en `.gitignore`, chequeo del CI) | 12 | R-02. Nadie abre ni copia el archivo legacy |
| `core/strava/strava_secrets.dart.example` | 4 | `tools/strava/` | `strava.local.example.json` (plantilla propia con valores vacíos; `strava.local.json` ignorado) | 12 | No hace falta leer el legacy para escribirla |
| `core/strava/strava_auth_service.dart` | 182 | Infra · `Infrastructure/Strava/` + Core + Platform | Infra: `StravaAuthClient` (intercambio, renovación, logout, datos del atleta). Core: `StravaTokenPolicy` (vence en `expires_at − 60 s`), `ISecureStore`. Platform: `IOAuthBrowser` → `AndroidDeepLinkAuth`, `LoopbackOAuthBrowser` (Windows, Editor), `AppleWebAuthSession` (iOS/macOS); `ISecureStore` → `AndroidKeystoreStore`, `WindowsDpapiStore`, `AppleKeychainStore` | 12 (Android, Windows, Editor), 10b (código nativo de Apple) | Mismas 5 claves de almacenamiento seguro. Se agrega `state` anti-CSRF. Errores al catálogo (Q-01) |
| `core/strava/strava_api_service.dart` | 303 | Infra · `Infrastructure/Strava/` + Core | Infra: `StravaApiClient` (subida multipart, sondeo de estado, actividades, streams, vueltas), DTOs `StravaActivity`, `StravaStreams`, `StravaActivityDetails`, `StravaLap` (lectura con `JToken`). Core: `UploadStatusParser` (regex `duplicate of activity (\d+)`) | 12 (~200: subida, duplicados, bloqueo en simulador), 13 (~103: actividades, streams, vueltas) | `external_id` nuevo `rowmate-u_{installId}_{id}` (R-11). Corte antes de cualquier request en simulador (`:81`) |
| `core/strava/strava_metadata.dart` | 205 | Core · `Core/Strava/` | `StravaMetadata` (escritura y lectura), `IntervalMetadata` | 12 (escritura, Q-72), 13 (lectura, Q-73) | Carga ≤ 200, comentario ≤ 217 (DC-53) |
| `core/strava/tcx_builder.dart` | 189 | Core · `Core/Strava/` | `TcxBuilder`, `TcxLap` (interna) | 12 | Salida idéntica: desfases, vueltas por paso, calorías, omisión de ceros, escape XML |

---

## 2. `lib/` — pantallas (`features/`)

### 2.1 Dispositivo

| Archivo legacy | Líneas | Assembly / ruta destino | Clase(s) Unity | It. | Notas |
|---|---|---|---|---|---|
| `features/device/device_provider.dart` | 141 | App · `App/Device/` + Core | App: `DeviceStore` (estado `DeviceState`: búsqueda, lista, conexión, errores, diagnóstico). Core: `RowerWatchdog` (≥ 5 s sin paquetes) | 2 (~80: vista conectada y estado vacío), 10a (~60: watchdog y diagnóstico) | "Error al escanear/conectar" al catálogo (Q-01, Q-42) |
| `features/device/device_screen.dart` | 480 | UI · `Presentation/Screens/Device/` | `DeviceScreen` (UXML), `ConnectedView`, `LiveMetricsGrid` (2/3 columnas), `MetricTile` (con `TabularNumber`), `ScanView`, `BluetoothStateView` (apagado / sin permiso), `BleDebugPanel` | 2 (~250: conectada y vacío), 10a (~230: búsqueda, Bluetooth apagado, diagnóstico) | Gesto extra del diagnóstico (Q-37). "Rower"/"Unknown device" al catálogo |
| `features/device/heart_rate_provider.dart` | 117 | App · `App/Device/` | `HeartRateStore`, `HrmCardState` (record) | 2 | Puerto de `heart_rate_provider_test` en `dotnet` |
| `features/device/heart_rate_card.dart` | 238 | UI · `Presentation/Screens/Device/` (vista) + App (reglas) | UI: `HeartRateCard`. App: `HrmCardPresenter` (qué texto y qué botones según el estado) | 2 | Corazón que late a la frecuencia real (Designer); pide 60 fps mientras es visible (ARCHITECTURE §13.1) |

### 2.2 Entrenamiento

| Archivo legacy | Líneas | Assembly / ruta destino | Clase(s) Unity | It. | Notas |
|---|---|---|---|---|---|
| `features/workout/workout_provider.dart` | 328 | Core · `Core/Workout/` + App · `App/Workout/` | Core: `WorkoutEngine` (fases, reloj activo de 1 s **por tiempo monótono con alcance**, avance de pasos, fin automático, fusión del pulso, telemetría cada 5 s, guardado incremental), `WorkoutPhase`, `StepProgress`. App: `WorkoutStore` (estado para la UI) | 4a | Llama a `SeriesTracker` antes de `_checkStepCompletion` (It. 5). Saneamiento de saltos de distancia también en pasos por distancia (Q-55). Segundo plano según Q-91 (ARCHITECTURE §6.1) |
| `features/workout/step_target_tracker.dart` | 71 | Core · `Core/Workout/` | `StepTargetTracker`, `StepResult`, `StepAccumulator` (interna) | 4a | — |
| `features/workout/series_tracker.dart` | 183 | Core · `Core/Workout/` | `SeriesTracker`, `Lap`, `RepSummary` | 5 | Interpolación de tiempo, pausas excluidas, `Rebase()` al reanudar, saltos negativos o > 10 m/s ignorados |
| `features/workout/step_pill.dart` | 105 | Core · `Core/Workout/` (estado) + UI · `Presentation/Controls/` (vista) | Core: `StepPillState`, `StepPillStateResolver`. UI: `StepPillElement` | 4b | Mismo ancho para todas (Q-59); pulso en los últimos 5 s (Designer) |
| `features/workout/series_panels.dart` | 392 | Core · `Core/Workout/` (formatos) + App (preferencias) + UI | Core: `WorkoutFormats.LapTime`, `.Split`, `.Meters`. App: `SeriesPanelsState` (`immersive.lapsExpanded`, `immersive.repsExpanded`, colapsados por defecto). UI: `WallClock`, `SeriesPanels`, `CollapsiblePanel`, `LapsContent`, `RepsContent`, `MetricRow` | 4a (formatos), 4b (`WallClock`, F-WKT-19), 5 (paneles) | Formatos `2:46.7`, `1:05.0`, `1:54`, `1.245 m` (`toStringAsFixed(1)` y `.round()` por ADR 012; vectores con `flutter test` aparte en la 4a) |
| `features/workout/immersive_workout_screen.dart` | 1 019 | UI · `Presentation/Screens/Workout/` + App | UI: `ImmersiveWorkoutPage` (capas, `ISceneHost.Enter/Exit`, `ISystemUi.SetImmersive`), `StageTimelineBar`, `FreeWorkoutTopBar`, `PhaseChip`, `ImmersiveHud`, `GlassMetricCard`, `CenterMetric`, `ImmersiveControls`, `FinishConfirmDialog`, `RoutineCompletedDialog`, `CompletionStats`. App: `HudModel` (valores, colores semánticos, semáforo) | 4a (~350: estructura mínima, tarjetas, banda, controles y diálogo de terminar), 4b (~500: barra de pasos, barra libre, reloj, diálogo de completado, transiciones), 5 (~170: integración de paneles), 6a (capa de escena real), 6b (vidrio con desenfoque), 12 (disparadores de subida a Strava) | Textos fijos al catálogo (Q-01). Al terminar a mano nunca aparece el diálogo de completado (Q-53). Valor blanco/rojo con etiqueta y borde del color de la métrica (Q-60, D-ART-01) |
| `features/workout/workout_screen.dart` | 988 | UI · `Presentation/Screens/Workout/` | `WorkoutScreen`, `IdleView` (aviso sin remo, libre, rutinas, sección "Escenario" en It. 8), `ActiveView` (marcador mientras el inmersivo está arriba) | 4a (~200 vivas), 8 (sección "Escenario") | Líneas 203-990 (`_FullscreenWorkoutPage`, `_CompactTopBar`, `_CompactStepBanner`, `_FullscreenMetrics`, `_FullMetric`, `_CompactControls`, `_CompletionStat`): **descarte** (F-WKT-23, Q-57) |

### 2.3 Escena inmersiva (`features/workout/scene/`)

| Archivo legacy | Líneas | Assembly / ruta destino | Clase(s) Unity | It. | Notas |
|---|---|---|---|---|---|
| `features/workout/scene/scene_camera.dart` | 46 | Core · `Core/Scene/` + Scene | Core: `SceneCamera` (2,2 m de alto, remero a z = 9,5, horizonte 40 %/42 %), `ChaseCameraMotion` (respiración). Scene: `ChaseCameraRig` | 6a | "Respiración" apagada con reducir movimiento (ARCHITECTURE §13.2) |
| `features/workout/scene/scene_state.dart` | 87 | Core · `Core/Scene/` | `SceneState`, `Puddle` | 6a (~50: velocidad, distancia y `ShaderDistance`), 7 (~37: fase de palada y charcos) | `500/split`, τ = 1,5 s, exige spm > 0, cierre a 12 spm, dt ≤ 0,1 s (DC-39). `ShaderDistance` (`% 4096`, `:34`) se porta como contrato y el agua nueva no lo usa (QA-P-28). No se toca para la fase visual (D-ANI-01) |
| `features/workout/scene/stroke_cycle.dart` | 51 | Core · `Core/Scene/` + `Core/Scene/Stroke/` | `StrokeCycle` (fase canónica, drive 0-0,40); `StrokeRhythm` (ritmo por SPM, Q-70), `VisualStrokeClock` (fase visual, D-ANI-01) | 7 | Los tests del legacy siguen sobre la fase canónica |
| `features/workout/scene/time_of_day.dart` | 117 | Core · `Core/Scene/` | `SceneTimeOfDay` (paleta legacy sin cambios), `ScenePalette`, `SceneLight`; nuevos `HourLighting`, `CelestialPlacement` (dos direcciones de sol, D-ART-05), `MoonPhase` | 6a | Hora real o forzada por el simulador; transiciones graduales (Q-62, Q-63) |
| `features/workout/scene/environment.dart` | 129 | Core · `Core/Scene/` + Scene | Core: `EnvironmentId`, `PropKind`, `ShoreProp`, `Environment` (abstracta: `PropsForSegment`, `VisibleProps`, topes, caché **por instancia** en lugar del mapa estático), `DartRandom`, `SlotIds`. Scene: `HorizonSilhouette` (reemplaza `paintHorizon`), `HorizonTint` (`horizonColor`) | 6a (base y lago), 8 (topes y siluetas de los demás) | Semilla `id·1000003 + k·7919 + 17` con `DartRandom` (paisaje idéntico, Q-61); vectores del oráculo en la 6a |
| `features/workout/scene/environments/lake_environment.dart` | 86 | Core · `Core/Scene/Environments/` | `LakeEnvironment` | 6a | Muelles y cabañas en metros exactos |
| `features/workout/scene/environments/river_environment.dart` | 72 | Core · `Core/Scene/Environments/` | `RiverEnvironment` | 8 | Puentes `k % 16 == 5` |
| `features/workout/scene/environments/coast_environment.dart` | 89 | Core · `Core/Scene/Environments/` | `CoastEnvironment` | 8 | Playa cada 250 m, oleaje 0,8 |
| `features/workout/scene/environments/regatta_environment.dart` | 72 | Core · `Core/Scene/Environments/` | `RegattaEnvironment` | 8 | Carteles cada 250 m con distancia de escena (Q-64) |
| `features/workout/scene/scene_settings.dart` | 43 | App · `App/Scene/` | `SceneSettingsStore` (`scene.environment`, por defecto `lake`; `HourOverride` del simulador) | 6a (~15: hora forzada), 8 (~28: preferencia) | — |
| `features/workout/scene/scene_view.dart` | 125 | Scene · `Scene/` + App | Scene: `SceneDirector` (un `Update` por frame, sin asignaciones), `WaterQualitySelector`. App: `ISceneHost` | 6a (director y capa de fondo con el agua degradada), 6b (elección avanzada/degradada y composición final; líneas contadas en la 6b) | Reemplaza el `Ticker`; capa de fondo del inmersivo |
| `features/workout/scene/water_shader.dart` | 23 | Scene · `Scene/Water/` | `WaterMaterialBinder` (carga del shader y detección de soporte) | 6b | Si el shader no está soportado → agua degradada (R-04, `SubShader`/`Fallback`) |
| `features/workout/scene/environment_picker.dart` | 66 | UI · `Presentation/Screens/Workout/` + App | UI: `EnvironmentPicker` (4 miniaturas). App: `EnvironmentNames` (claves del catálogo) | 8 | Miniaturas **horneadas** por `EnvironmentThumbnailBaker`, a las 12:00 por defecto (QA-P-11, ARCHITECTURE §14.5) |
| `features/workout/scene/painters/sky_painter.dart` | 124 | Scene · `Scene/Sky/` + Core | Scene: `SkyController` (skybox procedural), `CloudLayer`, `StarField`, `SunMoonController`. Core: `SkyModel` (7 nubes, parallax, opacidades, estrellas desde ambiente ≤ 0,85) | 6a | Las 80 estrellas usan `DartRandom(42)` (`:59`, titileo `0,5+0,5·sin(2t+i)`); las 7 nubes son **datos fijos** (`_cloudData`, `:29`) con deriva `0,0004·dist + 0,004·t` (`:95`). La función legacy queda en `SkyModel` para su test; la vista aplica D-ART-06 (sin deriva por distancia) si el PO la confirma |
| `features/workout/scene/painters/water_painter.dart` | 108 | Scene · `Scene/Water/` + Core | Scene: `WaterSurface` (malla y material), `WaterFallbackRenderer` (degradé de 3 tramos), `WaterInteractionRenderer` (textura RGBA8 256×200 sin estado). Core: `WaterParams` (luz, popa, oleaje, fases y desplazamientos de normal en `double`) | 6a (~40: respaldo degradado), 6b (~68: agua avanzada), 8 (oleaje de la costa) | Sin salto cada 4096 m (Q-68) |
| `features/workout/scene/painters/shore_painter.dart` | 289 | Scene · `Scene/Environments/` + Core | Scene: `ShoreStreamer` (pool por `PropKind`), `PropCatalog` (slots `PropSlot`), `DistanceMarkerLabel`. Core: `PropVariant` (variante por `seed % n`), `PropCulling` (detrás de la cámara se omite) | 6a (~150: lago), 8 (~139: resto del catálogo) | Puente con tablero 6-7,7 m y arco 17 m (DC-42) |
| `features/workout/scene/painters/boat_painter.dart` | 268 | Scene · `Scene/Boat/` + Core · `Core/Scene/Stroke/` | Scene: `BoatVisual` (pivote de cabeceo con casco, remero y remos, D-ANI-03), `RowerRigDriver` (IK, Animation Rigging), `OarRig`, `StrokeVfx`, `WaterInteractionRenderer` (charcos), `BoatShadow`. Core: `LegacyBoatGeometry` (geometría exagerada del legacy, solo para `boat_painter_test`), `BoatSpec` (medidas reales, D-ART-04), `ScullingKinematics`, `PuddleField` (charcos en `BladeExit`, D-ANI-02), `BoatSurge`, `BoatMotion` | 7 | Slots `RowerCharacterSlot`, `BoatPartSlot`, `OarSlot` con respaldo procedural. Nombres unificados con ANIMATION_BIBLE §7 (antes `BoatKinematics`, `OarController`, `PuddleDecals`) |
| `features/workout/scene/painters/thumbnail_painter.dart` | 44 | Editor + Scene | Editor: `EnvironmentThumbnailBaker`. Scene: `ThumbnailCamera` | 8 | Todos los objetos a < 150 m (DC-42). Horneado, nunca en ejecución |

### 2.4 Rutinas

| Archivo legacy | Líneas | Assembly / ruta destino | Clase(s) Unity | It. | Notas |
|---|---|---|---|---|---|
| `features/routines/routines_provider.dart` | 53 | App · `App/Routines/` | `RoutinesStore` | 3 | Doble toque en Guardar = una rutina (Q-19) |
| `features/routines/routines_screen.dart` | 120 | UI · `Presentation/Screens/Routines/` | `RoutinesScreen`, `RoutineCard`, menú editar/borrar con confirmación | 3 | — |
| `features/routines/routine_editor_screen.dart` | 739 | UI · `Presentation/Screens/Routines/` + App | UI: `RoutineEditorPage`, `StepDialog`, `EditorItemList` (paso suelto / cabecera de serie / paso de serie / pie). App: `RoutineEditorModel` (aplanado de ítems, interpretación de campos "abc"/"0"/"−2"/"5", serie por defecto Trabajo 120 s + Descanso 60 s ×3) | 3 | Lógica del editor testeable en `dotnet` (casos 16-21 de RUT §5.3). Sin validaciones nuevas (Q-25) |

### 2.5 Historial

| Archivo legacy | Líneas | Assembly / ruta destino | Clase(s) Unity | It. | Notas |
|---|---|---|---|---|---|
| `features/history/history_provider.dart` | 35 | App · `App/History/` | `HistoryStore` | 9 | Recarga sin parpadeo y también al terminar una sesión (Q-22) |
| `features/history/history_screen.dart` | 159 | UI · `Presentation/Screens/History/` | `HistoryScreen`, `SessionCard`, `StatChip` | 9 (ícono de Strava en 12) | Fecha `dd/MM/yyyy  HH:mm`, pulso `{prom}/{máx}` |
| `features/history/session_detail_screen.dart` | 581 | UI + App + Core | UI: `SessionDetailPage`, `MetricChartElement` (Painter2D), `StepPerformanceList`, `MetricSelector`. App: `SessionDetailModel`. Core: `ChartGeometry` (escala, split invertido, Q-24), `StepBands`, `TargetLines`, `StepPerformance` (duración real del paso, Q-23) | 9 (~520), 12 (~60: botón de subida, oculto en simulador Q-78) | Objetivos con la rutina actual (Q-16). Ejes de 11 px (D-ART-03) |

### 2.6 Perfil (Strava)

| Archivo legacy | Líneas | Assembly / ruta destino | Clase(s) Unity | It. | Notas |
|---|---|---|---|---|---|
| `features/profile/profile_provider.dart` | 431 | App · `App/Profile/` + Core | App: `ProfileStore`. Core: `UploadPreference` (`auto`/`ask`/`manual`, `strava_upload_pref`, por defecto `auto`), `SessionRepair` (F-HIS-21), `ImportedTelemetryBuilder` (ritmo, suavizado ventana 15, vueltas, tipo de paso; F-STR-13) | 12 (~120: conexión, preferencia, subida), 13 (~311: sincronización en ambos sentidos, pendientes, limpieza) | Importadas con `origin = 'strava'`; calorías de Strava conservadas (Q-75) |
| `features/profile/profile_screen.dart` | 467 | UI · `Presentation/Screens/Profile/` | `ProfileScreen`, `StravaConnectionCard`, `UploadSettingsCard`, `PrefTile`, `SyncCard`, `PendingUploadsCard`, `PendingSessionTile`, `MaintenanceCard` | 12 (~220), 13 (~247) | "Mantenimiento" y su diálogo al catálogo; borra solo importadas (Q-71) |

---

## 3. `lib/` — shell, tema, textos y compartidos

| Archivo legacy | Líneas | Assembly / ruta destino | Clase(s) Unity | It. | Notas |
|---|---|---|---|---|---|
| `main.dart` | 181 | Boot + App + UI + Platform | Boot: `AppRoot` (MonoBehaviour), `CompositionRoot`, `PlatformFactory`. App: `AppComposer`, `AppContext`, `AppCapabilities`. UI: `AppShellController` (`MainShell`: 4/5 pestañas, `IndexedStack`), `TabBar`, `FramePacing`. Platform: `ISystemLocale` (lista de idiomas preferidos) | 2 (shell, servicios e idioma), 4a (wakelock al arrancar, `IWakeLock`), 10b (idiomas y wakelock en Apple), 12 (quinta pestaña) | `sqfliteFfiInit` no aplica. `autoConnect` del pulsómetro al arrancar (It. 11). `supportedLocales: [en, es]` (`:103-105`): Flutter resuelve sobre la **lista** del sistema con `basicLocaleListResolution` (`widgets/app.dart:146-238`) → `LocaleResolver` con lista y el mismo algoritmo (QA-P-20, D-06; F-APP-09 en la It. 1) |
| `shared/theme.dart` | 69 | Core · `Core/Design/` + `Core/Models/` + UI · `Presentation/Styles/` | Core: **`Tokens`** (única fuente, ADR 016), `StepTypeInfo` (color e ícono por tipo: work `#EF476F`, rest `#06D6A0`, warmup `#FFD166`, cooldown `#118AB2`) y `MetricPalette` (contrato F-APP-06, pasa a la It. 2 por D-09), que leen de `Tokens`. UI: `RowMateTheme.tss` y `Tokens.uss` **generado** por `tools/tokens` | 1 (~30: tipos de paso y `Tokens`), 2 (tema, paleta, tipografía) | Base `#0D1B2A`, superficie `#1A2E45`, semilla `#0077B6`. Colores de barra sin efecto no se migran (DC-58). Test de contraste WCAG en `dotnet` (QA-P-05) |
| `shared/widgets/metric_card.dart` | 68 | — | **Descarte** (F-APP-08) | 2 (cierre) | Sin uso en el legacy |
| `shared/widgets/interval_tile.dart` | 76 | — | **Descarte** (F-APP-08 + F-RUT-19) | 2 (cierre) | Sin uso en el legacy |
| `l10n/app_es.arb` | 208 | `Localization/strings.es.json` (datos) | Consumido por `Core.Localization.Localizer` | 1 | Mismas 159 claves y placeholders; 4 cambian por Q-02 y se suman 15 nuevas (Q-01), 174 en total |
| `l10n/app_en.arb` | 208 | `Localization/strings.en.json` (datos) | Ídem | 1 | `en` es el idioma de respaldo |
| `l10n/app_localizations.dart` | 1 087 | Core · `Core/Localization/` | `Localizer`, `MessageFormatter` (ICU mínimo: placeholders y plurales), `LocaleResolver` (lista de idiomas preferidos), `L10nKeys` (constantes de las claves, mantenidas a mano con un test de sincronía con los catálogos; D-05 de la It. 1) | 1 | Código generado por Flutter: no se porta línea a línea |
| `l10n/app_localizations_en.dart` | 511 | — | No se porta (los textos están en el JSON) | 1 | Generado |
| `l10n/app_localizations_es.dart` | 516 | — | No se porta (los textos están en el JSON) | 1 | Generado |

---

## 4. `shaders/`

| Archivo legacy | Líneas | Assembly / ruta destino | Clase(s) Unity | It. | Notas |
|---|---|---|---|---|---|
| `shaders/water.frag` | 98 | `Art/Shaders/Water/` (sin código; parámetros desde `Core.Scene.WaterParams`) | `RowMateWater.shader` (HLSL URP: ondas con fases en `double` acotadas en CPU, normales, reflejo del cielo y la orilla, brillo, estela, niebla) con `SubShader`/`Fallback` degradado (`RowMateWaterFallback`, degradé de 3 tramos) | 6a (degradado), 6b (avanzado), 8 (oleaje de la costa) | Lo escribe el Designer con el contrato de `WaterParams`. Sin bucles por píxel; ≤ 70 ALU / 5 tex en Medio (ARCHITECTURE §20) |

---

## 5. `test/` → tests NUnit

"EM" = `Assets/RowMate/Tests/EditMode/` (compilado también por `tools/dotnet`). "Sim" = `Tests/Simulator/` (también en `dotnet`, con `ROWMATE_SIMULATOR`). "PM" = `Tests/PlayMode/` (solo Unity, requiere Editor). Los *fakes* viven en `Tests/Support/` (asmdef `RowMate.Tests.Support`). Las líneas de cada archivo coinciden con `wc -l` del legacy (2 136 en total con `widget_test`).

| Test legacy | Líneas | Casos | Destino | Clase(s) de test | It. | Notas |
|---|---|---|---|---|---|---|
| `test/rowing_simulator_test.dart` | 76 | 7 | Sim · `Simulator/` | `RowingSimulatorTests` | 1 | `noise: false` → `IRandom` determinista; vectores del oráculo |
| `test/heart_rate_parser_test.dart` | 39 | 6 | EM · `Core/Ble/` | `HeartRateParserTests` | 1 | — |
| `test/scan_filter_test.dart` | 36 | 2 | EM · `Core/Ble/` | `ScanFilterTests` | 1 | `clientSide` inyectado; `Guid('180d')` contra el UUID de 128 bits con `BleUuid` |
| `test/routine_positions_test.dart` | 28 | 1 | EM · `Core/Models/` | `RoutinePositionsTests` | 1 | — |
| `test/session_stats_test.dart` | 24 | 3 | EM · `Core/Models/` | `SessionStatsTests` | 1 | P99 idéntico (Q-14) |
| `test/simulated_ble_service_test.dart` | 84 | 3 | Sim · `Simulator/` | `SimulatedRowerServiceTests` | 2 | `FakeScheduler` (de `Tests.Support`) en lugar de `fake_async` |
| `test/simulated_heart_rate_service_test.dart` | 95 | 3 | Sim · `Simulator/` | `SimulatedHeartRateServiceTests` | 2 | — |
| `test/simulator_overlay_test.dart` | 124 | 2 | Sim + PM | Sim: `SimulatorPanelModelTests` (150 → 160 W, 24 → 25 spm, "Hora real", Fuerte = 280 W/30 spm, "Remando", caída del pulsómetro). PM: `SimulatorOverlayUiTests` (capa por encima de la navegación, 800×360) | 2 | La parte de reglas corre en `dotnet` |
| `test/heart_rate_card_test.dart` | 182 | 4 | EM + PM | EM: `HrmCardPresenterTests` (textos y botones por estado, exclusión de búsquedas). PM: `HeartRateCardUiTests` (textos en español, landscape 640×360 sin desbordes) | 2 (los 4 casos, con servicios simulados) | :121, :142 y :161 se **vuelven a correr** en la 10a contra el remo real con adaptador falso (QA-P-27) |
| `test/heart_rate_provider_test.dart` | 127 | 3 | EM · `App/Device/` | `HeartRateStoreTests` | 2 | — |
| `test/workout_provider_hr_test.dart` | 181 | 4 | EM · `Core/Workout/` | `WorkoutEngineHeartRateTests` | 4a | Fusión del pulso (DC-22) |
| `test/step_target_tracker_test.dart` | 74 | 6 | EM · `Core/Workout/` | `StepTargetTrackerTests` | 4a | — |
| `test/step_pill_test.dart` | 54 | 1 | EM + PM | EM: `StepPillStateResolverTests`. PM: `StepPillElementTests` | 4b | — |
| `test/series_panels_test.dart` | 74 | 4 | EM + PM | EM: `WorkoutFormatsTests` (caso "formatos"), `SeriesPanelsStateTests` (colapsados por defecto, preferencia guardada, sin serie no hay panel de repeticiones). PM: `SeriesPanelsUiTests` | 4a ("formatos"), 5 (los otros 3) | — |
| `test/series_tracker_test.dart` | 221 | 12 | EM · `Core/Workout/` | `SeriesTrackerTests` | 5 | — |
| `test/scene/scene_camera_test.dart` | 43 | 4 | EM · `Core/Scene/` | `SceneCameraTests` | 6a | — |
| `test/scene/scene_state_test.dart` | 80 | 7 | EM · `Core/Scene/` | `SceneStateTests` | 6a (:14, :26, :55), 7 (:37, :46, :60, :69) | `:55` (`shaderDistance` mód. 4096) se porta como contrato aunque el agua nueva no lo use (QA-P-28) |
| `test/scene/time_of_day_test.dart` | 48 | 6 | EM · `Core/Scene/` | `SceneTimeOfDayTests` | 6a | — |
| `test/scene/environment_test.dart` | 67 | 3 × 4 + 3 | EM · `Core/Scene/` | `EnvironmentTests` (parametrizado por `EnvironmentId`) | 6a (genéricos y lago), 8 (río, costa, regata) | Determinismo con `DartRandom` |
| `test/scene/sky_shore_painter_test.dart` | 84 | 16 + 2 | EM + PM | EM: `SkyModelTests` (estrellas desde las 19 h), `PropCullingTests` (objetos detrás de la cámara se omiten; puente encima no rompe). PM: `SceneRenderSmokeTests` (escenarios × tamaños × horas dibujan sin excepciones) | 6a (cielo y lago), 8 (resto) | Los "pinta sin excepciones" pasan a smoke tests de escena |
| `test/scene/water_painter_test.dart` | 48 | 2 | EM + PM | EM: `WaterParamsTests` (28 valores en el orden del shader → contrato de `WaterParams`). PM: `WaterFallbackSmokeTests` | 6a (:11, respaldo), 8 (:27, oleaje 0,8) | `uSunX` centrado al mediodía (acimut 0, compatible con D-ART-05) |
| `test/scene/scene_view_test.dart` | 38 | 16 | PM · `Scene/` (+ EM si hay regla pura) | `SceneDirectorTests` | 6a (los 4 del lago, que se vuelven a correr en la 6b), 8 (los 12 de río, costa y regata) | Parametrizado: **4 escenarios × 2 tamaños (390×844, 844×390) × 2 horas (12 h, 23 h)** (`scene_view_test.dart:8-10`), "anima sin excepciones" con `spm 24`, `pace 110`, `300 m` |
| `test/scene/stroke_cycle_test.dart` | 36 | 4 | EM · `Core/Scene/` | `StrokeCycleTests` | 7 | Fase canónica |
| `test/scene/boat_painter_test.dart` | 45 | 1 + 2 | EM + PM | EM: `LegacyBoatGeometryTests` (geometría de la palada del legacy). PM: `BoatRigSmokeTests` (2 tamaños, todas las fases) | 7 | Las medidas reales (D-ART-04) tienen tests propios (`ScullingKinematicsTests`, ANIMATION_BIBLE §7.5) |
| `test/scene/environment_picker_test.dart` | 44 | 2 | EM + PM | EM: `SceneSettingsStoreTests` (arranca con el guardado; tocar persiste `river`). PM: `EnvironmentPickerUiTests` ("Lago alpino" en español) | 8 | — |
| `test/reconnect_loop_test.dart` | 177 | 7 | EM · `Core/Ble/` | `ReconnectLoopTests` (6), `StaleWatchdogTests` (1) | 11 | — |
| `test/widget_test.dart` | 7 | 0 | — | No se porta (vacío, DC-16) | — | — |

---

## 6. Totales por iteración (archivos legacy con algo que portar)

| It. | Archivos que toca (total o en parte) |
|---|---|
| 1 | `ftms_parser`, `heart_rate_parser`, `scan_filter`, `rowing_simulator`, `dev_config`, `rowing_data`, `interval_step`, `routine`, `workout_session`, `theme` (tipos de paso → `Tokens`), `app_es.arb`, `app_en.arb`, `app_localizations*` + 5 tests (19 casos) + vectores del oráculo |
| 2 | `main`, `theme`, `device_provider`, `device_screen`, `heart_rate_card`, `heart_rate_provider`, `simulated_ble_service`, `simulated_heart_rate_service`, `simulator_overlay`, `ble_service` (interfaz), `heart_rate_service` (interfaz), `dev_config` (guardia), descartes de `shared/widgets` + 5 tests (15 casos) |
| S-1 | Ninguno nuevo (usa `heart_rate_parser` y `scan_filter` de la It. 1) |
| 3 | `routines_provider`, `routines_screen`, `routine_editor_screen`, `database_service` (rutinas y archivo) |
| 4a | `workout_provider`, `step_target_tracker`, `series_panels` (formatos), `immersive_workout_screen` (~350), `workout_screen` (~200 vivas), `database_service` (sesiones y puntos), `main` (wakelock) + 3 tests (11 casos) |
| 4b | `step_pill`, `series_panels` (`WallClock`), `immersive_workout_screen` (~500) + 1 test |
| 5 | `series_tracker`, `series_panels` (paneles), `immersive_workout_screen` (~170) + 2 tests (15 casos) |
| 6a | `scene_camera`, `scene_state` (velocidad, distancia, `ShaderDistance`), `time_of_day`, `environment`, `lake_environment`, `scene_settings` (hora forzada), `scene_view` (director), `sky_painter`, `water_painter` (respaldo), `shore_painter` (lago), `water.frag` (degradado), `immersive_workout_screen` (capa de escena) + 7 tests (parciales) |
| 6b | `water_painter` (avanzada), `water_shader`, `water.frag` (avanzado), `scene_view` (cierre), `immersive_workout_screen` (vidrio con desenfoque) + `scene_view_test` (4 del lago otra vez) |
| 7 | `scene_state` (palada y charcos), `stroke_cycle`, `boat_painter` + 3 tests |
| 8 | `river_environment`, `coast_environment`, `regatta_environment`, `environment`, `shore_painter`, `thumbnail_painter`, `environment_picker`, `scene_settings` (preferencia), `water_painter` y `water.frag` (oleaje), `workout_screen` (sección "Escenario") + 5 tests (parciales, incluidos los 12 de `scene_view_test`) |
| 9 | `history_provider`, `history_screen`, `session_detail_screen`, `database_service` (consultas) |
| 10a | `ble_service`, `device_screen`, `device_provider` + `AndroidManifest.xml` y plugin de Windows; vuelve a correr `heart_rate_card_test` :121, :142, :161 |
| 10b | Código nativo de Apple de `ble_service`, `heart_rate_service`, `strava_auth_service` (Keychain, `ASWebAuthenticationSession`), `main` (wakelock de macOS, idiomas, barra de estado); `Info.plist` y *entitlements* |
| 11 | `heart_rate_service`, `reconnect_loop` + 1 test (7 casos) |
| 12 | `strava_config`, `strava_auth_service`, `strava_api_service`, `strava_metadata`, `tcx_builder`, `profile_provider`, `profile_screen`, `session_detail_screen`, `immersive_workout_screen` (disparadores), `main` (quinta pestaña) |
| 13 | `profile_provider`, `profile_screen`, `strava_api_service`, `strava_metadata`, `database_service` (limpieza y pendientes) |
| 14 | Configuración de plataformas (`android/`, `ios/`, `macos/`, `windows/`, `.github/`, `codemagic.yaml`): identidad, íconos, arranque, CI; auditoría de textos |

---

## Cambios por QA (plan)

Correcciones por el [gate de armonía del plan](../QA_PLAN_REVIEW.md), 2026-10-27.

| ID | Qué cambió |
|---|---|
| Sincronía con el ROADMAP final | Columna "It." con las iteraciones 4a/4b, 6a/6b, 10a/10b y S-1 en todas las tablas, y §6 rehecha por iteración |
| QA-P-19 (E1) | `sky_painter`: `Random(42)` es de las **estrellas**; las nubes son datos fijos (`:29`, `:59`, `:95`) |
| QA-P-19 (E2) | `scene_view_test` = 4 escenarios × 2 tamaños × 2 horas, repartido 4 (6a, y otra vez en la 6b) / 12 (8) |
| QA-P-27 | `heart_rate_card_test`: los 4 casos en la It. 2; :121, :142 y :161 se vuelven a correr en la 10a |
| QA-P-05 | `shared/theme.dart` → `Core/Design/Tokens` como única fuente; `StepTypeInfo`/`MetricPalette` la consumen; `Tokens.uss` generado. Se elimina `ThemeColors` |
| QA-P-28 | `scene_state` y su test `:55`: `ShaderDistance` como contrato |
| QA-P-02 | Notas de `rowing_simulator` (`:60`), `workout_session` y `series_panels` con `DartMath`/`Fmt`/`IsoDate` |
| QA-P-20 | `main.dart` y `app_localizations`: `LocaleResolver` sobre la lista de idiomas preferidos |
| QA-P-11 | `environment_picker` y `thumbnail_painter`: miniaturas horneadas, 12:00 por defecto |
| QA-P-06 | `ble_service` y `workout_provider`: keepalive nativo y reloj por tiempo monótono en segundo plano |
| Pedidos del Designer / QA-P-29 (nombres) | `boat_painter`, `stroke_cycle`, `time_of_day`: `LegacyBoatGeometry`, `BoatSpec`, `VisualStrokeClock`, `PuddleField`, `OarRig`, `BoatVisual`, `CelestialPlacement`, `HourLighting`, `MoonPhase` |

## Cambios de la It. 1 (diseño técnico e implementación)

| ID | Qué cambió |
|---|---|
| D-05 | `l10n/app_{es,en}.arb` → catálogos con un script Dart único (`tools/l10n/arb_to_catalog.dart`, bajo la guardia de `tools/oracle/run-oracle.ps1 -L10n`), no con una consola `dotnet`; `L10nKeys` a mano con test de sincronía |
| D-09 | `shared/theme.dart`: `Icons.cs` (codepoints) y `MetricPalette` pasan a la It. 2; en la It. 1 el ícono del tipo de paso es su nombre legacy como dato (`StepTypeInfo.IconName`) |
| D-01 | `test/`: los vectores dorados que reemplazan la ejecución del legacy van a `Assets/RowMate/Tests/Golden/oracle/` |
