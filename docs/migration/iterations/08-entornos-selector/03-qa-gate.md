# It. 8 — Gate único de QA (armonía QUÉ ↔ CÓMO)

| Campo | Valor |
|---|---|
| Iteración | 8 · `08-entornos-selector` |
| Leído | [01-po-spec](01-po-spec.md), [02-tech-design](02-tech-design.md), [00-arte-entornos](00-arte-entornos.md) |
| Legacy contrastado | [rowmate-legacy](https://github.com/figuibej/rowmate-legacy) (`../rowmate-legacy`, solo lectura): `scene/environment.dart`, `environments/{river,coast,regatta}_environment.dart`, `painters/{shore,thumbnail,water}_painter.dart`, `shaders/water.frag`, `environment_picker.dart`, `scene_settings.dart`, `scene_camera.dart`, `workout_screen.dart:55-105` y los tests `environment_test`, `environment_picker_test`, `sky_shore_painter_test`, `water_painter_test`, `scene_view_test`. `git status --porcelain` del legacy vacío. No se leyó el módulo de credenciales |
| dotnet (línea de base, antes de la It. 8) | `tools/dotnet/run-tests.ps1 -ArtifactsPath <scratch>/it8/gate`: **3 191 tests, 3 152 pasan, 39 omitidos, 0 fallan** (29,2 s) |
| Sonda propia | proyecto de consola en `<scratch>/it8/gate/probe` con el `DartRandom` del repo y el generador del río del legacy, segmentos 0–2 000 (no se versiona) |
| Veredicto | **APROBADO CON OBSERVACIONES**: 0 bloqueantes, 1 mayor (corregible en el diseño antes de cerrar WP-A), 14 menores |

---

## 1. Lo que está bien (verificado contra el legacy)

- **Números de los generadores**: semilla, orden de tiradas (`x`, `scale`, `seed` en el orden de los argumentos con nombre), metros de cada hito (puente 270/1 070/1 870; playa 100–150; faro 530; tribuna 210; torre 610; carteles `50k` sin separador; banderas `k mod 4` alternadas), colores por variante y topes 120/60/200 coinciden con `river/coast/regatta_environment.dart` y `shore_painter.dart`. `MaxPropsPerSegment` (río 16, costa 4, regata 23) cubre el máximo real (15, 3, 22).
- **Ola**: la fórmula de AC-SCN-12-01 es `waveH` de `water.frag:45-53` sin el ruido; ganancia `× (1 + h·A·0,25)` = `:68`. Arfada por promedio en 5,3/9,5/13,5 m: máximo calculado **0,111 m** (cumple el ± 0,12 que pide el test).
- **LT-124**: `[7] 0,8`, `[9] ancho/2`, `[26] 0,5`, `[27] 9,5 − 4,2 = 5,3` = `water_painter_test.dart:27-47`.
- **Preferencia**: `_touched` antes de comparar, igual → no escribe, desconocido → lago sin escribir, lectura demorada ignorada si hubo toque = `scene_settings.dart:26-42`. El patrón `SeriesPanelPrefs` ya existe y `IPreferences` tiene `IsLoaded`/`Loaded`.
- **Selector**: orden y separaciones (`Escenario`, 8, fila, 16, `Entrenamiento libre`) = `workout_screen.dart:79-83`; las 5 claves del catálogo ya existen en `strings.{es,en}.json`. En TV entran 2 tarjetas y asoman 39 dp de la 3.ª; en W1280 entran las 4 (638 dp).
- **Miniatura**: `visibleProps(20)` filtrado a `z < 170`, 12:00, tiempo 0, sin bote = `thumbnail_painter.dart:16-39` (DT8-02 manda la spec sobre D8-09). A 20 m la regata no llega al tope (≈ 171 de 200), así que filtrar después del tope da lo mismo que el legacy.
- **Cobertura**: los 40 AC tienen componente y test planificado (matriz §7.4 del diseño); LT-99 a LT-138 tienen archivo de test. Con esto queda portado todo `test/scene/`.
- **Contradicciones arte ↔ spec**: el diseño las resolvió todas a favor de la spec (§9.2): tribuna de 12 m, cifras de 0,55, sin fase en la costa, −40 % bajo el puente, sin columnas de luz ni árboles de vereda, talud desde 9,8.
- **Repo público y UTF-8**: los tres documentos sin rutas de la PC ni datos del equipo; UTF-8 sin doble codificación (0 apariciones de `Ã`).

## 2. Issues

| ID | Sev. | Resp. | Issue | Evidencia | Corrección sugerida |
|---|---|---|---|---|---|
| **QA-G8-01** | **mayor** | architect (+ designer) | **Los volúmenes 3D del río se cruzan entre sí y con el canal; el diseño no tiene regla ni test.** El legacy dibuja en 2D y nada choca; en 3D con las plantas del arte (edificio 6 × 10, club 8 × 12, puente con estribos hasta ±24) sí. Tampoco se dice dónde va el ancla en `z` de edificios y clubes (de eso depende la paridad ± 2 dp de AC-SCN-14-10: frente en el ancla ≠ volumen centrado). | Sonda, segmentos 0–2 000: **116 de 125 puentes** tienen un edificio o club en su `z` (183 objetos dentro de tablero/estribos); **63 faroles** (±9,5) quedan dentro de un club (frente del club en `|x| − 4s`, que llega a 9,0); **954 pares** de casas vecinas se interpenetran (prof. 10s/12s contra un paso de 0 a 38 m en el borde del segmento). La rampa del club (12°, de 1,2 a −0,2 m = **6,6 m**) no entra entre la cara del club (`|x|` 9,0–12,6) y el muro (9,0): si arranca en la cara, su pie llega a `|x|` **2,55**, en el carril del bote y sobre las boyas de ±3. Además, la spec dice "fachadas desde 13 m" (AC-SCN-16-04), pero el eje del legacy en 13–17 con 6·s de ancho pone el frente en 10,0–14,2 | Fijar en `PropPlacement` (Core, WP-A): (a) ancla = cara hacia la cámara en `z` del objeto para edificio y club (el volumen crece hacia +z); (b) el fondo de cada casa se recorta, sin tocar el generador, a `mín(fondo; z de la siguiente de esa orilla − 1 m)` y a la cara del puente; (c) el puente termina en el malecón (`|x|` ≤ 12,5) o los volúmenes en su tramo se recortan; (d) un club cuyo frente quede a `|x|` < 10,1 en el `z` de un farol se retrae a 10,1 (la paridad es del eje, no del frente); (e) la rampa entra al agua solo hasta `|x|` ≥ 8 (escalonada o paralela al muro). Test dotnet en `EnvironmentGeometryTests` sobre 100 km: ningún volumen del río cruza otro, el muro ni `|x|` < 8,5. La spec cambia "fachadas desde 13 m" por "el eje en 13–17 (frente desde ≈ 10)" |
| QA-G8-02 | menor | architect | `Glow` lleva "niebla aplicada" (§8.4), pero AC-SCN-20-04 pide las luces de noche "sin tinte ni niebla" (como `shore_painter.dart:194-196`) | 02 §8.4 vs 01 AC-SCN-20-04 | Halos y lámparas sin niebla; el haz del faro puede llevarla (es agregado) |
| QA-G8-03 | menor | po | Cada cambio de escenario en reposo hace "un dibujo de calentamiento del kit" (02 §3.2); AC-SCN-01-06 y -22-05 dicen "después, 0 dibujos" | 02 §3.2 vs 06a AC-SCN-01-06 | Precisar: un dibujo de calentamiento por kit armado, antes de su `Ready`; 0 después |
| QA-G8-04 | menor | architect | `ThumbnailTests` (huella de los PNG) y `RepoHygieneTests` (miniaturas versionadas) están en WP-A, pero los PNG nacen en WP-B1: el "dotnet verde" de WP-A no se puede cumplir | 02 §1.1, §8.1 | Esos dos casos entran en B1 (o `Ignore` con motivo hasta B1) |
| QA-G8-05 | menor | architect | `HorizonEnvironmentTests` pide "± 1 dp en TV contra el oráculo", pero el mapeo es lineal `px/411` rad: en el borde de TV hay 28,6° contra 26,6° reales (≈ 15 dp) | 02 §2.5; arte DA8-05 | Comparar en ángulo (oráculo pasado a `px/411`) o usar `atan`; la paridad de pantalla del horizonte no es un AC |
| QA-G8-06 | menor | architect | `TrailingLandmarks` lee "los segmentos en caché": arrancando con el puente ya detrás (p. ej. `E-280`) el segmento 5 nunca se generó y el puente no sale | 02 §2.2 | Que llame a `PropsForSegment` para los segmentos de `[d − 15; d + 1)` (no asigna) |
| QA-G8-07 | menor | designer | El faro (r 1,1 en `x` −14) queda medio afuera del promontorio cuya cara está en −14: media base sobre las rocas a 0,4–1,4 m | 02 DT8-10, §2.4; `shore_painter.dart:202-227` | El bloque de `z₀ + 25` del tramo del faro avanza hasta `x` ≤ −12,6 |
| QA-G8-08 | menor | designer | Torre de llegada 3 × 3 en −12 ocupa `|x|` 10,5–13,5: la mitad sobre el talud (9,8–11,95) | 02 §2.4 | Zócalo hasta 1,0 m o pie con la regla de `PropPlacement` |
| QA-G8-09 | menor | architect | En la costa, a la derecha no hay terreno que tape el borde del plano de agua (1,6 × 1,5 km): queda una franja de cielo de ≈ 1 dp bajo el horizonte. El arte pide el "faldón" (§3.2 H3) y el diseño no lo nombra | 06a/02 §WaterSurface; arte §3.2 | Sumar el faldón a `HorizonRenderer` o al agua de la costa |
| QA-G8-10 | menor | designer | Las boyas en ±3 (río y regata, r 0,25: borde interno 2,75) rozan la pala: la punta llega a ≈ 2,78 m con el remo perpendicular (`BoatSpec.Real`: perno 0,80 + 1,76 + 0,235) | `Core/Scene/Stroke/BoatSpec.cs:25` | Aceptar (3 cm a 9,5 m de la cámara) o radio visual 0,22 |
| QA-G8-11 | menor | po | AC-SCN-19-04 dice gaviotas "de 5 a 9 px de envergadura"; en el legacy `s = 5 + i` es la semienvergadura: 10 a 18 px | `coast_environment.dart:79-84` | Corregir el texto (el diseño ya usa escala relativa) |
| QA-G8-12 | menor | po | AC-SCN-22-03 anima "borde y ancho con `spring.snappy`"; el diseño (D8-11) anima anillo con opacidad y escala, sin `border-width` | 02 §4 | Ajustar el texto del AC a D8-11 |
| QA-G8-13 | menor | architect | La huella de miniaturas toma todo `Art/Shaders/**` (cambiar el vidrio del HUD en la 6b rompe el CI dotnet sin cambiar la imagen) y no toma lo de Unity que sí la cambia (materiales del *bootstrapper*, URP, `Volume`) | 02 DT8-16 | Limitar a `Art/Shaders/Scene/**` + `Include/**` y sumar la versión de `SceneMaterialsBuilder` |
| QA-G8-14 | menor | architect | `DistanceMarkerText` debe dar índices de glifo en un búfer reservado, no un `string` por segmento: el cambio de segmento cae dentro de la ventana de `FrameAllocations` | 02 §3.3, §5 | Glifos desde el entero con `DartMath`, sin formatear |
| QA-G8-15 | menor | architect | AC-SCN-21-05 ("la sesión no registra el escenario"; "solo en reposo") sin caso explícito | 02 §7.4 | Un caso en `EnvironmentSwitchTests` o en la prueba de guardado |

## 3. Matriz AC → test planificado (resumen)

| AC | dotnet | Unity / captura / dispositivo | Gate |
|---|---|---|---|
| 14-08, 14-09 (LT-99 a -109) | River/Coast/Regatta, Golden | — | OK |
| 14-10 | Horizon (apoyo) | EnvironmentParityTests, CAP-44 a -46 + LEG | OK con QA-G8-01 (ancla) |
| 16-01, -02, -03 (LT-110) | River, Bridge | BridgePassTests, CAP-43, VID-02 | OK |
| 16-04 | Geometry, Terrain | CAP-09 a -16 | **QA-G8-01** |
| 17-01 a -04 | Coast, WaterInputs, Geometry, Terrain | CAP-17 a -24, -42, VID-03 | OK (QA-G8-07) |
| 12-01 (LT-124), 06-04 | CoastSwell, WaterInputs, BoatRollAndCamera | CAP-42, VID-03, -05 | OK |
| 18-01 a -04 (LT-108) | Regatta, MarkerText, Geometry | CAP-25 a -32, -40, -41, VID-04 | OK |
| 19-03 a -05 | Horizon, SeagullFlight | CAP-12, -16, VID-03 | OK (QA-G8-05) |
| 20-03, -04 | Geometry, FacadeWindows, NightLights | CAP-12, -16, -24 | OK (QA-G8-02) |
| 20-05 (LT-111 a -123) | — | EnvironmentSceneTests | OK |
| 20-06, -07 | CrowdAtlas, Seagull, RepoHygiene | VID-01 a -04, EnvironmentSlot/Assets, `git ls-files` | OK |
| 21-02 a -04 (LT-125, -126) | SceneEnvironmentChoiceTests | EnvironmentPicker/RestartTests, VID-06, [Device-Win] | OK |
| 21-05 | — | EnvironmentSwitchTests, DEMO | OK (QA-G8-15) |
| 22-01 a -05 | Thumbnail | EnvironmentPickerTests, ThumbnailAssets, CAP-33 a -39, `[PERF]` | OK (QA-G8-03, -04) |
| 01-17 (LT-127 a -138), -18, -19 | — | EnvironmentScene/Switch, SceneBudget, FrameAllocations, `[PERF]`; Android NO VERIFICABLE | OK |
| WKT-12-02, SCN-21-01, SCN-14-05 (cambian) | Choice, Golden | WorkoutRestLayoutTests | OK |

## 4. Riesgos que quedan para la verificación

- SetPass en Medio: el lago ya mide 50 (el tope). Los kits no deben sumar *shaders* del lago; `SceneBudgetTests` por escenario desde B1.
- Horneado byte a byte en GPU: depende de la máquina; si difiere entre corridas, la huella sigue valiendo (es de entradas, no de píxeles).
- Precarga de las variantes de los 4 escenarios en el primer armado: medir la peor unidad con `[PERF]` (AC-WKT-13-15).

## 5. Para issues

- QA-G8-10 (pala contra boyas de ±3) si se acepta como está.
- QA-G8-13 (alcance de la huella) si no se corrige en B1.
