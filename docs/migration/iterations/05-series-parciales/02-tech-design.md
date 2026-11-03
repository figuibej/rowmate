# It. 5 — Series en vivo: parciales de 500 m y repeticiones: diseño técnico

| Campo | Valor |
|---|---|
| Autor | unity-architect |
| Fecha | 2026-10-30 |
| Iteración | 5 · `05-series-parciales` ([ROADMAP §It. 5](../../ROADMAP.md)) |
| Entrada | [01-po-spec](01-po-spec.md) (74 AC = 46 nuevos + 28 de iteraciones anteriores; LT-47 a LT-61; CAP-01 a -26, DEMO-01 a -12, VID-01 a -09; DV-48 a DV-51; P-01 a P-13; SD-01 a SD-19 decididos en §11.3; corregida por el gate); [04-design-notes](04-design-notes.md) (R-01 a R-15, DD5-13 a DD5-27, §9: integradas en §9 de este documento); [03-qa-harmony-paridad](03-qa-harmony-paridad.md) y [03-qa-harmony-tecnica](03-qa-harmony-tecnica.md) (el gate); [00-deuda-4b](00-deuda-4b.md) (lo saldado al empezar: el HUD en el negro, `HudWarmup`, O-1, O-4, O-5, QA-V4b-R2-03, el modo compacto de la barra libre); [00-deuda-diseno](00-deuda-diseno.md) (la base de diseño de los paneles y los pedidos **DD5-01 a DD5-12**); [02-tech-design de la 4b](../04b-hud-exr/02-tech-design.md) con sus "Cambios por gate QA", "por implementación" y "por verificación"; [REPORT de la 4b](../04b-hud-exr/REPORT.md) §7 y §9; [ARCHITECTURE](../../architecture/ARCHITECTURE.md) §2, §3, §6, §7.8, §9.4, §13.2, §15, §20.2; ADR [012](../../architecture/adr/012-paridad-numerica-dart.md), [016](../../architecture/adr/016-tokens-de-diseno.md), [017](../../architecture/adr/017-segundo-plano.md) y [020](../../architecture/adr/020-reloj-despierto.md); [ART_BIBLE](../../design/ART_BIBLE.md) §3.4, §5.2, §10.4; [ANIMATION_BIBLE](../../design/ANIMATION_BIBLE.md) §0.1, §2.3, §2.10, §2.11 |
| Legacy leído (solo lectura, `3ae0f3a`) | [rowmate-legacy](https://github.com/figuibej/rowmate-legacy): `lib/features/workout/series_tracker.dart` y `series_panels.dart` (enteros), `lib/features/workout/workout_provider.dart:1-328`, `lib/features/workout/immersive_workout_screen.dart:85-120` y `:590-700`, `test/series_tracker_test.dart` y `test/series_panels_test.dart` (enteros). No se leyó `lib/core/strava/strava_secrets.dart`. `git -C ../rowmate-legacy status --porcelain` vacío al empezar y al terminar |
| Código leído (este repo) | `Core/Workout/{WorkoutEngine,WorkoutSnapshot,SanitizedDistance,WorkoutFormats}.cs`, `Core/Models/StepPosition.cs`, `Core/Design/{HudGeometry,HudTimelines,Icons,Tokens,FontAssetPaths}.cs`, `App/Workout/{WorkoutHudState,WorkoutHudStore}.cs`, `Infrastructure/Database/DatabaseFiles.cs`, `Bootstrap/{AppRoot,DataLocation}.cs`, `Presentation/Workout/{HudView,HudZones,CenterCallouts,ImmersiveWorkoutPage,HudText}.cs`, `Presentation/Controls/TabularNumber.cs`, `Tests/PlayMode/HudBudgetTests.cs`, `Tests/UiSupport/{GcAllocations,UiQuery}.cs`, `Tests/Support/{RoutineSeeds,RowerScripts}.cs`, `Snapshots/HudExrCatalog.cs`, `tools/oracle/` (el conductor del motor y `bin/routine_vectors.dart`); para el gate, además: `Tests/EditMode/Workout/SeriesPanelsTests.cs`, `Tests/Support/{ScriptedRower,LifecycleHarness}.cs`, `Tests/UiSupport/{UiTestHost,PixelProbe}.cs`, `Presentation/Controls/RmButton.cs`, `Presentation/Workout/{StepInfoBar,SealRow,HudWarmup}.cs`, `Presentation/Motion/Hud/ImmersiveCascadeMotion.cs`, `Core/Common/ObservableValue.cs` y las consultas de `hud-seal-*` y `hud-up-next*` en `Snapshots/` y `Tests/PlayMode/` |
| Verificación previa | Un puerto en Python del `SeriesTracker` del legacy con el modelo de tiempo de la spec (§0.4: L(k) en k − 0,5; la pausa pierde la fracción, Q-58), en `<scratch>/it5/arch/sim.py`, sin red. Reproduce **todos** los números de la spec que se usan acá: `Series 600` con `R-4` (170 s: `0:15.0` `60 m` `#1 2:05.0` `Rep 1/3 · 560 m`; 180: `0:25.0` `100 m`; 190: `640 m`; 197: `668 m`; 201: `Rep 1` 680 / 600 / 150 s; 250: `0:50.0` `200 m`; 325: `#1`; 371: [`Rep 2`, `Rep 1`]; 541: fuera de la serie), `Trabajo 1000` con `R-4`, `R-4R`, `R-4Z` y `R-4D` (termina en 250, 251, 252 y 254; `#1` de 125, 126, 127 y 129 s), `R-3` a los 170 s (`#1` 166,667 s; `10 m` y 3,333 s), `Fondo 3000` con `R-4A` a los 600 s (`#5` a `#3` de 100 s, `#2` y `#1` de 125 s; `0:50.0` `250 m`), la pausa de 20,7 a 50,7 s (`0:20.0` `80 m`, después `0:21.0` `84 m`, `#1` en 125), `Cinco reps` a los 541 y 545 s y `Dos series` a los 120, 121, 181 y 241 s |
| Estado | **Diseño corregido por el gate de armonía** ([03-qa-harmony-paridad](03-qa-harmony-paridad.md), [03-qa-harmony-tecnica](03-qa-harmony-tecnica.md)) e **integrado con las notas del Designer** ([04-design-notes](04-design-notes.md): R-01 a R-15, DD5-13 a DD5-27; §9) y con la spec corregida (SD-01 a SD-19 decididos, spec §11.3). Sin código. Orden, **en serie**: **WP-A** → **WP-B0** (la deuda de la 4b, M5 y la sonda S-B5) → **WP-D1** → **WP-B1** → **WP-B2** → **WP-B3** → **WP-D2** (§7). Lo que cambió: "Cambios por notas del Designer" (§9) y "Cambios por gate QA" (al final) |

Convenciones: las de la 4b. Rutas relativas a `Assets/RowMate/` salvo que empiecen con `tools/`, `docs/` o `ProjectSettings/`. Marcas: **N** nuevo, **C** cambia, **G** generado. Dueño entre corchetes: **[A]** WP-A, **[B0]** a **[B3]** las entregas de WP-B, **[D1]** y **[D2]** las de WP-D (§7). `dp` = px de USS; `s` = `hudScale`; **`sp`** = la escala de los paneles (§3.2: igual a `s` salvo en W1280, 1,20); "base" = antes de multiplicar. Perfiles: **TV** 411 × 914, **P360** 360 × 800, **TH** 914 × 411, **L640** 640 × 360, **W1280** 1280 × 720. Las citas `lib/...:línea` y `test/...:línea` son del legacy. "Segundo" = un paso de 1 s del reloj activo del motor (`ActiveClock`). "Parciales" = el panel de vueltas; "repeticiones" = el de `Series anteriores`; "cerrado" = el chip; "abierto" = expandido.

---

## 0. Resumen

1. **Un solo saneamiento, una sola fuente** (spec §12 punto 2; Q-55, DV-26; AC-WKT-07-09). El `SeriesTracker` (puerto fiel de `series_tracker.dart`) es **dueño** del único `SanitizedDistance` de la sesión. El motor deja de tener su `_meter`: el paso por distancia lee `SeriesTracker.Meters`, que es la misma suma, en el mismo segundo, con la misma regla. No hay dos copias que puedan divergir (una prueba de código lo fija, §6.3).
2. **El tracker vive en el motor y se alimenta en el segundo** (AC-WKT-07-03; `workout_provider.dart:176-186`): `Update` antes de los objetivos y del fin del paso; `Begin` al iniciar la rutina y al volver a reposo (`:128`, `:315`); `Rebase` al reanudar (`:248`). Nada en pausa, en el libre ni después del fin. La foto del motor suma `Series` (un `SeriesState` inmutable que el tracker rehace solo cuando cambia, una vez por segundo). Nada se guarda (AC-WKT-07-11).
3. **Lo que muestra cada panel es una regla pura** (`SeriesPanels.For`, Core): parciales solo con rutina; repeticiones solo en serie (LT-61); las 4 últimas vueltas, la más reciente primero; el segundo de atraso en el cambio de paso sale solo del tracker (P-01); **DV-48** (el primer cuadro ya muestra el primer paso) es una regla de presentación encima del tracker, así el oráculo sigue comparando el tracker crudo.
4. **El oráculo** (AC-WKT-07-12): un script de Dart puro (`tools/oracle/bin/series_vectors.dart`) corre el `SeriesTracker` del legacy con los guiones de la spec (los 12 casos legacy, las rutinas de `F-5` con los nueve remos, las pausas y el paso de 0 s). Cada guion trae sus **entradas** (qué paso, qué lectura y qué reloj recibe el tracker en cada segundo, con la regla de pasos de la app: DV-26) y sus **estados**. dotnet compara dos cosas: el tracker de la app con esas entradas (paridad del tracker) y que el motor de la app le da **exactamente** esas entradas (paridad de la alimentación).
5. **Las preferencias llegan** (spec §12 punto 4; ARCHITECTURE §9.4): `IPreferences` (Core), `JsonFilePreferences` (Infrastructure: `prefs.json` en la carpeta privada de la app, la misma de la base, compartido por los dos builds; lectura en el arranque, escritura fuera del hilo principal, entera o nada) y `SeriesPanelPrefs` (App: las dos claves del legacy, cerrados por defecto, el toque antes de la lectura gana). Ninguna prueba ni toma toca el archivo del usuario (AC-HIS-20-04): las pruebas usan memoria o un archivo temporal, el arranque en desarrollo usa `ROWMATE_DATA_DIR`.
6. **La UI sigue la base del Designer, ya decidida por el gate** (spec §11.3, SD-01 a SD-19; DV-50 y DV-51 van al usuario): **137 base de ancho fijo**, cerrado y abierto; la columna a 8·s debajo de SPM; la cuenta grande y `PAUSA` siempre en su lugar de la 4b; en W1280 `sp` = 1,20 con las filas desplazables adentro de cada panel; en TH y L640 la columna entera se desplaza. **Todos los números son constantes** (`SeriesPanelGeometry`, `MotionSpecs`): si el usuario rechaza DV-50 o DV-51 (140/200, 10·s, los tamaños del legacy), cambian las constantes, y la regla de la cuenta grande (que ya existe, §3.4) la esconde cuando un panel abierto ocupa su lugar (P-04).
7. **Un elemento por panel** (DD5-04; R4a-03). Cada panel es un `VisualElement` que **dibuja todo** en su `generateVisualContent` (el vidrio sin reflejo, la cabecera, la flecha, la vuelta en curso con sus celdas tabulares y su rodillo, las filas, la línea `Rep`, los vacíos en dos renglones parejos, la marca de la fila nueva y el anillo de foco). **Sin recortes**: cada renglón se dibuja entero con un alfa que sale de **su tinta** contra el vidrio, contra la ventana de filas y, solo en `ColumnScroll`, contra la máscara de la columna (las tres reglas de 04-design-notes §5.3; DD5-13 a DD5-15). **Nada se anima por *layout*** (ANIMATION_BIBLE §0.1 principio 5): el *layout* va a su estado final en el cuadro del cambio; lo propio del panel es dibujo, y lo que mueve el panel entero (la cascada, la salida, el FLIP del de abajo, la columna que sigue a SPM y el desplazamiento de la columna) es `style.translate` / `style.opacity`, con **un solo escritor**, `SeriesColumn` (DT5-14). Los toques van por la selección real de UI Toolkit, con un hijo oculto que agranda la caja hasta el área de toque (DT5-15).
8. **Presupuesto**: +2 elementos (los dos paneles) sobre el peor caso de la 4b (58-59). **M5** (WP-B0, −1) ya deja ≤ 60; **una junta más** (WP-B1, −1: el sello o `Próximo` en un elemento dibujado, con los nombres de hoy como ranuras) deja margen para la 6a. WP-B0 mide el número real primero; si no se llega, el tope pasa a 61 con la medición escrita en ARCHITECTURE §20.2 (§3.6).
9. **Motion** (WP-D1): `SeriesPanelMotion` (abrir y cerrar con `spring.smooth` solo en el alto; **el alto que cambia por un dato con el empuje** de la lista, `ease.move`, y la cola que baja con él; la fila nueva en 80-240 con su marca; el cambio de contenido **por pieza**; el *hover*) y `SeriesColumnMotion` (la serie que empieza o termina, la columna que sigue a SPM, la rueda). Leen líneas de tiempo puras (`SeriesTimelines`, WP-A, probadas contra las tablas de 04-design-notes §6) y no conocen las vistas (el contrato de la 4b, §7.4). **0 B por cuadro medido en cuadros reales**, con el repintado adentro y un control positivo (§3.6, DT5-18).
10. **Capturas**: el conjunto `it5` (`SeriesCatalog`) con CAP-01 a -26, DEMO-01 a -12 y VID-01 a -09, una preferencia temporal por toma, la regla de toma de la 4b más las fases finitas de los paneles; dos instantes se corren por la regla (CAP-11 a 881,2 s, DEMO-11 a 31,6 s: DT5-04). La regeneración única de `it4b` (las tomas con rutina, con los chips) con dos corridas iguales.

| Pregunta de la tarea | Dónde |
|---|---|
| 1) Árbol de archivos | §1 |
| 2) Reglas puras: tracker de parciales (con el saneamiento compartido), de repeticiones, visibilidad, preferencias, formatos | §2 |
| 3) UI de los paneles, convivencia con la cuenta grande y el sello, presupuesto | §3 |
| 4) Motion: desplegar, plegar, vuelta nueva, reducir movimiento, sin tocar el dato, nada animando al salir, 0 B | §4 |
| 5) Capturas y videos | §5 |
| 6) Tests, matriz AC → test / captura, los 15 casos legacy | §6 |
| 7) Paquetes, archivos disjuntos, contrato de motion y contrato USS | §7 |
| 8) Riesgos y desvíos | §8 |
| Las notas del Designer, integradas | §9 |
| El gate de armonía | "Cambios por gate QA" |

---

## 1. Estructura

### 1.1 Árbol de archivos

#### Core (`RowMate.Core`, puro)

| Archivo | Marca | Contenido |
|---|---|---|
| `Core/Workout/SanitizedDistance.cs` | C [A] | `Update` devuelve una `DistanceSample` (lo que había antes, lo que sumó, Δt): la usa el tracker para la interpolación y los segundos de trabajo. La regla no cambia (§2.1; `series_tracker.dart:43-46`, `:120-127`, `:143`) |
| `Core/Workout/SeriesTracker.cs` | N [A] | `Lap`, `RepSummary`, `SeriesState` y `SeriesTracker`: el puerto de `series_tracker.dart:1-183`, dueño del único `SanitizedDistance` (§2.2) |
| `Core/Workout/SeriesPanels.cs` | N [A] | `SeriesPanelKind`, `SeriesPanelsContent` y `SeriesPanels.For`: qué muestra cada panel (visibilidad, las 4 últimas, DV-48) (§2.4; `series_panels.dart:160-196`, `:279-317`, `:330-344`; `immersive_workout_screen.dart:105-106`) |
| `Core/Workout/WorkoutEngine.cs` | C [A] | `_series` en lugar de `_meter`: `Begin`, `Rebase`, `Update` en el segundo, el paso por distancia con `_series.Meters`; `Series` (el tracker, lectura) y la foto con `Series` (§2.3) |
| `Core/Workout/WorkoutSnapshot.cs` | C [A] | + `init SeriesState? Series` (§2.3) |
| `Core/Ports/IPreferences.cs` | N [A] | el puerto de preferencias (§2.7; ARCHITECTURE §9.4) |
| `Core/Common/MemoryPreferences.cs` | N [A] | las preferencias en memoria (la composición sin archivo: dotnet, pruebas, arnés) |
| `Core/Design/SeriesPanelGeometry.cs` | N [A] | el ancho, la columna (techo con y sin objetivo, piso), los altos naturales, la vista mínima, `sp` y las reglas 1 a 4, el reparto del lugar (en `RowsScroll`, con el mínimo de repeticiones reservado: DD5-26), el área de toque (tope 56, mínimo 48), la zona del bote, la anatomía y la vertical de cada renglón (DD5-23), las reglas de alfa por la tinta (DD5-13 a DD5-15), la escala de ajuste de los textos, la de título por idioma (DD5-20), el corte parejo de los vacíos (DD5-19) y la regla de la cuenta grande y `PAUSA` con los paneles (§3.2, §3.4; DD5-03) |
| `Core/Design/SeriesTimelines.cs` | N [A] | las líneas de tiempo puras de los paneles y sus *structs* de valores (§4.1; 04-design-notes §6, que revisa 00-deuda-diseno §4.1 a §4.6) |
| `Core/Design/HudTimelines.cs` | C [A] | `HudPiece` + `SeriesLaps` y `SeriesReps`, con el tramo de SPM en `ImmersiveTimeline` (§4.1) |
| `Core/Design/MotionSpecs.cs` | C [A] | el bloque de la 5 (00-deuda-diseno §4.7) |
| `Core/Design/Tokens.cs`, `Presentation/Styles/Tokens.uss` (G), `Typography.uss` (G) | C [A] | `num.lap` 34/36 → 28/28, `num.row` 24/28 → 20/22, `num.xs` 13/16/700 nuevo: 136 → 137 (DD5-08; excepción de dueño, §7.6) |
| `Core/Localization/L10nKeys.cs`, `Localization/strings.es.json`, `strings.en.json` | C [A] | las 9 claves de spec §11.2: 214 → **223** (§2.6) |

#### App (`RowMate.App`, puro)

| Archivo | Marca | Contenido |
|---|---|---|
| `App/Workout/SeriesPanelPrefs.cs` | N [A] | las claves `immersive.lapsExpanded` / `immersive.repsExpanded`, cerrados por defecto, el toque antes de la lectura gana, el guardado en el acto (§2.7; `series_panels.dart:120-156`) |
| `App/Workout/SeriesPanelsExpansion.cs` | N [A] | `ISeriesPanelsExpansion` (lo que la vista lee y toca: el estado abierto o cerrado y `Toggle`), que implementan `SeriesPanelPrefs` y `LocalSeriesPanelsExpansion` (en memoria, sin preferencias: el precalentado, QA-G5T-05) (§2.7) |
| `App/Workout/WorkoutHudState.cs` | C [A] | `HudLapRow`, `HudRepRow`, `HudSeries`; `WorkoutHudState` + `init HudSeries? Series` (§2.5) |
| `App/Workout/WorkoutHudStore.cs` | C [A] | arma `HudSeries` con una caché por revisión del tracker y por idioma; `ComposeSeries`, la composición pura, pública para las pruebas que arman el tracker a mano (LT-59, LT-60) (§2.5) |
| `App/Workout/HudSeriesChange.cs` | N [A] | qué cambió entre dos `HudSeries`, **por pieza** (DD5-18): la parte de arriba de parciales, las vueltas nuevas y las que se vacían, la repetición nueva, el vacío de repeticiones, la serie que empieza o termina: lo que anima la vista (§2.5) |
| `App/Composition/AppServices.cs`, `AppComposer.cs`, `AppContext.cs` | C [A] | + `IPreferences? Preferences` (`init`; null = en memoria) y `AppContext.SeriesPanels` (uno por app) (§2.8) |

#### Infrastructure

| Archivo | Marca | Contenido |
|---|---|---|
| `Infrastructure/Preferences/JsonFilePreferences.cs` | N [A] | `prefs.json` con lectura sincrónica al construir, escritura en un ejecutor serie, temporal + reemplazo, claves desconocidas conservadas (§2.7) |
| `Infrastructure/Preferences/PreferencesFiles.cs` | N [A] | el nombre del archivo (`prefs.json`, el mismo para los dos builds) |
| `Infrastructure/Preferences/IPreferencesFileOps.cs` | N [A] | (interno) las operaciones de archivo, para provocar fallas en las pruebas |

#### Presentation (`RowMate.Presentation`, Unity)

| Archivo | Marca | Contenido |
|---|---|---|
| `Presentation/Controls/DrawnText.cs` | N [B1] | lo que el texto dibujado necesita: los `FontAsset` por clase de tipo (con *holders* ocultos, como los afijos de `TabularNumber`), la medida de una corrida (`MeasureTextSize` del *holder*, solo con panel) y la de una corrida con *tracking* (avances del `FontAsset`), los extremos de tinta de cada corrida (04-design-notes §4.6), el corte parejo en dos renglones (DD5-19), el recorte con `…` en el piso de escala, el dibujo con `MeshGenerationContext.DrawText` con alfa y escala; `IDrawnTextSource` para las pruebas (§3.1) |
| `Presentation/Controls/TabularCells.cs` | N [B1] | celdas tabulares con el rodillo de 180 (`DigitRoll`) **dibujadas dentro de otro elemento**: la vuelta en curso, los tiempos, los splits, los metros y la línea `Rep` (§3.1) |
| `Presentation/Workout/SeriesPanelView.cs` | N [B1] | el panel (uno de cada tipo): un elemento que dibuja todo, con su hijo oculto de área de toque (`.rm-hud-series-panel__hit`, DT5-15) y su `ContainsPoint` (§3.1) |
| `Presentation/Workout/SeriesColumn.cs` | N [B1, B2] | (no es un elemento) coloca los dos paneles con `SeriesPanelGeometry`; es **el único que escribe `style.translate` y `style.opacity` de los paneles**: compone la cascada, la salida, el FLIP del de abajo, la serie que aparece, la columna que sigue a SPM y el desplazamiento de la columna (DT5-14); el desplazamiento (dedo, arrastre y rueda, DD5-27), la serie que empieza o termina y el foco que se pierde (§3.2, §3.3) |
| `Presentation/Workout/HudView.cs` | C [B1] | arma los paneles con rutina y con el `ISeriesPanelsExpansion` que le pasen (por defecto, `AppContext.SeriesPanels`); `Apply` les pasa `state.Series`; `Series` (el `SeriesColumn`, para que la página le pase la cascada y la salida); `Freeze`; `Dispose` (§3.3) |
| `Presentation/Workout/HudZones.cs` | C [B1] | `SeriesPanelGeometry.Column` y `Place` con cada disposición; la coloca en la columna; `SeriesLayout` para la página, las pruebas y el arnés (§3.3) |
| `Presentation/Workout/CenterCallouts.cs` | C [B1] | `SetBlockedByPanels(bool, bool instant)`: la regla de §3.4 (con 137 nunca se activa) |
| `Presentation/Workout/StepInfoBar.cs` | C [B0, B1] | B0: M5 (§7.5); B1, si la sonda elige `Próximo`: la junta (§3.6) |
| `Presentation/Workout/SealRow.cs` | C [B1] | si la sonda elige el sello: la junta, con `hud-seal-title` como elemento y `hud-seal-text` como ranura (§3.6) |
| `Presentation/Workout/ImmersiveWorkoutPage.cs` | C [B2] | el anillo de Tab con los paneles; las piezas de la cascada y de la salida de los paneles, que pasa a `HudView.Series` en lugar de escribir sus estilos (DT5-14); `SetInteractive` de los paneles; la regla del foco que se pierde (§3.5) |
| `Presentation/Workout/HudWarmup.cs` | C [B2] | el precalentado con los dos paneles abiertos y con contenido, con un `LocalSeriesPanelsExpansion` (nunca las preferencias: QA-G5T-05; QA-V4b-06 sigue valiendo, DD5-12) |
| `Presentation/Motion/Hud/SeriesPanelMotion.cs`, `SeriesColumnMotion.cs` | N [D1] | los `IMotion` de la 5 (§4.2) |
| `Presentation/Motion/Hud/ImmersiveCascadeMotion.cs` | C [D1] | las dos piezas nuevas en su arreglo (§4.1) |
| `Presentation/Styles/Base.uss` | C [B0, B1] | el contrato de §7.5 (B0: M5; B1: los paneles) |

#### Bootstrap, simulador, arnés, soporte de pruebas, herramientas y documentos

| Archivo | Marca | Contenido |
|---|---|---|
| `Bootstrap/DataLocation.cs` | C [B2] | `PreferencesFile(platform)`: `prefs.json` en la carpeta de la base (en desarrollo, `ROWMATE_DATA_DIR`) (§2.7) |
| `Bootstrap/AppRoot.cs` | C [B2] | `JsonFilePreferences` en la composición; la medida `[PERF]` de la 5 (solo en desarrollo, §3.7) |
| `Editor/Build/BuildOutputInspector.cs` | C [B3] | los argumentos nuevos de medida, prohibidos en el build normal |
| `SimulatorPresentation/FrameTimeProbe.cs` | C [B2] | la etiqueta de la medida de los paneles (§3.7) |
| `Snapshots/SeriesCatalog.cs` | N [B3] | el conjunto `it5` (§5) |
| `Snapshots/{SnapshotCatalog,SnapshotCli,SnapshotRunner,ScenarioScript,SnapshotTakeRule,WrenchAudit}.cs` | C [B3] | `-snapshotSet it5` por defecto; una preferencia temporal por toma; `-seed F5` y `-prefs`; las lecturas que faltan (`R-4D`); los `IMotion` de la 5 en la regla de toma; los paneles en `HudOverlap` y `WrenchAudit` (§5) |
| `Tests/Support/RecordingPreferences.cs` | N [A] | preferencias en memoria que anotan cada escritura, pueden fallar al guardar y pueden demorar la lectura |
| `Tests/Support/RowerScripts.cs`, `RoutineSeeds.cs`, `ScriptedRower.cs` | C [A] | `R3`, `R4A`, `R4D` (con lecturas que no llegan) y `R4S` (QA-G5T-06); `ScriptedRower` con un índice de lectura inyectable; `F5`, `Budget5` y `Lt59` (§6.2) |
| `Tests/Support/LifecycleHarness.cs` | C [A] | `RealReadingIndex`: el número de la lectura con el tiempo real (lo que contó el monitor, también con el equipo dormido), para `R-4S` (§2.3, §6.2) |
| `Tests/Support/SeriesSeeds.cs` | N [A] | `S-LARGO` y `ExtremeSplit` como `HudSeries` armados a mano (§6.2) |
| `Tests/UiSupport/{UiTestHost,UiHostOptions,UiQuery,PixelProbe}.cs` | C [B1, B2] | `Preferences` por anfitrión (memoria o archivo temporal), `OverrideSeries(HudSeries)`, `TapText` y `TapAt` / `DragAt` por la selección real, y las **ranuras** de `IDrawnTextSource` en `IsTextVisible`, `TextOf`, `IsShown`, `Bounds` y las sondas, buscadas por nombre como un elemento (§3.1, §3.6, §6.2) |
| `Tests/UiSupport/FrameAllocations.cs` | N [B1] | las asignaciones de cuadros reales (`yield return null`), con el repintado adentro (QA-G5T-03; §3.6) |
| `Tests/EditMode/**`, `Tests/Simulator/**` | N/C [A] | §6.3 |
| `Tests/PlayMode/**`, `Tests/PlayModeSimulator/**`, `Tests/UnityEditMode/**` | N/C [B, D1] | §6.3 |
| `tools/oracle/bin/series_vectors.dart` | N [A] | el oráculo del tracker (§2.9) |
| `Tests/Golden/oracle/series_tracker.json` | G [A] | sus vectores |
| `tools/unity/capture-snapshots.ps1`, `tools/unity/README.md` | C [B3] | `-Set it5` (por defecto) |
| `docs/migration/architecture/adr/021-preferencias.md` | N [A] | ADR corto: dónde está el archivo, cómo se guarda, qué pasa si falla, el aislamiento de las pruebas |
| `docs/migration/iterations/05-series-parciales/snapshots/*` | G [B3, D1] | el arnés |
| `docs/migration/iterations/05-series-parciales/00-deuda-4b.md` | C (gate) | §0 y §1.1 sin datos del equipo ni de la base del usuario (QA-G5T-15; ya corregido con este gate) |

**Manifest:** `Packages/manifest.json` no cambia. Ningún paquete nuevo (Infrastructure ya tiene `Newtonsoft.Json.dll`).

### 1.2 Asmdefs

**Ninguno nuevo y ninguna referencia nueva.** `Infrastructure/Preferences/` es una carpeta de `RowMate.Infrastructure` (que ya referencia Newtonsoft y compila en dotnet). `PureCodeRulesTests` suma los archivos nuevos de `Core/Workout`, `Core/Design` y `App/Workout` (sin reloj del sistema ni `UnityEngine`) y la regla del saneamiento único (§6.3). `LegacyIsolationTests` suma: ningún archivo de producción nombra las preferencias del legacy (`shared_preferences`, `FlutterSharedPreferences`; Q-11).

---

## 2. Reglas puras (Core y App, WP-A)

Todo en `netstandard2.1`, C# 9, sin `UnityEngine`, con el tiempo por argumento. **Las expresiones de coma flotante del tracker se copian en el mismo orden que el legacy** (ADR 012): `(target - _distance) / dd * dt` se evalúa de izquierda a derecha igual que en Dart; así el oráculo compara con 1e-9 sin sorpresas.

### 2.1 El saneamiento (`SanitizedDistance`, C; Q-55, DV-26; AC-WKT-07-06)

La regla de la 4a no cambia (`series_tracker.dart:120-127`): `dt = t − último`; `Δ = crudo − último crudo`; si `Δ < 0` o `Δ > 10 · máx(dt; 1)`, no suma; el tiempo sí; esa lectura pasa a ser la referencia. Lo nuevo es que `Update` devuelve lo que el tracker necesita para interpolar y para los segundos de trabajo, sin recalcularlo:

```csharp
namespace RowMate.Core.Workout
{
    // Una lectura saneada (series_tracker.dart:120-127): lo que había antes, lo que sumó y el Δt. Sin asignar.
    public readonly struct DistanceSample
    {
        public double MetersBefore { get; }   // _distance antes de la lectura
        public double TimeBefore { get; }     // _lastTime antes de la lectura
        public double Delta { get; }          // dd: 0 si el salto no cuenta
        public double Dt { get; }             // t − _lastTime (cuenta aunque el salto no sume)
        public double Time { get; }           // t
        public double MetersAfter => MetersBefore + Delta;   // = Meters después (:126)
    }

    public sealed class SanitizedDistance
    {
        public const double MaxSpeedMetersPerSecond = 10.0;      // :43-46
        public double Meters { get; }                            // sin cambios
        public double LastTime { get; }                          // _lastTime (:52)
        public void Begin(int rawMeters, int activeSeconds);     // :88-91 (sin cambios)
        public void Rebase(int rawMeters);                       // :106-108 (sin cambios)
        public DistanceSample Update(int rawMeters, int activeSeconds);   // :120-127, :143 (antes devolvía void)
    }
}
```

`SanitizedDistanceTests` de la 4a siguen igual; se suma que `DistanceSample` dé los mismos números que el legacy en los casos de LT-55 a LT-58.

### 2.2 El tracker (`SeriesTracker`, N; F-WKT-07, F-WKT-08; LT-47 a LT-58)

```csharp
namespace RowMate.Core.Workout
{
    // Vuelta de 500 m completada dentro de un paso de trabajo (series_tracker.dart:4-13).
    public readonly struct Lap { public int Number { get; } public double Seconds { get; } }

    // Resumen de una repetición completa (:15-34). WorkSplitSeconds = WorkSeconds / WorkMeters × 500, o null sin metros de
    // trabajo (:31-33; LT-54).
    public readonly struct RepSummary
    {
        public int Rep { get; } public int TotalMeters { get; } public int WorkMeters { get; } public double WorkSeconds { get; }
        public double? WorkSplitSeconds { get; }
    }

    // La foto del tracker: inmutable; la rehace el tracker solo cuando cambia (Begin, Update). Laps y PreviousReps son arreglos
    // que el tracker reemplaza cuando cierra una vuelta, entra a un paso o cambia de repetición (copia al escribir): una foto
    // nueva por segundo cuesta un objeto, no una copia de las listas.
    public sealed class SeriesState
    {
        public long Revision { get; }                       // sube con cada Begin / Update
        public int? StepIndex { get; }                      // null antes de la primera lectura (:54); DV-48 la mira (§2.4)
        public StepPosition? Position { get; }              // :55
        public bool IsWorkStep { get; }                     // :56, :70
        public IReadOnlyList<Lap> Laps { get; }             // las del paso, en orden (:61, :69)
        public int CurrentLapMeters { get; }                // :73-75: ⌊(d − inicio del paso) − 500·vueltas⌋ en trabajo; 0 si no
        public double CurrentLapSeconds { get; }            // :78: t − fin de la última vuelta en trabajo; 0 si no
        public bool InSeries { get; }                       // :80: Position?.GroupId != null
        public int Rep { get; }                             // :81 (0 sin posición)
        public int RepCount { get; }                        // :82
        public int RepMeters { get; }                       // :83: ⌊d − inicio de la repetición⌋
        public IReadOnlyList<RepSummary> PreviousReps { get; }   // :67, :84: la más reciente primero, hasta 3
        public double StepMeters { get; }                   // d − inicio del paso: los mismos metros que el paso por distancia (AC-WKT-07-09)
        public static readonly SeriesState Empty;
    }

    // Lleva las vueltas de los pasos de trabajo y los totales de cada repetición de la serie en curso (:36-183). Dueño del
    // ÚNICO SanitizedDistance de la sesión: el motor lee Meters para el paso por distancia (DV-26; spec §12 punto 2).
    public sealed class SeriesTracker
    {
        public const int LapMeters = 500;          // :40
        public const int MaxPreviousReps = 3;      // :41

        public SeriesState State { get; }          // la foto vigente (la misma instancia hasta el próximo cambio)
        public double Meters { get; }              // los metros saneados desde Begin (_distance, :51)

        // :86-102: reinicia todo; rawMeters es la distancia del monitor al iniciar, activeSeconds el reloj activo (0).
        public void Begin(int rawMeters, int activeSeconds = 0);

        // :104-108: al reanudar, la distancia de ese momento es la referencia; el salto no cuenta (LT-58).
        public void Rebase(int rawMeters);

        // :110-145: una lectura del paso stepIndex. Si el paso cambió, primero entra (:147-182): repetición nueva si cambia la
        // serie o el número (:149-151); con la misma serie, el resumen de la que terminó va al frente (:154-166) y si quedan
        // más de 3 se va la más vieja; si no, la lista se vacía (:167-170); el paso nuevo empieza con la distancia y el
        // reloj de la lectura ANTERIOR (:178-181). Después sanea (SanitizedDistance.Update) y, en trabajo, suma los metros
        // que cuentan y TODO el Δt (:129-131) y cierra cada marca de 500 que se cruzó, interpolando (:132-140):
        // crossTime = dd > 0 ? t0 + (marca − d0) / dd · dt : t.
        public void Update(int stepIndex, IntervalStep step, StepPosition position, int rawMeters, int activeSeconds);
    }
}
```

| Regla | Legacy | LT | AC |
|---|---|---|---|
| Solo en trabajo hay vueltas; fuera, la vuelta en curso es 0 m y 0 s | `:73-78`, `:129`, `:178` | LT-50 | AC-WKT-07-01, -05 |
| Una vuelta = 500 m desde el inicio del paso; se numeran por paso; la lista se vacía al entrar a cada paso | `:40`, `:133-140`, `:179-181` | LT-47, -48, -49 | AC-WKT-07-02 |
| El cruce se interpola; una lectura puede cerrar varias | `:132-140` | LT-47, -48 | AC-WKT-07-04 |
| El salto no suma (Δ < 0 o > 10·máx(Δt; 1)); el tiempo sí | `:43-46`, `:120-127` | LT-55, -56, -57 | AC-WKT-07-06 |
| Metros de trabajo = los que suman; segundos de trabajo = todos los Δt | `:129-131` | LT-51, -55 | AC-WKT-08-06 |
| Repetición nueva si cambia la serie o el número; resumen al frente si es la misma serie, hasta 3; si no, vacía | `:147-174` | LT-51, -52, -53 | AC-WKT-08-03, -04, -05 |
| La última repetición nunca entra (al salir de la serie se vacía; al terminar la rutina, nadie entra) | `:153-174` | — | AC-WKT-08-07 |
| ⌊ ⌋ en los metros de la vuelta, de la repetición y del resumen | `:74`, `:83`, `:161-162` | LT-50, -51 | AC-WKT-08-02, -04 |

