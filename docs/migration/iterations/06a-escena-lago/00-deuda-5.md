# It. 6a — Deuda de la It. 5, saldada al empezar

| Campo | Valor |
|---|---|
| Autor | unity-architect |
| Fecha | 2026-11-02 |
| Iteración | 6a · `06a-escena-lago` ([ROADMAP §It. 6a](../../ROADMAP.md)). Primer trabajo de la iteración, antes de construir encima |
| Entrada | [REPORT de la 5](../05-series-parciales/REPORT.md) §5.1, §5.2, §7 y §9.2 (punto 1); [05-qa-verification de la 5](../05-series-parciales/05-qa-verification.md), §6 y "Ronda 2"; [lente de motion](../05-series-parciales/05-qa-lens-motion.md) §6 (O-3, O-4, O-5); [02-tech-design de la 5](../05-series-parciales/02-tech-design.md) §3.7 |
| Qué se salda | La verificación en el build que quedó NO VERIFICABLE en la 5: `[PERF]` de AC-WKT-13-15 y de AC-WKT-18-21 y la prueba a mano en la ventana (AC-WKT-18-10, -18-12, AC-APP-05-07, AC-BLE-15-09, AC-WKT-18-15, -18-16 y Q-12). De §7: O-3 (*hover* debajo del diálogo), O-4 (las cadenas de un carácter), O-5 (el corte de los vacíos), la capa de *hover* que no entraba en la medida de 0 bytes, y una herramienta nueva: el 198 de Unity sin licencia |
| No se toca | P-17 / AC-WKT-02-11: sigue esperando la demo (c) del usuario (§2). QA-V5-R2-01 es del Designer ([00-deuda-diseno](00-deuda-diseno.md) §1) |
| Legacy | [rowmate-legacy](https://github.com/figuibej/rowmate-legacy), solo lectura: no hizo falta leerlo. `git -C ../rowmate-legacy status --porcelain` vacío al empezar y al terminar. No se leyó el módulo de credenciales de Strava |
| Repo | Sin commits. Registros, capturas de la prueba a mano y ayudas Win32 en `<scratch>/it6a/debt/` (no van al repo: las capturas de la ventana se quedan ahí) |

---

## 0. Resumen

| ID | Qué faltaba | Qué se hizo | Estado |
|---|---|---|---|
| **AC-WKT-13-15** (`[PERF]` de la entrada) | La medida en el build de las tres entradas | Tres corridas de `-rowmate-perf-entries 3 -rowmate-perf-routines "Prueba 4a;Cinco reps"` con los dos paneles en verdadero, y una sin precalentado para comparar (§1.2) | **PASA**: en las tres entradas, ningún cuadro de los primeros 10 s pasa de **14,7 ms** (objetivo 16,7); el peor de los primeros 30 cuadros, 12,1-12,5 ms en el libre |
| **AC-WKT-18-21** (`[PERF]` con los paneles) | `Fondo 3000` y `Series 600` con los paneles abriéndose y cerrándose | Cuatro corridas de `-rowmate-perf-series` (una de 120 s), y una sin precalentado (§1.3) | **PASA**: ningún cuadro de más de 33 ms (el peor, **25,0 ms**) y el p99 entre 1,0 y 1,4 ms |
| Prueba a mano en la ventana (§5.1 y §5.2 de la 5) | Mouse y teclado, reinicio con los paneles recordados, `Simular desconexión`, la cuenta grande, el sello, `PAUSA`, matar el proceso | Tres arranques del build con una carpeta de datos temporal, manejados con entrada sintética (§1.4) | **PASA** todo: AC-WKT-18-10, -18-12, AC-APP-05-07, AC-BLE-15-09, AC-WKT-18-15, -18-16 y Q-12 |
| **O-3** (lente de motion) | ¿El *hover* de un panel queda debajo del velo si el diálogo se abre con el teclado y el mouse quieto encima? | Medido en el build (§1.4, paso 6) | **Cerrado sin cambios**: al aparecer el velo el *hover* se apaga |
| **O-4** | Las cadenas de un carácter: 8231 armadas de entrada (≈ 300 KB) en `DrawnFont`; en `TabularNumber`, lo que pasaba de U+00FF asignaba en cada dibujo | `Core/Common/CharStrings` (§3.1) | **Cerrado** |
| **O-5** | El corte de los vacíos en dos renglones se rehacía una vez por segundo con el mismo texto | Se guarda el corte hasta que cambie el texto o la fuente (§3.2) | **Cerrado** |
| La capa de *hover* fuera de la medida de 0 B | El puntero de prueba no la prendía | Una escena más en `HBT.SeriesNoAllocationsPerFrameInMoreScenes` (§3.3) | **Cerrado**: 0 bytes |
| Herramientas: el 198 | Después de reiniciar el equipo, con la Hub cerrada, Unity salía con 198 sin explicación | Los tres *scripts* lo explican y el README dice qué hacer (§3.4) | **Cerrado** |
| P-17 / AC-WKT-02-11 | La demo (c) del usuario | Nada (§2) | **Pendiente de la demo del usuario** |

| Corrida | Resultado |
|---|---|
| dotnet (`run-tests.ps1 -Matrix -UnityResults`) | 0 avisos, 0 errores · herramientas **192 / 192** · el resto **2547: 2513 pasan, 0 fallan, 34 ignoradas** (eran 2541 / 2507: +6, §3.5) · matriz de 658 IDs: **PASA 656, FALLA 0, IGNORADO 2** (Android y Apple, como en la 5) |
| Unity EditMode | **2606: 2568 pasan, 0 fallan, 38 ignoradas** (eran 2601 / 2563: +5, §3) |
| Unity EditMode con simulador | **2798: 2760 pasan, 0 fallan, 38 ignoradas, 0 inconclusas** (eran 2793 / 2755); el *define* restaurado |
| Unity PlayMode | **427 / 427** (eran 426: +1, SPV) |
| Unity PlayMode con simulador | **496 / 496** (eran 495); el *define* restaurado |
| Build de Windows con simulador | **Generado** con el código final en `Builds/Windows/simulator` (Mono, desarrollo). Con él, otra vez `[PERF]`: las tres entradas, 11,6 · 5,6 · 2,3 ms (peor ventana 11,6 · 10,9 · 7,8); los paneles, 22,5 ms la primera entrada directa de `Fondo 3000` y ≤ 7,3 ms el resto, p99 ≤ 1,2 ms |
| Arnés `it5` (A y B) | Las dos: `47 tomas, 0 con problemas, 0 sondas de aspecto que fallan`; **A = B = lo versionado**, byte a byte en los 49 archivos (en `manifest.json`, solo `durationMs`). No cambia ninguna toma |

---

## 1. La verificación en el build

### 1.1 Cómo

- **El build.** `Builds/Windows/simulator` tenía el build del cierre de la 5 (ningún archivo del proyecto era más nuevo que sus ensamblados). Las medidas de §1.2 y §1.3 y la prueba a mano de §1.4 son con ese build, el del código de la 5. Los cambios de §3 no tocan lo que se mide; con el build nuevo se repitió una medida de cada tipo (§4).
- **Los datos.** Nunca la carpeta de datos del usuario: `ROWMATE_DATA_DIR` en una carpeta propia de `<scratch>` por corrida y `-logFile` en ella. La base se sembró con `SnapshotCli.SeedDatabase -seed F5 -prefs laps,reps` (como `seed.db`, porque `TempDatabase.Guard` no deja escribir `rower_app_dev.db` fuera de la carpeta temporal de pruebas) y se copió como `rower_app_dev.db` en cada carpeta; `prefs.json` se copió solo donde se querían los paneles abiertos. Antes se tomó el nombre, el tamaño, la fecha y un *hash* de cada archivo de la carpeta de datos del usuario (en `<scratch>`): al terminar, igual (§4). Las preferencias de la ventana del *player* (registro de Windows) se resguardaron antes y se restauraron al final.
- **La ventana.** 1280 × 720 en ventana (`-screen-fullscreen 0 -screen-width 1280 -screen-height 720`), adelante durante cada medida. Solo se cerró o se mató el proceso que lanzó cada corrida.
- **El idioma.** El build sigue el idioma del sistema: las capturas de la prueba (en `<scratch>`) tienen los textos de ese idioma. Lo que se compara no depende del idioma.

### 1.2 `[PERF]` de las tres entradas (AC-WKT-13-15)

`RowMate.exe -rowmate-perf-entries 3 -rowmate-perf-routines "Prueba 4a;Cinco reps"`, con `prefs.json` en verdadero para los dos paneles: la primera entrada es libre, la segunda `Prueba 4a` y la tercera `Cinco reps`, que empieza con la serie, así los dos paneles llegan con la cascada. Cada entrada dura 12 s y termina como con `Terminar`. `FrameTimeProbe` escribe el peor de los primeros 30 cuadros de cada entrada y el peor y el p99 de cada ventana de 10 s (CPU del hilo principal).

| Corrida | Libre: 30 cuadros · ventana | `Prueba 4a`: 30 cuadros · ventana | `Cinco reps` (los dos abiertos): 30 cuadros · ventana | p99 |
|---|---|---|---|---|
| 1 | 12,5 · 12,5 ms | 6,0 · 14,7 ms | 3,1 · 7,9 ms | 1,1-1,5 ms |
| 2 | 12,4 · 12,4 ms | 6,5 · 8,0 ms | 2,5 · 7,8 ms | 1,1-1,3 ms |
| 3 | 12,1 · 12,1 ms | 7,0 · 9,0 ms | 2,7 · 8,1 ms | 1,2 ms |
| Sin precalentado (`-rowmate-no-hud-warmup`) | 18,0 · **39,3** ms | 8,2 · **37,4** ms | 3,0 · 14,0 ms | 1,1-1,4 ms |

- **Pasa.** El objetivo es que ningún cuadro que se mueve pase de 16,7 ms. La primera ventana de cada entrada empieza en el toque y cubre los primeros 10 s enteros (el fundido, el negro con el armado del HUD, la aparición del fondo y la cascada): su peor cuadro es de **14,7 ms**. No hizo falta separar los cuadros negros y quietos.
- **El precalentado (DT5-D2) hace su trabajo.** Sin él, el armado del HUD en el negro (DT5-D1) cuesta 37-39 ms en las dos primeras entradas, que es lo que AC-WKT-13-15 admite solo porque ese cuadro es negro y quieto. Con él, 12,5 ms o menos.
- **Un límite de la medida.** La línea `entrada del inmersivo` mira los primeros 30 cuadros: en una pantalla de más de 60 Hz no llega al cuadro del armado (≈ 230 ms después del toque). Por eso la tabla da también el peor de la ventana, que sí lo incluye. Para la 6a, §5.

### 1.3 `[PERF]` con los paneles (AC-WKT-18-21)

`RowMate.exe -rowmate-perf-series <s>` (sin `prefs.json`: los paneles arrancan cerrados): `Fondo 3000` y después `Series 600`, la mitad de los segundos cada una, abriendo y cerrando parciales y repeticiones cada 2 s por la API de las preferencias.

| Corrida | `Fondo 3000`: 30 cuadros · peor ventana | `Series 600`: 30 cuadros · peor ventana | p99 de las ventanas |
|---|---|---|---|
| 120 s | 24,3 · 24,3 ms (las demás, ≤ 2,4) | 3,0 · 11,2 ms | 1,0-1,4 ms |
| 24 s | 24,1 · 24,1 ms | 3,2 · 8,3 ms | 1,3-1,4 ms |
| 24 s | 25,0 · 25,0 ms | 2,9 · 7,6 ms | 1,3 ms |
| 24 s, sin precalentado | 29,3 · **61,5** ms | 2,9 · 9,8 ms | 1,4 ms |

- **Pasa**: ningún cuadro de más de 33 ms (el peor, 25,0) y el p99 muy por debajo de 16,7. Con los paneles abriéndose y cerrándose, las ventanas siguientes de `Fondo 3000` no pasan de 2,4 ms y las de `Series 600`, de 11,2.
- **El cuadro de 24-25 ms** es el de la primera entrada del proceso, y solo con este argumento: `-rowmate-perf-series` arranca `Fondo 3000` directo con `StartRoutine`, sin pasar por la lista ni por el diálogo de confirmación, así que la primera entrada paga también la primera vez del código que en la app real corre antes, al tocar la rutina. Con el camino del usuario (Workout → la rutina → `Iniciar`) la primera entrada del proceso, también con una rutina, dio **14,6 a 15,2 ms** en los tres arranques de la prueba a mano (§1.4). No es parte de AC-WKT-13-15 (sus tres entradas son las de §1.2) ni lo hace fallar a AC-WKT-18-21 (≤ 33 ms), pero queda anotado para la medida de la entrada de la 6a (§5).

### 1.4 La prueba a mano en la ventana

Tres arranques del build sobre la misma carpeta de datos temporal (sembrada con F5, sin `prefs.json` al empezar), manejados con ayudas Win32 de `<scratch>` (clic, movimiento del mouse y teclas solo si la ventana del build es la de adelante y el punto es suyo). Las capturas quedaron en `<scratch>`.

| # | Paso | Qué se vio | Resultado |
|---|---|---|---|
| 1 | `Prueba 4a` → `Iniciar`, sin preferencias | En el calentamiento, el chip `500 m` cerrado y ningún chip de repeticiones | Bien (AC-WKT-18-01, -18-02) |
| 2 | Clic en `500 m` | Se abre con el vacío en dos renglones; a los 30 s, el trabajo: la vuelta en curso, `Rep 1/2 · …` y aparece el chip de repeticiones | Bien (AC-WKT-18-09) |
| 3 | El teclado (con el código de rastreo, §1.5) | Tab recorre `Terminar` → `Pausa` → `500 m` → repeticiones → la llave → `Terminar`, cada uno con su anillo; Mayús+Tab al revés; Enter abre parciales y Espacio lo cierra y lo vuelve a abrir; Enter sobre `Terminar` abre el diálogo de terminar y Escape lo cierra con el foco de vuelta en `Terminar` | Bien (AC-WKT-18-12, AC-APP-05-07) |
| 4 | El sello con los dos paneles abiertos (al terminar el trabajo 2) | El sello de objetivo no alcanzado, con sus valores, sin tocar los paneles | Bien (AC-WKT-18-16) |
| 5 | La cuenta grande (los últimos 3 s del descanso 2) | `3` en su disco, con los paneles abiertos al costado, sin encimarse | Bien (AC-WKT-18-15) |
| 6 | **O-3**: el mouse quieto sobre la cabecera de parciales (con el *hover* encendido), Tab hasta `Terminar` y Enter | El diálogo aparece y la franja de la cabecera debajo del velo tiene el mismo color que con el mouse afuera: el *hover* se apagó al aparecer el velo. Medido en los píxeles de la franja: con *hover* sin diálogo, más clara; con el diálogo, igual con el mouse encima y afuera | Bien: **O-3 no pasa** (UI Toolkit manda la salida del puntero cuando el velo queda debajo) |
| 7 | `PAUSA` con los paneles | El disco en su lugar; los paneles se abren y se cierran en pausa | Bien |
| 8 | **Matar el proceso** a mitad de sesión (en marcha, en el enfriamiento) | La base quedó con la sesión `Prueba 4a` cortada (sin fin) y sus puntos cada 5 s del tiempo en marcha (45; la pausa no suma), leída con `RowMate.SessionList --db` sobre una copia. Al volver a abrir la app: arranca sin avisos ni errores en el registro y se puede empezar otra sesión | Bien (Q-12; P-03 de la 4a) |
| 9 | Reiniciar con los dos paneles abiertos | `Series 600`: parciales abierto desde el primer cuadro de la cascada, y repeticiones abierto desde que empieza la serie, con `Sin repeticiones completas` | Bien (AC-WKT-18-10) |
| 10 | `Simular desconexión` (la llave → el botón) | Durante ≈ 4 s los metros de la vuelta en curso quedaron quietos (64 m) mientras su tiempo seguía; después siguieron sin salto (71, 75, 83 m) y nada negativo. El panel del simulador no tapó Split, Watts ni la barra | Bien (AC-BLE-15-09) |
| 11 | Cerrar los dos, `Terminar` → `Terminar`, cerrar la app y volver a abrirla | `prefs.json` con las dos claves en falso; Workout en reposo sin diálogo ni aviso; al iniciar otra vez, los dos chips cerrados | Bien (AC-WKT-18-10) |

En los tres registros: ninguna excepción ni error; `[SIM] Desconexión simulada, reconectando en 3s` en el paso 10. Lo que no se probó a mano: la rueda (en la ventana de 1280 × 720 las filas se desplazan con 4 vueltas o más y la entrada sintética de la rueda no llegó, §1.5), el arrastre y el dedo (teléfono).

### 1.5 Lo que la prueba enseñó de las herramientas

- **La entrada sintética.** Con `keybd_event` sin el código de rastreo, el `KeyDown` llega a UI Toolkit (Espacio y Enter funcionaban, y el anillo se encendía), pero el Tab **no movía el foco**: la navegación de UI Toolkit sale del Input System (`InputSystemProvider`, la acción de Tab), que lee la tecla física. Parecía un error de foco de los paneles; con `MapVirtualKey(vk, 0)` como código de rastreo, todo se comporta como con el teclado de verdad (paso 3). Del mismo modo, `SetCursorPos` no mueve el puntero de Unity (ni *hover* ni rueda): hace falta `mouse_event` absoluto. Queda en `tools/unity/README.md` para QA.
- **El 198.** Después de reiniciar el equipo, con Unity Hub cerrada, el primer Unity en *batchmode* salió con 198 ("No valid Unity Editor license found"): la licencia la guarda el cliente de licencias de la Hub y el Editor, sin la Hub, arranca su propio cliente, que no la ve. Con la Hub abierta, la primera corrida salió con 1 (la Hub todavía cargaba) y la siguiente, bien. §3.4.

---

## 2. P-17 / AC-WKT-02-11

Sigue pendiente de la demo (c) del usuario ([REPORT de la 5 §3.1 (c)](../05-series-parciales/REPORT.md)): suspender Windows a mano (Inicio → Encender/apagar → Suspender) con un entrenamiento en marcha. No se hizo nada: el equipo no se suspende desde acá. Si al hacerla el reloj salta el tiempo suspendido, el criterio se reabre y va primero.

---

## 3. La deuda del Architect de REPORT §7

### 3.1 O-4: las cadenas de un carácter (`Core/Common/CharStrings`)

Los textos dibujados carácter por carácter (`DrawnFont`, `TabularCells` y `TabularNumber`) necesitan la cadena de cada carácter en cada repintado. Había dos tablas propias: la de `DrawnFont`, con las 8231 cadenas de U+0000 a U+2026 armadas al cargar (≈ 300 KB fijos), y la de `TabularNumber`, con las 256 de Latin-1 y `c.ToString()` para el resto, **en cada dibujo**. Hoy ningún texto lo dispara, pero un `—` o un `…` en un `TabularNumber`, o un signo menos (U+2212) en cualquiera, habrían asignado en cada cuadro.

`CharStrings.Of(c)` (Core puro, lo prueban dotnet y Unity) reemplaza a las dos: Latin-1, la puntuación general (U+2000-U+206F: `–`, `—`, `…`, los espacios finos) y el signo menos se arman al cargar la clase (369 cadenas, unos 10 KB), y cualquier otro carácter se arma la primera vez y queda guardado. `DrawnFont.CharString` y `TabularNumber.CharString` la llaman. No cambia nada de lo que se dibuja.

### 3.2 O-5: el corte de los vacíos

`SeriesPanelView.SplitEmpty` (los vacíos en dos renglones parejos, DD5-19) corría en cada `Remeasure`, es decir, en cada `Apply`: una vez por segundo, `Split`, las medidas de cada palabra y dos `Join` con el mismo texto. Ahora guarda el texto y la fuente con los que cortó y lo rehace solo si cambian (otro idioma) o si el panel vuelve a un panel de UI Toolkit (`OnAttach`, QA-G5T-12). `EmptySplits` (interno) cuenta los cortes para la prueba.

### 3.3 La capa de *hover* en la medida de 0 bytes

`HBT.SeriesNoAllocationsPerFrameInMoreScenes` suma la escena "la capa de hover": el puntero de prueba (`UiTestHost.PointerEnter` / `PointerLeave`, eventos de mouse) entra en parciales y sale 12 cuadros después, dentro de la misma ventana de 30 cuadros medida con el ayudante de cuadros reales. El *hover* llega a su alfa (0,04) y vuelve a 0; **0 bytes** por cuadro. `MeasureSeries` acepta una acción antes de cada cuadro, fuera de la medida (como el reloj de prueba).

### 3.4 Herramientas: el 198 de Unity

`run-unity-tests.ps1` y `capture-snapshots.ps1` (en `Invoke-Unity`) y `tools/build/build.ps1` escriben, si Unity sale con 198: `Unity salio con 198: no encontro una licencia valida. Abri Unity Hub (con la sesion iniciada), espera a que cargue y repeti el comando.` Siguen siendo solo ASCII. `tools/unity/README.md` suma una sección de la 6a con el 198, cómo sembrar la base de la medida (el nombre de la semilla y la copia; la línea de la 5 decía `rowmate.db`, que el build no abre), lo que mide cada argumento de `[PERF]` y la entrada sintética de §1.5.

### 3.5 Pruebas nuevas

| Dónde | Prueba | Qué fija |
|---|---|---|
| dotnet y Unity (`CharStringsTests`, CHS) | `EachCharacterGivesItsOwnText`, `TheSameCharacterAlwaysGivesTheSameString`, `LatinPunctuationAndMinusArePrebuiltAndTheRestIsBuiltOnce`; solo dotnet: `AskingForCharactersDoesNotAllocate` (0 bytes con el contador del hilo de .NET 8) | O-4 |
| dotnet y Unity (`ItSixADebtTests`, I6D) | `UnityLicenseExitIsExplained` (los tres *scripts*, solo ASCII, y el README); `DrawnTextsAskCharStringsForTheirCharacters` (sin tablas propias ni `c.ToString()`) | §3.4 y O-4 |
| PlayMode (SPV) | `TheEmptySplitIsRedoneOnlyWhenTheTextChanges`: 6 s del descanso con el mismo vacío, ningún corte nuevo; en inglés, otro corte y después quieto; los renglones de DD5-19 en los dos idiomas | O-5 |
| PlayMode (HBT) | La escena "la capa de hover" en `SeriesNoAllocationsPerFrameInMoreScenes` | §3.3 |

Las pruebas de dotnet no llevan la categoría de AC-WKT-18-21: es de Unity (`It05UnityOnlyIdsHaveNoDotnetResult`). Además, las cuatro pruebas de equipo de `WorkoutDeviceChecksTests` que esto verificó a mano (`WindowsEntryWithoutJank`, `WindowsPanelsPerf`, `WindowsKeyboardOnThePanels` y `WindowsPanelsSurviveARestart`) siguen como `[Ignore]` (son manuales), con el motivo al día: verificadas en el build, con la sección de este documento.

### 3.6 Lo demás de §7, que no es de ahora

- **DT5-13** (los números de los paneles repiten la lógica de los del HUD): se unifica cuando se toque esa pieza por otro motivo, como decía el REPORT.
- **Para la 6a, con la escena**: la fila de 200 píldoras en el teléfono (R4b-02), los 60 fps con el diálogo de completado abierto (O-2 de la 4b), el velo de pausa en la escena y el negro de la entrada en el vuelo de cámara. Son parte del diseño de la 6a, no deuda que se salde antes. El presupuesto del HUD sigue en **56-57 de 60**: nada de esto suma elementos.
- **O-R2-01 de la 4a** (`FlushAndWait`): cuando se use.
- **O-1 y O-6** de la lente de motion: comportamiento del legacy y del sistema, ya contados en el REPORT.

---

## 4. Corridas

Desde la raíz del repo, una instancia de Unity por vez. Todo con el código final, salvo las medidas de §1.2 a §1.4 (el build del cierre de la 5) y el motivo de los cuatro `[Ignore]` de §3.5, que se cambió después de las corridas de Unity (es un texto: lo compilaron dotnet, el build y el arnés).

| # | Comando | Resultado |
|---|---|---|
| 1 | `Unity -batchmode -quit -executeMethod RowMate.Snapshots.SnapshotCli.SeedDatabase -snapshotSeedDb <scratch>/…/seed.db -seed F5 -prefs laps,reps` | La semilla F5 y `prefs.json`. La primera vez salió con **198** (la Hub cerrada) y, con la Hub recién abierta, con 1; la tercera escribió la base y Unity reventó al cerrar (0xC0000005, la conocida), con la base y `prefs.json` escritos (§1.5) |
| 2 | `RowMate.exe -rowmate-perf-entries 3 -rowmate-perf-routines "Prueba 4a;Cinco reps"` × 3, y una con `-rowmate-no-hud-warmup` | §1.2. Las cuatro salen con 0 |
| 3 | `RowMate.exe -rowmate-perf-series 120`, `… 24` × 2 y `… 24 -rowmate-no-hud-warmup` | §1.3. Las cuatro salen con 0 |
| 4 | La prueba a mano: tres arranques sobre la misma carpeta de datos | §1.4 |
| 5 | `powershell -NoProfile -File tools/dotnet/run-tests.ps1 -ArtifactsPath <scratch>/it6a/debt -Matrix -UnityResults <scratch>/it6a/debt/unity` | 0 avisos, 0 errores · herramientas 192 / 192 · el resto 2547: 2513 pasan, 0 fallan, 34 ignoradas · matriz PASA 656, FALLA 0, IGNORADO 2. (Una corrida anterior, con el *define* del simulador puesto por la corrida de Unity de al lado, dio una ignorada más: `CommittedSettingsDoNotEnableTheSimulator`, que se aparta mientras dura esa corrida. Se repitió con Unity cerrado) |
| 6 | `tools/unity/run-unity-tests.ps1 -Platform EditMode` | **2606: 2568 pasan, 0 fallan, 38 ignoradas** |
| 7 | `… -Platform PlayMode` | **427 / 427**; `[HBT] la capa de hover: 0 asignaciones (0 en 18 dibujos y 0 en los pasos) en 30 cuadros`; el `TestResults.xml` del Editor, borrado por el *script* (O-R2-01) |
| 8 | `… -Platform EditMode -Simulator` | **2798: 2760 pasan, 0 fallan, 38 ignoradas, 0 inconclusas**; el *define* restaurado, sin la marca |
| 9 | `… -Platform PlayMode -Simulator` | **496 / 496**; el *define* restaurado |
| 10 | `tools/build/build.ps1 -Target Windows -Simulator` | 0: `Builds/Windows/simulator` con el código final |
| 11 | Con el build nuevo: `-rowmate-perf-entries 3 …` y `-rowmate-perf-series 60` | Las tres entradas, 11,6 · 5,6 · 2,3 ms (peor ventana 11,6 · 10,9 · 7,8); los paneles, 22,5 ms en la primera entrada directa de `Fondo 3000`, el resto ≤ 7,3 ms; p99 ≤ 1,2 ms. Sin errores en el registro |
| 12 | `tools/unity/capture-snapshots.ps1 -Set it5 -Out <scratch>/…/it5-A` y otra vez a `…/it5-B` | Las dos: 47 tomas, 0 con problemas, 0 sondas que fallan; A = B = lo versionado (salvo `durationMs`). `it4b` no se volvió a tomar: `CharStrings` devuelve las mismas cadenas que las tablas de antes y las tomas de `it5` (que tienen todo el HUD) salen iguales |

Al terminar: la carpeta de datos del usuario, igual a la línea de base (sin `prefs.json`; el `TestResults.xml` que dejaron las corridas PlayMode lo borró el *script*); las preferencias de la ventana del *player*, restauradas e iguales al resguardo; `ProjectSettings` sin cambios y sin la marca del simulador; ningún `RowMate.exe` ni Unity de estas corridas abierto. Quedó abierta Unity Hub, que se abrió para la licencia (§1.5): la necesitan las corridas de Unity que siguen.

---

## 5. Para QA, el PO y la 6a

- **QA**: la verificación del build de la 5 está hecha (§1). Para repetirla, los pasos están en §1.1 y en `tools/unity/README.md`; las teclas sintéticas, con el código de rastreo.
- **PO**: AC-WKT-13-15 y AC-WKT-18-21 pasan en el build; los "con parte pendiente" de la 5 que eran de la ventana (AC-WKT-18-10, -18-12, AC-APP-05-07, AC-BLE-15-09) se vieron en el build. Siguen pendientes los del teléfono, IL2CPP y la demo (c) del usuario.
- **Para el diseño de la 6a** (la entrada con el vuelo de cámara, AC-WKT-13-15 y AC-SCN-01-10):
  - medir la entrada **por tiempo**, no por cuadros: los primeros 30 cuadros no llegan al armado del HUD en una pantalla de más de 60 Hz (§1.2), y con el vuelo la entrada dura más;
  - que la medida de la entrada con rutinas pase por el camino del usuario (la lista y el diálogo de confirmación) o que el precalentado cubra también el arranque de una rutina: `-rowmate-perf-series` da 24-25 ms en su primera entrada y el camino real, 14,6-15,2 ms (§1.3);
  - el armado del HUD en el negro (DT5-D1) tiene que encontrar otro lugar si el negro se va con el vuelo: sin precalentado, ese armado cuesta 37-39 ms en el build.
