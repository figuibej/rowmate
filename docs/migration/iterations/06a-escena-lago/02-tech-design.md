# It. 6a — Escena: Core de escena, cielo, luz, hora, Lago alpino y agua degradada: diseño técnico

| Campo | Valor |
|---|---|
| Autor | unity-architect |
| Fecha | 2026-11-02 |
| Iteración | 6a · `06a-escena-lago` ([ROADMAP §It. 6a](../../ROADMAP.md)). **La primera iteración 3D**: el fondo provisorio del inmersivo pasa a ser una escena URP |
| Entrada | [01-po-spec](01-po-spec.md) (101 ID = 78 nuevos + 23 de iteraciones anteriores, corregida por el gate; LT-62 a LT-87; CAP-01 a -24, LEG-01 a -07, DEMO-01 a -08, VID-01 a -11; DV-52 a DV-62; P-01 a P-17); [00-deuda-5](00-deuda-5.md) (la deuda de la 5, saldada, y su §5 "para el diseño de la 6a"); [00-deuda-diseno](00-deuda-diseno.md) (la base de arte con números y los pedidos **DD6a-01 a DD6a-26**, P6a-01 a P6a-10); [02-tech-design de la 5](../05-series-parciales/02-tech-design.md) (el inmersivo, `ImmersiveWorkoutPage`, `HudWarmup`, el fondo provisorio, `FramePacingPolicy`, el arnés); [REPORT de la 5](../05-series-parciales/REPORT.md) §7 y §9; [ARCHITECTURE](../../architecture/ARCHITECTURE.md) §2, §3, §4, §7.2 a §7.4, §13, §14, §15, §20; ADR [012](../../architecture/adr/012-paridad-numerica-dart.md), [015](../../architecture/adr/015-escena-3d.md), [018](../../architecture/adr/018-niveles-de-calidad.md), [019](../../architecture/adr/019-canal-de-color-de-la-ui.md); [ART_BIBLE](../../design/ART_BIBLE.md) §5.2, §6, §7.1, §7.2, §7.7, §7.8, §8, §9, §10.1; [ANIMATION_BIBLE](../../design/ANIMATION_BIBLE.md) §2.11, §2.12, §5, §6, §7; [ASSET_SLOTS](../../design/ASSET_SLOTS.md) §0, §1.2 a §1.4, §5.2, §7, §8 |
| Legacy leído (solo lectura, `3ae0f3a`) | [rowmate-legacy](https://github.com/figuibej/rowmate-legacy), enteros: `lib/features/workout/scene/{scene_camera,scene_state,time_of_day,environment,scene_settings,water_shader,scene_view}.dart`, `scene/environments/lake_environment.dart`, `scene/painters/{sky_painter,water_painter}.dart`; `scene/painters/shore_painter.dart:1-140`; `immersive_workout_screen.dart:70-120`; `workout_screen.dart:36-50`; `workout_provider.dart:210-228`; `lib/core/dev/simulator_overlay.dart:135-152`; `test/scene/{scene_camera,scene_state,time_of_day,environment,sky_shore_painter,water_painter,scene_view}_test.dart`. No se leyó `lib/core/strava/strava_secrets.dart`. `git -C ../rowmate-legacy status --porcelain` vacío al empezar y al terminar |
| Código leído (este repo) | `Core/Common/{FrameClock,IClock,IRandom}.cs`, `Core/Design/{FramePacingPolicy,Rgb}.cs`, `Core/Workout/WorkoutSnapshot.cs`, `Core/Models/RowingData.cs`, `App/Scene/SceneSettings.cs`, `App/Composition/AppContext.cs`, `Simulator/Panel/{SimulatorPanelStore,SimulatorPanelState}.cs`, `Presentation/Workout/{ImmersiveWorkoutPage,HudBackdrop,HudView,HudWarmup,PillRowView}.cs`, `Presentation/Controls/BackdropTexture.cs`, `Presentation/Motion/FramePacer.cs`, `Presentation/Surface/UiSurface.cs`, `Scene/AssemblyInfo.cs`, los asmdef, `Snapshots/{SnapshotRunner,VideoRecorder}.cs`, `Tests/UiSupport/{UiTestHost,RtReader,DeviceProfile}.cs`, `Tests/EditMode/Traceability/It05Ids.cs`, `Tests/PlayMode/HudBudgetTests.cs`, `tools/oracle/README.md`, `tools/unity/capture-snapshots.ps1`, `Packages/manifest.json` |
| Verificación previa | Un programa propio de .NET 8 en `<scratch>/it6a/arch/calc` (sin red) que barre la hora minuto a minuto y la respiración 10 min con 200 muestras por segundo. Da dos contradicciones de la spec que van al gate (§8.1, DT6a-01 y DT6a-02): con el rolido de 0,18° el horizonte se aparta **3,267 dp** en W1280 (tope 3; con 0,15°, 2,932), y el astro se corre hasta **0,331°/min** a las 20:09 con el arco de 17° (0,257 con el de 12° del Designer), contra el tope de 0,2°/min de AC-SCN-10-05 (las dos quedaron resueltas en el gate: 0,15° y 0,3°/min con el arco de 10,5°). Confirma además los astros de la spec de entonces (22 h: −5,83°, 14,92° con el arco de 15,5°; 6 h: −17,5°, 1,5°) y que en el cambio de las 19:34:18 y las 4:25:41 los dos astros están a −2,5° |
| Estado | **Diseño corregido por el gate de armonía de QA** ([03-qa-harmony-paridad](03-qa-harmony-paridad.md), [03-qa-harmony-tecnica](03-qa-harmony-tecnica.md); "Cambios por gate QA" al final). Integra las decisiones de [04-design-notes](04-design-notes.md) (DN6a-01 a -24) y los pedidos de [00-deuda-diseno](00-deuda-diseno.md) §14 (DD6a-01 a -26) con **un valor por constante** (§9, que se aplica antes de WP-A), alineado con la spec corregida. Sin código. Orden, **en serie**: **WP-A** (Core puro y oráculo, dotnet) → **WP-B0** (sondas técnicas, ajustes del proyecto y esqueleto) → **WP-D1** (shaders, perfiles de arte y motion) → **WP-B1** (la escena, materiales y precalentado) → **WP-B2** (integración con el inmersivo, el simulador y los niveles) → **WP-B3** (arnés `it6a`) → **WP-D2** (pulido con las tomas y biblias). Los 19 puntos C-01 a C-19 quedaron decididos en el gate (§8.2) |

Convenciones: las de la 5. Rutas relativas a `Assets/RowMate/` salvo que empiecen con `tools/`, `docs/`, `Packages/` o `ProjectSettings/`. Marcas: **N** nuevo, **C** cambia, **G** generado. Dueño entre corchetes: **[A]**, **[B0]** a **[B3]**, **[D1]**, **[D2]** (§7). `dp` = px de USS; perfiles **V390** 390 × 844, **H844** 844 × 390 (los del legacy, densidad 1), **TV** 411 × 914, **P360** 360 × 800, **TH** 914 × 411, **L640** 640 × 360, **W1280** 1280 × 720. Mundo como el legacy: `x` lateral (+ derecha), `y` altura sobre el agua, `z` hacia adelante desde la cámara, en metros; `d` = distancia de escena; `relZ = z de mundo − d`. Las citas `lib/...:línea` y `test/...:línea` son del legacy; `scene/` = `lib/features/workout/scene/`.

---

## 0. Resumen

1. **Toda regla de la escena es Core puro** (Q-69, AC-SCN-01-14): `Core/Scene/` porta `scene_camera.dart`, la parte de velocidad y distancia de `scene_state.dart`, `time_of_day.dart`, `environment.dart`, `lake_environment.dart`, las fórmulas de `sky_painter.dart`, `water_painter.dart` (respaldo) y `shore_painter.dart` (luz, niebla, noche), y suma lo nuevo de la spec y del Designer: la proyección 4 × 4 de la cámara del legacy, la respiración, el vuelo, la salida y la toma aérea, el astro que se ve (D-ART-05), la puesta y la salida graduales (Q-63), la fase de la luna, la luz de cada hora, las luces de la noche de a una, la transición de `Hora escena`, el terreno, la colocación sin flotar y el relleno determinista. Un agregado por sesión, **`SceneSession`**, avanza todo en un `Tick` sin asignaciones y deja un **`SceneFrame`** con lo que hay que dibujar en ese cuadro. Unity solo lo aplica.
2. **`DartRandom` es el `math.Random` de la VM de Dart** (Q-61, ADR 012): los cuatro vectores dorados y, con el oráculo, los segmentos 0 a 2 000 del lago **bit a bit** (por resumen de 64 bits de cada segmento y filas completas de los segmentos 0 a 99), las 80 estrellas de `Random(42)` en los siete tamaños y las listas visibles a 0, 1, 49,5, 50, 920, 1 234, 5 000 y 42 195 m.
3. **El oráculo de la escena corre el código del legacy con Flutter** en el proyecto `tools/oracle/engine/` (ya existe desde la 4a), con `run-oracle.ps1 -Scene` dentro de la guardia del legacy: vectores (`scene_*.json`) y las referencias **LEG-01 a LEG-07** dibujadas con el `SkyPainter`, el respaldo del `WaterPainter` y el `ShorePainter` del legacy, con y sin marcas.
4. **La escena se dibuja con un pedido de render explícito y entra al panel de la UI como fondo de un elemento** (DT6a-05). La cámara 3D no se dibuja sola nunca: en cada cuadro del inmersivo, `SceneHost.Tick` avanza la sesión y pide el render (`RenderPipeline.SubmitRenderRequest`) a una RT sRGB del tamaño de la ventana por la resolución del nivel (§4.2); el elemento **`scene-surface`** (el primero del HUD) la muestra. Así: la composición del vidrio sobre la escena es **sRGB, como el legacy** (ADR 019: `#070D16 @86 %` sobre blanco sigue dando `#2A2F37`, y el contraste de AC-WKT-17-13 no cambia), el arnés de capturas lee la misma RT de la UI de siempre, el orden de los cuadros es determinista en el Editor y en el *player*, y fuera del inmersivo no se dibuja nada (AC-SCN-01-06). Las sondas de **WP-B0** lo comprueban antes de construir (§3.2).
5. **El HUD no suma elementos** (AC-WKT-18-21): `scene-surface` ocupa el lugar del fondo provisorio, que pasa a `display: none` mientras se ve la escena; solo durante el fundido de 0,6 s de la escena demorada (AC-SCN-01-07) se ven los dos (+1, ≤ 59 de 60 con el corte de la fila de 200 píldoras).
6. **El mundo se arma por código** (DT6a-04): sin escenas aditivas en la 6a. `SceneWorldDefinition` (un `ScriptableObject`) referencia el `EnvironmentDefinition` del lago y sus **slots**; `SceneWorldBuilder` lo arma por tramos de tiempo (≤ 3 ms por cuadro, solo con nada moviéndose o con la pantalla en negro) cuando Workout está en reposo (precarga, P-14). Todo lo de terceros entra por su slot (`Assets/ThirdParty/`, ignorado salvo los `.meta`); sin el Terrain Sample Asset Pack, terreno y vegetación procedurales.
7. **La cámara es la del legacy, exacta**: la matriz de proyección la calcula Core (`ChaseCameraProjection`, desfasada con el horizonte al 40 % / 42 % y la focal = lado corto) y Unity la usa tal cual; las marcas de AC-SCN-02-02 se proyectan en el píxel del legacy ± 1 dp. La respiración (0,15°, con su entrada de 0,6 s), el vuelo de entrada (220-1 820 ms, Bézier del Designer con `ease-in-out-sine` y la focal desde ×1,25), la salida y la toma aérea (con cabeceo de −4°) son líneas de tiempo puras.
8. **Coordenadas "cinta"** (ADR 015, Q-68): la cámara queda cerca del origen y el mundo se dibuja relativo a `d` en `double` en Core; las fases del agua y de las líneas de brillo se acotan en `double` antes de ir al shader. A 42 195 m nada tiembla ni salta (AC-SCN-01-15).
9. **Rendimiento** (§4): **una sola tabla** (la del Designer más la composición y las RT: ≈ 87 *draw calls*, ≈ 135 k triángulos, ≈ 11,3 ms de GPU estimados en el nivel Medio), los tres niveles de ADR 018 como `QualityTierProfile` con sus assets de URP (llegan en esta iteración; la resolución va en el tamaño de nuestra RT y nunca toca un asset), la adaptación térmica pura que mide solo con la escena dibujándose, **0 B por cuadro** medido en cuadros reales con `FrameAllocations`, con el `Tick` y el pedido de render adentro de lo medido y su control positivo, y el `[PERF]` del build por tiempo (00-deuda-5 §5), también de la salida y de la escena demorada. Los 60 fps en Android quedan **NO VERIFICABLE** sin teléfono ni módulo (regla 9, a la It. 14).
10. **Capturas y videos** (§5): el conjunto **`it6a`** (`SceneCatalog`) con las 24 capturas, las 8 tomas de la demo y los 11 videos de la spec, en el Editor en *batchmode* **sin `-nographics`**, con la compilación de shaders sincrónica y la escena avanzada en pasos fijos de 1/60 s; **dos corridas iguales byte a byte**, o una tolerancia de 1 nivel por canal en menos del 0,1 % de los píxeles, solo si QA la justifica toma por toma.
11. **Lo que cambió en el gate** (detalle en §9 y en "Cambios por gate QA"): un valor por constante con las notas del Designer y la spec corregida; la preparación de la escena **sin pasos indivisibles** (los *shaders* se calientan por tramos y el único dibujo de calentamiento es un cuadro normal de escena, fuera de pantalla y antes de `Ready`); un slot con el procedural asignado no avisa; ni las pruebas ni el arnés cambian `ProjectSettings` (y los *scripts* lo vigilan); las estrellas se apagan detrás del disco y del halo; el aviso BSD-3 del generador de Dart viaja con los builds (`THIRD_PARTY_NOTICES.md`).

| Pregunta de la tarea | Dónde |
|---|---|
| 1) Árbol de archivos y asmdefs | §1 |
| 2) Core de escena puro: firmas, fuentes del legacy, generador con vectores dorados, orilla determinista | §2 |
| 3) La escena URP: composición, cielo, luz, nubes, montañas, terreno, vegetación, agua, bote, cámara, slots; la convivencia con UI Toolkit y con el simulador | §3 |
| 4) Rendimiento: presupuesto, niveles, *frame pacing*, nada animando al salir, 0 B por cuadro | §4 |
| 5) Capturas y videos | §5 |
| 6) Tests, oráculo, matriz AC → test / captura, los 26 casos legacy | §6 |
| 7) Paquetes y contratos | §7 |
| 8) Riesgos y desvíos (y la spec contra la base del Designer) | §8 |
| 9) Las notas del Designer y los pedidos de la base: un valor por constante | §9 |

---

## 1. Estructura

### 1.1 Árbol de archivos

#### Core (`RowMate.Core`, puro, `Core/Scene/`)

| Archivo | Marca | Contenido (fuente legacy) |
|---|---|---|
| `Core/Scene/SceneCamera.cs` | N [A] | `SceneCamera` (*struct*): horizonte, focal, proyección, escala e inversa sobre el agua; el punto del remero (`scene_camera.dart:1-46`) (§2.1) |
| `Core/Scene/ChaseCameraProjection.cs` | N [A] | `SceneMatrix4` y la proyección desfasada que reproduce `project` (contrato y mezclas del vuelo) (§2.1) |
| `Core/Scene/ChaseCameraMotion.cs` | N [A] | respiración (ANIMATION_BIBLE §5.2), vuelo de entrada, salida, toma aérea; `ChaseCameraDirector` y `CameraPose` (§2.2). Va en `Core/Scene/` y no en `Stroke/` (DT6a-13) |
| `Core/Scene/SceneState.cs` | N [A] | velocidad, distancia, tiempo y `ShaderDistance` (`scene_state.dart:13-57`, la parte de la 6a) (§2.3) |
| `Core/Scene/SceneInputs.cs` | N [A] | las entradas de cada cuadro desde la foto del motor (`scene_view.dart:65-70`, `immersive_workout_screen.dart:81-86`) (§2.3) |
| `Core/Scene/SceneColor.cs` | N [A] | color sRGB con canales `double`, `Lerp` como `Color.lerp` del Flutter del legacy, paso a lineal (§2.4) |
| `Core/Scene/ScenePalette.cs`, `SceneTimeOfDay.cs` | N [A] | la paleta, `PaletteFor`, `SunElevation`, `IsNight`, `LightFor` (el astro del legacy) (`time_of_day.dart:1-117`) (§2.4) |
| `Core/Scene/SceneHour.cs` | N [A] | la hora efectiva (`scene_view.dart:81-86`) y `SceneHourTransition` (DV-58) (§2.5) |
| `Core/Scene/CelestialPlacement.cs` | N [A] | el astro que se ve, la luz, la puesta y la salida graduales (D-ART-05, Q-63; DV-52, DV-54) (§2.6) |
| `Core/Scene/MoonPhase.cs` | N [A] | edad, fase, iluminación y espejo del hemisferio sur (DV-57) (§2.6) |
| `Core/Scene/HourLighting.cs` | N [A] | la luz, el ambiente, la niebla y el post de cada hora, el factor dorado `g`, y la composición con el velo de pausa (AC-SCN-09-05, AC-WKT-13-12) (§2.7) |
| `Core/Scene/NightLights.cs` | N [A] | el umbral del legacy (`shore_painter.dart:26`) y el minuto propio de cada luz con su fundido (DV-60) (§2.7) |
| `Core/Scene/StarField.cs`, `CloudModel.cs` | N [A] | las 80 estrellas con su fundido (`StarFader`) y las 7 nubes con el fundido de la vuelta (`sky_painter.dart:29-33`, `:49-66`, `:88-103`; Q-62, D-ART-06, DV-53, DV-56) (§2.8) |
| `Core/Scene/SkyBackdrop.cs`, `CelestialMask.cs` | N [A] | la curva del telón del cielo que copia el *shader* `Sky` (DV-66, QA-6a-PAR-10) y la atenuación de las estrellas detrás del disco y del halo que copia `Stars` (QA-G6aT-08) (§2.8) |
| `Core/Scene/HorizonModel.cs` | N [A] | las dos cordilleras: perfil, color, nieve, alto por ventana y acercamiento (`lake_environment.dart:43-85`, `environment.dart:127-129`; Q-65) (§2.9) |
| `Core/Scene/DartRandom.cs` | N [A] | el `math.Random(seed)` de la VM de Dart (BSD-3, con atribución en el encabezado y en `THIRD_PARTY_NOTICES.md`) (§2.10) |
| `Core/Scene/EnvironmentId.cs`, `PropKind.cs`, `ShoreProp.cs` | N [A] | `environment.dart:10-37` (§2.11) |
| `Core/Scene/ShoreEnvironment.cs`, `LakeEnvironment.cs` | N [A] | el escenario base (constantes, semilla, ola) y el lago (`environment.dart:39-70`, `lake_environment.dart:1-40`) (§2.11) |
| `Core/Scene/ShoreSegments.cs` | N [A] | la caché por segmento, `PropsForSegment`, `VisibleProps` con el tope y la prioridad de hitos, el olvido (`environment.dart:72-120`) (§2.11) |
| `Core/Scene/ShoreShading.cs` | N [A] | tinte de ambiente, niebla y noche de la orilla (`shore_painter.dart:26`, `:44-50`) (§2.12) |
| `Core/Scene/DegradedWater.cs` | N [A] | los tres tramos y las 10 líneas (`water_painter.dart:79-103`) y su fase en `double` (§2.12) |
| `Core/Scene/TerrainProfile.cs` | N [A] | `TerrainHeight(entorno; x; zMundo)`, la orilla que serpentea hacia afuera, las envolventes (00-deuda-diseno §6.2; DD6a-09) (§2.13) |
| `Core/Scene/PropPlacement.cs` | N [A] | pie de cada objeto, la huella de la cabaña con el ancla fija (DT6a-08), la poda, el muelle hasta la playa (§6.3 del Designer; DD6a-10) (§2.13) |
| `Core/Scene/ShoreFill.cs` | N [A] | juncos, rocas, bosque, matas, pasto y helechos con sales propias (§6.4 del Designer; DD6a-11) (§2.13) |
| `Core/Scene/Geometry/*.cs` | N [A] | `MeshData` (arreglos puros) y los constructores: `PineMesh`, `CabinMesh`, `PierMesh`, `RockMesh`, `ReedTuftMesh`, `GrassCardMesh`, `HullMesh`, `TerrainChunkMesh`, `HorizonStripMesh` (§2.14) |
| `Core/Scene/Art/LakeArt.cs`, `SkyArt.cs`, `HourLookTable.cs`, `WaterArt.cs` | N [A crea, D dueño] | los números de arte: recetas de las mallas, colores de terreno y materiales, densidades del relleno, tabla de la luz por hora (ART_BIBLE §6.3 con 00-deuda-diseno §4.6), tamaños de los discos. WP-A los crea con los valores de las biblias y del Designer; desde WP-D1 son del Designer (excepción de dueño, §7.6) |
| `Core/Scene/SceneSession.cs`, `SceneFrame.cs` | N [A] | el agregado por sesión: `Open`, `Tick`, la coreografía, los controles de prueba; el cuadro a dibujar (§2.15) |
| `Core/Scene/SceneWarmupPolicy.cs` | N [A] | cuándo se puede preparar la escena y cuánto por cuadro (AC-WKT-13-15) (§2.16) |
| `Core/Scene/ThermalAdaptation.cs` | N [A] | la adaptación del detalle: orden, umbrales, histéresis (ARCHITECTURE §20.3; AC-SCN-01-13) (§2.16) |
| `Core/Scene/IRenderAssetQuality.cs` | N [A] | el puerto puro para lo que solo vive en el asset de URP (el nivel y la distancia de sombras): lo implementa `Platform/Display/PlayerUrpAssetQuality` en el *player*; en el Editor, Bootstrap pone uno que solo anota (§4.2; QA-G6aT-05) |
| `Core/Scene/SlotIds.cs` | N [A] | los ids de ASSET_SLOTS §1 más `vegetation.canopyFar` (DD6a-22) (§2.17) |
| `Core/Design/SceneTimelines.cs` | N [A] | las líneas de tiempo de la escena en el inmersivo: aparición 220-400, vuelo 220-1 820, salida, toma aérea, velo, escena demorada (0,6 s), fundido del HUD al rotar (§2.2, §3.9) |
| `Core/Design/MotionSpecs.cs` | C [A] | el bloque de la 6a (duraciones y curvas de la tabla anterior) |
| `Core/Design/HudContrast.cs` | N [A] | la composición de las píldoras con su variante `.hud` sobre el peor vidrio (AC-WKT-17-13; DD6a-18) (§2.18) |
| `Core/AssemblyInfo.cs` | C [A] | `InternalsVisibleTo("RowMate.Scene")`: `SceneHost` llama los controles de prueba `internal` de `SceneSession` desde sus propios controles `internal` (QA-G6aT-14; regla de código en `PureCodeRulesTests`, §6.3) |
| `THIRD_PARTY_NOTICES.md` (raíz del repo) | N [A] | la licencia BSD-3 del SDK de Dart por el puerto de `DartRandom` (QA-G6aT-19); viaja con los builds (§1.1, B2) |

#### App (`RowMate.App`, puro)

| Archivo | Marca | Contenido |
|---|---|---|
| `App/Scene/SceneSettings.cs` | C [A] | solo el comentario (la escena ya la lee); la hora forzada sigue `int?` y sin persistir (AC-SCN-23-03). Nada más: la escena lee `AppContext.Workout.State` y `AppContext.Scene` directamente (§3.8) |

#### Scene (`RowMate.Scene`, Unity)

| Archivo | Marca | Contenido |
|---|---|---|
| `Scene/AssemblyInfo.cs` | C [B0] | `InternalsVisibleTo` de `RowMate.Tests.PlayMode`, `RowMate.Tests.PlayMode.Simulator`, `RowMate.Tests.UiSupport`, `RowMate.Snapshots` (los controles de prueba son `internal`) |
| `Scene/SceneHost.cs` | N [B1] | el ciclo de vida: `Preload`, `Open`, `Tick`, `Close`, `Readiness`, `Output`; el observador del `Tick` para la medida de memoria (§4.4); los controles de prueba (§3.1) |
| `Scene/SceneRenderer.cs`, `SceneRenderTarget.cs` | N [B0] | la RT (ventana × resolución del nivel, filtro bilineal si se escala) y el pedido de render explícito, con su respaldo (§3.2). B0 los crea para las sondas; B1 no los vuelve a abrir salvo lo que digan las sondas |
| `Scene/SceneShaderWarmup.cs` | N [B1] | el precalentado por tramos: `ShaderVariantCollection.WarmUpProgressively` (y `GraphicsStateCollection` donde haya una grabada), la espera de la compilación asíncrona en el Editor y la prueba de que no queda nada compilando (§3.4; QA-G6aT-03, -07) |
| `Scene/Quality/SceneQualityApplier.cs` | N [B1] | aplica el nivel a lo que es de la cámara, de la RT, del `Volume` y de los materiales; lo que vive solo en el asset de URP lo pide a un `IRenderAssetQuality` que en el Editor no hace nada (§4.2; QA-G6aT-05) |
| `Scene/SceneWorldBuilder.cs` | N [B1] | arma el mundo por tramos desde `SceneWorldDefinition` y los slots (§3.4) |
| `Scene/SceneDirector.cs` | N [B1] | aplica el `SceneFrame` a la cámara, la luz, los materiales y los *streamers*, sin asignaciones (§3.1) |
| `Scene/ChaseCameraRig.cs` | N [B1] | la pose y la matriz de proyección de Core en la `Camera` (§3.5) |
| `Scene/SkyRenderer.cs`, `HorizonRenderer.cs` | N [B1] | cielo, estrellas, sol y luna, nubes; las dos cordilleras (§3.6) |
| `Scene/WaterSurface.cs` | N [B1] | el plano del agua degradada y sus parámetros (§3.6) |
| `Scene/TerrainStreamer.cs`, `ShoreStreamer.cs`, `FillStreamer.cs` | N [B1] | terreno por segmento, objetos del legacy y relleno instanciados, con *pools* (§3.6) |
| `Scene/BoatVisual.cs` | N [B1] | el casco provisorio y su sombra ovalada (§3.6) |
| `Scene/SceneLighting.cs` | N [B1] | luz principal, ambiente, niebla y el único `Volume` de la escena (§3.6) |
| `Scene/ScreenOverlay.cs` | N [B1] | el *quad* de la cámara para los degradés de pantalla, que son de la capa del HUD (DV-67): apagados en la escena sola (§3.3, §5.1) |
| `Scene/SceneShaderIds.cs` | N [B0] | **todos** los nombres de *shaders*, propiedades, globales y palabras clave del contrato con WP-D (§7.5, que incluye lo de 04-design-notes §14.2), antes de D1 (QA-G6aT-01) |
| `Scene/MeshUpload.cs`, `ProceduralTextures.cs` | N [B1] | `MeshData` → `Mesh` sin GC (búferes nativos persistentes); atlas de nubes, luna y ruido horneados al precargar |
| `Scene/Slots/{AssetSlotDefinition,AssetSlot,SlotResolution,SlotLog,PropSlot,TerrainLayerSlot,VegetationSlot,MaterialSlot,TextureSlot,DataSlot,SkyProfileSlot,HorizonProfileSlot,WaterProfileSlot}.cs` | N [B0] | el modelo de ARCHITECTURE §14.4 (los `DataSlot` concretos: Unity no crea assets de un `ScriptableObject` genérico) (§3.7) |
| `Scene/Slots/EnvironmentDefinition.cs`, `SceneWorldDefinition.cs` | N [B0] | los slots del lago y de la escena (§3.7) |
| `Scene/Quality/QualityTierProfile.cs` | N [B0] | un nivel de calidad (ADR 018) (§4.2) |

#### Platform, Presentation, Bootstrap, Editor, arnés, pruebas y herramientas

| Archivo | Marca | Contenido |
|---|---|---|
| `Platform/Display/GraphicsTier.cs`, `ThermalAdapter.cs` | N [B2] | el nivel inicial con `SystemInfo`; la medida con `FrameTimingManager` (tiempo de trabajo, solo con la escena dibujándose) que alimenta `ThermalAdaptation` (§4.2) |
| `Platform/Display/PlayerUrpAssetQuality.cs` | N [B2] | `IRenderAssetQuality` del *player*: el nivel con `QualitySettings.SetQualityLevel` y las copias en ejecución del asset de URP para la distancia de sombras; en el Editor no se arma (§4.2) |
| `Presentation/Workout/IImmersiveScene.cs` | N [B0] | el puerto que usa el inmersivo (con tipos de Unity: por eso vive acá y no en App, DT6a-03) (§3.3) |
| `Presentation/Workout/SceneSurface.cs` | N [B1] | el elemento `scene-surface`: la RT de la escena de fondo, sin eventos; se suscribe a `OutputChanged` al adjuntarse al panel y se desuscribe al soltarse (§3.3) |
| `Presentation/Workout/HudView.cs` | C [B2] | arma `SceneSurface` primero; el velo va también a la escena; `HideForCapture` (escena sola: oculta los hermanos de `scene-surface`) |
| `Presentation/Workout/HudBackdrop.cs` | C [B2] | el fondo provisorio teñido por la hora (DD6a-23) y `display: none` con la escena |
| `Presentation/Workout/ImmersiveWorkoutPage.cs` | C [B2] | abre la escena en el toque, el vuelo, la escena demorada, la salida, la toma aérea, el cierre detrás del negro, la pausa de los 60 fps con el diálogo quieto (P-07) (§3.9) |
| `Presentation/Workout/WorkoutScreenView.cs` | C [B2] | la precarga en reposo (P-14) |
| `Presentation/Workout/PillRowView.cs` | C [B2] | las píldoras del HUD con `step.*.hud` (AC-WKT-17-13; P6a-01) |
| `Presentation/Shell/UiContext.cs` | C [B2] | `IImmersiveScene? Scene` (null = sin escena: el fondo provisorio, como hasta la 5) |
| `Presentation/Motion/Hud/SceneFadeMotion.cs`, `HudReflowMotion.cs` | N [D1] | el fundido de 0,6 s de la escena demorada; el fundido del HUD al cambiar de orientación (DT6a-06) |
| `Presentation/Motion/Hud/PauseVeilMotion.cs` | C [D1] | el mismo alfa también para la escena (peso 0-1) |
| `Bootstrap/SceneBridge.cs` | N [B2] | implementa `IImmersiveScene` sobre `SceneHost` (el único que ve a Scene y a Presentation) (§3.3) |
| `Bootstrap/AppRoot.cs`, `ShellHost.cs`, `UiAssets.cs` | C [B2] | arma `SceneHost` y el puente; `Tick` en `LateUpdate`; niveles y adaptación; los argumentos `[PERF]` de la 6a (§4.5) |
| `Editor/Bootstrap/UrpProvisioner.cs` | C [B0] | `RowMate_URP_{High,Medium,Low}` con su *renderer* (`renderScale` 1 en los tres: la resolución va en la RT), los tres niveles de calidad (los únicos; Windows en Alto, Android en Medio), la capa `RowMateScene`, `PlayerSettings.enableFrameTimingStats`, la cámara de UI sin capas. Es el **único** archivo que asigna un asset de URP a la configuración del proyecto (regla de código, §6.3) |
| `Editor/Bootstrap/SceneAssetsBuilder.cs` | N [B0] | crea `Settings/Quality/*.asset`, `Slots/*.asset` (cada uno con el procedural asignado, §3.7) y `Scenes/SceneWorld.asset` (idempotente, como el resto del *bootstrapper*). Sin materiales |
| `Editor/Bootstrap/SceneMaterialsBuilder.cs` | N [B1] | crea `Art/Materials/Scene/*.mat` con los *shaders* de D1 y genera `Settings/Scene/SceneShaderVariants.shadervariants` (la lista de §3.4); idempotente (QA-G6aT-07, -14) |
| `ProjectSettings/{QualitySettings,TagManager,ProjectSettings,GraphicsSettings}.asset` | G [B0] | lo que escribe el *bootstrapper*: los tres niveles con su asset, la capa `RowMateScene`, *Frame Timing Stats*, el asset por defecto (QA-G6aT-14) |
| `Settings/Scene/SceneShaderVariants.shadervariants` | G [B1] | lo genera `SceneMaterialsBuilder` |
| `Editor/Slots/SlotValidator.cs` | N [B1] | el menú `RowMate/Slots/Validar` (lista también los procedurales, sin avisar) y el re-enlace por `expectedPath` (RA-17) |
| `Editor/Build/BuildOutputInspector.cs` | C [B2] | los argumentos `[PERF]` nuevos, prohibidos en el build normal; `THIRD_PARTY_NOTICES.md` presente en la salida |
| `Editor/Build/NoticesPostprocessor.cs` | N [B2] | copia `THIRD_PARTY_NOTICES.md` junto al ejecutable en cada build de Windows (QA-G6aT-19); Android lo suma en la It. 10 |
| `SimulatorPresentation/FrameTimeProbe.cs` | C [B2] | medida por tiempo, GPU si está, etiquetas de la 6a, la ventana de la salida (0 a 1 s desde la confirmación de `Terminar`), la entrada con la escena demorada y las compilaciones de *shaders* después de `Ready` (§4.5) |
| `Art/Shaders/Scene/*.shader`, `Art/Shaders/Include/RowMate*.hlsl` | N [D1, D2] | los shaders propios (§3.6, §7.5) |
| `Snapshots/SceneCatalog.cs`, `SceneMarks.cs` | N [B3] | el conjunto `it6a` y las marcas del legacy y del horizonte (§5) |
| `Snapshots/{SnapshotCatalog,SnapshotCli,SnapshotRunner,SnapshotTakeRule,ScenarioScript}.cs` | C [B3] | `-snapshotSet it6a`, la escena real en el anfitrión, la toma con la compilación sincrónica, la lectura de valores por cuadro de la escena; la escena sola con `HideForCapture` y `SetScrimsForTest(false)`; la sonda del brillo y la toma `it6a-low` (§5) |
| `Tests/UiSupport/{UiTestHost,UiHostOptions,DeviceProfile}.cs` | C [B1] | `SceneMode` (`None` por defecto, `Real`: con la compilación asíncrona de *shaders* apagada mientras dura el anfitrión, `ShaderUtil.allowAsyncCompilation`); los perfiles `V390` y `H844`; pasos fijos de la escena |
| `Tests/UiSupport/TierScope.cs` | N [B1] | fuerza un nivel de calidad para una prueba y lo restaura siempre (§4.2; QA-G6aT-05) |
| `Tests/UiSupport/FrameAllocations.cs` | C [B1] | `WatchScene(SceneHost)`: el `Tick` y el pedido de render adentro de lo medido, en dos cuentas (lo nuestro y el dibujo de URP), con su control positivo (§4.4; QA-G6aT-06) |
| `Tests/UiSupport/FakeImmersiveScene.cs` | N [B2] | un `IImmersiveScene` de prueba que cuenta suscriptores de `OutputChanged` y llamadas a `Preload` y `Open` (QA-G6aT-16) |
| `Tests/Support/SceneSeeds.cs` | N [A] | estados de prueba `E-0`, `E-920`, `E-1234`, `E-42K` y un escenario de prueba con boyas e hitos (AC-SCN-14-05), con `z` distintas en el corte (QA-G6aT-15) |
| `Tests/EditMode/Scene/**` | N [A] | §6.3 |
| `Tests/UnityEditMode/SceneAssetsTests.cs` | N [B0] | §6.3 |
| `Tests/UnityEditMode/SceneMaterialsTests.cs` | N [B1] | §6.3: los 13 *shaders* del contrato con sus colas, compilados para los destinos de §7.5; los materiales; la colección de variantes |
| `Tests/PlayMode/Scene/**`, `Tests/PlayModeSimulator/SceneHourSimulatorTests.cs` | N [B1, B2] | §6.3 |
| `Tests/PlayMode/HudWarmupSceneTests.cs` | N [B2] | 0 suscriptores en la escena después de desarmar y 0 llamadas a `Preload` u `Open` (QA-G6aT-16) |
| `Tests/EditMode/Traceability/It06aIds.cs` | N [A] | los 101 ID y LT-62 a LT-87 con su verificación (§6.5) |
| `Tests/EditMode/Project/{PureCodeRulesTests,ItSixAToolingTests,RepoHygieneTests}.cs` | C/N [A] | los archivos nuevos de Core sin reloj ni `UnityEngine`; `Assets/ThirdParty/` ignorado salvo `.meta`; ningún asset versionado referencia un GUID de `Assets/ThirdParty/` salvo los de `Slots/`; `THIRD_PARTY_NOTICES.md` si existe `DartRandom.cs`; el oráculo y el arnés; las reglas de código del gate (§6.3) |
| `tools/unity/ProjectSettingsGuard.ps1` | N [B0] | el resumen (SHA-256) de `ProjectSettings/` y `Assets/RowMate/Settings/` antes y después de una corrida de Unity; si algo cambió, la corrida falla con la lista (QA-G6aT-05) |
| `tools/unity/run-unity-tests.ps1` | C [B0] | usa la guardia de `ProjectSettings` |
| `tools/build/build.ps1` | C [B2] | usa la guardia de `ProjectSettings` |
| `tools/oracle/engine/test/scene_vectors_test.dart`, `scene_legacy_images_test.dart`, `tools/oracle/run-oracle.ps1`, `tools/oracle/README.md` | N/C [A] | el oráculo de la escena (`-Scene`) (§6.2) |
| `Tests/Golden/oracle/scene_{random,palette,light,sky,lake,water}.json` | G [A] | sus vectores (≤ 1 MiB cada uno) |
| `docs/migration/iterations/06a-escena-lago/snapshots/legacy/LEG-*.png` | G [A] | las referencias del legacy |
| `tools/unity/capture-snapshots.ps1`, `tools/unity/README.md` | C [B3] | `-Set it6a` (por defecto), la nota de la compilación sincrónica, la guardia de `ProjectSettings` |
| `.gitignore` | C [B0] | `Assets/ThirdParty/**` salvo `*.meta` (ya estaba en el plan; QA lo vuelve a verificar con `git check-ignore`) |
| `docs/migration/architecture/adr/022-escena-en-el-panel.md` | N [B0] | ADR corto: la escena dibujada por pedido explícito y compuesta en el panel (DT6a-05) |
| `docs/migration/architecture/ARCHITECTURE.md`, `adr/015-escena-3d.md` | C [B3] | §2 (el árbol), §3.2 (el puerto), §14 (sin escenas aditivas en la 6a), "Cambios de la It. 6a" |

**Manifest**: `Packages/manifest.json` no cambia (URP 17.3.0 y Shader Graph ya están). No se agrega Cinemachine (ADR 015).

### 1.2 Asmdefs

Ninguno nuevo. Cambios de referencias:

| Asmdef | Cambio | Por qué |
|---|---|---|
| `RowMate.Tests.UiSupport` | + `RowMate.Scene`, `Unity.RenderPipelines.Core.Runtime`, `Unity.RenderPipelines.Universal.Runtime` | el anfitrión arma la escena real (`SceneMode.Real`) |
| `RowMate.Tests.PlayMode`, `RowMate.Tests.PlayMode.Simulator` | + `Unity.RenderPipelines.Core.Runtime`, `Unity.RenderPipelines.Universal.Runtime` | contadores y la cámara en las pruebas |
| `RowMate.Snapshots` | + `RowMate.Scene` | `SceneCatalog` usa los controles de prueba |
| `RowMate.Platform` | + `Unity.RenderPipelines.Universal.Runtime` | `PlayerUrpAssetQuality` cambia la distancia de sombras de la **copia en ejecución** del asset activo (solo en el *player*); la resolución no toca URP (la aplica `SceneHost` en su RT) |

`RowMate.Scene` no referencia Presentation ni Platform (ARCHITECTURE §3.2, regla 2): el puente vive en Bootstrap, que también aplica las acciones de la adaptación (la resolución y la densidad en `SceneHost`, la distancia de sombras y el nivel en `IRenderAssetQuality`). `AsmdefLintTests` y los `.csproj` de `tools/dotnet` siguen el grafo (Core/Scene compila solo, por el `Core/**/*.cs` del proyecto de Core).

---

## 2. Core de escena (puro, WP-A)

Todo con `noEngineReferences`, C# 9, sin asignaciones en `Tick` (los arreglos se reservan al abrir la sesión o al cambiar la ventana), cultura invariante, `DartMath.Mod` para el `%` de Dart y los `double` con el mismo orden de operaciones que el legacy (las sumas y productos en el mismo orden de evaluación: la paridad bit a bit depende de eso).

**Bit a bit o con tolerancia** (QA-6a-PAR-05; el precedente es R-T4, `Simulator/RowingSimulator.cs:109`). El oráculo corre en la VM de Dart en Windows y el CI corre dotnet en `ubuntu-latest` y `windows-latest`: `sin`, `asin`, `cos`, `exp` y `pow` de glibc, de la UCRT y de la VM pueden diferir en el último bit. Por eso:

| Cómo se compara | Qué |
|---|---|
| **Bit a bit** (aritmética pura: `+`, `−`, `·`, `/`, `Mod`, `clamp`) | `DartRandom`; los segmentos y su resumen FNV-1a; la paleta (los 7 colores y el ambiente, `PaletteFor`); las listas visibles; `ShoreShading`; `DegradedWater` (con la fuerza de la luz **leída del oráculo** como entrada, no recalculada); `StarField.Layout`; `CloudModel` (`X`, `LegacyX`, `Y`, alfas, colores); `HorizonModel.Color`; `SkyBackdrop.Gradient` |
| **`Within(1e-12)` absoluto** (valores del orden de 1) | `SunElevation`, `HorizonModel.LegacyProfile`, `MoonPhase` (iluminación y ángulo), `ApproachScale`, la fuerza de `LightFor` |
| **`Within(1e-9)`** (la tolerancia de la spec; valores en dp, en grados o en horas) | `LightFor` (`x`, `y` en dp), `NightStartHour` y `NightEndHour`, `CelestialPlacement` (grados), la matriz de la cámara contra `TryProject` |
| **Exacto, con margen comprobado** | los booleanos que salen de una función trascendente (`IsNight` por minuto): la prueba afirma además que ninguna muestra cae a menos de 1e-6 h del umbral, así un último bit distinto no puede darlos vuelta |

`scene_palette.json` sigue guardando `sunElevation` en bits de `double` (es el formato del archivo), pero se compara con `Within(1e-12)`. Lo dice `SceneGoldenTests` en su encabezado y cada prueba usa el comparador de su fila.

### 2.1 La cámara del legacy y su matriz (`SceneCamera`, `ChaseCameraProjection`; F-SCN-02; LT-62 a LT-65)

```csharp
namespace RowMate.Core.Scene
{
    // scene_camera.dart:6-46. La pantalla en dp con el origen ARRIBA a la izquierda, como el legacy.
    public readonly struct SceneCamera
    {
        public const double CamHeight = 2.2;   // :12
        public const double BoatZ = 9.5;       // :16
        public const double MinZ = 0.5;        // :19
        public SceneCamera(double width, double height);              // :7-9: horizonte 0,42·alto si ancho > alto, si no 0,40; focal = lado corto
        public double Width { get; }
        public double Height { get; }
        public double HorizonY { get; }
        public double Focal { get; }
        public double CenterX { get; }                                // :25
        public bool TryProject(double x, double y, double z, out double sx, out double sy);   // :28-34 (false = null del legacy)
        public double ScaleAt(double z);                              // :37
        public void UnprojectWater(double sx, double sy, out double x, out double z);         // :41-45
        public double RowerY { get; }                                 // HorizonY + Focal·CamHeight/BoatZ (centro de la zona del bote)
    }

    // 4 × 4 en double, por filas; Multiply, MultiplyPoint, Inverse. Sin asignaciones (struct de 16 campos).
    public readonly struct SceneMatrix4 { /* ... */ }

    public static class ChaseCameraProjection
    {
        // La proyección que reproduce TryProject para un punto en coordenadas de cámara (convención de Unity: la cámara mira a
        // −z de vista). Con ancho W, alto H, focal f y horizonte h (dp):
        //   P = [ 2f/W, 0,    0,          0 ]
        //       [ 0,    2f/H, 2h/H − 1,   0 ]
        //       [ 0,    0,   −(F+N)/(F−N), −2FN/(F−N) ]
        //   y la cuarta fila (0, 0, −1, 0).
        // Con h = 0,40·H el término (2h/H − 1) vale −0,20 (el lensShift.y = −0,10 de ART_BIBLE §8.1); con 0,42, −0,16 (−0,08).
        public static SceneMatrix4 Contract(in SceneCamera cam, double near, double far);
        // La del vuelo: focal y horizonte interpolados (focal de 1,25 × la del contrato y horizonte en H/2 → contrato), §2.2.
        public static SceneMatrix4 Blend(in SceneCamera cam, double focalDp, double horizonDp, double near, double far);
        // Lo que daría Unity con esa matriz: el píxel en dp (para las pruebas de dotnet y para comparar con la cámara real).
        public static bool TryProjectWithPose(in SceneMatrix4 projection, in CameraPose pose, in SceneCamera cam,
                                              double x, double y, double z, out double sx, out double sy);
    }
}
```

- **Por qué la matriz y no la cámara física**: el signo del `lensShift` y la convención de `gateFit` de Unity son una fuente de error (ART_BIBLE §8.1 pide validarlo). La matriz la calcula Core y la prueba dotnet contra `TryProject` en los siete tamaños (error ≤ 1e-9). Unity la usa tal cual (`Camera.projectionMatrix`, §3.5); PlayMode solo comprueba que el píxel real coincide ± 1 dp (AC-SCN-02-02).
- **Recorte cercano**: el plano cercano es `MinZ` (0,5 m: AC-SCN-02-03, LT-64); el lejano, **2 000 m** (el terreno termina en 1 500 m; el cielo, las estrellas, las nubes y las cordilleras no escriben profundidad y no dependen de él).
- **Profundidad sin Z invertida** (nivel Bajo en GLES; QA-G6aT-21): con 24 bits el paso de profundidad es ≈ `z²/(N·2²⁴)`: 0,024 m a 450 m y **0,268 m a 1 500 m**, y casi no depende del plano lejano (con 2 000 o con 4 000 m da lo mismo: lo manda el cercano, que es contrato). Por eso el remedio es de geometría: el trozo lejano del terreno (de 450 a 1 500 m) se arma **sin la franja sumergida** y con la orilla levantada 0,4 m sobre el agua (≥ 1,5 veces el paso a 1 500 m; a 450 m son 0,37 dp en TV, no se ve), así nunca hay dos superficies a menos de 0,4 m en la orilla lejana. Los trozos cercanos (hasta 450 m) siguen con la franja sumergida (el paso ahí es de 2,4 cm). D3D11, Vulkan y Metal usan Z invertida y no tienen el problema.
- **El punto del remero** (`RowerY`) da el centro de la zona del bote: 0,504 del alto en TV y P360, 0,652 en horizontal (AC-SCN-02-01). En V390 da 0,507 (la spec dice 0,504 para todo vertical: es el valor de los tamaños de la app; §8.4).

### 2.2 Respiración, vuelo, salida y toma aérea (`ChaseCameraMotion`; AC-SCN-02-04 a -08)

```csharp
public readonly struct CameraPose
{
    // Posición en metros (la de contrato: (0; 2,2; 0)), giros en grados (guiñada, cabeceo, rolido; 0 = nivelada mirando a +z) y la
    // proyección (focal y horizonte en dp: las del contrato fuera del vuelo).
    public CameraPose(double x, double y, double z, double yawDeg, double pitchDeg, double rollDeg, double focalDp, double horizonDp);
}

public readonly struct BreathingSpec   // ANIMATION_BIBLE §5.2 sin los términos del bote (llegan en la 7)
{
    public static BreathingSpec Lake { get; }   // lateral 0,035·sin(2π·0,071t) + 0,015·sin(2π·0,173t + 1,7); vertical 0,025·sin(2π·0,093t + 0,4);
                                                // rolido RollDeg·sin(2π·0,061t + 2,1) (RollDeg = 0,15, DT6a-01); cabeceo 0,10·sin(2π·0,083t + 0,9)
}

public static class ChaseCameraMotion
{
    public static CameraPose Breathing(in SceneCamera cam, double sceneTime, double multiplier, in BreathingSpec spec);
    // El apartamiento del horizonte en los bordes de la pantalla, en dp (para el barrido de AC-SCN-02-04 y la marca de VID-10).
    public static double HorizonDeviationDp(in SceneCamera cam, in CameraPose pose);
    // El vuelo (00-deuda-diseno §3.3 con DN6a-08): Bézier cúbica P0 (3; 9; −14), P1 (2,4; 7,2; −8,5), P2 (0,6; 3,4; −2,5),
    // P3 (0; 2,2; 0); la posición y la orientación (slerp desde "mirar al bote", z 9,5 e y 0,4, hasta nivelada) con
    // ease-in-out-sine, (1 − cos(π·u))/2: pico de 17,0 m/s en lugar de 32,5 con ease.move. La focal desde 1,25 × la del contrato
    // hasta la del contrato con ease.move (con el FOV de 50° la vertical empezaba en ×2,38); el horizonte de la proyección desde H/2
    // hasta el del contrato entre 0,6 y 1 del recorrido, con ease.move. u = 1 da EXACTAMENTE la pose del contrato (sin redondeo:
    // la última muestra devuelve las constantes).
    public static CameraPose Flight(in SceneCamera cam, double u, in FlightSpec spec);
    // Salida (AC-SCN-02-06): +1,5 m arriba y −4 m atrás en 1,2 s con ease.move, desde la pose que tenga en ese cuadro.
    public static CameraPose Exit(in CameraPose from, double secondsSinceStart);
    // Toma aérea (AC-SCN-02-07, DN6a-07): 2,4 m (0,4 m/s de promedio durante 6 s) con ease.move, mitad arriba y mitad atrás
    // (45°: +1,70 y −1,70 m) y un cabeceo de −4° con la misma curva; después, quieta. En TV el horizonte termina en 336,9 dp y la
    // popa en 552,9 (dentro de la zona del bote).
    public static CameraPose Aerial(in CameraPose from, double secondsSinceStart, in AerialSpec spec);
}

public readonly struct FlightSpec  { public static FlightSpec Default { get; } }   // puntos, 1,6 s, focal ×1,25, horizonte 0,6-1
public readonly struct AerialSpec  { public static AerialSpec Default { get; } }   // +1,70 / −1,70 m, −4°, 6 s

public enum CameraMode { Hidden, Flight, Contract, Exit, Aerial }

public sealed class ChaseCameraDirector
{
    public const double BreathingRampSeconds = 0.6;          // DN6a-20: la respiración entra con S(0; 0,6 s)
    public CameraMode Mode { get; }
    public void Reset();                                     // al abrir: Hidden
    public void BeginFlight(double now, bool reduceMotion);  // con reducir movimiento: Contract en el acto (sin respiración)
    public void ShowInPlace(double now);                     // la escena demorada: Contract, sin vuelo, y la rampa de 0,6 s (AC-SCN-01-07)
    public void BeginExit(double now, bool reduceMotion);    // con reducir movimiento: la cámara no se mueve
    public void BeginAerial(double now, bool reduceMotion);  // solo con el diálogo de completado; la respiración baja de 1 a 0 en 6 s (ease.move)
    public void SetPaused(bool paused, double now);          // la respiración ×0,6 en 300 (ease.move) y ×1 en 240
    public CameraPose Evaluate(double now, double sceneTime, in SceneCamera cam, bool reduceMotion, bool breathingOff);
}
```

- **El multiplicador de la respiración** es el producto de cuatro factores: la pausa (×0,6 con sus rampas), la entrada (`S(0; 0,6 s)` desde el fin del vuelo o desde `ShowInPlace`), la toma aérea (de 1 a 0 en 6 s) y reducir movimiento o "respiración en 0" (×0). Así **no hay salto de pose** en el cuadro de los 1 820 ms (en ese cuadro la rampa vale 0) ni al aparecer la escena demorada.

- **Sin azar** (AC-SCN-02-04): la respiración es una suma de senos del **tiempo de escena** (sigue en pausa, AC-SCN-01-03). Con reducir movimiento o con "respiración en 0" (prueba), ×0.
- **El horizonte**: la traslación no lo mueve (está en el infinito); lo mueven el cabeceo y el rolido. Barrido de 10 min en los siete tamaños (dotnet): con 0,15°, 2,932 dp en W1280; con 0,18°, 3,267 (DT6a-01).
- **El vuelo no atraviesa nada** (AC-SCN-02-05): todos los puntos de control tienen `y ≥ 2,2` y `|x| ≤ 3`, `z ≤ 0` (el casco empieza en `z` 5,3 y la orilla en `|x|` 9): la curva queda adentro de su envoltura convexa. dotnet lo barre en 1 000 muestras y afirma además la velocidad máxima ≤ 17,5 m/s ("sin acelerones") y la focal inicial = 1,25 × la del contrato ("sin zoom brusco").
- **Las líneas de tiempo** (`SceneTimelines`, Core/Design): el vuelo anclado a los 220 ms del toque (no al primer cuadro: el último cuadro cae en 1 820 aunque un cuadro se demore); la aparición de la escena 220-400 (`ease.enter`, la del fondo de la 4b); la escena demorada 0,6 s (`ease.enter`); la salida con los tramos de la 4b (HUD 0-200, negro 200-400, Workout 400-640); el velo 300 / 240 (`ease.move`).
- **Una sola cámara** (AC-SCN-02-08): `ChaseCameraDirector` no tiene entrada de usuario; ninguna clase de la escena registra toques, arrastre, rueda ni teclado (prueba de código en `PureCodeRulesTests`: `Core/Scene` y `Scene/` no nombran `Input`, `Pointer`, `Keyboard` ni `Mouse`).

### 2.3 Velocidad, distancia y entradas (`SceneState`, `SceneInputs`; F-SCN-03; LT-66 a LT-68)

```csharp
public sealed class SceneState
{
    public const double SpeedTau = 1.5;                       // scene_state.dart:16
    public double Speed { get; }                              // m/s
    public double Distance { get; }                           // m de mundo recorridos (double, Q-68)
    public double Time { get; }                               // s de escena
    public double ShaderDistance => DartMath.Mod(Distance, 4096.0);   // :33-34 (contrato; el agua nueva no lo usa, QA-P-28)
    public static double TargetSpeedFor(double pace500m, double spm, bool isActive);   // :38-43
    public void Update(double dt, double pace500m, double spm, bool isActive);         // :45-57: paso acotado [0; 0,1], inercia, corte a 0 bajo 0,01
    internal void ForceForTest(double distance, double speed, double time);            // E-0, E-920… (InternalsVisibleTo de las pruebas)
}

public readonly struct SceneInputs
{
    // AC-SCN-03-07: el split en segundos enteros y el SPM de la ÚLTIMA lectura (la misma que muestra el HUD); "en marcha" = Phase == Active.
    public static SceneInputs From(WorkoutSnapshot snapshot);
    public double Pace500m { get; }
    public double Spm { get; }
    public bool IsActive { get; }
}
```

- **Puerto parcial a propósito**: la fase de palada, el cabeceo y los charcos (`scene_state.dart:59-85`) son de la It. 7 (`scene_state_test` :37, :46, :60, :69). La clase queda abierta para sumarlos sin cambiar la parte de la 6a.
- **El paso de cada cuadro** sale del reloj monótono inyectado (`IClock.MonotonicSeconds`), acotado a [0; 0,1] (AC-SCN-03-04, `scene_view.dart:63`). En las pruebas y el arnés, `SceneSession.Advance` parte un salto grande en pasos de 1/60 s (§2.15): el arnés no "salta" 60 s en un cuadro.
- **La distancia de escena no toca nada** (AC-SCN-03-05, AC-WKT-13-14): ningún archivo fuera de `Core/Scene` y `Scene/` la lee (prueba de código), y el motor no recibe nada de la escena.

### 2.4 La paleta (`SceneColor`, `ScenePalette`, `SceneTimeOfDay`; F-SCN-09, F-SCN-10; LT-69 a LT-74)

```csharp
// Canales en [0; 1] como el Color de Flutter del legacy: un hex de 8 bits se lee como byte/255.0, sin redondear después.
public readonly struct SceneColor
{
    public static SceneColor FromHex(string hex);                       // "#RRGGBB"
    // Color.lerp del SDK del legacy: clampDouble(a·(1 − t) + b·t; 0; 1) por canal (<flutter-sdk>/bin/cache/pkg/sky_engine/lib/ui/
    // painting.dart:449-452 y lerp.dart:28-31; QA-6a-PAR-06). Lo usan la paleta, horizonColor, el cuerpo de la nube, la nieve y ShoreShading.
    public static SceneColor Lerp(in SceneColor a, in SceneColor b, double t);
    public SceneColor ToLinear();                                       // EOTF sRGB, para el shader
    public double R { get; } public double G { get; } public double B { get; }
}

public readonly struct ScenePalette
{
    public SceneColor SkyTop, SkyHorizon, WaterNear, WaterFar, SunColor, SunGlow, Fog;  // (propiedades)
    public double Ambient;                                               // multiplicador de la luz de la orilla y el bote
    public static ScenePalette Lerp(in ScenePalette a, in ScenePalette b, double t);   // time_of_day.dart:29-38: colores con Lerp y el ambiente con a + (b − a)·t
}

public readonly struct LegacyLight { public double X, Y, Strength, XFraction, Elevation; public bool IsMoon; }

public static class SceneTimeOfDay
{
    public static readonly double NightStartHour;   // 6 + 12·(1 + asin(0,4)/π) = 19,571 88… (19:34:19)
    public static readonly double NightEndHour;     // 6 − 12·asin(0,4)/π = 4,428 12… (4:25:41)
    public static double SunElevation(double hour);                     // :52
    public static bool IsNight(double hour);                            // :55 (≤ −0,4)
    public static LegacyLight LightFor(double hour, in SceneCamera cam); // :57-67 (el astro del legacy: contrato, DV-52)
    public static ScenePalette PaletteFor(double hour);                 // :69-116: 6 keyframes, 22 → 28 → 29, DartMath.Mod(hour, 24)
}
```

- Los keyframes se escriben con los hex del legacy (`time_of_day.dart:69-98`) y la tabla de AC-SCN-09-02 los repite: una prueba compara los dos.
- **El recorte de `Color.lerp`** no cambia hoy ningún valor (barrido de QA de 1 440 minutos × 7 colores: 0 diferencias), pero se porta igual: es parte del contrato (AC-SCN-09-02) y protege los `t` fuera de [0; 1] de las mezclas derivadas. STD lo prueba con `t` = −0,5 y 1,5.
- **El oráculo** (§6.2): los 8 campos en cada minuto del día (1 440 filas, en bits de `double`) y el astro del legacy en cada minuto y en los siete tamaños. La paleta y el ambiente, **bit a bit** (aritmética pura); `sunElevation`, con `Within(1e-12)`; `LightFor` (`x`, `y` en dp), con `Within(1e-9)` (§2, "Bit a bit o con tolerancia").

### 2.5 La hora de la escena (`SceneHour`, `SceneHourTransition`; F-SCN-09, F-SCN-23; DV-58)

```csharp
public static class SceneHour
{
    // scene_view.dart:81-86: la forzada (simulador) o la local con resolución de minuto, h + m/60 (los segundos se ignoran).
    public static double Effective(int? forcedHour, DateTimeOffset localNow);
}

public enum HourChange { Forced, MinuteTick, Instant }   // simulador (−, +, Hora real) · la hora real avanzó un minuto · lo demás

public sealed class SceneHourTransition
{
    public const double Seconds = 1.5;                       // AC-SCN-23-02 (ease.move)
    public const double MinuteSeconds = 1.0;                 // AC-SCN-09-01, mejora P2 (DN6a-10): lineal
    public double Visible { get; }                           // la hora que dibuja la escena
    public bool IsRunning { get; }
    // Forced: desde la visible, por el camino más corto del reloj (Δ = Mod(objetivo − visible + 12; 24) − 12; con Δ = −12, +12),
    // en 1,5 s con ease.move. MinuteTick: la hora real pasó al minuto siguiente (Δ = +1/60 h exacto, también de 23:59 a 0:00):
    // de la vieja a la nueva en 1 s lineal. Instant: un cambio de zona o de la hora del equipo, la vuelta del segundo plano,
    // la apertura y ForceHourForTest (AC-SCN-01-04). Con reducir movimiento, todo en el acto. Un cambio durante otro sale de la visible.
    public void Retarget(double target, double now, HourChange kind, bool reduceMotion);
    public double Evaluate(double now);                      // Mod(origen + Δ·curva(u); 24)
}
```

- **Quién decide el tipo**: `SceneSession.Tick` compara la hora efectiva del cuadro con la anterior. Si la forzada cambió (o pasó a `null`), `Forced`; si es la real y avanzó exactamente un minuto, `MinuteTick`; cualquier otro cambio de la real, `Instant`. El dato es el del minuto (la paleta del cuadro sale de la hora visible; la efectiva no se toca).

- El reloj de pared del HUD no se entera (AC-SCN-23-04): sigue con `WallClockTicker` sobre el reloj real; la escena no escribe en `SceneSettings`.
- **Forzar un minuto** en las pruebas (§0.4 de la spec): `SceneSession.ForceHourForTest(double)` (sin transición; los fundidos de las estrellas y de las luces quedan en su valor final). En un build que se instala no hay forma de hacerlo (los controles de prueba son `internal` y solo los ven las pruebas y el arnés, que no llegan al build; `SceneHost` los alcanza por `InternalsVisibleTo("RowMate.Scene")` y solo desde sus propios controles `internal`, regla de §6.3).

### 2.6 Sol, luna y fase (`CelestialPlacement`, `MoonPhase`; F-SCN-10; DV-52, DV-54, DV-57)

```csharp
public readonly struct CelestialSpec
{
    public static CelestialSpec Default { get; }   // ArcDeg = 10,5 (12° al mediodía; AC-SCN-10-04, DV-52), BaseDeg = 1,5, HiddenDeg = −2,5,
                                                   // AzimuthSpanDeg = 70, LightMinIntensity = 0 (AC-SCN-10-05), MaxDegPerMinute = 0,3,
                                                   // ventanas: sol S(4,45; 5,50)·(1 − S(18,50; 19,55)); luna S(NightStart; 20,6) y S(NightEnd; 3,40)
                                                   // (los valores viven en Core/Scene/Art/SkyArt; el Designer los ajusta en WP-D2 con las pruebas al día)
}

public readonly struct CelestialState
{
    public bool IsNight { get; }                      // el del legacy
    public double DiskAzimuthDeg { get; }             // (xFrac − 0,5)·70 (el sol o la luna que corresponde)
    public double DiskElevationDeg { get; }           // −2,5 + (Base + Arc·e + 2,5)·vis
    public double DiskVisibility { get; }             // 0 detrás del paisaje
    public bool DiskIsMoon { get; }
    public double LightAzimuthDeg { get; }            // el mismo acimut
    public double LightElevationDeg { get; }          // sol 6 + 49·e; luna 10 + 40·e(h')
    public double LightIntensity { get; }             // sol K(h)·vis; luna (0,04 + 0,12·iluminación)·vis; 0 en el cambio de astro
    public double LegacyStrength { get; }             // 0,45 de noche; clamp(1,2·e; 0,2; 1) de día (time_of_day.dart:64; AC-SCN-10-03)
}

public static class CelestialPlacement
{
    public static CelestialState Evaluate(double hour, double moonIllumination, in CelestialSpec spec);
}

public readonly struct MoonPhaseInfo { public double AgeDays, PhaseAngle, Illumination; }

public static class MoonPhase
{
    public const double SynodicDays = 29.530588853;
    public static readonly DateTimeOffset ReferenceNewMoon;     // 2000-01-06 18:14 UTC
    public static MoonPhaseInfo At(DateTimeOffset utc);          // edad = Mod(t − ref; mes), φ = 2π·edad/mes, iluminación = (1 − cos φ)/2
    public static bool IsSouthernRegion(string? regionCode);     // AR, AU, BO, BR, CL, LS, MG, MZ, NA, NZ, PE, PY, SZ, UY, ZA, ZW (Designer, P2)
}
```

- **Los astros nunca saltan** (AC-SCN-10-05): barrido minuto a minuto (dotnet). En el cambio de astro los dos están a −2,5° y la luz vale **0**; la luz cambia de dirección solo ahí. Con el arco de 10,5° la velocidad máxima medida es **0,257°/min** (la luna que sale, ≈ 20:09), dentro del tope de **0,3°/min** (DT6a-02 con DN6a-03; con el arco de 17° era 0,331).
- **Disco y halo** (DV-63, AC-SCN-11-05): radios en `SkyArt` (sol 1,6°, luna `0,8 ×` el sol = 1,28°; halo `4 ×` el disco de cada astro: 6,4° y 5,12°), los alfas del halo del legacy (0,7 / 0,25 / 0) y, para la luna, × `0,4 + 0,6·iluminación` (DV-57). Las proporciones son contrato y tienen prueba de dotnet (QA-6a-PAR-10, §6.3 `SkyArtTests`); los radios en grados son de arte.
- **Fecha de la fase**: la del reloj del equipo (`IClock.UtcNow`), también con hora forzada. Con el reloj de prueba (2026-10-28 13:00 UTC), gibosa menguante ≈ 94 % (DV-57; los tres vectores del Designer con ± 1 d).
- **Hemisferio**: la región del sistema llega por `ISystemLocale`; sin región, el norte (también en las pruebas, §0.4 de la spec). La prueba del sur fija una región con `SceneHost.ForceRegionForTest("AR")` (para la sesión siguiente). Lo espeja el shader (`u → −u`, `_RmDiskShape.z = −1`).

### 2.7 La luz de cada hora y las luces de la noche (`HourLighting`, `NightLights`; AC-SCN-09-04 a -06, AC-WKT-13-12)

```csharp
public readonly struct HourLook
{
    // Todo derivado de la paleta y la hora (AC-SCN-09-04: una sola paleta manda).
    public SceneColor KeyColor { get; }  public double KeyIntensity { get; }
    public SceneColor AmbientSky { get; }  public SceneColor AmbientEquator { get; }  public SceneColor AmbientGround { get; }   // ×ambient·0,9
    public double ShadowStrength { get; }
    public SceneColor FogColor { get; }  public double FogStart { get; }  public double FogEnd { get; }
    public double ExposureEv { get; }  public double WhiteBalanceTemp { get; }  public double WhiteBalanceTint { get; }
    public double Saturation { get; }  public double Contrast { get; }  public double BloomIntensity { get; }  public double BloomThreshold { get; }
    public double Golden { get; }        // g: 1 a las 6 y a las 18, 0 de 9 a 15 (00-deuda-diseno §4.2)
    public double LowMist { get; }       // bruma baja m(h): 0 a las 5, 0,55 de 6 a 7, 0 a las 9:30 (AC-SCN-15-07)
}

public static class HourLighting
{
    public static HourLook Evaluate(double hour, in ScenePalette palette, in CelestialState sky, in HourLookTable table);
    // El velo de pausa sobre la luz de la hora, en un solo Volume (DT6a-10): saturación ×(1 − 0,3·w) y exposición + w·VeilEv.
    public static PostLook Compose(in HourLook look, double veilWeight, double veilEv);
}

public static class NightLights
{
    public static bool IsNightLegacy(double ambient);                   // < 0,6 (shore_painter.dart:26)
    public static readonly double DuskThresholdHour;                    // 19,428 6 (19:25:43): ambiente 0,6 al anochecer
    public static readonly double DawnThresholdHour;                    // 4,714 3 (4:42:51)
    // DV-60: cada luz (su semilla: la k de la cabaña) cambia en su propio minuto dentro de ±10 min del umbral; fuera, el legacy.
    public static bool IsLit(int lightSeed, double hour, double ambient);
}

public struct NightLightFader     // 0,4 s de fundido hacia el estado (en tiempo real; sigue con reducir movimiento)
{
    public double Level { get; }
    public void Step(bool lit, double dt);
}
```

- **La tabla** (`HourLookTable`, en `Core/Scene/Art/`, del Designer): los keyframes de ART_BIBLE §6.3 con los ajustes de 00-deuda-diseno §4.6 (P-17: el Designer los puede ajustar dejando la tabla al día) y *tonemapping* *Neutral* (R6a-04). dotnet prueba la interpolación y los valores en los keyframes; la "foto" la revisan QA y el Designer.
- **Estrellas y nubes** usan el mismo ambiente; las luces de la noche **no** se tiñen ni se pierden en la niebla (AC-SCN-09-06).
- **Sombras de noche** (04-design-notes §10.3: en Medio, sin sombras; en Alto, 0,25): `ShadowStrength` baja a **0** en lugar de apagar el mapa, así las palabras clave de las sombras de URP quedan **constantes durante toda la sesión** y una hora nueva nunca pide una variante sin calentar (QA-G6aT-07). Cuesta el mapa de sombras de noche en Medio (≈ 1,2 ms de GPU, lo mismo que de día, dentro de la tabla de §4.1).

### 2.8 Estrellas y nubes (`StarField`, `CloudModel`; F-SCN-11; Q-62, D-ART-06)

```csharp
public readonly struct StarPoint { public double X, Y, Radius; }       // en dp de la pantalla del legacy

public static class StarField
{
    public const int Count = 80;  public const int Seed = 42;            // sky_painter.dart:59-61
    // DartRandom(42) con las tres tiradas por estrella EN ORDEN: x = u₁·ancho, y = u₂·horizonte·0,85, radio = 0,6 + u₃.
    public static void Layout(double width, double horizonY, Span<StarPoint> into);
    public static double LegacyDarkness(double ambient);                // clamp((1 − a)/0,65; 0; 1)  (:51)
    public static bool IsShown(double ambient);                         // Q-62 (DV-53): ambiente ≤ 0,85 (6:12 sí, 6:13 no, 17:59 no, 18:00 sí)
    public static double Visibility(double ambient);                    // IsShown ? LegacyDarkness : 0 (el valor de régimen)
    // (0,4 + 0,5·(0,5 + 0,5·sin(2t + i)))·LegacyDarkness(a)·fade  (:65-66); con fade = 1 en régimen es la fórmula de la spec.
    public static double Alpha(int i, double sceneTime, double ambient, double fade);
}

public struct StarFader            // DN6a-05, DV-53: 0,9 s con ease.move cuando IsShown cambia; tiempo real del cuadro
{
    public const double Seconds = 0.9;
    public double Level { get; }                                        // 0-1 (ya con la curva)
    public void Step(bool shown, double dt);                            // sigue con reducir movimiento (es un fundido)
    public void Snap(bool shown);                                       // al abrir, al volver del segundo plano y con ForceHourForTest
}

public readonly struct CloudSpec { public double X0, YRel, SizeDp, Speed; }   // las 7 de sky_painter.dart:29-33

public static class CloudModel
{
    public static ReadOnlySpan<CloudSpec> Legacy { get; }
    public static double LegacyX(int i, double width, double distance, double time);   // :95 (contrato: con la deriva por distancia)
    public static double X(int i, double width, double time);           // D-ART-06 (DV-56): ((x₀ + t·0,004·v) mod 1,2)·ancho − 0,1·ancho
    public static double Y(int i, double horizonY);                     // horizonte·(0,12 + y·0,6)  (:96)
    // DV-56 (QA-6a-PAR-08): el factor de alfa cerca de la vuelta, sin tocar X: S(0; kR·s; 1,1·ancho − X)·S(0; kL·s; X + 0,1·ancho),
    // 0 en el punto de vuelta por los dos lados (kR 1,3 y kL 1,15 por defecto, los extremos de la nube; en SkyArt, del Designer).
    public static double WrapFade(int i, double width, double time, double kRight, double kLeft);
    public static double BodyAlpha(double ambient);                     // 0,08 + 0,5·a²  (:91)
    public static SceneColor BodyColor(in ScenePalette p);              // Lerp(blanco; skyHorizon; 0,25), con el recorte  (:92)
    public static double ShadeAlpha(double ambient);                    // 0,12·a  (:93)
}

public static class SkyBackdrop     // DV-66, DN6a-09, QA-6a-PAR-10: la curva del telón que copia el shader Sky
{
    public static double Fraction(double tau, double tauTop);           // q = clamp(τ/τ_top; 0; 1), τ_top = HorizonY/Focal
    public static double Gradient(double q);                            // 1 − (1 − q)²: 0 → skyHorizon, 1 → skyTop
}

public static class CelestialMask   // QA-G6aT-08: lo que tapa el astro a las estrellas, que copia el shader Stars
{
    // 1 fuera del halo; 1 − alfa del halo (0,7 / 0,25 / 0 del legacy, × vis y, la luna, × (0,4 + 0,6·il)) adentro; 0 adentro del
    // disco (también del lado oscuro de la luna), con el borde suave del disco.
    public static double StarAttenuation(double angleToDiskRad, double diskRadiusRad, double haloRadiusRad, double haloAlphaScale);
}
```

- **Cómo pasan al 3D**: la posición de pantalla del legacy (estrellas y nubes) se **desproyecta con la cámara del contrato** a una dirección del cielo para el tamaño de la ventana: con la respiración en 0 cada estrella y cada nube caen en su píxel del legacy (± 1 dp, AC-SCN-11-03); con la respiración se comportan como puntos en el infinito. Al cambiar la ventana se rehacen (80 + 7 direcciones, sin asignar: el arreglo se reserva para 80).
- **C-04 y C-05 quedaron en la spec**: el Designer aceptó las estrellas y las nubes del legacy en su píxel (04-design-notes §1.1); la base (00-deuda-diseno §4.4 y §4.5) queda descartada.
- **Las estrellas detrás del astro** (AC-SCN-11-01, QA-G6aT-08): las 80 se dibujan aditivas en la cola 1001, después del cielo (que tiene el disco y el halo), así que el *shader* `Stars` multiplica su alfa por `CelestialMask.StarAttenuation` (la misma cuenta, ≤ 8 ALU): adentro del disco no se ve ninguna y adentro del halo se apagan como con el halo del legacy encima. Se eligió esto y no una pasada propia del astro (una cola más, un *shader* más y un *draw call* más): el resultado es el mismo y el costo es de 80 *quads* chicos.
- **El fundido** (DV-53): el alfa de cada estrella es la fórmula del legacy × `StarFader.Level`. En régimen `Level` es 1 si se ven y 0 si no, que es la fórmula de la spec; al cruzar el umbral (a las 18:00 por la hora real o en el time-lapse), entra o sale en 0,9 s desde la oscuridad del legacy de ese cuadro (0,23 a las 18:00).
- LT-82: a las 19 h, `Visibility > 0` en las 80; a las 12 h, 0 (el port cuenta las estrellas visibles; la spec lo admite).

### 2.9 Las cordilleras (`HorizonModel`; F-SCN-19; Q-65, DV-55)

```csharp
public static class HorizonModel
{
    // lake_environment.dart:62: hMax·(0,45 + 0,35·sin(u·6,3 + fase) + 0,2·sin(u·15,1 + 2·fase)); lejano fase 0, frecuencia 1,0;
    // cercano fase 3,1, frecuencia 1,6, 0,7 del alto.
    public static double LegacyProfile(double u, double phase);
    public static SceneColor Color(in ScenePalette p, in SceneColor tone, double depth);   // environment.dart:127-129
    public static readonly SceneColor FarTone, NearTone, SnowTone;                       // #5C6E8A (1,0), #2F4A3A (0,3), #F2F6FA
    // El perfil por acimut (70° = un ancho del legacy, como el astro) y el alto máximo del perfil según la ventana (P-11, DV-65,
    // AC-SCN-19-01): tan Θ = mín(LegacyHeightFraction·alto/focal; tan MaxPeakDeg) = mín(0,12·alto/focal; tan 9°), el hMax del legacy
    // (lake_environment.dart:46) con un tope. Horizontal: 6,843° (= el legacy, 28,6 % del cielo); vertical: 9° (18,3 % en V390,
    // 17,8 % en TV y P360).
    public static double PeakAngleDeg(in SceneCamera cam, in HorizonSpec spec);
    // Q-65: se acercan sin correrse. k(d) = D₀/D(d), D(d) = Dmín + (D₀ − Dmín)·exp(−0,04·d/(D₀ − Dmín)) (lejano 3 500 / 2 500,
    // cercano 1 200 / 600): a 42,2 km, ×1,304 y ×1,887 (00-deuda-diseno §5).
    public static double ApproachScale(double distance, bool far, in HorizonSpec spec);
    // DV-64: la nieve del lejano donde p ≥ 0,58, con borde S(±0,03) y un ruido de valor determinista de ±0,04 en p con período de
    // 1,7° de acimut (las lenguas). Core da la máscara por acimut (la usan la prueba de cobertura y la malla, que la guarda por vértice).
    public static double SnowMask(double profile, double azimuthDeg, in HorizonSpec spec);
}

public readonly struct HorizonSpec { public static HorizonSpec Lake { get; } }  // 0,12; 9°; 0,58 / 0,03 / 0,04 / 1,7°; D₀ y Dmín (en LakeArt)
```

- El acercamiento es en `double` y la forma no depende de `d` (solo la escala): con el bote quieto, quietas.
- `LegacyProfile` con el desplazamiento por distancia del legacy (`distance·0,004`, `·0,008`) queda solo como contrato y no se dibuja (DV-55). La regla de nieve del legacy (picos locales sobre el 75 %, hasta el 82 %) queda como contrato en `HorizonModel.LegacySnowPeaks`, con su prueba, y no se dibuja (DV-64).
- **La fórmula del alto** es la del legacy (`0,12·alto`, QA-6a-PAR-03 y la spec corregida), no la de 04-design-notes §6.2 (`0,30·horizonte/focal`, que en horizontal da 7,18° y no 6,84°): en vertical las dos dan 9°.

### 2.10 El generador del legacy (`DartRandom`; AC-SCN-14-02; Q-61, ADR 012)

```csharp
// Puerto de _Random de la VM de Dart (sdk/lib/_internal/vm/lib/math_patch.dart y runtime/lib/math.cc; BSD-3, con atribución en el
// encabezado): la semilla se mezcla con la función de Thomas Wang de 64 bits (si da 0, 0x5A17); el estado es un MWC de base 2³² con
// A = 0xFFFFDA61 (estado = A·bajo + alto, en 64 bits sin signo); el constructor gira el estado 4 veces; NextInt(max) usa el bajo
// con máscara si max es potencia de 2 y, si no, rechazo (rnd32 − resultado + max > 2³²); NextDouble = (NextInt(2²⁶)·2²⁷ + NextInt(2²⁷))/2⁵³.
public sealed class DartRandom
{
    public DartRandom(long seed);
    public double NextDouble();
    public int NextInt(int max);      // max en [1; 2³¹ − 1] (el legacy pide 1000); fuera, ArgumentOutOfRangeException como el RangeError
    public bool NextBool();
    public void Reseed(long seed);    // sin asignar: el generador de cada segmento se reutiliza
}
```

- **Vectores dorados** (los cuatro de la spec y, del oráculo, 64 tiradas de `NextDouble`, `NextInt(1000)`, `NextInt(1 << 26)` y `NextInt(7)` para las semillas 0, 1, 17, 42, −1, 7 919, 55 450, 1 000 020 y 2⁴⁰ + 3), comparados **bit a bit**.
- No reemplaza a `IRandom` del simulador (ese no es el generador de Dart y el simulador sin ruido no lo consume).
- **Licencia** (QA-G6aT-19): el SDK de Dart es BSD-3 y el repo es MIT (compatibles). El encabezado de `DartRandom.cs` cita el origen y remite a `THIRD_PARTY_NOTICES.md` (raíz del repo), que reproduce el aviso de copyright, las tres condiciones y el descargo de la licencia del SDK de Dart. La distribución binaria lo lleva: `NoticesPostprocessor` lo copia junto al ejecutable en cada build de Windows y `BuildOutputInspector` falla si falta (Android, cuando haya build, en la It. 10). `RepoHygieneTests` exige el archivo, con esas partes, si existe `DartRandom.cs`.

### 2.11 La orilla determinista (`ShoreEnvironment`, `LakeEnvironment`, `ShoreSegments`; F-SCN-14, F-SCN-15; LT-75 a LT-77)

```csharp
public enum EnvironmentId { Lake, River, Coast, Regatta }      // environment.dart:10 (el índice entra en la semilla)
public enum PropKind { Pine, Cabin, Pier, Building, Boathouse, Bridge, LaneBuoy, Lamp, Cliff, Lighthouse, BeachUmbrella,
                       Grandstand, FinishTower, Flag, DistanceMarker }       // :12-15, en ese orden
public readonly struct ShoreProp { public double X, Z, Scale; public PropKind Kind; public int Seed; }   // :18-37

public abstract class ShoreEnvironment
{
    public const double SegmentLength = 50, BankX = 9, VisibleRange = 400;  // :43-47
    public abstract EnvironmentId Id { get; }
    public virtual int MaxVisibleProps => 120;                              // :50
    public abstract double WaveAmplitude { get; }  public abstract double WaveScale { get; }   // :57-61
    public static long SeedFor(EnvironmentId id, long k) => (long)id * 1000003 + k * 7919 + 17;   // :77
    // Llena `into` (sin vaciarlo) con los objetos del segmento k en el orden del legacy.
    public abstract void Generate(long k, DartRandom rnd, ShorePropBuffer into);
}

public sealed class LakeEnvironment : ShoreEnvironment { /* 0,25 / 2,5; Generate = lake_environment.dart:19-40 */ }

public sealed class ShoreSegments
{
    public ShoreSegments(ShoreEnvironment environment, int poolSize = 32);
    public ReadOnlySpan<ShoreProp> PropsForSegment(long k);                 // :74-80 (caché por instancia, no estática: DT6a-15)
    // :89-114: z en [d + 1; d + 400], de lejos a cerca; con el tope: se conservan los hitos, se completa con las boyas más cercanas,
    // si aún sobra se descartan los hitos más lejanos, y se reordena. Olvida los segmentos de atrás (:94, :116-120).
    public int VisibleProps(double distance, ShorePropBuffer into);
    public int CachedSegments { get; }                                      // AC-SCN-14-06: no crece con la distancia
}
```

- **El orden de las tiradas** es el del legacy: por orilla (izquierda y después derecha) `z` inicial; por pino `x`, escala, variante (`NextInt(1000)`) y el paso a la `z` siguiente (AC-SCN-15-02). Las expresiones con los mismos operandos en el mismo orden: `side·((BankX + 1) + r·6)`, `0.7 + r·0.6`, `z += 6 + r·6`, `while (z < z0 + 50)`.
- **Sin asignaciones**: los segmentos viven en búferes de un *pool* (24 objetos por segmento como máximo: 18 pinos, el muelle y la cabaña); un segmento que se olvida devuelve su búfer. El ordenamiento es por inserción sobre un arreglo propio (≈ 100 objetos ya casi ordenados).
- **El tope y los hitos** (AC-SCN-14-05) se prueban con `SceneSeeds.CapEnvironment`, un escenario de prueba (en `Tests/Support`) con muchas boyas y muchos hitos, contra la función del legacy corrida en el oráculo con la misma lista (§6.2). Los topes 60 y 200 son de la It. 8.
- **Empates en `z`** (QA-G6aT-15 a): `List.sort` de Dart no es estable con más de 32 elementos (`environment.dart:101`, `:111`), así que con dos objetos en la misma `z` el orden (y, en el corte del tope, cuál queda) puede variar. `CapEnvironment` se arma con `z` **distintas** alrededor del corte (la prueba lo afirma antes de comparar) y todas las comparaciones de listas visibles contra el oráculo ordenan cada tramo de `z` iguales por (tipo, `x`, semilla) en los dos lados. En el lago no hay empates (las `z` de los pinos son sumas de tiradas; muelle y cabaña caen en `z₀ + 25` y `z₀ + 10`), pero la regla vale igual.
- **La caché del legacy es estática y global** (`environment.dart:53`); acá es de cada `ShoreSegments` (una por sesión de escena): el resultado no cambia y una sesión nueva no hereda nada (DT6a-15, AC-SCN-01-03).

### 2.12 Luz y niebla de la orilla; el agua degradada (`ShoreShading`, `DegradedWater`; F-SCN-20, F-SCN-13)

```csharp
public static class ShoreShading
{
    public static SceneColor Tint(in SceneColor c, double ambient);   // r·a, g·a, b·min(1; a + 0,12)  (shore_painter.dart:47)
    public static double Fog(double relZ);                            // clamp((relZ − 80)/320; 0; 1)·0,8  (:48-49)
    public static SceneColor Shade(in SceneColor c, double relZ, in ScenePalette p);   // lerp(Tint; fog; Fog)  (:45-50)
    public static double ExtendedFog(double relZ);                    // la de 00-deuda-diseno §6.5: igual hasta 400 m y hasta 1 en 1 200 (terreno lejano)
}

public readonly struct ShimmerLine { public double Y, HalfWidth, CenterX, Alpha; }

public static class DegradedWater
{
    // water_painter.dart:79-90: de arriba a abajo lerp(waterFar; fog; 0,5), waterFar, waterNear (el lago no tiñe, environment.dart:63).
    public static void Bands(in ScenePalette p, out SceneColor top, out SceneColor middle, out SceneColor bottom);
    // :92-103: t = Mod(i/10 + Mod(d·0,02; 0,1); 1), y = horizonte + altoAgua·t², medio ancho ancho·(0,15 + 0,35·t),
    // alfa 0,05 + 0,1·(1 − t)·fuerza (el dato del legacy, contra el oráculo); centro = la x del astro que se ve (DV-59).
    public static ShimmerLine Line(int i, double distance, double width, double horizonY, double waterHeight, double centerX, double strength);
    public static double Phase(double distance);                      // Mod(d·0,02; 0,1) en double: lo que recibe el shader (Q-68)
    // DV-59 (DN6a-06): lo que se ve = el alfa del legacy × la visibilidad del astro que se ve (0 en el cambio de astro). El shader
    // multiplica con _RmShimmer.w; Core da la función para la prueba por minuto.
    public static double ScreenAlpha(double legacyAlpha, double diskVisibility);
}
```

### 2.13 Terreno, colocación y relleno (`TerrainProfile`, `PropPlacement`, `ShoreFill`; AC-SCN-15-05, -07)

- `TerrainProfile.Height(EnvironmentId, double x, double zWorld) : double`: el corte de 00-deuda-diseno §6.2 (fondo bajo el agua de 7 a 9 m, playa de 9 a 10, talud de 10 a 16, ladera a la izquierda y pradera a la derecha), la orilla que serpentea **solo hacia afuera** (`|x_orilla| = 9 + 0,5·n(z/41)`, ruido de valor determinista por lado, en `double`) y las envolventes. Continua entre segmentos (costura < 1 mm, dotnet).
- `PropPlacement` (DD6a-10, con DT6a-08):
  - **pino**: `x` y `z` del legacy, sin tocar; pie = el mínimo del terreno bajo el tronco − 0,20 m; nunca bajo el agua, nunca más de 2,5 m sobre ella (AC-SCN-15-05);
  - **cabaña**: el ancla `(13; z₀ + 10)` del legacy **no se mueve nunca**: es el centro del frente, que mira a la cámara; la profundidad crece hacia `+z` (4 m) y, si un tronco queda adentro de la huella + 0,3 m, se prueba 3 m y después 2,5 m; si aún choca, ese pino pierde las gradas bajas hasta pasar el alero (la "poda" del Designer). La regla del Designer que **corría** la cabaña hasta 0,5 m en `z` movería su eje hasta 7,5 dp en W1280 a 25 m (DT6a-08). dotnet en 100 km: ningún tronco adentro de una cabaña y el ancla igual al legacy;
  - **muelle**: el tablero de `x` −6 a la playa, a 0,40 m; los pilotes hasta el fondo; el ancla `(−9; z₀ + 25)` igual al legacy.
- `ShoreFill` (DD6a-11): juncos, rocas, bosque cercano, matas de la pradera, pasto y helechos con semillas `SeedFor(id; k) + sal·104 729` (sal 1 a 6): **nunca** consume el generador del legacy (dotnet: el paisaje del legacy es el mismo con y sin relleno). En `|x| < 18`, solo lo bajo (≤ 1,2 m, P-09): juncos de 0,6 a 1,2 m (DT6a-09, aceptado por el Designer).
- **Cada segmento se rellena solo** (QA-G6aT-15 b): el disco de Poisson del bosque cercano (6 m, DN6a-17) se muestrea por segmento en `[z₀ + 3; z₀ + 47]` (un margen interno de la mitad del radio), sin leer los puntos del vecino: dos segmentos vecinos quedan a ≥ 6 m en la costura y el resultado no depende del orden de los pedidos. dotnet: pedir `k + 1` antes que `k` (y los segmentos 0 a 200 en orden aleatorio con semilla fija) da el mismo relleno, bit a bit.
- **El bosque y la envolvente** (DN6a-14, DN6a-17): árboles sueltos en `|x|` 18-45 y `z` relativo ≤ 120 m; más allá, la copa en la malla del terreno (`vegetation.canopyFar`); la envolvente de la izquierda es `2,2 + 0,20·|x|` (en `LakeArt`).

### 2.14 Geometría pura (`Core/Scene/Geometry`; AC-SCN-15-06, AC-SCN-01-08)

`MeshData` guarda posiciones, normales, colores de vértice (oclusión y peso de viento), UV y triángulos en arreglos que se reservan una vez. Los constructores toman una **receta** (`Core/Scene/Art/LakeArt.cs`, del Designer) y una semilla:

| Constructor | Lo que fija la prueba de dotnet (con la receta por defecto) |
|---|---|
| `PineMesh.Build(recipe, seed, lod, mesh)` | alto 6,0 m × escala ± 10 %, ancho máximo ≈ 2,6 m × escala, 3 gradas, la paleta `seed mod 3`, silueta irregular (ningún radio igual en dos lóbulos vecinos), LOD 1 200 / 400 / 120 triángulos |
| `CabinMesh` | cuerpo 4 × 2,6, techo hasta 4,2 con alero, puerta 0,8 × 1,4, ventana 0,8 × 0,7 (± 10 %), la ventana como submalla aparte (se enciende) |
| `PierMesh` | tablones de 3 m hacia el centro del lago con la cubierta a 0,4 m; pilotes |
| `HullMesh` | loft de 24 secciones de `z` 5,3 a 13,5 (8,2 m), centrado en `x` 0; el punto a 9,5 m en la línea de crujía |
| `TerrainChunkMesh` | una orilla de un segmento de 50 m con `TerrainProfile`, bordes iguales al segmento vecino; el trozo lejano (450-1 500 m, 8 m de paso) sin la franja sumergida y con la orilla a +0,4 m (§2.1, QA-G6aT-21) |
| `HorizonStripMesh` | 260 columnas de −65° a +65° (la tira del cielo) |
| `RockMesh`, `ReedTuftMesh`, `GrassCardMesh` | las de 00-deuda-diseno §6.4 |

Unity solo copia los arreglos a un `Mesh` (con búferes nativos persistentes: sin GC).

### 2.15 El agregado (`SceneSession`, `SceneFrame`)

```csharp
public sealed class SceneSessionOptions
{
    public CelestialSpec Celestial { get; init; }   public BreathingSpec Breathing { get; init; }
    public FlightSpec Flight { get; init; }         public AerialSpec Aerial { get; init; }
    public HorizonSpec Horizon { get; init; }       public HourLookTable Look { get; init; }
    public double VeilEv { get; init; }             // la exposición del velo: −0,50 EV de partida (DN6a-19; WP-D2 la ajusta con la sonda)
    public string? RegionCode { get; init; }        // hemisferio de la luna (ISystemLocale; null = norte)
}

public readonly struct SceneTickInputs
{
    public double Now { get; }                       // monótono (s)
    public DateTimeOffset LocalNow { get; }  public DateTimeOffset UtcNow { get; }
    public SceneInputs Workout { get; }              // la última lectura y la fase
    public int? ForcedHour { get; }                  // SceneSettings.HourOverride
    public bool ReduceMotion { get; }
    public double WidthDp { get; }  public double HeightDp { get; }
    public double VeilWeight { get; }                // 0-1, de PauseVeilMotion
}

public sealed class SceneSession
{
    public SceneSession(ShoreEnvironment environment, SceneSessionOptions options);
    public SceneState State { get; }
    public ChaseCameraDirector Camera { get; }
    public void Open(double now);                    // velocidad, distancia y tiempo en 0 (AC-SCN-01-03); hora visible = la efectiva
    public void Tick(in SceneTickInputs inputs, ref SceneFrame frame);            // un cuadro, paso acotado
    public void Advance(in SceneTickInputs inputs, double seconds, ref SceneFrame frame);   // pruebas y arnés: pasos de 1/60 s
    // Controles de prueba (internal): distancia (ForceDistanceForTest), tiempo de escena fijo (FreezeSceneTimeForTest), hora forzada
    // a un minuto (ForceHourForTest, con los fundidos en su valor final), respiración en 0 (BreathingOffForTest).
    // La región, la escena demorada, el slot de prueba y los de las tomas son de SceneHost (§3.1).
}

public struct SceneFrame
{
    // Cámara (pose y matriz), paleta, astros (con el radio del disco y del halo y el espejo del sur), luz y post (PostLook),
    // estrellas (80 direcciones y alfas, ya con el fundido), nubes (7, con el fundido de la vuelta), cordilleras (escala y alto),
    // agua (los tres colores, las 10 líneas, la fase y la visibilidad del astro), niebla, bruma, luces de la noche (nivel por cabaña
    // visible), distancia (double), velocidad, tiempo de escena, la hora visible, y "dibujar" (false con la escena oculta).
    // WarmUpAll (solo el dibujo de calentamiento, §3.4): estrellas, ventanas y nubes a la vista para que el dibujo toque todo.
}
```

- `SceneFrame` tiene sus arreglos (80 estrellas, 7 nubes, 10 líneas, las luces) reservados al abrir: `Tick` solo escribe.
- La sesión es **una por inmersivo** (AC-SCN-01-03): se abre en el toque, sigue en pausa y con la sesión terminada (el tiempo corre), y se descarta al cerrar. La geometría y los materiales del mundo **no** son de la sesión (§3.4).

### 2.16 Preparación y adaptación (`SceneWarmupPolicy`, `ThermalAdaptation`)

- `SceneWarmupPolicy.SliceMs(bool workoutAtRest, bool screenBlackAndStill, bool immersiveOpen, int finiteMotions, bool hudBuildFrame) : double`: **3 ms** con la pantalla negra y quieta o con Workout en reposo y nada moviéndose; **2 ms** con el inmersivo abierto, sin movimiento finito y fuera del cuadro en que se arma el HUD (la escena demorada, **también después de la cascada**: AC-WKT-13-15 corregido); 0 en cualquier otro caso (la cascada, el vuelo, una transición).
- **No hay pasos indivisibles** (QA-G6aT-03). Cada paso del armado (§3.4) se parte en unidades de costo acotado y conocido: una fila de una textura procedural, una malla (≤ 1 200 triángulos), un material, **una variante de *shader*** (`WarmUpProgressively(1)`), un `Tick` sintético o **el único dibujo de calentamiento**, que con los *shaders* ya calentados cuesta lo que un cuadro normal de la escena (≤ 1,5 ms de la escena más el pedido de URP, §4.1): por eso se permite en cualquier cuadro en que se permitiría dibujar la escena, nunca durante un movimiento finito. La política devuelve el tramo y `SceneWorldBuilder` corre unidades mientras quede tiempo (con `Stopwatch`), al menos una por cuadro con tramo > 0. SWP lo prueba (dotnet) y SLC mide el peor cuadro de la escena demorada (PlayMode, §6.3); `[PERF]` lo mide en el build (§4.5).
- `ThermalAdaptation` (ARCHITECTURE §20.3): con **tiempos de trabajo** por cuadro (GPU de `FrameTimingManager`; de respaldo, CPU del hilo principal más el *render thread*, sin la espera de presentación), disparo a 13 ms de GPU (15 de CPU) sostenidos 3 s; orden **resolución** (pasos de 0,05 entre 0,75 y 1, como mucho uno cada 10 s; subir con 30 s holgados bajo 10 ms) → *reflejo* (no hay en la 6a: se salta) → **distancia de sombras** → **densidad de vegetación** → **nivel**. Devuelve una `QualityAction`; nunca baja la tasa de cuadros. **Cuándo mide** (QA-G6aT-10): solo los cuadros en que la escena se dibujó; con `renderFrameInterval` 2 (el diálogo quieto, P-07), fuera del inmersivo o con la escena oculta, **suspendida** (no suma muestras y al volver arranca la ventana de 3 s de cero). Nunca usa el intervalo entre cuadros: un cuadro de 33 ms de intervalo con 8 ms de trabajo es holgado. Probado en dotnet con series de tiempos (AC-SCN-01-13), entre ellas una a 30 fps con 8 ms de trabajo que no dispara.

### 2.17 `SlotIds`

Las constantes de ASSET_SLOTS §1 (los 15 `PropKind` con su `prop.*`, `sky.*`, `water.*`, `terrain.layer.*`, `vegetation.*`, `horizon.*`, `boat.*`) más `vegetation.canopyFar` (DD6a-22). dotnet: sin repetidos y un id por `PropKind`. Unity EditMode: cada id tiene su `.asset` en `Slots/`, con el procedural asignado salvo que el usuario haya asignado otro (§3.7).

### 2.18 El contraste de las píldoras (`HudContrast`; AC-WKT-17-13)

El método de `Contrast.cs` (composición sRGB sin redondear) contra `glass.fallback` sobre blanco (`#2A2F37`): la hecha, el relleno y el borde de la hueca de cada tipo con su `.hud` (`step.work.hud` `#FF5D7F`, `step.cooldown.hud` `#37A2CB`, `step.unknown.hud` `#7D9BA9`) ≥ 3:1. El borde de la hueca contra su propio interior al 18 % no llega a 3 ni con `.hud` (2,93 a 3,02): **excepción contada**, como la pista al 30 % (00-deuda-diseno §9.3). Los textos y valores del HUD no cambian: la composición sigue siendo sRGB sobre la escena (§3.2), así el peor fondo es el mismo de la 4b.

---

## 3. La escena URP (WP-B, con los shaders de WP-D)

### 3.1 Piezas y ciclo de vida

```
SceneHost (C#, Scene)                      ← AppRoot.LateUpdate / ShellHandle.Tick del arnés: Tick() una vez por cuadro, entre
 │                                            el BeginTick y el EndTick del observador de memoria (§4.4)
 ├── SceneWorldBuilder                      arma el mundo por tramos (precarga), desde SceneWorldDefinition y los slots
 ├── SceneShaderWarmup                      las variantes por tramos y, en el Editor, la espera de la compilación asíncrona
 ├── SceneQualityApplier                    el nivel en la cámara, la RT, el Volume y los materiales (§4.2)
 ├── SceneSession (Core)                    una por inmersivo: Open en el toque, Close detrás del negro
 ├── SceneDirector                          aplica SceneFrame (sin asignar)
 ├── SceneRenderer + SceneRenderTarget      el pedido de render y la RT sRGB (ventana × resolución del nivel)
 └── [scene-root] (GameObject, capa RowMateScene, inactivo fuera del inmersivo y de la precarga)
      ├── scene-camera      Camera (enabled = false SIEMPRE: solo se dibuja a pedido) + UniversalAdditionalCameraData (post y HDR por
      │                     nivel; dithering apagado; sin TAA: §5.1)
      ├── scene-key-light   Light direccional (sombras por nivel; de noche, fuerza 0 sin apagar el mapa: §2.7)
      ├── scene-volume      Volume global (perfil creado en ejecución, sin grano)
      ├── scene-sky         cielo con el disco y el halo, estrellas (enmascaradas por el astro), nubes (colas 1000 a 1002)
      ├── scene-horizon     las dos tiras (cola 1003)
      ├── scene-water       el plano del agua degradada
      ├── scene-terrain     un pool de trozos por segmento
      ├── scene-shore       (sin hijos por objeto: dibujos instanciados de ShoreStreamer y FillStreamer)
      ├── scene-boat        el casco provisorio y su sombra
      └── scene-overlay     los degradés de pantalla (capa del HUD, DV-67: apagados en la escena sola)
```

| Estado (`SceneReadiness`) | Qué pasa |
|---|---|
| `NotLoaded` | nada armado; `Preload()` empieza a armar |
| `Loading` | `SceneWorldBuilder` avanza en tramos (§2.16); no hay pasos indivisibles |
| `Ready` | todo armado; las variantes calentadas; los `Tick` sintéticos y **el único dibujo de calentamiento** hechos, fuera de pantalla (§3.4); en el Editor, además, nada de la escena compilando; la RT existe; no se dibuja nada hasta `Open` |
| `Failed` | un paso falló: un aviso en el registro, el inmersivo sigue con el fondo provisorio (nunca un error que frene la sesión) |

**Controles de prueba de `SceneHost`** (`internal`; los ven las pruebas, `RowMate.Tests.UiSupport` y el arnés; ninguno llega a lo que se instala, §0.4 de la spec):

| Control | Para qué |
|---|---|
| `DelayReadinessForTest(double seconds)` | "escena demorada" (AC-SCN-01-07, CAP-24, VID-11; QA-G6aT-03): el próximo `Open` encuentra el mundo sin armar (descarta lo precargado) y `Ready` no llega antes de `seconds` desde el `Open` aunque el armado termine antes; el armado corre por tramos como en producción |
| `ForceRegionForTest(string? region)` | la región de la luna para la sesión siguiente (§0.4 de la spec) |
| `SetScrimsForTest(bool)` | los degradés de pantalla apagados en la escena sola (DV-67; los usa el arnés junto con `HideForCapture`) |
| `SetStarsForTest(bool)` | la sonda de las estrellas detrás del astro (SLC) |
| `RenderWithoutPostForTest(RenderTexture)` | el mismo cuadro sin el `Volume` de la hora ni el *bloom*, para las sondas de color "antes del ajuste de la hora" (la ventana `#FFE082`, AC-SCN-09-06; QA-G6aT-13) |
| `StampFrameForTest(bool)` | la sonda S6a-7: el número de cuadro pintado en una esquina de la RT de la escena (§3.2) |
| `TickObserver` (`Action<bool>`) y `AllocateInTickForTest` | la medida de memoria y su control positivo (§4.4) |
| `ForceTierForTest(QualityTier)` | un nivel para las pruebas de presupuesto, aplicado por `SceneQualityApplier`; lo que es del asset de URP, con `TierScope` (§4.2) |
| Los de `SceneSession` | distancia, tiempo de escena, hora a un minuto, respiración en 0 (§2.15) |

- **Abrir** (`Open`, en el cuadro del toque): crea la `SceneSession` (en 0) y **empieza a integrar desde ese cuadro** aunque no dibuje (AC-SCN-02-05: "la escena corre desde el toque"). Si no estaba lista, sigue armando (escena demorada, §3.9).
- **Cerrar** (`Close`, a los 400 ms de la salida, cuando el negro la cubre): deja de integrar y de pedir dibujos; desactiva `scene-root`. El mundo queda armado (P-14) salvo `Application.lowMemory`, que lo desarma (se rearma en la próxima precarga).
- **Un solo `Tick`** por cuadro (ARCHITECTURE §14.1): avanza la sesión, aplica el cuadro y pide el dibujo. Sin `Update` en cada objeto.

### 3.2 Cómo llega la escena a la pantalla (DT6a-05; ADR 019; nuevo ADR 022)

**El problema.** La UI se dibuja en una RT `R8G8B8A8_UNorm` con `forceGammaRendering` (composición sRGB, ADR 019) que el presentador copia a la pantalla con un panel *Overlay*. Si la escena se dibujara en la pantalla y la UI quedara transparente encima, el vidrio se compondría en lineal: `#070D16 @86 %` sobre blanco daría `#6A6A6C` y no `#2A2F37` (R-T1), y AC-WKT-17-13 se rompería. Además, el arnés lee la RT de la UI.

**La decisión.**

1. La cámara de la escena **nunca** se dibuja sola (`enabled = false`). `SceneHost.Tick` arma el cuadro y llama `SceneRenderer.Render()`: `RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = rt })` (con su objeto de pedido reutilizado) sobre una RT **`R8G8B8A8_SRGB`** del tamaño de la ventana en píxeles **por la resolución del nivel** (Alto 1,0; Medio 0,85; Bajo 0,75; y la que baje la adaptación) (`SceneRenderTarget`, se rehace al cambiar la ventana o la resolución; la escena **no** se reinicia). Los assets de URP quedan con `renderScale` 1: la resolución es de nuestra RT y no toca ningún asset (QA-G6aT-05).
2. El elemento **`scene-surface`** (`Presentation/Workout/SceneSurface`, el primer hijo del HUD, `position: absolute` a pantalla completa, `pickingMode = Ignore`) tiene la RT de fondo (`Background.FromRenderTexture`, `scale-to-fit`). **Filtro** (el `filterMode` de la RT, que pone `SceneRenderTarget`): de punto si la RT mide lo mismo que la ventana (Alto); **bilineal** si está escalada (Medio, Bajo, la adaptación): así no se ve a bloques (QA-G6aT-09). El panel gamma la muestrea y la escribe en la RT de la UI: los bytes de la escena llegan como en la RT; encima, el HUD se compone **en sRGB**, como sobre el fondo provisorio.
3. Orden en un cuadro del *player*: `LateUpdate` de `AppRoot` (el `Tick` de la escena y su dibujo) → la UI (*offscreen*, muestrea la RT recién dibujada) → el presentador. **Cero cuadros de atraso** entre la escena y el HUD, y el panel vuelve a mostrar la RT **en cada cuadro**, aunque el HUD no cambie: lo comprueba la sonda S6a-7; si el panel no repinta solo, `SceneSurface` pide `MarkDirtyRepaint()` en cada cuadro en que la escena se dibujó (un elemento, sin asignar). En el arnés (modo edición) es el mismo orden: el `Tick` del anfitrión antes del repintado del panel.
4. Con `OnDemandRendering.willCurrentFrameRender == false` (30 fps) no se pide el dibujo.

**Sondas de WP-B0** (antes de construir nada encima; si una falla, el respaldo de la última columna y el ADR lo dice):

| Sonda | Pasa si | Respaldo |
|---|---|---|
| **S6a-1** El color | un *quad* sin luz de `#0B4FA8`, `#FFFFFF`, `#7F7F7F` y `#06122A` en la escena se lee en la RT de la UI ± 1 por canal; el vidrio sobre la parte blanca da `#2A2F37` ± 1; lo mismo leyendo la RT de la escena directo | RT `R8G8B8A8_UNorm` con la conversión a sRGB en el último pase (la *keyword* `_LINEAR_TO_SRGB_CONVERSION` o una pasada propia) |
| **S6a-2** El dibujo a pedido | `SubmitRenderRequest` en modo edición (*batchmode*, sin `-nographics`) y en *Play* dibuja: post (*Volume*), sombras de la luz direccional, `Graphics.RenderMeshInstanced` con `RenderParams.camera` y `MaterialPropertyBlock` con arreglos por instancia | `Camera.Render()` (URP lo admite) en los dos; si el instanciado no entra al pedido: `CommandBuffer.DrawMeshInstanced` en un `ScriptableRenderPass` propio (sin sombras propias: las de los objetos cercanos con un `ShadowCaster` aparte) |
| **S6a-3** 0 B | 120 cuadros reales con el pedido, 100 dibujos instanciados y el `Volume` cambiando: 0 asignaciones con `FrameAllocations.WatchScene` (el `Tick` y el pedido adentro de lo medido, §4.4), informadas en dos cuentas (lo nuestro y el dibujo de URP), y su control positivo (`byte[64]` en el `Tick`: 120 de 120) en la misma corrida | la cámara habilitada con `targetTexture` en el *player* (un cuadro de atraso, invisible) y el pedido solo en el arnés; en ese caso el dibujo se mide con `RenderPipelineManager.beginCameraRendering`/`endCameraRendering` de la cámara de la escena, así sigue adentro de la medida. Si URP asigna dentro del dibujo solo en el Editor, esa cuenta se mide en el build de desarrollo (`-rowmate-perf-scene`, §4.5) y queda escrito en ADR 022 |
| **S6a-4** Determinismo | dos lecturas del mismo cuadro en dos corridas del Editor iguales byte a byte, con `ShaderUtil.allowAsyncCompilation = false`, *dithering* de la cámara apagado y sin grano | la tolerancia de §5.1, justificada por QA |
| **S6a-5** RT escalada | con 0,85 la RT mide 0,85 × la ventana y `scene-surface` la estira con filtro bilineal: un borde vertical nítido de la escena pasa de un tono al otro en ≤ 2 px de la RT de la UI y sin escalones de 2 px | la RT al tamaño de la ventana y el `renderScale` de una copia del asset **solo en el *player*** (en el Editor, el nivel Alto) |
| **S6a-6** La matriz | con `Camera.projectionMatrix` = la de Core, `WorldToViewportPoint` × el tamaño de la RT (no `WorldToScreenPoint`, que usa el de la pantalla) de las marcas de AC-SCN-02-02 da el píxel del legacy ± 1 dp, y las cascadas de sombra y el recorte (*culling*) no dejan huecos | la cámara física con `lensShift` y el signo que dé la prueba |
| **S6a-7** Cero cuadros de atraso | con `StampFrameForTest`, el número de cuadro pintado en una esquina de la RT de la escena se lee en la RT de la UI **en el mismo cuadro**, en 120 cuadros seguidos, también en cuadros sin ningún cambio del HUD (QA-G6aT-09) | `SceneSurface.MarkDirtyRepaint()` en cada cuadro dibujado; si aun así hay un cuadro de atraso, se escribe en ADR 022 y el vuelo se mide con ese atraso |

### 3.3 La convivencia con UI Toolkit (el puerto, el HUD, el velo, el fondo provisorio)

```csharp
// Presentation/Workout/IImmersiveScene.cs — en Presentation porque lleva una Texture (DT6a-03). Lo implementa Bootstrap/SceneBridge
// sobre Scene/SceneHost. null en UiContext = sin escena (todas las pruebas y tomas de las It. 2 a 5: el fondo provisorio de siempre).
public interface IImmersiveScene
{
    IReadOnlyObservable<SceneReadiness> Readiness { get; }
    Texture? Output { get; }
    event Action? OutputChanged;            // la RT se rehízo (otra ventana)
    void Preload();                          // Workout en reposo (P-14)
    void Open(double now);                   // el cuadro del toque
    void BeginFlight(double now);            // el primer cuadro en que se ve (≥ 220 ms)
    void ShowInPlace(double now);            // reducir movimiento o la escena demorada (con la rampa de la respiración)
    void SetPauseVeil(double weight);        // 0-1
    void BeginExit(double now);
    void BeginAerial(double now);
    void Close();                            // detrás del negro (400 ms de la salida)
}
```

- **El HUD encima**: `HudView` arma `SceneSurface` primero (antes de `HudBackdrop`, que queda debajo) y las tarjetas, la barra, los controles, la cinta y la llave siguen como en la 5 (AC-WKT-13-02, AC-SCN-01-02). Las tarjetas no bajan de opacidad por la escena (AC-WKT-17-13).
- **`SceneSurface` y el puerto** (QA-G6aT-16): la superficie solo **muestra** `Output`; nunca llama `Preload`, `Open` ni ningún otro paso del ciclo de vida (los llaman `WorkoutScreenView` y `ImmersiveWorkoutPage`). Se suscribe a `OutputChanged` en `AttachToPanelEvent` y se desuscribe en `DetachFromPanelEvent` (y en `Dispose`), así una superficie que se desarma (la de `HudWarmup`, que arma y desarma dos `HudView` en el arranque, o la de un HUD que se cierra) no deja suscripciones colgadas: sin avisos de "objeto que ya no existe" (AC-WKT-13-09). `HudWarmupSceneTests` lo afirma con `FakeImmersiveScene`: 0 suscriptores después de desarmar y 0 llamadas a `Preload` u `Open`.
- **El presupuesto** (AC-WKT-18-21): con la escena a la vista, `HudBackdrop` está en `display: none`: el conteo de `HudBudgetTests` no cambia (56-57). Durante el fundido de 0,6 s de la escena demorada se ven los dos (+1). `HudBudgetTests` suma su caso con `SceneMode.Real`.
- **El velo de pausa** (AC-WKT-13-12): `PauseVeilMotion` (de la 4b) da el alfa 0 → 0,15 en 300 y vuelve en 240; `HudView` se lo pasa al fondo provisorio (su tinte, como en la 4b) **y** a la escena como peso `alfa/0,15`. En Core, `HourLighting.Compose` multiplica la saturación por `1 − 0,3·w` y suma `w·VeilEv` a la exposición en el **mismo** `Volume` de la hora (no un segundo `Volume`: URP interpolaría valores absolutos y de noche desaturaría menos; DT6a-10). La UI no pasa por el post. WP-D ajusta `VeilEv` con la sonda (la luminancia del cielo a 30° baja a 0,85 ± 0,03).
- **El fondo provisorio** (AC-SCN-01-01, DD6a-23): solo mientras la escena no está; se tiñe con `skyTop` y `waterNear` de la hora (el `BackdropTexture` de 1 × 100 se rellena otra vez al cambiar el minuto: 400 B, sin asignar con un arreglo propio). Los *tokens* `backdrop.*` quedan con los valores de las 12 h para sus pruebas.
- **Los degradés de pantalla** (DV-67, C-18): son de la capa del HUD (ART_BIBLE §3.2) y se dibujan con `ScreenOverlay` en la cámara de la escena (cola 3500, antes del post, así los alcanza el velo), con 0 elementos de UI. No llegan a la zona del bote.
- **Escena sola** (las tomas CAP-01 a -12 y VID-05, -06, -10; QA-6a-PAR-02, QA-G6aT-12): `HudView.HideForCapture()` (solo el arnés: `internal`) deja en `display: none` **los hermanos de `scene-surface`** (todo el HUD menos la superficie, que queda a la vista), y el arnés apaga los degradés con `SceneHost.SetScrimsForTest(false)`. Las sondas de color de 04-design-notes §10.4 y la comparación con LEG-01 a -07 se miden así, sin degradés.

### 3.4 El mundo, los slots y la precarga

- **Sin escenas aditivas en la 6a** (DT6a-04; ADR 015 dice `World.unity` + `Environments/Lake.unity`): todo el contenido es procedural o entra por un slot, así que una escena `.unity` no aporta nada y suma estado asíncrono (`LoadSceneAsync`), *build settings* y un *.unity* que habría que escribir a mano o generar. El mundo lo arma `SceneWorldBuilder` bajo `scene-root` (en `Boot.unity`, que es la única escena). Las escenas aditivas pueden volver en la 8 si el Designer quiere colocar cosas a mano; el ADR 015 se corrige.
- **`SceneWorldDefinition`** (`Scenes/SceneWorld.asset`, lo crea el *bootstrapper*): referencia el `EnvironmentDefinition` del lago (sus slots: `prop.*`, `terrain.layer.*`, `vegetation.*`, `horizon.lake`, `water.profile.lake`), `sky.profile`, `sky.clouds`, `sky.moon`, `boat.*` y los materiales de `Art/Materials/Scene/`. `AppRoot` lo recibe por `UiAssets` (lo serializa `BootSceneBuilder`).
- **Los pasos del armado**, todos partidos en unidades de costo acotado (§2.16; ninguno indivisible):
  1. cámara, luz, `Volume`, RT;
  2. los slots (resolución y validación, §3.7);
  3. texturas procedurales (atlas de nubes de 512 × 256 con las manchas del legacy, DN6a-18; luna de 256²; ruido de 256²; la capa lejana de 1 024 × 512 solo en Alto): por filas;
  4. mallas (pinos ×3 formas ×3 LOD, cabaña, muelle, casco, roca, juncos, pasto, helechos, las tiras del horizonte, los trozos de terreno del *pool*): una por unidad;
  5. materiales (los crea el *bootstrapper*; acá se toman y se les ponen los valores del nivel);
  6. **las variantes de los *shaders***: en el *player*, `ShaderVariantCollection.WarmUpProgressively(1)` por unidad sobre `Settings/Scene/SceneShaderVariants.shadervariants` (y, en Vulkan, D3D12 y Metal, `GraphicsStateCollection.WarmUpProgressively` si hay una grabada para la plataforma; sin teléfono no hay ninguna: R6a-12); en el Editor la compilación es asíncrona (el dibujo nunca espera) y `SceneShaderWarmup` mantiene `Loading` mientras `ShaderUtil.anythingCompiling` diga que hay algo de la escena compilando (`#if UNITY_EDITOR`, como `Platform/DeveloperOverrides.cs`). El arnés y las pruebas que leen píxeles apagan la compilación asíncrona y pueden esperar (§5.1);
  7. **los `Tick` sintéticos** (QA-G6aT-07: la primera vez del C#, el JIT de Mono en Windows): una `SceneSession` de usar y tirar avanza, sin dibujar, por las 12 h, las 22 h con el velo en 1, un paso del vuelo, uno de la salida y uno de la toma aérea, en `E-920` (con el muelle y la cabaña a la vista y el relleno), así `SceneSession.Tick`, `SceneDirector`, los *streamers*, el armado de las matrices del instanciado y la cámara pasan una vez por cada camino. Un `Tick` por unidad;
  8. **el único dibujo de calentamiento** (AC-SCN-01-06): un pedido de render a la RT, fuera de pantalla (`scene-surface` no está a la vista), con un `SceneFrame` de las 12 h con `WarmUpAll` (estrellas, ventanas encendidas, nubes y velo a la vista), así toca **todas** las variantes de la sesión. Las palabras clave son **constantes durante la sesión** (el nivel no cambia en el inmersivo; las sombras de noche bajan su fuerza a 0 sin apagar el mapa, §2.7; `RM_WATER_EXTRAS` y la niebla, fijas; el velo y el *bloom* son valores del `Volume`, no variantes): con eso no hace falta otro dibujo a las 22 h.

  `Ready` llega después de (8). La sesión de usar y tirar se descarta; el mundo queda armado.
- **La colección de variantes** la genera `SceneMaterialsBuilder` (WP-B1, en el *bootstrapper*): para cada uno de los 13 *shaders* y sus pasadas (`UniversalForward`, `ShadowCaster`, `DepthOnly`), las palabras clave de cada nivel (sombras de la luz principal con sus cascadas y su suavizado, instanciado, `RM_WATER_EXTRAS`, `RM_TERRAIN_LAYERS` con el pack, la niebla). `SceneMaterialsTests` afirma que cubre los 13. Lo que cuenta es la prueba del build: `[PERF]` corre con `GraphicsSettings.logWhenShaderIsCompiled` y falla si después de `Ready` aparece en el registro una compilación de un *shader* `RowMate/Scene/*` (§4.5).
- **Cuándo**: `WorkoutScreenView` llama `Preload()` cuando Workout queda en reposo a la vista (como el legacy precarga el agua en `workout_screen.dart:44`); `SceneWarmupPolicy` da el tramo de cada cuadro. Si el usuario toca antes de que termine, la escena llega tarde (§3.9) y termina de prepararse en el inmersivo, por tramos de 2 ms, también después de la cascada. **DEMO-01** cuenta los dibujos de la escena **después de `Ready`** (0); el de calentamiento va antes.

### 3.5 La cámara de persecución (`ChaseCameraRig`; F-SCN-02)

- Cada cuadro: `transform` = la pose del cuadro (posición en metros relativa a la cinta; el mundo se dibuja en `zProp − d`, así la cámara queda cerca del origen y no hay temblor a 42 km); `projectionMatrix` = `ChaseCameraProjection` del cuadro (la del contrato, o la mezcla del vuelo); `nearClipPlane` 0,5; `farClipPlane` 2 000 (§2.1).
- `ResetProjectionMatrix` al cerrar. La cámara de la UI no ve la capa `RowMateScene` (máscara vacía).
- **El bote provisorio** (`scene-boat`, AC-SCN-01-08) va en la cinta (`z` 5,3 a 13,5, quieto respecto de la cámara del contrato): con la respiración, el bote se mueve en pantalla lo que la cámara (≤ 2,2 dp en TV) y queda en la zona del bote.

### 3.6 Lo que se dibuja (con los shaders de WP-D)

| Pieza | Componente | Cómo | Contrato |
|---|---|---|---|
| Cielo | `SkyRenderer` | un triángulo de pantalla completa en la cola 1000 (sin escribir profundidad) que reconstruye la dirección de vista: el **telón** de `skyHorizon` a `skyTop` con `SkyBackdrop.Gradient(q)` sobre la fracción de cielo visible (DV-66, DN6a-09), la banda de bruma de 1,5°, el resplandor dorado, **el disco y el halo** del astro (DV-63; la luna con su fase, su lado oscuro y el espejo del sur, DV-57) y el campo tenue por *hash* (P-10, solo Alto); el bandeado de la noche se corrige solo con el *Bayer* fijo de `RowMateDither` | las capas en el orden de AC-SCN-11-01: degradé → estrellas → astro → nubes → silueta |
| Estrellas | `SkyRenderer` | 80 *quads* instanciados en sus direcciones (§2.8), cola 1001, aditivos, radio angular = radio/focal, alfa de `StarField.Alpha` con el fundido (titilan también en pausa) **× `CelestialMask.StarAttenuation`**: detrás del disco no se ven y en el halo se apagan como en el legacy (QA-G6aT-08) | ± 1 dp con la respiración en 0 |
| Nubes | `SkyRenderer` | 7 *quads* en sus direcciones, cola 1002, con el atlas de 4 cúmulos (`sky.clouds`, las manchas del legacy), cuerpo, sombra, luz interna; el alfa × `CloudModel.WrapFade` (DV-56); la capa lejana solo en Alto | posición de `CloudModel.X/Y`, alfa por ambiente, solo con el viento |
| Cordilleras | `HorizonRenderer` | dos tiras de 260 columnas centradas en la cámara (sin paralaje lateral), cola 1003; color por vértice con `HorizonModel.Color`, nieve de `SnowMask` con *alpenglow*, flancos, base; el borde encendido (P2, DN6a-12) si entra | por acimut; alto `PeakAngleDeg` (0,12·alto/focal con tope de 9°) × `ApproachScale` (se acercan, no se corren) |
| Agua | `WaterSurface` | un plano de 1,6 × 1,5 km centrado en la cámara (cinta) con el shader degradado: **los tres tramos y las 10 líneas del legacy en coordenadas de pantalla** (contra el horizonte real del cuadro), con el alfa de las líneas × la visibilidad del astro (`_RmShimmer.w`, DV-59), y encima, sin moverlos, lo del Designer que entra en el presupuesto (reflejo leve del cielo hacia lo lejos, franja somera junto a la orilla, bruma baja; `RM_WATER_EXTRAS`) | AC-SCN-13-02, -03; DT6a-07 (≤ 24 ALU); el camino de brillo con destellos del Designer es del agua avanzada (6b) |
| Terreno | `TerrainStreamer` | un trozo por orilla y por segmento **desde `d − 25 m`** (DN6a-11: el vuelo y la toma aérea) hasta 450 m (≈ 10 por lado) de un *pool*, armado con `TerrainChunkMesh` al entrar el segmento (en tramos de ≤ 2 ms o en un cuadro sin movimiento finito), y el lejano hasta 1,5 km sin la franja sumergida (§2.1); shader de terreno con las capas `terrain.layer.*` (con el pack, en `Texture2DArray`, DN6a-15) | nada flota; sin costuras |
| Objetos del legacy | `ShoreStreamer` | de `ShoreSegments` (z en [d + 1; d + 400], la lista del legacy, sin banda propia): matrices por (forma, LOD) en arreglos propios, `Graphics.RenderMeshInstanced`; alfa de aparición 400 → 360 m y desvanecido por tramado **de 2 a 1 m** (DV-62, spec corregida); cambio de LOD complementario en el 10 % del límite con *Bayer* fijo (DN6a-24); la paleta por `seed mod 3`; ventanas de cabaña con `NightLights` | AC-SCN-15-02 a -06, AC-SCN-14-07 |
| Relleno | `FillStreamer` | juncos, rocas, bosque cercano, matas, pasto y helechos de `ShoreFill` como mallas opacas (DN6a-16), instanciados por segmento desde `d − 25 m`; el nivel Bajo saca el bosque cercano y la mitad del pasto | determinista por distancia, se pidan en el orden que se pidan |
| Bote | `BoatVisual` | el casco de `HullMesh` (`#F5F5F5` → `#B0BEC5`, línea `#00B4D8`, *cockpit* `#263238`) y la sombra ovalada del lado opuesto al astro (`0,08·ambiente`) | AC-SCN-01-08 |
| Luz | `SceneLighting` | la luz direccional (color, intensidad, dirección de `CelestialPlacement`; fuerza de sombra del nivel y de la hora, **0 de noche en Medio sin apagar el mapa**), ambiente *Trilight* (`RenderSettings`, solo si cambió más de 1/1 024) y los globales de la escena (§7.5), niebla lineal de Unity de 80 a 480 m para lo de terceros (a 400 m da el 80 % del legacy); el modelo de luz de 04-design-notes §10.1 para lo nuestro y su equivalente de URP para lo de terceros (DN6a-13) | AC-SCN-09-05, AC-SCN-10-06, AC-SCN-20-01 |
| Post | `SceneLighting` | un `Volume` global con *Tonemapping* *Neutral*, *Color Adjustments*, *White Balance*, *Bloom* (Medio y Alto) y *Vignette*, con los valores de `PostLook` (la hora y el velo); **sin grano ni TAA** y con el *dithering* de la cámara apagado (§5.1) | AC-SCN-09-05, AC-WKT-13-12 |
| Degradés de pantalla | `ScreenOverlay` | el *quad* de la cámara en la cola 3500, antes del post (04-design-notes §12.3): arriba `#06122A` del 38 % a 0 en el 24 %, abajo del 32 % a 0 en el 18 %; son de la capa del HUD (DV-67) y se apagan en la escena sola | 0 elementos de UI; el velo los alcanza |

**Niebla y lo lejano**: los shaders de terreno, objetos y vegetación usan `ShoreShading` (80-400 m, 80 %; y la extendida para el terreno lejano); la bruma baja de la mañana es un término más de la niebla (AC-SCN-15-07, Alto y Medio).

### 3.7 Los slots (AC-SCN-15-08; ARCHITECTURE §14.4)

- `AssetSlotDefinition` (`slotId` de `SlotIds`, `expectedPath`, presupuesto) → `AssetSlot<T>` con `Validate` y `BuildPlaceholder` → subtipos (`PropSlot` con 3 LOD, `TerrainLayerSlot : AssetSlot<TerrainLayer>`, `VegetationSlot : AssetSlot<GameObject>`, `MaterialSlot`, `TextureSlot`, `SkyProfileSlot`, `HorizonProfileSlot`, `WaterProfileSlot`).
- **El procedural es un valor asignado** (QA-G6aT-04; AC-SCN-15-08 y §12 punto 6 de la spec corregida): cada `.asset` de `Slots/` tiene una fuente, `Procedural` (por defecto, la que deja `SceneAssetsBuilder`) o `Asset` (la que pone el usuario al asignar algo del pack o de otro lado). `Procedural` es válida siempre y **no avisa**: sin el pack (hoy, casi todos los slots), la app arranca y entrena sin un solo `[SLOT]`.
- **Resolución** en el paso (2) del armado: fuente `Procedural` → el procedural, sin aviso; fuente `Asset` con el asset presente y válido → se usa; fuente `Asset` con el asset que **falta** (una referencia vacía, o un GUID de `Assets/ThirdParty/` cuyo archivo no está en el disco: el `.meta` versionado sin el archivo) o que **no valida** (formato, tamaño, *mips*, presupuesto; las capas del pack que no entran en el `Texture2DArray`, DN6a-15) → el procedural y **un** aviso `[SLOT] <id>: <motivo>`. Nunca un error. El "un aviso por slot" es por `SceneHost` (un `HashSet` de su `SlotLog`, no estático): en la app hay uno por proceso, y cada prueba arma el suyo, así lo que ve una prueba no depende del orden en que corren.
- **El Terrain Sample Asset Pack** (opcional, no instalado): entra por `terrain.layer.*` (las capas de canto rodado, pasto, tierra y roca, copiadas al precargar a dos `Texture2DArray` que muestrea el shader de terreno, que no cambia de costo: DN6a-15) y por `vegetation.grass`, `.fern`, `.bush` (sus *prefabs*, de los que se toman la malla y el material para instanciar). Todo bajo `Assets/ThirdParty/TerrainSampleAssetPack/`, ignorado salvo los `.meta`. Los pinos, juncos, cabañas, muelles, cielo, agua y bote siguen procedurales.
- **El slot de prueba** (§0.4 de la spec): `Tests/PlayMode/Scene/Resources/test-terrain-layer.asset` asignado (fuente `Asset`) en una copia en memoria del `EnvironmentDefinition`: el shader lo usa; quitado (la referencia vacía con la fuente `Asset`), vuelve el procedural con un solo aviso. SSL además: todos los slots con `Procedural` → 0 avisos (`LogAssert.NoUnexpectedReceived`); una referencia a `Assets/ThirdParty/` sin el archivo → el procedural y un aviso.
- **`SlotValidator`** (menú `RowMate/Slots/Validar`) lista cada slot con su fuente; los procedurales salen como información, no como aviso.
- **Nada de terceros fuera de los slots** (QA-G6aT-18): `RepoHygieneTests` (dotnet, lectura de archivos) junta los GUID de los `.meta` versionados de `Assets/ThirdParty/` y falla si algún archivo versionado de `Assets/` que no sea un `.asset` de `Slots/` los referencia. Así el CI compila y pasa sin el pack (un slot que apunta a algo que no está cae al procedural).
- **D-ART-08** (P-01): sin texturas CC0 ni la luna de la NASA; `sky.moon` es procedural.

### 3.8 El simulador y la hora (`Hora escena`; F-SCN-23, AC-BLE-16-07)

- El panel escribe `SceneSettings.HourOverride` como desde la It. 2 (`SimulatorPanelStore`, sin cambios). `SceneSession.Tick` lee la hora efectiva en cada cuadro: si cambió el valor forzado (o pasó a `null`), `SceneHourTransition.Retarget(…, HourChange.Forced, …)` con 1,5 s; el paso de minuto de la hora real, `MinuteTick` (1 s lineal); con reducir movimiento, todo en el acto (§2.5).
- El panel muestra el valor nuevo en el acto (no espera a la escena). El reloj de pared, la hora real (AC-SCN-23-04).
- `Simular desconexión` y `Remando` apagado: la escena lee la última lectura de la foto del motor (AC-SCN-03-07, AC-BLE-15-09): con `Remando` apagado el SPM llega en 0 y el bote frena.

### 3.9 La coreografía en el inmersivo (`ImmersiveWorkoutPage`)

| Momento | Qué hace la página | La escena |
|---|---|---|
| Toque (0 ms) | lo de la 5 (página negra que entra, barras, llave, superficie) y `scene.Open(now)` | la sesión arranca en 0 y corre; no se dibuja |
| Primer cuadro ≥ 220 (`OnBackdrop` deja el 0) | **arma el HUD** (DT5-D1, como en la 5) | **no** se dibuja en ese cuadro (el HUD paga su armado solo: 00-deuda-5 §5) |
| Cuadro siguiente | `scene-surface` con la opacidad del fondo de la cascada (220-400) | `BeginFlight` anclado a los 220: el vuelo termina a los 1 820 en la pose del contrato y la respiración entra en 0,6 s |
| 400-1 120 | la cascada | vuela y se dibuja |
| Reducir movimiento | el fundido lineal de 120 de todo (P-15) | `ShowInPlace` en el cuadro 0: la cámara ya en su lugar; sin respiración |
| La escena no está a los 220 | el fondo provisorio (teñido por la hora) con la entrada de la 4b | sigue integrando; termina de prepararse **por tramos de ≤ 2 ms** (§2.16) en cada cuadro sin movimiento finito, **también después de la cascada**, sin pasos indivisibles: las variantes de una en una, los `Tick` sintéticos, y el dibujo de calentamiento, que con los *shaders* calientes cuesta lo de un cuadro de escena (AC-WKT-13-15 corregido; QA-G6aT-03). Listo → `ShowInPlace` (con la rampa de la respiración) y `SceneFadeMotion` (0,6 s, `ease.enter`) de `scene-surface` sobre el provisorio; al terminar, el provisorio en `display: none` |
| Pausa / reanudar | `PauseVeilMotion` | el velo en el `Volume`; la respiración ×0,6; el bote frena con su inercia |
| Diálogo de completado (300 ms después del fin) | lo de la 4b | `BeginAerial` (0,4 m/s, 6 s, cabeceo de −4°, la respiración a 0); con reducir movimiento, nada |
| Salida (reposo) | HUD 0-200, negro 200-400, Workout 400-640 | `BeginExit` en el cuadro 0; `scene-surface` funde a negro con el fondo (200-400); a los 400, `Close` |
| Rotar / otra ventana | el HUD se reacomoda con `HudReflowMotion` (DT6a-06) | la RT se rehace y la cámara toma el horizonte y la focal nuevos en el primer cuadro; la sesión sigue |

- **60 fps con el diálogo** (P-07): la página suelta su demanda de 60 fps cuando el diálogo de completado terminó su entrada y su conteo **y** la toma aérea terminó (6 s); vuelve a pedirla al salir. Las nubes y las estrellas a 30 fps no se notan. Mientras va a 30 (`renderFrameInterval` 2), la adaptación térmica queda suspendida (§2.16): no baja la resolución al final de cada rutina.
- **Nada queda animando** (AC-WKT-13-09, AC-SCN-01-06): después de `Close` el `Tick` de la escena no hace nada (prueba: 120 cuadros después de la salida, 0 dibujos de la cámara de la escena, 0 `RenderMeshInstanced`, ningún `IMotion` de la escena en el `MotionDriver`).
- **Al volver del segundo plano** (AC-SCN-01-04): el primer `Tick` avanza ≤ 0,1 s y la hora es la de ese cuadro (`Retarget` con `HourChange.Instant`; las estrellas y las luces, `Snap` a su valor final).
- **El fundido del HUD al rotar** (`HudReflowMotion`, DT6a-06): cuando la ventana cambia de clase (vertical ↔ horizontal), las piezas del HUD quedan en 0 en el *layout* nuevo y entran en 200 (`ease.enter`), y si llega otro cambio arranca de donde está; un cambio de tamaño dentro de la misma clase (arrastrar el borde de la ventana) se reacomoda en el acto, como hoy. Con reducir movimiento, en el acto. La escena se adapta en el acto (AC-WKT-17-07).

---

## 4. Rendimiento (WP-B, con el reparto del Designer)

### 4.1 Presupuesto por cuadro (nivel Medio, Galaxy A54 / Pixel 6a, 1 080 × 2 400; la única tabla)

Es la de 04-design-notes §15.1 (que reconcilió la de 00-deuda-diseno §10 con la versión anterior de esta sección) **más la composición y las RT** (QA-G6aT-11). Si el Designer la ajusta en WP-D2, se ajusta acá: no hay otra.

| Pieza | *Draw calls* | Triángulos (peor tramo) | Texturas y RT | GPU estimada |
|---|---|---|---|---|
| Cielo (telón, bruma, resplandor, disco y halo; el campo tenue solo en Alto) | 1 | 0 (pantalla) | luna 256² RG8: 0,13 MB | 0,4 ms |
| Estrellas (con la máscara del astro) y nubes | 2 | < 0,2 k | atlas 512 × 256 RG8: 0,25 MB (+ capa lejana 1 024 × 512 R8: 0,5 MB, solo Alto) | 0,2 |
| Cordilleras | 2 | 1 k | — | 0,2 |
| Agua degradada (≤ 24 ALU, 0 texturas) | 1 | 0,5 k | — | 0,6 |
| Terreno (≤ 10 + 10 trozos desde `d − 25 m` y el lejano, con la copa) | ≤ 21 (11 con los trozos de un lado unidos) | 40 k | ruido 256² RGBA8: 0,25 MB (+ ≈ 2 MB con el pack) | 1,6 |
| Pinos del legacy (3 formas × 3 LOD) | ≤ 9 | 32 k (12 / 22 / 73 pinos por banda) | — | 1,0 |
| Cabañas y muelles | 3 | 3 k | — | 0,2 |
| Bosque cercano y matas | 4 | 23 k | — | 1,0 |
| Pasto, helechos, juncos y rocas (mallas opacas) | 4 | 29 k | — | 1,4 |
| Bote y sombra | 2 | 6 k | — | 0,3 |
| Sombras (1 cascada de 1 024 a 40 m; de noche, fuerza 0) | ≤ 15 | (≈ 25 k vueltos a dibujar) | mapa 1 024²: 4 MB | 1,2 |
| Post (*bloom* a media resolución, color, *Neutral*, viñeta, velo) y degradés | ≤ 9 | — | cadena de *bloom*: ≈ 4 MB | 1,5 |
| **Composición** (DT6a-05): la última pasada de URP a nuestra RT (la del post, ya contada arriba), el panel que muestrea la RT de la escena a pantalla completa dentro de la RT de la UI, y la copia del presentador a la pantalla | 2 | — | RT de la escena sRGB a 0,85: 7,5 MB; color de URP con MSAA 2× (B10G11R11): 15 MB y su resolución 7,5 MB; profundidad con MSAA 2×: 15 MB | 0,7 |
| UI (HUD, 56-57 elementos) | ≤ 12 | — | (la RT de la UI, 10,4 MB, ya existía desde la It. 2) | 1,0 |
| **Total** | **≈ 87** (tope 150; SetPass ≈ 30, tope 50) | **≈ 135 k** (tope 350 k) | contenido **≈ 0,6 MB** (+ ≈ 2 MB con el pack); RT nuevas **≈ 53 MB** en el peor caso y **≈ 23 MB** si el MSAA y la profundidad quedan *memoryless* (Vulkan en Android); más los 10,4 MB de la UI (tope de §20.1: 180 MB) | **≈ 11,3 ms** (tope 12) |

- **El margen de GPU es chico** (0,7 ms): es estimado, y lo que lo cuida es la adaptación (la resolución baja primero; a 0,80 la escena cuesta ≈ 11 % menos). R6a-06.
- **CPU de escena ≤ 1,5 ms** (§20.2): `SceneSession.Tick` (~100 objetos visibles, 80 estrellas, 7 nubes) + aplicar + armar las matrices + un trozo de terreno cada ~12 s (en un cuadro sin movimiento finito o en tramos). Se mide en el Editor con `ProfilerRecorder` del marcador `RowMate.Scene.Tick` (AC-SCN-01-11).
- **Sin impostores en la 6a** (DT6a-14): el LOD2 de 120 triángulos llega a 400 m (35 pinos × 120 = 4,2 k triángulos en el peor tramo); el impostor del Designer queda para la 6b si la medida lo pide.
- **Los topes por nivel** (AC-SCN-01-11): `SceneBudgetTests` (PlayMode) con el lago a las 12 y a las 22 h, avanzando, en Alto, Medio y Bajo (con `ForceTierForTest` y `TierScope`, §4.2): `Batches Count`, `SetPass Calls Count`, `Triangles Count`, la memoria de texturas de la escena (`Texture.currentTextureMemory` antes y después del armado) y la de las RT (`Render Textures Bytes`, escalada al tamaño de 1 080 × 2 400), dentro de §20.1.
- **La GPU, con una línea de base** (QA-G6aT-11): ninguna prueba del Editor mide ms de GPU. `[PERF]` corre además con el nivel **Medio forzado** en la PC (`-rowmate-quality medium`, solo desarrollo) y guarda los ms de GPU de `FrameTimingManager` por pieza de la demo: no dice nada del teléfono, pero da la proporción entre piezas y una referencia contra la que medir en la It. 14.
- **El sobre-dibujo ≤ 2,5×** en la franja de vegetación (DN6a-16): `SceneBudgetTests.VegetationOverdraw` dibuja la escena con el modo *Overdraw* del *Rendering Debugger* de URP (opacos, con un máximo de 10 capas) a una RT, lee la franja (entre el horizonte y la zona del bote, fuera de la zona) y afirma el promedio ≤ 2,5. Si esa API no se puede usar en *batchmode*, la prueba queda `Inconclusive` con el motivo y la medida pasa a revisión de QA con el *Frame Debugger* (se anota en "Cambios por implementación").

### 4.2 Niveles y adaptación (ADR 018)

- `QualityTierProfile` ×3 (`Settings/Quality/{High,Medium,Low}.asset`) con su `RowMate_URP_{High,Medium,Low}` (los crea `UrpProvisioner`, con `renderScale` 1 en los tres): resolución de nuestra RT (1,0 / 0,85 / 0,75), MSAA (4× / 2× / FXAA), HDR (sí / sí / no), sombras (2 cascadas de 2 048 a 80 m / 1 de 1 024 a 40 m / sin mapa: el óvalo; de noche, fuerza 0,25 / 0 / —), *bloom* (sí / sí / no), campo tenue de estrellas y capa de nubes (Alto), densidad de relleno y fin del pasto (60 m / 40 m / 30 m con la mitad), LOD ×1 / ×1 / ×0,7, bruma baja (Alto y Medio). Son los de 04-design-notes §15.2.
- `GraphicsTier.Choose(SystemInfo…)`: Alto en escritorio con GPU dedicada (el de la PC de desarrollo y W1280), Medio en la lista de GPU de gama media, Bajo con Mali-G52 o menos de 4 GB.
- **Qué aplica cada uno** (QA-G6aT-05). `SceneQualityApplier` (Scene) aplica todo lo que es de la cámara, de la RT, del `Volume` y de los materiales: el tamaño de la RT (resolución), su MSAA, `allowHDR` y `renderShadows` de la cámara, el *bloom* y los valores del `Volume`, la fuerza de las sombras de la luz, las densidades, los LOD, el fondo de estrellas y la capa de nubes. Lo que **solo** vive en el asset de URP (la distancia de sombras, las cascadas, la resolución del mapa) se elige con el nivel de `QualitySettings`, que en el *player* lo pone `PlayerUrpAssetQuality` (Platform) al arrancar (`SetQualityLevel`, estado de ejecución: no se guarda). La distancia de sombras de la adaptación se cambia en una **copia en ejecución** del asset activo, **solo en el *player*** (como el `PanelSettings`, R2-05).
- **En el Editor nunca se toca la configuración del proyecto**: no se asignan copias a `QualitySettings.renderPipeline` ni a `GraphicsSettings.defaultRenderPipeline` (son los mismos *setters* con los que `UrpProvisioner` la **guarda**, y una copia que no es asset se perdería al guardar), no se cambia ningún asset de URP (en el Editor un cambio a un asset en *Play* queda) y no se usa `EditorSettings.asyncShaderCompilation` (se guarda; el que se usa es `ShaderUtil.allowAsyncCompilation`, que no). En el Editor, `IRenderAssetQuality` es una implementación que no hace nada y anota la acción (la adaptación se prueba igual: SAD mira las acciones y la RT).
- **Las pruebas que necesitan otro nivel** (los topes de `SceneBudgetTests` por nivel) usan `TierScope` (`Tests/UiSupport`): guarda el nivel actual del Editor, llama `QualitySettings.SetQualityLevel(nivel, false)`, aplica el resto con `ForceTierForTest`, y en `Dispose` (dentro de un `finally` y en el `[OneTimeTearDown]` del fixture) vuelve al nivel de antes; el fixture afirma al terminar que el nivel es el del principio. Las demás pruebas usan el nivel del proyecto (Alto en Windows).
- **La guardia de los *scripts***: `tools/unity/ProjectSettingsGuard.ps1` guarda el SHA-256 de cada archivo de `ProjectSettings/` y `Assets/RowMate/Settings/` antes de abrir Unity y lo compara al cerrar; si cambió algo (salvo el *define* del simulador, que ya restauran), la corrida **falla** con la lista y no restaura nada sola (podría pisar un cambio del usuario). La usan `run-unity-tests.ps1` (B0), `build.ps1` (B2) y `capture-snapshots.ps1` (B3). Las corridas del *bootstrapper* que **sí** escriben `ProjectSettings` (B0, con el menú o `-executeMethod`) no pasan por la guardia y sus cambios se versionan (marca G de §1.1).
- `SceneAssetsTests` afirma que cada nivel tiene **su asset del proyecto** (`QualitySettings.GetRenderPipelineAssetAt(i)` es el de `Settings/URP/` y `AssetDatabase.Contains` es verdadero) y que el asset por defecto también lo es.
- `ThermalAdapter` (Platform) lee `FrameTimingManager` (`CaptureFrameTimings` y un arreglo propio; *Frame Timing Stats* lo activa el *bootstrapper* en `PlayerSettings.enableFrameTimingStats` y `SceneAssetsTests` lo afirma) solo en los cuadros en que la escena se dibujó (§2.16) y alimenta `ThermalAdaptation` (Core). Bootstrap aplica la acción: resolución y densidad en `SceneHost`; distancia de sombras y nivel en `IRenderAssetQuality`. PlayMode lo prueba con tiempos de cuadro de prueba (AC-SCN-01-13). En equipo, NO VERIFICABLE sin teléfono.

### 4.3 *Frame pacing* y apagado

- El inmersivo sigue pidiendo 60 fps (ARCHITECTURE §13.1), salvo el diálogo de completado quieto (P-07, §3.9). Fuera del inmersivo: ningún dibujo de la escena después de `Ready` (el único de calentamiento va antes, fuera de pantalla); el armado de la precarga solo en cuadros sin movimiento (§2.16) y sin pedir 60 fps. En Workout en reposo la app vuelve a 30 fps (AC-SCN-01-06; `[PERF]`: la GPU en reposo en Workout igual que en la 5).

### 4.4 0 B por cuadro (AC-SCN-01-12)

- En régimen (remando, en pausa, durante una transición de hora, de noche): `SceneSession.Tick`, `SceneDirector`, los *streamers* y el pedido de render no asignan. Arreglos reservados al abrir o al cambiar la ventana; `MaterialPropertyBlock` reutilizados; `Shader.PropertyToID` en `SceneShaderIds` estáticos; sin LINQ, sin cierres, sin `foreach` sobre interfaces.
- Medido en **cuadros reales** con `FrameAllocations` (It. 5), con el repintado del HUD adentro y su **control positivo** en la misma corrida, en cuatro escenas de 120 cuadros: remando con `Fuerte` (pasan segmentos), en pausa, durante `Hora escena` 12 → 18, y de noche. Nunca con el contador del hilo (en Mono da 0).
- **Cómo entra la escena en lo medido** (QA-G6aT-06). `FrameAllocations` solo ve `Step(acción)` y los dibujos observados, y el `Tick` de la escena corre en `AppRoot.LateUpdate` (o en `ShellHandle.Tick` del arnés), fuera de los dos. Por eso `SceneHost` tiene un observador (`TickObserver`, como `SeriesPanelView.DrawObserver`) y `FrameAllocations.WatchScene(host)` lo engancha:
  - `BeginTick`/`EndTick` envuelven **todo** el `Tick`: la sesión de Core, `SceneDirector`, los *streamers* y el armado de las matrices (**cuenta "escena"**);
  - `BeginRender`/`EndRender` envuelven el pedido de render (`SubmitRenderRequest`, que dibuja adentro de la llamada) (**cuenta "URP"**). Con el respaldo de S6a-3 (la cámara habilitada) son `RenderPipelineManager.beginCameraRendering`/`endCameraRendering` de la cámara de la escena;
  - el **control positivo** va en la misma corrida: `AllocateInTickForTest` hace un `new byte[64]` dentro del `Tick` durante 120 cuadros (tienen que contarse 120 de 120 en la cuenta "escena") y después se apaga para los 120 cuadros limpios (0 y 0). Además, la cuenta "URP" con el control prendido sigue en 0 (el control no se filtra a la otra cuenta);
  - AC-SCN-01-12 pide **0 en las dos**. Si URP asigna dentro del dibujo solo en el Editor (por algo de depuración del Editor), la cuenta "URP" se mide en el build de desarrollo con `-rowmate-perf-scene` (el contador "GC Allocation In Frame Count" del hilo principal, que funciona en los builds de desarrollo) y queda escrito en ADR 022 y en "Cambios por implementación".
- Fuera de régimen y fuera de lo medido: el cambio de resolución de la adaptación (rehace la RT, como mucho una vez cada 10 s) y el de la ventana.

### 4.5 `[PERF]` en el build (AC-SCN-01-10, AC-WKT-13-15, AC-WKT-14-19; 00-deuda-5 §5)

| Argumento (solo desarrollo) | Mide |
|---|---|
| `-rowmate-perf-entries N` (C) | ahora **por tiempo**: los primeros 2,5 s desde el toque (el vuelo entero), con la escena, y **la salida de cada entrada: de 0 a 1 s desde la confirmación de `Terminar`** (AC-WKT-13-15 corregido; QA-G6aT-17); la primera entrada es la primera del proceso (la escena recién precalentada); además el camino del usuario para la rutina (la lista y la confirmación) |
| `-rowmate-scene-delay <s>` (N) | combinado con el anterior: la escena demorada (`<s>` s después del toque, sin precarga), para medir que su preparación por tramos y su primer dibujo no pasan de 16,7 ms, también después de la cascada (QA-G6aT-03) |
| `-rowmate-perf-scene <s>` (N) | libre con `Fuerte` (5 min): p99 y peor cuadro por ventana de 10 s, CPU del hilo principal y GPU si `FrameTimingManager` la da; la memoria total; las asignaciones por cuadro del hilo principal ("GC Allocation In Frame Count") |
| `-rowmate-quality <tier>` (N) | fuerza el nivel en el build de desarrollo; con `medium` y `-rowmate-perf-scene`, la línea de base de GPU para la It. 14 (§4.1) |
| `-rowmate-perf-hours` (N) | la secuencia de VID-05 (17 → 23 h con `+` cada 2 s) |
| `-rowmate-perf-series` (C) | `Serie larga`, los últimos 5 s de cada paso, con la escena |
| `-rowmate-perf-completion` (N) | `Corta` con el diálogo y la toma aérea (60 fps mientras se mueve; después puede bajar a 30, y la adaptación no dispara) |

En todas las corridas `[PERF]` el build de desarrollo arranca con `GraphicsSettings.logWhenShaderIsCompiled` y `FrameTimeProbe` cuenta en el registro las compilaciones de *shaders* `RowMate/Scene/*` **después de `Ready`**: tienen que ser 0 (la prueba de que el precalentado cubrió todo, QA-G6aT-07). Con `ROWMATE_DATA_DIR` temporal y la base sembrada (00-deuda-5 §1.1). Si el `RowMate.exe` del usuario está abierto: reintentos cada 2 min hasta 20 min y, si no, NO VERIFICABLE. Ninguno de estos argumentos funciona en el build normal (`BuildOutputInspector`, `ItSixAToolingTests`).

---

## 5. Capturas y videos (WP-B3; los valores por cuadro con WP-D)

### 5.1 Reglas del conjunto `it6a`

- **Dónde**: `docs/migration/iterations/06a-escena-lago/snapshots/` (`manifest.json` e `INDEX.md` sin rutas de la PC) y `snapshots/legacy/` para LEG-01 a -07.
- **Cómo**: `tools/unity/capture-snapshots.ps1 -Set it6a` (por defecto desde la 6a): el Editor en *batchmode* **sin `-nographics`** (Direct3D 11), con el *define* del simulador, `ROWMATE_DATA_DIR` temporal por toma, el reloj y la fecha de prueba (13:00 UTC, UTC−03:00, 2026-10-28), español, simulador sin ruido, R-4, sin la llave salvo que la toma la pida. `UiTestHost` con `SceneMode.Real` y el nivel **Alto** (el del escritorio).
- **Determinismo** (con QA-G6aT-12):
  - `ShaderUtil.allowAsyncCompilation = false` durante toda la corrida del arnés **y** mientras dure un `UiTestHost` con `SceneMode.Real` en las pruebas PlayMode que leen píxeles (SPR, SPL, SLC, SBT, las sondas): si no, el primer dibujo sale con el color de "compilando". Es el ajuste que no se guarda; `EditorSettings.asyncShaderCompilation` (que se guarda en `ProjectSettings`) no se toca nunca;
  - el precalentado de la escena (§3.4) antes de la primera toma;
  - ningún *shader* usa `_Time` (todo con el tiempo de escena y la fase de Core, también el viento en el *vertex shader*);
  - tramados de *Bayer* fijos en pantalla; **el bandeado del cielo de noche se corrige solo con ese *Bayer*** (en el *shader* `Sky`);
  - **el *dithering* de la cámara de URP apagado** (`UniversalAdditionalCameraData.dithering = false`: rota su ruido en cada cuadro) y **sin grano** (el perfil del `Volume` no tiene *Film Grain*); sin TAA (en Bajo, FXAA, que es espacial y determinista). SLC lo afirma sobre la cámara y el perfil armados (`CameraIsDeterministic`);
  - la escena avanza con `SceneSession.Advance` en pasos de 1/60 s mientras el reloj de prueba avanza: CAP-13 (60,2 s) tiene la distancia de escena de un remo real (≈ 234 m).
- **Dos corridas** (`it6a-A` y `it6a-B`) iguales **byte a byte** en PNG y MP4 (en `manifest.json`, solo `durationMs`). Si alguna toma 3D no lo permite en esta GPU, QA lo explica toma por toma y acepta solo **1 nivel por canal en menos del 0,1 % de los píxeles** (`SnapshotEvidenceTests` lo calcula y lo escribe en el `INDEX.md`).
- **Escena sola** (CAP-01 a -12, VID-05, -06, -10): `HudView.HideForCapture` (los hermanos de `scene-surface` en `display: none`) y `SceneHost.SetScrimsForTest(false)` (sin los degradés de pantalla, que son del HUD: DV-67); tiempo de escena 3,2 s, respiración en 0, `E-920`, sin región (la luna del norte), salvo lo que la toma diga. Con hora forzada, la toma 1,6 s después del último cambio.
- **Una toma en Bajo** (QA-G6aT-21): `it6a-low` (no es de la spec; solo evidencia) W1280 a las 12 h con `E-920`, dos cuadros seguidos con el bote quieto: 0 píxeles distintos en la orilla lejana. En el Editor (D3D11, Z invertida) no reproduce GLES: el margen de §2.1 se justifica con la cuenta y la parte de GLES queda NO VERIFICABLE hasta el teléfono (It. 14).
- **Marcas** (CAP-02, -05, -07 y sus LEG): `SceneMarks` (solo el arnés) dibuja en una capa de depuración encima una marca de 4 dp en la base de cada objeto del legacy (la lista de `ShoreSegments`, comparada antes con el oráculo) y anota en el `manifest.json` la diferencia en `x` entre la marca y el ancla 3D proyectada por la cámara real (sonda: ≤ 2 dp). El **contorno de la zona del bote** (CAP-13 a -18) es el de la 4a.
- **Valores por cuadro de los videos** (`manifest.json`): tiempo de escena, distancia, velocidad, hora visible, pose de la cámara, apartamiento del horizonte (dp), modo de la cámara, peso del velo, opacidad de `scene-surface`.
- **Las tomas de las It. 4a, 4b y 5** se quedan como están (sus conjuntos usan `SceneMode.None`, el fondo provisorio). Las que cambian por las píldoras `.hud` (`it4b`, `it5`) se regeneran **una vez** con dos corridas iguales y QA explica cada diferencia (solo los colores de trabajo, enfriamiento y otro en la barra).

### 5.2 La lista

| ID | Tamaño | Cómo se arma | Sondas (manifest) |
|---|---|---|---|
| CAP-01 + LEG-01 | V390 | escena sola, `E-920`, 06:00 | sol (acimut −17,5°, 1,5°) detrás del macizo: el disco tapado, el resplandor dorado arriba de la cresta; estrellas con alfa > 0 (oscuridad 0,27); bruma baja 0,55; tono dominante del cielo contra LEG-01 (matiz OKLCH ± 8°), sin degradés de pantalla |
| CAP-02 + LEG-02 | V390 | `E-920`, 12:00, con marcas | marcas ≤ 2 dp; el sol en (195,0; 254,7) ± 2 dp, radio 10,9 dp; 0 estrellas; las 7 nubes en su `x` e `y` del legacy ± 1 dp; nada flota (rayos al terreno); las sondas de color del cielo de 04-design-notes §10.4 (arriba y junto al horizonte). Aparte, sin imagen, la **sonda del brillo** (DN6a-21, AC-SCN-11-05): TV, escena sola, 12:00, Alto: el diámetro con luminancia > 0,9 ≥ 30 dp |
| CAP-03 + LEG-03 | V390 | `E-920`, 18:00 | sol (+17,5°, 1,5°) en el collado, con la cabaña a 40 m en contraluz; estrellas (oscuridad 0,23, con el fundido terminado) |
| CAP-04 + LEG-04 | V390 | `E-920`, 22:00 | luna (−5,83°, 10,59°) en (155,2; 264,3) ± 2 dp, radio 8,7 dp, gibosa menguante (≈ 94 %) con la parte oscura a la derecha; 80 estrellas, ninguna adentro del disco; la ventana `#FFE082` ± 3 **antes del ajuste de color de la hora** (`RenderWithoutPostForTest`, AC-SCN-09-06 corregido; QA-G6aT-13) |
| CAP-05 + LEG-05 | H844 | `E-920`, 12:00, con marcas | marcas ≤ 2 dp; el sol en (422,0; 80,9) ± 2 dp; las cordilleras con el alto del legacy (6,84°) |
| CAP-06 + LEG-06 | H844 | `E-920`, 22:00 | la luna en (382,2; 90,5) ± 2 dp y las estrellas |
| CAP-07 + LEG-07 | V390 | `E-0`, 12:00, con marcas | marcas ≤ 2 dp; muelle a 175 m con niebla ≈ 24 % |
| CAP-08 | TH | escena sola, 19:00 | sol con visibilidad 0,54 detrás del paisaje; 80 estrellas (oscuridad 0,5); ventana apagada; las líneas del agua × 0,54 |
| CAP-09 | TH | 20:00 | luna saliendo (visibilidad 0,38: se ve el halo); ventana encendida; las líneas × 0,38 |
| CAP-10 | TV | 00:00 | luna (0°, 12°) en (205,5; 278,2) ± 2 dp; la primera estrella en su lugar; líneas plateadas centradas |
| CAP-11 | TV | 15:00 | 0 estrellas; sol (+8,75°, 8,92°) en (268,8; 300,3) ± 2 dp |
| CAP-12 | W1280 | `E-42K`, 12:00 | objetos = oráculo a 42 195 m; sin temblor (dos cuadros seguidos sin moverse: 0 píxeles distintos con el bote quieto) |
| CAP-13 a CAP-17 | TV, TH, L640, P360, W1280 | libre con R-4 a los 60,2 s, 10:00, con el contorno | `1240` `m` en la banda; distancia de escena 234 ± 0,5 m; el punto del remero en el centro de la zona ± 2 dp; el sol en su lugar de 04-design-notes §16.2 ± 2 dp; los degradés de pantalla a la vista (DV-67) |
| CAP-18 | TV | `Series 600` a los 250,2 s, los dos paneles abiertos, 18:00 | lo de la 5; casco a la vista |
| CAP-19 | TV | `Prueba 4a` a los 90,6 s, 12:00, tiempo de escena elegido para que la nube 3 pase detrás de la barra | contraste de la hecha y del borde de la hueca contra el vidrio renderizado ≥ 3 |
| CAP-20, CAP-21 | TV, TH | libre: `Pausa` a los 60,2 s; toma a los 62,2 s | peso del velo 1; velocidad ≈ 1,05 m/s; HUD a pleno; la sonda del velo de 04-design-notes §10.4 (canal máximo × 0,85 ± 0,03, croma × 0,70 ± 0,05) |
| CAP-22 | TV | `Corta`: el diálogo a los 30,3 s; toma a los 36,5 s | cámara en modo aéreo y quieta (+1,70 m arriba, −1,70 m atrás, **cabeceo −4°**, respiración 0); el horizonte en **336,9 ± 1 dp**; la popa en 552,9 ± 2 dp, dentro de la zona del bote |
| CAP-23 | W1280 | libre a los 60,2 s, 22:00 | lo de la 4b de noche |
| CAP-24 | TV | libre con `DelayReadinessForTest(1,5)`, a los 0,6 s | fondo provisorio teñido por las 10 h (`#1E61BA` / `#145395`); `scene-surface` oculto |
| DEMO-01 a -08 | W1280 | la secuencia de la spec §7.4 con `F-4A` | DEMO-01: 0 dibujos de la escena **después de `Ready`** (el de calentamiento va antes); DEMO-03: el muelle a la izquierda; DEMO-04: el reloj de pared en la hora real; DEMO-08: la salida y 0 dibujos al final |
| VID-01 | TV, 2,5 s | `Entrenamiento libre` | opacidad de la escena 0 hasta 220, 1 a los 400; la focal empieza en 1,25 × la del contrato; velocidad de la cámara ≤ 17,5 m/s; la pose del contrato a los 1 820 sin salto (Δ entre los dos últimos cuadros < 1 % del recorrido) y **la respiración entrando con `S(0; 0,6 s)`**: en el cuadro de los 1 820 su multiplicador vale 0 y a los 2 420, 1 |
| VID-02 | TV, 1 s | VID-01 con reducir movimiento | modo `Contract` desde el cuadro 0; 120 ms lineal |
| VID-03 | W1280, 12 s | libre con `Fuerte` de 30 a 42 s | horizonte ≤ 3 dp; ningún objeto pasa de alfa 0 a 1 en un cuadro (aparición 400 → 360 m, tramado de 2 a 1 m, cambio de LOD complementario); ninguna nube aparece o desaparece en un cuadro en la vuelta; el muelle |
| VID-04 | TH, 14 s | `Pausa` a los 20 s, `Reanudar` a los 32 s | el velo en 300 y 240; el bote se desliza ≈ 6 m y para a los ≈ 9 s |
| VID-05 | TH, escena sola, `E-920`, 14 s | 17:00 y `+` cada 2 s hasta 23:00 | sin saltos del astro (≤ 0,3° por minuto de escena); en el minuto de escena 19:34, 0 píxeles de las líneas del agua; las estrellas entran a las 18:00 en 0,9 s; las luces con su fundido de 0,4 s |
| VID-06 | TH, escena sola, `E-920`, 10 s | 03:00 y `+` cada 2 s hasta 07:00 | las últimas estrellas visibles a las 6:12 y su fundido de salida; las líneas se apagan con la luna y vuelven con el sol; la bruma |
| VID-07 | TV, 1,2 s | libre a los 20 s: `Terminar` → `Terminar` | modo `Exit` desde el cuadro 0; negro a los 400; 0 dibujos desde los 400 |
| VID-08 | TV, 9 s | `Corta` de 29 a 38 s | modo `Aerial` desde los 30,3 s, quieta a los 36,3 s |
| VID-09 | TV → TH, 2 s | libre a los 20 s: rotar | la distancia sigue (sin reinicio); el HUD entra en 200 |
| VID-10 | TV, 20 s, escena sola | libre con R-4 | horizonte ≤ 3 dp con la línea del contrato marcada |
| VID-11 | TV, 3 s | libre con `DelayReadinessForTest(1,5)` | el provisorio; el fundido de 0,6 s; la distancia al día; el peor cuadro del trabajo de la escena mientras se prepara ≤ su tramo (marcador `RowMate.Scene.Warmup`) |

### 5.3 Las referencias del legacy (LEG-01 a -07)

Las dibuja el oráculo de Flutter (§6.2) con el código del legacy: `SkyPainter` (con su `paintHorizon` del lago), el respaldo de `WaterPainter` (`shader: null`) y `ShorePainter`, sin el bote, con el mismo tamaño (densidad 1), hora, distancia y tiempo de escena (3,2 s) que la toma de la app; las versiones "con marcas" agregan la marca de 4 dp en la base de cada objeto (`cam.project(x, 0, relZ)`). Si el oráculo no pudiera dibujarlas, quedan NO VERIFICABLE con el motivo y la comparación se hace con las marcas (obligatorias).

---

## 6. Tests

### 6.1 Suites y comandos

| Suite | Comando | Qué cambia |
|---|---|---|
| dotnet | `powershell -NoProfile -File tools/dotnet/run-tests.ps1 -ArtifactsPath <scratch>/it6a/… [-Matrix -UnityResults …]` | + `Tests/EditMode/Scene/**` (≈ 450 pruebas) |
| Unity EditMode (con y sin simulador) | `tools/unity/run-unity-tests.ps1 -Platform EditMode [-Simulator]` | las mismas de dotnet + `SceneAssetsTests`, `SceneMaterialsTests`; con la guardia de `ProjectSettings` |
| Unity PlayMode (con y sin simulador) | `… -Platform PlayMode [-Simulator]` | + `Tests/PlayMode/Scene/**`, `SceneHourSimulatorTests` |
| Oráculo | `powershell -NoProfile -File tools/oracle/run-oracle.ps1 -Scene` | genera `scene_*.json` y los LEG dentro de la guardia del legacy |
| Arnés | `tools/unity/capture-snapshots.ps1 -Set it6a -Out …` (A y B) | §5 |
| Build | `tools/build/build.ps1 -Target Windows -Simulator` | a `Builds/Windows/simulator` solamente |

### 6.2 El oráculo (`tools/oracle/engine/`, `-Scene`)

- **Por qué Flutter**: `time_of_day.dart`, `environment.dart` y `lake_environment.dart` importan `dart:ui` (`Color`, `Canvas`): necesitan el motor de Flutter. Se usa el proyecto `engine/` de la 4a (dependencia del legacy **por ruta relativa**, `pub get --offline`, todo lo generado ignorado), con dos pruebas nuevas, y `run-oracle.ps1 -Scene` las corre **dentro de la guardia** (los tres controles de antes y después; el legacy sin cambios; nunca se nombra el módulo de credenciales).
- `engine/test/scene_vectors_test.dart` escribe:

| Archivo | Contenido | Tamaño |
|---|---|---|
| `scene_random.json` | las tiradas de §2.10 por semilla | < 60 KB |
| `scene_palette.json` | los 8 campos en cada minuto (1 440 filas, bits de `double`); `isNight`, `sunElevation` | ≈ 520 KB |
| `scene_light.json` | `lightFor` en cada minuto y en los siete tamaños (x, y, fuerza, luna) | ≈ 700 KB (si pasa de 1 MiB: cada 2 min y los minutos de los cambios enteros) |
| `scene_sky.json` | las 80 estrellas en los siete tamaños; las 7 nubes a 0 y 1 234 m con t 0 y 3,2 (`LegacyX` con distancia); `horizonColor` de las dos cordilleras por hora entera; `_c` de la orilla (tinte y niebla) para 4 colores × 25 horas × 9 `relZ` | < 300 KB |
| `scene_lake.json` | segmentos 0 a 99 completos (tipo, bits de `x`, `z`, escala, semilla); segmentos 0 a 2 000 como resumen **FNV-1a de 64 bits** de los bytes canónicos de cada segmento (calculado igual en Dart y en C#); las listas visibles a 0, 1, 49,5, 50, 920, 1 234, 5 000 y 42 195 m; el tope con el escenario de prueba (la lista de entrada y la de salida de la función del legacy `visibleProps`, copiada a un `Environment` de prueba en el propio test de Dart; el escenario tiene `z` distintas alrededor del corte, QA-G6aT-15) | < 900 KB |
| `scene_water.json` | los tres tramos por hora entera; las 10 líneas a 0, 3, 920, 1 234, 4 096 y 42 195 m en V390 y H844 a 6, 12, 18 y 22 h | < 200 KB |

- `engine/test/scene_legacy_images_test.dart` dibuja LEG-01 a -07 (y las "con marcas") con `PictureRecorder` → `toImage` → PNG a `docs/migration/iterations/06a-escena-lago/snapshots/legacy/`.
- Los compara `SceneGoldenTests` (dotnet, sin Flutter), **con el comparador de cada valor** (§2, "Bit a bit o con tolerancia"; QA-6a-PAR-05): bit a bit la aritmética pura (generador, segmentos y su FNV, paleta y ambiente, visibles, `_c` de la orilla, líneas del agua con la fuerza leída del archivo, estrellas, nubes, `horizonColor`); `Within(1e-12)` `sunElevation` (guardado en bits, comparado con tolerancia), la fuerza del astro y el perfil de las cordilleras; `Within(1e-9)` `lightFor` en dp. El encabezado de la clase lo dice y cada prueba nombra su comparador. Las listas visibles se comparan después de ordenar cada tramo de `z` iguales por (tipo, `x`, semilla) en los dos lados (QA-G6aT-15 a). `GoldenCoverageTests` suma los seis archivos (≤ 1 MiB).

### 6.3 Archivos de test

**dotnet y Unity EditMode** (`Tests/EditMode/Scene/`, WP-A):

| Clase (prefijo) | Fija |
|---|---|
| `SceneCameraTests` (SCC) | LT-62 a LT-65; los siete tamaños (horizonte, focal, remero); la matriz contra `TryProject` (≤ 1e-9) en las marcas de AC-SCN-02-02 |
| `ChaseCameraMotionTests` (CCM) | la respiración (fórmulas con 0,15°, sin azar, ×0,6 en pausa con sus rampas, ×0 con reducir movimiento), el barrido de 10 min en los siete tamaños (≤ 3 dp; 2,93 en W1280) y el remero en la zona; **la entrada de la respiración** con `S(0; 0,6 s)` al terminar el vuelo y en `ShowInPlace` (sin salto de pose en el cuadro de los 1 820: el multiplicador vale 0 ahí); el vuelo (1 000 muestras: `y ≥ 2,2`, `|x| ≤ 3`, `z ≤ 0`, fin exacto, 220-1 820, **velocidad ≤ 17,5 m/s**, **focal inicial = 1,25 × la del contrato**, `ease-in-out-sine` en la posición); la salida; la toma aérea (2,4 m en 6 s, **cabeceo −4°**, la respiración en 0 al final, quieta; en TV el horizonte en 336,9 ± 0,5 y la popa en 552,9 ± 1) |
| `SceneStateTests` (SST) | LT-66, LT-67, LT-68; R-4 a los 60 s ≈ 234 m; pausa a los 60,2 s (≈ 1,05 m/s a los 2 s, ≈ 6 m, quieto a los ≈ 9 s); 0,5 + 2 s < 0,5 m/s; paso acotado |
| `SceneInputsTests` (SIN) | la última lectura, la fase, `Remando` apagado (SPM 0) |
| `SceneTimeOfDayTests` (STD) | LT-69 a LT-74; los ambientes de referencia; paleta (bit a bit) y astro del legacy (`Within(1e-9)`) contra el oráculo en cada minuto (y siete tamaños); el recorte de `SceneColor.Lerp` con `t` −0,5 y 1,5 (QA-6a-PAR-06) |
| `SceneHourTests` (SHR) | la hora efectiva (minutos, segundos ignorados, `hora + minuto/60`); la transición `Forced`: 12 → 18 en 1,5 s pasando por 15 a los 0,75 s, 23 → 1 por medianoche, 0 → 12 hacia adelante, un cambio durante otro, reducir movimiento en el acto; **`MinuteTick`**: de 10:00 a 10:01 en 1 s lineal (a los 0,5 s, 10:00:30), también de 23:59 a 0:00; `Instant`: un cambio de zona, la vuelta del segundo plano y `ForceHourForTest` en el acto; con reducir movimiento, todo en el acto |
| `CelestialPlacementTests` (CEL) | DV-52 con el arco de 10,5° (9:00 a 8,92°, 12:00 a 12°, 15:00 a 8,92°, 22:00 a 10,59°, 0:00 a 12°; los acimuts de la spec); el barrido minuto a minuto (los dos a −2,5° en el cambio, **≤ 0,3°/min**, el máximo ≈ 0,257; la luz **0** a las 19:34:19 y a las 4:25:41 y dirección continua salvo ahí); luz y sombras del lado opuesto; la fuerza del legacy |
| `SkyArtTests` (SKA) | las proporciones que son contrato (QA-6a-PAR-10, AC-SCN-11-05): halo = 4 × disco (sol y luna); disco de la luna = 0,8 × el del sol; halo de la luna = 0,8 × el del sol; los alfas del halo 0,7 / 0,25 / 0 |
| `SkyBackdropTests` (SKB) | la curva del telón (QA-6a-PAR-10, AC-SCN-11-02): `Gradient(0) = 0` (skyHorizon) y `Gradient(1) = 1` (skyTop) exactos; monótona no decreciente en 10 001 muestras; `Gradient(0,25) = 0,4375` y `Gradient(0,5) = 0,75`; `Fraction` recortada a [0; 1] |
| `CelestialMaskTests` (CMK) | QA-G6aT-08: 0 adentro del disco (también el lado oscuro), `1 − alfa del halo` adentro del halo (con `vis` y el factor de la luna), 1 afuera; continua en el borde suave |
| `MoonPhaseTests` (MOO) | los tres vectores (± 1 d); la fecha de prueba ≈ 94 %, menguante; el hemisferio (sin región, el norte; con `AR`, el espejo); el factor del halo `0,4 + 0,6·iluminación` |
| `HourLightingTests` (HLT) | los keyframes y la interpolación; `g` y la bruma; `Compose` con el velo (saturación ×0,7, exposición con `VeilEv` −0,50); la fuerza de sombra 0 de noche en Medio (y las palabras clave sin cambio) |
| `NightLightsTests` (NLT) | el umbral (19:26 y 4:43); cada luz en su minuto dentro de ±10; el fundido de 0,4 s |
| `StarFieldTests` (STR) | las 80 contra el oráculo en los siete tamaños; la primera en V390 (58,86; 173,37); Q-62 al minuto con la hora armada como el legacy: **6:12 sí, 6:13 no, 17:59 no, 18:00 sí**, 15:00 no (QA-6a-PAR-09); LT-82; `StarFader`: en régimen la fórmula de la spec; al cruzar el umbral, de 0 a la oscuridad de ese cuadro en 0,9 s (`ease.move`) y vuelta; `Snap` al abrir, al volver del segundo plano y con `ForceHourForTest` |
| `CloudModelTests` (CLD) | las 7; sin deriva por distancia (DV-56) y la del legacy como contrato; `Y`, alfa y colores; `WrapFade`: 0 en el punto de vuelta por los dos lados, 1 lejos de él, continua, y la `x` sin cambios |
| `HorizonModelTests` (HZN) | colores por hora contra el oráculo; `ApproachScale` (monótona, acotada, la tabla del Designer ± 0,001, sin deriva lateral); el alto por ventana: **9° en V390, TV y P360; `atan 0,12` = 6,843° en H844, TH, L640 y W1280**; la forma no depende de la ventana ni de `d`; la nieve: umbral 0,58, cobertura 45 % ± 3 en TV y 33 % ± 3 en TH; la regla del legacy (`LegacySnowPeaks`) como contrato: un pico a `d` 0 |
| `DartRandomTests` (DRN) | los cuatro vectores dorados y las tiradas del oráculo, bit a bit |
| `ShoreSegmentsTests` (SHS) | LT-75, LT-76, LT-77; los segmentos 0 a 2 000 por resumen y 0 a 99 enteros; las listas visibles (con los tramos de `z` iguales ordenados en los dos lados); el tope con el escenario de prueba (que afirma antes que no hay empates de `z` en el corte); el olvido (42 km sin crecer); sin asignar después de calentar (contador de .NET 8, solo dotnet) |
| `LakeEnvironmentTests` (LAK) | pinos (orden, rangos), muelles (175, 575, 975…), cabañas (360, 960, 1 560…), ola 0,25 / 2,5, ≤ 120 (92 a 0 m, 95 a 1 234 m, ≤ 102 en 100 km) |
| `ShoreShadingTests` (SSH), `DegradedWaterTests` (DWT) | contra el oráculo, bit a bit (DWT con la fuerza leída del archivo); la fase a 4 096 m y 42 195 m sin salto; `ScreenAlpha` por minuto: 0 a las 19:34 y a las 4:26, el alfa del legacy con la visibilidad en 1 (12 y 22 h) |
| `TerrainProfileTests` (TRN), `PropPlacementTests` (PPL), `ShoreFillTests` (SFL) | continuidad; nada flota en 100 km; el ancla de la cabaña igual al legacy y ningún tronco adentro; el legacy igual con y sin relleno; `|x| < 18` solo ≤ 1,2 m; nada del relleno fuera de la envolvente (0,20 a la izquierda); **pedir `k + 1` antes que `k`, y 0 a 200 en orden aleatorio, da el mismo relleno** (QA-G6aT-15 b); el trozo lejano sin la franja sumergida y la orilla a +0,4 m |
| `SceneGeometryTests` (GEO) | las medidas de §2.14 |
| `SceneSessionTests` (SSN) | de cero; sin reinicio al cambiar la ventana o pausar; el tiempo sigue en pausa; `Advance` en pasos; la distancia de escena no llega al motor (prueba de código y de comportamiento) |
| `ThermalAdaptationTests` (THA) | §2.16: el orden, los umbrales, la histéresis; **una serie a 30 fps (33 ms de intervalo) con 8 ms de trabajo no dispara**; suspendida (`renderFrameInterval` 2, escena oculta) no suma y al volver la ventana arranca de cero |
| `SceneWarmupPolicyTests` (SWP) | §2.16: los tramos (3 / 2 / 0 ms), 2 ms con el inmersivo abierto también después de la cascada, 0 durante un movimiento finito y en el cuadro del armado del HUD; ningún paso indivisible (la lista de unidades de §3.4 tiene todas costo acotado y el dibujo de calentamiento se permite donde se permite un cuadro de escena) |
| `SlotIdsTests` (SLT) | §2.17 |
| `HudContrastTests` (HCT) | AC-WKT-17-13 (§2.18) |
| `SceneGoldenTests` (SGT) | carga y formato de los seis JSON; el comparador de cada valor (§2, QA-6a-PAR-05), dicho en el encabezado |
| `ItSixAToolingTests` (I6T) | `run-oracle.ps1 -Scene` con la guardia; `capture-snapshots.ps1 -Set it6a`; los argumentos `[PERF]` y `-rowmate-scene-delay`, `-rowmate-quality` prohibidos en el build normal; sin rutas absolutas en `snapshots/**`; los tres *scripts* de Unity usan `ProjectSettingsGuard.ps1` |
| `PureCodeRulesTests`, `RepoHygieneTests` (C) | `Core/Scene` sin reloj del sistema, `UnityEngine` ni `Input`; `Assets/ThirdParty/` ignorado salvo `.meta` (`git check-ignore`); **ningún asset versionado referencia un GUID de `Assets/ThirdParty/` salvo los `.asset` de `Slots/`** (QA-G6aT-18); **`THIRD_PARTY_NOTICES.md` con la licencia del SDK de Dart si existe `DartRandom.cs`** (QA-G6aT-19); fuera de `Editor/Bootstrap/UrpProvisioner.cs` nadie asigna `QualitySettings.renderPipeline` ni `GraphicsSettings.defaultRenderPipeline`, y nadie usa `EditorSettings.asyncShaderCompilation` (QA-G6aT-05); en `Scene/`, los miembros `*ForTest` de Core solo se llaman desde miembros `internal` `*ForTest` (QA-G6aT-14); ninguna clave de `IPreferences` nombra la hora de la escena (AC-SCN-23-03) |

**Unity EditMode solo** (`Tests/UnityEditMode/`):

- `SceneAssetsTests` (B0): cada `SlotIds` con su `.asset`, con la fuente `Procedural` por defecto; los 3 niveles con **su asset de URP del proyecto** (no una copia) y el asset por defecto también; `renderScale` 1 en los tres; la capa `RowMateScene`; `PlayerSettings.enableFrameTimingStats` prendido; `SceneWorld.asset` completo.
- `SceneMaterialsTests` (B1): los 13 *shaders* de §7.5 encontrados, con sus colas, sin errores y compilados para Direct3D 11, GLCore, Vulkan y GLES 3 con la compilación del Editor (sin GPU: corre también en el CI de Linux); los materiales de `Art/Materials/Scene/`; la colección de variantes cubre los 13 y sus pasadas.

**PlayMode** (`Tests/PlayMode/Scene/`, B1 y B2). Las que dibujan y leen píxeles llevan `[Category("GPU")]` y `Assume` sobre `SystemInfo.graphicsDeviceType != Null`; las que afirman tiempos, `[Category("Perf")]` y sus topes de tiempo no corren en el CI (variable `CI`), donde quedan los conteos (QA-G6aT-18):

| Clase | Fija |
|---|---|
| `SceneProjectionTests` (SPR) | AC-SCN-02-01, -02 (marcas ± 1 dp en los siete tamaños con la cámara real, con `WorldToViewportPoint` × el tamaño de la RT), -03 (nada con z ≤ 0,5), -08 (una cámara, sin entrada) |
| `SceneCompositionTests` (SCP) | las sondas S6a-1, -5 y **-7** como regresión: el color en la RT de la UI; la RT escalada sin bloques; el número de cuadro de la escena leído en la RT de la UI en el mismo cuadro, 120 cuadros, también sin cambios del HUD (QA-G6aT-09) |
| `SceneLifecycleTests` (SLC) | LT-78 a LT-81, LT-83, LT-84 a LT-87 (también cerrando mientras carga); AC-SCN-01-03 a -07; tres entrenamientos sin avisos nuevos; **el orden de las colas** (cielo 1000 < estrellas 1001 < nubes 1002 < cordilleras 1003) y **las estrellas detrás del astro** (a las 22 h, con `SetStarsForTest` prendido y apagado: 0 diferencia adentro del disco y ≤ la de `CelestialMask` en el halo); `CameraIsDeterministic` (*dithering* apagado, sin TAA, sin grano); **la escena demorada** con `DelayReadinessForTest(1,5)`: aparece con su fundido, al día, y el marcador `RowMate.Scene.Warmup` no pasa de su tramo en ningún cuadro (con un margen de 1 ms por el Editor); el único dibujo de calentamiento antes de `Ready` y 0 después fuera del inmersivo |
| `SceneImmersiveTests` (SIM) | AC-WKT-13-02, -06, -08, -09, -12, -13, -14 (la secuencia del motor igual con escena, sin escena, con reducir movimiento y a cualquier hora); AC-SCN-02-05 a -07; AC-APP-05-08; AC-WKT-17-07, -09; AC-SCN-01-08 (el casco en la zona, el punto a 9,5 m en su centro ± 2 dp); AC-SCN-13-03 en pantalla (a las 19:34, 0 píxeles de línea) |
| `ScenePlacementTests` (SPL) | AC-SCN-15-05 (las anclas proyectadas por la cámara real ± 2 dp del legacy en los siete tamaños, a `E-0`, `E-920` y `E-1234`), -06 (medidas con los `bounds`), -07 (rayos al terreno); AC-SCN-14-07 (sonda de alfa en 360-400 m); el terreno y el relleno desde `d − 25 m` (sin huecos en el primer cuadro del vuelo, DN6a-11) |
| `SceneSlotsTests` (SSL) | AC-SCN-15-08: **todos con el procedural → 0 avisos**; el slot de prueba (entra; quitado, el procedural y un solo `[SLOT]`); una referencia a `Assets/ThirdParty/` sin el archivo → el procedural y un aviso; un `SlotLog` por anfitrión (no depende del orden de las pruebas) |
| `SceneBudgetTests` (SBT) | AC-SCN-01-11 por nivel (con `TierScope`, que restaura el nivel y el fixture lo afirma); AC-SCN-01-12 (0 B con `WatchScene`: las cuentas "escena" y "URP" y el control positivo en la misma corrida); AC-WKT-18-21 con la escena; AC-WKT-14-19 (la fila de 200 en Editor); el sobre-dibujo de la vegetación (§4.1) |
| `SceneAdaptationTests` (SAD) | AC-SCN-01-13 con tiempos de cuadro de prueba; 33 ms de intervalo con 8 ms de trabajo no dispara; en el Editor, las acciones del asset de URP solo se anotan y la resolución cambia la RT |
| `SceneLongSessionTests` (SLS) | AC-SCN-01-15 (`E-42K`: cámara y objetos estables, los del oráculo) |
| `SceneHourSimulatorTests` (PlayModeSimulator) | AC-SCN-23-01, -02, -04; AC-BLE-16-07, -02, -03; AC-WKT-19-03; AC-SCN-03-07 y AC-BLE-15-09 con `Simular desconexión` y `Remando` |
| `HudWarmupSceneTests` (HWS) | con `FakeImmersiveScene`: 0 suscriptores de `OutputChanged` después de desarmar y 0 llamadas a `Preload` u `Open` (QA-G6aT-16) |

**Equipo** (`WorkoutDeviceChecksTests`, `[Ignore]` con el motivo): `AndroidSceneSixtyFps` (AC-SCN-01-09), `WindowsScenePerf` (AC-SCN-01-10, AC-WKT-13-15 con la salida y la escena demorada, AC-WKT-14-19; 0 compilaciones de *shaders* de la escena después de `Ready`; la línea de base de GPU con Medio), `WindowsSceneHourDoesNotSurviveRestart` (AC-SCN-23-03), `WindowsThreeSessionsCleanLog` (AC-SCN-01-05), `WindowsGpuIdleInWorkout` (AC-SCN-01-06).

### 6.4 Los 26 casos legacy (LT-62 a LT-87)

| LT | Caso | Test | Dónde |
|---|---|---|---|
| LT-62 | `horizonte y focal según orientación` | `SCC.HorizonAndFocalByOrientation` | dotnet |
| LT-63 | `lo lejano queda más cerca del horizonte, centrado y más chico` | `SCC.FarIsCloserToTheHorizonCenteredAndSmaller` | dotnet |
| LT-64 | `detrás de la cámara no se proyecta` | `SCC.BehindTheCameraDoesNotProject` | dotnet |
| LT-65 | `unprojectWater invierte project para y = 0` | `SCC.UnprojectWaterInvertsProject` | dotnet |
| LT-66 | `la velocidad sale del split y se alcanza en unos segundos` | `SST.SpeedComesFromTheSplitAndIsReachedInSeconds` | dotnet |
| LT-67 | `al dejar de remar desacelera hasta detenerse` | `SST.StoppingDeceleratesToRest` | dotnet |
| LT-68 | `shaderDistance se acota a 4096 m` | `SST.ShaderDistanceWrapsAt4096` | dotnet |
| LT-69 a LT-74 | los seis de `time_of_day_test` | `STD.NoonIsDayWithTheSunUp`, `ElevenPmIsNightWithMoon`, `HalfPastSixInterpolatesSunsetAndDusk`, `TenPmToFourAmIsDeepNight`, `SevenPmIsStillDayAndEightPmHasTheMoon`, `PaletteIsContinuousAtMidnightAndDawn` | dotnet |
| LT-75 a LT-77 | `EnvironmentId.lake: …` (3) | `SHS.LakePropsForSegmentIsDeterministic`, `LakeSegmentsHaveProps…`, `LakeVisiblePropsSortedAndCapped` | dotnet |
| LT-78 a LT-81 | `EnvironmentId.lake WxH h pinta sin excepciones` (4) | `SLC.LakePaintsWithoutExceptions(390, 844, 12)` y sus variantes, a 0 y 1 234 m, t 3,2 | PlayMode |
| LT-82 | `las estrellas aparecen apenas oscurece (19 h)…` | `STR.StarsAppearAsSoonAsItDarkens` | dotnet |
| LT-83 | `sin shader pinta el fallback sin excepciones` | `SLC.DegradedWaterPaintsOnAFreshScene` | PlayMode |
| LT-84 a LT-87 | `EnvironmentId.lake WxH h anima sin excepciones` (4) | `SLC.LakeAnimatesWithoutExceptions(…)`: SPM 24, split 110, 300 m, en marcha, hora forzada; 6 cuadros de 100 ms; el cierre (también cargando) | PlayMode |

Cada prueba lleva `Category("LT-nn")` y las de sus AC.

### 6.5 Matriz AC → test / captura (`It06aIds.cs`)

`It06aIds` lista los **101 ID** de la spec (78 nuevos, 11 que cambian y 12 que se vuelven a verificar; AC-BLE-16-02 y -03 son dos aunque vayan en una fila) y LT-62 a LT-87. Marcas: d = dotnet (y Unity EditMode) · u = Unity EditMode solo · p = PlayMode · ps = PlayMode con simulador · cap = captura · eq = equipo (`[Ignore]` con motivo) · qa = revisión de QA.

| AC | Verificación | Tests y tomas |
|---|---|---|
| AC-SCN-02-01 | d p | SCC, SPR · CAP-13 a -17 |
| AC-SCN-02-02 | d p | SCC (matriz), SPR (marcas) |
| AC-SCN-02-03 | d p cap | SCC, SPR · VID-03 |
| AC-SCN-02-04 | d cap | CCM (barrido) · VID-03, VID-10 |
| AC-SCN-02-05 | d p cap | CCM, SIM · VID-01, VID-02 |
| AC-SCN-02-06 | d p cap | CCM, SIM · VID-07 |
| AC-SCN-02-07 | d p cap | CCM, SIM · CAP-22, VID-08 |
| AC-SCN-02-08 | d p | `PureCodeRulesTests`, SPR |
| AC-SCN-03-01 a -03 | d (cap) | SST · CAP-20, VID-04 |
| AC-SCN-03-04 | d p | SST, SLC |
| AC-SCN-03-05 | d p cap | SSN, SIM · CAP-13 |
| AC-SCN-03-06 | d | SST |
| AC-SCN-03-07 | d ps | SIN, `SceneHourSimulatorTests` |
| AC-SCN-09-01 | d p | SHR, SLC |
| AC-SCN-09-02, -03 | d | STD |
| AC-SCN-09-04 | d cap | HLT · CAP-01 a -04 |
| AC-SCN-09-05 | d cap qa | HLT · CAP-01 a -04, VID-05, VID-06 |
| AC-SCN-09-06 | d cap | NLT · CAP-04, CAP-09, VID-05 |
| AC-SCN-10-01 a -03 | d | STD, CEL |
| AC-SCN-10-04 | d cap | CEL · CAP-01 a -04, -10, -11 |
| AC-SCN-10-05 | d cap | CEL · CAP-08, -09, VID-05, -06 |
| AC-SCN-10-06 | d cap | CEL · CAP-01, -03 |
| AC-SCN-10-07 | d cap | MOO (fase, espejo con la región de prueba, factor del halo), SKA (0,8 y 4 ×) · CAP-04, -10 |
| AC-SCN-11-01 | d p cap | CMK, SLC (orden de colas y las estrellas detrás del astro) · CAP-01 a -04 |
| AC-SCN-11-02 | d cap qa | SKB (los extremos y la monotonía de la curva) · CAP-01 a -07 contra LEG-01 a -07, sin degradés, con las sondas de color de arriba y del horizonte |
| AC-SCN-11-03 | d cap | STR · CAP-10 |
| AC-SCN-11-04 | d cap | STR · CAP-08, -11 |
| AC-SCN-11-05 | d cap qa | SKA (halo = 4 × disco; luna = 0,8 × sol) · CAP-02 (con la sonda del brillo), CAP-10 |
| AC-SCN-11-06 | d cap | CLD · CAP-02, VID-03 |
| AC-SCN-11-07 | p | SLC (LT-78 a -81) |
| AC-SCN-14-01 a -06 | d | SHS, DRN, LAK |
| AC-SCN-14-07 | p cap | SPL · VID-03 |
| AC-SCN-15-01 a -04 | d | LAK |
| AC-SCN-15-05 | d p cap | SHS, SPL · CAP-02, -05, -07 |
| AC-SCN-15-06 | d p cap | GEO, SPL · CAP-02, -04 |
| AC-SCN-15-07 | d p cap qa | TRN, PPL, SFL, SPL · CAP-01, -02, -07, -12 |
| AC-SCN-15-08 | d p qa | `RepoHygieneTests` (nada fuera de los slots referencia `Assets/ThirdParty/`), SSL (0 avisos con el procedural; un aviso con un asset que falta) · `git check-ignore`, CI sin el pack |
| AC-SCN-23-01 | ps eq | `SceneHourSimulatorTests` · `[Ignore]` del build |
| AC-SCN-23-02 | d ps cap | SHR · VID-05, VID-06 |
| AC-SCN-23-03 | ps eq | `SceneHourSimulatorTests` (un anfitrión nuevo vuelve a real) · `WindowsSceneHourDoesNotSurviveRestart` |
| AC-SCN-23-04 | ps cap | `SceneHourSimulatorTests` · DEMO-04 |
| AC-SCN-01-02 | p cap | SIM · CAP-13 a -17 |
| AC-SCN-01-03 | d p cap | SSN, SLC · VID-09 |
| AC-SCN-01-04 | p eq | SLC (ciclo de vida de prueba) · Android NV |
| AC-SCN-01-05 | p eq | SLC, SSL (sin el pack, 0 `[SLOT]`), HWS · `WindowsThreeSessionsCleanLog` |
| AC-SCN-01-06 | p eq | SLC (un dibujo de calentamiento antes de `Ready`, 0 después), SIM · DEMO-01 · `WindowsGpuIdleInWorkout` |
| AC-SCN-01-07 | p cap | SLC (`DelayReadinessForTest`) · CAP-24, VID-11 |
| AC-SCN-01-08 | p cap | SIM · CAP-13 a -17 |
| AC-SCN-01-09 | eq | `AndroidSceneSixtyFps` (NO VERIFICABLE) |
| AC-SCN-01-10 | eq | `WindowsScenePerf` (`[PERF]` en el build) |
| AC-SCN-01-11 | p | SBT |
| AC-SCN-01-12 | p | SBT |
| AC-SCN-01-13 | d p eq | THA, SAD · Android NV |
| AC-SCN-01-14 | d u | todas las de dotnet en EditMode, con y sin simulador |
| AC-SCN-01-15 | d p cap | DWT, SHS, SLS · CAP-12 |
| AC-SCN-13-01 | p | SLC |
| AC-SCN-13-02, -03 | d p cap | DWT (el dato contra el oráculo y `ScreenAlpha` por minuto), SIM (a las 19:34, 0 píxeles de línea) · CAP-01 a -09, VID-03, VID-05 |
| AC-SCN-13-04 | p cap | SLC (LT-83) |
| AC-SCN-19-01 | d cap | HZN · CAP-01 a -07 |
| AC-SCN-19-02 | d cap | HZN · VID-03 |
| AC-SCN-20-01 | d cap | SSH · CAP-02, -04 |
| AC-SCN-20-02 | d cap | NLT · CAP-04, -09 |
| AC-SCN-21-01 | p qa | SLC (siempre el lago, `scene.environment` no se lee: prueba de código) |
| AC-WKT-17-13 | d cap | HCT · CAP-19, CAP-23 |
| AC-WKT-13-02 | p cap | SIM · CAP-13 a -17 |
| AC-WKT-13-06 | p cap | SIM · VID-01, -02 |
| AC-WKT-13-08 | p cap | SIM · VID-07 |
| AC-WKT-13-12 | d p cap | HLT, SIM · CAP-20, -21, VID-04 |
| AC-WKT-13-15 | d p eq | SWP, SIM (el orden), SLC (la escena demorada por tramos) · `WindowsScenePerf` (las entradas, la salida de 0 a 1 s y la escena demorada) |
| AC-WKT-14-19 | p eq | SBT · `WindowsScenePerf`, Android NV |
| AC-WKT-17-07 | p cap | SIM · VID-09 |
| AC-WKT-17-09 | p cap | SIM · CAP-13 a -18 |
| AC-BLE-16-07 | d ps cap | SHR, `SceneHourSimulatorTests` · DEMO-04 a -08 |
| AC-SCN-01-01 | p cap | SLC · CAP-24 |
| AC-APP-05-08 | p cap | SIM · VID-02 |
| AC-WKT-13-09, -13-13, -13-14 | p (d) | SIM, SSN |
| AC-WKT-19-03 | ps | `SceneHourSimulatorTests` |
| AC-WKT-17-01 | p cap | SIM · CAP-13 |
| AC-WKT-02-08 | p | SLC |
| AC-WKT-18-21 | p | SBT |
| AC-BLE-16-02, -03 | ps cap | `SceneHourSimulatorTests` (`WrenchAudit` con la escena) · DEMO-04 |
| AC-BLE-15-09 | ps | `SceneHourSimulatorTests` |
| AC-HIS-20-04 | d u qa | `ItSixAToolingTests` (las corridas con `ROWMATE_DATA_DIR`), el resguardo de la carpeta del usuario |
| AC-WKT-21-01 | p | SIM (una vez, ahora con la toma aérea) |

---

## 7. Paquetes de trabajo

### 7.0 Orden y reglas

0. **Antes de WP-A**: §9 (las notas del Designer y los pedidos de la base, con un valor por constante, alineados con la spec corregida). WP-A escribe sus pruebas contra esos valores; si alguno cambia después (WP-D2), cambia en `Core/Scene/Art` y en la prueba que lo lee, nunca como literal.
1. **WP-A** (dotnet, sin Unity) → 2. **WP-B0** (Unity: sondas S6a-1 a -7, `UrpProvisioner` con los niveles, la capa y *Frame Timing Stats*, los slots con el procedural asignado, `SceneAssetsBuilder` sin materiales, `SceneShaderIds` con **todo** el contrato de §7.5, `IImmersiveScene`, la guardia de `ProjectSettings`, ADR 022) → 3. **WP-D1** (los 13 *shaders* sobre los nombres de `SceneShaderIds`, los *includes*, `Core/Scene/Art`, `SceneFadeMotion`, `HudReflowMotion`, `PauseVeilMotion`) → 4. **WP-B1** (la escena: `SceneHost`, armado, precalentado, calidad, *streamers*, `SceneSurface`, los materiales y la colección de variantes, las pruebas PlayMode de la escena) → 5. **WP-B2** (el inmersivo, la precarga, el simulador, niveles y adaptación, `[PERF]`, build y avisos de licencias) → 6. **WP-B3** (`it6a`, A y B; regeneración única de `it4b` e `it5`) → 7. **WP-D2** (ajustes con las tomas, `VeilEv`, la tabla de la luz, las biblias).
2. Una sola instancia de Unity por proyecto; nunca se editan fuentes con una instancia propia abierta; nunca se toca la del usuario. Si Unity revienta al cerrar un *batchmode* que cambió *defines*, los *scripts* lo absorben.
3. Ninguna prueba, toma ni video toca `prefs.json` ni las bases del usuario (`ROWMATE_DATA_DIR` temporal siempre).
4. **`ProjectSettings` y `Assets/RowMate/Settings/` solo los escribe el *bootstrapper*** (B0 los niveles, la capa, *Frame Timing Stats* y los assets de URP; B1 los materiales y la colección de variantes), y esos cambios se versionan (G). Toda otra corrida de Unity (pruebas, arnés, build) pasa por `ProjectSettingsGuard.ps1` y falla si cambió algo (§4.2).
5. Repo público: rutas relativas, el legacy como su URL o `../rowmate-legacy`, `<scratch>`, `<flutter-sdk>`; sin datos del equipo.

### 7.1 WP-A — Core de escena puro y oráculo (dotnet)

Todo `Core/Scene/**` (incluidos `SkyBackdrop`, `CelestialMask`, `StarFader`, `CloudModel.WrapFade` y `Core/Scene/Art/**` con los valores de §9, que pasa a WP-D después), `Core/Design/{SceneTimelines,HudContrast}.cs`, `Core/Design/MotionSpecs.cs` (el bloque de la 6a), `Core/AssemblyInfo.cs` (`InternalsVisibleTo("RowMate.Scene")`), `App/Scene/SceneSettings.cs` (comentario), `THIRD_PARTY_NOTICES.md`, `Tests/EditMode/Scene/**`, `Tests/Support/SceneSeeds.cs`, `Tests/EditMode/Traceability/It06aIds.cs` (101 ID), `Tests/EditMode/Project/{PureCodeRulesTests,ItSixAToolingTests,RepoHygieneTests,GoldenCoverageTests}.cs`, `tools/oracle/**` (`-Scene`), `Tests/Golden/oracle/scene_*.json`, `snapshots/legacy/LEG-*.png`. **Entrega**: `dotnet test` verde (0 avisos) en el wrapper, la matriz con los ID `d` pasando, el oráculo corrido con la guardia y el legacy limpio.

### 7.2 WP-B — escena URP, integración y capturas (Unity)

- **B0**: `Scene/{AssemblyInfo,SceneShaderIds,SceneRenderTarget,SceneRenderer}.cs`, `Scene/Slots/**`, `Scene/Quality/QualityTierProfile.cs`, `Presentation/Workout/IImmersiveScene.cs`, `Editor/Bootstrap/{UrpProvisioner,SceneAssetsBuilder}.cs`, lo que generan (G: `ProjectSettings/{QualitySettings,TagManager,ProjectSettings,GraphicsSettings}.asset`, `Settings/URP/RowMate_URP_{High,Medium,Low}.asset` y sus *renderers*, `Settings/Quality/*.asset`, `Slots/*.asset`, `Scenes/SceneWorld.asset`), `Tests/UnityEditMode/SceneAssetsTests.cs`, `tools/unity/{ProjectSettingsGuard.ps1,run-unity-tests.ps1}`, `.gitignore`, `docs/migration/architecture/adr/022-escena-en-el-panel.md`, el informe de las sondas (en "Cambios por implementación" de este documento). Las sondas usan *quads* con el *Unlit* de URP: no necesitan los *shaders* de D1.
- **B1**: el resto de `Scene/**` (`SceneHost`, `SceneWorldBuilder`, `SceneShaderWarmup`, `Quality/SceneQualityApplier`, `SceneDirector`, `ChaseCameraRig`, `SkyRenderer`, `HorizonRenderer`, `WaterSurface`, los *streamers*, `BoatVisual`, `SceneLighting`, `ScreenOverlay`, `MeshUpload`, `ProceduralTextures`), `Presentation/Workout/SceneSurface.cs`, `Editor/Bootstrap/SceneMaterialsBuilder.cs` y lo que genera (G: `Art/Materials/Scene/*.mat`, `Settings/Scene/SceneShaderVariants.shadervariants`), `Editor/Slots/SlotValidator.cs`, `Tests/UiSupport/{UiTestHost,UiHostOptions,DeviceProfile,FrameAllocations,TierScope}.cs`, `Tests/UnityEditMode/SceneMaterialsTests.cs`, `Tests/PlayMode/Scene/{SceneProjectionTests,SceneCompositionTests,SceneLifecycleTests,ScenePlacementTests,SceneSlotsTests,SceneBudgetTests,SceneLongSessionTests}.cs`.
- **B2**: `Presentation/Workout/{HudView,HudBackdrop,ImmersiveWorkoutPage,WorkoutScreenView,PillRowView}.cs`, `Presentation/Shell/UiContext.cs`, `Platform/Display/{GraphicsTier,ThermalAdapter,PlayerUrpAssetQuality}.cs`, `Bootstrap/{SceneBridge,AppRoot,ShellHost,UiAssets}.cs`, `Editor/Bootstrap/BootSceneBuilder.cs`, `Editor/Build/{BuildOutputInspector,NoticesPostprocessor}.cs`, `SimulatorPresentation/FrameTimeProbe.cs`, `tools/build/build.ps1`, `Tests/UiSupport/FakeImmersiveScene.cs`, `Tests/PlayMode/Scene/{SceneImmersiveTests,SceneAdaptationTests}.cs`, `Tests/PlayMode/{HudBudgetTests,HudWarmupSceneTests}.cs`, `Tests/PlayModeSimulator/SceneHourSimulatorTests.cs`, `Tests/EditMode/Workout/WorkoutDeviceChecksTests.cs`, el build y el `[PERF]`.
- **B3**: `Snapshots/**`, `tools/unity/{capture-snapshots.ps1,README.md}`, `docs/migration/iterations/06a-escena-lago/snapshots/**` (salvo `legacy/`), la regeneración de `it4b` e `it5`, `ARCHITECTURE.md` y `adr/015`.

### 7.3 WP-D — arte, shaders, luz y motion (Designer)

- **D1**: `Art/Shaders/Include/{RowMateScene,RowMateFog,RowMateDither,RowMateLighting}.hlsl`; `Art/Shaders/Scene/{Sky,Stars,Clouds,Horizon,WaterDegraded,Terrain,Prop,Foliage,Grass,Reeds,Hull,BoatShadow,ScreenOverlay}.shader`, escritos contra los nombres de `SceneShaderIds` (B0) y con las cuentas de Core que copian (`SkyBackdrop.Gradient` en `Sky`, `CelestialMask.StarAttenuation` en `Stars`, `DegradedWater.ScreenAlpha` en `WaterDegraded`); desde acá, dueño de `Core/Scene/Art/**`; `Presentation/Motion/Hud/{SceneFadeMotion,HudReflowMotion}.cs` y `PauseVeilMotion.cs`. Los shaders de D1 cumplen el contrato de §7.5 y se ven bien en el *placeholder* (ASSET_SLOTS §8). Los materiales los crea B1 (`SceneMaterialsBuilder`), así B0 nunca depende de un *shader* que todavía no existe (QA-G6aT-14).
- **D2**: los ajustes con las tomas de `it6a` (la tabla de la luz, `VeilEv`, recetas, densidades, los degradés de pantalla, las constantes de `WrapFade`), las biblias (ART_BIBLE §6, §7.1, §7.2, §7.7, §8, §9; ANIMATION_BIBLE §2.11, §2.12, §5, §6; ASSET_SLOTS §1, §2, §7), `04-design-notes.md` con la lista de Mixamo para la 7 y la tabla de §4.1 si la mide distinta.

### 7.4 El contrato de motion (WP-D1 ↔ WP-B2)

| `IMotion` | Constructor | Entrada | Salida | Con reducir movimiento |
|---|---|---|---|---|
| `SceneFadeMotion` | `(MotionDriver, Action<double> apply)` | `Play()` cuando la escena demorada queda lista | opacidad de `scene-surface` 0 → 1 en 0,6 s (`ease.enter`); al terminar avisa (la página oculta el provisorio) | igual (es un fundido) |
| `HudReflowMotion` | `(MotionDriver, Action<double> apply)` | `OnOrientationClassChanged()` | opacidad común de las piezas del HUD 0 → 1 en 200 (`ease.enter`), desde donde esté | en el acto |
| `PauseVeilMotion` (C) | el de la 4b | `Pause` / `Resume` | el alfa 0 → 0,15 (300) y vuelta (240), que `HudView` pasa al fondo y a la escena (`alfa/0,15`) | igual |

Los tiempos salen de `SceneTimelines` (WP-A). La cámara (vuelo, salida, aérea, la entrada de la respiración), la hora visible, el fundido de las estrellas, el de las luces y el de la vuelta de las nubes no son `IMotion`: son de `SceneSession` (Core), en el `Tick` de la escena, y se descartan con ella.

### 7.5 El contrato de los shaders (WP-D1 ↔ WP-B1; `SceneShaderIds`)

`SceneShaderIds` (B0) tiene **todos** los nombres de esta sección como `Shader.PropertyToID` estáticos y las palabras clave como `LocalKeyword`/nombres; D1 escribe contra ellos (QA-G6aT-01: el contrato incluye lo que suma 04-design-notes §14.2).

| Qué | Contrato |
|---|---|
| Nombres | `RowMate/Scene/{Sky,Stars,Clouds,Horizon,WaterDegraded,Terrain,Prop,Foliage,Grass,Reeds,Hull,BoatShadow,ScreenOverlay}`; URP, sin `_Time` (todo por propiedades); sin `only_renderers`/`exclude_renderers`: compilan para Direct3D 11, Vulkan, GLES 3.2, Metal y **GLCore** (el CI de Linux, QA-G6aT-18) |
| *Includes* | `RowMateScene.hlsl` (los globales, `τ` y `q`, el disco), `RowMateLighting.hlsl` (04-design-notes §10.1), `RowMateFog.hlsl` (la del legacy, la extendida y la bruma baja), `RowMateDither.hlsl` (*Bayer* 4 × 4 fijo en pantalla: aparición, tramado cercano, cambio de LOD) |
| Globales (las pone `SceneLighting` en cada cuadro, en lineal los colores) | `_RmSceneTime`; la paleta `_RmPaletteSkyTop`, `_RmPaletteSkyHorizon`, `_RmPaletteWaterNear`, `_RmPaletteWaterFar`, `_RmPaletteSunColor`, `_RmPaletteSunGlow`, `_RmPaletteFog`; `_RmAmbient` (el del legacy), `_RmAmbientSky/Equator/Ground`; `_RmFogParams` (80; 320; 0,8; 1 200); `_RmLowMist`; `_RmGolden`; el astro: `_RmDiskDir` (dirección), `_RmDiskParams` (visibilidad; es luna; iluminación; ángulo de fase), **`_RmDiskShape`** (radio del disco en rad; radio del halo en rad; espejo del sur ±1; intensidad HDR); `_RmLightDir`; la ventana: `_RmHorizonY`, `_RmFocalDp`, `_RmViewDp` (ancho; alto); `_RmDensity`; **`_RmZonePhase`** (la fase de cinta de `d` en `double` desde Core, para el ruido del terreno) |
| `Sky` (1000, sin profundidad) | `_RmMoonTex` (`sky.moon`), `_RmGlowParams` (0,35; 14°; 20°), `_RmStarField` (densidad; alfa mín.; alfa máx.; solo Alto), `_RmMistBandDeg` (1,5°); el telón con `SkyBackdrop.Gradient` |
| `Stars` (1001, aditivo, instanciado) | por instancia `_RmStarDir` (dirección; radio en rad) y `_RmStarAlpha` (con el fundido); la máscara del astro con `_RmDiskDir`, `_RmDiskParams` y `_RmDiskShape` (`CelestialMask`) |
| `Clouds` (1002, alfa, instanciado) | `_RmCloudAtlas` (`sky.clouds`), `_RmCloudLayer` (la capa lejana, solo Alto); por instancia `_RmCloudDir` (dirección; tamaño en rad), `_RmCloudVariant`, `_RmCloudAlpha` (cuerpo; sombra; `WrapFade`) |
| `Horizon` (1003, sin profundidad) | por material `_RmHorizonTone`, `_RmHorizonDepth`, `_RmSnowEnabled` (lejano 1, cercano 0: un valor, no una variante); `_RmHorizonScale` (`tan Θ·k(d)`), `_RmSnowParams` (0,58; 0,03; 0,04; 1,7°), `_RmRim` (P2); por vértice `p`, pendiente, semilla |
| `WaterDegraded` (2450) | `_RmWaterBandTop`, `_RmWaterBandMid`, `_RmWaterBandBottom`, `_RmShimmer` (fase en [0; 0,1); `x` del disco; fuerza; **visibilidad del astro**), `_RmShimmerColor`, `_RmWaterExtras` (reflejo 0,25 y 30-400 m; somera 0,25 y 8,4-9,0 m; bruma 0,6), `_RmShallowColor`; palabra clave `RM_WATER_EXTRAS` (prendida; el gate la podía apagar si no aceptaba DT6a-07: lo aceptó) |
| `Terrain` (2000) | `_RmNoiseTex` (256²), los colores de 04-design-notes §7.5 (`_RmTerrainColors`), `_RmZonePhase` (global); con el pack, `_RmLayerAlbedo` y `_RmLayerNormal` (`Texture2DArray`) y la palabra clave `RM_TERRAIN_LAYERS` (fija por proceso: con o sin el pack) |
| `Prop` (2000, instanciado) | `_RmPlankParams`, `_RmWindowDay`, `_RmWindowNight` (HDR); por instancia `_RmFade`, `_RmLit` |
| `Foliage` (2000, instanciado) | `_RmPalettes` (los 3 × 3 verdes del legacy y los del bosque); por instancia `_RmVariant`, `_RmWindPhase`, `_RmFade`; por vértice grada, oclusión, peso del viento |
| `Grass` (2000, instanciado) | `_RmGrassColors`, `_RmGrassEnd` (fin y tramado); por instancia `_RmTint`, `_RmWindPhase` |
| `Reeds` (2000, instanciado) | `_RmReedColors`; por instancia `_RmWindPhase`, `_RmFade` |
| `Hull` (2000) | `_RmHullColors` (4), `_RmGelcoat` (0,85; 64; 0,3) |
| `BoatShadow` (2451, alfa) | `_RmShadowOffset` (± 0,35 m), `_RmShadowAlpha` (`0,08·ambient`) |
| `ScreenOverlay` (3500) | `_RmScrimTop` (`#06122A`; 0,38; 0,24), `_RmScrimBottom` (0,32; 0,18); `SetScrimsForTest(false)` apaga el *renderer* |
| Instanciado | las propiedades por instancia van en `UNITY_INSTANCING_BUFFER_START(Props)` / `UNITY_DEFINE_INSTANCED_PROP` con `#pragma multi_compile_instancing`, y se cargan con arreglos de `MaterialPropertyBlock` reservados una vez (`Graphics.RenderMeshInstanced`); lo que no se instancia, con las propiedades en `UnityPerMaterial` para el SRP Batcher |
| Palabras clave | **constantes durante la sesión** (QA-G6aT-07): las de sombras de URP según el nivel (de noche la fuerza baja a 0, no se apaga el mapa), `RM_WATER_EXTRAS` prendida, `RM_TERRAIN_LAYERS` según el pack, la niebla fija, el instanciado. Ninguna cambia por la hora ni por el velo |
| Capas y colas | capa `RowMateScene`; cielo 1000, estrellas 1001, nubes 1002, horizonte 1003 (sin escribir profundidad); opacos 2000; agua 2450; sombra del bote 2451; degradés de pantalla 3500 |
| Presupuesto de fragmento | `Sky` ≤ 35 ALU y 1 textura; `Stars` ≤ 8 (con la máscara); `Clouds` ≤ 12 y 1-2 texturas; `Horizon` ≤ 15; `WaterDegraded` ≤ 24 y 0 texturas (DT6a-07); `Terrain` ≤ 35 y 2 lecturas (≤ 40 y 5 con el pack); `Prop` ≤ 30; `Foliage` ≤ 25; `Grass` y `Reeds` ≤ 18; `Hull` ≤ 25; `BoatShadow` ≤ 5; `ScreenOverlay` ≤ 4 |

### 7.6 Excepciones de dueño

- `Core/Scene/Art/**`: lo crea WP-A con los valores de §9; desde WP-D1 es de WP-D (WP-A no lo vuelve a tocar; si una prueba de WP-A depende de un valor de arte, lee la receta, no un literal).
- `Core/Design/MotionSpecs.cs`: WP-A suma el bloque de la 6a; WP-D2 puede ajustar sus valores dejando las pruebas al día con WP-A.
- `Presentation/Motion/Hud/PauseVeilMotion.cs`: WP-D1; `HudView` (WP-B2) solo le cambia el *callback*.
- **Los *scripts* de Unity**: la guardia es un archivo propio (`ProjectSettingsGuard.ps1`, B0) y cada *script* la engancha en su paquete (`run-unity-tests.ps1` en B0, `build.ps1` en B2, `capture-snapshots.ps1` en B3): ningún archivo en dos paquetes.
- **El *bootstrapper***: `SceneAssetsBuilder` (B0: niveles, slots, `SceneWorld`) y `SceneMaterialsBuilder` (B1: materiales y variantes) son dos archivos, así B0 no afirma ni crea nada que dependa de los *shaders* de D1 (QA-G6aT-14).

---

## 8. Riesgos y desvíos

### 8.1 Desvíos propuestos (ninguno cambia un dato de la sesión)

| ID | Qué | Por qué | Estado después del gate |
|---|---|---|---|
| **DT6a-01** | Rolido de la respiración **0,15°** (la spec decía 0,18°, AC-SCN-02-04) | con 0,18° el horizonte se aparta 3,267 dp en W1280 y la misma AC fija ±3 dp en los siete tamaños; con 0,15°, 2,932 (barrido de 10 min). Es R6a-05 del Designer | **Aceptado**: la spec corregida dice 0,15° |
| **DT6a-02** | La velocidad del astro con tope (la spec decía 0,2°/min, AC-SCN-10-05) | con las ventanas de la spec el astro tiene que subir o bajar ≈ 14° en 62 min: ninguna curva continua entra en 0,2. Con el arco de 10,5° se mide **0,257°/min** (0,331 con el de 15,5°) | **Aceptado con 0,3°/min** (DN6a-03; spec corregida) |
| **DT6a-03** | El puerto del inmersivo es `Presentation/Workout/IImmersiveScene`, no `App.ISceneHost` (ARCHITECTURE §3.2) | lleva la `Texture` de la escena; App es puro. Lo implementa Bootstrap sobre `SceneHost`: Presentation y Scene siguen sin verse | Técnico |
| **DT6a-04** | Sin escenas aditivas en la 6a (ADR 015): el mundo se arma por código desde `SceneWorldDefinition` | todo es procedural o de un slot; una escena `.unity` no aporta y suma estado asíncrono y un archivo generado. Vuelven en la 8 si hacen falta | Técnico (ADR 015 corregido en B3) |
| **DT6a-05** | La escena se dibuja **a pedido** y entra al panel de la UI como fondo de `scene-surface` (ADR 022) | la composición sRGB del vidrio (ADR 019, AC-WKT-17-13), 0 cuadros de atraso, el arnés sin cambios, nada se dibuja fuera del inmersivo. Lo comprueban las sondas S6a-1 a -7 antes de construir | Técnico; S6a-7 sumada (QA-G6aT-09) |
| **DT6a-06** | Al rotar, el HUD **entra con un fundido de 200** en el *layout* nuevo, sin foto del anterior, y solo cuando cambia la clase de ventana (AC-WKT-17-07 dice "fundido cruzado" al rotar o cambiar el tamaño) | un fundido cruzado necesita una foto del HUD sin la escena, que con un solo panel no existe, y la foto de una ventana vertical sobre una horizontal se ve mal; en Android el sistema ya hace su fundido de la rotación; un cambio de tamaño dentro de la misma clase (arrastrar la ventana) se reacomoda en el acto, como hoy, para no titilar | Aceptado por el Designer y por QA (§7.4 de su informe). **La spec todavía no lo registró** (AC-WKT-17-07 y VID-09 dicen "fundido cruzado"): se construye así y el PO lo registra o lo rechaza (si lo rechaza: la foto con una RT aparte de solo el HUD, +1 panel, ≈ 1 ms) (§8.4) |
| **DT6a-07** | El agua degradada en **≤ 24 ALU** (ART_BIBLE §7.7 decía 12), con los tres tramos y las 10 líneas del legacy **sin moverlos** y encima los agregados del Designer | AC-SCN-13-04 admite mejorarla sin sacar ni mover lo del legacy; los 12 ALU no alcanzan para eso | **Aceptado** (spec corregida) |
| **DT6a-08** | La cabaña **nunca se corre**: ancla fija, profundidad de 4, 3 o 2,5 m hacia `+z` y, si aún choca, poda del pino | la regla de la base (corrida ≤ 0,5 m en `z`) mueve el eje hasta 7,5 dp en W1280 a 25 m: rompe AC-SCN-15-05 (± 2 dp) | Aceptado por el Designer |
| **DT6a-09** | Juncos de 0,6 a 1,2 m en `|x| < 18` (la base pedía 0,8-1,6) | P-09: ahí solo lo bajo, ≤ 1,2 m | Aceptado por el Designer |
| **DT6a-10** | El velo de pausa se compone en Core con la luz de la hora en **un solo** `Volume` | URP interpola valores absolutos entre `Volume`s: de noche (saturación −20) un segundo `Volume` a −30 desaturaría solo 10 | Aceptado por el Designer |
| **DT6a-11** | ~~La escena demorada espera al fin de la cascada para su primer dibujo~~ → **la escena demorada termina de prepararse por tramos de ≤ 2 ms, también después de la cascada, sin pasos indivisibles** | QA-G6aT-03: la versión anterior se contradecía (el primer dibujo "indivisible" no tenía cuándo ocurrir). Ahora las variantes se calientan de a una y el único dibujo de calentamiento cuesta lo de un cuadro de escena (§2.16, §3.4, §3.9) | Reemplazado en el gate; AC-WKT-13-15 corregido por el PO |
| **DT6a-12** | 0 elementos netos en el HUD, +1 solo durante el fundido de 0,6 s de la escena demorada | `scene-surface` reemplaza al fondo provisorio | — |
| **DT6a-13** | `ChaseCameraMotion` en `Core/Scene/` (ARCHITECTURE §2 lo ponía en `Stroke/`) | es de la cámara; la 7 le suma los términos del bote | — |
| **DT6a-14** | Sin impostores de pinos en la 6a (LOD2 de 120 triángulos hasta 400 m) | 4,2 k triángulos en el peor tramo; el impostor necesita un horneado por variante | Aceptado por el Designer; en la 6b si la medida lo pide |
| **DT6a-15** | La caché de segmentos es de cada sesión de escena, no estática y global como el legacy | mismo resultado; una sesión nueva no hereda nada y la memoria queda acotada | — |
| **DT6a-16** | La resolución del nivel y de la adaptación es el **tamaño de nuestra RT** (los assets de URP con `renderScale` 1), con filtro bilineal cuando se escala | así ni el nivel ni la adaptación cambian un asset (en el Editor, un cambio a un asset en *Play* queda guardado) y la RT escalada no se ve a bloques (QA-G6aT-05, -09) | Técnico (nuevo en el gate) |
| **DT6a-17** | Las estrellas se apagan detrás del disco y del halo **en su propio *shader*** (`CelestialMask`), no con una pasada aparte del astro | mismo resultado que el orden degradé → estrellas → astro de AC-SCN-11-01, sin una cola, un *shader* ni un *draw call* más (QA-G6aT-08) | Técnico (nuevo) |
| **DT6a-18** | El trozo lejano del terreno sin la franja sumergida y con la orilla a +0,4 m; plano lejano en 2 000 m | sin Z invertida (GLES, nivel Bajo) el paso de profundidad a 1,5 km es 0,268 m, sea el plano lejano 2 000 o 4 000 m; 0,4 m de separación lo cubre y no se ve (0,37 dp a 450 m en TV) (QA-G6aT-21) | Técnico (nuevo) |
| **DT6a-19** | De noche las sombras bajan su fuerza a 0 en lugar de apagar el mapa | las palabras clave quedan constantes en la sesión y ninguna hora pide una variante sin calentar; cuesta el mapa de noche en Medio (≈ 1,2 ms de GPU, lo de día) (QA-G6aT-07) | Técnico (nuevo) |

### 8.2 La spec y la base del Designer: lo que se construye (decidido en el gate)

La spec del PO no había integrado `00-deuda-diseno` (se escribieron a la par). El gate decidió los 19 puntos con 04-design-notes §1.1 y la spec corregida; los que cambian algo visible tienen su DV y van al usuario con P-18 (por defecto, se aceptan). Todos son constantes de arte o de perfil: si el usuario rechaza uno, vuelve el valor de la columna "si se rechaza" sin tocar lógica.

| # | Tema | Se construye | Dónde | Si el usuario lo rechaza |
|---|---|---|---|---|
| C-01 | Altura del sol | arco **10,5°** (12° al mediodía; 9 y 15 h 8,92°; 22 h 10,59°) | `CelestialSpec`; DV-52 | 15,5° (17°), con el sol del mediodía detrás de la barra en horizontal |
| C-02 | Luz en el cambio de astro | **0** | `CelestialSpec.LightMinIntensity` | — (constante del Architect y el Designer) |
| C-03 | Estrellas al atardecer | la spec (ambiente ≤ 0,85) con el **fundido de 0,9 s** al cruzar el umbral | `StarField`, `StarFader`; DV-53 | sin fundido |
| C-04 | Posición de las estrellas | la del legacy en su píxel (± 1 dp) | `StarField` | — |
| C-05 | Posición de las nubes | la del legacy en su píxel (el Designer la aceptó; la recomendación anterior de este diseño, la banda en vertical, queda retirada) | `CloudModel` | — |
| C-06 | Alto de las cordilleras | `tan Θ = mín(0,12·alto/focal; tan 9°)`: el legacy en horizontal (6,84°), 9° en vertical | `HorizonSpec`; DV-65 | el 12 % del alto también en vertical |
| C-07 | Nieve | todo lo que pasa el **58 %**, con borde de ± 0,03 y lenguas | `HorizonModel.SnowMask`; DV-64 | la regla del legacy sobre el perfil quieto |
| C-08 | Resolución de la hora real | la spec (minuto) y el paso de minuto en **1 s lineal** (P2) | `SceneHourTransition`; AC-SCN-09-01 | en el acto |
| C-09 | Transición de `Hora escena` | la spec (1,5 s; en el acto con reducir movimiento) | `SceneHourTransition` | — |
| C-10 | Escena demorada | la spec (0,6 s) | `SceneTimelines` | — |
| C-11 | Salida | la spec (+1,5 m y −4 m en 1,2 s) | `ChaseCameraMotion.Exit` | — |
| C-12 | Toma aérea | en la 6a, con **cabeceo de −4°** y la respiración que se apaga | `AerialSpec`; AC-SCN-02-07 | — |
| C-13 | Bote provisorio | el casco solo | `BoatVisual` | — |
| C-14 | Agua: brillo | las 10 líneas del legacy + los agregados, con las líneas × la visibilidad del astro | `DegradedWater.ScreenAlpha`, `_RmShimmer.w`; DV-59 | el alfa del legacy siempre |
| C-15 | Agua: costo | ≤ 24 ALU | DT6a-07 | — |
| C-16 | Juncos | 0,6-1,2 m en `|x| < 18` | DT6a-09 | — |
| C-17 | Rolido | 0,15° | DT6a-01 | — |
| C-18 | Degradés de pantalla | se suman como **capa del HUD**: a la vista con el HUD, fuera de la escena sola | `ScreenOverlay`; DV-67 | sin degradés |
| C-19 | Hemisferio de la luna | espejo en el sur con la región del sistema (sin región, el norte) | `MoonPhase`; DV-57 | disco entero y halo a pleno |

### 8.3 Riesgos

| ID | Riesgo | Mitigación |
|---|---|---|
| R6a-01 | El dibujo a pedido no se comporta igual en el Editor y en el *player*, o asigna | sondas S6a-2 y S6a-3 en B0, con respaldos; las dos cuentas de `WatchScene`; ADR 022 con el resultado |
| R6a-02 | El panel gamma muestrea la RT sRGB con otra conversión | S6a-1; respaldo UNorm con conversión propia |
| R6a-03 | Bit a bit en IL2CPP ARM64: el compilador de C++ puede fusionar `a·b + c` en FMA | IL2CPP emite cada operación en su temporal (sin contracción entre sentencias); NO VERIFICABLE sin el módulo: una prueba de autocontrol en el build de desarrollo (`-rowmate-self-check-scene`) compara los resúmenes de `scene_lake.json` en el equipo (It. 14). Lo trascendente ya se compara con tolerancia (§2) |
| R6a-04 | Determinismo de las tomas 3D | compilación sincrónica, sin `_Time`, tramados fijos, *dithering* de la cámara apagado, sin grano ni TAA; tolerancia justificada si hace falta |
| R6a-05 | La preparación de la escena tironea (la primera vez de los *shaders* o del C#) | preparación sin pasos indivisibles (§2.16): variantes de a una, `Tick` sintéticos, un único dibujo con los *shaders* calientes; palabras clave constantes; la prueba del build: 0 compilaciones de la escena después de `Ready` (§4.5) |
| R6a-06 | Presupuesto de GPU en Android (margen estimado de 0,7 ms con la composición) | la tabla única, los niveles, la adaptación (la resolución primero); la línea de base con Medio en la PC; NO VERIFICABLE sin teléfono (regla 9) |
| R6a-07 | El paquete de Terrain Sample Asset Pack con formatos o Shader Graph que no validan | el slot cae al procedural con un aviso; CI y pruebas sin el pack |
| R6a-08 | `.meta` de `Assets/ThirdParty/` borrados al abrir sin los archivos (RA-17) | `expectedPath` y `SlotValidator`; ASSET_SLOTS lo explica |
| R6a-09 | Las tomas de `it4b`/`it5` cambian por las píldoras `.hud` | regeneración única, dos corridas, QA explica cada diferencia |
| R6a-10 | El arnés tarda más de 30 min | la escena precalentada una vez por corrida; videos a 30 fps; si hace falta, `-Filter` por bloques |
| R6a-11 | Memoria del mundo cargado en Workout (P-14) y de las RT | ≈ 0,6 MB de texturas de contenido y ≈ 8 MB de mallas; las RT de la escena (≈ 23 a 53 MB en 1 080 × 2 400) existen solo con el inmersivo abierto y la precarga; todo se libera con `Application.lowMemory` |
| R6a-12 | Vulkan, D3D12 y Metal crean el estado de la GPU recién en el primer dibujo aunque la variante esté calentada | `GraphicsStateCollection` grabada en el equipo (It. 14); hasta entonces, el dibujo de calentamiento lo paga en reposo. En la PC (D3D11) no aplica. NO VERIFICABLE sin teléfono |
| R6a-13 | El panel no vuelve a muestrear la RT de la escena en un cuadro sin cambios del HUD | S6a-7; respaldo: `MarkDirtyRepaint` de `scene-surface` en cada cuadro dibujado |
| R6a-14 | El modo *Overdraw* del *Rendering Debugger* no se puede usar en *batchmode* | la prueba queda `Inconclusive` con el motivo y QA lo mide con el *Frame Debugger* |
| R6a-15 | Un `SetQualityLevel` o un asset de URP cambiado desde una prueba queda guardado en el proyecto | `TierScope` que restaura y lo afirma; ninguna copia asignada en el Editor (regla de código); la guardia de `ProjectSettings` en los tres *scripts* |
| R6a-16 | `FrameTimingManager` sin tiempos de GPU en algún dispositivo | el respaldo de CPU (hilo principal + *render thread*, sin la espera de presentación); el `[PERF]` dice cuál usó |

### 8.4 Para el PO, QA y el coordinador

- **El gate quedó cerrado** en la spec (DV-63 a DV-67, P-18, las constantes) y en este diseño (§9, "Cambios por gate QA"). Con los valores por defecto, todo se construye igual; si el usuario rechaza una DV, cambia una constante de `Core/Scene/Art` y la expectativa de su prueba.
- **Para el PO** (pendiente): DT6a-06 no está en la spec (AC-WKT-17-07 y VID-09 dicen "fundido cruzado de 200 ms" al rotar o cambiar el tamaño); se construye DT6a-06 y el PO lo registra o lo rechaza.
- **Para el Designer** (alineación de 04-design-notes, lo que toca a este diseño): §6.2 y §4.4 con la fórmula del legacy (6,84° en horizontal); §7.6 con el tramado de 2 a 1 m; §12.3 con los degradés fuera de la escena sola; §14.2 con `Stars` en ≤ 8 ALU (la máscara del astro, DT6a-17) y `_RmDiskShape` como global; §15.1 con la fila de la composición (§4.1 de acá es la tabla única); §5.1 con `WrapFade`; §10.4 con la sonda de la ventana antes del ajuste de la hora (`RenderWithoutPostForTest`); §10.3 con la fuerza 0 de noche en Medio (DT6a-19).
- **QA**: las sondas de B0 (S6a-1 a -7) se leen en "Cambios por implementación"; el `[PERF]` del cierre con el build nuevo (`-rowmate-perf-entries` con la salida y `-rowmate-scene-delay`, `-scene`, `-hours`, `-series`, `-completion`, la línea de base con `-rowmate-quality medium`, y 0 compilaciones de la escena después de `Ready`).
- **Usuario** (gate): D-ART-08 (por defecto, no), el Terrain Sample Asset Pack (opcional), DV-57 (la fase de la luna) y P-18 (las diferencias de arte).

---

## 9. Cambios por notas del Designer (se aplican antes de WP-A)

QA-G6aT-01: por cuarta iteración seguida las notas del Designer llegaron después del diseño técnico. Esta sección las integra: cada pedido de [04-design-notes §20](04-design-notes.md) (DN6a-01 a -24) y de [00-deuda-diseno §14](00-deuda-diseno.md) (DD6a-01 a -26) se adopta o se rechaza con su motivo, y §9.3 deja **un valor por constante**, el mismo de la spec corregida. El cuerpo de este documento ya está al día con esto; esta sección es el registro.

### 9.1 DN6a-01 a DN6a-24

| ID | Pedido | Decisión | Dónde | Prueba |
|---|---|---|---|---|
| DN6a-01 | `HorizonSpec`: 30 % del cielo con tope de 9° | **Adoptado con un cambio**: la fórmula del legacy, `mín(0,12·alto/focal; tan 9°)`, no `0,30·horizonte/focal` (en horizontal daba 7,18° en lugar de 6,84°; QA-6a-PAR-03, spec corregida). En vertical, 9° con las dos | §2.9 | HZN: 9° en V390, TV y P360; 6,843° en H844, TH, L640 y W1280 |
| DN6a-02 | Nieve sobre 0,58, borde 0,03, ruido 0,04 cada 1,7° | Adoptado (DV-64) | §2.9 `SnowMask` | HZN: cobertura 45 % ± 3 en TV y 33 % ± 3 en TH |
| DN6a-03 | Arco 10,5° y tope 0,3°/min | Adoptado (DV-52, AC-SCN-10-05) | §2.6 | CEL |
| DN6a-04 | Luz 0 en el cambio de astro | Adoptado | §2.6 | CEL: 0 a las 19:34:19 y 4:25:41 |
| DN6a-05 | Fundido de 0,9 s de las estrellas | Adoptado (DV-53): `StarFader` con `Snap` al abrir, al volver y con `ForceHourForTest` | §2.8 | STR |
| DN6a-06 | Líneas × la visibilidad del astro | Adoptado (DV-59): el dato no cambia; `ScreenAlpha` en Core y `_RmShimmer.w` en el *shader* | §2.12, §7.5 | DWT por minuto; SIM: 0 píxeles a las 19:34 |
| DN6a-07 | Toma aérea con −4° y la respiración a 0 | Adoptado (AC-SCN-02-07) | §2.2 `AerialSpec` | CCM; CAP-22 (horizonte 336,9) |
| DN6a-08 | Vuelo con focal ×1,25 y `ease-in-out-sine` | Adoptado (AC-SCN-02-05: "sin acelerones ni zoom brusco") | §2.2 `FlightSpec` | CCM: ≤ 17,5 m/s, focal inicial; VID-01 |
| DN6a-09 | El telón del cielo `1 − (1 − q)²` | Adoptado (DV-66), con la curva en Core que el *shader* copia (QA-6a-PAR-10) | §2.8 `SkyBackdrop` | SKB; sondas de color |
| DN6a-10 | El minuto real en 1 s lineal (P2) | Adoptado (AC-SCN-09-01): `HourChange.MinuteTick` | §2.5 | SHR |
| DN6a-11 | Terreno y relleno desde `d − 25 m` | Adoptado | §3.6 | SPL: sin huecos en el primer cuadro del vuelo |
| DN6a-12 | El borde encendido de la cresta (P2) | Adoptado como P2: `HorizonModel.RidgeAt` y `_RmRim`; si no entra en los ≤ 15 ALU de `Horizon`, pasa a la 6b sin tocar nada más | §3.6, §7.5 | toma a las 6 h |
| DN6a-13 | El modelo de luz y su equivalente en URP | Adoptado: el *include* es de D1; `SceneLighting` (B1) pone la luz y el ambiente de URP para lo de terceros | §3.6 | la sonda de la cabaña (± 25 %) |
| DN6a-14 | Envolvente del bosque 0,20 | Adoptado (en `LakeArt`) | §2.13 | SFL: nada fuera de la envolvente |
| DN6a-15 | Las capas del pack en `Texture2DArray` | Adoptado; una capa que no valida es un asset asignado que no valida: el procedural y un `[SLOT]` | §3.7 | SSL con el slot de prueba |
| DN6a-16 | Pasto, helechos y juncos opacos | Adoptado; el sobre-dibujo, con el modo *Overdraw* o revisión de QA (QA-G6aT-11) | §4.1 | SBT |
| DN6a-17 | Bosque cercano hasta 120 m con Poisson de 6 m | Adoptado, **por segmento sin leer vecinos** (QA-G6aT-15) | §2.13 | SFL (orden de los pedidos); SBT (≤ 160 k triángulos, tope de la prueba sobre los ≈ 135 k estimados) |
| DN6a-18 | El atlas de nubes con las manchas del legacy | Adoptado (`ProceduralTextures`, por filas al precargar) | §3.4 | CAP-02 (las 7 en su `x` e `y` ± 1 dp) |
| DN6a-19 | `VeilEv` −0,50 y la sonda del velo | Adoptado (WP-D2 lo ajusta) | §2.15 | CAP-20 |
| DN6a-20 | La entrada de la respiración en 0,6 s | Adoptado (AC-SCN-02-04, -05) | §2.2 | CCM; VID-01 |
| DN6a-21 | El disco en HDR con *bloom* (Alto y Medio), plano en Bajo | Adoptado (DV-63) | §3.6, §7.5 `_RmDiskShape.w` | la sonda del brillo (§5.2) |
| DN6a-22 | La capa lejana de nubes, solo Alto | Adoptado | §4.2 | toma W1280 a las 12 y 18 h |
| DN6a-23 | Los 13 *shaders* con sus nombres, colas y palabras clave | Adoptado, con **`Stars` en ≤ 8 ALU** (la máscara del astro, DT6a-17) y las palabras clave constantes (DT6a-19); los materiales los crea B1 | §7.5 | `SceneMaterialsTests` (los 13, compilados para D3D11, GLCore, Vulkan y GLES 3) |
| DN6a-24 | Tramados con *Bayer* fijo y LOD complementario | Adoptado | §3.6 | VID-03; dos corridas iguales |

### 9.2 DD6a-01 a DD6a-26 (la base)

| ID | Estado | Dónde |
|---|---|---|
| DD6a-01 | Integrado (`Color.lerp` en sRGB con su recorte, los umbrales como constantes), **salvo** la hora continua con segundos: la spec manda el minuto (C-08), con el paso de minuto en 1 s (DN6a-10) | §2.4, §2.5 |
| DD6a-02 | Cambiado por DN6a-03 e integrado (arco 10,5°, 0,3°/min) | §2.6 |
| DD6a-03 | Integrado, con la luz 0 en el cambio (DN6a-04) y *Neutral* | §2.7 |
| DD6a-04 | Integrado; la región por `ISystemLocale` y `ForceRegionForTest` | §2.6 |
| DD6a-05 | Cambiado (C-04, C-05, DN6a-05): las estrellas y las nubes del legacy en su píxel, con el fundido; sale el "envolver a ±60°" | §2.8 |
| DD6a-06 | Integrado (cielo, estrellas, nubes y cordilleras: 5 *draw calls* en la tabla única) | §3.6, §4.1 |
| DD6a-07 | Cambiado (DN6a-01, -02 y la spec): `0,12·alto/focal` con tope de 9°; nieve 0,58 | §2.9 |
| DD6a-08 | Integrado | §2.10, §2.11 |
| DD6a-09 | Integrado | §2.13 |
| DD6a-10 | Integrado **salvo** "la cabaña corrida ≤ 0,5 m": la reemplaza DT6a-08 (aceptado por el Designer) | §2.13 |
| DD6a-11 | Integrado, con el Poisson por segmento | §2.13 |
| DD6a-12 | Integrado, con el LOD complementario de DN6a-24 en lugar del *cross-fade* y el 0 B medido con `WatchScene` | §3.6, §4.4 |
| DD6a-13 | Cambiado (la receta de 04-design-notes §8.1 y DN6a-06) e integrado | §2.12, §3.6, §7.5 |
| DD6a-14 | Integrado con la matriz de Core en lugar del `lensShift` (S6a-6), rolido 0,15° | §2.1, §2.2 |
| DD6a-15 | Cambiado: la salida de la spec y la escena demorada de 0,6 s | §2.2, §3.9 |
| DD6a-16 | Integrado como DT6a-10 (un solo `Volume`), con la sonda en CAP-20 | §3.3 |
| DD6a-17 | Integrado como capa del HUD, fuera de la escena sola (DV-67) | §3.3, §3.6 |
| DD6a-18 | Integrado | §2.18 |
| DD6a-19 | Cambiado: 1,5 s (la spec) | §2.5 |
| DD6a-20 | Cambiado: el casco solo | §3.6 |
| DD6a-21 | Integrado con 04-design-notes §15.2 | §4.2 |
| DD6a-22 | Integrado, con el procedural asignado en cada `.asset` | §2.17, §3.7 |
| DD6a-23 | Integrado | §3.3 |
| DD6a-24 | Integrado | §5 |
| DD6a-25 | Integrado (`RowMateFog.hlsl`) | §3.6, §7.5 |
| DD6a-26 | Cambiado: la bruma baja es P1 (AC-SCN-15-07); la capa lejana y el relieve fino, solo Alto | §4.2 |

### 9.3 Un valor por constante

Los valores viven en `Core/Scene/Art/**` y en `Core/Design/MotionSpecs.cs` (WP-A los crea; WP-D es dueño desde D1). Las pruebas los leen de ahí.

| Constante | Valor | Vive en | AC |
|---|---|---|---|
| Rolido de la respiración | 0,15° (el resto de ANIMATION_BIBLE §5.2 sin el bote) | `BreathingSpec.Lake` | AC-SCN-02-04 |
| Entrada de la respiración | `S(0; 0,6 s)` desde el fin del vuelo o `ShowInPlace` | `ChaseCameraDirector` | AC-SCN-02-04, -05 |
| Respiración en pausa | ×0,6; 300 al pausar y 240 al reanudar, `ease.move` | `MotionSpecs` | AC-SCN-02-04 |
| Vuelo | 220-1 820 ms; P0 (3; 9; −14) → P3 (0; 2,2; 0); posición y orientación `ease-in-out-sine`; focal de 1,25 × la del contrato a 1 con `ease.move`; horizonte de H/2 al del contrato entre 0,6 y 1 | `FlightSpec`, `SceneTimelines` | AC-SCN-02-05 |
| Salida | +1,5 m y −4 m en 1,2 s, `ease.move` | `ChaseCameraMotion.Exit` | AC-SCN-02-06 |
| Toma aérea | +1,70 m, −1,70 m, cabeceo −4°, 6 s, `ease.move`; respiración de 1 a 0 | `AerialSpec` | AC-SCN-02-07 |
| Planos de la cámara | cercano 0,5 m; lejano 2 000 m | `ChaseCameraRig` | AC-SCN-02-03 |
| Astro | acimut `(xFrac − 0,5)·70°`; elevación `1,5° + 10,5°·e`; oculto −2,5°; ≤ 0,3°/min; luz 0 en el cambio; luz del sol `6° + 49°·e`, de la luna `10° + 40°·e` | `CelestialSpec` (`SkyArt`) | AC-SCN-10-04 a -06 |
| Ventanas | sol `S(4,45; 5,50)·(1 − S(18,50; 19,55))`; luna `S(19,5719; 20,6)` y `S(4,4281; 3,40)` | `CelestialSpec` | AC-SCN-10-05 |
| Disco y halo | sol 1,6° de radio; luna 0,8 × (1,28°); halo 4 × el disco de cada astro; alfas 0,7 / 0,25 / 0; halo de la luna × `0,4 + 0,6·iluminación`; HDR `3 + 5·e` (sol) y 1,6 (luna) | `SkyArt` | AC-SCN-11-05, AC-SCN-10-07 |
| Telón del cielo | `1 − (1 − q)²`; bruma de 1,5°; resplandor (0,35; 14°; 20°) | `SkyBackdrop`, `SkyArt` | AC-SCN-11-02 |
| Estrellas | visibles con ambiente ≤ 0,85; fundido 0,9 s `ease.move` | `StarField`, `StarFader` | AC-SCN-11-04 |
| Nubes | `x` del legacy sin la distancia; `WrapFade` con 1,3·s a la derecha y 1,15·s a la izquierda | `CloudModel`, `SkyArt` | AC-SCN-11-06 |
| Cordilleras | `tan Θ = mín(0,12·alto/focal; tan 9°)`; nieve ≥ 0,58 con borde 0,03 y ruido 0,04 cada 1,7°; `D₀`/`Dmín` 3 500/2 500 (lejana) y 1 200/600 (cercana) | `HorizonSpec` (`LakeArt`) | AC-SCN-19-01, -02 |
| Agua | las líneas del legacy × la visibilidad del astro; ≤ 24 ALU con `RM_WATER_EXTRAS` | `DegradedWater`, `WaterArt` | AC-SCN-13-03, -04 |
| Hora | `Hora escena` 1,5 s `ease.move`; el minuto real 1 s lineal; lo demás en el acto | `SceneHourTransition` | AC-SCN-09-01, AC-SCN-23-02 |
| Luces de la noche | ambiente < 0,6; ±10 min; fundido 0,4 s | `NightLights` | AC-SCN-09-06 |
| Velo | 300 / 240 `ease.move`; saturación × `(1 − 0,3·w)`; `VeilEv` −0,50 | `HourLighting`, `HourLookTable` | AC-WKT-13-12 |
| Escena demorada | 0,6 s `ease.enter` | `SceneTimelines` | AC-SCN-01-07 |
| HUD al rotar | 200 `ease.enter`, solo con cambio de clase (DT6a-06) | `SceneTimelines` | AC-WKT-17-07 |
| Aparición y tramados | 400 → 360 m; de 2 a 1 m; LOD complementario en el 10 % del límite | `LakeArt`, `RowMateDither` | AC-SCN-14-07, AC-SCN-02-03 |
| Terreno y relleno | desde `d − 25 m`; el lejano hasta 1,5 km con la orilla a +0,4 m | `LakeArt` | AC-SCN-15-07 |
| Envolventes | izquierda `2,2 + 0,20·|x|` desde 18; derecha `2,2 + 0,06·x` desde 30 | `LakeArt` | AC-SCN-15-07 |
| Juncos y franja del legacy | 0,6-1,2 m; en `|x| < 18`, ≤ 1,2 m | `LakeArt` | P-09 |
| Bosque cercano | `|x|` 18-45, `z` relativo ≤ 120 m, Poisson de 6 m por segmento | `LakeArt` | AC-SCN-15-07 |
| Preparación | tramos de 3 / 2 / 0 ms | `SceneWarmupPolicy` | AC-WKT-13-15 |
| Adaptación | 13 ms de GPU (15 de CPU) sostenidos 3 s; resolución de a 0,05 entre 0,75 y 1, uno cada ≥ 10 s; subir con 30 s bajo 10 ms | `ThermalAdaptation` | AC-SCN-01-13 |
| Niveles | resolución 1,0 / 0,85 / 0,75; MSAA 4× / 2× / FXAA; sombras 80 m (2 cascadas) / 40 m (1) / sin mapa, de noche 0,25 / 0 / —; pasto 60 / 40 / 30 m | `QualityTierProfile` | AC-SCN-01-11 |

---

## Verificación hecha

1. **Legacy, solo lectura**: los archivos de la cabecera y, en el gate, `scene/environment.dart:85-121` (el orden de `visibleProps` y su `sort`) y `scene/painters/sky_painter.dart:36-103` (el disco, el halo, las estrellas y las nubes). `git -C ../rowmate-legacy status --porcelain` vacío al empezar y al terminar; no se leyó `strava_secrets.dart`.
2. **El SDK de Flutter del oráculo**: `<flutter-sdk>/bin/cache/pkg/sky_engine/lib/ui/painting.dart:424-455` (`Color.lerp` con `clampDouble(_lerpDouble(…); 0; 1)` por canal) y `lerp.dart:20-31` (QA-6a-PAR-06).
3. **Los números de §8.1** salen del programa de .NET 8 en `<scratch>/it6a/arch/calc` (barrido de la hora minuto a minuto con las ventanas del Designer y los dos arcos, y de la respiración 10 min a 200 muestras por segundo en los siete tamaños). En el gate, una cuenta aparte (sin archivos en el repo) para el alto de las cordilleras por perfil con la fórmula del legacy (6,843° en horizontal, 9° en vertical; 28,6 % y 17,8-18,3 % del cielo), el paso de profundidad sin Z invertida (0,024 m a 450 m y 0,268 m a 1 500 m, igual con el plano lejano en 2 000 o 4 000 m), el escalón de 0,4 m a 450 m (0,37 dp en TV), lo que asoma la nube más grande en la vuelta (13,0 dp en V390, 10,9 en TV y 16,0 en P360; nada en horizontal) y la curva del telón (0,4375 en `q` 0,25). Los demás números del Designer se toman de 04-design-notes y 00-deuda-diseno y los vuelve a fijar el oráculo en WP-A.
4. **Código leído en el gate**: `Tests/UiSupport/FrameAllocations.cs`, `Presentation/Workout/HudWarmup.cs`, `Core/AssemblyInfo.cs`, `Scene/AssemblyInfo.cs`, `Editor/Bootstrap/UrpProvisioner.cs`, `Platform/DeveloperOverrides.cs` (el patrón `#if UNITY_EDITOR`), `Simulator/RowingSimulator.cs:105-112` (R-T4), `.github/workflows/{ci-core,ci-unity}.yml`, la lista de `Tests/EditMode/Project/` y de `Tests/UiSupport/`, los `asmdef` de `Snapshots` y `UiSupport`.
5. **El repositorio**: solo se escribió este documento. Unity no se abrió; no hay código de la 6a. Rutas relativas, el legacy por su URL o `../rowmate-legacy`, `<scratch>` y `<flutter-sdk>`. Sin *commits*.

---

## Cambios por gate QA

Gate de armonía: [03-qa-harmony-paridad](03-qa-harmony-paridad.md) y [03-qa-harmony-tecnica](03-qa-harmony-tecnica.md) (los dos, APROBADO CON OBSERVACIONES). Fecha: 2026-11-03. Se resolvieron los **21 issues del Architect** (7 mayores y 14 menores). Ninguno se disputa entero; en tres se eligió una de las opciones de QA o una tercera, y en uno (QA-G6aT-21) la corrección que proponía QA en primer lugar no alcanza y se explica por qué. También se alineó el diseño con la spec corregida por el PO ("Cambios por gate QA" de [01-po-spec](01-po-spec.md), punto 6) y con las partes de los issues de otros dueños que tocan a este documento.

### Issues del Architect

| Issue | Sev. | Resolución | Dónde |
|---|---|---|---|
| **QA-G6aT-01** | mayor | §9 integra DN6a-01 a -24 y DD6a-01 a -26 (adoptado o rechazado, con motivo) y deja un valor por constante; el cuerpo quedó al día (0,3°/min, nieve 0,58, toma aérea con −4°, vuelo con `ease-in-out-sine` y ×1,25, la rampa de 0,6 s, el minuto real, el fundido de las estrellas, las líneas × `vis`); §7.5 con todas las propiedades y palabras clave de 04-design-notes §14.2, las de instancia con `UNITY_INSTANCING_BUFFER`, y `SceneShaderIds` con el contrato entero en B0; las sondas al día (CAP-22 con el horizonte en 336,9 y la popa; VID-01 con la rampa); C-05 corregida (la spec) | §2.2, §2.5, §2.6, §2.8, §2.9, §2.12, §5.2, §7.5, §8.2, §9 |
| **QA-G6aT-03** | mayor | La preparación ya no tiene pasos indivisibles: las variantes de a una (`WarmUpProgressively(1)`; en el Editor, la compilación asíncrona y `Loading` mientras compila), los `Tick` sintéticos y un único dibujo de calentamiento que con los *shaders* calientes cuesta un cuadro de escena; la escena demorada se prepara en tramos de 2 ms también después de la cascada (DT6a-11 reemplazado). `SceneHost.DelayReadinessForTest`; SWP, SLC (el peor cuadro con el marcador `RowMate.Scene.Warmup`) y `[PERF]` con `-rowmate-scene-delay` | §2.16, §3.1, §3.4, §3.9, §4.5, §6.3, §8.1 |
| **QA-G6aT-04** | mayor | El procedural es un valor asignado (fuente `Procedural` en cada `.asset` de `Slots/`) y no avisa; `[SLOT]` solo para un asset asignado que falta (también una referencia a `Assets/ThirdParty/` sin el archivo) o no valida; el `SlotLog` es del anfitrión, no estático (las pruebas no dependen del orden); SSL "todos procedurales → 0 avisos" | §3.7, §6.3 |
| **QA-G6aT-05** | mayor | En el Editor nunca se asigna una copia a la configuración ni se cambia un asset de URP: la resolución es el tamaño de nuestra RT (DT6a-16), lo demás por cámara, `Volume` y materiales (`SceneQualityApplier`); lo que es del asset, solo en el *player* (`PlayerUrpAssetQuality`). `TierScope` restaura el nivel y el fixture lo afirma; `ProjectSettingsGuard.ps1` en los tres *scripts*; `SceneAssetsTests` afirma cada nivel con su asset del proyecto; reglas de código (`renderPipeline`, `asyncShaderCompilation`) | §3.2, §4.2, §6.3, §7.0, §7.2 |
| **QA-G6aT-06** | mayor | `SceneHost.TickObserver` y `FrameAllocations.WatchScene`: el `Tick` (cuenta "escena") y el pedido de render (cuenta "URP") adentro de lo medido, con el control positivo (`byte[64]` en el `Tick`, 120 de 120) en la misma corrida; con el respaldo de S6a-3, `beginCameraRendering`/`endCameraRendering`; si URP asigna solo en el Editor, esa cuenta se mide en el build de desarrollo | §3.2 (S6a-3), §4.4, §4.5 |
| **QA-G6aT-07** | mayor | Los `Tick` sintéticos (12 h, 22 h con el velo, vuelo, salida, toma aérea, `E-920` con el relleno) para la primera vez del C#; el dibujo de calentamiento con `WarmUpAll`; palabras clave constantes (DT6a-19: fuerza de sombra 0 de noche); la colección la genera `SceneMaterialsBuilder` y `SceneMaterialsTests` la afirma; `[PERF]` con `logWhenShaderIsCompiled`: 0 compilaciones de la escena después de `Ready`, desde la primera entrada del proceso | §2.7, §3.4, §4.5, §7.5 |
| **QA-G6aT-08** | menor | Opción de la máscara (DT6a-17): `Stars` multiplica su alfa por `CelestialMask.StarAttenuation` (0 en el disco, `1 − alfa del halo` en el halo), ≤ 8 ALU; `_RmDiskShape` global; CMK (dotnet) y SLC (el orden de colas y la sonda con las estrellas prendidas y apagadas). Se prefirió a una pasada propia del astro porque da lo mismo sin una cola, un *shader* ni un *draw call* más | §2.8, §3.6, §7.5, §6.3 |
| **QA-G6aT-09** | menor | S6a-7 (el número de cuadro en la RT de la escena leído en la RT de la UI en el mismo cuadro, también sin cambios del HUD; respaldo `MarkDirtyRepaint`); filtro bilineal cuando la RT se escala; SPR con `WorldToViewportPoint` × el tamaño de la RT; `SceneCompositionTests` como regresión | §3.2, §6.3 |
| **QA-G6aT-10** | menor | `ThermalAdaptation` mide solo con la escena dibujándose y con el tiempo de trabajo; suspendida con `renderFrameInterval` 2 y fuera del inmersivo; THA y SAD con 33 ms de intervalo y 8 ms de trabajo que no dispara; el *bootstrapper* prende `enableFrameTimingStats` y `SceneAssetsTests` lo afirma | §2.16, §3.9, §4.2, §6.3 |
| **QA-G6aT-11** | menor | Una sola tabla (la del Designer con la fila de la composición y la memoria de las RT: ≈ 87 *draw calls*, ≈ 135 k triángulos, ≈ 11,3 ms, RT ≈ 23-53 MB); la línea de base de GPU con `-rowmate-quality medium`; el sobre-dibujo con el modo *Overdraw* del *Rendering Debugger* o revisión de QA | §4.1, §4.5 |
| **QA-G6aT-12** | menor | §5.1: *dithering* de la cámara apagado, sin grano ni TAA, el bandeado de la noche solo con *Bayer*; `allowAsyncCompilation = false` también con `UiTestHost` en `SceneMode.Real`; `HideForCapture` oculta los hermanos de `scene-surface` (y el arnés apaga los degradés); la afirmación de la cámara va en SLC (`CameraIsDeterministic`) y no en `SceneAssetsTests` porque la cámara y el perfil del `Volume` se arman por código en ejecución (DT6a-04), no son assets | §3.3, §5.1, §6.3 |
| **QA-G6aT-14** | menor | Los materiales y la colección pasan a B1 en un archivo propio (`SceneMaterialsBuilder`), con su prueba (`SceneMaterialsTests`); B0 no depende de los *shaders* de D1. Los `ProjectSettings` que escribe el *bootstrapper* son G de B0. `Core/AssemblyInfo.cs` (`InternalsVisibleTo("RowMate.Scene")`) en WP-A, con la regla de código de que solo los controles `*ForTest` de `SceneHost` los llaman | §1.1, §7.2, §7.6, §6.3 |
| **QA-G6aT-15** | menor | (a) `CapEnvironment` con `z` distintas en el corte (la prueba lo afirma) y las listas comparadas con los tramos de `z` iguales ordenados en los dos lados; (b) el Poisson del bosque por segmento con margen interno, sin leer vecinos; SFL: `k + 1` antes que `k` y orden aleatorio dan lo mismo | §2.11, §2.13, §6.2, §6.3 |
| **QA-G6aT-16** | menor | `SceneSurface` solo muestra `Output`, nunca llama `Preload` ni `Open`, y se suscribe y desuscribe con el panel; `HudWarmupSceneTests` con `FakeImmersiveScene` (0 suscriptores, 0 llamadas). `HudWarmupTests` no existía como archivo: es uno nuevo | §3.3, §6.3, §1.1 |
| **QA-G6aT-17** | menor | `FrameTimeProbe` con la ventana de 0 a 1 s desde la confirmación de `Terminar`; el dibujo de calentamiento es único, fuera de pantalla y antes de `Ready`; DEMO-01 cuenta después de `Ready` | §3.4, §4.3, §4.5, §5.2 |
| **QA-G6aT-18** | menor | Categorías `GPU` (con `Assume` sobre el dispositivo gráfico) y `Perf` (sin topes de tiempo en el CI); los *shaders* compilan también para GLCore y `SceneMaterialsTests` compila sin GPU; `RepoHygieneTests`: ningún asset versionado referencia un GUID de `Assets/ThirdParty/` salvo los de `Slots/` | §3.7, §6.3, §7.5 |
| **QA-G6aT-19** | menor | `THIRD_PARTY_NOTICES.md` con la licencia BSD-3 del SDK de Dart (WP-A); `NoticesPostprocessor` lo copia junto al ejecutable y `BuildOutputInspector` lo exige (B2); `RepoHygieneTests` lo exige si existe `DartRandom.cs` | §1.1, §2.10, §7.1, §7.2 |
| **QA-G6aT-21** | menor | **En parte distinto de lo propuesto**: bajar el plano lejano casi no cambia el paso de profundidad sin Z invertida (lo manda el cercano, que es contrato: 0,268 m a 1,5 km con el lejano en 2 000 o en 4 000 m). Se adopta igual el plano en 2 000 m, y el remedio es el *offset* de geometría que QA daba como alternativa: el trozo lejano sin la franja sumergida y con la orilla a +0,4 m (DT6a-18). La toma `it6a-low`; la parte de GLES, NO VERIFICABLE hasta el teléfono | §2.1, §2.14, §3.5, §5.1, §8.1 |
| **QA-6a-PAR-05** | menor | Bit a bit solo la aritmética pura; `Within(1e-12)` los valores del orden de 1 que pasan por funciones trascendentes y `Within(1e-9)` los que van en dp, grados u horas; los booleanos de la noche, exactos con el margen afirmado; dicho en §2, §2.4, §6.2 y en `SceneGoldenTests` | §2, §2.4, §6.2, §6.3 |
| **QA-6a-PAR-06** | menor | `SceneColor.Lerp` con el `clampDouble(…; 0; 1)` de `Color.lerp` (verificado en el SDK); STD lo prueba con `t` fuera de [0; 1] | §2.4 |
| **QA-6a-PAR-10** | menor | `SkyBackdrop.Gradient` en Core (el *shader* la copia) con SKB (extremos exactos y monotonía) y `SkyArtTests` (halo = 4 × disco; luna = 0,8 × sol); la matriz de AC-SCN-11-02 y -05 pasa a "d cap qa" | §2.6, §2.8, §6.3, §6.5 |

### Alineación con la spec corregida y con issues de otros dueños

| Origen | Qué se cambió acá |
|---|---|
| Spec, "Cambios por gate QA" punto 6 | `CelestialSpec` (10,5°; luz 0; 0,3°/min), `BreathingSpec` (0,15°), `HorizonSpec` (`0,12·alto/focal` con 9°; nieve 0,58), el fundido de `StarField`, STR con 6:12 / 6:13 / 17:59 / 18:00, §3.6 con el tramado de 2 a 1 m, `HideForCapture` y los degradés fuera de la escena sola, la región de prueba, `It06aIds` con 101 ID, DEMO-01 después de `Ready`, la regla de `[SLOT]`, §8.2 cerrada (C-05 en la spec) |
| QA-6a-PAR-02 (Designer) | la escena sola sin los degradés (`SetScrimsForTest`), las sondas de §10.4 medidas así |
| QA-6a-PAR-04 y QA-G6aT-20 c (PO) | el tramado cercano de 2 a 1 m sobre la lista del legacy `[d + 1; d + 400]`, sin banda propia |
| QA-6a-PAR-08 (Designer) | `CloudModel.WrapFade` en Core (con la `x` del legacy) y `_RmCloudAlpha` |
| QA-6a-PAR-09 (PO) | STR con 6:12 sí, 6:13 no, 17:59 no, 18:00 sí, con la hora `hora + minuto/60` |
| QA-G6aT-13 (Designer) | `SceneHost.RenderWithoutPostForTest` para medir la ventana antes del ajuste de la hora (CAP-04) |
| QA-G6aT-20 (PO) | 101 ID; el dibujo de calentamiento y DEMO-01; el 0,504 en TV y P360 (ya estaba en §2.1) |
| AC-SCN-09-01 (mejora P2) y AC-WKT-13-15 (corregidos) | `HourChange.MinuteTick`; la escena demorada por tramos también después de la cascada; la salida de 0 a 1 s |

**Queda para otros**: el PO registra o rechaza DT6a-06 en AC-WKT-17-07 y VID-09 (§8.4); el Designer alinea 04-design-notes con lo que lista §8.4.

---

## Cambios por implementación de WP-A

WP-A quedó implementado con los archivos de §7.1: `Core/Scene/**` (con `Art/` y `Geometry/`), `Core/Design/{SceneTimelines,HudContrast}.cs`, el bloque de la 6a de `MotionSpecs`, `Core/AssemblyInfo.cs`, el comentario de `App/Scene/SceneSettings.cs`, `THIRD_PARTY_NOTICES.md`, `Tests/EditMode/Scene/**` (427 casos), `Tests/Support/SceneSeeds.cs`, `It06aIds.cs`, las reglas de `Tests/EditMode/Project/` y `Traceability/`, el oráculo (`-Scene`, los seis `scene_*.json` y LEG-01 a -07 con las tres "con marcas"). Precisiones, sin cambiar el QUÉ:

| # | Qué | Por qué |
|---|---|---|
| A6a-01 | **Tipos agrupados por archivo** (los nombres y firmas de §2 no cambian): `SkyBackdrop` y `CelestialMask` en `CloudModel.cs`; `ShimmerLine` y `DegradedWater` en `ShoreShading.cs`; `SceneHourTransition` y `HourChange` en `SceneHour.cs`; `StarFader` en `StarField.cs`; `NightLightFader` en `NightLights.cs`; `BreathingSpec`, `FlightSpec`, `AerialSpec`, `CameraMode` y `ChaseCameraDirector` en `ChaseCameraMotion.cs`; `CameraPose`, `SceneMatrix4`, `SceneQuaternion`, `SceneMath` (recortes, `smoothstep`, `lerp`, ángulos y direcciones) y `SceneHash` (el ruido de valor determinista del terreno, la orilla que serpentea, el relleno, las lenguas de nieve y el minuto de cada luz, sin tocar el generador del legacy) en archivos propios; el FNV-1a de 64 bits de los segmentos (el mismo cálculo que el oráculo) vive en el lector de pruebas `Tests/EditMode/Scene/SceneGolden.cs`; `QualityTier` con `IRenderAssetQuality` (lo usa `ThermalAdaptation`). En `Geometry/`: `TerrainChunkMesh`, `HorizonStripMesh` y `SmallMeshes.{Rock,ReedTuft,GrassTuft}` en `GroundMeshes.cs`; `CabinMesh`, `PierMesh` y `HullMesh` en `PropMeshes.cs` (con `CabinLayout` y `PierLayout` de `PropPlacement`). Las pruebas: THA, SWP, SLT, HCT y las líneas de tiempo en `ScenePolicyTests.cs`; TRN, PPL y SFL en `ShoreGroundTests.cs`; SSH y DWT en `ShoreShadingTests.cs`; SKA, SKB y CMK en `SkyContractTests.cs`; el lector de los vectores en `SceneGolden.cs` | Menos archivos chicos con la misma superficie pública; las clases de prueba conservan su prefijo |
| A6a-02 | **`It06aIds`: diez ID suman "d"** respecto de §6.5: AC-APP-05-08, AC-SCN-01-04, -01-07, -01-08, -01-12, -14-07, AC-WKT-13-06, -13-08, -17-07 y AC-BLE-15-09 (AC-WKT-13-14, "p (d)", quedó "d p") | Sus reglas puras (reducir movimiento, la hora al volver, el fundido de la escena demorada, el casco en la zona, 0 B en `Tick`, la aparición, las líneas de tiempo, la última lectura) tienen prueba de dotnet además de la de Unity, que sigue en su lugar |
| A6a-03 | **`It06aIds.DeviceOnlyIds`** (AC-SCN-01-09 y -01-10, solo "eq") va aparte de `UnityOnlyIds`: `TraceabilityTests.It06aIdsAreComplete` los separa antes de clasificar | No tienen prueba automática posible. **Hasta que WP-B2 sume sus `[Ignore]` en `WorkoutDeviceChecksTests`**, la matriz de dotnet los muestra como SIN TEST, igual que los nueve LT de PlayMode (LT-78 a -81, -83 a -87) hasta WP-B1 (el precedente son los LT `ps` de la It. 2) |
| A6a-04 | **`TraceabilityTests`, bloque de la 6a**: `It06aIdsAreComplete` (101 = 78 nuevos por ficha + 23, LT-62 a LT-87, 24 solo de Unity, 2 de equipo), `It06aDotnetIdsHaveTests`, `It06aLegacyCasesKeepTheirNames` (los 17 de dotnet), `It06aUnityOnlyIdsHaveAUnityLocation` (ignorada hasta la primera prueba de Unity de un AC-SCN nuevo; desde ahí exige también los LT de PlayMode con su nombre y su cita) y `It06aUnityOnlyIdsHaveNoDotnetResult`. `It05UnityOnlyIdsHaveNoDotnetResult` cuenta los d/s de la 6a como re-verificados (AC-WKT-13-15, -13-08, -13-12, -17-07), como ya hacían las de la 4a y la 4b | La convención de la 5 (A5-11) |
| A6a-05 | **`PureCodeRulesTests`** suma: `NoSystemClockInTheSceneCore` (Core/Scene, `SceneTimelines`, `HudContrast`), `TheSceneHasNoUserInput` (Core/Scene y Scene/: ni `Input.`, Input System, `Pointer*`, `Keyboard`, `Mouse`, `Touch`), `TheSceneDistanceStaysInTheScene` (fuera de Core/Scene y Scene/, ningún código de producción nombra `SceneState`, `SceneSession`, `SceneFrame` ni `ShaderDistance`; Snapshots, Editor y pruebas quedan afuera), `OnlyTheProvisionerAssignsTheRenderPipeline` (y nadie usa `EditorSettings.asyncShaderCompilation`), `SceneCallsCoreTestControlsOnlyFromItsOwnTestControls` con su control positivo (`TheTestControlRuleFindsAViolation`) y `NoPreferenceKeyNamesTheSceneHour` (un literal sin espacios con la hora de la escena; el rótulo `Hora escena` del panel no cuenta) | §6.3. **La regla de `scene.environment` (AC-SCN-21-01, "p qa") queda en SLC (WP-B1)** y la de las preferencias va con AC-BLE-16-11 y AC-BLE-16-07, no con AC-SCN-23-03 ("ps eq"): una categoría de dotnet en un ID solo de Unity rompe `It06aUnityOnlyIdsHaveNoDotnetResult` |
| A6a-06 | **`RepoHygieneTests`** suma `ThirdPartyPackIsIgnoredExceptMeta` (`git check-ignore` del pack y de sus `.meta`), `NoVersionedAssetReferencesThirdPartyOutsideTheSlots` (los GUID de los `.meta` de `Assets/ThirdParty/`, versionados o por versionar, contra los assets de texto de Unity; se admiten solo los `.asset` de **`Assets/RowMate/Slots/`**) con su control positivo, y `TheDartPortCarriesItsLicense`. `MachinePath` pasa a `internal` (lo usa I6T) | **Para WP-B0**: los `.asset` de los slots van en `Assets/RowMate/Slots/` (la ruta que admite la regla) |
| A6a-07 | **`ItSixAToolingTests`** (nuevo): el oráculo `-Scene` adentro de la guardia (el orden en el *script*), sin el módulo de credenciales y solo con `features/workout/scene/` del legacy, documentado; ninguna ruta de la PC en `snapshots/**` (también en los bytes de los PNG), en los `scene_*.json` ni en el oráculo; `ROWMATE_DATA_DIR` en las corridas de Unity (AC-HIS-20-04). Lo de WP-B queda **ignorado con el motivo mientras su archivo no existe**: la guardia de `ProjectSettings` (desde `tools/unity/ProjectSettingsGuard.ps1`; `build.ps1` desde `NoticesPostprocessor.cs`, `capture-snapshots.ps1` desde `SceneCatalog.cs`), el conjunto `it6a` por defecto (`[string]$Set = 'it6a',` y `return 'docs/migration/iterations/06a-escena-lago/snapshots'`) y los argumentos de §4.5 (`-rowmate-perf-scene`, `-rowmate-scene-delay`, `-rowmate-quality`, `-rowmate-perf-hours`, `-rowmate-perf-completion`) adentro de `#if UNITY_EDITOR \|\| DEVELOPMENT_BUILD` en `AppRoot` y nombrados en `BuildOutputInspector`, que además nombra `THIRD_PARTY_NOTICES.md` | Un archivo de WP-A no puede afirmar lo que todavía no existe; así B0, B2 y B3 encuentran su prueba escrita |
| A6a-08 | **`GoldenCoverageTests.SceneFilesPresentWithHeader`**: los seis `scene_*.json` con el generador `tools/oracle/engine/test/scene_vectors_test.dart`, el mismo commit del legacy que `fmt.json`, todas las secciones en tabla y ≤ 1 MiB (el mayor, `scene_palette.json`, 670 586 bytes) | §6.2 |
| A6a-09 | `tools/oracle/README.md` documenta `-Scene` (las dos pruebas, las secciones de cada archivo y el comparador de cada valor) | Fuera de la lista de §7.1: documentación |

**Corridas** (desde la raíz del repo; `<scratch>/it6a/wp-a` y `<scratch>/it6a/unity`):

| Suite | Resultado |
|---|---|
| `tools/dotnet/run-tests.ps1 -Matrix` | compilación con 0 avisos y 0 errores (también `RowMate.Tests.UnityApiCheck`); simulador 192 / 192; Core 3003 / 2965 pasan / 0 fallan / 38 ignoradas (las de antes, `It06aUnityOnlyIdsHaveAUnityLocation` y las tres de WP-B de I6T); los 75 ID "d" de la 6a, PASA; matriz: PASA 615, FALLA 0, IGNORADO 2, PENDIENTE UNITY 134, PENDIENTE WP-B 0, SIN TEST 11 (A6a-03) |
| `tools/oracle/run-oracle.ps1 -Engine -Scene` | las dos pruebas de Flutter en verde; los seis `scene_*.json`, `workout_engine.json` y los diez PNG **iguales byte a byte** a la corrida anterior salvo `generatedAt` (se dejaron los de antes); la guardia del legacy OK antes y después |
| `tools/unity/run-unity-tests.ps1` (Unity 6000.3.27f1, *batchmode*) | compilación limpia (los únicos avisos son los de antes, de `Infrastructure` y dos pruebas viejas; el `#error` de `SimulatorReleaseGuard` es el de su prueba); **EditMode**: total 3062, pasan 3018, fallan 0, ignorados 44, inconclusos 0; **EditMode `-Simulator`**: total 3254, pasan 3210, fallan 0, ignorados 44, inconclusos 0; **PlayMode**: total 427, pasan 427, fallan 0; **PlayMode `-Simulator`**: total 496, pasan 496, fallan 0. Las 427 pruebas de `Tests/EditMode/Scene` corren también en Unity (AC-SCN-01-14); las dos de asignaciones de .NET 8 (`TickDoesNotAllocate`, `VisiblePropsDoesNotAllocateAfterWarmup`) quedan ignoradas en Mono, como las de la 4a. El primer *import* creó los `.meta` de los archivos nuevos |
| Mutación de control | con la constante `A` del MWC de `DartRandom` cambiada (`0xFFFFDA61` → `0xFFFFDA63`) fallan 12 de las 427 de la escena (DRN, SHS, STR, LAK y SSN: los vectores dorados, los segmentos y su FNV-1a, las listas visibles, el tope, las estrellas, el cuadro de la sesión); restaurado byte a byte |

`git -C ../rowmate-legacy status --porcelain` vacío al empezar y al terminar; `lib/core/strava/strava_secrets.dart` no se leyó. Ni las pruebas ni las corridas de Unity cambiaron `ProjectSettings/` ni `Assets/RowMate/Settings/`. Sin *commits*.

## Cambios por implementación de WP-B

WP-B quedó implementado en el orden B0 → B1 → B2 → B3 con los archivos de §1.1 (Scene, Presentation, Bootstrap, Platform, Editor, arnés, pruebas y herramientas) y ADR 022. Las sondas S6a-1 a S6a-7 pasaron sin usar ningún respaldo (ADR 022, "Resultado de las sondas"). Precisiones, sin cambiar el QUÉ:

| # | Qué | Por qué |
|---|---|---|
| B6a-01 | **Tipos agrupados por archivo** (nombres y responsabilidades de §1.1 y §3.1): `WaterSurface`, `BoatVisual` y `ScreenOverlay` en `Scene/HorizonRenderer.cs`; `SceneObjects` en `SkyRenderer.cs`; `SegmentData` y `SegmentStage` en `SegmentStore.cs`; `ShoreMeshes` en `ShoreStreamer.cs`; `InstanceBatchSet` en `InstanceBatch.cs`; `SceneWorld` en `SceneWorldBuilder.cs`; `SceneReadiness` y `SceneHostOptions` en `SceneHost.cs`; `ProceduralTexture` en `ProceduralTextures.cs`; `SlotResolver` y `SlotLog` en `Slots/SlotResolution.cs`; `SceneMaterialSet` en `Slots/SceneWorldDefinition.cs`; `SceneQualityApplier` en `Quality/`. Las pruebas PlayMode de B1 y B2 en `Tests/PlayMode/Scene/{SceneLifecycle,SceneComposition,SceneProjection,SceneSlots,SceneBudget,SceneImmersive}Tests.cs` con `SceneTestBase` (SPR, SPL y SLT en `SceneProjectionTests.cs`; SBT, el 0 B y la adaptación en `SceneBudgetTests.cs`) | Menos archivos chicos con la misma superficie |
| B6a-02 | **`IImmersiveScene` suma `Drawn` e `IsShowing`** | `SceneSurface` repinta en cada cuadro dibujado (S6a-7: sin eso el panel podía mostrar la RT anterior en los cuadros sin cambios del HUD) e `ImmersiveWorkoutPage` decide el fundido tardío con lo que se ve |
| B6a-03 | **`MeshUpload` orienta los triángulos** con las normales de Core (`orient`) y duplica al revés los de una sola cara (`doubleSided`: juncos y pasto) | Las mallas puras salen con la vuelta mezclada (en el casco coincidían 552 de 644 triángulos; juncos y pasto de una cara): el casco no se veía con el *culling* de atrás |
| B6a-04 | **El presupuesto de dibujo** (§4.1; ARCHITECTURE §20.1): de 207 / 180 (*draw calls* / SetPass) en Alto en el primer armado a **91 / 61** (Alto), **71 / 46** (Medio) y **58 / 33** (Bajo), con 216 k / 156 k / 107 k triángulos; la UI sola, 2 dibujos y 1 SetPass. Cómo: `InstanceBatch` de 250 instancias con dos vectores y dos números por instancia y su propio `MaterialPropertyBlock`; una sola forma para rocas, juncos y bosque lejano; el pasto en tiras de 5 m con 6 *buffers* de 12 800; sombras solo en el LOD 0 cercano (`ShadowLods`: 2 en Alto, 1 en Medio, 0 en Bajo) y rocas sin sombra; cabañas, ventanas y muelles en lotes de uno por segmento. **El tope de SetPass de Alto queda en 80** (el permiso de §20.1 de ampliarlo en la 6a con medición; `SceneBudgetTests`); Medio y Bajo, 50 | El Designer confirma el 80 en WP-D2 |
| B6a-05 | **Bloom** sin filtrado de alta calidad, a media resolución y con 2 iteraciones | El costo de post de §20.1 (≤ 1,5 ms) en Medio |
| B6a-06 | **El pasto se ralea por la distancia plana** (`√(x² + relZ²)`), no solo por `z`; el que queda detrás de la cámara se dibuja solo con la cámara detrás del origen (`z < −1`: la salida y la toma aérea) | Los triángulos del pasto a los costados contaban como cercanos |
| B6a-07 | **La calidad del asset de URP en ejecución** (`PlayerUrpAssetQuality`): el nivel con `QualitySettings.SetQualityLevel` y la distancia de sombras en el asset activo **solo en el *player***, sin copias del asset; en el Editor, `EditorRenderAssetQuality` no toca nada | `OnlyTheProvisionerAssignsTheRenderPipeline` (A6a-05) y la guardia de `ProjectSettings` (QA-G6aT-05) |
| B6a-08 | **Bootstrapper v4**: `urp.tiers` (`UrpProvisioner`: los tres assets con un `RowMate_Renderer` compartido; los niveles Low/Medium/High conservan los ajustes generales de Medium, High y Ultra de Unity; Android, iOS y tvOS en Medio y el resto en Alto; la capa `RowMateScene`; Frame Timing Stats; la intensidad lineal y la temperatura de color de las luces; borra `RowMate_URP_Default.asset` de la It. 1), `scene.assets` (`SceneAssetsBuilder`) y `scene.materials` (`SceneMaterialsBuilder`: 15 materiales, con `PropWindow`, y la colección de variantes) | El constructor de `UniversalRenderPipeline` fija la intensidad lineal y la temperatura de color en cada arranque: escritas por el *bootstrapper*, la primera corrida que crea el pipeline ya no deja `GraphicsSettings.asset` cambiado (la guardia lo detectó en una corrida del arnés) |
| B6a-09 | La cámara de la UI (`BootSceneBuilder`) sin MSAA ni HDR | No dibuja nada 3D; ahorra la intermedia |
| B6a-10 | **Los `Tick` sintéticos del armado** usan las entradas de la sesión (`SceneTickInputs.ForcedHour`, el velo) y `ShoreSegments.VisibleProps(920)`, sin los controles `*ForTest` de Core | `SceneCallsCoreTestControlsOnlyFromItsOwnTestControls` (A6a-05) |
| B6a-11 | **La orilla por *streaming*** (`SegmentStore`): 16 segmentos en un *pool*, la ventana de `d − 25` a `d + 450` y el trozo lejano hasta 1,5 km que se rehace cuando la ventana avanza; pasadas por segmento (objetos, terreno, relleno, pasto en tiras de 5 m) con un tramo de 1 ms por cuadro; en el acto en un salto (> 50 m), en el arnés y en las pruebas | §3.4, sin una unidad indivisible |
| B6a-12 | **Los *prefabs* de vegetación del pack no se leen todavía**: de los slots con fuente Asset, solo las capas del terreno (`Texture2DArray`, DN6a-15) usan el asset de terceros; los demás resuelven y validan pero dibujan el procedural | La vegetación con *prefabs* es de la It. 8 (ADR 015) |
| B6a-13 | **El sobre-dibujo** (≤ 2,5× en Medio, §20.1) no tiene prueba automática | URP no tiene un modo de *overdraw* por línea de comandos; queda para la medición en el teléfono (It. 14) |
| B6a-14 | **La RT de la escena es de media precisión (`R16G16B16A16_SFloat`) en los niveles con HDR** (Alto y Medio) y `R8G8B8A8_SRGB` en Bajo (ADR 022, punto 3). `RtReader.Read` pasa una RT de media precisión a bytes sRGB y la marca de S6a-7 se escribe en el formato de la RT | Las primeras capturas mostraron el sol de mediodía gris (`#BEBEBE`): URP arma la intermedia con el formato de la RT de destino y con una de 8 bits no había HDR, así el disco de `3 + 5e` (DN6a-21) se recortaba en 1 antes del *tonemapping*. Con el cambio el disco sale blanco y las 38 pruebas de `Tests/PlayMode/Scene` siguen en verde (S6a-1 incluida) |
| B6a-15 | **El arnés** (§5): `SceneCatalog` + `SceneMarks`; `SnapshotScenario.Inspect` (las sondas que miran la imagen antes de guardarla y las marcas), `SceneMayBeLoading` (CAP-24) y `MotionMayRun`; `SnapshotTakeRule.SceneBlockers` (la escena lista y la orilla armada); `VideoSpec.BeforeFrame` y el lienzo de VID-09 (`VideoRecorder` centra cada cuadro en 2 400 × 2 400 px con bandas negras: 1 200 × 1 200 en el MP4); `ScenarioScript.StartFree(false)`; la compilación de *shaders* sincrónica durante toda la corrida (`SnapshotCli`); el tope del arnés pasa a 45 min | §5.1 |
| B6a-16 | **Los instantes de algunas tomas**: la escena sola se arma a los 2,2 s de la sesión (a los 2,5 llega una lectura de R-4 y la regla de la 4b no toma con los rodillos arrancando); DEMO-02 a los 2,2 s, DEMO-03 a los 39,2 s y DEMO-04 a -07 en el k + 0,2 siguiente a lo que pide la spec (≥ 1,6 s después del último cambio de hora); **CAP-22** se toma a los 36,5 s con `MotionMayRun`, porque en ese instante llega L(37) (los rodillos en progreso 0, con la cifra de antes, quedan en `inFlight`) | La regla de toma de la 4b: nunca en un segundo entero ni con un rodillo antes de su mitad |
| B6a-17 | **Las sondas que quedan como valores del `manifest.json`** y no como comprobación: el contraste de las píldoras de CAP-19 contra el vidrio renderizado, la ventana `#FFE082` de CAP-04 en la pasada de contrato, la luz de la cabaña, el matiz OKLCH de CAP-01, -03 y -04, el velo de CAP-20 contra el mismo cuadro sin pausa, los saltos del astro y el fundido de las estrellas de VID-05 y -06, y la aparición sin saltos de VID-03. Se comprueban: el astro (± 2 dp), las estrellas, las nubes (± 1 dp), las marcas (± 2 dp), el temblor (0 píxeles), las dos sondas de contrato del cielo de CAP-02 (`#0B4FA8` y `#2C70BE`: diferencia 0), la banda y la distancia de CAP-13 a -17, el velo y la velocidad de CAP-20 y -21, la toma aérea de CAP-22 (el horizonte es el de la proyección menos el apartamiento; la popa, a la altura de la borda), el fondo provisorio de CAP-24, DEMO-01 a -08 y las de VID-01, -02, -04, -07, -08, -09, -10 y -11 | Para QA (verificación) y WP-D2 (las de aspecto) |
| B6a-18 | **La distancia de escena de CAP-13 a -17** es 234,85 m (Core, con las lecturas de R-4 en los k − 0,5 de la sesión); la comprobación usa ≈ 234 ± 1 | §5.2 decía 234 ± 0,5; la spec dice "≈ 234 m" |
| B6a-19 | **El armado de la escena en el inmersivo** (§2.16, §3.4; lo encontró `[PERF]` en el build): `SceneBridge` le pasa a `SceneWarmupPolicy` solo las finitas que **no** son de cada lectura ni esperan un layout (`MotionDriver.HasFiniteNonRecurringMotions`: los rodillos `TabularNumber` y `TabularCells` y el destello `AccentFlash` llevan `IRecurringMotion`; un `ILayoutBoundMotion` que espera no cuenta). Además, `SceneHost` desarma el mundo **a medias** de un armado que se corta (la escena demorada, la memoria baja o el cierre en plena precarga). `[PERF]` suma a la línea de la escena los cuadros con y sin tramo y el tipo de lo que lo retuvo. Prueba nueva: `SceneLifecycleTests.TheDelayedSceneArrivesWithTheReadingsRunning` (con `UiHostOptions.ScenePlayerWarmup`, el tramo del player en lugar del armado en el acto del arnés) | En el build, con lecturas cada segundo siempre había un rodillo o un destello corriendo, y el `GrowMotion` del aviso de Workout quedaba esperando un layout que no llega detrás del inmersivo: el tramo era 0 en todos los cuadros y la escena demorada (o la que no terminó de prepararse en reposo) **no aparecía nunca** (`[PERF]`: "escena Loading", 1 cuadro con tramo y 4 784 sin tramo). El arnés y las pruebas no lo veían porque arman todo en el acto. Y el mundo a medias dejaba una segunda cámara de la escena (`MarksLandOnTheLegacyPixel` contó 2). El `GrowMotion` que espera para siempre detrás del inmersivo también mantiene los 60 fps del *frame pacing* en Workout: queda para QA (es de la 4a) |
| B6a-20 | **`[PERF]` en el build de simulador de Windows** (Direct3D 11, ventana de 1280 × 720, `ROWMATE_DATA_DIR` temporal con la base `F-5` sembrada; §4.5): **libre con `Fuerte` 60 s en Alto**: por ventana de 10 s, p99 2,7 ms y máx 3,1 a 3,5 ms de CPU del hilo principal, GPU ≤ 7,3 ms, 125 MB; **Medio** (`-rowmate-quality medium`, la línea de base): p99 2,1 ms, GPU ≤ 5,7 ms; la adaptación subió la resolución de 0,85 a 0,95 por los tiempos bajos; **la entrada** (el peor cuadro de los primeros 2,5 s): 12,0 a 23,1 ms (el cuadro que arma el HUD y dibuja la escena por primera vez; en los primeros 30 cuadros, 2,4 a 23,3 ms), la salida ≤ 10,9 ms; **la escena demorada** 1,5 s (`-rowmate-scene-delay`): llega en el inmersivo, con el peor cuadro de la preparación en 9,2 y 10,5 ms (el tramo es de 2 ms: alguna unidad, la variante de *shader* o el dibujo de calentamiento, pasa el tope de 1,5 ms de §2.16) y el cuadro entero en ≤ 14,0 ms; **las horas** (17 a 23 h, `-rowmate-perf-hours`) y **el completado** (`Corta` con el diálogo y la toma aérea, `-rowmate-perf-completion`): sin saltos (máx 3,6 ms de CPU por ventana después de la entrada; el diálogo quieto a 60 fps); **0 compilaciones de *shaders* de la escena después de `Ready`** en todas; tres sesiones seguidas (`-rowmate-perf-entries 3`: libre, `Corta` y `Series 600`) sin avisos nuevos en el registro y 0 `[SLOT]`. `THIRD_PARTY_NOTICES.md` queda al lado del ejecutable | AC-SCN-01-10. Lo de Android y el teléfono de referencia, NO VERIFICABLE (sin el módulo ni el teléfono: It. 14). La prueba a mano con la entrada sintética (la llave, `Hora escena` en 6, 12, 18 y 22 h, pausa y salida) no se hizo: lo cubren DEMO-01 a -08 en el Editor (el mismo panel del simulador) y las corridas de `[PERF]` (la misma hora por el *store*) |
| B6a-21 | **La regeneración única de `it4b` e `it5`** (§5.1): dos corridas de cada conjunto, iguales byte a byte (51 y 47 archivos; el `manifest.json`, igual salvo `durationMs`). En `it4b` cambian las 31 fotos y los 3 videos con rutina (las píldoras con los tokens `.hud`: trabajo, enfriamiento y otro); las del libre dan los mismos bytes. Las sondas de aspecto de las píldoras de `HudExrCatalog` pasan a los tokens `.hud` (`PillRowView.HudColor`): 0 sondas de aspecto que fallan. En `it5` cambian 44 tomas con rutina (y `manifest.json`, solo por `durationMs`) y ninguna sonda | QA explica cada diferencia |
| B6a-22 | **Lo que las tomas muestran para WP-D2** (de aspecto; las de contrato dan diferencia 0): (1) la imagen sale más oscura y menos saturada que LEG-01 a -07: el cielo de contrato es exacto (`#0B4FA8` / `#2C70BE`) y lo que oscurece es el ajuste de la hora (el *tonemapping* Neutral sobre la paleta y −0,15 EV a las 12); (2) las nubes en Alto se ven grises y más grandes que en Bajo y que en el legacy, y **de noche se ven claras** (la spec dice "casi invisibles": el alfa de 0,11 del legacy compuesto en lineal sobre un cielo oscuro pesa más que en la composición sRGB del legacy); (3) las montañas salen bajas y lavadas (bruma y nieve casi del color del cielo) contra las siluetas de LEG; (4) el brillo del sol de las 12 (`it6a-brillo`, TV) mide 22,9 dp con luminancia > 0,9 (mínimo 30: el *bloom* de 0,35 no alcanza); (5) el casco provisorio se lee como un palo celeste vertical (la cubierta con el color de las franjas); (6) el agua, apagada contra el degradé de LEG | WP-D2 ajusta con estas tomas (la tabla de la hora, las nubes, el horizonte, el *bloom*); ninguna es de WP-B |
| B6a-23 | **Lo que la corrida completa de PlayMode mostró** (no la de `Tests/PlayMode/Scene` sola): (1) `PillRowTests` (`ThumbTrackSitsOnTheGlass`, `CurrentFillHasNoHoles`) miran las píldoras con los tokens `.hud` (como el arnés, B6a-21); (2) `HudLayoutTests.RotationWithDialogSealOrBigCount` mira el sello después del fundido de 200 ms del cambio de clase de ventana (DT6a-06: el reacomodo es en el acto y las piezas entran con su fundido); (3) `AppRoot.OnDestroy` destruye la copia del panel (`RuntimePanelFactory.Destroy`): `UiSurface.Dispose` la dejaba sin RT, y una copia viva sin RT dibuja en la pantalla (cada prueba que carga y descarga `Boot.unity` dejaba una); (4) `SceneBudgetTests.DrawCountsPerTierWithinTheBudget` mide con el panel de la prueba en `display: none` (la escena se dibuja igual, a pedido) y deja en el registro lo que más dibuja en el cuadro (cámaras, paneles, mundos de la escena, cámaras que dibujó URP) | Los contadores de URP son de todo el cuadro: en la corrida completa el mismo HUD sin escena da 730 dibujos (solo, 2), probablemente por el atlas dinámico de UI Toolkit ya lleno, y la medida de la escena salía en 800-1 000 |

**Corridas** (desde la raíz del repo; los artefactos en `<scratch>/it6a/`):

| Suite | Resultado |
|---|---|
| `tools/dotnet/run-tests.ps1` | compilación con 0 avisos y 0 errores; simulador 192 / 192; Core 3041 / 3002 pasan / 0 fallan / 39 ignoradas |
| `tools/unity/run-unity-tests.ps1` (Unity 6000.3.27f1, *batchmode*) | compilación limpia; **EditMode**: total 3126, pasan 3081, fallan 0, ignorados 45; **EditMode `-Simulator`**: total 3318, pasan 3273, fallan 0, ignorados 45; **PlayMode**: total 468, pasan 468, fallan 0; **PlayMode `-Simulator`**: total 540, pasan 540, fallan 0. En cada una, la guardia de `ProjectSettings`: sin cambios |
| `tools/unity/capture-snapshots.ps1` (`-Set it6a`, por defecto) | dos corridas completas (45 archivos: 34 PNG y 11 MP4) **iguales byte a byte**, `manifest.json` igual salvo `durationMs`; 45 tomas, 0 con problemas, 0 sondas de aspecto que fallan; una tercera corrida, después de los últimos cambios de `SceneHost`, `SceneBridge` y `MotionDriver`, da los mismos bytes. `INDEX.md` y `manifest.json` sin rutas de la PC |
| `tools/unity/capture-snapshots.ps1 -Set it4b` y `-Set it5` | dos corridas de cada uno, iguales byte a byte (51 y 47 archivos); `it4b`: 0 sondas de aspecto que fallan (B6a-21) |
| `tools/build/build.ps1 -Target Windows -Simulator` | `Builds/Windows/simulator/RowMate.exe` (Mono, Development) con `THIRD_PARTY_NOTICES.md` al lado; la guardia, sin cambios. `[PERF]` en B6a-20 |

`git -C ../rowmate-legacy status --porcelain` vacío al empezar y al terminar; `lib/core/strava/strava_secrets.dart` no se leyó. Las pruebas, las capturas y las medidas usaron carpetas de datos temporales (`ROWMATE_DATA_DIR`): ni `prefs.json` ni las bases del usuario. Sin *commits*.

## Cambios por verificación (ronda 1)

Corrige los issues confirmados de [05-qa-verification](05-qa-verification.md) y de las lentes [05-qa-lens-rendimiento](05-qa-lens-rendimiento.md) y [05-qa-lens-paridad](05-qa-lens-paridad.md) que son del Architect. Lo de esta sección manda sobre §3.1, §3.3, §3.4, §3.9, §4.1, §4.2, §4.4, §4.5 y sobre B6a-14, -17 y -20. No cambia el QUÉ. Cada defecto se reprodujo primero con una prueba que falló contra el código de antes (la corrida "en rojo": 8 pruebas, 6 fallan, en `<scratch>/it6a/fix-r1/red/`) y después se corrigió.

### V1 · Los defectos

| # | Issue | Qué cambió | Prueba (en rojo antes de la corrección) |
|---|---|---|---|
| V1-01 | QA-V6a-01 = QA-V6a-REN-10 (D2-05; AC-SCN-01-07, AC-APP-05-08) | **`HudView` arma el fondo provisorio primero y `scene-surface` justo encima** (en UI Toolkit el hijo de después se dibuja arriba). §3.3 decía "SceneSurface primero (antes de HudBackdrop, que queda debajo)": el orden de los hijos era el contrario de lo que pedía. En el fundido de 0,6 s de la escena demorada se ven los dos y la escena funde **sobre** el provisorio; sin escena y con la escena a la vista no cambia nada (el provisorio en `display: none`) | `SceneLifecycleTests.TheLateSceneBlendsOverTheBackdrop` lee la RT de la UI antes de `Ready`, a mitad del fundido y al final, en una grilla de 11 × 19 puntos donde el provisorio y la escena difieren: antes, 0 de 127 mezclados (127 iguales al provisorio); ahora, 127 de 127 |
| V1-02 | QA-V6a-02 (D2-04; AC-WKT-17-07) | **`ImmersiveWorkoutPage` toma la clase de la ventana en el toque.** `_landscape` se llenaba recién en el primer `ShellLayout.Changed`: la PRIMERA rotación del inmersivo solo lo anotaba (no había con qué compararla) y el HUD aparecía entero. Las siguientes sí fundían (por eso `HudLayoutTests`, que rota varias veces, no lo veía) | `SceneImmersiveTests.TheHudFadesInWhenTheWindowChangesClass`: en el cambio de TV a TH las piezas en < 0,05 (antes, 1), a los 50 ms entre 0 y 1, a los 216 ms en 1; con reducir movimiento, 1 en el acto; la misma sesión de la escena |
| V1-03 | QA-V6a-REN-08 (AC-SCN-01-05) | **`SceneHost.DropBuild()`** suelta el iterador del armado **con `Dispose`** (en `Fail`, `DisposeWorld` y `Dispose`): el `finally` del `using (MeshUpload)` de `SceneWorldBuilder` corre y suelta los búferes nativos. `SceneWorldBuilder.UploadOpen` dice si el armado está en ese tramo y `MeshUpload.LiveForTest` cuenta los `MeshUpload` con búferes vivos (solo cuenta) | `SceneLifecycleTests.ClosingWhileLoadingLeavesNoNativeBuffers` (con `NativeLeakDetection` en `EnabledWithStackTrace`): una unidad por `Tick` hasta la mitad de las mallas y después `Application.lowMemory` (`SimulateLowMemoryForTest`) y `SceneHost.Dispose()`: antes quedaba 1 vivo; ahora, 0 |
| V1-04 | QA-V6a-REN-04 (AC-WKT-13-15, AC-SCN-01-03, AC-SCN-01-07) | (A) **Detrás del negro la orilla sigue a la sesión**: con la sesión abierta y la escena todavía sin verse, `SceneHost.StreamHidden()` arma hacia la distancia de la sesión con el tramo de la orilla (`StreamingBudgetMs`, 1 ms). (B) **`IImmersiveScene.CatchUp()`**: la página la llama en el cuadro del armado del HUD (negro y quieto; con reducir movimiento, el del toque) y, si la orilla de la sesión todavía no está, `SceneHost.CatchUp()` la termina ahí, en el acto. (C) **La escena demorada arma la orilla de la sesión abierta** (fase 8 del armado, con la distancia del cuadro) y vuelve a esa fase si la sesión se alejó más de 25 m antes de `Ready`. `SceneHost.InViewShoreRebuilds` cuenta los armados enteros a la vista (tiene que quedar en 0) y `CatchUps` los del cuadro del HUD | `SceneEntryTests.ReenteringRightAfterALongSessionNeverRebuildsTheShoreInView` (una sesión a 4 km, `Terminar` y otra entrada apenas responde `Entrenamiento libre`, con el rebobinado sin terminar) y `TheDelayedSceneArrivesWithTheShoreOfTheSession` (la demorada con el bote a 2 km): antes, 1 armado entero a la vista; ahora, 0. Con `UiHostOptions.ScenePlayerStreaming` (la orilla por tramos, como en el *player*) |
| V1-05 | QA-V6a-REN-05 (§3.1; AC-SCN-01-06) | **La RT de la sesión nace en la precarga** (fase 4, su propia unidad: la ventana, la resolución y el formato del nivel) y **el dibujo de calentamiento va a ella** (fuera de pantalla). Se borró la RT aparte de 256² `ARGB32`. Los estados de *pipeline* del destino de verdad (formato y MSAA) quedan calentados y la entrada no rehace la RT | `SceneEntryTests.ReadyAlreadyHasTheSessionTarget`: en `Ready` la RT existe con el tamaño y el formato del nivel, `WarmupDraws` = 1, `Draws` = 0 y la entrada usa la misma RT (la misma generación). Antes, `Output` nulo en `Ready` |
| V1-06 | QA-V6a-REN-01 (AC-WKT-13-15) | **El detector**: `Core/Scene/ShaderWorkLog` (puro) cuenta las líneas que el *player* escribe de verdad ("Uploaded shader variant to the GPU driver: …", y "Compiled shader" / "Compiling shader" del Editor) de `RowMate/Scene/*` **y** `Hidden/Universal Render Pipeline/*`, después de la marca `[PERF] marca: escena Ready` que ahora escribe `FrameTimeProbe`; la UI no cuenta. `FrameTimeProbe` las cuenta si le llegan por `logMessageReceived` y **`tools/build/perf-log-check.ps1 -Log <registro>`** las cuenta sobre el registro del *player* (0 ninguna, 2 alguna con la lista, 3 sin la marca). **El precalentado del post de URP**: dos unidades nuevas antes del dibujo de calentamiento, un pedido de render con la máscara de la cámara en 0 a una RT chica del formato de la sesión (el primer pedido: el código de URP por primera vez) y otro a la RT de la sesión (UberPost, Bloom y el LUT con las palabras clave de la sesión y sus intermedios de ese tamaño) | `ShaderWorkLogTests` (dotnet: el control positivo, una subida de UberPost después de la marca, cuenta 1; lo de la UI y lo de antes de la marca, 0; sin la marca, −1) e `ItSixAToolingTests.ThePerfLogCheckCountsShaderUploadsAfterReady` (corre el *script* de verdad con tres registros de prueba: 2, 0 y 3). En el build: 0 después de la marca en todas las corridas (el registro y la sonda); antes de la marca se ven las subidas de los 13 *shaders* de la escena, de `LutBuilderLdr`, `Bloom` y `UberPost` (`_BLOOM_LQ`) |
| V1-07 | QA-V6a-REN-02 (AC-WKT-13-15) | **La sonda dice cuál es el peor cuadro de la entrada**: con el tiempo del hilo principal del cuadro anterior (`ProfilerRecorder` "Main Thread", con un cuadro de atraso exacto) y un anillo con el estado de cada cuadro (ms desde el toque, negro y quieto = desde los 220 ms hasta el cuadro en que se arma el HUD, las recolecciones del GC), escribe el peor con su momento y el peor **con movimiento** (el que cuenta para AC-WKT-13-15), las recolecciones de la entrada y de cada ventana. La correspondencia por `frameStartTimestamp` de `FrameTimingManager` no sirve en este *player* (los dos relojes tienen otro origen; la sonda lo anota una vez). **La sesión siguiente se arma en reposo** (`SceneHost.PrepareNextSession`, con su `SceneFrame`): el toque ya no arma la sesión ni sus arreglos | Lo que dijo la sonda (§V1-14): el peor cuadro de la primera entrada es el del toque (12,6 ms, con movimiento) y el de las siguientes, el del armado del HUD (9,3 y 12,2 ms, negro y quieto; con movimiento ≤ 6,3 ms); hubo una recolección en el armado del HUD (a los 242-249 ms) sin pico. No hizo falta colectar en `Ready` |
| V1-08 | QA-V6a-REN-06 | **Cada unidad del armado se mide** (`SceneHost.WorstUnitMs`, `WorstUnitPhase`, `WorstUnitStep` y la peor de cada fase, `WorstUnitMsOf`; `[PERF]` las escribe). **Unidades más chicas**: el mundo en 25 pasos con nombre (`SceneWorldBuilder.Step`: la cámara y la luz por separado, cada textura con su subida en su unidad y 2 048 texeles por unidad, cada pino y cada pino podado, una variante de roca o de junco por unidad, los materiales en tres, las piezas en seis); el dibujo de calentamiento en dos (su cuadro y el dibujo) y el post de URP en dos (V1-06). **El toque, el arrastre y la rueda cuentan como movimiento**: `ShellHost` le avisa a `MotionDriver.NoteUserInput` desde la raíz y `SceneBridge` pasa `UserInputActive` a la política (hasta 0,3 s después del último evento y mientras haya un botón apretado). `ScenePolicyTests.NoIndivisibleStep` ya no lleva la categoría AC-WKT-13-15 (compara la tabla consigo misma) | `SceneEntryTests.EachWarmupUnitIsMeasured` (la primera y la segunda precarga con el tramo del *player*). Lo medido en el *player* en §V1-14: con la escena demorada (lo que corre en el inmersivo) la peor unidad es de 3,8 a 6,2 ms y el peor cuadro de la preparación, 5,0 a 7,7 ms. En la precarga en reposo las primeras veces del código (el JIT de Mono: el primer `Tick` sintético 14,4 ms, el primer pedido de URP 20,0 ms, el dibujo de calentamiento 9,1 ms) pasan el objetivo de 1,5 ms de §2.16 (ver "Lo que queda") |
| V1-09 | QA-V6a-REN-07 y QA-V6a-09 (AC-SCN-01-11) | (a) **El formato de la RT por nivel** (`SceneRenderTarget.FormatFor`): Medio (el teléfono) usa `B10G11R11_UFloatPack32` (4 B/px, la misma condición que URP usa para su intermedia HDR, con `R16G16B16A16_SFloat` si la plataforma no la mezcla), así URP arma su intermedia y su MSAA con 4 B/px como en la tabla de §4.1; Alto (el escritorio) sigue en `R16G16B16A16_SFloat` (la precisión de las sondas ± 1 y de las tomas); Bajo, `R8G8B8A8_SRGB`. (b) **Las texturas procedurales dejan de ser legibles** al subirse (`Apply(…, makeNoLongerReadable: true)`) y sueltan su búfer: el contenido era el doble de la tabla. (c) **La CPU de la escena sin el dibujo de URP** (`RowMate.Scene.Update`, `SceneHost.LastCpuMs`) | `SceneBudgetTests.MemoryAndSceneCpuPerTierWithinTheBudget` (Alto en W1280, Medio y Bajo en TV): el contenido (≤ 1 MB; Alto ≤ 1,6 MB con la capa lejana), las RT que suma la escena ("Render Textures Bytes" antes de la precarga y dibujando; Medio ≤ 64 MB, Bajo ≤ 40 MB, Alto ≤ 180 MB) y la CPU de la escena (p95 ≤ 1,5 ms). Los números, en §V1-13 |
| V1-10 | QA-V6a-REN-09 (AC-SCN-01-13; ADR 018) | **`SceneHost.LowerTier()`**: el último paso de la adaptación baja la escena entera (el perfil de abajo con `SceneQualityApplier` sobre el mundo armado, sin rehacerlo, más el asset de URP por el `IRenderAssetQuality` de sus opciones); la resolución no sube (`SetResolution` queda acotada por la del nivel). `AppRoot.ApplyQuality` lo usa. **`GraphicsTier`**: Medio en escritorio con una GPU integrada conocida (`IsIntegratedDesktopGpu`: Intel UHD/HD/Iris/Graphics sin "Arc" con modelo, las APU de AMD, el adaptador de software) | `SceneAdaptationTests.LoweringTheTierLowersTheWholeSceneProfile` (Alto → Medio → Bajo con la misma sesión y el mismo mundo: el campo de estrellas, el MSAA, las sombras, el relleno, el bloom, el HDR y el formato de la RT del perfil; Bajo es el último) y `GraphicsTierTests` (Unity EditMode, con nombres de catálogo) |
| V1-11 | QA-V6a-REN-03 | El build de simulador rehecho con el código final y todo §4.5 otra vez, con `Serie larga` (`-rowmate-perf-long-series <s>` y `SnapshotCli.SeedDatabase -seed F4B`; QA-V6a-13) y la cuenta de asignaciones por cuadro del hilo principal ("GC Allocation In Frame Count") en cada ventana | §V1-14 |
| V1-12 | QA-V6a-10 | Los tres CS8625 (`SceneLighting.cs`, `SceneRenderer.cs`): `null!` | La compilación de Unity sin avisos nuevos |

### V1 · Los menores resueltos

| # | Issue | Qué cambió |
|---|---|---|
| V1-M1 | QA-V6a-PAR-05 = QA-V6a-11 | LT-78 a LT-81 y LT-83 a LT-87 en `It06aIds.UnityOnlyIds`; `TraceabilityTests` clasifica también los LT (24 AC + 9 LT): sin `-UnityResults`, `run-tests.ps1 -Matrix` los da PENDIENTE UNITY |
| V1-M2 | QA-V6a-REN-11 = QA-V6a-07 | `SceneBudgetTests.NoAllocationsPerFrameWithTheHudAndTheScene`: con `Series 600`, los dos paneles abiertos y la escena, 120 cuadros reales en los que se mide TODO el C# del cuadro (el `MotionDriver` con el HUD, el `Tick` de la escena con la orilla, el pedido de render, el repintado de la superficie y el de los paneles), con un borde de segmento adentro de la ventana: 0; el control positivo (un `byte[64]` en el `Tick`, adentro de lo medido) cuenta |
| V1-M3 | QA-V6a-REN-13 | Con `-rowmate-quality` la adaptación queda apagada (la resolución del nivel en toda la corrida; el registro lo dice). La GPU "por pieza" de §4.1 no se escribe (ver "Lo que queda") |
| V1-M4 | QA-V6a-REN-14 | `FrameTimeProbe` escribe por ventana los dibujos y los SetPass máximos del cuadro (la escena y la UI): no crecen de una sesión a otra en las corridas de §V1-14 |
| V1-M5 | QA-V6a-REN-15 | `ci-unity.yml` corre con `-testCategory "!GPU"` y `SceneTestBase.AssumeGpu` ignora la prueba con un rasterizador por software (llvmpipe, softpipe, SwiftShader, el adaptador básico de Windows) |
| V1-M6 | QA-V6a-REN-16 | `it6a-low` se toma con el asset de URP de Bajo (`UiHostOptions.SceneUrpTier` → `TierScope` mientras dura el anfitrión) además del perfil; su texto dice que la profundidad de GLES queda NO VERIFICABLE hasta el teléfono (It. 14) |
| V1-M7 | QA-V6a-REN-17 | Los `[Ignore]` de `WorkoutDeviceChecksTests` dicen lo medido en §V1-14 |
| V1-M8 | QA-V6a-REN-18 | La guardia de los *scripts* también mira `Assets/RowMate/Scenes`, `Slots` y `Art/Materials/Scene` |
| V1-M9 | QA-V6a-04 | CAP-22 se toma cuadro a cuadro desde los 29 s (`ScenarioScript.StepTo`), como VID-08: la escena ve cada lectura y la fase de la toma aérea en su momento |
| V1-M10 | QA-V6a-06 | `-rowmate-perf-hours` fija la hora 2 s antes del toque; los *drivers* que arrancan con `StartRoutine` directo (`-perf-completion`, `-perf-series`, `-perf-long-series`) lo anotan en el registro: su primera entrada no es la del usuario |
| V1-M11 | QA-V6a-13 | `SnapshotCli.SeedDatabase -seed F4B` y `-rowmate-perf-long-series` (también en `BuildOutputInspector.DiagnosticNeedles` y en `ItSixAToolingTests`) |

### V1-14 · `[PERF]` en el build con el código final (reemplaza a B6a-20)

`tools/build/build.ps1 -Target Windows -Simulator` con el código de esta ronda (Mono, Development, Direct3D 11, ventana de 1280 × 720), `ROWMATE_DATA_DIR` en una carpeta temporal propia con la base `F-5` sembrada (y `prefs.json` con los dos paneles abiertos para las entradas) o `F-4B` para `Serie larga`; los argumentos del *player* en una sola cadena. Las preferencias de la ventana del *player* (registro de Windows) se resguardaron antes y se restauraron al final, iguales al resguardo. Ningún `RowMate.exe` del usuario estaba abierto. En **todas** las corridas: `tools/build/perf-log-check.ps1` da **0 subidas de *shaders* de la escena o de URP después de `Ready`** (la marca está en todas), **0 `[SLOT]`**, sin avisos ni excepciones nuevas.

| Corrida | Entrada (2,5 s) | Ventanas de 10 s después de la entrada | Salida (0-1 s) | La escena |
|---|---|---|---|---|
| `-rowmate-perf-entries 10 -rowmate-perf-routines "Prueba 4a;Cinco reps"` (paneles abiertos; el camino de un usuario que ya pasó por Workout) | 1.ª (libre, la primera del proceso) **12,7 ms**, en el cuadro del toque (con movimiento); 2.ª a 10.ª **7,0 a 10,5 ms**, siempre en el cuadro del armado del HUD (a los 224-225 ms, negro y quieto), **≤ 6,5 ms con movimiento** | paneles: máx ≤ 10,5 ms, p99 ≤ 3,0 ms, ≤ 129 MB; dibujos y SetPass máximos 88-103 / 59-60, sin crecer entre sesiones | **4,4 a 7,9 ms** | dos recolecciones del GC en el armado del HUD (a los 224 y 241 ms) sin pico; 0 armados de la orilla a la vista, 0 terminados en el negro |
| `… -rowmate-perf-entries 3 … -rowmate-scene-delay 1.5` (la escena demorada) | 14,8 / 9,0 / 8,2 ms (la 1.ª en el cuadro del toque, que en este modo de prueba desarma el mundo; las otras en el armado del HUD); con movimiento ≤ 15,1 ms | — | 5,4 a 9,2 ms | **el peor cuadro de la preparación 5,1 a 6,7 ms**; la peor unidad 3,4 a 5,6 ms (el almacén de la orilla); la escena llega en las tres |
| `-rowmate-perf-scene 300` (libre con `Fuerte`, Alto), **dos corridas** | 22,6 / 22,5 ms (el cuadro del toque del *driver*) | 1.ª: 29 ventanas con máx 3,0-3,6 ms y p99 2,6-3,0 ms, y **una con un cuadro de 93,5 ms** (a los 271 s, 17 cuadros de menos en la ventana, los dibujos y SetPass del cuadro al doble); 2.ª: **29 de 29 con máx ≤ 4,1 ms**, p99 ≤ 2,9 ms; GPU ≤ 5,5 ms; 124 MB estables | 8,7 ms | una recolección por corrida, sin pico en la 2.ª |
| `-rowmate-perf-scene 120 -rowmate-quality medium` (la línea de base, **sin adaptación**: la resolución en 0,85 toda la corrida) | 22,0 ms (el toque) | máx ≤ 3,6 ms, **p99 2,2-2,3 ms, GPU 5,1-5,3 ms**, 66-72 dibujos / 43-48 SetPass, 123 MB | 7,9 ms | la RT de la sesión `B10G11R11` |
| `-rowmate-perf-hours` (la hora fija 2 s antes del toque, después 18 → 23 h) | 21,2 ms (el toque) | máx ≤ 4,5 ms | 9,4 ms | — |
| `-rowmate-perf-completion` (`Corta` con `StartRoutine` directo, el diálogo y la toma aérea) | 24,1 ms (el toque) | máx ≤ 11,1 ms (la entrada del diálogo), p99 ≤ 3,1 ms | 10,0 ms | el diálogo quieto sigue a 60 fps (P-07, ver "Lo que queda") |
| `-rowmate-perf-series 60` (`Fondo 3000` y `Series 600` con `StartRoutine` directo y los paneles cada 2 s) | 1.ª 24,1 ms (el toque), 2.ª 7,8 ms | paneles: máx ≤ 17,1 ms (una ventana de la 1.ª rutina), el resto ≤ 4,5 ms; p99 ≤ 3,2 ms | 8,3 / 5,3 ms | — |
| `-rowmate-perf-long-series 150` (`Serie larga` con la escena, `Fuerte`; QA-V6a-13) | 22,4 ms (el toque) | **máx ≤ 7,5 ms, p99 ≤ 5,4 ms** en las 14 ventanas (los cambios de paso cada 30 s adentro), GPU ≤ 5,9 ms, 135-186 MB | 12,3 ms | — |

Lectura:

- **AC-WKT-13-15 (la entrada sin tirón)**: por el camino del usuario el peor cuadro con movimiento es el del toque de la primera entrada del proceso (12,7 ms) y en las siguientes ≤ 6,5 ms; lo de más de 10 ms cae en el cuadro del armado del HUD (negro y quieto, lo que el criterio admite). La sonda ahora lo dice en cada entrada. **Los *drivers* que tocan "Entrenamiento libre" o una rutina sin pasar por la lista (todos menos `-rowmate-perf-entries`) dan 21 a 24 ms en el cuadro del toque de su primera entrada**, como en B6a-20 y en la verificación (19,7 a 23,3 ms): no es la escena ni el armado del HUD (la sonda lo ubica a los 0 ms del toque), es la primera vez del toque llamado desde el programador; no pasa por el camino del usuario (la prueba a mano de QA: 11,9 a 13,0 ms). Queda anotado en el registro de cada *driver* (QA-V6a-06).
- **AC-SCN-01-10 (fluida en la PC)**: p99 ≤ 3,0 ms y GPU ≤ 5,9 ms en todas las ventanas de régimen; **un cuadro de 93,5 ms** en una de las 59 ventanas de las dos corridas de 5 min (no se repitió en la segunda ni en `Serie larga`; los contadores del cuadro al doble y 17 cuadros de menos son los de un proceso detenido, no los de un cuadro de la escena). Ver "Lo que queda".
- **AC-WKT-14-19 (`Serie larga` con la escena)**: máx 7,5 ms en 150 s con cinco cambios de paso.
- **Asignaciones por cuadro** ("GC Allocation In Frame Count", el hilo principal): remando en el libre, ≈ 22 cuadros por ventana de 10 s con ≈ 85 asignaciones (la actualización de 1 Hz del HUD, que §20.2 admite) y 0 en los demás; **con rutina hay ventanas con 840 a 1 650 cuadros con asignaciones** (en `Serie larga` alrededor de cada cambio de paso; en `Corta`, de los 10 a los 20 s), hasta 300 por cuadro. No es la escena (SBT: 0 en el `Tick`, en URP y en el HUD con la escena, V1-M2): es del HUD con rutina en el *player* y queda para QA (ver "Lo que queda").
- **El armado en reposo**, en el *player*: la peor unidad es la primera vez de un código (el JIT de Mono): el primer pedido de render de URP (19,1 a 20,0 ms, en la RT chica), el primer `Tick` sintético (14,4 ms) y el dibujo de calentamiento (8,6 a 10,3 ms); el resto ≤ 4,5 ms. Corren con Workout en reposo y quieto (a 30 fps), nunca con algo moviéndose ni con el usuario tocando o desplazando.

### Lo que queda (desvíos de esta ronda)

| Qué | Por qué | Para quién |
|---|---|---|
| **QA-V6a-REN-06, en parte**: las unidades de la primera vez del código en la precarga en reposo (el primer pedido de URP ≈ 20 ms, el primer `Tick` sintético ≈ 14 ms, el dibujo de calentamiento ≈ 9 ms) pasan el objetivo de 1,5 ms de §2.16 | Son el JIT de Mono del código de URP y de la sesión: no se parten sin partir los métodos. Corren con Workout en reposo y quieto (a 30 fps: entran en el cuadro de 33 ms), nunca con un movimiento finito ni con el usuario tocando o desplazando; con la escena demorada (lo que corre en el inmersivo) el JIT ya pasó y la peor unidad es de 3,4 a 5,6 ms y el peor cuadro de la preparación, 5,1 a 6,7 ms. `SceneWarmupPolicy.UnitCostBoundMs` queda como el objetivo del diseño; la evidencia es lo medido | QA (aceptar o pedir el JIT por adelantado en el arranque) |
| **El cuadro de 93,5 ms** de una de las dos corridas de 5 min (AC-SCN-01-10) | No se repitió (la segunda corrida, `Serie larga`, las entradas y las horas sin un cuadro de más de 17,2 ms). El cuadro tiene los contadores de dos cuadros (dibujos y SetPass al doble) y faltan 17 cuadros en su ventana: lo de un proceso detenido. NO DEMOSTRADO que sea de la app ni del GC (la única recolección de esa ventana no cae en ese cuadro según la sonda, que puede correrse un cuadro) | QA: repetir `-rowmate-perf-scene 300` con el equipo sin otra carga |
| **QA-V6a-08 = QA-V6a-PAR-02**: las sondas de la ventana `#FFE082` (contrato, CAP-04), de la luz de la cabaña (± 25 %), del matiz OKLCH de CAP-01, -03 y -04 y del velo de CAP-20 no están en el `manifest.json` | Ubicar la ventana y el frente de la cabaña en el cuadro pide proyectar los vértices de la malla de la cabaña con la cámara del cuadro (no es una marca del legacy ni un punto fijo en dp); no entró en esta ronda. B6a-17 queda corregido: esas sondas las mide QA en los PNG (como hizo WP-D2, 04-design-notes §10.4) | QA / Designer |
| **QA-V6a-REN-12** (P-07: el diálogo quieto no baja a 30 fps) | Es una decisión de producto: si P-07 sigue, hay que pausar lo continuo del HUD congelado detrás del diálogo y que el `GrowMotion` que espera un *layout* oculto no pida ritmo | PO |
| **QA-V6a-REN-13, en parte**: la GPU "por pieza de la demo" de §4.1 no se escribe | Pide apagar las piezas una por una en el *player*; la línea de base de Medio sin adaptación sí está (V1-14) | It. 14 |
| **QA-V6a-REN-14, en parte**: los dibujos y SetPass del cuadro se miden por ventana (no crecen en diez sesiones seguidas), sin un tope que haga fallar | Un tope de la UI en el *build* pide una corrida con cambios de idioma y rotaciones que el *driver* no hace | QA |
| **Asignaciones por cuadro con rutina en el *player*** (lo nuevo que muestra el contador de V1-11): ventanas con 840 a 1 650 cuadros con asignaciones (hasta 300 por cuadro) alrededor de los cambios de paso de `Serie larga` y en `Corta` de los 10 a los 20 s | No es la escena (0 B en el `Tick`, en URP y en el HUD con la escena: SBT); en el Editor, HBT da 0 en las ventanas que mide. Es del HUD con rutina y de la It. 4b/5 | QA (issue nuevo para el HUD) |
| `Medio` con `B10G11R11` | La precisión (6/6/5 bits de mantisa) contra el bandeado de los degradés del cielo en el teléfono | It. 14 (NO VERIFICABLE sin teléfono) |

### V1-13 · La memoria y la CPU de la escena por nivel (corrige §4.1)

Medido por `SceneBudgetTests.MemoryAndSceneCpuPerTierWithinTheBudget` (Unity PlayMode, Direct3D 11; Medio y Bajo en TV = 1 080 × 2 400, el teléfono de referencia; Alto en W1280, el escritorio), con la escena dibujando a las 12 h en E-920:

| Nivel | Contenido (GPU) | RT que suma la escena (la de la sesión, los intermedios de URP con su MSAA, el *bloom*, el mapa de sombras) | La RT de la sesión | CPU de la escena sin el dibujo de URP (p95 / máx de 120 cuadros) |
|---|---|---|---|---|
| Alto (W1280) | 1,15 MB (con la capa lejana de nubes) | 46,9 MB | 1 280 × 720 `R16G16B16A16_SFloat` | 1,32 / 10,6 ms |
| **Medio (TV)** | **0,65 MB** | **51,2 MB** (65,5 MB con `R16G16B16A16_SFloat`, antes de esta ronda) | 918 × 2 040 `B10G11R11_UFloatPack32` | **0,83** / 10,2 ms |
| Bajo (TV) | 0,65 MB | 38,2 MB | 810 × 1 800 `R8G8B8A8_SRGB` | 0,72 / 10,0 ms |

- La tabla de §4.1 queda con estos números en las columnas de texturas y RT: contenido ≈ 0,65 MB en Medio (sin la copia de la CPU: el Editor informa el doble con `GetRuntimeMemorySizeLong` aunque la textura ya no sea legible, por eso la prueba suma formato × tamaño × mips); **RT nuevas ≈ 51 MB en Medio**, con la profundidad de la RT de la sesión que la tabla no contaba (el tope de §20.1, 180 MB entre todo, sigue con margen).
- La CPU de la escena entra en el 1,5 ms de §4.1 con el p95; el máximo (≈ 10 ms) es el primer cuadro después de llevar la sesión a 920 m con el control de prueba (un salto: la orilla entera en el acto, lo que en el *player* nunca pasa a la vista, V1-04); en el *player* `[PERF]` da la CPU del hilo principal entera con p99 ≤ 3,0 ms (V1-14).
- Dibujos, SetPass y triángulos no cambiaron (`DrawCountsPerTierWithinTheBudget`: Alto 89-90 / 60-61 / 215 k, Medio 69-70 / 45-46 / 155 k, Bajo 56-57 / 32-33 / 106 k).

### Corridas de esta ronda (artefactos en `<scratch>/it6a/fix-r1/` y `<scratch>/it6a/fix-architect-r2/`)

| Suite | Resultado |
|---|---|
| Rojo (antes de corregir): `run-unity-tests.ps1 -Platform PlayMode -Filter …` con las pruebas nuevas | 8 pruebas: **6 fallan** (V1-01 a V1-05: 0 de 127 puntos mezclados, el HUD en 1 al rotar, 1 `MeshUpload` vivo, 1 armado entero de la orilla a la vista en las dos de V1-04, `Output` nulo en `Ready`) |
| `tools/dotnet/run-tests.ps1 -Matrix` | compilación con **0 avisos y 0 errores**; herramientas 192 / 192; Core **3049: 3010 pasan, 0 fallan, 39 ignoradas**; matriz: PASA 615, FALLA 0, IGNORADO 4, PENDIENTE UNITY 143, **SIN TEST 0** |
| `tools/unity/run-unity-tests.ps1` | **EditMode** 3137: 3092 pasan, 0 fallan, 45 ignorados; **EditMode `-Simulator`** 3329: 3284 / 0 / 45; **PlayMode 480 / 480**; **PlayMode `-Simulator` 552 / 552**; 0 inconclusos; en cada una la guardia (ahora también `Scenes`, `Slots` y los materiales de la escena): sin cambios. Sin los CS8625 de la escena |
| `tools/unity/capture-snapshots.ps1` (`it6a`), dos corridas (A a `<scratch>`, B al repo) | 45 tomas, 0 con problemas, 0 sondas de aspecto que fallan; **A y B iguales byte a byte** (46 archivos; `manifest.json`, solo `durationMs`). Contra lo publicado antes cambian **solo** CAP-22 (cuadro a cuadro desde los 29 s: la escena a 119,872 m y 0,051 m/s, como VID-08), `it6a-low` (con el asset de URP de Bajo), VID-09 (el fundido de 200 ms al rotar), VID-11 (el fundido de la escena demorada sobre el provisorio) e `INDEX.md` (el texto de `it6a-low`). `it2` a `it5` no cambian (sin escena no cambia nada del HUD) |
| `tools/build/build.ps1 -Target Windows -Simulator` | `Builds/Windows/simulator/RowMate.exe` con el código final y `THIRD_PARTY_NOTICES.md` al lado; la guardia, sin cambios. `[PERF]` en V1-14 |

`git -C ../rowmate-legacy status --porcelain` vacío al terminar; el módulo de credenciales del legacy no se leyó. Las pruebas, las capturas y las medidas usaron carpetas de datos temporales (`ROWMATE_DATA_DIR`): ni `prefs.json` ni las bases del usuario; las preferencias de la ventana del *player*, restauradas al resguardo. Sin *commits*.

## Cambios por verificación (ronda 2)

Corrige el mayor QA-V6a-R2-01 de [05-qa-verification](05-qa-verification.md) §R2.7 (que QA proponía como deuda al empezar la 6b: se hizo ahora) y los menores baratos del Architect. Lo de esta sección manda sobre lo anterior. No cambia el QUÉ ni el aspecto: el halo, el latido de la cuenta y la cuenta grande se ven igual (las tomas, abajo).

### V2-01 · QA-V6a-R2-01: el HUD con rutina asignaba en cada cuadro de los últimos 5 s de cada paso

**Dónde (con el Profiler del build de desarrollo).** El *driver* `-rowmate-perf-long-series` ahora acepta `-rowmate-perf-profile <archivo.raw>`: graba el Profiler (registro binario, con la pila de C# de cada asignación, `Profiler.enableAllocationCallstacks`) de los 25,5 a los 27,5 s de `Serie larga`, y `RowMate.EditorTools.ProfileAllocations.Dump` (Editor, `-executeMethod`, `-rowmate-profile <raw> -rowmate-profile-out <txt>`) lo lee con `ProfilerDriver` y escribe cada lugar que asigna con su cuenta, sus bytes, los marcadores de arriba y la pila. Lo que dio en el *player* (312 cuadros con asignaciones de 313):

| Lugar | Por qué | Cuánto |
|---|---|---|
| `PreLateUpdate.UIElementsUpdatePanels` → `UIElements.UpdateStyle` → `InlineStyleAccess.ApplyInlineStyles` → `ComputedStyle.ApplyStyleValue` (×2) y `ApplyStyleTransformOrigin` | `StepPulse` escribía en cada cuadro `style.color` (y `style.scale`) de `#hud-countdown`: un estilo en línea que cambia rehace el estilo del elemento en ese cuadro y UI Toolkit copia cada grupo de estilo que el elemento tiene en línea (el color, heredado; el margen; el origen y la escala) | 3 por cuadro, ≈ 550 B |
| `PostLateUpdate.UIElementsRepaintPanels` → `UIR.RenderTree.ProcessChanges` → `UIRenderDevice.Allocate` → `new Page` (`BlockPool`, `BestFitAllocator`, `GPUBufferAllocator`) | El halo de la en curso estaba en la fila de píldoras: su latido repintaba **la fila entera** (las 200 píldoras de `Serie larga`) en cada cuadro y UIR, con la malla anterior todavía en uso por la GPU, pedía páginas nuevas de vértices una y otra vez (las asignaciones del GC y la memoria **nativa** que subía de 135 a 178-186 MB en esas ventanas) | ráfagas de ≈ 30 por cuadro, varias veces por segundo |

El contador de la sonda ("GC Allocation In Frame Count") daba 136-300 por cuadro; el Profiler, con su propia carga, muestra los mismos lugares. Las pruebas no lo veían: `FrameAllocations` medía el paso del cuadro y los dibujos observados, y esto pasa en el **trabajo del panel** de UI Toolkit (estilos y mallas), fuera de los dos.

**La corrección.**

1. **Lo que cambia en cada cuadro va en el dibujo, no en estilos en línea.** `TabularNumber.SetPaintColor` / `ClearPaintColor`: el color de las cifras se aplica en su `generateVisualContent` (el mismo color que el estilo daba: los mismos píxeles); sin cambios, no repinta; `DrawnColor` para las pruebas y el arnés (CAP-22 de la 4a lo lee). `StepPulse` lo usa para el latido del color de la cuenta del paso y ya no escribe `style.color` en cada cuadro. La escala de la cuenta sigue en `style.scale` (con su `transform-origin` en el borde derecho) pero se escribe **solo si cambió**: se mueve en el resorte de la entrada y de la salida de "por terminar" (unos cuadros) y queda quieta en 1,1 mientras late. Una primera versión llevaba también la escala al dibujo (`SetPaintScale`, escalando el tamaño de letra): cambiaba ≈ 1 px la posición de las cifras en las tomas de la cuenta de la 4b y la 5 y se descartó. La opacidad del fondo de Próximo se escribe solo si cambia (por una opacidad UI Toolkit no copia estilos: se midió). Queda como regla para el HUD (ARCHITECTURE §20.2).
2. **El halo lo dibuja la barra.** `PillRowView.HostHalosIn(StepInfoBar)`: el halo (y el de la que termina) lo dibuja el `generateVisualContent` del PADRE de la fila, con la misma textura, los mismos 36 vértices (corridos a las coordenadas del padre con `ChangeCoordinatesTo`) y el mismo orden (el contenido del padre va debajo de sus hijos: debajo de las píldoras). No suma elementos (el presupuesto de 60 sigue en 56-57). `PillRowMotion.HaloRepaint`: el latido repinta solo el halo; la fila se repinta por el relleno (¼ px) y en el cambio de paso. Sin anfitrión (las pruebas de la fila sola), como antes.

**La prueba (en rojo antes).** `HudBudgetTests.LongSeriesNoAllocationsPerFrame` (AC-WKT-14-19, AC-WKT-17-06), de los 26,2 a los 31,5 s de `Serie larga`, remando y sin pausa (el halo, el latido de la cuenta, la cuenta grande con su aro, el cambio de paso a los 30 s y el halo que se apaga). Mide en **cuadros reales** todo lo del cuadro menos el reloj de prueba:

- `FrameAllocations.WatchTree(raíz)`: envuelve **todos** los `generateVisualContent` del inmersivo (57) con la cuenta por elemento (`AllocatingElements`, `DrawsOf`);
- `FrameAllocations.PanelStep()`: corre el `Update` y el `UpdateForRepaint` del panel (internos de UI Toolkit, con delegados armados una vez por reflexión) **adentro** de la medida, después del paso: el recálculo de estilos, el layout, los eventos de geometría y las mallas de UIR; los dibujos observados van aparte;
- el reloj de prueba (las lecturas del remo y el segundo del motor con su `Apply`, lo de 1 Hz que §20.2 permite) con `Clock`, con su tope de cuadros.

El control positivo, en la misma prueba y con la misma medida: un elemento del árbol que asigna en su dibujo y en su `GeometryChangedEvent` (cambia de alto en cada cuadro) se ve en **los 30 de 30 cuadros** y rompe el tope de cuadros seguidos. Lo que se afirma es lo de R2-01, ninguna asignación continua: a lo sumo 32 cuadros con asignaciones en los 318 (los eventos), ningún tramo seguido de más de 14 (el cambio de paso, ≈ 0,2 s) y a lo sumo 2 en el tramo quieto de 28,25 a 29,65 s (sin eventos del reloj). La cuenta grande, la cuenta del paso y el halo se repintan en la ventana (> 60 veces cada uno) y el latido de la cuenta se ve (la escala del resorte > 1,05 y el color dibujado distinto del de reposo).

| | Cuadros con asignaciones (de 318) | Tramo seguido más largo | En el panel |
|---|---|---|---|
| Antes (el código de la ronda 1) | **242** | > 100 | 1 270 asignaciones |
| Con el color de la cuenta en el dibujo | 34 | 6 (ráfagas de UIR de 5 cuadros) | 586 |
| Con el halo en la barra (final; corridas completas sin y con simulador) | **20** | **12** (el cambio de paso, de 30,000 a 30,183 s) | 293 en 19 cuadros |

Lo que queda en la ventana son eventos, no cuadros: el cambio de paso (el `StepSwap`, la salida de Próximo, la fila que se repinta mientras termina la píldora), la entrada de la cuenta grande, las lecturas del remo (`AccentFlash`) y, de a un cuadro, alguna página nueva de UIR cuando la fila se repinta por su relleno. En el Editor el control positivo del panel lo ve en cada cuadro.

**En el *player*** (el build de simulador rehecho; las mismas corridas de QA en §R2.4, con las bases `-seed F4B` y `F-5` de QA copiadas a una carpeta de datos propia; `perf-log-check.ps1`: 0 subidas de *shaders* después de `Ready` en todas). Dos tandas: **v1** con la primera versión (la escala de la cuenta también en el dibujo) y la PC sin otra carga; **final** con el código final y el build final, pero con otra aplicación pesada abierta en la PC (no de esta tarea: no se tocó), así que sus **tiempos no son comparables** y de ella valen las asignaciones y la memoria. La diferencia entre las dos versiones es solo cómo se escribe la escala de la cuenta (que en régimen no se escribe en ninguna de las dos).

| Corrida | Antes (QA, ronda 2) | v1 | Final |
|---|---|---|---|
| `-rowmate-perf-long-series 150`, las ventanas con los últimos 5 s de un paso | **≈ 840 cuadros con asignaciones** por ventana de 10 s (máx 169-184 por cuadro), memoria **178-186 MB**, p99 5,2-5,6 ms | **36-55 cuadros** (las demás ventanas, 27-30), memoria **135-151 MB** (135 en 10 de 14), p99 5,1-5,3 ms, máx ≤ 6,8 ms | **41-51 cuadros** (las demás, 26-31), memoria **135-151 MB** (135 en 12 de 14); tiempos con la PC cargada |
| `-rowmate-perf-completion` (`Corta`), de 10 a 30 s | **860 y 1 647** de 1 649 cuadros | **81 y 48** | **58 y 36** |
| `-rowmate-perf-entries 10` | 1.ª entrada 16,7 ms (R2-04) | 1.ª 12,7 ms (con movimiento, el cuadro del toque); 2.ª a 10.ª ≤ 9,8 ms en el armado del HUD (negro y quieto), ≤ 6,4 ms con movimiento | (tiempos con la PC cargada: 12,4-18,1 ms; no cuentan) |

El p99 de las ventanas con los últimos 5 s sigue más alto que el de las otras (v1: 5,1-5,3 contra 2,8-3,0 ms): es el trabajo de repintar cada cuadro el halo, la cuenta del paso y la cuenta grande con su aro (que antes, además, asignaba); entra holgado en los 16,7 ms. Las asignaciones que quedan en cada ventana (26-31 cuadros de 1 649) son las de 1 Hz que §20.2 admite: el `Compose` del HUD por segundo y por lectura (`WorkoutHudStore`) y, de paso, `DeviceScreenView.Render` (abajo, "Lo que queda"). **QA tiene que repetir los tiempos del build final** (`-rowmate-perf-long-series 150` y `-rowmate-perf-entries 10`) con la PC sin otra carga.

### V2 · Los menores

| # | Issue | Qué cambió |
|---|---|---|
| V2-M1 | QA-V6a-R2-02 (a) | `MemoryAndSceneCpuPerTierWithinTheBudget`: la CPU de la escena en tres tramos de 120 cuadros y la **mediana de sus tres p95** contra el tope de 1,5 ms (el registro escribe los tres). Alto 1,10 / 1,15 / 1,25 y 1,11 / 1,16 / 1,20 ms (mediana 1,15-1,16), Medio 0,79-0,84, Bajo 0,62-0,71 en las dos corridas completas |
| V2-M2 | QA-V6a-R2-03 (= QA-V6a-05) | DEMO-03 a los **36,2 s** (el muelle a la izquierda, acercándose; el texto del estado dice lo mismo); después la secuencia sigue desde los 39,2 s, así DEMO-04 en adelante no cambian |
| V2-M3 | QA-V6a-R2-04, en parte | La sonda ya no busca el cuadro de cada medida de `FrameTimingManager` por su `frameStartTimestamp` (no servía en este *player*: "sin el cuadro" en todas las entradas): se sacaron esa correspondencia, la línea de los relojes y el "peor" y "con movimiento" que dependían de ella. La línea de la entrada da el máximo de CPU y GPU y el dato del "Main Thread" del `ProfilerRecorder` (el momento, negro y quieto o con movimiento, el GC), que era el que ubicaba el cuadro |

### Lo que queda (desvíos de esta ronda)

| Qué | Por qué | Para quién |
|---|---|---|
| QA-V6a-R2-02 (b): las RT que suma la escena dependen del orden de las pruebas | Contar solo las de la escena pide los intermedios de URP de su cámara (internos de URP: `RTHandle` del *renderer*, que además se reusan entre pruebas). Los topes pasan en los dos órdenes (Medio 41,9-51,2 contra 64 MB) | QA / It. 14 (en el teléfono, con el Memory Profiler) |
| QA-V6a-R2-04, el margen de la primera entrada del proceso: en esta ronda dio 12,7 ms en el *driver* (QA: 16,7-16,8; el Architect en la ronda 1: 12,7), es decir, varía entre corridas | No se tocó el camino del toque: ejercitar por adelantado el JIT de lo que corre por primera vez en ese cuadro pide grabarlo con el Profiler (V2-01 ya lo permite) | 6b (deuda) |
| QA-V6a-R2-05 (la precarga en reposo: unidades de 13-20 ms de la primera vez del código) | Aceptado por QA en la PC; se mide en el teléfono | It. 14 |
| QA-V6a-PAR-02 = QA-V6a-08 (las sondas de la ventana `#FFE082`, la cabaña, el matiz OKLCH y el velo en el *manifest*) | Como en la ronda 1: ubicar la ventana y el frente de la cabaña pide proyectar su malla con la cámara del cuadro. Las mide QA en los PNG | QA / Designer |
| QA-V6a-REN-12 (P-07: el diálogo quieto sigue a 60 fps) | Decisión del PO | PO |
| QA-V6a-REN-13 y -14, en parte (la GPU por pieza; un tope de dibujos de la UI en el *build*) | Como en la ronda 1 | It. 14 / QA |
| Visto de paso con el Profiler: `DeviceScreenView.Render` corre en cada lectura del remo con el inmersivo arriba (≈ 20 asignaciones por lectura, a 1 Hz) | Es de la It. 2 (la pantalla de Dispositivo, debajo del inmersivo); a 1 Hz lo admite §20.2. Se puede saltar mientras no se ve | 6b (deuda) |

### Corridas de esta ronda (artefactos en `<scratch>/it6a/fix-architect-r3/`)

| Suite | Resultado |
|---|---|
| Rojo (antes de corregir): `run-unity-tests.ps1 -Platform PlayMode -Filter HudBudgetTests.LongSeries` | `LongSeriesNoAllocationsPerFrame` **falla**: 242 de 318 cuadros con asignaciones, 1 270 en el panel (≈ 4 por cuadro); `LongSeriesStaysSmooth` pasa (mide solo tiempos). Las corridas intermedias (34 cuadros con el color en el dibujo; 20 con el halo en la barra), en `<scratch>/it6a/fix-architect-r3/iter1` a `iter6` |
| El Profiler del *player* (antes de corregir) | `-rowmate-perf-long-series 32 -rowmate-perf-profile …` y `ProfileAllocations.Dump`: 312 de 313 cuadros con asignaciones; los lugares de la tabla de V2-01 |
| `tools/dotnet/run-tests.ps1 -Matrix` | compilación con **0 avisos y 0 errores**; Core **3049: 3010 pasan, 0 fallan, 39 ignoradas**; matriz: PASA 615, FALLA 0, IGNORADO 4, PENDIENTE UNITY 143, **SIN TEST 0** (QA-V6a-PAR-05 sigue cerrado) |
| `tools/unity/run-unity-tests.ps1` (el código final) | **EditMode** 3137: 3092 / 0 / 45; **EditMode `-Simulator`** 3329: 3284 / 0 / 45; **PlayMode 481 / 481**; **PlayMode `-Simulator` 553 / 553** (la prueba nueva en las dos); 0 inconclusos; la guardia, sin cambios en cada una; ningún aviso nuevo de compilación (los 14 de antes, ninguno en lo tocado). `MemoryAndSceneCpuPerTierWithinTheBudget` en verde en las dos (la mediana de los p95: Alto 1,15-1,16, Medio 0,82-0,83, Bajo 0,62-0,67 ms) |
| `tools/unity/capture-snapshots.ps1`, `it6a` dos corridas (A a `<scratch>`, B al repo) | 45 tomas, 0 con problemas; **A y B iguales byte a byte** salvo `manifest.json` (`durationMs`). Contra lo publicado cambian **solo DEMO-03** (a los 36,2 s, con el muelle a la izquierda), su texto en `INDEX.md` y el *manifest* (el estado y `takenAtSeconds` de DEMO-03; los dibujos acumulados de las tomas siguientes). DEMO-04 en adelante, CAP-22 y VID-08, iguales byte a byte |
| `it4b`, `it5`, `it4a` | `it4a`: **sin cambios** (el *manifest*, solo `durationMs`). `it4b`: cambia **solo CAP-22** (`Serie larga` a los 3 016,6 s, el pulgar de la fila angosta): 29 píxeles, máx 35/255, en el borde del relleno de la en curso dentro del pulgar (6 × 28 px). `it5`: cambian **solo VID-04 y VID-09** (los videos con el halo). Es lo esperado de V2-01 (2): la fila ya no se repinta en cada cuadro por el halo sino por su relleno, cada ¼ px (§3.4 de la 4b): el borde suavizado del relleno queda donde estaba en el último repintado, a menos de ¼ px. Las tomas de la cuenta del paso y de la cuenta grande (CAP-02, -06, -07, -08, -25, -27, DEMO-07, -09 de la 4b; CAP-19 a -21 de la 5), iguales byte a byte |
| `tools/build/build.ps1 -Target Windows -Simulator` | `Builds/Windows/simulator/RowMate.exe` con el código final y `THIRD_PARTY_NOTICES.md` al lado; la guardia, sin cambios. `[PERF]` arriba |

`git -C ../rowmate-legacy status --porcelain` vacío al terminar; el módulo de credenciales del legacy no se leyó. Las pruebas, las capturas y las medidas usaron carpetas de datos temporales (`ROWMATE_DATA_DIR`): ni `prefs.json` ni las bases del usuario; las preferencias de la ventana del *player* (registro de Windows), resguardadas antes y restauradas al final, iguales al resguardo. No se cerró ni se tocó ningún proceso que no lanzó esta tarea. Sin *commits*.
