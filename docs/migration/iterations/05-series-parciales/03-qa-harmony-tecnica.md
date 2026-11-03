# It. 5 — Series en vivo: parciales de 500 m y repeticiones: gate de armonía de QA (lente técnica)

| Campo | Valor |
|---|---|
| Autor | rowmate-qa |
| Fecha | 2026-10-30 |
| Iteración | 5 · `05-series-parciales` ([ROADMAP §It. 5](../../ROADMAP.md)) |
| Lente | **Técnica**: cobertura AC → componente → test o captura; trackers puros y deterministas con el motor y el oráculo; preferencias persistidas; presupuesto del HUD con los paneles (R4a-03) y convivencia con la cuenta grande y el sello; motion que nunca toca el dato; nada animando al salir (ROB-02); 0 B por cuadro; reducir movimiento; transiciones en *batchmode* y estados intermedios; ciclo de vida de UI Toolkit; la deuda de la 4b; paquetes disjuntos y contrato USS; contradicciones spec ↔ diseño ↔ notas; regla de repo público. El aspecto y el texto de producto los miran las otras lentes |
| Entrada | [01-po-spec](01-po-spec.md) (74 AC, LT-47 a LT-61, 26 CAP, 12 DEMO, 9 VID), [02-tech-design](02-tech-design.md) (DT5-01 a DT5-13, SD-01 a SD-11), [04-design-notes](04-design-notes.md) (R-01 a R-15, DD5-13 a DD5-27, SD-12 a SD-14), [00-deuda-4b](00-deuda-4b.md), [00-deuda-diseno](00-deuda-diseno.md); [05-qa-verification de la 4b](../04b-hud-exr/05-qa-verification.md) y [03-qa-harmony-tecnica de la 4b](../04b-hud-exr/03-qa-harmony-tecnica.md) (lecciones) |
| Legacy (solo lectura, `3ae0f3a`) | [rowmate-legacy](https://github.com/figuibej/rowmate-legacy): `lib/features/workout/series_tracker.dart` y `series_panels.dart` (enteros), `workout_provider.dart:30-328`, `immersive_workout_screen.dart:85-120` y `:590-700`, `lib/core/models/routine.dart:15-110`, `test/series_tracker_test.dart` y `test/series_panels_test.dart` (enteros). No se leyó `lib/core/strava/strava_secrets.dart`. `git -C ../rowmate-legacy status --porcelain` vacío antes y después |
| Código leído (este repo) | `Core/Workout/{SanitizedDistance,WorkoutEngine,WorkoutFormats,WorkoutStartRule}.cs`, `Core/Design/DigitRoll.cs`, `Core/Common/FrameScheduler.cs`, `Bootstrap/DataLocation.cs`, `Presentation/Controls/{TabularNumber,RmButton}.cs`, `Presentation/Styles/Base.uss` (foco), `Tests/Support/{ScriptedRower,RowerScripts,RoutineSeeds}.cs`, `Tests/UiSupport/{GcAllocations,UiTestHost,UiQuery}.cs`, `Tests/PlayMode/HudBudgetTests.cs`, `Tests/EditMode/Workout/WorkoutSuspendTests.cs`, `Snapshots/HudExrCatalog.cs`, `tools/unity/run-unity-tests.ps1`, el *manifest* de `it4a` |
| Verificación hecha | `dotnet test` de línea base (§1); un puerto propio en Python del `SeriesTracker` y de la regla de pasos de la app, con el modelo de tiempo de spec §0.4, independiente del `sim.py` del Architect (`<scratch>/it5/gate-tecnica/qa_sim.py`, `qa_sim2.py`); el cruce de la matriz AC con un *script* (`<scratch>/it5/gate-tecnica/coverage.py`). **Unity no se abrió** (la tarea es sin Unity). Sin *commits* |

---

## 0. Veredicto

**APROBADO CON OBSERVACIONES.** **0 bloqueantes, 5 mayores, 11 menores** (§14).

- **Cobertura.** Los 74 AC de la spec están en la matriz de 02-tech-design §6.5: 0 faltan y 0 sobran. Ninguno queda solo con **eq** o **qa**. Los 21 sin **d** ni **s** son los `UnityOnlyIds` que tiene que llevar `It05Ids` (§2). Lo que el Architect declara sin test está justificado (§2.2).
- **Fidelidad.** Recalculé 30 casos con un puerto propio del tracker y de la regla de pasos de la app (§3). Todos coinciden con la spec, con el diseño y con las afirmaciones del legacy, al número y al carácter.
- **El núcleo técnico está bien armado.** El saneamiento es único y está fijado por una regla de código. El tracker se alimenta en el segundo, antes del fin del paso, como `workout_provider.dart:176-186`. DV-48 queda como regla de presentación, encima del tracker crudo. El oráculo compara dos cosas: el tracker con las entradas del guion y que el motor le dé esas mismas entradas (DT5-07). Es lo correcto con DV-26.
- **Lo que más pesa:**
  1. Las 04-design-notes se escribieron **después** del diseño técnico y no están integradas. Es la tercera iteración seguida (QA-G4aT-01, QA-G4bT-01). Cambian líneas de tiempo, el contrato de motion, constantes y comprobaciones del arnés de **WP-A**, el primer paquete (QA-G5T-01).
  2. La spec dice lo contrario de lo que se construye en 14 puntos (SD-01 a SD-14) y en 4 más que nadie listó. La demo (b) no se puede cumplir: con 137 la cuenta grande aparece siempre (QA-G5T-02).
  3. La medición de 0 B por cuadro deja afuera el repintado, que en la 5 concentra casi todo el trabajo (QA-G5T-03).
  4. La selección de UI Toolkit descarta un elemento si el punto cae fuera de su caja, antes de llamar a `ContainsPoint`. El área de toque agrandada, el FLIP dibujado y el desplazamiento dibujado de la columna caen fuera de la caja (QA-G5T-04).
  5. El precalentado con los paneles abiertos no dice cómo los abre sin escribir las preferencias del usuario (QA-G5T-05).

**Qué cerrar antes de cada paquete:**

| Antes de | Issues |
|---|---|
| WP-A | QA-G5T-01 (integrar las notas), QA-G5T-02 (el PO decide SD-01 a SD-14 y los cuatro que faltan; la constante que cambie se cambia una vez), QA-G5T-06, -07 |
| WP-B0 | QA-G5T-10 (los archivos de la junta), QA-G5T-13, -14 |
| WP-B1 | QA-G5T-03 y QA-G5T-04 (en la sonda S-B5), QA-G5T-09, -11, -12, -16 |
| WP-B2 | QA-G5T-05 |
| Commit de la iteración | QA-G5T-15 |
| PO (sin código) | QA-G5T-02, -08, -14 |

---

## 1. Evidencia corrida

| Qué | Resultado |
|---|---|
| `powershell -NoProfile -File tools/dotnet/run-tests.ps1 -ArtifactsPath <scratch>/it5/gate-tecnica/dotnet` (línea base, con la deuda de la 4b sin *commit*) | `Build succeeded` · `0 Warning(s)` · `0 Error(s)` · herramientas **Total tests: 182** · Core **Total tests: 2332 · Passed: 2305 · Skipped: 27** · 0 fallan · `exit=0`. Coincide con 00-deuda-4b §6 corrida 1 |
| Puerto propio del tracker (`qa_sim.py`) | 30 casos de §3, todos iguales a la spec |
| Cobertura (`coverage.py`) | spec 74 IDs, matriz 74, faltan `[]`, sobran `[]`, solo eq/qa `[]`, 21 sin d ni s |
| `git -C ../rowmate-legacy status --porcelain` | vacío al empezar y al terminar |
| Procesos (solo lectura) | el `RowMate.exe` del usuario está abierto y su Editor de Unity también. No se tocó ninguno de los dos |

---

## 2. Cobertura: AC → componente → test

### 2.1 Matriz

Las 74 filas de 02-tech-design §6.5 tienen un componente en §1.1 y por lo menos un test de dotnet, de Unity EditMode o de PlayMode. Los **21 IDs sin d ni s**, que son los `UnityOnlyIds` de `It05Ids` (02-tech-design no los enumera; conviene escribirlos):

AC-APP-05-05, AC-APP-05-07, AC-BLE-16-03, AC-WKT-02-08, AC-WKT-13-07, -13-08, -13-09, -13-12, -13-15, -13-16, AC-WKT-16-07, AC-WKT-17-01, -17-06, -17-07, -17-10, AC-WKT-18-02, -18-03, -18-12, -18-17, -18-20, -18-21.

Huecos de calidad, no de cobertura:

- **AC-WKT-07-10, el salto al despertar**: la prueba no lo puede ver con el remo de prueba de hoy (QA-G5T-06).
- **AC-WKT-18-12, Espacio**: `NavigationSubmitEvent` no lo manda solo (QA-G5T-09).
- **AC-WKT-18-17, más allá de `S-LARGO`**: el recorte con `…` no tiene caso (QA-G5T-16).
- **AC-WKT-18-21, 0 B por cuadro**: no mide el repintado (QA-G5T-03).

### 2.2 Lo que el Architect declara sin test

| Parte | Mi lectura |
|---|---|
| Ninguno de los 74 queda solo con equipo o QA | **Cierto** (`coverage.py`) |
| [PERF] de AC-WKT-18-21 y AC-WKT-13-15 en el build de Windows | Justificado: necesita el *player*. Hoy está **bloqueado**, porque el `RowMate.exe` del usuario sigue abierto (QA-G5T-14) |
| AC-WKT-18-10: cerrar y abrir el build real | Justificado. `BST.PreferencesSurviveARestart` cubre lo mismo en el Editor (dos `AppRoot` sobre la misma carpeta) |
| AC-WKT-18-12 / AC-APP-05-07 con el teclado real | Justificado. Hay un hueco en la parte automática (QA-G5T-09) |
| Device-And: el arrastre con el dedo, el área táctil, el primer cuadro al volver, el reemplazo atómico con IL2CPP (R5-04) | Justificado: no están el módulo ni el teléfono |
| AC-WKT-02-11: lo verifica el usuario (demo (c) de 00-deuda-4b §1.5) | Justificado: hace falta suspender el equipo de verdad, y QA no lo suspende |

---

## 3. Fidelidad: casos del legacy recalculados

Puerto propio de `series_tracker.dart:36-183`, línea por línea y con el mismo orden de coma flotante. La regla de pasos es la del motor de la app (`WorkoutEngine.cs:421-496`: los metros saneados en el paso por distancia, DV-26). El modelo de tiempo es el de la spec §0.4: `L(k)` en `k − 0,5`, y la pausa pierde la fracción (Q-58).

| # | Caso | Spec / diseño | QA (puerto) | Legacy |
|---|---|---|---|---|
| 1 | LT-47 `R-3`, 170 s | `#1 2:46.7`, `0:03.3` `10 m` | 166,667 s · `2:46.7` · `0:03.3` `10 m` | `test/series_tracker_test.dart:46-57` |
| 2 | LT-55, lectura 0 | 2 vueltas de 125 ± 2; trabajo de la rep 1 entre 1060 y 1110 | [125,0; 125,0]; **1096** | `:155-178` |
| 3 | LT-56, primer salto de 5000 | 1 vuelta de 125 ± 2 | 1 vuelta de **126,0** s (dentro de ± 2) | `:180-189` |
| 4 | LT-58, `rebase` | 20 → 24 | 20 → 24 | `:211-220` |
| 5 | `Series 600`, 170 s | `0:15.0` `60 m`, `#1 2:05.0`, `Rep 1/3 · 560 m` | igual | `series_panels.dart:286-317` |
| 6 | `Series 600`, 180 s (P-01) | `0:25.0` `100 m`, `#1`, `Rep 1/3 · 600 m` | igual (la barra ya en el paso 2) | `workout_provider.dart:176-186` |
| 7 | `Series 600`, 181 / 190 / 197 s | `Sin parciales…`; `604` / `640` / `668 m` | igual | `series_tracker.dart:178` |
| 8 | `Series 600`, 201 s | `Rep 1` 680 / 600 / 150 s / `2:05`; `Rep 2/3 · 4 m` | igual | `:153-166` |
| 9 | `Series 600`, 250 s | `0:50.0` `200 m`, `Rep 2/3 · 200 m` | igual | — |
| 10 | `Series 600`, 325 s (VID-02) | `0:00.0` `0 m`, `Rep 2/3 · 500 m` | igual | `:133-140` |
| 11 | `Series 600`, 371 / 380 s | [`Rep 2`, `Rep 1`]; `0:10.0` `40 m` | igual | `:157-166` |
| 12 | `Series 600`, 541 s | fuera de la serie, lista vacía | igual | `:167-170` |
| 13 | `Trabajo 1000` con `R-4` | vueltas en 125 y 250; fin en 250 | igual | — |
| 14 | `R-4R` | fin en 251; `#1` 126 s | fin en 251; [126,0; 125,0] | `:120-127` |
| 15 | `R-4Z` | fin en 252; `#1` 127 s | fin en 252; [127,0; 125,0] | ídem |
| 16 | `R-4D` | fin en 254; `#1` 129 s | fin en 254; [129,0; 125,0] | ídem |
| 17 | `Series 600` con `R-4D` | 154 s y 600 m → `2:08` | `Rep 1 2:08 680 m` (w 600, 154 s) a los 205 s; el trabajo termina en 184 | `:31-33` |
| 18 | `Fondo 3000` con `R-4A`, 600 s | `0:50.0` `250 m`; `#5 #4 #3 1:40.0`, `#2 2:05.0` | igual | `series_panels.dart:279-282` |
| 19 | `Cinco reps`, 541 / 545 s | [4, 3, 2] con `540 m` y `2:05`; `0:05.0` `20 m` | igual | `series_tracker.dart:166` |
| 20 | `Dos series`, 120 / 121 / 181 / 241 s | `Rep 1` de A (`2:05`, 240 m); vacía; `Rep 1` de B; sin serie | igual | `:149-170` |
| 21 | `Pirámide 500`, 879 s | `Rep 1` 1112 / 752 / 188 s / `2:05` | igual; `1.112 m`. A los 881: `0:03.0` `12 m` (DT5-04) | `:161-163` |
| 22 | `Prueba 4a`, 90 / 121 s (CAP-24) | `1:00.0` `240 m`, `Rep 1/2 · 240 m`; `Rep 1` 360 m `2:05` | igual | — |
| 23 | `Corta`, 16 / 30 s | `Rep 1` 60 m `2:05`; termina sin agregar la rep 2 | igual; fin en 30 | `:153-174` |
| 24 | `Corta` con `R-4P` | `Rep 1 — 0 m` | `Rep 1 — 0 m` (w 0, 10 s) | `:31-33`, `series_panels.dart:339-341` |
| 25 | Pausa de 20,7 a 50,7 s | `0:20.0` `80 m` → `0:21.0` `84 m`; `#1` en 125 | igual | `workout_provider.dart:245-250` |
| 26 | Pausa de 1 s que cruza un cambio de paso (30,4 → 31,4) | — | entra al trabajo con `0:01.0` `4 m` (la referencia nueva no suma) | `series_tracker.dart:104-108` |
| 27 | Paso de 0 s (diseño §2.2) | recibe una lectura y termina en ese segundo | ocupa el segundo 6, exactamente uno | — |
| 28 | Posiciones de la rutina expandida | suelto (sin serie, 1, 1) | igual | `routine.dart:74-97` |
| 29 | Formatos de la vuelta y del split | `Math.Round` de Dart (mitad hacia afuera) | `WorkoutFormats.cs:92-111` usa `DartMath.Round` | `series_panels.dart:9-20` |
| 30 | `formatMeters` | punto de miles también en inglés | `PanelMeters` (LT-45, 4a) | `:23-24` |

Dos precisiones para la spec, que no son bugs:

- **LT-56.** La vuelta da 126 s, no 125, porque la primera lectura no suma. El legacy lo acepta con `closeTo(125, 2)`, y la prueba portada tiene que conservar esa tolerancia, no apretarla.
- **AC-WKT-18-04 dice "con 2 entre filas".** El legacy pone 2 arriba y 2 abajo de cada fila (`series_panels.dart:366-367`), así que entre filas quedan 4. El sistema visual igual fija sus propios aires (QA-G5T-02).

---

## 4. Trackers puros y deterministas, motor y oráculo

- **Un solo saneamiento** (DT5-06). `SeriesTracker` es dueño del único `SanitizedDistance` y el paso por distancia lee `_series.Meters`. Lo comprobé: los metros del paso del motor y `StepMeters` del tracker coinciden en el cambio de paso. El motor los fija al terminar el segundo `n` y el tracker los toma al entrar en `n + 1`, y en el medio nada los cambia. Mi puerto da los mismos fines con los cuatro remos (casos 13 a 16). `PureCodeRulesTests` lo fija en el código. Bien.
- **Orden y coma flotante.** El diseño copia `(target − _distance) / dd · dt` en el orden de Dart (ADR 012). En IL2CPP para ARM64, clang puede contraer a FMA; la tolerancia de 1e-9 lo absorbe. Bien.
- **El motor** (§2.3): `Update` antes de los objetivos y del fin del paso; `Begin` al iniciar y al volver a reposo; `Rebase` al reanudar. Es `workout_provider.dart:128`, `:178-186`, `:248` y `:315`. Bien.
- **El oráculo** (§2.9, DT5-07). Con DV-26, correr el `WorkoutProvider` del legacy daría otra secuencia de pasos con `R-4R`, `R-4Z` y `R-4D`. Por eso el diseño corre el tracker con entradas armadas por el modelo de la app y además exige que el motor de la app le dé exactamente esas entradas (`TheEngineFeedsTheOracleInputs`). Si el modelo de Dart se equivoca, falla esa prueba. `series_tracker.dart` solo importa `interval_step.dart` y `routine.dart`, que son Dart puro, así que corre como `routine_vectors.dart`. Bien.
- **Segundo plano** (AC-WKT-07-10). `FrameScheduler` dispara en orden, en tiempo virtual, el remo de prueba (`L(k)` en `k − 0,5`) y el motor (`k`): cada segundo tiene su lectura. Bien.
- **Equipo suspendido.** `ScriptedRower` manda sus lecturas con un *timer* del programador (`ScriptedRower.cs:74`), y el programador usa el reloj **despierto** (ADR 020). Durante el sueño no manda nada y al despertar sigue con `L(251)`, sin salto. La parte de la spec que dice "el primer segundo sin sumar metros" (`01-po-spec.md:179`) no se ve con ese remo (QA-G5T-06).
- **Solo en memoria** (AC-WKT-07-11). Nada en la base, en los puntos ni en el historial; `PIN` y `SessionRecorder` comparan los puntos con y sin el tracker. Bien.

---

## 5. Preferencias persistidas

- **Lugar y aislamiento** (DT5-05). `prefs.json` va en la carpeta de la base (`DataLocation.DatabaseDirectory`). En el Editor y en desarrollo la reemplaza `ROWMATE_DATA_DIR`, que ya ponen `run-unity-tests.ps1:212`, `capture-snapshots.ps1:190` y `BootSceneTests`. Las pruebas de dotnet y las de UI usan memoria o una carpeta temporal, y el arnés una `MemoryPreferences` por toma. Bien.
- **Faltan tres cosas:**
  - El precalentado del HUD (`HudWarmup`, B2) tiene que armar los paneles abiertos sin pasar por `SeriesPanelPrefs.Toggle`, que escribe, y sin dejar suscripciones. PFU calcula la ruta real de `DataLocation.PreferencesFile`: tiene que comprobarla sin escribir ahí (QA-G5T-05).
  - `JsonFilePreferences` contradice su propio contrato con una clave de otro tipo, y no dice en qué hilo se arma lo que se escribe (QA-G5T-07).
  - AC-WKT-18-11 promete, después de un guardado que falla, algo que ni el legacy ni el diseño hacen (QA-G5T-08).
- **El toque antes de la lectura** (LT-59, AC-WKT-18-10). En producción no pasa nunca: el archivo se lee de forma sincrónica al construir. Solo se ejercita con `RecordingPreferences.DeferLoad`. Es la regla del legacy (`series_panels.dart:130-151`) y está bien probada así.
- **IL2CPP.** El reemplazo atómico es R5-04, NO VERIFICABLE. Además, ARCHITECTURE RA-05 pide `JToken` y no deserialización genérica con Newtonsoft (QA-G5T-07).

---

## 6. Presupuesto, cuenta grande y sello

- **R4a-03** (§3.6). Las cuentas cierran:
  - el peor caso de la 4b es 58-59;
  - los dos paneles suman 2, uno por panel: los *holders* son `visibility: hidden` y `UiQuery.IsShown` no los cuenta (`UiQuery.cs:58`);
  - M5 resta 1 y la otra junta, 1 más.

  Con M5 solo ya se llega a ≤ 60. La segunda junta deja margen para la 6a (R4b-02). Pero cualquiera de las dos juntas rompe consultas por nombre en pruebas y sondas que WP-B0 no tiene en su lista (QA-G5T-10).
- **La cuenta grande y `PAUSA`.** Con 137, `BlocksCallout` da `false` en los cinco perfiles: el panel termina a 8,4 dp del disco en TV. La regla igual existe y se prueba forzada a 200 (P-04 por defecto). Es técnicamente sólido. Que la cuenta grande se vea siempre es una decisión del usuario (QA-G5T-02).
- **El sello** vive en la tercera fila de la barra y la columna empieza 8·s debajo de SPM: no se cruzan (CAP-24).

---

## 7. Motion, dato, salida, 0 B y reducir movimiento

- **El dato.** Cada animación sale de un dato del segundo o del toque, y ninguna cambia cuándo pasa un valor (§4.0). `PIN` (dotnet) e `IHT.PresentationNeverTouchesTheData` (PlayMode, con los paneles abriéndose y cerrándose, con reducir movimiento y sin HUD) lo cubren. La vuelta en curso no cuenta décimas en vivo. Bien.
- **ROB-02.** `HudView.Dispose` y `DetachFromPanelEvent` llaman a `Stop()` en las dos motions y en `TabularCells`. `ITT.NothingAnimatesAfterTheExit` suma dos salidas: con el resorte de un panel en curso y con la marca encendida. O-1 está cerrado en la deuda. **Falta un dueño** para los fundidos de *hover* y de presionado (90 / 160 ms, `04-design-notes.md:247`, `:494`): no están en la API de `SeriesPanelMotion`, ni en lo que bloquea la regla de toma, ni en el `Stop` (QA-G5T-11).
- **0 B por cuadro.** `GcAllocations.During(() => { 20 × H.Step(); })` (`HudBudgetTests.cs:160-177`) mide el C# del cuadro: el programador, el `MotionDriver`, `Apply` y los *callbacks*. **No mide el `generateVisualContent`**: lo corre Unity en su *loop*, entre `yield return null`, fuera de la acción. La 4b lo dejó escrito (`../04b-hud-exr/05-qa-verification.md:543`: "Miden el código del HUD de cada cuadro, no el `generateVisualContent` del repintado"). En la 4b el repintado era la fila de píldoras. En la 5 es casi todo, y en **cada cuadro de cada animación**: unas 80 llamadas a `DrawText` (S-B5), `Painter2D`, `Allocate` y el armado de `TabularCells` (QA-G5T-03).
- **Reducir movimiento** (§4.3 y notas §6.8). Se lee en cada llamada (QA-V4b-MOT-03), las variantes `reduced` se prueban fila por fila (`STL.ReducedVariants`) y la marca queda igual, porque es opacidad. Bien. El VID-08 de la spec no puede mostrar lo que pide (QA-G5T-02, SD-14).

---

## 8. *Batchmode* y estados intermedios

- La regla de toma de la 4b es instante exacto en ms enteros y bloqueo por fases finitas. La 5 le suma las fases de los paneles, y la marca bloquea hasta los 1400 ms (§5.1).
- Revisé los instantes de las 26 CAP y de las 12 DEMO contra las vueltas, las repeticiones y los cambios de paso de §3:
  - solo CAP-11 (880,2) y DEMO-11 (31,2) caían en una fase finita, y el diseño ya los corrió (DT5-04, confirmado);
  - CAP-06 (180,6) y CAP-24 (90,6) caen en el segundo del cambio de barra y antes del de los paneles (P-01), con los rodillos de 180 ms ya terminados.
- **Los rodillos de las celdas.** Duran 180 ms desde el cuadro del segundo, que llega hasta 16,7 ms tarde, así que terminan antes de los +0,2 s. Es el mismo margen que en la 4b (3 ms).
- **DEMO-03 y DEMO-05** se toman a 0,8 s del toque, contra los 0,82 de la regla. No bloquean, porque la regla mira `ISettlingMotion.RemainingDp` (0,04 dp en W1280) y no el tiempo. Es coherente.
- **Los videos** guardan en el *manifest* los valores de cada `IMotion` y los comparan con `SeriesTimelines` (± 0,01 y ± 0,1 base), en ms enteros como en la 4b. Bien, una vez que las tablas sean las de las notas (QA-G5T-01).
- **`it4a`** (DT5-08). Su *manifest* tiene 38 de 49 tomas `superseded: it4b`. Las 11 que quedan son de reposo y del diálogo de inicio, sin paneles. Confirmado: no hay nada que regenerar.

---

## 9. UI Toolkit: ciclo de vida, selección y teclado

- **Ciclo de vida.**
  - Los textos se miden solo cuando cambian (como mucho a 1 Hz) y solo con panel; sin panel, se aplican al volver.
  - Los paneles nacen con el HUD en el cuadro negro (DT5-D1): el primer toque no mide nada (QA-V4b-06).
  - `DetachFromPanelEvent` detiene lo que anima.

  Bien. Falta decir qué pasa con una motion detenida cuando el panel vuelve a un panel (`AttachToPanelEvent`): ir al estado final (QA-G5T-12).
- **Selección** (QA-G5T-04). En la selección de UI Toolkit que conozco (`Panel.PerformPick`), un elemento cuya `worldBoundingBox` no contiene el punto se descarta **antes** de llamar a `ContainsPoint`. La caja sale del *layout* (y de los hijos) y de las transformaciones de `style`, no de lo que dibuja `generateVisualContent`. El diseño pone afuera de la caja tres cosas:
  - el área de toque del chip cerrado, de hasta 56 dp contra un chip de 35,8 dp en TV (notas §5.5);
  - el panel de repeticiones corrido por el FLIP, con hasta −59 dp en VID-01 (notas §6.1);
  - el desplazamiento de la columna en TH y L640, aplicado "como corrimiento dibujado (y en el área de toque), nunca como `style`" (`02-tech-design.md:1021`).

  La sonda S-B5 prueba `ContainsPoint` con el rectángulo visual, pero no un punto afuera de la caja (`02-tech-design.md:991`). SCT pide "un toque a 3 dp del borde de abajo de `500 m`", que en TV cae fuera de la caja del chip: si va por la selección real, lo va a encontrar.
- **Dueño de `style.translate`.** §3.1 (`:489`) y §3.3 (`:587`) dicen que el FLIP es un `translate`; §3.3 (`:590`) y §7.4 (`:1021`) dicen que `style.translate` es de la cascada y de la salida, y lo propio, dibujado. Tiene que haber un solo dueño, y la corrección de QA-G5T-04 lo define.
- **Teclado.** El anillo, `Blur()` antes de esconder repeticiones y el siguiente Tab a `Terminar` están bien. `NavigationSubmitEvent` lo manda Enter, pero no Espacio: la app traduce `KeyCode.Space` a mano (`RmButton.cs:191-197`, `RoutineCardView.cs:197`). El diseño dice "`NavigationSubmitEvent` (Enter y Espacio)" (`02-tech-design.md:604`) (QA-G5T-09).
- **Foco por USS.** Los controles usan el hijo `.rm-focus-ring` (`Base.uss:222`). El panel dibuja su anillo y no tiene ese hijo, así que no hay doble anillo. Bien.

---

## 10. La deuda de la 4b (00-deuda-4b)

| Deuda | Estado | Lectura de QA |
|---|---|---|
| **P-17 / AC-WKT-02-11** | Sin bug; la demo (c), para el usuario al cierre | **Pendiente explícita**, bien escrita (§1.5: suspender de verdad y qué buscar en el registro). Las pruebas de registro (`SuspendAccountingTests`, `UnitySchedulerTests`, `SystemSleepSourceTests`) están en el árbol. El §1.1 publica datos del equipo y de la base del usuario (QA-G5T-15) |
| **QA-V4b-06 / AC-WKT-13-15** (el HUD fuera del cuadro del toque) | `HudWarmup` y el HUD en el negro; Editor 13,7 / 10,1 ms; build NO VERIFICABLE | Técnicamente bien: `StartingAt` conserva las fases y hay pruebas `ITT` y `4BD`. La medida en el *player* sigue pendiente y el `RowMate.exe` del usuario sigue abierto hoy. La spec pide verificarla **antes** de los paneles, y el diseño la deja condicional en WP-B0 (a) (QA-G5T-14) |
| **QA-V4b-R2-03** (Unity que revienta al cerrar) | Cerrado | Leí `run-unity-tests.ps1:101-131`: acepta `-1073741819` / `3221225477` solo si el *define* y la marca quedaron como se pidió; si no, repite una vez, y cualquier otro código es error. Está bien acotado |
| O-4, O-5, O-1, O-P01, O-P02, O-R2-01 | Cerrados | Las pruebas existen (`MotionDriverIsolationTests.cs`, `FourBDebtTests.cs`, `ItFiveDebtTests.cs`, sin seguimiento todavía). El dotnet de línea base da verde (§1) |
| **QA-V4b-R2-01 / AC-WKT-16-07** | Cerrado, con CAP-13 a CAP-15 regeneradas | No es de esta lente; las cuentas de §5 son coherentes |

---

## 11. Paquetes disjuntos y contrato USS

- **Archivos.** Las entregas de WP-B (B0 a B3) se suceden: `HudBudgetTests.cs` en B0 y B1, y `SeriesColumn.cs` y `UiSupport` en B1 y B2, son del mismo paquete y van en orden. Las excepciones de §7.6 están bien acotadas. Hay dos huecos:
  - la junta de B0 toca archivos que B0 no tiene (QA-G5T-10);
  - las notas piden cambios estructurales (DD5-16, DD5-18, DD5-26) en archivos de WP-A (`SeriesTimelines`, `HudSeriesChange`, `SeriesPanelGeometry`), y la excepción de §7.6 solo cubre cambiar **una constante** (QA-G5T-01).
- **En paralelo.** "WP-D1 (después de WP-A; independiente de B0)" (`02-tech-design.md:978`). Los dos necesitan Unity, y hay una sola instancia por proyecto. La regla "no se tocan fuentes mientras corre Unity" sale de un incidente real (00-deuda-4b §3) (QA-G5T-13).
- **Contrato USS** (§7.5). El panel sin fondo, borde, relleno, `overflow` ni transición en USS; los *holders* con su clase de tipo y el tamaño en línea; M5 con `margin-left: auto`. Es coherente con la regla de las It. 2 a 4b, y las notas no lo cambian.
- **ARCHITECTURE §20.2** dice "un `TabularNumber` por número". `TabularCells` dibuja números adentro de otro elemento (DT5-13). Al cerrar, §20.2 tiene que decirlo.

---

## 12. Contradicciones spec ↔ diseño técnico ↔ notas

**Diseño técnico ↔ notas** (QA-G5T-01). El diseño técnico es anterior a las notas y no las integra:

| Tema | 02-tech-design | 04-design-notes |
|---|---|---|
| Revelar por renglón | `α_borde = clamp((bordeVisible − abajo) / 12·sp)` (`:487`) | por la tinta, 4 base; ventana de filas; máscara de 12 solo en `ColumnScroll` (R-02, R-03, DD5-13 a DD5-15) |
| El alto por un dato | `spring.smooth` (§4.1, `ContentChanged` con resorte) | el empuje: 0-160 al crecer, 80-240 al achicar, `ease.move`, y `TailShiftBase` nuevo (R-04, DD5-16) |
| La fila nueva | `SeriesRowInStartMs` 60 (`:660`) | 80 (R-05, DD5-17) |
| El cambio de contenido | `ContentSwap` también en "trabajo nuevo" (`:340`) | por pieza: de trabajo a trabajo, el rodillo (R-06, DD5-18) |
| El vidrio | "el reflejo de arriba de las tarjetas" (`:474`) | sin reflejo (R-09, DD5-21) |
| El anillo de foco | radio 18 | 14·sp + 4 y 14·sp + 8 (R-10, DD5-22) |
| Los vacíos | `Sin parciales en` / `este paso`, `No splits in this` / `step` (CAP-05, CAP-13: `:713`, `:721`) | corte equilibrado (R-07, DD5-19) |
| Los títulos | una escala por panel | una escala por idioma: 0,949 en es (R-08, DD5-20) |
| CAP-01 en TV | "228,6 a 308,7 con objetivo" (`:709`) | 210,9 a 291,0: `Series 600` no tiene objetivos (R-12) |
| TH / L640 | 77,1 base | 77,1 con objetivo y 93,9 sin (R-13) |
| `RowsScroll` | de arriba hacia abajo | repeticiones con su mínimo reservado (DD5-26) |
| VID-08 | con los dos abiertos (`:770`) | parciales cerrado al empezar (R-15) |
| El *hover* | "por código", sin tiempos | 90 / 160 ms (QA-G5T-11) |

**Spec ↔ diseño y notas** (QA-G5T-02). Para el PO y, en P-03 y P-04, para el usuario:

- **SD-01 a SD-11** (diseño §8.2) y **SD-12 a SD-14** (notas §11.1) tocan AC-WKT-18-02, -03, -04, -07, -13, -14, -15, -18 y -19, AC-WKT-13-12, AC-WKT-14-18, la demo (b), CAP-20, CAP-22, VID-08, VID-09 y la regla de 0,6 s de §7.1.
- **Cuatro que no están en ninguna lista:**
  - **AC-WKT-18-02**: "a 6 la flecha". Las notas la ponen pegada al borde derecho, con vidrio vacío en el medio (§4.2).
  - **AC-WKT-18-03**: el título "con el estilo del chip", que en el legacy es 13/600 con 0,8 de espaciado (`series_panels.dart:241-244`). Las notas lo ponen en 11/600, sin espaciado y a una escala (§3).
  - **AC-WKT-18-04**: los metros de la vuelta en curso "al lado" del tiempo. Las notas los ponen pegados a x 127 (§4.4).
  - **AC-WKT-18-04**: "2 entre filas". En el legacy son 2 + 2 (§3).
- **La demo (b)** (`01-po-spec.md:84`) dice "con `500 m` abierto, la cuenta grande no aparece esa vez". Con 137 aparece siempre: si el usuario acepta SD-07, hay que reescribir la demo.

---

## 13. Regla de repo público

- 01-po-spec, 02-tech-design, 04-design-notes y 00-deuda-diseno: sin rutas de la PC ni datos personales. El legacy se cita por su URL y el *scratchpad* como `<scratch>`.
- `tools/unity/run-unity-tests.ps1:144` arma la ruta con `$env:USERPROFILE`, que es genérico. Está bien.
- **00-deuda-4b §1.1** (`:51`, `:52`, `:57`) publica:
  - el nombre y el contenido de la base de desarrollo del usuario;
  - detalles del registro de eventos de su equipo;
  - datos de energía de su equipo.

  Son "datos del equipo" (CLAUDE.md). Hay que generalizarlos antes del *commit* (QA-G5T-15).
- Los casos de prueba con rutas se tienen que armar en tiempo de ejecución, como en la 4b. Vale para `PreferencesIsolationTests` y PFU.

---

## 14. Issues

| ID | Sev. | Resp. | Issue | Evidencia | Corrección sugerida |
|---|---|---|---|---|---|
| QA-G5T-01 | mayor | architect | 02-tech-design no integra las 04-design-notes (R-01 a R-15, DD5-13 a DD5-27). Contradice sus líneas de tiempo, el contrato de motion, constantes, la geometría y las comprobaciones del arnés, y la mayoría cae en **WP-A** | §12 (tabla); `02-tech-design.md:487`, `:660`, `:668`, `:709`, `:713`, `:721`, `:770`; `04-design-notes.md` §2, §6.9 y §10 | Antes de WP-A, una sección "Cambios por notas del Designer" que adopte §6.9 y DD5-13 a DD5-27. Que defina `TailShiftBase` y el alto con el empuje en `SeriesTimelines`, el cambio por pieza en `HudSeriesChange`, el reparto de `RowsScroll` y las comprobaciones de CAP-01, -05, -12, -13, -15 a -17, -21, -23 y VID-08. Que diga que la excepción de §7.6 no alcanza para los cambios estructurales |
| QA-G5T-02 | mayor | po | La spec dice lo contrario de lo que se construye en SD-01 a SD-14 y en cuatro puntos que nadie listó. La demo (b) no se cumple con 137 | §12; `01-po-spec.md:84`, `:250`, `:257`, `:262-263` | En el gate: decidir SD-01 a SD-14 (P-03 y P-04 con el usuario) y reescribir AC-WKT-18-02, -03, -04, -07, -13, -14, -15, -18 y -19, AC-WKT-13-12, AC-WKT-14-18, la demo (b), CAP-20, CAP-22, VID-08, VID-09 y §7.1. Sumar los cuatro que faltan: la flecha a la derecha, el título 11/600, los metros a la derecha y el aire entre filas. Si gana la spec, cambian solo las constantes (R5-07) |
| QA-G5T-03 | mayor | architect | El "0 B por cuadro" no mide el repintado. `GcAllocations.During(() => H.Step())` deja afuera el `generateVisualContent`, y en la 5 ahí va casi todo, en cada cuadro de cada animación (unas 80 llamadas a `DrawText`, más `Painter2D`, `Allocate` y `TabularCells`) | `HudBudgetTests.cs:160-177`; `UiTestHost.cs:288-303`; `../04b-hud-exr/05-qa-verification.md:543`; `02-tech-design.md:619`, `:685`, `:993`; ARCHITECTURE §20.2 ("0 B/cuadro … verificado con el Profiler") | Medir con cuadros reales: `ProfilerRecorder` de `GC.Alloc` (suma por cuadro) o de "GC Allocated In Frame", durante 30 cuadros con `yield return null` de cada animación (abrir, cerrar, la vuelta nueva, la serie que empieza, la rueda y la máscara de `ColumnScroll`), con un control positivo como el R2.5 de la 4b. Sumarlo a la sonda S-B5 (80 `DrawText` por repintado, 0 asignaciones). `HBT.SeriesWorstCaseWork` con un tope coherente con §20.2 (UI ≤ 1 ms) además de los 16 ms |
| QA-G5T-04 | mayor | architect | La selección de UI Toolkit descarta un elemento si el punto cae fuera de su `worldBoundingBox` antes de `ContainsPoint`. Quedan fuera de la caja el área de toque del chip (hasta 56 dp contra 35,8), el FLIP dibujado (hasta −59 dp) y el desplazamiento dibujado de la columna en TH y L640. Después de desplazar, repeticiones puede quedar sin toque ni arrastre (AC-WKT-18-09, -13). Además, el dueño de `style.translate` es contradictorio | `02-tech-design.md:489`, `:587`, `:590`, `:991`, `:1021`; `04-design-notes.md` §5.5 y §6.1; §9 de este informe | Aplicar el FLIP y el desplazamiento de la columna como `style.translate` (transformación, no *layout*), con un solo escritor que componga la cascada, el FLIP y el desplazamiento. Para el área del chip: un hijo oculto del tamaño del área (agranda la caja y no cuenta en el presupuesto) o una caja de *layout* igual al área, con el chip dibujado adentro. Sumar a S-B5 los tres casos con puntos fuera de la caja, y que SCT toque por la selección real. Si nada de eso anda, el plan B de S-B5 (+2 elementos) |
| QA-G5T-05 | mayor | architect | `HudWarmup` arma "los dos paneles abiertos y con contenido" en cada arranque. El diseño no dice cómo los abre sin `SeriesPanelPrefs.Toggle`, que escribe `prefs.json`, ni que no deje suscripciones a `AppContext.SeriesPanels`. PFU calcula la ruta real de las preferencias | `02-tech-design.md:97`, `:842`; DD5-12; `00-deuda-4b.md:124` | El precalentado con un estado abierto local que no pasa por las preferencias. `4BD.HudWarmupIsNotSeenAndLeavesNothing` y `BST` afirman 0 escrituras (`RecordingPreferences`) y el archivo intacto, o que no exista. PFU compara solo la cadena de la ruta y escribe solo en una carpeta temporal |
| QA-G5T-06 | menor | architect | AC-WKT-07-10 "al despertar, el primer segundo sin sumar metros" no se puede probar: `ScriptedRower` manda con el reloj despierto, no hay salto, y `SEN` vería `0:51.0` `204 m` | `ScriptedRower.cs:74`; `01-po-spec.md:179`; `02-tech-design.md:264` | Un remo de prueba con el índice en el reloj crudo (el monitor siguió contando) en `LifecycleHarness`, que el PO sume a §0.4. Con `Series 600`, la suspensión de 600 s a los 250,4: a los 251, `0:51.0` `200 m`; a los 252, `0:52.0` `204 m` |
| QA-G5T-07 | menor | architect | `JsonFilePreferences` dice que con "una clave de otro tipo arranca vacío": contradice "conserva las claves que no conoce" y el contrato de `IPreferences` (`TryGet` false solo para esa clave), y borraría las de It. 11. Además no dice en qué hilo se arma lo que se escribe, si se juntan escrituras seguidas ni con qué API de Newtonsoft | `02-tech-design.md:361-363`, `:379-383`; ARCHITECTURE RA-05 | Otro tipo: solo esa clave da `false` y el resto queda; vacío solo si el archivo no se puede leer. El texto JSON se arma en el hilo principal en el `Set` y el ejecutor solo escribe bytes, juntando las escrituras pendientes. `JObject` / `JToken`, sin deserialización genérica |
| QA-G5T-08 | menor | po | AC-WKT-18-11 promete que "si guardar falla, … la sesión siguiente abre como quedó guardado la última vez". En el legacy el caché de `SharedPreferences` conserva el valor nuevo hasta reiniciar, y el diseño hace lo mismo (el valor en memoria queda) | `01-po-spec.md:304`; `02-tech-design.md:383`; `series_panels.dart:153-156` | "Las sesiones siguientes de la misma ejecución abren con lo elegido; después de reiniciar, con lo último que se guardó" |
| QA-G5T-09 | menor | architect | Espacio no manda `NavigationSubmitEvent`: la app lo traduce a mano en cada control. El diseño dice "`NavigationSubmitEvent` (Enter y Espacio)" | `02-tech-design.md:604`; `RmButton.cs:191-197`; `RoutineCardView.cs:197` | `KeyDownEvent` de `KeyCode.Space` → el mismo camino que Enter, como `RmButton`. SKT prueba Espacio con un `KeyDownEvent`, no solo con el evento de navegación |
| QA-G5T-10 | menor | architect | La junta de B0 (el sello o `Próximo` en un elemento) rompe consultas por nombre: `hud-seal-title`, `-text` e `-icon` en `HudExrCatalog.cs` y `HudLayoutTests.cs`; `hud-up-next*` en `StepBarTests`, `ImmersiveHudTests`, `ImmersiveTransitionTests`, `HudBudgetTests`, `WorkoutCatalog` y `HudExrCatalog`. B0 solo lista `StepInfoBar`, `SealRow` y `HudBudgetTests` | `02-tech-design.md:1004`; grep de los nombres | Sumar esos archivos a B0 o mantener los nombres como ranuras (`IDrawnTextSource`, como DT4b-01). `it4b` tiene que seguir igual byte a byte |
| QA-G5T-11 | menor | designer | Los fundidos de *hover* y de presionado (90 / 160 ms) no tienen dueño: no están en `SeriesPanelMotion`, ni en la regla de toma (`IsFinite`), ni en `Stop` al salir (ROB-02) | `04-design-notes.md:247`, `:494`; `02-tech-design.md:668` | Con el Architect: o son una fase de `SeriesPanelMotion` (finita, con `Stop` y reducir movimiento), o son instantáneos |
| QA-G5T-12 | menor | architect | `AttachToPanelEvent`: falta qué hace un panel que vuelve a un panel después de `DetachFromPanelEvent` (con motions detenidas y medidas viejas) | `02-tech-design.md:592` | Al volver: las motions en su estado final, medir otra vez y repintar |
| QA-G5T-13 | menor | architect | "WP-D1 independiente de B0": si corren en paralelo, chocan con la instancia única de Unity y con no tocar fuentes mientras corre | `02-tech-design.md:978`; `00-deuda-4b.md:197` | Escribirlo: B0 y D1 en serie, o D1 sin abrir Unity ni editar mientras corre la suite de B0 |
| QA-G5T-14 | menor | po | La medida de QA-V4b-06 en el *player* sigue NO VERIFICABLE: el `RowMate.exe` del usuario está abierto hoy. La spec pide verificar la deuda antes de los paneles, y WP-B0 (a) es condicional | `01-po-spec.md:384`; `02-tech-design.md:977`; `00-deuda-4b.md:146`; §1 | Aceptar que WP-B1 siga y que AC-WKT-13-15 se mida al cierre con las tres entradas (`Cinco reps` con los paneles), anotado como riesgo aceptado. Pedirle al usuario que cierre su `RowMate.exe` antes del cierre |
| QA-G5T-15 | menor | architect | Repo público: 00-deuda-4b §1.1 publica datos del equipo y de la base del usuario | `00-deuda-4b.md:51`, `:52`, `:57` | Generalizar, por ejemplo: "el registro de eventos no muestra suspensiones en la ventana de la demo; la base de desarrollo tiene una sola sesión corta". Sin la historia de sueño ni el contenido de la base |
| QA-G5T-16 | menor | architect | AC-WKT-18-17 "más allá de `S-LARGO`, … al final" no tiene caso; SPV cubre `S-LARGO` y `R-4X` | `02-tech-design.md:848`; `01-po-spec.md:340` | Un caso en SPV con un split de días y un metro de trabajo (`RepSplit` largo): la escala en su mínimo, `…` al final y nada fuera del panel |

---

## 15. Lo que este gate no verificó

- **Unity no se abrió.** No se comprobaron:
  - la selección real de UI Toolkit 6000.3 (QA-G5T-04 es un riesgo a confirmar en S-B5);
  - lo que asigna `DrawText` por repintado (QA-G5T-03);
  - `MeasureTextSize` con *holders* ocultos;
  - el resto de S-B5.
- **El *player* de Windows**: el [PERF] de AC-WKT-13-15 y de AC-WKT-18-21 no se pudo medir, porque el `RowMate.exe` del usuario está abierto (§1).
- **Las otras lentes**: el aspecto (contraste, legibilidad, sondas) y la paridad de textos son de ellas.
- **Los números de las notas** (alturas, alfas por cuadro, rectángulos) no se recalcularon. Solo se revisó su coherencia con el diseño técnico (§12).