Notas de paridad:

- **⌊ ⌋** de Dart (`double.floor()` → `int`) es `(int)Math.Floor(x)` con el valor siempre ≥ 0 (los metros nunca bajan: LT-57). Con los extremos de FTMS (`R-4X`, 24 bits = 16 777 215 m) cabe en `int`.
- **El paso de 0 s** (spec §4.1, AC-WKT-07-12): con la regla de pasos de la app un paso por tiempo de 0 s termina en el mismo segundo en que entra; el tracker recibe esa lectura en ese paso (es la lectura del segundo en que termina, AC-WKT-07-03) y el paso siguiente recibe la suya en el segundo siguiente. El oráculo lo cubre.
- **El tracker no conoce la presentación**: no sabe de 4 vueltas visibles, de DV-48 ni de textos.

### 2.3 El motor (`WorkoutEngine`, C; AC-WKT-07-03, -07, -08, -09, -10, -11)

| Dónde | Hoy (4a/4b) | Con la 5 | Legacy |
|---|---|---|---|
| Campo | `SanitizedDistance _meter` | `SeriesTracker _series` (sale `_meter`) | `:58` |
| `StartWithRoutine` | `_meter.Begin(raw, 0)` | `_series.Begin(_fusion.Data.DistanceMeters, 0)` | `:128` |
| `Resume` | `_meter.Rebase(raw)` | `_series.Rebase(_fusion.Data.DistanceMeters)`: la misma referencia para los parciales y el paso por distancia (AC-WKT-01-05) | `:248` |
| `ProcessSecond`, con rutina | `_meter.Update(raw, total)`; objetivos; fin del paso | `_series.Update(_stepIndex, steps[_stepIndex], _positions[_stepIndex], raw, _totalElapsed)`; objetivos; fin del paso | `:176-186` |
| Paso por distancia | `_meter.Meters − _stepStartMeters ≥ meta` | `_series.Meters − _stepStartMeters ≥ meta` (DV-26); al avanzar, `_stepStartMeters = _series.Meters` | `:198-201` (con DV-26) |
| `ReturnToIdle` | `_meter.Begin(0, 0)` | `_series.Begin(0, 0)` | `:315` |
| Foto | — | `Series = _steps != null && _phase != Idle ? _series.State : null` | `:91` (el getter `series`) |
| Lectura | — | `public SeriesTracker Series => _series` (como `Targets`: las pruebas y el oráculo) | — |

- **Por qué los metros son los mismos** (AC-WKT-07-09): `_stepStartMeters` se fija en el segundo en que el paso termina, con `_series.Meters` de ese segundo; el tracker fija el inicio del paso siguiente al entrar, en el segundo siguiente, con su `_distance` de antes de esa lectura, que es el mismo valor (nada lo cambió en el medio). La prueba lo afirma segundo a segundo (`SeriesState.StepMeters` contra la cuenta del motor) con `R-4`, `R-4R`, `R-4Z`, `R-4D` y `R-4X`.
- **Pausa** (AC-WKT-07-07): el motor no procesa segundos en pausa; al reanudar, `Rebase` con la distancia de ese momento. Con `Trabajo 1000`, `Pausa` a los 20,7 s y `Reanudar` a los 50,7 s: `0:20.0` `80 m` en pausa, `0:21.0` `84 m` en el primer segundo y `#1` a los 125 s de reloj activo (verificado, `<scratch>/it5/arch/sim.py`). Lo que no suma son **124 m** (de `L(20)` = 1080 a `L(51)` = 1204: la fracción que pierde la pausa, Q-58, y el medio segundo de las lecturas; spec AC-WKT-07-07): SEN afirma 124, no 120.
- **Segundo plano y equipo suspendido** (AC-WKT-07-10): el motor ya procesa en orden los segundos vencidos (AC-WKT-02-04) y cada uno llama a `Update` con su lectura; el sueño no existe para el reloj despierto (ADR 020): al despertar, el salto de distancia de la primera lectura no suma (es la regla de siempre) y desde la siguiente suma. Sin código nuevo en la app. **Para ver el salto hace falta otro remo de prueba** (QA-G5T-06): el `R-4` de `ScriptedRower` manda con un *timer* del programador, que corre con el reloj despierto, así que con el equipo dormido no manda nada y al despertar sigue con `L(251)`, sin salto (SEN vería `0:51.0` `204 m`). `R-4S` (spec §0.4) es `R-4` con el número de la lectura tomado del **tiempo real** del arnés: `LifecycleHarness.RealReadingIndex` = ⌊crudo + hora de pared extra + 10⁻⁶⌋ (el instante −0,5 es el 0; con `Wake(…, monotonicCounts: false)` el sueño va a la hora de pared, así que vale en los dos casos del crudo), y `ScriptedRower` usa ese índice en lugar de `Next++` cuando se lo pasan. Con `Series 600` y una suspensión de 600 s a los 250,4: `L(250)` = 2000 m; al despertar, la primera lectura es `L(851)` = 4404 m (un salto de 2404 m en 1 s, que no suma): a los 251, `0:51.0` `200 m`; a los 252, `L(852)` = 4408, `0:52.0` `204 m`. Lo prueba `SeriesEngineTests` (SEN) con `LifecycleHarness` y los dos casos del crudo.
- **En memoria** (AC-WKT-07-11): nada se escribe en la base, en los puntos ni en el historial (la telemetría no cambia: `PresentationIndependenceTests` y `SessionRecorder` comparan los puntos de `Series 600` con y sin el tracker leyendo, y el esquema sigue en v8).
- **La foto**: `Series` es la instancia vigente del tracker (no se copia). Entre dos segundos es la misma, así el *store* del HUD no rehace nada (caché por `Revision`, §2.5).

### 2.4 Qué muestra cada panel (`SeriesPanels`, N; AC-WKT-18-01, -04 a -08; DV-48; P-01)

```csharp
namespace RowMate.Core.Workout
{
    public enum SeriesPanelKind { Laps, Reps }

    // Lo que muestran los paneles en este momento (sin textos). Laps: las 4 últimas, la más reciente primero (series_panels.dart
    // :279-282). LapCount: todas las del paso (el número de la más reciente es LapCount).
    public readonly struct SeriesPanelsContent
    {
        public bool Visible { get; }                 // con rutina (immersive_workout_screen.dart:105-106); en el libre, false
        public int StepIndex { get; }                // el paso que muestran (el del tracker, o 0 con DV-48)
        public bool IsWork { get; }
        public int CurrentLapMeters { get; }
        public double CurrentLapSeconds { get; }
        public int LapCount { get; }
        public Lap Lap0 { get; } public Lap Lap1 { get; } public Lap Lap2 { get; } public Lap Lap3 { get; }   // sin asignar
        public int VisibleLaps { get; }              // min(LapCount; 4)
        public bool InSeries { get; }                // el panel de repeticiones y la línea Rep (series_panels.dart:178, :313)
        public int Rep { get; } public int RepCount { get; } public int RepMeters { get; }
        public IReadOnlyList<RepSummary> PreviousReps { get; }   // hasta 3, la más reciente primero
    }

    public static class SeriesPanels
    {
        public const int VisibleLaps = 4;            // :281

        // steps/positions: la rutina expandida del motor (null en el libre). DV-48: con StepIndex == null (antes del primer
        // segundo) muestra el primer paso: IsWork = steps[0] es trabajo, la vuelta en curso en 0 m y 0 s, sin vueltas;
        // InSeries = positions[0].GroupId != null, Rep = positions[0].Rep, RepCount, RepMeters = 0, sin anteriores. Si no, lo del
        // tracker tal cual: en el segundo en que termina un paso todavía muestra ese paso (P-01; :118).
        public static SeriesPanelsContent For(SeriesState? state, IReadOnlyList<IntervalStep>? steps, IReadOnlyList<StepPosition>? positions);
    }
}
```

| Caso (`R-4`) | Lo que da | AC |
|---|---|---|
| `Series 600`, primer cuadro | `IsWork` false (calentamiento), sin serie: el chip `500 m` solo | AC-WKT-18-01 |
| `Cinco reps`, primer cuadro (empieza con la serie) | `IsWork` true, `0:00.0` `0 m`, `Rep 1/5 · 0 m`, el panel de repeticiones (DV-48; VID-06) | AC-WKT-18-01, AC-WKT-13-06 |
| `Series 600` a los 180 s (fin del trabajo) | la barra ya en el descanso; los paneles con el trabajo: `0:25.0` `100 m`, `#1`, `Rep 1/3 · 600 m` | AC-WKT-18-08 |
| `Series 600` a los 181 s | `IsWork` false, `Rep 1/3 · 604 m` | AC-WKT-18-05, -06 |
| `Series 600` a los 541 s | fuera de la serie: sin panel de repeticiones ni línea | AC-WKT-18-01 |
| `Fondo 3000` con `R-4A` a los 600 s | `#5`, `#4`, `#3`, `#2`; `#1` no | AC-WKT-18-04 |

### 2.5 El estado del HUD y lo que cambió (`HudSeries`, `HudSeriesChange`; AC-WKT-18-04 a -08, -22)

```csharp
namespace RowMate.App.Workout
{
    // Una fila de vuelta: Number para la animación (la vista sabe cuál es nueva); Label = hudLapNumber ("#3"); Time = LapTime.
    public sealed record HudLapRow(int Number, string Label, string Time, double TimeValue);

    // Una fila de repetición: Label = hudRepLabel ("Rep 2"); Split = RepSplit ("2:05" o "—"); Meters = PanelMeters ("1.112 m").
    public sealed record HudRepRow(int Rep, string Label, string Split, double SplitValue, string Meters, int MetersValue);

    // Lo que dibujan los dos paneles, con los textos hechos (una vez por segundo, o al cambiar el idioma). Los rótulos fijos
    // (hudLapsChip, hudLapsTitle, hudLapsEmpty, hudRepsChip, hudRepsTitle, hudRepsEmpty) los pone la vista desde el Localizer.
    public sealed record HudSeries(long Session, int StepIndex, bool IsWork, string CurrentTime, double CurrentTimeValue,
                                   string CurrentMeters, int CurrentMetersValue, IReadOnlyList<HudLapRow> Laps, int LapCount,
                                   bool InSeries, int Rep, string? RepProgress, int RepMeters, IReadOnlyList<HudRepRow> Reps);

    // WorkoutHudState (C): + init HudSeries? Series (null en el libre y en reposo).

    // Qué le pasa a la parte de arriba de un panel cuando cambia su contenido (04-design-notes §6.3; DD5-18): solo funde lo que
    // cambia de clase (la vuelta en curso ↔ el vacío, la lista de repeticiones ↔ su vacío).
    public enum SeriesBodyChange { None, Roll, ToEmpty, FromEmpty }

    // Qué cambió entre dos estados, POR PIEZA (lo que anima la vista; 04-design-notes §6.2 a §6.4). Pura. Lo deriva de los dos
    // HudSeries: StepIndex, IsWork, la cantidad de vueltas y el Number de la más reciente, InSeries, la cantidad de repeticiones y
    // el Rep de la primera fila (dentro de una serie los números de repetición no se repiten, y una serie nueva siempre pasa por
    // la lista vacía: series_tracker.dart:149-170).
    public readonly struct HudSeriesChange
    {
        public bool StepChanged { get; }             // parciales muestra otro paso (el segundo siguiente al cambio: P-01)
        public SeriesBodyChange LapsBody { get; }    // con StepChanged: trabajo → trabajo Roll (el rodillo, sin fundir); trabajo → fuera
                                                     // ToEmpty; fuera → trabajo FromEmpty; fuera → fuera None (el vacío no se mueve)
        public int NewLaps { get; }                  // vueltas cerradas en este segundo, en el mismo paso (1, o más: AC-WKT-07-04)
        public bool LapDropped { get; }              // había 4 a la vista y se va la más vieja
        public int ClearedLaps { get; }              // con StepChanged: las filas que había (0 a 4), que salen en su lugar (0-120)
        public bool NewRep { get; }                  // una repetición entró al frente de la lista (con su marca)
        public bool RepDropped { get; }              // había 3 y se va la más vieja
        public SeriesBodyChange RepsBody { get; }    // FromEmpty: la primera fila reemplaza al vacío; ToEmpty: la lista se vacía (una
                                                     // serie nueva sigue a otra); None. Nunca Roll
        public bool SeriesStarted { get; }           // InSeries false → true: aparece repeticiones y entra la línea Rep (120-280)
        public bool SeriesEnded { get; }             // InSeries true → false: se va repeticiones y sale la línea Rep (0-120)
        public bool IsNone { get; }
        public static HudSeriesChange Between(HudSeries? before, HudSeries after);   // before null o otra sesión: sin cambios (el primer estado)
    }
}
```

- **El *store*** (`WorkoutHudStore.Compose`) arma `HudSeries` con `ComposeSeries(snapshot.Series, engine.Steps, engine.Positions, localizer, session)`, que llama a `SeriesPanels.For` y arma los textos. **Caché**: la clave es (sesión, `Series.Revision` o 0 con DV-48, el `Localizer`); entre dos segundos el *store* devuelve la misma instancia, así una lectura del remo en k − 0,5 no rehace nada (AC-WKT-18-08: los paneles cambian solo en el segundo). Las filas que no cambian se reusan (la `HudLapRow` del `#3` es la misma instancia mientras está a la vista), así por segundo se arman tres cadenas (el tiempo y los metros de la vuelta en curso y la línea `Rep`) y un *record* (lo que ARCHITECTURE §20.2 permite a 1 Hz).
- **`ComposeSeries` es pública y pura** (`public static HudSeries ComposeSeries(SeriesState? state, IReadOnlyList<IntervalStep>? steps, IReadOnlyList<StepPosition>? positions, Localizer localizer, long session)`): las pruebas de LT-59 y LT-60 arman el tracker como el legacy y pasan por acá y por `SeriesPanels.For`, nunca por un `HudSeries` armado a mano (QA-G5P-05, §6.4).
- **Textos** (§2.6): `Label = hudLapNumber(number)`, `RepProgress = hudRepProgress(rep, count, PanelMeters(repMeters))` solo con `InSeries`, `hudRepLabel(rep)`; los formatos de la 4a (`WorkoutFormats.LapTime`, `RepSplit`, `PanelMeters`; LT-45), con el punto de miles también en inglés (Q-06, P-08).
- **`HudSeriesChange`** se prueba en dotnet (HSC) con los casos de los videos y de DD5-18, con `R-4`:

  | Rutina · segundo | Lo que da | Toma |
  |---|---|---|
  | `Series 600` · 325 | `NewLaps` 1 | VID-02 |
  | `Series 600` · 181 | `StepChanged`, `LapsBody` `ToEmpty`, `ClearedLaps` 1 | VID-03 |
  | `Series 600` · 201 | `StepChanged`, `LapsBody` `FromEmpty`, `NewRep`, `RepsBody` `FromEmpty` | VID-04 |
  | `Series 600` · 371 | `StepChanged`, `LapsBody` `FromEmpty`, `NewRep` (de 1 a 2 filas) | CAP-04 |
  | `Series 600` · 541 (descanso → enfriamiento) | `StepChanged`, `LapsBody` **`None`**, `SeriesEnded`; nada más (el vacío no se mueve: QA-G5P-03) | CAP-07 |
  | `Series 600` · 31 | `StepChanged`, `LapsBody` `FromEmpty`, `SeriesStarted` | DEMO-11 |
  | `Dos series` · 61 (trabajo → trabajo) | `StepChanged`, `LapsBody` `Roll` (la vuelta en curso no funde), `NewRep`, `RepsBody` `FromEmpty` | — |
  | `Dos series` · 121 (serie A → serie B) | `StepChanged`, `LapsBody` `Roll`, `RepsBody` `ToEmpty`; ni `SeriesStarted` ni `SeriesEnded` | — |
  | `Dos series` · 241 | `StepChanged`, `LapsBody` `ToEmpty`, `SeriesEnded` | — |
  | `Pirámide 500` · 726 (trabajo → trabajo) | `StepChanged`, `LapsBody` `Roll`, `ClearedLaps` 1 | — |
  | `Cinco reps` · 541 | `StepChanged`, `LapsBody` `FromEmpty`, `NewRep`, `RepDropped` | CAP-10 |
  | `Fondo 3000` con `R-4A` · la vuelta 5 | `NewLaps` 1, `LapDropped` | CAP-08 |
  | dos vueltas en un segundo (estado armado a mano: con el tope de 10 m/s solo pasa con Δt > 100 s) | `NewLaps` 2 | — |
  | el primer estado, u otra sesión | `IsNone` | — |

  `Instant` no importa acá (lo decide la vista: con `Instant` no anima nada, §4.2).

### 2.6 Catálogo: 223 claves (AC-APP-10-12; AC-WKT-18-22)

Las 9 de spec §11.2, con el español idéntico al legacy (Q-01; `series_panels.dart:168-169`, `:183-184`, `:287`, `:315`, `:332`, `:338`): `hudLapsChip`, `hudLapsTitle`, `hudLapsEmpty`, `hudLapNumber` (`number`), `hudRepProgress` (`rep`, `count`, `meters`), `hudRepsChip`, `hudRepsTitle`, `hudRepsEmpty`, `hudRepLabel` (`rep`). `L10nKeys` las suma; `CatalogParityTests` y `L10nKeysTests` pasan de 214 a 223, ninguna en un solo idioma. Se reusa `hudUnitMeters`. Los formatos no son claves.

### 2.7 Las preferencias (`IPreferences`, `JsonFilePreferences`, `SeriesPanelPrefs`; AC-WKT-18-10, -11; AC-HIS-20-04; ARCHITECTURE §9.4)

```csharp
namespace RowMate.Core.Ports
{
    // Preferencias de la app con las claves del legacy (ARCHITECTURE §9.4). Hilo principal. Distingue "no existe" de un valor
    // de otro tipo: los dos dan false en TryGet* (el llamador usa su valor por defecto). Set* cambia el valor en memoria en el
    // acto y pide el guardado (que puede terminar después y fallar sin aviso: AC-WKT-18-11).
    public interface IPreferences
    {
        bool IsLoaded { get; }                    // false mientras la lectura no terminó (JsonFilePreferences lee al construirse)
        event Action? Loaded;                     // una vez, si arrancó sin leer
        bool TryGetBool(string key, out bool value);
        bool TryGetString(string key, out string value);
        void SetBool(string key, bool value);
        void SetString(string key, string value);
        void Remove(string key);
    }
}

namespace RowMate.Infrastructure.Preferences
{
    // prefs.json (ARCHITECTURE §9.4; ADR 021). En memoria es un JObject (Newtonsoft JToken, sin deserialización genérica: RA-05).
    // - Lectura: una vez al construirse, en el hilo que lo construye (el arranque: un archivo de unos cientos de bytes), con
    //   JObject.Parse. Si el archivo falta, está vacío, no se puede leer o no es un objeto JSON: arranca vacío sin aviso (una
    //   línea Info en el registro, sin la ruta) y el próximo Set guarda un archivo válido (AC-WKT-18-11).
    // - Una clave de OTRO TIPO (immersive.lapsExpanded = "sí", 1, null…): solo esa clave da false en TryGet* (el llamador usa
    //   su valor por defecto); las demás se leen y se conservan, y esa también, tal cual, hasta que un Set la reemplace (spec
    //   AC-WKT-18-11; el contrato de IPreferences). TryGetBool solo acepta JTokenType.Boolean y TryGetString solo String.
    // - Escritura: cada Set/Remove cambia el JObject en el acto y, en el MISMO hilo principal, arma el texto del archivo
    //   (JObject.ToString(Formatting.None) → UTF-8, unos cientos de bytes, una vez por toque: lo que §20.2 permite fuera del
    //   cuadro a cuadro). El ejecutor serie (fuera del hilo principal) solo escribe bytes: prefs.json.tmp y después lo pone en
    //   lugar de prefs.json (File.Replace si existe; File.Move si no), así un corte deja el viejo o el nuevo, nunca uno a medias.
    // - Escrituras seguidas se juntan: hay a lo sumo una escritura en curso y una pendiente; un Set nuevo reemplaza los bytes
    //   pendientes (con un número de versión), así dos toques rápidos dan dos o una escritura, y la última siempre gana.
    // - Si guardar falla: una línea Warning (sin la ruta) y nada más; el valor en memoria queda (la misma ejecución sigue con lo
    //   que tocó el usuario; después de reiniciar, lo último guardado: AC-WKT-18-11, como el caché de shared_preferences).
    // - Conserva las claves que no conoce (hrm.*, scene.*, de It. 8, 11 y 12).
    public sealed class JsonFilePreferences : IPreferences, IDisposable
    {
        public JsonFilePreferences(string path, IAppLogger logger);                                   // producción
        internal JsonFilePreferences(string path, IAppLogger logger, IDbExecutor writer, IPreferencesFileOps ops);   // pruebas
        public Task FlushAsync();     // espera lo pendiente (las pruebas y el cierre de la app)
    }

    public static class PreferencesFiles { public const string Name = "prefs.json"; }   // uno para los dos builds (P-11)
}

namespace RowMate.App.Workout
{
    public readonly struct SeriesPanelsExpanded { public bool Laps { get; } public bool Reps { get; } }

    // Lo que la vista de los paneles lee y toca (HudView, SeriesColumn). Lo implementan SeriesPanelPrefs (las preferencias de
    // la app) y LocalSeriesPanelsExpansion (en memoria; el precalentado: QA-G5T-05).
    public interface ISeriesPanelsExpansion
    {
        IReadOnlyObservable<SeriesPanelsExpanded> State { get; }
        void Toggle(SeriesPanelKind kind);
    }

    // Los dos paneles (series_panels.dart:113-156): cerrados por defecto (LT-59); con la clave en verdadero, abiertos desde que
    // se ven (LT-60); un toque antes de que termine la lectura gana (:130-132, :144-149); cada toque guarda en el acto (:153-156).
    // El de repeticiones conserva su estado aunque no se vea. Uno por app (AppContext.SeriesPanels); lo comparten todos los
    // inmersivos.
    public sealed class SeriesPanelPrefs : ISeriesPanelsExpansion, IDisposable
    {
        public const string LapsKey = "immersive.lapsExpanded";   // :120
        public const string RepsKey = "immersive.repsExpanded";   // :121
        public SeriesPanelPrefs(IPreferences preferences);
        public IReadOnlyObservable<SeriesPanelsExpanded> State { get; }   // un ObservableValue (SubscriberCount, para las pruebas)
        public void Toggle(SeriesPanelKind kind);
    }

    // Un estado abierto o cerrado que vive solo en memoria y nunca toca IPreferences: Toggle cambia su State y nada más.
    public sealed class LocalSeriesPanelsExpansion : ISeriesPanelsExpansion
    {
        public LocalSeriesPanelsExpansion(bool laps, bool reps);
    }
}
```

- **Dónde** (DT5-05): `prefs.json` en la carpeta privada de la app, **la misma de la base** (`DataLocation.PreferencesFile`, que usa `DataLocation.DatabaseDirectory`): en Windows `LocalLow/iguisoft/RowMate` y en Android la interna (no `persistentDataPath`, que es la externa, ARCHITECTURE §9.1). Así lo comparten el build normal y el de simulador (las bases tienen nombres distintos, el archivo de preferencias no: AC-WKT-18-11, P-11), y en el Editor y en desarrollo `ROWMATE_DATA_DIR` lo reemplaza, como a la base. ARCHITECTURE §9.4 se precisa al cerrar ("`persistentDataPath/prefs.json`" pasa a "la carpeta de la base").
- **Aislamiento** (AC-HIS-20-04): `AppComposer` sin `Preferences` usa `MemoryPreferences`; `UiTestHost` usa memoria o, en las pruebas del almacén real, un archivo en una carpeta temporal; el arnés, una `MemoryPreferences` por toma (sembrada con lo que la toma pide); `BootSceneTests`, `run-unity-tests.ps1` y `capture-snapshots.ps1` ya ponen `ROWMATE_DATA_DIR`. `PreferencesIsolationTests` (Unity EditMode) fija que con la variable puesta el archivo cae adentro de ella. **Ninguna prueba calcula la ruta real y escribe en ella**: `PreferencesFileTests` (PFU) solo compara la **cadena** que da `DataLocation.PreferencesFile` (que termine en la carpeta de la base y en `prefs.json`) y escribe solo en una carpeta temporal propia (QA-G5T-05). QA compara el archivo del usuario (o que no exista) antes y después de todo.
- **El precalentado** (`HudWarmup`, QA-V4b-06, DD5-12) arma los paneles abiertos con un `LocalSeriesPanelsExpansion(true, true)` propio, nunca con `AppContext.SeriesPanels`: no pasa por `Toggle` de las preferencias, no escribe `prefs.json` y no se suscribe al `State` de la app (QA-G5T-05). `4BD.HudWarmupIsNotSeenAndLeavesNothing` lo afirma con `RecordingPreferences` (0 escrituras) y con `SubscriberCount` del `State` de la app igual antes y después; `BST` (el arranque real, con `ROWMATE_DATA_DIR` temporal) afirma que sin toques `prefs.json` no existe al terminar y que, sembrado, queda con los mismos bytes y la misma fecha.
- **Q-11**: nunca se lee el `shared_preferences.json` del legacy (`LegacyIsolationTests`).

### 2.8 Composición

```csharp
public sealed record AppServices(/* los 9 posicionales */)
{
    public IPreferences? Preferences { get; init; }   // It. 5: null = MemoryPreferences
}

public sealed class AppContext
{
    public SeriesPanelPrefs SeriesPanels { get; }      // uno por app; AppContext.Dispose lo dispone
}
```

`AppRoot.Awake` (paso 4c, después de la base): `new JsonFilePreferences(DataLocation.PreferencesFile(_platform), logger)` y lo pasa en `AppServices`; `OnDestroy` espera `FlushAsync()` hasta 1 s (como la base). Ninguna ruta va al registro.

### 2.9 El oráculo (AC-WKT-07-12)

`tools/oracle/bin/series_vectors.dart` (N, Dart puro: `series_tracker.dart` solo importa los modelos, así corre como `routine_vectors.dart`) escribe `Tests/Golden/oracle/series_tracker.json` con `lib/golden.dart`. Se corre con `powershell -NoProfile -File tools/oracle/run-oracle.ps1 -Only series_vectors` (la guardia del legacy antes y después).

| Sección | Qué tiene |
|---|---|
| `legacyCases` | los 12 casos de `test/series_tracker_test.dart`, con los alimentadores de `:20-42` y `:133-152` reescritos en el script (son privados del test) |
| `scripts` | `Series 600`, `Cinco reps`, `Dos series`, `Pirámide 500` y `Corta` × `R-4`, `R-3`, `R-4A`, `R-4R`, `R-4Z`, `R-4D`, `R-4P` y `R-4X`; `Trabajo 1000` y `Fondo 3000` con los remos de AC-WKT-07-06; pausas de 1 s y largas, una que cruza un cambio de paso; un paso de 0 s |
| Cada guion | `ops`: la secuencia de llamadas (`begin(raw, t)`, `update(step, type, group, rep, repCount, raw, t)`, `rebase(raw)`) y `states`: el estado después de cada `update` (vueltas con número y segundos, si es trabajo, la vuelta en curso, si está en serie, la repetición, la cantidad y sus metros, y las anteriores con repetición, total, trabajo, segundos y split) |

- **Cómo se arman las entradas** (spec: "el tracker se alimenta con la secuencia de pasos de la app"; DT5-07). El script calcula las `ops` con el modelo de la spec (§0.4): el segundo n del reloj activo cae en el instante n (con pausas: la pausa a los p corta el tramo y el segundo siguiente cae en `reanudar + 1`, Q-58); la lectura vigente en el instante w es la última `L(k)` con `k − 0,5 ≤ w` (las que faltan, como en `R-4D`, no cuentan); el fin de cada paso es la regla de la app (por tiempo, `elapsed ≥ duración`; por distancia, los metros saneados del paso con la fórmula del legacy, DV-26). Es la regla de la app escrita en Dart, **y está verificada desde el lado de C#**: `SeriesGoldenTests.TheEngineFeedsTheOracleInputs` corre el motor de la app con el mismo remo, la misma rutina y las mismas pausas (`FakeScheduler`, `ScriptedRower`) y exige que cada llamada del motor al tracker sea la `op` del guion. Si el modelo de Dart se equivoca, falla esa prueba, no la de paridad.
- **Qué compara dotnet** (`SeriesGoldenTests`): (1) `TheTrackerMatchesTheLegacy`: el `SeriesTracker` de la app con las `ops` de cada guion da los `states` (enteros iguales; segundos con 1e-9); (2) `TheEngineFeedsTheOracleInputs`, arriba; (3) `TheEngineSeriesMatchesTheLegacy`: la foto `Series` del motor después de cada segundo es el `state` del guion.
- **Si Dart no está**, los vectores versionados alcanzan para (1) a (3); solo regenerarlos necesita `<flutter-sdk>` (el Dart que trae). Una prueba de herramientas fija que el archivo tenga el `legacyCommit` y el `generator`.

### 2.10 Tokens y contraste (DD5-08, DD5-09)

- `Tokens.cs`: `num.lap` 28/28/700, `num.row` 20/22/700 (ningún código los usa todavía: no mueve nada de la 4a ni de la 4b) y `num.xs` 13/16/700 (nuevo); `tools/tokens` regenera `Tokens.uss` y `Typography.uss`; `TokenCatalogTests` en 137 y `--check` en 0.
- `ContrastTests.ItFiveDesignerPairs`: C1 a C9 de 00-deuda-diseno §3.7 con sus valores de control a 3 decimales.

---

## 3. UI (WP-B)

### 3.1 El panel: un elemento que dibuja todo (`SeriesPanelView`; DD5-04; AC-WKT-18-02 a -07, -17)

```
#hud-laps-panel .rm-hud-series-panel .rm-hud-series-panel--laps     un VisualElement; absoluto; escala sp desde arriba a la izquierda;
│                                                                   layout = su rectángulo FINAL (137 × alto final, base); pickingMode
│                                                                   Position con ContainsPoint = el área de toque de este momento
│                                                                   (DT5-15); focusable por código; sin fondo ni borde en USS;
│                                                                   style.translate y style.opacity: solo SeriesColumn (DT5-14)
├── (holder oculto) .rm-type-label     Inter 600: el rótulo del chip, el título y los rótulos #n / Rep n
├── (holder oculto) .rm-type-body      Inter 400: los vacíos
├── (holder oculto) .rm-type-num-*     RowMate Numeric SC 700: las cifras
├── (holder oculto) .rm-icon           la fuente de íconos de contorno: timer / history
└── (oculto) .rm-hud-series-panel__hit absoluto, visibility hidden, pickingMode Ignore: agranda la caja del panel hasta el área
                                       de toque y lo que dibuja en una animación (DT5-15)
#hud-reps-panel .rm-hud-series-panel .rm-hud-series-panel--reps     ídem
```

Los *holders* son hijos de tamaño 0 con `visibility: hidden` (el patrón de los afijos de `TabularNumber`): resuelven el `FontAsset` de cada clase y miden con `MeasureTextSize`, y **no cuentan** para el presupuesto (`UiQuery.IsShown` mira `visibility`); el hijo del área de toque tampoco. Todo lo que se ve lo dibuja el `generateVisualContent` del panel, en este orden (de abajo arriba; 04-design-notes §4.9), con la anatomía de 04-design-notes §4 como constantes de `SeriesPanelGeometry` (DD5-23: la caja del ícono x 10-24, el rótulo desde x 28, la flecha, los metros pegados a x 127, la columna del rótulo, la marca en x 4-7 y la vertical de §4.6):

| Capa | Cómo | De dónde sale |
|---|---|---|
| El vidrio (relleno `glass.fallback`, borde 1·sp blanco @10 % por dentro, radio 14·sp), al **alto visual** `G`. **Sin reflejo**: el de las tarjetas de hoy (DD5-21) | `Painter2D`; en `ColumnScroll`, el relleno en franjas con color por vértice (`Allocate`) para la máscara (regla 3, DD5-15) | `SeriesPanelVisual.HeightBase` (la motion, §4.2); las sondas S5-01 a S5-04 (04-design-notes §9.6) |
| La capa de *hover* y presionado (blanco 4 % / 8 %, solo en la banda de la cabecera, y 0-34, con los radios de arriba) | `Painter2D` | `SeriesPanelVisual.HoverAlpha` (90 / 160 ms, §4.2); nunca en un panel inerte |
| La cabecera: ícono 14 @80 % (x 10-24), el rótulo corto 13/600 con 0,08 em (desde x 28) y el título 11/600 a la escala de título (desde x 28) cruzándose, y la flecha | `DrawText` (ícono y textos) + `Painter2D` (la flecha: el trazo de 1,5 base de 04-design-notes §4.3, que gira alrededor de (122,5; 17); con reducir movimiento, ∨ y ∧ con su fundido) | `HudSeries`, el `Localizer` y la motion |
| La vuelta en curso: el tiempo `num.lap` 28 desde x 10 y los metros `num.xs` 13 @80 % **pegados a x 127**, por la misma línea de base (SD-16) | `TabularCells` (celdas tabulares con su rodillo de 180) | `HudSeries.CurrentTime`, `.CurrentMeters` |
| Las filas: `#n` / `Rep n` 12/600 `text.secondary` en su columna (la de `#99` o la del más ancho a la vista), a 4 el valor `num.row` 20; en repeticiones, los metros `num.xs` 13 pegados a x 127 | `DrawText` + `TabularCells` | `HudSeries.Laps` / `.Reps` y la motion (el empuje, la que entra, la que sale, las que se vacían) |
| La línea `Rep r/R · m` (`num.stat` 16) | `TabularCells` | `HudSeries.RepProgress`; baja con la cola (`TailShiftBase`) |
| Los vacíos en **dos renglones parejos** (`body` 15/22 `text.tertiary`; `Sin parciales` / `en este paso`) | `DrawText` por renglón, con el corte de `SeriesPanelGeometry.BalancedBreak` (DD5-19) | el `Localizer` |
| La marca de la fila nueva (3 × 20 base, radio 1,5, x 4-7) | `Painter2D` | la motion (DT5-03; 04-design-notes §4.4) |
| El anillo de foco **en dp de ventana**: blanco de −4 a −2 dp del borde visual y halo `overlay.scrim` @50 % de −8 a −4, radios 14·sp + 4 y 14·sp + 8 (el trazo se divide por `sp`, porque el elemento está escalado) | `Painter2D` | el foco con el teclado (`FocusHighlight`, DT5-11), y sigue `G` en cada cuadro (DD5-22) |

**Sin recortes: el alfa de cada renglón sale de su tinta** (DT5-02, revisado por 04-design-notes §5.3; DD5-13 a DD5-15). UI Toolkit solo recorta con `overflow: hidden` sobre la caja del elemento (o con una máscara, que ARCHITECTURE §20.2 prohíbe anidar), y lo que se descubre mientras el vidrio crece o lo que se desplaza no coincide con una caja quieta. Cada renglón (la cabecera, la vuelta en curso, cada fila, la línea `Rep`, cada renglón de un vacío) se dibuja entero, con un alfa = el de su motion × estas reglas (en base del panel; la tinta de cada clase de renglón, de 04-design-notes §4.6, achicada alrededor del centro de la banda con su escala de ajuste):

1. **Revelar** (siempre, contra el vidrio): `α = clamp((G − tintaAbajo) / SeriesRevealFade)`, con `SeriesRevealFade` = 4. Es 0 hasta que el vidrio cubre toda la tinta y llega a 1 cuando la pasa por 4: nunca hay tinta fuera del vidrio y en reposo todo renglón está al 100 % (el último termina su tinta entre 7,2 y 11 antes del borde). La fórmula anterior (`α_borde` de 12·sp contra el borde de abajo de la caja) dejaba el último renglón al 67 % en reposo y tinta fuera del vidrio mientras crecía: queda reemplazada.
2. **La ventana de filas** (siempre, también en `FitAll`, contra la ventana de filas de su panel): `α = clamp(1 − o / SeriesRowsEdgeFade)`, con `o` lo que la tinta pasa el borde (abajo: `tintaAbajo − Vb`; arriba, solo con desplazamiento > 0: `Va − tintaArriba`) y `SeriesRowsEdgeFade` = 4. El borde de abajo de la ventana es el final de la última fila en el estado final, corrido por la cola (`TailShiftBase`). Así la que se va nunca pasa sobre la línea `Rep`, y en W1280 la fila de arriba se ve entera aunque la ventana mida 20.
3. **La máscara de la columna** (solo en `ColumnScroll`, TH y L640, contra el borde de la columna que tiene algo escondido detrás): el vidrio por vértice `× clamp((Cb − y) / 12)` y cada renglón `× clamp((Cb − tintaAbajo) / 12)` (arriba, lo mismo con `Ca`), mezclados con 1 por la fuerza `f = clamp(escondido / 12)`; `SeriesScrollFade` = 12. Si nada pasa el borde, no hay máscara (CAP-15).

