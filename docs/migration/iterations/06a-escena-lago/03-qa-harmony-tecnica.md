# It. 6a — Escena: Core de escena, cielo, luz, hora, Lago alpino y agua degradada: gate de armonía de QA (lente técnica)

| Campo | Valor |
|---|---|
| Autor | rowmate-qa |
| Fecha | 2026-11-02 |
| Iteración | 6a · `06a-escena-lago` ([ROADMAP §It. 6a](../../ROADMAP.md)). La primera iteración 3D |
| Lente | **Técnica**: cobertura AC → componente → test o captura; Core de escena puro y determinista (dotnet) y oráculo; escena URP con *asset slots* y respaldo procedural sin terceros (el CI compila sin ellos; nada de terceros versionado); render determinista de capturas en *batchmode*; rendimiento (60 fps en Android de gama media: presupuesto y medida; Windows; *frame pacing* en reposo); nada que siga dibujando o animando al salir del inmersivo; 0 B por cuadro; convivencia con UI Toolkit (HUD, velo de pausa, entrada con vuelo, `HudWarmup`) y con el simulador; la deuda de la 5; paquetes disjuntos y contratos; contradicciones spec ↔ diseño ↔ notas; repo público y licencias. El aspecto y el texto de producto los miran las otras lentes |
| Entrada | [01-po-spec](01-po-spec.md) (100 AC, LT-62 a LT-87, CAP-01 a -24, LEG-01 a -07, DEMO-01 a -08, VID-01 a -11, DV-52 a DV-62, P-01 a P-17); [02-tech-design](02-tech-design.md) (DT6a-01 a -15, C-01 a C-19, S6a-1 a -6, R6a-01 a -11); [04-design-notes](04-design-notes.md) (DN6a-01 a -24, §21); [00-deuda-5](00-deuda-5.md); [00-deuda-diseno](00-deuda-diseno.md) (DD6a-01 a -26); [03-qa-harmony-tecnica de la 5](../05-series-parciales/03-qa-harmony-tecnica.md) (formato y lecciones) |
| Legacy (solo lectura, `3ae0f3a`) | [rowmate-legacy](https://github.com/figuibej/rowmate-legacy), enteros: `lib/features/workout/scene/{scene_camera,scene_state,time_of_day,environment,scene_settings,water_shader,scene_view}.dart`, `scene/environments/lake_environment.dart`, `scene/painters/sky_painter.dart`; `painters/water_painter.dart:55-108`; `painters/shore_painter.dart:1-140`; `immersive_workout_screen.dart:60-125`; `workout_screen.dart:36-50`; `workout_provider.dart:205-230`; `core/models/rowing_data.dart` (tipos); `test/scene/{scene_camera,scene_state,time_of_day,environment,sky_shore_painter,water_painter,scene_view}_test.dart` enteros. No se leyó `lib/core/strava/strava_secrets.dart`. `git -C ../rowmate-legacy status --porcelain` vacío antes y después |
| Código leído (este repo) | `Core/AssemblyInfo.cs` (y los de App, Presentation, Scene, Platform), `Core/Common/IClock.cs`, `Core/Design/{FramePacingPolicy,Tokens}.cs`, `Presentation/Motion/{FramePacer,Hud/PauseVeilMotion}.cs`, `Presentation/Workout/{HudWarmup,ImmersiveWorkoutPage}.cs`, `Platform/Time/LocalOffsetSource.cs`, `Editor/Bootstrap/UrpProvisioner.cs`, `Tests/UiSupport/FrameAllocations.cs`, `Tests/PlayMode/HudBudgetTests.cs`, `tools/dotnet/*/*.csproj`, `tools/oracle/README.md`, `tools/unity/capture-snapshots.ps1`, `.gitignore`, `.github/workflows/{ci-core,ci-unity}.yml` |
| Verificación hecha | `dotnet test` (§1); un programa propio de .NET 8 en `<scratch>/it6a/gate-tecnica/calc` (sin red), independiente de los del Architect y del Designer: puerto propio del `math.Random` de la VM de Dart, los segmentos del lago, los visibles a 8 distancias, el vuelo de entrada contra el corte de `visibleProps`, la paleta en 18:00 y la transición de hora; `git check-ignore` de `Assets/ThirdParty/`; el cruce de los ID de la spec contra la matriz de 02-tech-design §6.5; búsqueda de rutas y datos del equipo en los documentos de la 6a. **Unity no se abrió** (la tarea es sin Unity). Sin *commits* |

---

## 0. Veredicto

**APROBADO CON OBSERVACIONES.** **0 bloqueantes, 7 mayores, 14 menores** (§14).

- **Cobertura.** Los 100 AC (101 ID: AC-BLE-16-02 y -03 van en una fila) están en la matriz de 02-tech-design §6.5: 0 faltan y 0 sobran (17 entran por rangos, p. ej. "AC-SCN-14-01 a -06"). Ninguno queda solo con **eq** o **qa** salvo los dos que no pueden tener otra cosa (AC-SCN-01-09, -10). Lo que el Architect declara sin test está justificado (§2.2), con tres sondas automáticas que conviene atar a sus AC.
- **Fidelidad.** Recalculé 24 casos del legacy (§3), entre ellos los vectores dorados del generador con un puerto propio, la primera estrella, los visibles a 0 y 1 234 m (92 y 95) y los umbrales de la noche y de las luces. Todos coinciden con la spec y con el diseño. Hay cuatro precisiones de texto para el PO (QA-G6aT-20).
- **El núcleo técnico está bien armado.** Toda regla es Core puro (`Core/**/*.cs` ya entra en el `.csproj` de dotnet), la cámara usa la matriz que calcula Core (la derivé: la fila 2 vale `2h/H − 1`, −0,20 y −0,16), el mundo va en coordenadas de cinta con las fases en `double`, la escena se dibuja a pedido y se compone en sRGB en el panel (DT6a-05, con sondas y respaldos), y `HudBackdrop` en `display: none` deja el presupuesto del HUD igual (lo confirmé: `CountVisible` cuenta solo `UiQuery.IsShown`).
- **Lo que más pesa:**
  1. Por cuarta iteración seguida, las 04-design-notes se escribieron **después** del diseño técnico y no están integradas. Cambian constantes de WP-A, el contrato de *shaders* de WP-B0 y sondas del arnés (QA-G6aT-01). La spec tiene que absorber lo que decida el gate antes de WP-A (QA-G6aT-02).
  2. La escena demorada se contradice en el propio diseño: el primer dibujo "indivisible" solo se permite en negro o en reposo, pero se agenda después de la cascada con el HUD a la vista (QA-G6aT-03).
  3. Un slot sin asignar "a propósito" (casi todos, sin el pack) avisaría `[SLOT]` en cada arranque: choca con "sin avisos" (AC-SCN-01-05) y con "un aviso" (AC-SCN-15-08) (QA-G6aT-04).
  4. Forzar el nivel y usar copias del asset de URP desde las pruebas y el arnés del Editor puede ensuciar `ProjectSettings` (QA-G6aT-05).
  5. El 0 B por cuadro no dice cómo entra el `Tick` de la escena y su dibujo en lo que mide `FrameAllocations` (QA-G6aT-06).
  6. El precalentado de la escena cubre los *shaders* de un dibujo a las 12 h, pero no el JIT de su camino de C# ni las variantes por hora y por nivel (QA-G6aT-07).

**Qué cerrar antes de cada paquete:**

| Antes de | Issues |
|---|---|
| WP-A | QA-G6aT-01 (integrar las notas), QA-G6aT-02 (el PO reescribe los AC con lo que decida el gate), -15, -19 (el aviso de la licencia, junto con `DartRandom`), -20 |
| WP-B0 | QA-G6aT-05, -08 (colas del cielo en el contrato), -09 (sondas S6a-7), -10 (*Frame Timing Stats*), -14 (orden B0 → D1, `ProjectSettings`, `InternalsVisibleTo`) |
| WP-D1 | QA-G6aT-08, -12, -13 |
| WP-B1 | QA-G6aT-03, -04, -06, -07, -16, -18, -21 |
| WP-B2 | QA-G6aT-10, -11, -17 |
| PO (sin código) | QA-G6aT-02, -20 |

---

## 1. Evidencia corrida

| Qué | Comando | Resultado |
|---|---|---|
| dotnet (línea base, con la deuda de la 5 sin *commit*) | `powershell -NoProfile -File tools/dotnet/run-tests.ps1 -ArtifactsPath <scratch>/it6a/gate-tecnica` | Herramientas **192 / 192**. El resto **2547: 2513 pasan, 0 fallan, 34 ignoradas**: lo mismo que dice [00-deuda-5](00-deuda-5.md) §0 |
| Programa propio (.NET 8) | `dotnet run -c Release` en `<scratch>/it6a/gate-tecnica/calc` | `Random(42)` → `0.15092545597797424`, `0.6041479725361217`, `77`; semilla 55 450 → `0.854835531354132`; primera estrella V390 (58,86; 173,37); visibles a 0 / 1 / 49,5 / 50 / 920 / 1 234 / 5 000 / 42 195 m: 92 / 93 / 94 / 94 / 98 / 95 / 91 / 92 (tope 120); máximo de objetos por segmento en 0 a 2 000: 15 (el *pool* reserva 24); el vuelo: 0 pinos del legacy que dejan `visibleProps` estando a la vista (§3, caso 24); `ambient(18) = 0.85` exacto; 100 000 transiciones a las 18 desde una hora intermedia: todas terminan en 18,0 exacto |
| `git check-ignore -v --no-index` | sobre `Assets/ThirdParty/…/rock.png`, `…/rock.png.meta`, `Assets/ThirdParty/Mixamo/YBot/YBot.fbx` y `Assets/ThirdParty.meta` | Ignorados el `.png` y el `.fbx` (`.gitignore:70`); el `.meta` lo vuelve a incluir `.gitignore:72`; `Assets/ThirdParty.meta` no lo toca ninguna regla. Correcto |
| Cobertura de la matriz | `grep` de los ID en negrita de §4 y de las filas de §5.3 de la spec contra 02-tech-design §6.5 | 101 ID (78 + 11 + 12); todos en la matriz, 17 por rango |
| Repo público | búsqueda de rutas absolutas de unidad, carpetas del perfil, nombres de usuario y marcas de *hardware* en `docs/migration/iterations/06a-escena-lago/*.md` | 0 coincidencias |
| Legacy | `git -C ../rowmate-legacy status --porcelain` | Vacío antes y después |

---

## 2. Cobertura: AC → componente → test

### 2.1 Matriz

- Los 78 AC nuevos, los 11 que cambian (§5.2) y los 12 ID que se vuelven a verificar (§5.3) tienen componente y test o toma en 02-tech-design §6.5. AC-WKT-02-11 es deuda de la 5 (§5.1) y no cuenta, como dice la spec.
- `It06aIds.cs` tiene que listar **101** ID, no 100: AC-BLE-16-02 y -03 son dos (QA-G6aT-20).
- Los 26 LT tienen prueba con nombre y lugar (02-tech-design §6.4). Los nombres siguen el `'$id …'` de Dart (`EnvironmentId.lake 390x844 12.0h …`).
- Los estados de prueba de spec §0.4 tienen su control: distancia (`ForceForTest`), tiempo de escena fijo, hora a un minuto, respiración en 0, fecha (por `IClock.UtcNow`), slot de prueba (copia en memoria). **Falta** el de "escena demorada" (AC-SCN-01-07, CAP-24, VID-11): no está en los controles de §2.15 ni de §3.1 (QA-G6aT-03).

### 2.2 Lo que el Architect declara sin test

| AC | Por qué no tiene test | ¿Justificado? | Comentario |
|---|---|---|---|
| AC-SCN-01-09 (60 fps en Android) | sin teléfono ni módulo | Sí (regla 9, It. 14) | Lo que lo anticipa en el Editor es AC-SCN-01-11 (contadores por nivel). Falta una medida relativa de GPU en la PC con el nivel Medio forzado, para tener una línea de base para la 14 (QA-G6aT-11) |
| AC-SCN-01-10 (`[PERF]` Windows) | medida con argumentos en el build, `[Ignore] WindowsScenePerf` | Sí, como en la 5 | La parte de GPU necesita *Frame Timing Stats* (QA-G6aT-10). La salida no está en ninguna ventana de medida (QA-G6aT-17) |
| AC-WKT-13-15, AC-WKT-14-19 (parte `[PERF]`/Android) | ídem | Sí | El orden y el presupuesto tienen PlayMode (SIM, SBT). La medida por tiempo responde a 00-deuda-5 §5 |
| AC-SCN-11-02 (degradé) | captura contra LEG y QA | Sí, pero se puede hacer mejor | Las sondas de color de 04-design-notes §10.4 (arriba a ΔE OK ≤ 0,05 de `skyTop`; el horizonte; el matiz ± 8°) son exactamente lo que pide el AC. Que `SnapshotEvidenceTests` las afirme y que `It06aIds` las cuente como **cap con sonda** |
| AC-SCN-11-05 (sol y luna) | captura | Sí | DN6a-21 trae una sonda (diámetro con luminancia > 0,9 ≥ 30 dp en Alto): atarla al AC |
| AC-SCN-09-05 (parte "fotográfica") | captura y revisión | Sí | Los valores tienen dotnet (HLT) |
| AC-SCN-15-07 (parte de aspecto) | captura y QA | Sí | "Nada flota" tiene dotnet (PPL) y PlayMode (SPL, rayos al terreno) |
| AC-SCN-23-03 (parte build) | reiniciar el exe | Sí | `SceneSettings.HourOverride` es `int?` en memoria (no hay clave): sumar a PureCodeRules que ninguna clave de `IPreferences` lo nombra |
| AC-SCN-01-04 (parte Android) | primer cuadro en el teléfono | Sí | El ciclo de vida de prueba lo cubre en PlayMode (SLC) |

---

## 3. Fidelidad: casos del legacy recalculados

| # | Legacy | Caso | Recalculado | Spec | Diseño | Resultado |
|---|---|---|---|---|---|---|
| 1 | `scene_camera.dart:7-9`; `scene_camera_test:9` | horizonte y focal | V390 337,6 / 390; H844 163,8 / 390 | AC-SCN-02-01 | `SceneCamera` | Bien |
| 2 | `scene_camera.dart:28-34` | la proyección como matriz | `clip_y = (2f/H)·y_v + (2h/H − 1)·z_v` → −0,20 (40 %) y −0,16 (42 %) | AC-SCN-02-02 | §2.1, `ChaseCameraProjection.Contract` | Bien |
| 3 | `scene_camera.dart:19`, `:29`; `scene_camera_test:30` | detrás de la cámara | `z = 0,2` → `null`; `minZ = 0,5` | AC-SCN-02-03, LT-64 | plano cercano 0,5 | Bien |
| 4 | `scene_camera.dart:12-16` | el punto del remero | TV 460,78 / 914 = 0,5041; V390 427,9 / 844 = **0,507**; H844 254,1 / 390 = 0,652 | "0,504 en vertical" | §8.4 lo señala | Precisión del PO (la spec habla de los tamaños de la app) |
| 5 | `scene_state.dart:38-57`; `scene_state_test:14` | velocidad objetivo e inercia | 500 / 107 = 4,673; el corte a 0 solo con objetivo 0 | AC-SCN-03-01 a -03 | `SceneState.Update` | Bien |
| 6 | `scene_state_test:55` | `shaderDistance` | 5 000 mod 4 096 = 904 | AC-SCN-03-06 | `DartMath.Mod` | Bien |
| 7 | `time_of_day.dart:52-55` | noche ≤ −0,4 | `6 + 12·(1 + asin 0,4/π)` = 19:34:19; `6 − 12·asin 0,4/π` = 4:25:41 | AC-SCN-10-01 | `NightStartHour`, `NightEndHour` | Bien |
| 8 | `time_of_day.dart:57-67` | el astro del legacy | 20:00 luna, x 0,333, elevación 0,5; 5:00 sol, x 0,208, fuerza 0,2; 12:00 `y = 0,1·horizonte` (< 0,2, LT-69) | AC-SCN-10-02, -03 | `LightFor` | Bien |
| 9 | `time_of_day.dart:100-116`; `time_of_day_test:25`, `:32` | paleta y ambiente | ambiente(0) = 0,35 **exacto** (24 h cae entre 22 y 28, los dos noche); 18:30 = 0,7625; 6:00 = 0,825; 15:00 = 0,925 | AC-SCN-09-02, -03 | `PaletteFor` | Bien |
| 10 | `time_of_day.dart:84-88` | las 18:00 exactas | `1,0 + (0,85 − 1,0)·1,0` = 0,85 exacto en `double`: no es "> 0,85", así que hay estrellas (oscuridad 0,23) | AC-SCN-11-04, CAP-03 | `StarField.Visibility` | Bien. Y la transición a las 18 termina en 18,0 exacto (100 000 orígenes intermedios) |
| 11 | `shore_painter.dart:26` | umbral de las luces | ambiente 0,6 a las 19:25:43 y a las 4:42:51 | AC-SCN-09-06 | `NightLights` (19,4286 y 4,7143) | Bien |
| 12 | `sky_painter.dart:51` | las estrellas se van | ambiente 0,85 a las 6,2 h (6:12) | AC-SCN-11-04 | STR "6:13 no" | Bien |
| 13 | `sky_painter.dart:58-64` | la primera estrella | `0.15092545597797424·390` = 58,86; `0.6041479725361217·337,6·0,85` = 173,37 (con el orden de operaciones del legacy) | AC-SCN-11-03 | STR | Bien |
| 14 | `math.Random` (VM de Dart) | vectores dorados | puerto propio (mezcla de Thomas Wang, MWC con `0xFFFFDA61`, cuatro giros, rechazo en `nextInt` no potencia de 2): los cuatro valores, bit a bit | AC-SCN-14-02 | `DartRandom` (§2.10) | Bien |
| 15 | `environment.dart:77`; `lake_environment.dart:35-38` | muelles y cabañas | muelle k = 3, 11, 19 → 175, 575, 975 m; cabaña k = 7, 19 → 360, 960 m; con `E-920`, cabaña a 40 m y muelle a 55 m | AC-SCN-15-03, §0.4 | LAK | Bien |
| 16 | `environment.dart:89-114`; `environment_test:30` | visibles | 92 a 0 m, 95 a 1 234 m (≤ 120), z no creciente | AC-SCN-14-04, -15-04 | LAK "92 a 0 m, 95 a 1 234 m" | Bien |
| 17 | `lake_environment.dart:22-34` | pinos por segmento | máximo 9 por orilla (`z0 + 0` y pasos de 6) → 18 + muelle + cabaña; medido en 0 a 2 000: 15 | AC-SCN-15-02 | *pool* de 24 por segmento | Bien |
| 18 | `shore_painter.dart:48` | niebla de la orilla | relZ 175 → 0,2375 (CAP-07 "≈ 24 %"); relZ 240 → 0,40 | AC-SCN-20-01 | `ShoreShading.Fog` | Bien |
| 19 | `water_painter.dart:92-103` | la línea 0 a 0 m al mediodía | `t = 0`, en el horizonte, medio ancho 0,15·W (0,3 del ancho), alfa 0,05 + 0,1·1·1,0 = 0,15 | AC-SCN-13-03 | `DegradedWater.Line` | Bien |
| 20 | `sky_painter.dart:95` | la deriva de las nubes | `fx + d·0,0004·v + t·0,004·v` | AC-SCN-11-06 (DV-56) | `CloudModel.LegacyX` (contrato) y `X` | Bien |
| 21 | `lake_environment.dart:46-51` | las cordilleras | `hMax = 0,12·alto`; corrimiento `d·0,004·60` = 0,24 dp/m y 0,48 dp/m | AC-SCN-19-01, -02 (DV-55) | `HorizonModel.LegacyProfile` | Bien |
| 22 | `sky_shore_painter_test:39` | LT-82 | el legacy cuenta círculos: a las 12 h 37 (2 del astro + 35 de las nubes), a las 19 h 117 → +80 | LT-82 "80 contra 0" | STR | Bien |
| 23 | `scene_view_test:11` | LT-84 a LT-87 | SPM 24, split 110, 300 m, en marcha, hora forzada, 6 cuadros de 100 ms y el desmontaje | AC-SCN-01-05 | SLC | Bien |
| 24 | `environment.dart:90` (`zMin = d + 1`) | el corte de lo cercano | el legacy saca de la lista lo que tiene relZ < **1 m** (no 0,5); con la cámara del contrato, un objeto de la orilla a relZ 1 cae a miles de dp fuera de la pantalla. Con el vuelo (la cámara 14 m atrás), barrí 220-1 820 ms en TV, TH, L640 y W1280 con la distancia de un R-4 desde el toque (y con 0,5 s de demora): **ningún** pino del segmento 0 sale de la lista estando a la vista. El primer pino izquierdo (z 0,565) nunca entra en la lista, como en el legacy | DV-62 dice 0,5 m | DN6a-11 (terreno desde `d − 25`) | Sin salto. Precisión de texto para el PO (QA-G6aT-20) |

Además: el apartamiento del horizonte con la respiración, analítico, en W1280: `640·tan 0,18° + 720·tan 0,10°` = 2,01 + 1,26 = **3,27 dp**; con 0,15°, 2,93. Confirma DT6a-01.

---

## 4. Core de escena puro y determinista; oráculo

- **Puro y medible fuera de Unity.** `tools/dotnet/RowMate.Core` compila `Core/**/*.cs` y `RowMate.Tests.EditMode` compila `Tests/EditMode/**`: `Core/Scene/**` y `Tests/EditMode/Scene/**` entran solos. `Tests/Support/SceneSeeds.cs` también (`Tests/Support/**`). `IsExternalInit` ya existe en Core (los `init` de `SceneSessionOptions` compilan en Unity).
- **Bit a bit, dónde sí y dónde no.** Lo que se compara bit a bit (el generador, los segmentos, sus resúmenes FNV-1a) es aritmética de `+` y `*` sobre `double`: da lo mismo en .NET 8, Mono e IL2CPP x64 (el riesgo de FMA en ARM64 está en R6a-03). Lo que usa `sin` o `asin` (astro, elevación, respiración, cordilleras) se compara con 1e-9: absorbe la diferencia de último bit entre bibliotecas matemáticas. Bien elegido.
- **El tope con boyas e hitos** (AC-SCN-14-05) se compara contra `visibleProps` corrido en el oráculo. Ojo: `List.sort` de Dart no es estable con más de 32 elementos. Si en el escenario de prueba dos hitos con la misma `z` caen justo en el corte, el legacy puede quedarse con cualquiera de los dos y la prueba sería frágil (QA-G6aT-15).
- **El relleno determinista** (`ShoreFill`): el bosque cercano usa un disco de Poisson de 6 m. Si un segmento mira los puntos del vecino para no superponerse, el resultado depende del orden en que se piden los segmentos. Eso rompería "determinista por distancia, se pidan en el orden que se pidan" (QA-G6aT-15).
- **Los controles de prueba son `internal` en Core** (`SceneState.ForceForTest`, los de `SceneSession`), y los llama `SceneHost` (en `RowMate.Scene`). `Core/AssemblyInfo.cs:4-7` solo da `InternalsVisibleTo` a `Tests.Support`, `Tests.EditMode`, `Tests.Simulator` y `Tests.UnityApiCheck`. El diseño cambia `Scene/AssemblyInfo.cs`, pero no `Core/AssemblyInfo.cs` (QA-G6aT-14).
- **El oráculo es factible.** `tools/oracle/engine/` ya corre `flutter test` dentro de la guardia y escribe con `ROWMATE_ORACLE_OUT`. `time_of_day.dart`, `environment.dart` y los *painters* importan `dart:ui`, así que hace falta Flutter, como dice el diseño. Para LEG-01 a -07, `Picture.toImage` en `flutter_test` necesita `tester.runAsync` (o `TestWidgetsFlutterBinding` con un `await` real): que lo diga la prueba. Los seis JSON entran en el tope de 1 MiB; `scene_light.json` (1 440 × 7 filas) es el más justo, y el respaldo (cada 2 min y los minutos de los cambios) está previsto.
- **La hora local** sale de `IClock.LocalNow`, que en Platform ya pasa por `LocalOffsetSource` (sin la caché de `TimeZoneInfo`, AC-WKT-19-04): el cambio de zona de AC-SCN-09-01 llega en el minuto siguiente. Bien.
- **La licencia del puerto de `math.Random`** está en QA-G6aT-19 (§13).

---

## 5. Escena URP, *slots*, respaldo procedural y CI

- **La composición (DT6a-05)** es la decisión correcta: es la única que conserva el vidrio sRGB de ADR 019 (`#2A2F37` sobre blanco) y deja el arnés leyendo la misma RT. Las sondas S6a-1 a -6 tienen respaldo. Faltan dos comprobaciones de lo que el diseño afirma (QA-G6aT-09):
  - "**cero cuadros de atraso**" y el repintado del panel en **cada** cuadro, aunque el HUD no cambie;
  - con el respaldo de S6a-5 (la RT al tamaño escalado), el filtro de punto de `scene-surface` se vería a bloques.
- **`.gitignore`** ya tiene `/Assets/ThirdParty/**` con la excepción de los `.meta` (`.gitignore:66-72`). Lo verifiqué con `git check-ignore` (§1).
- **El CI.** `ci-core.yml` (dotnet, Ubuntu y Windows) compila y prueba Core sin nada de Unity, y por lo tanto sin el pack. `ci-unity.yml` es opcional y a pedido: corre con GameCI en Ubuntu. Ahí las pruebas con `SceneMode.Real` corren con OpenGL por software (o sin dispositivo), los *shaders* tienen que compilar para GLCore y Vulkan, y los topes de tiempo (CPU de la escena ≤ 1,5 ms) pueden fallar por la máquina. Hoy nada de eso está previsto. Además, "el CI pasa sin el pack" no tiene una prueba concreta (QA-G6aT-18).
- **La semántica de `[SLOT]`** (QA-G6aT-04, mayor). Hoy la regla dice: "asignado y válido → se usa; si falta o no valida, el procedural y un aviso por slot y por proceso" (02-tech-design:790; ASSET_SLOTS §0). Sin el pack, y con pinos, cabañas, muelles, cielo, agua y bote procedurales "a propósito" (02-tech-design:791), casi todos los slots de la escena están sin asignar. Así, la app escribiría unos 20 `[SLOT]` en cada arranque. Eso contradice "se cierra sin excepciones ni avisos" (AC-SCN-01-05, LT-84 a -87), "tres entrenamientos sin avisos" y el "**un** aviso" de AC-SCN-15-08. Además, la prueba que mire avisos depende de cuál prueba arma la escena primero en el proceso.
- **El estado global del Editor** (QA-G6aT-05, mayor):
  - "Las pruebas fuerzan el nivel" (02-tech-design:850): con `QualitySettings.SetQualityLevel`, el nivel actual del Editor queda cambiado después de las pruebas;
  - las "copias en ejecución" de los assets de URP (:851) solo se usan si se asignan con `QualitySettings.renderPipeline` o `GraphicsSettings.defaultRenderPipeline`. Son los mismos *setters* que `UrpProvisioner.cs:53-64` usa justamente para **persistir** la configuración;
  - en el Editor, una copia que no es asset quedaría referenciada desde `ProjectSettings` y se perdería al guardar.

  Es el mismo tipo de problema que el *define* del simulador, que los *scripts* ya vigilan.
- **El orden B0 → D1** (QA-G6aT-14). WP-B0 crea `Art/Materials/Scene/*.mat` (`SceneAssetsBuilder`) y su `SceneAssetsTests` afirma "los *shaders* de §7.5 encontrados", pero los *shaders* llegan en WP-D1. Tampoco aparecen en ningún paquete los cambios de `ProjectSettings` que hace el *bootstrapper*: los tres niveles, la capa `RowMateScene` y *Frame Timing Stats*.

---

## 6. Render determinista de capturas en *batchmode*

Lo previsto está bien:

- el Editor ya corre el arnés en *batchmode* sin `-nographics` (`capture-snapshots.ps1:126`, `:163`);
- compilación sincrónica;
- sin `_Time` (todo por `_RmSceneTime` y las fases de Core, también el viento en el *vertex shader*);
- tramados de Bayer fijos;
- sin TAA;
- la escena avanza en pasos de 1/60 s;
- dos corridas byte a byte, con la tolerancia acotada y justificada toma por toma.

Falta sumar al contrato (QA-G6aT-12):

- **El *dithering* y el *film grain* de URP, apagados.** El *dithering* de la cámara de URP rota su textura de ruido en cada cuadro. Si el Designer lo prende contra el bandeado del cielo de noche (de `#050A1E` a `#121C3A` hay unos 13 niveles por canal en 337 dp), las corridas dejan de ser iguales. El bandeado se corrige con Bayer fijo en el *shader* `Sky`.
- **`ShaderUtil.allowAsyncCompilation = false` también en las pruebas PlayMode que leen píxeles** (SPL, las sondas de SBT), no solo en el arnés. Si no, el primer dibujo sale con el color de "compilando".
- **`HideForCapture`** oculta "el HUD" (02-tech-design:753), pero `scene-surface` es el primer hijo de ese HUD (:713, :749). Tiene que ocultar los hermanos y dejar visible la superficie.
- **La sonda de la ventana encendida** (`#FFE082 ± 3`, 04-design-notes:671) no se sostiene con el post de la noche (balance −25, saturación −20, exposición +0,6, *Neutral*; :642), aunque se apague el *bloom* (QA-G6aT-13).
- **LEG-01 a -07** salen del rasterizador de `flutter_test`: deterministas corrida a corrida, sin metadatos con rutas.

---

## 7. Rendimiento: presupuesto, medida y *frame pacing*

- **El presupuesto es concreto.** Hay *draw calls*, SetPass, triángulos, texturas, GPU y CPU por pieza, en las dos tablas (02-tech-design §4.1, 04-design-notes §15.1), contra los topes de ARCHITECTURE §20.1 y §20.2. Se mide en el Editor por nivel (`SceneBudgetTests`: `Batches`, `SetPass`, `Triangles`, `Texture.currentTextureMemory`, el marcador `RowMate.Scene.Tick`) y en la PC con `[PERF]` por tiempo. En el teléfono queda NO VERIFICABLE. Hay huecos (QA-G6aT-11):
  - **Las dos tablas no coinciden.** Dicen ≈ 82 contra ≈ 83 *draw calls*, ≤ 160 k contra ≈ 135 k triángulos, y ≤ 12 MB contra 0,6 MB de contenido más ≈ 20 MB de RT. Tiene que quedar una sola tabla.
  - **Falta la composición.** El camino de DT6a-05 suma pasadas de pantalla completa que ninguna fila cuenta: el último pase a la RT de la escena, el panel que muestrea la escena entera dentro de la RT de la UI y la copia del presentador. También faltan las RT: la de la escena, la intermedia HDR de URP, MSAA, profundidad, la cadena de *bloom* y la de la UI. En un 1080 × 2400, eso es del orden de 40-55 MB, no "≈ 20 MB".
  - **Los ms de GPU son estimados y no se miden en ninguna parte.** Una corrida de `-rowmate-perf-scene` en la PC con el nivel Medio forzado daría una línea de base relativa para la It. 14.
  - **El sobre-dibujo ≤ 2,5×** (DN6a-16) no tiene cómo medirse en una prueba PlayMode tal como está escrito.
  - **La profundidad en GLES** (nivel Bajo, sin Z invertida) también va en QA-G6aT-21.
- **Escena demorada** (QA-G6aT-03, mayor). §2.16 dice: "los pasos indivisibles (el primer dibujo, que compila) solo con *negro y quieto* o *reposo quieto*". §3.9 dice: "el primer dibujo (indivisible) espera a que termine la cascada". Después de la cascada no hay ni negro ni reposo: o la escena demorada no aparece nunca en la sesión (AC-SCN-01-07 falla), o su primer dibujo tironea con el HUD a la vista, que es lo que prohíbe AC-WKT-13-15 ("cargar o preparar la escena solo puede tardar con la pantalla entera en negro y quieta, o mientras se ve Workout en reposo"). Unity 6 tiene `ShaderVariantCollection.WarmUpProgressively` (y `GraphicsStateCollection`) para calentar en tramos.
- **El precalentado** (QA-G6aT-07, mayor). El paso (6) del armado (02-tech-design:759) es "el primer dibujo con la pose del contrato a las 12 h", que compila. La lección de la 5 es que la primera vez del código cuesta: sin `HudWarmup`, 37-39 ms en el build (00-deuda-5 §1.2). Del camino de la escena falta:
  - el JIT de Mono de `SceneSession.Tick`, `SceneDirector`, los *streamers*, el instanciado y el vuelo;
  - las variantes que aparecen después: de noche sin sombras en Medio, el velo, `RM_WATER_EXTRAS`, *bloom* por nivel;
  - quién genera y mantiene la `ShaderVariantCollection` que nombra el diseño.
- **La adaptación térmica** (QA-G6aT-10). Con el diálogo de completado quieto (P-07), la escena sigue abierta a 30 fps. Si `ThermalAdapter` mide el intervalo, o un tiempo de CPU que incluye la espera, ve 33 ms "sostenidos" y baja la resolución al final de cada rutina. El diseño no dice cuándo mide. Además, ARCHITECTURE §20.3 pide *Frame Timing Stats* activado, y ningún paquete lo activa ni lo prueba.
- ***Frame pacing* en reposo.** Bien:
  - `FramePacingPolicy` y `FramePacer` ya dan 30 fps en reposo y 60 con demandas, y el inmersivo pide 60 (`ImmersiveWorkoutPage.cs:612-640`);
  - la precarga usa tramos en cuadros sin movimiento y no pide 60;
  - después de `Ready` nada dibuja;
  - P-07 suelta los 60 con el diálogo quieto.

  El único roce: el dibujo de calentamiento de la precarga ocurre en Workout en reposo. Choca con DEMO-01 ("0 dibujos de la escena") y con el texto de AC-SCN-01-06 ("sin dibujarse"). Hay que definir que es un solo dibujo fuera de pantalla y contar los de DEMO-01 después de `Ready` (QA-G6aT-17).
- **`[PERF]` de la salida** (QA-G6aT-17). AC-WKT-13-15 incluye "tampoco la salida y la descarga de la escena hacen tironear la vuelta de Workout". `-rowmate-perf-entries` mide los primeros 2,5 s desde el toque, pero ninguna ventana mide de 0 a 1 s desde la confirmación de `Terminar`.

---

## 8. Nada animando al salir; 0 B por cuadro

- **Al salir** está bien cubierto: `Close` a los 400 ms; el `Tick` no hace nada después; `scene-root` inactivo; la prueba de 120 cuadros con 0 dibujos, 0 `RenderMeshInstanced` y ningún `IMotion` de la escena (02-tech-design:817). La sesión se descarta y con ella los fundidos de las luces y de las estrellas. Lo que queda abierto: `ThermalAdapter` midiendo fuera del inmersivo (QA-G6aT-10) y las suscripciones del HUD de `HudWarmup` (§9).
- **0 B por cuadro** (QA-G6aT-06, mayor). `FrameAllocations` mide el `GC.Alloc` del hilo principal solo dentro de `Step(acción)` y de los dibujos observados (`FrameAllocations.cs:11-19`, `:83-122`). El diseño pone el `Tick` de la escena y el pedido de render en `AppRoot.LateUpdate` / `ShellHandle.Tick` del arnés (02-tech-design:677, :714), y no dice que corran dentro de lo medido. Tampoco hay un control positivo en ese camino: el de §4.4 es el del repintado del HUD. Sin eso, AC-SCN-01-12 puede pasar sin medir la escena. Si el respaldo de S6a-3 es la cámara habilitada, el dibujo queda **fuera** de la medida.

---

## 9. Convivencia con UI Toolkit y con el simulador

- **El presupuesto del HUD** (AC-WKT-18-21): `scene-surface` reemplaza al fondo provisorio, que pasa a `display: none`, y `HudBudgetTests.CountVisible` cuenta solo lo visible (`HudBudgetTests.cs:19-31`). Neto 0, y +1 durante el fundido de 0,6 s (≤ 59 con el corte de la fila de 200). Bien.
- **El velo de pausa**: `PauseVeilMotion` da 0 → 0,15 en 300 ms y vuelve en 240 (comentario del código); el peso `alfa/0,15` en el mismo `Volume` de la hora (DT6a-10). La UI no pasa por el post. Bien.
- **La entrada con el vuelo**: el negro de 0-220 se conserva y el HUD se arma en el primer cuadro ≥ 220, sin dibujar la escena (02-tech-design:806). Responde al punto 3 de 00-deuda-5 §5. Bien.
- **`HudWarmup`** (QA-G6aT-16): arma dos `HudView` en el arranque y los desarma en el cuadro siguiente (`HudWarmup.cs:13-21`). Desde la 6a, cada `HudView` arma `SceneSurface`, que se suscribe a `IImmersiveScene.OutputChanged`. El diseño no dice que el precalentado no llame `Preload` ni `Open`, ni que la superficie se desuscriba al desarmarse. Una suscripción que queda es un aviso de "objeto que ya no existe" en la salida (AC-WKT-13-09).
- **El simulador**: `Hora escena` sigue escribiendo `SceneSettings.HourOverride` (`int?`, sin persistir) y la escena lo lee en cada cuadro. `Simular desconexión` y `Remando` apagado salen de la última lectura de la foto del motor. `SceneHourSimulatorTests` cubre AC-SCN-23-01, -02, -04, AC-BLE-16-02, -03, -07, AC-BLE-15-09 y AC-WKT-19-03. Bien.

---

## 10. La deuda de la 5 (00-deuda-5)

| Punto | Estado en 00-deuda-5 | Verificado acá | Resultado |
|---|---|---|---|
| AC-WKT-13-15 `[PERF]` (tres entradas) | PASA: peor cuadro 14,7 ms en la primera ventana; con el build nuevo, 11,6 ms | La tabla y el método (la ventana de 10 s que incluye el armado del HUD) son coherentes. No se repitió (sin Unity) | Cerrado |
| AC-WKT-18-21 `[PERF]` (paneles) | PASA: peor 25,0 ms, p99 ≤ 1,4 ms | ídem. El cuadro de 24-25 ms queda explicado (la primera entrada directa) y queda para la 6a | Cerrado |
| Prueba a mano de §R2.6 de la 5 | PASA (once pasos) | La lista de §R2.6 (mouse y teclado, reinicio con los paneles, desconexión, la cuenta grande, el sello, `PAUSA`, matar el proceso) está toda. La rueda no se probó a mano, pero no estaba en §R2.6 y lo dice §1.4 | Cerrado |
| O-3, O-4, O-5, la capa de *hover*, el 198 | Cerrados | `CharStrings.Of` en `DrawnText.cs:302` y `TabularNumber.cs:952`; el mensaje del 198 en los tres *scripts*; dotnet 2547 / 2513 / 34 igual que el documento | Cerrados |
| P-17 / AC-WKT-02-11 | Pendiente de la demo (c) del usuario | Explícito en §2 y en spec §5.1 | Pendiente explícito |
| QA-V5-R2-01 | Del Designer | Decidido, opción (a), en [00-deuda-diseno](00-deuda-diseno.md) §1, con las notas de la 5 y ANIMATION_BIBLE §2.10 alineadas | Cerrado |
| §5 "para el diseño de la 6a" (tres pedidos) | — | Medir por tiempo (`-rowmate-perf-entries`, 2,5 s); el camino del usuario para la rutina (§4.5); el armado del HUD sigue en el negro (§3.9) | Atendidos |

---

## 11. Paquetes disjuntos y contratos

- **El orden en serie** (A → B0 → D1 → B1 → B2 → B3 → D2) evita choques de archivos. No encontré archivos de dos paquetes salvo las excepciones declaradas (`Core/Scene/Art/**`, `MotionSpecs.cs`, `PauseVeilMotion.cs`).
- **Faltan dueños** (QA-G6aT-14) para:
  - `Core/AssemblyInfo.cs` (`InternalsVisibleTo` de `RowMate.Scene`);
  - los `ProjectSettings/*.asset` que genera el *bootstrapper* (niveles, capa, *Frame Timing Stats*);
  - el orden de los materiales y de la prueba de los *shaders* (B0) contra los *shaders* (D1).
- **El contrato de *shaders*** (02-tech-design §7.5) no tiene lo que 04-design-notes §14.2 suma. B0 lo necesita en `SceneShaderIds` **antes** de D1 (QA-G6aT-01):
  - `_RmMoonTex`, `_RmDiskRadius`, `_RmHaloRadius`, `_RmGlowParams`, `_RmStarField`, `_RmCloudAtlas`, `_RmCloudLayer`;
  - `_RmHorizonScale`, `_RmSnowParams`, `_RmNoiseTex`, `_RmZonePhase`, `_RmLayerAlbedo`, `_RmLayerNormal`, `RM_TERRAIN_LAYERS`;
  - `_RmPlankParams`, `_RmWindowDay`, `_RmWindowNight`, `_RmPalettes`, `_RmGrass*`, `_RmReedColors`, `_RmHullColors`, `_RmGelcoat`;
  - `_RmShadow*`, `_RmScrim*`, `_RmRim`;
  - y `_RmShimmer` con `vis` y el color aparte.

  Las propiedades por instancia van con `UNITY_INSTANCING_BUFFER` y `#pragma multi_compile_instancing` (con `RenderMeshInstanced`). El SRP Batcher queda para lo que no se instancia.
- **El contrato de motion** (§7.4) está completo y coincide con las notas: `SceneFadeMotion` 0,6 s; `HudReflowMotion` 200 ms solo en cambio de clase; `PauseVeilMotion`.

---

## 12. Contradicciones spec ↔ diseño técnico ↔ notas

| # | Tema | Spec | 02-tech-design | 04-design-notes | Efecto técnico | Issue |
|---|---|---|---|---|---|---|
| 1 | Rolido | 0,18° | 0,15° (DT6a-01) | 0,15° | Barrido de dotnet; VID-10 | -02 |
| 2 | Arco del sol | 15,5° (17° al mediodía) | 15,5° por defecto; recomienda 12° | 10,5° (12°) | `CelestialSpec`; AC-SCN-10-04; CAP-04, -10, -11 | -02 |
| 3 | Tope de velocidad del astro | 0,2°/min | 0,35°/min (DT6a-02) | 0,3°/min | **Tres números distintos** para la misma prueba de CEL | -01, -02 |
| 4 | Luz en el cambio de astro | 0,05 | 0,05; recomienda 0 | 0 (DN6a-04) | CEL | -02 |
| 5 | Nieve | 75 % | 75 % por defecto, "55 % en R6a-08" (:500) | 58 % ± 0,03 con ruido | `HorizonSpec.SnowThreshold` | -01, -02 |
| 6 | Alto de las cordilleras | 30 % del cielo en las dos orientaciones | recomienda los 9° fijos de la base | 30 % con tope de 9° | HZN | -01, -02 |
| 7 | Toma aérea | no pide nivelada | "nivelada" (:252); CAP-22 sin cabeceo (:910) | cabeceo −4° y respiración a 0 (DN6a-07) | CCM y la sonda de CAP-22 | -01 |
| 8 | Vuelo | 1,6 s, curva suave | `ease.move`, FOV de 50° al empezar (:244-248) | `ease-in-out-sine`, focal ×1,25 (DN6a-08) | CCM (velocidad máxima ≤ 17,5 m/s) | -01 |
| 9 | Fin del vuelo | "sin salto en el último cuadro" | `u = 1` da el contrato; la respiración empieza en el cuadro siguiente | rampa de 0,6 s (DN6a-20, P0) | Sin la rampa, un salto de ≈ 0,1° de cabeceo (0,7 dp en TV) a los 1 820 ms | -01 |
| 10 | Minuto de la hora real | en el acto | en el acto (`SceneHourTransition`) | 1 s lineal (DN6a-10, P2) | SHR | -01 |
| 11 | Estrellas al umbral | aparecen a las 18:00 | — | fundido de 0,9 s (DN6a-05) | STR; `ForceHourForTest` las pone en su valor final | -01, -02 |
| 12 | Líneas del agua | alfa del legacy | alfa del legacy | × la visibilidad del astro (DN6a-06) | `_RmShimmer.w`; DWT no cambia | -01, -02 |
| 13 | Costo del agua | ≤ 12 ALU | ≤ 24 (DT6a-07) | ≤ 24 | `RM_WATER_EXTRAS` | -02 |
| 14 | Nubes (C-05) | `x`, `y` del legacy | recomienda la banda del Designer en vertical | acepta la spec | La recomendación de 02-tech-design quedó vieja | -01 |
| 15 | Orden del cielo | degradé → estrellas → astro → nubes → silueta | el disco va en `Sky` (cola 1000) y las estrellas, aditivas, en 1001 | "tapadas por la luna y el halo" con ≤ 4 ALU | Estrellas encima de la luna; SLC "orden de colas" | -08 |
| 16 | Contraste de las píldoras | borde de la hueca ≥ 3:1 | excepción contada (2,93 a 3,02) | excepción contada | HCT no puede afirmar el texto de la AC | -20 |
| 17 | La escena en Workout | "sin dibujarse" | un dibujo de calentamiento en reposo | — | DEMO-01 "0 dibujos" | -17, -20 |
| 18 | Presupuesto | topes de §20 | ≈ 82 DC, ≤ 160 k, ≤ 12 MB | ≈ 83 DC, ≈ 135 k, 0,6 MB + ≈ 20 MB de RT | Dos tablas | -11 |
| 19 | Cantidad de AC | "100" | "los 100 AC" en `It06aIds` | — | Son 101 ID | -20 |

---

## 13. Regla de repo público y licencias

- **Los documentos de la 6a** (spec, diseño, notas, las dos deudas): sin rutas de la PC, nombres de usuario ni datos del equipo (§1). El legacy va por su URL o `../rowmate-legacy`; el *scratch*, como `<scratch>`. Este informe sigue la misma regla.
- **Las tomas.** `ItSixAToolingTests` prohíbe rutas absolutas en `snapshots/**` (`manifest.json`, `INDEX.md`). LEG-01 a -07 salen de `toByteData(png)`, sin metadatos.
- **Terceros.** Los archivos de Mixamo y del Asset Store no se versionan: lo verifiqué con `git check-ignore` (§1). Sus licencias permiten usarlos en la app, no redistribuirlos sueltos. D-ART-08, por defecto no: sin texturas CC0 ni la luna de la NASA. La luna es procedural (`sky.moon`).
- **El puerto de `math.Random` de la VM de Dart** (QA-G6aT-19). El código es BSD-3 (Dart SDK) y el repo es MIT: son compatibles. La atribución en el encabezado cubre la fuente, pero BSD-3 también pide reproducir el aviso en la documentación o en los materiales de la **distribución binaria**: los builds de Windows y de Android. Hoy el repo no tiene un `THIRD_PARTY_NOTICES`.

---

## 14. Issues

| ID | Sev. | Resp. | Issue | Evidencia | Corrección sugerida |
|---|---|---|---|---|---|
| QA-G6aT-01 | mayor | architect | 02-tech-design no integra las decisiones finales de 04-design-notes (DN6a-01 a -24) y su contrato de *shaders* (§7.5) no tiene lo que suma §14.2. WP-A arrancaría con constantes y expectativas viejas, y WP-B0 con un `SceneShaderIds` incompleto antes de D1. Es la cuarta iteración seguida (QA-G5T-01) | §12, filas 3, 5 a 12 y 14; `02-tech-design.md:244-252`, `:500`, `:910`, `:914`, `:1165-1167`, `:1186`, `:1211`; `04-design-notes.md:1079-1102`, `:857-873` | Antes de WP-A, una sección "Cambios por notas del Designer" que adopte o rechace con motivo cada DN6a y deje **un** valor por constante: el tope por minuto (0,3 con el arco de 10,5°, medido 0,257), la nieve 0,58, el alto con tope de 9°, la toma aérea con −4° y la respiración a 0, el vuelo con `ease-in-out-sine` y la focal ×1,25, la rampa de 0,6 s (DN6a-20), el minuto real, el fundido de las estrellas y las líneas × `vis`. Además: §7.5 con todas las propiedades y palabras clave de §14.2 (las de instancia con `UNITY_INSTANCING_BUFFER`); las sondas de §5.2 al día (CAP-22 con el horizonte en 336,9; VID-01 con la rampa); C-05 corregida |
| QA-G6aT-02 | mayor | po | Los números de la spec son las expectativas de las pruebas de dotnet de WP-A, y el gate los cambia: el rolido, el arco y sus ejemplos, el tope por minuto, la luz en el cambio, la nieve, el alto de las cordilleras, el fundido de las estrellas, el disco del sol, las líneas × `vis` y el costo del agua | §12, filas 1 a 6 y 11 a 13; `04-design-notes.md` §21.1 | En el gate, decidir y reescribir AC-SCN-02-04, -10-04, -10-05, -11-04, -11-05, -13-03, -13-04 y -19-01, y CAP-01, -03, -04, -10, -11 y -22, con un solo número por constante, antes de WP-A |
| QA-G6aT-03 | mayor | architect | La escena demorada se contradice. §2.16 permite el primer dibujo (indivisible, compila) solo con "negro y quieto" o "reposo quieto". §3.9 lo hace después de la cascada, con el HUD a la vista. O la escena no aparece en toda la sesión (falla AC-SCN-01-07) o tironea donde AC-WKT-13-15 lo prohíbe. Tampoco existe el control de prueba "escena demorada" (§0.4, CAP-24, VID-11) | `02-tech-design.md:659`, `:810`; `01-po-spec.md:291`, `:370`; `02-tech-design.md:643` (controles) | Hacer divisible el primer dibujo: en el *player*, `ShaderVariantCollection.WarmUpProgressively` (o `GraphicsStateCollection`) en tramos ≤ 2 ms; en el Editor, la compilación asíncrona (salvo en el arnés, que la apaga y puede esperar). Que SWP lo pruebe y SLC mida el peor cuadro de la escena demorada. Sumar `SceneHost.DelayReadinessForTest(segundos)` (internal). El PO precisa AC-WKT-13-15: después de la cascada, la escena demorada se prepara solo en tramos |
| QA-G6aT-04 | mayor | architect | Con la regla de `[SLOT]` de hoy, cada slot sin asignar "a propósito" (sin el pack, casi todos: pinos, cabañas, muelles, cielo, agua, bote, capas, vegetación) avisa en cada proceso. Son unos 20 avisos por arranque, y chocan con "sin avisos" de AC-SCN-01-05 y LT-84 a -87, con "tres entrenamientos sin avisos" y con el "**un** aviso" de AC-SCN-15-08. La prueba que mire avisos depende del orden de las pruebas | `02-tech-design.md:790-791`; `01-po-spec.md:269`, `:289`; ASSET_SLOTS §0 | El procedural es el valor **asignado** por defecto: un *provider* procedural en cada `.asset` de `Slots/`, válido. `[SLOT]` solo para un asset asignado que falta o no valida (también una referencia a `Assets/ThirdParty/` que no está en el disco). SSL suma "sin asignar o procedural → 0 avisos", y `SlotValidator` lista los procedurales sin avisar. Con eso, AC-SCN-01-05 se prueba sin filtrar el registro |
| QA-G6aT-05 | mayor | architect | Las pruebas y el arnés del Editor van a cambiar estado global que vive en `ProjectSettings`. "Las pruebas fuerzan el nivel" (con `SetQualityLevel`, el nivel actual del Editor queda cambiado). Las "copias en ejecución" del asset de URP solo se usan si se asignan con `QualitySettings.renderPipeline` o `GraphicsSettings.defaultRenderPipeline`: los mismos *setters* con los que `UrpProvisioner` **persiste** la configuración. Una copia que no es asset quedaría referenciada desde `ProjectSettings` y se perdería al guardar | `02-tech-design.md:850-852`, `:882`; `Editor/Bootstrap/UrpProvisioner.cs:53-64`; 00-deuda-5 §4 ("ProjectSettings sin cambios") | En el Editor, nunca asignar copias en ejecución a la configuración del proyecto. Las pruebas fuerzan el nivel con un *override* que lee `SceneHost` (perfil y ajustes por cámara) o lo restauran en `TearDown`, con una prueba que lo afirme. `run-unity-tests.ps1` y `capture-snapshots.ps1` fallan si `git status --porcelain ProjectSettings Assets/RowMate/Settings` cambió. `SceneAssetsTests` afirma que cada nivel sigue con su asset del proyecto |
| QA-G6aT-06 | mayor | architect | El 0 B por cuadro de la escena no dice cómo entra el `Tick` (y el pedido de render) en lo que mide `FrameAllocations`, que solo ve `Step(acción)` y los dibujos observados. El diseño pone el `Tick` en `AppRoot.LateUpdate` / `ShellHandle.Tick`. No hay control positivo en el camino de la escena, y el respaldo de S6a-3 (la cámara habilitada) deja el dibujo fuera de la medida. AC-SCN-01-12 podría pasar sin medir la escena | `Tests/UiSupport/FrameAllocations.cs:11-19`, `:83-122`; `02-tech-design.md:677`, `:714`, `:723`, `:858-861` | Un observador en `SceneHost.Tick` (como `DrawObserver`: `BeginDraw` / `EndDraw` alrededor del `Tick` y del pedido), con su control positivo en la misma corrida (un `byte[64]` en el `Tick`, 120 de 120). S6a-3 informa por separado lo nuestro y lo interno de URP, por marcadores. Si URP asigna dentro del pedido solo en el Editor, esa parte se mide en el build de desarrollo (`-rowmate-perf-scene` con el contador de `GC.Alloc`) y queda escrito |
| QA-G6aT-07 | mayor | architect | El precalentado de la escena cubre los *shaders* de un dibujo a las 12 h. No cubre la primera vez del C# (JIT de Mono de `SceneSession.Tick`, `SceneDirector`, *streamers*, instanciado, vuelo) ni las variantes que aparecen después: la noche sin sombras en Medio, el velo, `RM_WATER_EXTRAS`, *bloom* por nivel. Tampoco dice quién genera la `ShaderVariantCollection`. Sin `HudWarmup`, el HUD costaba 37-39 ms en la primera entrada | `02-tech-design.md:759`; R6a-05; 00-deuda-5 §1.2 | El armado corre, fuera de pantalla, un `Tick` sintético completo (sesión, director, *streamers*, instanciado, un paso de vuelo) a las 12 h y a las 22 h con el velo, en tramos. Las palabras clave quedan constantes entre horas (fuerza de sombra 0 en lugar de apagar el mapa). La colección de variantes la genera el *bootstrapper* (o `GraphicsStateCollection`) con su prueba. `[PERF]` de la primera entrada del proceso con la escena |
| QA-G6aT-08 | menor | architect | El disco y el halo van en `Sky` (cola 1000) y las estrellas aditivas en 1001: se dibujan **encima** de la luna, también sobre su lado oscuro. Contradice AC-SCN-11-01 y la prueba "orden de colas" de SLC. El Designer espera que las tape la luna con ≤ 4 ALU | `02-tech-design.md:772-773`, `:1168`; `04-design-notes.md:288`, `:862`; `01-po-spec.md:204` | El disco y el halo en una pasada propia después de las estrellas (estrellas 1001, astro 1002, nubes 1003, horizonte 1004), o `Stars` que se enmascara con `_RmDiskDir` (y el costo al día). SLC afirma el orden |
| QA-G6aT-09 | menor | architect | Dos afirmaciones de la composición no tienen sonda: "cero cuadros de atraso" entre la escena y el HUD, y que el panel vuelva a mostrar la RT de la escena en cada cuadro aunque el HUD no cambie. Con el respaldo de S6a-5 (RT escalada), el filtro de punto de `scene-surface` se vería a bloques. SPR con `WorldToScreenPoint` usa el tamaño de la pantalla, no el de la RT | `02-tech-design.md:713-714`, `:725`, `:1000` | S6a-7: un contador de cuadros pintado en la RT de la escena y leído en la RT de la UI en el mismo cuadro, también en cuadros sin cambios del HUD. Filtro bilineal si la RT se escala. SPR con `WorldToViewportPoint` × el tamaño de la RT |
| QA-G6aT-10 | menor | architect | `ThermalAdapter` no dice cuándo mide. Con el diálogo quieto (P-07) la escena sigue abierta a 30 fps: si mide el intervalo, o una CPU que incluye la espera, ve 33 ms sostenidos y baja la resolución al final de cada rutina. *Frame Timing Stats* (ARCHITECTURE §20.3) no lo activa ningún paquete | `02-tech-design.md:852`, `:816`; ARCHITECTURE §20.3 | Medir solo con la escena dibujándose y con el tiempo de trabajo (GPU, o hilo principal + *render thread*), suspendido con `renderFrameInterval` 2 y fuera del inmersivo. THA y SAD con una serie a 33 ms de intervalo que no dispara. El *bootstrapper* activa `PlayerSettings.enableFrameTimingStats` y `SceneAssetsTests` lo afirma |
| QA-G6aT-11 | menor | architect | El presupuesto de 60 fps tiene huecos (§7): dos tablas que no coinciden; falta el costo de la composición (tres pasadas de pantalla completa) y de las RT (40-55 MB en 1080 × 2400, no ≈ 20); los ms de GPU no se miden en ninguna parte; el sobre-dibujo ≤ 2,5× (DN6a-16) no tiene método | `02-tech-design.md:825-841`; `04-design-notes.md:886-903`, `:1094` | Una sola tabla, con la fila de la composición y la memoria de las RT. Una corrida `[PERF]` con el nivel Medio forzado en la PC que guarde los ms de GPU (línea de base para la It. 14). El sobre-dibujo con el modo *Overdraw* del *Rendering Debugger* de URP a una RT, o pasarlo a revisión de QA |
| QA-G6aT-12 | menor | architect | Faltan reglas en el contrato del render determinista: el *dithering* y el *film grain* de URP apagados (el *dithering* rota su ruido por cuadro); el bandeado del cielo de noche, solo con Bayer fijo; `allowAsyncCompilation = false` también en las pruebas PlayMode que leen píxeles. `HideForCapture` oculta "el HUD", pero `scene-surface` es su primer hijo | `02-tech-design.md:753`, `:713`, `:749`, `:883` | Sumarlas a §5.1 y a `SceneAssetsTests` (la cámara de la escena con `dithering = false`). `HideForCapture` oculta los hermanos de `scene-surface`. `UiTestHost` con `SceneMode.Real` apaga la compilación asíncrona |
| QA-G6aT-13 | menor | designer | La sonda de la ventana encendida (`#FFE082 ± 3`, sin *bloom*) no se sostiene con el post de la noche (balance −25, saturación −20, exposición +0,6, *Neutral*). AC-SCN-09-06 y AC-SCN-20-02 dicen que brilla `#FFE082` | `04-design-notes.md:642`, `:671`; `01-po-spec.md:184`, `:340` | Medir antes del post (una pasada de depuración sin `Volume`) o con matiz y croma después del post, con la tolerancia escrita. Si se quiere `#FFE082` en pantalla, compensar la ventana por el balance y la exposición de la hora |
| QA-G6aT-14 | menor | architect | Faltan dueños y el orden falla en tres puntos: WP-B0 crea los materiales y afirma los 13 *shaders* antes de WP-D1; los `ProjectSettings` que genera el *bootstrapper* (tres niveles, capa `RowMateScene`, *Frame Timing Stats*) no están en ningún paquete; `Core/AssemblyInfo.cs` necesita `InternalsVisibleTo("RowMate.Scene")` para los controles de prueba de `SceneSession` y no está en la lista | `02-tech-design.md:131`, `:994`, `:1140`, `:643`, `:289`; `Core/AssemblyInfo.cs:4-7` | B0 crea *shaders* mínimos con los nombres del contrato, que D1 reemplaza, o la afirmación y los materiales pasan a B1. Sumar a B0 `ProjectSettings/{QualitySettings,TagManager,ProjectSettings}.asset` (G) y a WP-A `Core/AssemblyInfo.cs` (C), o los controles detrás de una interfaz de prueba |
| QA-G6aT-15 | menor | architect | Dos detalles del determinismo de Core. (a) El tope con el escenario de prueba se compara con `visibleProps` del oráculo, y `List.sort` de Dart no es estable con más de 32 elementos: hitos con la misma `z` en el corte dan listas distintas. (b) El Poisson del bosque cercano: si un segmento mira al vecino, el relleno depende del orden de los pedidos | `environment.dart:101`, `:104-112`; `02-tech-design.md:561`, `:595`; `04-design-notes.md:488` | (a) `CapEnvironment` con `z` distintas en el corte, o la comparación por conjunto en cada `z`. (b) Poisson por segmento sin leer vecinos (margen interno), con una prueba de dotnet: pedir k + 1 antes que k da el mismo relleno |
| QA-G6aT-16 | menor | architect | `HudWarmup` arma y desarma dos `HudView` en el arranque. Desde la 6a, cada uno arma `SceneSurface`. El diseño no dice que el precalentado no llame `Preload` ni `Open`, ni que la superficie se desuscriba de `OutputChanged` al desarmarse: una suscripción que queda es un aviso de "objeto que ya no existe" (AC-WKT-13-09) | `Presentation/Workout/HudWarmup.cs:13-21`; `02-tech-design.md:737`, `:749` | `SceneSurface` se suscribe al adjuntarse al panel y se desuscribe al soltarse. `HudWarmupTests`: 0 suscriptores en `IImmersiveScene` después de desarmar y 0 llamadas a `Preload` u `Open` |
| QA-G6aT-17 | menor | architect | El `[PERF]` no mide la salida, que AC-WKT-13-15 incluye. El dibujo de calentamiento de la precarga ocurre en Workout en reposo, contra "0 dibujos" de DEMO-01 y "sin dibujarse" de AC-SCN-01-06 | `01-po-spec.md:290`, `:370`; `02-tech-design.md:759`, `:863-873`, `:913` | `FrameTimeProbe` con una ventana de 0 a 1 s desde la confirmación de `Terminar`. El calentamiento es un único dibujo fuera de pantalla antes de `Ready`, y DEMO-01 cuenta después de `Ready`. El PO lo precisa en AC-SCN-01-06 (QA-G6aT-20) |
| QA-G6aT-18 | menor | architect | `ci-unity.yml` (Ubuntu, GameCI) va a correr las pruebas con `SceneMode.Real` sin GPU real (OpenGL por software o sin dispositivo): *shaders* para GLCore y Vulkan, topes de tiempo frágiles. "El CI pasa sin el pack" (AC-SCN-15-08) no tiene una prueba concreta | `.github/workflows/ci-unity.yml`; `01-po-spec.md:270`, `:273` | Una categoría `GPU`/`Perf` con `Assume` sobre `SystemInfo.graphicsDeviceType` y sin topes de tiempo en el CI. Los *shaders* con los destinos de Linux. Una prueba EditMode: ningún asset versionado referencia un GUID de `Assets/ThirdParty/` salvo los `.asset` de `Slots/` (que caen al procedural, QA-G6aT-04) |
| QA-G6aT-19 | menor | architect | `DartRandom` porta código BSD-3 de la VM de Dart. El encabezado cubre la fuente, pero los builds de Windows y Android (distribución binaria) también tienen que llevar el aviso, y el repo no tiene `THIRD_PARTY_NOTICES` | `02-tech-design.md:510-513`; ADR 012:45; `LICENSE` (MIT) | `THIRD_PARTY_NOTICES.md` con la licencia del Dart SDK, incluido en el build (o en "Acerca de"). `RepoHygieneTests` exige el aviso si existe `DartRandom.cs` |
| QA-G6aT-20 | menor | po | Precisiones de la spec: (a) son 101 ID (AC-BLE-16-02 y -03); (b) AC-WKT-17-13 pide el borde de la hueca ≥ 3:1, y el diseño cuenta una excepción (2,93-3,02); (c) DV-62: el legacy corta lo cercano en relZ < **1 m** (`zMin = d + 1`), no en 0,5; (d) AC-SCN-09-04: las estrellas del legacy son blancas (`Colors.white`), no salen de la paleta; (e) AC-SCN-01-06 "sin dibujarse" contra el dibujo de calentamiento; (f) "el remero en el 0,504 del alto en vertical" vale para TV y P360 (V390 da 0,507) | `01-po-spec.md:11`, `:389`, `:348`, `:657`, `:182`, `:290`, `:142`; `environment.dart:90`; `sky_painter.dart:66`; `04-design-notes.md:790` | Corregir los textos: 101 ID, la excepción del borde de la hueca como en la 4b, "1 m", "las estrellas, blancas como el legacy", "un dibujo de calentamiento fuera de pantalla", "0,504 en TV y P360" |
| QA-G6aT-21 | menor | architect | Riesgo de profundidad en GLES (nivel Bajo, sin Z invertida): con el plano cercano en 0,5 y el lejano en 4 000 m, el paso de profundidad a 1,5 km es de ≈ 0,27 m. El terreno lejano (≈ 0 m en la orilla) y el plano del agua pueden pelearse en la orilla lejana | `02-tech-design.md:220`, `:764`, `:777`; ARCHITECTURE §20.1 (Bajo GLES 3.2) | El plano lejano en ≈ 1,6-2 km (el terreno termina en 1,5 km y el horizonte no escribe profundidad), o un *offset* de profundidad para el terreno lejano. Una toma en Bajo |

---

## 15. Lo que este gate no verificó

- **Nada de Unity**: las sondas S6a-1 a -6 (y la S6a-7 propuesta), el costo real de los *shaders*, el determinismo de las tomas 3D en esta GPU, el comportamiento de `SubmitRenderRequest` en modo edición, la persistencia del nivel de calidad en el Editor (QA-G6aT-05 es un riesgo a confirmar, no una falla vista) y las asignaciones de URP en el Editor.
- **El oráculo de la escena no se corrió** (no existe todavía): los vectores de esta revisión salen de un puerto propio, independiente, contra los números de la spec.
- **El `[PERF]` de la 5** no se repitió: se revisó su método y sus números contra 00-deuda-5.
- **El aspecto** (la luz de cada hora, el terreno, la vegetación, el bote): son de las lentes de producto y de diseño, y de la verificación con las tomas.
