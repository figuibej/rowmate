# It. 15 — Gate de QA (armonía QUÉ ↔ CÓMO)

| Campo | Valor |
|---|---|
| Entrada | [01-po-spec](01-po-spec.md) (40 AC), [02-tech-design](02-tech-design.md), [00-arte-deuda](00-arte-deuda.md) |
| Contra | Los 21 issues del alcance (API pública), el código del árbol y [rowmate-legacy](https://github.com/figuibej/rowmate-legacy) (solo lectura; `git status` vacío; no se leyó el módulo de credenciales de Strava) |
| Línea de base | `run-tests.ps1` (dotnet, `<scratch>/it15/gate`): **4 023 tests, 3 984 pasan, 39 omitidos (todos de equipo), 0 fallan** |
| Veredicto | **APROBADO CON OBSERVACIONES**: 0 bloqueantes, 5 mayores (QA-G15-01 a -05). Se corrigen en los documentos **antes** de implementar la parte que tocan. El resto puede arrancar ya |

## 1. Lo que se confirmó

- **Cobertura**: los 40 AC tienen un componente y un test en la matriz del §6 del diseño. Faltan algunas afirmaciones dentro de los tests: m-03.
- **Diagnósticos citados que coinciden con el código**:
  - `ChartSmoothing.cs:72-77`: los controles no tienen tope.
  - `StatRowView.SetNamePrefix` no guarda el prefijo, y `SnapshotManifest.Passed` ignora `Aspect`.
  - `ChartGeometry.cs:90/123/136`.
  - `TcxBuilder.cs:76`: la vuelta es `last − first`, sin piso.
  - `TcxOptions` es un `record` posicional y `bundleVersion` es `1.0.0`. La raíz ya declara `xmlns:xsi`, así que `xsi:type` es válido.
  - `Creator` va después de `Notes`, como pide el orden de `Activity_t`.
  - `run-tests.ps1` no lee `DeviceOnlyIds`.
  - `ToastLayer.TargetBottom` usa la barra de pestañas.
  - `HudCard.SetLabel` es un `Label` con USS (`letter-spacing: 8px`).
  - `HudReflowMotion` con "reducir movimiento" ya es instantáneo, como pide la spec.
  - El catálogo tiene 256 claves por idioma y la cabecera de `L10nKeys` dice 202.
- **DT15-14**: `person` se usa (`ShellStore.cs:34`) y `link_off` también (`SimulatorPanelView.cs:73`). No hay ningún uso de `Icons.Cloud*` ni de `Icons.Link`.
- **DT15-04**: la tarjeta toma las estadísticas de los puntos (`HistoryQuery.cs:54`, y `SessionStats.Empty` si no hay). `HasData` equivale a `IsExportable` y deja afuera a la `F-9` n.º 4.
- **DT15-18**: coincide con el cambio sin commitear del coordinador. La guarda es una afirmación dura (`NextAttemptAt − ahora > ventana + 0,1`), no un `Ignore`, y el arco sigue en > 0,5.
- **#71a**: los 4 tests de `ItTwelveToolingTests` hoy **pasan**, porque sus condiciones ya se cumplen. Sacar las guardas `Assert.Ignore` alcanza.
- **`uv`**: tiene un Python propio, y fontTools 4.67.0 y 4.68.0 están en la caché. Las recetas de los íconos y de `freeze_tnum.py` corren sin instalar nada.
- **DV**: la numeración sigue a DV-139 sin choques.

## 2. Issues

| ID | Sev. | Resp. | Problema | Evidencia | Corrección |
|---|---|---|---|---|---|
| QA-G15-01 | mayor | po | AC-D15-34 inventa una estela que no es del legacy y rompe un AC ya verificado. Hoy no hay estela: el "rectángulo" es casi seguro la popa del casco (arte D15-05). AC-SCN-13-04 (verificado) dice "el agua degradada no tiene estela (es de la 6b)". La estela del legacy es otra: una **V que se abre** hacia atrás (`0,3 + back·0,35`), con turbulencia en el centro y una fuerza de `speed/4`. La de la spec se **angosta** hacia la punta y tiene un alfa fijo. Se descarta cuando entre la 6b | `shaders/water.frag:84-94` y `test/scene/water_painter_test.dart:45` (legacy); 06a spec:331; DT15-17 | AC-D15-34 pasa a ser la toma de diagnóstico AUX-15-01 (arte D15-05/A15-12), y la popa va a issues. Si el PO quiere la estela igual: portar la V del legacy (fuerza `speed/4`) y enmendar AC-SCN-13-04 a la vista, no el parche angosto |
| QA-G15-02 | mayor | po | AC-D15-25 no se puede cumplir. Las cifras tabulares de Inter son ≈ 30 % más anchas que las de `RowMate Numeric SC`: medido, Inter SemiBold `0` = 0,660 em contra 0,507 em de la SC. Una fecha con 12 cifras a 13 dp crece unos 24 dp, así que "los anchos no cambian más de 1 dp" (y `NumericFontTests` ±1 dp) no puede dar. Además cambia el layout verificado en las It. 9 y 14 (recorte del renglón 1 de la tarjeta y `AxisCrowding`) | `UI/Fonts/Inter/Inter-SemiBold.ttf` y `UI/Fonts/RowMateNumericSC/RowMateNumericSC-SemiBold.ttf` (hmtx); ART_BIBLE §4.2 (TY-02) | Modo rápido: quedarse con la SC (WD-01 y arte D15-01) y cerrar ese punto de #76/#37 con esa razón. Si no, sacar el criterio de ±1 dp y pedir sondas de recorte en L640 a 1,3× y en P360, y re-tomar las capturas |
| QA-G15-03 | mayor | po · architect | AC-D15-30 `SceneLoading` se contradice. Pide "la entrada de la cámara se ve completa" y a la vez "sin romper AC-SCN-01-07", que dice "sin vuelo". El código, con la escena demorada, hace `ShowInPlace` más un fundido de 0,6 s. El diseño no dice qué afirma el test | `ImmersiveWorkoutPage.cs:596-601`; `ChaseCameraMotion.cs:390-395`; 06a spec:302; 14 spec AC-DEBT-09 | Adoptar el arte 82-c. Si la escena queda lista **antes** de que termine el negro: vuelo completo de 1 600 ms. Si queda lista **después**: en su lugar, con el fundido de 600 ms, y el indicador funde a 0 en 160 ms en el mismo cuadro. El test cubre los dos casos |
| QA-G15-04 | mayor | po | AC-D15-22 dice "cada métrica se llama igual en todas las pantallas", pero lo que queda igual lo contradice: el pulso del HUD es `hudBpm` = `BPM` (AC-D15-24 lo conserva) y el editor dice `Incremento Watts (+/rep)` en es (`editorProgressionWatts`). Sin un alcance cerrado, `MetricNamesTests.OneNamePerMetric` o falla o no prueba nada | `strings.es.json` (`hudBpm`, `editorProgressionWatts`); spec §3.5 | Que el AC enumere las familias de claves que cubre (`hud*` de rótulo, `historyStat*`, `sessionChart*`, `metric*`) y las excepciones (`BPM` como unidad del HUD y los campos del editor). Otra opción: sumar `editorProgressionWatts` → `Incremento Vatios` como 4.ª clave que cambia |
| QA-G15-05 | mayor | architect | Para #34 (AC-D15-03, que dice "Entero") el diseño solo agrega la sonda. En el Editor, el peor cuadro de una recarga dio **96 ms**, y el issue ya propone agregar sin materializar los 72 000 `DataPoint` o usar una lista virtual. Si el build pasa de 50 ms no hay plan | 09 REPORT:60; issue #34 | Agregar el paso: medir primero en el build y, si pasa de 50 ms, aplicar el plan B (agregados en SQL sin materializar, o `ListView` virtual) con su test. O declarar desde ya que AC-D15-03 puede quedar FAIL y volver a #34 |

### Menores (una línea cada uno)

- **m-01** (architect): §2.2 no lista los tests de la 12 que cambian. Son `TcxBuilderTests.NegativeDistanceKept` (hoy afirma `-15`, :111-118), `NotesAfterLastLap` (que con `Creator` rompe `lines[notes+1] == "</Activity>"`, :199) y las filas `hudSplit`/`hudWatts`/`historyStatPower` de `CatalogParityTests` (:63-64, :177). Listarlos con su AC (AC-D15-40).
- **m-02** (architect): el respaldo `Romance Standard Time` de D15-16c no hace falta. `TestZones.Madrid` es `CreateCustomTimeZone` (`TestZones.cs:29-35`) y no busca la zona del sistema.
- **m-03** (architect): a `HistoryCardExportTests` le faltan el nombre accesible `exportTcxFor` y la ayuda `exportTcx` (AC-D15-09). AC-D15-05 no prueba los nombres `session-summary-stat-{j}`.
- **m-04** (po · architect): en AC-D15-36, la spec dice "se corrige" y el riesgo del diseño admite "NO VERIFICABLE con el dato medido". Alinearlos: manda la spec, o el PO acepta el NV.
- **m-05** (designer): `00-arte-deuda` contradice la spec en más de 10 puntos (sombra 45 %/σ6/+4, 20/5 %, menú ⋮ y sello, sin recorte de íconos, Numeric SC, estela N/A, tomas del set `it15`). El §1 del diseño lo resuelve, pero falta una nota "reemplazado por 01/02" en la cabecera para que WP-D no implemente esos valores.
- **m-06** (po): AC-D15-01 dice "ningún vértice de la línea ni del área". Con un trazo de 2 dp y su borde suavizado, la malla sale ±1 dp. El test va sobre la línea central teselada, como en el diseño; aclararlo en la spec.
- **m-07** (po): en §4 de #81, "qué queda" omite el set `it12`, que el issue también nombra.
- **m-08** (architect): si la estela sigue (QA-G15-01), `WaterInteractionRenderer.Capacity = 16`. Confirmar que entra una instancia más o subir la capacidad con su test de 0 B.
- **m-09** (architect): Unity guarda también la ventana y el modo en sus `Screenmanager*` del registro, compartidos entre el build normal y el de desarrollo. La restauración de DT15-08 tiene que ganarle a eso, y `-WindowSmoke` no tiene que depender del registro.
- **m-10** (architect): `RowMate.ico` en `Assets/RowMate/Art/Identity/` genera un `.meta` y Unity lo importa como `DefaultAsset`. Está bien si se acepta, pero hay que anotarlo, o sacarlo a `tools/icon/out/`.

## 3. Matriz de cobertura (gate)

| AC | Estado en el gate |
|---|---|
| D15-01, -02, -05 a -24, -26 a -29, -31 a -33, -35 a -40 | Cubiertos por el §6 del diseño (salvo m-01, m-03 y m-06) |
| D15-03 | Cubierto el test; falta el plan si falla (QA-G15-05) |
| D15-04 | Cubierto (`ChartAllocationTests`); la causa se mide primero (DT15-16) |
| D15-22 | Test sin alcance cerrado (QA-G15-04) |
| D15-25 | Criterio imposible (QA-G15-02) |
| D15-30 | `SceneLoading` contradictorio (QA-G15-03) |
| D15-34 | Contradice AC-SCN-13-04 y el legacy (QA-G15-01) |
| [Device-Win] D15-03, -19 (humo) y -37 | Verificables en la PC (build de desarrollo con `ROWMATE_DATA_DIR` temporal); el Explorador real (D15-13) y el Narrador real (D15-29) quedan como NO VERIFICABLE (demo) |

## 4. Para issues

- La popa del casco que se lee como una tabla (arte §5), con la evidencia de AUX-15-01.
- La estela del legacy (V con fuerza `speed/4`) va con la 6b (#6), no como parche.