Los extremos de tinta se calculan **cuando cambia el texto** (como mucho una vez por segundo, o con el idioma y la ventana), nunca por cuadro; por cuadro solo se multiplican alfas. **El *layout* no cambia por cuadro** (ANIMATION_BIBLE §0.1 principio 5): el rectángulo del panel va a su estado final en el cuadro del cambio (un toque, un dato, una rotación) y la animación es dibujo; lo que mueve el panel entero es `style.translate` y `style.opacity`, que escribe solo `SeriesColumn` (DT5-14, §3.3).

**Los textos y sus medidas** (`DrawnText`, `TabularCells`):

- Se miden cuando cambian (una vez por segundo como mucho, o con el idioma y la ventana), **nunca por cuadro ni al abrir** (el primer toque no mide nada: QA-V4b-06, 00-deuda-diseno §5); sin panel no se mide y se aplica al volver (QA-G4bT-08 (b) de la 4b).
- Las corridas sin *tracking* (el título, los rótulos `#n` / `Rep n`, los vacíos) se miden con `MeasureTextSize` de su *holder* y se dibujan enteras con `DrawText`: el *kerning* lo pone el generador de texto en los dos casos. El rótulo del chip (con 0,08 em, que `DrawText` no acepta) se dibuja carácter por carácter con los avances del `FontAsset` más el *tracking* (como los afijos de la 4b; la unidad de TextCore es 1/100 em: `letter-spacing` 8, QA-V4b-02).
- **La escala de título es una por idioma** (DD5-20): la menor entre las que necesitan los dos títulos para entrar en 85,7 base, aunque el de repeticiones no se vea (es 0,949 por `Series anteriores`; en 1). Se calcula con el idioma y la ventana, así no cambia cuando aparece o se va un panel. En un idioma en que no entre a 0,8, recién ahí `…` al final (AC-WKT-18-03).
- **Lo que no entra se achica** (DV-49, AC-WKT-18-17; 04-design-notes §4.7; DD5-24), con `SeriesPanelGeometry.FitScale`:
  - la vuelta en curso: solo la cifra (`FitBox`, anclada en x 10 y la línea de base), con los metros a 13 pegados a x 127 y al menos 6 de aire;
  - las filas de vueltas: **una escala común** a las que están a la vista (rótulo, columna, aire y tiempo);
  - las filas de repeticiones: **una escala común** a las que están a la vista para las tres piezas, con los metros pegados a x 127;
  - la línea `Rep`: una escala pareja;
  - una escala común cambia cuando cambian las filas a la vista, en el cuadro del segundo, y llega a su valor nuevo con el empuje (0-160, `ease.move`): la vista interpola con `SeriesPanelVisual.PushProgress`;
  - **el piso**: ninguna escala de números baja de `SeriesNumberMinScale` = 0,6 (`S-LARGO` llega a 0,638, así que nunca se recorta). Lo que a 0,6 no entra (un valor más largo que `S-LARGO`, como un split de días con un metro de trabajo) se recorta con `…` al final de **esa pieza** (en una fila de repeticiones, el split; el rótulo y los metros quedan enteros), y nada sale del panel (AC-WKT-18-17; DN5-06);
  - con `S-LARGO`: 0,652, 0,932, 0,712 y 0,638; con `Rep 1 2:05 1.112 m`, 0,969 (las del Designer con las tablas K2; WP-B1 las vuelve a medir en Unity, R5-02).
- **Los vacíos** van siempre en dos renglones (DD5-19): de los cortes entre palabras que dejan las dos líneas en 117 o menos, el de la línea más larga más corta (en un empate, el primero); si ninguno entra, la regla del título. El corte se calcula con las medidas, con el idioma y la ventana.
- **Cifras tabulares** en todos los números, también los metros (ART_BIBLE §4.2). `TabularCells` es la lógica de celdas y rodillo de `TabularNumber` para dibujar adentro de otro elemento: `TabularNumber` no se toca en esta iteración (está verificado en la 4a y la 4b); unificarlos queda como deuda (DT5-13).

**La API para las pruebas** (DT4b-01, spec §12 punto 8): los paneles son `#hud-laps-panel` y `#hud-reps-panel`; lo de adentro son **ranuras** de la vista, con texto, rectángulo (dp de ventana), alfa y escala: `hud-laps-chip`, `hud-laps-title`, `hud-laps-current`, `hud-laps-current-meters`, `hud-laps-row-0` a `-3` (0 = la más reciente), `hud-laps-empty`, `hud-rep-progress`, `hud-reps-chip`, `hud-reps-title`, `hud-reps-row-0` a `-2`, `hud-reps-empty`. `SeriesPanelView` implementa `IDrawnTextSource` y `UiQuery.IsTextVisible` lo recorre, así LT-59 dice `IsTextVisible("Parciales 500 m")` como el `find.text` del legacy, y `UiTestHost.TapText("500 m")` toca el centro de esa ranura **por la selección real** (`TapAt`, `panel.Pick`). Para que sea el `find.text` del legacy (QA-G5P-05):

- **coincidencia exacta** con el texto de la ranura (`500 m` no encuentra `Parciales 500 m`);
- una ranura con **alfa efectivo ≤ 0,01** (el producto de su motion y de las tres reglas) **no se ve**: el título con el panel cerrado, el rótulo del chip con el panel abierto, el contenido con el panel cerrado, lo que está bajo la máscara;
- la ranura de un vacío tiene **el texto entero** del catálogo (`Sin repeticiones completas`) y sus dos renglones aparte (`Lines`), así `IsTextVisible("Sin repeticiones completas")` lo encuentra aunque se dibuje en dos.

### 3.2 La geometría (`SeriesPanelGeometry`, N; DD5-03; AC-WKT-18-13, -14)

```csharp
namespace RowMate.Core.Design
{
    public enum SeriesFitRule { FitAll, RowsScroll, ColumnScroll }   // reglas 1 (y 3), 2 y 4 de 00-deuda-diseno §2.4

    // La columna para una ventana (por ventana, con el peor caso: SPM con objetivo, en serie, los dos abiertos; así no cambia
    // con los datos ni con el paso). dp de ventana salvo lo que dice base.
    public readonly struct SeriesColumnLayout
    {
        public double Scale { get; }            // sp: s, o min(s; lugar / 182) ≥ 0,9 redondeado hacia abajo a 0,01 (W1280: 1,20)
        public double Left { get; }             // 14·s (el de SPM)
        public double WidthBase { get; }        // min(137; ventanaBase / 2 − 58)
        public double TopWithTarget { get; }    // borde de abajo de SPM con objetivo + 8·s
        public double TopWithoutTarget { get; } // ídem sin objetivo (Series 600, Trabajo 1000, Fondo 3000, Cinco reps, Dos series)
        public double Floor { get; }            // 8·s arriba de la banda (vertical: alto − 220·s; horizontal: alto − 96·s)
        public double RoomWithTargetBase { get; }     // el lugar entre el techo y el piso, en base del panel (TH 77,1)
        public double RoomWithoutTargetBase { get; }  // (TH 93,9: R-13)
        public SeriesFitRule Rule { get; }      // por ventana, con el peor caso (con objetivo, en serie, los dos abiertos)
    }

    // El lugar de cada panel en un momento: el alto final, la ventana de sus filas y el desplazamiento.
    public readonly struct SeriesPanelPlacement
    {
        public double TopDp { get; }            // su borde de arriba final (sin el desplazamiento de la columna)
        public double HeightBase { get; }       // su alto final
        public double RowsViewportBase { get; } // regla 2: lo que se ve de sus filas; si no, sus filas enteras
        public double RowsContentBase { get; }
        public DpRect HitRect { get; }          // cerrado: el chip agrandado hacia arriba y hacia abajo hasta 56 dp de alto (48 como
                                                // mínimo), cada uno con la mitad del aire entre los dos (04-design-notes §5.5);
                                                // abierto: el panel entero, en su alto visual
    }

    public static class SeriesPanelGeometry
    {
        // 00-deuda-diseno §4.7 y 04-design-notes §4 y §6.9 (base): el ancho, el relleno, los aires de la columna, los altos, los
        // aires de adentro, la cabecera, la marca, el desplazamiento y el toque, la escala mínima y las líneas de los vacíos.
        public const double PanelWidth = 137, PadH = 10, PadV = 8, ColumnGap = 8, ChipHeight = 34, HeaderHeight = 18;
        public const double HeroHeight = 28, RowHeight = 20, BlockGap = 6, RowsGap = 4, RowGap = 2, LabelGap = 4;
        public const double MetersMinGap = 6, HeroMetersGap = 6, Icon = 14, IconGap = 4, Chevron = 18;
        public const double MarkerWidth = 3, MarkerInset = 4, DragSlopDp = 8, HitMinDp = 48, HitRoomyDp = 56;   // 56 es tope (R-14)
        public const double MinViewHeight = 182, MinScale = 0.9, EmptyLines = 2, EmptyLineHeight = 22, RepLineHeight = 16;
        // La anatomía (DD5-23; 04-design-notes §4.2 a §4.6): el contenido de x 10 a 127; el ícono en x 10-24; el rótulo y el título
        // desde x 28 (lugar hasta 109,7 y 113,7); la flecha, trazo de 1,5 por (118,75; 15,125) → (122,5; 18,875) → (126,25; 15,125),
        // centro (122,5; 17); la marca en x 4-7; el aire de 4 entre la columna del rótulo y el valor; la línea de base y la tinta
        // de cada clase de renglón (la tabla de §4.6).
        public const double ContentLeft = 10, ContentRight = 127, IconLeft = 10, LabelLeft = 28, LabelRoom = 81.7, TitleRoom = 85.7;
        public const double ChevronCenterX = 122.5, ChevronCenterY = 17, SeriesChevronStroke = 1.5, MarkerLeft = 4, EmptyLineRoom = 117;
        // Las reglas de alfa por la tinta (§3.1; DD5-13 a DD5-15) y el piso de las escalas de números (QA-G5T-16).
        public const double SeriesRevealFade = 4, SeriesRowsEdgeFade = 4, SeriesScrollFade = 12, SeriesNumberMinScale = 0.6;
        public const double TitleMinScale = 0.8;

        public static SeriesColumnLayout Column(HudLayout layout);

        // El alto natural (base) de un panel con su contenido (00-deuda-diseno §2.4): cerrado 34; parciales abierto:
        // 8 + 18 + 6 + (trabajo ? 28 + (n > 0 ? 4 + 20·n + 2·(n − 1) : 0) : 2·22) + (en serie ? 6 + 16 : 0) + 8, con n ≤ 4
        // (68 sin vueltas ni serie; 180 con 4 vueltas y la línea; 84 y 106 fuera de un trabajo); repeticiones abierto:
        // 8 + 18 + 6 + (m > 0 ? 20·m + 2·(m − 1) : 2·22) + 8 (60, 82, 104; 84 vacío).
        public static double NaturalHeight(SeriesPanelKind kind, bool expanded, in SeriesPanelsContent content);

        // El reparto del lugar con la regla de la ventana: FitAll, todo; ColumnScroll, cada uno a su natural y la columna entera se
        // desplaza; RowsScroll (DD5-26; 04-design-notes §5.2), con las filas de cada uno desplazables y la cabecera, la vuelta en
        // curso y la línea Rep fijas: repeticiones tiene reservado su mínimo (34 cerrado; abierto, 60 —la cabecera y una fila o el
        // primer renglón de su vacío— o su natural si es menor); parciales recibe min(su natural; lugar − 8 − ese mínimo), y
        // repeticiones lo que queda, hasta su natural. Sin repeticiones a la vista, parciales recibe min(su natural; lugar).
        public static void Place(in SeriesColumnLayout column, bool spmHasTarget, double lapsNatural, double repsNatural, bool repsShown,
                                 bool lapsExpanded, bool repsExpanded, in SeriesPanelsContent content,
                                 out SeriesPanelPlacement laps, out SeriesPanelPlacement reps, out double columnContentBase);

        // El corte parejo de un vacío en dos renglones (DD5-19): el índice del espacio donde se corta, con los anchos de cada
        // palabra y del espacio (medidos al cambiar el texto); -1 si ninguno entra en EmptyLineRoom (la regla del título).
        public static int BalancedBreak(ReadOnlySpan<double> wordWidths, double spaceWidth, double room = EmptyLineRoom);

        // La escala de título de un idioma (DD5-20): la menor entre las de los dos títulos, con TitleMinScale.
        public static double TitleScale(double lapsTitleWidth, double repsTitleWidth, double room = TitleRoom);

        // La zona del bote (AC-WKT-18-14; 00-deuda-diseno §2.2, P-03): cerrados, nunca; abiertos en vertical, solo en
        // x ≤ ancho / 2 − 44·s; en horizontal, nunca.
        public static bool RespectsBoatZone(DpRect panel, bool expanded, in HudLayout layout);

        // La cuenta grande y PAUSA (AC-WKT-18-15): true si el rectángulo FINAL de un panel abierto (agrandado 8·s) se cruza con el
        // disco. Con el ancho de 137 no pasa en ningún perfil; con los 200 de la spec pasaría en TV y P360 (P-04).
        public static bool BlocksCallout(DpRect lapsFinal, DpRect? repsFinal, in HudLayout layout);

        // La escala pareja para que un ancho entre (DV-49): 1 si entra; si no, available / width, con su mínimo (el título
        // TitleMinScale 0,8; los números SeriesNumberMinScale 0,6). Lo que en el mínimo no entra, la vista lo recorta con … al final.
        public static double FitScale(double available, double width, double minimum = 0);
    }
}
```

04-design-notes §5.1 (dp de ventana; `Series 600` y las demás rutinas de la 5 en horizontal **no tienen objetivos en SPM**, R-12, R-13):

| Perfil | `s` · `sp` | El panel (x) · ancho | Techo con objetivo · sin | Piso | Lugar (base) con · sin | Regla | Los dos chips |
|---|---|---|---|---|---|---|---|
| TV | 1,054 · 1,054 | 14,8-159,1 · 144,4 | 228,6 · **210,9** | 682,2 | 430,3 · 447,2 | `FitAll` (lo más alto: 292) | sí |
| P360 | 0,923 · 0,923 | 12,9-139,4 · 126,5 | 200,3 · 184,7 | 596,9 | 429,7 · 446,6 | `FitAll` | sí |
| TH | 1,054 · 1,054 | 14,8-159,1 · 144,4 | 228,6 · 210,9 | 309,8 | 77,1 · **93,9** | `ColumnScroll` | sí (76 base; con objetivo sobra 1,1) |
| L640 | 0,923 · 0,923 | 12,9-139,4 · 126,5 | 200,3 · 184,7 | 271,4 | 77,1 · **93,9** | `ColumnScroll` | sí, ídem |
| W1280 | 1,6 · **1,20** | 22,4-186,8 · 164,4 | 347,1 · 320,2 | 566,4 | 182,7 · 205,2 | `RowsScroll` | sí |

`SeriesPanelGeometryTests` (SPG) afirma esta tabla (± 0,1), los altos naturales (§3.2: 34; parciales 68 / 90 sin y con la serie, 92-158 / 114-180 con 1 a 4 vueltas, 84 / 106 fuera de un trabajo; repeticiones 60, 82, 104 y 84 vacío; la primera vuelta suma 24 y cada una más 22: R-01), el reparto de `RowsScroll` con los cuatro casos de 04-design-notes §5.2, con y sin objetivo (DD5-26: 4 vueltas + 3 repeticiones, 114,7 · 60 y 137,2 · 60; 1 vuelta + vacío, 114 · 60,7 y 114 · 83,2; 4 vueltas + cerrado, 140,7 · 34 y 163,2 · 34; CAP-18, todo), y las áreas de toque de abajo.

- **El desplazamiento** (AC-WKT-18-13; 04-design-notes §5.2): empieza arriba, sin barra; un toque que se mueve menos de 8 dp abre o cierra, uno que se mueve más desplaza; el arrastre sigue al dedo 1:1 y al soltarlo queda donde está, **sin inercia**, dentro de sus topes; la rueda de a una fila (22·sp) con `spring.snappy` (en el acto con reducir movimiento); al rotar, a 0. Con `RowsScroll` el arrastre o la rueda sobre las filas de un panel mueve sus filas (dibujo, adentro del panel); con `ColumnScroll`, sobre cualquiera de los dos, la columna (`style.translate`, DT5-14). **Cuando cambia el contenido** el desplazamiento conserva su valor dentro de sus topes; si entra una fila con la vista desplazada (desplazamiento > 0), la fila entra arriba, fuera de la vista, y el desplazamiento suma 22·sp·k en el mismo cuadro, así lo que se ve no se mueve (DD5-27); con el desplazamiento en 0, la lista baja con el empuje y lo de abajo sale por la ventana.
- **El área de toque** (AC-WKT-18-09; ART_BIBLE §10.4; 04-design-notes §5.5): cerrado, el chip agrandado hacia arriba y hacia abajo hasta **56 dp de alto (tope; 48 como mínimo)**, cada uno con la mitad del aire entre los dos, así **nunca se superponen**, y si hace falta el aire con SPM y el relleno de abajo de SPM o de arriba de la banda (que no responden); abierto, el panel entero en su alto visual; en `ColumnScroll`, recortada a la ventana de la columna (lo que está bajo SPM o sobre la banda no responde). `HudOverlap` y SPG lo comprueban:

  | Perfil | Chip | `500 m` llega a | `Reps` llega a |
  |---|---|---|---|
  | TV · TH · W1280 | 35,8 · 35,8 · 40,8 | 56 | 56 |
  | P360 | 31,4 | 51,7 (hasta el relleno de SPM) | 56 |
  | L640 | 31,4 | 51,7 | 52,7 (hasta el relleno de la banda) |
- **Las tarjetas no se mueven** por los paneles (AC-WKT-18-16): la geometría de la 4b no cambia; la columna solo lee SPM y la banda.
- **El espacio de la llave, el panel del simulador y el sello**: los paneles no llegan a ninguno (la llave va a la derecha o arriba de `Pausa`; el sello vive en la barra; el panel del simulador puede taparlos en parte, AC-BLE-16-03). `WrenchAudit` y `HudOverlap` suman los rectángulos visuales (§5.6).

### 3.3 La integración en el HUD (`HudView`, `HudZones`, `SeriesColumn`; AC-WKT-18-01, -08, -09, -20; AC-WKT-13-06 a -09)

- **Con rutina**, `HudView` arma `LapsPanel` y `RepsPanel` y un `SeriesColumn` que los coordina; **en el libre, nada** (sin lugar reservado: SPM queda sola, como en la 4b). Los paneles nacen con el HUD, en el cuadro negro de la entrada (DT5-D1 de la deuda), con su estado guardado y el primer paso (DV-48): nada aparece después de la cascada.
- **`Apply(state)`**: `SeriesColumn.Apply(state.Series, instant)` → `HudSeriesChange.Between(anterior, nuevo)` → cada panel recibe su contenido y la motion lo que cambió, pieza por pieza (§2.5, §4.2). Con `Instant` (segundo plano, despertar, el primer estado) todo va a su estado final, sin animar lo que ya pasó (AC-WKT-02-08). Con `Freeze()` (la última foto, O-01) no aplica nada más: la salida se lleva el último segundo (AC-WKT-13-08). Con el panel cerrado no se anima nada del contenido: al abrirlo, la lista ya está, sin marcas.
- **`HudZones.Update`**: con cada disposición, `SeriesPanelGeometry.Column(layout)` y `Place(…)`; los paneles van absolutos con su `TopDp`, su ancho y su alto finales y la escala `sp` (en el *layout*: solo cuando cambia la disposición o el alto final). La columna sigue a SPM: el techo cambia con `SpmHasTarget` en el cuadro del cambio de paso, y el corrimiento visual lo hace `SeriesColumnMotion` con los tiempos de la 4a (00-deuda-diseno §4.4; 04-design-notes §6.5), así el aire con SPM es 8·s en cada cuadro (HLT). `SeriesLayout` (el último `SeriesColumnLayout` y las dos `SeriesPanelPlacement`) queda para la página, las pruebas y el arnés.
- **Un solo escritor de `style.translate` y `style.opacity` de los paneles: `SeriesColumn`** (DT5-14; QA-G5T-04). En cada cuadro en que algo cambió, compone y escribe, por panel (en dp de ventana; `StyleTranslate` es un *struct*, sin asignar; solo si el valor cambió):
  - `translate.y` = la subida de la cascada (`PieceVisual.ShiftDp` × `sp`) + el corrimiento de la columna que sigue a SPM (`FollowBase` × `s`) − el desplazamiento de la columna (`ScrollBase` × `sp`, solo en `ColumnScroll`) y, en repeticiones, + su subida al aparecer (`RepsRiseBase` × `s`) + **el FLIP**: (borde visual de abajo de parciales − su borde final), con el `G` de `SeriesPanelVisual` de parciales;
  - `opacity` = la de la cascada × la de la salida × (en repeticiones) `RepsAlpha`.

  La página (`ApplyPiece`, la salida) ya no escribe los estilos de `SeriesLaps` ni de `SeriesReps`: se los pasa a `HudView.Series.SetPiece(piece, opacity, shiftBase)`. Lo que el panel dibuja (el vidrio, los renglones, los desplazamientos de las filas en `RowsScroll`) sigue siendo dibujo. Como el FLIP y el desplazamiento de la columna son transformaciones, la caja del panel (`worldBoundingBox`) va con ellos y la selección de UI Toolkit los encuentra donde se ven (lo confirma la sonda S-B5, §7.0).
- **El de abajo sigue al de arriba** (FLIP): el panel de repeticiones va en su lugar final y se corre con ese `translate`: el aire de 8·sp vale lo mismo en cada cuadro y nada más se mueve (AC-WKT-18-18; en VID-01, hasta −59 dp).
- **La serie que empieza y la que termina** (04-design-notes §6.4): el panel de repeticiones pasa a `display: flex` en el cuadro del cambio y aparece en su estado guardado, sin abrirse (opacidad y +8·s → 0, 120-400); en el mismo segundo, parciales suma la línea `Rep` (120-280) y crece en 0-160. Al terminar, se va (0-120) sin moverse, y después a `display: none`; parciales pierde la línea `Rep` (0-120) y se achica en 80-240. Si tenía el foco, primero `Blur()` (§3.5).
- **El toque** (DT5-15): `PointerDown` / `PointerMove` / `PointerUp` en cada panel, que llegan por la selección real. Como la selección de UI Toolkit (`Panel.PerformPick`) descarta un elemento si el punto cae fuera de su `worldBoundingBox` **antes** de llamar a `ContainsPoint` (QA-G5T-04), cada panel tiene un hijo oculto `.rm-hud-series-panel__hit` (absoluto, `visibility: hidden`, `pickingMode: Ignore`, sin dibujo) que agranda su caja hasta la unión del área de toque y de lo que dibuja en la animación en curso (al cerrar, el vidrio está más alto que el *layout* final; al achicarse por un dato, también): se ajusta en `Place`, al empezar cada animación del alto y al terminarla, nunca por cuadro (con un resorte crítico, sin rebote, el alto queda siempre entre el de partida y el final). `ContainsPoint` (en base del panel) = el área de toque de este momento (cerrado, el chip agrandado; abierto o animando, de 0 a `G`), recortada a la ventana de la columna en `ColumnScroll`. Si el puntero se movió menos de 8 dp, `Expansion.Toggle(kind)` (en la app, `AppContext.SeriesPanels`: guarda en el acto); la vista cambia con su `State` (los dos inmersivos que conviven un instante en QA-V4a-01 ven lo mismo). **No responde** con un diálogo abierto (el velo ya captura el puntero), durante la salida (la página está inerte) ni antes de su tramo de la cascada (`SetInteractive(false)` hasta los 520 ms, como los controles hasta los 840: AC-WKT-13-07, P-15); con la rutina terminada (detrás del diálogo de completado) tampoco (AC-WKT-18-20). Inerte, sin *hover*, sin presionado y sin anillo. Abrir o cerrar no toca el motor, la sesión ni la telemetría (AC-WKT-13-14). Si la sonda S-B5 muestra que el hijo oculto no agranda la caja, el plan B: un elemento transparente de toque por panel (+2, §3.6).
- **La cascada** (AC-WKT-13-06; VID-06): `HudPiece.SeriesLaps` y `HudPiece.SeriesReps` con el tramo de SPM (520, 280, +8 → 0); `ImmersiveCascadeMotion` no cambia de contrato (da un `PieceVisual` por pieza) y la página se lo pasa a `SeriesColumn` (arriba). Con reducir movimiento, el fundido de 120 con todo el HUD. En W1280 la subida es 8·sp (9,6 dp) y no 8·s (12,8): se acepta (DT5-10, DN5-03).
- **La rotación** (AC-WKT-18-20, AC-WKT-17-07): en el acto, con la geometría nueva, `sp` nueva, el estado, los valores y el desplazamiento en 0, sin animación (`Retarget(…, instant: true)`).
- **Nada animando al salir** (AC-WKT-13-09; ROB-02): `HudView.Dispose` llama `Stop()` en las dos motions y en `TabularCells`; cada panel las detiene en `DetachFromPanelEvent` (§4.4).
- **El panel que vuelve a un panel** (`AttachToPanelEvent` después de un `DetachFromPanelEvent`; QA-G5T-12): las motions detenidas van a su **estado final** (el alto, los fundidos, la flecha, la marca apagada, sin empuje), el panel vuelve a medir sus textos con su *holder* (las medidas de antes pueden ser de otra ventana o de otra letra), recalcula las tintas, las escalas y el corte de los vacíos, ajusta su hijo de toque y pide `MarkDirtyRepaint`. Nada anima lo que pasó sin panel. Lo prueba SPV (`ReattachGoesToTheFinalState`: sacar el panel a mitad de un resorte, cambiar la ventana, volver a ponerlo).

### 3.4 La cuenta grande, `PAUSA` y el sello (AC-WKT-18-15, -16; AC-WKT-13-12, -14-18; D-ANI-05)

- **Con la geometría del Designer** (137 base), el panel termina 8·s antes del disco de `HudLayout.BigCount` en todos los perfiles (TV: 159,1 contra 167,6), así **la cuenta grande y `PAUSA` se ven siempre en su lugar de la 4b**, con los paneles cerrados o abiertos (CAP-19 a CAP-23; VID-09 sin nada que salga). P-04 queda sin efecto.
- **La regla igual existe** (spec §12 punto 6: "decide en cada cuadro"): `CenterCallouts.SetBlockedByPanels(SeriesPanelGeometry.BlocksCallout(lapsFinal, repsFinal, layout), instant)` se evalúa con los rectángulos **finales** en el cuadro de cada toque o cambio, así la salida del disco (200, o 120 con reducir movimiento) termina antes de que el panel, que tarda ≈ 590 ms con su resorte, llegue a su lugar. Con 137 da `false` en los cinco perfiles; con los números de la spec (200 abiertos) da `true` en TV y P360 y el disco no se muestra (P-04 por defecto). `SeriesPanelGeometryTests` prueba los dos (DT5-01).
- **El sello** es la tercera fila de la barra: los paneles nunca llegan (CAP-24).
- **La cuenta de la barra** (crece y se tiñe) y el halo de la píldora no cambian.

### 3.5 El teclado (AC-WKT-18-12; AC-APP-05-07)

- El anillo del inmersivo pasa a `Terminar` → `Pausa` / `Reanudar` → **parciales** → **repeticiones** (si se ve) → la llave (con el simulador) → `Terminar` (P-06). El primer Tab sigue en `Terminar` (la regla de la 4b). Con un diálogo abierto, Tab no llega (la página se abstiene, como hoy).
- Los paneles son `focusable` solo desde su tramo de la cascada (como los controles, QA-G4bT-09 (a)). **Enter** llega como `NavigationSubmitEvent` y lo abre o cierra y guarda, como un toque. **Espacio no manda `NavigationSubmitEvent`** en esta app (QA-G5T-09): como `RmButton` (`Presentation/Controls/RmButton.cs`, `OnKeyDown`), el panel escucha `KeyDownEvent` con `KeyCode.Space`, lo detiene (`StopPropagation`) y va por el mismo camino que Enter. Un mismo toque de tecla cambia el panel **una sola vez** (si en el mismo cuadro llegara además un `NavigationSubmitEvent` del Espacio, se ignora): SKT lo prueba con un `KeyDownEvent` de Espacio de verdad, y cuenta una sola escritura.
- **El anillo de foco lo dibuja el panel**, en dp de ventana (blanco de −4 a −2 y halo de −8 a −4 del borde visual, radios 14·sp + 4 y 14·sp + 8: 18,8 y 22,8 en TV, 20,8 y 24,8 en W1280; DD5-22), y sigue su alto visual en cada cuadro del resorte (CAP-26), solo en el modo teclado de `FocusHighlight` (DT5-11).
- **El panel enfocado que desaparece** (repeticiones al salir de la serie): `Blur()` antes de esconderse; el siguiente Tab llega a la raíz y `OnPanelNavigation` lo manda a `Terminar` (índice −1 → el primero del anillo).

### 3.6 El presupuesto (R4a-03; AC-WKT-18-21; DD5-11)

| Cuenta | Elementos a la vista |
|---|---|
| El peor caso de la 4b (tres objetivos fuera, BPM, `Próximo`, la cuenta grande y el sello en dos etiquetas: 04b B-01) | **58-59** (WP-B0 lo mide con `DescribeVisible` antes de tocar nada: el número real va al informe) |
| + los dos paneles abiertos, en cualquier contenido (un elemento cada uno; los *holders* están ocultos) | 60-61 |
| **M5** (WP-B0): `margin-left: auto` en el primero del grupo de la derecha que esté a la vista (`#hud-up-next`, o `#hud-countdown` sin `Próximo`) en lugar del espaciador `.rm-flex-spacer` de la fila (−1; la sonda S-B4b de la 4b no lo descartó: quedó sin hacer por opcional). El margen pasa de uno al otro cuando `Próximo` entra o sale del árbol (no por cuadro), así con `Próximo` la cuenta conserva su aire de 8 o 6 y sin él queda pegada a la derecha, como con el espaciador (§7.5) | **59-60** (ya ≤ 60) |
| **Una de dos** (WP-B1, −1, para dejar margen a la 6a: R4b-02): (a) el sello en **un** elemento que dibuja el título y el detalle con `DrawnText`, o (b) `Próximo:` y su tipo en un elemento (M3) con `DrawnText`. Se elige la que dé con la sonda S-B5 y con menos riesgo sobre lo verificado de la 4b (el sello no tiene motion propia de ancho; `Próximo` sí, su resorte): **se prefiere (a)**. Va en WP-B1 porque necesita `DrawnText`, que nace ahí | **58-59** |

- **Los nombres de hoy siguen valiendo** (QA-G5T-10). La junta deja el elemento con el nombre del primero (`#hud-seal-title` en (a), `#hud-up-next-label` en (b)) y el otro pasa a **ranura** del mismo elemento (`hud-seal-text`; `hud-up-next-type`), con su texto, su rectángulo, su alfa y su color, por `IDrawnTextSource` (DT4b-01). `UiTestHost.TextOf`, `IsShown`, `Bounds`, `Find`… y `PixelProbe` buscan primero un elemento con ese nombre y, si no hay, una ranura (§6.2). Así no cambian las consultas de `Snapshots/HudExrCatalog.cs` (`hud-seal-*`, `hud-up-next*` y las sondas de color del sello), `Snapshots/WorkoutCatalog.cs` (`hud-up-next-label`, `-type`), `Tests/PlayMode/HudLayoutTests.cs` (`hud-seal-title`: uno solo), `StepBarTests.cs`, `ImmersiveTransitionTests.cs` y `HudBudgetTests.cs` (`hud-up-next`, que no cambia). Lo que mira un **estilo** del elemento que se va sí cambia y se lista en B1: en (b), `ImmersiveHudTests.cs:116-117` (`resolvedStyle.color` y `opacity` de `hud-up-next-type` y `-label`) pasa al color y al alfa de la ranura; en (a), nada. El conjunto `it4b` tiene que salir **igual byte a byte** con la junta: B1 la hace **primero**, antes de conectar los paneles a `HudView`, y corre `capture-snapshots.ps1 -Set it4b -Out <scratch>/…` con 0 diferencias de bytes contra las versionadas; recién después suma los paneles.
- **El tope sigue en 60.** Si con todo lo de arriba no se llega (la sonda falla en las dos juntas), el tope pasa a **61** con la medición en ARCHITECTURE §20.2: `HBT.SeriesWorstCaseWork` y la ventana `[PERF]` del build (máx ≤ 33 ms, p99 ≤ 16,7 ms) con los paneles abiertos y cerrándose (§3.7). Lo decide el informe de WP-B0, antes de WP-B1.
- **El peor caso de la 5** (`HudBudgetTests.AtMostSixtyVisibleElementsInTheFiveWorstCase`): `Presupuesto 5` (`RoutineSeeds.Budget5`: la rutina del presupuesto de la 4b dentro de una serie ×2, así a los 5,5 s están los tres objetivos fuera, BPM, `Próximo`, la cuenta grande, el sello y el panel de repeticiones), con las dos preferencias en verdadero, en TV, P360, TH, L640 y W1280. Como cada panel es un elemento en cualquier estado (y sus hijos ocultos no cuentan), la cuenta no depende de las filas; las filas (4 vueltas, la línea, 3 repeticiones y una vuelta entrando) y `S-LARGO` se miden aparte, en tiempo de dibujo (`HBT.SeriesWorstCaseWork`).
- **El trabajo por cuadro** (`HBT.SeriesWorstCaseWork`; ARCHITECTURE §20.2, UI ≤ 1,0 ms): en 60 cuadros reales del peor caso animando (la vuelta nueva entrando con 4 vueltas, 3 repeticiones y la línea; abrir; `S-LARGO`), con el `LastApplyMs` del HUD y el `LastDrawMs` de cada panel y de la fila de píldoras (`Stopwatch`, como `PillRowView`): **la mediana ≤ 1,0 ms** (el tope de UI de §20.2, en el Editor como indicador: el teléfono es [Device-And]) y **el peor cuadro ≤ 16 ms** (el de la 4b). Si la mediana no da, va al informe de B1 con lo medido y el plan B de la sonda (las filas que no cambian guardadas).
- **0 B por cuadro, en cuadros reales** (AC-WKT-18-21; QA-G5T-03; DT5-18). `GcAllocations.During(() => H.Step())` mide el C# del cuadro (el programador, el `MotionDriver`, `Apply`, los *callbacks*), **no** el `generateVisualContent`, que Unity corre en su *loop* entre dos `yield return null` (lo dejó escrito la 4b, `../04b-hud-exr/05-qa-verification.md` R2.5). En la 5 casi todo el trabajo es repintado (≈ 80 `DrawText`, `Painter2D`, `Allocate` y `TabularCells` por cuadro de cada animación), así que se mide con **`FrameAllocations`** (`Tests/UiSupport`, N): un `ProfilerRecorder` de la muestra `GC.Alloc` en el hilo principal (`CollectOnlyOnCurrentThread`; la suma por cuadro de `ProfilerRecorderSample.Count`) y otro del contador "GC Allocated In Frame" (bytes), leídos después de cada `yield return null` durante **30 cuadros reales** de cada animación: abrir, cerrar, la vuelta nueva entrando con la marca, el cambio de contenido por pieza, la serie que empieza, la rueda en W1280 y la máscara de `ColumnScroll` en TH. Cuál de los dos contadores manda lo decide S-B5 (el que vea el control positivo en *batchmode*). Siempre con su **control positivo**, que queda como prueba (`FrameAllocationsTests`, B1): un elemento cuyo `generateVisualContent` asigna un `byte[64]` en cada repintado da ≥ 30 en 30 cuadros; el mismo sin asignar da 0; y el HUD montado y quieto (la línea de base del anfitrión) da 0. Si la línea de base no da 0 (ruido del corredor de pruebas o del Editor), cada medición resta la línea de base tomada en la misma prueba justo antes, con los mismos cuadros, y el control positivo tiene que seguir viendo su asignación por cuadro; si la línea de base no es estable, va al informe de B0 y QA decide. `GcAllocations` sigue para el C# de las motions (SMT). Los textos se arman a 1 Hz (§2.5); `TabularCells` copia a sus `char[]` sin asignar; `DrawText` recibe cadenas ya hechas (las de `HudSeries` y las de un carácter precalculadas). Si `DrawText` asigna por llamada (lo mide S-B5), es lo que dice R5-11.

