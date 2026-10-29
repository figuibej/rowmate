# It. 4a — Verificación de QA (principal)

| Campo | Valor |
|---|---|
| Autor | rowmate-qa ([rol](../../../../.claude/agents/rowmate-qa.md)), momento 2: verificación |
| Fecha | 2026-10-28 |
| Iteración | 4a · `04a-motor-hud-funcional` |
| Alcance | La verificación principal y la única que usa Unity: dotnet, las cuatro suites de Unity, el arnés de capturas (`it4a`, y `it3` e `it2` para comparar), los dos builds de Windows, la prueba en equipo con el build de simulador, cada AC de [01-po-spec](01-po-spec.md), la deuda de la It. 3 ([00-deuda-it3](00-deuda-it3.md)) y la revisión estática. Una corrida anterior de esta verificación se cortó por el límite de uso y no dejó informe. Esta la rehace completa, con corridas y evidencia propias; de aquella solo se reusaron algunos scripts de ayuda, en `<scratch>` |
| Lentes | [05-qa-lens-paridad](05-qa-lens-paridad.md) y [05-qa-lens-robustez](05-qa-lens-robustez.md) ya reportaron. No se repitió su trabajo. Los dos mayores de la lente de robustez (ROB-01 y ROB-02) se **reprodujeron en Unity** (§4) |
| Legacy | [rowmate-legacy](https://github.com/figuibej/rowmate-legacy) en `3ae0f3a`, solo lectura. `git -C ../rowmate-legacy status --porcelain` vacío al empezar y al terminar. No se leyó `lib/core/strava/strava_secrets.dart` |
| Repo | Sin commits. El único archivo nuevo es este. Los archivos que escribió QA (scripts, logs, bases, fotos) quedaron en `<scratch>/it4a/qa-main-c1/` |
| Ronda 2 | Después de los arreglos del Architect: **APROBADO CON OBSERVACIONES**, con 0 bloqueantes, **0 mayores** (los dos de la ronda 1 quedaron corregidos) y 8 menores abiertos. AC: 156 PASS, 2 FAIL y 4 NO VERIFICABLE. Ver [Ronda 2](#ronda-2), al final |

---

## 0. Veredicto

**APROBADO CON OBSERVACIONES.** 0 bloqueantes, **2 mayores** y **10 menores** (§5).

**Lo que anda.** El motor, el guardado y el HUD hacen lo que pide la spec y lo que hace el legacy:

- Todas las suites están en verde (tabla de abajo).
- Las capturas son deterministas: dos corridas seguidas dan los mismos bytes que el repo.
- En el build de simulador, en la PC, funcionaron con la base real del simulador:
  - el entrenamiento libre, con pausa, reanudación y terminar con confirmación;
  - dos rutinas hasta el final;
  - el semáforo, corregido desde el panel;
  - el aviso sin remo;
  - la ventana minimizada 2 min;
  - el proceso terminado a la fuerza a mitad de sesión.
- Los íconos ▶ y ⏸ se ven bien: no salen como un cuadrado.

**Los dos mayores** son los de la lente de robustez, ahora reproducidos en Unity con una sonda temporal (§4):

- **QA-V4a-01 (= ROB-01).** Una sesión iniciada mientras sale el inmersivo anterior queda **sin HUD**. Hace fallar AC-WKT-01-09 y AC-WKT-13-01.
- **QA-V4a-02 (= ROB-02).** El latido de BPM y el pulso de la fila del paso **quedan en el `MotionDriver`** después de salir del inmersivo.

**Los menores nuevos:**

- **QA-V4a-03.** El divisor de la banda queda con ancho 0. Hace fallar AC-WKT-17-04.
- **QA-V4a-04.** El primer Tab después de usar el mouse no muestra el anillo de foco. Hace fallar AC-APP-05-07.
- **QA-V4a-05.** `TabularNumber` sigue animando elementos que ya no están en el panel: avisos de Unity en el registro del player.
- **QA-V4a-06.** No se integraron las fuentes de WP-B0.
- **QA-V4a-07.** La búsqueda de rutas del repo público da 1 coincidencia: un texto de prueba sintético.

Siguen abiertos los 5 menores de las lentes (QA-V4a-L01, -L02, -ROB-03, -04 y -05).

**Recomendación.** Corregir QA-V4a-01, -02 y -03 antes de cerrar la iteración:

- Son cambios chicos y localizados.
- Las PlayMode de la lente sirven de regresión: QA las corrió como sonda y hoy fallan.
- QA-V4a-03 pide regenerar `it4a` con la regla de dos corridas.

Los demás menores pueden ir a [STATUS](../../STATUS.md) como deuda para la 4b.

| Qué | Resultado |
|---|---|
| dotnet (`run-tests.ps1 -Matrix`) | `Build succeeded. 0 Warning(s) 0 Error(s)` · Simulator **173 / 173** · EditMode **2050: 2028 pasan, 0 fallan, 22 ignoradas** |
| Unity EditMode | **2088: 2062 pasan, 0 fallan, 26 ignoradas**, exit 0 |
| Unity EditMode + simulador | **2261: 2235 pasan, 0 fallan, 26 ignoradas**, 0 inconclusas; Enable → 0, Restore → 0 |
| Unity PlayMode | **261: 260 pasan, 0 fallan, 1 ignorada** (`HudLayoutTests.ScalesOfTheDesignNotes`, QA-V4a-06), exit 0 |
| Unity PlayMode + simulador | **325: 324 pasan, 0 fallan, 1 ignorada** (la misma), exit 0 |
| Matriz con los resultados de Unity (`-UnityResults`) | **PASA 534, FALLA 0, IGNORADO 1** (AC-WKT-02-10, Android), PENDIENTE UNITY 0, SIN TEST 0 (535 IDs de las It. 1 a 4a) |
| Arnés `it4a` (A y B) | 49 tomas, 0 con problemas, 0 sondas de aspecto que fallan. A = B = repo byte a byte; en `manifest.json` cambia solo `durationMs` |
| Arnés `it3` e `it2` | Iguales al repo byte a byte (48 + 43 archivos); en `manifest.json` cambia solo `durationMs` |
| Build de simulador (`Builds/Windows/simulator`) | `Development`, `Result: Success` |
| Build normal (`Builds/Windows/normal`) | `Result: Success` · `BuildOutputInspector: sin rastros del simulador, de diagnósticos ni de tests.` |
| AC de la spec (162) | **154 PASS · 4 FAIL · 4 NO VERIFICABLE** (§6) |
| Casos legacy LT-35 a LT-45 | 11 / 11 PASS (dotnet y Unity) |
| Repo público | 1 coincidencia de la búsqueda obligatoria, falsa alarma (QA-V4a-07) |

---

## 1. Corridas

Todo en serie: nunca hubo dos Editores de Unity a la vez. Al empezar no había ningún Editor abierto, solo el `unity.exe` de la Hub, que no se tocó. Las carpetas de resultados están en `<scratch>/it4a/qa-main-c1/`.

| # | Comando (desde la raíz del repo) | Resultado | Tiempo |
|---|---|---|---|
| 1 | `powershell -NoProfile -File tools/dotnet/run-tests.ps1 -Matrix -ArtifactsPath <scratch>/it4a/qa-main-c1` | Build 0/0. Simulator 173/173. EditMode 2050, 2028 pasan, 22 ignoradas (equipo, pantalla y GitHub). Matriz: PASA 425, FALLA 0, IGNORADO 1, PENDIENTE UNITY 109, SIN TEST 0 | 20 s |
| 2 | `tools/build/build.ps1 -Target Windows -Simulator -ArtifactsPath <scratch>/…/build-sim` | `Windows simulator Mono2x Development → Builds/Windows/simulator/RowMate.exe`, `Success` | 25 s |
| 3 | `tools/unity/run-unity-tests.ps1 -Platform EditMode -ArtifactsPath <scratch>/…/unity` | 2088 / 2062 / 0 / 26, exit 0 | 24 s |
| 4 | … `-Platform EditMode -Simulator` | 2261 / 2235 / 0 / 26, 0 inconclusas, exit 0 | 52 s |
| 5 | … `-Platform PlayMode` | 261 / 260 / 0 / 1, exit 0 | 2 min 41 s |
| 6 | … `-Platform PlayMode -Simulator` | 325 / 324 / 0 / 1, exit 0 | 3 min 29 s |
| 7 | `tools/unity/capture-snapshots.ps1 -Set it4a -Out <scratch>/…/snapA` | `[SNAPSHOT] listo: 49 tomas, 0 con problemas, 0 sondas de aspecto que fallan` | 58 s |
| 8 | lo mismo, `-Out <scratch>/…/snapB` | Igual | 57 s |
| 9 | `capture-snapshots.ps1 -Set it3 -Out <scratch>/…/snap-it3` | exit 0 | 55 s |
| 10 | `capture-snapshots.ps1 -Set it2 -Out <scratch>/…/snap-it2` | exit 0 | 54 s |
| 11 | `tools/build/build.ps1 -Target Windows -ArtifactsPath <scratch>/…/build-normal` | `Builds/Windows/normal`, `Success`, inspector sin rastros | 36 s |
| 12 | `tools/dotnet/run-tests.ps1 -UnityResults <scratch>/…/unity -ArtifactsPath <scratch>/…/dotnet-u` | Las mismas cifras de dotnet. Matriz: **PASA 534, FALLA 0, IGNORADO 1, PENDIENTE UNITY 0, SIN TEST 0** | 21 s |
| 13 | Sonda temporal (§4): `run-unity-tests.ps1 -Platform PlayMode -Filter QaProbe4a -ArtifactsPath <scratch>/…/probe` | 4 / 0 / **4 fallan**: es la evidencia. El archivo de la sonda y su `.meta` se borraron después | 1 min 40 s |

Las ignoradas:

- **dotnet (22)**: los `[Ignore]` de equipo (Android, iOS, Windows), de pantalla (Unity) y de GitHub. Los 5 de la 4a son `AndroidBackgroundTwoMinutes`, `AndroidBackOpensTheConfirmation`, `AndroidFirstFrameAfterBackground`, `AndroidMonotonicClockAcrossDeepSleep` y `AndroidScreenStaysOnAfterResume`.
- **Unity EditMode (26)**: las mismas 21 que quedan ignoradas en Unity, más las 5 de `SessionListToolTests`, que necesitan la herramienta compilada por dotnet y en dotnet pasan.
- **Unity PlayMode (1)**: `ScalesOfTheDesignNotes` (QA-V4a-06).

Después de las corridas:

- `ProjectSettings/ProjectSettings.asset` no tiene `ROWMATE_SIMULATOR` y no queda la marca `Library/RowMate/simulator-tests.txt`.
- No hay `Assets/Resources`.
- `UI/Fonts/Generated`, `UI/Icons/Generated` y `Settings/` no cambiaron.
- `git status --porcelain` da las mismas 340 entradas que al empezar, las del trabajo de la 4a. La carpeta de la iteración ya figuraba entera como nueva, así que este archivo no suma una entrada.

---

## 2. Capturas

### 2.1 Determinismo

| Comparación | Resultado |
|---|---|
| A contra B (`it4a`, dos corridas seguidas) | 50 de 51 archivos iguales byte a byte. `manifest.json` difiere solo en `durationMs` |
| A contra `docs/migration/iterations/04a-motor-hud-funcional/snapshots/` | Lo mismo: 46 PNG, 3 MP4 e `INDEX.md` iguales; en `manifest.json`, solo `durationMs` |
| `it3` nueva contra `03-rutinas/snapshots/` | 48 iguales; `manifest.json`, solo `durationMs` |
| `it2` nueva contra `02-dispositivo-simulador/snapshots/` | 43 iguales; `manifest.json`, solo `durationMs` |
| Las carpetas de capturas del repo, antes y después de todas las corridas | md5 iguales: QA escribió siempre en `<scratch>` |

La carpeta de la 4a tiene **49 tomas**: 32 PNG de CAP (CAP-01 a -30, con CAP-27 a, b y c), 14 DEMO y 3 VID, además de `manifest.json` e `INDEX.md`. En `INDEX.md` las 49 filas dicen `ok`.

### 2.2 Lo que se ve, toma por toma

Se miraron las 46 fotos y se compararon con la spec §7.3 y §7.4, con [04-design-notes](04-design-notes.md) §8.2 y §8.3 y con `docs/image-02.png` y `docs/image-03.png` del legacy. De los videos se miraron cuadros sueltos, extraídos con PyAV sin red en `<scratch>`. El análisis cuadro a cuadro es del Designer (04-design-notes §17.4).

| Toma | Lo esperado | Resultado |
|---|---|---|
| CAP-01 / CAP-04 | Barra `Entrenamiento` / `Workout`; `▶ Entrenamiento libre` a todo el ancho; `O elegí una rutina:`. Las 4 tarjetas en orden, con mini-barras de 8, 14, 1 y 7, resúmenes (`1 serie · 1 descanso`, `2 series · 1 descanso`, `1 paso`, `4 series · 3 descansos`; en inglés `1 interval · 1 rest`, `2 intervals · 1 rest`, `1 step`, `4 intervals · 3 rests`) y *chevron*. La llave abajo a la izquierda | OK |
| CAP-02 | Aviso ocre con su ícono; el botón al 38 % y las tarjetas al 50 % | OK |
| CAP-03 / CAP-06 / CAP-07 | `Creá una rutina en la pestaña "Rutinas"` centrado; solo el indicador; error y `↻ Reintentar` con el botón libre habilitado | OK |
| CAP-05 / CAP-11 | L640 con letra 1,3× y desplazada al final: la última tarjeta entera, sobre la barra y la llave. Letra 1,3× sin cortes | OK |
| CAP-08 | Velo; `Escalera SPM` · `1 serie · 1 descanso` · línea en blanco · `¿Iniciar el entrenamiento?`; `Cancelar` e `Iniciar`. La cinta y la llave sobre el velo | OK |
| CAP-09 | Sin diálogo; el aviso de `workoutRoutineCannotStart` en dos líneas, entero, sobre la llave | OK |
| CAP-10 / DEMO-01 | Contra `image-02.png`: las mismas posiciones, sin el escenario (DV-34) | OK |
| CAP-12 / CAP-13 / CAP-14 | `Entrenamiento Libre` con el ícono de remo; SPM, Split y Watts; banda `00:42 │ 158 m`; kcal (y BPM rosa en CAP-13); `Terminar` y `⏸ Pausa`, o `▶ Reanudar` verde en pausa con el reloj quieto. La llave sobre kcal | OK |
| CAP-15 / CAP-23 (TH) | Fila de abajo `banda │ Terminar Pausa │ llave │ BPM kcal`; la pastilla entera; nada se encima | OK |
| CAP-16 / CAP-24 (L640) | Valores largos con la zona del bote libre; SPM y Watts fuera, con `▲` y su `obj.`; la llave sobre `Pausa` | OK |
| CAP-17 / CAP-25 / DEMO-02 (W1280) | Contra `image-03.png`: las mismas zonas. kcal y BPM en fila (DV-33); la llave a la derecha de `Pausa` | OK |
| CAP-18 | `Calentamiento` amarillo, `0:24`, sin pastilla ni `Próximo` | OK, salvo que **la banda no tiene divisor** (QA-V4a-03) |
| CAP-19 / CAP-21 / CAP-22 | `Trabajo` rosa y la pastilla `>160W · 22 spm · ↗ SPM +2/rep` en blanco. Watts `▲ 150` rojo con `obj. 160`, borde y halo; SPM blanco con `obj. 22`. `Próximo: Descanso` entero; `0:03` grande y teñida | OK (la pastilla recortada con `…` en cian: 04-design-notes §17.6, aceptado) |
| CAP-20 | `500m`; pastilla recortada; Watts `▲` con `obj. 150`; Split `▼` con `obj. 02:05` y borde naranja | OK, salvo **la banda sin divisor** (QA-V4a-03) |
| CAP-26 / CAP-27b | Velo y diálogo opaco: `Terminar entrenamiento` / `Se guardará la sesión. ¿Continuar?`; en inglés `Finish workout` / `The session will be saved. Continue?` | OK, salvo **la banda sin divisor** debajo del velo (QA-V4a-03) |
| CAP-27a / CAP-27c | `Free Workout`, `SPM`, `Split /500m`, `Watts`, `kcal`, `Finish`, `Pause`; `Work`, `Up next: Rest` en blanco, `target 160` | OK; **CAP-27a sin divisor** (QA-V4a-03) |
| CAP-28 / DEMO-13 | Workout en reposo con `¡Entrenamiento completado!`, 8 dp sobre la llave | OK |
| CAP-29 | `No se pudo guardar la sesión` entero, entre la banda y los controles, con el reloj a la vista | OK |
| CAP-30 | `?` blanco; sin `Próximo` | OK, salvo **la banda sin divisor** (QA-V4a-03) |
| DEMO-03 | El panel crecido desde la llave, a la izquierda de kcal; Split y Watts a la vista; `160 W` | OK |
| DEMO-04 a DEMO-12, DEMO-14 | `Reanudar`; el diálogo; reposo sin aviso; el diálogo de `Prueba 4a`; `Calentamiento` con `Próximo: Trabajo`; Watts fuera y después cumpliendo; SPM fuera con `obj. 24`; `0:03` teñida sin `Próximo`; el aviso ocre con todo apagado | OK |
| VID-01 | `Próximo: Trabajo` entra a los 0,5 s; `0:05` crece; a los 15,5 s pasa a `Trabajo` con su pastilla, y SPM y Watts toman borde, halo y `obj.` | OK en los cuadros mirados |
| VID-02 | SPM de 24 a 21, rojo con `▲`; vuelve a blanco en 22 y 23 | OK en los cuadros mirados |
| VID-03 | `Reanudar` en el cuadro del toque; el diálogo entra y sale; el inmersivo se funde sobre Workout, con la llave otra vez a la izquierda | OK |

**El ▶ y el ⏸ son los glifos de verdad, no un cuadrado:**

- `▶ Entrenamiento libre` / `▶ Free workout`: CAP-01, -04, -10 y DEMO-01.
- `⏸ Pausa`: CAP-12 y todas las del HUD.
- `▶ Reanudar`: CAP-14 y DEMO-04.
- `▶ Free workout`, `⏸ Pause` y `▶ Resume` en el build, en la PC (§3).

El código es `RmButton.FilledOnly` (`Presentation/Controls/RmButton.cs:105-123`): elige la fuente Filled para los glifos que solo existen rellenos.

### 2.3 Las tomas de las It. 2 y 3 frente a `HEAD`

Las 88 tomas regeneradas (`it2` e `it3`) se compararon contra `HEAD` píxel por píxel, con un programa propio sobre `System.Drawing` en `<scratch>`. Los PNG de `HEAD` se sacaron con `git show … | git lfs smudge`.

- **Iguales o casi (máximo ±1 por canal).** En `it2`: las tarjetas del pulsómetro (CAP-10 a -15, AUX-01), CAP-20b, CAP-22 y CAP-23.
- **Cambia solo la zona de la llave**, que pasó de abajo a la derecha a abajo a la izquierda (D-ART-10). La caja de diferencias es la franja de abajo: `(42, 2085)-(1037, 2189)` en TV y `(16, 600)-(1263, 639)` en W1280. Pasa en casi todas las de `it2` con simulador y de `it3`. En it3 CAP-11, `HEAD` tenía la llave sobre la `✕` de `Enfriamiento` y ahora no tapa nada.
- **El panel del simulador** anclado abajo a la izquierda, de 280 dp: it2 CAP-16 a -19. Es la spec §5 y AC-BLE-16-03.
- **it2 CAP-21a.** Workout dejó de ser provisoria: ahora se ve Workout en reposo (AC-APP-02-08).
- **it3 CAP-18, CAP-19 y CAP-28.** El aviso 8 dp sobre la llave (spec §5).
- **it3 CAP-12b.** El editor, desplazado al final, termina 70 px más abajo: la llave ya no reserva lugar a la derecha. El contenido es el mismo.
- **it3 DEMO-04, -05, -06 y -08.** Cursor, foco y desplazamiento de QA-V3-03 y -04 (00-deuda-it3 §6). `INDEX.md` cambia solo en DEMO-05.

**Ningún cambio queda sin explicar.** Las fuentes nuevas de WP-B0 todavía no están (QA-V4a-06). Cuando entren, hay que regenerar una vez más `it2`, `it3` e `it4a`.

---

## 3. Prueba en equipo (Windows, build de simulador)

### 3.1 Cómo se hizo

- **El build.** El de simulador de la corrida 2, abierto **siempre desde su carpeta de salida por defecto** (`Builds/Windows/simulator`).
- **Los datos.** `ROWMATE_DATA_DIR` apuntaba a `<scratch>/it4a/qa-main-c1/devdata`. El registro dice `[APP] base rower_app_dev.db (carpeta de desarrollo)`. En esa carpeta, antes de abrir, se puso una base v8 con `F-4A` (`Prueba 4a`, `Corta`, `Distancia 100` y `Sin meta`), sembrada con `RoutineSeeds` en un archivo de nombre neutro y copiada como `rower_app_dev.db`.
- **El registro del player.** Fue a `<scratch>` (`-logFile`).
- **La ventana.** 1280 × 720, con `-screen-fullscreen 0`.
- **Cómo se manejó.** Con clics y teclas por Win32 desde PowerShell. Antes de cada clic se comprobaba que la ventana de QA estaba adelante y que el punto era suyo, y después se devolvía el puntero a donde estaba. Las teclas se mandaron con su *scan code*: el Input System de Unity no reconoce una tecla sin él (con 0 no llegaba ninguna).
- **Otra instancia del build.** Durante toda la prueba hubo otra instancia del mismo build de simulador, abierta desde el Explorador antes que las de QA y con la carpeta de datos por defecto. QA no la tocó ni la cerró.
- **La primera instancia de QA, descartada.** Abrió maximizada y registró una sesión `Corta` que QA no inició. Se cerró, esa base se guardó aparte en `<scratch>` y **se repitió todo** con una base nueva y la ventana en 1280 × 720.
- **Las preferencias de ventana de Unity del producto.** El player las guarda al cerrar; quedaron restauradas a los valores de antes (maximizada).
- **Lo que se ve.** El build sale en inglés, como Windows. Su simulador tiene ruido, así que los valores no son los exactos de la spec, que pide el simulador sin ruido del arnés.

### 3.2 Pasos

Las fotos y `device.log` están en `<scratch>/it4a/qa-main-c1/device/`.

| Paso | Qué se hizo | Resultado |
|---|---|---|
| **E1** | Abrir | Dispositivo, `Remo simulado` · `Connected` (01) |
| **E2** | Workout | `Workout`, `▶ Free workout` y `Or pick a routine:`. Las 4 tarjetas de `F-4A`, con `Prueba 4a` y su mini-barra de 6 tramos (amarillo, rosa, verde, rosa, verde, azul). La llave abajo a la izquierda (02) |
| **E3** | Entrenamiento libre | El inmersivo se abre en el acto: `Free Workout`, SPM, Split, Watts, banda `00:04` y kcal; `Finish` y `⏸ Pause` (03b). `Pause`: el botón pasa a `▶ Resume` verde y el reloj queda en `00:17` durante 3 s, mientras distancia, SPM, vatios y kcal siguen cambiando (04, 05). `Resume`: `00:19` a los 2,2 s (06b). `Finish` abre el diálogo y el reloj sigue (07b); `Cancel` y la sesión sigue (08). `Finish` y `Finish`: Workout en reposo, **sin aviso** (09c) |
| **E4** | `Corta` hasta el final | El diálogo `Corta` / `1 interval · 1 rest` / `Start the workout?` (10). En el paso: `Work` rosa, la pastilla `18 spm · ↗ SPM +2/rep`, `Up next: Rest` y `0:09`, con SPM blanco con borde y `target 18` (11). Por terminar: `0:04` agrandado y teñido (12). Después `Rest` verde con `Up next: Work` y `0:03` (13). En el último paso no hay `Up next` (14). A los 30 s la rutina terminó sola: Workout en reposo con `Workout Completed!` (14b, 15) |
| **E5** | `Prueba 4a` | `Warmup` con `Up next: Work` y `0:12` (18). Trabajo de la rep. 1: la pastilla `>160W · 22 spm · ↗ SPM +2/rep`, Watts `▲ 150` rojo con `target 160` y SPM `24.1` blanco con `target 22` (19). La llave abre el panel desde su esquina, con Split y Watts a la vista (20); `+` de Watts (160) y Watts queda blanco, `165` y `160` (21b, 21c). Rep. 2: `>160W · 24 spm`, `target 24` (23a); `−` de Ritmo (23) da SPM `▲ 22.4` rojo (23b) y `+` lo vuelve a blanco (23d). A los 235 s, `Finish` en el enfriamiento (24b, 24c); a los 240 s el diálogo **se cerró solo**, apareció `Workout Completed!` y quedó una sola sesión cerrada (24d) |
| **E6** | Sin remo | Llave, `Simular desconexión`: aviso `⚠ Connect the rower to start a workout`, todo apagado, y un toque en `Free workout` no hace nada (31b). A los 3 s todo vuelve (31c). Dispositivo → `Disconnect`: en Workout, aviso y tarjetas al 50 %, y un toque en `Prueba 4a` no abre nada (32c). `Search devices` → `Connected` (33a). La base no sumó sesiones |
| **E7** | Minimizar 2 min | Libre; minimizada a los 9,9 s y restaurada a los 130,6 s. El reloj dice `02:10` a los 130,9 s (40b). Leída minimizada, a los 129,9 s, la base tenía 26 puntos: 2 de antes de minimizar y **24 más** |
| **E8** | Matar el proceso | La misma sesión, terminada con `Stop-Process -Force` a los 158,0 s. La base: `finished_at` vacío, totales en 0, **31 puntos** (segundos 5 a 155) en el `-wal` y `integrity_check` ok. Al reabrir arranca en Dispositivo, sin ofrecer nada, y la sesión no cambió (42) |
| **E9** | Teclado | Escape sin diálogo: nada (50a). Escape con el diálogo: lo cancela (53b). Enter y Espacio después de un `Cancel` con el mouse: no reabren nada (53c, 53d). Tab: `Finish` → `Pause` (anillo blanco) → la llave (anillo cian) → `Finish` (52b a 52d). **El primer Tab después del mouse enfoca sin anillo**: no se ve nada, pero un Enter abre el diálogo de terminar (54a, 54b). En Workout en reposo pasa lo mismo (55a, 55b): QA-V4a-04 |
| **E10** | Cerrar | Cerrar la ventana: el proceso sale en 223 ms, sin `-wal` ni `-shm` (*checkpoint* hecho) |
| **E11** | Pantalla encendida | NO VERIFICABLE en equipo (§3.4) |

### 3.3 La base del simulador al terminar

Se leyó con `tools/sessions` (compilada en el paso 1, `--db <scratch>/…/rower_app_dev.db`), que trabaja sobre una copia: los md5 de la base y su `-wal` no cambiaron. También con una herramienta propia en `<scratch>`, que vuelca sesiones y puntos de una copia y recalcula los totales.

```
[sessions] rower_app_dev.db: 5 sesiones
  5 · 2026-10-28 21:39:40 · 2026-10-28 21:42:39 · libre · 36 puntos
  4 · 2026-10-28 21:36:19 · — · libre · 31 puntos
  3 · 2026-10-28 21:30:03 · 2026-10-28 21:34:03 · Prueba 4a · 49 puntos
  2 · 2026-10-28 21:28:42 · 2026-10-28 21:29:12 · Corta · 7 puntos
  1 · 2026-10-28 21:27:04 · 2026-10-28 21:28:02 · libre · 11 puntos
user_version 8 · integrity ok · puntos huérfanos 0 · rutinas 4
```

| Sesión | Puntos | Coherencia |
|---|---|---|
| 1, libre (E3) | 11 | 54,6 s en marcha (58,0 − 3,4 de pausa): 10 puntos más el final. El tiempo del monitor salta de 44 a 53 al reanudar porque el monitor siguió contando en pausa (AC-WKT-06-08). Totales guardados = recalculados: 323 m, 86 s, 151 W (P99), 24.2 spm (P99) y 19 kcal. `step_*` vacíos, `origin` = `app`, `strava_activity_id` vacío |
| 2, `Corta` (E4) | 7 | `step_index` 0, 0, 1, 2, 2, 3, 3 y tipos `work, work, rest, work, work, rest, rest` (AC-WKT-06-03). Los dos últimos iguales (AC-WKT-06-07). `routine_id` y `routine_name` de la plantilla; 30,03 s de inicio a fin |
| 3, `Prueba 4a` (E5) | 49 | Calentamiento 6, trabajo 12, descanso 6, trabajo 12, descanso 6 y enfriamiento 6 + 1 final. 240,01 s. Los dos últimos iguales. Totales = recalculados (1686 m, 444 s, 167 W, 24.9 spm, 102 kcal) |
| 4, libre (E7 y E8) | 31 | **Cortada**: `finished_at` vacío, totales en 0, sin el punto final (AC-WKT-06-10). Los tiempos del monitor van de 556 a 706, de 5 en 5 |
| 5, libre (E9) | 36 | 179 s: 35 puntos más el final. Totales = recalculados |

### 3.4 Pantalla siempre encendida

**NO VERIFICABLE en equipo** (AC-APP-14-01 a -03):

- `powercfg /requests` pide una consola de administrador, y QA corre sin elevación.
- El apagado de pantalla del plan de energía, en corriente, está en "nunca" (valor 0). No se puede ver que la pantalla no se apague, y QA no cambia la configuración de energía del usuario.
- El estado de ejecución del sistema (`CallNtPowerInformation`) marca pantalla y sistema pedidos antes y después de abrir el build. Es global y ya lo pedía otro proceso, entre ellos la instancia del usuario: no distingue.

Lo que sí hay:

- `WakeLockTests.WindowsRequestIsAcceptedAndReleased` y `ReleaseRunsOnTheRequestingThread` (Unity EditMode) llaman a `SetThreadExecutionState` de verdad: pedido `0x80000003` aceptado y liberado en el mismo hilo. Pasan.
- `ScreenKeepAwakeTests` (dotnet) pasan.
- Lo estático coincide con la lente de robustez: se pide en `Awake` y se libera en `OnDestroy` (`Bootstrap/AppRoot.cs`; `Platform/Power/WindowsWakeLock.cs`).

### 3.5 Firewall

No apareció ningún aviso nuevo. Las reglas de entrada de `rowmate.exe` son **12 antes y después**: 2 por cada una de las 6 carpetas de siempre (`Builds/Windows/simulator`, `dev-smoke` y `simulator-it3`, y tres de `<scratch>` de corridas viejas). Es el mismo número que midió 00-deuda-it3 §8.1. Ningún build de QA se abrió desde una carpeta nueva.

### 3.6 Las bases del usuario (AC-HIS-20-04)

- `rower_app.db` no existe.
- `rower_app_dev.db` **cambió** durante la verificación: el archivo, a las 21:48; su `-wal` siguió creciendo hasta después de que terminaran todas las corridas de QA. La escribió **la instancia que abrió el usuario**: su registro dice `[APP] base rower_app_dev.db`, la carpeta por defecto. Las de QA dicen `(carpeta de desarrollo)`.
- Las corridas de QA no usaron esa carpeta. `run-unity-tests.ps1` y `capture-snapshots.ps1` ponen `ROWMATE_DATA_DIR` en una carpeta temporal propia; las pruebas usan `TempDatabase`; la prueba en equipo usó `<scratch>`; la herramienta de sesiones solo lee una copia.
- Por eso AC-HIS-20-04 queda en **PASS**, aunque la comparación de antes y después no sirve en esta corrida.
- Observación sin issue: cada corrida de PlayMode deja `TestResults.xml` en la carpeta de datos de la app. Lo hace el Unity Test Framework; no es una base y ya pasaba en las It. 2 y 3.

---

## 4. Sondas en Unity (temporales)

Las PlayMode que propuso la lente de robustez (§4.1 y §4.2), adaptadas a `WorkoutUiTestBase`, más dos sobre el divisor de la banda. Iban en `Tests/PlayMode/QaProbe4aTests.cs`, se corrieron con `-Filter QaProbe4a` y **se borraron, con su `.meta`**. `git status` quedó igual que antes.

| Sonda | Qué hace | Resultado |
|---|---|---|
| `Rob01StartDuringThePreviousExitHasItsOwnImmersive` | `Corta` termina sola a los 30 s; con el inmersivo saliendo (`Pages.IsExiting` = true), `H.Tap("workout-free")` y `Settle` | Las tres primeras afirmaciones pasan: reposo, el inmersivo viejo saliendo y la fase `Active` después del toque. **Falla la última: `la sesión nueva tiene su HUD`, `H.Immersive` = null.** ROB-01 reproducido |
| `Rob02LeavingTheImmersiveStopsItsContinuousMotions` | `Prueba 4a` con pulso 140, a los 20 s (`Próximo` late); `Terminar` y confirmar | El inmersivo se cierra, pero queda `[QAPROBE] ROB-02: corazón en el MotionDriver=True, pulso de la fila=True`. ROB-02 reproducido |
| `BandDividerKeepsItsWidthAtSixSeconds` | `Prueba 4a` a los 6 s, en TV (el estado de CAP-18) | `divisor a los 6 s: layout.width=0 flexShrink=1` → falla (QA-V4a-03) |
| `BandDividerKeepsItsWidthAtFortyTwoSecondsFree` | Libre a los 42 s, en TV | `layout.width=0 flexShrink=1` → falla (QA-V4a-03) |

---

## 5. Issues

| ID | Sev. | Resp. | Issue | Evidencia | Corrección sugerida |
|---|---|---|---|---|---|
| QA-V4a-01 (= ROB-01) | **mayor** | architect | **Una sesión iniciada mientras sale el inmersivo anterior se queda sin HUD**, sin `Pausa` ni `Terminar`. La ventana es de 200 ms después del fin automático o de cerrar un diálogo (la descripción completa está en la lente). AC-WKT-13-01 y AC-WKT-01-09 fallan en ese caso | Sonda `Rob01…` en Unity (§4): `Phase = Active` y `H.Immersive = null`. `Presentation/Workout/WorkoutScreenView.cs:207`: `Immersive` devuelve la página que está saliendo, porque `PageStack.HasPage` la encuentra hasta `OnExited` | La de la lente: que `Immersive` no cuente la página que sale (`!_ui.Pages.IsExiting(_immersive)`), o volver a mirar la fase en el `OnPopped` del inmersivo viejo. Versionar la PlayMode de la lente §4.1 |
| QA-V4a-02 (= ROB-02) | **mayor** | architect | **Al salir del inmersivo quedan para siempre en el `MotionDriver` el latido de BPM y el pulso de la fila del paso.** Retienen el HUD de cada sesión y dejan la app a 60 fps en reposo hasta cerrarla | Sonda `Rob02…` (§4): `H.Handle.Motion.Contains(heart)` = true y `Contains(pulse)` = true. Código en la lente (`HudView.cs`, `HudCard.cs`, `StepPulse.cs`, `HeartbeatAnimator.cs`) | La de la lente: parar los dos al quitar el HUD del panel (`Heartbeat.Stop()`, un `StepPulse.Stop()` nuevo), y que `StepInfoBar.Apply(null)` apague el pulso. Versionar la PlayMode de la lente §4.2 |
| QA-V4a-03 | menor | architect | **El divisor de la banda no se dibuja como pide AC-WKT-17-04**: debería medir 1 × 32 dp al 12 %. El USS no le pone `flex-shrink: 0`, así que cuando la banda tiene apenas menos ancho que su contenido, el divisor (1 dp, el único hijo que se puede achicar) cede ese ancho. En W1280 queda entero (1,6 dp). En TV, TH y L640 queda entre 0 y 0,6 dp. Con 0, o casi, no se ve: CAP-18, -20, -26, -27a, -27b y -30 en TV, y CAP-16 y -24 en L640 (0,07 dp). En CAP-12 y similares se ve solo una línea de 1 px, por el suavizado | Sondas `BandDivider…` (§4): `layout.width=0`, `flexShrink=1`. `manifest.json`: el hueco entre `hud-time` y `hud-distance` en TV es 34,16 o 33,76 dp, y el esperado es 2 × 16 × 1,054 + 1,054 = 34,78. En W1280 da 33,60, el esperado. Escaneo de píxeles: en CAP-12 hay una columna `#263241` en x = 374 y 375; en CAP-18, -20, -27a y -30, ninguna. `Base.uss:2016-2032` (columna y fila con `flex-shrink: 0`, el divisor sin regla); `Presentation/Workout/HudBand.cs:68-74` | `.rm-hud-band__divider { flex-shrink: 0; }`, o `style.flexShrink = 0` en `HudBand`. Revisar que `HudZones` le dé a la banda el ancho de su contenido redondeado a píxeles (`TabularNumber.ApplySize` redondea hacia arriba). Sumar una afirmación de ancho a `ImmersiveHudTests.BandVitalsAndLiveValues`. Regenerar `it4a` (dos corridas) y que el Designer lo vea |
| QA-V4a-04 | menor | architect | **El primer Tab después de usar el mouse enfoca sin anillo** (AC-APP-05-07). Con DT4a-01, un clic no deja ningún control enfocado. Entonces el `KeyDown` y el `NavigationMoveEvent` del primer Tab van a la raíz del panel y no pasan por `#app-root`, donde `FocusHighlight` escucha para poner `.rm-keyboard-nav`. El foco llega a `Finish` (o a `Free workout` en reposo) sin anillo, y un Enter lo activa sin que se vea qué estaba enfocado. Desde el segundo Tab todo anda | Equipo E9: 54a, sin anillo después de un Tab; 54b, el Enter abre `Finish workout`; 55a y 55b en reposo; 52a a 52d. `Presentation/Controls/FocusHighlight.cs:16-20`; se crea sobre `#app-root` en `Presentation/Shell/AppShellView.cs:103` | Registrar `FocusHighlight` en `root.panel.visualTree`, o poner la clase en un `FocusInEvent` que no venga de un puntero. Sumar una PlayMode: "sin foco, un Tab enfoca el primer control **con** anillo" |
| QA-V4a-05 | menor | architect | **`TabularNumber.Tick` sigue animando el rodillo de dígitos sobre elementos que ya salieron del panel.** Unity anota `Trying to access the DPI setting of a visual element that is not on a panel.` desde `TabularNumber.ApplySize` → `scaledPixelsPerPoint`, con traza de pila en los builds de desarrollo. Pasa entre 48 y 64 veces cada vez que el inmersivo se cierra con cifras rodando. Es finito, pero ensucia el registro y hace trabajo de más por cuadro. Es del mismo tipo que QA-V4a-02 | Registros del player en la PC: 64 avisos en la primera instancia (un cierre) y 144 en la segunda (tres cierres). Todas las pilas: `TabularNumber:ApplySize (TabularNumber.cs:365)` ← `TabularNumber:Tick (:193)` ← `MotionDriver:Tick` ← `ShellHandle:Tick` ← `AppRoot:Update` | Terminar el rodillo (`FinishRoll`) en `DetachFromPanelEvent`, o no medir si `panel == null`. Que la PlayMode de QA-V4a-02 también afirme que no queda ningún `IMotion` del HUD en el `MotionDriver` |
| QA-V4a-06 | menor | architect | **WP-B0 no se integró: kerning K2 y RowMate Numeric SC** (DD4a-06, DD4a-07; 02-tech-design §6.14 y §7.4). Están las `.ttf` derivadas y las listas `.pairs.json` de WP-D0, pero `FontAssetPaths` sigue con Barlow; siguen los `BarlowSemiCondensed-*-SDF.asset`; no existen `ItFourAFontTests` ni `FontAndIconAssetsTests`; `HudLayoutTests.ScalesOfTheDesignNotes` está ignorada "hasta WP-D0". 04-design-notes §17.6 lo deja pendiente, y STATUS no lo dice. Ningún AC lo pide, pero el diseño técnico lo daba por hecho en la 4a | `grep RowMateNumeric\|Kerning` en `Core/Design/FontAssetPaths.cs` y `Editor/Bootstrap`: nada. `UI/Fonts/Generated/BarlowSemiCondensed-*-SDF.asset` presentes. El `[Ignore]` en `Tests/PlayMode/HudLayoutTests.cs:301` | Hacer B0 en el cierre o pasarlo explícito a la 4b en STATUS, con lo que arrastra: la regeneración única de `it2`, `it3` e `it4a`, el `[Ignore]` de `ScalesOfTheDesignNotes` y las dos comprobaciones flojas de CAP-15 y CAP-16 |
| QA-V4a-07 | menor | architect | **La búsqueda obligatoria del repo público da 1 coincidencia**: `Assets/RowMate/Tests/EditMode/Project/RepoHygieneTests.cs:203`, el primer `[TestCase]` de ese bloque. Es nuevo en la 4a. Es un caso de prueba sintético del detector de rutas: la carpeta de usuario de Windows de una persona inventada ("alguien"), en la unidad del sistema. No es una ruta de esta PC ni un nombre de usuario, pero la regla mecánica pide 0 | La búsqueda que pide el coordinador para el repo público (rutas de usuario de Windows y de Git Bash, y el prefijo de las carpetas de trabajo de Claude), sobre lo versionado y lo nuevo (`git grep --untracked -nIE`): esa sola línea | Armar el literal en dos partes, con la barra en la primera y "Users" en la segunda, concatenadas con `+`. Es el mismo caso y la búsqueda da 0 |
| QA-V4a-L01 | menor | po | Lente de paridad: un monitor que no informa la distancia cada segundo puede trabar un paso por distancia (Q-55). No se repitió | [05-qa-lens-paridad](05-qa-lens-paridad.md) §2 | La de la lente: sumarlo a P-04 y DV-26 y decidirlo antes de la 10a |
| QA-V4a-L02 | menor | architect | Lente de paridad: el oráculo del motor no tiene 12 caminos que la lente comprobó a mano. No se repitió | Lente de paridad §2 | La de la lente |
| QA-V4a-ROB-03 | menor | architect | Lente de robustez: al pasar a segundo plano no se mandan los puntos de un lote que falló | [05-qa-lens-robustez](05-qa-lens-robustez.md) §3 (L03) | La de la lente |
| QA-V4a-ROB-04 | menor | architect | Lente de robustez: terminar con un lote en vuelo e irse enseguida a segundo plano deja la sesión cortada | Lente de robustez §3 (L04, L08b) | La de la lente; si no, anotarlo con RA-08 |
| QA-V4a-ROB-05 | menor | architect | Lente de robustez: en primer plano, un cuadro muy tarde publica una foto por segundo procesado. Queda la pregunta al PO sobre la suspensión del equipo | Lente de robustez §3 (L05) | La de la lente |

---

## 6. Matriz AC → test → resultado

**Las fuentes:**

- La matriz de `run-tests.ps1 -UnityResults` (paso 12): los tests de dotnet y de las cuatro suites de Unity. Por AC se nombran los dos primeros.
- Las capturas de §2.
- La prueba en equipo (E1 a E11, §3).
- Las sondas de §4.

**La columna "Verif."** repite la marca de `It04aIds`: `d` dotnet, `u` Unity EditMode, `p` PlayMode, `ps` PlayMode con simulador, `cap` captura, `eq` equipo y `qa` revisión.

**Cómo se lee "Resultado".** PASS quiere decir que todos los tests del AC pasan y que las tomas y el equipo coinciden con la spec. En FAIL y NO VERIFICABLE se dice el motivo.

**Totales:** 162 AC = **154 PASS, 4 FAIL, 4 NO VERIFICABLE**; LT-35 a LT-45, 11 PASS.

| AC | Verif. | Resultado | Evidencia |
|---|---|---|---|
| AC-WKT-01-01 | d | PASS | `AppComposerTests.ComposesTheWorkoutEngineAndItsStores`, `WorkoutEngineLifecycleTests.FourPhasesStartingIdle` |
| AC-WKT-01-02 | d p | PASS | `WorkoutEngineLifecycleTests.ClockNeverWaitsMoreThan100Ms`, `WorkoutEngineLifecycleTests.StartFreeCreatesTheSessionThenTheClock`; equipo E3 (el inmersivo abre en el acto; `00:04` a los 4 s) |
| AC-WKT-01-03 | d | PASS | `WorkoutEngineLifecycleTests.RoutineStartExpandsAndPositions`, `WorkoutEngineLifecycleTests.SessionKeepsTheTemplateIdAndName` |
| AC-WKT-01-04 | d | PASS | `WorkoutEngineLifecycleTests.PauseFreezesClocksSamplingTargetsAndMeters`; equipo E3 (reloj quieto 3 s en pausa; valores en vivo) |
| AC-WKT-01-05 | d | PASS | `ActiveClockTests.ResumeRestartsTheRhythm`, `SanitizedDistanceTests.RebaseIgnoresWhatWasRowedInPause` |
| AC-WKT-01-06 | d p | PASS | `WorkoutEngineLifecycleTests.FinishSavesThenGoesIdle`, `WorkoutHudStoreTests.ControlsFollowThePhase`; equipo E3, E4, E5 (cierre y vuelta a reposo; base: sesiones cerradas) |
| AC-WKT-01-07 | d | PASS | `WorkoutEngineLifecycleTests.IdleKeepsTheLastReadingAndTheLastFinished` |
| AC-WKT-01-08 | d | PASS | `WorkoutEngineLifecycleTests.NoUndoAndStartOnlyFromIdle` |
| AC-WKT-01-09 | p | **FAIL** | `WorkoutScreenTests.TabShowsOnlyInProgressUnderTheImmersive`; QA-V4a-01 (ROB-01): sonda de QA en Unity, la sesión iniciada durante la salida queda sin inmersivo y la pestaña muestra `workoutInProgress` de forma permanente |
| AC-WKT-01-10 | d p | PASS | `WorkoutStoreTests.SecondStartIsIgnored`, `WorkoutScreenTests.OneTapOneSession` |
| AC-WKT-01-11 | d p cap | PASS | `SessionRecorderTests.CreateFailureDiscardsThePoints`, `WorkoutEngineLifecycleTests.CreateFailureStillRuns`; tomas CAP-29 |
| AC-WKT-02-01 | d | PASS | `ActiveClockTests.CountsWholeSecondsOfTheSegment`, `ActiveClockTests.OneSecondPerActiveSecondOnly` |
| AC-WKT-02-02 | d | PASS | `WorkoutEngineLifecycleTests.SecondOrderIsTheLegacyOrder` |
| AC-WKT-02-03 | d p | PASS | `WorkoutEngineLifecycleTests.HudClockIsTheActiveClock` |
| AC-WKT-02-04 | d | PASS | `FrameSchedulerTests.TickIsTheRunDueOfIterationTwo`, `PureCodeRulesTests.NoSystemClockInTheWorkoutEngine` |
| AC-WKT-02-05 | d | PASS | `FrameSchedulerTests.CatchUpFiresEverythingInOrderWithVirtualTime`, `WorkoutBackgroundTests.BackgroundIsLikeForeground` |
| AC-WKT-02-06 | d p | PASS | `ForegroundTimeoutTests.CreatedInTheBackgroundStartsWholeOnReturn`, `ForegroundTimeoutTests.RunsLikeAScheduleInTheForeground` |
| AC-WKT-02-07 | d | PASS | `WorkoutBackgroundTests.PausedSessionDoesNotMoveInBackground` |
| AC-WKT-02-08 | d p eq | PASS | `WorkoutBackgroundTests.EightHourCatchUpIsBounded`, `WorkoutDeviceChecksTests.AndroidFirstFrameAfterBackground`; parte [Device-And] NO VERIFICABLE (sin módulo ni teléfono) |
| AC-WKT-02-09 | d eq | PASS | `BootstrapperStaticTests.PlatformMinimumsComeFromAppIdentity`, `WorkoutStoreTests.HeldToastShowsOnFocus`; equipo E7: 120,7 s minimizada; al restaurar, `02:10` a los 130,9 s; la base pasó de 2 a 26 puntos (+24) |
| AC-WKT-02-10 | d eq | **NO VERIFICABLE** | `WorkoutDeviceChecksTests.AndroidBackgroundTwoMinutes`, `WorkoutDeviceChecksTests.AndroidMonotonicClockAcrossDeepSleep`; Android: sin módulo ni teléfono (`[Ignore]` de `WorkoutDeviceChecksTests`) |
| AC-WKT-03-01 | d | PASS | `StepAdvanceTests.Prueba4aExpandsToSixSteps` |
| AC-WKT-03-02 | d | PASS | `StepAdvanceTests.TimeStepEndsAtItsDuration`, `StepAdvanceTests.TimeWinsOverDistance` |
| AC-WKT-03-03 | d | PASS | `Workout.SanitizedDistanceTests.NegativeAndTooFastJumpsDoNotCountAndBecomeTheR...`, `Workout.StepAdvanceTests.DistanceStepWithTheSanitizer`; observación QA-V4a-L01 (lente): un monitor que no informa cada segundo puede trabar un paso por distancia |
| AC-WKT-03-04 | d | PASS | `StepAdvanceTests.StepChangeKeepsTheExtraMetersInTheOldStep` |
| AC-WKT-03-05 | d p cap | PASS | `StepAdvanceTests.FinishedRoutineStaysOnTheLastStep`, `WorkoutEngineLifecycleTests.LastStepFinishesAndSaves`; tomas CAP-28, DEMO-13; equipo E4 (`Corta`, aviso `Workout Completed!`), E5 |
| AC-WKT-03-06 | d | PASS | `StepAdvanceTests.FinishedRoutineStaysOnTheLastStep`, `StepProgressTests.FinishedRoutine` |
| AC-WKT-03-07 | d p | PASS | `StepAdvanceTests.NegativeDurationEndsAtTheFirstSecond`, `StepAdvanceTests.ZeroSecondStepEndsAtTheFirstSecond` |
| AC-WKT-03-08 | d p cap | PASS | `StepAdvanceTests.NoGoalStepNeverEnds`, `WorkoutFormatsTests.DistanceStepAndNoGoal`; tomas CAP-30 |
| AC-WKT-03-09 | d | PASS | `StepAdvanceTests.FreeHasNoStepsNorEnd` |
| AC-WKT-03-10 | d ps | PASS | `StepAdvanceTests.RowerDisconnectKeepsClockAndTimeSteps`, `WorkoutEngineLifecycleTests.EngineIgnoresTheConnection`; equipo E5 (`Simular desconexión` con la rutina en marcha: siguió) |
| AC-WKT-04-01 | d | PASS | `StepProgressTests.ExistsOnlyWithRoutineOutsideIdle` |
| AC-WKT-04-02 | d | PASS | `StepProgressTests.NegativeDurationShowsZero`, `StepProgressTests.Remaining` |
| AC-WKT-04-03 | d | PASS | `StepProgressTests.Progress` |
| AC-WKT-04-04 | d | PASS | `StepProgressTests.EndingSoonOneToFive` |
| AC-WKT-04-05 | d | PASS | `StepProgressTests.UpNextZeroToFifteen` |
| AC-WKT-04-06 | d p cap | PASS | `StepProgressTests.ExamplesOfTheSpec`, `ImmersiveHudTests.UpNextAndEndingSoonOnScreen`; tomas CAP-21, CAP-22; equipo E4 (paso de 10 s: `Up next` todo el paso; por terminar `0:04`) |
| AC-WKT-04-07 | d | PASS | `StepProgressTests.FinishedRoutine`, `StepProgressTests.FinishedRoutineByDistance` |
| AC-WKT-05-01 | d | PASS | `Workout.WorkoutProviderHrTests.el pulsómetro manda sobre el FTMS y se actuali...` |
| AC-WKT-05-02 | d | PASS | `Workout.WorkoutProviderHrTests.orden real de caída: bpm 0 y después disconnected` |
| AC-WKT-05-03 | d | PASS | `Workout.WorkoutProviderHrTests.un bpm recibido durante connecting se conserva...` |
| AC-WKT-05-04 | d | PASS | `Workout.WorkoutProviderHrTests.connected del sensor sin bpm previo no toca lo...` |
| AC-WKT-05-05 | d | PASS | `TelemetryTests.PointsCarryTheFusedPulse`, `WorkoutProviderHrTests.FusionInEveryPhaseFeedsHudAndPoints` |
| AC-WKT-05-06 | ps cap | PASS | `WorkoutSimulatorTests.SimulatedHeartRateOnTheHud`; tomas CAP-13 |
| AC-WKT-06-01 | d | PASS | `SqliteSessionRepositoryTests.CreateWritesTheLegacyRow`, `TelemetryTests.StartCreatesTheRow` |
| AC-WKT-06-02 | d | PASS | `TelemetryTests.PointEveryFiveActiveSeconds`, `TelemetryTests.TelemetryPointCopiesTheReading` |
| AC-WKT-06-03 | d | PASS | `TelemetryTests.PointBelongsToTheStepThatEnds`, `WorkoutGoldenTests.Corta`; equipo E4: 7 puntos, `step_index` 0, 0, 1, 2, 2, 3, 3 y tipos `work, work, rest, work, work, rest, rest` |
| AC-WKT-06-04 | d | PASS | `TelemetryTests.NoSamplingInPause` |
| AC-WKT-06-05 | d eq | PASS | `SqliteSessionRepositoryTests.KilledAt31_5sHasSixPoints`, `Infrastructure.SqliteSessionRepositoryTests.KilledAt31_5sHasSixPointsWithTheS...`; equipo E8: proceso terminado a la fuerza a los 158,0 s → 31 puntos (segundos 5 a 155) |
| AC-WKT-06-06 | d | PASS | `TelemetryTests.FinishWritesTheFinalPointAndTotals`; equipo E10: en las 4 sesiones cerradas, totales = los recalculados de sus puntos (máximos y P99) |
| AC-WKT-06-07 | d | PASS | `TelemetryTests.LastTwoPointsEqualWhenEndingOnAFive`, `WorkoutGoldenTests.Corta`; equipo E4, E5 (los dos últimos puntos iguales); observación QA-V4a-L02 (lente) |
| AC-WKT-06-08 | d | PASS | `TelemetryTests.MonitorCountersAreSaved` |
| AC-WKT-06-09 | d | PASS | `TelemetryTests.ShortAndUnrowedSessions` |
| AC-WKT-06-10 | d eq qa | PASS | `LocalStoreTests.DisposeFinishesTheQueuedWork`, `SqliteSessionRepositoryTests.CutSessionIsOpenWithItsPoints`; equipo E8: `finished_at` vacío, totales en 0; al reabrir, Dispositivo y la sesión sin cambios |
| AC-WKT-06-11 | d | PASS | `WorkoutBackgroundTests.LongBackgroundAfterTheEndStillSaves`, `WorkoutEngineLifecycleTests.LastFinishedOnlyWhenTheCloseSucceeds` |
| AC-WKT-06-12 | d p cap | PASS | `SqliteSessionRepositoryTests.CloseIsAtomic`, `Infrastructure.SqliteSessionRepositoryTests.CloseRetriesTheFailedBatchInOneTr...`; tomas CAP-29 |
| AC-WKT-06-13 | d p | PASS | `SqliteSessionRepositoryTests.BatchIsOneTransactionInOrder`, `SessionRecorderTests.SlowDatabaseKeepsOrderAndBatches` |
| AC-WKT-06-14 | d qa | PASS | `SqliteSessionRepositoryTests.NothingButSessionsAndPointsIsWritten`, `PureCodeRulesTests.InfrastructureDoesNotKnowTargetsOrSplits` |
| AC-WKT-06-15 | d | PASS | `SqliteSessionRepositoryTests.PointsAfterAFailedCreateWriteNothing`, `SqliteSessionRepositoryTests.PointsAfterCreateWaitForTheCreate`; observaciones QA-V4a-ROB-03 y -ROB-04 (lente): casos borde con un lote fallado o en vuelo |
| AC-WKT-09-01 | d | PASS | `Workout.StepTargetTrackerTests.cumplido cuando el promedio de vatios alcanza ...` |
| AC-WKT-09-02 | d | PASS | `Workout.StepTargetTrackerTests.no cumplido si falla cualquiera de los objetiv...` |
| AC-WKT-09-03 | d | PASS | `Workout.StepTargetTrackerTests.el split se cumple por debajo del objetivo ` |
| AC-WKT-09-04 | d | PASS | `Workout.StepTargetTrackerTests.un paso sin objetivos no tiene resultado` |
| AC-WKT-09-05 | d | PASS | `Workout.StepTargetTrackerTests.sin lecturas remando el objetivo no se cumple`, `...` |
| AC-WKT-09-06 | d | PASS | `Workout.StepTargetTrackerTests.un paso no completado no tiene resultado y res...` |
| AC-WKT-09-07 | d | PASS | `StepAdvanceTests.TargetsFromTheEngine`, `StepTargetTrackerTests.WhatCountsAsATargetAndAsRowing` |
| AC-WKT-10-01 | d p | PASS | `TargetLightTests.OnlyCardsWithTheirTarget` |
| AC-WKT-10-02 | d | PASS | `TargetLightTests.Spm` |
| AC-WKT-10-03 | d | PASS | `TargetLightTests.WattsIgnoresTheMax` |
| AC-WKT-10-04 | d | PASS | `TargetLightTests.SplitAndNonPositiveTargets` |
| AC-WKT-10-05 | p cap | PASS | `HudLayoutTests.CardsArrowAndTargetMatchTheDesignNotes`, `ImmersiveHudTests.TargetPillAndTrafficLight`; tomas CAP-19, CAP-20, CAP-24 |
| AC-WKT-10-06 | d cap | PASS | `TargetLightTests.EvaluatedWithEachReadingWithoutTolerance`, `WorkoutHudStoreTests.EvaluatedOnEveryReadingAlsoInPause`; tomas VID-02 |
| AC-WKT-10-07 | p | PASS | `ImmersiveHudTests.StoppedIsOff` |
| AC-WKT-10-08 | p | PASS | `ImmersiveHudTests.FreeTopBar` |
| AC-WKT-10-09 | ps cap | PASS | `WorkoutSimulatorTests.DemoTrafficLight`; tomas CAP-19, DEMO-09, DEMO-10, DEMO-11; equipo E5 (Watts `▲ 150` con `target 160` → `+` a 160 → blanco; rep. 2 `target 24`, `−` de Ritmo → `▲` rojo, `+` → blanco) |
| AC-WKT-11-01 | d | PASS | `Workout.WorkoutFormatsTests.LiveSplit` |
| AC-WKT-11-02 | d | PASS | `Workout.WorkoutFormatsTests.Spm` |
| AC-WKT-11-03 | d | PASS | `WorkoutFormatsTests.Watts` |
| AC-WKT-11-04 | d | PASS | `WorkoutFormatsTests.HudDistance` |
| AC-WKT-11-05 | d | PASS | `WorkoutFormatsTests.KcalAndPulse` |
| AC-WKT-11-06 | d | PASS | `Workout.WorkoutFormatsTests.SessionClock` |
| AC-WKT-11-07 | d | PASS | `Workout.WorkoutFormatsTests.Countdown` |
| AC-WKT-11-08 | d | PASS | `WorkoutFormatsTests.DistanceStepAndNoGoal` |
| AC-WKT-11-09 | d | PASS | `SeriesPanelsTests.formatos`, `Workout.WorkoutFormatsTests.LapTime` |
| AC-WKT-11-10 | d | PASS | `SeriesPanelsTests.formatos`, `WorkoutFormatsTests.RepSplit` |
| AC-WKT-11-11 | d | PASS | `SeriesPanelsTests.formatos`, `Workout.WorkoutFormatsTests.PanelMeters` |
| AC-WKT-11-12 | d | PASS | `Workout.WorkoutFormatsTests.WallClock` |
| AC-WKT-11-13 | d | PASS | `WorkoutFormatsTests.TargetLabelOfTheExpandedStep`, `WorkoutFormatsTests.TargetValueOfTheHud` |
| AC-WKT-11-14 | d | PASS | `WorkoutFormatsTests.RoutineSummary` |
| AC-WKT-11-15 | d | PASS | `WorkoutFormatsTests.FinalSummary` |
| AC-WKT-11-16 | d | PASS | `Workout.WorkoutFormatsTests.SameUnderAnyCulture` |
| AC-WKT-12-01 | p cap | PASS | `WorkoutScreenTests.WorkoutIsNotAPlaceholder`; tomas CAP-01 |
| AC-WKT-12-02 | p | PASS | `WorkoutScreenTests.ContentInOrder` |
| AC-WKT-12-03 | d p cap | PASS | `WorkoutStoreTests.FreeNeedsTheRower`, `WorkoutScreenTests.ContentInOrder`; tomas CAP-01 |
| AC-WKT-12-04 | p cap | PASS | `WorkoutScreenTests.RoutineListLikeRoutines`; tomas CAP-01, CAP-10 |
| AC-WKT-12-05 | d p | PASS | `AppComposerTests.ComposesTheWorkoutEngineAndItsStores`, `WorkoutStoreTests.UsesTheSharedCollection` |
| AC-WKT-12-06 | p cap | PASS | `WorkoutScreenTests.LoadingErrorAndEmpty`; tomas CAP-03, CAP-06, CAP-07 |
| AC-WKT-12-07 | d p cap | PASS | `WorkoutStoreTests.TapConfirmsWhenItCanStart`, `WorkoutScreenTests.StartDialog`; tomas CAP-08, DEMO-07; equipo E4, E5 (diálogo `Corta` / `Prueba 4a`) |
| AC-WKT-12-08 | d p cap | PASS | `RoutineExpansionTests.SaturateClampsAtTheIntLimits`, `RoutineExpansionTests.SaturateWithoutOverflowIsTheSame`; tomas CAP-09 |
| AC-WKT-12-09 | p cap | PASS | `WorkoutScreenTests.ScrollsAsOne`; tomas CAP-05 |
| AC-WKT-20-01 | p cap | PASS | `HudControlsTests.ControlsRow`; tomas CAP-12, CAP-14 |
| AC-WKT-20-02 | p | PASS | `HudControlsTests.PauseAndResumeAtOnce`; equipo E3 (el botón cambia en el acto) |
| AC-WKT-20-03 | p cap | PASS | `HudControlsTests.FinishConfirmation`; tomas CAP-26; equipo E3, E9; "Atrás" de Android NO VERIFICABLE |
| AC-WKT-20-04 | p | PASS | `HudControlsTests.ConfirmEndsWithoutSummary`; equipo E3 (reposo sin aviso) |
| AC-WKT-20-05 | p | PASS | `HudControlsTests.FinishFromPause` |
| AC-WKT-20-06 | p | PASS | `HudControlsTests.FinishShowsTheSpinnerAfter300Ms`, `HudControlsTests.NoDoubles` |
| AC-WKT-20-07 | p | PASS | `HudControlsTests.DialogClosesWhenTheRoutineEndsInBackground`, `HudControlsTests.FinishTapAfterTheEndDoesNothing`; equipo E5: diálogo abierto a los 238,6 s; a los 240 s se cerró solo, aviso `Workout Completed!` y una sola sesión cerrada (49 puntos) |
| AC-WKT-23-01 | d qa | PASS | `LegacyDiscardTests.NoClassicWorkoutScreenPort`; [QA] legacy: `_FullscreenWorkoutPage` solo se declara (`workout_screen.dart:205-212`) |
| AC-BLE-10-03 | d p cap | PASS | `WorkoutStoreTests.AllFourNoRowerStates`, `WorkoutScreenTests.NoRowerBannerInEveryState`; tomas CAP-02; equipo E6 |
| AC-BLE-10-04 | d p cap | PASS | `WorkoutStoreTests.FreeNeedsTheRower`, `WorkoutScreenTests.StartDisabledWithoutRower`; tomas CAP-02; equipo E6 (el toque en `Free workout` y en una tarjeta no hace nada; la base no cambió) |
| AC-BLE-10-05 | ps cap | PASS | `WorkoutSimulatorTests.NoRowerBannerFollowsTheRowerLive`; tomas DEMO-14; equipo E6 (`Simular desconexión` 3 s; `Disconnect` hasta `Search devices`) |
| AC-BLE-10-06 | d p | PASS | `WorkoutStoreTests.StartRoutineDoesNotCheckTheRower`, `WorkoutScreenTests.ConnectionCheckedOnlyOnTap` |
| AC-BLE-10-07 | d | PASS | `WorkoutEngineLifecycleTests.EngineIgnoresTheConnection` |
| AC-APP-14-01 | u eq | **NO VERIFICABLE** | `WakeLockTests.AndroidAndIosKeepTheScreenOnWithSleepTimeout`, `WakeLockTests.WindowsRequestIsAcceptedAndReleased`; [Device-Win]: `powercfg /requests` pide consola de administrador; el apagado de pantalla en corriente está en "nunca". El pedido real lo prueba `WakeLockTests.WindowsRequestIsAcceptedAndReleased` (Unity EditMode, pasa) |
| AC-APP-14-02 | d eq | **NO VERIFICABLE** | `ScreenKeepAwakeTests.DisposeUnsubscribes`, `ScreenKeepAwakeTests.RequestsAgainOnResumeAndFocus`; Windows: lo mismo que AC-APP-14-01; Android sin módulo ni teléfono |
| AC-APP-14-03 | u eq | **NO VERIFICABLE** | `WakeLockTests.OtherPlatformsRejectUntil10b`, `WakeLockTests.ReleaseRunsOnTheRequestingThread`; [Device-Win]: ídem; `WakeLockTests.ReleaseRunsOnTheRequestingThread` (pasa) |
| AC-APP-14-04 | d qa | PASS | `ScreenKeepAwakeTests.RejectedRequestOnlyLogsOnce`, `ScreenKeepAwakeTests.ThrowingWakeLockCountsAsARejection` |
| AC-WKT-13-01 | p | **FAIL** | `ImmersiveHudTests.OpensAtOnce`; QA-V4a-01 (ROB-01): un toque en `Entrenamiento libre` durante la salida del inmersivo anterior inicia la sesión pero no abre el inmersivo (sonda de QA en Unity) |
| AC-WKT-13-02 | p cap | PASS | `ImmersiveHudTests.Layers`; tomas CAP-12, CAP-18 |
| AC-WKT-13-03 | p eq | PASS | `WorkoutDeviceChecksTests.AndroidBackOpensTheConfirmation`, `HudControlsTests.BackOpensTheFinishDialogAndEscapeDoesNothing`; equipo E9 (Escape sin diálogo no hace nada; con diálogo, lo cancela); "Atrás" de Android NO VERIFICABLE |
| AC-WKT-13-04 | p | PASS | `HudControlsTests.ConfirmEndsWithoutSummary`, `HudControlsTests.ImmersiveIsStackedLikeTheEditor`; equipo E3 a E5; ver QA-V4a-01 (una sesión nueva sin HUD) |
| AC-WKT-13-05 | p | PASS | `ImmersiveHudTests.StepRowWithoutTheRoutineName` |
| AC-WKT-14-01 | p cap | PASS | `ImmersiveHudTests.StepRowWithoutTheRoutineName`; tomas CAP-18 |
| AC-WKT-14-02 | p cap | PASS | `ImmersiveHudTests.StepRowWithoutTheRoutineName` |
| AC-WKT-14-03 | p cap | PASS | `ImmersiveHudTests.TargetPillAndTrafficLight`; tomas CAP-19, CAP-23 |
| AC-WKT-14-04 | p cap | PASS | `ImmersiveHudTests.UpNextAndEndingSoonOnScreen`; tomas CAP-21, VID-01 |
| AC-WKT-14-05 | p cap | PASS | `ImmersiveHudTests.UpNextAndEndingSoonOnScreen`; tomas CAP-20, CAP-22 |
| AC-WKT-14-06 | p cap | PASS | `ImmersiveHudTests.PausedRowStaysWithItsPulses` |
| AC-WKT-16-01 | p cap | PASS | `ImmersiveHudTests.FreeTopBar`; tomas CAP-12 |
| AC-WKT-17-01 | d p cap | PASS | `HudGeometryTests.AnchorsAreTheLegacyZonesTimesTheScale`, `Design.HudGeometryTests.ScalesOfTheDesignNotes`; tomas CAP-12, CAP-15, CAP-16, CAP-17 |
| AC-WKT-17-02 | p | PASS | `ImmersiveHudTests.CardsAndColors` |
| AC-WKT-17-03 | p | PASS | `ImmersiveHudTests.CardsAndColors` |
| AC-WKT-17-04 | p | **FAIL** | `ImmersiveHudTests.BandVitalsAndLiveValues`; QA-V4a-03: el divisor de 1 dp queda con ancho 0 (`flex-shrink` 1) y no se dibuja en CAP-18, -20, -26, -27a, -30 (sonda de QA y escaneo de píxeles) |
| AC-WKT-17-05 | p cap | PASS | `ImmersiveHudTests.BandVitalsAndLiveValues`; tomas CAP-12, CAP-13, CAP-15 |
| AC-WKT-17-06 | p | PASS | `HudBudgetTests.NoAllocationsPerFrameWhileItOnlyBeats`, `ImmersiveHudTests.BandVitalsAndLiveValues` |
| AC-WKT-17-07 | p | PASS | `ImmersiveHudTests.RotationKeepsTheSession` |
| AC-WKT-17-08 | d p cap | PASS | `Design.HudGeometryTests.RectanglesOfTheDesignNotes`; tomas CAP-15, CAP-16, CAP-23, CAP-24 |
| AC-WKT-17-09 | d p cap | PASS | `HudGeometryTests.BoatZone`, `HudGeometryTests.ZonesDoNotOverlap`; tomas CAP-16 |
| AC-WKT-17-10 | p | PASS | `HudLayoutTests.ExtremeValuesFit`, `HudLayoutTests.ScalesOfTheDesignNotes` |
| AC-HIS-17-07 | d qa | PASS | `LocalStoreTests.OneDatabaseForBothRepositories`, `SqliteSessionRepositoryTests.NothingButSessionsAndPointsIsWritten`; [QA] base del simulador después de la prueba: v8, `integrity_check` ok, 5 sesiones con `origin` = `app`, 0 puntos huérfanos |
| AC-HIS-19-01 | d | PASS | `SqliteSessionRepositoryTests.GetByIdInLocalTime` |
| AC-HIS-19-02 | d | PASS | `SqliteSessionRepositoryTests.PointsOrderedByMonitorTime` |
| AC-SCN-01-01 | p cap | PASS | `ImmersiveHudTests.Layers`; tomas CAP-12 |
| AC-APP-03-09 | p cap | PASS | `HudControlsTests.ImmersiveIsStackedLikeTheEditor`; tomas VID-03 |
| AC-APP-03-10 | p cap | PASS | `HudControlsTests.FinishConfirmation`, `WorkoutScreenTests.StartDialog`; tomas CAP-08, CAP-26 |
| AC-APP-03-11 | p cap | PASS | `HudControlsTests.RoutineEndsWithTheFinishDialogOpen`, `WorkoutPersistenceTests.SaveErrorsShowOnceAndTheWorkoutGoesOn`; tomas CAP-09, CAP-28, CAP-29 |
| AC-APP-10-10 | d | PASS | `CatalogParityTests.SameKeysInBothLanguages`, `CatalogParityTests.TwelveItFourAKeys` |
| AC-BLE-16-01 | ps cap | PASS | `RoutinesSimulatorLayerTests.SimulatorLayerAboveEverything`, `SimulatorOverlayTests.RibbonIsAlwaysOnTop`; tomas CAP-12, CAP-26 |
| AC-BLE-16-02 | d s ps cap | PASS | `BottomObstaclesTests.PositionsOfTheDesignNotes`, `HudGeometryTests.WrenchSlot`; todas las tomas con simulador (`WrenchAudit` y `HudOverlap` en verde); equipo E2 a E6 |
| AC-BLE-16-03 | s ps cap | PASS | `SimulatorPanelLayoutTests.InTheWorkoutItGrowsUpAndLeftWithTheWattsCeiling`; tomas DEMO-03; equipo E5 (panel desde la llave; Split y Watts a la vista) |
| AC-APP-02-08 | p cap | PASS | `RoutinesScreenTests.RoutinesIsNotAPlaceholder`, `ShellTests.PlaceholdersShowTitleAndDimIconOnly`; tomas CAP-01 |
| AC-APP-06-01 | p cap | PASS | `MetricPaletteTests.AccentRestsAtSixtyPercent`, `Design.MetricPaletteTests.ContractHexes`; tomas CAP-12, CAP-13, CAP-19 |
| AC-APP-05-05 | p cap | PASS | `TextScaleStepsTests.ClassNames`, `TextScaleStepsTests.FourStepsInOrder`; tomas CAP-05, CAP-11 |
| AC-APP-05-07 | p ps eq | **FAIL** | `HudControlsTests.FocusReturnsOnlyWithTheKeyboard`, `HudControlsTests.TabOrder`; QA-V4a-04: el primer Tab después de usar el mouse enfoca sin anillo (equipo E9); el resto (orden, Enter/Espacio, Escape, foco tras `Cancelar`) pasa |
| AC-APP-05-08 | p | PASS | `DigitRollTests.ReducedMotionIsAnEightyMsFade`, `ImmersiveHudTests.ReducedMotion` |
| AC-RUT-06-08 | d p | PASS | `RoutineExpansionTests.Case7EmptyOrNonPositiveRepeats`, `RoutineExpansionTests.MatchesOracle` |
| AC-RUT-10-04 | ps cap | PASS | `RoutinesScreenTests.FabOpensANewEditor`, `RoutinesSimulatorLayerTests.FabTappableEverywhere`; tomas It. 3 CAP-01 |
| AC-RUT-10-09 | ps cap | PASS | `BottomObstaclesTests.PositionsOfTheDesignNotes`, `RoutinesScreenTests.ScrollPositionSurvivesEditorAndTabs`; tomas It. 3 CAP-06 |
| AC-RUT-10-11 | p cap | PASS | `RoutineStructureTests.FixtureSegments`, `RoutineStructureTests.ImpossibleRepeatsNeverExpand`; tomas CAP-01, CAP-02, DEMO-01; equipo E2 (`Prueba 4a`: 6 tramos) |
| AC-HIS-17-02 | d qa | PASS | `DatabaseSchemaTests.ColumnsTypesAndDefaults`, `DatabaseSchemaTests.CreationLeavesTheVersionInTheMainFile` |
| AC-HIS-20-04 | d qa | PASS | `ItThreeToolingTests.Cap27ProtectsTheUserDatabase`, `ItThreeToolingTests.Cap27RehearsalNeverTouchesTheUserFolder`; [QA] las corridas usaron carpetas temporales; ver §3.6 (la instancia del usuario sí escribió su base) |
| AC-APP-01-09 | p | PASS | `AppComposerTests.SlowLoadDoesNotBlockTheStart`, `AppComposerTests.StartLoadsRoutines` |
| LT-35 | d | PASS | `Workout.WorkoutProviderHrTests.el pulsómetro manda sobre el FTMS y se actuali...` |
| LT-36 | d | PASS | `Workout.WorkoutProviderHrTests.orden real de caída: bpm 0 y después disconnected` |
| LT-37 | d | PASS | `Workout.WorkoutProviderHrTests.un bpm recibido durante connecting se conserva...` |
| LT-38 | d | PASS | `Workout.WorkoutProviderHrTests.connected del sensor sin bpm previo no toca lo...` |
| LT-39 | d | PASS | `Workout.StepTargetTrackerTests.cumplido cuando el promedio de vatios alcanza ...` |
| LT-40 | d | PASS | `Workout.StepTargetTrackerTests.no cumplido si falla cualquiera de los objetiv...` |
| LT-41 | d | PASS | `Workout.StepTargetTrackerTests.el split se cumple por debajo del objetivo ` |
| LT-42 | d | PASS | `Workout.StepTargetTrackerTests.un paso sin objetivos no tiene resultado` |
| LT-43 | d | PASS | `Workout.StepTargetTrackerTests.sin lecturas remando el objetivo no se cumple`, `...` |
| LT-44 | d | PASS | `Workout.StepTargetTrackerTests.un paso no completado no tiene resultado y res...` |
| LT-45 | d | PASS | `SeriesPanelsTests.formatos` |

---

## 7. Deuda de la It. 3 que 00-deuda-it3 da por resuelta

| ID | Qué dice 00-deuda-it3 | Comprobado acá | Estado |
|---|---|---|---|
| QA-V3-02 | Lo que se va, las hojas, los colores del tipo y la línea de error, con su motion | `RoutineMotionDebtTests`: 14 / 14 en PlayMode y 14 / 14 con simulador. it3 VID-01, igual byte a byte al regenerado | Cerrado |
| QA-V3-03 | DEMO-05 con las progresiones a la vista | it3 DEMO-05, igual a la regeneración; `INDEX.md` dice "desplazado al final, Incremento SPM 2" | Cerrado |
| QA-V3-04 | `Type` deja el cursor al final y `Tap` mueve el foco como un clic | `TypeLeavesTheCaretAtTheEnd`, `TapMovesTheFocusLikeAClick` y `PointerDownFocusesAButton` pasan. it3 CAP-16 y las DEMO, iguales | Cerrado |
| QA-V3-05 | Los motivos de `[Ignore]` dicen dónde se verifica cada cosa | `ItThreeDebtTests.IgnoreReasonsNameWhereTheyAreVerified` pasa (dotnet) | Cerrado |
| QA-V3-06 | Capturas deterministas | `it4a` A = B = repo; `it3` e `it2` nuevas = repo, corridas en el orden `it4a` → `it4a` → `it3` → `it2`. `AtlasCharactersCoverEveryText` y `TabularNumberMeasuresEveryCharacterItDraws` pasan | Cerrado |
| QA-V3-07 | El comentario de `RmTextField` | `SingleLineCommentTellsTheLegacyRuleAndTheDifference` pasa | Cerrado |
| QA-V3-08 | Builds de desarrollo siempre en la misma carpeta, sin avisos nuevos del Firewall | `DevelopmentBuildsAlwaysUseTheSameFolder` y `DevelopmentBuildRefusesANewFolderForReal` pasan. El build de QA fue a `Builds/Windows/simulator` y se abrió desde ahí: 12 reglas antes y después, ningún aviso | Cerrado |
| QA-V3-10 | Ninguna tarea sin observar en Presentation | `PresentationNeverDiscardsATask` pasa | Cerrado |
| QA-V3-12 | El marco visible de la ventana; la foto de CAP-27 nunca va directo al repo | `WindowCaptureUsesTheVisibleFrame` y `Cap27PhotoNeverGoesStraightToTheRepo` pasan; `capture-cap27.ps1` no se corrió (es solo a pedido) | Cerrado |
| Hallazgo `TabularNumber` | El `×N` medía 0,5 em sin glifo | `TabularNumberMeasuresEveryCharacterItDraws` pasa; it3 CAP-10, igual | Cerrado |
| §11.1, el foco con el clic | Para el diseño de la 4a (DT4a-01) | Aplicado: en equipo, después de un clic no queda foco y Enter no reactiva nada (E9). Efecto lateral: el primer Tab sin anillo (QA-V4a-04) | Cerrado, con QA-V4a-04 |

---

## 8. Revisión estática

**Hilos.**

- En el código nuevo de la 4a no hay `async void`, `.Wait()`, `Thread.Sleep`, `Task.Run` ni `new Thread`. Se buscó en `Core/Workout`, `App/Workout`, `Infrastructure/Sessions`, `Infrastructure/Database/LocalStore.cs`, `Presentation/Workout`, `Platform/Lifecycle`, `Platform/Power`, `Core/Common/FrameScheduler.cs`, `ForegroundTimeout.cs` y `Bootstrap/AppRoot.cs`.
- El único `.Result` (`Infrastructure/Sessions/SqliteSessionRepository.cs:126`) se lee en el hilo de la base, después de comprobar `IsCompleted`, y no bloquea.
- Lo demás (lotes en serie, cierre en una transacción, observación de las tareas) lo revisó la lente de robustez §2, y esta corrida no lo contradice: en equipo, 5 sesiones con sus puntos en orden, sin huérfanos.

**Asignaciones por cuadro del HUD.**

- `HudBudgetTests.NoAllocationsPerFrameWhileItOnlyBeats` (0 B por cuadro mientras solo late) y `AtMostSixtyVisibleElementsInTheWorstCase` pasan en PlayMode y en PlayMode con simulador.
- Los `Tick` de `HeartbeatAnimator`, `StepPulse`, `OffTargetMotion`, `MotionDriver` y `TabularNumber` no asignan: `new Scale(new Vector3(…))` son *structs*.
- El trabajo de más por cuadro que sí hay es el de las animaciones que no paran al salir (QA-V4a-02 y QA-V4a-05).

**Simulador fuera del release.**

- `Builds/Windows/normal/RowMate_Data/Managed` no tiene `RowMate.Simulator*.dll`, y ningún ensamblado nombra `SimulatedBleService` ni `RowingSimulator`.
- `BuildOutputInspector` está en verde.
- El de simulador sí los trae, con sus `.pdb`.
- `ProjectSettings.asset` quedó sin el define después de todas las corridas.

**Repo público.**

- La búsqueda obligatoria del coordinador (`git grep --untracked -nIE` con su patrón de rutas de usuario de Windows y de Git Bash y del prefijo de las carpetas de trabajo), sobre lo versionado y lo nuevo, da **1 coincidencia**: el caso sintético de QA-V4a-07. Este informe no agrega ninguna. Por eso no copia ni el patrón ni el literal: se volvió a correr al terminar.
- En `tools/oracle/engine` solo son versionables `pubspec.yaml`, que toma el legacy por ruta relativa (`../../../../rowmate-legacy`), y `test/engine_vectors_test.dart`, sin rutas de la PC. `.dart_tool/`, `build/` y `pubspec.lock` están ignorados por git.
- Este informe cita el repo con rutas relativas, el legacy por su URL y el scratch como `<scratch>`. No nombra la carpeta del usuario ni datos del equipo.

---

## 9. Observaciones sin issue

- **O-01 · Durante el fundido de salida (200 ms), el HUD muestra la foto de reposo.** Al confirmar `Finish`, en el cuadro de 120 ms la banda ya dice `00:00` (equipo, foto 09a). Es el orden de la spec (AC-WKT-01-06 y -07: reposo y después el cierre). Si el Designer prefiere que el HUD salga con su última foto, es una decisión de la 4b.
- **O-02 · Con el simulador del build (con ruido), 160 W contra un objetivo de 160 alterna entre rojo y blanco.** Es lo esperado: la regla es `<` sin margen (AC-WKT-10-03 y -06). La demo de la spec usa el simulador sin ruido del arnés.
- **O-03 · El panel del simulador está en español con la app en inglés.** Es la herramienta de desarrollo, como en la It. 2.
- **O-04 · Con el aviso sin remo (W1280), la llave pisa unos píxeles de la esquina de la última tarjeta.** Pasa en el equipo y en DEMO-14. Las tarjetas están apagadas, y la regla de AC-BLE-16-02 habla de los *chevrons* (a la derecha) y de los botones, no del cuerpo de la tarjeta. No tapa ninguna acción.
- **O-05 · La pastilla recortada sale con el "…" en cian y sin la flecha** (CAP-21, CAP-27c). Ya está aceptado en 04-design-notes §17.6.

---

## 10. Verificación hecha

1. **Legacy, solo lectura.** `git -C ../rowmate-legacy status --porcelain` vacío al empezar y al terminar (`3ae0f3a`). Se leyó `lib/features/workout/workout_screen.dart` (para AC-WKT-23-01) y `docs/image-02.png` y `image-03.png`. No se leyó `lib/core/strava/strava_secrets.dart`.
2. **Unity.** Una sola instancia por vez, todas lanzadas por QA y cerradas solas. No se tocó ningún proceso de Unity ajeno.
3. **Los builds.** Se abrieron solo desde `Builds/Windows/simulator`. Los tres `RowMate.exe` de QA se cerraron: dos con la ventana y el de E8 a la fuerza, a propósito. La instancia que abrió el usuario siguió abierta y QA no la tocó.
4. **El repo.** Sin commits. Se sumó solo este archivo. La sonda temporal y su `.meta` se borraron, y `git status --porcelain` volvió a las 340 entradas del principio. Las carpetas de capturas del repo no cambiaron (md5).
5. **Las bases.** Las de prueba y la del simulador, en carpetas temporales o en `<scratch>`. La del usuario, ver §3.6.
6. **El equipo.** Las preferencias de ventana del producto se restauraron a como estaban. No se cambió la configuración de energía.
7. **El repo público.** El resultado está en §8 y en QA-V4a-07.

---

## Ronda 2

| Campo | Valor |
|---|---|
| Fecha | 2026-10-28 |
| Qué se verifica | Los arreglos de [02-tech-design, "Cambios por verificación (ronda 1)"](02-tech-design.md#cambios-por-verificación-ronda-1). Uno por uno: QA-V4a-01, -02, -ROB-01 y -ROB-02, y además lo que el Architect da por corregido (QA-V4a-07, -ROB-03, -ROB-04, -ROB-05, la parte de QA-V4a-L02 y el hallazgo del indicador de carga). Se repitió todo lo de la ronda 1: dotnet, las cuatro suites de Unity, el arnés (`it4a` dos veces, `it3` e `it2`), los dos builds de Windows, la prueba en equipo, cada AC, la deuda de la It. 3 y la revisión estática |
| Legacy | [rowmate-legacy](https://github.com/figuibej/rowmate-legacy) en `3ae0f3a`, solo lectura. `git -C ../rowmate-legacy status --porcelain` vacío al empezar y al terminar. Se miró `docs/image-02.png`, `docs/image-03.png` y el listado de `lib/features/routines/` (una búsqueda de `PopScope`). No se leyó `lib/core/strava/strava_secrets.dart` |
| Repo | Sin commits. Solo cambió este archivo: esta sección y una fila del encabezado. Los scripts, registros, bases y fotos de QA quedaron en `<scratch>/it4a/qa-main-c2/` |

### R2.0 Veredicto

**APROBADO CON OBSERVACIONES.** 0 bloqueantes, **0 mayores** y 8 menores abiertos (R2.6).

**Los dos mayores de la ronda 1 están corregidos.** Se verificaron de cuatro maneras: el código, las pruebas nuevas del Architect, sondas propias de QA en Unity contra el código arreglado y la prueba en equipo.

- **QA-V4a-01 (= ROB-01).** Una sesión iniciada mientras sale el inmersivo anterior ahora tiene su HUD en el mismo toque. Se probó con el fin automático y con Terminar a mano, con el toque a los 150 ms. AC-WKT-01-09 y AC-WKT-13-01 pasan de FAIL a PASS.
- **QA-V4a-02 (= ROB-02).** Al salir del inmersivo, el latido y el pulso de la fila salen del `MotionDriver`. Quedan 0 animaciones y 0 demandas de 60 fps, también con tres sesiones seguidas. En la PC, la CPU en reposo después de salir con `Up next` latiendo no sube.

**También están corregidos** QA-V4a-07, ROB-03, ROB-04 y ROB-05, y el indicador de carga que pedía 60 fps. De QA-V4a-L02 se hizo la parte que no necesita Flutter.

**Siguen abiertos**, como declaró el Architect, QA-V4a-03, -04, -05 y -06. Esta ronda los volvió a ver:

- **QA-V4a-03, el divisor de la banda.** Sigue con ancho 0 y por eso AC-WKT-17-04 sigue en FAIL.
- **QA-V4a-04, el primer Tab sin anillo.** Se reprodujo otra vez en la PC y por eso AC-APP-05-07 sigue en FAIL. Ahora se vio también en el editor de Rutinas: ahí el foco invisible cae en `←`, y un Enter sale del editor sin guardar, sin confirmación, como en el legacy.
- **QA-V4a-05, los avisos de DPI.** Salieron 88 en el registro del player.
- **QA-V4a-06, WP-B0.** Sigue sin integrarse.

Siguen abiertos también QA-V4a-L01 (del PO) y la pregunta de ROB-05 sobre la suspensión del equipo.

**Hay un menor nuevo, operativo: QA-V4a-08.**

- La carpeta por defecto del build de simulador, `Builds/Windows/simulator`, tiene todavía el build de la ronda 1, el de **antes** de los arreglos.
- No se recompiló porque el usuario tenía abierta una instancia de ese build (R2.4.1). Pisarla habría tocado archivos en uso, y la compilación anterior reescribió uno que hoy está bloqueado.
- La prueba en equipo se hizo con un build del código actual en `Builds/Windows/simulator-it3`. Es una carpeta de builds de simulador que ya tenía su regla del Firewall: las reglas fueron 12 antes y 12 después, y no hubo ningún aviso.

**AC de la spec (162): 156 PASS · 2 FAIL · 4 NO VERIFICABLE** (R2.7). Respecto de la ronda 1, pasan de FAIL a PASS AC-WKT-01-09 y AC-WKT-13-01.

**Recomendación.**

- **QA-V4a-03.** Corregirlo antes de cerrar la 4a. Es `flex-shrink: 0` en una regla y regenerar `it4a` con dos corridas.
- **QA-V4a-04.** Si no entra en el cierre, que sea lo primero de la 4b. Su alcance ahora incluye salir del editor sin guardar.
- **QA-V4a-08.** Recompilar `Builds/Windows/simulator` cuando el usuario cierre su RowMate.
- **Lo demás.** A [STATUS](../../STATUS.md) como deuda.

| Qué | Resultado |
|---|---|
| dotnet (`run-tests.ps1 -Matrix`) | `Build succeeded. 0 Warning(s) 0 Error(s)` · Simulator **173 / 173** · EditMode **2055: 2033 pasan, 0 fallan, 22 ignoradas** (ronda 1: 2050; +5 de los arreglos) |
| Unity EditMode | **2093: 2067 pasan, 0 fallan, 26 ignoradas**, exit 0 (+5) |
| Unity EditMode + simulador | **2266: 2240 pasan, 0 fallan, 26 ignoradas**, 0 inconclusas; Enable → 0, Restore → 0 (+5) |
| Unity PlayMode | **264: 263 pasan, 0 fallan, 1 ignorada** (`HudLayoutTests.ScalesOfTheDesignNotes`, QA-V4a-06), exit 0 (+3) |
| Unity PlayMode + simulador | **328: 327 pasan, 0 fallan, 1 ignorada** (la misma), exit 0 (+3) |
| Matriz con los resultados de Unity (`-UnityResults`) | **PASA 534, FALLA 0, IGNORADO 1** (AC-WKT-02-10, Android), PENDIENTE UNITY 0, SIN TEST 0 (535 IDs) |
| Sondas temporales de QA en Unity (R2.5) | 6: **5 pasan** (ROB-01 ×2, ROB-02 ×2, DPI) y **1 falla, la esperada** (el divisor, QA-V4a-03 abierto) |
| Arnés `it4a` (A y B) | 49 tomas, 0 con problemas, 0 sondas de aspecto que fallan. A = B = repo byte a byte; en `manifest.json` cambia solo `durationMs` |
| Arnés `it3` (47 tomas) e `it2` (41) | Iguales al repo byte a byte; en `manifest.json` cambia solo `durationMs` |
| Build de simulador | `Development`, `Result: Success`, en `Builds/Windows/simulator-it3` (R2.4.1) |
| Build normal (`Builds/Windows/normal`) | `Result: Success` · `BuildOutputInspector: sin rastros del simulador, de diagnósticos ni de tests.` |
| Prueba en equipo | E1 a E10 como pide la spec, salvo QA-V4a-04 (E9). E11, la pantalla encendida: NO VERIFICABLE (R2.4) |
| Repo público | **0 coincidencias** de la búsqueda obligatoria (lo versionado y lo nuevo, también sin distinguir mayúsculas) |

### R2.1 Corridas

Todo en serie, con una sola instancia de Unity por vez. Al empezar no había ningún Editor abierto; solo estaba el `unity.exe` de la Hub, que no se tocó. Los resultados están en `<scratch>/it4a/qa-main-c2/`.

| # | Comando (desde la raíz del repo) | Resultado | Tiempo |
|---|---|---|---|
| 1 | `powershell -NoProfile -File tools/dotnet/run-tests.ps1 -Matrix -ArtifactsPath <scratch>/it4a/qa-main-c2` | Build 0/0. Simulator 173/173. EditMode 2055, 2033 pasan, 22 ignoradas. Matriz: PASA 425, FALLA 0, IGNORADO 1, PENDIENTE UNITY 109, SIN TEST 0 | 19 s |
| 2 | `tools/unity/run-unity-tests.ps1 -Platform EditMode -ArtifactsPath <scratch>/…/unity` | 2093 / 2067 / 0 / 26, exit 0 | 37 s |
| 3 | … `-Platform EditMode -Simulator` | 2266 / 2240 / 0 / 26, 0 inconclusas, exit 0 | 53 s |
| 4 | … `-Platform PlayMode` | 264 / 263 / 0 / 1, exit 0 | 2 min 45 s |
| 5 | … `-Platform PlayMode -Simulator` | 328 / 327 / 0 / 1, exit 0 | 3 min 31 s |
| 6 | `tools/unity/capture-snapshots.ps1 -Set it4a -Out <scratch>/…/snapA` | `[SNAPSHOT] listo: 49 tomas, 0 con problemas, 0 sondas de aspecto que fallan` | 57 s |
| 7 | lo mismo, `-Out <scratch>/…/snapB` | Igual | 55 s |
| 8 | `capture-snapshots.ps1 -Set it3 -Out <scratch>/…/snap-it3` | 47 tomas, 0 con problemas, exit 0 | 54 s |
| 9 | `capture-snapshots.ps1 -Set it2 -Out <scratch>/…/snap-it2` | 41 tomas, 0 con problemas, exit 0 | 52 s |
| 10 | Sonda temporal (R2.5): `run-unity-tests.ps1 -Platform PlayMode -Simulator -Filter QaProbe4aR2 -ArtifactsPath <scratch>/…/probe` | 6 / 5 / **1 falla** (la esperada), exit 2. El archivo de la sonda y su `.meta` se borraron después | 1 min 40 s |
| 11 | `tools/dotnet/run-tests.ps1 -UnityResults <scratch>/…/unity -ArtifactsPath <scratch>/…/dotnet-u` | Las mismas cifras de dotnet. Matriz: **PASA 534, FALLA 0, IGNORADO 1, PENDIENTE UNITY 0, SIN TEST 0** | 19 s |
| 12 | `tools/build/build.ps1 -Target Windows -Simulator -Out Builds/Windows/simulator-it3 -AllowNewFolder -ArtifactsPath <scratch>/…/build-sim` | `Windows simulator Mono2x Development`, `Success` | 35 s |
| 13 | `tools/build/build.ps1 -Target Windows -ArtifactsPath <scratch>/…/build-normal` | `Builds/Windows/normal`, `Success`, inspector sin rastros | 25 s |

**Una corrida descartada.** Una primera vuelta del paso 11 se corrió mientras el arnés tenía puesto su define temporal. Por eso `SimulatorInUnityTests.CommittedSettingsDoNotEnableTheSimulator` salió inconclusa, con el mensaje "quedó cortada" y 2032 que pasan. Es la guardia funcionando bien. Se repitió con Unity cerrado: es la del paso 11.

**Las ignoradas** son las mismas de la ronda 1:

- **dotnet (22):** los `[Ignore]` de equipo, de pantalla y de GitHub.
- **Unity EditMode (26):** las 21 de Unity más las 5 de `SessionListToolTests`.
- **Unity PlayMode (1):** `ScalesOfTheDesignNotes`, por QA-V4a-06.

**Después de todas las corridas:**

- `ProjectSettings/ProjectSettings.asset` no tiene `ROWMATE_SIMULATOR`, no queda `Library/RowMate/simulator-tests.txt` y no hay `Assets/Resources`.
- `UI/Fonts/Generated`, `UI/Icons/Generated` y `Settings/` tienen los mismos md5 que al empezar.
- Las tres carpetas de capturas del repo tienen los mismos md5 que al empezar.
- `git status --porcelain` da las mismas 340 entradas del principio. Este archivo ya figuraba entre ellas.

### R2.2 Los arreglos, uno por uno

| ID | Qué se verificó | Evidencia | Estado |
|---|---|---|---|
| **QA-V4a-01 = QA-V4a-ROB-01** (mayor) | Que una sesión iniciada durante el fundido de salida del inmersivo anterior tenga su propio inmersivo en el mismo toque, con Pausa, Terminar y "Atrás", y que la página vieja no le toque nada al irse | **Código.** `Presentation/Workout/WorkoutScreenView.cs:213`: `Immersive` ya no cuenta la página que sale (`!_ui.Pages.IsExiting(_immersive)`), y `OpenImmersive` (`:416-431`) apila una nueva. La página vieja, al irse (`ImmersiveWorkoutPage.OnPopped`, `:89-105`), solo retira lo suyo: `ShellLayout.SetSurface` y `SetBottomObstacle` van por dueño (`Presentation/Shell/ShellLayout.cs:101-141`, `:239-278`), y los callbacks que desengancha son de su propia instancia. **Pruebas.** `WorkoutScreenTests.StartDuringThePreviousExitHasItsOwnImmersive` pasa en PlayMode y en PlayMode con simulador, con y sin reducir movimiento; AC-WKT-01-09 y AC-WKT-13-01 la cuentan. **Sondas de QA** (R2.5): con el fin automático de `Corta`, el toque en libre durante la salida da HUD nuevo, la vieja no deja superficie, la segunda sesión se cierra y quedan 2 sesiones; con Terminar a mano, el toque a los 150 ms del confirmar (con el viejo todavía saliendo) da HUD nuevo, y Pausa y Terminar responden | **Cerrado** |
| **QA-V4a-02 = QA-V4a-ROB-02** (mayor) | Que al salir del inmersivo no quede ninguna animación del HUD en el `MotionDriver` ni ninguna demanda de 60 fps | **Código.** `HudView.Dispose` (`Presentation/Workout/HudView.cs:257-281`) para `Bpm.Heartbeat` y `Steps.Pulse`. `StepPulse.Stop` (`Presentation/Motion/StepPulse.cs:168-183`) sale del `MotionDriver` en el acto y deja la cuenta en reposo. `StepInfoBar.Apply(null)` (`:159-165`) suelta el pulso con `Release`. `MotionDriver.Remove` y `Compact` liberan la demanda una sola vez (`MotionDriver.cs:142-157`, `:193-214`). **Pruebas.** `ImmersiveHudTests.LeavingTheImmersiveStopsItsContinuousMotions` pasa, con tres salidas: Terminar con `Próximo`, Terminar en "por terminar" y la vuelta del segundo plano. **Sondas de QA.** Con `Prueba 4a` y pulso 140, a los 20 s, el corazón y el pulso salen del `MotionDriver`; 5 s después hay 0 animaciones y 0 demandas. Tres sesiones libres seguidas con pulso no acumulan nada. **Equipo (E5).** La CPU en reposo después de terminar con `Up next` latiendo es 0,188 s/s; antes de cualquier sesión era 0,241 y después del libre 0,207 | **Cerrado** |
| Hallazgo del Architect: el indicador de carga | Que los `Spinner` de Workout y de Rutinas no giren ocultos | `Spinner.SetShown` se registra solo a la vista y en el panel (`Presentation/Controls/Spinner.cs:47-69`). `WorkoutScreenTests.LoadingSpinnersSpinOnlyWhileShown` pasa | **Cerrado** |
| QA-V4a-07 (menor) | La búsqueda obligatoria del repo público | 0 coincidencias en lo versionado y en lo nuevo (`git grep --untracked`), también sin distinguir mayúsculas. `RepoHygieneTests.MachinePathsAreRecognized` sigue pasando (dotnet y Unity) | **Cerrado** |
| QA-V4a-ROB-03 (menor) | Que al pasar a segundo plano se manden también los puntos de un lote que falló | `SessionRecorder.RetryWithThePending` (`Core/Workout/SessionRecorder.cs:345-354`) se llama en `FlushAndWait` y en `Abandon`. `WorkoutBackgroundTests.BackgroundFlushAlsoSendsTheFailedBatchAgain` pasa (es la L03 de la lente) | **Cerrado** |
| QA-V4a-ROB-04 (menor) | Que Terminar con un lote en vuelo, seguido de segundo plano o de cerrar la app, deje la sesión cerrada | `FlushAndWait` y `Abandon` esperan la base con tope; si lo que estaba en vuelo volvió, `TryQueueCloseNow` (`:359-390`) lo resuelve sin bloquear (`Result` solo con la tarea completa y sin falla) y encola el cierre. La continuación de `FinishAsync` espera ese mismo cierre (`:210-213`). Revisado a mano: cada lote se asienta una sola vez y el cierre sale una sola vez. Pasan `WorkoutBackgroundTests.FinishThenBackgroundWithABatchInFlightStillCloses` y `.QuitRightAfterFinishKeepsTheClose`. La variante completa queda para la 10a, como dice el Architect | **Cerrado** (lo mínimo de la lente) |
| QA-V4a-ROB-05 (menor) | Que un cuadro muy tarde en primer plano publique una sola foto | `WorkoutEngine.OnClock` junta los segundos (`_coalescing`) y publica una foto `Instant` al final (`Core/Workout/WorkoutEngine.cs:317-357`, `:625-635`); los puntos, los pasos y los avisos no cambian. Pasan `WorkoutHudStoreTests.OneInstantStateAfterALateFrameInTheForeground` y `ActiveClockTests.SixHundredSecondsWithALateFrame` | **Cerrado.** Sigue abierta la pregunta al PO sobre la suspensión (R2.6) |
| QA-V4a-L02 (menor) | La parte del oráculo que no necesita Flutter | `WorkoutGoldenTests.SanitizerScenariosMatchUntilTheFirstStepChange` pasa | **Parcial.** Los guiones nuevos del oráculo quedan para la 4b |

### R2.3 Capturas

**Determinismo.**

- `it4a`: A = B = repo en 50 de 51 archivos; en `manifest.json` cambia solo `durationMs`.
- `it3` (49 archivos) e `it2` (44): iguales al repo; en `manifest.json`, solo `durationMs`.
- Los PNG, MP4 e `INDEX.md` de las tres carpetas del repo tienen los mismos md5 que en la ronda 1. Ningún arreglo de esta ronda cambia lo que se ve en una toma, como dijo el Architect.

**Lo que se ve.** Se miraron con Read **las 46 fotos** de `it4a` y se compararon con la spec §7.3 y §7.4, con 04-design-notes §8.2 y §8.3 y con `docs/image-02.png` y `docs/image-03.png` del legacy.

- Coinciden toma por toma con la tabla de la ronda 1 (§2.2).
- Siguen sin divisor en la banda CAP-18, -20, -26, -27a, -27b y -30 en TV, y CAP-16 y -24 en L640: es QA-V4a-03, abierto.
- CAP-17 frente a `image-03.png`: las mismas zonas, con kcal y BPM en fila por DV-33.
- CAP-10 y DEMO-01 frente a `image-02.png`: las mismas posiciones, sin el escenario (DV-34).
- Los videos son los mismos bytes que en la ronda 1, que miró sus cuadros.

**Los íconos ▶ y ⏸ son los glifos de verdad, no un cuadrado:**

- `▶ Entrenamiento libre` en CAP-01, -02 (apagado), -03, -06, -07, -08, -09, -10, -11, -28 y DEMO-01, -06, -07, -13 y -14; `▶ Free workout` en CAP-04.
- `⏸ Pausa` en CAP-12, -13 y en todas las tomas del HUD; `⏸ Pause` en CAP-27a.
- `▶ Reanudar` en CAP-14 y DEMO-04.
- En la PC (R2.4): `▶ Free workout`, `⏸ Pause` y `▶ Resume`.

**Las tomas de las It. 2 y 3 frente a `HEAD`:**

- Difieren los mismos 88 archivos que en la ronda 1: 78 PNG, 6 MP4 y 4 entre `INDEX.md` y `manifest.json`.
- Sus md5 son los de la ronda 1, así que vale la explicación píxel por píxel de §2.3: la llave abajo a la izquierda (D-ART-10), el panel anclado, Workout en reposo en it2 CAP-21a, los avisos 8 dp sobre la llave y lo de QA-V3-03 y -04.
- Se miraron de nuevo it2 CAP-21a (Workout en reposo, con la llave abajo a la izquierda) e it3 CAP-04 (la llave abajo a la izquierda y el botón flotante a la derecha).
- **Ningún cambio sin motivo.**

### R2.4 Prueba en equipo (Windows, build de simulador)

#### R2.4.1 El build y la carpeta

Durante toda la verificación hubo abierta una instancia del build de simulador de `Builds/Windows/simulator`. La abrió el usuario desde el Explorador, antes de que empezara esta ronda, y QA no la tocó ni la cerró.

**Por qué no se recompiló esa carpeta.**

- **Archivos en uso.** Una consulta de solo lectura al Restart Manager de Windows mostró que esa instancia tiene abiertos `RowMate.exe`, `UnityPlayer.dll`, `lib_burst_generated.dll`, `globalgamemanagers`, dos `.resS` y los recursos de Unity.
- **Un archivo que ya no se puede escribir.** La compilación anterior, la de la ronda 1, reescribió `lib_burst_generated.dll`, y hoy está bloqueado. Una compilación encima habría fallado a mitad o le habría cambiado archivos a un proceso en marcha que no es de QA.
- **Lo que quedó.** Esa carpeta tiene todavía el build de la ronda 1, sin los arreglos: es QA-V4a-08.

**Qué carpeta se usó.**

- El build del código actual se hizo con `tools/build/build.ps1 -Simulator -Out Builds/Windows/simulator-it3 -AllowNewFolder` y se abrió solo desde ahí.
- Es una carpeta de builds de simulador de la It. 3, sin ningún proceso abierto, que ya tenía su regla de permiso del Firewall.
- Las reglas de entrada de `rowmate.exe` fueron **12 antes y 12 después** (`Get-NetFirewallApplicationFilter`). No hubo ninguna regla nueva ni ningún aviso.
- Es un desvío de la regla "solo la carpeta de salida por defecto". Se respetó lo que la regla protege: ningún aviso nuevo del Firewall en la pantalla del usuario.

#### R2.4.2 Cómo se hizo

- **Los datos.** `ROWMATE_DATA_DIR` apuntaba a `<scratch>/it4a/qa-main-c2/devdata`. El registro dice `[APP] base rower_app_dev.db (carpeta de desarrollo)`. La base se sembró antes de abrir, una v8 con `F-4A`, con la herramienta de la ronda 1 recompilada contra los binarios de esta ronda.
- **El registro del player.** Fue a `<scratch>` (`-logFile`).
- **La ventana.** 1280 × 720, con `-screen-fullscreen 0`.
- **Cómo se manejó.**
  - Con clics y teclas por Win32, con los ayudantes de la ronda 1.
  - Cada clic solo se hace si la ventana de QA está adelante y el punto es suyo, y el puntero vuelve a su lugar.
  - Las teclas van con su *scan code*.
  - `Qa-Proc` solo mira el PID que lanzó QA y la ruta de `simulator-it3`.
- **Lo que se ve.** El build sale en inglés y su simulador tiene ruido.
- **El registro de Windows.** Antes de abrir se exportó `HKCU\Software\iguisoft\RowMate`, donde el player guarda la ventana al cerrar. Al terminar se importó: QA había cambiado 11 valores, y después de importar hay 0 diferencias con lo exportado.

Las fotos y `device.log` están en `<scratch>/it4a/qa-main-c2/device/`.

| Paso | Qué se hizo | Resultado |
|---|---|---|
| **E1** | Abrir | Dispositivo, `Remo simulado` · `Connected` (e1) |
| **E2** | Workout | `Workout`, `▶ Free workout` y `Or pick a routine:`. Las 4 tarjetas de `F-4A`; `Prueba 4a` con 6 tramos. La llave abajo a la izquierda. CPU en reposo, 10 s: 0,241 s/s (e2) |
| **E3** | Entrenamiento libre | El inmersivo en el acto: `Free Workout`, `00:04` a los 4 s, `Finish` y `⏸ Pause` (e3b). `Pause`: `▶ Resume` verde y el reloj en `00:13`; 3 s después sigue en `00:13`, y SPM, vatios, distancia y kcal cambian (e3c, e3d). `Resume`: `00:15` a los 2,2 s (e3e). `Finish` abre el diálogo y el reloj sigue, `00:18` (e3g). `Cancel`: la sesión sigue, `00:19` (e3h). `Finish` y `Finish`: Workout en reposo, **sin aviso** (e3i, e3j). CPU en reposo, 8 s: 0,207 s/s |
| **E4** | `Corta` hasta el final | El diálogo `Corta` / `1 interval · 1 rest` / `Start the workout?` (e4a). A 1,5 s: `Work` rosa, la pastilla `18 spm · ↗ SPM +2/rep`, `Up next: Rest` y `0:09`, con SPM en blanco, borde y `target 18` (e4b). A 6,6 s: `0:04` agrandado y teñido (e4c). A 12,4 s: `Rest` con `Up next: Work` y `0:03` (e4d). En el último paso no hay `Up next` (e4e). A los 30 s terminó sola: Workout en reposo con `Workout Completed!` (e4g) |
| **E5** | `Prueba 4a` | A los 34 s, trabajo de la rep. 1: la pastilla `>160W · 22 spm · ↗ SPM +2/rep`; Watts `▲ 146` rojo con `target 160`, borde y halo; SPM `24.4` blanco con `target 22` (e5b). La llave del HUD abre el panel con Split y Watts a la vista; dos `+` llevan Watts a 170 W, y la tarjeta queda blanca con `167` (e5d) y `164` 2 s después (e5f). A los 80 s, `Up next: Rest` (e5g). `Finish` y `Finish` con `Up next` latiendo: reposo sin aviso (e5h). **CPU en reposo después, 10 s: 0,188 s/s**, no más que antes de cualquier sesión (QA-V4a-02) |
| **E6** | Sin remo | Llave en reposo, `Simular desconexión`: aviso `⚠ Connect the rower to start a workout`, todo apagado, y un toque en `Free workout` no hace nada (e6c). A los 3 s todo vuelve (e6d). Dispositivo → `Disconnect` → Workout: aviso y tarjetas apagadas; un toque en `Prueba 4a` no abre nada (e6g). `Search devices` → `Connected` (e6i). La base no sumó sesiones |
| **E7** | Minimizar | Libre. Minimizada a los 9,8 s y restaurada a los 35,7 s. Leída minimizada, a los 34,8 s, la base tenía **6 puntos**: el de antes y 5 más mientras estaba minimizada. Con los arreglos de ROB-03 y -04, `FlushAndWait` corre al perder el foco. La ronda 1 hizo los 2 min de AC-WKT-02-09 por el mismo camino |
| **E8** | Matar el proceso | La misma sesión, terminada con `Stop-Process -Force` a los 47,53 s, solo el PID de QA. La base: `finished_at` vacío, totales en 0, **9 puntos** (segundos 5 a 45, tiempos del monitor 387 a 427), `-wal` presente e `integrity_check` ok. Al reabrir arranca en Dispositivo, sin ofrecer nada, y la sesión no cambió (e8b) |
| **E9** | Teclado | **(a)** Clic en la pestaña Workout y Tab: el foco va a la pestaña `Routines` **con** anillo, y Enter abre Rutinas (e9b, e9c). **(b)** Clic en la tarjeta `Corta`, que abre el editor; el primer Tab **no muestra anillo**; Enter **sale del editor**, porque el foco invisible estaba en `←` (e9d, e9e). **(c)** El caso de la ronda 1, en el HUD: `Finish` y `Cancel` con el mouse; el primer Tab **no muestra anillo** (e9j); Enter abre `Finish workout` (e9k); Escape lo cierra (e9l); el segundo Tab muestra el anillo blanco en `Pause` (e9m). Escape en la lista de Rutinas no hace nada. QA-V4a-04 sigue (R2.6) |
| **E10** | Cerrar | Libre terminado con `Finish` y `Finish`. Cerrar la ventana (`WM_CLOSE`, solo a la de QA): el proceso sale en 193 ms, sin `-wal` ni `-shm` (*checkpoint* hecho) |
| **E11** | Pantalla encendida | **NO VERIFICABLE** en equipo: `powercfg /requests` pide una consola de administrador (la respuesta textual es "This command requires administrator privileges"). Lo que hay: `WakeLockTests` (4, Unity EditMode, con `SetThreadExecutionState` de verdad) y `ScreenKeepAwakeTests` (5) pasan en las dos EditMode |

#### R2.4.3 La base del equipo al terminar

Se leyó con la herramienta de QA sobre una **copia** de la base y su `-wal`.

```
user_version 8 · integrity ok
S1 (libre)      02:23:18 → 02:23:42  260 m  69 s  149 W  24.4 spm  15 kcal  origin=app  5 puntos
S2 Corta/3      02:24:29 → 02:24:59  550 m 146 s  155 W  24.6 spm  33 kcal  origin=app  7 puntos
S3 Prueba 4a/4  02:25:38 → 02:26:59 1010 m 266 s  174 W  24.8 spm  61 kcal  origin=app 17 puntos
S4 (libre)      02:29:17 → NULL       0 m   0 s    0 W   0   spm   0 kcal  origin=app  9 puntos
S5 (libre)      02:31:47 → 02:32:20  403 m 107 s  156 W  24.6 spm  24 kcal  origin=app  7 puntos
puntos huérfanos 0 · rutinas 4 (horas en UTC)
```

| Sesión | Coherencia |
|---|---|
| S1, libre (E3) | 24,3 s de reloj con 3,6 s de pausa: 4 puntos y el final. El tiempo del monitor salta de 55 a 64 al reanudar, porque el monitor sigue contando en pausa (AC-WKT-06-08). El final repite la lectura del segundo 20 porque todavía no había otra |
| S2, `Corta` (E4) | `step_index` 0, 0, 1, 2, 2, 3, 3 y tipos `work, work, rest, work, work, rest, rest` (AC-WKT-06-03). Los dos últimos iguales (AC-WKT-06-07). 30,02 s de inicio a fin; `routine_id` y `routine_name` de la plantilla |
| S3, `Prueba 4a` (E5) | Terminada a mano a los 81,1 s: 16 puntos más el final, que es distinto del último (`t` 265 → 266) |
| S4, libre (E7 y E8) | **Cortada**: `finished_at` vacío, totales en 0, sin punto final (AC-WKT-06-10) |
| S5, libre (E9 y E10) | 33,6 s: 6 puntos más el final |

**Totales.** En las 4 sesiones cerradas, los totales guardados son iguales a los recalculados de sus puntos:

- el máximo de distancia, de tiempo del monitor y de kcal;
- el P99 de vatios y de SPM, con la regla de `Core/Models/SessionStats.cs`: índice ⌊(n − 1) · 0,99⌋ sobre los valores ordenados (AC-WKT-06-06).

#### R2.4.4 Lo demás del equipo

- **Registro del player.**
  - Ningún error ni ninguna excepción; las líneas con `System.Exception` son la firma del registrador de la app en la traza de pila de los mensajes normales de un build de desarrollo.
  - Ningún aviso `WORKOUT`, o sea, ninguna falla de guardado.
  - **88 avisos de DPI**, todos de `TabularNumber:ApplySize (TabularNumber.cs:365)`, en la primera instancia: QA-V4a-05.
  - La segunda instancia (E8 a E10) no tiene ninguno.
- **Las bases del usuario (AC-HIS-20-04).**
  - `rower_app.db` no existe.
  - El `-wal` de `rower_app_dev.db` de la carpeta por defecto se escribió una vez durante esta ronda, a las 23:19:04 (hora local), con el mismo tamaño.
  - El Restart Manager muestra que esa base, su `-wal` y su `-shm` los tiene abiertos **solo** la instancia del usuario.
  - A esa hora QA no tenía ningún Unity abierto ni ningún player, y no mandó clics ni teclas. Los builds empezaron casi un minuto después, y sus registros no abren ninguna base.
  - Las corridas de QA usaron carpetas temporales o `<scratch>`. Igual que en la ronda 1, la escribió la instancia del usuario.
  - `TestResults.xml` en esa carpeta: lo escribe el Unity Test Framework en cada corrida de PlayMode, como en las It. 2 y 3. No es una base.

### R2.5 Sondas en Unity (temporales)

- **Dónde estaban.** En `Tests/PlayMode/QaProbe4aR2Tests.cs`, sobre `WorkoutUiTestBase`.
- **Cómo se corrieron.** Con `-Platform PlayMode -Simulator -Filter QaProbe4aR2`.
- **Después.** Se borraron con su `.meta`, y `git status` quedó igual que al empezar.
- **Para qué.** Son las sondas de la ronda 1 contra el código arreglado, más dos variantes nuevas.

| Sonda | Qué hace | Resultado |
|---|---|---|
| `R2_Rob01_AutoEndThenFreeDuringExit` | `Corta` termina sola a los 30 s. Con el inmersivo saliendo, toque en `workout-free` y `Settle`. Después, `Finish` y confirmar | **Pasa.** HUD nuevo, distinto del viejo. El viejo no deja superficie. 2 sesiones, la segunda cerrada. 0 demandas al final |
| `R2_Rob01_ManualFinishThenFreeDuringExit` | Libre a los 12 s, `Finish` y confirmar. Avanza de a 10 ms hasta que no queda diálogo, el viejo sigue saliendo y `workout-free` responde. Ahí, el toque | **Pasa.** El toque fue a los 150 ms del confirmar. HUD nuevo, con Pausa y Terminar que responden. 2 sesiones |
| `R2_Rob02_HeartAndPulseLeaveTheDriver` | `Prueba 4a` con pulso 140, a los 20 s (`Próximo` late). `Finish` y confirmar; después, 5 s en reposo | **Pasa.** `corazón=False pulso=False animaciones=0 (reposo 0) demandas=0` |
| `R2_Rob02_ThreeFreeSessionsWithPulseLeaveNothing` | Tres libres seguidas con pulso 140, de 8 s cada una | **Pasa.** Después de cada una, el corazón fuera, las animaciones como en reposo y 0 demandas |
| `R2_BandDividerAtSixSeconds` | `Prueba 4a` a los 6 s, en TV (411 × 914) | **Falla, la esperada**: `layout.width=0 flexShrink=1`. QA-V4a-03 abierto |
| `R2_DpiWarningsWhenClosingWhileDigitsRoll` | Cuenta los avisos de DPI al cerrar con cifras rodando | Pasa (solo informa): 0 en el anfitrión de pruebas. En el player salen, como se ve en R2.4.4 |

**Lo que las sondas no cubren.** El anfitrión de pruebas no reserva la llave: el registro de la sonda dice `superficie=Tab`. Por eso la convivencia de dos inmersivos con la superficie de la llave no tiene prueba. Se revisó en el código: `SetSurface` y `SetBottomObstacle` van por dueño, así que la página vieja solo retira lo suyo (R2.2).

### R2.6 Issues

**Cerrados en esta ronda** (detalle en R2.2): QA-V4a-01 (= ROB-01) y QA-V4a-02 (= ROB-02), los dos mayores; QA-V4a-07, ROB-03, ROB-04 y ROB-05, menores; y el hallazgo del indicador de carga.

**Abiertos:**

| ID | Sev. | Resp. | Issue | Evidencia | Corrección sugerida |
|---|---|---|---|---|---|
| QA-V4a-03 | menor | architect | **El divisor de la banda sigue con ancho 0** (AC-WKT-17-04). No se tocó en esta ronda | Sonda R2: `layout.width=0 flexShrink=1` a los 6 s en TV. `Presentation/Workout/HudBand.cs:68-74` le pone `width` y `height` pero no `flexShrink`, y `Presentation/Styles/Base.uss` no tiene ninguna regla `.rm-hud-band__divider` (solo `.rm-hud-band`, `__column`, `__row` y `__unit`, `:2016-2034`). CAP-18, -20, -26, -27a, -27b y -30 sin divisor; son los mismos bytes que en la ronda 1 | La de la ronda 1: `flex-shrink: 0` en el divisor (USS o `style.flexShrink = 0`), una afirmación de ancho en `ImmersiveHudTests.BandVitalsAndLiveValues`, regenerar `it4a` con dos corridas y que lo vea el Designer |
| QA-V4a-04 | menor | architect | **El primer Tab después de usar el mouse enfoca sin anillo** (AC-APP-05-07). Sigue igual y **se vio también en el editor de Rutinas**. Después de un clic, el primer Tab deja el foco en `←` sin anillo, y un Enter cierra el editor. `RoutineEditorPage.OnBackRequested` cierra sin confirmar: es paridad con el legacy, que tampoco confirma (`lib/features/routines/routine_editor_screen.dart` no tiene `PopScope` ni `WillPopScope`). Así, un Tab y un Enter sin ver el foco descartan lo que no se guardó | Equipo E9 (b): e9d sin anillo, e9e de vuelta en la lista. E9 (c), el caso de la ronda 1: e9j sin anillo y e9k con el diálogo `Finish workout`. Con el foco ya en el teclado, el segundo Tab muestra el anillo (e9m). `FocusHighlight` escucha en `#app-root` (`Presentation/Controls/FocusHighlight.cs:15-21`; se crea en `Presentation/Shell/AppShellView.cs:103`). Después de un clic, con DT4a-01 no queda ningún control enfocado, y el primer `KeyDown`/`NavigationMoveEvent` no pasa por ahí | La de la ronda 1: registrar `FocusHighlight` en `root.panel.visualTree`, o poner la clase en un `FocusInEvent` que no venga de un puntero. Una PlayMode "sin foco, un Tab enfoca el primer control **con** anillo", en el HUD y en el editor |
| QA-V4a-05 | menor | architect | **`TabularNumber.Tick` sigue animando cifras que ya salieron del panel**: avisos de DPI de Unity en el registro | Registro del player de la PC: 88 avisos, todos con la pila `TabularNumber:ApplySize (TabularNumber.cs:365)` | La de la ronda 1: terminar el rodillo en `DetachFromPanelEvent`, o no medir con `panel == null` |
| QA-V4a-06 | menor | architect | **WP-B0 sin integrar**: el kerning K2 y RowMate Numeric SC | `HudLayoutTests.ScalesOfTheDesignNotes` sigue con `[Ignore]`: es la única ignorada de PlayMode | La de la ronda 1: hacer B0 en el cierre, o pasarlo a la 4b en STATUS con lo que arrastra |
| QA-V4a-08 | menor | architect | **Nuevo, operativo.** `Builds/Windows/simulator` tiene todavía el build de la ronda 1, sin los arreglos, porque la instancia del usuario lo tenía abierto (R2.4.1). Quien pruebe desde ahí sigue viendo ROB-01 y ROB-02 | R2.4.1: los archivos abiertos (Restart Manager), `lib_burst_generated.dll` bloqueado y la fecha de los archivos del build | Cuando el usuario cierre su RowMate: `tools/build/build.ps1 -Target Windows -Simulator`, sin `-Out`. No hace falta repetir la prueba en equipo: el código es el mismo que el de `simulator-it3` |
| QA-V4a-ROB-05 (pregunta) | menor | po | Si Windows suspende el equipo con una sesión en marcha, ¿el tiempo suspendido cuenta como tiempo de sesión? AC-WKT-02-09 solo cubre la ventana minimizada o sin foco | [02-tech-design, "Cambios por verificación (ronda 1)"](02-tech-design.md#cambios-por-verificación-ronda-1), la pregunta para el PO | Que decida el PO. Después, quien pueda suspender el equipo mide el `Stopwatch` |
| QA-V4a-L01 | menor | po | Lente de paridad: un monitor que no informa la distancia cada segundo puede trabar un paso por distancia (Q-55) | [05-qa-lens-paridad](05-qa-lens-paridad.md) §2 | La de la lente: sumarlo a P-04 y DV-26 y decidirlo antes de la 10a |
| QA-V4a-L02 | menor | architect | **Parcial.** Faltan los guiones nuevos del oráculo, los que la lente corrió a mano | Hecho: `WorkoutGoldenTests.SanitizerScenariosMatchUntilTheFirstStepChange`. Pendiente según 02-tech-design, "Pendientes de esta ronda" | La propuesta del Architect: la 4b, junto con el diálogo de completado |

### R2.7 Matriz AC → test → resultado (ronda 2)

**De dónde sale cada columna:**

- **Tests:** la matriz de `run-tests.ps1 -UnityResults` del paso 11, que junta dotnet y las cuatro suites de Unity. Se nombran los dos primeros tests de cada AC.
- **Tomas:** las mismas de la ronda 1, iguales byte a byte.
- **Equipo y sondas:** los de esta ronda (R2.4 y R2.5).
- **R1:** el resultado de la ronda 1 (§6).
- **Verif.:** la marca de `It04aIds`.

**Totales:** 162 AC = **156 PASS, 2 FAIL, 4 NO VERIFICABLE**; LT-35 a LT-45, 11 PASS. Cambian respecto de la ronda 1 AC-WKT-01-09 y AC-WKT-13-01, que pasan de FAIL a PASS.

| AC | Verif. (en LT, el test del legacy) | R1 | R2 | Evidencia |
|---|---|---|---|---|
| AC-WKT-01-01 | d | PASS | PASS | `Composition.AppComposerTests.ComposesTheWorkoutEngineAndItsStores`, `Workout.WorkoutEngineLifecycleTests.FourPhasesStartingIdle` |
| AC-WKT-01-02 | d p | PASS | PASS | `Workout.WorkoutEngineLifecycleTests.ClockNeverWaitsMoreThan100Ms`, `Workout.WorkoutEngineLifecycleTests.StartFreeCreatesTheSessionThenTheClock`; equipo E3 (el inmersivo abre en el acto; `00:04` a los 4 s) |
| AC-WKT-01-03 | d | PASS | PASS | `Workout.WorkoutEngineLifecycleTests.RoutineStartExpandsAndPositions`, `Workout.WorkoutEngineLifecycleTests.SessionKeepsTheTemplateIdAndName` |
| AC-WKT-01-04 | d | PASS | PASS | `Workout.WorkoutEngineLifecycleTests.PauseFreezesClocksSamplingTargetsAndMeters`; equipo E3 (reloj quieto en `00:13` durante 3 s, con los valores en vivo) |
| AC-WKT-01-05 | d | PASS | PASS | `Workout.ActiveClockTests.ResumeRestartsTheRhythm`, `Workout.SanitizedDistanceTests.RebaseIgnoresWhatWasRowedInPause` |
| AC-WKT-01-06 | d p | PASS | PASS | `Workout.WorkoutEngineLifecycleTests.FinishSavesThenGoesIdle`, `Workout.WorkoutHudStoreTests.ControlsFollowThePhase`; equipo E3, E4, E5 (cierre y vuelta a reposo; 4 sesiones cerradas) |
| AC-WKT-01-07 | d | PASS | PASS | `Workout.WorkoutEngineLifecycleTests.IdleKeepsTheLastReadingAndTheLastFinished` |
| AC-WKT-01-08 | d | PASS | PASS | `Workout.WorkoutEngineLifecycleTests.NoUndoAndStartOnlyFromIdle` |
| AC-WKT-01-09 | p | FAIL | PASS | `WorkoutScreenTests.StartDuringThePreviousExitHasItsOwnImmersive`, `WorkoutScreenTests.TabShowsOnlyInProgressUnderTheImmersive`; **corregido** (QA-V4a-01): `WorkoutScreenTests.StartDuringThePreviousExitHasItsOwnImmersive` (con y sin reducir movimiento); sonda R2 con fin automático y con Terminar a mano |
| AC-WKT-01-10 | d p | PASS | PASS | `Workout.WorkoutStoreTests.SecondStartIsIgnored`, `WorkoutScreenTests.OneTapOneSession` |
| AC-WKT-01-11 | d p cap | PASS | PASS | `Workout.SessionRecorderTests.CreateFailureDiscardsThePoints`, `Workout.WorkoutEngineLifecycleTests.CreateFailureStillRuns`; tomas CAP-29 |
| AC-WKT-02-01 | d | PASS | PASS | `Workout.ActiveClockTests.CountsWholeSecondsOfTheSegment`, `Workout.ActiveClockTests.OneSecondPerActiveSecondOnly` |
| AC-WKT-02-02 | d | PASS | PASS | `Workout.WorkoutEngineLifecycleTests.SecondOrderIsTheLegacyOrder` |
| AC-WKT-02-03 | d p | PASS | PASS | `Workout.WorkoutEngineLifecycleTests.HudClockIsTheActiveClock` |
| AC-WKT-02-04 | d | PASS | PASS | `Common.FrameSchedulerTests.TickIsTheRunDueOfIterationTwo`, `Project.PureCodeRulesTests.NoSystemClockInTheWorkoutEngine` |
| AC-WKT-02-05 | d | PASS | PASS | `Common.FrameSchedulerTests.CatchUpFiresEverythingInOrderWithVirtualTime`, `Workout.WorkoutBackgroundTests.BackgroundIsLikeForeground` |
| AC-WKT-02-06 | d p | PASS | PASS | `Common.ForegroundTimeoutTests.CreatedInTheBackgroundStartsWholeOnReturn`, `Common.ForegroundTimeoutTests.RunsLikeAScheduleInTheForeground` |
| AC-WKT-02-07 | d | PASS | PASS | `Workout.WorkoutBackgroundTests.PausedSessionDoesNotMoveInBackground` |
| AC-WKT-02-08 | d p eq | PASS | PASS | `Workout.WorkoutBackgroundTests.EightHourCatchUpIsBounded`, `Workout.WorkoutDeviceChecksTests.AndroidFirstFrameAfterBackground`; parte [Device-And] NO VERIFICABLE (sin módulo ni teléfono) |
| AC-WKT-02-09 | d eq | PASS | PASS | `Project.BootstrapperStaticTests.PlatformMinimumsComeFromAppIdentity`, `Workout.WorkoutStoreTests.HeldToastShowsOnFocus`; equipo E7: 25 s minimizada, la base siguió sumando puntos (6 a los 34,8 s); la ronda 1 hizo los 2 min con el mismo camino |
| AC-WKT-02-10 | d eq | NO VERIFICABLE | **NO VERIFICABLE** | `Workout.WorkoutDeviceChecksTests.AndroidBackgroundTwoMinutes`, `Workout.WorkoutDeviceChecksTests.AndroidMonotonicClockAcrossDeepSleep`; Android: sin módulo ni teléfono (`[Ignore]` de `WorkoutDeviceChecksTests`) |
| AC-WKT-03-01 | d | PASS | PASS | `Workout.StepAdvanceTests.Prueba4aExpandsToSixSteps` |
| AC-WKT-03-02 | d | PASS | PASS | `Workout.StepAdvanceTests.TimeStepEndsAtItsDuration`, `Workout.StepAdvanceTests.TimeWinsOverDistance` |
| AC-WKT-03-03 | d | PASS | PASS | `Workout.SanitizedDistanceTests.NegativeAndTooFastJumpsDoNotCountAndBecomeTheR...`, `Workout.StepAdvanceTests.DistanceStepWithTheSanitizer("R-4",25)`; sigue abierta la observación QA-V4a-L01 (del PO) |
| AC-WKT-03-04 | d | PASS | PASS | `Workout.StepAdvanceTests.StepChangeKeepsTheExtraMetersInTheOldStep` |
| AC-WKT-03-05 | d p cap | PASS | PASS | `Workout.StepAdvanceTests.FinishedRoutineStaysOnTheLastStep`, `Workout.WorkoutEngineLifecycleTests.LastStepFinishesAndSaves`; tomas CAP-28, DEMO-13; equipo E4 (`Corta` terminó sola a los 30 s, con `Workout Completed!`) |
| AC-WKT-03-06 | d | PASS | PASS | `Workout.StepAdvanceTests.FinishedRoutineStaysOnTheLastStep`, `Workout.StepProgressTests.FinishedRoutine` |
| AC-WKT-03-07 | d p | PASS | PASS | `Workout.StepAdvanceTests.NegativeDurationEndsAtTheFirstSecond`, `Workout.StepAdvanceTests.ZeroSecondStepEndsAtTheFirstSecond` |
| AC-WKT-03-08 | d p cap | PASS | PASS | `Workout.StepAdvanceTests.NoGoalStepNeverEnds`, `Workout.WorkoutFormatsTests.DistanceStepAndNoGoal`; tomas CAP-30 |
| AC-WKT-03-09 | d | PASS | PASS | `Workout.StepAdvanceTests.FreeHasNoStepsNorEnd` |
| AC-WKT-03-10 | d ps | PASS | PASS | `Workout.StepAdvanceTests.RowerDisconnectKeepsClockAndTimeSteps`, `Workout.WorkoutEngineLifecycleTests.EngineIgnoresTheConnection` |
| AC-WKT-04-01 | d | PASS | PASS | `Workout.StepProgressTests.ExistsOnlyWithRoutineOutsideIdle` |
| AC-WKT-04-02 | d | PASS | PASS | `Workout.StepProgressTests.NegativeDurationShowsZero`, `Workout.StepProgressTests.Remaining` |
| AC-WKT-04-03 | d | PASS | PASS | `Workout.StepProgressTests.Progress` |
| AC-WKT-04-04 | d | PASS | PASS | `Workout.StepProgressTests.EndingSoonOneToFive` |
| AC-WKT-04-05 | d | PASS | PASS | `Workout.StepProgressTests.UpNextZeroToFifteen` |
| AC-WKT-04-06 | d p cap | PASS | PASS | `Workout.StepProgressTests.ExamplesOfTheSpec`, `ImmersiveHudTests.UpNextAndEndingSoonOnScreen`; tomas CAP-21, CAP-22; equipo E4 (`Up next` todo el paso de 10 s; `0:04` agrandado y teñido) |
| AC-WKT-04-07 | d | PASS | PASS | `Workout.StepProgressTests.FinishedRoutine`, `Workout.StepProgressTests.FinishedRoutineByDistance` |
| AC-WKT-05-01 | d | PASS | PASS | `Workout.WorkoutProviderHrTests.el pulsómetro manda sobre el FTMS y se actuali...` |
| AC-WKT-05-02 | d | PASS | PASS | `Workout.WorkoutProviderHrTests.orden real de caída: bpm 0 y después disconnected` |
| AC-WKT-05-03 | d | PASS | PASS | `Workout.WorkoutProviderHrTests.un bpm recibido durante connecting se conserva...` |
| AC-WKT-05-04 | d | PASS | PASS | `Workout.WorkoutProviderHrTests.connected del sensor sin bpm previo no toca lo...` |
| AC-WKT-05-05 | d | PASS | PASS | `Workout.TelemetryTests.PointsCarryTheFusedPulse`, `Workout.WorkoutProviderHrTests.FusionInEveryPhaseFeedsHudAndPoints` |
| AC-WKT-05-06 | ps cap | PASS | PASS | `WorkoutSimulatorTests.SimulatedHeartRateOnTheHud`; tomas CAP-13 |
| AC-WKT-06-01 | d | PASS | PASS | `Infrastructure.SqliteSessionRepositoryTests.CreateWritesTheLegacyRow`, `Workout.TelemetryTests.StartCreatesTheRow` |
| AC-WKT-06-02 | d | PASS | PASS | `Workout.TelemetryTests.PointEveryFiveActiveSeconds`, `Workout.TelemetryTests.TelemetryPointCopiesTheReading` |
| AC-WKT-06-03 | d | PASS | PASS | `Workout.TelemetryTests.PointBelongsToTheStepThatEnds`, `Workout.WorkoutGoldenTests.Corta`; equipo E4: 7 puntos, `step_index` 0, 0, 1, 2, 2, 3, 3 y tipos `work, work, rest, work, work, rest, rest` |
| AC-WKT-06-04 | d | PASS | PASS | `Workout.TelemetryTests.NoSamplingInPause` |
| AC-WKT-06-05 | d eq | PASS | PASS | `Infrastructure.SqliteSessionRepositoryTests.KilledAt31_5sHasSixPoints`, `Infrastructure.SqliteSessionRepositoryTests.KilledAt31_5sHasSixPointsWithTheS...`; equipo E8: proceso terminado a la fuerza a los 47,53 s → 9 puntos (segundos 5 a 45) |
| AC-WKT-06-06 | d | PASS | PASS | `Workout.TelemetryTests.FinishWritesTheFinalPointAndTotals`; equipo: en las 4 sesiones cerradas, totales = recalculados de sus puntos (máximos y P99 con índice ⌊(n−1)·0,99⌋) |
| AC-WKT-06-07 | d | PASS | PASS | `Workout.TelemetryTests.LastTwoPointsEqualWhenEndingOnAFive`, `Workout.WorkoutGoldenTests.Corta`; equipo E4 (los dos últimos puntos de `Corta`, iguales); QA-V4a-L02 parcial |
| AC-WKT-06-08 | d | PASS | PASS | `Workout.TelemetryTests.MonitorCountersAreSaved` |
| AC-WKT-06-09 | d | PASS | PASS | `Workout.TelemetryTests.ShortAndUnrowedSessions` |
| AC-WKT-06-10 | d eq qa | PASS | PASS | `Infrastructure.LocalStoreTests.DisposeFinishesTheQueuedWork`, `Infrastructure.SqliteSessionRepositoryTests.CutSessionIsOpenWithItsPoints`; equipo E8: `finished_at` vacío, totales en 0, sin punto final; al reabrir, Dispositivo y la sesión sin cambios |
| AC-WKT-06-11 | d | PASS | PASS | `Workout.WorkoutBackgroundTests.LongBackgroundAfterTheEndStillSaves`, `Workout.WorkoutEngineLifecycleTests.LastFinishedOnlyWhenTheCloseSucceeds` |
| AC-WKT-06-12 | d p cap | PASS | PASS | `Infrastructure.SqliteSessionRepositoryTests.CloseIsAtomic`, `Infrastructure.SqliteSessionRepositoryTests.CloseRetriesTheFailedBatchInOneTr...`; tomas CAP-29 |
| AC-WKT-06-13 | d p | PASS | PASS | `Infrastructure.SqliteSessionRepositoryTests.BatchIsOneTransactionInOrder`, `Workout.SessionRecorderTests.SlowDatabaseKeepsOrderAndBatches` |
| AC-WKT-06-14 | d qa | PASS | PASS | `Infrastructure.SqliteSessionRepositoryTests.NothingButSessionsAndPointsIsWritten`, `Project.PureCodeRulesTests.InfrastructureDoesNotKnowTargetsOrSplits` |
| AC-WKT-06-15 | d | PASS | PASS | `Infrastructure.SqliteSessionRepositoryTests.PointsAfterAFailedCreateWriteNothing`, `Infrastructure.SqliteSessionRepositoryTests.PointsAfterCreateWaitForTheCreate`; ROB-03 y ROB-04 **corregidos**: `WorkoutBackgroundTests.BackgroundFlushAlsoSendsTheFailedBatchAgain`, `.FinishThenBackgroundWithABatchInFlightStillCloses`, `.QuitRightAfterFinishKeepsTheClose` |
| AC-WKT-09-01 | d | PASS | PASS | `Workout.StepTargetTrackerTests.cumplido cuando el promedio de vatios alcanza ...` |
| AC-WKT-09-02 | d | PASS | PASS | `Workout.StepTargetTrackerTests.no cumplido si falla cualquiera de los objetiv...` |
| AC-WKT-09-03 | d | PASS | PASS | `Workout.StepTargetTrackerTests.el split se cumple por debajo del objetivo (má...` |
| AC-WKT-09-04 | d | PASS | PASS | `Workout.StepTargetTrackerTests.un paso sin objetivos no tiene resultado` |
| AC-WKT-09-05 | d | PASS | PASS | `Workout.StepTargetTrackerTests.sin lecturas remando el objetivo no se cumple;...` |
| AC-WKT-09-06 | d | PASS | PASS | `Workout.StepTargetTrackerTests.un paso no completado no tiene resultado y res...` |
| AC-WKT-09-07 | d | PASS | PASS | `Workout.StepAdvanceTests.TargetsFromTheEngine`, `Workout.StepTargetTrackerTests.WhatCountsAsATargetAndAsRowing` |
| AC-WKT-10-01 | d p | PASS | PASS | `Workout.TargetLightTests.OnlyCardsWithTheirTarget` |
| AC-WKT-10-02 | d | PASS | PASS | `Workout.TargetLightTests.Spm` |
| AC-WKT-10-03 | d | PASS | PASS | `Workout.TargetLightTests.WattsIgnoresTheMax` |
| AC-WKT-10-04 | d | PASS | PASS | `Workout.TargetLightTests.SplitAndNonPositiveTargets` |
| AC-WKT-10-05 | p cap | PASS | PASS | `HudLayoutTests.CardsArrowAndTargetMatchTheDesignNotes`, `ImmersiveHudTests.TargetPillAndTrafficLight`; tomas CAP-19, CAP-20, CAP-24 |
| AC-WKT-10-06 | d cap | PASS | PASS | `Workout.TargetLightTests.EvaluatedWithEachReadingWithoutTolerance`, `Workout.WorkoutHudStoreTests.EvaluatedOnEveryReadingAlsoInPause`; tomas VID-02 |
| AC-WKT-10-07 | p | PASS | PASS | `ImmersiveHudTests.StoppedIsOff` |
| AC-WKT-10-08 | p | PASS | PASS | `ImmersiveHudTests.FreeTopBar` |
| AC-WKT-10-09 | ps cap | PASS | PASS | `WorkoutSimulatorTests.DemoTrafficLight`; tomas CAP-19, DEMO-09, DEMO-10, DEMO-11; equipo E5 (Watts `▲ 146` rojo con `target 160` → dos `+` en el panel, 170 W → blanco `167` y `164`) |
| AC-WKT-11-01 | d | PASS | PASS | `Workout.WorkoutFormatsTests.LiveSplit(133,"02:13")`, `Workout.WorkoutFormatsTests.LiveSplit(59,"00:59")` |
| AC-WKT-11-02 | d | PASS | PASS | `Workout.WorkoutFormatsTests.Spm(24.0d,"24.0")`, `Workout.WorkoutFormatsTests.Spm(23.5d,"23.5")` |
| AC-WKT-11-03 | d | PASS | PASS | `Workout.WorkoutFormatsTests.Watts` |
| AC-WKT-11-04 | d | PASS | PASS | `Workout.WorkoutFormatsTests.HudDistance` |
| AC-WKT-11-05 | d | PASS | PASS | `Workout.WorkoutFormatsTests.KcalAndPulse` |
| AC-WKT-11-06 | d | PASS | PASS | `Workout.WorkoutFormatsTests.SessionClock(0,"00:00")`, `Workout.WorkoutFormatsTests.SessionClock(175,"02:55")` |
| AC-WKT-11-07 | d | PASS | PASS | `Workout.WorkoutFormatsTests.Countdown(65,"1:05")`, `Workout.WorkoutFormatsTests.Countdown(5,"0:05")` |
| AC-WKT-11-08 | d | PASS | PASS | `Workout.WorkoutFormatsTests.DistanceStepAndNoGoal` |
| AC-WKT-11-09 | d | PASS | PASS | `Workout.SeriesPanelsTests.formatos`, `Workout.WorkoutFormatsTests.LapTime(166.667d,"2:46.7")` |
| AC-WKT-11-10 | d | PASS | PASS | `Workout.SeriesPanelsTests.formatos`, `Workout.WorkoutFormatsTests.RepSplit` |
| AC-WKT-11-11 | d | PASS | PASS | `Workout.SeriesPanelsTests.formatos`, `Workout.WorkoutFormatsTests.PanelMeters(1245,"1.245 m")` |
| AC-WKT-11-12 | d | PASS | PASS | `Workout.WorkoutFormatsTests.WallClock(9,5,"09:05")`, `Workout.WorkoutFormatsTests.WallClock(13,54,"13:54")` |
| AC-WKT-11-13 | d | PASS | PASS | `Workout.WorkoutFormatsTests.TargetLabelOfTheExpandedStep`, `Workout.WorkoutFormatsTests.TargetValueOfTheHud` |
| AC-WKT-11-14 | d | PASS | PASS | `Workout.WorkoutFormatsTests.RoutineSummary` |
| AC-WKT-11-15 | d | PASS | PASS | `Workout.WorkoutFormatsTests.FinalSummary` |
| AC-WKT-11-16 | d | PASS | PASS | `Workout.WorkoutFormatsTests.SameUnderAnyCulture("de-DE")`, `Workout.WorkoutFormatsTests.SameUnderAnyCulture("ar-SA")` |
| AC-WKT-12-01 | p cap | PASS | PASS | `WorkoutScreenTests.WorkoutIsNotAPlaceholder`; tomas CAP-01 |
| AC-WKT-12-02 | p | PASS | PASS | `WorkoutScreenTests.ContentInOrder` |
| AC-WKT-12-03 | d p cap | PASS | PASS | `Workout.WorkoutStoreTests.FreeNeedsTheRower`, `WorkoutScreenTests.ContentInOrder`; tomas CAP-01 |
| AC-WKT-12-04 | p cap | PASS | PASS | `WorkoutScreenTests.RoutineListLikeRoutines`; tomas CAP-01, CAP-10 |
| AC-WKT-12-05 | d p | PASS | PASS | `Composition.AppComposerTests.ComposesTheWorkoutEngineAndItsStores`, `Workout.WorkoutStoreTests.UsesTheSharedCollection` |
| AC-WKT-12-06 | p cap | PASS | PASS | `WorkoutScreenTests.LoadingErrorAndEmpty`, `WorkoutScreenTests.LoadingSpinnersSpinOnlyWhileShown`; tomas CAP-03, CAP-06, CAP-07; y `WorkoutScreenTests.LoadingSpinnersSpinOnlyWhileShown` (hallazgo del Architect) |
| AC-WKT-12-07 | d p cap | PASS | PASS | `Workout.WorkoutStoreTests.TapConfirmsWhenItCanStart`, `WorkoutScreenTests.StartDialog`; tomas CAP-08, DEMO-07; equipo E4, E5 (diálogos `Corta` y `Prueba 4a`) |
| AC-WKT-12-08 | d p cap | PASS | PASS | `Models.RoutineExpansionTests.SaturateClampsAtTheIntLimits`, `Models.RoutineExpansionTests.SaturateWithoutOverflowIsTheSame`; tomas CAP-09 |
| AC-WKT-12-09 | p cap | PASS | PASS | `WorkoutScreenTests.ScrollsAsOne`; tomas CAP-05 |
| AC-WKT-20-01 | p cap | PASS | PASS | `HudControlsTests.ControlsRow`; tomas CAP-12, CAP-14 |
| AC-WKT-20-02 | p | PASS | PASS | `HudControlsTests.PauseAndResumeAtOnce`; equipo E3 (el botón cambia en el acto) |
| AC-WKT-20-03 | p cap | PASS | PASS | `HudControlsTests.FinishConfirmation`; tomas CAP-26; equipo E3, E9; "Atrás" de Android NO VERIFICABLE |
| AC-WKT-20-04 | p | PASS | PASS | `HudControlsTests.ConfirmEndsWithoutSummary`; equipo E3, E5 (reposo sin aviso) |
| AC-WKT-20-05 | p | PASS | PASS | `HudControlsTests.FinishFromPause` |
| AC-WKT-20-06 | p | PASS | PASS | `HudControlsTests.FinishShowsTheSpinnerAfter300Ms`, `HudControlsTests.NoDoubles` |
| AC-WKT-20-07 | p | PASS | PASS | `HudControlsTests.DialogClosesWhenTheRoutineEndsInBackground`, `HudControlsTests.FinishTapAfterTheEndDoesNothing` |
| AC-WKT-23-01 | d qa | PASS | PASS | `Project.LegacyDiscardTests.NoClassicWorkoutScreenPort`; [QA] legacy: `_FullscreenWorkoutPage` solo se declara (ronda 1) |
| AC-BLE-10-03 | d p cap | PASS | PASS | `Workout.WorkoutStoreTests.AllFourNoRowerStates`, `WorkoutScreenTests.NoRowerBannerInEveryState`; tomas CAP-02; equipo E6 (simulada y con `Disconnect`) |
| AC-BLE-10-04 | d p cap | PASS | PASS | `Workout.WorkoutStoreTests.FreeNeedsTheRower`, `WorkoutScreenTests.StartDisabledWithoutRower`; tomas CAP-02; equipo E6 (los toques en `Free workout` y en `Prueba 4a` no hacen nada; ninguna sesión nueva en la base) |
| AC-BLE-10-05 | ps cap | PASS | PASS | `WorkoutSimulatorTests.NoRowerBannerFollowsTheRowerLive`; tomas DEMO-14; equipo E6 (`Simular desconexión`: vuelve a los 3 s; `Disconnect` → `Search devices` → `Connected`) |
| AC-BLE-10-06 | d p | PASS | PASS | `Workout.WorkoutStoreTests.StartRoutineDoesNotCheckTheRower`, `WorkoutScreenTests.ConnectionCheckedOnlyOnTap` |
| AC-BLE-10-07 | d | PASS | PASS | `Workout.WorkoutEngineLifecycleTests.EngineIgnoresTheConnection` |
| AC-APP-14-01 | u eq | NO VERIFICABLE | **NO VERIFICABLE** | `WakeLockTests.AndroidAndIosKeepTheScreenOnWithSleepTimeout`, `WakeLockTests.WindowsRequestIsAcceptedAndReleased`; [Device-Win]: `powercfg /requests` pide consola de administrador; `WakeLockTests` (4) pasan en las dos EditMode |
| AC-APP-14-02 | d eq | NO VERIFICABLE | **NO VERIFICABLE** | `Common.ScreenKeepAwakeTests.DisposeUnsubscribes`, `Common.ScreenKeepAwakeTests.RequestsAgainOnResumeAndFocus`; Windows: lo mismo que AC-APP-14-01; Android sin módulo ni teléfono |
| AC-APP-14-03 | u eq | NO VERIFICABLE | **NO VERIFICABLE** | `WakeLockTests.OtherPlatformsRejectUntil10b`, `WakeLockTests.ReleaseRunsOnTheRequestingThread`; [Device-Win]: ídem; `WakeLockTests.ReleaseRunsOnTheRequestingThread` pasa |
| AC-APP-14-04 | d qa | PASS | PASS | `Common.ScreenKeepAwakeTests.RejectedRequestOnlyLogsOnce`, `Common.ScreenKeepAwakeTests.ThrowingWakeLockCountsAsARejection` |
| AC-WKT-13-01 | p | FAIL | PASS | `ImmersiveHudTests.OpensAtOnce`, `WorkoutScreenTests.StartDuringThePreviousExitHasItsOwnImmersive`; **corregido** (QA-V4a-01): `WorkoutScreenTests.StartDuringThePreviousExitHasItsOwnImmersive`; sonda R2 (la sesión nueva tiene su HUD en el mismo toque) |
| AC-WKT-13-02 | p cap | PASS | PASS | `ImmersiveHudTests.Layers`; tomas CAP-12, CAP-18 |
| AC-WKT-13-03 | p eq | PASS | PASS | `Workout.WorkoutDeviceChecksTests.AndroidBackOpensTheConfirmation`, `HudControlsTests.BackOpensTheFinishDialogAndEscapeDoesNothing`; equipo E9 (Escape sin diálogo no hace nada; con el diálogo, lo cancela); "Atrás" de Android NO VERIFICABLE |
| AC-WKT-13-04 | p | PASS | PASS | `HudControlsTests.ConfirmEndsWithoutSummary`, `HudControlsTests.ImmersiveIsStackedLikeTheEditor`; QA-V4a-01 y -02 **corregidos**: `ImmersiveHudTests.LeavingTheImmersiveStopsItsContinuousMotions` (3 salidas); sondas R2; equipo E3 a E5 |
| AC-WKT-13-05 | p | PASS | PASS | `ImmersiveHudTests.StepRowWithoutTheRoutineName` |
| AC-WKT-14-01 | p cap | PASS | PASS | `ImmersiveHudTests.StepRowWithoutTheRoutineName`; tomas CAP-18 |
| AC-WKT-14-02 | p cap | PASS | PASS | `ImmersiveHudTests.StepRowWithoutTheRoutineName` |
| AC-WKT-14-03 | p cap | PASS | PASS | `ImmersiveHudTests.TargetPillAndTrafficLight`; tomas CAP-19, CAP-23 |
| AC-WKT-14-04 | p cap | PASS | PASS | `ImmersiveHudTests.LeavingTheImmersiveStopsItsContinuousMotions`, `ImmersiveHudTests.UpNextAndEndingSoonOnScreen`; tomas CAP-21, VID-01; y `ImmersiveHudTests.LeavingTheImmersiveStopsItsContinuousMotions`; equipo E4, E5 |
| AC-WKT-14-05 | p cap | PASS | PASS | `ImmersiveHudTests.LeavingTheImmersiveStopsItsContinuousMotions`, `ImmersiveHudTests.UpNextAndEndingSoonOnScreen`; tomas CAP-20, CAP-22; y `ImmersiveHudTests.LeavingTheImmersiveStopsItsContinuousMotions`; equipo E4 (`0:04`) |
| AC-WKT-14-06 | p cap | PASS | PASS | `ImmersiveHudTests.PausedRowStaysWithItsPulses` |
| AC-WKT-16-01 | p cap | PASS | PASS | `ImmersiveHudTests.FreeTopBar`; tomas CAP-12 |
| AC-WKT-17-01 | d p cap | PASS | PASS | `Design.HudGeometryTests.AnchorsAreTheLegacyZonesTimesTheScale`, `Design.HudGeometryTests.ScalesOfTheDesignNotes("TV",True,libre,1.0d,1.0d,1.0d...`; tomas CAP-12, CAP-15, CAP-16, CAP-17 |
| AC-WKT-17-02 | p | PASS | PASS | `ImmersiveHudTests.CardsAndColors` |
| AC-WKT-17-03 | p | PASS | PASS | `ImmersiveHudTests.CardsAndColors` |
| AC-WKT-17-04 | p | FAIL | **FAIL** | `ImmersiveHudTests.BandVitalsAndLiveValues`; QA-V4a-03 **sigue abierto**: sonda R2 `layout.width = 0`, `flexShrink = 1` a los 6 s en TV; CAP-18, -20, -26, -27a, -27b y -30 sin divisor (iguales a la ronda 1) |
| AC-WKT-17-05 | p cap | PASS | PASS | `ImmersiveHudTests.BandVitalsAndLiveValues`; tomas CAP-12, CAP-13, CAP-15 |
| AC-WKT-17-06 | p | PASS | PASS | `HudBudgetTests.NoAllocationsPerFrameWhileItOnlyBeats`, `ImmersiveHudTests.BandVitalsAndLiveValues`; `HudBudgetTests` en PlayMode y PlayMode con simulador; equipo E5 (CPU en reposo después de salir, igual o menor que antes) |
| AC-WKT-17-07 | p | PASS | PASS | `ImmersiveHudTests.RotationKeepsTheSession` |
| AC-WKT-17-08 | d p cap | PASS | PASS | `Design.HudGeometryTests.RectanglesOfTheDesignNotes("TV",libre+BPM,"TopBar",12...`, `Design.HudGeometryTests.RectanglesOfTheDesignNotes("TV",libre+BPM,"Controls",...`; tomas CAP-15, CAP-16, CAP-23, CAP-24 |
| AC-WKT-17-09 | d p cap | PASS | PASS | `Design.HudGeometryTests.BoatZone`, `Design.HudGeometryTests.ZonesDoNotOverlap`; tomas CAP-16 |
| AC-WKT-17-10 | p | PASS | PASS | `HudLayoutTests.ExtremeValuesFit`, `HudLayoutTests.ScalesOfTheDesignNotes` |
| AC-HIS-17-07 | d qa | PASS | PASS | `Infrastructure.LocalStoreTests.OneDatabaseForBothRepositories`, `Infrastructure.SqliteSessionRepositoryTests.NothingButSessionsAndPointsIsWritten`; [QA] base del equipo: v8, `integrity_check` ok, 5 sesiones con `origin` = `app`, 0 puntos huérfanos |
| AC-HIS-19-01 | d | PASS | PASS | `Infrastructure.SqliteSessionRepositoryTests.GetByIdInLocalTime` |
| AC-HIS-19-02 | d | PASS | PASS | `Infrastructure.SqliteSessionRepositoryTests.PointsOrderedByMonitorTime` |
| AC-SCN-01-01 | p cap | PASS | PASS | `ImmersiveHudTests.Layers`; tomas CAP-12 |
| AC-APP-03-09 | p cap | PASS | PASS | `HudControlsTests.ImmersiveIsStackedLikeTheEditor`; tomas VID-03 |
| AC-APP-03-10 | p cap | PASS | PASS | `HudControlsTests.FinishConfirmation`, `WorkoutScreenTests.StartDialog`; tomas CAP-08, CAP-26 |
| AC-APP-03-11 | p cap | PASS | PASS | `HudControlsTests.RoutineEndsWithTheFinishDialogOpen`, `WorkoutPersistenceTests.SaveErrorsShowOnceAndTheWorkoutGoesOn`; tomas CAP-09, CAP-28, CAP-29 |
| AC-APP-10-10 | d | PASS | PASS | `Localization.CatalogParityTests.SameKeysInBothLanguages`, `Localization.CatalogParityTests.TwelveItFourAKeys` |
| AC-BLE-16-01 | ps cap | PASS | PASS | `RoutinesSimulatorLayerTests.SimulatorLayerAboveEverything`, `SimulatorOverlayTests.RibbonIsAlwaysOnTop`; tomas CAP-12, CAP-26 |
| AC-BLE-16-02 | d s ps cap | PASS | PASS | `Design.BottomObstaclesTests.PositionsOfTheDesignNotes`, `Design.HudGeometryTests.WrenchSlot`; todas las tomas con simulador (`WrenchAudit` y `HudOverlap` en verde); equipo E2 a E6 |
| AC-BLE-16-03 | s ps cap | PASS | PASS | `SimulatorPanelLayoutTests.InTheWorkoutItGrowsUpAndLeftWithTheWattsCeiling(411...`, `SimulatorPanelLayoutTests.InTheWorkoutItGrowsUpAndLeftWithTheWattsCeiling(360...`; tomas DEMO-03; equipo E5 (panel desde la llave en el HUD, con Split y Watts a la vista) |
| AC-APP-02-08 | p cap | PASS | PASS | `RoutinesScreenTests.RoutinesIsNotAPlaceholder`, `ShellTests.PlaceholdersShowTitleAndDimIconOnly`; tomas CAP-01 |
| AC-APP-06-01 | p cap | PASS | PASS | `Design.MetricPaletteTests.AccentRestsAtSixtyPercent`, `Design.MetricPaletteTests.ContractHexes(Split,"#FF9F1C")`; tomas CAP-12, CAP-13, CAP-19 |
| AC-APP-05-05 | p cap | PASS | PASS | `Design.TextScaleStepsTests.ClassNames`, `Design.TextScaleStepsTests.FourStepsInOrder`; tomas CAP-05, CAP-11 |
| AC-APP-05-07 | p ps eq | FAIL | **FAIL** | `HudControlsTests.FocusReturnsOnlyWithTheKeyboard`, `HudControlsTests.TabOrder`; QA-V4a-04 **sigue abierto**: equipo E9, después de `Cancel` con el mouse el primer Tab no muestra anillo y Enter abre `Finish workout`; en el editor de Rutinas, Enter sale del editor |
| AC-APP-05-08 | p | PASS | PASS | `Design.DigitRollTests.ReducedMotionIsAnEightyMsFade`, `ImmersiveHudTests.ReducedMotion` |
| AC-RUT-06-08 | d p | PASS | PASS | `Models.RoutineExpansionTests.Case7EmptyOrNonPositiveRepeats`, `Models.RoutineExpansionTests.MatchesOracle` |
| AC-RUT-10-04 | ps cap | PASS | PASS | `RoutinesScreenTests.FabOpensANewEditor`, `RoutinesSimulatorLayerTests.FabTappableEverywhere`; tomas CAP-01 |
| AC-RUT-10-09 | ps cap | PASS | PASS | `Design.BottomObstaclesTests.PositionsOfTheDesignNotes`, `RoutinesScreenTests.ScrollPositionSurvivesEditorAndTabs`; tomas CAP-06 |
| AC-RUT-10-11 | p cap | PASS | PASS | `Routines.RoutineStructureTests.FixtureSegments`, `Routines.RoutineStructureTests.ImpossibleRepeatsNeverExpand`; tomas CAP-01, CAP-02, DEMO-01; equipo E2 (`Prueba 4a`: 6 tramos) |
| AC-HIS-17-02 | d qa | PASS | PASS | `Infrastructure.DatabaseSchemaTests.ColumnsTypesAndDefaults`, `Infrastructure.DatabaseSchemaTests.CreationLeavesTheVersionInTheMainFile` |
| AC-HIS-20-04 | d qa | PASS | PASS | `Project.ItThreeToolingTests.Cap27ProtectsTheUserDatabase`, `Project.ItThreeToolingTests.Cap27RehearsalNeverTouchesTheUserFolder`; [QA] todas las corridas en carpetas temporales o en `<scratch>`; la base del usuario la tiene abierta solo su instancia (§R2.4) |
| AC-APP-01-09 | p | PASS | PASS | `Composition.AppComposerTests.SlowLoadDoesNotBlockTheStart`, `Composition.AppComposerTests.StartLoadsRoutines` |
| LT-35 | test/workout_provider_hr_test.dart | PASS | PASS | `Workout.WorkoutProviderHrTests.el pulsómetro manda sobre el FTMS y se actuali...` |
| LT-36 | test/workout_provider_hr_test.dart | PASS | PASS | `Workout.WorkoutProviderHrTests.orden real de caída: bpm 0 y después disconnected` |
| LT-37 | test/workout_provider_hr_test.dart | PASS | PASS | `Workout.WorkoutProviderHrTests.un bpm recibido durante connecting se conserva...` |
| LT-38 | test/workout_provider_hr_test.dart | PASS | PASS | `Workout.WorkoutProviderHrTests.connected del sensor sin bpm previo no toca lo...` |
| LT-39 | test/step_target_tracker_test.dart | PASS | PASS | `Workout.StepTargetTrackerTests.cumplido cuando el promedio de vatios alcanza ...` |
| LT-40 | test/step_target_tracker_test.dart | PASS | PASS | `Workout.StepTargetTrackerTests.no cumplido si falla cualquiera de los objetiv...` |
| LT-41 | test/step_target_tracker_test.dart | PASS | PASS | `Workout.StepTargetTrackerTests.el split se cumple por debajo del objetivo (má...` |
| LT-42 | test/step_target_tracker_test.dart | PASS | PASS | `Workout.StepTargetTrackerTests.un paso sin objetivos no tiene resultado` |
| LT-43 | test/step_target_tracker_test.dart | PASS | PASS | `Workout.StepTargetTrackerTests.sin lecturas remando el objetivo no se cumple;...` |
| LT-44 | test/step_target_tracker_test.dart | PASS | PASS | `Workout.StepTargetTrackerTests.un paso no completado no tiene resultado y res...` |
| LT-45 | test/series_panels_test.dart | PASS | PASS | `Workout.SeriesPanelsTests.formatos` |

### R2.8 Deuda de la It. 3 que 00-deuda-it3 da por resuelta

| ID | Comprobado en la ronda 2 | Estado |
|---|---|---|
| QA-V3-02 | `RoutineMotionDebtTests`: 14 / 14 en PlayMode y 14 / 14 con simulador. it3 VID-01, igual byte a byte a la regeneración | Cerrado |
| QA-V3-03 | it3 DEMO-05, igual byte a byte a la regeneración | Cerrado |
| QA-V3-04 | `TypeLeavesTheCaretAtTheEnd`, `TapMovesTheFocusLikeAClick` y `PointerDownFocusesAButton` pasan, con y sin simulador. it3 CAP-16 y las DEMO, iguales | Cerrado |
| QA-V3-05 | `ItThreeDebtTests.IgnoreReasonsNameWhereTheyAreVerified` pasa (dotnet) | Cerrado |
| QA-V3-06 | `it4a` A = B = repo; `it3` e `it2` regeneradas = repo, en el orden `it4a` → `it4a` → `it3` → `it2`. Pasan `AtlasCharactersCoverEveryText` (dotnet) y `TabularNumberMeasuresEveryCharacterItDraws` (PlayMode, con y sin simulador) | Cerrado |
| QA-V3-07 | `SingleLineCommentTellsTheLegacyRuleAndTheDifference` pasa | Cerrado |
| QA-V3-08 | `DevelopmentBuildsAlwaysUseTheSameFolder` y `DevelopmentBuildRefusesANewFolderForReal` pasan. En la PC: 12 reglas del Firewall antes y después, ningún aviso. En esta ronda el build de simulador fue con `-AllowNewFolder` a `Builds/Windows/simulator-it3`, que ya tenía su regla (R2.4.1) | Cerrado |
| QA-V3-10 | `PresentationNeverDiscardsATask` pasa | Cerrado |
| QA-V3-12 | `WindowCaptureUsesTheVisibleFrame`, `Cap27PhotoNeverGoesStraightToTheRepo` y `WindowCaptureStartsTheExeForReal` pasan. `capture-cap27.ps1` no se corrió: es solo a pedido | Cerrado |
| Hallazgo `TabularNumber` (`×N`) | `TabularNumberMeasuresEveryCharacterItDraws` pasa; it3 CAP-10, igual | Cerrado |
| §11.1, el foco con el clic | Aplicado: DT4a-01. Después de un clic con el mouse no queda foco (E9) | Cerrado. Su efecto lateral, QA-V4a-04, sigue abierto y ahora también se ve en el editor |

### R2.9 Revisión estática

**Hilos.**

- En el código nuevo o tocado de la 4a no hay `async void`, `.Wait()`, `Thread.Sleep`, `Task.Run`, `new Thread` ni `GetAwaiter().GetResult()`. Se buscó en `Core/Workout`, `App/Workout`, `Infrastructure/Sessions`, `Infrastructure/Database/LocalStore.cs`, `Presentation/Workout`, `Presentation/Motion`, `Platform/Lifecycle`, `Platform/Power`, `Core/Common/FrameScheduler.cs`, `ForegroundTimeout.cs` y `Bootstrap/AppRoot.cs`.
- Hay dos `.Result` y ninguno bloquea: `SessionRecorder.cs:376`, nuevo de ROB-04, se lee después de comprobar `IsCompleted` y que la tarea no falló ni se canceló; `SqliteSessionRepository.cs:126` se lee en el hilo de la base, después de `IsCompleted`.
- La única espera es `WaitForPendingWork` con tope: la de `FlushAndWait` al perder el foco y la de `Abandon` al cerrar, 1 s como mucho (AC-WKT-06-15). Es del diseño.

**Asignaciones por cuadro del HUD.**

- `HudBudgetTests.NoAllocationsPerFrameWhileItOnlyBeats` y `AtMostSixtyVisibleElementsInTheWorstCase` pasan en PlayMode y con simulador.
- Lo nuevo no corre por cuadro: `StepPulse.Stop` y `Release`, `HeartbeatAnimator.Stop` y `Spinner.SetShown` solo escriben estilos con *structs* y `StyleKeyword`.
- El trabajo de más que quedaba después de salir (QA-V4a-02) ya no está: R2.5 y E5.
- El de los rodillos sobre elementos fuera del panel es finito y sigue: QA-V4a-05.

**Simulador fuera del release.**

- `Builds/Windows/normal/RowMate_Data/Managed` tiene solo los 7 ensamblados de la app, sin `RowMate.Simulator*`, y ninguno nombra `SimulatedBleService`, `RowingSimulator` ni el define.
- `BuildOutputInspector` está en verde.
- El build de simulador sí los trae, con sus `.pdb`.
- `ProjectSettings.asset` quedó sin el define después de todas las corridas, y el CI lo controla con `CommittedSettingsDoNotEnableTheSimulator`, que pasa.

**Repo público.**

- La búsqueda obligatoria del coordinador, sobre lo versionado (`git grep -nIE`) y sobre lo versionado y lo nuevo (`git grep --untracked -nIE`), da **0 coincidencias**; también sin distinguir mayúsculas (`-i`). Busca las rutas de usuario de Windows y de Git Bash y el prefijo de las carpetas de trabajo. Este informe no copia el patrón ni ningún literal, y se volvió a correr después de escribirlo.
- `tools/oracle/engine`:
  - Solo son versionables `pubspec.yaml`, que toma el legacy por ruta relativa (`../../../../rowmate-legacy`), y `test/engine_vectors_test.dart`, sin rutas.
  - Los archivos con rutas de la PC están todos en `.dart_tool/`, que git ignora (`.gitignore:46`), igual que `build/`, `.flutter-plugins*`, `pubspec_overrides.yaml` y `pubspec.lock` (`:47-50`). `git check-ignore -v` lo confirma.
- El nombre del equipo no aparece en el repo. El de la persona aparece solo en `LICENSE`, como titular del copyright: es intencional.
- Este informe cita el repo con rutas relativas, el legacy por su URL y el scratch como `<scratch>`. No nombra la carpeta del usuario, ni direcciones de red ni datos del equipo.

### R2.10 Observaciones sin issue

- **O-R2-01 · `FlushAndWait` devuelve `true` si `TryQueueCloseNow` no encola.** Pasa en todo caso en que no encola, también en uno que en la práctica no ocurre: la base quieta con una tarea sin completar. El valor no lo usa nadie en producción (`WorkoutEngine.cs:611` lo ignora). Si algún día se usa, que distinga "nada por escribir" de "no se pudo encolar".
- **O-R2-02 · Las observaciones de la ronda 1 (§9):**
  - **Siguen igual** O-03, el panel del simulador en español con la app en inglés (E5, e5d); O-04, la llave sobre la esquina de la última tarjeta con el aviso (E6, e6g); y O-05, el "…" en cian (CAP-21).
  - **No se volvieron a mirar** O-01 (el cuadro de 120 ms de la salida) ni O-02. En E5 se subió a 170 W justamente para que el color no alternara alrededor de 160.
- **O-R2-03 · El editor de Rutinas sale sin confirmar.** Es paridad con el legacy. Se nombra solo porque agrava QA-V4a-04.

### R2.11 Verificación hecha

1. **Legacy, solo lectura.** `git -C ../rowmate-legacy status --porcelain` vacío al empezar y al terminar (`3ae0f3a`). Se miró `docs/image-02.png`, `docs/image-03.png` y el listado de `lib/features/routines/`, con una búsqueda de `PopScope`. No se leyó `lib/core/strava/strava_secrets.dart`.
2. **Unity.** Una sola instancia por vez, todas lanzadas por QA y cerradas solas. No se tocó ningún proceso de Unity ajeno, y solo quedó el `unity.exe` de la Hub, como al empezar.
3. **Los builds.**
   - Se abrieron solo desde `Builds/Windows/simulator-it3` (R2.4.1).
   - Las dos instancias de QA se cerraron: la primera a la fuerza, a propósito (E8), y la segunda con `WM_CLOSE` (E10).
   - La instancia del usuario siguió abierta y QA no la tocó; tampoco su carpeta.
4. **El repo.** Sin commits. Cambió solo este archivo. La sonda temporal y su `.meta` se borraron, y `git status --porcelain` volvió a las 340 entradas del principio. Las carpetas de capturas del repo y los assets generados no cambiaron (md5).
5. **Las bases.** Las de prueba y la del equipo, en carpetas temporales o en `<scratch>`. La del usuario: R2.4.4.
6. **El equipo.** El registro de la ventana del producto, restaurado a como estaba (0 diferencias). No se cambió la configuración de energía ni la del Firewall.
7. **El repo público.** R2.9: 0 coincidencias, también después de escribir este informe.
