# It. 6a — Verificación de QA, lente "paridad"

| Campo | Valor |
|---|---|
| Iteración | 6a `06a-escena-lago` (Escena: Core de escena, cielo, luz, hora, Lago alpino y agua degradada) |
| Lente | Paridad contra el legacy ([rowmate-legacy](https://github.com/figuibej/rowmate-legacy)), en paralelo con la verificación principal |
| Veredicto | **APROBADO CON OBSERVACIONES**: 0 bloqueantes, 0 mayores, 5 menores |
| Alcance | `scene_camera.dart`, `scene_state.dart`, `time_of_day.dart`, `environment.dart`, `environments/lake_environment.dart`, `painters/sky_painter.dart`, `painters/shore_painter.dart`, `painters/water_painter.dart` (el respaldo), `scene_settings.dart` y `Hora escena` (`core/dev/simulator_overlay.dart`); los tests legacy del ROADMAP; el oráculo de escena; DV-52 a DV-67; CAP-01 a CAP-07 contra LEG-01 a LEG-07 |
| Sin Unity | Esta lente no abrió Unity. Lo que es [Editor-PM], [Captura en vivo] o [Device] queda NO VERIFICABLE acá, salvo lo que se puede leer en las capturas ya generadas (`snapshots/` y `snapshots/manifest.json`) |
| Repo | Ningún archivo del repo cambió salvo este reporte. Todo lo demás (copia del oráculo, programas de medición, recortes) está en `<scratch>/it6a/lens-paridad/` |

## 1. Lo que se corrió

### 1.1 dotnet

`powershell -NoProfile -File tools/dotnet/run-tests.ps1 -Matrix -ArtifactsPath <scratch>/it6a/lens-paridad/artifacts`:

- Compilación de la solución entera: **0 avisos, 0 errores**.
- Herramientas: **192 / 192** pasan.
- Core: **3044** pruebas, **3005 pasan, 0 fallan, 39 ignoradas** (las de equipo y de Unity, con su motivo: `AndroidSceneSixtyFps`, `WindowsScenePerf`, `WindowsSceneHourDoesNotSurviveRestart`, `ProjectOpensWithoutCompileErrors`…).
- Matriz (`ac-matrix.md`, solo dotnet): 615 PASA, 134 PENDIENTE UNITY, 4 IGNORADO, **0 FALLA** y 9 SIN TEST. Los 9 SIN TEST son LT-78 a LT-81 y LT-83 a LT-87, que se prueban en PlayMode (QA-V6a-PAR-05).

### 1.2 El oráculo de escena, regenerado desde el legacy

Se copió `tools/oracle/engine/` (solo `pubspec.yaml`, `pubspec.lock` y los dos tests de escena) y `tools/oracle/lib/golden.dart` a `<scratch>/it6a/lens-paridad/oracle/`, con un `pubspec_overrides.yaml` que apunta a `../rowmate-legacy`, y se corrió `<flutter-sdk>/bin/flutter pub get --offline` y `flutter test test/scene_vectors_test.dart test/scene_legacy_images_test.dart`, con `ROWMATE_ORACLE_OUT` y `ROWMATE_LEG_OUT` en el scratch. Así no se escribió nada en `Assets/` ni en `docs/`.

- Las dos pruebas pasan (`+2: All tests passed!`), con los vectores de control de la spec adentro (`Random(42)` → `0.15092545597797424`, `0.6041479725361217`, `nextInt(1000) = 77`; `Random(55450)` → `0.854835531354132`).
- **Los seis `scene_*.json` regenerados son iguales a los del repo** salvo `generatedAt` (resumen MD5 igual en `scene_lake`, `scene_light`, `scene_palette`, `scene_random`, `scene_sky` y `scene_water`), con el mismo `legacyCommit` (`3ae0f3a8…`).
- **Las diez imágenes LEG (LEG-01 a LEG-07 y las tres con marcas) son iguales byte a byte** a las de `snapshots/legacy/`.
- Guardia del legacy: `git status --porcelain` vacío antes y después; el listado `--porcelain --ignored --untracked-files=all` y la instantánea de metadatos (ruta, tamaño, fecha) de todo archivo fuera de `.git/` son **iguales** antes y después. El módulo de credenciales del legacy no se leyó.
- Lo que el oráculo copia de lo privado del legacy (el alfa de las estrellas, la x e y de las nubes, el perfil de las cordilleras y sus picos nevados, el `_c` de la orilla y el alfa de las líneas del agua) se comparó línea por línea con `sky_painter.dart:51-66`, `:91-96`, `lake_environment.dart:62`, `:63-84`, `shore_painter.dart:45-50` y `water_painter.dart:94-97`: **es igual**.

### 1.3 Mediciones propias

- Recortes ampliados y sondas de píxel de CAP-01 a CAP-07 contra LEG-01 a LEG-07 (PowerShell con System.Drawing, en `<scratch>/it6a/lens-paridad/tools/`).
- Una transcripción a C# en `float` del fragmento de `WaterDegraded.shader` (líneas 168 a 180) contra las 10 líneas del legacy (`<scratch>/it6a/lens-paridad/shimmer/Program.cs`), para QA-V6a-PAR-01.

## 2. Paridad por archivo del legacy

| Legacy | Código nuevo | Resultado | Evidencia |
|---|---|---|---|
| `scene_camera.dart:6-46` | `Core/Scene/SceneCamera.cs`, `ChaseCameraProjection.cs` | **Igual**: horizonte 0,42 / 0,40 (cuadrado = vertical), focal = lado corto, `project`, `minZ` 0,5, `scaleAt`, `unprojectWater`. La matriz de Unity reproduce `TryProject` | LT-62 a LT-65 y `TheMatrixReproducesTryProject` (siete tamaños) pasan; en las capturas con marcas, `máx |Δx|` entre la marca del legacy y el ancla 3D proyectada por la cámara real = **0 dp** (CAP-02, -05, -07) |
| `scene_state.dart:13-57` (la parte de la 6a) | `SceneState.cs` | **Igual**: paso acotado a [0; 0,1], `τ` 1,5 s, corte a 0 bajo 0,01 m/s con objetivo 0, distancia con la velocidad nueva, `time` que corre también en pausa, `shaderDistance` mod 4096. Mejora sin efecto visible: `dt` NaN cuenta como 0 (en Dart el NaN pasaría) | LT-66 a LT-68 con los mismos casos y el mismo `run()` (pasos de 0,05 s; `round` de Dart = `Floor(x + 0,5)` para positivos) |
| `immersive_workout_screen.dart:81-86`, `scene_view.dart:61-86` | `SceneInputs.cs`, `SceneHour.cs`, `SceneSession.cs:165-169`, `SceneHost.cs:426-431` | **Igual**: `isActive` = fase activa del motor; split entero y SPM de la última lectura; hora `hour + minute/60` local, la forzada si la hay; la sesión arranca en 0 | Lectura de código |
| `time_of_day.dart:1-117` | `SceneTimeOfDay.cs`, `ScenePalette.cs`, `SceneColor.cs` | **Igual bit a bit**: los 6 keyframes y el cierre 22 → 28 → 29, `Color.lerp` con el *clamp* del SDK, `sunElevation`, `isNight`, `lightFor` (fracción, elevación, fuerza 0,45 / `clamp(1,2·e; 0,2; 1)`) | `PaletteMatchesTheOracleEveryMinuteBitForBit`, `SunElevationAndNightMatchTheOracleEveryMinute`, `TheLegacyLightMatchesTheOracleEveryMinuteInSevenSizes`, LT-69 a LT-74 |
| `environment.dart:39-129` | `ShoreEnvironment.cs`, `ShoreSegments.cs`, `ShoreProp.cs`, `HorizonModel.Color` | **Igual**: segmentos de 50 m, semilla `id·1 000 003 + k·7 919 + 17` en 64 bits, `bankX` 9, rango 400, tope 120, `zMin = d + 1`, olvido de `k < first − 1`, prioridad de los hitos y de las boyas más cercanas, orden de lejos a cerca, `horizonColor`. Diferencia de diseño sin efecto (DT6a-15): la caché es de la sesión, no global | Segmentos 0 a 99 completos y 0 a 2 000 por FNV-1a **bit a bit**; las 8 listas visibles (0, 1, 49,5, 50, 920, 1 234, 5 000 y 42 195 m); el escenario de prueba del tope (`capSegments`, `capVisible`); LT-75 a LT-77 |
| `math.Random` de la VM de Dart | `DartRandom.cs` | **Igual bit a bit**: mezcla de Thomas Wang, MWC con `A = 0xFFFFDA61`, 4 giros, `nextInt` por máscara o rechazo, `nextDouble` de 53 bits | 9 semillas (con −1 y 2⁴⁰ + 3) × 5 operaciones × 64 tiradas del oráculo; vectores de la spec |
| `lake_environment.dart:1-86` | `LakeEnvironment.cs`, `HorizonModel.cs` | **Igual**: orden de las tiradas (z inicial; x, escala, variante, paso), pinos `|x|` 10 a 16, escala 0,7 a 1,3, muelle `k mod 8 = 3` en (−9; z₀ + 25), cabaña `k mod 12 = 7` en (13; z₀ + 10) con variante `k`, agua 0,25 / 2,5 sin tinte; perfil de las cordilleras, sus tonos y la nieve `lerp(#F2F6FA; fog; 0,3)` exactos en la lógica. Las cordilleras quietas, la nieve sobre el 58 % y el tope de 9° son DV-55, DV-64 y DV-65 | `scene_sky.json` (`horizon`, `profile`, `snowPeaks`), `scene_lake.json`; el máximo de pinos por segmento (9 por orilla + muelle + cabaña = 20) entra en los 24 del búfer |
| `sky_painter.dart:9-124` | `StarField.cs`, `CloudModel.cs`, `CelestialPlacement.cs`, `SkyBackdrop`, `CelestialMask`, `Sky.shader`, `Stars.shader` | **Igual en la lógica**: 80 estrellas con `Random(42)` (x, y·0,85·horizonte, radio 0,6 + u), oscuridad y titileo; 7 nubes con sus datos, `y`, color y alfas; la `x` sin la distancia es D-ART-06 / DV-56. En pantalla las 80 caen en su píxel del legacy (recortes de CAP-04 y CAP-06 contra LEG-04 y LEG-06: mismas posiciones; las de más son el fondo tenue de P-10). Diferencias registradas: DV-52, -53, -56, -57, -63, -66 | `stars` (560 filas), `clouds` (196), `cloudColors`; manifest: "nubes: 7 a la vista; máx error contra el punto del legacy = 0 dp" |
| `shore_painter.dart:1-289` (pino, cabaña, muelle, `_c`, `_night`) | `ShoreShading.cs`, `NightLights.cs`, `PropPlacement.cs`, `PineMesh.cs`, `ShoreStreamer.cs`, `RowMateLighting.hlsl`, `RowMateFog.hlsl` | **Igual en la lógica**: tinte (rojo y verde × a, azul × mín(1; a + 0,12)), niebla `clamp((relZ − 80)/320)·0,8` (la del *shader* es igual hasta 400 m), noche con ambiente < 0,6 (fuera de la ventana de DV-60, que es continua en sus bordes: 19:15:43-19:35:43 y 4:32:51-4:52:51), pino de 6 m × escala y 2,6 m de ancho, cabaña 4 × 2,6 con techo a 4,2, muelle de −9 a −6. La base sobre el terreno está permitida por AC-SCN-15-05 | `shore` (1 350 filas) bit a bit; recortes de CAP-02 y CAP-05: cada pino, el muelle y la cabaña con el eje sobre su marca (± 1 dp en el recorte), con la base más arriba solo por el terreno (≈ 1,6 m y ≈ 2,1 m en dos pinos de CAP-02 y ≈ 2,3 m el piso de la cabaña; el zócalo baja a ≈ 0,75 m; tope 2,5 m) |
| `water_painter.dart:79-104` (respaldo) | `DegradedWater` (`ShoreShading.cs:56-104`), `WaterDegraded.shader` | **Igual en la lógica** (tres tramos y las 10 líneas, bit a bit contra `bands` y `lines`); en pantalla las líneas van bajo el astro que se ve y con su visibilidad (DV-59). **Diferencia sin registrar en el *shader*:** con la fase ≈ 0 dibuja una **11.ª línea** en la fila de abajo (QA-V6a-PAR-01) | Sondas de píxel y transcripción (§4.2) |
| `scene_settings.dart:1-43`, `simulator_overlay.dart:74`, `:139-150` | `App/Scene/SceneSettings.cs`, `Simulator/Panel/SimulatorPanelStore.cs:104-121`, `SimulatorPanelState.cs:17-19`, `SceneHourTransition` | **Igual**: hora forzada entera, `±1` desde la forzada o desde la hora entera del reloj con el `%` de Dart (23 → 0, 0 → 23), `real` / `H:00` sin cero a la izquierda, `Hora real` solo con la hora forzada, no se guarda; `scene.environment` no se lee ni se escribe en la 6a. La transición de 1,5 s por el camino corto es DV-58 | Lectura de código; `SceneHourTests` en dotnet |

## 3. Los tests legacy del ROADMAP

| Archivo legacy | Casos | Portado | Resultado en esta lente |
|---|---|---|---|
| `test/scene/scene_camera_test.dart` | 4 (:9, :16, :30, :34) | LT-62 a LT-65, mismos casos y tolerancias | PASA (dotnet) |
| `test/scene/scene_state_test.dart` | :14, :26, :55 (los de la 6a) | LT-66 a LT-68, mismo `run()` y mismas aserciones | PASA (dotnet). :37, :46, :60 y :69 son de la It. 7 |
| `test/scene/time_of_day_test.dart` | 6 | LT-69 a LT-74 | PASA (dotnet) |
| `test/scene/environment_test.dart` | los 3 genéricos del lago | LT-75 a LT-77 | PASA (dotnet). Los de río, costa y regata son de la It. 8 |
| `test/scene/sky_shore_painter_test.dart` | lago × 2 tamaños × 2 horas y las estrellas de las 19 h | LT-78 a LT-81 (PlayMode) y LT-82 (dotnet) | LT-82 PASA; LT-78 a LT-81 NO VERIFICABLE sin Unity (están en `Tests/PlayMode/Scene/SceneLifecycleTests.cs`). El caso :57 (todos los `PropKind`) es de la It. 8 |
| `test/scene/water_painter_test.dart` | :11 (respaldo) | LT-83 (PlayMode) | NO VERIFICABLE sin Unity; la lógica de los tramos y las líneas pasa en dotnet contra el oráculo |
| `test/scene/scene_view_test.dart` | los 4 del lago | LT-84 a LT-87 (PlayMode) | NO VERIFICABLE sin Unity |

## 4. CAP-01 a CAP-07 contra LEG-01 a LEG-07

### 4.1 Lo que coincide y lo que cambia a propósito

| Par | Coincide | Cambia, registrado |
|---|---|---|
| CAP-01 / LEG-01 (V390, 6 h, E-920) | Orilla, pinos, muelle y cabaña en su lugar; estrellas tenues (ambiente 0,825); tono del agua (sondas a 400-800 dp: diferencia ≤ 12/255) y del cielo | El sol detrás del macizo a −17,5° y 1,5° (DV-52, DV-54); líneas centradas en x 72 (DV-59); nubes sin deriva por distancia (DV-56); cordilleras (DV-55, -64, -65); degradé (DV-66); bote (DV-61); orilla con terreno (mejora EXR) |
| CAP-02 / LEG-02 (V390, 12 h, marcas) | Cada objeto sobre su marca; las 10 líneas en las mismas filas (342, 357, 383, 418, 464, 520, 586, 662, 748) | Sol a 12° en (195; 254,7) y más chico (DV-52, DV-63); resto como arriba |
| CAP-03 / LEG-03 (V390, 18 h) | Estrellas (ambiente 0,85: se ven), ventana apagada, tono del agua | Líneas centradas en x 318 en vez de 292,5 (DV-59) |
| CAP-04 / LEG-04 (V390, 22 h) | Las 80 estrellas en su píxel; ventana encendida; líneas plateadas (fila 747: `#19263F` contra `#15233C` del legacy) | Luna en (155,2; 264,3) con su fase (DV-52, DV-57); halo 4 × su disco (DV-63); fondo tenue de estrellas (P-10) |
| CAP-05 / LEG-05 (H844, 12 h, marcas) | Cada objeto sobre su marca; la cabaña en x ≈ 548,75 | Ídem CAP-02 |
| CAP-06 / LEG-06 (H844, 22 h) | Las 80 estrellas en su píxel (también las de adentro de las nubes, que se siguen viendo) | Ídem CAP-04; capa lejana de nubes (solo Alto, admitida en AC-SCN-11-06) |
| CAP-07 / LEG-07 (V390, 12 h, E-0, marcas) | Cada objeto sobre su marca; **las nubes coinciden en x e y** con las del legacy (a 0 m la deriva por distancia vale 0) | Ídem CAP-02 |

### 4.2 Diferencias que no tienen DV

1. **Una 11.ª línea de brillo en la fila de abajo** (QA-V6a-PAR-01). Sondas de la última fila contra la anterior:

   | Toma | Fila de abajo | Fila anterior | Legacy, fila de abajo |
   |---|---|---|---|
   | CAP-07 (V390, E-0, 12 h), x 300 | `#184F88` | `#0A4784` | LEG-07: `#0E4F8F` (= waterNear, sin línea) |
   | CAP-02 (V390, E-920, 12 h), x 5 / 300 / 385 | `#164B81` / `#184F88` / `#164B81` | `#09437E` / `#0A4784` / `#09437E` | LEG-02: `#0E4F8F` |
   | CAP-04 (V390, E-920, 22 h), x 300 / **385** | `#141F35` / **`#060F26`** | `#071128` / `#061026` | — |
   | CAP-05 y CAP-06 (H844, E-920), fila 389 | `#18518B`, `#142037` | `#0B4988`, `#07122A` | LEG-05: `#0E4F8F` |
   | CAP-12 (W1280, E-42K), fila 719 | `#18518B` | `#0B4988` | — |

   En CAP-04 la fila de abajo no cambia en x 385: queda fuera del medio ancho de una línea con t = 1 (0,5·390 = 195 desde la luna, en x 155). Es una línea de brillo, no una viñeta ni la bruma.

   La transcripción del fragmento (`Program.cs`, V390, fuerza 1) da las 10 filas del legacy y además `fila 843: n=10 t=1.0000 cobertura=1.00 alfa=0.050` con la distancia en 0 y en 920 m, y `fila 389: n=10` en H844. Con 0,02 m la cobertura baja a 0,19 y con 3 m desaparece.

2. **El tono del borde de arriba del cielo en la imagen publicada** (QA-V6a-PAR-03). Lo mueve el ajuste de color de la hora (AC-SCN-09-05). La sonda de contrato (sin post) de CAP-02 da diferencia 0, pero en la toma:

   | Toma | Píxel (195; 8) | skyTop de la paleta | Δ máx |
   |---|---|---|---|
   | CAP-01, 6 h | `#304489` | `#324B9A` | 17 (azul) |
   | CAP-02 y CAP-07, 12 h | `#0848A1` | `#0B4FA8` | 7 |
   | CAP-03, 18 h | `#2A3780` | `#2E3C8C` | 12 |
   | CAP-04, 22 h | `#050A1E` | `#050A1E` | 0 |

   El tono dominante es el mismo en todas, y el agua también (diferencia ≤ 12/255 en las sondas de 360 a 800 dp). No es un error de *shader*: es una cuestión de redacción del AC.

3. **Nada más sin registrar.** El bote (DV-61), el astro (DV-52, -54, -57, -63), las cordilleras (DV-55, -64, -65), el degradé con la bruma (DV-66), las nubes (DV-56), las estrellas de día (DV-53), las líneas bajo el astro (DV-59), el fundido de cerca y de lejos (DV-62), las luces de a una (DV-60), la transición de la hora (DV-58), el terreno, el bosque, los juncos y las rocas (AC-SCN-15-07), el fondo tenue de estrellas y la capa lejana de nubes (P-10, AC-SCN-11-03 y -06) y los agregados del agua (AC-SCN-13-04) están en la spec. DV-67 no aparece en la escena sola, como corresponde.

**Nota sin issue.** Los pinos tienen una inclinación de hasta 2° por forma (04-design-notes §7.3, `LakeArt.PineMaxTiltDeg`). La base y el eje al pie caen sobre la marca (0 dp), pero la punta de un pino cercano se corre unos dp (≈ 11 dp a 10 m en TH, con escala 1,3). Se lee "el eje de cada objeto" de AC-SCN-15-05 como el pie, que es lo que mide la sonda. Si el PO quiere el eje entero, son 2° de arte.

## 5. DV-57 a DV-67 (y las ampliaciones de DV-52, -53 y -59)

| DV | Se ve como dice la spec | Evidencia |
|---|---|---|
| DV-57 luna con fase | Sí: gibosa menguante, iluminada 0,937 con la fecha de prueba | manifest CAP-04, -06, -10 |
| DV-58 hora en 1,5 s | Lógica: camino corto (23 → 0 hacia adelante, 0 → 23 hacia atrás, 12 h hacia adelante), con reducir movimiento en el acto | `SceneHour.cs:94-143`; video NO VERIFICABLE acá |
| DV-59 líneas bajo el astro que se ve | Sí: x 72 a las 6, 318 a las 18 y 155 a las 22; a las 19 h, visibilidad 0,536 | manifest CAP-08; sondas |
| DV-60 luces de a una | Lógica continua en los bordes de la ventana (fuera de ella, el estado del legacy) | `NightLights.cs:197-217` |
| DV-61 casco provisorio | Sí | CAP-01 a -07 |
| DV-62 fundidos | Lógica (`MaterialArt.InstanceFade`); la lista de visibles es la del legacy | `ShoreStreamer.cs:169-174`; VID-03 NO VERIFICABLE acá |
| DV-63 disco 1,6° y halo 4× | Sí: radio 10,9 dp en V390, 20,1 en W1280; luna 8,7 | manifest |
| DV-64 nieve sobre el 58 % | Sí (macizo y pico central en vertical) | CAP-02, -07 |
| DV-65 tope de 9° en vertical | Sí (cordilleras más bajas en V390 que en LEG-02) | CAP-02 contra LEG-02 |
| DV-66 degradé con bruma | Sí; ver QA-V6a-PAR-03 por el borde de arriba en la toma | §4.2 |
| DV-67 degradés del HUD | No están en la escena sola (correcto) | CAP-01 a -07 |
| DV-52, DV-53, DV-59 ampliadas | Sí (12° al mediodía; sin estrellas a las 15 h; líneas a la mitad a las 19 h) | manifest CAP-02, -11, -08 |

## 6. Issues

| ID | Severidad | Responsable | Issue | Evidencia | Corrección sugerida |
|---|---|---|---|---|---|
| **QA-V6a-PAR-01** | menor | designer | **El agua degradada dibuja una 11.ª línea de brillo en la fila de abajo cuando la fase vale ≈ 0.** El legacy dibuja 10 (`for (var i = 0; i < 10; i++)`) y AC-SCN-13-03 pide 10. El fragmento elige la línea más cercana con `n = max(round(√tp/0,1 − fase/0,1); 0)`, sin tope. En la última fila `√tp ≥ 0,95` da `n = 10` → `t = 1 + fase` → `y` = el borde de abajo, con alfa `0,05·vis` y medio ancho `0,5·ancho`. Pasa mientras la fase sea < 0,25/alto del agua (≈ 2,5 cm de cada 5 m en V390): en E-0, E-920 y E-42K y **al abrir la sesión mientras el bote está quieto en 0 m** (antes de la primera palada con el remo real, o con `Remando` apagado). Secundario, por la misma regla: con la fase < 0,02 la línea 0 (en el horizonte) pierde la cobertura de 0,57 a 1 dp, porque el redondeo en el espacio de `√tp` elige la línea 1 (en H844, la fila 164 pasa de 0,6 a 0) | `Art/Shaders/Scene/WaterDegraded.shader:175-179` contra `water_painter.dart:93-103`. Sondas de §4.2 (CAP-02, -04, -05, -06, -07 y -12; LEG-02, -05 y -07 sin línea). Transcripción: `fila 843: n=10 t=1.0000 cobertura=1.00 alfa=0.050` y `fila 389: n=10` en H844 | `n = clamp(round(…); 0; 9)` (1 ALU, dentro de los 24 de DT6a-07). Para la línea 0, comparar las dos candidatas `floor` y `floor + 1` por la distancia en `y` (o aceptar la diferencia de < 1 dp en el horizonte y anotarla). Sumar a `SceneShaderTests` (GPU) una prueba con fase 0 en V390: la fila de abajo igual a waterNear y 10 líneas |
| **QA-V6a-PAR-02** | menor | architect | **Faltan en el `manifest.json` las sondas que 02-tech-design B6a-17 y 04-design-notes §10.4 prometen como valores.** Son: la ventana `#FFE082` de CAP-04 en la pasada de contrato (el color del legacy, AC-SCN-09-06 y AC-SCN-20-02), la luz de la cabaña (DN6a-13), el matiz OKLCH de CAP-01, -03 y -04 (el "tono dominante" de AC-SCN-09-05 contra LEG-01 a -04) y el velo de CAP-20. Solo están las dos de contrato del cielo de CAP-02 y la del brillo | `grep -c FFE082 manifest.json` = 0; los `values` de CAP-01, -04 y -20 no tienen esas líneas; `Snapshots/SceneCatalog.cs:57-61` llama a `SceneMarks.Contract` solo en CAP-02. Medido a mano en la toma: la ventana de CAP-04 es `#EDE09A` (matiz OKLCH ≈ 99°, croma ≈ 0,09: dentro de la sonda de aspecto); en LEG-04, `#FFE082` | Sumar en `SceneCatalog` las sondas de contrato de CAP-04 (ventana, ± 3) y de la cabaña, y las de aspecto (matiz de CAP-01, -03 y -04; velo de CAP-20), como valores del manifest según B6a-17, y regenerar las capturas |
| **QA-V6a-PAR-03** | menor | po | **AC-SCN-11-02 dice que el color de la hora enriquece el degradé "sin cambiar esos dos extremos", pero en la toma publicada el ajuste de la hora los mueve.** El borde de arriba se corre hasta 17/255 (6 h), 12 (18 h) y 7 (12 h). Se mide (y pasa con diferencia 0) solo en la pasada de contrato, sin post (DN6a-27, §10.4). El AC además pide "CAP-01 a CAP-07 … con las sondas de color de arriba y del horizonte", y hay sondas solo en CAP-02 (arriba y a media altura) | §4.2 punto 2; `snapshots/manifest.json` (CAP-02: "contrato cielo de arriba (195; 8): #0B4FA8 … (diferencia 0)"); `01-po-spec.md` AC-SCN-11-02 | Escribir "antes del ajuste de color de la hora", como en AC-SCN-09-06 y -20-02, y que la verificación diga dónde están las sondas (§10.4: CAP-02, arriba y a media altura). En el REPORT, contar que en la imagen final los extremos se corren ≤ 17/255 por AC-SCN-09-05 |
| **QA-V6a-PAR-04** | menor | po | **AC-SCN-11-04 da a las 6:12 "ambiente 0,8499999999999999", y el legacy da 0,85 exacto**, como a las 18:00. La regla no cambia (las dos se ven); es el número del texto, que venía de QA-6a-PAR-09 | `scene_palette.json`, minuto 372: ambiente `3feb333333333333` (= 0,85), igual que el minuto 1 080; `Tests/EditMode/Scene/StarFieldTests.cs:70-73` ya lo anota | Corregir el número en AC-SCN-11-04 (y en 03-qa-harmony-paridad §2, fila 11, si se toca) |
| **QA-V6a-PAR-05** | menor | architect | **LT-78 a LT-81 y LT-83 a LT-87 salen SIN TEST en la matriz de dotnet en vez de PENDIENTE UNITY.** Se verifican solo en PlayMode (`p`), pero `UnityOnlyIds` solo lista AC, así que `run-tests.ps1 -Matrix` sin `-UnityResults` los da por no probados. Es la primera iteración con LT que no son de dotnet | `ac-matrix.md` (9 filas SIN TEST, todas esas LT); `Tests/EditMode/Traceability/It06aIds.cs:79-88` (verificación `p`) y `:94-99` (`UnityOnlyIds` sin LT) | Que `-Matrix` derive PENDIENTE UNITY también de la verificación de `LegacyCases` (o sumar esas LT a `UnityOnlyIds` y ajustar `TraceabilityTests`) |

## 7. Matriz AC → prueba → resultado (los de esta lente)

| AC | Prueba / evidencia | Resultado |
|---|---|---|
| AC-SCN-02-01, -02 | `SceneCameraTests` (LT-62, -63, -65, siete tamaños, la matriz), marcas 0 dp | PASA (dotnet); la parte [Editor-PM] la mide el QA principal |
| AC-SCN-02-03 | LT-64, `zMin = d + 1` en `ShoreSegments` | PASA (dotnet) |
| AC-SCN-03-01 a -06 | `SceneStateTests` (LT-66 a -68) | PASA |
| AC-SCN-03-07 | `SceneInputs.From`, `SceneHost.Inputs` | PASA (código); `-Simulator` NO VERIFICABLE acá |
| AC-SCN-09-01 | `SceneHour.Effective`, `SceneHourTests` | PASA (dotnet) |
| AC-SCN-09-02, -03 | Oráculo por minuto, bit a bit; ambientes de referencia | PASA |
| AC-SCN-09-04 | `SceneSession.Tick`: todo sale de `PaletteFor(visible)` | PASA (código) |
| AC-SCN-09-05 | Tono dominante igual en CAP-01 a -04 contra LEG-01 a -04 (sondas propias) | PASA con observación (QA-V6a-PAR-02, -03) |
| AC-SCN-09-06, AC-SCN-20-02 | `NightLightsTests`; la ventana encendida a las 22 h y apagada a las 18 h | PASA (dotnet y a la vista); sonda de contrato faltante (QA-V6a-PAR-02) |
| AC-SCN-10-01 a -03 | `scene_light.json` y `scene_palette.json` por minuto, siete tamaños | PASA |
| AC-SCN-10-04 | manifest: astro en (72,0; 326,9) a las 6, (195; 254,7) a las 12, (318,0; 326,9) a las 18 en V390 = `centro + focal·tan(acimut)` | PASA |
| AC-SCN-10-05 | `CelestialPlacementTests` (barrido) | PASA (dotnet) |
| AC-SCN-11-02 | Sondas de contrato de CAP-02 (diferencia 0) | PASA con observación (QA-V6a-PAR-03) |
| AC-SCN-11-03 | `stars` del oráculo; recortes de CAP-04 y CAP-06 | PASA |
| AC-SCN-11-04 | `StarFieldTests` (bordes, LT-82) | PASA (texto: QA-V6a-PAR-04) |
| AC-SCN-11-06 | `clouds`, `cloudColors`; manifest 0 dp; CAP-07 contra LEG-07 | PASA |
| AC-SCN-11-07 | LT-78 a LT-81 (PlayMode) | NO VERIFICABLE sin Unity |
| AC-SCN-13-02 | `bands` del oráculo bit a bit; sondas del agua | PASA |
| AC-SCN-13-03 | `lines` del oráculo bit a bit (dotnet); capturas | **PASA en dotnet, FALLA en la captura** (11 líneas con la fase ≈ 0: QA-V6a-PAR-01, menor) |
| AC-SCN-14-01 a -06 | `ShoreSegmentsTests`, `DartRandomTests` (oráculo) | PASA |
| AC-SCN-15-01 a -04 | `LakeEnvironmentTests` | PASA |
| AC-SCN-15-05 | Segmentos y listas bit a bit; marcas 0 dp; recortes de CAP-02, -05 y -07 (eje al pie sobre la marca, base más arriba ≤ 2,5 m) | PASA |
| AC-SCN-19-01 | `horizon`, `profile`, `snowPeaks` (colores exactos en la lógica); alto y nieve por DV-64 y DV-65 | PASA |
| AC-SCN-20-01 | `shore` del oráculo bit a bit | PASA |
| AC-SCN-21-01 | `SceneSettings` sin escenario | PASA (código) |
| AC-SCN-23-01, -03, -04 | — | NO VERIFICABLE sin Unity ni build |
| AC-SCN-23-02 | `SceneHourTests` | PASA (dotnet) |
| AC-BLE-16-07 | `SimulatorPanelStore`, `SimulatorPanelState` contra `simulator_overlay.dart:74`, `:139-150` | PASA (código y dotnet) |
| LT-62 a LT-77, LT-82 | dotnet | PASA |
| LT-78 a LT-81, LT-83 a LT-87 | PlayMode | NO VERIFICABLE sin Unity (QA-V6a-PAR-05) |

## 8. Resumen para el coordinador

La lógica de la escena es una réplica fiel del legacy: generador, segmentos, paleta, astro, estrellas, nubes, orilla, agua y cámara coinciden bit a bit (o en 1e-12 / 1e-9 donde hay trigonometría) con el oráculo. El oráculo regenerado desde el legacy es idéntico al del repo y el legacy quedó intacto. En las capturas, cada objeto del lago cae sobre su marca del legacy y las diferencias visibles están registradas, con dos excepciones: la 11.ª línea del agua (QA-V6a-PAR-01, *shader*) y el borde de arriba del cielo movido por el ajuste de la hora, que es un tema de redacción (QA-V6a-PAR-03). Lo demás es cobertura y texto: sondas prometidas que faltan en el manifest (PAR-02), un número del AC (PAR-04) y la clasificación de las LT de PlayMode en la matriz (PAR-05).