### 3.7 Rendimiento en el build (AC-WKT-18-21 `[PERF]`; AC-WKT-13-15)

- **`-rowmate-perf-series <segundos>`** (solo en el Editor y en desarrollo, con `ROWMATE_DATA_DIR`; `BuildOutputInspector` lo busca en el build normal): corre `Fondo 3000` y después `Series 600` con el simulador, abre y cierra los paneles cada 2 s (por la API de las preferencias, sin el toque de verdad) y escribe las ventanas `[PERF]` de `FrameTimeProbe` (`[PERF] paneles: máx X ms, p99 Y ms, N cuadros`). Objetivo: ningún cuadro de más de 33 ms y p99 ≤ 16,7 ms.
- **AC-WKT-13-15** (la entrada sin tirón, la deuda de la 4b): `-rowmate-perf-entries 3 -rowmate-perf-routines "Prueba 4a;Cinco reps"` con `prefs.json` sembrado con las dos claves en verdadero (`SnapshotCli.SeedDatabase -seed F5 -prefs laps,reps`), así la tercera entrada lleva los dos paneles desde la cascada.
- **Los builds** van solo a `Builds/Windows/simulator`; si el `RowMate.exe` del usuario está abierto ahí, no se cierra: se reintenta cada 2 minutos hasta 20 y, si sigue, esa parte queda NO VERIFICABLE (spec §12 punto 11).

---

## 4. Motion (WP-A las líneas de tiempo, WP-D1 los `IMotion`)

### 4.0 La regla de oro, en cada pieza (ANIMATION_BIBLE §0.1; AC-WKT-13-14, AC-WKT-18-08)

| Animación | De qué dato sale | Por qué no adelanta ni frena |
|---|---|---|
| Abrir y cerrar | la preferencia, que cambia y se guarda en el cuadro del toque | el contenido ya es el del segundo; solo cambian el alto visual, los fundidos y la flecha |
| La vuelta en curso | `HudSeries`, una vez por segundo | el texto cambia en el cuadro del segundo, con el rodillo de 180 en las celdas que cambian; **no cuenta décimas en vivo** |
| La vuelta nueva y la repetición nueva | `HudSeriesChange.NewLaps` / `NewRep` en el cuadro del segundo | la fila entra con su valor final (a los 200 ms se lee entera); la marca es de aspecto |
| El cambio de contenido, por pieza (la vuelta en curso ↔ el vacío funde; trabajo → trabajo, el rodillo; vacío → vacío, nada) | el tracker en el segundo siguiente al del cambio de paso (P-01) | sale el viejo y entra el nuevo, ya con sus valores; el alto se acomoda con el empuje (0-160 / 80-240), que termina antes del segundo siguiente |
| La serie que empieza o termina | `InSeries` del tracker | el panel entra o se va; su estado (abierto o cerrado) es el guardado |
| La columna que sigue a SPM | `SpmHasTarget` del paso nuevo | es el corrimiento de Watts de la 4a |
| El desplazamiento | el usuario | no toca el dato |

La prueba: `PresentationIndependenceTests` (dotnet: la secuencia del motor con y sin el *store*, con las preferencias cambiando) e `ImmersiveHudTests.PresentationNeverTouchesTheData` (PlayMode: con los paneles abriéndose, cerrándose, con reducir movimiento y sin HUD; los valores de los paneles cambian en el mismo cuadro que el reloj de la banda).

### 4.1 Las líneas de tiempo puras (`SeriesTimelines`, WP-A) y las constantes

Funciones de los **ms enteros** desde el comienzo (`HudTimelines.Ms`, `Reached`; QA-G4bT-07 (b) de la 4b), con la variante de reducir movimiento, que devuelven *structs* y no asignan. **Dos clases de movimiento** (04-design-notes §6.0; SD-12): lo que hace el usuario (abrir y cerrar, y su inversión) va con el resorte `spring.smooth` (ω 12), evaluado con la forma cerrada de `CriticalSpring` **desde su posición y su velocidad** (un toque en medio la invierte sin saltos: AC-WKT-18-18); lo que cambia el dato (una vuelta, una repetición, un paso, la serie) va con curvas de duración fija que terminan en 160-280 ms, antes del segundo siguiente, y **el alto que cambia por un dato va con el empuje**, no con el resorte (R-04, DD5-16).

| Línea de tiempo | Devuelve | Tabla que reproduce (± 0,01; ± 0,1 base) |
|---|---|---|
| `SeriesTimelines.Open(t, from, to, v0, reduced)` / `.Close(…)` | `PanelToggleVisual`: el alto (el resorte; al cerrar, desde los 80), el contenido (80-240 / 0-120), el rótulo corto y el título (0-80 / 80-200), la flecha (grados, 240; con reducir movimiento, los dos trazos con su fundido). La cola queda en su lugar final (`TailShiftBase` 0: la revela el vidrio) | 04-design-notes §6.1 (VID-01: de 34 a 90; cerrar de 90 a 34; los fines del resorte de §6.1); con reducir movimiento, §6.8 |
| `.DataResize(t, fromBase, toBase, reduced)` | el alto por un dato (DD5-16): crece en 0-160, se achica en 80-240, `ease.move`; con reducir movimiento, en el acto a los 0 si crece y a los 120 si se achica | §6.2 y §6.3 (VID-03: 113,9 · 112,8 · 109,0 · 106,6 · 106,0 a los 100 · 133 · 167 · 200 · 233; VID-04: 105,8 · 103,7 · 96,1 · 91,1 · 90,0 y 83,7 · 80,5 · 69,1 · 61,7 · 60,0) |
| `.RowsChange(t, newRows, dropped, reduced)` | `RowsVisual`: el empuje (−22·k → 0, 0-160, `ease.move`), la que entra (**80-240**, −8·sp → 0, `ease.enter`, R-05), la que sale (0-120, `ease.exit`, además de la regla 2 de §3.1) y `PushProgress` (para la escala común, DD5-24) | §6.2 (la tabla de VID-02: el empuje −22,0 · −21,0 · −15,7 · −4,6 · −0,6 · 0; la fila nueva 0,331 a los 100 y 0,983 a los 200; la que se va 0,978 · 0,127 · 0) |
| `.Marker(t, reduced)` | el alfa de la marca (entra con su fila, en 1 hasta los 600, se apaga en 600-1400, `ease.move`; igual con reducir movimiento) | §6.2 (0,929 · 0,500 · 0,071 a los 800 · 1000 · 1200) |
| `.Swap(t, change, reduced)` | `SwapVisual`, **por pieza** (DD5-18): lo viejo sale (0-120), lo nuevo entra (120-280, +4 → 0); solo para lo que cambia de clase (`SeriesBodyChange.ToEmpty` / `FromEmpty`); con `Roll` o `None`, nada; las filas que se vacían salen en su lugar (0-120); la línea `Rep` sale o entra solo con la serie | §6.3 (0,978 · 0,823 · 0,422 · 0 y 0,226 · 0,646 · 0,872 · 0,973 · 0,999) |
| `.RepsPanel(t, appearing, reduced)` | la opacidad y la subida del panel de repeticiones (120-400 con +8·s; 0-120) | §6.4 (0,134 · 0,424 · 0,634 · 0,785 · 0,890 · 0,952 · 1) |
| `.ColumnFollow(t, gaining, reduced)` | el corrimiento de la columna (0-240 al ganar `obj.`; 80-280 al perderlo; 16,9·s) | §6.5 |
| `.Wheel(t, from, to, v0, reduced)` | el desplazamiento con `spring.snappy` (ω 22) | §6.6 |
| `.Hover(t, from, to)` | la capa de *hover* y presionado: hacia 0,04 / 0,08 en 90 (`ease.enter`), hacia 0 en 160 (`ease.exit`), desde donde esté | §4.9 |
| `ImmersiveTimeline.Entry(SeriesLaps / SeriesReps, …)` | `PieceVisual` con el tramo de SPM (520-800) | §6.7 |

- **La cola** (`TailShiftBase`, DD5-16): en un cambio por dato, la línea `Rep` y la ventana de filas bajan o suben con el alto: `G − H_final`; al abrir o cerrar, 0. Si el resorte del alto estaba corriendo (un toque menos de 0,6 s antes), el resorte toma el alto nuevo desde su posición y su velocidad, y las filas y la cola nunca van más rápido que el vidrio: `corrimiento = min(empuje; G − H_final)`; la regla 1 esconde lo que todavía no cubre.
- **Los aires mínimos** que estas curvas garantizan (04-design-notes §6.2, §6.3) son pruebas de STL: la fila nueva y la que baja, ≥ 5 base en todos los cuadros de VID-02 (5,48 con 80-240); lo nuevo y la cola, ≥ 5 base en VID-03 y VID-04 (5,34 con el empuje).

**Constantes nuevas de `MotionSpecs`** (00-deuda-diseno §4.7, con 04-design-notes §6.9):

| Constante | Valor |
|---|---|
| `SeriesHeight` | `spring.smooth`, **solo para abrir, cerrar y su inversión** |
| `SeriesShrinkDelayMs` | 80 (cerrar) |
| `SeriesDataGrowMs` | 0-160, `ease.move` (= `SeriesRowsPushMs`) |
| `SeriesDataShrinkStartMs` / `EndMs` | 80 / 240, `ease.move` |
| `SeriesContentInStartMs` / `EndMs` · `SeriesContentOutMs` | 80 / 240 (`ease.enter`) · 120 (`ease.exit`) |
| `SeriesLabelOutMs` · `SeriesLabelInStartMs` / `EndMs` | 80 · 80 / 200 |
| `SeriesChevronMs` | 240 (`ease.move`) |
| `SeriesRowsPushMs` | 160 (`ease.move`) |
| `SeriesRowInStartMs` / `EndMs` | **80** / 240 (`ease.enter`, `shift.s`) |
| `SeriesRowOutMs` | 120 |
| `SeriesMarkerOutStartMs` / `EndMs` | 600 / 1400 (`ease.move`) |
| `SeriesSwapOutMs` · `SeriesSwapInStartMs` / `EndMs` · `SeriesSwapRiseBase` | 120 · 120 / 280 · 4 |
| `SeriesRepsAppearStartMs` / `EndMs` (con 8) · `SeriesRepsLeaveMs` | 120 / 400 · 120 |
| `SeriesHoverInMs` / `OutMs` · `SeriesHoverAlpha` / `SeriesPressedAlpha` | 90 (`ease.enter`) / 160 (`ease.exit`) · 0,04 / 0,08 |
| `SeriesReducedInMs` · `SeriesReducedOutMs` | 160 · 120 (lineales) |
| `SeriesReducedChevronOutMs` / `InMs` | 0-80 / 80-160 (lineales) |
| `SeriesWheel` | `spring.snappy` |
| `SeriesSettleTapMs` | 820 (la regla de toma, §5.1) |

Las de geometría y de alfa por la tinta (`SeriesRevealFade` 4, `SeriesRowsEdgeFade` 4, `SeriesScrollFade` 12, `SeriesChevronStroke` 1,5, `HitRoomyDp` 56 tope) están en `SeriesPanelGeometry` (§3.2). Si una revisión del Designer cambia **un número**, el paquete que siga cambia **solo esa constante y su valor en la prueba** (la excepción de la 4b, §7.6). Un cambio de **estructura** (una curva por otra, una fase nueva, otro reparto) no entra en esa excepción: se escribe primero en este diseño, como se hizo con las notas (§9).

### 4.2 Los `IMotion` de la 5 (WP-D1) y su contrato

Como en la 4b (§7.4 de su diseño): no conocen las vistas; reciben el `MotionDriver` y un *callback* con un *struct* de WP-A; respetan `Driver.Armed`, `Driver.Settings.ReduceMotion` e `Instant`; pasan el tiempo a ms enteros; no asignan por cuadro; dicen `IsFinite` por fase (lo mira la regla de toma); salen del *driver* con `Stop()`, que la vista llama en su `Dispose` y en su `DetachFromPanelEvent`. Si una lanza, `MotionDriver` ya la aísla (O-4).

| `IMotion` (D1) | Construcción | Lo que expone | Fases (`IsFinite`) | Reducir movimiento | Lo usa (B) |
|---|---|---|---|---|---|
| `SeriesPanelMotion` (uno por panel) | `(MotionDriver, Action<SeriesPanelVisual> apply)` | `SetExpanded(bool expanded, double targetHeightBase, bool instant)` (el resorte), `ContentChanged(in HudSeriesChange change, double targetHeightBase, bool instant)` (el empuje, la cola y las piezas: DD5-16, DD5-18), `Hover(bool hovered, bool pressed, bool instant)`, `Retarget(double heightBase, bool instant)` (rotación, `Instant`), `Visual`, `Stop()` | F mientras corre el resorte del alto (hasta que le falte < 0,15 dp, `ISettlingMotion`), el alto por un dato (0-240), un fundido, la flecha, el empuje, la fila que entra o sale, las piezas que cambian, **la marca hasta los 1400** y el fundido del *hover*; si no, sale | 04-design-notes §6.8: el alto en el acto (por un dato, a los 0 si crece y a los 120 si se achica), fundidos lineales, la flecha cambia de trazo (0-80 / 80-160), la marca igual; el *hover*, el mismo fundido (es opacidad; DN5-07) | `SeriesPanelView` |
| `SeriesColumnMotion` | `(MotionDriver, Action<SeriesColumnVisual> apply)` | `RepsAppear(bool instant)`, `RepsLeave(bool instant, Action gone)`, `FollowSpm(bool gaining, bool instant)`, `WheelTo(double offsetBase, bool instant)`, `ShiftScroll(double deltaBase)` (DD5-27, en el acto), `Visual`, `Stop()` | F en la entrada (120-400) y la salida (0-120) del panel de repeticiones, en el corrimiento (0-280) y en el resorte de la rueda; si no, sale | solo opacidad; el corrimiento y la rueda en el acto | `SeriesColumn` |

`SeriesPanelVisual` (WP-A, en `SeriesTimelines.cs`): `HeightBase` (`G`), `TailShiftBase` (la cola y la ventana de filas: `G − H_final` por un dato, 0 al abrir o cerrar; DD5-16), `ContentAlpha`, `ChipLabelAlpha`, `TitleAlpha`, `ChevronDegrees` (o, con reducir movimiento, `ChevronDownAlpha` y `ChevronUpAlpha`), `RowsPushBase`, `PushProgress`, `NewRowsAlpha`, `NewRowsShiftBase`, `NewRows`, `DroppedAlpha`, `ClearedRowsAlpha`, `MarkerAlpha0..3` (una por fila nueva), `BodyOldAlpha`, `BodyNewAlpha`, `BodyNewRiseBase` (la pieza que cambia de clase), `RepLineAlpha`, `RepLineRiseBase` (la línea `Rep` que entra o sale con la serie), `HoverAlpha`. Las reglas de alfa por la tinta (§3.1) no son de la motion: las aplica la vista con `G`, `TailShiftBase` y el desplazamiento. `SeriesColumnVisual`: `RepsAlpha`, `RepsRiseBase`, `FollowBase`, `ScrollBase`.

**Con el panel cerrado no se anima el contenido** (04-design-notes §6.0): el chip no cambia; al abrirlo, la lista ya está, sin marcas. **En pausa** nada cambia y se puede abrir y cerrar (AC-WKT-18-20). **Al volver del segundo plano o al rotar**, `Instant`: las vueltas y las repeticiones de ese lapso están en el primer cuadro, sin empuje, sin marca y sin rodillos (AC-WKT-02-08, AC-WKT-18-19). **Inerte** (diálogo, salida, antes de los 520, rutina terminada): `Hover(false, false, instant: true)`.

### 4.3 Reducir movimiento (AC-APP-05-08; VID-08)

La tabla de 04-design-notes §6.8 (que revisa 00-deuda-diseno §4.6), entera, en las variantes `reduced` de `SeriesTimelines` y probada fila por fila (`STL.ReducedVariants`): abrir con el alto en el cuadro 0 y el contenido en 0-160 (0,208 · 0,417 · 0,625 · 0,833 · 1); cerrar con el contenido en 0-120 (0,723 · 0,444 · 0,167 · 0) y el alto a los 120; el panel de abajo en el acto; la vuelta nueva que funde en 0-160 sin deslizarse, con la marca igual; el cambio de contenido con los mismos fundidos, sin subir, y el alto a los 0 si crece y a los 120 si se achica; la serie que empieza o termina, solo opacidad; la columna y la rueda en el acto; los rodillos, el fundido de 80. Las reglas de alfa por la tinta valen igual. Nada de esto cambia los datos ni cuándo pasan.

### 4.4 Nada animando al salir (AC-WKT-13-09; ROB-02; O-1)

`ITT.NothingAnimatesAfterTheExit` suma dos salidas: a mano en `Series 600` con el resorte de un panel en curso (el toque a 200 ms del `Terminar`) y con una marca encendida (a 300 ms de una vuelta). Después de los 640 de la salida, el `MotionDriver` y las demandas de 60 fps vuelven a las de reposo, sin avisos de DPI en el registro. Los dos resortes de la 4a (O-1) ya los termina `StepInfoBar` (deuda).

### 4.5 0 B por cuadro

