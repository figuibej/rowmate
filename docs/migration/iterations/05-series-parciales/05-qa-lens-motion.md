# It. 5 — Verificación QA, lente "motion"

> Lente paralela al QA principal. Foco: motion y robustez. Se revisó que ninguna animación adelante ni frene el reloj ni las métricas; que nada quede animando ni retenga el HUD al salir del inmersivo (ROB-02), con los paneles abiertos, a mitad de abrir o cerrar y con una vuelta nueva entrando; reducir movimiento; *hover* y presionado; toques y foco (la selección fuera de la caja, Espacio y Enter, el doble toque, las páginas que salen); las preferencias (escritura fuera del hilo principal, temporal y reemplazo, archivo dañado o con tipos raros, el precalentado que nunca escribe); 0 B por cuadro con el repintado; el presupuesto del HUD; y la deuda de la 4b (el precalentado, el armado en el negro y P-17 con los registros nuevos de cada despertar).
> **Sin Unity** (la tarea lo pide): todo lo de pantalla es revisión estática del código, apoyada en la geometría real del Core, que sí se corrió en dotnet. Lo de dotnet se corrió de verdad.
> Legacy de referencia: [rowmate-legacy](https://github.com/figuibej/rowmate-legacy) (solo lectura, `3ae0f3a`). No hizo falta leerlo para esta lente: el legacy no anima los paneles ni tiene desplazamiento por columna con máscara (02-tech-design, "Cambios por implementación de WP-D1"). `git -C ../rowmate-legacy status --porcelain` vacío al empezar y al terminar. No se leyó `lib/core/strava/strava_secrets.dart`.

---

## 0. Veredicto

**APROBADO CON OBSERVACIONES.** 0 bloqueantes, **1 mayor** y **4 menores** (§3). El mayor (LM5-01) hay que corregirlo antes del cierre.

- **La regla de oro se cumple.** Las motions de la 5 (`SeriesPanelMotion`, `SeriesColumnMotion`, `TabularCells`) leen `MotionDriver.Now` y nunca lo mueven. Los valores de los paneles cambian solo con un `HudSeries` nuevo (una vez por segundo del motor); el rodillo de 180 ms es aspecto. `PresentationIndependenceTests` (dotnet) da la misma secuencia del motor con y sin el *store*, también en `Series 600` con las preferencias cambiando cada 3 s.
- **No se repite ROB-02 en el código.** Al salir, `PageStack` saca la raíz del árbol **antes** de `OnPopped`: cada panel se detiene en `DetachFromPanelEvent` (su motion, su rueda y sus tres `TabularCells`) y se da de baja del `State` de `AppContext.SeriesPanels` (el `Bind` vive mientras el panel está adjunto); después `HudView.Dispose` detiene la `SeriesColumnMotion`. El precalentado dispone antes de sacar. Falta una prueba que fije la baja del `State` al salir (LM5-05).
- **Las preferencias son robustas.** La escritura va por un `SerialExecutor` propio; el texto se arma en el hilo principal; temporal y `File.Replace`/`File.Move`; archivo vacío, dañado, que no es objeto o con una clave de otro tipo, bien (JFP 15 / 15). El precalentado usa un `LocalSeriesPanelsExpansion(true, true)`: no hay camino a `SetBool`.
- **Mayor:**
  - **LM5-01**: en `ColumnScroll` (TH, L640) el desplazamiento de la columna **no se recorta cuando el contenido se achica por un dato**. Con la columna desplazada al tope (lo que hace falta para ver `Series anteriores` en TH), al terminar la serie los paneles quedan **enteros arriba del techo, invisibles**, y ni el dedo ni la rueda los devuelven (el tope nuevo es 0 y `OnDragged`/`OnWheeled` no hacen nada). Demostrado con la geometría real del Core (§5).
- **Menores:**
  - LM5-02: el anillo de foco no respeta la máscara de la columna: en TH y L640 cae sobre la banda (y el Tab llega a un panel entero debajo del piso).
  - LM5-03: una `HudMotion` que lanza (O-4) queda marcada "en el *driver*" para siempre: no vuelve a animar y `IsRunning` miente.
  - LM5-04: la medida de 0 B con repintado cubre 5 de las 7 escenas del diseño, en TV, con 18 cuadros (no 30): faltan la serie que empieza, la rueda en W1280 y la máscara de TH.
  - LM5-05: ninguna prueba fija que, al salir del inmersivo, el HUD deje de estar suscripto a `AppContext.SeriesPanels.State`.
- **NO VERIFICABLE en esta lente** (§7): todo lo PlayMode y Unity EditMode (SMT, SPV, SCT, SKT, HBT, FAT, ITT, 4BD, MDI, PFU), las capturas y videos, el build (`[PERF]`, AC-WKT-13-15), Android (IL2CPP, `File.Replace` en la carpeta interna) y la demo (c) de P-17.

---

## 1. Corridas

| Corrida | Resultado |
|---|---|
| dotnet, solución del repo (`powershell -NoProfile -File tools/dotnet/run-tests.ps1 -ArtifactsPath <scratch>/it5/lens-motion/artifacts`) | `Build succeeded. 0 Warning(s) 0 Error(s)`. Simulador **192 / 192**. Core **2536: 2502 pasan, 0 fallan, 34 ignoradas** (dispositivo y Unity). `exit=0`. En el foco de esta lente, todas en verde: `SeriesTimelinesTests` 18, `SeriesPanelPrefsTests` 9, `JsonFilePreferencesTests` 15, `PresentationIndependenceTests` 3, `SuspendAccountingTests` 22 (con las 8 nuevas de P-17: `EveryWakeIsLoggedWithBothSources`, `AWakeWithOnlyTheMeasureSaysTheSignalDidNotCome`, `AWakeWithOnlyTheSignalSaysThereIsNoMeasure`, `AResumeAfterTheWakeBringsItsDurationOnItsOwnLine`, `ManyWakesEachGetTheirLine`, `AWallJumpWithoutEvidenceIsLoggedButNotDiscounted`, `NothingIsLoggedWithoutEvents`, `AWakeAtRestIsLoggedThroughTheHarness`), `SeriesPanelGeometryTests` 43, `HudSeriesChangeTests` 12, `MotionSpecsTests` 6, `ItFiveDebtTests` 4 (`AppRootWarmsUpTheHudAfterStartingTheApp`, `PerfArgumentsOnlyInDevelopmentBuilds`…) |
| Sonda de la lente (`<scratch>/it5/lens-motion/probe`: un proyecto NUnit propio contra los `RowMate.Core.dll` y `RowMate.Tests.Support.dll` recién compilados; ningún archivo del repo) | **6 casos: 2 pasan (informativos), 4 fallan a propósito**: afirman lo esperado y muestran el defecto con los números de la geometría real (§5) |
| Repo | No se cambió nada salvo este archivo. Otros agentes trabajan en paralelo (aparecen capturas y `05-qa-lens-paridad.md`); nada de eso es de esta lente |
| Legacy | `git status --porcelain` vacío al empezar y al terminar |

---

## 2. El foco, punto por punto

| Tema | Qué se miró | Resultado |
|---|---|---|
| Reloj y métricas | `SeriesPanelMotion`, `SeriesColumnMotion`, `TabularCells`, `SeriesColumn.Apply`, `SeriesPanelView.Apply`; `HudView.Apply` y `Freeze` | Sin hallazgos. Nada lee el reloj del sistema; el contenido cambia en el cuadro del `HudSeries` (con `Instant` en el acto, sin marcas ni rodillos); con el panel cerrado no se anima el contenido; `Freeze` corta el dato en la salida. dotnet: `PresentationIndependenceTests` 3 / 3 |
| ROB-02 al salir | `SeriesPanelView.OnDetach`/`Dispose`/`Stop` (`Presentation/Workout/SeriesPanelView.cs:481-500`, `:550-555`), `SeriesColumn.Dispose` (`:596-607`), `HudView.Dispose`, `PageStack.OnExited` (`Presentation/Shell/PageStack.cs:221-241`: `RemoveFromHierarchy` y después `OnPopped`), `ViewBinding` (baja en `DetachFromPanelEvent`), `HudWarmup.Finish` | Sin bug. `RepsLeave` cortado por `Stop` no llama `gone` (D1-10): da igual, el panel se va con el HUD. Falta la prueba de la baja del `State` (LM5-05). ITT `NothingAnimatesAfterTheExitWithThePanels` (con el resorte y con la marca) existe; no se corrió acá |
| Reducir movimiento | `SeriesTimelines` (las variantes `reduced`), `FinishForReducedMotion` de las dos motions, `TabularCells` (`_reduced` al empezar) | Sin hallazgos. dotnet: STL 18 / 18 |
| *Hover* y presionado | `OnPointerEnter/Leave/Down/Up/Cancel`, `Motion.Hover`, `SetInteractive(false)` | Sin bug. Solo el mouse da *hover*; el presionado también con el dedo; inerte apaga la capa en 160 (04-design-notes §4.9). Observación O-3 (un diálogo abierto con el teclado y el mouse quieto sobre el panel) |
| Toques | `ContainsPoint` por estado (cerrado, abriendo, cerrándose), el hijo `.rm-hud-series-panel__hit`, el recorte de `ColumnScroll`, el umbral de 8 dp, la captura y su liberación, `Responds` (inerte, diálogo, salida, rutina terminada) | Sin bug, salvo que en `ColumnScroll` un panel corrido arriba del techo queda sin toque y sin forma de volver (LM5-01) |
| Teclado y foco | El anillo de Tab (`ImmersiveWorkoutPage.cs:772-831`), `OnSubmit`/`OnKeyDown` (Espacio una vez), el panel que se va con el foco, `DrawFocusRing` | En `ColumnScroll` el anillo no respeta la máscara y el Tab llega a un panel que no se ve (LM5-02). Observación O-2 (Espacio) |
| Doble toque | Dos toques seguidos | Correcto: el segundo invierte el resorte desde donde está (AC-WKT-18-18); las dos escrituras se juntan |
| Páginas que salen | `BeginExit` (`SetInteractive(false)`, `Freeze`), la convivencia de dos inmersivos (QA-V4a-01) con el `SeriesPanelPrefs` compartido | Sin bug: los paneles del nuevo responden desde su tramo (520) y el viejo ya salió (640) antes de que se pueda tocar el nuevo después del revelado (400) |
| Preferencias | `JsonFilePreferences` (lectura, `RequestWrite`/`Drain` con su compuerta, `FlushAsync`, `Dispose`), `SystemPreferencesFileOps`, `SeriesPanelPrefs`, `AppRoot.OnDestroy` (espera 1 s y dispone), `DataLocation.PreferencesFile`, `RunPerfSeries` (solo con `ROWMATE_DATA_DIR`) | Sin bug. La compuerta `_draining`/`_pending` bajo el mismo `lock` no pierde escrituras; la última gana. Observación O-1 (dos procesos sobre el mismo archivo) |
| El precalentado y las preferencias | `HudWarmup.Start` (`Presentation/Workout/HudWarmup.cs:78`: `new LocalSeriesPanelsExpansion(true, true)`), el `HudView` libre (sin columna), `_interactive` en falso | Nunca escribe ni se suscribe al `State` de la app. 4BD lo fija con `RecordingPreferences` y `SubscriberCount` (no corrida acá) |
| 0 B por cuadro | `FrameAllocations` (el `Recorder` de `GC.Alloc` del hilo principal prendido alrededor de `Walk` y del paso), FAT (el control positivo), HBT `SeriesNoAllocationsPerFrame`; el dibujo (`DrawnFont.CharString` precalculadas hasta U+2026, `TabularCells.DrawPlain`, `Painter2D`, `Allocate` con un arreglo propio) | El método es el correcto (no usa `GC.GetAllocatedBytesForCurrentThread`). Revisado a mano, `Walk` no asigna en ninguna rama. La cobertura queda corta respecto del diseño (LM5-04) |
| Presupuesto | `HBT.AtMostSixtyVisibleElementsInTheFiveWorstCase` (los 5 perfiles); cada panel es un elemento visible y sus 7 hijos van ocultos (`UiQuery.IsShown`) | Por código, un elemento por panel. El número (56 / 57) es del informe de WP-B: no se corrió acá |
| Deuda 4b: el precalentado | `AppRoot.Start` (después de `_app.Start()`), `Update` (lo desarma), `OnDestroy` | Sin hallazgos. `I5D.AppRootWarmsUpTheHudAfterStartingTheApp` en verde |
| Deuda 4b: el armado en el negro | `ImmersiveWorkoutPage.BuildView` con `MotionDriver.StartingAt(_openedAt)`; qué arranca adentro del alcance | Sin hallazgos: dentro del alcance todo lo de los paneles es `Instant` (el primer `Apply`, el primer `SetLayout`, el `OnExpansion` del primer `Bind`), así que `StartingAt` no corre ninguna fase de los paneles |
| Deuda 4b: P-17 | `SuspendAccounting` (el registro de cada aviso, cada despertar y el salto de pared sin evidencia), `WindowsSleepSource` (la hora de cada aviso con `GetTickCount64`, protegida), `UnityScheduler.Clock` (contadores antes que la duración) | Sin bug. dotnet: las 8 nuevas en verde. Observación O-6 (Windows manda dos "reanuda" por despertar) |
| Deuda 4b: O-4 y O-5 | `MotionDriver.Tick` (`try`/`catch` por animación), `ImmersiveWorkoutPage.OpenCompletion` | O-5 bien. O-4 aísla, pero deja colgada la `HudMotion` que lanzó (LM5-03) |

---

## 3. Issues

| ID | Sev. | Resp. | Issue | Evidencia | Corrección sugerida |
|---|---|---|---|---|---|
| **LM5-01** | **mayor** | architect | En `ColumnScroll` (TH, L640) el desplazamiento de la columna no se recorta cuando el contenido se achica por un dato. Con la columna desplazada (para ver `Series anteriores` en TH hay que desplazarla), un cambio que achica la columna deja los paneles corridos arriba del techo: en `Series 600`, al terminar la serie, **parciales queda entero arriba del techo, escondido por la máscara**, repeticiones se fue, **el arrastre y la rueda no hacen nada** (el tope nuevo es 0) y el panel no se puede tocar (su caja está fuera de la ventana de la columna). Queda así hasta una rotación o un cambio de disposición. Dos síntomas del mismo mecanismo: (b) los recortes que sí existen son **en el acto** y saltan mientras el vidrio todavía anima; (c) DD5-27 no se aplica en `ColumnScroll` | `Presentation/Workout/SeriesColumn.cs:211-212` (`Apply`: `Compute` y `PlaceBoth`, sin `ClampScroll`), `:403-407` y `:433-437` (`OnDragged`/`OnWheeled` devuelven `false` con el tope ≤ 0,001), `:446-454` (`ClampScroll` solo desde `OnExpansion` `:311` y `Replace` `:392`, y con `WheelTo(max, true)`), `:540-542` (la traslación usa `ScrollBase` sin recortar). `SeriesPanelView.cs:323-331` (la compensación de DD5-27 solo con `_rowsScrollRule`), `:358-361` y `:375` (las filas a 0 o al tope en el acto). Sonda (§5): TH, contenido 180 → 84 base, tope 86,1 → 0, parciales de 120,1 a 208,7 dp con el techo en 210,9; L640, de 105,2 a 182,8 con el techo en 184,7. Spec AC-WKT-18-13 ("lo que se ve no salta"; "en TH y L640 … se ven sin desplazar su cabecera"); 04-design-notes §5.2 (`:307` "dentro de sus topes", `:309` DD5-27). SCT `ColumnScrollInLandscape` no cambia el contenido con la columna desplazada | En `SeriesColumn.Apply`, después de `Compute`/`PlaceBoth`, recortar el desplazamiento al tope nuevo, **sin salto**: con la curva del alto por un dato (`DataResize`, 80-240) o con `spring.snappy`, de modo que el borde de abajo del contenido acompañe el achique; mismo criterio en `OnExpansion` (al cerrar con la columna al tope, hoy salta 48 base = 50,6 dp en TH en el cuadro del toque) y en las filas de `RowsScroll`. Que el Designer confirme si DD5-27 vale para `ColumnScroll` (§5.2 lo dice para los dos) y, si vale, `Motion.ShiftScroll` en la columna. Prueba nueva en SCT: TH, los dos abiertos a 380,2 s, arrastrar al tope, avanzar a 541,2 s → `ScrollBase ≤ ScrollMaxBase`, la cabecera de parciales a la vista y elegible por `panel.Pick` |
| LM5-02 | menor | architect (designer) | El anillo de foco no respeta la máscara de la columna. En TH y L640 con los dos abiertos (CAP-16), repeticiones queda entero debajo del piso; Tab llega igual (es `focusable` y `canGrabFocus` no mira la máscara) y el anillo blanco y su halo se dibujan enteros **sobre la banda** (tiempo │ distancia), y Enter abre o cierra un panel que no se ve. Lo mismo con parciales abierto y vueltas: la parte del anillo debajo del piso | `SeriesPanelView.cs:927-930` (el anillo con solo `HasFocusRing`), `:1443-1460` (colores enteros, sin `ColumnMaskAlpha`), `:1261-1265` (la máscara solo para los renglones) y `:192` (`FocusInEvent` solo repinta: nada lleva el panel a la vista). `ImmersiveWorkoutPage.cs:787-795` y `:823` (el anillo de Tab con repeticiones si `RepsShown`). Sonda (§5): TH, repeticiones de 314,1 a 400,6 dp con el piso en 309,8 y la banda desde 318,3 (ancla abajo a la izquierda en x 14,8); anillo y halo de 306,1 a 408,6 dp en x 14,8-159,1. 04-design-notes §5.3 (`:326`: "más allá del borde de la columna están SPM y la banda… tiene que apagarse"). SKT solo prueba W1280 y TV | Al enfocar un panel en `ColumnScroll`, desplazar la columna hasta que su cabecera quede a la vista (`WheelTo` con `spring.snappy`) y aplicar al anillo la misma máscara de la regla 3 (o recortarlo a la ventana de la columna). Prueba en SKT con TH |
| LM5-03 | menor | architect | O-4 (deuda 4b): cuando una `HudMotion` lanza en `Advance`, `MotionDriver` la saca de su lista, pero su `_inDriver` queda en `true`. Desde ahí `Run()` vuelve sin agregarla (nunca más anima: un panel que lanzó una vez queda congelado en el primer cuadro de cada animación siguiente) e `IsRunning` dice `true` para siempre (`SeriesColumn.IsAnimating`, `RemainingDp` y la regla de toma esperan sin fin). Se recupera recién con `Stop()` (la salida) | `Presentation/Motion/Hud/HudMotion.cs:56` (la excepción sale antes de `_inDriver = false`, `:65`) y `:89-91` (`Run` vuelve si `_inDriver`); `Presentation/Motion/MotionDriver.cs:239-255` (saca el elemento sin avisarle). MDI (`Tests/UnityEditMode/MotionDriverIsolationTests.cs`) usa un `IMotion` crudo, no una `HudMotion` | En `HudMotion.Tick`, `try`/`catch` alrededor de `Advance` que deje `_inDriver = false` (y su generación) y vuelva a lanzar, o un aviso del *driver* a la animación que saca. Prueba MDI con una `HudMotion` que lanza una vez: después `Run()` la vuelve a agregar y avanza, e `IsRunning` es `false` en el medio |
| LM5-04 | menor | architect | La medida de 0 B con repintado (DT5-18) cubre menos de lo que dice el diseño: 5 escenas (abrir, la vuelta nueva, el cambio por pieza, la repetición nueva, cerrar), solo en TV, de 18 cuadros. Faltan **la serie que empieza, la rueda en W1280 y la máscara de `ColumnScroll` en TH** (el camino de `DrawMaskedGlass` con `Allocate` nunca se mide con repintado), el anillo de foco, la capa de *hover* y el apagado de la marca (600-1400 ms: la ventana de la vuelta nueva termina a los 300). El comentario de la prueba habla de una línea de base que el código no resta (afirma 0, que es más estricto) | `Tests/PlayMode/HudBudgetTests.cs:221-279` (`Frames18`; `CreateWorkout` con el perfil por defecto, TV). 02-tech-design §4.5 (`:837`: "30 cuadros reales de cada animación: abrir, cerrar, la vuelta nueva con la marca, el cambio de contenido por pieza, la serie que empieza, la rueda en W1280 y la máscara de `ColumnScroll` en TH") y §3.6 (`:742`). Revisado a mano, `Walk` no asigna en esas ramas: es un hueco de prueba, no un bug | Sumar a HBT las tres escenas que faltan (una por perfil), una ventana que cruce 600-1400 ms de la marca y una con el anillo de foco en modo teclado; 30 cuadros como dice el diseño; el comentario al día |
| LM5-05 | menor | architect | Ninguna prueba fija que, al salir del inmersivo, su HUD deje de estar suscripto al `State` de `AppContext.SeriesPanels` (uno por app: una suscripción que quedara retendría el HUD entero). Hoy está bien por el orden de `PageStack` (saca la raíz antes de `OnPopped`) y porque `Bind` se da de baja al soltarse; si ese orden cambia, nadie lo ve | `Tests/PlayMode/ImmersiveTransitionTests.cs:299-350` (mira `ActiveCount` y `Demands`, no `SubscriberCount`); `Tests/PlayMode/FourBDebtTests.cs:32`, `:70` (lo mira solo para el precalentado); `Presentation/Shell/PageStack.cs:221-241` | En `ITT.NothingAnimatesAfterTheExitWithThePanels`, afirmar `H.App.SeriesPanels.SubscriberCount` igual al de reposo después de la salida (y con dos salidas seguidas) |

---

## 4. Detalle

### 4.1 LM5-01: la columna que se achica con la vista desplazada

**El camino.** En TH y L640 la columna entera se desplaza (`SeriesFitRule.ColumnScroll`). `SeriesColumn.Compose` traslada los dos paneles con `− ScrollBase · sp` (`SeriesColumn.cs:540-542`) y `UpdateMask` esconde con la regla 3 lo que pasa el techo o el piso. El tope (`ScrollMaxBase`, `:101`) sale de `_columnContent`, que `Compute` recalcula en cada `Apply`. Pero `Apply` (`:159-234`) **nunca** recorta el desplazamiento: solo lo hacen `OnExpansion` (un toque) y `Replace` (un cambio de disposición de `HudZones`). Y si el tope nuevo es 0, `OnDragged` y `OnWheeled` (`:398-444`) devuelven `false` sin tocar nada.

**El escenario (TH, `Series 600`, los dos abiertos).** Es CAP-16: a los 380 s, parciales mide 90 base y repeticiones 82, la columna 180 contra 93,9 de lugar, y `Series anteriores` empieza debajo del piso. Para verlo, el usuario arrastra la columna hasta el tope (86,1 base), como hace SCT `ColumnScrollInLandscape`. A los 541 s termina la serie: repeticiones se va (`_repsShown = false`) y parciales pasa al vacío sin la línea `Rep` (84 base). El contenido baja a 84 y el tope a 0, pero el desplazamiento sigue en 86,1:

| | TH | L640 |
|---|---|---|
| Contenido antes → después (base) | 180,0 → 84,0 | 180,0 → 84,0 |
| Tope antes → después (base) | 86,1 → 0,0 | 86,1 → 0,0 |
| Parciales después (dp, con el desplazamiento que queda) | de 120,1 a 208,7 | de 105,2 a 182,8 |
| Techo de la columna (dp) | 210,9 | 184,7 |
| ¿El arrastre o la rueda lo corrigen? | no (`max ≤ 0,001`) | no |

Parciales queda **entero arriba del techo**: la máscara lo pone en 0 y no se ve; su caja cae fuera de la ventana de la columna, así que `ContainsPoint` no lo toma (`SeriesPanelView.cs:627-631`). En `Series 600` no hay objetivos de SPM, así que `HudZones` no rehace la disposición en el cambio de paso. La columna queda vacía todo el enfriamiento y detrás del diálogo de completado, que según AC-WKT-18-20 debería mostrar `Sin parciales en este paso`. Solo la devuelven una rotación, un cambio de disposición (otra cifra en un valor) o un Enter desde el teclado, que en TH casi no hay.

Pasa lo mismo, en menos, en cualquier cambio que achique la columna con la vista desplazada. Con `max' > 0`, la columna queda con un hueco abajo y el primer arrastre salta hasta el tope.

**(b) Los recortes que sí existen saltan.** `ClampScroll` usa `WheelTo(max, true)`. Un ejemplo: en TH, con la columna al tope, se cierra repeticiones (que ahí sí se ve y se toca). El tope pasa de 86,1 a 38,1 y toda la columna baja **50,6 dp en el cuadro del toque**, mientras el vidrio de repeticiones recién empieza su resorte. En W1280 pasa lo mismo con las filas: `SetExpanded(false)` las lleva a 0 en el acto (`SeriesPanelView.cs:358-361`) mientras el contenido todavía está entero (se apaga en 0-120), y `Retarget` las recorta en el acto (`:375`).

**(c) DD5-27 solo en `RowsScroll`.** 04-design-notes §5.2 (`:309`) lo pide para el desplazamiento en general, pero `SeriesPanelView.Apply` compensa solo con `_rowsScrollRule` (`:323-331`), y la columna nunca llama a `Motion.ShiftScroll`. En TH, con la columna desplazada, cada vuelta nueva baja lo que se ve con el empuje.

### 4.2 LM5-02: el anillo de foco en `ColumnScroll`

`HasFocusRing` (`SeriesPanelView.cs:242`) solo mira si el panel responde, si tiene el foco y si es modo teclado. `DrawFocusRing` (`:1443-1460`) traza el blanco y el halo con colores enteros alrededor de todo el vidrio, `G` incluido. La regla 3 se aplica a los renglones (`RowAlpha`, `:1261-1265`) y al vidrio (`DrawMaskedGlass`), pero no al anillo. En CAP-16 (TH, desplazamiento 0) el Tab llega a repeticiones, que está entero debajo del piso: `ImmersiveWorkoutPage.MoveFocus` lo pone en el anillo con `RepsShown`, y `canGrabFocus` no sabe de la máscara. Así quedan dos rectángulos redondeados de 2 y 4 dp de 306,1 a 408,6 dp, en x 14,8-159,1, encima de la banda, que en TH empieza en 318,3 con su ancla abajo a la izquierda en x 14,8. Además, Enter abre o cierra y guarda un panel que no se ve. AC-WKT-18-12 pide que no quede un foco invisible y AC-WKT-18-16, que nada se encime con la banda. En la PC se llega con una ventana corta y ancha.

### 4.3 LM5-03: la `HudMotion` que lanzó queda colgada

`HudMotion.Tick` hace `var keep = Advance(now)` y recién después `_inDriver = false`. Si `Advance` lanza (o lanza su *callback*, por ejemplo `OnVisual` → `SeriesColumn.Compose`), la excepción sale antes, y `MotionDriver` (O-4) atrapa, cuenta, registra y anula el elemento. Pero la animación sigue creyendo que está adentro:

- `Run()` vuelve en `if (_inDriver)` y nunca la vuelve a agregar. Cada `SetExpanded`, `ContentChanged` o `Hover` aplica solo el cuadro de la llamada: un panel que lanzó una vez ya no abre ni cierra a la vista, aunque la preferencia cambie y se guarde.
- `IsRunning` queda en `true`, y con él `SeriesColumn.IsAnimating` y `RemainingDp` (la regla de toma de la 4b espera sin fin).

Se arregla solo con `Stop()` (la salida, `Leave` pone `_inDriver = false`). AC-WKT-13-16 se cumple ("las demás siguen y la salida termina"). Lo que falla es que la aislada no se recupera y miente sobre su estado. Vale para todas las `HudMotion` (también las de la 4b: kcal, el velo, la cascada…).

---

## 5. La sonda de la lente

`<scratch>/it5/lens-motion/probe` (fuera del repo): un proyecto NUnit propio contra los `RowMate.Core.dll` y `RowMate.Tests.Support.dll` de la corrida de dotnet de arriba. `SeriesColumn` es de `Presentation` y no compila en dotnet. Por eso la sonda usa la geometría real (`HudGeometry.Layout`, `SeriesPanelGeometry.Column` y `Place`, las alturas naturales) y modela de `SeriesColumn` solo las tres líneas de §4.1: la traslación con `ScrollBase`, `Apply` sin recorte y la guarda de `OnDragged`/`OnWheeled`. Los casos que fallan afirman lo esperado; la falla **es** el defecto. Salida de la consola:

```
TH: contenido 180.0 -> 84.0 base; lugar 93.9; tope 86.1 -> 0.0; desplazamiento que queda 86.1; parciales de 120.1 a 208.7 dp con el techo de la columna en 210.9 y el piso en 309.8; el arrastre o la rueda lo corrigen: False
  Failed SeriesSixHundredScrolledToTheEndThenTheSeriesEnds("TH")
     TH: la cabecera de parciales queda arriba del techo de la columna (escondida por la máscara) y nada la devuelve
  Expected: greater than 210.86829230769231d
  But was:  155.96923076923071d
L640: contenido 180.0 -> 84.0 base; lugar 93.9; tope 86.1 -> 0.0; desplazamiento que queda 86.1; parciales de 105.2 a 182.8 dp con el techo de la columna en 184.7 y el piso en 271.4; el arrastre o la rueda lo corrigen: False
  Failed SeriesSixHundredScrolledToTheEndThenTheSeriesEnds("L640")
  Expected: greater than 184.70215384615383d
  But was:  136.61538461538464d
TH (CAP-16): repeticiones de 314.1 a 400.6 dp; piso 309.8; banda desde 318.3; anillo y halo de 306.1 a 408.6 dp, en x 14.8-159.1; alto de la ventana 411
  Failed TheFocusRingOfTheRepsPanelBelowTheFloor("TH")
  Expected: less than or equal to 309.83076923076919d
  But was:  408.56060000000002d
L640 (CAP-16): repeticiones de 275.2 a 350.9 dp; piso 271.4; banda desde 278.8; anillo y halo de 267.2 a 358.9 dp, en x 12.9-139.4; alto de la ventana 360
  Failed TheFocusRingOfTheRepsPanelBelowTheFloor("L640")
TH: banda con ancla (14.8; 394.1) esquina BottomLeft, ancho máx. 228.8 base, escala 1.054
L640: banda con ancla (12.9; 345.2) esquina BottomLeft, ancho máx. 178.7 base, escala 0.923
Total tests: 6 · Passed: 2 · Failed: 4
```

Las pruebas PlayMode que lo fijarían de verdad (para quien tenga el Editor) están descritas en la última columna de LM5-01 y LM5-02.

---

## 6. Matriz AC → evidencia → resultado (lo de esta lente)

"dotnet" = corrido en esta lente. "Revisión" = estática. Lo de Unity no se corrió acá: el resultado de esas pruebas es del QA principal.

| AC | Qué mira la lente | Evidencia | Resultado |
|---|---|---|---|
| AC-WKT-13-09 | Nada animando ni retenido al salir | Revisión (§2, ROB-02); ITT `NothingAnimatesAfterTheExitWithThePanels` (no corrida) | Revisión **sin bug**; prueba **NO VERIFICABLE** acá; hueco de prueba LM5-05 |
| AC-WKT-13-14 | La presentación no toca el dato | dotnet `PresentationIndependenceTests` 3 / 3 (con `Series 600` y las preferencias cambiando) | **PASA** (dotnet); la parte PlayMode, NO VERIFICABLE |
| AC-WKT-13-15 | La entrada sin tirón | Revisión del precalentado y del armado en el negro | Revisión sin hallazgos; `[PERF]` **NO VERIFICABLE** (build) |
| AC-WKT-13-16 | Una falla de la presentación no traba la sesión | Revisión de O-4 y O-5 | O-5 bien; O-4 con **LM5-03** (menor); MDI y 4BD NO VERIFICABLE acá |
| AC-WKT-02-11 | P-17 y el registro de cada despertar | dotnet `SuspendAccountingTests` (las 8 nuevas en verde) | **PASA** (dotnet, el registro); la demo (c), NO VERIFICABLE |
| AC-WKT-18-08 | Cuándo cambian los valores | dotnet `SeriesPanelsTests`, `WorkoutHudStoreTests`, `HudSeriesChangeTests`; revisión de `Apply` | **PASA** (dotnet); PlayMode NO VERIFICABLE |
| AC-WKT-18-09 | Un toque abre o cierra | Revisión (`ContainsPoint`, el hijo de toque, el umbral, `Responds`) | Revisión sin bug, salvo el panel sin toque de **LM5-01** |
| AC-WKT-18-10 / -11 | Se recuerda; las preferencias a salvo | dotnet `JsonFilePreferencesTests` 15 / 15, `SeriesPanelPrefsTests` 9 / 9; revisión de `AppRoot`, `DataLocation`, `HudWarmup` | **PASA** (dotnet); el reinicio del build, NO VERIFICABLE |
| AC-WKT-18-12 / AC-APP-05-07 | Teclado y foco | Revisión; sonda | **FALLA** en `ColumnScroll` (**LM5-02**, menor); el resto, NO VERIFICABLE (SKT) |
| AC-WKT-18-13 | El lugar y el desplazamiento | Revisión; sonda | **FALLA** (**LM5-01**, mayor) |
| AC-WKT-18-18 / -19 | Abrir, cerrar y la vuelta nueva | dotnet STL 18 / 18 (las curvas y sus finales); revisión de las motions | Curvas **PASA** (dotnet); los saltos en el acto de **LM5-01** (b); SMT NO VERIFICABLE acá |
| AC-WKT-18-20 | Pausa, rotación y fin | Revisión (`Instant` en la rotación; inerte con la rutina terminada) | Revisión sin bug propio; con **LM5-01** la columna puede llegar vacía al diálogo de completado |
| AC-WKT-18-21 | 0 B por cuadro y presupuesto | Revisión de `FrameAllocations`, FAT y HBT | Método correcto; cobertura corta (**LM5-04**); números NO VERIFICABLE acá |
| AC-APP-05-08 | Reducir movimiento | dotnet STL (las variantes `reduced`); revisión de `FinishForReducedMotion` | **PASA** (dotnet); VID-08 NO VERIFICABLE acá |
| AC-HIS-20-04 | Ninguna prueba toca las preferencias del usuario | Revisión: memoria por anfitrión y por toma; JFP en una carpeta temporal; `RunPerfSeries` solo con `ROWMATE_DATA_DIR`; esta lente no abrió Unity | Revisión **sin bug**; la comparación del archivo del usuario la hace el QA principal |

---

## 7. Observaciones (sin issue)

- **O-1 · Dos procesos con el mismo `prefs.json`.** Pasa si se juegan a la vez el Editor sin `ROWMATE_DATA_DIR` y el build de simulador, o el build normal y el de simulador. Cada proceso guarda su copia entera en memoria, así que gana el último que escribe y se pisa la clave que cambió el otro. Además los dos usan el mismo temporal, `prefs.json.tmp`, y una escritura que falla borra un temporal que puede ser del otro (`JsonFilePreferences.cs:374-388`). Es la conducta del legacy (`shared_preferences` reescribe el mapa entero) y lo acepta P-11. Queda anotado para el ADR 021.
- **O-2 · Espacio.** La guarda de 50 ms (`SeriesPanelView.cs:1517`) cubre el orden `KeyDown` → `NavigationSubmit`, no el inverso; S-B5 vio que en esta app Espacio no manda `NavigationSubmit`. Mantener Espacio apretado abre y cierra con la repetición del teclado, como `RmButton`.
- **O-3 · *Hover* con un diálogo abierto desde el teclado.** Si el mouse está quieto sobre el panel y se abre el diálogo de terminar con Escape, `Responds` impide un *hover* nuevo, pero no apaga el que había (04-design-notes `:262` pide que se apague en 160). Queda debajo del velo. No se puede verificar sin Unity si UI Toolkit manda `PointerLeave` cuando aparece el velo.
- **O-4 · `DrawnFont.CharStrings`.** Precalcula 8231 cadenas de un carácter (hasta U+2026, unos 300 KB) en el primer `DrawnFont`, durante el precalentado. Es memoria fija, no asigna por cuadro. Un carácter más alto (por ejemplo el signo menos U+2212) asignaría en cada dibujo; hoy ningún texto de los paneles lo usa.
- **O-5 · `Remeasure` una vez por segundo.** Rehace el corte de los vacíos (`string.Split`/`Join`) aunque el texto no cambió. Lo permite §20.2 (1 Hz), pero se podría guardar.
- **O-6 · P-17 en Windows.** Un despertar pedido por el usuario manda `PBT_APMRESUMEAUTOMATIC` y `PBT_APMRESUMESUSPEND`: el contador sube de a 2 y el registro dirá `reanuda (2)`, no `(1)` como el ejemplo de 00-deuda-4b §1.3. `SignalAsleepSeconds` usa el segundo aviso. Es solo el registro, pero conviene decirlo en las instrucciones de la demo (c).

---

## 8. NO VERIFICABLE en esta lente (y por qué)

- **Todo lo de Unity** (la tarea es sin Unity): SMT, SPV, SCT, SKT, HBT (0 B, el presupuesto, `SeriesWorstCaseWork`), FAT, ITT, 4BD, MDI, PFU, `BootScene*`, las capturas y los videos de `it5`.
- **El build** (`[PERF]` de AC-WKT-13-15 y de AC-WKT-18-21, el reinicio con las preferencias): no se compiló.
- **Android e IL2CPP**: no están instalados. Incluye `File.Replace` sobre la carpeta interna con IL2CPP.
- **La demo (c) de P-17**: la hace el usuario con una suspensión real.
