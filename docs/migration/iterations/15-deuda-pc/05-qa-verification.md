# It. 15 — Verificación de QA

Ronda 1 abajo (§1 a §7, sin cambios); la **Ronda 2** está al final.

## Ronda 1

| Campo | Valor |
|---|---|
| Entrada | [01-po-spec](01-po-spec.md) (40 AC), [02-tech-design](02-tech-design.md) (WP-A, WP-B, WP-D), [04-design-notes](04-design-notes.md), el árbol sin commitear (sobre `8f4073c`) |
| Corridas | Todas de QA, en `<scratch>/it15/qa-r1`, con `ROWMATE_DATA_DIR` temporal; una sola instancia de Unity |
| Veredicto | **RECHAZADO**: 0 bloqueantes, **5 mayores** (QA-V15-01 a -05), 8 menores. Todo lo que ya estaba (It. 1-14) sigue en verde y los builds y humos pasan; lo que falla son AC de la 15 sin prueba, capturas viejas y dos causas sin medir. La corrección es acotada (§5) |
| Conteo | **PASS 34 · FAIL 6 · NO VERIFICABLE 0** (de 40). Partes NO VERIFICABLES (demo del usuario) dentro de AC: el Explorador real (D15-13) y los 60 fps con el Narrador real (D15-29) |

## 1. Pruebas (salida real)

| Corrida | Resultado | Línea de base (It. 14) |
|---|---|---|
| `run-tests.ps1 -Matrix` (dotnet) | **4 084: 4 045 pasan, 39 ignorados (todos de equipo), 0 fallan**. Matriz: PASA 890, FALLA 0, IGNORADO 2 (Android/Apple), PENDIENTE UNITY 197, **EN EQUIPO 18**, SIN TEST 0 | 4 023 / 3 984 / 39 |
| Unity EditMode | **4 216: 4 162 pasan, 54 ignorados, 0 fallan** | 4 152 / 4 098 / 54 |
| Unity EditMode `-Simulator` | **4 410: 4 356 pasan, 54 ignorados, 0 fallan** | 4 346 / 4 292 / 54 |
| Unity PlayMode | **724/724** (685 s) | 690/690 |
| Unity PlayMode `-Simulator`, corrida 1 | **805/805** (750 s) | 767/767 |
| Unity PlayMode `-Simulator`, corrida 2 | **805/805** (750 s) | — |
| `BleAllocationTests` (clase, PlayMode) × 20 | **20/20** (4/4 cada vez: `TheReconnectBannerDoesNotAllocatePerFrame`, `TheSonarDoesNotAllocatePerFrame`, `HrmConnectedZeroBytesPerFrame`, `TheHudLinkNoticeDoesNotAllocatePerFrame`) | — |