- `SMT.NoAllocationsPerFrame` (cada `IMotion` con elementos de prueba, sin repintado propio): `GcAllocations` en 0 durante 30 `Tick` de cada animación de §4.1 (el C# de las motions).
- `HBT.SeriesNoAllocationsPerFrame` (el HUD montado, **con el repintado**): `FrameAllocations` en 0 (o igual a la línea de base, §3.6) durante 30 cuadros reales de cada animación: abrir, cerrar, la vuelta nueva con la marca, el cambio de contenido por pieza, la serie que empieza, la rueda en W1280 y la máscara de `ColumnScroll` en TH; con el control positivo de `FrameAllocationsTests` en la misma corrida (QA-G5T-03).

---

## 5. Capturas y videos (WP-B3; WP-D1 para los valores por cuadro)

### 5.1 Reglas de toma de la 5

Las de la 4b (su §5.2: el instante exacto en ms enteros, la toma que no espera y falla si algo la bloquea, `inFlight` en el *manifest*, dos corridas iguales) y las de spec §7.1, con estas precisiones:

1. **Una `MemoryPreferences` por toma**, sembrada con lo que la toma pide (`Prefs(laps: true, reps: true)`); en la secuencia de la demo, la misma para todas sus tomas (DEMO-11 ve lo que dejó DEMO-03 y DEMO-05). Ninguna toma lee ni escribe un archivo de preferencias (AC-HIS-20-04).
2. **Lo que bloquea** suma los `IMotion` de la 5 en sus fases finitas (§4.2): el resorte de un panel, el alto por un dato (0-240), un fundido, la flecha, el empuje, la fila que entra o sale, **la marca hasta los 1400 ms**, el cambio de contenido por pieza, la serie que empieza o termina, el corrimiento, la rueda y el fundido del *hover* o del presionado (el que dejan los toques del arnés: 160 ms después de soltar).
3. **Un panel recién abierto o cerrado** se toma al menos **0,82 s** después del toque (`SeriesSettleTapMs`; SD-11; 04-design-notes §6.1: a los 820 ms al resorte le faltan 0,08 base en el peor caso de una toma). Las tomas que lo necesitan arrancan con la preferencia en verdadero, salvo DEMO-03 y DEMO-05 (a los 0,8 s, con el resorte a 0,04 dp en W1280: no bloquea).
4. **Ninguna toma fija cae en los 1400 ms que siguen a una vuelta o a una repetición nueva** (la marca, spec §7.1), salvo las que la muestran a propósito (ninguna CAP). La regla no espera: la marca es una fase finita y la toma falla. Por eso dos instantes de la spec se corrieron (DT5-04, aceptado en spec §11.3): **CAP-11 a los 881,2 s** (a los 880,2 la marca de `Rep 1`, que entró a los 879, va por 0,071: se ve `0:03.0` `12 m` y `Rep 2/4 · 12 m`), y **DEMO-11 a los 31,6 s** (a los 31,2 el panel de repeticiones está apareciendo, 0,634; a los 31,6 sigue siendo el segundo 31: `0:01.0` y ≈ `3 m`).
5. **Con desplazamiento**, siempre en 0 (arriba).
6. **El contorno de la zona del bote** (`UiHostOptions.BoatZoneOutline`) en CAP-03, CAP-14 a CAP-18.
7. **Las sondas de aspecto** de 04-design-notes §9.6 (S5-01 a S5-11) en toda toma con un panel abierto, en la tabla de `SeriesCatalog` (los valores los ajusta WP-D2).
8. **Las comprobaciones de rectángulos** de cada toma son las de 04-design-notes §9.2, §9.3 y §9.5 (± 0,5 dp; DD5-25): `Series 600` y las demás rutinas de la 5 **no tienen objetivos en SPM**, así que en TV la columna empieza en **210,9** (no en 228,6); en TH y L640 entran 93,9 base. Si una toma de `Series 600` da la columna en 228,6, está mal la toma, no la geometría.
9. **`S-LARGO`** (CAP-25) se arma a mano: `Series 600` a los 170,2 s con los dos abiertos y `UiTestHost.OverrideSeries(SeriesSeeds.Largo)` (como la fila de 1998 píldoras de la 4b), sin correr el motor más.
10. **El remo de la demo** es el simulador sin ruido; las tomas con `R-4D` usan el remo de prueba con lecturas que no llegan (`ScenarioScript`).

### 5.2 El catálogo `it5` (`Snapshots/SeriesCatalog.cs`)

Archivos `CAP-nn-<tema>.png`, `DEMO-nn-<tema>.png` y `VID-nn-<tema>.mp4` en `docs/migration/iterations/05-series-parciales/snapshots/`, con `manifest.json` e `INDEX.md` sin rutas de la PC. Todas en español, `F-5`, sin pulsómetro y con `R-4` salvo lo dicho. Cada una deja en el *manifest* los rectángulos de los paneles (los visuales y las áreas de toque), `sp`, la regla de la columna y las ranuras con su texto, su alfa y su escala. "Comprobaciones" = `Checks` (si fallan, la toma falla); además, `HudOverlap` y `WrenchAudit` en todas (§5.6). Los rectángulos van en dp de ventana (± 0,5; DD5-25), con la columna **sin objetivo** en SPM salvo lo dicho (04-design-notes §9.2).

| ID | Perfil · estado | Comprobaciones |
|---|---|---|
| CAP-01 | TV · `Series 600` a los 60,2 s | `hud-laps-chip` `500 m` y `hud-reps-chip` `Reps`, cerrados, de 144,4 × 35,8 en x 14,8-159,1: `500 m` en y **210,9-246,7** y `Reps` en 255,1-291,0, con 8,4 de aire (R-12: `Series 600` no tiene objetivos); la flecha ∨ pegada a la derecha, con vidrio vacío entre el rótulo y la flecha; a 32,7 dp de la zona del bote; la barra con 8 píldoras, `Trabajo`, `600m` |
| CAP-02 | TV · 170,2 s · parciales abierto | parciales en y 210,9-331,0 (114): `hud-laps-title` `Parciales 500 m` a 0,949; `hud-laps-current` `0:15.0`, `-meters` `60 m` pegado a la derecha, por la misma línea de base (± 0,25 dp); `hud-laps-row-0` `#1` `2:05.0`; `hud-rep-progress` `Rep 1/3 · 560 m`; el chip `Reps` en 339,4-375,3, a 8·sp del borde visual |
| CAP-03 | TV · 250,2 s · los dos abiertos · contorno | parciales 210,9-305,7 (90): `0:50.0` `200 m`, sin filas; `Rep 2/3 · 200 m`; repeticiones 314,1-377,4 (60): `hud-reps-row-0` `Rep 1` `2:05` `680 m` con los metros pegados a x 127; repeticiones entra en la zona del bote (y > 323,7) solo hasta x 159,1 (`RespectsBoatZone`) |
| CAP-04 | TV · 380,2 s · los dos | parciales 210,9-305,7: `0:10.0` `40 m`; `Rep 3/3 · 40 m`; repeticiones 314,1-400,6: `Rep 2` arriba y `Rep 1` abajo, `2:05` `680 m`, con las columnas alineadas |
| CAP-05 | TV · 190,2 s · los dos | parciales 210,9-322,6 (106): `hud-laps-empty` `Sin parciales en este paso` en dos renglones **`Sin parciales` / `en este paso`** (DD5-19) en `text.tertiary`; `Rep 1/3 · 640 m`; repeticiones 331,0-419,5 (84): `Sin repeticiones` / `completas`; la barra en `Descanso` |
| CAP-06 | TV · 180,6 s · los dos | la barra en `Descanso` y `0:20`; parciales todavía con el trabajo (210,9-331,0): `0:25.0` `100 m`, `#1 2:05.0`, `Rep 1/3 · 600 m`; repeticiones vacío (339,4-428,0) (P-01) |
| CAP-07 | TV · 545,2 s · los dos en verdadero | parciales 210,9-299,4 (84): solo el vacío, sin `hud-rep-progress`; `#hud-reps-panel` fuera del árbol visible |
| CAP-08 | TV · `Fondo 3000` con `R-4A` a los 600,2 s · parciales | parciales 210,9-377,4 (158): `0:50.0` `250 m`; filas `#5` `#4` `#3` `1:40.0` y `#2` `2:05.0`, con los rótulos en la columna de `#99` y los tiempos alineados; sin `#1`, sin línea `Rep`, sin chip `Reps` |
| CAP-09 | TV · `Trabajo 1000` con `R-3` a los 170,2 s · parciales | parciales 210,9-307,8 (92): `0:03.3` `10 m`; `#1 2:46.7` |
| CAP-10 | TV · `Cinco reps` a los 545,2 s · los dos | parciales 210,9-305,7: `0:05.0` `20 m`; `Rep 5/5 · 20 m`; repeticiones 314,1-423,7 (104): `Rep 4`, `Rep 3`, `Rep 2`, cada una `2:05` `540 m`, a escala 1 |
| CAP-11 | TV · `Pirámide 500` a los **881,2 s** · los dos (DT5-04) | SPM sin objetivo; parciales 210,9-305,7: `0:03.0` `12 m`; `Rep 2/4 · 12 m`; repeticiones 314,1-377,4: `Rep 1 2:05 1.112 m` a la escala común **0,969** (± 0,01 de lo que mida Unity, R5-02); sin sello |
| CAP-12 | TV · en · CAP-03 | `500 m splits` y `Previous reps` a la escala de título del inglés, **1** (DD5-20); `0:50.0` `200 m`; `Rep 2/3 · 200 m`; `Rep 1 2:05 680 m`; los rectángulos de CAP-03 |
| CAP-13 | TV · en · CAP-05 | **`No splits` / `in this step`** y **`No completed` / `reps`** (DD5-19); `Rep 1/3 · 640 m`; los rectángulos de CAP-05 |
| CAP-14 | P360 · CAP-04 · contorno | parciales x 12,9-139,4, y 184,7-267,8; repeticiones 275,2-350,9; no se cruza con Split ni Watts; todo entero |
| CAP-15 | TH · CAP-01 · contorno | los dos chips enteros, sin desplazar, en y 210,9-246,7 y 255,1-291,0, con 18,8 dp hasta el piso (309,8; 76 de 93,9 base); **sin máscara** (nada escondido); la zona del bote a 96,8 dp a la derecha |
| CAP-16 | TH · CAP-04 · contorno | `ColumnScroll`, desplazamiento 0, máscara de y 297,2 a 309,8 (el piso); al 100 % la cabecera y `0:10.0` `40 m`; `Rep 3/3 · 40 m` a 0,925 (± 0,02); el vidrio de parciales se apaga desde y 297,2 hasta su borde de abajo (305,7), donde queda en 0,33 (± 0,03); repeticiones no se ve (empieza debajo del piso) ni responde; nada sobre la banda ni los controles (DD5-15) |
| CAP-17 | L640 · CAP-04 · contorno | lo mismo: la máscara de y 260,3 a 271,4 y el vidrio de parciales hasta su borde (267,8); el panel **no se angosta**: termina en x 139,4, a 39,8 de la zona |
| CAP-18 | W1280 · 340,2 s · los dos · contorno | `sp` 1,20 (± 0,005); parciales x 22,4-186,8, y 320,2-457,0: `0:15.0` `60 m`, `#1 2:05.0`, `Rep 2/3 · 560 m`; repeticiones 466,6-538,6: `Rep 1 2:05 680 m`; todo sin desplazar y al 100 %, sin fundidos |
| CAP-19 | TV · 197,6 s | `#hud-big-countdown` `3` en su disco (x 167,6-243,4, y 234,0-309,9), con **8,4 dp (± 0,5)** de aire a los chips (210,9-291,0); `Próximo: Trabajo`; el halo |
| CAP-20 | TV · CAP-19 · parciales | la cuenta grande **en su lugar**, a 8,4 dp de parciales abierto (210,9-322,6: el vacío en dos renglones y `Rep 1/3 · 668 m`); el chip `Reps` en 331,0-366,8; `0:03` de la barra agrandada y teñida |
| CAP-21 | TH · CAP-19 · los dos | la cuenta grande en su lugar de TH (desde x 419,1); parciales abierto en la columna con el vacío entero; la línea `Rep` todavía no se ve (su tinta pasa el piso: alfa ≤ 0,01); la máscara abajo; el vidrio en 0 en el piso; nada se encima |
| CAP-22 | TV · `Pausa` a los 250,2 s, toma a los 252,2 · los dos | el velo; `Reanudar`; parciales 210,9-305,7 con `0:50.0` `200 m` quieto; repeticiones 314,1-377,4; **`PAUSA` en su disco**, a 8,4 dp |
| CAP-23 | TH · CAP-22 | el velo y `PAUSA` en su lugar; la columna como CAP-16 (la máscara, los alfas), con `0:50.0` `200 m` |
| CAP-24 | TV · `Prueba 4a` a los 90,6 s · los dos | el sello `No alcanzado · 150 / 160 W · 24.0 / 22 spm` en la tercera fila; las tarjetas en su lugar de la 4b; la columna ya subió (SPM perdió el objetivo): parciales 210,9-305,7 con `1:00.0` `240 m` y `Rep 1/2 · 240 m`; repeticiones vacío, 314,1-402,7 |
| CAP-25 | TV · `S-LARGO` · los dos | parciales 210,9-400,6 (180); repeticiones 409,0-518,6 (104); `5999:59.9` `499 m` con la cifra a 0,652; `#1999` a `#1996` a 0,932; `Rep 999/999 · 999.999 m` a 0,712; `Rep 999` a `997` con `5999:59` `999.999 m` a 0,638 (± 0,01 de lo que mida Unity, R5-02); nada cortado (ningún `…`), partido ni fuera del vidrio |
| CAP-26 | W1280 · CAP-18 sin foco · Tab ×3 | el anillo alrededor de parciales: blanco de 2 dp de x 18,4-190,8 e y 316,2-461,0 (por fuera), radio 20,8; el halo hasta x 14,4-194,8 e y 312,2-465,0, radio 24,8; ni el anillo ni el halo tocan SPM (que termina en 307,4) ni el panel de abajo (466,6) |

### 5.3 La demo (DEMO-01 a -12, una secuencia; spec §7.4)

W1280, español, simulador sin ruido a 150 W, `F-5`, una `MemoryPreferences` vacía para toda la secuencia. Los números exactos que da el simulador van al *manifest* y QA los compara con el Core.

| ID | Paso | Comprobaciones |
|---|---|---|
| DEMO-01 | Workout | reposo; `Series 600` primera en la lista |
| DEMO-02 | `Series 600` → `Iniciar`, a los 10,2 s | el chip `500 m` solo, cerrado, debajo de SPM, en y 320,2-361,0 (la columna sin objetivo, 04-design-notes §9.3) |
| DEMO-03 | `TapText("500 m")` a los 12 s; toma a los 12,8 | parciales abierto (84): `Parciales 500 m`; `Sin parciales` / `en este paso`; sin línea `Rep`; `immersive.lapsExpanded` en verdadero en la `MemoryPreferences` |
| DEMO-04 | a los 60,2 s | parciales (90): `0:30.0`; los metros ≈ 113 y `Rep 1/3 · ≈ 113 m` (los del *manifest*); el chip `Reps` en 437,8-478,6 |
| DEMO-05 | `TapText("Reps")` a los 61; toma a los 61,8 | `Series anteriores` abierto y vacío (437,8-538,6); `Sin repeticiones` / `completas` |
| DEMO-06 | a los 170,2 s | parciales (114) con `#1` con el tiempo del *manifest* (≈ `2:12.6`) y la vuelta en curso con pocos metros; repeticiones con sus dos renglones a la vista (`RowsScroll`: 83,2 de 84 base, DD5-26) |
| DEMO-07 | a los 230,2 s | `Rep 1` con `2:13` y ≈ `676 m`; `Rep 2/3 · …` |
| DEMO-08 | llave → `Simular desconexión` a los 240; toma a los 242,2 con el panel del simulador abierto | la vuelta en curso con el tiempo que avanza y los metros quietos (comparados con los del segundo 240); el panel del simulador sin tapar Split, Watts ni la barra |
| DEMO-09 | cerrar el panel del simulador; toma a los 250,2 s | los metros siguen desde donde estaban (ningún salto ni vuelta nueva) |
| DEMO-10 | `Terminar` → `Terminar`; toma a los 800 ms de la salida | Workout en reposo, sin diálogo ni aviso |
| DEMO-11 | `Series 600` → `Iniciar`; toma a los **31,6 s** (DT5-04) | los dos abiertos desde que se ven; `Series anteriores` vacío; `0:01.0` y ≈ `3 m`; `Rep 1/3 · ≈ 3 m`; nada de la sesión anterior |
| DEMO-12 | cerrar los dos; `Terminar` → `Terminar`; toma a los 800 ms | reposo; las dos preferencias en falso |

"Cerrar la app y volver a abrirla" (spec §1 (a) 8) no se puede en el arnés: es [Device-Win] y lo cubre además `BootSceneTests.PreferencesSurviveARestart` (dos arranques de `AppRoot` sobre la misma carpeta temporal).

### 5.4 Los videos (VID-01 a -09; 30 cuadros por segundo; valores por cuadro en el *manifest*)

Cada video deja los valores que devuelven los `IMotion` (el alto, la cola, los fundidos, la flecha, el empuje, la marca, el corrimiento) y los alfas efectivos de cada ranura (con las reglas de la tinta), y sus comprobaciones los comparan con `SeriesTimelines` y con las tablas de 04-design-notes §6 y §9.4 (± 0,01; ± 0,1 base; los alfas por la tinta ± 0,02), y miran que ninguna pieza que no se mueve salte entre dos cuadros y que ningún renglón con alfa > 0 tenga tinta fuera del vidrio (DD5-13). El cuadro `n` es `inicio + n/30`.

| ID | Qué se hace | Comprobaciones |
|---|---|---|
| VID-01 | TV · `Series 600` a los 250 s con repeticiones abierto y parciales cerrado: tocar `500 m` a los 250,3 (cuadro 9) y otra vez a los 251,8 (cuadro 54) | cuadros 9-30: la tabla de 04-design-notes §6.1 (el alto de 34 a 90 con el resorte, el contenido, el título, la flecha, el revelar por la tinta y el FLIP de `Series anteriores`, de −59,0 dp a −0,1); cuadro 30 (251,0): `0:51.0` `204 m` y `Rep 2/3 · 204 m` con el rodillo, en el mismo cuadro que la banda; cuadros 54-75: cerrar (el contenido se va antes de que baje el vidrio); el ancho no cambia en ningún cuadro y ninguna otra pieza se mueve |
| VID-02 | TV · 323,5 a 327,5 s · los dos | cuadro 45 (325,0): `0:00.0` `0 m` y `Rep 2/3 · 500 m` con el rodillo, en el cuadro de la banda; cuadros 45-50: el alto de 90 a 114 con el empuje y la línea `Rep` y `Series anteriores` bajando 25,3 dp con él; cuadros 47-52: `#1 2:05.0` entra desde −8·sp (0,331 en el 48 y 0,983 en el 51); la marca en su pico en los cuadros 52 a 63 y apagándose hasta el 87; el aire entre la fila nueva y la de abajo ≥ 5 base; cuadro 75 (326,0): `0:01.0` `4 m` |
| VID-03 | TV · 178,5 a 184,5 s · los dos | cuadro 45 (180,0): la barra pasa a `Descanso` y los paneles siguen con el trabajo (`0:25.0` `100 m`, `#1`); cuadro 75 (181,0), por pieza: la vuelta en curso y `#1` salen en los cuadros 75-78 y el vacío entra en 79-83 subiendo 4; el alto de 114 a 106 en los cuadros 77-82 (se achica en 80-240); la línea `Rep` sube 8 con el vidrio y su rodillo pasa a `604 m`; `Series anteriores` sube 8,4 dp con el FLIP; el aire entre lo nuevo y la cola ≥ 5 base |
| VID-04 | TV · 198,5 a 203,5 s · los dos | cuadros 15 y 45: la cuenta grande `1` y su salida, en su disco y sin tocar los paneles; cuadro 45 (200,0): la barra al trabajo de la rep 2, los paneles siguen en el descanso; cuadro 75 (201,0): parciales de 106 a 90 (sale el vacío, entra `0:01.0` `4 m`, la línea pasa de `Rep 1/3 · 680 m` a `Rep 2/3 · 4 m` con su rodillo); repeticiones de 84 a 60 (sale el vacío y entra `Rep 1 2:05 680 m` en los cuadros 79-83 con su marca); los dos altos con la misma curva |
| VID-05 | W1280 · simulador sin ruido · 238 a 248 s · parciales · `Simular desconexión` a los 240 | durante el corte, los metros de la vuelta en curso iguales y su tiempo +1 por segundo, cada uno con su rodillo en el cuadro del segundo; después siguen desde los mismos metros; ninguna fila nueva; ningún negativo (AC-BLE-15-09) |
| VID-06 | TV · `Cinco reps` → `Iniciar` con los dos en verdadero | cuadros 16-24 (520-800 ms): los dos paneles, abiertos, con SPM (opacidad y +8·sp), ya con `0:00.0` `0 m`, `Rep 1/5 · 0 m` y el vacío; cuadro 30: `0:01.0` `4 m` con el rodillo; nada aparece de golpe después de la cascada (DV-48) |
| VID-07 | TV · 252,2 s · los dos · `Terminar` → `Terminar` antes del 253 | la salida (0-200 ms) con `0:52.0` `208 m`, `Rep 2/3 · 208 m` y `Rep 1 2:05 680 m` en todos los cuadros, quietos, con la opacidad del HUD; nunca vacíos ni en cero |
| VID-08 | TV · VID-02 con reducir movimiento, **con parciales cerrado y repeticiones abierto al empezar** (SD-14, R-15), y un toque en `500 m` a los 323,8 (cuadro 9), que lo abre | cuadro 9: el alto de 34 a 90 en el acto, y `Series anteriores` corrido 59 dp en el mismo cuadro; el contenido funde en los cuadros 9-14; la flecha cambia de trazo con su fundido; cuadro 45 (325,0): el alto, la lista y la línea `Rep` en su lugar nuevo en ese cuadro; `#1` funde en los cuadros 45-50, sin deslizarse; la marca igual que en VID-02 |
| VID-09 | TV · 196,5 a 199,5 s · cerrados · tocar `500 m` a los 197,3 (cuadro 24) | la cuenta grande aparece a los 197,0 (cuadro 15) y **sigue en su lugar** mientras parciales se abre a 8,4 dp (de 34 a 106, desde y 210,9: el vidrio hasta y 272,3 · 299,2 · 319,0 · 322,6 a los 100 · 200 · 400 · 820 ms); ningún cuadro con un píxel de los dos encimado; `2` en el cuadro 45 y `1` en el 75; la cuenta de la barra sigue |

### 5.5 La regeneración única (spec §7.1; QA-V4b-R2-01; DT5-08)

- **`it4b`**: las tomas con rutina (CAP-01 a -12, -19 a -27, -29 a -31, DEMO-05 a -11, VID-03 a -05) ahora muestran los chips debajo de SPM, cerrados y sin preferencias (los dos si la rutina está en serie y el paso también; si no, solo `500 m`), en los rectángulos de 04-design-notes §9.5 (TV · TH: 228,6-264,5 · 272,9-308,7 con objetivo en SPM, 210,9-246,7 · 255,1-291,0 sin; P360 · L640: 200,3-231,6 · 239,0-270,4 y 184,7-216,1 · 223,5-254,9; W1280: 347,1-387,9 · 397,5-438,3 y 320,2-361,0 · 370,6-411,4; en TH y L640 con objetivo, sin máscara). Se regeneran **una vez**, con dos corridas iguales (A a `<scratch>`, B al repo), antes de las de `it5`. QA explica cada diferencia que no sea esa. Las del libre (CAP-13 a -18, DEMO-02 a -04, VID-01, -02, -06 a -08) tienen que salir **iguales byte a byte** a las de 00-deuda-4b §6 (las de AC-WKT-16-07).
- **`it4a`**: sus tomas del HUD están `superseded: it4b` desde la 4b (DT4b-07) y no se vuelven a tomar; CAP-01 a CAP-11 (reposo y el diálogo de inicio) no tienen paneles y tienen que dar los mismos bytes. La spec dice "las tomas con rutina de `it4a`": no queda ninguna para regenerar (DT5-08, para QA y el PO).
- `it2` e `it3`, iguales byte a byte.

### 5.6 `HudOverlap` y `WrenchAudit` con los paneles (DD5-10; AC-WKT-18-16, AC-BLE-16-02)

- `HudOverlap` suma el rectángulo visual de cada panel (y su área de toque): nunca se cruza con la barra (en su alto mayor, con el sello), ninguna tarjeta, la banda, kcal y BPM, los controles, la llave, el disco de la cuenta grande y `PAUSA`, ni sale de la ventana; la zona del bote con `RespectsBoatZone`; las áreas de toque no se superponen. En `ColumnScroll` cuenta lo **visible**: el rectángulo recortado a la ventana de la columna (lo que queda debajo del piso tiene alfa 0 por la máscara y no responde). La cinta `SIMULADOR` no los toca. El panel del simulador puede taparlos (AC-BLE-16-03): no cuenta.
- `WrenchAudit`: la llave nunca tapa un panel, cerrado o abierto, en los cinco perfiles; corre en toda toma con simulador.

---

## 6. Tests

### 6.1 Suites y comandos

| Suite | Qué | Comando (desde la raíz del repo) |
|---|---|---|
| dotnet | Core (tracker, saneamiento, motor, paneles, geometría, líneas de tiempo, tokens, contraste), App (preferencias, *store*, cambios), Infrastructure (el archivo de preferencias), el oráculo, el simulador | `powershell -NoProfile -File tools/dotnet/run-tests.ps1 -ArtifactsPath <scratch>/it5/<carpeta>` (y `-Matrix -UnityResults …`) |
| Unity EditMode, con y sin simulador | las mismas fuentes; `Tests/UnityEditMode` (el archivo de preferencias con Mono y su carpeta) | `tools/unity/run-unity-tests.ps1 [-Simulator]` |
| Unity PlayMode, con y sin simulador | la UI montada con el remo de prueba, el reloj, el ciclo de vida, las preferencias de prueba | ídem |
| Oráculo del tracker | §2.9 (solo para regenerar los vectores) | `powershell -NoProfile -File tools/oracle/run-oracle.ps1 -Only series_vectors` |
| Arnés | §5 | `tools/unity/capture-snapshots.ps1 -Set it5` (y `-Set it4b` para la regeneración) |
| Builds | simulador Mono a `Builds/Windows/simulator`; normal Mono; IL2CPP y Android NO VERIFICABLE sin los módulos | `tools/build/build.ps1 …` |

Si Unity revienta al cerrar un `-batchmode` que cambió los *defines* (0xC0000005), los *scripts* ya siguen solos (QA-V4b-R2-03); si una corrida igual queda fallida, se repite. **No se tocan fuentes mientras corre Unity** (lección de la deuda §3).

### 6.2 Dobles y semillas (Tests/Support, nunca en un build)

| Doble | Qué |
|---|---|
| `RecordingPreferences` (N) | memoria; anota cada escritura; `FailWrites`; `DeferLoad` (arranca sin leer y se lee con `CompleteLoad()`: el toque antes de la lectura) |
| `RowerScripts` (C) | `R3(k)` (1000 + 3k, split 167), `R4A(k)` (`R-4` hasta `L(250)`, después 2000 + 5(k − 250), split 100), `R4D(k)` (`R-4` sin `L(101)` a `L(103)`: devuelve `null`); `R4S` = `R4` con el índice del tiempo real (abajo) |
| `ScriptedRower` (C) | acepta `Func<int, RowingData?>`: `null` = esa lectura no llega; y un `Func<int>? index` opcional: si está, cada lectura usa `index()` en lugar de `Next++` (`R-4S`) |
| `LifecycleHarness` (C) | `RealReadingIndex`: ⌊crudo + hora de pared extra + 10⁻⁶⌋, el número de la lectura que mandaría un monitor que siguió contando mientras el equipo dormía (§2.3; QA-G5T-06). No cambia nada de lo que ya hace |
| `RoutineSeeds` (C) | `F5`: `Series 600`, `Trabajo 1000`, `Fondo 3000`, `Cinco reps`, `Dos series`, y copias de `Pirámide 500`, `Prueba 4a` y `Corta` (spec §0.4); `Budget5` (§3.6); `Lt59`: una serie ×3 de un trabajo de 1000 m, el `_work` de `test/series_panels_test.dart:8-10`, y su versión sin grupo (LT-61) |
| `SeriesSeeds` (N) | `Largo`: el `HudSeries` de `S-LARGO` (spec §0.4) con los textos de los formatos y del catálogo; `ExtremeSplit`: `S-LARGO` con la repetición anterior `Rep 999` de un trabajo de 86 400 s y 1 m (split `RepSplit(43 200 000)`, más largo que `5999:59`: AC-WKT-18-17, QA-G5T-16) |
| `UiHostOptions` / `UiTestHost` (C) | `Preferences` (por defecto `MemoryPreferences`; `UseTempPreferencesFile()` para el almacén real en una carpeta temporal), `OverrideSeries(HudSeries)`, `TapText(text)` (el centro de la ranura, por `TapAt`), `TapAt(point)` y `DragAt(from, to, frames)` por la selección real (`panel.Pick`, con `PointerDown` / `Move` / `Up` al elegido), `Wheel(point, delta)`, `SeriesLayout`, los paneles por nombre, y las ranuras de `IDrawnTextSource` en `TextOf`, `IsShown`, `Bounds`, `Find`, `IsTextVisible` (coincidencia exacta, alfa > 0,01) |
| `PixelProbe` (C) | busca el rectángulo por nombre de elemento o, si no hay, de ranura (la junta de §3.6) |
| `FrameAllocations` (N) | §3.6: `Start()`, `Sample()` después de cada `yield return null`, `Count`, `Bytes`, `Frames`; y su prueba con el control positivo (`FrameAllocationsTests`) |

### 6.3 Archivos de test

**dotnet + Unity EditMode** (WP-A, `Tests/EditMode/`):

| Archivo | Abrev. | Cubre |
|---|---|---|
| `Workout/SeriesTrackerTests.cs` (N) | STR | LT-47 a LT-58 (§6.4); AC-WKT-07-01, -02, -04, -05, -06 (los cuatro remos de `Trabajo 1000`, con sus segundos de fin y sus `#1`/`#2`), -08; AC-WKT-08-01 a -07 con los casos de la app (`Series 600`, `Cinco reps`, `Dos series`, `Pirámide 500`, `Corta` con `R-4P`, `R-4D` con 154 s y `2:08`), `Fondo 3000` con `R-4A`, `R-3` |
| `Workout/SanitizedDistanceTests.cs` (C) | SAN | `DistanceSample` igual al legacy en LT-55 a LT-58; lo de la 4a sigue |
| `Workout/SeriesEngineTests.cs` (N) | SEN | AC-WKT-07-03 (el orden en el segundo; los segundos 31-180 y 181-200 de la rep 1; nada en pausa, en libre ni después del fin), -07 (la pausa de 20,7 a 50,7, con los **124 m** que no suman; AC-WKT-01-05), -09 (los mismos metros segundo a segundo con `R-4`, `-4R`, `-4Z`, `-4D`, `-4X`; el fin en 250, 251, 252 y 254; AC-WKT-03-03), -10 (segundo plano de 100 a 400 s igual que en primer plano; **con `R-4S`** y una suspensión de 600 s a los 250,4: `0:50.0` `200 m`, a los 251 `0:51.0` `200 m` y a los 252 `0:52.0` `204 m`, con el crudo que cuenta el sueño y con el que no; AC-WKT-02-04, -02-11), -11 (`Begin` al iniciar y al volver a reposo; sesión nueva sin nada de la anterior; los puntos y las filas de la sesión iguales a los de la 4a), AC-WKT-08-08 |
| `Workout/SeriesGoldenTests.cs` (N) | SGD | AC-WKT-07-12 (§2.9: el tracker, la alimentación del motor y la foto `Series`, guion por guion) |
| `Workout/SeriesPanelsTests.cs` (**C**: existe desde la 4a con `formatos`, LT-45, que queda intacto; QA-G5P-04) | SPN | se suman: AC-WKT-18-01 (la visibilidad, DV-48 en `Series 600` y `Cinco reps`, la salida de la serie a los 541), -04 a -08 (qué filas y qué textos en los casos de spec §4.3, el segundo de atraso), LT-61 y la parte de reglas de LT-59 |
| `Workout/HudSeriesChangeTests.cs` (N, App) | HSC | la tabla de §2.5, pieza por pieza (con `Series 600` a los 541: `SeriesEnded` y `LapsBody` `None`, sin cambio de clase en parciales; QA-G5P-03; DD5-18); el primer estado sin cambios |
| `Workout/SeriesPanelPrefsTests.cs` (N, App) | SPP | AC-WKT-18-10 (sin clave, cerrados; con la clave, abiertos; el toque antes de la lectura gana; el de repeticiones conserva su estado; cada toque guarda en el acto), LT-59 y LT-60 (reglas, con el tracker armado como el legacy, §6.4); `LocalSeriesPanelsExpansion` nunca escribe |
| `Preferences/JsonFilePreferencesTests.cs` (N, Infrastructure) | JFP | AC-WKT-18-11: falta, vacío, dañado (no es un objeto) → cerrados sin aviso y el próximo toque guarda uno válido; **una clave de otro tipo → solo esa da false, las demás se leen y se conservan al guardar** (QA-G5T-07); guardar que falla → el valor queda en memoria y no hay aviso; el corte entre el temporal y el reemplazo deja el viejo; las claves desconocidas se conservan; **el texto se arma en el hilo del `Set`** (el ejecutor de prueba recibe bytes) y **dos `Set` seguidos con una escritura en curso dan a lo sumo dos escrituras y la última gana**; el nombre es el mismo para los dos builds; Q-11; todo en carpetas temporales |
| `Workout/WorkoutHudStoreTests.cs` (C) | WHS | `HudSeries` en es y en (AC-WKT-18-22: `500 m splits`… `Rep 2/3 · 200 m`, `1.112 m` también en inglés), la caché (la misma instancia entre segundos; 0 B con `AllocatedBytes` en dotnet entre dos segundos), AC-WKT-11-09 a -11 a la vista |
| `Workout/PresentationIndependenceTests.cs` (C) | PIN | AC-WKT-13-14 con `Series 600`: con y sin *store*, con las preferencias cambiando cada 3 s |
| `Design/SeriesPanelGeometryTests.cs` (N) | SPG | DD5-03: la tabla de §3.2 (04-design-notes §5.1, con y sin objetivo; ± 0,1), `sp` = 1,20 en W1280 y `s` en los demás, la regla de cada perfil, los dos chips enteros en los cinco, los altos naturales (R-01), el reparto de `RowsScroll` con los casos de 04-design-notes §5.2 (DD5-26), las áreas de toque de §3.2 (tope 56, mínimo 48, sin superponerse), `RespectsBoatZone` (AC-WKT-18-14), `BlocksCallout` (AC-WKT-18-15: nunca con 137; sí con 200 en TV y P360), ninguna pieza de la 4b se cruza (AC-WKT-18-16, AC-WKT-13-13), `FitScale` con los casos de 04-design-notes §4.7 y el piso de 0,6 (DV-49), `TitleScale` (es 0,949 ± 0,005; en 1: DD5-20), `BalancedBreak` (`Sin parciales` / `en este paso`, `No splits` / `in this step`, `Sin repeticiones` / `completas`, `No completed` / `reps`: DD5-19), las reglas de alfa por la tinta con la vertical de 04-design-notes §4.6 (en reposo, todo en 1; CAP-16: la línea `Rep` a 0,925 y el vidrio a 0,33 en su borde) |
| `Design/SeriesTimelinesTests.cs` (N) | STL | las tablas de 04-design-notes §6.1 a §6.5 (± 0,01; ± 0,1 base): abrir y cerrar con el resorte, el alto por un dato con el empuje (DD5-16), la cola, la fila nueva en 80-240 (DD5-17), la marca, el cambio por pieza (DD5-18), la serie que empieza y termina, la columna, la rueda, el *hover*; los aires mínimos (≥ 5 base en VID-02, -03 y -04); la inversión a mitad de camino; `corrimiento = min(empuje; G − H_final)` con el resorte corriendo; varias vueltas en un segundo; `ReducedVariants` (04-design-notes §6.8, fila por fila); la cascada con el tramo de SPM |
| `Design/MotionSpecsTests.cs` (C) | MSP | `SeriesOfIterationFive`: las constantes de §4.1 |
| `Design/TokenCatalogTests.cs`, `TypographyUssSyncTests.cs` (C) | TOK | 137 tokens; `num.lap`, `num.row`, `num.xs` |
| `Design/ContrastTests.cs` (C) | CON | `ItFiveDesignerPairs` (C1 a C9) |
| `Localization/CatalogParityTests.cs`, `L10nKeysTests.cs` (C) | CAT | AC-APP-10-12: 223 claves |
| `Project/PureCodeRulesTests.cs` (C) | PCR | los archivos nuevos sin reloj del sistema; **el saneamiento único**: `WorkoutEngine.cs` no contiene `new SanitizedDistance` ni otra fórmula de salto (`MaxSpeedMetersPerSecond`), y llama a `_series.Update` una sola vez |
| `Project/LegacyIsolationTests.cs` (C) | LIS | Q-11: nada de producción nombra las preferencias del legacy |
| `Project/OracleToolingTests.cs` (C) | OTT | `series_tracker.json` con `generator` y `legacyCommit`; el script en solo ASCII de rutas |
| `Traceability/It05Ids.cs` (N), `TraceabilityTests.cs` (C) | ITF | los 74 IDs y LT-47 a LT-61, con sus `UnityOnlyIds` |
| `Workout/WorkoutDeviceChecksTests.cs` (C) | WDC | `[Ignore("NO VERIFICABLE: …")]` con la categoría: el desplazamiento con el dedo, el área táctil y el primer cuadro al volver en Android (AC-WKT-18-13, -09, AC-WKT-02-08 [Device-And]); AC-WKT-02-11 (el usuario) |
| `Tests/Simulator/SeriesSimulatorTests.cs` (N) | SSM (s) | AC-BLE-15-09: `Series 600` con el remo simulado sin ruido y `SimulateDisconnect` en un trabajo: durante el corte la vuelta en curso no suma metros y su tiempo sí; después sigue desde los mismos metros; ninguna vuelta inventada; nada negativo |
| `Tests/Simulator/WrenchPlacementTests.cs` (C) | WPL (s) | AC-BLE-16-02 (la regla): el lugar de la llave libre de los paneles cerrados y abiertos en los cinco perfiles |

**Unity EditMode** (`Tests/UnityEditMode/`, WP-B2): `PreferencesFileTests` (N, PFU: `File.Replace` con Mono deja el viejo o el nuevo, **solo en una carpeta temporal propia**; de `DataLocation.PreferencesFile` compara **solo la cadena** —la carpeta de la base y `prefs.json`—, sin abrir, crear ni escribir ese archivo: QA-G5T-05) y `PreferencesIsolationTests` (N, PIS: con `ROWMATE_DATA_DIR` puesta en una carpeta temporal, el archivo cae adentro). Las rutas de los casos se arman en tiempo de ejecución (regla de repo público).

**PlayMode** (WP-B y WP-D1, `Tests/PlayMode/`; con simulador, `Tests/PlayModeSimulator/`):

| Archivo | Abrev. | Cubre |
|---|---|---|
| `SeriesPanelViewTests.cs` (N, B1) | SPV | LT-59, LT-60 y LT-61 montados (§6.4: el tracker como el legacy, por `ComposeSeries` y `OverrideSeries`, con un archivo de preferencias temporal; `IsTextVisible` exacto y con alfa); AC-WKT-18-02 a -07 en los cinco perfiles (las ranuras y sus rectángulos de 04-design-notes §9.2 ± 0,5 dp, la anatomía de DD5-23, la línea de base del tiempo y de los metros de la vuelta en curso iguales ± 0,25 dp, los colores con sondas, los vacíos en dos renglones parejos, la escala de título por idioma), -17 (`S-LARGO` y `R-4X`: nada cortado ni negativo; las escalas ± 0,01 de lo medido; **`ExtremeSplit`: la escala común de las filas de repeticiones en su piso, 0,6, el split recortado con `…` al final, el rótulo y los metros enteros, y ninguna ranura fuera del vidrio**: QA-G5T-16), -22 (en inglés); las reglas de alfa por la tinta (DD5-13, DD5-14: en reposo todo en 1; en cada cuadro de VID-01 y VID-02, ningún renglón con alfa > 0 con tinta fuera del vidrio ni a menos de 4 base de la línea `Rep` o de la vuelta en curso); el cambio por pieza (DD5-18: en `Dos series` a los 61 s la vuelta en curso no funde; en `Series 600` a los 541 el vacío no se mueve); las escalas comunes con el empuje (DD5-24: una fila nueva que cambia la escala no salta); `ReattachGoesToTheFinalState` (QA-G5T-12); un elemento por panel (sus hijos ocultos no cuentan) |
| `SeriesColumnTests.cs` (N, B1-B2) | SCT | AC-WKT-18-09 **por la selección real** (`TapAt`, `DragAt`: QA-G5T-04): un toque a 3 dp del borde de abajo de `500 m` (fuera de la caja de *layout* del chip en TV) abre `500 m` y no `Reps`; un toque en `Series anteriores` corrido por el FLIP (a mitad de abrir parciales, hasta −59 dp) lo encuentra donde se ve; en TH con la columna desplazada, un toque y un arrastre sobre repeticiones llegan a él, y sobre la parte bajo la máscara no; un arrastre de 20 dp no abre; en pausa sí; con un diálogo, durante la salida y antes de los 520 ms no), -13 (el lugar, la columna, `RowsScroll` en W1280 con el reparto de DD5-26 y `ColumnScroll` en TH y L640, la rueda de a una fila, sin inercia, lo que se ve no salta cuando cambia el contenido: con la rueda abajo, una vuelta nueva no mueve ninguna fila a la vista, DD5-27), -14 (la zona del bote con el contorno), -15 (la cuenta grande y `PAUSA` en su lugar con los paneles abiertos; con la regla forzada a 200, no se muestran: P-04), -16, -20 (pausa, rotación, la rutina terminada detrás del diálogo); AC-WKT-13-12, AC-WKT-14-18; el único escritor de `style.translate` (DT5-14: la cascada, el FLIP y la columna compuestos, sin saltos entre cuadros) |
| `SeriesKeyboardTests.cs` (N, B2) | SKT | AC-WKT-18-12 (el orden del anillo; Enter con `NavigationSubmitEvent` y **Espacio con un `KeyDownEvent` de `KeyCode.Space`**, cada uno una sola vez y una sola escritura: QA-G5T-09; el anillo en dp de ventana con sus radios, DD5-22; el foco que se pierde → `Terminar`; con un diálogo no llegan), AC-APP-05-07 |
| `SeriesMotionTests.cs` (N, D1) | SMT | cada `IMotion` de §4.2 con elementos de prueba: las tablas cuadro a cuadro, `IsFinite` en cada fase (también el *hover*), la inversión, `Instant`, reducir movimiento, `Stop` y 0 B por cuadro del C# de las motions (`GcAllocations`) |
| `HudBudgetTests.cs` (C, B0, B1) | HBT | B0: la medida del peor caso de la 4b (`DescribeVisible`) y M5. B1: AC-WKT-18-21: `AtMostSixtyVisibleElementsInTheFiveWorstCase` en los cinco perfiles (o el tope medido, §3.6); `SeriesNoAllocationsPerFrame` con `FrameAllocations` en cuadros reales (QA-G5T-03); `SeriesWorstCaseWork` (4 vueltas, la línea, 3 repeticiones y una vuelta entrando; abrir; `S-LARGO`: la mediana ≤ 1,0 ms y el peor cuadro ≤ 16 ms, §3.6); lo de la 4b sigue |
| `FrameAllocationsTests.cs` (N, B1) | FAT | el control positivo de `FrameAllocations` (§3.6): un `generateVisualContent` que asigna, ≥ 30 en 30 cuadros; sin asignar, 0; el HUD quieto, la línea de base |
| `ImmersiveTransitionTests.cs` (C, B2) | ITT | AC-WKT-13-06 (los paneles en la cascada con SPM, desde el primer cuadro con su estado y DV-48), -07 (responden desde los 520), -08 (la última foto), -09 (`NothingAnimatesAfterTheExit` con las dos salidas de §4.4); AC-WKT-13-15 y -16 siguen (deuda) |
| `ImmersiveHudTests.cs` (C, B1-B2) | IHT | AC-WKT-13-14 (`PresentationNeverTouchesTheData` con los paneles), AC-WKT-18-08 (el mismo cuadro que el reloj de la banda), AC-WKT-17-06 (las interacciones), AC-WKT-17-01, AC-APP-05-05 (no siguen la letra), AC-APP-05-08 |
| `WorkoutBackgroundUiTests.cs` (C, B2) | WBU | AC-WKT-02-08 (las vueltas y repeticiones del segundo plano en el primer cuadro, sin animar), AC-WKT-18-19 (la parte del segundo plano) |
| `HudLayoutTests.cs` (C, B1) | HLT | AC-WKT-13-13 (cinco perfiles × cerrados y abiertos × pulsómetro × sello × valores largos), AC-WKT-17-07 (rotar con los paneles abiertos), -09, -10 (`R-4X`); la columna a 8·s de SPM en cada cuadro de un cambio de paso con `obj.` |
| `BootSceneTests.cs` (C, B2) | BST | el arranque arma `JsonFilePreferences` en la carpeta temporal; `PreferencesSurviveARestart` (dos arranques); el precalentado con los paneles y sin nada que quede: **sin toques, `prefs.json` no existe al terminar; sembrado, los mismos bytes y la misma fecha** (QA-G5T-05) |
| `FourBDebtTests.cs` (C, B2) | 4BD | `HudWarmupIsNotSeenAndLeavesNothing` con los paneles abiertos del precalentado: 0 escrituras en `RecordingPreferences` y el `SubscriberCount` del `State` de `AppContext.SeriesPanels` igual antes y después (QA-G5T-05) |
| `PlayModeSimulator/SimulatorOverlayTests.cs` (C, B2) | SOT (ps) | AC-BLE-16-02 (la llave no tapa los paneles), -16-03 (el panel del simulador puede taparlos en parte; la parte que se ve responde) |
| `PlayModeSimulator/WorkoutSimulatorTests.cs` (C, B2) | WSM (ps) | AC-BLE-15-09 en pantalla (la vuelta en curso durante `Simular desconexión`) |

### 6.4 Los 15 casos legacy (LT-47 a LT-61)

Convención de ARCHITECTURE §15.3: el método en inglés, el nombre del legacy al carácter en `// legacy: test/…:línea '…'`, `[Category("LT-nn")]` y las de sus AC. Las afirmaciones, una por una y en su orden.

| LT | Legacy | Test | AC |
|---|---|---|---|
| LT-47 | `test/series_tracker_test.dart:46` 'interpola el tiempo de la vuelta entre lecturas' | `STR.InterpolatesTheLapTimeBetweenReadings` | AC-WKT-07-04, -05 |
| LT-48 | `:59` 'dos vueltas dentro del mismo paso' | `STR.TwoLapsWithinTheSameStep` | AC-WKT-07-02, -04 |
| LT-49 | `:65` 'un paso de trabajo de menos de 500 m no genera vueltas' | `STR.AWorkStepUnderFiveHundredMetersHasNoLaps` | AC-WKT-07-02 |
| LT-50 | `:71` 'el enfriamiento no genera vueltas pero suma metros a la repetición' | `STR.CooldownHasNoLapsButAddsToTheRep` | AC-WKT-07-01, -05, AC-WKT-08-01, -02 |
| LT-51 | `:85` 'al cambiar de repetición guarda el resumen de la anterior' | `STR.ChangingRepKeepsTheSummaryOfThePreviousOne` | AC-WKT-08-03, -04, -06 |
| LT-52 | `:102` 'guarda solo las últimas 3 repeticiones, la más reciente primero' | `STR.KeepsOnlyTheLastThreeRepsMostRecentFirst` | AC-WKT-08-04 |
| LT-53 | `:112` 'una serie nueva o un paso suelto vacían la comparación' | `STR.ANewSeriesOrALooseStepClearsTheComparison` | AC-WKT-08-05 |
| LT-54 | `:126` 'una repetición sin metros de trabajo no tiene split' | `STR.ARepWithoutWorkMetersHasNoSplit` | AC-WKT-08-06 |
| LT-55 | `:155` 'una lectura de 0 dentro del paso no genera metros negativos ni dobles' | `STR.AZeroReadingInsideTheStepMakesNoNegativeNorDoubleMeters` | AC-WKT-07-06, AC-WKT-08-04 |
| LT-56 | `:180` 'un primer salto desde 0 no crea vueltas falsas' | `STR.AFirstJumpFromZeroMakesNoFakeLaps` | AC-WKT-07-06, -08 |
| LT-57 | `:191` 'un reinicio del monitor no genera valores negativos' | `STR.AMonitorResetMakesNoNegativeValues` | AC-WKT-07-06, AC-WKT-08-02 |
| LT-58 | `:211` 'rebase no cuenta el salto al reanudar' | `STR.RebaseDoesNotCountTheJumpOnResume` | AC-WKT-07-07 |
| LT-59 | `test/series_panels_test.dart:39` 'arrancan colapsados y al tocar se expanden y se recuerda' | `SPP.StartCollapsedTapExpandsAndRemembers` (dotnet: la regla y la escritura) y `SPV.StartCollapsedTapExpandsAndRemembers` (PlayMode, con el archivo temporal) | AC-WKT-18-02, -03, -06, -07, -09, -10 |
| LT-60 | `:62` 'respeta la preferencia guardada' | `SPP.RespectsTheSavedPreference` y `SPV.RespectsTheSavedPreference` | AC-WKT-18-10 |
| LT-61 | `:68` 'sin serie no aparece el panel de repeticiones' | `SPN.WithoutASeriesThereIsNoRepsPanel` y `SPV.WithoutASeriesThereIsNoRepsPanel` | AC-WKT-18-01 |

El caso `formatos` (`:32`) ya es LT-45 (4a), en `SeriesPanelsTests.cs`, que se conserva (QA-G5P-04).

**Cómo se arma el tracker de LT-59 a LT-61** (QA-G5P-05; `test/series_panels_test.dart:12-22`). Como el legacy, con **una sola lectura**: `var t = new SeriesTracker(); t.Begin(0, 0); t.Update(0, RoutineSeeds.Lt59Work, new StepPosition("g", 1, 3), 300, 60);` (sin grupo para LT-61: `(null, 1, 1)`). Esa lectura **suma**, porque 300 ≤ 10 × máx(60; 1): la vuelta en curso queda en `1:00.0` `300 m` y la línea en `Rep 1/3 · 300 m`. El motor no puede dar un Δt de 60 (y con `R-4` los 300 m llegan a los 75 s, `1:15.0`), así que no se corre el motor: `HudSeries` sale de `WorkoutHudStore.ComposeSeries(t.State, [Lt59Work], [(g, 1, 3)], localizer, session)`, que pasa por `SeriesPanels.For`, y SPV lo monta con `OverrideSeries` sobre el HUD de `RoutineSeeds.Lt59` (nunca un `HudSeries` armado a mano). Las afirmaciones, en el orden del legacy y con `IsTextVisible` exacto y con alfa (§3.1):

| Legacy | SPV |
|---|---|
| `find.text('Parciales 500 m')` y `('Series anteriores')`: nada | `IsTextVisible` falso: los títulos tienen alfa 0 con los paneles cerrados |
| `find.text('500 m')` y `('Reps')`: uno cada uno | verdadero (los rótulos de los chips) |
| `tap(find.text('500 m'))` y `pumpAndSettle` | `TapText("500 m")` por la selección real y `Settle` (el resorte quieto) |
| `Parciales 500 m` y `Rep 1/3 · 300 m`: uno | verdadero; `500 m` ya no (el rótulo del chip abierto tiene alfa 0) |
| `prefs.getBool(lapsKey)`: verdadero | el archivo temporal tiene `immersive.lapsExpanded` en `true` (después de `FlushAsync`) |
| `tap(find.text('Reps'))`; `Sin repeticiones completas`: uno | `TapText("Reps")`; `IsTextVisible("Sin repeticiones completas")` (la ranura tiene el texto entero, aunque se dibuje en dos renglones) |
| LT-60: con la clave en `true`, `Parciales 500 m`: uno | verdadero desde el primer cuadro, sin pasar por cerrado |
| LT-61: `500 m`: uno; `Reps`: nada | verdadero; `#hud-reps-panel` fuera del árbol visible y `Reps` falso |

### 6.5 Matriz AC → test / captura

Marcas: **d** dotnet (y Unity EditMode) · **s** dotnet con simulador · **u** Unity EditMode solo · **p** PlayMode · **ps** PlayMode con simulador · **cap** captura · **eq** equipo · **qa** revisión de QA. Ningún ID queda solo con **eq** o **qa** (`TraceabilityTests`).

| AC | Tests | Capturas |
|---|---|---|
| AC-WKT-07-01 | d STR, SPN | CAP-05, CAP-07 |
| AC-WKT-07-02 | d STR (LT-47 a -49; `Pirámide 500`) | — |
| AC-WKT-07-03 | d SEN | VID-03 |
| AC-WKT-07-04 | d STR (LT-47, -48) | CAP-09 |
| AC-WKT-07-05 | d STR, SEN | CAP-02, CAP-09 |
| AC-WKT-07-06 | d STR (LT-55 a -57), SEN (`R-4R`, `-4Z`, `-4D`) | — |
| AC-WKT-07-07 | d STR (LT-58), SEN | CAP-22 |
| AC-WKT-07-08 | d STR (LT-56), SEN | — |
| AC-WKT-07-09 | d SEN, PCR | — |
| AC-WKT-07-10 | d SEN (con `LifecycleHarness` y `R-4S`, §2.3) | — |
| AC-WKT-07-11 | d SEN, PIN | DEMO-11 |
| AC-WKT-07-12 | d SGD | — |
| AC-WKT-08-01 a -07 | d STR (LT-50 a -54), SPN | CAP-04, CAP-10, CAP-11 |
| AC-WKT-08-08 | d SEN | CAP-04, CAP-11 |
| AC-WKT-18-01 | d SPN (LT-61); p SPV, ITT | CAP-01, CAP-07, CAP-08, VID-06 |
| AC-WKT-18-02 | p SPV (LT-59) | CAP-01, CAP-15 |
| AC-WKT-18-03 | p SPV | CAP-02, CAP-12 |
| AC-WKT-18-04 | d SPN, WHS; p SPV | CAP-02, CAP-08, CAP-09 |
| AC-WKT-18-05 | d SPN; p SPV | CAP-05 |
| AC-WKT-18-06 | d SPN, WHS; p SPV | CAP-02 a CAP-05 |
| AC-WKT-18-07 | d SPN, WHS; p SPV | CAP-04, CAP-10, CAP-11 |
| AC-WKT-18-08 | d SPN, HSC; p IHT, SPV (por pieza) | CAP-06, VID-03, VID-04 |
| AC-WKT-18-09 | d SPP, SPG (las áreas); p SCT (por la selección real), SPV | DEMO-03, DEMO-05 |
| AC-WKT-18-10 | d SPP (LT-59, -60); p SPV, BST; eq (cerrar y abrir el build) | DEMO-11, DEMO-12 |
| AC-WKT-18-11 | d JFP; u PFU, PIS; p BST, 4BD (el precalentado no escribe); qa | — |
| AC-WKT-18-12 | p SKT (Enter y el `KeyDownEvent` de Espacio); eq | CAP-26 |
| AC-WKT-18-13 | d SPG; p SCT; eq (Android, WDC) | CAP-14 a CAP-18 |
| AC-WKT-18-14 | d SPG; p SCT | CAP-03, CAP-14 a CAP-18 |
| AC-WKT-18-15 | d SPG; p SCT | CAP-19 a CAP-23, VID-09 |
| AC-WKT-18-16 | d SPG; p HLT, SCT | CAP-01 a CAP-26 (`HudOverlap`) |
| AC-WKT-18-17 | d SPG (`FitScale`, el piso); p SPV (`S-LARGO`, `R-4X`, `ExtremeSplit`), HLT | CAP-25 |
| AC-WKT-18-18 | d STL; p SMT, SPV (el revelar por la tinta) | VID-01, VID-08 |
| AC-WKT-18-19 | d STL, HSC; p SMT, SPV, WBU | VID-02 a VID-04, VID-08 |
| AC-WKT-18-20 | p SCT, ITT, HLT | CAP-22, VID-07 |
| AC-WKT-18-21 | p HBT (cuadros reales con el repintado), FAT (el control positivo); eq (`[PERF]`) | — |
| AC-WKT-18-22 | d CAT, WHS; p SPV | CAP-12, CAP-13 |
| AC-WKT-13-15 | p ITT (deuda); eq (`[PERF]`) | — |
| AC-WKT-13-16 | p ITT, `4BD.*` (deuda) | — |
| AC-WKT-16-07 | p FBT (deuda) | CAP-13 a CAP-15 de la 4b |
| AC-APP-10-12 | d CAT | — |
| AC-WKT-13-06 | d STL; p ITT | VID-06 |
| AC-WKT-13-08 | p ITT | VID-07 |
| AC-WKT-13-12 | p SCT | CAP-22, CAP-23 |
| AC-WKT-13-13 | d SPG; p HLT | todas (`HudOverlap`) |
| AC-WKT-14-18 | d SPG; p SCT | CAP-19 a CAP-21 |
| AC-WKT-17-06 | p IHT | — |
| AC-WKT-17-09 | d SPG; p SCT | CAP-03, CAP-14 a CAP-18 |
| AC-WKT-02-08 | p WBU; eq (Android, WDC) | — |
| AC-APP-05-07 | p SKT; eq | CAP-26 |
| AC-APP-05-08 | d STL; p SMT, IHT | VID-08 |
| AC-BLE-16-02 | s WPL; ps SOT | todas con simulador (`WrenchAudit`) |
| AC-BLE-16-03 | ps SOT | DEMO-08 |
| AC-HIS-20-04 | d JFP; u PIS; qa | — |
| AC-WKT-01-05 | d SEN | — |
| AC-WKT-02-04 | d SEN | — |
| AC-WKT-02-11 | d SEN; eq (el usuario, WDC) | — |
| AC-WKT-03-03 | d SEN | — |
| AC-WKT-11-09 a -11 | d WHS (y LT-45 de la 4a) | CAP-02, CAP-11 |
| AC-WKT-13-07 | p ITT, SCT | — |
| AC-WKT-13-09 | p ITT | — |
| AC-WKT-13-14 | d PIN; p IHT | — |
| AC-WKT-17-01 | p IHT, SPV (sondas del vidrio) | CAP-02 |
| AC-WKT-17-07 | p HLT | — |
| AC-WKT-17-10 | p HLT, SPV | — |
| AC-APP-05-05 | p IHT | — |
| AC-BLE-15-09 | s SSM; ps WSM | DEMO-08, DEMO-09, VID-05 |

