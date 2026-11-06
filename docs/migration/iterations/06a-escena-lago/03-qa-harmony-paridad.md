# It. 6a — Gate de armonía, lente "paridad"

| Campo | Valor |
|---|---|
| Autor | rowmate-qa |
| Fecha | 2026-11-02 |
| Iteración | 6a · `06a-escena-lago` |
| Lente | **Paridad con el legacy**: `scene_camera.dart`, `scene_state.dart` (velocidad y distancia), `time_of_day.dart`, `environment.dart`, `lake_environment.dart` (semillas, orilla, `Random` de Dart), `sky_painter.dart`, `shore_painter.dart`, `water_painter.dart` (respaldo), `scene_settings.dart`, `Hora escena` y los tests de la ROADMAP; Q-61 a Q-69; huecos del spec |
| Entrada | [01-po-spec](01-po-spec.md), [02-tech-design](02-tech-design.md), [04-design-notes](04-design-notes.md), [00-deuda-diseno](00-deuda-diseno.md), [00-deuda-5](00-deuda-5.md) |
| Legacy leído (solo lectura, `3ae0f3a`) | [rowmate-legacy](https://github.com/figuibej/rowmate-legacy): `lib/features/workout/scene/{scene_camera,scene_state,time_of_day,environment,scene_settings,scene_view,water_shader}.dart`, `scene/environments/lake_environment.dart`, `scene/painters/{sky_painter,water_painter}.dart`, `scene/painters/shore_painter.dart:1-160`, `scene/painters/boat_painter.dart:20-30`, `immersive_workout_screen.dart:60-100`, `workout_provider.dart:13`, `:50`, `:210-235`, `core/models/rowing_data.dart:1-30`, `lib/core/dev/simulator_overlay.dart:74-150`; `test/scene/{scene_camera,scene_state,time_of_day,environment,sky_shore_painter,water_painter,scene_view}_test.dart`. Del SDK: `<flutter-sdk>/bin/cache/pkg/sky_engine/lib/ui/painting.dart:424-454` (`Color.lerp`) y `lerp.dart:20-31`; `<flutter-sdk>/bin/cache/dart-sdk/lib/_internal/vm/lib/math_patch.dart:175-280` (`Random` de la VM). No se leyó `lib/core/strava/strava_secrets.dart`. `git -C ../rowmate-legacy status --porcelain` vacío al empezar y al terminar |
| Verificado | Un programa propio de .NET 8 en `<scratch>/it6a/gate-paridad/calc` (sin red, fuera del repo) que reimplementa desde el código del legacy y del SDK: el `Random` de la VM de Dart, `LakeEnvironment.generate`, `visibleProps`, la paleta con `Color.lerp`, `SceneState.update`, `lightFor` y el perfil de las cordilleras; y otro en `<scratch>/it6a/gate-paridad/ok` para una sonda de color (OKLab). `dotnet test` del repo sin cambios (§1). Unity no se abrió |
| Veredicto | **APROBADO CON OBSERVACIONES** (0 bloqueantes, 2 mayores, 8 menores; §4) |

---

## 0. Resumen

1. **El contrato del legacy está bien especificado y bien diseñado.** Los 20 casos muestreados (§2) coinciden con el legacy: los vectores dorados del generador, la semilla del segmento 7, los pinos del segmento 0, los muelles y las cabañas, los visibles a ocho distancias (92 / 95 / máx. 102 en 100 km), la velocidad, la inercia y la frenada (LT-66, LT-67, R-4, la pausa), los ambientes de referencia, los umbrales de noche, de las luces y de las estrellas al minuto, la primera estrella, el astro del legacy, la fuerza, las líneas del agua, la fase de la luna y la demo con `Fuerte`. La descripción de `DartRandom` (§2.10 del diseño) coincide con `math_patch.dart` del SDK (mezcla de Wang, 4 giros, MWC con `0xFFFFDA61`, rechazo en `NextInt`, `NextDouble` de 26 + 27 bits).
2. **Los 26 casos legacy (LT-62 a LT-87) están todos**, con sus afirmaciones y en el lugar correcto (dotnet o PlayMode) (§3). Lo que la ROADMAP deja para la 7 y la 8 está bien excluido.
3. **Q-61 a Q-69 están aplicadas** sin desvíos de producto (§5).
4. **Lo que hay que corregir** es de alineación, no de contrato: las diferencias visibles que trae el Designer (sol a la mitad del tamaño, cordilleras más bajas en vertical, nieve, líneas del agua que se apagan, fundido de estrellas, degradé del cielo, *scrims*, halo de la luna) **no tienen DV** y chocan con AC que tienen números explícitos (QA-6a-PAR-01); los *scrims* dentro de la escena rompen la comparación con LEG y la sonda de cielo del propio Designer (QA-6a-PAR-02). El resto son precisiones de texto y de tolerancia.

---

## 1. Corridas

| Qué | Resultado |
|---|---|
| `dotnet test` del repo (wrapper `tools/dotnet/run-tests.ps1`, sin cambios de código de la 6a) | herramientas **192 / 192**; el resto **2547: 2513 pasan, 0 fallan, 34 ignoradas** (igual que el cierre de la deuda de la 5, [00-deuda-5 §0](00-deuda-5.md)) |
| Programa de paridad (`<scratch>/it6a/gate-paridad/calc`) | §2 (todas las cifras de esta nota salen de ahí, salvo que se diga otra cosa) |
| Legacy | `git -C ../rowmate-legacy status --porcelain` vacío antes y después |

---

## 2. Muestreo: casos del legacy contra la spec y el diseño

Cada fila se calculó con el código del legacy portado línea a línea en el programa de `<scratch>` (no con los documentos).

| # | Caso del legacy | Valor (medido) | Spec | Diseño / notas | Resultado |
|---|---|---|---|---|---|
| 1 | `math.Random(42)`: `nextDouble`, `nextDouble`, `nextInt(1000)` (`sky_painter.dart:59-64`, ADR 012) | `0.15092545597797424`, `0.6041479725361217`, `77` | AC-SCN-14-02 | §2.10, DRN | OK |
| 2 | Semilla del segmento 7 del lago (`environment.dart:77`) | `0·1000003 + 7·7919 + 17 = 55450` → `0.854835531354132` | AC-SCN-14-02 | DRN | OK |
| 3 | Segmento 0 (`lake_environment.dart:22-34`), `(x; z; escala; semilla)` | izq. `(−15,899; 0,565; 0,760; 992)`, `(−12,258; 9,748; 1,265; 197)`, `(−11,767; 19,441; 0,784; 234)`; der. `(12,552; 0,720; 1,001; 329)`, `(13,236; 12,068; 0,706; 340)`; 9 a 14 pinos por segmento (11,48 en promedio, 0 a 2 000) | AC-SCN-15-02 | 00-deuda-diseno §6.1 (idénticos) | OK |
| 4 | Muelles y cabañas (`lake_environment.dart:35-38`) | muelles en 175, 575, 975, 1 375 (`x` −9); cabañas en 360, 960, 1 560 (`x` 13, semilla `k`) | AC-SCN-15-03 | LAK | OK |
| 5 | `visibleProps` (`environment.dart:89-114`) | 92 (0 m), 93 (1), 94 (49,5 y 50), 98 (920), 95 (1 234), 91 (5 000), 92 (42 195); en 100 km cada 1 m: **mín. 86, máx. 102** (a 7 291 m): el tope 120 nunca actúa | AC-SCN-14-04, AC-SCN-15-04 ("≈ 90 a 100") | LAK "92 a 0 m, 95 a 1 234 m, ≤ 102" | OK (la spec dice "≈ 90 a 100": hay tramos con 86) |
| 6 | `E-920` | cabaña a `relZ` 40 (der.), muelle a `relZ` 55 (izq.); con `E-0`, muelle a 175 y cabaña a 360 | §0.4, CAP-07 | §16.1 (x 131,2 y 321,8 en V390) | OK |
| 7 | `scene_state_test:14` y `:26` (pasos de 0,05 s) | a los 8 s, 4,6523 m/s y 30,64 m; 6 s con split 0 → 0,0796 m/s (la distancia crece); 10 s más → **0 exacto** | LT-66, LT-67 | SST | OK |
| 8 | `SceneState.update` con R-4 a 1/60 s | 60 s → 234,07 m; `Pausa` a los 60,2 s: +2 s → 1,0465 m/s, se desliza 5,92 m, 0 a los 8,95 s | AC-SCN-03-02, -03 | SST | OK |
| 9 | `paletteFor` (`time_of_day.dart:100-116`), ambiente | 4:30 → 0,5249999999999999; 6 → 0,825; 9 → 0,97; 15 → 0,925; 18:30 → 0,7625; 19 → 0,675; 21 → 0,425; 18:00 → **0,85 exacto** | AC-SCN-09-03 | STD | OK |
| 10 | `isNight` al minuto (`time_of_day.dart:52-55`, `scene_view.dart:81-86`) | 19:34 día, 19:35 noche; 4:25 noche, 4:26 día; continuo 19:34:19 y 4:25:41 | AC-SCN-10-01 | `NightStartHour`, `NightEndHour` | OK |
| 11 | Estrellas con Q-62 (`sky_painter.dart:51`) al minuto | 17:59 no (0,8504); **18:00 sí** (0,85); **6:12 sí** (0,8499999999999999); 6:13 no (0,8521) | AC-SCN-11-04 ("aparecen a las 18:00 y se van a las 6:12") | STR "18:00 sí, 6:13 no" | OK (precisión de texto: QA-6a-PAR-09) |
| 12 | Primera estrella en V390 (`sky_painter.dart:62-64`) | (58,8609; 173,3663), radio 1,2617 | AC-SCN-11-03 (58,86; 173,37) | STR | OK |
| 13 | Luces de la noche (`shore_painter.dart:26`) | ambiente < 0,6 de 19:25:43 a 4:42:51; al minuto, encendidas de 19:26 a 4:42 | AC-SCN-09-06 | NLT (19:26 y 4:43) | OK |
| 14 | `lightFor` (`time_of_day.dart:57-67`) | 5 h sol 0,2083 / 0; 9 h 0,375 / 0,7071; 12 h 0,5 / 1; 18 h 0,75 / 0; 19 h 0,7917 / 0; 20 h luna 0,3333 / 0,5; 22 h 0,4167 / 0,866; 0 h 0,5 / 1; 4 h 0,6667 / 0,5 | AC-SCN-10-02 | CEL, `scene_light.json` | OK |
| 15 | Fuerza (`time_of_day.dart:64`) | 5 h y 18 h 0,2; 9 h 0,849; 12 h 1,0; noche 0,45; 1,0 de 9:45:46 a 14:14:14 | AC-SCN-10-03 | `LegacyStrength` | OK |
| 16 | Astro que se ve (DV-52): acimut `(fx − 0,5)·70` y elevación | 6 h −17,5° / 1,5°; 9 h −8,75° / 12,46° (8,92° con 10,5°); 12 h 0° / 17° (12°); 15 h +8,75° / 12,46°; 22 h −5,83° / 14,92° (10,59°) | AC-SCN-10-04, CAP-04, CAP-11 | CEL; 04-design-notes §3.1 (b) | OK (con los dos arcos) |
| 17 | Respaldo del agua (`water_painter.dart:92-103`) a 0 m y 12 h | línea 0 en `t` 0, medio ancho 0,15·ancho, alfa 0,15000000000000002 | AC-SCN-13-03 | DWT | OK |
| 18 | Fase de la luna (DV-57; referencia 2000-01-06 18:14 UTC, 29,530588853 d) con la fecha de prueba | edad 17,157 d, iluminación 0,9366, menguante | AC-SCN-10-07 (≈ 94 %, gibosa menguante) | MOO | OK |
| 19 | Cordilleras (`lake_environment.dart:43-85`) a `d` 0, 48 pasos | lejana: máx. **0,8896·hMax** en `i` 7 (acimut −24,8° con 70° por ancho), pico central 0,657; cercana: 0,825 y 0,745. Picos con nieve (> 0,75 y máximo local): **uno** a `d` 0; en V390, de 0 a 3 000 m cada 10 m: 0 picos en 34 muestras, 1 en 249, 2 en 18 | AC-SCN-19-01 | 04-design-notes §6.1 (macizo −25,4°, 0,892; central −2,3°, 0,658; "el 75 % deja un solo pico") | OK (con la precisión de QA-6a-PAR-03) |
| 20 | Demo con el simulador (`RowingSimulator.cs:112`, `500·(2,8/W)^(1/3)`) | `Fuerte` 280 W → 108 s → 4,63 m/s: el muelle pasa la cámara a los 39,3 s y la cabaña a los 79,3 s; 150 W → 133 s → 3,76 m/s: 48,0 s y 97,3 s | §1 (a) paso 3 | — | OK |

**Además** (sin fila propia): `Color.lerp` del SDK aplica `clampDouble(…; 0; 1)` por canal; en un barrido de 1 440 minutos × 7 colores, con y sin el *clamp*, **0 diferencias** (QA-6a-PAR-06). `RowingData.pace500mSeconds` es `int` y `strokeRate` `double` en el legacy y en Unity (`Core/Models/RowingData.cs:14`, `:23`); `WorkoutPhase` tiene las mismas cuatro fases (`workout_provider.dart:13`, `Core/Workout/WorkoutPhase.cs`), así que "en marcha" = `Active` es exacto (AC-SCN-03-07). `SceneSettings.HourOverride` es `int?` y el legacy solo escribe horas enteras (`simulator_overlay.dart:143-144`): sin pérdida.

---

## 3. Los 26 casos legacy

| LT | Legacy | Afirmación del legacy | Spec | Test del diseño | ¿Fiel? |
|---|---|---|---|---|---|
| LT-62 | `scene_camera_test:9` | horizonte 844·0,40 y 390·0,42 (± 0,001); focal 390 y 390 | AC-SCN-02-01 | `SCC.HorizonAndFocalByOrientation` (dotnet) | Sí |
| LT-63 | `:16` | `(3;0;200)` más arriba que `(3;0;10)`, bajo el horizonte, más centrado, menor escala; `1e9` → punto de fuga ± 0,01 | AC-SCN-02-02 | `SCC.FarIsCloser…` | Sí |
| LT-64 | `:30` | `z` 0,2 → null | AC-SCN-02-03 | `SCC.BehindTheCamera…` | Sí |
| LT-65 | `:34` | ida y vuelta en H844 para `(−4;6)`, `(0;30)`, `(7,5;120)`, error < 1e-6 | AC-SCN-02-02 | `SCC.UnprojectWater…` | Sí |
| LT-66 | `scene_state_test:14` | objetivo 4,67 ± 0,01; tres ceros; 8 s en pasos de 0,05 → 4,67 ± 0,05 y > 25 m | AC-SCN-03-01, -02 | `SST.SpeedComesFromTheSplit…` | Sí (los pasos tienen que ser `SceneState.Update(0,05)`, no `SceneSession.Advance` de 1/60) |
| LT-67 | `:26` | 8 s + 6 s con split 0 → < 0,1 y la distancia creció; 10 s más → 0 exacto | AC-SCN-03-03 | `SST.StoppingDecelerates…` | Sí |
| LT-68 | `:55` | 5 000 → 904 | AC-SCN-03-06 | `SST.ShaderDistanceWrapsAt4096` (con `ForceForTest`) | Sí |
| LT-69 a LT-74 | `time_of_day_test:9` a `:44` | los seis, con sus tolerancias (`1e-9`, `0,01`, igualdad en `0,35` y en la fuerza `1,0`) | AC-SCN-09-02, -03; AC-SCN-10-01 a -03 | `STD.*` | Sí (`strength == 1,0` a las 12 es exacto en cualquier `libm`: `clamp(sin(π/2)·1,2; 0,2; 1)`) |
| LT-75 a LT-77 | `environment_test:10`, `:17`, `:30` con `EnvironmentId.lake` | determinista (tipo, x, z, semilla) y ≠ segmento 8; 0 a 29 no vacíos, `z` en el segmento, `|x| ≥ 9`; a 1 234: ≤ 120, `z` no creciente, en [1 235; 1 634] | AC-SCN-14-01, -03, -04; AC-SCN-15-04 | `SHS.*` | Sí |
| LT-78 a LT-81 | `sky_shore_painter_test:16` (lago, 2 tamaños × 12 y 23 h) | cielo + orilla a 0 y 1 234 m, `t` 3,2, sin excepciones | AC-SCN-11-07 | `SLC.LakePaintsWithoutExceptions(…)` (PlayMode) | Sí |
| LT-82 | `:39` | `circles(19) − circles(12) ≥ 80` con `t` 0 | AC-SCN-11-04 | `STR.StarsAppearAsSoonAsItDarkens` (estrellas visibles: 80 contra 0) | Sí (el legacy cuenta además disco y halo, que se cancelan) |
| LT-83 | `water_painter_test:11` | respaldo en V390, 12 h, lago, `SceneState()` nuevo, sin excepciones | AC-SCN-13-04 | `SLC.DegradedWaterPaintsOnAFreshScene` | Sí |
| LT-84 a LT-87 | `scene_view_test:11` (lago) | SPM 24, split 110, 300 m, `isActive`, hora forzada; 6 `pump` de 100 ms; desmontar sin excepciones | AC-SCN-01-05 | `SLC.LakeAnimatesWithoutExceptions(…)` (también cerrando mientras carga) | Sí |

Fuera de la 6a, bien excluidos: `scene_state_test` :37, :46, :60 (la parte de velocidad sí está en AC-SCN-03-03), :69 (It. 7); `environment_test` de río, costa y regata, :42, :54, :60 (It. 8); `sky_shore_painter_test:57` (necesita el puente y el cartel, It. 8); `water_painter_test:27` (uniforms con la costa, It. 8).

---

## 4. Issues

| ID | Sev. | Resp. | Issue | Evidencia | Corrección sugerida |
|---|---|---|---|---|---|
| **QA-6a-PAR-01** | mayor | po | **Las diferencias visibles con el legacy que traen el Designer y §8.2 del diseño no tienen DV**, y varias chocan con AC que hoy tienen números explícitos: con las notas aceptadas, esos AC fallarían en la verificación tal como están escritos | 04-design-notes §1.1, §3.3, §4.2, §4.3, §6.2, §6.5, §8.1 paso 2b, §12.3 y §21.1; spec §11.1 (DV-52 a DV-62). Lista: (1) **disco del sol de 21,8 dp y halo de 87 dp en V390** contra 44 y 176 del legacy (`sky_painter.dart:73-85`; AC-SCN-11-05); (2) **halo de la luna de 0,8× y × (0,4 + 0,6·iluminación)** contra "el mismo halo" (AC-SCN-10-07; el legacy usa `r·4` también para la luna); (3) **cordilleras en vertical a 9°** (≈ 18 % del cielo, ≈ 60 % del alto del legacy) (AC-SCN-19-01, P-11); (4) **nieve donde el perfil pasa 0,58** contra los picos locales sobre 0,75 (a `d` 0, un solo pico; §2 fila 19); (5) **líneas del agua × la visibilidad del astro**: entre ≈ 18:30 y 20:36 y entre ≈ 3:24 y 5:30 se apagan, y a las 19:34 no hay ninguna (el legacy nunca baja de alfa 0,05; AC-SCN-13-03, -04 "sin sacar ni mover lo del legacy"); (6) **fundido de 0,9 s de las estrellas**; (7) **degradé del cielo `1 − (1 − q)²` con la banda de bruma** contra el lineal de `y` 0 al horizonte (`sky_painter.dart:39-48`); (8) **los *scrims*** (C-18), que el legacy no tiene; (9) **el hemisferio sur de la luna** (C-19); (10) DV-52 con el arco de 12° (C-01) | Si el gate acepta las notas: sumar DV-63 en adelante (o ampliar DV-52, -57, -59) con "Legacy (verificado) / App / Motivo / AC" para cada punto, y cambiar los textos de AC según 04-design-notes §21.1. Si alguna se rechaza, el Designer construye la spec en ese punto. "Para mostrarle al usuario" (spec §11.1) tiene que sumar el sol más chico y las montañas más bajas en vertical |
| **QA-6a-PAR-02** | mayor | designer | **Los *scrims* van dentro de la cámara de la escena y siguen en las tomas de "escena sola"** (CAP-01 a -12, VID-05, -06, -10): oscurecen el 24 % de arriba y el 18 % de abajo de las tomas que se comparan con LEG-01 a -07, y hacen fallar por construcción la sonda "Cielo de arriba" de las propias notas y AC-SCN-11-02 ("el tono de arriba … es el de la paleta") | 04-design-notes §12.3 (cola 3500, antes del post) y §10.4 (TV (205; 8), 12 h, escena sola, ΔE OK ≤ 0,05 de `#0B4FA8`); 02-tech-design §3.3 y §5.1 (`HideForCapture` solo oculta el HUD). En (205; 8) el *scrim* vale 0,38·(1 − 8/219,36) = 0,366 de `#06122A`: el cielo queda en `#09397A` (mezcla sRGB, ΔE OK 0,096) o `#09408B` (mezcla lineal, ΔE OK 0,061), los dos por encima de 0,05 antes de cualquier post. ART_BIBLE §3.2 los define "detrás del HUD, delante de la escena": son de la capa del HUD | Que `HideForCapture` (escena sola) apague también `ScreenOverlay`, o que el arnés tenga un interruptor propio; las sondas de §10.4 y la comparación con LEG se miden sin *scrims*. WP-D2 no puede compensar el *scrim* con la exposición de la hora. Si los *scrims* se quedan en "escena sola", el PO cambia AC-SCN-11-02 y la sonda pasa a la composición con el *scrim* |
| **QA-6a-PAR-03** | menor | designer | **El alto de las cordilleras no reproduce el del legacy en horizontal, y "el pico más alto ≈ 30 %" no es el del legacy.** El legacy usa `hMax = 0,12·alto` de pantalla (`lake_environment.dart:46`), o sea `τ = 0,12·alto/focal`: 30 % del cielo en vertical, pero **28,6 %** en horizontal (0,12/0,42). La fórmula de las notas, `tan Θ = mín(0,30·horizonte/focal; tan 9°)`, da 7,18° en horizontal contra **6,84°** del legacy (+5 %): "E es el legacy exacto en horizontal" no es exacto. Además el perfil congelado a `d` 0 (DV-55) llega a 0,89·hMax: el pico más alto del lejano queda en **26,7 %** del cielo en V390 y **25,4 %** en H844, no en 30 % | §2 fila 19; 04-design-notes §6.2 y §21.1; spec AC-SCN-19-01 y P-11 | `tan Θ = mín(0,12·alto/focal; tan 9°)` (idéntico al legacy en las dos orientaciones antes del tope) y el PO escribe en AC-SCN-19-01 "el alto máximo del perfil es el 12 % del alto de pantalla (30 % del cielo en vertical, 28,6 % en horizontal); el pico más alto del lejano, 0,89 de ese alto" |
| **QA-6a-PAR-04** | menor | po | **DV-62 y AC-SCN-02-03 describen mal el corte cercano del legacy.** Para los objetos de la orilla el legacy los saca en `relZ < 1 m` (`visibleProps`: `zMin = distance + 1`, `environment.dart:90`), no en 0,5 m: el `project` con `minZ` 0,5 solo actúa con objetos pasados a mano (`sky_shore_painter_test:57`, It. 8). Y el ejemplo "un pino de la orilla en un teléfono horizontal" no pasa en el lago: la orilla (`|x|` 9) sale del cuadro a `relZ` 8,1 m en TH, 10,1 en W1280 y 18 en TV, y la punta del muelle (`x` −6) a 5,4 m en TH | `environment.dart:90-98`; `shore_painter.dart:32-33`; §2 (cálculo del cuadro); 02-tech-design §3.6 (`ShoreStreamer` toma `[d + 1; d + 400]` "más una banda de fundido"); 04-design-notes §7.6 ("casi nunca se ve") | El PO corrige DV-62 ("se cortan al pasar 1 m, y antes salen del cuadro por el costado") y el ejemplo de AC-SCN-02-03. El Architect deja explícito en §3.6 de qué lista sale el tramado de 2 a 0,5 m: con `[d + 1; d + 400]` solo puede actuar de 2 a 1 m; o una banda cercana propia del *streamer* desde `d + 0,5`, sin tocar `VisibleProps` (LT-77) |
| **QA-6a-PAR-05** | menor | architect | **Igualdad bit a bit esperada en valores que pasan por `Math.Sin`/`Asin`/`Exp`/`Pow`.** El diseño dice "error ≤ 1e-9 (se espera 0)" para la paleta y el astro, y `scene_palette.json` guarda `sunElevation` "en bits de `double`". El oráculo corre en la VM de Dart en Windows y el CI corre dotnet en `ubuntu-latest` **y** `windows-latest`: `sin` de glibc y de la UCRT pueden diferir en el último bit. El proyecto ya lo vivió con `pow` (R-T4) | 02-tech-design §2.4, §6.2; `.github/workflows` (matriz `ubuntu-latest`, `windows-latest`); `Assets/RowMate/Simulator/RowingSimulator.cs:109` ("Math.Pow puede diferir en el último bit … `Within(1e-9)`") | Bit a bit solo en lo que es aritmética pura: `DartRandom`, los segmentos (FNV), la paleta (colores y ambiente), las listas visibles, `ShoreShading`, `DegradedWater`, `StarField.Layout`, `CloudModel`. Con tolerancia (`Within(1e-12)` absoluto o el `1e-9` de la spec) todo lo que pasa por funciones trascendentes: `SunElevation`, `LightFor`, `HorizonModel.LegacyProfile`, `NightStartHour`/`EndHour`, `MoonPhase`, `ApproachScale`, `CelestialPlacement`. Decirlo en §6.2 y en `SceneGoldenTests` |
| **QA-6a-PAR-06** | menor | architect | **`SceneColor.Lerp` sin el *clamp* de Flutter.** `Color.lerp` hace `clampDouble(_lerpDouble(a; b; t); 0; 1)` en cada canal; la spec (AC-SCN-09-02) y el diseño (§2.4) solo dan `a·(1 − t) + b·t` | `<flutter-sdk>/bin/cache/pkg/sky_engine/lib/ui/painting.dart:449-452`; barrido de 1 440 minutos × 7 colores con y sin *clamp*: 0 diferencias hoy | Agregar el *clamp* a `SceneColor.Lerp` (cuesta nada y lo usan también `horizonColor`, el cuerpo de la nube, la nieve y `ShoreShading`); el PO lo nombra en AC-SCN-09-02 |
| **QA-6a-PAR-07** | menor | designer | **Fila de las 19:34 de la tabla §3.1 (b): "Fuerza del legacy 0,45".** Al minuto 19:34 el legacy todavía es de día (`isNight(19:34)` = falso; la noche empieza a las 19:34:19 y al minuto, a las 19:35): la fuerza es **0,20** | `time_of_day.dart:55`, `:64`; §2 filas 10 y 15 | Corregir la fila (0,20 a las 19:34; 0,45 desde las 19:35). Las pruebas toman la fuerza del oráculo, no de la tabla |
| **QA-6a-PAR-08** | menor | designer | **Hueco: las nubes "saltan" en el borde al dar la vuelta.** El legacy las envuelve en ±0,1·ancho, pero las grandes ocupan de `x − 1,3·s` a `x + 1,15·s`: con `s` 34 a 40 y 0,1·ancho de 36 a 41 dp (V390, P360, TV), un pedazo blando desaparece a la derecha y aparece a la izquierda en un cuadro. El legacy lo hace igual; la spec no lo trata y la mejora EXR pide que nada aparezca de golpe (§10.7) | `sky_painter.dart:95-101`; 04-design-notes §5.1 (los extremos de la nube) | Conservar la `x` del legacy (contrato) y bajar el alfa de la nube cerca del punto de vuelta (p. ej., × `S` en el último `1,3·s` de cada lado), o que el PO lo registre como paridad. Sin cambio de dato |
| **QA-6a-PAR-09** | menor | po | **AC-SCN-11-04: "se van a las 6:12" es ambiguo** y el borde es frágil: a las 6:12 el ambiente vale 0,8499999999999999 (se ven) y a las 6:13, 0,8521 (no); a las 18:00 vale 0,85 exacto (se ven) y a las 17:59, 0,8504 (no). Cualquier reordenamiento de `a + (b − a)·t` o de `h + m/60` puede mover el minuto | §2 fila 11; `time_of_day.dart:107-116`; 02-tech-design §6.3 STR ("18:00 sí, 6:13 no, 15:00 no") | Texto: "visibles hasta las 6:12 inclusive; a las 6:13 y a las 17:59, ninguna; a las 18:00, sí". STR suma 6:12 sí y 17:59 no, con la hora armada como el legacy (`hora + minuto/60`) |
| **QA-6a-PAR-10** | menor | architect | **Dos AC sin prueba de dotnet que la pueden tener** (de la lista "sin test"): AC-SCN-11-02 y AC-SCN-11-05 quedan solo con captura, pero su parte de contrato es pura: los extremos del degradé del telón (`q` 0 → `skyHorizon`, `q` 1 → `skyTop`, antes de la bruma y del *scrim*) y las proporciones del legacy que el Designer conserva (halo = 4 × disco; luna = 0,8 × sol) | 04-design-notes §3.3 (DN6a-09, en el *shader*) y §4.2-§4.3; 02-tech-design §6.5 (AC-SCN-11-02: "cap qa"; AC-SCN-11-05: "cap qa") | Dejar la curva del telón como función de Core (`SkyBackdrop.Gradient(q)` o en `HourLighting`) que el *shader* copia, con su prueba de extremos y monotonía; y una prueba de `SkyArt` sobre las dos proporciones |

---

## 5. Q-61 a Q-69

| Q | Decisión (FEATURE_INVENTORY §4.6) | Dónde queda | Lo verificado |
|---|---|---|---|
| Q-61 (revisada) | Mismo generador, mismos objetos, tamaños y variantes a la misma distancia | AC-SCN-14-02, AC-SCN-15-05; `DartRandom`, `LakeEnvironment`, `ShoreSegments`, `SCN` marcas ± 2 dp | Vectores dorados, semilla k = 7, segmento 0, hitos y visibles (§2 filas 1-6). `ShoreFill` usa semillas propias y no consume el generador (02-tech-design §2.13): correcto. La cabaña y el muelle conservan el ancla (DT6a-08) |
| Q-62 | Estrellas solo con ambiente ≤ 0,85 | AC-SCN-11-04 (DV-53), `StarField.Visibility` | 18:00 y 6:12 incluidos (§2 fila 11); QA-6a-PAR-09 |
| Q-63 | Puesta y salida graduales con los umbrales del legacy | AC-SCN-10-05 (DV-54), `CelestialPlacement` | Umbrales 19:34:19 y 4:25:41 sin cambios; las ventanas del diseño (S(4,45; 5,50), 1 − S(18,50; 19,55), S(19,5719; 20,6), S(4,4281; 3,40)) dan los minutos de la spec. El tope de 0,2°/min es inalcanzable (DT6a-02, ya en el gate) |
| Q-64 | — | It. 8 | — |
| Q-65 | El horizonte se acerca, no se desliza | AC-SCN-19-02 (DV-55), `HorizonModel.ApproachScale` | El deslizamiento del legacy (0,24 y 0,48 dp/m) queda como contrato sin dibujarse: correcto |
| Q-66 | Una sola cámara; respiración como mejora | AC-SCN-02-04, -08 | Sin entrada de usuario (prueba de código) |
| Q-67 | El agua degradada existe | AC-SCN-13-01 a -04 | Tres tramos y 10 líneas del legacy (§2 fila 17); QA-6a-PAR-01 punto 5 |
| Q-68 | Sin saltos a ninguna distancia | AC-SCN-01-15, `DegradedWater.Phase` en `double`, la cinta | `shaderDistance` queda como contrato (LT-68) y el agua no lo usa |
| Q-69 | Reglas en Core puro con dotnet | AC-SCN-01-14 | §2.0 del diseño (todo `noEngineReferences`, sin reloj) |

---

## 6. Contradicciones y huecos, vistos desde la paridad

**Contradicciones QUÉ ↔ CÓMO ↔ diseño visual** (además de los 19 puntos C-01 a C-19 y DT6a-01 a DT6a-15, que ya están planteados para el gate y no se repiten):

- Los *scrims* contra AC-SCN-11-02 y la sonda §10.4 (QA-6a-PAR-02).
- AC-SCN-11-05, AC-SCN-10-07 ("el mismo halo"), AC-SCN-13-03/-04 y AC-SCN-19-01 tienen números del legacy que las notas cambian sin DV (QA-6a-PAR-01).
- DV-62 describe un corte del legacy que no es el que actúa en la orilla (QA-6a-PAR-04).

**Huecos del spec** (comportamiento del legacy que el PO no incluyó o no dejó preciso):

- La vuelta de las nubes en el borde (QA-6a-PAR-08).
- El *clamp* de `Color.lerp` (QA-6a-PAR-06).
- El alto real del perfil y del pico de las cordilleras (QA-6a-PAR-03).
- No se encontró otro comportamiento del legacy de los archivos de la lente que falte en la spec: la cámara (pose, proyección, inversa, `minZ`), la velocidad (objetivo, inercia, corte a 0, paso acotado en `scene_view.dart:63` y en `update`), la distancia propia, la hora (`hora + minuto/60`, la forzada), la paleta (8 keyframes con 22 → 28 → 29), el astro y su fuerza, las 80 estrellas, las 7 nubes (con la deriva por distancia como contrato), las cordilleras (perfil, tono, profundidad, nieve, deslizamiento como contrato), la orilla (semilla, orden de tiradas, rango, orden, tope y prioridad, olvido), el tinte y la niebla de la orilla, la noche de la orilla, el respaldo del agua y `Hora escena` (con `_nowHour()` entero, `% 24` y "Hora real") están todos con su AC y su prueba.

**La lista "sin test" del Architect**, con esta lente:

| AC | ¿Se acepta? |
|---|---|
| AC-SCN-01-09 (60 fps en Android) | Sí: NO VERIFICABLE sin teléfono ni módulo (regla 9, a la It. 14) |
| AC-SCN-01-10 (`[PERF]` en el build) | Sí: medida con argumentos, como en la 5 (`[Ignore] WindowsScenePerf` con motivo) |
| AC-WKT-13-15 y AC-WKT-14-19, parte `[PERF]`/Android | Sí: el orden y el presupuesto tienen PlayMode |
| AC-SCN-11-02 (degradé del cielo) | **Parcial**: su parte de contrato (los extremos) puede ser dotnet (QA-6a-PAR-10); el resto, captura y revisión, sin *scrims* (QA-6a-PAR-02) |
| AC-SCN-11-05 (sol y luna [VISUAL]) | **Parcial**: las proporciones del legacy pueden ser dotnet (QA-6a-PAR-10); el aspecto, captura |
| AC-SCN-09-05, parte "fotográfica" | Sí: los valores tienen dotnet; el aspecto es de QA y el Designer |
| AC-SCN-15-07, parte de aspecto | Sí: "nada flota" y el determinismo tienen prueba |
| AC-SCN-23-03, parte del build | Sí: en PlayMode, un anfitrión nuevo vuelve a `real`; el reinicio del `.exe`, en el equipo |
| AC-SCN-01-04, parte Android | Sí: NO VERIFICABLE sin teléfono |

---

## 7. Riesgos de paridad

| Riesgo | Mitigación |
|---|---|
| Un `i / 10` o un `minuto / 60` en C# con enteros (división entera) en `DegradedWater.Line` o `SceneHour.Effective`: el legacy divide en `double` | Las pruebas contra el oráculo (DWT, SHR con minutos distintos de 0) lo detectan; que existan con `i` de 1 a 9 y minutos no nulos |
| `%` de Dart (euclídeo) contra `%` de C# en horas negativas o mayores que 24 durante la transición de `Hora escena` | `DartMath.Mod` (diseño §2) y `SceneHourTransition.Evaluate` normaliza; SHR prueba 23 → 1 y 0 → 23 |
| `IL2CPP` ARM64 con FMA en `a·b + c` (R6a-03 del diseño) | Ya previsto: la autoprueba del build con los resúmenes de `scene_lake.json` (It. 14) |
| El orden de las tiradas del generador (x, escala, semilla, paso; izquierda y después derecha) | `scene_lake.json` con las filas completas de 0 a 99 y el resumen de 0 a 2 000 lo fija |

---

## 8. Matriz de los AC de contrato de esta lente (gate)

| AC | Componente | Test planificado | Oráculo | Gate |
|---|---|---|---|---|
| AC-SCN-02-01, -02, -03 | `SceneCamera`, `ChaseCameraProjection` | SCC (LT-62 a -65), SPR | — | OK (QA-6a-PAR-04 en el texto de -03) |
| AC-SCN-03-01 a -07 | `SceneState`, `SceneInputs` | SST (LT-66 a -68), SIN, SSN | — | OK |
| AC-SCN-09-01 a -03, -06 | `SceneHour`, `SceneTimeOfDay`, `NightLights` | SHR, STD (LT-69 a -74), NLT | `scene_palette.json` | OK (QA-6a-PAR-05, -06) |
| AC-SCN-10-01 a -03 | `SceneTimeOfDay`, `CelestialState.LegacyStrength` | STD, CEL | `scene_light.json` | OK (QA-6a-PAR-05, -07) |
| AC-SCN-10-04, -05, -07 | `CelestialPlacement`, `MoonPhase` | CEL, MOO | — | OK con C-01/DT6a-02 del gate; QA-6a-PAR-01 |
| AC-SCN-11-03, -04, -06 | `StarField`, `CloudModel` | STR (LT-82), CLD | `scene_sky.json` | OK (QA-6a-PAR-08, -09) |
| AC-SCN-11-02, -05 | `SkyRenderer` (telón) | captura | LEG-01 a -07 | QA-6a-PAR-01, -02, -10 |
| AC-SCN-14-01 a -06 | `DartRandom`, `ShoreSegments`, `LakeEnvironment` | DRN, SHS (LT-75 a -77), LAK | `scene_random.json`, `scene_lake.json` | OK |
| AC-SCN-15-01 a -05 | `LakeEnvironment`, `PropPlacement` | LAK, SHS, SPL, PPL | `scene_lake.json`, marcas | OK |
| AC-SCN-13-02, -03 | `DegradedWater` | DWT | `scene_water.json` | OK (QA-6a-PAR-01 punto 5) |
| AC-SCN-19-01 | `HorizonModel` | HZN | `scene_sky.json` (colores) | QA-6a-PAR-01, -03 |
| AC-SCN-20-01, -02 | `ShoreShading`, `NightLights` | SSH, NLT | `scene_sky.json` (`_c`) | OK |
| AC-SCN-23-01 a -04, AC-BLE-16-07 | `SceneHour`, `SceneHourTransition` | SHR, `SceneHourSimulatorTests` | — | OK |
| AC-SCN-11-07, AC-SCN-13-04, AC-SCN-01-05 | la escena URP | SLC (LT-78 a -81, -83 a -87) | — | OK |

---

## Verificación hecha

1. **Legacy, solo lectura**: los archivos de la cabecera; `git -C ../rowmate-legacy status --porcelain` vacío al empezar y al terminar; no se leyó el módulo de credenciales.
2. **Programas propios** en `<scratch>/it6a/gate-paridad/` (fuera del repo, sin red): `calc` (el `Random` de la VM, el lago, los visibles, la paleta, la escena, el astro, las cordilleras, la luna, las líneas del agua y el cuadro) y `ok` (la sonda OKLab de QA-6a-PAR-02).
3. **`dotnet test`** del repo: herramientas 192 / 192; el resto 2547: 2513 pasan, 0 fallan, 34 ignoradas.
4. **El repositorio**: solo se escribió este documento. Unity no se abrió. Sin *commits*. Rutas relativas, el legacy por su URL o `../rowmate-legacy`, `<scratch>` y `<flutter-sdk>`.