- Ningún ignorado nuevo: en los 54 de EditMode no hay ninguno con "pendiente WP-…" (las guardas de WP-A/B/D se dispararon a afirmaciones duras). PlayMode, 0 ignorados.
- Ningún test de las It. 1-14 se rompió ni bajó la cuenta. Los que cambiaron lo hicieron por su AC (golden `T-1`, `NegativeDistanceKept`, textos de §3.5, `ExportGlyphTests` borrado por AC-D15-11).
- `TheReconnectBannerDoesNotAllocatePerFrame` (arreglo del #78): **pasó en las 2 corridas completas con simulador y en la de PlayMode normal**.
- `[CAT] AC-D15-04 FAIL … entrada 7/48, cambio 10/24, cursor 2/60` en las 3 corridas de PlayMode (igual que WP-B-04).

## 2. Capturas

**Set `it15`** (11 PNG, *manifest* 11/11 `passed`, auditoría sin desbordes, escribe solo en `15-deuda-pc/snapshots`). Miradas una por una:

| Toma | Lo que se ve | Contra spec/arte |
|---|---|---|
| CAP-01 | Botón de exportar (glifo `download`) a la izquierda del chevron en 5 tarjetas; la `F-9` n.º 4 (00:00, `--:--`) sin botón; `Vatios`; split `3:00`, `2:10` | OK (AC-D15-09, -22, -23) |
| CAP-02 | `Saved to Documents\RowMate\TCX` + `Open folder` sobre la barra de pestañas | OK (AC-D15-10) |
| CAP-03 | Fila 2 con borde de 1 dp del color de trabajo; franja del paso 2 más clara; resumen `3:00`. La caída a 0 en la fila 6 es el dato sembrado (E-02, 425 y 430 s), no un sobrepaso | OK (AC-D15-02) |
| CAP-04 | `Split 500m`: escalones sin picos, cursor `3:00` con sombra (sutil sobre fondo oscuro) | OK; el resumen `m:ss` no entra en el cuadro (menor m-01) |
| CAP-05 | Barra del Detalle con el glifo de la fuente, del tamaño de la flecha | OK (AC-D15-11) |
| CAP-06 | `No se pudo abrir la carpeta`, sin acción, al pie | OK (AC-D15-13) |
| CAP-07 / CAP-08 | `Vatios`/`Watts`, `Split 500m`, `kcal`; L640 a 1,3× sin recortes | OK (AC-D15-22, -24) |
| CAP-09 | Cinta `SIMULADOR` en la esquina; `Rutinas` empieza en 44 dp, sin tocarse | OK (AC-D15-35) |
| CAP-10 / AUX-15-01 | El "rectángulo" claro detrás del bote está en CAP-10 y **desaparece** en AUX-15-01: es la popa del casco | OK (AC-D15-34) |

**Muestreo de sets anteriores**: los sets `it9` (24 tomas) e `it14` (16) regenerados en esta iteración **son viejos**: se tomaron a las 17:32-17:33, antes del recorte nuevo de íconos (17:58) y del bloque USS de WP-D (18:37). Donde va el glifo de exportar se ve un **cuadrado blanco** (respaldo dibujado de `IconView`): `09-historial/snapshots/CAP-01`, `CAP-03`, `CAP-04`, `CAP-15` y `14-pulido-plataformas/snapshots/CAP-23` (mirados), y por la hora todas las del Historial/Detalle de los dos sets (QA-V15-03). Fuera de eso no empeoraron: CAP-23 de la 14 muestra el `0`, el eje X y los extremos redondos; la sonda de aspecto de CAP-01 de la 9 da 2/2.

## 3. Matriz AC → evidencia → resultado

| AC | Evidencia | Resultado |
|---|---|---|
| D15-01 | `ChartSmoothingTests` ×3 (dotnet y Unity); CAP-04 | PASS |
| D15-02 | `ChartGeometryTests.SelectedStepBandsReach22`, `StepSelectionTests` (6 PlayMode); CAP-03 | PASS |
| D15-03 | `build.ps1 -PerfHistory` de QA: `recarga 15.6 ms · base 80.2 ms · gc 7 · detalle 16.4 ms · rueda p95 5.9 ms · >33 ms 0.0 %`; `PerfHistoryIsDevelopmentOnly`; el inspector del build normal sin rastros de diagnóstico | PASS (sin plan B) |
| D15-04 | `[CAT]` en 3 corridas: entrada **7/48**, cambio **10/24**, cursor **2/60** (objetivo ≤ 1, ≤ 1, 0) | **FAIL** (QA-V15-01) |
| D15-05 | `HistoryCardNamesTests` ×2, `SnapshotManifest` con aspecto; `it9` CAP-01 2/2 | PASS |
| D15-06 | `TooltipShadowTests` ×2; CAP-04 | PASS |
| D15-07 | `HistoryStoreTests`, `OnePointDoesNotFail`, `CapacityMessagesUseAppFormat`, `AddedWhileTabHiddenIsVisibleWithin400Ms` | PASS |
| D15-08 | Lectura: `09-historial/` 01 (×3), 02 (×4), 00-arte (×1) con "Corregido en la It. 15"; 72 400 puntos, "a la altura de su ritmo" | PASS (está en `DeviceOnlyIds` por error: m-04) |
| D15-09 | `HistoryCardExportTests` (5); CAP-01 | PASS |
| D15-10 | `TapExportsWithoutOpeningTheDetail`, `SameBytesAsTheDetail`, …; CAP-02 | PASS |
| D15-11 | `IconsTests.ExportGlyphsInSubset`, `ExistingGlyphsUnchanged`, `FontAndIconAssetsTests`; CAP-05 | PASS |
| D15-12 | `EveryGlyphHasAUseAndEveryUseIsInSubset`, `FortyThreeOutlineAndSixFilled` | PASS |
| D15-13 | `RevealFailureNotifies`, `WindowsFileExporterTests`, `OpenFolderFailedToast`; CAP-06 | PASS (Explorador real: NO VERIFICABLE, demo) |
| D15-14 | `NegativeLapDistanceIsZero`, `NegativeDistanceKept` (vuelta 0, puntos 20/0/5) | PASS |
| D15-15 | `CreatorClosesActivity`, `VersionPartsFromAppVersion` ×9, golden `T-1` (orden `Notes` → `Creator`, `Device_t`, 1/0/0/0) | PASS |
| D15-16 | `ItTwelveToolingTests` sin `Ignore` (4), `ExportHasNoNetworkAccess`, `TcxExportFrameTests` 3 h, `TcxTimeTests` en Unity | PASS |
| D15-17 | `ThreeHoursUnder200Ms` pasa; el sonar pasó en 3 corridas completas y en el bucle de 20, **pero la causa no se buscó ni se escribió** (02-tech-design no tiene la línea; WP-B no lo menciona) | **FAIL** (QA-V15-05) |
| D15-18 | Documentos de la 12 y `SessionDetailPage.cs` bien; **el set `it9` no muestra el botón de exportar** (cuadrado) | **FAIL** (QA-V15-03) |
| D15-19 | `WindowPlacementTests` ×7 (dotnet y Unity); `-WindowSmoke` de QA: puesta (140, 90, 1000 × 640) → al reabrir (140, 90, 1000 × 640) | PASS |
| D15-20 | `IcoTests.SixSizesReproducible`, `VersionedIcoIsCurrent` | PASS |
| D15-21 | `AltEnterTests` (PlayMode y simulador) | PASS |
| D15-22 | `MetricNamesTests` (es, en, sin claves fuera de la tabla), `CatalogParityTests`; CAP-07/08 | PASS |
| D15-23 | `SplitIsMinutesColonSeconds` ×6; CAP-01/03 | PASS |
| D15-24 | `LabelTrackingTests` ×12, `HudDebtTests` ×2; CAP-07 | PASS |
| D15-25 | `AxisCrowdingTests.UsesNumericScAdvance`, `ChartAxisFontTests` ×2 | PASS |
| D15-26 | `UnknownTextIsOneViolation`, `ExemptPanels`, `SimulatorPanelIsExempt`; auditoría en los 3 sets | PASS |
| D15-27 | `FindsTextArgumentOfAppControls`, `HeaderCountMatchesCatalog` (258) | PASS |
| D15-28 | `FramesMatchTheScreenAndHitTest`, `ListsReportScrolling`, `PanelsReportExpanded` | PASS |
| D15-29 | `DialogReturnsFocusToOpener` pasa; **no hay prueba** de "lector apagado: 0 B en 120 cuadros" ni de "con el lector encendido las métricas no reconstruyen el árbol" (`ScreenReaderCostTests` del §6 no existe) | **FAIL** (QA-V15-04) |
| D15-30 | **Ninguna prueba**: no existen `ItFourteenGapTests` (`MinWindowLayout`, `WindowMode`, `HapticEvents`, `DeviceLandscapeScroll`, `SceneLoadingBeforeBlackEnds`, `SceneLoadingDelayed`) ni `HudBudgetTests.DelayedScene` | **FAIL** (QA-V15-02) |
| D15-31 | Título del INDEX y origen `tools/icon` de CAP-01/02: bien. **Faltan 13 tomas** (CAP-06, -08 a -11, -13, -14, -21, -24 a -26, VID-01/02: no están en `PolishCatalog`) y CAP-22/23 salieron con el cuadrado | **FAIL** (QA-V15-03) |
| D15-32 | `ThreeGroupsWithDelaysAndShift`, `ReducedMotionDoesNotAnimate`, `TheHudReflowCascadeInThreeGroups` | PASS |
| D15-33 | `ScrollAnchorTests.HistoryList` (con la recarga sin cambios) y `DetailPage` | PASS (Dispositivo sin prueba: m-03) |
| D15-34 | CAP-10 vs AUX-15-01; `ItFifteenHarnessTests`; tests de AC-SCN-13-04 en verde | PASS |
| D15-35 | `SimRibbonTitleTests` (W1280, TV, L640), `BackArrowUnderTheRibbonWorks`; CAP-09 | PASS |
| D15-36 | `SceneBudgetTests(High)` y `HudBudgetTests.SeriesWorstCaseWork` pasaron en las 3 corridas completas de QA (y en las 3 de WP-B) | PASS |
| D15-37 | `-rowmate-perf-scene 120` × 4 en `Builds/Windows/simulator` (165 Hz, sin modo de reserva): 11 ventanas cada uno, p99 de CPU ≤ 3,0 / 1,7 / 1,8 / 1,7 ms, GPU máx ≤ 7,2 ms; `perf-log-check` 0 en los 4 | PASS |
| D15-38 | `MatrixReadsDeviceOnlyIds`; la matriz informa "EN EQUIPO 18" | PASS |
| D15-39 | 20/20 de la clase + 2 corridas completas con `-Simulator` + 1 de PlayMode: **23/23**. La falla única de WP-B ("1 vez en el paso") no se reprodujo; la causa del reintento está en DT15-18 | PASS |
| D15-40 | Conteos de §1, 258 claves por idioma, `ProjectSettings` y `.mat` sin cambios, repo limpio (§6) | PASS |

## 4. Issues elegidos

| Issue | Estado | Qué queda |
|---|---|---|
| #32 | Resuelto | — |
| #33 | En parte (lo previsto) | Lo de la spec §4 |
| #34 | **En parte** | AC-D15-03 pasa; **AC-D15-04 FAIL** (7/48, 10/24, 2/60) |
| #35, #37, #38, #39 | Resueltos | — |
| #67 | En parte (lo previsto) | Sello y exportar varias |
| #66 | Resuelto | — |
| #70 | En parte (lo previsto) | Explorador real (demo) |
| #71 | **En parte** | La causa de la asignación del sonar (AC-D15-17) |
| #72 | **En parte** | El set `it9` con el glifo bueno |
| #73 | En parte (lo previsto) | FIT |
| #74 | En parte (lo previsto) | Marca, *launcher*, pantalla completa recordada |
| #75, #76, #79 | Resueltos | — |
| #77 | Fuera | Vidrio aceptado por el usuario |
| #78 | Resuelto | — (AC-D15-36 a -39 pasan; la falla única de WP-B no se reprodujo en 23 corridas) |
| #80 | **En parte** | Costo del lector (AC-D15-29) y 60 fps con el Narrador (demo) |
| #81 | **En parte** | Las 6 pruebas de la 14 (AC-D15-30) y las 13 tomas de `it14` (AC-D15-31); sets de la 2, 3, 10a, 11 y 12 (previsto) |
| #82 | **En parte** | `SceneLoading` (AC-D15-30); popa del casco y estela (previsto) |

## 5. Hallazgos

| ID | Sev. | Resp. | Problema | Evidencia | Corrección |
|---|---|---|---|---|---|
| QA-V15-01 | mayor | architect | AC-D15-04 FAIL: el gráfico sigue asignando en la entrada, el cambio de métrica y el cursor | `[CAT] … entrada 7/48, cambio 10/24, cursor 2/60` (3 corridas); WP-B-04: `chart-mesh` 103 asignaciones en 42 dibujos, panel 260-287 en el cambio | Medir con *callstacks* del Profiler (no solo contar) qué asigna el dibujo de `chart-mesh` y el panel en el cambio; corregir. Si no se llega, el PO decide si AC-D15-04 vuelve a #34 con el dato (como el plan B de D15-03) |
| QA-V15-02 | mayor | architect | AC-D15-30 sin ninguna prueba | `grep` de `ItFourteenGap`, `MinWindowLayout`, `HapticEvents`, `SceneLoading*`, `DelayedScene` en `Tests/`: 0 resultados; la matriz del §6 los promete | Escribir los 6 casos de `ItFourteenGapTests` (con `SceneLoading` (a) y (b) del gate QA-G15-03) y `HudBudgetTests.DelayedScene` |
| QA-V15-03 | mayor | architect | Capturas viejas y faltantes: `it9` e `it14` se tomaron antes del recorte de íconos y de la USS de WP-D (cuadrado en lugar del glifo); a `it14` le faltan 13 tomas | Horas de archivo (17:32 vs fuente 17:58, USS 18:37); `09-historial/snapshots/CAP-04-lista-w1280.png`, `14-pulido-plataformas/snapshots/CAP-23-detalle-w1280.png`; `PolishCatalog.cs` solo tiene 16 IDs | Sumar las 13 tomas a `PolishCatalog` (la lista de 02-tech-design §4) y volver a tomar `it9` e `it14` enteros con el árbol actual (y `it15` por las dudas, para confirmar bytes) |
| QA-V15-04 | mayor | architect | AC-D15-29: falta la mitad del costo | Sin `ScreenReaderCostTests`; solo `It14PolishTests:134` mira que no haya árbol con el lector apagado | Prueba con FrameAllocations del HUD con el lector apagado (120 cuadros, 0 B) y otra que cuente reconstrucciones del árbol con el lector encendido mientras cambian las métricas |
| QA-V15-05 | mayor | architect | AC-D15-17: la causa de la asignación esporádica del sonar no se buscó | 02-tech-design no tiene la línea; WP-B no lo nombra. El test pasó en las 3 corridas completas de QA y en el bucle | Medirla con FrameAllocations y *callstacks* (o, si no se reproduce en N corridas, decirlo con el N) y escribir la línea en "Cambios por implementación" |

### Menores (una línea cada uno)

- **m-01** (architect): CAP-04 no muestra el resumen `m:ss` que pide la spec §5 (el cuadro está desplazado al gráfico); el `m:ss` se ve en CAP-01 y CAP-03.
- **m-02** (architect): `WpdGuards`/`SimWpdGuards` y las guardas `Assert.Ignore("pendiente WP-…")` de `IconsTests`, `ItFifteenToolingTests` y `FontAndIconAssetsTests` siguen en el código: hoy no se disparan, pero si alguien saca el glifo o la regla USS, la prueba se ignora en lugar de fallar. Sacarlas.
- **m-03** (architect): AC-D15-33 en Dispositivo está implementado (`ScanView`, `ConnectedView` con `ScrollAnchor`) pero sin prueba.
- **m-04** (architect): `It15Ids.cs` pone AC-D15-08 (lectura de documentos) en `DeviceOnlyIds`; la matriz lo informa "EN EQUIPO".
- **m-05** (architect): `DebtCatalogTests.AuxRestoresHull` (§6, riesgo §7) no existe; el `finally` de `DebtCatalog` no tiene prueba.
- **m-06** (architect): varios AC de la 15 con `[TestCase]` (p. ej. `SimRibbonTitleTests.TitlesClearTheRibbon`) no aparecen con su categoría en el XML de Unity a nivel de caso (va en el *suite* parametrizado); la matriz con `-UnityResults` puede verlos como "sin resultado". Confirmar con `-UnityResults`.
- **m-07** (po): el bucle de 20 de AC-D15-17/-39 es de la clase en PlayMode sin `-Simulator` (la clase no depende del simulador); si el PO lo quiere con `-Simulator`, son 20 × 3 arranques de Unity.
- **m-08** (architect): "Resultados medidos (WP-B)" dice `it9` e `it14` "enteros" sin avisar que se tomaron antes de WP-D, y no lista AC-D15-29/-30 como faltantes: corregir esa tabla al cerrar QA-V15-01 a -05, para que el REPORT no la herede.

## 6. Builds, humos y reglas

| Qué | Resultado |
|---|---|
| `build.ps1` normal | 0 · `BuildOutputInspector`: sin simulador, diagnósticos ni tests; sin credenciales ni red de Strava · `RowMate 1.0.0` |
| `build.ps1 -Simulator` | 0 |
| `build.ps1` desarrollo (`dev-smoke`) con `-BootSmoke -CloseSmoke -PerfHistory -WindowSmoke -DbSmoke -BleSmoke -ExportSmoke` | 0. Arranque: mediana 1 397 / 1 400 ms, 10 salidas limpias. Cierre: 7 con `WM_CLOSE`, salida 0. `[PERF] history` arriba (§3). Ventana: ok ± 2 px. `[DB] humo: ok`. **`[BLE] humo: estado=On busquedas=3 ftms=0 fallas=0 colgado=no soloFtms=si`, `[HRM] humo: busquedas=1 sensores=0 ajenos=0 fallas=0` → ok**. Export: 2 archivos iguales a `tools/sessions`, Documentos sin cambios |
| `ProjectSettings` | Sin cambios (`git status` vacío; guardia de los scripts "sin cambios" en cada corrida de Unity y de build); `ROWMATE_SIMULATOR` 0 veces en `ProjectSettings.asset` |
| `HorizonFar.mat` / `HorizonNear.mat` | Sin cambios después de todas las corridas |
| Repo público | Rutas absolutas, usuario, nombre del equipo y la carpeta del *scratchpad*: 0 en los 179 archivos de texto cambiados o nuevos, y 0 en los PNG, MP4, TTF e ICO nuevos o cambiados (búsqueda en bytes). `Usuario` solo aparece como rol en ROADMAP/ART_BIBLE (de antes) |
| UTF-8 | 179 archivos decodifican como UTF-8 estricto; sin BOM; sin dobles codificaciones (búsqueda de las secuencias típicas de UTF-8 leído como Latin-1) |
| Legacy | `git status --porcelain` vacío; no se abrió el módulo de credenciales de Strava |

## 7. Para issues

- Popa del casco (AUX-15-01 lo confirma) y estela del legacy con la 6b (#6), como dice la spec §8.
- `editorProgressionWatts` = `Incremento Watts (+/rep)` en es (spec §8).
- Re-tomar los sets del HUD de la 4a, 4b y 5 con `Vatios`/`Split 500m` (spec §8).
- Los 6 glifos reservados sin uso (`delete_sweep`, `expand_more`, `expand_less`, `landscape`, `arrow_drop_up`, `arrow_drop_down`, WP-A-04).

## Ronda 2

| Campo | Valor |
|---|---|
| Entrada | "Cambios por verificación (ronda 1)" de [02-tech-design](02-tech-design.md) (QA-V15-01 a -05, m-08) y el árbol sin commitear actual |
| Corridas | Todas de QA, en `<scratch>/it15/qa-r2`, con `ROWMATE_DATA_DIR` temporal y una sola instancia de Unity. **La PC no estuvo libre**: la VM de WSL (`vmmemwsl`, ajena, no se tocó) usó entre 8 y 14 de los 24 procesadores lógicos durante todas las corridas (60-87 % de CPU total, medido con los contadores de proceso) |
| Veredicto | **RECHAZADO**: 0 bloqueantes, **2 mayores** (QA-V15-R2-01: el intermitente del #78 volvió con otra causa; QA-V15-R2-02: decisión del PO sobre AC-D15-04), 4 menores. QA-V15-02 a -05 quedan resueltos; QA-V15-01 hizo lo que pedía la corrección, pero AC-D15-04 sigue FAIL |
| Conteo | **PASS 38 · FAIL 2 (D15-04, D15-39) · NO VERIFICABLE 0** (de 40). Partes NO VERIFICABLES dentro de AC (demo del usuario): el Explorador real (D15-13) y los 60 fps con el Narrador (D15-29) |

### R2.1 Pruebas (salida real)

| Corrida | Resultado | Ronda 1 |
|---|---|---|
| `run-tests.ps1 -Matrix` (dotnet) | **4 084: 4 045 pasan, 39 ignorados, 0 fallan**. Matriz: PASA 890, FALLA 0, IGNORADO 2, PENDIENTE UNITY 197, EN EQUIPO 18, SIN TEST 0 | igual |
| Unity EditMode | **4 217: 4 163 pasan, 54 ignorados, 0 fallan** (+1: `WindowMode`) | 4 216 |
| Unity EditMode `-Simulator` | **4 411: 4 357 pasan, 54 ignorados, 0 fallan** | 4 410 |
| Unity PlayMode | 732: **731 pasan, 1 falla** (`SceneBudgetTests(High)` 1,78 ms) (745 s) | 724/724 |
| PlayMode `-Simulator`, corrida 1 | 813: **811 pasan, 2 fallan** (`SceneBudgetTests(High)` 1,75 ms; `HudBudgetTests.SeriesWorstCaseWork` mediana 1,106 ms) (808 s) | 805/805 |
| PlayMode `-Simulator`, corrida 2 | 813: **811 pasan, 2 fallan** (`SceneBudgetTests(High)` 1,70 ms; **`BleAllocationTests.TheReconnectBannerDoesNotAllocatePerFrame`**) (807 s) | 805/805 |
| PlayMode `-Simulator`, corrida 3 (extra, con `ROWMATE_ALLOC_PROFILE` para cazar la pila) | 813: **812 pasan, 1 falla** (`SceneBudgetTests(High)` 1,61 ms). El aviso pasó: no hubo pila que capturar | — |

- Los 8 de PlayMode nuevos (`ItFourteenGapTests` ×5, `ScreenReaderCostTests` ×2, `HudBudgetTests.DelayedScene`) y `ChartAllocationTests` pasaron en las 4 corridas. Sin ignorados nuevos; las guardas `pendiente WP-…` no se dispararon (0 en los XML).
- **Carga externa, no regresión.** Las tres capas de `SceneBudgetTests` subieron en la misma proporción con los mismos dibujos y triángulos: High 1,12 → 1,61-1,78 ms, Medium 0,71 → 1,17-1,21, Low 0,55 → 0,96-0,98 (ronda 1 con la PC libre → esta). `SeriesWorstCaseWork`: 0,749 (ronda 1) → 0,865 / 1,106 / 0,914 / 0,861. Desde la ronda 1 no cambió ningún archivo de la escena (lo cambiado es el gráfico, las pruebas y el arnés). Los topes no se tocaron. AC-D15-36 queda con la evidencia de la ronda 1 (3/3 con la PC libre); en esta ronda sería NO VERIFICABLE por la carga, como prevé el AC.
- `-BootSmoke` sí pasó con la carga: mediana 1 691 / 1 677 ms, diferencia 14 ms, 10 salidas limpias.

### R2.2 Hallazgos de la ronda 1

| ID | Estado | Evidencia |
|---|---|---|
| QA-V15-01 | **Hecho lo que pedía** (medir con pilas y corregir); **AC-D15-04 sigue FAIL** → QA-V15-R2-02 | `[CAT]` en las 4 corridas, siempre igual: entrada **2/48** (antes 7), cambio **10/24** (asignaciones 287 → 91), cursor **2/60** en frío y **0/60** en caliente; `chart-mesh` 0 en 42 dibujos. La causa de lo que queda está medida y escrita (páginas de UIR; cruce de colores de `SegmentedControl`). Revisión de `ChartMeshUpload`: tamaño fijo, `Dispose` al salir del panel, triángulos sobrantes degenerados; sin hallazgos. CAP-04 de la 15 y CAP-15 de la 9: la curva igual que antes |
| QA-V15-02 | **Resuelto** | `ItFourteenGapTests` (`MinWindowLayout`, `HapticEvents`, `DeviceLandscapeScroll`, `SceneLoadingBeforeBlackEnds`, `SceneLoadingDelayed`), `It15GapTests.WindowMode` (EditMode) y `HudBudgetTests.DelayedScene` (`[HBT]` 57/58/58/58 ≤ 60) pasan en todas. `SceneLoadingDelayed` prueba los dos casos del gate QA-G15-03 (sin vuelo, fundido 0 → 1, indicador a 0 a los 160 ms, ningún cuadro con los dos ≥ 0,99) |
| QA-V15-03 | **Resuelto** | `it9` 24/24, `it14` **29/29** (con las 13 que faltaban), `it15` 11/11: todas `passed`, 0 desbordes, 0 sondas que fallan, tomadas después del último cambio de fuente que les toca. Miradas: `it9` CAP-04 y CAP-15 (glifo `download` en tarjetas y barra; `Vatios`; `2:10`); `it14` CAP-06, -11, -14, -23 (el `0`, el eje X; distinta de CAP-27), -24, -25, -26 |
| QA-V15-04 | **Resuelto** | `ScreenReaderCostTests.ReaderOffHudZeroBytesPerFrame` (`[SRC]` 0 en el paso y en 48 dibujos en 120 cuadros; el reloj de prueba asigna solo en los 4 cuadros de 1 Hz, como el resto de las pruebas del HUD) y `ReaderOnMetricsDoNotRebuildTheTree` (sin `Clear` ni reconciliaciones, los mismos nodos) pasan en las 4 corridas |
| QA-V15-05 | **Resuelto** (como pedía la corrección: no reproducida, con el N) | El sonar dio 0 en las 4 corridas de esta ronda: con las del arquitecto y las de QA, **40 corridas** sin la asignación esporádica. La línea está en "Cambios por verificación" |
| m-08 | Resuelto | La tabla de WP-B quedó aclarada |
| m-01, m-03, m-05 a m-07 | Siguen (menores, sin cambios) | — |
| m-02 | Sigue | 19 `Assert.Ignore("pendiente WP-…")` y `WpdGuards.cs` en `Tests/` |
| m-04 | Sigue | `It15Ids.DeviceOnlyIds` todavía tiene AC-D15-08 |

### R2.3 Matriz (solo lo que cambia frente a la ronda 1)

| AC | Evidencia | Resultado |
|---|---|---|
| D15-04 | `[CAT] AC-D15-04 FAIL … entrada 2/48, cambio 10/24, cursor 2/60` (4 corridas) | **FAIL** (QA-V15-R2-02) |
| D15-17 | `ThreeHoursUnder200Ms` en dotnet; sonar 0 en 40 corridas; la línea de la causa ("no reproducida en 36", ahora 40) en 02-tech-design | PASS |
| D15-18 | `it9` regenerado con el glifo | PASS |
| D15-29 | `ScreenReaderCostTests` ×2 + `DialogReturnsFocusToOpener` | PASS (60 fps con el Narrador: demo) |
| D15-30 | Los 6 casos + `DelayedScene` | PASS |
| D15-31 | `it14` con 29 tomas, el INDEX y CAP-01/02 de `tools/icon` | PASS |
| D15-36 | Ronda 1: 3/3 con la PC libre. Ronda 2: fallan por la carga externa (§R2.1); el tope no subió | PASS (ronda 1) |
| D15-39 | **Falló en 1 de 4 corridas completas** (PlayMode normal pasa; `-Simulator` 1 pasa, **2 falla**, 3 pasa) | **FAIL** (QA-V15-R2-01) |
| D15-40 | Conteos, 258 claves, `ProjectSettings` y `.mat` sin cambios, repo limpio. El intermitente va contado en D15-39 | PASS |

El resto, igual que la ronda 1 (PASS).

### R2.4 Issues elegidos (cambios frente a la ronda 1)

| Issue | Estado | Qué queda |
|---|---|---|
| #34 | **En parte** | AC-D15-04: el cruce de colores de `SegmentedControl` y las páginas de UIR de la entrada (con la causa medida), si el PO lo acepta (QA-V15-R2-02) |
| #71 | Resuelto | — (el sonar: no reproducida en 40, con la herramienta lista) |
| #72 | Resuelto | — |
| #78 | **En parte** | El intermitente de `TheReconnectBannerDoesNotAllocatePerFrame` volvió con otra causa (QA-V15-R2-01); repetir los topes sin la VM de WSL (m-09) |
| #80 | En parte (lo previsto) | 60 fps con el Narrador real (demo) |
| #81 | En parte (lo previsto) | Sets de la 2, 3, 10a, 11 y 12 |
| #82 | En parte (lo previsto) | Popa del casco y estela (6b) |

El resto, como en la ronda 1.

### R2.5 Hallazgos nuevos

| ID | Sev. | Resp. | Problema | Evidencia | Corrección |
|---|---|---|---|---|---|
| QA-V15-R2-01 | mayor | architect | `BleAllocationTests.TheReconnectBannerDoesNotAllocatePerFrame` (arreglo del #78) falló en 1 de las 2 corridas completas con simulador que pide la tarea. **No es la causa del #78**: la guarda nueva pasó (la ventana medida termina > 0,1 s antes del intento de los 3 s). La falla es otra: **1 asignación en 1 cuadro del reloj de prueba** (`Scheduler.Advance`), con 0 en el paso y en los dibujos | `play-sim2/playmode-simulator-results.xml`: `caja del enlace (arco de espera): el reloj no asigna: … reloj de prueba 1 en 1 cuadros. Expected: 0 But was: 1`; en las otras 3 corridas, `reloj de prueba 0 en 0 cuadros`. La corrida 3, con `ROWMATE_ALLOC_PROFILE`, no lo reprodujo | Correr la suite completa con `-Simulator` y `ROWMATE_ALLOC_PROFILE` hasta que aparezca (la clase sola no lo reproduce: 31 de 31) y leer la pila con `ProfileAllocations.Dump -rowmate-profile-filter RowMate.Diag.Measured`; corregir lo que asigne en el reloj (un temporizador o una continuación que cae en la ventana según el reparto de CPU). Sin relajar la afirmación |
| QA-V15-R2-02 | mayor | po | AC-D15-04 FAIL con la causa medida y lo que se pudo corregir, hecho (QA-V15-01) | §R2.2 | Decisión por defecto (como el plan B de D15-03): AC-D15-04 se acepta como FAIL con el dato y lo que queda vuelve a #34 con las dos causas medidas; escribirlo con una línea en la spec (§3.1 y §4). La prueba ya impide retroceder (`chart-mesh` 0, cursor en caliente 0, topes de la 9) |

**Menores nuevos**

- **m-09** (architect): `SceneBudgetTests(High)` y `SeriesWorstCaseWork` fallan con la VM de WSL encendida (datos en §R2.1). Repetir AC-D15-36 con la PC libre antes del cierre, sin subir el tope.
- **m-10** (designer): `it14` CAP-26 muestra `00:03` con `1012 m` y `40 kcal` (lecturas fijas del maniquí con el reloj recién arrancado): datos incoherentes en la toma, no en la app.
- **m-11** (architect): la ronda 1 de este documento citaba literalmente las secuencias de doble codificación y hacía saltar la búsqueda de UTF-8; corregido en esta ronda (solo el texto).
- **m-12** (architect): `HapticEvents` repite el patrón de `TheReconnectBannerDoesNotAllocatePerFrame` (caída, `Settle` y el intento de los 3 s); si R2-01 cambia ese tiempo o el `Settle`, revisar las dos.

### R2.6 Builds, humos y reglas

| Qué | Resultado |
|---|---|
| `build.ps1` normal | 0 · `RowMate 1.0.0` · `BuildOutputInspector`: sin simulador, diagnósticos ni tests; sin credenciales ni red de Strava |
| `build.ps1 -Simulator` | 0 |
| Desarrollo (`dev-smoke`) con `-CloseSmoke -PerfHistory -WindowSmoke -DbSmoke -BleSmoke -ExportSmoke` | 0. Cierre: 7 con `WM_CLOSE` (5 en ventana, 2 en pantalla completa), salida 0. `[PERF] history: recarga 11.8 ms · base 97.2 ms · gc 7 · detalle 18.2 ms · rueda p95 6.4 ms · >33 ms 0.0 %`. Ventana (140, 90, 1000 × 640) → igual al reabrir. `[DB] humo: ok`. **`[BLE] humo: estado=On busquedas=3 ftms=0 fallas=0 colgado=no soloFtms=si`; `[HRM] humo: busquedas=1 sensores=0 ajenos=0 fallas=0`**. Export: 2 archivos iguales a `tools/sessions`, Documentos sin cambios |
| `-BootSmoke` | 0: mediana 1 691 / 1 677 ms, 10 salidas limpias |
| `ProjectSettings` | Sin cambios (guardia de los scripts en cada corrida); `ROWMATE_SIMULATOR` 0 veces en `ProjectSettings.asset` |
| `HorizonFar.mat` / `HorizonNear.mat` | Sin cambios |
| Repo público | Búsqueda de rutas de la PC, usuario, carpeta temporal y nombre del equipo en los 253 archivos cambiados o nuevos (texto y binarios) y `git grep` del repo: 0. Solo aparecen los patrones de las pruebas que buscan rutas, nombres de archivos de código y "Usuario" como rol en ROADMAP/ART_BIBLE |
| UTF-8 | 253 archivos: UTF-8 estricto, sin BOM, sin dobles codificaciones (ver m-11) |
| Legacy | `git status --porcelain` vacío; no se abrió el módulo de credenciales de Strava |
| Procesos | Ninguno nuestro quedó abierto; builds solo en `Builds/Windows/` |