(AC-WKT-08-01 a -07 van en una fila porque comparten pruebas; cada uno tiene su categoría en `It05Ids` y en sus métodos.)

**`It05Ids.UnityOnlyIds`** (los que no tienen **d** ni **s**; `TraceabilityTests` los marca `PENDIENTE UNITY` hasta las corridas de Unity): AC-APP-05-05, AC-APP-05-07, AC-BLE-16-03, AC-WKT-02-08, AC-WKT-13-07, -13-08, -13-09, -13-12, -13-15, -13-16, AC-WKT-16-07, AC-WKT-17-01, -17-06, -17-07, -17-10, AC-WKT-18-02, -18-03, -18-12, -18-20 y -18-21 (20; AC-WKT-18-17 sale de la lista porque SPG prueba `FitScale` y el piso en dotnet).

### 6.6 Conteos esperados (aproximados)

dotnet: +≈ 205 (tracker y saneamiento ≈ 45; motor ≈ 27, con `R-4S`; oráculo ≈ 60 casos parametrizados; paneles y cambios ≈ 30, con la tabla de HSC por pieza; preferencias ≈ 28; geometría ≈ 30, con el reparto, la escala de título y el corte parejo; líneas de tiempo ≈ 35; catálogo, tokens, contraste, reglas y trazabilidad ≈ 15). Unity EditMode: +2 archivos. PlayMode: +≈ 70 sin simulador (con `FrameAllocationsTests` y los casos de la selección) y +≈ 6 con simulador. Arnés `it5`: 47 archivos (26 CAP, 12 DEMO, 9 VID) más `INDEX.md` y `manifest.json`.

### 6.7 Lo que queda sin test automatizado

- **[Device-Win]**: la demo en la ventana real; la preferencia que sobrevive a cerrar y abrir el build (además de BST); el teclado real; `[PERF]` en la entrada (AC-WKT-13-15) y con los paneles (AC-WKT-18-21); `Simular desconexión`.
- **[Device-And]**, NO VERIFICABLE sin el módulo y el teléfono: el desplazamiento con el dedo, el área táctil de los chips, el primer cuadro al volver, el reemplazo atómico del archivo con IL2CPP (R5-04).
- **El usuario**: AC-WKT-02-11 (la demo (c) de 00-deuda-4b §1.5).

---

## 7. Paquetes de trabajo

### 7.0 Orden y reglas

**Antes de WP-A**: este diseño ya integra las notas del Designer (§9) y las decisiones del gate (spec §11.3); WP-A construye con ellas desde el principio (QA-G5T-01). Las comprobaciones de QA-G5T-06 y -07 (el remo `R-4S`, las preferencias) están en §2.3 y §2.7.

1. **WP-A** (dotnet, sin Unity): todo §2 y §4.1, las constantes de §3.2 y §4.1 (con §6.9 de las notas), los dobles (`R-4S`, `Lt59`, `ExtremeSplit`), las pruebas de dotnet de §6.3, el oráculo (§2.9) y el ADR 021. Termina con `run-tests.ps1` en verde y la matriz con los IDs de Unity en `PENDIENTE UNITY`.
2. **WP-B0** (Unity, después de WP-A y **después de que QA verifique la deuda de la 4b**, spec §5.1): (a) el build de 00-deuda-4b §2.4 si el usuario cerró su `RowMate.exe` (la medida de QA-V4b-06 que quedó NO VERIFICABLE; si sigue abierto, se mide al cierre con las tres entradas y los paneles: riesgo aceptado por el PO, QA-G5T-14); (b) la medida del peor caso real de la 4b (`DescribeVisible`) y **M5**; (c) **la sonda S-B5** (abajo) y las decisiones de §3.6 (cuál junta, en B1) y de §3.6 / DT5-18 (cuál contador de asignaciones); (d) las cuatro suites de Unity en verde y `capture-snapshots.ps1 -Set it4b -Out <scratch>/…` con 0 comprobaciones y 0 sondas que fallan y **los mismos bytes** que las versionadas (M5 no cambia lo que se ve). El informe va al final de este documento ("Cambios por implementación de WP-B").
3. **WP-D1** (después de WP-A **y de WP-B0, en serie**; QA-G5T-13): `SeriesPanelMotion`, `SeriesColumnMotion`, las piezas de la cascada y `SeriesMotionTests` sobre elementos de prueba. Los dos paquetes necesitan Unity y hay una sola instancia por proyecto; además no se tocan fuentes mientras corre una suite (el incidente de 00-deuda-4b §3). D1 no edita ni abre Unity mientras corre algo de B0, y al revés.
4. **WP-B1** (después de D1): **primero** `DrawnText` y la junta del presupuesto elegida, con `it4b` igual byte a byte (§3.6); después `TabularCells`, `SeriesPanelView`, `SeriesColumn`, `HudView`, `HudZones`, `CenterCallouts`, `Base.uss`, `FrameAllocations` y las PlayMode de B1.
5. **WP-B2**: la página (el anillo, `SetInteractive`, las piezas y la salida por `SeriesColumn`), el precalentado (con `LocalSeriesPanelsExpansion`), `DataLocation`, `AppRoot` (las preferencias y la medida), `FrameTimeProbe`, las pruebas de segundo plano, de teclado, del arranque, de la deuda y del simulador.
6. **WP-B3**: el arnés `it5` con sus dos corridas, la regeneración única de `it4b` (§5.5), los builds (simulador y normal Mono; IL2CPP NO VERIFICABLE) y, al cerrar, ARCHITECTURE (§2, §7.8, §9.4, y §20.2: el tope medido si cambió, la medición de 0 B en cuadros reales y que `TabularCells` dibuja números adentro de otro elemento, DT5-13) y el ADR 021 al día.
7. **WP-D2** (Designer, 04-design-notes §12): las tomas contra 04-design-notes §9.2 y §9.3, las sondas S5-01 a S5-11 en la tabla de `SeriesCatalog`, VID-01 a VID-09 cuadro a cuadro (el revelar por la tinta en VID-01, -02, -03 y -08), y las biblias (ART_BIBLE §3.2, §3.3, §3.4, §4.3, §5.2, §10; ANIMATION_BIBLE §2.3, §2.10, §2.11; "Cambios al iniciar la It. 5" y "Cambios de la It. 5").

**Reglas** (las de la 4b): cada archivo tiene un solo dueño (§7.6 las excepciones); una sola instancia de Unity por proyecto (si está tomado, se espera y se reintenta; nunca se mata un Unity que no lanzó el paquete, tampoco el Editor del usuario); **los paquetes de Unity van en serie**; ningún paquete hace *commits*; no se tocan fuentes mientras corre Unity; los builds solo a su carpeta por defecto, sin cerrar el `RowMate.exe` del usuario (reintento cada 2 minutos hasta 20; si sigue, NO VERIFICABLE); nada escribe rutas de la PC en lo versionado (tampoco en casos de prueba: se arman en tiempo de ejecución).

**La sonda S-B5** (WP-B0, en el proyecto real, con una prueba temporal que se borra con su `.meta`; la evidencia va al informe):

| Qué | Si no da |
|---|---|
| En **un** `generateVisualContent`: `DrawText` con cuatro `FontAsset` (Inter 600, Inter 400, Numeric SC 700 y la fuente de íconos) con alfa por corrida, `Painter2D` (el vidrio redondeado, la flecha, la marca, el anillo) y `Allocate` con color por vértice (las franjas de la máscara de `ColumnScroll`) | la máscara con `Painter2D` en franjas de alfa decreciente (más vértices, sin elemento) |
| **La selección con puntos fuera de la caja de *layout*** (QA-G5T-04), por `panel.Pick` y con eventos de puntero y de rueda dentro de un HUD con `pickingMode: Ignore`: (1) un punto dentro del hijo oculto `.rm-hud-series-panel__hit` y fuera de la caja del chip (TV: a 3 y a 10 dp debajo de los 35,8 del chip, dentro de los 56) llega al panel y a su `ContainsPoint`; (2) un panel corrido por `style.translate` (−59 dp, el FLIP de VID-01) se elige donde se ve y no donde está su *layout*; (3) en TH, un panel desplazado con `translate` por la columna se elige donde se ve, y un punto de su caja que queda fuera de la ventana de la columna no lo elige; (4) el foco con `NavigationSubmitEvent` y un `KeyDownEvent` de Espacio | un elemento transparente de toque por panel, que `SeriesColumn` mueve con el mismo `translate` y dimensiona en los mismos momentos que el hijo oculto (+2: cambia §3.6; con M5 y la junta, 60-61) |
| La medida de una corrida con `MeasureTextSize` de un *holder* oculto contra el ancho que dibuja `DrawText` (± 0,5 dp), y la corrida con *tracking* por avances | las corridas medidas con los avances y los pares de *kerning* del `FontAsset` |
| El dibujo de 4 filas, la vuelta en curso y la línea con ≈ 80 llamadas a `DrawText` por repintado, en ≤ 1 ms en el Editor, y **sus asignaciones en 30 cuadros reales** con `FrameAllocations` (QA-G5T-03): el control positivo (un `byte[64]` por repintado: ≥ 30), el mismo sin asignar (0), la línea de base del HUD quieto, y los dos contadores (`GC.Alloc` en el hilo principal y "GC Allocated In Frame") para elegir el que vea el control en *batchmode* | el tiempo: las filas que no cambian en una malla guardada (se repinta solo lo que cambia); las asignaciones: R5-11 |
| El sello en un elemento (título Inter 700 y detalle Inter 600 por corridas) o `Próximo:` y su tipo en uno (M3), con la medida y la escala de ajuste de la 4b, y **las consultas por nombre como ranuras** (`TextOf`, `IsShown`, `Bounds` y `PixelProbe` de `hud-seal-text` o de `hud-up-next-type`) | la otra de las dos; si ninguna, el tope medido (§3.6) |

### 7.1 WP-A — reglas puras y pruebas de dotnet (≈ 45 archivos)

Core: `Workout/{SeriesTracker,SeriesPanels}.cs` (N), `SanitizedDistance.cs`, `WorkoutEngine.cs`, `WorkoutSnapshot.cs` (C); `Ports/IPreferences.cs` (N); `Common/MemoryPreferences.cs` (N); `Design/{SeriesPanelGeometry,SeriesTimelines}.cs` (N), `HudTimelines.cs`, `MotionSpecs.cs` (C); `Design/Tokens.cs` y `Presentation/Styles/{Tokens,Typography}.uss` (C / G, excepción de §7.6); `Localization/L10nKeys.cs`, `strings.es.json`, `strings.en.json` (C). App: `Workout/{SeriesPanelPrefs,SeriesPanelsExpansion,HudSeriesChange}.cs` (N), `WorkoutHudState.cs`, `WorkoutHudStore.cs` (C); `Composition/{AppServices,AppComposer,AppContext}.cs` (C). Infrastructure: `Preferences/{JsonFilePreferences,PreferencesFiles,IPreferencesFileOps}.cs` (N). Tests/Support: `RecordingPreferences.cs`, `SeriesSeeds.cs` (N), `RowerScripts.cs`, `RoutineSeeds.cs`, `ScriptedRower.cs`, `LifecycleHarness.cs` (C). Tests: los de dotnet de §6.3 (`Tests/EditMode/Workout/SeriesPanelsTests.cs` **C**, con `formatos` intacto) y `Tests/Simulator/*`. `tools/oracle/bin/series_vectors.dart` (N), `Tests/Golden/oracle/series_tracker.json` (G). `docs/migration/architecture/adr/021-preferencias.md` (N).

### 7.2 WP-B — UI, composición, arnés y pruebas de Unity (≈ 40 archivos)

| Entrega | Archivos |
|---|---|
| B0 | `Presentation/Workout/StepInfoBar.cs` (solo M5); `Presentation/Styles/Base.uss` (la regla de M5, si va por USS); `Tests/PlayMode/HudBudgetTests.cs` (la medida); la prueba temporal de S-B5 (se borra con su `.meta`); el informe. **La junta no va en B0** (necesita `DrawnText`: QA-G5T-10) |
| B1 | `Presentation/Controls/{DrawnText,TabularCells}.cs` (N); la junta: `Presentation/Workout/SealRow.cs` (a) o la parte de `Próximo` de `StepInfoBar.cs` (b) (C); `Presentation/Workout/{SeriesPanelView,SeriesColumn}.cs` (N); `HudView.cs`, `HudZones.cs`, `CenterCallouts.cs` (C); `Presentation/Styles/Base.uss` (C); `Tests/UiSupport/{UiTestHost,UiHostOptions,UiQuery,PixelProbe}.cs` (C), `FrameAllocations.cs` (N); `Tests/PlayMode/{SeriesPanelViewTests,SeriesColumnTests,FrameAllocationsTests}.cs` (N), `HudLayoutTests.cs`, `HudBudgetTests.cs`, `ImmersiveHudTests.cs` (C; en (b), sus líneas 116-117 a la ranura). **Se verifican sin cambiar** (las consultas por nombre, que resuelven ranuras): `Snapshots/HudExrCatalog.cs`, `Snapshots/WorkoutCatalog.cs`, `Tests/PlayMode/{StepBarTests,ImmersiveTransitionTests}.cs`; si alguno necesitara un cambio, se lista en el informe de B1 con el motivo |
| B2 | `Presentation/Workout/{ImmersiveWorkoutPage,HudWarmup}.cs` (C); `Bootstrap/{DataLocation,AppRoot}.cs` (C); `SimulatorPresentation/FrameTimeProbe.cs` (C); `Tests/PlayMode/SeriesKeyboardTests.cs` (N), `ImmersiveTransitionTests.cs`, `WorkoutBackgroundUiTests.cs`, `BootSceneTests.cs`, `FourBDebtTests.cs` (C); `Tests/PlayModeSimulator/{SimulatorOverlayTests,WorkoutSimulatorTests}.cs` (C); `Tests/UnityEditMode/{PreferencesFileTests,PreferencesIsolationTests}.cs` (N) |
| B3 | `Snapshots/SeriesCatalog.cs` (N); `Snapshots/{SnapshotCatalog,SnapshotCli,SnapshotRunner,ScenarioScript,SnapshotTakeRule,WrenchAudit}.cs` (C); `Editor/Build/BuildOutputInspector.cs` (C); `tools/unity/{capture-snapshots.ps1,README.md}` (C); `docs/…/05-series-parciales/snapshots/*` y la regeneración de `docs/…/04b-hud-exr/snapshots/*` (G); al cerrar, `docs/migration/architecture/ARCHITECTURE.md` |

### 7.3 WP-D — motion (D1) y pulido del Designer (D2)

| Entrega | Archivos |
|---|---|
| D1 | `Presentation/Motion/Hud/{SeriesPanelMotion,SeriesColumnMotion}.cs` (N); `Presentation/Motion/Hud/ImmersiveCascadeMotion.cs` (C: las dos piezas en su arreglo, nada más); `Tests/PlayMode/SeriesMotionTests.cs` (N); los valores por cuadro de VID-01 a -09 los graba B3 con estos `IMotion` |
| D2 | `docs/…/05-series-parciales/04-design-notes.md`; las sondas de aspecto de `SeriesCatalog` (sus valores, en la tabla que le deja B3; excepción de §7.6); `docs/migration/design/{ART_BIBLE,ANIMATION_BIBLE}.md` ("Cambios al iniciar la It. 5"). `Skin.uss` y `Motion.uss` no cambian: el panel no tiene aspecto en USS (§7.5) |

### 7.4 El contrato de motion (WP-D1 ↔ WP-B)

La tabla de §4.2. Además:

- **El panel no le pide nada al *layout* durante una animación**: B llama a la motion con el alto final y la motion devuelve el alto visual; B repinta (`MarkDirtyRepaint`) en cada *callback* y no toca `style.height`.
- **`style.translate` y `style.opacity` de cada panel tienen un solo escritor: `SeriesColumn`** (DT5-14; QA-G5T-04). Compone la cascada y la salida (que la página le pasa), el *callback* de `SeriesColumnMotion` (la serie que aparece o se va, la columna que sigue a SPM, el desplazamiento de `ColumnScroll`) y el FLIP (con el `G` de parciales). Ninguna motion escribe estilos: devuelven *structs*. Lo propio del panel (el vidrio, los renglones, sus alfas, el desplazamiento de las filas en `RowsScroll`) es dibujo. El hijo oculto del área de toque lo dimensiona el panel (`SeriesPanelView`) al empezar y al terminar cada animación del alto, nunca por cuadro (DT5-15).
- **La regla de toma** (B3) lee `IsFinite` de las dos motions por fase (§5.1) y, para el resorte del alto, `ISettlingMotion.RemainingDp` (< 0,15 dp no bloquea).
- **`Instant` y reducir movimiento** se leen en cada llamada (la lección de QA-V4b-MOT-03): si reducir movimiento se prende con un resorte en curso, va a su final en el acto.

### 7.5 El contrato USS (WP-B en `Base.uss` o por código; WP-D2 no lo cambia)

Las reglas de las It. 2 a 4b: sin colores literales; nadie define tamaño ni fuente en un elemento con `.rm-type-*` (en el HUD el tamaño va en línea por código); lo que va en línea por código manda; las clases de estado las pone el código.

| Elemento | Contrato | AC |
|---|---|---|
| `.rm-hud-series-panel` (`#hud-laps-panel`, `#hud-reps-panel`; `--laps`, `--reps`) | `position: absolute`; posición, tamaño, escala `sp` y `transform-origin` por código; **sin `background`, `border`, `padding`, `overflow` ni transición en USS** (todo lo dibuja el código con los tokens); `pickingMode` y `focusable` por código; `display` por código (el de repeticiones) | AC-WKT-18-02 a -07, -13 |
| Los *holders* (`.rm-hud-series-panel__font`) | hijos absolutos de 0 × 0, `visibility: hidden`, con su clase de tipo (`.rm-type-label`, `.rm-type-body`, `.rm-type-num-*`) y la de la fuente de íconos; el tamaño en línea por código | — |
| El hijo de toque (`.rm-hud-series-panel__hit`) | absoluto, `visibility: hidden`, `pickingMode: Ignore`, sin fondo; posición y tamaño por código (DT5-15) | AC-WKT-18-09 |
| `.rm-hud-step-row` (M5) | sale el espaciador `.rm-flex-spacer` de la fila; `margin-left: auto` en el primero del grupo de la derecha que esté en el árbol (`#hud-up-next`, o `#hud-countdown` sin `Próximo`), puesto por código cuando `Próximo` entra o sale (con `Próximo`, la cuenta conserva su aire de 8 o 6). Lo que se ve no cambia: `it4b` igual byte a byte | AC-WKT-14-04 |

**Lo que crea WP-B para WP-D2:** las clases de arriba. **Lo que es de WP-D2:** ningún estilo de los paneles (es dibujo); los valores de color, alfa y tamaño son tokens y constantes (§2.10, §3.2), que cambia por la excepción de §7.6.

### 7.6 Excepciones de dueño

| Archivo | Dueño | Excepción |
|---|---|---|
| `Core/Design/Tokens.cs`, `Presentation/Styles/{Tokens,Typography}.uss` (G) | el Designer | **WP-A suma solo** `num.lap`, `num.row` y `num.xs` (136 → 137) con `tools/tokens` y `--check` en 0, porque los paneles los necesitan antes de WP-D2 (DD5-08) |
| `Core/Design/{MotionSpecs,SeriesPanelGeometry}.cs` y sus tests | WP-A | si una revisión del Designer cambia **un número** (con el visto bueno del PO cuando contradice la spec), el paquete que siga cambia **solo esa constante y su valor en el test**, y lo anota. **No alcanza para un cambio de estructura** (una curva por otra, una fase o un campo nuevo de un *struct*, otro reparto, otra regla de alfa): eso se escribe primero en este diseño, con su prueba, como se hizo con las notas de la 5 (§9; QA-G5T-01) |
| `Tests/EditMode/Design/ContrastTests.cs` | WP-A | WP-A suma `ItFiveDesignerPairs` con los valores de 00-deuda-diseno §3.7 (sin cambios en 04-design-notes §7); si WP-D2 los revisa, cambia solo ese grupo |
| `Presentation/Motion/Hud/ImmersiveCascadeMotion.cs` | WP-D1 (desde la 4b) | solo las dos piezas nuevas en su arreglo |
| `Presentation/Workout/StepInfoBar.cs`, `SealRow.cs` | WP-B (desde la 4b) | WP-B0 solo M5 y WP-B1 solo la junta del presupuesto, sin cambiar lo que se ve (`it4b` igual byte a byte y sus sondas) |
| `Snapshots/SeriesCatalog.cs` | WP-B3 | WP-D2 completa los valores de las sondas de aspecto (S5-01 a S5-11) en su tabla |
| `Tests/Golden/oracle/series_tracker.json` | WP-A | lo genera el script; nadie lo edita a mano |
| `docs/migration/design/*` | WP-D2 | — |

---

## 8. Riesgos y desvíos

### 8.1 Desvíos propuestos (técnicos o de presentación; ninguno cambia un dato)

| ID | Qué | Por qué | Decide |
|---|---|---|---|
| DT5-01 | **Se construye con la base del Designer** (00-deuda-diseno §2 a §5, revisada por 04-design-notes §2): 137 base fijo cerrado y abierto, el chip de 34 con relleno 10 × 8, 8·s debajo de SPM, la cuenta grande y `PAUSA` siempre en su lugar, `sp` 1,20 en W1280 con las filas desplazables, `ColumnScroll` en TH y L640, los tamaños y colores de 04-design-notes §3 | Es lo que cumple las garantías de AC-WKT-18-13 a -16 en los cinco perfiles con el peor contenido. Todo es constante; la regla de la cuenta grande con los paneles existe y está probada para los dos casos (§3.4) | **Decidido** en el gate (spec §11.3, SD-01 a SD-19); DV-50 y DV-51 van al usuario |
| DT5-02 | Los paneles **no recortan**: cada renglón se dibuja entero, con un alfa **por su tinta** contra el vidrio (regla 1), contra la ventana de filas (regla 2) y, solo en `ColumnScroll`, contra la máscara de la columna (regla 3); el *layout* va a su estado final en el cuadro del cambio y la animación es dibujo | Un elemento por panel (R4a-03) sin máscaras (ARCHITECTURE §20.2) y sin *layout* por cuadro (ANIMATION_BIBLE §0.1 principio 5). La fórmula anterior (12·sp contra la caja del renglón) dejaba el último renglón al 67 % en reposo y tinta fuera del vidrio (DN5-01) | **Revisado** por el Designer (04-design-notes §5.3, DD5-13 a DD5-15); lo ve en VID-01, -02, -03 y -08 (WP-D2) |
| DT5-03 | La vuelta nueva lleva **la marca** ámbar de 3·sp a la izquierda en lugar del fondo ámbar al 20 % (AC-WKT-18-19) | Con el fondo, el valor ámbar baja de 7,127 a 4,857 de contraste (00-deuda-diseno §3.7) | **Aceptado** (spec §11.3, con SD-09) |
| DT5-04 | **CAP-11 a los 881,2 s** (`0:03.0` `12 m`, `Rep 2/4 · 12 m`) y **DEMO-11 a los 31,6 s** | A los 880,2 la marca de `Rep 1` (entró a los 879) sigue apagándose, y a los 31,2 el panel de repeticiones está apareciendo: las dos son fases finitas y la regla de toma no espera (§5.1). Lo que tienen que mostrar no cambia | **Aceptado** (spec §7.1, §7.3, §7.4) |
| DT5-05 | `prefs.json` va en **la carpeta de la base** (la privada; en Android la interna), no en `persistentDataPath`; se lee en el arranque y se guarda fuera del hilo principal (el texto se arma en el hilo principal; el ejecutor solo escribe bytes) | ARCHITECTURE §9.1 ya descartó `persistentDataPath` en Android (es la externa); así lo comparten los dos builds y `ROWMATE_DATA_DIR` aísla las pruebas sin código nuevo. Guardar en el hilo principal podría costar un tirón en el teléfono en el cuadro del toque | — (ARCHITECTURE §9.4 se precisa al cerrar) |
| DT5-06 | El paso por distancia lee `SeriesTracker.Meters`: el motor no tiene otro saneamiento | Spec §12 punto 2 ("no con dos copias que puedan divergir") | — |
| DT5-07 | El oráculo arma las entradas del tracker con el modelo de la spec en Dart y la app verifica que su motor dé exactamente esas entradas (§2.9) | El legacy cierra los pasos por distancia con la distancia cruda y la app con la saneada (DV-26): correr el `WorkoutProvider` daría otra secuencia de pasos con `R-4R`, `R-4Z` y `R-4D` | QA (confirmado en los dos gates) |
| DT5-08 | La "regeneración única de las tomas con rutina de `it4a`" no tiene nada que regenerar: están `superseded: it4b` desde la 4b | DT4b-07 | **Aceptado** (spec §7.1) |
| DT5-09 | El presupuesto: M5 en WP-B0 (ya ≤ 60) y una junta más en WP-B1 (el sello o `Próximo` en un elemento dibujado, con los nombres de hoy como ranuras) para dejar margen; si no, el tope 61 con la medición | §3.6 | El informe de WP-B0, antes de B1 |
| DT5-10 | En W1280 la subida de la cascada de los paneles es 8·sp (9,6 dp) y no 8·s (12,8) | Los paneles van con su escala `sp`; ajustarlo pediría otro *translate* por pieza | **Aceptado** por el Designer (DN5-03) y en la spec (AC-WKT-13-06) |
| DT5-11 | El anillo de foco de los paneles lo dibuja el panel (sigue su alto visual), en dp de ventana | El USS de foco usaría la caja del *layout*, que en la animación es la final | — (el Designer lo confirma, DD5-22) |
| DT5-12 | `HudPiece.SeriesLaps` y `SeriesReps` con el tramo de SPM; los paneles responden desde los 520 ms | AC-WKT-13-07 ("desde que empiezan a aparecer, como los controles") | — |
| DT5-13 | Las celdas tabulares de los paneles (`TabularCells`) repiten la lógica de `TabularNumber` en lugar de compartirla | `TabularNumber` está verificado en la 4a y la 4b; refactorizarlo ahora es riesgo sin beneficio visible. Se unifican cuando se toque `TabularNumber` por otro motivo. ARCHITECTURE §20.2 ("un `TabularNumber` por número") se precisa al cerrar | — (deuda) |
| DT5-14 | **Un solo escritor de `style.translate` y `style.opacity` de los paneles** (`SeriesColumn`): la cascada, la salida, el FLIP, la serie que aparece, la columna que sigue a SPM y el desplazamiento de `ColumnScroll`, compuestos; la página ya no escribe esos estilos de los paneles | La selección de UI Toolkit sigue la caja transformada, no lo dibujado (QA-G5T-04): un FLIP o un desplazamiento dibujados dejarían el panel sin toque donde se ve. El diseño anterior decía a la vez que el FLIP era un `translate` y que el `translate` era solo de la cascada | — (QA lo verifica en S-B5 y SCT) |
| DT5-15 | **El área de toque fuera de la caja**: un hijo oculto `.rm-hud-series-panel__hit` agranda la caja del panel hasta el área de toque y lo que dibuja en la animación en curso (ajustado al empezar y al terminar cada animación, nunca por cuadro); `ContainsPoint` decide | `Panel.PerformPick` descarta un elemento si el punto cae fuera de su `worldBoundingBox` antes de llamar a `ContainsPoint` (QA-G5T-04); el chip de 35,8 dp tiene un área de 56. El hijo está oculto: no cuenta para el presupuesto | — (S-B5; plan B: +2 elementos) |
| DT5-16 | **El piso de las escalas de números**, `SeriesNumberMinScale` = 0,6, y por debajo `…` al final de la pieza | AC-WKT-18-17 pide que lo que pasa de `S-LARGO` se recorte "en el extremo" y la regla necesita un extremo definido; `S-LARGO` llega a 0,638, así que nunca se recorta (QA-G5T-16) | Designer (DN5-06) |
| DT5-17 | **M5 en WP-B0; la junta en WP-B1** | La junta usa `DrawnText`, que nace en B1; en B0 obligaría a adelantar la maquinaria de texto dibujado y las consultas por ranura (QA-G5T-10). Con M5 solo ya se llega a ≤ 60 | — |
| DT5-18 | **0 B por cuadro medido en cuadros reales**, con el repintado adentro (`FrameAllocations`) y un control positivo permanente (`FrameAllocationsTests`) | `GcAllocations.During(() => H.Step())` no ve el `generateVisualContent`, que en la 5 es casi todo el trabajo de cada cuadro de cada animación (QA-G5T-03) | QA (el contador lo elige S-B5) |

### 8.2 La spec y el Designer: lo que decidió el gate

La tabla de 00-deuda-diseno §8.1 y de 04-design-notes §11.1, con los cinco puntos que encontró QA (QA-G5P-02, QA-G5T-02), **ya decidida por el PO** (spec §11.3): **se adopta la columna "Designer" en SD-01 a SD-19**, y lo que se ve distinto del legacy va al usuario como DV-50 (el ancho, el lugar, la zona del bote, la cuenta grande) y DV-51 (los tamaños y la disposición de adentro). Si el usuario rechaza DV-50 o DV-51, cambian solo las constantes indicadas (R5-07; la regla de §3.4 esconde la cuenta grande y `PAUSA` donde corresponda).

| # | Criterio | Spec antes del gate | Queda (Designer) | Dónde |
|---|---|---|---|---|
| SD-01 | AC-WKT-18-02 | chip de relleno 14 × 10, mínimo 140 | 137 × 34, relleno 10 × 8 | `SeriesPanelGeometry.PanelWidth`, `ChipHeight`, `PadH`, `PadV` |
| SD-02 | AC-WKT-18-03 | abierto, 200 fijo | 137 fijo (× `sp`) | `PanelWidth` |
| SD-03 | AC-WKT-18-04 | `num.lap` 34, metros al 55 %, `#n` 15/600 al 60 % en 52, `num.row` 24 | `num.lap` 28, metros 13 `.hud @80 %`, `#n` 12/600 `text.secondary` en una columna del ancho de `#99`, `num.row` 20 | tokens (§2.10) y constantes del panel |
| SD-04 | AC-WKT-18-07 | metros 16/700, columna de 52 | `num.xs` 13/700, columna del `Rep n` más ancho, una escala común | ídem |
| SD-05 | AC-WKT-18-13 | 10·s debajo de SPM; en L640 el segundo chip se desplaza | 8·s; los dos chips entran en los cinco; `sp` 1,20 en W1280 con las filas desplazables | `ColumnGap`, `MinViewHeight`, `MinScale`, `SeriesFitRule` |
| SD-06 | AC-WKT-18-14, P-03 | en vertical abiertos hasta 225,6 en TV; en L640 angostarlo | abiertos en vertical solo hasta 159,1 (la franja de afuera); en horizontal nunca, sin angostar | `RespectsBoatZone` |
| SD-07 | AC-WKT-18-15, P-04, AC-WKT-13-12, AC-WKT-14-18, demo (b) | en vertical, con un panel abierto, la cuenta grande y `PAUSA` no se muestran | se muestran siempre en su lugar | `BlocksCallout` (da `false` con 137) |
| SD-08 | AC-WKT-18-18 | alto y ancho; el contenido entra o se va en 160 demorado 80 | solo el alto; entra en 80-240 y se va en 0-120 | `MotionSpecs.Series*` |
| SD-09 | AC-WKT-18-19, AC-APP-05-08 | la fila crece de 0 en 200 desde −8 dp con fondo ámbar 20 % que se apaga en 1,2 s | la lista baja en 0-160, la fila entra en **80-240** desde −8·sp, la marca se apaga en 600-1400 | `SeriesTimelines.RowsChange`, `.Marker` (DT5-03) |
| SD-10 | AC-WKT-18-21 | el tope de 60 o uno nuevo | un elemento por panel; M5 y una junta | §3.6 (DT5-09) |
| SD-11 | spec §7.1 | el toque al menos 0,6 s antes | 0,82 s, o el resorte quieto | `SeriesSettleTapMs` |
| SD-12 | AC-WKT-18-19, VID-02 a VID-04 | el alto con el resorte; todo funde | el alto por un dato con el empuje; cambia solo lo que cambia, por pieza | `SeriesTimelines.DataResize`, `.Swap`; `HudSeriesChange` |
| SD-13 | AC-WKT-18-05, -07, CAP-05, CAP-13 | sin decir dónde se cortan los vacíos | dos renglones parejos | `SeriesPanelGeometry.BalancedBreak` |
| SD-14 | VID-08 | con los dos abiertos | parciales cerrado al empezar | `SeriesCatalog` |
| SD-15 | AC-WKT-18-03 | el título con el estilo del chip | 11/600, sin espaciado, en el color pleno, a una escala por idioma | `SeriesPanelGeometry.TitleScale` |
| SD-16 | AC-WKT-18-04 | los metros de la vuelta en curso "al lado" | pegados a x 127, con al menos 6 de aire | `ContentRight`, `HeroMetersGap` |
| SD-17 | AC-WKT-18-02 | el rótulo del chip al 80 % | en el color pleno | la constante de alfa del rótulo (1) |
| SD-18 | AC-WKT-18-02 | la flecha a 6 del texto | pegada al borde derecho, con vidrio vacío en el medio | la anatomía (DD5-23) |
| SD-19 | AC-WKT-18-04, -07 | 2 entre filas (el legacy, 2 + 2) | 2 entre filas; el aire de 4 debajo de la vuelta en curso, solo con vueltas | `RowGap`, `RowsGap` |

La corrección de "+4 la primera" (la primera vuelta suma **24** base: 4 de aire + 20 de fila) la confirmó el Designer (R-01; DN5-04).

### 8.3 Riesgos

