# It. 8 — Verificación de QA (ronda 1, sobre el árbol final)

> **Ronda 2 (2026-11-07): APROBADO CON OBSERVACIONES** — 39 PASS · 0 FAIL · 1 NO VERIFICABLE; 0 mayores. Ver [Ronda 2](#ronda-2) al final. Lo que sigue es la ronda 1, como quedó.

| Campo | Valor |
|---|---|
| Iteración | 8 · `08-entornos-selector` |
| Contra | [01-po-spec](01-po-spec.md) (40 AC, LT-99 a LT-138), [02-tech-design](02-tech-design.md) (con B8-01 a B8-09 y §14), [00-arte-entornos](00-arte-entornos.md), [04-design-notes](04-design-notes.md) y [rowmate-legacy](https://github.com/figuibej/rowmate-legacy) (`../rowmate-legacy`, solo lectura; `git status --porcelain` vacío al empezar y al terminar; no se leyó el módulo de credenciales) |
| Árbol verificado | El árbol de trabajo sin commitear sobre `7e7b3c9`, **congelado durante toda la ronda**: el mismo `md5` de `git diff` + archivos nuevos y el mismo `git status` al empezar (15:33) y al terminar (16:12). Es el árbol que dejó WP-D3 (último cambio de fuentes a las 15:05). Reemplaza la primera pasada de esta ronda (15:11), que midió un árbol que cambiaba mientras tanto |
| Veredicto | **RECHAZADO**: 29 PASS · 9 FAIL · 2 NO VERIFICABLE. **4 mayores** y 9 menores. El núcleo funciona y rinde (generadores bit a bit, preferencia y selector, reinicio, ola de la costa, luces de noche, presupuesto por escenario, `[PERF]` en la PC); fallan el "mundo vivo" y piezas del arte que quedaron fuera sin el OK del PO, los 25 casos legacy de Unity sin portar, la paridad de pantalla (AC-SCN-14-10) y las tomas que faltan |

---

## 1. Corridas

| Qué | Comando | Resultado |
|---|---|---|
| dotnet (.NET 8) | `tools/dotnet/run-tests.ps1 -Matrix -ArtifactsPath <scratch>/it8/qa-r1` | Build **0 avisos, 0 errores**. Herramientas **192 / 192**. Core **3 340: 3 301 pasan, 0 fallan, 39 omitidos** (46 s). Matriz: PASA 700, FALLA 0, IGNORADO 4, PENDIENTE UNITY 157, SIN TEST 0 |
| Unity EditMode | `run-unity-tests.ps1 -Platform EditMode` | **3 444: 3 399 pasan, 0 fallan, 45 ignorados**, 0 inconclusos |
| Unity EditMode + simulador | `… -Simulator` | **3 636: 3 591 pasan, 0 fallan, 45 ignorados**; define restaurado |
| Unity PlayMode | `-Platform PlayMode` | **522: 522 pasan** (incluye `EnvironmentBudgetTests` 12 / 12 y `SceneBudgetTests` Alto / Medio / Bajo) |
| Unity PlayMode + simulador | `-Platform PlayMode -Simulator` | **595: 595 pasan** |
| Guardia de `ProjectSettings` | las cuatro corridas y el build | "sin cambios" en todas; al final solo queda `RowMateBootstrap.json`, que ya estaba cambiado al empezar |
| Tomas | las del repo (15:03–15:05, WP-D3) contra su corrida A en `<scratch>` | **48 de 48 archivos iguales byte a byte**; `manifest.json` igual salvo `durationMs`. Son del código final |
| Build | `tools/build/build.ps1 -Target Windows -Simulator` | **Éxito** → `Builds/Windows/simulator/RowMate.exe` (Mono, desarrollo), `THIRD_PARTY_NOTICES.md` al lado |
| `[PERF]` y demo | build + `ROWMATE_DATA_DIR` temporal por corrida (base sembrada `-seed F5`, `prefs.json` por escenario) | §4. Nada tocó `prefs.json` ni las bases del usuario (fechas sin cambiar) |

---

## 2. Matriz AC → evidencia → resultado

| AC | Evidencia | Resultado |
|---|---|---|
| AC-SCN-14-08 (LT-99 a -107) | 22 tests dotnet (`River/Coast/RegattaEnvironmentTests`, `EnvironmentGoldenTests`: segmentos 0–2 000 bit a bit y listas visibles) | **PASS** |
| AC-SCN-14-09 (LT-109) | 9 tests dotnet (topes 60 / 200, tope real = función del legacy) | **PASS** |
| AC-SCN-14-10 | Solo apoyo dotnet (ancla de las casas, *skyline* por columna). **No existe `EnvironmentParityTests` ni CAP-44 a -46** con marcas; LEG-01 a -03 sí están | **FAIL** (QA-V8-03) |
| AC-SCN-16-01, -02 | dotnet; CAP-09 a -16: fachadas, clubes, faroles, boyas amarillas | **PASS** |
| AC-SCN-16-03 (LT-110) | 7 tests dotnet (puente 270 / 1 070, `BridgeShade` −40 %); VID-02 a los 17,5 s: bajo el arco, sin cortes, agua y casco más oscuros | **PASS** |
| AC-SCN-16-04 | dotnet: el río sin cruces en 100 km; CAP-09 a -16: muro, paseo, fachadas, faroles, nada flota | **PASS** |
| AC-SCN-17-01 | dotnet (`WaterInputsTests`, tinte 30 %) | **PASS** (con la arista del mar, QA-V8-06) |
| AC-SCN-17-02, -03 | dotnet (playa 100–150, faro 530 / 1 530); CAP-18, -42: acantilados, faro, mar a la derecha; CAP-20 / -24: halo y haz | **PASS** |
| AC-SCN-17-04 | Acantilados, estratos, pasto, rocas, arena y sombrillas; **sin espuma** en la base ni en la playa (`Foam`: "No", 04-design-notes §1) | **FAIL** (QA-V8-01) |
| AC-SCN-12-01 (LT-124) | 8 tests dotnet; PlayMode `TheCoastSwellMovesTheBoatButNotTheDistance`; CAP-42: bandas suaves | **PASS** |
| AC-SCN-06-04 | 6 tests dotnet; VID-05: arfada y cabeceo ≠ 0 por cuadro (`manifest.json`) | **PASS** |
| AC-SCN-18-01 (LT-108) | dotnet (4 líneas, color por tramo de 250 m; CAP-26: rojas y amarillas desde 250 m). **Las boyas no cabecean** (DV-88; horneadas en la malla del segmento) | **FAIL** (QA-V8-01) |
| AC-SCN-18-02 | 6 tests dotnet; CAP-40 (`250 m`, 12 h) y CAP-41 (`750 m` de noche) se leen | **PASS** |
| AC-SCN-18-03 | dotnet (tribuna 210, torre 610, banderas `k mod 4`); VID-04 y CAP-26: tribuna, banderas | **PASS** |
| AC-SCN-18-04 | Taludes, tribuna con techo y público, álamos, torre; **las banderas no flamean y el público no se mueve** (B8-05, `RM_PROP_CLOTH`: "No") | **FAIL** (QA-V8-01) |
| AC-SCN-19-03 | dotnet; CAP-12 / -16: ventanas del *skyline* de noche | **PASS** |
| AC-SCN-19-04 | `SeagullFlight` se calcula (`Core/Scene/SceneSession.cs:395-405`) pero **nadie lo dibuja** (ningún archivo de `Assets/RowMate/Scene/` usa `Seagull`); LEG-02 tiene gaviotas, VID-03 (4 y 12 s) y CAP-17 a -24, ninguna | **FAIL** (QA-V8-01) |
| AC-SCN-19-05 | Tierra, tribunas y torres lejanas sí; **faltan los grupos de 4 banderas** (`Core/Scene/Geometry/EnvironmentHorizonStrip.cs:68-83`) | **FAIL** (QA-V8-01) |
| AC-SCN-20-03 | 16 tests dotnet (medidas ± 10 %, paletas por variante) | **PASS** |
| AC-SCN-20-04 | 8 tests dotnet; CAP-12 / -16 / -24: ventanas, faroles con halo, faro. `manifest`: 70 luces encendidas en el río a las 22 h, 0 de día. Sin sondas de color (QA-V8-04) | **PASS** |
| AC-SCN-20-05 (LT-111 a -123) | **Ningún test de Unity** con esas categorías (`grep` en `Tests/`: solo `It08Ids.cs`); `EnvironmentSceneTests` tiene 6 casos | **FAIL** (QA-V8-02) |
| AC-SCN-20-06 | Solo gira el haz del faro; banderas, boyas, público y gaviotas, quietos o ausentes | **FAIL** (QA-V8-01) |
| AC-SCN-20-07 | `EnvironmentAssetsTests`, `EachEnvironmentBuildsAndDraws` (0 `[SLOT]`), `RepoHygieneTests.TheEnvironmentArtIsOwnAndBrandless`; `git ls-files` sin terceros | **PASS** |
| AC-SCN-21-02, -03 (LT-125) | 9 tests dotnet + `EnvironmentPickerTests` (4 / 4) | **PASS** |
| AC-SCN-21-04 (LT-126) | dotnet + PlayMode. **Build**: Workout → `Coast` con un clic real → `prefs.json` = `{"scene.environment":"coast"}` → cerrar y abrir → `Coast` elegida al primer cuadro de Workout → `Free workout` rema en la costa; ídem `Regatta course`. Los 4 desde `prefs.json` en `[PERF]`: la línea `[PERF] escenario` dice kit y sesión iguales al guardado y 0 cambios de kit | **PASS** |
| AC-SCN-21-05 | `EachEnvironmentBuildsAndDraws`, `SwitchingInRestRebuildsOnlyTheKit` | **PASS** |
| AC-SCN-22-01 | `WorkoutScreenTests`; CAP-37: aviso, `Escenario`, fila, `Entrenamiento libre` | **PASS** |
| AC-SCN-22-02 | PlayMode (4 tarjetas, 152 × 96, separación 10); CAP-37, CAP-38 (las 4 en W1280), CAP-39 (inglés) y el build | **PASS** |
| AC-SCN-22-03 | Toque y Enter (PlayMode); clic real en el build; foco en CAP-39; `EnvironmentCardMotion` | **PASS** |
| AC-SCN-22-04 | dotnet (`ThumbnailRule`, `BoxDownsample`); `TheBakedThumbnailsAreAssignedAndLight`; CAP-33 a -36 | **PASS** |
| AC-SCN-22-05 | CAP-38 ("dibujos de la escena en reposo: 0"); miniaturas ≤ 1 MB | **PASS** |
| AC-SCN-01-17 (LT-127 a -138) | **Ningún test de Unity** con esas categorías | **FAIL** (QA-V8-02) |
| AC-SCN-01-18 | `SwitchingInRestRebuildsOnlyTheKit` pasa. Las 10 alternancias con la memoria a ± 5 % no tienen prueba ni herramienta en el build | **NO VERIFICABLE** (QA-V8-08) |
| AC-SCN-01-19 | **PC: cumple** (`EnvironmentBudgetTests` 12 / 12 por escenario × nivel × 12 / 22 h, con 0 B y control positivo; build §4: p99 ≤ 3,3 ms, sin *shaders* después de `Ready`). Android de gama media: sin teléfono, como dice la spec | **NO VERIFICABLE** (Android) |
| AC-WKT-12-02 (cambia) | `WorkoutScreenTests`: `Escenario` se ve, en su orden | **PASS** |
| AC-SCN-21-01 (cambia) | `SceneEnvironmentChoiceTests` | **PASS** |
| AC-SCN-14-05 (cambia) | `EnvironmentGoldenTests` con los topes reales | **PASS** |

**Casos legacy:** LT-99 a LT-110 y LT-124 a LT-126 portados y pasan (dotnet). **LT-111 a LT-123 y LT-127 a LT-138 (25 casos) no están portados.**

**Se vuelven a verificar (spec §4):** las suites de la 6a y la 7 pasan en las cuatro corridas. Ahora `EnvironmentBudgetTests` cubre por escenario los topes y los 0 B (AC-SCN-01-12) en río, costa y regata; AC-SCN-08-06, AC-WKT-17-09 / -13 siguen probados solo en el lago. En las tomas, el HUD se lee sobre el muro, la arena y el cielo del mar.

---

## 3. Lo que se vio en las tomas y los videos

Se miraron las 43 CAP en hojas por escenario y hora (TV y TH), recortes de CAP-18 y CAP-26, LEG-01 a -03, y cuadros de VID-02 (5; 17,5 s), VID-03 (4; 12 s), VID-04 (4; 4,5; 15 s) y VID-05 (2; 8 s), todo del código final.

- **Bien:** los cuatro lugares se distinguen y se sienten creíbles a las 4 horas. Río: encajonado, puente a 20 m, ciudad encendida de noche (ventanas y halos). Costa: faro rojo y blanco, acantilados con pasto, playa con sombrillas, el sol bajo sobre el mar a las 18 h, haz del faro a las 22 h. Regata: carriles con boyas rojas → amarillas desde 250 m, cartel `250 m` legible, tribuna con público de colores, álamos, banderas en la orilla. Bajo el puente, sin cortes (VID-02). Selector: CAP-37 a -39 como pide la spec, y las miniaturas son de cada escenario.
- **Mal:** sin gaviotas (LEG-02 las tiene), banderas y boyas quietas, sin banderas lejanas en la regata (LEG-03 las tiene), sin espuma. En la costa sigue la **arista diagonal en el mar abierto de la derecha** (CAP-17, -18, -42; QA-V8-06). En la costa TH de las 22 h el farol del faro queda bajo la barra superior (se ve solo el haz).
- Contra el legacy: los mismos objetos y el mismo orden en río, costa y regata; la medida en pantalla (± 2 dp) no se puede afirmar sin CAP-44 a -46.

---

## 4. Build y `[PERF]` (W1280, Alto, simulador, carpeta de datos temporal)

| Corrida | Entrada (máx) | Régimen: p99 / máx del hilo principal | GPU máx | Dibujos / SetPass | Asignaciones en régimen |
|---|---|---|---|---|---|
| Lago, `-rowmate-perf-scene 40` | 23,1 ms | 2,9 / 4,2 ms | 8,2 ms | 101–104 / 63–65 | ≤ 86 en 23–24 cuadros, GC 0 |
| Río, ídem | 23,2 ms | 1,7 / 2,7 ms | 8,6 ms | 82–84 / 27–28 | ≤ 85 en 22–23 cuadros, GC 0 |
| Costa, ídem | 24,7 ms | 1,9 / 3,2 ms (un cuadro de 11 ms) | 9,2 ms | 73–75 / 27–28 | ≤ 85 en 23 cuadros, GC 0 |
| Regata, `-rowmate-perf-scene 60` (de 0 a ≈ 240 m: pasa `E-180`) | 22,1 ms | 1,7–2,6 / 8,2 ms; **un cuadro de 58,1 ms** (GPU 19,5) a los 42 s | 7,5 ms | 73–75 / 27–28 | ≤ 86, GC 0 |
| Regata, repetida 2 veces | 24,2 / 23,6 ms | 1,7–1,9 / ≤ 3,3 ms | ≤ 9,9 ms | ≤ 75 / 28 | ≤ 86, GC 0 |
| Los 4 con `-rowmate-perf-hours` (17 → 23 h) | 22,9–25,6 ms | ≤ 2,7 / ≤ 4,4 ms | ≤ 24,2 ms (lago, al cambiar la hora) | ≤ 105 / ≤ 66 | ≤ 85, GC 0–1 |
| Regata, `-rowmate-perf-completion` (`Corta` hasta el diálogo y la toma aérea) | 25,6 ms | paneles: p99 2,2–2,3 / 9,2 ms | 9,3 ms | 93 / 28 | — |

- La línea `[PERF] escenario` está en todas: `kit X, sesión X, cambios de kit 0`, con X el escenario de `prefs.json`.
- `perf-log-check.ps1`: 0 subidas de *shaders* después de `Ready` en las 11 corridas. Sin excepciones en los registros.
- El cuadro de 58 ms no se repitió en dos corridas más; en ese minuto corría el Unity de otra sesión sobre su worktree (QA-V8-13). El máximo de cada entrada es el cuadro del toque (≤ 25,6 ms, < 33).
- **Demo a mano en el build** (clics reales): Workout → `Coast` → se guarda → cerrar y abrir → `Coast` elegida → `Free workout` rema entre el faro y los acantilados; `Regatta course` → rema entre boyas y álamos → panel del simulador → `Hora escena` `+` tres veces → `17:00` con el cielo del atardecer y `Hora real` para volver. La rutina hasta el final: `-rowmate-perf-completion` en la regata, sin errores.

---

## 5. Repo público, terceros y codificación

- Rutas de la PC: `git grep` en lo versionado y `grep` en los 275 archivos cambiados o nuevos (sin `.claude/worktrees/`) de la carpeta de usuario, la carpeta temporal, los IDs de sesión y de trabajo, `.claude/jobs`, el nombre del equipo y `scratchpad/`: **solo** los patrones de los tests de higiene y las rutas de instalación estándar (`C:\Program Files\…`, ya admitidas por `RepoHygieneTests`). `manifest.json` e `INDEX.md` sin rutas. → **limpio**.
- Terceros: nada de `Assets/ThirdParty/` ni `.fbx` en `git ls-files` ni en los nuevos; `.gitignore:70` ignora `/Assets/ThirdParty/**` (solo pasan los `.meta`, como en la 7).
- UTF-8: los 147 archivos de texto cambiados o nuevos son UTF-8 válido y sin doble codificación (el único aviso era la versión anterior de este reporte, que nombraba las secuencias a buscar); `á`, `ñ` y `—` se leen bien. Varios con fin de línea LF (git los pasa a CRLF): no es un problema.
- Legacy: `git status --porcelain` vacío al principio y al final.
- Riesgo: `.claude/worktrees/` sigue sin estar en `.gitignore` (QA-V8-09).

---

## 6. Issues

| ID | Sev. | Resp. | Issue | Evidencia | Corrección sugerida |
|---|---|---|---|---|---|
| **QA-V8-01** | **mayor** | architect (+ designer) | **El "mundo vivo" y piezas del arte quedaron fuera sin el OK del PO.** Sin gaviotas; banderas, boyas y público quietos; sin espuma ni banderas lejanas en la regata. Rompe AC-SCN-17-04, -18-01, -18-04, -19-04, -19-05 y -20-06 (DV-88 y DV-89 eran mejoras aceptadas en la spec) | `SceneSession.cs:395-405` calcula `frame.Seagulls` y ningún archivo de `Assets/RowMate/Scene/` lo lee; `EnvironmentHorizonStrip.cs:68-83`; 02-tech-design B8-05; 04-design-notes §1 (`Foam`, `Crowd`, `RM_PROP_CLOTH`, cabeceo de boyas: "No") | Un `SeagullRenderer` desde `frame.Seagulls` (un dibujo); pivote y semilla por vértice en `KitSegmentMesh` para la tela y el cabeceo (o esas piezas fuera de la malla horneada, instanciadas); banderas en la tira lejana; espuma en la base del acantilado. Si no entra en la 8, que el PO pase estos AC a "cambia" con su DV y lo lleve al gate del usuario |
| **QA-V8-02** | **mayor** | architect | **25 casos legacy sin portar**: LT-111 a LT-123 (cielo y orilla sin excepciones en V390 / H844 × 12 / 23 h a 0 y 1 234 m; puente a relZ 0,3 y 1,0; cartel `750 m`; los 15 tipos a 20 m) y LT-127 a LT-138 (6 cuadros de 100 ms y desmontar). AC-SCN-20-05 y AC-SCN-01-17 en FAIL | `grep -rn "LT-11[1-9]\|LT-12[0-3]\|LT-12[7-9]\|LT-13[0-8]" Assets/RowMate/Tests` solo encuentra `It08Ids.cs` | `EnvironmentSceneTests` parametrizado: escenario × {V390, H844} × {12, 23 h} × {0, 1 234 m} con `LogAssert.NoUnexpectedReceived`; el caso de LT-123; 6 cuadros de 100 ms con SPM 24, split 110, 300 m, y desmontar. Cada uno con su `Category("LT-…")` |
| **QA-V8-03** | **mayor** | architect | **AC-SCN-14-10 sin verificación**: no existe `EnvironmentParityTests` (eje de cada objeto a ± 2 dp de la x del legacy) ni CAP-44 a -46 con marcas | `Tests/PlayMode/Scene/` sin el archivo; `EnvironmentSnapshotCatalog.cs` (CAP-37 a -43, VID-01 a -05) | Portar el `SceneMarks` de la 6a a los kits (`EnvironmentMarks`) y sumar la prueba y las 3 tomas contra LEG-01 a -03 |
| **QA-V8-04** | **mayor** | architect | **Conjunto de tomas incompleto y sin sondas de color**: faltan CAP-44 a -46, VID-06 (reinicio) y DEMO-01 a -05 (la spec §6 pide 46 CAP, 6 VID y 5 DEMO; hay 43, 5 y 0). `"probes": []` en las 48: el issue #9 (sondas de color de ventana, cabaña, farol, faro y *skyline*) no se cierra con la lista de luces encendidas. Las tomas que hay sí son del código final y repetibles (A = B) | `snapshots/INDEX.md`; `manifest.json` | Sumar al catálogo las tomas que faltan y las sondas (`#FFE082`, `#FFF8E1` ± 3 y las de la cabaña) y regenerar A / B |
| QA-V8-06 | menor | designer (+ architect) | En la costa, el mar abierto de la derecha tiene una **arista diagonal** que sale del punto de fuga: más allá, una franja gris clara | CAP-17, -18, -42, VID-03; LEG-02 es uniforme | En la costa, sin franja somera ni corte de las líneas a la derecha |
| QA-V8-08 | menor | architect | Pruebas planificadas que no existen: `BridgePassTests`, `EnvironmentSwitchTests` (10 alternancias, memoria ± 5 %: AC-SCN-01-18 queda NV), `EnvironmentSlotTests`, `EnvironmentRestartTests`, rueda, flechas, letra 1,3× y 0 B al desplazar el selector | 02-tech-design §7.3 y §14 ("Desvíos") | Sumarlas; AC-SCN-21-04 y AC-SCN-22-02 / -03 se cubrieron a mano en el build |
| QA-V8-09 | menor | architect | `.claude/worktrees/` no está en `.gitignore`: un `git add -A` sumaría el worktree de los agentes como repo embebido | `git check-ignore .claude/worktrees/x` no devuelve nada | Agregar `/.claude/worktrees/` |
| QA-V8-10 | menor | architect | VID-05 ("bote quieto y en pausa") se desliza ≈ 4,5 m al principio (d 500,1 → …) | `manifest.json`, `VID-05.values` | Arrancar la toma con la velocidad en 0 |
| QA-V8-11 | menor | architect | Sin `-rowmate-perf-env` ni `-rowmate-perf-hour` (02-tech-design §5): el escenario va por `prefs.json` y la hora 12 no se puede fijar. La línea `[PERF] escenario` ya está | `AppRoot.cs:55-90` | Agregar los argumentos o documentar `prefs.json` en `tools/unity/README.md` |
| QA-V8-12 | menor | po | AC-SCN-01-19 pide la regata "en `E-180`", pero no hay forma de arrancar el build en una distancia: se midió remando de 0 a ≈ 240 m | §4 | Aceptar la medición remando o pedir `-rowmate-perf-distance` |
| QA-V8-13 | menor | architect | Un cuadro de 58,1 ms (GPU 19,5 ms) a los 42 s de la regata en una de tres corridas; con otra sesión corriendo Unity sobre su worktree en paralelo | `<scratch>/it8/qa-r1/v2/p-regatta/player.log` contra `p-regatta2` / `p-regatta3` (≤ 3,3 ms) | Repetir en la verificación siguiente con la PC sola |
| QA-V8-14 | menor | designer | Costa TH a las 22 h: el farol del faro queda debajo de la barra superior del HUD; se ve solo el haz | CAP-24 | Revisar el encuadre o el alto del faro en TH (o aceptarlo) |
| QA-V8-15 | menor | architect | El reporte de la primera pasada (15:11) no valía: el árbol cambió mientras se medía (antes QA-V8-05). En esta pasada el árbol quedó fijo; queda como regla: un solo paso a la vez sobre el proyecto | §1 (md5 igual al empezar y al terminar) | — |

Resueltos desde la primera pasada: QA-V8-05 (árbol congelado; tomas del repo del código final y repetibles) y QA-V8-07 (`EnvironmentBudgetTests` mide por escenario dibujos, SetPass, triángulos, CPU y 0 B con control positivo; `SceneBudgetTests` Alto pasa en las dos corridas de PlayMode).

---

## 7. Para issues

- Ventanas del *skyline* con la máscara exacta del legacy (hoy un *hash* fijo; 04-design-notes §4).
- *Hover* 1,02 de las tarjetas en escritorio (04-design-notes §7.2).
- Reloj real en la torre de llegada: no se ve en ninguna toma (la torre está a 610 m).
- El panel del simulador queda en español con la app en inglés (viene de la It. 2).
- Fin de línea LF en los archivos nuevos (git los normaliza a CRLF).

---

## Ronda 2

| Campo | Valor |
|---|---|
| Fecha | 2026-11-07, 22:20 a 23:15 |
| Contra | Lo mismo que la ronda 1, más [02-tech-design](02-tech-design.md) §15 (reconciliación del worktree) y §16 (correcciones de la ronda 1) |
| Árbol verificado | El árbol sin commitear sobre `7e7b3c9` que dejó la ronda de correcciones (último cambio de fuentes a las 21:51; 02-tech-design a las 22:19). **Fijo durante toda la ronda**: el mismo `md5` de `git diff` + archivos nuevos y el mismo `git status` al empezar y al terminar. No vi cambios ajenos; en la PC no corría otro Unity (solo el `unity.exe serve` de la Hub, que no toqué) |
| Veredicto | **APROBADO CON OBSERVACIONES**: **39 PASS · 0 FAIL · 1 NO VERIFICABLE** (AC-SCN-01-19 en Android). 0 mayores; los 4 mayores de la ronda 1 quedan resueltos. Quedan menores para issues |

### R2.1 Corridas (conteos reales)

| Qué | Comando | Resultado |
|---|---|---|
| dotnet (.NET 8) | `tools/dotnet/run-tests.ps1 -Matrix -ArtifactsPath <scratch>/it8c/qa-r2/dotnet` | Build **0 avisos, 0 errores**. Herramientas **192 / 192**. Core **3 350: 3 311 pasan, 0 fallan, 39 omitidos** (43 s). Matriz: PASA 700, FALLA 0, IGNORADO 4, PENDIENTE UNITY 157, SIN TEST 0 |
| Unity EditMode | `run-unity-tests.ps1 -Platform EditMode` | **3 453: 3 408 pasan, 0 fallan, 45 ignorados**, 0 inconclusos |
| Unity EditMode + simulador | `… -Simulator` | **3 645: 3 600 pasan, 0 fallan, 45 ignorados**; define restaurado |
| Unity PlayMode | `-Platform PlayMode` | **551: 551 pasan** (`EnvironmentParityTests` 3 / 3, `EnvironmentBudgetTests` 12 / 12, `SceneBudgetTests` 10 / 10, los 25 casos legacy, `TenSwitchesKeepTheMemoryFlat`) |
| Unity PlayMode + simulador | `-Platform PlayMode -Simulator` | **624: 624 pasan** |
| Guardia de `ProjectSettings` | las cuatro corridas y el build | "sin cambios"; al final solo queda `RowMateBootstrap.json`, cambiado desde antes de empezar |
| Build | `tools/build/build.ps1 -Target Windows -Simulator` | **Éxito** → `Builds/Windows/simulator/RowMate.exe` (Mono, desarrollo), con `THIRD_PARTY_NOTICES.md` |
| `[PERF]` y demo | build, `ROWMATE_DATA_DIR` temporal por corrida (base sembrada `-seed F5`) | R2.4. Nada tocó `prefs.json` ni las bases del usuario (las fechas de la carpeta de datos del usuario no cambiaron) |
| Tomas | no se volvieron a sacar: se miraron las 57 del repo (las del arnés de §16; `it8-A` = `it8-B` según el Architect) | R2.3 |

### R2.2 Issues de la ronda 1

| ID | Estado | Evidencia |
|---|---|---|
| QA-V8-01 (mundo vivo) | **Resuelto** | Gaviotas: `SeagullRenderer` (un dibujo, solo en la costa) lo arma `SceneWorldBuilder.cs:634` y lo aplica y dibuja `SceneDirector.cs:121, 149`; se ven en CAP-17 a -24, CAP-45 y cuadros de VID-03 (4 y 12 s). Banderas, sombrillas, boyas y público: `Prop.shader` `PropMotion` (códigos 1, 3, 5 y 7 en el uv, en las tres pasadas) con `_RmSceneTime`; `KitSegmentMesh` marca boyas (también las de la regata), sombrillas, banderas y público; `FlagsUmbrellasAndBuoysAreMarkedToMove` y `TheCrowdIsMarkedToMove` pasan en dotnet. Banderas lejanas: `EnvironmentHorizonStrip.AddRegattaFlags`, visibles en CAP-26 (recorte del horizonte: un grupo de 4 colores). Espuma: `FoamRenderer`, la franja blanca al pie del acantilado en CAP-18 |
| QA-V8-02 (25 casos legacy) | **Resuelto** | `EnvironmentSceneTests`: LT-111 a -122 (`pinta sin excepciones`), LT-123 y LT-127 a -138 (`anima sin excepciones`), con el nombre del legacy, su `Category("LT-…")` y `LogAssert.NoUnexpectedReceived`; los 25 pasan en PlayMode |
| QA-V8-03 (paridad en pantalla) | **Resuelto** | `EnvironmentParityTests` 3 / 3: río, costa y regata × los 7 tamaños × 0 m, el lugar y 1 234 m; máx \|Δx\| 0 dp; base máx 1,2 / 2,5 / 1,0 m. CAP-44 a -46 con las marcas, lado a lado con LEG-01 a -03: los mismos objetos en el mismo lugar |
| QA-V8-04 (tomas y sondas) | **Resuelto, con un resto menor** | 57 tomas: CAP-01 a -46, VID-01 a -06 y DEMO-01 a -05, 0 con problemas. 11 sondas de color: pasan las de la cabaña (CAP-04, -08), las ventanas, el club y el *skyline* del río (CAP-12) y el faro en TV (CAP-20). Fallan 2 de aspecto en TH (CAP-16 farol `#F1D37B`, CAP-24 faro `#D2B86D`): es QA-V8-14. El farol (`#FFF8E1`) no tiene ninguna sonda que pase → QA-V8-R2-02 |
| QA-V8-06 (arista del mar) | **Resuelto** | CAP-17, -18 y -42 y VID-03: el mar abierto de la derecha es parejo hasta el horizonte |
| QA-V8-08 (pruebas que faltan) | **En parte** | Se sumó `TenSwitchesKeepTheMemoryFlat` (mallas 116 → 116; memoria administrada 732 012 544 → 731 955 200 B, −0,01 %). Siguen sin existir `BridgePassTests`, `EnvironmentSlotTests`, `EnvironmentRestartTests` y el selector con rueda, flechas, letra 1,3× y 0 B al desplazar → para issues |
| QA-V8-09 (`.claude/worktrees/`) | **Resuelto** | `git check-ignore -v .claude/worktrees/x` → `.gitignore:119:/.claude/worktrees/` |
| QA-V8-10 (VID-05 se deslizaba) | **Resuelto** | `manifest.json`, `VID-05.values`: `d 500` fijo en todos los cuadros, con arfada y cabeceo distintos de 0 |
| QA-V8-11 (`-rowmate-perf-env` / `-hour`) | **Resuelto** | `AppRoot.cs:81-82`; usados en las corridas de R2.4 (la línea `[PERF] medición de la escena` dice el escenario y la hora) |
| QA-V8-12 (`E-180` en el build) | **Abierto, menor** (po) | Sigue sin `-rowmate-perf-distance`: la regata se midió remando de 0 a ≈ 250 m (60 s), que pasa por `E-180` y la tribuna de 210 m |
| QA-V8-13 (tirón en la regata) | **Abierto, menor** | Con la PC sola se repitió una vez: ver QA-V8-R2-01 |
| QA-V8-14 (linterna bajo la barra en TH) | **Abierto, menor** (designer) | CAP-16 y CAP-24: la linterna del farol y la del faro quedan cortadas bajo la barra superior; son las 2 sondas de aspecto que fallan |

### R2.3 Lo que se vio en las tomas y los videos

Hojas por escenario y perfil (TV y TH × 6, 12, 18 y 22 h), recortes de CAP-18 y CAP-26, CAP-33 a -36, CAP-44 a -46 contra LEG-01 a -03, DEMO-01 y cuadros de VID-02 (17,5 s), VID-03 (4; 4,5; 12 s), VID-04 (4; 15 s) y VID-06 (2; 10; 16; 22; 28 s).

- **Bien:** todo lo de la ronda 1 sigue igual y ahora el mundo está completo. Costa: gaviotas oscuras sobre el mar y los acantilados, espuma al pie de las rocas, mar parejo sin la arista, faro con halo y haz a las 22 h. Regata: banderas lejanas en grupos de colores junto a la tira de tribunas y torres, público de colores en la tribuna, boyas rojas y después amarillas. Río: ciudad encendida de noche y el puente sin cortes (VID-02). Selector y miniaturas como en la ronda 1; VID-06 muestra elegir Costa, la app reabierta con Costa elegida y el libre en la costa.
- **Mal (menor):** en TH la linterna del faro (22 h) y la del farol del río (22 h) quedan bajo la barra superior (QA-V8-14). El público sigue siendo cajas sin el atlas de poses (DA8-12, ya para issues).
- **Contra el legacy:** las mismas piezas, en el mismo orden y en la misma x de pantalla (CAP-44 a -46 contra LEG-01 a -03 y la prueba de paridad en los 7 tamaños).

### R2.4 Build, demo y `[PERF]` (W1280, Alto, simulador, carpeta de datos temporal por corrida)

**Demo con clics reales en el build** (`<scratch>/it8c/qa-r2/demo`): Workout muestra `Scenery` con las 4 miniaturas y el lago elegido → `City river` → `prefs.json` = `{"scene.environment":"river"}` → `Free workout` rema en el río (23 h reales: ventanas y faroles encendidos) → cerrar y abrir → `Coast` → se guarda `"coast"` → rema entre el acantilado y la playa, de noche → `Regatta course` → `"regatta"` → rema entre las boyas, con tribuna y banderas → cerrar y abrir: Workout muestra `Regatta course` elegida al primer cuadro y `Free workout` va directo a la regata; `prefs.json` sin cambios al arrancar. El lago, por defecto, rema en una carpeta nueva. **`Hora escena`**: en el panel del simulador, de `real` a `22:00` y después a `12:00`; la regata pasa de la noche al mediodía con el sol, y `Hora real` aparece al forzarla. Sin excepciones en los registros. Los 4 elegidos y remados: 4 de 4.

| Corrida (`-rowmate-perf-scene`, `-env`, `-hour`) | Entrada máx | Régimen: p99 / máx del hilo principal | GPU máx | Dibujos / SetPass | Asignaciones en régimen |
|---|---|---|---|---|---|
| Lago 12 h (primera corrida después del build) | 36,4 ms | 3,8–5,3 / 31,1 ms | **33,2 ms** | 84 / 51 | ≤ 94; nivel **Low**, resolución 0,75; 3 subidas de *shaders* de URP después de `Ready` |
| Lago 12 h, repetida 2 veces | 23,0 / 23,8 ms | 2,8–3,1 / ≤ 11,1 ms | ≤ 7,0 ms | ≤ 104 / 65 | ≤ 85, GC 0; nivel High; 0 subidas |
| Río 12 h y 22 h | 20,8 / 20,6 ms | 1,8–2,0 / ≤ 6,4 ms | ≤ 5,8 ms | ≤ 86 / 31 | ≤ 85, GC 0 |
| Costa 12 h | 20,7 ms | 1,8–3,3 / **38,9 ms** a los 22 s | 6,2 ms | 77 / 30 | **2 112** en ese tramo, GC 0 |
| Costa 12 h (repetida) y 22 h | 21,1 / 22,1 ms | 1,9–2,0 / ≤ 6,4 ms | ≤ 6,9 ms | ≤ 78 / 32 | ≤ 86, GC 0 |
| Regata 12 h (60 s, pasa `E-180`) | 23,3 ms | 1,9–2,0 / ≤ 6,5 ms | 6,5 ms | ≤ 75 / 28 | ≤ 86, GC 0 |
| Regata 22 h | 23,4 ms | 1,9–2,1 / **89,9 ms** a los 33 s | 6,6 ms | **143 / 55** en ese tramo | 295, GC 0 |
| Regata 22 h (repetida 2 veces) y regata desde `prefs.json` | 21,2–23,5 ms | 1,9–2,0 / ≤ 6,4 ms | ≤ 7,0 ms | ≤ 77 / 30 | ≤ 86; GC 0 (1 en la entrada desde `prefs.json`) |

- La línea `[PERF] escenario` está en todas: `kit X, sesión X`, con 1 cambio de kit al elegir por argumento y 0 desde `prefs.json`.
- `perf-log-check.ps1`: 0 subidas de *shaders* después de `Ready` en 12 de 13 corridas; la excepción es la primera del lago (3 de URP, `UberPost` y `FinalPost` con `_FXAA`, al bajar de nivel).
- p99 ≤ 5,3 ms en las 13 corridas (tope 16,7). En 10 de 13, ningún cuadro > 33 ms; los 3 de más no se repitieron en 5 corridas más con la PC sola (QA-V8-R2-01).

### R2.5 Matriz AC → evidencia → resultado

| AC | Evidencia | Resultado |
|---|---|---|
| AC-SCN-14-08 (LT-99 a -107) | dotnet (`River/Coast/RegattaEnvironmentTests`, `EnvironmentGoldenTests`) | **PASS** |
| AC-SCN-14-09 (LT-109) | dotnet (topes 60 / 200 = la función del legacy) | **PASS** |
| AC-SCN-14-10 | `EnvironmentParityTests` 3 / 3 (7 tamaños, máx 0 dp, base ≤ 2,5 m); CAP-44 a -46 contra LEG-01 a -03 | **PASS** |
| AC-SCN-16-01 | dotnet; CAP-09 a -16 | **PASS** |
| AC-SCN-16-02 | dotnet; CAP-09 a -16 | **PASS** |
| AC-SCN-16-03 (LT-110) | dotnet; LT-123 (relZ 0,3 y 1,0) pasa; VID-02 a los 17,5 s | **PASS** |
| AC-SCN-16-04 | dotnet (sin cruces); CAP-09 a -16 | **PASS** |
| AC-SCN-17-01 | dotnet (`WaterInputsTests`); mar parejo en CAP-17, -18 y -42 | **PASS** |
| AC-SCN-17-02 | dotnet; CAP-18, -42 | **PASS** |
| AC-SCN-17-03 | dotnet; CAP-20, -24, VID-03 | **PASS** |
| AC-SCN-17-04 | CAP-18: estratos, pasto, rocas, **espuma** al pie, arena y sombrillas; `FoamRenderer` | **PASS** |
| AC-SCN-12-01 (LT-124) | dotnet; `TheCoastSwellMovesTheBoatButNotTheDistance`; CAP-42 | **PASS** |
| AC-SCN-06-04 | dotnet; VID-05 con `d` fijo y arfada y cabeceo ≠ 0 | **PASS** |
| AC-SCN-18-01 (LT-108) | dotnet; boyas con `MotionBuoy` (`PropMotion`: `0,08·sin(1,7t + fase)`) | **PASS** |
| AC-SCN-18-02 | dotnet; CAP-40, CAP-41; LT-123 (cartel de 750 m a 30 m) | **PASS** |
| AC-SCN-18-03 | dotnet; CAP-26, VID-04 (el reloj de la torre no se ve en ninguna toma: para issues, como en la ronda 1) | **PASS** |
| AC-SCN-18-04 | CAP-25 a -32; banderas con `MotionFlag` (`0,2·sin(4t + fase)`, con onda) y público con `MotionCrowd` (`TheCrowdIsMarkedToMove`) | **PASS** |
| AC-SCN-19-03 | dotnet; CAP-12, -16 (la sonda del *skyline* pasa) | **PASS** |
| AC-SCN-19-04 | dotnet (`SeagullFlockTests`); CAP-17 a -24, CAP-45, VID-03 | **PASS** |
| AC-SCN-19-05 | dotnet (`TheRegattaStripCarriesTheFarFlags`); CAP-26 | **PASS** |
| AC-SCN-20-03 | dotnet (medidas ± 10 %, paletas) | **PASS** |
| AC-SCN-20-04 | dotnet; sondas de CAP-04, -08, -12 y -20; CAP-12, -16, -24 a la vista (el farol sin sonda que pase: QA-V8-R2-02) | **PASS** |
| AC-SCN-20-05 (LT-111 a -123) | 13 casos de `EnvironmentSceneTests` en PlayMode | **PASS** |
| AC-SCN-20-06 | `PropMotion` con `_RmSceneTime` (sigue en pausa y con reducir movimiento), haz del faro, gaviotas y banderas lejanas `sin(3t + i)` en `Horizon.shader`; VID-03, VID-04 | **PASS** |
| AC-SCN-20-07 | `EachEnvironmentBuildsAndDraws` (0 `[SLOT]`), `EnvironmentAssetsTests`, `RepoHygieneTests`; `git ls-files` sin terceros | **PASS** |
| AC-SCN-21-02 | dotnet; `SceneEnvironmentChoiceTests` | **PASS** |
| AC-SCN-21-03 (LT-125) | dotnet + `EnvironmentPickerTests`; demo: se guardan `river`, `coast` y `regatta` | **PASS** |
| AC-SCN-21-04 (LT-126) | dotnet + PlayMode; VID-06; demo: reabierta con la elección y `prefs.json` igual | **PASS** |
| AC-SCN-21-05 | `EachEnvironmentBuildsAndDraws`, `SwitchingInRestRebuildsOnlyTheKit`; demo | **PASS** |
| AC-SCN-22-01 | `WorkoutScreenTests`; CAP-37, DEMO-01 y el build | **PASS** |
| AC-SCN-22-02 | `EnvironmentPickerTests`; CAP-37 a -39 y el build (las 4 en W1280) | **PASS** |
| AC-SCN-22-03 | `EnvironmentPickerTests` (toque y Enter); CAP-39; clics en el build | **PASS** |
| AC-SCN-22-04 | dotnet (`ThumbnailRule`); CAP-33 a -36 | **PASS** |
| AC-SCN-22-05 | `TheBakedThumbnailsAreAssignedAndLight`; CAP-38 (0 dibujos de escena en reposo) | **PASS** |
| AC-SCN-01-17 (LT-127 a -138) | 12 casos de `EnvironmentSceneTests` en PlayMode (6 cuadros de 100 ms y desmontar) | **PASS** |
| AC-SCN-01-18 | `TenSwitchesKeepTheMemoryFlat` (± 5 %: −0,01 %, mallas iguales) y `SwitchingInRestRebuildsOnlyTheKit`. La prueba no entra ni sale del inmersivo en cada cambio (QA-V8-R2-04) | **PASS** |
| AC-SCN-01-19 | **PC: cumple** (`EnvironmentBudgetTests` 12 / 12; build: p99 ≤ 5,3 ms en las 13 corridas, GC 0 en régimen, con 3 cuadros aislados > 33 ms que no se repitieron: QA-V8-R2-01). Android de gama media: sin teléfono | **NO VERIFICABLE** (Android) |
| AC-WKT-12-02 (cambia) | `WorkoutScreenTests` | **PASS** |
| AC-SCN-21-01 (cambia) | `SceneEnvironmentChoiceTests` | **PASS** |
| AC-SCN-14-05 (cambia) | `EnvironmentGoldenTests` con los topes reales | **PASS** |

**Casos legacy:** LT-99 a LT-138, los 40, portados y pasando (LT-99 a -110 y -124 a -126 en dotnet; LT-111 a -123 y -127 a -138 en PlayMode).

**Se vuelven a verificar (spec §4):** las suites de la 6a y la 7 pasan en las cuatro corridas de Unity; `SceneBudgetTests` Alto, Medio y Bajo pasa.

### R2.6 Repo público, terceros y codificación

- Rutas de la PC: `grep` de la carpeta de usuario, la carpeta temporal, los IDs de sesión, `.claude/jobs`, el nombre del equipo y `scratchpad/` en lo versionado y en los 306 archivos cambiados o nuevos (sin `.claude/worktrees/`): solo los patrones de los tests de higiene, la ruta genérica `AppData/LocalLow/iguisoft/RowMate` de ARCHITECTURE y el autor en `LICENSE` y `README`. Este reporte usa `<scratch>`. → **limpio**.
- Terceros: nada de `Assets/ThirdParty/` (salvo `.meta`) ni `.fbx` en lo versionado ni en lo nuevo; `.gitignore:70` lo ignora. `.claude/worktrees/` ignorado (`.gitignore:119`).
- UTF-8: los 229 archivos de texto cambiados o nuevos son UTF-8 válido, sin doble codificación.
- Legacy: `git status --porcelain` vacío al principio y al final; no se leyó el módulo de credenciales.
- No quedaron procesos míos corriendo (ni Unity ni `RowMate.exe`).

### R2.7 Issues nuevos (todos menores)

| ID | Sev. | Resp. | Issue | Evidencia | Corrección sugerida |
|---|---|---|---|---|---|
| QA-V8-R2-01 | menor | architect | **Tirones aislados en el build** (sigue QA-V8-13, ahora con la PC sola): regata 22 h, un cuadro de 85,3 ms de CPU (89,9 ms del hilo principal) a los 33 s, con **143 dibujos y 55 SetPass** en ese tramo (el doble del régimen); costa 12 h, uno de 38,9 ms a los 22 s con 2 112 asignaciones, junto a los avisos de Direct3D (`GetFrameStatistics`) y del hilo de entrada en el registro; la primera corrida del lago bajó a Low (resolución 0,75), con 33 ms de GPU, 36,4 ms en la entrada y 3 subidas de *shaders* de URP después de `Ready`. Ninguno se repitió en 5 corridas más | `<scratch>/it8c/qa-r2/perf/{regatta22,coast12,lake12}/player.log` contra `regatta22b`, `regatta22c`, `coast12b`, `lake12b` y `lake12c` | Perfilar la regata de noche con `-rowmate-perf-profile` y explicar el doble de dibujos en ese tramo; que la caída de nivel en frío no suba variantes de URP sin precalentar |
| QA-V8-R2-02 | menor | architect | El farol (`#FFF8E1`, issue #9) no tiene sonda que pase: la única está en CAP-16 (TH), cortada por la barra | `manifest.json`; CAP-12 sin sonda de farol | Sumar la sonda del farol en CAP-12 (TV). Con eso, el #9 queda cerrado |
| QA-V8-R2-03 | menor | architect | Al entrar al libre 1 s después de abrir Workout, la escena demorada todavía no había aparecido a los 19 s de remar (fondo provisorio liso). A los 28 s ya estaba, en el lago y en la regata. No se probó que venga de la 8 | `<scratch>/it8c/qa-r2/demo/shots/d6-regatta-12h.png` (19 s) contra `rA.png` y `rB.png` (28 s); `[PERF] entrada … escena Loading` | Medir cuánto tarda la escena demorada si se entra enseguida y, si pasa de unos segundos, priorizar su preparación en el inmersivo |
| QA-V8-R2-04 | menor | architect | `TenSwitchesKeepTheMemoryFlat` cambia de escenario en reposo, pero AC-SCN-01-18 pide alternar **entrando y saliendo del inmersivo** y mirar la memoria total; la prueba mide la administrada y la cantidad de mallas | `EnvironmentSceneTests.cs:82-125` | Entrar y salir del libre en cada cambio y sumar la memoria nativa (`Profiler.GetTotalAllocatedMemoryLong`) |
| QA-V8-R2-05 | menor | architect | LT-127 a -138 no fijan SPM 24 ni split 110 (usan el monitor del arnés y `ForceDistanceForTest(300)`) | `EnvironmentSceneTests.cs:371-392` | Fijar en la prueba los valores del monitor del legacy |

### R2.8 Para issues

- QA-V8-R2-01 a -05 (arriba).
- Lo que sigue abierto de la ronda 1: QA-V8-08 (`BridgePassTests`, `EnvironmentSlotTests`, `EnvironmentRestartTests`, selector con rueda, flechas, letra 1,3× y 0 B al desplazar), QA-V8-12 (`-rowmate-perf-distance`) y QA-V8-14 (linterna bajo la barra en TH).
- De la ronda 1 §7: la máscara exacta de las ventanas del *skyline*, el *hover* 1,02 de las tarjetas, el reloj real de la torre, el panel del simulador en español con la app en inglés y el fin de línea LF.
- De 02-tech-design §15 y §16: `EnvironmentCardLook` sin copiar, el presupuesto de `Foam` (62 ALU), el público en cajas (DA8-12) y la sombra de las gaviotas.
