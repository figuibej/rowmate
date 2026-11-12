# It. 6b — Verificación de QA (principal)

| Campo | Valor |
|---|---|
| Entrada | [01-po-spec](01-po-spec.md) (AC-6B-01 a -38), [02-tech-design](02-tech-design.md) (§10 y "Cambios por implementación" WP-A/WP-B), [04-design-notes](04-design-notes.md), [snapshots/](snapshots/) |
| Contra | El legacy [rowmate-legacy](https://github.com/figuibej/rowmate-legacy) (solo lectura; `git status --porcelain` vacío al empezar y al terminar) y lo cerrado en las It. 1-15 |
| Artefactos | `<scratch>/it6b/qa-r1/` (XML de NUnit, registros de Unity y de los players, recortes de las sondas, carga de la PC). Nada de eso se versiona |
| Pantalla en las medidas | **Modo de respaldo: 1024 × 768 a 60 Hz** (la ventana del player queda en 1024 × 720, no en W1280). Se midió igual, como pide la spec |
| Carga de otros procesos | 11-22 % de la CPU total en cada corrida de build y 12-38 % (media 15-21 %) en las de Unity. No se tocó ningún proceso |

## Veredicto: **RECHAZADO**

0 bloqueantes, **7 mayores**, 10 menores. **38 AC: 28 PASS, 7 FAIL, 3 NO VERIFICABLE.**

El agua avanzada funciona en los 4 escenarios y en los 3 niveles, la caída a la degradada (por falla o por la adaptación térmica) no corta la sesión y deja una sola línea, el vidrio funciona, los presupuestos del Editor se cumplen sin relajar topes y el CPU en régimen del build está muy por debajo de 16,7 ms. Las capturas son reproducibles (32/32 iguales byte a byte en una segunda corrida). **No se puede cerrar así** por tres cosas: la deuda #1 (AC-6B-01) sigue en FAIL en el build, que era lo primero de la spec; la entrega visual está incompleta (faltan los 5 videos, CAP-27 repite CAP-25, CAP-29 no es la toma pedida y faltan casi todas las sondas de la spec), y cuatro criterios visuales fallan con la medida de QA (-04, -14, -17, -27). Además, AC-6B-22 no tiene prueba.

**Para aprobar:** QA-V6b-01 a -07.

## 1. Pruebas (corridas de QA)

| Corrida | Resultado | Carga de otros procesos |
|---|---|---|
| `tools/dotnet/run-tests.ps1 -Matrix` | **4165: 4126 pasan, 0 fallan, 39 ignorados** (+194/194 del otro ensamblado); matriz: PASA 919, FALLA 0, PENDIENTE UNITY 204, EN EQUIPO 20, SIN TEST 0 | — |
| Unity EditMode | **4308: 4253 pasan, 0 fallan, 55 ignorados** | 17 % antes; 21 % de media, 30 % máx |
| Unity EditMode `-Simulator` | **4502: 4447 pasan, 0 fallan, 55 ignorados** | 23 % antes; 19 % de media |
| Unity PlayMode | **767: 766 pasan, 1 falla**: `BleAllocationTests.TheSonarDoesNotAllocatePerFrame` ("reloj de prueba 1 en 1 cuadros"), la inestable conocida de #71. La clase sola, otra vez: 4/4 | 12 % antes; 20 % de media, 32 % máx |
| Unity PlayMode `-Simulator`, corrida 1 | **848: 848 pasan** | 12 % antes; 18 % de media, 38 % máx |
| Unity PlayMode `-Simulator`, corrida 2 | **848: 848 pasan** | 12 % antes; 18 % de media, 29 % máx |
| `capture-snapshots.ps1 -Set it6b -Out <scratch>` | 32 tomas, **32/32 PNG iguales byte a byte** a las versionadas; las mismas 16 sondas de aspecto fallan; no escribe en otras carpetas | — |

Ningún test de las It. 1-15 se rompió. La única falla es la intermitente de #71 (sin relación con la 6b: no cambió ese código). La guardia de `ProjectSettings` dio "sin cambios" en todas las corridas.

Presupuestos del Editor (PlayMode, con la avanzada a la vista y el maniquí):

| Medida | Alto | Medio | Bajo | Tope |
|---|---|---|---|---|
| SetPass lago 12 / 22 h | 71 / 72 | 50 / 50 (también con el *slot*) | 36 / 37 | 80 / 50 / 50 (D-01) |
| SetPass río, costa y regata | 30-34 | 22-26 | 17-21 | ídem |
| Triángulos lago | 301 870 | 167 912 | 113 875 | 600 k / 350 k / 180 k |
| CPU de escena p95 | 1,42 ms (W1280) | 1,01 ms | 0,74 ms | 1,5 ms |
| Malla del agua | 12 816 | 4 536 | 4 536 | 20 k |
| Fragmento del agua (D3D11) | 82 ALU / 4 tex | 69 / 3 | 32 / 1 | 110/6, 70/5, 35/2 |
| `Foam` (#22) · `WaterDegraded` · `Glow` | 23 (tope 30) · 28 (tope 28) · 14 (tope 14) | | | |
| Memoria tras 10 vueltas | −0,9 % (`SceneMemoryTests`) | | | ± 5 % |
| HUD | 56-57 elementos (58 con la escena demorada, como antes) | | | 60 |
| Asignaciones (FrameAllocations) | 0 B remando, en pausa, de noche, al cambiar la hora y en los 3 escenarios | | | 0 B |

## 2. Rendimiento en el build (`[PERF]`, `Builds/Windows/simulator`)

**Pantalla: 1024 × 768 a 60 Hz (modo de respaldo).** En ese modo el player va a ≈ 13 cuadros/s y la GPU del cuadro informa ≈ 80 ms **en todas las corridas, también con el agua degradada forzada**: la GPU y el "ningún cuadro > 33 ms" de reloj quedan **NO VERIFICABLE** en esta PC; el CPU del hilo principal sí se mide.

| Medida | Resultado |
|---|---|
| `-PerfEarly -EarlySeconds 1` (5 lago + río, costa y regata, en frío) | Toque **1,39-3,05 ms** (8/8 ≤ 16,7) · peor cuadro con movimiento **43,7-52,4 ms** (8/8 > 16,7), siempre a los ≈ 541-544 ms del toque, en la unidad "sesión preparada"; negro quieto 32-39 ms |
| `-PerfWater` (Alto y Medio × 4 escenarios × 12 y 22 h) | Primera ventana (con la entrada): p99 **12,3-16,5 ms**, máx **21,8-25,4 ms**. Ventanas de régimen: p99 3,3-4,3 ms, máx ≤ 6,8 ms. La avanzada se dibuja en las 16 (el espejo en Alto); 0 líneas del agua; SetPass del build ≤ 77; memoria 131-137 MB; GC 0 en régimen |
| Base con la degradada forzada (lago 12 h, Alto) | Primera ventana p99 14,3 ms; régimen p99 4,3 ms: la primera ventana cuesta lo mismo con o sin la avanzada |
| Diálogo de completado (`-rowmate-perf-completion`, Alto / Medio) | Ventana del diálogo p99 10,1 / 8,7 ms, máx 12,3 / 11,7 ms. La primera (`Corta` con `StartRoutine` directo) p99 21,6 / 23,7 ms |
| `-rowmate-water-fail 15` (lago, Alto, 40 s) | **1** línea `[Scene] Shader de agua no disponible, usando fallback: falla forzada de prueba`; la ventana del cambio con máx 6,8 ms de CPU; la sesión sigue y se guarda (sale con 0); al final "Degraded … líneas del agua en el registro 1" |
| Adaptación sin `-rowmate-quality` (sin carga y con `-rowmate-perf-gpu-load 20`) | Orden: resolución ×5 (0,95 → 0,75) → reflejo → sombras → vegetación → nivel → **agua degradada** con su línea. Pasa también **sin carga**: con los ≈ 80 ms de GPU del modo de respaldo, la app llega a la degradada en menos de 60 s (QA-V6b-10) |
| GPU del desenfoque (≤ 0,6 ms en Medio) | Ninguna línea `[PERF] vidrio: GPU` (nadie lee el marcador `RowMate.HudBlur`) y la GPU no se puede medir en este modo |
| `-BleSmoke` | `[BLE] humo: estado=On busquedas=3 ftms=0 fallas=0 colgado=no soloFtms=si` y `[HRM] humo: busquedas=1 sensores=0 ajenos=0 fallas=0` → ok |
| Builds | `normal` (release) 0, sin rastros del simulador, `RowMateBle.dll`, VERSIONINFO 1.0.0; `simulator` (×2) y `dev-smoke` 0. `ProjectSettings` sin cambios y sin `ROWMATE_SIMULATOR` |

## 3. Matriz AC → evidencia → resultado

| AC | Evidencia | Resultado |
|---|---|---|
| 01 | Política en dotnet y Editor (9 verdes). Build: con movimiento 43,7-52,4 ms en 8/8 | **FAIL** (QA-V6b-01) |
| 02 | `PlayerLoopDispatchesUiBeforeLateUpdate`, `HeavyNeverSharesTheTapFrame`; build: toque 1,39-3,05 ms en 8/8, mín./máx. y carga anotados | PASS |
| 03 | `DegradedClampsToTenLines`; CAP-21: 10 líneas; la 11.ª ausente en CAP-21 a -24; `it6a` regenerado con la degradada | PASS (6 y 18 h no tienen sonda; CAP-22 a -24 cuentan 9 por el borde, de aspecto) |
| 04 | `TheMountainsStandOut`, `TheGoldenGlowIsStronger` en dotnet. Sin sondas en CAP-03 y CAP-17; QA en CAP-17: silueta central 1,01-1,07:1 contra el cielo y la bruma | **FAIL** (QA-V6b-05) |
| 05 | `DeviceCoverTests` 2/2 | PASS |
| 06 | `WaterModeSelectorTests` (una por fila), `UnsupportedSubShaderIsDetected` (2), `WaterAdvancedHasNoFallbackAndIsTagged`; build: forzada y térmica | PASS |
| 07 | `WaterFadeTests`, `AdvancedArrivesWithFadeKeepingTheScene` | PASS (falta VID-02, ver -33) |
| 08 | `WaterAvailabilityTests`, `FailureFadesToDegraded`, `DrawExceptionCutsInstantly`, `NoUserVisibleError`; build con `-rowmate-water-fail` | PASS (QA-V6b-11) |
| 09 | CAP-21 a -24 con la degradada; `DegradedWaterHasNoWake`; `OldSetsDrawDegradedWater`; los charcos se ven | PASS (QA-V6b-16) |
| 10 | `WaterModelTests.TheLegacyContractNumbers`, contrato de 28 entradas | PASS |
| 11 | `WaterProfilesTests`. QA: desvío de luminancia del agua cercana regata 0,052 ≤ lago 0,068 < río 0,149 < costa 0,182 | PASS |
| 12 | `HighDrawsBoatAndRower`, `MediumAndLowHaveNoPlanar`, `TheSilhouetteByAzimuth`. QA en CAP-31: agua bajo los pinos de la izquierda 12,5 % más oscura | PASS (el espejo no refleja las instancias, DD6b-06) |
| 13 | `ColumnFollowsAstroAndFades`. QA: columna más brillante a −9 a +17 dp del sol a las 12 h; luna más tenue que el sol en los 4. A las 6 y 18 h el sol queda tapado, salvo la costa a las 18 h (−42 dp, camino débil) | PASS (QA-V6b-14) |
| 14 | `WakeTrailTests` (dotnet), `WakeRendererTests`. Con la estela precargada se ve bien; en una sesión real (CAP-04 de `it7`) arranca con borde recto. Falta VID-03 | **FAIL** (QA-V6b-02) |
| 15 | `FogWeightCurve`, `FogReachesFogColorAt400`; costura 1/255 en la costa. Canal: no concluyente (11-13 columnas); sin `fogRow` | NO VERIFICABLE en captura (QA-V6b-08) |
| 16 | `MicroRingFieldTests`, `MarksByTier` (2 anillos en Alto, 0 en Bajo) | PASS (CAP-32 sale con "0 anillos") |
| 17 | `SwirlOrientationTests`. QA en CAP-32: contraste del charco 1,02-1,09:1; sin la sonda de 40° | **FAIL** (QA-V6b-03) |
| 18 | `LightColumnsTests` (7), `HourChangeWithoutJumps`; columnas en el cuadro: río 29, lago 1, costa 1 (la regata no tiene luces a las 22 h) | PASS (las columnas casi no se ven; falta VID-04) |
| 19 | `CrestFoamTests`, `CoastBuoySwellTests`, `TheCoastBuoyRidesTheSwell` | PASS (CAP-10 sin boya a la vista; falta VID-05) |
| 20 | `NoJumpAt4096`, `TapeMatchesWorld`; QA: CAP-30 (42 km) y CAP-31 (920 m) con medias por franja a ≤ 0,06 | PASS (falta VID-01) |
| 21 | `HullMeshTests`; CAP-31: variación de 13,1 % (≥ 8 %) | PASS |
| 22 | Ningún test con la categoría; `SceneLifecycleTests` no cambió y corre con la degradada (el valor por defecto) | **FAIL** (QA-V6b-06) |
| 23 | `GlassTintTests.Alpha`, `EveryGlassPieceShowsTheBlur` (Alto y Medio). QA: CAP-17 contra CAP-32, la variación dentro de la tarjeta es el 1,4 % de la de sin tarjeta | PASS (QA-V6b-13) |
| 24 | `CompositeIsMonotonic`, `ContrastWorstCase` (≥ 13,49:1, IA6b-02) | PASS |
| 25 | `LowUsesFallback`, `TierChangeCrossfades200Ms`, `BlurIndependentOfWater` | PASS |
| 26 | HUD sin elementos nuevos (56-57); GPU no medible (modo de respaldo) y sin la línea del marcador | NO VERIFICABLE (QA-V6b-09) |
| 27 | `TheCompletionDialogIsGlass`. QA en CAP-29: popa debajo del diálogo +4-5 % (pide ≥ 10 %); la toma no es `Corta` ni la aérea | **FAIL** (QA-V6b-04) |
| 28 | CPU del build: p99 ≤ 16,5 ms y máx ≤ 25,4 ms en 16 combinaciones; diálogo p99 ≤ 10,1 ms; 0 B en el Editor; GC 0 en régimen | PASS en CPU; GPU NO VERIFICABLE (QA-V6b-12) |
| 29 | Tabla del §1 | PASS |
| 30 | `TenEnvironmentRoundsReturnWithinFivePercent` (−0,9 %); build 120-137 MB | PASS |
| 31 | `OrderEndsInDegradedWater`, `ResolutionStillEveryTenSecondsAtMost`; build: el orden en el registro | PASS |
| 32 | — | NO VERIFICABLE (sin teléfono ni módulo de Android; #7) |
| 33 | Matriz 4 × 4 TV, TH, degradadas y niveles OK y reproducibles. Faltan VID-01 a -05; CAP-27 = CAP-25 byte a byte; CAP-29 fuera de la spec; faltan las sondas de -04, -11, -12, -13, -15, -17, -18, -19, -23 y -27 | **FAIL** (QA-V6b-07) |
| 34 | `HourChangeWithoutJumps` (la avanzada no se corta 18 → 20 → 22 h) | PASS (no mide saltos de píxel; falta VID-04) |
| 35 | dotnet `-Matrix`, EditMode y PlayMode, con y sin simulador (§1); los sets viejos con la degradada | PASS (la intermitente de #71) |
| 36 | Lo [dotnet] corre igual en EditMode | PASS |
| 37 | `WaterNormalFootprint`, `SameBytesTwiceAndSeamless`; `water-normal.png` procedural (semilla y SHA-256 en `water.json`); `THIRD_PARTY_NOTICES.md` sin cambios | PASS |
| 38 | `ProjectSettings` sin cambios y sin el *define*; `HorizonFar.mat`/`HorizonNear.mat` sin cambios; legacy limpio; búsqueda de rutas y datos del equipo limpia | PASS |

## 4. Issues

| ID | Sev. | Resp. | Qué | Evidencia | Corrección sugerida |
|---|---|---|---|---|---|
| QA-V6b-01 | mayor | architect | **AC-6B-01: la entrada temprana sigue con tirón en el build** (#1). Las pruebas del Editor pasan, pero en el player un cuadro con movimiento paga la unidad "sesión preparada" | `-PerfEarly -EarlySeconds 1`: 43,7 / 48,8 / 43,7 / 46,0 / 44,5 (lago), 44,5 (río), 43,9 (costa), 52,4 ms (regata), todos a los ≈ 541-544 ms del toque, con "unidad sesión preparada"; carga externa 11-22 % (3× el tope: no lo explica la carga) | Esa unidad no puede caer en un cuadro visible: partirla o llevarla al negro o al reposo, y una prueba que la mida en el player (IL2CPP/Mono, primera vez) |
| QA-V6b-02 | mayor | designer | **AC-6B-14: en una sesión real la estela no nace en la popa.** Sin `PrimeWakeForTest`, los brazos arrancan con un borde recto a ≈ 4,5 m de la cámara, no se apagan hacia ella y el agua tiene un corte horizontal en la fila de la popa. Las tomas de la 6b no lo muestran porque precargan la estela | `07-remero-palada/snapshots/CAP-04-charcos-tv.png`: corte a y ≈ 1412 px y brazos que arrancan a y ≈ 1487 px (recorte en `<scratch>/it6b/qa-r1/crop-it7cap04.png`); en CAP-02 de la 6b (precargada) no pasa. VID-03 no existe | Revisar la cabeza y el apagado hacia la cámara con la historia real (sin `Prime`); VID-03 o una toma sin precarga que lo pruebe |
| QA-V6b-03 | mayor | designer | **AC-6B-17 (#12): los charcos no se distinguen** a la escala del remero | CAP-32 (TH, Alto, charcos de 1,0 s): luminancia adentro/borde/afuera 0,548/0,583/0,531 y 0,501/0,537/0,513 → **1,02-1,09:1** (pide ≥ 1,15). El hervidero no se ve; "0 anillos" en el manifiesto; sin la sonda de 40° | Más contraste en el aplanado y el borde de espuma; sondas de 1,15:1 y 40° ± 5° en CAP-32 y en la CAP-04 de `it7` |
| QA-V6b-04 | mayor | designer | **AC-6B-27: el bote no se ve detrás del diálogo** y CAP-29 no es la toma pedida | CAP-29: popa +4,1 % (fila 1190) y +5,2 % (fila 1385) sobre el agua (pide ≥ 10 %). La toma abre el diálogo a mano en un libre (`OpenCompletionForTest`, con 20:00 / 4000 m / 300 inventados mientras el HUD dice 00:03 / 1012 m) y la cámara no está en la toma aérea | Tomar `Corta` hasta el diálogo de los 36,3 s con la toma aérea; ajustar el tinte o la composición del diálogo para que la popa llegue a ≥ 10 %, con la sonda en el manifiesto |
| QA-V6b-05 | mayor | designer | **AC-6B-04 (#3): sin sondas, y la silueta central no contrasta** | CAP-17: la cordillera central contra el cielo y la bruma 1,01-1,07:1; la de la izquierda 1,58:1 (en TV). El halo de las 18 h no se puede medir porque el sol queda detrás de los pinos (04-design-notes §6.1). La ladera (≤ 30 %) no se midió | Las tres sondas (a, b y c) en el manifiesto; si (a) falla, subir el contraste de la cordillera lejana; para (c), una toma con el disco a la vista |
| QA-V6b-06 | mayor | architect | **AC-6B-22 sin prueba**: los 16 casos de `scene_view_test.dart:11` con la avanzada y el cierre mientras la avanzada se prepara | `SceneLifecycleTests` no cambió y `UiHostOptions.SceneWater` vale `Degraded` por defecto; ningún test tiene la categoría AC-6B-22. `EveryEnvironmentShowsTheAdvancedWater` cubre 4 casos en TV y Medio | Parametrizar los 16 casos (4 escenarios × V390/H844 × 12/23 h) con `SceneWater = Auto`, 6 cuadros de 100 ms, más el cierre con `HoldAdvancedWaterForTest` |
| QA-V6b-07 | mayor | designer | **AC-6B-33: la entrega de capturas está incompleta** | Faltan VID-01 a -05 (5 videos: AC-6B-07, -08, -14, -18, -19, -20 b, -34). CAP-27 y CAP-25 son el mismo PNG (mismo SHA-256): no hay `Series 600` ni los dos paneles. CAP-29 (QA-V6b-04). Faltan las sondas de la spec en -04, -11, -12, -13 (columna ± 40 dp), -15 (`fogRow`), -17, -18, -19, -23 y -27; las que existen son de aspecto | Sumar los videos y las sondas a `WaterCatalog`/`WaterProbes`; CAP-27 con `Series 600` y los paneles |
| QA-V6b-08 | menor | designer | AC-6B-15: la costura en el canal no es concluyente (11-13 columnas) y `fogRow` no existe | INDEX: 18-81/255 en 13 columnas (no concluyente); 04-design-notes §6.1 | La pasada sin siluetas y las dos pasadas de `fogRow` (diseño §8) |
| QA-V6b-09 | menor | architect | AC-6B-26: no existe la línea `[PERF] vidrio: GPU p50/p95` del diseño §6 | `HudBlurPass` pone el marcador `RowMate.HudBlur`, pero ningún `ProfilerRecorder` lo lee; ningún registro de `-PerfWater` tiene la línea | Leer el marcador con `GpuRecorder` y escribir la línea (o "sin GpuRecorder") |
| QA-V6b-10 | menor | architect | En el modo de respaldo de la pantalla, la GPU del cuadro informa ≈ 80 ms siempre (también con la degradada) y la adaptación lleva la app a la degradada sola en < 60 s | `auto-noload-lake12`: resolución ×5 → … → `DegradeWater` sin carga | Anotarlo en ARCHITECTURE (es el comportamiento de DV-152) y evaluar si un tiempo de GPU acotado por la presentación debe disparar la adaptación |
| QA-V6b-11 | menor | architect | AC-6B-08: faltan `NoFrameOver33` y `SavedSessionIdentical` del §10 del diseño | `WaterFallbackTests` tiene 5 de los 7 | Escribirlas, con el reloj de prueba (G6b-17) |
| QA-V6b-12 | menor | architect | El build cuenta asignaciones en régimen (máx 85-86 por cuadro, 17-23 cuadros con alguna), **también con la degradada** (108/13). El Editor da 0 B | Ventanas de régimen de `-PerfWater` y de la base degradada | Decir de dónde vienen (¿Profiler o UI del build de desarrollo?) o bajarlas a 0 |
| QA-V6b-13 | menor | designer | AC-6B-23: la sonda pasa, pero el desenfoque casi no se ve dentro de las tarjetas (G6b-09) | CAP-17, tarjeta de Vatios sobre la orilla: desvío 0,0007 (sin la tarjeta 0,050); gradiente 0,0001 contra 0,0112 × (1 − α) ≈ 0,002 | La sonda de alta frecuencia de G6b-09 y, si no llega, menos tinte en Alto y Medio |
| QA-V6b-14 | menor | designer | Costa a las 18 h: el camino del sol es débil y no tiene el tono de la hora | CAP-11: bajo el sol, 1,15× la franja; la columna más brillante a −42 dp del sol (tope ± 40) | Más brillo del astro bajo con la normal perturbada en la costa |
| QA-V6b-15 | menor | designer | La línea de contacto dibuja un óvalo oscuro que pasa la popa (CAP-09, CAP-11) y dos rieles oscuros paralelos al casco (CAP-04 de `it7`), que DD6b-05 daba por resueltos | Recortes en `<scratch>/it6b/qa-r1/` | Cerrar el huso en la popa y no extenderlo detrás |
| QA-V6b-16 | menor | po | AC-6B-09 y -29 dicen "la degradada ≤ 24 ALU"; el tope vigente desde la It. 8 es 28 y mide 28 | `ShaderArt` (HEAD: `WaterArt.MaxAlu`); `EachFragmentFitsItsBudget` | Corregir el texto de la spec |
| QA-V6b-17 | menor | architect | `BleAllocationTests.TheSonarDoesNotAllocatePerFrame` volvió a fallar una vez ("reloj de prueba 1 en 1 cuadros") | PlayMode sin simulador; 4/4 al repetir la clase; 848/848 dos veces con simulador | Sigue en #71 (la causa no se buscó, QA-V15-05) |

## 5. Issues elegidos

| Issue | Estado | Qué queda |
|---|---|---|
| #6 (la iteración) | **En parte** | QA-V6b-01 a -07; 60 fps en Android (AC-6B-32, #7) |
| #1 (tirón antes de la precarga) | **No** | El toque cumple (1,4-3,1 ms), pero el peor cuadro con movimiento llega a 44-52 ms (QA-V6b-01) |
| #2 (11 líneas de brillo) | **Resuelto** | — |
| #3 (montañas y halo) | **En parte** | Las sondas y el contraste de la cordillera central (QA-V6b-05) |
| #12 (efectos poco visibles a 47 dp) | **No** | Contraste de 1,02-1,09:1 (QA-V6b-03) |
| #13 (reflejo del bote y del remero, micro-anillos, estela con *surge*) | **En parte** | El reflejo planar en Alto pasa sus pruebas; los micro-anillos no salen en CAP-32 y la estela real tiene el borde (QA-V6b-02) |
| #22 (`Foam` ≤ 30 ALU) | **Resuelto** | 23 ALU con tope 30 |
| #31 (reflejos, columnas, espuma de crestas) | **En parte** | El espejo sin las instancias (DD6b-06); las columnas casi no se ven; la boya y la espuma de crestas sin video |
| #85 (popa como tabla blanca) | **Resuelto** | 13,1 % de punta a punta; la media 0,897 (informativa) queda para el PO (04-design-notes §8) |

## 6. Capturas (revisión a la vista)

- **Matriz de la 6b**: el agua se lee bien en los 4 escenarios. El lago y la regata están planchados, el río tiene *chop* y la costa, mar de fondo con el camino del sol ancho. De noche aparece el camino de la luna. Las degradadas son las de la 6a sin la 11.ª línea. Medio se ve casi igual que Alto y Bajo queda sin destellos.
- **Defectos**: los de QA-V6b-02, -03, -04, -14 y -15. Además, la estela precargada se lee como dos haces de luz que llegan al borde de abajo (CAP-02, -03, -08), y los remolinos de CAP-32 parecen dos arquitos ("carita").
- **Sets viejos** (muestreo: 6a CAP-08, 7 CAP-04): la 6a sigue con la degradada (DT6b-24). La CAP-04 de la 7 pasó a la avanzada, como pide el diseño, y ahí se ve QA-V6b-02.

## 7. Repo, legacy y assets

- `git -C ../rowmate-legacy status --porcelain`: vacío. No se leyó el módulo de credenciales de Strava.
- `ProjectSettings/`: sin cambios y sin `ROWMATE_SIMULATOR`; la marca del simulador no quedó. `Assets/RowMate/Settings/Scene/SceneShaderVariants.shadervariants` cambia (la colección con las variantes nuevas, §7 del diseño). `HorizonFar.mat`/`HorizonNear.mat`: sin cambios.
- Búsqueda en los 143 archivos cambiados o nuevos y en `06b-agua-avanzada/` (rutas absolutas de usuario, carpeta temporal de la sesión, nombre del usuario, nombres de equipo y de hardware): **limpia**. Los 142 de texto están en UTF-8 sin doble codificación.
- Assets: `water-normal.png` procedural (`water.json`: semilla, tamaño y SHA-256), *shaders* propios, `THIRD_PARTY_NOTICES.md` sin cambios.

## 8. Para issues

- El espejo de Alto sin las instancias (pinos, cabaña, fachadas, tribunas) (DD6b-06).
- La estela precargada se lee como haces de luz hasta el borde de abajo.
- Los remolinos del charco parecen una "carita" (CAP-32).
- La primera ventana de `Corta` con `StartRoutine` directo: p99 21,6-23,7 ms (no es el camino del usuario; la ventana del diálogo da ≤ 10,1 ms).
- La regata en `E-180` con público no se mide en el build (`-rowmate-perf-scene` arranca en 0 m).
- La GPU y el "ningún cuadro > 33 ms" de AC-6B-26 y -28 se tienen que repetir con el monitor en su modo normal.

---

## Ronda 2

| Campo | Valor |
|---|---|
| Re-verifica | QA-V6b-01 a -07 y -11, contra [02-tech-design §17](02-tech-design.md) (architect) y [04-design-notes §10](04-design-notes.md) (designer) |
| Artefactos | `<scratch>/it6b/qa-r2/` (XML de NUnit, registros de Unity y de los players, la segunda corrida de las capturas, la carga de la PC). Nada se versiona |
| Pantalla en las medidas | **Modo normal del monitor** (no el de respaldo) (ventana del player 1280 × 720). Ya no está el modo de respaldo de la ronda 1: **la GPU ahora se mide** |
| Carga de otros procesos | La VM de WSL varió entre 0,3 y 16 de los 24 procesadores. Por corrida: tabla de abajo. No se tocó ningún proceso |

### Veredicto: **RECHAZADO**

0 bloqueantes, **3 mayores**, 4 menores nuevos. **38 AC: 31 PASS, 3 FAIL, 4 NO VERIFICABLE.**

Lo de la ronda 1 que era del designer quedó bien resuelto: la estela real nace en la popa, los charcos se leen (1,26-1,39:1 y 39,4-39,6°), la popa se ve bajo el diálogo (+18 %), las montañas tienen sus tres sondas y la entrega está completa (38 tomas y videos, **38/38 iguales byte a byte** y las mismas sondas en una segunda corrida). AC-6B-22 tiene sus 24 pruebas y AC-6B-08 sus dos que faltaban, todas en verde. **No se puede cerrar** por tres cosas. Con la pantalla en su modo normal, **AC-6B-01 sigue en FAIL**: la medida nueva del architect daba 4,8-5,8 ms en el modo de respaldo, pero con el monitor en su modo normal las unidades de la precarga caen en cuadros con movimiento (14,0-20,2 ms; 6 de 16 pasan de 16,7). **AC-6B-02 falla en 16/16**: el toque cuesta 22,9-36,1 ms, también con la PC casi quieta. Y **AC-6B-20 falla en VID-01**: el agua salta cada 16 m (pico de 4,06 × contra el tope de 1,5 ×).

**Para aprobar:** QA-V6b-R2-01 a -03.

### R2.1 Pruebas

| Corrida | Resultado | Otros procesos (media / máx) |
|---|---|---|
| `run-tests.ps1 -Matrix` | **4177: 4138 pasan, 0 fallan, 39 ignorados** (+194/194). Matriz: PASA 919, FALLA 0, IGNORADO 2, PENDIENTE UNITY 204, EN EQUIPO 20, SIN TEST 0 | — |
| Unity EditMode | **4320: 4265 pasan, 0 fallan, 55 ignorados** | 29 % / 70 % |
| Unity EditMode `-Simulator` | **4514: 4459 pasan, 0 fallan, 55 ignorados** | 22 % / 30 % |
| Unity PlayMode | **796: 795 pasan, 1 falla**: `SceneBudgetTests…(High)`, CPU p95 **1,515 ms** (tope 1,5) | 28 % / 53 % |
| PlayMode `-Simulator`, corrida 1 | **877: 876 pasan, 1 falla**: la misma, **1,58 ms** | 30 % / 55 % (WSL 6-8,5 procesadores) |
| PlayMode `-Simulator`, corrida 2 | **877: 876 pasan, 1 falla**: la misma, **1,80 ms** | 31 % / 69 % |
| `SceneBudgetTests` sola, dos veces | 1.ª: 9/10 (Alto **1,85**, Medio 1,19, Bajo 0,74 ms). 2.ª: **10/10** (Alto **1,35**, Medio 0,87, Bajo 0,64 ms) | 26 % / 49 %; 31 % / 41 % |
| `capture-snapshots.ps1 -Set it6b -Out <scratch>` | 38 tomas, **38/38 PNG y MP4 iguales byte a byte** a los versionados, las mismas sondas y comprobaciones; 16 sondas de aspecto fallan, igual que antes, y VID-01 falla su comprobación (sale con 1) | — |

Las pruebas nuevas, en las tres corridas de PlayMode: `AdvancedLifecycleTests` **24/24**, `WaterFallbackTests` **9/9** (`[WFT]` fundido 4,82 ms, corte 4,81 ms), `WakeRealHistoryTests` 1/1 y `EarlyEntryTests` 5/5. No se rompió ningún test de las It. 1-15. La intermitente de #71 no falló esta vez. La única falla es `SceneBudgetTests(High)` (QA-V6b-R2-04). La guardia de `ProjectSettings` dio "sin cambios" en todas las corridas.

### R2.2 Rendimiento en el build (`Builds/Windows/simulator`, recompilado por QA)

| Medida | Resultado |
|---|---|
| `build.ps1 -Simulator -PerfEarly -EarlySeconds 1` (8 en frío) | Sale con **15**. Toque **22,9-36,1 ms** (8/8 > 16,7). Peor cuadro con movimiento **19,6 / 18,2 / 20,2 / 17,0 / 15,0** (lago), **14,0** (río), **14,7** (costa), **14,5 ms** (regata). Siempre a los ≈ 2,0-2,4 s del toque, en una unidad de la precarga: `mundo` (hasta 18,3 ms), `orilla de 920 m` o `Tick sintético`. Otros procesos: 53, 41, 26, 27, 0, 34, 10 y 10 % |
| Las 8 otra vez (`-rowmate-perf-early 1`) | Toque **23,9-31,7 ms** (8/8 > 16,7). Con movimiento **15,6 / 16,3 / 18,1 / 15,8 / 16,1 / 14,2 / 15,4 / 17,8 ms**. Total de la PC: 3, 22, 15, 22, 27, 29, 43 y 40 %. Con la PC casi quieta (3 %) da 15,6 ms; con el 15 % ya da 18,1 ms |
| `-rowmate-perf-scene 20` (Alto y Medio × 4 escenarios × 12 y 22 h) | Hilo principal: **p99 2,2-3,5 ms**. **GPU del cuadro, máx. 5,6-8,9 ms** (ahora sí se mide). El máximo de CPU (22-37 ms) es siempre el cuadro del toque. La avanzada se ve en las 16 combinaciones (el espejo en Alto), 0 líneas del agua, SetPass ≤ 78, memoria 132-137 MB. Otros procesos: 10-73 % |
| `-rowmate-water-fail 15` (lago, Alto, 40 s) | **1** línea `[Scene] Shader de agua no disponible, usando fallback: falla forzada de prueba`. Ventanas después del cambio: máx. 4,5 / 4,3 ms, peor del hilo principal 7,9 ms, GPU ≤ 5,9 ms. La sesión sigue, el player sale con 0 y al final "Degraded… líneas del agua en el registro 1" |
| Sin `-rowmate-quality`, 60 s | Se queda en **Alto con la avanzada** en las 4 ventanas de régimen (p99 3,5-3,9 ms, GPU ≤ 5,9 ms). Con la pantalla normal la adaptación ya no baja sola a la degradada: QA-V6b-10 era del modo de respaldo |
| GPU del desenfoque (≤ 0,6 ms en Medio) | Sigue sin la línea `[PERF] vidrio: GPU` (QA-V6b-09, que esta ronda no tocó). El cuadro entero de Medio cuesta ≤ 6,5 ms de GPU, pero el desenfoque no se puede separar |
| Builds | `normal` (release): 0, sin rastros del simulador, `RowMateBle.dll`, VERSIONINFO 1.0.0 (iguisoft). `simulator`: compila (sale con 15 por la medida). `dev-smoke` con **`-BleSmoke`: ok** (`[BLE] humo: estado=On busquedas=3 ftms=0 fallas=0 colgado=no soloFtms=si`, `[HRM] humo: busquedas=1 sensores=0 ajenos=0 fallas=0`) |

### R2.3 Cambios en la matriz de AC (los demás siguen como en la ronda 1)

| AC | Evidencia de la ronda 2 | Resultado |
|---|---|---|
| 01 | Con la pantalla normal, 6 de 16 arranques pasan de 16,7 ms con movimiento (14,0-20,2). Con la PC al 3 % da 15,6 ms; con el 15 %, 18,1 ms. La medida nueva (`EarlyEntryFrames`) atribuye bien los cuadros (0 sin atribuir), pero los 4,8-5,8 ms del architect eran del modo de respaldo a 13 cuadros/s | **FAIL** (QA-V6b-R2-01) |
| 02 | Toque 22,9-36,1 ms en 16/16, también al 3 % de carga (23,9 ms). `StartFree` 9,7-13,0 ms; el `Open` de la escena 0,2 ms (el arreglo del toque en frío funciona) | **FAIL** (QA-V6b-R2-02; antes PASS por la medida mal atribuida) |
| 04 | (a) CAP-02: **1,339:1**; CAP-17: **1,316:1** (peor tercio, tope 1,3). (b) **28,3 %** (tope 30). (c) CAP-03: **+76 %**; CAP-33: **+135 %**. El disco no se ve a las 18 h: la sonda mide el cielo encima, como anotó el designer. A la vista, la cordillera central de CAP-17 ya se separa del cielo | PASS (con poco margen) |
| 08 | `NoFrameOver33` (4,8 ms), `SavedSessionIdentical` y el build con la falla | PASS |
| 14 | `WakeRealHistoryTests`, `TheHeadIsTheDrawnSternEveryFrame`. La CAP-04 de la 7, sin precarga: los brazos salen de la popa, sin borde recto ni corte horizontal. VID-03 sin problemas en sus comprobaciones | PASS |
| 17 | CAP-32: charcos adentro **1,307 y 1,264:1**, borde 1,376 y 1,393:1, remolinos a **39,6° y 39,4°**, hervidero **1,173:1**; 4 micro-anillos. En la CAP-04 de la 7, adentro 1,16 y 1,17:1 | PASS (sigue la "carita", de aspecto) |
| 20 | (a) y (c) como en la ronda 1. (b) VID-01: pico **4,06 ×** la mediana justo en el cruce (tope 1,5 ×); el designer midió 5,7 × en el cruce de 4112 m: salta cada 16 m (`GridAnchorZ`, §10.1 de las notas) | **FAIL** (QA-V6b-R2-03) |
| 22 | `AdvancedLifecycleTests`: los 16 casos con `Auto` y Alto, y los 8 de cierre; **24/24** en las tres corridas | PASS |
| 26 | El HUD sigue sin elementos nuevos. La GPU ahora informa (cuadro de Medio ≤ 6,5 ms), pero no existe la medida del desenfoque | NO VERIFICABLE (QA-V6b-09) |
| 27 | CAP-29: `Corta` de verdad hasta el diálogo, toma `Aerial`, datos de la sesión; popa **+18,0 %** (tope 10 %). A la vista, el bote se adivina apenas detrás del vidrio | PASS |
| 28 | Régimen: p99 ≤ 3,9 ms, GPU ≤ 8,9 ms, peor del hilo principal ≤ 7,9 ms en los 4 escenarios, Alto y Medio. Los cuadros de más de 33 ms (3 de 16, entre 33,2 y 37,5) son el toque, con el 59-73 % de carga externa (AC-6B-02). Las 85 asignaciones por cuadro siguen (QA-V6b-12) | PASS (la GPU ya se verificó; la regata en `E-180` sigue sin medirse) |
| 29 | `SceneBudgetTests(High)` pasa del tope en 4 de 5 corridas (1,515 / 1,58 / 1,80 / 1,85 ms) y lo cumple en 1 (1,35 ms), siempre con el 25-31 % de carga externa. El resto de los topes se cumple | NO VERIFICABLE en la CPU de Alto (QA-V6b-R2-04) |
| 33 | 38 tomas con videos y sondas; CAP-27 ≠ CAP-25 (`Series 600` con dos paneles); 38/38 iguales en dos corridas. Pero el `INDEX.md` versionado lista **solo CAP-32**, porque quedó de una corrida parcial | PASS (QA-V6b-R2-05) |

### R2.4 Issues re-verificados

| ID | Estado | Evidencia |
|---|---|---|
| QA-V6b-01 | **No resuelto** → QA-V6b-R2-01 | La medida ahora es correcta, pero con la pantalla normal el peor cuadro con movimiento llega a 14,0-20,2 ms |
| QA-V6b-02 | **Resuelto** | CAP-04 de la 7 sin precarga, `WakeRealHistoryTests`, VID-03 |
| QA-V6b-03 | **Resuelto** | Las sondas de CAP-32 y de la CAP-04 de la 7 |
| QA-V6b-04 | **Resuelto** | CAP-29 +18 %, con la toma y los datos reales |
| QA-V6b-05 | **Resuelto** | Las sondas (a), (b) y (c) pasan; (a) con 0,016-0,039 de margen |
| QA-V6b-06 | **Resuelto** | 24/24 en tres corridas |
| QA-V6b-07 | **Resuelto** (salvo el `INDEX.md`, QA-V6b-R2-05) | Los 5 videos, las sondas de la spec y la segunda corrida idéntica |
| QA-V6b-11 | **Resuelto** | `NoFrameOver33` y `SavedSessionIdentical` en verde |

### R2.5 Issues nuevos

| ID | Sev. | Resp. | Qué | Evidencia | Corrección sugerida |
|---|---|---|---|---|---|
| QA-V6b-R2-01 | mayor | architect | **AC-6B-01 sigue en FAIL con la pantalla normal (#1).** Las unidades de la precarga (`mundo` hasta 18,3 ms, `orilla de 920 m`, `Tick sintético` 16,7) caen en cuadros con movimiento a los ≈ 2,0-2,4 s del toque | 16 arranques con el monitor en su modo normal: 6 pasan de 16,7 ms (máx. 20,2). Con la PC al 3 % da 15,6 ms (1,1 ms de margen) y con el 15 %, 18,1 ms. En el modo de respaldo daba 4,8-5,8 ms | Partir esas tres unidades por debajo de ≈ 10 ms o llevarlas al negro o al reposo. Repetir `-PerfEarly` con la pantalla en su modo normal (DV: el modo de respaldo no sirve para medir) |
| QA-V6b-R2-02 | mayor | architect | **AC-6B-02: el primer toque cuesta 22,9-36,1 ms** (antes PASS con la medida mal atribuida; el architect ya lo había destapado en el §17) | 16/16 > 16,7, con 23,9 ms al 3 % de carga. `StartFree` 9,7-13,0 ms más ≈ 12 ms del panel; la escena ya no suma (`Open` 0,2 ms) | Precalentar en el arranque la página del inmersivo y el camino de `StartFree`, como `HudWarmup` (la propuesta del §17), y medir con la pantalla normal |
| QA-V6b-R2-03 | mayor | architect | **AC-6B-20 b: el agua salta cada 16 m** (la malla en anillos con el ancla `GridAnchorZ` = Mod(D; 16)). A 4 m/s es un salto cada 4 s en toda sesión | VID-01: pico 4,06 × la mediana en el cruce (tope 1,5 ×); el designer midió 5,7 × en el de 4112 m (04-design-notes §10.1). El manifiesto lo marca y la corrida de capturas sale con 1 | Anclar la malla sin que cambie la densidad que ve cada punto (anillos fijos en la cámara con el desplazamiento por fase, o el ancla en múltiplos del paso de cada anillo con continuidad), o llevar la pendiente de B1 al fragmento (≈ 3 ALU: Medio está en 70/70, hay que hacer lugar) |
| QA-V6b-R2-04 | menor | architect | `SceneBudgetTests(High)` en el borde: falla en 3/3 corridas completas y en 1 de 2 sola. Bajo y Medio cambian poco. Antes de esta ronda daba 1,33-1,48 ms | 1,515 / 1,58 / 1,80 / 1,85 ms (fallan) y 1,35 ms (pasa), con el 25-31 % de carga externa documentada. No es una falla demostrada del código, pero la suite no queda en verde | Recuperar margen en Alto (las 48 rampas de la estela por cuadro pueden ir al *shader* o calcularse una vez por punto nuevo) y repetir con la PC sin la VM. No se relaja el tope |
| QA-V6b-R2-05 | menor | designer | **El `INDEX.md` de la 6b lista solo CAP-32 y el de la 7, solo CAP-04** (quedaron de corridas parciales con `-Filter`). El de la 7 perdió 54 filas contra HEAD | `06b-agua-avanzada/snapshots/INDEX.md`: 1 fila; `07-remero-palada/snapshots/INDEX.md`: 1 fila (HEAD: 55). La corrida completa de QA en `<scratch>` genera las 38 filas | Regenerar el `INDEX.md` de la 6b con una corrida completa y restaurar el de la 7 desde HEAD con la fila nueva de CAP-04 antes de commitear |
| QA-V6b-R2-06 | menor | designer | Los sets viejos con el lago (6a, 7, 8, 14 y 15) ya no coinciden con el código: el contraluz (DD6b-17) y el vidrio cambian lo que se dibuja, y no se regeneraron | 04-design-notes §10.2 | Regenerarlos al cerrar la iteración o anotarlo en sus `INDEX.md` |
| QA-V6b-R2-07 | menor | designer | La sonda (a) de AC-6B-04 pasa con muy poco margen (1,316 y 1,339 contra 1,3): un cambio chico del cielo o de la bruma la tira | Manifiesto: CAP-17 y CAP-02 | Dejar ≥ 0,05 de margen o fijarla con una prueba de Core |

Siguen abiertos de la ronda 1, sin cambios: QA-V6b-08, -09, -12, -13, -14, -15 (rieles y el óvalo de CAP-09 y CAP-11, que todavía se ven), -16 y -17. QA-V6b-10 se cierra: era del modo de respaldo; con la pantalla normal la app se queda en Alto.

### R2.6 Issues elegidos

| Issue | Estado | Qué queda |
|---|---|---|
| #6 (la iteración) | **En parte** | QA-V6b-R2-01 a -03; Android (AC-6B-32, #7) |
| #1 (tirón antes de la precarga) | **No** | El toque (22,9-36,1 ms) y el peor cuadro con movimiento (hasta 20,2 ms) con la pantalla normal |
| #2 | **Resuelto** | — |
| #3 (montañas y halo) | **Resuelto** | Margen de (a) (QA-V6b-R2-07) |
| #12 (efectos a 47 dp) | **Resuelto** | La "carita" de los remolinos (aspecto) |
| #13 (reflejos, micro-anillos, estela con *surge*) | **En parte** | El espejo de Alto sin las instancias (DD6b-06) |
| #22 | **Resuelto** | — |
| #31 (reflejos, columnas, espuma de crestas) | **En parte** | El espejo sin las instancias. Las columnas ya se ven (CAP-08: 20 de 20) |
| #85 | **Resuelto** | La media de 0,897, para el PO |

### R2.7 Capturas (a la vista)

- **6b**: CAP-29 tiene el diálogo real de `Corta`. Detrás del vidrio el bote se adivina apenas: la sonda pasa, pero a la vista se lee poco. CAP-27 muestra `Series 600` con los dos paneles. CAP-32 muestra los charcos con el borde de espuma, los remolinos y los anillos. Sigue la "carita". CAP-08 tiene columnas de luz bajo cada farol y cada ventana. CAP-11 todavía tiene el óvalo oscuro alrededor de la popa (QA-V6b-15) y los brazos de la estela arrancan separados de la popa, por la rampa de 1,2 m. CAP-33: el disco queda detrás de la loma.
- **Sets viejos** (muestreo): CAP-04 de la 7 (regenerada) tiene la estela real sin el borde recto y los charcos en el cuadro. CAP-08 de la 6a sigue con la degradada y 10 líneas.

### R2.8 Repo, legacy y assets

- `git -C ../rowmate-legacy status --porcelain`: vacío. No se leyó el módulo de credenciales de Strava.
- `ProjectSettings/`: sin cambios y sin `ROWMATE_SIMULATOR` después de todas las corridas. No quedó la marca del simulador. `HorizonFar.mat`/`HorizonNear.mat` y `THIRD_PARTY_NOTICES.md`: sin cambios.
- La búsqueda en los 318 archivos cambiados o nuevos dio **limpia**: rutas absolutas de usuario, la carpeta temporal de la sesión, nombre del usuario, nombres de equipo y hardware. Las coincidencias son las de siempre: `iguisoft` en ARCHITECTURE y `$env:USERPROFILE` en `run-unity-tests.ps1`. Los 229 de texto están en UTF-8 válido, sin doble codificación.
- Assets: sin cambios de licencia; todo procedural y con *shaders* propios.

### R2.9 Para issues

- La regata en `E-180` con público sigue sin medirse en el build.
- `[PERF] cpu del hilo principal` de `-rowmate-perf-scene` es una sola ventana de 20 s con el toque adentro. Conviene separar la entrada del régimen, como hacía la ronda 1, para leer "ningún cuadro > 33 ms" en régimen.
- El bote detrás del diálogo pasa la sonda (+18 %), pero a la vista apenas se ve.

---

## Ronda del salto

| Campo | Valor |
|---|---|
| Re-verifica | AC-6B-20 b (QA-V6b-R2-03, #91) contra [02-tech-design §18](02-tech-design.md) (DT6b-27: la malla en anillos queda quieta en cinta y lo que el paso no alcanza se apaga hacia su media). También AC-6B-20 a y c, la degradada (AC-6B-03, -09) y la caída (AC-6B-07, -08) |
| Artefactos | `<scratch>/it6b/qa-r4/` (XML de NUnit, registros de Unity y del player, la sonda de Core, las series por captura y la corrida de capturas). Nada se versiona |
| Pantalla en las medidas | Modo normal del monitor, el mismo de la ronda 2 (no el de respaldo). Ventana del player 1280 × 720 |
| Carga de otros procesos | 4-14 % en cada corrida (tabla de abajo); la VM de WSL no corría (0 %). No se tocó ningún proceso |

### Veredicto: **APROBADO CON OBSERVACIONES**

0 bloqueantes, 0 mayores, 4 menores nuevos. **AC re-verificados: 6 PASS, 0 FAIL** (AC-6B-03, -07, -08, -09, -20 y -29). Con esto la matriz de la 6b queda en **38 AC: 33 PASS, 2 FAIL, 3 NO VERIFICABLE**: siguen en FAIL AC-6B-01 y -02 (la entrada, #1; esta ronda no los tocó) y en NO VERIFICABLE AC-6B-15, -26 y -32.

El salto del agua cada 16 m ya no está. VID-01 pasa (1,41 × contra 1,5 ×, y el pico no cae en el cruce). En el build de desarrollo, 3 recorridos de ≈ 430-470 m a ≈ 4,7 m/s (Lago Alto, Costa Alto y Costa Medio) no tienen ningún pico local mayor que 1,5 × en ninguna de las 3 zonas de agua. En el lago no aparece ninguna periodicidad. Una sonda de Core de 400 m con el ancla vieja encuentra el salto cada 16,0 m en todos los escenarios, y con la malla quieta no lo encuentra. El aspecto del agua no empeoró: el cambio contra la ronda 2 es de 0,09-0,53/255 de media, solo en el agua lejana, y la degradada quedó igual byte a byte. Todas las suites están en verde, también `SceneBudgetTests(High)`.

### RS.1 Pruebas

| Corrida | Resultado | Otros procesos (antes / media / máx) |
|---|---|---|
| `run-tests.ps1 -Matrix` | **4182: 4143 pasan, 0 fallan, 39 ignorados** (+5 contra la ronda 2: `WaterMeshShiftTests`). Matriz: PASA 919, FALLA 0, IGNORADO 2, PENDIENTE UNITY 204, EN EQUIPO 20, SIN TEST 0. `[WMS]`: de 1,01 a 1,27 × contra los ±0,25 s en Alto y Bajo × 4 escenarios × 5 campos | 4 % antes |
| Unity EditMode | **4325: 4270 pasan, 0 fallan, 55 ignorados** | 0 / 11,9 / 39 % |
| Unity EditMode `-Simulator` | **4519: 4464 pasan, 0 fallan, 55 ignorados** | 3 / 8,3 / 17 % |
| Unity PlayMode | **796: 796 pasan**. `SceneBudgetTests` 10/10: CPU de la escena p95 en Alto **1,06 ms** (1,05 / 1,06 / 1,12; tope 1,5) | 7 / 4,7 / 18 % |
| Unity PlayMode `-Simulator` | **877: 877 pasan**. Alto **1,06 ms**, Medio 0,72 ms, Bajo 0,54 ms. `[WFT]` fundido 4,28 ms y corte 4,29 ms | 13 / 4,6 / 21 % |
| `capture-snapshots.ps1 -Set it6b -Out <scratch>` | 38 tomas. **38/38 PNG y MP4 iguales byte a byte** a los versionados. `INDEX.md` igual (38 filas) y `manifest.json` igual salvo `durationMs`. Sale con 1 por las mismas 16 sondas de aspecto de siempre: no cambia el estado de ninguna de las 85 sondas contra la ronda 2. VID-01 ahora pasa | — |

Contra la ronda 2: no hay ninguna prueba nueva en rojo. `AdvancedLifecycleTests` 24/24, `WaterFallbackTests` 9/9, `AdvancedWaterTests` 10/10, `EarlyEntryTests` 5/5 y `WakeRealHistoryTests` 1/1 pasan en las dos PlayMode. Las intermitentes de `BleAllocationTests` (#71) no fallaron esta vez (4/4). `SceneBudgetTests(High)` pasa en las dos corridas con 1,06 ms y el 5 % de carga externa. En la ronda 2 daba 1,515-1,85 ms con el 25-31 %. Eso confirma que la falla venía de la carga (QA-V6b-R2-04). El tope no se tocó. La guardia de `ProjectSettings` dio "sin cambios" en todas las corridas.

### RS.2 AC-6B-20 b: el salto

**VID-01** (la medida de la ronda 2): `diferencia entre cuadros del agua: mediana 0.0062, pico 0.0087 a los 2.6667 s (1.4066 ×); cruce de 4096 m a los 0.7667 s`. **Pasa**. El pico no cae en el cruce (0,77 s) ni en el próximo múltiplo de 16 m. A los 2,67 s el bote está en ≈ 4103 m.

**Sonda por cuadro (Core, dotnet)**. Usa la malla de verdad (`WaterRingMesh` en Alto y Bajo), los campos de `WaterVertexField` interpolados por triángulo en z de 16 a 60 m y |x| ≤ 6 m, a 60 fps y 4 m/s durante **400 m**, desde 0 y desde 3900 m (cruza 4096). Se mide por cuadro la diferencia media contra la mediana de los ±0,25 s. Hay tres modos: `nuevo` (`MeshOffsetZ`), `control16` (el ancla vieja −Mod(D; 16), para probar que la sonda ve el salto) e `ideal` (sin malla).

| Modo | Resultado (4 escenarios × Alto y Bajo × color, pendiente de B1, cresta, espuma y calma) |
|---|---|
| `control16` | Salto en todos los casos: **25 eventos cada 16,0 m**, de 50 a 4000 × la mediana |
| `nuevo` | **Ningún evento > 1,5 ×** en color, B1, cresta ni calma en ningún escenario ni nivel. Cruce de 4096 m sin pico. Única excepción: la **espuma de las crestas de la costa en Alto desde 0 m**, con 1,72 × cada 15,7 m (QA-V6b-R3-01). Los picos del último cuarto de segundo de cada corrida son del borde de la ventana: también aparecen en `ideal` |

**Recorridos en el build** (`Builds/Windows/simulator`, de desarrollo, compilado después del último cambio de fuentes). Se usó `-rowmate-perf-scene 100 -rowmate-perf-env <e> -rowmate-perf-hour 12 -rowmate-quality <n>` (libre con `Fuerte`: 280 W, split 1:46-1:50, ≈ 4,6-4,7 m/s), con `ROWMATE_DATA_DIR` temporal y `ROWMATE_BLE=off`. Una sonda de solo lectura lee la ventana con `PrintWindow`, sin tomar el foco, cada ≈ 48 ms durante 92 s. Usa 3 zonas de agua sin HUD, bote, remos ni charcos: adelante a la izquierda, adelante a la derecha y cerca a la izquierda. Calcula la diferencia media de luminancia en bloques de 8 × 8 px, normalizada al intervalo, y descarta las capturas con el intervalo fuera de ±30 %. En el HUD se leen 317 m a los 64 s, así que cada recorrido cubre ≈ 430-470 m.

| Recorrido | Pico local (±0,25 s) por zona | Eventos > 1,5 × | Periodicidad (autocorrelación de 1 a 6 s) | Otros procesos |
|---|---|---|---|---|
| Lago, Alto | 1,39 / 1,22 / 1,43 × | **0** | Ninguna (r ≤ 0,11, la palada a ≈ 2,2 s) | 4 % |
| Costa, Alto | 1,30 / 1,16 / 1,36 × | **0** | Palada a 1,8-2,0 s; **3,5 s r = 0,40** en la zona cercana | 4 % |
| Costa, Medio (malla de Bajo) | 1,32 / 1,21 / 1,43 × | **0** | Igual que en Alto (3,5 s r = 0,41) | 4 % |

Cómo se leen los 3,5 s de la costa: es el período del mar de fondo en un lugar fijo de la pantalla, 2π / (v/4 + 0,6) = 3,5 s con v = 4,7 m/s y la escala de la costa de 4 m (`CoastSwell.Phases`). Es el oleaje que pasa, un movimiento continuo sin picos locales, igual que en el legacy. Con el ancla vieja serían 3,4 s, pero ese salto tendría que verse también en el lago, y ahí no hay ninguna periodicidad. Además `MeshOffsetZ` es 0 (lo prueba `TheVertexMaxStepCoversItsTriangles`). Sensibilidad de la sonda: a ≈ 20 capturas/s la mediana sube ≈ 3 × contra los 60 fps de VID-01, y el salto viejo (+3 × la mediana de VID-01) daría ≈ 2 ×. Se vería por encima de 1,5 ×. No hay control positivo en el build: el código viejo ya no está compilado.

**AC-6B-20 a**: `NoJumpAt4096` pasa en dotnet y Unity, y la sonda cruza 4096 m sin pico. **AC-6B-20 c**: CAP-30 (42 km, `water: Advanced`) está igual byte a byte a la versionada y pasa. **AC-6B-20: PASS.**

### RS.3 La degradada y la caída

- **Degradada**: CAP-21 a -24 son **iguales a las de la ronda 2** (diferencia 0,00) y sus sondas no cambian. CAP-23 sigue contando 9 líneas por el borde, como antes (de aspecto). AC-6B-03 y -09: PASS.
- **Caída en el build** (`-rowmate-water-fail 15`, lago, Alto, 40 s): una sola línea `[Scene] Shader de agua no disponible, usando fallback: falla forzada de prueba`. Al final da `Degraded/High (se ve Degraded; falla forzada de prueba)… líneas del agua en el registro 1`. La sesión sigue (155 m a los 30 s, después de la falla) y el player sale con 0. En el Editor, VID-02 pasa y `WaterFallbackTests` da 9/9 en las dos PlayMode. AC-6B-07 y -08: PASS. En la degradada, la sonda de pantalla ve un evento cada ≈ 1,1 s: son las 10 líneas del legacy que se desplazan sobre un fondo casi quieto, el mismo comportamiento que en la 6a. Queda fuera de AC-6B-20 b, que es del agua avanzada.

### RS.4 Capturas (a la vista)

- El set it6b regenerado contra el de la ronda 2 (`<scratch>/it6b/qa-r2/cap6b`) tiene una diferencia media de **0,09-0,53/255** y entre 0,3 y 1,6 % de píxeles con más de 8/255. **En la mitad de abajo, el agua cercana, la diferencia es 0,00** en las tomas TV. Lo que cambia está en el agua lejana, donde la ola ahora se apaga hacia su media (el desvío anotado en DT6b-27).
- Vistas CAP-10 y CAP-19 (costa) y CAP-31 (lago, popa): el mar de fondo, el brillo del sol, la estela y el reflejo del bote se ven igual que en la ronda 2. No se ve ninguna banda nueva, el horizonte no quedó liso de golpe y no aparecen costuras.
- Sondas que cambiaron de valor sin cambiar de estado: AC-6B-12 en el lago **36,5 → 20,4 %** y en el río 10,6 → 26,6 % (≥ 10 %). AC-6B-13: el camino del sol queda a ± 40 dp en todos los casos. La espuma de las crestas 15,3 → 15,7 % (informativa). AC-6B-04 sin cambios (1,339 y 1,316:1).
- `INDEX.md`: la 6b tiene **38 filas**. La 7 tiene 55, como HEAD, con solo la fila de CAP-04 cambiada (QA-V6b-R2-05 resuelto). Los demás sets (02 a 15) tienen las mismas filas que HEAD.

### RS.5 Repo, legacy y `ProjectSettings`

- `git -C ../rowmate-legacy status --porcelain`: vacío al empezar y al terminar. No se leyó el módulo de credenciales de Strava.
- `ProjectSettings/`: sin diferencias y sin `ROWMATE_SIMULATOR` después de todas las corridas. La guardia dio "sin cambios" en las 5. `HorizonFar.mat`, `HorizonNear.mat` y `THIRD_PARTY_NOTICES.md` sin cambios. El `git status` del árbol es idéntico antes y después, y los 40 archivos de `06b-agua-avanzada/snapshots/` tienen el mismo MD5. Las carpetas temporales de datos se borraron.
- Búsqueda en los 323 archivos cambiados o nuevos (233 de texto): no hay rutas de usuario, carpeta del scratchpad, nombre de usuario, nombre del equipo ni modelo de CPU o GPU. Las coincidencias son las de siempre (las regex de `ItFourA/BToolingTests`, `$env:USERPROFILE` en `run-unity-tests.ps1` e `iguisoft`). Única observación: el modo del monitor (QA-V6b-R3-04).

### RS.6 Issues re-verificados

| ID | Estado | Evidencia |
|---|---|---|
| QA-V6b-R2-03 (#91) | **Resuelto** | VID-01 1,41 ×; la sonda de 400 m no da eventos (el control sí, cada 16 m); 3 recorridos de ≈ 450 m en el build sin picos locales > 1,5 × |
| QA-V6b-R2-04 | **Resuelto** (era la carga) | `SceneBudgetTests(High)` 1,06 ms en las dos PlayMode, con el 5 % de carga externa |
| QA-V6b-R2-05 | **Resuelto** | `INDEX.md` de la 6b con 38 filas e igual al de la corrida de QA; el de la 7 con 55 |
| QA-V6b-R2-01, -02 | Sin cambios (fuera de esta ronda) | AC-6B-01 y -02 siguen en FAIL (#1) |

### RS.7 Issues nuevos

| ID | Sev. | Resp. | Qué | Evidencia | Corrección sugerida |
|---|---|---|---|---|---|
| QA-V6b-R3-01 | menor | architect | La **espuma de las crestas de la costa en Alto** tiene un pico local de 1,72 × cada 15,7 m en la sonda de Core desde 0 m. `WaterMeshShiftTests` no lo ve porque mide solo 6 s desde 4090 m | `<scratch>/it6b/qa-r4/probe.txt`: `High Coast nuevo D0=0 espuma local 1.72x, 25 eventos cada 15.7 m`. Con `ideal` (sin malla) no aparece: es la interpolación de un umbral angosto. En pantalla no se ve como salto (costa, pico local ≤ 1,36 × en Alto y ≤ 1,43 × en Medio) | Llevar `WaterMeshShiftTests` a un recorrido de ≥ 400 m desde 0 (o sumarle la costa desde 0 m), y apagar la espuma una octava antes en los anillos de 4-8 m o pasarla al fragmento en Alto (sobran 27 ALU) |
| QA-V6b-R3-02 | menor | po | La prueba de Core mide el pico contra la mediana de los **±0,25 s**, y AC-6B-20 b dice "la mediana del video". En la costa, la espuma da 1,88 × contra la mediana global y 1,27 × contra la local (6 s) | Salida de `[WMS]` en dotnet | Escribir en la spec que la mediana es la local de ±0,25 s para los campos umbralizados (VID-01 sigue con la global y pasa) |
| QA-V6b-R3-03 | menor | designer | VID-01 pasa con poco margen: 1,41 × contra 1,5 ×, con el pico a los 2,67 s, fuera del cruce | Registro de la corrida de capturas | Revisar qué produce el pico (la palada en el borde de la zona) o alargar VID-01 para que la mediana sea más estable |
| QA-V6b-R3-04 | menor | po | En los docs versionados figura el modo exacto del monitor, un dato del hardware de la PC (repo público) | `01-po-spec.md:208`, `04-design-notes.md:160`, `05-qa-verification.md:173`, `REPORT.md:42`, `STATUS.md:321` | Cambiarlo por "modo normal del monitor (no el de respaldo)" |

### RS.8 Para issues

- Repetir los recorridos en un Android de gama media (AC-6B-32, #7): [Device-Android] NO VERIFICABLE.
- La degradada salta a la vista cada ≈ 1,1 s por las 10 líneas del legacy que se desplazan. Es el contrato de la 6a, pero un fundido en la entrada y la salida de cada línea lo suavizaría.