| ID | Riesgo | Mitigación |
|---|---|---|
| R5-01 | Dibujar todo en un elemento con cuatro `FontAsset`, `Painter2D` y `Allocate` con color por vértice en UI Toolkit 6000.3 (en este uso, sin verificar) | La sonda S-B5 primero, con un plan B por fila (§7.0); la 4b ya probó `DrawText` con otros `FontAsset`, `Painter2D` y `Allocate` en el mismo elemento (04b B-01) |
| R5-02 | Los anchos y escalas del Designer salen de fontTools con las tablas K2, no de Unity (como R4b-09) | WP-B1 los mide en Unity; las comprobaciones de CAP-02, -11, -12, -13 y -25 usan lo medido ± 0,01 y el informe anota cada diferencia |
| R5-03 | El presupuesto queda en el tope o se pasa (R4a-03) | La medida real primero (B0), M5, una junta más (B1), y el tope medido como salida (§3.6). La 6a necesita 1 más si corta la fila de píldoras (R4b-02) |
| R5-04 | El reemplazo atómico de `prefs.json` con IL2CPP en Android | `File.Replace` / `File.Move` sobre `rename(2)`; NO VERIFICABLE sin el módulo; si falla, el próximo arranque lee el viejo (nunca uno a medias) |
| R5-05 | La regla de toma con las motions nuevas (las tomas a +0,2 s de un segundo con rodillos, la marca, el *hover* de los toques del arnés) | La lista de bloqueos de §5.1 y dos corridas iguales; los instantes que caían en una fase finita ya se movieron (DT5-04) |
| R5-06 | El modelo de tiempo del oráculo en Dart no coincide con el motor | Lo detecta `TheEngineFeedsTheOracleInputs` (§2.9); el modelo reproduce los números de la spec (`<scratch>/it5/arch/sim.py`), y QA lo confirmó con dos puertos propios |
| R5-07 | El usuario rechaza DV-50 o DV-51 (200, 10·s, los tamaños del legacy) | Constantes (§8.2) y la regla de la cuenta grande ya probada para los dos casos |
| R5-08 | El toque contra el arrastre con el dedo (8 dp) y la rueda en la PC | PlayMode con eventos sintéticos por la selección real; el teléfono es NO VERIFICABLE hasta la 6a |
| R5-09 | El repintado de los paneles cada cuadro mientras animan, junto con la fila de píldoras en sus últimos 5 s | Repintan solo con un dato o mientras animan; HBT mide el trabajo del HUD (la mediana ≤ 1 ms y el peor ≤ 16 ms); `[PERF]` en el build (§3.7) |
| R5-10 | El build de la PC sigue tomado por el `RowMate.exe` del usuario | La regla de reintento; esa parte NO VERIFICABLE si sigue (QA-G5T-14: riesgo aceptado, se mide al cierre) |
| R5-11 | `MeshGenerationContext.DrawText` asigna memoria por llamada (lo de adentro de TextCore), y el repintado de los paneles en régimen (los rodillos de cada segundo con un panel abierto) deja de dar 0 B | S-B5 lo mide en cuadros reales con su control positivo. Si asigna: (a) las cifras que ruedan pasan a un dibujo propio que no llame a `DrawText` por cuadro (los *quads* de los glifos del `FontAsset`, armados al cambiar el texto y copiados con `Allocate`), si el material de texto de UI Toolkit lo permite; si no, (b) va al informe de B0 con lo medido, como desvío de AC-WKT-18-21 para el PO y QA, y se escribe en ARCHITECTURE §20.2 |
| R5-12 | El hijo oculto no agranda la caja de selección (`worldBoundingBox` podría no contar un hijo oculto) o el `translate` compuesto deja de coincidir con lo dibujado en algún cuadro | S-B5 lo prueba con los tres casos de QA-G5T-04; el plan B es un elemento transparente de toque por panel (+2, §3.6); SCT toca siempre por `panel.Pick` |

### 8.4 Pedidos al Designer

DN5-01 a DN5-05 están respondidos en 04-design-notes §1 (el revelar por la tinta, el vidrio sin reflejo, la subida de 9,6 dp aceptada, "+24 la primera" y las biblias en WP-D2). Quedan:

| ID | Qué |
|---|---|
| DN5-06 | DT5-16: confirmar el piso de 0,6 para las escalas de números y que lo recortado con `…` sea la pieza que no entra (el split, en una fila de repeticiones); revisar `ExtremeSplit` en WP-D2 |
| DN5-07 | QA-G5T-11 (del Designer, integrado acá): el *hover* y el presionado son una fase de `SeriesPanelMotion` (`Hover`, finita, con `Stop` y sin *hover* en un panel inerte). Falta que el Designer confirme qué hace con reducir movimiento: este diseño deja **el mismo fundido** (es opacidad, como la marca) |

### 8.5 Para el PO, QA y el coordinador

| Para | Qué |
|---|---|
| PO | Nada pendiente del gate: SD-01 a SD-19, DT5-03, DT5-04, DT5-08 y DT5-10 están en la spec (§11.3), y `R-4S` en §0.4. DT5-16 (el piso de 0,6) es una precisión de AC-WKT-18-17 que no cambia lo que se ve con `S-LARGO` |
| QA | DT5-02 (las tres reglas por la tinta), DT5-07 (las entradas del oráculo), DT5-09 y DT5-17 (el presupuesto), DT5-14 y DT5-15 (la selección: S-B5 y SCT), DT5-18 (la medida de 0 B y su control). El archivo de preferencias del usuario: comparar su contenido y su fecha (o que no exista) antes y después de todo (AC-HIS-20-04) |
| Coordinador | DV-50 y DV-51 al usuario (spec §11.1). Si el `RowMate.exe` del usuario sigue abierto en `Builds/Windows/simulator`, los builds esperan (§7.0) y la medida de AC-WKT-13-15 va al cierre (QA-G5T-14). Para la demo (c) del cierre, los pasos de 00-deuda-4b §1.5. Para regenerar el oráculo del tracker hace falta el Dart de `<flutter-sdk>` (los vectores versionados alcanzan para las pruebas) |

---

## 9. Cambios por notas del Designer

[04-design-notes](04-design-notes.md) se escribió después de la primera versión de este diseño. Sus cambios a la base (R-01 a R-15) y sus pedidos (DD5-13 a DD5-27) se integran **acá, antes de WP-A** (QA-G5T-01): ninguno cambia un dato del producto, ninguno contradice la spec corregida (spec §11.3 los decidió como SD-08, SD-09, SD-12 a SD-19) y el contrato USS (§7.5) no cambia. **Varios son de estructura** (DD5-16, DD5-18, DD5-26: un campo nuevo, una curva en lugar del resorte, otro reparto), así que no entran en la excepción de §7.6, que solo cubre cambiar un número: van escritos en las secciones de abajo y WP-A construye con ellos desde el principio.

| Nota | Qué cambia en este diseño | Dónde |
|---|---|---|
| R-01 / DN5-04 | La primera vuelta suma 24 base; los totales ya estaban bien | §3.2 (SPG) |
| R-02, R-03 / DD5-13, -14, -15 | El α_borde de 12·sp sale. Tres reglas por la tinta: revelar (4), la ventana de filas (4, siempre, con la cola), la máscara de la columna (12, solo `ColumnScroll`, el vidrio por vértice) | §3.1, §3.2 (constantes), §8.1 DT5-02 |
| R-04 / DD5-16 | El alto por un dato con el empuje (`DataResize`: 0-160 / 80-240, `ease.move`); `TailShiftBase`; el resorte solo para abrir y cerrar; `min(empuje; G − H_final)` | §4.1, §4.2 |
| R-05 / DD5-17 | `SeriesRowInStartMs` 80 | §4.1 |
| R-06 / DD5-18 | El cambio de contenido por pieza: `HudSeriesChange` con `LapsBody`, `ClearedLaps`, `RepsBody`; `ContentSwap` sale | §2.5, §4.1, §4.2 |
| R-07 / DD5-19 | Los vacíos en dos renglones parejos (`BalancedBreak`) | §3.1, §3.2, §5.2 (CAP-05, -13) |
| R-08 / DD5-20 | Una escala de título por idioma (`TitleScale`) | §3.1, §3.2, §5.2 (CAP-02, -12) |
| R-09 / DD5-21 | El vidrio sin reflejo | §3.1 |
| R-10 / DD5-22 | El anillo en dp de ventana, radios 14·sp + 4 y 14·sp + 8 | §3.1, §3.5, §5.2 (CAP-26) |
| R-11 / DD5-23 | La anatomía como constantes (la flecha dibujada, el ícono, el rótulo, los metros en x 127, la columna del rótulo, la marca, la vertical de cada renglón) | §3.1, §3.2 |
| R-12, R-13 / DD5-25 | La columna sin objetivo: TV desde 210,9; TH y L640 con 93,9 base; las comprobaciones de todas las tomas con los rectángulos de 04-design-notes §9.2, §9.3 y §9.5 | §3.2, §5.1, §5.2, §5.3, §5.5 |
| R-14 | El área de toque con tope 56 (48 como mínimo) | §3.2 |
| R-15 | VID-08 con parciales cerrado al empezar | §5.4 |
| DD5-24 | Las escalas comunes de las filas de vueltas y de repeticiones, que cambian con el empuje (`PushProgress`) | §3.1, §4.2 |
| DD5-26 | El reparto de `RowsScroll` con el mínimo de repeticiones reservado | §3.2 |
| DD5-27 | La fila que entra con la vista desplazada suma 22·sp·k al desplazamiento en el mismo cuadro | §3.2, §4.2 (`ShiftScroll`) |
| §4.9, §6.9 | El *hover* y el presionado (90 / 160, 0,04 / 0,08) como fase de `SeriesPanelMotion`; las constantes nuevas (`SeriesDataGrowMs`, `SeriesDataShrink*`, `SeriesHover*`, `SeriesReducedChevron*`, `SeriesChevronStroke`, `SeriesRevealFade`, `SeriesRowsEdgeFade`) | §4.1, §4.2, §3.2 |
| §6.8 | La tabla de reducir movimiento revisada (la flecha con dos trazos; el alto por un dato a los 0 o a los 120) | §4.3 |
| §9.1, §9.6 | Ninguna toma en los 1400 ms de una marca; el desplazamiento en 0; las sondas S5-01 a S5-11 | §5.1 |
| §9.4 | Los cuadros clave de VID-01 a VID-09 | §5.4 |

**De 00-deuda-diseno** (DD5-01 a DD5-12): DD5-01 y DD5-02 (el modo compacto de la barra libre) ya están saldados con la deuda de la 4b (00-deuda-4b); DD5-03 a DD5-12 ya estaban integrados, con las revisiones de arriba (DD5-04: sin "contenido recortado", por la tinta; DD5-06 y DD5-07: con las tablas de 04-design-notes §6; DD5-11: 0 B medido en cuadros reales, §3.6).

**Lo que no se integra**: nada. El único pedido que el Designer dejó con dos salidas (QA-G5T-11, el *hover*) se integra como fase de la motion, y queda para el Designer la variante de reducir movimiento (DN5-07).

---

## Verificación hecha

1. **Legacy, solo lectura** (`3ae0f3a`): los archivos del encabezado y, para el gate, otra vez `lib/features/workout/series_tracker.dart:84-145` (`begin`, `rebase`, `update`: la lectura única de LT-59 suma porque 300 ≤ 10 × máx(60; 1)) y `test/series_panels_test.dart` entero (el `_trackerAt300m`, los `find.text` exactos y `Sin repeticiones completas`). `git -C ../rowmate-legacy status --porcelain` vacío al empezar y al terminar; no se leyó `lib/core/strava/strava_secrets.dart`.
2. **Los números de la spec**, con el puerto del tracker y el modelo de tiempo de §0.4 en `<scratch>/it5/arch/sim.py` (Python, sin red): todos los que usa este diseño coinciden (ver el encabezado). QA los confirmó con dos puertos propios (88 de 88 y 30 de 30). Los de `R-4S` salen de `R-4` (`L(250)` = 2000, `L(851)` = 4404, `L(852)` = 4408) y coinciden con spec AC-WKT-07-10.
3. **El código del repo** (para el gate): `Tests/EditMode/Workout/SeriesPanelsTests.cs` existe con `formatos` (LT-45); `Presentation/Controls/RmButton.cs` traduce `KeyCode.Space` a `NavigationSubmitEvent` a mano; `Tests/Support/ScriptedRower.cs` manda con un *timer* del programador y `LifecycleHarness` tiene el crudo y la hora de pared extra; `Tests/UiSupport/GcAllocations.cs` mide una acción sincrónica y `HudBudgetTests.MeasureAllocations` mide 20 `H.Step()` sin cuadros reales; `UiTestHost.TapAt` ya elige con `panel.Pick`; la página escribe `style.translate` / `style.opacity` de cada pieza en `ApplyPiece` y en la salida; `ObservableValue` tiene `SubscriberCount`; los nombres `hud-seal-*` y `hud-up-next*` se consultan en `SealRow`, `StepInfoBar`, `HudExrCatalog`, `WorkoutCatalog`, `HudLayoutTests`, `HudBudgetTests`, `ImmersiveHudTests`, `ImmersiveTransitionTests` y `StepBarTests`; el espaciador de la fila es `.rm-flex-spacer`.
4. **Es un documento**: no se escribió código, no se corrió dotnet y no se abrió Unity. Sin *commits*.

---

## Cambios por gate QA

Correcciones por [03-qa-harmony-paridad](03-qa-harmony-paridad.md) y [03-qa-harmony-tecnica](03-qa-harmony-tecnica.md), alineadas con la spec corregida (SD-01 a SD-19, `R-4S`, AC-WKT-07-07 y -18-11) y con [04-design-notes](04-design-notes.md) (§9). Verificado contra el legacy (`3ae0f3a`, solo lectura: `series_tracker.dart:84-145`, `test/series_panels_test.dart`) y contra el código del repo (Verificación hecha, punto 3). No se disputa ningún issue.

| Issue | Qué cambió | Dónde |
|---|---|---|
| **QA-G5T-01** (mayor) | Las notas del Designer, integradas antes de WP-A: el revelar por la tinta y las tres reglas (sale el α_borde de 12·sp), el alto por un dato con el empuje y `TailShiftBase`, la fila nueva en 80-240, el cambio por pieza, el vidrio sin reflejo, el anillo con sus radios, los vacíos parejos, la escala de título por idioma, la anatomía como constantes, el reparto de `RowsScroll`, la fila que entra con la vista desplazada, el *hover* como fase, la tabla de reducir movimiento, y las comprobaciones de todas las tomas con la geometría real (CAP-01 desde 210,9; TH y L640 con 93,9; CAP-05, -12, -13, -15 a -17, -21, -23; VID-01 a -09, con VID-08 que arranca con parciales cerrado). §7.6 dice que su excepción no alcanza para cambios de estructura | §0, §1.1, §2.5, §3.1, §3.2, §3.3, §3.5, §4.1 a §4.3, §5.1 a §5.5, §6.3, §7.6, **§9** |
| **QA-G5T-03** (mayor) | 0 B por cuadro en **cuadros reales**, con el repintado: `FrameAllocations` (`ProfilerRecorder` de `GC.Alloc` en el hilo principal o "GC Allocated In Frame", 30 cuadros de cada animación) con un control positivo permanente (`FrameAllocationsTests`) y la línea de base; sumado a S-B5 (80 `DrawText`, 0 asignaciones); `HBT.SeriesWorstCaseWork` con la mediana ≤ 1,0 ms (§20.2) además del peor cuadro ≤ 16 ms; R5-11 si `DrawText` asigna | §3.6, §4.5, §6.2, §6.3, §7.0 (S-B5), §8.1 DT5-18, §8.3 |
| **QA-G5T-04** (mayor) | El FLIP y el desplazamiento de la columna son `style.translate`, con **un solo escritor** (`SeriesColumn`) que compone la cascada, la salida, el FLIP, la serie que aparece, la columna que sigue a SPM y el desplazamiento; la página le pasa la cascada y la salida. El área de toque fuera de la caja, con un hijo oculto que agranda la caja (no cuenta en el presupuesto) y `ContainsPoint` recortado a la ventana de la columna. S-B5 prueba los tres casos con puntos fuera de la caja; SCT toca por `panel.Pick`. Plan B: +2 elementos | §0, §1.1, §3.1, §3.3, §6.3 (SCT), §7.0, §7.4, §7.5, §8.1 DT5-14 y DT5-15, §8.3 R5-12 |
| **QA-G5T-05** (mayor) | `ISeriesPanelsExpansion` con dos implementaciones; el precalentado usa un `LocalSeriesPanelsExpansion(true, true)` que no pasa por las preferencias ni se suscribe al `State` de la app. `4BD` afirma 0 escrituras y el mismo `SubscriberCount`; `BST`, que `prefs.json` no existe sin toques o queda con los mismos bytes y fecha. PFU solo compara la cadena de la ruta y escribe en una carpeta temporal propia | §1.1, §2.7, §6.3, §7.2 (B2: `FourBDebtTests`) |
| QA-G5T-06 | `R-4S`: `LifecycleHarness.RealReadingIndex` (crudo + hora de pared extra) y `ScriptedRower` con un índice inyectable. SEN con `Series 600` y la suspensión de 600 s a los 250,4: a los 251 `0:51.0` `200 m` (`L(851)` = 4404, un salto de 2404 m que no suma), a los 252 `0:52.0` `204 m`, con los dos casos del crudo. El PO ya sumó `R-4S` a §0.4 | §1.1, §2.3, §6.2, §6.3, §6.5 |
| QA-G5T-07 | `JsonFilePreferences`: una clave de otro tipo da `false` solo para ella y las demás se conservan; vacío solo si el archivo no se puede leer o no es un objeto; el texto se arma en el hilo principal en cada `Set` y el ejecutor solo escribe bytes, juntando las pendientes (la última gana); `JObject` / `JToken`, sin deserialización genérica (RA-05). JFP lo prueba | §2.7, §6.3 |
| QA-G5T-09 | Espacio llega como `KeyDownEvent` de `KeyCode.Space` y va por el camino de Enter, como `RmButton`, una sola vez por tecla; SKT lo prueba con un `KeyDownEvent` de verdad | §3.5, §6.3, §6.5 |
| QA-G5T-10 | La junta pasa a WP-B1 (necesita `DrawnText`); los nombres de hoy quedan como elemento y ranura, y las consultas por nombre (`TextOf`, `IsShown`, `Bounds`, `Find`, `PixelProbe`) resuelven ranuras; la lista de archivos que consultan esos nombres, para verificar o adaptar (en (b), `ImmersiveHudTests.cs:116-117`); `it4b` igual byte a byte con la junta, antes de los paneles | §1.1, §3.6, §6.2, §7.0, §7.2, §8.1 DT5-17 |
| QA-G5T-12 | `AttachToPanelEvent` después de un `DetachFromPanelEvent`: las motions a su estado final, volver a medir, recalcular tintas, escalas y cortes, ajustar el hijo de toque y repintar; SPV `ReattachGoesToTheFinalState` | §3.3, §6.3 |
| QA-G5T-13 | B0 y D1 **en serie** (una sola instancia de Unity y no tocar fuentes mientras corre una suite); los paquetes de Unity van en serie | Estado, §7.0 |
| QA-G5T-15 | 00-deuda-4b §0 y §1.1 generalizados: sin el nombre ni el contenido de la base de desarrollo, sin la historia de sueño del equipo ni el detalle de su registro de eventos | `00-deuda-4b.md` §0, §1.1 |
| QA-G5T-16 | El piso de las escalas de números (`SeriesNumberMinScale` 0,6; `S-LARGO` llega a 0,638) y el `…` al final de la pieza que no entra; SPV con `ExtremeSplit` (un split de 86 400 s y 1 m de trabajo): la escala en su piso, el split con `…`, el rótulo y los metros enteros, nada fuera del panel | §3.1, §3.2, §6.2, §6.3, §8.1 DT5-16, §8.4 DN5-06 |
| QA-G5P-03 | HSC a los 541 de `Series 600`: `SeriesEnded` y `LapsBody` `None` (descanso → enfriamiento: el vacío no se mueve), sin `ContentSwap`, que deja de existir; la tabla de HSC por pieza (DD5-18) | §2.5, §6.3 |
| QA-G5P-04 | `Tests/EditMode/Workout/SeriesPanelsTests.cs` pasa a **C**: se suman los casos de SPN y `formatos` (LT-45) queda intacto | §6.3, §6.4, §7.1 |
| QA-G5P-05 | LT-59 a LT-61 con el tracker como el legacy (`Begin(0)` y una sola `Update` de 300 m a los 60 s, que suma), por `WorkoutHudStore.ComposeSeries` (pública y pura) y `SeriesPanels.For`, montado con `OverrideSeries`; `IsTextVisible` con coincidencia exacta y alfa (el título cerrado y el rótulo del chip abierto no se ven), y la ranura del vacío con el texto entero | §2.5, §3.1, §6.2 (`Lt59`), §6.4 |

**Además, por la spec corregida y por las notas** (sin issue propio): lo remado en pausa que no suma son 124 m, no 120 (QA-G5P-06), en §2.3 y SEN; el guardado que falla conserva el valor en la misma ejecución (QA-G5P-07, QA-G5T-08) en §2.7; DT5-01, -03, -04, -08 y -10 pasan a decididos (§8.1); §8.2 pasa de "para el gate" a "decidido", con SD-12 a SD-19; QA-G5T-14 (la medida de AC-WKT-13-15 al cierre) en §7.0 y R5-10; M5 con `.rm-flex-spacer` (el nombre real del espaciador) y el margen automático en el primero del grupo de la derecha que esté en el árbol, para que sin `Próximo` la cuenta siga pegada a la derecha (§3.6, §7.5); `It05Ids.UnityOnlyIds` escritos (§6.5); ARCHITECTURE §20.2 al cerrar con `TabularCells` y la medida de 0 B en cuadros reales (§7.0).

**Lo que este gate no verificó** (sin Unity, como pedía la tarea): la selección real de UI Toolkit 6000.3 con el hijo oculto y el `translate` (S-B5), lo que asigna `DrawText` por repintado (S-B5, R5-11), qué contador de asignaciones ve el control positivo en *batchmode* (S-B5) y las medidas de Unity de los anchos y las escalas (R5-02).

---

## Cambios por implementación de WP-A

WP-A quedó implementado con los archivos de §7.1 (Core, App, Infrastructure, los dobles de `Tests/Support`, las pruebas de dotnet de §6.3 y `Tests/Simulator`, el oráculo `tools/oracle/bin/series_vectors.dart` con `Tests/Golden/oracle/series_tracker.json` y el ADR [021](../../architecture/adr/021-preferencias.md)), sin abrir Unity. Precisiones, sin cambiar el QUÉ:

| # | Qué | Por qué |
|---|---|---|
| A5-01 | **`JsonFilePreferences` usa el lector JSON del Core** (`JsonParser` / `JsonNode`) y un escritor mínimo propio (`JsonText`), no Newtonsoft `JObject` (§2.7) | `RowMate.Infrastructure` **no** referencia Newtonsoft (`overrideReferences` sin la dll; tampoco el `csproj` de dotnet): la premisa de §1.1 no se cumplía y sumarla era una referencia nueva (§1.2). El lector del Core ya distingue los tipos, conserva el orden de las claves y el texto exacto de los números; sin deserialización genérica (RA-05). Cada clave guarda su texto JSON (lo que se escribe) y su nodo (lo que se lee) |
| A5-02 | **El formato de `series_tracker.json`**: una fila por llamada al tracker `[op, raw, t, cm, cs, rm, x]` (`op` = `"b"`, `"r"` o el índice del paso; `cs` el `double` de Dart, entero si lo es; `x` lo que cambió del resto del estado: `w`, `L`, `s`, `P`). Con R-4P y R-4X (la distancia quieta) los pasos por distancia no terminan: esos guiones cortan a los 120 s (Series 600), 60 (Cinco reps) y 660 (Pirámide 500); `until` y `end` lo dicen | El tope de 1 MiB por archivo: con todos los guiones de la spec (30 671 filas) quedó en 865 KB. Las tres comparaciones de §2.9 usan 1e-9 para los segundos y exactos los enteros |
| A5-03 | **Los guiones**: los 40 de spec AC-WKT-07-12 (5 rutinas × 8 remos), `Trabajo 1000` con R-4, R-3, R-4R, R-4Z y R-4D, `Fondo 3000` con R-4A, cinco con pausas (la de AC-WKT-07-07, una de 1 s, una que cruza el cambio de paso por distancia, una con el paso por tiempo que cierra después de la pausa y dos seguidas con R-4D) y `Paso cero` (calentamiento 3 s; serie ×2 [trabajo 0 s; trabajo 100 m; descanso 0 s]; enfriamiento 3 s), que vive en `RoutineSeeds.PasoCero` y no está en F-5. Los ids de serie de F-5 salen de su fecha (`RoutineSeeds.F5Day`, el 2026-10-29); las copias conservan los de la original | `SGD.TheOracleRoutinesAreTheSeeds` fija que las plantillas del guion sean las de `RoutineSeeds`; `TheEngineFeedsTheOracleInputs` (las 50 corridas del motor) da exactamente las entradas del guion |
| A5-04 | **`SeriesTracker.Trace`** (interno, nulo en la app): un delegado con cada llamada (`SeriesCall`: Begin, Rebase o Update con el paso, la posición, la lectura y el reloj) | Para `TheEngineFeedsTheOracleInputs` (la alimentación del motor) y SEN (el orden en el segundo). Sin costo en la app; `Tests.EditMode` lo ve por `InternalsVisibleTo` |
| A5-05 | **`SeriesTimelines`**: `Swap(t, changesClass, reduced)` recibe un `bool` (Core no ve `SeriesBodyChange`, que es de App: §2.5); `PanelToggleStart` para la inversión (el alto con su velocidad y cada fundido desde su valor, con la parte de su duración que le falta; la demora de 80 ms del cierre, solo si arranca quieto); `RepLine` (la línea `Rep` que entra o sale con la serie); `FadeRiseVisual`, `SpringValue`, `TailShift`, `FollowGlass` y los finales de cada fase (`*EndMs`). La flecha: sin reducir movimiento, el trazo ∨ girado; con reducir movimiento, los dos trazos con sus alfas | El contrato de §4.2 (D1 compone `SeriesPanelVisual` con esto) sin estructura nueva en la vista |
| A5-06 | **STL**: el alto con ± 0,15 base (las tablas de 04-design-notes son por cuadro de 30 fps, n/30 s, y las líneas de tiempo van en ms enteros: a los 133 y no a los 133,3); el aire mínimo de VID-02 da ≈ 5,2 base y no 5,48 | El Designer usó la curva exacta de USS (ease-in-out-cubic); `Tokens.EaseMove` es su aproximación cubic-bezier para C#. Sigue por encima de 5 en todos los cuadros (la garantía) |
| A5-07 | **`SeriesPanelGeometry`** suma lo que necesitan el dibujo y las pruebas: `SeriesColumnLayout.HudScale`, `Width`, `Right`, `Top(bool)`, `RoomBase(bool)`; la vertical de cada renglón (`SeriesRowKind`, `LapRowTop`, `RepRowTop`, `EmptyLineTop`, `RepLineTop`, `Baseline`, `InkTop` / `InkBottom` y `*At` con la escala); las tres reglas de alfa (`RevealAlpha`, `RowsEdgeAlpha`, `ColumnMaskAlpha`); las partes fijas y las filas de cada panel (`LapsFixedHeight`, `RepsRowsHeight`…). El área de toque cerrada se agranda lo mismo hacia cada lado y lo que un lado no puede lo toma el otro; en `RowsScroll` cada panel conserva al menos su parte fija | Las tablas de §3.2 y 04-design-notes §5.1, §5.2 y §5.5 dan con ± 0,1 (L640 `Reps` 52,7 con SPM con objetivo; 56 sin él) |
| A5-08 | `WorkoutHudStore.ComposeSeries` devuelve `HudSeries?` (null sin rutina); la caché es interna (`SeriesFor`) y reusa la lista de filas y cada fila del mismo paso. `HudSeriesChange`: `NewLaps` por la diferencia de números de la vuelta más reciente (con cambio de paso, las del paso nuevo); con la serie que empieza o termina, `RepsBody` es `None` (el panel entero aparece o se va) | La tabla de §2.5, pieza por pieza (HSC) |
| A5-09 | `SeriesPanelPrefs(IPreferences, IAppLogger? = null)` con `SubscriberCount`; `LocalSeriesPanelsExpansion(bool, bool, IAppLogger? = null)`; `AppContext.Preferences` (las de `AppServices` o `MemoryPreferences`) | El `ObservableValue` necesita un registro; 4BD mide `SubscriberCount` |
| A5-10 | Los anchos de palabra de `SPG.TheEmptyTextsBreakIntoTwoEvenLines` son de prueba (con los totales de 04-design-notes §4.8); los mide Unity (R5-02, WP-B1) | `BalancedBreak` recibe medidas |
| A5-11 | `TraceabilityTests.It05UnityOnlyIdsHaveAUnityLocation` se ignora hasta que aparezca una prueba de Unity de un AC-WKT-18-* (las de la deuda de la 4b, AC-WKT-13-15 y AC-WKT-16-07, ya existen y no abren la puerta). **AC-WKT-13-16 no tiene todavía su categoría en `Tests/PlayMode`** (00-deuda-4b la prueba con `4BD.*`): WP-B0 o B2 se la ponen. Las comprobaciones de la 4a y la 4b ("sin resultado de dotnet") cuentan como re-verificados los d/s de la It. 5 (AC-WKT-17-09, -13-13…) | Si no, las pruebas de la deuda harían exigir ya los 20 IDs de Unity |
| A5-12 | `tools/oracle/README.md` suma la fila de `series_vectors.dart`; `Tests/EditMode/Workout/WorkoutHarness.cs` acepta lecturas que no llegan (`Func<int, RowingData?>`) | Fuera de la lista de §7.1: de documentación y de prueba |
| **A5-13** | **Para WP-B2 (y el Designer): con el diálogo de completado abierto, `WrenchPlacement` lleva la llave al carril izquierdo (x 16-56; TH y 270-310, L640 y 231-271), que se cruza con la columna de los paneles**; `HudLayout.LeftLaneObstacles` no los incluye (no existían en la 4b). Si la página sumara la columna como obstáculo, en TH no quedaría lugar en el carril. WPL prueba el lugar del HUD sin diálogo (los cinco perfiles, con y sin objetivo, cerrados y abiertos: libre); el caso del diálogo queda para B2 | AC-BLE-16-02 ("tampoco tapa los paneles"); `WrenchAudit` lo va a ver en B3 |

**Corridas** (desde la raíz del repo; `<scratch>/it5/wp-a`):

| Suite | Resultado |
|---|---|
| `tools/dotnet/run-tests.ps1 -Matrix` | compilación con 0 avisos y 0 errores (también `RowMate.Tests.UnityApiCheck`: las fuentes de prueba con `netstandard2.1` y NUnit 3.5); simulador 188 / 188; Core 2532 / 2497 pasan / 0 fallan / 35 ignoradas (las 27 de antes, las 7 de equipo de WDC y `It05UnityOnlyIdsHaveAUnityLocation`); matriz: PASA 531, FALLA 0, IGNORADO 2, PENDIENTE UNITY 125, PENDIENTE WP-B 0, SIN TEST 0 |
| `tools/oracle/run-oracle.ps1 -Only series_vectors` | `series_tracker.json`: 865 673 bytes; la guardia del legacy OK antes y después |
| `RowMate.Tokens --check` | `Tokens.uss` y `Typography.uss` al día (137 tokens) |
| Mutación de control | sin la interpolación del cruce (`crossTime = t`) fallan tres de SGD y dos de STR; restaurado |

Unity no se abrió (la tarea es sin Unity): los archivos nuevos no tienen `.meta` todavía (los crea el primer import, en WP-B0). `git -C ../rowmate-legacy status --porcelain` vacío al empezar y al terminar; `lib/core/strava/strava_secrets.dart` no se leyó. Sin *commits*.

---

## Cambios por implementación de WP-D1

WP-D1 quedó implementado con los archivos de §7.3: `Presentation/Motion/Hud/{SeriesPanelMotion,SeriesColumnMotion}.cs` (N), `Presentation/Motion/Hud/ImmersiveCascadeMotion.cs` (C: las dos piezas en su arreglo, nada más) y `Tests/PlayMode/SeriesMotionTests.cs` (N, SMT: 24 casos). El legacy no anima los paneles (abre y cierra en el acto: es la mejora EXR de AC-WKT-18-18 y -19), así que no hay nada que portar de su motion; lo único que se conserva es que nada de esto toca el dato. Precisiones para WP-B1 a WP-B3, sin cambiar el QUÉ:

| # | Qué | Por qué |
|---|---|---|
| D1-01 | `SeriesPanelMotion(MotionDriver, SeriesPanelKind kind, Action<SeriesPanelVisual> apply)`: suma el tipo de panel | `ContentChanged` lee de `HudSeriesChange` las piezas de su panel: parciales, `NewLaps`, `LapDropped`, `ClearedLaps`, `LapsBody` y la serie (la línea `Rep`); repeticiones, `NewRep`, `RepDropped` y `RepsBody` |
| D1-02 | Lecturas de más: `Scale` (`sp`, solo para medir el resorte en dp; la pone la vista con la geometría), `Expanded`, `TargetHeightBase`, `HeightRemainingDp`, `IsTimedPhaseRunning`. `ISettlingMotion.RemainingDp` = el resorte del alto en dp si es lo único que corre, e **infinito** si además corre una fase de duración fija. `SeriesColumnMotion`: `Scale`, `ScrollTargetBase`, `RepsLeaving` | Con una sola motion por panel, la regla de toma de la 4b (que mira `ISettlingMotion` antes que `HudMotion`) dejaría pasar una toma con una marca o un fundido en curso si el resorte estuviera a menos de 0,15 dp; con el infinito bloquea sin cambios, y B3 puede dar un mensaje fino con `HeightRemainingDp` e `IsTimedPhaseRunning` |
| D1-03 | **Un dato con el resorte corriendo** (§4.1, 04-design-notes §6.2): las filas y la cola van con `máx(mín(empuje; G − H_final); el corrimiento de partida)`; en un achique, la cola va con su curva | El `mín` de §4.1 sin la cota salta \|G − H_anterior\| en el cuadro del dato durante una apertura (la mutación de control lo midió: 15,7 base a los 150 ms de abrir 34 → 68) y sube la cola y las filas sobre la vuelta en curso. Con la cota quedan donde estaban hasta que el vidrio las alcanza, después lo siguen y nunca van más rápido que el empuje: es la regla del Designer ("nunca más rápido que el vidrio") sin el salto. **Para WP-D2**: verlo en un video con un toque menos de 0,6 s antes de una vuelta |
| D1-04 | El resorte del alto llega (queda en su final y sale del `MotionDriver`) con ≤ 0,05 dp y ≤ 0,05 dp/s, como `SpringMotion` (34 → 90 en TV, ≈ 1,0 s). Mientras corre es F (`Settle` lo espera); la regla de toma lo deja pasar a menos de 0,15 dp | La convención de la 4a |
| D1-05 | Repeticiones: la primera fila, que reemplaza al vacío (`FromEmpty` con `NewRep`), lleva su marca con 40 ms de demora (120 con reducir movimiento), así entra con su fila (el cambio de pieza, 120-280) y no con la curva de la fila nueva (80-240); sin empuje | §6.2 ("es un cambio de contenido, con la marca") y §6.3 |
| D1-06 | `MarkerAlpha0..3` son **por fila dibujada** (0 = la más reciente; repeticiones usa 0 a 2): con k filas nuevas las marcas bajan k lugares (la que sale de la vista se va); con un cambio de paso sin vueltas nuevas quedan en las filas que se vacían (la vista las multiplica por `ClearedRowsAlpha`); al cerrar se apagan con el contenido y al abrir no hay ninguna | Ninguna marca salta |
| D1-07 | `RepLineAlpha` queda en 0 después de la salida de la línea (hasta que la serie vuelve a empezar o un estado final, que la deja en 1); `DroppedAlpha` es 0 si no se va ninguna fila (la línea de tiempo da 1); `NewRows` es k solo mientras corre la fase de filas | La vista dibuja la línea con la serie o mientras se va, y la fila que se va solo si existe |
| D1-08 | `ContentChanged` con el panel cerrado (o cerrándose) no empieza nada del contenido: solo toma el alto final. Repeticiones con `SeriesStarted` va a su estado final en el acto (la entrada es de `SeriesColumnMotion.RepsAppear`) y con `SeriesEnded` no hace nada (se va con la columna, sin moverse) | §6.0, §6.4 |
| D1-09 | `Retarget(h, instant: false)` (el reparto de `RowsScroll` cuando el otro panel se abre o se cierra): el resorte desde donde está, con la cola pegada al vidrio (`G − H_final`). Con `Instant`, todo a su estado final y la capa de estado en su objetivo. `SetExpanded` con el mismo estado y otro alto es un `Retarget` | §4.2 no decía qué hace sin `Instant`; lo causa un toque (resorte) |
| D1-10 | `Stop()`: sale del `MotionDriver`, la capa de estado va a 0 (en `Visual`) y nada más cambia ni avisa (tampoco el `gone` de la columna). La llamada siguiente parte del estado final; al volver a un panel la vista llama con `Instant` (QA-G5T-12) | ROB-02 |
| D1-11 | `SeriesColumnMotion`: `RepsLeave` sin `Instant` avisa `gone` una vez, después de aplicar el cuadro de los 120 (con `Instant`, en el acto); una entrada que corta la salida la cancela sin avisar; la salida y la entrada siguen desde la opacidad que tienen; un `FollowSpm` que corta otro sigue desde su corrimiento; `ShiftScroll` corre también el origen y el destino de la rueda. Las fases son independientes: en `RowsScroll` (W1280) la vista puede usar una instancia por panel solo para su rueda | El contrato de §4.2 tiene una sola `ScrollBase` |
| D1-12 | Reducir movimiento que se prende a mitad de camino: el resorte, el alto por un dato, el empuje y los fundidos de abrir y cerrar van a su final en el acto; las subidas de las piezas, de la línea `Rep` y de repeticiones, a 0; las marcas y la capa de estado siguen (son opacidad) | QA-V4b-MOT-03 |
| D1-13 | `Tests/PlayMode/HudMotionTests.cs` (fuera de la lista de §7.3): los arreglos de la cascada pasan de 7 a `PieceCount` (las 9 piezas de `HudPiece`) y el conteo de avisos con reducir movimiento, de 7 a 9. Nada más cambia en HMT | La cascada avisa a todas sus piezas: con las dos nuevas, `opacity[(int)p]` se salía del arreglo. La página (`ApplyPiece`, `OnPieceInteractive`) ya ignora las piezas que no conoce, hasta que B2 se las pase a `SeriesColumn` |

**Corridas** (desde la raíz del repo; `<scratch>/it5/wp-d1driversdemotion`):

| Suite | Resultado |
|---|---|
| `tools/dotnet/run-tests.ps1` | compilación con 0 avisos y 0 errores; simulador 188 / 188; Core 2532: 2497 pasan, 0 fallan, 35 ignoradas (las de WP-A; dotnet no compila `Presentation` ni `Tests/PlayMode`) |
| Unity EditMode | 2588: 2549 pasan, 0 fallan, 39 ignoradas. La compilación de los archivos de D1, sin avisos |
| Unity PlayMode | 387 / 387 (SMT 24, HMT con la cascada de 9 piezas) |
| Unity EditMode `-Simulator` | 2776: 2737 pasan, 0 fallan, 39 ignoradas |
| Unity PlayMode `-Simulator` | 453 / 453. El paso que prende el *define* reventó al cerrar (0xC0000005) y el script siguió (QA-V4b-R2-03); el *define* quedó restaurado y sin marca |
| Mutación de control (`-Filter SeriesMotionTests`) | cuatro errores a propósito, cuatro rojos y solo esos: sin la cota de D1-03, `ADataChangeDuringTheOpeningSpringRetargetsItFromWhereItIs` ("la cola no salta": 52,26 en lugar de 68); sin la demora de D1-05, `TheFirstRepReplacesTheEmptyWithItsMarker` (la marca a los 83 ms antes que su fila); sin `gone`, `TheRepsPanelAppearsAndLeavesWithTheSeries`; sin las dos piezas, `TheCascadeBringsThePanelsWithSpm`. Restaurado |
| 0 B por cuadro (SMT) | `GcAllocations` en 0 en 30 cuadros de cada escena (abrir, la vuelta nueva con su marca, el cambio por pieza, cerrar, el dato con el resorte corriendo, la serie que empieza con la columna, la capa de estado con el corrimiento y la rueda, la salida de repeticiones con `ShiftScroll`), después de una vuelta de calentamiento; el control positivo (un `byte[64]` en cada *callback*) da ≥ 30 en la misma prueba, en *batchmode*. El repintado lo mide HBT con `FrameAllocations` (B1) |

El primer *import* de Unity creó los `.meta` de los archivos nuevos de WP-A y de D1 (WP-B0 todavía no había abierto el proyecto). Ninguna prueba toca `prefs.json` ni la base. `git -C ../rowmate-legacy status --porcelain` vacío al empezar y al terminar; `lib/core/strava/strava_secrets.dart` no se leyó. Sin *commits*.

---

## Cambios por implementación de WP-B

WP-B (B0 a B3) quedó implementado con los archivos de §7.2 y los de la tabla de abajo. Precisiones y desvíos, sin cambiar el QUÉ (los que tocan lo que se ve o un AC van marcados **para PO y QA**).

### Informe de WP-B0

| Punto | Resultado |
|---|---|
| (a) El build de 00-deuda-4b §2.4 | **NO VERIFICABLE**: el `RowMate.exe` del usuario siguió abierto en `Builds/Windows/simulator` durante todo el trabajo (no se tocó). Va al cierre con las tres entradas y los paneles (QA-G5T-14, riesgo aceptado por el PO) |
| (b) El peor caso real de la 4b (`DescribeVisible`, HBT) | 55 elementos en TV y P360, 56 en TH, L640 y W1280. Con **M5** (`StepInfoBar`: `margin-left: auto` en `#hud-up-next`, o en `#hud-countdown` sin `Próximo`; sale `.rm-flex-spacer`), 54 y 55. `it4b` con M5: 51 tomas, 0 problemas y los 52 archivos (imágenes, videos e `INDEX.md`) iguales byte a byte (en el *manifest* cambia solo `durationMs`) |
| (c) S-B5 (prueba temporal, borrada con su `.meta`) | (1) un punto a 3 y a 10 dp debajo del chip (fuera de su caja de *layout*, dentro de los 56) llega al panel por el hijo oculto; a 12 dp, no; (2) con `translate` −59 se elige donde se ve; (3) en TH, desplazado por la columna, adentro de la ventana de la columna lo elige y afuera no (`ContainsPoint` con el recorte en dp de mundo); (4) el foco, `NavigationSubmitEvent` (1 vez) y Espacio (1 vez). Las cuatro `FontAsset` (Inter SemiBold y Regular, RowMateNumericSC Bold y los íconos), `Painter2D` y `Allocate` con color por vértice en un mismo elemento: bien. 80 `DrawText` + `Painter2D` + `Allocate` por repintado: 0,429 ms de media y 0,500 el peor, sin asignar. **El plan B (+2) no hizo falta** |
| (c) El contador de asignaciones (DT5-18) | En *batchmode*, los `ProfilerRecorder` de `GC.Alloc` y de "GC Allocated In Frame" leídos después de cada `yield return null` dan 0 también con el control positivo, o el ruido de todo el proceso (más de 70 000 B en 30 cuadros después de otras pruebas). Lo que sí ve el control: un `Recorder` de `GC.Alloc` con `FilterToCurrentThread`, prendido **adentro** del dibujo observado (`SeriesPanelView.DrawObserver`, alrededor de `Walk`) y del paso del cuadro. `FrameAllocations` mide así: el control positivo (un `byte[64]` por repintado o por paso) da 30 en 30 cuadros y el mismo sin asignar da 0 (FAT) |
| (c) La junta (DT5-09) | **No se hizo.** El sello dibujado con `DrawText` no sale igual byte a byte (2941 de 59 535 píxeles distintos, hasta 65 de diferencia: el ajuste al píxel de `Label` y el de `DrawText` no son el mismo) y §3.6 pide `it4b` igual; `Próximo` tiene su resorte de ancho (más riesgo). Con M5, el peor caso de la 5 da **56 en TV y P360 y 57 en TH, L640 y W1280** (HBT): **el tope sigue en 60**, con 3 de margen para la 6a (ARCHITECTURE §20.2) |

### Precisiones de B1 a B3

| # | Qué | Por qué |
|---|---|---|
| B5-01 | **`FrameAllocations`** mide los dibujos observados de los paneles (`Watch`) y el paso de la UI de cada cuadro (`Step(H.Handle.Tick)`); el avance del reloj de prueba de 1 Hz (`Scheduler.Advance`: la lectura del remo, el motor y `WorkoutHudStore`) va **afuera** | La lectura del remo de prueba asigna (100 asignaciones en 18 cuadros: la `RowingData` y su evento) y no es del cuadro de la UI: es el dato de 1 Hz de Core y App que §2.5 ya cuenta. HBT `SeriesNoAllocationsPerFrame`: 0 en los dibujos y 0 en los pasos, con más de 5 dibujos medidos por escena |
| B5-02 | El título del panel se mide con un portador propio de **11 dp** (`_titleFont`), no con el de 12 escalado | Con 12 escalado, `Parciales 500 m` daba 0,933 y no la escala de título del español de DD5-20 (0,949) |
| B5-03 | Las comprobaciones de los rectángulos del diseño (CAP, SPV) van con **± 1,0 dp**; las de la geometría (`SeriesPanelGeometry` contra lo dibujado), con ± 0,5 | Unity resuelve la escala de TV en 1,0549 y las tablas del diseño usan 1,054: entre 210 y 430 dp la diferencia llega a 0,7 dp |
| B5-04 | **DD5-27** (con la rueda abajo, una vuelta nueva no mueve ninguna fila a la vista): el contenido de la columna cuenta las vueltas **a la vista** (`min(vueltas; 4)`, no el número de la última), `PlaceBoth` va **antes** de `Apply` y el panel compensa el empuje cuando la columna ya corrió el desplazamiento | Con el número de la última vuelta (1999 en S-LARGO) el máximo de la rueda salía mal y la fila saltaba. SCT `RowsScrollOnTheDesktop` lo prueba con 3 vueltas y después la 4.ª |
| B5-05 | `Place` compara con `float.IsNaN(…) \|\| …` | La primera vez los valores guardados son `NaN` y `Math.Abs(NaN − x) > ε` es falso: los paneles quedaban en (0; 0) |
| B5-06 | **Para PO y QA (AC-WKT-17-09, -18-14)**: el chip `Reps` **cerrado** debajo de parciales **abierto** baja con él y entra en la franja de afuera de la zona del bote (CAP-02, CAP-14 y CAP-20 del diseño: 339,4-375,3 en TV, con la zona desde 323,7). La auditoría lo cuenta como la columna abierta (solo por la franja de afuera, nunca en la columna del centro); `RespectsBoatZone` no cambia | Los rectángulos de esas tomas son los del diseño; "cerrados, nunca" pensaba en los chips en su lugar |
| B5-07 | La auditoría de la zona del bote usa los bordes **en x de la geometría** (la columna) y comprueba aparte que la caja del mundo coincida con ellos a ± 0,5 | La caja del mundo va ajustada al píxel: en P360 termina en 139,53 y la franja de afuera en 139,52 |
| B5-08 | `LayoutAudit` no cuenta un panel con la máscara de la columna (`.rm-hud-series-panel--masked`, que pone `SetColumnMask` en `ColumnScroll`) | En TH, `Reps` abierto queda entero debajo del piso (alfa 0, no responde) y su caja de *layout* pasa la ventana; `HudOverlap` ya mide lo visible recortado (§5.6) |
| B5-09 | **El reloj de prueba de `it5`**: con `ScenarioScript.ReadingZeroAtStart` (lo pide `SeriesCatalog`), `L(0)` llega justo antes del toque que empieza la sesión (`ScriptedRower.EmitIndex(0)`) | La spec §0.4 dice que `L(0)` llega antes de iniciar y que en el segundo `k` la rutina lleva `4k` m. El arnés de la 4b deja como referencia la última lectura del reposo (una `L(j)` cualquiera) y `L(1)` cuenta como reinicio: en las rutinas que empiezan con trabajo (`Fondo 3000`, `Trabajo 1000`, `Cinco reps`) la vuelta en curso salía 1 s y 4 m atrás (CAP-08 daba `0:49.2` `246 m` y `#2 2:04.8` en lugar de `0:50.0` `250 m` y `#2 2:05.0`). `it4b` sigue sin esto (su regeneración es byte a byte en las tomas libres); el motor no cambia |
| B5-10 | **Para PO y QA (AC-BLE-16-02; A5-13): la llave con el diálogo de completado y los paneles.** `OverlaySurface.Series` lleva lo que se ve de la columna (`SeriesColumn.VisibleBounds`, solo con el diálogo) y `WrenchPlacement` (R10) prueba, en orden: el carril de siempre sin tocar la columna (TV y P360: debajo del diálogo, como en la 4b); si no hay hueco, el carril pegado a la derecha de la columna, a 8 dp (TH: x 167,2, y 270,3-310,3); si tampoco, el lugar de la 4b con `WrenchSpot.OverSeries` | En **L640 no hay ningún hueco libre en la ventana** (el diálogo va de x 120 a 520 y de y 37,7 a 322,3; a la izquierda, SPM, la columna y la banda llenan el carril; a la derecha, Split, Watts y kcal): la llave queda sobre el chip inerte, debajo del velo, como en la 4b. `WrenchAudit` acepta solo ese caso (el diálogo arriba y `OverSeries`). WPT `WithTheDialogTheWrenchAvoidsTheSeriesColumn` (dotnet) y `WithTheCompletionDialogTheWrenchAvoidsThePanels` (PlayMode con `-Simulator`). Por esto cambia `it4b` CAP-09 (la llave a la derecha de los chips) |
| B5-11 | El anillo de Tab del inmersivo: `Terminar` → `Pausa` → parciales → repeticiones (si se ve) → la llave. Los paneles responden desde su tramo de la cascada (`OnPieceInteractive(SeriesLaps)`, 520 ms) y nunca durante la salida, con la rutina terminada ni con un diálogo; si repeticiones se va con el foco, Tab sigue en `Terminar` (SKT) | §3.3; 04-design-notes §4.9 |
| B5-12 | `prefs.json` en la carpeta de la base (`DataLocation.PreferencesFile`); `AppRoot` lo abre con `JsonFilePreferences` y al cerrar espera hasta 1 s la escritura pendiente. `UiTestHost` usa `MemoryPreferences` o, con `UiHostOptions.PreferencesFile`, un archivo en una carpeta temporal propia que borra al terminar | ADR 021. Ninguna prueba ni captura toca el `prefs.json` del usuario (BootScenePreferencesTests, PFU, PIS) |
| B5-13 | `-rowmate-perf-routines "A;B"` (con `-rowmate-perf-entries`: la primera entrada libre y después esas rutinas, por nombre) y `-rowmate-perf-series <s>` (`Fondo 3000` y `Series 600`, los paneles cada 2 s), solo en desarrollo y con `ROWMATE_DATA_DIR`. `SnapshotCli.SeedDatabase -seed F5 -prefs laps,reps` siembra la base y `prefs.json` al lado | §3.7. `BuildOutputInspector` busca los dos en el build normal |
| B5-14 | Las consultas de prueba por ranura (`UiQuery.FindSlot`, `UiTestHost.TextOf` / `IsShown` / `BoundsOf` / `TapText` / `SlotOf`, `PixelProbe`): primero un elemento con ese nombre y, si no hay, una ranura de un `IDrawnTextSource`. `TapText` sobre una ranura toca su panel por la selección real | §6.2 |
| B5-15 | `RoutineSeeds.Lt59Seed`, `UiTestHost.OverrideSeries` (S-LARGO en CAP-25 y SPV) y `HudWarmup.WarmSeries` (el precalentado con una serie de 600 m) | Fuera de la lista de §7.2: de prueba y del precalentado de AC-WKT-13-15 |

**Corridas** (desde la raíz del repo; `<scratch>/it5/wp-b`):

| Suite | Resultado |
|---|---|
| `tools/dotnet/run-tests.ps1` | compilación con 0 avisos y 0 errores; simulador 192 / 192 (con los 4 casos nuevos de WPT); Core 2536: 2502 pasan, 0 fallan, 34 ignoradas |
| Unity EditMode | 2595: 2557 pasan, 0 fallan, 38 ignoradas. Ningún aviso nuevo de compilación (quedan los de antes, de `Infrastructure/*Sqlite*` y dos pruebas viejas) |
| Unity PlayMode | 420 / 420 |
| Unity EditMode `-Simulator` | 2787: 2749 pasan, 0 fallan, 38 ignoradas |
| Unity PlayMode `-Simulator` | 489 / 489 (la corrida final, después de rehacer el tiempo de WSM `SimulateDisconnectInTheCurrentLap`: el corte a los 238,6 sin asentar y la referencia en el segundo 239 del motor, que toma lo que llegó antes del corte; y de llevar a la vista la tarjeta de `Corta` en la prueba de la llave con el diálogo) |
| HBT | 0 asignaciones en los dibujos y en los pasos de abrir, cerrar, la vuelta nueva, el cambio por pieza y la repetición nueva (14 a 32 dibujos por escena); el trabajo de los paneles, mediana 0,743 ms y peor 0,933 ms en 60 cuadros; el peor caso de la 4b con M5, 54 / 55, y el de la 5, 56 / 57 |
| `capture-snapshots.ps1 -Set it5` | A a `<scratch>` y B al repo: 47 tomas, 0 problemas, 0 sondas de aspecto que fallan; los 48 archivos de imagen, video e `INDEX.md` iguales byte a byte entre las dos (en `manifest.json` cambia solo `durationMs`); una tercera corrida después del último cambio de código, igual |
| `capture-snapshots.ps1 -Set it4b` (la regeneración única) | A a `<scratch>` y B al repo: 51 tomas, 0 problemas, 0 sondas que fallan; A y B iguales byte a byte salvo `durationMs`. Contra lo versionado: las tomas libres (CAP-13 a -18, CAP-28, DEMO-01 a -04 y -12, VID-01, -02, -06 a -08) iguales byte a byte; cambian las 34 con rutina (los chips; en CAP-09 además la llave, B5-10). `it4a`, `it3` e `it2` no se tocaron |
| `tools/build/build.ps1 -Target Windows` (normal, Mono) | `Builds/Windows/normal`: bien; `BuildOutputInspector` sin rastros del simulador, de diagnósticos, de pruebas ni de `-rowmate-perf-*` |
| `tools/build/build.ps1 -Target Windows -Simulator` | **NO VERIFICABLE**: el `RowMate.exe` del usuario siguió abierto en `Builds/Windows/simulator` en los 11 intentos (cada 2 min, 20 min; no se tocó su proceso). Por eso tampoco: las pruebas a mano en el build (abrir y cerrar, la vuelta en curso y las repeticiones, reiniciar y ver los paneles recordados, `Simular desconexión`) ni las ventanas `[PERF]` (`-rowmate-perf-entries 10 -rowmate-perf-routines "Prueba 4a;Cinco reps"` y `-rowmate-perf-series`). Lo cubren en el Editor BootScenePreferencesTests (`PreferencesSurviveARestart`), WSM, SCT, SKT, DEMO-01 a -12 y VID-05; va al cierre con QA-G5T-14 |
| IL2CPP y Android | NO VERIFICABLE (no están instalados) |

Ninguna prueba ni captura tocó el `prefs.json` ni la base del usuario. `git -C ../rowmate-legacy status --porcelain` vacío al empezar y al terminar; `lib/core/strava/strava_secrets.dart` no se leyó. Sin *commits*.

---

## Cambios por verificación (ronda 1)

Corrige los dos mayores confirmados de la verificación ([05-qa-verification](05-qa-verification.md) y las lentes [05-qa-lens-paridad](05-qa-lens-paridad.md) y [05-qa-lens-motion](05-qa-lens-motion.md)) y los menores que son del Architect, sin cambiar el QUÉ. Cada arreglo tiene una prueba que **corrió en rojo contra el código anterior** y pasa después (la columna "Prueba"; las salidas rojas, en `<scratch>/it5/fix-architect-r2/`), salvo LM5-04 y LM5-05, que son huecos de prueba (la prueba nueva pasa desde el principio). Legacy: [rowmate-legacy](https://github.com/figuibej/rowmate-legacy) en `3ae0f3a`, solo lectura (`series_panels.dart:330-345`, citado por la lente); no se leyó el módulo de credenciales de Strava.

| # | Issue | Qué cambió | Prueba |
|---|---|---|---|
| V5-01 | **QA-V5-P01** (mayor; AC-WKT-02-08, -18-07) | `WorkoutHudStore` reusa las filas de `Series anteriores` **solo si salen de la misma instancia** de `SeriesState.PreviousReps` (`_seriesRepsSource`, `ReferenceEquals`): el tracker reemplaza esa lista en cada cambio (copia al escribir, `SeriesTracker.EnterStep`), así que la misma instancia es el mismo contenido. `RepRows` ya no compara `Rep` y metros: dos series de la misma forma pueden coincidir en los dos con otro split, y en segundo plano no hay un segundo con la lista vacía entre ellas. Si no es la misma lista, las filas se arman desde el tracker, como el legacy en cada dibujo. La caché por revisión y el reuso dentro de una serie no cambian (`TheSeriesChangeOnlyOnTheSecondAndReuseTheirRows` sigue igual) | `WorkoutHudStoreTests.TheRepsOfAnotherSeriesAreRebuiltAfterTheBackground` (dotnet; la rutina de la sonda, A ×2 y B ×2 [500 m; 60 s] con la Rep 1 en 620 m y 2:05 / 1:40, en primer plano y con segundo plano de 200,25 a 540,25 s). Rojo: `al volver del segundo plano: el split de la Rep 1 de B. Expected: "1:40" But was: "2:05"` (el control en primer plano pasaba) |
| V5-02 | **LM5-01 (a)** (mayor; AC-WKT-18-13, -18-20) | `SeriesColumn.Apply` recorta el desplazamiento al tope nuevo después de `Compute` y `PlaceBoth` (`ClampScroll(instant ‖ first)`). El recorte va **sin salto**: `WheelTo(tope, instant)` con `spring.snappy` desde donde está, como la rueda; en el acto solo con `Instant` (la vuelta del segundo plano, el primer estado), la rotación o reducir movimiento. Mientras el resorte corre el tope ya es el nuevo (el arrastre y la rueda parten del destino). `VisibleBounds` (la llave con el diálogo de completado) usa el destino del desplazamiento, "en reposo" | `SCT.TheColumnScrollFollowsAColumnThatShrinks` (TH y L640: los dos abiertos a los 380,2 s, la columna arrastrada al tope y la serie que termina a los 541,2 s; afirma el desplazamiento ≤ el tope (0), la cabecera de parciales entre el techo y el piso, elegida por `panel.Pick` y con el título a alfa 1). Rojo: `TH: dentro de su tope. Expected ≤ 0,05 But was 86,094` |
| V5-03 | **LM5-01 (b)** | Ningún recorte salta en el cuadro del toque. `ColumnScroll`: `OnExpansion` y `Replace` recortan con el mismo criterio (`ClampScroll(instant)`; cerrar repeticiones con la columna al tope ya no la baja 48 base = 50,6 dp de golpe). `RowsScroll` (W1280): cerrar un panel lleva sus filas a 0 con `spring.snappy` mientras el contenido se apaga (`SetExpanded`), y `Retarget` y `Apply` recortan con el resorte salvo con `Instant` (`ClampRowsScroll(instant)`, que antes ignoraba el argumento). DD5-27 suma al desplazamiento **solo lo que cabe debajo del tope** (`Place` ya dejó el tope nuevo): lo que no cabe (las filas al tope, con la más vieja que sale por abajo) lo hace el empuje en su proporción (`_pushScale`); antes sumaba 22 y lo recortaba en el acto, y la fila a la vista saltaba 22·sp | `SCT.ClosingAPanelWithTheColumnAtTheTopDoesNotJump` (TH; el desplazamiento dibujado, leído en el acto, cuadro a cuadro: < 12 dp por cuadro y termina en el tope nuevo). Rojo: `el peor, 50.6 dp`. `SCT.TheRowsScrollIsClampedWithoutJumps` (W1280 con S-LARGO y las filas al tope: la vuelta 2000 sin salto, < 8 dp por cuadro; cerrar parciales con las filas desplazadas, < 12 dp por cuadro y a 0 al final). Rojo: `el peor cuadro, 26.4 dp`. `UiTestHost.OverrideSeries` y `SeriesColumn.OverrideSeries` suman `instant` (por defecto `true`, como antes) para aplicar un estado armado como un dato del motor |
| V5-04 | **LM5-01 (c)** | **No cambia** (queda para el Designer): DD5-27 en `ColumnScroll`. En `RowsScroll` la fila nueva entra arriba de la ventana de filas, fuera de la vista; en `ColumnScroll` la fila nueva entra debajo de la cabecera y la vuelta en curso de parciales, que se desplazan con la columna, así que sumar 22·sp no deja "lo que se ve" quieto: deja quieto lo de abajo (repeticiones) y sube la cabecera y la vuelta en curso. La verificación que pide 04-design-notes §10 para DD5-27 es la de W1280. Propuesta para el Designer: en `ColumnScroll`, compensar solo cuando el lugar donde entra la fila ya está arriba del techo (desplazamiento ≥ el borde de arriba de las filas), y si no, el empuje como con el desplazamiento en 0. Hasta que lo decida, la columna sigue sin `ShiftScroll` (lo que se ve baja con el empuje, sin saltos) | — |
| V5-05 | **LM5-02** (menor; AC-WKT-18-12, -18-16, AC-APP-05-07) | (a) El panel que recibe el foco **con el teclado** en `ColumnScroll` se trae a la vista: `SeriesPanelView` avisa `KeyboardFocused` en `FocusInEvent` si está en modo teclado y responde (un toque ya está a la vista: como `FocusScroller`), y `SeriesColumn.OnKeyboardFocus` desplaza la columna con `spring.snappy` lo justo para que entre entero con el lugar del halo (8 dp) adentro o, si no entra, hasta que su cabecera quede en el techo. (b) El anillo se recorta a la ventana de la columna: `FocusRingSpanBase` (el vidrio entero sin máscara; con la máscara, la parte en la ventana menos los 8 dp del halo) y `DrawFocusRing` lo rodea con los radios de DD5-22 (sin máscara, igual que antes: CAP-26 no cambia). El borde de afuera del halo nunca pasa el techo ni el piso: en L640 la banda empieza 7,1 dp debajo del piso | `SKT.InColumnScrollTheFocusedPanelComesIntoView` (TH y L640 con CAP-16: Tab hasta repeticiones, que empieza entero debajo del piso; su cabecera a la vista, el anillo y el halo entre el techo y el piso y sin tocar la banda; Mayús+Tab trae parciales). Rojo: `Expected ≤ 310,65 But was 350,15` (la cabecera de repeticiones debajo del piso). Con el primer recorte (el halo por fuera de la ventana) L640 todavía tocaba la banda (279,2 contra 278,5): por eso el halo va adentro |
| V5-06 | **LM5-03** (menor; AC-WKT-13-16; O-4 de la 4b) | `HudMotion.Tick`: si `Advance` lanza, deja `_inDriver` en falso (y sube su generación) antes de volver a lanzar; el `MotionDriver` la saca y la registra como antes (O-4). Así `IsRunning` no miente (`SeriesColumn.IsAnimating` y la regla de toma no esperan sin fin) y la llamada siguiente (`Run`) la vuelve a agregar. Vale para todas las `HudMotion` (también las de la 4b) | `MDI.AHudMotionThatThrewRecovers` (Unity EditMode: una `HudMotion` que lanza una vez sale, `IsRunning` en falso, sin pedido de 60 fps, y la llamada siguiente la agrega y anima). Rojo: `y no dice que sigue corriendo` |
| V5-07 | **LM5-04** (menor; AC-WKT-18-21; hueco de prueba) | `HBT.SeriesNoAllocationsPerFrame`: 30 cuadros por escena (antes 18) y el comentario al día (afirma 0 sin restar ninguna línea de base). `HBT.SeriesNoAllocationsPerFrameInMoreScenes` (nueva): la serie que empieza (36 cuadros desde los 30,9 s: aparece repeticiones y entra la línea `Rep`), la marca de `#1` que se apaga (48 cuadros, de los 620 a los 1420 ms), el anillo de foco en modo teclado con el resorte, la rueda de las filas en W1280 (S-LARGO) y la máscara de `ColumnScroll` en TH (la rueda de la columna: `DrawMaskedGlass` y la regla 3 en cada cuadro). Antes de medir, cada camino corre una vez. Queda sin medir la capa de *hover* (el puntero de mouse de prueba no la prende) | Las diez escenas, **0 asignaciones** en los dibujos y en los pasos (14 a 60 dibujos por escena: abrir 31, la vuelta nueva 14, el cambio por pieza 16, la repetición nueva 32, cerrar 31, la serie que empieza 19, la marca que se apaga 48, el anillo 31, la rueda en W1280 30, la máscara en TH 60), con el control positivo de FAT en la misma corrida; el trabajo de los paneles, mediana 0,745 ms y peor 0,809 ms en 60 cuadros |
| V5-08 | **LM5-05** (menor; AC-WKT-13-09; hueco de prueba) | `ITT.NothingAnimatesAfterTheExitWithThePanels` afirma, en sus dos salidas, que `AppContext.SeriesPanels.SubscriberCount` sube con el HUD y vuelve al de reposo después de la salida | la misma (pasa) |
| V5-09 | **QA-V5-02** (menor; AC-WKT-18-19; H5-D2-01) | La línea `Rep r/R · m` rueda **por tramos**. `Core/Design/DigitRuns` (N, puro) parte un texto en tramos de cifras (dígitos con `.`, `:` o `,` entre dos dígitos: el punto de miles, el reloj) y de texto, y dice si dos textos tienen la misma forma. `TabularCells.DrawFromLeft` (lo anclado a la izquierda con un rótulo): con la misma forma, el texto no rueda, cada tramo de cifras rueda alineado a su derecha con su ancho interpolado (180 ms) y lo que sigue se corre con él; si no, como antes. Los tramos se arman en el cuadro del dato, en arreglos propios (0 B por cuadro). `TabularNumber` y `DigitRollPlan` no cambian (la 4a y la 4b, iguales); la vuelta en curso es un solo tramo de cifras y se ve como antes | `DigitRunsTests` (dotnet, 4: los tramos de la línea, la misma forma con `8 m` → `12 m` y `999 m` → `1.000 m`, la celda que compara dentro del tramo, el tope de 8). `SPV.TheRepLineRollsOnlyWhatChanges` (TV: `Rep 1/3 · 680 m` → `Rep 2/3 · 4 m` ruedan solo el `2` y el `4`; `Rep 2/3 · 8 m` → `Rep 2/3 · 12 m`, solo `12`). VID-02 a -04 tomados otra vez |
| V5-10 | **QA-V5-03** (menor; H5-D2-02) | VID-06: `L(0)` justo antes del toque de `Iniciar` (`ScriptedRower.Next = 1` y `EmitIndex(0)`, lo mismo que `ReadingZeroAtStart` hace en las demás tomas de it5): la referencia del motor es la de `L(0)` y al segundo 1 la vuelta en curso dice `0:01.0` `4 m` (spec §7.5). El producto no cambia | *manifest* de VID-06: `values[24]` = `hero=0:01.0 4 m rep=Rep 1/5 · 4 m` (antes `0:01.0 0 m`); visto en la hoja de cuadros |
| V5-11 | **QA-V5-04** (cosmético; H5-D2-03) | VID-06 y VID-07 duran 2,5 s (75 cuadros), como pide la spec §7.5 | *manifest* e `INDEX.md` |

**Lo que no es del Architect y queda abierto**: QA-V5-01 (AC-BLE-16-02 en L640 con el diálogo; B5-10) y QA-V5-05 (redacción de AC-WKT-18-14 y -17-09) son del PO; QA-V5-P02 (la clave de otro tipo) es de documentación del PO. V5-04 (DD5-27 en `ColumnScroll`) espera al Designer.

**Archivos** (sin *asmdef* nuevos; `Packages/manifest.json` sin cambios): `App/Workout/WorkoutHudStore.cs`; `Core/Design/DigitRuns.cs` (N); `Presentation/Controls/TabularCells.cs`; `Presentation/Motion/Hud/HudMotion.cs`; `Presentation/Workout/SeriesColumn.cs` y `SeriesPanelView.cs`; `Snapshots/SeriesCatalog.cs`; pruebas: `Tests/EditMode/Workout/WorkoutHudStoreTests.cs`, `Tests/EditMode/Design/DigitRunsTests.cs` (N), `Tests/UnityEditMode/MotionDriverIsolationTests.cs`, `Tests/PlayMode/SeriesColumnTests.cs`, `SeriesKeyboardTests.cs`, `SeriesPanelViewTests.cs`, `HudBudgetTests.cs`, `ImmersiveTransitionTests.cs` y `Tests/UiSupport/UiTestHost.cs`.

**Capturas** (dos corridas seguidas: A a `<scratch>/it5/fix-architect-r2/capA-it5`, B al repo): `listo: 47 tomas, 0 con problemas, 0 sondas de aspecto que fallan` en las dos; los 48 archivos de imagen, video e `INDEX.md` **iguales byte a byte** entre A y B (en `manifest.json` solo `durationMs`). Contra lo que había en el repo cambian **solo** VID-04 y VID-05, y solo en la caja de la línea `Rep` (diferencia de píxeles cuadro a cuadro contra la toma anterior): en VID-04, desde el cuadro 75, `Rep 1/3 · 680 m` → `Rep 2/3 · 4 m` y `8 m` → `12 m`, mirados en hojas de cuadros: `Rep `, `/3 · ` y ` m` quedan quietos y solo ruedan las cifras que cambian (antes rodaba y se corría la línea entera); en VID-05, los rodillos de los metros (`109 m` → `113 m`…, del mismo largo) con diferencias de menos de 9 niveles (las celdas se suman desde la izquierda y no desde la derecha); VID-06 y VID-07 (2,5 s; VID-06 con `4 m`), `INDEX.md` y `manifest.json`. Las 38 PNG y VID-01 a -03, -08 y -09, iguales byte a byte (VID-02 y -03 no cambian: sus líneas `Rep` no cambian de largo y el plan por tramos rueda las mismas celdas). `it4b` tomado otra vez a `<scratch>`: 51 tomas, 0 con problemas, **52 de 53 archivos iguales byte a byte** a lo versionado (`manifest.json`, solo `durationMs`).

**Corridas** (desde la raíz del repo; artefactos en `<scratch>/it5/fix-architect-r2`):

| Suite | Resultado |
|---|---|
| `tools/dotnet/run-tests.ps1 -ArtifactsPath <scratch>/it5/fix-architect-r2` | compilación con 0 avisos y 0 errores; herramientas 192 / 192; Core **2541: 2507 pasan, 0 fallan, 34 ignoradas** (+5: QA-V5-P01 y `DigitRunsTests`) |
| `… -UnityResults` con los cuatro XML de Unity | matriz: **PASA 656, FALLA 0, IGNORADO 2**, PENDIENTE UNITY 0, SIN TEST 0 |
| `tools/unity/run-unity-tests.ps1 -Platform EditMode` | **2601: 2563 pasan, 0 fallan, 38 ignoradas**, 0 inconclusas |
| `… -Platform EditMode -Simulator` | **2793: 2755 pasan, 0 fallan, 38 ignoradas**, 0 inconclusas; Unity reventó al cerrar el paso de restaurar (0xC0000005) y el *script* siguió con el *define* como se pidió (QA-V4b-R2-03) |
| `… -Platform PlayMode` | **426 / 426** (+6: SCT 3, SKT 1, SPV 1, HBT 1) |
| `… -Platform PlayMode -Simulator` | **495 / 495** |
| Pruebas en rojo contra el código anterior (`-Filter`) | MDI `AHudMotionThatThrewRecovers`; SCT `TheColumnScrollFollowsAColumnThatShrinks`, `ClosingAPanelWithTheColumnAtTheTopDoesNotJump`, `TheRowsScrollIsClampedWithoutJumps`; SKT `InColumnScrollTheFocusedPanelComesIntoView` (los mensajes, en la tabla de arriba) |
| `tools/unity/capture-snapshots.ps1 -Set it5` (A y B) y `-Set it4b` (A) | arriba |
| Build de Windows con simulador | **No se compiló**: no lo pide esta ronda y el `RowMate.exe` del usuario estaba abierto en `Builds/Windows/simulator` (no se tocó su proceso). Lo de §6 de la verificación sigue pendiente |

Sin avisos de compilación nuevos (los de antes: `Infrastructure/*Sqlite*`, `RoutineMotionDebtTests` y `SimulatorModeTests`); el único `error CS` del registro es el `#error` buscado por `SimulatorModeTests`. Después de todas las corridas, `ProjectSettings/ProjectSettings.asset` sin `ROWMATE_SIMULATOR`, sin la marca `Library/RowMate/simulator-tests.txt` y sin cambios en `ProjectSettings`, `Packages`, `Assets/RowMate/UI` ni `Assets/RowMate/Settings`. Ninguna prueba ni captura tocó el `prefs.json` ni la base del usuario (`MemoryPreferences` por toma y por prueba; `ROWMATE_DATA_DIR` temporal en cada corrida). `git -C ../rowmate-legacy status --porcelain` vacío al empezar y al terminar. Sin *commits*.
